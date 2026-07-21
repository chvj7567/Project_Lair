using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# spec §5.1 회귀 — variant 틴트를 HitFlash 원본으로 (재)설정하면 피격 원복·풀 재사용 후에도 유지되는가.
    public class HitFlashVariantTests
    {
        private GameObject _go;
        private HitFlash _flash;
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

            //# EditMode 에선 Awake 미실행 → CacheRenderers 를 직접 호출해 원본 색 캐시(.material 인스턴스화).
            Invoke("CacheRenderers");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Object.DestroyImmediate(_go);
        }

        private void Invoke(string method)
        {
            MethodInfo m = typeof(HitFlash).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"메서드 미발견: {method}");
            m.Invoke(_flash, null);
        }

        private Color ReadBaseColor()
        {
            Material mat = _renderer.material;
            return mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
        }

        [Test]
        public void SetBaselineColor_후_피격_원복하면_틴트가_유지된다()
        {
            _flash.SetBaselineColor(Color.red);
            Assert.AreEqual(Color.red, ReadBaseColor());

            //# 피격 flash 흐름 재현 — 반전 적용 후 원복.
            Invoke("ApplyInvertedColors");
            Invoke("RestoreOriginalColors");

            Assert.AreEqual(Color.red, ReadBaseColor());
        }

        [Test]
        public void SetBaselineColor_후_풀재사용_원복해도_틴트가_유지된다()
        {
            _flash.SetBaselineColor(Color.green);
            //# OnEnable(풀 재사용)이 부르는 원복 경로 재현.
            Invoke("RestoreOriginalColors");
            Assert.AreEqual(Color.green, ReadBaseColor());
        }
    }
}
