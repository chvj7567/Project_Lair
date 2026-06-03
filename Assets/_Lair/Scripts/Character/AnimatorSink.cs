using UnityEngine;

namespace Lair.Character
{
    //# IAnimatorSink 의 런타임 구현 — UnityEngine.Animator 파라미터로 위임.
    //# 파라미터명은 Knight.controller 계약과 일치해야 함(Speed/Attack/AttackVariant/Hit/Dead/Spawn).
    public class AnimatorSink : IAnimatorSink
    {
        private static readonly int SpeedId = Animator.StringToHash("Speed");
        private static readonly int AttackId = Animator.StringToHash("Attack");
        private static readonly int VariantId = Animator.StringToHash("AttackVariant");
        private static readonly int HitId = Animator.StringToHash("Hit");
        private static readonly int DeadId = Animator.StringToHash("Dead");
        private static readonly int SpawnId = Animator.StringToHash("Spawn");

        private readonly Animator _animator;

        public AnimatorSink(Animator animator) => _animator = animator;

        public void SetSpeed(float speed) => _animator.SetFloat(SpeedId, speed);

        public void TriggerAttack(int variant)
        {
            _animator.SetInteger(VariantId, variant);
            _animator.SetTrigger(AttackId);
        }

        public void TriggerHit() => _animator.SetTrigger(HitId);
        public void SetDead(bool dead) => _animator.SetBool(DeadId, dead);
        public void TriggerSpawn() => _animator.SetTrigger(SpawnId);
    }
}
