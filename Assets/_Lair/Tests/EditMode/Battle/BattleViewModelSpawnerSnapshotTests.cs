using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 C (BattleViewModel SpawnerSnapshot · AttachSpawners · 이벤트 누수).
    //#
    //# 검증 포인트:
    //#  - AttachSpawners 후 6개 SpawnerSnapshot 가 초기 폴링값으로 채워짐.
    //#  - OnOutputTypeChanged 핸들 → 해당 인덱스 CurrentType + AppliedBuffs 재계산 + OnSpawnerSnapshotChanged.
    //#  - OnOutputCountChanged 핸들 → 해당 인덱스 OutputCount 갱신.
    //#  - OnTypeModifierChanged 핸들 → 동일 종 출력 스포너 *모두* 재계산 (같은 종 여러 셀).
    //#  - 다른 종 강화는 영향 X.
    //#  - DetachSpawners 후 이벤트 미수신 (누수 방지).
    //#  - AttachSpawners 중복 호출 멱등 (Detach 후 재attach).
    //#  - null Spawner / null controller 안전.
    public class BattleViewModelSpawnerSnapshotTests
    {
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            //# 정적 CharacterRegistry leak 방지.
            Lair.Character.CharacterRegistry.Monsters.Clear();
            Lair.Character.CharacterRegistry.Heroes.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) UnityEngine.Object.DestroyImmediate(go);
            _spawned.Clear();
            Lair.Character.CharacterRegistry.Monsters.Clear();
            Lair.Character.CharacterRegistry.Heroes.Clear();
        }

        //# 비활성 BattleController — Start 미실행. 카드 source 추적 검증에 필요한 _ctx 주입.
        private BattleController CreateIsolatedController()
        {
            GameObject go = new GameObject("BC_VM_UT");
            go.SetActive(false);
            _spawned.Add(go);
            BattleController bc = go.AddComponent<BattleController>();
            //# 카드 리뉴얼 v0.6 — BattleContext 2-arg 시그니처 (BuildSynergyService 주입). null 안전.
            SetPrivate(bc, "_ctx", new BattleContext(bc, null));
            return bc;
        }

        //# 기본 Spawner — _outputType 만 다르게 생성.
        private Spawner CreateSpawner(EMonster type)
        {
            GameObject go = new GameObject($"Spawner_{type}");
            _spawned.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", type);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            //# OnEnable 호출 — _currentType = _outputType, _outputCount = 1.
            MethodInfo mi = typeof(Spawner).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            mi?.Invoke(sp, null);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# 6개 다른 종 Spawner 배열.
        private Spawner[] MakeSixSpawners()
        {
            return new[]
            {
                CreateSpawner(EMonster.Wisp),
                CreateSpawner(EMonster.Reaper),
                CreateSpawner(EMonster.Phantom),
                CreateSpawner(EMonster.Wisp),     //# 같은 종 (index 0 과 동시)
                CreateSpawner(EMonster.Wraith),
                CreateSpawner(EMonster.Hex),
            };
        }

        //# ===== AttachSpawners 초기 채움 =====

        //# 정상 — Attach 후 Spawners.Count == 6, 각 인덱스에 스냅샷 채워짐 (출력 종 / count / buffs).
        [Test]
        public void AttachSpawners_6개_초기_스냅샷_채워진다()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();

            vm.AttachSpawners(spawners, bc);

            Assert.AreEqual(6, vm.Spawners.Count);
            //# 각 스냅샷 — Index/CurrentType/OutputCount/AppliedBuffs(빈) 모두 채워짐.
            for (int i = 0; i < 6; ++i)
            {
                BattleViewModel.SpawnerSnapshot snap = vm.Spawners[i];
                Assert.IsNotNull(snap, $"index {i} 스냅샷 non-null");
                Assert.AreEqual(i, snap.Index, $"index 필드 = {i}");
                Assert.AreEqual(spawners[i].CurrentType, snap.CurrentType, $"index {i} CurrentType 일치");
                Assert.AreEqual(1, snap.OutputCount, $"index {i} 초기 OutputCount = 1");
                Assert.IsNotNull(snap.AppliedBuffs, $"index {i} AppliedBuffs non-null");
                Assert.AreEqual(0, snap.AppliedBuffs.Count, $"index {i} 초기 buffs 0");
            }
        }

        //# 정상 — IncrementOutput 한 Spawner 가 있으면 초기 스냅샷에 그 값(1 초과) 반영.
        //# Spawner.OnEnable 이 1 로 리셋해버리므로, 본 테스트는 Attach 직전 Increment 가 누적되는 케이스.
        [Test]
        public void AttachSpawners_초기_OutputCount_폴링값_반영()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            //# 0번에만 Increment 2회 — OutputCount = 3 인 상태에서 Attach.
            spawners[0].IncrementOutput();
            spawners[0].IncrementOutput();

            vm.AttachSpawners(spawners, bc);

            Assert.AreEqual(3, vm.Spawners[0].OutputCount, "Attach 시점 폴링값 3 반영");
            Assert.AreEqual(1, vm.Spawners[1].OutputCount, "다른 Spawner 영향 없음");
        }

        //# 엣지 — null spawners 또는 null controller 시 Attach 무동작 (예외 없음).
        [Test]
        public void AttachSpawners_null_인자시_noop_예외없음()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();

            Assert.DoesNotThrow(() => vm.AttachSpawners(null, bc));
            Assert.DoesNotThrow(() => vm.AttachSpawners(new Spawner[0], null));
            Assert.AreEqual(0, vm.Spawners.Count, "Spawners 컬렉션 그대로 빈 상태");
        }

        //# 엣지 — 배열에 null Spawner 있어도 안전 (해당 인덱스 = null slot, 다른 인덱스 정상).
        [Test]
        public void AttachSpawners_배열에_null_있어도_정상_채워짐()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = new[]
            {
                CreateSpawner(EMonster.Wisp),
                null,
                CreateSpawner(EMonster.Hex),
            };

            vm.AttachSpawners(spawners, bc);

            Assert.AreEqual(3, vm.Spawners.Count, "Count 는 입력과 동일 (null slot 유지)");
            Assert.IsNotNull(vm.Spawners[0]);
            Assert.IsNull(vm.Spawners[1], "null Spawner 슬롯 = null 스냅샷");
            Assert.IsNotNull(vm.Spawners[2]);
        }

        //# ===== Spawner 이벤트 핸들 =====

        //# 정상 — Spawner.IncrementOutput → VM.OnSpawnerSnapshotChanged(해당 인덱스) 발행 + 스냅샷 OutputCount 갱신.
        [Test]
        public void IncrementOutput_VM_해당_인덱스_스냅샷_갱신_이벤트_발행()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            int callbackIndex = -1;
            vm.OnSpawnerSnapshotChanged += idx => callbackIndex = idx;

            spawners[2].IncrementOutput();   //# 인덱스 2 Phantom

            Assert.AreEqual(2, callbackIndex, "OnSpawnerSnapshotChanged 인자 = 2");
            Assert.AreEqual(2, vm.Spawners[2].OutputCount, "인덱스 2 OutputCount = 2");
            Assert.AreEqual(1, vm.Spawners[3].OutputCount, "다른 인덱스 영향 없음");
        }

        //# 정상 — Spawner.ReplaceOutput → 해당 인덱스 스냅샷 CurrentType + AppliedBuffs 재계산.
        [Test]
        public void ReplaceOutput_VM_해당_인덱스_CurrentType_갱신()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            int callbackIndex = -1;
            vm.OnSpawnerSnapshotChanged += idx => callbackIndex = idx;

            spawners[0].ReplaceOutput(EMonster.Plague);

            Assert.AreEqual(0, callbackIndex);
            Assert.AreEqual(EMonster.Plague, vm.Spawners[0].CurrentType);
        }

        //# 회귀 — ReplaceOutput 으로 출력 종이 바뀐 인덱스의 AppliedBuffs 는 *새 종* 기준으로 교체.
        [Test]
        public void ReplaceOutput_후_AppliedBuffs는_새_종_기준()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            //# Wraith 강화 픽 — Wraith 종에 buff 1개 누적.
            FakeCardEffect wraithEffect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Power, 1.5f)
            };
            CardData wraithCard = FakeCardData.Create(ECardId.WraithDamageBoost, effect: wraithEffect);
            bc.ApplyCardEffect(wraithCard);

            //# 인덱스 0 (Wisp) 을 Wraith 로 변경 → 그 인덱스의 AppliedBuffs 가 Wraith buff 노출되어야.
            spawners[0].ReplaceOutput(EMonster.Wraith);

            Assert.AreEqual(EMonster.Wraith, vm.Spawners[0].CurrentType);
            Assert.AreEqual(1, vm.Spawners[0].AppliedBuffs.Count, "Wraith 강화 1픽이 새 종 기준 buffs 에 노출");
            Assert.AreSame(wraithCard, vm.Spawners[0].AppliedBuffs[0].Source);
        }

        //# ===== OnTypeModifierChanged 핸들 (같은 종 여러 셀) =====

        //# 정상 — 강화 카드 픽 → BattleController.OnTypeModifierChanged → VM 이 동일 종 *모든* 인덱스 갱신.
        //# 6 Spawner 중 Wisp 가 2개(인덱스 0, 3) — 두 인덱스 모두 OnSpawnerSnapshotChanged 발행.
        [Test]
        public void 강화_카드_픽시_동일_종_모든_인덱스_스냅샷_갱신()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();   //# 0=Wisp, 3=Wisp.
            vm.AttachSpawners(spawners, bc);

            List<int> changedIndexes = new List<int>();
            vm.OnSpawnerSnapshotChanged += idx => changedIndexes.Add(idx);

            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);
            bc.ApplyCardEffect(card);

            //# 인덱스 0 과 3 (Wisp 출력) 만 통지.
            CollectionAssert.AreEquivalent(new[] { 0, 3 }, changedIndexes,
                "Wisp 출력 인덱스 2개만 통지 (1·2·4·5 영향 X)");

            //# 두 인덱스 모두 AppliedBuffs 에 Wisp 강화 1픽 누적.
            Assert.AreEqual(1, vm.Spawners[0].AppliedBuffs.Count);
            Assert.AreEqual(1, vm.Spawners[3].AppliedBuffs.Count);
            //# 다른 인덱스는 빈 buffs 유지.
            Assert.AreEqual(0, vm.Spawners[1].AppliedBuffs.Count, "Reaper");
            Assert.AreEqual(0, vm.Spawners[2].AppliedBuffs.Count, "Phantom");
            Assert.AreEqual(0, vm.Spawners[4].AppliedBuffs.Count, "Wraith");
            Assert.AreEqual(0, vm.Spawners[5].AppliedBuffs.Count, "Hex");
        }

        //# 회귀 — 다른 종 강화 픽은 미매칭 인덱스에 영향 없음 (이벤트 미발행).
        [Test]
        public void 다른_종_강화_픽시_미매칭_인덱스_이벤트_미발행()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();   //# 0=Wisp, 1=Reaper, 2=Phantom, 3=Wisp, 4=Wraith, 5=Hex.
            //# 6 종 중 Plague 가 없는 상태에서 Plague 강화 픽.
            vm.AttachSpawners(spawners, bc);

            int callCount = 0;
            vm.OnSpawnerSnapshotChanged += _ => callCount++;

            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Plague, EMonsterStatKind.SlowFactor, 0.75f)
            };
            CardData card = FakeCardData.Create(ECardId.PlagueSlowBoost, effect: effect);
            bc.ApplyCardEffect(card);

            Assert.AreEqual(0, callCount,
                "출력 중인 Spawner 없는 종(Plague) 강화 픽 — 인덱스 갱신 0회");
        }

        //# 회귀 — 같은 종 1픽 후 같은 종 1픽 → 두 인덱스 모두 2번씩 통지 (셀 갱신이 매 픽마다 발생).
        [Test]
        public void 같은_종_2픽_연속_각_매칭_인덱스_2회씩_통지()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            Dictionary<int, int> perIndexCalls = new Dictionary<int, int>();
            vm.OnSpawnerSnapshotChanged += idx =>
            {
                perIndexCalls.TryGetValue(idx, out int c);
                perIndexCalls[idx] = c + 1;
            };

            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);
            bc.ApplyCardEffect(card);
            bc.ApplyCardEffect(card);   //# 같은 카드 재픽.

            Assert.AreEqual(2, perIndexCalls.GetValueOrDefault(0), "인덱스 0 (Wisp) — 2회 통지");
            Assert.AreEqual(2, perIndexCalls.GetValueOrDefault(3), "인덱스 3 (Wisp) — 2회 통지");
            //# 2픽 후 PickCount 누적 확인.
            Assert.AreEqual(2, vm.Spawners[0].AppliedBuffs[0].PickCount);
            Assert.AreEqual(2.25f, vm.Spawners[0].AppliedBuffs[0].AggregateMultiplier, 0.0001f);
        }

        //# ===== DetachSpawners 누수 방지 =====

        //# 회귀 — DetachSpawners 후 Spawner.IncrementOutput 이벤트 미수신.
        [Test]
        public void DetachSpawners_이후_Spawner_이벤트_미수신()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            int callCount = 0;
            vm.OnSpawnerSnapshotChanged += _ => callCount++;

            vm.DetachSpawners();

            spawners[0].IncrementOutput();
            spawners[1].ReplaceOutput(EMonster.Plague);

            Assert.AreEqual(0, callCount,
                "Detach 후 Spawner 이벤트가 VM 으로 흘러들어오지 않음 (누수 방지)");
        }

        //# 회귀 — DetachSpawners 후 BattleController.OnTypeModifierChanged 미수신.
        [Test]
        public void DetachSpawners_이후_OnTypeModifierChanged_미수신()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            int callCount = 0;
            vm.OnSpawnerSnapshotChanged += _ => callCount++;

            vm.DetachSpawners();

            FakeCardEffect effect = new FakeCardEffect
            {
                OnApply = ctx => ctx.RegisterMonsterTypeBuff(EMonster.Wisp, EMonsterStatKind.Hp, 1.5f)
            };
            CardData card = FakeCardData.Create(ECardId.WispHpBoost, effect: effect);
            bc.ApplyCardEffect(card);

            Assert.AreEqual(0, callCount, "Detach 후 controller 이벤트 미수신");
        }

        //# 회귀 — DetachSpawners 후 Spawners 컬렉션이 비워짐.
        [Test]
        public void DetachSpawners_이후_Spawners_컬렉션_비워짐()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);
            Assert.AreEqual(6, vm.Spawners.Count);

            vm.DetachSpawners();

            Assert.AreEqual(0, vm.Spawners.Count, "Detach 후 Spawners 빈 컬렉션");
        }

        //# 회귀 — DetachSpawners 멱등 — 두 번 호출해도 예외 없음.
        [Test]
        public void DetachSpawners_두번_호출시_예외없음()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] spawners = MakeSixSpawners();
            vm.AttachSpawners(spawners, bc);

            vm.DetachSpawners();
            Assert.DoesNotThrow(() => vm.DetachSpawners(), "두 번째 Detach 도 예외 없이 no-op");
        }

        //# ===== AttachSpawners 중복 호출 — 멱등 (내부에서 Detach 먼저) =====

        //# 회귀 — AttachSpawners 두 번 호출 시 이전 구독을 해제하고 새 구독만 활성 (중복 호출 방지).
        [Test]
        public void AttachSpawners_중복_호출시_이전_구독_해제후_새로_attach()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            BattleController bc = CreateIsolatedController();
            Spawner[] firstSet = MakeSixSpawners();
            vm.AttachSpawners(firstSet, bc);

            //# 두 번째 attach — 새 spawner 셋.
            Spawner[] secondSet = new[]
            {
                CreateSpawner(EMonster.Plague),
                CreateSpawner(EMonster.Plague),
            };
            vm.AttachSpawners(secondSet, bc);

            //# Count 갱신.
            Assert.AreEqual(2, vm.Spawners.Count, "두 번째 attach 의 Count 적용");
            Assert.AreEqual(EMonster.Plague, vm.Spawners[0].CurrentType);

            //# 이전 spawner 이벤트는 더 이상 흘러들어가지 않음 (구독 해제 확인).
            int callCount = 0;
            vm.OnSpawnerSnapshotChanged += _ => callCount++;
            firstSet[0].IncrementOutput();
            firstSet[2].ReplaceOutput(EMonster.Hex);

            Assert.AreEqual(0, callCount, "두 번째 attach 후 첫 set 의 이벤트는 무시");

            //# 두 번째 set 의 이벤트는 흘러들어옴.
            secondSet[0].IncrementOutput();
            Assert.AreEqual(1, callCount, "두 번째 set 이벤트는 정상 수신");
        }

        //# ===== Spawners 컬렉션 read-only 노출 =====

        //# 정상 — Spawners 는 IReadOnlyList<SpawnerSnapshot> 시그니처로 노출.
        [Test]
        public void Spawners_프로퍼티는_IReadOnlyList_타입()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            //# 외부에 IReadOnlyList<SpawnerSnapshot> 으로 노출되어야 — 리플렉션으로 시그니처 확인.
            PropertyInfo prop = typeof(BattleViewModel).GetProperty("Spawners");
            Assert.IsNotNull(prop, "Spawners 프로퍼티 존재");
            Assert.AreEqual(typeof(IReadOnlyList<BattleViewModel.SpawnerSnapshot>),
                prop.PropertyType,
                "Spawners 의 외부 타입 = IReadOnlyList<SpawnerSnapshot>");
        }
    }
}
