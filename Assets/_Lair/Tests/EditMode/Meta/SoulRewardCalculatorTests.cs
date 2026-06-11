using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 수치는 기획서 docs/design/village-meta-hub.md §2.1 확정값 (Win 100 / 0.5 / Lose 60).
    public class SoulRewardCalculatorTests
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
        public void 승리는_기본보상에_남은시간_보너스를_더한다()
        {
            //# 300초 중 180초에 처치 → 남은 120초 × 0.5 = 60 보너스
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Win, deathTime: 180f, totalSeconds: 300f, heroDamagedRatio: 1f, cfg: _cfg);
            Assert.AreEqual(160, r.Souls);
            Assert.AreEqual(100, r.Xp);
        }

        [Test]
        public void 패배는_영웅HP_깎은_비율에_비례한다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, heroDamagedRatio: 0.6f, cfg: _cfg);
            Assert.AreEqual(36, r.Souls);   //# floor(60 × 0.6)
            Assert.AreEqual(40, r.Xp);
        }

        [Test]
        public void 패배_무피해면_소울_0_XP는_지급된다()
        {
            SoulReward r = SoulRewardCalculator.Calculate(BattleResult.Lose, 300f, 300f, 0f, _cfg);
            Assert.AreEqual(0, r.Souls);
            Assert.AreEqual(40, r.Xp);
        }
    }
}
