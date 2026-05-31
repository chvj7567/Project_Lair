using Lair.Battle;
using Lair.Card;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — MarkOfDeathAura 단위 검증.
    //# 기획서 §10.4 디자인 단정: 영웅 받는 데미지 ×1.5, _duration 초.
    //# Health.DamageTakenScale 을 곱연산으로 누적 / Detach 시 동일 배율로 나누어 복원.
    public class MarkOfDeathAuraTests
    {
        [Test]
        public void MarkOfDeath_OnAttached_DamageTakenScale_1점5_곱연산()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);
                Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f, "선조건 — 기본 1.0");

                MarkOfDeathAura aura = new MarkOfDeathAura(1.5f);
                aura.OnAttached(hp);

                Assert.AreEqual(1.5f, hp.DamageTakenScale, 0.0001f,
                    "OnAttached → DamageTakenScale ×= 1.5");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# OnDetached → 동일 배율로 나누어 복원 — Mark 가 지속시간만큼만 작용 (§10.4).
        [Test]
        public void MarkOfDeath_OnDetached_DamageTakenScale_복원_1점0()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);

                MarkOfDeathAura aura = new MarkOfDeathAura(1.5f);
                aura.OnAttached(hp);
                Assert.AreEqual(1.5f, hp.DamageTakenScale, 0.0001f, "Attach 후 1.5");

                aura.OnDetached(hp);
                Assert.AreEqual(1f, hp.DamageTakenScale, 0.0001f,
                    "Detach 후 1.0 복원 (1.5 / 1.5 = 1.0)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# 재부착 가드 — _applied 가 true 면 두 번째 OnAttached 는 곱연산 안 함 (이중 적용 방지).
        [Test]
        public void MarkOfDeath_OnAttached_재호출_곱연산_재적용_안됨()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);

                MarkOfDeathAura aura = new MarkOfDeathAura(1.5f);
                aura.OnAttached(hp);
                aura.OnAttached(hp);   //# 재호출 — _applied 가드로 무시

                Assert.AreEqual(1.5f, hp.DamageTakenScale, 0.0001f,
                    "OnAttached 재호출 → 동일 인스턴스 곱연산 1회만 (×1.5)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# 두 개의 다른 Aura 인스턴스 부착 — 각각 OnAttached 가 호출되면 누적 곱연산 (×1.5 × ×1.5 = ×2.25).
        //# 단 HeroAuraRunner 의 같은 type 가드가 이를 막으므로 본 케이스는 Aura 자체 검증.
        [Test]
        public void MarkOfDeath_두_인스턴스_부착_DamageTakenScale_곱연산_누적()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);

                MarkOfDeathAura a1 = new MarkOfDeathAura(1.5f);
                MarkOfDeathAura a2 = new MarkOfDeathAura(1.5f);
                a1.OnAttached(hp);
                a2.OnAttached(hp);

                Assert.AreEqual(1.5f * 1.5f, hp.DamageTakenScale, 0.0001f,
                    "두 다른 인스턴스 OnAttached → 각 ×1.5 누적 = ×2.25");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# 부착 후 영웅 TakeDamage — DamageTakenScale ×1.5 적용해 실제 데미지가 1.5배 되는지 검증.
        //# Health.TakeDamage 가 DamageTakenScale 을 곱해 실제 데미지로 환산하는 경로 확인.
        [Test]
        public void MarkOfDeath_부착_중_TakeDamage_1점5배_적용()
        {
            GameObject heroGo = new GameObject("hero_t");
            try
            {
                Health hp = heroGo.AddComponent<Health>();
                hp.SetMax(100, resetCurrent: true);

                MarkOfDeathAura aura = new MarkOfDeathAura(1.5f);
                aura.OnAttached(hp);

                int before = hp.Current;
                hp.TakeDamage(10);   //# DamageTakenScale 1.5 → 실제 15 차감
                int actualDamage = before - hp.Current;

                Assert.AreEqual(15, actualDamage, "Mark 부착 중 — 받는 데미지 ×1.5 (10 → 15)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# IHealth 가 Health 가 아닌 stub 일 때 (테스트 경로) — OnAttached early-return 으로 안전.
        [Test]
        public void MarkOfDeath_Health_아닌_IHealth_stub_OnAttached_안전_무동작()
        {
            FakeHealth fake = new FakeHealth();
            MarkOfDeathAura aura = new MarkOfDeathAura(1.5f);

            Assert.DoesNotThrow(() => aura.OnAttached(fake),
                "Health 가 아닌 IHealth stub — early return, 예외 없음");
            Assert.DoesNotThrow(() => aura.OnDetached(fake),
                "stub 에서 OnDetached 도 안전");
        }

        //# FakeHealth — IHealth 전체 멤버 implement (Rule: production 인터페이스 부분 더블 금지).
        private class FakeHealth : IHealth
        {
            public int Current => 100;
            public int Max => 100;
            public float Ratio => 1f;
            public bool IsAlive => true;
            public event System.Action<int, int> OnChanged { add { } remove { } }
            public event System.Action OnDied { add { } remove { } }
            public void TakeDamage(int amount) { }
            public void Heal(int amount) { }
            public void SetMax(int max, bool resetCurrent = true) { }
        }
    }
}
