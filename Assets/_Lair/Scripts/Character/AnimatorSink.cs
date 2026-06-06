using ChvjUnityInfra;
using Lair.Data;
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

        //# 스윙음 재생 여부 — 영웅(Knight)만 true. 몬스터는 false(스윙음 무음). Driver 가 _attackGate != null 로 주입.
        private readonly bool _playSwingSound;

        public AnimatorSink(Animator animator) : this(animator, false)
        {
        }

        public AnimatorSink(Animator animator, bool playSwingSound)
        {
            _animator = animator;
            _playSwingSound = playSwingSound;
        }

        public void SetSpeed(float speed) => _animator.SetFloat(SpeedId, speed);

        public void TriggerAttack(int variant)
        {
            _animator.SetInteger(VariantId, variant);
            _animator.SetTrigger(AttackId);
            //# 영웅 한정 스윙음 — variant 0=Slash01 / 1=Slash02 / 2=Stab. 몬스터(_playSwingSound=false)는 무음.
            if (_playSwingSound)
                CHMSound.Instance?.Play(VariantToSwingAudio(variant));
        }

        private static EAudio VariantToSwingAudio(int variant)
        {
            if (variant == 0)
                return EAudio.Slash01;
            if (variant == 1)
                return EAudio.Slash02;
            return EAudio.Stab;
        }

        public void TriggerHit() => _animator.SetTrigger(HitId);
        public void SetDead(bool dead) => _animator.SetBool(DeadId, dead);
        public void TriggerSpawn() => _animator.SetTrigger(SpawnId);
    }
}
