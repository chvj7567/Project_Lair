using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.EditMode
{
    //# 출하 데이터 회귀 (so-to-json-migration §10) — 카드 JSON 2종이 로드 검증(SJ3~SJ5)을 통과하고 에셋 키 규약(SJ6)을 지킨다.
    public class CardShippedDataTests
    {
        private const string GroupDir = "Assets/AddressableAssetsData/AssetGroups";
        private static readonly EData[] Keys = { EData.Card, EData.Card_Param };

        [Test]
        public void 출하_카드_JSON은_검증을_통과하고_행_수가_기획서와_같다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            Assert.AreEqual(28, data.Cards.Length, "Card");
            Assert.AreEqual(26, data.Params.Length, "Card_Param");

            CardDatabase db = WorldMapTestSupport.BuildCards(data);

            int params26 = 0;
            int noParam = 0;
            foreach (CardEntry entry in db.Cards)
            {
                params26 += entry.Params.Values.Count;
                if (entry.Params.Values.Count == 0)
                {
                    noParam++;
                }
            }
            Assert.AreEqual(26, params26);
            Assert.AreEqual(7, noParam, "파라미터 없는 카드");
            Assert.AreEqual(7, CountAxis(db, EBuildAxis.Tank));
            Assert.AreEqual(7, CountAxis(db, EBuildAxis.Dps));
            Assert.AreEqual(7, CountAxis(db, EBuildAxis.Debuff));
            Assert.AreEqual(7, CountAxis(db, EBuildAxis.Swarm));
        }

        private static int CountAxis(CardDatabase db, EBuildAxis axis)
        {
            int count = 0;
            foreach (CardEntry entry in db.Cards)
            {
                if (entry.Axis == axis)
                {
                    count++;
                }
            }
            return count;
        }

        [Test]
        public void Strings_Ko_1632에서_1687은_56개가_연속_존재하고_비어있지_않다()
        {
            for (int id = 1632; id <= 1687; id++)
            {
                Assert.IsTrue(WorldMapTestSupport.Strings().ContainsKey(id), $"id {id} 없음");
                Assert.IsNotEmpty(WorldMapTestSupport.Text(id), $"id {id} 비어 있음");
            }
        }

        //# 카드 id 와 효과 이름이 다른 6장 — 세이브 문자열(ECardId)은 옛 이름을 유지하고 효과는 새 이름이다.
        [Test]
        public void 카드_id_와_효과_이름이_다른_6장은_기획서_매핑과_같다()
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();
            Dictionary<ECardId, ECardEffect> renamed = new Dictionary<ECardId, ECardEffect>
            {
                { ECardId.PlagueSlowBoost, ECardEffect.PlagueToxinBoost },
                { ECardId.ReplaceWispsToWraith, ECardEffect.WispWraithPowerBoost },
                { ECardId.ReplaceReapersToHex, ECardEffect.ReaperHexPowerBoost },
                { ECardId.Multiply, ECardEffect.FastBreeding },
                { ECardId.Berserk, ECardEffect.GuardianRage },
                { ECardId.WallOfWisps, ECardEffect.ToughHide },
            };
            int different = 0;
            foreach (CardEntry entry in db.Cards)
            {
                if (entry.Id.ToString() != entry.Effect.ToString())
                {
                    different++;
                    Assert.AreEqual(renamed[entry.Id], entry.Effect, entry.Id.ToString());
                }
            }
            Assert.AreEqual(6, different);
        }

        [Test]
        public void 수치_누락_초과_0_채움을_모아_던진다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            List<Card_ParamRow> rows = new List<Card_ParamRow>(data.Params);
            rows.RemoveAll(row => row.cardId == "Bleed" && row.paramKey == "ratio");
            rows.Add(new Card_ParamRow { cardId = "Fear", paramKey = "bogus", value = 1f });
            rows.Find(row => row.cardId == "WispHpBoost" && row.paramKey == "hpMul").value = 0f;
            data.Params = rows.ToArray();
            data.Cards[1].nameKey = 0;

            ChapterDataException e = Assert.Throws<ChapterDataException>(() => WorldMapTestSupport.BuildCards(data));

            StringAssert.Contains("Card_Param.json 에 Bleed/ratio 가 없음", e.Message);
            StringAssert.Contains("Card_Param.json Fear/bogus 는 Fear 효과의 수치가 아님", e.Message);
            StringAssert.Contains("Card_Param[WispHpBoost/hpMul].value", e.Message);
            StringAssert.Contains("Card[WraithDamageBoost] nameKey 0", e.Message);
        }

        //# SJ5 — Card.json ↔ 카드 풀 SO 두 개의 카드가 양방향으로 같다. 런타임은 이 대조를 하지 않고(§6.4) 여기서만 한다.
        [Test]
        public void 출하_카드_풀과_Card_JSON은_양방향으로_일치한다()
        {
            HashSet<ECardId> pool = new HashSet<ECardId>(WorldMapTestSupport.ShippedPoolCardIds());
            HashSet<ECardId> json = new HashSet<ECardId>();
            foreach (CardEntry entry in WorldMapTestSupport.ShippedCards().Cards)
            {
                json.Add(entry.Id);
            }

            Assert.AreEqual(28, pool.Count, "풀 카드 수(중복 없음)");
            Assert.IsTrue(pool.SetEquals(json), "풀 ↔ Card.json 카드 집합");
            Assert.IsFalse(pool.Contains(ECardId.HeroPoisonAura), "폐기 자리는 풀에 없다");
        }

        //# SJ4 — Card_Species.cardId 는 부모 Card 가 실재하므로 Card.json 에 있어야 한다. 런타임은 이 FK 를 대조하지 않는다(편성 묶음 소관).
        [Test]
        public void 출하_Card_Species의_cardId는_모두_Card_JSON에_있다()
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();

            foreach (Card_SpeciesRow row in WorldMapTestSupport.ShippedCardSpecies())
            {
                Assert.IsTrue(System.Enum.TryParse(row.cardId, out ECardId id), row.cardId);
                Assert.IsNotNull(db.GetCard(id), $"Card_Species cardId {row.cardId} 가 Card.json 에 없음");
            }
        }

        [Test]
        public void 카드_JSON_2종은_EData_값명_파일로_Addressables에_Resource_라벨로_한_번만_등록돼_있다()
        {
            string groups = string.Empty;
            foreach (string path in Directory.GetFiles(GroupDir, "*.asset"))
            {
                groups += File.ReadAllText(path).Replace("\r\n", "\n") + "\n";
            }

            foreach (EData key in Keys)
            {
                string jsonPath = WorldMapTestSupport.JsonDir + key + ".json";
                Assert.IsTrue(File.Exists(jsonPath), jsonPath);
                string entry = "  - m_GUID: " + AssetDatabase.AssetPathToGUID(jsonPath) + "\n    m_Address: " + key
                    + "\n    m_ReadOnly: 0\n    m_SerializedLabels:\n    - Resource\n";
                StringAssert.Contains(entry, groups, key.ToString());
                Assert.AreEqual(1, Regex.Matches(groups, "m_Address: " + key + "\n").Count, key + " 주소 중복");
            }
        }
    }
}
