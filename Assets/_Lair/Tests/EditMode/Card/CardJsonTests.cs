using System.IO;
using Lair.Data;
using Lair.Tests.EditMode;
using NUnit.Framework;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 2 Task 14 — Card.json 항목 수 + Multiply 자리 보존 + axis 키 회귀 (출하 Card.json 기준).
    //# W1 결정 (Berserk 패턴 적용): Multiply enum 자리(값 20) 보존 + SO 파일명 "Multiply.asset" 보존
    public class CardJsonTests
    {
        [Test]
        public void Card_json_28장_Multiply_자리_보존()
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();
            Assert.AreEqual(28, db.Cards.Count, "카드 항목 28장 (Multiply 자리 보존, 신규 3장 추가)");

            //# W1: Multiply enum 자리 보존 정책에 따라 Card.json 의 "Multiply" 행도 살아있음.
            CardEntry multiply = db.GetCard(ECardId.Multiply);
            Assert.IsNotNull(multiply, "Multiply 행은 W1 자리 보존 정책에 따라 Card.json 에 살아있어야 함");
            Assert.AreEqual(EBuildAxis.Swarm, multiply.Axis, "Multiply 자리의 axis = Swarm (효과 FastBreeding)");
        }

        [Test]
        public void Card_json_axis_키_사용_category_미사용()
        {
            //# 모든 행이 axis 값명을 가진다(로드 검증이 보장) + 구 "category" 키가 남아 있지 않다 — Phase 2 키명 마이그레이션 회귀.
            string raw = File.ReadAllText(WorldMapTestSupport.JsonDir + "Card.json");
            StringAssert.DoesNotContain("\"category\"", raw, "Card.json 에 구 category 키 잔존");
            foreach (CardRow row in WorldMapTestSupport.ShippedCardData().Cards)
            {
                Assert.IsFalse(string.IsNullOrEmpty(row.axis), $"카드 {row.cardId} 에 axis 누락");
            }
        }
    }
}
