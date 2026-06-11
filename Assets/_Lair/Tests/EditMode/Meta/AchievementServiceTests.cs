using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class AchievementServiceTests
    {
        private MetaConfig _cfg;
        private MetaProfile _profile;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", DisplayName = "첫 사냥감", Condition = EAchievementCondition.FirstWin, RewardSouls = 30 });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win120", DisplayName = "신속한 처형", Condition = EAchievementCondition.WinUnderSeconds, Threshold = 120f, RewardSouls = 40 });
            _profile = new MetaProfile();
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        private RunSummary 승리런(float deathTime)
            => new RunSummary { Result = BattleResult.Win, DeathTime = deathTime, HeroDamagedRatio = 1f, MaxSynergyTier = 0 };

        [Test]
        public void 첫_승리에_FirstWin이_달성되고_보상이_지급된다()
        {
            _profile.TotalWins = 1;   //# 정산 후 호출 가정 — 이번 런 반영 완료 상태 (기획서 §5.3)
            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f), _cfg);
            Assert.AreEqual(1, got.Count);
            Assert.AreEqual("FirstWin", got[0].Id);
            Assert.AreEqual(30, _profile.Souls);
            Assert.Contains("FirstWin", _profile.AchievedIds);
        }

        [Test]
        public void 이미_달성한_과제는_다시_달성되지_않는다()
        {
            _profile.TotalWins = 2;
            _profile.AchievedIds.Add("FirstWin");
            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(200f), _cfg);
            Assert.AreEqual(0, got.Count);
            Assert.AreEqual(0, _profile.Souls);
        }

        [Test]
        public void 시간_조건은_threshold_미만_승리만_인정한다()
        {
            _profile.TotalWins = 1;
            List<AchievementDef> got = AchievementService.Evaluate(_profile, 승리런(110f), _cfg);
            CollectionAssert.Contains(got.ConvertAll(a => a.Id), "Win120");

            //# 엣지 — 패배 런은 시간 조건 불인정.
            MetaProfile loseProfile = new MetaProfile();
            RunSummary loseRun = new RunSummary { Result = BattleResult.Lose, DeathTime = 110f };
            List<AchievementDef> loseGot = AchievementService.Evaluate(loseProfile, loseRun, _cfg);
            CollectionAssert.DoesNotContain(loseGot.ConvertAll(a => a.Id), "Win120");
        }
    }
}
