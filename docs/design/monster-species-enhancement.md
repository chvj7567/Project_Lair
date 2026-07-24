# 몬스터 종족별 강화 (상점 「몬스터 강화」 탭)

> 입력: spec `docs/superpowers/specs/2026-07-24-monster-species-enhancement-design.md` · plan `docs/superpowers/plans/2026-07-24-monster-species-enhancement.md`
> 이 기획서는 plan 의 `⟨기획서 확정⟩` 자리(배수 곡선·가격·발광 세기·셀 표현·요약줄·문구)를 닫는다. 파일 구조·시그니처·TDD 골격은 plan 이 SoT, 도메인 수치·디자인은 본 문서가 SoT.

---

## § 헤더

- **목표**: 상점에 6종(Wisp/Wraith/Reaper/Hex/Plague/Phantom) 개별 강화 「몬스터 강화」 탭을 추가해, 플레이어가 자기 취향의 종족을 Lv0~Lv5 로 키우고 그 종족이 전장에서 발광으로 눈에 띄게 만든다.
- **검증 가설**: 종족별 개별 강화 축이 (a) "내가 키운 종족이 실제로 보인다"는 피드백으로 재방문 동기를 강화하는가, (b) 소울 소모 곡선이 장기 그라인드 목표로 기능하는가. v0.3 가설(재방문)의 메타 콘텐츠 보강.
- **현재 단계 범위 적합성**: **범위 내**. CLAUDE.md §8 "메타 진행 허용" + spec 이 §8 기존 리소스 재조합 범위로 확인. 신규 몬스터/카드/영웅 리소스 제작 없음 — 기존 6종 프리팹 + 발광 채널 + 기존 `MonsterIcons/*.png` 스프라이트만 재사용.
- **핵심 메커니즘**: 한 탭 항목 = 한 종족. 강화 효과 = 그 종족 HP·공격력 **단일 배수**(레벨당 ×1.18 곱연산). 최종 스탯 = `기본 × 스탯강화(전종 글로벌) × 종족강화`, 3축 독립 곱연산. 시각은 종족 base color 유지 + 레벨별 **발광 세기만** escalate(Lv0 발광 off).

---

## 1. 배경 — 기존 파이프에 효과종류 1개만 얹는다

신규 병렬 시스템을 만들지 않는다(spec §4). 기존 소울 상점 파이프:

```
ShopItemDef(효과종류+레벨) → MetaProfile.ShopLevels(저장/클라우드동기)
  → ShopService(구매/가격/만렙) → MetaBattleBonus(전투 시작 배율 집계)
  → BattleController.ApplyMonsterStats(raw × 배율)
```

에 `EShopEffectKind.MonsterSpecies` 하나를 추가하고, 6종족 항목을 `MetaConfig.ShopItems` 에 등록한다. 저장은 기존 `ShopLevels` 그대로 → 클라우드 동기 자동 포함, 마이그레이션 불필요.

기존 상점 항목(참고 기준선):

| 항목 | EffectKind | PerLevelMul | MaxLevel | BasePrice | PriceGrowth |
|---|---|---|---|---|---|
| MonsterHpUp (강골 군세) | MonsterStat/Hp | 1.02 | 5 | 80 | 1.6 |
| MonsterPowerUp (흉포한 발톱) | MonsterStat/Power | 1.015 | 5 | 100 | 1.6 |
| SpawnerHasteUp (깨어나는 둥지) | SpawnerPeriod | 0.985 | 5 | 150 | 1.6 |

전종 글로벌 강화는 레벨당 **+1.5~2%** 의 미세 배율(만렙 HP ×1.10 / Power ×1.077)이다. 종족 강화는 이와 **다른 축** — 한 종족을 크게 키우는 굵은 배율이다.

---

## 2. 강화 곡선 (열린 결정 §7-①, ⟨기획서 확정⟩)

### 2.1 단일 배수 vs 분리 — **단일 배수 확정**

강화 1레벨 = 그 종족의 HP·공격력에 **같은 배수**를 함께 적용한다(`MetaBattleBonus.GetSpeciesMul(EMonster)` → `float` 하나).

- **근거**: (a) plan 의 `GetSpeciesMul` 단일 float 시그니처를 그대로 유지 → 구현·테스트 골격 무변경. (b) 플레이어 멘탈 모델이 단순 — "도깨비불 각성 Lv3 → 도깨비불 HP·공격력 ×N" 한 문장. (c) 종족 개성(탱커/스웜/원거리)은 이미 base 스탯·역할·스폰 주기에 담겨 있어, 강화는 "내 취향을 굵게" 축으로만 기능하면 충분.
- **분리안(HP/Power 각각 배수)은 데이터 게이트 fallback** 으로만 남긴다(§8). 단일 배수가 공격력을 HP만큼 키우는 것이 처치 속도를 과하게 당기면(§8 게이트 B 실패) 분리로 전환.

### 2.2 배수 곡선 — **전 종족 통일 PerLevelMul = 1.18** 확정

`PerLevelMul = 1.18`(레벨당 ×1.18 곱연산, 표시 "+18%/Lv"). 6종 모두 동일.

| Lv | 배수(= 1.18^Lv) | 표시 |
|---|---|---|
| 0 | 1.000 | 미강화 |
| 1 | 1.180 | +18% |
| 2 | 1.392 | +39% |
| 3 | 1.643 | +64% |
| 4 | 1.939 | +94% |
| 5 | 2.288 | +129% |

검산: `1.18^5 = 1.18×1.18×1.18×1.18×1.18 = 2.288` — 보스 스테이지 HP 곡선 종점(2.3)과 거의 일치. 즉 "완전 강화한 종족 = 대략 보스급 내구 스케일" 이라는 익숙한 기준선을 준다.

**왜 종족별 차등이 아니라 통일인가** (열린 결정 §7 근거):
- 곡선을 종족마다 다르게 주면(예: 탱커는 더 가파르게), 단일 배수 모델에서는 "소울당 스탯 효율이 가장 좋은 종족"이 수학적 정답이 되어 다른 종족 투자가 열등해진다 → "내 취향을 키운다"는 핵심 판타지가 깨진다.
- 통일 곡선이면 6종의 강화 효율이 동일하므로, 선택은 순수하게 **플레이 취향 + 전략(어느 종족이 내 스폰 구성에서 가동률이 높은가)** 이 된다. 예: Phantom(스폰 주기 6s, 필드에 다수) 강화는 전장 전체 화력을, Wraith(주기 20s, 소수) 강화는 벽 한 장을 굵게 키운다 — 정답 없는 트레이드오프가 자연 발생.

**대안 비교**:

| 안 | 장점 | 단점 | 판정 |
|---|---|---|---|
| **통일 1.18 (채택)** | 정답 종족 없음 → 취향 판타지 유지, 밸런싱 1개 곡선으로 단순 | 종족 개성을 곡선으로 강조 못 함 | ✅ |
| 종족별 차등 곡선 | 개성 강조 | 최고효율 종족 = 정답 → 다양성 붕괴 | ✗ |
| 통일 1.15 (완만) | 처치속도 과열 위험↓ | 만렙 매력 약화 | 데이터 게이트 fallback(§8) |

---

## 3. 가격 / 소울 경제 (열린 결정 §7, ⟨기획서 확정⟩)

### 3.1 가격 — **전 종족 BasePrice 150 / PriceGrowth 1.6** 확정

`ShopService.PriceOf` 재사용, 공식 `BasePrice × PriceGrowth^level`. 6종 통일.

- **PriceGrowth 1.6**: 기존 7개 상점 항목이 전부 1.6 — **관례 준수**. 강화 곡선을 통일했듯 가격도 통일해 "종족마다 더 싼/비싼 딜"이 없게 한다(효율 정답 제거와 일관).
- **BasePrice 150**: 기존 글로벌 항목 범위(80~150)의 **상단** = 가장 비싼 글로벌(SpawnerHasteUp 150)과 동급. 근거: 종족 강화 1레벨(+18%)은 글로벌 1레벨(+1.5~2%)보다 압도적으로 굵은 효과 → 최상단 가격이 정당.

레벨별 가격(= 150 × 1.6^level, `ShopService.PriceOf` 관례대로 정수화):

| 구매 | 산식 | 가격(소울) |
|---|---|---|
| Lv0→1 | 150 × 1.6^0 | 150 |
| Lv1→2 | 150 × 1.6^1 | 240 |
| Lv2→3 | 150 × 1.6^2 | 384 |
| Lv3→4 | 150 × 1.6^3 | 614 |
| Lv4→5 | 150 × 1.6^4 | 983 |
| **1종 만렙 누계** | 150+240+384+614+983 | **2,371** |
| **6종 전부 만렙** | 2,371 × 6 | **14,226** |

검산: `1.6^4 = 6.5536`, `150 × 6.5536 = 983.04 ≈ 983`. 누계 `150+240=390, +384=774, +614=1388, +983=2371`.

### 3.2 소울 소모 페이싱

승리 보상 규모: `WinBaseSouls 100 + WinTimeBonusPerSec 0.5 × 남은초` → 대략 100~200 소울/승(빠른 클리어일수록↑). 평균 ~135 가정.

- **초반 매력(진입 장벽 낮음)**: Lv1 = 150 소울 ≈ 1승 남짓. 첫 구매로 +18% + 발광 on 을 바로 체감 → "샘플링 훅"이 싸게 열린다.
- **후반 그라인드(장기 목표)**: 1종 만렙 2,371 ≈ 17~18승, 6종 완전강화 14,226 ≈ 100승 규모. 재방문 가설을 지탱하는 장기 소울 싱크. 기존 글로벌 7항목(만렙 총액 ≈ 11.8천 소울 — BasePrice 합 750 × Σ(1.6^0..4)≈15.81)과 합치면 메타 전체 그라인드가 촘촘해진다.
- 가격 곡선(1.6 지수)이 배수 곡선(1.18 지수)보다 가파르므로, **고레벨일수록 소울당 효율이 체감 감소** → 한 종족 몰빵보다 여러 종족을 고루 초반 레벨로 올리는 것이 초기엔 효율적 → 자연스러운 폭넓은 샘플링 유도.

---

## 4. 발광 세기 & 색 (열린 결정 §7-③, ⟨기획서 확정⟩)

### 4.1 발광 세기 5단계 — `_emissionByLevel` 확정

`MonsterEnhancementVisual._emissionByLevel`(index0 = Lv1), **6종 프리팹 모두 동일 값**:

| Lv | Emission Intensity | 비고 |
|---|---|---|
| 0 | (off) | 발광 키워드 disable — spec §3.5 락 |
| 1 | **1.5** | 보스가 처음 발광하는 검증된 가시 세기 |
| 2 | 1.9 | |
| 3 | 2.3 | |
| 4 | 2.7 | |
| 5 | 3.2 | 보스 최종(Stage5) 세기와 동급 = "만렙" 정점감 |

`_emissionByLevel = [1.5, 1.9, 2.3, 2.7, 3.2]`. 단계 델타 0.4·0.4·0.4·0.5 — 균등하게 오르다 만렙 직전 폭을 키워 정점을 강조.

**왜 Lv1 = 1.5 인가 (가시성 게이트)**: 보스 곡선은 Stage1~2 발광 0, Stage3에서 처음 1.5로 켜진다 — 즉 **1.5가 이 프로젝트 블룸 파이프에서 "확실히 보이는" 검증된 하한**이다. Lv1(가장 싼 샘플 구매)의 변화가 눈에 안 보이면 "내가 키운 게 보인다" 훅 자체가 무너지므로, Lv1을 이 검증 하한에 맞춘다.
- **가시성 검증 게이트(프리팹 배선 시) — 종족별 확인**: "1.5=검증된 하한"은 보스 Stage3 의 **밝은 청색**(max 0.97) 기준이다. `SpeciesGlowColor` 를 max=0.90 으로 정규화(§4.2)했으므로 6종 유효 휘도가 균일해졌지만, 실제 씬 블룸에서 **6종 각각 Lv1(1.5)이 육안 발광하는지 개별 확인**한다(특히 정규화된 Wraith·Phantom). 어느 종이라도 하한 미달이면 그 종만이 아니라 **`_emissionByLevel` 하한을 전종 공통 상향**(보스 최종 3.2 이하에서 재분배 — 곡선은 6종 동일 유지). 과하게 눈부시면(스웜 다수 동시) Lv1 을 1.2 까지 하향 여지 — 단 6종 모두 육안 발광이 필수.

### 4.2 발광 색 — **종족 고유색 + 단일 SoT `SpeciesGlowColor`** 확정 (공통색 아님)

강화 발광색은 **하나의 static 진실**에서만 나온다: **`Lair.Data.SpeciesGlowColor(EMonster) → Color`**(신규, `SpawnerStatusCell.SpeciesColor` 와 동일 패턴). **전투 발광(`MonsterEnhancementVisual`)과 셀 미리보기 프레임(`ShopItemCell`)이 둘 다 이 하나의 메서드를 읽는다** → "메뉴에서 본 색 = 전장에서 빛나는 색"이 구조적으로 보장된다(두 곳에 색을 따로 두지 않음).

- **왜 프리팹 `_enhanceGlowColor` serialized 필드가 아니라 static 인가**: 색을 프리팹 필드에 두면 셀(UI)이 그 값을 못 읽어(Character↔UI 역참조 회피) 셀은 다른 색을 쓰게 되고 → 두 색이 갈린다(design-reviewer 지적). static 하나면 양쪽이 같은 값을 읽는다. `EMonster` 는 `Lair.Data`(최하위 레이어)라 Character·UI 둘 다 자유 참조 → 역참조 문제 없음.
- **왜 기존 `SpeciesColor` 를 그대로 안 쓰나**: `SpeciesColor` 는 **평면 UI 식별색**(테두리·도감)이라 어두워도 됨 — Wraith #6B7280(max 0.50)·Phantom #1F2937(max 0.22)은 다크 배경(#262626) 위 프레임으로도, emission(색×세기)으로도 **빛이 안 난다**. 발광/프레임엔 최소 휘도가 필요하므로 별도 SoT 를 둔다(의미가 다른 두 색: 평면 식별 vs 발광).
- **정규화 규칙(비임의)**: `SpeciesGlowColor` = 각 종족 `SpeciesColor` **색조 유지, 최대 RGB 성분 = 0.90 으로 스케일**. 6종 모두 동일 휘도 상한 → 다크 배경 프레임·Lv1 세기 1.5 발광에서 균일하게 보인다. (색조 불변이므로 정체성 유지, Lv5 동일화 없음 — spec §3.5 준수.)

| 종족 | `SpeciesColor` (평면 식별, 기존) | `SpeciesGlowColor` (발광 SoT, max=0.90) |
|---|---|---|
| Wisp (도깨비불) | (0.133, 0.773, 0.369) #22C55E | (0.155, 0.900, 0.430) **#28E66E** |
| Wraith (망령) | (0.420, 0.447, 0.502) #6B7280 | (0.753, 0.801, 0.900) **#C0CCE6** (냉백 유령빛) |
| Reaper (사신) | (0.937, 0.267, 0.267) #EF4444 | (0.900, 0.256, 0.256) **#E64141** |
| Hex (저주술사) | (0.918, 0.702, 0.031) #EAB308 | (0.900, 0.688, 0.030) **#E6AF08** |
| Plague (역병귀) | (0.659, 0.333, 0.969) #A855F7 | (0.612, 0.309, 0.900) **#9C4FE6** |
| Phantom (환령) | (0.122, 0.161, 0.216) #1F2937 | (0.508, 0.671, 0.900) **#82ABE6** (청회 상향) |

검산(정규화): Phantom max 0.216 → ×(0.90/0.216=4.17) → (0.508, 0.671, 0.900). Wraith max 0.502 → ×1.79 → (0.753, 0.801, 0.900). 두 어두운 종이 이 규칙으로 밝아져 프레임/발광 모두 가시화(BLOCKER + 개선권장① 해소).

**공통 원칙**:
- **종족 고유색 강조(공통색 아님)**: 공통 강화색(금색 헤일로 등)을 쓰면 Lv5 끼리 같은 후광 → 약한 Lv5 동일화 재발. 종족 색조를 세기만 키워 강화된 Wisp·Wraith 가 서로 구별되게 유지.
- **틴트 불변**: base color(`_BaseColor`/`_BaseMap`) 불변. 발광 채널(`_EmissionColor` + `_EMISSION` 키워드)만 사용 → `HitFlash` baseline 충돌 없음(spec §4.3).
- **아웃라인 미사용(발광 단일 축)**: spec §7 열린 결정 — emission 단일 축 확정. Phantom·Reaper 등 다수 동시 등장 시 아웃라인은 겹쳐 노이즈. 발광은 원거리에서도 세기 escalate 가 읽힘. 컴포넌트 단순 유지.

### 4.3 적용 시점

전투 스폰(풀 Pop) 시 `ApplyMonsterStats(character, key, …)` 경로에서 그 종족 현재 강화 레벨로 **`ApplyLevel(int level, EMonster species)`** 1회 호출. 컴포넌트는 `SpeciesGlowColor(species)`(§4.2 SoT)로 발광색을 얻는다 — 프리팹에 색을 배선하지 않는다. Lv0 = off. 풀 재사용 리셋은 `OnEnable → ApplyLevel(0, …)` 로 발광 off(plan Task 3), 스폰 경로가 즉시 실제 레벨·종족으로 재지정. 소급(런 도중 이미 스폰된 몬스터)은 별도 처리 없음 — 강화는 전투 시작 시점 스냅샷이므로 런 중 상점 접근이 없는 현 루프에서 문제 없음.

> **plan sync (delta)**: plan Task 3 은 `_enhanceGlowColor` serialized 필드 + `ApplyLevel(int level)` 시그니처를 전제했다. 본 기획서는 색 단일 SoT 를 위해 (a) `ApplyLevel(int level, EMonster species)` 로 시그니처 확장, (b) 프리팹 `_enhanceGlowColor` 필드 제거, (c) 색은 `Lair.Data.SpeciesGlowColor(species)` 에서 취득으로 확정한다. plan Task 3/4 및 PlayMode 테스트(색 주입부)는 이 delta 로 갱신 필요 — gameplay-programmer/test-engineer 반영.

---

## 5. 셀 표현 (열린 결정 §7-②, ⟨기획서 확정⟩)

### 5.1 기존 `ShopItemCell` 확장 (전용 셀 신설 아님) 확정

- **근거**: plan Task 5 는 **단일 `_scrollView` + 탭 데이터 필터** 구조다(탭 전환 = `BuildCellData(profile, cfg, tab)` 결과 교체). `CHPoolingScrollView<TItem,TData>` 는 셀 타입 1개에 바인딩되므로, 두 탭이 같은 스크롤뷰·같은 셀 타입을 공유해야 한다 → 종족 전용 셀 신설은 구조 충돌. 따라서 `ShopItemCell` 을 확장한다.
- 종족 셀에서만 쓰는 위젯은 **선택적 표시**(글로벌 항목이면 비활성).

### 5.2 셀에 추가할 요소

1. **종족 아이콘** — 기존 `Assets/_Lair/Art/Sprites/MonsterIcons/{Wisp,Wraith,Reaper,Hex,Plague,Phantom}.png` 재사용(신규 에셋 없음). 셀 좌측 아이콘 슬롯에 표시. 글로벌 항목이면 아이콘 슬롯 숨김.
2. **발광 미리보기 프레임 — 현재 Lv + 다음 Lv 힌트** (spec §4.3): 아이콘 테두리에 **종족 발광색 글로우 프레임**을 얹는다. 색 = **`Lair.Data.SpeciesGlowColor(Species)`**(§4.2 단일 SoT — 전투 발광과 **같은 메서드**를 읽으므로 "메뉴 색 = 전장 색"이 구조적으로 일치, Phantom·Wraith 도 정규화되어 다크 배경에서 가시). 셀은 `cellData.Species` 로 이 static 을 직접 호출 — 색 필드 추가 없음.
   - **현재 Lv 프레임**: 밝기(알파/블룸) = `level / MaxLevel` **선형**(Lv0=0 → 꺼짐, Lv5=1 → 최대). "지금 이만큼 컸다"를 한눈에.
   - **다음 Lv 힌트**(만렙 아닐 때만): 현재 프레임 바깥에 **얇은 반투명 링**을 `(level+1) / MaxLevel` 밝기·낮은 알파(≈0.35×)로 함께 그린다. "한 번 더 사면 여기까지 밝아진다"는 다음 단계 예고. 만렙이면 다음 링 숨김.
   - **세기(intensity)는 선형 근사 — 의도된 재량**(§7 셀 표현은 기획서 재량): §4.3 의 "같은 발광 세기"는 전투 발광 곡선(`_emissionByLevel = [1.5…3.2]`)을 가리키나, 그 곡선은 **전투 프리팹(`Lair.Character.MonsterEnhancementVisual`)** 에 있어 상점 UI 에서 깔끔히 참조 불가(Character↔UI 역참조 회피). 따라서 셀은 세기를 `level/MaxLevel` 선형으로 **근사**한다 — 색은 정확히 일치, 세기는 "진행도" 근사. 곡선을 셀에 미러링하면 곡선이 두 곳에 중복되므로 의도적으로 근사를 택한다.
3. 기존 요소(이름·`Lv n/5`·설명·가격·구매 버튼)는 그대로.

### 5.3 데이터 전달 — 아이콘은 인스펙터 배선 resolver (CodexPopup 관례 준수)

**중요**: 이 레포는 종족 스프라이트를 Enum 키 Addressable 로드가 아니라 **인스펙터 `[SerializeField]` 스프라이트 참조 + `switch` resolver** 로 해결한다(기존 `CodexPopup.SpeciesIcon(EMonster) => _wispIcon/…` 관례). 종족 강화 셀도 이 관례를 따른다.

- `ShopItemCellData` 에 필드 추가: `EMonster? Species`(null=글로벌), `int Level`·`int MaxLevel`(프레임 밝기 계산용), `Sprite Icon`(종족 아이콘 — 글로벌이면 null).
- **`BuildCellData(profile, cfg, tab)` 는 순수 데이터만** 채운다 — `Species`/`Level`/`MaxLevel` 설정. **`Icon` 은 여기서 건드리지 않는다**(plan Task 5 테스트 시그니처 `BuildCellData(…, ShopTab)` 유지, `Sprite` 의존 없이 EditMode 테스트 가능).
- **아이콘 주입은 `ShopPopup.Rebuild`** 에서 — 기존에 `cell.OnBuy = HandleBuy` 를 채우는 바로 그 루프에서 `cellData.Icon = SpeciesIcon(cellData.Species)` 로 인스펙터 스프라이트를 주입한 뒤 `SetItemList`. `ShopPopup` 에 `[SerializeField] Sprite _wispIcon/_wraithIcon/…` 6개 + `SpeciesIcon(EMonster) => switch` (CodexPopup 복제).
- 셀 `Bind` 은 `Icon != null` 이면 아이콘·발광 프레임 표시, null 이면 숨김(글로벌 셀).

---

## 6. "현재 강화" 요약줄 포함 여부 (열린 결정 §7, plan Task 7, ⟨기획서 확정⟩)

**미포함 확정**(plan 기본안 유지). `ShopPopup._bonusSummaryText`("현재 강화 HP +10% · 공속 +5% …")는 **글로벌 항목만** 유지.

- **근거**: (a) 요약줄은 한 줄 컴팩트 개요다. 종족 6개를 붙이면 6세그먼트가 더해져 한 줄 overflow·가독성 붕괴. (b) 「몬스터 강화」 탭 자체가 6종의 현재 레벨·발광 현황을 이미 보여주므로 요약 중복. (c) `DungeonPowerSummary.Build` 무변경 → 스코프 최소화.
- 향후 도감/요약 화면을 종족 강화까지 확장할 필요가 생기면 별도 기획으로 승격(YAGNI).

---

## 7. 문구 (DisplayName / Description) (⟨기획서 확정⟩)

표시 순서 = `EMonster` enum 순서(Wisp → Wraith → Reaper → Hex → Plague → Phantom). `MetaConfig.ShopItems` 에 이 순서로 등록.

| Id | Species | DisplayName | Description |
|---|---|---|---|
| `Enhance_Wisp` | Wisp | 도깨비불 각성 | 도깨비불 HP·공격력 +18%/Lv |
| `Enhance_Wraith` | Wraith | 망령 각성 | 망령 HP·공격력 +18%/Lv |
| `Enhance_Reaper` | Reaper | 사신 각성 | 사신 HP·공격력 +18%/Lv |
| `Enhance_Hex` | Hex | 저주술사 각성 | 저주술사 HP·공격력 +18%/Lv |
| `Enhance_Plague` | Plague | 역병귀 각성 | 역병귀 HP·공격력 +18%/Lv |
| `Enhance_Phantom` | Phantom | 환령 각성 | 환령 HP·공격력 +18%/Lv |

- **명명 규칙**: "[종족 한글명] 각성" — 6항목 리스트에서 종족명 접두가 스캔성을 높인다("각성" = 발광하며 강해지는 컨셉과 맞음). 기존 글로벌 항목의 은유형 명명("강골 군세")과 달리, 종족 리스트는 **어느 종족인지 즉시 읽히는 것**이 우선.
- **설명 규칙**: 기존 글로벌 설명 포맷("모든 몬스터 HP +2%/Lv")과 동일하게 "+18%/Lv" 표기. 실제는 ×1.18 곱연산이나 기존 항목도 동일 관례(1.02 → "+2%/Lv")이므로 일관.

---

## 8. 밸런스 페이싱 & qa-simulator 게이트

### 8.1 3축 곱연산 파워 상한

만렙 시 한 종족 최종 배율(글로벌까지 합산):

- HP: `기본 × 글로벌1.10 × 종족2.29 = ×2.52`
- Power: `기본 × 글로벌1.077 × 종족2.29 = ×2.47`

### 8.2 처치 속도 과열 리스크 — 반드시 게이트

**단일 배수는 공격력(= 처치 속도)을 HP만큼 키운다.** 보스 곡선은 HP를 2.3까지 올리되 Power는 1.5에 그치는데, 종족 강화 단일 배수는 Power도 2.29까지 끌어올린다 — 즉 종족 강화의 **Power 증가분 +129%(×2.29)가 보스 최고단계 Power 증가분 +50%(×1.5)의 약 2.58배**(129/50=2.58)로 가파르다. 컨셉 §8 튜닝 창(평균 빌드로 영웅이 **2~4분**에 사망, **2분 미만 = 튜닝 실패**)을 만렙 군세가 붕괴시킬 수 있다.

이는 **의도된 메타 보상**(오래 그라인드한 플레이어가 빠른 클리어를 얻음)일 개연성이 크지만, 데이터로 확인해야 한다. 수치를 지금 확정(1.18 / 150·1.6 / 발광 [1.5…3.2])하되 아래 메트릭 게이트를 건다:

**qa-simulator 게이트 (구현 후 별도 호출 — 메트릭·결정선 명시)**:

| 게이트 | 조건 | 측정 | 통과선 | 실패 시 조정 |
|---|---|---|---|---|
| A | 단일 종족 Lv5 + 평균 카드 빌드 | 영웅 사망 시간 중앙값 | **≥ 2:00** (§8 하한 유지) | PerLevelMul 1.18 → 1.15 하향 |
| B | 전 6종 Lv5 + 평균 카드 빌드 | 영웅 사망 시간 중앙값 | **≥ 1:30** (최소 반격 여지) | 1.15 로도 hot 이면 **분리 배수 전환**: HP 1.18 / Power 1.10(보스식 완만 파워, Lv5 Power 1.61) |
| 기준 | 강화 0(Lv0 전종) + 평균 빌드 | 영웅 사망 시간 중앙값 | **2:00~4:00** (§8 창) | 강화와 무관 — 기존 밸런스 회귀 신호 |

- 분리 배수 전환은 plan `GetSpeciesMul` 을 `(float hp, float power)` 로 확장해야 하므로 plan↔기획서 sync 필요(그 시점 plan 에 delta 마일스톤 보강). 단일 배수가 게이트 통과하면 확장 불필요.

### 8.3 페이싱 요약

- **초반**: Lv1 150 소울(~1승) + 즉시 발광 → 싸고 눈에 보이는 첫 투자. 여러 종족을 얕게 찍는 게 소울 효율상 유리(가격 지수 > 배수 지수) → 폭넓은 샘플링.
- **중반**: 주력 1~2종을 Lv3~4로. 전장에서 그 종족만 또렷이 빛나 "내 빌드 색"이 생긴다.
- **후반**: 6종 완전강화 14,226 소울 = 장기 목표. 글로벌 강화와 합쳐 재방문마다 소울 쓸 곳이 남는다.

---

## 9. 구현 요청사항 (gameplay-programmer 용)

> 시그니처·파일 구조는 plan 이 SoT. 아래는 도메인 값·명명의 확정.

### Enum (Rule 02 §8 — `CommonEnum.cs`)
- `EShopEffectKind` 에 `MonsterSpecies` 추가(plan Task 1 Step 3). 기존 `EMonster`(Wisp~Phantom) 재사용 — 신규 Enum 없음.
- 탭 enum `ShopTab { Stat, Species }` — 단일 시스템(ShopPopup) 내부이므로 `ShopPopup.cs` 파일 내 정의(plan Task 5).

### Interface / 신규 static
- 신규 인터페이스 없음. `MonsterEnhancementVisual` 은 MonoBehaviour 컴포넌트(인스펙터 `[SerializeField]` 렌더러 와이어링, Rule 02 §5).
- **신규 `Lair.Data.SpeciesGlowColor(EMonster) → Color`** static (강화 발광색 단일 SoT — §4.2). `EMonster` 와 같은 `Lair.Data` 레이어에 두어 Character(전투)·UI(셀) 양쪽이 참조. 값 = §4.2 표의 `SpeciesGlowColor` 6종. (기존 `SpeciesColor` 는 평면 식별색으로 별개 유지 — 의미가 다름.)

### 에셋 키 / 아이콘 배선
- 종족 아이콘: 기존 `Assets/_Lair/Art/Sprites/MonsterIcons/{Wisp,Wraith,Reaper,Hex,Plague,Phantom}.png` 재사용. **로드 방식은 Enum 키 Addressable 이 아니라 인스펙터 `[SerializeField] Sprite` 참조 + `switch` resolver**(§5.3, `CodexPopup.SpeciesIcon` 관례). `ShopPopup` 에 6개 스프라이트 필드 + `SpeciesIcon(EMonster)` switch 추가.
- 종족 발광색: 코드 `SpeciesGlowColor` static 에서만 취득(§4.2) — 프리팹/데이터에 색을 배선하지 않음.
- 신규 프리팹/스프라이트 생성 없음.

### SO 스키마 / 수치 필드
- `ShopItemDef.Species` (`EMonster`) 필드 추가 — `EffectKind == MonsterSpecies` 일 때만 유효(plan Task 1 Step 4).
- `ShopItemCellData` 에 `EMonster? Species`, `int Level`, `int MaxLevel`, `Sprite Icon` 추가(§5.3). `Icon` 은 `BuildCellData` 가 아니라 `ShopPopup.Rebuild` 가 주입(테스트 시그니처 보존).
- `MetaConfig.asset` 종족 6항목 등록(§7 표 + 아래 공통값):
  - `EffectKind = MonsterSpecies`, `Species = <해당 종족>`, `PerLevelMul = 1.18`, `MaxLevel = 5`, `BasePrice = 150`, `PriceGrowth = 1.6`.
  - 등록 순서 = enum 순서(Wisp→Phantom).
- `MonsterEnhancementVisual`: `_emissionByLevel = [1.5, 1.9, 2.3, 2.7, 3.2]`(6종 프리팹 **모두 동일**). `_renderers` = 각 프리팹의 스프라이트/스킨드 렌더러 인스펙터 와이어링. **`_enhanceGlowColor` serialized 필드 없음** — 발광색은 `ApplyLevel(int level, EMonster species)` 가 `SpeciesGlowColor(species)` 로 취득(§4.3 plan delta).
- 셀 발광 프레임: 색 = `Lair.Data.SpeciesGlowColor(Species)`(전투와 동일 SoT), 밝기 = `Level / MaxLevel` 선형 + 다음 Lv 힌트 링 `(Level+1)/MaxLevel`(§5.2). `_emissionByLevel` 은 셀에서 참조하지 않음(세기 선형 근사).

### UI
- `ShopPopup` 프리팹: 탭 버튼 2개(CHButton — 「스탯 강화」/「몬스터 강화」) + `_statTabButton`/`_speciesTabButton` 인스펙터 연결 + 선택 탭 강조(색/언더라인). 기존 단일 `_scrollView` 공유, 탭 필터로 데이터 교체.
- `ShopItemCell`: 종족 아이콘 Image + 발광 프레임 요소 추가(글로벌 항목이면 비활성).
- `_bonusSummaryText` 요약줄은 글로벌만 — 변경 없음(§6).

---

## 10. Self-Review

- **Placeholder 잔존 0**: 미정 마커·애매한 권유·두 갈래 위임·본문 비움 참조·검산 누락 — 없음. 발광 Lv1 가시성과 처치속도는 "확정값 + qa-sim/블룸 검증 게이트 + 실패 시 결정선"으로 명시(§4.1, §8). 분리 배수는 "데이터 게이트 fallback"으로 단일안으로 좁힘.
- **스펙 커버리지**: spec §3.1 종족개별 → §2·5·7 / §3.2 HP·공격력 배수 → §2 / §3.3 3축 곱연산 → §2·8 / §3.4 5단계 → §2.2·§4.1(MaxLevel 5) / §3.5 발광·틴트X → §4 / §4.3 시각(전투 발광 + 메뉴 미리보기 — 색은 종족색 정확 일치, 세기는 선형 근사[§7 셀 재량, 곡선은 전투 프리팹 소관], 현재 Lv + 다음 Lv 힌트) → §4·§5.2 / §4.4 2탭 → §5·9 / §5 경제 → §3 / §7 열린 결정 3건(단일/분리·셀·발광축) → §2.1·§5·§4.2 전부 닫음. 갭 0.
- **레포 관례 정합(검증 완료)**: 종족 아이콘은 인스펙터 switch resolver(CodexPopup 관례)로 배선 — Enum 키 로드 아님(§5.3). `BuildCellData(…, ShopTab)` 테스트 시그니처 보존(Icon 은 Rebuild 주입).
- **design-reviewer 1차 반영(BLOCKER 1 + 개선 2)**: (BLOCKER) 발광색을 프리팹 필드/UI 재사용 두 곳에서 → **단일 SoT `Lair.Data.SpeciesGlowColor`** 로 통합, 전투·셀이 같은 메서드 읽음 → Phantom(#82ABE6)·Wraith(#C0CCE6) 정규화로 다크 배경 가시 + "메뉴 색=전장 색" 구조 보장(§4.2·§4.3·§5.2·§9). (개선①) `SpeciesGlowColor` max=0.90 정규화에 Wraith 포함 + §4.1 가시성 게이트 "6종 개별 확인"으로 명시. (개선②) §8.2 파워 가파름 2.58배로 정정, §3.2 글로벌 만렙 총액 ≈11.8천 정정.
- **기존 `PlagueVenomUp` 관계**: 스탯 강화 탭의 `PlagueVenomUp`(Plague SlowFactor 강화)과 신규 `Enhance_Plague`(Plague HP·공격력)는 **대상 스탯이 다른 별개 축**(둔화 vs 체력/화력)이며 탭도 `EffectKind` 로 분리되어 기능 충돌 없음. 두 항목이 "Plague 를 키운다"는 점만 겹치나 강화하는 값이 달라 중복 아님.
- **내부 일관성**: 배수 1.18(§2·9), 가격 150·1.6(§3·9), 발광 [1.5,1.9,2.3,2.7,3.2](§4·9), 셀 밝기 level/MaxLevel(§5·9) — 본문·표·구현요청 동일.
- **시그니처/명명 일관성**: `EShopEffectKind.MonsterSpecies`·`ShopItemDef.Species`·`GetSpeciesMul`·`MonsterEnhancementVisual.ApplyLevel(int, EMonster)`·`_emissionByLevel`·`Lair.Data.SpeciesGlowColor`·`ShopTab{Stat,Species}`·`Enhance_<EMonster>` — plan(+ §4.3 delta)과 글자 그대로 일치. `_enhanceGlowColor` 필드는 제거되어 문서 전체에서 미참조(설명 문맥 §4.2·§9 제외).
- **모호 표현 0**: "적당히/유연하게/또는(디자인 결정)" 없음.
- **스코프**: 단일 구현 단위(상점 탭 1개 + 효과종류 1개 + 시각 1컴포넌트). 분할 불필요.
- **구현 요청사항 완전성**: Enum/Interface(없음 명시)/에셋 키/SO 스키마/UI 모두 명세.
- **UI 목업**: `.mockups/monster-species-enhancement.html` 작성(다크 #262626 · 흰 아웃라인 · Jua · MonsterIcons 재사용 · 2탭 · 발광 프레임 · 구매 흐름).

판정: **통과**.
