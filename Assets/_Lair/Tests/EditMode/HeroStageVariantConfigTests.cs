using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# GetStage 클램프 정본 (hero-stage-variant plan Task 2).
    public class HeroStageVariantConfigTests
    {
        //# 프로덕션에 테스트 전용 메서드를 두지 않으려 private [SerializeField] _stages 를 reflection 주입(프로젝트 관례 TestReflection).
        private static HeroStageVariantConfig MakeConfig(HeroStageVariant[] stages)
        {
            HeroStageVariantConfig cfg = ScriptableObject.CreateInstance<HeroStageVariantConfig>();
            TestReflection.SetField(cfg, "_stages", stages);
            return cfg;
        }

        [Test]
        public void GetStage_는_1미만이면_1스테이지로_클램프한다()
        {
            HeroStageVariantConfig cfg = MakeConfig(new[]
            {
                new HeroStageVariant { ScaleMultiplier = 1f },
                new HeroStageVariant { ScaleMultiplier = 2f },
            });
            Assert.AreEqual(1f, cfg.GetStage(0).ScaleMultiplier);
            Assert.AreEqual(2f, cfg.GetStage(99).ScaleMultiplier);
        }

        [Test]
        public void GetStage_는_빈목록이면_기본_variant를_반환한다()
        {
            HeroStageVariantConfig cfg = MakeConfig(new HeroStageVariant[0]);
            HeroStageVariant v = cfg.GetStage(3);
            Assert.IsNotNull(v);
            Assert.AreEqual(1f, v.ScaleMultiplier);
        }
    }
}
