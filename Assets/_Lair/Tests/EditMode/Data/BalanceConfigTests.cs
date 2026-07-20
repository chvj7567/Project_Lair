using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceConfig.GetMonster 키 조회 검증.
    //# private [SerializeField] 는 JsonUtility.FromJsonOverwrite 로 채운다 (UnityEditor 의존 회피).
    public class BalanceConfigTests
    {
        [Test]
        public void GetMonster_등록된키_스탯반환()
        {
            BalanceConfig config = ScriptableObject.CreateInstance<BalanceConfig>();
            //# EMonster.Wraith == 1 (Wisp=0, Wraith=1, ...)
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":1,\"Stat\":{\"Hp\":500,\"Power\":20}}]}",
                config);

            BalanceConfig.CharacterStat stat = config.GetMonster(EMonster.Wraith);

            Assert.IsNotNull(stat);
            Assert.AreEqual(500, stat.Hp);
            Assert.AreEqual(20, stat.Power);
        }

        [Test]
        public void GetMonster_미등록키_null반환()
        {
            BalanceConfig config = ScriptableObject.CreateInstance<BalanceConfig>();
            Assert.IsNull(config.GetMonster(EMonster.Phantom));
        }

        //# GetSpawnPeriod — 등록 행의 SpawnPeriod 값을 정확히 반환.
        [Test]
        public void GetSpawnPeriod_등록된키_주기반환()
        {
            BalanceConfig config = ScriptableObject.CreateInstance<BalanceConfig>();
            //# EMonster.Wraith == 1. SpawnPeriod = 20.
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":1,\"Stat\":{\"Hp\":500},\"SpawnPeriod\":20.0}]}",
                config);

            Assert.AreEqual(20f, config.GetSpawnPeriod(EMonster.Wraith), 0.001f);
        }

        //# GetSpawnPeriod — 미등록 키는 fallback 9f 반환 + 경고.
        [Test]
        public void GetSpawnPeriod_미등록키_fallback9f()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig config = ScriptableObject.CreateInstance<BalanceConfig>();

            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Phantom), 0.001f);
        }
    }
}
