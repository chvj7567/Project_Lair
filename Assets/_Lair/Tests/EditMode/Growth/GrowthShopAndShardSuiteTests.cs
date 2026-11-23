using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# 상점 결제 재화 · 해금 · 출하 상점 무결성 + 유물 잔해 변환 · 교환 본격 스위트 (monster-growth-currencies §2.4·§3.5·§6.3-7·§8.3).
    public class GrowthShopAndShardSuiteTests : RelicStaticStateTestBase
    {
        private static readonly EMonster[] Species = System.Linq.Enumerable.ToArray(MonsterGrades.BaseMonsters);

        private GrowthDatabase _prevGrowth;

        [SetUp]
        public void 육성_정적상태를_저장한다()
        {
            _prevGrowth = GrowthDatabase.Current;
            ResetGrowthShopValidationCache();
        }

        [TearDown]
        public void 육성_정적상태를_원복한다()
        {
            GrowthDatabase.Current = _prevGrowth;
            ResetGrowthShopValidationCache();
        }

        //# ValidateGrowthShop 은 같은 config 를 한 번만 검증한다 - 실행 순서 의존을 끊는다.
        private static void ResetGrowthShopValidationCache()
        {
            FieldInfo f = typeof(ChapterDatabaseLoader).GetField("_growthShopValidatedFor", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(f, "ChapterDatabaseLoader._growthShopValidatedFor");
            f.SetValue(null, null);
        }

        private static MetaConfig ShippedConfig()
        {
            return WorldMapTestSupport.ShippedMeta();
        }

        private static int[] Prices(ShopItemDef def)
        {
            int[] prices = new int[def.MaxLevel];
            for (int lv = 0; lv < def.MaxLevel; ++lv)
            {
                prices[lv] = ShopService.PriceOf(def, lv);
            }
            return prices;
        }

        //# ===== 출하 상점 항목 (§6.2 표 · §3.5 가격) =====

        [Test]
        public void 출하_상점의_육성_18항목은_종족마다_축별_재화와_가격_곡선을_가진다()
        {
            MetaConfig cfg = ShippedConfig();
            foreach (EMonster m in Species)
            {
                ShopItemDef awaken = cfg.FindShopItem("Awaken_" + m);
                ShopItemDef nest = cfg.FindShopItem("Nest_" + m);
                ShopItemDef trait = cfg.FindShopItem("Trait_" + m);
                Assert.IsNotNull(awaken, "Awaken_" + m);
                Assert.IsNotNull(nest, "Nest_" + m);
                Assert.IsNotNull(trait, "Trait_" + m);

                Assert.AreEqual(ECurrency.AwakenStone, awaken.Currency);
                Assert.AreEqual(ECurrency.MoonSeal, nest.Currency);
                Assert.AreEqual(ECurrency.AbyssCrystal, trait.Currency);
                Assert.AreEqual(m, awaken.Species);
                Assert.AreEqual(m, nest.Species);
                Assert.AreEqual(m, trait.Species);
                CollectionAssert.AreEqual(new[] { 10, 15, 22, 33, 50 }, Prices(awaken), "각성 " + m);
                CollectionAssert.AreEqual(new[] { 20, 30, 45 }, Prices(nest), "둥지 " + m);
                CollectionAssert.AreEqual(new[] { 3, 6, 12 }, Prices(trait), "특성 " + m);
                Assert.AreEqual(ECurrency.Soul, cfg.FindShopItem("Enhance_" + m).Currency, "체급은 소울 유지");
            }
        }

        [Test]
        public void 출하_상점_소울_항목은_결제_재화가_전부_소울이다()
        {
            foreach (ShopItemDef item in ShippedConfig().ShopItems)
            {
                bool growthAxis = item.EffectKind == EShopEffectKind.MonsterAwaken || item.EffectKind == EShopEffectKind.NestMastery || item.EffectKind == EShopEffectKind.MonsterTrait;
                if (growthAxis == false)
                {
                    Assert.AreEqual(ECurrency.Soul, item.Currency, item.Id);
                }
            }
        }

        [Test]
        public void 출하_데이터로_전투_시작_상점_검증을_돌려도_육성_기능은_켜진_채다()
        {
            GrowthDatabase.Current = GrowthTestKit.Shipped();

            ChapterDatabaseLoader.ValidateGrowthShop(ShippedConfig());

            Assert.IsNotNull(GrowthDatabase.Current, "§6.3-7 위반 0 → 비활성 안 됨");
        }

        [Test]
        public void 상점_검증_위반이면_오류_로그_후_육성_기능을_끈다()
        {
            GrowthDatabase.Current = GrowthTestKit.Shipped();
            MetaConfig cfg = new MetaConfig();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "Awaken_Wisp", EffectKind = EShopEffectKind.MonsterAwaken, Species = EMonster.Wisp, Currency = ECurrency.Soul, MaxLevel = 5 },
            };
            LogAssert.Expect(LogType.Error, new Regex("육성 상점 항목 오류"));

            ChapterDatabaseLoader.ValidateGrowthShop(cfg);

            Assert.IsNull(GrowthDatabase.Current, "§6.3-8 로드 실패 = 육성 비활성");
        }

        [Test]
        public void 상점_검증은_소울_결제_만렙_불일치를_각각_오류로_모으고_기존_소울_항목은_건너뛴다()
        {
            MetaConfig cfg = new MetaConfig();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "Awaken_Wisp", EffectKind = EShopEffectKind.MonsterAwaken, Species = EMonster.Wisp, Currency = ECurrency.Soul, MaxLevel = 5 },
                new ShopItemDef { Id = "Nest_Hex", EffectKind = EShopEffectKind.NestMastery, Species = EMonster.Hex, Currency = ECurrency.MoonSeal, MaxLevel = 4 },
                new ShopItemDef { Id = "Trait_Plague", EffectKind = EShopEffectKind.MonsterTrait, Species = EMonster.Plague, Currency = ECurrency.AbyssCrystal, MaxLevel = 2 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, Currency = ECurrency.Soul, MaxLevel = 5 },
                null,
            };

            List<string> errors = GrowthTestKit.Shipped().ValidateShopItems(cfg);

            Assert.AreEqual(3, errors.Count, string.Join("\n", errors));
            Assert.IsTrue(errors.Exists(e => e.Contains("Awaken_Wisp") && e.Contains("Soul")));
            Assert.IsTrue(errors.Exists(e => e.Contains("Nest_Hex") && e.Contains("MaxLevel")));
            Assert.IsTrue(errors.Exists(e => e.Contains("Trait_Plague") && e.Contains("MaxLevel")));
        }

        //# ===== 결제 재화 분기 =====

        private static MetaConfig MixedConfig()
        {
            MetaConfig cfg = new MetaConfig();
            cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, Currency = ECurrency.Soul, MaxLevel = 5, BasePrice = 150, PriceGrowth = 1.6f, PerLevelMul = 1.18f },
                new ShopItemDef { Id = "Nest_Wisp", EffectKind = EShopEffectKind.NestMastery, Species = EMonster.Wisp, Currency = ECurrency.MoonSeal, MaxLevel = 3, BasePrice = 20, PriceGrowth = 1.5f, PerLevelMul = 1f },
                new ShopItemDef { Id = "Trait_Wisp", EffectKind = EShopEffectKind.MonsterTrait, Species = EMonster.Wisp, Currency = ECurrency.AbyssCrystal, MaxLevel = 3, BasePrice = 3, PriceGrowth = 2f, PerLevelMul = 1f },
            };
            //# 체급은 티어 단계표(390/998/983)로 구매한다 (dungeon-formation-free §3).
            cfg.EnhanceTiers = EnhanceTierTable.Build(new[]
            {
                new EnhanceTierRow { tier = 1, levelEquivalent = 2, soulCost = 390 },
                new EnhanceTierRow { tier = 2, levelEquivalent = 4, soulCost = 998 },
                new EnhanceTierRow { tier = 3, levelEquivalent = 5, soulCost = 983 },
            });
            return cfg;
        }

        [Test]
        public void 소울_항목은_소울만_특수_항목은_그_재화만_차감한다()
        {
            MetaConfig cfg = MixedConfig();
            MetaProfile p = new MetaProfile { Souls = 500 };
            p.AddCurrency(ECurrency.MoonSeal, 25);
            p.AddCurrency(ECurrency.AbyssCrystal, 3);
            ShopService shop = new ShopService(p, cfg, c => true);

            Assert.IsTrue(shop.Buy("Enhance_Wisp"));
            Assert.IsTrue(shop.Buy("Nest_Wisp"));
            Assert.IsTrue(shop.Buy("Trait_Wisp"));

            Assert.AreEqual(110, p.Souls);
            Assert.AreEqual(5, p.GetCurrency(ECurrency.MoonSeal));
            Assert.AreEqual(0, p.GetCurrency(ECurrency.AbyssCrystal));
        }

        [Test]
        public void 다음_단계_가격이_모자라면_구매를_거부하고_잔액과_레벨이_그대로다()
        {
            MetaConfig cfg = MixedConfig();
            MetaProfile p = new MetaProfile { Souls = 99999 };
            p.AddCurrency(ECurrency.MoonSeal, 94);
            ShopService shop = new ShopService(p, cfg, c => true);

            bool lv1 = shop.Buy("Nest_Wisp");
            bool lv2 = shop.Buy("Nest_Wisp");
            bool lv3 = shop.Buy("Nest_Wisp");

            Assert.IsTrue(lv1 && lv2, "20 + 30 = 50 <= 94");
            Assert.IsFalse(lv3, "남은 44 < 45");
            Assert.AreEqual(2, p.GetShopLevel("Nest_Wisp"));
            Assert.AreEqual(99999, p.Souls, "특수 재화 부족을 소울로 대신 내지 않는다");
        }

        [Test]
        public void 특성_만렙_3단계면_재화가_넉넉해도_구매가_막힌다()
        {
            MetaConfig cfg = MixedConfig();
            MetaProfile p = new MetaProfile();
            p.AddCurrency(ECurrency.AbyssCrystal, 999);
            ShopService shop = new ShopService(p, cfg, c => true);

            for (int i = 0; i < 4; ++i)
            {
                shop.Buy("Trait_Wisp");
            }

            Assert.AreEqual(3, p.GetShopLevel("Trait_Wisp"));
            Assert.AreEqual(999 - 21, p.GetCurrency(ECurrency.AbyssCrystal), "3 + 6 + 12");
            Assert.IsFalse(shop.CanBuy("Trait_Wisp"));
        }

        [Test]
        public void 육성_데이터가_없으면_기본_해금_판정이_특수_재화_항목을_잠그고_소울_항목은_연다()
        {
            GrowthDatabase.Current = null;
            MetaConfig cfg = MixedConfig();
            MetaProfile p = new MetaProfile { Souls = 999, ClearedStage = 25 };
            p.AddCurrency(ECurrency.MoonSeal, 999);
            ShopService shop = new ShopService(p, cfg);

            Assert.IsTrue(shop.IsLocked("Nest_Wisp"), "§6.3-8 로드 실패 - 잠김");
            Assert.IsFalse(shop.CanBuy("Nest_Wisp"));
            Assert.IsFalse(shop.IsLocked("Enhance_Wisp"));
            Assert.IsTrue(shop.CanBuy("Enhance_Wisp"));
            Assert.AreEqual(999, p.GetCurrency(ECurrency.MoonSeal), "잠겨도 보유 재화는 보존 (E-3)");
        }

        //# ===== 재화 해금 (§1) =====

        [TestCase(ECurrency.Soul, 0, true)]
        [TestCase(ECurrency.AwakenStone, 0, false)]
        [TestCase(ECurrency.AwakenStone, 1, true)]
        [TestCase(ECurrency.MoonSeal, 4, false)]
        [TestCase(ECurrency.MoonSeal, 5, true)]
        [TestCase(ECurrency.AbyssCrystal, 24, false)]
        [TestCase(ECurrency.AbyssCrystal, 25, true)]
        public void 재화_해금은_그_재화를_주는_콘텐츠의_해금_스테이지를_따른다(ECurrency currency, int cleared, bool expected)
        {
            MetaProfile p = new MetaProfile { ClearedStage = cleared };

            Assert.AreEqual(expected, CurrencyRules.IsUnlocked(currency, p, 5, 25));
        }

        [Test]
        public void 유물_잔해는_처음_얻은_순간부터_해금되고_다_써도_해금이_유지된다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 25 };
            bool before = CurrencyRules.IsUnlocked(ECurrency.RelicShard, p, 5, 25);
            p.AddCurrency(ECurrency.RelicShard, 1);
            p.TrySpendCurrency(ECurrency.RelicShard, 1);

            Assert.IsFalse(before);
            Assert.IsTrue(CurrencyRules.IsUnlocked(ECurrency.RelicShard, p, 5, 25));
        }

        //# ===== 잔해 교환 (출하 비율 3/5/8) =====

        [Test]
        public void 출하_교환_비율로_잔해_16개가_각성석_인장_결정_각_1개로_바뀐다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile();
            p.AddCurrency(ECurrency.RelicShard, 16);

            Assert.AreEqual(EExchangeResult.Ok, CurrencyExchange.Exchange(p, growth, ECurrency.AwakenStone, c => true));
            Assert.AreEqual(13, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(EExchangeResult.Ok, CurrencyExchange.Exchange(p, growth, ECurrency.MoonSeal, c => true));
            Assert.AreEqual(8, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(EExchangeResult.Ok, CurrencyExchange.Exchange(p, growth, ECurrency.AbyssCrystal, c => true));

            Assert.AreEqual(0, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(1, p.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(1, p.GetCurrency(ECurrency.MoonSeal));
            Assert.AreEqual(1, p.GetCurrency(ECurrency.AbyssCrystal));
            Assert.AreEqual(16, p.GetCurrencyTotalEarned(ECurrency.RelicShard), "교환으로 써도 누적 획득은 줄지 않는다");
        }

        [TestCase(ECurrency.AwakenStone, 2)]
        [TestCase(ECurrency.MoonSeal, 4)]
        [TestCase(ECurrency.AbyssCrystal, 7)]
        public void 비율보다_하나_모자라면_교환을_거부하고_아무것도_바꾸지_않는다(ECurrency to, int shards)
        {
            MetaProfile p = new MetaProfile();
            p.AddCurrency(ECurrency.RelicShard, shards);

            Assert.AreEqual(EExchangeResult.Insufficient, CurrencyExchange.Exchange(p, GrowthTestKit.Shipped(), to, c => true));
            Assert.AreEqual(shards, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(0, p.GetCurrency(to));
        }

        [Test]
        public void 미해금_목표는_잔해가_없어도_잠김을_먼저_돌려준다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(EExchangeResult.Locked, CurrencyExchange.Check(p, GrowthTestKit.Shipped(), ECurrency.AbyssCrystal, c => c != ECurrency.AbyssCrystal));
        }

        [Test]
        public void 소울_잔해_대상_교환이나_육성_미로드는_교환_불가다()
        {
            MetaProfile p = new MetaProfile();
            p.AddCurrency(ECurrency.RelicShard, 99);
            GrowthDatabase growth = GrowthTestKit.Shipped();

            Assert.AreEqual(EExchangeResult.Unavailable, CurrencyExchange.Exchange(p, growth, ECurrency.Soul, c => true), "소울 환전 금지");
            Assert.AreEqual(EExchangeResult.Unavailable, CurrencyExchange.Exchange(p, growth, ECurrency.RelicShard, c => true));
            Assert.AreEqual(EExchangeResult.Unavailable, CurrencyExchange.Exchange(p, null, ECurrency.AwakenStone, c => true));
            Assert.AreEqual(99, p.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(0, p.Souls, "소울로 바뀌지 않는다");
        }

        //# ===== 잔해 변환 (§2.4 C) =====

        private static void FillRarity(RelicDatabase db, MetaProfile p, string heroId, ERelicRarity? only)
        {
            foreach (Relic_DropRow drop in db.DropsOf(heroId))
            {
                RelicData data = db.Get(drop.relicId);
                if (data == null || (only.HasValue && data.Rarity != only.Value))
                    continue;
                while (p.GetRelicStack(drop.relicId) < data.MaxStack)
                {
                    p.AddRelic(drop.relicId);
                }
            }
        }

        [Test]
        public void 같은_등급에_미완성_유물이_남아_있으면_50개_시드에서_잔해_규칙_전과_결과가_같다()
        {
            RelicDatabase db = Db();
            string maxedCommon = null;
            int commons = 0;
            foreach (Relic_DropRow drop in db.DropsOf("Knight"))
            {
                RelicData data = db.Get(drop.relicId);
                if (data != null && data.Rarity == ERelicRarity.Common)
                {
                    ++commons;
                    maxedCommon = maxedCommon ?? drop.relicId;
                }
            }
            Assume.That(commons >= 2, "기사 테이블에 Common 2종 이상 필요");
            GrowthDatabase growth = GrowthTestKit.Shipped();

            for (int seed = 0; seed < 50; ++seed)
            {
                MetaProfile legacy = new MetaProfile { RelicMissStreak = seed % 4 };
                MetaProfile shards = new MetaProfile { RelicMissStreak = seed % 4 };
                SetStack(legacy, maxedCommon, db.Get(maxedCommon).MaxStack);
                SetStack(shards, maxedCommon, db.Get(maxedCommon).MaxStack);
                System.Random ra = new System.Random(seed);
                System.Random rb = new System.Random(seed);

                RelicDropResult a = RelicDropRoller.Roll(db, legacy, "Knight", 1 + seed % 5, 1 + seed % 5, seed % 3, true, () => (float)ra.NextDouble());
                RelicDropResult b = RelicDropRoller.Roll(db, shards, "Knight", 1 + seed % 5, 1 + seed % 5, seed % 3, true, () => (float)rb.NextDouble(), growth);

                Assert.AreEqual(a.Outcome, b.Outcome, "seed " + seed);
                Assert.AreEqual(a.RelicId, b.RelicId, "seed " + seed);
                Assert.AreEqual(legacy.RelicMissStreak, shards.RelicMissStreak, "seed " + seed);
                Assert.AreEqual(0, shards.GetCurrency(ECurrency.RelicShard), "seed " + seed);
            }
        }

        [Test]
        public void 영웅_테이블을_전부_다_모으면_잔해_규칙에서는_굴림이_잔해가_되고_기존_규칙은_굴리지_않는다()
        {
            RelicDatabase db = Db();
            MetaProfile legacy = new MetaProfile();
            MetaProfile shards = new MetaProfile { RelicMissStreak = 3 };
            FillRarity(db, legacy, "Knight", null);
            FillRarity(db, shards, "Knight", null);

            RelicDropResult a = RelicDropRoller.Roll(db, legacy, "Knight", 1, 1, 0, true, new ScriptedRng(0f, 0f).Next);
            RelicDropResult b = RelicDropRoller.Roll(db, shards, "Knight", 1, 1, 0, true, new ScriptedRng(0f, 0f).Next, GrowthTestKit.Shipped());

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, a.Outcome, "잔해 데이터 없음 = 기존 E-4");
            Assert.IsTrue(b.IsShard, "E-4 폐지 - 풀셋도 굴리고 성공하면 잔해");
            Assert.AreEqual(ERelicRarity.Common, b.ShardRarity, "등급 난수 0 = 첫 등급");
            Assert.AreEqual(1, b.ShardAmount);
            Assert.AreEqual(1, shards.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(0, shards.RelicMissStreak, "잔해도 획득 - pity 0");
        }

        [TestCase(ERelicRarity.Common, 0f, 1)]
        [TestCase(ERelicRarity.Rare, 0.85f, 3)]
        [TestCase(ERelicRarity.Legendary, 0.9999f, 8)]
        public void 다_찬_등급이_뽑히면_그_등급의_잔해량을_준다(ERelicRarity rarity, float rarityRoll, int expected)
        {
            RelicDatabase db = Db();
            MetaProfile p = new MetaProfile();
            FillRarity(db, p, "Knight", rarity);

            RelicDropResult r = RelicDropRoller.Roll(db, p, "Knight", 1, 1, 0, true, new ScriptedRng(0f, rarityRoll, 0f).Next, GrowthTestKit.Shipped());

            Assert.IsTrue(r.IsShard, rarity.ToString());
            Assert.AreEqual(rarity, r.ShardRarity);
            Assert.AreEqual(expected, r.ShardAmount);
            Assert.AreEqual(expected, p.GetCurrency(ECurrency.RelicShard));
        }

        [Test]
        public void 데일리_확정_굴림은_풀셋이면_항상_잔해를_주고_연패_카운터를_건드리지_않는다()
        {
            RelicDatabase db = Db();
            MetaProfile p = new MetaProfile { RelicMissStreak = 4 };
            FillRarity(db, p, "Knight", null);

            RelicDropResult r = RelicDropRoller.RollGuaranteed(db, p, "Knight", 1, new ScriptedRng(0.5f).Next, GrowthTestKit.Shipped());

            Assert.IsTrue(r.IsShard);
            Assert.AreEqual(4, p.RelicMissStreak);
            Assert.AreEqual(r.ShardAmount, p.GetCurrency(ECurrency.RelicShard));
        }

        [Test]
        public void 패배_굴림이_빗나가면_잔해도_없고_연패_카운터도_그대로다()
        {
            RelicDatabase db = Db();
            MetaProfile p = new MetaProfile { RelicMissStreak = 2 };
            FillRarity(db, p, "Knight", null);

            RelicDropResult r = RelicDropRoller.Roll(db, p, "Knight", 1, 1, 0, false, new ScriptedRng(0.9999f).Next, GrowthTestKit.Shipped());

            Assert.AreEqual(ERelicDropOutcome.Missed, r.Outcome);
            Assert.AreEqual(2, p.RelicMissStreak);
            Assert.AreEqual(0, p.GetCurrency(ECurrency.RelicShard));
        }

        [Test]
        public void 승리_굴림이_빗나가면_연패가_1_오르고_잔해는_없다()
        {
            RelicDatabase db = Db();
            MetaProfile p = new MetaProfile { RelicMissStreak = 1 };
            FillRarity(db, p, "Knight", null);

            RelicDropResult r = RelicDropRoller.Roll(db, p, "Knight", 1, 1, 0, true, new ScriptedRng(0.9999f).Next, GrowthTestKit.Shipped());

            Assert.AreEqual(ERelicDropOutcome.Missed, r.Outcome);
            Assert.AreEqual(2, p.RelicMissStreak);
            Assert.AreEqual(0, p.GetCurrency(ECurrency.RelicShard));
        }
    }
}
