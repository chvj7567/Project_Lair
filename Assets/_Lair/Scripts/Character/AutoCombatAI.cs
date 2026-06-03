using UnityEngine;

namespace Lair.Character
{
    //# 자동전투 행동 — 인터페이스 5개 조합으로만 동작.
    //# 영웅/몬스터 공통. ITargetProvider 구현체로 진영이 결정됨.
    //# AI 결정(MoveTo/Stop)을 같은 프레임의 애니 Tick(Driver)보다 먼저 — IsMoving 1프레임 지연 슬라이드 제거.
    [DefaultExecutionOrder(-10)]
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

        //# 영웅 공격 게이트 (hero-animation-timing-sync §3.2). 영웅 프리팹만 부착 → 몬스터는 null(보류 미적용).
        private IAttackGate _attackGate;

        //# 스폰 게이트 (§1.2). 영웅 한정 — spawn 모션 재생 중 교전/이동 보류. OnSpawnAnimEnd relay 또는 fallback 으로 open.
        //# 몬스터(DeferStrike=false)는 무시. 풀 재사용 대비 OnEnable 리셋.
        [SerializeField] private float _spawnGateFallback = 1.8f;
        private bool _spawnGateOpen;
        private float _enabledTime;

        //# B3 — 공포 카드. true 면 주변 위협 무리의 반대 방향으로 도주, 공격 안 함.
        public bool FleeMode { get; set; }

        //# 도주 시 위협 centroid 를 모으는 반경. 포위 상황에서 진동(갇힘) 방지.
        [SerializeField] private float _fleeThreatRadius = 4f;

        //# 교전 히스테리시스 — 사거리 경계 Move/Stop 매 프레임 토글(동기 stop-go) 방지.
        //# dist<=Range 면 교전 진입, 교전 중엔 dist>Range+버퍼 여야 해제 (6종·영웅 공통 절대값).
        [SerializeField] private float _engageBuffer = 0.5f;
        private bool _engaged;

        private void Awake()
        {
            _mover = GetComponent<IMover>();
            _health = GetComponent<IHealth>();
            _attacker = GetComponent<IAttacker>();
            _targetProvider = GetComponent<ITargetProvider>();
            _rotator = GetComponent<IRotator>();
            _attackGate = GetComponent<IAttackGate>();   //# null=몬스터(보류 로직 미적용)
        }

        //# 풀 재사용 시 도주 상태 잔존 방지 + 초기 방향 스냅.
        //# Vector3.zero 가 ring 중심 → 스폰 직후 몬스터는 영웅(중심)을 바라보고 출발.
        private void OnEnable()
        {
            FleeMode = false;
            _engaged = false;   //# 풀 재사용 + enabled=true 전환 시 교전 상태 잔존 방지
            //# 스폰 게이트 — 영웅(DeferStrike)만 닫고 시작. 몬스터는 게이트 검사 자체를 건너뛰므로 무관.
            _spawnGateOpen = false;
            _enabledTime = Time.time;
            _rotator?.SnapToDirection(Vector3.zero - transform.position);
        }

        //# OnSpawnAnimEnd relay(§B4) 가 호출 — 영웅 spawn 게이트 open. 몬스터는 호출되지 않음.
        public void OpenSpawnGate() => _spawnGateOpen = true;

        //# 영웅 spawn 게이트가 열렸는가 — 닫힘이면 교전/이동 보류. fallback 초과 시 강제 open(이벤트 유실 안전망).
        //# 몬스터(DeferStrike=false)는 항상 true 취급(게이트 무시).
        private bool IsSpawnGatePassed()
        {
            if (_attacker == null || _attacker.DeferStrike == false) return true;
            if (_spawnGateOpen) return true;
            if (Time.time - _enabledTime >= _spawnGateFallback)
            {
                _spawnGateOpen = true;
                return true;
            }
            return false;
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

            //# 영웅 스폰 게이트 — spawn 모션 재생 중엔 교전/이동/도주 모두 보류(§1.2). 몬스터는 즉시 통과.
            if (IsSpawnGatePassed() == false)
            {
                _mover.Stop();
                return;
            }

            //# 영웅 공격 중(IsAttacking) — windup~recovery 동안 다음 공격 보류 + 이동 정지(§3.2). 몬스터는 게이트 null.
            if (_attackGate != null && _attackGate.IsAttacking)
            {
                _mover.Stop();
                return;
            }

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
            float range = _attacker.Range;

            //# 히스테리시스 — 미교전: 사거리 닿으면 진입 / 교전: 버퍼 밖으로 벗어나야 해제.
            if (_engaged)
            {
                if (dist > range + _engageBuffer)
                {
                    _engaged = false;
                }
            }
            else
            {
                if (dist <= range)
                {
                    _engaged = true;
                }
            }

            if (_engaged)
            {
                //# Attacking — 정지 + 타겟 향해 회전.
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.Stop();

                //# DeferStrike 분기 (§B.2) — 몬스터: 즉시 데미지(현행). 영웅: 개시 판정 → 게이트가 strike 까지 데미지 지연.
                if (_attacker.DeferStrike == false)
                {
                    _attacker.TryAttack(th, transform.position, t.position, Time.time);
                }
                else if (_attacker.TryBeginAttack(th, transform.position, t.position, Time.time))
                {
                    //# 개시 성공 — 게이트가 IsAttacking=true + 공격 애니 트리거 개시 신호 발행(§2.7 ②).
                    _attackGate?.BeginAttack();
                }
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
