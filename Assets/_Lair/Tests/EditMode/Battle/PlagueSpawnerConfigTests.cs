using NUnit.Framework;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# Plague 스폰 주기 정합 회귀 — 스폰 주기 SoT 는 MonsterStat.json (so-to-json-migration §2.1).
    //# in-memory config(SpawnPeriodBalanceTests/BalanceConfigTests) 와 달리 출하 JSON 을 조립해 Plague=10 을 박제한다.
    public class PlagueSpawnerConfigTests
    {
        //# 출하 MonsterStat.json 의 Plague 주기가 10초로 정확한지 검증 (기획서 §5.1 / §5.3).
        [Test]
        public void Plague_스폰주기_10초_BalanceConfig_정합()
        {
            BalanceConfig config = Lair.Tests.EditMode.WorldMapTestSupport.ShippedBalance();

            Assert.AreEqual(10f, config.GetSpawnPeriod(EMonster.Plague), 0.001f,
                "출하 MonsterStat.json 의 Plague 스폰 주기 = 10.0s");
        }
    }
}
