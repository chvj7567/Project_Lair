using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 카이팅 상태 (chapter-system §5.3). 접근 → 교전 → 후퇴 → 갇힘.
    public enum EKiteState
    {
        Approach,   //# 타깃이 사거리 밖 — 접근
        Engage,     //# 사거리 안, 위협 거리 밖 — 정지 + 사격
        Retreat,    //# 가장 가까운 몬스터가 triggerDist 안 — 위협 반대로 후퇴, 사격 억제
        Cornered,   //# 후퇴 중 막힘 — 후퇴 중단, 가장 가까운 몬스터에게 사격 허용
    }

    //# HeroArchetype_Kite row → 불변 설정 (HeroArchetype_Kite.json, chapter-system §1.2).
    public readonly struct KiteSettings
    {
        public readonly float TriggerDist;
        public readonly float ResumeDist;
        public readonly float ThreatRadius;
        public readonly float ArenaSoftRadius;
        public readonly float CorneredCheckSec;
        public readonly float CorneredMinMove;
        public readonly float MinDwellSec;

        public KiteSettings(float triggerDist, float resumeDist, float threatRadius, float arenaSoftRadius,
            float corneredCheckSec, float corneredMinMove, float minDwellSec)
        {
            TriggerDist = triggerDist;
            ResumeDist = resumeDist;
            ThreatRadius = threatRadius;
            ArenaSoftRadius = arenaSoftRadius;
            CorneredCheckSec = corneredCheckSec;
            CorneredMinMove = corneredMinMove;
            MinDwellSec = minDwellSec;
        }

        public static KiteSettings From(HeroArchetype_KiteRow row)
        {
            return new KiteSettings(row.triggerDist, row.resumeDist, row.threatRadius, row.arenaSoftRadius,
                row.corneredCheckSec, row.corneredMinMove, row.minDwellSec);
        }
    }

    //# 한 프레임 입력 — AutoCombatAI 가 월드에서 수집해 넘긴다(순수 로직이 월드를 모르게).
    public struct KiteInput
    {
        public Vector3 Position;
        public bool HasThreat;              //# 살아있는 몬스터가 하나라도 있나
        public Vector3 NearestThreatPos;
        public float NearestThreatDist;
        public bool HasCentroid;            //# threatRadius 안 위협 무게중심 유효
        public Vector3 ThreatCentroid;
        public float TargetDist;            //# 사격 타깃까지 거리
        public float Range;                 //# 공격 사거리
        public float EngageBuffer;          //# 교전 해제 히스테리시스 버퍼
    }

    //# 카이팅 상태기계 + 후퇴 방향 계산 (Unity 월드 비의존 — Vector3 값만). 기사(Melee) 경로와 무관하게 Kiter 영웅에만 생성된다.
    public class KiteBehavior
    {
        private readonly KiteSettings _settings;
        private float _stateEnterTime;
        private float _checkStartTime;
        private Vector3 _checkStartPos;

        public KiteBehavior(KiteSettings settings)
        {
            _settings = settings;
            Reset();
        }

        public KiteSettings Settings => _settings;
        public EKiteState State { get; private set; }
        //# 후퇴 단위방향(XZ). Retreat 일 때만 의미.
        public Vector3 RetreatDirection { get; private set; }

        //# 사격 허용 — 교전 또는 갇힘. 후퇴 중엔 억제(모션에 묶이지 않게).
        public bool CanShoot => State == EKiteState.Engage || State == EKiteState.Cornered;

        //# 풀 재사용 — 상태 잔존 방지.
        public void Reset()
        {
            State = EKiteState.Approach;
            RetreatDirection = Vector3.zero;
            _stateEnterTime = float.NegativeInfinity;
            _checkStartTime = 0f;
            _checkStartPos = Vector3.zero;
        }

        public void Update(float now, KiteInput input)
        {
            switch (State)
            {
                case EKiteState.Approach:
                case EKiteState.Engage:
                    UpdateApproachOrEngage(now, input);
                    break;
                case EKiteState.Retreat:
                    UpdateRetreat(now, input);
                    break;
                default:
                    UpdateCornered(now, input);
                    break;
            }
        }

        private void UpdateApproachOrEngage(float now, KiteInput input)
        {
            if (input.HasThreat && input.NearestThreatDist < _settings.TriggerDist && DwellPassed(now))
            {
                EnterRetreat(now, input);
                return;
            }

            //# 접근↔교전은 사거리 히스테리시스만 — 최소 유지 시간 대상 아님(교전↔후퇴 전이만 진동 방지 대상).
            if (State == EKiteState.Engage)
            {
                State = input.TargetDist > input.Range + input.EngageBuffer ? EKiteState.Approach : EKiteState.Engage;
            }
            else
            {
                State = input.TargetDist <= input.Range ? EKiteState.Engage : EKiteState.Approach;
            }
        }

        private void UpdateRetreat(float now, KiteInput input)
        {
            RetreatDirection = ComputeRetreatDirection(input, RetreatDirection, _settings.ArenaSoftRadius);

            //# 해제 우선 — 위협이 resumeDist 이상 멀어졌고 최소 유지 시간 경과(멀어진 뒤엔 정지해도 갇힘이 아니다).
            if ((input.HasThreat == false || input.NearestThreatDist >= _settings.ResumeDist) && DwellPassed(now))
            {
                EnterBase(now, input);
                return;
            }

            //# 갇힘 판정 — 관찰 구간 동안 이동이 corneredMinMove 미만.
            if (now - _checkStartTime >= _settings.CorneredCheckSec)
            {
                float moved = PlanarDistance(input.Position, _checkStartPos);
                if (moved < _settings.CorneredMinMove)
                {
                    EnterState(EKiteState.Cornered, now, input);
                    return;
                }
                _checkStartTime = now;
                _checkStartPos = input.Position;
            }
        }

        private void UpdateCornered(float now, KiteInput input)
        {
            if ((input.HasThreat == false || input.NearestThreatDist >= _settings.ResumeDist) && DwellPassed(now))
            {
                EnterBase(now, input);
                return;
            }

            //# 관찰 구간마다 이동(넉백 등)이 재개됐는지 확인 — 재개됐으면 후퇴를 다시 시도.
            if (now - _checkStartTime >= _settings.CorneredCheckSec)
            {
                float moved = PlanarDistance(input.Position, _checkStartPos);
                _checkStartTime = now;
                _checkStartPos = input.Position;
                if (moved >= _settings.CorneredMinMove)
                {
                    EnterRetreat(now, input);
                }
            }
        }

        private void EnterRetreat(float now, KiteInput input)
        {
            RetreatDirection = ComputeRetreatDirection(input, RetreatDirection, _settings.ArenaSoftRadius);
            EnterState(EKiteState.Retreat, now, input);
        }

        //# 후퇴/갇힘 해제 후 기본 상태(접근/교전) 복귀.
        private void EnterBase(float now, KiteInput input)
        {
            EnterState(input.TargetDist <= input.Range ? EKiteState.Engage : EKiteState.Approach, now, input);
        }

        private void EnterState(EKiteState next, float now, KiteInput input)
        {
            State = next;
            _stateEnterTime = now;
            _checkStartTime = now;
            _checkStartPos = input.Position;
        }

        private bool DwellPassed(float now)
        {
            return now - _stateEnterTime >= _settings.MinDwellSec;
        }

        private static float PlanarDistance(Vector3 a, Vector3 b)
        {
            a.y = 0f;
            b.y = 0f;
            return Vector3.Distance(a, b);
        }

        //# 후퇴 방향 — 위협 무게중심의 반대, 중심이 자기 위치와 거의 같으면(대칭 포위) 직전 방향 유지, 직전도 없으면 최근접 반대.
        //# 원점에서 arenaSoftRadius 를 넘으면 원점 방향 단위벡터를 더해 정규화(전장 가장자리에 몰리지 않게).
        public static Vector3 ComputeRetreatDirection(KiteInput input, Vector3 previous, float arenaSoftRadius)
        {
            Vector3 pos = input.Position;
            pos.y = 0f;

            Vector3 away = Vector3.zero;
            if (input.HasCentroid)
            {
                Vector3 centroid = input.ThreatCentroid;
                centroid.y = 0f;
                away = pos - centroid;
            }
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = previous;
            }
            if (away.sqrMagnitude <= 0.0001f && input.HasThreat)
            {
                Vector3 nearest = input.NearestThreatPos;
                nearest.y = 0f;
                away = pos - nearest;
            }
            if (away.sqrMagnitude <= 0.0001f)
            {
                away = -pos;   //# 위협 정보가 전혀 없으면 원점 쪽
            }

            Vector3 dir = away.normalized;
            if (arenaSoftRadius > 0f && pos.magnitude > arenaSoftRadius && pos.sqrMagnitude > 0.0001f)
            {
                Vector3 toOrigin = (-pos).normalized;
                Vector3 blended = dir + toOrigin;
                if (blended.sqrMagnitude > 0.0001f)
                {
                    dir = blended.normalized;
                }
                else
                {
                    dir = toOrigin;
                }
            }
            return dir;
        }

        //# 회피 도약 목적지 등 — 원점 기준 반경 안으로 클램프(XZ). radius <= 0 이면 무제한.
        public static Vector3 ClampToRadius(Vector3 position, float radius)
        {
            if (radius <= 0f)
                return position;
            Vector3 flat = new Vector3(position.x, 0f, position.z);
            if (flat.magnitude <= radius)
                return position;
            Vector3 clamped = flat.normalized * radius;
            return new Vector3(clamped.x, position.y, clamped.z);
        }
    }
}
