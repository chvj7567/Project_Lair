using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using Lair.Data;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# Apply 가 틴트를 HitFlash 원본으로 위임하고 스케일 배수를 적용하는가 (plan Task 5).
    public class HeroStageVariantApplierTests
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

        [Test]
        public void Apply_는_틴트를_HitFlash_원본과_머터리얼에_반영한다()
        {
            _applier.Apply(new HeroStageVariant { TintColor = Color.green, ScaleMultiplier = 1f });

            Assert.AreEqual(Color.green, ReadFlashBaseline());
            Assert.AreEqual(Color.green, _renderer.material.GetColor("_BaseColor"));
        }

        [Test]
        public void Apply_는_ScaleMultiplier로_root_스케일을_배수한다()
        {
            _applier.Apply(new HeroStageVariant { TintColor = Color.white, ScaleMultiplier = 2f });

            Assert.AreEqual(new Vector3(2f, 2f, 2f), _go.transform.localScale);
        }
    }
}
