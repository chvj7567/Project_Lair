# 인게임 상태 셀 강화 레벨 표현 (도감 동일)

> 입력: spec `docs/superpowers/specs/2026-07-26-ingame-spawner-cell-enhance-level-design.md` · plan `docs/superpowers/plans/2026-07-26-ingame-spawner-cell-enhance-level.md`
> 이 기획서는 plan 의 `⟨기획서 확정⟩` 자리(발광 오버레이 크기·배지 위치·스케일 헤드룸·발광 세기·레벨 소스 배선)를 닫는다. 파일 구조·시그니처·TDD 골격은 plan 이 SoT, 레벨→시각 매핑 값은 도감 기획서 `docs/design/monster-codex-prefab-icon-enhancement.md` §2·3·4 가 **공유 SoT**(본 문서는 그 값을 **읽기만** 하고, 상태 셀 전용 배치·기하만 확정).

---

## § 헤더

- **목표**: 인게임 하단 6칸 상태 패널의 각 셀(`SpawnerStatusCell`) 중앙 아이콘에, 그 종족의 상점 강화 레벨(Lv0~5)을 **도감(`CodexCell`)과 동일한 4채널**(발광 오버레이·아이콘 틴트·스케일·"Lv N" 배지)로 반영한다. Lv0 은 담백한 원본.
- **검증 가설**: 전장에서 이미 발광하는 몬스터(`MonsterEnhancementVisual`)와 상태 셀 아이콘이 **같은 종족색·같은 밝기 티어**로 호응해, "내가 키운 종족"이 HUD 에서도 즉시 읽히는가. 두 표현이 **같은 레벨 소스**(`GetShopLevel("Enhance_"+종)`)와 **같은 시각 매핑 SoT**(`EnhanceLevelVisual`)를 공유해 "전장 = 도감 = 상태 셀" 일치가 유지되는가.
- **현재 단계 범위 적합성**: **범위 내**. 신규 몬스터/카드/영웅/레벨 리소스 제작 0 — 기존 `MonsterIcons`·`SpeciesGlowColor`·`UISoftGlow.png`(도감 작업 L 이 신설한 라디얼 소프트글로우, 이미 레포에 존재) 재사용. spec §2 "신규 리소스 0" 준수. 신규 에셋 0건.
- **핵심 메커니즘**: 도감의 레벨→시각 매핑 3배열 + 4채널 적용 로직을 공유 헬퍼 `Lair.UI.EnhanceLevelVisual` 로 추출(plan Task 1) → `CodexCell`·`SpawnerStatusCell` 이 동일 코드를 호출(drift 방지). 상태 셀은 작고 빽빽하므로(아이콘 64px, 셀 134×170) **배치·기하만 셀 전용으로 확정**하고, 매핑 값·색은 도감 SoT 를 그대로 공유한다.

---

## 1. 셀 실측 기하 (프리팹 SoT — 모든 검산의 기준)

`Assets/_Lair/Art/UI/SpawnerStatusCell.prefab` 실측. 셀 루트 `sizeDelta 134×170`, pivot (0,0). 좌표는 **셀 좌하단(0,0) 기준**. 패널(`SpawnerStatusPanel`)의 `HorizontalLayoutGroup` 은 `ChildControlWidth/Height=0`·`ChildForceExpand=0`·`ChildScale=0` 이라 **셀을 리사이즈하지 않는다** → 이 134×170 이 런타임 실치수(검산 확정: 6셀×134 + 5간격×6 = 834 ≤ 컨테이너 850).

| 요소 | 앵커/피벗 | anchoredPos | size | 셀 좌하단 기준 범위 (x / y) |
|---|---|---|---|---|
| **Border**(종색 프레임) | stretch (0,0)-(1,1) | — | 134×170 | 셀 전체 외곽 |
| **InnerBackground**(#1F2937 α1) | stretch | — | 128×164 | 셀 내부 |
| **Icon**(중앙 아이콘) | center (0.5,0.5) | (0, 30) | 64×64 | x 35–99 / y 83–147 (중심 67,115) |
| **CountText ×N**(노랑) | center, pivot (1,0.5) | (57, 5.5) | 25×15 | x ~99–124 / y 83–98 (중심 90.5, 우측) |
| **BodyRow / SpeciesText**(종명) | (0,0)-(1,0), pivot(0,0) | (0, 44) | 폭 134×30 | 전폭 / y 44–74 (중앙 정렬) |
| **ProgressBackground**(진행 바) | (0,0)-(1,0), pivot(0.5,0) | (0, 27) | 110×17 | x 12–122 / y 27–44 |
| **txtSpawnTime**(남은 초 Ns) | center | (0, −65) | 40×20 | x 47–87 / y 10–30 (중심 20) |

**세로 스택(위→아래)**: 아이콘(y83–147) → 종명(44–74) → 진행 바(27–44) → 남은 초(10–30). ×N 은 아이콘 우하단(y83–98)에 겹쳐 배치. **아이콘 위 여백** = 셀 상단 170 − 아이콘 상단 147 = **23px**. **아이콘 아래 여백** = 아이콘 하단 83 − 종명 상단 74 = **9px**.

---

## 2. 도감 대비 — 셀은 값 공유·기하만 재계산

| 항목 | 도감(`CodexCell`) | 상태 셀(`SpawnerStatusCell`) | 관계 |
|---|---|---|---|
| 셀 크기 | 106×128 | 134×170 | 셀 전용 기하 |
| 아이콘 | 84×84 | 64×64 | 셀 전용 기하 |
| 배경 | #1F2937 α0.95 | #1F2937(Inner α1 + 루트 α0.85) | **동일 다크 배경** → 발광 대비 동일 |
| 틴트 계수 `IconTintByLevel` | [0, .16, .28, .40, .52, .64] | **동일(공유 SoT)** | 값 공유 |
| 발광 α `GlowOverlayAlphaByLevel` | [0, .25, .42, .58, .74, .90] | **동일(공유 SoT)** | 값 공유 |
| 스케일 `ScaleByLevel` | [1, 1.015, 1.03, 1.05, 1.07, 1.10] | **동일(공유 SoT)** | 값 공유 |
| 발광 오버레이 스프라이트 | `UISoftGlow.png`(라디얼) | **동일 스프라이트 재사용** | 신규 에셋 0 |

**결론**: 4채널의 **값·색·스프라이트는 전부 도감과 공유**(SoT `EnhanceLevelVisual` + `SpeciesVisual.SpeciesGlowColor` + `UISoftGlow.png`). 상태 셀이 셀 전용으로 확정하는 것은 **오직 배치·기하 4건**(§3~§6): 발광 오버레이 크기, 배지 위치, 스케일 pivot·헤드룸, 그리고 발광 세기 유지 판단.

---

## 3. 발광 오버레이 크기·배치 (⟨기획서 확정⟩)

**적용**: 아이콘 **뒤**(z-order상 Icon 앞 형제)에 종족색 라디얼 소프트글로우 `_glowOverlay`(Image, `UISoftGlow.png`). `_glowOverlay.color = SpeciesGlowColor(종)` + 알파만 레벨로 조절. Lv0 = 비활성.

### 결정: 76×76, 아이콘 중심 정렬

- **앵커/피벗**: center (0.5,0.5), anchoredPosition (0, 30) = **아이콘과 정확히 동일 중심**(셀 좌하단 기준 67,115). size **76×76**.
- **범위(검산)**: x 29–105, y 77–153, 반경 38.

### 근거 — "6px 링 패리티" (도감과 동일한 아우라 두께)

플레이어에게 실제로 읽히는 것은 아이콘 둘레로 삐져나오는 **발광 링의 두께**다.

- 도감: (오버레이 96 − 아이콘 84) / 2 = **6px 링**
- 상태 셀: (오버레이 76 − 아이콘 64) / 2 = **6px 링** → **도감과 동일**

즉 아이콘이 작아도(64 vs 84) 링 두께를 6px 로 맞추면 아우라가 도감과 **똑같은 두께로** 읽힌다. 이것이 "도감 값 그대로 비례 축소(64/84 × 96 ≈ 73 → 링 4.6px)"보다 나은 이유 — 비례 축소는 이미 작은 셀에서 아우라를 더 얇게 만들어 발광 신호가 약해진다(의도 반대). 76 은 절대 크기는 도감보다 작지만 **링 두께 = 헤드라인 발광 신호는 동일**.

### 검산 — 겹침 없음

- **종명 비침범**: 오버레이 중심 y115 → 종명 상단 y74 까지 거리 = 41 > 반경 38 → 종명 위치에서 발광 알파 **≈0**(라디얼 falloff). 바운딩 박스(하단 77)로도 종명(74) 위 3px, 실효 겹침 0.
- **셀 상단 비침범**: 중심 y115 → 셀 상단 170 거리 55 > 38.
- **아이콘 아우라 포함**: Lv5 아이콘 반폭 = 64×1.10/2 = 35.2 < 반경 38 → 만렙에서도 아이콘이 아우라 안에 머문다(링 2.8px, Lv0 6px).

**대안 비교**:

| 안 | 링 두께 | 판정 |
|---|---|---|
| **76×76 (채택)** | 6px = 도감과 동일 | ✅ 아우라가 도감과 같은 두께로 읽힘 |
| 96×96 (도감 값 그대로) | 16px | ✗ 링 16px → 종명(하단 67)·셀 상단(163) 침범, 셀에 과함 |
| 73×73 (비례 축소) | 4.6px | ✗ 작은 셀에서 아우라가 더 얇아져 발광 신호 약화 |

---

## 4. "Lv N" 배지 위치 (⟨기획서 확정⟩ — ×N·이름·초와 안 겹치고 구분)

**적용**: `_levelBadge`(CHText, Rule 03 §3) + 어두운 알약 칩 배경. Lv0 = 숨김(`SetActive(false)`), Lv1~5 = "Lv N".

### 결정: 우상단 코너 칩, 32×16

- **앵커/피벗**: 우상단 (1,1), anchoredPosition **(−4, −4)**.
- **크기**: **32×16**. 배경 = 알약형 칩 `#0F1520` α0.85(라운드), 텍스트 = `CHText` 흰색 "Lv N" 폰트 **11**(Jua, 도감 13 → 셀이 작아 11 로 축소).
- **범위(검산)**: 배지 우상단 피벗 = 셀 우상단(134,170) − (4,4) = (130,166) → x **98–130**, y **150–166**.

### 검산 — 기존 요소와 분리

| 기존 요소 | 위치(y / x) | 배지(y 150–166, x 98–130)와 관계 |
|---|---|---|
| **×N 노랑** | y 83–98, x 우측 | 세로 ~52px 이격(배지 하단 150 − ×N 상단 98) + 색·형태 상이 → **명확 구분** |
| **아이콘** | y 83–147(Lv0) | 배지 하단 150 > 아이콘 상단 147 → 3px 위, 본체 비침범 |
| **종명** | y 44–74 | 76px 이격 |
| **남은 초** | y 10–30 | 무관 |

**×N 과의 구분(핵심)** — 둘 다 셀 우측이지만:
1. **색**: 배지 = 다크 칩(#0F1520) + 흰 글자 / ×N = 배경 없는 **노랑(#FBBF24)** 글자.
2. **형태**: 배지 = 알약 칩 / ×N = 맨 텍스트.
3. **위치**: 배지 = **우상단 코너**(y150–166) / ×N = **우측 중단**(y83–98). 세로 52px 분리.
→ "Lv 3"(강화 레벨)과 "×3"(스폰 수)을 혼동하지 않는다.

**Lv5 스케일과의 미세 접촉**: Lv5 아이콘 우상단 = (102.2, 150.2)(§5), 배지 좌하단 = (98,150) → 겹침 x98–102.2·y150–150.2 = **4×0.2px 미세 코너**. 배지가 형제 순서 **최후미**(맨 위 렌더)라 아이콘 위로 그려져 가림 → 시각 문제 0.

**대안 비교**:

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **우상단 코너 칩 (채택)** | ×N(우중단)과 세로 분리, 아이콘 위 여백(23px) 활용 | Lv5 코너 0.2px 접촉(가림) | ✅ |
| 좌상단 코너 | ×N 과 완전 반대편 | 좌상단은 아이콘 좌상단(x35,y147)과 대칭 겹침 동일, 코너 관습(등급=우상단) 약화 | △ |
| 아이콘 아래(진행 바 위) | ×N 과 멀다 | y74–83 구간 9px뿐 → 칩(16) 안 들어감, 종명·바 침범 | ✗ |

---

## 5. 스케일 헤드룸 (⟨기획서 확정⟩ — pivot·여백)

**적용**: `_iconRect.localScale = Vector3.one * ScaleByLevel[lv]`. `_iconRect` = 중앙 Icon 의 RectTransform.

### 결정: pivot center (0.5,0.5) 유지 · 상한 1.10 유지 (도감 값 그대로 공유)

- **pivot**: 아이콘 기존 pivot **(0.5,0.5) 그대로 — 변경 없음**. (도감은 pivot (0.5,1) 상단 고정으로 아래로 자라게 했으나, **상태 셀은 그럴 필요 없음** — 아래 검산.)
- **스케일 배열**: 공유 SoT `ScaleByLevel [1, 1.015, 1.03, 1.05, 1.07, 1.10]` **그대로**(상한 1.10 유지, 셀 헤드룸 충분 → 도감보다 낮출 이유 없음).

### 검산 — Lv5(1.10) 중심 스케일, 사방 여유

Lv5 아이콘 = 64×1.10 = **70.4px**, 중심(67,115) 고정, 사방 +3.2px:

| 방향 | Lv5 아이콘 경계 | 이웃 요소 | 여유 |
|---|---|---|---|
| **위** | 상단 y 147+3.2 = **150.2** | 셀 상단 170 | 19.8px ✓ (배지 150 은 §4 대로 가림) |
| **아래** | 하단 y 83−3.2 = **79.8** | 종명 상단 74 | **5.8px** ✓ |
| **좌우** | x 35−3.2=31.8 ~ 99+3.2=**102.2** | 셀 134 / ×N 좌 99 | 좌우 여백 31.8px, ×N 은 아이콘 위(형제 후순위)라 가림 ✓ |

가장 빡빡한 제약(아래 5.8px) 이 양수 → **1.10 유지 가능**. 도감은 이름텍스트까지 1.6px 였으나(pivot 상단 필요), 상태 셀은 아이콘 아래 9px 여백 + 중심 스케일이라 **더 넉넉**(5.8px). 따라서 도감 값 그대로 공유가 검산으로 성립 → 상한을 1.06 등으로 낮출 필요 없음(SoT 유지).

**발광 오버레이와의 관계**: 오버레이 76 고정, Lv5 아이콘 70.4 < 76 → 만렙에도 아이콘이 아우라 안(§3). "빛을 뚫고 살짝 커진 생물" 연출 유지.

**대안 비교**:

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **center pivot · 상한 1.10 (채택)** | 도감 배열 그대로 공유(SoT 무변경), 사방 여유, prefab pivot 변경 0 | 아래 여유 5.8px(빡빡하진 않음) | ✅ |
| pivot 상단(0.5,1)·1.10 | 아래로만 자람 → 위 여백 낭비 | 아래 여유 = 9−7.04 = 1.96px 로 오히려 빡빡 | ✗ |
| 상한 1.06 하향 | 아래 여유 10px+ | 도감 SoT 와 배열 분기 → drift, "도감=셀" 깨짐 | ✗ |

---

## 6. 발광 알파/틴트 세기 (⟨기획서 확정⟩ — 도감 값 공유 유지)

### 결정: 도감 매핑 값 그대로 공유 — 셀별 조정 계수 도입 안 함

틴트 `[0,.16,.28,.40,.52,.64]` · 발광 α `[0,.25,.42,.58,.74,.90]` 를 **공유 SoT `EnhanceLevelVisual` 값 그대로** 사용. 상태 셀 전용 감쇠 계수(예: α×0.8)를 **도입하지 않는다**.

**근거**:
1. **SoT drift 방지(락 결정)** — spec §3.3 이 매핑을 공유 SoT 로 락함("값을 바꾸면 도감도 바뀜"). 셀별 계수를 두면 두 곳의 표현이 갈려 "도감=전장=셀" 일치가 깨진다.
2. **작은 셀 우려는 기하로 이미 해소** — "작아서 과하다" 우려의 실체는 아우라 **면적**인데, 오버레이를 76(링 6px, §3)으로 잡아 도감과 **같은 링 두께**로 맞췄다. 단위 면적당 밝기(알파)는 같고 총 면적은 작으므로 절대 발광량은 오히려 도감보다 작다 → 과하지 않다.
3. **동일 배경** — 셀 배경 #1F2937(§2)이 도감 #1F2937 과 같아 같은 알파가 같은 대비로 읽힌다.
4. **전장·상점 호응** — 값을 공유해야 전장 몬스터 발광·상점 강화 셀·도감·상태 셀이 한 색·한 곡선으로 묶인다(§8).

**만약 육안 검증에서 과하다면(에스케이프 해치, 지금은 미사용)**: 장래에 `EnhanceLevelVisual.Apply` 에 선택적 `float alphaScale = 1f` 파라미터를 **추가**해 호출부에서만 곱하는 방식이 가능(배열 SoT 는 불변 유지). **단 본 기획의 `Apply` 시그니처(§10)에는 이 파라미터를 포함하지 않는다** — 현 결정은 **무조정 공유(alphaScale 없음)**. Task 3 목업/Play 육안(§목업 게이트)에서 과함이 확인될 때만 사용자 승인 하에 시그니처를 확장한다.

---

## 7. 레벨 소스 배선 (⟨기획서 확정⟩)

### 결정: 바인드 시 조회 — `RebindSnapshot` 에서 `MetaSession.GetOrLoad().GetShopLevel("Enhance_" + snapshot.CurrentType)`

- **키 형식**: `"Enhance_" + snapshot.CurrentType`(EMonster.ToString → "Enhance_Wisp" 등). 도감 `CodexPopup` 이 쓰는 `profile.GetShopLevel("Enhance_" + type)` 와 **글자 그대로 동일**(SoT 정합, 확인: `CodexPopup.cs:184`).
- **호출 시점**: `RebindSnapshot` 끝(종족·수 변경 시마다 재조회). 종족이 카드로 바뀌면 새 종족 레벨을 재조회.

**근거 & 대안**:

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **바인드 시 조회 (채택)** | 스냅샷/VM 무변경, `CurrentType` 이미 스냅샷에 있음, 셀 기존 로컬 리졸버(`SpeciesSprite`·`SpeciesColor`·`SpeciesName`)와 동일 패턴 | 셀(View)이 `MetaSession` 직접 조회(경미한 MVVM 냄새) | ✅ |
| 스냅샷 필드에 레벨 적재 | View 가 세션 미조회 | `SpawnerSnapshot`·VM 배관 추가(전투 중 불변인데 과설계) | ✗ |

- **전투 중 불변**: 강화는 마을 상점에서만 발생 → 전투 진입 후 레벨 고정. 어느 방식도 정합하나, **바인드 시 조회가 최소 변경**.
- **MVVM 참고**: 셀은 이미 `SpeciesVisual`(static SoT)을 직접 읽는다 → `MetaSession`(세션 접근자) 조회도 같은 결의 로컬 리졸브라 기존 셀 아키텍처와 일관. plan Task 2 는 "MetaSession 접근이 셀에서 부적절하면 Panel 이 Bind 인자로 전달"하는 fallback 을 허용하나, **본 기획서 SoT 권장은 바인드 시 조회**다(단일 갈래). gameplay-programmer 가 fallback 을 택할 경우 레벨 값·키·표현은 불변(배선 위치만 상이).

---

## 8. 전장 몬스터 발광과 상태 셀의 호응

전장의 실제 몬스터는 `MonsterEnhancementVisual.ApplyLevel(level, species)` 로 `SpeciesGlowColor(species) × emission[level]` 발광한다(레벨은 `ApplyMetaBonuses` → `GetShopLevel("Enhance_"+type)`). 상태 셀도 **같은 레벨 소스**(§7)와 **같은 색 SoT**(`SpeciesGlowColor`)를 읽는다.

- **같은 색만이 아니라 같은 레벨 티어**: 전장 몬스터와 상태 셀이 둘 다 `GetShopLevel("Enhance_"+type)` 를 읽으므로, 필드에서 밝게 빛나는 종족은 HUD 셀도 **같은 밝기 티어**로 빛난다(단순 색 일치가 아니라 밝기 일치). 플레이어가 "저 무리를 키웠다"를 필드·HUD 양쪽에서 동일 신호로 읽는다.
- **역할 분담**: 전장 발광 = 전투 중 "지금 강한 무리" 즉각 인지 / 상태 셀 4채널 = 스폰 슬롯별 "어느 종을 얼마나 키웠나"를 정적으로 요약. 같은 색·레벨로 묶여 두 화면이 한 언어로 읽힌다.
- **종색 이중 신호**: 셀 테두리(`_border`)도 이미 종족색이다(§1). 여기에 종족색 아우라(§3)가 더해져 종족 식별이 강화되되, 발광은 레벨>0 일 때만 켜져 "강화 여부"를 추가로 전달(테두리=종 식별, 아우라=강화 신호).

---

## 9. 미강화(Lv0) / 비몬스터 상태 처리

| 셀 상태 | 아이콘 틴트 | 발광 오버레이 | 배지 | 스케일 |
|---|---|---|---|---|
| 종족 + 강화(Lv1~5) | 종족색 Lerp(§6) | α by Lv(§3·6) | "Lv N"(§4) | by Lv(§5) |
| 종족 + 미강화(Lv0) | white(원본) | off | 숨김 | 1.0 |
| 아이콘 없음(스프라이트 미해결) | (아이콘 숨김) | off | 숨김 | 1.0 |

- **Lv0 = 담백**: 발광·배지·틴트·스케일 전부 항등 → 미강화 종은 원본 그대로. 강화한 종만 셀에서 도드라진다.
- **풀 재사용 리셋(락)**: `RebindSnapshot` 이 매 바인드마다 4채널을 **전부 재설정**(Lv0 이면 명시 off). `OnEnable` 의 기존 리셋(아이콘 sprite/active)에 오버레이 off·배지 off·스케일 1·icon.color white 를 추가하되, **실제 재설정 소유권은 `RebindSnapshot` 의 `Apply` 호출**(RecordsStageCell 교훈 — OnEnable 리셋 의존 금지, 바인드가 소유). 종족 전이 시 이전 종족의 배지·발광·스케일 잔상 0.

---

## 10. 구현 요청사항 (gameplay-programmer 용)

> 시그니처·파일 구조·TDD 는 plan 이 SoT. 아래는 도메인 값·기하·배선의 확정.

### Enum (Rule 02 §8)
- **신규 Enum 없음**. 기존 `EMonster` 재사용.

### Interface / static (공유 SoT)
- **신규 `Lair.UI.EnhanceLevelVisual`**(plan Task 1 이 SoT):
  - `public static readonly float[] IconTintByLevel = {0f,.16f,.28f,.40f,.52f,.64f}`
  - `public static readonly float[] GlowOverlayAlphaByLevel = {0f,.25f,.42f,.58f,.74f,.90f}`
  - `public static readonly float[] ScaleByLevel = {1f,1.015f,1.03f,1.05f,1.07f,1.10f}`
  - `public const int MaxLevel = 5`
  - `public static void Apply(int level, EMonster species, Image icon, Image glowOverlay, CHText levelBadge, RectTransform iconRect, Color baseIconColor)` — 값은 도감 `CodexCell.ApplyEnhancement` 의 4채널 로직과 **완전 동일**(도감 동작 불변). 각 위젯 null 가드.
- **신규 인터페이스 없음**. `SpeciesVisual.SpeciesGlowColor(EMonster) → Color`(단일 SoT) 읽기만.

### 에셋 키 (신규 0)
- 발광 오버레이 스프라이트 = **기존 `Assets/_Lair/Art/Sprites/UISoftGlow.png`**(도감 작업 L 신설분 재사용, 인스펙터 직접 참조 — 아이콘 리졸버와 동일 관례, Addressables 아님). **신규 에셋 제작 0**.

### 전제 조건 — 아이콘 스프라이트 정합 (확인 완료)
- `_icon` 이 참조하는 6종 스프라이트(`_wispIcon`~`_phantomIcon`) = **도감·상점과 동일한 투명배경 `MonsterIcons/*.png` 렌더**. 확인: `SpawnerStatusCell.prefab` 의 `_wispIcon` guid `c57bbfb5…` = `MonsterIcons/Wisp.png`, `_reaperIcon` guid `de04e176…` = `MonsterIcons/Reaper.png`(도감 `CodexPopup.prefab`·`ShopPopup.prefab` 이 참조하는 동일 파일). 도감 작업 L 이 이 PNG 들을 **투명배경 원본 렌더로 in-place 재베이크(GUID 보존)** 했으므로, 상태 셀도 자동으로 투명배경 아이콘을 쓴다 → 아이콘 **뒤** 발광 아우라가 생물 실루엣 주변으로 새어 "도감과 동일한 표현"이 성립(불투명 사각 배경이면 6px 발광 링이 사각 매트로 깨짐). **재배선 불필요**(이미 정합).

### SO 스키마 / 데이터 필드
- **신규 필드 없음**. 레벨은 `RebindSnapshot` 에서 `MetaSession.GetOrLoad().GetShopLevel("Enhance_" + snapshot.CurrentType)` 조회(§7). `SpawnerSnapshot` 무변경.

### SpawnerStatusCell 위젯 필드 (`[SerializeField] private`, Rule 02 §6.1)
- `_glowOverlay` (Image) — 아이콘 뒤 종족색 발광 아우라(§3). 스프라이트 = `UISoftGlow.png`. 초기 비활성.
- `_levelBadge` (CHText) — 우상단 "Lv N" 배지(§4). 초기 비활성.
- `_iconRect` (RectTransform) — 스케일 대상 = 기존 `_icon`(Icon)의 RectTransform(§5).

### RebindSnapshot 적용 로직
```
int lv = Mathf.Clamp(MetaSession.GetOrLoad().GetShopLevel("Enhance_" + snapshot.CurrentType), 0, 5);
EnhanceLevelVisual.Apply(lv, snapshot.CurrentType, _icon, _glowOverlay, _levelBadge, _iconRect, Color.white);
```
- `baseIconColor` = `Color.white`(상태 셀 아이콘은 항상 원본 흰색 기준 — 도감의 실루엣/색칩 분기 없음). Lv0 → `Apply` 내부에서 오버레이/배지 off·스케일 1·icon.color white.

### 프리팹 배선 (plan Task 3 — ⛔ 목업 승인 게이트 선행)
`SpawnerStatusCell.prefab` 에 자식 추가·배선. **Z-order(형제 순서) load-bearing**:

- **`_glowOverlay`**: **InnerBackground 와 Icon 사이**에 삽입(아이콘 뒤·다크 배경 위). 앵커/피벗 center(0.5,0.5), anchoredPosition **(0, 30)**(= 아이콘 중심), size **76×76**, `UISoftGlow.png`, 초기 `SetActive(false)`.
- **`_levelBadge`**: 형제 순서 **최후미**(맨 위 렌더 → ×N·아이콘 위에 그려짐). 우상단 (1,1) 피벗, anchoredPosition **(−4,−4)**, 칩 배경 Image `#0F1520` α0.85 라운드 **32×16** + 자식 CHText 흰색 "Lv N" 폰트 **11**(Jua). 초기 `SetActive(false)`.
- **`_iconRect`**: 기존 Icon GameObject 의 RectTransform 배선(pivot (0.5,0.5) **변경 없음**).

---

## 11. 테스트 관점 (plan Task 1·2 와 정합)

- **매핑 SoT 회귀**: `EnhanceLevelVisual.IconTintByLevel/GlowOverlayAlphaByLevel/ScaleByLevel` 길이 6·Lv0 항등·단조 증가·Lv5 상한(0.64/0.90/1.10). 도감 값 무변경(작업 L `CodexEnhanceMappingArrayTests` 참조를 `EnhanceLevelVisual.*` 로 갱신).
- **`Apply` 계약**: lv0 → 오버레이/배지 비활성·스케일 1·icon.color = baseIconColor / lv5 → α0.90·스케일 1.10·배지 "Lv 5"·틴트 Lerp(white, glow, 0.64). 더미 Image/RectTransform 로 검증.
- **레벨 조회 키**: `"Enhance_" + type` 형식(도감과 동일 문자열).
- **회귀**: 진행 바·×N·이름·남은 초·클릭 무변경.

---

## 12. Self-Review

- **Placeholder 잔존 0**: 미정 마커·애매한 권유·두 갈래 위임 없음. 5개 열린 결정(발광 크기 76·배지 우상단 32×16·스케일 center/1.10·발광값 공유·레벨 바인드 조회) 전부 단정 + 실측 검산(6px 링 패리티, Lv5 아래 여유 5.8px, 배지 vs ×N 세로 52px 분리, 종명 거리 41>반경 38). 발광 세기 에스케이프 해치는 "기본 1.0 무조정" 으로 단정, 사용 조건 명시.
- **스펙 커버리지**: spec §5 열린 4항목(발광/배지 배치·스케일 헤드룸·발광 세기·레벨 소스) → §3/§4/§5/§6/§7 전부 닫음. §3.1 4채널 → §2·§10. §3.3 공유 SoT → §2·§6·§10. §3.4 Lv0 off → §9. §3.5 풀 리셋 바인드 소유 → §9. 갭 0.
- **내부 일관성**: 오버레이 76(§3·§10), 배지 32×16 우상단(−4,−4)(§4·§10), 스케일 center·1.10(§5·§10), 매핑 값(§2·§6·§10·§11 동일), 레벨 키 `"Enhance_"+type`(§7·§10·§11). 본문·표·구현요청·테스트 전부 동일 수치.
- **시그니처/명명 일관성**: `EnhanceLevelVisual.IconTintByLevel/GlowOverlayAlphaByLevel/ScaleByLevel/Apply`, `SpawnerStatusCell._glowOverlay/_levelBadge/_iconRect`, `MetaSession.GetOrLoad().GetShopLevel`, `SpeciesVisual.SpeciesGlowColor`, `"Enhance_"+snapshot.CurrentType`, `UISoftGlow.png` — plan·기존 코드와 글자 그대로 일치(Grep 확인: `GetShopLevel`·`Enhance_`·`SpeciesGlowColor` 실존, `UISoftGlow.png` 실존).
- **모호 표현 0**: 발광 세기·레벨 소스 등 두 갈래 후보를 각각 한 갈래로 단정. fallback 은 "값 불변, 위치만" 으로 명시.
- **스코프**: 단일 구현 단위(공유 헬퍼 추출 + 상태 셀 4채널 배선). 분할 불필요.
- **구현 요청사항 완전성**: Enum(없음)·Interface(EnhanceLevelVisual, `Apply` 시그니처에 alphaScale **미포함**으로 §6과 정합)·에셋 키(UISoftGlow 재사용)·SO 스키마(신규 없음)·위젯 필드·배선 로직·프리팹 z-order·**아이콘 스프라이트 전제(도감 동일 MonsterIcons 정합 확인 완료)** 전부 명세.
- **UI 목업**: `.mockups/ingame-spawner-cell-enhance-level.html` 작성 — 실측 134×170 셀 6칸을 real `SpeciesGlowColor` hex(#28E66E 등)·발광 α·틴트·스케일·배지로 렌더, 종족별 레벨 슬라이더로 Lv0~5 겹침 없음 검증. z-order(발광=아이콘 뒤, 배지=맨 위, ×N=아이콘 위) 반영.

판정: **통과** (신규 에셋 0건 — 도감 SoT·기존 `UISoftGlow.png` 재사용).
