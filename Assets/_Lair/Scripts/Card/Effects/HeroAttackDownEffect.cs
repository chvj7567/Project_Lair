using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Card
{
    //# 영웅 공격력 영구 -25%. 무제한 지속 오라로 부착 (HeroPoisonAura 와 같은 환경 카테고리).
    [Serializable]
    public class HeroAttackDownEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            IAttacker atk = heroT.GetComponent<IAttacker>();
            if (atk == null) return;
            ctx.ApplyHeroAura(new HeroAttackDownAura(atk), durationSeconds: -1f);
        }
    }
}
