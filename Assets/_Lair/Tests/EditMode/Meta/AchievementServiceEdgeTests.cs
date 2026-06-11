using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# AchievementService 엣지 — 다중 동시 달성/경계값/멱등 (기존 AchievementServiceTests 의 정상 케이스와 비중복).
    public class AchievementServiceEdgeTests
    {
        private MetaConfig _cfg;
        private MetaProfile _profile;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _profile = new MetaProfile();
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        private RunSummary 승리런(float deathTime, int maxSynergyTier = 0)
            => new RunSummary { Result = BattleResult.Win, DeathTime = deathTime, HeroDamagedRatio = 1f, MaxSynergyTier = maxSynergyTier };

        private void 기획서_5종_세팅()
        {
            //# 기획서 §5.3 — 첫 런 50s 승리 시 5건 동시 달성 시나리오의 대상 과제.
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstRun", Condition = EAchievementCondition.TotalRuns, Threshold = 1, RewardSouls = 20 });
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", Condition = EAchievementCondition.FirstWin, Threshold = 1, RewardSouls = 30 });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win120", Condition = EAchievementCondition.WinUnderSeconds, Threshold = 120f, RewardSouls = 40 });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win90", Condition = EAchievementCondition.WinUnderSeconds, Threshold = 90f, RewardSouls = 60 });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win60", Condition = EAchievementCondition.WinUnderSeconds, Threshold = 60f, RewardSouls = 100 });
        }

        [Test]
        public void 한_런에서_다중_과제가_동시_달성되고_보상이_합산된다()
        {
            기획서_5종_세팅();
            _profile.TotalRuns = 1;
            _profile.TotalWins = 1;

            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(50f), _cfg);

            Assert.AreEqual(5, got.Count);
            Assert.AreEqual(20 + 30 + 40 + 60 + 100, _profile.Souls);
            Assert.AreEqual(5, _profile.AchievedIds.Count);
        }

        [Test]
        public void 시간_조건_경계_정확히_threshold면_미달성이다()
        {
            //# 기획서 §5.3 — DeathTime < Threshold (strict). 정확히 120s 처치는 Win120 미달성.
            기획서_5종_세팅();
            _profile.TotalRuns = 1;
            _profile.TotalWins = 1;

            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(120f), _cfg);

            List<string> ids = got.ConvertAll(a => a.Id);
            CollectionAssert.DoesNotContain(ids, "Win120");
            CollectionAssert.Contains(ids, "FirstWin");
        }

        [Test]
        public void 시너지_조건_경계_threshold와_같으면_달성이다()
        {
            //# 기획서 §5.3 — MaxSynergyTier >= Threshold. Tier2 도달 = SynergyTier2 달성.
            _cfg.Achievements.Add(new AchievementDef { Id = "SynergyTier2", Condition = EAchievementCondition.SynergyTierReached, Threshold = 2, RewardSouls = 50 });
            _profile.TotalRuns = 1;

            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f, maxSynergyTier: 2), _cfg);
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual("SynergyTier2", got[0].Id);
        }

        [Test]
        public void 시너지_조건_threshold_미만이면_미달성이다()
        {
            _cfg.Achievements.Add(new AchievementDef { Id = "SynergyTier2", Condition = EAchievementCondition.SynergyTierReached, Threshold = 2, RewardSouls = 50 });
            _profile.TotalRuns = 1;

            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f, maxSynergyTier: 1), _cfg);
            Assert.AreEqual(0, got.Count);
        }

        [Test]
        public void 도전과제_목록이_비어있으면_아무것도_달성되지_않는다()
        {
            _profile.TotalRuns = 1;
            _profile.TotalWins = 1;

            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(50f), _cfg);
            Assert.AreEqual(0, got.Count);
            Assert.AreEqual(0, _profile.Souls);
        }

        [Test]
        public void 같은_런으로_Evaluate_2회_호출해도_이중지급_없다()
        {
            기획서_5종_세팅();
            _profile.TotalRuns = 1;
            _profile.TotalWins = 1;
            RunSummary run = 승리런(50f);

            AchievementService.Evaluate(_profile, run, _cfg);
            int soulsAfterFirst = _profile.Souls;

            List<AchievementDef> second = AchievementService.Evaluate(_profile, run, _cfg);
            Assert.AreEqual(0, second.Count);
            Assert.AreEqual(soulsAfterFirst, _profile.Souls);
        }

        [Test]
        public void null_입력은_빈_결과를_반환한다()
        {
            Assert.AreEqual(0, AchievementService.Evaluate(null, 승리런(50f), _cfg).Count);
            Assert.AreEqual(0, AchievementService.Evaluate(_profile, null, _cfg).Count);
            Assert.AreEqual(0, AchievementService.Evaluate(_profile, 승리런(50f), null).Count);
        }
    }
}
