using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lair.Card;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# 독장판 삭제 · 끈질긴 저주 신설 카드 무결성 (monster-growth-currencies §15.3) — 총수 28장 집계는 CardRenewalConsistencyTests 가 맡고 여기선 구성을 본다.
    public class GrowthCardIntegrityTests
    {
        private const string Dir = "Assets/_Lair/Data/Json/";
        private const string PassivePool = "Assets/_Lair/Art/Cards/CardPool_Passive.asset";
        private const string ActivePool = "Assets/_Lair/Art/Cards/CardPool_Active.asset";

        private static JArray Cards() => JArray.Parse(File.ReadAllText(Dir + "cards.json"));

        private static List<CardData> PoolCards()
        {
            List<CardData> all = new List<CardData>();
            all.AddRange(AssetDatabase.LoadAssetAtPath<CardPool>(PassivePool).Cards);
            all.AddRange(AssetDatabase.LoadAssetAtPath<CardPool>(ActivePool).Cards);
            return all;
        }

        [Test]
        public void cards_json_은_축마다_7장이고_Debuff_축은_끈질긴_저주를_포함하고_독장판이_없다()
        {
            JArray cards = Cards();
            Dictionary<string, List<string>> byAxis = cards
                .GroupBy(c => (string)c["axis"])
                .ToDictionary(g => g.Key, g => g.Select(c => (string)c["id"]).ToList());

            foreach (string axis in new[] { "Tank", "Dps", "Debuff", "Swarm" })
            {
                Assert.AreEqual(7, byAxis[axis].Count, axis);
            }
            CollectionAssert.Contains(byAxis["Debuff"], "LingeringCurse");
            CollectionAssert.DoesNotContain(cards.Select(c => (string)c["id"]).ToList(), "HeroPoisonAura");
        }

        [Test]
        public void 카드_풀_JSON과_SO_풀_어디에도_독장판이_없고_끈질긴_저주는_패시브에_있다()
        {
            JObject pools = JObject.Parse(File.ReadAllText(Dir + "card_pools.json"));
            List<string> passive = pools["passive"].Select(t => (string)t).ToList();
            List<string> active = pools["active"].Select(t => (string)t).ToList();
            CardPool passiveSo = AssetDatabase.LoadAssetAtPath<CardPool>(PassivePool);

            CollectionAssert.Contains(passive, "LingeringCurse");
            CollectionAssert.DoesNotContain(passive, "HeroPoisonAura");
            CollectionAssert.DoesNotContain(active, "HeroPoisonAura");
            Assert.IsTrue(passiveSo.Cards.Any(c => c != null && c.Id == ECardId.LingeringCurse));
            Assert.IsFalse(PoolCards().Any(c => c != null && c.Id == ECardId.HeroPoisonAura));
        }

        [Test]
        public void Debuff_축_SO_카드는_7장이고_패시브_4장_액티브_3장이다()
        {
            CardPool passive = AssetDatabase.LoadAssetAtPath<CardPool>(PassivePool);
            CardPool active = AssetDatabase.LoadAssetAtPath<CardPool>(ActivePool);

            Assert.AreEqual(4, passive.Cards.Count(c => c != null && c.Axis == EBuildAxis.Debuff), "역병의 손길·역병 증식·끈질긴 저주·약화의 저주");
            Assert.AreEqual(3, active.Cards.Count(c => c != null && c.Axis == EBuildAxis.Debuff), "공포·출혈·무력화");
        }

        [Test]
        public void 독장판_카드_SO_와_효과_클래스는_삭제되고_enum_자리만_남는다()
        {
            Assert.IsNull(AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Lair/Art/Cards/Items/HeroPoisonAura.asset"));
            Assert.IsNull(typeof(CardData).Assembly.GetType("Lair.Card.HeroPoisonAuraEffect"));
            Assert.IsNull(typeof(CardData).Assembly.GetType("Lair.Card.PoisonAura"));
            Assert.AreEqual(13, (int)ECardId.HeroPoisonAura, "정수 자리 보존");
            Assert.AreEqual(28, (int)ECardId.LingeringCurse, "맨 끝 append");
        }

        [Test]
        public void 끈질긴_저주는_편성_카드_종족_매핑에_없어_어떤_편성에서도_후보에서_빠지지_않는다()
        {
            JArray species = JArray.Parse(File.ReadAllText(Dir + "Card_Species.json"));
            List<string> ids = species.Select(r => (string)r["cardId"]).ToList();

            CollectionAssert.DoesNotContain(ids, "LingeringCurse");
            CollectionAssert.DoesNotContain(ids, "HeroPoisonAura");
            CollectionAssert.Contains(ids, "PlagueSlowBoost", "역병의 손길은 역병귀 편성 카드");
        }

        [Test]
        public void 끈질긴_저주_카드는_자기_일러스트를_가지고_약화의_저주_일러스트를_빌리지_않는다()
        {
            CardData lingering = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Lair/Art/Cards/Items/LingeringCurse.asset");
            CardData attackDown = AssetDatabase.LoadAssetAtPath<CardData>("Assets/_Lair/Art/Cards/Items/HeroAttackDown.asset");
            Assert.IsNotNull(lingering);
            Assert.IsNotNull(attackDown);

            Assert.AreEqual(ECardId.LingeringCurse, lingering.Id);
            Assert.IsNotNull(lingering.CardImage);
            Assert.IsNotNull(lingering.Icon);
            Assert.AreNotEqual(attackDown.CardImage, lingering.CardImage, "도감에서 두 카드가 같아 보이면 안 됨 (§15.3)");
        }

        [Test]
        public void 역병의_손길은_ID_를_유지한_채_독_피해_x1점4_효과로_바뀌었다()
        {
            JToken card = Cards().First(c => (string)c["id"] == "PlagueSlowBoost");

            Assert.AreEqual("Debuff", (string)card["axis"]);
            Assert.AreEqual("PlagueToxinBoostEffect", (string)card["effect"]["$type"]);
            Assert.AreEqual(1.4f, (float)card["effect"]["toxinMul"], 0.0001f);
        }
    }
}
