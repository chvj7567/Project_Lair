# Content Audit — 2026-06-30 — Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 복합 팬텀 출력 5대, MaxSpawnerSimultaneousOutput 손잡이 미설계

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7
- 참조 spec/plan 수: 30개 (specs/) + 30개 (plans/)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22)
- 과거 감사 이력 (git log): 18건 (가장 최근: 2026-06-28)

## 1. 현황

| 카테고리 | 컨셉 §11 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 없음 |
| 몬스터 | 6종 | 6종 (Wisp·Wraith·Reaper·Hex·Plague·Phantom 프리팹) | 없음 |
| 패시브 카드 | 16장 | 16장 (28장 중 P 타입, 4축×4장) | 없음 |
| 액티브 카드 | 12장 | 12장 (28장 중 A 타입, 4축×3장) | 없음 |
| 카드 효과 구현 | 28장 | 27장 (SwarmRush 미구현, FastBreedingEffect 잔존) | **1 미구현** — Multiply(FastBreedingEffect, 팬텀 스포너 주기 ×0.6) 가 SwarmRush(팬텀 6마리 즉시 소환) 자리를 임시 점유 중 |

### 계획 있으나 미구현

- **SwarmRush (Phantom 6마리 즉시 소환)**: card-renewal.md §3.4 원안에서 Multiply 교체 예정. 현행 FastBreedingEffect(팬텀 스포너 주기 ×0.6 영구) 잔존. 별도 구현 사이클 대기.

### QA 권고 미해결 (2026-05-22 리포트)

- `BattleController.DebugAutoPicker` 델리게이트 훅 미추가 — gameplay-programmer 대기
- 시뮬레이션 실행 방식(사용자 대화형 에디터 vs `[UnityTest]` 래핑) 미결정
- 전체 N판 시뮬 캠페인 미실행 → 밸런스 메트릭 전부 이론 계산 기반

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | SHA | subject 설명 |
|---|---|---|
| 2026-06-28 | 6d21dc5 | Debuff 패시브 SpawnPlagues 3픽 다중 Plague 동시 슬로우 중첩 방식 미설계 — MinHeroMoveSpeedScale 이중 보호 누락 |
| 2026-06-26 | 614c299 | Tank 패시브 WispHpBoost·WraithDamageBoost 3픽+Tank Tier1 복합 HP ×4.39 — MaxMonsterHpScale 손잡이 미설계 |
| 2026-06-25 | 63ecbd3 | Swarm 액티브 Slow 이중 효과 배율 하드코딩 — SlowMonsterAccelMul·SlowHeroSlowFactor BalanceConfig 손잡이 미설계 |
| 2026-06-24 | 128bdb8 | 시너지 임계값 3/5/7 하드코딩 — 액티브 트리거 9→5 감소 후 Tier3 도달 난이도 50% 픽집중 · SynergyTierThreshold 손잡이 미설계 |
| 2026-06-23 | b83b566 | Dps 패시브 ReaperAtkSpeed 3픽+Tier2 복합 Reaper 쿨다운 0.137s — MinReaperCooldownScale 손잡이 미설계 |
| 2026-06-22 | 9118936 | Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계 |
| 2026-06-20 | a1e0ba4 | Debuff 패시브 PlagueSlowBoost 3픽+Tier1 복합 영웅 이속 27% — MinHeroMoveSpeedScale 손잡이 미설계 |
| 2026-06-19 | 0fb40b1 | Tank 패시브 ReplaceWispsToWraith 3픽 Power ×2.197 + Tank Tier2 ×1.2 — BalanceConfig MaxTankPowerScale 손잡이 미설계 |
| 2026-06-18 | dcaa8b7 | Dps 액티브 Frenzy 공속 배율 하드코딩 + MarkOfDeath 복합 압박 BalanceConfig 손잡이 미설계 |
| 2026-06-17 | 3a9bed3 | Swarm 패시브 SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안 |
| 2026-06-16 | d8fdcfe | Swarm 패시브 PhantomMoveSpeedBoost 3픽+SwarmTier1 복합 이속 10.53 m/s — BalanceConfig MaxMoveSpeedScale 손잡이 미설계 |
| 2026-06-15 | 68db140 | Debuff 패시브 HeroPoisonAura 독장판 DPS 5 실효 기여도 재조정 제안 (영웅 HP 4000 기준 3픽 합산 1.875% — 스킬 도입 후 격차 확대) |
| 2026-06-14 | 6e02b2a | Dps 액티브 BloodThirst 처치 회복량(HealAmount=30) 하드코딩 — 종별 HP 불균형 + BalanceConfig 손잡이 이관 제안 |
| 2026-06-13 | c07cc2c | BalanceConfig 손잡이 추가 제안 — Swarm Tier2 스포너 주기 배율 ×0.85 하드코딩 이관 (메타 상점 복합 위험 시나리오 연계) |
| 2026-06-12 | 8de2ecb | Debuff 패시브 HeroAttackDown 3픽+Tier2 복합 하한 미설계 — BalanceConfig MinHeroAttackScale 손잡이 추가 제안 |
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |

## 2. 추가 컨텐츠 후보 (권장 1개)

### Swarm 패시브 SpawnPhantoms 3픽 + Swarm Tier3 전체 스포너 출력 +1 복합 — 팬텀 스포너 동시 출력 5대, 글로벌 캡 포화 주기 가속 및 MaxSpawnerSimultaneousOutput BalanceConfig 손잡이 미설계

- **카테고리**: BalanceConfig 손잡이 추가 / Swarm 시너지
- **요지**: 순수 Swarm 빌드(7픽 Tier3 달성) 안에서 SpawnPhantoms 를 3픽하면 팬텀 스포너 동시 출력이 1(base)+3(카드 가산)+1(Tier3)=5대가 된다. GlobalCap(18)이 첫 3사이클(18s) 만에 포화되고, SpawnerHaste·Multiply 맞물림 시 주기는 1.84s 로 단축돼 캡 재포화가 사실상 즉각 일어난다. 이를 조정할 `MaxSpawnerSimultaneousOutput` 손잡이가 BalanceConfig 에 없다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 4 / 2 / 4 / 3 → 종합 **15**
- **근거**:
  - card-renewal.md §3.4 — SpawnPhantoms 가산 누적 정책(`SpawnPhantomsEffect`, `+1` 3픽 캡)
  - card-renewal.md §4.2 Tier 표 — Swarm Tier3 "모든 스포너 동시 출력 +1 (영구)"
  - spawn-period-balance.md §2 — BalanceConfig MonsterStatRow 현행 구조(SpawnPeriod 필드만 있고 MaxSimultaneousOutput 없음)
  - 컨셉 §4.1 — 글로벌 캡 18, 백오프 모델
- **MVP 범위**: 컨셉 §11.2 패시브 카드 16장(SpawnPhantoms 포함) + §5.2 Swarm Tier3 시너지 + §4.1 글로벌 캡 모델

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   패시브 선택지 팝업(HP 10%마다, 최대 9회)에서 플레이어가 SpawnPhantoms 를 3회 선택해 3-pick 캡에 도달한다. 이 과정에서 Swarm 축 카드 누적이 7장에 달하면 Swarm Tier3 시너지가 즉시 1회 발화해 "모든 스포너 동시 출력 +1" 이 적용된다. SpawnPhantoms 3픽 + Swarm 다른 4장(PhantomMoveSpeedBoost, SpawnWisps, SpawnerHaste, TimeStop 등)이 조합되는 순간이 최단 달성 경로다.

2. **화면 변화**
   Tier3 발화 순간 시너지 패널의 SWARM 아이콘이 3개로 증가한다. 이후 전투 화면에서 팬텀이 6초마다 1마리가 아닌 5마리씩 동시에 필드 가장자리에서 내부로 수렴한다. 흑색 소형 구체(Phantom)가 무리 지어 나타나는 시각 밀도가 급격히 높아지고, 글로벌 캡(18) 도달 후 팬텀이 처치되면 곧장 5마리가 다시 채워지는 패턴이 반복된다.

3. **입력 행동**
   플레이어는 카드 선택 팝업에서 SpawnPhantoms 를 총 3회 클릭하는 것 외에 직접 조작이 없다. 전투는 자동 진행이며, 이후 팬텀 증가는 별도 조작 없이 스포너가 자동 처리한다.

4. **시스템 반응**
   `SpawnPhantomsEffect.Apply()` 가 팬텀 스포너의 동시 출력 카운터에 +1 을 3회 가산(최종 +3). Swarm Tier3 발화 시 `ScaleAllSpawnersSimultaneousOutput(+1)` 에 해당하는 로직이 모든 스포너에 +1 을 추가 적용해 팬텀 스포너 총 출력 = 5. 이후 6s 주기마다 Spawner 가 팬텀 5마리를 동시 스폰하여 GlobalCap 방향으로 빠르게 충원한다. 캡 도달 시 Spawner 백오프가 발동하나, 팬텀 HP(30)가 낮아 즉사율이 높으므로 슬롯이 자주 비고 재충전도 자주 일어난다.

5. **반복·재발생 패턴**
   팬텀 HP=30, 영웅 공격력 50+ → 팬텀 1타 즉사. 죽은 자리를 5마리 출력 스포너가 6s 이내에 재충전한다. SpawnerHaste 3픽(주기 ×0.8^3=×0.512)이 맞물리면 스폰 주기 = 6×0.512≈3.07s, Multiply(팬텀 주기 ×0.6 추가)까지 겹치면 6×0.512×0.6≈1.84s 로 단축된다. 이때 팬텀 5마리가 1.84s 마다 생성되어 필드 캡 복구가 사실상 연속 루프가 된다.

6. **종료·해소 조건**
   영웅 HP 0(처치 성공) 또는 5분 타임오버로 라운드 종료 시만 해소된다. SpawnPhantoms 가산 누적과 Swarm Tier3 보너스는 모두 영구 적용이므로 라운드 중 되돌릴 수단이 없다. 3-pick 캡 도달 이후에는 선택지 풀에서 SpawnPhantoms 가 제외되어 추가 픽도 불가하다.

7. **다른 시스템과 상호작용**
   - **GlobalCap(컨셉 §4.1)**: 팬텀 5대 충원 속도가 다른 종 스포너 충원을 밀어내어 글로벌 캡 내 팬텀 점유 비율이 높아진다. Wisp·Reaper·Plague 스포너도 백오프 경쟁.
   - **PhantomMoveSpeedBoost(2026-06-16 감사)**: 이속 10.53 m/s(3픽+Tier1) + 출력 5대 복합 시 영웅 포위 속도 최대화. 두 이슈가 같은 Swarm 7픽 경로 위에서 동시 발화 가능.
   - **Swarm Tier1(Phantom·Wisp MoveSpeed ×1.3)**: 동일 Tier3 달성 경로에서 Tier1도 이미 발화 → 이속 보너스까지 동반.
   - **Multiply/FastBreedingEffect(팬텀 스포너 주기 ×0.6)**: 출력 수 증폭 + 주기 단축 이중 강화. §5 에서 계산한 1.84s 재충전이 이 카드 병행 픽 시 현실화.
   - **BalanceConfig.SpawnPeriod(spawn-period-balance.md §2)**: Phantom 기본 6s 위에서 Multiply·SpawnerHaste 곱연산이 작동 — MaxSimultaneousOutput 손잡이가 없으면 주기 하한(MinSpawnPeriodScale)과 출력 상한이 서로 독립으로 제어 불가.

8. **엣지 케이스**
   - Swarm Tier3 발화 직전(6장째) vs 직후(7장째 픽) 사이에 영웅이 사망하면 Tier3 미발화 상태로 종료 → 같은 전략이어도 달성 타이밍에 따라 팬텀 출력 4 vs 5 로 분기, 밸런스 분산 요인.
   - SpawnWisps 3픽 + Swarm Tier3 조합 시 위스프 스포너도 출력 5대가 됨. Wisp HP=200 으로 팬텀보다 필드 잔존 시간이 길어 캡 압박이 더 강할 수 있음 — 같은 MaxSimultaneousOutput 손잡이로 동시 규제 가능.
   - Tank Tier3(캡 +6→24) + Swarm Tier3(출력 +1) 동시 달성: 패시브 한도 9픽 내에서 두 Tier3 각 7픽 = 14픽 필요 → 불가. 실전에서 두 Tier3 동시 달성은 패시브 9+액티브 9=18픽으로 이론상 불가능하여 이 엣지는 비발생.
   - SpawnPhantoms 3픽 이후 Swarm Tier3 미달(6장 이하)인 경우: 출력 = 4대. 이 경우도 기본 1대 대비 4배 출력이므로 손잡이 미설계 문제는 동일하게 적용.

9. **유저 정보·피드백**
   시너지 패널(card-renewal.md §8) 에서 Swarm 픽 카운트를 실시간 확인 가능 — Tier3(7장) 달성 여부 인지 가능. 3-pick 캡 도달 후 SpawnPhantoms 가 선택지에서 사라지는 피드백은 card-3pick-cap.md 구현에 포함. 그러나 "팬텀 스포너 현재 동시 출력 5대" 수치는 게임 내 어디에도 표시되지 않으며, 스포너 상태 UI(spawner-status-ui.md) 에도 출력 수 표시가 현재 포함되어 있지 않다. 유저는 "팬텀이 갑자기 많이 나온다"는 시각적 감각으로만 피드백 수신.

### 보류

- **SpawnReapers 3픽 + Swarm Tier3**: Reaper 스포너 동시 출력 5대. Reaper HP=100·DPS=40 으로 팬텀보다 강력한 조합. 동일 손잡이로 규제 가능하나 이번 회차는 팬텀 중심 1건 집중.
- **Weaken(A) + HeroAttackDown 3픽 + Debuff Tier2 복합 영웅 공격력 최저치**: 미감사 항목. 다음 회차 후보.

## 3. 과거 감사 대비 차별성

git log 조회 18건 검토 완료.

가장 유사한 과거 커밋:
- 2026-06-22 (9118936): "Swarm 패시브 Multiply·SpawnerHaste 3픽 복합 팬텀 주기 0.664s — MinSpawnPeriodScale 손잡이 미설계" — **차별점**: 해당 커밋은 스폰 *주기(period)* 하한이 없음을 지적. 이번 후보는 스폰 *동시 출력 수(simultaneous output count)* 상한이 없음을 지적. 두 이슈는 BalanceConfig 의 서로 다른 필드(`SpawnPeriod` vs `MaxSimultaneousOutput`)에 해당하며, 같은 SpawnerHaste·Multiply 카드가 연관되어도 설계 공백의 차원이 다름.
- 2026-06-17 (3a9bed3): "SpawnWisps Wisp수량 카드 Swarm 축 귀속 — Tank·Swarm 교차 픽 딜레마 설계 검증 제안" — **차별점**: 해당 커밋은 SpawnWisps 의 축 귀속이 설계적으로 딜레마를 만드는지 검증 제안. 이번 후보는 SpawnX 가산 3픽 + Tier3 합산으로 출력이 5까지 늘어나는 수치 계산 공백.

카테고리(출력 수 손잡이 미설계)·요지(SpawnPhantoms×3+Tier3=5대)·근거(card-renewal.md §3.4 + §4.2 Tier 표)가 기존 18건과 모두 겹치는 항목 없음.

## 4. 제외 (범위 밖)

- 신규 팬텀 변종/몬스터 종 추가 → CLAUDE.md §8 "신규 영웅·몬스터·카드 리소스 제작 금지"
- SwarmRush 미구현 구현 → card-renewal.md §3.4 별도 사이클 대기, 컨텐츠 감사 범위 밖
- 서버 연동(v0.3) 관련 클라이언트 항목 → 컨텐츠 후보 영역 밖

## 5. 다음 단계 제안

채택 시 game-designer 에게 정식 기획 요청:

1. `BalanceConfig` 에 스포너 동시 출력 상한 손잡이 추가 — 종별 `MonsterStatRow.MaxSimultaneousOutput` 또는 전역 `GlobalMaxSpawnerOutput` 설계
2. Phantom·Wisp 스포너 SpawnX 3픽 + Tier3 복합 시나리오별 적정 상한값(예: 3~4대) 수치 근거 작성
3. 관련 QA 시뮬레이션(DebugAutoPicker 훅 구현 후): Swarm 7장 빌드 × SpawnPhantoms 3픽 전략 N판 → 평균 사망 시각·클리어율 검증

## 6. 쉬운 설명 (비개발자 요약)

"팬텀"은 작고 까만 유령처럼 생긴 몬스터로, 혼자는 약하지만 여러 마리가 떼로 몰려다니며 영웅을 괴롭힌다. 평소에는 6초마다 1마리씩 나오지만, "팬텀 더 부르기" 카드를 3번 선택하고 "Swarm 시너지 최고 단계(Tier3)"까지 달성하면 한 번에 5마리씩 등장한다. 영웅이 열심히 처치해도 6초도 안 돼 5마리가 다시 채워지므로, 화면은 쉴 틈 없이 팬텀으로 가득 찬다. 문제는 이 숫자에 브레이크 역할을 하는 설정값이 게임 밸런스 파일에 아직 없다는 것 — 개발자가 "최대 몇 마리까지 동시에 내보낼 수 있나"를 마음대로 조절할 수단이 빠져 있다. 그래서 이번에 제안하는 것은: 스포너가 동시에 내보낼 수 있는 몬스터 최댓값을 설정 파일에 추가해 밸런스를 쉽게 조절할 수 있게 하는 것이다.
