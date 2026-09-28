using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# 강화 오버레이 프레임 동기(monster-2d-conversion.md §6.3.6) — 몸 스프라이트 이름 접미 인덱스로
    //# 활성 티어 오버레이를 매칭한다. 시드: gameplay-programmer.
    //# 본 스위트는 "정상 매칭 + 엣지 1개(매칭 프레임 없음)" 만 — 나머지 티어 전환/플립 동기는 test-engineer.
    public class MonsterTierOverlaySyncTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private static Sprite MakeSprite(string name)
        {
            Texture2D tex = new Texture2D(1, 1);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), Vector2.one * 0.5f);
            sprite.name = name;
            return sprite;
        }

        private MonsterTierOverlay NewOverlay(out SpriteRenderer overlayRenderer)
        {
            _go = new GameObject("EnhanceUT");
            overlayRenderer = _go.AddComponent<SpriteRenderer>();
            MonsterTierOverlay overlay = _go.AddComponent<MonsterTierOverlay>();
            overlay.SetOverlayRendererForTest(overlayRenderer);
            return overlay;
        }

        [Test]
        public void Tier1_몸프레임과_같은_인덱스의_오버레이가_표시된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            Sprite tier1Frame7 = MakeSprite("Wisp_Sheet_T1_7");
            overlay.SetFramesForTest(new[] { tier1Frame7 }, null, null);
            overlay.SetTier(1);

            Sprite bodyFrame7 = MakeSprite("Wisp_Sheet_7");
            overlay.Tick(bodyFrame7, false);

            Assert.IsTrue(rd.enabled, "매칭 프레임이 있으면 오버레이 활성");
            Assert.AreEqual(tier1Frame7, rd.sprite);
        }

        //# 엣지 — 활성 티어에 해당 인덱스 프레임이 없으면(파츠 없는 칸) 오버레이를 비활성화한다.
        [Test]
        public void 매칭프레임이_없으면_오버레이가_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            Sprite tier1Frame7 = MakeSprite("Wisp_Sheet_T1_7");
            overlay.SetFramesForTest(new[] { tier1Frame7 }, null, null);
            overlay.SetTier(1);

            Sprite bodyFrame3 = MakeSprite("Wisp_Sheet_3");   //# 인덱스 3 은 티어1 시트에 없음
            overlay.Tick(bodyFrame3, false);

            Assert.IsFalse(rd.enabled, "매칭 프레임이 없으면 오버레이 비활성");
        }
    }
}
