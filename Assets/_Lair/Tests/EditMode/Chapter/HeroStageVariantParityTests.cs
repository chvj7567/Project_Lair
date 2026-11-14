using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# SJ1 일회용 패리티 (so-to-json-migration §10, C1 커밋 ①) — HeroStageVariantConfig SO 25 엔트리 vs Stage.json heroScale·heroTier.
    //# Portrait(Sprite)는 SO 에 남으므로 비교 대상 아님. 커밋 ② 에서 SO 필드를 지울 때 이 파일도 삭제한다.
    public class HeroStageVariantParityTests
    {
        private const string ConfigPath = "Assets/_Lair/Data/HeroStageVariantConfig.asset";

        private static HeroStageVariantConfig Config()
        {
            HeroStageVariantConfig config = AssetDatabase.LoadAssetAtPath<HeroStageVariantConfig>(ConfigPath);
            Assert.IsNotNull(config, "HeroStageVariantConfig 에셋 없음");
            return config;
        }

        private static StageRow[] Stages()
        {
            return JsonArrayUtility.FromJsonArray<StageRow>(File.ReadAllText(WorldMapTestSupport.JsonDir + "Stage.json"));
        }

        [Test]
        public void SO_25엔트리와_Stage_JSON_25행의_챕터_스테이지_키가_일치한다()
        {
            HashSet<string> so = new HashSet<string>();
            foreach (HeroStageVariant variant in Config().Stages)
            {
                Assert.IsTrue(so.Add($"{variant.ChapterId}-{variant.StageIndex}"), $"SO 키 중복 {variant.ChapterId}-{variant.StageIndex}");
            }
            HashSet<string> json = new HashSet<string>();
            foreach (StageRow row in Stages())
            {
                json.Add($"{row.chapterId}-{row.stageIndex}");
            }

            Assert.AreEqual(25, so.Count, "SO 엔트리 수");
            Assert.AreEqual(25, json.Count, "Stage.json 행 수");
            Assert.IsTrue(so.SetEquals(json), "SO 와 Stage.json 의 (챕터, 스테이지) 키 집합이 다름");
        }

        [Test]
        public void SO_ScaleMultiplier_와_heroScale_이_float_오차_없이_같다()
        {
            Dictionary<string, StageRow> json = ByKey();
            foreach (HeroStageVariant variant in Config().Stages)
            {
                StageRow row = json[$"{variant.ChapterId}-{variant.StageIndex}"];
                Assert.AreEqual(variant.ScaleMultiplier, row.heroScale, 0f, $"{variant.ChapterId}-{variant.StageIndex} heroScale");
            }
        }

        [Test]
        public void SO_Tier_와_heroTier_가_같다()
        {
            Dictionary<string, StageRow> json = ByKey();
            foreach (HeroStageVariant variant in Config().Stages)
            {
                StageRow row = json[$"{variant.ChapterId}-{variant.StageIndex}"];
                Assert.AreEqual(variant.Tier, row.heroTier, $"{variant.ChapterId}-{variant.StageIndex} heroTier");
            }
        }

        private static Dictionary<string, StageRow> ByKey()
        {
            Dictionary<string, StageRow> map = new Dictionary<string, StageRow>();
            foreach (StageRow row in Stages())
            {
                map[$"{row.chapterId}-{row.stageIndex}"] = row;
            }
            return map;
        }
    }
}
