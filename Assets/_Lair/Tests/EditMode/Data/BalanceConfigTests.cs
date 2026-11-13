using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceConfig.GetMonster / GetSpawnPeriod 키 조회 검증.
    //# 일부 행만 가진 config 는 검증 없는 생성자에 row 리터럴을 넘겨 만든다.
    public class BalanceConfigTests
    {
        private static BalanceConfig Config(params MonsterStatRow[] rows)
        {
            return new BalanceConfig(new BalanceConfigRow(), rows);
        }

        [Test]
        public void GetMonster_등록된키_스탯반환()
        {
            BalanceConfig config = Config(new MonsterStatRow { monster = "Wraith", hp = 500, power = 20 });

            BalanceConfig.CharacterStat stat = config.GetMonster(EMonster.Wraith);

            Assert.IsNotNull(stat);
            Assert.AreEqual(500, stat.Hp);
            Assert.AreEqual(20, stat.Power);
        }

        [Test]
        public void GetMonster_미등록키_null반환()
        {
            BalanceConfig config = Config();
            Assert.IsNull(config.GetMonster(EMonster.Phantom));
        }

        //# GetSpawnPeriod — 등록 행의 spawnPeriod 값을 정확히 반환.
        [Test]
        public void GetSpawnPeriod_등록된키_주기반환()
        {
            BalanceConfig config = Config(new MonsterStatRow { monster = "Wraith", hp = 500, spawnPeriod = 20f });

            Assert.AreEqual(20f, config.GetSpawnPeriod(EMonster.Wraith), 0.001f);
        }

        //# GetSpawnPeriod — 미등록 키는 fallback 9f 반환 + 경고.
        [Test]
        public void GetSpawnPeriod_미등록키_fallback9f()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig config = Config();

            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Phantom), 0.001f);
        }
    }
}
