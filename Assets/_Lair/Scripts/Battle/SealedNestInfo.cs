using System;
using Lair.Data;

namespace Lair.Battle
{
    //# 봉인된 둥지 규칙 데이터 조회 순수 헬퍼 — 스포너 상태 UI 봉인 표기(해제 HP%)가 Battle 쪽에서 규칙 데이터를 읽을 때 쓴다. Unity 비의존.
    public static class SealedNestInfo
    {
        //# 이 규칙이 봉인된 둥지면 해제 HP%(정수), 아니면 0 — 봉인 종과 무관(대체 규칙으로 다른 종 둥지가 봉인될 수 있어 셀은 실제 봉인 여부와 함께 쓴다).
        public static int AnyUnlockHpPercent(StageRuleData data)
        {
            if (data.RuleId != EStageRule.SealedNest)
                return 0;
            return (int)Math.Round(data.Params["unlockHpFraction"] * 100f);
        }

        //# 봉인 안내 팝업 본문 — {0}=종족 이름, {1}=해제 HP%, {2}=현재 영웅 HP%(올림), {3}=남은 %p(최소 1).
        public static string FormatNestPopupBody(string template, string speciesName, int unlockPercent, float heroHpRatio)
        {
            //# 부동소수 오차(0.56f*100 = 56.0000002)로 올림이 1 튀지 않게 작은 보정을 뺀다.
            int current = (int)Math.Ceiling((double)heroHpRatio * 100.0 - 1e-6);
            int remain = Math.Max(1, current - unlockPercent);
            return string.Format(template, speciesName, unlockPercent, current, remain);
        }

        //# 이 규칙이 봉인된 둥지이고 species 를 봉인하면 해제 HP%(정수), 아니면 0.
        public static int UnlockHpPercent(StageRuleData data, EMonster species)
        {
            if (data.RuleId != EStageRule.SealedNest)
                return 0;
            if (MonsterGrades.SameSpecies((EMonster)(int)Math.Round(data.Params["lockedSpecies"]), species) == false)
                return 0;
            return (int)Math.Round(data.Params["unlockHpFraction"] * 100f);
        }
    }
}
