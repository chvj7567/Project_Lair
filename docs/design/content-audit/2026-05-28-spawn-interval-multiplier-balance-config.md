# Content Audit — 2026-05-28 — 스폰 주기 배율 손잡이 BalanceConfig 추가

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.5 (2026-05-22, 마지막 §11.3 패시브 카드 지속 스폰 시맨틱 갱신)
- 참조 spec/plan 수: 8개 (specs: 8개 / plans: 8개)
- 참조 QA 리포트 수: 6개 (최신: 2026-05-26 — 6차 밸런스 시뮬레이션)
- 과거 감사 이력 (git log): 0건 (첫 회차)

## 1. 현황

| 카테고리 | 컨셉 §11 목표 | 실제 구현 | 차이 |
|---|---|---|---|
| 영웅 | 1명 | 1명 (Knight.prefab) | 없음 ✓ |
| 몬스터 | 6종 | 6종 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 없음 ✓ |
| 패시브 카드 | 15장 | 15장 (SO + Effect.cs 각 15개) | 없음 ✓ |
| 액티브 카드 | 10장 | 10장 (SO + Effect.cs 각 10개) | 없음 ✓ |

### 계획 있으나 미구현

- **SpawnPlague 소환처 부재**: `continuous-spawn-round.md §7` 에 "Plague는 초기 Spawner 6개에 포함되지 않는다" 명시. SpawnPlagues·PlagueSlowBoost 두 카드가 사실상 no-op(Spawner=0 이므로 효과 없음). 의도된 설계이나, 패시브 15장 중 2장이 유효성이 없는 상태.
- **스폰 주기 배율 손잡이**: QA 6차 §6.4 "2순위" 권고 — BalanceConfig에 `SpawnIntervalMultiplier` 필드 없음. 현재 각 Spawner의 주기(6~20s)는 `continuous-spawn-round.md §3.1` 하드코딩 수준. 런타임에 배율 조절 손잡이가 없어 HP 단독 knob 한계를 우회할 방법이 없음.
- **가속 배율 아티팩트 재검증**: QA 6차 §5에서 5x vs 15x 차이 −11.4% 관측. N=3으로 통계 검정력 낮아 "유의미한 차이 없음" 증명 못 함. v10 이상 진입 시 15x 측정값 신뢰도 재평가 권고됨.

### QA 권고 미해결

| 출처 | 내용 | 상태 |
|---|---|---|
| QA 6차 §4 ③ | 평균 영웅 사망 76.04s — 목표 ≥80s 미달 (-4.95%) | 미해결 |
| QA 6차 §4 ④ | 5분 타임오버 판 0 — 목표 ≥1판 미달 | 미해결 |
| QA 6차 §4 ⑤ | 클리어율 100% — 목표 ≤80% 미달 | 미해결 |
| QA 6차 §6.4 1순위 | HP 4000→4600 단독 옵션 (③ 통과 예측 ~80.5s, 마진 좁음) | 미실행 |
| QA 6차 §6.4 2순위 | 스폰 주기 배율 손잡이 신규 추가 (분산 확장 → ④⑤ 접근 가능) | 미실행 |
| QA 6차 §5 | 가속 아티팩트 재검증 (N≥10 @5x) | 미실행 |
| QA 6차 §6.5 | Hero HP: Golem HP 비율 이미 ×8. 4600 채택 시 ×9.2 — game-designer 판단 요청 | 미결정 |

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| — | — | 첫 회차 — 이전 감사 이력 없음 |

## 2. 추가 컨텐츠 후보 (권장 1개)

### 스폰 주기 배율 손잡이 — BalanceConfig에 SpawnIntervalMultiplier 추가

- **카테고리**: BalanceConfig 손잡이 추가
- **요지**: `BalanceConfig.asset` 에 `_spawnIntervalMultiplier` 필드(기본값 1.0)를 추가하고, 모든 Spawner 의 스폰 주기에 곱연산으로 적용한다. QA 6차 데이터에서 HP 단독으로 목표 밴드(120~240s)에 도달하는 것이 수학적으로 불가능함이 확인됐으며, 스폰 주기 연장이 분산을 확장해 ④(타임오버 ≥1판)·⑤(클리어율 ≤80%) 통과 경로를 여는 유일한 미검증 손잡이다.
- **검증/구현/시너지/데이터**: 5/2/4/5 → 종합 **18**
- **근거**: QA 6차 리포트 `docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md` §6.4 2순위 / §6.3 "④⑤ 통과는 분모 변수 투입 필수" / `docs/design/continuous-spawn-round.md` §3.1 스폰 주기 표
- **MVP 범위**: 컨셉 §11.2 BalanceConfig 손잡이 튜닝은 §8 밸런싱 기준("영웅이 2~4분 사이 사망") 달성을 위한 수단이므로 MVP 범위 내

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   Unity 에디터에서 `Assets/_Lair/Data/BalanceConfig.asset` 을 인스펙터로 연다. `SpawnIntervalMultiplier` 필드가 기존 몬스터 스탯 항목들 아래에 새로 노출된다. 이 손잡이는 플레이어가 인게임에서 직접 조작하는 것이 아니라 개발자/QA 튜닝 전용 노브다. 씬이 로드될 때 BattleController 가 BalanceConfig 를 읽어 Spawner 에 주기 배율을 전달한다.

2. **화면 변화**
   인스펙터에서 `SpawnIntervalMultiplier` 값을 1.0(기본) → 1.5로 변경하면, 씬 플레이 시작 직후 각 Spawner 의 스폰 주기가 `원래 주기 × 1.5` 로 늘어난다. 예: Phantom Spawner 기준 주기 6.0s → 9.0s. SpawnerStatusPanel 의 진행 바 채워지는 속도가 눈에 띄게 느려진다.

3. **입력 행동**
   개발자가 BalanceConfig Inspector 에서 `SpawnIntervalMultiplier` 슬라이더(범위 0.5~3.0 권장) 를 드래그하거나 직접 값을 입력한다. QA qa-simulator 는 이 값을 코드에서 주입해 자동 시뮬레이션을 돌린다.

4. **시스템 반응**
   BattleController 초기화 시 각 Spawner 에 `multiplier` 를 전달하고, Spawner 는 자신의 `_spawnInterval = baseInterval * multiplier` 로 적용한다. 이미 진행 중인 타이머에는 영향 없이 다음 스폰 주기부터 반영된다(런타임 핫 변경은 MVP 단계에선 불필요). 카드 효과(Spawn 카드, 강화 카드)와 독립적으로 동작하며 중첩 없음.

5. **반복·재발생 패턴**
   QA 시뮬레이션 사이클(라운드 5 이상)에서 이 손잡이만 조정한 채 100판씩 실행해 평균 사망 시각·분산을 재측정한다. 이전 HP 단독 라운드들과 같은 단일 손잡이 원칙을 유지해 교란 변수를 없앤다. multiplier ×1.5, ×2.0 등 단계별로 올리며 파워법칙 교정계수 데이터를 축적한다.

6. **종료·해소 조건**
   평균 영웅 사망 시각 ≥80s (기준 ③), 5분 타임오버 ≥1판 (기준 ④), 클리어율 ≤80% (기준 ⑤) 세 조건이 모두 통과되면 해당 `SpawnIntervalMultiplier` 값이 기준 수치로 확정된다. 이후 이 값은 `BalanceConfig.asset` 에 최종 저장되고 손잡이 탐색이 종료된다.

7. **다른 시스템과 상호작용**
   - Spawn 카드(SpawnWisps 등) 가 Spawner 의 동시 출력 수를 +1 하는 것과 독립 — 주기 배율은 주기에만 곱하고 출력 수에는 영향 없음.
   - Replace 카드로 Spawner 출력 종이 바뀌어도 배율은 그대로 적용됨 — 교체 후 새 종의 주기에도 동일 배율.
   - 강화 카드(버프 배율) 는 몬스터 스탯에 영향, 주기 배율은 Spawner 타이밍에만 영향 — 두 손잡이 완전 직교.
   - SpawnerStatusPanel 진행 바가 채워지는 속도가 배율에 따라 변함 — UI에서 즉시 가시 피드백.

8. **엣지 케이스**
   - `SpawnIntervalMultiplier = 0.0` 또는 음수: 무한 루프/즉시 스폰 방지를 위해 `Mathf.Max(0.1f, multiplier)` 클램핑 필요.
   - 배율이 매우 클 때(예: 10.0): 스폰 주기 200s 이상 → 판 내 몬스터가 거의 나오지 않음. 글로벌 캡(18마리)과의 상호작용에서 캡 미달 시간이 길어져 판이 매우 단조로워짐. 튜닝 범위(0.5~3.0)를 인스펙터 `[Range]` 애트리뷰트로 제한하면 실수 방지 가능.
   - 멀티 Spawner 동기화: 각 Spawner 는 초기 지연(0~2.5s)이 다르므로 주기 배율이 각각에 독립 적용돼도 스폰 분산 효과는 유지됨.

9. **유저 정보·피드백**
   개발자/QA 입장에서 손잡이 변경 후 SpawnerStatusPanel 의 진행 바 속도를 육안으로 확인할 수 있다. qa-simulator 리포트에서 "평균 사망 시각" 과 "클리어율" 지표로 손잡이 효과가 정량 검증된다. 기획서(continuous-spawn-round.md §3.1) 의 주기 표와 `SpawnIntervalMultiplier` 값을 함께 보면 실제 주기를 즉시 역산 가능(예: Phantom 6.0s × 1.5 = 9.0s).

### 보류

- **HP 4000→4600 단독 상향** (QA 6차 §6.4 1순위): game-designer 가 Hero HP : Golem HP 비율 9.2배를 허용하는지 먼저 결정 필요. 스폰 주기 손잡이 대비 종합점수 낮음(검증가치 4/구현비용 1/시너지폭 2/데이터근거 5 → 15). 단일 라운드로 ③만 통과 가능, ④⑤는 구조적으로 불가.
- **저픽률 액티브 카드 재조정 (Fear 13% / Berserk 7%)** (QA 6차 §3.3): 구현비용 1로 낮지만, 전투 시간 자체가 짧아 카드 픽 기회가 부족한 상태에서 조정해도 데이터가 오염됨. 전투 시간 밴드를 먼저 잡은 뒤 카드 균형 조정이 순서상 맞음. 보류.
- **Plague Spawner 초기 배치**: SpawnPlagues·PlagueSlowBoost가 no-op인 상태는 기획 의도(continuous-spawn-round.md §7). 카드 다양성 관점에서 검토 가치 있으나, 기존 밸런스 데이터가 이 상태로 축적됐으므로 변경 시 과거 QA 데이터와의 비교 신뢰도가 떨어짐. 보류.

## 3. 과거 감사 대비 차별성

git log 조회 0건 — 이번이 첫 회차. 과거 감사 이력 없음, 중복 비교 대상 없음.

## 4. 제외 (범위 밖)

- **신규 몬스터 종 추가**: 컨셉 §11.2 "몬스터 6종" 확정, 신규 종은 MVP 후 (§13 TBD)
- **카드 매수 변경 (15장/10장 초과)**: §11.3 매수 lock
- **메타 진행 / 서버 연동**: §11.2 제외 항목
- **사운드 / 비주얼 아트 작업**: CLAUDE.md §8 금지
- **메인 메뉴 / 세팅 화면**: §11.2 제외

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청 — `docs/design/` 하위 기획서 작성 (continuous-spawn-round.md §3 또는 새 파일에 손잡이 정의 추가)
- 이전에 QA 6차에서 권고한 1순위(HP 4600)와의 순서를 game-designer 가 결정 — Hero HP:Monster HP 비율 리스크(§6.5) 평가 후 1순위 먼저 실행할지 2순위(본 권장)로 직행할지 판단
- 채택 후 qa-simulator 로 `SpawnIntervalMultiplier ×1.5` 기준 100판 시뮬 → ③④⑤ 기준 동시 충족 여부 확인
