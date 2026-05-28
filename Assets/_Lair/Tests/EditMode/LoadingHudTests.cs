using System.Reflection;
using NUnit.Framework;
using Lair.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    public class LoadingHudTests
    {
        private GameObject _hudGo;
        private LoadingHud _hud;
        private Image _fill;

        [SetUp]
        public void SetUp()
        {
            _hudGo = new GameObject("TestHud");
            _hud = _hudGo.AddComponent<LoadingHud>();
            _fill = new GameObject("Fill").AddComponent<Image>();
            typeof(LoadingHud)
                .GetField("_progressFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_hud, _fill);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fill.gameObject);
            Object.DestroyImmediate(_hudGo);
        }

        [Test]
        public void SetProgress_ratio_0점5_fillAmount_0점5()
        {
            _hud.SetProgress(0.5f, "테스트");

            Assert.AreEqual(0.5f, _fill.fillAmount, 0.001f);
        }

        [Test]
        public void SetProgress_ratio_1_fillAmount_1()
        {
            _hud.SetProgress(1f, "완료");

            Assert.AreEqual(1f, _fill.fillAmount, 0.001f);
        }

        [Test]
        public void SetProgress_null필드_예외없이_처리()
        {
            //# SerializeField 미연결 상태 — 씬 배치 전 방어 검증
            LoadingHud emptyHud = new GameObject("Empty").AddComponent<LoadingHud>();

            Assert.DoesNotThrow(() => emptyHud.SetProgress(0.5f, "테스트"));

            Object.DestroyImmediate(emptyHud.gameObject);
        }
    }
}
