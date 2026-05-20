using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 IAttacker.Enabled = false. OnDetached 시 백업값으로 복원.
    //# MeleeAttacker 의 OnEnable 에서 _lastAttackTime 리셋되므로
    //# 침묵 해제 직후 즉시 공격 가능 — Rule 12 풀 재사용 정책과 일치.
    [Serializable]
    public class SilenceAura : IHeroAura
    {
        private readonly IAttacker _attacker;
        private bool _backup;

        public SilenceAura(IAttacker attacker)
        {
            _attacker = attacker;
        }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null) return;
            _backup = _attacker.Enabled;
            _attacker.Enabled = false;
        }

        public void Tick(IHealth hero, float dt) { /* 무동작 */ }

        public void OnDetached(IHealth hero)
        {
            if (_attacker != null) _attacker.Enabled = _backup;
        }
    }
}
