using System.Collections.Generic;
using Lair.Battle;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 기록 팝업 행 상한 — 슬라이스 (c) 에서 챕터 1 한정(1221bca)을 풀어 선형 스테이지 전체(MaxStage)로 확장. 행 라벨은 "{챕터}-{인덱스}".
    public class RecordsVisibleStageCountTests : ChapterStaticStateTestBase
    {
        private static string Text(int id)
        {
            switch (id)
            {
                case StageLabels.HintPreviousStageStringId: return "스테이지 {0}-{1} 클리어 필요";
                case StageLabels.HintChapterBossStringId: return "{0}장 보스 클리어 필요";
                default: return string.Empty;
            }
        }

        [Test]
        public void VisibleStageCount는_MaxStage와_같다()
        {
            Assert.AreEqual(StageProgress.MaxStage, RecordsPopup.VisibleStageCount);
            Assert.AreEqual(10, RecordsPopup.VisibleStageCount);
        }

        [Test]
        public void 챕터수를_늘리면_행도_따라_늘어난다()
        {
            StageProgress.ConfigureChapterCount(3);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile(), null);

            Assert.AreEqual(15, rows.Count);
        }

        [Test]
        public void 행_라벨은_챕터_인덱스_형식이고_위협도_별은_챕터_내_인덱스_기준이다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 10 }, null);

            Assert.AreEqual(10, rows.Count);
            Assert.AreEqual("STAGE 1-1", rows[0].StageText);
            Assert.AreEqual("STAGE 1-5", rows[4].StageText);
            Assert.AreEqual("STAGE 2-1", rows[5].StageText);
            Assert.AreEqual("STAGE 2-5", rows[9].StageText);
            Assert.AreEqual("★☆☆☆☆", rows[5].ThreatText, "2-1 은 인덱스 1 기준");
            Assert.AreEqual("★★★★★", rows[9].ThreatText);
        }

        [Test]
        public void 챕터2_첫_행의_잠금_안내는_1장_보스_클리어_필요다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 4 }, null, Text);

            Assert.IsTrue(rows[5].IsLocked);
            Assert.AreEqual("1장 보스 클리어 필요", rows[5].LockHintText);
            //# rows[6] = 2-2 → 직전 스테이지 2-1 안내(기획서 §6.4: 인덱스 > 1 이면 {챕터}-{인덱스-1}).
            Assert.AreEqual("스테이지 2-1 클리어 필요", RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 5 }, null, Text)[6].LockHintText);
        }

        [Test]
        public void 선택_스테이지가_챕터2면_해당_행에_선택_표시가_붙는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 10, SelectedStage = 7 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            Assert.IsTrue(rows[6].IsSelected);
            Assert.IsFalse(rows[0].IsSelected);
        }

        [Test]
        public void 챕터2_진행이어도_챕터1_행은_모두_해금이다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 7 };

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            for (int i = 0; i < 5; ++i)
            {
                Assert.IsFalse(rows[i].IsLocked, $"STAGE {rows[i].Stage}");
            }
        }

        [Test]
        public void 챕터2_전적은_행에_나타난다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 7 };
            p.RecordStageRun(7, true, 100f);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null);

            Assert.AreEqual("1승", rows[6].WinText);
        }

        [Test]
        public void profile이_null이어도_첫_행만_해금이다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(null, null);

            Assert.AreEqual(StageProgress.MaxStage, rows.Count);
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
