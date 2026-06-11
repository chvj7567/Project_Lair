using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# MetaConfig.asset 실에셋 회귀 가드 — 기획서 village-meta-hub.md §2.1/§3.2/§4/§5.2/§6 수치 박제 (Gate G4).
    //# 에셋 변조/빌더 재실행 시 기획서와 어긋나면 본 스위트가 잡는다. 수치 변경은 기획서 갱신과 동행해야 한다.
    public class MetaConfigAssetRegressionTests
    {
        private const string AssetPath = "Assets/_Lair/Data/MetaConfig.asset";

        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            //# 영속 에셋 — DestroyImmediate 금지 (TearDown 없음).
            _cfg = AssetDatabase.LoadAssetAtPath<MetaConfig>(AssetPath);
            Assert.IsNotNull(_cfg, $"MetaConfig.asset 이 {AssetPath} 에 있어야 함");
        }

        [Test]
        public void 소울_XP_수치가_기획서_2_1절_4_1절과_일치한다()
        {
            Assert.AreEqual(100, _cfg.WinBaseSouls);
            Assert.AreEqual(0.5f, _cfg.WinTimeBonusPerSec, 0.0001f);
            Assert.AreEqual(60, _cfg.LoseMaxSouls);
            Assert.AreEqual(100, _cfg.WinXp);
            Assert.AreEqual(40, _cfg.LoseXp);
            Assert.AreEqual(100, _cfg.XpPerLevelBase);
            Assert.AreEqual(1.25f, _cfg.XpGrowth, 0.0001f);
        }

        [Test]
        public void 상점_7품목_구성이_기획서_3_2절_마스터표와_일치한다()
        {
            Assert.AreEqual(7, _cfg.ShopItems.Count);

            //# (Id, EffectKind, StatKind, PerLevelMul, BasePrice) — 기획서 §3.2 마스터 표 그대로.
            (string id, EShopEffectKind kind, EMonsterStatKind stat, float mul, int price)[] expected =
            {
                ("MonsterHpUp",        EShopEffectKind.MonsterStat,   EMonsterStatKind.Hp,         1.02f,  80),
                ("MonsterPowerUp",     EShopEffectKind.MonsterStat,   EMonsterStatKind.Power,      1.015f, 100),
                ("MonsterAtkSpeedUp",  EShopEffectKind.MonsterStat,   EMonsterStatKind.Cooldown,   0.99f,  100),
                ("MonsterMoveSpeedUp", EShopEffectKind.MonsterStat,   EMonsterStatKind.MoveSpeed,  1.015f, 80),
                ("MonsterRangeUp",     EShopEffectKind.MonsterStat,   EMonsterStatKind.Range,      1.015f, 120),
                ("PlagueVenomUp",      EShopEffectKind.MonsterStat,   EMonsterStatKind.SlowFactor, 0.98f,  120),
                ("SpawnerHasteUp",     EShopEffectKind.SpawnerPeriod, EMonsterStatKind.Hp,         0.985f, 150),
            };

            foreach ((string id, EShopEffectKind kind, EMonsterStatKind stat, float mul, int price) e in expected)
            {
                ShopItemDef def = _cfg.FindShopItem(e.id);
                Assert.IsNotNull(def, $"품목 {e.id} 누락");
                Assert.AreEqual(e.kind, def.EffectKind, $"{e.id} EffectKind");
                if (e.kind == EShopEffectKind.MonsterStat)
                {
                    Assert.AreEqual(e.stat, def.StatKind, $"{e.id} StatKind");
                }
                Assert.AreEqual(e.mul, def.PerLevelMul, 0.0001f, $"{e.id} PerLevelMul");
                Assert.AreEqual(e.price, def.BasePrice, $"{e.id} BasePrice");
                Assert.AreEqual(5, def.MaxLevel, $"{e.id} MaxLevel — 전 품목 5 (기획서 §3.2)");
                Assert.AreEqual(1.6f, def.PriceGrowth, 0.0001f, $"{e.id} PriceGrowth — 전 품목 1.6");
                Assert.IsFalse(string.IsNullOrEmpty(def.DisplayName), $"{e.id} DisplayName 비어있음");
                Assert.IsFalse(string.IsNullOrEmpty(def.Description), $"{e.id} Description 비어있음 (§11.3 [추가 3])");
            }
        }

        [Test]
        public void 상점_전품목_만렙_누적비용이_기획서_3_5절_검산_11849와_일치한다()
        {
            int total = 0;
            foreach (ShopItemDef def in _cfg.ShopItems)
            {
                for (int level = 0; level < def.MaxLevel; ++level)
                {
                    total += ShopService.PriceOf(def, level);
                }
            }
            Assert.AreEqual(11849, total);
        }

        [Test]
        public void 영주_트랙_9개_구성과_보상합_600이_기획서_4_3절과_일치한다()
        {
            Assert.AreEqual(9, _cfg.LordRewards.Count);

            //# (Level, IsLockedDummy, RewardSouls) — 잠금 더미는 짝수 레벨 4·6·8·10 (기획서 §4.3).
            (int level, bool dummy, int souls)[] expected =
            {
                (2, false, 50), (3, false, 80), (4, true, 0), (5, false, 120), (6, true, 0),
                (7, false, 150), (8, true, 0), (9, false, 200), (10, true, 0),
            };

            int sum = 0;
            for (int i = 0; i < expected.Length; ++i)
            {
                LordRewardDef def = _cfg.LordRewards[i];
                Assert.AreEqual(expected[i].level, def.Level, $"트랙 {i} Level");
                Assert.AreEqual(expected[i].dummy, def.IsLockedDummy, $"Lv{def.Level} IsLockedDummy");
                Assert.AreEqual(expected[i].souls, def.RewardSouls, $"Lv{def.Level} RewardSouls");
                if (def.IsLockedDummy == false)
                {
                    sum += def.RewardSouls;
                }
            }
            Assert.AreEqual(600, sum);
        }

        [Test]
        public void 도전과제_13개_구성과_보상합_880이_기획서_5_2절과_일치한다()
        {
            Assert.AreEqual(13, _cfg.Achievements.Count);

            //# (Id, Condition, Threshold, RewardSouls) — 기획서 §5.2 마스터 표 그대로.
            (string id, EAchievementCondition cond, float threshold, int souls)[] expected =
            {
                ("FirstRun",     EAchievementCondition.TotalRuns,          1,   20),
                ("FirstWin",     EAchievementCondition.FirstWin,           1,   30),
                ("Win120",       EAchievementCondition.WinUnderSeconds,    120, 40),
                ("Win90",        EAchievementCondition.WinUnderSeconds,    90,  60),
                ("Win60",        EAchievementCondition.WinUnderSeconds,    60,  100),
                ("Wins5",        EAchievementCondition.TotalWins,          5,   40),
                ("Wins10",       EAchievementCondition.TotalWins,          10,  60),
                ("Wins25",       EAchievementCondition.TotalWins,          25,  100),
                ("Wins50",       EAchievementCondition.TotalWins,          50,  150),
                ("Runs10",       EAchievementCondition.TotalRuns,          10,  50),
                ("Runs30",       EAchievementCondition.TotalRuns,          30,  100),
                ("SynergyTier2", EAchievementCondition.SynergyTierReached, 2,   50),
                ("SynergyTier3", EAchievementCondition.SynergyTierReached, 3,   80),
            };

            Dictionary<string, AchievementDef> byId = new Dictionary<string, AchievementDef>();
            int sum = 0;
            foreach (AchievementDef def in _cfg.Achievements)
            {
                byId[def.Id] = def;
                sum += def.RewardSouls;
            }

            foreach ((string id, EAchievementCondition cond, float threshold, int souls) e in expected)
            {
                Assert.IsTrue(byId.ContainsKey(e.id), $"도전과제 {e.id} 누락");
                AchievementDef def = byId[e.id];
                Assert.AreEqual(e.cond, def.Condition, $"{e.id} Condition");
                Assert.AreEqual(e.threshold, def.Threshold, 0.0001f, $"{e.id} Threshold");
                Assert.AreEqual(e.souls, def.RewardSouls, $"{e.id} RewardSouls");
                Assert.IsFalse(string.IsNullOrEmpty(def.DisplayName), $"{e.id} DisplayName 비어있음");
            }
            Assert.AreEqual(880, sum);
        }

        [Test]
        public void 잠금슬롯_수가_기획서_6절과_일치한다()
        {
            Assert.AreEqual(3, _cfg.HeroLockedSlots);
            Assert.AreEqual(2, _cfg.CodexLockedSlots);
        }
    }
}
