using Lair.Card;
using Lair.Data;
using Lair.Tests.EditMode;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# 테스트용 CardData 생성기 — 전역 CardDatabase.Current 를 건드리지 않고 조회 값을 고정 주입한다(so-to-json-migration §7.4).
    //# CardData 는 비직렬화 캐시(_entry·_effect·_injected)를 리플렉션으로 채우면 DB 와 무관하게 이 값만 돌려준다.
    public static class FakeCardData
    {
        //# effect: null 이면 Effect 없음 — ApplyCardEffect 가 null 검사로 no-op 분기 들어감.
        public static CardData Create(
            ECardId id,
            EBuildAxis axis = EBuildAxis.Tank,
            ICardEffect effect = null,
            string displayName = null,
            string description = "")
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            CardEntry entry = new CardEntry
            {
                Id = id,
                Axis = axis,
                DisplayName = displayName ?? id.ToString(),
                Description = description,
            };
            TestReflection.SetField(card, "_id", id);
            TestReflection.SetField(card, "_entry", entry);
            TestReflection.SetField(card, "_effect", effect);
            TestReflection.SetField(card, "_injected", true);
            return card;
        }

        //# 출하 Card.json 행으로 채운 카드 — 실제 SO 인스턴스(공유 캐시)를 오염시키지 않도록 새 인스턴스에 주입한다.
        public static CardData FromShipped(ECardId id)
        {
            CardEntry entry = WorldMapTestSupport.ShippedCards().GetCard(id);
            return Create(id, entry.Axis, CardData.CreateEffect(entry), entry.DisplayName, entry.Description);
        }
    }
}
