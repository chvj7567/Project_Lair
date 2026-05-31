using System;
using Lair.Card;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — EternalBleedAura 단위 검증 (Debuff Tier3 시너지).
    //# 기획서 §4.2·§10.3 단정: ratio 0.01 (1%/s) — 영웅 *이동 시에만* 발동. 라운드 끝까지 무제한.
    //# 본 스위트는 IMover.IsMoving 분기 + ratio 적용 + 정지 시 무발동을 망라.
    public class EternalBleedAuraTests
    {
        //# 영웅 이동 중 1초 누적 → Max × 0.01 만큼 데미지.
        [Test]
        public void EternalBleed_이동_중_1초_경과_Max_1퍼센트_데미지()
        {
            FakeMover mover = new FakeMover { IsMoving = true };
            FakeHealthCounter hp = new FakeHealthCounter { Max = 1000 };
            EternalBleedAura aura = new EternalBleedAura(mover, 0.01f);

            aura.OnAttached(hp);
            aura.Tick(hp, 0.5f);   //# 누적 0.5s — 아직 1초 미만 → 데미지 없음
            Assert.AreEqual(0, hp.DamageTaken, "0.5s — 데미지 없음");

            aura.Tick(hp, 0.5f);   //# 누적 1.0s → 1회 데미지
            Assert.AreEqual(10, hp.DamageTaken, "1.0s 누적 — Max 1000 × 0.01 = 10 데미지");

            aura.Tick(hp, 1.0f);   //# 추가 1초 → 1회 더
            Assert.AreEqual(20, hp.DamageTaken, "2.0s 누적 — 추가 10 데미지");
        }

        //# 영웅 정지 시 데미지 없음 — IsMoving=false 분기.
        [Test]
        public void EternalBleed_정지_상태_Tick_데미지_없음()
        {
            FakeMover mover = new FakeMover { IsMoving = false };
            FakeHealthCounter hp = new FakeHealthCounter { Max = 1000 };
            EternalBleedAura aura = new EternalBleedAura(mover, 0.01f);

            aura.OnAttached(hp);
            for (int i = 0; i < 10; ++i) aura.Tick(hp, 1f);   //# 10초 동안 Tick

            Assert.AreEqual(0, hp.DamageTaken, "정지 상태 — 10초 누적 Tick 도 데미지 없음 (이동 조건)");
        }

        //# 정지 → 이동 → 정지 — 누적 시간이 정지 상태에서 보존되지 않음 (early return 으로 _acc 미증가).
        [Test]
        public void EternalBleed_정지_이동_정지_누적시간_이동중에만_누적()
        {
            FakeMover mover = new FakeMover { IsMoving = false };
            FakeHealthCounter hp = new FakeHealthCounter { Max = 1000 };
            EternalBleedAura aura = new EternalBleedAura(mover, 0.01f);

            aura.OnAttached(hp);

            //# 정지 상태 0.7s — 누적 안 됨.
            aura.Tick(hp, 0.7f);
            Assert.AreEqual(0, hp.DamageTaken, "정지 0.7s — 누적 없음");

            //# 이동 시작.
            mover.IsMoving = true;
            //# 이동 0.5s — 0.5s 누적, 아직 1초 미만.
            aura.Tick(hp, 0.5f);
            Assert.AreEqual(0, hp.DamageTaken, "이동 0.5s — 누적 0.5s, 데미지 없음");

            //# 정지 — _acc 보존되어야 함.
            mover.IsMoving = false;
            aura.Tick(hp, 0.4f);   //# 정지 — early return, _acc 변함 없음
            Assert.AreEqual(0, hp.DamageTaken, "정지 중 누적 없음");

            //# 다시 이동 — 0.6s 추가 → 0.5+0.6=1.1s → 1회 발동.
            mover.IsMoving = true;
            aura.Tick(hp, 0.6f);
            Assert.AreEqual(10, hp.DamageTaken, "재이동 0.6s 누적 → 총 1.1s → 1회 발동");
        }

        //# OnDetached — no-op (라운드 끝까지 무제한 정책 보장 — Detach 시 별도 복원 없음).
        [Test]
        public void EternalBleed_OnDetached_no_op_상태_변화_없음()
        {
            FakeMover mover = new FakeMover { IsMoving = true };
            FakeHealthCounter hp = new FakeHealthCounter { Max = 1000 };
            EternalBleedAura aura = new EternalBleedAura(mover, 0.01f);

            aura.OnAttached(hp);
            aura.Tick(hp, 1f);

            Assert.DoesNotThrow(() => aura.OnDetached(hp), "OnDetached 예외 없음");
            Assert.AreEqual(10, hp.DamageTaken, "Detach 후에도 누적 데미지 그대로");
        }

        //# Mover 또는 hp 가 null 일 때 — 안전 early return.
        [Test]
        public void EternalBleed_mover_null_안전_early_return()
        {
            FakeHealthCounter hp = new FakeHealthCounter { Max = 1000 };
            EternalBleedAura aura = new EternalBleedAura(null, 0.01f);

            aura.OnAttached(hp);
            Assert.DoesNotThrow(() => aura.Tick(hp, 1f), "mover null — 예외 없음");
            Assert.AreEqual(0, hp.DamageTaken, "mover null — 데미지 없음");
        }

        //# ===== Fakes =====

        private class FakeMover : IMover
        {
            public float Speed { get; set; } = 1f;
            public bool IsMoving { get; set; }
            public void MoveTo(Vector3 target) { }
            public void Stop() { }
        }

        //# 데미지 누적 추적용 FakeHealth.
        private class FakeHealthCounter : IHealth
        {
            public int Current { get; private set; } = 1000;
            public int Max { get; set; } = 1000;
            public float Ratio => (float)Current / Max;
            public bool IsAlive => Current > 0;
            public int DamageTaken { get; private set; }

            public event Action<int, int> OnChanged { add { } remove { } }
            public event Action OnDied { add { } remove { } }

            public void TakeDamage(int amount)
            {
                DamageTaken += amount;
                Current = Mathf.Max(0, Current - amount);
            }
            public void Heal(int amount) { }
            public void SetMax(int max, bool resetCurrent = true) { Max = max; }
        }
    }
}
