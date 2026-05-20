using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 이동·공격 완전 정지. OnDetached 시 백업값 복원.
    [Serializable]
    public class TimeStopAura : IHeroAura, IStatusVisual
    {
        public EVisual VisualKey => EVisual.TimeStopStatus;
        public Vector3 Offset => new Vector3(0f, 0.5f, 0f);

        private readonly IMover _mover;
        private readonly IAttacker _attacker;
        private float _speedBackup;
        private bool _atkBackup;

        public TimeStopAura(IMover mover, IAttacker attacker)
        {
            _mover = mover;
            _attacker = attacker;
        }

        public void OnAttached(IHealth hero)
        {
            if (_mover != null)
            {
                _speedBackup = _mover.Speed;
                _mover.Speed = 0f;
                _mover.Stop();
            }
            if (_attacker != null)
            {
                _atkBackup = _attacker.Enabled;
                _attacker.Enabled = false;
            }
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            if (_mover != null) _mover.Speed = _speedBackup;
            if (_attacker != null) _attacker.Enabled = _atkBackup;
        }
    }
}
