# Content Audit — 2026-06-15 — BloodThirst 처치 회복량 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10)
- 참조 spec/plan 수: 28개 (specs), 29개 (plans)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED — 시뮬레이션 미실행)
- 과거 감사 이력 (git log `[Routines][Daily Content Audit]` 패턴): 7건 (가장 최근: 2026-06-13 UTC)
- 비고: `docs/design/content-audit/` 폴더에 2026-05-28 이후 파일 다수 존재하나 두 grep 패턴 모두 미탐색 — 규칙상 git log 미발견 항목은 dedup 대상 외.

## 1. 현황

| 카테고리 | 컨셉 | 실제 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | ✓ |
| 몬스터 | 6 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom.prefab) | ✓ |
| 패시브 카드 SO | 16 | 16 (Art/Cards/Items/ — 4축×4장) | ✓ |
| 액티브 카드 SO | 12 | 12 (Art/Cards/Items/ — 4축×3장) | ✓ |
| 시너지 Tier 클래스 | 12 (4축×3Tier) | 12 (Scripts/Card/Synergy/*.cs) | ✓ |
| 카드 Effect 클래스 | 28 | 28 (Scripts/Card/Effects/*.cs) | ✓ |

### 계획 있으나 미구현
- **SwarmRush**: `card-renewal.md §3.4` 에서 Multiply 자리에 SwarmRush(Phantom 6마리 즉시 소환)를 신설하기로 했으나 미실현. `Multiply.asset` (FastBreedingEffect, 팬텀 스포너 주기 ×0.6 영구) 잔존. ECardId.Multiply 및 SO 유지.
- **DebugAutoPicker 훅**: QA 리포트(2026-05-22) §3에서 요청한 `BattleController.DebugAutoPicker` 델리게이트 미구현 — 시뮬레이션 자동 픽 불가 상태 지속.

### QA 권고 미해결
- QA 리포트 2026-05-22 전체가 BLOCKED. DebugAutoPicker 훅 구현 전까지 N판 헤드리스 캠페인 불가.
- 밸런스 실측 데이터(평균 사망 시각, 클리어율, 픽률 분포) 전무 → 이론 계산만 근거.

### 과거 감사 후보 (git log 조회 결과)
| 날짜 (UTC) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-13 | c07cc2c | Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 (메타 상점 복합 위험 시나리오 연계) |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-07 | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |

## 2. 추가 컨텐츠 후보 (권장 1개)

### BloodThirst 처치 회복량 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안

- **카테고리**: Dps 액티브 카드 / BalanceConfig 손잡이 추가
- **요지**: `BloodThirstService.cs:10` 의 `private const int HealAmount = 30` 이 C# 컴파일 상수로 고정되어 있어 BalanceConfig SoT 밖에서 관리됨. Phantom(최대 HP 30)은 한 번의 처치로 100% 완전 회복, Wraith(최대 HP 500)는 6%만 회복 — 종별 HP 규모 차이를 무시한 평면 고정값이 의도치 않은 저HP 몬스터 회복 편향을 만든다.
- **검증/구현/시너지/데이터**: 4/2/4/4 → 종합 16
- **근거**: `Assets/_Lair/Scripts/Battle/BloodThirstService.cs:10` (`private const int HealAmount = 30`), `card-renewal.md §3.2` (BloodThirst 효과 기술), `continuous-spawn-round.md §4` (종별 HP 수치)
- **MVP 범위**: 컨셉 §11.2 액티브 카드 12장 항목, BalanceConfig SoT (`docs/superpowers/specs/2026-05-21-slice-c-balance-tooling-design.md`)

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**: Dps 축 액티브 카드 `BloodThirst` ("피의 갈증")를 픽하면 30초간 `BloodThirstService` 가 활성화된다. 해당 30초 동안 어떤 몬스터든 죽을 때마다 반경 내 모든 살아있는 몬스터에게 회복이 즉시 발동한다. 이전에 픽한 BloodThirst 가 아직 남아 있으면 잔여시간이 연장된다 (지속시간 누적 정책).

2. **화면 변화**: 카드 픽 즉시 BattleHud 의 액티브 효과 표시가 갱신된다. 전투 중 몬스터가 처치될 때마다 반경 내 몬스터에 시각 피드백(현재 프리미티브 범위라 별도 이펙트 없을 수 있음)이 발생하고, 대상 몬스터 HP 바가 +30 증가로 짧게 늘어나는 것을 볼 수 있다.

3. **입력 행동**: 사용자가 카드 선택 팝업에서 BloodThirst 카드를 클릭하면 종료. 이후 추가 입력 없이 자동 발동. 같은 카드를 재픽하면 잔여시간이 30초 연장된다.

4. **시스템 반응**: `BloodThirstService` 가 활성화된 동안 `IBattleEntity` 사망 이벤트를 수신하고, 사망 위치 반경 내 살아있는 몬스터 전체에 `entity.Health.Heal(30)` 을 즉시 호출한다. 회복량 30은 모든 종에 동일하게 적용된다.

5. **반복·재발생 패턴**: 30초 내 몬스터 처치가 발생할 때마다 반복 발동. Dps 빌드(Reaper·Hex 공속 강화)에서 처치 빈도가 올라갈수록 회복 횟수가 증가한다. Phantom(HP 30)은 Reaper의 한 타 40 DPS(기본) 에도 피격 시 처치될 수 있어, 처치 → 완전 회복 → 재이동의 사이클이 빠르게 반복될 수 있다.

6. **종료·해소 조건**: 픽 시점 기준 30초(또는 재픽 시 연장된 잔여시간) 후 자동 비활성. 전투 종료 시 초기화. 3픽 캡(전역 2026-06-01) 으로 최대 누적 지속시간은 90초.

7. **다른 시스템과 상호작용**:
   - **Dps Tier1 (Reaper·Hex Power ×1.3)** 과 조합 시 처치 속도 증가 → BloodThirst 발동 횟수 증가.
   - **Dps Tier2 (Reaper·Hex Cooldown ×0.8, 공속 +25%)** 와 조합 시 처치가 더 빠르게 발생 → Phantom 완전 회복 주기 더 빈번.
   - **Swarm 축** Phantom(HP 30)은 BloodThirst 최대 수혜자 — Dps 빌드인데 Swarm 몬스터(Phantom)가 비례적으로 훨씬 강해지는 **크로스 축 시너지**.
   - **글로벌 캡(18마리)** 과의 상호작용: BloodThirst로 Phantom이 죽지 않고 버티면 캡 소비 없이 위협을 유지 → Swarm Tier3 이전에도 사실상 스웜 밀도 유지 효과.

8. **엣지 케이스**:
   - **Phantom(HP 30) vs Wraith(HP 500) 회복량 비교**: Phantom은 처치 1회로 100% 완전 회복 → 사실상 불사에 가까운 상태. Wraith는 같은 처치 1회로 6% (30/500) 회복 → 미미. 30 HP라는 평면값이 종별로 1~100% 사이의 극단적 편차를 만든다.
   - **3픽 BloodThirst(90s) + Dps Tier2 빌드**: Reaper 공속 ×0.8 + 처치당 30 회복 × 90s 활성. Phantom이 필드에 남아있으면 사실상 지속 회복.
   - **Heal(30) 이 최대 HP를 초과하는 경우**: HP 30인 Phantom이 HP 1인 상태에서 회복 시 29만 적용. 별도 클램프가 있다면 안전하나, Health.Heal 구현에 따라 초과분 처리가 다를 수 있음.
   - **BloodThirst 활성 중 처치가 0회인 경우 (전투 초반)**: 회복 없음 — 정상 동작.

9. **유저 정보·피드백**:
   - 카드 SO description 에는 "_duration 초간 몬스터 처치 시 주변 몬스터 회복 (30초)" 만 표시, **회복량 30이 UI 에 노출되지 않음** — 유저가 수치를 알 수 없는 블랙박스.
   - BattleHud 시너지 패널에 회복 효과 별도 표시 없음.
   - 현재 HealAmount = 30 이 BalanceConfig 밖에 있어 인게임 에디터(`LairBalanceWindow`)로 확인·수정 불가 — 코드 변경 + 재컴파일 필요.

### 보류

- **DebuffSynergyTier3 Ratio(0.01f) BalanceConfig 이관**: `const float Ratio = 0.01f` 하드코딩 확인. 검증가치 3, 종합 15. Debuff 카테고리 7일 내 2회(6/08·6/12) → BloodThirst가 더 높은 점수.
- **SwarmRush 미구현 해소**: Swarm 카테고리 7일 내 2회(6/11·6/13), 차별성 있지만 구현비용 3으로 점수 낮음.
- **BloodThirst 평면 → 비율 기반 회복 리뉴얼**: 더 근본적인 재설계(HealAmount 평면 → HealRatio = MaxHp × n%)이나 기획서 갱신·Effect 클래스 변경이 필요해 이번 감사 범위 초과. BalanceConfig 손잡이 이관 이후 별도 밸런스 조정 흐름에서 진행 권장.

## 3. 과거 감사 대비 차별성

git log 조회 7건 검토 완료.

가장 유사했던 과거 커밋:
- **307ec17 (2026-06-07)**: `Dps 축 ReaperAtkSpeed 배율 재조정` — **차이**: 해당 커밋은 Dps 패시브 카드(`ReaperAtkSpeed`, Cooldown ×0.7) 배율 수치 조정 제안. 이번 제안은 Dps 액티브 카드(`BloodThirst`) 의 처치 회복 서비스 (`BloodThirstService.HealAmount`)가 C# 컴파일 상수로 고정된 구조적 문제 + 종별 HP 불균형 분석. 파일·메커니즘·근거가 모두 다름.
- **440794c (2026-06-09)**: `Dps HexRangeBoost 3픽+Tier3 중첩 배율` — **차이**: 패시브 카드 사거리 배율 + Tier 복합. 이번은 액티브 카드 회복 서비스 상수 + 종 HP 비례 불균형 — 완전히 다른 메커니즘.

Dps 카테고리 7일 내 2회(6/07 패시브·6/09 패시브) 접근. 이번 제안은 **액티브 카드, 처치 회복 서비스 상수, 크로스 축(Dps×Swarm) 시너지 불균형** — 과거 두 커밋(패시브 배율 조정)과 파일·카드 유형·문제 차원이 모두 다름. 차별성 충분.

## 4. 제외 (범위 밖)

- 신규 영웅/몬스터/카드 리소스 생성 (CLAUDE.md §8 금지)
- 서버 연동 (§8 금지)
- BloodThirst 평면 → 비율 기반 리뉴얼 (기획서 대규모 갱신 필요, 별도 사이클)
- 사운드/이펙트 연동 BloodThirst 비주얼 개선 (v0.2 에셋 허용이나 이번 범위 초과)

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청
- gameplay-programmer 에 다음 구현 의뢰:
  1. `Assets/_Lair/Data/BalanceConfig.cs` 에 `public int BloodThirstHealAmount = 30;` 추가
  2. `BloodThirstService.cs:10` 의 `private const int HealAmount = 30` → `BalanceConfig` 주입으로 교체
  3. `Assets/_Lair/Data/BalanceConfig.asset` 에 기본값 30 입력
  4. (선택) JsonSync DTO 갱신 (`balance_config.json` 자동 재생성)
- 구현 후 DebugAutoPicker 훅이 생기면 qa-simulator 로 Phantom 완전 회복 실측 검증 권장

## 6. 쉬운 설명 (비개발자 요약)

"피의 갈증" 카드를 사용하면 30초 동안 몬스터가 죽을 때마다 주변 아군 몬스터들이 HP를 조금씩 회복합니다. 지금은 회복량이 "무조건 30"으로 고정되어 있는데, HP가 30인 팬텀(가장 약한 몬스터)은 딱 1번만 처치가 일어나도 100% 완전 회복이 되고, HP가 500인 레이스는 같은 처치에도 고작 6%만 회복됩니다 — 같은 카드인데 몬스터마다 효과가 1배 vs 16배 차이가 나는 셈입니다. 게다가 이 숫자 "30"은 코드 안에 꽁꽁 숨겨져 있어서 게임 데이터 편집창에서 볼 수도, 바꿀 수도 없습니다. 그래서 이번에 제안하는 것은: "30"을 게임 밸런스 설정판으로 꺼내서 쉽게 조정할 수 있게 하고, 나아가 몬스터 HP에 비례한 회복 방식을 검토하자는 것입니다.
