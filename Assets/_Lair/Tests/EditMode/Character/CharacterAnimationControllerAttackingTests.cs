using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# CharacterAnimationController.SetAttacking(영웅 게이트 미러) — hero-animation-timing-sync §3.4.
    //# 신규 SetAttacking 분기는 기존 CharacterAnimationControllerTests(12)/...EdgeTests(16) 어디에도 없는 빈 축 → 본 파일이 채운다.
    //# 검증: IsAttacking=true 시 OnDamaged 가 피격 Hit 을 억제(공격 모션 안 끊김). false(기본) 시 기존 0.5s 윈도우 동작 보존.
    //# Dead 최우선·OnAttack·Tick 등 기존 28건 가정은 SetAttacking 토글과 직교함을 함께 확인.
    public class CharacterAnimationControllerAttackingTests
    {
        private class FakeSink : IAnimatorSink
        {
            public float Speed;
            public int AttackCount;
            public int LastAttackVariant = -1;
            public int HitCount;
            public bool Dead;
            public int SpawnCount;
            public void SetSpeed(float speed) => Speed = speed;
            public void TriggerAttack(int variant) { AttackCount++; LastAttackVariant = variant; }
            public void TriggerHit() => HitCount++;
            public void SetDead(bool dead) => Dead = dead;
            public void TriggerSpawn() => SpawnCount++;
        }

        //# hitCooldown=0.4, attackSuppress=0.5 — 기존 테스트와 동일 파라미터로 회귀 정합 보장.
        private CharacterAnimationController Make(FakeSink sink)
            => new CharacterAnimationController(sink, hitReactionCooldown: 0.4f, attackSuppressWindow: 0.5f);

        //# ===== SetAttacking=true 시 피격 Hit 억제 (§3.4) =====

        //# IsAttacking=true 동안 OnDamaged 가 Hit 트리거를 억제 — 공격 스윙이 끊기지 않게.
        [Test]
        public void SetAttacking_true_피격Hit_억제()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.SetAttacking(true);
            c.OnDamaged(now: 0f);

            Assert.AreEqual(0, sink.HitCount, "공격 모션 중(IsAttacking) 피격 리액션 억제");
        }

        //# IsAttacking=true 면 쿨다운·공격억제윈도우와 무관하게 모든 피격 억제 — 클립 전체(0.5s 초과) 보호.
        [Test]
        public void SetAttacking_true_장시간경과해도_Hit억제_지속()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.SetAttacking(true);
            //# 0.5s(suppressWindow)·0.4s(hitCooldown) 모두 초과한 시점이어도 IsAttacking 이 우선 억제.
            c.OnDamaged(now: 2.0f);
            c.OnDamaged(now: 4.0f);

            Assert.AreEqual(0, sink.HitCount, "IsAttacking 동안은 윈도우 초과여도 억제(클립 전체 보호)");
        }

        //# ===== SetAttacking=false(기본) 시 기존 0.5s 윈도우 동작 보존 (회귀) =====

        //# IsAttacking=false 면 기존대로 첫 피격은 Hit 트리거(억제 안 함).
        [Test]
        public void SetAttacking_false_첫피격_Hit트리거()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.SetAttacking(false);
            c.OnDamaged(now: 0f);

            Assert.AreEqual(1, sink.HitCount, "공격 중 아니면 기존 피격 리액션 정상");
        }

        //# true→false 전이 후 피격 정상 — 클립 종료(EndAttack) 후 피격 리액션 복구.
        [Test]
        public void SetAttacking_true후_false전이_피격리액션_복구()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.SetAttacking(true);
            c.OnDamaged(now: 0f);
            Assert.AreEqual(0, sink.HitCount, "공격 중 억제");

            c.SetAttacking(false);
            c.OnDamaged(now: 1.0f);   //# 직전 lastHit/lastAttack 갱신 없었으므로 쿨다운/윈도우 통과.
            Assert.AreEqual(1, sink.HitCount, "공격 종료 후 피격 리액션 복구");
        }

        //# 회귀: SetAttacking 미호출(몬스터 경로 — 게이트 null) 시 controller 는 기본 false 라 기존 동작 동일.
        [Test]
        public void SetAttacking_미호출_기본false_기존동작_보존()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            //# SetAttacking 한 번도 호출 안 함 = 몬스터 경로.
            c.OnDamaged(now: 0f);

            Assert.AreEqual(1, sink.HitCount, "게이트 없는 몬스터는 기존 피격 리액션 그대로");
        }

        //# ===== Dead 최우선 — IsAttacking 과 직교 =====

        //# Dead 면 IsAttacking 여부와 무관하게 OnDamaged 가 항상 억제(기존 Dead 가정 보존).
        [Test]
        public void Dead_상태에서는_SetAttacking무관하게_Hit억제()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.OnDied();
            c.SetAttacking(true);
            c.OnDamaged(now: 0f);
            c.SetAttacking(false);
            c.OnDamaged(now: 1.0f);

            Assert.AreEqual(0, sink.HitCount, "사망 중에는 IsAttacking 토글과 무관하게 피격 억제");
        }

        //# Reset 후 IsAttacking 도 false 로 초기화 — 풀 재사용 시 공격 억제 잔존 0(상태 누수 방지).
        [Test]
        public void Reset_후_IsAttacking_초기화_피격리액션_정상()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);

            c.SetAttacking(true);   //# 전 사이클에서 공격 중이던 잔존 상태 시뮬.
            c.Reset();

            //# Reset 이 _isAttacking=false 로 리셋했으므로 첫 피격이 정상 트리거.
            c.OnDamaged(now: 0f);
            Assert.AreEqual(1, sink.HitCount, "Reset 후 IsAttacking 잔존 없음 → 피격 리액션 복구");
        }
    }
}
