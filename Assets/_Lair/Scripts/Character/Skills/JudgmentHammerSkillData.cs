using UnityEngine;

namespace Lair.Character
{
    //# 성기사 심판의 망치 (HP 85%, chapter-5 §4) - 현재 표적이 영웅에게서 castRange 이내이면 그 위치에 radius 반경 damage + stunSec 기절.
    [CreateAssetMenu(fileName = "HeroSkill_JudgmentHammer", menuName = "Lair/Hero Skills/Judgment Hammer")]
    public class JudgmentHammerSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 110;
        [SerializeField] private float _cooldown = 6f;
        [SerializeField] private float _radius = 2.4f;
        [SerializeField] private float _stunSec = 0.6f;
        [SerializeField] private float _castRange = 5f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float Radius => _radius;
        public float StunSec => _stunSec;
        public float CastRange => _castRange;

        //# 동시 조건 시 우선순위 - 정화의 빛(2) > 성역(1) > 심판의 망치(0).
        public override int CastPriority => 0;

        public override IHeroSkillRuntime CreateRuntime() => new JudgmentHammerRuntime(this);
    }

    //# 가변 상태 = 쿨다운 + 트리거 검사 타이머.
    public class JudgmentHammerRuntime : IHeroSkillRuntime
    {
        public const float CheckIntervalSec = 0.25f;

        private readonly JudgmentHammerSkillData _data;
        private float _cooldownRemain;
        private float _checkTimer;
        private bool _missingContextLogged;

        public JudgmentHammerRuntime(JudgmentHammerSkillData data)
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
            if (paladin.TryGetHeroTargetPosition(_data.CastRange, out Vector3 position) == false)
                return;

            _cooldownRemain = _data.Cooldown;
            //# FX 는 반경이 프리팹에 구워져 있어 스케일 1 - 자동 반환.
            HeroSkillFx.SpawnAtRequired(Lair.Data.EVisual.HeroJudgmentHammerFx, position, 1f, Quaternion.identity);
            paladin.DamageAndStunMonstersInSphere(position, _data.Radius, _data.Damage, _data.StunSec);
        }

        public void OnDeactivate() { }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[JudgmentHammerRuntime] IPaladinSkillContext 미구현 컨텍스트 - 성기사 스킬 비활성");
        }
    }
}
