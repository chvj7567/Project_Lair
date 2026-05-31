using Lair.Card;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# 테스트용 CardData 생성기 — SerializedObject 우회.
    //# 카드 리뉴얼 v0.6 — 구 카테고리 → EBuildAxis (Phase 1 자리 치환).
    public static class FakeCardData
    {
        //# effect: null 이면 _effect 미설정. ApplyCardEffect 가 null 검사로 no-op 분기 들어감.
        public static CardData Create(
            ECardId id,
            EBuildAxis axis = EBuildAxis.Tank,
            ICardEffect effect = null)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            //# reflection 으로 private SerializeField 주입 (테스트 한정 허용)
            System.Type t = typeof(CardData);
            t.GetField("_id", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, id);
            t.GetField("_axis", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
                ?.SetValue(card, axis);
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
