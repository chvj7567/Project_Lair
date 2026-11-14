using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# GetStage 클램프 엣지 — 음수/int.Max/종점 경계 (hero-stage-variant plan Task 2, 기획서 §5).
    //# 0/99/빈목록은 HeroStageVariantConfigTests 가 이미 커버 — 여기선 경계 확장만.
    public class HeroStageVariantConfigEdgeTests
    {
        private static HeroStageVariantConfig MakeConfig(HeroStageVariant[] stages)
        {
            HeroStageVariantConfig cfg = ScriptableObject.CreateInstance<HeroStageVariantConfig>();
            //# 엔트리 순서대로 선형 키(1~5 = 챕터 1) 부여 - GetStage 는 (챕터, 인덱스) 키로 조회한다.
            for (int i = 0; stages != null && i < stages.Length; i++)
            {
                stages[i].ChapterId = i / 5 + 1;
                stages[i].StageIndex = i % 5 + 1;
            }
            TestReflection.SetField(cfg, "_stages", stages);
            return cfg;
        }

        //# 엔트리 구분은 StageIndex(1~5) 로 한다.
        private static HeroStageVariant[] FiveStages()
        {
            return new[]
            {
                new HeroStageVariant(),
                new HeroStageVariant(),
                new HeroStageVariant(),
                new HeroStageVariant(),
                new HeroStageVariant(),
            };
        }

        [Test]
        public void GetStage_는_음수면_1스테이지로_클램프한다()
        {
            HeroStageVariantConfig cfg = MakeConfig(FiveStages());
            Assert.AreEqual(1, cfg.GetStage(-100).StageIndex);
        }

        [Test]
        public void GetStage_는_int_MaxValue면_마지막_스테이지로_클램프한다()
        {
            HeroStageVariantConfig cfg = MakeConfig(FiveStages());
            //# 5스테이지 목록에서 int.Max → 5번째 반환. 오버플로 없음.
            Assert.AreEqual(5, cfg.GetStage(int.MaxValue).StageIndex);
        }

        [Test]
        public void GetStage_5스테이지_경계_1과5는_정확히_해당스테이지_6은_5로_클램프()
        {
            HeroStageVariantConfig cfg = MakeConfig(FiveStages());
            Assert.AreEqual(1, cfg.GetStage(1).StageIndex); //# 하한 정확
            Assert.AreEqual(5, cfg.GetStage(5).StageIndex); //# 상한 정확
            Assert.AreEqual(5, cfg.GetStage(6).StageIndex); //# 종점 초과 → 5로 클램프
        }

        [Test]
        public void GetStage_는_null_stages여도_기본variant를_반환한다()
        {
            //# _stages 미할당(null) 상태 — 빈배열과 별개 분기(NRE 방지).
            HeroStageVariantConfig cfg = MakeConfig(null);
            HeroStageVariant v = cfg.GetStage(1);
            Assert.IsNotNull(v);
            Assert.IsNull(v.Portrait);
        }
    }
}
