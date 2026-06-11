# Card Ideas — 2026-06-12 — 팬텀·플레이그·헥스 빠진 스탯 채우기 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 몬스터 스탯 공백 완성 — 현재 28장에서 팬텀(HP 강화 없음)·플레이그(공격력 강화 없음)·헥스(스포너 출력 카드 없음) 세 종에게만 비어 있는 각 1개 슬롯을 채운다. 이 3장은 기존 카드와 조합해 처음으로 "팬텀 전용 생존력 서브빌드", "플레이그 대미지+둔화 복합 빌드", "헥스 원거리 포격 지속 빌드"를 완성한다.
- **목록**: PhantomHpBoost (팬텀 HP 강화) / PlaguePowerBoost (플레이그 공격력 강화) / SpawnHexes (헥스 스포너 출력 추가)
- **기존 28장 + git log 과거 15회차와의 중복 회피 확인됨**
  - 기존 28장: Phantom 관련 카드 = PhantomMoveSpeedBoost(이동속도), SpawnPhantoms(스포너+1), SpawnWisps(Swarm축). HP 강화 카드 없음.
  - 기존 28장: Plague 관련 카드 = PlagueSlowBoost(둔화 배율), SpawnPlagues(스포너+1). 공격력 강화 카드 없음.
  - 기존 28장: Hex 관련 카드 = HexRangeBoost(사거리), ReplaceReapersToHex(교체). 스포너 동시 출력 +1 카드 없음(6종 중 Hex만 부재).
  - 과거 15회차 전부 검토: Phantom HP·Plague 공격력·Hex 스포너 출력 카드 어느 회차에도 미제안. 06-01회차(리퍼·헥스 딜러 심화)의 HexRapidFire = 쿨다운 배율, ExecutionSquad = 즉시 혼합 소환 — 스포너 영구 출력 증가와 메커니즘이 다름.

---

## 1. PhantomHpBoost — 팬텀의 생명력 (가칭)

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - Phantom 종 글로벌 HP ×1.5 (영구). 기존 스폰된 Phantom에 즉시 소급.
  - 기본 Phantom: HP 30. 적용 후: HP 45.
  - 수치 근거: WispHpBoost(위스프 HP ×1.5: 200→300), WraithDamageBoost(레이스 HP ×1.5: 500→750)와 동일 배율. Phantom은 HP가 가장 낮아 영웅(공격력 50, 공속 1s = 50 DPS)에게 단발 처치되는 경우가 많음(0.6초 생존). ×1.5로 HP 45가 되면 영웅의 단발 처치가 0.9초 생존으로 연장 → Phantom의 공격 기회가 평균 1회 추가 발생.
  - 필드 Phantom 평균 4~6마리(SpawnPhantoms 픽 전제) × 추가 공격 1회(DPS 5) = +20~30 추가 데미지/사이클 기여. 절대값은 작지만 떼 압박의 누적 밀도를 높임.
- **구현 패턴**: `PhantomHpBoostEffect.cs` — WispHpBoostEffect 구조 그대로, EMonster.Phantom 종만 교체. `ctx.GetMonsters(EMonster.Phantom)` 순회 → `health.SetMaxHp(health.Max * 1.5f)`. MonsterBuffService 글로벌 타입 버프 등록 패턴 재사용.
- **시너지 후크**:
  - PhantomMoveSpeedBoost + SpawnPhantoms + PhantomHpBoost: Swarm 축 3카드 → Tier1 발동(Wisp·Wraith MoveSpeed ×1.3). Phantom이 더 많이, 더 빠르게, 더 오래 생존 → 영웅 포위 압박 극대화.
  - SpawnerHaste(모든 스포너 주기 ×0.8): 더 빠른 보충으로 Phantom 수 유지 → HP 강화가 "생존 안정성" 제공, 떼 밀도가 유지됨.
  - Slow(영웅 이속 ×0.5, 10s): 영웅이 느려진 상태에서 HP가 높아진 Phantom이 더 오래 붙어 있으면 DPS 지속시간 증가.
- **구현 비용 추정**: 1 (WispHpBoostEffect 완전 동일 패턴. EMonster.Phantom Enum 교체만)
- **중복 재검증**: 기존 28장에 Phantom HP 강화 없음. 과거 15회차 중 Phantom 관련: 06-07(각성 = Power/Speed + OnHit DoT), 06-02(PhantomBirth = OnDeath 소환), 06-03(위스프 벽 연계 전술) — 모두 HP 배율 카드 없음. 이 카드는 단순 종 HP 배율로 완전 신규.

---

## 2. PlaguePowerBoost — 역병 독점 (가칭)

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - Plague 종 공격력 글로벌 영구 ×1.3. 기존 스폰된 Plague에 즉시 소급.
  - 기본 Plague: HP 50, 공격력 5, 공속 1s = 5 DPS. 적용 후: 6.5 DPS.
  - 수치 근거: Plague의 핵심 역할은 DPS보다 둔화 적용에 있으므로 ×1.5처럼 강하게 올리지 않고 ×1.3으로 보수적 설정. 둔화를 걸면서 소소하게 체력도 깎는 "독 복합 딜러" 포지션. SpawnPlagues(스포너 +1)와 조합해 필드 Plague 평균 2~3마리 × 6.5 DPS = 13~19.5 DPS. 메인 딜 소스는 아니지만 Debuff 축 Tier1 달성(PlagueFactor ×0.8 강화)과 함께 둔화+독 데미지 복합 압박.
- **구현 패턴**: `PlaguePowerBoostEffect.cs` — WispHpBoostEffect 구조 동일, 대상 Enum = EMonster.Plague, 조작 필드 = `MeleeAttacker._power × 1.3f`. 06-01 제안 ReaperLethalStrikeEffect와 동일 패턴 — 종과 배율만 다름.
- **시너지 후크**:
  - PlagueSlowBoost + SpawnPlagues + PlaguePowerBoost: Debuff 축 3카드 → Tier1 발동(PlagueFactor ×0.8). 영웅 이동속도 대폭 저하 + 공격력 소폭 증가. 이 조합 하나로 Debuff 빌드 기반 완성.
  - Bleed(이동 시 HP -2%, 10s) + PlagueSlowBoost + PlaguePowerBoost: 영웅이 느려질수록 이동 HP 손실 축적 + Plague 공격 피해 누적 → Debuff 복합 빌드의 핵심 3-card 콤보.
  - HuntersInstinct(06-04 제안, 둔화 영웅 추가 피해 +60%): Plague로 둔화된 영웅에게 Reaper/Hex가 추가 피해 → Debuff→Dps 크로스 축 연계를 더욱 강화.
- **구현 비용 추정**: 1 (WispHpBoostEffect / OrcAtkSpeedEffect 패턴 완전 동일. Enum·배율 교체만)
- **중복 재검증**: 기존 28장에 Plague 공격력 강화 없음. 과거 15회차 중 Plague 관련: 05-30(독 연쇄 = 사망 트리거 독 체인), 06-08(도주 처벌 = 영웅 도주 연동), 06-04(Dps×Debuff = HuntersInstinct 연계) — 모두 Plague 직접 공격력 배율 없음. 이 카드는 Plague 공격력 수치 배율로 완전 신규.

---

## 3. SpawnHexes — 원거리 포대 증설 (가칭)

- **카테고리**: 패시브 추가 (Dps 축)
- **효과 모델**:
  - Hex 스포너 동시 출력 +1 (영구). 기존 Hex 스포너가 내보내는 동시 마릿수가 1→2로 증가.
  - 기본 Hex: HP 60, 공격력 30, 사거리 5.0. 스포너 1마리 → 2마리 동시 스폰.
  - 수치 근거: SpawnReapers(Reaper +1)와 완전 동일 패턴. Hex는 원거리(사거리 5)이므로 영웅과 일정 거리 유지 → 처치 위험 낮아 Reaper보다 생존성 높음 → DPS 지속시간이 더 길다. 대신 Hex 자체 HP 60으로 낮아 근접전 취약. 두 Hex 동시 포격 = 60 DPS 안정 출력.
  - HexRangeBoost 조합: 사거리 ×1.4 = 7.0 유닛에서 2마리 동시 사격 → 영웅이 접근 불가한 사정권에서 포격 압박.
- **구현 패턴**: `SpawnHexesEffect.cs` — SpawnReapersEffect 구조 그대로, EMonster.Hex로 종만 교체. `spawnerManager.IncrementOutput(EMonster.Hex, 1)`.
- **시너지 후크**:
  - HexRangeBoost + SpawnHexes: 멀리서 2마리 동시 사격 → "원거리 포격 빌드" 핵심 콤보. Dps 2카드 → Tier1까지 1카드 더 필요.
  - ReplaceReapersToHex(Reaper 스포너 → Hex 출력 전환) + SpawnHexes: Hex 생산량 극대화 → 필드에 원거리 몬스터 다수. Tank 유닛(Wisp/Wraith)이 근접을 막는 동안 Hex가 안전 포격하는 복합 전선.
  - MarkOfDeath(영웅 받는 데미지 ×1.5, 5s) + SpawnHexes: 2마리 동시 Hex 포격 × 1.5배 피해 = 90 DPS 집중 피해 창(窓).
  - 06-01 제안 ExecutionSquad(Reaper+Hex 즉시 혼합 소환, 액티브·일시적)와 역할 차이: ExecutionSquad는 타이밍 투입, SpawnHexes는 영구 생산력 증가 → 상호 보완적.
- **구현 비용 추정**: 1 (SpawnReapersEffect 완전 동일 패턴. Enum 교체만)
- **중복 재검증**: 기존 28장에 SpawnHexes 없음 — 6종 중 Hex만 스포너 출력 추가 카드 부재. 과거 15회차: 06-01 ExecutionSquad = Reaper+Hex 즉시 소환(액티브·비영구), SpawnHexes = 패시브 스포너 영구 출력 증가 — 메커니즘 완전히 다름.

---

## 4. 공통 테마 고찰

세 카드 모두 **"기존 28장에서 특정 몬스터 종(種)에 비어 있는 스탯 슬롯"** 을 채운다:

| 카드 | 대상 종 | 비어 있던 슬롯 | 완성되는 빌드 경로 |
|---|---|---|---|
| PhantomHpBoost | Phantom | HP 강화 | Swarm 3카드 Tier1 + 생존력 보강 서브빌드 |
| PlaguePowerBoost | Plague | 공격력 강화 | Debuff 3카드 Tier1 + 둔화·독 복합 딜 |
| SpawnHexes | Hex | 스포너 출력 +1 | Dps 원거리 포격 지속 빌드 완성 |

**왜 이 테마를 오늘 골랐는가:**
- QA 리포트가 BLOCKED(시뮬 데이터 없음) 상태이므로 픽률 데이터 대신 **구조적 공백 분석**을 근거로 삼았다.
- 기존 28장을 종별·슬롯별로 매핑하면 Phantom(HP없음) / Plague(Power없음) / Hex(Spawn카드없음) 세 공백이 단번에 식별된다.
- 특히 SpawnHexes는 설계 의도적으로 빠진 게 아니라 v0.6 카드 리뉴얼 과정에서 누락된 것으로 보인다 — 나머지 5종(위스프·레이스·리퍼·플레이그·팬텀)은 모두 스포너 출력 카드를 보유 중.
- 세 카드 모두 구현 비용 1(기존 패턴 Enum·수치 교체)로 v0.2 풀 확장의 "빠른 채우기" 전략에 최적합.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- ECardId 후보: `PhantomHpBoost`, `PlaguePowerBoost`, `SpawnHexes`
- v0.2 진입 전까지 backlog 보관
- **채택 우선순위**: SpawnHexes > PlaguePowerBoost > PhantomHpBoost. SpawnHexes가 가장 명백한 설계 공백(의도적 누락이 아닌 오누락)이므로 1순위. PlaguePowerBoost는 Debuff 빌드 완성도에 즉각 기여. PhantomHpBoost는 Swarm Tier1 달성 선택지를 확대.

---

## 6. 쉬운 설명 (비개발자 요약)

게임에서 몬스터마다 나름의 역할이 있는데, 팬텀은 "떼로 달려드는 몬스터", 플레이그는 "영웅을 느리게 만드는 몬스터", 헥스는 "멀리서 공격하는 원거리 몬스터"입니다. 그런데 팬텀은 강화 카드가 속도뿐이라 너무 쉽게 죽고, 플레이그는 둔화만 시킬 뿐 공격이 너무 약하고, 헥스는 소환 카드가 없어 혼자 싸우는 경우가 많습니다. 마치 팀 스포츠에서 특정 선수에게만 유독 훈련 장비가 부족한 상황이죠. 그래서 오늘 제안하는 카드 3장은: 팬텀이 좀 더 오래 버티게 해주는 카드, 플레이그가 좀 더 아프게 물게 해주는 카드, 헥스 포격병을 하나 더 불러내는 카드입니다.
