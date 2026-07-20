using UnityEngine;
using Lair.Data;

namespace Lair.Character
{
    //# 자동전투 행동 — 인터페이스 5개 조합으로만 동작.
    //# 영웅/몬스터 공통. ITargetProvider 구현체로 진영이 결정됨.
    //# AI 결정(MoveTo/Stop)을 같은 프레임의 애니 Tick(Driver)보다 먼저 — IsMoving 1프레임 지연 슬라이드 제거.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(LairCharacter))]
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

        //# A-2 도주 방향 시간감쇠 rate. 1-exp(-rate·dt) 로 프레임 독립 수렴. τ=1/rate (기획서 §2.2: 5.0=τ0.2s).
        [SerializeField] private float _fleeTurnSmoothing = 5f;
        //# 감쇠된 도주 단위방향(지속). FaceDirection/MoveTo 에 사용. OnEnable·FleeMode 진입 시 리셋/스냅.
        private Vector3 _fleeDirSmoothed;
        //# 이전 프레임 FleeMode — false→true 전이에서 _fleeDirSmoothed 를 raw 로 스냅(stale lerp 방지).
        private bool _wasFleeing;

        //# B 영웅 중앙 끌림 — 영웅 프리팹만 true(몬스터 기본 false → 동작 불변). 명시 플래그로 영웅 식별.
        [SerializeField] private bool _centerPullEnabled;
        //# 사이클 주기(s) / 그 중 중앙이동 창 길이(s) / 중앙거리 deadzone(units). 기획서 §3.1·§3.2.
        [SerializeField] private float _centerPullInterval = 3f;
        [SerializeField] private float _centerPullDuration = 1f;
        [SerializeField] private float _centerPullDeadzone = 3f;
        //# [0,interval) 사이클 위상 타이머. [0,duration) 구간이 중앙이동 창. OnEnable 리셋.
        private float _centerPullTimer;

        //# 교전 히스테리시스 — 사거리 경계 Move/Stop 매 프레임 토글(동기 stop-go) 방지.
        //# dist<=Range 면 교전 진입, 교전 중엔 dist>Range+버퍼 여야 해제 (6종·영웅 공통 절대값).
        [SerializeField] private float _engageBuffer = 0.5f;
        private bool _engaged;

        //# 영웅 한정 타겟 고정 — 영웅 프리팹만 true(몬스터 기본 false → 매 프레임 최근접 유지, 동작 불변).
        [SerializeField] private bool _targetHysteresisEnabled;
        //# 새 후보가 현재 타겟보다 이 거리(units) 이상 가까울 때만 교체. 등거리 깜빡임 흡수 밴드(기획서 §3).
        [SerializeField] private float _retargetMargin = 1.5f;
        //# 현재 고정된 타겟 + HP 참조. OnEnable 리셋. 사망/소멸/멀어짐 시 교체.
        private Transform _currentTarget;
        private IHealth _currentTargetHealth;

        private void Awake()
        {
            LairCharacter character = GetComponent<LairCharacter>();
            _mover = character.Get<IMover>();
            _health = character.Get<IHealth>();
            _attacker = character.Get<IAttacker>();
            _targetProvider = character.Get<ITargetProvider>();
            _rotator = character.Get<IRotator>();
            _attackGate = character.Get<IAttackGate>();   //# null=몬스터(보류 로직 미적용)
        }

        //# 풀 재사용 시 도주 상태 잔존 방지 + 초기 방향 스냅.
        //# Vector3.zero 가 ring 중심 → 스폰 직후 몬스터는 영웅(중심)을 바라보고 출발.
        private void OnEnable()
        {
            FleeMode = false;
            _engaged = false;   //# 풀 재사용 + enabled=true 전환 시 교전 상태 잔존 방지
            //# A-3 풀 재사용 시 도주 감쇠 상태 잔존 방지 — 다음 진입에서 raw 로 재스냅.
            _fleeDirSmoothed = Vector3.zero;
            _wasFleeing = false;
            //# B 풀 재사용 시 중앙 끌림 사이클 위상 잔존 방지.
            _centerPullTimer = 0f;
            //# 풀 재사용 시 이전 고정 타겟 잔존 방지(영웅 한정 히스테리시스).
            _currentTarget = null;
            _currentTargetHealth = null;
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

            //# A-3 비도주 프레임마다 스냅 플래그 해제 — 아래 가드(TimeStop/게이트/no-target)에 막혀도
            //# 다음 FleeMode 재진입이 stale 방향 lerp 대신 raw 스냅하도록 보장.
            if (FleeMode == false)
            {
                _wasFleeing = false;
            }

            //# IAttacker.Enabled=false (TimeStop 등) — 완전 정지: 교전/이동/회전 모두 보류.
            //# AutoCombatAI 는 공격 시도(TryBeginAttack/TryAttack)의 단일 진입점 — 여기서 막아야 공격이 실제로 멈춘다.
            if (_attacker != null && _attacker.Enabled == false)
            {
                _mover.Stop();
                return;
            }

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
            if (_targetProvider.TryFindNearest(transform.position, out Transform candidate, out IHealth candidateH) == false)
            {
                _currentTarget = null;
                _currentTargetHealth = null;
                _mover.Stop();
                return;
            }

            //# 영웅(히스테리시스 ON): 현재 타겟 고정 후 margin 초과 시만 교체. 몬스터(OFF): 매 프레임 최근접(불변).
            Transform t;
            IHealth th;
            if (_targetHysteresisEnabled)
            {
                t = ResolveStickyTarget(candidate, candidateH, out th);
            }
            else
            {
                t = candidate;
                th = candidateH;
            }

            //# B3 공포 (Fleeing) — 주변 위협 centroid 의 반대 방향으로 도주, 공격 X.
            //# A-1 centroid 는 Engaging 무관(반경 내 전체)로 집계해 교전 토글 진동원 제거.
            //# A-2 raw 단위방향을 _fleeDirSmoothed 로 시간감쇠 → 프레임당 뒤집힘을 8%만 반영(기획서 §2.2).
            if (FleeMode)
            {
                Vector3 fleeDir = transform.position - t.position;
                if (_targetProvider.TryGetFleeCentroid(
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
                //# 단위방향만 감쇠(magnitude 가 turn rate 에 새지 않게). 0벡터면 직전 smoothed 유지.
                Vector3 rawDir = fleeDir.sqrMagnitude > 0.0001f ? fleeDir.normalized : _fleeDirSmoothed;
                //# A-3 FleeMode 진입 순간(false→true) raw 로 스냅 — 이전 stale 값에서 lerp 시작 방지.
                if (_wasFleeing == false)
                {
                    _fleeDirSmoothed = rawDir;
                }
                else
                {
                    float k = 1f - Mathf.Exp(-_fleeTurnSmoothing * Time.deltaTime);
                    _fleeDirSmoothed += (rawDir - _fleeDirSmoothed) * k;
                }
                _wasFleeing = true;

                Vector3 dir = _fleeDirSmoothed.sqrMagnitude > 0.0001f ? _fleeDirSmoothed.normalized : rawDir;
                Vector3 away = transform.position + dir * 5f;
                //# 도주 — 방향이 이미 _fleeDirSmoothed 로 평활화됨 + A-3 재진입 스냅 계약 → 즉시 스냅 유지(현행).
                _rotator?.FaceDirection(dir);
                _mover.MoveTo(away);
                return;
            }

            //# B 영웅 중앙 끌림 — 사망/TimeStop/스폰·공격 게이트 통과 + 비도주 상태에서만.
            //# 3초 주기 중 첫 1초 창은 중앙(원점 XZ)으로 이동 우선·공격 보류. deadzone 안이면 창 미발동(이동만 생략).
            if (_centerPullEnabled)
            {
                _centerPullTimer += Time.deltaTime;
                if (_centerPullTimer >= _centerPullInterval)
                {
                    _centerPullTimer -= _centerPullInterval;
                }

                //# 중앙까지 XZ 거리 — 원점은 y=0 이고 런타임 위치도 y=0(SimpleMover)이라 일치하나 명시적으로 평면 계산.
                Vector3 toCenter = Vector3.zero - transform.position;
                toCenter.y = 0f;
                bool inWindow = _centerPullTimer < _centerPullDuration;
                if (inWindow && toCenter.magnitude > _centerPullDeadzone)
                {
                    //# 중앙끌림 — 비전투 재정렬. Smooth 로 180° 홱 돌기 제거(영웅).
                    _rotator?.FaceDirection(toCenter, FacingMode.Smooth);
                    //# MoveTo(원점) — MoveTowards 가 중앙에서 감속·정지(과주행 없음, 기획서 §3.2).
                    _mover.MoveTo(Vector3.zero);
                    return;
                }
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
                //# Attacking — 정지 + 타겟 향해 회전. 교전은 AttackAligned — 영웅만 즉시 스냅(공격 정렬), 몬스터는 보간.
                _rotator?.FaceDirection(t.position - transform.position, FacingMode.AttackAligned);
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
                //# Moving — 이동 목표(=타겟) 방향. 즉시 스냅(AttackAligned 위임) — 히스테리시스가 등거리 깜빡임 흡수(기획서 §2).
                _rotator?.FaceDirection(t.position - transform.position);
                _mover.MoveTo(t.position);
            }
        }

        //# 현재 타겟을 유지하되 (1)사망/소멸 또는 (2)새 후보가 margin 이상 가까울 때만 교체.
        //# 등거리 후보 깜빡임을 흡수해 영웅이 휙휙 도는 현상을 막는다(영웅 한정).
        private Transform ResolveStickyTarget(Transform candidate, IHealth candidateH, out IHealth resolvedHealth)
        {
            bool currentInvalid =
                _currentTarget == null ||
                _currentTargetHealth == null ||
                _currentTargetHealth.IsAlive == false ||
                _currentTarget.gameObject.activeInHierarchy == false;

            if (currentInvalid)
            {
                _currentTarget = candidate;
                _currentTargetHealth = candidateH;
            }
            else if (candidate != _currentTarget)
            {
                float curDist = Vector3.Distance(transform.position, _currentTarget.position);
                float candDist = Vector3.Distance(transform.position, candidate.position);
                if (curDist - candDist > _retargetMargin)
                {
                    _currentTarget = candidate;
                    _currentTargetHealth = candidateH;
                }
            }

            resolvedHealth = _currentTargetHealth;
            return _currentTarget;
        }
    }
}
