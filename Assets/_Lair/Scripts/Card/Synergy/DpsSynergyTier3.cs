using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier3 (7장 임계) — Reaper+Hex Range ×1.3 (글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier3 : IBuildSynergyTier
    {
        private const float RangeMul = 1.3f;

        //# 스트링 205 = "사신·저주술사 사거리 ×{0}".
        public int DescriptionStringId => 205;
        public string[] DescriptionArgs
            => new[] { RangeMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Range, RangeMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Range, RangeMul);
        }
    }
}
