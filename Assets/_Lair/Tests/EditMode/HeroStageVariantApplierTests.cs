using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# Apply 가 스케일 배수 + 몸/오버레이 배타성(§6.4.1)을 적용하는가 (hero-2d-conversion §11 — TintColor/HitFlash 위임 제거 후 갱신).
    public class HeroStageVariantApplierTests
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
            TestReflection.SetField(_applier, "_emissionTargets", new[] { _body, overlayRenderer });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_go);
        }

        //# Apply() 가 내부적으로 renderer.material(non-shared getter) 을 호출해 EditMode 전용 벤더 경고를 낸다
        //# (몸·오버레이 렌더러 각 1회, 최초 접근 시에만 — production 코드 관점에선 올바른 호출: Mat_Monster2D 는
        //# 영웅·몬스터 6종이 공유하므로 인스턴스화 없이 sharedMaterial 을 직접 건드리면 다른 캐릭터까지 오염된다).
        private static void ExpectMaterialInstantiationWarnings()
        {
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Instantiating material"));
            LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("Instantiating material"));
        }

        [Test]
        public void Apply는_ScaleMultiplier로_root_스케일을_배수한다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 2f, Tier = 0 });

            Assert.AreEqual(new Vector3(2f, 2f, 2f), _go.transform.localScale);
        }

        //# §6.4.1 — 스테이지1(Tier0)은 몸만 보이고 오버레이는 꺼진다.
        [Test]
        public void Apply는_Tier0에서_몸을_켜고_오버레이_티어는_0이다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1f, Tier = 0 });

            Assert.IsTrue(_body.enabled, "Tier 0 은 몸 렌더러 활성");
            Assert.AreEqual(0, _tierOverlay.CurrentTierForTest);
        }

        //# 엣지 — Tier>0 이면 몸을 끄고 오버레이 티어를 그대로 전달(부분 커버리지 위험 회피).
        [Test]
        public void Apply는_Tier가_0보다_크면_몸을_끄고_오버레이_티어를_전달한다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.4f, Tier = 4 });

            Assert.IsFalse(_body.enabled, "Tier>0 은 오버레이만 보이도록 몸 비활성(§6.4.1)");
            Assert.AreEqual(4, _tierOverlay.CurrentTierForTest);
        }
    }
}
