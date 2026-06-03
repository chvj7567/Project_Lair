using System;

namespace Lair.Character
{
    //# 애니메이션 결정 로직 — Unity 비의존(순수 C#) → EditMode 테스트 대상.
    //# 도메인 상태를 받아 IAnimatorSink 로만 출력. 게임플레이 로직 없음(표현 결정만).
    public class CharacterAnimationController
    {
        private const int AttackVariantCount = 3;   //# slash01 / slash02 / stab

        private readonly IAnimatorSink _sink;
        private readonly float _hitReactionCooldown;
        private readonly float _attackSuppressWindow;
        private readonly Random _rng;

        private bool _dead;
        private float _lastHitTime;
        private float _lastAttackTime;

        //# 영웅 공격 게이트 상태 미러 (hero-animation-timing-sync §3.4). Driver 가 IAttackGate.IsAttacking 을 push.
        //# 기본 false=현행(선행 0.5s 윈도우만 작동). 몬스터는 게이트 없어 항상 false → 동작 불변.
        private bool _isAttacking;

        public CharacterAnimationController(
            IAnimatorSink sink, float hitReactionCooldown, float attackSuppressWindow)
            : this(sink, hitReactionCooldown, attackSuppressWindow, new Random())
        {
        }

        //# seed 주입 오버로드 — 공격 variant 랜덤화의 테스트 결정성 확보(Task 7).
        public CharacterAnimationController(
            IAnimatorSink sink, float hitReactionCooldown, float attackSuppressWindow, Random rng)
        {
            _sink = sink;
            _hitReactionCooldown = hitReactionCooldown;
            _attackSuppressWindow = attackSuppressWindow;
            _rng = rng;
            ResetState();
        }

        //# 풀 재사용/재진입 시 호출 — 상태 초기화 + Dead=false 반영.
        public void Reset()
        {
            ResetState();
            _sink.SetDead(false);
        }

        private void ResetState()
        {
            _dead = false;
            _lastHitTime = float.NegativeInfinity;
            _lastAttackTime = float.NegativeInfinity;
            _isAttacking = false;
        }

        public void OnSpawn() => _sink.TriggerSpawn();

        //# 영웅 게이트 상태 미러 — Driver 가 IAttackGate.IsAttacking 을 push (§3.4). 몬스터는 호출 안 됨(항상 false).
        public void SetAttacking(bool attacking) => _isAttacking = attacking;

        //# IAttacker.OnHit 구독 — 공격 적중 순간 스윙 재생. variant 0~2 랜덤 주입(§1.1 #5).
        public void OnAttack(float now)
        {
            if (_dead)
                return;
            _lastAttackTime = now;
            _sink.TriggerAttack(_rng.Next(0, AttackVariantCount));
        }

        //# Health.OnChanged 감소 감지 시. 스팸 가드 — 공격 중/쿨다운 내면 억제(기획서 §2.1·§2.2).
        //# 영웅 공격 모션 내내(IsAttacking) 추가 억제 — 스윙이 안 끊김(§3.4). Dead 는 여전히 최우선(별도 경로).
        public void OnDamaged(float now)
        {
            if (_dead)
                return;
            if (_isAttacking)
                return;
            if (now - _lastAttackTime < _attackSuppressWindow)
                return;
            if (now - _lastHitTime < _hitReactionCooldown)
                return;
            _lastHitTime = now;
            _sink.TriggerHit();
        }

        public void OnDied()
        {
            _dead = true;
            _sink.SetDead(true);
        }

        //# 매 프레임 — 이동 여부/도주 여부로 Speed 파라미터 결정.
        public void Tick(bool isMoving, bool isFleeing, float walkSpeed, float runSpeed)
        {
            if (_dead)
                return;
            float speed = 0f;
            if (isMoving)
            {
                speed = isFleeing ? runSpeed : walkSpeed;
            }
            _sink.SetSpeed(speed);
        }
    }
}
