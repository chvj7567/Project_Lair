using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 공격력 × _factor. OnDetached 시 백업값 복원.
    [Serializable]
    public class WeakenAura : IHeroAura
    {
        private readonly IAttacker _attacker;
        private readonly float _factor;
        private float _backup;

        public WeakenAura(IAttacker attacker, float factor = 0.5f)
        {
            _attacker = attacker;
            _factor = factor;
        }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null) return;
            _backup = _attacker.PowerScale;
            _attacker.PowerScale = _backup * _factor;
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            if (_attacker != null) _attacker.PowerScale = _backup;
        }
    }
}
