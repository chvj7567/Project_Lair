using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# WorldMapViewModel 본격 검증 — 탭/노드 상태 분류, 초기 선택 보정, 선택·확정 이벤트, 출격 불가 게이트, 상세 문구 (chapter-system §6·§9).
    //# 스모크(WorldMapViewModelTests)와 겹치지 않는 경계·이벤트·null 경로 위주.
    public class WorldMapViewModelBehaviorTests : ChapterStaticStateTestBase
    {
        private static WorldMapViewModel NewVm(MetaProfile profile, Action<int> onConfirm = null)
        {
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), profile, WorldMapTestSupport.Text, onConfirm);
        }

        private static MetaProfile Profile(int cleared, int selected = 1)
        {
            return new MetaProfile { ClearedStage = cleared, SelectedStage = selected };
        }

        //# --- 생성자 ---

        [Test]
        public void 챕터DB가_null이면_ArgumentNullException이다()
        {
            Assert.Throws<ArgumentNullException>(() => new WorldMapViewModel(null, new MetaProfile(), WorldMapTestSupport.Text, null));
        }

        [Test]
        public void 프로필이_null이면_ArgumentNullException이다()
        {
            Assert.Throws<ArgumentNullException>(() => new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), null, WorldMapTestSupport.Text, null));
        }

        [Test]
        public void 문구_함수가_null이어도_생성되고_문구는_빈_문자열이다()
        {
            WorldMapViewModel vm = new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), new MetaProfile(), null, null);

            Assert.AreEqual(string.Empty, vm.ChapterTitleText.Trim());
            Assert.AreEqual(1, vm.SelectedChapter);
        }

        //# --- 초기 선택 보정 ---

        [TestCase(0, 1, 1, 1)]
        [TestCase(0, 0, 1, 1)]
        [TestCase(0, -3, 1, 1)]
        [TestCase(3, 4, 1, 4)]
        [TestCase(3, 5, 1, 4)]
        [TestCase(3, 99, 1, 4)]
        [TestCase(4, 5, 1, 5)]
        [TestCase(5, 5, 1, 5)]
        [TestCase(5, 6, 2, 1)]
        [TestCase(10, 7, 2, 2)]
        [TestCase(10, 10, 2, 5)]
        [TestCase(10, 11, 2, 5)]
        [TestCase(7, 10, 2, 3)]
        public void 저장된_선택이_잠금이면_해금된_최대_플레이_스테이지로_보정되고_해금_범위_안이면_챕터2도_유지된다(int cleared, int selected, int expectedChapter, int expectedIndex)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared, selected));

            Assert.AreEqual(expectedChapter, vm.SelectedChapter);
            Assert.AreEqual(expectedIndex, vm.SelectedStageIndex);
            Assert.AreEqual(StageProgress.ToLinear(expectedChapter, expectedIndex), vm.SelectedLinear);
        }

        [Test]
        public void 선택이_가능한_저장값은_그대로_유지된다()
        {
            WorldMapViewModel vm = NewVm(Profile(4, 3));

            Assert.AreEqual(3, vm.SelectedStageIndex);
        }

        [Test]
        public void 생성은_프로필을_수정하지_않는다()
        {
            MetaProfile p = Profile(0, 9);

            NewVm(p);

            Assert.AreEqual(9, p.SelectedStage, "보정은 VM 내부 상태일 뿐 저장은 호출자 책임");
            Assert.AreEqual(0, p.ClearedStage);
        }

        //# --- 탭 ---

        [TestCase(0)]
        [TestCase(4)]
        public void 챕터2_탭은_챕터1_보스를_클리어하기_전에는_잠금_표기로_잠겨_있다(int cleared)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared));

            Assert.IsTrue(vm.Tabs[1].IsLocked);
            Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrLocked), vm.Tabs[1].LockTagText);
            Assert.IsFalse(vm.Tabs[1].IsPlaceholder);
            Assert.IsFalse(vm.Tabs[1].IsSelected);
        }

        [TestCase(5)]
        [TestCase(10)]
        public void 챕터2_탭은_챕터1_보스를_클리어하면_열리고_잠금_표기가_없다(int cleared)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared));

            Assert.IsFalse(vm.Tabs[1].IsLocked);
            Assert.AreEqual(string.Empty, vm.Tabs[1].LockTagText);
            Assert.IsFalse(vm.Tabs[1].IsPlaceholder);
            Assert.IsFalse(vm.Tabs[1].IsSelected, "초기 선택이 챕터 1 이면 챕터 2 탭은 선택되지 않는다");
        }

        [Test]
        public void 챕터1_탭은_열려_있고_선택되어_있으며_잠금_표기가_없다()
        {
            WorldMapViewModel vm = NewVm(Profile(0));

            Assert.IsFalse(vm.Tabs[0].IsLocked);
            Assert.IsTrue(vm.Tabs[0].IsSelected);
            Assert.AreEqual(string.Empty, vm.Tabs[0].LockTagText);
            Assert.AreEqual(1, vm.Tabs[0].ChapterId);
        }

        [Test]
        public void 챕터_데이터가_없는_슬롯_4_5는_자리표시자이고_장_번호_물음표_라벨이다()
        {
            WorldMapViewModel vm = NewVm(Profile(10));

            for (int slot = 4; slot <= 5; ++slot)
            {
                WorldMapTabData tab = vm.Tabs[slot - 1];
                Assert.IsTrue(tab.IsPlaceholder, $"{slot}장");
                Assert.IsTrue(tab.IsLocked);
                Assert.IsFalse(tab.IsSelected);
                Assert.AreEqual(slot, tab.ChapterId);
                Assert.AreEqual($"{slot}장 ???", tab.Label);
                Assert.AreEqual(string.Empty, tab.LockTagText);
            }
        }

        [Test]
        public void 챕터3_탭은_데이터가_있어도_출격_게이트가_닫혀_있어_준비_중으로_잠겨_있다()
        {
            WorldMapViewModel vm = NewVm(Profile(10));

            WorldMapTabData tab = vm.Tabs[2];
            Assert.IsFalse(tab.IsPlaceholder);
            Assert.IsTrue(tab.IsLocked);
            Assert.IsFalse(tab.IsSelected);
            Assert.AreEqual("3장 그림자 성채", tab.Label);
            Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrNotReady), tab.LockTagText);
        }

        [Test]
        public void 챕터3_탭을_눌러도_출격_게이트가_닫혀_있어_이동하지_않는다()
        {
            WorldMapViewModel vm = NewVm(Profile(10));

            Assert.IsFalse(vm.SelectChapter(3));
            Assert.AreEqual(1, vm.SelectedChapter);
        }

        [Test]
        public void 탭은_항상_5칸이고_선택된_탭은_정확히_하나다()
        {
            foreach (int cleared in new[] { 0, 3, 5, 10 })
            {
                WorldMapViewModel vm = NewVm(Profile(cleared));
                int selected = 0;
                foreach (WorldMapTabData tab in vm.Tabs)
                {
                    if (tab.IsSelected)
                    {
                        ++selected;
                    }
                }

                Assert.AreEqual(WorldMapViewModel.ChapterSlotCount, vm.Tabs.Count);
                Assert.AreEqual(1, selected, $"cleared {cleared}");
            }
        }

        [TestCase(0)]
        [TestCase(99)]
        [TestCase(-1)]
        [TestCase(3)]
        [TestCase(5)]
        public void 챕터_DB에_없는_탭을_누르면_이동도_안내도_이벤트도_없다(int chapterId)
        {
            WorldMapViewModel vm = NewVm(Profile(10));
            int notices = 0;
            int changed = 0;
            vm.OnNotice += _ => ++notices;
            vm.OnChanged += () => ++changed;

            Assert.IsFalse(vm.SelectChapter(chapterId));

            Assert.AreEqual(0, notices);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(1, vm.SelectedChapter);
        }

        [TestCase(0)]
        [TestCase(4)]
        public void 챕터2_탭_클릭은_보스를_클리어하기_전에는_이동하지_않고_열리는_조건을_안내한다(int cleared)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared));
            List<string> notices = new List<string>();
            int changed = 0;
            vm.OnNotice += notices.Add;
            vm.OnChanged += () => ++changed;

            Assert.IsFalse(vm.SelectChapter(2));

            Assert.AreEqual(1, notices.Count);
            Assert.AreEqual(string.Format(WorldMapTestSupport.Text(WorldMapViewModel.StrChapterLockedHint), 2, 1), notices[0]);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(1, vm.SelectedChapter);
        }

        [TestCase(5)]
        [TestCase(10)]
        public void 챕터2_탭_클릭은_열려_있으면_이동하고_변경_이벤트가_한번_발행된다(int cleared)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared));
            List<string> notices = new List<string>();
            int changed = 0;
            vm.OnNotice += notices.Add;
            vm.OnChanged += () => ++changed;

            Assert.IsTrue(vm.SelectChapter(2));

            Assert.AreEqual(0, notices.Count);
            Assert.AreEqual(1, changed);
            Assert.AreEqual(2, vm.SelectedChapter);
            Assert.AreEqual(1, vm.SelectedStageIndex, "기존 인덱스 1 유지(2-1 해금)");
            Assert.AreEqual(6, vm.SelectedLinear);
            Assert.IsTrue(vm.Tabs[1].IsSelected);
        }

        [Test]
        public void 챕터2에서_잠긴_인덱스가_있으면_탭_이동_시_1번으로_돌아가고_이전_챕터_인덱스를_다시_고를_수_있다()
        {
            WorldMapViewModel vm = NewVm(Profile(5, 5));

            Assert.IsTrue(vm.SelectChapter(2));
            Assert.AreEqual(1, vm.SelectedStageIndex, "2-5 는 잠겨 있어 1 로");
            Assert.IsTrue(vm.SelectChapter(1));
            Assert.AreEqual(1, vm.SelectedChapter);
        }

        [Test]
        public void 챕터1_탭을_다시_누르면_이동하고_스테이지_인덱스는_유지되며_변경_이벤트가_한번_발행된다()
        {
            WorldMapViewModel vm = NewVm(Profile(4, 3));
            int changed = 0;
            vm.OnChanged += () => ++changed;

            Assert.IsTrue(vm.SelectChapter(1));

            Assert.AreEqual(3, vm.SelectedStageIndex);
            Assert.AreEqual(1, changed);
        }

        [Test]
        public void 구독자가_없어도_탭_노드_확정_호출은_예외없다()
        {
            WorldMapViewModel vm = NewVm(Profile(0));

            Assert.DoesNotThrow(() => vm.SelectChapter(2));
            Assert.DoesNotThrow(() => vm.SelectNode(5));
            Assert.DoesNotThrow(() => vm.SelectNode(1));
            Assert.DoesNotThrow(() => vm.Confirm());
        }

        //# --- 노드 ---

        [Test]
        public void 클리어_0이면_1번만_열려_있고_나머지는_잠금이며_클리어_태그가_없다()
        {
            WorldMapViewModel vm = NewVm(Profile(0));

            Assert.AreEqual(EWorldMapNodeState.Open, vm.Nodes[0].State);
            for (int i = 1; i < 5; ++i)
            {
                Assert.AreEqual(EWorldMapNodeState.Locked, vm.Nodes[i].State, $"노드 {i + 1}");
                Assert.AreEqual("???", vm.Nodes[i].NameText);
                Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrLocked), vm.Nodes[i].LabelText);
            }
            foreach (WorldMapNodeData node in vm.Nodes)
            {
                Assert.AreEqual(string.Empty, node.ClearedTagText);
            }
        }

        [Test]
        public void 해금된_노드는_챕터_인덱스_라벨과_스테이지명을_가진다()
        {
            WorldMapViewModel vm = NewVm(Profile(2, 1));

            Assert.AreEqual("1-1", vm.Nodes[0].LabelText);
            Assert.AreEqual("1-3", vm.Nodes[2].LabelText);
            Assert.IsNotEmpty(vm.Nodes[0].NameText);
            Assert.AreNotEqual("???", vm.Nodes[2].NameText);
            Assert.AreEqual("???", vm.Nodes[3].NameText);
        }

        [Test]
        public void 노드_상태_경계는_클리어_N이면_N까지_클리어_N_플러스_1이_열림_나머지_잠금이다()
        {
            for (int cleared = 0; cleared <= 5; ++cleared)
            {
                WorldMapViewModel vm = NewVm(Profile(cleared));
                for (int index = 1; index <= 5; ++index)
                {
                    EWorldMapNodeState expected = index <= cleared
                        ? EWorldMapNodeState.Cleared
                        : (index == cleared + 1 ? EWorldMapNodeState.Open : EWorldMapNodeState.Locked);

                    Assert.AreEqual(expected, vm.Nodes[index - 1].State, $"cleared {cleared} / 노드 {index}");
                }
            }
        }

        [Test]
        public void 챕터1_보스를_클리어하면_5개_노드가_모두_클리어이고_클리어_태그가_붙는다()
        {
            WorldMapViewModel vm = NewVm(Profile(5, 5));

            foreach (WorldMapNodeData node in vm.Nodes)
            {
                Assert.AreEqual(EWorldMapNodeState.Cleared, node.State);
                Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrCleared), node.ClearedTagText);
            }
        }

        [Test]
        public void 보스_플래그는_5번_노드에만_있고_선택_플래그는_하나뿐이다()
        {
            WorldMapViewModel vm = NewVm(Profile(5, 3));

            int selected = 0;
            for (int i = 0; i < vm.Nodes.Count; ++i)
            {
                Assert.AreEqual(i == 4, vm.Nodes[i].IsBoss, $"노드 {i + 1}");
                if (vm.Nodes[i].IsSelected)
                {
                    ++selected;
                }
                Assert.AreEqual(i + 1, vm.Nodes[i].StageIndex);
                Assert.AreEqual(i + 1, vm.Nodes[i].Linear);
            }
            Assert.AreEqual(1, selected);
            Assert.IsTrue(vm.Nodes[2].IsSelected);
        }

        [TestCase(0)]
        [TestCase(6)]
        [TestCase(-1)]
        [TestCase(99)]
        public void 범위_밖_노드_번호는_무시되고_이벤트가_없다(int stageIndex)
        {
            WorldMapViewModel vm = NewVm(Profile(5, 2));
            int notices = 0;
            int changed = 0;
            vm.OnNotice += _ => ++notices;
            vm.OnChanged += () => ++changed;

            Assert.IsFalse(vm.SelectNode(stageIndex));

            Assert.AreEqual(0, notices);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(2, vm.SelectedStageIndex);
        }

        [Test]
        public void 해금된_노드를_누르면_선택이_옮겨지고_변경_이벤트가_한번_발행된다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 1));
            int changed = 0;
            vm.OnChanged += () => ++changed;

            Assert.IsTrue(vm.SelectNode(4));

            Assert.AreEqual(4, vm.SelectedStageIndex);
            Assert.AreEqual(1, changed);
            Assert.IsTrue(vm.Nodes[3].IsSelected);
            Assert.IsFalse(vm.Nodes[0].IsSelected);
        }

        [Test]
        public void 같은_노드를_다시_눌러도_선택_성공이고_상태는_같다()
        {
            WorldMapViewModel vm = NewVm(Profile(3, 2));

            Assert.IsTrue(vm.SelectNode(2));

            Assert.AreEqual(2, vm.SelectedStageIndex);
        }

        [TestCase(0, 2, "스테이지 1-1 클리어 필요")]
        [TestCase(0, 5, "스테이지 1-4 클리어 필요")]
        [TestCase(3, 5, "스테이지 1-4 클리어 필요")]
        [TestCase(1, 4, "스테이지 1-3 클리어 필요")]
        public void 잠긴_노드_안내는_직전_스테이지를_가리키고_상태는_그대로다(int cleared, int stageIndex, string expectedNotice)
        {
            WorldMapViewModel vm = NewVm(Profile(cleared));
            string notice = null;
            int changed = 0;
            vm.OnNotice += message => notice = message;
            vm.OnChanged += () => ++changed;
            int before = vm.SelectedStageIndex;

            Assert.IsFalse(vm.SelectNode(stageIndex));

            Assert.AreEqual(expectedNotice, notice);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(before, vm.SelectedStageIndex);
        }

        //# --- 확정 ---

        [Test]
        public void 확정은_초기_선택도_바로_가능하고_선형_번호와_이벤트_순서가_지켜진다()
        {
            List<string> order = new List<string>();
            int confirmed = -1;
            WorldMapViewModel vm = NewVm(Profile(0), linear =>
            {
                confirmed = linear;
                order.Add("confirm");
            });
            vm.OnConfirmed += () => order.Add("closed");

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(1, confirmed);
            CollectionAssert.AreEqual(new[] { "confirm", "closed" }, order, "저장 후 닫기 순서");
        }

        [Test]
        public void 확정은_onConfirm이_null이어도_OnConfirmed를_발행한다()
        {
            WorldMapViewModel vm = NewVm(Profile(0), null);
            int closed = 0;
            vm.OnConfirmed += () => ++closed;

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(1, closed);
        }

        [Test]
        public void 확정은_프로필의_SelectedStage를_직접_바꾸지_않는다()
        {
            MetaProfile p = Profile(3, 1);
            WorldMapViewModel vm = NewVm(p);
            vm.SelectNode(4);

            vm.Confirm();

            Assert.AreEqual(1, p.SelectedStage, "저장은 호출자(onConfirm) 책임");
        }

        [Test]
        public void 거부된_선택_뒤_확정은_마지막으로_성공한_선택을_넘긴다()
        {
            int confirmed = -1;
            WorldMapViewModel vm = NewVm(Profile(3, 1), linear => confirmed = linear);
            vm.SelectNode(3);
            vm.SelectNode(5);

            vm.Confirm();

            Assert.AreEqual(3, confirmed);
        }

        [Test]
        public void 확정을_여러번_불러도_매번_같은_선형_번호를_넘긴다()
        {
            List<int> confirmed = new List<int>();
            WorldMapViewModel vm = NewVm(Profile(3, 2), confirmed.Add);

            vm.Confirm();
            vm.Confirm();

            CollectionAssert.AreEqual(new[] { 2, 2 }, confirmed);
        }

        //# --- VillageViewModel 연동 ---

        [Test]
        public void 확정이_마을_캐러셀_스테이지로_반영되고_변경_통지가_한번_나간다()
        {
            MetaProfile p = Profile(3, 1);
            VillageViewModel village = new VillageViewModel(p, null, WorldMapTestSupport.ShippedDb(), WorldMapTestSupport.Text);
            int stageChanged = 0;
            village.OnStageChanged += () => ++stageChanged;
            WorldMapViewModel vm = NewVm(p, village.SelectStage);
            vm.SelectNode(2);

            vm.Confirm();

            Assert.AreEqual(2, village.SelectedStage);
            Assert.AreEqual("STAGE 1-2", village.StageIndicatorText);
            Assert.AreEqual(1, stageChanged);
        }

        [Test]
        public void 마을이_이미_같은_스테이지면_확정해도_변경_통지가_없다()
        {
            MetaProfile p = Profile(3, 1);
            VillageViewModel village = new VillageViewModel(p, null, WorldMapTestSupport.ShippedDb(), WorldMapTestSupport.Text);
            int stageChanged = 0;
            village.OnStageChanged += () => ++stageChanged;
            WorldMapViewModel vm = NewVm(p, village.SelectStage);
            vm.SelectNode(village.SelectedStage);

            vm.Confirm();

            Assert.AreEqual(0, stageChanged);
        }

        //# --- 상세 / 헤더 ---

        [Test]
        public void 초기_상세는_번호_별_영웅_잠금_상태_하단_문구를_가진다()
        {
            WorldMapViewModel vm = NewVm(Profile(0));

            Assert.AreEqual("1-1", vm.DetailNumberText);
            Assert.AreEqual("★☆☆☆☆", vm.DetailStarsText);
            StringAssert.StartsWith("영웅: ", vm.DetailHeroText);
            Assert.IsFalse(vm.DetailLocked);
            Assert.IsFalse(vm.DetailIsBoss);
            Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrSelectNote), vm.BottomText);
            Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrSelectButton), vm.SelectButtonText);
            Assert.IsTrue(vm.SelectEnabled);
        }

        [Test]
        public void 보스_노드를_선택하면_상세에_보스_표시와_별_5개가_나온다()
        {
            WorldMapViewModel vm = NewVm(Profile(4, 1));

            vm.SelectNode(5);

            Assert.IsTrue(vm.DetailIsBoss);
            Assert.AreEqual("★★★★★", vm.DetailStarsText);
            Assert.AreEqual("1-5", vm.DetailNumberText);
        }

        [Test]
        public void 상세_이름은_선택한_스테이지의_스테이지명이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            WorldMapViewModel vm = NewVm(Profile(4, 1));

            vm.SelectNode(3);

            Assert.AreEqual(WorldMapTestSupport.Text(db.GetStage(1, 3).nameKey), vm.DetailNameText);
        }

        [Test]
        public void 헤더는_챕터명_테마_영웅을_담는다()
        {
            WorldMapViewModel vm = NewVm(Profile(0));

            Assert.AreEqual("1장 지하 묘지", vm.ChapterTitleText);
            StringAssert.StartsWith("테마: ", vm.ChapterThemeText);
            StringAssert.Contains(" / 영웅: ", vm.ChapterThemeText);
        }

        [Test]
        public void 기록은_선택한_스테이지_것만_보여_주고_패배만_있으면_최단은_대시다()
        {
            MetaProfile p = Profile(3, 1);
            p.RecordStageRun(3, false, 0f);
            WorldMapViewModel vm = NewVm(p);

            Assert.AreEqual("기록 없음", vm.DetailRecordText, "1-1 에는 기록이 없다");
            vm.SelectNode(3);
            Assert.AreEqual("최단 - / 0승 1판", vm.DetailRecordText);
        }

        [Test]
        public void 기록_최단은_소수_첫째_자리_내림_m_ss_t_형식이다()
        {
            MetaProfile p = Profile(1, 1);
            p.RecordStageRun(1, true, 65.99f);
            WorldMapViewModel vm = NewVm(p);

            Assert.AreEqual("최단 1:05.9 / 1승 1판", vm.DetailRecordText);
        }

        [Test]
        public void 노드_이동에_따라_규칙_문구가_갱신되고_이전_문구가_남지_않는다()
        {
            WorldMapViewModel vm = NewVm(Profile(10, 1));
            string none = WorldMapTestSupport.Text(WorldMapViewModel.StrNoRules);

            Assert.AreEqual(none, vm.DetailRulesText);
            vm.SelectNode(2);
            StringAssert.Contains("성수 샘", vm.DetailRulesText);
            vm.SelectNode(3);
            StringAssert.Contains("짙은 안개", vm.DetailRulesText);
            StringAssert.DoesNotContain("성수 샘", vm.DetailRulesText);
            vm.SelectNode(1);
            Assert.AreEqual(none, vm.DetailRulesText);
        }

        [Test]
        public void 보스_페이즈_문구는_보스_노드에서만_있고_다른_노드로_옮기면_비워진다()
        {
            WorldMapViewModel vm = NewVm(Profile(10, 5));

            Assert.IsTrue(vm.HasBossPhases);
            StringAssert.Contains("55%", vm.DetailBossPhaseText);

            vm.SelectNode(4);

            Assert.IsFalse(vm.HasBossPhases);
            Assert.AreEqual(string.Empty, vm.DetailBossPhaseText);
        }

        [Test]
        public void 상시_규칙과_보스_페이즈는_서로의_목록에_섞이지_않는다()
        {
            WorldMapViewModel vm = NewVm(Profile(10, 5));

            StringAssert.DoesNotContain("수호 결계", vm.DetailRulesText);
            StringAssert.Contains("수호 결계", vm.DetailBossPhaseText);
        }
    }
}
