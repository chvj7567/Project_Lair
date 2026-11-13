using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 몬스터 등급 2~5 자체 확인 (gameplay-programmer 정상 + 엣지 수준). 본격 스위트(T1~T8)는 test-engineer.
    public class MonsterGradeSmokeTests
    {
        private FormationDatabase _db;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
        }

        [Test]
        public void 등급_몬스터는_값명_규칙대로_기준_몬스터와_등급으로_해석된다()
        {
            Assert.AreEqual(EMonster.Wisp, MonsterGrades.BaseOf(EMonster.WispG2));
            Assert.AreEqual(2, MonsterGrades.GradeOf(EMonster.WispG2));
            Assert.AreEqual(EMonster.PhantomG5, MonsterGrades.Of(EMonster.Phantom, 5));
            Assert.AreEqual(EMonster.Hex, MonsterGrades.Of(EMonster.HexG4, 1));
            Assert.AreEqual(1, MonsterGrades.GradeOf(EMonster.Phantom));
            Assert.AreEqual(6, MonsterGrades.BaseMonsters.Count);
            Assert.AreEqual(5, _db.MonstersOf(ERace.Miar).Count, "Race_Monster 30행 - 종족마다 1~5등급");
        }

        [Test]
        public void 등급_배율은_HP와_공격력에만_곱하고_1등급은_1이다()
        {
            Assert.AreEqual(1f, _db.HpMulOf(EMonster.Reaper), 0.0001f);
            Assert.AreEqual(1.22f, _db.HpMulOf(EMonster.ReaperG3), 0.0001f);
            Assert.AreEqual(1.52f, _db.AtkMulOf(EMonster.ReaperG5), 0.0001f);
        }

        [Test]
        public void 견습_승급은_소울을_내고_보유_등급이_오르며_6행_편성의_그_종족_둥지에_자동_배치되고_편성_창이_열린다()
        {
            MetaProfile p = new MetaProfile { Souls = 1000, ClearedStage = 3, Formation = FormationTestKit.Rows(FormationTestKit.Default()) };

            Assert.IsTrue(GradePromotion.TryPromote(p, _db, ERace.Ignis));

            Assert.AreEqual(200, p.Souls);
            Assert.AreEqual(2, p.GetRaceGrade(ERace.Ignis));
            Assert.AreEqual("WispG2", p.Formation[0].Monster, "둥지 1 = 이그니스");
            Assert.AreEqual(EMonster.WispG2, _db.DefaultFormation(p.GetRaceGrade)[0]);
            Assert.IsTrue(FormationRules.ShouldShowPopup(_db, p.ClearedStage, p.GetRaceGrade));
        }

        [Test]
        public void 특수_재화가_모자라면_승급을_거부하고_소울도_차감하지_않는다()
        {
            MetaProfile p = new MetaProfile { Souls = 5000 };
            p.SetRaceGrade(ERace.Nox, 2);

            Assert.IsFalse(GradePromotion.TryPromote(p, _db, ERace.Nox), "숙련 = 소울 1500 + 각성석 30");
            Assert.AreEqual(5000, p.Souls);
            Assert.AreEqual(2, p.GetRaceGrade(ERace.Nox));
        }

        [Test]
        public void 군주는_더_승급할_수_없다()
        {
            MetaProfile p = new MetaProfile { Souls = 99999 };
            p.SetRaceGrade(ERace.Hexia, 5);

            Assert.AreEqual(0, GradePromotion.NextGrade(p, _db, ERace.Hexia));
            Assert.IsFalse(GradePromotion.TryPromote(p, _db, ERace.Hexia));
        }

        [Test]
        public void 보유하지_않은_등급을_가리키는_저장_편성은_그_둥지만_보유_최고_등급이_된다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 3 };
            p.SetRaceGrade(ERace.Nox, 2);
            EMonster[] saved = FormationTestKit.Default();
            saved[1] = EMonster.ReaperG3;

            EMonster[] resolved = FormationSave.Resolve(_db, FormationTestKit.Rows(saved), p.ClearedStage, p.GetRaceGrade);

            Assert.AreEqual(EMonster.ReaperG2, resolved[1]);
            Assert.AreEqual(EMonster.Wisp, resolved[0]);
        }

        [Test]
        public void 카드_종족_제외는_등급_몬스터가_나가도_그_종족_카드를_남긴다()
        {
            FormationCardSpecies cards = FormationTestKit.ShippedCards(_db);
            HashSet<EMonster> outputs = new HashSet<EMonster> { EMonster.WispG4 };

            Assert.IsTrue(cards.IsAvailable(ECardId.WispHpBoost, m => MonsterGradesOutputting(outputs, m)));
            Assert.IsFalse(cards.IsAvailable(ECardId.SpawnReapers, m => MonsterGradesOutputting(outputs, m)));
        }

        [Test]
        public void 상점_몬스터_행은_승급_칸을_5번째로_갖고_행_머리에_보유_등급_접두어가_붙는다()
        {
            MetaConfig cfg = WorldMapTestSupport.ShippedMeta();
            MetaProfile p = new MetaProfile { Souls = 500 };
            p.SetRaceGrade(ERace.Ignis, 5);

            List<ShopItemCellData> cells = ShopPopup.BuildCellData(p, cfg, null, null, WorldMapTestSupport.Text, _db);
            ShopItemCellData wisp = cells.Find(c => c.Species == EMonster.Wisp);
            ShopItemCellData reaper = cells.Find(c => c.Species == EMonster.Reaper);

            Assert.AreEqual(5, wisp.Slots.Length);
            Assert.IsTrue(wisp.Slots[4].IsPromotion);
            Assert.IsTrue(wisp.Slots[4].IsMax, "군주 보유 = 최고 등급");
            StringAssert.Contains("군주", wisp.SpeciesName);
            Assert.IsFalse(reaper.Slots[4].CanBuy, "견습 소울 800 > 보유 500");
            Assert.AreEqual(ECurrency.Soul, reaper.Slots[4].LackingCurrency);
        }

        private static bool MonsterGradesOutputting(HashSet<EMonster> outputs, EMonster species)
        {
            foreach (EMonster m in outputs)
            {
                if (MonsterGrades.SameSpecies(m, species))
                    return true;
            }
            return false;
        }
    }
}
