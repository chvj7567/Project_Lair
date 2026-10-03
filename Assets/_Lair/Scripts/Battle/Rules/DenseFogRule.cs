using Lair.Data;

namespace Lair.Battle
{
    //# 짙은 안개 (chapter-system §3.4) — 몬스터 공격 사거리 배율 하향(모든 사거리 보정 이후 마지막 곱, 하한 minRange).
    //# 배율 적용은 컨텍스트(BattleController.ApplyMonsterStats 단일 경로)가 담당 — 규칙은 값만 건다.
    public class DenseFogRule : IStageRule
    {
        private readonly float _monsterRangeMul;
        private readonly float _minRange;

        private IStageRuleContext _ctx;
        private IStageFx _fx;

        public DenseFogRule(StageRuleParams p)
        {
            _monsterRangeMul = p.Get("monsterRangeMul");
            _minRange = p.Get("minRange");
        }

        public EStageRule Id => EStageRule.DenseFog;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            IsActive = true;
            _ctx.SetMonsterRangeRule(_monsterRangeMul, _minRange);
            _fx = _ctx.ShowFx(EVisual.StageFogFx, _ctx.ArenaCenter, 1f);
        }

        public void Tick(float elapsed, float dt) { }

        public void Deactivate()
        {
            if (IsActive == false)
                return;
            IsActive = false;
            _ctx.ClearMonsterRangeRule();
            if (_fx != null)
            {
                _fx.Release();
                _fx = null;
            }
        }
    }
}
