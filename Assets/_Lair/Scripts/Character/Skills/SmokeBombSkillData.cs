using UnityEngine;

namespace Lair.Character
{
    //# 암살자 연막 탄 (HP 45%, chapter-3-hero §4) — triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면 현재 위치에 durationSec 연막 지대.
    //# 지대 안 몬스터 이동속도 x monsterSpeedMul, 공격 사거리 x monsterRangeMul(하한 max(monsterMinRange, 충돌 거리 + 0.1)). 시전 직후 슬립을 쿨다운 무시로 1회 요청.
    [CreateAssetMenu(fileName = "HeroSkill_SmokeBomb", menuName = "Lair/Hero Skills/Smoke Bomb")]
    public class SmokeBombSkillData : HeroSkillData
    {
        [SerializeField] private float _radius = 3f;
        [SerializeField] private float _durationSec = 3f;
        [SerializeField] private float _monsterSpeedMul = 0.6f;
        [SerializeField] private float _monsterRangeMul = 0.5f;
        [SerializeField] private float _monsterMinRange = 0.8f;
        [SerializeField] private float _cooldown = 12f;
        [SerializeField] private int _triggerMonsterCount = 4;
        [SerializeField] private float _triggerRadius = 2.4f;

        public float Radius => _radius;
        public float DurationSec => _durationSec;
        public float MonsterSpeedMul => _monsterSpeedMul;
        public float MonsterRangeMul => _monsterRangeMul;
        public float MonsterMinRange => _monsterMinRange;
        public float Cooldown => _cooldown;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        //# 동시 조건 시 우선순위 — 군중 탈출이 가장 위급: 연막 탄(2) > 그림자 습격(1) > 칼날 선풍(0).
        public override int CastPriority => 2;

        public override IHeroSkillRuntime CreateRuntime() => new SmokeBombRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머 + 진행 중 지대(남은 시간·고정 중심). 지대 중에도 쿨다운은 시작 시점부터 흐른다.
    public class SmokeBombRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly SmokeBombSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private float _zoneRemain;
        private Vector3 _zoneCenter;
        private IAssassinSkillContext _activeCtx;
        private bool _missingContextLogged;

        public SmokeBombRuntime(SmokeBombSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            IAssassinSkillContext assassin = ctx as IAssassinSkillContext;
            if (assassin == null)
            {
                LogMissingContext();
                return;
            }

            //# 지대는 월드 지속 효과라 영웅 정지(TimeStop) 중에도 시간이 흐르지만, 시전은 영웅이 행동 가능할 때만.
            _cooldownRemain -= dt;
            if (_zoneRemain > 0f)
            {
                _zoneRemain -= dt;
                if (_zoneRemain <= 0f)
                {
                    EndZone();
                }
                else
                {
                    assassin.ApplyMonsterSlowZone(_zoneCenter, _data.Radius, _data.MonsterSpeedMul, _data.MonsterRangeMul, _data.MonsterMinRange);
                }
            }

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _zoneRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (assassin.IsHeroActive == false)
                return;
            if (assassin.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            _activeCtx = assassin;
            _zoneCenter = assassin.HeroPosition;
            _zoneRemain = _data.DurationSec;
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroSmokeBombFx, _zoneCenter, _data.Radius * 2f, Quaternion.identity);
            assassin.ApplyMonsterSlowZone(_zoneCenter, _data.Radius, _data.MonsterSpeedMul, _data.MonsterRangeMul, _data.MonsterMinRange);
            assassin.RequestHeroSlip();
        }

        public void OnDeactivate()
        {
            EndZone();
        }

        //# 지대 해제 — 영향받던 몬스터 전원 원복. 중복 호출 안전.
        private void EndZone()
        {
            _zoneRemain = 0f;
            _activeCtx?.ClearMonsterSlowZone();
            _activeCtx = null;
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[SmokeBombRuntime] IAssassinSkillContext 미구현 컨텍스트 — 암살자 스킬 비활성");
        }
    }
}
