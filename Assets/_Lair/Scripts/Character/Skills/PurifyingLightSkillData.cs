using UnityEngine;

namespace Lair.Character
{
    //# 성기사 정화의 빛 (HP 65%, chapter-5 §4) - 영웅의 시한 디버프 슬롯이 triggerDebuffCount 종 이상이면 전부 해제하고 최대 HP healPercent 회복.
    //# 영구 디버프(HeroAttackDown·EternalBleed)는 해제하지 않는다. SlowAura 슬롯은 출처와 무관하게 1종으로 센다.
    [CreateAssetMenu(fileName = "HeroSkill_PurifyingLight", menuName = "Lair/Hero Skills/Purifying Light")]
    public class PurifyingLightSkillData : HeroSkillData
    {
        [SerializeField] private int _triggerDebuffCount = 2;
        [SerializeField] private float _healPercent = 0.08f;
        [SerializeField] private float _cooldown = 12f;

        public int TriggerDebuffCount => _triggerDebuffCount;
        public float HealPercent => _healPercent;
        public float Cooldown => _cooldown;

        //# 조건이 드물게 맞으므로 맞을 때 놓치지 않게 최우선 - 정화의 빛(2) > 성역(1) > 심판의 망치(0).
        public override int CastPriority => 2;

        public override IHeroSkillRuntime CreateRuntime() => new PurifyingLightRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머.
    public class PurifyingLightRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly PurifyingLightSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private bool _missingContextLogged;

        public PurifyingLightRuntime(PurifyingLightSkillData data)
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

            _cooldownRemain -= dt;
            _checkTimer += dt;
            if (_cooldownRemain > 0f || _checkTimer < CheckIntervalSec)
                return;
            _checkTimer = 0f;

            if (paladin.IsHeroActive == false)
                return;
            if (paladin.CountTimedDebuffs() < _data.TriggerDebuffCount)
                return;

            _cooldownRemain = _data.Cooldown;
            paladin.ClearTimedDebuffs();
            paladin.HealHeroPercent(_data.HealPercent);
            //# FX 는 프리팹에 구워진 크기(스케일 1) - 영웅 위치에서 1회 재생 후 자동 반환.
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroPurifyingLightFx, paladin.HeroPosition, 1f, Quaternion.identity);
        }

        public void OnDeactivate() { }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[PurifyingLightRuntime] IPaladinSkillContext 미구현 컨텍스트 - 성기사 스킬 비활성");
        }
    }
}
