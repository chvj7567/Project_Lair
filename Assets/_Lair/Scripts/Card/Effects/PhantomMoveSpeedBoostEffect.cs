using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 팬텀 종 이동속도 글로벌 영구 버프 (×_speedMul).
    public class PhantomMoveSpeedBoostEffect : ICardEffect
    {
        private readonly float _speedMul;

        public PhantomMoveSpeedBoostEffect(CardParams p)
        {
            _speedMul = p.Get("speedMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Phantom, EMonsterStatKind.MoveSpeed, _speedMul);
    }
}
