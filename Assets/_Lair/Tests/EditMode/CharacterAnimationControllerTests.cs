using System;
using NUnit.Framework;
using Lair.Character;

namespace Lair.Tests.EditMode
{
    public class CharacterAnimationControllerTests
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

        //# walkSpeed=1, runSpeed=2, hitCooldown=0.4, attackSuppress=0.5 / seed 고정으로 variant 결정성 확보.
        private CharacterAnimationController Make(FakeSink sink)
            => new CharacterAnimationController(sink, hitReactionCooldown: 0.4f, attackSuppressWindow: 0.5f, rng: new Random(12345));

        [Test]
        public void Tick_정지_속도0()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: false, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(0f, sink.Speed);
        }

        [Test]
        public void Tick_이동_걷기속도()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(1f, sink.Speed);
        }

        [Test]
        public void Tick_도주_달리기속도()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.Tick(isMoving: true, isFleeing: true, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(2f, sink.Speed);
        }

        [Test]
        public void OnAttack_공격트리거_1회()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 0f);
            Assert.AreEqual(1, sink.AttackCount);
        }

        [Test]
        public void OnAttack_variant_0에서2_범위내()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            for (int i = 0; i < 50; i++)
            {
                c.OnAttack(now: i);
                Assert.GreaterOrEqual(sink.LastAttackVariant, 0);
                Assert.LessOrEqual(sink.LastAttackVariant, 2);
            }
        }

        [Test]
        public void OnDamaged_첫피격_히트트리거()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            Assert.AreEqual(1, sink.HitCount);
        }

        [Test]
        public void OnDamaged_쿨다운내_억제()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            c.OnDamaged(now: 0.2f);   //# < 0.4 쿨다운
            Assert.AreEqual(1, sink.HitCount);
        }

        [Test]
        public void OnDamaged_쿨다운경과_재트리거()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDamaged(now: 0f);
            c.OnDamaged(now: 0.5f);   //# > 0.4 쿨다운
            Assert.AreEqual(2, sink.HitCount);
        }

        [Test]
        public void OnDamaged_공격억제창내_억제()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnAttack(now: 1f);
            c.OnDamaged(now: 1.3f);   //# < 0.5 공격 억제창
            Assert.AreEqual(0, sink.HitCount);
        }

        [Test]
        public void OnDied_Dead세팅_이후Tick은Speed갱신안함()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            Assert.IsTrue(sink.Dead);
            sink.Speed = 99f;
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(99f, sink.Speed);   //# 사망 후 속도 갱신 안 함
        }

        [Test]
        public void OnAttack_사망중_무시()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.OnAttack(now: 1f);
            Assert.AreEqual(0, sink.AttackCount);
        }

        [Test]
        public void Reset_Dead상태_해제()
        {
            FakeSink sink = new FakeSink();
            CharacterAnimationController c = Make(sink);
            c.OnDied();
            c.Reset();
            Assert.IsFalse(sink.Dead);
            c.Tick(isMoving: true, isFleeing: false, walkSpeed: 1f, runSpeed: 2f);
            Assert.AreEqual(1f, sink.Speed);   //# 리셋 후 다시 동작
        }
    }
}
