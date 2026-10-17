using UnityEngine;

namespace Lair.Character
{
    //# 암살자 칼날 선풍 (HP 65%, chapter-3-hero §4) — triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면 시전.
    //# tickIntervalSec 간격으로 tickCount 번 반경 radius 안 전원에 damagePerTick(1틱 1회). 시전 중 영웅 받는 피해 x damageTakenMul.
    [CreateAssetMenu(fileName = "HeroSkill_BladeWhirl", menuName = "Lair/Hero Skills/Blade Whirl")]
    public class BladeWhirlSkillData : HeroSkillData
    {
        [SerializeField] private int _damagePerTick = 35;
        [SerializeField] private int _tickCount = 4;
        [SerializeField] private float _tickIntervalSec = 0.3f;
        [SerializeField] private float _radius = 2.4f;
        [SerializeField] private float _cooldown = 9f;
        [SerializeField] private float _damageTakenMul = 0.7f;
        [SerializeField] private int _triggerMonsterCount = 2;
        [SerializeField] private float _triggerRadius = 2.4f;

        public int DamagePerTick => _damagePerTick;
        public int TickCount => _tickCount;
        public float TickIntervalSec => _tickIntervalSec;
        public float Radius => _radius;
        public float Cooldown => _cooldown;
        public float DamageTakenMul => _damageTakenMul;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        public override IHeroSkillRuntime CreateRuntime() => new BladeWhirlRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머 + 진행 중 회전(남은 타수·타이머·피해 배율 적용 여부). 회전 중에도 쿨다운은 시작 시점부터 흐른다.
    public class BladeWhirlRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly BladeWhirlSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private int _ticksLeft;
        private float _tickTimer;
        private bool _guardApplied;
        private IAssassinSkillContext _activeCtx;
        private bool _missingContextLogged;

        public BladeWhirlRuntime(BladeWhirlSkillData data)
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

            //# TimeStop·사망 중엔 시전도 회전 진행도 멈춘다.
            if (assassin.IsHeroActive == false)
                return;

            _cooldownRemain -= dt;

            if (_ticksLeft > 0)
            {
                _tickTimer -= dt;
                while (_ticksLeft > 0 && _tickTimer <= 0f)
                {
                    FireTick(assassin);
                }
                if (_ticksLeft == 0)
                {
                    EndWhirl();
                }
            }

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _ticksLeft > 0 || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (assassin.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            _activeCtx = assassin;
            _cooldownRemain = _data.Cooldown;
            _ticksLeft = _data.TickCount;
            _tickTimer = 0f;   //# 첫 타격은 시전 즉시
            assassin.MultiplyHeroDamageTaken(_data.DamageTakenMul);
            _guardApplied = true;
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroBladeWhirlFx, assassin.HeroPosition, _data.Radius * 2f, Quaternion.identity);
            while (_ticksLeft > 0 && _tickTimer <= 0f)
            {
                FireTick(assassin);
            }
            if (_ticksLeft == 0)
            {
                EndWhirl();
            }
        }

        public void OnDeactivate()
        {
            _ticksLeft = 0;
            EndWhirl();
        }

        private void FireTick(IAssassinSkillContext assassin)
        {
            assassin.DamageMonstersInRing(0f, _data.Radius, _data.DamagePerTick, 0f);
            --_ticksLeft;
            _tickTimer += _data.TickIntervalSec;
        }

        //# 받는 피해 배율 원복 — 시전 때 곱한 값의 역수. 중복 호출 안전.
        private void EndWhirl()
        {
            if (_guardApplied == false)
                return;
            _guardApplied = false;
            if (_data.DamageTakenMul > 0f)
            {
                _activeCtx?.MultiplyHeroDamageTaken(1f / _data.DamageTakenMul);
            }
            _activeCtx = null;
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[BladeWhirlRuntime] IAssassinSkillContext 미구현 컨텍스트 — 암살자 스킬 비활성");
        }
    }
}
