using UnityEngine;

namespace Lair.Character
{
    //# 자동전투 행동 — 인터페이스 5개 조합으로만 동작.
    //# 영웅/몬스터 공통. ITargetProvider 구현체로 진영이 결정됨.
    [RequireComponent(typeof(SimpleMover))]
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(MeleeAttacker))]
    [RequireComponent(typeof(SimpleRotator))]
    public class AutoCombatAI : MonoBehaviour
    {
        private IMover _mover;
        private IHealth _health;
        private IAttacker _attacker;
        private ITargetProvider _targetProvider;
        private IRotator _rotator;

        //# B3 — 공포 카드. true 면 가장 가까운 적의 반대 방향으로 도주, 공격 안 함.
        public bool FleeMode { get; set; }

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _health = GetComponent<IHealth>();
            _attacker = GetComponent<IAttacker>();
            _targetProvider = GetComponent<ITargetProvider>();
            _rotator = GetComponent<IRotator>();
        }

        //# 풀 재사용 시 도주 상태 잔존 방지 + 초기 방향 스냅.
        //# Vector3.zero 가 ring 중심 → 스폰 직후 몬스터는 영웅(중심)을 바라보고 출발.
        //# 자기 위치가 zero 와 거의 같으면(영웅) SnapToDirection 은 magnitude 가드로 no-op.
        private void OnEnable()
        {
            FleeMode = false;
            _rotator?.SnapToDirection(Vector3.zero - transform.position);
        }

        private void Update()
        {
            //# Dead — 마지막 yaw 유지 (회전 명령 없음).
            if (_health == null || _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }
            if (_targetProvider == null) return;

            //# Idle (타겟 없음) — 마지막 yaw 유지.
            if (_targetProvider.TryFindNearest(transform.position, out var t, out var th) == false)
            {
                _mover.Stop();
                return;
            }

            //# B3 공포 (Fleeing) — 가장 가까운 적의 반대 방향으로 이동, 공격 X.
            //# 회전은 이동 목표(away) 방향 = 자연스럽게 적의 반대.
            if (FleeMode)
            {
                Vector3 away = transform.position
                    + (transform.position - t.position).normalized * 5f;
                _rotator?.FaceDirection(away - transform.position);
                _mover.MoveTo(away);
                return;
            }

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist <= _attacker.Range)
            {
                //# Attacking — 타겟 방향을 정확히 바라봄.
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.Stop();
                _attacker.TryAttack(th, transform.position, t.position, Time.time);
            }
            else
            {
                //# Moving — 이동 목표(=타겟) 방향.
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.MoveTo(t.position);
            }
        }
    }
}
