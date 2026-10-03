using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 사냥꾼의 덫 (chapter-system §3.4) — 보스 페이즈. 영웅 주변 링에 덫 trapCount 개를 설치(lifeSec 유지, rearmSec 마다 재설치).
    //# 덫 안 몬스터는 이동속도 monsterSpeedMul 배(이탈 즉시 복구) + 초당 damagePerSec 피해(0.5초 틱).
    public class HunterTrapsRule : IStageRule
    {
        //# 피해 틱 간격 — §3.4 "0.5초 틱 x 5" (파라미터 집합에 없는 명세 고정값).
        private const float DamageTickSec = 0.5f;
        //# 알림 배너 문구 id — "사냥꾼의 덫" (Strings_Ko 1206).
        public const int NoticeStringId = 1206;

        private readonly int _trapCount;
        private readonly float _ringDistance;
        private readonly float _trapRadius;
        private readonly float _lifeSec;
        private readonly float _rearmSec;
        private readonly float _monsterSpeedMul;
        private readonly float _damagePerSec;

        private IStageRuleContext _ctx;
        private readonly List<Vector3> _centers = new List<Vector3>();
        private readonly List<IStageFx> _fxList = new List<IStageFx>();
        private float _cycleElapsed;
        private float _damageAcc;
        private bool _trapsUp;

        public HunterTrapsRule(StageRuleParams p)
        {
            _trapCount = p.GetInt("trapCount");
            _ringDistance = p.Get("ringDistance");
            _trapRadius = p.Get("trapRadius");
            _lifeSec = p.Get("lifeSec");
            _rearmSec = p.Get("rearmSec");
            _monsterSpeedMul = p.Get("monsterSpeedMul");
            _damagePerSec = p.Get("damagePerSec");
        }

        public EStageRule Id => EStageRule.HunterTraps;
        public bool IsActive { get; private set; }

        public void Activate(IStageRuleContext ctx)
        {
            _ctx = ctx;
            IsActive = true;
            _cycleElapsed = 0f;
            _ctx.ShowNotice(NoticeStringId);
            InstallTraps();
        }

        public void Tick(float elapsed, float dt)
        {
            if (IsActive == false)
                return;

            _cycleElapsed += dt;
            if (_trapsUp && _cycleElapsed >= _lifeSec)
            {
                RemoveTraps();
            }
            if (_cycleElapsed >= _rearmSec)
            {
                _cycleElapsed -= _rearmSec;
                RemoveTraps();
                InstallTraps();
            }

            if (_trapsUp == false)
                return;

            //# 감속은 매 프레임 판정 — 덫 밖으로 나가면 즉시 복구.
            _ctx.SetMonsterSpeedScaleInSpheres(_centers, _trapRadius, _monsterSpeedMul);
            _damageAcc += dt;
            while (_damageAcc >= DamageTickSec)
            {
                _damageAcc -= DamageTickSec;
                _ctx.DamageMonstersInSpheres(_centers, _trapRadius, Mathf.RoundToInt(_damagePerSec * DamageTickSec));
            }
        }

        public void Deactivate()
        {
            IsActive = false;
            RemoveTraps();
        }

        //# 설치 시점 영웅 위치 기준 링에 균등 배치 — 전장 밖은 ClampToArena 로 당겨 넣는다.
        private void InstallTraps()
        {
            RemoveTraps();
            _trapsUp = true;
            _damageAcc = 0f;
            Vector3 origin = _ctx.HeroPosition;
            for (int i = 0; i < _trapCount; ++i)
            {
                float angle = i * (360f / _trapCount) * Mathf.Deg2Rad;
                Vector3 raw = origin + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _ringDistance;
                Vector3 position = _ctx.ClampToArena(raw);
                _centers.Add(position);
                _fxList.Add(_ctx.ShowFx(EVisual.StageTrapFx, position, _trapRadius * 2f));
            }
        }

        //# 덫 제거 — FX 반환 + 감속 즉시 원복. 중복 호출 안전.
        private void RemoveTraps()
        {
            if (_trapsUp == false)
                return;
            _trapsUp = false;
            foreach (IStageFx fx in _fxList)
            {
                fx.Release();
            }
            _fxList.Clear();
            _centers.Clear();
            _ctx.ClearMonsterSpeedScale();
        }
    }
}
