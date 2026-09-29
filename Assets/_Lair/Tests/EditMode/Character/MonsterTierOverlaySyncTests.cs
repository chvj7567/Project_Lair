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

        //# 신규(hero-2d-conversion §6.4) — 영웅 스테이지5(Tier4) 오버레이. 몬스터 tier0~3 로직 무변경(additive) 확인.
        [Test]
        public void Tier4_몸프레임과_같은_인덱스의_오버레이가_표시된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            Sprite tier4Frame9 = MakeSprite("Knight_Sheet_S5_109");
            overlay.SetTier4FramesForTest(new[] { tier4Frame9 });
            overlay.SetTier(4);

            Sprite bodyFrame9 = MakeSprite("Knight_Sheet_109");
            overlay.Tick(bodyFrame9, false);

            Assert.IsTrue(rd.enabled, "Tier4 매칭 프레임이 있으면 오버레이 활성");
            Assert.AreEqual(tier4Frame9, rd.sprite);
        }

        //# ───────── 이하 test-engineer 보강분 — ③ 매칭 규칙 망라 + flipX 동기 + 티어 전환 + 안전 가드 ─────────

        [Test]
        public void 오버레이_flipX가_몸의_flipX와_동기화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_4") }, null, null);
            overlay.SetTier(1);

            overlay.Tick(MakeSprite("Wisp_Sheet_4"), true);

            Assert.IsTrue(rd.flipX, "몸이 flipX=true 이면 오버레이도 flipX=true 로 동기");
        }

        [Test]
        public void 티어전환시_같은_인덱스라도_새_티어_오버레이로_즉시_전환된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            Sprite t1Frame = MakeSprite("Wisp_Sheet_T1_2");
            Sprite t2Frame = MakeSprite("Wisp_Sheet_T2_2");
            overlay.SetFramesForTest(new[] { t1Frame }, new[] { t2Frame }, null);
            Sprite body = MakeSprite("Wisp_Sheet_2");

            overlay.SetTier(1);
            overlay.Tick(body, false);
            Assert.AreEqual(t1Frame, rd.sprite, "티어1일 때 T1 프레임");

            overlay.SetTier(2);
            overlay.Tick(body, false);
            Assert.AreEqual(t2Frame, rd.sprite, "같은 몸 인덱스라도 티어가 바뀌면 T2 프레임으로 전환");
        }

        //# 엣지 — 몸 스프라이트 자체가 null(Animator 미배선 등)이면 비활성화.
        [Test]
        public void 몸스프라이트가_null이면_오버레이가_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_1") }, null, null);
            overlay.SetTier(1);

            overlay.Tick(null, false);

            Assert.IsFalse(rd.enabled);
        }

        //# 엣지 — 접미사가 정수로 파싱되지 않으면(오탈자·비규격 명명) 매칭 실패로 안전하게 비활성화.
        [Test]
        public void 접미사가_숫자가_아니면_안전하게_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_attack") }, null, null);
            overlay.SetTier(1);

            overlay.Tick(MakeSprite("Wisp_Sheet_attack"), false);

            Assert.IsFalse(rd.enabled, "숫자가 아닌 접미사는 인덱스 파싱 실패 → 비활성화");
        }

        //# 엣지 — 언더스코어 자체가 없는 이름(시트 명명 규칙 위반)도 안전하게 비활성화.
        [Test]
        public void 언더스코어가_없는_이름이면_안전하게_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_0") }, null, null);
            overlay.SetTier(1);

            overlay.Tick(MakeSprite("WispSheet"), false);

            Assert.IsFalse(rd.enabled);
        }

        //# 엣지 — 이름이 언더스코어로 끝나(접미사 빈 문자열) 파싱 대상이 없는 경우도 안전 처리.
        [Test]
        public void 언더스코어로_끝나는_이름이면_안전하게_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_0") }, null, null);
            overlay.SetTier(1);

            overlay.Tick(MakeSprite("Wisp_Sheet_"), false);

            Assert.IsFalse(rd.enabled);
        }

        //# 엣지 — SetTier 에 정의 범위(0~4, hero-2d-conversion §6.4 로 4 까지 확장) 밖 값이 들어와도 활성 맵 없음으로 안전 처리.
        [Test]
        public void SetTier가_정의범위_밖_값이면_오버레이가_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(null, null, new[] { MakeSprite("Wisp_Sheet_T3_0") });

            overlay.SetTier(5);   //# 정의된 값은 0~4 뿐
            overlay.Tick(MakeSprite("Wisp_Sheet_0"), false);

            Assert.IsFalse(rd.enabled, "정의되지 않은 티어 값은 오버레이 off 로 안전 처리");
        }

        [Test]
        public void SetTier가_음수이면_오버레이가_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new[] { MakeSprite("Wisp_Sheet_T1_0") }, null, null);

            overlay.SetTier(-1);
            overlay.Tick(MakeSprite("Wisp_Sheet_0"), false);

            Assert.IsFalse(rd.enabled);
        }

        //# 엣지 — 해당 티어에 프레임이 하나도 없는(빈 배열) 경우도 전부 비활성화.
        [Test]
        public void 빈_프레임배열이면_해당_티어가_전부_비활성화된다()
        {
            MonsterTierOverlay overlay = NewOverlay(out SpriteRenderer rd);
            overlay.SetFramesForTest(new Sprite[0], null, null);
            overlay.SetTier(1);

            overlay.Tick(MakeSprite("Wisp_Sheet_0"), false);

            Assert.IsFalse(rd.enabled);
        }

        //# 엣지 — _overlayRenderer 가 배선되지 않은 상태(구형 프리팹 등)에서도 Tick 호출이 예외를 던지지 않는다.
        [Test]
        public void 오버레이렌더러가_없으면_Tick_호출해도_예외없다()
        {
            _go = new GameObject("NoRenderer");
            MonsterTierOverlay overlay = _go.AddComponent<MonsterTierOverlay>();

            Assert.DoesNotThrow(() => overlay.Tick(MakeSprite("Wisp_Sheet_0"), false));
        }
    }
}
