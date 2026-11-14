using System.Collections.Generic;
using System.Reflection;
using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SJ1 일회용 패리티 (so-to-json-migration §10) — CardData SO 28장(이전 전 값)과 Card·Card_Param JSON 조립 결과를 전 필드·순서까지 비교한다.
    //# 커밋 ② 에서 SO 필드와 함께 이 파일을 삭제한다. 효과 수치 키는 효과 클래스의 [SerializeField] 필드명(앞 '_' 제외)이다.
    public class CardJsonParityTests
    {
        private const string CardDir = "Assets/_Lair/Art/Cards/";

        private CardDatabase _json;

        [SetUp]
        public void SetUp()
        {
            _json = WorldMapTestSupport.ShippedCards();
        }

        private static List<CardData> PoolCards(string pool)
        {
            CardPool asset = AssetDatabase.LoadAssetAtPath<CardPool>(CardDir + pool + ".asset");
            Assert.IsNotNull(asset, pool);
            return new List<CardData>(asset.Cards);
        }

        private static List<CardData> AllCards()
        {
            List<CardData> all = PoolCards("CardPool_Passive");
            all.AddRange(PoolCards("CardPool_Active"));
            return all;
        }

        //# 효과 클래스가 선언한 [SerializeField] 수치 필드.
        private static List<FieldInfo> TuningFields(ICardEffect effect)
        {
            List<FieldInfo> result = new List<FieldInfo>();
            foreach (FieldInfo field in effect.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            {
                if (field.GetCustomAttribute<SerializeField>() != null)
                {
                    result.Add(field);
                }
            }
            return result;
        }

        [Test]
        public void 카드_28장은_id_축_이름_설명_효과_종류가_같다()
        {
            List<CardData> all = AllCards();
            Assert.AreEqual(28, all.Count, "풀의 카드 수");
            Assert.AreEqual(28, _json.Cards.Count, "JSON 카드 수");
            foreach (CardData card in all)
            {
                CardEntry entry = _json.GetCard(card.Id);
                string at = card.Id.ToString();
                Assert.IsNotNull(entry, at);
                Assert.AreEqual(card.Axis, entry.Axis, at + " axis");
                Assert.AreEqual(card.DisplayName, entry.DisplayName, at + " DisplayName");
                Assert.AreEqual(card.Description, entry.Description, at + " Description");
                Assert.IsNotNull(card.Effect, at + " Effect");
                Assert.AreEqual(card.Effect.GetType().Name, entry.Effect + "Effect", at + " effectKind");
            }
        }

        //# 행 순서는 ECardId 정수값 순이다(폐기 자리 HeroPoisonAura 는 행이 없다).
        [Test]
        public void 카드_행_순서는_ECardId_정수값_순이다()
        {
            int previous = -1;
            foreach (CardEntry entry in _json.Cards)
            {
                Assert.Greater((int)entry.Id, previous, entry.Id.ToString());
                previous = (int)entry.Id;
            }
            Assert.IsNull(_json.GetCard(ECardId.HeroPoisonAura));
        }

        //# 필드 집합이 키 집합과 양방향으로 같아야 누락·초과가 없고, 실수 값은 오차 0 으로 비교한다.
        [Test]
        public void 효과_수치_26개는_필드_집합과_값이_모두_같다()
        {
            int total = 0;
            foreach (CardData card in AllCards())
            {
                List<FieldInfo> fields = TuningFields(card.Effect);
                CardParams actual = _json.GetCard(card.Id).Params;
                List<string> keys = new List<string>();
                foreach (FieldInfo field in fields)
                {
                    keys.Add(field.Name.TrimStart('_'));
                }
                CollectionAssert.AreEquivalent(keys, new List<string>(actual.Values.Keys), card.Id + " paramKey 집합");

                foreach (FieldInfo field in fields)
                {
                    string key = field.Name.TrimStart('_');
                    Assert.AreEqual((float)field.GetValue(card.Effect), actual.Get(key), card.Id + "/" + key);
                    total++;
                }
            }
            Assert.AreEqual(26, total, "수치 총 개수");
        }

        //# 카드 풀 순서는 이 이전에서 바뀌면 안 된다(데일리 카드 시드가 의존, SJ8) — 기획서 §4.8 의 소속·순서와 같다.
        [Test]
        public void 카드_풀_소속과_순서는_기획서와_같다()
        {
            ECardId[] passive =
            {
                ECardId.WispHpBoost, ECardId.WraithDamageBoost, ECardId.SpawnWraith, ECardId.ReplaceWispsToWraith, ECardId.ReaperAtkSpeed,
                ECardId.HexRangeBoost, ECardId.SpawnReapers, ECardId.ReplaceReapersToHex, ECardId.PlagueSlowBoost, ECardId.SpawnPlagues,
                ECardId.LingeringCurse, ECardId.HeroAttackDown, ECardId.PhantomMoveSpeedBoost, ECardId.SpawnPhantoms, ECardId.SpawnWisps,
                ECardId.SpawnerHaste,
            };
            ECardId[] active =
            {
                ECardId.IronWill, ECardId.WallOfWisps, ECardId.Berserk, ECardId.Frenzy, ECardId.BloodThirst, ECardId.MarkOfDeath,
                ECardId.Fear, ECardId.Bleed, ECardId.Weaken, ECardId.TimeStop, ECardId.Multiply, ECardId.Slow,
            };

            CollectionAssert.AreEqual(passive, PoolCards("CardPool_Passive").ConvertAll(card => card.Id), "패시브 풀");
            CollectionAssert.AreEqual(active, PoolCards("CardPool_Active").ConvertAll(card => card.Id), "액티브 풀");
        }
    }
}
