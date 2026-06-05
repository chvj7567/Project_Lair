using System;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 공포 — 영웅 _duration 초간 도주.
    [Serializable]
    public class FearEffect : ICardEffect
    {
        [SerializeField] private float _duration = 3f;

        //# 스컬 FX 를 영웅 머리 위에 거는 로컬 Y (피벗=발밑 기준).
        //# Knight CapsuleCollider 머리꼭대기 y≈1.8 + 여유 0.3.
        private const float FxLiftY = 2.1f;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            AutoCombatAI ai = heroT.GetComponent<AutoCombatAI>();
            if (ai == null) return;
            ctx.ApplyHeroAura(new FearAura(ai), _duration);

            //# 공포 적용 순간 영웅에 스컬 FX 부착 — 영웅이 이동하면 따라간다(인프라 null 시 무동작, 자동 풀 반환은 ReturnToPoolAfter).
            HeroSkillFx.SpawnAttached(EVisual.FearSkull, heroT, new Vector3(0f, FxLiftY, 0f), 1f);
        }
    }
}
