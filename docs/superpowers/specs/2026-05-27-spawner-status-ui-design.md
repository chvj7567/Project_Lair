# 스포너 상태 UI — 설계 스펙

> 작성: brainstorming · 2026-05-27
> 대상 버전: MVP
> 후속: `docs/superpowers/plans/` 의 같은 토픽 implementation plan

---

## 1. 개요

현재 스포너 상태는 월드 위 컴포넌트로 표시된다.

- `SpawnerBody` — 디스크 색상 틴트로 출력 종 표시 (`Spawner.cs` 기반)
- `SpawnerCooldownBar` — 디스크 위 World-space 진행 바, Cool→Warm 2단계 색상

플레이어가 카드 픽 결정을 빠르게 내리려면 더 많은 스포너 상태(동시 출력 수, 적용된 종 강화)를 한눈에 보여줘야 한다. 그러나 6개 스포너가 ring 외곽에 흩어져 있어 카메라 시점에 따라 일부가 가려지거나 시선이 분산된다.

본 스펙은 다음을 결정한다.

1. World-space 진행 바를 제거하고 **Screen-space `BattleHud`** 하단에 6셀 가로 패널로 통합한다.
2. 각 셀에 **종 색칩 + 종명 + ×N 동시 출력 + 진행 바** + **상단 강화 아이콘 row** 를 노출한다.
3. 셀 클릭 시 셀 위 floating 툴팁으로 **해당 스포너에 적용된 강화 카드 효과 상세** 를 표시한다.
4. `BuildPanel` 은 종 강화 카드(6장)를 제외한 카드만 표시하고, 패널 클릭 시 화면 중앙 모달로 **픽한 모든 카드의 능력 설명** 을 띄운다.
5. 월드 디스크 본체(`SpawnerBody`) 는 그대로 유지한다 — Replace 카드 효과 시각 피드백.

---

## 2. 결정 사항 요약

### 2.1 확정 (사용자 합의)

| # | 항목 | 결정 |
|---|---|---|
| 1 | HUD 배치 | 화면 하단 가로 6셀 |
| 2 | 각 셀 내용 | 종 색칩 + 종명 + ×N + 진행 바 (Cool→Warm) |
| 3 | 셀 상단 | 출력 종에 적용된 강화 카드 아이콘 row (출력 종 변경 시 즉시 갱신) |
| 4 | 셀 클릭 | 셀 위 floating 툴팁 — 해당 스포너 강화 효과 상세 |
| 5 | World-space 진행 바 | 제거, BattleHud 로 통합 |
| 6 | BuildPanel | 종 강화 6장 제외, 기존 패시브/액티브 분리 유지 |
| 7 | BuildPanel 클릭 | 화면 중앙 모달 — 픽한 모든 카드 능력 설명 |

### 2.2 디자인 기본값 (사용자 동의)

| α | 월드 디스크 본체(Cylinder) | 유지 (위치 + 종 색상 틴트) |
| β | 셀 정렬 순서 | ring 0°→300° (컨셉서 §3.1 표 순서, 내부 인덱스와 일치) |
| γ | 중첩 픽 아이콘 표시 | 아이콘 옆 작은 ×N 배지 |
| δ | BuildPanel 셀별 `_detailRoot` | 제거, 모달로 통합 (셀/빈 영역 클릭 모두 모달) |
| ε | 셀 너비 | 약 46px (6셀 + 좌우 여백 기준, 종명 3~4글자 + ×N + 진행 바 수용) |

---

## 3. 데이터 소유처와 흐름

```
Spawner (씬 정적 오브젝트, 6개)
  ├ CurrentType         (EMonster, ISpawnerOutputProvider)
  ├ OutputCount         (int, 신규 노출)
  └ Progress            (float 0~1, ISpawnerProgress)
        │
        ▼ 이벤트 발행
BattleController
  ├ _typeModifiers      (Dictionary<EMonster, StatMultiplier>)
  └ _typeModifierPicks  (Dictionary<EMonster, List<CardData>> — 신규)
        │
        ▼ ApplyPick / RegisterMonsterTypeBuff 시 갱신
BattleViewModel
  ├ Spawners            (IReadOnlyList<SpawnerSnapshot>)
  └ OnSpawnerSnapshotChanged(int index)
        │
        ▼ 구독
BattleHud
  └ SpawnerStatusPanel  (6 SpawnerStatusCell)
        ├ 셀 클릭 → SpawnerStatusTooltip toggle
        └ BuildPanel 클릭 → CHMUI.ShowUI(EUI.BuildModalPopup, …)
```

핵심 원칙:

- `Spawner` / `BattleController` 는 **데이터 소유**, View 를 모름 (Rule 05 MVVM)
- `BattleViewModel` 이 **통합 노출** — UI 는 VM만 본다
- 강화 카드는 종 단위 글로벌이므로, 같은 종 출력 스포너 셀들은 **같은 아이콘 row** 를 동시에 갖는다

---

## 4. `BattleViewModel` 확장

### 4.1 신규 데이터 구조

```text
SpawnerSnapshot
  int           Index            ring 인덱스 (0°→300° 순)
  EMonster      CurrentType
  int           OutputCount
  float         Progress         (0~1, 초기 지연 = 0)
  IReadOnlyList<AppliedBuff> AppliedBuffs   현 출력 종에 적용된 강화 카드 리스트

AppliedBuff
  CardData      Source           강화 카드 ScriptableObject
  int           PickCount        중첩 픽 횟수 (×N)
  EMonsterStatKind  Stat
  float         AggregateMultiplier  곱연산 누적 결과
```

### 4.2 신규 이벤트

```text
event Action<int> OnSpawnerSnapshotChanged   인덱스 1개만 갱신 — 셀 단독 리프레시
IReadOnlyList<SpawnerSnapshot> Spawners       늦은 구독자용 현재값
```

기존 `OnBuildChanged` 는 강화 카드 픽 포함 그대로 — `BuildModalPopup` 이 모든 카드 표시.

### 4.3 갱신 트리거

이벤트 기반 갱신은 **종·카운트·강화 변경에만 사용** 한다. Progress 는 매 프레임 폴링이라 이벤트 발행은 비효율적.

- `Spawner.OnOutputTypeChanged` → 해당 인덱스 스냅샷 재계산 (CurrentType + AppliedBuffs)
- `Spawner.OnOutputCountChanged` (신규) → 해당 인덱스 OutputCount 갱신
- `BattleController.OnTypeModifierChanged(EMonster)` (신규) → 동일 종 출력 스포너 모두 AppliedBuffs 재계산
- **Progress 는 View 측 폴링** — `SpawnerStatusCell.Update` 에서 직접 `ISpawnerProgress.Progress` 를 매 프레임 읽어 fillAmount 갱신 (기존 `SpawnerCooldownBar` 패턴 그대로). VM 이벤트 우회

---

## 5. UI 컴포넌트 일람

### 5.1 신규

| 파일 | 역할 |
|---|---|
| `Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs` | 6셀 컨테이너. `BattleViewModel.Spawners` 구독, 셀 풀링은 `CHMPool` (Rule 12) |
| `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs` | 1셀 — 아이콘 row + 색칩 + 종명(`CHText`) + ×N(`CHText`) + 진행 바(`Image fillAmount`). 클릭은 `CHButton` (Rule 11) |
| `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs` | 셀 위 floating 툴팁. `UIBase` 파생, 동일 파일에 `SpawnerStatusTooltipArg` (Rule 13). CHMUI 로 띄움 |
| `Assets/_Lair/Scripts/UI/BuildModalPopup.cs` | 화면 중앙 모달. `UIBase` 파생, 동일 파일에 `BuildModalPopupArg` (Rule 13). `EUI.BuildModalPopup` 키 |

### 5.2 수정

| 파일 | 변경 |
|---|---|
| `Assets/_Lair/Scripts/UI/BattleHud.cs` | `SpawnerStatusPanel` 직렬화 필드 추가 + `Bind` |
| `Assets/_Lair/Scripts/UI/BuildPanel.cs` | 종 강화(6장) 필터 제외. `_detailRoot` · `ShowDetail` 제거. 패널 루트에 `CHButton` 부착해 `CHMUI.ShowUI(EUI.BuildModalPopup, …)` 호출 |
| `Assets/_Lair/Scripts/UI/BattleViewModel.cs` | §4 의 `SpawnerSnapshot` · `OnSpawnerSnapshotChanged` · `Spawners` 추가 |
| `Assets/_Lair/Scripts/UI/BattleStateModel.cs` | 필요 시 SpawnerSnapshot 캐시 추가 (VM 내부 보관도 가능) |
| `Assets/_Lair/Scripts/Battle/Spawner.cs` | `OutputCount` get 노출 + `OnOutputCountChanged` 이벤트. `IncrementOutput` 호출 시 발행 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | `_typeModifierPicks` 추가 + `OnTypeModifierChanged(EMonster)` 이벤트. `RegisterMonsterTypeBuff` 끝에 발행. VM에 6 Spawner 참조 + `OnTypeModifierChanged` 핸들 주입 |
| `Assets/_Lair/Scripts/Battle/CommonInterface.cs` | `ISpawnerOutputProvider` 에 `int OutputCount` + `event Action<int> OnOutputCountChanged` 추가 (또는 신규 `ISpawnerOutputCount` 인터페이스로 분리 — gameplay-programmer 재량) |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EUI` 에 `BuildModalPopup` 추가 (Rule 08·09) |

### 5.3 제거

| 파일 | 사유 |
|---|---|
| `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs` | World-space 진행 바 폐기 — BattleHud 진행 바로 이전 |
| `Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs` | 위 컴포넌트 제거에 따라 테스트도 폐기 |

### 5.4 유지

| 파일 | 사유 |
|---|---|
| `Assets/_Lair/Scripts/Battle/SpawnerBody.cs` | 디스크 색상 틴트 — 출력 종 변경 시각 피드백, 그대로 유지 |
| `Assets/_Lair/Scripts/Battle/Spawner.cs` 의 `Progress` / `ISpawnerProgress` | World 대신 BattleHud 가 읽음 — 인터페이스 자체는 그대로 |

---

## 6. UI 디자인 디테일

### 6.1 셀 레이아웃 (≈46×40 dp)

```text
┌─────────────────┐ ← 상단 강화 아이콘 row (높이 ~10)
│ [H×2] [M]       │   적용된 강화 카드 작은 색칠 원 + 글자 1자 + ×N 배지
├─────────────────┤
│ [■] Wisp   ×2   │ ← 색칩 + 종명(CHText) + 동시 출력(CHText)
│ ▓▓▓▓▓▓▓░░░░     │ ← 진행 바 (Cool/Warm)
└─────────────────┘
```

- 셀 폭 ≈ 46px, 6셀이 화면 하단 가운데 정렬
- 아이콘 row 와 셀 본체 사이 1~2px 간격
- 진행 바: `Image fillAmount`, 색상 Cool `#60A5FA` (0~69%) / Warm `#F97316` (70~100%) — 기존 `SpawnerCooldownBar` 색상·threshold 그대로
- 초기 지연 국면(`Progress = 0`) 은 빈 바 — 기존 정책 유지

### 6.2 강화 카드 아이콘 매핑

| 카드 | 글자 | 배경색 (출력 종 색상) |
|---|---|---|
| WispHpBoost | `H` | `#22C55E` |
| WraithDamageBoost | `D` | `#6B7280` |
| ReaperAtkSpeed | `S` | `#EF4444` |
| HexRangeBoost | `R` | `#EAB308` |
| PhantomMoveSpeedBoost | `M` | `#1F2937` |
| PlagueSlowBoost | `P` | `#A855F7` |

- 작은 색칠 원 (지름 ~10px) + 글자 1자 흰색 (Phantom 검정 배경) 또는 검정 (밝은 배경)
- 중첩 픽: 원 우측 하단 작은 `×N` 배지 (γ 결정)
- 출력 종 변경(Replace) 시 row 즉시 갱신 — 같은 종 출력 스포너 셀들은 동일 아이콘 row

### 6.3 셀 정렬

| 인덱스 | 각도 | 초기 종 (컨셉서 §3.1) |
|---|---|---|
| 0 | 0° | Wisp |
| 1 | 60° | Reaper |
| 2 | 120° | Phantom |
| 3 | 180° | Wisp |
| 4 | 240° | Wraith |
| 5 | 300° | Hex |

좌→우 = 0→5. 카드 픽으로 출력 종이 변해도 인덱스(위치)는 고정.

### 6.4 셀 위 툴팁 (`SpawnerStatusTooltip`)

- 트리거: `SpawnerStatusCell` 클릭 (`CHButton.OnClick`)
- 위치: 셀 바로 위 + 셀을 가리키는 작은 화살표
- 토글: 같은 셀 재클릭 시 닫힘 (다른 셀 클릭 시 닫고 새로 열림)
- 내용:
  - 상단: `Spawner #N — 현재 출력: <종> ×<OutputCount>` (`CHText`)
  - 중단: 적용 강화 리스트 (`CHText` 또는 `CHPoolingScrollView` — 강화 종류 적어서 직접 자식으로도 가능)
    - 예: `WispHpBoost ×2픽 — HP ×2.25 (200 → 450)`
  - 강화가 없으면 `"적용된 강화 없음"` 한 줄
- 디스플레이 크기: 폭 ~140px, 높이 가변

### 6.5 BuildPanel 수정

- 종 강화 카드는 `BuildPanel.Refresh` 의 entry 순회에서 제외 (CardData 카테고리 또는 EffectType 검사로 필터)
- 패시브/액티브 분리 유지 — 추가/교체/환경(패시브) + 저주/버프/와일드(액티브)
- 셀별 `_detailRoot` · `ShowDetail` · `_detailShown` 제거
- 자식 셀(`BuildIconCell`) 의 클릭 콜백도 제거 — 표시 전용 (uGUI Raycaster 가 자식 버튼을 먼저 잡아 패널 루트로 propagate 안 되므로 셀에 버튼 두면 모달이 안 뜸)
- 패널 루트(또는 `Background` Image)에 `CHButton` 부착 → `OnClick` 시 `CHMUI.ShowUI(EUI.BuildModalPopup, new BuildModalPopupArg { ViewModel = vm })`

### 6.6 BuildModalPopup

- 트리거: `BuildPanel` 클릭 또는 외부 호출
- 형태: 화면 중앙 모달, 배경 dim
- 내용:
  - 상단: 타이틀 `"빌드 상세"`
  - 좌측 섹션: 패시브 (강화 6장 **포함** — 모달은 모든 카드 표시)
  - 우측 섹션: 액티브
  - 각 카드: 이름 + 설명 + 중첩 픽 ×N
- 닫기: 배경 외부 클릭 또는 우상단 X (`CHButton`)
- 일시정지 결합: MVP 에선 모달 열어도 게임 진행 (전투 5분 압박 유지). 향후 일시정지 결합은 별도 결정.

---

## 7. 에셋 / 키

### 7.1 Addressable 프리팹 (Rule 08 · 14)

| 프리팹 | 경로 | Addressables 등록 |
|---|---|---|
| `SpawnerStatusPanel.prefab` | `Assets/_Lair/Art/UI/` | BattleHud 자식이라 인스턴스 단일 — Addressable 불필요 (BattleHud 프리팹에 nested) |
| `SpawnerStatusCell.prefab` | `Assets/_Lair/Art/UI/` | `CHMPool` 풀링 대상 — Addressable 등록은 풀 사용 패턴에 따라 (BuildIconCell 선례 참조) |
| `SpawnerStatusTooltip.prefab` | `Assets/_Lair/Art/UI/` | CHMUI 로 띄우려면 `EUI` 키 필요. 단일 인스턴스 충분 |
| `BuildModalPopup.prefab` | `Assets/_Lair/Art/UI/` | `EUI.BuildModalPopup` 필수 |

### 7.2 Enum (Rule 08)

`CommonEnum.cs EUI` 추가:

```text
public enum EUI {
    BattleHud,
    ResultPopup,
    CardSelectionPopup,
    BuildModalPopup,        // 신규
    SpawnerStatusTooltip,   // 신규 (또는 BattleHud 내부 직접 인스턴스화 — 빌더 재량)
}
```

스폰 종류·강화 매핑 enum 은 `EMonster` 와 `EMonsterStatKind` 가 이미 존재 — 신규 없음.

### 7.3 머티리얼

추가 없음. UI 셀 색상은 Image `color` 인라인 (스프라이트 공용 + 색상 교체).

---

## 8. 마이그레이션

### 8.1 씬 `Battle.unity`

- 6 Spawner 오브젝트에서 `CooldownBarWrapper` World-space Canvas 자식 제거
- `SpawnerBody` 자식은 그대로 유지
- 정리 후 씬 저장

### 8.2 에디터 빌더

- 기존 `LairSpawnerVisualBuilder` (또는 비슷한 메뉴) 의 진행 바 빌드 스텝 제거
- 디스크 본체 + 머티리얼 적용 스텝만 남김
- 신규 `LairBattleHudBuilder` (또는 기존 HUD 빌더에 통합) — `SpawnerStatusPanel` · `SpawnerStatusCell` · `BuildModalPopup` 프리팹 빌드 메뉴

### 8.3 테스트 (Rule 별 — test-engineer 영역)

| 기존 | 처리 |
|---|---|
| `SpawnerCooldownBarTests.cs` | 컴포넌트 제거에 따라 폐기 |
| `SpawnerProgressTests.cs` | 유지 — `Spawner.Progress` 인터페이스만 검증 |
| `SpawnerBodyTests.cs` | 유지 — 디스크 색상 틴트 동작 검증 |
| `SpawnerTests.cs` | `OnOutputCountChanged` 이벤트 추가 검증 케이스 추가 |
| `BattleViewModelTests.cs` | `SpawnerSnapshot` · `OnSpawnerSnapshotChanged` 신규 테스트 추가 |

신규 PlayMode 테스트: `SpawnerStatusPanelPlayTests` — VM 변경 → 셀 갱신 통합 검증.

---

## 9. MVP 범위 확인

| 항목 | 범위 |
|---|---|
| Screen-space 6셀 패널 | MVP 내 |
| 셀 상단 강화 아이콘 row | MVP 내 (시너지 가시성 — 컨셉 §5.2) |
| 셀 클릭 툴팁 | MVP 내 |
| BuildPanel 필터 + 모달 | MVP 내 |
| 월드 디스크 본체 유지 | MVP 내 (기존 그대로) |
| 사운드 hook | MVP 외 |
| 모달 열림 시 게임 일시정지 | MVP 외 — 추후 결정 |
| 셀 정렬 옵션 (종 그룹화 등) | MVP 외 — 기본 ring 인덱스 순 |
| 누적 스폰 수 통계 노출 | MVP 외 |

---

## 10. 구현 요청사항 (gameplay-programmer 용)

### Enum
- `CommonEnum.cs EUI` 에 `BuildModalPopup` 추가 (필요 시 `SpawnerStatusTooltip` 도)

### Interface
- `Battle/CommonInterface.cs` — `ISpawnerOutputProvider` 에 `int OutputCount` 와 `event Action<int> OnOutputCountChanged` 추가, 또는 신규 `ISpawnerOutputCount` 인터페이스 분리. 둘 중 선택은 호출부 영향 최소화 기준
- `Spawner.cs` — 위 계약 구현 + `IncrementOutput` 호출 시 이벤트 발행

### Data 노출 (BattleController)
- `_typeModifierPicks: Dictionary<EMonster, List<AppliedBuff>>` 신규 — 종별로 어떤 강화 카드를 몇 번 픽했는지 기록 (`§4.1` AppliedBuff 구조)
- `OnTypeModifierChanged: Action<EMonster>` 신규 이벤트 — 픽 등록 마지막에 발행
- **권장 — `RegisterMonsterTypeBuff` 시그니처 확장**: `RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier, CardData source)` — `source` 가 `_typeModifierPicks` 기록의 키가 된다. 모든 강화 카드 효과(WispHpBoostEffect 등) 의 `Apply(IBattleContext ctx)` 가 자신의 `CardData` 를 전달하도록 함께 수정 필요. `IBattleContext` 시그니처도 같이 확장
- 대안: 카드 픽 시점(`AddPick`) 에서 카드 카테고리 검사로 추정 — 그러나 카드→종 매핑이 효과 클래스 내부에 숨어 있어 깨지기 쉬움. **권장안(시그니처 확장)** 으로 진행하고, 영향 범위가 크면 gameplay-programmer 가 재검토

### ViewModel (BattleViewModel)
- `SpawnerSnapshot` 구조체 · `IReadOnlyList<SpawnerSnapshot> Spawners` · `OnSpawnerSnapshotChanged(int index)`
- Spawner 6개와 BattleController hook 을 받아 캐시 유지 + Progress 폴링

### UI (Rule 05 · 11 · 13)
- `SpawnerStatusPanel.cs` · `SpawnerStatusCell.cs` · `SpawnerStatusTooltip.cs` · `BuildModalPopup.cs` 신규
- `CHText` / `CHButton` 사용, `UIBase` 와 동일 파일에 `UIArg` 페어
- 셀 풀링 `CHMPool`
- BuildPanel.cs 수정 — 종 강화 필터 제외, `_detailRoot` 제거, 패널 클릭 → `CHMUI.ShowUI(EUI.BuildModalPopup, …)`

### 에셋 경로 (Rule 14)
- `Assets/_Lair/Art/UI/SpawnerStatusPanel.prefab`
- `Assets/_Lair/Art/UI/SpawnerStatusCell.prefab`
- `Assets/_Lair/Art/UI/SpawnerStatusTooltip.prefab`
- `Assets/_Lair/Art/UI/BuildModalPopup.prefab`

### 마이그레이션
- `SpawnerCooldownBar.cs` + `SpawnerCooldownBarTests.cs` 삭제
- `Battle.unity` 의 6 Spawner 에서 `CooldownBarWrapper` 자식 제거
- 에디터 빌더에서 진행 바 빌드 스텝 제거

### 색상 상수 (변경 없음)
| 용도 | Hex |
|---|---|
| Fill Cool (0~69%) | `#60A5FA` |
| Fill Warm (70~100%) | `#F97316` |
| 종 색상 (6종) | 컨셉 §11.4 매핑 그대로 |

---

## 11. 검증 가설

본 변경이 다음 사용자 경험을 개선하는지 5분 한 판 플레이로 검증한다.

1. 카드 픽 화면에서 "어느 스포너에 무엇이 적용됐는지" 를 한눈에 보고 결정한다 (시너지 가시성)
2. 6개 스포너 상태를 화면 하단 한 줄로 동시 비교한다 (공간 분산 ↓)
3. BuildPanel 모달로 픽한 모든 카드 효과를 빠르게 복기한다

검증 실패 시: 셀 정렬 옵션(종 그룹화) 도입, 모달 일시정지 결합, 디스크 본체 색상 강조 강화 등 후속 작업으로 분기.

---

## 변경 이력

- **v0.1 (2026-05-27)**: 초안. World-space 진행 바를 BattleHud 6셀로 이전. 셀 상단 강화 아이콘 row + 셀 클릭 툴팁. BuildPanel 종 강화 필터 + 클릭 모달. 디스크 본체 유지.
