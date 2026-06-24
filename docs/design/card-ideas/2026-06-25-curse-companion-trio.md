# Card Ideas — 2026-06-25 — 저주 동반자: 세 저주에 쌍을 이루는 증폭 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 저주 동반자 (Curse Companion) — 기존 액티브 저주 3장(Fear·Bleed·Weaken) 각각에 대해 "그 저주가 활성화된 동안" 발동하는 보조 카드를 1장씩 짝지어 제안. 이 카드들은 단독으로 약하거나 폴백 효과만 주지만, 짝이 되는 저주와 세트로 운용하면 강한 콤보를 형성한다.
- **목록**: 공포 돌격 (FearCharge) / 상처 광분 (WoundedFrenzy) / 약해진 먹잇감 (WeakenedPrey)
- **기존 28장 + git log 과거 15회차 (2026-06-08 ~ 2026-06-24) + 폴더 내 이전 파일(2026-05-28 ~ 2026-06-07) 모두와의 중복 회피 확인됨**
  - **기존 28장**: Fear 상태 중 몬스터 이속/공속 강화 카드 없음 ✅ / Bleed 중 발동하는 카드 없음 ✅ / Weaken 중 Plague 연동 없음 ✅
  - **2026-06-04 HuntersInstinct**: 영웅 둔화(Slow) 중 딜러 추가 피해 — 상태이상 종류(Slow vs Fear/Bleed/Weaken)와 효과 축(공격 히트 당 추가피해 vs 지속 이속·공속·스폰) 모두 다름 ✅
  - **2026-06-15 CurseResonance**: 현재 활성 디버프 지속시간 +8초 연장 — 오늘 제안은 "디버프 활성 중 추가 효과 발동"이지 "지속시간 연장"이 아님 ✅
  - **2026-06-04 VenomStrike**: 리퍼 공격 명중 시 독 재부착 — 출혈(Bleed) 조건부 버프와 완전 다름 ✅
  - **2026-06-05 CurseOfTime**: 30초마다 누적 이속 스택 — 타이머 기반이지 디버프 상태 조건 아님 ✅

---

## 1. 공포 돌격 (FearCharge) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영구 조건부 패시브. 영웅이 **Fear(공포) 상태로 도주 중인 동안** (Fear 카드 효과 지속 3초), 모든 몬스터의 이동속도 ×1.4 + 공격 쿨다운 ×0.85 (=공속 +약 18%) 동시 적용.
  - Fear 종료 시(3초 경과 또는 영웅 HP 0%) 즉시 해제. Fear가 다시 발동되면 즉시 재활성.
  - **수치 근거** (컨셉 §8 2~4분 사망 기준):
    - Fear 3초 간 이속 ×1.4 효과: 도주 방향 영웅을 약 40% 빠르게 추격 → 도주 거리 단축 → 도주 3초 동안 받는 공격 횟수 증가 추정 +20~35%
    - 공속 +18%: Fear 3초 동안 리퍼(쿨 1s) 기준 0.5회 추가 타격 (2.5회 → 3회). 영웅에게 40 DPS × 0.5 = 추가 20 HP 피해.
    - Fear 1픽 당 기대 추가 피해: 이속+공속 복합으로 약 +25~40 HP (영웅 HP 1000의 2.5~4% 추가 감소)
    - 액티브 Fear는 최대 9번 픽 가능 — FearCharge 1장 + Fear 다수 픽 시 회차당 기대치 복리 누적
- **구현 패턴**:
  ```
  FearChargeEffect.Apply →
    IBattleContext.OnHeroFearChanged += OnFearStateChanged

  OnFearStateChanged(bool isFearing):
    if (isFearing)
      MonsterBuffService.SetConditionalBuff("FearCharge",
        moveSpeedMul: 1.4f, atkCooldownMul: 0.85f)
    else
      MonsterBuffService.ClearConditionalBuff("FearCharge")
  ```
  `IBattleContext.OnHeroFearChanged` 이벤트가 미노출이면 FearEffect 의 `Apply`/`Expire` 시점에 이벤트 발화 1줄 추가로 해결. `SetConditionalBuff` / `ClearConditionalBuff` 는 이름 키 기반으로 중복 등록 방지.
- **시너지 후크**:
  - **Fear(A, Debuff 축)** — 필수 짝. Fear 없이 픽하면 효과 없는 사실상 공란 카드. "Fear + FearCharge 세트"로 활용해야 의미가 있음.
  - **Bleed(A)**: Bleed 10초 + Fear 3초 중첩 구간(Fear 발동 즉시) → FearCharge 활성 동안 출혈+추격 동시 작용.
  - **Debuff Tier1** (Plague SlowFactor ×0.8): 평소 영웅이 둔화 → 도망 속도 감소 → Fear 3초 중 추격 효율 추가 향상.
  - **PanicStampede(2026-06-19 제안)**: Fear 중 영웅 주변 즉시 소환 카드와 이속+공속 강화를 복합할 경우 공포 구간 폭딜 극대화.
- **구현 비용 추정**: 3 (IBattleContext.OnHeroFearChanged 이벤트 노출 + SetConditionalBuff 패턴 신규. 폴링 없이 이벤트 구독으로 처리하므로 Update 비용 없음)
- **중복 재검증**: 기존 28장 중 Fear 상태를 조건으로 삼는 카드 없음 ✅. 2026-06-04 HuntersInstinct는 Slow 조건 + 딜러 단발 추가피해 — 조건(Fear vs Slow), 효과 방식(지속 버프 vs 히트 당 단발), 대상(전체 vs 딜러만) 세 축 모두 다름 ✅

---

## 2. 상처 광분 (WoundedFrenzy) — 가칭

- **카테고리**: 액티브 버프 (Debuff 축)
- **효과 모델**:
  - 픽 즉시 **조건 분기**:
    - **영웅이 Bleed(출혈) 활성 상태**: ① 전체 몬스터 Power ×1.4, 12초 ② 현재 활성 중인 Bleed 효과의 HP 감소율 ×1.5 즉시 증폭 (남은 지속시간 동안 유지).
    - **영웅이 Bleed 상태 아닐 때 (폴백)**: 전체 몬스터 Power ×1.15, 8초만.
  - **수치 근거**:
    - 기본 Bleed: 이동 시 1s당 HP -2%, 10초 → 이동 거리 비례 최대 -20% HP
    - WoundedFrenzy 강화 Bleed: 이동 시 1s당 HP -3%, 남은 시간 → 약 10% 추가 HP 피해
    - Power ×1.4, 12초: 리퍼(40) → 56 DPS, 헥스(30) → 42 DPS. 12초간 리퍼 2마리 × 56 = 1344 잠재 피해. 영웅 HP 1000 대비 이론상 치사 가능 구간 (실제는 이동·회피로 완전 히트 불가하므로 조정 필요)
    - 폴백 ×1.15, 8초: Frenzy(공속 +50%, 10초)보다 훨씬 약함 — "조건 달성이 핵심" 구조 유지
    - 밸런스 체크: Bleed + WoundedFrenzy 강화 버전이 Frenzy(공속 +50%) + Bleed 기본 조합과 비슷한 수준이도록 조정. Frenzy = 공속 기반 피해 증가이고 WoundedFrenzy = Power 기반 피해 증가이므로 스택 방식이 달라 중복 픽 상승폭 다름.
  - **Debuff Tier3 시너지**: Tier3(영구 출혈 등록, 이동 시 HP -1%)이 활성화되면 항상 Bleed 상태 → WoundedFrenzy가 항상 강화 버전으로 발동 보장.
- **구현 패턴**:
  ```
  WoundedFrenzyEffect.Apply(IBattleContext ctx):
    bool isBleeding = ctx.IsHeroBleeding()

    if (isBleeding)
      MonsterBuffService.ApplyGlobalPowerBuff(1.4f, 12f)
      BleedEffect activeBleed = ctx.GetActiveHeroEffect<BleedEffect>()
      if (activeBleed != null)
        activeBleed.MultiplyDamageRate(1.5f)   //# 남은 기간 동안만 ×1.5
    else
      MonsterBuffService.ApplyGlobalPowerBuff(1.15f, 8f)
  ```
  `ctx.IsHeroBleeding()` — `HeroAura` 내 BleedEffect 활성 여부 조회. `ctx.GetActiveHeroEffect<T>()` — 기존 오라 리스트 generic 조회 (미노출 시 1메서드 추가). `activeBleed.MultiplyDamageRate` — BleedEffect 에 `damageRateMultiplier` 필드 추가 (기본 1.0f) 후 Apply 내부에서 곱연산.
- **시너지 후크**:
  - **Bleed(A, Debuff 축)** — 필수 짝. Bleed 픽 후 WoundedFrenzy 픽 = "출혈 + 광분" 세트 콤보.
  - **Debuff Tier3(영구 출혈)**: 영구 출혈로 항상 강화 버전 보장 → WoundedFrenzy 중반 픽 시 매 30초마다 Power ×1.4, 12초 반복 가능.
  - **FearCharge(카드 1)**: Fear 도주 3초 동안 이속+공속 강화. 만약 Fear + Bleed + WoundedFrenzy 세 카드를 조합하면 Fear 발동 순간 → 전체 이속 ×1.4 + Power ×1.4(WoundedFrenzy 기발동 중) = 3초 집중 폭딜 구간.
  - **MarkOfDeath(A, Dps 축)**: 영웅 받는 데미지 ×1.5, 5초. WoundedFrenzy Power ×1.4와 중첩 구간(5초)에서 DPS 최대 1.4×1.5 = ×2.1 달성 가능 — 크로스 축 콤보.
- **구현 비용 추정**: 4 (BleedEffect.MultiplyDamageRate 신규 API + ctx.GetActiveHeroEffect\<T\>() 신규. 기존 IronWillEffect·GuardianRageEffect 패턴 참조로 구조는 명확하나 BleedEffect 내부 수치 변경 경로가 새로움)
- **중복 재검증**: CurseResonance(2026-06-15) = 디버프 지속시간 +8초 연장 — WoundedFrenzy는 출혈 DPS 배율 변경이지 지속시간이 아님 ✅. VenomStrike(2026-06-04) = 리퍼 공격 연동 독 재부착 — 출혈 상태 조건부 Power 버프와 트리거·효과 모두 다름 ✅

---

## 3. 약해진 먹잇감 (WeakenedPrey) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영구 조건부 패시브. 영웅이 **Weaken(약화) 활성 상태** (Weaken 카드 효과 지속 10초) 동안, Plague 종 전체의 이동속도 ×1.3 + 모든 Plague Spawner 스폰 주기 ×0.8 (임시, Weaken 해제 시 원복).
  - Weaken 종료 시(10초 경과) 즉시 해제. Weaken 재발동 시 즉시 재활성.
  - **수치 근거**:
    - Plague 기본 이속: "공격 시 영웅 둔화 20%" 특성으로 이속 자체는 설정 기본값. ×1.3이면 영웅 포위 도달 시간 단축.
    - Plague Spawner 스폰 주기 ×0.8 (임시): SpawnerHaste 카드(전체 주기 ×0.8 영구)의 "Plague 한정 임시 버전". 10초 동안 Plague 스폰 수 기대치 +25% 증가.
    - Weaken 10초 + WeakenedPrey → 10초 동안 Plague 가속 스폰 + 이속 강화. Plague(HP 50, DPS 5)는 약하지만 수로 압박하고 둔화 누적 효과가 핵심.
    - Plague SlowFactor ×0.75(PlagueSlowBoost) + Weaken(영웅 ATK ×0.5) 동시 활성 중 WeakenedPrey → 영웅이 느리고 약한 상태에 Plague 홍수 = Debuff 축 극한 시나리오.
    - 평균 빌드(2~4분 사망) 기준 Weaken 최대 9픽 → 10초 × 9회 = 누적 90초 WeakenedPrey 활성 가능 (실제는 Weaken 픽 수에 의존).
- **구현 패턴**:
  ```
  WeakenedPreyEffect.Apply →
    IBattleContext.OnHeroWeakenChanged += OnWeakenStateChanged

  OnWeakenStateChanged(bool isWeakened):
    if (isWeakened)
      MonsterBuffService.SetConditionalBuff("WeakenedPrey_Speed",
        monsterType: EMonster.Plague, moveSpeedMul: 1.3f)
      SpawnerService.SetTemporaryHaste(EMonster.Plague, 0.8f)
    else
      MonsterBuffService.ClearConditionalBuff("WeakenedPrey_Speed")
      SpawnerService.ClearTemporaryHaste(EMonster.Plague)
  ```
  `IBattleContext.OnHeroWeakenChanged` — WeakenEffect 의 Apply/Expire 시점에 발화. `SpawnerService.SetTemporaryHaste` — Spawner 별 임시 배율 재정 (SpawnerHaste 카드의 영구 배율과 병존, 곱연산 누적). FearCharge(카드 1)와 동일한 이벤트 구독 패턴으로 폴링 없이 경량 처리.
- **시너지 후크**:
  - **Weaken(A, Debuff 축)** — 필수 짝. Weaken 없이 픽하면 효과 발동 없음.
  - **PlagueSlowBoost(P) + SpawnPlagues(P)**: Plague 심화 빌드에 WeakenedPrey 추가 시 Weaken 발동 순간 Plague 스폰 폭증 + 이속 증가로 "Plague 홍수" 구간 형성.
  - **HeroPoisonAura(P)**: 영웅 발 밑 독 장판 + Weaken(ATK 반토막) 중 Plague 가속 스폰 → 독 장판을 밟으면서 약해진 영웅 주변에 Plague 무리 집결.
  - **Debuff Tier2** (HeroAttackDown 자동 등록, 영웅 ATK ×0.85 영구): 기반 ATK 감소 상태에서 Weaken(×0.5) 중첩 → ATK ×0.85 × 0.5 = ×0.425. WeakenedPrey는 이 10초 동안 Plague로 집중 피해를 넣는 최적 타이밍.
  - **FearCharge(카드 1)**: Weaken과 Fear를 동시 활성화하면 FearCharge(이속+공속) + WeakenedPrey(Plague 가속) 동시 발동 → 전체 몬스터 이속 + Plague 특화 이속의 이중 버프.
- **구현 비용 추정**: 3 (SpawnerService.SetTemporaryHaste 신규 API. FearCharge 카드 1의 이벤트 구독 패턴 공유로 구조 비용 감소. OnHeroWeakenChanged = WeakenEffect 수정 1줄)
- **중복 재검증**: 기존 28장 중 Weaken 상태를 트리거로 삼는 카드 없음 ✅. 2026-06-23 PlagueAtkSpeedBoost = Plague 공속 영구 강화 — WeakenedPrey는 임시(Weaken 10초 한정) + 이속+스폰 복합이지 공속이 아님 ✅. SpawnerHaste = 전체 스포너 영구 가속 — WeakenedPrey는 Plague 한정 임시 가속 ✅

---

## 4. 공통 테마 고찰

세 카드는 **기존 Debuff 축 액티브 저주 3장(Fear·Bleed·Weaken)에 각각 동반 카드를 짝지어 "2장 세트 콤보"를 완성**한다는 단일 설계 철학으로 묶인다.

**왜 오늘 이 테마인가?**
- 기존 Debuff 축 카드의 구성 현황:
  - 패시브: PlagueSlowBoost·SpawnPlagues·HeroPoisonAura·HeroAttackDown (4장)
  - 액티브: Fear·Bleed·Weaken (3장)
  - 빌드 패턴: 액티브 저주를 걸고 패시브로 Plague를 강화하는 "군집 독 압박" 라인이 주력.
- **문제**: Fear·Bleed·Weaken 세 저주는 서로 조합해서 "더 강해지는" 구조가 없음. 각각 독립적으로 발동하고 종료된다. 저주 간 혹은 저주와 다른 카드 간 연계가 CurseResonance(지속시간 연장) 한 장뿐.
- **오늘 제안**: 각 저주마다 "발동 중일 때 쌍으로 강해지는" 보조 카드를 만들어, Debuff 축 내 "저주 세트 픽" 전략 라인을 신설. 플레이어가 "Fear + FearCharge 세트" 또는 "Bleed + WoundedFrenzy 세트"를 의식적으로 조합하는 의사결정 레이어 추가.
- **QA 데이터**: 최신 QA 리포트(2026-05-22)가 BLOCKED 상태라 픽률 데이터 없음. 구조 공백 분석(각 저주의 동반 강화 카드 부재)을 근거로 삼음.

**구현 패턴 공통화 기회**: FearCharge와 WeakenedPrey 모두 `IBattleContext.OnHero[State]Changed` 이벤트 구독 패턴을 공유한다. FearCharge 구현 시 이 패턴을 내부 표준으로 정립하면 WeakenedPrey 구현 비용이 자연히 감소한다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- 세 카드가 세트이므로 단독 채택보다 3장 묶음 채택 권장 (짝 카드가 없으면 효과 없는 카드가 발생)
- 선행 과제: `IBattleContext.IsHero[State]()` 계열 및 `OnHero[State]Changed` 이벤트 API 표준화 → FearCharge·WeakenedPrey 동시 구현 시 공통 인프라 1회 설계로 양 카드 적용
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임엔 영웅을 겁주거나(공포), 상처를 내거나(출혈), 힘을 빼놓는(약화) 저주 카드가 있어요. 그런데 이 저주들은 그냥 걸어두기만 할 뿐, 저주가 걸려있는 동안 "추가로 더 나쁜 일"이 일어나지는 않죠.

오늘 제안하는 카드들은 각 저주의 "짝꿍" 역할을 합니다. 영웅이 겁을 먹고 도망치는 동안 몬스터들이 더 빨리 달려가 더 많이 때리고, 영웅이 피를 흘리는 동안 모든 몬스터의 공격력이 폭발적으로 오르고, 영웅이 약해졌을 때 독 몬스터 떼가 순식간에 밀려드는 식이에요. 저주 하나를 걸고 짝꿍 카드를 함께 고르면, 저주가 걸린 10초가 단순한 10초가 아니라 집중 폭격 시간이 됩니다.

그래서 오늘 제안하는 카드 3장은: 겁(공포)·상처(출혈)·약화(힘 빼기) 저주를 각각 몇 배로 무섭게 만들어주는 "저주 증폭기" 세트입니다.
