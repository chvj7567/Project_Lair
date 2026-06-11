using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SoulRewardCalculator 엣지 — 비정상 입력 클램프 회귀 박제 (기획서 §2.1 / §10.2).
    public class SoulRewardCalculatorEdgeTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.WinBaseSouls = 100;
            _cfg.WinTimeBonusPerSec = 0.5f;
            _cfg.LoseMaxSouls = 60;
            _cfg.WinXp = 100;
            _cfg.LoseXp = 40;
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 승리_사망시각이_총시간을_넘으면_잔여보너스_0으로_클램프된다()
        {
            //# 시계 오차 등으로 deathTime > totalSeconds — 음수 잔여가 보상을 깎으면 안 된다.
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, deathTime: 320f, totalSeconds: 300f, heroDamagedRatio: 1f, cfg: _cfg);
            Assert.AreEqual(100, r.Souls);
            Assert.AreEqual(100, r.Xp);
        }

        [Test]
        public void 승리_총시간이_0이면_기본보상만_지급된다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, 0f, totalSeconds: 0f, heroDamagedRatio: 1f, cfg: _cfg);
            Assert.AreEqual(100, r.Souls);
        }

        [Test]
        public void 승리_총시간이_음수여도_기본보상만_지급된다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, 10f, totalSeconds: -50f, heroDamagedRatio: 1f, cfg: _cfg);
            Assert.AreEqual(100, r.Souls);
        }

        [Test]
        public void 패배_깎은비율_1초과는_상한_60으로_클램프된다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, heroDamagedRatio: 1.5f, cfg: _cfg);
            Assert.AreEqual(60, r.Souls);
        }

        [Test]
        public void 패배_깎은비율_음수는_0으로_클램프되고_XP는_지급된다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, heroDamagedRatio: -0.5f, cfg: _cfg);
            Assert.AreEqual(0, r.Souls);
            Assert.AreEqual(40, r.Xp);
        }

        [Test]
        public void 결과_None은_패배와_동일하게_정산된다()
        {
            //# EndBattle 중복 가드로 None 정산은 정상 흐름에서 불가 — 도달 시 패배 분기로 떨어지는 현행 동작 박제.
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.None, 100f, 300f, heroDamagedRatio: 0.5f, cfg: _cfg);
            Assert.AreEqual(30, r.Souls);
            Assert.AreEqual(40, r.Xp);
        }

        [Test]
        public void 설정이_null이면_0보상이다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, 100f, 300f, 1f, cfg: null);
            Assert.AreEqual(0, r.Souls);
            Assert.AreEqual(0, r.Xp);
        }
    }
}
