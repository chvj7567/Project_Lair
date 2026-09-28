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
    }
}
