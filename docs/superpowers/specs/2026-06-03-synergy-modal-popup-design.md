# 시너지 효과 모달 팝업 — 설계 (spec)

> 작성일 2026-06-03 · 단계 MVP · 관련 컨셉 §5.2(시너지 가시성) · 기획서 `docs/design/card-renewal.md` §4.2

## 1. 의도 / 범위

좌측 상단 `BuildSynergyPanel`(4축 카운트·티어 상시 표시)을 클릭하면, 패시브/액티브 빌드 상태창(`BuildModalPopup`)과 같은 형태의 모달이 열려 **현재 적용된(임계 도달한) 시너지 효과 목록**을 보여준다.

- **검증 가치**: 플레이어가 "내 빌드에 지금 무슨 시너지가 걸려 있는지"를 카드 픽 팝업이 닫힌 상태에서도 한 번에 확인 — 컨셉 §5.2 시너지 가시성 강화.
- **범위**: 읽기 전용 표시 UI. 시너지 발동 로직(`BuildSynergyService`·`Synergy/*.cs Tier 클래스`)은 **일절 변경하지 않는다**.

### 결정 락 (사용자 승인 2026-06-03)
1. **표시 내용**: 활성 티어만 — 임계(3/5/7장) 도달한 Tier 효과만 나열. 미달 축은 표시 안 함.
2. **설명 데이터 위치**: 코드 정적 테이블 (`static readonly Dictionary`). SO 미사용 (MVP 범위 절약).
3. **셀 구성**: 셀 1종이 `Kind`(Header/Effect) 플래그로 축 헤더 행 / 티어 효과 행 분기.

## 2. 트리거

- `BuildSynergyPanel` 루트에 `CHButton _rootButton` 추가 (`BuildPanel._rootButton`과 동일 관례).
- 클릭 시 `CHMUI.Instance.ShowUI(EUI.SynergyModalPopup, new SynergyModalPopupArg { ViewModel = _vm })`.
- 리스너 수명은 `CompositeDisposable`로 관리, `Unbind` 시 `Clear` (BuildPanel 동일 패턴).

## 3. 표시 내용 — 활성 티어만

- 데이터 소스: `BattleViewModel.GetBuildCount(EBuildAxis)` 로 4축 카운트 조회.
- 임계 배열 `{3, 5, 7}` 기준 도달한 Tier 만 행 생성.
- 한 축이라도 활성 티어가 있으면:
  - **축 헤더 행** — `TANK (5장)` + 축 색 (`BuildSynergyPanel.AxisColor`/`AxisLabel` 재사용).
  - **티어 효과 행** (활성 티어 수만큼) — `Tier1  Wisp·Wraith HP ×1.3`.
- 모든 축 < 3장(활성 티어 0개) → 빈 상태 라벨 `아직 발동한 시너지가 없습니다` 표시, 스크롤 리스트 비움.
- 정렬: 축 순서 Tank→Dps→Debuff→Swarm, 축 내 Tier1→2→3.

## 4. 효과 설명 데이터 (코드 정적 테이블)

`SynergyModalPopup` 내부에 12개 (`(EBuildAxis, tier) → string`) 정적 테이블. 문구는 기획서 §4.2 마스터 표 그대로:

| 축 | Tier1 (3장) | Tier2 (5장) | Tier3 (7장) |
|---|---|---|---|
| Tank | Wisp·Wraith HP ×1.3 | Wisp·Wraith Power ×1.2 | 필드 캡 +6 (18→24) |
| Dps | Reaper·Hex Power ×1.3 | Reaper·Hex 공속 +25% | Reaper·Hex Range ×1.3 |
| Debuff | Plague 둔화 ×0.8 | 영웅 공격력 ×0.85 | 출혈 영구 — 이동 시 1s당 HP -1% |
| Swarm | Phantom·Wisp 이동속도 ×1.3 | 모든 스포너 주기 ×0.85 | 모든 스포너 동시 출력 +1 |

- 축 색/라벨은 `BuildSynergyPanel`의 `public static` 딕셔너리를 **재사용**한다 — 중복 정의 금지 (Rule 02 §5).

## 5. UI 구조 (BuildModalPopup 패턴 — Rule 03 §3)

```
SynergyModalPopup.prefab (UIBase)
├─ DimButton (CHButton, #000 α0.6)   — 클릭 시 Close(reuse: true)
├─ CloseButton (CHButton, X)         — 클릭 시 Close(reuse: true)
├─ EmptyText (CHText)                — 활성 티어 0개일 때만 활성
└─ ScrollView
   └─ SynergyModalCardPoolingScrollView : CHPoolingScrollView<SynergyModalCell, SynergyModalCellData>
       └─ origin: SynergyModalCell.prefab (풀 prototype)

SynergyModalCell.prefab (한 행)
└─ RectTransform + 축 색 띠(Image) + CHText(라벨/효과) — Kind 로 표시 전환
```

- **3-class 분리** (Rule 03 §3 BuildModalPopup 패턴):
  - `SynergyModalPopup : UIBase` — `[SerializeField] _scrollView` 참조, `Build(vm)` 로 데이터 가공 → `SetItemList`. `OnBuildChanged` 구독.
  - `SynergyModalCardPoolingScrollView : CHPoolingScrollView<…>` — `InitItem`/`InitPoolingObject` 만 오버라이드, `_origin` 인스펙터.
  - `SynergyModalCell : MonoBehaviour` — `[SerializeField]` 자식 참조, `Bind(data)`, `OnEnable` 풀 재사용 리셋.
- 코드 동적 GameObject 생성 금지 — 모든 GameObject 는 prefab 정적 배치 + 인스펙터 참조.

### 셀 데이터
```
class SynergyModalCellData
{
    enum Kind { Header, Effect }
    Kind   RowKind;
    Color  AxisColor;     // 헤더·효과 공통 (축 색 띠)
    string Label;         // Header: "TANK (5장)" / Effect: "Tier1  Wisp·Wraith HP ×1.3"
}
```

## 6. 갱신 / 수명

- 팝업 열린 동안 `vm.OnBuildChanged` 구독 → 픽 발생 시 `Build` 재호출로 자동 갱신 (BuildModalPopup 동일).
- `closeDisposable` 로 구독 해제 + `_vm` null 화 (cached UI 재사용 대비).
- prefab active/inactive 저장 양쪽 케이스 대응: `InitUI` 끝 `isActiveAndEnabled` 분기 + `OnEnable` 에서 `ForceRebuildLayoutImmediate` 후 `Build` (BuildModalPopup `BuildAndLayout` 패턴 그대로).

## 7. 신규 / 변경 파일

**신규**
- `Assets/_Lair/Scripts/UI/SynergyModalPopup.cs` (+ `SynergyModalPopupArg`)
- `Assets/_Lair/Scripts/UI/SynergyModalCell.cs` (+ `SynergyModalCellData`)
- `Assets/_Lair/Scripts/UI/SynergyModalCardPoolingScrollView.cs`
- `Assets/_Lair/Art/UI/SynergyModalPopup.prefab`
- `Assets/_Lair/Art/UI/SynergyModalCell.prefab`

**변경**
- `Assets/_Lair/Scripts/Data/CommonEnum.cs` — `EUI.SynergyModalPopup` 추가 (prefab명과 정확히 일치, Rule 03 §2).
- `Assets/_Lair/Scripts/UI/BuildSynergyPanel.cs` — `CHButton _rootButton` 필드 + 클릭 → ShowUI + `CompositeDisposable` 수명 관리. `Bind`/`Unbind` 보강.
- `BuildSynergyPanel.prefab` — 루트에 CHButton 추가 + `_rootButton` 인스펙터 연결.

## 8. 비범위 (YAGNI)

- 미달 티어/로드맵 표시 — 안 함 (활성 티어만).
- 시너지 효과 수치 밸런스 변경 — 안 함.
- 사운드/아트 — MVP §8 금지.
- SO 기반 설명 데이터 — 안 함.

## 9. 테스트 관점 (test-engineer 입력)

- `Build` 데이터 가공: 축 카운트 조합별로 헤더+효과 행 수가 맞는가 (예: Tank 5 → 헤더1+효과2, Dps 3 → 헤더1+효과1).
- 활성 티어 0개 → 빈 리스트 + EmptyText 활성.
- 7+ 도달 → Tier1·2·3 모두 행 생성.
- 정렬: 축 순서·티어 순서.
- `TierDesc` 12개 키 전부 존재 (누락 키 접근 시 빈 문자열/방어).
