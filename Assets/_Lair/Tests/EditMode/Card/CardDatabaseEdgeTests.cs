using System;
using System.Collections.Generic;
using System.Reflection;
using Lair.Card;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# CardDatabase.Build 검증 경계·조회 API (so-to-json-migration §3.10·§10 SJ3~SJ5·§12.5-4). 출하 JSON 행을 한 곳씩 비틀어 위반을 만든다.
    public class CardDatabaseEdgeTests
    {
        //# 효과별 필수 paramKey — 기획서 §3.10 표를 production 표와 독립으로 다시 적은 기대값(수치 행 26).
        public static readonly Dictionary<ECardEffect, string[]> RequiredKeys = new Dictionary<ECardEffect, string[]>
        {
            { ECardEffect.WispHpBoost, new[] { "hpMul" } },
            { ECardEffect.WraithDamageBoost, new[] { "hpMul" } },
            { ECardEffect.ReaperAtkSpeed, new[] { "cdMul" } },
            { ECardEffect.HexRangeBoost, new[] { "rangeMul" } },
            { ECardEffect.PlagueToxinBoost, new[] { "toxinMul" } },
            { ECardEffect.PhantomMoveSpeedBoost, new[] { "speedMul" } },
            { ECardEffect.SpawnWisps, new string[0] },
            { ECardEffect.SpawnWraith, new string[0] },
            { ECardEffect.SpawnReapers, new string[0] },
            { ECardEffect.SpawnPlagues, new string[0] },
            { ECardEffect.SpawnPhantoms, new string[0] },
            { ECardEffect.WispWraithPowerBoost, new[] { "powerMul" } },
            { ECardEffect.ReaperHexPowerBoost, new[] { "powerMul" } },
            { ECardEffect.HeroAttackDown, new string[0] },
            { ECardEffect.Fear, new[] { "duration" } },
            { ECardEffect.Bleed, new[] { "ratio", "duration" } },
            { ECardEffect.Weaken, new[] { "factor", "duration" } },
            { ECardEffect.Slow, new[] { "heroFactor", "monsterMul", "duration" } },
            { ECardEffect.Frenzy, new[] { "duration" } },
            { ECardEffect.FastBreeding, new[] { "periodMul" } },
            { ECardEffect.BloodThirst, new[] { "duration" } },
            { ECardEffect.IronWill, new[] { "duration" } },
            { ECardEffect.TimeStop, new[] { "duration" } },
            { ECardEffect.GuardianRage, new[] { "duration" } },
            { ECardEffect.ToughHide, new string[0] },
            { ECardEffect.MarkOfDeath, new[] { "dmgTakenMul", "duration" } },
            { ECardEffect.SpawnerHaste, new[] { "periodMul" } },
            { ECardEffect.LingeringCurse, new[] { "durationBonusPerPick" } },
        };

        private static bool Exists(int id) => WorldMapTestSupport.Strings().ContainsKey(id);

        private static CardDatabase Build(CardDataSet data, Func<int, bool> exists = null, Func<int, string> text = null)
        {
            Func<int, bool> existsCheck = exists != null ? exists : new Func<int, bool>(Exists);
            Func<int, string> textLookup = text != null ? text : new Func<int, string>(WorldMapTestSupport.Text);
            return CardDatabase.Build(data, existsCheck, textLookup);
        }

        private static void AssertFails(CardDataSet data, params string[] fragments)
        {
            BuildEdgeTestKit.AssertFails(() => Build(data), fragments);
        }

        private static IEnumerable<ECardEffect> AllEffects()
        {
            foreach (ECardEffect effect in Enum.GetValues(typeof(ECardEffect)))
            {
                yield return effect;
            }
        }

        private static CardRow RowOfEffect(CardDataSet data, ECardEffect effect)
        {
            return Array.Find(data.Cards, row => row.effectKind == effect.ToString());
        }

        private static void RemoveParam(CardDataSet data, string cardId, string key)
        {
            List<Card_ParamRow> rows = new List<Card_ParamRow>(data.Params);
            rows.RemoveAll(row => row.cardId == cardId && row.paramKey == key);
            data.Params = rows.ToArray();
        }

        private static void AddParam(CardDataSet data, string cardId, string key, float value)
        {
            List<Card_ParamRow> rows = new List<Card_ParamRow>(data.Params);
            rows.Add(new Card_ParamRow { cardId = cardId, paramKey = key, value = value });
            data.Params = rows.ToArray();
        }

        //# --- 출하 구조 ---

        [Test]
        public void 필수_수치_표는_ECardEffect_28종을_덮고_수치_행_합계는_26이다()
        {
            int total = 0;
            foreach (ECardEffect effect in AllEffects())
            {
                Assert.IsTrue(RequiredKeys.ContainsKey(effect), effect.ToString());
                total += RequiredKeys[effect].Length;
            }

            Assert.AreEqual(28, RequiredKeys.Count);
            Assert.AreEqual(26, total);
        }

        [Test]
        public void 출하_카드는_효과_종류마다_정확히_한_장씩이다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();

            foreach (ECardEffect effect in AllEffects())
            {
                int count = 0;
                foreach (CardRow row in data.Cards)
                {
                    count += row.effectKind == effect.ToString() ? 1 : 0;
                }
                Assert.AreEqual(1, count, effect.ToString());
            }
        }

        [TestCaseSource(nameof(AllEffects))]
        public void 출하_카드의_수치_키_집합은_기획서_표와_정확히_같다(ECardEffect effect)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            CardDatabase db = WorldMapTestSupport.BuildCards(data);
            ECardId id = (ECardId)Enum.Parse(typeof(ECardId), RowOfEffect(data, effect).cardId);

            CardEntry entry = db.GetCard(id);

            Assert.AreEqual(effect, entry.Effect);
            CollectionAssert.AreEquivalent(RequiredKeys[effect], new List<string>(entry.Params.Values.Keys), effect.ToString());
        }

        [Test]
        public void 출하_Cards는_ECardId_정수값_오름차순이고_HeroPoisonAura는_없다()
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();

            Assert.AreEqual(28, db.Cards.Count);
            for (int i = 1; i < db.Cards.Count; i++)
            {
                Assert.Less((int)db.Cards[i - 1].Id, (int)db.Cards[i].Id, "인덱스 " + i);
            }
            Assert.IsNull(db.GetCard(ECardId.HeroPoisonAura), "폐기 자리는 행이 없다");
            Assert.IsNull(db.GetCard((ECardId)999));
            Assert.IsNull(db.GetCard((ECardId)(-1)));
        }

        [Test]
        public void 출하_모든_카드는_Params가_null이_아니고_파라미터_없는_7장은_비어_있다()
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();
            int empty = 0;

            foreach (CardEntry entry in db.Cards)
            {
                Assert.IsNotNull(entry.Params, entry.Id.ToString());
                empty += entry.Params.Values.Count == 0 ? 1 : 0;
            }

            Assert.AreEqual(7, empty);
        }

        //# --- 필수 입력 ---

        [Test]
        public void 묶음이_null이면_비어_있음_오류다()
        {
            BuildEdgeTestKit.AssertFails(() => CardDatabase.Build(null, Exists, WorldMapTestSupport.Text), "카드 데이터가 비어 있음");
        }

        [Test]
        public void 카드_배열이_null이거나_비어_있으면_Card_JSON_비어_있음_오류다()
        {
            AssertFails(new CardDataSet(), "Card.json 이 비어 있음");
            AssertFails(new CardDataSet { Cards = new CardRow[0], Params = new Card_ParamRow[0] }, "Card.json 이 비어 있음");
        }

        [Test]
        public void 수치_배열이_null이면_수치_필요_카드의_모든_키가_누락으로_보고된다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Params = null;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            Assert.AreEqual(26, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# --- Card ---

        [TestCase("Ghost")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("wisphpboost")]
        public void 카드_id가_ECardId_값명이_아니면_위반이다(string cardId)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[0].cardId = cardId;

            AssertFails(data, "Card.json cardId '" + cardId + "' 가 ECardId 에 없음");
        }

        [Test]
        public void 카드_행이_null이면_위반으로_보고하고_예외로_새지_않는다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[5] = null;

            AssertFails(data, "Card.json cardId '' 가 ECardId 에 없음");
        }

        [Test]
        public void 카드_id가_중복이면_오류다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[1].cardId = data.Cards[0].cardId;

            AssertFails(data, "Card.json cardId WispHpBoost 중복");
        }

        [Test]
        public void 행_순서가_ECardId_정수값_오름차순이_아니면_위반이다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            CardRow swap = data.Cards[0];
            data.Cards[0] = data.Cards[1];
            data.Cards[1] = swap;

            AssertFails(data, "Card.json 행 순서는 ECardId 정수값 오름차순이어야 함: WispHpBoost");
        }

        [Test]
        public void ECardId_정수값_오름차순이면_중간이_비어_있어도_통과한다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();

            CardDatabase db = Build(data);

            Assert.AreEqual(ECardId.ReplaceReapersToHex, db.Cards[12].Id);
            Assert.AreEqual(ECardId.HeroAttackDown, db.Cards[13].Id, "HeroPoisonAura(13) 자리를 건너뛴다");
        }

        [TestCase("Bogus")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("1")]
        [TestCase("tank")]
        public void 카드_axis가_EBuildAxis_값명이_아니면_위반이다(string axis)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[0].axis = axis;

            AssertFails(data, "Card[WispHpBoost].axis '" + axis + "' 가 EBuildAxis 에 없음");
        }

        [Test]
        public void 카드_axis_4종은_그대로_조립된다()
        {
            foreach (string axis in Enum.GetNames(typeof(EBuildAxis)))
            {
                CardDataSet data = WorldMapTestSupport.ShippedCardData();
                data.Cards[0].axis = axis;

                Assert.AreEqual((EBuildAxis)Enum.Parse(typeof(EBuildAxis), axis), Build(data).GetCard(ECardId.WispHpBoost).Axis, axis);
            }
        }

        [TestCase("Bogus")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("wispHpBoost")]
        public void 카드_effectKind가_ECardEffect_값명이_아니면_위반이다(string effectKind)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[0].effectKind = effectKind;

            AssertFails(data, "Card[WispHpBoost].effectKind '" + effectKind + "' 가 ECardEffect 에 없음");
        }

        [TestCase(0)]
        [TestCase(-1)]
        [TestCase(99999)]
        public void 카드_이름_설명_키가_0_음수_없는_id면_위반이다(int key)
        {
            Func<int, bool> exists = id => id != 99999 && Exists(id);
            CardDataSet nameBad = WorldMapTestSupport.ShippedCardData();
            nameBad.Cards[2].nameKey = key;
            CardDataSet descBad = WorldMapTestSupport.ShippedCardData();
            descBad.Cards[2].descKey = key;

            BuildEdgeTestKit.AssertFails(() => Build(nameBad, exists), "Card[ReaperAtkSpeed] nameKey " + key);
            BuildEdgeTestKit.AssertFails(() => Build(descBad, exists), "Card[ReaperAtkSpeed] nameKey " + descBad.Cards[2].nameKey + "·descKey " + key);
        }

        [Test]
        public void 표시_문구는_Strings_조회_결과이고_null이면_빈_문자열이다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();

            CardDatabase mapped = Build(data, id => true, id => "T" + id);
            CardDatabase nulls = Build(data, id => true, id => null);

            CardEntry first = mapped.Cards[0];
            Assert.AreEqual("T" + data.Cards[0].nameKey, first.DisplayName);
            Assert.AreEqual("T" + data.Cards[0].descKey, first.Description);
            Assert.AreEqual(string.Empty, nulls.Cards[0].DisplayName);
            Assert.AreEqual(string.Empty, nulls.Cards[0].Description);
        }

        //# --- Card_Param ---

        //# 수치를 가진 효과(21종)만 — 수치 없는 7종은 아래 "없는 수치가 붙으면 위반" 테스트가 맡는다.
        private static IEnumerable<ECardEffect> EffectsWithParams()
        {
            foreach (ECardEffect effect in AllEffects())
            {
                if (RequiredKeys[effect].Length > 0)
                {
                    yield return effect;
                }
            }
        }

        [TestCaseSource(nameof(EffectsWithParams))]
        public void 수치_하나가_빠지면_그_키만_위반이다(ECardEffect effect)
        {
            string[] keys = RequiredKeys[effect];
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            CardRow card = RowOfEffect(data, effect);
            RemoveParam(data, card.cardId, keys[0]);

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("Card_Param.json 에 " + card.cardId + "/" + keys[0] + " 가 없음", e.Message);
            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        //# 수치 없는 7종도 포함 — 카드 id(Berserk)와 효과 이름(GuardianRage)이 다른 카드는 메시지에 둘 다 나온다.
        [TestCaseSource(nameof(AllEffects))]
        public void 효과에_없는_수치가_붙으면_그_키만_위반이다(ECardEffect effect)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            CardRow card = RowOfEffect(data, effect);
            AddParam(data, card.cardId, "bogus", 1f);

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("Card_Param.json " + card.cardId + "/bogus 는 " + effect + " 효과의 수치가 아님", e.Message);
            Assert.AreEqual(1, BuildEdgeTestKit.Violations(e).Length, e.Message);
        }

        [Test]
        public void 효과_종류를_바꾸면_이전_효과의_수치는_초과_새_효과의_수치는_누락이다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[0].effectKind = "Bleed";

            AssertFails(data, "WispHpBoost/ratio 가 없음", "WispHpBoost/duration 가 없음", "WispHpBoost/hpMul 는 Bleed 효과의 수치가 아님");
        }

        [Test]
        public void 수치_행의_복합키가_중복이면_오류다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            AddParam(data, "Bleed", "ratio", 0.5f);

            AssertFails(data, "Card_Param.json 복합키 중복: Bleed/ratio");
        }

        [Test]
        public void 같은_paramKey라도_다른_카드면_중복이_아니다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();

            Assert.IsNotNull(Array.Find(data.Params, row => row.cardId == "Fear" && row.paramKey == "duration"));
            Assert.IsNotNull(Array.Find(data.Params, row => row.cardId == "Frenzy" && row.paramKey == "duration"));
            Assert.DoesNotThrow(() => Build(data));
        }

        [TestCase("Ghost")]
        [TestCase("")]
        [TestCase(null)]
        [TestCase("0")]
        [TestCase("HeroPoisonAura")]
        public void 수치_행의_cardId가_Card_JSON에_없으면_위반이다(string cardId)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            AddParam(data, cardId, "hpMul", 1f);

            AssertFails(data, "Card_Param.json cardId '" + cardId + "' 가 Card.json 에 없음");
        }

        [Test]
        public void 수치_행이_null이면_위반으로_보고하고_예외로_새지_않는다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Params[0] = null;

            AssertFails(data, "Card_Param.json cardId '' 가 Card.json 에 없음");
        }

        [TestCase("")]
        [TestCase(null)]
        public void 수치_행의_paramKey가_비어_있으면_위반이다(string paramKey)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            AddParam(data, "Fear", paramKey, 1f);

            AssertFails(data, "Card_Param.json Fear 의 paramKey 가 비어 있음");
        }

        [TestCase(0f)]
        [TestCase(-0.5f)]
        [TestCase(float.NegativeInfinity)]
        [TestCase(float.NaN)]
        public void 수치_값이_0_이하이거나_NaN이면_위반이다(float value)
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            Array.Find(data.Params, row => row.cardId == "Fear" && row.paramKey == "duration").value = value;

            AssertFails(data, "Card_Param[Fear/duration].value");
        }

        [Test]
        public void 수치_값이_0에_가까운_양수면_통과한다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            Array.Find(data.Params, row => row.cardId == "Fear" && row.paramKey == "duration").value = 0.0001f;

            Assert.AreEqual(0.0001f, Build(data).GetCard(ECardId.Fear).Params.Get("duration"), 0f);
        }

        //# --- 위반 수집 ---

        [Test]
        public void 카드와_수치의_위반이_섞여도_메시지에_전부_담긴다()
        {
            CardDataSet data = WorldMapTestSupport.ShippedCardData();
            data.Cards[0].axis = "Bogus";
            data.Cards[3].nameKey = 0;
            data.Cards[4].cardId = "Ghost";
            RemoveParam(data, "Bleed", "duration");
            AddParam(data, "SpawnWisps", "bogus", 1f);
            Array.Find(data.Params, row => row.cardId == "Fear" && row.paramKey == "duration").value = 0f;

            ChapterDataException e = BuildEdgeTestKit.Fails(() => Build(data));

            StringAssert.Contains("Card[WispHpBoost].axis 'Bogus'", e.Message);
            StringAssert.Contains("Card[HexRangeBoost] nameKey 0", e.Message);
            StringAssert.Contains("Card.json cardId 'Ghost'", e.Message);
            StringAssert.Contains("Bleed/duration 가 없음", e.Message);
            StringAssert.Contains("SpawnWisps/bogus", e.Message);
            StringAssert.Contains("Card_Param[Fear/duration].value", e.Message);
            StringAssert.Contains("Card_Param.json cardId 'PlagueSlowBoost'", e.Message);
        }
    }

    //# CardData.CreateEffect — ECardEffect 28값 전부에 대해 효과 타입과 수치 전달 (so-to-json-migration §4.9).
    public class CardEffectCreateTests
    {
        private static IEnumerable<ECardEffect> AllEffects()
        {
            foreach (ECardEffect effect in Enum.GetValues(typeof(ECardEffect)))
            {
                yield return effect;
            }
        }

        private static CardEntry ShippedEntry(ECardEffect effect)
        {
            CardDatabase db = WorldMapTestSupport.ShippedCards();
            foreach (CardEntry entry in db.Cards)
            {
                if (entry.Effect == effect)
                    return entry;
            }
            return null;
        }

        private static float ReadField(object effect, string key)
        {
            FieldInfo field = effect.GetType().GetField("_" + key, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, effect.GetType().Name + "._" + key + " 필드 없음 (필드명 = paramKey 계약)");
            return Convert.ToSingle(field.GetValue(effect));
        }

        [TestCaseSource(nameof(AllEffects))]
        public void 효과_종류가_접미사_Effect_타입으로_만들어진다(ECardEffect effect)
        {
            CardEntry entry = ShippedEntry(effect);
            Assert.IsNotNull(entry, effect + " 효과를 쓰는 출하 카드가 없음");

            ICardEffect created = CardData.CreateEffect(entry);

            Assert.IsNotNull(created);
            Assert.AreEqual(effect + "Effect", created.GetType().Name);
        }

        [TestCaseSource(nameof(AllEffects))]
        public void 출하_수치가_같은_이름_필드로_전달된다(ECardEffect effect)
        {
            CardEntry entry = ShippedEntry(effect);

            ICardEffect created = CardData.CreateEffect(entry);

            foreach (KeyValuePair<string, float> pair in entry.Params.Values)
            {
                Assert.AreEqual(pair.Value, ReadField(created, pair.Key), 0f, effect + "." + pair.Key);
            }
        }

        //# 키마다 다른 값 — 출하 값이 우연히 같아 필드 뒤바뀜(ratio ↔ duration)이 가려지는 것을 막는다.
        [TestCaseSource(nameof(AllEffects))]
        public void 키마다_다른_값을_넣어도_각_필드가_자기_키_값을_읽는다(ECardEffect effect)
        {
            string[] keys = CardDatabaseEdgeTests.RequiredKeys[effect];
            Dictionary<string, float> values = new Dictionary<string, float>();
            for (int i = 0; i < keys.Length; i++)
            {
                values[keys[i]] = (i + 1) * 7.5f;
            }
            CardEntry entry = new CardEntry { Id = ECardId.WispHpBoost, Effect = effect, Params = new CardParams(values) };

            ICardEffect created = CardData.CreateEffect(entry);

            foreach (KeyValuePair<string, float> pair in values)
            {
                Assert.AreEqual(pair.Value, ReadField(created, pair.Key), 0f, effect + "." + pair.Key);
            }
        }

        [TestCaseSource(nameof(AllEffects))]
        public void Create는_호출마다_새_효과_인스턴스를_만든다(ECardEffect effect)
        {
            CardEntry entry = ShippedEntry(effect);

            Assert.AreNotSame(CardData.CreateEffect(entry), CardData.CreateEffect(entry));
        }

        [TestCase(999)]
        [TestCase(-1)]
        [TestCase(28)]
        public void Enum_범위_밖_효과는_ArgumentOutOfRangeException이다(int raw)
        {
            CardEntry entry = new CardEntry { Effect = (ECardEffect)raw, Params = new CardParams(new Dictionary<string, float>()) };

            Assert.Throws<ArgumentOutOfRangeException>(() => CardData.CreateEffect(entry));
        }

        [TestCaseSource(nameof(AllEffects))]
        public void 수치가_필요한_효과는_키가_빠지면_KeyNotFoundException이고_없는_효과는_빈_묶음도_만든다(ECardEffect effect)
        {
            string[] keys = CardDatabaseEdgeTests.RequiredKeys[effect];
            CardEntry empty = new CardEntry { Effect = effect, Params = new CardParams(new Dictionary<string, float>()) };

            if (keys.Length == 0)
            {
                Assert.IsNotNull(CardData.CreateEffect(empty), effect.ToString());
                return;
            }
            Assert.Throws<KeyNotFoundException>(() => CardData.CreateEffect(empty), effect.ToString());
        }
    }
}
