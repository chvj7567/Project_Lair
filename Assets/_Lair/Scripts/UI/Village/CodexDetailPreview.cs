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
    public class CodexDetailPreview : MonoBehaviour
    {
        [SerializeField] private RawImage _image;
        [SerializeField] private Camera _camera;         //# 도감 전용 카메라(평소 비활성) — 프리팹에서 holder 를 바라보게 배치
        [SerializeField] private Transform _holder;      //# 화면 밖 무대 앵커
        [SerializeField] private int _previewLayer;      //# 도감 전용 레이어(TagManager 등록 필요) — 카메라 cullingMask 와 일치
        [SerializeField] private int _textureSize = 256;

        private const float FitPad = 1.25f;
        private const float CameraDistance = 10f;
        private const float MinHeightWorld = 0.5f;
        private const int LoadTimeoutMs = 5000;
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
        }
        private int _version;
        private Transform _stageRoot;
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

        //# monster = 이 칸 등급의 몬스터, baseMonster = 종족 기준. silhouette 면 1등급 프리팹 + 검은 틴트. tier 는 티어 시트(종족 공통)로 치환된다.
        public async Task ShowAsync(EMonster monster, EMonster baseMonster, int tier, bool silhouette, IMonsterTierSheetProvider tiers)
        {
            if (_rt == null)
                return;
            int version = ++_version;
            try
            {
                EMonster key = silhouette ? baseMonster : monster;
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
                MonsterLookApplier.Apply(poolable.gameObject, key, baseMonster, tier == 0 ? 0 : CodexRules.MinLevelOfTier(tier), true, tiers);

                if (_image != null)
                {
                    _image.color = silhouette ? Silhouette : Color.white;
                }
                Fit();
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

        //# Push 전에 쇼케이스 모드·레이어 원복 — 풀은 쇼케이스·전투와 공유.
        private void ReturnInstance()
        {
            if (_instance == null)
                return;
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
