using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 종족 강화 레벨 → 4채널 표현의 유일한 진입점(Rule 02 §10 루트 파사드) — 발광(보조)·티어 오버레이·바닥 문장·HP바 높이.
    //# 렌더러/하위 컴포넌트는 [SerializeField] 와이어링(Rule 02 §5). 세부는 monster-2d-conversion.md §6.3 참조.
    public class MonsterEnhancementVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        //# index0 = Lv1 … (길이 5). 값 = 기획서 §4.1 [1.5, 1.9, 2.3, 2.7, 3.2] (6종 프리팹 동일).
        [SerializeField] private float[] _emissionByLevel;

        //# 신설(§6.3.8) — 레벨 0 이면 없음, 스폰 시 없을 수도 있는 하위(2D 전환 전 프리팹 호환 위해 null 허용).
        [SerializeField] private MonsterTierOverlay _tierOverlay;
        [SerializeField] private MonsterEnhanceSigil _sigil;
        [SerializeField] private MonsterHpBar _hpBar;
        //# index0=Lv0(티어0) · 1=T1 · 2=T2 · 3=T3, 종별 §6.3.7 표의 월드 높이값.
        [SerializeField] private float[] _hpBarHeightByTier;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";
        private const int MaxLevel = 5;

        //# 레벨(0~5)→외형 티어(0~3) — Lv1·Lv3·Lv5 에서 한 단계씩 성장(§6.3.1).
        private static readonly int[] TierByLevel = { 0, 1, 1, 2, 2, 3 };
        //# 레벨(0~5)→바닥 문장 단계(0=숨김·1=링1·2=링2·3=링2+파편) — Lv2·Lv4·Lv5 에서 갱신(§6.3.1).
        private static readonly int[] SigilStageByLevel = { 0, 0, 1, 1, 2, 3 };

        //# level: 0 = 미강화(발광 off), 1~N = _emissionByLevel[level-1] 세기 × SpeciesGlowColor(species).
        //# 발광 판정은 기존 계약 그대로(변경 없음) — 티어/문장/HP바는 별도로 clamp 된 레벨로 파생(§6.3.8).
        public void ApplyLevel(int level, EMonster species)
        {
            bool on = level >= 1 && _emissionByLevel != null && level <= _emissionByLevel.Length;
            float intensity = on ? Mathf.Max(0f, _emissionByLevel[level - 1]) : 0f;
            Color emission = on ? SpeciesVisual.SpeciesGlowColor(species) * intensity : Color.black;
            ApplyEmission(on, emission);

            int clampedLevel = Mathf.Clamp(level, 0, MaxLevel);
            int tier = TierByLevel[clampedLevel];
            _tierOverlay?.SetTier(tier);

            int sigilStage = SigilStageByLevel[clampedLevel];
            _sigil?.SetStage(sigilStage, SpeciesVisual.SpeciesGlowColor(species));

            ApplyHpBarHeight(tier);
        }

        private void ApplyHpBarHeight(int tier)
        {
            if (_hpBar == null || _hpBarHeightByTier == null)
                return;
            if (tier < 0 || tier >= _hpBarHeightByTier.Length)
                return;
            _hpBar.SetHeightAboveRoot(_hpBarHeightByTier[tier]);
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

        //# 풀 재사용 리셋 — 레벨은 스폰 경로가 ApplyLevel 로 재지정하므로 여기선 네 채널 모두 레벨 0 상태로 초기화(Rule 03 §4).
        private void OnEnable()
        {
            ApplyEmission(false, Color.black);
            _tierOverlay?.SetTier(0);
            _sigil?.SetStage(0, Color.white);
            ApplyHpBarHeight(0);
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRenderersForTest(Renderer[] r) => _renderers = r;
        public void SetEmissionByLevelForTest(float[] byLevel) => _emissionByLevel = byLevel;
        public void SetTierOverlayForTest(MonsterTierOverlay overlay) => _tierOverlay = overlay;
        public void SetSigilForTest(MonsterEnhanceSigil sigil) => _sigil = sigil;
        public void SetHpBarForTest(MonsterHpBar hpBar) => _hpBar = hpBar;
        public void SetHpBarHeightByTierForTest(float[] heights) => _hpBarHeightByTier = heights;
#endif
    }
}
