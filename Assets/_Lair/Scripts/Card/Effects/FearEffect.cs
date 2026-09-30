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

        //# 스컬 FX 로컬 Y 오프셋 — 시트 피벗이 영웅 발이라 0(해골 높이는 시트에 포함).
        private const float FxLiftY = 0f;

        public void Apply(IBattleContext ctx)
        {
            Transform heroT = ctx.GetHeroTransform();
            if (heroT == null) return;
            AutoCombatAI ai = heroT.GetComponent<AutoCombatAI>();
            if (ai == null) return;
            ctx.ApplyHeroAura(new FearAura(ai), _duration);

            //# 공포 적용 순간 영웅에 스컬 FX 부착 — 영웅이 이동하면 따라간다(인프라 null 시 무동작, 재생 종료 시 SpriteSheetFx 가 풀 반환).
            HeroSkillFx.SpawnAttached(EVisual.FearSkull, heroT, new Vector3(0f, FxLiftY, 0f), 1f);
        }
    }
}
