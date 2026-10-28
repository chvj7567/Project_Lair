using System;
using System.IO;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 난이도 선택 — MetaProfile v6→v7 마이그레이션 · CopyFrom · JSON 라운드트립 · 챕터 DB 미로드 상태 (difficulty-select §3).
    public class DifficultyMetaSaveTests : ChapterStaticStateTestBase
    {
        private string _tempDir;

        [SetUp]
        public void 임시_세이브_폴더를_만든다()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lair_difficulty_" + Guid.NewGuid().ToString("N"));
        }

        [TearDown]
        public void 임시_세이브_폴더를_지운다()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        private static string HeroOf(int linear)
        {
            return "Knight";
        }

        private static DifficultyClearEntry Entry(int stage, int maxDifficulty)
        {
            return new DifficultyClearEntry { Stage = stage, MaxDifficulty = maxDifficulty };
        }

        private static MetaProfile V6(int cleared, int selected = 1)
        {
            return new MetaProfile { Version = 6, ClearedStage = cleared, SelectedStage = selected };
        }

        //# --- 기본값 / v6 로드 ---

        [Test]
        public void 신규_프로필은_Version_8_Easy_빈_행으로_시작하고_현재_버전과_같다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(MetaProfileMigration.CurrentVersion, p.Version);
            Assert.AreEqual(0, p.SelectedDifficulty);
            Assert.IsNotNull(p.DifficultyClears);
            Assert.AreEqual(0, p.DifficultyClears.Count);
        }

        [Test]
        public void 난이도_필드가_없는_v6_JSON은_Easy_빈_리스트로_로드되고_마이그레이션이_8로_승격한다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>("{\"Version\":6,\"ClearedStage\":3,\"SelectedStage\":2}");

            Assert.AreEqual(0, p.SelectedDifficulty);
            Assert.IsNotNull(p.DifficultyClears);
            Assert.AreEqual(0, p.DifficultyClears.Count);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(3, p.ClearedStage);
            Assert.AreEqual(2, p.SelectedStage);
            Assert.AreEqual(0, p.DifficultyClears.Count);
            Assert.AreEqual(1, p.MaxSelectableDifficulty(3, 2), "D-6: 기존 클리어 스테이지는 Normal 오픈");
        }

        [Test]
        public void Store_Load는_v6_세이브_파일을_v8로_올리고_진행값을_보존한다()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), JsonUtility.ToJson(V6(4, 3)));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(11, loaded.Version);
            Assert.AreEqual(4, loaded.ClearedStage);
            Assert.AreEqual(3, loaded.SelectedStage);
            Assert.AreEqual(0, loaded.SelectedDifficulty);
            Assert.AreEqual(0, loaded.DifficultyClears.Count);
        }

        //# --- JSON 라운드트립 ---

        [Test]
        public void JsonUtility_라운드트립은_선택_난이도와_행을_보존한다()
        {
            MetaProfile p = new MetaProfile { ClearedStage = 6, SelectedDifficulty = 2 };
            p.DifficultyClears.Add(Entry(1, 1));
            p.DifficultyClears.Add(Entry(4, 2));

            MetaProfile copy = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(11, copy.Version);
            Assert.AreEqual(2, copy.SelectedDifficulty);
            Assert.AreEqual(2, copy.DifficultyClears.Count);
            Assert.AreEqual(1, copy.GetMaxClearedDifficulty(1));
            Assert.AreEqual(2, copy.GetMaxClearedDifficulty(4));
        }

        [Test]
        public void Store_Save_Load_라운드트립은_챕터_데이터가_없어도_행을_자르지_않는다()
        {
            ChapterDatabase.Current = null;
            MetaProfileStore store = new MetaProfileStore(_tempDir);
            MetaProfile p = new MetaProfile { ClearedStage = 13, SelectedStage = 13, SelectedDifficulty = 1 };
            p.DifficultyClears.Add(Entry(12, 2));
            store.Save(p);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(13, loaded.ClearedStage);
            Assert.AreEqual(1, loaded.SelectedDifficulty);
            Assert.AreEqual(2, loaded.GetMaxClearedDifficulty(12));
        }

        [Test]
        public void Store_Save_Load_라운드트립은_챕터_데이터가_있어도_유효한_행을_보존한다()
        {
            ChapterDatabase.Current = ChapterDataSet.Valid().Build();
            MetaProfileStore store = new MetaProfileStore(_tempDir);
            MetaProfile p = new MetaProfile { ClearedStage = 5, SelectedStage = 3, SelectedDifficulty = 2 };
            p.DifficultyClears.Add(Entry(3, 2));
            store.Save(p);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(2, loaded.SelectedDifficulty, "상한 클램프는 마이그레이션이 아니라 읽는 순간의 몫");
            Assert.AreEqual(2, loaded.GetMaxClearedDifficulty(3));
        }

        //# --- 마이그레이션 정리 규칙 (§3.1) ---

        [Test]
        public void 범위_밖_Stage와_MaxDifficulty_1_미만과_null_행은_제거된다()
        {
            MetaProfile p = V6(10);
            p.DifficultyClears.Add(Entry(-1, 1));
            p.DifficultyClears.Add(Entry(0, 1));
            p.DifficultyClears.Add(Entry(1, 0));
            p.DifficultyClears.Add(Entry(2, -3));
            p.DifficultyClears.Add(null);
            p.DifficultyClears.Add(Entry(3, 1));
            p.DifficultyClears.Add(Entry(11, 2));
            p.DifficultyClears.Add(Entry(99, 2));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(3, p.DifficultyClears[0].Stage);
        }

        [Test]
        public void Stage_경계_1과_ClearedStage와_MaxStage는_남고_ClearedStage_초과는_제거된다()
        {
            MetaProfile p = V6(4);
            p.DifficultyClears.Add(Entry(1, 1));
            p.DifficultyClears.Add(Entry(4, 1));
            p.DifficultyClears.Add(Entry(5, 1));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(2, p.DifficultyClears.Count);
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(1));
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(4));
            Assert.AreEqual(0, p.GetMaxClearedDifficulty(5));

            MetaProfile top = V6(10);
            top.DifficultyClears.Add(Entry(10, 2));
            MetaProfileMigration.Apply(top, HeroOf);
            Assert.AreEqual(2, top.GetMaxClearedDifficulty(10), "Stage == MaxStage == ClearedStage");
        }

        [Test]
        public void ClearedStage가_먼저_MaxStage로_잘리고_그_기준으로_행이_정리된다()
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = 99 };
            p.DifficultyClears.Add(Entry(10, 1));
            p.DifficultyClears.Add(Entry(11, 2));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(10, p.ClearedStage);
            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(10, p.DifficultyClears[0].Stage);
        }

        [Test]
        public void 챕터_수가_3으로_늘면_행의_Stage_상한도_15가_된다()
        {
            StageProgress.ConfigureChapterCount(3);
            MetaProfile p = V6(15);
            p.DifficultyClears.Add(Entry(15, 2));
            p.DifficultyClears.Add(Entry(16, 2));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(15, p.DifficultyClears[0].Stage);
        }

        [Test]
        public void 같은_Stage_중복은_MaxDifficulty_최댓값_한_행으로_병합된다()
        {
            MetaProfile p = V6(5);
            p.DifficultyClears.Add(Entry(2, 1));
            p.DifficultyClears.Add(Entry(3, 1));
            p.DifficultyClears.Add(Entry(2, 2));
            p.DifficultyClears.Add(Entry(2, 1));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(2, p.DifficultyClears.Count);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(2));
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(3));
        }

        [TestCase(-1, 0)]
        [TestCase(-99, 0)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(2, 2)]
        [TestCase(7, 7)]
        public void 선택_난이도는_음수만_0으로_보정되고_상한_클램프는_하지_않는다(int selected, int expected)
        {
            MetaProfile p = V6(1);
            p.SelectedDifficulty = selected;

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(expected, p.SelectedDifficulty);
        }

        [Test]
        public void DifficultyClears가_null이면_빈_리스트가_된다()
        {
            MetaProfile p = V6(3);
            p.DifficultyClears = null;

            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(p, HeroOf));

            Assert.IsNotNull(p.DifficultyClears);
            Assert.AreEqual(0, p.DifficultyClears.Count);
        }

        [Test]
        public void 마이그레이션은_난이도_정리에서도_멱등이다()
        {
            MetaProfile once = V6(5);
            once.SelectedDifficulty = -2;
            once.DifficultyClears.Add(Entry(2, 1));
            once.DifficultyClears.Add(Entry(2, 2));
            once.DifficultyClears.Add(Entry(9, 1));
            once.DifficultyClears.Add(Entry(3, 0));
            MetaProfile twice = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(once));

            MetaProfileMigration.Apply(once, HeroOf);
            MetaProfileMigration.Apply(twice, HeroOf);
            MetaProfileMigration.Apply(twice, HeroOf);

            Assert.AreEqual(once.SelectedDifficulty, twice.SelectedDifficulty);
            Assert.AreEqual(once.DifficultyClears.Count, twice.DifficultyClears.Count);
            Assert.AreEqual(2, twice.GetMaxClearedDifficulty(2));
            Assert.AreEqual(1, twice.DifficultyClears.Count);
        }

        [Test]
        public void 마이그레이션은_난이도_외_진행_필드를_건드리지_않는다()
        {
            MetaProfile p = V6(5);
            p.Souls = 77;
            p.TotalRuns = 4;
            p.StageRecords.Add(new StageRecordEntry { Stage = 2, Runs = 3, Wins = 2, BestClearTime = 91.5f });
            p.DifficultyClears.Add(Entry(2, 2));

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(77, p.Souls);
            Assert.AreEqual(4, p.TotalRuns);
            Assert.AreEqual(3, p.StageRecords[0].Runs);
            Assert.AreEqual(91.5f, p.StageRecords[0].BestClearTime, 0.0001f);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(2));
        }

        //# --- 챕터 데이터 미로드 상태 ---

        [Test]
        public void v6_세이브는_챕터_데이터_미로드에서_유효한_행을_유지하고_음수만_보정한다()
        {
            MetaProfile p = V6(14, 13);
            p.SelectedDifficulty = -1;
            p.DifficultyClears.Add(Entry(12, 1));
            p.DifficultyClears.Add(Entry(0, 1));
            p.DifficultyClears.Add(Entry(13, 0));
            p.DifficultyClears.Add(null);

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(14, p.ClearedStage, "기본 MaxStage 10 으로 자르지 않는다");
            Assert.AreEqual(13, p.SelectedStage);
            Assert.AreEqual(1, p.GetMaxClearedDifficulty(12));
            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(0, p.SelectedDifficulty);
        }

        [Test]
        public void 챕터DB_오버로드_null도_v6_진행_범위와_행을_자르지_않는다()
        {
            MetaProfile p = V6(14, 13);
            p.DifficultyClears.Add(Entry(14, 2));

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual(14, p.ClearedStage);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(14));
        }

        [Test]
        public void v7_세이브의_챕터_미로드_호출은_v8로_승격하되_진행값과_난이도를_자르지_않는다()
        {
            MetaProfile p = new MetaProfile { Version = 7, ClearedStage = 13, SelectedDifficulty = 2 };
            p.DifficultyClears.Add(Entry(13, 2));

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(13, p.ClearedStage);
            Assert.AreEqual(2, p.SelectedDifficulty);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(13));
        }

        [Test]
        public void 챕터_데이터_미로드_구버전_v5는_기본_범위로_행도_정리된다()
        {
            MetaProfile p = new MetaProfile { Version = 5, ClearedStage = 99 };
            p.DifficultyClears.Add(Entry(10, 1));
            p.DifficultyClears.Add(Entry(11, 1));

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual(11, p.Version);
            Assert.AreEqual(10, p.ClearedStage);
            Assert.AreEqual(1, p.DifficultyClears.Count);
        }

        //# --- CopyFrom / 클라우드 복원 ---

        [Test]
        public void CopyFrom은_선택_난이도와_행을_복사한다()
        {
            MetaProfile src = new MetaProfile { ClearedStage = 6, SelectedDifficulty = 2 };
            src.DifficultyClears.Add(Entry(2, 1));
            src.DifficultyClears.Add(Entry(5, 2));
            MetaProfile dst = new MetaProfile { SelectedDifficulty = 1 };
            dst.DifficultyClears.Add(Entry(1, 2));

            dst.CopyFrom(src);

            Assert.AreEqual(2, dst.SelectedDifficulty);
            Assert.AreEqual(2, dst.DifficultyClears.Count);
            Assert.AreEqual(1, dst.GetMaxClearedDifficulty(2));
            Assert.AreEqual(2, dst.GetMaxClearedDifficulty(5));
            Assert.AreEqual(0, dst.GetMaxClearedDifficulty(1), "복원은 문서 기준 전체 대체");
        }

        [Test]
        public void CopyFrom은_원본_리스트가_null이면_빈_리스트로_받는다()
        {
            MetaProfile src = new MetaProfile { DifficultyClears = null };
            MetaProfile dst = new MetaProfile();
            dst.DifficultyClears.Add(Entry(1, 1));

            Assert.DoesNotThrow(() => dst.CopyFrom(src));

            Assert.IsNotNull(dst.DifficultyClears);
            Assert.AreEqual(0, dst.DifficultyClears.Count);
        }

        [Test]
        public void v6_형태_문서를_복원하면_Easy_빈_상태가_되고_재적용으로_v8이_된다()
        {
            MetaProfile cloud = JsonUtility.FromJson<MetaProfile>("{\"Version\":6,\"ClearedStage\":5,\"SelectedStage\":5}");
            MetaProfile live = new MetaProfile { SelectedDifficulty = 2 };
            live.DifficultyClears.Add(Entry(3, 2));

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(11, live.Version);
            Assert.AreEqual(0, live.SelectedDifficulty);
            Assert.AreEqual(0, live.DifficultyClears.Count);
        }

        [Test]
        public void 복원_후_재적용은_범위_밖_행을_정리하고_유효한_행은_남긴다()
        {
            MetaProfile cloud = new MetaProfile { Version = 7, ClearedStage = 4, SelectedDifficulty = 2 };
            cloud.DifficultyClears.Add(Entry(2, 2));
            cloud.DifficultyClears.Add(Entry(9, 1));
            cloud.DifficultyClears.Add(Entry(2, 1));
            MetaProfile live = new MetaProfile();

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(1, live.DifficultyClears.Count);
            Assert.AreEqual(2, live.GetMaxClearedDifficulty(2));
            Assert.AreEqual(2, live.SelectedDifficulty);
        }

        [Test]
        public void 복원_후에도_live_인스턴스_참조가_유지된다()
        {
            MetaProfile live = new MetaProfile();
            MetaProfile same = live;
            MetaProfile cloud = new MetaProfile { ClearedStage = 2, SelectedDifficulty = 1 };
            cloud.DifficultyClears.Add(Entry(1, 1));

            live.CopyFrom(cloud);

            Assert.AreSame(same, live);
            Assert.AreEqual(1, same.GetMaxClearedDifficulty(1));
        }
    }
}
