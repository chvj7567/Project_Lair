using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 IMover.Speed 를 slowFactor 배로 일시 변경. OnDetached 시 백업값 복원.
    //# Tick 은 사용 X — 지속 시간 관리는 HeroAuraRunner.
    [Serializable]
    public class SlowAura : IHeroAura, IStatusVisual
    {
        public EVisual VisualKey => EVisual.SlowStatus;
        public Vector3 Offset => new Vector3(0f, 0.05f, 0f);

        private readonly IMover _mover;
        private readonly float _factor;
        private float _backup;

        public SlowAura(IMover mover, float slowFactor = 0.6f)
        {
            _mover = mover;
            _factor = slowFactor;
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
