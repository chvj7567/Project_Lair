using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 재화 지급 본격 스위트 (monster-growth-currencies §2·§7.2·§9) — 각성석 첫 클리어/반복/소급, 달빛 인장 반올림 표, 심연 결정 구간·이정표·멱등·시즌.
    //# 패배·뒤로가기 0 은 BattleController.EndBattle 이 승리만 정산 함수로 넘기는 분기라 이 순수 함수 스위트 범위 밖이다.
    public class GrowthSettlementSuiteTests : ChapterStaticStateTestBase
    {
        //# ===== 각성석 =====

        [Test]
        public void Normal_다음_Hard_첫_클리어는_이번_판분_25만_주고_소급분은_0이다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile { ClearedStage = 1 };
            p.RecordDifficultyClear(1, 1);
            CurrencySettlement.SettleChapterWin(p, growth, 1, 0);
            int paidBefore = p.GetAwakenPaid(1);
            p.RecordDifficultyClear(1, 2);

            ChapterAwakenGain hard = CurrencySettlement.SettleChapterWin(p, growth, 2, paidBefore);

            Assert.AreEqual(1, paidBefore);
            Assert.AreEqual(25, hard.ThisRun, "Hard 분만 (Normal 10 은 이미 받음)");
            Assert.AreEqual(0, hard.Retroactive);
            Assert.AreEqual(35, p.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(2, p.GetAwakenPaid(1));
        }

        [Test]
        public void Hard_반복_승리는_4_Hard_를_깬_스테이지의_Normal_재승리는_2를_준다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile { ClearedStage = 1 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 2, AwakenPaid = 2 });

            ChapterAwakenGain hardRepeat = CurrencySettlement.SettleChapterWin(p, growth, 2, 2);
            ChapterAwakenGain normalRepeat = CurrencySettlement.SettleChapterWin(p, growth, 1, 2);

            Assert.AreEqual(4, hardRepeat.ThisRun);
            Assert.AreEqual(2, normalRepeat.ThisRun);
            Assert.AreEqual(0, hardRepeat.Retroactive + normalRepeat.Retroactive);
            Assert.AreEqual(6, p.GetCurrency(ECurrency.AwakenStone));
        }

        [Test]
        public void Easy_승리는_각성석이_0이고_다른_스테이지_미지급분도_정산하지_않는다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 4, MaxDifficulty = 2 });

            ChapterAwakenGain easy = CurrencySettlement.SettleChapterWin(p, growth, 0, 0);

            Assert.AreEqual(0, easy.ThisRun + easy.Retroactive);
            Assert.AreEqual(0, p.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(0, p.GetAwakenPaid(4), "Easy 판은 정산 함수를 돌리지 않는다 - 소급은 마을 진입이 담당");
        }

        [Test]
        public void 첫_클리어_정산은_음수로_변조된_AwakenPaid_도_Normal_부터_지급한다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 2, AwakenPaid = -3 });
            p.DifficultyClears.Add(null);

            int paid = CurrencySettlement.SettleAwakenFirstClears(p, growth);

            Assert.AreEqual(35, paid, "10 + 25 - null 행은 건너뛴다");
            Assert.AreEqual(2, p.GetAwakenPaid(2));
        }

        [Test]
        public void 소급_정산은_여러_스테이지를_한_번에_합산하고_두_번째_마을_진입엔_0이다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = new MetaProfile();
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 1 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 2, AwakenPaid = 1 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 3, MaxDifficulty = 2, AwakenPaid = 2 });

            List<CurrencyGain> first = CurrencySettlement.SettleRetroactive(p, growth, null, 2);
            List<CurrencyGain> second = CurrencySettlement.SettleRetroactive(p, growth, null, 2);

            Assert.AreEqual(1, first.Count, "탑 데이터가 없으면 각성석 줄만");
            Assert.AreEqual(ECurrency.AwakenStone, first[0].Currency);
            Assert.AreEqual(35, first[0].Amount, "10 (1-1 Normal) + 25 (1-2 Hard) + 0 (1-3 이미 지급)");
            Assert.AreEqual(0, second.Count, "0 은 줄을 만들지 않는다 - 토스트 없음");
        }

        //# ===== 달빛 인장 (§2.2 표 전 행, 출하 DailyConfig — 0.10 float → decimal 정밀도까지 검증) =====

        [TestCase(1, 3, 8)]
        [TestCase(2, 3, 9)]
        [TestCase(3, 4, 10)]
        [TestCase(4, 4, 11)]
        [TestCase(5, 4, 11)]
        [TestCase(6, 5, 13)]
        [TestCase(7, 5, 13)]
        [TestCase(10, 5, 13)]
        [TestCase(0, 3, 8)]
        public void 달빛_인장_표_전_행은_판_종료분과_첫_승리분을_각각_반올림한다(int streak, int finishOnly, int withFirstWin)
        {
            DailyConfigRow cfg = GrowthTestKit.ShippedDailyConfig();

            Assert.AreEqual(finishOnly, CurrencySettlement.DailySeals(false, streak, cfg));
            Assert.AreEqual(withFirstWin, CurrencySettlement.DailySeals(true, streak, cfg));
        }

        [Test]
        public void 하루_3판과_첫_승리로_첫_7일_누계는_131이다()
        {
            DailyConfigRow cfg = GrowthTestKit.ShippedDailyConfig();
            int total = 0;
            for (int day = 1; day <= 7; ++day)
            {
                total += CurrencySettlement.DailySeals(true, day, cfg) + 2 * CurrencySettlement.DailySeals(false, day, cfg);
            }

            Assert.AreEqual(131, total, "14 + 15 + 18 + 19 + 19 + 23 + 23");
        }

        [Test]
        public void 인장_설정이_없거나_음수면_0이다()
        {
            DailyConfigRow negative = new DailyConfigRow { sealPerFinish = -3, sealFirstWin = -5, streakBonusPerDay = 0.1f, streakBonusMaxDays = 7 };

            Assert.AreEqual(0, CurrencySettlement.DailySeals(true, 3, null));
            Assert.AreEqual(0, CurrencySettlement.DailySeals(true, 3, negative));
        }

        //# ===== 심연 결정 (§2.3 누적표, 출하 TowerConfig + Difficulty_TowerCrystal) =====

        [TestCase(5, 5)]
        [TestCase(10, 20)]
        [TestCase(15, 30)]
        [TestCase(20, 50)]
        [TestCase(25, 65)]
        [TestCase(30, 90)]
        [TestCase(35, 105)]
        [TestCase(40, 130)]
        public void 한_시즌_도달_층별_누적_결정은_기획서_누적표와_같다(int floor, int expected)
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            TowerDatabase tower = GrowthTestKit.ShippedTower();
            int total = 0;
            for (int f = 1; f <= floor; ++f)
            {
                total += CurrencySettlement.CrystalsForFloor(f, growth, tower, 2);
            }

            Assert.AreEqual(expected, total);
        }

        [TestCase(1, 1)]
        [TestCase(9, 1)]
        [TestCase(10, 11)]
        [TestCase(11, 2)]
        [TestCase(19, 2)]
        [TestCase(20, 12)]
        [TestCase(21, 3)]
        [TestCase(30, 13)]
        [TestCase(0, 0)]
        public void 층당_결정은_1_10층_1_11_20층_2_21층부터_3이고_10의_배수층은_10을_더한다(int floor, int expected)
        {
            Assert.AreEqual(expected, CurrencySettlement.CrystalsForFloor(floor, GrowthTestKit.Shipped(), GrowthTestKit.ShippedTower(), 2));
        }

        [Test]
        public void 난이도_표_미로드면_탑_등급이_0으로_묶여_층당_1과_이정표만_준다()
        {
            Assert.AreEqual(1, CurrencySettlement.CrystalsForFloor(25, GrowthTestKit.Shipped(), GrowthTestKit.ShippedTower(), 0));
            Assert.AreEqual(11, CurrencySettlement.CrystalsForFloor(30, GrowthTestKit.Shipped(), GrowthTestKit.ShippedTower(), 0));
        }

        [Test]
        public void 같은_층을_다시_정산해도_결정은_한_번만이고_새_층만_더해진다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            TowerDatabase tower = GrowthTestKit.ShippedTower();
            MetaProfile p = new MetaProfile();
            int season = tower.Config.seasonId;
            p.RecordTowerClear(1, 30f, season);
            p.RecordTowerClear(2, 30f, season);

            int first = CurrencySettlement.SettleTowerCrystals(p, growth, tower, 2);
            p.RecordTowerClear(2, 20f, season);
            int again = CurrencySettlement.SettleTowerCrystals(p, growth, tower, 2);
            p.RecordTowerClear(3, 30f, season);
            int next = CurrencySettlement.SettleTowerCrystals(p, growth, tower, 2);

            Assert.AreEqual(2, first);
            Assert.AreEqual(0, again, "기록 갱신(더 빠른 클리어)도 재지급 없음");
            Assert.AreEqual(1, next);
            Assert.AreEqual(3, p.GetCurrency(ECurrency.AbyssCrystal));
        }

        [Test]
        public void 시즌이_바뀌면_지난_시즌_미지급_층은_지급하지_않고_보유_결정은_유지된다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            TowerDatabase tower = GrowthTestKit.ShippedTower();
            MetaProfile p = new MetaProfile { TowerSeason = tower.Config.seasonId - 1 };
            p.AddCurrency(ECurrency.AbyssCrystal, 7);
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 10, ClearTime = 40f, CrystalPaid = false });

            List<CurrencyGain> gains = CurrencySettlement.SettleRetroactive(p, growth, tower, 2);

            Assert.AreEqual(0, gains.Count, "시즌 정리 → 정산 순 (E-8)");
            Assert.AreEqual(0, p.TowerFloorClears.Count);
            Assert.AreEqual(7, p.GetCurrency(ECurrency.AbyssCrystal));
        }

        [Test]
        public void 새_시즌에_같은_층을_다시_깨면_결정을_다시_받는다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            TowerDatabase tower = GrowthTestKit.ShippedTower();
            int season = tower.Config.seasonId;
            MetaProfile p = new MetaProfile { TowerSeason = season - 1 };
            for (int f = 1; f <= 10; ++f)
            {
                p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = f, ClearTime = 30f, CrystalPaid = true });
            }
            p.AddCurrency(ECurrency.AbyssCrystal, 20);

            p.NormalizeTowerSeason(season);
            for (int f = 1; f <= 10; ++f)
            {
                p.RecordTowerClear(f, 30f, season);
            }
            int paid = CurrencySettlement.SettleTowerCrystals(p, growth, tower, 2);

            Assert.AreEqual(20, paid, "U-2 - 시즌마다 재획득");
            Assert.AreEqual(40, p.GetCurrency(ECurrency.AbyssCrystal));
        }

        [Test]
        public void 같은_시즌의_소급_정산은_업데이트_이전_층만_주고_이미_받은_층은_건너뛴다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            TowerDatabase tower = GrowthTestKit.ShippedTower();
            MetaProfile p = new MetaProfile { TowerSeason = tower.Config.seasonId };
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 10, ClearTime = 30f, CrystalPaid = false });
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 11, ClearTime = 30f, CrystalPaid = true });

            List<CurrencyGain> gains = CurrencySettlement.SettleRetroactive(p, growth, tower, 2);

            Assert.AreEqual(1, gains.Count);
            Assert.AreEqual(ECurrency.AbyssCrystal, gains[0].Currency);
            Assert.AreEqual(11, gains[0].Amount);
            Assert.IsTrue(p.TowerFloorClears.TrueForAll(e => e.CrystalPaid));
        }
    }
}
