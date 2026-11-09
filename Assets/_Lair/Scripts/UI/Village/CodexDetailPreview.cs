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
        private static readonly Color Silhouette = Color.black;

        private RenderTexture _rt;
        private CHPoolable _instance;
        private IMonsterShowcase _showcase;
        private readonly List<(Transform, int)> _layers = new List<(Transform, int)>();
        private int _version;

        public void Open()
        {
            if (_rt != null)
                return;
            _rt = new RenderTexture(_textureSize, _textureSize, 16, RenderTextureFormat.ARGB32);
            if (_camera != null)
            {
                _camera.targetTexture = _rt;
                _camera.enabled = false;
            }
            if (_image != null)
            {
                _image.texture = _rt;
            }
        }

        //# 닫힘·탭 전환 — 인스턴스 반환 + RenderTexture 해제.
        public void Close()
        {
            _version++;
            ReturnInstance();
            if (_camera != null)
            {
                _camera.enabled = false;
                _camera.targetTexture = null;
            }
            if (_image != null)
            {
                _image.texture = null;
            }
            if (_rt != null)
            {
                _rt.Release();
                Destroy(_rt);
                _rt = null;
            }
        }

        //# monster = 이 칸 등급의 몬스터, baseMonster = 종족 기준. silhouette 면 1등급 프리팹 + 검은 틴트. tier 는 티어 시트(종족 공통)로 치환된다.
        public async Task ShowAsync(EMonster monster, EMonster baseMonster, int tier, bool silhouette, IMonsterTierSheetProvider tiers)
        {
            if (_rt == null)
                return;
            int version = ++_version;
            EMonster key = silhouette ? baseMonster : monster;
            GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            if (prefab == null && key != baseMonster)
            {
                Debug.LogWarning($"[CodexPreview] {key} 프리팹 로드 실패 — 기준 몬스터로 폴백");
                key = baseMonster;
                prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
            }
            if (prefab == null || version != _version || _rt == null)
                return;

            ReturnInstance();
            CHPoolable poolable = CHMPool.Instance.Pop(prefab, _holder);
            if (poolable == null)
                return;
            _instance = poolable;
            poolable.transform.localPosition = Vector3.zero;
            poolable.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            SetLayers(poolable.transform);

            LairCharacter lair = poolable.GetComponent<LairCharacter>();
            _showcase = lair != null ? lair.Get<IMonsterShowcase>() : null;
            if (_showcase != null)
            {
                _showcase.SetShowcaseMode(true);
            }
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
            camTransform.position = _holder.position - camTransform.forward * CameraDistance + camTransform.up * (height * 0.5f);
        }

        private void SetLayers(Transform root)
        {
            _layers.Clear();
            Transform[] all = root.GetComponentsInChildren<Transform>(true);
            foreach (Transform t in all)
            {
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
        }
    }
}
