using System.Globalization;

namespace Lair.Card
{
    //# Debuff Tier1 (3장 임계) — 역병귀 독 최대 중첩 +2 (3 → 5, monster-growth-currencies §15.2). 기획서 §4.2 표.
    public class DebuffSynergyTier1 : IBuildSynergyTier
    {
        public const int MaxStackBonus = 2;

        //# 스트링 206 = "역병귀 독 최대 중첩 +{0}".
        public int DescriptionStringId => 206;
        public string[] DescriptionArgs
            => new[] { MaxStackBonus.ToString(CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.AddPlagueToxinMaxStack(MaxStackBonus);
        }
    }
}
