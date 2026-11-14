using Lair.Data;

namespace Lair.Card
{
    //# 지속 스폰 — 리퍼 종 공격 쿨다운 글로벌 영구 버프 (×_cdMul, 낮을수록 빠른 공격).
    public class ReaperAtkSpeedEffect : ICardEffect
    {
        private readonly float _cdMul;

        public ReaperAtkSpeedEffect(CardParams p)
        {
            _cdMul = p.Get("cdMul");
        }

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, _cdMul);
    }
}
