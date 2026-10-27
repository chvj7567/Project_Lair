using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 테스트 공용 — shipped JSON 10종으로 RelicDatabase 를 구축한다 (챕터 DB 로 교차 검증 포함).
    public static class RelicTestSupport
    {
        private static T[] Load<T>(string name)
        {
            return JsonArrayUtility.FromJsonArray<T>(File.ReadAllText(WorldMapTestSupport.JsonDir + name + ".json"));
        }

        public static RelicDatabase ShippedDb()
        {
            return RelicDatabase.Build(
                Load<RelicRow>("Relic"), Load<Relic_EffectRow>("Relic_Effect"), Load<Relic_Effect_ParamRow>("Relic_Effect_Param"),
                Load<Relic_DropRow>("Relic_Drop"), Load<RelicRarityRow>("RelicRarity"), Load<RelicStackRow>("RelicStack"),
                JsonUtility.FromJson<RelicConfigRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "RelicConfig.json")),
                Load<Stage_RelicRow>("Stage_Relic"), Load<Difficulty_RelicRow>("Difficulty_Relic"),
                Load<Difficulty_Relic_RarityRow>("Difficulty_Relic_Rarity"),
                WorldMapTestSupport.FullDb(), id => WorldMapTestSupport.Text(id).Length > 0);
        }
    }

    //# gameplay-programmer 자체 테스트(정상 + 엣지 1개) — 본격 스위트는 test-engineer.
    public class RelicSupportAndSelfTests
    {
        [Test]
        public void 출하_유물_데이터_10종이_검증을_통과하고_행_수가_기획서와_같다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();

            Assert.AreEqual(16, db.Relics.Count);
            Assert.AreEqual(75, CountDrops(db));
            Assert.AreEqual(5, db.MaxScaleStack);
        }

        private static int CountDrops(RelicDatabase db)
        {
            int count = 0;
            foreach (string hero in new[] { "Knight", "Archer", "Assassin", "Berserker", "Paladin" })
            {
                count += db.DropsOf(hero).Count;
            }
            return count;
        }

        [Test]
        public void 승리_굴림이_획득하면_유물이_추가되고_연패카운터가_0이_된다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile { RelicMissStreak = 3 };
            profile.AddRelic("HeartOfStone");

            RelicDropResult result = RelicDropRoller.Roll(db, profile, "Knight", 1, 1, 0, true, () => 0f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual(0, profile.RelicMissStreak);
            Assert.GreaterOrEqual(profile.GetRelicStack(result.RelicId), 1);
        }

        [Test]
        public void 후보가_전부_최대_중첩이면_굴리지_않고_연패카운터가_변하지_않는다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile { RelicMissStreak = 2 };
            foreach (Relic_DropRow drop in db.DropsOf("Archer"))
            {
                for (int i = 0; i < db.Get(drop.relicId).MaxStack; ++i)
                {
                    profile.AddRelic(drop.relicId);
                }
            }

            RelicDropResult result = RelicDropRoller.Roll(db, profile, "Archer", 2, 1, 0, true, () => 0f);

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, result.Outcome);
            Assert.AreEqual(2, profile.RelicMissStreak);
        }

        [Test]
        public void 패배_보상은_소울주머니와_위로의깃털을_합산해_한번만_내림한다()
        {
            RelicDatabase db = RelicTestSupport.ShippedDb();
            MetaProfile profile = new MetaProfile();
            profile.AddRelic("SoulPurse");
            profile.AddRelic("ConsolationFeather");

            RelicBattleBonus bonus = RelicBattleBonus.From(profile, db);
            SoulReward reward = bonus.ApplyReward(new SoulReward { Souls = 100, Xp = 50 }, false);

            Assert.AreEqual(129, reward.Souls);   //# 100 x (1 + 0.04 + 0.25)
            Assert.AreEqual(50, reward.Xp);
        }

        [Test]
        public void v7_세이브는_v8로_승격되고_유물_필드가_기본값이다()
        {
            MetaProfile profile = JsonUtility.FromJson<MetaProfile>("{\"Version\":7,\"Souls\":5}");
            MetaProfileMigration.Apply(profile, (System.Func<int, string>)null);

            Assert.AreEqual(9, profile.Version);
            Assert.IsNotNull(profile.Relics);
            Assert.AreEqual(0, profile.Relics.Count);
            Assert.AreEqual(0, profile.RelicMissStreak);
            Assert.IsFalse(profile.BattleSpeedOn);
        }

        [Test]
        public void CopyFrom은_유물과_연패카운터를_복사하고_BattleSpeedOn은_복사하지_않는다()
        {
            MetaProfile src = new MetaProfile { RelicMissStreak = 4, BattleSpeedOn = true };
            src.AddRelic("SoulPurse");
            src.AddRelic("SoulPurse");
            MetaProfile dst = new MetaProfile { BattleSpeedOn = false };

            dst.CopyFrom(src);

            Assert.AreEqual(2, dst.GetRelicStack("SoulPurse"));
            Assert.AreEqual(4, dst.RelicMissStreak);
            Assert.IsFalse(dst.BattleSpeedOn);
        }

        [Test]
        public void 마이그레이션은_잘못된_유물_행을_정리하고_중복은_최댓값으로_합친다()
        {
            MetaProfile profile = new MetaProfile { RelicMissStreak = -3 };
            profile.Relics.Add(null);
            profile.Relics.Add(new RelicEntry { RelicId = "", Stack = 2 });
            profile.Relics.Add(new RelicEntry { RelicId = "LordsTome", Stack = 0 });
            profile.Relics.Add(new RelicEntry { RelicId = "SwiftBoots", Stack = 2 });
            profile.Relics.Add(new RelicEntry { RelicId = "SwiftBoots", Stack = 4 });

            MetaProfileMigration.Apply(profile, (System.Func<int, string>)null);

            Assert.AreEqual(1, profile.Relics.Count);
            Assert.AreEqual(4, profile.GetRelicStack("SwiftBoots"));
            Assert.AreEqual(0, profile.RelicMissStreak);
        }

        [Test]
        public void 배속2에서_Pause_Resume을_거쳐도_목표_속도로_복원된다()
        {
            float prev = Time.timeScale;
            try
            {
                PauseService pause = new PauseService();
                pause.SetTargetScale(2f);
                pause.Pause();
                Assert.AreEqual(0f, Time.timeScale);
                pause.SetTargetScale(1f);   //# 정지 중 변경은 보관만
                Assert.AreEqual(0f, Time.timeScale);
                pause.SetTargetScale(2f);
                pause.Resume();
                Assert.AreEqual(2f, Time.timeScale);
                pause.ResetAll();
                Assert.AreEqual(1f, Time.timeScale);
            }
            finally
            {
                Time.timeScale = prev;
            }
        }
    }
}
