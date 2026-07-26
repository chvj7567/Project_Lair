using System.Globalization;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Swarm Tier3 (7장 임계) — 모든 Spawner 동시 출력 +1 (영구).
    //# 기획서 §4.2 표·§10.3.
    public class SwarmSynergyTier3 : IBuildSynergyTier
    {
        private const int OutputDelta = 1;

        //# 스트링 211 = "모든 스포너 동시 출력 +{0}". 정수 delta 그대로.
        public int DescriptionStringId => 211;
        public string[] DescriptionArgs
            => new[] { OutputDelta.ToString(CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.IncrementAllSpawnerOutputs(OutputDelta);
        }
    }
}
