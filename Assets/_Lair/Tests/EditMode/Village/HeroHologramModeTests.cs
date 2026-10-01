using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Village
{
    //# 영웅 홀로그램 모드 — 기획서 scene-2d-conversion §5.5 · §7.11. 풀 재사용 시 기본값 복구(Rule 03 §4).
    public class HeroHologramModeTests
    {
        private GameObject _root;
        private SpriteRenderer _body;
        private GameObject _light;
        private HeroHologramMode _mode;
        private Material _original;
        private Material _holo;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("hero");
            _root.SetActive(false);
            GameObject bodyGo = new GameObject("body");
            bodyGo.transform.SetParent(_root.transform);
            _body = bodyGo.AddComponent<SpriteRenderer>();
            _original = new Material(Shader.Find("Sprites/Default"));
            _holo = new Material(Shader.Find("Sprites/Default"));
            _body.sharedMaterial = _original;
            _body.color = Color.white;
            _light = new GameObject("light");
            _light.transform.SetParent(_root.transform);
            _mode = _root.AddComponent<HeroHologramMode>();
            _mode.ConfigureForTest(new[] { _body }, _holo, new[] { _light });
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            Object.DestroyImmediate(_original);
            Object.DestroyImmediate(_holo);
        }

        [Test]
        public void 홀로그램_켜면_머티리얼이_바뀌고_반투명이며_조명이_꺼진다()
        {
            _mode.SetHologram(true);
            Assert.AreSame(_holo, _body.sharedMaterial);
            Assert.AreEqual(0.7f, _body.color.a, 1e-4f);
            Assert.IsFalse(_light.activeSelf);
        }

        [Test]
        public void 홀로그램_끄면_원래대로_복구된다()
        {
            _mode.SetHologram(true);
            _mode.SetHologram(false);
            Assert.AreSame(_original, _body.sharedMaterial);
            Assert.AreEqual(1f, _body.color.a, 1e-4f);
            Assert.IsTrue(_light.activeSelf);
        }

        [Test]
        public void 풀_재사용_OnEnable에서_기본값으로_복구된다()
        {
            _mode.SetHologram(true);
            //# EditMode 에선 OnEnable 이 자동 호출되지 않아 리플렉션으로 호출(풀 재사용 시 호출되는 경로)
            typeof(HeroHologramMode).GetMethod("OnEnable", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).Invoke(_mode, null);
            Assert.AreSame(_original, _body.sharedMaterial);
            Assert.IsTrue(_light.activeSelf);
        }
    }
}
