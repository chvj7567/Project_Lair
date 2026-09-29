using System.Reflection;
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

        //# ───────── 이하 test-engineer 보강분 — §9 "풀 재사용 초기화" (OnEnable 리셋 계약 실제 재현) ─────────

        //# EditMode [Test] 는 player loop 가 없어 GameObject.SetActive 토글로는 Awake/OnEnable/OnDisable 이
        //# 재호출되지 않는다(MonsterVisual2DFacingTests 의 InvokeVoid 관례와 동일 이유 — Awake 는 SetUp 의 AddComponent
        //# 시점에 이미 한 번 실행됐고, 그 뒤 SetActive 는 아무 콜백도 재호출하지 않는다). 풀 Pop 때 Unity 가 실제로
        //# 호출하는 OnEnable() 을 reflection 으로 직접 구동해 "풀 재사용 → 티어0 리셋" 계약(Rule 03 §4, MonsterTierOverlay
        //# 주석 "풀 재사용 — 티어 0(오버레이 off)으로 복귀")을 재현한다.
        private static void InvokeVoid(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 메서드 존재 확인 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        //# 풀 재사용 — 이전 스폰(스테이지5)에서 풀 Pop 재활성화된 뒤, BattleController 가 새 스폰의
        //# variant(스테이지2)로 재 Apply 하면 잔존 없이 새 값으로 전부 갱신된다.
        //# MonsterTierOverlay.OnEnable 이 SetTier(0) 으로 먼저 리셋하고(Rule 03 §4), 그 위에 Apply 가 최종값을 덮어쓴다.
        //# (발광 리셋은 HeroStageVariantApplierRegressionTests 가 URP 셰이더 재질로 이미 검증 — 본 테스트는 티어/스케일/배타성만.)
        [Test]
        public void 풀_재사용_OnEnable리셋_후_Apply하면_이전_스폰_값이_남지_않는다()
        {
            ExpectMaterialInstantiationWarnings();
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.4f, Tier = 4 });
            Assert.IsFalse(_body.enabled, "이전 스폰(스테이지5) — 몸 비활성");
            Assert.AreEqual(4, _tierOverlay.CurrentTierForTest);

            //# 풀 Pop — Unity 가 실제로 호출하는 OnEnable() 재현. Apply 호출 전 중간 상태로 티어0 리셋을 확인.
            InvokeVoid(_tierOverlay, "OnEnable");
            Assert.AreEqual(0, _tierOverlay.CurrentTierForTest, "OnEnable 직후(Apply 전) 오버레이는 티어0으로 리셋(§9 풀 재사용 초기화)");

            //# BattleController 가 새 스폰의 variant 로 재적용 — 스테이지2(Tier1), 스케일 1.25.
            _applier.Apply(new HeroStageVariant { ScaleMultiplier = 1.25f, Tier = 1 });

            Assert.IsFalse(_body.enabled, "새 스폰도 Tier>0 이므로 몸 비활성 유지");
            Assert.AreEqual(1, _tierOverlay.CurrentTierForTest, "오버레이 티어가 새 스폰 값(1)으로 갱신");
            AssertScale(new Vector3(1.25f, 1.25f, 1.25f), _go.transform.localScale, "스케일이 이전(1.4) 아닌 새 스폰 값(1.25)");
        }

        private static void AssertScale(Vector3 expected, Vector3 actual, string message)
        {
            Assert.AreEqual(expected.x, actual.x, 1e-4f, message);
            Assert.AreEqual(expected.y, actual.y, 1e-4f, message);
            Assert.AreEqual(expected.z, actual.z, 1e-4f, message);
        }
    }
}
