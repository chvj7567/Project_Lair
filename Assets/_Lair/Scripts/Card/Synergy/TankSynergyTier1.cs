using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Tank Tier1 (3장 임계) — Wisp+Wraith HP ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class TankSynergyTier1 : IBuildSynergyTier
    {
        private const float HpMul = 1.3f;

        //# 스트링 200 = "도깨비불·망령 HP ×{0}". 수치는 자기 const 유래(단일 소스).
        public int DescriptionStringId => 200;
        public string[] DescriptionArgs
            => new[] { HpMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Hp, HpMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, HpMul);
        }
    }
}
