using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 몬스터 육성 재화 슬라이스 A 자체 테스트 (정상 + 엣지) — 본격 스위트는 test-engineer (monster-growth-currencies §11.7).
    public class CurrencySettlementTests
    {
        private static GrowthDatabase Growth()
        {
            return GrowthDatabase.Build(new GrowthDataSet
            {
                Currencies = new[]
                {
                    new CurrencyRow { currencyId = "Soul", nameKey = 1465, colorHex = "#5ef0b4" },
                    new CurrencyRow { currencyId = "AwakenStone", nameKey = 1466, colorHex = "#ff9f43" },
                    new CurrencyRow { currencyId = "MoonSeal", nameKey = 1467, colorHex = "#d8e4ff" },
                    new CurrencyRow { currencyId = "AbyssCrystal", nameKey = 1468, colorHex = "#2ec4c4" },
                    new CurrencyRow { currencyId = "RelicShard", nameKey = 1469, colorHex = "#c8a27a" },
                },
                Exchanges = new[] { new Currency_ExchangeRow { fromCurrency = "RelicShard", toCurrency = "AwakenStone", fromAmount = 3, toAmount = 1 } },
                DifficultyAwakens = new[]
                {
                    new Difficulty_AwakenRow { difficulty = 1, firstClear = 10, repeatWin = 2 },
                    new Difficulty_AwakenRow { difficulty = 2, firstClear = 25, repeatWin = 4 },
                },
                TowerCrystals = new[]
                {
                    new Difficulty_TowerCrystalRow { difficulty = 0, crystalPerFloor = 1 },
                    new Difficulty_TowerCrystalRow { difficulty = 1, crystalPerFloor = 2 },
                    new Difficulty_TowerCrystalRow { difficulty = 2, crystalPerFloor = 3 },
                },
            }, 2);
        }

        [Test]
        public void Normal_첫_클리어는_10_같은_난이도_재승리는_2를_준다()
        {
            GrowthDatabase growth = Growth();
            MetaProfile p = new MetaProfile { ClearedStage = 1 };
            p.RecordDifficultyClear(1, 1);

            ChapterAwakenGain first = CurrencySettlement.SettleChapterWin(p, growth, 1, 0);
            int paid = p.GetAwakenPaid(1);
            ChapterAwakenGain repeat = CurrencySettlement.SettleChapterWin(p, growth, 1, paid);

            Assert.AreEqual(10, first.ThisRun);
            Assert.AreEqual(0, first.Retroactive);
            Assert.AreEqual(2, repeat.ThisRun);
            Assert.AreEqual(12, p.GetCurrency(ECurrency.AwakenStone));
        }

        [Test]
        public void Normal_기록없이_Hard_를_깬_행은_소급정산이_10_더하기_25를_한_번만_준다()
        {
            GrowthDatabase growth = Growth();
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 3, MaxDifficulty = 2 });

            int once = CurrencySettlement.SettleAwakenFirstClears(p, growth);
            int twice = CurrencySettlement.SettleAwakenFirstClears(p, growth);

            Assert.AreEqual(35, once);
            Assert.AreEqual(0, twice);
            Assert.AreEqual(35, p.GetCurrencyTotalEarned(ECurrency.AwakenStone));
        }

        [Test]
        public void 이번_판과_함께_정산된_다른_스테이지_기록은_소급분으로_나뉜다()
        {
            GrowthDatabase growth = Growth();
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 2 });
            p.RecordDifficultyClear(1, 1);

            ChapterAwakenGain gain = CurrencySettlement.SettleChapterWin(p, growth, 1, 0);

            Assert.AreEqual(10, gain.ThisRun);
            Assert.AreEqual(35, gain.Retroactive);
            Assert.AreEqual(45, p.GetCurrency(ECurrency.AwakenStone));
        }

        [TestCase(1, false, 3)]
        [TestCase(2, true, 9)]
        [TestCase(4, true, 11)]
        [TestCase(7, true, 13)]
        public void 달빛_인장은_판_종료분과_첫_승리분을_각각_반올림한다(int streak, bool firstWin, int expected)
        {
            DailyConfigRow cfg = new DailyConfigRow { sealPerFinish = 3, sealFirstWin = 5, streakBonusPerDay = 0.1f, streakBonusMaxDays = 7 };

            Assert.AreEqual(expected, CurrencySettlement.DailySeals(firstWin, streak, cfg));
        }

        [Test]
        public void 특수_재화_항목은_재화_잔액으로_결제하고_잠기면_구매를_거부한다()
        {
            MetaConfig cfg = new MetaConfig();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "Awaken_Wisp", EffectKind = EShopEffectKind.MonsterAwaken, Currency = ECurrency.AwakenStone, MaxLevel = 5, BasePrice = 10, PriceGrowth = 1.5f },
            };
            MetaProfile p = new MetaProfile { Souls = 999 };
            p.AddCurrency(ECurrency.AwakenStone, 12);
            bool unlocked = false;
            ShopService shop = new ShopService(p, cfg, c => c == ECurrency.Soul || unlocked);

            bool lockedBuy = shop.Buy("Awaken_Wisp");
            unlocked = true;
            bool buy = shop.Buy("Awaken_Wisp");

            Assert.IsFalse(lockedBuy);
            Assert.IsTrue(buy);
            Assert.AreEqual(2, p.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(999, p.Souls, "소울은 차감하지 않는다");
            Assert.AreEqual(1, p.GetShopLevel("Awaken_Wisp"));
        }

        [Test]
        public void 마이그레이션은_재화_행을_정리하고_멱등이다()
        {
            MetaProfile p = new MetaProfile { Version = 11 };
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "Soul", Amount = 5, TotalEarned = 5 });
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "MoonSeal", Amount = -3, TotalEarned = 0 });
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "AwakenStone", Amount = 7, TotalEarned = 2 });
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "AwakenStone", Amount = 4, TotalEarned = 9 });

            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);
            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);

            Assert.AreEqual(14, p.Version);
            Assert.AreEqual(1, p.Currencies.Count);
            Assert.AreEqual(7, p.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(9, p.GetCurrencyTotalEarned(ECurrency.AwakenStone));
        }
    }
}
