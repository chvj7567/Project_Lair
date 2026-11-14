using System;
using System.Collections.Generic;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# 카드 효과 테스트 조립 — 효과 클래스는 JSON 조립 결과(CardParams)로만 만들어진다(so-to-json-migration §4.9).
    //# 이 키트는 전환 전 코드 기본값을 명시 수치로 그대로 넘겨 기존 테스트의 기대값을 유지한다(이 표는 출하 Card_Param.json 과 같은 값이며, 출하 값의 단일 출처는 JSON 이다).
    public static class CardEffectTestKit
    {
        private static readonly Dictionary<ECardEffect, (string key, float value)[]> Defaults = new Dictionary<ECardEffect, (string key, float value)[]>
        {
            { ECardEffect.WispHpBoost, new[] { ("hpMul", 1.5f) } },
            { ECardEffect.WraithDamageBoost, new[] { ("hpMul", 1.5f) } },
            { ECardEffect.ReaperAtkSpeed, new[] { ("cdMul", 0.7f) } },
            { ECardEffect.HexRangeBoost, new[] { ("rangeMul", 1.4f) } },
            { ECardEffect.PlagueToxinBoost, new[] { ("toxinMul", 1.4f) } },
            { ECardEffect.PhantomMoveSpeedBoost, new[] { ("speedMul", 1.5f) } },
            { ECardEffect.WispWraithPowerBoost, new[] { ("powerMul", 1.3f) } },
            { ECardEffect.ReaperHexPowerBoost, new[] { ("powerMul", 1.3f) } },
            { ECardEffect.Fear, new[] { ("duration", 3f) } },
            { ECardEffect.Bleed, new[] { ("ratio", 0.02f), ("duration", 10f) } },
            { ECardEffect.Weaken, new[] { ("factor", 0.5f), ("duration", 10f) } },
            { ECardEffect.Slow, new[] { ("heroFactor", 0.5f), ("monsterMul", 1.3f), ("duration", 10f) } },
            { ECardEffect.Frenzy, new[] { ("duration", 10f) } },
            { ECardEffect.FastBreeding, new[] { ("periodMul", 0.6f) } },
            { ECardEffect.BloodThirst, new[] { ("duration", 30f) } },
            { ECardEffect.IronWill, new[] { ("duration", 15f) } },
            { ECardEffect.TimeStop, new[] { ("duration", 5f) } },
            { ECardEffect.GuardianRage, new[] { ("duration", 15f) } },
            { ECardEffect.MarkOfDeath, new[] { ("dmgTakenMul", 1.5f), ("duration", 5f) } },
            { ECardEffect.SpawnerHaste, new[] { ("periodMul", 0.8f) } },
            { ECardEffect.LingeringCurse, new[] { ("durationBonusPerPick", 0.25f) } },
        };

        //# 클래스 이름(XxxEffect)에서 효과를 정해 기본 수치로 만든다. 수치를 바꿔야 하는 테스트는 CardParams 를 직접 만들어 생성자에 넘긴다.
        public static T Make<T>() where T : class, ICardEffect
        {
            ECardEffect kind = (ECardEffect)Enum.Parse(typeof(ECardEffect), typeof(T).Name.Replace("Effect", string.Empty));
            return (T)CardData.CreateEffect(DefaultEntry(kind));
        }

        public static CardEntry DefaultEntry(ECardEffect kind)
        {
            Dictionary<string, float> values = new Dictionary<string, float>();
            if (Defaults.TryGetValue(kind, out (string key, float value)[] pairs))
            {
                foreach ((string key, float value) pair in pairs)
                {
                    values[pair.key] = pair.value;
                }
            }
            return new CardEntry { Effect = kind, DisplayName = string.Empty, Description = string.Empty, Params = new CardParams(values) };
        }
    }
}
