using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# XP 곡선 = floor(100 × 1.25^(Lv−1)) — 기획서 §4.1 / §4.2 표와 동일.
    public class LordLevelServiceTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.XpPerLevelBase = 100;
            _cfg.XpGrowth = 1.25f;
            //# 필요 XP: Lv1→2 = 100, Lv2→3 = 125, Lv3→4 = 156(floor)
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test] public void XP_0은_레벨1이다()      => Assert.AreEqual(1, LordLevelService.LevelFromXp(0, _cfg));
        [Test] public void XP_100은_레벨2다()      => Assert.AreEqual(2, LordLevelService.LevelFromXp(100, _cfg));
        [Test] public void XP_224는_아직_레벨2다() => Assert.AreEqual(2, LordLevelService.LevelFromXp(224, _cfg));
        [Test] public void XP_225는_레벨3이다()    => Assert.AreEqual(3, LordLevelService.LevelFromXp(225, _cfg));

        [Test]
        public void 진행률은_현재_레벨_구간_내_비율이다()
        {
            //# Lv2 구간(100~225) 의 중간 162.5 근처 → 0.5
            Assert.AreEqual(0.5f, LordLevelService.ProgressInLevel(162, _cfg), 0.01f);
        }

        //# ===== 영주 보상 자동 수령 (기획서 §4.4) =====

        private void 보상트랙_세팅()
        {
            _cfg.LordRewards.Add(new LordRewardDef { Level = 2, IsLockedDummy = false, RewardSouls = 50, DisplayName = "영주의 첫 봉급" });
            _cfg.LordRewards.Add(new LordRewardDef { Level = 3, IsLockedDummy = false, RewardSouls = 80, DisplayName = "던전 운영비" });
            _cfg.LordRewards.Add(new LordRewardDef { Level = 4, IsLockedDummy = true, RewardSouls = 0, DisplayName = "???" });
        }

        [Test]
        public void 레벨업_구간_보상을_합산_지급하고_멱등하다()
        {
            보상트랙_세팅();
            //# Lv1 → Lv3 (XP 225): Lv2(50) + Lv3(80) 합산 지급.
            MetaProfile p = new MetaProfile { LordXp = 225 };
            int granted = LordLevelService.ClaimRewards(p, _cfg);
            Assert.AreEqual(130, granted);
            Assert.AreEqual(130, p.Souls);
            Assert.AreEqual(3, p.LordRewardGrantedLevel);

            //# 같은 상태에서 재호출 — 이중 지급 없음 (멱등 가드).
            Assert.AreEqual(0, LordLevelService.ClaimRewards(p, _cfg));
            Assert.AreEqual(130, p.Souls);
        }

        [Test]
        public void 잠금_더미_레벨만_오르면_소울_0이다()
        {
            보상트랙_세팅();
            //# Lv3 까지 수령 완료 상태에서 Lv4(잠금 더미) 도달 — 줄 유지·+N 소울 생략 케이스 (기획서 §9.2).
            MetaProfile p = new MetaProfile { LordXp = 381, LordRewardGrantedLevel = 3 };
            Assert.AreEqual(4, LordLevelService.LevelFromXp(381, _cfg));
            Assert.AreEqual(0, LordLevelService.ClaimRewards(p, _cfg));
            Assert.AreEqual(4, p.LordRewardGrantedLevel);
        }

        //# ===== 트랙 만렙 게이지 고정 (기획서 §10.7 — LordRewards 최고 Level 기준, 하드코딩 금지) =====

        [Test]
        public void 트랙_만렙_도달시_게이지가_1로_고정된다()
        {
            보상트랙_세팅();   //# 트랙 만렙 = Lv4 (LordRewards 최고 Level)
            //# XP 381 = 정확히 Lv4 진입 — Lv5 구간 진행률(0)로 리셋되면 안 되고 1f 고정.
            Assert.AreEqual(4, LordLevelService.LevelFromXp(381, _cfg));
            Assert.AreEqual(1f, LordLevelService.ProgressInLevel(381, _cfg));
        }

        [Test]
        public void 트랙_만렙_초과_XP에서도_게이지가_1을_유지한다()
        {
            보상트랙_세팅();
            Assert.AreEqual(1f, LordLevelService.ProgressInLevel(10000, _cfg));
        }

        [Test]
        public void 트랙_만렙_미만에서는_구간_진행률을_유지한다()
        {
            보상트랙_세팅();
            //# Lv2 구간(필요 125) 중간 62 소모 — 트랙 세팅이 만렙 미만 진행률에 영향 없음.
            Assert.AreEqual(0.5f, LordLevelService.ProgressInLevel(162, _cfg), 0.01f);
        }
    }
}
