using UnityEngine;

namespace Lair.Character
{
    //# P3 (HP 30%) — 쿨다운마다 영웅 주변 원형 폭발. 반경 내 몬스터 일괄 데미지 + 넉백.
    public class AoeNovaSkillData : HeroSkillData
    {
        private int _damage;
        private float _cooldown;
        private float _radius;
        private float _knockbackStrength;

        public int Damage => _damage;
        public float Cooldown => _cooldown;
        public float Radius => _radius;
        public float KnockbackStrength => _knockbackStrength;

        public AoeNovaSkillData(string displayName, Lair.Data.HeroSkillParams p) : base(displayName)
        {
            _damage = p.GetInt("damage");
            _cooldown = p.Get("cooldown");
            _radius = p.Get("radius");
            _knockbackStrength = p.Get("knockbackStrength");
        }

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
            //# 시전음 — 쿨다운형(DashStrike 와 동일 구조), 매 쿨다운 발동마다 P2Skill.
            HeroSkillFx.PlaySound(Lair.Data.EAudio.P2Skill);
        }

        public void OnDeactivate() { }
    }
}
