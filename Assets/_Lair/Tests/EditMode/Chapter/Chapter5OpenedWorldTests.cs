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
    //# 챕터 5 출격 개방 후(ChapterGate 5) 월드맵·마을 캐러셀·쇼케이스·기록·마이그레이션·랭킹 범위 (chapter-5 §8·§8.1). Chapter4OpenedWorldTests 선례.
    //# 챕터 수 정적 상태는 ShippedDb()(로더처럼 ConfigureChapterCount) + ChapterStaticStateTestBase 가 격리·원복한다.
    public class Chapter5OpenedWorldTests : ChapterStaticStateTestBase
    {
        private const string VariantAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";

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
        public void 출격_가능_챕터는_5이고_스테이지는_25이며_MaxStage와_Chapter_json_행수와_같다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(5, ChapterGate.PlayableChapterCount);
            Assert.AreEqual(25, ChapterGate.PlayableStageCount);
            Assert.AreEqual(db.ChapterCount, ChapterGate.PlayableChapterCount);
            Assert.AreEqual(25, StageProgress.MaxStage);
            Assert.AreEqual(25, StageLeaderboard.MaxStage);
        }

        [TestCase(0, false)]
        [TestCase(5, true)]
        [TestCase(6, false)]
        public void 챕터_5까지만_출격_가능하다(int chapter, bool expected)
        {
            Assert.AreEqual(expected, ChapterGate.IsChapterPlayable(chapter));
        }

        [TestCase(21, true)]
        [TestCase(25, true)]
        [TestCase(26, false)]
        public void 스테이지_21에서_25는_출격_가능하고_그_다음은_불가다(int linear, bool expected)
        {
            Assert.AreEqual(expected, ChapterGate.IsStagePlayable(linear));
        }

        [TestCase(21, true)]
        [TestCase(25, true)]
        [TestCase(26, false)]
        public void 랭킹_스테이지_유효_범위는_1에서_25이다(int stage, bool expected)
        {
            WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(expected, StageLeaderboard.IsValidStage(stage));
        }

        [Test]
        public void 랭킹_문서_ID는_챕터5_선형_번호를_그대로_쓴다()
        {
            Assert.AreEqual("21_uid", StageLeaderboard.DocId(21, "uid"));
            Assert.AreEqual("25_uid", StageLeaderboard.DocId(25, "uid"));
        }

        //# ===== 월드맵 챕터 5 =====

        [Test]
        public void 챕터5_탭은_챕터4_보스를_클리어해야_열리고_자리표시자_탭은_하나도_없다()
        {
            WorldMapViewModel locked = NewVm(19);
            WorldMapViewModel opened = NewVm(20);

            Assert.IsTrue(locked.Tabs[4].IsLocked);
            Assert.IsFalse(opened.Tabs[4].IsLocked);
            Assert.AreEqual("5장 서리 대성당", opened.Tabs[4].Label);
            Assert.AreEqual("성기사", opened.Tabs[4].HeroText);
            Assert.AreEqual(WorldMapViewModel.ChapterSlotCount, opened.Tabs.Count);
            Assert.AreEqual(5, WorldMapViewModel.ChapterSlotCount);
            foreach (WorldMapTabData tab in opened.Tabs)
            {
                Assert.IsFalse(tab.IsPlaceholder, $"{tab.ChapterId}장");
            }
        }

        [Test]
        public void 잠긴_챕터5_탭을_누르면_4장_보스_클리어_안내가_나오고_열리면_5_1로_이동한다()
        {
            WorldMapViewModel locked = NewVm(19);
            string notice = null;
            locked.OnNotice += message => notice = message;

            Assert.IsFalse(locked.SelectChapter(5));
            Assert.AreEqual("5장은 4장 보스를 클리어하면 열립니다", notice);

            WorldMapViewModel opened = NewVm(20, 99);
            Assert.AreEqual(5, opened.SelectedChapter, "범위 밖 저장값은 최대 플레이 가능(21 = 5-1)으로 보정");
            Assert.AreEqual("5-1", opened.DetailNumberText);
        }

        [TestCase(21, 1)]
        [TestCase(22, 2)]
        [TestCase(23, 3)]
        [TestCase(24, 4)]
        [TestCase(25, 5)]
        public void 챕터5_노드_상태는_클리어_진행도_따라_클리어_열림_잠금으로_나뉜다(int cleared, int clearedCount)
        {
            WorldMapViewModel vm = NewVm(cleared);
            vm.SelectChapter(5);

            Assert.AreEqual(5, vm.Nodes.Count, "노드는 챕터당 5개");
            for (int index = 1; index <= 5; ++index)
            {
                EWorldMapNodeState expected = index <= clearedCount
                    ? EWorldMapNodeState.Cleared
                    : (index == clearedCount + 1 ? EWorldMapNodeState.Open : EWorldMapNodeState.Locked);
                Assert.AreEqual(expected, vm.Nodes[index - 1].State, $"cleared {cleared} / 5-{index}");
                Assert.AreEqual(index == 5, vm.Nodes[index - 1].IsBoss);
            }
        }

        [Test]
        public void 챕터5_헤더와_상세는_챕터명_테마_성기사_스테이지명을_담는다()
        {
            WorldMapViewModel vm = NewVm(25);
            vm.SelectChapter(5);

            Assert.AreEqual("5장 서리 대성당", vm.ChapterTitleText);
            Assert.AreEqual("테마: 성기사단의 정화 / 영웅: 성기사", vm.ChapterThemeText);
            Assert.AreEqual("영웅: 성기사", vm.DetailHeroText);
            for (int index = 1; index <= 5; ++index)
            {
                Assert.IsTrue(vm.SelectNode(index));
                Assert.AreEqual(WorldMapTestSupport.Text(1140 + index), vm.DetailNameText, $"5-{index}");
                Assert.IsNotEmpty(vm.DetailNameText);
                Assert.AreEqual($"5-{index}", vm.DetailNumberText);
                Assert.AreEqual(StageLabels.Stars(index), vm.DetailStarsText);
            }
        }

        [Test]
        public void 챕터5_상세_규칙은_기획서_배정과_같고_보스_페이즈는_5_5만_두_건이다()
        {
            WorldMapViewModel vm = NewVm(25);
            vm.SelectChapter(5);
            string none = WorldMapTestSupport.Text(WorldMapViewModel.StrNoRules);

            vm.SelectNode(1);
            Assert.AreEqual(none, vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(2);
            StringAssert.Contains("봉인된 둥지", vm.DetailRulesText);
            StringAssert.Contains("60%", vm.DetailRulesText);

            vm.SelectNode(3);
            StringAssert.Contains("짙은 안개", vm.DetailRulesText);

            vm.SelectNode(4);
            StringAssert.Contains("성수 샘", vm.DetailRulesText);
            StringAssert.Contains("봉인된 둥지", vm.DetailRulesText);
            StringAssert.Contains("55%", vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(5);
            Assert.AreEqual(none, vm.DetailRulesText);
            Assert.IsTrue(vm.HasBossPhases);
            string[] lines = vm.DetailBossPhaseText.Split('\n');
            Assert.AreEqual(4, lines.Length, "규칙명+설명 x 2건");
            Assert.AreEqual("덫", lines[0]);
            StringAssert.Contains("75%", lines[1]);
            Assert.AreEqual("수호 결계", lines[2]);
            StringAssert.Contains("35%", lines[3]);
        }

        [Test]
        public void 잠긴_챕터5_노드를_누르면_직전_스테이지_안내가_나오고_선택은_유지된다()
        {
            WorldMapViewModel vm = NewVm(20);
            vm.SelectChapter(5);
            List<string> notices = new List<string>();
            vm.OnNotice += notices.Add;

            Assert.IsFalse(vm.SelectNode(2));
            Assert.IsFalse(vm.SelectNode(5));

            Assert.AreEqual("스테이지 5-1 클리어 필요", notices[0]);
            Assert.AreEqual("스테이지 5-4 클리어 필요", notices[1]);
            Assert.AreEqual(1, vm.SelectedStageIndex);
        }

        [Test]
        public void 챕터5_선택_확정은_선형_번호를_넘기고_마을_캐러셀이_5챕터_표기로_바뀐다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile { ClearedStage = 22, SelectedStage = 1 };
            VillageViewModel village = new VillageViewModel(profile, null, db, WorldMapTestSupport.Text);
            WorldMapViewModel vm = new WorldMapViewModel(db, profile, WorldMapTestSupport.Text, village.SelectStage);
            vm.SelectChapter(5);
            vm.SelectNode(2);

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(22, village.SelectedStage);
            Assert.AreEqual("STAGE 5-2", village.StageIndicatorText);
            Assert.AreEqual("성기사 · 2단계", village.IntruderText);
        }

        [TestCase(20, 99, 5, 1)]
        [TestCase(22, 99, 5, 3)]
        [TestCase(25, 99, 5, 5)]
        [TestCase(25, 0, 5, 5)]
        public void 저장된_선택이_범위를_벗어나면_해금_상한으로_보정되어_챕터5로_시작한다(int cleared, int selected, int chapter, int index)
        {
            WorldMapViewModel vm = NewVm(cleared, selected);

            Assert.AreEqual(chapter, vm.SelectedChapter);
            Assert.AreEqual(index, vm.SelectedStageIndex);
        }

        //# ===== 마지막 챕터 종점 (5-5 클리어 후 다음 챕터 없음) =====

        [Test]
        public void 전_클리어_상태에서도_5_5에_머문다()
        {
            WorldMapViewModel vm = NewVm(25, 99);
            VillageViewModel village = NewVillage(25);

            Assert.AreEqual(5, vm.SelectedChapter);
            Assert.AreEqual("5-5", vm.DetailNumberText);
            Assert.IsFalse(vm.DetailLocked);
            Assert.AreEqual(25, village.SelectedStage);
            Assert.IsFalse(village.CanMoveNext);
            Assert.IsTrue(village.IsCurrentStageUnlocked);
            Assert.AreEqual("스테이지 5-4 클리어 필요", village.StageLockHintText, "안내는 항상 직전 선행 조건이며 해금 상태에서는 View 가 숨긴다");
        }

        [Test]
        public void 어떤_잠금_안내도_존재하지_않는_6장을_가리키지_않는다()
        {
            List<string> messages = new List<string>();
            for (int chapter = 1; chapter <= 5; ++chapter)
            {
                WorldMapViewModel locked = NewVm(0);
                locked.OnNotice += messages.Add;
                locked.SelectChapter(chapter);
                for (int index = 1; index <= 5; ++index)
                {
                    locked.SelectNode(index);
                }
            }
            for (int stage = 1; stage <= 25; ++stage)
            {
                VillageViewModel fresh = NewVillage(0);
                fresh.SelectStage(stage);
                messages.Add(fresh.StageLockHintText);
                messages.Add(StageLabels.UnlockHint(stage, WorldMapTestSupport.Text));
            }

            Assert.IsNotEmpty(messages);
            foreach (string message in messages)
            {
                StringAssert.DoesNotContain("6장", message);
            }
        }

        //# ===== 마을 캐러셀 25 / 쇼케이스 =====

        [TestCase(20, 21)]
        [TestCase(24, 25)]
        [TestCase(25, 25)]
        [TestCase(99, 25)]
        public void 캐러셀_초기_위치는_최대_플레이_스테이지를_25로_clamp한다(int cleared, int expected)
        {
            Assert.AreEqual(expected, NewVillage(cleared).SelectedStage);
        }

        [Test]
        public void 캐러셀은_25에서_더_이동하지_않고_챕터_경계_20_21을_그대로_통과한다()
        {
            VillageViewModel vm = NewVillage(25);
            Assert.IsFalse(vm.CanMoveNext);
            vm.MoveStage(1);
            Assert.AreEqual(25, vm.SelectedStage);

            vm.SelectStage(20);
            vm.MoveStage(1);
            Assert.AreEqual(21, vm.SelectedStage);
            Assert.AreEqual("STAGE 5-1", vm.StageIndicatorText);
            vm.MoveStage(-1);
            Assert.AreEqual("STAGE 4-5", vm.StageIndicatorText);
            Assert.IsTrue(vm.CanMoveNext);
        }

        [TestCase(21, "STAGE 5-1", "★☆☆☆☆", "성기사 · 1단계")]
        [TestCase(23, "STAGE 5-3", "★★★☆☆", "성기사 · 3단계")]
        [TestCase(25, "STAGE 5-5", "★★★★★", "성기사 · 5단계")]
        [TestCase(20, "STAGE 4-5", "★★★★★", "광전사 · 5단계")]
        public void 캐러셀_표기는_선택_스테이지_챕터의_인디케이터_위협도_영웅이다(int stage, string indicator, string stars, string intruder)
        {
            VillageViewModel vm = NewVillage(25);

            vm.SelectStage(stage);

            Assert.AreEqual(indicator, vm.StageIndicatorText);
            Assert.AreEqual(stars, vm.StageThreatText);
            Assert.AreEqual(intruder, vm.IntruderText);
        }

        [Test]
        public void 캐러셀_챕터5_첫_스테이지의_잠금_안내는_4장_보스_클리어_필요다()
        {
            VillageViewModel vm = NewVillage(19);

            vm.SelectStage(21);

            Assert.AreEqual("4장 보스 클리어 필요", vm.StageLockHintText);
            Assert.IsFalse(vm.IsCurrentStageUnlocked);
        }

        private static EHero ShowcaseHeroOf(int linear)
        {
            MethodInfo mi = typeof(VillageController).GetMethod("ShowcaseHeroOf", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "VillageController.ShowcaseHeroOf 존재 - 시그니처 변경 감지");
            return (EHero)mi.Invoke(null, new object[] { linear });
        }

        [Test]
        public void 쇼케이스_영웅은_21에서_25가_성기사이고_26_이상은_기사로_폴백한다()
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();

            for (int linear = 21; linear <= 25; ++linear)
            {
                Assert.AreEqual(EHero.Paladin, ShowcaseHeroOf(linear), $"linear {linear}");
            }
            Assert.AreEqual(EHero.Berserker, ShowcaseHeroOf(20));
            Assert.AreEqual(EHero.Knight, ShowcaseHeroOf(26));
        }

        [Test]
        public void 성기사_프리팹이_존재하고_영웅_표시_이름은_챕터_데이터에서_온다()
        {
            Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Lair/Art/Characters/Paladin.prefab"));
            Assert.AreEqual("성기사", WorldMapTestSupport.Text(WorldMapTestSupport.ShippedDb().GetHero("Paladin").nameKey));
        }

        //# ===== 기록 팝업 25행 / 영웅 재스킨 =====

        [Test]
        public void 기록_행은_25개이고_21에서_25행이_STAGE_5_인덱스_라벨과_정본_초상을_가진다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 25 }, config, WorldMapTestSupport.Text);

            Assert.AreEqual(25, RecordsPopup.VisibleStageCount);
            Assert.AreEqual(25, rows.Count);
            for (int index = 1; index <= 5; ++index)
            {
                RecordsStageCellData row = rows[20 + index - 1];
                Assert.AreEqual(20 + index, row.Stage);
                Assert.AreEqual($"STAGE 5-{index}", row.StageText);
                Assert.AreEqual(StageLabels.Stars(index), row.ThreatText);
                Assert.IsNotNull(row.Portrait, $"5-{index} 초상");
                Assert.IsFalse(row.IsLocked);
            }
        }

        [Test]
        public void 잠긴_기록_행_5_1의_안내는_4장_보스_클리어_필요이고_5_2는_직전_스테이지다()
        {
            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 10 }, null, WorldMapTestSupport.Text);

            Assert.AreEqual("4장 보스 클리어 필요", rows[20].LockHintText);
            Assert.AreEqual("스테이지 5-1 클리어 필요", rows[21].LockHintText);
        }

        [Test]
        public void 영웅_재스킨_정본은_25엔트리이고_모든_스테이지에_초상이_있다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);

            Assert.AreEqual(25, config.Stages.Count);
            for (int linear = 1; linear <= 25; ++linear)
            {
                Assert.IsNotNull(config.GetStage(linear).Portrait, $"linear {linear} 초상");
            }
            for (int linear = 21; linear <= 25; ++linear)
            {
                HeroStageVariant variant = config.GetStage(linear);
                Assert.AreEqual(5, variant.ChapterId, $"linear {linear}");
                Assert.AreEqual(linear - 20, variant.StageIndex, $"linear {linear}");
                Assert.AreEqual(linear - 21, variant.Tier, $"linear {linear} Tier");
            }
        }

        [Test]
        public void 성기사_외형은_기획서_3_5_발광_스케일_값이다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            bool[] expectedEmission = { false, false, true, true, true };
            float[] expectedIntensity = { 0f, 0f, 1.5f, 2.2f, 3.2f };

            for (int i = 0; i < 5; ++i)
            {
                HeroStageVariant v = config.GetStage(5, i + 1);
                Assert.AreEqual(expectedEmission[i], v.UseEmission, $"5-{i + 1} UseEmission");
                Assert.AreEqual(expectedIntensity[i], v.EmissionIntensity, 0.0001f, $"5-{i + 1} EmissionIntensity");
                Assert.AreEqual(i == 4 ? 1.3f : 1f, v.ScaleMultiplier, 0.0001f, $"5-{i + 1} ScaleMultiplier");
            }
        }

        //# ===== 마이그레이션 (v6 유지) =====

        [Test]
        public void v6_프로필의_Cleared_21에서_25는_보존되고_26_이상은_25로_잘린다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            foreach (int cleared in new[] { 21, 23, 25 })
            {
                MetaProfile p = new MetaProfile { Version = 7, ClearedStage = cleared, SelectedStage = cleared };
                MetaProfileMigration.Apply(p, db);
                Assert.AreEqual(8, p.Version);
                Assert.AreEqual(cleared, p.ClearedStage);
                Assert.AreEqual(cleared, p.SelectedStage);
            }

            MetaProfile over = new MetaProfile { Version = 7, ClearedStage = 99, SelectedStage = 99 };
            MetaProfileMigration.Apply(over, db);
            Assert.AreEqual(25, over.ClearedStage);
            Assert.AreEqual(25, over.SelectedStage);
        }

        [Test]
        public void StageRecords는_25까지_남고_26부터_제거되며_선택_영웅은_성기사가_된다()
        {
            MetaProfile p = new MetaProfile { Version = 7, ClearedStage = 22, SelectedStage = 22, SelectedHero = "Knight" };
            foreach (int stage in new[] { 1, 21, 25, 26, 30 })
            {
                p.StageRecords.Add(new StageRecordEntry { Stage = stage, Runs = 1 });
            }

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            List<int> kept = new List<int>();
            foreach (StageRecordEntry entry in p.StageRecords)
            {
                kept.Add(entry.Stage);
            }
            CollectionAssert.AreEqual(new[] { 1, 21, 25 }, kept);
            Assert.AreEqual("Paladin", p.SelectedHero);
        }
    }
}
