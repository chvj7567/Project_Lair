# Slice B3 — 카드/몬스터 컨텐츠 확장 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 몬스터 3종 + 패시브 8장 + 액티브 10장 + 몬스터 글로벌 버프 시스템으로 기획서 §11.3 컨텐츠를 완성한다.

**Architecture:** 인터페이스 3종 확장(`IHealth.Heal`/`IMover.IsMoving`/`IAttacker.OnHit`) + 런타임 오버레이 배율(`DamageTakenScale`/`CooldownScale`/`PowerScale`)로 글로벌 버프를 직접 필드 수정 없이 처리. `MonsterBuffService` 가 매 tick 전체 몬스터에 버프 재적용.

**Tech Stack:** Unity 2022.3 / URP / TDD POCO / ChvjPackage(CHMResource·CHMUI·CHMPool)

설계서: `docs/superpowers/specs/2026-05-20-slice-b3-content-expansion-design.md`

---

## 파일 구조

**M1 — 몬스터 3종**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EMonster +3)
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` (Archer/Spider/Bat spec)

**M2 — 인터페이스 확장**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/Health.cs` / `SimpleMover.cs` / `MeleeAttacker.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Helpers/FakeHealth.cs` / `FakeMover.cs` / `FakeAttacker.cs`
- Create: `Assets/_Lair/Scripts/Character/SpiderSlowOnHit.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/OverlayScaleTests.cs`

**M3 — 패시브 8장**
- Create: `Assets/_Lair/Scripts/Card/Effects/` 효과 8개
- Create: `Assets/_Lair/Scripts/Card/Auras/HeroAttackDownAura.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/B3PassiveEffectTests.cs`

**M4 — 몬스터 글로벌 버프**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EMonsterBuff)
- Create: `Assets/_Lair/Scripts/Battle/MonsterBuffService.cs` / `BloodThirstService.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/` 몬스터 액티브 효과 4개
- Test: `Assets/_Lair/Tests/EditMode/Battle/MonsterBuffServiceTests.cs` / `BloodThirstServiceTests.cs`

**M5 — 영웅 액티브 + SO 재빌드**
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs` (FleeMode)
- Create: `Assets/_Lair/Scripts/Card/Auras/` 오라 4종
- Create: `Assets/_Lair/Scripts/Card/Effects/` 영웅 액티브 효과 6개
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (ECardId 재정의)
- Delete: `Assets/_Lair/Scripts/Card/Effects/MonsterAoeDamageEffect.cs` / `InstantSpawnGolemEffect.cs` / `InstantSpawnSlimesEffect.cs` / `HeroSilenceEffect.cs`
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` (PassiveSpecs 15 / ActiveSpecs 10)
- Test: `Assets/_Lair/Tests/EditMode/Card/B3ActiveEffectTests.cs`

**M6 — 통합**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Create: `Assets/_Lair/Tests/PlayMode/B3SmokeTest.cs`

---

## B3-M1: 몬스터 3종

### Task 1: EMonster 확장 + 몬스터 3종 프리팹

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs`

- [ ] **Step 1: EMonster 에 3종 추가**

```csharp
public enum EMonster
{
    Slime, Golem, Orc,
    Archer, Spider, Bat,   //# B3 신규
}
```

- [ ] **Step 2: LairCharacterPrefabBuilder.AllSpecs 에 3종 추가**

`AllSpecs` 배열 끝에 추가:
```csharp
new Spec { Name = nameof(EMonster.Archer), Mesh = PrimitiveType.Capsule, ColorHex = "#EAB308", Scale = 0.8f, Hp = 60, Power = 30, Range = 5.0f, Cooldown = 1.0f, MoveSpeed = 2.0f, IsHero = false },
new Spec { Name = nameof(EMonster.Spider), Mesh = PrimitiveType.Cube,    ColorHex = "#A855F7", Scale = 0.5f, Hp = 50, Power = 5,  Range = 1.0f, Cooldown = 1.0f, MoveSpeed = 2.0f, IsHero = false },
new Spec { Name = nameof(EMonster.Bat),    Mesh = PrimitiveType.Sphere,  ColorHex = "#1F2937", Scale = 0.3f, Hp = 30, Power = 5,  Range = 1.0f, Cooldown = 0.8f, MoveSpeed = 3.5f, IsHero = false },
```

- [ ] **Step 3: 거미 Y 스케일 납작 처리**

`BuildOne` 의 `localScale` 설정 후, Spider 면 Y 를 0.3 배율로:
```csharp
go.transform.localScale = Vector3.one * spec.Scale;
if (spec.Name == nameof(EMonster.Spider))
    go.transform.localScale = new Vector3(spec.Scale, spec.Scale * 0.3f, spec.Scale);
```

- [ ] **Step 4: 메뉴 실행 → 6종 프리팹 빌드**

Unity 에디터 `Lair/Setup/M3 - Build Character Prefabs` 실행. Project 창에 `Archer.prefab`/`Spider.prefab`/`Bat.prefab` 생성 + Addressables 등록 확인.

- [ ] **Step 5: 커밋 제안**

```
# [feat] - B3 몬스터 3종(궁수/거미/박쥐) 프리팹 + EMonster 확장
```

---

## B3-M2: 인터페이스 확장 + 오버레이

### Task 2: IHealth.Heal

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/Health.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Helpers/FakeHealth.cs`

- [ ] **Step 1: IHealth 에 Heal 추가**

```csharp
//# IHealth 인터페이스 내
void Heal(int amount);   //# Max 초과 불가
```

- [ ] **Step 2: Health.Heal 구현**

```csharp
public void Heal(int amount)
{
    if (IsAlive == false) return;
    int next = Mathf.Min(_max, Current + amount);
    if (next == Current) return;
    Current = next;
    OnChanged?.Invoke(Current, _max);
}
```

- [ ] **Step 3: FakeHealth.Heal 구현**

```csharp
public void Heal(int amount)
{
    Current = Math.Min(Max, Current + amount);
    OnChanged?.Invoke(Current, Max);
}
```

### Task 3: IMover.IsMoving

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/SimpleMover.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Helpers/FakeMover.cs`

- [ ] **Step 1: IMover 에 IsMoving 추가**

```csharp
//# IMover 인터페이스 내
bool IsMoving { get; }
```

- [ ] **Step 2: SimpleMover.IsMoving 구현**

`SimpleMover` 가 현재 목적지로 이동 중인지 추적. `MoveTo` 호출 시 `_isMoving = true`, `Stop` 또는 목적지 도달 시 `false`. 프로퍼티 `public bool IsMoving => _isMoving;`

- [ ] **Step 3: FakeMover.IsMoving 구현**

```csharp
public bool IsMoving { get; set; }
```

### Task 4: IAttacker.OnHit

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/CommonInterface.cs`
- Modify: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Helpers/FakeAttacker.cs`

- [ ] **Step 1: IAttacker 에 OnHit 추가**

```csharp
//# IAttacker 인터페이스 내 (using System; 필요)
event Action<IHealth> OnHit;
```

- [ ] **Step 2: MeleeAttacker.OnHit 발행**

`event Action<IHealth> OnHit;` 필드 추가. `TryAttack` 의 `target.TakeDamage` 직후 `OnHit?.Invoke(target);`

- [ ] **Step 3: FakeAttacker.OnHit**

```csharp
public event Action<IHealth> OnHit;
public void RaiseOnHit(IHealth t) => OnHit?.Invoke(t);
```

### Task 5: 오버레이 스케일

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/Health.cs`
- Modify: `Assets/_Lair/Scripts/Character/MeleeAttacker.cs`
- Test: `Assets/_Lair/Tests/EditMode/Character/OverlayScaleTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Character;
using UnityEngine;

namespace Lair.Tests.Character
{
    public class OverlayScaleTests
    {
        [Test]
        public void Health_DamageTakenScale_데미지에_곱셈_적용()
        {
            var go = new GameObject("h");
            var h = go.AddComponent<Health>();
            h.SetMax(100);
            h.DamageTakenScale = 0.5f;
            h.TakeDamage(40);   //# 실제 20
            Assert.AreEqual(80, h.Current);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void MeleeAttacker_PowerScale_데미지에_곱셈_적용()
        {
            var go = new GameObject("a");
            var atk = go.AddComponent<MeleeAttacker>();
            atk.Configure(2f, 0f, 100);
            atk.PowerScale = 0.5f;
            var target = new Lair.Tests.Helpers.FakeHealth();
            target.SetMax(1000);
            atk.TryAttack(target, Vector3.zero, Vector3.zero, 100f);
            Assert.AreEqual(50, target.LastDamage);
            Object.DestroyImmediate(go);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — 컴파일 에러 (프로퍼티 미정의)

- [ ] **Step 3: Health 에 DamageTakenScale**

```csharp
//# 글로벌 디버프 오버레이 — MonsterBuffService 가 매 tick 설정. 기본 1.0.
public float DamageTakenScale { get; set; } = 1f;
```
`TakeDamage` 진입부:
```csharp
public void TakeDamage(int amount)
{
    if (IsAlive == false) return;
    amount = Mathf.RoundToInt(amount * DamageTakenScale);
    int next = Mathf.Max(0, Current - amount);
    // ... 기존
}
```
`OnEnable` 에 리셋 추가: `DamageTakenScale = 1f;`

- [ ] **Step 4: IAttacker 에 PowerScale 추가 + MeleeAttacker 에 CooldownScale / PowerScale**

`PowerScale` 은 `WeakenAura`/`HeroAttackDownAura` 가 `IAttacker` 타입으로 접근하므로 **인터페이스에** 추가:
```csharp
//# IAttacker 인터페이스 내
float PowerScale { get; set; }   //# 무력화/약화 카드용 데미지 배율
```
`CooldownScale` 은 `MonsterBuffService` 가 `MeleeAttacker` 구체 타입으로만 접근하므로 인터페이스 불필요.

`MeleeAttacker`:
```csharp
public float CooldownScale { get; set; } = 1f;
public float PowerScale { get; set; } = 1f;
```
`TryAttack` 수정:
```csharp
if (now - _lastAttackTime < _cooldown * CooldownScale) return false;
target.TakeDamage(Mathf.RoundToInt(_power * PowerScale));
```
`OnEnable` 에 리셋 추가: `CooldownScale = 1f; PowerScale = 1f;`

`FakeAttacker` 에도 `public float PowerScale { get; set; } = 1f;` 추가.

- [ ] **Step 5: 테스트 PASS 확인**

- [ ] **Step 6: 커밋 제안**

```
# [feat] - B3 인터페이스 확장(Heal/IsMoving/OnHit) + 오버레이 배율
```

### Task 6: SpiderSlowOnHit

**Files:**
- Create: `Assets/_Lair/Scripts/Character/SpiderSlowOnHit.cs`
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs`

- [ ] **Step 1: SpiderSlowOnHit 작성**

```csharp
using Lair.Card;
using UnityEngine;

namespace Lair.Character
{
    //# 거미 전용 — 공격 적중 시 영웅에게 짧은 둔화 부착. IAttacker.OnHit 구독.
    [RequireComponent(typeof(MeleeAttacker))]
    public class SpiderSlowOnHit : MonoBehaviour
    {
        [SerializeField] private float _slowFactor = 0.8f;
        [SerializeField] private float _duration = 1.5f;

        private IAttacker _attacker;

        private void Awake() => _attacker = GetComponent<IAttacker>();

        private void OnEnable()
        {
            if (_attacker != null) _attacker.OnHit += HandleHit;
        }

        private void OnDisable()
        {
            if (_attacker != null) _attacker.OnHit -= HandleHit;
        }

        //# 강화 카드(SpiderSlowBoost)가 호출 — 둔화 배율을 더 강하게.
        public void SetSlowFactor(float factor) => _slowFactor = factor;

        private void HandleHit(IHealth target)
        {
            if (target is not MonoBehaviour mb || mb == null) return;
            //# 영웅만 대상 — HeroAuraRunner 보유 여부로 판별
            var runner = mb.GetComponent<HeroAuraRunner>();
            if (runner == null) return;
            runner.Attach(new SlowAura(mb.GetComponent<IMover>(), _slowFactor), _duration);
        }
    }
}
```

- [ ] **Step 2: 거미 프리팹에 SpiderSlowOnHit 부착**

`LairCharacterPrefabBuilder.BuildOne` 의 몬스터 분기에 추가:
```csharp
if (spec.Name == nameof(EMonster.Spider))
    go.AddComponent<SpiderSlowOnHit>();
```

- [ ] **Step 3: 메뉴 재실행 → 거미 프리팹 갱신**

- [ ] **Step 4: 커밋 제안**

```
# [feat] - B3 거미 공격 시 영웅 둔화 (SpiderSlowOnHit)
```

---

## B3-M3: 패시브 8장 효과 클래스

### Task 7: 강화 효과 3개

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/ArcherRangeBoostEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/SpiderSlowBoostEffect.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/BatMoveSpeedBoostEffect.cs`

- [ ] **Step 1: ArcherRangeBoostEffect — 기존 OrcAtkSpeedEffect 패턴 참조**

궁수 `MeleeAttacker._range` ×1.4. `ctx.GetMonsters(EMonster.Archer)` 순회. (B1 강화 효과가 MeleeAttacker 필드를 SerializedObject 가 아닌 런타임에 어떻게 바꾸는지 — 기존 `OrcAtkSpeedEffect` 구현 확인 후 동일 패턴 사용. 런타임 필드 변경용 메서드가 MeleeAttacker 에 있으면 재사용, 없으면 `Configure` 활용.)

```csharp
using System;
using Lair.Character;
using Lair.Data;

namespace Lair.Card
{
    //# 궁수 사거리 +40%. 기존 강화 카드 패턴.
    [Serializable]
    public class ArcherRangeBoostEffect : ICardEffect
    {
        public void Apply(IBattleContext ctx)
        {
            foreach (var h in ctx.GetMonsters(EMonster.Archer))
            {
                if (h is MonoBehaviour mb && mb != null)
                {
                    var atk = mb.GetComponent<MeleeAttacker>();
                    if (atk != null) atk.Configure(atk.Range * 1.4f, atk.Cooldown, atk.Power);
                }
            }
        }
    }
}
```

- [ ] **Step 2: SpiderSlowBoostEffect**

거미들의 `SpiderSlowOnHit.SetSlowFactor(0.6f)` 호출:
```csharp
foreach (var h in ctx.GetMonsters(EMonster.Spider))
    if (h is MonoBehaviour mb && mb != null)
        mb.GetComponent<SpiderSlowOnHit>()?.SetSlowFactor(0.6f);
```

- [ ] **Step 3: BatMoveSpeedBoostEffect**

박쥐 `IMover.Speed ×1.5`:
```csharp
foreach (var h in ctx.GetMonsters(EMonster.Bat))
    if (h is MonoBehaviour mb && mb != null)
    {
        var mover = mb.GetComponent<IMover>();
        if (mover != null) mover.Speed *= 1.5f;
    }
```

### Task 8: 소환 효과 3개

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/SpawnOrcsEffect.cs` / `SpawnSpidersEffect.cs` / `SpawnBatsEffect.cs`

- [ ] **Step 1: 기존 SpawnSlimesEffect 패턴 복제**

각각 `ctx.SpawnMonster(EMonster.X, heroPos)` 반복. SpawnOrcs=2, SpawnSpiders=2, SpawnBats=5.
```csharp
[Serializable]
public class SpawnOrcsEffect : ICardEffect
{
    [SerializeField] private int _count = 2;
    public void Apply(IBattleContext ctx)
    {
        var heroT = ctx.GetHeroTransform();
        if (heroT == null) return;
        for (int i = 0; i < _count; ++i)
            ctx.SpawnMonster(Lair.Data.EMonster.Orc, heroT.position);
    }
}
```
(`SpawnSpidersEffect` _count=2 / Spider, `SpawnBatsEffect` _count=5 / Bat)

### Task 9: 교체 효과 1개

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/ReplaceOrcsToArchersEffect.cs`

- [ ] **Step 1: 기존 ReplaceSlimesToGolemEffect 패턴 참조 — 1:1 교체**

오크 스냅샷 → 각 오크 위치에 궁수 1마리 스폰 + 오크 제거. (기존 `ReplaceSlimesToGolemEffect` 의 Push/스폰 방식 그대로, 비율만 1:1)

### Task 10: 환경 효과 1개 + HeroAttackDownAura

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Auras/HeroAttackDownAura.cs`
- Create: `Assets/_Lair/Scripts/Card/Effects/HeroAttackDownEffect.cs`

- [ ] **Step 1: HeroAttackDownAura**

```csharp
using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 공격력 영구 -25%. 무제한 지속 (HeroAuraRunner duration<0).
    [Serializable]
    public class HeroAttackDownAura : IHeroAura
    {
        private readonly IAttacker _attacker;
        private readonly float _factor;
        private bool _applied;

        public HeroAttackDownAura(IAttacker attacker, float factor = 0.75f)
        {
            _attacker = attacker;
            _factor = factor;
        }

        public void OnAttached(IHealth hero)
        {
            if (_attacker == null || _applied) return;
            _attacker.PowerScale *= _factor;
            _applied = true;
        }

        public void Tick(IHealth hero, float dt) { }

        public void OnDetached(IHealth hero)
        {
            //# 무제한 효과 — 정상 흐름에선 Detach 안 됨. 풀 회수 시엔 OnEnable 리셋이 복원.
        }
    }
}
```

- [ ] **Step 2: HeroAttackDownEffect**

```csharp
[Serializable]
public class HeroAttackDownEffect : ICardEffect
{
    public void Apply(IBattleContext ctx)
    {
        var heroT = ctx.GetHeroTransform();
        if (heroT == null) return;
        var atk = heroT.GetComponent<Lair.Character.IAttacker>();
        if (atk == null) return;
        ctx.ApplyHeroAura(new HeroAttackDownAura(atk), durationSeconds: -1f);
    }
}
```

### Task 11: M3 EditMode 테스트

**Files:**
- Test: `Assets/_Lair/Tests/EditMode/Card/B3PassiveEffectTests.cs`

- [ ] **Step 1: POCO 가능한 효과 테스트**

`HeroAttackDownAura` 는 FakeAttacker 로 PowerScale 검증:
```csharp
[Test]
public void HeroAttackDownAura_OnAttached_PowerScale_0_75배()
{
    var atk = new FakeAttacker { PowerScale = 1f };
    var aura = new HeroAttackDownAura(atk, 0.75f);
    aura.OnAttached(new FakeHealth());
    Assert.AreEqual(0.75f, atk.PowerScale, 0.001f);
}
```
(`FakeAttacker` 에 `PowerScale` 프로퍼티 추가 필요 — Task 4 에서 누락 시 여기서 보강)

- [ ] **Step 2: 테스트 PASS + 커밋 제안**

```
# [feat] - B3 패시브 카드 효과 8종 (강화/소환/교체/환경)
```

---

## B3-M4: 몬스터 글로벌 버프

### Task 12: EMonsterBuff + MonsterBuffService

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Create: `Assets/_Lair/Scripts/Battle/MonsterBuffService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/MonsterBuffServiceTests.cs`

- [ ] **Step 1: EMonsterBuff enum 추가 (CommonEnum.cs)**

```csharp
//# B3 — 몬스터 글로벌 버프 종류
public enum EMonsterBuff { Frenzy, IronWill, BerserkPower }
```

- [ ] **Step 2: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    public class MonsterBuffServiceTests
    {
        [Test]
        public void AddBuff_후_Tick_하면_활성()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.Frenzy, 10f);
            Assert.IsTrue(svc.IsActive(EMonsterBuff.Frenzy));
        }

        [Test]
        public void 지속시간_경과_후_만료()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.Frenzy, 5f);
            svc.Tick(6f);
            Assert.IsFalse(svc.IsActive(EMonsterBuff.Frenzy));
        }

        [Test]
        public void 같은_버프_재부착_시_지속시간_연장()
        {
            var svc = new MonsterBuffService();
            svc.AddBuff(EMonsterBuff.IronWill, 5f);
            svc.Tick(3f);
            svc.AddBuff(EMonsterBuff.IronWill, 5f);
            svc.Tick(4f);
            Assert.IsTrue(svc.IsActive(EMonsterBuff.IronWill));   //# 연장됐으면 아직 활성
        }
    }
}
```

- [ ] **Step 3: MonsterBuffService 구현**

```csharp
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 모든 몬스터에 글로벌 버프를 매 tick 재적용. HeroAuraRunner 의 몬스터판.
    public class MonsterBuffService
    {
        private class Buff { public EMonsterBuff Type; public float Remain; }

        private readonly List<Buff> _buffs = new();

        public bool IsActive(EMonsterBuff type)
        {
            foreach (var b in _buffs) if (b.Type == type) return true;
            return false;
        }

        //# 같은 type 이 있으면 Remain 을 더 큰 값으로 연장.
        public void AddBuff(EMonsterBuff type, float duration)
        {
            foreach (var b in _buffs)
            {
                if (b.Type == type) { b.Remain = Mathf.Max(b.Remain, duration); return; }
            }
            _buffs.Add(new Buff { Type = type, Remain = duration });
        }

        //# 1) 만료 제거 2) 전체 몬스터 스케일 base 리셋 3) 활성 버프 곱셈 적용
        public void Tick(float dt)
        {
            for (int i = _buffs.Count - 1; i >= 0; --i)
            {
                _buffs[i].Remain -= dt;
                if (_buffs[i].Remain <= 0f) _buffs.RemoveAt(i);
            }

            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null) continue;
                var atk = e.Transform.GetComponent<MeleeAttacker>();
                var hp  = e.Transform.GetComponent<Health>();
                if (atk != null) { atk.CooldownScale = 1f; atk.PowerScale = 1f; }
                if (hp  != null) hp.DamageTakenScale = 1f;

                foreach (var b in _buffs)
                {
                    switch (b.Type)
                    {
                        case EMonsterBuff.Frenzy:
                            if (atk != null) atk.CooldownScale *= 0.67f;
                            break;
                        case EMonsterBuff.IronWill:
                            if (hp != null) hp.DamageTakenScale *= 0.7f;
                            break;
                        case EMonsterBuff.BerserkPower:
                            if (atk != null) atk.PowerScale *= 3f;
                            break;
                    }
                }
            }
        }
    }
}
```

- [ ] **Step 4: 테스트 PASS 확인**

### Task 13: BloodThirstService

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/BloodThirstService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/BloodThirstServiceTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    public class BloodThirstServiceTests
    {
        [Test]
        public void 활성_중에는_IsActive_true()
        {
            var svc = new BloodThirstService();
            svc.Activate(30f);
            Assert.IsTrue(svc.IsActive);
        }

        [Test]
        public void 지속시간_경과_후_비활성()
        {
            var svc = new BloodThirstService();
            svc.Activate(30f);
            svc.Tick(31f);
            Assert.IsFalse(svc.IsActive);
        }
    }
}
```

- [ ] **Step 2: BloodThirstService 구현**

```csharp
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 활성 30초간 몬스터 사망 시 사망 위치 주변 몬스터 HP +30.
    public class BloodThirstService
    {
        private const float HealRadius = 3f;
        private const int HealAmount = 30;

        private float _remain;
        public bool IsActive => _remain > 0f;

        public void Activate(float duration) => _remain = Mathf.Max(_remain, duration);

        public void Tick(float dt)
        {
            if (_remain > 0f) _remain -= dt;
        }

        //# Health.OnDied 구독으로 BattleController 가 호출 — 사망 위치 전달.
        public void NotifyMonsterDied(Vector3 pos)
        {
            if (IsActive == false) return;
            float sqr = HealRadius * HealRadius;
            foreach (var e in CharacterRegistry.Monsters)
            {
                if (e?.Transform == null || e.Health == null || !e.Health.IsAlive) continue;
                if ((e.Transform.position - pos).sqrMagnitude <= sqr)
                    e.Health.Heal(HealAmount);
            }
        }
    }
}
```

- [ ] **Step 3: 테스트 PASS 확인**

### Task 14: 몬스터 액티브 효과 4개

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/FrenzyEffect.cs` / `IronWillEffect.cs` / `BloodThirstEffect.cs` / `BerserkEffect.cs`

- [ ] **Step 1: IBattleContext 확장 검토**

이 효과들은 `MonsterBuffService`/`BloodThirstService` 에 접근해야 함. `IBattleContext` 에 추가:
```csharp
void AddMonsterBuff(EMonsterBuff type, float duration);
void ActivateBloodThirst(float duration);
void HalveAllMonsterHp();   //# Berserk 즉발 HP -50%
```
`BattleContext` 에서 `_owner` 의 서비스로 위임 (M6 에서 BattleController 가 서비스 보유).

- [ ] **Step 2: FrenzyEffect / IronWillEffect**

```csharp
[Serializable]
public class FrenzyEffect : ICardEffect
{
    [SerializeField] private float _duration = 10f;
    public void Apply(IBattleContext ctx) => ctx.AddMonsterBuff(Lair.Data.EMonsterBuff.Frenzy, _duration);
}
//# IronWillEffect — EMonsterBuff.IronWill, _duration = 15f
```

- [ ] **Step 3: BloodThirstEffect**

```csharp
[Serializable]
public class BloodThirstEffect : ICardEffect
{
    [SerializeField] private float _duration = 30f;
    public void Apply(IBattleContext ctx) => ctx.ActivateBloodThirst(_duration);
}
```

- [ ] **Step 4: BerserkEffect — 즉발 HP 절감 + BerserkPower 버프**

```csharp
[Serializable]
public class BerserkEffect : ICardEffect
{
    [SerializeField] private float _duration = 15f;
    public void Apply(IBattleContext ctx)
    {
        ctx.HalveAllMonsterHp();
        ctx.AddMonsterBuff(Lair.Data.EMonsterBuff.BerserkPower, _duration);
    }
}
```

- [ ] **Step 5: 커밋 제안**

```
# [feat] - B3 몬스터 글로벌 버프 시스템 + 액티브 효과 4종
```

---

## B3-M5: 영웅 액티브 + SO 재빌드

### Task 15: AutoCombatAI FleeMode

**Files:**
- Modify: `Assets/_Lair/Scripts/Character/AutoCombatAI.cs`

- [ ] **Step 1: FleeMode 프로퍼티 + 도주 로직**

```csharp
public bool FleeMode { get; set; }
```
`Update` 의 타겟 처리부 수정 — `FleeMode` 면 nearest 의 **반대 방향**으로 이동, 공격 안 함:
```csharp
if (FleeMode)
{
    Vector3 away = transform.position + (transform.position - t.position).normalized * 5f;
    _mover.MoveTo(away);
    return;
}
//# ... 기존 거리 분기
```
`OnEnable` 에 `FleeMode = false;` 리셋 (풀 재사용 대비).

### Task 16: 영웅 오라 4종

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Auras/FearAura.cs` / `BleedAura.cs` / `WeakenAura.cs` / `TimeStopAura.cs`

- [ ] **Step 1: FearAura**

```csharp
using System;
using Lair.Character;

namespace Lair.Card
{
    //# 영웅 AutoCombatAI 도주 모드 ON. OnDetached 시 복원.
    [Serializable]
    public class FearAura : IHeroAura
    {
        private readonly AutoCombatAI _ai;
        public FearAura(AutoCombatAI ai) => _ai = ai;
        public void OnAttached(IHealth hero) { if (_ai != null) _ai.FleeMode = true; }
        public void Tick(IHealth hero, float dt) { }
        public void OnDetached(IHealth hero) { if (_ai != null) _ai.FleeMode = false; }
    }
}
```

- [ ] **Step 2: BleedAura**

```csharp
//# 영웅 이동 중일 때만 1초당 Max×_ratio 데미지.
[Serializable]
public class BleedAura : IHeroAura
{
    private readonly IMover _mover;
    private readonly float _ratio;   //# 0.02 = 2%
    private float _acc;
    public BleedAura(IMover mover, float ratio = 0.02f) { _mover = mover; _ratio = ratio; }
    public void OnAttached(IHealth hero) { _acc = 0f; }
    public void Tick(IHealth hero, float dt)
    {
        if (hero == null || _mover == null || !_mover.IsMoving) return;
        _acc += dt;
        while (_acc >= 1f) { _acc -= 1f; hero.TakeDamage((int)(hero.Max * _ratio)); }
    }
    public void OnDetached(IHealth hero) { }
}
```

- [ ] **Step 3: WeakenAura**

```csharp
//# 영웅 공격력 ×_factor. OnDetached 시 복원.
[Serializable]
public class WeakenAura : IHeroAura
{
    private readonly IAttacker _attacker;
    private readonly float _factor;
    private float _backup;
    public WeakenAura(IAttacker attacker, float factor = 0.5f) { _attacker = attacker; _factor = factor; }
    public void OnAttached(IHealth hero)
    {
        if (_attacker == null) return;
        _backup = _attacker.PowerScale;
        _attacker.PowerScale = _backup * _factor;
    }
    public void Tick(IHealth hero, float dt) { }
    public void OnDetached(IHealth hero) { if (_attacker != null) _attacker.PowerScale = _backup; }
}
```

- [ ] **Step 4: TimeStopAura**

```csharp
//# 영웅 이동·공격 완전 정지. OnDetached 시 복원.
[Serializable]
public class TimeStopAura : IHeroAura
{
    private readonly IMover _mover;
    private readonly IAttacker _attacker;
    private float _speedBackup;
    private bool _atkBackup;
    public TimeStopAura(IMover mover, IAttacker attacker) { _mover = mover; _attacker = attacker; }
    public void OnAttached(IHealth hero)
    {
        if (_mover != null) { _speedBackup = _mover.Speed; _mover.Speed = 0f; _mover.Stop(); }
        if (_attacker != null) { _atkBackup = _attacker.Enabled; _attacker.Enabled = false; }
    }
    public void Tick(IHealth hero, float dt) { }
    public void OnDetached(IHealth hero)
    {
        if (_mover != null) _mover.Speed = _speedBackup;
        if (_attacker != null) _attacker.Enabled = _atkBackup;
    }
}
```

### Task 17: 영웅 액티브 효과 6개

**Files:**
- Create: `Assets/_Lair/Scripts/Card/Effects/FearEffect.cs` / `BleedEffect.cs` / `WeakenEffect.cs` / `SlowEffect.cs` / `MultiplyEffect.cs` / `TimeStopEffect.cs`

- [ ] **Step 1: FearEffect / BleedEffect / WeakenEffect / TimeStopEffect**

각각 영웅 컴포넌트 획득 후 `ctx.ApplyHeroAura`. 패턴 동일 — 예:
```csharp
[Serializable]
public class FearEffect : ICardEffect
{
    [SerializeField] private float _duration = 3f;
    public void Apply(IBattleContext ctx)
    {
        var heroT = ctx.GetHeroTransform();
        if (heroT == null) return;
        var ai = heroT.GetComponent<Lair.Character.AutoCombatAI>();
        if (ai == null) return;
        ctx.ApplyHeroAura(new FearAura(ai), _duration);
    }
}
```
- `BleedEffect` _duration=10 → `BleedAura(ctx.GetHeroMover(), 0.02f)`
- `WeakenEffect` _duration=10 → `WeakenAura(hero IAttacker, 0.5f)`
- `TimeStopEffect` _duration=5 → `TimeStopAura(hero IMover, hero IAttacker)`

- [ ] **Step 2: SlowEffect — B2 HeroSlowEffect 재명명/재활용**

기존 `HeroSlowEffect` 를 `SlowEffect` 로 — 수치 `_factor=0.5f`, `_duration=10f`. (파일명·클래스명 변경)

- [ ] **Step 3: MultiplyEffect — 즉발 최다 종 2배**

```csharp
[Serializable]
public class MultiplyEffect : ICardEffect
{
    public void Apply(IBattleContext ctx)
    {
        //# EMonster 별 집계 → 최다 종 찾기
        var counts = new System.Collections.Generic.Dictionary<Lair.Data.EMonster, int>();
        foreach (Lair.Data.EMonster key in System.Enum.GetValues(typeof(Lair.Data.EMonster)))
        {
            int c = 0;
            foreach (var _ in ctx.GetMonsters(key)) c++;
            if (c > 0) counts[key] = c;
        }
        if (counts.Count == 0) return;
        Lair.Data.EMonster top = default; int max = 0;
        foreach (var kv in counts) if (kv.Value > max) { max = kv.Value; top = kv.Key; }

        var heroT = ctx.GetHeroTransform();
        Vector3 pos = heroT != null ? heroT.position : Vector3.zero;
        for (int i = 0; i < max; ++i) ctx.SpawnMonster(top, pos);
    }
}
```

### Task 18: ECardId 재정의 + B2 폐기 + LairCardPrefabBuilder

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Delete: `MonsterAoeDamageEffect.cs` / `InstantSpawnGolemEffect.cs` / `InstantSpawnSlimesEffect.cs` / `HeroSilenceEffect.cs` / `HeroSlowEffect.cs` (+ .meta)
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: ECardId 최종형 (설계서 §7.2)**

```csharp
public enum ECardId
{
    //# 패시브 15장
    SlimeHpBoost, GolemDamageBoost, OrcAtkSpeed,
    ArcherRangeBoost, SpiderSlowBoost, BatMoveSpeedBoost,
    SpawnSlimes, SpawnGolem, SpawnOrcs, SpawnSpiders, SpawnBats,
    ReplaceSlimesToGolem, ReplaceOrcsToArchers,
    HeroPoisonAura, HeroAttackDown,
    //# 액티브 10장
    Fear, Bleed, Weaken, Slow,
    Frenzy, Multiply, BloodThirst, IronWill,
    TimeStop, Berserk,
}
```

- [ ] **Step 2: B2 폐기 효과 클래스 4개 + HeroSlowEffect 삭제** (SlowEffect 로 대체됨)

- [ ] **Step 3: LairCardPrefabBuilder — PassiveSpecs 15 / ActiveSpecs 10**

기존 `AllSpecs` → `PassiveSpecs` (15장), `ActiveSpecs` → 10장으로 전면 교체. 각 Spec 의 EffectFactory 를 신규 효과로 연결. `BuildCardsAndPool` 헬퍼 재사용. 메뉴 `B1 - Build Card Assets` → `B3 - Rebuild All Cards` 로 통합 (패시브+액티브 동시).

- [ ] **Step 4: 컴파일 확인**

### Task 19: 카드 SO 25장 재빌드

- [ ] **Step 1: 메뉴 `Lair/Setup/B3 - Rebuild All Cards` 실행**

`Assets/_Lair/Data/Cards/Cards/` 에 25장 + `CardPool_Passive`(15) / `CardPool_Active`(10) 재생성 확인.

- [ ] **Step 2: B3ActiveEffectTests 작성 + PASS**

POCO 가능 오라(WeakenAura/TimeStopAura/FearAura/BleedAura) FakeXxx 로 검증.

- [ ] **Step 3: 커밋 제안**

```
# [feat] - B3 영웅 액티브 효과 6종 + ECardId 재정의 + 카드 SO 25장 재빌드
```

---

## B3-M6: 통합 + 검증

### Task 20: BattleController 통합

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleContext.cs`

- [ ] **Step 1: 풀 워밍 6종**

`PrewarmPools` 의 몬스터 배열에 `Archer`/`Spider`/`Bat` 추가.

- [ ] **Step 2: MonsterBuffService / BloodThirstService 인스턴스화 + Tick**

```csharp
private MonsterBuffService _monsterBuffs;
private BloodThirstService _bloodThirst;
```
`Start` 에서 생성. `Update` 에서:
```csharp
_monsterBuffs?.Tick(Time.deltaTime);
_bloodThirst?.Tick(Time.deltaTime);
```
몬스터 사망 시 `_bloodThirst.NotifyMonsterDied(pos)` — `Health.OnDied` 구독 또는 `DespawnOnDeath` 연동.

- [ ] **Step 3: BattleContext 위임 메서드 구현**

`AddMonsterBuff` / `ActivateBloodThirst` / `HalveAllMonsterHp` 를 `_owner` 의 서비스로 위임. BattleController 에 public 접근 메서드 추가.

- [ ] **Step 4: 컴파일 + 콘솔 에러 0 확인**

### Task 21: PlayMode 스모크 + 수동 검증

**Files:**
- Create: `Assets/_Lair/Tests/PlayMode/B3SmokeTest.cs`

- [ ] **Step 1: PlayMode 스모크 — 6종 몬스터 풀 로드 + 카드 25장 풀 로드 확인**

- [ ] **Step 2: 수동 검증 체크리스트 (사용자)**

설계서 §10 성공 기준 11항목.

- [ ] **Step 3: 커밋 제안**

```
# [feat] - B3 BattleController 통합 + 6종 몬스터 풀 워밍 + PlayMode 스모크
```

---

## 자기 검토 (Self-Review)

**Spec 커버리지:**
- §2 몬스터 3종 → Task 1, 6 ✓
- §3 패시브 8장 → Task 7~11 ✓
- §4 인터페이스/오버레이 → Task 2~5 ✓
- §5 글로벌 버프 → Task 12~14 ✓
- §6 액티브 10장 → Task 14, 16~17 ✓
- §7 폐기/재빌드 → Task 18~19 ✓

**플레이스홀더 스캔:** B1 강화 효과의 런타임 필드 변경 패턴은 Task 7 Step 1 에서 기존 코드 확인 후 적용 — 구현자가 `OrcAtkSpeedEffect` 를 먼저 읽어야 함. 그 외 TBD 없음.

**타입 일관성:** `PowerScale`/`CooldownScale`/`DamageTakenScale` (Task 5) ↔ MonsterBuffService (Task 12) ↔ WeakenAura/HeroAttackDownAura ✓. `FakeAttacker.PowerScale` 는 Task 4/11 에서 보강 명시 ✓.
