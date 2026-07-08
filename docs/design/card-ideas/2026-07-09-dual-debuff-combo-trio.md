# Card Ideas — 2026-07-09 — 동시 상태이상 콤보 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 영웅에게 두 가지 특정 상태이상이 동시 활성일 때만 발동하는 AND 콤보 메커니즘 3종 — PanicBleed / WeakenedMire / BrokenWill
- **목록**: PanicBleed(공황 출혈) / WeakenedMire(약화의 수렁) / BrokenWill(의지 붕괴)
- 기존 28장 + git log 과거 40+회차와의 중복 회피 확인됨
  - 06-25 "저주 동반자" 3종(FearCharge·WoundedFrenzy·WeakenedPrey): 단일 저주 활성 조건 → 본 카드들은 **두 저주 동시** AND 조건으로 완전히 다른 레이어
  - 06-04 "HuntersInstinct": Slow 단일 조건 → DPS 추가 피해. 본 테마는 두 저주 쌍 AND 조건

---

## 1. PanicBleed — 공황 출혈

- **카테고리**: 패시브 추가 (Add)
- **효과 모델**: 영웅에게 Fear(공포)와 Bleed(출혈)가 **동시에 활성** 상태인 동안, Bleed 출혈 피해가 ×2.0 으로 증폭된다. 두 상태이상 중 하나라도 해제되면 배율 즉시 복귀. 영구 패시브.
  - 수치: Bleed tick damage multiplier = 2.0 (Fear AND Bleed 동시 활성 구간에만 적용)
  - 기본 Bleed: 이동 시 HP -4%/s → 동시 활성 중 -8%/s
  - Fear 도주(이동 강제) 중 출혈 가속 → "공포에 떨며 피가 쏟아지는" 내러티브 일치
- **구현 패턴**: `IBattleContext.OnHeroStatusChanged` (또는 `IHeroAura` 상태이상 변경 이벤트) 구독 → `heroAura.HasFear && heroAura.HasBleed` 조건 평가 → `true`이면 `BleedEffect` 내부 `_damageMultiplier = 2.0f`, `false`이면 `1.0f` 복귀. BleedEffect 에 `float DamageMultiplier` 프로퍼티 노출 필요 (~3줄 추가).
- **시너지 후크**:
  - Fear(기존 카드) + Bleed(기존 카드) 이미 덱에 있어야 발동 → 3카드 빌드 요구
  - Debuff 축 Tier3 (저주 7장) 달성 시 PanicBleed 가 자동 상시 발동 확률 급증
  - HeroPoisonAura(독 지속) + 이 카드 → Fear+Bleed+Poison 삼중 저주 시너지 가능
  - Weaken(공격력 감소) 추가 시 영웅의 반격력도 낮아져 공황 효과 극대화
- **구현 비용 추정**: 2 (BleedEffect multiplier 프로퍼티 추가 + 상태이상 이중 체크 구독 — 이벤트 패턴은 HeroPoisonAuraEffect 기존 패턴 재사용)
- **중복 재검증**: 기존 Bleed 카드는 "Bleed 효과 적용" 자체. FearCharge(06-25)는 "Fear 활성 → 몬스터 이속/공속 강화"로 영웅에 걸린 Bleed 와 연관 없음. PanicBleed 는 "두 저주의 AND 조건 → 한 저주의 피해 증폭"이라는 콤보 레이어 — 어느 회차도 이 구조 없음.

---

## 2. WeakenedMire — 약화의 수렁

- **카테고리**: 패시브 환경 (Environment)
- **효과 모델**: Slow(이동속도 감소)와 Weaken(공격력 감소)이 **동시에** 영웅에게 활성된 동안, Plague 스포너의 순간 출력이 조건부 +1 증가한다. 두 상태이상 중 하나라도 해제되면 즉시 -1 복귀. 영구 패시브.
  - 수치: Plague Spawner.ActiveOutputBonus = +1 (Slow AND Weaken 동시 활성 구간에만)
  - "느리고 약해진 영웅" = "수렁에 빠진 상태" → Plague 가 집중 공략
  - 07-02 TwinSpawn(무작위 스포너 영구 +1)과 달리 **조건부·일시적** 증폭으로 리스크/리워드 곡선 차별화
- **구현 패턴**: `IBattleContext.OnHeroStatusChanged` 구독 → `heroAura.HasSlow && heroAura.HasWeaken` 평가 → Plague `Spawner` 의 조건부 출력 계수 `_conditionalOutputBonus`(int) 를 +1/-1 토글. 스폰 루프 내 `SpawnCount = 1 + _conditionalOutputBonus` 처리. SpawnerHasteEffect / 07-02 TwinSpawn 의 Spawner 접근 경로 재사용.
- **시너지 후크**:
  - Slow(기존 카드) + Weaken(기존 카드) + WeakenedMire = 3카드로 Plague 스포너 상시 +1 출력
  - PlagueSlowBoost(Plague 히트 시 Slow 재적용)와 조합 → Slow 지속 → WeakenedMire 지속 발동 루프
  - SpawnPlagues(즉시 Plague 4마리 스폰)와 중첩 시 순간 필드 압박 가중
  - Debuff 축 Tier2(저주 5장) 달성 → Slow+Weaken 항시 유지 가능성 → WeakenedMire 거의 상시
- **구현 비용 추정**: 2 (Spawner 조건부 출력 보너스 필드 추가 + 이중 상태이상 이벤트 구독 — Spawner 접근은 SpawnerHasteEffect 패턴 재사용)
- **중복 재검증**: PlagueSlowBoost 는 "Plague 히트 시 Slow 강도 증가". WeakenedMire 는 "두 저주 동시 → Plague 스포너 출력 조건부 증가". 06-04 HuntersInstinct 는 "Slow 단일 조건 → DPS 피해 +60%"로 대상 축이 다름. 07-02 SacrificedSpawner 는 "스포너 1개 희생 → 나머지 가속"으로 완전히 다른 구조. 어느 회차도 "Slow AND Weaken 동시 조건 → Plague 스포너 출력 조건부 부스트"는 없음.

---

## 3. BrokenWill — 의지 붕괴

- **카테고리**: 액티브 (Active)
- **효과 모델**: 발동 시 현재 영웅에게 활성화된 상태이상(디버프) 수 × 영웅 최대HP 3% 즉발 피해를 가한다. 상태이상이 없으면 0 피해, 1개 = -3%, 2개 = -6%, 3개 = -9%, 4개(Fear+Bleed+Slow+Weaken) = -12%.
  - 수치: 즉발 피해 = `activeDebuffCount × 0.03 × hero.MaxHP`
  - 발동 타이밍(30초 액티브 픽)에 영웅 상태이상 숫자가 많을수록 폭발적 즉발 피해
  - "의지가 무너지면 몸도 무너진다" — 저주 누적 → 즉각 응징
- **구현 패턴**: `IBattleContext.GetActiveHeroDebuffCount()` (또는 `IHeroAura` 상태이상 열거 카운트) → `count × 0.03 × heroHealth.MaxHP` 값으로 `heroHealth.TakeDamage(amount)` 즉발 호출. 단발성 Apply 로 구현 — MarkOfDeathEffect(기존 즉발 피해 패턴) 재사용. `GetActiveHeroDebuffCount()` 가 `IBattleContext` 에 없으면 Fear/Bleed/Slow/Weaken 각 `HasX` bool 합산으로 대체 (~4줄).
- **시너지 후크**:
  - Fear + Bleed + Slow + Weaken 모두 걸린 상태에서 BrokenWill 발동 시 -12% MaxHP 즉발 → 4분 누적 시 영웅 HP 대폭 삭감
  - PanicBleed + BrokenWill: Fear+Bleed 조합으로 PanicBleed 출혈 2배 + BrokenWill -6% 즉발 이중 시너지
  - TimeStop(전투 정지, 기존 카드)과 타이밍 조합: TimeStop으로 영웅 상태이상 장기 누적 → BrokenWill 로 즉발 피해 극대화
  - Debuff 축 특화 빌드의 "딜링 피니셔" 액티브 카드 역할 수행
- **구현 비용 추정**: 2 (MarkOfDeathEffect 패턴 재사용 + 상태이상 카운트 로직 — 신규 IBattleContext API 없이 HasX bool 합산으로 구현 가능)
- **중복 재검증**: MarkOfDeath(기존)는 "즉발 -15% MaxHP 고정". BrokenWill 은 "상태이상 수 × 3% — 0에서 12%까지 가변". BloodThirst(기존)는 "처치 시 HP 회복" 방향이 반대. 06-09 KillEcho 계열은 "처치 누적 → 버프/피해" 구조로 트리거가 다름. "활성 디버프 카운트 × MaxHP% 즉발 피해" 구조는 어느 회차에도 없음.

---

## 4. 공통 테마 고찰

세 카드 모두 **"상태이상 조합 AND 조건"** 이라는 새 레이어를 정의한다. 기존 Debuff 축 카드들이 단일 상태이상의 "적용(Apply)" 또는 "단일 조건 반응(Single Condition)"에 집중한 것과 달리, 이 세 카드는 두 개 이상의 상태이상이 **동시 활성화됐을 때만** 발동하거나 극대화된다.

**왜 이 테마인가?** 현재 Debuff 축은 Fear · Bleed · Slow · Weaken · HeroPoisonAura · HeroAttackDown 등 다양한 상태이상 카드가 존재하지만, 그것들을 "조합"하는 **메타-레이어 카드**가 없었다. 각 상태이상을 독립 수집하는 게 아닌, 특정 두 개를 함께 유지해야 폭발적 시너지가 터지는 "콤보 덱" 빌드를 처음으로 가능하게 한다.

**세 카드가 다루는 차원**:
- **PanicBleed**: 두 저주 → 한 저주의 피해 배율 증폭 (Bleed×2)
- **WeakenedMire**: 두 저주 → 스포너 출력 조건부 증가 (Plague+1)
- **BrokenWill**: 모든 활성 저주 카운팅 → 누적 배율 즉발 피해 (0~12%)

특히 BrokenWill 은 "저주를 많이 걸수록 즉발 피해가 세진다"는 스케일링 구조로, Debuff 특화 빌드를 완성하는 딜링 피니셔 역할을 한다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- `IBattleContext` / `IHeroAura` 에 `HasFear` / `HasBleed` / `HasSlow` / `HasWeaken` bool 접근자 노출 여부를 gameplay-programmer 와 선확인 권장 (현재 HeroPoisonAuraEffect 참조 패턴 확인 필요)
- PanicBleed(비용 2) → WeakenedMire(비용 2) → BrokenWill(비용 2) 순으로 구현 권장
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 Project Lair 에서 영웅에게 "저주"를 거는 카드들(공포·출혈·둔화·약화 등)은 각자 독립적으로 작동한다. 오늘 제안하는 3장은 **두 개의 저주가 동시에 걸려있을 때 비로소 터지는 콤보 카드**다.

첫 번째는 공포에 떨면서 동시에 출혈 중인 영웅의 피가 두 배로 빠르게 쏟아지는 카드, 두 번째는 느리고 약해진 영웅을 보면 역병 몬스터들이 더 많이 쏟아져 나오는 카드, 세 번째는 영웅에게 걸린 저주가 많을수록 한 번에 더 큰 타격을 주는 "의지 붕괴" 액티브 카드다. 각 저주 카드 하나하나는 이미 있었지만, 그것들을 "같이 써야 강해지는 조합 덱"의 핵심 연결고리가 오늘 처음 생기는 것이다.
