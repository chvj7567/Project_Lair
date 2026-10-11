using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 챕터 3 출격 개방 후(ChapterGate 3) 월드맵·마을 캐러셀·쇼케이스·기록 셀·마이그레이션 방어·게이트 보강 (chapter-3-hero §6·§7.3).
    //# 챕터 수 정적 상태는 ShippedDb()(로더처럼 ConfigureChapterCount) + ChapterStaticStateTestBase 가 격리·원복한다.
    public class Chapter3OpenedWorldTests : ChapterStaticStateTestBase
    {
        private const string VariantAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";
        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();
        private string _tempDir;

        [SetUp]
        public void 임시_폴더_경로를_정한다()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lair_ch3open_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void 생성물과_임시_폴더를_정리한다()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            _spawned.Clear();
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private static WorldMapViewModel NewVm(int cleared, int selected = 1, Action<int> onConfirm = null)
        {
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), new MetaProfile { ClearedStage = cleared, SelectedStage = selected }, WorldMapTestSupport.Text, onConfirm);
        }

        //# ===== ChapterGate 경계 =====

        [Test]
        public void 출격_가능_챕터는_3이고_출시_챕터_수와_같다()
        {
            Assert.AreEqual(3, ChapterGate.PlayableChapterCount);
            Assert.AreEqual(15, ChapterGate.PlayableStageCount);
            Assert.AreEqual(WorldMapTestSupport.ShippedDb().ChapterCount, ChapterGate.PlayableChapterCount, "데이터에 있는 챕터는 모두 열려 있다");
            Assert.AreEqual(ChapterGate.PlayableStageCount, StageProgress.MaxStage);
        }

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, true)]
        [TestCase(4, false)]
        [TestCase(5, false)]
        public void 챕터_출격_가능_경계는_1부터_3까지다(int chapter, bool expected)
        {
            Assert.AreEqual(expected, ChapterGate.IsChapterPlayable(chapter));
        }

        [TestCase(0, false)]
        [TestCase(10, true)]
        [TestCase(11, true)]
        [TestCase(15, true)]
        [TestCase(16, false)]
        [TestCase(25, false)]
        public void 스테이지_출격_가능_경계는_1부터_15까지다(int linear, bool expected)
        {
            Assert.AreEqual(expected, ChapterGate.IsStagePlayable(linear));
        }

        //# ===== 월드맵 챕터 3 =====

        [Test]
        public void 챕터3_진입_직후_노드는_3_1만_열리고_나머지는_잠금이다()
        {
            WorldMapViewModel vm = NewVm(10);
            Assert.IsTrue(vm.SelectChapter(3));

            Assert.AreEqual(3, vm.SelectedChapter);
            Assert.AreEqual(EWorldMapNodeState.Open, vm.Nodes[0].State);
            for (int i = 1; i < 5; ++i)
            {
                Assert.AreEqual(EWorldMapNodeState.Locked, vm.Nodes[i].State, $"3-{i + 1}");
                Assert.AreEqual("???", vm.Nodes[i].NameText);
            }
            Assert.AreEqual("3-1", vm.Nodes[0].LabelText);
            Assert.AreEqual("성채의 외벽", vm.Nodes[0].NameText);
        }

        [TestCase(11, 1)]
        [TestCase(12, 2)]
        [TestCase(13, 3)]
        [TestCase(14, 4)]
        [TestCase(15, 5)]
        public void 챕터3_노드_상태는_클리어_진행도_따라_클리어_열림_잠금으로_나뉜다(int cleared, int clearedCount)
        {
            WorldMapViewModel vm = NewVm(cleared);
            vm.SelectChapter(3);

            for (int index = 1; index <= 5; ++index)
            {
                EWorldMapNodeState expected = index <= clearedCount
                    ? EWorldMapNodeState.Cleared
                    : (index == clearedCount + 1 ? EWorldMapNodeState.Open : EWorldMapNodeState.Locked);
                Assert.AreEqual(expected, vm.Nodes[index - 1].State, $"cleared {cleared} / 3-{index}");
            }
        }

        [Test]
        public void 챕터3_보스_노드만_보스_표시고_5개_모두_클리어면_클리어_태그가_붙는다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);

            for (int i = 0; i < 5; ++i)
            {
                Assert.AreEqual(i == 4, vm.Nodes[i].IsBoss, $"3-{i + 1}");
                Assert.AreEqual(EWorldMapNodeState.Cleared, vm.Nodes[i].State);
                Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrCleared), vm.Nodes[i].ClearedTagText);
            }
        }

        [Test]
        public void 챕터3_헤더는_챕터명_테마_암살자를_담는다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);

            Assert.AreEqual("3장 그림자 성채", vm.ChapterTitleText);
            Assert.AreEqual("테마: 자객단의 침투 / 영웅: 암살자", vm.ChapterThemeText);
            Assert.AreEqual("영웅: 암살자", vm.DetailHeroText);
        }

        [Test]
        public void 챕터3_스테이지명은_노드를_누를_때마다_상세에_반영된다()
        {
            string[] names = { "성채의 외벽", "그림자 회랑", "달빛 없는 정원", "봉인된 서고", "자객 수장의 은신처" };
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);

            for (int index = 1; index <= 5; ++index)
            {
                Assert.IsTrue(vm.SelectNode(index));

                Assert.AreEqual(names[index - 1], vm.DetailNameText, $"3-{index}");
                Assert.AreEqual($"3-{index}", vm.DetailNumberText);
                Assert.AreEqual(StageLabels.Stars(index), vm.DetailStarsText);
                Assert.AreEqual(index == 5, vm.DetailIsBoss);
            }
        }

        [Test]
        public void 챕터3_1_2_3_상세_규칙_문구가_기획서_배정과_같다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);
            string none = WorldMapTestSupport.Text(WorldMapViewModel.StrNoRules);

            vm.SelectNode(1);
            Assert.AreEqual(none, vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);

            vm.SelectNode(2);
            StringAssert.Contains("봉인된 둥지", vm.DetailRulesText);
            StringAssert.Contains("역병귀 둥지가 봉인되어 있다 - 영웅 HP 60% 이하에서 해제", vm.DetailRulesText);

            vm.SelectNode(3);
            StringAssert.Contains("성수 샘", vm.DetailRulesText);
            StringAssert.Contains("초당 최대 HP의 0.4% 회복", vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);
        }

        [Test]
        public void 챕터3_4_상세는_봉인과_안개_두_규칙이_모두_보이고_보스_페이즈는_없다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);

            vm.SelectNode(4);

            StringAssert.Contains("저주술사 둥지가 봉인되어 있다 - 영웅 HP 50% 이하에서 해제", vm.DetailRulesText);
            StringAssert.Contains("짙은 안개", vm.DetailRulesText);
            StringAssert.Contains("몬스터 공격 사거리 x0.7 (최소 0.8)", vm.DetailRulesText);
            Assert.IsFalse(vm.HasBossPhases);
            Assert.AreEqual(string.Empty, vm.DetailBossPhaseText);
        }

        [Test]
        public void 챕터3_5_상세는_상시_규칙이_없고_보스_페이즈_두_건_결계_덫이_ruleIndex_순서로_나온다()
        {
            WorldMapViewModel vm = NewVm(15);
            vm.SelectChapter(3);

            vm.SelectNode(5);

            Assert.AreEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrNoRules), vm.DetailRulesText);
            Assert.IsTrue(vm.HasBossPhases);
            string[] lines = vm.DetailBossPhaseText.Split('\n');
            Assert.AreEqual(4, lines.Length, "규칙명+설명 x 2건");
            Assert.AreEqual("수호 결계", lines[0]);
            Assert.AreEqual("영웅 HP 75% 이하에서 15초간 영웅이 받는 피해 x0.6", lines[1]);
            Assert.AreEqual("덫", lines[2]);
            Assert.AreEqual("영웅 HP 35% 이하에서 영웅 주변에 덫 6개 - 몬스터 이동속도 x0.5, 초당 피해 10", lines[3]);
        }

        [Test]
        public void 챕터3_스테이지로_이동하면_이전_챕터의_규칙과_보스_페이즈_문구가_남지_않는다()
        {
            WorldMapViewModel vm = NewVm(15, 5);
            Assert.IsTrue(vm.HasBossPhases, "전제: 1-5 보스 페이즈");
            StringAssert.Contains("55%", vm.DetailBossPhaseText);

            vm.SelectChapter(3);
            vm.SelectNode(1);

            Assert.IsFalse(vm.HasBossPhases);
            Assert.AreEqual(string.Empty, vm.DetailBossPhaseText);
        }

        [Test]
        public void 잠긴_챕터3_노드를_누르면_직전_스테이지_안내가_나오고_선택은_유지된다()
        {
            WorldMapViewModel vm = NewVm(10);
            vm.SelectChapter(3);
            List<string> notices = new List<string>();
            vm.OnNotice += notices.Add;

            Assert.IsFalse(vm.SelectNode(2));
            Assert.IsFalse(vm.SelectNode(5));

            Assert.AreEqual("스테이지 3-1 클리어 필요", notices[0]);
            Assert.AreEqual("스테이지 3-4 클리어 필요", notices[1]);
            Assert.AreEqual(1, vm.SelectedStageIndex);
        }

        [Test]
        public void 챕터3_선택_확정은_선형_번호를_넘기고_마을_캐러셀이_3챕터_표기로_바뀐다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile { ClearedStage = 12, SelectedStage = 1 };
            VillageViewModel village = new VillageViewModel(profile, null, db, WorldMapTestSupport.Text);
            WorldMapViewModel vm = new WorldMapViewModel(db, profile, WorldMapTestSupport.Text, village.SelectStage);
            vm.SelectChapter(3);
            vm.SelectNode(2);

            Assert.IsTrue(vm.Confirm());

            Assert.AreEqual(12, village.SelectedStage);
            Assert.AreEqual("STAGE 3-2", village.StageIndicatorText);
            Assert.AreEqual("암살자 · 2단계", village.IntruderText);
        }

        [TestCase(10, 12, 3, 1)]
        [TestCase(12, 99, 3, 3)]
        [TestCase(15, 99, 3, 5)]
        [TestCase(15, 0, 3, 5)]
        public void 저장된_선택이_범위를_벗어나면_해금_상한으로_보정되어_챕터3로_시작한다(int cleared, int selected, int chapter, int index)
        {
            WorldMapViewModel vm = NewVm(cleared, selected);

            Assert.AreEqual(chapter, vm.SelectedChapter);
            Assert.AreEqual(index, vm.SelectedStageIndex);
        }

        [Test]
        public void 챕터3에서_챕터1로_탭을_돌아가면_인덱스를_유지하고_챕터1_상세로_바뀐다()
        {
            WorldMapViewModel vm = NewVm(15, 12);
            Assert.AreEqual(3, vm.SelectedChapter);
            Assert.AreEqual(2, vm.SelectedStageIndex);

            Assert.IsTrue(vm.SelectChapter(1));

            Assert.AreEqual(1, vm.SelectedChapter);
            Assert.AreEqual(2, vm.SelectedStageIndex);
            Assert.AreEqual("1-2", vm.DetailNumberText);
            Assert.AreEqual("영웅: 기사", vm.DetailHeroText);
        }

        //# ===== 마을 캐러셀 15 / 쇼케이스 =====

        private static VillageViewModel NewVillage(int cleared)
        {
            return new VillageViewModel(new MetaProfile { ClearedStage = cleared }, null, WorldMapTestSupport.ShippedDb(), WorldMapTestSupport.Text);
        }

        [TestCase(0, 1)]
        [TestCase(9, 10)]
        [TestCase(10, 11)]
        [TestCase(14, 15)]
        [TestCase(15, 15)]
        [TestCase(99, 15)]
        public void 캐러셀_초기_위치는_최대_플레이_스테이지를_15로_clamp한다(int cleared, int expected)
        {
            Assert.AreEqual(expected, NewVillage(cleared).SelectedStage);
        }

        [Test]
        public void 캐러셀은_15에서_더_이동하지_않고_챕터_경계_5_6_10_11을_그대로_통과한다()
        {
            VillageViewModel vm = NewVillage(15);
            Assert.IsFalse(vm.CanMoveNext);
            vm.MoveStage(1);
            Assert.AreEqual(15, vm.SelectedStage);

            vm.SelectStage(10);
            vm.MoveStage(1);
            Assert.AreEqual(11, vm.SelectedStage);
            Assert.AreEqual("STAGE 3-1", vm.StageIndicatorText);
            vm.MoveStage(-1);
            Assert.AreEqual("STAGE 2-5", vm.StageIndicatorText);
            Assert.IsTrue(vm.CanMoveNext);
        }

        [TestCase(11, "STAGE 3-1", "★☆☆☆☆", "암살자 · 1단계")]
        [TestCase(13, "STAGE 3-3", "★★★☆☆", "암살자 · 3단계")]
        [TestCase(15, "STAGE 3-5", "★★★★★", "암살자 · 5단계")]
        [TestCase(10, "STAGE 2-5", "★★★★★", "궁수 · 5단계")]
        [TestCase(5, "STAGE 1-5", "★★★★★", "기사 · 5단계")]
        public void 캐러셀_표기는_선택_스테이지_챕터의_인디케이터_위협도_영웅이다(int stage, string indicator, string stars, string intruder)
        {
            VillageViewModel vm = NewVillage(15);

            vm.SelectStage(stage);

            Assert.AreEqual(indicator, vm.StageIndicatorText);
            Assert.AreEqual(stars, vm.StageThreatText);
            Assert.AreEqual(intruder, vm.IntruderText);
        }

        [Test]
        public void 캐러셀_챕터3_첫_스테이지의_잠금_안내는_2장_보스_클리어_필요다()
        {
            VillageViewModel vm = NewVillage(9);

            vm.SelectStage(11);

            Assert.AreEqual("2장 보스 클리어 필요", vm.StageLockHintText);
            Assert.IsFalse(vm.IsCurrentStageUnlocked);
        }

        private static EHero ShowcaseHeroOf(int linear)
        {
            MethodInfo mi = typeof(VillageController).GetMethod("ShowcaseHeroOf", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "VillageController.ShowcaseHeroOf 존재 - 시그니처 변경 감지");
            return (EHero)mi.Invoke(null, new object[] { linear });
        }

        [Test]
        public void 쇼케이스_영웅은_1에서_5가_기사_6에서_10이_궁수_11에서_15가_암살자다()
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();

            for (int linear = 1; linear <= 15; ++linear)
            {
                EHero expected = linear <= 5 ? EHero.Knight : (linear <= 10 ? EHero.Archer : EHero.Assassin);
                Assert.AreEqual(expected, ShowcaseHeroOf(linear), $"linear {linear}");
            }
        }

        [TestCase(0)]
        [TestCase(-3)]
        [TestCase(16)]
        [TestCase(99)]
        public void 쇼케이스_영웅은_범위_밖_선형_번호에서_예외없이_기사로_폴백한다(int linear)
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(EHero.Knight, ShowcaseHeroOf(linear));
        }

        [Test]
        public void 챕터_데이터가_없으면_모든_스테이지의_쇼케이스_영웅은_기사다()
        {
            ChapterDatabase.Current = null;

            for (int linear = 1; linear <= 15; ++linear)
            {
                Assert.AreEqual(EHero.Knight, ShowcaseHeroOf(linear), $"linear {linear}");
            }
        }

        [Test]
        public void 쇼케이스_영웅_이름은_프리팹_파일명이고_세_영웅_프리팹이_모두_존재한다()
        {
            foreach (EHero hero in new[] { EHero.Knight, EHero.Archer, EHero.Assassin })
            {
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<GameObject>($"Assets/_Lair/Art/Characters/{hero}.prefab"), hero.ToString());
            }
        }

        [TestCase("Knight", "기사")]
        [TestCase("Archer", "궁수")]
        [TestCase(null, "기사")]
        [TestCase("", "기사")]
        [TestCase("Paladin", "Paladin")]
        public void 영웅_표시_이름_폴백은_기사_궁수_미지정_원문이다(string hero, string expected)
        {
            Assert.AreEqual(expected, VillageViewModel.HeroDisplayName(hero));
        }

        //# ===== 기록 셀 / 초상 =====

        [Test]
        public void 기록_행_초상은_영웅_재스킨_정본에서_오고_정본이_없으면_모든_행이_null이다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            MetaProfile profile = new MetaProfile { ClearedStage = 15 };
            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            List<RecordsStageCellData> withConfig = RecordsPopup.BuildCellData(profile, config, WorldMapTestSupport.Text);
            List<RecordsStageCellData> withoutConfig = RecordsPopup.BuildCellData(profile, null, WorldMapTestSupport.Text);

            Assert.AreEqual(15, withConfig.Count);
            for (int i = 0; i < 15; ++i)
            {
                Assert.IsNotNull(withConfig[i].Portrait, $"행 {i + 1}");
                Assert.AreSame(config.GetStage(i + 1).Portrait, withConfig[i].Portrait);
                Assert.IsNull(withoutConfig[i].Portrait, $"행 {i + 1} 정본 없음");
            }
        }

        private RecordsStageCell NewCell(out Image portrait)
        {
            GameObject cellGo = new GameObject("Cell");
            _spawned.Add(cellGo);
            GameObject portraitGo = new GameObject("Portrait");
            portraitGo.transform.SetParent(cellGo.transform);
            portrait = portraitGo.AddComponent<Image>();
            RecordsStageCell cell = cellGo.AddComponent<RecordsStageCell>();
            FieldInfo field = typeof(RecordsStageCell).GetField("_portrait", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "RecordsStageCell._portrait 존재 - 시그니처 변경 감지");
            field.SetValue(cell, portrait);
            return cell;
        }

        private Sprite NewSprite()
        {
            Texture2D texture = new Texture2D(2, 2);
            _spawned.Add(texture);
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, 2, 2), new Vector2(0.5f, 0.5f));
            _spawned.Add(sprite);
            return sprite;
        }

        [Test]
        public void 기록_셀은_초상이_없으면_이미지를_숨기고_있으면_보이며_틴트를_적용한다()
        {
            RecordsStageCell cell = NewCell(out Image portrait);
            Sprite sprite = NewSprite();

            cell.Bind(new RecordsStageCellData { Portrait = null, PortraitTint = Color.white });
            Assert.IsFalse(portrait.gameObject.activeSelf);

            cell.Bind(new RecordsStageCellData { Portrait = sprite, PortraitTint = new Color(0.5f, 0.5f, 0.5f, 1f) });
            Assert.IsTrue(portrait.gameObject.activeSelf);
            Assert.AreSame(sprite, portrait.sprite);
            Assert.AreEqual(0.5f, portrait.color.r, 0.0001f);
        }

        [Test]
        public void 기록_셀은_풀_재사용_Bind마다_초상_표시를_다시_정한다()
        {
            RecordsStageCell cell = NewCell(out Image portrait);
            Sprite sprite = NewSprite();

            cell.Bind(new RecordsStageCellData { Portrait = sprite, PortraitTint = Color.white });
            Assert.IsTrue(portrait.gameObject.activeSelf);
            cell.Bind(new RecordsStageCellData { Portrait = null, PortraitTint = Color.white });
            Assert.IsFalse(portrait.gameObject.activeSelf, "이전 행의 초상이 남지 않는다");
            Assert.IsNull(portrait.sprite);
            cell.Bind(new RecordsStageCellData { Portrait = sprite, PortraitTint = Color.white });
            Assert.IsTrue(portrait.gameObject.activeSelf);
        }

        [Test]
        public void 기록_셀에_null_데이터를_바인딩해도_예외없이_이전_상태를_유지한다()
        {
            RecordsStageCell cell = NewCell(out Image portrait);
            cell.Bind(new RecordsStageCellData { Portrait = null, PortraitTint = Color.white });

            Assert.DoesNotThrow(() => cell.Bind(null));
            Assert.IsFalse(portrait.gameObject.activeSelf);
        }

        //# ===== 마이그레이션 방어 =====

        [TestCase(6)]
        [TestCase(7)]
        public void 현재_버전_이상_세이브는_챕터_데이터_로드_전에는_진행_범위를_자르지_않는다(int version)
        {
            MetaProfile p = new MetaProfile { Version = version, ClearedStage = 99, SelectedStage = 88, SelectedHero = "Archer" };
            p.StageRecords.Add(new StageRecordEntry { Stage = 99, Runs = 1 });

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);
            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(version, p.Version);
            Assert.AreEqual(99, p.ClearedStage);
            Assert.AreEqual(88, p.SelectedStage);
            Assert.AreEqual("Archer", p.SelectedHero);
            Assert.AreEqual(1, p.StageRecords.Count);
        }

        [Test]
        public void 현재_버전_세이브도_챕터_데이터가_있으면_MaxStage로_자른다()
        {
            StageProgress.ConfigureChapterCount(3);
            MetaProfile p = new MetaProfile { Version = 6, ClearedStage = 99, SelectedStage = 99 };

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(15, p.ClearedStage);
            Assert.AreEqual(15, p.SelectedStage);
        }

        [TestCase(5)]
        [TestCase(4)]
        [TestCase(1)]
        public void 구버전_세이브는_챕터_데이터_로드_전에도_기본_범위_10으로_자른다(int version)
        {
            MetaProfile p = new MetaProfile { Version = version, ClearedStage = 15, SelectedStage = 15 };
            p.StageRecords.Add(new StageRecordEntry { Stage = 11, Runs = 1 });
            p.StageRecords.Add(new StageRecordEntry { Stage = 10, Runs = 1 });

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(10, p.ClearedStage);
            Assert.AreEqual(10, p.SelectedStage);
            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(10, p.StageRecords[0].Stage);
        }

        [Test]
        public void 현재_버전_세이브를_디스크에서_읽을_때는_챕터_데이터_유무와_무관하게_그대로다()
        {
            ChapterDatabase.Current = null;
            Directory.CreateDirectory(_tempDir);
            MetaProfile saved = new MetaProfile { Version = 6, ClearedStage = 13, SelectedStage = 12, SelectedHero = "Assassin" };
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), JsonUtility.ToJson(saved));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(13, loaded.ClearedStage);
            Assert.AreEqual(12, loaded.SelectedStage);
            Assert.AreEqual("Assassin", loaded.SelectedHero);
        }

        [Test]
        public void 클라우드_복원_직후_챕터_데이터_로드_전_재적용도_챕터3_진행을_보존한다()
        {
            MetaProfile live = new MetaProfile { DisplayName = "로컬" };
            live.CopyFrom(new MetaProfile { Version = 6, ClearedStage = 14, SelectedStage = 13 });

            MetaProfileMigration.Apply(live, ChapterDatabase.Current);

            Assert.AreEqual(14, live.ClearedStage);
            Assert.AreEqual(13, live.SelectedStage);
        }
    }
}
