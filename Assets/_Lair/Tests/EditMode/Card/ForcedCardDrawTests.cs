using System.Collections.Generic;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;

namespace Lair.Tests.Card
{
    //# 지정 카드 강제 등장 (에디터 디버그) — 자체 스모크: 정상 1 + 엣지 4.
    //# 스크립트: 액티브 1픽 TimeStop / 액티브 2픽 Fear / 패시브 1픽 HeroPoisonAura.
    public class ForcedCardDrawTests
    {
        private bool _savedEnabled;

        [SetUp]
        public void SetUp()
        {
            //# 토글은 SessionState 백업 정적 상태 — 테스트 간 오염 방지로 저장/복원.
            _savedEnabled = ForcedCardDraw.Enabled;
        }

        [TearDown]
        public void TearDown()
        {
            ForcedCardDraw.Enabled = _savedEnabled;
        }

        private static List<CardData> Cards(params ECardId[] ids)
        {
            List<CardData> list = new List<CardData>();
            foreach (ECardId id in ids)
            {
                list.Add(FakeCardData.Create(id));
            }
            return list;
        }

        private static List<CardData> AllCards()
            => Cards(ECardId.TimeStop, ECardId.Fear, ECardId.HeroPoisonAura,
                     ECardId.Frenzy, ECardId.Bleed, ECardId.Slow,
                     ECardId.WispHpBoost, ECardId.ReaperAtkSpeed, ECardId.HexRangeBoost);

        private static int CountOf(IReadOnlyList<CardData> cards, ECardId id)
        {
            int count = 0;
            foreach (CardData c in cards)
            {
                if (c != null && c.Id == id)
                {
                    count++;
                }
            }
            return count;
        }

        private static void AssertDistinct3(IReadOnlyList<CardData> cards, string label)
        {
            Assert.AreEqual(3, cards.Count, $"{label} — 선택지 3장 유지");
            HashSet<ECardId> ids = new HashSet<ECardId>();
            foreach (CardData c in cards)
            {
                ids.Add(c.Id);
            }
            Assert.AreEqual(3, ids.Count, $"{label} — 중복 카드 2장 노출 금지");
        }

        //# 정상 — 토글 ON: 액티브 1픽 TimeStop · 2픽 Fear · 패시브 1픽 HeroPoisonAura 포함 + 3장 중복 없음.
        [Test]
        public void 토글ON_액티브1픽TimeStop_2픽Fear_패시브1픽HeroPoisonAura_포함_중복없음()
        {
            ForcedCardDraw.Enabled = true;
            ForcedCardDraw forced = new ForcedCardDraw();
            List<CardData> all = AllCards();

            IReadOnlyList<CardData> active1 = forced.Apply(
                Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);
            IReadOnlyList<CardData> passive1 = forced.Apply(
                Cards(ECardId.WispHpBoost, ECardId.ReaperAtkSpeed, ECardId.HexRangeBoost), isPassive: true, all, id => false);
            IReadOnlyList<CardData> active2 = forced.Apply(
                Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);

            Assert.AreEqual(1, CountOf(active1, ECardId.TimeStop), "액티브 1픽 — TimeStop 포함");
            Assert.AreEqual(1, CountOf(passive1, ECardId.HeroPoisonAura), "패시브 1픽 — HeroPoisonAura 포함");
            Assert.AreEqual(1, CountOf(active2, ECardId.Fear), "액티브 2픽 — Fear 포함");
            AssertDistinct3(active1, "액티브 1픽");
            AssertDistinct3(passive1, "패시브 1픽");
            AssertDistinct3(active2, "액티브 2픽");
        }

        //# 엣지 — 스크립트 소진 후(액티브 3픽~) 정상 드로우 그대로 (영향 0).
        [Test]
        public void 토글ON_스크립트소진후_액티브3픽은_정상드로우_그대로()
        {
            ForcedCardDraw.Enabled = true;
            ForcedCardDraw forced = new ForcedCardDraw();
            List<CardData> all = AllCards();

            forced.Apply(Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);
            forced.Apply(Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);

            List<CardData> drawn3 = Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow);
            IReadOnlyList<CardData> active3 = forced.Apply(drawn3, isPassive: false, all, id => false);

            Assert.AreSame(drawn3, active3, "액티브 3픽 — 스크립트 소진, 원본 드로우 무변경");
        }

        //# 엣지 — 토글 OFF: 1픽이어도 정상 드로우 그대로.
        [Test]
        public void 토글OFF_정상드로우_그대로()
        {
            ForcedCardDraw.Enabled = false;
            ForcedCardDraw forced = new ForcedCardDraw();

            List<CardData> drawn = Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow);
            IReadOnlyList<CardData> result = forced.Apply(drawn, isPassive: false, AllCards(), id => false);

            Assert.AreSame(drawn, result, "토글 OFF — 원본 드로우 무변경");
        }

        //# 엣지 — 토글 OFF 동안에도 픽 순번 진행: OFF 로 액티브 1픽 소모 → ON 전환 → 다음 액티브 픽은 2픽 (Fear).
        [Test]
        public void 토글OFF_동안에도_픽순번_진행_ON전환후_다음액티브픽은_Fear()
        {
            ForcedCardDraw forced = new ForcedCardDraw();
            List<CardData> all = AllCards();

            ForcedCardDraw.Enabled = false;
            forced.Apply(Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);

            ForcedCardDraw.Enabled = true;
            IReadOnlyList<CardData> active2 = forced.Apply(
                Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow), isPassive: false, all, id => false);

            Assert.AreEqual(0, CountOf(active2, ECardId.TimeStop), "OFF 동안 소모된 1픽 (TimeStop) 은 건너뜀");
            Assert.AreEqual(1, CountOf(active2, ECardId.Fear), "ON 전환 후 다음 액티브 픽 — 2픽 Fear 포함");
        }

        //# 엣지 — 지정 카드가 이미 정상 드로우에 포함 → 치환 스킵 (중복 2장 노출 금지).
        [Test]
        public void 지정카드가_이미_드로우에_포함되면_치환스킵()
        {
            ForcedCardDraw.Enabled = true;
            ForcedCardDraw forced = new ForcedCardDraw();

            List<CardData> drawn = Cards(ECardId.TimeStop, ECardId.Bleed, ECardId.Slow);
            IReadOnlyList<CardData> result = forced.Apply(drawn, isPassive: false, AllCards(), id => false);

            Assert.AreSame(drawn, result, "TimeStop 이미 포함 — 치환 불요");
            Assert.AreEqual(1, CountOf(result, ECardId.TimeStop), "TimeStop 1장만 노출");
        }

        //# 엣지 — 지정 카드가 3픽 캡 도달 → 강제 등장 스킵, 정상 드로우 유지.
        [Test]
        public void 지정카드가_3픽캡_도달이면_강제등장_스킵()
        {
            ForcedCardDraw.Enabled = true;
            ForcedCardDraw forced = new ForcedCardDraw();

            List<CardData> drawn = Cards(ECardId.Frenzy, ECardId.Bleed, ECardId.Slow);
            IReadOnlyList<CardData> result = forced.Apply(
                drawn, isPassive: false, AllCards(), id => id == ECardId.TimeStop);

            Assert.AreSame(drawn, result, "TimeStop 캡 도달 — 강제 등장 스킵");
        }
    }
}
