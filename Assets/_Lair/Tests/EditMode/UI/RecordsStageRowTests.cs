using System.Collections.Generic;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 기록 팝업 스테이지 행 조립 — 항상 5행 / 잠금 판정 / 승률 / 최단 표기 (spec §6.3).
    public class RecordsStageRowTests
    {
        [Test]
        public void 항상_스테이지_1부터_5까지_다섯_행이_나온다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null, null);

            Assert.AreEqual(5, rows.Count);
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i + 1, rows[i].Stage);
            }
        }

        [Test]
        public void 미클리어_프로필은_1스테이지만_해금이고_나머지는_잠금이다()
        {
            MetaProfile p = new MetaProfile();   //# ClearedStage = 0

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsFalse(rows[0].IsLocked);
            Assert.IsTrue(rows[1].IsLocked);
            Assert.IsTrue(rows[4].IsLocked);
        }

        [Test]
        public void 세_스테이지_클리어면_네번째까지_해금이다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 3 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsFalse(rows[3].IsLocked);   //# 스테이지 4
            Assert.IsTrue(rows[4].IsLocked);    //# 스테이지 5
        }

        [Test]
        public void 전부_클리어면_잠긴_행이_없다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 5 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            foreach (RecordsStageCellData row in rows)
            {
                Assert.IsFalse(row.IsLocked);
            }
        }

        [Test]
        public void 잠긴_행은_해금_조건_문구를_들고_전적_문구는_비어_있다()
        {
            MetaProfile p = new MetaProfile();   //# ClearedStage = 0 → 스테이지 3 잠금

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[2];

            Assert.AreEqual("스테이지 2 클리어 필요", row.LockHintText);
            Assert.IsEmpty(row.WinText);
            Assert.IsEmpty(row.RunRateText);
        }

        [Test]
        public void 해금_행은_승리수와_판수_승률을_문구로_만든다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 2 };
            for (int i = 0; i < 3; ++i)
                p.RecordStageRun(1, win: true, clearTime: 200f);
            p.RecordStageRun(1, win: false, clearTime: 300f);

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[0];

            Assert.AreEqual("3승", row.WinText);
            Assert.AreEqual("4판 · 75%", row.RunRateText);
        }

        [Test]
        public void 판수가_0이면_승률은_0퍼센트로_표기된다()
        {
            MetaProfile p = new MetaProfile();

            RecordsStageCellData row = RecordsPopup.BuildCellData(p, null, null)[0];

            Assert.AreEqual("0승", row.WinText);
            Assert.AreEqual("0판 · 0%", row.RunRateText);
        }

        [Test]
        public void 승률은_반올림된다()
        {
            MetaProfile p = new MetaProfile();
            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(1, win: true, clearTime: 100f);
            p.RecordStageRun(1, win: false, clearTime: 100f);

            //# 2/3 = 66.67% → 67%
            Assert.AreEqual("3판 · 67%", RecordsPopup.BuildCellData(p, null, null)[0].RunRateText);
        }

        [Test]
        public void 위협도는_스테이지_수만큼_별이_찬다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null, null);

            Assert.AreEqual("★☆☆☆☆", rows[0].ThreatText);
            Assert.AreEqual("★★★★★", rows[4].ThreatText);
        }

        [Test]
        public void 최단시간이_없으면_대시로_표기된다()
        {
            Assert.AreEqual("-", RecordsPopup.FormatClearTime(-1f));
        }

        [Test]
        public void 최단시간은_분초로_표기된다()
        {
            Assert.AreEqual("3:18", RecordsPopup.FormatClearTime(198.4f));
            Assert.AreEqual("0:07", RecordsPopup.FormatClearTime(7f));
        }

        [Test]
        public void 선택중_배지는_해금된_선택스테이지에만_붙는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 2, SelectedStage = 3 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, null);

            Assert.IsTrue(rows[2].IsSelected);
            Assert.IsFalse(rows[0].IsSelected);
        }

        [Test]
        public void 잠긴_스테이지가_선택중이어도_배지는_붙지_않는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 0, SelectedStage = 4 };

            Assert.IsFalse(RecordsPopup.BuildCellData(p, null, null)[3].IsSelected);
        }

        [Test]
        public void 상단총계_최단클리어는_분초로_표기되고_초글자가_없다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 4, TotalWins = 2, BestClearTime = 150f };

            string body = RecordsPopup.BuildBody(p);

            StringAssert.Contains("2:30", body);
            StringAssert.DoesNotContain("초", body);
        }

        [Test]
        public void 상단총계_최단기록이_없으면_대시로_표기된다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 0, TotalWins = 0, BestClearTime = -1f };

            string body = RecordsPopup.BuildBody(p);

            StringAssert.Contains("최단 클리어  -", body);
        }

        [Test]
        public void 프로필이_null이면_진행도_0으로_폴백하고_예외가_없다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(null, null, null);

            Assert.AreEqual(5, rows.Count);
            Assert.IsFalse(rows[0].IsLocked);
            Assert.IsTrue(rows[1].IsLocked);
        }
    }
}
