# 영웅 디버프 상태 표시 비주얼 설계서

> Project Lair — MVP 검증 보조. 영웅에 걸린 디버프를 화면에 시각 피드백.
> 작성일: 2026-05-20
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
카드 효과(특히 영웅 디버프)가 적용됐을 때 **화면에서 무슨 일이 일어났는지 보이게** 하여,
밸런싱·페이싱 검증의 질을 높인다.

### 0.2 In Scope
- 영웅 디버프 6종의 시각 표시 (프리미티브 부착물):
  둔화 / 공포 / 무력화 / 약화의 저주 / 시간정지 / 출혈
- `IStatusVisual` 인터페이스 + `HeroAuraRunner` 중앙 visual 관리
- visual 프리팹 6종 + `LairVisualPrefabBuilder` 제네릭화

### 0.3 Out of Scope
- **아이콘 UI 방식** (영웅 머리 위 아이콘) — 향후 교체 예정 (§6)
- 몬스터 글로벌 버프 표시 (광폭화/강철의지/폭주)
- 독 장판(PoisonAura) — 이미 자체 visual 보유, 변경 없음
- 사운드

### 0.4 검증 가설
"상태가 눈에 보이면 한 판 플레이로 카드 효과·페이싱을 판단하기 쉬워지는가."

---

## 1. 프로젝트 룰 매핑

| 룰 | 적용 |
|---|---|
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | Aura 는 visual 을 직접 관리 안 함 — IStatusVisual 노출만, HeroAuraRunner 가 처리 |
| 04 프리팹화 | visual 프리팹 6종 |
| 07 ChvjPackage | CHMResource/CHMPool 재사용 |
| 08 Enum 키 | EVisual 값명 = 프리팹 파일명 |
| 09 CommonEnum | EVisual 6값 추가 → CommonEnum.cs |
| 10 CommonInterface | IStatusVisual → Card/CommonInterface.cs |
| 12 CHMPool 스폰 | visual 은 전부 CHMPool.Pop/Push |

---

## 2. 아키텍처

### 2.1 IStatusVisual — sibling 인터페이스
`IHeroAura` 는 건드리지 않는다. 영웅 추적 visual 이 필요한 Aura 만 추가로 구현:

```csharp
//# Card/CommonInterface.cs
public interface IStatusVisual
{
    EVisual VisualKey { get; }   //# CHMResource 로 로드할 프리팹 키
    Vector3 Offset { get; }      //# 영웅 위치 기준 상대 오프셋
}
```

- 6개 디버프 Aura → `IHeroAura` + `IStatusVisual` 둘 다 구현
- `PoisonAura` → `IStatusVisual` 미구현 (장판은 영웅을 안 따라감, 자체 관리 유지)

### 2.2 HeroAuraRunner 중앙 관리
`Slot` 에 visual 상태 추가:

```csharp
private class Slot
{
    public IHeroAura Aura;
    public float Remain;
    public bool Indefinite;
    public CHPoolable Visual;   //# IStatusVisual 인 경우만 — root 레벨 풀 인스턴스
}
```

**Attach:**
- 신규 슬롯이고 `aura is IStatusVisual sv` → `CHMResource.Load(sv.VisualKey, ...)` 콜백에서 `CHMPool.Pop` → `Slot.Visual` 저장
- visual 은 **root 레벨** (parent = null) — 영웅 자식으로 붙이면 영웅 Push 시 SetParent 막힘
- **재부착(같은 type) 시 early return** — 기존 코드 그대로. visual 중복 Pop 금지

**Update:**
- 각 슬롯의 `Visual` 이 있으면 `Visual.transform.position = hero.position + sv.Offset` 으로 매 프레임 추적
- 위치 추적은 HeroAuraRunner.Update 한 곳에서만 (각 Aura.Tick 에 분산 X)

**슬롯 해제 (Remain 만료):**
1. `Aura.OnDetached(_hero)` 호출
2. `Slot.Visual` 있으면 `CHMPool.Push(Slot.Visual)`
3. 슬롯 제거

**OnDisable (풀 반환):**
- 모든 슬롯에 대해 1) `Aura.OnDetached` 2) `Visual` Push — 순서 고정 (Aura 먼저: Aura 로직이 visual 참조할 수 있으므로)
- `_slots.Clear()`

### 2.3 데이터 흐름
```
[카드 효과 Apply] → ctx.ApplyHeroAura(aura, duration)
        ▼
[HeroAuraRunner.Attach]
        │ aura is IStatusVisual? → CHMResource.Load → CHMPool.Pop → Slot.Visual
        ▼
[HeroAuraRunner.Update] 매 프레임 Visual.position = hero.position + Offset
        ▼
[Remain 만료 / OnDisable] → Aura.OnDetached → CHMPool.Push(Visual)
```

---

## 3. visual 프리팹 6종

`EVisual` 확장 (PoisonAura 뒤 append — 시프트 없음):
```csharp
public enum EVisual
{
    PoisonAura,
    SlowStatus, FearStatus, WeakenStatus,
    AttackDownStatus, TimeStopStatus, BleedStatus,
}
```

| EVisual | 메쉬 | 색 (RGBA) | Scale | Offset | 대응 Aura |
|---|---|---|---|---|---|
| SlowStatus | Sphere | `#0EA5E9` α0.5 | 0.4 | (0, 0.05, 0) 발밑 | SlowAura |
| FearStatus | Cube | `#A855F7` α1.0 | 0.3 | (0, 1.3, 0) 머리 위 | FearAura |
| WeakenStatus | Cube | `#6B7280` α1.0 | 0.3 | (-0.5, 0.6, 0) 왼쪽 | WeakenAura |
| AttackDownStatus | Cube | `#7F1D1D` α1.0 | 0.25 | (0.5, 0.6, 0) 오른쪽 | HeroAttackDownAura |
| TimeStopStatus | Sphere | `#E5E7EB` α0.3 | 1.5 | (0, 0.5, 0) 영웅 감쌈 | TimeStopAura |
| BleedStatus | Sphere | `#DC2626` α1.0 | 0.25 | (0.4, 0.05, 0) 발밑 옆 | BleedAura |

- 반투명(α<1) 은 `SlowStatus`/`TimeStopStatus` — URP Lit Transparent Surface
- 불투명(α1.0) 은 URP Lit 기본 — `BuildPoisonAura` 와 동일
- 출혈은 본체 변색이 아닌 **부착물** (HitFlash 의 Renderer 색 조작과 충돌 회피)

### 3.1 LairVisualPrefabBuilder 제네릭화
현재 하드코딩된 `BuildPoisonAura` 를 일반 `BuildVisual(VisualSpec)` 로 리팩터:

```csharp
public class VisualSpec
{
    public EVisual Key;
    public PrimitiveType Mesh;
    public string ColorHex;
    public float Alpha;
    public float Scale;
}
```
- `Alpha < 1` 이면 URP Lit Transparent 셋업 (`_Surface=1`, `_Blend=0`, renderQueue=Transparent)
- Collider 제거 + Addressables 등록 (Resource 라벨) — 기존 패턴 동일
- PoisonAura 도 VisualSpec 으로 흡수 (디스크는 scale 비균일이라 special-case 유지 가능)

---

## 4. BattleController 풀 워밍
`PrewarmPools` 에 status visual 6종 추가 — 각 count 1~2 (동시 표시 가능성 낮음):
```csharp
foreach (var key in new[] { EVisual.SlowStatus, EVisual.FearStatus, EVisual.WeakenStatus,
                            EVisual.AttackDownStatus, EVisual.TimeStopStatus, EVisual.BleedStatus })
{
    var fx = await CHMResource.Instance.LoadAsync<GameObject>(key);
    if (fx != null) CHMPool.Instance.CreatePool(fx, count: 2);
}
```
워밍 시 CHMResource 캐시도 채워져 Attach 의 Load 콜백이 즉시 동작.

---

## 5. 마일스톤

| 마일스톤 | 산출물 | 검증 |
|---|---|---|
| M1 | EVisual 6값 + IStatusVisual + LairVisualPrefabBuilder 제네릭화 + visual 프리팹 6종 | 메뉴 실행 → 6 프리팹 |
| M2 | HeroAuraRunner 중앙 visual 관리 + 6개 Aura 에 IStatusVisual 구현 + PrewarmPools | EditMode 회귀 PASS |
| M3 | PlayMode/MCP 통합 검증 + 수동 검증 | 한 판 — 디버프별 visual 확인 |

---

## 6. 향후 — 아이콘 UI 방식 (Out of Scope, 기록용)
프리미티브 부착물은 검증용. 추후 **영웅 머리 위 아이콘**(WorldSpace Canvas 또는 화면 고정 UI)으로 교체 예정.
`IStatusVisual` 가 렌더링 방식을 추상화하므로, 교체 시 `HeroAuraRunner` 의 visual 관리부 + 프리팹만
바뀌고 **Aura 코드(VisualKey/Offset 노출)는 그대로 재사용**된다.

---

## 7. 위험 요소

| 위험 | 영향 | 완화 |
|---|---|---|
| Load 콜백 지연 — Attach 직후 짧은 duration 만료 시 Visual 콜백이 늦게 와 Push 누락 | visual 풀 누수 | PrewarmPools 로 캐시·풀 비축 → 콜백 즉시. 콜백 시 슬롯이 이미 사라졌으면 즉시 Push |
| visual 이 root 레벨 — 씬 전환 시 잔존 | 다음 씬 오염 | OnDisable 에서 전 슬롯 Visual Push 보장 |
| 동시 다중 디버프 시 부착물 겹침 | 시각 혼동 | Offset 을 6종 모두 다른 위치로 분산 (§3 표) |
| 반투명 머티리얼 URP 셋업 누락 | 분홍/검정 깨짐 | BuildVisual 에서 Alpha<1 분기 명시 |

---

## 8. 성공 기준 (사용자 검증)
- [ ] 둔화/공포/무력화/약화/시간정지/출혈 카드 적용 시 각각 visual 등장
- [ ] 디버프 만료 시 visual 사라짐
- [ ] visual 이 영웅을 따라다님
- [ ] 영웅 사망/풀 반환 후 visual 잔존 X
- [ ] EditMode 회귀 PASS
