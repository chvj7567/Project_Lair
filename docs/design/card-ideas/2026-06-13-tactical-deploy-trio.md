# Card Ideas — 2026-06-13 — 즉시 소환 전술 배치 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 즉시 소환 전술 배치 — 현재 Tank 축에만 있는 `WallOfWisps` (영웅 주변 즉시 소환 액티브) 패턴을 Swarm/Debuff/Dps 축에도 대응쌍으로 완성한다. 30초 액티브 창에 특정 종(種)을 순식간에 투입하는 전술적 증원 카드 3종.
- **목록**: SwarmRush (Phantom 6마리 즉시 소환 — Swarm 축) / PlagueCloud (Plague 4마리 즉시 소환 — Debuff 축) / ReaperStrike (Reaper 3마리 즉시 소환 + 전 Reaper 공격력 ×1.5, 8s — Dps 축)
- **기존 28장 + git log 과거 16회차와의 중복 회피 확인됨**
  - 기존 28장: 즉시 소환 액티브는 `WallOfWisps`(Wisp 4마리, Tank 축) 1장 뿐. Debuff/Dps/Swarm 축 대응쌍 부재.
  - SwarmRush: 컨셉서 §11.3 명시 원안 미구현 카드. `Multiply`의 원안(`SwarmRushEffect`, Phantom 6마리 즉시 소환)이 `FastBreedingEffect`(스포너 주기 ×0.6)로 교체·미구현 상태. 제안이 아닌 원안 복구.
  - 과거 16회차 검토: 06-03(wisp-wall-encirclement) = 기존 `WallOfWisps` 전술 활용 분석. 즉시 소환을 Debuff/Dps/Swarm 축 액티브로 제안한 회차 없음.

---

## 1. SwarmRush — 무리 돌격 (가칭)

- **카테고리**: 액티브 — Swarm 축
- **효과 모델**:
  - Phantom 6마리 즉시 소환. 영웅 중심 60도 간격 6방위 ring 배치 → 안쪽(영웅) 방향으로 수렴 공격.
  - 소환된 Phantom은 즉시 CHMPool 에서 Pop → 글로벌 캡 내 일반 전투 합류.
  - 수치 근거: `WallOfWisps` = Wisp 4마리. Phantom(HP 30, DPS 5)은 Wisp(HP 200, DPS 10)보다 훨씬 약하므로 4 → 6마리로 보상. 6마리 × DPS 5 = 30 DPS 순간 집중. 수렴 이동 평균 2~3s 생존 시 60~90 추가 피해 burst. 이후 글로벌 캡 경쟁으로 일부 자연 교체.
  - PhantomMoveSpeedBoost(이속 ×1.5) 선픽 시: 소환 즉시 빠른 수렴 → 영웅이 회피 전에 피해 적중 확률 ↑.
- **구현 패턴**: `SwarmRushEffect.cs` — `WallOfWispsEffect` 구조 완전 동일. 파라미터만 `EMonster.Phantom`, `count = 6`, `angleDeg = 60`. 컨셉서 §11.3 미구현 원안 그대로 복구. 기존 `ECardId.Multiply` 슬롯·enum 값 재활용 예정(`FastBreedingEffect` → `SwarmRushEffect` 교체).
- **시너지 후크**:
  - `PhantomMoveSpeedBoost` + `SpawnPhantoms` + SwarmRush: Swarm 3카드 → Tier1 발동(전 Phantom·Wisp 이속 ×1.3 즉시). 여기서 SwarmRush 사용 시 빠른 Phantom 6마리 ring burst → 강력한 순간 압박 창.
  - `SpawnerHaste`(스포너 주기 ×0.8) + SwarmRush: 소환 직후 스포너가 Phantom을 빠르게 보충 → burst 후 연속 파도 유지.
  - `Slow`(영웅 이속 ×0.5, 10s) + SwarmRush: 느려진 영웅 주변에 Phantom ring → 도주 불가 포위.
  - `PhantomHpBoost`(06-12 제안): SwarmRush로 소환된 Phantom이 HP 45로 더 오래 생존 → burst 시간 연장.
- **구현 비용 추정**: 1 (`WallOfWispsEffect` 파라미터 교체만. `FastBreedingEffect`→`SwarmRushEffect` 명칭 변경 포함해도 1)
- **중복 재검증**: 기존 28장에 SwarmRush 없음(`FastBreedingEffect`가 Multiply에 임시 잔존 중). 과거 16회차: 06-03(wisp-wall-encirclement)은 `WallOfWisps` 기반 포위 전술 조합 제안이지, Phantom 즉시 소환 액티브 카드 신규 제안이 아님. 완전 신규(원안 복구).

---

## 2. PlagueCloud — 역병 구름 (가칭)

- **카테고리**: 액티브 — Debuff 축
- **효과 모델**:
  - Plague 4마리 즉시 소환. 영웅 중심 90도 간격 4방위 ring 배치.
  - 소환된 Plague는 즉시 영웅 공격 개시 → 각 Plague의 OnHit 둔화(SlowFactor 20% 기본) 즉각 적용.
  - 수치 근거: Plague(HP 50, DPS 5, SlowFactor 20%). 4마리 동시 접촉 시 슬로우 중첩 — 단 `SlowFactor`가 가산이 아닌 최솟값 취합 방식이면 1마리로도 최대치 도달하므로, BalanceConfig에 슬로우 중첩 가산 정책 확인 필요. 중첩 가산 허용 시 4마리 동시 = 80% 슬로우 (실질 캡 제한 권장: 60% max). 중첩 비적용 시 효과는 Plague 숫자 보장 자체가 가치 (글로벌 캡 여유 소진 없이 즉각 포위).
  - `PlagueSlowBoost` 선픽 시: 각 Plague SlowFactor ×0.75 → 단일 Plague도 강한 슬로우, 4마리 cloud = 최대 슬로우 보장.
- **구현 패턴**: `PlagueCloudEffect.cs` — `WallOfWispsEffect` 구조 완전 동일. `EMonster.Plague`, `count = 4`, `angleDeg = 90`.
- **시너지 후크**:
  - `PlagueSlowBoost` + `SpawnPlagues` + PlagueCloud: Debuff 3카드 → Tier1 발동(SlowFactor ×0.8 추가). cloud 사용 시 즉각 고슬로우 4마리 ring → 영웅 이동 거의 불가.
  - `Fear`(영웅 3s 도주) + PlagueCloud: Fear 사용 → 영웅 도주 방향에 PlagueCloud 투하 → 도주 경로 봉쇄 + 즉각 둔화.
  - `Bleed`(이동 시 HP -2%, 10s) + PlagueCloud: 슬로우로 이동 강제 + Bleed로 이동 시 HP 감소 — 움직이면 피가 깎이는 딜레마.
  - `HeroPoisonAura`(독 장판 5 DPS) + PlagueCloud: 독 장판이 영웅 발밑에, Plague 4마리가 외곽 — 안쪽도 바깥도 위험한 포위망.
- **구현 비용 추정**: 1 (`WallOfWispsEffect` 파라미터 교체만)
- **중복 재검증**: 기존 28장에 Plague 즉시 소환 액티브 없음. 과거 16회차: 05-30(plague-poison-chain) = Plague 사망 트리거 독 연쇄, 06-04(dps-debuff-prey-hunt) = Plague 둔화+Dps 연계 패시브 — 모두 즉시 소환 패턴 아님. 완전 신규.

---

## 3. ReaperStrike — 처형 돌격 (가칭)

- **카테고리**: 액티브 — Dps 축
- **효과 모델**:
  - Reaper 3마리 즉시 소환 + 필드의 모든 Reaper 공격력 ×1.5, 8초 지속.
  - 소환된 Reaper 3마리 + 기존 필드 Reaper 전부에 적용.
  - 수치 근거: Reaper 기본 DPS = 40. ×1.5 적용 시 60 DPS. 소환 3마리 + 기존 평균 2~3마리 = 5~6마리 총 Reaper 60 DPS = 300~360 DPS 집중 8초. 영웅 HP 기준(2~4분 교전, 약 400~700 HP 잔여) — 8초 최대 2400~2880 피해 잠재치. 실제: 영웅 반격(50 DPS) + 처치로 Reaper 감소 → 실현 피해 약 600~1200. 충분히 위협적이되 즉사 수준은 아님(밸런스 §8 기준 준수).
  - 버프는 GuardianRage·IronWill 패턴과 동일 — 타입별 일시 배율 적용, 8초 후 해제.
  - `WallOfWisps`와 비교: Wisp 4마리 소환 vs Reaper 3마리 + 전체 버프. Reaper가 DPS가 높아 숫자를 3마리로 낮추고 버프 추가로 패키지 완성.
- **구현 패턴**: `ReaperStrikeEffect.cs` — 소환: `WallOfWispsEffect` 동일 (`EMonster.Reaper`, `count = 3`, `angleDeg = 120`). 버프: `GuardianRageEffect` 동일 — `MonsterBuffService.ApplyTemporaryPowerMultiplier(EMonster.Reaper, 1.5f, 8f)` 패턴 재사용. 소환 먼저 → 버프 적용 순서로 소환 Reaper도 버프 적용받음.
- **시너지 후크**:
  - `ReaperAtkSpeed`(쿨다운 ×0.7) + ReaperStrike: 빠른 Reaper × ×1.5 공격력 = DPS 더욱 집중. Dps 2카드 → Tier1까지 1카드 부족.
  - `MarkOfDeath`(영웅 피해 ×1.5, 5s) + ReaperStrike(×1.5, 8s): 5초 중첩 구간 — 소환 Reaper 3마리 × 60 DPS × 1.5 피해 = 270 DPS. 영웅에게 8초 내 ~1350 burst.
  - `SpawnReapers`(스포너 +1) + ReaperStrike: 필드 Reaper 수 증가 → 버프 적용 대상 증가 → ReaperStrike 가치 배증.
  - `BloodThirst`(on-kill HP 회복, 30s) + ReaperStrike: 강화된 Reaper가 처치 시 주변 몬스터 HP +30 회복 → 공격 중 아군 생존력 보완.
- **구현 비용 추정**: 2 (WallOfWispsEffect + GuardianRageEffect 두 패턴 조합. 소환 후 버프 순서 보장 1회성 로직 추가)
- **중복 재검증**: 기존 28장에 Reaper 즉시 소환 액티브 없음(Dps 액티브 = Frenzy·BloodThirst·MarkOfDeath — 모두 소환 없음). 과거 16회차: 06-01(reaper-hex-dps-deepening) = Reaper/Hex 패시브 스탯 심화. 06-08(escape-punishment) = 리퍼 도주 추격. 모두 즉시 소환+버프 콤보 패턴 아님. 완전 신규.

---

## 4. 공통 테마 고찰

세 카드는 **"`WallOfWisps` 즉시 소환 패턴의 4축 완성"** 이라는 하나의 설계 공백을 채운다:

| 카드 | 축 | 소환 종 | 추가 효과 | WallOfWisps와의 차이 |
|---|---|---|---|---|
| WallOfWisps (기존) | Tank | Wisp × 4 | 없음 (소환만) | 기준 패턴 |
| SwarmRush (신규) | Swarm | Phantom × 6 | 없음 (소환만, +2마리) | Phantom은 약해 수로 보상 |
| PlagueCloud (신규) | Debuff | Plague × 4 | 없음 (소환만, OnHit 슬로우 자동) | Plague OnHit이 즉각 효과 |
| ReaperStrike (신규) | Dps | Reaper × 3 | 전 Reaper 공격력 ×1.5, 8s | Reaper는 강해 수 줄이고 버프 패키지 |

**왜 이 테마를 오늘 골랐는가:**
- 어제(06-12) 제안의 3장이 "패시브 스탯 공백 채우기"였다면, 오늘은 "액티브 패턴 공백 채우기" — 자연스러운 연속 설계 탐색.
- `WallOfWisps`는 v0.6 카드 리뉴얼에서 신규 추가되었으나 Tank 축 단독이었다. 4축 균형 설계에서 3축의 대응쌍이 빠진 상태.
- `SwarmRush`는 컨셉서 §11.3에 미구현 원안으로 명시되어 있어 제안 확신도가 가장 높다 — game-designer 채택 시 "복구"로 처리 가능.
- QA 리포트가 BLOCKED 상태이므로 픽률 데이터 대신 **구조적 대칭 분석**을 근거로 삼았다.
- 3장 모두 `WallOfWispsEffect` + `GuardianRageEffect` 기존 패턴 조합으로 구현 비용 1~2 수준.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- ECardId 후보: `SwarmRush`(기존 `Multiply` 슬롯 재활용 — FastBreedingEffect 교체), `PlagueCloud`, `ReaperStrike`
- v0.2 진입 전까지 backlog 보관
- **채택 우선순위**: SwarmRush > PlagueCloud > ReaperStrike.
  - SwarmRush는 원안 복구이므로 가장 확신도 높음. enum 슬롯 이미 보존됨.
  - PlagueCloud는 구현 비용 1이고 Debuff 축 액티브 다양성을 즉시 확보.
  - ReaperStrike는 구현 비용 2 (2패턴 조합)지만 Dps 액티브 중 유일한 "소환+버프" 조합으로 가장 극적인 카드.
- **주의**: SwarmRush 채택 시 `Multiply`(`FastBreedingEffect`) enum 값을 `SwarmRush`(`SwarmRushEffect`)로 교체하는 작업 포함. 컨셉서 §11.3 "SwarmRushEffect 교체 예정이었으나 미구현" 항목 해소.

---

## 6. 쉬운 설명 (비개발자 요약)

던전 주인은 30초마다 카드를 한 장 쓸 수 있는데, 대부분의 카드는 몬스터를 조금씩 강하게 만들거나 영웅에게 저주를 거는 식입니다. 그런데 "지금 당장 몬스터가 필요해!" 싶은 위기 순간에 쓸 수 있는 '즉각 증원' 카드가 지금은 위스프 종 하나뿐입니다. 마치 게임에서 '긴급 구조 버튼'이 탱크 팀에게만 있고 딜러팀이나 방해 팀에는 없는 것처럼요. 그래서 오늘 제안하는 카드 3장은: 팬텀 6마리를 순식간에 소환해 영웅을 포위하는 카드, 역병 생물 4마리를 뿌려 영웅의 발걸음을 꽁꽁 묶는 카드, 처형자 리퍼 3마리를 순간이동시키고 잠시 동안 모든 리퍼가 더 강하게 공격하는 카드입니다.
