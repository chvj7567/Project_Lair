# Card Ideas — 2026-06-30 — 피격 분노 임계 3종 (죽어가는 몬스터가 역으로 강해진다)

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요
- 테마: 몬스터 개체 HP 가 임계치 이하로 내려가는 순간 해당 개체가 자체 분노 — "죽이면 오히려 위험해진다" 역설 압박
- 목록: LastRoar (Tank 패시브) / DeathRattle (Dps 패시브) / BloodRage (와일드 액티브)
- 기존 25장(실제 28장) + 17회차 git log 과거 루틴과의 중복 회피 확인됨

**중복 회피 근거:**
- 기존 28장은 모두 "픽 순간 즉시 글로벌 영구 버프" 또는 "액티브 발동 시 전체/특정 종 일시 버프" 패턴
- 과거 루틴 선도자 오라(06-22): "다른 종이 필드에 살아있을 때 아군 강화" → 조건이 외부 존재 여부
- 과거 루틴 공격 반격 패널티(06-14): "영웅이 공격하는 이벤트 시 반격" → 조건이 히트 이벤트
- 과거 루틴 영웅 저체력 포식자(06-19): "영웅 HP가 낮아질 때 몬스터 강화" → 조건이 영웅 HP
- **이번 제안**: "몬스터 자신의 HP% 가 임계 이하로 처음 내려가는 순간 해당 개체만 자체 분노" → 신규 조건 축

---

## 1. LastRoar — 탱커의 최후 포효 (가칭)

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - 대상 종: Wisp / Wraith
  - 조건: 해당 개체 HP 가 **40% 이하로 처음 내려가는 순간** 즉시 발동
  - 효과: 해당 개체 공격력 ×2.5 + 이동속도 ×1.5 (잔여 생존 기간 동안 영구 유지)
  - 발동 한정: 개체당 1회 (이미 분노한 개체는 재발동 없음, 내부 EnrageTag 플래그로 관리)
  - 밸런싱 근거: Wisp 기본 DPS 10 → 분노 시 25. Wraith 기본 DPS 20 → 분노 시 50. 영웅 HP 잔여 60% 수준에서 최초 발동하면 약 15초 이내 추가 타격 상당.
- **구현 패턴**:
  - MonsterBuffService 에서 `IMonsterHpChangedEvent` 구독
  - 대상 종 필터 → `hpPercent <= 0.4f && !monster.HasTag("EnrageLastRoar")` 체크
  - 통과 시 `monster.SetTag("EnrageLastRoar")` + `MonsterBuffService.ApplyPermanentBuff(attackMul: 2.5f, speedMul: 1.5f)`
  - IBattleContext 패턴 안에서 자연스럽게 구현 가능
- **시너지 후크**:
  - Tank Tier1 (Wisp·Wraith HP ×1.3) → 더 두꺼운 HP 위에서 분노 → 분노 상태 지속 시간 연장
  - IronWill (받는 데미지 ×0.7, 15s) 발동 중 분노 Wraith → 받는 데미지 감소 + 공격력 2.5배 동시 → 순간 압박 급증
  - GuardianRage (Wisp·Wraith HP ×2.0 + 받는 데미지 ×0.5, 15s) + LastRoar → 체력 2배 상태에서 임계 40% = 기본 HP 기준 80%가 남았을 때 분노 → 분노 유지 시간이 매우 길어짐
- **구현 비용 추정**: 2 (HP 변경 이벤트 구독 패턴 신규 추가 + EnrageTag 플래그 1개, 기존 MonsterBuffService 확장 최소)
- **중복 재검증**: 기존 WispHpBoost / WraithDamageBoost 는 "픽 즉시 글로벌 HP 배율". LastRoar 는 "개체 HP 임계 도달 시 해당 개체만 자체 분노" — 조건·범위·시점 모두 다름.

---

## 2. DeathRattle — 딜러의 임종 독화살 (가칭)

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - 대상 종: Reaper / Hex
  - 조건: 해당 개체 HP 가 **30% 이하로 처음 내려가는 순간** 즉시 발동
  - 효과:
    - Reaper: 공격 쿨다운 ×0.4 + 이동속도 ×1.8 (죽기 전 극초고속 돌진)
    - Hex: 사거리 ×2.0 + 공격 쿨다운 ×0.5 (죽기 전 초원거리 속사)
  - 발동 한정: 개체당 1회 (EnrageTag "DeathRattle")
  - 밸런싱 근거: Reaper 기본 쿨 1초 → 0.4초 (250% 공속), 이동속도 ×1.8 → 영웅 기준 속도 대비 빠른 추격. Hex 기본 사거리 ×2.0 → 화면 절반 이상 커버, 쿨다운 ×0.5 → 초당 2발. 지속 시간이 짧은 대신 (개체 생존 기간 한정) 극단 수치.
- **구현 패턴**:
  - LastRoar 와 동일 HP 변경 이벤트 구독 패턴
  - 대상 종별 다른 버프 파라미터 분기 적용
  - Reaper: `MonsterBuffService.ApplyPermanentBuff(cooldownMul: 0.4f, speedMul: 1.8f)`
  - Hex: `MonsterBuffService.ApplyPermanentBuff(rangeMul: 2.0f, cooldownMul: 0.5f)`
- **시너지 후크**:
  - Dps Tier1 (Reaper·Hex Power ×1.3) → 분노 Reaper 공격력 1.3배 × 쿨다운 0.4배 = 실질 DPS 3.25배
  - Dps Tier2 (Reaper·Hex Cooldown ×0.8) + DeathRattle (쿨다운 ×0.4) → 곱연산 0.32배 쿨다운 = 3.1배 공속
  - ReaperAtkSpeed (쿨다운 ×0.7) + DeathRattle (×0.4) → 분노 시 0.28배 = 초당 3.6타
- **구현 비용 추정**: 2 (LastRoar 와 같은 이벤트 구독 구조 재사용, 종별 버프 파라미터 분기만 추가)
- **중복 재검증**: ReaperAtkSpeed (픽 즉시 글로벌 쿨다운 ×0.7), HexRangeBoost (픽 즉시 글로벌 사거리 ×1.4) 와 조건·시점·범위 완전히 다름. DeathRattle 은 "개체 HP 30% 이하 시 해당 개체만" 임시 극단 버프.

---

## 3. BloodRage — 부상 군단의 피의 격노 (가칭)

- **카테고리**: 액티브 와일드
- **효과 모델**:
  - 발동 즉시 필드에 존재하는 **HP 50% 이하 개체 전부** 에게 20초간 이동속도 ×2.0 + 공격력 ×2.0
  - HP 50% 초과 개체는 영향 없음 — "아직 멀쩡한 몬스터"는 분노하지 않음
  - 중첩 가능: 이미 LastRoar / DeathRattle 분노 상태인 개체에도 시간제 ×2.0 추가 배율 적용
  - 밸런싱 근거: 영웅이 2분가량 싸워 필드 부상 몬스터가 많아진 시점(약 2:00 ~ 3:00)에 발동하면 최대 효과. 픽 직후라면 부상 개체가 적어 효과 소폭. "언제 쓰느냐"가 핵심 타이밍 판단.
- **구현 패턴**:
  - `IBattleContext.GetAllMonsters().Where(m => m.HpPercent <= 0.5f)` 로 대상 수집
  - `MonsterBuffService.ApplyTimedBuff(each, speedMul: 2.0f, attackMul: 2.0f, duration: 20f)` 일괄 적용
  - 기존 FrenzyEffect / IronWillEffect 패턴과 동일 (전체 대신 조건 필터만 다름)
- **시너지 후크**:
  - LastRoar + DeathRattle 함께 픽 시: 부상 개체가 이미 분노 상태 → BloodRage 발동 시 배율 추가 누적 → 영웅에게 극단적 압박
  - IronWill (받는 데미지 ×0.7) 와 동시 발동 시: 분노한 부상 개체들이 데미지도 덜 받고 공속도 2배 → 즉각 위협
  - Swarm Tier2 (모든 스포너 주기 ×0.85) → 빠른 스폰으로 부상 개체 풀 빠르게 확보 → BloodRage 대상 수 증가
- **구현 비용 추정**: 2 (기존 액티브 이펙트 구조 재사용, HP 필터 조건 추가 1줄)
- **중복 재검증**:
  - Frenzy (모든 몬스터 공속 +50%, 10s): 조건 없이 전체 대상
  - IronWill (받는 데미지 ×0.7, 15s): 방어 버프
  - GuardianRage (Wisp·Wraith HP ×2.0 + 받는 데미지 ×0.5, 15s): 종 한정 + 방어 집중
  - BloodRage 는 "HP 50% 이하 전 종 조건부 + 공속·공격 쌍 버프" — 조건·대상·효과 모두 신규.

---

## 4. 공통 테마 고찰

**테마: 죽어가는 몬스터가 역으로 강해진다 (Enrage-on-Threshold)**

현재 28장 카드 전부는 "픽 순간 효과 즉시 적용" 또는 "액티브 발동 시 전체/종 일시 버프" 두 패턴만 존재한다. 몬스터 개체의 실시간 HP 상태를 조건으로 하는 분기가 전혀 없다.

이 빈 공간은 플레이어 경험에 흥미로운 역설을 만든다:

> 영웅이 몬스터를 "거의 다 잡았다"고 느끼는 순간이 가장 위험한 순간이 된다.

HP 임계 분노 카드들은 영웅 처치 행위 자체에 새로운 딜레마를 추가한다:
- "저 Wraith를 지금 처치해야 하나, 분노 전 영웅이 위치를 피해야 하나?"
- "부상 몬스터가 늘어날수록 BloodRage가 더 치명적이 된다"

**오늘 이 테마를 고른 이유:**
- QA 리포트가 blocked(픽률 데이터 없음)이므로 픽률 공백 대신 카드 효과 패턴 공백 분석
- 기존 카드 중 "조건부 개체 단위 버프"가 0장인 점이 v0.2 풀 확장 시 반드시 채워야 할 빈 축
- MonsterBuffService의 OnHpChanged 이벤트 구독 패턴은 이미 IHeroAura / IBattleContext 레이어에서 검증된 확장 방식

---

## 5. 채택 흐름 제안

- **채택 시**: game-designer 호출 입력으로 이 문서를 전달, 특히 §1~2의 EnrageTag 중복 발동 방지 규칙과 §4의 역설 압박 의도를 전달
- **우선순위 제안**: LastRoar → BloodRage → DeathRattle 순서로 채택 검토
  - LastRoar: 구현 패턴 신규 도입 (HP 이벤트 구독 + 태그), 나머지 둘의 기반
  - BloodRage: 기존 액티브 이펙트 구조 재사용이라 단독으로도 빠르게 구현 가능
  - DeathRattle: LastRoar 패턴 완성 후 파라미터 분기 추가로 저비용 추가
- **v0.2 진입 전까지** backlog 보관 — MVP §11 매수 lock 해제 후 풀 확장 시 우선 후보

---

## 6. 쉬운 설명 (비개발자 요약)

지금은 우리 몬스터들이 영웅한테 얻어맞으면 그냥 쓰러져요. 영웅 입장에서는 "이 몬스터 거의 잡았다!" 싶으면 마음이 편해지죠. 그런데 오늘 제안하는 카드들을 쓰면, 몬스터가 거의 죽을 뻔한 바로 그 순간에 갑자기 분노해서 더 빠르고 더 세게 달려들어요. 마치 영화에서 악당이 쓰러지는 척하다가 벌떡 일어나는 것처럼요. 영웅이 "이제 다 됐다!" 싶을 때 오히려 가장 위험한 순간이 된다면, 플레이어 입장에서 훨씬 긴장감 있는 싸움이 됩니다. 그래서 오늘 제안하는 카드 3장은: **LastRoar(탱커 분노)**, **DeathRattle(딜러 임종 반격)**, **BloodRage(부상 군단 일제 격노)** — 죽어가는 몬스터를 가장 위험한 순간으로 바꾸는 카드들입니다.
