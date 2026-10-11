using System.Collections.Generic;
using System.Reflection;
using Lair.Tests.EditMode;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.UI
{
    //# 카드 선택 비주얼 폴리시(card-selection-visual-polish §4·§5) — 호버/눌림 연출·틴트·가드 최소 검증.
    public class CardSelectPolishTests
    {
        private readonly List<Object> _garbage = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _garbage.Count; ++i)
            {
                if (_garbage[i] != null)
                    Object.DestroyImmediate(_garbage[i]);
            }
            _garbage.Clear();
        }

        private GameObject NewGo(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        private static void Invoke(object target, string method)
        {
            target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(target, null);
        }

        private class Parts
        {
            public CardView View;
            public CardPointerRelay Relay;
            public Image Frame;
            public Image Glow;
            public Image Dim;
            public GameObject Guard;
            public Image[] KindTint;
        }

        private Parts Make(bool wired)
        {
            GameObject root = new GameObject("Card", typeof(RectTransform));
            _garbage.Add(root);
            Parts p = new Parts { View = root.AddComponent<CardView>() };
            if (wired == false)
                return p;

            p.Frame = NewGo("Frame", root.transform).AddComponent<Image>();
            p.Frame.color = new Color32(0x8A, 0x6A, 0x2C, 0xFF);
            p.Glow = NewGo("Glow", root.transform).AddComponent<Image>();
            p.Glow.color = new Color(0.97f, 0.78f, 0.29f, 0f);
            p.Dim = NewGo("Dim", root.transform).AddComponent<Image>();
            p.Dim.color = new Color(0.02f, 0.03f, 0.05f, 0f);
            p.Guard = NewGo("Guard", root.transform);
            p.Guard.SetActive(false);
            p.Relay = NewGo("Pick", root.transform).AddComponent<CardPointerRelay>();
            p.KindTint = new Image[] { NewGo("T0", root.transform).AddComponent<Image>() };
            p.KindTint[0].color = new Color(1f, 1f, 1f, 0.7f);

            TestReflection.SetField(p.View, "_frameGold", p.Frame);
            TestReflection.SetField(p.View, "_hoverGlow", p.Glow);
            TestReflection.SetField(p.View, "_dimOverlay", p.Dim);
            TestReflection.SetField(p.View, "_hoverGuard", p.Guard);
            TestReflection.SetField(p.View, "_pointerRelay", p.Relay);
            TestReflection.SetField(p.View, "_kindTintImages", p.KindTint);
            TestReflection.SetField(p.View, "_tweenSeconds", 0f);
            Invoke(p.View, "CaptureRest");
            Invoke(p.View, "OnEnable");
            return p;
        }

        [Test]
        public void 호버_눌림_해제_표시값이_휴지로_복귀한다()
        {
            Parts p = Make(true);
            float baseY = ((RectTransform)p.View.transform).anchoredPosition.y;

            p.Relay.OnPointerEnter(null);
            Assert.AreEqual(0.8f, p.Glow.color.a, 0.001f);
            Assert.AreEqual(baseY + 6f, ((RectTransform)p.View.transform).anchoredPosition.y, 0.001f);
            Assert.IsTrue(p.Guard.activeSelf);

            p.Relay.OnPointerDown(null);
            Assert.IsTrue(p.View.IsPressed);
            Assert.AreEqual(1.03f, p.View.transform.localScale.x, 0.001f);

            p.Relay.OnPointerExit(null);
            Assert.IsFalse(p.View.IsPressed);
            Assert.AreEqual(0f, p.Glow.color.a, 0.001f);
            Assert.AreEqual(baseY, ((RectTransform)p.View.transform).anchoredPosition.y, 0.001f);
            Assert.IsFalse(p.Guard.activeSelf);
        }

        [Test]
        public void 눌림_카드는_어두움을_무시하고_종류_틴트는_알파를_보존한다()
        {
            Parts p = Make(true);
            p.View.SetDimmed(true);
            Assert.AreEqual(0.5f, p.Dim.color.a, 0.001f);

            p.Relay.OnPointerDown(null);
            Assert.AreEqual(0f, p.Dim.color.a, 0.001f);

            p.View.SetKind(true);
            Assert.AreEqual(0.7f, p.KindTint[0].color.a, 0.001f);
            p.View.SetKind(false);
            Assert.AreEqual(0.7f, p.KindTint[0].color.a, 0.001f);
        }

        [Test]
        public void 신규_필드가_모두_비어도_예외없이_동작한다()
        {
            Parts p = Make(false);
            Assert.DoesNotThrow(() =>
            {
                p.View.SetKind(true);
                p.View.SetAxisIcon(null, null);
                p.View.SetAxisTier(2);
                p.View.SetDimmed(true);
                Invoke(p.View, "OnEnable");
                Invoke(p.View, "OnDisable");
            });
        }
    }
}
