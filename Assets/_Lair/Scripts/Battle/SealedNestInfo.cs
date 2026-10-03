using System;
using Lair.Data;

namespace Lair.Battle
{
    //# 봉인된 둥지 규칙 데이터 조회 순수 헬퍼 — 스포너 상태 UI 봉인 표기(해제 HP%)가 Battle 쪽에서 규칙 데이터를 읽을 때 쓴다. Unity 비의존.
    public static class SealedNestInfo
    {
        //# 이 규칙이 봉인된 둥지이고 species 를 봉인하면 해제 HP%(정수), 아니면 0.
        public static int UnlockHpPercent(StageRuleData data, EMonster species)
        {
            if (data.RuleId != EStageRule.SealedNest)
                return 0;
            if ((EMonster)(int)Math.Round(data.Params["lockedSpecies"]) != species)
                return 0;
            return (int)Math.Round(data.Params["unlockHpFraction"] * 100f);
        }
    }
}
