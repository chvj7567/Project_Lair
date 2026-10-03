using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 성수 샘 (chapter-system §3.4) — 주기적으로 열리는 회복 지대. 개방 중 영웅이 반경 안이면 0.5초 틱으로 회복(소수 누적).
    //# 회복은 Health.Heal 이라 HP 임계(패시브/스킬/보스 트리거)의 래치된 최저값을 재무장시키지 않는다(E-1).
    public class HolySpringRule : IStageRule
    {
        //# 회복 틱 간격 — §3.4 "회복은 0.5초 틱" (파라미터 집합에 없는 명세 고정값).
        private const float HealTickSec = 0.5f;
        //# 소수 누적의 부동소수 오차 보정 — 8.0 이 7.99999 로 계산돼 한 틱 늦게 반영되는 것을 막는다.
        private const float CarryEpsilon = 0.0001f;

        private readonly float _centerX;
        private readonly float _centerZ;
        private readonly float _radius;
        private readonly float _healPercentPerSec;
        private readonly float _activeSec;
        private readonly float _periodSec;
        private readonly float _firstActiveSec;

        private IStageRuleContext _ctx;
        private IStageFx _fx;
        private float _tickAcc;
        private float _carry;

        public HolySpringRule(StageRuleParams p)
        {
            _centerX = p.Get("centerX");
            _centerZ = p.Get("centerZ");
            _radius = p.Get("radius");
            _healPercentPerSec = p.Get("healPercentPerSec");
            _activeSec = p.Get("activeSec");
            _periodSec = p.Get("periodSec");
            _firstActiveSec = p.Get("firstActiveSec");
        }

        public EStageRule Id => EStageRule.HolySpring;
        public bool IsActive { get; private set; }

        //# 개방 구간 = [first + k x period, first + k x period + active). BattleClock 기준 elapsed.
        public static bool IsOpen(float elapsed, float firstActiveSec, float periodSec, float activeSec)
        {
            if (elapsed < firstActiveSec || periodSec <= 0f)
                return false;
            float since = elapsed - firstActiveSec;
            float inCycle = since - (float)Math.Floor(since / periodSec) * periodSec;
            return inCycle < activeSec;
        }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            _tickAcc = 0f;
            _carry = 0f;
            IsActive = true;
        }

        public void Tick(float elapsed, float dt)
        {
            if (IsActive == false)
                return;

            bool open = IsOpen(elapsed, _firstActiveSec, _periodSec, _activeSec);
            UpdateFx(open);

            _tickAcc += dt;
            while (_tickAcc >= HealTickSec)
            {
                _tickAcc -= HealTickSec;
                if (open && HeroInside())
                {
                    HealOnce();
                }
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            ReleaseFx();
        }

        private Vector3 Center()
        {
            return _ctx.ArenaCenter + new Vector3(_centerX, 0f, _centerZ);
        }

        private bool HeroInside()
        {
            Vector3 offset = _ctx.HeroPosition - Center();
            offset.y = 0f;
            return offset.sqrMagnitude <= _radius * _radius;
        }

        //# 초당 최대 HP 의 N% x 틱 길이를 소수로 누적 → 정수분만 반영. 최대 HP 초과는 Heal 이 막는다.
        private void HealOnce()
        {
            _carry += _ctx.HeroMaxHp * (_healPercentPerSec / 100f) * HealTickSec;
            int whole = (int)(_carry + CarryEpsilon);
            if (whole < 1)
                return;
            _carry -= whole;
            _ctx.HealHero(whole);
        }

        private void UpdateFx(bool open)
        {
            if (open && _fx == null)
            {
                _fx = _ctx.ShowFx(EVisual.StageSpringFx, Center(), _radius * 2f);
            }
            else if (open == false)
            {
                ReleaseFx();
            }
        }

        private void ReleaseFx()
        {
            if (_fx == null)
                return;
            _fx.Release();
            _fx = null;
        }
    }
}
