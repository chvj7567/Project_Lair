using System;
using System.Linq;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - 출하 JSON 5종 로드/교차 검증과 §3.3 위반 케이스별 로드 오류 (dungeon-formation §9.1 "데이터 로드").
    public class FormationDataTests
    {
        //# --- 출하 데이터 ---

        [Test]
        public void 출하_설정은_상한_13_해금_1_종당_2에서_3_확장_5다()
        {
            FormationConfigRow c = FormationDataSet.Shipped().Build().Config;
            Assert.AreEqual(13, c.costCap);
            Assert.AreEqual(1, c.unlockClearedStage);
            Assert.AreEqual(2, c.maxPerSpeciesBase);
            Assert.AreEqual(3, c.maxPerSpeciesExpanded);
            Assert.AreEqual(5, c.expandClearedStage);
        }

        [Test]
        public void 출하_몬스터_6행은_JSON_순서대로_코스트_3_3_3_2_1_1이다()
        {
            FormationDatabase db = FormationTestKit.ShippedDb();
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom },
                db.Monsters);
            CollectionAssert.AreEqual(new[] { 3, 3, 3, 2, 1, 1 }, db.Monsters.Select(db.CostOf).ToArray());
        }

        [Test]
        public void 몬스터_행은_EMonster_전_종을_정확히_한_번씩_덮는다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            Assert.AreEqual(Enum.GetValues(typeof(EMonster)).Length, set.Monsters.Count);
            CollectionAssert.AreEquivalent(Enum.GetNames(typeof(EMonster)), set.Monsters.Select(m => m.monster).ToArray());
        }

        [Test]
        public void 출하_슬롯_6행은_slot_1에서_6_순서이고_기본_편성_순서와_같다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            Assert.AreEqual(6, set.Slots.Count);
            CollectionAssert.AreEqual(new[] { 1, 2, 3, 4, 5, 6 }, set.Slots.Select(s => s.slot).ToArray());
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom },
                set.Build().DefaultFormation());
        }

        [Test]
        public void 슬롯_행_순서가_뒤섞여도_slot_번호_기준으로_기본_편성이_정렬된다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Slots.Reverse();
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom },
                set.Build().DefaultFormation());
        }

        [Test]
        public void 기본_편성_복사본을_바꿔도_DB는_영향받지_않는다()
        {
            FormationDatabase db = FormationTestKit.ShippedDb();
            EMonster[] copy = db.DefaultFormation();
            copy[0] = EMonster.Phantom;
            Assert.AreEqual(EMonster.Wisp, db.DefaultFormation()[0]);
            Assert.AreNotSame(db.DefaultFormation(), db.DefaultFormation());
        }

        [Test]
        public void 출하_기본_편성은_상한과_종당_기본_최대_안의_유효_편성이다()
        {
            FormationDatabase db = FormationTestKit.ShippedDb();
            Assert.IsNull(FormationRules.Describe(db.DefaultFormation(), db.Config.costCap, db.Config.maxPerSpeciesBase, db.CostOf));
            Assert.AreEqual(13, FormationRules.CostSum(db.DefaultFormation(), db.CostOf), "기본 편성 코스트 합 = 상한(경계)");
        }

        [Test]
        public void 출하_Card_Species_16행은_모두_ECardId_값명과_편성_종이다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            Assert.AreEqual(16, set.CardSpecies.Count);
            string[] cardNames = Enum.GetNames(typeof(ECardId));
            foreach (Card_SpeciesRow row in set.CardSpecies)
            {
                CollectionAssert.Contains(cardNames, row.cardId);
                Assert.IsTrue(FormationDatabase.TryParseMonster(row.monster, out _), row.monster);
            }
            Assert.AreEqual(16, set.CardSpecies.Select(r => r.cardId + "|" + r.monster).Distinct().Count(), "복합키 (cardId, monster) 중복 없음");
            Assert.AreEqual(15, set.CardSpecies.Select(r => r.cardId).Distinct().Count(), "Berserk 만 두 종에 걸침");
            CollectionAssert.AreEquivalent(
                new[] { "Wisp", "Wraith" },
                set.CardSpecies.Where(r => r.cardId == "Berserk").Select(r => r.monster).ToArray());
            Assert.DoesNotThrow(() => set.BuildCards(set.Build()));
        }

        [Test]
        public void 출하_챕터_성향_5행은_Chapter_JSON과_일치하고_문구_1408에서_1412에_매핑된다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            FormationDatabase db = set.Build();
            Assert.AreEqual(5, set.Hints.Count);
            CollectionAssert.AreEquivalent(
                WorldMapTestSupport.FullDb().Chapters.Select(c => c.chapterId).ToArray(),
                set.Hints.Select(h => h.chapterId).ToArray());
            for (int chapter = 1; chapter <= 5; ++chapter)
            {
                Assert.AreEqual(1407 + chapter, db.HintKeyOf(chapter), "챕터 " + chapter);
                Assert.IsNotEmpty(WorldMapTestSupport.Text(db.HintKeyOf(chapter)), "챕터 " + chapter + " 문구");
            }
            Assert.AreEqual(0, db.HintKeyOf(6), "없는 챕터는 0");
            Assert.AreEqual(0, db.HintKeyOf(0));
        }

        [Test]
        public void Strings_Ko_1393에서_1412는_20개가_빠짐없이_연속_존재한다()
        {
            for (int id = 1393; id <= 1412; ++id)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), "Strings_Ko " + id);
            }
        }

        [Test]
        public void 편성_VM_문구_id의_포맷_인자_수가_문구의_자리표시자와_일치한다()
        {
            int[] argCounts = { 2, 1, 2, 1, 1, 2, 2, 3, 0 };   //# 1397~1405
            for (int i = 0; i < argCounts.Length; ++i)
            {
                int id = 1397 + i;
                string text = WorldMapTestSupport.Text(id);
                int highest = -1;
                foreach (Match m in Regex.Matches(text, @"\{(\d+)\}"))
                {
                    highest = Math.Max(highest, int.Parse(m.Groups[1].Value));
                }
                Assert.AreEqual(argCounts[i], highest + 1, $"{id}: {text}");
            }
        }

        //# --- FormationConfig 위반 ---

        private static void AssertThrows(FormationDataSet set, string expectedPart)
        {
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());
            StringAssert.Contains(expectedPart, e.Message);
        }

        [Test]
        public void 설정이_null이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Config = null;
            Assert.Throws<ChapterDataException>(() => set.Build());
        }

        [Test]
        public void costCap이_6_미만이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Config.costCap = 5;
            AssertThrows(set, "costCap 5");
        }

        [Test]
        public void costCap_6은_설정_검증은_통과하지만_기본_편성_초과로_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Config.costCap = 6;
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());
            StringAssert.DoesNotContain("costCap 6 는", e.Message);
            StringAssert.Contains("기본 편성이 유효하지 않음", e.Message);
        }

        [Test]
        public void 종당_최대_위반은_각각_로드_오류다()
        {
            FormationDataSet zero = FormationDataSet.Shipped();
            zero.Config.maxPerSpeciesBase = 0;
            AssertThrows(zero, "maxPerSpeciesBase");

            FormationDataSet inverted = FormationDataSet.Shipped();
            inverted.Config.maxPerSpeciesBase = 3;
            inverted.Config.maxPerSpeciesExpanded = 2;
            AssertThrows(inverted, "maxPerSpeciesBase");

            FormationDataSet seven = FormationDataSet.Shipped();
            seven.Config.maxPerSpeciesExpanded = 7;
            AssertThrows(seven, "maxPerSpeciesExpanded");
        }

        [Test]
        public void 해금_단계_위반은_로드_오류다()
        {
            FormationDataSet negative = FormationDataSet.Shipped();
            negative.Config.unlockClearedStage = -1;
            AssertThrows(negative, "unlockClearedStage");

            FormationDataSet afterExpand = FormationDataSet.Shipped();
            afterExpand.Config.unlockClearedStage = 6;
            AssertThrows(afterExpand, "unlockClearedStage");
        }

        [Test]
        public void 해금과_확장이_같은_단계여도_유효하다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Config.unlockClearedStage = 5;
            Assert.DoesNotThrow(() => set.Build());
        }

        //# --- FormationMonster 위반 ---

        [Test]
        public void 코스트가_0_이하인_몬스터는_로드_오류다()
        {
            FormationDataSet zero = FormationDataSet.Shipped();
            zero.Monsters[0].cost = 0;
            AssertThrows(zero, "cost 는 1 이상");

            FormationDataSet negative = FormationDataSet.Shipped();
            negative.Monsters[5].cost = -2;
            AssertThrows(negative, "cost 는 1 이상");
        }

        [Test]
        public void 몬스터_행이_부족하거나_넘치면_로드_오류다()
        {
            FormationDataSet few = FormationDataSet.Shipped();
            few.Monsters.RemoveAt(5);
            AssertThrows(few, "FormationMonster.json 행");

            FormationDataSet many = FormationDataSet.Shipped();
            many.Monsters.Add(new FormationMonsterRow { monster = "Wisp", cost = 3 });
            AssertThrows(many, "FormationMonster.json 행");
        }

        [Test]
        public void 알_수_없는_종_이름과_숫자_문자열과_대소문자_불일치는_로드_오류다()
        {
            foreach (string bad in new[] { "Dragon", "1", "wisp", "", null })
            {
                FormationDataSet set = FormationDataSet.Shipped();
                set.Monsters[0].monster = bad;
                AssertThrows(set, "EMonster 값명이 아님");
            }
        }

        [Test]
        public void 같은_종이_두_행이면_중복_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Monsters[1].monster = "Wisp";
            AssertThrows(set, "Wisp 중복");
        }

        [Test]
        public void 몬스터_행_배열이_null이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => FormationDatabase.Build(
                set.Config, null, set.Slots.ToArray(), set.Hints.ToArray()));
            StringAssert.Contains("FormationMonster.json", e.Message);
        }

        //# --- FormationSlot 위반 ---

        [Test]
        public void 슬롯_행이_누락되거나_추가되면_로드_오류다()
        {
            FormationDataSet missing = FormationDataSet.Shipped();
            missing.Slots.RemoveAt(2);
            AssertThrows(missing, "정확히 6개");

            FormationDataSet extra = FormationDataSet.Shipped();
            extra.Slots.Add(new FormationSlotRow { slot = 7, defaultMonster = "Wisp" });
            AssertThrows(extra, "정확히 6개");
        }

        [Test]
        public void 슬롯_번호가_중복이거나_범위_밖이면_로드_오류다()
        {
            FormationDataSet dup = FormationDataSet.Shipped();
            dup.Slots[5].slot = 1;
            AssertThrows(dup, "slot 1 중복");

            FormationDataSet zero = FormationDataSet.Shipped();
            zero.Slots[0].slot = 0;
            AssertThrows(zero, "slot 은 1~6");

            FormationDataSet seven = FormationDataSet.Shipped();
            seven.Slots[0].slot = 7;
            AssertThrows(seven, "slot 은 1~6");
        }

        [Test]
        public void 슬롯의_기본_몬스터가_알_수_없는_종이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Slots[3].defaultMonster = "Dragon";
            AssertThrows(set, "defaultMonster 'Dragon'");
        }

        [Test]
        public void 기본_편성_코스트가_상한을_넘으면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Config.costCap = 12;
            AssertThrows(set, "기본 편성이 유효하지 않음");
        }

        [Test]
        public void 기본_편성이_종당_기본_최대를_넘으면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            foreach (FormationSlotRow slot in set.Slots)
            {
                slot.defaultMonster = "Phantom";
            }
            AssertThrows(set, "종당 최대");
        }

        [Test]
        public void 기본_편성이_종당_기본_최대와_같으면_유효하다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Slots[1].defaultMonster = "Wisp";
            Assert.DoesNotThrow(() => set.Build(), "도깨비불 2곳 = 종당 2 경계, 코스트 13");
        }

        [Test]
        public void 여러_위반이_한_번의_예외에_모두_모인다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Monsters[0].cost = 0;
            set.Slots[0].slot = 9;
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.Build());
            StringAssert.Contains("cost 는 1 이상", e.Message);
            StringAssert.Contains("slot 은 1~6", e.Message);
        }

        //# --- Chapter_FormationHint 위반 ---

        [Test]
        public void 챕터_성향_행이_누락되면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Hints.RemoveAt(4);
            AssertThrows(set, "chapterId 5 행이 없음");
        }

        [Test]
        public void Chapter_JSON에_없는_챕터의_성향_행은_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Hints.Add(new Chapter_FormationHintRow { chapterId = 6, hintKey = 1412 });
            AssertThrows(set, "chapterId 6 가 Chapter.json 에 없음");
        }

        [Test]
        public void 챕터_성향_chapterId가_중복이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Hints[1].chapterId = 1;
            AssertThrows(set, "chapterId 1 중복");
        }

        [Test]
        public void 성향_문구_id가_Strings_Ko에_없으면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Hints[0].hintKey = 99999;
            AssertThrows(set, "hintKey 99999");
        }

        [Test]
        public void 챕터_목록과_문구_검증_인자가_없으면_교차_검사를_생략한다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.Hints.RemoveAt(4);
            set.Hints[0].hintKey = 99999;
            Assert.DoesNotThrow(() => set.BuildWithoutCrossCheck());
        }

        [Test]
        public void 성향_행_배열이_null이면_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            Assert.Throws<ChapterDataException>(() => FormationDatabase.Build(set.Config, set.Monsters.ToArray(), set.Slots.ToArray(), null));
        }

        //# --- Card_Species 위반 ---

        [Test]
        public void Card_Species_알_수_없는_카드_id는_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.CardSpecies[0].cardId = "NoSuchCard";
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.BuildCards(set.Build()));
            StringAssert.Contains("ECardId 값명이 아님", e.Message);
        }

        [Test]
        public void Card_Species_알_수_없는_종은_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.CardSpecies[0].monster = "Dragon";
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.BuildCards(set.Build()));
            StringAssert.Contains("FormationMonster 에 없음", e.Message);
        }

        [Test]
        public void Card_Species_복합키_중복은_로드_오류다()
        {
            FormationDataSet set = FormationDataSet.Shipped();
            set.CardSpecies.Add(new Card_SpeciesRow { cardId = "WispHpBoost", monster = "Wisp" });
            ChapterDataException e = Assert.Throws<ChapterDataException>(() => set.BuildCards(set.Build()));
            StringAssert.Contains("중복", e.Message);
        }

        [Test]
        public void Card_Species_배열이_null이면_로드_오류이고_빈_배열은_허용된다()
        {
            Assert.Throws<ChapterDataException>(() => FormationCardSpecies.Build(null, FormationTestKit.ShippedDb()));
            FormationCardSpecies empty = FormationCardSpecies.Build(new Card_SpeciesRow[0], FormationTestKit.ShippedDb());
            Assert.IsTrue(empty.IsAvailable(ECardId.WispHpBoost, m => false), "행이 없으면 모든 카드가 항상 후보");
        }

        [Test]
        public void Card_Species_null_행은_건너뛰고_formation이_null이면_종_존재만_검사한다()
        {
            Card_SpeciesRow[] rows = { null, new Card_SpeciesRow { cardId = "WispHpBoost", monster = "Wisp" } };
            FormationCardSpecies table = FormationCardSpecies.Build(rows, null);
            Assert.IsFalse(table.IsAvailable(ECardId.WispHpBoost, m => false));
            Assert.IsTrue(table.IsAvailable(ECardId.WispHpBoost, m => m == EMonster.Wisp));
        }

        [Test]
        public void 정적_Current는_Build만으로_바뀌지_않는다()
        {
            FormationDatabase prev = FormationDatabase.Current;
            FormationCardSpecies prevCards = FormationCardSpecies.Current;
            FormationTestKit.ShippedCards(FormationTestKit.ShippedDb());
            Assert.AreSame(prev, FormationDatabase.Current);
            Assert.AreSame(prevCards, FormationCardSpecies.Current);
        }

        [Test]
        public void TryParseMonster는_값명_정확_일치만_허용한다()
        {
            Assert.IsTrue(FormationDatabase.TryParseMonster("Phantom", out EMonster m));
            Assert.AreEqual(EMonster.Phantom, m);
            foreach (string bad in new[] { null, "", " Wisp", "wisp", "0", "Wisp,Hex" })
            {
                Assert.IsFalse(FormationDatabase.TryParseMonster(bad, out _), "[" + bad + "]");
            }
        }

        [Test]
        public void 코스트는_알_수_없는_종에서_0이다()
        {
            FormationDatabase db = FormationTestKit.ShippedDb();
            Assert.AreEqual(0, db.CostOf((EMonster)99));
        }
    }
}
