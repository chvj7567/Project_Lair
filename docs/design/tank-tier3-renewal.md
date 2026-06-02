# Tank 시너지 Tier3 리뉴얼 — 캡 +6 → Wisp·Wraith 추가 내구 버프

> 작성일: 2026-06-03 · 단계: MVP · 작성: game-designer
> 관련 컨셉서: `docs/design/project_lair_concept.md` §4(코어 루프) · §8(밸런싱) · §5.2(시너지)
> 관련 spec: `docs/superpowers/specs/2026-06-03-monster-cap-removal-active-trigger-trim-design.md`
> 관련 plan: `docs/superpowers/plans/2026-06-03-monster-cap-removal-active-trigger-trim.md`
> 단일 진실 정합: `docs/design/card-renewal.md` §4 (12개 빌드 시너지 Tier 표)

---

## § 헤더

- **목표**: 동시 몬스터 캡 제거로 무의미해진 Tank 시너지 Tier3(7장 임계, 구: 글로벌 캡 +6) 효과를 Wisp·Wraith **추가 내구(HP) 버프**로 교체한다.
- **검증 가설**: "7장 한 축 몰빵(진성 빌드)에 대한 Tier3 보상이, 캡 제거로 이미 물량이 늘어난 상황에서도 과하지 않게 — 그러나 Tier1+Tier2 합산 위에서 체감되게 작동하는가."
- **현재 단계 범위 적합성**: **범위 내**. 컨셉 §11.2 의 MVP 포함 항목(시너지 검증·카드 28장) 안. 신규 스탯/시스템 추가 없이 기존 Tier1/2 와 동일한 `RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float)` 구조를 재사용한다.
- **핵심 메커니즘**: Tier3 발화(7장 임계) 시 Wisp·Wraith 종에 `EMonsterStatKind.Hp` **×1.4** 글로벌 영구 버프를 등록. 이 ×1.4 는 **Tier 단독 기여분**이며, Tier1 HP ×1.3·개별 패시브 카드(WispHpBoost·WraithDamageBoost ×1.5)와 **동일한 `RegisterMonsterTypeBuff` HP dict 표면 위에서 곱연산 누적**된다. 7장 진성 빌드의 실측 누적은 §2.2 검산 참조.

---

## 1. 배경 — 왜 교체하는가

spec §2.A 에서 동시 몬스터 캡(`BattleController._monsterCap = 18`)을 개념째 제거하기로 확정됐다. 구 Tank Tier3 는 이 캡을 +6(18→24) 올리는 효과였으므로, 캡이 사라지면 **효과 대상 자체가 없어진다**. spec §2.A 표가 정한 교체 방향:

> Tank Tier3 카드: 캡 +6 효과 → **Wisp+Wraith 추가 내구 버프로 교체** (테마 일관, Tier1/2 와 동일한 `RegisterMonsterTypeBuff` 구조). 구체 스탯·수치는 game-designer 가 §8 밸런스 맥락에서 설계.

본 기획서는 그 "구체 스탯·수치"를 확정한다.

기존 Tank 축 Tier 구조 (card-renewal.md §4.2):

| Tier | 임계 | 구 효과 | 적용 표면 |
|---|---|---|---|
| Tier1 | 3장 | Wisp·Wraith HP ×1.3 (글로벌 영구) | `RegisterMonsterTypeBuff(_, Hp, 1.3)` |
| Tier2 | 5장 | Wisp·Wraith Power ×1.2 (글로벌 영구) | `RegisterMonsterTypeBuff(_, Power, 1.2)` |
| Tier3 (구) | 7장 | 글로벌 캡 +6 (18→24, 영구) | `IncrementGlobalMonsterCap(6)` ← **제거 대상** |

Tank 축 정체성은 card-renewal.md §2 가 정의: "영웅을 **묶어 둔다** (HP·맷집·진로 방해)". HP 추가 버프는 이 정체성과 정확히 일치한다 — 캡 확장보다 오히려 Tank 축의 색깔에 더 부합한다.

---

## 2. 신효과 정의 (확정)

### 2.1 결정값

| 항목 | 결정값 |
|---|---|
| 적용 스탯 | `EMonsterStatKind.Hp` (CommonEnum.cs L94 에 실재 확인) |
| 배율 | **×1.4** |
| 적용 종 | `EMonster.Wisp`, `EMonster.Wraith` (각 1회씩, Tier1/2 와 동일 종 집합) |
| 적용 표면 | `IBattleContext.RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float)` |
| 지속 | 글로벌 영구 (이후 스폰 전부 + 현재 필드 소급 — Tier1/2 와 동일 시맨틱) |
| 누적 | 곱연산 (Tier1 HP ×1.3 과 동일 스탯 위에서 누적) |

### 2.2 누적 결과 검산

**핵심 — 동일 표면 곱연산**: Wisp·Wraith 의 HP 는 *Tier 시너지*와 *개별 패시브 카드*가 **모두 같은 `RegisterMonsterTypeBuff(type, Hp, mul)` dict 표면 위에서 곱연산 누적**된다 (card-renewal.md §7.3 L340 "본 기획의 모든 종 글로벌 스탯 카드 + 시너지 Tier 효과가 동일 표면 사용"; 코드 증거 `TankSynergyTier1.cs` L13-14 가 이 표면을 호출). 따라서 ×1.4 를 "Tier 단독 배율"로만 검산하면 7장 빌드의 실측 누적을 과소평가한다.

같은 HP 표면에 곱연산되는 항목 (Tank 7장 빌드 = P4 + A3 에서 픽 가능한 것):

| 출처 | 대상 종 | 배율 | 분류 | 누적 정책 |
|---|---|---|---|---|
| Tank Tier1 (3장) | Wisp·Wraith | ×1.3 | 시너지 | 1회 (Tier 발화) |
| **Tank Tier3 (7장)** | Wisp·Wraith | **×1.4** | 시너지 | 1회 (Tier 발화) |
| `WispHpBoost` (P) | Wisp | ×1.5 / 픽 | 개별 카드 | 곱연산, 최대 3픽 (전역 3픽 캡, §9.2 L447) |
| `WraithDamageBoost` (P) | Wraith | ×1.5 / 픽 | 개별 카드 | 곱연산, 최대 3픽 |

> Tank 패시브 4장 풀 = {WispHpBoost, WraithDamageBoost, SpawnWraith, ReplaceWispsToWraith}. 진성 7장 빌드는 P4 를 거의 다 픽하므로 **WispHpBoost·WraithDamageBoost 가 최소 1픽씩 들어오는 것이 정상 경로**다. 즉 Tier 배율만의 누적은 현실에서 거의 발생하지 않는다.

#### (a) Tier 단독 배율 (개별 카드 0픽 — 이론적 하한, 실제로는 드묾)

```
Tier1(×1.3) × Tier3(×1.4) = ×1.82
```

| 종 | 베이스 HP | Tier1+Tier3 (×1.82) |
|---|---|---|
| Wisp | 200 | 364 |
| Wraith | 500 | 910 |

#### (b) 실측 누적 — 개별 카드 곱연산 포함 (7장 진성 빌드의 정상 범위)

WispHpBoost·WraithDamageBoost 를 각 1픽한 경우 (가장 흔한 진성 빌드):

```
Wisp   = 200 × 1.3 × 1.4 × 1.5      = 546     (= 364 × 1.5)
Wraith = 500 × 1.3 × 1.4 × 1.5      = 1365    (= 910 × 1.5)
```

각 2픽까지 몰빵한 경우 (3픽 캡 내 상한 근처):

```
Wisp   = 200 × 1.3 × 1.4 × 1.5²     = 819     (= 364 × 2.25)
Wraith = 500 × 1.3 × 1.4 × 1.5²     = 2047.5  (= 910 × 2.25)
```

| 종 | 베이스 HP | Tier만(a) | +개별1픽(b) | +개별2픽 |
|---|---|---|---|---|
| Wisp | 200 | 364 (×1.82) | 546 (×2.73) | 819 (×4.10) |
| Wraith | 500 | 910 (×1.82) | 1365 (×2.73) | 2047.5 (×4.10) |

> **검산 한 줄**: 개별 카드 1픽만 들어와도 Wraith 누적 배율은 ×2.73 으로 이미 ×2 벽을 한참 넘는다. ×2 천장은 Tier 배율(×1.4 vs ×1.5)이 아니라 개별 카드 곱연산이 좌우한다. 이 누적 정책은 card-renewal.md §9.2 L442 ("WispHpBoost 3픽 → ×1.5³=×3.375") Layer2 정책과 정합한다.

> Power(Tier2 ×1.2, ReplaceWispsToWraith ×1.3)는 별개 스탯 표면이므로 HP 곱연산에 관여하지 않는다. 7장 빌드의 Wisp·Wraith 는 위 HP 누적 + Power 누적을 동시에 받는다.

---

## 3. 수치 근거 (§8 밸런스 맥락)

### 3.1 왜 ×1.4 인가 — 대안 비교

핵심 디자인 결정이므로 3 대안을 검토했다. 모두 `RegisterMonsterTypeBuff(_, Hp, X)` 구조로 동일하고 Tier3 단독 배율 X 만 다르다.

**전제 정정**: 이전 검토는 "×1.5 면 Wraith 가 ×2 벽에 근접" 이라며 X 선택이 ×2 천장을 좌우하는 것처럼 다뤘으나, 이는 **개별 패시브 카드 곱연산을 무시한 오류**였다. §2.2(b) 검산대로 7장 진성 빌드는 WispHpBoost·WraithDamageBoost(각 ×1.5/픽) 가 동일 HP 표면에 곱연산되므로, 1픽만 들어와도 Wraith 누적은 이미 ×2.73 이다. 즉 **×2 벽을 넘기는 주범은 Tier3 배율(×1.4 vs ×1.5)이 아니라 개별 카드 곱연산**이며, Tier 배율 한 단계 차이가 만드는 누적 차이(×2.73 → ×2.92, +7%)는 개별 카드가 만드는 배율(×1.5~×2.25)에 비하면 사소하다. 따라서 대안 선택의 실질 기준은 "×2 벽 회피" 가 아니라 **투자-보상 순서를 수치로 드러내는가**다.

| 대안 | Tier3 배율 X | Tier 단독 합산 (참고) | trade-off |
|---|---|---|---|
| A | ×1.3 | ×1.69 | Tier1 과 동일 배율 — "또 같은 HP 1.3" 으로 7장 보상의 체감 차별성이 약함. Tier3 가 Tier1 의 반복처럼 느껴져 투자-보상 순서가 평평해짐 |
| **B (권장)** | **×1.4** | **×1.82** | Tier1(×1.3)보다 한 단계 큰 단독 기여 → "더 깊은 투자 = 더 큰 보상" 순서가 수치로 명확. 내구형이라 화면 물량 폭발에 직접 기여 X (캡 제거 물량 리스크와 독립) |
| C | ×1.5 | ×1.95 | 개별 카드 1픽 포함 시 Wraith ×2.73→×2.92 (대안 B 대비 +7%). 차이는 작지만, Tier1·Tier3 가 둘 다 ×1.5 가 되면 두 Tier 의 **단독 기여가 동률**이라 투자-보상 순서가 모호해짐 (대안 A 의 반대 방향 문제) |

**권장: 대안 B (×1.4)**. 주논거:
1. **투자-보상 순서의 명확성 (주논거, 데이터 없이도 타당)** — Tier3 단독 기여(×1.4)가 Tier1 단독 기여(×1.3)보다 한 단계 크다. "9픽 중 7픽을 한 축에 몰아야 도달하는 극단 빌드" 의 보상이 첫 임계(3장)보다 크다는 사실을 *Tier 사다리 자체의 배율 순서*로 드러낸다 (card-renewal.md §4.3 "Tier3 = 진성 빌드 보상" 정합). 이 논거는 개별 카드 곱연산 유무와 무관하게 성립한다 — 개별 카드는 모든 Tier 에 동일하게 곱해지므로 *Tier 간 상대 순서*는 Tier 배율만으로 결정된다.
2. **캡 제거 물량 리스크와의 독립성** — spec §4 의 캡 제거 핵심 리스크는 "물량 무제한 누적 → 렌더/물리/AI 선형 증가" 다. HP 버프는 **기존 몬스터의 맷집**만 키울 뿐 **새 몬스터를 더 만들지 않으므로** 캡 제거가 만든 물량 리스크를 가속하지 않는다. (출력형/스폰형 효과였다면 캡 제거 리스크와 곱해져 위험했을 것.)
3. **×1.4 vs ×1.5 의 사소함 인정** — 위 전제 정정대로 두 후보의 누적 차이는 실측 기준 +7% 에 불과하다. ×1.4 를 고른 것은 "×1.5 가 위험해서" 가 아니라 **Tier1(×1.3)과 차별화하면서도 Tier 사다리 한 칸씩 올리는 가장 깔끔한 정수 단계(1.3→1.4→…)** 이기 때문이다. 실제 절대 강도(과한가/약한가)는 Tier 배율이 아니라 개별 카드 픽 수가 좌우하므로, 강도 판정은 §3.2 의 qa-simulator 사이클로 넘긴다.

### 3.2 사망 시간 영향 (정성)

card-renewal.md §4.3 의 Tier 의도 사다리는 각 Tier 가 평균 사망을 5~8s 단축한다고 가정한다. 구 Tier3(캡 +6)는 물량(동시 24마리) 으로 DPS 총량을 늘려 사망을 앞당겼다. 신 Tier3(HP ×1.4)는 **방향이 반대** — Wisp·Wraith 가 더 오래 살아남아 영웅의 진로를 더 오래 막고(Tank 정체성 "묶어 둔다"), 그 사이 다른 종/액티브가 일할 시간을 번다. 즉 "직접 DPS 증가" 가 아니라 "탱킹 지속 → 간접 사망 단축" 으로 메커니즘이 바뀐다.

정확한 사망 시간 델타는 **qa-simulator 검증 후 결정**한다. 결정 메트릭:
- 평균 영웅 사망 시각이 §8 기준(2~4분 = 120~240s) 안에 머무는가
- Tank 7장 빌드의 타임오버(5:00 도달, 클리어 실패) 발생률이 0%에 가까운가 — **이 천장을 위협하는 실질 변수는 Tier3 배율(×1.4 vs ×1.5)이 아니라 §2.2(b) 의 개별 카드 곱연산 누적(Wraith ×2.73~×4.10)이다.** 시뮬은 WispHpBoost·WraithDamageBoost 픽 수(0/1/2픽)별로 분리 계측하는 것이 정확하다.

본 기획은 Tier3 단독 배율(×1.4) 만 확정한다. 사망 시간 절대값 튜닝과, 만약 타임오버 빈발이 관측될 경우의 조정 대상(Tier 배율 하향이 아니라 개별 카드 곱연산 상한/정책 재검토가 우선 후보)은 spec §4 가 예고한 별도 qa-simulator 사이클로 넘긴다.

---

## 4. 추가 검토 — 캡 제거 + 액티브 픽 9→5 동시 적용의 페이싱 리스크 (범위 외 · 가벼운 코멘트)

> **범위 명시**: 본 § 은 Tank Tier3 리뉴얼(본 기획서의 결정 대상)의 범위 *밖* 이다. spec §4 가 다루는 사이클 전체 페이싱 리스크에 대한 game-designer 의 비구속 코멘트일 뿐이며, 본 기획서의 결정값(§2·§3)에 어떤 제약도 걸지 않는다. 정식 페이싱 판정·대응은 spec §4 와 후속 qa-simulator 사이클의 소관이다. 아래는 그 사이클 설계 시 참고용 관찰 노트.

spec 은 (A) 캡 제거와 (B) 액티브 픽 9→5 축소를 한 사이클로 묶는다. 두 변경이 **페이싱·난이도를 서로 반대 방향으로 민다** 는 점이 리스크의 핵심이다.

- **(A) 캡 제거**는 난이도를 **낮추는**(영웅 사망 가속) 방향이다. 물량이 truncate 없이 누적 → 영웅 주변 밀집도·총 DPS 상승 → 평균 사망이 더 빨라진다. 특히 Swarm 계열(스포너 주기 단축 ×0.512 상한 + 출력 +1)과 겹치면 캡 부재 효과가 증폭된다.
- **(B) 액티브 픽 9→5**는 난이도를 **높이는**(영웅 사망 지연) 방향이다. 플레이어가 영웅에게 거는 저주/몬스터 버프 횟수가 4회 줄어 빌드 누적 출력이 감소 → 평균 사망이 더 느려진다.

두 힘이 상쇄될지, 한쪽이 우세할지는 데이터 없이는 단정할 수 없다. 위험 시나리오는 **상쇄가 아니라 분포 양극화**다 — 물량형(Swarm) 빌드는 (A) 우세로 더 빨리 끝나고, 저주 의존(Debuff) 빌드는 (B) 우세로 더 느려져, 컨셉 §8 이 경계하는 양극단(초고속 / 타임오버)이 동시에 늘어날 수 있다. 또한 액티브 픽 시점이 {30,90,150,210,270}로 균등해지면서 60·120·180·240초의 "분 단위 박자감"이 사라져, 페이싱의 결정 빈도 체감(컨셉 §4.2 평균 17초마다 1회 → 더 성김)이 달라진다.

**대응**: 별도 수치 조정은 하지 않는다(spec §4 합의). 이 사이클 마무리 후 **qa-simulator 1회 검증을 강하게 권장**한다 — 핵심 메트릭은 (1) 평균 사망 시각의 §8 기준 이탈 여부, (2) 빌드축별 사망 시각 분산 확대 여부, (3) 타임오버 발생률. 이 데이터가 나오면 그때 별 밸런스 사이클을 돈다.

---

## 5. 구현 요청사항 (gameplay-programmer 용)

> 본 기획은 **신규 스탯/Enum/Interface/에셋을 추가하지 않는다.** 기존 자산 재사용만으로 완결된다. plan Task 3 의 `<TIER3_HP_MUL>` 자리에 아래 확정값을 주입한다.

- **Enum**: 신규 없음. 기존 `EMonsterStatKind.Hp`(CommonEnum.cs L94) 와 `EMonster.Wisp` / `EMonster.Wraith` 재사용.
- **Interface**: 신규 없음. 기존 `IBattleContext.RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float)` 재사용 (Tier1/2 가 이미 호출).
- **에셋 키**: 신규 없음. Tank Tier3 는 SO 가 아니라 `Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier3.cs` 코드 영역(card-renewal.md §4.2 비고). 에셋 파일 변경 없음.
- **SO 스키마 / 수치 필드**: 해당 없음 (코드 상수).

### 5.1 TankSynergyTier3.cs 교체 명세

```
- private const int CapDelta = 6;
- ctx.IncrementGlobalMonsterCap(CapDelta);
+ private const float HpMul = 1.4f;   //# <TIER3_HP_MUL> 확정값
+ ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Hp, HpMul);
+ ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, HpMul);
```

- 파일 상단 `using Lair.Data;` 추가 (현 TankSynergyTier3 는 `Lair.Data` 미참조 — Tier1/2 는 이미 참조).
- 클래스 주석을 신효과 기준으로 갱신 (예: "Tank Tier3 (7장 임계) — Wisp·Wraith HP ×1.4 추가 내구 버프 (글로벌 영구). 구 캡 +6 을 캡 제거에 따라 테마 일관 내구 강화로 교체. 기획서 `tank-tier3-renewal.md` §2.").
- Tier1/2 와 동일한 클래스 구조(`IBuildSynergyTier.Apply(IBattleContext ctx)`) 유지.

### 5.2 plan 주입 값

| plan 자리표시자 | 확정값 |
|---|---|
| `<TIER3_HP_MUL>` | `1.4` |

---

## 6. card-renewal.md §4 동기화 필요 (문서 정합)

본 변경 승인 후 card-renewal.md 의 다음 위치를 신효과로 갱신해야 한다 (단일 진실 유지 — gameplay-programmer 구현 시점 또는 docs sync 시점):

- §4.2 Tier 효과 마스터 표: `**필드 캡 +6** (18→24, 영구)` → `Wisp·Wraith HP ×1.4 (글로벌 영구)`
- §4.4 Tier3 특수 효과 설계 의도: "Tank Tier3 (필드 캡 +6)" 항목을 HP ×1.4 내구 버프 의도로 교체
- §4.5 시너지 적용 표면: "Tank Tier3 = 신규 표면 필요: `IncrementGlobalMonsterCap`" 줄 삭제, "Tank Tier1·2·3 / ... = `RegisterMonsterTypeBuff`" 로 Tier3 를 RegisterMonsterTypeBuff 그룹에 편입
- §7.3 / §9.2 정합 확인 (수정 불요 — 검증만): Tier3 가 RegisterMonsterTypeBuff HP 표면에 편입되면서 §7.3 L340 "모든 종 글로벌 스탯 카드 + 시너지 Tier 효과가 동일 표면 사용" 진술이 Tier3 까지 자동 포함한다. WispHpBoost·WraithDamageBoost(×1.5) 와의 곱연산 누적 정책(§9.2 Layer2)도 그대로 적용 — 본 기획서 §2.2(b) 실측 누적이 이 정책의 Tank 7장 구체 사례다. card-renewal.md 본문 수정은 불필요하나, sync 시 이 정합을 깨지 않았는지 확인.

> 이 §6 은 후속 작업 체크리스트다. 본 기획서 자체의 결정값(§2)은 §4 표보다 이 문서가 우선(SoT)이며, card-renewal.md 는 본 변경에 맞춰 따라온다.
