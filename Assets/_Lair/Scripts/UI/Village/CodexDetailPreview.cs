using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 상세 큰 미리보기(monster-codex §5.2·§8) — 쇼케이스 모드 몬스터 프리팹을 도감 전용 카메라로 찍어 RenderTexture(256x256) → RawImage.
    //# 팝업 오픈에 Open, 닫힘/탭 전환에 Close. 프리팹은 CHMPool 로 Pop/Push(공유 풀 — Push 전에 쇼케이스 모드·레이어를 원복한다).
    //# 잠금 칸은 1등급 기준 프리팹을 같은 방식으로 찍고 RawImage 를 검게 칠해 실루엣(이펙트 없음)으로 보인다.
    //# 실행 순서를 앞으로 - LateUpdate 의 방향·위치 고정이 MonsterVisual2D(좌우 판정)·이펙트 빌보드보다 먼저 돌아 항상 같은 방향으로 판정되게 한다.
    [DefaultExecutionOrder(-1000)]
    public class CodexDetailPreview : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Camera _camera;         //# 도감 전용 카메라(평소 비활성) — 프리팹에서 holder 를 바라보게 배치
        [SerializeField] private Transform _holder;      //# 화면 밖 무대 앵커
        [SerializeField] private int _previewLayer;      //# 도감 전용 레이어(TagManager 등록 필요) — 카메라 cullingMask 와 일치
        [SerializeField] private int _textureSize = 256;
        //# 선곱 알파 합성 머티리얼 — RT 에 가산 이펙트를 그린 뒤 일반 UI 알파 합성하면 이펙트가 옅어지므로(알파 누적 안 됨) RawImage 를 선곱으로 합성한다. null 이면 기본 UI.
        [SerializeField] private Material _compositeMaterial;

        private const float FitPad = 1.25f;
        private const float CameraDistance = 10f;
        private const float MinHeightWorld = 0.5f;
        private const int LoadTimeoutMs = 5000;
        private const float FitMargin = 0.10f;        //# 자동 프레이밍 여백(반경 기준 +10%)
        private const float MinFitHalf = 0.25f;       //# 너무 작은 몸이 과도하게 확대되지 않게 하는 하한(월드)
        private const float FitWindowSec = 1.5f;      //# Idle 애니·이펙트 프레임 변화를 모으는 시간(이 동안의 bounds 합집합)
        private static readonly Color Silhouette = Color.black;

        private RenderTexture _rt;
        private CHPoolable _instance;
        private IMonsterShowcase _showcase;
        private readonly List<(Transform, int)> _layers = new List<(Transform, int)>();
        //# 풀 인스턴스별 1회 캐싱(Rule 02 §5) — 같은 인스턴스를 다시 Pop 해도 GetComponent* 를 반복하지 않는다.
        private readonly Dictionary<CHPoolable, Cached> _cache = new Dictionary<CHPoolable, Cached>();

        private sealed class Cached
        {
            public IMonsterShowcase Showcase;
            public Transform[] Transforms;
            public SpriteRenderer[] Renderers;
        }
        private const string FxShaderName = "Lair/GradeFxAdditive";
        //# 실루엣(미해금) 동안 가산 이펙트 렌더러의 원래 머티리얼 — ReturnInstance 가 풀 반환 전에 복원한다.
        private readonly List<(SpriteRenderer, Material)> _swappedFx = new List<(SpriteRenderer, Material)>();
        private int _version;
        private Transform _stageRoot;
        private SpriteRenderer[] _renderers = System.Array.Empty<SpriteRenderer>();
        private bool _fitActive;
        private float _fitUntil;
        private bool _fitHasData;
        private Vector2 _fitMin;
        private Vector2 _fitMax;
        private Camera _mainCamera;
        private int _mainMask;

        public void Open()
        {
            if (_rt != null)
                return;
            DetachStage();
            RenderTextureFormat format = SystemInfo.SupportsRenderTextureFormat(RenderTextureFormat.ARGB32) ? RenderTextureFormat.ARGB32 : RenderTextureFormat.Default;
            RenderTexture rt = new RenderTexture(_textureSize, _textureSize, 16, format);
            rt.antiAliasing = 1;
            if (rt.Create() == false)
            {
                Debug.LogError($"[CodexPreview] RenderTexture 생성 실패 ({_textureSize}x{_textureSize}, {format}) - 미리보기 비활성");
                Destroy(rt);
                SetImageVisible(false);
                return;
            }
            _rt = rt;
            if (_camera != null)
            {
                _camera.targetTexture = _rt;
                _camera.cullingMask = 1 << _previewLayer;
                _camera.enabled = false;
            }
            HideLayerFromMainCamera();
            if (_image != null)
            {
                _image.texture = _rt;
                if (_compositeMaterial != null)
                {
                    _image.material = _compositeMaterial;
                }
            }
            //# 첫 렌더 전에는 텍스처 내용이 정의되지 않아 흰 사각형으로 보일 수 있다 - 그림이 준비될 때까지 숨긴다.
            SetImageVisible(false);
        }

        //# 닫힘·탭 전환 - 인스턴스 반환 + RenderTexture 해제.
        public void Close()
        {
            _version++;
            ReturnInstance();
            if (_camera != null)
            {
                _camera.enabled = false;
                _camera.targetTexture = null;
            }
            RestoreMainCamera();
            if (_image != null)
            {
                _image.texture = null;
            }
            SetImageVisible(false);
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }
        }

        //# 무대(카메라·홀더)를 캔버스 밖 씬 루트로 뺀다 - 캔버스 스케일이 몬스터 크기·카메라 높이에 곱해져 해상도별로 화면 밖/과대가 되는 것을 막는다.
        private void DetachStage()
        {
            Transform stage = _holder != null ? _holder.parent : null;
            if (stage == null || stage == _stageRoot)
                return;
            stage.SetParent(null, false);
            stage.position = Vector3.zero;
            stage.rotation = Quaternion.identity;
            stage.localScale = Vector3.one;
            _stageRoot = stage;
        }

        //# 메인 카메라가 도감 레이어(화면 밖 무대)를 같이 그리지 않게 한다(이중 렌더 방지).
        private void HideLayerFromMainCamera()
        {
            if (_mainCamera != null)
                return;
            Camera main = Camera.main;
            if (main == null)
                return;
            _mainCamera = main;
            _mainMask = main.cullingMask;
            main.cullingMask = _mainMask & ~(1 << _previewLayer);
        }

        private void RestoreMainCamera()
        {
            if (_mainCamera != null)
            {
                _mainCamera.cullingMask = _mainMask;
            }
            _mainCamera = null;
        }

        private void SetImageVisible(bool visible)
        {
            if (_image != null)
            {
                _image.enabled = visible;
            }
        }

        //# monster = 이 칸 등급의 몬스터, baseMonster = 종족 기준. silhouette 면 검은 틴트(이펙트 포함). tier 는 티어 시트(종족 공통)로 치환된다.
        public async Task ShowAsync(EMonster monster, EMonster baseMonster, int tier, bool silhouette, IMonsterTierSheetProvider tiers)
        {
            if (_rt == null)
                return;
            int version = ++_version;
            try
            {
                //# 미해금도 그 칸 등급 프리팹을 로드해 등급 이펙트·크기를 그린다(검은 틴트가 이펙트까지 검게). 1등급은 monster == baseMonster.
                EMonster key = monster;
                GameObject prefab = await LoadWithTimeout(key);
                if (prefab == null && key != baseMonster && version == _version)
                {
                    Debug.LogWarning($"[CodexPreview] {key} 프리팹 로드 실패 - 기준 몬스터로 폴백");
                    key = baseMonster;
                    prefab = await LoadWithTimeout(key);
                }
                if (version != _version || _rt == null || _holder == null)
                    return;
                if (prefab == null)
                {
                    Debug.LogError($"[CodexPreview] {key} 프리팹 로드 실패 - 미리보기 숨김");
                    SetImageVisible(false);
                    return;
                }

                ReturnInstance();
                CHPoolable poolable = CHMPool.Instance.Pop(prefab, _holder);
                if (poolable == null)
                {
                    Debug.LogError($"[CodexPreview] 풀에서 {key} 를 꺼내지 못함 - 미리보기 숨김");
                    SetImageVisible(false);
                    return;
                }
                _instance = poolable;
                poolable.transform.localPosition = Vector3.zero;
                poolable.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
                Cached cached = CacheOf(poolable);
                SetLayers(cached.Transforms);
                _showcase = cached.Showcase;
                if (_showcase == null)
                {
                    //# 쇼케이스 모드가 없으면 전투 컴포넌트가 켜진 채 돌아다닌다 - 보여 주지 않고 되돌린다.
                    Debug.LogError($"[CodexPreview] {key} 프리팹에 MonsterShowcase 없음 - 미리보기 숨김");
                    ReturnInstance();
                    SetImageVisible(false);
                    return;
                }
                _showcase.SetShowcaseMode(true);
                //# 풀 인스턴스는 직전 사용(전투·쇼케이스 보행)의 Animator Speed 가 남아 있을 수 있다 - 제자리 Idle 로 고정.
                _showcase.SetMoving(false);
                _showcase.SetVisualOffset(Vector3.zero);
                //# 상세는 이펙트 슬롯 상한을 넘어서도 전부 켠다(칸·쇼케이스가 먼저 소진해도 풋내기 불씨만 남지 않게).
                GradeFxBudget.BypassCap = true;
                try
                {
                    MonsterLookApplier.Apply(poolable.gameObject, key, baseMonster, tier == 0 ? 0 : CodexRules.MinLevelOfTier(tier), true, tiers);
                }
                finally
                {
                    GradeFxBudget.BypassCap = false;
                }

                if (silhouette)
                {
                    SilhouetteFx(cached.Renderers);
                }
                if (_image != null)
                {
                    _image.color = silhouette ? Silhouette : Color.white;
                }
                Fit();
                _renderers = cached.Renderers;
                BeginAutoFrame();
                if (_camera != null)
                {
                    _camera.enabled = true;
                }
                //# 카메라가 한 프레임 그린 뒤에 보여 준다(흰 사각형 방지).
                await Task.Yield();
                if (version != _version || _rt == null)
                    return;
                SetImageVisible(_rt.IsCreated());
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[CodexPreview] 미리보기 실패 - {e}");
                SetImageVisible(false);
            }
        }

        private static async Task<GameObject> LoadWithTimeout(EMonster key)
        {
            Task<GameObject> load = CHMResource.Instance.LoadAsync<GameObject>(key);
            Task finished = await Task.WhenAny(load, Task.Delay(LoadTimeoutMs));
            if (finished != load)
            {
                Debug.LogError($"[CodexPreview] {key} 프리팹 로드 시간 초과({LoadTimeoutMs}ms)");
                return null;
            }
            return load.Result;
        }

        //# 어떤 컴포넌트가 몸을 움직여도 도감 미리보기는 holder 중앙·고정 방향에 둔다.
        private void LateUpdate()
        {
            if (_instance == null)
                return;
            Transform t = _instance.transform;
            t.localPosition = Vector3.zero;
            t.rotation = Quaternion.Euler(0f, 90f, 0f);
            if (_fitActive)
            {
                AccumulateBounds();
                ApplyAutoFrame();
                if (Time.unscaledTime >= _fitUntil)
                {
                    _fitActive = false;
                }
            }
        }

        //# 자동 프레이밍 시작 - 몸+이펙트 SpriteRenderer 의 bounds 합집합(카메라 기준 좌표)을 FitWindowSec 동안 모아 직교 크기·위치를 정한다.
        private void BeginAutoFrame()
        {
            _fitHasData = false;
            _fitActive = true;
            _fitUntil = Time.unscaledTime + FitWindowSec;
        }

        //# 화면에 그려지는 렌더러(활성 + 스프라이트 있음)의 월드 bounds 8개 모서리를 카메라 회전 기준(holder 원점)으로 바꿔 x/y 최소·최대를 넓힌다.
        private void AccumulateBounds()
        {
            if (_camera == null || _holder == null)
                return;
            Quaternion inverse = Quaternion.Inverse(_camera.transform.rotation);
            Vector3 origin = _holder.position;
            foreach (SpriteRenderer sr in _renderers)
            {
                if (sr == null || sr.enabled == false || sr.sprite == null || sr.gameObject.activeInHierarchy == false)
                    continue;
                Bounds b = sr.bounds;
                for (int i = 0; i < 8; ++i)
                {
                    Vector3 corner = b.center + Vector3.Scale(b.extents, new Vector3((i & 1) == 0 ? -1f : 1f, (i & 2) == 0 ? -1f : 1f, (i & 4) == 0 ? -1f : 1f));
                    Vector3 local = inverse * (corner - origin);
                    if (_fitHasData == false)
                    {
                        _fitMin = new Vector2(local.x, local.y);
                        _fitMax = _fitMin;
                        _fitHasData = true;
                        continue;
                    }
                    _fitMin = Vector2.Min(_fitMin, new Vector2(local.x, local.y));
                    _fitMax = Vector2.Max(_fitMax, new Vector2(local.x, local.y));
                }
            }
        }

        //# 합집합이 정사각 화면에 들어가도록 반경 = max(가로, 세로 반폭) x (1 + 여백), 중심은 합집합 중심. 종족 시트 크기·등급 배율·이펙트가 달라도 항상 같은 비율로 채운다.
        private void ApplyAutoFrame()
        {
            if (_fitHasData == false || _camera == null || _holder == null)
                return;
            float half = Mathf.Max(MinFitHalf, Mathf.Max((_fitMax.x - _fitMin.x) * 0.5f, (_fitMax.y - _fitMin.y) * 0.5f)) * (1f + FitMargin);
            Vector2 center = (_fitMin + _fitMax) * 0.5f;
            _camera.orthographic = true;
            _camera.orthographicSize = half;
            Transform cam = _camera.transform;
            cam.position = _holder.position - cam.forward * CameraDistance + cam.right * center.x + cam.up * center.y;
        }

        //# 표시 높이에 맞춰 직교 카메라 크기·위치를 정한다(등급 sizeMul 반영된 DisplayHeightWorld).
        private void Fit()
        {
            if (_camera == null || _holder == null)
                return;
            float height = _showcase != null ? Mathf.Max(MinHeightWorld, _showcase.DisplayHeightWorld) : 1f;
            _camera.orthographic = true;
            _camera.orthographicSize = height * 0.5f * FitPad;
            Transform camTransform = _camera.transform;
            //# 몸 스프라이트·이펙트가 메인 카메라 방향으로 빌보드하므로 같은 회전으로 찍어야 납작해 보이지 않는다.
            Camera main = Camera.main;
            if (main != null)
            {
                camTransform.rotation = main.transform.rotation;
            }
            camTransform.position = _holder.position - camTransform.forward * CameraDistance + camTransform.up * (height * 0.5f);
        }

        private Cached CacheOf(CHPoolable poolable)
        {
            if (_cache.TryGetValue(poolable, out Cached cached))
                return cached;
            LairCharacter lair = poolable.GetComponent<LairCharacter>();
            cached = new Cached
            {
                Showcase = lair != null ? lair.Get<IMonsterShowcase>() : null,
                Transforms = poolable.GetComponentsInChildren<Transform>(true),
                Renderers = poolable.GetComponentsInChildren<SpriteRenderer>(true),
            };
            _cache[poolable] = cached;
            return cached;
        }

        private void SetLayers(Transform[] all)
        {
            _layers.Clear();
            foreach (Transform t in all)
            {
                if (t == null)
                    continue;
                _layers.Add((t, t.gameObject.layer));
                t.gameObject.layer = _previewLayer;
            }
        }

        //# 미해금 — 가산 이펙트는 검은 틴트 + 알파 0 이라 RT 에서 사라지므로, 이펙트 렌더러를 몸과 같은 알파 합성 머티리얼로 바꿔 검은 모양(알파 유지)으로 남긴다.
        private void SilhouetteFx(SpriteRenderer[] renderers)
        {
            Material alphaMaterial = null;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null && sr.sharedMaterial != null && sr.sharedMaterial.shader != null && sr.sharedMaterial.shader.name != FxShaderName)
                {
                    alphaMaterial = sr.sharedMaterial;
                    break;
                }
            }
            if (alphaMaterial == null)
                return;
            foreach (SpriteRenderer sr in renderers)
            {
                if (sr != null && sr.sharedMaterial != null && sr.sharedMaterial.shader != null && sr.sharedMaterial.shader.name == FxShaderName)
                {
                    _swappedFx.Add((sr, sr.sharedMaterial));
                    sr.sharedMaterial = alphaMaterial;
                }
            }
        }

        private void RestoreFx()
        {
            foreach ((SpriteRenderer sr, Material original) in _swappedFx)
            {
                if (sr != null)
                {
                    sr.sharedMaterial = original;
                }
            }
            _swappedFx.Clear();
        }

        //# Push 전에 쇼케이스 모드·레이어 원복 — 풀은 쇼케이스·전투와 공유.
        private void ReturnInstance()
        {
            if (_instance == null)
                return;
            RestoreFx();
            if (_showcase != null)
            {
                _showcase.SetShowcaseMode(false);
            }
            foreach ((Transform t, int layer) in _layers)
            {
                if (t != null)
                {
                    t.gameObject.layer = layer;
                }
            }
            _layers.Clear();
            CHMPool.Instance.Push(_instance);
            _instance = null;
            _showcase = null;
            _fitActive = false;
            _renderers = System.Array.Empty<SpriteRenderer>();
        }

        private void OnDestroy()
        {
            Close();
            if (_stageRoot != null)
            {
                Destroy(_stageRoot.gameObject);
            }
        }
    }
}
