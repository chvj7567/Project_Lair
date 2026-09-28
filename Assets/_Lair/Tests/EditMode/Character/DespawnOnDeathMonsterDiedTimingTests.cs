using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# 버그 수정 회귀 — MonsterDied 는 풀 반환 지연(_delay)과 무관하게 사망 순간 즉시 발행돼야 한다
    //# (monster-2d-conversion.md §5.5 불변식 1). root cause: 기존 코드는 지연 후 실행되는 DespawnNow() 안에서
    //# MonsterDied 를 발행해 _delay>0 프리팹에서 지연만큼 늦게 통지됐다 — 수정: HandleDied() 진입 즉시 발행.
    //# 기각 가설: "코루틴 자체가 문제" — 아니었다, 발행 위치(DespawnNow 내부)가 원인이었다(코루틴은 그대로 유지).
    //# 시드: gameplay-programmer. 본 테스트는 회귀 박제 1건 — 본격 스위트는 test-engineer.
    public class DespawnOnDeathMonsterDiedTimingTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
        }

        private static void InvokeHandleDied(DespawnOnDeath component)
        {
            MethodInfo mi = typeof(DespawnOnDeath).GetMethod("HandleDied",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "DespawnOnDeath.HandleDied 메서드 존재 — 시그니처 변경 감지");
            mi.Invoke(component, null);
        }

        private static void SetDelay(DespawnOnDeath component, float delay)
        {
            FieldInfo fi = typeof(DespawnOnDeath).GetField("_delay",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "DespawnOnDeath._delay 필드 존재 — 시그니처 변경 감지");
            fi.SetValue(component, delay);
        }

        [Test]
        public void 풀반환_지연이_있어도_MonsterDied는_HandleDied_호출_즉시_발행된다()
        {
            _go = new GameObject("MonsterUT", typeof(Health), typeof(MonsterTag));
            DespawnOnDeath despawn = _go.AddComponent<DespawnOnDeath>();
            SetDelay(despawn, 0.5f);

            bool fired = false;
            void Handler(Vector3 pos) => fired = true;
            DespawnOnDeath.MonsterDied += Handler;
            try
            {
                InvokeHandleDied(despawn);
                Assert.IsTrue(fired, "지연(_delay>0) 여부와 무관하게 HandleDied 호출 즉시 MonsterDied 가 발행된다");
            }
            finally
            {
                DespawnOnDeath.MonsterDied -= Handler;
            }
        }

        //# ───────── 이하 test-engineer 보강분 — ① 지연 0 케이스 + 실제 Health 흐름 통합 + 비-몬스터/구독해제 엣지 ─────────

        //# 엣지 — 지연이 0(기존 6종 현행값)인 경우도 동일하게 즉시 발행돼야 한다(회귀 대상 자체가 지연 유무 분기 버그였음).
        [Test]
        public void 지연이_0이어도_MonsterDied는_HandleDied_호출_즉시_발행된다()
        {
            _go = new GameObject("MonsterUT", typeof(Health), typeof(MonsterTag));
            DespawnOnDeath despawn = _go.AddComponent<DespawnOnDeath>();
            SetDelay(despawn, 0f);

            bool fired = false;
            void Handler(Vector3 pos) => fired = true;
            DespawnOnDeath.MonsterDied += Handler;
            try
            {
                InvokeHandleDied(despawn);
                Assert.IsTrue(fired, "_delay=0 에서도 HandleDied 호출 즉시 MonsterDied 발행");
            }
            finally
            {
                DespawnOnDeath.MonsterDied -= Handler;
            }
        }

        private static void InvokeVoid(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 메서드 존재 확인 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        //# 통합 — 리플렉션으로 HandleDied 를 직접 부르는 대신, 실제 Health.OnDied 이벤트 체인(TakeDamage → 사망)을
        //# 그대로 태워 MonsterDied 가 TakeDamage 호출이 반환되기 전에(동기) 발행되는지 검증. 지연 0/있음 둘 다.
        [TestCase(0f)]
        [TestCase(0.5f)]
        public void 실제_사망_흐름에서_TakeDamage_호출_안에서_MonsterDied가_동기발행된다(float delay)
        {
            _go = new GameObject("MonsterUT", typeof(Health), typeof(MonsterTag));
            Health health = _go.GetComponent<Health>();
            DespawnOnDeath despawn = _go.AddComponent<DespawnOnDeath>();
            SetDelay(despawn, delay);

            InvokeVoid(health, "Awake");     //# Current = Max
            InvokeVoid(despawn, "Awake");    //# _health 캐시
            InvokeVoid(despawn, "OnEnable"); //# Health.OnDied 구독

            bool firedDuringCall = false;
            void Handler(Vector3 pos) => firedDuringCall = true;
            DespawnOnDeath.MonsterDied += Handler;
            try
            {
                health.TakeDamage(999999);   //# 오버킬 → Current 0 → OnDied 동기 발행
                Assert.IsTrue(firedDuringCall,
                    $"delay={delay} 여부와 무관하게 TakeDamage 호출 안에서 MonsterDied 가 발행된다(피의 갈증 카드 타이밍 불변)");
            }
            finally
            {
                DespawnOnDeath.MonsterDied -= Handler;
            }
        }

        //# 엣지 — MonsterTag 가 없는 캐릭터(영웅 등)의 사망은 MonsterDied 를 발행하지 않는다.
        [Test]
        public void MonsterTag가_없으면_MonsterDied가_발행되지_않는다()
        {
            _go = new GameObject("HeroUT", typeof(Health));   //# MonsterTag 미부착
            DespawnOnDeath despawn = _go.AddComponent<DespawnOnDeath>();
            SetDelay(despawn, 0f);

            bool fired = false;
            void Handler(Vector3 pos) => fired = true;
            DespawnOnDeath.MonsterDied += Handler;
            try
            {
                InvokeHandleDied(despawn);
                Assert.IsFalse(fired, "MonsterTag 없는 캐릭터(영웅 등) 사망은 MonsterDied 를 발행하지 않는다");
            }
            finally
            {
                DespawnOnDeath.MonsterDied -= Handler;
            }
        }

        //# 엣지 — OnDisable(구독 해제, 예: 풀 반환 직후 비활성) 이후에는 Health 가 사망해도 HandleDied 가 불리지 않는다.
        [Test]
        public void OnDisable_이후에는_구독이_해제되어_MonsterDied가_발행되지_않는다()
        {
            _go = new GameObject("MonsterUT", typeof(Health), typeof(MonsterTag));
            Health health = _go.GetComponent<Health>();
            DespawnOnDeath despawn = _go.AddComponent<DespawnOnDeath>();
            SetDelay(despawn, 0f);

            InvokeVoid(health, "Awake");
            InvokeVoid(despawn, "Awake");
            InvokeVoid(despawn, "OnEnable");
            InvokeVoid(despawn, "OnDisable");   //# 구독 해제 시뮬레이션
            //# AddComponent 시점의 Unity 자동 OnEnable 호출 여부가 불확실 —
            //# 중복 구독이 있었더라도 완전히 해제되도록 방어적으로 한 번 더 호출(멱등, 안전).
            InvokeVoid(despawn, "OnDisable");

            bool fired = false;
            void Handler(Vector3 pos) => fired = true;
            DespawnOnDeath.MonsterDied += Handler;
            try
            {
                health.TakeDamage(999999);
                Assert.IsFalse(fired, "OnDisable 이후 구독 해제 — Health.OnDied 는 발행돼도 HandleDied 는 불리지 않는다");
            }
            finally
            {
                DespawnOnDeath.MonsterDied -= Handler;
            }
        }
    }
}
