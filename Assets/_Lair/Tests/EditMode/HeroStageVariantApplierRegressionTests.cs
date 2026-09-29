using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# Apply 복리 방지 + 발광 override + null 가드 회귀 (hero-2d-conversion §11 — TintColor/아웃라인 제거 후 갱신).
    //# 단발 스케일/배타성은 HeroStageVariantApplierTests 가 커버 — 여기선 반복 Apply·발광·null 가드.
    public class HeroStageVariantApplierRegressionTests
    {
        private GameObject _go;
        private HeroStageVariantApplier _applier;
        private SpriteRenderer _body;
        private MonsterTierOverlay _tierOverlay;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Hero");
            GameObject visual2D = new GameObject("Visual2D");
            visual2D.transform.SetParent(_go.transform, false);
            _body = visual2D.AddComponent<SpriteRenderer>();
            _body.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));

            GameObject enhance = new GameObject("Enhance");
            enhance.transform.SetParent(visual2D.transform, false);
            SpriteRenderer overlayRenderer = enhance.AddComponent<SpriteRenderer>();
            overlayRenderer.sharedMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            _tierOverlay = enhance.AddComponent<MonsterTierOverlay>();
            _tierOverlay.SetOverlayRendererForTest(overlayRenderer);

            _applier = _go.AddComponent<HeroStageVariantApplier>();
            TestReflection.SetField(_applier, "_body", _body);
            TestReflection.SetField(_applier, "_tierOverlay", _tierOverlay);
            TestReflection.SetField(_applier, "_emissionTargets", new[] { _body, overlayRenderer });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static void AssertVec3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f);
            Assert.AreEqual(expected.y, actual.y, 1e-4f);
            Assert.AreEqual(expected.z, actual.z, 1e-4f);
        }

        //# Apply() 가 내부적으로 renderer.material(non-shared getter) 을 호출해 EditMode 전용 벤더 경고를 낸다
        //# (몸·오버레이 렌더러 각 1회, 최초 접근 시에만 — production 코드 관점에선 올바른 호출: Mat_Monster2D 는
        //# 영웅·몬스터 6종이 공유하므로 인스턴스화 없이 sharedMaterial 을 직접 건드리면 다른 캐릭터까지 오염된다).
        //# 테스트 안에서 Apply 를 여러 번 불러도 두 번째부터는 이미 인스턴스화돼 재경고되지 않으므로 2회로 고정.
        private static void ExpectMaterialInstantiationWarnings()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Instantiating material"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Instantiating material"));
        }

        [Test]
        public void Apply를_2회_반복해도_스케일이_복리로_누적되지_않는다()
        {
            ExpectMaterialInstantiationWarnings();
            //# 복리면 2×2=4배. baseScale 1회 캐시로 항상 base×mul 이어야 한다.
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 2f, Tier = 0 });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale);

            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 2f, Tier = 0 });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale); //# 4배 아님
        }

        [Test]
        public void 스테이지_전환시_이전_스케일이_리셋되어_base기준으로_재계산된다()
        {
            ExpectMaterialInstantiationWarnings();
            //# 5스테이지(1.4배) → 1스테이지(1.0배) 재사용. 이전 확대가 남으면 안 된다.
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.4f, Tier = 4 });
            AssertVec3(new Vector3(1.4f, 1.4f, 1.4f), _go.transform.localScale);

            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.0f, Tier = 0 });
            AssertVec3(new Vector3(1f, 1f, 1f), _go.transform.localScale); //# base 로 복귀
        }

        [Test]
        public void 발광_스테이지는_EmissionColor에_색x강도가_들어간다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant
            {
                UseEmission = true,
                EmissionColor = new Color(0f, 1f, 0f, 1f),
                EmissionIntensity = 2f,
                ScaleMultiplier = 1f,
                Tier = 2,
            });
            Color e = _body.material.GetColor("_EmissionColor");
            //# (0,1,0,1) × 2 = (0,2,0,2). RGB 채널만 확인.
            Assert.AreEqual(0f, e.r, 1e-4f);
            Assert.AreEqual(2f, e.g, 1e-4f);
            Assert.AreEqual(0f, e.b, 1e-4f);
        }

        [Test]
        public void 비발광_스테이지는_잔존_발광을_검정으로_명시적으로_덮는다()
        {
            ExpectMaterialInstantiationWarnings();
            //# 기획서 §1.5 회귀 — 발광 스테이지 후 비발광 스테이지로 전환 시 발광이 새지 않아야 한다.
            _applier.Apply(new HeroStageVariant
            {
                UseEmission = true,
                EmissionColor = new Color(0f, 1f, 0f, 1f),
                EmissionIntensity = 2f,
                ScaleMultiplier = 1f,
                Tier = 2,
            });
            _applier.Apply(new HeroStageVariant { UseEmission = false, ScaleMultiplier = 1f, Tier = 0 });

            Color e = _body.material.GetColor("_EmissionColor");
            Assert.AreEqual(0f, e.r, 1e-4f);
            Assert.AreEqual(0f, e.g, 1e-4f); //# 이전 발광 잔존 없음
            Assert.AreEqual(0f, e.b, 1e-4f);
        }

        //# 스테이지1↔스테이지5 왕복 전환에서도 몸/오버레이 배타성(§6.4.1)이 매번 정확히 갱신된다.
        [Test]
        public void 스테이지1에서_스테이지5로_전환시_몸이_꺼지고_오버레이_티어가_바뀐다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1f, Tier = 0 });
            Assert.IsTrue(_body.enabled);

            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.4f, Tier = 4 });
            Assert.IsFalse(_body.enabled, "§6.4.1 — Tier>0 은 오버레이만");
            Assert.AreEqual(4, _tierOverlay.CurrentTierForTest);
        }

        [Test]
        public void Apply는_variant가_null이면_상태를_바꾸지_않고_예외없다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 2f, Tier = 4 });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale);

            Assert.DoesNotThrow(() => _applier.Apply(null));
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale); //# 불변
            Assert.IsFalse(_body.enabled, "null Apply 로 상태가 바뀌지 않음 — 직전(Tier4) 유지");
        }
    }
}
