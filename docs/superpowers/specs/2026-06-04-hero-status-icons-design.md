# 영웅 상태 아이콘 (HP바 아래) 설계서

> Project Lair — 영웅에 걸린 액티브 상태를 HP바 아래 아이콘으로 표시.
> 기존 월드 프리미티브 도형(status-visuals, 2026-05-20) 을 아이콘 UI 로 교체.
> 작성일: 2026-06-04
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
영웅에 걸린 상태(시간정지·둔화 등)를 **HP바 바로 아래 아이콘**으로 보여준다. 상태가
활성화된 동안에만 아이콘이 뜨고, 해제되면 사라진다. 기존 status-visuals 설계서 §6
("향후 — 아이콘 UI 방식") 에서 예고된 교체를 실행한다.

### 0.2 In Scope
- 영웅 상태 8종을 HP바 아래 아이콘으로 표시 (둔화/공포/무력화/시간정지/출혈/죽음의표식/공격력감소/영구출혈).
- 아이콘 = **그 상태에 대응하는 능력(카드)의 기존 `CardData.Icon` Sprite 재사용** — 신규 아트 0장.
- 상태 시작/종료에 따라 아이콘 on/off. 지속시간 있는 상태는 떴다 사라지고, 무기한 상태는 걸려있는 동안 지속.
- 월드 프리미티브 status 도형(6종) 및 관련 코드/에셋 제거.
- `HpBar.prefab` 빌더(`EnsureHpBarPrefab`)에 아이콘 행 추가 + 현재 수작업 상태 reconcile (M0).

### 0.3 Out of Scope
- **잔여시간 시각화** (radial fill / 카운트다운) — on/off 만.
- 신규 상태 아이콘 아트 제작 — 기존 카드 아이콘만 재사용.
- 독 장판(`PoisonAura`) — 자체 visual(장판) 보유, 상태 아이콘 아님, 변경 없음.
- 몬스터 글로벌 버프(광폭화/강철의지/폭주) 표시.
- 사운드.

### 0.4 검증 가설
"영웅 상태가 HP바 아래 아이콘으로 보이면, 어떤 액티브가 걸려 있는지 한눈에 읽혀 페이싱 판단이 쉬워지는가."

---

## 1. 프로젝트 룰 매핑

| 룰 | 적용 |
|---|---|
| 01 커밋 | 기획 관점 한글 커밋 메시지(안) |
| 02 §1 주석 `//#` | 모든 신규 주석 |
| 02 §5 종속성 | Aura 는 icon 을 직접 관리 안 함 — 마커만 노출, HeroAuraRunner→ViewModel→View 흐름 |
| 02 §6 MVVM | 상태 변화는 BattleViewModel 통해 View(BattleHud/HpBarView) 로 단방향 통지. View 로직 금지 |
| 02 §6.1 캡슐화 | HpBarView 가 아이콘 행 위젯을 private 소유, 의도 API(`AddStatusIcon` 등)만 노출 |
| 03 §3 UI 래퍼 | 아이콘은 `Image`(Sprite) — Legacy Text 등 미사용. 정적 슬롯이라 풀링 불필요 |
| 03 §4 풀 스폰 | 아이콘 슬롯은 prefab 정적 배치 + on/off (런타임 Instantiate 금지) |
| 04 §1 프리팹화 | 아이콘 행은 `HpBar.prefab` 안에 정적 배치, 빌더 코드로 생성 |
| 04 에셋 | 아이콘 행은 `EnsureHpBarPrefab()` 빌더에 작성해야 영속 ([[reference_m4_clobbers_prefab_handedits]]) |

---

## 2. 아키텍처

### 2.1 아이콘 바인딩 = Aura 타입 기반 (소스 무관)
**`_currentCardScope` passthrough 를 쓰지 않는다.** `PlagueSlowOnHit` 가 카드 없이
`runner.Attach(new SlowAura(...))` 를 직접 호출하므로(코어 몬스터 프로크), 카드 스코프
방식은 그 둔화의 아이콘을 누락한다(회귀). 또한 `HeroAuraRunner.Attach` 는 같은 타입
재부착 시 early-return 하므로 카드 스코프가 이벤트에 도달하지 못하는 경우가 생긴다.

대신 **aura 타입 → 대표 ECardId → 카드 Sprite** 로 해석한다:

```csharp
//# Card/CommonInterface.cs — IStatusVisual 을 아이콘 마커로 교체
//# (기존 EVisual VisualKey / Vector3 Offset 멤버 제거)
public interface IStatusVisual
{
    ECardId IconCardId { get; }   //# 이 상태를 대표하는 카드(능력) — 그 카드의 Icon 을 표시
}
```

- 8개 Aura 가 각자 자기 상태를 대표하는 `ECardId` 를 노출.
- `ECardId → Sprite` 해석은 카드 풀에서 만든 `Dictionary<ECardId, Sprite>` 로 **View 계층(BattleHud)** 에서 수행 — ViewModel 은 Sprite(Unity 의존)를 들지 않고 `ECardId`(enum)만 전달.

#### aura → 대표 ECardId 표 (game-designer 확정)
| Aura | 대표 ECardId | 지속 | 비고 |
|---|---|---|---|
| SlowAura | `Slow` | 유한 | 카드 + Plague 프로크 공용 (같은 둔화 아이콘) |
| FearAura | `Fear` | 유한 | |
| WeakenAura | `Weaken` | 유한 | |
| TimeStopAura | `TimeStop` | 유한 | |
| BleedAura | `Bleed` | 유한 | |
| MarkOfDeathAura | `MarkOfDeath` | 유한 | |
| HeroAttackDownAura | `HeroAttackDown` | **무기한(-1)** | 자기 카드 아이콘(값 14) |
| EternalBleedAura | `Bleed` | **무기한(-1)** | 전용 카드 없음 — 동일 "출혈" 능력 아이콘 재사용 |

> 무기한 상태(공격력감소·영구출혈)는 걸려있는 동안 아이콘 지속, 해제(OnDetached/풀반환) 시 제거. on/off 모델이라 카운트다운 없이 자연 처리.

### 2.2 라이프사이클 권한 = HeroAuraRunner
월드 도형 Pop/Push 로직을 **제거**하고, 상태 시작/종료 이벤트 발행으로 대체:

```csharp
//# HeroAuraRunner
public event Action<object, ECardId> OnStatusShown;   //# key, 대표 ECardId
public event Action<object> OnStatusHidden;           //# key
```
- 신규 슬롯이고 `aura is IStatusVisual sv` → `OnStatusShown(aura.GetType(), sv.IconCardId)` 발행.
- 같은 타입 재부착(연장) → 이벤트 재발행 안 함 (이미 표시 중).
- Remain 만료 / OnDisable(풀 반환) → `OnStatusHidden(aura.GetType())`.
- key = aura 타입 — 중복 표시 방지 + 종료 매칭.
- `Slot.Visual`(CHPoolable) 필드 및 CHMResource/CHMPool visual 경로 삭제.

### 2.3 배선 = MVVM (기존 HP 흐름과 동일 패턴)
```
HeroAuraRunner (월드, 영웅 GameObject)
   └ OnStatusShown / OnStatusHidden (key, ECardId)
        ▼  BattleController 가 영웅 셋업 시 구독 → BattleViewModel 로 재발행
BattleViewModel
   └ OnStatusIconAdded(object key, ECardId) / OnStatusIconRemoved(object key)
        ▼  BattleHud 구독
BattleHud
   └ ECardId → Sprite 해석 (카드 아이콘 dict) → HpBarView.AddStatusIcon / RemoveStatusIcon
```
- BattleController 는 영웅 HeroAuraRunner 를 셋업 시점에 확보(get/add)해 이벤트 구독, ViewModel 로 forward. 영웅 풀 반환/재사용 시 구독 해제·재구독 정리.
- 카드 아이콘 dict(`ECardId→Sprite`)는 카드 풀에서 1회 구성해 BattleHud 에 주입(`BattleHudArg` 확장 또는 기존 카드 컬렉션 재사용).

### 2.4 HpBarView — 아이콘 행 (HpBar.prefab 내부)
```csharp
//# HpBarView 의도 API 추가 (내부 위젯 private 소유 — Rule 02 §6.1)
public void AddStatusIcon(object key, Sprite icon);
public void RemoveStatusIcon(object key);
public void ClearStatusIcons();
```
- HP Fill 아래에 `HorizontalLayoutGroup` 컨테이너 + 정적 N개(=8) `Image` 슬롯을 private 소유.
- `AddStatusIcon` — 비어있는 슬롯에 sprite 세팅 + 활성화, key↔슬롯 매핑 기록.
- `RemoveStatusIcon` — 해당 key 슬롯 비활성화 + 매핑 해제.
- 컨테이너는 **기본 비활성** — `HpBar.prefab` 을 공유하는 몬스터 바(`MonsterHpBar`)는 아이콘이 안 붙어 깨끗하게 유지. 첫 아이콘 추가 시 컨테이너 활성화, 마지막 제거 시 비활성화.
- 런타임 `Instantiate` 없음 — 슬롯은 전부 prefab 정적 배치.

---

## 3. HpBar.prefab 빌더 작업 (M0 reconcile 선행)

`EnsureHpBarPrefab()` 은 **매 빌드마다 전체 재생성**(`new GameObject` → `SaveAsPrefabAsset` 덮어쓰기)이며 M3 빌드에서 무조건 호출된다. 현재 `HpBar.prefab` 에는 사용자 수작업 델타가 있어, 아이콘 행을 빌더에 넣기 **전에** 현재 상태를 빌더에 reconcile 해야 한다. 그렇지 않으면 다음 빌드에 수작업이 소실된다.

### 3.1 현재 프리팹 vs 빌더 출력 — 반영해야 할 수작업 델타
| 항목 | 현재 프리팹(진실) | 현재 빌더 출력 | 조치 |
|---|---|---|---|
| Background 색 | 어두운 회색 `(0.264, 0.264, 0.264, 1)` | 흰색 기본 | 빌더가 bg 색 지정 |
| txtHp 폰트 | 오토사이징 (min 6 / max 10) | 고정 12 | 빌더가 autoSize 셋업 |
| txtHp Rect | inset (sizeDelta `-20,-14`, anchoredPos y:1) | full stretch | 빌더가 inset 적용 |
| Background/Fill 스프라이트·폰트 GUID | 특정 에셋 지정 | 빌더 경로 로드 | 일치 여부 확인 후 맞춤 |

> gameplay-programmer 는 **현재 `HpBar.prefab` 을 단일 진실**로 삼아 위 델타를 `EnsureHpBarPrefab()` 에 반영한다. (수치는 구현 시 현재 prefab YAML 재확인.)

### 3.2 아이콘 행 추가 (reconcile 후)
- `EnsureHpBarPrefab()` 에 HP Fill 아래 `StatusIconRow` 컨테이너 + 8 슬롯 Image 를 코드로 생성.
- `HorizontalLayoutGroup` 배치, 컨테이너 기본 비활성.
- `HpBarView` 의 새 private 필드(`_statusIconRow`, `_iconSlots[]`) 와이어링(`SetPrivateField`).

---

## 4. cleanup 범위 (명시)

- `IStatusVisual` 멤버 교체: `EVisual VisualKey` / `Vector3 Offset` 제거 → `ECardId IconCardId` 추가. **8개 Aura** 구현 수정.
- `HeroAuraRunner`: 월드 visual Pop/Push/추적 코드 + `Slot.Visual` 제거, 이벤트 발행으로 대체.
- 월드 status 프리팹 **6종 삭제** + `EVisual` 의 status 6값(`SlowStatus`/`FearStatus`/`WeakenStatus`/`AttackDownStatus`/`TimeStopStatus`/`BleedStatus`) 제거. **`EVisual.PoisonAura` 는 유지**(PoisonAura 자체 장판).
- `LairVisualPrefabBuilder` 의 status visual 생성 코드 제거(PoisonAura 생성은 유지).
- `BattleController.PrewarmPools` 의 status visual 워밍 6종 제거.
- 기존 테스트 갱신: `HpBarViewTests`, status-visuals EditMode 회귀.

---

## 5. 마일스톤

| 마일스톤 | 산출물 | 검증 |
|---|---|---|
| **M0** | 현재 `HpBar.prefab` 수작업 델타를 `EnsureHpBarPrefab()` 에 reconcile | 빌더 실행 → 출력이 현재 프리팹과 동일(시각·수치) |
| M1 | `IStatusVisual` 마커 교체 + 8 Aura `IconCardId` 구현 + `HeroAuraRunner` 이벤트화(월드 visual 제거) | EditMode 회귀 PASS |
| M2 | `HpBarView` 아이콘 행 API + `EnsureHpBarPrefab()` 아이콘 슬롯 8개 추가 | 빌더 실행 → 행 생성, 몬스터 바 영향 없음 |
| M3 | BattleController 구독 + BattleViewModel 이벤트 + BattleHud ECardId→Sprite 배선 | 카드 적용 시 아이콘 표시 |
| M4 | cleanup(프리팹/Enum/PrewarmPools/Builder) + 테스트 갱신 | 빌드·테스트 그린, 월드 도형 미출현 |
| M5 | 수동/MCP 검증 — 한 판 | 6 유한 상태 떴다 사라짐 + 2 무기한 지속 |

---

## 6. 위험 요소

| 위험 | 영향 | 완화 |
|---|---|---|
| 빌더 미반영 수작업 → 다음 빌드 소실 | 사용자 수정 유실 | **M0 reconcile 선행**, 아이콘 행도 `EnsureHpBarPrefab()` 에 작성 |
| `HpBar.prefab` 공유(몬스터 바)에 아이콘 누출 | 몬스터 바 오염 | 컨테이너 기본 비활성, 영웅 경로만 채움 |
| 영웅 풀 재사용 시 이벤트 구독 누수 | 중복/잔존 아이콘 | 영웅 셋업/해제에서 구독 정리, OnDisable 시 ClearStatusIcons |
| 무기한 상태 아이콘이 안 사라짐 | 잔존 | OnDetached/OnDisable 에서 OnStatusHidden 보장 |
| ECardId→Sprite dict 에 매핑 누락 | 아이콘 null | null 이면 슬롯 미표시(graceful) + 경고 로그 |
| 동시 다수 상태 > 슬롯 수 | 아이콘 잘림 | 슬롯 8 = 아이콘 대상 8종 상한과 동일, 초과 불가 |

---

## 7. 성공 기준 (사용자 검증)
- [ ] 둔화/공포/무력화/시간정지/출혈/죽음의표식 카드 적용 시 HP바 아래 해당 카드 아이콘 등장, 지속시간 후 사라짐.
- [ ] 시간정지 5초면 아이콘이 5초간 떠 있다가 사라짐.
- [ ] 공격력감소/영구출혈은 걸려있는 동안 아이콘 지속.
- [ ] Plague 몬스터의 둔화 프로크도 둔화 아이콘 표시.
- [ ] 동시 다중 상태 시 아이콘이 가로로 나열.
- [ ] 월드 프리미티브 status 도형은 더 이상 출현하지 않음.
- [ ] 몬스터 HP 바에는 아이콘 행이 비어있음.
- [ ] 영웅 사망/풀 반환 후 아이콘 잔존 X.
- [ ] EditMode 회귀 PASS, `HpBar.prefab` 빌더 출력 = 현재 수작업 상태 + 아이콘 행.
