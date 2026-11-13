using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SJ1 일회용 패리티 (so-to-json-migration §10) — BalanceConfig.asset(이전 전 값)과 JSON 조립 결과 전 필드 비교.
    //# 커밋 ② 에서 SO 에셋과 함께 이 파일을 삭제한다.
    public class BalanceJsonParityTests
    {
        private const string AssetPath = "Assets/_Lair/Data/BalanceConfig.asset";

        [Test]
        public void BalanceConfig_에셋과_JSON_조립_결과는_전_필드가_같다()
        {
            BalanceConfig asset = AssetDatabase.LoadAssetAtPath<BalanceConfig>(AssetPath);
            Assert.IsNotNull(asset, AssetPath);
            BalanceConfig json = WorldMapTestSupport.ShippedBalance();
            try
            {
                Assert.AreEqual(asset.Hero.Hp, json.Hero.Hp, "heroHp");
                Assert.AreEqual(asset.Hero.Power, json.Hero.Power, "heroPower");
                Assert.AreEqual(asset.RunDuration, json.RunDuration, "runDuration");
                CollectionAssert.AreEqual(asset.PassiveThresholds, json.PassiveThresholds, "passiveThresholds");
                CollectionAssert.AreEqual(asset.ActiveThresholds, json.ActiveThresholds, "activeThresholds");

                //# 에셋 행이 기준 6종보다 많으면 비교에서 빠지는 행이 생기므로 행 수부터 맞춘다.
                Assert.AreEqual(MonsterGrades.BaseMonsters.Count, new SerializedObject(asset).FindProperty("_monsters").arraySize, "에셋 몬스터 행 수");
                foreach (EMonster monster in MonsterGrades.BaseMonsters)
                {
                    BalanceConfig.CharacterStat expected = asset.GetMonster(monster);
                    BalanceConfig.CharacterStat actual = json.GetMonster(monster);
                    Assert.IsNotNull(expected, $"에셋 {monster}");
                    Assert.IsNotNull(actual, $"JSON {monster}");
                    Assert.AreEqual(expected.Hp, actual.Hp, $"{monster} hp");
                    Assert.AreEqual(expected.Power, actual.Power, $"{monster} power");
                    Assert.AreEqual(expected.Range, actual.Range, $"{monster} range");
                    Assert.AreEqual(expected.Cooldown, actual.Cooldown, $"{monster} cooldown");
                    Assert.AreEqual(expected.MoveSpeed, actual.MoveSpeed, $"{monster} moveSpeed");
                    Assert.AreEqual(asset.GetSpawnPeriod(monster), json.GetSpawnPeriod(monster), $"{monster} spawnPeriod");
                }
            }
            finally
            {
                Object.DestroyImmediate(json);
            }
        }
    }
}
