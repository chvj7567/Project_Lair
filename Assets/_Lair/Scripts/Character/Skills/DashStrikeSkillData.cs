using UnityEngine;

namespace Lair.Character
{
    //# P1 (HP 90%) — 영웅이 몬스터 무게중심 방향으로 부채꼴(cone) 관통 데미지(+넉백).
    [CreateAssetMenu(fileName = "HeroSkill_DashStrike", menuName = "Lair/Hero Skills/Dash Strike")]
    public class DashStrikeSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 80;
        [SerializeField] private float _cooldown = 3f;
        [SerializeField] private float _dashLength = 7f;
        [SerializeField] private float _coneHalfAngle = 35f;   //# 부채꼴 반각(도) — 전체각 = 2×
        [SerializeField] private float _knockbackStrength = 2f;
        [SerializeField] private float _centroidRadius = 8f;   //# 방향 결정용 무게중심 수집 반경

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float DashLength => _dashLength;
        public float ConeHalfAngle => _coneHalfAngle;
        public float KnockbackStrength => _knockbackStrength;
        public float CentroidRadius => _centroidRadius;

        public override IHeroSkillRuntime CreateRuntime() => new DashStrikeRuntime(this);
    }

    //# 가변 상태 = 쿨다운 타이머. 발동 시 비주얼은 CHMPool(가용 시)로 스폰.
    public class DashStrikeRuntime : IHeroSkillRuntime
    {
        private readonly DashStrikeSkillData _data;
        private float _cooldownRemain;

        public DashStrikeRuntime(DashStrikeSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;   //# 활성 직후 즉발 방지 — 첫 쿨다운 대기
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            if (_cooldownRemain > 0f)
                return;

            Vector3 centroid = ctx.MonsterCentroid(_data.CentroidRadius);
            Vector3 dir = centroid - ctx.HeroPosition;
            dir.y = 0f;
            if (dir.sqrMagnitude < 0.0001f)
                return;   //# 몬스터 없음 — 발동 보류(쿨다운은 유지, 다음 프레임 재시도)

            dir.Normalize();
            ctx.DamageMonstersInCone(dir, _data.DashLength, _data.ConeHalfAngle, _data.Damage, _data.KnockbackStrength);
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnCone(Lair.Data.EVisual.HeroDashFx, ctx.HeroPosition, dir, _data.DashLength);
        }

        public void OnDeactivate() { }
    }
}
