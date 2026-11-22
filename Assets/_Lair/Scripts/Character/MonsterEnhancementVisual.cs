using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 종족 강화 레벨 → 4채널 표현의 유일한 진입점(Rule 02 §10 루트 파사드) — 발광(보조)·티어 오버레이·HP바 높이.
    //# 렌더러/하위 컴포넌트는 [SerializeField] 와이어링(Rule 02 §5). 세부는 monster-2d-conversion.md §6.3 참조.
    public class MonsterEnhancementVisual : MonoBehaviour
    {
        [SerializeField] private Renderer[] _renderers;
        //# index0 = Lv1 … (길이 5). 값 = 기획서 §4.1 [1.5, 1.9, 2.3, 2.7, 3.2] (6종 프리팹 동일).
        [SerializeField] private float[] _emissionByLevel;

        //# 신설(§6.3.8) — 레벨 0 이면 없음, 스폰 시 없을 수도 있는 하위(2D 전환 전 프리팹 호환 위해 null 허용).
        [SerializeField] private MonsterTierOverlay _tierOverlay;
        [SerializeField] private MonsterHpBar _hpBar;
        //# 티어 전신 시트 치환 대상(monster-tier-sheets §6) — null 허용(2D 전환 전 프리팹·테스트).
        [SerializeField] private MonsterVisual2D _visual2D;
        //# index0=Lv0(티어0) · 1=T1 · 2=T2 · 3=T3, 종별 §6.3.7 표의 월드 높이값.
        [SerializeField] private float[] _hpBarHeightByTier;

        //# 전투 컨텍스트가 주입(BattleController). null 이면 시트 치환 없이 기존 표(_hpBarHeightByTier)만 쓴다.
        private IMonsterTierSheetProvider _tierProvider;
        //# 등급 Variant 의 연출 진입점 — HP바 높이 x sizeMul 합성용(monster-grades §4). 1등급은 null → 배율 1.
        private IMonsterGradeVisual _gradeVisual;
        private const float HpBarPad = 0.08f;

        //# 마지막으로 적용된 표시 높이(월드, 등급 sizeMul·티어 시트 반영, HP바 하단 = 몸 높이 + 0.08). 쇼케이스 fit 계산 입력.
        public float AppliedHeightWorld { get; private set; }

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";
        public const int MaxLevel = EnhanceTierTable.MaxStoredLevel;

        public void SetTierSheetProvider(IMonsterTierSheetProvider provider) => _tierProvider = provider;

        //# 반드시 ApplyLevel 보다 먼저(Configure 가 sizeMul 을 정한 뒤) 주입.
        public void SetGradeVisual(IMonsterGradeVisual gradeVisual) => _gradeVisual = gradeVisual;

        //# 레벨(0~5)→외형 티어(0~3). 로더가 필요 목록을 만들 때도 같은 표를 쓴다.
        public static int TierOfLevel(int level) => EnhanceTierTable.TierOfLevel(level);

        //# level: 0 = 미강화, 1~N = _emissionByLevel[level-1] 세기. 세기 = max(등급 하한, 강화 곡선)(monster-grades §3.2), 0 이면 발광 off.
        //# 색 = 등급 발광색(등급 3~5) 또는 SpeciesGlowColor(species). 티어/HP바는 별도로 clamp 된 레벨로 파생(§6.3.8).
        public void ApplyLevel(int level, EMonster species)
        {
            bool curveOn = level >= 1 && _emissionByLevel != null && level <= _emissionByLevel.Length;
            float intensity = curveOn ? Mathf.Max(0f, _emissionByLevel[level - 1]) : 0f;
            bool on = intensity > 0f;
            Color glow = SpeciesVisual.SpeciesGlowColor(species);
            Color emission = on ? glow * intensity : Color.black;
            ApplyEmission(on, emission);

            int tier = TierOfLevel(level);
            _tierOverlay?.SetTier(tier);

            //# 티어 시트가 로드돼 있으면 치환·시트 HP바 높이, 주입자는 있는데 세트가 없으면(로드 실패) 풋내기 값으로 폴백(§7.3).
            MonsterTierSheetSet sheet = tier > 0 && _tierProvider != null ? _tierProvider.Find(MonsterGrades.BaseOf(species), tier) : null;
            _visual2D?.SetTierSheet(sheet);
            if (sheet != null && sheet.HpBarHeightWorld > 0f)
            {
                ApplyHpBarHeightValue(sheet.HpBarHeightWorld);
            }
            else
            {
                ApplyHpBarHeight(_tierProvider != null ? 0 : tier);
            }
        }

        //# HP바 하단 = heightWorld x sizeMul + 0.08 — 저장값은 heightWorld + 0.08 이라 패드를 빼고 곱한다. sizeMul 1(1등급) 이면 불변.
        private float ScaleBySize(float heightWithPad)
        {
            float size = _gradeVisual != null ? _gradeVisual.SizeMul : 1f;
            return (heightWithPad - HpBarPad) * size + HpBarPad;
        }

        private void ApplyHpBarHeightValue(float height)
        {
            AppliedHeightWorld = ScaleBySize(height);
            if (_hpBar == null)
                return;
            _hpBar.SetHeightAboveRoot(AppliedHeightWorld);
        }

        private void ApplyHpBarHeight(int tier)
        {
            if (_hpBarHeightByTier == null || tier < 0 || tier >= _hpBarHeightByTier.Length)
                return;
            AppliedHeightWorld = ScaleBySize(_hpBarHeightByTier[tier]);
            if (_hpBar == null)
                return;
            _hpBar.SetHeightAboveRoot(AppliedHeightWorld);
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
            _visual2D?.SetTierSheet(null);
            ApplyHpBarHeight(0);
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRenderersForTest(Renderer[] r) => _renderers = r;
        public void SetEmissionByLevelForTest(float[] byLevel) => _emissionByLevel = byLevel;
        public void SetTierOverlayForTest(MonsterTierOverlay overlay) => _tierOverlay = overlay;
        public void SetHpBarForTest(MonsterHpBar hpBar) => _hpBar = hpBar;
        public void SetHpBarHeightByTierForTest(float[] heights) => _hpBarHeightByTier = heights;
#endif
    }
}
