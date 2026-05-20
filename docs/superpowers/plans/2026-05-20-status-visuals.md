# 영웅 디버프 상태 표시 비주얼 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 영웅 디버프 6종(둔화/공포/무력화/약화/시간정지/출혈)을 프리미티브 부착물로 시각 표시한다.

**Architecture:** `IStatusVisual` sibling 인터페이스를 6개 Aura 가 구현, `HeroAuraRunner` 가 visual 을 중앙에서 Pop/추적/Push. visual 은 root 레벨 + 매 프레임 영웅 추적.

**Tech Stack:** Unity 2022.3 / URP / ChvjPackage(CHMResource·CHMPool)

설계서: `docs/superpowers/specs/2026-05-20-status-visuals-design.md`

---

## 파일 구조
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EVisual +6)
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs` (IStatusVisual)
- Modify: `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs` (제네릭화 + 6종)
- Modify: `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs` (Slot.Visual 중앙 관리)
- Modify: `Assets/_Lair/Scripts/Card/Auras/SlowAura.cs` / `FearAura.cs` / `WeakenAura.cs` / `HeroAttackDownAura.cs` / `TimeStopAura.cs` / `BleedAura.cs` (IStatusVisual 구현)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (PrewarmPools)

---

## M1: Enum / 인터페이스 / 프리팹

### Task 1: EVisual 확장 + IStatusVisual

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs`

- [ ] **Step 1: EVisual 6값 추가**

```csharp
public enum EVisual
{
    PoisonAura,
    SlowStatus,
    FearStatus,
    WeakenStatus,
    AttackDownStatus,
    TimeStopStatus,
    BleedStatus,
}
```

- [ ] **Step 2: IStatusVisual 추가 (Card/CommonInterface.cs)**

```csharp
//# 영웅 추적 상태 visual 을 노출하는 sibling 인터페이스 (IHeroAura 와 별개).
public interface IStatusVisual
{
    EVisual VisualKey { get; }
    Vector3 Offset { get; }
}
```
(`using Lair.Data;` `using UnityEngine;` 이미 존재 확인)

- [ ] **Step 3: 컴파일 확인 + 커밋 제안**

```
# [feat] - 상태 visual EVisual 6값 + IStatusVisual 인터페이스
```

### Task 2: LairVisualPrefabBuilder 제네릭화 + visual 6종

**Files:**
- Modify: `Assets/_Lair/Editor/LairVisualPrefabBuilder.cs`

- [ ] **Step 1: VisualSpec + BuildVisual 일반화**

기존 `BuildPoisonAura` 하드코딩을 `VisualSpec` 배열 + `BuildVisual(spec)` 로 리팩터:
```csharp
public class VisualSpec
{
    public EVisual Key;
    public PrimitiveType Mesh;
    public string ColorHex;
    public float Alpha;
    public float Scale;
}

public static readonly VisualSpec[] StatusSpecs = new[]
{
    new VisualSpec { Key = EVisual.SlowStatus,       Mesh = PrimitiveType.Sphere, ColorHex = "#0EA5E9", Alpha = 0.5f, Scale = 0.4f  },
    new VisualSpec { Key = EVisual.FearStatus,       Mesh = PrimitiveType.Cube,   ColorHex = "#A855F7", Alpha = 1.0f, Scale = 0.3f  },
    new VisualSpec { Key = EVisual.WeakenStatus,     Mesh = PrimitiveType.Cube,   ColorHex = "#6B7280", Alpha = 1.0f, Scale = 0.3f  },
    new VisualSpec { Key = EVisual.AttackDownStatus, Mesh = PrimitiveType.Cube,   ColorHex = "#7F1D1D", Alpha = 1.0f, Scale = 0.25f },
    new VisualSpec { Key = EVisual.TimeStopStatus,   Mesh = PrimitiveType.Sphere, ColorHex = "#E5E7EB", Alpha = 0.3f, Scale = 1.5f  },
    new VisualSpec { Key = EVisual.BleedStatus,      Mesh = PrimitiveType.Sphere, ColorHex = "#DC2626", Alpha = 1.0f, Scale = 0.25f },
};
```

`BuildVisual(spec, settings, group)`:
- `GameObject.CreatePrimitive(spec.Mesh)`, name = `spec.Key.ToString()`, localScale = `Vector3.one * spec.Scale`
- Collider 제거
- URP Lit 머티리얼 — `spec.ColorHex` 파싱, alpha = `spec.Alpha`
- **`spec.Alpha < 1f` 이면 Transparent 셋업:**
  ```csharp
  mat.SetFloat("_Surface", 1f);          //# Transparent
  mat.SetFloat("_Blend", 0f);            //# Alpha blend
  mat.SetOverrideTag("RenderType", "Transparent");
  mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
  mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
  ```
  색 적용 시 `new Color(r,g,b, spec.Alpha)` 로 alpha 포함
- 프리팹 저장 + Addressables 등록 (address = `spec.Key`, label = Resource) — 기존 `BuildPoisonAura` 패턴

- [ ] **Step 2: 메뉴에서 StatusSpecs 6종 빌드**

`BuildAllVisuals` 메뉴(`Lair/Setup/B1 - Build Visual Prefabs`)가 PoisonAura + StatusSpecs 6종 모두 빌드하도록.

- [ ] **Step 3: 메뉴 실행 → 7개 visual 프리팹 확인**

`Assets/_Lair/Prefabs/FX/` 에 PoisonAura + SlowStatus 등 6종.

- [ ] **Step 4: 커밋 제안**

```
# [feat] - 상태 visual 프리팹 6종 + LairVisualPrefabBuilder 제네릭화
```

---

## M2: HeroAuraRunner 중앙 관리 + Aura

### Task 3: HeroAuraRunner Slot.Visual

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/HeroAuraRunner.cs`

- [ ] **Step 1: Slot 에 Visual 필드 + Attach visual Pop**

```csharp
private class Slot
{
    public IHeroAura Aura;
    public float Remain;
    public bool Indefinite;
    public CHPoolable Visual;   //# IStatusVisual 인 경우만
}
```

`Attach` 의 신규 슬롯 생성 후:
```csharp
var slot = new Slot { Aura = aura, Remain = duration, Indefinite = duration < 0f };
_slots.Add(slot);
aura.OnAttached(_hero);

//# 상태 visual 이 있으면 풀에서 Pop (root 레벨).
if (aura is IStatusVisual sv)
{
    CHMResource.Instance.Load<GameObject>(sv.VisualKey, prefab =>
    {
        if (prefab == null) return;
        var poolable = CHMPool.Instance.Pop(prefab, null);
        if (poolable == null) return;
        //# 콜백 도착 시 슬롯이 이미 사라졌으면 즉시 반환.
        if (_slots.Contains(slot)) slot.Visual = poolable;
        else                       CHMPool.Instance.Push(poolable);
    });
}
```
(`aura.OnAttached` 호출 위치는 기존대로 — slot 추가 순서만 조정)

- [ ] **Step 2: Update 에서 visual 위치 추적**

```csharp
var s = _slots[i];
s.Aura.Tick(_hero, Time.deltaTime);
if (s.Visual != null && s.Aura is IStatusVisual sv)
    s.Visual.transform.position = transform.position + sv.Offset;
```

- [ ] **Step 3: 만료 시 visual Push**

```csharp
if (s.Remain <= 0f)
{
    s.Aura.OnDetached(_hero);
    if (s.Visual != null) CHMPool.Instance.Push(s.Visual);
    _slots.RemoveAt(i);
}
```

- [ ] **Step 4: OnDisable 에서 visual Push**

```csharp
private void OnDisable()
{
    for (int i = _slots.Count - 1; i >= 0; --i)
    {
        try { _slots[i].Aura.OnDetached(_hero); } catch { }
        if (_slots[i].Visual != null) CHMPool.Instance.Push(_slots[i].Visual);
    }
    _slots.Clear();
}
```

(`using ChvjUnityInfra;` 추가 확인 — CHMResource/CHMPool/CHPoolable)

### Task 4: 6개 Aura 에 IStatusVisual 구현

**Files:**
- Modify: `SlowAura.cs` / `FearAura.cs` / `WeakenAura.cs` / `HeroAttackDownAura.cs` / `TimeStopAura.cs` / `BleedAura.cs`

- [ ] **Step 1: 각 Aura 클래스 선언에 IStatusVisual 추가 + 멤버 구현**

```csharp
//# SlowAura
public class SlowAura : IHeroAura, IStatusVisual
{
    public EVisual VisualKey => EVisual.SlowStatus;
    public Vector3 Offset => new Vector3(0f, 0.05f, 0f);
    // ... 기존 멤버
}
```

각 Aura 의 VisualKey / Offset (설계서 §3 표):
| Aura | VisualKey | Offset |
|---|---|---|
| SlowAura | SlowStatus | (0, 0.05, 0) |
| FearAura | FearStatus | (0, 1.3, 0) |
| WeakenAura | WeakenStatus | (-0.5, 0.6, 0) |
| HeroAttackDownAura | AttackDownStatus | (0.5, 0.6, 0) |
| TimeStopAura | TimeStopStatus | (0, 0.5, 0) |
| BleedAura | BleedStatus | (0.4, 0.05, 0) |

`using Lair.Data;` (EVisual) / `using UnityEngine;` (Vector3) 각 파일에 확인.

- [ ] **Step 2: 컴파일 확인**

### Task 5: BattleController PrewarmPools

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: status visual 6종 워밍 추가**

`PrewarmPools` 의 PoisonAura 워밍 근처에:
```csharp
foreach (var key in new[] { EVisual.SlowStatus, EVisual.FearStatus, EVisual.WeakenStatus,
                            EVisual.AttackDownStatus, EVisual.TimeStopStatus, EVisual.BleedStatus })
{
    var fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
    if (fx != null) CHMPool.Instance.CreatePool(fx, count: 2);
}
```

- [ ] **Step 2: 컴파일 + EditMode 회귀 PASS + 커밋 제안**

```
# [feat] - HeroAuraRunner 상태 visual 중앙 관리 + 6개 Aura IStatusVisual
```

---

## M3: 검증

### Task 6: 통합 검증

- [ ] **Step 1: MCP refresh + 컴파일 에러 0 확인**

- [ ] **Step 2: 메뉴 `Lair/Setup/B1 - Build Visual Prefabs` 재실행** — 7개 visual 확인

- [ ] **Step 3: PlayMode 진입 — 디버프 카드 적용 시 visual 등장/추적/소멸 확인**

설계서 §8 성공 기준 5항목.

- [ ] **Step 4: 사용자 수동 검증 + 커밋 제안**

---

## 자기 검토 (Self-Review)

**Spec 커버리지:** §2 IStatusVisual/Runner → Task 1·3·4 / §3 프리팹 → Task 2 / §4 워밍 → Task 5 ✓

**플레이스홀더:** Offset 전부 const 값 명시 ✓. TBD 없음.

**타입 일관성:** `IStatusVisual.VisualKey`(EVisual) / `Offset`(Vector3) ↔ HeroAuraRunner Update 추적 ↔ 6개 Aura 구현 ✓. `Slot.Visual`(CHPoolable) ↔ CHMPool.Pop/Push ✓.
