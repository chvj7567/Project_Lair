using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Swarm Tier1 (3장 임계) — Phantom+Wisp MoveSpeed ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class SwarmSynergyTier1 : IBuildSynergyTier
    {
        private const float MoveMul = 1.3f;

        //# 스트링 209 = "환령·도깨비불 이동속도 ×{0}".
        public int DescriptionStringId => 209;
        public string[] DescriptionArgs
            => new[] { MoveMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Phantom, EMonsterStatKind.MoveSpeed, MoveMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,    EMonsterStatKind.MoveSpeed, MoveMul);
        }
    }
}
