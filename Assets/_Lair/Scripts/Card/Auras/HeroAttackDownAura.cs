using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 공격력 영구 -25%. 무제한 지속 (HeroAuraRunner duration<0).
    //# PowerScale 에 _factor 를 1회 곱함 — 같은 카드 중복 선택 시 누적.
    [Serializable]
    public class HeroAttackDownAura : IHeroAura, IStatusVisual
    {
        public EVisual VisualKey => EVisual.AttackDownStatus;
        public Vector3 Offset => new Vector3(0.5f, 0.6f, 0f);

        private readonly IAttacker _attacker;
        private readonly float _factor;
        private bool _applied;

        public HeroAttackDownAura(IAttacker attacker, float factor = 0.75f)
        {
            _attacker = attacker;
            _factor = factor;
        }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null || _applied) return;
            _attacker.PowerScale *= _factor;
            _applied = true;
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            //# 무제한 효과 — 정상 흐름에선 Detach 안 됨.
            //# 풀 회수 시엔 MeleeAttacker.OnEnable 의 PowerScale=1 리셋이 복원.
        }
    }
}
