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
        public void 저장_편성의_무효_슬롯만_종족_보유_최고_등급으로_대체된다()
        {
            List<string> warnings = new List<string>();
            EMonster[] resolved = FormationSave.Resolve(_db, FormationTestKit.Rows(FormationTestKit.Mismatched()), 5, null, warn: warnings.Add);

            CollectionAssert.AreEqual(FormationTestKit.Default(), resolved, "슬롯 1·4 가 다른 종족 - 그 슬롯만 보유 최고 등급, 나머지는 저장값 유지(원래 기본과 같음)");
            Assert.AreEqual(1, warnings.Count);
        }

        [Test]
        public void 모든_종족이_1등급뿐이면_1_1_클리어_후에도_편성_팝업을_열지_않는다()
        {
            Assert.IsFalse(FormationRules.ShouldShowPopup(_db, 25, new MetaProfile().GetRaceGrade));
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
