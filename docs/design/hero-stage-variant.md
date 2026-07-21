# 스켈레톤 영웅 5스테이지 재스킨 시스템 — 기획서

- **작성**: game-designer
- **날짜**: 2026-07-21
- **단계**: v0.3
- **입력 문서**: spec `docs/superpowers/specs/2026-07-21-hero-stage-variant-design.md` · plan `docs/superpowers/plans/2026-07-21-hero-stage-variant.md` · 컨셉서 `docs/design/project_lair_concept.md`(§8 밸런싱 · §11.4 비주얼 매핑)

---

## § 헤더

- **목표**: 스켈레톤 영웅 모델 1종(`EHero.Knight`)을 셰이더/머터리얼·Transform 으로 재스킨해, 신규 리소스 제작 없이 5스테이지의 서로 다른 적 + 순차 해금 + 스탯 배수를 만든다.
- **검증 가설**: "같은 모델 재스킨(틴트/아웃라인/발광/스케일 누적) + 스탯 배수만으로 5단계 난이도 곡선이 '다른 적을 잡는다'는 체감과 재도전 동기를 만드는가."
- **현재 단계 범위 적합성**: **범위 내 (사용자 명시 승격)**. 스테이지 시스템은 v0.3 마을 허브 spec 의 원래 범위가 아니나, 사용자가 대화 중 스테이지 도입을 명시적으로 승격했다(spec §2.1). 신규 영웅/몬스터/카드 리소스 제작은 없으며(CLAUDE.md §8 준수) 스켈레톤 1모델 재사용에 한정된다.
- **핵심 메커니즘**: SO 정본(`HeroStageVariantConfig`, 5엔트리)이 스테이지별 외형값 + 스탯 배수를 보유 → `HeroStageVariantApplier` 가 스폰 시 Knight 프리팹에 적용. 마을에서 스테이지 선택(순차 해금, 5 종점), 클리어 시 다음 해금. HP/공격력은 배수(multiplier)로 스케일링하며 스테이지 1 = 배수 1.0 = 현재 밸런스 기준점.

---

## 1. 외형 스펙 (범위 A)

### 1.1 스테이지별 적용 기법 (spec §3 준수 — 변경 없음)

| 스테이지 | 기법 조합 | 의도한 인상 |
|---|---|---|
| 1 | A (틴트) | 기본 해골 적 |
| 2 | A+B (틴트+아웃라인) | 실루엣이 또렷한 강화 개체 |
| 3 | A+C (틴트+발광) | 발광하는 유령형 적 |
| 4 | A+B+C (전부) | 정예 |
| 5 | A+B+C+D (전부+스케일) | 보스급 거대 |

### 1.2 스테이지별 외형값 표 (game-designer 확정)

> 필드명은 plan Task 2 의 `HeroStageVariant` 구조를 글자 그대로 따른다: `TintColor` / `UseOutline` + `OutlineColor` / `UseEmission` + `EmissionColor` + `EmissionIntensity` / `ScaleMultiplier`.
> 색은 RGBA(0~1) 로 SO 에 입력한다. 괄호 hex 는 참고용.

| 스테이지 | TintColor (RGBA / hex) | UseOutline | OutlineColor (RGBA / hex) | UseEmission | EmissionColor (RGBA / hex) | EmissionIntensity | ScaleMultiplier |
|---|---|---|---|---|---|---|---|
| 1 | (0.910, 0.878, 0.784, 1) `#E8E0C8` 본(bone) | false | (0, 0, 0, 0) `#000000` (미사용) | false | (0, 0, 0, 1) `#000000` (미사용) | 0 (미사용) | 1.00 |
| 2 | (0.290, 0.871, 0.502, 1) `#4ADE80` 독성 그린 | true | (0.973, 0.980, 0.988, 1) `#F8FAFC` 라이트 림 | false | (0, 0, 0, 1) `#000000` (미사용) | 0 (미사용) | 1.00 |
| 3 | (0.220, 0.741, 0.973, 1) `#38BDF8` 유령 시안 | false | (0, 0, 0, 0) `#000000` (미사용) | true | (0.220, 0.741, 0.973, 1) `#38BDF8` | 1.5 | 1.00 |
| 4 | (0.659, 0.333, 0.969, 1) `#A855F7` 정예 퍼플 | true | (0.910, 0.475, 0.976, 1) `#E879F9` 마젠타 림 | true | (0.659, 0.333, 0.969, 1) `#A855F7` | 2.2 | 1.00 |
| 5 | (0.937, 0.267, 0.267, 1) `#EF4444` 보스 크림슨 | true | (0.992, 0.878, 0.278, 1) `#FDE047` 골드 림 | true | (1.000, 0.341, 0.133, 1) `#FF5722` 화염 오렌지 | 3.2 | 1.40 |

**No-Placeholder 준수**: UseOutline/UseEmission = false 인 칸도 빈칸을 두지 않고 `(0,0,0,0)` / intensity `0` + "(미사용)" 을 명시했다. 적용부(`HeroStageVariantApplier`)는 미사용 스테이지에서 아웃라인 서브머터리얼을 비활성, `EmissionColor` 를 검정(0,0,0)·intensity 0 으로 **명시적으로 덮어써** 기본 머터리얼의 잔존 발광이 새지 않게 한다(§1.5 참조).

### 1.3 값 근거 — 색 대비 / 실루엣 구분

색 램프는 **로그라이크 관례의 위협 등급 램프(본 → 그린 → 시안 → 퍼플 → 레드)** 이다. 이 중 컨셉서 §11.4 몬스터/카드축 팔레트와 겹치는 것은 **스테이지 4 퍼플(`#A855F7` = Debuff축/Plague)·5 레드(`#EF4444` = Dps축/Reaper) 둘뿐**이며(§1.3 하단·§6 색 겹침 주의 참조), 스테이지 1 본(`#E8E0C8`)·2 그린(`#4ADE80`)·3 시안(`#38BDF8`)은 §11.4 팔레트에 없는 재스킨 전용 색이다(2 의 `#4ADE80` 은 Tank축 `#22C55E` 와 hue 는 같으나 더 밝은 별개 값). 각 스테이지는 **직전 대비 (a) 색상(hue)이 크게 점프하고 (b) 기법 조합이 추가/교체**되어 이중으로 구분된다.

| 전환 | 색상 대비 | 기법 대비 | 판정 |
|---|---|---|---|
| 1→2 | 본(무채색) → 그린(≈140°) | +아웃라인(실루엣 강조) | 뚜렷 |
| 2→3 | 그린(140°) → 시안(≈200°) | 아웃라인 제거, +발광(다른 신호로 교체) | 뚜렷 |
| 3→4 | 시안(200°) → 퍼플(≈275°) | 아웃라인·발광 **둘 다** 부착(정예) | 뚜렷(누적) |
| 4→5 | 퍼플(275°) → 크림슨(≈0°) | 골드 림 + 화염 발광 + 1.4배 스케일 | 최대(실루엣까지) |

- **본 틴트(1)** 는 near-white 라 스켈레톤 뼈 텍스처를 거의 그대로 살려 "자연 그대로의 해골 = 기본" 인상을 준다. 뒤 스테이지의 유채색 틴트와 채도 차가 커 1 이 눈에 띄게 "밋밋"하게 구분된다.
- **2 의 라이트 림 아웃라인** 은 무채색이라 그린 바디를 흐리지 않으면서, 다크 아레나 배경(`#262626` 계열) 대비 실루엣만 또렷하게 띄운다.
- **3 은 발광(글로우)으로 교체** — 아웃라인이 "날카로움"이라면 발광은 "초자연/위험"으로 읽혀 2 보다 상위 위협으로 자연 인지된다. 시안은 그린보다 차갑고 유령스러워 등급 상승 방향과 일치.
- **4 는 정예답게 전부 부착** + 퍼플(전통적 "정예/에픽" 색). 마젠타 림이 퍼플 바디보다 밝아 림이 분리돼 읽힌다.
- **5 는 보스** — 크림슨 바디 + 골드 림(보스 관례색) + 최강 화염 발광 + **스케일 1.4** 로 실루엣 크기까지 커져 한눈에 "격이 다른 개체".

### 1.4 스케일 1.40 근거 (D 기법, 스테이지 5 전용)

- 스테이지 1~4 는 `ScaleMultiplier = 1.00`(spec §3 상 D 는 5 전용). 5 만 확대.
- **1.40 선택** (Task 요구 범위 1.3~1.5 중간값): 지면 투영 면적이 약 2배(1.4² ≈ 1.96)로 늘어 "거대"가 확실히 읽힌다. 1.3 은 체감이 약하고, 1.5 는 (a) 중앙 영웅이 수렴 몬스터 무리·머리 위 HP 바를 과하게 가리고 (b) 카메라 45° 탑다운에서 화면 점유가 과해질 위험이 있어 제외. 1.4 가 "보스 인상"과 "가독성/오클루전" 사이 균형점.
- 스케일은 root Transform `localScale *= 1.4` 로만 적용(셰이더 무관, spec §3-D).

### 1.5 틴트 렌더링 전제 (구현 검증 노트)

- 스켈레톤 머터리얼(`Skeleton_Mat`)의 `_BaseColor` 는 현재 흰색(1,1,1) + 본 텍스처(`_BaseMap`) 구조임을 확인했다. 따라서 본 표의 `TintColor` 는 텍스처에 **곱연산(multiply)** 으로 얹혀 의도대로 발색된다(near-neutral 알베도 전제 충족).
- **주의(발광 잔존)**: 기본 머터리얼에 잔존 `_EmissionColor (0, 2, 0.13)` 초록 발광이 남아 있다. 스테이지 1·2 는 `UseEmission = false` 이므로 적용부가 `EmissionColor` 를 검정(0,0,0)·intensity 0 으로 **명시적으로 덮어써** 이 잔존 발광이 새지 않게 해야 한다. (스테이지별 값이 항상 기본값을 override 하도록 — 미적용이 아니라 명시적 0 적용.)
- 색 채널 단일화(variant 틴트 ↔ HitFlash 동일 `_BaseColor` 채널, `MaterialPropertyBlock` 금지)는 spec §5.1 / plan Task 4·5 의 구현 사항으로, 본 기획서는 틴트 **값만** 정하고 채널 메커니즘은 건드리지 않는다.

### 1.6 전장 색 가독성 게이트 (범위 4·5 — 동일축 빌드 vs 동일색 영웅)

스테이지 4 퍼플·5 레드는 각각 Debuff축·Dps축 몬스터/카드 색과 일치한다(§1.3·§6). 문제는 **플레이어가 그 축을 키운 전장** 이다: 퍼플 Debuff 빌드(Plague 다수 + 퍼플 카드 테두리)가 **퍼플 4영웅** 을 둘러싸거나, 레드 Dps 빌드(Reaper 다수)가 **레드 5영웅** 을 둘러쌀 때, 영웅이 자기 몬스터 무리에 색으로 묻혀 **"어느 게 잡을 대상인지" 구분이 흐려질 위험** 이 있다.

- **본 기획서의 판단**: 영웅은 중앙·대형·해골 실루엣이고 4·5 는 아웃라인(마젠타/골드 림)·발광·(5는)1.4배 스케일이 얹혀 몬스터와 **실루엣·발광·크기로 분리** 되므로 색 겹침만으로 가독성이 깨지진 않는다고 본다.
- **단, 이는 정지 목업(§4.3)이 검증하지 못하는 영역이다** — 목업은 UI 셀만 커버하고 실제 전장(수렴하는 동일색 몬스터 무리 + 이펙트 위의 영웅)은 커버하지 않는다. 따라서 **파이프라인 8단계(qa-simulator/플레이테스트)에서 전장 가독성 게이트로 확인** 한다.
- **결정 메트릭 / 교정**: 스테이지 4(퍼플 Debuff 빌드)·5(레드 Dps 빌드) 실전 화면에서 영웅이 동일축 몬스터 무리와 **한눈에 구분되는가**. 구분이 흐리면 값을 갖고 씨름하지 말고 **아웃라인/발광의 대비를 우선 강화**(예: 4 골드 림 승격, 5 발광 intensity 상향), 그래도 부족하면 4·5 TintColor 를 축색 hex 에서 소폭 이격한다. 이 교정은 spec §3 기법 조합(4=A+B+C, 5=+D)을 바꾸지 않는 범위에서 한다.

---

## 2. 스탯 배수 (범위 C)

### 2.1 배수 표 (game-designer 확정)

> 필드명 plan Task 2 준수: `HpMultiplier` / `PowerMultiplier`. 기준 스탯은 `balance_config.json` 의 hero(HP 4000, Power 50) = 스테이지 1.

| 스테이지 | HpMultiplier | 실효 HP | PowerMultiplier | 실효 Power | 직전 대비 HP 증가폭 |
|---|---|---|---|---|---|
| 1 | 1.00 | 4000 | 1.00 | 50 | — (기준점) |
| 2 | 1.25 | 5000 | 1.10 | 55 | +0.25 |
| 3 | 1.55 | 6200 | 1.20 | 60 | +0.30 |
| 4 | 1.90 | 7600 | 1.35 | 68(67.5) | +0.35 |
| 5 | 2.30 | 9200 | 1.50 | 75 | +0.40 |

배수는 기존 `ApplyStats` 결과(baseline)에 **곱(baseline × mul)** 으로 1회 적용한다(복리 누적 아님, plan Task 6 §Step 3).

**정수 반올림 규칙(확정)**: HP·Power 는 정수 스탯이므로 `baseline × mul` 의 소수는 **round-half-up(반올림)** 으로 정수화한다. 이에 따라 **스테이지 4 Power = 50 × 1.35 = 67.5 → 68** 로 확정한다(내림 67 아님). 위 표의 "68(67.5)" 는 이 규칙의 결과이며 gameplay-programmer 재량 여지 없음. HpMultiplier 값들은 곱셈 결과가 정수라 반올림 무관.

### 2.2 근거 — 5단계 난이도 곡선 (컨셉서 §8)

- **컨셉서 §8**: 평균 빌드에서 영웅이 **2~4분** 사이에 죽도록 튜닝.
- **스테이지 1 = 배수 1.0 = 현재 밸런스 기준점(앵커)** — 단, 이 앵커가 컨셉 §8 의 2~4분 창을 실제로 만족하는지는 **미검증 가정**이다. 유일한 QA 리포트(`docs/qa-reports/2026-05-22.md`)가 BLOCKED 상태(밸런스 시뮬 하베스 미구축)라 HP 4000 baseline 의 **실측 클리어타임 데이터가 아직 없다**. 본 배수 곡선은 "baseline 이 창 안"이라는 가정 위에 세운 **상대 스케일**이므로, 앵커가 틀리면 곡선 전체가 함께 이동(shift)한다 → §2.3 게이트에서 앵커부터 검증한다.
- **HP 가 주(主) 난이도 레버**: 영웅 처치까지 걸리는 시간을 거의 선형으로 늘린다. 그래서 난이도 상승은 HP 배수로 설계한다.
- **Power 는 부(副) 레버 — 의도적으로 완만**: 영웅은 최근접 단일 타깃 공격이고, 몬스터 HP 는 대부분 30~100(예외: Wisp 200/Wraith 500)이라 Power 50 이면 이미 잡몹을 1~2타에 정리한다. 따라서 Power 를 50→75 로 올려도 보드 정리 속도 변화는 Wraith/Wisp 같은 탱커 상대에서만 유의미하다. Power 배수를 크게 주면 영웅이 몬스터를 즉시 쓸어 **플레이어가 보드를 구축할 틈이 사라져**(컨셉 §8 "HP 30초 안에 깎임 → 빌드업 X" 실패 조건의 대칭) 재미가 아니라 좌절이 된다. 그래서 Power 는 탱커 상대 압박만 소폭 더하는 완만한 램프로 제한한다.
- **곡선 형태 — 완만 시작 · 후반 가속**: HP 증가폭이 +0.25 → +0.30 → +0.35 → +0.40 으로 매 스테이지 조금씩 가팔라진다. 초반(1→2→3)은 메커니즘 학습·메타 성장 유도를 위해 완만하게, 후반(4·5)은 메타 투자를 강제하는 "벽"으로 의도했다. 5 = HP ×2.3(9200) 은 종점 보스로서 상당한 성장을 전제한다.

### 2.3 밸런스 확정 메트릭 (qa-simulator 검증 대상)

배수 값은 **shipping 기본값**으로 확정하되, 아래 미지수들에 대한 시뮬 데이터가 없으므로 파이프라인 8단계(qa-simulator)에서 재튜닝 후보로 표시한다. 재튜닝은 **값을 비워두는 것이 아니라** 위 표를 기본으로 놓고 아래 기준으로 조정한다. **게이트 순서가 중요하다 — 앵커(G0)를 먼저 검증한 뒤 상대 곡선(HP/Power)을 검증한다:**

- **G0 — 앵커(스테이지 1 baseline) 검증 (선행)**: 대표 빌드로 **스테이지 1(HP 4000, Power 50, 배수 1.0)** 의 평균 클리어 시간이 컨셉 §8 의 **2~4분 창** 안인가. 이 곡선 전체가 baseline 에 대한 상대 스케일이므로, **앵커가 창을 벗어나면 baseline HP(4000) 자체를 먼저 교정**한다. 이 경우 4·5 HpMultiplier 재튜닝만으로는 곡선 전체 shift 를 되돌릴 수 없다(모든 스테이지가 함께 이동). 앵커 교정 후에 아래 상대 곡선을 재검증한다.
- **결정 메트릭 (HP, G0 통과 후)**: 각 스테이지 N 에서 "대표 빌드 + 스테이지 N 도달 시점에 플레이어가 현실적으로 보유한 메타 파워" 조합의 **평균 클리어 시간이 컨셉 §8 의 2~4분 창 안**인가. 창을 벗어나면 **스테이지 4·5 의 HpMultiplier 부터** 우선 조정(배수 곡선 내 가장 재튜닝 가능성 높은 값). 단 앵커 자체 문제면 G0 로 회귀.
- **결정 메트릭 (Power)**: Power 배수는 클리어 시간이 아니라 **보드 몬스터 평균 개체수/빌드 성립 여부**로 본다. 스테이지 5 에서 영웅 Power 때문에 보드가 붕괴(플레이어가 축을 3장 이상 못 쌓음)하면 PowerMultiplier[5] 를 1.5 → 1.35 로 낮춘다.

---

## 3. 해금 · 페이싱 (범위 B)

### 3.1 순차 해금 규칙 (spec §6 준수)

- `MetaProfile.SelectedStage`(1~5, 기본 1) / `ClearedStage`(0~5, 기본 0) 로 진행 저장.
- **해금 조건**: `stage <= ClearedStage + 1` 이면 해금, 그 이상은 잠금. (예: `ClearedStage = 2` → 스테이지 3까지 해금, 4·5 잠금.)
- **클리어 갱신**: 승리 시 `ClearedStage = StageProgress.ResolveClearedStage(ClearedStage, SelectedStage)` = `Max(ClearedStage, SelectedStage)`, 5 초과 없음.
- **스테이지 5 종점**(spec §6.1): 5 클리어 시 더 해금할 스테이지 없음 = "전체 클리어". 5 재도전은 허용하되 새 해금은 발생하지 않는다.

### 3.2 체감 곡선 의도

- 곡선은 §2.2 와 동일한 의도를 UX 로 반영한다: **초반 완만(1→3) · 후반 급(4·5)**.
- 1→2→3 은 "다음이 열렸으니 바로 도전 가능"한 완만한 계단 — 신규 플레이어가 재스킨 메커니즘과 난이도 상승을 부담 없이 학습하고, 클리어로 메타(소울)를 모은다.
- 4·5 는 의도적으로 **한 번에 클리어 안 될 수 있는 벽** — 플레이어가 마을 상점 업그레이드·영주 레벨 등 메타 성장에 투자한 뒤 재도전하게 만들어, 5분 런과 런 사이 메타 성장 루프(컨셉 §7)를 맞물린다.
- 재도전은 항상 허용(잠금 해제된 어떤 스테이지도 다시 선택 가능) — 소울 파밍·빌드 실험 여지를 남긴다.

---

## 4. 스테이지 선택 UI 사양 (범위 B) — v2 마을 통합 캐러셀

> **UX 재설계(v2, 사용자 확정 2026-07-21)**: v1 의 별도 스크롤 팝업(`StageSelectPopup` 3-class + 프리팹)은 **완전 폐기**한다(§4.6). 스테이지 선택은 **마을 씬에 통합된 캐러셀**로 대체 — 중앙 쇼케이스 영웅이 현재 스테이지의 재스킨 외형을 실시간으로 보여주고, 좌/우 화살표로 1↔5 를 넘겨 보며, 출격은 현재 스테이지가 해금 상태일 때만 입장한다. 별도 팝업 없음. **신규 아트 리소스 없이** 텍스트·글리프·오버레이 알파로만 상태를 구분한다.

### 4.1 전체 흐름

1. 마을 진입 → `VillageController.SpawnIdleHero` 가 중앙에 Knight 배치 → 곧바로 `HeroStageVariantApplier.Apply(GetStage(SelectedStage))` 로 **현재 SelectedStage 외형**을 쇼케이스에 반영.
2. 플레이어가 **◀ / ▶** 로 스테이지를 이동 → 쇼케이스 영웅이 그 스테이지 외형으로 즉시 재스킨(잠긴 스테이지도 외형은 보여주되 잠금 오버레이를 얹는다) + 인디케이터(STAGE N, 위협도) 갱신.
3. 현재 스테이지가 **해금**이면 **출격 버튼 활성** → 누르면 그 스테이지로 Battle 입장. **잠금**이면 출격 버튼 비활성(§4.5) + 잠금 안내.

### 4.2 마을 HUD 배치 (VillageHud 확장)

쇼케이스 영웅은 화면 중앙(기존 `_heroAnchor`). 캐러셀 위젯을 `VillageHud` 에 추가하되, 기존 상단바(소울/이름/영주Lv)·좌우 메뉴 6종·하단 출격 버튼과 겹치지 않게 **영웅을 감싸는 중앙 레이어**에 둔다.

| 위젯 | 위치 | 내용 |
|---|---|---|
| 스테이지 인디케이터 | 쇼케이스 영웅 **머리 위**(상단바 아래, 화면 상단-중앙) | `STAGE {N}` (대문자 강조) + 아래줄 위협도 `★`×N + `☆`×(5-N) |
| ◀ 이전 버튼 | 영웅의 **좌측**(화면 좌-중앙, 좌측 메뉴 열보다 안쪽) | 글리프 `◀` (U+25C0) |
| ▶ 다음 버튼 | 영웅의 **우측**(화면 우-중앙, 우측 메뉴 열보다 안쪽) | 글리프 `▶` (U+25B6) |
| 잠금 오버레이 그룹 | 쇼케이스 영웅 **영역만** 덮음(전체 화면 아님 — 메뉴/출격은 보이게) | 반투명 어둠(α0.55) + 중앙 텍스트 `잠금` + 하단 잠금 안내 문구 |
| 출격 버튼 | 하단-중앙(기존 `_sortieButton` 유지) | 해금이면 활성, 잠금이면 비활성(§4.5) |

- **인디케이터 문구**: `STAGE {N}` — 스테이지 번호를 크게. 위협도 스타 `★★★☆☆`(별 개수 = 스테이지 번호)로 난이도 상승을 출격 전에 노출(페이싱 가시성). `★`(U+2605)/`☆`(U+2606) 는 문자 글리프(이모지 아님, 신규 아트 불필요).
- **화살표 글리프**: `◀`(U+25C0) / `▶`(U+25B6) — 기하 글리프로 대부분의 폰트에 포함. 별도 아트 불필요.

### 4.3 잠금 표시 (사용자 확정)

현재 캐러셀 위치가 잠긴 스테이지(`StageProgress.IsUnlocked(stage, ClearedStage) == false`)일 때만 표시:

- **어둠 오버레이**: 쇼케이스 영웅 영역 위에 검정 반투명 Image. **알파 = 0.55** (RGBA `(0, 0, 0, 0.55)`). 근거: 재스킨 외형(틴트/발광/실루엣)이 어두워진 채로도 **보이되**(사용자 요구 "외형은 보이되") 흰 잠금 표시·안내 문구가 또렷하게 떠 "잠김/비활성" 상태가 명확. 0.55 는 완전 은폐(1.0)와 밋밋한 그림자(≤0.3) 사이 균형점. (BLOCKER 재설계에서도 α0.55 유지.)
- **잠금 표시 — 최종 확정: 텍스트 라벨 `잠금` (옵션 3)**: 오버레이 중앙에 **CHText 로 `잠금`** 을 크게(굵게) 흰색 `#FFFFFF` 로 표시한다. 이모지 자물쇠 `🔒`(U+1F512)는 **채택하지 않는다** — 프로젝트 기본 TMP 폰트가 `NotoSansKR SDF`(VillageHud.prefab 등이 참조하는 폰트, 앞서 "Jua" 로 오기한 것을 정정)이고, 번들된 어떤 텍스트 폰트(NotoSansKR/Jua/Gaegu/Liberation/NotoSans)에도 컬러 이모지 U+1F512 글리프가 없어 **글리프 소스가 없다** → 렌더 불가. 옵션 비교:
  - (1) 신규 UI 스프라이트 자물쇠 아이콘 — 견고하나 **이미지 애셋 신규 제작 필요**(기존 lock 스프라이트 프로젝트에 없음을 grep 으로 확인). 아이콘 폴리시가 필요하면 후속 업그레이드 경로로 남긴다.
  - (2) 이모지 폰트(Noto Emoji 등) TMP fallback import — 🔒 하나 위해 폰트 애셋 추가 비용.
  - (3) **텍스트 `잠금`(채택)** — `NotoSansKR SDF` 에 확실히 존재하는 글리프, 신규 애셋 0, 렌더 리스크 0. 잠금 의미가 바로 아래 안내 문구(`스테이지 {N-1} 클리어 필요`)와 결합해 명확. **가장 견고하고 의존성 없음 → 단일 확정.**
- **잠금 안내 문구**: 오버레이 하단에 `스테이지 {N-1} 클리어 필요` (색 `#F5F5F5`). 모든 잠금 스테이지에서 `N-1` 은 항상 "직전 스테이지"로 정확(잠금 조건이 `stage > ClearedStage+1` 이므로 잠긴 최소 스테이지는 `ClearedStage+2`, 그 직전 `ClearedStage+1` 이 다음 해금 대상이나, 문구는 **각 스테이지 기준 직전** 인 `N-1` 을 안내해 "바로 앞을 깨면 열린다"를 전달).
- **글리프 렌더 검증(구현 게이트)**: 캐러셀이 쓰는 전 글리프 — `◀`(U+25C0)·`▶`(U+25B6)·`★`(U+2605)·`☆`(U+2606)·`잠금` — 는 모두 BMP 문자로 `NotoSansKR SDF`(Dynamic 모드, 소스 .ttf 온디맨드 래스터화) 에서 해결될 것으로 예상되나, gameplay-programmer 는 프리팹 배선 후 실제 폰트 애셋에 대해 이 글리프들이 정상 표시되는지 1회 확인한다(누락 시 해당 소스 폰트에 글리프 유무 재확인).

### 4.4 캐러셀 경계 동작 (결정 — 클램프, wrap 아님)

- **권장안: 끝에서 클램프 + 화살표 비활성.** 스테이지 1 에서 `◀` 비활성(Interactable=false, 흐리게), 스테이지 5 에서 `▶` 비활성.
- **근거**: 스테이지가 5개뿐이라 wrap(1↔5 순환)은 "어디가 처음/끝인지" 감각을 흐린다. 끝에서 화살표가 비활성되면 "여기가 경계"라는 피드백이 즉시 전달되고, 스테이지 1 = 항상 시작점이라는 멘탈 모델과도 일치. 잠긴 스테이지도 `▶` 로 계속 넘겨 볼 수 있으므로(외형 감상) 경계는 5(마지막)에서만 막힌다.

### 4.5 선택 저장 시점 · 출격 버튼 상태 (결정)

- **SelectedStage 저장 시점 (권장: 이동 즉시 로컬 저장)**: ◀/▶ 이동 시 `SelectedStage` 를 즉시 갱신하고 **로컬 세이브(`MetaSession.Store.Save`)만** 수행한다. 근거: 쇼케이스가 이미 그 스테이지를 반영하므로 상태 일관성을 위해 즉시 반영이 자연스럽고, 마을 재진입 시 마지막으로 보던 스테이지가 복원된다. 출격은 별도 확정 없이 현재 `SelectedStage` 를 그대로 읽는다.
  - **주의(네트워크)**: `SelectedStage` 는 로컬 선호값(plan Task 1)이다. 화살표 탭마다 **클라우드 백업(`BackupToCloud`)을 호출하지 않는다** — 브라우징 왕복이 네트워크를 도배하지 않게, 클라우드 동기는 다른 실제 프로필 변경(상점 구매 등)이나 출격/클리어 시점에 편승한다. 즉 이동 즉시 = 로컬 저장만, 클라우드 백업 제외.
- **출격 버튼 상태 (권장: 비활성 회색)**: 현재 스테이지가 잠금이면 출격 버튼 `Interactable = false`(회색). 근거: 잠금 사유가 이미 쇼케이스 오버레이+안내 문구로 **화면에 상시 노출**되어 있으므로, 눌렀을 때 흔들림/토스트로 재차 알리는 것은 중복이다. 비활성 회색이 "지금은 못 간다"를 가장 조용하고 명확하게 전달(HeroSelectCell 의 잠금 `Interactable=false` 관례와 동일).

### 4.6 v1 폐기 항목 (구현 단계에서 제거)

아래 v1 산출물은 **구현 단계에서 제거**한다(코드·프리팹·enum·배선·테스트 모두). 단 **해금 판정 도메인 로직은 삭제가 아니라 순수 헬퍼로 re-home** 한다 — 아래 참조.

- **제거 코드**: `StageSelectPopup.cs`(Panel+Arg), `StageSelectPoolingScrollView.cs`, `StageSelectCell.cs`. (plan Task 7 의 3-class — 폐기.)
- **제거 프리팹**: `StageSelectPopup.prefab`, `StageSelectCell.prefab` (+ .meta, Addressable 엔트리).
- **제거/전환 배선**: `VillageController.OpenStageSelect` / `HandleStageSelected` / `StageSelectPopupArg` 및 `Sortie()` 가 팝업을 여는 흐름 → **캐러셀 이동 + 출격 직접 입장**으로 교체. `EUI.StageSelectPopup` enum 값은 더 이상 참조되지 않으므로 제거.
- **제거 테스트 + 로직 이전(중요)**: `Tests/EditMode/StageSelectCellDataTests.cs`, `StageSelectCellDataEdgeTests.cs` 는 `StageSelectPopup.BuildCellData`/`StageSelectCellData` 를 참조하므로 그대로 두면 `Lair.Tests.EditMode` asmdef 컴파일이 깨진다 → **두 파일 제거**. 그러나 이들이 검증하던 **해금 판정 경계 로직**(`stage <= ClearedStage + 1`)은 도메인 진실이므로 **`StageProgress`(기존 static 순수 클래스, `Scripts/Battle/StageProgress.cs`)로 이전**한다:
  - **추가할 순수 함수**: `StageProgress.IsUnlocked(int stage, int clearedStage) => stage <= clearedStage + 1`. (기존 `ResolveClearedStage`/`ScaleStat` 옆.) 캐러셀 VM/컨트롤러가 잠금 판정에 이 함수를 호출한다 — 판정 로직이 UI 셀 코드가 아니라 순수 헬퍼에 단일 소유.
  - **테스트 이전처**: 폐기되는 두 테스트의 해금 경계 케이스(예: `ClearedStage=2` → 3 해금·4·5 잠금, `ClearedStage=0` → 1 해금·2 잠금, `ClearedStage=5` → 전부 해금)는 **`StageProgressEdgeTests.cs`(기존 파일)로 옮겨** `IsUnlocked` 를 검증하도록 재작성한다. (테스트 실제 재작성은 test-engineer 담당 — 본 기획서는 "어디로 옮기고 무엇을 검증하는가"를 확정한다.)
- **유지**: `MetaProfile.SelectedStage/ClearedStage`, `HeroStageVariantConfig`, `HeroStageVariantApplier`, `StageProgress.{ResolveClearedStage, ScaleStat, IsUnlocked}`, HitFlash 연동 — **외형/스탯/진행 로직은 그대로**. 바뀌는 것은 "선택 UI 를 어떻게 노출하는가" 뿐이다.

### 4.7 이번 사이클 제외 (YAGNI)

- **"클리어 완료(✓)" 배지 제외**: 캐러셀 인디케이터에 "이미 클리어" 표식은 넣지 않는다(핵심 상태 = 현재 스테이지의 해금/잠금 + 위협도는 모두 노출됨). 필요 시 후속 delta 로 인디케이터에 클리어 마크 추가.

---

## 5. 구현 요청사항 (gameplay-programmer 용)

> 파일 구조·시그니처·메커니즘은 plan 이 확정한 것을 따른다. 아래는 **도메인 값/문구/위젯** 만 명세한다.

- **Enum 값**:
  - **`EUI.StageSelectPopup` 는 추가하지 않는다(폐기, §4.6).** v2 캐러셀은 별도 팝업이 아니라 `VillageHud`/`VillageController` 확장이므로 신규 `EUI` 값이 불필요. (v1 에서 이미 추가된 `EUI.StageSelectPopup` 이 있으면 제거.)
  - **`EStage` enum 은 만들지 않는다** — 스테이지 식별은 `int`(1~5)(spec §4).
- **Interface**: 신규 인터페이스 없음. `HeroStageVariantApplier` 의 `HitFlash` 참조는 `[SerializeField]`/`Awake` 1회 캐싱(Rule 02 §5, plan Task 5).
- **순수 헬퍼 추가**: `StageProgress.IsUnlocked(int stage, int clearedStage) => stage <= clearedStage + 1` (`Scripts/Battle/StageProgress.cs`, 기존 `ResolveClearedStage`/`ScaleStat` 옆). 잠금 판정은 이 함수를 단일 소유(§4.6).
- **UI 위젯 (VillageHud 확장 — 신규 프리팹 없음, 기존 VillageHud 프리팹에 추가)**:
  - `[SerializeField] private CHButton _stagePrevButton;` (◀), `_stageNextButton;` (▶)
  - `[SerializeField] private CHText _stageIndicatorText;` (`STAGE {N}`), `_stageThreatText;` (위협도 스타)
  - `[SerializeField] private GameObject _stageLockOverlay;` (어둠 오버레이 그룹 루트 — 켜고/끔)
  - `[SerializeField] private Image _stageLockDim;` (검정 반투명, 알파 0.55), `_stageLockLabel;`(CHText, 텍스트 `잠금` — §4.3 최종 확정), `_stageLockHintText;` (`스테이지 {N-1} 클리어 필요`)
  - 출격 버튼은 기존 `_sortieButton` 재사용 — 잠금 시 `Interactable=false`.
  - **MVVM 저장 책임(개선 반영)**: `VillageViewModel` 은 의존성 없는 POCO 이므로 **persistence 를 넣지 않는다**. 화살표 클릭 → VM 은 `SelectedStage` 를 in-memory 로 이동(경계 클램프) + `OnChanged` 통지만. **로컬 저장은 `VillageController` 가 소유** — 기존 persistence 소유자 관례(`VillageController.HandleProfileChanged`)와 동형. 단 스테이지 브라우징은 **로컬 저장만**(`MetaSession.Store.Save`) 하고 `BackupToCloud` 는 호출하지 않는다(§4.5 네트워크 주의). View 가 인디케이터/오버레이/출격 상태 갱신 + `VillageController` 가 쇼케이스에 `HeroStageVariantApplier.Apply` 재적용. View 에 비즈니스 로직 금지(Rule 02 §6).
  - **방어적 가드**: `SelectedStage` 가 잠긴 값으로 저장돼 있을 수 있다(예: 세이브 마이그레이션·데이터 편집). 실사용은 출격 게이팅(잠금 시 비활성)으로 안전하나, 마을 진입/출격 직전 `StageProgress.IsUnlocked` 로 확인하고, 잠긴 값이면 표시상 잠금 오버레이가 뜨며 출격 불가로 자연 차단된다. BattleController 도 `GetStage` 가 1~5 클램프하므로 범위 이탈은 방어된다.
- **에셋 키 / 프리팹**:
  - **폐기: `StageSelectPopup.prefab`, `StageSelectCell.prefab` (구현 단계에서 제거, §4.6).**
  - `HeroStageVariantConfig.asset` (`Assets/_Lair/Data/` — BalanceConfig.asset 옆, 비-Addressable). **인스펙터 serialized 참조 2곳**: `BattleController`(전투 스폰 재스킨+스탯 배수) **및 `VillageController`(마을 쇼케이스 재스킨 — §4.1 `GetStage(SelectedStage)` 적용에 필요)**. plan Task 8 §Step1·Step4 에 두 참조 배선 추가.
  - `HeroOutline.shader` + `Mat_HeroOutline.mat` (`Assets/_Lair/Art/Materials/`, plan Task 3) — 아웃라인 서브머터리얼. `_OutlineColor`/`_OutlineWidth` 값은 §1.2 표의 OutlineColor 를 스테이지별로 applier 가 주입.
  - **잠금 표시는 신규 애셋 0** — 텍스트 `잠금`(§4.3 옵션 3 확정). 이모지 폰트/스프라이트 추가 없음.
- **SO 스키마** (`HeroStageVariantConfig` / `HeroStageVariant`, plan Task 2 — 필드명 그대로):
  - `HeroStageVariant`: `Color TintColor` · `bool UseOutline` · `Color OutlineColor` · `bool UseEmission` · `Color EmissionColor` · `float EmissionIntensity` · `float ScaleMultiplier` · `float HpMultiplier` · `float PowerMultiplier`
  - 5엔트리 값 = §1.2 표 + §2.1 표.
  - `GetStage(int stage1Based)` 는 1~5 클램프(1↔5).
- **적용 순서 주의**(spec §5.1, plan Task 4·5): variant 틴트 적용 후 `HitFlash.SetBaselineColor(TintColor)` 로 원본 캐시를 variant 색으로 (재)설정 → 피격/공격 flash·풀 재사용 후에도 틴트 유지. 비발광 스테이지는 `EmissionColor` 검정·intensity 0 명시 적용(§1.5). **쇼케이스에도 동일 applier 적용** — 마을 idle 영웅은 전투 컴포넌트 off 지만 외형 재스킨은 적용.
- **UI 문구**(§4.2·§4.3): 인디케이터 `STAGE {N}`, 위협도 `★`×N + `☆`×(5-N), 잠금 표시 텍스트 `잠금`, 잠금 안내 `스테이지 {N-1} 클리어 필요`. 화살표 `◀`/`▶`. 색상은 흰 텍스트 + 오버레이 검정 알파 0.55.
- **글리프 렌더 검증**: 사용 글리프 `◀`(U+25C0)·`▶`(U+25B6)·`★`(U+2605)·`☆`(U+2606)·`잠금` 은 모두 `NotoSansKR SDF`(Dynamic) 소스 .ttf 에서 온디맨드 해결 예상 → 프리팹 배선 후 실표시 1회 확인(§4.3).

---

## 6. Self-Review

- **Placeholder 잔존 0건**: 미정 마커 없음(배수는 shipping 기본값 확정 + §2.3 결정 메트릭 병기). UseOutline/UseEmission false 칸도 `(0,0,0,0)`/`0`+"(미사용)" 명시. 애매 권유/두 갈래 위임 없음. "약/대략" 어림 없음(스케일 1.4² ≈ 1.96 검산 명시).
- **스펙 커버리지**: spec §2 A→§1, B→§3·§4(v2 캐러셀), C→§2 / §3 외형표→§1.2 / §4 SO·int식별→§5 / §5.1 색충돌→§1.5+§5(값만, 메커니즘 불건드림) / §6 해금→§3 / §6.1 5종점→§3.1 / §9 비범위(디졸브·신규리소스·고유AI) 준수. 갭 0. spec §6 의 "선택 = 팝업" 서술은 v2 에서 마을 캐러셀로 대체(사용자 확정), 진행/해금 로직은 spec 그대로.
- **내부 일관성**: §1.2/§2.1 수치가 §5 구현 요청·표와 동일. 배수 실효값 검산 일치(4000×2.3=9200 등). §4 폐기 항목(§4.6)과 §5 구현 요청의 "폐기/제거" 표기 일치.
- **시그니처/명명 일관성**: `TintColor`/`UseOutline`/`OutlineColor`/`UseEmission`/`EmissionColor`/`EmissionIntensity`/`ScaleMultiplier`/`HpMultiplier`/`PowerMultiplier`, `HeroStageVariant(Config)`, `HeroStageVariantApplier`, `StageProgress.ResolveClearedStage` — plan 표기와 글자 단위 일치. **v2 폐기 식별자**(`StageSelectPopup`/`StageSelectCell`/`StageSelectPoolingScrollView`/`StageSelectCellData`/`BuildCellData`/`EUI.StageSelectPopup`)는 §4.6·§5 에서 "제거 대상"으로만 등장(신규 구현 계약 아님).
- **v2 재설계 · plan 재sync 필요**: 이 UI 재설계는 plan Task 7(StageSelectPopup 3-class)·Task 8(팝업 프리팹)을 폐기·교체한다. plan↔기획서 sync 규칙에 따라 **plan 도 delta 마일스톤(팝업 제거 + VillageHud 캐러셀 배선 + `StageProgress.IsUnlocked` 추가 + 테스트 이전)으로 보강 필요** — 메인/오케스트레이터가 처리하도록 명시.
- **BLOCKER 1 해소(폐기 vs 테스트/로직)**: 폐기되는 `StageSelectCellDataTests.cs`/`StageSelectCellDataEdgeTests.cs` 를 §4.6 제거 목록에 포함하되, 이들이 검증하던 해금 경계 로직(`stage <= ClearedStage+1`)을 **`StageProgress.IsUnlocked` 순수 함수로 이전 + `StageProgressEdgeTests.cs` 로 케이스 이전**을 명시 → asmdef 컴파일 파손·도메인 로직 유실 없음.
- **BLOCKER 2 해소(잠금 표시)**: 실제 기본 폰트가 `NotoSansKR SDF`(guid 12e8e80… 확인, "Jua" 오기 정정)이고 번들 폰트에 🔒(U+1F512) 글리프 없음 → **텍스트 `잠금`(옵션 3)으로 단일 확정**(신규 애셋 0, 렌더 리스크 0). 옵션 1(UI 스프라이트)·2(이모지 폰트) 는 근거와 함께 기각. α0.55 유지.
- **§11.4 색 재사용 주의**: 스테이지 **4 퍼플(`#A855F7` = Debuff축/Plague)·5 레드(`#EF4444` = Dps축/Reaper) 둘만** 컨셉 카드축 색과 겹친다(1 본·2 그린·3 시안은 §11.4 팔레트에 없는 재스킨 전용 색 — §1.3). 톤 일관성 상 의도적 재사용이며, 영웅은 중앙·대형·해골 실루엣 + 아웃라인/발광/스케일로 축 몬스터와 분리된다고 판단. **단 정지 목업이 못 보는 전장 가독성(동일축 빌드 vs 동일색 영웅)은 §1.6 게이트로 파이프라인 8단계(qa-simulator/플레이테스트)에서 확인**한다.
- **밸런스 앵커 미검증 명시**: 스테이지 1 baseline(HP 4000)의 실측 클리어타임 데이터가 없음(QA 리포트 BLOCKED)을 §2.2 에 미검증 가정으로 표기하고, §2.3 에 앵커 선행 검증 게이트(G0)를 추가했다. 배수는 shipping 기본값이되 앵커→상대곡선 순으로 검증.
- **모호 표현 0**, **스코프**: 단일 구현 단위 적정(재스킨+선택UI+배수 = 한 기능, plan 8태스크로 이미 분해됨).
- **UI 목업**: `.mockups/hero-stage-variant.html` **v2 로 갱신**(마을 캐러셀 — 중앙 쇼케이스 영웅 재스킨 실시간 반영, ◀/▶ 이동, STAGE N + 위협도, 잠금 어둠 오버레이(알파 0.55)+텍스트 `잠금`+안내, 출격 버튼 해금/잠금 상태). 다크 `#262626` 앱 톤 일치.

**Self-Review: 통과** (Placeholder 0 / 스펙 갭 0 / 명명 일치 / §11.4 색겹침 근거 / v2 캐러셀 5개 도메인 결정 확정 · 두 갈래 위임 0 — 잠금 표시·경계·저장·출격 모두 단일안 / BLOCKER 1·2 해소).
