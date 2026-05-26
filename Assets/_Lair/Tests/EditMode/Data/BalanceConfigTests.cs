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
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            //# EMonster.Wraith == 1 (Wisp=0, Wraith=1, ...)
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":1,\"Stat\":{\"Hp\":500,\"Power\":20}}]}",
                config);

            var stat = config.GetMonster(EMonster.Wraith);

            Assert.IsNotNull(stat);
            Assert.AreEqual(500, stat.Hp);
            Assert.AreEqual(20, stat.Power);
        }

        [Test]
        public void GetMonster_미등록키_null반환()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            Assert.IsNull(config.GetMonster(EMonster.Phantom));
        }
    }
}
