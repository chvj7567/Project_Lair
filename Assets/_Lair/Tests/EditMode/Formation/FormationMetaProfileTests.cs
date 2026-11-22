using System.Collections.Generic;
using System.IO;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - MetaProfile v9(Formation) 마이그레이션 · CopyFrom · JSON 라운드트립 · Store.Load 정리 (dungeon-formation §4, §9.1 "MetaProfile").
    //# 챕터 정적상태(ChapterDatabase.Current · ChapterCount)를 베이스가 격리한다 - Store.Load 의 마이그레이션 분기가 Current 를 읽는다.
    public class FormationMetaProfileTests : ChapterStaticStateTestBase
    {
        private string _dir;

        private static string HeroOf(int linearStage)
        {
            return "Knight";
        }

        [SetUp]
        public void 임시_폴더를_만든다()
        {
            _dir = Path.Combine(Path.GetTempPath(), "lair_formation_meta_test");
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
            Directory.CreateDirectory(_dir);
        }

        [TearDown]
        public void 임시_폴더를_지운다()
        {
            if (Directory.Exists(_dir))
            {
                Directory.Delete(_dir, true);
            }
        }

        private static List<FormationSlotEntry> Valid()
        {
            return FormationTestKit.Rows(FormationTestKit.Mismatched());
        }

        private static string RowsJson(params string[] monsters)
        {
            List<string> items = new List<string>();
            for (int i = 0; i < monsters.Length; ++i)
            {
                items.Add("{\"Slot\":" + (i + 1) + ",\"Monster\":\"" + monsters[i] + "\"}");
            }
            return "[" + string.Join(",", items) + "]";
        }

        private void WriteSave(string json)
        {
            File.WriteAllText(Path.Combine(_dir, MetaProfileStore.FileName), json);
        }

        //# --- 신규 / v8 → v9 ---

        [Test]
        public void 신규_프로필은_Version_9에_빈_편성_리스트다()
        {
            MetaProfile p = new MetaProfile();
            Assert.AreEqual(14, p.Version);
            Assert.AreEqual(14, MetaProfileMigration.CurrentVersion);
            Assert.IsNotNull(p.Formation);
            Assert.AreEqual(0, p.Formation.Count);
        }

        [Test]
        public void v8_JSON은_Formation_필드가_없어도_로드되고_마이그레이션이_진행값을_보존한채_v9로_올린다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>("{\"Version\":8,\"Souls\":5,\"ClearedStage\":3,\"SelectedStage\":2,\"SelectedDifficulty\":1}");

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(14, p.Version);
            Assert.AreEqual(5, p.Souls);
            Assert.AreEqual(3, p.ClearedStage);
            Assert.AreEqual(2, p.SelectedStage);
            Assert.AreEqual(1, p.SelectedDifficulty);
            Assert.IsNotNull(p.Formation);
            Assert.AreEqual(0, p.Formation.Count);
        }

        [Test]
        public void 마이그레이션은_null_편성을_빈_리스트로_바꾼다()
        {
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 2, Formation = null };
            MetaProfileMigration.Apply(p, HeroOf);
            Assert.IsNotNull(p.Formation);
            Assert.AreEqual(0, p.Formation.Count);
        }

        [Test]
        public void 마이그레이션은_유효한_6행을_보존한다()
        {
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 2, Formation = Valid() };
            MetaProfileMigration.Apply(p, HeroOf);
            Assert.AreEqual(6, p.Formation.Count);
            Assert.AreEqual("Reaper", p.Formation[0].Monster);
        }

        [Test]
        public void 마이그레이션은_코스트나_종당_최대_위반_6행은_모양이_정상이라_건드리지_않는다()
        {
            List<FormationSlotEntry> overCost = FormationTestKit.Rows(
                EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp);
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 2, Formation = overCost };
            MetaProfileMigration.Apply(p, HeroOf);
            Assert.AreEqual(6, p.Formation.Count, "코스트/종당 검사는 읽는 지점(Resolve) 소유 (§4.1)");
        }

        [Test]
        public void 마이그레이션은_부분_중복_미지_null_행을_빈_리스트로_정리한다()
        {
            List<List<FormationSlotEntry>> bad = new List<List<FormationSlotEntry>>();
            bad.Add(FormationTestKit.Rows(EMonster.Wisp, EMonster.Reaper));

            List<FormationSlotEntry> dup = Valid();
            dup[3].Slot = 1;
            bad.Add(dup);

            List<FormationSlotEntry> unknown = Valid();
            unknown[1].Monster = "Dragon";
            bad.Add(unknown);

            List<FormationSlotEntry> hole = Valid();
            hole[2] = null;
            bad.Add(hole);

            foreach (List<FormationSlotEntry> rows in bad)
            {
                MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 2, Formation = rows };
                MetaProfileMigration.Apply(p, HeroOf);
                Assert.AreEqual(0, p.Formation.Count);
            }
        }

        [Test]
        public void 챕터_DB_미로드_Apply도_편성을_정리하고_진행값을_자르지_않는다()
        {
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 13, SelectedStage = 13, Formation = FormationTestKit.Rows(EMonster.Wisp) };

            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);

            Assert.AreEqual(0, p.Formation.Count);
            Assert.AreEqual(13, p.ClearedStage, "챕터 3 이후 진행값 보존");
            Assert.AreEqual(14, p.Version);
        }

        [Test]
        public void 이미_v9인_프로필도_챕터_DB_없이_편성_정리는_조기_반환보다_먼저_수행된다()
        {
            MetaProfile p = new MetaProfile { Version = 9, ClearedStage = 13, Formation = FormationTestKit.Rows(EMonster.Wisp, EMonster.Reaper) };

            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);

            Assert.AreEqual(0, p.Formation.Count);
            Assert.AreEqual(13, p.ClearedStage);
        }

        [Test]
        public void 마이그레이션과_NormalizeFormation은_멱등이다()
        {
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 2, Formation = Valid() };
            MetaProfileMigration.Apply(p, HeroOf);
            string once = JsonUtility.ToJson(p);
            MetaProfileMigration.Apply(p, HeroOf);
            MetaProfileMigration.Apply(p, (System.Func<int, string>)null);
            MetaProfileMigration.NormalizeFormation(p);
            MetaProfileMigration.NormalizeFormation(p);
            Assert.AreEqual(once, JsonUtility.ToJson(p));

            MetaProfile dirty = new MetaProfile { Version = 8, ClearedStage = 2, Formation = FormationTestKit.Rows(EMonster.Wisp) };
            MetaProfileMigration.Apply(dirty, HeroOf);
            string cleaned = JsonUtility.ToJson(dirty);
            MetaProfileMigration.Apply(dirty, HeroOf);
            Assert.AreEqual(cleaned, JsonUtility.ToJson(dirty));
        }

        [Test]
        public void NormalizeFormation은_0행을_그대로_두고_새_리스트로_바꾸지_않는다()
        {
            MetaProfile p = new MetaProfile();
            List<FormationSlotEntry> before = p.Formation;
            MetaProfileMigration.NormalizeFormation(p);
            Assert.AreSame(before, p.Formation);
        }

        [Test]
        public void NormalizeFormation은_유효한_6행의_객체를_바꾸지_않는다()
        {
            MetaProfile p = new MetaProfile { Formation = Valid() };
            List<FormationSlotEntry> before = p.Formation;
            MetaProfileMigration.NormalizeFormation(p);
            Assert.AreSame(before, p.Formation);
        }

        //# --- CopyFrom ---

        [Test]
        public void CopyFrom은_편성을_깊은_복사하고_원본_변경에_영향받지_않는다()
        {
            MetaProfile src = new MetaProfile { Formation = Valid() };
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);

            Assert.AreEqual(6, dst.Formation.Count);
            Assert.AreNotSame(src.Formation, dst.Formation);
            Assert.AreNotSame(src.Formation[0], dst.Formation[0]);
            src.Formation[0].Monster = "Phantom";
            src.Formation.Clear();
            Assert.AreEqual(6, dst.Formation.Count);
            Assert.AreEqual("Reaper", dst.Formation[0].Monster);
            Assert.AreEqual(1, dst.Formation[0].Slot);
        }

        [Test]
        public void CopyFrom은_대상의_기존_편성을_덮어쓴다()
        {
            MetaProfile src = new MetaProfile();
            MetaProfile dst = new MetaProfile { Formation = Valid() };
            dst.CopyFrom(src);
            Assert.AreEqual(0, dst.Formation.Count, "원본이 0행(기본 편성)이면 대상도 0행");
        }

        [Test]
        public void CopyFrom_null_행은_null로_복사되고_이후_마이그레이션이_정리한다()
        {
            MetaProfile src = new MetaProfile { Formation = Valid() };
            src.Formation[3] = null;
            MetaProfile dst = new MetaProfile();
            dst.CopyFrom(src);
            Assert.AreEqual(6, dst.Formation.Count);
            Assert.IsNull(dst.Formation[3]);

            MetaProfileMigration.Apply(dst, HeroOf);
            Assert.AreEqual(0, dst.Formation.Count, "복원 후 Migration 재적용");
        }

        [Test]
        public void CopyFrom_복원_뒤_Apply는_유효_편성을_유지한다()
        {
            MetaProfile cloud = new MetaProfile { Version = 8, ClearedStage = 3, Formation = Valid() };
            MetaProfile local = new MetaProfile();
            local.CopyFrom(cloud);
            MetaProfileMigration.Apply(local, HeroOf);
            Assert.AreEqual(14, local.Version);
            Assert.AreEqual(6, local.Formation.Count);
        }

        //# --- JSON 라운드트립 ---

        [Test]
        public void JsonUtility_라운드트립은_편성_6행을_보존한다()
        {
            MetaProfile p = new MetaProfile { Formation = Valid() };
            MetaProfile copy = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));
            Assert.AreEqual(14, copy.Version);
            Assert.AreEqual(6, copy.Formation.Count);
            for (int i = 0; i < 6; ++i)
            {
                Assert.AreEqual(p.Formation[i].Slot, copy.Formation[i].Slot);
                Assert.AreEqual(p.Formation[i].Monster, copy.Formation[i].Monster);
            }
        }

        [Test]
        public void JsonUtility_라운드트립은_0행_기본_편성을_보존한다()
        {
            MetaProfile copy = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(new MetaProfile()));
            Assert.IsNotNull(copy.Formation);
            Assert.AreEqual(0, copy.Formation.Count);
        }

        //# --- MetaProfileStore.Load ---

        [Test]
        public void Store_Load는_v8_세이브를_v9로_올리고_부분_행을_정리한다()
        {
            WriteSave("{\"Version\":8,\"ClearedStage\":4,\"SelectedStage\":4,\"Formation\":" + RowsJson("Wisp", "Reaper") + "}");

            MetaProfile loaded = new MetaProfileStore(_dir).Load();

            Assert.AreEqual(14, loaded.Version);
            Assert.AreEqual(0, loaded.Formation.Count);
            Assert.AreEqual(4, loaded.ClearedStage);
        }

        [Test]
        public void Store_Load는_v8_세이브의_유효_편성을_유지한다()
        {
            WriteSave("{\"Version\":8,\"ClearedStage\":4,\"SelectedStage\":4,\"Formation\":" + RowsJson("Reaper", "Reaper", "Hex", "Phantom", "Plague", "Phantom") + "}");

            MetaProfile loaded = new MetaProfileStore(_dir).Load();

            Assert.AreEqual(14, loaded.Version);
            Assert.AreEqual(6, loaded.Formation.Count);
        }

        [Test]
        public void Store_Load는_최신_v9_파일의_손상_편성도_정리한다()
        {
            WriteSave("{\"Version\":9,\"ClearedStage\":4,\"SelectedStage\":4,\"Formation\":" + RowsJson("Wisp", "Reaper", "Hex", "Wraith", "Plague", "Dragon") + "}");

            MetaProfile loaded = new MetaProfileStore(_dir).Load();

            Assert.AreEqual(14, loaded.Version);
            Assert.AreEqual(0, loaded.Formation.Count);
            Assert.AreEqual(4, loaded.ClearedStage, "챕터 DB 없이도 진행값 유지");
        }

        [Test]
        public void Store_Load는_Formation이_없는_v9_파일에서도_빈_리스트다()
        {
            WriteSave("{\"Version\":9,\"ClearedStage\":4,\"SelectedStage\":4}");
            MetaProfile loaded = new MetaProfileStore(_dir).Load();
            Assert.IsNotNull(loaded.Formation);
            Assert.AreEqual(0, loaded.Formation.Count);
        }

        [Test]
        public void Store_Save_Load_라운드트립은_편성을_보존한다()
        {
            MetaProfileStore store = new MetaProfileStore(_dir);
            MetaProfile p = new MetaProfile { ClearedStage = 3, SelectedStage = 3, Formation = Valid() };
            store.Save(p);

            MetaProfile loaded = new MetaProfileStore(_dir).Load();

            Assert.AreEqual(6, loaded.Formation.Count);
            Assert.AreEqual("Reaper", loaded.Formation[1].Monster);
            Assert.AreEqual("Phantom", loaded.Formation[5].Monster);
        }
    }
}
