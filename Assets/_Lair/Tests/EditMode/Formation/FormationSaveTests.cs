using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - FormationSave 변환/판정/Resolve (dungeon-formation §3.1, §6.3, §9.1 "EffectiveFormation"·"저장 규칙").
    public class FormationSaveTests
    {
        private FormationDatabase _db;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
        }

        private static List<FormationSlotEntry> Rows(params EMonster[] monsters)
        {
            return FormationTestKit.Rows(monsters);
        }

        //# --- ToSaved ---

        [Test]
        public void ToSaved는_기본과_같으면_0행_한_칸이라도_다르면_6행이다()
        {
            Assert.AreEqual(0, FormationSave.ToSaved(_db.DefaultFormation(), _db.DefaultFormation()).Count);
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                EMonster[] changed = _db.DefaultFormation();
                changed[i] = changed[i] == EMonster.Phantom ? EMonster.Plague : EMonster.Phantom;
                Assert.AreEqual(6, FormationSave.ToSaved(changed, _db.DefaultFormation()).Count, "슬롯 " + (i + 1) + " 만 바뀜");
            }
        }

        [Test]
        public void ToSaved_6행은_슬롯_1에서_6_순서와_종_값명을_담는다()
        {
            List<FormationSlotEntry> rows = FormationSave.ToSaved(FormationTestKit.AlteredValid(), _db.DefaultFormation());
            for (int i = 0; i < 6; ++i)
            {
                Assert.AreEqual(i + 1, rows[i].Slot);
                Assert.AreEqual(FormationTestKit.AlteredValid()[i].ToString(), rows[i].Monster);
            }
        }

        [Test]
        public void ToSaved는_호출마다_새_리스트를_돌려준다()
        {
            List<FormationSlotEntry> a = FormationSave.ToSaved(_db.DefaultFormation(), _db.DefaultFormation());
            List<FormationSlotEntry> b = FormationSave.ToSaved(_db.DefaultFormation(), _db.DefaultFormation());
            Assert.AreNotSame(a, b);
            a.Add(new FormationSlotEntry());
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void 다른_편성을_6행으로_저장한_뒤_원래_기본으로_되돌리면_0행이다()
        {
            List<FormationSlotEntry> changed = FormationSave.ToSaved(FormationTestKit.AlteredValid(), _db.DefaultFormation());
            Assert.AreEqual(6, changed.Count);
            Assert.AreEqual(0, FormationSave.ToSaved(FormationSave.FromSaved(changed), FormationSave.FromSaved(changed)).Count);
            Assert.AreEqual(0, FormationSave.ToSaved(_db.DefaultFormation(), _db.DefaultFormation()).Count);
        }

        [Test]
        public void ToSaved_결과는_FromSaved로_원래_편성을_복원한다()
        {
            CollectionAssert.AreEqual(
                FormationTestKit.AlteredValid(),
                FormationSave.FromSaved(FormationSave.ToSaved(FormationTestKit.AlteredValid(), _db.DefaultFormation())));
        }

        //# --- FromSaved ---

        [Test]
        public void FromSaved는_null_0행_5행_7행에서_null이다()
        {
            Assert.IsNull(FormationSave.FromSaved(null));
            Assert.IsNull(FormationSave.FromSaved(new List<FormationSlotEntry>()));
            Assert.IsNull(FormationSave.FromSaved(Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague)));
            List<FormationSlotEntry> seven = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            seven.Add(new FormationSlotEntry { Slot = 7, Monster = "Wisp" });
            Assert.IsNull(FormationSave.FromSaved(seven));
        }

        [Test]
        public void FromSaved는_행_순서와_무관하게_슬롯_번호_위치에_배치한다()
        {
            List<FormationSlotEntry> rows = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            rows.Reverse();
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom },
                FormationSave.FromSaved(rows));
        }

        [Test]
        public void FromSaved는_중복_슬롯_범위_밖_슬롯에서_null이다()
        {
            List<FormationSlotEntry> dup = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            dup[5].Slot = 1;
            Assert.IsNull(FormationSave.FromSaved(dup));

            foreach (int bad in new[] { 0, 7, -1, 100 })
            {
                List<FormationSlotEntry> rows = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
                rows[2].Slot = bad;
                Assert.IsNull(FormationSave.FromSaved(rows), "slot " + bad);
            }
        }

        [Test]
        public void FromSaved는_미지_종_숫자_종_대소문자_다른_종_null_종에서_null이다()
        {
            foreach (string bad in new[] { "Dragon", "2", "reaper", "", null })
            {
                List<FormationSlotEntry> rows = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
                rows[3].Monster = bad;
                Assert.IsNull(FormationSave.FromSaved(rows), "[" + bad + "]");
            }
        }

        [Test]
        public void FromSaved는_null_행에서_null이다()
        {
            List<FormationSlotEntry> rows = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            rows[4] = null;
            Assert.IsNull(FormationSave.FromSaved(rows));
        }

        [Test]
        public void FromSaved는_코스트나_종당_최대는_검사하지_않는다()
        {
            List<FormationSlotEntry> rows = Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp);
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp },
                FormationSave.FromSaved(rows));
        }

        //# --- IsMalformedSave ---

        [Test]
        public void IsMalformedSave는_null과_0행과_정상_6행에서_false다()
        {
            Assert.IsFalse(FormationSave.IsMalformedSave(null));
            Assert.IsFalse(FormationSave.IsMalformedSave(new List<FormationSlotEntry>()));
            Assert.IsFalse(FormationSave.IsMalformedSave(Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom)));
        }

        [Test]
        public void IsMalformedSave는_코스트_초과_6행은_모양이_정상이라_false다()
        {
            Assert.IsFalse(FormationSave.IsMalformedSave(Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp)));
        }

        [Test]
        public void IsMalformedSave는_부분_행_중복_슬롯_미지_종_null_행에서_true다()
        {
            Assert.IsTrue(FormationSave.IsMalformedSave(Rows(EMonster.Wisp)), "1행");
            Assert.IsTrue(FormationSave.IsMalformedSave(Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague)), "5행");

            List<FormationSlotEntry> dup = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            dup[1].Slot = 1;
            Assert.IsTrue(FormationSave.IsMalformedSave(dup), "중복 슬롯");

            List<FormationSlotEntry> unknown = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            unknown[0].Monster = "Dragon";
            Assert.IsTrue(FormationSave.IsMalformedSave(unknown), "미지 종");

            List<FormationSlotEntry> hole = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            hole[0] = null;
            Assert.IsTrue(FormationSave.IsMalformedSave(hole), "null 행");
        }

        //# --- Resolve ---

        [Test]
        public void Resolve는_데이터_미로드면_고정_편성이_있어도_null이다()
        {
            Assert.IsNull(FormationSave.Resolve(null, Rows(EMonster.Wisp), 5));
            Assert.IsNull(FormationSave.Resolve(null, null, 0, FormationTestKit.AlteredValid()), "db null 이 고정 편성보다 우선");
        }

        [Test]
        public void Resolve는_잠김_null_0행이면_기본_편성_복사본이다()
        {
            List<FormationSlotEntry> valid = Rows(FormationTestKit.AlteredValid());
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, valid, 0), "ClearedStage 0 잠김");
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, null, 5), "저장 null");
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, new List<FormationSlotEntry>(), 5), "0행");

            EMonster[] result = FormationSave.Resolve(_db, null, 5);
            result[0] = EMonster.Phantom;
            Assert.AreEqual(EMonster.Wisp, _db.DefaultFormation()[0], "반환값을 바꿔도 DB 불변");
        }

        [Test]
        public void Resolve는_유효한_저장_편성을_그대로_돌려주고_경계_코스트_13도_유효다()
        {
            CollectionAssert.AreEqual(FormationTestKit.AlteredValid(), FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 1));
            EMonster[] cost13 = { EMonster.Wisp, EMonster.Wisp, EMonster.Wraith, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            CollectionAssert.AreEqual(cost13, FormationSave.Resolve(_db, Rows(cost13), 1));
        }

        [Test]
        public void Resolve는_코스트_상한을_넘는_저장은_기본으로_폴백하고_경고한다()
        {
            List<string> warnings = new List<string>();
            CollectionAssert.AreEqual(
                _db.DefaultFormation(),
                FormationSave.Resolve(_db, Rows(FormationTestKit.Cost14()), 5, warn: warnings.Add));
            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("기본 편성으로 시작", warnings[0]);
            StringAssert.Contains("상한", warnings[0]);
        }

        [Test]
        public void Resolve의_종당_최대는_ClearedStage로_갈린다()
        {
            EMonster[] triple = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Phantom, EMonster.Hex, EMonster.Hex };
            List<string> warnings = new List<string>();
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, Rows(triple), 4, warn: warnings.Add), "4: 종당 2 -> 무효");
            Assert.AreEqual(1, warnings.Count);
            CollectionAssert.AreEqual(triple, FormationSave.Resolve(_db, Rows(triple), 5), "5: 종당 3 -> 유효");
        }

        [Test]
        public void Resolve는_모양이_틀린_6행을_기본으로_폴백하고_프로필_행을_수정하지_않는다()
        {
            List<FormationSlotEntry> dup = Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            dup[5].Slot = 2;
            List<string> warnings = new List<string>();
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, dup, 5, warn: warnings.Add));
            Assert.AreEqual(1, warnings.Count);
            StringAssert.Contains("모양", warnings[0]);
            Assert.AreEqual(6, dup.Count, "읽기 전용");
            Assert.AreEqual(2, dup[5].Slot);
        }

        [Test]
        public void Resolve는_5행_부분_저장도_기본으로_폴백한다()
        {
            CollectionAssert.AreEqual(
                _db.DefaultFormation(),
                FormationSave.Resolve(_db, Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague), 5));
        }

        [Test]
        public void Resolve는_경고_콜백이_null이어도_예외없이_폴백한다()
        {
            Assert.DoesNotThrow(() => FormationSave.Resolve(_db, Rows(FormationTestKit.Cost14()), 5, warn: null));
        }

        [Test]
        public void Resolve는_유효하고_경고가_없는_경로에서_경고를_부르지_않는다()
        {
            int calls = 0;
            FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 5, warn: s => ++calls);
            FormationSave.Resolve(_db, null, 5, warn: s => ++calls);
            FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 0, warn: s => ++calls);
            Assert.AreEqual(0, calls);
        }

        [Test]
        public void 외부_고정_편성은_6칸일_때만_복사본으로_우선_적용된다()
        {
            EMonster[] fixedFormation = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague };
            EMonster[] result = FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 5, fixedFormation);
            CollectionAssert.AreEqual(fixedFormation, result, "상한/종당 검증 없이 그대로");
            Assert.AreNotSame(fixedFormation, result);
            result[0] = EMonster.Wisp;
            Assert.AreEqual(EMonster.Plague, fixedFormation[0]);

            EMonster[] five = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague };
            CollectionAssert.AreEqual(FormationTestKit.AlteredValid(), FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 5, five), "5칸 고정은 무시");
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, null, 0, new EMonster[0]), "빈 고정도 무시");
        }

        [Test]
        public void 외부_고정_편성은_잠김_상태에서도_적용된다()
        {
            EMonster[] fixedFormation = FormationTestKit.AlteredValid();
            CollectionAssert.AreEqual(fixedFormation, FormationSave.Resolve(_db, null, 0, fixedFormation));
        }

        [Test]
        public void 코스트_상한_override는_0이면_데이터값_양수면_그_값을_쓴다()
        {
            List<FormationSlotEntry> cost14 = Rows(FormationTestKit.Cost14());
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, cost14, 5, costCapOverride: 0));
            CollectionAssert.AreEqual(FormationTestKit.Cost14(), FormationSave.Resolve(_db, cost14, 5, costCapOverride: 14), "상한 14 로 올리면 유효");
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 5, costCapOverride: 10), "상한 10 으로 내리면 코스트 11 편성이 무효");
        }

        [Test]
        public void 종당_최대_override는_0이면_단계값_양수면_그_값을_쓴다()
        {
            EMonster[] triple = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Phantom, EMonster.Hex, EMonster.Hex };
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, Rows(triple), 1, maxPerSpeciesOverride: 0));
            CollectionAssert.AreEqual(triple, FormationSave.Resolve(_db, Rows(triple), 1, maxPerSpeciesOverride: 3), "ClearedStage 1 이어도 override 3");
            CollectionAssert.AreEqual(_db.DefaultFormation(), FormationSave.Resolve(_db, Rows(triple), 5, maxPerSpeciesOverride: 2), "override 2 가 단계값 3 을 낮춘다");
        }

        [Test]
        public void Resolve_결과_편성은_ToSaved를_거쳐도_같은_편성으로_수렴한다()
        {
            EMonster[] resolved = FormationSave.Resolve(_db, Rows(FormationTestKit.AlteredValid()), 5);
            CollectionAssert.AreEqual(resolved, FormationSave.FromSaved(FormationSave.ToSaved(resolved, _db.DefaultFormation())));
        }
    }
}
