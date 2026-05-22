# 빌드 패널 (Build Panel) 설계서

> Project Lair — 플레이어가 픽한 카드를 HUD에 항상 표시하는 빌드 아이콘 패널.
> 작성일: 2026-05-21
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
플레이어(던전 주인)가 이번 런에 픽한 카드를 HUD에 항상 노출해, "내가 어떤 빌드를 쌓았는지"를
한눈에 파악하게 한다. 현재 HUD는 타이머·영웅 HP만 표시하고 누적 픽을 보여주지 않는다.

### 0.2 In Scope
- 픽한 카드를 패시브 / 액티브 분리 섹션으로 HUD에 항상 표시
- 카드별 고유 아이콘(25종), 4카테고리 색 프레임
- 같은 카드 중복 픽은 ×N 배지로 누적
- 아이콘 클릭 시 이름 + 설명 상세 표시

### 0.3 Out of Scope
- 아이콘 스프라이트 아트 제작 — 누락 시 카테고리 색 폴백으로 동작 (PNG는 추후 사용자가 제작)
- 액티브 효과의 잔여 시간 표시 — 본 기능은 "픽 목록"이지 "활성 효과 상태"가 아님
- 토글/숨김 패널 — 항상 표시로 확정
- 저주/버프/와일드 전용 카테고리·색 신설 — 기존 4카테고리 색 유지

### 0.4 검증 가설
"플레이어가 전투 중 자신의 누적 빌드(패시브/액티브 픽)를 아이콘으로 즉시 확인할 수 있는가."

---

## 1. 프로젝트 룰 매핑

| 룰 | 적용 |
|---|---|
| 01 자동 커밋 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만, 관련 파일 `git add` 까지 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | `BuildPanel`/`BuildIconCell` 은 VM·`CardData`(데이터)만 참조. 역참조 없음 |
| 04 프리팹화 | 아이콘 셀은 `BuildIconCell.prefab` 단일 프리팹, 픽마다 인스턴스 |
| 05 MVVM | 픽 추적·집계는 `BattleViewModel`. View(`BuildPanel`/`BuildIconCell`)는 표시·입력만 |
| 07 ChvjPackage | `CHMPool`(셀 스폰)·`CHText`/`CHButton` 재사용 |
| 08 Enum 키 | 아이콘 PNG 파일명 = `ECardId` 값명 (`SlimeHpBoost.png` 등) |
| 11 CHText/CHButton | ×N 배지·상세 텍스트는 `CHText`, 셀 클릭은 `CHButton` |
| 12 CHMPool 스폰 | `BuildIconCell` 은 `CHMPool.Pop/Push` |
| 13 UIArg | `BattleHudArg` 변경 없음 — `BuildPanel` 은 `UIBase` 가 아닌 HUD 하위 컴포넌트 |
| 14 에셋 폴더 | 아이콘 → `Art/Sprites/CardIcons/`, `BuildIconCell.prefab` → `Art/UI/` |

---

## 2. Part 1 — 데이터 계층

### 2.1 `CardData._icon`
`CardData` 에 아이콘 스프라이트 필드 추가:

```csharp
[SerializeField] private Sprite _icon;
public Sprite Icon => _icon;
```

### 2.2 `LairCardPrefabBuilder` 아이콘 자동 할당
- 신규 상수 `IconDir = "Assets/_Lair/Art/Sprites/CardIcons"`.
- `BuildCardsAndPool` 의 카드 루프에서 id/category/name/description 설정 후:
  `<IconDir>/<spec.Id>.png` 를 `Sprite` 로 로드해 `_icon` 에 배정 (`SerializedObject`).
- PNG 의 `textureType = Sprite` / `spriteImportMode = Single` 임포트 설정은 빌더가 보정
  (`LairCharacterPrefabBuilder.EnsureHpBarPrefab` 의 스프라이트 처리 패턴 재사용).
- `_icon` 은 `_effect`(C-M2 비파괴 보존 대상)와 달리 **매 rebuild 마다 이름 규칙으로 재배정** —
  이름 붙은 PNG 파일이 진실. 파일 미존재 시 `null` 배정 (런타임 폴백).

### 2.3 픽 추적 — `BattleViewModel`

```csharp
//# 빌드 패널 1개 항목 — 카드 + 패시브 여부 + 중복 픽 횟수
public class BuildEntry
{
    public CardData Card;
    public bool IsPassive;
    public int Count;
}

public IReadOnlyList<BuildEntry> Build => _build;   //# 늦은 구독자용 현재값
public event Action OnBuildChanged;

public void AddPick(CardData card, bool isPassive);
```

- `AddPick` — `_build` 에서 같은 `Card` 엔트리가 있으면 `Count++`, 없으면 신규 엔트리 추가.
  이후 `OnBuildChanged` 발행. ×N 집계가 VM 에서 일어나 EditMode 테스트 가능.
- 한 `CardData` 는 패시브 풀 또는 액티브 풀 중 한쪽에만 존재하므로 `Card` 단독 키로 충분.
- 픽 목록은 `CardData`(ScriptableObject)를 참조 → "Unity 의존성 0" 인 `BattleStateModel` 이 아닌
  **`BattleViewModel`** 에 보유.

### 2.4 `BattleController` 연동
`TryProcessNext` 의 `OnPicked` 콜백에 추가 (기존 `_recorder.RecordPick` 옆):

```csharp
if (card != null)
    _vm.AddPick(card, entry.SourceType == TriggerQueue.Source.Passive);
```

`entry` 는 `while` 루프가 디큐한 변수 — `await tcs.Task` 로 픽 해소까지 반복이 진행되지 않으므로
클로저 캡처 안전.

---

## 3. Part 2 — UI 계층

### 3.1 `BuildIconCell` (신규)
- `Scripts/UI/BuildIconCell.cs` + `Art/UI/BuildIconCell.prefab`
- 구성:
  - `Image _iconImage` — `card.Icon`. `null` 이면 비활성 (프레임 색이 폴백 역할)
  - `Image _frameImage` — 카테고리 색 (`CardView.CategoryColor` 와 동일 매핑)
  - `CHText _countText` — `×N` 배지. `Count >= 2` 일 때만 표시
  - `CHButton _button` — 클릭 시 상세 콜백
- API: `Bind(CardData card, Action onClick)`, `SetCount(int count)`
- `OnEnable` 에서 상태 리셋 (카운트 텍스트 비활성, 아이콘 클리어) — 풀 재사용 대비 (Rule 12)

### 3.2 `BuildPanel` (신규)
- `Scripts/UI/BuildPanel.cs` — `BattleHud` 프리팹의 자식 컴포넌트 (`UIBase` 아님)
- 직렬화 참조: 패시브 섹션 컨테이너 `Transform`, 액티브 섹션 컨테이너 `Transform`,
  셀 프리팹 `GameObject`, 상세 패널 (루트 `GameObject` + 이름 `CHText` + 설명 `CHText`)
- `Bind(BattleViewModel vm)`:
  - `vm.OnBuildChanged` 구독, `BattleHud.closeDisposable` 로 해제
  - 초기 동기화 — `vm.Build` 로 1회 정합
- `OnBuildChanged` 처리 — `vm.Build` 재조회 후 셀 정합:
  - `Dictionary<CardData, BuildIconCell>` 로 카드별 1셀 관리
  - 신규 엔트리 → `CHMPool.Pop` 으로 셀 생성, `IsPassive` 에 따라 해당 섹션 컨테이너에 배치
  - 기존 엔트리 → `SetCount`
  - 픽은 추가만 되고 제거 없음 (한 판 = 한 씬, 재시작 시 새 HUD)
- 상세: 셀 클릭 → `ShowDetail(card)` (이름·설명 채우고 상세 패널 활성). 재클릭/다른 셀 클릭 시 갱신·토글.

### 3.3 `BattleHud` 연동
- `BattleHud` 에 `[SerializeField] BuildPanel _buildPanel` 추가.
- `BattleHud.Bind(vm)` 에서 `_buildPanel.Bind(vm)` 호출.
- `BattleHudArg` / `UIArg` 변경 없음 (Rule 13).

### 3.4 UI 프리팹 빌드
- `BattleHud.prefab` 은 `LairUIPrefabBuilder.BuildBattleHud` 가 절차적으로 생성한다.
- `LairUIPrefabBuilder` 확장:
  - 신규 메서드로 `BuildIconCell.prefab` 절차적 생성 (Image×2 + CHText + Button + CHButton — Rule 11)
  - `BuildBattleHud` 확장 — 화면 하단에 패시브/액티브 섹션 컨테이너 + 상세 패널 추가, `_buildPanel` 와이어
- 빌드 스트립을 빌더 절차에 포함시켜, 빌더 재실행이 수동 편집분을 잃지 않게 한다.

### 3.5 배치 (참고)
```
                         5:00
      [영웅 머리 위 HP바]
      ... 전장 ...
┌ 패시브 ───────────────┐ ┌ 액티브 ────────┐
│ [▣][▣²][▣][▣]        │ │ [▣][▣]        │
└───────────────────────┘ └────────────────┘
   클릭 → 이름+설명 상세
```
정확한 위치·크기는 프로토타입에서 튜닝.

---

## 4. 테스트

### 4.1 EditMode (TDD — POCO)
- `BattleViewModel.AddPick` — 카드 픽 → `Build` 엔트리 1개·`Count` 1 / 같은 카드 재픽 → `Count` 2 /
  패시브+액티브 픽 → `IsPassive` 정확 / `OnBuildChanged` 발행. 기존 `FakeCardData` 헬퍼 사용.

### 4.2 PlayMode
- 기존 `BattleSmokeTest` / `CardFlowSmokeTest` 가 `BattleHud` 에 `BuildPanel` 추가 후에도
  통과 (씬 로드·HUD 표시 무영향) 확인. 픽 0 일 때 빈 패널.

### 4.3 수동 검증
- 한 판 플레이 — 픽이 올바른 섹션에 아이콘으로 누적 / 중복 ×N / 클릭 상세 /
  아이콘 누락 시 카테고리 색 폴백 / 빌더 rebuild 가 `<ECardId>.png` 자동 배정.

---

## 5. 마일스톤

| MS | 산출물 | 검증 |
|---|---|---|
| M1 | `CardData._icon` + `LairCardPrefabBuilder` 아이콘 자동 할당 + `BattleViewModel` 픽 추적(`AddPick`/`Build`/`OnBuildChanged`/`BuildEntry`) + `BattleController` 연동 + EditMode 테스트 | EditMode PASS, rebuild 시 `_icon` 배정 확인 |
| M2 | `BuildIconCell`(스크립트+프리팹), `BuildPanel`(스크립트), `BattleHud` 연동, `LairUIPrefabBuilder` 확장(셀 프리팹 + HUD 스트립·상세 패널), ×N 배지·클릭 상세·아이콘 폴백 | 컴파일, PlayMode 스모크 PASS, 수동 검증 |

각 마일스톤은 컴파일·테스트 통과하는 동작 상태를 유지.

---

## 6. 위험 요소

| 위험 | 완화 |
|---|---|
| 아이콘 스프라이트 25종 부재 | 빌더가 누락 PNG 에 `null` 배정, 셀이 `null` 아이콘 → 카테고리 색 프레임이 폴백 |
| `BattleHud.prefab` 수정이 PlayMode 스모크 깨뜨림 | 픽 0 일 때 빈 패널 — 씬 로드·HUD 표시 무영향. M2 에서 재실행 확인 |
| 풀 재사용 시 셀 상태 잔존 | `BuildIconCell.OnEnable` 에서 카운트·아이콘 리셋 (Rule 12 패턴) |
| `LairUIPrefabBuilder` 재실행이 BattleHud 수동 편집 덮어씀 | 빌드 스트립을 빌더 절차에 포함 — 빌더가 곧 진실 |
| `BattleViewModel` 의 `CardData` 참조로 테스트 어려움 | 기존 `FakeCardData` 테스트 더블 사용 |
| C-M2 비파괴 rebuild 와 `_icon` 재배정 충돌 | `_icon` 은 보존 대상 아님 — `_displayName` 처럼 빌더가 매번 설정 |

---

## 7. 성공 기준 (사용자 검증)

- [ ] 패시브 / 액티브 픽이 분리된 섹션에 아이콘으로 누적
- [ ] 같은 카드 중복 픽 시 ×N 배지
- [ ] 아이콘 클릭 시 이름 + 설명 상세 표시
- [ ] 아이콘 스프라이트 누락 시 카테고리 색 폴백
- [ ] 빌더 rebuild 가 `<ECardId>.png` 를 자동 배정
- [ ] EditMode + PlayMode 테스트 전부 PASS
