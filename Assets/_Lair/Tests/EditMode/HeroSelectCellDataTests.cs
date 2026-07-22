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

        //# ===== 폴백 2종 격리 (village-meta-hub §6.1 "폴백(방어값)") =====
        //# 위 테스트는 두 폴백을 동시에 걸어 검증한다 — 한쪽만 null 일 때 다른 축이 살아있는지는 구분하지 못한다.

        [Test]
        public void 영웅목록_profile만_null이면_진행도0으로_1스테이지만_해금되고_틴트는_유지된다()
        {
            Color[] tints = NewDistinctTints();
            HeroStageVariantConfig variantConfig = NewVariantConfig(tints);
            Sprite portrait = NewSprite();

            //# profile 만 null → cleared 0 취급. variantConfig 는 살아있으므로 틴트 폴백(흰색)은 일어나면 안 된다.
            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(null, variantConfig, portrait);

            Assert.AreEqual(StageProgress.MaxStage, list.Count);
            Assert.IsFalse(list[0].IsLocked, "진행도 0 이면 스테이지 1 만 해금");
            Assert.AreEqual(tints[0], list[0].PortraitTint, "profile 폴백이 틴트까지 흰색으로 덮으면 안 된다");
            for (int i = 1; i < list.Count; ++i)
            {
                Assert.IsTrue(list[i].IsLocked, $"스테이지 {i + 1} 은 잠금이어야 한다");
                Assert.AreEqual(Color.Lerp(tints[i], Color.black, HeroSelectPopup.LockedDimRatio), list[i].PortraitTint,
                    $"스테이지 {i + 1} 잠금 셀도 자기 스테이지 틴트를 어둡게 한 값이어야 한다");
            }
        }

        [Test]
        public void 영웅목록_variantConfig만_null이면_흰색틴트로_폴백해도_해금판정은_유지된다()
        {
            //# ClearedStage=2 → 1~3 해금. variantConfig 만 null → 틴트만 흰색 폴백, 해금 판정은 그대로.
            MetaProfile profile = new MetaProfile { ClearedStage = 2 };
            Sprite portrait = NewSprite();

            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(profile, null, portrait);

            Assert.AreEqual(StageProgress.MaxStage, list.Count);
            for (int i = 0; i < list.Count; ++i)
            {
                int stage = i + 1;
                Assert.AreSame(portrait, list[i].Portrait, $"스테이지 {stage} 셀에 초상 누락");
                if (stage <= 3)
                {
                    Assert.IsFalse(list[i].IsLocked, $"스테이지 {stage} 는 해금이어야 한다 — config 폴백이 해금 판정을 건드리면 안 된다");
                    Assert.AreEqual(Color.white, list[i].PortraitTint, $"스테이지 {stage} 해금 셀은 무채색 폴백");
                }
                else
                {
                    Assert.IsTrue(list[i].IsLocked, $"스테이지 {stage} 는 잠금이어야 한다");
                    Assert.AreEqual(Color.Lerp(Color.white, Color.black, HeroSelectPopup.LockedDimRatio), list[i].PortraitTint,
                        $"스테이지 {stage} 잠금 셀은 흰색을 어둡게 한 값");
                }
            }
        }

        //# ===== 경계값 — 전 스테이지 해금 (잠금 셀 0개) =====

        //# 상단 테스트들은 전부 잠금 셀이 1개 이상인 진행도만 본다 — 잠금 표기가 영구히 남는 회귀를 못 잡는다.
        [Test]
        public void 영웅목록_ClearedStage_4이상이면_5칸_모두_해금되고_잠금표기가_사라진다()
        {
            Color[] tints = NewDistinctTints();
            HeroStageVariantConfig variantConfig = NewVariantConfig(tints);
            Sprite portrait = NewSprite();

            //# IsUnlocked(stage, cleared) = stage <= cleared+1 → cleared 4 에서 이미 스테이지 5 해금. 5 는 전클리어 종점.
            foreach (int cleared in new[] { 4, 5 })
            {
                MetaProfile profile = new MetaProfile { ClearedStage = cleared };

                List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(profile, variantConfig, portrait);

                Assert.AreEqual(StageProgress.MaxStage, list.Count);
                for (int i = 0; i < list.Count; ++i)
                {
                    Assert.IsFalse(list[i].IsLocked, $"cleared={cleared} 에서 스테이지 {i + 1} 은 해금이어야 한다");
                    Assert.AreEqual(tints[i], list[i].PortraitTint, $"cleared={cleared} 해금 셀은 어둠 보간 없이 원 틴트");
                    Assert.AreEqual($"스테이지 {i + 1}", list[i].DisplayName, $"cleared={cleared} 해금 셀에 잠금 표기 잔존");
                }
            }
        }

        //# ===== 표시 문구·계수 회귀 고정 =====

        //# 기존 잠금 표기 단언은 Contains("잠금") 이라 "스테이지 4잠금" 같은 오타도 통과한다 — 글자 단위로 못 박는다.
        [Test]
        public void 영웅목록_표시명이_기획서_7절_문구와_글자단위로_일치한다()
        {
            MetaProfile profile = new MetaProfile { ClearedStage = 2 };
            Sprite portrait = NewSprite();

            List<HeroSelectCellData> list = HeroSelectPopup.BuildCellData(profile, NewVariantConfig(NewDistinctTints()), portrait);

            Assert.AreEqual("스테이지 1", list[0].DisplayName);
            Assert.AreEqual("스테이지 3", list[2].DisplayName);
            //# 구분자는 em dash(—, U+2014) + 앞뒤 공백. 하이픈(-)/en dash(–) 로 바뀌면 실패해야 한다.
            Assert.AreEqual("스테이지 4 — 잠금", list[3].DisplayName);
            Assert.AreEqual("스테이지 5 — 잠금", list[4].DisplayName);
        }

        //# 기존 단언들은 전부 프로덕션 상수 LockedDimRatio 를 기대값 계산에 그대로 써서 값이 바뀌어도 통과한다 — 값 자체를 못 박는다.
        [Test]
        public void 영웅목록_잠금_어둠계수가_기획서_hero_stage_variant_4_3절_알파와_일치한다()
        {
            //# 캐러셀 잠금 오버레이 α0.55 와 같은 톤 (hero-stage-variant.md §4.3 · village-meta-hub.md §6.1 "잠금 칸 표시").
            Assert.AreEqual(0.55f, HeroSelectPopup.LockedDimRatio, 0.0001f);
        }
    }
}
