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
            Assert.AreEqual(0, FormationSave.ToSaved(_db.DefaultFormation(null), _db.DefaultFormation(null)).Count);
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                EMonster[] changed = _db.DefaultFormation(null);
                changed[i] = changed[i] == EMonster.Phantom ? EMonster.Plague : EMonster.Phantom;
                Assert.AreEqual(6, FormationSave.ToSaved(changed, _db.DefaultFormation(null)).Count, "슬롯 " + (i + 1) + " 만 바뀜");
            }
        }

        [Test]
        public void ToSaved_6행은_슬롯_1에서_6_순서와_종_값명을_담는다()
        {
            List<FormationSlotEntry> rows = FormationSave.ToSaved(FormationTestKit.Mismatched(), _db.DefaultFormation(null));
            for (int i = 0; i < 6; ++i)
            {
                Assert.AreEqual(i + 1, rows[i].Slot);
                Assert.AreEqual(FormationTestKit.Mismatched()[i].ToString(), rows[i].Monster);
            }
        }

        [Test]
        public void ToSaved는_호출마다_새_리스트를_돌려준다()
        {
            List<FormationSlotEntry> a = FormationSave.ToSaved(_db.DefaultFormation(null), _db.DefaultFormation(null));
            List<FormationSlotEntry> b = FormationSave.ToSaved(_db.DefaultFormation(null), _db.DefaultFormation(null));
            Assert.AreNotSame(a, b);
            a.Add(new FormationSlotEntry());
            Assert.AreEqual(0, b.Count);
        }

        [Test]
        public void 다른_편성을_6행으로_저장한_뒤_원래_기본으로_되돌리면_0행이다()
        {
            List<FormationSlotEntry> changed = FormationSave.ToSaved(FormationTestKit.Mismatched(), _db.DefaultFormation(null));
            Assert.AreEqual(6, changed.Count);
            Assert.AreEqual(0, FormationSave.ToSaved(FormationSave.FromSaved(changed), FormationSave.FromSaved(changed)).Count);
            Assert.AreEqual(0, FormationSave.ToSaved(_db.DefaultFormation(null), _db.DefaultFormation(null)).Count);
        }

        [Test]
        public void ToSaved_결과는_FromSaved로_원래_편성을_복원한다()
        {
            CollectionAssert.AreEqual(
                FormationTestKit.Mismatched(),
                FormationSave.FromSaved(FormationSave.ToSaved(FormationTestKit.Mismatched(), _db.DefaultFormation(null))));
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

        //# --- IsMalformedSave ---

        [Test]
        public void IsMalformedSave는_null과_0행과_정상_6행에서_false다()
        {
            Assert.IsFalse(FormationSave.IsMalformedSave(null));
            Assert.IsFalse(FormationSave.IsMalformedSave(new List<FormationSlotEntry>()));
            Assert.IsFalse(FormationSave.IsMalformedSave(Rows(EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom)));
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
    }
}
