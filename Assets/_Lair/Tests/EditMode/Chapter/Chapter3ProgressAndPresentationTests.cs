using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# 챕터 3 진행·표시 보강 (chapter-3-hero §2·§5·§7.3·§8.1) - 선형 15 경계, v5->v6 마이그레이션, 랭킹 1~15, Assassin 스탯, 영웅 재스킨 15엔트리, 문구, 월드맵 3번째 탭, 기록 팝업 15행.
    //# 챕터 수 정적 상태는 ChapterStaticStateTestBase 가 격리하고, 챕터 3 가 로드된 상태는 ConfigureChapterCount(3) 으로 재현한다.
    public class Chapter3ProgressAndPresentationTests : ChapterStaticStateTestBase
    {
        private const string VariantAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";
        private string _tempDir;

        [SetUp]
        public void 챕터3가_로드된_상태로_만든다()
        {
            StageProgress.ConfigureChapterCount(3);
            _tempDir = Path.Combine(Path.GetTempPath(), "lair_ch3_" + System.Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void 임시_폴더를_지운다()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        //# ===== StageProgress 15 =====

        [Test]
        public void 챕터3가_로드되면_MaxStage는_15다()
        {
            Assert.AreEqual(3, StageProgress.ChapterCount);
            Assert.AreEqual(15, StageProgress.MaxStage);
        }

        [Test]
        public void 로더와_같이_DB_챕터_수로_구성하면_MaxStage가_15가_된다()
        {
            StageProgress.ConfigureChapterCount(StageProgress.DefaultChapterCount);

            StageProgress.ConfigureChapterCount(WorldMapTestSupport.ShippedDb().ChapterCount);

            Assert.AreEqual(15, StageProgress.MaxStage);
        }

        [TestCase(10, 2, 5)]
        [TestCase(11, 3, 1)]
        [TestCase(12, 3, 2)]
        [TestCase(14, 3, 4)]
        [TestCase(15, 3, 5)]
        [TestCase(16, 4, 1)]
        public void 선형_번호_10_11_15_16은_챕터와_인덱스로_분해된다(int linear, int chapter, int index)
        {
            Assert.AreEqual(chapter, StageProgress.ChapterOf(linear));
            Assert.AreEqual(index, StageProgress.IndexOf(linear));
            Assert.AreEqual(linear, StageProgress.ToLinear(chapter, index));
        }

        [TestCase(10, 11, true)]
        [TestCase(10, 12, false)]
        [TestCase(14, 15, true)]
        [TestCase(15, 15, true)]
        [TestCase(15, 16, true)]
        [TestCase(15, 17, false)]
        public void 해금은_챕터_경계_10_11과_15를_그대로_통과한다(int cleared, int stage, bool expected)
        {
            Assert.AreEqual(expected, StageProgress.IsUnlocked(stage, cleared));
        }

        [TestCase(3, 9, false)]
        [TestCase(3, 10, true)]
        [TestCase(3, 15, true)]
        [TestCase(2, 4, false)]
        [TestCase(2, 5, true)]
        public void 챕터3_해금은_챕터2_보스_클리어_cleared_10에서_열린다(int chapter, int cleared, bool expected)
        {
            Assert.AreEqual(expected, StageProgress.IsChapterUnlocked(chapter, cleared));
        }

        [TestCase(0, 0, 0)]
        [TestCase(10, 11, 11)]
        [TestCase(14, 15, 15)]
        [TestCase(15, 16, 15)]
        [TestCase(15, 99, 15)]
        [TestCase(12, 3, 12)]
        public void 클리어_갱신은_MaxStage_15를_상한으로_한다(int cleared, int justCleared, int expected)
        {
            Assert.AreEqual(expected, StageProgress.ResolveClearedStage(cleared, justCleared));
        }

        [TestCase(9, 10)]
        [TestCase(10, 11)]
        [TestCase(14, 15)]
        [TestCase(15, 15)]
        [TestCase(99, 15)]
        public void 최대_플레이_스테이지는_15를_상한으로_한다(int cleared, int expected)
        {
            Assert.AreEqual(expected, StageProgress.MaxPlayableStage(cleared));
        }

        //# ===== MetaProfile v5 -> v6 =====

        private static MetaProfile V5(int cleared, int selected, string hero = "Knight")
        {
            return new MetaProfile { Version = 5, ClearedStage = cleared, SelectedStage = selected, SelectedHero = hero };
        }

        [Test]
        public void 신규_프로필의_버전은_6이고_마이그레이션_현재_버전과_같다()
        {
            Assert.AreEqual(6, new MetaProfile().Version);
            Assert.AreEqual(6, MetaProfileMigration.CurrentVersion);
        }

        [TestCase(0, 1, 1)]
        [TestCase(5, 5, 5)]
        [TestCase(10, 10, 10)]
        [TestCase(10, 11, 11)]
        [TestCase(10, 15, 11)]
        public void v5_진행값은_v6에서_보존되고_선택은_최대_플레이_이하로_보정된다(int cleared, int selected, int expectedSelected)
        {
            MetaProfile p = V5(cleared, selected);

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(cleared, p.ClearedStage);
            Assert.AreEqual(expectedSelected, p.SelectedStage);
        }

        [Test]
        public void v5_챕터2_보스_클리어_유저는_챕터3_첫_스테이지가_해금된_상태가_된다()
        {
            MetaProfile p = V5(10, 10);

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.IsTrue(StageProgress.IsChapterUnlocked(3, p.ClearedStage));
        }

        [Test]
        public void 선택_스테이지가_챕터3면_SelectedHero가_암살자로_재설정된다()
        {
            MetaProfile p = V5(12, 11, "Knight");

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual("Assassin", p.SelectedHero);
        }

        [TestCase(5, "Knight")]
        [TestCase(6, "Archer")]
        [TestCase(10, "Archer")]
        [TestCase(11, "Assassin")]
        [TestCase(15, "Assassin")]
        public void SelectedHero는_선택_스테이지_챕터의_영웅이다(int selected, string expectedHero)
        {
            MetaProfile p = V5(15, selected, "Knight");

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(expectedHero, p.SelectedHero);
        }

        [TestCase(14, 14)]
        [TestCase(15, 15)]
        [TestCase(16, 15)]
        [TestCase(999, 15)]
        [TestCase(-3, 0)]
        public void ClearedStage는_0에서_15_사이로_clamp된다(int cleared, int expected)
        {
            MetaProfile p = new MetaProfile { Version = 5, ClearedStage = cleared };

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(expected, p.ClearedStage);
        }

        [Test]
        public void StageRecords는_15까지_남고_16부터_제거된다()
        {
            MetaProfile p = V5(0, 1);
            foreach (int stage in new[] { 0, 1, 10, 11, 15, 16, 25 })
            {
                p.StageRecords.Add(new StageRecordEntry { Stage = stage, Runs = 1 });
            }

            MetaProfileMigration.Apply(p, WorldMapTestSupport.ShippedDb());

            List<int> kept = new List<int>();
            foreach (StageRecordEntry entry in p.StageRecords)
            {
                kept.Add(entry.Stage);
            }
            CollectionAssert.AreEqual(new[] { 1, 10, 11, 15 }, kept);
        }

        [Test]
        public void 마이그레이션은_15_경계에서도_멱등이다()
        {
            MetaProfile once = V5(15, 15, "Knight");
            MetaProfile twice = V5(15, 15, "Knight");

            MetaProfileMigration.Apply(once, WorldMapTestSupport.ShippedDb());
            MetaProfileMigration.Apply(twice, WorldMapTestSupport.ShippedDb());
            MetaProfileMigration.Apply(twice, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(once.Version, twice.Version);
            Assert.AreEqual(once.ClearedStage, twice.ClearedStage);
            Assert.AreEqual(once.SelectedStage, twice.SelectedStage);
            Assert.AreEqual(once.SelectedHero, twice.SelectedHero);
        }

        [Test]
        public void v5_클라우드_문서를_복원하면_재적용으로_v6_선택_영웅이_보정된다()
        {
            MetaProfile live = new MetaProfile { DisplayName = "로컬" };
            MetaProfile cloud = V5(12, 99, "Knight");

            live.CopyFrom(cloud);
            Assert.AreEqual(5, live.Version, "전제: CopyFrom 은 Version 을 그대로 복사");
            MetaProfileMigration.Apply(live, WorldMapTestSupport.ShippedDb());

            Assert.AreEqual(6, live.Version);
            Assert.AreEqual(12, live.ClearedStage);
            Assert.AreEqual(13, live.SelectedStage, "Selected 99 는 MaxPlayable(13) 로 보정");
            Assert.AreEqual("Assassin", live.SelectedHero);
            Assert.AreEqual("로컬", live.DisplayName);
        }

        [Test]
        public void 챕터DB가_null이면_Version과_clamp만_적용되고_영웅은_유지된다()
        {
            MetaProfile p = V5(14, 99, "Knight");

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(15, p.SelectedStage);
            Assert.AreEqual("Knight", p.SelectedHero);
        }

        private void WriteSave(MetaProfile profile)
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), UnityEngine.JsonUtility.ToJson(profile));
        }

        [Test]
        public void Store_Load_v5_세이브는_Current가_있으면_v6_승격과_챕터3_영웅_재설정을_한다()
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();
            WriteSave(V5(12, 12, "Knight"));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual(12, loaded.ClearedStage);
            Assert.AreEqual("Assassin", loaded.SelectedHero);
        }

        [Test]
        public void Store_Load_v5_세이브는_Current가_null이면_영웅만_건너뛰고_v6이_된다()
        {
            ChapterDatabase.Current = null;
            WriteSave(V5(12, 12, "Knight"));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual(12, loaded.SelectedStage);
            Assert.AreEqual("Knight", loaded.SelectedHero);
        }

        [Test]
        public void Store_Load_v5_세이브의_챕터3_범위_전적은_보존되고_범위_밖은_제거된다()
        {
            ChapterDatabase.Current = WorldMapTestSupport.ShippedDb();
            MetaProfile v5 = V5(10, 10);
            v5.StageRecords.Add(new StageRecordEntry { Stage = 11, Runs = 2 });
            v5.StageRecords.Add(new StageRecordEntry { Stage = 16, Runs = 2 });
            WriteSave(v5);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(1, loaded.StageRecords.Count);
            Assert.AreEqual(11, loaded.StageRecords[0].Stage);
        }

        //# ===== 랭킹 1~15 =====

        [TestCase(0, false)]
        [TestCase(1, true)]
        [TestCase(10, true)]
        [TestCase(11, true)]
        [TestCase(15, true)]
        [TestCase(16, false)]
        [TestCase(-1, false)]
        public void 랭킹_스테이지_유효_범위는_1에서_15다(int stage, bool expected)
        {
            Assert.AreEqual(expected, StageLeaderboard.IsValidStage(stage));
        }

        [Test]
        public void 랭킹_MaxStage와_문서_ID는_챕터3_선형_번호를_그대로_쓴다()
        {
            Assert.AreEqual(15, StageLeaderboard.MaxStage);
            Assert.AreEqual("15_uid", StageLeaderboard.DocId(15, "uid"));
            Assert.AreEqual("11_uid", StageLeaderboard.DocId(11, "uid"));
        }

        //# ===== Assassin 스탯 =====

        [TestCase(1, 5768, 42)]
        [TestCase(2, 6440, 46)]
        [TestCase(3, 7112, 51)]
        [TestCase(4, 7560, 56)]
        [TestCase(5, 8960, 61)]
        public void 암살자_스테이지별_HP와_공격력은_앵커_곱_직업배율_곱_스테이지배수_한번만_반올림이다(int index, int expectedHp, int expectedPower)
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            HeroStats stats = HeroStatCalculator.Compute(4000, 50, db.GetHero("Assassin"), db.GetStage(3, index));

            Assert.AreEqual(expectedHp, stats.Hp, $"3-{index} HP");
            Assert.AreEqual(expectedPower, stats.Power, $"3-{index} Power");
        }

        [Test]
        public void 암살자_3_2_공격력은_45_5가_올림으로_46이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(46, HeroStatCalculator.Compute(4000, 50, db.GetHero("Assassin"), db.GetStage(3, 2)).Power);
        }

        [Test]
        public void 암살자_사거리_쿨다운_이동속도는_직업_값_그대로다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            HeroStats stats = HeroStatCalculator.Compute(4000, 50, db.GetHero("Assassin"), db.GetStage(3, 1));

            Assert.AreEqual(1.6f, stats.Range, 0.0001f);
            Assert.AreEqual(0.7f, stats.Cooldown, 0.0001f);
            Assert.AreEqual(4.2f, stats.MoveSpeed, 0.0001f);
        }

        [Test]
        public void 기사_궁수_스탯은_챕터3_추가_후에도_불변이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(4000, HeroStatCalculator.Compute(4000, 50, db.GetHero("Knight"), db.GetStage(1, 1)).Hp);
            Assert.AreEqual(7680, HeroStatCalculator.Compute(4000, 50, db.GetHero("Knight"), db.GetStage(1, 5)).Hp);
            Assert.AreEqual(4992, HeroStatCalculator.Compute(4000, 50, db.GetHero("Archer"), db.GetStage(2, 1)).Hp);
            Assert.AreEqual(44, HeroStatCalculator.Compute(4000, 50, db.GetHero("Archer"), db.GetStage(2, 1)).Power);
        }

        [Test]
        public void 암살자는_같은_스테이지_기준_기사보다_HP가_낮다_직업_70퍼센트()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();
            StageRow stage = db.GetStage(3, 1);

            int assassin = HeroStatCalculator.Compute(4000, 50, db.GetHero("Assassin"), stage).Hp;
            int knight = HeroStatCalculator.Compute(4000, 50, db.GetHero("Knight"), stage).Hp;

            Assert.Less(assassin, knight);
        }

        //# ===== HeroStageVariantConfig 15엔트리 =====

        [Test]
        public void 영웅_재스킨_정본은_15엔트리이고_선형_11_15가_챕터3_엔트리다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);

            Assert.AreEqual(15, config.Stages.Count);
            for (int linear = 11; linear <= 15; ++linear)
            {
                HeroStageVariant variant = config.GetStage(linear);
                Assert.AreEqual(3, variant.ChapterId, $"linear {linear}");
                Assert.AreEqual(linear - 10, variant.StageIndex, $"linear {linear}");
            }
        }

        [Test]
        public void 영웅_재스킨_조회는_16_이상을_마지막_엔트리로_클램프한다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);

            Assert.AreEqual(5, config.GetStage(16).StageIndex);
            Assert.AreEqual(3, config.GetStage(16).ChapterId);
            Assert.AreEqual(3, config.GetStage(int.MaxValue).ChapterId);
        }

        [Test]
        public void 영웅_재스킨_엔트리는_15개_키가_모두_서로_다르다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);
            HashSet<int> keys = new HashSet<int>();

            foreach (HeroStageVariant variant in config.Stages)
            {
                keys.Add(StageProgress.ToLinear(variant.ChapterId, variant.StageIndex));
            }

            Assert.AreEqual(15, keys.Count);
            for (int linear = 1; linear <= 15; ++linear)
            {
                Assert.IsTrue(keys.Contains(linear), $"linear {linear} 엔트리 없음");
            }
        }

        [Test]
        public void 챕터3_보스_엔트리만_스케일_1_2이고_보스_앞_4개는_1이다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(VariantAssetPath);

            Assert.AreEqual(1.2f, config.GetStage(15).ScaleMultiplier, 0.0001f);
            for (int linear = 11; linear <= 14; ++linear)
            {
                Assert.AreEqual(1f, config.GetStage(linear).ScaleMultiplier, 0.0001f, $"linear {linear}");
            }
        }

        //# ===== Strings_Ko =====

        [TestCase(1003, "그림자 성채")]
        [TestCase(1013, "자객단의 침투")]
        [TestCase(1023, "암살자")]
        [TestCase(1121, "성채의 외벽")]
        [TestCase(1122, "그림자 회랑")]
        [TestCase(1123, "달빛 없는 정원")]
        [TestCase(1124, "봉인된 서고")]
        [TestCase(1125, "자객 수장의 은신처")]
        public void 챕터3_신규_문구_id가_기획서_값으로_존재한다(int id, string expected)
        {
            Assert.AreEqual(expected, WorldMapTestSupport.Text(id));
        }

        [Test]
        public void 규칙명_1206은_덫으로_일반화됐다()
        {
            Assert.AreEqual("덫", WorldMapTestSupport.Text(1206));
            Assert.AreEqual("덫", StageRuleText.Name(EStageRule.HunterTraps, WorldMapTestSupport.Text));
        }

        [Test]
        public void 챕터_스테이지_영웅_문구_id는_챕터3까지_모두_존재한다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            foreach (ChapterRow chapter in db.Chapters)
            {
                Assert.IsNotEmpty(WorldMapTestSupport.Text(chapter.nameKey), $"챕터 {chapter.chapterId}");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(chapter.themeTextKey), $"챕터 {chapter.chapterId} 테마");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(db.GetHero(chapter.heroId).nameKey), $"챕터 {chapter.chapterId} 영웅");
                for (int index = 1; index <= 5; ++index)
                {
                    Assert.IsNotEmpty(WorldMapTestSupport.Text(db.GetStage(chapter.chapterId, index).nameKey), $"{chapter.chapterId}-{index}");
                }
            }
        }

        [Test]
        public void 문구_id는_챕터3_추가_후에도_오름차순이고_중복이_없다()
        {
            StringEntry[] entries = ChvjUnityInfra.JsonArrayUtility.FromJsonArray<StringEntry>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Strings_Ko.json"));

            for (int i = 1; i < entries.Length; ++i)
            {
                Assert.Less(entries[i - 1].id, entries[i].id, $"위치 {i}");
            }
        }

        [Test]
        public void 챕터3_규칙_설명은_실제_수치로_치환된다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual("역병귀 둥지가 봉인되어 있다 - 영웅 HP 60% 이하에서 해제", StageRuleText.Description(db.GetRules(3, 2)[0], WorldMapTestSupport.Text));
            Assert.AreEqual("40초마다 8초간 샘이 열려, 샘 안의 영웅이 초당 최대 HP의 0.4% 회복", StageRuleText.Description(db.GetRules(3, 3)[0], WorldMapTestSupport.Text));
            Assert.AreEqual("저주술사 둥지가 봉인되어 있다 - 영웅 HP 50% 이하에서 해제", StageRuleText.Description(db.GetRules(3, 4)[0], WorldMapTestSupport.Text));
            Assert.AreEqual("영웅 HP 75% 이하에서 15초간 영웅이 받는 피해 x0.6", StageRuleText.Description(db.GetRules(3, 5)[0], WorldMapTestSupport.Text));
            Assert.AreEqual("영웅 HP 35% 이하에서 영웅 주변에 덫 6개 - 몬스터 이동속도 x0.5, 초당 피해 10", StageRuleText.Description(db.GetRules(3, 5)[1], WorldMapTestSupport.Text));
        }

        [Test]
        public void 챕터3_봉인_표기_대상은_3_2_역병귀_60과_3_4_저주술사_50이다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            Assert.AreEqual(60, StageRuleText.SealUnlockHpPercent(db.GetRules(3, 2)[0], EMonster.Plague));
            Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(db.GetRules(3, 2)[0], EMonster.Hex));
            Assert.AreEqual(50, StageRuleText.SealUnlockHpPercent(db.GetRules(3, 4)[0], EMonster.Hex));
            Assert.AreEqual(0, StageRuleText.SealUnlockHpPercent(db.GetRules(3, 4)[0], EMonster.Plague));
        }

        //# ===== 월드맵 3번째 탭 =====

        private static WorldMapViewModel NewVm(int cleared, int selected = 1)
        {
            return new WorldMapViewModel(WorldMapTestSupport.ShippedDb(), new MetaProfile { ClearedStage = cleared, SelectedStage = selected }, WorldMapTestSupport.Text, null);
        }

        [TestCase(0, true)]
        [TestCase(5, true)]
        [TestCase(9, true)]
        [TestCase(10, false)]
        [TestCase(15, false)]
        public void 세번째_탭은_2장_보스를_클리어하면_열리고_그전에는_잠금이며_준비_중_표기는_없다(int cleared, bool expectedLocked)
        {
            WorldMapViewModel vm = NewVm(cleared);

            WorldMapTabData tab = vm.Tabs[2];
            Assert.AreEqual(3, tab.ChapterId);
            Assert.IsFalse(tab.IsPlaceholder, "챕터 3 는 데이터가 있어 자리표시자가 아니다");
            Assert.AreEqual(expectedLocked, tab.IsLocked);
            Assert.IsFalse(tab.IsSelected);
            Assert.AreEqual("3장 그림자 성채", tab.Label);
            Assert.AreEqual(expectedLocked ? WorldMapTestSupport.Text(WorldMapViewModel.StrLocked) : string.Empty, tab.LockTagText);
            Assert.AreNotEqual(WorldMapTestSupport.Text(WorldMapViewModel.StrNotReady), tab.LockTagText, "ChapterGate 가 3챕터까지 열려 준비 중 표기는 사라진다");
        }

        [Test]
        public void 네번째_다섯번째_탭은_자리표시자로_남는다()
        {
            WorldMapViewModel vm = NewVm(15);

            Assert.IsTrue(vm.Tabs[3].IsPlaceholder);
            Assert.IsTrue(vm.Tabs[4].IsPlaceholder);
            Assert.AreEqual("4장 ???", vm.Tabs[3].Label);
            Assert.AreEqual("5장 ???", vm.Tabs[4].Label);
        }

        [Test]
        public void 출격_가능_범위_안의_챕터3_저장값은_그대로_유지되고_범위_밖은_챕터3_보스로_보정된다()
        {
            WorldMapViewModel inside = NewVm(15, 13);
            Assert.AreEqual(3, inside.SelectedChapter);
            Assert.AreEqual(3, inside.SelectedStageIndex);

            WorldMapViewModel outside = NewVm(15, 16);
            Assert.AreEqual(3, outside.SelectedChapter, "16 은 출격 가능 상한(15) 밖 — 최대 플레이 가능으로 보정");
            Assert.AreEqual(5, outside.SelectedStageIndex);
        }

        //# ===== 기록 팝업 15행 =====

        [Test]
        public void 기록_행_상한은_챕터3가_로드되면_15다()
        {
            Assert.AreEqual(15, RecordsPopup.VisibleStageCount);
            Assert.AreEqual(15, RecordsPopup.BuildCellData(new MetaProfile(), null, WorldMapTestSupport.Text).Count);
        }

        [Test]
        public void 기록_행_라벨은_챕터3_행에서_STAGE_3_1부터_3_5이고_별은_챕터_내_인덱스다()
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = 15 }, null, WorldMapTestSupport.Text);

            for (int index = 1; index <= 5; ++index)
            {
                RecordsStageCellData row = rows[10 + index - 1];
                Assert.AreEqual(10 + index, row.Stage);
                Assert.AreEqual($"STAGE 3-{index}", row.StageText);
                Assert.AreEqual(StageLabels.Stars(index), row.ThreatText);
            }
        }

        [TestCase(0)]
        [TestCase(5)]
        [TestCase(9)]
        public void 진행으로_잠긴_챕터3_행은_잠금이고_직전_스테이지나_챕터_보스_안내를_가진다(int cleared)
        {
            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(new MetaProfile { ClearedStage = cleared }, null, WorldMapTestSupport.Text);

            Assert.IsTrue(rows[10].IsLocked);
            Assert.AreEqual("2장 보스 클리어 필요", rows[10].LockHintText, "3-1 은 직전 챕터 보스 안내");
            Assert.IsTrue(rows[11].IsLocked);
            Assert.AreEqual("스테이지 3-1 클리어 필요", rows[11].LockHintText);
            Assert.AreEqual("스테이지 3-4 클리어 필요", rows[14].LockHintText);
        }

        [Test]
        public void 챕터3_행의_전적은_선형_번호로_조회된다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 15 };
            p.RecordStageRun(13, true, 200f);

            List<RecordsStageCellData> rows = RecordsPopup.BuildCellData(p, null, WorldMapTestSupport.Text);

            Assert.AreEqual("1승", rows[12].WinText);
            Assert.AreEqual("최단 3:20", rows[12].BestText);
            Assert.AreEqual("0승", rows[11].WinText);
        }
    }
}
