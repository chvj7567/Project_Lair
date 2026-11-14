using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 헥스 종 사거리 글로벌 영구 버프 (×_rangeMul).
    public class HexRangeBoostEffect : ICardEffect
    {
        private readonly float _rangeMul;

        public HexRangeBoostEffect(CardParams p)
        {
            _rangeMul = p.Get("rangeMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Hex, EMonsterStatKind.Range, _rangeMul);
    }
}
