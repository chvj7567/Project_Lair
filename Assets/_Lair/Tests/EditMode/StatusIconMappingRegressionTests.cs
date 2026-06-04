using System;
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Card;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.EditMode
{
    //# 상태 아이콘 — 매핑/Enum/VM forward 회귀 박제 (test-engineer).
    //#  #5  8 Aura 의 IconCardId 가 기획서 §2 표와 일치 (변경 시 깨지게 고정)
    //#  #9  EVisual 에서 status 6값 제거 + PoisonAura 유지 회귀
    //#  #10 BattleViewModel forward — AddStatusIcon→OnStatusIconAdded / RemoveStatusIcon→OnStatusIconRemoved
    public class StatusIconMappingRegressionTests
    {
        //# ---------- #5: aura → 대표 ECardId 매핑 표 (기획서 §2, 8행) ----------
        //# 각 aura 를 실제 생성(의존성 null — IconCardId 는 상수라 null 안전)해 IconCardId 를 표와 대조.
        //# 표가 바뀌면(예: EternalBleed→다른 ID) 이 테스트가 깨져 회귀를 잡는다.

        [Test]
        public void IconCardId_SlowAura_Slow()
            => Assert.AreEqual(ECardId.Slow, ((IStatusVisual)new SlowAura(null)).IconCardId);

        [Test]
        public void IconCardId_FearAura_Fear()
            => Assert.AreEqual(ECardId.Fear, ((IStatusVisual)new FearAura(null)).IconCardId);

        [Test]
        public void IconCardId_WeakenAura_Weaken()
            => Assert.AreEqual(ECardId.Weaken, ((IStatusVisual)new WeakenAura(null)).IconCardId);

        [Test]
        public void IconCardId_TimeStopAura_TimeStop()
            => Assert.AreEqual(ECardId.TimeStop, ((IStatusVisual)new TimeStopAura(null, null)).IconCardId);

        [Test]
        public void IconCardId_BleedAura_Bleed()
            => Assert.AreEqual(ECardId.Bleed, ((IStatusVisual)new BleedAura()).IconCardId);

        [Test]
        public void IconCardId_MarkOfDeathAura_MarkOfDeath()
            => Assert.AreEqual(ECardId.MarkOfDeath, ((IStatusVisual)new MarkOfDeathAura()).IconCardId);

        //# 무기한 2종 — 표의 핵심 회귀 포인트.
        [Test]
        public void IconCardId_HeroAttackDownAura_HeroAttackDown()
            => Assert.AreEqual(ECardId.HeroAttackDown, ((IStatusVisual)new HeroAttackDownAura(null)).IconCardId);

        //# EternalBleed → 전용 카드 없음 → Bleed 아이콘 재사용 (기획서 §2 확정). 변경 시 깨지게 박제.
        [Test]
        public void IconCardId_EternalBleedAura_Bleed재사용()
            => Assert.AreEqual(ECardId.Bleed, ((IStatusVisual)new EternalBleedAura(null)).IconCardId);

        //# 표 전체를 한 번에 박제 — 한 행이라도 어긋나면 실패 + 어느 타입인지 메시지.
        [Test]
        public void IconCardId_8Aura_기획서_2_표_전수일치()
        {
            (IStatusVisual aura, ECardId expected)[] table =
            {
                (new SlowAura(null),            ECardId.Slow),
                (new FearAura(null),            ECardId.Fear),
                (new WeakenAura(null),          ECardId.Weaken),
                (new TimeStopAura(null, null),  ECardId.TimeStop),
                (new BleedAura(),               ECardId.Bleed),
                (new MarkOfDeathAura(),         ECardId.MarkOfDeath),
                (new HeroAttackDownAura(null),  ECardId.HeroAttackDown),
                (new EternalBleedAura(null),    ECardId.Bleed),
            };

            foreach ((IStatusVisual aura, ECardId expected) row in table)
            {
                Assert.AreEqual(row.expected, row.aura.IconCardId,
                    $"{row.aura.GetType().Name} 의 IconCardId 가 기획서 §2 표와 불일치");
            }
        }

        //# ---------- #9: EVisual enum 회귀 — PoisonAura 유지, status 6값 제거 ----------

        [Test]
        public void EVisual_PoisonAura_유지()
            => Assert.IsTrue(Enum.IsDefined(typeof(EVisual), "PoisonAura"));

        //# 제거된 status 6값이 부활하면 실패 (PoisonAura 복귀 방지 회귀).
        [TestCase("SlowStatus")]
        [TestCase("FearStatus")]
        [TestCase("WeakenStatus")]
        [TestCase("AttackDownStatus")]
        [TestCase("TimeStopStatus")]
        [TestCase("BleedStatus")]
        public void EVisual_제거된status값_없음(string removed)
            => Assert.IsFalse(Enum.IsDefined(typeof(EVisual), removed),
                $"EVisual.{removed} 가 부활했다 — 상태 아이콘 교체(2026-06-04)로 제거됐어야 함");

        //# ---------- #10: BattleViewModel forward 통합 ----------

        private BattleViewModel MakeVm()
            => new BattleViewModel(new BattleStateModel());

        [Test]
        public void BattleViewModel_AddStatusIcon_OnStatusIconAdded_발행()
        {
            BattleViewModel vm = MakeVm();
            object gotKey = null;
            ECardId gotId = default;
            int count = 0;
            vm.OnStatusIconAdded += (key, id) => { gotKey = key; gotId = id; count++; };

            object key = typeof(TimeStopAura);
            vm.AddStatusIcon(key, ECardId.TimeStop);

            Assert.AreEqual(1, count);
            Assert.AreSame(key, gotKey);
            Assert.AreEqual(ECardId.TimeStop, gotId);
        }

        [Test]
        public void BattleViewModel_RemoveStatusIcon_OnStatusIconRemoved_발행()
        {
            BattleViewModel vm = MakeVm();
            object gotKey = null;
            int count = 0;
            vm.OnStatusIconRemoved += key => { gotKey = key; count++; };

            object key = typeof(TimeStopAura);
            vm.RemoveStatusIcon(key);

            Assert.AreEqual(1, count);
            Assert.AreSame(key, gotKey);
        }

        //# 구독자 없을 때 — null 이벤트로 NRE 안 남(graceful).
        [Test]
        public void BattleViewModel_구독자없음_AddRemove_예외없음()
        {
            BattleViewModel vm = MakeVm();
            Assert.DoesNotThrow(() => vm.AddStatusIcon(typeof(SlowAura), ECardId.Slow));
            Assert.DoesNotThrow(() => vm.RemoveStatusIcon(typeof(SlowAura)));
        }

        //# 다중 forward — 여러 key 가 순서대로 통지된다 (HeroAuraRunner→VM 다중 상태 회귀).
        [Test]
        public void BattleViewModel_다중Add_순서대로_forward()
        {
            BattleViewModel vm = MakeVm();
            List<ECardId> ids = new List<ECardId>();
            vm.OnStatusIconAdded += (key, id) => ids.Add(id);

            vm.AddStatusIcon(typeof(SlowAura), ECardId.Slow);
            vm.AddStatusIcon(typeof(FearAura), ECardId.Fear);
            vm.AddStatusIcon(typeof(BleedAura), ECardId.Bleed);

            CollectionAssert.AreEqual(
                new[] { ECardId.Slow, ECardId.Fear, ECardId.Bleed }, ids);
        }
    }
}
