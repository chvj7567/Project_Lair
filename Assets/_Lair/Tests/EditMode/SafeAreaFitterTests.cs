using NUnit.Framework;
using Lair.UI;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class SafeAreaFitterTests
    {
        [Test]
        public void ComputeAnchors_교집합을_pixel_기준으로_정규화한다()
        {
            //# pixel 100~1100 x 0~500, safe 200~1000 x 0~400 → x 0.1~0.9, y 0~0.8.
            SafeAreaFitter.ComputeAnchors(new Rect(200f, 0f, 800f, 400f), new Rect(100f, 0f, 1000f, 500f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(0.1f, min.x, 1e-4f);
            Assert.AreEqual(0f, min.y, 1e-4f);
            Assert.AreEqual(0.9f, max.x, 1e-4f);
            Assert.AreEqual(0.8f, max.y, 1e-4f);
        }

        [Test]
        public void ComputeAnchors_safe가_pixel을_포함하면_전체()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 0f, 2000f, 1000f), new Rect(100f, 100f, 800f, 400f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void ComputeAnchors_빈_교집합이면_전체_폴백()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 0f, 50f, 50f), new Rect(100f, 100f, 800f, 400f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }

        [Test]
        public void ComputeAnchors_pixelRect_0이면_전체_폴백()
        {
            SafeAreaFitter.ComputeAnchors(new Rect(0f, 0f, 100f, 100f), new Rect(0f, 0f, 0f, 0f), out Vector2 min, out Vector2 max);
            Assert.AreEqual(Vector2.zero, min);
            Assert.AreEqual(Vector2.one, max);
        }
    }
}
