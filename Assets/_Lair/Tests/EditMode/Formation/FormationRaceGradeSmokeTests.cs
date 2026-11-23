using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 종족·등급 개편 자체 확인 (gameplay-programmer 정상 + 엣지 수준). 본격 스위트는 test-engineer (dungeon-formation §0.10 테스트 목록).
    public class FormationRaceGradeSmokeTests
    {
        private FormationDatabase _db;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
        }

        [Test]
        public void 출하_데이터의_기본_편성은_승급_전_현행_씬_배치와_같다()
        {
            CollectionAssert.AreEqual(FormationTestKit.Default(), _db.DefaultFormation(null));
            Assert.AreEqual(ERace.Ignis, _db.SlotRace(0));
            Assert.AreEqual(3, _db.SlotOf(ERace.Lemur));
        }

        [Test]
        public void 저장_편성은_종족_고정_없이_읽고_중복_몬스터와_미보유_등급_행만_비운다()
        {
            //# 자유 편성(dungeon-formation-free §1·§5) — 사신 2·환령 2 중복은 낮은 번호만 남기고 빈칸, 빈칸을 기본 몬스터로 채우지 않는다.
            EMonster?[] slots = FormationSave.ReadCustom(_db, FormationTestKit.Rows(FormationTestKit.Mismatched()), null, out bool repaired);

            CollectionAssert.AreEqual(
                new EMonster?[] { EMonster.Reaper, null, EMonster.Hex, EMonster.Phantom, EMonster.Plague, null }, slots);
            Assert.IsTrue(repaired, "비운 행이 있으면 복구 표시");

            List<FormationSlotEntry> notOwned = new List<FormationSlotEntry>
            {
                new FormationSlotEntry { Slot = 1, Monster = EMonster.WispG3.ToString() },
                new FormationSlotEntry { Slot = 2, Monster = EMonster.Wisp.ToString() },
            };
            slots = FormationSave.ReadCustom(_db, notOwned, null, out repaired);
            CollectionAssert.AreEqual(new EMonster?[] { null, EMonster.Wisp, null, null, null, null }, slots, "숙련 도깨비불은 1등급 보유라 비움");
            Assert.IsTrue(repaired);
        }

        [Test]
        public void 유효한_부분_편성은_복구_표시_없이_빈칸을_그대로_둔다()
        {
            EMonster?[] slots = FormationSave.ReadCustom(_db, new List<FormationSlotEntry>
            {
                new FormationSlotEntry { Slot = 3, Monster = EMonster.Reaper.ToString() },
                new FormationSlotEntry { Slot = 5, Monster = EMonster.Wisp.ToString() },
            }, null, out bool repaired);

            CollectionAssert.AreEqual(new EMonster?[] { null, null, EMonster.Reaper, null, EMonster.Wisp, null }, slots);
            Assert.IsFalse(repaired);
        }

        [Test]
        public void 모든_종족이_1등급뿐이어도_1_1_클리어_후에는_편성_팝업을_연다()
        {
            Assert.IsTrue(FormationRules.ShouldShowPopup(_db, 25, new MetaProfile().GetRaceGrade), "위치 변경·비우기가 가능해 선택지가 항상 있다");
            Assert.IsTrue(FormationRules.ShouldShowPopup(_db, 1, new MetaProfile().GetRaceGrade));
            Assert.IsFalse(FormationRules.ShouldShowPopup(_db, 0, new MetaProfile().GetRaceGrade), "1-1 클리어 전");
            Assert.IsFalse(FormationRules.ShouldShowPopup(null, 25, null), "데이터 미로드");
        }

        [Test]
        public void 봉인은_봉인_종과_같은_종족의_둥지_한_곳이다()
        {
            Assert.AreEqual(3, FormationRules.PickSealedSlot(FormationTestKit.Default(), EMonster.Wraith));
            Assert.AreEqual(-1, FormationRules.PickSealedSlot(new[] { EMonster.Wisp, EMonster.Hex }, EMonster.Wraith), "그 종족 둥지가 없으면 봉인 없음");
        }

        [Test]
        public void 보유_등급_정리는_잘못된_행을_지우고_같은_종족은_최고_등급만_남긴다()
        {
            MetaProfile p = new MetaProfile
            {
                RaceGrades = new List<RaceGradeEntry>
                {
                    new RaceGradeEntry { Race = "Ignis", Grade = 2 },
                    new RaceGradeEntry { Race = "Ignis", Grade = 4 },
                    new RaceGradeEntry { Race = "Nope", Grade = 3 },
                    new RaceGradeEntry { Race = "Nox", Grade = 9 },
                    null,
                },
            };

            MetaProfileMigration.NormalizeRaceGrades(p);

            Assert.AreEqual(1, p.RaceGrades.Count);
            Assert.AreEqual(4, p.GetRaceGrade(ERace.Ignis));
            Assert.AreEqual(1, p.GetRaceGrade(ERace.Nox), "행 없음 = 1등급");
        }

        [Test]
        public void 개별_몬스터_이름에는_등급_접두어가_붙고_데이터가_없으면_기존_이름이다()
        {
            Assert.AreEqual("풋내기 도깨비불", SpeciesVisual.MonsterName(EMonster.Wisp, _db, WorldMapTestSupport.Text));
            Assert.AreEqual("도깨비불", SpeciesVisual.MonsterName(EMonster.Wisp, null, WorldMapTestSupport.Text));
            Assert.AreEqual("도깨비불", SpeciesVisual.SpeciesName(EMonster.Wisp), "카드·규칙 문구용 이름은 접두어 없음");
        }
    }
}
