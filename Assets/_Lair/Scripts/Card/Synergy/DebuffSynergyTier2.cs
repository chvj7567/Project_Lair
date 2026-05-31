using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Debuff Tier2 (5장 임계) — HeroAttackDownAura 영구 부착 (×0.85).
    //# 기획서 §4.5: ApplyHeroAura(new HeroAttackDownAura(_attacker, 0.85f), -1f) 호출.
    //# HeroAttackDownAura.OnAttached 가 PowerScale ×= 0.85 → 동일 PowerScale 위에 카드 픽 ×0.75 곱연산 누적 자연.
    public class DebuffSynergyTier2 : IBuildSynergyTier
    {
        private const float Factor = 0.85f;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            IAttacker atk = heroT.GetComponent<IAttacker>();
            if (atk == null) return;
            ctx.ApplyHeroAura(new HeroAttackDownAura(atk, Factor), durationSeconds: -1f);
        }
    }
}
