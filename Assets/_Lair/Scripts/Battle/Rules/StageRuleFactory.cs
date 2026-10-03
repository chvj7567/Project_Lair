using System;
using Lair.Data;

namespace Lair.Battle
{
    //# Stage_Rule 데이터 row → IStageRule 구현체 (chapter-system §3.3). 파라미터는 StageRuleData.Params 에서만 읽는다.
    public static class StageRuleFactory
    {
        public static IStageRule Create(StageRuleData data)
        {
            StageRuleParams p = new StageRuleParams(data.Params);
            switch (data.RuleId)
            {
                case EStageRule.HolySpring:
                    return new HolySpringRule(p);
                case EStageRule.DenseFog:
                    return new DenseFogRule(p);
                case EStageRule.SealedNest:
                    return new SealedNestRule(p);
                case EStageRule.BossBulwark:
                    return new BossBulwarkRule(p);
                case EStageRule.ArrowRain:
                    return new ArrowRainRule(p);
                case EStageRule.HunterTraps:
                    return new HunterTrapsRule(p);
                default:
                    throw new ArgumentOutOfRangeException(nameof(data), data.RuleId, "구현되지 않은 스테이지 규칙");
            }
        }
    }
}
