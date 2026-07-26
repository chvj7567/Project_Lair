using System.Globalization;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Swarm Tier2 (5장 임계) — 모든 Spawner 주기 ×0.85 (영구).
    //# 기획서 §4.2 표·§10.3 — SpawnerHaste 카드와 동일 표면 공유.
    public class SwarmSynergyTier2 : IBuildSynergyTier
    {
        private const float PeriodMul = 0.85f;

        //# 스트링 210 = "모든 스포너 주기 ×{0}".
        public int DescriptionStringId => 210;
        public string[] DescriptionArgs
            => new[] { PeriodMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.ScaleAllSpawnerPeriods(PeriodMul);
        }
    }
}
