using Lair.Data;

namespace Lair.Battle
{
    //# 수호 결계 (chapter-system §3.4) — 보스 페이즈 1회 발동. durationSec 동안 영웅이 받는 모든 피해에 damageTakenMul 곱.
    //# 영웅 이동·공격·저주 카드 효과는 무관. 만료/종료 시 역수로 원복(MarkOfDeath 와 같은 곱·역수 패턴).
    public class BossBulwarkRule : IStageRule
    {
        //# 알림 배너 문구 id — "수호 결계" (Strings_Ko 1204).
        public const int NoticeStringId = 1204;

        private readonly float _damageTakenMul;
        private readonly float _durationSec;

        private IStageRuleContext _ctx;
        private IStageFx _fx;
        private float _remain;
        private bool _applied;

        public BossBulwarkRule(StageRuleParams p)
        {
            _damageTakenMul = p.Get("damageTakenMul");
            _durationSec = p.Get("durationSec");
        }

        public EStageRule Id => EStageRule.BossBulwark;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            IsActive = true;
            _remain = _durationSec;
            _applied = true;
            _ctx.MultiplyHeroDamageTaken(_damageTakenMul);
            _fx = _ctx.ShowFx(EVisual.StageBulwarkFx, _ctx.HeroPosition, 1f);
            _ctx.ShowNotice(NoticeStringId);
        }

        public void Tick(float elapsed, float dt)
        {
            if (IsActive == false)
                return;

            _fx?.SetPosition(_ctx.HeroPosition);
            _remain -= dt;
            if (_remain <= 0f)
            {
                Deactivate();
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            if (_applied)
            {
                _applied = false;
                _ctx.MultiplyHeroDamageTaken(1f / _damageTakenMul);
            }
            if (_fx != null)
            {
                _fx.Release();
                _fx = null;
            }
        }
    }
}
