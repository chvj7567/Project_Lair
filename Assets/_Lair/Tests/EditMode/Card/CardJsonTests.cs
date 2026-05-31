using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 Phase 2 Task 14 — cards.json 항목 수 + Multiply 자리 보존 + axis 키 회귀.
    //# W1 결정 (Berserk 패턴 적용): Multiply enum 자리(값 20) 보존 + SO 파일명 "Multiply.asset" 보존
    //# + 효과 클래스 SwarmRushEffect 교체. cards.json 의 "Multiply" id 도 보존됨.
    public class CardJsonTests
    {
        private const string CardsJsonPath = "Assets/_Lair/Data/Json/cards.json";

        [Test]
        public void cards_json_28장_Multiply_자리_보존()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", CardsJsonPath);
            Assert.IsTrue(File.Exists(fullPath), $"cards.json 부재: {fullPath}");

            string raw = File.ReadAllText(fullPath);
            JArray arr = JArray.Parse(raw);
            Assert.AreEqual(28, arr.Count, "카드 항목 28장 (Multiply 자리 보존, 신규 3장 추가)");

            //# W1: Multiply enum 자리 보존 정책에 따라 cards.json 의 "Multiply" id 도 살아있음.
            JToken multiply = arr.FirstOrDefault(t => (string)t["id"] == "Multiply");
            Assert.IsNotNull(multiply, "Multiply 항목은 W1 자리 보존 정책에 따라 cards.json 에 살아있어야 함");
            Assert.AreEqual("Swarm", (string)multiply["axis"], "Multiply 자리의 axis = Swarm (효과 SwarmRushEffect)");
        }

        [Test]
        public void cards_json_axis_키_사용_category_미사용()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", CardsJsonPath);
            string raw = File.ReadAllText(fullPath);
            JArray arr = JArray.Parse(raw);

            //# 모든 항목이 "axis" 키 보유 + "category" 키 미사용 — Phase 2 키명 마이그레이션 회귀.
            foreach (JToken t in arr)
            {
                Assert.IsNotNull(t["axis"], $"카드 {t["id"]} 에 axis 키 누락");
                Assert.IsNull(t["category"], $"카드 {t["id"]} 에 구 category 키 잔존");
            }
        }
    }
}
