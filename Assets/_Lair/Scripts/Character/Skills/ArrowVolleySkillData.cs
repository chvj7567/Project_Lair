using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 궁수 화살 소나기 (HP 65%, chapter-system §5.4) — 영웅 중심 searchRadius 안 가장 밀집한 지점에 tickCount 연속 타격(한 몬스터 1틱 1회).
    //# 보스 규칙 ArrowRain(화살비)과는 별개 — 표시명도 "화살 소나기".
    [CreateAssetMenu(fileName = "HeroSkill_ArrowVolley", menuName = "Lair/Hero Skills/Arrow Volley")]
    public class ArrowVolleySkillData : HeroSkillData
    {
        [SerializeField] private int _damagePerTick = 40;
        [SerializeField] private int _tickCount = 3;
        [SerializeField] private float _tickIntervalSec = 0.5f;
        [SerializeField] private float _radius = 2.8f;
        [SerializeField] private float _cooldown = 8f;
        [SerializeField] private float _targetSearchRadius = 7f;

        public int DamagePerTick => _damagePerTick;
        public int TickCount => _tickCount;
        public float TickIntervalSec => _tickIntervalSec;
        public float Radius => _radius;
        public float Cooldown => _cooldown;
        public float TargetSearchRadius => _targetSearchRadius;

        public override IHeroSkillRuntime CreateRuntime() => new ArrowVolleyRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 진행 중 연타(남은 타수·타이머·고정된 낙하 지점). 연타 중에도 쿨다운은 시작 시점부터 흐른다.
    public class ArrowVolleyRuntime : IHeroSkillRuntime
    {
        private readonly ArrowVolleySkillData _data;
        private readonly List<Vector3> _center = new List<Vector3>(1) { Vector3.zero };
        private float _cooldownRemain;
        private int _ticksLeft;
        private float _tickTimer;
        private bool _missingContextLogged;

        public ArrowVolleyRuntime(ArrowVolleySkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            IArcherSkillContext archer = ctx as IArcherSkillContext;
            if (archer == null)
            {
                LogMissingContext();
                return;
            }

            _cooldownRemain -= dt;

            //# 진행 중 연타 — 첫 타격은 발동 즉시, 이후 tickIntervalSec 간격.
            if (_ticksLeft > 0)
            {
                _tickTimer -= dt;
                while (_ticksLeft > 0 && _tickTimer <= 0f)
                {
                    archer.DamageMonstersInSpheres(_center, _data.Radius, _data.DamagePerTick, 0f);
                    --_ticksLeft;
                    _tickTimer += _data.TickIntervalSec;
                }
            }

            if (_cooldownRemain > 0f || _ticksLeft > 0)
                return;

            if (archer.TryFindMonsterDensePoint(_data.TargetSearchRadius, _data.Radius, out Vector3 point) == false)
                return;   //# 몬스터 없음 — 보류

            _center[0] = point;
            _cooldownRemain = _data.Cooldown;
            _ticksLeft = _data.TickCount;
            _tickTimer = 0f;   //# 다음 Tick 이 아니라 이번 호출에서 바로 첫 타격
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroArrowVolleyFx, point, _data.Radius * 2f, Quaternion.identity);
            while (_ticksLeft > 0 && _tickTimer <= 0f)
            {
                archer.DamageMonstersInSpheres(_center, _data.Radius, _data.DamagePerTick, 0f);
                --_ticksLeft;
                _tickTimer += _data.TickIntervalSec;
            }
        }

        public void OnDeactivate()
        {
            _ticksLeft = 0;
        }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[ArrowVolleyRuntime] IArcherSkillContext 미구현 컨텍스트 — 궁수 스킬 비활성");
        }
    }
}
