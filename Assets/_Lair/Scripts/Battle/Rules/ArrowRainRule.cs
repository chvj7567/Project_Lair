using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 화살비 (chapter-system §3.4) — 보스 페이즈. intervalSec 주기로 영웅 주변 몬스터 최밀집 지점에 범위 연속 피해.
    //# 한 주기 = 예고(WarningSec) → tickCount 회 타격(tickIntervalSec 간격). 몬스터가 없으면 그 주기는 건너뛴다.
    public class ArrowRainRule : IStageRule
    {
        //# 첫 낙하 전 예고 시간 — §3.4 "예고: 붉은 원형 경고(1.0초)" (파라미터 집합에 없는 명세 고정값).
        private const float WarningSec = 1f;
        //# 알림 배너 문구 id — "화살비" (Strings_Ko 1205).
        public const int NoticeStringId = 1205;

        private readonly float _intervalSec;
        private readonly float _searchRadius;
        private readonly float _radius;
        private readonly int _tickCount;
        private readonly float _tickIntervalSec;
        private readonly int _damagePerTick;

        private IStageRuleContext _ctx;
        private IStageFx _fx;
        private readonly List<Vector3> _center = new List<Vector3>(1) { Vector3.zero };
        private float _cycleElapsed;
        private bool _hasTarget;
        private int _ticksDone;

        public ArrowRainRule(StageRuleParams p)
        {
            _intervalSec = p.Get("intervalSec");
            _searchRadius = p.Get("searchRadius");
            _radius = p.Get("radius");
            _tickCount = p.GetInt("tickCount");
            _tickIntervalSec = p.Get("tickIntervalSec");
            _damagePerTick = p.GetInt("damagePerTick");
        }

        public EStageRule Id => EStageRule.ArrowRain;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            IsActive = true;
            _cycleElapsed = 0f;
            _ctx.ShowNotice(NoticeStringId);
            BeginCycle();
        }

        public void Tick(float elapsed, float dt)
        {
            if (IsActive == false)
                return;

            _cycleElapsed += dt;
            while (_hasTarget && _ticksDone < _tickCount && _cycleElapsed >= WarningSec + _ticksDone * _tickIntervalSec)
            {
                _ctx.DamageMonstersInSpheres(_center, _radius, _damagePerTick);
                ++_ticksDone;
                if (_ticksDone >= _tickCount)
                {
                    ReleaseFx();
                }
            }

            if (_cycleElapsed >= _intervalSec)
            {
                _cycleElapsed -= _intervalSec;
                BeginCycle();
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            _hasTarget = false;
            ReleaseFx();
        }

        //# 주기 시작 — 지금 밀집 지점을 확정(예고 동안 고정). 대상이 없으면 다음 주기로.
        private void BeginCycle()
        {
            ReleaseFx();
            _ticksDone = 0;
            _hasTarget = _ctx.TryFindMonsterDensePoint(_searchRadius, _radius, out Vector3 point);
            if (_hasTarget == false)
                return;

            _center[0] = point;
            _fx = _ctx.ShowFx(EVisual.StageArrowRainFx, point, _radius * 2f);
        }

        private void ReleaseFx()
        {
            if (_fx == null)
                return;
            _fx.Release();
            _fx = null;
        }
    }
}
