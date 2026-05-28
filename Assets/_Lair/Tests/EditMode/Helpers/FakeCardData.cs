using Lair.Card;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# 테스트용 CardData 생성기 — SerializedObject 우회.
    public static class FakeCardData
    {
        //# effect: null 이면 _effect 미설정. ApplyCardEffect 가 null 검사로 no-op 분기 들어감.
        public static CardData Create(
            ECardId id,
            ECardCategory category = ECardCategory.Enhance,
            ICardEffect effect = null)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            //# reflection 으로 private SerializeField 주입 (테스트 한정 허용)
            System.Type t = typeof(CardData);
            t.GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, id);
            t.GetField("_category", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, category);
            t.GetField("_displayName", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, id.ToString());
            if (effect != null)
            {
                t.GetField("_effect", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                    ?.SetValue(card, effect);
            }
            return card;
        }
    }
}
