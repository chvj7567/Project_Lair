using System.Reflection;
using ChvjUnityInfra;
using NUnit.Framework;
using Lair.Character;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    public class HpBarViewTests
    {
        private GameObject _barGo;
        private HpBarView _view;
        private Image _fill;
        private CHText _txtHp;
        private TextMeshProUGUI _tmp;
        private GameObject _iconRow;
        private Image[] _iconSlots;

        [SetUp]
        public void SetUp()
        {
            _barGo = new GameObject("TestHpBar");
            _view = _barGo.AddComponent<HpBarView>();

            _fill = new GameObject("Fill").AddComponent<Image>();
            _fill.transform.SetParent(_barGo.transform, false);
            _fill.type = Image.Type.Filled;

            //# CHText 는 RequireComponent(TMP_Text) — TMP 먼저 부착.
            GameObject txtGo = new GameObject("txtHp");
            txtGo.transform.SetParent(_barGo.transform, false);
            _tmp = txtGo.AddComponent<TextMeshProUGUI>();
            _txtHp = txtGo.AddComponent<CHText>();

            typeof(HpBarView)
                .GetField("_fill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_view, _fill);
            typeof(HpBarView)
                .GetField("_txtHp", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_view, _txtHp);

            //# 상태 아이콘 행 위젯 — 8 슬롯 Image + 컨테이너(기본 비활성).
            _iconRow = new GameObject("StatusIconRow");
            _iconRow.transform.SetParent(_barGo.transform, false);
            _iconSlots = new Image[8];
            for (int i = 0; i < 8; i++)
            {
                _iconSlots[i] = new GameObject($"Slot{i}").AddComponent<Image>();
                _iconSlots[i].transform.SetParent(_iconRow.transform, false);
                _iconSlots[i].gameObject.SetActive(false);
            }
            _iconRow.SetActive(false);
            typeof(HpBarView)
                .GetField("_statusIconRow", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_view, _iconRow);
            typeof(HpBarView)
                .GetField("_iconSlots", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_view, _iconSlots);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_barGo);
        }

        [Test]
        public void SetHp_정상_fill비율과_텍스트_갱신()
        {
            _view.SetHp(250, 1000);

            Assert.AreEqual(0.25f, _fill.fillAmount, 0.0001f);
            Assert.AreEqual("250/1000", _tmp.text);
        }

        [Test]
        public void SetHp_max0_엣지_fill0()
        {
            _view.SetHp(5, 0);

            Assert.AreEqual(0f, _fill.fillAmount, 0.0001f);
        }

        [Test]
        public void SetTextVisible_false_fill만갱신_텍스트미갱신_그리고비활성()
        {
            _view.SetTextVisible(false);
            _view.SetHp(250, 1000);

            Assert.AreEqual(0.25f, _fill.fillAmount, 0.0001f);
            //# 갓 AddComponent 한 TMP.text 는 null 일 수 있어 string.Empty 단언 불가.
            //# 핵심은 텍스트가 "250/1000" 으로 갱신되지 않았는지.
            Assert.AreNotEqual("250/1000", _tmp.text);
            Assert.IsFalse(_txtHp.gameObject.activeSelf);
        }

        [Test]
        public void SetTextVisible_true복귀_텍스트_다시갱신()
        {
            _view.SetTextVisible(false);
            _view.SetHp(250, 1000);

            _view.SetTextVisible(true);
            _view.SetHp(300, 1000);

            Assert.AreEqual("300/1000", _tmp.text);
            Assert.IsTrue(_txtHp.gameObject.activeSelf);
        }

        [Test]
        public void AddStatusIcon_슬롯활성화_스프라이트세팅_컨테이너활성()
        {
            Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            _view.AddStatusIcon("slow", sp);

            Assert.IsTrue(_iconRow.activeSelf);
            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.AreEqual(sp, _iconSlots[0].sprite);
        }

        [Test]
        public void RemoveStatusIcon_마지막제거시_컨테이너비활성()
        {
            Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            _view.AddStatusIcon("slow", sp);
            _view.RemoveStatusIcon("slow");

            Assert.IsFalse(_iconSlots[0].gameObject.activeSelf);
            Assert.IsFalse(_iconRow.activeSelf);
        }

        [Test]
        public void AddStatusIcon_같은key중복_슬롯1개만사용()
        {
            Sprite sp = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            _view.AddStatusIcon("slow", sp);
            _view.AddStatusIcon("slow", sp);

            Assert.IsTrue(_iconSlots[0].gameObject.activeSelf);
            Assert.IsFalse(_iconSlots[1].gameObject.activeSelf);
        }
    }
}
