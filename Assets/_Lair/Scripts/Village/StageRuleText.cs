using System;
using System.Globalization;
using Lair.Battle;
using Lair.Data;

namespace Lair.Village
{
    //# 스테이지 규칙 → 월드맵 표시 문구 (chapter-system §3.4 설명 문구 1221~1226, 규칙명 1201~1206). 파라미터는 StageRuleData 에서만 읽는다.
    //# Unity 비의존 — text 는 Strings_Ko id → 문구 조회 함수(테스트는 Fake 주입).
    public static class StageRuleText
    {
        private const int NameBase = 1200;   //# 1201 성수 샘 ~ 1206 사냥꾼의 덫 (EStageRule 순서 + 1)
        private const int DescriptionBase = 1220;   //# 1221 ~ 1226

        public static string Name(EStageRule rule, Func<int, string> text)
        {
            return text(NameBase + (int)rule + 1);
        }

        //# "규칙명\n설명" 한 항목. 보스 페이즈(HasTrigger)는 설명 문구가 "영웅 HP {x}% 이하에서 ..." 로 시작한다.
        public static string Entry(StageRuleData data, Func<int, string> text)
        {
            return Name(data.RuleId, text) + "\n" + Description(data, text);
        }

        public static string Description(StageRuleData data, Func<int, string> text)
        {
            string format = text(DescriptionBase + (int)data.RuleId + 1);
            float triggerPercent = data.TriggerHpFraction * 100f;
            switch (data.RuleId)
            {
                case EStageRule.HolySpring:
                    return string.Format(format, N(P(data, "periodSec")), N(P(data, "activeSec")), N(P(data, "healPercentPerSec")));
                case EStageRule.DenseFog:
                    return string.Format(format, N(P(data, "monsterRangeMul")), N(P(data, "minRange")));
                case EStageRule.SealedNest:
                    return string.Format(format, SpeciesVisual.SpeciesName((EMonster)(int)Math.Round(P(data, "lockedSpecies"))), N(P(data, "unlockHpFraction") * 100f));
                case EStageRule.BossBulwark:
                    return string.Format(format, N(triggerPercent), N(P(data, "durationSec")), N(P(data, "damageTakenMul")));
                case EStageRule.ArrowRain:
                    return string.Format(format, N(triggerPercent), N(P(data, "intervalSec")), N(P(data, "tickCount")), N(P(data, "damagePerTick")));
                case EStageRule.HunterTraps:
                    return string.Format(format, N(triggerPercent), N(P(data, "trapCount")), N(P(data, "monsterSpeedMul")), N(P(data, "damagePerSec")));
                default:
                    return string.Empty;
            }
        }

        //# 봉인된 둥지의 해제 HP% (정수). 봉인 규칙이 아니거나 종이 다르면 0. 변환 본체는 Battle.SealedNestInfo(스포너 UI 가 Village 를 참조하지 않게).
        public static int SealUnlockHpPercent(StageRuleData data, EMonster species)
        {
            return SealedNestInfo.UnlockHpPercent(data, species);
        }

        private static float P(StageRuleData data, string key)
        {
            return data.Params[key];
        }

        //# 숫자 표기 — 소수 2자리까지, 불필요한 0 제거(0.70 → "0.7", 55.000004 → "55").
        private static string N(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
