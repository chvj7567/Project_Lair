using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 마을 캐러셀 상한/초기 위치(ChapterGate 단일 소유)와 기록 팝업 10행 라벨 (chapter-system §6.4·§2.3).
    //# 캐러셀 상한 = ChapterGate.PlayableStageCount — 값이 3챕터(15)로 올라도 관계식은 그대로 성립한다(상한 시나리오는 ClearedStage = PlayableStageCount 로 만든다).
    public class VillageCarouselAndRecordsLabelTests : ChapterStaticStateTestBase
    {
        private static VillageViewModel NewVillage(MetaProfile profile, bool withChapters = true)
        {
            return withChapters
                ? new VillageViewModel(profile, null, WorldMapTestSupport.ShippedDb(), WorldMapTestSupport.Text)
                : new VillageViewModel(profile, null);
        }

        //# --- 초기 위치 ---

        [TestCase(-5)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(9)]
        [TestCase(10)]
        [TestCase(11)]
        [TestCase(14)]
        [TestCase(15)]
        public void 초기_위치는_최대_플레이_스테이지를_출격_가능_상한으로_clamp한_값이다(int cleared)
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = cleared });
            int expected = System.Math.Min(ChapterGate.PlayableStageCount, StageProgress.MaxPlayableStage(cleared));

            Assert.AreEqual(expected, vm.SelectedStage);
        }

        [Test]
        public void 저장된_SelectedStage는_캐러셀_초기_위치에_영향을_주지_않는다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = 3, SelectedStage = 1 });

            Assert.AreEqual(System.Math.Min(ChapterGate.PlayableStageCount, 4), vm.SelectedStage);
        }

        [Test]
        public void 프로필이_null이면_진행도_0으로_취급해_1에서_시작한다()
        {
            VillageViewModel vm = NewVillage(null);

            Assert.AreEqual(1, vm.SelectedStage);
            Assert.AreEqual(0, vm.ClearedStage);
        }

        [Test]
        public void 클리어가_출격_상한을_넘어도_ClearedStage는_원값을_돌려준다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });

            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.ClearedStage);
            Assert.IsFalse(vm.CanMoveNext, "상한에서는 다음 불가");
        }

        //# --- 이동 ---

        [Test]
        public void SelectStage는_1_하한과_출격_가능_상한으로_clamp한다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });

            vm.SelectStage(0);
            Assert.AreEqual(1, vm.SelectedStage);
            vm.SelectStage(-9);
            Assert.AreEqual(1, vm.SelectedStage);
            vm.SelectStage(99);
            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage);
            vm.SelectStage(ChapterGate.PlayableStageCount + 1);
            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage);
        }

        [Test]
        public void SelectStage는_실제_변경일_때만_통지한다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });
            int changed = 0;
            vm.OnStageChanged += () => ++changed;

            vm.SelectStage(ChapterGate.PlayableStageCount);
            Assert.AreEqual(0, changed, "같은 위치");
            vm.SelectStage(2);
            Assert.AreEqual(1, changed);
            vm.SelectStage(2);
            Assert.AreEqual(1, changed);
            vm.SelectStage(99);
            Assert.AreEqual(2, changed, "clamp 결과가 달라지면 통지");
            vm.SelectStage(99);
            Assert.AreEqual(2, changed, "clamp 결과가 같으면 통지 없음");
        }

        [Test]
        public void MoveStage는_상한_하한에서_더_이동하지_않고_통지하지_않는다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });
            int changed = 0;
            vm.OnStageChanged += () => ++changed;

            vm.MoveStage(1);
            vm.MoveStage(5);
            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage);
            Assert.AreEqual(0, changed);

            vm.MoveStage(-1000);
            Assert.AreEqual(1, vm.SelectedStage);
            Assert.AreEqual(1, changed);
            vm.MoveStage(-1);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void 이동_가능_표시는_양끝에서_꺼진다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });

            Assert.IsFalse(vm.CanMoveNext);
            Assert.IsTrue(vm.CanMovePrev);

            vm.SelectStage(1);
            Assert.IsFalse(vm.CanMovePrev);
            Assert.IsTrue(vm.CanMoveNext);
        }

        //# --- 표기 ---

        [Test]
        public void 인디케이터와_위협도는_선택_스테이지의_챕터_인덱스_기준이다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount });

            for (int stage = 1; stage <= ChapterGate.PlayableStageCount; ++stage)
            {
                vm.SelectStage(stage);

                Assert.AreEqual($"STAGE {StageProgress.ChapterOf(stage)}-{StageProgress.IndexOf(stage)}", vm.StageIndicatorText);
                Assert.AreEqual(StageLabels.Stars(StageProgress.IndexOf(stage)), vm.StageThreatText);
                Assert.AreEqual(StageProgress.IndexOf(stage), vm.SelectedStageIndex);
            }
        }

        [TestCase(0, 2, "스테이지 1-1 클리어 필요")]
        [TestCase(0, 5, "스테이지 1-4 클리어 필요")]
        [TestCase(2, 5, "스테이지 1-4 클리어 필요")]
        public void 잠금_안내는_직전_스테이지의_챕터_인덱스를_가리킨다(int cleared, int stage, string expected)
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = cleared });

            vm.SelectStage(stage);

            Assert.AreEqual(expected, vm.StageLockHintText);
        }

        [Test]
        public void 문구_함수를_주지_않으면_잠금_안내는_빈_문자열이다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = 0 }, withChapters: false);

            vm.SelectStage(3);

            Assert.AreEqual(string.Empty, vm.StageLockHintText);
        }

        [Test]
        public void 침입자_문구는_챕터_영웅명과_챕터_내_단계다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = 10 });

            vm.SelectStage(4);

            Assert.AreEqual($"{WorldMapTestSupport.Text(WorldMapTestSupport.ShippedDb().GetHeroOfLinear(4).nameKey)} · 4단계", vm.IntruderText);
        }

        [Test]
        public void 챕터_데이터가_없으면_침입자_영웅명은_프로필_영웅_폴백이다()
        {
            VillageViewModel vm = NewVillage(new MetaProfile { ClearedStage = 10, SelectedHero = "Knight" }, withChapters: false);

            vm.SelectStage(3);

            Assert.AreEqual($"{VillageViewModel.HeroDisplayName("Knight")} · 3단계", vm.IntruderText);
        }

        [Test]
        public void 전적_패널은_선택_스테이지의_선형_번호_기록을_읽는다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 10 };
            p.RecordStageRun(3, true, 100f);
            VillageViewModel vm = NewVillage(p);

            vm.SelectStage(3);

            Assert.AreEqual("1:40.0", vm.BestDefenseText);
            vm.SelectStage(2);
            Assert.AreEqual("-", vm.BestDefenseText);
        }

        //# --- 기록 팝업 10행 라벨 ---

        private static string ExpectedHint(int linear)
        {
            int chapter = StageProgress.ChapterOf(linear);
            int index = StageProgress.IndexOf(linear);
            return index > 1 ? $"스테이지 {chapter}-{index - 1} 클리어 필요" : $"{chapter - 1}장 보스 클리어 필요";
        }

        [Test]
        public void 기록_행은_10개이고_모든_행의_라벨과_위협도가_챕터_인덱스_기준이다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 10 }, null, WorldMapTestSupport.Text);

            Assert.AreEqual(10, rows.Count);
            for (int i = 0; i < rows.Count; ++i)
            {
                int linear = i + 1;
                Assert.AreEqual(linear, rows[i].Stage);
                Assert.AreEqual($"STAGE {StageProgress.ChapterOf(linear)}-{StageProgress.IndexOf(linear)}", rows[i].StageText);
                Assert.AreEqual(StageLabels.Stars(StageProgress.IndexOf(linear)), rows[i].ThreatText);
            }
        }

        [TestCase(0)]
        [TestCase(3)]
        [TestCase(5)]
        [TestCase(7)]
        public void 잠긴_행의_안내는_직전_스테이지나_직전_챕터_보스를_가리키고_해금_행은_안내가_비어_있다(int cleared)
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = cleared }, null, WorldMapTestSupport.Text);

            foreach (RecordsStageCellData row in rows)
            {
                if (row.Stage <= cleared + 1)
                {
                    Assert.IsFalse(row.IsLocked, $"행 {row.Stage}");
                    Assert.AreEqual(string.Empty, row.LockHintText, $"행 {row.Stage}");
                }
                else
                {
                    Assert.IsTrue(row.IsLocked, $"행 {row.Stage}");
                    Assert.AreEqual(ExpectedHint(row.Stage), row.LockHintText, $"행 {row.Stage}");
                }
            }
        }

        [Test]
        public void 잠긴_행은_전적_문구가_비고_해금_행은_전적_문구가_있다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 2 }, null, WorldMapTestSupport.Text);

            Assert.IsNotEmpty(rows[0].WinText);
            Assert.IsNotEmpty(rows[2].WinText, "cleared+1 = 3 은 해금");
            Assert.AreEqual(string.Empty, rows[3].WinText);
            Assert.AreEqual(string.Empty, rows[3].RunRateText);
            Assert.AreEqual(string.Empty, rows[3].BestText);
        }

        [Test]
        public void 챕터2_행의_전적은_선형_번호로_조회된다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 10 };
            p.RecordStageRun(8, true, 150f);
            p.RecordStageRun(8, false, 0f);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, WorldMapTestSupport.Text);

            Assert.AreEqual("1승", rows[7].WinText);
            Assert.AreEqual("2판 · 50%", rows[7].RunRateText);
            Assert.AreEqual("최단 2:30", rows[7].BestText);
            Assert.AreEqual("0승", rows[2].WinText);
        }

        [Test]
        public void 잠금_행의_초상은_어둡게_처리되고_해금_행은_원색이다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 1 }, null, WorldMapTestSupport.Text);

            Assert.AreEqual(UnityEngine.Color.white, rows[0].PortraitTint);
            Assert.AreNotEqual(UnityEngine.Color.white, rows[9].PortraitTint);
        }
    }
}
