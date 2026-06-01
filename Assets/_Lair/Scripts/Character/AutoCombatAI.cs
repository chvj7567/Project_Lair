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

        //# B3 — 공포 카드. true 면 주변 위협 무리의 반대 방향으로 도주, 공격 안 함.
        public bool FleeMode { get; set; }

        //# 도주 시 위협 centroid 를 모으는 반경. 포위 상황에서 진동(갇힘) 방지.
        [SerializeField] private float _fleeThreatRadius = 4f;

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
            if (_targetProvider.TryFindNearest(transform.position, out Transform t, out IHealth th) == false)
            {
                _mover.Stop();
                return;
            }

            //# B3 공포 (Fleeing) — 주변 위협 centroid 의 반대 방향으로 도주, 공격 X.
            //# 포위 시 최근접 1마리만 보면 매 프레임 방향 뒤집혀 제자리 진동 → centroid 로 안정화.
            if (FleeMode)
            {
                Vector3 fleeDir = transform.position - t.position;
                if (_targetProvider.TryGetThreatCentroid(
                        transform.position, _fleeThreatRadius, out Vector3 centroid, out int count)
                    && count > 0)
                {
                    Vector3 fromCentroid = transform.position - centroid;
                    //# centroid 가 자기 위치와 거의 일치(대칭 포위)면 0벡터 → 최근접 fallback 유지.
                    if (fromCentroid.sqrMagnitude > 0.0001f)
                    {
                        fleeDir = fromCentroid;
                    }
                }
                Vector3 away = transform.position + fleeDir.normalized * 5f;
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
