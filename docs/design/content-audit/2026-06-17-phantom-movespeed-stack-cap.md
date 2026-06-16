# Content Audit — 2026-06-17 — Swarm 패시브 PhantomMoveSpeedBoost 3픽 + SwarmTier1 복합 Phantom 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

---

## 0. 입력 스냅샷

- 컨셉서 버전: v0.7 (2026-06-10 마을 허브 승격 반영)
- 참조 spec/plan 수: 29개 spec (`docs/superpowers/specs/`), 다수 plan
- 참조 QA 리포트 수: 1개 (`docs/qa-reports/2026-05-22.md` — BLOCKED 상태, 시뮬 미실행)
- 과거 감사 이력 (git log): 9건 (가장 최근: 2026-06-16, `68db140`)

---

## 1. 현황

| 카테고리 | 컨셉 §11.2 목표 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (`Knight.prefab`) | 없음 |
| 몬스터 | 6종 | 6종 프리팹 완료 + LittleGhost 5 variant (v0.2 스켈레톤 아트) | 컨셉 6종 완료 |
| 패시브 카드 SO | 16 | 16 (전체 28 SO 중 패시브 16) | 완료 |
| 액티브 카드 SO | 12 | 12 (전체 28 SO 중 액티브 12) | 완료 |
| 스포너 배치 | 6개 (ring r=14 유닛) | 씬 Spawner 6개 + BalanceConfig SpawnPeriod 이관 완료 | 완료 |

### 계획 있으나 미구현

- `SwarmRush` 카드 (팬텀 6마리 즉시 소환) — `card-renewal.md §3.4`: `Multiply.asset`("빠른 번식", `FastBreedingEffect`) 잔존 중
- `EternalBleedAura` — Debuff Tier3 시너지 (`card-renewal.md §10.3`): 신규 클래스 구현 여부 불명
- QA 시뮬레이터 `BattleController.DebugAutoPicker` 훅 — `qa-reports/2026-05-22.md §3` 요청사항, 미구현

### QA 권고 미해결

- ④ 5분 타임오버 ≥1판 발생: QA 7차 미실행으로 미검증 (`card-renewal.md §9.7`)
- ⑤ 클리어율 ≤80%: 동일 사유로 미검증
- QA 시뮬레이터 자체가 `DebugAutoPicker` 훅 대기 중 (시뮬 캠페인 불가)

### 과거 감사 후보 (git log 조회 결과 — 9건)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-16 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 |
| 2026-06-15 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — BalanceConfig 손잡이 이관 제안 |
| 2026-06-14 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 |
| 2026-06-13 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-12 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-11 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-10 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-09 | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-08 | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### [Swarm 패시브 PhantomMoveSpeedBoost 3픽 + SwarmTier1 복합 — Phantom 이동속도 10.53 m/s 상한 미설계 및 BalanceConfig MaxMoveSpeedScale 손잡이 부재]

- **카테고리**: Swarm 패시브 + 시너지 Tier1 (이동속도 축)
- **요지**: `PhantomMoveSpeedBoost` 3픽(곱연산 `_speedMul 1.5³ = ×3.375`) + SwarmTier1 발화(Phantom·Wisp 이속 ×1.3)가 동일 `RegisterMonsterTypeBuff` 표면에 누적되어 Phantom 이동속도가 기본 2.4 m/s → **10.53 m/s**로 치솟는다. 스포너 링 반경 14 유닛을 **1.33초**에 횡단해 사실상 즉시 도달 패턴이 형성되나, BalanceConfig에 이속 상한 손잡이가 없고 `RegisterMonsterTypeBuff` 구현에 상한 클램프가 문서화되어 있지 않다.
- **검증/구현/시너지/데이터**: 4/2/4/4 → 종합 **16**
- **근거**: `continuous-spawn-round.md §4` (Phantom 기본 이속 2.4), `card-renewal.md §3.4 #1` (`_speedMul=1.5`), `card-renewal.md §4.2` (SwarmTier1 이속 ×1.3), `card-renewal.md §10.3` (동일 `RegisterMonsterTypeBuff` 표면), `spawn-period-balance.md §3.2` (`ScalePeriod` 최소 클램프 0.05s — 이속에는 동등 상한 없음)
- **MVP 범위**: 컨셉 §11.2 ✅ (Swarm 빌드 축 — 패시브 카드·시너지 모두 MVP 내 기구현)

#### 유저 플로우

1. **노출 시점·트리거**: 영웅 HP 10% 감소 시 패시브 선택창이 뜬다. Swarm 축 카드(`PhantomMoveSpeedBoost` 포함)가 3장의 선택지 중 하나로 등장할 수 있다. 같은 카드를 3번까지 픽할 수 있으므로 라운드 중 HP 90%→80%→70% 시점에 연속 픽이 가능하다.

2. **화면 변화**: 첫 픽 시점부터 필드의 Phantom(검정 작은 구체, 스케일 0.3)들이 눈에 띄게 빠르게 움직이기 시작한다. 2픽 → 5.4 m/s, 3픽 → 8.1 m/s로 단계적으로 가속된다. SwarmTier1(Swarm 3장 임계) 발화 순간에는 화면 중앙 상단에 "Swarm 시너지 Tier 1 발동!" 토스트가 1.5s 표시되며 Phantom과 Wisp 모두 이속이 추가 ×1.3 배 증가한다.

3. **입력 행동**: 플레이어는 패시브 선택창에서 `PhantomMoveSpeedBoost`("환령의 발걸음") 카드를 클릭(CHButton)한다. 선택창 상단 Swarm 빌드 카운트 셀이 픽 즉시 갱신되어 N/3 → (N+1)/3 으로 표시된다. SwarmTier1 임계(3장) 도달 시 셀이 0.3s 펄스 애니메이션.

4. **시스템 반응**: `PhantomMoveSpeedBoostEffect.Apply` → `IBattleContext.RegisterMonsterTypeBuff(EMonster.Phantom, EMonsterStatKind.MoveSpeed, 1.5f)` 호출. 내부에서 Phantom 종의 글로벌 MoveSpeed 배율 dict에 ×1.5 곱연산 누적. 현재 필드의 모든 Phantom에 즉시 소급 적용되고, 이후 스폰되는 Phantom도 해당 배율을 상속한다. 3픽 누적: ×3.375 (dict 값). SwarmTier1 발화 시 동일 표면으로 ×1.3 추가 누적: 최종 ×4.388 (= 2.4 × 4.388 = **10.53 m/s**).

5. **반복·재발생 패턴**: Phantom 스포너(주기 6.0s 기본)는 라운드 내내 6초마다 Phantom을 공급한다. 이속 버프는 영구라 한 번 적용 후 라운드 끝까지 유지. Slow 액티브 카드(Swarm 축) 발동 시 `EMonsterBuff.SwarmSpeed`(×1.3, 10초)가 추가로 곱산되어 일시적 최고 **13.69 m/s**가 된다. Slow 카드 3픽 시 SwarmSpeed 버프 지속시간 30초까지 연장 가능.

6. **종료·해소 조건**: 이속 버프(`RegisterMonsterTypeBuff`)는 라운드 종료(영웅 사망 또는 5분 타임오버)까지 해제되지 않는 영구 효과다. SwarmSpeed(`MonsterBuffService.AddBuff`) 는 10s 시한으로 만료 후 해제. 다음 런 시작 시 모든 버프가 초기화된다.

7. **다른 시스템과 상호작용**: `SpawnPhantoms` 카드(가산 누적, 최대 출력 +3 = 총 4마리/사이클)와 결합 시 빠른 Phantom 4마리가 동시에 접근해 영웅 주변 포위 밀도가 급상승한다. Tank Tier3(캡 +6 = 24마리) 발동 시 Phantom이 캡의 더 큰 비중을 차지할 수 있다. 영웅 AutoCombat AI(가장 가까운 몬스터 타겟)는 고속 Phantom이 링 외곽에서 이미 근접해 있으면 Phantom을 우선 타겟으로 삼아 의도치 않게 HP 30짜리 몬스터 처치에 DPS를 낭비할 수 있다.

8. **엣지 케이스**: 링 반경 14 유닛에서 10.53 m/s 이속이면 횡단 시간 1.33초. 영웅의 공격 쿨다운 1.0초보다 짧아 스폰 직후 Phantom이 영웅 공격권 진입 전에 이미 근접 완료된다. `RegisterMonsterTypeBuff` 구현에 이속 상한 클램프 로직이 없을 경우 추후 카드 추가나 시너지 확장 시 이론적 상한 없이 누적 가능. 스폰 주기에는 `ScalePeriod` 내 0.05s 클램프가 존재하나(`spawn-period-balance.md §3.2`), 이동속도에는 동등한 문서화된 상한이 없다. 극단 케이스(향후 PhantomMoveSpeedBoost `_speedMul` 상향 조정 시): 3픽 × 2.0 = ×8.0 × SwarmTier1 ×1.3 = 2.4 × 10.4 = 24.96 m/s — 링을 0.56초 안에 횡단.

9. **유저 정보·피드백**: BattleHud 좌상단 시너지 패널에 Swarm 축 카운트(N/3→N/5→N+)와 활성 티어(아이콘 수)가 상시 표시된다. 그러나 Phantom 이동속도의 실제 수치(m/s)는 어디에도 노출되지 않으므로, 플레이어는 3픽 후 이속이 얼마나 극단적으로 상승했는지 인식하기 어렵다. 빠른 Phantom의 시각적 움직임만이 유일한 피드백이며, 이것이 의도된 강화인지 밸런스 이상인지 플레이어가 판단할 단서가 없다.

### 보류

- **Slow 카드 SwarmSpeed × PhantomMoveSpeedBoost 일시 피크 13.69 m/s**: 10초 한정 버프라 영구 효과와 구분 필요. 오늘 제안의 **영구 10.53 m/s**가 더 우선 검증 대상.
- **Swarm Tier3 (IncrementAllSpawnerOutputs) 캡 포화 시간**: June 14 BalanceConfig 커버리지와 연계 필요. 별도 사이클로.
- **Multiply × SpawnerHaste × SwarmTier2 Phantom 주기 0.56s**: June 14 (Swarm Tier2 하드코딩 이관)과 카테고리 근접. 본 제안이 채택된 뒤 후속으로 검토.

---

## 3. 과거 감사 대비 차별성

- git log 조회 9건 검토 완료.
- 가장 유사했던 과거 커밋: **c07cc2c (2026-06-14)** "Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관" — 차별점: Tier2는 *스폰 주기(period)* 이관 이슈, 오늘 제안은 *이동속도(MoveSpeed)* 상한 미설계 이슈. 동일 Swarm 축이나 다른 메커니즘(주기 vs 이속), 다른 카드(`SpawnerHaste` vs `PhantomMoveSpeedBoost`), 다른 시너지(SwarmTier2 vs SwarmTier1).
- 직전 7일: Swarm 카테고리는 **June 12 (Swarm 액티브 TimeStop·Fear)**와 **June 14 (Swarm Tier2 주기)** 2건. 오늘은 Swarm 패시브 × Swarm Tier1 이속 조합으로 이전 2건과 메커니즘·카드·시너지 레이어 모두 다름.
- **이속 상한 미설계** 이슈는 9건 어디에도 등장하지 않음. 과거 하한(floor) 미설계 이슈(HeroAttackDown ×0.358 하한 → June 13, DamageTakenScale 하한 → June 11)의 반대 방향(상한·ceiling)으로 신규 카테고리.

---

## 4. 제외 (범위 밖)

- **SwarmRush 신규 카드 구현**: Multiply → SwarmRush 교체는 별도 기획 사이클 필요. 오늘 제안 범위 아님.
- **신규 Phantom 아트/애니메이션**: v0.2 범위 아님 (CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지").
- **서버 연동 밸런스 데이터 공유**: v0.3+ 범위.
- **마을 메타 진행과 이속 연계**: 상점 영구 업그레이드(v0.2 신규)가 이속 배율에 영향을 줄 경우를 아직 분석하지 않음 — 해당 기획서(`village-meta-hub.md`) 내용과 별도 사이클 필요.

---

## 5. 다음 단계 제안

- 채택 시 **game-designer** 에게 정식 기획 요청:
  - `BalanceConfig.MaxMonsterMoveSpeedScale` 손잡이 추가 (예: 기본 5.0 — Phantom 기본 2.4 × 5.0 = 12 m/s 상한)
  - 또는 `RegisterMonsterTypeBuff` 구현부에 MoveSpeed 종별 상한 테이블 추가
  - `spawn-period-balance.md §3.2` 의 `Mathf.Max(0.05f, period)` 패턴 참조해 `Mathf.Min(maxScale, accumulated)` 클램프 구조 제안
- QA 시뮬레이터 훅(`DebugAutoPicker`) 구현 후 Swarm 패시브 집중 전략으로 실측 검증 권장

---

## 6. 쉬운 설명 (비개발자 요약)

게임 속 팬텀이라는 작은 검은 유령 몬스터는 원래 꽤 빠르긴 해도 1초에 약 2.4미터 정도 움직인다. 그런데 "환령의 발걸음" 이라는 카드를 같은 종류로 세 번 고르면 속도가 8배 이상 올라가 1초에 8미터 넘게 달리게 된다. 거기다 같은 계열 카드를 3장 모으면 자동으로 발동하는 스웜 시너지가 속도를 또 30%나 올려주는데, 이렇게 되면 유령이 던전 끝에서 영웅 앞까지 달려오는 데 겨우 1~2초밖에 안 걸린다. 현재 게임에는 이 속도를 일정 수준 이상으로 올라가지 않도록 막아주는 장치(속도 상한)가 없는 상태다. 그래서 이번에 제안하는 것은: "팬텀 이동속도가 최대 몇 배까지 오를 수 있는지 한도를 정하고, 그 수치를 쉽게 조정할 수 있도록 설정 파일에 넣어두는 것."
