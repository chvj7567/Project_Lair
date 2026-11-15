using System;
using System.Collections.Generic;
using System.Globalization;
using Lair.Battle;
using Lair.Data;

namespace Lair.Meta
{
    //# 육성 재화 표시 문구 조립 (monster-growth-currencies §8) — 문구 원문은 Strings_Ko, 수치는 GrowthDatabase. text 는 id → 문구(호출부 주입). Unity 비의존.
    public static class GrowthText
    {
        public static string CurrencyName(ECurrency currency, GrowthDatabase growth, Func<int, string> text)
        {
            CurrencyRow row = growth != null ? growth.GetCurrency(currency) : null;
            string name = row != null && text != null ? text(row.nameKey) : null;
            return string.IsNullOrEmpty(name) ? currency.ToString() : name;
        }

        //# 결과창·지갑 색 — 데이터가 없으면 흰색.
        public static string CurrencyColorHex(ECurrency currency, GrowthDatabase growth)
        {
            CurrencyRow row = growth != null ? growth.GetCurrency(currency) : null;
            return row != null ? row.colorHex : "#ffffff";
        }

        //# 잠긴 칸·교환 버튼 안내 — "{0} 클리어 시 해금" (인자 = 1-1·1-5·5-5). 콘텐츠 데이터가 없으면 빈 문자열.
        public static string UnlockText(ECurrency currency, Func<int, string> text)
        {
            int stage = CurrencyRules.UnlockClearedStage(currency);
            if (text == null || stage < 1 || stage == int.MaxValue)
                return string.Empty;
            return string.Format(text(GrowthStrings.UnlockAt), StageLabels.Number(stage));
        }

        //# 둥지 숙련 단계 하나의 효과 한 줄 — "첫 스폰 -2초" / "스폰 주기 -7%" / "스폰 보호 3초".
        public static string NestLevelText(int level, GrowthDatabase growth, Func<int, string> text)
        {
            if (growth == null || text == null || growth.TryGetNestEffect(level, out ENestMasteryEffect effect) == false)
                return string.Empty;
            switch (effect)
            {
                case ENestMasteryEffect.FirstSpawnAdvance:
                    return string.Format(text(GrowthStrings.NestAdvance), Num(growth.NestParam(level, "advanceSec")));
                case ENestMasteryEffect.SpawnPeriod:
                    return string.Format(text(GrowthStrings.NestPeriod), Num((1f - growth.NestParam(level, "periodMul")) * 100f));
                default:
                    return string.Format(text(GrowthStrings.NestGuard), Num(growth.NestParam(level, "durationSec")));
            }
        }

        //# 둥지 칸 문구 — 다음 단계 효과, 만렙이면 누적 효과 전체(" · " 구분).
        public static string NestSlotText(int currentLevel, GrowthDatabase growth, Func<int, string> text)
        {
            if (growth == null || growth.HasNest == false)
                return string.Empty;
            if (currentLevel < growth.NestMaxLevel)
                return NestLevelText(currentLevel + 1, growth, text);
            List<string> parts = new List<string>();
            for (int lv = 1; lv <= growth.NestMaxLevel; ++lv)
            {
                parts.Add(NestLevelText(lv, growth, text));
            }
            return string.Join("\n", parts);
        }

        //# 특성 칸 문구 — 특성 이름 + 대표 수치 (Lv0 = 다음 값, 중간 = 현재→다음, 만렙 = 현재). 예 "가시 껍질 10→17%".
        public static string TraitSlotText(EMonster monster, int currentTier, GrowthDatabase growth, Func<int, string> text)
        {
            MonsterTraitRow row = growth != null ? growth.TraitOf(monster) : null;
            if (row == null || text == null || growth.TryGetTraitKind(monster, out EMonsterTrait trait) == false)
                return string.Empty;
            string name = text(row.nameKey);
            string unit = TraitUnit(trait);
            int max = GrowthDatabase.TraitMaxTier;
            if (currentTier <= 0)
                return $"{name} {TraitValue(monster, trait, 1, growth)}{unit}";
            if (currentTier >= max)
                return $"{name} {TraitValue(monster, trait, max, growth)}{unit}";
            return $"{name} {TraitValue(monster, trait, currentTier, growth)}→{TraitValue(monster, trait, currentTier + 1, growth)}{unit}";
        }

        //# 특성 설명 한 줄 (1489~1494) — 그 단계 Param 값을 포맷 인자로.
        public static string TraitDescText(EMonster monster, int tier, GrowthDatabase growth, Func<int, string> text)
        {
            MonsterTraitRow row = growth != null ? growth.TraitOf(monster) : null;
            if (row == null || text == null || growth.TryGetTraitKind(monster, out EMonsterTrait trait) == false)
                return string.Empty;
            float P(string key) => growth.TraitParam(monster, tier, key);
            string format = text(row.descKey);
            switch (trait)
            {
                case EMonsterTrait.ThornShell: return string.Format(format, Num(P("reflectRatio") * 100f));
                case EMonsterTrait.Grudge: return string.Format(format, Num((P("powerMul") - 1f) * 100f), Num(P("durationSec")));
                case EMonsterTrait.Execute: return string.Format(format, Num(P("hpThreshold") * 100f), Num((P("damageMul") - 1f) * 100f));
                case EMonsterTrait.CurseStack: return string.Format(format, Num(P("perStack") * 100f), Num(P("maxStack")));
                case EMonsterTrait.Contagion: return string.Format(format, Num(P("addStacks")));
                default: return string.Format(format, Num(P("dodgeCount")), Num((P("hasteMul") - 1f) * 100f));
            }
        }

        private static string TraitValue(EMonster monster, EMonsterTrait trait, int tier, GrowthDatabase growth)
        {
            float P(string key) => growth.TraitParam(monster, tier, key);
            switch (trait)
            {
                case EMonsterTrait.ThornShell: return Num(P("reflectRatio") * 100f);
                case EMonsterTrait.Grudge: return Num((P("powerMul") - 1f) * 100f);
                case EMonsterTrait.Execute: return Num((P("damageMul") - 1f) * 100f);
                case EMonsterTrait.CurseStack: return Num(P("perStack") * 100f);
                case EMonsterTrait.Contagion: return Num(P("addStacks"));
                default: return Num(P("dodgeCount"));
            }
        }

        private static string TraitUnit(EMonsterTrait trait)
        {
            switch (trait)
            {
                case EMonsterTrait.Contagion: return "중첩";
                case EMonsterTrait.Afterimage: return "회";
                default: return "%";
            }
        }

        //# 정수면 정수, 아니면 소수 한 자리 — 반올림 오차(0.93 → 7.0000005) 제거.
        private static string Num(float value)
        {
            double rounded = Math.Round(value, 1);
            return rounded.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
