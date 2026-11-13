using System.Collections.Generic;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SJ1 일회용 패리티 (so-to-json-migration §10) — MetaConfig.asset(이전 전 값)과 JSON 조립 결과를 전 필드·순서까지 비교한다.
    //# 커밋 ② 에서 SO 에셋과 함께 이 파일을 삭제한다. 문구는 SO 문자열과 Strings_Ko 문자열의 문자 단위 비교.
    public class MetaJsonParityTests
    {
        private const string AssetPath = "Assets/_Lair/Data/MetaConfig.asset";

        private MetaConfig _asset;
        private MetaConfig _json;

        [SetUp]
        public void SetUp()
        {
            _asset = AssetDatabase.LoadAssetAtPath<MetaConfig>(AssetPath);
            Assert.IsNotNull(_asset, AssetPath);
            _json = WorldMapTestSupport.ShippedMeta();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_json);
        }

        [Test]
        public void 공식_상수_7개가_같다()
        {
            Assert.AreEqual(_asset.WinBaseSouls, _json.WinBaseSouls, "winBaseSouls");
            Assert.AreEqual(_asset.WinTimeBonusPerSec, _json.WinTimeBonusPerSec, "winTimeBonusPerSec");
            Assert.AreEqual(_asset.LoseMaxSouls, _json.LoseMaxSouls, "loseMaxSouls");
            Assert.AreEqual(_asset.WinXp, _json.WinXp, "winXp");
            Assert.AreEqual(_asset.LoseXp, _json.LoseXp, "loseXp");
            Assert.AreEqual(_asset.XpPerLevelBase, _json.XpPerLevelBase, "xpPerLevelBase");
            Assert.AreEqual(_asset.XpGrowth, _json.XpGrowth, "xpGrowth");
        }

        //# 관계 행이 없는 항목의 기본값(Hp·Wisp·1.0·빈 설명)까지 11개 필드 전부를 비교한다 — 조립 버그는 여기서 드러난다.
        [Test]
        public void 상점_31항목은_전_필드와_순서가_같다()
        {
            Assert.AreEqual(31, _asset.ShopItems.Count, "에셋 항목 수");
            Assert.AreEqual(_asset.ShopItems.Count, _json.ShopItems.Count, "항목 수");
            for (int i = 0; i < _asset.ShopItems.Count; i++)
            {
                ShopItemDef expected = _asset.ShopItems[i];
                ShopItemDef actual = _json.ShopItems[i];
                string at = $"[{i}] {expected.Id}";
                Assert.AreEqual(expected.Id, actual.Id, at + " Id");
                Assert.AreEqual(expected.DisplayName, actual.DisplayName, at + " DisplayName");
                Assert.AreEqual(expected.Description ?? string.Empty, actual.Description, at + " Description");
                Assert.AreEqual(expected.EffectKind, actual.EffectKind, at + " EffectKind");
                Assert.AreEqual(expected.StatKind, actual.StatKind, at + " StatKind");
                Assert.AreEqual(expected.Species, actual.Species, at + " Species");
                Assert.AreEqual(expected.PerLevelMul, actual.PerLevelMul, at + " PerLevelMul");
                Assert.AreEqual(expected.MaxLevel, actual.MaxLevel, at + " MaxLevel");
                Assert.AreEqual(expected.BasePrice, actual.BasePrice, at + " BasePrice");
                Assert.AreEqual(expected.PriceGrowth, actual.PriceGrowth, at + " PriceGrowth");
                Assert.AreEqual(expected.Currency, actual.Currency, at + " Currency");
            }
        }

        //# 잠금 칸의 DisplayName 은 화면이 읽지 않는 값이라(LordLevelPopup 이 리터럴 표시) 이전하지 않는다 — 이름은 보상 칸만 비교.
        [Test]
        public void 영주_트랙_9칸은_잠금_여부와_소울과_순서가_같고_보상_칸_이름이_같다()
        {
            Assert.AreEqual(9, _asset.LordRewards.Count, "에셋 칸 수");
            Assert.AreEqual(_asset.LordRewards.Count, _json.LordRewards.Count, "칸 수");
            for (int i = 0; i < _asset.LordRewards.Count; i++)
            {
                LordRewardDef expected = _asset.LordRewards[i];
                LordRewardDef actual = _json.LordRewards[i];
                string at = $"[{i}] Lv{expected.Level}";
                Assert.AreEqual(expected.Level, actual.Level, at + " Level");
                Assert.AreEqual(expected.IsLockedDummy, actual.IsLockedDummy, at + " IsLockedDummy");
                Assert.AreEqual(expected.RewardSouls, actual.RewardSouls, at + " RewardSouls");
                if (expected.IsLockedDummy == false)
                {
                    Assert.AreEqual(expected.DisplayName, actual.DisplayName, at + " DisplayName");
                }
            }
        }

        [Test]
        public void 도전과제_13개는_전_필드와_순서가_같다()
        {
            Assert.AreEqual(13, _asset.Achievements.Count, "에셋 도전과제 수");
            Assert.AreEqual(_asset.Achievements.Count, _json.Achievements.Count, "도전과제 수");
            for (int i = 0; i < _asset.Achievements.Count; i++)
            {
                AchievementDef expected = _asset.Achievements[i];
                AchievementDef actual = _json.Achievements[i];
                string at = $"[{i}] {expected.Id}";
                Assert.AreEqual(expected.Id, actual.Id, at + " Id");
                Assert.AreEqual(expected.DisplayName, actual.DisplayName, at + " DisplayName");
                Assert.AreEqual(expected.Description, actual.Description, at + " Description");
                Assert.AreEqual(expected.Condition, actual.Condition, at + " Condition");
                Assert.AreEqual(expected.Threshold, actual.Threshold, at + " Threshold");
                Assert.AreEqual(expected.RewardSouls, actual.RewardSouls, at + " RewardSouls");
            }
        }

        //# 세이브 키 — MetaProfile.ShopLevels.ItemId 31개, AchievedIds 13개가 글자 그대로 일치해야 저장 데이터가 이어진다.
        [Test]
        public void 세이브_키_집합이_일치한다()
        {
            List<string> assetItems = new List<string>();
            List<string> jsonItems = new List<string>();
            foreach (ShopItemDef def in _asset.ShopItems)
            {
                assetItems.Add(def.Id);
            }
            foreach (ShopItemDef def in _json.ShopItems)
            {
                jsonItems.Add(def.Id);
            }
            List<string> assetAchievements = new List<string>();
            List<string> jsonAchievements = new List<string>();
            foreach (AchievementDef def in _asset.Achievements)
            {
                assetAchievements.Add(def.Id);
            }
            foreach (AchievementDef def in _json.Achievements)
            {
                jsonAchievements.Add(def.Id);
            }

            CollectionAssert.AreEquivalent(assetItems, jsonItems, "itemId");
            CollectionAssert.AreEquivalent(assetAchievements, jsonAchievements, "achievementId");
        }
    }
}
