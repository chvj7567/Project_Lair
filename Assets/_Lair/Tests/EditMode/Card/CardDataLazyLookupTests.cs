using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# CardData 지연 조회·캐시와 CardPool.PlayableCards (so-to-json-migration §7.4·§9 E5).
    //# 전역 정적 CardDatabase.Current 를 바꾸므로 SetUp 에서 저장·초기화하고 TearDown 에서 반드시 원복한다. 출하 CardData SO 에셋은 건드리지 않는다(캐시·로그 플래그가 에디터 메모리에 남는다).
    public class CardDataLazyLookupTests
    {
        private CardDatabase _previous;
        private readonly List<Object> _created = new List<Object>();

        [SetUp]
        public void 카드_DB_전역값을_저장하고_비운다()
        {
            _previous = CardDatabase.Current;
            CardDatabase.Current = null;
        }

        [TearDown]
        public void 카드_DB_전역값을_원복하고_만든_SO를_정리한다()
        {
            CardDatabase.Current = _previous;
            foreach (Object created in _created)
            {
                Object.DestroyImmediate(created);
            }
            _created.Clear();
        }

        private CardData NewCard(ECardId id)
        {
            CardData card = ScriptableObject.CreateInstance<CardData>();
            TestReflection.SetField(card, "_id", id);
            _created.Add(card);
            return card;
        }

        //# 행 3종만 아는 합성 DB — 행 순서는 ECardId 정수값 오름차순. 표시 문구는 tag + 키라 DB 마다 달라진다.
        private static CardDatabase Db(string tag, params ECardId[] ids)
        {
            List<ECardId> sorted = new List<ECardId>(ids);
            sorted.Sort((a, b) => ((int)a).CompareTo((int)b));
            List<CardRow> cards = new List<CardRow>();
            List<Card_ParamRow> parameters = new List<Card_ParamRow>();
            foreach (ECardId id in sorted)
            {
                string axis;
                string effect;
                switch (id)
                {
                    case ECardId.WispHpBoost:
                        axis = "Tank";
                        effect = "WispHpBoost";
                        parameters.Add(new Card_ParamRow { cardId = id.ToString(), paramKey = "hpMul", value = 1.5f });
                        break;
                    case ECardId.Fear:
                        axis = "Debuff";
                        effect = "Fear";
                        parameters.Add(new Card_ParamRow { cardId = id.ToString(), paramKey = "duration", value = 3f });
                        break;
                    default:
                        axis = "Swarm";
                        effect = "SpawnWisps";
                        break;
                }
                cards.Add(new CardRow { cardId = id.ToString(), axis = axis, nameKey = 10 + (int)id, descKey = 100 + (int)id, effectKind = effect });
            }
            return CardDatabase.Build(new CardDataSet { Cards = cards.ToArray(), Params = parameters.ToArray() }, key => true, key => tag + key);
        }

        private static Regex Missing(ECardId id) => new Regex("Card.json 에 " + id + " 행이 없어");

        //# --- DB 없음 / 있음 ---

        [Test]
        public void DB가_없으면_HasData_false이고_값은_기본이며_로그도_없다()
        {
            CardData card = NewCard(ECardId.WispHpBoost);

            Assert.IsFalse(card.HasData);
            Assert.AreEqual(default(EBuildAxis), card.Axis);
            Assert.AreEqual(string.Empty, card.DisplayName);
            Assert.AreEqual(string.Empty, card.Description);
            Assert.IsNull(card.Effect);
            Assert.AreEqual(ECardId.WispHpBoost, card.Id);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DB에_행이_있으면_축_문구_효과가_행에서_온다()
        {
            CardDatabase.Current = Db("A", ECardId.WispHpBoost, ECardId.Fear);
            CardData card = NewCard(ECardId.Fear);

            Assert.IsTrue(card.HasData);
            Assert.AreEqual(EBuildAxis.Debuff, card.Axis);
            Assert.AreEqual("A" + (10 + (int)ECardId.Fear), card.DisplayName);
            Assert.AreEqual("A" + (100 + (int)ECardId.Fear), card.Description);
            Assert.AreEqual(typeof(FearEffect), card.Effect.GetType());
        }

        [Test]
        public void Effect를_HasData보다_먼저_읽어도_조회된다()
        {
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            CardData card = NewCard(ECardId.WispHpBoost);

            Assert.IsNotNull(card.Effect);
            Assert.IsTrue(card.HasData);
        }

        //# --- 재조회 ---

        [Test]
        public void DB가_교체되면_다음_조회에서_새_DB_값으로_다시_푼다()
        {
            CardData card = NewCard(ECardId.WispHpBoost);
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            string first = card.DisplayName;

            CardDatabase.Current = Db("B", ECardId.WispHpBoost);

            Assert.AreEqual("A10", first);
            Assert.AreEqual("B10", card.DisplayName);
        }

        [Test]
        public void DB가_null로_되돌려지면_행이_있던_카드도_HasData_false로_돌아가고_로그는_없다()
        {
            CardData card = NewCard(ECardId.WispHpBoost);
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            Assert.IsTrue(card.HasData);

            CardDatabase.Current = null;

            Assert.IsFalse(card.HasData);
            Assert.IsNull(card.Effect);
            Assert.AreEqual(string.Empty, card.DisplayName);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DB가_나중에_로드되면_처음_null이었던_카드도_조회된다()
        {
            CardData card = NewCard(ECardId.WispHpBoost);
            Assert.IsFalse(card.HasData);

            CardDatabase.Current = Db("A", ECardId.WispHpBoost);

            Assert.IsTrue(card.HasData);
            Assert.AreEqual("A10", card.DisplayName);
        }

        [Test]
        public void 같은_DB에서는_효과_인스턴스를_캐시하고_새_DB가_오면_새로_만든다()
        {
            CardDatabase dbA = Db("A", ECardId.WispHpBoost);
            CardDatabase.Current = dbA;
            CardData card = NewCard(ECardId.WispHpBoost);

            ICardEffect first = card.Effect;
            bool hasData = card.HasData;
            ICardEffect second = card.Effect;
            CardDatabase.Current = dbA;
            ICardEffect third = card.Effect;
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            ICardEffect fourth = card.Effect;

            Assert.IsTrue(hasData);
            Assert.AreSame(first, second, "같은 DB — 캐시");
            Assert.AreSame(first, third, "같은 DB 를 다시 대입해도 캐시");
            Assert.AreNotSame(first, fourth, "다른 DB(같은 행이라도) — 다시 푼다");
        }

        //# --- 행이 없는 카드 ---

        [Test]
        public void 행이_없는_카드는_HasData_false이고_LogError를_한_번만_남긴다()
        {
            CardDatabase.Current = Db("A", ECardId.Fear);
            CardData card = NewCard(ECardId.WispHpBoost);
            LogAssert.Expect(LogType.Error, Missing(ECardId.WispHpBoost));

            bool first = card.HasData;
            bool second = card.HasData;
            string name = card.DisplayName;
            ICardEffect effect = card.Effect;

            Assert.IsFalse(first);
            Assert.IsFalse(second);
            Assert.AreEqual(string.Empty, name);
            Assert.IsNull(effect);
            LogAssert.NoUnexpectedReceived();
        }

        //# _missingLogged 는 인스턴스 평생 유지 — 행이 생겼다가 다른 DB 에서 다시 없어져도 로그를 반복하지 않는다.
        [Test]
        public void 행이_없다가_생기고_다시_없어져도_로그는_인스턴스당_한_번이다()
        {
            CardData card = NewCard(ECardId.WispHpBoost);
            LogAssert.Expect(LogType.Error, Missing(ECardId.WispHpBoost));
            CardDatabase.Current = Db("A", ECardId.Fear);
            Assert.IsFalse(card.HasData);

            CardDatabase.Current = Db("B", ECardId.WispHpBoost, ECardId.Fear);
            bool afterPresent = card.HasData;
            CardDatabase.Current = Db("C", ECardId.Fear);
            bool afterMissingAgain = card.HasData;

            Assert.IsTrue(afterPresent);
            Assert.IsFalse(afterMissingAgain);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void 같은_id의_다른_CardData_인스턴스는_각자_한_번씩_로그를_남긴다()
        {
            CardDatabase.Current = Db("A", ECardId.Fear);
            CardData first = NewCard(ECardId.WispHpBoost);
            CardData second = NewCard(ECardId.WispHpBoost);
            LogAssert.Expect(LogType.Error, Missing(ECardId.WispHpBoost));
            LogAssert.Expect(LogType.Error, Missing(ECardId.WispHpBoost));

            Assert.IsFalse(first.HasData);
            Assert.IsFalse(second.HasData);
            LogAssert.NoUnexpectedReceived();
        }

        //# --- 테스트 더블 고정 주입 ---

        [Test]
        public void FakeCardData는_DB와_무관하게_주입한_값을_유지한다()
        {
            CardData fake = FakeCardData.Create(ECardId.Fear, EBuildAxis.Swarm, null, "가짜 이름", "가짜 설명");
            _created.Add(fake);

            CardDatabase.Current = null;
            Assert.IsTrue(fake.HasData);
            CardDatabase.Current = Db("A", ECardId.Fear);

            Assert.IsTrue(fake.HasData);
            Assert.AreEqual(EBuildAxis.Swarm, fake.Axis);
            Assert.AreEqual("가짜 이름", fake.DisplayName);
            Assert.AreEqual("가짜 설명", fake.Description);
            Assert.IsNull(fake.Effect);
        }

        //# --- CardPool.PlayableCards ---

        private CardPool NewPool(params CardData[] cards)
        {
            CardPool pool = ScriptableObject.CreateInstance<CardPool>();
            TestReflection.SetField(pool, "_cards", new List<CardData>(cards));
            _created.Add(pool);
            return pool;
        }

        [Test]
        public void PlayableCards는_null과_행_없는_카드를_빼고_풀_순서를_지킨다()
        {
            CardDatabase.Current = Db("A", ECardId.WispHpBoost, ECardId.Fear, ECardId.SpawnWisps);
            CardData wisp = NewCard(ECardId.WispHpBoost);
            CardData ghost = NewCard(ECardId.Bleed);
            CardData fear = NewCard(ECardId.Fear);
            CardData spawn = NewCard(ECardId.SpawnWisps);
            CardPool pool = NewPool(spawn, null, ghost, wisp, fear);
            LogAssert.Expect(LogType.Error, Missing(ECardId.Bleed));

            List<CardData> playable = pool.PlayableCards();

            CollectionAssert.AreEqual(new[] { spawn, wisp, fear }, playable);
            Assert.AreEqual(5, pool.Cards.Count, "풀 자체는 그대로");
        }

        [Test]
        public void PlayableCards는_호출마다_새_목록이고_두_번째_호출은_로그를_더하지_않는다()
        {
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            CardData wisp = NewCard(ECardId.WispHpBoost);
            CardData ghost = NewCard(ECardId.Bleed);
            CardPool pool = NewPool(wisp, ghost);
            LogAssert.Expect(LogType.Error, Missing(ECardId.Bleed));

            List<CardData> first = pool.PlayableCards();
            List<CardData> second = pool.PlayableCards();
            first.Clear();

            Assert.AreNotSame(first, second);
            CollectionAssert.AreEqual(new[] { wisp }, second);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DB가_없으면_PlayableCards는_비어_있고_로그도_없다()
        {
            CardPool pool = NewPool(NewCard(ECardId.WispHpBoost), NewCard(ECardId.Fear));

            Assert.AreEqual(0, pool.PlayableCards().Count);
            LogAssert.NoUnexpectedReceived();
        }

        [Test]
        public void DB가_바뀌어_행이_생기면_PlayableCards에_풀_순서대로_들어온다()
        {
            CardData fear = NewCard(ECardId.Fear);
            CardData wisp = NewCard(ECardId.WispHpBoost);
            CardPool pool = NewPool(fear, wisp);
            CardDatabase.Current = Db("A", ECardId.WispHpBoost);
            LogAssert.Expect(LogType.Error, Missing(ECardId.Fear));
            CollectionAssert.AreEqual(new[] { wisp }, pool.PlayableCards());

            CardDatabase.Current = Db("B", ECardId.WispHpBoost, ECardId.Fear);

            CollectionAssert.AreEqual(new[] { fear, wisp }, pool.PlayableCards());
        }

        [Test]
        public void 빈_풀의_PlayableCards는_빈_목록이다()
        {
            CardPool pool = NewPool();

            Assert.AreEqual(0, pool.PlayableCards().Count);
        }
    }
}
