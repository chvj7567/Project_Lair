# 영웅 상태 아이콘 (HP바 아래) 기획서

> Project Lair — 영웅에 걸린 액티브 상태 8종을 HP바 바로 아래 아이콘으로 on/off 표시.
> 입력 문서: spec `docs/superpowers/specs/2026-06-04-hero-status-icons-design.md` · plan `docs/superpowers/plans/2026-06-04-hero-status-icons.md`
> 작성일: 2026-06-04 · 작성: game-designer · 상태: Draft v1 — design-reviewer 검토 대기

---

## § 헤더

- **목표**: 영웅에 걸린 상태(둔화·공포·무력화·시간정지·출혈·죽음의표식·공격력감소·영구출혈) 8종을 HP바 바로 아래 가로 아이콘 행으로 보여준다. 아이콘은 그 상태를 대표하는 카드의 기존 `CardData.Icon` 을 재사용한다 (신규 아트 0장).
- **검증 가설**: "영웅에 어떤 액티브 상태가 걸려 있는지 HP바 아래 아이콘으로 한눈에 읽히면, 플레이어가 페이싱(언제 더 압박할지 / 어떤 빌드가 먹히는지)을 더 쉽게 판단하는가."
- **현재 단계 범위 적합성**: **범위 내**. MVP 컨텐츠(액티브 카드 12장 · Plague 둔화 프로크)는 이미 §11.3 에 확정돼 있고, 본 기획은 그 상태들의 **시각 표시 방식 교체**(월드 프리미티브 도형 → HP바 아이콘)다. 신규 콘텐츠/시스템 추가 없음. 아트 금지 룰은 카드 아이콘 재사용 한정으로 사용자 승격됨(2026-06-02, CLAUDE.md §8 / 컨셉 §11.4 "카드 / UI") — 신규 아트는 만들지 않으므로 승격 범위 안.
- **핵심 메커니즘**: Aura 타입 → 대표 `ECardId` → 그 카드의 `Icon` Sprite 로 해석(소스 무관). 유한 지속 상태는 떴다 사라지고, 무기한(-1) 상태(공격력감소·영구출혈)는 걸려있는 동안 지속. 표시는 on/off 만 — 잔여시간 시각화 없음.

---

## 1. 확정 결정 요약 (사용자 합의 — 변경 금지)

본 기획서가 새로 결정하지 않고 **그대로 반영**하는 항목 (입력 받은 락):

| # | 결정 | 근거 |
|---|---|---|
| 1 | 영웅 상태 **8종 전부** HP바 아래 아이콘 표시 | 사용자 합의 |
| 2 | 아이콘 = 대표 카드의 기존 `CardData.Icon` 재사용, 신규 아트 0장 | 사용자 합의 + 아트 금지 룰 준수 |
| 3 | 표시는 **on/off** 만 — radial fill / 카운트다운 없음 | 사용자 합의 (MVP 최소) |
| 4 | 바인딩은 **aura 타입 → 대표 ECardId → Sprite** (소스 무관) | Plague 프로크 등 카드 없는 상태도 누락 없이 표시 |

아래 §2~§5 가 game-designer 가 채운 도메인 결정이다.

---

## 2. 결정 A — aura → 대표 ECardId 표 (8행 최종 확정)

spec §2.1 의 권장 표를 카드 SO 실측으로 검증해 **확정**한다. 8개 카드 SO(`Assets/_Lair/Art/Cards/Items/`)를 직접 열어 `_icon` 필드가 비어있지 않음(GUID ≠ 0)을 전수 확인했다.

| Aura | 대표 ECardId | `_id` | 지속 | `_icon` GUID(앞 8자리) | 확정 사유 |
|---|---|---|---|---|---|
| SlowAura | `Slow` | 18 | 유한 10s | `5a8d7896` | 동명 카드 직결. 카드 둔화 + Plague 프로크 공용(같은 둔화 아이콘) |
| FearAura | `Fear` | 15 | 유한 3s | `305f6836` | 동명 카드 직결 |
| WeakenAura | `Weaken` | 17 | 유한 10s | `b1835a0d` | 동명 카드 직결 |
| TimeStopAura | `TimeStop` | 23 | 유한 5s | `8ed87d63` | 동명 카드 직결 |
| BleedAura | `Bleed` | 16 | 유한 10s | `04d4d139` | 동명 카드 직결 |
| MarkOfDeathAura | `MarkOfDeath` | 26 | 유한 5s | `ccb6a8d3` | 동명 카드 직결 |
| HeroAttackDownAura | `HeroAttackDown` | 14 | **무기한(-1)** | `fab793e2` | 자기 카드(값 14, 실제 Debuff 패시브) 아이콘 |
| EternalBleedAura | `Bleed` | 16 | **무기한(-1)** | `04d4d139` | **전용 카드 없음** → 동일 "출혈" 능력이므로 `Bleed` 아이콘 재사용 |

**전수 검증 결과 (game-designer 확정)**:
- **8개 모두 `_icon` 스프라이트 보유** — 누락 카드 0건. 아이콘 null fallback(슬롯 미표시)은 정상 동작 시 발생하지 않으며, 안전망(graceful)으로만 둔다.
- **EternalBleedAura → `ECardId.Bleed` 확정**. EternalBleed 전용 카드 ID 는 존재하지 않고(MVP 28장 마스터 표에 없음), Debuff Tier3 시너지로만 등록되는 "영구 출혈" 능력이다. 능력 의미가 `Bleed`(출혈)와 동일(영웅 이동 시 HP 감소)하므로 같은 출혈 아이콘 재사용이 맞다. 두 출혈이 동시 표출될 때의 시각 이슈는 §5 결정 D 에 기록.
- 카드 `_id` 값(`ECardId` enum 정수값)은 SO 실측: Slow=18, Fear=15, Weaken=17, TimeStop=23, Bleed=16, MarkOfDeath=26, HeroAttackDown=14. spec 이 명시한 "HeroAttackDown 값 14" 와 일치 확인.

> **단일 진실**: gameplay-programmer 의 8 Aura `IconCardId` 구현은 이 표를 따른다. plan Task 1 Step 2 의 코드 블록과 동일하다.

---

## 3. 결정 B — 아이콘 행 UX / 배치 수치

### 3.1 기준 치수 (HpBar.prefab 실측)

`HpBar.prefab` 의 루트 RectTransform `m_SizeDelta = (120, 20)` — **HP바는 폭 120px × 높이 20px**. Background full-stretch, txtHp inset(-20,-14). 영웅·몬스터 공용 프리팹이며 영웅 머리 위 월드 스페이스 표시.

### 3.2 핵심 제약 — 8 슬롯 worst-case 가 폭 120 안에 들어와야 한다

spec §6 이 **8 슬롯 = 상한, 초과 불가**를 락했다(아이콘 대상 8종과 슬롯 수 동일). 따라서 아이콘 행은 **8개가 전부 떠 있는 최악의 경우에도 HP바 폭(120px) 안에 들어와야** 시각적으로 깨지지 않는다. 흔한 경우가 아니라 worst-case 로 사이즈를 정한다.

**plan Task 4 가 제안한 IconSize 16 은 폐기한다.** 검산:

```
IconSize 16 · spacing 0 (간격 제거 극단):  8 × 16              = 128px  > 120  ✗
IconSize 16 · spacing 2 (plan 제안):       8 × 16 + 7 × 2 = 142px  > 120  ✗
```

간격을 0으로 줄여도 16px × 8 = 128 > 120 이라 **IconSize 16 은 물리적으로 불가능**. 간격 조정 문제가 아니라 아이콘 크기 자체를 낮춰야 한다.

### 3.3 확정 수치

| 항목 | 확정값 | 근거 |
|---|---|---|
| 슬롯 수 (`IconSlotCount`) | **8** | 아이콘 대상 8종 = 상한. spec §6 초과 불가 락 |
| 아이콘 크기 (`IconSize`) | **12 px** | worst-case 8개가 120 안에 들어오는 최대 균형값(아래 검산) |
| 슬롯 간격 (`spacing`) | **2 px** | plan 값 유지 — 아이콘 분리 가독 최소치 |
| 행 위치 (`anchoredPosition.y`) | **-2 px** (HP바 바로 아래) | plan 값 유지. 행 pivot.y = 1(상단 기준) → HP바 하단에서 2px 띄워 아래로 |
| 행 폭 (RectTransform) | HP바 폭에 stretch (anchorMin.x=0 / anchorMax.x=1, sizeDelta.x=0) | 행이 120px 로 늘어나고 그 안에서 콘텐츠 중앙 정렬 |
| 행 높이 (sizeDelta.y) | **12 px** (= IconSize) | 한 줄 아이콘 높이 |
| 정렬 (`HorizontalLayoutGroup.childAlignment`) | **MiddleCenter** | plan 값 유지 — 1~8개 어떤 개수든 행 중앙에 모여 표시 |
| childControl / childForceExpand | 모두 **false** | 슬롯 크기를 12×12 로 고정(레이아웃이 늘리지 않음) — plan 값 유지 |
| `Image.preserveAspect` | **true** | 카드 아이콘 원본 비율 보존 — plan 값 유지 |

**worst-case 검산 (8개 전부 표시)**:

```
콘텐츠 폭 = 8 × 12 + 7 × 2 = 96 + 14 = 110px  ≤  120  ✓
좌우 여백 = (120 - 110) / 2 = 5px  (HLG MiddleCenter 가 110px 콘텐츠를 120px 행 중앙에 배치)
```

110 ≤ 120, 좌우 5px 여백 — 안전. (참고: IconSize 13 이면 8×13+7×2=118px, 좌우 1px 로 너무 빡빡 → 12 가 깔끔한 상한.)

### 3.4 HP바와의 시각 균형 근거

- HP바 높이는 20px. 아이콘 12px 는 바 높이의 60% → 바보다 **작아 종속적**으로 읽히되(상태는 HP의 보조 정보), 8px 미만으로 작아 식별 불가해지지도 않는다. plan 의 16px 는 바 높이(20)에 육박해 "바와 같은 위계"로 보이고 폭도 초과해 부적합했다.
- 행이 HP바 폭(120)에 stretch 하고 콘텐츠를 중앙 정렬하므로, 아이콘 1개만 떠도 바 정중앙 아래에 정렬돼 HP바와 세로 중심선이 맞는다 → 시선 이동 비용 최소.

---

## 4. 결정 C — 동시 다중 상태 정렬 규칙

### 4.1 부착 / 정렬 규칙 (확정)

1. **빈 슬롯 중 인덱스가 가장 낮은 슬롯(lowest free slot)에 채운다.** (`AddStatusIcon` 이 `_iconSlots[0..7]` 을 순회하며 첫 비활성 슬롯 사용)
2. **시각적 좌→우 순서 = 슬롯 인덱스 순서**(Icon0 가 가장 왼쪽). `HorizontalLayoutGroup` 이 자식 인덱스(Icon0..Icon7) 순으로 배치하므로, 부착 시각 순서가 아니라 **슬롯 인덱스가 표시 순서를 결정**한다.
3. **슬롯 인덱스 재할당은 없다(배열은 안정)** — 상태 해제 시 해당 슬롯만 비활성화되고, 남은 아이콘은 자기 슬롯 인덱스를 그대로 유지한다(`_keyToSlot` 매핑 불변). **단 시각 위치는 reflow 된다** — `HorizontalLayoutGroup` 이 비활성 자식을 건너뛰고 활성 슬롯만 좌측부터 모아 그리므로, 중간 슬롯이 비면 오른쪽 아이콘들이 그 자리로 당겨져 보이고, 다음에 새로 걸리는 상태가 그 빈 인덱스를 재사용해 **행 중간에 나타날 수 있다**. 즉 "데이터상 슬롯은 고정, 화면상 위치는 활성 아이콘 수에 따라 이동"이 의도된 동작이며 on/off 가독에는 문제없다(MVP 허용).

> 요약 규칙(단일 진실): **"가장 낮은 빈 슬롯에 채운다 · 표시 순서 = 슬롯 인덱스 · 해제 시 명시적 재정렬은 안 하되 HLG 가 활성 아이콘만 좌측부터 모아 그림."** gameplay-programmer 는 이 규칙대로 구현하며 별도 정렬 우선순위(상태 종류별 고정 순서) 는 두지 않는다.

### 4.2 슬롯 초과(>8) 불가

대상 상태가 정확히 8종이고 슬롯도 8개이므로 **동시 9개 이상은 구조적으로 발생하지 않는다.** 같은 타입 재부착(연장)은 키(aura 타입) 중복으로 새 슬롯을 쓰지 않으므로(§5.1) 한 타입은 항상 1슬롯만 점유. 따라서 슬롯이 모자라 잘리는 상황은 정상 플레이에서 도달 불가. 만에 하나 8개가 다 차 있을 때 `AddStatusIcon` 이 빈 슬롯을 못 찾으면 **조용히 무시**(graceful no-op)한다 — 8종 전부 동시 표출은 이 무시 분기에 도달하기 직전의 정상 상한이다.

---

## 5. 결정 D — 무기한 상태 UX (시각 과밀 — 기록만, 스코프 추가 금지)

무기한(-1) 상태 2종(공격력감소 `HeroAttackDown` · 영구출혈 `EternalBleed`)은 한 번 걸리면 라운드 끝까지 떠 있다. 시각 과밀 가능성을 점검하되, **MVP 스코프 추가는 하지 않고 기록만** 한다.

### 5.1 점검 결과

| 우려 | 평가 | 조치 |
|---|---|---|
| 무기한 2종이 항상 슬롯 점유 → 유한 상태 표시 공간 잠식 | 슬롯 8 ≥ 대상 8 이므로 **공간 부족 없음**. 무기한 2 + 유한 6 = 8 = 슬롯 수. 잠식 불가 | 조치 불필요 |
| 무기한 아이콘이 계속 떠 있어 "변화"가 안 보임(노이즈화) | 무기한 상태는 본래 지속 정보이므로 떠 있는 게 정상. on/off 만이라 카운트다운 노이즈도 없음 | MVP 허용 — 잔여시간/펄스 등 강조는 v0.2 후보 |
| **두 출혈(BleedAura 유한 + EternalBleedAura 무기한)이 동시 표출 시 동일 `Bleed` 아이콘이 2개** | 도달 가능: Debuff Tier3 가 영구출혈(무기한)을 등록한 상태에서 출혈 액티브 카드(재픽 가능)가 유한으로 겹치면, **똑같은 출혈 아이콘 2개**가 나란히 떠 버그처럼 보일 수 있음 | **기록만.** §1 결정 4(타입 기반·소스 무관)와 결정 2(신규 아트 0장)의 직접 귀결. 해소하려면 전용 EternalBleed 아이콘 신규 제작이 필요 → 결정 2 위반이므로 MVP 에선 두지 않음. v0.2 에서 무기한 출혈 전용 아이콘/배지 검토 |

### 5.2 결론

무기한 상태로 인한 **공간 과밀은 없다**(슬롯 수가 상한을 덮음). 유일하게 남는 인지 이슈는 "출혈 아이콘 2개 동시 표출"이며, 이는 신규 아트 0장 제약의 의도된 트레이드오프로 **MVP 에서 수용**한다. 본 기획은 이를 해소하는 추가 작업(전용 아이콘·배지·뱃지 카운터)을 **포함하지 않는다** — v0.2 기록 항목으로만 남긴다.

---

## 6. 페이싱 / 시너지 가시성 영향

- **페이싱**: 액티브 카드는 30초마다 픽되고(컨셉 §4.2), 유한 상태 지속은 3~10초(Fear 3 / TimeStop 5 / MarkOfDeath 5 / Slow·Weaken·Bleed 10)다. 아이콘이 이 짧은 창 동안만 떴다 사라지므로, 플레이어는 "지금 영웅이 묶여 있다 / 약화됐다"를 실시간으로 읽어 다음 픽 타이밍을 판단할 수 있다. on/off 라 트리거 빈도를 바꾸지 않으므로 컨셉이 정한 결정 빈도(평균 ~17초/회)를 침해하지 않는다.
- **시너지 가시성**: 무기한 2종(공격력감소·영구출혈)은 Debuff 축 Tier2·Tier3 시너지의 결과물(컨셉 §5.2)이다. 아이콘이 라운드 끝까지 떠 있으면 플레이어가 "내 Debuff 빌드가 발화했다"를 HP바 아래에서 지속 확인 → 빌드 방향이 화면에 노출된다(컨셉 §5.2 시너지 가시성 원칙과 정합). 단 본 기획은 표시만 담당하고 빌드 카운트 바(카드 픽 팝업 상단)는 별도 시스템(`card-renewal.md` §8) 영역이다.

---

## 7. 구현 요청사항 (gameplay-programmer 용)

> 시그니처·파일 구조의 단일 진실은 plan `2026-06-04-hero-status-icons.md`. 본 절은 **도메인 결정값**(수치·표·매핑)만 명세하며, plan 과 어긋나는 수치는 **본 기획서가 우선**(project.md 문서 분담: 수치 SoT = 기획서).

### 7.1 Enum

- **신규 Enum 값 추가 없음.** 사용하는 `ECardId` 값 8종(Slow/Fear/Weaken/TimeStop/Bleed/MarkOfDeath/HeroAttackDown)은 이미 `CommonEnum.cs` 에 존재.
- **제거**: `EVisual` 의 status 6값(`SlowStatus`/`FearStatus`/`WeakenStatus`/`AttackDownStatus`/`TimeStopStatus`/`BleedStatus`) 삭제. **`EVisual.PoisonAura` 는 유지**(독 장판 자체 visual). (plan Task 6 / spec §4)

### 7.2 Interface

- `IStatusVisual` 멤버 교체: 기존 `EVisual VisualKey` / `Vector3 Offset` 제거 → `ECardId IconCardId { get; }` 추가 (plan Task 1). 8개 Aura 가 §2 표대로 구현.

### 7.3 에셋 키 / 스프라이트 소스

- **신규 에셋 0개.** 아이콘 Sprite 는 8개 카드 SO 의 기존 `_icon` 필드를 재사용(§2 GUID 표).
- `ECardId → Sprite` dict 는 카드 풀(`CardPool_Active.asset` / `CardPool_Passive.asset`)에서 1회 구성해 BattleHud 에 주입 (plan Task 5 Step 2~3).
- **주의 (silent-bug 방지)**: 8종 중 `HeroAttackDown` 은 **유일한 패시브 카드**(`CardPool_Passive`, 컨셉 §11.3 Debuff 4번 — `_axis: 2`). 나머지 7종은 액티브 카드(`CardPool_Active`)다. dict 구성 시 **반드시 두 풀을 모두 스캔**해야 한다. 액티브 풀만 스캔하면 `ECardId.HeroAttackDown` 매핑이 누락되고, §3.3 graceful no-op(아이콘 null → 슬롯 미표시) 때문에 **공격력감소 아이콘만 조용히 안 뜨는, 에러 없는 보이지 않는 버그**가 된다(Debuff Tier2 발화 때만 표면화).

### 7.4 SO 스키마 / 수치 필드

- **CardData SO 스키마 변경 없음** — 기존 `_icon`(Sprite) 필드를 읽기만 한다.
- **HpBar.prefab 아이콘 행 빌더 수치** (`EnsureHpBarPrefab`, plan Task 4 — 아래 값으로 plan 의 16 을 대체):

  | 빌더 상수 | 값 |
  |---|---|
  | `IconSlotCount` | 8 |
  | `IconSize` | **12f** (plan 의 16f 폐기) |
  | `HorizontalLayoutGroup.spacing` | 2f |
  | `HorizontalLayoutGroup.childAlignment` | `TextAnchor.MiddleCenter` |
  | childControlWidth/Height · childForceExpandWidth/Height | 전부 false |
  | 행 `anchoredPosition` | (0, -2) |
  | 행 `anchorMin`/`anchorMax`/`pivot` | (0,0)/(1,0)/(0.5,1) |
  | 행 `sizeDelta` | (0, 12) — 폭은 stretch, 높이 = IconSize |
  | 각 슬롯 `sizeDelta` | (12, 12) |
  | 슬롯 `Image.preserveAspect` | true |
  | 행·슬롯 기본 활성 | 전부 **비활성**(SetActive(false)) — 몬스터 바 공유 클린 유지 |

- HpBarView 의 아이콘 행 API(`AddStatusIcon`/`RemoveStatusIcon`/`ClearStatusIcons`) 동작 규칙은 §4.1: lowest-free-slot 채움 · 같은 key 중복 무시 · 마지막 제거 시 행 비활성.

---

## 8. 미해결 / 주의점

| 항목 | 처리 |
|---|---|
| plan Task 4 의 `IconSize 16` 과 본 기획서 `12` 불일치 | **본 기획서(12)가 우선**(수치 SoT). gameplay-programmer 는 plan 의 16 을 12 로 치환. plan ↔ 기획서 sync 규칙(project.md)에 따라 plan 도 delta 로 12 반영 권장 |
| 출혈 아이콘 2개 동시 표출(Bleed 유한 + EternalBleed 무기한) | §5.2 — MVP 수용(신규 아트 0장 제약의 트레이드오프), v0.2 기록 항목 |
| M0 reconcile (HpBar 수작업 델타) 선행 필수 | spec §3 / plan Task 0 영역. 본 기획서는 수치 결정만 — reconcile 자체는 구현 작업 |
| 카드 아이콘 dict 구성 위치(BattleHud 주입) | plan Task 5 영역(아키텍처). 본 기획서 결정 아님 |

---

## 9. Self-Review

- **Placeholder 잔존**: 0건. TBD/추후결정/적절히/또는(디자인 결정) 없음. IconSize 16→12 는 검산 동반 단정.
- **스펙 커버리지**: spec §2.1 표 → §2 / spec §6 슬롯 상한·초과불가 → §3.2·§4.2 / spec §0.2~0.3 범위 → § 헤더·§1 / plan Task 4 수치 → §3.3·§7.4. 입력 4개 락 전부 §1·§2 매핑. 갭 0.
- **내부 일관성**: IconSize 12 · spacing 2 · 8슬롯 · 110px 검산이 §3.3 / §7.4 동일. EternalBleed→Bleed 가 §2 / §5 동일.
- **시그니처/명명 일관성**: `IconCardId` · `AddStatusIcon` · `RemoveStatusIcon` · `ClearStatusIcons` · `IconSize` · `IconSlotCount` · `_statusIconRow` · `_iconSlots` — plan 과 글자 그대로 일치(수치값만 16→12 override).
- **모호 표현**: 0건. 출혈 2개 이슈는 "사용자 선택 필요"가 아니라 MVP 수용으로 명시 결정.
- **스코프**: 표시 방식 교체 단일 단위. 분할 불요.
- **구현 요청사항 완전성**: Enum(추가 0·제거 6) / Interface(`IStatusVisual` 교체) / 에셋 키(신규 0·기존 8 재사용) / SO 스키마(변경 0, 빌더 수치 표) 명세 완료.

**Self-Review: 통과** (IconSize 16→12 1항목 보강 후 통과 — plan 제안값이 폭 120 초과라 검산 후 12 로 확정).
