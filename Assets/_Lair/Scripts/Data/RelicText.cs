using System;
using System.Globalization;

namespace Lair.Data
{
    //# 유물 효과 설명문 조립 (relics §7.2) — descKey 문구의 {0}/{1} 에 중첩 stack 의 실효값을 채운다. Unity 비의존 순수 함수.
    //# 문구 인자는 "주 효과(첫 효과)" 기준이다(KingsCrownShard 는 HP·공격력이 같은 값이라 하나로 충분).
    public static class RelicText
    {
        //# 보유(1 이상)/미보유(1 로 간주) 중첩 stack 의 설명문. 문구가 없거나 포맷 인자가 안 맞으면 원문을 그대로 반환.
        public static string Describe(RelicDatabase db, RelicData data, int stack, Func<int, string> text)
        {
            if (db == null || data == null || text == null || data.Effects.Count == 0)
                return string.Empty;
            string format = text(data.DescKey);
            if (string.IsNullOrEmpty(format))
                return string.Empty;
            object[] args = BuildArgs(db, data.Effects[0], Math.Max(1, stack));
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                return format;
            }
        }

        //# 효과 종류별 문구 인자 — 퍼센트류는 절댓값 x 100, 정수류는 정수 문자열.
        public static object[] BuildArgs(RelicDatabase db, RelicEffectData effect, int stack)
        {
            switch (effect.Kind)
            {
                case ERelicEffectKind.MonsterStat:
                case ERelicEffectKind.SpawnerPeriod:
                case ERelicEffectKind.HeroAttackDown:
                case ERelicEffectKind.SoulBonus:
                case ERelicEffectKind.XpBonus:
                case ERelicEffectKind.LoseSoulBonus:
                    return new object[] { Number(Math.Abs(db.ScaledParam(effect, "amount", stack)) * 100f) };
                case ERelicEffectKind.HeroBurn:
                    return new object[] { Number(db.ScaledParam(effect, "hpRatioPerSec", stack) * 100f) };
                case ERelicEffectKind.OpeningSummon:
                    return new object[] { Number(db.ScaledParam(effect, RelicDatabase.CountPerStackKey, stack)) };
                case ERelicEffectKind.HeroLightning:
                    return new object[] { Number(db.ScaledParam(effect, "intervalSec", stack)), Number(db.ScaledParam(effect, "hpRatio", stack) * 100f) };
                case ERelicEffectKind.LowHpFrenzy:
                    return new object[] { Number(db.ScaledParam(effect, "hpFraction", stack) * 100f), Number(db.ScaledParam(effect, "durationSec", stack)) };
                case ERelicEffectKind.BattleSpeed:
                    return new object[] { Number(db.ScaledParam(effect, "speedScale", stack)) };
                case ERelicEffectKind.SpawnPrelude:
                    return new object[] { Number(db.ScaledParam(effect, "advanceSec", stack)) };
                default:
                    return Array.Empty<object>();
            }
        }

        //# 소수 셋째 자리까지(불필요한 0 제거) — 0.056 / 8.4 / 52.5 / 3 형태. 부동소수 잔여 오차는 반올림으로 제거.
        public static string Number(float value)
        {
            return Math.Round((double)value, 3).ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}
