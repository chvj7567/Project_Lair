# 마을 메타 가시화 — 던전 강화 요약 + 도전과제 진행도 기능 기획서

> 작성: game-designer
> 작성일: 2026-06-12
> 입력 spec: `docs/superpowers/specs/2026-06-12-village-meta-visibility-design.md`
> 입력 plan: `docs/superpowers/plans/2026-06-12-village-meta-visibility.md`
> 상위 기획서: `docs/design/village-meta-hub.md` (마을+메타 본 기획서 — §3.2 상점 7품목 / §7 문자열 표기 컨벤션 rev4·rev5)
> 현행 코드: `Assets/_Lair/Scripts/Meta/MetaBattleBonus.cs`, `Assets/_Lair/Scripts/UI/Village/{ShopPopup,ShopItemCell,QuestPopup,QuestCell}.cs`
> **본 기획서가 이 기능(던전 강화 요약줄 · 도전과제 진행도)의 모든 표기 문구·라벨·반올림 표시 규칙의 단일 진실(SoT)이다.** plan 의 잠정 라벨/문구는 본 문서 확정값으로 교체한다.

---

## § 헤더

- **목표**: 마을 안에서 메타 성장을 *읽히게* 한다 — ① 상점 팝업 상단에 "현재 던전 강화" 누적 효과를 스탯별 퍼센트 한 줄(들)로, ② 도전과제 팝업의 누적형 셀에 "현재/목표" 진행 바를 붙인다. **밸런스 수치·저장 필드·JSON id 추가 0건의 표시 전용 기능**이다.
- **검증 가설**: **"런 사이 메타 성장이 재방문 동기를 만드는가"** 의 보강 — 성장의 *체감*(구매가 누적으로 얼마나 쌓였는지)과 *장기 목표 가시성*(다음 런을 누르면 도전과제가 몇 칸 차는지)을 마을 안에서 직접 노출해, 가설의 절반("구매가 다음 런에서 체감되는가" · "장기 목표가 다음 런 이유가 되는가")을 전투 진입 없이 마을에서 닫는다.
- **현재 단계 범위 적합성**: 범위 내. v0.2 마을+메타(2026-06-10 승격)의 가시화 후속이며, spec §2 사용자 선택 락(상점 팝업 상단 요약줄 / 스탯별 % 나열 / 누적형만 진행 바) 그대로. 신규 리소스·저장 필드·서버 0건.
- **핵심 메커니즘**:
  1. **던전 강화 요약줄** — `MetaBattleBonus` 집계 배율을 재사용해 `cfg.ShopItems` 순서로 레벨>0 품목만 라벨+양수 % 로 환산(감소형 mul 은 역수). `ShopPopup` 상단에 한 줄로 표기, 구매 직후 자동 갱신.
  2. **도전과제 진행 바** — 누적형(`TotalWins`·`TotalRuns`, 단 Threshold≥2)만 `현재/목표` 진행 바를 표시. 달성 시 진행 바를 숨기고 기존 "달성" 뱃지로 대체(상호 배타).

---

## 1. 디자인 원칙 (이 기능의 결정 기준)

- **표시 전용 — 단일 출처 강제**: 요약줄 % 는 전투 적용과 *같은* `MetaBattleBonus` 집계 배율을 읽는다(중복 환산 금지, Rule 02 §5). 화면 숫자와 전투 효과가 어긋날 원천을 차단한다.
- **문구는 본 기획서가 SoT**: 라벨·접두·구분자·반올림은 전부 **코드 리터럴**(상위 기획서 §7 rev4 ②표 규칙 — `Strings_Ko.json` id 추가 없음). 본 문서의 표가 글자 단위 진실이며, 문구 변경 시 본 문서와 코드 리터럴을 함께 갱신한다.
- **부호는 항상 "강해짐 = 양수"**: 감소형 스탯(공속/둔화/스폰률은 mul<1)도 플레이어에겐 "강화"다. `(1/mul − 1)` 로 환산해 화면엔 `+%` 만 보이게 한다. "−1.5%/Lv" 같은 내부 방향은 노출하지 않는다.
- **두 표기 표면은 분리**: 요약줄의 짧은 라벨(HP/공격/…)은 §3.2 상점 셀의 `ShopItemDef.Description`("모든 몬스터 HP +2%/Lv")과 **다른 표면**이다. 셀 설명은 "레벨당 효과·대상 범위" 풀 문장, 요약줄은 "누적 결과"의 압축 토큰. 본 문서는 요약줄 라벨만 소유한다.
- **톤 일관**: 상위 기획서 §7(소울 잔액 `N 소울`, 수량 `+N 소울`)·§9.1(접두 뒤 더블 스페이스 `보상  소울 +212`, 구분자 ` · `)의 컨벤션을 그대로 잇는다.

---

## 2. ① 던전 강화 요약줄 (상점 팝업 상단)

### 2.1 스탯별 라벨 7종 (확정 — 표시 SoT)

`DungeonPowerSummary.Build` 가 `cfg.ShopItems` 순서로 순회하며 레벨>0 품목을 아래 라벨 + 환산 % 로 환산한다. plan 잠정 라벨(HP/공격/공속/이동/사거리/둔화/스폰률)을 **전건 확정 채택**한다 — desync 없음(§6 sync delta).

| 순서 | 상점 품목 Id | StatKind / EffectKind | **요약줄 라벨(확정)** | 환산 방향 | 만렙 % 검산 (산식) |
|---|---|---|---|---|---|
| 1 | `MonsterHpUp` | Hp | **HP** | 증가형 `(mul−1)` | `1.02⁵−1 = 0.1041` → **+10%** |
| 2 | `MonsterPowerUp` | Power | **공격** | 증가형 `(mul−1)` | `1.015⁵−1 = 0.0773` → **+8%** |
| 3 | `MonsterAtkSpeedUp` | Cooldown | **공속** | 감소형 `(1/mul−1)` | `1/0.99⁵−1 = 1/0.9510−1 = 0.0515` → **+5%** |
| 4 | `MonsterMoveSpeedUp` | MoveSpeed | **이동** | 증가형 `(mul−1)` | `1.015⁵−1 = 0.0773` → **+8%** |
| 5 | `MonsterRangeUp` | Range | **사거리** | 증가형 `(mul−1)` | `1.015⁵−1 = 0.0773` → **+8%** |
| 6 | `PlagueVenomUp` | SlowFactor | **둔화** | 감소형 `(1/mul−1)` | `1/0.98⁵−1 = 1/0.9039−1 = 0.1063` → **+11%** |
| 7 | `SpawnerHasteUp` | SpawnerPeriod | **스폰률** | 감소형 `(1/mul−1)` | `1/0.985⁵−1 = 1/0.9272−1 = 0.0785` → **+8%** |

- **부호 일치 확인**: 7항목 만렙 % 가 모두 양수 — §3.2 의 효과 방향(공속↑·둔화 강화·스폰률↑)과 일치. 감소형 3종(공속/둔화/스폰률)은 mul<1 이라 `(1/mul−1)` 환산이 필수(spec §7 리스크).
- **라벨 채택 근거**: 모두 2~3자 압축 토큰 — 한 줄에 7개를 나열해도 화면 폭을 넘기지 않게 짧게 잡았다(§2.3 폭 검산). "공격력"→"공격", "이동속도"→"이동", "사거리"는 이미 3자라 유지. 셀 설명(§3.2 Description)이 풀 문장을 담당하므로 요약줄은 의도적으로 최소 토큰.
- **둔화 대상 비대칭 — 한정자 없이 "둔화"로 확정**: `PlagueVenomUp` 은 역병귀 한정(다른 6품목은 전 몬스터)이나, 요약줄은 7품목을 동일 형식으로 다룬다(spec §2 락). 한정자("둔화(역병)")를 붙이면 토큰이 길어지고 다른 6항목과 표기 비대칭이 생긴다 → **플랫 "둔화"로 표기**. 대상 범위는 상점 셀 설명("역병귀 둔화 +2%/Lv 강화")이 이미 명시하므로 정보 손실 없음.

### 2.2 요약줄 문구 포맷 (확정 — 표시 SoT)

`ShopPopup` 이 `DungeonPowerSummary.Build` 결과를 한 문자열로 조립한다(라벨 join 은 표시 책임 → 팝업 담당, plan Task 2 Step 3).

| 요소 | 확정값 | 비고 |
|---|---|---|
| 접두 | `현재 강화` + **더블 스페이스(2칸)** | §9.1 `보상  …`·`현재 강화  …` 더블 스페이스 컨벤션 일치 |
| 항목 토큰 | `{라벨} +{퍼센트}%` | 예: `HP +10%` · `공속 +5%` (퍼센트 사이 공백 없음, `+`·`%` 밀착) |
| 구분자 | ` · ` (스페이스 + 가운뎃점 U+00B7 + 스페이스) | §9.1 `소울 +212 · XP +100` 와 동일 구분자 |
| 강화 0건 | `현재 강화  아직 없음` | 접두 + 더블 스페이스 + `아직 없음` (베이스라인 문구) |

**조립 예시 (확정 표기 — 코드 리터럴이 글자 단위로 동일해야 함)**:

| 상황 | 표기 결과 |
|---|---|
| 강화 0건 | `현재 강화  아직 없음` |
| HP 만렙만 | `현재 강화  HP +10%` |
| HP 만렙 + 공속 만렙 + 스폰률 만렙 | `현재 강화  HP +10% · 공속 +5% · 스폰률 +8%` |
| 전 7품목 만렙 (최대 길이) | `현재 강화  HP +10% · 공격 +8% · 공속 +5% · 이동 +8% · 사거리 +8% · 둔화 +11% · 스폰률 +8%` |

- **갱신 시점**: `ShopPopup.Rebuild()` 에서 세팅 — 구매 성공 시 `HandleBuy → Rebuild` 경로로 **즉시 재표기**(현행 `_soulText` 와 동일 갱신 경로). 별도 이벤트 불요.
- **`소울` 단위 미사용**: 요약줄은 % 만 다루므로 소울 단위 표기(§7 `N 소울`)와 무관 — 혼동 방지.

### 2.3 표시 폭 / 오버플로우 거동 (확정 — plan delta)

> **plan delta**: plan Task 5 Step 1 의 "한 줄 + Overflow Ellipsis" 는 본 §2.3 **"word-wrap 1~2줄 가변, ellipsis 금지(잘림 없음)"** 로 대체한다(사유 아래). plan↔기획서 sync 규칙에 따라 plan 에 delta 보강 필요.

- **최대 길이 검산**: 전 7품목 만렙 줄 = 접두 `현재 강화` (5자) + 더블 스페이스(2) + 7토큰 + 구분자 6개. 토큰 문자 수 = `HP +10%`(7) `공격 +8%`(7) `공속 +5%`(7) `이동 +8%`(7) `사거리 +8%`(8) `둔화 +11%`(8) `스폰률 +8%`(8) = 52자. 구분자 ` · ` 6개 = 18자. 합계 = 5 + 2 + 52 + 18 = **77자**(공백 포함, CJK·ASCII 혼합) — 모달 팝업 1줄 가용 폭을 넘기는 길이.
- **결정 — ellipsis 금지, 줄바꿈으로 전체 노출**: plan 의 ellipsis 를 쓰면 **trailing 항목(스폰률·둔화 등)이 잘려 §1 "성장 체감" 의도를 정면으로 깨뜨린다**(최고가 150 품목 `SpawnerHasteUp` 의 성과가 안 보임). 따라서:
  - TMP `enableWordWrapping = true`, `overflowMode = Overflow`(잘림 없음) — 폭에 따라 **1~2줄로 가변**, 줄 수 상한을 강제하지 않고 항상 전체 토큰을 노출한다(잘림 없음이 보장).
  - 줄바꿈은 토큰 경계(` · ` 의 공백)에서 발생 — 한 토큰(`사거리 +8%`)이 두 줄로 쪼개지지 않게 TMP word-wrapping 의 공백 기준 분리에 의존(토큰 내부엔 줄바꿈 유발 공백이 `라벨`과 `+%` 사이 1칸뿐이라, 폭이 충분하면 토큰 단위 wrap 됨).
- **요약줄 노드 높이**: 콘텐츠 줄 수에 따라 늘어나는 **세로 가변 영역**(prefab 의 `_bonusSummaryText` 노드는 상단 고정 + 콘텐츠 높이 따라 아래 스크롤 영역이 밀리도록 레이아웃, §5.1 빌더 명세). 1줄 케이스(강화 0건/소수 항목)가 일반적이며, 전 만렙 근접 시에만 2줄로 확장 — `overflow=Overflow` + 가변 높이라 어느 경우에도 잘림이 발생하지 않는다.

### 2.4 반올림 표시 규칙 (확정 — 명문화)

| 규칙 | 확정 | 근거 |
|---|---|---|
| 반올림 단위 | **정수 % (`Mathf.RoundToInt`)** — 소수점 표기 없음 | plan 채택. % 한 줄 압축 표기에 소수점은 노이즈. (현 7품목 만렙 % 는 x.5 경계에 정확히 걸리는 값이 없어 반올림이 안정적 — 가장 근접한 HP 10.41 도 10.5 까지 0.09 여유) |
| 0% 항목 제외 | **반올림 결과 0% 인 항목은 요약줄에서 제외** | plan 채택. "+0%" 는 강화 체감 0 → 노출 가치 없음 |
| 0% 제외 발화 검산 | **현 7품목 수치에선 0% 가 절대 발생하지 않음** — 레벨 1 최소 변화도 ≥1%(예: Power Lv1 = `1.015−1 = 1.5%` → 반올림 +2%, 공속 Lv1 = `1/0.99−1 = 1.01%` → +1%). 따라서 0% 제외 절은 **방어적 가드**(세이브 변조·미래 미세 수치 대비)이며 현행에선 dead branch 가 아니라 "발화 안 함이 정상" | 의도된 방어 — 죽은 코드 아님 |

- **검산 — 레벨 1(최저 강화)도 반올림 ≥1%**: 7품목 중 반올림 % 가 가장 작은 건 공속 Lv1 = `1/0.99−1 = 0.0101` → 반올림 **+1%**. 모든 품목이 Lv1 부터 ≥1% 표기 → 구매 즉시 요약줄에 반영(체감 끊김 없음).

---

## 3. ② 도전과제 진행도 (퀘스트 셀)

### 3.1 진행 대상 판정 (확정 — FirstRun carve-out 포함)

> **숨은 함정 해소**: §5.2 #1 `FirstRun` 은 `Condition=TotalRuns, Threshold=1` 이다. plan 잠정 룰(`cumulative = TotalWins || TotalRuns`)을 그대로 쓰면 첫 런 전(TotalRuns=0) `FirstRun` 이 **"0/1" 진행 바**로 뜨고 1런 후 뱃지로 바뀐다. 1/1 짜리 진행 바는 "장기 목표 가시화"(spec §1) 가 아니라 노이즈다 → **Threshold≥2 carve-out** 으로 비누적 취급.

진행 바 표시 조건 = **다음 3개 AND**:

| 조건 | 판정식 | 사유 |
|---|---|---|
| 누적형 조건 | `Condition == TotalWins \|\| Condition == TotalRuns` | spec §2 락 — 시간형/시너지/첫승은 비대상 |
| 장기 목표 | `Threshold >= 2` | `FirstRun`(TotalRuns/1)·기타 1짜리 누적형 제외 — 1/1 진행 바는 무의미 |
| 미달성 | `AchievedIds.Contains(def.Id) == false` | 달성 시 기존 "달성" 뱃지로 대체(상호 배타, §3.3) |

- 위 3조건을 모두 만족하면 `QuestCellData.HasProgress = true`. 그 외(비누적형·Threshold 1·이미 달성)는 `HasProgress = false` → 현행 동작 불변(진행 바 노드 비활성).
- **현 13개 도전과제 적용 결과(상위 기획서 §5.2)**:

| Id | Condition | Threshold | 진행 바? | 사유 |
|---|---|---|---|---|
| `FirstRun` | TotalRuns | 1 | **없음** | Threshold<2 carve-out |
| `Runs10` | TotalRuns | 10 | **있음** | 누적형 + 장기 |
| `Runs30` | TotalRuns | 30 | **있음** | 누적형 + 장기 |
| `Wins5` | TotalWins | 5 | **있음** | 누적형 + 장기 |
| `Wins10` | TotalWins | 10 | **있음** | 누적형 + 장기 |
| `Wins25` | TotalWins | 25 | **있음** | 누적형 + 장기 |
| `Wins50` | TotalWins | 50 | **있음** | 누적형 + 장기 |
| `FirstWin` · `Win120/90/60` · `SynergyTier2/3` | FirstWin / WinUnderSeconds / SynergyTierReached | — | **없음** | 비누적형 — 현행 바이너리 유지 |

→ 진행 바 대상은 **6개**(Runs10·Runs30·Wins5·Wins10·Wins25·Wins50). 나머지 7개는 현행 달성/미달성 바이너리 유지.

### 3.2 진행 텍스트 포맷 (확정 — 표시 SoT)

| 요소 | 확정값 | 근거 |
|---|---|---|
| 진행 텍스트 | `{현재}/{목표}` (예: `12/25`) | plan 잠정값 채택. 슬래시 구분자, 좌우 공백 없음 |
| 천 단위 콤마 | **미사용** | 최대 목표값 = `Wins50` 의 50 → 자릿수 ≤2. §7 의 콤마 규칙은 **소울 한정**(`1,234 소울`), 진행 카운트는 콤마 없음 — 표기 표면 구분. 최대 케이스 검산: `50/50` (콤마 발생 자릿수 미도달) |
| 현재값 클램프 | `현재 = min(원시 누적값, 목표)` | 세이브 변조·달성 플래그 누락 방어. 표시상 `현재 ≤ 목표` 보장(예: 누적값 ≥ 목표인데 `AchievedIds` 에 플래그가 없는 불일치 세이브 — `Runs10` 셀에 TotalRuns=30 이면 `10/10` 으로 클램프 표기. plan `현재값은_목표를_넘지_않도록_클램프된다` 테스트 케이스) |

- **콤마 미사용 검산**: 진행 바 대상 6개 중 최대 목표 = 50(`Wins50`). 최대 표기 = `50/50`(달성 직전 클램프 케이스는 §3.3 으로 진행 바 미표시이므로 실제 표기 상한은 `49/50`). 두 경우 모두 4자 이하 → 천 단위 콤마 진입 불가. 따라서 콤마 미사용은 가정이 아니라 검산된 사실.

### 3.3 진행 바 거동 (확정 — 명문화)

| 항목 | 확정 | 비고 |
|---|---|---|
| 표시 조건 | `HasProgress == true` 일 때만 진행 바 루트(`_progressRoot`) 활성 | §3.1 3조건 AND |
| fill 비율 | `_progressFill.fillAmount = Target>0 ? (float)Current/Target : 0f` | Image type=Filled / Horizontal. Current 는 §3.2 클램프값이라 fill ≤ 1 보장 |
| 달성 시 거동 | 진행 바 숨김 → 기존 "달성" 뱃지(`_achievedBadge`)로 대체 | **상호 배타** — 진행 바와 달성 뱃지가 동시에 보이지 않음(spec §3.2) |
| 비누적형/Threshold1 | 진행 바 노드 비활성 — 현행 표시(이름/설명/보상 + 미달성/달성 뱃지) 완전 불변 | 회귀 0 |

- **상호 배타 보장**: `HasProgress` 정의에 `미달성` 이 AND 로 포함(§3.1)되므로, `HasProgress==true` 와 `Achieved==true` 는 동시 성립 불가. 달성 셀은 `HasProgress=false` → 진행 바 미표시 + 뱃지 표시. 레이아웃상 진행 바 영역과 뱃지 영역이 겹쳐도 토글이 배타라 충돌 없음(§5.1 빌더 명세).
- **fill 경계**: `Target>0` 가드는 누적형이면 Threshold≥2 라 항상 참(0 division 불가). `Current/Target` 은 미달성 셀에서 `0 ≤ Current < Target` → fill ∈ [0, 1) (달성 직전도 < 1, 달성은 진행 바 자체가 사라짐).

---

## 4. 데이터 흐름 (확정)

```
① profile + cfg
   → MetaBattleBonus.From(집계 배율, 전투와 단일 출처)
   → DungeonPowerSummary.Build(라벨 + 양수 %, §2.1 환산)
   → ShopPopup.Rebuild → BuildSummaryText(§2.2 조립) → _bonusSummaryText.SetText

② profile(TotalWins/TotalRuns) + cfg(Achievements)
   → QuestPopup.BuildCellData(§3.1 HasProgress/Current/Target 산출)
   → QuestCell.Bind(§3.3 진행 바 fill + N/M 텍스트, 달성 시 뱃지 대체)
```

- 의존 방향: `UI/Village → Meta`(plan). `DungeonPowerSummary` 는 `MetaBattleBonus`·`MetaConfig` 에만 의존, UI 비참조.

---

## 5. 프리팹 / 빌더 (영속화 명세)

> spec §3.3 / 메모리 "M4 가 프리팹 수작업 덮어씀": `Lair/Setup/V2` 재실행 시 UI 프리팹이 재생성된다. 프리팹 손-편집만 하면 다음 V2 에서 소실 — **빌더 코드에 노드 생성을 함께 넣어야 영속**. 본 절은 노드 구성·시각값을 명세(코드 구조는 gameplay-programmer).

### 5.1 ShopPopup 요약줄 노드

| 항목 | 명세 |
|---|---|
| 노드명 | `_bonusSummaryText` (CHText + TMP_Text 동행 — Rule 03 §3, 동적 라벨도 CHText 필수) |
| 위치 | 팝업 상단 — 소울 잔액(`_soulText`) **아래**, 스크롤 목록 위 |
| 폭 | 팝업 콘텐츠 폭(좌우 패딩 내 가용 전폭) |
| 줄/오버플로우 | word-wrapping on, overflow=Overflow(잘림 없음), 1~2줄 가변·상한 미강제(§2.3) — 콘텐츠 높이 가변 |
| `_stringID` | -1 (미사용 — 동적 SetText, §7 ②표 코드 리터럴) |

### 5.2 QuestCell 진행 바 노드

| 노드 | 명세 |
|---|---|
| `_progressRoot` | 빈 RectTransform 컨테이너 — 진행 바 표시/숨김 토글 단위(`SetActive(HasProgress)`) |
| `_progressFill` | Image, type=Filled / Method=Horizontal / Origin=Left — `fillAmount` 갱신 대상. 배경 Image(트랙)는 별도 자식 |
| `_progressText` | CHText + TMP_Text 동행 — `N/M` 표기. `_stringID` -1 |
| 배치 | 달성 뱃지(`_achievedBadge`)와 동일 영역 — 런타임 토글이 상호 배타(§3.3)라 레이아웃 충돌 무관 |

---

## 6. plan ↔ 기획서 sync delta

| 항목 | plan 잠정값 | 본 기획서 확정값 | delta 유형 |
|---|---|---|---|
| 요약줄 라벨 7종 | HP/공격/공속/이동/사거리/둔화/스폰률 (잠정) | **동일 — 전건 확정 채택** | 없음(라벨 desync 0) |
| 요약줄 오버플로우 | "한 줄 + Overflow Ellipsis" (Task 5 Step 1) | **word-wrap 1~2줄 가변, ellipsis 금지(잘림 없음)** (§2.3) | **delta — plan 보강 필요** |
| 진행 바 판정(`cumulative`) | `TotalWins \|\| TotalRuns` (Task 3 Step 4) | **+ `Threshold>=2` AND** (FirstRun carve-out, §3.1) | **delta — plan 보강 필요** |
| 진행 텍스트 / 반올림 | `N/M` · 정수 % / 0% 제외 | **동일 채택** (§3.2 / §2.4) | 없음 |

> plan 보강 2건(오버플로우 word-wrap, Threshold≥2 carve-out)은 구현 시 plan 에 delta 마일스톤으로 반영한다(project.md "plan↔기획서 sync 규칙").

---

## 7. 구현 요청사항 (gameplay-programmer 용)

> 코드 구조·ChvjPackage API 선택은 gameplay-programmer 판단. 아래는 데이터·표시 계약만. plan 의 파일 구조/시그니처를 존중하되 본 문서의 라벨·문구·UX 를 채운다.

### 7.1 Enum

- **추가 없음.** 본 기능은 신규 Enum 값 0건(spec §5 — 신규 저장 필드·id 추가 금지). 기존 `EMonsterStatKind`·`EShopEffectKind`·`EAchievementCondition` 만 읽는다.

### 7.2 Interface

- **추가 없음.** `DungeonPowerSummary` 는 정적 클래스(plan Task 1), UI 비참조.

### 7.3 신규/수정 타입 시그니처 (plan 시그니처 존중 — 라벨/문구만 확정)

| 타입 | 멤버 | 확정 내용 |
|---|---|---|
| `DungeonPowerLine`(struct, 신설) | `string Label` · `int Percent` | Label = §2.1 라벨 7종 / Percent = 양수 정수 % |
| `DungeonPowerSummary`(static, 신설) | `List<DungeonPowerLine> Build(MetaProfile, MetaConfig)` | §2.1 환산·순서·레벨0·0% 제외 |
| `ShopPopup`(수정) | `[SerializeField] CHText _bonusSummaryText` + `BuildSummaryText(MetaProfile, MetaConfig)` | §2.2 조립 문구. `Rebuild()` 에서 SetText |
| `QuestCellData`(수정) | `bool HasProgress` · `int Current` · `int Target` 추가 | §3.1/§3.2 산출. 기존 필드 뒤 append |
| `QuestPopup.BuildCellData`(수정) | 시그니처 불변 — 진행 필드 산출 추가 | §3.1 3조건 AND, §3.2 클램프 |
| `QuestCell`(수정) | `[SerializeField] GameObject _progressRoot` · `Image _progressFill` · `CHText _progressText` 추가 | §3.3/§5.2 |

### 7.4 에셋 키 (Enum 값명 = 파일명, Rule 03 §2)

- **신규 에셋 0건.** 기존 프리팹 2종 노드 추가만: `ShopPopup.prefab`(`_bonusSummaryText` 노드) · `QuestCell.prefab`(진행 바 노드 3종). Addressables 주소=파일명 유지(`ShopPopup`·`QuestCell`) — 엔트리 변동 없음.
- 두 프리팹 변경은 `LairVillageBuilder`(V2 메뉴) 빌더 코드에 함께 반영(§5, spec §3.3).

### 7.5 SO 스키마 / 수치 필드

- **추가 없음.** `MetaConfig`·`MetaProfile`·`ShopItemDef`·`AchievementDef` 스키마 불변(spec §5). 기존 필드(`ShopItems`·`Achievements`·`TotalWins`·`TotalRuns`·`AchievedIds`·`Threshold`·`Condition`·`PerLevelMul`·`MaxLevel`)만 읽는다.

### 7.6 표시 전용 보증 (회귀 가드)

- 본 기능은 `MetaBattleBonus` 의 *전투 적용* 경로를 건드리지 않는다 — 동일 집계 배율을 **읽기만** 한다(§1). 전투 진입 시 몬스터 배율 적용은 불변. plan Milestone 5 Step 4(표시 전용 보증)로 회귀 확인.

---

## 8. Self-Review (2026-06-12)

- **Placeholder 잔존**: 0건. 미정 마커·애매한 권유·두 갈래 위임·본문 비움 참조·검산 누락 5카테고리 전부 스캔. 7라벨 만렙 % 는 산식 + 검산 병기(§2.1), 폭 77자 검산(§2.3), 콤마 미사용 50/50 검산(§3.2), 0% 제외 발화 검산(§2.4) — 어림 표현 0건.
- **스펙 커버리지**: spec §2(범위 락: 상단 요약줄·스탯별 %·누적형만)→§2/§3.1 / §3.1(DungeonPowerSummary 골격)→§2.1/§4 / §3.2(QuestCellData 확장·BuildCellData·QuestCell)→§3/§7.3 / §3.3(빌더 영속)→§5 / §5(비범위: 저장필드0·id0)→§7.1/§7.5 / §7(부호 방향·빌더 SoT·표시 전용)→§2.1/§5/§7.6. 입력 task 의 확정 항목 5개(라벨/요약 포맷/반올림/진행 텍스트/진행 바 거동) 전부 표로 1:1 박음. 갭 0건.
- **내부 일관성**: 라벨 7종이 §2.1·§2.2 예시·§6 sync 표에서 동일. 만렙 % (HP+10·공격+8·공속+5·이동+8·사거리+8·둔화+11·스폰률+8)가 §2.1·§2.2 전 만렙 예시·§2.3 폭 검산에서 동일. 진행 바 대상 6개가 §3.1 표·§3.2 콤마 검산에서 동일. `HasProgress` AND 미달성 ⇒ 달성 셀 진행 바 미표시(상호 배타)가 §3.1·§3.3 일치.
- **시그니처/명명 일관성**: `DungeonPowerLine{Label,Percent}` · `DungeonPowerSummary.Build` · `_bonusSummaryText` · `BuildSummaryText` · `QuestCellData{HasProgress,Current,Target}` · `QuestPopup.BuildCellData` · `_progressRoot/_progressFill/_progressText` — plan 시그니처와 글자 단위 일치, 본문 내 변형 표기 0건.
- **모호 표현**: 0건. 오버플로우(2줄 wrap·ellipsis 금지)·FirstRun(Threshold≥2 carve-out)·둔화 대상(플랫 "둔화")·콤마(소울 한정) 등 두 갈래/애매 항목 전부 한 갈래 확정 + 근거.
- **스코프**: 단일 표시 전용 기능 — 분할 불요(plan 이 M1~M5 로 구현 분해 완료).
- **구현 요청사항 완전성**: Enum/Interface/에셋 키/SO 스키마 = 전부 "추가 없음" 명시(표시 전용) + 신규/수정 타입 시그니처 표(§7.3) + 빌더 노드 명세(§5) 완비.

---

## 9. 변경 이력

- **rev 1 (2026-06-12)** — 최초 작성. 던전 강화 요약줄(라벨 7종·문구 포맷·반올림·오버플로우 word-wrap) + 도전과제 진행도(FirstRun Threshold≥2 carve-out·N/M·진행 바 거동) 확정. plan delta 2건(오버플로우·carve-out) 명시.
