using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 종족 강화 레벨 → 발광 세기만 표현(틴트 불변, 종족색 유지). 발광색은 SpeciesGlowColor(species) SoT 취득.
    //# 렌더러는 [SerializeField] 와이어링(Rule 02 §5). 세부는 기획서 §4.3 참조.
    public class MonsterEnhancementVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        //# index0 = Lv1 … (길이 5). 값 = 기획서 §4.1 [1.5, 1.9, 2.3, 2.7, 3.2] (6종 프리팹 동일).
        [SerializeField] private float[] _emissionByLevel;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";

        //# level: 0 = 미강화(발광 off), 1~N = _emissionByLevel[level-1] 세기 × SpeciesGlowColor(species).
        public void ApplyLevel(int level, EMonster species)
        {
            bool on = level >= 1 && _emissionByLevel != null && level <= _emissionByLevel.Length;
            float intensity = on ? Mathf.Max(0f, _emissionByLevel[level - 1]) : 0f;
            Color emission = on ? SpeciesVisual.SpeciesGlowColor(species) * intensity : Color.black;
            ApplyEmission(on, emission);
        }

        //# 발광 채널만 조작 — 키워드 토글 + _EmissionColor 주입.
        private void ApplyEmission(bool on, Color emission)
        {
            if (_renderers == null)
                return;
            for (int i = 0; i < _renderers.Length; i++)
            {
                Renderer rd = _renderers[i];
                if (rd == null)
                    continue;
                Material mat = rd.material;
                if (mat == null)
                    continue;
                if (on)
                {
                    mat.EnableKeyword(EmissionKeyword);
                }
                else
                {
                    mat.DisableKeyword(EmissionKeyword);
                }
                if (mat.HasProperty(EmissionColorId))
                {
                    mat.SetColor(EmissionColorId, emission);
                }
            }
        }

        //# 풀 재사용 리셋 — 레벨은 스폰 경로가 ApplyLevel 로 재지정하므로 여기선 발광 off 로 초기화(Rule 03 §4).
        private void OnEnable() => ApplyEmission(false, Color.black);

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRenderersForTest(Renderer[] r) => _renderers = r;
        public void SetEmissionByLevelForTest(float[] byLevel) => _emissionByLevel = byLevel;
#endif
    }
}
