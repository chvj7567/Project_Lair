using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# HeroAttackGate(IAttackGate) 계약 — hero-animation-timing-sync §3.2·§2.7.
    //# 순수 상태 전이(BeginAttack→IsAttacking→EndAttack)와 OnAttackBegin 이벤트는 EditMode 에서 검증.
    //# (_attackEndFallback 타임아웃·OnEnable 리셋은 Update/Time.time/라이프사이클 의존 → PlayMode 스위트에서 보강.)
    //# MonoBehaviour 지만 new 로 생성해 BeginAttack/EndAttack(Update 비경유)만 호출 — Time.time 은 BeginAttack 의 _beginTime
    //# 기록에만 쓰이고 IsAttacking 전이엔 영향 없으므로 결정적.
    public class HeroAttackGateTests
    {
        [Test]
        public void 초기상태_IsAttacking_false()
        {
            HeroAttackGate gate = new HeroAttackGate();
            Assert.IsFalse(gate.IsAttacking, "생성 직후 공격 중 아님");
        }

        [Test]
        public void BeginAttack_IsAttacking_true()
        {
            HeroAttackGate gate = new HeroAttackGate();
            gate.BeginAttack();
            Assert.IsTrue(gate.IsAttacking, "BeginAttack 후 공격 중");
        }

        [Test]
        public void BeginAttack_후_EndAttack_IsAttacking_false()
        {
            HeroAttackGate gate = new HeroAttackGate();
            gate.BeginAttack();
            gate.EndAttack();
            Assert.IsFalse(gate.IsAttacking, "EndAttack 후 공격 중 해제");
        }

        //# OnAttackBegin 은 BeginAttack 마다 정확히 1회 발행(§2.7 ②) — Driver 가 이 신호로 TriggerAttack.
        [Test]
        public void BeginAttack_OnAttackBegin_정확히1회발행()
        {
            HeroAttackGate gate = new HeroAttackGate();
            int count = 0;
            gate.OnAttackBegin += () => count++;

            gate.BeginAttack();

            Assert.AreEqual(1, count, "개시당 OnAttackBegin 1회");
        }

        //# 연속 BeginAttack 2회 → 이벤트 2회. (게이트는 게임플레이 판정 안 함 — 받은 만큼 신호)
        [Test]
        public void BeginAttack_연속2회_OnAttackBegin_2회()
        {
            HeroAttackGate gate = new HeroAttackGate();
            int count = 0;
            gate.OnAttackBegin += () => count++;

            gate.BeginAttack();
            gate.EndAttack();
            gate.BeginAttack();

            Assert.AreEqual(2, count);
        }

        //# EndAttack 단독 호출(begin 없이) — IsAttacking 은 false 유지(멱등, 음의 전이 없음).
        [Test]
        public void EndAttack_begin없이_호출시_false유지()
        {
            HeroAttackGate gate = new HeroAttackGate();
            gate.EndAttack();
            Assert.IsFalse(gate.IsAttacking);
        }

        //# 이벤트 구독 없이 BeginAttack — NRE 없이 정상(?. 호출).
        [Test]
        public void BeginAttack_구독자없어도_예외없음()
        {
            HeroAttackGate gate = new HeroAttackGate();
            Assert.DoesNotThrow(() => gate.BeginAttack());
            Assert.IsTrue(gate.IsAttacking);
        }
    }
}
