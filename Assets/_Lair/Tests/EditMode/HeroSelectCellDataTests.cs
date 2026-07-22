using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 영웅 목록 셀 데이터 — 스테이지별 초상/틴트/잠금 매핑 검증 (gameplay-programmer 자체 스모크).
    public class HeroSelectCellDataTests
    {
        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                {
                    UnityEngine.Object.DestroyImmediate(obj);
                }
            }
            _spawned.Clear();
        }

        private Sprite NewSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            _spawned.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            _spawned.Add(sprite);
            return sprite;
        }

        //# 스테이지 1~5 에 서로 다른 틴트를 주입 — 셀 n 이 stage n 을 집는지(인덱스 매핑) 구분하기 위함.
        private static Color[] NewDistinctTints()
        {
            return new[]
            {
                new Color(0.1f, 0f, 0f, 1f),
                new Color(0f, 0.3f, 0f, 1f),
                new Color(0f, 0f, 0.5f, 1f),
                new Color(0.7f, 0.7f, 0f, 1f),
                new Color(0.9f, 0f, 0.9f, 1f),
            };
        }

        //# 프로덕션에 테스트 전용 API 를 두지 않으려 private [SerializeField] _stages 를 reflection 주입 (HeroStageVariantConfigTests 관례).
        private HeroStageVariantConfig NewVariantConfig(Color[] tints)
        {
            HeroStageVariantConfig cfg = ScriptableObject.CreateInstance<HeroStageVariantConfig>();
            _spawned.Add(cfg);
            HeroStageVariant[] stages = new HeroStageVariant[tints.Length];
            for (int i = 0; i < tints.Length; ++i)
            {
                stages[i] = new HeroStageVariant { TintColor = tints[i] };
            }
            TestReflection.SetField(cfg, "_stages", stages);
            return cfg;
        }

        [Test]
        public void 영웅목록_셀n은_스테이지n의_틴트를_받고_잠금스테이지는_어두워진다()
        {
            //# ClearedStage=2 → 1~3 해금, 4·5 잠금 (StageProgress.IsUnlocked).
            MetaProfile profile = new MetaProfile { ClearedStage = 2 };
            Color[] tints = NewDistinctTints();
            HeroStageVariantConfig variantConfig = NewVariantConfig(tints);
            Sprite portrait = NewSprite();

            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(profile, variantConfig, portrait);

            Assert.AreEqual(StageProgress.MaxStage, list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                int stage = i + 1;
                //# 기대값은 주입한 원본 배열에서 뽑는다 — GetStage 재호출로 기대값을 만들면 인덱스 버그를 못 잡는다.
                Color tint = tints[i];
                Assert.AreSame(portrait, list[i].Portrait, $"스테이지 {stage} 셀에 초상 누락");
                if (stage <= 3)
                {
                    Assert.IsFalse(list[i].IsLocked, $"스테이지 {stage} 는 해금이어야 한다");
                    Assert.AreEqual(tint, list[i].PortraitTint, $"스테이지 {stage} 셀이 다른 스테이지 틴트를 집었다");
                    Assert.AreEqual($"스테이지 {stage}", list[i].DisplayName);
                }
                else
                {
                    Assert.IsTrue(list[i].IsLocked, $"스테이지 {stage} 는 잠금이어야 한다");
                    Assert.AreEqual(Color.Lerp(tint, Color.black, HeroSelectPopup.LockedDimRatio), list[i].PortraitTint,
                        $"스테이지 {stage} 잠금 셀 틴트 불일치");
                    Assert.IsTrue(list[i].DisplayName.Contains("잠금"), $"스테이지 {stage} 잠금 표기 누락");
                }
            }
        }

        [Test]
        public void 영웅목록_config가_null이고_진행도0이어도_5셀을_틴트없이_만든다()
        {
            Sprite portrait = NewSprite();

            //# profile null(진행도 0 취급) + variantConfig null → 폴백: 5셀, 1만 해금, 틴트 흰색.
            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(null, null, portrait);

            Assert.AreEqual(StageProgress.MaxStage, list.Count);
            Assert.IsFalse(list[0].IsLocked);
            Assert.AreEqual(Color.white, list[0].PortraitTint);
            for (int i = 1; i < list.Count; ++i)
            {
                Assert.IsTrue(list[i].IsLocked, $"스테이지 {i + 1} 은 잠금이어야 한다");
                Assert.AreSame(portrait, list[i].Portrait, $"스테이지 {i + 1} 잠금 셀도 초상은 유지");
                Assert.AreEqual(Color.Lerp(Color.white, Color.black, HeroSelectPopup.LockedDimRatio), list[i].PortraitTint);
            }
        }
    }
}
