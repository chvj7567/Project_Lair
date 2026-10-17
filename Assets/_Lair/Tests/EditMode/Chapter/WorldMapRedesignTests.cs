using Lair.Battle;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 침입 지도 리디자인 본격 검증(worldmap-popup-redesign §9.1) — 클리어 수 경계, 탭 카드 표시 데이터, 연결선 실선 판정, 챕터 팔레트 폴백.
    //# 3장 출격 게이트(ChapterGate)는 const 라 닫힘 상태를 만들 수 없다 — 값 계산이 게이트와 독립임은 StageProgress 순수 함수 테스트로 고정한다.
    public class WorldMapRedesignTests : ChapterStaticStateTestBase
    {
        private static WorldMapViewModel NewVm(int cleared, int selected = 1)
        {
            MetaProfile profile = new MetaProfile { ClearedStage = cleared, SelectedStage = selected };
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), profile, WorldMapTestSupport.Text, null);
        }

        //# --- ClearedCountInChapter 경계 ---

        [TestCase(0, 0, 0, 0)]
        [TestCase(4, 4, 0, 0)]
        [TestCase(5, 5, 0, 0)]
        [TestCase(9, 5, 4, 0)]
        [TestCase(10, 5, 5, 0)]
        [TestCase(14, 5, 5, 4)]
        [TestCase(15, 5, 5, 5)]
        [TestCase(-1, 0, 0, 0)]
        public void 클리어_스테이지_경계별_챕터1_2_3_클리어_수가_기획서_표와_같다(int cleared, int ch1, int ch2, int ch3)
        {
            Assert.AreEqual(ch1, StageProgress.ClearedCountInChapter(1, cleared));
            Assert.AreEqual(ch2, StageProgress.ClearedCountInChapter(2, cleared));
            Assert.AreEqual(ch3, StageProgress.ClearedCountInChapter(3, cleared));
        }

        [TestCase(0, 0)]
        [TestCase(14, 0)]
        [TestCase(15, 0)]
        [TestCase(19, 0)]
        [TestCase(20, 0)]
        [TestCase(21, 1)]
        [TestCase(24, 4)]
        [TestCase(25, 5)]
        [TestCase(99, 5)]
        public void 챕터4_5도_산식대로_계산되고_상한_5로_클램프된다(int cleared, int ch5Expected)
        {
            Assert.AreEqual(ch5Expected, StageProgress.ClearedCountInChapter(5, cleared));
            Assert.AreEqual(cleared >= 20 ? 5 : (cleared >= 15 ? cleared - 15 : 0), StageProgress.ClearedCountInChapter(4, cleared));
        }

        [Test]
        public void 클리어_수는_어떤_입력에서도_0에서_5_사이이고_클리어_스테이지에_대해_단조_증가한다()
        {
            for (int chapter = 1; chapter <= 5; ++chapter)
            {
                int previous = 0;
                for (int cleared = -3; cleared <= 40; ++cleared)
                {
                    int count = StageProgress.ClearedCountInChapter(chapter, cleared);
                    Assert.GreaterOrEqual(count, 0, $"{chapter}장 / {cleared}");
                    Assert.LessOrEqual(count, StageProgress.StagesPerChapter, $"{chapter}장 / {cleared}");
                    Assert.GreaterOrEqual(count, previous, $"{chapter}장 / {cleared} 단조");
                    previous = count;
                }
            }
        }

        [Test]
        public void 챕터별_클리어_수의_합은_0에서_MaxStage_범위에서_클리어_스테이지와_같다()
        {
            for (int cleared = 0; cleared <= 25; ++cleared)
            {
                int sum = 0;
                for (int chapter = 1; chapter <= 5; ++chapter)
                {
                    sum += StageProgress.ClearedCountInChapter(chapter, cleared);
                }
                Assert.AreEqual(cleared, sum, $"cleared {cleared}");
            }
        }

        //# --- WorldMapTabData ---

        [TestCase(0, 0, 0, 0)]
        [TestCase(4, 4, 0, 0)]
        [TestCase(9, 5, 4, 0)]
        [TestCase(10, 5, 5, 0)]
        [TestCase(14, 5, 5, 4)]
        [TestCase(15, 5, 5, 5)]
        public void 탭의_ClearedCount는_VM을_거쳐도_챕터별_산식과_같다(int cleared, int ch1, int ch2, int ch3)
        {
            WorldMapViewModel vm = NewVm(cleared);

            Assert.AreEqual(ch1, vm.Tabs[0].ClearedCount);
            Assert.AreEqual(ch2, vm.Tabs[1].ClearedCount);
            Assert.AreEqual(ch3, vm.Tabs[2].ClearedCount);
        }

        [TestCase(0)]
        [TestCase(10)]
        [TestCase(15)]
        public void 미출시_슬롯_4_5는_클리어_수_0_영웅_빈_문자열_챕터명은_Label과_같다(int cleared)
        {
            WorldMapViewModel vm = NewVm(cleared);

            for (int slot = 4; slot <= 5; ++slot)
            {
                WorldMapTabData tab = vm.Tabs[slot - 1];
                Assert.IsTrue(tab.IsPlaceholder, $"{slot}장");
                Assert.AreEqual(0, tab.ClearedCount, $"{slot}장 / cleared {cleared}");
                Assert.AreEqual(string.Empty, tab.HeroText, $"{slot}장");
                Assert.AreEqual(tab.Label, tab.ChapterNameText, $"{slot}장");
                Assert.AreEqual($"{slot}장 ???", tab.ChapterNameText);
            }
        }

        [Test]
        public void 개방_챕터_탭의_챕터명은_장_번호_접두어_없이_챕터_이름만이고_Label은_접두어를_유지한다()
        {
            WorldMapViewModel vm = NewVm(15);

            Assert.AreEqual("지하 묘지", vm.Tabs[0].ChapterNameText);
            Assert.AreEqual("1장 지하 묘지", vm.Tabs[0].Label);
            Assert.AreEqual(WorldMapTestSupport.Text(1002), vm.Tabs[1].ChapterNameText);
            Assert.AreEqual("그림자 성채", vm.Tabs[2].ChapterNameText);
            Assert.AreEqual("3장 그림자 성채", vm.Tabs[2].Label);
        }

        [Test]
        public void 개방_챕터_탭의_영웅_직업은_챕터_영웅의_이름이다()
        {
            WorldMapViewModel vm = NewVm(15);

            Assert.AreEqual("기사", vm.Tabs[0].HeroText);
            Assert.AreEqual("궁수", vm.Tabs[1].HeroText);
            Assert.AreEqual("암살자", vm.Tabs[2].HeroText);
        }

        [TestCase(0)]
        [TestCase(4)]
        public void 잠금_챕터도_챕터명_영웅이_채워지고_클리어_수는_0이다(int cleared)
        {
            WorldMapViewModel vm = NewVm(cleared);

            WorldMapTabData ch2 = vm.Tabs[1];
            Assert.IsTrue(ch2.IsLocked);
            Assert.IsFalse(ch2.IsPlaceholder);
            Assert.AreEqual(WorldMapTestSupport.Text(1002), ch2.ChapterNameText);
            Assert.AreEqual("궁수", ch2.HeroText);
            Assert.AreEqual(0, ch2.ClearedCount);
            WorldMapTabData ch3 = vm.Tabs[2];
            Assert.IsTrue(ch3.IsLocked);
            Assert.AreEqual("암살자", ch3.HeroText);
            Assert.AreEqual(0, ch3.ClearedCount);
        }

        [Test]
        public void 잠금_챕터라도_클리어_수는_게이트나_잠금과_독립으로_계산된다()
        {
            //# 3장은 ClearedStage 9 에서 잠금이지만 값 계산은 해금 판정과 무관하게 산식 그대로다(0). 칩 숨김은 View 가 IsLocked 로 결정.
            WorldMapViewModel vm = NewVm(9);

            Assert.IsTrue(vm.Tabs[2].IsLocked);
            Assert.AreEqual(StageProgress.ClearedCountInChapter(3, 9), vm.Tabs[2].ClearedCount);
            Assert.IsFalse(vm.Tabs[1].IsLocked);
            Assert.AreEqual(4, vm.Tabs[1].ClearedCount);
        }

        [Test]
        public void 전부_클리어해도_탭은_5칸이고_개방_탭_ClearedCount는_모두_5다()
        {
            WorldMapViewModel vm = NewVm(15);

            Assert.AreEqual(WorldMapViewModel.ChapterSlotCount, vm.Tabs.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.IsFalse(vm.Tabs[i].IsLocked, $"탭 {i + 1}");
                Assert.AreEqual(5, vm.Tabs[i].ClearedCount, $"탭 {i + 1}");
            }
        }

        [Test]
        public void 탭_이동_후에도_ClearedCount_챕터명_영웅은_선택과_무관하게_같다()
        {
            WorldMapViewModel vm = NewVm(15);
            int[] before = { vm.Tabs[0].ClearedCount, vm.Tabs[1].ClearedCount, vm.Tabs[2].ClearedCount };

            Assert.IsTrue(vm.SelectChapter(3));

            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(before[i], vm.Tabs[i].ClearedCount, $"탭 {i + 1}");
            }
            Assert.AreEqual("암살자", vm.Tabs[2].HeroText);
            Assert.AreEqual("지하 묘지", vm.Tabs[0].ChapterNameText);
        }

        //# --- WorldMapNodeData.NextPathCleared ---

        [Test]
        public void 클리어_3이면_1장_노드_1_2_3의_다음_구간이_실선이고_4_5는_아니다()
        {
            WorldMapViewModel vm = NewVm(3);

            bool[] expected = { true, true, true, false, false };
            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(expected[i], vm.Nodes[i].NextPathCleared, $"노드 {i + 1}");
            }
        }

        [Test]
        public void 클리어_0이면_모든_노드의_다음_구간이_점선이다()
        {
            WorldMapViewModel vm = NewVm(0);

            foreach (WorldMapNodeData node in vm.Nodes)
            {
                Assert.IsFalse(node.NextPathCleared, $"노드 {node.StageIndex}");
            }
        }

        [Test]
        public void 클리어_10에서_2장을_보면_노드_1_4는_실선이고_보스_노드_5는_구간이_없어_false다()
        {
            WorldMapViewModel vm = NewVm(10, 6);
            Assert.AreEqual(2, vm.SelectedChapter);

            for (int i = 0; i < 4; ++i)
            {
                Assert.IsTrue(vm.Nodes[i].NextPathCleared, $"노드 {i + 1}");
            }
            Assert.AreEqual(EWorldMapNodeState.Cleared, vm.Nodes[4].State);
            Assert.IsFalse(vm.Nodes[4].NextPathCleared);
        }

        [Test]
        public void 클리어_5_이상에서_1장_보스가_클리어여도_보스_노드의_NextPathCleared는_false다()
        {
            WorldMapViewModel vm = NewVm(5, 5);

            Assert.AreEqual(EWorldMapNodeState.Cleared, vm.Nodes[4].State);
            Assert.IsFalse(vm.Nodes[4].NextPathCleared);
            for (int i = 0; i < 4; ++i)
            {
                Assert.IsTrue(vm.Nodes[i].NextPathCleared, $"노드 {i + 1}");
            }
        }

        [Test]
        public void 클리어_14에서_3장을_보면_노드_1_4는_실선이고_보스는_열림이라_false다()
        {
            WorldMapViewModel vm = NewVm(14, 15);
            Assert.AreEqual(3, vm.SelectedChapter);

            for (int i = 0; i < 4; ++i)
            {
                Assert.IsTrue(vm.Nodes[i].NextPathCleared, $"노드 {i + 1}");
            }
            Assert.AreEqual(EWorldMapNodeState.Open, vm.Nodes[4].State);
            Assert.IsFalse(vm.Nodes[4].NextPathCleared);
        }

        [Test]
        public void 열림_노드와_잠금_노드는_NextPathCleared가_false다()
        {
            WorldMapViewModel vm = NewVm(2, 1);

            Assert.IsTrue(vm.Nodes[1].NextPathCleared);
            Assert.AreEqual(EWorldMapNodeState.Open, vm.Nodes[2].State);
            Assert.IsFalse(vm.Nodes[2].NextPathCleared, "열림(미클리어)");
            Assert.AreEqual(EWorldMapNodeState.Locked, vm.Nodes[3].State);
            Assert.IsFalse(vm.Nodes[3].NextPathCleared, "잠금");
        }

        [Test]
        public void 클리어_5에서_2장으로_이동하면_2장_노드는_열림과_잠금이라_전부_false다()
        {
            WorldMapViewModel vm = NewVm(5, 1);

            Assert.IsTrue(vm.SelectChapter(2));

            foreach (WorldMapNodeData node in vm.Nodes)
            {
                Assert.IsFalse(node.NextPathCleared, $"2-{node.StageIndex}");
            }
        }

        [Test]
        public void NextPathCleared는_모든_조합에서_클리어이면서_보스가_아닌_노드와_같다()
        {
            for (int cleared = 0; cleared <= 15; ++cleared)
            {
                WorldMapViewModel vm = NewVm(cleared, 1);
                for (int chapter = 1; chapter <= 3; ++chapter)
                {
                    if (StageProgress.IsChapterUnlocked(chapter, cleared) == false)
                        continue;

                    vm.SelectChapter(chapter);
                    foreach (WorldMapNodeData node in vm.Nodes)
                    {
                        bool expected = node.State == EWorldMapNodeState.Cleared && node.IsBoss == false;
                        Assert.AreEqual(expected, node.NextPathCleared, $"cleared {cleared} / {chapter}-{node.StageIndex}");
                    }
                }
            }
        }

        [Test]
        public void 실선_구간은_앞쪽부터_연속이다_점선_뒤에_실선이_나오지_않는다()
        {
            for (int cleared = 0; cleared <= 5; ++cleared)
            {
                WorldMapViewModel vm = NewVm(cleared, 1);
                bool seenDashed = false;
                foreach (WorldMapNodeData node in vm.Nodes)
                {
                    if (node.NextPathCleared == false)
                    {
                        seenDashed = true;
                    }
                    else
                    {
                        Assert.IsFalse(seenDashed, $"cleared {cleared} / 노드 {node.StageIndex}");
                    }
                }
            }
        }

        //# --- 팔레트 ---

        [TestCase(1, 0x4a, 0x6f, 0xa5)]
        [TestCase(2, 0x2f, 0x8a, 0x5f)]
        [TestCase(3, 0x7a, 0x55, 0xc8)]
        public void 챕터1_3_Accent는_기획서_표와_같다(int chapter, int r, int g, int b)
        {
            Assert.AreEqual((Color)new Color32((byte)r, (byte)g, (byte)b, 0xff), WorldMapChapterPalette.Accent(chapter));
        }

        [TestCase(1, 0x14, 0x1b, 0x2c)]
        [TestCase(2, 0x0e, 0x22, 0x16)]
        [TestCase(3, 0x1b, 0x11, 0x2d)]
        public void 챕터1_3_지도_배경은_기획서_표와_같다(int chapter, int r, int g, int b)
        {
            Assert.AreEqual((Color)new Color32((byte)r, (byte)g, (byte)b, 0xff), WorldMapChapterPalette.MapBackground(chapter));
        }

        [TestCase(4)]
        [TestCase(5)]
        [TestCase(6)]
        [TestCase(99)]
        [TestCase(0)]
        [TestCase(-1)]
        public void 챕터4_이상과_범위_밖은_중립색으로_폴백한다(int chapter)
        {
            Assert.AreEqual((Color)new Color32(0x2c, 0x37, 0x50, 0xff), WorldMapChapterPalette.Accent(chapter));
            Assert.AreEqual((Color)new Color32(0x0d, 0x11, 0x19, 0xff), WorldMapChapterPalette.MapBackground(chapter));
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        [TestCase(0)]
        public void 배너_틴트는_Accent에_알파_0_53만_적용한다(int chapter)
        {
            Color accent = WorldMapChapterPalette.Accent(chapter);
            Color tint = WorldMapChapterPalette.BannerTint(chapter);

            Assert.AreEqual(accent.r, tint.r);
            Assert.AreEqual(accent.g, tint.g);
            Assert.AreEqual(accent.b, tint.b);
            Assert.AreEqual(0.53f, tint.a, 0.0001f);
            Assert.AreEqual(1f, WorldMapChapterPalette.Accent(chapter).a, "Accent 원본 알파는 변하지 않는다");
        }

        [Test]
        public void 챕터1_3의_Accent와_지도_배경은_서로_모두_다르다()
        {
            for (int a = 1; a <= 3; ++a)
            {
                for (int b = a + 1; b <= 3; ++b)
                {
                    Assert.AreNotEqual(WorldMapChapterPalette.Accent(a), WorldMapChapterPalette.Accent(b), $"{a} vs {b} Accent");
                    Assert.AreNotEqual(WorldMapChapterPalette.MapBackground(a), WorldMapChapterPalette.MapBackground(b), $"{a} vs {b} 배경");
                }
            }
        }
    }
}
