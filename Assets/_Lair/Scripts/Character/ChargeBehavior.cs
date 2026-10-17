using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 군집 돌진 상태 (chapter-4 §3.3). 탐색 → 이동 → 교전.
    public enum EChargeState
    {
        Seek,     //# 후보 몬스터 없음 — 정지 대기
        Move,     //# 목표가 사거리 밖 — 접근
        Engage,   //# 사거리 안 — 정지 + 범위 베기
    }

    //# HeroArchetype_Charge row → 불변 설정 (HeroArchetype_Charge.json).
    public readonly struct ChargeSettings
    {
        public readonly float HuntRadius;
        public readonly float ClusterRadius;
        public readonly int RetargetCountMargin;
        public readonly float RetargetIntervalSec;
        public readonly float MinDwellSec;
        public readonly float ArenaSoftRadius;

        public ChargeSettings(float huntRadius, float clusterRadius, int retargetCountMargin, float retargetIntervalSec, float minDwellSec, float arenaSoftRadius)
        {
            HuntRadius = huntRadius;
            ClusterRadius = clusterRadius;
            RetargetCountMargin = retargetCountMargin;
            RetargetIntervalSec = retargetIntervalSec;
            MinDwellSec = minDwellSec;
            ArenaSoftRadius = arenaSoftRadius;
        }

        public static ChargeSettings From(HeroArchetype_ChargeRow row)
        {
            return new ChargeSettings(row.huntRadius, row.clusterRadius, row.retargetCountMargin, row.retargetIntervalSec, row.minDwellSec, row.arenaSoftRadius);
        }
    }

    //# 목표 후보 1체 — 월드 조회 결과를 순수 로직에 넘기는 값 묶음.
    public struct ClusterCandidate
    {
        public Transform Transform;
        public IHealth Health;
        public Vector3 Position;
    }

    //# 한 프레임 입력 — AutoCombatAI 가 월드에서 수집해 넘긴다(순수 로직이 월드를 모르게).
    public struct ChargeInput
    {
        public bool HasTarget;
        public bool TargetChanged;   //# 직전 프레임과 다른 목표 — 전이 최소 유지 시간을 무시한다
        public float TargetDist;
        public float Range;
        public float EngageBuffer;
    }

    //# 군집 돌진 상태기계 + 목표 선정 (Unity 월드 비의존 — 값만). Charger 영웅에만 생성된다(기사·궁수·암살자 경로 불변).
    public class ChargeBehavior
    {
        private readonly ChargeSettings _settings;
        private readonly List<int> _scores = new List<int>();
        private float _stateEnterTime;
        private float _nextEvalTime;
        private float _retargetBlockedUntil;

        public ChargeBehavior(ChargeSettings settings)
        {
            _settings = settings;
            Reset();
        }

        public ChargeSettings Settings => _settings;
        public EChargeState State { get; private set; }

        //# 풀 재사용 — 상태 잔존 방지.
        public void Reset()
        {
            State = EChargeState.Seek;
            _stateEnterTime = float.NegativeInfinity;
            _nextEvalTime = float.NegativeInfinity;
            _retargetBlockedUntil = float.NegativeInfinity;
        }

        //# 스킬(사슬 갈고리)이 군집을 키운 직후 목표 교체를 막는다 — 현재 목표가 죽은 경우는 예외(ShouldReevaluate).
        public void SuspendRetarget(float now, float seconds)
        {
            _retargetBlockedUntil = Mathf.Max(_retargetBlockedUntil, now + seconds);
        }

        //# 목표를 (재)선택할 시점인가. 목표가 무효면 즉시, 아니면 재평가 주기(차단 중 제외)마다.
        public bool ShouldReevaluate(float now, bool targetInvalid)
        {
            if (targetInvalid == false && (now < _nextEvalTime || now < _retargetBlockedUntil))
                return false;
            _nextEvalTime = now + _settings.RetargetIntervalSec;
            return true;
        }

        //# 상태 갱신. 추적↔교전 전이는 사거리 히스테리시스 + 최소 유지 시간(새 목표는 면제).
        public void Update(float now, ChargeInput input)
        {
            if (input.HasTarget == false)
            {
                State = EChargeState.Seek;
                return;
            }

            bool dwellPassed = input.TargetChanged || now - _stateEnterTime >= _settings.MinDwellSec;
            if (State == EChargeState.Engage)
            {
                if (input.TargetDist > input.Range + input.EngageBuffer && dwellPassed)
                {
                    EnterState(EChargeState.Move, now);
                }
            }
            else if (input.TargetDist <= input.Range && (State == EChargeState.Seek || dwellPassed))
            {
                EnterState(EChargeState.Engage, now);
            }
            else if (State == EChargeState.Seek)
            {
                EnterState(EChargeState.Move, now);
            }
        }

        private void EnterState(EChargeState next, float now)
        {
            if (State == next)
                return;
            State = next;
            _stateEnterTime = now;
        }

        //# 목표 선정 — clusterScore(후보 중심 clusterRadius 안 후보 수, 자신 포함) 최대, 동점이면 영웅에 가까운 쪽. 후보가 없으면 -1.
        //# current 가 후보에 있으면 최고 점수가 current 점수 + retargetCountMargin 이상일 때만 교체(진동 방지).
        public int SelectTarget(IReadOnlyList<ClusterCandidate> candidates, Vector3 heroPosition, Transform current)
        {
            if (candidates == null || candidates.Count == 0)
                return -1;

            _scores.Clear();
            for (int i = 0; i < candidates.Count; ++i)
            {
                _scores.Add(ClusterScore(candidates, i, _settings.ClusterRadius));
            }

            int best = 0;
            int currentIndex = candidates[0].Transform == current && current != null ? 0 : -1;
            for (int i = 1; i < candidates.Count; ++i)
            {
                if (current != null && candidates[i].Transform == current)
                {
                    currentIndex = i;
                }
                if (IsBetter(i, best, candidates, heroPosition))
                {
                    best = i;
                }
            }

            if (currentIndex < 0 || currentIndex == best)
                return best;
            return _scores[best] >= _scores[currentIndex] + _settings.RetargetCountMargin ? best : currentIndex;
        }

        //# 후보 index 중심 clusterRadius 안 후보 수(자신 포함, XZ).
        public static int ClusterScore(IReadOnlyList<ClusterCandidate> candidates, int index, float clusterRadius)
        {
            int count = 0;
            Vector3 center = candidates[index].Position;
            for (int j = 0; j < candidates.Count; ++j)
            {
                if (SkillGeometry.InSphere(candidates[j].Position, center, clusterRadius))
                {
                    ++count;
                }
            }
            return count;
        }

        private bool IsBetter(int a, int b, IReadOnlyList<ClusterCandidate> candidates, Vector3 heroPosition)
        {
            if (_scores[a] != _scores[b])
                return _scores[a] > _scores[b];
            return (candidates[a].Position - heroPosition).sqrMagnitude < (candidates[b].Position - heroPosition).sqrMagnitude;
        }
    }
}
