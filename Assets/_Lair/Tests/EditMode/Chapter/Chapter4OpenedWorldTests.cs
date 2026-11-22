using System;
using System.Collections.Generic;
using System.Reflection;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 챕터 4 출격 개방 후 월드맵·마을 캐러셀·쇼케이스·기록·마이그레이션·랭킹 범위 (chapter-4 §8·§8.1). Chapter3OpenedWorldTests 선례.
    //# 챕터 수 정적 상태는 ShippedDb()(로더처럼 ConfigureChapterCount) + ChapterStaticStateTestBase 가 격리·원복한다.
    public class Chapter4OpenedWorldTests : ChapterStaticStateTestBase
    {
        private static WorldMapViewModel NewVm(int cleared, int selected = 1, Action<int> onConfirm = null)
        {
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), new MetaProfile { ClearedStage = cleared, SelectedStage = selected }, WorldMapTestSupport.Text, onConfirm);
        }

        private static VillageViewModel NewVillage(int cleared)
        {
            return new VillageViewModel(new MetaProfile { ClearedStage = cleared }, null, WorldMapTestSupport.ShippedDb(), WorldMapTestSupport.Text);
        }

        //# ===== ChapterGate / MaxStage =====

        [Test]
        public void 챕터4는_출격_가능_범위에_포함되고_게이트_MaxStage_랭킹_상한이_일치한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.GreaterOrEqual(ChapterGate.PlayableChapterCount, 4);
            Assert.AreEqual(db.ChapterCount, ChapterGate.PlayableChapterCount);
            Assert.AreEqual(ChapterGate.PlayableStageCount, StageProgress.MaxStage);
            Assert.AreEqual(ChapterGate.PlayableStageCount, StageLeaderboard.MaxStage);
        }

        [TestCase(4, true)]
        [TestCase(16, true)]
        [TestCase(20, true)]
        public void 챕터4와_스테이지_16에서_20은_출격_가능하다(int value, bool expected)
        {
            bool isChapter = value <= 5;
            Assert.AreEqual(expected, isChapter ? ChapterGate.IsChapterPlayable(value) : ChapterGate.IsStagePlayable(value));
        }

        [TestCase(16, true)]
        [TestCase(20, true)]
        public void 랭킹_스테이지_챕터4_범위_16에서_20은_유효하다(int stage, bool expected)
        {
            WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(expected, StageLeaderboard.IsValidStage(stage));
        }

        [Test]
        public void 랭킹_문서_ID는_챕터4_선형_번호를_그대로_쓴다()
        {
            Assert.AreEqual("16_uid", StageLeaderboard.DocId(16, "uid"));
            Assert.AreEqual("20_uid", StageLeaderboard.DocId(20, "uid"));
        }

        //# ===== 월드맵 챕터 4 =====

        [Test]
        public void 챕터4_탭은_챕터3_보스를_클리어해야_열리고_열린다()
        {
            WorldMapViewModel locked = NewVm(14);
            WorldMapViewModel opened = NewVm(15);

            Assert.IsTrue(locked.Tabs[3].IsLocked);
            Assert.IsFalse(opened.Tabs[3].IsLocked);
            Assert.IsFalse(opened.Tabs[3].IsPlaceholder);
            Assert.AreEqual("4장 불꽃의 용광로", opened.Tabs[3].Label);
            Assert.AreEqual("광전사", opened.Tabs[3].HeroText);
            Assert.IsFalse(opened.Tabs[4].IsPlaceholder);
            Assert.AreEqual(5, opened.Tabs.Count);
        }

        [Test]
        public void 잠긴_챕터4_탭을_누르면_3장_보스_클리어_안내가_나오고_열리면_4_1로_이동한다()
        {
            WorldMapViewModel locked = NewVm(14);
            string notice = null;
            locked.OnNotice += message => notice = message;

            Assert.IsFalse(locked.SelectChapter(4));
            Assert.AreEqual("4장은 3장 보스를 클리어하면 열립니다", notice);

            WorldMapViewModel opened = NewVm(15, 99);
            Assert.AreEqual(4, opened.SelectedChapter, "범위 밖 저장값은 최대 플레이 가능(16 = 4-1)으로 보정");
            Assert.AreEqual("4-1", opened.DetailNumberText);
        }

        [TestCase(16, 1)]
        [TestCase(17, 2)]
        [TestCase(18, 3)]
        [TestCase(19, 4)]
        [TestCase(20, 5)]
        public void 챕터4_노드_상태는_클리어_진행도_따라_클리어_열림_잠금으로_나뉜다(int cleared, int clearedCount)
        {
            WorldMapViewModel vm = NewVm(cleared);
            vm.SelectChapter(4);

            for (int index = 1; index <= 5; ++index)
            {
                EWorldMapNodeState expected = index <= clearedCount
                    ? EWorldMapNodeState.Cleared
                    : (index == clearedCount + 1 ? EWorldMapNodeState.Open : EWorldMapNodeState.Locked);
                Assert.AreEqual(expected, vm.Nodes[index - 1].State, $"cleared {cleared} / 4-{index}");
                Assert.AreEqual(index == 5, vm.Nodes[index - 1].IsBoss);
            }
        }

        [Test]
        public void 챕터4_헤더와_상세는_챕터명_테마_광전사_스테이지명을_담는다()
        {
            WorldMapViewModel vm = NewVm(20);
            vm.SelectChapter(4);

            Assert.AreEqual("4장 불꽃의 용광로", vm.ChapterTitleText);
            Assert.AreEqual("테마: 용병단의 난입 / 영웅: 광전사", vm.ChapterThemeText);
            Assert.AreEqual("영웅: 광전사", vm.DetailHeroText);
            for (int index = 1; index <= 5; ++index)
            {
                Assert.IsTrue(vm.SelectNode(index));
                Assert.AreEqual(WorldMapTestSupport.Text(1130 + index), vm.DetailNameText, $"4-{index}");
                Assert.IsNotEmpty(vm.DetailNameText);
                Assert.AreEqual($"4-{index}", vm.DetailNumberText);
                Assert.AreEqual(StageLabels.Stars(index), vm.DetailStarsText);
            }
        }

        [Test]
        public void 챕터4_상세_규칙은_기획서_배정과_같고_보스_페이즈는_4_5만_두_건이다()
        {
            WorldMapViewModel vm = NewVm(20);
            vm.SelectChapter(4);
            string none = WorldMapTestSupport.Text(WorldMapViewModel.StrNoRules);

            vm.SelectNode(1);
            Assert.AreEqual(none, vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(2);
            StringAssert.Contains("봉인된 둥지", vm.DetailRulesText);
            StringAssert.Contains("60%", vm.DetailRulesText);

            vm.SelectNode(3);
            StringAssert.Contains("성수 샘", vm.DetailRulesText);

            vm.SelectNode(4);
            StringAssert.Contains("봉인된 둥지", vm.DetailRulesText);
            StringAssert.Contains("짙은 안개", vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(5);
            Assert.AreEqual(none, vm.DetailRulesText);
            Assert.IsTrue(vm.HasBossPhases);
            string[] lines = vm.DetailBossPhaseText.Split('\n');
            Assert.AreEqual(4, lines.Length, "규칙명+설명 x 2건");
            Assert.AreEqual("수호 결계", lines[0]);
            StringAssert.Contains("55%", lines[1]);
            StringAssert.Contains("35%", lines[3]);
        }

        [Test]
        public void 잠긴_챕터4_노드를_누르면_직전_스테이지_안내가_나오고_선택은_유지된다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(4);
            List<string> notices = new List<string>();
            vm.OnNotice += notices.Add;

            Assert.IsFalse(vm.SelectNode(2));
            Assert.IsFalse(vm.SelectNode(5));

            Assert.AreEqual("스테이지 4-1 클리어 필요", notices[0]);
            Assert.AreEqual("스테이지 4-4 클리어 필요", notices[1]);
            Assert.AreEqual(1, vm.SelectedStageIndex);
        }

        [Test]
        public void 챕터4_선택_확정은_선형_번호를_넘기고_마을_캐러셀이_4챕터_표기로_바뀐다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile { ClearedStage = 17, SelectedStage = 1 };
            VillageViewModel village = new VillageViewModel(profile, null, db, WorldMapTestSupport.Text);
            WorldMapViewModel vm = new WorldMapViewModel(db, profile, WorldMapTestSupport.Text, village.SelectStage);
            vm.SelectChapter(4);
            vm.SelectNode(2);

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(17, village.SelectedStage);
            Assert.AreEqual("STAGE 4-2", village.StageIndicatorText);
            Assert.AreEqual("광전사 · 2단계", village.IntruderText);
        }

        [TestCase(15, 99, 4, 1)]
        [TestCase(17, 99, 4, 3)]
        [TestCase(20, 99, 5, 1)]
        [TestCase(20, 0, 5, 1)]
        public void 저장된_선택이_범위를_벗어나면_해금_상한으로_보정되어_해금_상한_챕터로_시작한다(int cleared, int selected, int chapter, int index)
        {
            WorldMapViewModel vm = NewVm(cleared, selected);

            Assert.AreEqual(chapter, vm.SelectedChapter);
            Assert.AreEqual(index, vm.SelectedStageIndex);
        }

        //# ===== 마을 캐러셀 / 쇼케이스 =====

        [TestCase(15, 16)]
        [TestCase(19, 20)]
        [TestCase(20, 21)]
        [TestCase(99, 25)]
        public void 캐러셀_초기_위치는_최대_플레이_스테이지를_25로_clamp한다(int cleared, int expected)
        {
            Assert.AreEqual(expected, NewVillage(cleared).SelectedStage);
        }

        [Test]
        public void 캐러셀은_챕터_경계_15_16을_그대로_통과한다()
        {
            VillageViewModel vm = NewVillage(20);

            vm.SelectStage(15);
            vm.MoveStage(1);
            Assert.AreEqual(16, vm.SelectedStage);
            Assert.AreEqual("STAGE 4-1", vm.StageIndicatorText);
            vm.MoveStage(-1);
            Assert.AreEqual("STAGE 3-5", vm.StageIndicatorText);
            Assert.IsTrue(vm.CanMoveNext);
        }

        [TestCase(16, "STAGE 4-1", "★☆☆☆☆", "광전사 · 1단계")]
        [TestCase(18, "STAGE 4-3", "★★★☆☆", "광전사 · 3단계")]
        [TestCase(20, "STAGE 4-5", "★★★★★", "광전사 · 5단계")]
        [TestCase(15, "STAGE 3-5", "★★★★★", "암살자 · 5단계")]
        public void 캐러셀_표기는_선택_스테이지_챕터의_인디케이터_위협도_영웅이다(int stage, string indicator, string stars, string intruder)
        {
            VillageViewModel vm = NewVillage(20);

            vm.SelectStage(stage);

            Assert.AreEqual(indicator, vm.StageIndicatorText);
            Assert.AreEqual(stars, vm.StageThreatText);
            Assert.AreEqual(intruder, vm.IntruderText);
        }

        [Test]
        public void 캐러셀_챕터4_첫_스테이지의_잠금_안내는_3장_보스_클리어_필요다()
        {
            VillageViewModel vm = NewVillage(14);

            vm.SelectStage(16);

            Assert.AreEqual("3장 보스 클리어 필요", vm.StageLockHintText);
            Assert.IsFalse(vm.IsCurrentStageUnlocked);
        }

        private static EHero ShowcaseHeroOf(int linear)
        {
            MethodInfo mi = typeof(VillageController).GetMethod("ShowcaseHeroOf", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "VillageController.ShowcaseHeroOf 존재 - 시그니처 변경 감지");
            return (EHero)mi.Invoke(null, new object[] { linear });
        }

        [Test]
        public void 쇼케이스_영웅은_16에서_20이_광전사다()
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();

            for (int linear = 16; linear <= 20; ++linear)
            {
                Assert.AreEqual(EHero.Berserker, ShowcaseHeroOf(linear), $"linear {linear}");
            }
        }

        [Test]
        public void 광전사_프리팹이_존재하고_영웅_표시_이름은_챕터_데이터에서_온다()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/Berserker.prefab"));
            Assert.AreEqual("광전사", WorldMapTestSupport.Text(WorldMapTestSupport.ShippedDb().GetHero("Berserker").nameKey));
        }

        //# ===== 기록 팝업 20행 / 영웅 재스킨 =====

        [Test]
        public void 기록_행은_스테이지_수만큼이고_16에서_20행이_STAGE_4_인덱스_라벨과_정본_초상을_가진다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>("Assets/_Lair/Data/HeroStageVariantConfig.asset");
            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 20 }, config, WorldMapTestSupport.Text);

            Assert.AreEqual(ChapterGate.PlayableStageCount, RecordsPopup.VisibleStageCount);
            Assert.AreEqual(ChapterGate.PlayableStageCount, rows.Count);
            for (int index = 1; index <= 5; ++index)
            {
                RecordsStageCellData row = rows[15 + index - 1];
                Assert.AreEqual(15 + index, row.Stage);
                Assert.AreEqual($"STAGE 4-{index}", row.StageText);
                Assert.AreEqual(StageLabels.Stars(index), row.ThreatText);
                Assert.IsNotNull(row.Portrait, $"4-{index} 초상");
                Assert.IsFalse(row.IsLocked);
            }
        }

        [Test]
        public void 잠긴_기록_행_4_1의_안내는_3장_보스_클리어_필요이고_4_2는_직전_스테이지다()
        {
            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 10 }, null, WorldMapTestSupport.Text);

            Assert.AreEqual("3장 보스 클리어 필요", rows[15].LockHintText);
            Assert.AreEqual("스테이지 4-1 클리어 필요", rows[16].LockHintText);
        }

        //# ===== 마이그레이션 (v6 유지) =====

        [Test]
        public void v6_프로필의_Cleared_16에서_20은_보존된다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            foreach (int cleared in new[] { 16, 18, 20 })
            {
                MetaProfile p = new MetaProfile { Version = 7, ClearedStage = cleared, SelectedStage = cleared };
                MetaProfileMigration.Apply(p, db);
                Assert.AreEqual(14, p.Version);
                Assert.AreEqual(cleared, p.ClearedStage);
                Assert.AreEqual(cleared, p.SelectedStage);
            }
        }

        [Test]
        public void StageRecords는_스테이지_상한까지_남고_초과분은_제거되며_선택_영웅은_광전사가_된다()
        {
            MetaProfile p = new MetaProfile { Version = 7, ClearedStage = 17, SelectedStage = 17, SelectedHero = "Knight" };
            foreach (int stage in new[] { 1, 16, 20, 26 })
            {
                p.StageRecords.Add(new StageRecordEntry { Stage = stage, Runs = 1 });
            }

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            List<int> kept = new List<int>();
            foreach (StageRecordEntry entry in p.StageRecords)
            {
                kept.Add(entry.Stage);
            }
            CollectionAssert.AreEqual(new[] { 1, 16, 20 }, kept);
            Assert.AreEqual("Berserker", p.SelectedHero);
        }
    }
}
