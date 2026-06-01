using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Card;
using Lair.Character;

namespace Lair.Tests.Battle
{
    //# HeroAuraRunner.Update 재진입(re-entrancy) 자체 테스트 — 정상 1 + 엣지 1.
    //# 엣지: Aura.Tick 도중 _slots 가 비워져도(영웅 사망→OnDisable cascade 모사) Update 가 예외 없이 끝나는지.
    //# 본격 엣지·회귀·통합은 test-engineer 단계.
    public class HeroAuraRunnerReentrancyTests
    {
        //# EditMode 는 Awake/OnEnable 을 호출하지 않으므로 lifecycle/private 필드를 직접 구동.
        private static void Invoke(MonoBehaviour mb, string method)
        {
            MethodInfo m = mb.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(m, $"{mb.GetType().Name}.{method} 미발견 — 시그니처 변경?");
            m.Invoke(mb, null);
        }

        //# Public + NonPublic 둘 다 — Slot.Remain 은 public 필드, _hero/_slots 는 private.
        private static void SetField(object target, string field, object value)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} 미발견 — 시그니처 변경?");
            f.SetValue(target, value);
        }

        private static object GetField(object target, string field)
        {
            FieldInfo f = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.IsNotNull(f, $"{target.GetType().Name}.{field} 미발견 — 시그니처 변경?");
            return f.GetValue(target);
        }

        //# IHealth 더블 — runner 는 IsAlive·TakeDamage 만 본다.
        private class FakeHero : MonoBehaviour, IHealth
        {
            public int Max => 100;
            public int Current { get; private set; } = 100;
            public float Ratio => Current / 100f;
            public bool IsAlive => Current > 0;
            public event Action<int, int> OnChanged;
            public event Action OnDied;
            public void TakeDamage(int amount)
            {
                Current -= amount;
                OnChanged?.Invoke(Current, Max);
                if (Current <= 0) OnDied?.Invoke();
            }
            public void Heal(int amount) { }
            public void SetMax(int max, bool resetCurrent = true) { }
        }

        //# 만료 동작 검증용 — Tick/OnDetached 호출 횟수만 센다.
        private class CountingAura : IHeroAura
        {
            public int TickCount;
            public int DetachedCount;
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt) => TickCount++;
            public void OnDetached(IHealth hero) => DetachedCount++;
        }

        //# 엣지 모사 — Tick 이 호출되면 runner.OnDisable 을 유발해 _slots 를 비운다.
        //# (실전: DoT Tick → 영웅 사망 → OnDied → EndBattle → 영웅 비활성 → OnDisable Clear)
        private class SelfDisablingAura : IHeroAura
        {
            private readonly HeroAuraRunner _runner;
            public int TickCount;
            public SelfDisablingAura(HeroAuraRunner runner) => _runner = runner;
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt)
            {
                TickCount++;
                //# OnDisable 이 _slots.Clear() 실행 — Update 루프 한복판에서 컬렉션 비움.
                Invoke(_runner, "OnDisable");
            }
            public void OnDetached(IHealth hero) { }
        }

        [Test]
        public void 만료된_슬롯은_OnDetached후_제거된다()
        {
            GameObject go = new GameObject("hero");
            FakeHero hero = go.AddComponent<FakeHero>();
            HeroAuraRunner runner = go.AddComponent<HeroAuraRunner>();
            SetField(runner, "_hero", hero);   //# Awake 미실행 대체 — IHealth 캐시 주입.

            CountingAura aura = new CountingAura();
            runner.Attach(aura, 0.5f);   //# 0.5초 만료.

            //# deltaTime 이 Remain 을 넘기도록 충분히 큰 dt 가 필요하나 EditMode 는 0.
            //# Remain 을 음수로 직접 세팅해 만료 분기를 강제(슬롯 1개).
            IList slots = (IList)GetField(runner, "_slots");
            object slot = slots[0];
            SetField(slot, "Remain", -1f);

            Invoke(runner, "Update");

            Assert.AreEqual(1, aura.TickCount, "만료 슬롯도 이번 프레임 Tick 1회는 받는다");
            Assert.AreEqual(1, aura.DetachedCount, "만료 시 OnDetached 1회");
            Assert.AreEqual(0, slots.Count, "만료 후 슬롯 제거");

            UnityEngine.Object.DestroyImmediate(go);
        }

        [Test]
        public void Tick중_slots가_비워져도_Update가_예외없이_종료된다()
        {
            GameObject go = new GameObject("hero");
            FakeHero hero = go.AddComponent<FakeHero>();
            HeroAuraRunner runner = go.AddComponent<HeroAuraRunner>();
            SetField(runner, "_hero", hero);

            //# Tick 중 _slots 를 비우는 aura 부착 → 재진입으로 _slots 가 비면
            //# 루프가 비워진 컬렉션을 만나 IndexOutOfRange 를 던지던 회귀.
            runner.Attach(new SelfDisablingAura(runner), 0.5f);

            //# Remain 을 음수로 강제 만료 — 가드 제거 시 만료 분기→빈 리스트 RemoveAt 로 throw 하게 만들어
            //# 가드가 실제로 그 경로를 막는지 실증(가드 없으면 RED, 있으면 early-return GREEN).
            IList slots = (IList)GetField(runner, "_slots");
            SetField(slots[0], "Remain", -1f);

            Assert.DoesNotThrow(() => Invoke(runner, "Update"),
                "Tick 도중 _slots 가 비워져도 Update 는 예외 없이 종료해야 한다");
            Assert.AreEqual(0, slots.Count, "OnDisable cascade 로 슬롯은 정리된 상태");

            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
