using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    //# Apply 복리 방지 + 몸/오버레이 배타성 연속 전환 회귀 (hero-2d-conversion §11).
    //# 단발 스케일/배타성은 HeroStageVariantApplierTests 가 커버 — 여기선 반복 Apply.
    public class HeroStageVariantApplierRegressionTests
    {
        private GameObject _go;
        private HeroStageVariantApplier _applier;
        private SpriteRenderer _body;
        private MonsterTierOverlay _tierOverlay;

        [SetUp]
        public void SetUp()
        {
            _go = new GameObject("Hero");
            GameObject visual2D = new GameObject("Visual2D");
            visual2D.transform.SetParent(_go.transform, false);
            _body = visual2D.AddComponent<SpriteRenderer>();

            GameObject enhance = new GameObject("Enhance");
            enhance.transform.SetParent(visual2D.transform, false);
            SpriteRenderer overlayRenderer = enhance.AddComponent<SpriteRenderer>();
            _tierOverlay = enhance.AddComponent<MonsterTierOverlay>();
            _tierOverlay.SetOverlayRendererForTest(overlayRenderer);

            _applier = _go.AddComponent<HeroStageVariantApplier>();
            TestReflection.SetField(_applier, "_body", _body);
            TestReflection.SetField(_applier, "_tierOverlay", _tierOverlay);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        private static void AssertVec3(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f);
            Assert.AreEqual(expected.y, actual.y, 1e-4f);
            Assert.AreEqual(expected.z, actual.z, 1e-4f);
        }

        [Test]
        public void Apply를_2회_반복해도_스케일이_복리로_누적되지_않는다()
        {
            //# 복리면 2×2=4배. baseScale 1회 캐시로 항상 base×mul 이어야 한다.
            _applier.Apply(2f, 0);
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale);

            _applier.Apply(2f, 0);
            AssertVec3(new Vector3(2f, 2f, 2f), _go.transform.localScale); //# 4배 아님
        }

        [Test]
        public void 스테이지_전환시_이전_스케일이_리셋되어_base기준으로_재계산된다()
        {
            //# 5스테이지(1.4배) → 1스테이지(1.0배) 재사용. 이전 확대가 남으면 안 된다.
            _applier.Apply(1.4f, 4);
            AssertVec3(new Vector3(1.4f, 1.4f, 1.4f), _go.transform.localScale);

            _applier.Apply(1.0f, 0);
            AssertVec3(new Vector3(1f, 1f, 1f), _go.transform.localScale); //# base 로 복귀
        }

        //# 스테이지1↔스테이지5 왕복 전환에서도 몸/오버레이 배타성(§6.4.1)이 매번 정확히 갱신된다.
        [Test]
        public void 스테이지1에서_스테이지5로_전환시_몸이_꺼지고_오버레이_티어가_바뀐다()
        {
            _applier.Apply(1f, 0);
            Assert.IsTrue(_body.enabled);

            _applier.Apply(1.4f, 4);
            Assert.IsFalse(_body.enabled, "§6.4.1 — Tier>0 은 오버레이만");
            Assert.AreEqual(4, _tierOverlay.CurrentTierForTest);
        }

        //# ───────── 이하 test-engineer 보강분 — §9 "몸/오버레이 렌더러 배타성" 5스테이지 전부 + 전환 순서 엣지 ─────────

        //# 5스테이지(Tier 0~4) 전부에서 몸/오버레이가 정확히 하나만 켜진다(§6.4.1). 스테이지1(Tier0)만 몸, 나머지는 오버레이.
        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(4)]
        public void Apply는_5스테이지_전부에서_몸_오버레이가_상호배타적이다(int tier)
        {
            _applier.Apply(1f, tier);

            bool expectBody = tier == 0;
            Assert.AreEqual(expectBody, _body.enabled, $"Tier {tier} — 몸 렌더러 활성 여부(§6.4.1)");
            Assert.AreEqual(tier, _tierOverlay.CurrentTierForTest, $"Tier {tier} — 오버레이 티어 전달");
        }

        //# 여러 스테이지를 연속 전환(1→3→5→2→4→1, Tier 0→2→4→1→3→0)해도 매 전환마다 배타성이 정확히 갱신된다.
        //# 단발/왕복 전환(위 테스트들)과 달리 3회 이상 연속 전환에서 잔존 상태가 없는지 확인.
        [Test]
        public void 여러_스테이지를_연속_전환해도_매번_배타성이_정확히_갱신된다()
        {
            int[] tierSequence = { 0, 2, 4, 1, 3, 0 };
            foreach (int tier in tierSequence)
            {
                _applier.Apply(1f, tier);
                bool expectBody = tier == 0;
                Assert.AreEqual(expectBody, _body.enabled, $"연속 전환 중 Tier {tier} — 몸 활성 여부");
                Assert.AreEqual(tier, _tierOverlay.CurrentTierForTest, $"연속 전환 중 Tier {tier} — 오버레이 티어");
            }
        }
    }
}
