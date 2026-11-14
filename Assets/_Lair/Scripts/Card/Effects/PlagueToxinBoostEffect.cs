using Lair.Data;

namespace Lair.Card
{
    //# 카드 "역병의 손길" (ID PlagueSlowBoost 유지) — 역병귀 독 피해 배율 글로벌 영구 버프 (PoisonDamageMul x_toxinMul, monster-growth-currencies §15.0).
    public class PlagueToxinBoostEffect : ICardEffect
    {
        private readonly float _toxinMul;

        public PlagueToxinBoostEffect(CardParams p)
        {
            _toxinMul = p.Get("toxinMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.PoisonDamage, _toxinMul);
    }
}
