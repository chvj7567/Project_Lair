using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# GetStage 클램프 정본 (hero-stage-variant plan Task 2) + 영웅 외형 조회 클램프 동치성(so-to-json-migration §7.6).
    public class HeroStageVariantConfigTests
    {
        private const string ConfigAssetPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";

        [Test]
        public void 실제_asset의_25엔트리는_챕터키가_챕터마다_1부터_5까지_채워져있다()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(ConfigAssetPath);
            Assert.IsNotNull(config, $"asset 로드 실패: {ConfigAssetPath}");
            Assert.AreEqual(25, config.Stages.Count, "챕터 5개 x 5스테이지 엔트리");

            for (int i = 0; i < config.Stages.Count; i++)
            {
                Assert.AreEqual(i / 5 + 1, config.Stages[i].ChapterId, $"엔트리 {i} 의 ChapterId");
                Assert.AreEqual(i % 5 + 1, config.Stages[i].StageIndex, $"엔트리 {i} 의 StageIndex");
            }
        }

        //# 영웅 외형(heroScale·heroTier)은 ChapterDatabase.GetStageClamped 로 고른다 — 초상 SO 의 클램프 규칙("이하 중 최대, 없으면 최소")과 같은 스테이지를 골라야 한다.
        [TestCase(int.MinValue)]
        [TestCase(-5)]
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(13)]
        [TestCase(25)]
        [TestCase(26)]
        [TestCase(30)]
        [TestCase(int.MaxValue)]
        public void 영웅_외형_클램프는_초상_SO_의_클램프와_같은_스테이지를_고른다(int linear)
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(ConfigAssetPath);
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            HeroStageVariant portraitEntry = config.GetStage(linear);
            StageRow stage = db.GetStageClamped(linear);

            Assert.IsNotNull(stage, $"linear {linear}");
            Assert.AreEqual(portraitEntry.ChapterId, stage.chapterId, $"linear {linear} chapterId");
            Assert.AreEqual(portraitEntry.StageIndex, stage.stageIndex, $"linear {linear} stageIndex");
        }

        [Test]
        public void 영웅_외형_클램프_경계는_1_1과_5_5다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            Assert.AreEqual(1, db.GetStageClamped(0).chapterId);
            Assert.AreEqual(1, db.GetStageClamped(0).stageIndex);
            Assert.AreEqual(5, db.GetStageClamped(26).chapterId);
            Assert.AreEqual(5, db.GetStageClamped(30).stageIndex);
            Assert.AreEqual(1.3f, db.GetStageClamped(30).heroScale, 0f, "5-5 heroScale");
            Assert.AreEqual(4, db.GetStageClamped(30).heroTier, "5-5 heroTier");
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
                new HeroStageVariant(),
                new HeroStageVariant(),
            });
            Assert.AreEqual(1, cfg.GetStage(0).StageIndex);
            Assert.AreEqual(2, cfg.GetStage(99).StageIndex);
        }

        [Test]
        public void GetStage_는_빈목록이면_기본_variant를_반환한다()
        {
            HeroStageVariantConfig cfg = MakeConfig(new HeroStageVariant[0]);
            HeroStageVariant v = cfg.GetStage(3);
            Assert.IsNotNull(v);
            Assert.IsNull(v.Portrait);
        }
    }
}
