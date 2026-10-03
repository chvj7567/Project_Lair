using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;

namespace Lair.Battle
{
    //# 스테이지 규칙 러너 (chapter-system §3.1) — 전투 시작 시 규칙 생성·활성, BattleClock 틱으로 갱신, 종료 시 일괄 비활성.
    //# 트리거 row 가 있는 규칙(보스 페이즈)은 영웅 HP 비율이 hpFraction 이하로 "처음" 내려가는 순간 1회 활성 — HeroSkillPhaseGate 래치를
    //# 재사용하므로 회복으로 임계를 재무장하지 않고(E-1), 한 프레임에 여러 임계를 건너뛰면 높은 값부터 순서대로 활성(E-2).
    public class StageRuleRunner
    {
        private readonly IStageRuleContext _ctx;
        private readonly IHealth _hero;
        private readonly List<IStageRule> _always = new List<IStageRule>();
        private readonly List<IStageRule> _triggered = new List<IStageRule>();
        private readonly List<IStageRule> _active = new List<IStageRule>();
        private readonly List<int> _newly = new List<int>();
        private HeroSkillPhaseGate _gate;
        private float _lastElapsed;
        private bool _started;
        private bool _shutdown;

        //# factory 미지정 시 StageRuleFactory.Create. 테스트는 Fake 규칙을 반환하는 factory 를 주입.
        public StageRuleRunner(IStageRuleContext ctx, IHealth hero, IReadOnlyList<StageRuleData> rules, Func<StageRuleData, IStageRule> factory = null)
        {
            _ctx = ctx;
            _hero = hero;
            Func<StageRuleData, IStageRule> create = factory ?? StageRuleFactory.Create;

            List<KeyValuePair<float, IStageRule>> triggered = new List<KeyValuePair<float, IStageRule>>();
            foreach (StageRuleData data in rules)
            {
                IStageRule rule = create(data);
                if (data.HasTrigger)
                {
                    triggered.Add(new KeyValuePair<float, IStageRule>(data.TriggerHpFraction, rule));
                }
                else
                {
                    _always.Add(rule);
                }
            }

            //# 높은 HP 임계부터 — 게이트 인덱스(오름차순)가 곧 활성 순서.
            triggered.Sort((a, b) => b.Key.CompareTo(a.Key));
            List<float> fractions = new List<float>(triggered.Count);
            foreach (KeyValuePair<float, IStageRule> pair in triggered)
            {
                fractions.Add(pair.Key);
                _triggered.Add(pair.Value);
            }
            if (fractions.Count > 0)
            {
                _gate = new HeroSkillPhaseGate(fractions);
            }
        }

        public int AlwaysRuleCount => _always.Count;
        public int TriggeredRuleCount => _triggered.Count;

        //# 스테이지 시작 — 상시 규칙 활성. 스포너 바인딩 직후, 첫 스포너 Tick 전에 호출해야 봉인이 첫 스폰보다 앞선다.
        public void Start()
        {
            if (_started || _shutdown)
                return;
            _started = true;
            foreach (IStageRule rule in _always)
            {
                rule.Activate(_ctx);
                _active.Add(rule);
            }
        }

        //# BattleClock.OnTick(elapsed) 에 구독. 일시정지 중(dt = 0)엔 활성·갱신을 미룬다 — 카드 팝업과 효과가 겹치지 않는다(E-4·E-5).
        public void Tick(float elapsed)
        {
            if (_started == false || _shutdown)
                return;

            float dt = elapsed - _lastElapsed;
            _lastElapsed = elapsed;
            if (dt <= 0f)
                return;

            if (_gate != null && _hero != null && _hero.IsAlive)
            {
                _gate.Poll(_hero.Ratio, _newly);
                for (int i = 0; i < _newly.Count; ++i)
                {
                    IStageRule rule = _triggered[_newly[i]];
                    rule.Activate(_ctx);
                    _active.Add(rule);
                }
            }

            for (int i = 0; i < _active.Count; ++i)
            {
                _active[i].Tick(elapsed, dt);
            }
        }

        //# 전투 종료 — 전 규칙 비활성화(부수효과 원복 + FX 풀 반환, E-6). 중복 호출 안전.
        public void Shutdown()
        {
            if (_shutdown)
                return;
            _shutdown = true;
            for (int i = _active.Count - 1; i >= 0; --i)
            {
                _active[i].Deactivate();
            }
            _active.Clear();
        }
    }
}
