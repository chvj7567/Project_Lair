# Content Audit — 2026-06-26 — Slow 카드 이중 효과 배율(SlowMonsterAccelMul×1.3 / SlowHeroSlowFactor×0.5) BalanceConfig 손잡이 미설계

> 작성: Daily Content Audit Routine
> 날짜: 2026-06-26
> 축: Swarm (액티브 #7)

---

## 0. 입력 스냅샷

| 항목 | 값 |
|---|---|
| 오늘 날짜 | 2026-06-26 |
| 현재 단계 | v0.3 |
| 참조 컨셉 §§ | §5.2 (시너지), §8 (밸런싱 기준), §11.3 (카드 테이블) |
| git log 감사 이력 (신규 포맷) | 16건 (2026-06-08 ~ 2026-06-25) |
| 최근 Swarm 감사 | 2026-06-22 — MinSpawnPeriodScale 하한 하드코딩 |
| 최근 Dps 감사 | 2026-06-23 — PhantomMoveSpeedBoost 3-pick 누적 검증 |
| BalanceConfig 경로 | `Assets/_Lair/Data/BalanceConfig.asset` |
| 카드 SO 경로 | `Assets/_Lair/Art/Cards/Items/Slow.asset` |
| 효과 클래스 | `Assets/_Lair/Scripts/Card/Effects/SlowEffect.cs` |

---

## 1. 현황 — Slow 카드 이중 효과 배율 하드코딩

### 1.1 카드 정의

`ECardId.Slow` (Swarm 액티브 #7, card-renewal.md §3.4):

| 필드 | 현재 값 | 위치 |
|---|---|---|
| `_heroFactor` | `0.5` | `SlowEffect.cs` 하드코딩 |
| `_monsterMul` | `1.3` | `SlowEffect.cs` 하드코딩 |
| `_duration` | `10`초 | `SlowEffect.cs` 하드코딩 |
| 버프 종류 | `EMonsterBuff.SwarmSpeed` | 전 종족 적용, dedup (동일 버프 스택 불가) |
| 지속시간 정책 | duration-stack (남은 시간 연장) | card-renewal.md §7.2 |

### 1.2 이중 효과 구조

Slow 카드는 한 번의 픽으로 **두 벡터를 동시에 움직인다**:

1. **영웅 이속 감속** (`_heroFactor=0.5`): 영웅의 이동속도를 10초 동안 ×0.5로 떨어뜨린다 — 영웅이 느려질수록 몬스터가 더 많은 타격 기회를 얻는다.
2. **몬스터 이속 가속** (`_monsterMul=1.3`): `EMonsterBuff.SwarmSpeed` 를 통해 전 종족 이속을 10초 동안 ×1.3로 높인다 — 몬스터가 빠르게 따라붙는다.

두 값 모두 `SlowEffect.cs` 에 하드코딩되어 있으며, `BalanceConfig.asset` 에 대응 손잡이가 없다.

### 1.3 시너지 결합 시나리오 (복합 최대치)

2026-06-16 감사(PhantomMoveSpeedBoost + MaxMoveSpeedScale) 와 2026-06-23 감사(3-pick 누적) 데이터를 교차 적용한 복합 시나리오:

| 단계 | 배율 | Phantom 이속 (기준 ≈ 2.4 m/s) |
|---|---|---|
| 기준 | ×1.0 | 2.40 m/s |
| PhantomMoveSpeedBoost 3-pick (×1.5³) | ×3.375 | 8.10 m/s |
| Swarm Tier1 (Phantom·Wisp ×1.3, 영구) | ×1.3 | 10.53 m/s |
| Slow SwarmSpeed (×1.3, 10초) | ×1.3 | **13.69 m/s** |

→ MaxMoveSpeedScale 손잡이가 2026-06-16 에 제안됐으나 아직 미구현.  
→ Slow SwarmSpeed 와 Tier1 SwarmSpeed 가 **독립 배율로 중첩**되는 구조 확인 필요.  
→ 10.53 m/s 를 이미 cap 없이 통과한 상태에서 Slow 픽 시 13.69 m/s 까지 단기 폭발.

---

## 2. 추가 컨텐츠 후보 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이

### 2.1 제안 요약

`SlowEffect.cs` 의 두 하드코딩 값을 `BalanceConfig` 로 끌어올려 인스펙터에서 조정 가능하게 한다:

| 신규 손잡이 | 제안 기본값 | 현재 값 (하드코딩) | 영향 방향 |
|---|---|---|---|
| `SlowMonsterAccelMul` | `1.3f` | `_monsterMul=1.3` | 높일수록 몬스터 이속 폭발↑ |
| `SlowHeroSlowFactor` | `0.5f` | `_heroFactor=0.5` | 낮출수록 영웅 감속↑ (0=정지) |

### 2.2 유저 플로우 9개

#### Flow 1 — 기준 플로우 (Slow 단독)
1. 플레이어가 30초 시점 액티브 픽에서 Slow 선택
2. `SlowEffect.Apply(_ctx)` 호출 → `EMonsterBuff.SwarmSpeed` 등록 (전 종족), 영웅 속도 ×0.5 적용
3. 10초 동안 몬스터 이속 ×1.3, 영웅 이속 ×0.5 유지
4. 10초 후 버프 만료 → 원래 속도 복귀
5. **밸런스 포인트**: `SlowMonsterAccelMul=1.3` 이 너무 낮으면 체감 효과 미미, 너무 높으면 몬스터 폭주

#### Flow 2 — 3-pick cap 적용 후 스택 플로우
1. 플레이어가 Slow를 2회 픽 (3-pick cap 중 2소진)
2. 2픽 시점 버프가 이미 활성 → `EMonsterBuff.SwarmSpeed` dedup → 지속시간만 연장 (duration-stack)
3. 3픽 시점 재픽 → cap 만료 후 추가 픽 불가 (3-pick global cap)
4. **밸런스 포인트**: duration-stack 으로 최대 30초 연속 가속이 이론상 가능 — `SlowMonsterAccelMul` 이 높으면 30초 내내 폭발 속도

#### Flow 3 — Swarm Tier1 + Slow 조합
1. Phantom·Wisp 픽 3장 이상 → Swarm Tier1 영구 활성 (이속 ×1.3)
2. 이후 Slow 픽 → SwarmSpeed 버프 추가 적용 (×1.3 또는 독립 중첩)
3. Phantom 이속: 기준 × Tier1(1.3) × Slow(1.3) = **×1.69**
4. **밸런스 포인트**: 두 SwarmSpeed 버프가 독립 배율인지 동일 dedup 그룹인지 구현 확인 필요 → `SlowMonsterAccelMul` 조정 폭이 Tier1 과의 결합 여부에 따라 달라짐

#### Flow 4 — PhantomMoveSpeedBoost 3-pick + Swarm Tier1 + Slow 조합 (최대치)
1. PhantomMoveSpeedBoost 3-pick → Phantom ×3.375
2. Swarm Tier1 → ×1.3 추가 → 10.53 m/s
3. Slow 픽 → SwarmSpeed ×1.3 → **13.69 m/s** (10초)
4. MaxMoveSpeedScale 손잡이 미구현 상태 → 어떤 cap 도 없음
5. **밸런스 포인트**: `SlowMonsterAccelMul` 이 인스펙터 손잡이라면 최대치 실험 후 1.1~1.2 로 하향 가능

#### Flow 5 — SlowHeroSlowFactor 극단값 테스트 (영웅 준정지)
1. `SlowHeroSlowFactor=0.1` (손잡이 조정 시나리오) — 영웅 이속 ×0.1
2. 영웅이 사실상 멈춰서 몬스터 집중 타격
3. 10초 안에 영웅 체력 대량 감소 → 패시브 카드 연속 발화 가능
4. **밸런스 포인트**: 0.1 은 너무 강력할 수 있어 0.3~0.5 구간이 적정 — 손잡이 없으면 테스트 불가

#### Flow 6 — Debuff Tier1 + Slow 조합 (복합 디버프)
1. Debuff 카드 3장 이상 → Debuff Tier1 활성 (영웅 방어력 감소 등)
2. Slow 픽 → 영웅 이속 ×0.5 추가
3. 영웅이 느리면서 방어력도 낮아 → 몬스터 DPS 배율 효과 극대화
4. **밸런스 포인트**: Debuff×Slow 복합 상태가 5분 이내 처치를 너무 쉽게 달성하면 승률 목표(§8: 40~60%) 이탈 위험

#### Flow 7 — 액티브 트리거 30초 주기와 Slow 타이밍
1. 액티브 트리거는 30초 주기 → Slow 가능 시점: 30s / 60s / 90s ...
2. Slow duration=10초 → 30초 주기 중 10초만 효과 (coverage 33%)
3. `SlowMonsterAccelMul=1.3` → 10초 × 33% coverage = 지속 효과 체감 상대적으로 약함
4. **밸런스 포인트**: coverage 낮다면 `SlowMonsterAccelMul` 을 높이거나 `_duration` 을 15~20초로 늘리는 방향 — 현재 두 값 모두 손잡이 없어 실험 단위가 없음

#### Flow 8 — 패시브 트리거(HP 10%마다)와 Slow 상호작용
1. Slow 픽 → 영웅 이속 ×0.5 → 몬스터 타격 증가 → HP 하락 가속
2. HP 하락 가속 → 패시브 트리거 더 빨리 발화 → 추가 카드 픽
3. 추가 카드 픽이 Tank 계열이면 몬스터 생존력도 동시 상승
4. **밸런스 포인트**: Slow 가 패시브 트리거 발화 빈도를 간접 가속하는 피드백 루프 존재 — `SlowHeroSlowFactor` 가 낮을수록 루프 강도 증가

#### Flow 9 — 게임 후반(3분 이후) Slow 픽
1. 3분 이후 영웅 HP 50~60% 수준 (§8 목표: 2~4분 사망)
2. 이 시점 Slow 픽 → 영웅 이속 ×0.5 + 몬스터 이속 ×1.3
3. 이미 강화된 Swarm 빌드에 Slow 추가 → 사망 타이밍 단축
4. **밸런스 포인트**: 후반에 Slow 를 픽했을 때 사망 타이밍이 4분 이내로 들어오는지 — `SlowMonsterAccelMul` 이 너무 낮으면 후반 Slow 가 의미 없음

### 2.3 밸런스 근거

- **2026-06-16 감사**: PhantomMoveSpeedBoost 3-pick + Swarm Tier1 = 10.53 m/s, MaxMoveSpeedScale 손잡이 미구현 지적
- **현재 상태**: Slow SwarmSpeed 추가 시 10.53 × 1.3 = 13.69 m/s 도달 가능 — cap 없음
- **손잡이 부재의 리스크**: 13.69 m/s 가 over-tune 으로 판명돼도 `SlowMonsterAccelMul` 없이는 하드코딩 수정만 가능 → 빠른 반복 불가

### 2.4 점수

| 축 | 점수 (1-5) | 근거 |
|---|---|---|
| 검증가치 | 4 | 하드코딩 두 값 모두 시너지 최대치에 직결; 손잡이 없으면 복합 시나리오 빠른 조정 불가 |
| 구현비용 | 4 (→점수 2) | BalanceConfig 필드 2개 추가 + SlowEffect 참조 교체 — 약 15~20줄, 난이도 낮음 |
| 시너지폭 | 4 | Swarm Tier1·PhantomMoveSpeedBoost·Debuff Tier1·패시브 트리거 피드백 루프 4개와 교차 |
| 데이터근거 | 3 | 2026-06-16·06-23 감사 수치 교차 계산; QA 시뮬 미실행 상태 |
| **종합** | **13** | (4 + 2 + 4 + 3) |

---

## 3. 과거 감사 대비 차별성

| 날짜 | 감사 대상 | 이번과의 차이 |
|---|---|---|
| 2026-06-22 | MinSpawnPeriodScale 하한 (×0.512 cap) | 스폰 **주기** 하한 — 이번은 이속 **가속 배율** 원천값 |
| 2026-06-16 | MaxMoveSpeedScale cap 미구현 | 이속 **상한** 손잡이 — 이번은 Slow SwarmSpeed **증분 배율** 손잡이 |
| 2026-06-23 | PhantomMoveSpeedBoost 3-pick 누적 검증 | Dps 축 영구 버프 — 이번은 Swarm 축 **10초 한시** 이중 효과 |

→ 스폰 주기(2022-06-22)·이속 상한(06-16)·Dps 누적(06-23)을 모두 다룬 뒤, 이번이 그 세 값의 **중간 링크인 Slow SwarmSpeed 증분 배율**을 처음으로 지적한다.

---

## 4. 제외 후보 및 사유

| 후보 | 제외 사유 |
|---|---|
| EternalBleedAura balance | 2026-06-02 감사 기록 존재 (folder 확인) |
| SwarmRush 교체 | 2026-06-04 감사 기록 존재 (folder 확인) |
| Swarm Tier3 갱신 | 2026-06-19 git log 기록 존재 |
| Tank Tier2 HealthRegen | 2026-06-25 감사 기록 존재 (최신, 재사용 불가) |
| 시너지 Tier 임계값 하드코딩 | 2026-06-25 git log 기록 존재 |

---

## 5. 다음 단계

1. **사용자 / game-designer**: 손잡이 기본값 확정 (제안: `SlowMonsterAccelMul=1.3f`, `SlowHeroSlowFactor=0.5f`)
2. **game-designer**: Flow 3 (Tier1 SwarmSpeed와 Slow SwarmSpeed 중첩 방식) 검토 — 독립 배율 vs dedup 여부 명확화
3. **gameplay-programmer**: `BalanceConfig` 에 `SlowMonsterAccelMul`, `SlowHeroSlowFactor` 필드 추가 + `SlowEffect.cs` 참조 교체 (~15~20줄)
4. **code-reviewer**: BalanceConfig 필드 추가 패턴 일관성 확인
5. **test-engineer**: 손잡이 변경 후 Flow 4 (최대치 복합) 회귀 테스트 작성
6. **(장기) qa-simulator**: `BattleController.DebugAutoPicker` 구현 후 Slow 3-pick 전략 시뮬 — `SlowMonsterAccelMul` 구간별 승률 측정

---

## 6. 쉬운 설명 (비개발자 요약)

Slow 카드를 고르면 두 가지 일이 동시에 일어납니다 — 영웅이 느려지고, 몬스터들은 더 빨라집니다. 그런데 이 "얼마나 느려지고, 얼마나 빨라지는지"를 정하는 숫자가 코드에 고정되어 있어서 개발자가 직접 코드를 바꾸지 않으면 조절할 수가 없습니다. 다른 강화 카드와 Slow를 함께 쓰면 몬스터가 너무 빠르게 달려올 수 있는데, 숫자 손잡이가 없으면 과하게 강할 때 빠르게 낮추기 어렵습니다. 이 두 숫자를 게임 설정 파일에 옮겨두면 코드 수정 없이 간단하게 조율할 수 있게 됩니다.
