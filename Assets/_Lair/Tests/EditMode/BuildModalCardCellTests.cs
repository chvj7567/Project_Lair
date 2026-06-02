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
    }
}
