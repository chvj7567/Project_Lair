using System;
using System.IO;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# MetaProfile v4 → v5 마이그레이션 (chapter-system §2.2·§9 E-10·E-15) — 멱등·clamp·클라우드 복원·Store 로드·Current null 경로.
    public class MetaProfileMigrationTests : ChapterStaticStateTestBase
    {
        private string _tempDir;

        [SetUp]
        public void 임시_세이브_폴더를_만든다()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lair_migration_" + Guid.NewGuid().ToString("N"));
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
            return linear > 5 ? "Archer" : "Knight";
        }

        private static MetaProfile V4(int cleared, int selected, string hero = "Knight")
        {
            return new MetaProfile { Version = 4, ClearedStage = cleared, SelectedStage = selected, SelectedHero = hero };
        }

        //# --- 단계 1~3: 버전 / clamp ---

        [TestCase(0, 5, 1)]
        [TestCase(3, 5, 4)]
        [TestCase(5, 5, 5)]
        [TestCase(5, 1, 1)]
        [TestCase(3, 2, 2)]
        public void v4_진행값은_해금_범위_안에서_보존되고_Version이_6이_된다(int cleared, int selected, int expectedSelected)
        {
            MetaProfile p = V4(cleared, selected);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(cleared, p.ClearedStage);
            Assert.AreEqual(expectedSelected, p.SelectedStage);
        }

        [Test]
        public void v4_Cleared5_유저는_2장_S1이_해금된_상태가_된다()
        {
            MetaProfile p = V4(5, 5);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.IsTrue(StageProgress.IsChapterUnlocked(2, p.ClearedStage));
        }

        [TestCase(0, 0)]
        [TestCase(-7, 0)]
        [TestCase(10, 10)]
        [TestCase(11, 10)]
        [TestCase(999, 10)]
        public void ClearedStage는_0에서_MaxStage_사이로_clamp된다(int cleared, int expected)
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = cleared };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(expected, p.ClearedStage);
        }

        [TestCase(0, 3, 1)]
        [TestCase(-4, 3, 1)]
        [TestCase(10, 10, 10)]
        [TestCase(99, 10, 10)]
        [TestCase(8, 7, 8)]
        [TestCase(6, 7, 6)]
        public void SelectedStage는_1에서_MaxPlayable_사이로_clamp된다(int selected, int cleared, int expected)
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = cleared, SelectedStage = selected };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(expected, p.SelectedStage);
        }

        [Test]
        public void ClearedStage는_먼저_clamp되고_그_값으로_Selected_상한이_정해진다()
        {
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = 999, SelectedStage = 999 };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(10, p.ClearedStage);
            Assert.AreEqual(10, p.SelectedStage);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(4)]
        [TestCase(5)]
        public void 구버전은_Version이_6으로_승격된다(int version)
        {
            MetaProfile p = new MetaProfile { Version = version };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(6, p.Version);
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(99)]
        public void 이미_6_이상이면_Version을_낮추지_않는다(int version)
        {
            MetaProfile p = new MetaProfile { Version = version };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(version, p.Version);
        }

        [Test]
        public void 챕터수가_3으로_늘면_clamp_상한도_15가_된다()
        {
            StageProgress.ConfigureChapterCount(3);
            MetaProfile p = new MetaProfile { Version = 4, ClearedStage = 14, SelectedStage = 99 };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(14, p.ClearedStage);
            Assert.AreEqual(15, p.SelectedStage);
        }

        //# --- 단계 4: StageRecords ---

        [Test]
        public void StageRecords는_범위_밖_엔트리만_제거되고_경계값은_남는다()
        {
            MetaProfile p = V4(0, 1);
            int[] stages = { -1, 0, 1, 5, 6, 10, 11, 99 };
            foreach (int stage in stages)
            {
                p.StageRecords.Add(new StageRecordEntry { Stage = stage, Runs = stage });
            }

            MetaProfileMigration.Apply(p, HeroOf);

            int[] kept = new int[p.StageRecords.Count];
            for (int i = 0; i < kept.Length; ++i)
            {
                kept[i] = p.StageRecords[i].Stage;
            }
            CollectionAssert.AreEqual(new[] { 1, 5, 6, 10 }, kept);
        }

        [Test]
        public void StageRecords의_null_엔트리도_제거되고_예외가_없다()
        {
            MetaProfile p = V4(0, 1);
            p.StageRecords.Add(null);
            p.StageRecords.Add(new StageRecordEntry { Stage = 2, Runs = 3 });

            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(p, HeroOf));

            Assert.AreEqual(1, p.StageRecords.Count);
            Assert.AreEqual(3, p.StageRecords[0].Runs);
        }

        [Test]
        public void StageRecords_리스트가_null이어도_예외없다()
        {
            MetaProfile p = V4(0, 1);
            p.StageRecords = null;

            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(p, HeroOf));
        }

        [Test]
        public void 남은_전적의_값은_변하지_않는다()
        {
            MetaProfile p = V4(5, 5);
            p.StageRecords.Add(new StageRecordEntry { Stage = 3, Runs = 4, Wins = 2, BestClearTime = 123.5f });

            MetaProfileMigration.Apply(p, HeroOf);

            StageRecordEntry entry = p.StageRecords[0];
            Assert.AreEqual(4, entry.Runs);
            Assert.AreEqual(2, entry.Wins);
            Assert.AreEqual(123.5f, entry.BestClearTime, 0.0001f);
        }

        //# --- 단계 5: SelectedHero ---

        [Test]
        public void SelectedHero는_선택_스테이지_챕터의_영웅으로_재설정된다()
        {
            MetaProfile ch1 = V4(3, 2, "Archer");
            MetaProfile ch2 = V4(8, 7, "Knight");

            MetaProfileMigration.Apply(ch1, HeroOf);
            MetaProfileMigration.Apply(ch2, HeroOf);

            Assert.AreEqual("Knight", ch1.SelectedHero, "E-15: 챕터 1 스테이지인데 Archer 로 남은 세이브");
            Assert.AreEqual("Archer", ch2.SelectedHero);
        }

        [Test]
        public void 영웅_조회가_clamp_이후_Selected_기준으로_호출된다()
        {
            int queried = -1;
            MetaProfile p = V4(0, 9);

            MetaProfileMigration.Apply(p, linear =>
            {
                queried = linear;
                return "Knight";
            });

            Assert.AreEqual(1, queried);
        }

        [TestCase(null)]
        [TestCase("")]
        public void 영웅_조회가_null이나_빈값이면_SelectedHero를_유지한다(string heroId)
        {
            MetaProfile p = V4(8, 7, "Knight");

            MetaProfileMigration.Apply(p, linear => heroId);

            Assert.AreEqual("Knight", p.SelectedHero);
        }

        [Test]
        public void 영웅_조회_함수가_null이면_영웅_단계만_건너뛰고_나머지는_적용된다()
        {
            MetaProfile p = V4(99, 99, "Archer");

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual("Archer", p.SelectedHero);
            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(10, p.ClearedStage);
        }

        [Test]
        public void 챕터DB가_null이면_영웅은_유지되고_Version과_clamp는_적용된다()
        {
            MetaProfile p = V4(7, 99, "Archer");

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual("Archer", p.SelectedHero);
            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(8, p.SelectedStage);
        }

        [Test]
        public void 챕터DB_오버로드는_DB의_챕터_영웅으로_재설정한다()
        {
            ChapterDatabase db = ChapterDataSet.Valid().Build();
            MetaProfile p = V4(7, 7, "Knight");

            MetaProfileMigration.Apply(p, db);

            Assert.AreEqual("Archer", p.SelectedHero);
        }

        [Test]
        public void 챕터DB에_없는_챕터를_선택하고_있어도_영웅은_유지된다()
        {
            StageProgress.ConfigureChapterCount(3);
            ChapterDatabase db = ChapterDataSet.Valid().Build();
            MetaProfile p = V4(14, 12, "Archer");

            MetaProfileMigration.Apply(p, db);

            Assert.AreEqual(12, p.SelectedStage);
            Assert.AreEqual("Archer", p.SelectedHero, "챕터 3 은 DB 에 없어 GetHeroOfLinear == null");
        }

        //# --- null / 멱등 ---

        [Test]
        public void 프로필이_null이면_예외없이_무시한다()
        {
            Assert.DoesNotThrow(() => MetaProfileMigration.Apply((MetaProfile)null, HeroOf));
            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(null, (ChapterDatabase)null));
        }

        [Test]
        public void 마이그레이션은_멱등이다_두번_적용해도_같은_결과다()
        {
            MetaProfile once = V4(99, 7, "Knight");
            once.StageRecords.Add(new StageRecordEntry { Stage = 2, Runs = 1 });
            once.StageRecords.Add(new StageRecordEntry { Stage = 42, Runs = 1 });
            MetaProfile twice = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(once));

            MetaProfileMigration.Apply(once, HeroOf);
            MetaProfileMigration.Apply(twice, HeroOf);
            MetaProfileMigration.Apply(twice, HeroOf);

            Assert.AreEqual(once.Version, twice.Version);
            Assert.AreEqual(once.ClearedStage, twice.ClearedStage);
            Assert.AreEqual(once.SelectedStage, twice.SelectedStage);
            Assert.AreEqual(once.SelectedHero, twice.SelectedHero);
            Assert.AreEqual(once.StageRecords.Count, twice.StageRecords.Count);
        }

        [Test]
        public void 마이그레이션은_진행과_무관한_필드를_건드리지_않는다()
        {
            MetaProfile p = V4(5, 5);
            p.Souls = 123;
            p.TotalRuns = 9;
            p.TotalWins = 4;
            p.BestClearTime = 77f;
            p.DisplayName = "영주";
            p.AddCardPick("CardA", 3);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(123, p.Souls);
            Assert.AreEqual(9, p.TotalRuns);
            Assert.AreEqual(4, p.TotalWins);
            Assert.AreEqual(77f, p.BestClearTime, 0.0001f);
            Assert.AreEqual("영주", p.DisplayName);
            Assert.AreEqual(3, p.GetCardPickCount("CardA"));
        }

        //# --- 클라우드 복원 경로 (CopyFrom 직후 동일 함수 재적용) ---

        [Test]
        public void v4_클라우드_문서를_CopyFrom한_뒤_재적용하면_v5로_보정된다()
        {
            MetaProfile live = new MetaProfile { Version = 5, DisplayName = "로컬" };
            MetaProfile cloud = V4(5, 10, "Knight");
            cloud.StageRecords.Add(new StageRecordEntry { Stage = 12, Runs = 1 });

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(6, live.Version);
            Assert.AreEqual(5, live.ClearedStage);
            Assert.AreEqual(6, live.SelectedStage, "복원 후 Selected 는 MaxPlayable(6) 이하로 보정");
            Assert.AreEqual("Archer", live.SelectedHero);
            Assert.AreEqual(0, live.StageRecords.Count);
            Assert.AreEqual("로컬", live.DisplayName, "DisplayName 은 복원으로 덮이지 않는다");
        }

        [Test]
        public void CopyFrom은_Version을_그대로_복사하므로_재적용_전에는_4가_남는다()
        {
            MetaProfile live = new MetaProfile();

            live.CopyFrom(V4(2, 2));

            Assert.AreEqual(4, live.Version, "호출부가 CopyFrom 직후 Apply 를 반드시 불러야 하는 이유");
        }

        [Test]
        public void 복원_재적용은_live_인스턴스_참조를_유지한다()
        {
            MetaProfile live = new MetaProfile();
            MetaProfile same = live;

            live.CopyFrom(V4(5, 5));
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreSame(same, live);
            Assert.AreEqual(6, same.Version);
        }

        [Test]
        public void 이미_v5인_클라우드_문서의_복원_재적용은_값을_바꾸지_않는다()
        {
            MetaProfile cloud = new MetaProfile { Version = 5, ClearedStage = 8, SelectedStage = 7, SelectedHero = "Archer" };
            MetaProfile live = new MetaProfile();

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(8, live.ClearedStage);
            Assert.AreEqual(7, live.SelectedStage);
            Assert.AreEqual("Archer", live.SelectedHero);
        }

        //# --- MetaProfileStore.Load 경로 ---

        private void WriteSave(MetaProfile profile)
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), JsonUtility.ToJson(profile));
        }

        [Test]
        public void Store_Load_v4_세이브는_Current가_있으면_영웅까지_재설정된다()
        {
            ChapterDatabase.Current = ChapterDataSet.Valid().Build();
            WriteSave(V4(7, 7, "Knight"));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual("Archer", loaded.SelectedHero);
        }

        [Test]
        public void Store_Load_v4_세이브는_Current가_null이면_영웅만_건너뛰고_Version은_6이_된다()
        {
            ChapterDatabase.Current = null;
            WriteSave(V4(7, 7, "Knight"));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual("Knight", loaded.SelectedHero);
            Assert.AreEqual(7, loaded.SelectedStage);
        }

        [Test]
        public void Store_Load_v4_세이브의_범위_밖_전적은_제거된다()
        {
            MetaProfile v4 = V4(5, 5);
            v4.StageRecords.Add(new StageRecordEntry { Stage = 3, Runs = 1 });
            v4.StageRecords.Add(new StageRecordEntry { Stage = 11, Runs = 1 });
            WriteSave(v4);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(1, loaded.StageRecords.Count);
            Assert.AreEqual(3, loaded.StageRecords[0].Stage);
        }

        [Test]
        public void Store_Load_v4_세이브는_Cleared5_Selected5가_보존된다()
        {
            WriteSave(V4(5, 5));

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(5, loaded.ClearedStage);
            Assert.AreEqual(5, loaded.SelectedStage);
        }

        [Test]
        public void Store_Load_v6_세이브는_마이그레이션을_건너뛰어_영웅이_그대로다()
        {
            ChapterDatabase.Current = ChapterDataSet.Valid().Build();
            MetaProfile v6 = new MetaProfile { Version = 6, ClearedStage = 7, SelectedStage = 7, SelectedHero = "Knight" };
            WriteSave(v6);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual("Knight", loaded.SelectedHero, "Version >= 6 는 Load 에서 재적용하지 않는다(현행 동작)");
        }

        [Test]
        public void Store_Load_v5_세이브는_진행값을_보존한_채_v6으로_승격되고_영웅은_챕터_영웅으로_재설정된다()
        {
            ChapterDatabase.Current = ChapterDataSet.Valid().Build();
            MetaProfile v5 = new MetaProfile { Version = 5, ClearedStage = 7, SelectedStage = 7, SelectedHero = "Knight" };
            WriteSave(v5);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual(7, loaded.ClearedStage);
            Assert.AreEqual(7, loaded.SelectedStage);
            Assert.AreEqual("Archer", loaded.SelectedHero);
        }

        [Test]
        public void v5_세이브의_클리어_10은_챕터3이_로드된_뒤에도_그대로_보존된다()
        {
            StageProgress.ConfigureChapterCount(3);
            MetaProfile p = new MetaProfile { Version = 5, ClearedStage = 10, SelectedStage = 10 };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(6, p.Version);
            Assert.AreEqual(10, p.ClearedStage);
            Assert.AreEqual(10, p.SelectedStage);
        }

        [Test]
        public void v6_재적용은_챕터3_진행값_15를_보존하고_16은_15로_clamp한다()
        {
            StageProgress.ConfigureChapterCount(3);
            MetaProfile ok = new MetaProfile { Version = 6, ClearedStage = 15, SelectedStage = 15 };
            MetaProfile over = new MetaProfile { Version = 6, ClearedStage = 16, SelectedStage = 99 };
            over.StageRecords.Add(new StageRecordEntry { Stage = 15, Runs = 1 });
            over.StageRecords.Add(new StageRecordEntry { Stage = 16, Runs = 1 });

            MetaProfileMigration.Apply(ok, HeroOf);
            MetaProfileMigration.Apply(over, HeroOf);
            MetaProfileMigration.Apply(over, HeroOf);

            Assert.AreEqual(15, ok.ClearedStage);
            Assert.AreEqual(15, ok.SelectedStage);
            Assert.AreEqual(15, over.ClearedStage);
            Assert.AreEqual(15, over.SelectedStage);
            Assert.AreEqual(1, over.StageRecords.Count, "멱등 — 범위 밖 전적(16)만 제거");
            Assert.AreEqual(15, over.StageRecords[0].Stage);
        }

        [Test]
        public void Store_Load_v4_후_Save하면_디스크에_Version_6이_기록된다()
        {
            WriteSave(V4(2, 2));
            MetaProfileStore store = new MetaProfileStore(_tempDir);

            store.Save(store.Load());
            MetaProfile reloaded = JsonUtility.FromJson<MetaProfile>(File.ReadAllText(Path.Combine(_tempDir, MetaProfileStore.FileName)));

            Assert.AreEqual(6, reloaded.Version);
        }

        [Test]
        public void Store_Load_세이브_파일이_없으면_새_프로필이_v5다()
        {
            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual(0, loaded.ClearedStage);
            Assert.AreEqual(1, loaded.SelectedStage);
        }

        [Test]
        public void Version_필드가_없는_구_세이브_JSON도_Version_6_기본값으로_로드된다()
        {
            Directory.CreateDirectory(_tempDir);
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), "{\"ClearedStage\":3,\"SelectedStage\":2}");

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(6, loaded.Version);
            Assert.AreEqual(3, loaded.ClearedStage);
            Assert.AreEqual(2, loaded.SelectedStage);
        }
    }
}
