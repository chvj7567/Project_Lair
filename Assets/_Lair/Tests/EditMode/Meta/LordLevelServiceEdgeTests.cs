using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# LordLevelService 엣지 — 비정상 XP/곡선 붕괴/트랙 경계 (기존 LordLevelServiceTests 의 정상·자동수령 케이스와 비중복).
    public class LordLevelServiceEdgeTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.XpPerLevelBase = 100;
            _cfg.XpGrowth = 1.25f;
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 음수_XP는_레벨1_진행률0이다()
        {
            Assert.AreEqual(1, LordLevelService.LevelFromXp(-50, _cfg));
            Assert.AreEqual(0f, LordLevelService.ProgressInLevel(-50, _cfg));
        }

        [Test]
        public void 선형곡선_XpGrowth_1에서도_레벨은_상한_99에서_멈춘다()
        {
            //# growth 1.0 → 레벨당 100 고정. XP 20000 = 200레벨 분량이지만 MaxLevel 99 가드에서 정지.
            _cfg.XpGrowth = 1.0f;
            Assert.AreEqual(LordLevelService.MaxLevel, LordLevelService.LevelFromXp(20000, _cfg));
        }

        [Test]
        public void 선형곡선_XpGrowth_1은_레벨당_100으로_누적된다()
        {
            _cfg.XpGrowth = 1.0f;
            Assert.AreEqual(3, LordLevelService.LevelFromXp(250, _cfg));
            Assert.AreEqual(0.5f, LordLevelService.ProgressInLevel(250, _cfg), 0.001f);
        }

        [Test]
        public void 붕괴곡선_XpGrowth_0에서도_무한루프_없이_상한에서_멈춘다()
        {
            //# growth 0 → Lv2 부터 필요 XP 0 — 감산 루프가 MaxLevel 가드 없이는 무한 진행하는 코너.
            _cfg.XpGrowth = 0f;
            Assert.AreEqual(1, LordLevelService.LevelFromXp(99, _cfg));
            Assert.AreEqual(LordLevelService.MaxLevel, LordLevelService.LevelFromXp(100, _cfg));
        }

        [Test]
        public void 거대_XP에서도_레벨_계산이_상한_안에서_끝난다()
        {
            //# int.MaxValue XP — 1.25 곡선 누적상 약 Lv69 부근. 오버플로/행 없이 1~99 범위 반환만 보장.
            int level = LordLevelService.LevelFromXp(int.MaxValue, _cfg);
            Assert.GreaterOrEqual(level, 50);
            Assert.LessOrEqual(level, LordLevelService.MaxLevel);
            float progress = LordLevelService.ProgressInLevel(int.MaxValue, _cfg);
            Assert.GreaterOrEqual(progress, 0f);
            Assert.LessOrEqual(progress, 1f);
        }

        [Test]
        public void 빈_보상트랙이면_TrackMaxLevel은_0이고_게이지는_고정되지_않는다()
        {
            Assert.AreEqual(0, LordLevelService.TrackMaxLevel(_cfg));
            //# 트랙 미정의 시 게이지 1f 고정 없이 구간 진행률 유지.
            Assert.AreEqual(0.5f, LordLevelService.ProgressInLevel(162, _cfg), 0.01f);
        }

        [Test]
        public void 단일_트랙이면_그_레벨이_만렙이고_도달시_게이지_1이다()
        {
            _cfg.LordRewards.Add(new LordRewardDef { Level = 2, IsLockedDummy = false, RewardSouls = 50, DisplayName = "영주의 첫 봉급" });
            Assert.AreEqual(2, LordLevelService.TrackMaxLevel(_cfg));
            Assert.AreEqual(1f, LordLevelService.ProgressInLevel(100, _cfg));
        }

        [Test]
        public void 트랙에_null_항목이_섞여도_무시된다()
        {
            _cfg.LordRewards.Add(null);
            _cfg.LordRewards.Add(new LordRewardDef { Level = 3, IsLockedDummy = false, RewardSouls = 80, DisplayName = "던전 운영비" });
            Assert.AreEqual(3, LordLevelService.TrackMaxLevel(_cfg));
            //# ClaimRewards 도 null 항목을 건너뛴다.
            MetaProfile p = new MetaProfile { LordXp = 225 };
            Assert.AreEqual(80, LordLevelService.ClaimRewards(p, _cfg));
        }

        [Test]
        public void ClaimRewards_프로필이_null이면_0이다()
        {
            Assert.AreEqual(0, LordLevelService.ClaimRewards(null, _cfg));
        }

        [Test]
        public void 트랙_정의_너머_레벨업은_지급할_보상이_없다()
        {
            _cfg.LordRewards.Add(new LordRewardDef { Level = 2, IsLockedDummy = false, RewardSouls = 50, DisplayName = "영주의 첫 봉급" });
            //# Lv2 보상 수령 완료 후 Lv3 도달 — 트랙 밖 레벨이라 0 지급, 멱등 가드만 전진.
            MetaProfile p = new MetaProfile { LordXp = 225, LordRewardGrantedLevel = 2 };
            Assert.AreEqual(0, LordLevelService.ClaimRewards(p, _cfg));
            Assert.AreEqual(3, p.LordRewardGrantedLevel);
        }
    }
}
