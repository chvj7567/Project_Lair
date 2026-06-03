using UnityEngine;

namespace Lair.Character
{
    //# View 계층(Rule 02 §6) — 도메인 상태를 관찰만 하고 Animator 에 반영.
    //# 영웅/몬스터 공통 재사용 가능하게 인터페이스 의존. 결정 로직은 Controller 에 위임.
    //# AutoCombatAI(-10) 이후 실행 — 같은 프레임의 MoveTo 결과(IsMoving)를 즉시 애니에 반영.
    [DefaultExecutionOrder(10)]
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

        //# 영웅 공격 게이트 (hero-animation-timing-sync §2.7). 영웅만 부착 → null=몬스터(현행 OnHit→애니 경로).
        private IAttackGate _attackGate;

        private int _lastKnownHp;

        private void Awake()
        {
            if (_animator == null)
                _animator = GetComponentInChildren<Animator>();
            _health = GetComponent<IHealth>();
            _mover = GetComponent<IMover>();
            _attacker = GetComponent<IAttacker>();
            _ai = GetComponent<AutoCombatAI>();
            _attackGate = GetComponent<IAttackGate>();
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
            //# 영웅(§2.7) — 공격 애니 트리거는 OnHit(strike 후행) 이 아니라 개시 신호로. strike→windup→strike 순환 차단.
            if (_attackGate != null)
            {
                _attackGate.OnAttackBegin += HandleAttackBegin;
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
            if (_attackGate != null)
            {
                _attackGate.OnAttackBegin -= HandleAttackBegin;
            }
        }

        private void Update()
        {
            bool fleeing = _ai != null && _ai.FleeMode;
            bool moving = _mover != null && _mover.IsMoving;
            //# 영웅 공격 모션 중 피격 리액션 억제를 controller 에 push(§3.4). 몬스터는 게이트 null → false.
            if (_attackGate != null)
                _controller.SetAttacking(_attackGate.IsAttacking);
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

        //# OnHit 구독 — 영웅(게이트 존재)은 애니 트리거 우회(개시 경로가 담당, §2.7). OnHit 은 연출(AttackJuice/Plague) 전용.
        //# 몬스터(게이트 null)는 현행 OnHit→OnAttack 애니 트리거 경로 보존.
        private void HandleAttackHit(IHealth target)
        {
            if (_attackGate != null)
                return;
            _controller.OnAttack(Time.time);
        }

        //# 영웅 개시 신호(§2.7 ②) — IAttackGate.BeginAttack 이 발행. Driver(View)가 받아 TriggerAttack 출력만.
        private void HandleAttackBegin() => _controller.OnAttack(Time.time);
    }
}
