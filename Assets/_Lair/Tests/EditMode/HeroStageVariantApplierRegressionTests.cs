using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using Lair.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# Apply 복리 방지 + 발광 override + fallback 회귀 (hero-stage-variant plan Task 4·5, 기획서 §1.5).
    //# 틴트/스케일 단발 반영은 HeroStageVariantApplierTests 가 커버 — 여기선 반복 Apply/스테이지 전환/발광/fallback.
    public class HeroStageVariantApplierRegressionTests
    {
        private GameObject _go;
        private HitFlash _flash;
        private HeroStageVariantApplier _applier;
        private Renderer _renderer;

        [SetUp]
        public void SetUp()
        {
            //# EditMode 에서 renderer.material 접근 시 Unity 가 [Error] 벤더 로그를 뱉는다(런타임엔 정상 API).
            //# 이 로그로 SetUp 이 실패하지 않도록 톨러런스 — TearDown 에서 원복해 다른 테스트로 누출 방지.
            LogAssert.ignoreFailingMessages = true;

            _go = new GameObject("Hero");
            _renderer = _go.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", Color.white);
            _renderer.sharedMaterial = mat;

            _go.AddComponent<Health>();
            _flash = _go.AddComponent<HitFlash>();
            InvokeFlash("CacheRenderers");

            _applier = _go.AddComponent<HeroStageVariantApplier>();
            TestReflection.SetField(_applier, "_hitFlash", _flash);
            TestReflection.SetField(_applier, "_skeletonRenderers", new Renderer[] { _renderer });
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Object.DestroyImmediate(_go);
        }

        private void InvokeFlash(string method)
        {
            MethodInfo m = typeof(HitFlash).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(_flash, null);
        }

        private Color ReadFlashBaseline()
        {
            FieldInfo f = typeof(HitFlash).GetField("_originalColors", BindingFlags.NonPublic | BindingFlags.Instance);
            List<Color> colors = f.GetValue(_flash) as List<Color>;
            return colors[0];
        }

        private static void AssertVec3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f);
            Assert.AreEqual(expected.y, actual.y, 1e-4f);
            Assert.AreEqual(expected.z, actual.z, 1e-4f);
        }

        [Test]
        public void Apply를_2회_반복해도_스케일이_복리로_누적되지_않는다()
        {
            //# 복리면 2×2=4배. baseScale 1회 캐시로 항상 base×mul 이어야 한다.
            _applier.Apply(new HeroStageVariant { TintColor = Color.white, ScaleMultiplier = 2f });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale);

            _applier.Apply(new HeroStageVariant { TintColor = Color.white, ScaleMultiplier = 2f });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale); //# 4배 아님
        }

        [Test]
        public void 스테이지_전환시_이전_스케일이_리셋되어_base기준으로_재계산된다()
        {
            //# 5스테이지(1.4배) → 1스테이지(1.0배) 재사용. 이전 확대가 남으면 안 된다.
            _applier.Apply(new HeroStageVariant { TintColor = Color.red, ScaleMultiplier = 1.4f });
            AssertVec3(new Vector3(1.4f, 1.4f, 1.4f), _go.transform.localScale);

            _applier.Apply(new HeroStageVariant { TintColor = Color.white, ScaleMultiplier = 1.0f });
            AssertVec3(new Vector3(1f, 1f, 1f), _go.transform.localScale); //# base 로 복귀
        }

        [Test]
        public void Apply를_반복해도_HitFlash_baseline은_최신_틴트만_반영한다()
        {
            //# 스테이지 재적용마다 baseline 이 누적/오염 없이 마지막 틴트로 갱신되는지.
            _applier.Apply(new HeroStageVariant { TintColor = Color.red, ScaleMultiplier = 1f });
            Assert.AreEqual(Color.red, ReadFlashBaseline());

            _applier.Apply(new HeroStageVariant { TintColor = Color.green, ScaleMultiplier = 1f });
            Assert.AreEqual(Color.green, ReadFlashBaseline());
            Assert.AreEqual(Color.green, _renderer.material.GetColor("_BaseColor"));
        }

        [Test]
        public void 발광_스테이지는_EmissionColor에_색x강도가_들어간다()
        {
            _applier.Apply(new HeroStageVariant
            {
                TintColor = Color.white,
                UseEmission = true,
                EmissionColor = new Color(0f, 1f, 0f, 1f),
                EmissionIntensity = 2f,
                ScaleMultiplier = 1f,
            });
            Color e = _renderer.material.GetColor("_EmissionColor");
            //# (0,1,0,1) × 2 = (0,2,0,2). RGB 채널만 확인.
            Assert.AreEqual(0f, e.r, 1e-4f);
            Assert.AreEqual(2f, e.g, 1e-4f);
            Assert.AreEqual(0f, e.b, 1e-4f);
        }

        [Test]
        public void 비발광_스테이지는_잔존_발광을_검정으로_명시적으로_덮는다()
        {
            //# 기획서 §1.5 회귀 — 발광 스테이지 후 비발광 스테이지로 전환 시 발광이 새지 않아야 한다.
            _applier.Apply(new HeroStageVariant
            {
                TintColor = Color.white,
                UseEmission = true,
                EmissionColor = new Color(0f, 1f, 0f, 1f),
                EmissionIntensity = 2f,
                ScaleMultiplier = 1f,
            });
            _applier.Apply(new HeroStageVariant
            {
                TintColor = Color.white,
                UseEmission = false,
                ScaleMultiplier = 1f,
            });
            Color e = _renderer.material.GetColor("_EmissionColor");
            Assert.AreEqual(0f, e.r, 1e-4f);
            Assert.AreEqual(0f, e.g, 1e-4f); //# 이전 발광 잔존 없음
            Assert.AreEqual(0f, e.b, 1e-4f);
        }

        [Test]
        public void Apply는_variant가_null이면_상태를_바꾸지_않고_예외없다()
        {
            _applier.Apply(new HeroStageVariant { TintColor = Color.red, ScaleMultiplier = 2f });
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale);

            Assert.DoesNotThrow(() => _applier.Apply(null));
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale); //# 불변
            Assert.AreEqual(Color.red, ReadFlashBaseline());
        }

        [Test]
        public void Apply는_HitFlash_미할당이어도_스켈레톤_렌더러에_틴트를_직접_기록한다()
        {
            //# 이 테스트는 SetUp 의 _renderer 대신 본문에서 새 렌더러를 만들어 첫 .material 접근이 본문에서 발생한다.
            //# UTF 가 SetUp↔Test 경계에서 LogScope 를 리셋하므로 본문에서도 명시적으로 톨러런스(TearDown 이 원복).
            LogAssert.ignoreFailingMessages = true;

            //# WriteBaseColorFallback 분기 — _hitFlash 가 null 인 예외 구성. 독립 GameObject 로 격리.
            GameObject go = new GameObject("HeroNoFlash");
            Renderer rd = go.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", Color.white);
            rd.sharedMaterial = mat;

            HeroStageVariantApplier applier = go.AddComponent<HeroStageVariantApplier>();
            TestReflection.SetField(applier, "_hitFlash", null);
            TestReflection.SetField(applier, "_skeletonRenderers", new Renderer[] { rd });

            applier.Apply(new HeroStageVariant { TintColor = Color.yellow, ScaleMultiplier = 1f });
            Assert.AreEqual(Color.yellow, rd.material.GetColor("_BaseColor"));

            Object.DestroyImmediate(go);
        }
    }
}
