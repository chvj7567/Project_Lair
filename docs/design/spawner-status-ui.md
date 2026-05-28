# 스포너 상태 UI — 기능 기획서

> 작성: game-designer · 2026-05-27
> 대상 버전: MVP
> 입력 스펙: `docs/superpowers/specs/2026-05-27-spawner-status-ui-design.md` (결정 락)
> 현재 문서 버전: **v1.0** (§7 변경 이력)

---

## § 헤더

- **목표**: 월드 위에 흩어진 6 Spawner 의 진행/출력/강화 상태를 화면 좌측 2/3 폭의 6셀 패널 한 줄로 통합하고, 셀/패널 클릭 한 번으로 강화 효과와 빌드 전체를 즉시 복기할 수 있게 한다.
- **검증 가설**: 6 스포너 상태(진행/출력 종/적용 강화)를 한눈에 보이게 하면 플레이어가 카드 픽 결정 시 시너지를 더 빠르게 잡는다 (컨셉 §5.2 시너지 가시성).
- **현재 단계 범위 적합성**: MVP 범위 내 (컨셉 §11.4 프리미티브 비주얼 방침 — 글자·색상만 사용, 신규 아트 에셋 없음).
- **핵심 메커니즘**:
  1. World-space `SpawnerCooldownBar` 제거 → 화면 **좌측 하단 2/3 폭**의 `SpawnerStatusPanel` 의 6 셀(134×168) 로 통합 (셀당 색칩+종명+×N+진행 바, 상단 **강화 + 추가 생산 2 슬롯 아이콘 row** — v1.0 신규). **v0.6 (64×80) 대비 추가 2.09× 확대** — 사용자의 화면 가로 분할 정책 (좌측 2/3 = 스포너, 우측 1/3 = 빌드) 채택으로 좌측 영역을 가득 채우는 셀 크기 결정 (§7 v0.7 변경 이력).
  2. 셀 클릭 → 셀 위 floating 툴팁 (해당 스포너 출력 종에 적용된 강화 카드 상세). 툴팁 폭 = 셀 1개의 1.5배 (201px). **툴팁 본문(강화 줄 영역)은 `CHPoolingScrollView<BuffLine, AppliedBuff>` 파생 컴포넌트로 래핑** (§2.5.5 v0.8) — 사용자 명시 요청에 따라 미래 1↔다 매핑 확장 또는 PickCount 누적 표현 풍부화에 대비한 구조 보험.
  3. `BuildPanel` 은 종 강화 6장 제외(`ECardCategory.Enhance && IsPassive` 필터) + 패널 클릭 시 화면 중앙 `BuildModalPopup` (픽한 모든 카드 표시). **위치는 화면 우측 1/3 폭의 세로 컬럼** (폭 427px, §2.8 v0.7) — SpawnerStatusPanel 과 가로 분할로 시각 경쟁 회피. **PassiveSection / ActiveSection 각각이 `CHPoolingScrollView<BuildIconCell, BuildEntry>` 파생 컴포넌트** (§2.8.5 v0.8) — Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용으로 풀(패시브 9 + 액티브 10) 모두 픽 시 클리핑/풀링을 단일 컴포넌트가 자동 처리.

---

## 1. 개요

스펙(§1~§3)이 골격을 확정한다. 본 기획서는 게임 디자인 도메인 디테일을 채운다.

- 셀 정확 치수 / 화면 배치
- 종 아이콘 글자·색 매핑
- 셀 텍스트 포맷·폰트
- 셀 클릭 활성화 표시
- 셀 위 툴팁 문구 포맷 (스탯별 템플릿)
- `BuildModalPopup` 레이아웃 디테일
- BuildPanel 필터 조건
- **BuildPanel 화면 배치** (§2.8 v0.4 신규)
- **BuildPanel 섹션 CHPoolingScrollView** (§2.8.5 v0.8 — Rule 03 §3 완전 적용, v0.5 의 raw ScrollRect 정책 교체)
- **BuildModalPopup 섹션 CHPoolingScrollView** (§2.7.2 v0.8 — Rule 03 §3 완전 적용, v0.3 의 단순 ScrollRect 예외 철회)
- **SpawnerStatusTooltip 본문 CHPoolingScrollView** (§2.5.5 v0.8 — Rule 03 §3 완전 적용, v0.7 의 raw ScrollRect 정책 교체)
- **IconRow 2 슬롯 확장 — 좌 Enhance / 우 Spawn** (§2.3 v1.0 — 사용자 명시 요청, Spawn 5 카드 매핑 + 픽 추적 + BuffLine FormatBody Spawn 분기 + 빌더 IconRow 자식 2 슬롯)

스펙이 락해둔 결정 (HUD 배치 / 셀 구성 / 클릭 인터랙션 / BuildPanel 변경 / 디스크 본체 유지 등)은 변경하지 않는다.

---

## 2. 구성요소

### 2.1 SpawnerStatusPanel (6셀 컨테이너)

화면 **좌측 하단** 정렬, 좌→우 = ring 인덱스 0→5 (컨셉 §3.1, 스펙 §6.3 그대로 확정).

| 항목 | 값 | 비고 |
|---|---|---|
| 정렬 | 화면 **좌측 하단** (Anchor min/max `(0, 0)`, Pivot `(0, 0)`) | v0.6 의 하단 중앙 → v0.7 좌측 하단 |
| 화면 좌측 offset | **0px** (anchored X = 0) | 화면 좌측 가장자리에 정확히 붙음 |
| 화면 하단 offset | **0px** (anchored Y = 0) | 화면 하단 가장자리에 정확히 붙음 (v0.6 +24 → 0). 좌측 2/3 가로 분할로 우측 영역과 가로상 분리되므로 하단 safe area 여백 무용 — 화면 끝까지 사용 |
| 셀 간 간격 | 6px | `HorizontalLayoutGroup.spacing` (v0.3 유지) |
| 좌우 패딩 | 8px | `HorizontalLayoutGroup.padding (left/right)` (v0.3 유지) |
| 패널 가로 폭 | 6×134 + 5×6 + 2×8 = **850px** | 직접 계산. 화면 1280×720 의 좌측 2/3 = 853.33 → 패널 850 + 좌측 margin 3 (anchoredPosition.x = 0 이라 패널이 화면 좌측 끝부터 850 까지 차지, 우측 잔여 3px 는 자연 여백). v0.6 430px → **v0.7 850px** |
| 패널 세로 높이 | **168px** (셀 높이와 동일, 자식 컨트롤 X). v0.6 80px → **v0.7 168px** | |
| 정렬 순서 | ring 인덱스 0~5 고정 (출력 종이 변해도 인덱스 위치 유지) | 스펙 §6.3 그대로 |

> **확대 근거 (v0.7)**: 사용자가 화면 가로 분할 정책을 **"스포너 패널이 가로 기준 왼쪽에서 3분의 2, 빌드패널은 우측에서 3분의 1"** 로 결정. 좌측 2/3 = 약 853px 폭에 6셀이 들어가야 함 → 셀 폭 = (853 − 좌우 padding 16 − 셀 간 간격 30) ÷ 6 = 134.5 → 정수 단정 **134px**. 셀 비례 134/64 = 2.094× — 셀 높이는 64×80 의 1.25 비례 유지하여 134×167.5 → 정수 단정 **168px** (134/64 = 2.094× → 80×2.094 = 167.5 → 168). 패널 가로 폭은 셀 6개 + spacing 5×6 + 좌우 padding 2×8 = **850px**. 좌측 anchor 변경 사유: 좌측 2/3 가로 영역을 가득 사용하려면 화면 좌측 끝에 붙어야 함 (중앙 정렬 시 화면 좌우에 여백 발생 → 좌측 2/3 영역 활용도 손해).

### 2.2 SpawnerStatusCell (셀 1개)

```text
[N=1 — Enhance 만 픽, 가장 흔한 케이스]
┌─ 134 × 168 dp ───────────────────────────────────┐
│  ◉H                                              │ ← 아이콘 row (높이 42) — 좌 Enhance 슬롯만
├──────────────────────────────────────────────────┤
│  [■] Wisp                                        │ ← 색칩 + 종명 (본체 row 높이 58, 색칩 30 + gap 14 + 종명 70)
│  ▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░        │ ← 진행 바 (높이 17)
│                                                  │ ← 잔여 padding
└──────────────────────────────────────────────────┘

[N=1 — Enhance + Spawn 둘 다 픽, v1.0 신규]
┌─ 134 × 168 dp ───────────────────────────────────┐
│  ◉H       ◉+                                     │ ← 아이콘 row (높이 42) — 좌 Enhance (x=12) / 우 Spawn (x=68)
├──────────────────────────────────────────────────┤
│  [■] Wisp                                        │
│  ▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░        │
│                                                  │
└──────────────────────────────────────────────────┘

[N≥2 — Enhance + Spawn 둘 다 다회 픽, ×N 배지 동시 표시]
┌─ 134 × 168 dp ───────────────────────────────────┐
│  ◉H×2     ◉+×3                                   │ ← 두 슬롯 모두 PickCount≥2 배지 — slot 1 배지 우측 끝 64 ↔ slot 2 좌측 끝 68 (spacing 4)
├──────────────────────────────────────────────────┤
│  Wisp                                  ×3        │ ← 종명 + 본체 ×N (Spawner.OutputCount = 3, 색칩·gap 일시 숨김 → 종명 가용 폭 80px)
│  ▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░        │
│                                                  │
└──────────────────────────────────────────────────┘

[Spawn 만 픽, Enhance 미픽 — 좌 슬롯 비움]
┌─ 134 × 168 dp ───────────────────────────────────┐
│           ◉+                                     │ ← 우 Spawn 슬롯만 (x=68), 좌 Enhance 슬롯은 비활성
├──────────────────────────────────────────────────┤
│  [■] Wisp                                        │
│  ▓▓▓▓▓▓▓░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░░        │
│                                                  │
└──────────────────────────────────────────────────┘
```

> 도식의 `◉H` 는 "WispHpBoost 픽 (Enhance 좌 슬롯)", `◉+` 는 "SpawnWisps 픽 (Spawn 우 슬롯)" 예시. v1.0 부터 **IconRow 는 2 슬롯 구조** (좌 Enhance / 우 Spawn 고정 — §2.3.1 v1.0). 종 1 ↔ Enhance 카드 1 매핑 + 종 1 ↔ Spawn 카드 1 매핑 (Hex 제외 — §2.3.3 v1.0 매핑) 이므로 **각 슬롯의 distinct 아이콘은 항상 0 또는 1**. 두 슬롯 독립 토글 — 픽되지 않은 슬롯만 비활성. **셀 본체 row 의 `×N` 은 Spawner.OutputCount (현재 동시 출력 수), Spawn 슬롯의 `×N` 배지는 SpawnX 카드 PickCount — 두 값은 의미가 다름** (§2.3.5 v1.0). N≥2 케이스에서 색칩이 사라져도 셀 상단 강화 아이콘 row 의 배경색(§2.3.3) 과 월드 디스크 본체 틴트(§2.4) 가 종 색상을 표시 — 인지 손해 없음 (§2.2.3 v0.5 색칩 일시 숨김 결정 사유).

#### 2.2.1 셀 치수 (v0.7 — 추가 2.094× 확대)

| 항목 | 값 | v0.6 비교 |
|---|---|---|
| 셀 가로 | **134px** | 64 → 134 (×2.094) |
| 셀 세로 | **168px** | 80 → 168 (×2.1) |
| 셀 내부 padding (좌/우) | **12px** | 6 → 12 |
| 셀 내부 padding (상/하) | **8px** | 4 → 8 |
| 아이콘 row 영역 높이 | **42px** (상단) | 20 → 42 |
| 본체 영역 높이 | **58px** (색칩 + 종명 + ×N) | 28 → 58 |
| 진행 바 영역 높이 | **17px** | 8 → 17 |
| 영역 사이 간격 | **8px** | 4 → 8 |
| 배경 색 | `#1F2937` (셀 본체, alpha 0.85) — Phantom 색과 동일 톤이지만 alpha 로 구분 | (유지) |
| 셀 테두리 (기본) | 없음 (border Image alpha 0 으로 숨김 — 활성 시 노랑으로 fade-in) | (유지) |
| 셀 테두리 (클릭 활성) | **2px `#FBBF24` (노랑)** — 툴팁 표시 중인 셀 강조 | (유지) |

**세로 budget 검산 (v0.8 단일 진실)**: 상단 padding 8 + 아이콘 row 42 + 영역 간격 8 + 본체 row 58 + 영역 간격 8 + 진행 바 17 + 하단 padding 8 + **잔여 19** = **168px**. 본체 row 58px 안에 30px 색칩 + 22pt 종명 텍스트(N=1 케이스) 또는 22pt 종명 + 20pt ×N(N≥2 케이스, 색칩 숨김 §2.2.3) 가 세로 가운데 정렬로 충분히 들어감.

**진행 바 anchoredPosition.y 단일 결정 (v0.8 — design-reviewer BLOCKER B1 해소)**:
- 셀 RectTransform pivot 은 `(0, 0)` (좌측 하단). 자식 진행 바 RectTransform 도 pivot `(0, 0)` 로 통일.
- y 누적 (아래에서 위로): 하단 padding 8 + 진행 바 17 + 잔여 19 + … = 진행 바의 anchoredPosition.y = `하단 padding 8 + 잔여 19 = 27`. **진행 바 `anchoredPosition.y = 27f` 로 단일 단정**.
- 본체 row anchoredPosition.y = 하단 padding 8 + 진행 바 17 + 잔여 19 + 영역 간격 8 = **52** (본체 row pivot (0, 0)).
- 아이콘 row anchoredPosition.y = 본체 row 52 + 본체 row 높이 58 + 영역 간격 8 = **118**.
- 셀 sizeDelta y = 168, 아이콘 row top edge = 118 + 42 = 160, 상단 잔여 = 168 − 160 = 8 (상단 padding 8 일치). 자기 정합 OK.

> **폰트 cap 정책 (v0.7)**: 셀 비례 2.094× 를 폰트에 그대로 적용하면 종명 31pt 등 글자가 셀 면적 대비 과도하게 커진다. 사용자가 advisor 검토에서 cap 을 명시 허용 — **종명 22pt · ×N 20pt · 아이콘 글자 16pt · ×N 배지 14pt** 로 cap 적용. 셀 면적 확대분은 폰트 크기 비례 확대가 아니라 **여백 확대 (padding 12/8, 영역 간격 8, 잔여 padding 19) 와 색칩·아이콘 원의 면적 확대** 로 흡수.

#### 2.2.2 색칩 (출력 종 표시)

| 항목 | 값 |
|---|---|
| 형태 | 정사각형 작은 색 패치 (Image, sprite = white square) |
| 크기 | **30 × 30 px** (v0.6 14×14 → v0.7) |
| 위치 | 본체 영역 좌측 |
| 색상 | 출력 종 색 (§ 2.4 매핑 — 컨셉 §11.4 그대로) |
| 갱신 트리거 | `Spawner.OnOutputTypeChanged` 수신 시 즉시 (Replace 카드 효과) |
| **표시 토글 (v0.5)** | **N=1 일 때만 노출. N≥2 일 때 일시 숨김** — 종명 가용 폭을 76px 로 회복 (§2.2.3 폴백 정책 — v0.7 재산출). 종 색상 정보는 셀 상단 강화 아이콘 row 배경(§2.3.3) + 월드 디스크 본체 틴트(§2.4) 가 3중 redundancy 로 보전. |
| 토글 트리거 | `Spawner.OnOutputCountChanged` 수신 시 (N≥2 ↔ N=1 경계에서 visibility 갱신) |

#### 2.2.3 종명 텍스트 (CHText)

| 항목 | 값 |
|---|---|
| 표시 형식 | **영문 풀네임** (`Wisp` / `Wraith` / `Reaper` / `Hex` / `Plague` / `Phantom`) |
| 폰트 사이즈 | **22pt** (v0.6 15pt → v0.7, cap 적용 — §2.2.1 폰트 cap 정책) |
| 정렬 | 좌측 정렬 (색칩 우측에 **14px gap** — v0.6 6px → v0.7) |
| 색상 | `#FFFFFF` (흰색) |
| 폰트 | `NotoSansKR SDF` (현재 프로젝트 폰트) |
| 잘림 처리 | overflow `Truncate` |

**가용 폭 분석 (v0.7 134px 셀 기준)**:

| 케이스 | 가용 폭 계산 | 결과 |
|---|---|---|
| ×N 미노출 (N=1, 다수) | 셀 폭 134 − 좌우 padding 24 − 색칩 30 − gap 14 − 우측 잔여 패딩 영역 | **약 66px** |
| ×N 노출 (N≥2) — 색칩 숨김 | 셀 폭 134 − 좌우 padding 24 − ×N 영역 (~30px, 20pt `×N` 우측 정렬) | **약 80px** ← 풀네임 22pt 들어감 |

22pt `NotoSansKR SDF` 에서 `Phantom` (7자) 의 preferredWidth 추정 ≈ 70~78px (라틴 영문 평균 글자당 10~11px). N=1 케이스는 66px 가용으로 풀네임이 빠듯하지만 마지막 1자 잘림 가능성 존재 — 4글자 truncate 안전망(아래) 으로 cover. N≥2 케이스는 색칩 숨김으로 80px 가용 → 풀네임 안전.

**폴백 정책 (v0.7 — v0.5 정책 그대로 적용, 가용 폭만 재산출)**:

v0.5 정책: ×N 노출 케이스에서 본체 row 의 **색칩(30px) + gap(14px) = 44px 를 일시 숨김** → 종명 가용 폭이 좁아진 만큼 회복.

| 케이스 | 본체 row 레이아웃 | 종명 표시 | 가용 폭 (v0.7) |
|---|---|---|---|
| **×N 미노출 (N=1)** | `[색칩 30] [gap 14] [종명 66] [잔여]` | **풀네임** (안전망: 잘리면 4글자 truncate) | 66px |
| **×N 노출 (N≥2)** | `[종명 80] [×N ~30]` — 색칩·gap 숨김 | **풀네임** | 80px |

**색칩 일시 숨김의 정보 손실 검증 (없음 확인)** — v0.5 와 동일:
- ×N 노출 케이스에서 종 색상은 두 곳에서 추가 표시됨:
  - 셀 상단 **강화 아이콘 row** (§2.3.3) — 아이콘 배경색이 종 색 그대로 (Wisp 초록, Phantom 검정 등)
  - 월드 디스크 본체 (§2.4, 컨셉 §11.4) — `SpawnerBody` 색상 틴트
- 즉 색칩(§2.2.2) 은 종 색을 표시하는 **3중 redundancy 중 1개** — ×N 노출 시 일시 숨김으로 인지 손해 없음.
- 강화 아이콘 row 가 비어있는 (강화 미적용) 케이스에서도 ×N 이 동시에 노출될 가능성 자체는 있음 (`Replace + Spawn`만 픽됨). 이 경우 색상 redundancy 가 디스크 본체 1개로 줄지만, ×N 노출은 N≥2 만이라 **본체 row 좌측에 색칩 없이도 셀 자체가 한 종 출력 중인 슬롯임을 위치(ring 인덱스) + ×N 배지가 충분히 식별**.

**4글자 truncate 안전망 (구현 단계 검증용)**:
- 본 기획서가 단정하는 1차 표시는 **풀네임 풀-에어리어 사용** (N=1 66px / N≥2 80px).
- 실제 폰트 메트릭에서 `Phantom` (7자) 가 22pt `NotoSansKR SDF` 66px 에 안 들어가면 N=1 케이스 폴백 발동.
- 폴백 형식: 첫 4글자 truncate — `Wisp`/`Hex`/`Plague`(Plag) 등. 6 종의 4자 prefix (`Wisp` / `Wrai` / `Reap` / `Hex` / `Plag` / `Phan`) 가 모두 unique 라 종 식별성 손해 없음.
- gameplay-programmer 가 빌드 후 1회 측정 → 풀네임 66px 안 들어가면 truncate 활성. 측정은 코드의 `TMP_Text.preferredWidth` 로 자동 (`overflow=Truncate` 라 표시는 안전, 사용자 가시 truncation 만 사전 분기).

**결정 사유 (22pt 영문 풀네임 + cap)**: 셀 비례 2.094× 를 폰트에 그대로 적용 시 종명 31pt 가 되어 셀 면적 대비 글자가 비대해짐. cap 22pt 로 두면 종명 너비가 가용 폭 안 (또는 안전망 발동 가능 수준) 에 들어가면서 셀 시각 밸런스 유지. v0.5 의 영문 풀네임 + 4자 truncate 안전망 정책은 그대로.

#### 2.2.4 동시 출력 ×N (CHText)

| 항목 | 값 |
|---|---|
| 표시 형식 | `×N` (N=1 이면 **숨김**, N≥2 일 때만 표시) |
| 폰트 사이즈 | **20pt** (v0.6 14pt → v0.7, cap 적용) |
| 정렬 | 본체 영역 우측 정렬 |
| 색상 | `#FBBF24` (노랑 — 증가 강조) |
| 갱신 트리거 | `Spawner.OnOutputCountChanged` 수신 시 |

#### 2.2.5 진행 바

기존 `SpawnerCooldownBar` 의 색·threshold 규칙 그대로 이전 (스펙 §6.1 락).

| 항목 | 값 |
|---|---|
| 형태 | Image + `fillAmount` (Horizontal, Left) |
| 가로 | 셀 폭 - 좌우 padding = **110px** (v0.6 52px → v0.7. 134 − 12×2 = 110) |
| 세로 (Fill+Background) | **17px** (v0.6 8px → v0.7) |
| Fill 색상 (0~69%) | `#60A5FA` (하늘 파랑 — Cool) |
| Fill 색상 (70~100%) | `#F97316` (주황 — Warm) |
| Background 색상 | `#374151` (회색) |
| 초기 지연 국면 | `Progress = 0` 빈 바 (`SpawnerCooldownBar` 정책 그대로) |
| 갱신 | View 측 매 프레임 폴링 (`ISpawnerProgress.Progress`) — 스펙 §4.3 |

### 2.3 아이콘 Row (v1.0 — 2 슬롯: 좌 Enhance / 우 Spawn)

셀 상단에 **종에 적용된 카드 아이콘 최대 2개** (좌측 = 강화 슬롯, 우측 = 추가 생산 슬롯) + 각 슬롯 중첩 픽 배지를 표시.

**v1.0 변경 사유 (사용자 직접 인용)**: *"스포너 추가 생산도 x2 표시되는건 확인했는데 위에 버프 표시되는거처럼도 표시해줘"* — 셀 본체 row 의 `×N` 동시 출력 표시는 v0.x 부터 동작하고 있으나, **셀 상단 IconRow 에 강화 카드(H/D/S/R/M/P) 가 표시되듯 추가 생산 카드도 같은 패턴으로 노출** 요청. v1.0 은 IconRow 를 1 슬롯 → 2 슬롯으로 확장하여 좌 Enhance / 우 Spawn 을 독립 토글로 표시한다.

**전제 (컨셉 §11.3 종 1 ↔ 카드 1 매핑, v1.0 갱신)**:
- **Enhance 카드**: 한 종에 정확히 1장 — Wisp ↔ WispHpBoost / Wraith ↔ WraithDamageBoost / Reaper ↔ ReaperAtkSpeed / Hex ↔ HexRangeBoost / Plague ↔ PlagueSlowBoost / Phantom ↔ PhantomMoveSpeedBoost.
- **Spawn 카드**: 한 종에 정확히 1장, **단 Hex 만 Spawn 카드 없음** — Wisp ↔ SpawnWisps / Wraith ↔ SpawnWraith / Reaper ↔ SpawnReapers / Plague ↔ SpawnPlagues / Phantom ↔ SpawnPhantoms. Hex 는 자연스럽게 Spawn 슬롯이 영구 비활성 (`IconLetterFor` 반환 글자 ` ` → 슬롯 미표시, §2.3.3 v1.0).

따라서 셀이 한 종을 출력 중일 때 **각 슬롯의 distinct 아이콘 수는 항상 0 (해당 카드 1장도 안 픽) 또는 1 (해당 카드 픽됨)** 이다. 2 슬롯이 동시 활성될 수 있으나, 한 슬롯 안의 distinct 아이콘은 여전히 0 또는 1.

#### 2.3.1 한 개 아이콘 치수 (v0.7 — v1.0 동일, 2 슬롯 각각에 적용)

| 항목 | 값 | v0.6 비교 |
|---|---|---|
| 형태 | 원형 Image (sprite = white circle, alpha mask) + 글자 1자 (TMP_Text) | (유지) |
| 지름 | **30px** | 14 → 30 |
| 글자 폰트 사이즈 | **16pt** (cap 적용) | 11 → 16 |
| 글자 정렬 | 원 중앙 | (유지) |
| 글자 색 | Phantom 배경(`#1F2937`)일 때 `#FFFFFF`, 나머지(밝은 배경)는 `#000000` (가독성) | (유지) |
| 아이콘 ↔ ×N 배지 gap | 2px (배지가 원 우측 하단에 살짝 겹침) | 1 → 2 |
| **슬롯 1 (Enhance) anchoredPosition.x** | **12px** (셀 좌측 padding 과 일치, 기존 위치 유지) | (v1.0 — 기존 위치 보존 결정) |
| **슬롯 2 (Spawn) anchoredPosition.x** | **68px** (= 슬롯 1 배지 우측 끝 64 + 슬롯 간 spacing 4 — 배지 형상 검산 §2.3.1 v1.0) | (v1.0 신규) |
| **슬롯 간 spacing** | **4px** (슬롯 1 배지 우측 끝 ↔ 슬롯 2 아이콘 좌측 끝) | (v1.0 신규) |
| **배지 형상 검산 (v1.0 — 배지 우측 확장 고려)** | 배지 RectTransform 은 IconCircle 자식, `anchorMin/Max (1, 0)` (icon 우하단 모서리), `pivot (0, 1)` (배지 좌상단), `anchoredPosition (-2, 1)`, `sizeDelta (24, 14)` → **배지 우측 끝이 icon 우측 끝보다 +22px 확장** (24 − 2 = 22). 슬롯 1 icon 우측 끝 = 12 + 30 = 42, 슬롯 1 배지 우측 끝 = 42 + 22 = **64**. 슬롯 2 icon 좌측 끝 ≥ 64 + 4 = **68** 단정. 슬롯 2 icon 우측 끝 = 68 + 30 = 98, 슬롯 2 배지 우측 끝 = 98 + 22 = **120** → Row 가용 폭 (실제 stretch 폭 = 134) 안에 14px 여유. **두 슬롯 모두 PickCount≥2 인 케이스에도 배지 충돌 없음**. | (v1.0 신규 — advisor 검토에서 배지 충돌 발견 후 단정) |
| Row 영역 가용 폭 | **134px** (`anchorMin/Max (0, 0)~(1, 0)` 가로 stretch → Row 가로 폭 = 셀 폭). 셀 좌측 padding 12 를 슬롯 1 anchoredPosition.x = 12 로 흡수, 셀 우측 padding 12 는 슬롯 2 배지 우측 끝 120 + 14 = 134 까지 자연 흡수 | (v1.0 표현 정정 — v0.x 의 "110px 가용" 은 padding 차감 추정치였음. 실측 Row 폭은 stretch 134, 좌·우 padding 은 자식 anchoredPosition 으로 흡수) |
| 두 슬롯 모두 미픽 시 | row 영역 비움 (양 슬롯 모두 `SetActive(false)`), 빈 **42px** 공간 유지로 본체 영역 위치 고정 | 20 → 42 |
| 한 슬롯만 픽 시 | 픽된 슬롯만 `SetActive(true)`, 다른 슬롯은 `SetActive(false)` — 도식 [N=1 Enhance 만] / [Spawn 만] 케이스 | (v1.0 신규) |

> **슬롯 순서 결정 사유 (좌 Enhance / 우 Spawn)**: (1) v0.x 까지 `_iconCircle` (Enhance) 가 `anchoredPosition.x = 12` 의 좌측 슬롯에 자리 — 기존 셀의 가장 흔한 케이스 (Enhance 만 픽) 의 시각 위치 보존. Spawn 슬롯을 좌측에 두면 모든 기존 셀이 우로 시각 이동, 일반 케이스에서 시각 regression 발생. (2) 사용자 발화 *"위에 버프 표시되는거처럼도"* 의 *"도"* 는 추가 의미 — 우측에 추가하는 게 의미적으로 자연. (3) §2.3.4 갱신 트리거 표는 Enhance 경로 변경 없음, Spawn 경로만 신규.

> **배지 충돌 회피 결정 사유 (advisor 검토 후 추가, v1.0)**: 기존 `LairSpawnerStatusUIBuilder.BuildCellPrefab` (line 148~166) 의 배지 RectTransform 은 icon 의 우하단 모서리 (anchor (1, 0)) 에 pivot (0, 1) 로 위치하고 anchoredPosition (-2, 1) + sizeDelta (24, 14) 로 **icon 우측 끝에서 +22px 우측 확장**. 슬롯 1 와 슬롯 2 사이 spacing 만 4px (= 30 icon + 4 spacing = 34px 간격) 으로 두면 슬롯 1 배지가 슬롯 2 icon 영역을 침범 (배지 우측 끝 64 vs 슬롯 2 좌측 끝 46 → 18px 시각 충돌). 슬롯 2 anchoredPosition.x 를 **68 로 단정** — 슬롯 1 배지 영역 (12~64) 회피 + spacing 4 추가. 두 슬롯 모두 ×N 배지 표시 케이스에도 시각 충돌 없음.

#### 2.3.2 중첩 픽 ×N 배지 (각 슬롯 독립)

각 슬롯의 ×N 배지는 **그 슬롯에 매핑된 카드의 PickCount 단독 기준** 으로 독립 표시한다.

| 항목 | 값 | v0.6 비교 |
|---|---|---|
| 표시 조건 | 슬롯의 카드 PickCount `≥ 2` 일 때만 | (유지) |
| 형태 | 원 우측 하단에 작은 텍스트 (TMP_Text) — 별도 배경 없이 outline 으로 가독성 확보 | (유지) |
| 폰트 사이즈 | **14pt** (cap 적용) | 10 → 14 |
| 색상 | `#FBBF24` (노랑) + `#000000` outline 1px | (유지) |
| 표시 형식 | `×N` 단일 표기 (MVP 는 N 이 두 자릿수에 도달하지 않는 페이싱 — `×9+` 같은 압축 표기 도입 안 함) | (유지) |
| **슬롯 독립성 (v1.0)** | Enhance 슬롯 PickCount=2 + Spawn 슬롯 PickCount=3 일 때 → **두 배지 동시 표시** (`◉H×2 ◉+×3`). 한 슬롯만 ≥2 면 그 슬롯만 표시. | (v1.0 신규) |

#### 2.3.3 슬롯별 글자·배경 매핑 (v1.0 — Enhance 6 + Spawn 5 = 11 카드)

스펙 §6.2 의 Enhance 6 매핑을 **그대로 확정**하고, v1.0 에서 Spawn 5 매핑을 추가한다.

**Enhance 슬롯 (좌측, 글자 H/D/S/R/M/P)**:

| 카드 (ECardId) | 글자 | 배경색 (Hex) | 글자 색 | 의미 |
|---|---|---|---|---|
| WispHpBoost | `H` | `#22C55E` (초록) | `#000000` | **H**p (체력) |
| WraithDamageBoost | `D` | `#6B7280` (회색) | `#000000` | **D**amage (공격력) |
| ReaperAtkSpeed | `S` | `#EF4444` (빨강) | `#000000` | **S**peed (공격속도) |
| HexRangeBoost | `R` | `#EAB308` (노랑) | `#000000` | **R**ange (사거리) |
| PhantomMoveSpeedBoost | `M` | `#1F2937` (검정) | `#FFFFFF` | **M**ove (이동속도) |
| PlagueSlowBoost | `P` | `#A855F7` (보라) | `#000000` | **P**lague (둔화 강화) |

**Spawn 슬롯 (우측, 글자 `+`, v1.0 신규)**:

| 카드 (ECardId) | 글자 | 배경색 (Hex) | 글자 색 | 의미 |
|---|---|---|---|---|
| SpawnWisps | `+` | `#22C55E` (초록 = Wisp 종 색) | `#000000` | Wisp 동시 출력 +1 |
| SpawnWraith | `+` | `#6B7280` (회색 = Wraith 종 색) | `#000000` | Wraith 동시 출력 +1 |
| SpawnReapers | `+` | `#EF4444` (빨강 = Reaper 종 색) | `#000000` | Reaper 동시 출력 +1 |
| SpawnPlagues | `+` | `#A855F7` (보라 = Plague 종 색) | `#000000` | Plague 동시 출력 +1 |
| SpawnPhantoms | `+` | `#1F2937` (검정 = Phantom 종 색) | `#FFFFFF` | Phantom 동시 출력 +1 |
| (Hex 종) | (없음) | — | — | **SpawnHex 카드 존재하지 않음** — Hex 셀의 Spawn 슬롯은 영구 비활성 |

**결정 사유 (v1.0 Spawn 슬롯 글자 `+` 단일 선택)**:
- 종 첫 글자 (W/Wr/R/P/Ph) 안: 강화 글자 (H/D/S/R/M/P) 와 충돌 가능 (`P` = Plague Enhance vs SpawnPlagues / SpawnPhantoms) → 후순위.
- 곱 기호 `×`: 본체 row 의 동시 출력 `×N` 과 시각 충돌 → 후순위.
- **`+` 글자 (증가의 보편 기호)**: 모든 5 Spawn 카드에 단일 글자 적용, 종 구분은 배경색으로 (종 색 매핑 그대로 재사용). 강화 글자 충돌 없음, 가산의 직관적 의미. v1.0 단일 단정.
- Hex 종은 Spawn 카드 자체가 풀에 존재하지 않음 — `IconLetterFor(ECardId)` 의 fallback `(' ', Color.gray, Color.white)` 에 자연 진입하여 슬롯 미표시 처리.

#### 2.3.4 아이콘 표시 갱신 (v1.0 — Enhance + Spawn 동일 트리거)

| 트리거 | 동작 |
|---|---|
| `BattleController.OnTypeModifierChanged(EMonster)` (기존) | 출력 종이 일치하는 모든 셀의 **아이콘 row 양 슬롯 재구성** — 강화 또는 추가 생산 카드가 픽된 경우 동일하게 발행 |
| `Spawner.OnOutputTypeChanged` (기존) | 해당 셀의 아이콘 row 양 슬롯 재구성 (출력 종 자체가 바뀌었으므로 새 종 기준으로 Enhance/Spawn 둘 다 재계산) |

같은 종을 출력하는 스포너 셀들은 항상 동일한 아이콘 row (좌 + 우 슬롯 동일) 를 가진다 (강화·추가 생산 모두 종 단위 글로벌). v1.0 부터 SpawnX 카드 픽 시점에도 `OnTypeModifierChanged(type)` 이 발행되도록 BattleController 의 `IncrementSpawnerOutput` 경로가 픽 추적 + 이벤트 발행을 한다 (§4.3 v1.0).

#### 2.3.5 셀 본체 `×N` vs Spawn 슬롯 `×N` 의미 구분 (v1.0 명시)

두 `×N` 표기가 한 셀 안에 동시 노출될 수 있어 의미 혼동 방지용으로 단정한다.

| 위치 | 출처 | 의미 |
|---|---|---|
| 셀 본체 row 우측 (CHText `_countText`, 20pt 노랑) | `Spawner.OutputCount` (해당 Spawner 의 현재 동시 출력 마릿수) | **현재 살아있는 스폰 가능 마릿수** — 동프레임 마릿수 상태 |
| Spawn 슬롯 ×N 배지 (아이콘 우측 하단, 14pt 노랑) | `AppliedBuff.PickCount` (해당 종의 SpawnX 카드 누적 픽 횟수) | **빌드 히스토리** — 이 종을 위해 SpawnX 카드를 몇 번 픽했나 |

**두 값이 다를 수 있는 케이스**:
- `Replace` 카드로 종이 교체된 셀: 원래 Wisp 출력이었던 셀이 Wraith 로 교체되면, 본체 `×N` 은 Wraith Spawner.OutputCount (1~여러) 인데 Spawn 슬롯 PickCount 는 SpawnWraith 픽 횟수 (별개).
- 한 종을 출력하는 Spawner 가 여러 대 (예: 2 대) 일 때 SpawnWisps 1회 픽 → 2 Spawner 모두 OutputCount=2 (=2), Spawn 슬롯 PickCount=1.

두 값은 **상호 보완** — 본체 `×N` 은 현재 마릿수 상태, Spawn 슬롯 `×N` 은 빌드 결정 히스토리. 한 셀에 모두 노출됨으로써 플레이어가 "지금 몇 마리 / 내가 몇 번 강화했나" 둘 다 즉답 가능.

#### 2.3.6 Retroactive 시각 정책 (v1.0)

**케이스**: 플레이어가 SpawnPhantoms 를 픽한 시점에 Phantom 출력 Spawner 가 0 대일 수 있다. 그 경우 `IncrementSpawnerOutput(Phantom)` 은 no-op (CurrentType==Phantom 인 Spawner 0 개) 이지만, **카드 픽 추적은 `_typeModifierPicks[Phantom]` 에 누적된다** (TrackCardPick 진입 — §4.3 v1.0).

이후 Replace 카드로 한 Spawner 가 Phantom 으로 교체되면, 그 셀의 Spawn 슬롯에는 **이전에 픽됐던 SpawnPhantoms 의 `+` 아이콘이 PickCount 그대로 retroactively 표시된다** (셀 RebindIconRow 가 `snapshot.AppliedBuffs` 를 읽으므로).

**v1.0 결정 — 옵션 (a) 채택 (advisor 권장)**: 위 retroactive 시각을 **의도된 동작** 으로 단정. 사유:
- Enhance 카드도 같은 retroactive 의미론을 가짐 — Hex 종 출력 Spawner 가 0 대인 상태에서 HexRangeBoost 픽 시 `_typeModifiers[Hex]` 가 누적되고, 이후 Replace 로 Hex 출력 Spawner 가 생기면 `ApplyMonsterStats` 가 강화 배율을 그대로 적용 (BattleController.cs:300~316 의 글로벌 dict 정책). Enhance 와 Spawn 의 retroactive 의미론을 일치시킨다.
- 시각 의미 = "이 종의 빌드 풀에 무엇이 픽됐나" (cell-level 의 빌드 카드 디스플레이) — 현재 출력 마릿수와 직교. 본체 `×N` 이 실시간 마릿수를 담당하므로 슬롯 아이콘은 빌드 히스토리만 보여주면 됨.
- 옵션 (b) (Spawner 존재 시점에만 픽 추적) 는 카드 픽 순서에 따라 시각 표시가 달라지는 비결정적 동작 — 같은 픽 카드가 라운드 진행 시점에 따라 보일 수도 안 보일 수도 있어 플레이어가 빌드 히스토리를 이해하기 어려워짐.

> 본 §2.3.6 정책은 §7 v1.0 항목에 락 결정으로 명시되어 차후 재논의를 회피한다.

### 2.4 종 색상 매핑 (참조 — 컨셉 §11.4 그대로)

| 종 | 색칩 / 아이콘 배경 |
|---|---|
| Wisp | `#22C55E` (초록) |
| Wraith | `#6B7280` (회색) |
| Reaper | `#EF4444` (빨강) |
| Hex | `#EAB308` (노랑) |
| Plague | `#A855F7` (보라) |
| Phantom | `#1F2937` (검정) |

### 2.5 SpawnerStatusTooltip (셀 위 floating)

#### 2.5.1 외형

| 항목 | 값 |
|---|---|
| 가로 | **201px** (셀 폭 134 × 1.5 = 201 — 사용자 명시 요청 "스포너 한 패널의 1.5배". "한 패널" = 셀 1개 해석. §7 v0.7 결정 사유 참조) |
| 세로 | 가변 (자식 텍스트 높이 + padding) |
| 최소 세로 | **252px** (셀 높이 168 × 1.5 = 252 — 헤더 + ScrollRect 구조 수용에 적절) |
| 배경 색 | `#1F2937` (alpha 0.95) |
| 테두리 | 1px `#FBBF24` (노랑 — 활성 셀 색과 일치) |
| 내부 padding | 8px |
| 화살표 | 하단 중앙에 ▼ 모양 (8×6 px, 같은 배경색) — 셀을 가리킴 |

#### 2.5.2 배치

| 항목 | 값 |
|---|---|
| 위치 | 해당 셀 바로 위 (셀 상단으로부터 +8px gap) |
| Pivot | `(0.5, 0)` (하단 중앙이 셀 상단을 가리킴) |
| 화면 좌우 끝 clamping | **좌측 4px margin 유지** — v0.4~v0.6 동안 "패널 중앙 정렬" 가정으로 clamping 분기 제거가 검토됐으나 코드는 분기를 계속 유지해 왔음 (`SpawnerStatusTooltip.cs:98-107`). v0.7 좌측 anchor 변경으로 셀 0 의 툴팁이 좌측을 벗어나며 분기 발동 조건 충족 — **코드 변경 없이 자동 발동** (아래 검산) |

**Clamping 자동 발동 검산 (v0.8 1280×720 기준, 좌측 anchor 패널)**:
- 패널 anchor `(0, 0)` / pivot `(0, 0)` / anchoredPosition `(0, 0)` → 패널 x-range = **(0, 850)**.
- **셀 0 중심**: 패널 좌측 + 좌측 padding 8 + 첫 셀 폭 134/2 = **75px**. 툴팁 폭 201, pivot (0.5, 0) → 툴팁 x-range = **(−25.5, +175.5)** — 화면 좌측 25.5px 벗어남.
- **셀 1 중심**: 패널 좌측 + 좌측 padding 8 + 셀 134 + spacing 6 + 134/2 = 8 + 134 + 6 + 67 = **215px**. 툴팁 x-range = **(114.5, +315.5)** — **화면 안 (좌측 114.5px > safeMargin 4)**.
- **셀 5 중심**: 패널 좌측 + 좌측 padding 8 + 셀 134×5 + spacing 6×5 + 134/2 = 8 + 670 + 30 + 67 = **775px**. 툴팁 x-range = **(674.5, 875.5)** — 화면 안 (1280 이내).
- 결론: **셀 0 위 툴팁만** 좌측 clamping 발동 (design-reviewer 권장수정 C2 정정 — v0.7 본문의 "셀 0/1 좌측 벗어남" 은 부정확). 셀 1~5 는 분기 미발동.

**Clamping 구현 — 코드 변경 없음 (v0.8 design-reviewer 권장수정 C1 정정)**:
- 현행 `SpawnerStatusTooltip.cs:98-107` 의 좌·우 양방향 `Mathf.Clamp(localInCanvas.x, minX, maxX)` 분기는 **v0.4~v0.7 내내 그대로 유지** (제거된 적 없음).
- v0.7 좌측 anchor 변경 후 셀 0 케이스에서 `localInCanvas.x` 가 `minX` 미만이 되어 자동으로 clamp 발동. **코드 변경 불요**, 빌더 변경 불요.
- 클램핑 발동 시 ▼ 화살표는 셀 위로 그대로 위치 (anchor cell 의 가로 중앙 정렬 유지) — 툴팁 본체만 평행 이동. 화살표가 툴팁의 좌측 끝에 위치하는 시각 부조화 허용 (MVP — 셀 0 에서만 발생, 빈도 낮음).

> 본 문서 v0.4~v0.6 의 "clamping 제거 락 결정" 은 본문 기술상 결정이었으며 코드 분기는 보존되어 있었다. v0.7 좌측 anchor 변경으로 분기 발동 조건이 자연 충족 → v0.8 에선 "본문/코드 일관성 유지" 로 정정 (제거된 적 없는 분기를 "부활" 표현했던 v0.7 표현 폐기).

#### 2.5.3 내용 구성

```text
┌─ 201px ─────────────────────────┐
│ Spawner #0 — Wisp ×2            │ ← 헤더 (CHText, 11pt, 흰색) — CHPoolingScrollView 외부 고정
│ ─────────────────────────────── │
│ ┌─ CHPoolingScrollView (BuffLine, AppliedBuff) ┐ │ ← 본문 줄 영역 (v0.8 / v1.0 행 종류 확장)
│ │ ◉H ×2  체력 ×2.25           │ │ ← Enhance BuffLine (좌 슬롯 카드, §2.5.5)
│ │         (200 → 450)         │ │
│ │ ◉+ ×3  동시 출력 +3          │ │ ← Spawn BuffLine (v1.0 신규, §2.5.5)
│ │ (현 페이싱 — Enhance 1 + Spawn 1) │ │
│ └─────────────────────────────┘ │
└─────────────────────────────────┘
```

> 종 1 ↔ Enhance 카드 1 매핑 + 종 1 ↔ Spawn 카드 1 매핑 (Hex 제외) 이므로 본문 줄은 **현 페이싱에선 0줄 (둘 다 미픽) ~ 2줄 (Enhance 1 + Spawn 1, Hex 종은 최대 1줄)**. v0.7 에서 본문 영역을 ScrollRect 로 래핑한 이유는 사용자 명시 요청 — **미래 1↔다 매핑 확장 또는 PickCount 누적 표현 풍부화에 대비한 구조 보험**. v0.8 에서 Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙을 완전 적용해 **`CHPoolingScrollView<BuffLine, AppliedBuff>` 파생 컴포넌트로 재단정** — `SetItemList(IReadOnlyList<AppliedBuff>)` 단일 호출로 줄 수만큼 BuffLine 인스턴스 자동 생성·재사용. v1.0 부터 `AppliedBuffs` 리스트에 Enhance + Spawn 두 카테고리 엔트리가 함께 들어가며, BuffLine 의 `FormatBody` 가 `Source.Category` 분기로 두 종류 본문 포맷을 모두 처리한다. 헤더는 CHPoolingScrollView 외부에 고정되어 스크롤 시에도 항상 보임.

**다중 자식 BuffLine 단정 (v0.8 — design-reviewer BLOCKER B3 해소)**:
v0.7 에서 모호하게 남았던 "단일 CHText vs 다중 자식" 의 두 안을 **다중 자식 BuffLine 로 단정**. 사유:
- CHPoolingScrollView 의 `TItem` 은 한 풀 인스턴스 = 한 줄 = 한 `MonoBehaviour` 클래스. 단일 CHText 접근은 CHPoolingScrollView 가 풀 인스턴스를 자동 관리한다는 정신과 충돌.
- 미래 1↔다 매핑 확장 / PickCount 다수 누적 표시 시점에 자연 동작. 단일 CHText 안 줄바꿈으로 흉내내는 안은 그 시점에 다시 재구성 비용 발생.
- 한 줄 = `IconLetter 30×30 원 + ×N 배지 + 스탯명 + 누적 배율 + before/after` 의 복합 레이아웃 (§2.5.5 강화 줄 포맷). 단일 CHText 로는 IconLetter 의 원형 배경색·outline 등의 시각 표현이 불가능.

#### 2.5.4 헤더 포맷

| 항목 | 값 |
|---|---|
| 표시 형식 | `Spawner #{index} — {EMonster} ×{OutputCount}` |
| 폰트 사이즈 | 11pt |
| 색상 | `#FFFFFF` |
| 예시 | `Spawner #0 — Wisp ×2` |

#### 2.5.5 강화 줄 포맷 (스탯별 템플릿)

종에 적용된 강화 카드의 효과를 한 줄로 표시한다 (종 1 ↔ 카드 1 매핑이므로 강화 적용 시 정확히 1 줄). 한 줄에 ① 아이콘 + 글자 ② 픽 횟수 배지 ③ 스탯명 + 누적 배율 ④ before → after 절대값 순.

**CHPoolingScrollView 구조 (v0.8 신규 — Rule 03 §3 완전 적용)**:

툴팁 본문 영역 = `BuffPoolingScrollView : CHPoolingScrollView<BuffLine, AppliedBuff>` 1세트 (TItem = `BuffLine` MonoBehaviour, TData = `BattleViewModel.AppliedBuff`).

| 컴포넌트 / 직렬화 필드 | 설정 | 비고 |
|---|---|---|
| GameObject 부착 컴포넌트 | `RectTransform + ScrollRect + BuffPoolingScrollView (CHPoolingScrollView<BuffLine, AppliedBuff> 파생)` | `CHPoolingScrollView` 가 `[RequireComponent(typeof(ScrollRect))]` — 함께 부착 자동 |
| `ScrollRect.horizontal / vertical` | `false / true` (`SetItemList` 호출 시 `_scrollDirection = Vertical` 에 의해 자동 설정 — CHPoolingScrollView 내부 line 252~266) | 명시 빌더 작업 불요 |
| `ScrollRect.MovementType` | **Elastic** (빌더 직접 설정) | 끝에서 살짝 튕김, MVP 단순화 |
| `ScrollRect.Elasticity` | **0.1** (Unity 기본값) | |
| `ScrollRect.Inertia` | **on** | |
| `ScrollRect.DecelerationRate` | **0.135** (Unity 기본값) | |
| `ScrollRect.ScrollSensitivity` | **1** (Unity 기본값) | |
| Scrollbar | **추가하지 않음** | MVP 단순화 |
| Viewport (자식 GameObject — `ScrollRect.viewport` 직렬화) | `RectTransform` 전체 stretch + `RectMask2D` + **Image (alpha 0.001, raycastTarget = true)** | 거의 투명 Image 로 drag 입력 receiver 역할 |
| Content (Viewport 자식 GameObject — `ScrollRect.content` 직렬화) | **`RectTransform` 만** — CHPoolingScrollView 가 anchor `(0.5, 1)` / pivot `(0.5, 1)` / sizeDelta 를 `SetContentTransform` (line 454~) 에서 자동 설정 | **VerticalLayoutGroup / ContentSizeFitter 부착 X** — CHPoolingScrollView 가 자식 RectTransform 의 anchor / pivot / anchoredPosition 을 `InitItemTransform` (line 770~) 에서 직접 박는다 (Layout 컴포넌트와 충돌하므로 둘 다 부착하면 안 됨) |
| `BuffPoolingScrollView._origin` (CHPoolingScrollView 직렬화) | `BuffLine.prefab` 인스턴스 참조 — Content 의 첫 자식으로 사전 배치, `Start` 시 `SetActive(false)` 자동 처리 (CHPoolingScrollView line 212) | 신규 prefab — §4.6 |
| `BuffPoolingScrollView._itemSize` | **런타임 무시 — dead serialization** (`CHPoolingScrollView.SetItemSize` (CHPoolingScrollView.cs:394-402) 이 `SetItemList` 호출 시마다 origin RectTransform 의 `rect.width/height × localScale` 로 무조건 덮어씀). **BuffLine prefab sizeDelta = (185, 24) 가 단일 진실** — 빌더는 prefab sizeDelta 만 박으면 충분, `_itemSize` 직렬화 wire-up 불요 (v0.9 P1 정정) | |
| `BuffPoolingScrollView._itemGap` | `(0, 4)` (세로 줄 사이 4px) | v0.7 spacing 4 그대로 |
| `BuffPoolingScrollView._padding` | `(2, 2, 2, 2)` (RectOffset left/right/top/bottom) | v0.7 padding 그대로 |
| `BuffPoolingScrollView._scrollDirection` | `PoolingScrollViewDirection.Vertical` | |
| `BuffPoolingScrollView._align` | `PoolingScrollViewAlign.LeftOrTop` | 위→아래 쌓임 |
| `BuffPoolingScrollView._rowCount / _columnCount / _poolItemCount` | `0 / 0 / 0` (자동) | CHPoolingScrollView 가 viewport 폭 ÷ itemSize.x = 열 수 자동 계산 (line 404~) |

**영역 치수 단일 진실 (v0.8 — design-reviewer BLOCKER B2 해소)**:
- 툴팁 본체 폭 201, 내부 padding 8 → 본문 가용 폭 = 201 − 8×2 = **185px**
- 헤더 영역 32px (CHText 11pt + 8px padding), CHPoolingScrollView 영역 시작 y = 헤더 하단 + 8px gap
- CHPoolingScrollView RectTransform: anchorMin/anchorMax = `(0, 0)` / `(1, 1)` (본문 안 stretch), offsetMin `(8, 8)` / offsetMax `(-8, -40)` — 좌·하 padding 8 / 우 padding 8 / 상 헤더 32 + gap 8 = 40
- 결과 ScrollRect viewport 폭 = 201 − 8 − 8 = **185px**, 세로 = 252 − 8 − 40 = **204px**
- 본문 가용 세로 = 252 − 헤더 32 − padding×2 16 − gap 8 = **196px** (위 ScrollRect 세로 204 와 8px 오차 — ScrollRect 가 내부 padding 2×2 = 4 와 BuffLine ItemGap 4 까지 흡수해 실 표시 영역은 196 안)
- BuffLine 한 줄 높이 = **24px** (**BuffLine prefab sizeDelta.y 가 단일 진실** — `_itemSize` 직렬화는 `SetItemSize` 가 매번 덮어씀, v0.9 P1)
- 현 페이싱 1줄: 24px 사용 → 196 안에 여유 충분
- 미래 6장 확장 시: 24×6 + ItemGap 4×5 + padding 2×2 = 168 → 196 안에 들어감 / 더 많으면 CHPoolingScrollView 가 자동 스크롤 풀링

**BuffLine MonoBehaviour 명세 (v0.8 신규 — §4.6)**:
- 자식 구조: `IconLetterRoot (원형 Image + IconLetter CHText 12pt) + ×N Badge CHText 10pt + 본문 CHText 10pt`
- `CHPoolingScrollView<BuffLine, AppliedBuff>.InitItem(BuffLine item, AppliedBuff data, int index)` 가 풀 인스턴스 재사용 시 호출 → BuffLine 의 `Bind(AppliedBuff, EMonster, BalanceConfig)` 가 IconLetter / 배지 / 본문 텍스트를 setter
- `InitPoolingObject(BuffLine item)` 은 1회 초기화 (대부분 비어 있어도 OK — `InitItem` 이 매번 호출되므로 별도 클렌업 불요)
- BuffLine prefab 의 폰트 cap (v0.8 — 셀 cap 정책과 동일 정신, 비례 적용 안 함): **IconLetter 12pt · ×N 배지 10pt · 본문 텍스트 10pt** (v0.7 폰트 사이즈 그대로 유지). 사유 — 셀 면적 2.094× 확대분이 폰트가 아니라 padding·아이콘 원 면적으로 흡수되는 정책(§2.2.1)을 툴팁에도 동일 적용.

**raycast 정책 (v0.8)**: 툴팁 본체는 EUI.SpawnerStatusTooltip 으로 단독 떠 있고 외부 click 으로 닫지 않으므로 (§2.5.6) CHPoolingScrollView 가 drag 입력을 받는 데 다른 UI 와 경쟁할 일 없음. Viewport 의 거의 투명 Image (alpha 0.001, raycastTarget=true) 가 drag 입력 receiver 역할.

**CHPoolingScrollView 도입의 비용 검증**: 현 페이싱에선 0~1 줄밖에 안 나오는데 CHPoolingScrollView 가 과한가? — 아니다. CHPoolingScrollView 는 풀 인스턴스를 viewport 크기 기준으로 자동 (line 430~) 최소 개수만 생성, 0줄일 땐 `SetItemList(empty)` → 모든 자식 SetActive(false), 1줄일 땐 1개만 활성 — 비용 무시 수준. **사용자 명시 결정 ("룰에 chscrollview 쓰라고 안되어있어?") 으로 Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 우선 원칙을 그대로 적용** — 미래 1↔다 매핑 확장 / PickCount 다수 누적 시 본 구조가 그대로 동작.

**Enhance 카테고리 (`Source.Category == ECardCategory.Enhance`)** — 스탯별 6 템플릿:

| 스탯 (EMonsterStatKind) | 줄 포맷 | 예시 (1픽, ×N 배지 없음) |
|---|---|---|
| `Hp` | `{아이콘}[ ×N] {스탯명} ×{누적배율} ({Base} → {Result})` | `H 체력 ×1.5 (200 → 300)` |
| `Power` | `{아이콘}[ ×N] {스탯명} ×{누적배율} ({Base} → {Result})` | `D 공격력 ×1.5 (200 → 300)` |
| `Range` | `{아이콘}[ ×N] {스탯명} ×{누적배율} ({Base} → {Result})` | `R 사거리 ×1.4 (4.0 → 5.6)` |
| `MoveSpeed` | `{아이콘}[ ×N] {스탯명} ×{누적배율} ({Base} → {Result})` | `M 이동속도 ×1.5 (2.0 → 3.0)` |
| `Cooldown` | `{아이콘}[ ×N] {스탯명} ×{공격속도역수} (cd {Base}s → {Result}s)` | `S 공격속도 ×1.43 (cd 1.0s → 0.7s)` |
| `SlowFactor` | `{아이콘}[ ×N] {스탯명} (둔화 {Base} → {Result}) — 강화` | `P 둔화 효과 (0.8 → 0.6) — 강화` |

> `[ ×N]` 은 PickCount≥2 일 때만 노출 (§2.3.2 정책 그대로).

**Spawn 카테고리 (`Source.Category == ECardCategory.Spawn`, v1.0 신규)** — 단일 템플릿:

| 카테고리 | 줄 포맷 | 예시 (1픽 / 2픽) |
|---|---|---|
| `Spawn` | `{아이콘}[ ×N] 동시 출력 +{PickCount}` | (1픽) `+ 동시 출력 +1` / (2픽) `+ ×2 동시 출력 +2` |

**Spawn 카테고리 결정 사유 (advisor 권장 단정)**:
- 본문이 `{Base} → {Result}` 형식이 아닌 이유: Spawn 카드는 stat 곱연산이 아니라 **이벤트 누적 (`IncrementSpawnerOutput` 1회 호출 = +1 마릿수 이벤트)**. 종을 출력하는 Spawner 마다 OutputCount 가 다를 수 있어 단일 "Base → Result" 표시 자체가 불가능.
- `(1 → N+1)` 같은 표현은 misleading — Spawner 마다 시작 OutputCount 가 다르고 (1 또는 그 이상), 픽 시점의 매핑 종을 출력 중인 Spawner 들에 대해서만 +1 씩 누적되므로 단일 base 가 없음.
- **`동시 출력 +{PickCount}` 단일 단정** — "이 종을 위한 SpawnX 카드를 N 번 픽해서 동시 출력이 N 더해졌다" 의미. 현재 마릿수 상태는 본체 row `×N` 이 별도 표시 (§2.3.5).
- PickCount==1 일 때도 `+1` 로 표기 (Enhance 카테고리의 `×1.5` 와 일관 — 1픽도 명시).
- `{아이콘}[ ×N]` 부분은 Enhance 와 동일 — Spawn 아이콘은 `+` 글자 + 종 색 배경 (§2.3.3 v1.0 매핑), `×N` 배지는 PickCount≥2 일 때만.

**`AppliedBuff.Stat` 필드 정책 (v1.0 명시)** — Spawn 카테고리는 stat 이 의미 없음. `EMonsterStatKind` 에 `None` 같은 신규 값을 **추가하지 않는다** (Rule 02 §8 Enum churn 회피).
- **컨벤션**: Spawn 카테고리 픽의 `AppliedBuff.Stat` 은 `EMonsterStatKind.Hp` (default 값, **읽히지 않음**) 로 설정.
- `BuffLine.FormatBody` 가 `buff.Source.Category == ECardCategory.Spawn` 분기를 **`switch (buff.Stat)` 보다 먼저** 검사 — Spawn 카테고리는 Stat 필드를 보지 않고 Spawn 단일 포맷으로 분기.
- Enhance 카테고리는 기존 6 stat switch 그대로.
- 본 컨벤션은 §4.3 v1.0 의 `TrackSpawnPick` 명세에 명시 적용.

**스탯명 라벨 (한국어 통일)**

| EMonsterStatKind | 라벨 |
|---|---|
| Hp | `체력` |
| Power | `공격력` |
| Cooldown | `공격속도` (cd 의 역수 의미로 변환 표시) |
| Range | `사거리` |
| MoveSpeed | `이동속도` |
| SlowFactor | `둔화 효과` |

**Cooldown 표기 주의**: `Cooldown` 스탯은 곱 0.7 = 공격속도 빨라짐. 사용자 멘탈 모델("공격속도 ↑")에 맞추기 위해 화면에는 **공격속도 ×(1/cdMul)** 으로 표시한다. 예: cdMul 0.7 → 표시 `공격속도 ×1.43`. 절대값은 `cd 1.0s → 0.7s` 형태로 원본 단위(초) 명시.

**SlowFactor 표기 주의**: `SlowFactor` 는 0.75 곱 = 둔화 강화 (적용 후 영웅 이동속도가 더 느려짐). 사용자에게 "둔화 효과가 강해짐" 의미가 전달되어야 함 → 라벨 끝에 `— 강화` 부가.

**중첩 픽 표기**: `×N` 배지는 아이콘 우측에 별도 표시. **N≥2 일 때만** 노출 (§2.3.2). N=1 이면 배지 생략. 누적 배율은 곱연산으로 계산된 결과를 그대로 표시.
- 예 1픽: `H 체력 ×1.5 (200 → 300)`
- 예 2픽: `H ×2  체력 ×2.25 (200 → 450)` (1.5 × 1.5 = 2.25)

**Base 값 (각 종 기본 스탯)**: Hp/Power/Range/Cooldown/MoveSpeed 5개 스탯은 `BalanceConfig.asset` (경로: `Assets/_Lair/Data/BalanceConfig.asset`) 의 `_monsters` 행에서 읽는다. 기획서 작성 시점 기준 컨셉 §11.3 의 6종 표:

| 종 | HP | DPS(Power)¹ | Range² | MoveSpeed³ | Cooldown⁴ |
|---|---|---|---|---|---|
| Wisp | 200 | — | — | — | — |
| Wraith | 500 | — | — | — | — |
| Reaper | 100 | 40 | — | — | 1.0s |
| Hex | 60 | 30 | 4.0 | — | — |
| Plague | 50 | 5 | — | — | — |
| Phantom | 30 | 5 | — | 2.0 | — |

¹ 컨셉서는 DPS 로만 기재 — 강화 카드는 `Power`(=공격력) 곱연산. 실제 직렬화 기본값은 gameplay-programmer 가 `BalanceConfig.asset` 에서 확인 후 사용.
² Hex 사거리 기본 4.0 추정 — 실제 값은 `BalanceConfig._monsters[Hex].Range` 필드.
³ Phantom 이동속도 — 가장 빠른 종 가정 2.0.
⁴ Reaper 공격 cd 1.0s 추정.

> **데이터 의존 (5 스탯)**: Hp/Power/Range/Cooldown/MoveSpeed Base 값은 `BalanceConfig.GetMonster(EMonster).Hp/Power/Range/Cooldown/MoveSpeed` 가 단일 진실이다. 툴팁 구현 시 BalanceConfig 에서 직접 읽어 표시 (하드코딩 금지). 만약 SO 값이 위 표와 다르면 SO 값을 우선.

**Plague SlowFactor Base 예외**: `BalanceConfig.CharacterStat` 에는 `SlowFactor` 필드가 없다 (현재 5 스탯만 보유). Plague 의 baseline slow factor 는 코드 상수 **`Lair.Character.PlagueSlowOnHit.BaseSlowFactor` (= 0.8f)** 가 단일 진실. 툴팁의 `SlowFactor` Base 값은 이 상수를 직접 참조한다.
- 본 기획에서 `BalanceConfig.CharacterStat` 에 `SlowFactor` 필드를 추가하지 **않음** — MVP 범위 외 작업 (다른 시스템에서 광범위 영향).
- 향후 `BaseSlowFactor` 도 BalanceConfig 로 이주하면 본 항목 함께 갱신.

**강화 없음 케이스**: 출력 종에 강화 카드가 1장도 픽되지 않았으면 헤더만 표시하고 본문에 `"적용된 강화 없음"` 한 줄 (10pt, `#9CA3AF` 회색).

#### 2.5.6 토글 동작

| 트리거 | 동작 |
|---|---|
| 셀 클릭 (현재 닫힘) | 해당 셀 위에 툴팁 열림, 셀 테두리 노랑(`#FBBF24`)으로 강조 |
| 같은 셀 재클릭 | 툴팁 닫힘, 셀 테두리 원복 |
| 다른 셀 클릭 | 이전 셀 테두리 원복 + 툴팁을 새 셀 위로 이동, 내용 갱신 |
| 출력 종 변경 (열려있는 동안) | 헤더 + 강화 줄 즉시 재계산해 갱신 (`OnOutputTypeChanged` + `OnTypeModifierChanged`) |
| 외부 클릭 (패널/셀 바깥) | MVP 에선 무처리 (재클릭으로만 닫힘 — 단순화) |

> **툴팁 vs 모달 dismiss 비대칭 근거**: 툴팁은 hover-like 가벼운 보조 UI (셀 위 floating, 화면 일부만 덮음) → 외부 클릭은 게임 조작과 충돌 가능성이 커서 무처리. 모달(§2.7.5) 은 전체 화면 dim 으로 게임 조작 자체를 차단하는 무거운 UI → 배경 dim 클릭 닫기로 빠른 dismiss 제공. 두 UX 는 의도적으로 다름.

### 2.6 BuildPanel 수정 (스펙 §6.5)

#### 2.6.1 필터

```
foreach (var entry in vm.Build)
{
    //# 종 강화 패시브 6장 제외 — ECardCategory.Enhance + IsPassive 둘 다 만족할 때만 거름.
    //# IsPassive 추가 이유: 현재 액티브 카드 4장(Berserk/BloodThirst/Frenzy/IronWill)도
    //# SO 상 _category: 0 (= Enhance) 로 직렬화되어 있어, Category 단일 조건으로 거르면
    //# 액티브 카드까지 함께 사라짐. BuildEntry.IsPassive 가 이미 VM 에 있으므로 단순 추가.
    if (entry.Card.Category == ECardCategory.Enhance && entry.IsPassive) continue;
    //# 이하 기존 로직 (Spawn / Replace / Environment 패시브 + 액티브 전부)
}
```

대상 카드:
- **제외 (6장)**: WispHpBoost / WraithDamageBoost / ReaperAtkSpeed / HexRangeBoost / PlagueSlowBoost / PhantomMoveSpeedBoost — `_category = Enhance` 이면서 `IsPassive = true`.
- **유지 (액티브 4장)**: Berserk / BloodThirst / Frenzy / IronWill — `_category = Enhance` 이지만 `IsPassive = false`.
- **유지 (나머지 패시브 9장)**: Spawn / Replace / Environment 카테고리 → Category 검사에서 자연 통과.

> SO `_category` 데이터 카테고리화는 효과 분류와 일치하지 않는 상태(액티브 4장이 Enhance 로 직렬화). 본 기획은 SO 데이터를 수정하지 않고 BuildPanel 필터에서만 우회한다. SO `_category` 재분류는 별도 작업.

#### 2.6.2 셀별 `_detailRoot` 제거

- `BuildPanel.cs` 의 `_detailRoot` / `_detailName` / `_detailDesc` / `_detailShown` 직렬화 필드 + `ShowDetail` 메서드 제거.
- `BuildIconCell.Bind` 호출부에서 `onClick` 콜백 `null` 전달 (현행 구현). 시그니처는 그대로 유지 — 향후 다른 호출처가 콜백을 사용할 가능성 보존.
- `BuildIconCell._button.OnClick(...)` 호출 제거 — 자식 셀은 클릭 raycast 를 받지 않음 (raycaster blocker 비활성). 패널 루트에서 클릭을 받아 모달을 띄움.

#### 2.6.3 패널 루트 클릭

- BuildPanel 의 루트 GameObject (또는 Background Image) 에 **`CHButton`** 부착 (Rule 03 §3).
- `BuildPanel.cs` 가 자체 직렬화 필드 `CHButton _rootButton` 보유, `Bind(vm)` 에서 `_rootButton.OnClick(() => CHMUI.Instance.ShowUI(EUI.BuildModalPopup, new BuildModalPopupArg { ViewModel = vm }), closeDisposable)` 등록.
- **ScrollRect 와의 raycast 일관성 (v0.5 — §2.8.5 와 묶임)**: BuildPanel 내부 PassiveSection / ActiveSection 각각이 ScrollRect 를 가지더라도, Unity EventSystem 이 **drag 와 click 을 자동 분리** 처리한다. Pointer down 후 drag threshold 를 넘으면 `IBeginDragHandler` 가 ScrollRect 로 bubble — drag 가 승, click 은 cancel. Threshold 를 안 넘고 pointer up 하면 `IPointerClickHandler` 가 root `CHButton` 으로 bubble — 모달 열림. 별도 dispatcher 분기·pointerDown/pointerClick 분리 코드 불필요. v0.3 의 "셀이 raycast 안 받음" 정책(§2.6.2)도 그대로 유지 — 셀은 click/drag 모두 통과, ScrollRect 가 drag 를 잡고 root CHButton 이 click 을 잡는다.

### 2.7 BuildModalPopup (화면 중앙 모달)

#### 2.7.1 외형

| 항목 | 값 |
|---|---|
| 화면 배치 | 화면 중앙 (Anchor `middle-center`, Pivot `(0.5, 0.5)`) |
| 모달 가로 | **640px** |
| 모달 세로 | **480px** |
| 배경 dim (전체 화면 덮개) | `#000000` alpha 0.6 |
| 모달 배경 색 | `#1F2937` alpha 0.98 |
| 모달 테두리 | 2px `#FBBF24` (노랑) |
| 내부 padding | 16px |

#### 2.7.2 내부 레이아웃

```text
┌─ 640 × 480 ─────────────────────────────────────────┐
│ 빌드 상세                              [X]   │ ← 헤더 24px
│ ─────────────────────────────────────────── │
│ 패시브 (좌, 50%)        │ 액티브 (우, 50%)  │
│ ─────────────────────  │ ─────────────────  │
│ [□] WispHpBoost   ×2  │ [□] Fear          │
│     체력 ×2.25 …      │     영웅 3초간 도망  │
│                       │                    │
│ [□] SpawnWisps        │ [□] Frenzy        │
│     동시출력 +1        │     공속 +50% …    │
│ …                     │ …                  │
│ (Scroll if 8+ items)  │ (Scroll if 8+ items)│
└─────────────────────────────────────────────┘
```

| 영역 | 값 |
|---|---|
| 헤더 영역 높이 | 32px |
| 좌(패시브) : 우(액티브) 분할 비율 | **50 : 50** |
| 좌우 섹션 사이 구분선 | 1px `#374151` 세로 |
| 섹션 내부 카드 셀 높이 | 56px |
| 카드 셀 간 간격 | 4px |
| 스크롤 | 각 섹션 내부 **`CHPoolingScrollView<BuildModalCardCell, BuildEntry>`** 파생 컴포넌트 1세트 (v0.8 — v0.3 의 "Rule 03 §3 예외" 결정 철회. 사용자 명시 결정 "룰에 chscrollview 쓰라고 안되어있어?" 로 Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용) |
| 카드 셀 표시 형식 | `[프레임색] {DisplayName}[ ×N]` 한 줄 + 아래 `{Description}` 한 줄 (`×N` 은 N≥2 일 때만) |
| 프레임 색 | `CardView.CategoryColor(Category)` 기존 함수 그대로 (Enhance 도 포함 — 모달은 6장 전부 표시) |

**좌/우 섹션 CHPoolingScrollView 구조 (v0.8 신규)**:

| 컴포넌트 / 직렬화 필드 | 설정 | 비고 |
|---|---|---|
| 각 섹션 GameObject 부착 | `RectTransform + ScrollRect + BuildModalCardPoolingScrollView (CHPoolingScrollView<BuildModalCardCell, BuildEntry> 파생)` | 좌 패시브 / 우 액티브 각각 1세트 |
| `ScrollRect.MovementType` | **Elastic** | §2.5.5 / §2.8.5 와 동일 파라미터 묶음 |
| Viewport (자식) | `RectTransform` 전체 stretch + `RectMask2D` + Image (alpha 0.001, raycastTarget = true) | drag receiver |
| Content (Viewport 자식) | `RectTransform` 만 — CHPoolingScrollView 가 자동 설정 | VLG/CSF 부착 X |
| `_origin` | `BuildModalCardCell.prefab` (Content 의 첫 자식) | 신규 prefab — §4.6 |
| `_itemSize` | **런타임 무시 — dead serialization** (`SetItemSize` 가 origin RectTransform sizeDelta 로 매번 덮어씀). **BuildModalCardCell prefab sizeDelta 가 단일 진실 — 표시 값 (303, 56)** (모달 헤더 32 제외 가용 세로 = 480 − 32 − padding 16×2 = 416, 가용 폭 = (640 − padding 32 − 구분선 1) ÷ 2 ≈ 303). 빌더가 prefab sizeDelta = (303, 56) 박음 (v0.9 P1 — 기존 prefab 280×56 갱신 필요, §4.11) | 셀 높이 56px (§2.7.2 기존) |
| `_itemGap` | `(0, 4)` | 셀 간 간격 4px (§2.7.2 기존) |
| `_padding` | `(0, 0, 0, 0)` | 섹션 자체 padding 은 모달 헤더/footer 가 흡수 |
| `_scrollDirection` | `Vertical` | |
| `_align` | `LeftOrTop` | 위→아래 쌓임 |

**BuildModalCardCell MonoBehaviour 명세 (v0.8 신규 — §4.6)**:
- 현재 `Assets/_Lair/Scripts/UI/BuildModalPopup.cs:118` 안에 nested 정의되어 있는 `BuildModalCardCell` 클래스를 **별도 파일 `Assets/_Lair/Scripts/UI/BuildModalCardCell.cs` 로 분리**한다 — CHPoolingScrollView 의 TItem 으로서 `_origin` prefab 의 component로 직렬화되어야 함. (Rule 03 §5 의 "UIArg 페어" 와는 무관 — 단순 MonoBehaviour 분리, 클래스 자체는 같은 namespace `Lair.UI`).
- 기존 `Bind(CardData, int)` 시그니처는 유지하되, CHPoolingScrollView 의 `InitItem(BuildModalCardCell item, BuildEntry data, int index)` 가 내부에서 `item.Bind(data.Card, data.Count)` 를 호출하는 어댑터 역할.
- `InitPoolingObject(BuildModalCardCell item)` 은 1회 초기화 (현재 `OnEnable` 의 ×N 텍스트 활성화 reset 로직이 풀 인스턴스 재사용 시 그대로 동작 — 추가 작업 불요).
- prefab 경로 — `Assets/_Lair/Art/UI/BuildModalCardCell.prefab` (이미 존재 — script 부착만 필요).

**기존 코드 영향 (v0.8 — gameplay-programmer 가 처리)**:
- `BuildModalPopup.cs` 의 `_passiveContent` / `_activeContent` 직렬화 필드 (현재 `Transform`) 의 의미가 변경 — "Content Transform" → "각 섹션 CHPoolingScrollView 파생 인스턴스 참조" (필드 타입 변경 + 변수명 변경 권장: `_passiveScrollView` / `_activeScrollView`).
- 기존 `FillSection(Transform, List<BuildEntry>)` (line 93~) → `FillSection(BuildModalCardPoolingScrollView, List<BuildEntry>)` 로 변경, 본문은 `scrollView.SetItemList(entries)` 단일 호출로 단순화 (`CHMPool.Pop` 직접 호출 폐기 — CHPoolingScrollView 가 자체 인스턴스 관리).
- `_spawnedCells` 추적 + `Close` 시 `CHMPool.Push` 루프 (line 30, 44~56) **삭제** — CHPoolingScrollView 가 Content 자식으로 풀 인스턴스를 보유하므로 모달 닫혀도 prefab 자식들이 같이 비활성화됨. 풀 반환 불요.
- `_cellPrefab` 직렬화 필드 (line 25) **삭제** — prefab 참조는 각 CHPoolingScrollView 의 `_origin` 으로 이주.

#### 2.7.3 카드 셀 내부

| 요소 | 값 |
|---|---|
| 프레임 (좌측) | 8×40 px 세로 막대, 카테고리 색 |
| 카드 이름 (CHText) | 12pt 흰색, 좌측 상단 |
| ×N (CHText) | 10pt 노랑(`#FBBF24`), 이름 우측 (N≥2 일 때만) |
| 설명 (CHText) | 10pt 회색(`#D1D5DB`), 이름 아래, 1줄 truncate (모달 폭 절반 안에 들어감) |

#### 2.7.4 정렬 순서

**카테고리 그룹화 + 픽 시간 순** 혼합:

- **패시브 섹션 정렬** (위→아래):
  1. Enhance (강화 6장)
  2. Spawn (추가 5장)
  3. Replace (교체 2장)
  4. Environment (환경 2장)
- **액티브 섹션 정렬**: 픽한 시간 순 (`BuildEntry` 의 리스트 추가 순서 그대로)

**근거**: 패시브는 카테고리별로 빌드 방향이 다름(강화/추가/교체/환경) — 카테고리로 묶어야 빌드 형상이 보임. 액티브는 시간 트리거(30초 마다)라 종류 구분보다 시간 순이 자연스러움.

#### 2.7.5 헤더 + 닫기

| 항목 | 값 |
|---|---|
| 타이틀 | `"빌드 상세"` (한국어, CHText 14pt 흰색, 좌측 정렬) |
| X 버튼 (CHButton) | 우상단 (모달 우측 끝 + padding 안쪽), 크기 24×24 px |
| X 버튼 표시 | 글자 `×` (TMP_Text 16pt, `#FFFFFF`, 가운데 정렬) — 신규 아이콘 에셋 금지 (MVP §11.4) |
| 배경 dim 클릭 | 모달 닫힘 (배경 dim Image 에 `CHButton` 부착) |
| 게임 일시정지 결합 | **없음** (MVP §11 외 — 전투 5분 압박 유지) |

#### 2.7.6 빈 상태

- `_vm.Build.Count == 0` (게임 시작 직후) 인 경우: 양 섹션에 `"아직 픽한 카드가 없습니다"` 한 줄 (10pt `#9CA3AF` 회색).

### 2.8 BuildPanel 화면 배치 (v0.7 — 우측 1/3 폭)

**배경**: v0.4~v0.6 까지 BuildPanel 은 폭 240px 의 우측 컬럼 (offset 24/32/120). 사용자가 v0.7 에서 가로 분할 정책을 **"빌드패널은 우측에서 3분의 1"** 로 결정 — 폭을 240 → 427 로 확대하고, 좌측 2/3 의 SpawnerStatusPanel 과 정확히 화면을 가로로 분할.

#### 2.8.1 패널 anchor / 위치 / 크기 (단일 결정 값 v0.7)

| 항목 | 값 | 비고 |
|---|---|---|
| Anchor min | `(1, 0)` | 우하단 (v0.4 유지) |
| Anchor max | `(1, 1)` | 우상단 (우측 세로 stretch, v0.4 유지) |
| Pivot | `(1, 0.5)` | 우측 가운데 (v0.4 유지) |
| 화면 우측 offset | **0px** (offsetMax.x = 0) | 화면 우측 가장자리에 정확히 붙음. v0.6 24px → v0.7 0px (우측 1/3 영역을 가득 사용) |
| 화면 상단 offset | **0px** (offsetMax.y = 0) | 화면 상단 가장자리에 정확히 붙음. v0.6 32px → v0.7 0px |
| 화면 하단 offset | **0px** (offsetMin.y = 0) | 화면 하단 가장자리에 정확히 붙음. v0.6 120px → v0.7 0px — **좌/우 가로 분할로 가로상 겹침이 원천 불가능 (SpawnerStatusPanel x∈(0, 850), BuildPanel x∈(853, 1280))** 하므로 BuildPanel 하단 padding 으로 SpawnerStatusPanel 회피할 필요 없음. v0.6 의 "세로 안전 여백 120px" 추론은 v0.7 좌측 anchor 변경으로 무효화 |
| 패널 폭 | **427px** | 우측 1/3 = 1280/3 = 426.67 → 정수 단정 427 (v0.6 240 → v0.7 427). 좌측 패널 850 + 우측 패널 427 = 1277 → 두 패널 사이 가로 3px 여백 (좌측 패널 우측 끝 850 ↔ BuildPanel 좌측 끝 853) |
| 패널 높이 (결과) | 720 − 0 − 0 = **720px** | 화면 전체 세로 사용 (v0.6 568px → v0.7 720px) |
| 배경 Image | 거의 투명 (alpha 0.001) — raycast 용 (v0.3 그대로) | |
| 루트 `CHButton` | v0.3 §2.6.3 그대로 (Bind 시 `BuildModalPopup` 열기) | |

> **1280×720 기준 시각 검증 (v0.7)**: SpawnerStatusPanel x-range (0, 850), BuildPanel x-range = (1280 − 427, 1280) = (**853, 1280**). 두 패널 사이 가로 여백 = 853 − 850 = **3px** — 화면을 가로로 정확히 2/3 + 1/3 분할. 좌/우 가로 분할로 세로상 두 패널이 서로 침범할 가능성 자체가 없음. v0.6 의 "두 패널 가로 161px 분리 / 세로 16px 안전 여백" 표현은 무효화 — v0.7 에선 가로 3px 만 분리, 세로는 두 패널 모두 화면 전체 세로 사용 (BuildPanel 720 / SpawnerStatusPanel 168).

#### 2.8.2 PassiveSection / ActiveSection 분할 (v0.3 좌우 50:50 → v0.4 상하 50:50)

| 항목 | 값 |
|---|---|
| 분할 방향 | **상하 50:50 세로 분할** (v0.3 의 좌우 50:50 가로 분할 폐기) |
| 상단 = `PassiveSection` | 패시브 카드 (강화 6장 제외 필터 — §2.6.1) |
| 하단 = `ActiveSection` | 액티브 카드 (4장 — Berserk/BloodThirst/Frenzy/IronWill) |
| 섹션 사이 구분선 | 1px `#374151` 가로 |
| 라벨 정렬 | 각 섹션 상단에 좌측 정렬 (기존 `BuildBuildSection.Label` 그대로) |
| Container Layout | **CHPoolingScrollView 가 자동 처리** (v0.8 — Rule 03 §3 완전 적용으로 v0.4~v0.7 의 raw `VerticalLayoutGroup + ContentSizeFitter` 정책 폐기). CHPoolingScrollView 가 `InitItemTransform` (line 770~) 에서 자식 RectTransform 의 anchor/pivot/anchoredPosition 을 직접 설정 — 별도 Layout 컴포넌트 부착하면 충돌하므로 부착 X |
| Layout 파라미터 | §2.8.5 CHPoolingScrollView 직렬화 필드 표 참조 (`_itemSize / _itemGap / _padding / _scrollDirection / _align`) |
| 카드 셀 배치 방향 | 위→아래 (CHPoolingScrollView 의 `_align = LeftOrTop` + `_scrollDirection = Vertical`) |
| 셀 수 초과 시 | CHPoolingScrollView 자동 풀링 + 세로 스크롤 (§2.8.5) |

**근거 (v0.7 폭 확장 반영)**: 우측 컬럼이 폭 427 / 높이 720 으로 v0.6 대비 가로·세로 모두 확장. 상하 50:50 분할 유지 — 각 섹션 폭 427px / 세로 약 359px (720÷2 − 라벨 32 − 분할선 1/2) 영역에서 라벨 32px 제외 후 가용 세로 약 **327px** → 셀 72px + spacing 6px 단위로 **약 4~5 셀** 이 보이고 나머지는 ScrollRect 로 스크롤. 폭이 427 로 넓어졌어도 BuildIconCell 단일 컬럼 VerticalLayoutGroup 유지 — 사용자 권장 (단일 컬럼 유지 / 멀티 컬럼 미도입). 가용 폭 419 ÷ 셀 72 ≈ 5.8 셀이 한 줄에 들어갈 수 있는 폭이지만, 셀이 좌측 정렬로 1개씩만 들어가고 우측에 약 347px 의 여백이 생긴다. 이 여백은 추후 카드 설명·자세히 보기 등의 확장 영역으로 남겨두고 MVP 에선 빈 채로 둔다.

#### 2.8.3 BuildIconCell / 카드 아이콘 표시 (v0.8 — CHPoolingScrollView 완전 적용)

**셀 크기 현실 확인 (v0.7 그대로)**: 현행 `BuildIconCell.prefab` 의 셀 크기 = **72×72** (`Assets/_Lair/Editor/LairUIPrefabBuilder.cs:180` 빌더가 `rootRt.sizeDelta = new Vector2(72f, 72f)` 로 박아둠) — v0.8 에서도 그대로 유지 (사용자 권장 단일 컬럼 유지). v0.7 BuildPanel 한 섹션 가용 영역 = 폭 427 / 세로 (720 − 분할 1px) ÷ 2 − 라벨 32 ≈ **327px** → 수용 한계 = 327 ÷ (72 + spacing 6) ≈ **4.2 → 섹션당 약 4 셀** 가시. 패시브 unique 9장 (강화 6장 제외 시) / 액티브 unique 10장 풀이 한 판에서 모두 픽되어도 CHPoolingScrollView 로 자동 풀링 + 스크롤 처리.

| 항목 | 값 |
|---|---|
| 셀 자체 크기·표시 | **기존 `BuildIconCell.prefab` 그대로 72×72** (v0.3 §2.6 행위 변경만 유지 — Bind 시 onClick null, raycast 차단) |
| 섹션 내부 구조 | **`CHPoolingScrollView<BuildIconCell, BuildEntry>` 파생 컴포넌트** (각 섹션마다 1세트, v0.8 — Rule 03 §3 완전 적용으로 v0.5 의 raw ScrollRect+VLG+CSF 정책 교체) |
| 셀 정렬 | CHPoolingScrollView 의 `_align = LeftOrTop` + `_scrollDirection = Vertical` 로 위→아래 자동 배치 (좌측 정렬 단일 컬럼) |
| 스크롤 동작 | 한 섹션 셀 수가 가용 세로(약 327px) 초과 시 CHPoolingScrollView 자동 풀링 + 세로 스크롤 |
| 셀 ↔ 모달 관계 | 셀 클릭 자체는 비활성 (v0.3 §2.6.2). drag = 스크롤, click = root CHButton → 모달 (Unity EventSystem 자동 분리, §2.6.3 단일 결정) |
| 멀티 컬럼 도입 여부 | **단일 컬럼 유지** (v0.7 — 사용자 권장). 가용 폭 419 ÷ 셀 72 = 5.8 → 한 줄에 5셀 들어갈 폭은 있으나 멀티 컬럼은 별도 작업 영역. MVP 단순화 |

#### 2.8.5 CHPoolingScrollView 구조 단일 결정 값 (v0.8 — Rule 03 §3 완전 적용)

각 섹션(PassiveSection / ActiveSection)의 내부 구성 — 양 섹션이 **동일 구조 1세트씩** 갖는다. v0.5~v0.7 의 raw `ScrollRect + Viewport + Content(VLG+CSF)` 구조를 **`CHPoolingScrollView<BuildIconCell, BuildEntry>` 파생 컴포넌트** 로 교체.

| 컴포넌트 / 직렬화 필드 | 설정 | 비고 |
|---|---|---|
| 각 섹션 GameObject 부착 | `RectTransform + ScrollRect + BuildIconPoolingScrollView (CHPoolingScrollView<BuildIconCell, BuildEntry> 파생)` | `CHPoolingScrollView` 가 `[RequireComponent(typeof(ScrollRect))]` — 함께 부착 자동 |
| `ScrollRect.horizontal / vertical` | `false / true` (CHPoolingScrollView 가 `SetItemList` 호출 시 `_scrollDirection = Vertical` 에 따라 자동) | 명시 빌더 작업 불요 |
| `ScrollRect.MovementType` | **Elastic** (빌더 직접 설정) | 끝에서 살짝 튕김 |
| `ScrollRect.Elasticity` | **0.1** | |
| `ScrollRect.Inertia` | **on** | |
| `ScrollRect.DecelerationRate` | **0.135** | |
| `ScrollRect.ScrollSensitivity` | **1** | |
| Scrollbar | **추가하지 않음** | MVP 단순화 |
| Viewport (자식 — `ScrollRect.viewport` 직렬화) | `RectTransform` 전체 stretch + `RectMask2D` + Image (alpha 0.001, raycastTarget = true) | drag receiver (v0.5 정책 그대로) |
| Content (Viewport 자식 — `ScrollRect.content` 직렬화) | **`RectTransform` 만** — CHPoolingScrollView 가 anchor `(0.5, 1)` / pivot `(0.5, 1)` / sizeDelta 자동 설정 (line 454~) | **`VerticalLayoutGroup / ContentSizeFitter` 부착 X** — CHPoolingScrollView 의 `InitItemTransform` (line 770~) 과 충돌 |
| `BuildIconPoolingScrollView._origin` (CHPoolingScrollView 직렬화) | `BuildIconCell.prefab` 인스턴스 참조 — Content 첫 자식으로 사전 배치 (현행 prefab 그대로 사용) | 기존 prefab 변경 없음 |
| `_itemSize` | **런타임 무시 — dead serialization** (`SetItemSize` 가 origin RectTransform sizeDelta 로 매번 덮어씀). **BuildIconCell prefab sizeDelta = (72, 72) 가 단일 진실 — 표시 값 (72, 72)**. 현행 prefab 그대로 (`LairUIPrefabBuilder.cs:180` 직박이), 빌더 wire-up 불요 (v0.9 P1) | |
| `_itemGap` | `(0, 6)` (v0.5 spacing 6 그대로) | |
| `_padding` | `(4, 4, 4, 4)` (v0.5 padding 그대로) | RectOffset(left, right, top, bottom) |
| `_scrollDirection` | `Vertical` | |
| `_align` | `LeftOrTop` | 위→아래 쌓임 |
| `_columnCount` | **`1` (단일 컬럼 강제, v0.9 BLOCKER B1 단정)** | **auto (`_columnCount <= 0`) 로 두면 `CHPoolingScrollView.SetColumnCount` (CHPoolingScrollView.cs:404-415) 가 viewport 폭 419 ÷ (itemSize 72 + itemGap.x 0) = 5.8 → `FloorToInt = 5` → **멀티 컬럼화 (5열)**. 기획서가 §2.8.3 / §2.8.5 전반에서 단정한 "단일 컬럼 유지" 와 직접 충돌. `_columnCount = 1` 로 명시 단정** |
| `_rowCount / _poolItemCount` | `0 / 0` (자동) | viewport 세로로 자동 산출 (단일 컬럼이라 row 가 곧 셀 수) |

**섹션당 가용 영역 재검산 (v0.7 그대로)**:
- BuildPanel 폭 427, 높이 720
- 섹션 라벨 영역 (상단 좌측 정렬) 32px + 섹션 사이 구분선 1px → 한 섹션 = (720 − 32 − 32 − 1) ÷ 2 ≈ **327.5px** (≈ 328px)
- Viewport 폭 ≈ **419px** (CHPoolingScrollView 가 `_padding.left/right` 4 + 안전 여백을 자동 흡수) → 셀 72px 가 가로로 1개만 들어감 (좌측 정렬 단일 컬럼 유지)
- Viewport 세로 = 328 − 라벨 32 = **296px** → 셀 72 + ItemGap 6 단위로 **약 4 셀** 보임, 나머지는 풀링 스크롤
- 패시브 9장 / 액티브 10장 풀 모두 픽 시 → 5~6 셀 스크롤 영역 자동 처리

**raycast 일관성 (§2.6.3 와 묶임)**: 
- root `CHButton` 은 BuildPanel 루트에 부착 (v0.3 §2.6.3 그대로)
- 각 섹션의 `ScrollRect` (CHPoolingScrollView 의 자식 컴포넌트) 는 자식 영역 — drag 발생 시 `IBeginDragHandler` 로 bubble, click 발생 시 `IPointerClickHandler` 로 bubble. Unity EventSystem 의 표준 동작이라 별도 분기 코드 불필요.
- BuildIconCell 자체는 raycast 차단(v0.3 §2.6.2) — drag/click 모두 통과 → ScrollRect 가 drag 잡고, root CHButton 이 click 잡는다.

**기존 코드 영향 (v0.8 — gameplay-programmer 가 처리)**:
- `BuildPanel.cs` 의 `_passiveContainer` / `_activeContainer` 직렬화 필드 (현재 `Transform`) 의 의미가 변경 — "Content Transform" → "각 섹션 CHPoolingScrollView 파생 인스턴스 참조" (필드 타입 변경 + 변수명 변경 권장: `_passiveScrollView` / `_activeScrollView`).
- 기존 `Refresh()` 본문 (line 53~74) 의 `CHMPool.Pop` 분기 + `_cells` Dict 추적 + `cell.Bind` / `cell.SetCount` 호출 → **`scrollView.SetItemList(filteredEntries)` 단일 호출로 단순화**. CHPoolingScrollView 가 자체로 셀 인스턴스 풀링·재바인딩 처리.
- `BuildIconCellAdapter` 역할 — `CHPoolingScrollView<BuildIconCell, BuildEntry>.InitItem(BuildIconCell, BuildEntry, int)` 가 내부에서 `item.Bind(data.Card, null) + item.SetCount(data.Count)` 호출 (현 BuildIconCell API 그대로 활용).
- `_cells` Dict (line 21) **삭제** — CHPoolingScrollView 가 인스턴스 재바인딩을 자동 처리, 외부 추적 불요.
- `_cellPrefab` 직렬화 필드 (line 16) **삭제** — prefab 참조는 각 CHPoolingScrollView 의 `_origin` 으로 이주.

#### 2.8.6 hit area 변화 (v0.7)

| 버전 | BuildPanel raycast 영역 | 면적 |
|---|---|---|
| v0.3 | 약 1240×150 (화면 하단 가로 전체) | 약 **186K px²** |
| v0.4 / v0.5 / v0.6 | 240×568 (화면 우측 컬럼) | 약 **136K px²** |
| v0.7 | 427×720 (우측 1/3 가로 분할 — 폭 확대 + 세로 풀높이) | 약 **307K px²** |

v0.6 대비 **+126% 확대** (236%). **의도된 변경** — 사용자가 화면 가로 분할 정책 도입으로 빌드 영역을 명확히 우측 1/3 에 할당. 우측 컬럼이 시각적으로 BuildPanel 임을 명확히 드러내고 (배경 frame + 세로 컬럼 자체가 affordance), hit area 가 커져서 클릭 빈도/정확도 모두 개선. BuildPanel 의 비중을 화면 좌우 분할 수준으로 격상하는 결정.

> **빌더 변경 영향 (v0.7 갱신)**: `LairUIPrefabBuilder.BuildBattleHud` 의 BuildPanel 빌드 절 (현재 `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` line 405~412) 의 offsetMin/offsetMax 4 값을 §2.8.1 v0.7 단일 결정 값으로 갱신:
> - 변경 전 (v0.6): `bpRt.offsetMin = (-264, 120)`, `bpRt.offsetMax = (-24, -32)` — 폭 240·상단 32·하단 120·우측 24
> - 변경 후 (v0.7): `bpRt.offsetMin = (-427, 0)`, `bpRt.offsetMax = (0, 0)` — 폭 427·상하 0·우측 0
>
> `BuildBuildSection(bool top)` 헬퍼와 §2.8.5 ScrollRect 구조는 v0.5 그대로 변경 없음 — 섹션 폭이 자동으로 427 로 늘어나고, 가용 세로도 자동으로 327 로 늘어남 (anchor min/max 비율 분할이라 BuildPanel 크기가 바뀌면 자동 따라감).

#### 2.8.4 안전 여백 검산 (1280×720 기준 단일 진실 v0.7)

| 두 UI | 가로/세로 위치 | 여백 |
|---|---|---|
| SpawnerStatusPanel | x (0, 850), y (0, 168) | — (화면 좌측 하단 2/3 영역) |
| BuildPanel | x (853, 1280), y (0, 720) | **가로 3px 분리** (853 − 850 = 3) → 세로 겹침은 원천 불가능 |
| TimerText | 화면 가로 중앙, 상단 (y ≈ 640~720) | TimerText x ≈ (440, 840) — SpawnerStatusPanel x (0, 850) 와 살짝 가로 겹침 가능. 단 TimerText 의 y ≈ 640~720 / SpawnerStatusPanel y (0, 168) 로 **세로 분리** 됨 → 시각 충돌 없음 |
| HeroHpBar | 화면 가로 중앙, 상단 (y ≈ 556~580) | TimerText 동상 — SpawnerStatusPanel 과 세로 분리 |

> **v0.7 정정**: v0.6 의 "두 패널 가로 161px 분리 / 세로 16px 안전 여백" 결정은 v0.7 가로 분할 정책으로 무효화. **v0.7 에선 가로 3px 만 분리, 세로는 두 패널 모두 화면 전체 세로 사용**. 좌측 패널은 좌측 2/3 영역만 (세로는 하단 168), 우측 패널은 우측 1/3 영역 전체 세로 사용. TimerText/HeroHpBar 는 화면 가로 중앙·상단 — SpawnerStatusPanel 의 y (0, 168) 보다 위쪽이라 세로 분리. BuildPanel 의 x (853, 1280) 와도 가로 분리 (TimerText/HeroHpBar 는 중앙). 세로 겹침은 모든 조합에서 발생하지 않음.

> 향후 해상도 변경 시 본 표 재계산. MVP 는 1280×720 단일 기준.

---

## 3. 색·치수 정리표 (한 곳에 응집)

### 3.1 색상 상수

| 용도 | Hex | 비고 |
|---|---|---|
| 진행 바 Fill Cool (0~69%) | `#60A5FA` | 기존 `SpawnerCooldownBar` 그대로 |
| 진행 바 Fill Warm (70~100%) | `#F97316` | 기존 그대로 |
| 진행 바 Background | `#374151` | 기존 그대로 |
| 셀 배경 | `#1F2937` (alpha 0.85) | |
| 셀 테두리 (활성) | `#FBBF24` (2px) | 노랑 강조 |
| 종명 텍스트 | `#FFFFFF` | |
| ×N 노랑 강조 | `#FBBF24` | 색칩·아이콘과 별개 강조색 |
| 툴팁 배경 | `#1F2937` (alpha 0.95) | |
| 툴팁 테두리 | `#FBBF24` (1px) | |
| 모달 배경 | `#1F2937` (alpha 0.98) | |
| 모달 테두리 | `#FBBF24` (2px) | |
| 모달 dim | `#000000` (alpha 0.6) | |
| 섹션 구분선 | `#374151` (1px) | |
| 설명 텍스트 회색 | `#D1D5DB` | |
| 빈 상태 회색 | `#9CA3AF` | |
| 종 6색 | 컨셉 §11.4 매핑 그대로 | (위 §2.4) |

### 3.2 치수 상수 (v0.7)

| 용도 | 값 | v0.6 비교 |
|---|---|---|
| **패널 anchor / pivot** | **(0, 0) / (0, 0) — 좌측 하단** | v0.6 (0.5, 0)/(0.5, 0) → v0.7 좌측 하단 |
| 패널 화면 좌측 offset | **0px** (anchored X = 0) | (v0.7 신규) |
| 패널 화면 하단 offset | **0px** (anchored Y = 0) | v0.6 24px → 0 (가로 분할로 safe area 무용) |
| 패널 좌우 padding | 8px | (유지) |
| 셀 간 간격 | 6px | (유지) |
| 패널 가로 / 세로 | **850 / 168 px** | 430/80 → 850/168 |
| 셀 가로 / 세로 | **134 / 168 px** | 64/80 → 134/168 (×2.094) |
| 셀 내부 padding (좌/우, 상/하) | **12 / 8 px** | 6/4 → 12/8 |
| 색칩 (정사각형) | **30×30 px** | 14×14 → 30×30 |
| 색칩 ↔ 종명 gap | **14px** (※ 본체 영역 좌측부터: padding 12 + 색칩 30 + gap 14 + 종명영역 …) | 6 → 14 |
| **색칩 visibility (v0.5)** | **N=1 만 노출 / N≥2 일시 숨김** (§2.2.2, §2.2.3 폴백 정책) | (유지) |
| 종명 폰트 사이즈 | **22pt** (cap 적용) | 15 → 22 |
| ×N 폰트 사이즈 | **20pt** (cap 적용) | 14 → 20 |
| 아이콘 row 높이 | **42px** | 20 → 42 |
| 아이콘 원 지름 | **30px** | 14 → 30 |
| 아이콘 글자 폰트 | **16pt** (cap 적용) | 11 → 16 |
| 아이콘 ×N 배지 폰트 | **14pt** (cap 적용) | 10 → 14 |
| **IconRow 슬롯 수 (v1.0)** | **2 슬롯** — 좌 Enhance / 우 Spawn 고정 | (v1.0 신규 — v0.9 는 1 슬롯) |
| **슬롯 1 (Enhance) anchoredPosition.x** | **12px** | (v0.x 위치 보존) |
| **슬롯 2 (Spawn) anchoredPosition.x** | **68px** (= 슬롯 1 배지 우측 끝 64 + 슬롯 간 spacing 4) | (v1.0 신규 — 배지 형상 검산 §2.3.1) |
| **슬롯 간 spacing** | **4px** (슬롯 1 **배지** 우측 끝 ↔ 슬롯 2 **아이콘** 좌측 끝) | (v1.0 신규) |
| **배지 형상 (slot 1/2 동일)** | child of IconCircle, anchor (1, 0) / pivot (0, 1) / anchoredPosition (-2, 1) / sizeDelta (24, 14) → **배지 우측 끝이 icon 우측 끝보다 +22px 확장** | (v1.0 명시 — 기존 builder 형상) |
| **슬롯 2 배지 우측 끝** | **120px** (= 68 + 30 + 22) | (v1.0 신규 — Row stretch 폭 134 안에 14px 여유) |
| 진행 바 가로 / 세로 | **110 / 17 px** | 52/8 → 110/17 |
| 본체 row 높이 | **58px** | 28 → 58 |
| 영역 사이 간격 | **8px** | 4 → 8 |
| 툴팁 가로 / 최소 세로 | **201 / 252 px** | 180/56 → 201/252 |
| 툴팁 padding | 8px | (유지) |
| 툴팁 ↔ 셀 gap | 8px | (유지) |
| **툴팁 화면 좌우 clamping** | **유지 — 좌측 4px margin** (§2.5.2 v0.8 검산. `SpawnerStatusTooltip.cs:98-107` 의 좌·우 양방향 분기는 v0.4~v0.7 내내 코드상 그대로 유지 — 제거된 적 없음. v0.7 좌측 anchor 변경 후 셀 0 케이스에서 자동 발동) | (v0.8 — design-reviewer 권장수정 C1 표현 정정) |
| **툴팁 본문 구조 (v0.8)** | **`CHPoolingScrollView<BuffLine, AppliedBuff>` 파생 컴포넌트 1세트** (Rule 03 §3 완전 적용 — v0.7 의 raw ScrollRect 구조 교체) | (v0.8 갱신) |
| **툴팁 헤더 위치** | **CHPoolingScrollView 외부 고정** (스크롤 시에도 보임) | (v0.7 유지) |
| **툴팁 헤더 영역 높이** | **32px** | (v0.7 유지) |
| **툴팁 CHPoolingScrollView RectTransform** | anchor (0,0)~(1,1) / offsetMin (8,8) / offsetMax (-8,-40) → 폭 185 / 세로 204 | (v0.8 단일 진실) |
| **툴팁 본문 가용 세로 (BLOCKER B2 단일 진실)** | **196px** (252 − 헤더 32 − padding×2 16 − gap 8) | (v0.8 명시) |
| **툴팁 BuffLine 한 줄 sizeDelta** | **(185, 24) — BuffLine prefab sizeDelta 가 단일 진실** (`_itemSize` 직렬화는 `SetItemSize` 가 매번 prefab sizeDelta 로 덮어쓰므로 dead) | (v0.9 P1 정정) |
| **툴팁 BuffPoolingScrollView `_itemGap` / `_padding`** | **(0, 4) / (2, 2, 2, 2)** | (v0.8 — v0.7 spacing 4 / padding 2 그대로) |
| **툴팁 BuffLine 폰트 cap** | **IconLetter 12pt · ×N 배지 10pt · 본문 10pt** (셀 cap 정책과 동일 정신, 비례 적용 안 함) | (v0.8 — design-reviewer 권장수정 C3 단정) |
| **BuildPanel 우측 offset** | **0px** | v0.6 24 → 0 (우측 끝 정렬) |
| **BuildPanel 상단 offset** | **0px** | v0.6 32 → 0 (상단 끝 정렬) |
| **BuildPanel 하단 offset** | **0px** | v0.6 120 → 0 (가로 분할로 SpawnerStatusPanel 회피 불요) |
| **BuildPanel 폭 / 높이** | **427 / 720 px** (1280×720 기준 — 우측 1/3 전체) | 240/568 → 427/720 |
| **BuildPanel 섹션 분할** | **상하 50:50 세로** | (v0.4 유지) |
| **BuildPanel 섹션 라벨 영역 높이** | **32px** | (v0.5 유지) |
| **BuildPanel 섹션 가용 세로 (라벨 제외)** | **약 327px** (720 ÷ 2 − 라벨 32 − 분할선 1 / 2) | 219 → 327 |
| **BuildPanel 섹션 Viewport 폭** | **약 419px** (427 − padding 4×2 − 안전 여백) | 228 → 419 |
| **BuildPanel 섹션 구조 (v0.8)** | **`CHPoolingScrollView<BuildIconCell, BuildEntry>` 파생 컴포넌트** (Rule 03 §3 완전 적용 — v0.5 의 raw ScrollRect+VLG+CSF 정책 교체) | (v0.8 갱신) |
| **BuildPanel 섹션 `_itemSize`** | **dead serialization — 표시 값 (72, 72)** (BuildIconCell.prefab sizeDelta = 단일 진실, `SetItemSize` 가 매번 덮어씀) | (v0.9 P1 정정) |
| **BuildPanel 섹션 `_itemGap`** | **(0, 6)** | (v0.5 spacing 6 → v0.8 ItemGap 6 으로 표현 변경, 의미 동일) |
| **BuildPanel 섹션 `_padding`** | **(4, 4, 4, 4)** | (v0.5 padding 4 → v0.8 동일) |
| **BuildPanel 섹션 `_scrollDirection` / `_align`** | **Vertical / LeftOrTop** | (v0.8 신규) |
| **BuildPanel 섹션 마스킹** | **RectMask2D** (Mask + Image 조합 X) | (v0.5 유지) |
| **BuildPanel 섹션 Scrollbar** | **없음** | (v0.5 유지) |
| **BuildPanel BuildIconCell 크기** | **72×72 px** (`LairUIPrefabBuilder.BuildBuildIconCell` 빌더 직박이 — 변경 없음) | (현행 유지) |
| **BuildPanel 멀티 컬럼 도입** | **없음 (단일 컬럼 유지)** — 가용 폭 419 / 셀 72 = 5.8 가능하나 MVP 단순화. **`_columnCount = 1` 직박이 단정 (v0.9 B1)** — auto 시 viewport 419 / itemSize 72 = 5 가 나와 멀티 컬럼화되므로 `BuildIconPoolingScrollView._columnCount = 1` 로 빌더가 명시 설정 | (v0.9 B1 단정) |
| 모달 가로 / 세로 | 640 / 480 px | (유지) |
| 모달 padding | 16px | (유지) |
| 모달 헤더 높이 | 32px | (유지) |
| 모달 X 버튼 크기 | 24×24 px | (유지) |
| 모달 카드 셀 높이 | 56px | (유지) |
| 모달 카드 셀 프레임 | 8×40 px | (유지) |
| **모달 섹션 구조 (v0.8)** | **`CHPoolingScrollView<BuildModalCardCell, BuildEntry>` 파생 컴포넌트** (각 섹션 1세트, Rule 03 §3 완전 적용 — v0.3 의 raw ScrollRect 예외 철회) | (v0.8 갱신) |
| **모달 섹션 `_itemSize`** | **dead serialization — 표시 값 (303, 56)** (BuildModalCardCell.prefab sizeDelta = 단일 진실, `SetItemSize` 가 매번 덮어씀; v0.9 P1 정정). **기존 prefab 280×56 을 (303, 56) 으로 빌더가 갱신 (§4.11)** | |
| **모달 섹션 `_itemGap`** | **(0, 4)** | (v0.7 셀 간 간격 4 → v0.8 동일) |
| **모달 섹션 `_padding`** | **(0, 0, 0, 0)** | (v0.8 신규 — 섹션 자체 padding 은 모달 헤더가 흡수) |

---

## 4. 구현 요청사항 (gameplay-programmer 용)

### 4.1 Enum 추가 (`Assets/_Lair/Scripts/Data/CommonEnum.cs`)

```text
public enum EUI
{
    BattleHud,
    ResultPopup,
    CardSelectionPopup,
    BuildModalPopup,            //# 신규
    SpawnerStatusTooltip,       //# 신규
}
```

**근거**: 둘 다 `CHMUI.ShowUI(EUI.X, arg)` 로 띄우는 단일 인스턴스 popup (Rule 03 §2).

기타 enum(`ECardCategory.Enhance`, `EMonsterStatKind`, `EMonster`) 은 기존 값으로 충분 — 신규 없음.

### 4.2 Interface / 카드 source 추적

#### `Assets/_Lair/Scripts/Battle/CommonInterface.cs` (수정)

기존 `ISpawnerOutputProvider` 에 `OutputCount` + 변경 이벤트를 **함께 추가** (인터페이스 분리 X — 둘 다 출력 상태라는 같은 도메인).

```text
public interface ISpawnerOutputProvider
{
    EMonster CurrentType { get; }
    int OutputCount { get; }                              //# 신규
    event Action<EMonster> OnOutputTypeChanged;
    event Action<int> OnOutputCountChanged;               //# 신규
}
```

#### `Assets/_Lair/Scripts/Card/CommonInterface.cs` (변경 없음)

다음은 **모두 시그니처 변경 X**:
- `ICardEffect.Apply(IBattleContext ctx)` — 25개 효과 클래스 일괄 수정 불필요
- `IBattleContext.RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float)` — 기존 그대로
- 6개 강화 효과 클래스 (`WispHpBoostEffect` 등) 본문 — 기존 그대로

#### Source 추적 방식 — BattleController 내부 카드 스코프 (단일 결정)

**결정**: BattleController 가 `card.Effect.Apply(_ctx)` 호출을 `ApplyCardEffect(card)` 메서드로 래핑하고, 그 메서드 안에서 `_currentCardScope` private 필드로 현재 카드를 잠시 저장한다. `RegisterMonsterTypeBuff` 가 내부에서 이 필드를 source 로 읽는다.

**근거**: source 가 필요한 호출 경로는 `RegisterMonsterTypeBuff` 하나뿐이고, 그 호출 진입점은 `BattleController` 내부의 `card.Effect.Apply(_ctx)` 3개 call site 전부. 인터페이스 / 효과 클래스 25개를 건드리지 않고 BattleController 한 곳만 수정하면 충분하다. ICardEffect 시그니처 확장안과 비교해 변경 영향 범위가 압도적으로 작다.

**BattleController 신규 메서드 (스케치 — 구체 구현은 gameplay-programmer)**:

```text
private CardData _currentCardScope;

//# 카드 효과 적용의 단일 진입점. 3개 기존 호출지점을 이 메서드로 치환.
public void ApplyCardEffect(CardData card)
{
    if (card?.Effect == null || _ctx == null) return;
    _currentCardScope = card;
    try { card.Effect.Apply(_ctx); }
    finally { _currentCardScope = null; }
}

//# 기존 RegisterMonsterTypeBuff 시그니처 유지. 내부에서 _currentCardScope 를 source 로 사용.
public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
{
    //# ... 기존 dict 곱연산 / 소급 적용 로직 그대로 ...

    //# 신규 — 카드 픽 추적. _currentCardScope 가 null 이면 (시뮬레이션 외 직접 호출 등)
    //# 추적 데이터 갱신만 생략하고 누적 곱연산은 정상 수행.
    if (_currentCardScope != null)
    {
        TrackCardPick(type, stat, _currentCardScope);   //# §4.3
    }

    OnTypeModifierChanged?.Invoke(type);
}
```

**3개 call site 치환** (`BattleController.cs` 내부 — line 365 / 387 / 477):

```text
//# Before
if (picked.Effect != null && _ctx != null) picked.Effect.Apply(_ctx);

//# After
ApplyCardEffect(picked);
```

영향 범위 (확정):
- `BattleController.cs` — `_currentCardScope` 필드 + `ApplyCardEffect` 메서드 + 3개 call site 치환 + `RegisterMonsterTypeBuff` 내부에 추적 한 줄
- `BattleContext.cs` — **변경 없음** (위임 그대로)
- 6개 강화 효과 클래스 — **변경 없음**
- 19개 비강화 효과 클래스 — **변경 없음** (source 안 씀)
- `ICardEffect` / `IBattleContext` — **변경 없음**

### 4.3 데이터 노출 (`BattleController`) — v1.0: Spawn 픽 추적 추가

기존 (v0.9) `_typeModifierPicks` 가 Enhance 픽만 추적했다. **v1.0 은 동일 자료구조 (`Dictionary<EMonster, List<AppliedBuff>>`) 에 Spawn 픽도 함께 누적**한다 (Source.Category 만으로 자연 구분 가능 — 데이터 모델 확장 불요).

신규 필드 + 이벤트 (변경 없음 — v0.9 그대로):

```text
//# 종별 적용된 카드 픽 누적. Enhance + Spawn 카테고리 모두 같은 list 에 누적 (v1.0).
//# 분기는 BuffLine.FormatBody / SpawnerStatusCell.RebindIconRow 에서 Source.Category 로 처리.
private Dictionary<EMonster, List<AppliedBuff>> _typeModifierPicks = new();
public event Action<EMonster> OnTypeModifierChanged;
```

**Enhance 픽 추적 (기존 v0.9 그대로)** — `RegisterMonsterTypeBuff` 내부에서 `_currentCardScope` non-null 일 때 `TrackCardPick` 호출:

```text
private void TrackCardPick(EMonster type, EMonsterStatKind stat, CardData source)
{
    if (_typeModifierPicks.TryGetValue(type, out var list) == false)
        _typeModifierPicks[type] = list = new();

    var existing = list.Find(b => b.Source == source);
    if (existing != null) existing.PickCount++;
    else list.Add(new AppliedBuff { Source = source, PickCount = 1, Stat = stat, AggregateMultiplier = 1f });

    //# 동일 종·동일 Stat 의 누적 배율 일괄 갱신 (Enhance 만 — Spawn 은 Multiplier 무의미).
    if (_typeModifiers.TryGetValue(type, out var m))
    {
        foreach (var b in list)
            if (b.Stat == stat && b.Source != null && b.Source.Category == ECardCategory.Enhance)
                b.AggregateMultiplier = m.Get(stat);
    }
}
```

> **v0.9 → v1.0 미세 변경**: `AggregateMultiplier` 일괄 갱신 루프에 `Category == Enhance` 필터 추가. 같은 list 에 들어가는 Spawn 엔트리의 AggregateMultiplier 가 잘못 덮어쓰이지 않도록 방어.

**Spawn 픽 추적 (v1.0 신규)** — `IncrementSpawnerOutput` 내부에서 `_currentCardScope` non-null 일 때 `TrackSpawnPick` 호출:

```text
//# 지속 스폰 — 추가소환 카드. 출력 마릿수 +1 + 픽 추적 (v1.0 신규).
public void IncrementSpawnerOutput(EMonster type)
{
    if (_spawners == null) return;
    foreach (var sp in _spawners)
        if (sp != null && sp.CurrentType == type) sp.IncrementOutput();

    //# v1.0 — 픽 추적. type 을 출력하는 Spawner 가 0 대여도 픽은 누적 (retroactive 정책 §2.3.6).
    if (_currentCardScope != null)
        TrackSpawnPick(type, _currentCardScope);

    //# v1.0 — Spawn 슬롯 아이콘 row 갱신을 위해 동일 이벤트 발행.
    //# OnTypeModifierChanged 한 이벤트가 Enhance + Spawn 양 슬롯 재구성 트리거 (§2.3.4).
    OnTypeModifierChanged?.Invoke(type);
}

//# v1.0 신규 — Spawn 카테고리 픽 누적. Enhance 의 TrackCardPick 와 자료구조 공유.
//# Stat 필드는 EMonsterStatKind.Hp (default, BuffLine.FormatBody 에서 Category 분기로 읽히지 않음).
private void TrackSpawnPick(EMonster type, CardData source)
{
    if (_typeModifierPicks.TryGetValue(type, out var list) == false)
        _typeModifierPicks[type] = list = new();

    var existing = list.Find(b => b.Source == source);
    if (existing != null)
        existing.PickCount++;
    else
        list.Add(new AppliedBuff
        {
            Source = source,
            PickCount = 1,
            Stat = EMonsterStatKind.Hp,    //# default, unused — Category 분기로 읽히지 않음
            AggregateMultiplier = 1f,      //# unused for Spawn
        });
}
```

**`OnTypeModifierChanged` 의미 확장 (v1.0)**: v0.9 까지 "Enhance 누적 갱신" 이벤트였으나, v1.0 부터 **"종 X 의 카드 픽 (Enhance 또는 Spawn) 이 갱신됐다"** 의미로 확장. 이벤트 발행은 두 경로 모두에서:
- `RegisterMonsterTypeBuff` 내부 (Enhance 픽) — 기존 그대로
- `IncrementSpawnerOutput` 내부 (Spawn 픽) — v1.0 신규

VM 측 구독은 변경 없음 — 동일 이벤트 핸들러가 출력 종 일치 셀들의 양 슬롯 재계산을 트리거.

**읽기 전용 노출 (v0.9 그대로)**:

```text
public IReadOnlyList<AppliedBuff> GetAppliedBuffs(EMonster type)
    => _typeModifierPicks.TryGetValue(type, out var list)
        ? (IReadOnlyList<AppliedBuff>)list
        : System.Array.Empty<AppliedBuff>();
```

**Retroactive 추적 보존 (§2.3.6 — v1.0 락 결정)**: `type` 을 출력하는 Spawner 가 0 대일 때도 `TrackSpawnPick` 은 호출되어 픽이 누적된다. 이후 Replace 카드로 해당 종을 출력하는 Spawner 가 생기면 그 셀의 Spawn 슬롯이 retroactive 표시. Enhance 의 `_typeModifiers` 글로벌 dict 정책과 의미론 일치.

신규 데이터 구조 (`AppliedBuff` — **`BattleViewModel.cs` 안에 같이 정의**, `Lair.UI` namespace. Rule 02 §9 의 "동일 도메인 단일 파일" 정신 + 기존 `BuildEntry` 가 이미 같은 파일에 있어 일관성 유지):

```text
public class AppliedBuff
{
    public CardData Source;                  //# 어느 카드인지 (Wisp~Phantom 강화 6장 중 1)
    public int PickCount;                    //# 중첩 픽 횟수 (×N 배지 출처)
    public EMonsterStatKind Stat;            //# 어느 스탯
    public float AggregateMultiplier;        //# 곱연산 누적 결과 (툴팁 ×배율 표시 출처)
}
```

VM 에 6 Spawner + BattleController 참조 주입 (기존 hook 패턴):

```text
//# BattleController 초기화 시 VM 에 주입
vm.AttachSpawners(_spawners, this);   //# this == BattleController, OnTypeModifierChanged hook
```

### 4.4 ViewModel (`BattleViewModel.cs`)

신규 데이터:

```text
public class SpawnerSnapshot
{
    public int Index;                       //# 0~5 ring 인덱스
    public EMonster CurrentType;
    public int OutputCount;
    //# Progress 는 VM 캐시 X — View 가 매 프레임 ISpawnerProgress 폴링.
    //# Progress 폴링용 참조는 SpawnerSnapshot 에 넣지 않는다 (스냅샷은 이벤트 영역 전용).
    //# Cell 이 ISpawnerProgress 참조를 어떻게 얻는지는 §4.6 Panel 책임 참고.
    public IReadOnlyList<AppliedBuff> AppliedBuffs;
}

public IReadOnlyList<SpawnerSnapshot> Spawners { get; }   //# 6개 캐시
public event Action<int> OnSpawnerSnapshotChanged;        //# 인덱스 단독 리프레시

//# AttachSpawners(spawners, controller) 메서드:
//#   - 각 Spawner 의 OnOutputTypeChanged / OnOutputCountChanged 구독 → 해당 인덱스 갱신
//#   - controller.OnTypeModifierChanged 구독 → 출력 종 일치하는 모든 인덱스 갱신
//#   - 초기 스냅샷 6개 채움
```

`AppliedBuffs` 는 `controller._typeModifierPicks[snapshot.CurrentType]` 의 read-only view. 출력 종이 바뀌면 새 종 기준 buffs 로 교체.

### 4.5 Spawner 수정 (`Assets/_Lair/Scripts/Battle/Spawner.cs`)

```text
public int OutputCount => _outputCount;
public event Action<int> OnOutputCountChanged;

//# IncrementOutput 호출 시 broadcast.
public void IncrementOutput()
{
    _outputCount++;
    OnOutputCountChanged?.Invoke(_outputCount);
}
```

**초기값 broadcast 정책 (단일 출처)**: `Spawner.OnEnable` 에서 `OnOutputCountChanged` 를 발행하지 **않는다**. 초기 스냅샷은 `BattleViewModel.AttachSpawners` 가 6개 Spawner 의 현재 `OutputCount` 값을 직접 읽어 채운다 (§4.4). OnEnable 시점에 VM 이 아직 구독을 안 걸었을 가능성이 있어 broadcast 가 무의미하고, AttachSpawners 가 직접 스냅샷 채우는 쪽이 단일 출처로 깔끔하다.

> 이후 `IncrementOutput` 으로 인한 변화만 이벤트 broadcast 로 처리 — 초기 채움(VM 직접 폴링) vs 갱신(이벤트) 가 명확히 분리된다.

### 4.6 신규 .cs 파일

| 파일 | 역할 | UIArg 페어 (Rule 03 §5) |
|---|---|---|
| `Assets/_Lair/Scripts/UI/SpawnerStatusPanel.cs` | 6셀 컨테이너, VM 구독, 셀 풀링 `CHMPool`. **셀 생성·바인딩 시 해당 인덱스의 `ISpawnerProgress` 참조도 함께 cell 에 주입** — cell 의 매 프레임 Progress 폴링은 이 참조로 이뤄짐 (스냅샷에는 안 들어감, 폴링 영역과 이벤트 영역 분리). | (없음 — `MonoBehaviour` 직접) |
| `Assets/_Lair/Scripts/UI/SpawnerStatusCell.cs` | 1셀 — 색칩·종명·×N·진행바·**2 슬롯 아이콘 row (v1.0)**. **`Bind(SpawnerSnapshot, ISpawnerProgress)` 시그니처로 받아, Progress 는 매 프레임 폴링, 스냅샷 필드는 이벤트 수신 시 교체.** **색칩 visibility 는 `RebindSnapshot` 안에서 `_colorChip.gameObject.SetActive(snapshot.OutputCount < 2)` 로 토글 — N≥2 일 때 숨김 (§2.2.2 v0.5).** **v1.0 신규** — IconRow 자식 구조를 단일 슬롯 → 2 슬롯 (좌 Enhance / 우 Spawn) 로 확장: 직렬화 필드 `Image _iconCircleEnhance / CHText _iconLetterEnhance / CHText _iconBadgeEnhance` (기존 `_iconCircle / _iconLetter / _iconBadge` 의 변수명 변경) + 신규 직렬화 필드 `Image _iconCircleSpawn / CHText _iconLetterSpawn / CHText _iconBadgeSpawn`. `RebindIconRow` 가 `snapshot.AppliedBuffs` 를 순회하면서 `Source.Category == ECardCategory.Enhance` 면 Enhance 슬롯, `== ECardCategory.Spawn` 면 Spawn 슬롯에 바인딩. **Hex 종의 Spawn 슬롯은 `IconLetterFor` 반환 글자 ` ` 으로 자연 비활성**. `IconLetterFor` 에 SpawnWisps/SpawnWraith/SpawnReapers/SpawnPlagues/SpawnPhantoms 5 매핑 추가 (§2.3.3 v1.0). | (없음) |
| `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs` | `UIBase` 파생, `CHMUI.ShowUI(EUI.SpawnerStatusTooltip, …)` | `SpawnerStatusTooltipArg : UIArg { int SpawnerIndex; BattleViewModel ViewModel; RectTransform AnchorCell; }` 같은 파일 상단 |
| `Assets/_Lair/Scripts/UI/BuildModalPopup.cs` | `UIBase` 파생 | `BuildModalPopupArg : UIArg { BattleViewModel ViewModel; }` 같은 파일 상단 |
| **`Assets/_Lair/Scripts/UI/BuildModalCardCell.cs`** (v0.8 신규) | 현재 `BuildModalPopup.cs:118` 안 nested 정의를 **별도 파일로 분리**. `CHPoolingScrollView<BuildModalCardCell, BuildEntry>` 의 `TItem`. 기존 `Bind(CardData, int)` 시그니처 유지. namespace `Lair.UI`. 자식 직렬화 필드 그대로 (`_frame Image / _nameText / _countText / _descText CHText`). | (없음 — 단순 MonoBehaviour) |
| **`Assets/_Lair/Scripts/UI/BuffLine.cs`** (v0.8 신규, v1.0 갱신) | `CHPoolingScrollView<BuffLine, AppliedBuff>` 의 `TItem`. 자식 — `IconLetterRoot Image(원형) + IconLetter CHText 12pt + Badge CHText 10pt + Body CHText 10pt`. **`Bind(AppliedBuff buff, EMonster type, BalanceConfig balance)` 메서드** — 현 `SpawnerStatusTooltip.FormatBuffLine` (현 .cs line 149~205) 의 스탯별 분기 로직을 BuffLine 안으로 **이주** (한 줄 한 줄을 BuffLine 자기책임). namespace `Lair.UI`. **v1.0 추가** — `FormatBody` 가 `buff.Source.Category == ECardCategory.Spawn` 분기를 `switch (buff.Stat)` 보다 먼저 처리하여 Spawn 단일 포맷 `동시 출력 +{PickCount}` 반환 (§2.5.5 v1.0). `IconLetterFor` (SpawnerStatusCell 의 static 함수) 가 SpawnX 5 카드의 매핑 (글자 `+` + 종 색 배경) 을 함께 반환하므로 IconLetter / 배경색 wire-up 은 변경 없이 동작. | (없음 — 단순 MonoBehaviour) |
| **`Assets/_Lair/Scripts/UI/BuildIconPoolingScrollView.cs`** (v0.8 신규) | `CHPoolingScrollView<BuildIconCell, BuildEntry>` 파생 클래스. `InitItem(item, data, index)` 안에서 `item.Bind(data.Card, null) + item.SetCount(data.Count)` 호출. `InitPoolingObject(item)` 빈 구현. namespace `Lair.UI`. | (없음) |
| **`Assets/_Lair/Scripts/UI/BuildModalCardPoolingScrollView.cs`** (v0.8 신규) | `CHPoolingScrollView<BuildModalCardCell, BuildEntry>` 파생 클래스. `InitItem(item, data, index)` 안에서 `item.Bind(data.Card, data.Count)` 호출. `InitPoolingObject(item)` 빈 구현. namespace `Lair.UI`. | (없음) |
| **`Assets/_Lair/Scripts/UI/BuffPoolingScrollView.cs`** (v0.8 신규) | `CHPoolingScrollView<BuffLine, AppliedBuff>` 파생 클래스. `InitItem(item, data, index)` 안에서 `item.Bind(data, _currentType, _balance)` 호출 — `_currentType / _balance` 는 `SpawnerStatusTooltip` 이 `SetItemList` 호출 직전 setter 로 주입 (`SetContext(EMonster, BalanceConfig)`). `InitPoolingObject(item)` 빈 구현. namespace `Lair.UI`. | (없음) |

**파일 분할 사유 (v0.8)**: CHPoolingScrollView 파생 클래스는 `abstract` 의 `InitItem` / `InitPoolingObject` 두 추상 메서드를 구현해야 하므로 별도 .cs 파일이 자연스럽다 (한 화면에 흐름이 잡힘). BuffLine / BuildModalCardCell 도 prefab 의 component 참조 대상이므로 별도 클래스로 분리.

### 4.7 수정 .cs 파일

| 파일 | 변경 |
|---|---|
| `Assets/_Lair/Scripts/UI/BattleHud.cs` | `SpawnerStatusPanel _spawnerStatusPanel` 직렬화 필드 + `Bind` 추가 |
| `Assets/_Lair/Scripts/UI/BuildPanel.cs` | §2.6 — `Category==Enhance && IsPassive` 필터, `_detailRoot`/`ShowDetail` 제거, 루트 `CHButton` 추가. **v0.8 추가** — `_passiveContainer` / `_activeContainer` `Transform` 직렬화 필드를 `BuildIconPoolingScrollView _passiveScrollView` / `_activeScrollView` 로 교체. `_cells` Dict + `_cellPrefab` 직렬화 필드 + `CHMPool.Pop` 분기 제거. `Refresh()` 본문은 `vm.Build` 를 필터·분할해 `_passiveScrollView.SetItemList(passive) + _activeScrollView.SetItemList(active)` 단일 호출로 단순화 |
| `Assets/_Lair/Scripts/UI/BuildModalPopup.cs` | **v0.8 추가** — `_passiveContent` / `_activeContent` `Transform` 직렬화 필드를 `BuildModalCardPoolingScrollView _passiveScrollView` / `_activeScrollView` 로 교체. `_spawnedCells` List + `Close` 의 `CHMPool.Push` 루프 + `_cellPrefab` 직렬화 필드 + `FillSection` 의 `CHMPool.Pop` 분기 제거. `FillSection(BuildModalCardPoolingScrollView, List<BuildEntry>)` 시그니처로 변경, 본문 `scrollView.SetItemList(entries)` 단일 호출. **nested `BuildModalCardCell` 클래스 정의를 `BuildModalCardCell.cs` 신규 파일로 이주** (§4.6) |
| `Assets/_Lair/Scripts/UI/SpawnerStatusTooltip.cs` | **v0.8 추가** — `_buffText CHText` 직렬화 필드 (line 32) 를 `_buffScrollView BuffPoolingScrollView` 로 교체. `FormatBuffLine` 메서드 (line 149~205) 를 `BuffLine.cs` 로 **이주**. `RefreshContent` 본문 (line 109~145) 의 한 줄 텍스트 setter 분기를 `_buffScrollView.SetContext(snap.CurrentType, _arg.Balance); _buffScrollView.SetItemList(snap.AppliedBuffs.ToList())` 단일 호출로 단순화. "적용된 강화 없음" 빈 상태는 buffs.Count==0 일 때 `_buffScrollView.SetItemList(empty)` + 별도 `_emptyText CHText` 직렬화 필드 (CHPoolingScrollView 외부) 의 `SetActive(true)` 토글 |
| `Assets/_Lair/Scripts/UI/BattleViewModel.cs` | §4.4 — SpawnerSnapshot · OnSpawnerSnapshotChanged · AttachSpawners |
| `Assets/_Lair/Scripts/Battle/Spawner.cs` | §4.5 — OutputCount + OnOutputCountChanged |
| `Assets/_Lair/Scripts/Battle/CommonInterface.cs` | §4.2 — ISpawnerOutputProvider 확장 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | §4.2 — `_currentCardScope` + `ApplyCardEffect(card)` + 3개 call site 치환 + §4.3 `_typeModifierPicks` · `TrackCardPick` · `OnTypeModifierChanged` · `GetAppliedBuffs`. **v1.0 추가** — `IncrementSpawnerOutput(type)` 내부에 `_currentCardScope != null` 일 때 `TrackSpawnPick(type, _currentCardScope)` + `OnTypeModifierChanged?.Invoke(type)` 호출 (§4.3 v1.0). `TrackSpawnPick` 메서드 신규. `TrackCardPick` 의 `AggregateMultiplier` 일괄 갱신 루프에 `Category == Enhance` 필터 추가. |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | §4.1 — EUI 두 값 추가 |

**변경 없음 (명시)**: `BattleContext.cs`, `Card/CommonInterface.cs`, 6개 `*Boost*Effect.cs`, 19개 비강화 효과 클래스 — 모두 기존 시그니처 / 본문 그대로 유지 (§4.2 결정으로 인터페이스 / 효과 클래스 불변화).

### 4.8 삭제 .cs 파일 (스펙 §5.3)

- `Assets/_Lair/Scripts/Battle/SpawnerCooldownBar.cs`
- `Assets/_Lair/Tests/EditMode/Battle/SpawnerCooldownBarTests.cs`

### 4.9 에셋 경로 (Rule 04 §2)

| 프리팹 | 경로 | Addressable 등록 |
|---|---|---|
| `SpawnerStatusPanel.prefab` | `Assets/_Lair/Art/UI/SpawnerStatusPanel.prefab` | 불필요 (BattleHud 자식으로 nested) |
| `SpawnerStatusCell.prefab` | `Assets/_Lair/Art/UI/SpawnerStatusCell.prefab` | 불필요 (직렬화 필드로 panel 이 보유, `CHMPool` 풀링 — `BuildIconCell` 선례 그대로) |
| `SpawnerStatusTooltip.prefab` | `Assets/_Lair/Art/UI/SpawnerStatusTooltip.prefab` | **필요** — `EUI.SpawnerStatusTooltip` 키로 `CHMUI` 등록 |
| `BuildModalPopup.prefab` | `Assets/_Lair/Art/UI/BuildModalPopup.prefab` | **필요** — `EUI.BuildModalPopup` 키로 `CHMUI` 등록 |
| **`BuildModalCardCell.prefab`** (v0.8 — 파일 이미 존재) | `Assets/_Lair/Art/UI/BuildModalCardCell.prefab` | 불필요 (각 모달 섹션 CHPoolingScrollView 의 `_origin` 으로 nested, Content 첫 자식) — **gameplay-programmer 가 `BuildModalCardCell.cs` 부착 + `_frame / _nameText / _countText / _descText` 직렬화 wire-up 필요** |
| **`BuffLine.prefab`** (v0.8 신규) | `Assets/_Lair/Art/UI/BuffLine.prefab` | 불필요 (SpawnerStatusTooltip 의 BuffPoolingScrollView `_origin` 으로 nested) — **빌더가 IconLetterRoot + IconLetter CHText + Badge CHText + Body CHText 자식 구성 + sizeDelta (185, 24)** |

신규 머티리얼 / 스프라이트 없음. 아이콘 원 = Unity 기본 `UISprite` (혹은 흰 원형 sprite) + `Image.color` 인라인. 셀 배경 = 흰 사각형 + `Image.color`. BuffLine 의 IconLetterRoot 원형 Image 도 동일 흰 원형 sprite 재사용.

### 4.10 SO 스키마

**신규 SO 없음.** 본 기획서 수치는 전부 컴포넌트 직렬화 필드 또는 상수.

색·치수 상수는 `SpawnerStatusCell` / `SpawnerStatusPanel` / `SpawnerStatusTooltip` / `BuildModalPopup` 의 내부 상수 또는 직렬화 필드로 인라인. 추후 튜닝 빈도가 높아지면 `BalanceConfig` 또는 신규 `UIConfig.asset` 으로 흡수 검토 (MVP 외).

### 4.11 마이그레이션

- `Battle.unity` 6 Spawner 오브젝트에서 `CooldownBarWrapper` World-space Canvas 자식 제거.
- `SpawnerBody` 자식은 유지 (디스크 색상 틴트).
- 에디터 빌더 — 기존 `LairSpawnerVisualBuilder` (있다면) 의 진행 바 빌드 스텝 제거, 디스크 본체 + 머티리얼 스텝만 남김.
- 신규 빌더 메뉴 — **`Lair/Setup/Spawner Status UI`** 에 4개 신규 프리팹(SpawnerStatusPanel / SpawnerStatusCell / SpawnerStatusTooltip / BuildModalPopup) 빌드 스텝 추가. 기존 `LairSpawnerVisualBuilder` 빌더 명명 컨벤션 (`Lair/Setup/...`) 을 따라 본 기획서가 단일 결정.
- (v0.4~v0.6 의 BuildPanel anchor / `BuildBuildSection` 시그니처 / ScrollRect 구조 빌드 작업은 모두 v0.7 에서도 유효 — 누적 결정 그대로 적용)
- **v0.7 추가 — `LairSpawnerStatusUIBuilder.cs` 갱신**:
  - line 26~27 셀 치수 상수 갱신: `CellWidth = 134f` (v0.6 64f), `CellHeight = 168f` (v0.6 80f).
  - `BuildPanelPrefab` (line 278~) 의 anchor / pivot / anchoredPosition / sizeDelta 4 값 갱신:
    - `rootRt.anchorMin = (0, 0)`, `rootRt.anchorMax = (0, 0)`, `rootRt.pivot = (0, 0)` (v0.6 모두 (0.5, 0))
    - `rootRt.anchoredPosition = (0, 0)` (v0.6 (0, 24))
    - `rootRt.sizeDelta = (850, 168)` (v0.6 (430, 80))
  - `BuildCellPrefab` (line 83~) 의 모든 내부 위치 / 크기 / 폰트 상수를 §3.2 v0.7 컬럼에 맞춰 갱신:
    - 셀 sizeDelta 134×168
    - 색칩 sizeDelta 30×30 / anchoredPosition x = 12 (좌측 padding)
    - 종명 폰트 22pt / offsetMin (56, 0) = padding 12 + 색칩 30 + gap 14
    - ×N 폰트 20pt
    - 아이콘 row sizeDelta y = 42 / anchoredPosition y = -8 (상단 padding 8)
    - 본체 row sizeDelta y = 58 / anchoredPosition y = -58 (상단 padding 8 + 아이콘 row 42 + 영역 간격 8 = 58)
    - 진행 바 sizeDelta (-24, 17) / anchoredPosition y = 19 (잔여 padding 19 그러나 budget 검산 §2.2.1 의 잔여 19 와 일치)
    - **아이콘 row 자식 — 2 슬롯 구성 (v1.0)**:
      - **슬롯 1 (Enhance)**: 아이콘 원 sizeDelta 30×30 / anchoredPosition x = **12** + 아이콘 글자 16pt + ×N 배지 14pt / sizeDelta (24, 14) — 배지 RectTransform 형상 유지 (anchor (1,0) / pivot (0,1) / anchoredPosition (-2,1)) 라 배지 우측 끝이 icon 우측 끝 +22px = **64**
      - **슬롯 2 (Spawn, v1.0 신규)**: 아이콘 원 sizeDelta 30×30 / anchoredPosition x = **68** (= 슬롯 1 배지 우측 끝 64 + 슬롯 간 spacing 4 — §2.3.1 v1.0 배지 형상 검산) + 아이콘 글자 16pt (`+` 표시) + ×N 배지 14pt / sizeDelta (24, 14, 동일 형상). 슬롯 2 배지 우측 끝 = 68+30+22 = **120**, Row stretch 폭 134 안에 14px 여유
    - **SpawnerStatusCell SerializedObject wire-up (v1.0)** — `_iconCircle / _iconLetter / _iconBadge` 단일 셋 → `_iconCircleEnhance / _iconLetterEnhance / _iconBadgeEnhance` (변수명 변경) + `_iconCircleSpawn / _iconLetterSpawn / _iconBadgeSpawn` (신규) 6 직렬화 필드 wire-up
- **v0.8 갱신 — `LairSpawnerStatusUIBuilder.BuildTooltipPrefab` (line 317~)** (v0.7 의 raw ScrollRect 구조를 CHPoolingScrollView 로 교체):
  - Body sizeDelta (201, 252) (v0.6 (180, 90)) — v0.7 결정 그대로
  - 기존 BuffText 단일 GameObject (line 365~) 를 **CHPoolingScrollView 구조 1세트** 로 치환:
    ```
    BuffScrollView (RectTransform + ScrollRect[MovementType=Elastic, Elasticity=0.1, Inertia=on, no Scrollbar]
                                  + BuffPoolingScrollView)
     └ BuffViewport (RectTransform 전체 stretch + RectMask2D + Image[alpha 0.001, raycastTarget=true])
         └ BuffContent (RectTransform 만 — VLG/CSF 부착 X. CHPoolingScrollView 가 anchor (0.5,1)/pivot (0.5,1)/sizeDelta 자동 설정)
              └ BuffLine origin (BuffLine.prefab 인스턴스, Content 첫 자식) — Start 시 SetActive(false) 자동
    ```
  - BuffScrollView RectTransform : anchorMin (0, 0) / anchorMax (1, 1) / offsetMin (8, 8) / offsetMax (-8, -40) (헤더 32 + gap 8 만큼 상단 offset) — v0.7 결정 그대로
  - BuffPoolingScrollView 직렬화 필드 빌더 설정 (v0.9 — `_itemSize` 제외, dead serialization P1): `_origin = BuffLine prefab`, `_itemGap = (0, 4)`, `_padding = (2, 2, 2, 2)`, `_scrollDirection = Vertical`, `_align = LeftOrTop`, `_rowCount/_columnCount/_poolItemCount = 0` (auto — viewport 폭 185 ÷ itemSize 185 = 1 자동, BuildPanel 같은 5열 위험 없음). **BuffLine prefab sizeDelta = (185, 24) 가 단일 진실** — `BuildBuffLinePrefab` 헬퍼가 박음 (§4.11 아래 항목)
  - ScrollRect 의 `viewport / content` 직렬화 필드: 각각 BuffViewport / BuffContent RectTransform 참조 wire-up
  - Header 영역 sizeDelta y = 24 그대로 유지 (헤더는 BuffScrollView 외부 고정) — v0.7 결정 그대로
  - `SpawnerStatusTooltip.cs` 의 직렬화 필드 — **`_buffText` 제거**, **`_buffScrollView BuffPoolingScrollView` 신규**, **`_emptyText CHText` 신규** (빈 상태 "적용된 강화 없음" 표시용, BuffScrollView 외부 고정 자식)
- **v0.8 추가 — `BuildBuffLinePrefab` (`LairSpawnerStatusUIBuilder` 신규 헬퍼)**:
  - Root: GameObject + RectTransform + BuffLine (MonoBehaviour). sizeDelta = (185, 24)
  - 자식 1 — IconLetterRoot: GameObject + RectTransform + Image (원형 sprite, color = 종 6색 매핑 자체는 Bind 시 결정). sizeDelta = (16, 16), anchoredPosition x = 2 (좌측 margin)
  - 자식 1.1 — IconLetter: GameObject + RectTransform + TMP_Text + CHText (12pt, color = 종 6색 매핑 자체는 Bind 시 결정). 부모 stretch
  - 자식 2 — Badge: GameObject + RectTransform + TMP_Text + CHText (10pt, color = `#FBBF24` 노랑 + 1px outline). sizeDelta = (24, 12), anchoredPosition x = 20 (IconLetterRoot 우측)
  - 자식 3 — Body: GameObject + RectTransform + TMP_Text + CHText (10pt, color = 흰색). anchorMin (0, 0)/anchorMax (1, 1) stretch, offsetMin (44, 0)/offsetMax (-2, 0) — 좌측 IconLetterRoot+Badge 영역 44px 제외 후 stretch
- **v0.8 갱신 — `LairUIPrefabBuilder.cs` BuildPanel 섹션** (v0.7 의 raw ScrollRect 구조를 CHPoolingScrollView 로 교체):
  - line 408~412 `BuildPanel` RectTransform offset 4 값 갱신 (v0.7 결정 그대로):
    - `bpRt.offsetMin = (-427, 0)` (v0.6 (-264, 120))
    - `bpRt.offsetMax = (0, 0)` (v0.6 (-24, -32))
    - anchor min/max / pivot 은 v0.4 결정 (1, 0)~(1, 1) / (1, 0.5) 유지.
  - `BuildBuildSection(bool top)` 헬퍼 (v0.4 시그니처) 본문을 **CHPoolingScrollView 구조 1세트** 로 치환:
    ```
    Section GameObject (anchor 비율로 상하 50:50 분할 — v0.4 결정 그대로)
     ├ Label (CHText 라벨 영역 32px — v0.5 결정 그대로)
     └ ScrollView (RectTransform + ScrollRect[Elastic 0.1, Inertia on, no Scrollbar]
                                 + BuildIconPoolingScrollView)
        └ Viewport (RectTransform 전체 stretch + RectMask2D + Image[alpha 0.001, raycastTarget=true])
            └ Content (RectTransform 만 — VLG/CSF 부착 X)
                 └ BuildIconCell origin (BuildIconCell.prefab 인스턴스, Content 첫 자식)
    ```
  - BuildIconPoolingScrollView 직렬화 필드 빌더 설정 (v0.9 — `_itemSize` 제외, dead P1 / `_columnCount = 1` 명시, B1): `_origin = BuildIconCell prefab` (`Assets/_Lair/Art/UI/BuildIconCell.prefab`), `_itemGap = (0, 6)`, `_padding = (4, 4, 4, 4)`, `_scrollDirection = Vertical`, `_align = LeftOrTop`, **`_columnCount = 1`** (단일 컬럼 강제 — auto 시 viewport 419 / itemSize 72 = 5 멀티 컬럼화 위험, §2.8.5 B1), `_rowCount/_poolItemCount = 0` (자동 — 단일 컬럼이라 row 가 곧 셀 수). **BuildIconCell prefab sizeDelta = (72, 72) 가 단일 진실** — 현행 prefab 그대로 유지 (`LairUIPrefabBuilder.cs:180`)
- **v0.8 갱신 — `LairUIPrefabBuilder.cs` BuildModalPopup 섹션** (v0.3 의 raw ScrollRect 예외를 CHPoolingScrollView 로 교체):
  - BuildModalPopup prefab 좌/우 섹션 각각에 **CHPoolingScrollView 구조 1세트** 부착:
    ```
    Section GameObject (좌 패시브 / 우 액티브 — 50:50 가로 분할)
     └ ScrollView (RectTransform + ScrollRect[Elastic 0.1] + BuildModalCardPoolingScrollView)
        └ Viewport (RectTransform 전체 stretch + RectMask2D + Image[alpha 0.001, raycastTarget=true])
            └ Content (RectTransform 만)
                 └ BuildModalCardCell origin (BuildModalCardCell.prefab 인스턴스)
    ```
  - BuildModalCardPoolingScrollView 직렬화 필드 빌더 설정 (v0.9 — `_itemSize` 제외, dead P1): `_origin = BuildModalCardCell prefab` (`Assets/_Lair/Art/UI/BuildModalCardCell.prefab`), `_itemGap = (0, 4)`, `_padding = (0, 0, 0, 0)`, `_scrollDirection = Vertical`, `_align = LeftOrTop`, `_rowCount/_columnCount/_poolItemCount = 0` (auto — 모달 가용 폭 303 ÷ itemSize 303 = 1 자동, BuildPanel 같은 5열 위험 없음). **BuildModalCardCell prefab sizeDelta = (303, 56) 가 단일 진실** — **현행 prefab sizeDelta 가 (280, 56) 이므로 빌더가 (303, 56) 으로 갱신해야 함** (v0.9 P1 발견 — `BuildModalCardCell.prefab:41` 의 `m_SizeDelta` 를 (303, 56) 으로 박는 빌더 스텝 추가)
  - 빈 상태 라벨 `_passiveEmptyText` / `_activeEmptyText` 는 각 ScrollView 외부 고정 (현행 BuildModalPopup.cs 직렬화 필드 그대로)
- **v0.8 — 툴팁 clamping 코드 변경 없음** (design-reviewer 권장수정 C1 정정): `SpawnerStatusTooltip.cs:98-107` 의 좌·우 양방향 `Mathf.Clamp` 분기는 v0.4~v0.7 내내 그대로 유지되어 있어 v0.7 좌측 anchor 변경 후 셀 0 케이스에서 자동 발동. **추가 코드 변경 / 빌더 변경 불요**.
- **v0.8 — BuildPanel 셀 raycast 차단 변경 없음**: v0.3 §2.6.2 의 BuildIconCell.Bind 시 onClick null 분기로 raycast 차단 (`BuildIconCell.cs:30~61` 의 `Interactable = false` + `raycastTarget = false`) 정책 그대로 — CHPoolingScrollView 의 `InitItem` 에서 호출되는 `cell.Bind(card, null)` 가 이 분기에 진입. drag/click 의 raycast 일관성(§2.8.5 raycast 일관성 표) 유지.

---

## 5. MVP 범위 확인

| 항목 | 범위 |
|---|---|
| Screen-space 6셀 패널 | MVP 내 (시너지 가시성 — 컨셉 §5.2) |
| 셀 상단 강화 아이콘 row | MVP 내 |
| **셀 상단 추가 생산(Spawn) 아이콘 슬롯 (v1.0)** | **MVP 내** — 사용자 명시 요청, 시너지 가시성 (컨셉 §5.2) |
| 셀 클릭 툴팁 (스탯별 포맷) | MVP 내 |
| BuildPanel Enhance 필터 | MVP 내 |
| BuildModalPopup (전체 카드 표시) | MVP 내 |
| 디스크 본체 (월드) 유지 | MVP 내 (스펙 §2.2 α) |
| World-space 진행 바 제거 | MVP 내 (스펙 §2.1) |
| **BuildPanel / BuildModalPopup / 툴팁 본문 — CHPoolingScrollView 완전 적용** | **MVP 내** (v0.8 — Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 원칙 완전 적용. v0.5 의 raw ScrollRect 정책 / v0.3 의 모달 예외 / v0.7 의 툴팁 raw ScrollRect 정책 모두 교체) |
| 사운드 hook | MVP 외 (§11.2) |
| 모달 일시정지 결합 | MVP 외 |
| 셀 정렬 옵션 (종 그룹화) | MVP 외 (기본 ring 인덱스 순) |
| 누적 스폰 수 / 누적 처치 수 통계 | MVP 외 |
| 신규 아트 에셋 (아이콘/배경) | MVP 외 (§11.4) |

---

## 6. 검증 가설 → 측정 메트릭

5분 한 판 플레이로 다음을 검증:

| 가설 | 측정 |
|---|---|
| 카드 픽 시 시너지 결정이 빨라진다 | (정성) 사용자 플레이 관찰 — 픽 화면에서 HUD 하단 글랜스 빈도, 의사결정 시간 |
| 6개 스포너 상태 동시 비교가 쉬워진다 | (정성) "어느 스포너가 어느 종을 뽑는지" 즉답 가능 여부 |
| BuildPanel 모달로 빌드 복기가 빠르다 | (정성) 모달 사용 빈도 + "내 빌드가 뭐였지" 질문 발생 빈도 |

검증 실패 시 후속 안:
- 셀 정렬 옵션(종 그룹화) 도입
- 모달 일시정지 결합
- 디스크 본체 색상 강조 강화 (월드 시각 피드백 추가)
- 셀 폭 확장 → 한국어 라벨 검토

---

## 7. 변경 이력

- **v0.1 (2026-05-27)**: 초안. 스펙 §1~§11 결정 락 기반으로 셀 치수·아이콘 매핑·툴팁 포맷·모달 레이아웃·구현 요청사항 디테일 채움. `ECardCategory.Enhance` 필터로 종 강화 6장 식별, `IBattleContext.RegisterMonsterTypeBuff` 시그니처 확장으로 카드 픽 추적, 스탯별 6 템플릿으로 툴팁 한 줄 포맷 명시. EUI 두 값 추가 (`BuildModalPopup`, `SpawnerStatusTooltip`).
- **v0.2 (2026-05-27)**: design-reviewer 1차 검토 BLOCKER 4건 + 권장수정 4건 반영.
  - **BLOCKER 1 (§2.6.1)**: BuildPanel 필터를 `Category == Enhance` 단일 조건에서 `Category == Enhance && IsPassive` 로 보강. 액티브 카드 4장(Berserk/BloodThirst/Frenzy/IronWill) 이 SO `_category: 0 (Enhance)` 로 직렬화된 현실과 충돌해 액티브가 함께 사라지는 문제 해소.
  - **BLOCKER 2 (§2.3, §2.5)**: 종 1 ↔ 강화 카드 1 매핑 (컨셉 §11.3) 전제 명시. row 의 distinct 아이콘 = 항상 0 또는 1 로 정정 (기존 "최대 6개" 오해 + 38px 폭 계산 모순 제거). §2.2 도식·§2.5.3 도식·§2.5.5 "각 강화 카드의" 복수 표현 단수화. ×N 배지 노출 조건(N≥2)도 §2.5.5 예시에 일관 반영.
  - **BLOCKER 3 (§2.5.5)**: Plague SlowFactor Base 값 출처 명시. `BalanceConfig.CharacterStat` 에 SlowFactor 필드가 없는 현실 인정 — Plague baseline 은 코드 상수 `Lair.Character.PlagueSlowOnHit.BaseSlowFactor (= 0.8f)` 에서 직접 읽도록 예외 명시. 나머지 5 스탯은 BalanceConfig 단일 진실 그대로.
  - **BLOCKER 4 (§4.2, §4.3, §4.7)**: `ICardEffect.Apply` 시그니처 변경안을 폐기. 대신 `BattleController` 내부에 `_currentCardScope` private 필드 + `ApplyCardEffect(card)` 래퍼 메서드로 카드 source 를 BattleController 안에서 추적. `IBattleContext.RegisterMonsterTypeBuff` 시그니처 / 6개 강화 효과 클래스 / `BattleContext.cs` / 25개 ICardEffect 구현체 **모두 변경 없음** — 변경 영향이 BattleController 한 곳으로 국소화. "재량" 표현 제거.
  - **권장수정 5 (§2.2.3)**: "11pt 측정 기준" 단정 제거. 측정 미실시 명시 + 폰트 다운 → 4자 truncate → ×N 인라인 폴백 정책 3단계 추가.
  - **권장수정 6 (§2.5.5)**: 라벨 표 `Hp → 체력` 으로 한국어 통일 (`HP` 영문 잔존 제거). §2.5.3 도식 예시도 `HP ×2.25` → `체력 ×2.25` 반영.
  - **권장수정 7 (§4.5)**: Spawner `OnEnable` 의 `OnOutputCountChanged` 초기 broadcast 제거. 초기 스냅샷은 `VM.AttachSpawners` 가 직접 폴링으로 채우고, 이벤트는 `IncrementOutput` 갱신 시에만 발행 — 더블 broadcast 해소.
  - **권장수정 8 (§2.5.6)**: 툴팁 vs 모달 dismiss UX 비대칭 1줄 근거 추가 (툴팁=가벼운 hover-like 무처리 / 모달=무거운 전체화면 dim 클릭 닫기, 의도적 차이).
- **v0.3 (2026-05-27)**: design-reviewer 2차 검토 BLOCKER 0건 + 잔존 권장수정 4건 + 옵션 1건 반영. "재량" / "또는" / "TBD" 잔존 0건 달성.
  - **정정 1 (§2.7.2)**: 모달 스크롤뷰를 `ScrollRect + ContentSizeFitter + VerticalLayoutGroup` 단순 구조로 단일 결정. 모달 카드 셀은 최대 패시브 7장 + 액티브 10장 ≈ 17개로 풀링 이득이 작아 Rule 03 §3 의 CHPoolingScrollView 우선 원칙에서 예외임을 명시. "재량" 표현 제거.
  - **정정 2 (§4.11)**: 신규 빌더 메뉴명을 `Lair/Setup/Spawner Status UI` 로 본 기획서가 단일 결정. 기존 `LairSpawnerVisualBuilder` 명명 컨벤션 따름. "재량" 표현 제거.
  - **정정 3 (§2.3.2)**: ×N 배지 표시 형식을 `×N` 단일 표기로 확정. `×9+` 압축 표기 옵션 제거. MVP 페이싱에선 두 자릿수 도달 안 함.
  - **정정 4 (§4.4, §4.6)**: SpawnerSnapshot 에 Progress 폴링 경로가 implicit 했던 부분 명시화. SpawnerSnapshot 주석에 "스냅샷은 이벤트 영역 전용" 명시 + Panel 책임에 "셀 생성·바인딩 시 해당 인덱스의 ISpawnerProgress 참조도 함께 cell 에 주입" 한 줄 + Cell 시그니처를 `Bind(SpawnerSnapshot, ISpawnerProgress)` 로 명시. 폴링 영역과 이벤트 영역 분리 명확화.
  - **옵션 5 (§4.3)**: `AppliedBuff` 위치를 `BattleViewModel.cs` 안 단일 결정 (Rule 02 §9 의 동일 도메인 단일 파일 정신, 기존 `BuildEntry` 와 같은 파일). "또는 별도 파일" 표현 제거.
- **v0.4 (2026-05-27)**: 사용자가 v0.3 구현 결과를 화면에서 확인한 뒤, **(1) SpawnerStatusPanel 가시성 부족** + **(2) BuildPanel 위치 명세 공백으로 두 패널이 하단에서 경쟁** 두 문제를 발견하여 두 가지 배치 변경을 결정. v0.3 의 모든 BLOCKER/권장수정 결정은 유지하고 배치 명세만 갱신.
  - **변경 A (§2.1, §2.2, §2.3, §3.2)** — **셀 1.4× 확대**: 46×56 → **64×80**, 패널 322×56 → **430×80** (6×64 + 5×6 + 2×8 단일 결정 값). 셀 내부 (padding 4/3→6/4, 색칩 10→14, gap 4→6, 종명 폰트 11→15pt, ×N 폰트 10→14pt, 진행 바 38×6→52×8, 아이콘 row 14→20, 원 지름 10→14, 아이콘 글자 8→11pt, ×N 배지 7→10pt, 본체 row 18→28) 모두 1.4× 비례 + 정수 반올림으로 단일 결정. **세로 budget 검산** 추가 (§2.2.1) — 80px 안에 모든 영역이 잔여 padding 포함 정확히 들어감.
  - **변경 B (§2.2.3 폴백 단순화)** — v0.3 의 3단계 폴백을 **2단계로 축약**. ×N 미노출 케이스(N=1, 다수) 의 종명 가용폭이 32px 로 충분 — v0.3 의 "×N 인라인 압축" 분기는 본체 row 28px 확대로 무용해져 제거. 측정 미실시 인정하되 1단계(폰트 다운) + 2단계(4글자 truncate) 안전망은 유지.
  - **변경 C (§2.5.2 clamping 제거)** — 셀이 커졌어도 패널 폭 430 + 화면 중앙 정렬 + 1280×720 기준에서 양 끝 셀(0/5) 의 툴팁 x-range 가 화면 안에 정확히 들어감을 검산 (§2.5.2). v0.3 의 "안전 margin 4px clamping" 분기는 무용 — 제거.
  - **변경 D (§2.8 신규 — BuildPanel 우측 배치)**: v0.3 까지 비어 있던 BuildPanel 화면 위치 명세를 채움. **Anchor `(1,0)`~`(1,1)`, Pivot `(1,0.5)`, 우측 offset 24px / 상단 32px / 하단 120px / 폭 240px / 결과 높이 568px (1280×720 기준)**. PassiveSection/ActiveSection 을 v0.3 의 좌우 50:50 가로 분할 → **상하 50:50 세로 분할** 로 변경 (우측 좁은 컬럼엔 세로 분할이 자연). 두 패널 가로 간격 161px / 세로 안전 여백 16px 검산 추가 (§2.8.4).
  - **변경 E (§4.11 빌더 작업 추가)**: `LairUIPrefabBuilder.BuildBattleHud` BuildPanel 빌드 절의 anchor/pivot/offset 4 값 갱신 + `BuildBuildSection(bool left)` → `bool top` 으로 시그니처 변경 + Container Layout 을 `HorizontalLayoutGroup` → `VerticalLayoutGroup` 으로 교체.
- **v0.5 (2026-05-27)**: design-reviewer 의 v0.4 검토 (BLOCKER 1건 + 권장수정 2건 + 의견 1건) 반영. v0.3/v0.4 의 모든 락 결정 유지하고, 셀 수용 한계 / 종명 폴백 / 안전 여백 표현 / hit area 변화 네 항목을 단정 갱신.
  - **BLOCKER 1 (§2.8.3 — BuildPanel 셀 수용 불일치, ScrollRect 승격)**: v0.4 §2.8.3 의 "줄바꿈 거의 없음" 단정이 코드 현실과 모순임을 확인 (`BuildIconCell.prefab` 셀 크기 = 72×72 이 `LairUIPrefabBuilder.cs:180` 에 직박이, 섹션 가용 영역 252px ÷ 78 ≈ 3.23 → 섹션당 최대 3셀 / 패시브 9장 + 액티브 10장 풀 모두 픽 시 클리핑). v0.5 는 ScrollRect 를 MVP 승격 — 각 섹션에 **`ScrollRect + Viewport(RectMask2D) + Content(VerticalLayoutGroup + ContentSizeFitter)`** 1세트씩. ScrollRect 옵션 (Vertical only / MovementType=Elastic / Elasticity 0.1 / Inertia on / Scrollbar 없음) 단일 결정. raycast 정책은 (a) — Unity EventSystem 의 자동 drag/click 분리로 ScrollRect drag 와 root CHButton click 이 자연 공존, 별도 분기 코드 불필요. §2.6.3 도 raycast 일관성 한 줄 보강. 셀 크기는 72×72 그대로, BuildIconCell.prefab / 빌더 라인 180 변경 없음.
  - **권장수정 5 (§2.2.3 종명 폴백 — 가로 폭 12px 제약 직접 해결)**: v0.4 가 폴백을 3단계→2단계로 줄인 근거 "본체 row 세로 확대" 가 실제 제약 (×N 노출 시 가로 12px) 과 무관했음을 인정. v0.5 1차 단정은 단순 4자 truncate 였으나 advisor 재검토 — 15pt `NotoSansKR SDF` 에서 4자 `Phan` 도 약 28~30px 로 12px 안 들어감을 확인 후 보강. **최종 단정: ×N 노출 케이스(N≥2)에서 색칩(14px) + gap(6px) = 20px 를 일시 숨김 → 종명 가용 폭 12 + 20 = 32px 로 회복 → 풀네임 유지**. 색칩 정보 손실은 셀 상단 강화 아이콘 row 배경색(§2.3.3) + 월드 디스크 본체 틴트(§2.4) 의 종 색상 3중 redundancy 로 보전. 4글자 truncate 는 폰트 메트릭 측정 후 풀네임이 32px 안 들어갈 때만 발동하는 **안전망** 으로 격하. 6 종의 4자 prefix (`Wisp`/`Wrai`/`Reap`/`Hex`/`Plag`/`Phan`) 가 모두 unique 라 종 식별성 손해 없음.
  - **권장수정 6 (§2.8.4 "세로 16px 안전 여백" 표현 정정)**: BuildPanel 과 SpawnerStatusPanel 은 **가로 161px 분리** 로 세로 겹침이 원천 불가능하다는 사실을 명시. 세로 거리 16px 는 단순 위치 결과값일 뿐 안전 여백 개념이 아님을 한 줄로 정정.
  - **의견 7 (§2.8.6 신규 — BuildPanel hit area 변화)**: v0.3 hit area 약 186K px² → v0.4/v0.5 약 136K px² (−27%) 정량 명시. **의도된 변경** — 우측 컬럼이 시각적으로 BuildPanel 임을 명확히 드러내므로 hit area 축소가 UX 손해를 만들지 않음을 단정.
- **v0.6 (2026-05-27)**: v0.5 권장수정 1건 (§4.6 색칩 visibility 토글 구현 요청 hook 누락) 반영. 의견 2건 (양방향 표기 / Bind 시그니처 잔존) 사용자 결정으로 패스.
- **v0.7 (2026-05-27)**: 사용자가 v0.6 구현 결과를 화면에서 확인한 뒤, **화면 가로 분할 정책 도입 + 툴팁 본문 ScrollRect 추가** 두 결정 요청. 사용자 발화 직접 인용 — **"스포너 패널이 가로 기준 왼쪽에서 3분의 2, 빌드패널은 우측에서 3분의 1, 툴팁은 스포너 한 패널의 1.5배 정도, 그리고 적용된 효과가 많을 수 있으니 텍스트는 스크롤뷰로 해줘"**. v0.3~v0.6 의 모든 락 결정은 유지하고, 패널 위치 / 셀 크기 / 툴팁 구조 / 빌더 작업 네 영역을 단정 갱신.
  - **변경 A (§2.1, §2.2, §2.3, §3.2) — SpawnerStatusPanel 좌측 하단 + 셀 2.094× 확대**: 패널 anchor `(0.5, 0)/(0.5, 0)` → **`(0, 0)/(0, 0)`** (좌측 하단). 화면 좌측 2/3 = 853px 폭에 6셀이 들어가야 함 → 셀 폭 단정 **134px** ((853 − padding 16 − spacing 30) ÷ 6 = 134.5 정수 단정). 셀 높이 = 134/64 × 80 = 167.5 → 정수 단정 **168px**. 패널 폭 6×134 + 5×6 + 2×8 = **850px**, 높이 168px. 셀 비례 2.094× 를 폰트에 그대로 적용 시 글자가 과도하게 비대 → advisor 권고대로 폰트 **cap 적용 — 종명 22pt · ×N 20pt · 아이콘 글자 16pt · ×N 배지 14pt**. 셀 면적 확대분은 padding / 영역 간격 / 색칩·아이콘 원 면적 확대로 흡수. 세로 budget 재산출 (8×2 + 42 + 8 + 58 + 8 + 17 + 잔여 19 = 168). 종명 가용 폭 재산출 (N=1 66px / N≥2 색칩 숨김 시 80px) — 풀네임 22pt 안전, 4글자 truncate 안전망은 v0.5 정책 그대로.
  - **변경 B (§2.8, §3.2) — BuildPanel 우측 1/3 폭 확대**: BuildPanel 폭 240 → **427px** (1280/3 = 426.67 → 정수 427). offset 4 값 모두 0 으로 단정 — `offsetMin = (-427, 0)` / `offsetMax = (0, 0)`. **하단 padding 결정 사유**: v0.6 는 SpawnerStatusPanel 회피용 세로 안전 여백 120px 였으나, v0.7 좌/우 가로 분할로 두 패널의 가로 영역이 겹치지 않음 (SpawnerStatusPanel x∈(0, 850) / BuildPanel x∈(853, 1280)) → 세로 겹침 원천 불가능 → 하단 padding 0 (전체 세로 사용). BuildPanel 폭 확대로 hit area +126% (136K → 307K px²). PassiveSection/ActiveSection 의 상하 50:50 분할 + ScrollRect 구조 (v0.4/v0.5) 는 그대로 유지 — anchor 비율 분할이라 BuildPanel 크기 변경 시 자동 비례 확대. 가용 세로 219px → 327px / 가용 폭 228px → 419px. 멀티 컬럼 도입 여부는 단일 컬럼 유지 (사용자 권장).
  - **변경 C (§2.5.5) — SpawnerStatusTooltip 본문 ScrollRect 추가**: 사용자 명시 요청 ("적용된 효과가 많을 수 있으니 텍스트는 스크롤뷰로 해줘") 직접 반영. 툴팁 폭 180 → **201px** (셀 1개 134 × 1.5 = 201). 높이 56 → **252px** (셀 168 × 1.5 = 252). **"스포너 한 패널의 1.5배" 의 "한 패널" 해석** — 셀 1개로 단정 (패널 전체 850 의 1.5배는 1275 로 화면 폭 1280 거의 가득 차서 비현실적, 셀 1개 134 의 1.5배가 자연스러움). **강화 줄 영역을 ScrollRect 로 래핑** — Vertical only / Elastic 0.1 / Inertia on / Scrollbar 없음 / RectMask2D Viewport + 거의 투명 raycast Image / Content VLG + CSF (BuildPanel §2.8.5 와 동일 파라미터 묶음). 헤더는 ScrollRect 외부 고정. **현 페이싱 (종 1↔카드 1) 에선 강화 줄이 0~1줄로 ScrollRect 가 과한 듯 보이지만, 사용자 명시 요청 + 미래 1↔다 매핑 확장 / PickCount 누적 표현 풍부화의 구조 보험 commit** — 비용 작고 (5개 컴포넌트), 0~1줄에선 자동으로 scroll 비활성.
  - **변경 D (§2.5.2) — 툴팁 좌우 clamping 부활**: v0.4~v0.6 의 "패널 중앙 정렬 → clamping 불필요" 결정은 v0.7 패널 좌측 anchor 변경으로 무효화. **셀 0 중심 = 75px (패널 좌측 + padding 8 + 셀 폭/2)** → 툴팁 폭 201 / pivot (0.5, 0) → 툴팁 x-range (−25.5, 175.5) → **좌측 화면 벗어남 25.5px**. **좌측 4px margin clamping 분기 부활** — `tooltipLeft < 4` 이면 `tooltipLeft = 4`. 우측 (셀 5 = 775 → x-range (674.5, 875.5)) 은 1276 이내라 발동 안 함, 방어 코드만 유지. ▼ 화살표는 anchor cell 가로 중앙 정렬 유지, 본체만 평행 이동 — 시각 부조화 허용 (좌측 1~2 셀에서만 발생, 빈도 낮음).
  - **변경 E (§4.11, §2.8.6) — 빌더 작업 갱신**: `LairSpawnerStatusUIBuilder.cs` 의 `CellWidth/CellHeight` 상수 (line 26~27) + `BuildPanelPrefab` anchor/pivot/anchoredPosition/sizeDelta (line 278~) + `BuildCellPrefab` 의 모든 내부 위치/크기/폰트 (line 83~) + `BuildTooltipPrefab` 의 Body 치수 + BuffText → ScrollRect 구조 치환 (line 317~) + clamping 분기 부활. `LairUIPrefabBuilder.cs` 의 BuildPanel offset (line 408~412) 갱신. `BuildBuildSection` 헬퍼 (v0.4~v0.5) 와 ScrollRect 구조 빌드는 변경 없이 유지 — anchor 비율 분할이라 BuildPanel 크기 변경 시 자동 비례.
  - **유지 (v0.3/v0.4/v0.5/v0.6 락 결정)** — Enhance+IsPassive 필터 / 종 1↔카드 1 매핑 / Plague SlowFactor 상수 / `_currentCardScope` / AttachSpawners 폴링 / Cooldown 표기 / SlowFactor 표기 / 모달 ScrollRect 단순구조 / ×N 단일 표기 / AppliedBuff 위치 / BuildPanel ScrollRect 구조 / 색칩 visibility 토글 / IconLetter/Badge CHText 명시 주입 — 모두 퇴행 없이 유지.
- **v0.8 (2026-05-27)**: 사용자가 v0.7 의 모달/툴팁/BuildPanel raw ScrollRect 구조를 보고 **Rule 03 §3 의 `ScrollRect + 수동 풀링 → CHPoolingScrollView` 우선 원칙 완전 적용** 을 결정. 사용자 발화 직접 인용 — **"룰에 chscrollview 쓰라고 안되어있어?"**. 추가로 design-reviewer 의 v0.7 검토 (BLOCKER 3건 + 권장수정 3건) 를 한 패스에 흡수. v0.3~v0.7 의 모든 락 결정은 유지하고, 세 ScrollRect 영역 / BLOCKER 1/2 단정 / 권장수정 3건을 정리.
  - **변경 A (§ 헤더, §1, §2.5.5, §2.7.2, §2.8.2, §2.8.3, §2.8.5, §3.2, §4.6, §4.7, §4.9, §4.11) — Rule 03 §3 완전 적용**: 사용자 결정 인용 후 세 raw ScrollRect 영역을 모두 `CHPoolingScrollView<TItem, TData>` 파생 컴포넌트로 재단정.
    - **§2.5.5 SpawnerStatusTooltip 본문** — `CHPoolingScrollView<BuffLine, AppliedBuff>` (TItem `BuffLine` 신규, TData `BattleViewModel.AppliedBuff`). 다중 자식 BuffLine 단정 (v0.7 의 "단일 CHText vs 다중 자식" 모호 — BLOCKER B3 해소).
    - **§2.7.2 BuildModalPopup 좌/우 섹션** — `CHPoolingScrollView<BuildModalCardCell, BuildEntry>` 각 섹션 1세트 (v0.3 의 "Rule 03 §3 예외" 결정 철회).
    - **§2.8.5 BuildPanel PassiveSection / ActiveSection** — `CHPoolingScrollView<BuildIconCell, BuildEntry>` 각 섹션 1세트 (v0.5 의 raw ScrollRect+VLG+CSF 정책 교체).
    - 모든 raw `VerticalLayoutGroup + ContentSizeFitter` 부착 폐기 — CHPoolingScrollView 의 `InitItemTransform` (Packages 의 line 770~) 이 자식 RectTransform 직접 설정.
    - 각 CHPoolingScrollView 의 직렬화 필드 (`_origin / _itemSize / _itemGap / _padding / _scrollDirection / _align`) 단일 결정값으로 §3.2 / §2.x.5 / §4.11 동기화.
  - **변경 B (§2.2.1) — 진행 바 anchoredPosition.y 단일 진실** (design-reviewer BLOCKER B1 해소): 셀 RectTransform pivot (0, 0) 가정 시 진행 바 anchoredPosition.y = `하단 padding 8 + 잔여 19 = 27`, 본체 row y = 52, 아이콘 row y = 118 로 budget 검산 자기 정합. 빌더 직박이 단일값으로 단정.
  - **변경 C (§2.5.5) — 툴팁 본문 가용 세로 단일 진실** (design-reviewer BLOCKER B2 해소): CHPoolingScrollView RectTransform `anchorMin/Max (0, 0)/(1, 1)` + `offsetMin (8, 8) / offsetMax (-8, -40)` → 폭 185 / 세로 204. 본문 가용 세로 196 (= 252 − 헤더 32 − padding×2 16 − gap 8) 단일 단정. BuffLine 한 줄 sizeDelta (185, 24) — `_itemSize` 직렬화 + prefab sizeDelta 일치.
  - **변경 D (§2.5.2) — clamping "부활" → "유지" 표현 정정** (design-reviewer 권장수정 C1): `SpawnerStatusTooltip.cs:98-107` 의 좌·우 양방향 `Mathf.Clamp` 분기는 v0.4~v0.7 내내 코드상 그대로 유지되어 있었음. v0.7 좌측 anchor 변경 후 셀 0 케이스에서 자동 발동 — "제거된 적 없는 분기" 임을 정정. 추가 코드/빌더 변경 불요.
  - **변경 E (§2.5.2) — "셀 0/1 좌측 벗어남" → "셀 0 만" 정정** (design-reviewer 권장수정 C2): 셀 0 중심 75 / 셀 1 중심 215 검산. 셀 1 의 툴팁 x-range (114.5, 315.5) 는 화면 안 (114.5 > safeMargin 4) — 좌측 clamping 발동 안 함. **셀 0 한 곳만** clamping 발동.
  - **변경 F (§2.5.5) — BuffLine 폰트 cap 정책 명시** (design-reviewer 권장수정 C3): 셀의 폰트 cap 정책(§2.2.1 — 비례 적용 안 함)을 툴팁에도 동일 적용. **IconLetter 12pt · ×N 배지 10pt · 본문 10pt** (v0.7 사이즈 그대로 유지) 단정. 사유 — 툴팁 면적 1.5× 확대분도 패딩·아이콘 원 면적으로 흡수.
  - **변경 G (§4.6) — 신규 .cs 파일 5건**: `BuildModalCardCell.cs` (현 `BuildModalPopup.cs:118` nested 정의를 파일 분리) + `BuffLine.cs` 신규 + 3개 CHPoolingScrollView 파생 클래스 (`BuildIconPoolingScrollView.cs` / `BuildModalCardPoolingScrollView.cs` / `BuffPoolingScrollView.cs`). `BuildModalCardCell.prefab` 은 이미 존재 (script 부착만), `BuffLine.prefab` 신규.
  - **변경 H (§4.7) — 수정 .cs 파일 3건 명세 갱신**: `BuildPanel.cs` (`_passiveContainer` → `_passiveScrollView` 타입 교체, `_cells` Dict + `_cellPrefab` 제거, `Refresh` 단순화) / `BuildModalPopup.cs` (`_passiveContent` → `_passiveScrollView` 교체, `_spawnedCells` + `CHMPool.Push` 루프 제거, `_cellPrefab` 제거, nested `BuildModalCardCell` 이주) / `SpawnerStatusTooltip.cs` (`_buffText` 제거, `_buffScrollView` + `_emptyText` 신규, `FormatBuffLine` 메서드 `BuffLine.cs` 로 이주, `RefreshContent` 단순화).
  - **변경 I (§4.11) — 빌더 작업 갱신**: `LairSpawnerStatusUIBuilder.BuildTooltipPrefab` 의 v0.7 raw ScrollRect 구조를 BuffPoolingScrollView 구조로 교체. `BuildBuffLinePrefab` 헬퍼 신규. `LairUIPrefabBuilder.BuildBuildSection` 의 v0.4~v0.5 raw ScrollRect 구조를 BuildIconPoolingScrollView 구조로 교체. BuildModalPopup 빌더에 BuildModalCardPoolingScrollView 구조 추가. 각 ScrollRect 의 `viewport / content` 직렬화 + 각 CHPoolingScrollView 의 6개 직렬화 필드 wire-up 명세.
  - **유지 (v0.3/v0.4/v0.5/v0.6/v0.7 락 결정)** — Enhance+IsPassive 필터 / 종 1↔카드 1 매핑 / Plague SlowFactor 상수 / `_currentCardScope` / AttachSpawners 폴링 / Cooldown 표기 / SlowFactor 표기 / ×N 단일 표기 / AppliedBuff 위치 / 색칩 visibility 토글 / IconLetter/Badge CHText 명시 주입 / 셀 134×168 / 패널 850×168 / BuildPanel 427×720 우측 / 툴팁 201×252 — 모두 퇴행 없이 유지.
  - **Rule 03 §3 의 "수동 풀링" 조건 충족 검토**: Rule 03 §3 표 (`.claude/rules/11-use-chvj-ui-components.md:12`) 는 `ScrollRect + 수동 풀링 → CHPoolingScrollView<TItem, TData>` 를 명시. 현 세 영역은 — (1) BuildPanel: v0.5~v0.7 BuildPanel.cs 가 `_cells` Dict + `CHMPool.Pop` 직접 호출로 수동 풀링 (Rule 03 §4 풀 사용은 합 — 단 ScrollRect 와 결합한 풀링은 CHPoolingScrollView 가 자동화), (2) BuildModalPopup: v0.3 BuildModalPopup.cs 가 `_spawnedCells` List + `CHMPool.Pop` 으로 수동 풀링, (3) Tooltip: v0.7 spec 자체가 future 1↔다 확장 시 줄 수만큼 인스턴스 풀링 — 셋 다 "ScrollRect + 수동 풀링" 조건을 명백히 충족. v0.3 의 "최대 17개로 풀링 이득 작음" / v0.5 의 "raw ScrollRect 로 충분" 판단은 Rule 03 §3 의 명시 원칙 앞에 후순위 — Rule 03 §3 이 단정한 조건에서는 이득 크기를 사후 판단하지 말고 원칙 적용이 옳음.
- **v0.9 (2026-05-27)**: design-reviewer 의 v0.8 검토 (BLOCKER B1 1건 + 권장수정 P1 1건) 를 한 패스에 흡수. v0.3~v0.8 의 모든 락 결정은 퇴행 없이 유지, BuildPanel `_columnCount` 직박이 1건 + `_itemSize` dead serialization 정정 1건만 세 영역에 일관 적용. 결정 근거는 `Packages/com.chvj.unityinfra/Runtime/UI/CHPoolingScrollView.cs` line 394-415 직접 확인.
  - **BLOCKER B1 (§2.8.5 + §3.2 + §4.11) — BuildPanel `_columnCount = 1` 직박이 단정**: `CHPoolingScrollView.SetColumnCount` (CHPoolingScrollView.cs:404-415) 는 `_columnCount <= 0` 일 때 viewport 폭 ÷ (itemSize.x + itemGap.x) 로 자동 산출. BuildPanel 섹션에서 viewport 약 419 / itemSize 72 → `FloorToInt(419/72) = 5` → **멀티 컬럼화 (5열)** — 기획서 단정 "단일 컬럼 유지" 와 충돌. 툴팁 (viewport 185 / itemSize 185 = 1) 과 모달 (viewport 303 / itemSize 303 = 1) 은 auto 산출도 1 이라 영향 없음. **BuildPanel `BuildIconPoolingScrollView._columnCount = 1` 만 명시 단정**. 위치 3곳 일관 갱신:
    - §2.8.5 직렬화 필드 표 — 기존 `_rowCount / _columnCount / _poolItemCount | 0 / 0 / 0` 통합 행을 분리해 `_columnCount = 1` 단일 행 + `_rowCount / _poolItemCount = 0` 행으로 정리, 결정 근거 인라인.
    - §3.2 "BuildPanel 멀티 컬럼 도입" 행 — `_columnCount = 1` 직박이 단정 보강.
    - §4.11 BuildIconPoolingScrollView wire-up — `_columnCount = 1` 명시 추가 (BuffPoolingScrollView / BuildModalCardPoolingScrollView 의 wire-up 은 변경 없음, auto 로 두면 됨).
  - **권장수정 P1 (§2.5.5 + §2.7.2 + §2.8.5 + §3.2 + §4.11) — `_itemSize` dead serialization 정정**: `CHPoolingScrollView.SetItemSize` (CHPoolingScrollView.cs:394-402) 는 `SetItemList` 호출 시마다 `_itemSize.x/y` 를 origin RectTransform 의 `rect.width/height × localScale` 로 **무조건 덮어쓴다**. 직렬화 `_itemSize` 는 dead — **prefab sizeDelta 가 단일 진실**. v0.8 의 "`_itemSize` 직렬화 + prefab sizeDelta 일치" / "어느 쪽이든 OK" 표현은 부정확. 위치 5곳 일관 정리:
    - §2.5.5 BuffPoolingScrollView 표 line 349 + 본문 line 362 — `_itemSize` 행을 "dead serialization, BuffLine prefab sizeDelta = (185, 24) 단일 진실" 로 정정.
    - §2.7.2 BuildModalCardPoolingScrollView 표 line 529 — `_itemSize` 행을 "dead, prefab sizeDelta (303, 56) 단일 진실" 로 정정.
    - §2.8.5 BuildIconPoolingScrollView 표 line 650 — `_itemSize` 행을 "dead, BuildIconCell.prefab sizeDelta (72, 72) 단일 진실" 로 정정.
    - §3.2 line 764 / 776 / 791 — 세 행 모두 "dead serialization, prefab sizeDelta = 단일 진실" 정신으로 정정.
    - §4.11 line 1084 / 1109 / 1119 — 세 wire-up 모두 `_itemSize = (...)` 항목 제거 (빌더가 prefab sizeDelta 만 박으면 충분, `_itemSize` 직렬화 wire-up 불요).
  - **P1 부수 발견 — BuildModalCardCell.prefab sizeDelta 불일치**: 검증 중 `Assets/_Lair/Art/UI/BuildModalCardCell.prefab:41` 의 `m_SizeDelta` 가 `(280, 56)` 임을 확인. 기획서 단정 `(303, 56)` 과 불일치. P1 정신("prefab sizeDelta 가 단일 진실")에 따라 **빌더가 prefab sizeDelta 를 (303, 56) 으로 갱신해야 함** — §4.11 line 1119 wire-up 항목에 이 빌더 스텝 추가. BuildIconCell.prefab (72, 72) 와 BuffLine.prefab (185, 24) 는 prefab 자체 sizeDelta 가 기획서 단정값과 일치하므로 별도 갱신 불요.
  - **유지 (v0.3~v0.8 락 결정 전체 재확인)** — Enhance+IsPassive 필터 / 종 1↔카드 1 매핑 / Plague SlowFactor 상수 / `_currentCardScope` / AttachSpawners 폴링 / Cooldown 표기 / SlowFactor 표기 / ×N 단일 표기 / AppliedBuff 위치 / 색칩 visibility 토글 / IconLetter/Badge CHText 명시 주입 / 셀 134×168 / 패널 850×168 / BuildPanel 427×720 우측 / 툴팁 201×252 / 진행 바 anchoredPosition.y = 27 / 본체 row y = 52 / 아이콘 row y = 118 / 본문 가용 세로 196 / 다중 자식 BuffLine 단정 / 세 영역 CHPoolingScrollView 완전 적용 — 모두 퇴행 없이 유지.
- **v1.0 (2026-05-27)**: 사용자가 v0.9 구현 결과를 화면에서 확인한 뒤, **셀 IconRow 에 추가 생산(Spawn) 카드 아이콘 표시** 요청. 사용자 발화 직접 인용 — **"스포너 추가 생산도 x2 표시되는건 확인했는데 위에 버프 표시되는거처럼도 표시해줘"**. 셀 본체 row 의 `×N` 동시 출력 표시는 v0.x 부터 정상 동작 중 (Spawner.OutputCount 기반). 본 요청은 **셀 상단 IconRow 에 강화 카드(H/D/S/R/M/P) 가 표시되듯 추가 생산 카드도 같은 패턴으로 노출** 하는 것. v0.3~v0.9 의 모든 락 결정은 퇴행 없이 유지, IconRow 1 슬롯 → 2 슬롯 확장 + Spawn 5 카드 매핑 추가 + Spawn 픽 추적 + BuffLine FormatBody Spawn 분기 + 빌더 IconRow 2 슬롯 자식 빌드 7 영역에 일관 적용.
  - **변경 A (§2.3, §2.3.1, §3.2) — IconRow 1 → 2 슬롯 확장 (advisor 2회 검토 후 슬롯 순서·위치 단정)**: 셀 상단 IconRow 를 단일 슬롯에서 **좌 Enhance / 우 Spawn 2 슬롯 고정 구조** 로 확장. 슬롯 1 (Enhance) anchoredPosition.x = **12** (기존 위치 보존 — v0.x 까지 `_iconCircle` 가 12px 자리, 일반 케이스 시각 regression 회피), 슬롯 2 (Spawn, v1.0 신규) anchoredPosition.x = **68** (= 슬롯 1 배지 우측 끝 64 + 슬롯 간 spacing 4 — 배지 형상 검산 결과). 각 슬롯의 distinct 아이콘은 여전히 0 또는 1 — 두 슬롯이 독립 토글, 픽되지 않은 슬롯만 `SetActive(false)`.
    - **슬롯 순서 결정 사유 (advisor 권장)**: (1) v0.x 까지 Enhance 가 12px 좌측 자리 — 일반 케이스 (Enhance 만 픽) 의 시각 위치 보존. Spawn 슬롯을 좌측에 두면 모든 기존 셀이 우로 시각 이동, regression. (2) 사용자 발화 *"위에 버프 표시되는거처럼**도**"* 의 *"도"* 는 추가 의미 — 우측에 추가하는 게 의미적으로 자연. (3) §2.3.4 갱신 트리거는 Enhance 경로 변경 없음, Spawn 경로만 신규.
    - **슬롯 2 위치 단정 사유 (advisor 2차 검토 — 배지 충돌 회피)**: 1차 안 (46px) 은 icon 사이즈만 고려한 잘못된 계산. `LairSpawnerStatusUIBuilder.BuildCellPrefab` line 148~166 의 배지 RectTransform 은 icon 의 child 로 anchor (1, 0) / pivot (0, 1) / anchoredPosition (-2, 1) / sizeDelta (24, 14) → **배지 우측 끝이 icon 우측 끝 +22px 확장**. 슬롯 1 배지 우측 끝 = (12+30) − 2 + 24 = 64. 슬롯 2 icon 을 x=46 에 두면 슬롯 1 배지가 슬롯 2 icon 영역 (46~76) 을 18px 침범. **슬롯 2 x = 68 단정** (= 64 + 4 spacing). 슬롯 2 배지 우측 끝 = 68+30+22 = 120 → Row stretch 폭 134 안에 14px 여유. 두 슬롯 모두 PickCount≥2 케이스에도 배지 시각 충돌 없음을 빌드 전 검산.
  - **변경 B (§2.3.3) — Spawn 5 카드 매핑 추가**: SpawnWisps/SpawnWraith/SpawnReapers/SpawnPlagues/SpawnPhantoms 모두 글자 `+` + 종 색 배경 + 글자 색 (Phantom 만 흰색, 나머지 검정 — 가독성). **Hex 종은 SpawnHex 카드가 존재하지 않음** (`Assets/_Lair/Scripts/Data/CommonEnum.cs:66-70` 의 ECardId 5 Spawn 카드 확인) → Hex 셀의 Spawn 슬롯은 영구 비활성 (`IconLetterFor` 반환 ` ` → `SetActive(false)`). `IconLetterFor` 함수에 5 매핑 추가가 단일 진실 — BuffLine 의 IconLetter wire-up 도 같은 함수 재사용으로 자동 동작.
    - **글자 `+` 단일 결정 사유**: 종 첫 글자 (W/Wr/R/P/Ph) 는 강화 글자 (H/D/S/R/M/P) 와 충돌 (`P` 충돌). 곱 기호 `×` 는 본체 row 의 `×N` 과 시각 충돌. **`+` (증가의 보편 기호)** 가 충돌 없고 가산 의미 직관적.
  - **변경 C (§2.3.5) — 셀 본체 `×N` vs Spawn 슬롯 `×N` 의미 구분 명시**: 두 `×N` 이 한 셀에 동시 노출 가능. 본체 `×N` = `Spawner.OutputCount` (현재 동시 출력 마릿수, 실시간 상태), Spawn 슬롯 `×N` = `AppliedBuff.PickCount` (SpawnX 카드 누적 픽 횟수, 빌드 히스토리). Replace 카드로 종 교체된 셀에서 두 값이 다를 수 있음. 한 셀에 두 값 모두 노출되어 "지금 몇 마리 / 내가 몇 번 강화했나" 둘 다 즉답 가능 (§2.3.5 표 명시).
  - **변경 D (§2.3.6) — Retroactive 시각 정책 락 결정 (advisor 옵션 a 채택)**: SpawnPhantoms 픽 시점에 Phantom 출력 Spawner 가 0 대여도 `_typeModifierPicks[Phantom]` 에 픽 누적. 이후 Replace 로 Phantom 출력 Spawner 가 생기면 셀의 Spawn 슬롯에 retroactive 표시. **의도된 동작** — Enhance 의 글로벌 dict `_typeModifiers` 정책과 의미론 일치 (Enhance 도 출력 종 없을 때 픽되면 이후 출력 시 retroactive 적용). 옵션 b (Spawner 존재 시점에만 추적) 는 픽 순서에 따라 비결정적 시각 — 락 결정으로 거부.
  - **변경 E (§4.3) — BattleController.IncrementSpawnerOutput 픽 추적 + 이벤트 발행**: `_currentCardScope != null` 이면 `TrackSpawnPick(type, _currentCardScope)` 호출 + `OnTypeModifierChanged?.Invoke(type)` 발행 추가. `TrackSpawnPick` 신규 메서드 — `_typeModifierPicks` (Enhance 와 자료구조 공유) 에 누적, `Stat = EMonsterStatKind.Hp` (default, 읽히지 않음 — Category 분기로 우회), `AggregateMultiplier = 1f` (unused for Spawn). `TrackCardPick` (Enhance 경로) 의 `AggregateMultiplier` 일괄 갱신 루프에 `Category == Enhance` 필터 추가 — Spawn 엔트리의 Multiplier 덮어쓰기 방어.
    - **`AppliedBuff.Stat` 필드 컨벤션 (v1.0 명시)**: Spawn 카테고리는 stat 무의미 → `EMonsterStatKind` 에 `None` 추가 안 함 (Rule 02 §8 Enum churn 회피). 컨벤션 — Spawn 픽의 Stat 은 default `Hp`, `BuffLine.FormatBody` 가 `Source.Category == Spawn` 분기를 stat switch 보다 먼저 검사하여 Stat 읽지 않음.
  - **변경 F (§2.5.5) — BuffLine FormatBody Spawn 분기 추가**: `FormatBody` 시작부에 `if (buff.Source.Category == ECardCategory.Spawn) return $"동시 출력 +{buff.PickCount}";` 추가. Enhance 카테고리는 기존 stat switch 그대로. Spawn 본문 단일 단정 — `동시 출력 +{PickCount}` (advisor 권장 `(1 → N+1)` 형식 거부 — Spawner 마다 시작 OutputCount 가 다르므로 단일 Base→Result 형식 자체가 misleading). PickCount=1 일 때도 `+1` 표기 (Enhance `×1.5` 와 일관). `OnEnable` 의 `_badge.gameObject.SetActive(false)` reset 은 변경 없음 — Spawn `[ ×N]` 도 PickCount≥2 일 때만 표시.
  - **변경 G (§4.6 SpawnerStatusCell) — IconRow 2 슬롯 직렬화 필드 확장**: 기존 `_iconCircle / _iconLetter / _iconBadge` 단일 셋 → 명명 정리로 **`_iconCircleEnhance / _iconLetterEnhance / _iconBadgeEnhance`** (변수명 변경, 기능 동일) + 신규 **`_iconCircleSpawn / _iconLetterSpawn / _iconBadgeSpawn`** 6 직렬화 필드. `RebindIconRow` 가 `snapshot.AppliedBuffs` 를 순회하면서 `Source.Category` 분기로 슬롯 결정. 두 슬롯 모두 미픽이면 `_iconRow.gameObject.SetActive(false)` (기존 정책), 한 슬롯만 픽이면 픽된 슬롯만 활성. Hex 종 Spawn 슬롯은 `IconLetterFor` 의 ` ` fallback 으로 자연 비활성.
  - **변경 H (§4.11 빌더) — BuildCellPrefab IconRow 2 슬롯 자식 빌드**: `LairSpawnerStatusUIBuilder.BuildCellPrefab` 의 IconRow 자식 구성을 단일 슬롯 → 2 슬롯으로 확장. 슬롯 1 (Enhance) x=12, 슬롯 2 (Spawn) x=68 (= 슬롯 1 배지 우측 끝 64 + 슬롯 간 spacing 4 — §2.3.1 v1.0 배지 형상 검산), 각각 30×30 원 + 16pt 글자 CHText + 14pt 배지 CHText. SpawnerStatusCell SerializedObject wire-up 도 6 신규 필드 (`_iconCircleEnhance/Spawn`, `_iconLetterEnhance/Spawn`, `_iconBadgeEnhance/Spawn`) 추가. BuffLine prefab 빌더 (`BuildBuffLinePrefab`) 변경 없음 — IconLetter wire-up 은 동일, FormatBody 분기만 코드 변경. **Prefab wire-up 보존 절차 명시**: 변수명 변경 (`_iconCircle → _iconCircleEnhance` 등) 시 Unity 직렬화가 필드명을 키로 하므로 기존 `SpawnerStatusCell.prefab` 의 fileID 참조가 끊긴다. `Lair/Setup/Spawner Status UI` 메뉴 1회 재실행으로 빌더 `BuildCellPrefab` 의 `SaveAsPrefabAsset` (line 906) 이 prefab 을 새 필드명 wire-up 으로 재생성하여 자동 해소. (`[FormerlySerializedAs]` 마킹 안 함 — 빌더 재실행이 단일 진실 진입점)
  - **유지 (v0.3~v0.9 락 결정 전체 재확인)** — Enhance+IsPassive 필터 / 종 1↔Enhance 카드 1 매핑 (v1.0 신규: 종 1↔Spawn 카드 1 매핑, Hex 제외) / Plague SlowFactor 상수 / `_currentCardScope` / AttachSpawners 폴링 / Cooldown 표기 / SlowFactor 표기 / ×N 단일 표기 (셀 본체 + Spawn 슬롯 배지) / AppliedBuff 위치 / 색칩 visibility 토글 / 셀 134×168 / 패널 850×168 / BuildPanel 427×720 우측 / 툴팁 201×252 / 진행 바 anchoredPosition.y = 27 / 본체 row y = 52 / 아이콘 row y = 118 / 본문 가용 세로 196 / 다중 자식 BuffLine 단정 / 세 영역 CHPoolingScrollView 완전 적용 / `_columnCount = 1` BuildPanel / `_itemSize` dead serialization — 모두 퇴행 없이 유지. `_typeModifierPicks` 자료구조는 v0.x 그대로 (Enhance + Spawn 통합 누적 — Category 분기로 자연 구분).
- **v1.1 (2026-05-27)**: v1.0 design-reviewer BLOCKER 1건 (changelog leftover x=46) + 권장수정 1건 (prefab wire-up 보존) 흡수. 의견 2건 (BuffLine 본문 Spawn 표시 의도 확인 / `OnTypeModifierChanged` rename) 은 사용자 판단으로 패스. v0.3~v1.0 의 모든 락 결정은 퇴행 없이 유지, §7 v1.0 변경 H 의 "x=46" → "x=68" 정정 + prefab wire-up 보존 한 줄 추가 2 영역에만 적용.
  - **BLOCKER 정정 (§7 v1.0 변경 H) — "x=46" → "x=68"**: §7 v1.0 changelog 변경 H 본문 안의 "슬롯 2 (Spawn) x=46" 표기는 변경 A (line 1399) 가 이미 폐기한 1차 안의 leftover. 본문 단정 위치 (§2.3.1 line 235, §3.2 line 854, §4.11 line 1235) 는 모두 x=68 로 정합. 변경 H 한 줄만 정정 + spacing 단정 (= 64 + 4) 동행 정합.
  - **권장수정 흡수 (§7 v1.0 변경 H) — prefab wire-up 보존 절차 한 줄 명시**: SpawnerStatusCell 변수명 변경 (`_iconCircle → _iconCircleEnhance` 등) 시 Unity 직렬화가 필드명을 키로 하므로 기존 `SpawnerStatusCell.prefab` 의 fileID 참조가 끊긴다. 그러나 빌더 `BuildCellPrefab` 의 `SaveAsPrefabAsset` (line 906) 이 prefab 을 완전히 덮어쓰는 구조라 `Lair/Setup/Spawner Status UI` 메뉴 재실행으로 자동 해소. 이 절차를 변경 H 본문에 한 줄 추가하여 gameplay-programmer 가 prefab 재생성 단계를 누락하지 않도록 명시. `[FormerlySerializedAs]` 마킹은 안 함 — 빌더 재실행이 단일 진실 진입점이라 마킹은 dead annotation 이 됨.
  - **패스 (의견 2건)** — (1) BuffLine 본문 Spawn 표시 의도 (현 `동시 출력 +{PickCount}` 단정) 는 사용자가 v1.0 승인 단계에서 직접 검수 영역. (2) `OnTypeModifierChanged` 이름 rename (Enhance+Spawn 통합 의미 반영) 은 MVP 외 작업 — 현 이름이 자료구조 `_typeModifierPicks` 와 자연 일치하므로 churn 회피.
  - **유지 (v0.3~v1.0 락 결정 전체 재확인)** — Enhance+IsPassive 필터 / 종 1↔Enhance 카드 1 매핑 / 종 1↔Spawn 카드 1 매핑 (Hex 제외) / Plague SlowFactor 상수 / `_currentCardScope` / AttachSpawners 폴링 / Cooldown 표기 / SlowFactor 표기 / ×N 단일 표기 (셀 본체 + Spawn 슬롯 배지) / AppliedBuff 위치 / 색칩 visibility 토글 / 셀 134×168 / 패널 850×168 / BuildPanel 427×720 우측 / 툴팁 201×252 / 진행 바 anchoredPosition.y = 27 / 본체 row y = 52 / 아이콘 row y = 118 / 본문 가용 세로 196 / 다중 자식 BuffLine 단정 / 세 영역 CHPoolingScrollView 완전 적용 / `_columnCount = 1` BuildPanel / `_itemSize` dead serialization / IconRow 2 슬롯 / Spawn 카드 매핑 / Retroactive 정책 — 모두 퇴행 없이 유지. v1.1 은 §7 의 leftover 정정 + 절차 한 줄 추가만으로, 본문 단정·자료구조·구현 요청사항은 한 글자도 손대지 않음.
