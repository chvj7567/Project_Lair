using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.EditMode
{
    //# 스테이지 해금 갱신 + 스탯 배수 정수화 정본 (hero-stage-variant plan Task 6, 기획서 §2.1).
    public class StageProgressionTests
    {
        [Test]
        public void 더_높은_스테이지_클리어시_ClearedStage_갱신되고_5를_넘지_않는다()
        {
            Assert.AreEqual(3, StageProgress.ResolveClearedStage(2, 3));
            Assert.AreEqual(4, StageProgress.ResolveClearedStage(4, 2)); //# 낮은 재도전은 유지
            Assert.AreEqual(5, StageProgress.ResolveClearedStage(5, 5)); //# 5 종점
        }

        [Test]
        public void ScaleStat_은_기획서_HP배수를_정확한_실효HP로_정수화한다()
        {
            Assert.AreEqual(4000, StageProgress.ScaleStat(4000, 1.00f));
            Assert.AreEqual(5000, StageProgress.ScaleStat(4000, 1.25f));
            Assert.AreEqual(6200, StageProgress.ScaleStat(4000, 1.55f));
            Assert.AreEqual(7600, StageProgress.ScaleStat(4000, 1.90f));
            Assert.AreEqual(9200, StageProgress.ScaleStat(4000, 2.30f));
        }

        [Test]
        public void ScaleStat_은_Power_50x1_35를_round_half_up으로_68로_만든다()
        {
            Assert.AreEqual(50, StageProgress.ScaleStat(50, 1.00f));
            Assert.AreEqual(55, StageProgress.ScaleStat(50, 1.10f));
            Assert.AreEqual(60, StageProgress.ScaleStat(50, 1.20f));
            Assert.AreEqual(68, StageProgress.ScaleStat(50, 1.35f)); //# 67.5 → 68 (기획서 §2.1 확정)
            Assert.AreEqual(75, StageProgress.ScaleStat(50, 1.50f));
        }
    }
}
