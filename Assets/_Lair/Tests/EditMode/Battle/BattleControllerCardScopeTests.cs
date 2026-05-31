using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using Lair.UI;

namespace Lair.Tests.Battle
{
    //# 스포너 상태 UI — 영역 A (BattleController 카드 source 추적, 기획서 §4.2 BLOCKER 4 결정).
    //#
    //# 검증 포인트:
    //#  - ApplyCardEffect(card) 가 _currentCardScope 를 카드로 잠시 설정하고 finally 로 해제.
    //#  - 강화 카드 픽 → RegisterMonsterTypeBuff 가 _currentCardScope 를 source 로 TrackCardPick 호출.
    //#  - 중첩 픽: PickCount 누적 + AggregateMultiplier 곱연산 동기화.
    //#  - 다른 종 강화는 같은 dict 의 키 분리.
    //#  - _currentCardScope == null (시뮬레이션 외 직접 호출) 일 때는 추적 데이터 누적 안 됨.
    //#
    //# 본격 스위트 (test-engineer) — gameplay-programmer 자체 검증을 넘어 엣지·회귀 망라.
    public class BattleControllerCardScopeTests
    {
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            //# CharacterRegistry 는 정적 — 다른 테스트에서 leak 됐을 수 있어 비운다.
            //# (RegisterMonsterTypeBuff 가 필드 동일 종 소급 순회에 사용)
            Lair.Character.CharacterRegistry.Monsters.Clear();
            Lair.Character.CharacterRegistry.Heroes.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
            Lair.Character.CharacterRegistry.Monsters.Clear();
            Lair.Character.CharacterRegistry.Heroes.Clear();
        }

        //# 비활성 BattleController — Start(async void) 가 안 돌도록 SetActive(false) 후 생성.
        //# RegisterMonsterTypeBuff 가 require 하는 의존(_ctx 등)을 reflection 으로 주입.
        private BattleController CreateIsolated()
        {
            GameObject go = new GameObject("BC_UT");
            go.SetActive(false);
            _spawned.Add(go);
            BattleController bc = go.AddComponent<BattleController>();
            //# BattleContext 주입 — Apply 가 ctx 를 받아 RegisterMonsterTypeBuff 위임할 때 사용.
            //# 카드 리뉴얼 v0.6 — BattleContext 가 BuildSynergyService 주입을 받는 2-arg 시그니처로 변경.
            //# 본 테스트의 검증 대상은 빌드 시너지가 아니므로 null 주입(BattleContext.RegisterCardPick null 가드).
            SetPrivate(bc, "_ctx", new BattleContext(bc, null));
            return bc;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인 (production 시그니처 변경 감지)");
            fi.SetValue(target, value);
        }

        private static T GetPrivate<T>(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            return (T)fi.GetValue(target);
        }

        //# ===== _currentCardScope 라이프사이클 =====

        //# 정상 — ApplyCardEffect 진입 시 _currentCardScope 가 인자 카드로 설정되어 Apply 본문에서 보임.
        [Test]
        public void ApplyCardEffect_진입시_currentCardScope에_카드_저장된다()
        {
            BattleController bc = CreateIsolated();
            CardData seen = null;
            FakeCardEffect effect = new FakeCardEffect { OnApply = _ => { seen = GetPrivate<CardData>(bc, "_currentCardScope"); } };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            bc.ApplyCardEffect(card);

            Assert.AreSame(card, seen, "Apply 본문 실행 시점에 _currentCardScope == card");
        }

        //# 정상 — Apply 호출 후 _currentCardScope 가 null 로 복원 (finally 보장).
        [Test]
        public void ApplyCardEffect_복귀후_currentCardScope_null로_복원()
        {
            BattleController bc = CreateIsolated();
            FakeCardEffect effect = new FakeCardEffect();
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            bc.ApplyCardEffect(card);

            Assert.IsNull(GetPrivate<CardData>(bc, "_currentCardScope"),
                "Apply 정상 종료 후 _currentCardScope 는 null");
        }

        //# 회귀 — Apply 가 예외를 던져도 finally 가 _currentCardScope 를 null 로 복원.
        //# 이 보장이 깨지면 후속 직접 호출(RegisterMonsterTypeBuff)이 잘못된 source 로 추적된다.
        [Test]
        public void ApplyCardEffect_Apply가_예외던져도_finally로_scope_복원()
        {
            BattleController bc = CreateIsolated();
            FakeCardEffect effect = new FakeCardEffect { OnApply = _ => throw new System.InvalidOperationException("테스트 예외") };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            Assert.Throws<System.InvalidOperationException>(() => bc.ApplyCardEffect(card),
                "Apply 가 던진 예외는 ApplyCardEffect 가 swallow 하지 않고 그대로 전파");

            Assert.IsNull(GetPrivate<CardData>(bc, "_currentCardScope"),
                "예외 발생 후에도 finally 가 _currentCardScope 를 null 로 복원");
        }

        //# 엣지 — card == null 일 때 ApplyCardEffect 는 no-op, scope 변경 없음.
        [Test]
        public void ApplyCardEffect_card_null이면_noop_예외없음()
        {
            BattleController bc = CreateIsolated();
            Assert.DoesNotThrow(() => bc.ApplyCardEffect(null));
            Assert.IsNull(GetPrivate<CardData>(bc, "_currentCardScope"));
        }

        //# 엣지 — card.Effect == null 일 때도 ApplyCardEffect 는 no-op (Effect 미설정 카드 방어).
        [Test]
        public void ApplyCardEffect_card_Effect_null이면_noop()
        {
            BattleController bc = CreateIsolated();
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: null);

            Assert.DoesNotThrow(() => bc.ApplyCardEffect(card));
            Assert.IsNull(GetPrivate<CardData>(bc, "_currentCardScope"));
        }

        //# 엣지 — _ctx == null (BattleController.Start 가 안 돈 미초기화 상태) 면 ApplyCardEffect no-op.
        //# DebugApplyCard 가 Start 전 호출되는 케이스에 대한 방어 검증.
        [Test]
        public void ApplyCardEffect_ctx_null이면_noop_예외없음()
        {
            GameObject go = new GameObject("BC_UT_NullCtx");
            go.SetActive(false);
            _spawned.Add(go);
            BattleController bc = go.AddComponent<BattleController>();
            //# _ctx 주입 생략 — null 그대로.

            FakeCardEffect effect = new FakeCardEffect();
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            Assert.DoesNotThrow(() => bc.ApplyCardEffect(card),
                "_ctx null 시 Apply 호출하지 않고 no-op");
            Assert.AreEqual(0, effect.ApplyCount, "Effect.Apply 가 호출되지 않아야 함");
        }

        //# ===== _currentCardScope 가 있을 때 TrackCardPick 동작 =====

        //# 정상 — 강화 카드 1픽: AppliedBuffs[0] 에 PickCount=1, AggregateMultiplier=배율 반영.
        [Test]
        public void 강화_카드_1픽시_AppliedBuffs에_PickCount_1로_기록()
        {
            BattleController bc = CreateIsolated();
            //# Apply 본문이 ctx.RegisterMonsterTypeBuff 를 호출하는 강화 카드 시뮬레이션.
            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            bc.ApplyCardEffect(card);

            IReadOnlyList<BattleViewModel.AppliedBuff> buffs = bc.GetAppliedBuffs(EMonster.Wisp);
            Assert.AreEqual(1, buffs.Count, "Wisp 강화 1픽 → 엔트리 1개");
            Assert.AreSame(card, buffs[0].Source, "Source = 픽한 카드");
            Assert.AreEqual(1, buffs[0].PickCount, "PickCount = 1");
            Assert.AreEqual(EMonsterStatKind.Hp, buffs[0].Stat);
            Assert.AreEqual(1.5f, buffs[0].AggregateMultiplier, 0.0001f, "1픽 배율 1.5");
        }

        //# 정상 — 강화 카드 2픽: PickCount 누적 + AggregateMultiplier 곱연산 (1.5 × 1.5 = 2.25).
        [Test]
        public void 강화_카드_중첩_2픽시_PickCount_2_AggregateMultiplier_2점25()
        {
            BattleController bc = CreateIsolated();
            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);

            bc.ApplyCardEffect(card);
            bc.ApplyCardEffect(card);   //# 같은 카드 재픽

            IReadOnlyList<BattleViewModel.AppliedBuff> buffs = bc.GetAppliedBuffs(EMonster.Wisp);
            Assert.AreEqual(1, buffs.Count, "같은 카드 → 같은 엔트리 (Source 동일성)");
            Assert.AreEqual(2, buffs[0].PickCount, "PickCount 2");
            Assert.AreEqual(2.25f, buffs[0].AggregateMultiplier, 0.0001f, "1.5 × 1.5 = 2.25");
        }

        //# 정상 — 다른 종 강화는 dict 의 다른 키로 들어가 서로 영향 없음.
        [Test]
        public void 다른_종_강화는_dict_키_분리_서로_영향없음()
        {
            BattleController bc = CreateIsolated();
            FakeCardEffect wispEffect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            FakeCardEffect wraithEffect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Power, 1.5f)
            };
            CardData wispCard   = FakeCardData.Create(ECardId.WispHpBoost, effect: wispEffect);
            CardData wraithCard = FakeCardData.Create(ECardId.WraithDamageBoost, effect: wraithEffect);

            bc.ApplyCardEffect(wispCard);
            bc.ApplyCardEffect(wraithCard);

            IReadOnlyList<BattleViewModel.AppliedBuff> wispBuffs   = bc.GetAppliedBuffs(EMonster.Wisp);
            IReadOnlyList<BattleViewModel.AppliedBuff> wraithBuffs = bc.GetAppliedBuffs(EMonster.Wraith);
            Assert.AreEqual(1, wispBuffs.Count, "Wisp 엔트리 1");
            Assert.AreEqual(1, wraithBuffs.Count, "Wraith 엔트리 1");
            Assert.AreSame(wispCard,   wispBuffs[0].Source);
            Assert.AreSame(wraithCard, wraithBuffs[0].Source);
        }

        //# 정상 — GetAppliedBuffs(미픽 종) 은 빈 배열 반환 (null 아님).
        [Test]
        public void GetAppliedBuffs_미픽_종은_빈_배열_반환()
        {
            BattleController bc = CreateIsolated();

            IReadOnlyList<BattleViewModel.AppliedBuff> buffs = bc.GetAppliedBuffs(EMonster.Plague);

            Assert.IsNotNull(buffs, "null 아닌 빈 collection");
            Assert.AreEqual(0, buffs.Count, "빈 배열");
        }

        //# 회귀 — RegisterMonsterTypeBuff 를 ApplyCardEffect 밖에서 직접 호출 시
        //# (_currentCardScope == null) TrackCardPick 이 호출되지 않아 _typeModifierPicks 누적 안 됨.
        //# 그러나 StatMultiplier 누적은 정상 (강화 효과 자체는 적용).
        [Test]
        public void RegisterMonsterTypeBuff_scope_없이_호출시_추적_미누적_배율은_정상()
        {
            BattleController bc = CreateIsolated();

            //# scope 없이 직접 호출.
            bc.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f);

            //# 추적 데이터(picks) 는 누적 안 됨.
            Assert.AreEqual(0, bc.GetAppliedBuffs(EMonster.Wisp).Count,
                "_currentCardScope null 시 TrackCardPick 미호출 — 빈 리스트 유지");

            //# 글로벌 dict (_typeModifiers) 는 정상 곱연산 — StatMultiplier 자체는 유지.
            Dictionary<EMonster, StatMultiplier> modifiers = GetPrivate<Dictionary<EMonster, StatMultiplier>>(bc, "_typeModifiers");
            Assert.IsTrue(modifiers.ContainsKey(EMonster.Wisp), "_typeModifiers 에 Wisp 엔트리 있음");
            Assert.AreEqual(1.5f, modifiers[EMonster.Wisp].HpMul, 0.0001f,
                "곱연산 누적 자체는 정상 적용");
        }

        //# 정상 — RegisterMonsterTypeBuff 호출 시 OnTypeModifierChanged 이벤트 발행 (scope 유무 무관).
        [Test]
        public void RegisterMonsterTypeBuff_호출시_OnTypeModifierChanged_발행()
        {
            BattleController bc = CreateIsolated();
            EMonster? captured = null;
            bc.OnTypeModifierChanged += type => captured = type;

            bc.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, 0.7f);

            Assert.AreEqual(EMonster.Reaper, captured, "이벤트 인자 = 갱신된 종");
        }

        //# 회귀 — ApplyCardEffect 안의 RegisterMonsterTypeBuff 도 OnTypeModifierChanged 발행.
        //# VM 의 HandleTypeModifierChanged 가 깨지지 않도록 보장.
        [Test]
        public void ApplyCardEffect_경유시도_OnTypeModifierChanged_정상_발행()
        {
            BattleController bc = CreateIsolated();
            int callCount = 0;
            bc.OnTypeModifierChanged += _ => callCount++;

            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);
            bc.ApplyCardEffect(card);

            Assert.AreEqual(1, callCount, "강화 카드 1픽 → 1회 발행");
        }

        //# 회귀 — TrackCardPick 의 AggregateMultiplier 동기화: 동일 종·동일 스탯의 list 엔트리들이
        //# _typeModifiers[type].Get(stat) 으로 일괄 갱신 (종 1↔카드 1 매핑이라 사실상 1개 엔트리).
        [Test]
        public void TrackCardPick_AggregateMultiplier가_StatMultiplier_Get으로_동기화()
        {
            BattleController bc = CreateIsolated();
            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Reaper, EMonsterStatKind.Cooldown, 0.7f)
            };
            CardData card = FakeCardData.Create(ECardId.ReaperAtkSpeed, effect: effect);

            bc.ApplyCardEffect(card);
            bc.ApplyCardEffect(card);   //# 2픽 — 0.7 × 0.7 = 0.49.

            IReadOnlyList<BattleViewModel.AppliedBuff> buffs = bc.GetAppliedBuffs(EMonster.Reaper);
            Assert.AreEqual(1, buffs.Count);
            Assert.AreEqual(0.49f, buffs[0].AggregateMultiplier, 0.0001f,
                "AggregateMultiplier = StatMultiplier.Get(Cooldown) = 0.49");
        }
    }
}
