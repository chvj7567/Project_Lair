# 시너지 효과 모달 팝업 — 기획서

> 작성일 2026-06-03 · 단계 MVP · game-designer
> 입력: spec `docs/superpowers/specs/2026-06-03-synergy-modal-popup-design.md` · plan `docs/superpowers/plans/2026-06-03-synergy-modal-popup.md`
> 연관 단일 진실: 컨셉 §5.2(시너지 가시성) · `docs/design/card-renewal.md` §4.2(Tier 마스터 표) / §5.3·§8.0(빌드 시너지 패널·아이콘) / §2.6~2.7(BuildModalPopup)
> **델타 이력**:
> - 2026-06-03 후속 A — 헤더 행에 축 아이콘 추가 (사용자 화면 확인 후 요청). §3.1·§4·§8·§10 갱신, §10 이 해당 델타의 변경점 단일 요약.
> - 2026-06-03 후속 B — 모달 스크롤바 추가 (풀링 부분표시 근본원인 디버깅 확정 후 사용자 결정: 스크롤 유지 + 스크롤바). §4·§4.1(신규)·§7·§8·§11(신규) 갱신. **§11 이 본 델타의 변경점 단일 요약.**

---

## § 헤더

- **목표**: 좌측 상단 `BuildSynergyPanel` 클릭 시, 현재 적용된(임계 도달) 시너지 효과 목록을 `BuildModalPopup` 과 같은 형태의 모달로 보여준다.
- **검증 가설**: 카드 픽 팝업이 닫힌 상태에서도 "내 빌드에 지금 무슨 시너지가 걸려 있는지"를 한 번에 확인할 수 있으면, 컨셉 §5.2 시너지 가시성이 강화되어 빌드 의도성이 높아지는가.
- **현재 단계 범위 적합성**: **범위 내**. 읽기 전용 표시 UI 로 시너지 발동 로직·수치 무변경 (밸런스 불변). MVP §8 제약(프리미티브·아트 금지·사운드 금지) 준수 — 색 띠 + 텍스트만 사용, 사운드 hook 미등록.
- **핵심 메커니즘**: `BattleViewModel.GetBuildCount(axis)` 로 4축 카운트를 읽어 임계(3/5/7) 도달 Tier 만 `[축 헤더 행 + 활성 티어 효과 행]` 으로 평탄화 → 단일 세로 `CHPoolingScrollView` 로 표시. 활성 티어 0개면 빈 상태 라벨.
- **2026-06-03 델타 A**: 축 헤더 행 좌측에 **축 아이콘(Image)** 을 추가 표시. `[축 아이콘] {축라벨} ({카운트}장)` 순. 아이콘은 `BuildSynergyPanel` 의 인스펙터 직접 Sprite 참조 관례를 그대로 차용(`SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png` 4장 재사용 — 신규 아트 없음, MVP §8 준수). 효과 행은 아이콘 없음(기존 색 띠 + 텍스트 유지).
- **2026-06-03 델타 B**: 행 수가 뷰포트를 넘으면 풀링상 가시 행(약 11개)까지만 활성·표시되고 나머지는 스크롤해야 보인다(풀링 정상 동작). 이를 "한눈에 본다" 목적과 절충하기 위해 **세로 스크롤바를 추가**한다. 모달/행 높이·풀링 동작 무변경, "전부-한화면"이 아니라 "스크롤 + 아래에 더 있음 affordance" 로 락. 스크롤바 가시성은 **AutoHideAndExpandViewport** (짧은 목록에서는 자동 숨김) 로 결정 — §4.1.

---

## 1. 표시 데이터 규칙 (활성 티어만)

- 데이터 소스: `BattleViewModel.GetBuildCount(EBuildAxis)` — 4축 누적 픽 카운트 (card-renewal §4.1 누적 정책).
- 임계 배열 `{3, 5, 7}`. 활성 티어 수 = count 가 도달(>=)한 임계 개수.
  - count < 3 → 활성 티어 0 (해당 축 표시 안 함)
  - 3 <= count < 5 → 활성 티어 1
  - 5 <= count < 7 → 활성 티어 2
  - count >= 7 → 활성 티어 3
- 한 축의 활성 티어가 1개 이상이면: **축 헤더 행 1개 + 활성 티어 수만큼 효과 행** 을 생성.
- 활성 티어 0개 축은 헤더·효과 모두 생성하지 않는다 (스킵).
- 모든 축이 활성 티어 0개 → 행 리스트 비움 + 빈 상태 라벨 표시 (§5).
- **정렬**: 축 순서 `Tank → Dps → Debuff → Swarm` (`BuildSynergyPanel.AllAxes` 와 동일 순서), 축 내 `Tier1 → Tier2 → Tier3`.

> card-renewal §4.1 누적 정책(유지 + 추가)과 정합: Tier2 도달 빌드는 Tier1+Tier2 가 동시 적용 상태이므로, 본 모달도 Tier1·Tier2 효과 행을 **모두** 표시한다 (활성 = 도달한 모든 하위 티어 포함).

---

## 2. Tier 효과 표시 문구 (최종 한글 표기 — 12개)

코드 정적 테이블(`TierDesc`, `(EBuildAxis, tier) → string`)의 표시 문구 정본. card-renewal §4.2 마스터 표와 정합하며, plan Task2 `TierDesc` 와 글자 그대로 일치한다.

| 축 | Tier1 (3장) | Tier2 (5장) | Tier3 (7장) |
|---|---|---|---|
| **Tank** | `Wisp·Wraith HP ×1.3` | `Wisp·Wraith Power ×1.2` | `필드 캡 +6 (18→24)` |
| **Dps** | `Reaper·Hex Power ×1.3` | `Reaper·Hex 공속 +25%` | `Reaper·Hex Range ×1.3` |
| **Debuff** | `Plague 둔화 ×0.8` | `영웅 공격력 ×0.85` | `출혈 영구 — 이동 시 1s당 HP -1%` |
| **Swarm** | `Phantom·Wisp 이동속도 ×1.3` | `모든 스포너 주기 ×0.85` | `모든 스포너 동시 출력 +1` |

### 2.1 §4.2 마스터 표 대비 표기 차이 (플레이어 표기로 한글화한 토큰 3건 — 변경 아님)

- 플레이어 표기로 한글화한 토큰은 3건(Dps2 공속, Debuff1 둔화, Swarm1 이동속도)이며 효과 수치는 §4.2 와 동일하다. 3건 모두 plan Task2 `TierDesc` 와 글자 그대로 일치한다.
  - **Dps Tier2**: `Reaper·Hex Cooldown ×0.8` → `Reaper·Hex 공속 +25%` (검산: 1 / 0.8 = 1.25 = +25%)
  - **Debuff Tier1**: `Plague SlowFactor ×0.8` → `Plague 둔화 ×0.8`
  - **Swarm Tier1**: `Phantom·Wisp MoveSpeed ×1.3` → `Phantom·Wisp 이동속도 ×1.3`
- 본 모달은 **플레이어용 표시 UI** 이므로 위 3건은 시스템 구현 표기 대신 플레이어 표기를 채택한다.
- 나머지 9개 문구는 §4.2 표의 효과 부분을 그대로 옮긴 것이며, 괄호 안 보조 설명(`글로벌 영구`, `강한 둔화 추가`, `HeroAttackDown 자동 등록`, `라운드 끝까지` 등 시스템/누적 설명)은 플레이어가 읽을 표시 문구에서 제외했다. 표시되는 효과 본문 수치는 모두 §4.2 와 동일.
- **이 표가 표시 문구의 단일 진실.** 이후 §4.2 표의 효과 수치가 바뀌면 본 표와 코드 `TierDesc` 를 동시 갱신한다 (plan↔기획서 sync 규칙).

---

## 3. 행 텍스트 포맷 / 정렬

행은 2종이며 셀 1종(`SynergyModalCell`)이 `RowKind`(Header/Effect) 로 렌더 분기한다.

### 3.1 축 헤더 행 (Header)

- **레이아웃 (좌→우)**: `[색 띠] · [축 아이콘(Image, 28×28px)] · [라벨 텍스트]`. 라벨 텍스트 포맷은 `{축라벨} ({카운트}장)` — 예: `TANK (5장)`, `DPS (3장)`.
  - 순서: 색 띠 → (여백 8px) → 아이콘(28px) → (간격 8px) → 라벨(가변 stretch).
  - **아이콘 영역(아이콘 28 + 우측 간격 8 = 36px)** 은 효과 행에도 동일 폭으로 예약된다 (§8 `LayoutElement` 36px). 이로써 헤더 라벨 시작 X좌표 = 효과 라벨 시작 X좌표 (들여쓰기 일관). 검산: 라벨 시작 X = 색띠폭 + 8(여백) + 36(아이콘 예약).
- **축 아이콘**: 해당 축 Sprite (`SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png`). 색 틴트 없음 — 원본 색 그대로 (card-renewal §8.0 아이콘 정책과 동일). 아이콘 미할당(null)이면 아이콘 Image 비활성, 라벨만 표시 (가드).
- 축라벨은 `BuildSynergyPanel.AxisLabel[axis]` 재사용 (대문자 `TANK`/`DPS`/`DEBUFF`/`SWARM` — §3.4 표기 결정 참조).
- 카운트는 `GetBuildCount(axis)` 원값 (해당 축 누적 픽 수). `(N장)` 표기 유지 (사용자 요청: 카운트 표기 유지).
- 좌측 축 색 띠 색상 = `BuildSynergyPanel.AxisColor[axis]` 재사용 (Tank 녹/Dps 적/Debuff 보라/Swarm 남색).
- 시각 강조: MVP 텍스트 전용 — 굵게/들여쓰기 없이 색 띠 + 아이콘 + `(N장)` 표기로 헤더/효과를 구분한다 (별도 폰트 변형 금지, plan Task5 비고와 일치).

### 3.4 시너지명 표기 — 대문자 `AxisLabel` 재사용 (락)

사용자 요청 본문은 "Tank" 였으나, 헤더 표기는 **기존 대문자 `AxisLabel`(`TANK`/`DPS`/`DEBUFF`/`SWARM`) 을 그대로 재사용**한다 (Title-case 로 변경하지 않음).

- **결정**: 모달 헤더 시너지명 = `BuildSynergyPanel.AxisLabel[axis]` (대문자), 모달 전용 별도 표기 테이블을 두지 않는다.
- **근거 (대안 비교)**:
  - **대안 A — `AxisLabel` 대문자 그대로 재사용 (채택)**: `BuildSynergyPanel` 좌측 패널·카드 축 태그(card-renewal §8.3) 와 표기가 통일된다. 단일 진실 1곳(`AxisLabel`) 유지 → Rule 02 §5 중복 정의 회피. 코드 변경 0.
  - **대안 B — `AxisLabel` 을 Title-case(`Tank`)로 변경**: 사용자가 적은 표기와 일치하나, `AxisLabel` 은 `BuildSynergyPanel` 좌측 패널 헤더에도 공유되므로(card-renewal §8.0 다이어그램 `TANK`/`DPS`/`SWARM` 대문자) 좌측 패널 표기까지 동시에 바뀐다 — 본 델타 범위(모달만) 를 넘는 회귀. 거부.
  - **대안 C — 모달 전용 Title-case 표기 테이블 신설**: 같은 개념(축 이름)에 두 표기가 공존 → 일관성 저하·유지 부담. 사용자가 적은 "Tank" 는 *구어적 지칭*일 뿐 표기 변경 요청이 아니라고 판단. 거부.
- **결론**: 헤더는 `TANK (5장)` 형태(대문자 유지). 사용자가 좌·우 패널 전체를 Title-case 로 통일하고 싶다면 이는 본 델타 범위 밖의 별도 표기 변경 작업으로 분리한다 (그 경우 `AxisLabel` 1곳만 고치면 양쪽 동시 반영됨).

### 3.2 티어 효과 행 (Effect)

- **포맷**: `Tier{n}  {효과문구}` — `Tier` + 티어번호 + **공백 2칸** + §2 표의 효과 문구.
  - 예: `Tier1  Wisp·Wraith HP ×1.3`, `Tier3  출혈 영구 — 이동 시 1s당 HP -1%`.
- 공백 2칸은 티어 토큰과 효과 문구의 시각 분리용 (단일 행 내 정렬 컬럼 대용). plan `$"Tier{tier}  {desc}"` 와 일치.
- 좌측 축 색 띠 색상 = 같은 축 헤더와 동일 색 (효과 행도 축 색 띠를 가져 같은 축 그룹임을 시각적으로 잇는다).
- **아이콘 없음**: 효과 행은 헤더와 달리 아이콘 Image 를 표시하지 않는다. `SynergyModalCellData.Icon` 이 null(효과 행은 항상 null) 이므로 셀이 `RowKind == Effect` 일 때 아이콘 Image 를 비활성화한다 (§8 셀 분기). 아이콘은 축 그룹 헤더에만 1회 표시 — 효과 행은 색 띠로만 같은 축임을 잇는다.

### 3.3 정렬 (행 순서)

- 리스트 평탄화 순서가 곧 표시 순서 (VerticalLayoutGroup top→bottom).
- 예시 (Tank 5장, Dps 3장) — Header 행은 좌측에 축 아이콘 동반:
  1. `[TANK 아이콘] TANK (5장)` (Header)
  2. `Tier1  Wisp·Wraith HP ×1.3` (Effect, 아이콘 없음)
  3. `Tier2  Wisp·Wraith Power ×1.2` (Effect, 아이콘 없음)
  4. `[DPS 아이콘] DPS (3장)` (Header)
  5. `Tier1  Reaper·Hex Power ×1.3` (Effect, 아이콘 없음)
- 검산: Tank 5장 = 헤더1 + 효과2 = 3행, Dps 3장 = 헤더1 + 효과1 = 2행, 총 5행 (plan 테스트 `Tank5_Dps3_헤더와_효과행_수_검증` 과 일치).

---

## 4. 모달 레이아웃 (BuildModalPopup 대비 단순화)

`BuildModalPopup` 의 좌(패시브):우(액티브) 50:50 2분할을 제거하고 **단일 세로 리스트 1열** 로 단순화한다.

```
SynergyModalPopup (전체 화면 루트)
├─ DimButton (CHButton, 전체 화면 #000 α0.6)   — 클릭 시 닫힘
├─ 중앙 패널 (세로 카드형)
│  ├─ 타이틀 (CHText, 정적 라벨) "적용 중인 시너지"
│  ├─ CloseButton (CHButton, 우상단 X)
│  ├─ EmptyText (CHText)  — 활성 티어 0개일 때만 활성
│  └─ ScrollView (단일 세로)
│     ├─ Viewport → Content(VerticalLayoutGroup) → origin Cell
│     └─ VerticalScrollbar (UnityEngine.UI.Scrollbar, 우측 세로)  — 델타 B
└─ (BuildModalPopup 의 우측 섹션 없음)
```

- **타이틀**: 정적 라벨 `적용 중인 시너지`. Rule 03 §3 — 코드 참조 없는 정적 라벨도 `CHText` 동반.
- **중앙 패널 폭**: 단일 열이므로 `BuildModalPopup` 중앙 패널(2분할 합산 폭)보다 좁게 잡는다. 화면 가로의 약 절반(50% 기준). 행 1종 폭 = 패널 내부 폭 그대로 채움(VerticalLayoutGroup stretch).
- **행 높이**: 고정 44px (plan Task6 Step1 기준). 헤더·효과 동일 높이 — 셀 1종 재사용을 위해 통일. 헤더 행 아이콘(28×28px)은 44px 행 높이 안에 세로 중앙 정렬(상하 여백 각 8px = (44−28)/2).
- **셀 내부 구조 (1종, RowKind 분기)**: 좌측 `_axisStrip`(색 띠) → `_icon`(Image, 헤더만 활성) → `_label`(CHText, 가로 stretch). 아이콘 Image 는 prefab 에 정적 배치하고 `RowKind == Effect` 일 때 비활성화 (코드 동적 생성 금지 — Rule 03 §3).
- **효과 행 텍스트 들여쓰기 일관**: 효과 행에서 아이콘이 비활성이어도 `_label` 시작 위치가 헤더와 좌우로 흔들리지 않도록, 아이콘 영역(28px 아이콘 + 8px 간격 = 36px)을 고정 폭 `LayoutElement` 로 예약한다. 효과 행은 이 36px 자리를 빈 칸으로 두고 그 우측부터 `Tier{n} …` 텍스트를 시작한다 (헤더 라벨 시작 X좌표 = 효과 라벨 시작 X좌표). 검산: 셀 좌측 패딩 = 색띠폭 + 8(여백) + 36(아이콘 예약) → 헤더·효과 라벨 동일 X 정렬.
- **스크롤**: 행 수가 뷰포트 높이를 넘으면 세로 스크롤. 최대 행 수 = 4축 × (헤더1 + 효과3) = **16행** (4축 동시 7+ 도달, §6.3). 16행 × 44px = 704px 가 콘텐츠 최대 높이. 뷰포트(약 392px)는 풀링상 동시 활성 약 11셀까지만 표시(검산: 392 ÷ 44 + 2 ≈ 11) → 초과분은 content 가 scrollable 하며 스크롤 시 풀 재활용으로 노출. **세로 스크롤바**로 "아래에 더 있음" 을 알린다 (§4.1).
- **닫기 동작**: DimButton 또는 CloseButton 클릭 → `Close(reuse: true)` (BuildModalPopup 동일, cached UI 재사용).

## 4.1 세로 스크롤바 (델타 B — 가시성 affordance)

> 근본 원인(디버깅 확정): 모달은 `CHPoolingScrollView` 기반이라 viewport(약 392px)에 맞춰 동시 활성 셀이 약 11개로 캐싱된다(`poolItemCount ≈ 392 ÷ 44 + 2 = 11`). viewport 는 정상 정착하며, 시너지 행이 가시 한도(약 8~11행)를 넘으면 아래 행은 스크롤해야 보인다 — 풀링의 정상 동작이다. 저스택(3행)은 전부 보여 문제없다. 사용자 결정: **풀링·행 높이·모달 크기 유지 + 스크롤바 추가** (전부-한화면으로 바꾸지 않는다).

- **추가 컴포넌트**: `ScrollView` 자식에 `UnityEngine.UI.Scrollbar` 1개를 **세로(Direction = Bottom To Top)** 로 배치하고 `ScrollRect.verticalScrollbar` 슬롯에 연결한다.
- **위치**: 모달 중앙 패널 **우측 세로**. 스크롤바 트랙은 Viewport 우측 가장자리에 anchor (상하 stretch). 트랙 폭 8px, 우측 마진 2px → Viewport 우측 폭에서 10px 를 스크롤바 영역으로 예약. 검산: 스크롤바 영역 10px = 트랙 8 + 우측 마진 2.
- **MVP 비주얼 (신규 아트 0, §8 준수)**: 단색 thumb 만. 신규 스프라이트 제작 금지 — Unity 기본 UI sprite(`UISprite`) 또는 단색 `Image` 사용.
  - **트랙(Background) 색**: 회색 `#3C3C3C` α0.5 (어두운 패널 위에서 은은히).
  - **thumb(Handle) 색**: 밝은 회색 `#C8C8C8` α0.9 (단색, 화이트/그레이 계열). 둥근 모서리·그라데이션 없음 (MVP 텍스트/단색 전용 정신).
  - thumb 최소 길이: `ScrollRect`/`Scrollbar` 기본값 사용 (별도 minSize 지정 없음).
- **가시성 동작 — `ScrollRect.verticalScrollbarVisibility = AutoHideAndExpandViewport` (락)**:
  - **결정**: 내용이 뷰포트보다 짧으면 스크롤바를 자동 숨기고 뷰포트를 스크롤바 폭만큼 확장, 내용이 길면 스크롤바를 표시하며 뷰포트를 그만큼 좁힌다.
  - **근거 (대안 비교)**:
    - **대안 A — AutoHideAndExpandViewport (채택)**: 현실 표시는 보통 1~2축 활성(§7 비고)이라 대부분 3~6행으로 뷰포트보다 짧다. 이 다수 케이스에서 불필요한 빈 스크롤바가 뜨지 않아 깔끔하고, 짧은 목록에서 스크롤바 폭만큼 뷰포트가 확장되어 행 텍스트 영역이 넓어진다. 스크롤이 실제로 필요한 8행 이상에서만 바가 등장 → "스크롤 가능" 신호가 의미 있게 켜진다.
    - **대안 B — Permanent (항상 표시)**: 행 수와 무관하게 바가 항상 보여 "스크롤 영역" 임을 늘 알리지만, 다수 케이스(짧은 목록)에서 비활성(thumb 가 트랙 전체를 채운) 바가 상시 노출되어 시각 노이즈가 되고 뷰포트 폭도 항상 10px 손실. MVP 단순함·다수 케이스 우선 원칙에 어긋나 거부.
    - **대안 C — AutoHide(뷰포트 비확장)**: Unity `ScrollRect`에는 단독 AutoHide(확장 없음) 옵션이 없음(`Permanent` / `AutoHide` / `AutoHideAndExpandViewport` 3종 중 `AutoHide` 는 표시/숨김만, 확장 안 함). 짧은 목록에서 뷰포트 확장 이득이 없어 A 대비 장점 없음. 거부.
  - **결론**: `AutoHideAndExpandViewport`. 짧은 목록(대부분)에서는 바 자동 숨김, 길어지면(8행+) 바 등장.
- **스크롤 동작 확인 요구 (구현 단계 검증)**: `CHPoolingScrollView` 가 스크롤 시 `UpdateContent` 경로로 하위(뷰포트 밖) 행을 풀 재활용으로 정상 노출하는지 구현 단계에서 반드시 확인한다 — 스크롤바를 드래그하거나 휠로 내렸을 때 12·16행 케이스에서 마지막 행(예: Swarm Tier3)까지 텍스트·색 띠·아이콘이 깨짐 없이 표시되어야 한다. (스크롤바만 달고 풀 재활용이 안 되면 빈 셀/잔여 텍스트 노출 → 회귀.)
- **Rule 03 §3 적용 제외**: `Scrollbar` 는 CHText/CHButton/CHToggle 래퍼 대상이 아니다 (Rule 03 §3 표의 래퍼 4종에 미포함, `Slider` 와 동류의 예외). 따라서 **일반 `UnityEngine.UI.Scrollbar` 를 그대로 사용**한다 (`CHScrollbar` 같은 래퍼 도입 불요). 단, 스크롤바 내부에 라벨 텍스트는 없으므로 CHText 동반 대상도 없음.

> BuildModalPopup 과 달리 패시브/액티브 구분이 없으므로 EmptyText 도 1개만 둔다 (BuildModalPopup 은 2개).

---

## 5. 빈 상태 (활성 티어 0개)

- **문구**: `아직 발동한 시너지가 없습니다` (spec §3 / plan Task6 와 일치).
- **표시 조건**: `BuildRows` 결과 행 0개 (모든 축 < 3장). 이때 EmptyText 활성 + ScrollView 리스트 비움.
- **배치**: 중앙 패널 안, ScrollView 영역 중앙 정렬. 활성 티어가 1개라도 생기면 EmptyText 비활성 + 리스트 표시.

---

## 6. UX — 열기 / 닫기 / 갱신

### 6.1 열기

- 트리거: `BuildSynergyPanel` 루트의 `CHButton _rootButton` 클릭 (`BuildPanel._rootButton` 동일 관례).
- 동작: `CHMUI.Instance.ShowUI(EUI.SynergyModalPopup, new SynergyModalPopupArg { ViewModel = _vm })`.
- `_vm == null` 이면 무시 (가드).
- 게임은 자동전투 진행 중 — 모달은 dim 으로 화면을 덮되 **게임 일시정지는 하지 않는다** (BuildModalPopup 과 동일. 픽 팝업이 아니라 조회용이므로 시간/HP 트리거 진행 유지).

### 6.2 팝업 열린 중 픽 발생 시 갱신

- 모달이 열린 동안 `vm.OnBuildChanged` 구독 → 카드 픽으로 축 카운트가 바뀌면 `Build` 재호출로 행 리스트 자동 갱신 (BuildModalPopup 동일 패턴).
- 갱신 시: 새 임계를 넘은 축은 효과 행이 추가되고, 신규 활성 축은 헤더+효과가 새로 등장한다. **별도 애니메이션/펄스 없음** (BuildSynergyPanel 의 JustCrossed 펄스는 패널 전용 — 모달은 정적 갱신, MVP 단순화).
- 모달이 열린 상태에서 픽 팝업이 동시에 뜨는 케이스: MVP 에서 픽 팝업과 본 모달은 사용자가 직접 닫고 열어야 하므로 동시 표출은 사용자 조작에 의해서만 발생. 둘 다 떠 있어도 `OnBuildChanged` 갱신은 양쪽 독립 동작 (회귀 위험 없음, 읽기 전용).

### 6.3 닫기 / 수명

- DimButton·CloseButton → `Close(reuse: true)`.
- `closeDisposable` 로 `OnBuildChanged` 구독 해제 + `_vm = null` (cached UI 재사용 대비, BuildModalPopup `closeDisposable` 패턴 그대로).
- prefab active/inactive 저장 양쪽 케이스: `InitUI` 끝 `isActiveAndEnabled` 분기 + `OnEnable` 에서 `ForceRebuildLayoutImmediate` 후 `Build` (BuildModalPopup `BuildAndLayout` 패턴 그대로).

---

## 7. 엣지 케이스

| 케이스 | 동작 |
|---|---|
| **활성 티어 0개** (모든 축 < 3장) | 행 0개, EmptyText `아직 발동한 시너지가 없습니다` 활성, 스크롤 리스트 비움 (§5). |
| **한 축만 활성** (예: Tank 3장, 나머지 0~2장) | Tank 헤더1 + 효과1 = 2행만. 나머지 축 미표시. |
| **7+ 도달** (count >= 7, 예: Tank 9장) | 활성 티어 3 (Tier1·2·3 모두). 헤더 라벨은 원 카운트 `TANK (9장)` 표시(임계 7 고정 아님). 효과 행은 Tier1·2·3 = 3행. count 가 8·9 여도 Tier4 없음(임계 3개 상한) → 효과 행 최대 3. |
| **4축 동시 활성** (4축 모두 7+) | 4축 × (헤더1 + 효과3) = 16행. 정렬 Tank→Dps→Debuff→Swarm, 축 내 Tier1→2→3. 콘텐츠(704px)가 뷰포트(약 392px) 초과 → 동시 활성 셀은 풀링상 약 11셀, 초과분은 **세로 스크롤바**로 노출(§4.1). 스크롤 시 풀 재활용으로 마지막 행(Swarm Tier3)까지 표시. (실전에서 한 라운드 9픽으로 4축 동시 7+ 도달은 불가능하나, 표시 로직은 산술적으로 방어한다.) |
| **뷰포트 초과 행 (8행 이상)** | 콘텐츠가 뷰포트보다 길어 `AutoHideAndExpandViewport` 가 스크롤바를 표시(§4.1). 뷰포트 내 행은 전부 표시, 초과분은 content 가 scrollable + 스크롤바 존재. 풀링상 동시 활성 셀이 16개 미만일 수 있음(정상). |
| **뷰포트 이내 행 (약 7행 이하)** | 콘텐츠가 뷰포트보다 짧아 스크롤바 자동 숨김 + 뷰포트 확장(§4.1). 모든 행 동시 활성·전부 표시, 스크롤 불필요. |
| **카운트 경계** (정확히 3·5·7) | `>=` 기준이므로 3 → Tier1 활성, 5 → Tier1·2, 7 → Tier1·2·3. (경계 미만 4 → Tier1 만, 6 → Tier1·2.) |
| **TierDesc 키 누락** | 12개 키 전부 정의 필수. 누락 키 접근 시 `TryGetValue` 로 빈 문자열 방어 → 효과 행은 `Tier{n}  ` (효과 문구 공백). 정상 빌드에서는 12키 전부 채워져 발생하지 않음 (plan 테스트 `TierDesc_12개_키_전부_채워짐` 으로 회귀 방지). |

> 실전 도달 가능 상한: 한 라운드 패시브 픽 수에 의해 4축 동시 고티어는 제약된다. card-renewal §4.3 은 "9픽 중 7픽을 한 축에 몰아야 Tier3 도달 → 한 라운드에 한 축만 가능" 으로 명시. 따라서 **현실 표시는 보통 1~2축 활성**이며, 16행은 산술 상한(방어 목적)이다.

---

## 8. 구현 요청사항 (gameplay-programmer 용)

> 코드 시그니처·파일 구조는 plan 이 단일 진실. 본 섹션은 도메인 결정(문구·색·포맷·레이아웃)을 명세한다.

### Enum
- `Assets/_Lair/Scripts/Data/CommonEnum.cs` — `EUI` 에 `SynergyModalPopup` 추가 (prefab 파일명과 정확히 일치, Rule 03 §2). `BuildModalPopup` 아래 줄.

### Interface
- 신규 인터페이스 없음. `BattleViewModel.GetBuildCount(EBuildAxis)` (기존, `int` 반환) 와 `OnBuildChanged` (기존 event) 를 그대로 사용.

### 인스펙터 직접 Sprite 참조 (2026-06-03 델타 — Addressables 키 아님)
- `SynergyModalPopup` 에 `[SerializeField] Sprite` 4개 추가: `_tankIcon` · `_dpsIcon` · `_debuffIcon` · `_swarmIcon` (필드명은 `BuildSynergyPanel` 관례 글자 그대로). 인스펙터에서 `Assets/_Lair/Art/Sprites/SynergyIcons/{TANK,DPS,DEBUFF,SWARM}.png` 4장을 각각 연결.
- `EBuildAxis → Sprite` 매핑은 `BuildSynergyPanel.AxisIcon` 과 동일한 분기 메서드(`AxisIcon(EBuildAxis) → Sprite`) 를 `SynergyModalPopup` 에 둔다. **Rule 03 §2(Enum값명=파일명) 적용 안 됨** — 카드 아이콘 `CardData._icon` 과 동일 예외, `ESynergyIcon` 같은 enum 두지 않음 (card-renewal §8.0 결정 정합).
  > 같은 `SynergyIcons` 4장을 `BuildSynergyPanel` 과 `SynergyModalPopup` 두 prefab 이 각각 인스펙터로 참조한다(공유 SO 미도입). MVP 범위에서 참조 중복은 허용 — 별도 아이콘 provider SO 추출은 §9 비범위(YAGNI).

### 에셋 키 (Addressables — Rule 03 §2)
- `Assets/_Lair/Art/UI/SynergyModalPopup.prefab` — Addressables 주소 = `SynergyModalPopup` (= `EUI.SynergyModalPopup` 값명, 대소문자 일치), 라벨 `Resource` (기존 UI 동일). **델타: 위 4개 Sprite 인스펙터 슬롯 채움 필요.**
- `Assets/_Lair/Art/UI/SynergyModalCell.prefab` — Popup 내부 정적 참조(origin). Addressables 등록은 `BuildModalCardCell` 관례를 따른다 (Popup 내부 참조이므로 별도 주소 불요). **델타: 셀 prefab 에 아이콘 `Image` 자식 1개 정적 추가 + `SynergyModalCell._icon` 인스펙터 참조 연결.**
  - **delta prefab 체크리스트 (델타 A)**:
    - [ ] `SynergyModalCell.prefab` (별도 셀 파일) 에 아이콘 `Image` 자식 추가 + `_icon` 인스펙터 참조 연결.
    - [ ] **`SynergyModalPopup.prefab` 안에 정적 배치된 origin 셀 인스턴스에도 `_icon` Image 를 동기화하고 `_icon` 인스펙터 참조를 연결한다** (Rule 03 §3: origin 셀 컴포넌트/참조 누락 시 reference null → 시각 깨짐). 셀 prefab 만 고치고 Popup 내 origin 인스턴스를 빠뜨리지 않는다.

### 스크롤바 prefab 작업 (델타 B — Addressables/Enum 무관, 코드 참조 없음)
- `SynergyModalPopup.prefab` 의 `ScrollView` 자식에 **`UnityEngine.UI.Scrollbar`** GameObject 1개 추가 (일반 UGUI Scrollbar — `CHScrollbar` 래퍼 없음, Rule 03 §3 예외).
  - **delta prefab 체크리스트 (델타 B)**:
    - [ ] `ScrollView` 자식에 Scrollbar(트랙 8px + Handle) 추가, `Direction = Bottom To Top`, 우측 세로 stretch anchor(우측 마진 2px). 검산: 폭 10px = 트랙 8 + 마진 2.
    - [ ] `ScrollRect.verticalScrollbar` 슬롯에 위 Scrollbar 연결.
    - [ ] `ScrollRect.verticalScrollbarVisibility = AutoHideAndExpandViewport` (§4.1 락).
    - [ ] 트랙 색 `#3C3C3C` α0.5 / Handle 색 `#C8C8C8` α0.9 (단색, 신규 스프라이트 0 — Unity 기본 UISprite 또는 단색 Image).
    - [ ] Scrollbar 내부에 텍스트 라벨 없음 → CHText 동반 대상 아님.
  - **구현 검증 요구**: 12·16행 케이스에서 스크롤(휠/드래그) 시 `CHPoolingScrollView.UpdateContent` 가 하위 행을 풀 재활용으로 정상 노출하는지 확인 (마지막 행까지 텍스트·색 띠·아이콘 정상, 빈 셀/잔여 텍스트 없음). §4.1.

### SO 스키마 / 데이터 타입
- **신규 SO 없음** (결정 락 §2: 설명 데이터는 코드 정적 테이블). 효과 문구는 §2 표 12건을 `TierDesc` (`static readonly Dictionary<(EBuildAxis,int), string>`) 에 그대로 채운다.
- 셀 데이터 타입 `SynergyModalCellData` (델타 — 필드 1개 추가):
  - `RowKind`(Header/Effect) · `AxisColor`(Color) · `Label`(string) — 기존.
  - **`Sprite Icon` 추가** — 헤더 행만 사용(축 아이콘), 효과 행은 항상 `null`.
- 셀 `SynergyModalCell` (델타 — 아이콘 Image 참조 + 분기):
  - `[SerializeField] Image _icon` 추가 (좌측 색 띠 우측).
  - `Bind(data)`: `_icon.sprite = data.Icon`; `data.Icon != null && RowKind == Header` 일 때만 `_icon.gameObject.SetActive(true)`, 그 외 `SetActive(false)`.
  - `OnEnable` 풀 재사용 리셋: `_icon` 비활성·sprite null 로 초기화 (잔여 아이콘 누수 방지).
- **`BuildRows` 시그니처 변경 (델타 — `iconOf` 는 optional 파라미터)**:
  - 기존: `public static List<SynergyModalCellData> BuildRows(Func<EBuildAxis,int> countOf)`
  - 변경: `public static List<SynergyModalCellData> BuildRows(Func<EBuildAxis,int> countOf, Func<EBuildAxis,Sprite> iconOf = null)`
  - **`iconOf` 는 반드시 기본값 `= null` 을 가진 optional 파라미터로 선언한다** (필수 2-arg 로 만들지 않는다). 이유: 기존 EditMode 호출부(`SynergyModalPopupBuildTests.cs` + `SynergyModalPopupEdgeCasesTests.cs`, 약 26곳)가 전부 1-arg 호출(`BuildRows(Counts(...))`, `BuildRows(null)` 등)이라, 필수 2-arg 로 바꾸면 CS7036(인자 누락)으로 EditMode 어셈블리 전체가 컴파일 불가가 된다. optional 로 두면 기존 1-arg 호출부 26곳이 **무수정·무회귀**로 컴파일·통과한다.
  - `iconOf` 는 헤더 행 생성 시 `Icon = iconOf?.Invoke(axis)` 로 채운다 (`iconOf` 가 null 이면 헤더 `Icon = null` → 셀이 아이콘 숨김). 효과 행은 `Icon` 미설정(기본 null) 유지.
  - 호출부 `SynergyModalPopup.Build`: `BuildRows(_vm.GetBuildCount, AxisIcon)` (2-arg 주입).
  - **EditMode 순수성 보존**: `iconOf` 기본값 null 덕에 기존 BuildRows 테스트(행 수/RowKind/Label 검증) 26곳은 1-arg 호출 그대로 두어도 헤더 `Icon = null` 로 안전 동작·통과한다. 아이콘 채움 검증은 `iconOf` 주입 케이스로 별도 확인(test-engineer 영역).

### 표시 문구 / 색 / 포맷 (도메인 결정)
- Tier 효과 문구 12건: §2 표 (정본).
- 헤더 레이아웃: `[축 아이콘 28×28] {AxisLabel} ({count}장)` — §3.1. 축라벨 대문자 유지 (§3.4 락).
- 효과 포맷: `Tier{n}  {효과문구}` (공백 2칸), 아이콘 없음 — §3.2.
- 축 색/라벨/아이콘: `BuildSynergyPanel.AxisColor` / `AxisLabel` 재사용 + `AxisIcon` 분기 동형 (중복 정의 금지 정신, Rule 02 §5 — 색·라벨은 static 공유, 아이콘 Sprite 는 prefab별 인스펙터 참조).
- 타이틀 정적 라벨: `적용 중인 시너지`. 빈 상태 라벨: `아직 발동한 시너지가 없습니다`.
- 행 높이 44px, 단일 세로 1열, 패널 폭 화면 가로 약 50%. 우측 세로 스크롤바 10px(§4.1).

### 재현 테스트 정합 (델타 B — 기대치 정정 필요)
- `Assets/_Lair/Tests/PlayMode/UI/SynergyModalPopupPoolingRepro.cs` 의 "16행 전부 활성" 단언(`모달_첫열림_4축최대_활성셀수가_16행과_일치한다`, `모달_재오픈_4축최대_활성셀수가_여전히_16행과_일치한다`)은 **본 결정(스크롤 유지 + 풀링 유지)과 어긋난다**. 풀링상 동시 활성 셀은 viewport 기준 약 11개가 정상이므로, "활성 셀 == 16" 단언은 거짓 회귀를 낸다.
- **올바른 기대치(test-engineer 가 정정)**: ① 뷰포트 내 행은 전부 활성·표시, ② 콘텐츠가 뷰포트를 초과하면 `ScrollRect.content` 가 scrollable(content 높이 > viewport 높이)이고 세로 Scrollbar 가 연결·존재(`ScrollRect.verticalScrollbar != null`), ③ 스크롤(정규화 위치 0→1)을 끝까지 내린 뒤 마지막 행(Swarm Tier3) 셀이 활성·표시. "활성 셀 수 == BuildRows.Count(16)" 직접 단언은 제거 또는 "활성 셀 <= 16 && 스크롤 끝에서 마지막 행 노출" 로 치환.
- 저스택 케이스(`모달_첫열림_1축Tier2_활성셀수가_3행과_일치한다`, 3행)는 뷰포트 이내이므로 **3행 전부 활성 단언 유지** (정정 불요). 이 결정과 정합.

---

## 9. 비범위 (YAGNI)

- 미달 티어 / 로드맵(다음 임계까지 몇 장) 표시 — 안 함 (활성 티어만). 미달 진행도는 `BuildSynergyPanel` 상시 표시가 담당.
- 시너지 효과 수치·발동 로직 변경 — 안 함 (표시 전용).
- 헤더/효과 펄스·애니메이션 — 안 함 (MVP 정적 갱신).
- 사운드 / 신규 아트 — MVP §8 금지. 아이콘은 기존 `SynergyIcons` 4 png 재사용만 (신규 제작 0).
- SO 기반 설명 데이터 — 안 함 (코드 정적 테이블).
- 게임 일시정지 — 안 함 (조회용, 자동전투 진행 유지).
- **효과 행 아이콘** — 안 함 (헤더 행만 아이콘, 효과 행은 색 띠+텍스트).
- **아이콘 색 틴트 / 활성 티어 수만큼 반복** — 안 함. 모달 헤더 아이콘은 축 식별용 1개 고정·원본 색. (좌측 `BuildSynergyPanel` 의 "활성 티어 수만큼 반복" 정책(card-renewal §8.0)은 패널 전용 — 모달 헤더에는 적용하지 않는다.)
- **아이콘 provider 공유 SO 추출** — 안 함. `BuildSynergyPanel` 과 `SynergyModalPopup` 이 같은 4 png 를 각자 인스펙터로 참조(참조 중복 허용, MVP).
- **시너지명 Title-case 통일** — 안 함 (§3.4: 대문자 `AxisLabel` 유지). 좌·우 패널 표기 전체 변경은 별도 작업으로 분리.
- **전부-한화면(풀링 제거/모달 확대/행 높이 축소)** — 안 함 (델타 B). 풀링·행 높이 44px·모달 폭 무변경, 스크롤바 affordance 로만 해결.
- **`CHScrollbar` 래퍼 도입 / 커스텀 스크롤바 스프라이트** — 안 함. 일반 `UnityEngine.UI.Scrollbar` + 단색 (Rule 03 §3 예외, MVP §8 신규 아트 0).
- **스크롤 펄스/도달 알림/스크롤 위치 기억** — 안 함 (MVP 단순화).

---

## 10. 2026-06-03 후속 델타 A — 헤더 아이콘 변경점 단일 요약

> 사용자 화면 확인 후 요청한 "헤더 행 아이콘 추가" 델타(A)의 변경점만 모은다. 본 절이 델타 A 범위의 단일 진실. 수치·밸런스·시너지 발동 로직 무변경 (표시 UI 한정).

| # | 영역 | 변경 전 | 변경 후 |
|---|---|---|---|
| 1 | 헤더 행 레이아웃 (§3.1) | `[색 띠] {축라벨} ({N}장)` | `[색 띠] [축 아이콘 28×28] {축라벨} ({N}장)` (아이콘과 라벨 간격 8px) |
| 2 | 시너지명 표기 (§3.4) | 대문자 `AxisLabel` | **변경 없음** — 대문자 `TANK`/`DPS`/`DEBUFF`/`SWARM` 유지 (Title-case 거부, 사유 §3.4). 카운트 `(N장)` 유지. |
| 3 | 효과 행 (§3.2) | 색 띠 + 텍스트 | **변경 없음** — 아이콘 없음 명시 (Icon=null → Image 비활성) |
| 4 | 데이터 타입 (§8) | `SynergyModalCellData{RowKind, AxisColor, Label}` | `Sprite Icon` 필드 1개 추가 (헤더만 사용, 효과 null) |
| 5 | `BuildRows` 시그니처 (§8) | `BuildRows(Func<EBuildAxis,int> countOf)` | `BuildRows(Func<EBuildAxis,int> countOf, Func<EBuildAxis,Sprite> iconOf = null)` — `iconOf` 는 **optional**(기본 null). 헤더 `Icon = iconOf?.Invoke(axis)`. 기존 EditMode 1-arg 호출부 26곳 무수정·무회귀로 통과. |
| 6 | 셀 컴포넌트 (§8) | `_axisStrip` + `_label` | `Image _icon` 추가, RowKind=Header & Icon!=null 일 때만 활성 |
| 7 | 인스펙터 참조 (§8) | 없음 | `_tankIcon`/`_dpsIcon`/`_debuffIcon`/`_swarmIcon` 4 Sprite + `AxisIcon` 분기 (BuildSynergyPanel 관례 동형) |
| 8 | prefab (§8) | — | Popup 에 4 Sprite 슬롯 채움 / Cell prefab 에 아이콘 Image 자식 정적 추가 |

- **불변 (회귀 없음)**: 빈 상태(§5)·정렬(§3.3 행 순서)·열기/닫기/갱신(§6)·엣지(§7)·표시 문구 12건(§2). 효과 문구·임계·색·축 순서 전부 그대로.
- **plan↔기획서 sync**: 본 델타는 plan `2026-06-03-synergy-modal-popup.md` 에도 delta 마일스톤(BuildRows 시그니처·SynergyModalCellData.Icon·셀 아이콘 Image)으로 보강 필요 (`.claude/project.md` plan↔기획서 sync 규칙).

---

## 11. 2026-06-03 후속 델타 B — 스크롤바 변경점 단일 요약

> 풀링 부분표시 근본원인 디버깅 확정 후 사용자 결정("스크롤 유지 + 스크롤바") 의 변경점만 모은다. 본 절이 델타 B 범위의 단일 진실. 풀링 동작·행 높이·모달 폭·수치·밸런스·시너지 발동 로직 무변경 (가시성 affordance UI 한정).

| # | 영역 | 변경 전 | 변경 후 |
|---|---|---|---|
| 1 | 근본원인 (§4.1) | 뷰포트 초과 행이 잘려 보임 (버그로 의심) | 풀링상 동시 활성 약 11셀(`392÷44+2`)·viewport 정상 정착 = **풀링 정상 동작**으로 확정. 전부-한화면 시도 안 함 |
| 2 | 스크롤바 (§4·§4.1) | 없음 (스크롤만 가능, affordance 없음) | `ScrollView` 자식에 세로 `UnityEngine.UI.Scrollbar` 추가 (우측 세로, 트랙 8px+마진 2px=10px), `ScrollRect.verticalScrollbar` 연결 |
| 3 | 가시성 동작 (§4.1) | — | `verticalScrollbarVisibility = AutoHideAndExpandViewport` (락, 대안 B/C 거부) — 짧은 목록 자동 숨김, 8행+ 등장 |
| 4 | 비주얼 (§4.1) | — | 단색 thumb: 트랙 `#3C3C3C` α0.5 / Handle `#C8C8C8` α0.9. 신규 스프라이트 0 (Unity 기본/단색 Image), MVP §8 준수 |
| 5 | Rule 03 §3 (§4.1) | — | Scrollbar 는 래퍼 4종 미포함(Slider류 예외) → 일반 `UnityEngine.UI.Scrollbar` 사용, `CHScrollbar` 미도입 명시 |
| 6 | 엣지 (§7) | "16행 패널 초과 시 스크롤" | 뷰포트 초과(8행+) = 스크롤바 표시 / 뷰포트 이내(≤7행) = 스크롤바 자동 숨김+뷰포트 확장 행 추가 |
| 7 | 재현 테스트 (§8) | "활성 셀 == 16 전부" 단언 | 결정과 어긋남 → "뷰포트 내 전부 표시 / 초과분 scrollable + 스크롤바 존재 / 스크롤 끝 마지막 행 노출" 로 정정 (test-engineer) |
| 8 | 구현 검증 (§4.1·§8) | — | 12·16행 케이스에서 스크롤 시 `UpdateContent` 풀 재활용으로 마지막 행까지 정상 노출 확인 요구 |

- **불변 (회귀 없음)**: 풀링 동작 자체·행 높이 44px·모달 폭(화면 50%)·표시 문구 12건(§2)·정렬(§3.3)·헤더 아이콘(델타 A, §3.1)·빈 상태(§5)·열기/닫기/갱신(§6). 밸런스·시너지 발동 로직 전부 그대로.
- **Enum/Interface/SO 무변경**: 스크롤바는 코드 참조 없는 prefab 정적 컴포넌트 (`ScrollRect` 슬롯 연결만). 신규 Enum/Interface/SO 없음.
- **plan↔기획서 sync**: 본 델타도 plan `2026-06-03-synergy-modal-popup.md` 에 delta 마일스톤(스크롤바 prefab 작업·verticalScrollbarVisibility·재현 테스트 기대치 정정)으로 보강 필요.
