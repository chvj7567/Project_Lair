using System.Reflection;
using NUnit.Framework;
using Lair.Card;
using Lair.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    public class BuildModalCardCellTests
    {
        private GameObject _cellGo;
        private BuildModalCardCell _cell;
        private Image _icon;

        [SetUp]
        public void SetUp()
        {
            _cellGo = new GameObject("TestCell");
            _cell = _cellGo.AddComponent<BuildModalCardCell>();
            _icon = new GameObject("Icon").AddComponent<Image>();
            _icon.transform.SetParent(_cellGo.transform, false);
            typeof(BuildModalCardCell)
                .GetField("_icon", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_cell, _icon);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cellGo);
        }

        private static CardData MakeCard(Sprite icon)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            typeof(CardData)
                .GetField("_icon", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(card, icon);
            return card;
        }

        [Test]
        public void Bind_아이콘있으면_active_및_sprite_할당()
        {
            Sprite sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), Vector2.zero);
            CardData card = MakeCard(sprite);
            //# Bind 가 켜는지 검증하려면 꺼진 상태에서 시작.
            _icon.gameObject.SetActive(false);

            _cell.Bind(card, 1);

            Assert.IsTrue(_icon.gameObject.activeSelf);
            Assert.AreEqual(sprite, _icon.sprite);

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(card);
        }

        [Test]
        public void Bind_아이콘null이면_비활성()
        {
            CardData card = MakeCard(null);

            _cell.Bind(card, 1);

            Assert.IsFalse(_icon.gameObject.activeSelf);

            Object.DestroyImmediate(card);
        }

        [Test]
        public void Bind_종류색_장식에_rgb만_칠하고_알파는_보존()
        {
            Image deco = new GameObject("Deco").AddComponent<Image>();
            deco.transform.SetParent(_cellGo.transform, false);
            deco.color = new Color(1f, 1f, 1f, 0.65f);
            typeof(BuildModalCardCell)
                .GetField("_kindTinted", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_cell, new Image[] { deco, null });
            CardData card = MakeCard(null);

            _cell.Bind(card, 1, true);
            Assert.AreEqual(UiDotPalette.CardKind(true).g, deco.color.g, 0.005f);
            Assert.AreEqual(0.65f, deco.color.a, 0.005f);

            _cell.Bind(card, 1, false);
            Assert.AreEqual(UiDotPalette.CardKind(false).b, deco.color.b, 0.005f);

            Object.DestroyImmediate(card);
        }

        [Test]
        public void Bind_배지장식은_2장이상일때만_켜지고_풀재사용시_꺼진다()
        {
            GameObject badge = new GameObject("Badge");
            badge.transform.SetParent(_cellGo.transform, false);
            typeof(BuildModalCardCell)
                .GetField("_countBadgeDecor", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_cell, badge);
            CardData card = MakeCard(null);

            _cell.Bind(card, 2, true);
            Assert.IsTrue(badge.activeSelf);
            _cell.Bind(card, 1, true);
            Assert.IsFalse(badge.activeSelf);

            Object.DestroyImmediate(card);
        }
    }
}
