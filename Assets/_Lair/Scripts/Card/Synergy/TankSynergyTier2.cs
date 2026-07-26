using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Tank Tier2 (5장 임계) — Wisp+Wraith Power ×1.2 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class TankSynergyTier2 : IBuildSynergyTier
    {
        private const float PowerMul = 1.2f;

        //# 스트링 201 = "도깨비불·망령 공격력 ×{0}".
        public int DescriptionStringId => 201;
        public string[] DescriptionArgs
            => new[] { PowerMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Power, PowerMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Power, PowerMul);
        }
    }
}
