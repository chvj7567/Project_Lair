using UnityEngine;

namespace Lair.Character
{
    //# P3 (HP 30%) — 쿨다운마다 영웅 주변 원형 폭발. 반경 내 몬스터 일괄 데미지 + 넉백.
    [CreateAssetMenu(fileName = "HeroSkill_AoeNova", menuName = "Lair/Hero Skills/AOE Nova")]
    public class AoeNovaSkillData : HeroSkillData
    {
        [SerializeField] private int _damage = 100;
        [SerializeField] private float _cooldown = 7f;
        [SerializeField] private float _radius = 3.5f;
        [SerializeField] private float _knockbackStrength = 3f;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float Radius => _radius;
        public float KnockbackStrength => _knockbackStrength;

        public override IHeroSkillRuntime CreateRuntime() => new AoeNovaRuntime(this);
    }

    public class AoeNovaRuntime : IHeroSkillRuntime
    {
        private readonly AoeNovaSkillData _data;
        private float _cooldownRemain;

        public AoeNovaRuntime(AoeNovaSkillData data)
        {
            _data = data;
            _cooldownRemain = data.Cooldown;
        }

        public void Tick(IHeroSkillContext ctx, float dt)
        {
            _cooldownRemain -= dt;
            if (_cooldownRemain > 0f)
                return;

            ctx.DamageMonstersInRing(0f, _data.Radius, _data.Damage, _data.KnockbackStrength);
            _cooldownRemain = _data.Cooldown;
            HeroSkillFx.SpawnAt(Lair.Data.EVisual.HeroNovaFx, ctx.HeroPosition, _data.Radius * 2f);
        }

        public void OnDeactivate() { }
    }
}
