using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    //# CharacterAnimationDriver 통합/회귀 — Addressable 비의존, GameObject 합성으로 구성.
    //# Driver 는 Awake 에서 자체 AnimatorSink 를 만들어 실제 Animator 에 위임하므로 sink 호출을
    //# 직접 관찰할 수 없다. 따라서 관찰 가능한 축으로 검증한다:
    //#   (1) Health 이벤트 흐름이 Driver 를 거쳐도 예외(NRE) 없이 통과
    //#   (2) IAttacker.OnHit 구독자 수로 OnEnable/OnDisable 구독·해제 누수 검증
    //#   (3) 풀 재사용(OnDisable→OnEnable) 시 재구독·Reset 경로 정상
    public class HeroAnimationDriverTests
    {
        //# 구독자 수를 노출하는 페이크 공격자 — Driver 의 OnEnable/OnDisable 구독 누수 추적용.
        //# IAttacker 전 멤버 구현(부분 더블 아님). Driver 는 GetComponent<IAttacker>() 로 이걸 집는다.
        private class SubscriptionCountingAttacker : MonoBehaviour, IAttacker
        {
            public float Range => 1f;
            public float Cooldown => 1f;
            public int Power => 1;
            public bool Enabled { get; set; } = true;
            public float PowerScale { get; set; } = 1f;

            public event Action<IHealth> OnHit;

            //# 현재 OnHit 구독자 수 — 누수 검증의 핵심 메트릭.
            public int HitSubscriberCount => OnHit == null ? 0 : OnHit.GetInvocationList().Length;

            //# 테스트에서 공격 적중을 강제 발행 → Driver.HandleAttackHit 경로 구동.
            public void RaiseHit(IHealth target) => OnHit?.Invoke(target);

            public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now) => false;
        }

        private static GameObject MakeHero(out Health health, out SubscriptionCountingAttacker attacker)
        {
            GameObject go = new GameObject("HeroDriverTest");
            //# Animator(컨트롤러 미할당) — SetTrigger/SetFloat 가 경고만 내고 throw 안 함을 함께 검증.
            go.AddComponent<Animator>();
            health = go.AddComponent<Health>();
            attacker = go.AddComponent<SubscriptionCountingAttacker>();
            go.AddComponent<CharacterAnimationDriver>();
            return go;
        }

        //# ===== 통합: Health 데미지 흐름이 Driver 를 거쳐도 예외 없음 =====

        [UnityTest]
        public IEnumerator Driver_HP감소이벤트수신_예외없이처리()
        {
            GameObject go = MakeHero(out Health health, out _);
            //# Awake(Current=_max) → 다음 프레임 OnEnable 구독 완료 보장.
            yield return null;

            //# 실제 데미지 → Health.OnChanged(current<lastKnownHp) → Driver.OnDamaged(Time.time).
            health.TakeDamage(10);
            yield return null;

            //# 컨트롤러 미할당 Animator 에 Hit 트리거가 가도 throw 되지 않아야 함(스모크 가드).
            Assert.AreEqual(90, health.Current);
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Driver_공격적중이벤트수신_예외없이처리()
        {
            GameObject go = MakeHero(out _, out SubscriptionCountingAttacker attacker);
            yield return null;

            //# OnHit 발행 → Driver.HandleAttackHit → controller.OnAttack → sink.TriggerAttack(variant).
            attacker.RaiseHit(null);
            yield return null;

            Assert.IsNotNull(go);
            UnityEngine.Object.Destroy(go);
        }

        //# ===== 회귀: OnEnable/OnDisable 구독·해제 누수 없음 =====

        [UnityTest]
        public IEnumerator Driver_OnEnable시_OnHit단일구독()
        {
            GameObject go = MakeHero(out _, out SubscriptionCountingAttacker attacker);
            yield return null;

            //# OnEnable 에서 Driver 가 정확히 1회 구독.
            Assert.AreEqual(1, attacker.HitSubscriberCount, "OnEnable 후 OnHit 구독자 1");
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Driver_OnDisable시_OnHit구독해제()
        {
            GameObject go = MakeHero(out _, out SubscriptionCountingAttacker attacker);
            yield return null;
            Assert.AreEqual(1, attacker.HitSubscriberCount);

            go.SetActive(false);
            yield return null;

            //# OnDisable 에서 해제 → 0.
            Assert.AreEqual(0, attacker.HitSubscriberCount, "OnDisable 후 구독자 0 — 해제 누수 없음");
            UnityEngine.Object.Destroy(go);
        }

        [UnityTest]
        public IEnumerator Driver_풀재사용_반복enable시_구독중복없음()
        {
            GameObject go = MakeHero(out _, out SubscriptionCountingAttacker attacker);
            yield return null;
            Assert.AreEqual(1, attacker.HitSubscriberCount);

            //# 풀 반환→재사용 3회 반복 — 매번 1로 유지되어야 한다(누적 구독 = 누수 회귀).
            for (int i = 0; i < 3; i++)
            {
                go.SetActive(false);
                yield return null;
                Assert.AreEqual(0, attacker.HitSubscriberCount, $"{i}회차 disable 후 0");

                go.SetActive(true);
                yield return null;
                Assert.AreEqual(1, attacker.HitSubscriberCount, $"{i}회차 re-enable 후 1 — 중복구독 없음");
            }

            UnityEngine.Object.Destroy(go);
        }

        //# ===== 회귀: 사망(OnDied) 후 풀 재사용 시 Reset 으로 복귀 + 재구독 =====

        [UnityTest]
        public IEnumerator Driver_사망후_풀재사용_재구독되고_피격흐름복구()
        {
            GameObject go = MakeHero(out Health health, out SubscriptionCountingAttacker attacker);
            yield return null;
            Assert.AreEqual(1, attacker.HitSubscriberCount);

            //# 치명타로 사망 → Driver.HandleDied → controller.OnDied(Dead=true).
            health.TakeDamage(1000);
            yield return null;
            Assert.AreEqual(0, health.Current, "사망 처리");
            Assert.IsFalse(health.IsAlive);

            //# 풀 반환 → OnDisable(구독 해제).
            go.SetActive(false);
            yield return null;
            Assert.AreEqual(0, attacker.HitSubscriberCount, "사망 후 disable 시 구독 해제");

            //# 풀 재사용 → Health.OnEnable(Current 복원) + Driver.OnEnable(controller.Reset + 재구독).
            go.SetActive(true);
            yield return null;
            Assert.AreEqual(1, attacker.HitSubscriberCount, "재사용 시 단일 재구독");
            Assert.IsTrue(health.IsAlive, "Health 풀 재사용으로 HP 복원");
            Assert.Greater(health.Current, 0);

            //# Reset 으로 Dead 해제됐으므로 재사용 후 데미지 흐름이 다시 정상 처리(NRE 없음).
            health.TakeDamage(10);
            yield return null;
            Assert.AreEqual(health.Max - 10, health.Current, "재사용 후 데미지 흐름 복구");

            UnityEngine.Object.Destroy(go);
        }

        //# ===== 회귀: 비활성 상태에서 발생한 이벤트는 Driver 가 받지 않음 =====

        [UnityTest]
        public IEnumerator Driver_비활성중_공격이벤트_Driver무시()
        {
            GameObject go = MakeHero(out _, out SubscriptionCountingAttacker attacker);
            yield return null;

            go.SetActive(false);
            yield return null;
            Assert.AreEqual(0, attacker.HitSubscriberCount);

            //# 비활성 중 OnHit 발행 — 구독자 0 이므로 Driver 핸들러 호출 안 됨(예외 없이 무시).
            Assert.DoesNotThrow(() => attacker.RaiseHit(null));

            UnityEngine.Object.Destroy(go);
        }
    }
}
