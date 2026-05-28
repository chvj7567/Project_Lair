# Card Ideas — 2026-05-29 — 종 간 연계 시너지 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 종 간 연계 시너지 — 특정 몬스터 종 조합이 동시에 살아있을 때 매 tick ON/OFF되는 조건부 상시 패시브 효과 3장
- **목록**: 탱크-딜러 협약 (Passive/강화) / 유틸-딜러 공조 (Passive/강화) / 군단의 방패 (Passive/강화)
- **기존 25장 + git log 과거 회차 중복 회피 확인**:
  - git log 조회 결과 루틴 과거 커밋 **1건** (2026-05-28: 전장 상태 감지 3종 — 군중의 포효 / 약자의 기백 / 연쇄 저주)
  - 과거 회차는 "픽 시점 스냅샷 기반 스케일링"이고, 이번 3장은 "매 tick 조건 감시 ON/OFF 시너지" — 메커니즘 축이 다름
  - 기존 25장 전원 검토: 단일 종 무조건 강화 / 소환 / 교체 / 영웅 오라 / 시한부 버프·저주만 존재 → 종 간 조건부 시너지 미구현 확인

---

## 1. 탱크-딜러 협약 (Tanker-Dealer Pact) — 가칭

- **카테고리**: 패시브 강화 (복합 조건형)
- **효과 모델**:
  - 영구 조건부 버프. 매 tick마다 Wisp 또는 Wraith(탱커) 중 하나 이상 AND Reaper 또는 Hex(딜러) 중 하나 이상이 필드에 동시에 살아있으면, 전체 몬스터 데미지 **+25%**.
  - 조건 불충족 시(탱커 0마리 또는 딜러 0마리) 버프 즉시 해제. 조건 복원 시 다음 tick에 자동 재적용.
  - 밸런스 근거 (컨셉 §8): WraithDamageBoost = 레이스에만 +50% 무조건 영구 (pick 47%). 이 카드 = 전체 종 +25%이지만 탱커·딜러 공존 조건 필요. QA 6차 기준 다양성 빌드는 탱커·딜러 동시 유지 구간 약 70% → 기대 효과 +17.5%. 교체 카드 자제 비용을 치르고 +25% 전체 버프를 얻는 트레이드오프.

- **구현 패턴**: MonsterBuffService.Tick() 내 조건 분기 추가.
  ```
  bool hasTanker = ctx.GetMonsters(EMonster.Wisp).Any()
               || ctx.GetMonsters(EMonster.Wraith).Any();
  bool hasDealer = ctx.GetMonsters(EMonster.Reaper).Any()
               || ctx.GetMonsters(EMonster.Hex).Any();
  if (hasTanker && hasDealer)
      ApplyGlobalPowerScale(1.25f);  //# base 1.0 리셋 후 재적용 (기존 패턴 준수)
  ```
  기존 MonsterBuffService "매 tick base 리셋 → 활성 버프 곱셈 누적" 패턴 자연 확장. IBattleContext.GetMonsters(filter) 재사용, 신규 시스템 없음.

- **시너지 후크**:
  - SpawnWraith + SpawnReapers 조합 → 탱커·딜러 상시 유지 → 협약 상시 활성.
  - ReplaceWispsToWraith + 교체 카드 최소화 빌드 → 탱커 수 극대화.
  - 컨셉서 §5.2 "동족 only → 데미지 +30%" 방향성과 유사하나, 동족이 아닌 **역할 조합** 기반으로 차별화.

- **구현 비용 추정**: **2** — MonsterBuffService.Tick() 내 조건 로직 추가 + 기존 PowerScale 패턴 재사용. 신규 인터페이스·시스템 없음.

- **중복 재검증**: 기존 강화 6장(WispHpBoost / WraithDamageBoost / ReaperAtkSpeed / HexRangeBoost / PlagueSlowBoost / PhantomMoveSpeedBoost)은 전부 단일 종 무조건 영구 버프. 이 카드는 탱커 종 AND 딜러 종 공존 조건 — 종 간 상호작용 축 자체가 신규.

---

## 2. 유틸-딜러 공조 (Utility-Dealer Coordination) — 가칭

- **카테고리**: 패시브 강화 (복합 조건형)
- **효과 모델**:
  - 영구 조건부 버프. 매 tick마다 Plague 또는 Spider(둔화 유틸) 중 하나 이상이 살아있으면, Reaper와 Hex 전체 공격속도 **+35%** (CooldownScale ×0.74).
  - 유틸 사망 시 딜러 버프 즉시 해제 → 유틸을 보호해야 딜러가 강해지는 설계. 유틸이 살아난(스폰) 다음 tick에 자동 재활성.
  - 밸런스 근거: ReaperAtkSpeed 카드 = 리퍼에만 +30% 무조건 영구 (pick 41%). 이 카드 = Reaper + Hex 양쪽 +35% 동시지만 Plague(50HP) 또는 Spider(50HP) 생존 조건. 유틸 몬스터는 저HP라 영웅이 빠르게 처치할 수 있어 조건 유지가 실질적 전략 도전.

- **구현 패턴**: MonsterBuffService.Tick() 내 조건 분기.
  ```
  bool hasUtility = ctx.GetMonsters(EMonster.Plague).Any()
                 || ctx.GetMonsters(EMonster.Spider).Any();
  if (hasUtility)
  {
      ApplyCooldownScaleToType(EMonster.Reaper, 0.74f);
      ApplyCooldownScaleToType(EMonster.Hex,    0.74f);
  }
  ```
  기존 CooldownScale 패턴 재사용. 종별 필터 적용은 이미 강화 카드 효과 클래스(ReaperAtkSpeedEffect 등)에서 사용 중인 패턴과 동일.

- **시너지 후크**:
  - SpawnPlagues + PlagueSlowBoost → Plague 강화+보전 → Reaper/Hex 지속 버프.
  - SpawnSpiders + SpiderSlowBoost → Spider 강화 → 공조 활성화.
  - "유틸 보호"라는 새로운 전략 목표 생성 — 기존 빌드에서 Plague/Spider는 선택 사항이었으나, 이 카드 픽 후 핵심 유지 대상으로 격상.
  - 컨셉서 §5.2 "둔화 + 광역 → 둔화 상태 영웅에게 광역 데미지↑" 방향성의 가장 직접적 카드 구현.

- **구현 비용 추정**: **2** — MonsterBuffService.Tick() 내 조건 분기 + 종별 CooldownScale 적용. 기존 패턴 재사용.

- **중복 재검증**: 기존 ReaperAtkSpeed = 단일 종 무조건. 기존 PlagueSlowBoost = Plague 둔화 효과 강화(다른 종 관계 없음). 이 카드는 "Plague/Spider 생존 → Reaper/Hex 공격속도 증폭"이라는 종 간 의존 관계 구축 — 단방향 연계 구조로 기존 25장 어느 카드와도 겹치지 않음.

---

## 3. 군단의 방패 (Legion Shield) — 가칭

- **카테고리**: 패시브 강화 (수량 조건형)
- **효과 모델**:
  - 영구 조건부 버프. 매 tick마다 필드 전체 생존 몬스터 수가 **10마리 이상**이면, 모든 몬스터의 받는 데미지 **-30%** (DamageTakenScale = 0.7).
  - 10마리 미만 시 해제, 복원 시 자동 재활성.
  - 밸런스 근거 (컨셉 §8 / QA 6차): IronWill(액티브 버프) = -30% 15초 시한부, 픽률 19%. 이 카드는 동일한 -30%이지만 영구 조건부. QA 6차 §3.5 기준 평균 종료 시 필드 17.5마리(캡 18의 97%) → 중후반부 거의 상시 활성. 초반부(0~60초, 캡 포화 전)는 10마리 미달 가능성 있어 자연스러운 시간 조절 역할 겸함. IronWill의 패시브·상시 버전이지만 전략 요건(대량 소환 유지)이 달라 중복 아님.

- **구현 패턴**: MonsterBuffService.Tick() 내 count 조건 분기.
  ```
  int aliveCount = ctx.GetMonsters().Count();
  if (aliveCount >= 10)
      ApplyGlobalDamageTakenScale(0.7f);  //# base 1.0 리셋 후 재적용
  ```
  IBattleContext.GetMonsters(null) 전체 조회 후 count. 기존 DamageTakenScale 패턴 재사용. 신규 API 없음.

- **시너지 후크**:
  - SpawnWisps + SpawnPhantoms + SpawnBats → 다수 몬스터 유지 → 방패 상시 활성.
  - WispHpBoost → Wisp 생존력 향상 → 총 생존 수 유지 구간 연장 → 방패 활성 시간 증가.
  - 소환 중심·교체 자제 빌드에서 자연스럽게 강한 시너지.
  - 탱크-딜러 협약과 동시 픽 시 → "구성 다양성" + "수량 유지" 이중 목표 달성 시 중첩 보상.

- **구현 비용 추정**: **2** — MonsterBuffService.Tick() 내 count 조건 + 기존 DamageTakenScale 패턴 재사용.

- **중복 재검증**: 기존 IronWill = 액티브 버프, 15초 시한부. 이 카드 = 패시브 조건부 상시, 총 수량 기준. 기존 25장 패시브 중 총 생존 수를 조건으로 쓰는 카드 없음. 과거 루틴 1회차의 "약자의 기백"(개별 HP 비율 기반)과도 감시 대상(총 수량 vs 개별 HP 비율)이 달라 중복 아님.

---

## 4. 공통 테마 고찰

세 카드 모두 **"특정 몬스터 구성이 동시에 유지될 때 매 tick 활성화되는 조건부 상시 시너지"** 축을 도입한다.

| | 기존 25장 강화 | 전회차 루틴 | **이번 3장** |
|---|---|---|---|
| 효과 결정 시점 | 픽 즉시 고정 | 픽 시점 스냅샷 | 매 tick 조건 감시 |
| 대상 | 단일 종 | 전체 (스케일링) | 복수 종 조합 |
| 상태 유지 보상 | 없음 | 없음 | **있음 (ON/OFF)** |

| 카드 | 감시하는 상태 | 생성되는 전략 목표 |
|---|---|---|
| 탱크-딜러 협약 | 탱커 종 AND 딜러 종 동시 생존 | 교체 자제 + 구성 다양성 유지 |
| 유틸-딜러 공조 | 둔화 유틸(Plague/Spider) 생존 | 저HP 유틸을 보호하여 딜러 성능 유지 |
| 군단의 방패 | 총 생존 수 ≥10 | 소환 중심 수량 빌드에 방어 시너지 |

**이 테마를 선택한 이유 — QA 데이터 기반**:
- QA 6차 §3.4: 전략 분산 span 1.9%로 전략 수렴. 현재 카드 효과가 구성(전략)과 독립적이어서 어떤 빌드를 해도 비슷한 결과가 나오는 문제.
- 이 3장은 각각 구체적 "구성 목표"를 부여해 전략 분기를 발생시킨다 → 전략 다양성 지수 향상 기대.
- QA 6차 §3.3: IronWill 19%, Frenzy 22% 등 버프 카드들이 낮은 픽률 기록. 이 카드들이 "패시브처럼 상시 작동하는" 버전을 제공해 동일 효과를 더 매력적인 형태로 재제시.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **우선 채택 추천 순서**: 3장 모두 MonsterBuffService.Tick() 확장 패턴이 동일 → 동시 구현 권장. 구현 비용 모두 2로 동일해 1일 내 초안 가능.
- **밸런스 검증 우선순위**: 군단의 방패(QA 데이터로 발동 빈도 추정 가능, 수치 검증 용이) → 탱크-딜러 협약(조건 빈도 측정 필요) → 유틸-딜러 공조(유틸 생존 시간 측정 필요).

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임의 카드들은 리퍼를 강하게 만들거나, 위스프를 더 소환하거나, 영웅에게 독을 퍼붓는 식으로 각자 혼자 작동합니다. 오늘 제안하는 카드 3장은 마치 "팀워크 보너스"처럼, 특정 몬스터들이 **함께 살아있는 동안에만** 효과가 켜지는 새로운 방식입니다 — 탱커와 딜러가 둘 다 살아있을 때 전체 공격력이 오르고, 독 뿌리는 몬스터가 살아있는 동안 근접 딜러들이 더 빠르게 공격하는 식입니다. 한 마리가 죽으면 보너스가 꺼지기 때문에, 단순히 더 강한 몬스터로 바꾸는 대신 "우리 팀 구성을 유지하자"는 새로운 전략 목표가 생깁니다. 그래서 오늘 제안하는 카드 3장은: "탱크-딜러 협약"(탱커와 딜러가 함께 있을 때 전체 공격력 +25%), "유틸-딜러 공조"(독/둔화 유틸이 살아있을 때 딜러 공격속도 +35%), "군단의 방패"(10마리 이상 무리가 유지될 때 영웅 공격에 -30% 피해 감소)입니다.
