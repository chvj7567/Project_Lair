using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier1 (3장 임계) — Reaper+Hex Power ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier1 : IBuildSynergyTier
    {
        private const float PowerMul = 1.3f;

        //# 스트링 203 = "사신·저주술사 공격력 ×{0}".
        public int DescriptionStringId => 203;
        public string[] DescriptionArgs
            => new[] { PowerMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Power, PowerMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Power, PowerMul);
        }
    }
}
