using System.Globalization;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 카드 리뉴얼 v0.6 Dps Tier2 (5장 임계) — Reaper+Hex Cooldown ×0.8 (=공속 +25%, 글로벌 영구).
    //# 기획서 §4.2 표·§10.3.
    public class DpsSynergyTier2 : IBuildSynergyTier
    {
        private const float CooldownMul = 0.8f;

        //# 스트링 204 = "사신·저주술사 공속 +{0}%". 쿨다운 배율 → 공속% 파생: (1/mul - 1)*100.
        public int DescriptionStringId => 204;
        public string[] DescriptionArgs
            => new[]
            {
                Mathf.RoundToInt((1f / CooldownMul - 1f) * 100f)
                    .ToString(CultureInfo.InvariantCulture),
            };

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, CooldownMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Hex,    EMonsterStatKind.Cooldown, CooldownMul);
        }
    }
}
