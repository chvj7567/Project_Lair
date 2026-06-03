using System;
using UnityEngine;

namespace Lair.Character
{
    //# IAttackGate 구현 — 영웅 공격 모션 재생 중 상태(IsAttacking) 소유 + 개시 신호 중계.
    //# 루트(Knight)에 부착(영웅 프리팹만). hero-animation-timing-sync §3.2·§2.7·§3.4.
    //# 게임플레이 판정 안 함 — AutoCombatAI/MeleeAttacker 가 통과시킨 개시 결과만 받아 애니 신호(OnAttackBegin) 발행.
    public class HeroAttackGate : MonoBehaviour, IAttackGate
    {
        //# 공격 end 누락 fallback (§3.4·§7.6). stab 1.63s + 0.17 마진. _attackSuppressWindow(0.5) 와 별도 — 겸용 금지(M3).
        //# OnAttackEnd 이벤트 유실 시 IsAttacking 영구 true 굳음 방지.
        [SerializeField] private float _attackEndFallback = 1.8f;

        private bool _isAttacking;
        private float _beginTime;

        public bool IsAttacking => _isAttacking;

        //# 개시 신호 — CharacterAnimationDriver(View)가 구독해 TriggerAttack 출력(§2.7 ②).
        public event Action OnAttackBegin;

        //# 풀 재사용 + 사망→재스폰 락 방지 — 활성화마다 공격 상태 해제.
        private void OnEnable()
        {
            _isAttacking = false;
        }

        //# TryBeginAttack 성공 직후 AutoCombatAI 가 호출 — IsAttacking=true + 애니 트리거 개시 신호.
        public void BeginAttack()
        {
            _isAttacking = true;
            _beginTime = Time.time;
            OnAttackBegin?.Invoke();
        }

        //# OnAttackEnd relay(§B4) 가 호출 — 다음 공격 개시 판정 재개.
        public void EndAttack()
        {
            _isAttacking = false;
        }

        //# end 이벤트 유실 안전망 — fallback 초과 시 IsAttacking 강제 해제.
        private void Update()
        {
            if (_isAttacking == false) return;
            if (Time.time - _beginTime >= _attackEndFallback)
            {
                _isAttacking = false;
            }
        }
    }
}
