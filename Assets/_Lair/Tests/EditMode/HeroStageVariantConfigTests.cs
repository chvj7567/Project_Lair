using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# GetStage 클램프 정본 (hero-stage-variant plan Task 2).
    public class HeroStageVariantConfigTests
    {
        private const string ConfigAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";

        //# hero-2d-conversion §11 경고 — Tier 는 C# 기본값이 0 이라, 에셋에 명시하지 않으면 전 스테이지가
        //# 스테이지1 표현(오버레이 off)으로 남는다. 실제 .asset 값을 실측해 회귀 감시.
        [Test]
        public void 실제_asset의_10엔트리는_챕터키와_Tier가_챕터마다_0부터_4까지_채워져있다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(ConfigAssetPath);
            Assert.IsNotNull(config, $"asset 로드 실패: {ConfigAssetPath}");
            Assert.AreEqual(10, config.Stages.Count, "챕터 2개 x 5스테이지 엔트리");

            for (int i = 0; i < config.Stages.Count; i++)
            {
                Assert.AreEqual(i / 5 + 1, config.Stages[i].ChapterId, $"엔트리 {i} 의 ChapterId");
                Assert.AreEqual(i % 5 + 1, config.Stages[i].StageIndex, $"엔트리 {i} 의 StageIndex");
                Assert.AreEqual(i % 5, config.Stages[i].Tier, $"엔트리 {i} 의 Tier == {i % 5}");
            }
        }

        //# 프로덕션에 테스트 전용 메서드를 두지 않으려 private [SerializeField] _stages 를 reflection 주입(프로젝트 관례 TestReflection).
        private static HeroStageVariantConfig MakeConfig(HeroStageVariant[] stages)
        {
            HeroStageVariantConfig cfg = ScriptableObject.CreateInstance<HeroStageVariantConfig>();
            //# 엔트리 순서대로 선형 키(챕터 1: 1~5, 챕터 2: 6~10) 부여 - GetStage 는 키로 조회한다.
            for (int i = 0; stages != null && i < stages.Length; i++)
            {
                stages[i].ChapterId = i / 5 + 1;
                stages[i].StageIndex = i % 5 + 1;
            }
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
