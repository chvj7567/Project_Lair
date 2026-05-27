using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# 스포너 상태 UI — 영역 H (인터페이스 시그니처 호환성 회귀, 기획서 §4.2 BLOCKER 4).
    //#
    //# BLOCKER 4 결정: ICardEffect.Apply / IBattleContext.RegisterMonsterTypeBuff 시그니처를 변경하지 않고
    //# BattleController 내부 _currentCardScope 로 source 추적. 25개 효과 클래스 / BattleContext / 6개 강화 효과
    //# 모두 시그니처 불변. 본 테스트는 그 결정의 회귀 보호.
    //#
    //# 검증:
    //#  - ICardEffect.Apply(IBattleContext) 1 인자 시그니처 유지.
    //#  - IBattleContext.RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float) 3 인자 시그니처 유지.
    //#  - IBattleContext 의 다른 메서드(IncrementSpawnerOutput / ReplaceSpawnerOutput) 도 시그니처 유지.
    //#  - BattleContext 가 RegisterMonsterTypeBuff 를 그대로 위임 (시그니처 불변).
    public class CardEffectSignatureRegressionTests
    {
        //# ===== ICardEffect.Apply 시그니처 불변 =====

        [Test]
        public void ICardEffect_Apply_시그니처_IBattleContext_1인자_유지()
        {
            var method = typeof(ICardEffect).GetMethod("Apply");
            Assert.IsNotNull(method, "ICardEffect.Apply 메서드 존재");

            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length,
                "Apply 의 인자 수 = 1 (기획서 §4.2 BLOCKER 4 — 시그니처 변경 X)");
            Assert.AreEqual(typeof(IBattleContext), parameters[0].ParameterType,
                "Apply 의 단일 인자는 IBattleContext");
            Assert.AreEqual(typeof(void), method.ReturnType, "Apply 반환 타입 = void");
        }

        //# ===== IBattleContext.RegisterMonsterTypeBuff 시그니처 불변 =====

        [Test]
        public void IBattleContext_RegisterMonsterTypeBuff_시그니처_3인자_유지()
        {
            var method = typeof(IBattleContext).GetMethod("RegisterMonsterTypeBuff");
            Assert.IsNotNull(method, "IBattleContext.RegisterMonsterTypeBuff 메서드 존재");

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length,
                "RegisterMonsterTypeBuff 인자 수 = 3 (CardData source 인자 추가 안 함 — BLOCKER 4 결정)");
            Assert.AreEqual(typeof(EMonster),         parameters[0].ParameterType, "1번째: EMonster");
            Assert.AreEqual(typeof(EMonsterStatKind), parameters[1].ParameterType, "2번째: EMonsterStatKind");
            Assert.AreEqual(typeof(float),            parameters[2].ParameterType, "3번째: float (배율)");
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        //# ===== BattleContext 위임 시그니처 일치 =====

        [Test]
        public void BattleContext_RegisterMonsterTypeBuff_위임_시그니처_일치()
        {
            //# BattleContext (구체) 도 IBattleContext.RegisterMonsterTypeBuff 와 동일 시그니처.
            var method = typeof(BattleContext).GetMethod("RegisterMonsterTypeBuff");
            Assert.IsNotNull(method, "BattleContext.RegisterMonsterTypeBuff 메서드 존재");

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length);
            Assert.AreEqual(typeof(EMonster), parameters[0].ParameterType);
            Assert.AreEqual(typeof(EMonsterStatKind), parameters[1].ParameterType);
            Assert.AreEqual(typeof(float), parameters[2].ParameterType);
        }

        //# ===== BattleController 의 핵심 메서드 시그니처 =====

        //# 회귀 — RegisterMonsterTypeBuff (BattleController 구체) 도 시그니처 불변.
        [Test]
        public void BattleController_RegisterMonsterTypeBuff_시그니처_3인자()
        {
            var method = typeof(BattleController).GetMethod("RegisterMonsterTypeBuff",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method, "BattleController.RegisterMonsterTypeBuff 메서드 존재");

            var parameters = method.GetParameters();
            Assert.AreEqual(3, parameters.Length, "공개 시그니처 3인자 유지");
        }

        //# 회귀 — ApplyCardEffect(CardData) 신규 시그니처가 그대로 노출 (1 인자).
        [Test]
        public void BattleController_ApplyCardEffect_시그니처_CardData_1인자()
        {
            var method = typeof(BattleController).GetMethod("ApplyCardEffect",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method, "BattleController.ApplyCardEffect 메서드 존재");

            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length, "ApplyCardEffect 인자 수 = 1");
            Assert.AreEqual(typeof(CardData), parameters[0].ParameterType);
            Assert.AreEqual(typeof(void), method.ReturnType);
        }

        //# 회귀 — GetAppliedBuffs(EMonster) 가 IReadOnlyList<AppliedBuff> 반환.
        [Test]
        public void BattleController_GetAppliedBuffs_시그니처_EMonster_to_IReadOnlyList()
        {
            var method = typeof(BattleController).GetMethod("GetAppliedBuffs",
                BindingFlags.Instance | BindingFlags.Public);
            Assert.IsNotNull(method);

            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(EMonster), parameters[0].ParameterType);
            Assert.AreEqual(typeof(System.Collections.Generic.IReadOnlyList<Lair.UI.BattleViewModel.AppliedBuff>),
                method.ReturnType,
                "GetAppliedBuffs 반환 = IReadOnlyList<AppliedBuff>");
        }

        //# ===== IBattleContext 의 IncrementSpawnerOutput / ReplaceSpawnerOutput =====

        [Test]
        public void IBattleContext_IncrementSpawnerOutput_시그니처_EMonster_1인자()
        {
            var method = typeof(IBattleContext).GetMethod("IncrementSpawnerOutput");
            Assert.IsNotNull(method);
            var parameters = method.GetParameters();
            Assert.AreEqual(1, parameters.Length);
            Assert.AreEqual(typeof(EMonster), parameters[0].ParameterType);
        }

        [Test]
        public void IBattleContext_ReplaceSpawnerOutput_시그니처_EMonster_2인자()
        {
            var method = typeof(IBattleContext).GetMethod("ReplaceSpawnerOutput");
            Assert.IsNotNull(method);
            var parameters = method.GetParameters();
            Assert.AreEqual(2, parameters.Length);
            Assert.AreEqual(typeof(EMonster), parameters[0].ParameterType);
            Assert.AreEqual(typeof(EMonster), parameters[1].ParameterType);
        }

        //# ===== ICardEffect 구현 클래스 회귀 — 시그니처 변경 시 컴파일 깨짐 =====

        //# Lair.Card 어셈블리 안의 ICardEffect 구현 클래스 수 — 컴파일 검증 (회귀 측면).
        //# 25개 효과 클래스 모두 시그니처 그대로 유지 (Apply(IBattleContext) 1개 인자만).
        [Test]
        public void ICardEffect_구현_클래스_모두_Apply_1인자_보존()
        {
            //# Lair 어셈블리 안에서 ICardEffect 를 구현한 모든 타입 수집.
            var assembly = typeof(ICardEffect).Assembly;
            var implementations = assembly.GetTypes()
                .Where(t => t.IsClass && !t.IsAbstract && typeof(ICardEffect).IsAssignableFrom(t))
                .ToArray();

            Assert.Greater(implementations.Length, 0, "ICardEffect 구현체가 1개 이상 존재");

            //# 각 구현체의 Apply 메서드가 (IBattleContext) 1 인자 시그니처를 따르는지.
            //# 시그니처가 (IBattleContext, CardData) 로 확장된 적이 있던가? 라면 본 테스트 실패로 알아챈다.
            foreach (var impl in implementations)
            {
                var apply = impl.GetMethod("Apply", new[] { typeof(IBattleContext) });
                Assert.IsNotNull(apply,
                    $"{impl.Name} 의 Apply(IBattleContext) 시그니처 존재 — " +
                    "BLOCKER 4 결정대로 1 인자 유지");
            }
        }
    }
}
