using Lair.Character;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Debuff Tier3 (7장 임계) — 영구 출혈 부착 (라운드 끝까지, ratio 0.01).
    //# 기획서 §4.2 / §10.3 / §10.4 — 영웅 이동 시 1s당 HP -1%.
    public class DebuffSynergyTier3 : IBuildSynergyTier
    {
        private const float Ratio = 0.01f;

        public void Apply(IBattleContext ctx)
        {
            IMover mover = ctx.GetHeroMover();
            if (mover == null) return;
            ctx.ApplyHeroAura(new EternalBleedAura(mover, Ratio), durationSeconds: -1f);
        }
    }
}
