using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 습격-이탈 상태 (chapter-3-hero §3.3). 추적 → 교전 → 슬립.
    public enum ESkirmishState
    {
        Chase,   //# 표적이 사거리 밖 — 접근
        Engage,  //# 사거리 안 — 정지 + 기본 공격
        Slip,    //# 군중/교전 시간 초과 이탈 — 이동 소유권은 AutoCombatAI 의 도약 보간
    }

    //# HeroArchetype_Skirmish row → 불변 설정 (HeroArchetype_Skirmish.json).
    public readonly struct SkirmishSettings
    {
        public readonly float HuntRadius;
        public readonly float RetargetScoreMul;
        public readonly float StrikeWindowSec;
        public readonly int CrowdCount;
        public readonly float CrowdRadius;
        public readonly float SlipDistance;
        public readonly float SlipDurationSec;
        public readonly float SlipInvulnSec;
        public readonly float SlipCooldownSec;
        public readonly float MinDwellSec;
        public readonly float ArenaSoftRadius;

        public SkirmishSettings(float huntRadius, float retargetScoreMul, float strikeWindowSec, int crowdCount, float crowdRadius,
            float slipDistance, float slipDurationSec, float slipInvulnSec, float slipCooldownSec, float minDwellSec, float arenaSoftRadius)
        {
            HuntRadius = huntRadius;
            RetargetScoreMul = retargetScoreMul;
            StrikeWindowSec = strikeWindowSec;
            CrowdCount = crowdCount;
            CrowdRadius = crowdRadius;
            SlipDistance = slipDistance;
            SlipDurationSec = slipDurationSec;
            SlipInvulnSec = slipInvulnSec;
            SlipCooldownSec = slipCooldownSec;
            MinDwellSec = minDwellSec;
            ArenaSoftRadius = arenaSoftRadius;
        }

        public static SkirmishSettings From(HeroArchetype_SkirmishRow row)
        {
            return new SkirmishSettings(row.huntRadius, row.retargetScoreMul, row.strikeWindowSec, row.crowdCount, row.crowdRadius,
                row.slipDistance, row.slipDurationSec, row.slipInvulnSec, row.slipCooldownSec, row.minDwellSec, row.arenaSoftRadius);
        }
    }

    //# 표적 후보 1체 — 월드 조회 결과를 순수 로직에 넘기는 값 묶음.
    public struct SkirmishCandidate
    {
        public Transform Transform;
        public IHealth Health;
        public Vector3 Position;
        public float Score;   //# threatScore = 몬스터 DPS ÷ max(1, 현재 HP)
    }

    //# 한 프레임 입력 — AutoCombatAI 가 월드에서 수집해 넘긴다(순수 로직이 월드를 모르게).
    public struct SkirmishInput
    {
        public Vector3 Position;
        public bool HasTarget;
        public bool TargetChanged;     //# 직전 프레임과 다른 표적 — 교전 시간 누적을 새로 센다
        public float TargetDist;
        public float Range;
        public float EngageBuffer;
        public int CrowdCount;         //# crowdRadius 안 살아있는 몬스터 수
        public bool HasCentroid;       //# CentroidRadius 안 몬스터 무게중심 유효
        public Vector3 Centroid;
    }

    //# 습격-이탈 상태기계 + 표적 점수/선정 (Unity 월드 비의존 — 값만). Skirmisher 영웅에만 생성된다(기사·궁수 경로 불변).
    public class SkirmishBehavior
    {
        //# 슬립 방향용 군중 무게중심 집계 반경 (§3.3 상태표 — 명세 고정값).
        public const float CentroidRadius = 3f;
        //# 프레임 간격이 이보다 크면 AI 가 길게 멈춘 구간(TimeStop 등)으로 보고 교전 시간에 더하지 않는다. 공격 모션(0.6초) 동안의 정지는 교전에 포함한다.
        public const float ObservationGapSec = 1f;

        private readonly SkirmishSettings _settings;
        private float _stateEnterTime;
        private float _lastSlipTime;
        private float _lastUpdateTime;
        private float _engageSec;
        private bool _forceSlip;

        public SkirmishBehavior(SkirmishSettings settings)
        {
            _settings = settings;
            Reset();
        }

        public SkirmishSettings Settings => _settings;
        public ESkirmishState State { get; private set; }

        //# 기본 공격 허용 — 교전 중만. 추적·슬립 중엔 억제.
        public bool CanAttack => State == ESkirmishState.Engage;

        //# 풀 재사용 — 상태 잔존 방지.
        public void Reset()
        {
            State = ESkirmishState.Chase;
            _stateEnterTime = float.NegativeInfinity;
            _lastSlipTime = float.NegativeInfinity;
            _lastUpdateTime = float.NegativeInfinity;
            _engageSec = 0f;
            _forceSlip = false;
        }

        //# 스킬(연막 탄)이 시전 직후 슬립을 쿨다운·최소 유지 무시로 1회 요청.
        public void RequestForcedSlip()
        {
            _forceSlip = true;
        }

        //# 한 프레임 갱신. 이번 프레임에 슬립을 시작했으면 true(호출부가 도약·무적·FX 를 시작한다).
        public bool Update(float now, SkirmishInput input)
        {
            float dt = _lastUpdateTime == float.NegativeInfinity ? 0f : now - _lastUpdateTime;
            _lastUpdateTime = now;
            bool resumedFromGap = dt > ObservationGapSec;

            if (State == ESkirmishState.Slip)
            {
                //# AI 는 도약 중 Update 를 부르지 않으므로 여기 도달 = 슬립 이동 종료.
                EnterState(ESkirmishState.Chase, now);
                _engageSec = 0f;
                return false;
            }

            if (input.TargetChanged)
            {
                _engageSec = 0f;
            }
            else if (State == ESkirmishState.Engage && resumedFromGap == false)
            {
                _engageSec += dt;
            }

            if (ShouldSlip(now, input))
            {
                _forceSlip = false;
                _lastSlipTime = now;
                _engageSec = 0f;
                EnterState(ESkirmishState.Slip, now);
                return true;
            }

            if (input.HasTarget == false)
            {
                State = ESkirmishState.Chase;
                return false;
            }

            //# 추적↔교전 전이는 사거리 히스테리시스만 본다. 다만 EnterState 가 상태 진입 시각을 갱신하므로 교전 진입 직후 minDwellSec 동안은 슬립이 지연된다(의도).
            if (State == ESkirmishState.Engage)
            {
                if (input.TargetDist > input.Range + input.EngageBuffer)
                {
                    EnterState(ESkirmishState.Chase, now);
                }
            }
            else if (input.TargetDist <= input.Range)
            {
                EnterState(ESkirmishState.Engage, now);
            }
            return false;
        }

        private bool ShouldSlip(float now, SkirmishInput input)
        {
            if (_forceSlip)
                return true;
            bool crowd = input.CrowdCount >= _settings.CrowdCount;
            bool overstay = State == ESkirmishState.Engage && _engageSec >= _settings.StrikeWindowSec;
            if (crowd == false && overstay == false)
                return false;
            return now - _lastSlipTime >= _settings.SlipCooldownSec && now - _stateEnterTime >= _settings.MinDwellSec;
        }

        private void EnterState(ESkirmishState next, float now)
        {
            if (State == next)
                return;
            State = next;
            _stateEnterTime = now;
        }

        //# 슬립 목적지 — 군중 무게중심 반대 방향 slipDistance, 전장 클램프(아레나 소프트 반경 + 이동 경계).
        //# 클램프 뒤 실이동이 부족하면(가장자리) 원점 쪽을 섞어 재계산 — 궁수 도약과 같은 공식. 중심이 없으면 원점 쪽.
        public static Vector3 ComputeSlipDestination(Vector3 position, bool hasCentroid, Vector3 centroid, float distance, System.Func<Vector3, Vector3> clamp)
        {
            Vector3 threat = hasCentroid ? centroid : position;
            return EvadeRuntime.ComputeLeapDestination(position, threat, distance, clamp);
        }

        //# 위협 점수 = 몬스터 DPS ÷ max(1, 현재 HP). DPS = 공격력 x PowerScale ÷ 실효 쿨다운(버프 반영). 공격자 없으면 0.
        public static float ThreatScore(IAttacker attacker, IHealth health)
        {
            if (attacker == null || health == null)
                return 0f;
            float cooldown = attacker is IEffectiveCooldown effective ? effective.EffectiveCooldown : attacker.Cooldown;
            float dps = attacker.Power * attacker.PowerScale / Mathf.Max(0.05f, cooldown);
            return dps / Mathf.Max(1, health.Current);
        }

        //# 표적 선정 — 점수 최대(동점이면 가까운 쪽). current 가 후보에 있고 최고점이 current 점수의 retargetScoreMul 배를 넘지 않으면 유지.
        //# 후보가 없으면 -1.
        public static int SelectTarget(IReadOnlyList<SkirmishCandidate> candidates, Vector3 heroPosition, Transform current, float retargetScoreMul)
        {
            if (candidates == null || candidates.Count == 0)
                return -1;

            int best = 0;
            int currentIndex = -1;
            for (int i = 0; i < candidates.Count; ++i)
            {
                if (current != null && candidates[i].Transform == current)
                {
                    currentIndex = i;
                }
                if (i == 0)
                    continue;
                if (IsBetter(candidates[i], candidates[best], heroPosition))
                {
                    best = i;
                }
            }

            if (currentIndex < 0 || currentIndex == best)
                return best;
            return candidates[best].Score > candidates[currentIndex].Score * retargetScoreMul ? best : currentIndex;
        }

        private static bool IsBetter(SkirmishCandidate a, SkirmishCandidate b, Vector3 heroPosition)
        {
            if (Mathf.Approximately(a.Score, b.Score) == false)
                return a.Score > b.Score;
            return (a.Position - heroPosition).sqrMagnitude < (b.Position - heroPosition).sqrMagnitude;
        }
    }
}
