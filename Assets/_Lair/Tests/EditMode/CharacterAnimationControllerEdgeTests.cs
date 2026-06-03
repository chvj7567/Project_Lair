using System;
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    //# CharacterAnimationController 의 엣지·경계·상태전이 망라.
    //# 기존 CharacterAnimationControllerTests(정상 12케이스)와 중복 회피 — 경계값·시퀀스·조합만 다룬다.
    //# 결정성: now 인자 직접 주입 + seed 고정 rng → Time.time 비의존.
    public class CharacterAnimationControllerEdgeTests
    {
        private const float HitCooldown = 0.4f;
        private const float AttackSuppress = 0.5f;

        //# variant 기록까지 가능한 풀 더블(IAnimatorSink 전 멤버 구현 — 부분 더블 아님).
        private class RecordingSink : IAnimatorSink
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

        //# seed 고정 — variant 시퀀스 결정성 확보.
        private CharacterAnimationController Make(RecordingSink sink, int seed = 12345)
            => new CharacterAnimationController(
                sink, hitReactionCooldown: HitCooldown, attackSuppressWindow: AttackSuppress, rng: new Random(seed));

        //# ===== 공격 억제창 경계값 (정확히 0.5s) =====

        [Test]
        public void OnDamaged_공격억제창_정확히경계미만_억제()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 1f);
            //# now - lastAttack = 0.4999 < 0.5 → 억제 (조건은 strict <)
            c.OnDamaged(now: 1.4999f);
            Assert.AreEqual(0, sink.HitCount);
        }

        [Test]
        public void OnDamaged_공격억제창_정확히경계_허용()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 1f);
            //# now - lastAttack = 0.5 → !(0.5 < 0.5) → 억제 통과. (쿨다운은 첫 피격이라 통과)
            c.OnDamaged(now: 1.5f);
            Assert.AreEqual(1, sink.HitCount);
        }

        //# ===== 피격 쿨다운 경계값 (정확히 0.4s) =====

        [Test]
        public void OnDamaged_쿨다운_정확히경계미만_억제()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            //# now - lastHit = 0.3999 < 0.4 → 억제
            c.OnDamaged(now: 0.3999f);
            Assert.AreEqual(1, sink.HitCount);
        }

        [Test]
        public void OnDamaged_쿨다운_정확히경계_허용()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            //# now - lastHit = 0.4 → !(0.4 < 0.4) → 허용
            c.OnDamaged(now: 0.4f);
            Assert.AreEqual(2, sink.HitCount);
        }

        //# ===== 연속 공격 variant 시퀀스 (seed 고정 결정성) =====

        [Test]
        public void OnAttack_연속공격_variant매번갱신_seed고정시퀀스재현()
        {
            //# 동일 seed 두 컨트롤러 → 같은 variant 시퀀스가 나와야 결정적(System.Random 계약).
            RecordingSink sinkA = new RecordingSink();
            RecordingSink sinkB = new RecordingSink();
            CharacterAnimationController a = Make(sinkA, seed: 999);
            CharacterAnimationController b = Make(sinkB, seed: 999);

            int[] seqA = new int[20];
            int[] seqB = new int[20];
            for (int i = 0; i < 20; i++)
            {
                a.OnAttack(now: i);
                seqA[i] = sinkA.LastAttackVariant;
                b.OnAttack(now: i);
                seqB[i] = sinkB.LastAttackVariant;
            }

            CollectionAssert.AreEqual(seqA, seqB);
            Assert.AreEqual(20, sinkA.AttackCount);
            //# variant 가 매 공격마다 0~2 범위 안에서 실제로 갱신되는지(전부 동일값 고정 아님).
            bool anyDiffer = false;
            for (int i = 1; i < 20; i++)
            {
                if (seqA[i] != seqA[i - 1])
                {
                    anyDiffer = true;
                    break;
                }
            }
            Assert.IsTrue(anyDiffer, "variant 가 시퀀스 내내 동일값으로 고정됨 — 랜덤 갱신 누락 의심");
        }

        [Test]
        public void OnAttack_variant_항상_0_1_2_중하나()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            for (int i = 0; i < 200; i++)
            {
                c.OnAttack(now: i);
                Assert.GreaterOrEqual(sink.LastAttackVariant, 0);
                Assert.LessOrEqual(sink.LastAttackVariant, 2);
            }
        }

        //# ===== 사망 후 Reset 전까지 모든 입력 무시 =====

        [Test]
        public void OnDied_후_OnDamaged무시()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.OnDamaged(now: 5f);
            Assert.AreEqual(0, sink.HitCount);
        }

        [Test]
        public void OnDied_후_OnAttack과Tick과OnDamaged_전부무시()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            sink.Speed = 42f;

            c.OnAttack(now: 1f);
            c.OnDamaged(now: 2f);
            c.Tick(isMoving: true, isFleeing: true, walkSpeed: 1f, runSpeed: 2f);

            Assert.AreEqual(0, sink.AttackCount, "사망 후 공격 무시");
            Assert.AreEqual(0, sink.HitCount, "사망 후 피격 무시");
            Assert.AreEqual(42f, sink.Speed, "사망 후 Speed 갱신 안 함");
            Assert.IsTrue(sink.Dead);
        }

        [Test]
        public void OnDied_후_Reset_그후입력정상수신()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.OnAttack(now: 1f);   //# 무시됨
            c.Reset();

            //# Reset 후 lastAttack/lastHit 가 NegativeInfinity 로 복원 → 첫 입력 즉시 통과.
            c.OnAttack(now: 2f);
            c.OnDamaged(now: 2f);
            Assert.AreEqual(1, sink.AttackCount, "Reset 후 공격 1회");
            //# OnAttack(2) 직후 OnDamaged(2) 는 공격억제창(0.5) 내라 억제되는 게 정상.
            Assert.AreEqual(0, sink.HitCount, "Reset 직후 공격과 동시각 피격은 억제창에 막힘");
            Assert.IsFalse(sink.Dead, "Reset 으로 Dead 해제");
        }

        [Test]
        public void Reset_쿨다운타이머도초기화_재트리거즉시가능()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 10f);   //# lastHit=10
            Assert.AreEqual(1, sink.HitCount);

            c.Reset();
            //# Reset 으로 lastHit=-inf → now=10.1 이 직전(10) 과 0.1 차이여도 즉시 통과.
            c.OnDamaged(now: 10.1f);
            Assert.AreEqual(2, sink.HitCount, "Reset 후 쿨다운 타이머 초기화로 즉시 재트리거");
        }

        //# ===== OnSpawn 은 사망/생존 상태와 무관 =====

        [Test]
        public void OnSpawn_사망상태에서도_트리거발행()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.OnSpawn();
            //# OnSpawn 은 _dead 가드를 보지 않는다(입장 연출은 상태 무관) — 1회 발행.
            Assert.AreEqual(1, sink.SpawnCount);
        }

        [Test]
        public void OnSpawn_복수호출_매번발행()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.OnSpawn();
            c.OnSpawn();
            Assert.AreEqual(2, sink.SpawnCount);
        }

        //# ===== Tick 모순 입력 처리 (정지 우선) =====

        [Test]
        public void Tick_정지인데도주플래그_정지우선_속도0()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            //# isMoving=false 가 우선 — isFleeing=true 라도 Speed 0.
            c.Tick(isMoving: false, isFleeing: true, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(0f, sink.Speed);
        }

        [Test]
        public void Tick_이동중도주해제_걷기속도()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: true, isFleeing: true, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(2f, sink.Speed);
            //# 같은 프레임 흐름에서 도주 해제 → walk 으로 즉시 전환.
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(1f, sink.Speed);
        }

        //# ===== 상태 전이 조합: 도주 중 피격 (§2.2-b — Hit 허용) =====

        [Test]
        public void OnDamaged_도주중피격_쿨다운만통과하면_Hit허용()
        {
            //# 컨트롤러는 FleeMode 를 보지 않는다(§2.2-b). 공격을 안 했으므로 억제창 비활성 → 쿨다운만 본다.
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            //# 도주 중에는 OnAttack 호출이 없음 → lastAttack = -inf → 억제창 항상 통과.
            c.OnDamaged(now: 0f);
            c.OnDamaged(now: 0.5f);
            Assert.AreEqual(2, sink.HitCount, "도주 중에도 쿨다운만 만족하면 매번 Hit");
        }

        //# ===== 공격 직후 쿨다운/억제창 상호작용 =====

        [Test]
        public void OnDamaged_공격억제창통과후_쿨다운도따로검사()
        {
            RecordingSink sink = new RecordingSink();
            CharacterAnimationController c = Make(sink);
            //# 0s 공격 → 0.6s 피격: 억제창(0.5) 통과 + 첫 피격이라 쿨다운 통과 → Hit.
            c.OnAttack(now: 0f);
            c.OnDamaged(now: 0.6f);
            Assert.AreEqual(1, sink.HitCount);
            //# 0.7s 피격: 직전 Hit(0.6) 과 0.1 차 < 0.4 쿨다운 → 억제.
            c.OnDamaged(now: 0.7f);
            Assert.AreEqual(1, sink.HitCount);
            //# 1.1s 피격: 직전 Hit(0.6) 과 0.5 차 ≥ 0.4 쿨다운 → Hit.
            //# 0.6f 는 0.60000002 로 부정확 표현 → 1.0f 경계(0.4 차)는 float 손실로 0.39999998<0.4 라 막힘.
            //# 경계가 아닌 명확히 넘기는 값(0.5 차)으로 쿨다운 충족 의도를 안정적으로 검증.
            c.OnDamaged(now: 1.1f);
            Assert.AreEqual(2, sink.HitCount);
        }
    }
}
