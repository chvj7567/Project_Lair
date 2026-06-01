using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — 전역 3픽 캡 × BuildSynergy 상호작용 + 캡 경계 망라.
    //# 기존 CardPickCounterTests(누적·IsCapped·Reset), CardDeckCapExclusionTests(제외·graceful·null),
    //# BuildSynergyEdgeCasesTests(같은 카드 3픽 → 카운트 3 → Tier1 발화) 와 중복 금지.
    //# 본 스위트의 고유 가치:
    //#   - 캡 메커니즘(Draw 제외)이 픽을 3에서 멈춰 단일 카드 단독 Tier2(5)/Tier3(7) "발화 불가" 를 검증 (기획서 §2.4).
    //#   - 서로 다른 같은-축 카드 조합으로는 5·7 도달·발화 가능을 검증.
    //#   - 풀 전체 제외 → 빈 리스트, 적격 정확히 N 경계, 4+ 픽 후 GetCount 증가 + IsCapped 유지.
    public class CardPickCapInteractionTests
    {
        //# ===== 1. CardDeck 제외 — 풀 전체 캡 / 적격 경계 (gap #1) =====

        //# 모든 카드가 캡 도달 → Draw 가 빈 리스트(NRE 없음, graceful). 기존 테스트는 "일부 제외" 만 확인.
        [Test]
        public void Draw_풀전체_캡이면_빈리스트_반환()
        {
            List<CardData> pool = NewPool(ECardId.WispHpBoost, ECardId.Frenzy, ECardId.Slow);
            CardDeck deck = new CardDeck(pool, seed: 99);

            //# 모든 id 가 제외 대상.
            IReadOnlyList<CardData> drawn = deck.Draw(3, _ => true);

            Assert.IsNotNull(drawn, "빈 리스트여도 null 아님");
            Assert.AreEqual(0, drawn.Count, "전체 제외 → 후보 0장");
        }

        //# 적격이 정확히 3장이면 3장 (제외 다수). 기존은 "1장만" 만 확인 → 경계 보강.
        [Test]
        public void Draw_적격_정확히3장이면_3장_반환()
        {
            List<CardData> pool = NewPool(
                ECardId.WispHpBoost, ECardId.HexRangeBoost, ECardId.PhantomMoveSpeedBoost,
                ECardId.Frenzy, ECardId.Slow, ECardId.TimeStop);
            CardDeck deck = new CardDeck(pool, seed: 99);

            //# 6장 중 3장 제외 → 적격 정확히 3장.
            IReadOnlyList<CardData> drawn = deck.Draw(3,
                id => id == ECardId.Frenzy || id == ECardId.Slow || id == ECardId.TimeStop);

            Assert.AreEqual(3, drawn.Count, "적격 3장 = 요청 3장");
            foreach (CardData c in drawn)
                Assert.IsTrue(IsEligible3(c.Id), "제외 카드가 후보에 섞이면 안 됨");
        }

        //# 적격 2장 + 제외 다수 → 2장만 (요청 3 미만으로 graceful).
        [Test]
        public void Draw_적격2장_제외다수면_2장만_반환()
        {
            List<CardData> pool = NewPool(
                ECardId.WispHpBoost, ECardId.HexRangeBoost,
                ECardId.Frenzy, ECardId.Slow, ECardId.TimeStop, ECardId.Bleed);
            CardDeck deck = new CardDeck(pool, seed: 99);

            IReadOnlyList<CardData> drawn = deck.Draw(3,
                id => id != ECardId.WispHpBoost && id != ECardId.HexRangeBoost);

            Assert.AreEqual(2, drawn.Count, "적격 2장 → 가능한 만큼 2장");
        }

        //# ===== 2. CardPickCounter 경계 — 4+ 픽 (gap #4) =====

        //# 캡(3) 초과로 4·5픽 호출해도 GetCount 는 계속 증가하지만 IsCapped 는 true 유지 (캡 후에도 안전).
        [Test]
        public void RecordPick_4픽이상도_GetCount_증가하고_IsCapped_true_유지()
        {
            CardPickCounter counter = new CardPickCounter();
            for (int i = 0; i < 5; ++i)
                counter.RecordPick(ECardId.WispHpBoost);

            Assert.AreEqual(5, counter.GetCount(ECardId.WispHpBoost), "캡 초과 호출도 카운트는 누적");
            Assert.IsTrue(counter.IsCapped(ECardId.WispHpBoost), "캡 도달 후에도 IsCapped 유지");
        }

        //# 여러 카드 독립 카운트 — 한 카드 캡이 다른 카드에 영향 없음.
        [Test]
        public void RecordPick_여러카드_독립카운트_상호간섭없음()
        {
            CardPickCounter counter = new CardPickCounter();
            for (int i = 0; i < 3; ++i)
                counter.RecordPick(ECardId.WispHpBoost);
            counter.RecordPick(ECardId.HexRangeBoost);

            Assert.IsTrue(counter.IsCapped(ECardId.WispHpBoost), "WispHpBoost 캡");
            Assert.AreEqual(1, counter.GetCount(ECardId.HexRangeBoost), "HexRangeBoost 는 1픽");
            Assert.IsFalse(counter.IsCapped(ECardId.HexRangeBoost), "HexRangeBoost 미캡");
        }

        //# ===== 3. 캡 × 시너지 — 단일 카드 단독 Tier2/3 발화 불가 (gap #2, 기획서 §2.4 핵심) =====

        //# 단일 카드(같은 ECardId) 를 캡까지 픽 → 캡 메커니즘(Draw 제외)이 픽을 3에서 멈춤
        //# → Tank 축 카운트 3 → Tier1(3) 발화, Tier2(5)/Tier3(7) 는 바인딩되어도 단독 미발화.
        //# 기존 "RecordPick ×3" 과 달리 *캡 제외가 추가 픽을 차단하는지* 를 통해 카운트가 멈추는 것을 검증.
        [Test]
        public void 단일카드_캡까지반복픽_Tank카운트_3에서멈추고_Tier2_3_미발화()
        {
            //# 풀에 Tank 축 단일 카드 1장 + 잡카드들 (잡카드는 시너지 비-Tank 라 Tank 카운트 무관).
            List<CardData> pool = new List<CardData>
            {
                FakeCardData.Create(ECardId.WispHpBoost, EBuildAxis.Tank),
                FakeCardData.Create(ECardId.HexRangeBoost, EBuildAxis.Dps),
                FakeCardData.Create(ECardId.PhantomMoveSpeedBoost, EBuildAxis.Swarm),
            };
            CardDeck deck = new CardDeck(pool, seed: 7);

            CardPickCounter cap = new CardPickCounter();
            BuildSynergyService synergy = new BuildSynergyService();
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankTier1 = new FakeTier();
            FakeTier tankTier2 = new FakeTier();
            FakeTier tankTier3 = new FakeTier();
            synergy.BindTier(EBuildAxis.Tank, 3, tankTier1);
            synergy.BindTier(EBuildAxis.Tank, 5, tankTier2);
            synergy.BindTier(EBuildAxis.Tank, 7, tankTier3);

            //# Tank 카드(WispHpBoost) 만 반복 픽 시도. 캡 도달 후 Draw 가 제외 → 더 못 뽑음.
            //# 픽 루프: WispHpBoost 만 고른다고 가정 (다른 카드는 무시). 최대 20회 시도.
            int wispPicks = 0;
            for (int attempt = 0; attempt < 20; ++attempt)
            {
                IReadOnlyList<CardData> choices = deck.Draw(3, id => cap.IsCapped(id));
                CardData target = FindById(choices, ECardId.WispHpBoost);
                if (target == null)
                    break;   //# WispHpBoost 가 캡으로 후보에서 빠짐 → 더 픽 불가.

                cap.RecordPick(target.Id);
                synergy.RegisterPick(target.Axis, ctx);
                ++wispPicks;
            }

            Assert.AreEqual(CardPickCounter.Cap, wispPicks, "단일 카드는 캡(3)까지만 픽 가능");
            Assert.AreEqual(3, synergy.GetCount(EBuildAxis.Tank), "Tank 카운트는 3 에서 멈춤");
            Assert.AreEqual(1, tankTier1.AppliedCount, "Tier1(3) 발화");
            Assert.AreEqual(0, tankTier2.AppliedCount, "Tier2(5) 단독 미발화 (캡 3)");
            Assert.AreEqual(0, tankTier3.AppliedCount, "Tier3(7) 단독 미발화 (캡 3)");
        }

        //# 서로 다른 같은-축(Tank) 카드 3종을 각각 캡까지 픽 → Tank 카운트 9 도달
        //# → Tier1(3)·Tier2(5)·Tier3(7) 모두 발화. 단일 카드로는 불가능했던 Tier2/3 가 조합으로 열림 (기획서 §2.4).
        [Test]
        public void 서로다른_같은축카드_조합으로_Tier2_3_도달_발화()
        {
            List<CardData> pool = new List<CardData>
            {
                FakeCardData.Create(ECardId.WispHpBoost, EBuildAxis.Tank),
                FakeCardData.Create(ECardId.IronWill, EBuildAxis.Tank),
                FakeCardData.Create(ECardId.WallOfWisps, EBuildAxis.Tank),
            };
            CardDeck deck = new CardDeck(pool, seed: 13);

            CardPickCounter cap = new CardPickCounter();
            BuildSynergyService synergy = new BuildSynergyService();
            FakeBattleContext ctx = new FakeBattleContext();
            FakeTier tankTier1 = new FakeTier();
            FakeTier tankTier2 = new FakeTier();
            FakeTier tankTier3 = new FakeTier();
            synergy.BindTier(EBuildAxis.Tank, 3, tankTier1);
            synergy.BindTier(EBuildAxis.Tank, 5, tankTier2);
            synergy.BindTier(EBuildAxis.Tank, 7, tankTier3);

            //# 적격 카드를 계속 픽. 3종 × 캡3 = 최대 9픽 후 전부 캡 → 후보 고갈.
            int totalPicks = 0;
            for (int attempt = 0; attempt < 30; ++attempt)
            {
                IReadOnlyList<CardData> choices = deck.Draw(3, id => cap.IsCapped(id));
                if (choices.Count == 0)
                    break;

                CardData picked = choices[0];
                cap.RecordPick(picked.Id);
                synergy.RegisterPick(picked.Axis, ctx);
                ++totalPicks;
            }

            Assert.AreEqual(9, totalPicks, "3종 × 캡3 = 9픽까지 가능");
            Assert.AreEqual(9, synergy.GetCount(EBuildAxis.Tank), "Tank 카운트 9 도달");
            Assert.AreEqual(1, tankTier1.AppliedCount, "Tier1(3) 발화");
            Assert.AreEqual(1, tankTier2.AppliedCount, "Tier2(5) 발화 — 조합으로 도달");
            Assert.AreEqual(1, tankTier3.AppliedCount, "Tier3(7) 발화 — 조합으로 도달");
        }

        //# ===== Helper =====

        private static List<CardData> NewPool(params ECardId[] ids)
        {
            List<CardData> list = new List<CardData>();
            foreach (ECardId id in ids)
                list.Add(FakeCardData.Create(id));
            return list;
        }

        private static bool IsEligible3(ECardId id)
            => id == ECardId.WispHpBoost || id == ECardId.HexRangeBoost || id == ECardId.PhantomMoveSpeedBoost;

        private static CardData FindById(IReadOnlyList<CardData> list, ECardId id)
        {
            foreach (CardData c in list)
            {
                if (c.Id == id)
                    return c;
            }
            return null;
        }

        //# ===== Fakes (기존 스위트와 동일 패턴 — private nested 라 재선언) =====

        private class FakeTier : IBuildSynergyTier
        {
            public int AppliedCount { get; private set; }
            public void Apply(IBattleContext ctx) { ++AppliedCount; }
        }

        //# 본 스위트는 ctx 전달만 확인 — 메서드 호출 자체는 더미.
        private class FakeBattleContext : IBattleContext
        {
            public float DeltaTime => 0f;
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public Transform GetHeroTransform() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void IncrementGlobalMonsterCap(int delta) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
            public void ScaleSpawnerPeriodForType(EMonster type, float mul) { }
        }
    }
}
