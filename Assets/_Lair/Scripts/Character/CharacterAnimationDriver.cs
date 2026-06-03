using UnityEngine;

namespace Lair.Character
{
    //# View 계층(Rule 02 §6) — 도메인 상태를 관찰만 하고 Animator 에 반영.
    //# 영웅/몬스터 공통 재사용 가능하게 인터페이스 의존. 결정 로직은 Controller 에 위임.
    [RequireComponent(typeof(Health))]
    public class CharacterAnimationDriver : MonoBehaviour
    {
        [SerializeField] private Animator _animator;
        [SerializeField] private float _walkSpeed = 1f;
        [SerializeField] private float _runSpeed = 2f;
        [SerializeField] private float _hitReactionCooldown = 0.4f;
        [SerializeField] private float _attackSuppressWindow = 0.5f;

        private IHealth _health;
        private IMover _mover;
        private IAttacker _attacker;
        private AutoCombatAI _ai;
        private CharacterAnimationController _controller;

        private int _lastKnownHp;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            _health = GetComponent<IHealth>();
            _mover = GetComponent<IMover>();
            _attacker = GetComponent<IAttacker>();
            _ai = GetComponent<AutoCombatAI>();
            _controller = new CharacterAnimationController(
                new AnimatorSink(_animator), _hitReactionCooldown, _attackSuppressWindow);
        }

        //# 풀 재사용 — 상태 리셋 + 입장 연출 + 이벤트 구독.
        private void OnEnable()
        {
            _controller.Reset();
            _lastKnownHp = _health != null ? _health.Current : 0;

            if (_health != null)
            {
                _health.OnChanged += HandleHpChanged;
                _health.OnDied += HandleDied;
            }
            if (_attacker != null)
            {
                _attacker.OnHit += HandleAttackHit;
            }

            _controller.OnSpawn();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.OnChanged -= HandleHpChanged;
                _health.OnDied -= HandleDied;
            }
            if (_attacker != null)
            {
                _attacker.OnHit -= HandleAttackHit;
            }
        }

        private void Update()
        {
            bool fleeing = _ai != null && _ai.FleeMode;
            bool moving = _mover != null && _mover.IsMoving;
            _controller.Tick(moving, fleeing, _walkSpeed, _runSpeed);
        }

        private void HandleHpChanged(int current, int max)
        {
            if (current < _lastKnownHp)
            {
                _controller.OnDamaged(Time.time);
            }
            _lastKnownHp = current;
        }

        private void HandleDied() => _controller.OnDied();

        private void HandleAttackHit(IHealth target) => _controller.OnAttack(Time.time);
    }
}
