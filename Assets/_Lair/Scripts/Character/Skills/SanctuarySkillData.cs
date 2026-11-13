using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# 성기사 성역 (HP 45%, chapter-5 §4) - triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면 시전 위치에 durationSec 동안
    //# 영웅 받는 피해 x damageTakenMul + 반경 radius 안 몬스터 이동 x monsterSpeedMul. 쿨다운은 시전 시점부터 흐른다.
    public class SanctuarySkillData : HeroSkillData
    {
        private float _durationSec;
        private float _damageTakenMul;
        private float _radius;
        private float _monsterSpeedMul;
        private float _cooldown;
        private int _triggerMonsterCount;
        private float _triggerRadius;

        public float DurationSec => _durationSec;
        public float DamageTakenMul => _damageTakenMul;
        public float Radius => _radius;
        public float MonsterSpeedMul => _monsterSpeedMul;
        public float Cooldown => _cooldown;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        public SanctuarySkillData(string displayName, Lair.Data.HeroSkillParams p) : base(displayName)
        {
            _durationSec = p.Get("durationSec");
            _damageTakenMul = p.Get("damageTakenMul");
            _radius = p.Get("radius");
            _monsterSpeedMul = p.Get("monsterSpeedMul");
            _cooldown = p.Get("cooldown");
            _triggerMonsterCount = p.GetInt("triggerMonsterCount");
            _triggerRadius = p.Get("triggerRadius");
        }

        //# 동시 조건 시 우선순위 - 정화의 빛(2) > 성역(1) > 심판의 망치(0).
        public override int CastPriority => 1;

        public override IHeroSkillRuntime CreateRuntime() => new SanctuaryRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머 + 진행 중 성역(남은 시간·고정 중심·FX 핸들).
    public class SanctuaryRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly SanctuarySkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private float _zoneRemain;
        private Vector3 _zoneCenter;
        private IPaladinSkillContext _activeCtx;
        private CHPoolable _fx;
        private bool _missingContextLogged;

        public SanctuaryRuntime(SanctuarySkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            IPaladinSkillContext paladin = ctx as IPaladinSkillContext;
            if (paladin == null)
            {
                LogMissingContext();
                return;
            }

            //# 성역은 월드 지속 효과라 영웅 정지 중에도 시간이 흐르고, 시전만 영웅이 행동 가능할 때 한다.
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
                    paladin.ApplyMonsterSlowZone(_zoneCenter, _data.Radius, _data.MonsterSpeedMul, 1f, 0f);
                }
            }

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _zoneRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (paladin.IsHeroActive == false)
                return;
            if (paladin.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            _activeCtx = paladin;
            _zoneCenter = paladin.HeroPosition;
            _zoneRemain = _data.DurationSec;
            _cooldownRemain = _data.Cooldown;
            paladin.MultiplyHeroDamageTaken(_data.DamageTakenMul);
            paladin.ApplyMonsterSlowZone(_zoneCenter, _data.Radius, _data.MonsterSpeedMul, 1f, 0f);
            //# 루프 FX(반경 2.6 이 프리팹에 구워짐, 스케일 1) - 영웅에 부착, EndZone 이 풀에 반환. 풀/프리팹이 없으면 null(FX 만 생략).
            _fx = HeroSkillFx.SpawnAttached(Lair.Data.EVisual.HeroSanctuaryFx, paladin.HeroTransform, Vector3.zero, 1f);
        }

        public void OnDeactivate()
        {
            EndZone();
        }

        //# 피해 배율 원복(시전 때 곱한 값의 역수) + 지대 해제 + FX 반환. 중복 호출 안전.
        private void EndZone()
        {
            if (_activeCtx != null)
            {
                _activeCtx.ClearMonsterSlowZone();
                if (_data.DamageTakenMul > 0f)
                {
                    _activeCtx.MultiplyHeroDamageTaken(1f / _data.DamageTakenMul);
                }
            }
            _activeCtx = null;
            _zoneRemain = 0f;
            if (_fx != null && CHMPool.Instance != null)
            {
                CHMPool.Instance.Push(_fx);
            }
            _fx = null;
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[SanctuaryRuntime] IPaladinSkillContext 미구현 컨텍스트 - 성기사 스킬 비활성");
        }
    }
}
