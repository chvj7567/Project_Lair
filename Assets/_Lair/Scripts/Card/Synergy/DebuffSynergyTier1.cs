using System.Globalization;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Debuff Tier1 (3장 임계) — Plague SlowFactor ×0.8 (강한 둔화 추가, 글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DebuffSynergyTier1 : IBuildSynergyTier
    {
        private const float SlowMul = 0.8f;

        //# 스트링 206 = "역병귀 둔화 ×{0}".
        public int DescriptionStringId => 206;
        public string[] DescriptionArgs
            => new[] { SlowMul.ToString("0.##", CultureInfo.InvariantCulture) };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, SlowMul);
        }
    }
}
