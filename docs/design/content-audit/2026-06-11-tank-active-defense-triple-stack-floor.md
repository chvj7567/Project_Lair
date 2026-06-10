# Content Audit — 2026-06-11 — Tank 액티브 3중 방어 동시 활성 시 Wisp·Wraith 데미지 감소 하한 설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.6 (2026-05-31)
- 참조 spec/plan 수: 27개 specs / 27개 plans (전체 목록 참조)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태, 실행 데이터 없음)
- 과거 감사 이력 (git log): 4건 (가장 최근: 2026-06-09 UTC — 파일명 기준 2026-06-10)

---

## 1. 현황

| 카테고리 | 컨셉 §11 기준 | 실제 구현 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (기사) | 1명 (Knight.prefab) | 없음 ✓ |
| 몬스터 | 6종 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) | 6종 프리팹 모두 확인 | 없음 ✓ |
| 패시브 카드 | 16장 (4축 × 4장) | 16장 .asset 확인 | 없음 ✓ |
| 액티브 카드 | 12장 (4축 × 3장) | 12장 .asset 확인 | 없음 ✓ |
| 카드 효과 클래스 | 28종 | 28개 .cs 확인 | 없음 ✓ |

### 계획 있으나 미구현

- **SwarmRush (Swarm 액티브 #6)**: `card-renewal.md §3.4` 원안에서 Multiply(FastBreedingEffect) → SwarmRush (Phantom 6마리 즉시 소환)로 교체 예정이었으나, 현행 `Multiply.asset` ("빠른 번식") 잔존. `ECardId.Multiply` enum 유지 중 — 별도 구현 사이클 대기.
- **QA DebugAutoPicker 훅 (`BattleController`)**: `2026-05-22 QA 리포트 §3` 에서 gameplay-programmer 에 요청됐으나 구현 여부 미확인 (QA 리포트 이후 추가 리포트 없음).

### QA 권고 미해결

- `2026-05-22` QA 리포트: `BattleController` 에 `#if UNITY_EDITOR` 델리게이트(`DebugAutoPicker`) 추가 — 헤드리스 시뮬레이션 활성화용. 요청 이후 QA 리포트 없음 → 해결 여부 불명.
- **이 권고가 해결되지 않으면 데이터 기반 밸런스 검증이 불가능** — 모든 이후 감사가 수치 추론에 의존하는 상태 지속.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (커밋 시각) | 커밋 SHA | subject 설명 (카테고리 + 요지) |
|---|---|---|
| 2026-06-09 22:11 UTC | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 22:11 UTC | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-07 22:16 UTC | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |
| 2026-06-05 22:12 UTC | e198aa4 | Swarm Tier2×SpawnerHaste 복합 스택 — Nova 쿨 가드레일 재검토 |

*비고*: 위 4건은 신규 포맷(`# [Routines][Daily Content Audit]`) 기준이다. 구 포맷(`# [docs] - 컨텐츠 감사`) 커밋은 별도 grep 에서도 미검출 — 해당 시기의 감사는 폴더 파일 존재(2026-05-28~2026-06-04)로 간접 확인되나, 상세 내용은 git log 조회 범위 밖이다.

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Tank 액티브 3중 방어 동시 활성 시 Wisp·Wraith DamageTakenScale 하한 설계

- **카테고리**: Tank 축 패시브·액티브 밸런스 재조정
- **요지**: Tank 축의 3가지 액티브 카드(IronWill · ToughHide/WallOfWisps · GuardianRage/Berserk)가 동시 활성화될 때 Wisp·Wraith의 DamageTakenScale이 이론상 0.2625까지 감소(73.75% 데미지 감소)한다. 영웅 공격력 50 DPS 기준 실질 DPS가 13.1로 떨어져, HP 최대 강화 Wraith(975 HP) 한 마리를 제거하는 데 74초가 소요되는 극단 시나리오가 발생한다. `DamageTakenScale` 에 하한값(0.35~0.40)을 도입해 Tank 정체성을 유지하면서 과도한 수비 누적을 완화할 것을 제안한다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 5 / 3 / 5 / 3 → 종합 **16**
- **근거**:
  - `docs/design/card-renewal.md §3.1` #5·#6·#7 — IronWill(×0.7, 15s, 전체종) · ToughHide(×0.75, 영구, Wisp+Wraith) · GuardianRage(×0.5, 15s, Wisp+Wraith) 각각 독립 `EMonsterBuff` 값이므로 `AddBuff` dedup 은 동일 enum 내부에서만 작동, **서로 다른 세 버프는 동시 활성 가능**
  - `docs/design/card-renewal.md §3.1` 노트: "IronWill·ToughHide·GuardianRage 는 다른 EMonsterBuff 값이므로 `AddBuff` dedup 이 각각 독립 동작. 동시에 3개 모두 활성 가능"
  - `docs/design/project_lair_concept.md §11.3` — Wraith 기본 HP 500, 영웅 공격력 50/타·공속 1초
  - `docs/design/card-renewal.md §4.2` Tank Tier1(HP ×1.3)·§7.1 WispHpBoost/WraithDamageBoost(HP ×1.5 곱연산)
  - `docs/qa-reports/2026-05-22.md` §4 — 평균 사망 기준 76초 목표, 시뮬 미실행으로 실 데이터 없음
- **MVP 범위**: 컨셉 §11.2 포함 항목(패시브·액티브 카드 밸런스), §11.3 Tank 축 수치 범위 내

#### 핵심 수치 추론

> 시뮬레이션 데이터 없이 설계 문서 수치로 추론한 최악 시나리오. QA 데이터 수집 후 재검증 필요.

**Wraith HP 최대 강화 경로**:
- 기본 HP 500
- WraithDamageBoost 3픽: ×1.5³ = ×3.375 → 1687 HP (이론 상한, 전역 3픽 캡)
- WispHpBoost는 적용 안 됨(Wisp 전용)
- Tank Tier1 (Wraith·Wisp HP ×1.3): ×1.3 추가 → **2193 HP**
- (현실 2픽 기준): WraithDamageBoost 2픽(×2.25) × Tier1(×1.3) = 500 × 2.25 × 1.3 = **1462 HP**

**영웅 실질 DPS vs Tank 몬스터 (3중 방어 동시 활성)**:
| 활성 버프 | 적용 종 | DamageTakenScale |
|---|---|---|
| 없음(기본) | 전체 | 1.00 |
| ToughHide만 (영구) | Wisp+Wraith | 0.75 |
| ToughHide + GuardianRage (15s) | Wisp+Wraith | 0.375 |
| ToughHide + GuardianRage + IronWill (15s) | Wisp+Wraith | **0.2625** |

- 영웅 기본 DPS: 50
- 3중 중첩 시 실질 DPS: 50 × 0.2625 = **13.1 DPS**
- 2픽 Wraith(1462 HP) 제거 시간: 1462 / 13.1 = **약 112초** (거의 2분)
- 1픽 Wraith(975 HP) 제거 시간: 975 / 13.1 = **약 74초**

→ 컨셉 §8 기준 "평균 영웅 사망 2~4분" 달성이 불가능해지는 시나리오. Wraith 1마리를 처치하는 데 런 시간의 절반이 소요된다.

**ToughHide 추가 문제 — 픽 낭비 패턴**:

`ToughHide(WallOfWisps)` 는 `AddBuff` dedup 으로 **멱등**이다 — 같은 `EMonsterBuff.ToughHide` 는 인스턴스 1개만 유지되어 2번째·3번째 픽은 효과 없이 소비된다. 플레이어가 이를 모르고 반복 픽하면 Tank 카운트만 올라갈 뿐 실질 버프는 없다. IronWill과 GuardianRage는 dedup 은 되지만 Remain 을 연장해 유용성이 있는 반면, ToughHide는 중복 픽이 완전한 낭비다. **이 정보가 UI에 노출되지 않는 것도 UX 문제**다.

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**
30초 액티브 트리거마다 Tank 축 액티브 카드 3종(IronWill·WallOfWisps·Berserk)이 후보에 포함될 수 있다. 특히 패시브에서 Tank 카드를 집중 픽해 Tier1(3장)을 달성하면, 이후 액티브 풀에서도 Tank 카드가 지속 노출되어 3중 방어 중첩을 향한 경로가 열린다. 첫 중첩 위험 시점은 30s(첫 액티브)와 60s(두 번째 액티브) 모두 다른 Tank 액티브를 골랐을 때로, 전투 1분 내에 이미 2중 방어가 가능하다.

**2. 화면 변화**
카드 선택 팝업 상단 빌드 카운트 바에 Tank 수치가 올라가고, 팝업이 닫히면 전투 화면 좌상단 시너지 패널의 Tank 행에 축 아이콘이 추가된다. Tank 액티브를 픽할 때마다 전투 화면에서 위스프·레이스의 색상이나 시각 변화가 있어야 하지만, 현재 구현에서는 ToughHide 영구 버프가 적용됐음을 알리는 별도 시각 피드백이 없다.

**3. 입력 행동**
플레이어가 30초마다 뜨는 카드 팝업(3택 1)에서 IronWill("강철 의지"), WallOfWisps("단단한 살갗"), Berserk("수호자의 분노") 중 하나를 선택한다. 각각 다른 EMonsterBuff 값이므로 세 카드를 각각 한 번씩 픽하면 세 버프 모두 동시 활성화된다. WallOfWisps를 2번째 픽하면 효과가 없지만(dedup), Tank 카운트는 올라가 Tier 진입에는 기여한다.

**4. 시스템 반응**
ToughHide 픽 시: `MonsterBuffService.AddBuff(ToughHide, -1f)` 호출 → 현재 필드·이후 스폰 Wisp·Wraith 의 `DamageTakenScale *= 0.75f` (영구). GuardianRage 픽 시: 15초 시한 버프로 Wisp·Wraith `DamageTakenScale *= 0.5f`. IronWill 픽 시: 15초 시한 버프로 전체 종 `DamageTakenScale *= 0.7f`. 세 버프가 동시 활성이면 Wisp·Wraith의 최종 DamageTakenScale = 0.75 × 0.5 × 0.7 = **0.2625** (영웅이 가한 데미지의 26.25%만 실제 적용).

**5. 반복·재발생 패턴**
GuardianRage와 IronWill은 15초 지속이므로 30초마다 뜨는 액티브 주기에 맞춰 매 턴 갱신하면 사실상 상시 유지된다(3픽 캡 이전까지). 전역 3픽 캡 이후에는 시한이 다하면 해소되지만, ToughHide 의 영구 효과는 라운드 내내 남는다. 따라서 1픽(0.75) 상시 + 2개 시한 버프가 겹치는 15초 창은 5분 런 내에 여러 차례 반복된다.

**6. 종료·해소 조건**
IronWill과 GuardianRage는 각각 15초 후 해소되어 `DamageTakenScale` 이 다시 올라간다. 하지만 ToughHide(×0.75)는 라운드 종료까지 영구 유지된다. 영웅이 처치되거나 5분 타이머가 만료될 때만 모든 버프가 해소된다.

**7. 다른 시스템과 상호작용**
Dps 축의 Tier1(Reaper·Hex Power ×1.3)·Tier2(Cooldown ×0.8)로 영웅 DPS를 높이더라도, Wisp·Wraith의 DamageTakenScale 0.2625 앞에서는 실질 증가분이 크게 희석된다. Tank Tier1(HP ×1.3) + WispHpBoost/WraithDamageBoost 중첩(최대 HP ×3.375) 까지 더해지면 수치 조합이 설계 의도를 벗어난 "무적 몬스터" 시나리오를 만든다. 반대로 Debuff 축의 Plague·출혈·공포 디버프는 영웅 HP를 직접 깎거나 이동 속도를 낮추므로 Tank 방어 버프의 영향을 덜 받아 상대적으로 유리해진다.

**8. 엣지 케이스**
ToughHide 멱등 문제: WallOfWisps를 2번째 이후 픽하면 Tank 카운트만 올라가고 방어 버프는 증가하지 않는다. 이 정보가 UI에 없어 플레이어가 인지하기 어렵다. IronWill은 전체 종 대상이라 Phantom, Plague 같은 비Tank 몬스터도 15초 동안 -30% 보호받는다 — Swarm 또는 Debuff 빌드와 혼합 시 예상치 못한 상호작용이 발생한다. GuardianRage가 만료되기 직전에 다시 픽하면(15s + 15s = 30s 커버리지), 해소 타이밍 없이 사실상 연속 적용된다.

**9. 유저 정보·피드백**
현재 DamageTakenScale 누적값이 UI에 표시되지 않는다. 영웅이 공격을 가해도 몬스터 HP 바가 거의 줄지 않는 것으로 간접 체감할 뿐, 왜 그런지 알기 어렵다. "Tank 몬스터가 너무 살아남는다" 는 인상을 주지만 어느 버프가 원인인지 판단할 정보가 없다. ToughHide가 dedup(멱등)이라는 사실도 UI에 표시되지 않아 2번째 픽이 낭비임을 모르고 선택할 수 있다.

---

### 보류

| 후보 | 보류 이유 |
|---|---|
| Debuff Tier2 HeroAttackDown 자동 등록 하한 없음 | 검증가치 4, 시너지폭 3 — 종합 13. 차순위. 추후 별도 감사 권장 |
| TimeStop + Fear 중첩 영웅 잠금 | 검증가치 4, 데이터근거 3 — 종합 14 (`card-renewal.md §7.2` 모니터링 항목 명기). 차차순위 |
| Multiply(FastBreeding) 미구현 SwarmRush 교체 | 컨셉 §11.3 Swarm A 자리 설계 미완 — 단순 밸런스가 아닌 구현 작업 필요 → game-designer 신규 기획 사이클 |

---

## 3. 과거 감사 대비 차별성

git log 조회 4건 검토 완료. 가장 유사했던 과거 커밋:
- `440794c` (2026-06-09): **Dps 패시브 HexRangeBoost Tier3 중첩** — Dps 축 카드 배율 상한 문제
- `307ec17` (2026-06-07): **Dps ReaperAtkSpeed Tier2 중첩 하한** — Dps 액티브 배율 누적

이번 제안은 **Tank 축** 이며 **액티브 방어 감소의 동시 활성 문제**로, Dps 축 누적 배율 상한과 카테고리·요지·근거가 모두 다르다. 또한 2026-06-05의 Tank Tier3(필드 캡 +6)와 같은 Tank 축이지만, Tier3 감사는 **글로벌 필드 캡 확장** 메커니즘에 집중한 반면 이번은 **액티브 카드 3종의 DamageTakenScale 곱연산 누적과 하한 부재** 문제다 — 분석 대상(Tier 발화 vs 개별 카드 버프 스택)과 해결 방향(캡 수치 vs 하한값 도입)이 명확히 다르다. 

---

## 4. 제외 (범위 밖)

| 항목 | 제외 이유 |
|---|---|
| DamageTakenScale 시각 표시 UI 신설 | 컨셉 §11.2 UI 항목("일시정지 + 큐") 외 신규 UI — 현재 MVP 범위 명시 없음, game-designer 승격 필요 |
| 영웅 공격력 UI 피드백 (얼마나 막혀있는지) | 동일 이유 |
| Tank Tier4 설계 | 컨셉 §11 밖 (3Tier까지만) |
| 영구 IronWill 카드 신설 | 신규 카드 — 패시브 16/액티브 12 매수 lock |

---

## 5. 다음 단계 제안

- **채택 시**: game-designer 에게 정식 기획 요청 — `MonsterBuffService` 에 `DamageTakenScale` 하한(0.35 또는 0.40) 도입. 구현 난이도 낮음(MonsterBuffService 단일 파일 수정).
- **선행 확인 권장**: QA DebugAutoPicker 훅 구현 여부 확인 — 시뮬 데이터가 있으면 최악 시나리오 발생 빈도를 실제 데이터로 검증 가능.
- **ToughHide dedup 알림 UI**: WallOfWisps 픽 시 "이미 효과가 영구 적용됨" 표시 — game-designer 정식 기획 후 gameplay-programmer 구현.
- **보류 2순위 (TimeStop + Fear)**: 다음 감사 회차 우선 검토 — `card-renewal.md §7.2` 에서 모니터링 항목으로 이미 플래그 된 사항.

---

## 6. 쉬운 설명 (비개발자 요약)

우리 게임에서 플레이어는 몬스터를 강화하는 카드를 골라가며 영웅을 물리치는 던전 주인이다. 특히 "탱커(Tank)" 몬스터인 위스프와 레이스는 단단하게 만드는 카드가 세 장 있는데, 이 세 장을 모두 쓰면 영웅의 공격력이 사실상 4분의 1 이하로 줄어든다. 예를 들어 강화된 레이스 한 마리를 처치하는 데 1분이 넘게 걸리는 상황이 생길 수 있어서, 영웅이 5분 동안 몬스터들한테 계속 맞는데 정작 몬스터를 하나도 못 잡는 꼴이 된다. 그래서 이번에 제안하는 것은: 몬스터가 받는 데미지를 줄여주는 방어 효과들을 모두 합쳐도 최소 35~40%는 피해를 받도록 하한선을 만들어, 어떤 조합을 써도 영웅이 전혀 못 싸우는 상황을 막자는 것이다.
