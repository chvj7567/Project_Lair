using System;
using System.Collections.Generic;
using System.IO;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 — MetaProfile v8 (relics §3.2, §4, §12.6 "MetaProfile"): 마이그레이션 · CopyFrom · JSON · 헬퍼 · 클라우드 복원 재적용.
    public class RelicMetaProfileTests : ChapterStaticStateTestBase
    {
        private string _tempDir;

        [SetUp]
        public void 임시_세이브_폴더를_정한다()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "lair_relic_" + Guid.NewGuid().ToString("N"));
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

        private static void Add(MetaProfile p, string id, int stack)
        {
            p.Relics.Add(new RelicEntry { RelicId = id, Stack = stack });
        }

        //# --- 기본값 / 헬퍼 ---

        [Test]
        public void 신규_프로필은_유물_없음_연패_0_배속_꺼짐_Version_8이다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(8, p.Version);
            Assert.IsNotNull(p.Relics);
            Assert.AreEqual(0, p.Relics.Count);
            Assert.AreEqual(0, p.RelicMissStreak);
            Assert.IsFalse(p.BattleSpeedOn);
        }

        [Test]
        public void AddRelic은_없으면_Stack_1로_추가하고_있으면_1씩_올리며_추가_후_중첩을_돌려준다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(1, p.AddRelic("SoulPurse"));
            Assert.AreEqual(2, p.AddRelic("SoulPurse"));
            Assert.AreEqual(1, p.AddRelic("LordsTome"));
            Assert.AreEqual(2, p.Relics.Count);
            Assert.AreEqual(2, p.GetRelicStack("SoulPurse"));
            Assert.AreEqual(1, p.GetRelicStack("LordsTome"));
        }

        [Test]
        public void AddRelic은_상한을_판정하지_않는다_호출부_책임()
        {
            MetaProfile p = new MetaProfile();

            for (int i = 0; i < 9; ++i)
            {
                p.AddRelic("HeartOfStone");
            }

            Assert.AreEqual(9, p.GetRelicStack("HeartOfStone"));
        }

        [Test]
        public void AddRelic은_null이나_빈_id를_무시한다()
        {
            MetaProfile p = new MetaProfile();

            Assert.AreEqual(0, p.AddRelic(null));
            Assert.AreEqual(0, p.AddRelic(string.Empty));
            Assert.AreEqual(0, p.Relics.Count);
        }

        [Test]
        public void AddRelic은_Relics가_null이면_리스트를_만들고_null_행을_건너뛴다()
        {
            MetaProfile p = new MetaProfile { Relics = null };
            Assert.AreEqual(1, p.AddRelic("SoulPurse"));

            MetaProfile withNull = new MetaProfile();
            withNull.Relics.Add(null);
            Assert.AreEqual(1, withNull.AddRelic("SoulPurse"));
            Assert.AreEqual(2, withNull.Relics.Count);
        }

        [Test]
        public void GetRelicStack은_없는_id_null_리스트_null_행에서_0이고_부수효과가_없다()
        {
            MetaProfile p = new MetaProfile();
            p.Relics.Add(null);

            Assert.AreEqual(0, p.GetRelicStack("SoulPurse"));
            Assert.AreEqual(0, p.GetRelicStack(null));
            Assert.AreEqual(1, p.Relics.Count, "조회는 리스트를 바꾸지 않는다");
            p.Relics = null;
            Assert.AreEqual(0, p.GetRelicStack("SoulPurse"));
        }

        //# --- v7 → v8 ---

        [Test]
        public void v7_JSON은_유물_필드가_없어도_로드되고_마이그레이션이_진행값을_보존한채_v8로_올린다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>("{\"Version\":7,\"Souls\":5,\"ClearedStage\":3,\"SelectedStage\":2,\"SelectedDifficulty\":1}");

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(8, p.Version);
            Assert.AreEqual(5, p.Souls);
            Assert.AreEqual(3, p.ClearedStage);
            Assert.AreEqual(2, p.SelectedStage);
            Assert.AreEqual(1, p.SelectedDifficulty);
            Assert.AreEqual(0, p.Relics.Count);
            Assert.AreEqual(0, p.RelicMissStreak);
            Assert.IsFalse(p.BattleSpeedOn);
        }

        [Test]
        public void Store_Load는_v7_세이브를_v8로_올리고_더러운_유물_행을_정리한다()
        {
            Directory.CreateDirectory(_tempDir);
            string json = "{\"Version\":7,\"ClearedStage\":4,\"SelectedStage\":4,"
                + "\"Relics\":[{\"RelicId\":\"\",\"Stack\":2},{\"RelicId\":\"SwiftBoots\",\"Stack\":2},{\"RelicId\":\"SwiftBoots\",\"Stack\":4},{\"RelicId\":\"LordsTome\",\"Stack\":0}],"
                + "\"RelicMissStreak\":-2}";
            File.WriteAllText(Path.Combine(_tempDir, MetaProfileStore.FileName), json);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(8, loaded.Version);
            Assert.AreEqual(1, loaded.Relics.Count);
            Assert.AreEqual(4, loaded.GetRelicStack("SwiftBoots"));
            Assert.AreEqual(0, loaded.RelicMissStreak);
            Assert.AreEqual(4, loaded.ClearedStage);
        }

        //# --- 챕터 DB 미로드 상태 ---

        [Test]
        public void 챕터_미로드_v8도_유물_정리는_수행하고_진행값은_자르지_않는다()
        {
            MetaProfile p = new MetaProfile { Version = 8, ClearedStage = 13, SelectedStage = 13, RelicMissStreak = -1 };
            Add(p, "", 3);
            Add(p, "SoulPurse", 2);

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(13, p.ClearedStage, "기본 MaxStage(10) 로 자르지 않는다");
            Assert.AreEqual(13, p.SelectedStage);
            Assert.AreEqual(1, p.Relics.Count);
            Assert.AreEqual(0, p.RelicMissStreak);
        }

        [Test]
        public void 챕터_미로드_v7은_v8로_올리되_범위_보정을_건너뛰고_유물을_정리한다()
        {
            MetaProfile p = new MetaProfile { Version = 7, ClearedStage = 13, SelectedStage = 13 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 12, MaxDifficulty = 2 });
            Add(p, "HeartOfStone", 1);
            Add(p, "HeartOfStone", 3);

            MetaProfileMigration.Apply(p, (ChapterDatabase)null);

            Assert.AreEqual(8, p.Version);
            Assert.AreEqual(13, p.ClearedStage);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(12));
            Assert.AreEqual(3, p.GetRelicStack("HeartOfStone"));
            Assert.AreEqual(1, p.Relics.Count);
        }

        [Test]
        public void profile이_null이면_예외_없이_무시한다()
        {
            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(null, (ChapterDatabase)null));
            Assert.DoesNotThrow(() => MetaProfileMigration.Apply(null, HeroOf));
        }

        //# --- 정리 규칙 (relics §4.1) ---

        [Test]
        public void 정리_null_행과_null_또는_빈_RelicId_행은_제거된다()
        {
            MetaProfile p = new MetaProfile();
            p.Relics.Add(null);
            Add(p, null, 2);
            Add(p, "", 2);
            Add(p, "SoulPurse", 1);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(1, p.Relics.Count);
            Assert.AreEqual("SoulPurse", p.Relics[0].RelicId);
        }

        [Test]
        public void 정리_Stack_0과_음수_행은_제거되고_1은_남는다()
        {
            MetaProfile p = new MetaProfile();
            Add(p, "A", 0);
            Add(p, "B", -5);
            Add(p, "C", 1);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(1, p.Relics.Count);
            Assert.AreEqual("C", p.Relics[0].RelicId);
        }

        [Test]
        public void 정리_같은_id_중복은_Stack_최댓값_한_행으로_병합된다()
        {
            MetaProfile p = new MetaProfile();
            Add(p, "SwiftBoots", 2);
            Add(p, "SwiftBoots", 4);
            Add(p, "SwiftBoots", 3);
            Add(p, "LordsTome", 5);
            Add(p, "LordsTome", 1);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(2, p.Relics.Count);
            Assert.AreEqual(4, p.GetRelicStack("SwiftBoots"));
            Assert.AreEqual(5, p.GetRelicStack("LordsTome"), "큰 값이 앞에 와도 최댓값");
        }

        [Test]
        public void 정리_병합_후에도_첫_등장_순서가_유지된다()
        {
            MetaProfile p = new MetaProfile();
            Add(p, "B", 1);
            Add(p, "A", 1);
            Add(p, "B", 2);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual("B", p.Relics[0].RelicId);
            Assert.AreEqual("A", p.Relics[1].RelicId);
        }

        [Test]
        public void 정리_Relics가_null이면_빈_리스트가_된다()
        {
            MetaProfile p = new MetaProfile { Relics = null };

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.IsNotNull(p.Relics);
            Assert.AreEqual(0, p.Relics.Count);
        }

        [Test]
        public void 정리_음수_연패는_0으로_보정되고_양수와_0은_보존된다()
        {
            MetaProfile negative = new MetaProfile { RelicMissStreak = -7 };
            MetaProfile positive = new MetaProfile { RelicMissStreak = 3 };
            MetaProfile zero = new MetaProfile { RelicMissStreak = 0 };

            MetaProfileMigration.Apply(negative, HeroOf);
            MetaProfileMigration.Apply(positive, HeroOf);
            MetaProfileMigration.Apply(zero, HeroOf);

            Assert.AreEqual(0, negative.RelicMissStreak);
            Assert.AreEqual(3, positive.RelicMissStreak);
            Assert.AreEqual(0, zero.RelicMissStreak);
        }

        [Test]
        public void 정리는_유물_id_존재와_maxStack_초과를_건드리지_않는다()
        {
            MetaProfile p = new MetaProfile();
            Add(p, "GhostRelic", 7);
            Add(p, "HeartOfStone", 99);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(7, p.GetRelicStack("GhostRelic"), "relics §3.6 — 되돌릴 때 복구");
            Assert.AreEqual(99, p.GetRelicStack("HeartOfStone"), "읽기 지점이 클램프한다");
        }

        [Test]
        public void 정리는_다른_필드를_건드리지_않는다()
        {
            MetaProfile p = new MetaProfile { Souls = 123, LordXp = 45, BattleSpeedOn = true, SelectedDifficulty = 2, ClearedStage = 6 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 3, MaxDifficulty = 2 });
            Add(p, "A", 0);

            MetaProfileMigration.Apply(p, HeroOf);

            Assert.AreEqual(123, p.Souls);
            Assert.AreEqual(45, p.LordXp);
            Assert.IsTrue(p.BattleSpeedOn);
            Assert.AreEqual(2, p.SelectedDifficulty);
            Assert.AreEqual(2, p.GetMaxClearedDifficulty(3));
        }

        [Test]
        public void 마이그레이션은_멱등이다()
        {
            MetaProfile p = new MetaProfile { Version = 7, RelicMissStreak = -1 };
            Add(p, "SwiftBoots", 2);
            Add(p, "SwiftBoots", 4);
            Add(p, "", 1);
            Add(p, "LordsTome", 0);

            MetaProfileMigration.Apply(p, HeroOf);
            string once = JsonUtility.ToJson(p);
            MetaProfileMigration.Apply(p, HeroOf);
            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(once, JsonUtility.ToJson(p));
        }

        //# --- JSON 라운드트립 / Store ---

        [Test]
        public void JsonUtility_라운드트립은_유물_연패_배속을_모두_보존한다()
        {
            MetaProfile p = new MetaProfile { RelicMissStreak = 3, BattleSpeedOn = true };
            Add(p, "SoulPurse", 2);
            Add(p, "KingsCrownShard", 1);

            MetaProfile copy = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));

            Assert.AreEqual(8, copy.Version);
            Assert.AreEqual(2, copy.Relics.Count);
            Assert.AreEqual(2, copy.GetRelicStack("SoulPurse"));
            Assert.AreEqual(1, copy.GetRelicStack("KingsCrownShard"));
            Assert.AreEqual(3, copy.RelicMissStreak);
            Assert.IsTrue(copy.BattleSpeedOn);
        }

        [Test]
        public void Store_Save_Load_라운드트립은_챕터_미로드에서도_유물을_그대로_보존한다()
        {
            ChapterDatabase.Current = null;
            MetaProfileStore store = new MetaProfileStore(_tempDir);
            MetaProfile p = new MetaProfile { ClearedStage = 13, RelicMissStreak = 2, BattleSpeedOn = true };
            Add(p, "HeartOfStone", 99);
            Add(p, "GhostRelic", 4);
            store.Save(p);

            MetaProfile loaded = new MetaProfileStore(_tempDir).Load();

            Assert.AreEqual(8, loaded.Version);
            Assert.AreEqual(13, loaded.ClearedStage);
            Assert.AreEqual(99, loaded.GetRelicStack("HeartOfStone"), "저장은 비클램프");
            Assert.AreEqual(4, loaded.GetRelicStack("GhostRelic"));
            Assert.AreEqual(2, loaded.RelicMissStreak);
            Assert.IsTrue(loaded.BattleSpeedOn);
        }

        //# --- CopyFrom / 클라우드 복원 (relics §4.2) ---

        [Test]
        public void CopyFrom은_유물과_연패를_문서_기준으로_전체_대체한다()
        {
            MetaProfile src = new MetaProfile { RelicMissStreak = 4 };
            Add(src, "SoulPurse", 2);
            MetaProfile dst = new MetaProfile { RelicMissStreak = 1 };
            Add(dst, "LordsTome", 5);

            dst.CopyFrom(src);

            Assert.AreEqual(2, dst.GetRelicStack("SoulPurse"));
            Assert.AreEqual(0, dst.GetRelicStack("LordsTome"), "복원은 전체 대체");
            Assert.AreEqual(4, dst.RelicMissStreak);
        }

        [Test]
        public void CopyFrom은_BattleSpeedOn을_복사하지_않고_양방향_모두_로컬값을_유지한다()
        {
            MetaProfile offToOn = new MetaProfile { BattleSpeedOn = true };
            offToOn.CopyFrom(new MetaProfile { BattleSpeedOn = false });
            MetaProfile onToOff = new MetaProfile { BattleSpeedOn = false };
            onToOff.CopyFrom(new MetaProfile { BattleSpeedOn = true });

            Assert.IsTrue(offToOn.BattleSpeedOn);
            Assert.IsFalse(onToOff.BattleSpeedOn);
        }

        [Test]
        public void CopyFrom은_원본_Relics가_null이면_빈_리스트로_받는다()
        {
            MetaProfile src = new MetaProfile { Relics = null };
            MetaProfile dst = new MetaProfile();
            Add(dst, "SoulPurse", 1);

            Assert.DoesNotThrow(() => dst.CopyFrom(src));

            Assert.IsNotNull(dst.Relics);
            Assert.AreEqual(0, dst.Relics.Count);
        }

        [Test]
        public void CopyFrom은_other가_null이면_아무것도_바꾸지_않는다()
        {
            MetaProfile dst = new MetaProfile { RelicMissStreak = 3 };
            Add(dst, "SoulPurse", 1);

            dst.CopyFrom(null);

            Assert.AreEqual(1, dst.GetRelicStack("SoulPurse"));
            Assert.AreEqual(3, dst.RelicMissStreak);
        }

        [Test]
        public void v7_문서를_복원하면_유물이_비고_재적용으로_v8이_된다()
        {
            MetaProfile cloud = JsonUtility.FromJson<MetaProfile>("{\"Version\":7,\"ClearedStage\":5,\"SelectedStage\":5}");
            MetaProfile live = new MetaProfile { RelicMissStreak = 3, BattleSpeedOn = true };
            Add(live, "SoulPurse", 2);

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(8, live.Version);
            Assert.AreEqual(0, live.Relics.Count, "v7 문서에는 유물이 없다 — 문서 기준 전체 대체");
            Assert.AreEqual(0, live.RelicMissStreak);
            Assert.IsTrue(live.BattleSpeedOn, "배속은 로컬 유지");
        }

        [Test]
        public void 복원_후_재적용은_클라우드_문서의_더러운_유물_행을_정리한다()
        {
            MetaProfile cloud = new MetaProfile { Version = 8, RelicMissStreak = -4 };
            Add(cloud, "SwiftBoots", 1);
            Add(cloud, "SwiftBoots", 3);
            Add(cloud, "", 2);
            MetaProfile live = new MetaProfile();

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);

            Assert.AreEqual(1, live.Relics.Count);
            Assert.AreEqual(3, live.GetRelicStack("SwiftBoots"));
            Assert.AreEqual(0, live.RelicMissStreak);
        }

        [Test]
        public void 복원한_프로필로_보너스를_다시_집계하면_복원된_유물이_즉시_반영된다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile cloud = new MetaProfile();
            Add(cloud, "SoulPurse", 1);
            MetaProfile live = new MetaProfile();
            RelicBattleBonus before = RelicBattleBonus.From(live, db);

            live.CopyFrom(cloud);
            MetaProfileMigration.Apply(live, HeroOf);
            RelicBattleBonus after = RelicBattleBonus.From(live, db);

            Assert.AreEqual(100, before.ApplyReward(new SoulReward { Souls = 100 }, true).Souls);
            Assert.AreEqual(104, after.ApplyReward(new SoulReward { Souls = 100 }, true).Souls);
        }
    }
}
