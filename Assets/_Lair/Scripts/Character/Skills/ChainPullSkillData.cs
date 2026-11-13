using UnityEngine;

namespace Lair.Character
{
    //# 광전사 사슬 갈고리 (HP 65%, chapter-4 §4) — triggerRadius 안 몬스터가 triggerMonsterCount 마리 이상이면 pullRadius 안 가까운 순 최대 maxTargets 마리에
    //# damage 를 주고 영웅 pullToDistance 거리까지 pullDurationSec 에 걸쳐 당긴다. 시전 직후 군집 목표 재평가를 잠시 중단.
    public class ChainPullSkillData : HeroSkillData
    {
        private int _damage;
        private float _cooldown;
        private float _pullRadius;
        private int _maxTargets;
        private float _pullToDistance;
        private float _pullDurationSec;
        private int _triggerMonsterCount;
        private float _triggerRadius;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float PullRadius => _pullRadius;
        public int MaxTargets => _maxTargets;
        public float PullToDistance => _pullToDistance;
        public float PullDurationSec => _pullDurationSec;
        public int TriggerMonsterCount => _triggerMonsterCount;
        public float TriggerRadius => _triggerRadius;

        public ChainPullSkillData(string displayName, Lair.Data.HeroSkillParams p) : base(displayName)
        {
            _damage = p.GetInt("damage");
            _cooldown = p.Get("cooldown");
            _pullRadius = p.Get("pullRadius");
            _maxTargets = p.GetInt("maxTargets");
            _pullToDistance = p.Get("pullToDistance");
            _pullDurationSec = p.Get("pullDurationSec");
            _triggerMonsterCount = p.GetInt("triggerMonsterCount");
            _triggerRadius = p.Get("triggerRadius");
        }

        //# 동시 조건 시 우선순위 — 모으기가 베기 효율의 전제: 사슬 갈고리(2) > 피의 광란(1) > 분쇄 도약(0).
        public override int CastPriority => 2;

        public override IHeroSkillRuntime CreateRuntime() => new ChainPullRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머. 당김 진행은 컨텍스트가 소유하고 런타임이 매 프레임 Tick 한다.
    public class ChainPullRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;
        //# 시전 중·직후 군집 재평가 중단 시간 — 명세 고정값(chapter-4 §3.3 #6).
        public const float RetargetLockSec = 0.6f;

        private readonly ChainPullSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private bool _missingContextLogged;

        public ChainPullRuntime(ChainPullSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            IBerserkerSkillContext berserker = ctx as IBerserkerSkillContext;
            if (berserker == null)
            {
                LogMissingContext();
                return;
            }
            if (berserker.IsHeroActive == false)
                return;

            _cooldownRemain -= dt;
            berserker.TickMonsterPull(dt);

            _checkTimer += dt;
            if (_cooldownRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (berserker.CountMonstersInRadius(_data.TriggerRadius) < _data.TriggerMonsterCount)
                return;

            _cooldownRemain = _data.Cooldown;
            berserker.SuspendHeroRetarget(RetargetLockSec);
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroChainPullFx, berserker.HeroPosition, 1f, Quaternion.identity);
            berserker.BeginMonsterPull(_data.PullRadius, _data.MaxTargets, _data.PullToDistance, _data.PullDurationSec, _data.Damage);
        }

        public void OnDeactivate() { }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[ChainPullRuntime] IBerserkerSkillContext 미구현 컨텍스트 — 광전사 스킬 비활성");
        }
    }
}
