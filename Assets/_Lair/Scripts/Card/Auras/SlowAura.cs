using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 IMover.Speed 를 slowFactor 배로 일시 변경. OnDetached 시 백업값 복원.
    //# Tick 은 사용 X — 지속 시간 관리는 HeroAuraRunner.
    [Serializable]
    public class SlowAura : IHeroAura, IStatusVisual, IDebuffAxisAura
    {
        public ECardId IconCardId => ECardId.Slow;

        private readonly IMover _mover;
        private readonly float _factor;
        private float _backup;

        //# 역병귀 적중 둔화만 Debuff 축(true) — Swarm Slow 카드는 같은 타입이지만 false (chapter-5 §3.3 #5).
        public bool IsDebuffAxis { get; }

        public SlowAura(IMover mover, float slowFactor = 0.6f, bool isDebuffAxis = false)
        {
            _mover = mover;
            _factor = slowFactor;
            IsDebuffAxis = isDebuffAxis;
        }

        public void OnAttached(IHealth hero)
        {
            if (_mover == null) return;
            _backup = _mover.Speed;
            _mover.Speed = _backup * _factor;
        }

        public void Tick(IHealth hero, float dt) { /* 무동작 */ }

        public void OnDetached(IHealth hero)
        {
            if (_mover != null) _mover.Speed = _backup;
        }
    }
}
