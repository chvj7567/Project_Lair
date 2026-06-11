using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# §11.4 정산 순서 계약 통합 — 보상 가산 → 영주 자동 수령 → 도전과제 판정 순서가 결과를 만드는지.
    //# BattleController.EndBattle 의 Meta 코어 호출 순서를 그대로 시뮬레이트 (씬 구동 불요, MetaSession 미사용).
    public class MetaSettlementOrderTests
    {
        private const string AssetPath = "Assets/_Lair/Data/MetaConfig.asset";

        private struct SettleResult
        {
            public int SoulsGained;
            public int XpGained;
            public int LordLevelUp;        //# 레벨 업 시 신규 레벨, 아니면 0 (기획서 §9.2 줄 생략 키)
            public int LordRewardSouls;
            public List<AchievementDef> NewlyAchieved;
        }

        //# BattleController.EndBattle 정산부와 동일 순서 — 순서가 바뀌면 본 스위트의 기대값이 깨진다.
        private static SettleResult Settle(MetaProfile profile, MetaConfig cfg, BattleResult result,
            float deathTime, float totalSeconds, float damagedRatio, int maxSynergyTier = 0)
        {
            SoulReward reward = SoulRewardCalculator.Calculate(result, deathTime, totalSeconds, damagedRatio, cfg);

            int levelBefore = LordLevelService.LevelFromXp(profile.LordXp, cfg);
            profile.Souls += reward.Souls;
            profile.LordXp += reward.Xp;
            profile.TotalRuns++;
            if (result == BattleResult.Win)
            {
                profile.TotalWins++;
                if (profile.BestClearTime < 0f || deathTime < profile.BestClearTime)
                {
                    profile.BestClearTime = deathTime;
                }
            }

            int levelAfter = LordLevelService.LevelFromXp(profile.LordXp, cfg);
            int lordRewardSouls = LordLevelService.ClaimRewards(profile, cfg);

            RunSummary summary = new RunSummary
            {
                Result = result,
                DeathTime = deathTime,
                HeroDamagedRatio = damagedRatio,
                MaxSynergyTier = maxSynergyTier,
            };
            List<AchievementDef> achieved = AchievementService.Evaluate(profile, summary, cfg);

            return new SettleResult
            {
                SoulsGained = reward.Souls,
                XpGained = reward.Xp,
                LordLevelUp = levelAfter > levelBefore ? levelAfter : 0,
                LordRewardSouls = lordRewardSouls,
                NewlyAchieved = achieved,
            };
        }

        private static MetaConfig 실에셋()
        {
            MetaConfig cfg = AssetDatabase.LoadAssetAtPath<MetaConfig>(AssetPath);
            Assert.IsNotNull(cfg, $"MetaConfig.asset 이 {AssetPath} 에 있어야 함");
            return cfg;
        }

        [Test]
        public void 첫_승리_정산은_런보상_영주보상_도전과제가_한_번에_합산된다()
        {
            //# 기획서 §2.4 첫 런 시나리오 (76s 승리) + §4.4 자동 수령 + §5.3 동시 달성.
            //# 런 212 + 영주 Lv2 보상 50 + 도전과제(FirstRun 20 + FirstWin 30 + Win120 40 + Win90 60) = 412.
            MetaConfig cfg = 실에셋();
            MetaProfile p = new MetaProfile();

            SettleResult r = Settle(p, cfg, BattleResult.Win, deathTime: 76f, totalSeconds: 300f, damagedRatio: 1f);

            Assert.AreEqual(212, r.SoulsGained);
            Assert.AreEqual(100, r.XpGained);
            Assert.AreEqual(2, r.LordLevelUp);
            Assert.AreEqual(50, r.LordRewardSouls);
            Assert.AreEqual(4, r.NewlyAchieved.Count);
            Assert.AreEqual(412, p.Souls);
            Assert.AreEqual(1, p.TotalRuns);
            Assert.AreEqual(1, p.TotalWins);
            Assert.AreEqual(76f, p.BestClearTime, 0.0001f);
            Assert.AreEqual(2, p.LordRewardGrantedLevel);
        }

        [Test]
        public void 도전과제_TotalWins는_이번_런_가산_후_판정된다()
        {
            //# 순서 계약 핵심 — Evaluate 가 TotalWins++ 전에 불리면 FirstWin 이 첫 승리에 미달성된다.
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.WinBaseSouls = 100;
            cfg.WinXp = 100;
            cfg.XpPerLevelBase = 100;
            cfg.XpGrowth = 1.25f;
            cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", Condition = EAchievementCondition.FirstWin, RewardSouls = 30 });
            MetaProfile p = new MetaProfile();   //# TotalWins 0 — 정산 전 상태.

            SettleResult r = Settle(p, cfg, BattleResult.Win, 100f, 300f, 1f);

            Assert.AreEqual(1, r.NewlyAchieved.Count);
            Assert.AreEqual("FirstWin", r.NewlyAchieved[0].Id);

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void 무피해_패배_정산은_소울0_XP40에_FirstRun만_달성한다()
        {
            //# 기획서 §10.2 — 첫 런 빈손 방어선: 소울 0 이어도 FirstRun 20 + XP 40 은 전진.
            MetaConfig cfg = 실에셋();
            MetaProfile p = new MetaProfile();

            SettleResult r = Settle(p, cfg, BattleResult.Lose, deathTime: 300f, totalSeconds: 300f, damagedRatio: 0f);

            Assert.AreEqual(0, r.SoulsGained);
            Assert.AreEqual(40, r.XpGained);
            Assert.AreEqual(0, r.LordLevelUp);        //# XP 40 < 100 — 레벨 업 없음 → 영주 줄 생략 키.
            Assert.AreEqual(0, r.LordRewardSouls);
            Assert.AreEqual(1, r.NewlyAchieved.Count);
            Assert.AreEqual("FirstRun", r.NewlyAchieved[0].Id);
            Assert.AreEqual(20, p.Souls);
            Assert.AreEqual(0, p.TotalWins);
            Assert.AreEqual(-1f, p.BestClearTime, 0.0001f);   //# 패배는 최단 기록 미갱신.
        }

        [Test]
        public void 두_번째_정산은_이미_달성한_보상을_이중지급하지_않는다()
        {
            MetaConfig cfg = 실에셋();
            MetaProfile p = new MetaProfile();

            Settle(p, cfg, BattleResult.Win, 76f, 300f, 1f);
            Assert.AreEqual(412, p.Souls);

            //# 2런째 동일 승리 — XP 200 은 아직 Lv2 (Lv3 경계 225 미만) → 영주 보상 0, 신규 과제 0.
            SettleResult second = Settle(p, cfg, BattleResult.Win, 76f, 300f, 1f);

            Assert.AreEqual(212, second.SoulsGained);
            Assert.AreEqual(0, second.LordLevelUp);
            Assert.AreEqual(0, second.LordRewardSouls);
            Assert.AreEqual(0, second.NewlyAchieved.Count);
            Assert.AreEqual(412 + 212, p.Souls);
            Assert.AreEqual(2, p.TotalWins);
        }

        [Test]
        public void 한_정산에서_2레벨_점프는_구간보상을_합산_지급한다()
        {
            //# 기획서 §4.4 — Lv1→Lv3 점프 시 Lv2(50) + Lv3(80) 합산, ResultPopup 은 최종 레벨 1줄.
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.WinBaseSouls = 0;
            cfg.WinTimeBonusPerSec = 0f;
            cfg.WinXp = 225;   //# 한 런에 Lv3 경계 정확 도달.
            cfg.XpPerLevelBase = 100;
            cfg.XpGrowth = 1.25f;
            cfg.LordRewards.Add(new LordRewardDef { Level = 2, IsLockedDummy = false, RewardSouls = 50, DisplayName = "영주의 첫 봉급" });
            cfg.LordRewards.Add(new LordRewardDef { Level = 3, IsLockedDummy = false, RewardSouls = 80, DisplayName = "던전 운영비" });
            MetaProfile p = new MetaProfile();

            SettleResult r = Settle(p, cfg, BattleResult.Win, 100f, 300f, 1f);

            Assert.AreEqual(3, r.LordLevelUp);
            Assert.AreEqual(130, r.LordRewardSouls);
            Assert.AreEqual(130, p.Souls);
            Assert.AreEqual(3, p.LordRewardGrantedLevel);

            Object.DestroyImmediate(cfg);
        }
    }
}
