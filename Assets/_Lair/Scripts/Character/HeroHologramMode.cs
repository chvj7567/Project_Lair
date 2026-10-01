using UnityEngine;

namespace Lair.Character
{
    //# 영웅 2D 비주얼 홀로그램 모드(Rule 02 §10 비주얼 루트) — scene-2d-conversion §5.5 · §7.11.
    //# 마을 투영진 위 푸른 유령상: 조명 안 받는 반투명 머티리얼 + 영웅 조명·그림자 숨김. 풀 재사용 시 기본값 복구.
    public class HeroHologramMode : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _renderers;
        [SerializeField] private Material _hologramMaterial;
        //# 홀로그램 동안 숨길 오브젝트(영웅 조명·그림자)
        [SerializeField] private GameObject[] _hideWhileHologram;
        [SerializeField] private float _alpha = 0.7f;

        private Material[] _originalMaterials;
        private Color[] _originalColors;

        private void Awake()
        {
            CacheOriginals();
        }

        //# 풀 재사용 리셋 — 이전 마을에서 켜 둔 홀로그램이 전투 영웅에 남지 않게(Rule 03 §4)
        private void OnEnable()
        {
            SetHologram(false);
        }

        public void SetHologram(bool on)
        {
            if (_renderers == null)
                return;
            if (_originalMaterials == null)
            {
                CacheOriginals();
            }
            for (int i = 0; i < _renderers.Length; i++)
            {
                SpriteRenderer r = _renderers[i];
                if (r == null)
                    continue;
                if (on)
                {
                    if (_hologramMaterial != null)
                    {
                        r.sharedMaterial = _hologramMaterial;
                    }
                    Color c = _originalColors[i];
                    c.a = _alpha;
                    r.color = c;
                }
                else
                {
                    r.sharedMaterial = _originalMaterials[i];
                    r.color = _originalColors[i];
                }
            }
            if (_hideWhileHologram == null)
                return;
            foreach (GameObject go in _hideWhileHologram)
            {
                if (go != null)
                {
                    go.SetActive(on == false);
                }
            }
        }

        private void CacheOriginals()
        {
            int n = _renderers != null ? _renderers.Length : 0;
            _originalMaterials = new Material[n];
            _originalColors = new Color[n];
            for (int i = 0; i < n; i++)
            {
                if (_renderers[i] == null)
                    continue;
                _originalMaterials[i] = _renderers[i].sharedMaterial;
                _originalColors[i] = _renderers[i].color;
            }
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void ConfigureForTest(SpriteRenderer[] renderers, Material hologram, GameObject[] hide)
        {
            _renderers = renderers;
            _hologramMaterial = hologram;
            _hideWhileHologram = hide;
            CacheOriginals();
        }
#endif
    }
}
