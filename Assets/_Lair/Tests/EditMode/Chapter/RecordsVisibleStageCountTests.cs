using System.Collections.Generic;
using Lair.Battle;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 기록 팝업 행 상한 — 월드맵(슬라이스 c) 전까지 챕터 1(5행)만 노출 (1221bca). 진행이 6~10 이어도 행은 늘지 않는다.
    public class RecordsVisibleStageCountTests : ChapterStaticStateTestBase
    {
        [Test]
        public void VisibleStageCount는_챕터당_스테이지_수_5다()
        {
            Assert.AreEqual(5, RecordsPopup.VisibleStageCount);
            Assert.AreEqual(StageProgress.StagesPerChapter, RecordsPopup.VisibleStageCount);
        }

        [Test]
        public void VisibleStageCount는_MaxStage_10보다_작다()
        {
            Assert.Less(RecordsPopup.VisibleStageCount, StageProgress.MaxStage);
        }

        [Test]
        public void 챕터수를_늘려도_행은_5개로_유지된다()
        {
            StageProgress.ConfigureChapterCount(3);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null);

            Assert.AreEqual(5, rows.Count);
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(10)]
        public void 클리어_진행이_챕터2여도_행은_5개다(int cleared)
        {
            MetaProfile p = new MetaProfile { ClearedStage = cleared };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(5, rows[rows.Count - 1].Stage);
        }

        [Test]
        public void 어떤_진행에서도_STAGE_6_이상_행은_없다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 10, SelectedStage = 10 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            foreach (RecordsStageCellData row in rows)
            {
                Assert.LessOrEqual(row.Stage, 5);
                StringAssert.DoesNotContain("STAGE 6", row.StageText);
                StringAssert.DoesNotContain("STAGE 10", row.StageText);
            }
        }

        [Test]
        public void 선택_스테이지가_6_이상이면_보이는_행_중_선택_표시는_없다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 10, SelectedStage = 7 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            foreach (RecordsStageCellData row in rows)
            {
                Assert.IsFalse(row.IsSelected, $"STAGE {row.Stage}");
            }
        }

        [Test]
        public void 챕터2_진행이어도_챕터1_행은_모두_해금이다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 7 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            foreach (RecordsStageCellData row in rows)
            {
                Assert.IsFalse(row.IsLocked, $"STAGE {row.Stage}");
            }
        }

        [Test]
        public void 범위_밖_전적_6_이상은_행에_나타나지_않아도_프로필에서_조회는_된다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 7 };
            p.RecordStageRun(7, true, 100f);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            Assert.AreEqual(5, rows.Count);
            Assert.AreEqual(1, p.GetStageRecord(7).Wins);
        }

        [Test]
        public void profile이_null이어도_5행이고_첫_행만_해금이다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(null, null);

            Assert.AreEqual(5, rows.Count);
            Assert.IsFalse(rows[0].IsLocked);
            Assert.IsTrue(rows[1].IsLocked);
        }

        [Test]
        public void 위협도_별은_챕터_내_5칸_기준이다()
        {
            Assert.AreEqual("★★★★★", RecordsPopup.BuildThreat(5));
            Assert.AreEqual("☆☆☆☆☆", RecordsPopup.BuildThreat(0));
            Assert.AreEqual("★★★★★", RecordsPopup.BuildThreat(10), "5 초과는 5 로 clamp");
        }
    }
}
