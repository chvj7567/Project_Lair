# Daily Content Audit — 2026-06-23

**주제**: Swarm 패시브 `Multiply` × `SpawnerHaste` 3픽 복합 — 팬텀 스폰 주기 0.664s 극단 시나리오 · `MinSpawnPeriodScale` 손잡이 미설계

---

## §1 후보 선정 근거

| 항목 | 값 |
|---|---|
| 검증 가치 | 4 |
| 구현 비용 역수 | 4 (비용 2) |
| 시너지 폭 | 4 |
| 데이터 근거 | 4 |
| **합계** | **16 / 20** |

**과거 감사와의 차별점**

| 날짜 | 내용 |
|---|---|
| 2026-06-16 | Swarm PhantomMoveSpeedBoost — 이속(speed) 축 |
| 2026-06-17 | Swarm SpawnWisps — Wisp 수량 · Tank·Swarm 교차 딜레마 |
| 2026-06-13 | Swarm Tier2 스포너 주기×0.85 하드코딩 — 코드 구조 이관 |

오늘 후보는 **카드 효과 레이어(Multiply · SpawnerHaste) 두 장의 3픽 곱연산이 만드는 스폰 주기 극단값**이다. Tier 시너지 하드코딩(2026-06-13) · 이속(2026-06-16) · 수량(2026-06-17) 과 축이 다르다.

---

## §2 관련 카드 스펙

### Multiply (ECardId.Multiply)

- 트리거: 패시브 (영웅 HP% 구간)
- 효과 클래스: `FastBreedingEffect`
- 대상: 팬텀(Phantom) 스포너 주기만
- 스택 방식: **곱연산** (`_spawnPeriod *= _periodMul`)
- 픽당 배율: ×0.6
- 최대 픽: 3
- 3픽 누적: `0.6³ = ×0.216`

### SpawnerHaste (ECardId.SpawnerHaste)

- 트리거: 패시브 (영웅 HP% 구간)
- 효과 클래스: `SpawnerHasteEffect`
- 대상: **모든** 스포너 주기
- 스택 방식: **곱연산** (`_spawnPeriod *= _periodMul`)
- 픽당 배율: ×0.8
- 최대 픽: 3
- 3픽 누적: `0.8³ = ×0.512`

---

## §3 수치 시뮬레이션

### 3-1 팬텀 기본 주기

`docs/design/continuous-spawn-round.md` 기준:

- 스포너 #3 (Phantom, 120°, 연속 스폰 모델)
- 기본 스폰 주기: **6.0s**
- 초기 딜레이: 1.0s

### 3-2 Multiply 3픽 + SpawnerHaste 3픽 복합

```
팬텀 주기 = 6.0 × (Multiply 3픽) × (SpawnerHaste 3픽)
           = 6.0 × 0.216 × 0.512
           = 0.664s
```

### 3-3 + Swarm Tier2 (모든 스포너 ×0.85)

```
0.664 × 0.85 = 0.564s
```

Swarm Tier2 달성 조건: Swarm 축 카드 5장 (위 6픽 중 일부가 Swarm 축이므로 Tier1 · Tier2 동시 달성 가능)

### 3-4 + SpawnPhantoms 3픽 · Swarm Tier3 수량 버프

- SpawnPhantoms 3픽: 스폰 시 팬텀 +3마리
- Swarm Tier3 (7장): 추가 +1마리
- 사이클당 **5마리 스폰**

### 3-5 글로벌 캡 포화 시점

- 글로벌 몬스터 캡: 18마리 (`docs/design/continuous-spawn-round.md`)
- 팬텀 초기 4마리 (스포너 배치 기준) 가정
- 남은 슬롯: 14마리 → **약 2.0s 만에 글로벌 캡 포화**

### 3-6 하드웨어 플로어 vs 소프트 캡

`spawn-period-balance.md` 기준:

```csharp
// SetBasePeriod
_spawnPeriod = Mathf.Max(0.05f, period);

// ScalePeriod
_spawnPeriod = Mathf.Max(0.05f, _spawnPeriod * scale);
```

- 절대 플로어: **0.05s** (하드웨어 프레임 보호용)
- **게임플레이 소프트 캡(MinSpawnPeriodScale): 미설계**

0.664s는 0.05s 플로어보다 훨씬 높아 충돌하지 않지만, 이 값 자체가 "의도된 최솟값인지" 판단하는 손잡이가 BalanceConfig에 없다. 실제 6픽을 모두 Swarm 패시브에 쏟는 극단 픽은 단순 합산이지만, 그게 생기는 게임플레이 결과(2.0s 글로벌 캡 포화)가 밸런서가 의도한 범위인지 확인할 수단이 없다.

---

## §4 기획서·분석 문서와의 갭

### 4-1 hero-skills.md §4 분석의 전제

`docs/design/hero-skills.md` §4 "maxed Swarm 시나리오":

> SpawnerHaste(×0.8 1픽 가정) + Swarm Tier2(×0.85) → Phantom 주기 6.0 × 0.8 × 0.85 = **4.08s**

- **가정**: SpawnerHaste 1픽 · Multiply 미분석
- **오늘 시나리오**: Multiply 3픽 + SpawnerHaste 3픽 → **0.664s** (4.08s 대비 6.1배 빠름)

영웅 스킬 설계 당시 이 복합 극단치가 고려되지 않았다.

### 4-2 BalanceConfig 손잡이 현황

`Assets/_Lair/Data/BalanceConfig.asset` 내 `MonsterStatRow`:

| 필드 | 존재 여부 |
|---|---|
| `SpawnPeriod` (기본 주기) | ✅ 있음 |
| `MinSpawnPeriodScale` (스케일 하한) | ❌ 없음 |
| `MaxSpawnCountPerCycle` (사이클 수량 상한) | ❌ 없음 |

밸런서가 "팬텀 스폰 주기가 X초 이하로 내려가면 캡을 건다"는 설정을 넣을 손잡이가 없다.

### 4-3 QA 데이터 공백

`docs/qa-reports/2026-05-22.md` 기준: `DebugAutoPicker` 훅 미구현 → 시뮬레이션 전체 BLOCKED. 극단 픽 조합의 실제 전투 데이터가 없다.

---

## §5 위험 평가

| 항목 | 평가 |
|---|---|
| 극단 주기(0.664s) 의도 여부 | 불명 — 설계 문서에서 이 조합 미검토 |
| 2.0s 글로벌 캡 포화 게임플레이 영향 | 팬텀이 상시 18 슬롯 점유 → 다른 스포너 밀림 가능 |
| 영웅 스킬 재설계 필요성 | hero-skills.md §4 분석이 4.08s 기준 → 0.664s 환경에서 재검토 필요 |
| BalanceConfig 손잡이 부재 | 밸런서가 사후 조정 불가 — 코드 수정 필요 |
| QA 데이터 공백 | 실제 영향 미측정 |

---

## §6 쉬운 설명 (비개발자 요약)

게임에는 **팬텀**이라는 유령형 몬스터를 자동으로 불러오는 장치(스포너)가 있다. 기본적으로 **6초마다 한 마리씩** 나온다.

카드 시스템에는 이 소환 속도를 빠르게 만드는 카드가 두 장 있다.

- **Multiply**: 팬텀 스포너만 빠르게 만든다. 한 번 고를 때마다 속도가 ×0.6배 (더 빠르게). 세 번 고르면 ×0.6×0.6×0.6 = 기존 속도의 21.6% 만 남는다.
- **SpawnerHaste**: 모든 스포너를 빠르게 만든다. 한 번 고를 때마다 ×0.8배. 세 번 고르면 51.2%.

이 두 카드를 각각 세 번씩 모두 고르면, 팬텀 스포너에 두 효과가 **동시에** 적용된다:

```
6초 × 21.6% × 51.2% ≈ 0.66초에 한 마리
```

여기에 전장에 몬스터가 많을수록 더 빠르게 싸워주는 **Swarm Tier2 시너지**(×0.85)까지 붙으면 **0.56초**까지 내려간다.

문제는 게임 내 몬스터 최대 수가 18마리인데, 이 속도라면 **약 2초 만에 다 채워버린다**. 그런데 이 조합이 "의도된 플레이"인지, "너무 강한 조합"인지 확인한 기록이 없다. 영웅 스킬 설계 문서를 보면 "SpawnerHaste 한 번만 쓴다고 가정"해서 계산(4.08초)했기 때문에, 두 카드를 모두 최대로 쌓는 상황은 분석 범위 밖이었다.

그래서 이번에 제안하는 것은:

1. **BalanceConfig에 `MinSpawnPeriodScale` 손잡이 추가** — 팬텀 스폰 주기의 최솟값(예: 0.5배·1.0s 등)을 인스펙터에서 설정할 수 있게 한다.
2. **hero-skills.md §4 분석 갱신** — 1픽 가정 대신 Multiply 3픽+SpawnerHaste 3픽 복합 시나리오를 추가 분석한다.
3. **QA 시뮬레이션 우선 실행** — DebugAutoPicker 구현 후 이 극단 픽 조합으로 N판 시뮬레이션해서 실제 영웅 생존 시간을 측정한다.

---

## §7 제안 액션 아이템

| 우선순위 | 항목 | 담당 | 예상 비용 |
|---|---|---|---|
| ⭐⭐⭐ | QA 시뮬레이션 (DebugAutoPicker 구현 선행) | qa-simulator | 중 |
| ⭐⭐ | BalanceConfig `MinSpawnPeriodScale` 필드 추가 + SpawnerHaste/Multiply 효과에 적용 | gameplay-programmer | 소 |
| ⭐⭐ | hero-skills.md §4 "maxed Swarm" 시나리오 갱신 (Multiply 포함) | game-designer | 소 |
| ⭐ | card-3pick-cap.md §2.1 복합 시나리오 표 추가 | game-designer | 소 |

---

*감사 생성일: 2026-06-23 (KST) | 루틴: Daily Content Audit*
