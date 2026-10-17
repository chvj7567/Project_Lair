using UnityEngine;

namespace Lair.Character
{
    //# 궁수 관통 사격 (HP 85%, chapter-system §5.4) — 몬스터 무게중심 방향으로 직선 관통 데미지(+약한 넉백). 접근하는 몬스터 줄을 꿴다.
    [CreateAssetMenu(fileName = "HeroSkill_PiercingShot", menuName = "Lair/Hero Skills/Piercing Shot")]
    public class PiercingShotSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 70;
        [SerializeField] private float _cooldown = 4f;
        [SerializeField] private float _lineLength = 9f;
        [SerializeField] private float _lineHalfWidth = 0.6f;
        [SerializeField] private float _knockbackStrength = 0.5f;
        [SerializeField] private float _centroidRadius = 8f;   //# 방향 결정용 무게중심 수집 반경

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float LineLength => _lineLength;
        public float LineHalfWidth => _lineHalfWidth;
        public float KnockbackStrength => _knockbackStrength;
        public float CentroidRadius => _centroidRadius;

        public override IHeroSkillRuntime CreateRuntime() => new PiercingShotRuntime(this);
    }

    //# 가변 상태 = 쿨다운 타이머. 몬스터가 없으면 발동을 보류하고 다음 프레임 재시도(DashStrike 와 같은 규약).
    public class PiercingShotRuntime : IHeroSkillRuntime
    {
        private readonly PiercingShotSkillData _data;
        private float _cooldownRemain;
        private bool _missingContextLogged;

        public PiercingShotRuntime(PiercingShotSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            if (_cooldownRemain > 0f)
                return;

            IArcherSkillContext archer = ctx as IArcherSkillContext;
            if (archer == null)
            {
                LogMissingContext();
                return;
            }

            Vector3 dir = archer.MonsterCentroid(_data.CentroidRadius) - archer.HeroPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;   //# 몬스터 없음 — 보류

            dir.Normalize();
            archer.DamageMonstersInLine(dir, _data.LineLength, _data.LineHalfWidth, _data.Damage, _data.KnockbackStrength);
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnLineRequired(Lair.Data.EVisual.HeroPiercingShotFx, archer.HeroPosition, dir, _data.LineLength);
        }

        public void OnDeactivate() { }

        private void LogMissingContext()
        {
            if (_missingContextLogged)
                return;
            _missingContextLogged = true;
            Debug.LogError("[PiercingShotRuntime] IArcherSkillContext 미구현 컨텍스트 — 궁수 스킬 비활성");
        }
    }
}
