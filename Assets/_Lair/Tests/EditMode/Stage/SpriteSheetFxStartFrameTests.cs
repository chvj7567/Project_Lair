using System.Reflection;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode.Stage
{
    //# 시트 FX 시작 위상·null 프레임 — 기획서 scene-2d-conversion §7.5. 풀 FX 기본값 0 이라 동작 불변.
    public class SpriteSheetFxStartFrameTests
    {
        private GameObject _go;
        private SpriteRenderer _sr;
        private SpriteSheetFx _fx;
        private Sprite _a;
        private Sprite _b;
        private Texture2D _tex;

        [SetUp]
        public void SetUp()
        {
            _tex = new Texture2D(4, 4);
            _a = Sprite.Create(_tex, new Rect(0, 0, 2, 2), Vector2.zero);
            _b = Sprite.Create(_tex, new Rect(2, 2, 2, 2), Vector2.zero);
            _go = new GameObject("fx");
            _sr = _go.AddComponent<SpriteRenderer>();
            _fx = _go.AddComponent<SpriteSheetFx>();
            TestReflection.SetField(_fx, "_renderer", _sr);
            TestReflection.SetField(_fx, "_fps", 12f);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
            Object.DestroyImmediate(_a);
            Object.DestroyImmediate(_b);
            Object.DestroyImmediate(_tex);
        }

        private void Enable()
        {
            MethodInfo m = typeof(SpriteSheetFx).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(_fx, null);
        }

        [Test]
        public void null_프레임은_빈_스프라이트로_표시된다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[] { _a, null, _b });
            TestReflection.SetField(_fx, "_loop", true);
            TestReflection.SetField(_fx, "_startTimeOffset", 1f / 12f);
            Enable();
            Assert.AreEqual(1, _fx.CurrentFrame);
            Assert.IsNull(_sr.sprite);
        }

        [Test]
        public void 시작_오프셋_0이면_첫_프레임에서_시작한다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[] { _a, null, _b });
            TestReflection.SetField(_fx, "_loop", true);
            Enable();
            Assert.AreEqual(0, _fx.CurrentFrame);
            Assert.AreSame(_a, _sr.sprite);
        }

        [Test]
        public void 시작_오프셋이_프레임_수를_넘으면_루프에서_감긴다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[] { _a, null, _b });
            TestReflection.SetField(_fx, "_loop", true);
            TestReflection.SetField(_fx, "_startTimeOffset", 4f / 12f);
            Enable();
            Assert.AreEqual(1, _fx.CurrentFrame);
        }

        [Test]
        public void 한번_재생은_오프셋이_길이를_넘으면_마지막_프레임에서_멈춘다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[] { _a, null, _b });
            TestReflection.SetField(_fx, "_loop", false);
            TestReflection.SetField(_fx, "_startTimeOffset", 10f);
            Enable();
            Assert.AreEqual(2, _fx.CurrentFrame);
            Assert.AreSame(_b, _sr.sprite);
        }

        [Test]
        public void 다시_활성화하면_상태가_시작_오프셋으로_복구된다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[] { _a, null, _b });
            TestReflection.SetField(_fx, "_loop", true);
            TestReflection.SetField(_fx, "_startTimeOffset", 2f / 12f);
            Enable();
            Assert.AreEqual(2, _fx.CurrentFrame);
            Enable();
            Assert.AreEqual(2, _fx.CurrentFrame);
            Assert.IsFalse(_fx.IsFinished);
        }

        [Test]
        public void 프레임_배열이_비어도_예외가_없다()
        {
            TestReflection.SetField(_fx, "_frames", new Sprite[0]);
            Assert.DoesNotThrow(Enable);
            Assert.AreEqual(0, _fx.CurrentFrame);
        }

        [Test]
        public void StartFrame_음수_오프셋은_0이다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.StartFrame(-3f, 12f, 24, true));
            Assert.AreEqual(0, SpriteSheetFrames.StartFrame(-3f, 12f, 24, false));
        }

        [Test]
        public void StartFrame_프레임_수_0이면_0이다()
        {
            Assert.AreEqual(0, SpriteSheetFrames.StartFrame(5f, 12f, 0, true));
        }

        [Test]
        public void StartFrame_1회_재생은_마지막_프레임에서_고정된다()
        {
            Assert.AreEqual(23, SpriteSheetFrames.StartFrame(100f, 12f, 24, false));
        }
    }
}
