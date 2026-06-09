# Card Ideas — 2026-06-10 — Tank 재생·분열: 죽어도 버티는 몬스터 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: Tank 축 재생·분열 심화 — 현재 Tank 카드 7장(WispHpBoost·WraithDamageBoost·SpawnWraith·SpawnWisps·ReplaceWispsToWraith·WallOfWisps·GuardianRage)은 HP 배율 강화·수량 증가·즉시 소환·교체에 집중되어 있으나, **위스프 사망 시 분열 / 레이스 공격 시 흡혈 자가 회복 / 탱커 피해를 영웅에게 역반사**하는 "버티는 방식 다양화" 슬롯이 전혀 비어 있다. 오늘 3장이 이 공백을 채운다.
- **목록**: 위스프 분열 / 레이스 흡혈 / 거울 갑옷
- **기존 28장 + git log 과거 회차와의 중복 회피 확인됨**
  - 기존 Tank 7장은 모두 "처음부터 HP를 많이 주거나 수를 늘리는" 접근. 재생·분열·피해 반사 메커니즘 전무.
  - git log 과거 13회차 전부 검토: 독·플레이그·Dps 심화·레이스-팬텀 각성·도주 처벌·킬 카운터 등 — 어느 회차에서도 "Tank 재생/분열/반사" 개념 없음.

---

## 1. 위스프 분열 (Wisp Fission) — 가칭

- **카테고리**: 패시브 환경 (Tank 축)
- **효과 모델**:
  - 위스프가 사망할 때마다 **50% 확률**로 HP 100짜리 소위스프 1마리가 해당 위치에서 즉시 스폰.
  - 소위스프는 원래 위스프와 동일한 DPS(10)·이동속도를 갖고, HP만 절반(100).
  - **캡 체크 적용**: 현재 필드 몬스터 수가 글로벌 캡(기본 18) 이상이면 분열 억제 — 무한 증식 루프 방지.
  - 이 카드를 2회 픽하면 분열 확률 75%로 증가, 3회 픽 시 확률 87.5% (매 중첩마다 미분열 확률 절반).
  - 밸런스 근거 (컨셉 §8): 위스프 HP 200, 분열체 HP 100. 50% 확률 → 평균 기대 위스프 병력 ×1.5배. 캡 도달 시 억제로 상한 보장. 위스프 기본 DPS 10이므로 분열체 합산 평균 DPS 기여 ≈ +50% (캡 이하 구간). 2~4분 밴드 안에서 동작.
- **구현 패턴**: IBattleContext / CharacterRegistry OnDied 이벤트 구독 + CHMPool.Pop
  ```
  WispFissionService:
    Start:
      CharacterRegistry 의 Wisp 종 OnDied 구독
    OnWispDied(position):
      if (Random.value > 0.5f) return
      if (ctx.GetTotalMonsterCount() >= ctx.GetGlobalCap()) return
      CHPoolable child = CHMPool.Instance.Pop(wispPrefab, parent)
      child.GetComponent<IHealth>().SetHp(100)
      child.transform.position = position
  ```
  - BloodThirstService / (2026-05-30 제안) PlagueDeathPoisonService 패턴 거의 동일 — OnDied 이벤트 구독 구조.
  - CHMPool.Pop 사용 필수 — Object.Instantiate 금지 (Rule 03 §4).
  - `IBattleContext.GetTotalMonsterCount()` / `GetGlobalCap()` API 신규 추가 필요 (미지원 시).
- **시너지 후크**:
  - SpawnWisps + SpawnerHaste: 위스프 밀도 상승 → 사망 트리거 빈도 증가 → 분열 발생 누적.
  - Swarm Tier3 (글로벌 캡 +6, 18→24): 캡 여유가 늘어 분열 억제 빈도 감소 → 분열체 더 자주 생존.
  - WispHpBoost (HP ×1.5): 위스프가 오래 살아 사망 트리거 빈도 감소 — 역방향 트레이드오프. "바로 죽혀 분열시킬지 vs 오래 살려 탱커로 쓸지" 선택지.
- **구현 비용 추정**: 3 (OnDied 구독 + 조건부 CHMPool.Pop + IBattleContext 캡 체크 API 신규)
- **중복 재검증**: 기존 WallOfWisps = 픽 즉시 Wisp 4마리 소환 (트리거: 픽 시점). 이 카드 = 사망 시 50% 분열 (트리거: 사망 이벤트, 조건부). 트리거·조건·HP·대상 모두 다름. 과거 13회차 전부 검토 — 분열 개념 없음.

---

## 2. 레이스 흡혈 (Wraith Life Drain) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - 레이스가 영웅을 공격(타격 히트)할 때마다 레이스 자신 HP **+20 회복** (흡혈).
  - 타격이 실제 피해를 줘야 발동 — 빗나감·회피·피해 무효화 시 미발동.
  - 밸런스 근거 (컨셉 §8):
    - 영웅 공격력 50/s vs 레이스 → 레이스 흡혈 20/s → 순 피해 30/s → 생존 시간 500/30 ≈ **16.7s** (기본 500/50 = 10s 대비 +67% 연장).
    - WraithDamageBoost (HP ×1.5) 조합: 레이스 HP 750 → 750/30 ≈ **25s 생존** (기본 대비 2.5배).
    - GuardianRage (HP ×2.0, 받는 데미지 ×0.5, 15s 창) 조합: 레이스 HP 1000, 영웅 실효 피해 25 → 흡혈 20 → 순 5/s → 15s 창 내 레이스 HP 손실 75 — 사실상 전사 불가. 15s 종료 후 일반 교전 전환 → 순 30/s → 생존 925/30 ≈ 31s. 컨셉 §8 2~4분 밴드 안에서 동작.
- **구현 패턴**: Wraith OnHitHero 이벤트 구독 + IHealth.Heal
  ```
  WraithLifeDrainService:
    Start:
      CharacterRegistry 의 Wraith 종 OnHitHero 이벤트 구독
    OnWraithHitHero(wraith):
      IHealth hp = wraith.GetComponent<IHealth>()
      if (hp == null) return
      hp.Heal(20)   //# HP + 20, Max HP 캡 적용
  ```
  - BloodThirstService 구조 거의 동일 (처치 이벤트 → 인근 회복 → 이 카드는 타격 이벤트 → 자신 회복).
  - `IHealth.Heal(int amount)` 미지원 시 `SetHp(Mathf.Min(Current + amount, Max))` 로 대체.
- **시너지 후크**:
  - WraithDamageBoost + SpawnWraith + 이 카드 = "레이스 3종 세트" — 레이스가 질기고 수도 많아 Tank Tier2·3 달성 용이.
  - GuardianRage (15s 창): 창 안에서 레이스가 거의 죽지 않아 영웅 어그로를 레이스에 묶는 극한 탱킹.
  - Bleed (영웅 이동 시 HP -2%/s, 10s) 조합: 영웅이 레이스를 피해 도망가면 출혈로 사망, 공격하면 레이스가 회복 — 어느 선택도 나쁜 역설적 압박.
- **구현 비용 추정**: 2 (OnHitHero 이벤트 구독 + IHealth.Heal — BloodThirstService 패턴 재활용, 신규 패턴 없음)
- **중복 재검증**: BloodThirst = **처치 시** → **인근** 몬스터 HP +30. 이 카드 = **타격 히트 시** → **자신** HP +20. 트리거(처치 vs 타격)·대상(인근 vs 자신)·종(전체 vs Wraith) 모두 다름. 과거 13회차 — 흡혈 개념 없음.

---

## 3. 거울 갑옷 (Mirror Armor) — 가칭

- **카테고리**: 액티브 버프 (Tank 축)
- **효과 모델**:
  - **10초간** Wisp·Wraith 종이 받는 피해의 **20%** 를 즉시 영웅에게 역반사.
  - 영웅이 위스프/레이스를 때릴수록 자기 자신도 피해를 입는다.
  - 반사 피해는 최소 1 보장, 영웅 HP 1 이하로 감소시키지 않음 (반사만으로 즉사 없음).
  - 밸런스 근거 (컨셉 §8):
    - 영웅 공격력 50, 공속 1s → 10타 × 50 × 20% = **역반사 총 100 피해** (영웅 HP 10% 감소).
    - IronWill (받는 데미지 ×0.7) 함께 사용: 탱커 실효 피해 35 → 35×20% = 7 → 10s 70 역반사 (-7%). 이 조합은 반사 효과 약하지만 탱커 자체가 훨씬 오래 버팀.
    - GuardianRage (받는 데미지 ×0.5): 레이스 실효 피해 25 → 25×20% = 5 → 50 역반사 (-5%). 반사량은 줄지만 레이스가 극히 질겨 총 압박은 더 강함.
    - 영웅 최종 사망 기여: 타 몬스터 DPS + 역반사 100으로 영웅 HP -10% 가속. 2~4분 밴드 안에서 10% 가속 기여 적정.
- **구현 패턴**: 데미지 이벤트 인터셉트 훅 (10초 만료 후 자동 해제)
  ```
  MirrorArmorEffect:
    Apply(ctx):
      ctx.AddTimedDamageHook(OnTankDamaged, 10f)

  OnTankDamaged(target, amount):
    if (target is not Wisp and target is not Wraith) return
    int reflect = Mathf.Max(1, Mathf.RoundToInt(amount * 0.20f))
    IHealth hero = ctx.GetHero()
    if (hero.Current > 1)
      hero.TakeDamage(Mathf.Min(reflect, hero.Current - 1))
  ```
  - `IBattleContext.AddTimedDamageHook(callback, duration)` 신규 API 필요 — IronWillEffect 의 "받는 데미지 배율" 후킹 지점과 같은 레이어에 설치 가능하면 비용 절감.
  - 기존 MonsterHealthComponent 또는 CharacterStats 가 OnDamageTaken 이벤트를 공개하고 있는지 먼저 확인.
- **시너지 후크**:
  - WispHpBoost + SpawnWisps: 위스프가 많아 영웅이 위스프를 자주 공격 → 역반사 누적.
  - Wraith Life Drain + 이 카드: 영웅이 레이스를 때리면 레이스 HP 회복 + 영웅 HP 손실 — 이중 패널티. 영웅 입장에서 레이스 공격이 순손해.
  - GuardianRage + 이 카드: 레이스 극단적으로 질김 + 때릴수록 영웅이 다침 → 영웅이 탱커 라인을 뚫기 어려운 15s 창 형성.
- **구현 비용 추정**: 4 (데미지 이벤트 인터셉트 훅 시스템 신규 또는 MonsterHealthComponent 내 OnDamageTaken 콜백 확장 — 기존 패턴 미지원 시 가장 높은 비용. IronWillEffect 구현 구조 선 확인 권장)
- **중복 재검증**: 기존 IronWill = 몬스터 받는 데미지 ×0.7 (방어 증폭, 역반사 없음). 거울 갑옷 = 받은 피해 20%를 영웅에게 역방향 전달. 수혜 방향(몬스터 방어 vs 영웅 피해 추가) 반대. 과거 13회차 전부 검토 — 피해 반사 개념 없음.

---

## 4. 공통 테마 고찰

### 왜 오늘 이 테마인가

QA 리포트(2026-05-22)는 시뮬레이션 미실행으로 픽률 데이터가 없으나, 카드 구조 분석에서 두 가지 공백이 식별된다.

1. **Tank 축 "버티는 방식"이 단조로움**: 기존 Tank 카드 7장은 모두 "처음부터 HP를 더 주거나 수를 더 늘리는" 정적 접근. 전투 중 동적으로 HP를 회복하거나 사망 후 분열해 재등장하는 "능동적으로 버티는" 경로가 전무하다.
2. **영웅이 Tank 몬스터를 공격하는 행위 자체에 아무 결과가 없음**: 위스프/레이스를 때리면 그냥 HP가 줄 뿐, 영웅에게 어떤 반작용도 없다. 거울 갑옷은 "탱커를 때리는 것 자체가 위험해지는" 새로운 긴장을 도입한다.

### 과거 13회차와의 차별성 요약

| 과거 회차 | 핵심 개념 | 이번 트리오 |
|---|---|---|
| 독·플레이그 생태계 (2026-05-30) | Plague 사망 이벤트 → 독 DPS | Wisp 사망 이벤트 → 스폰 분열 (독 없음) |
| 레이스·팬텀 각성 (2026-06-07) | Wraith Power·Speed / Phantom OnHit 독침 | Wraith OnHit 자가 흡혈 — 스탯 증폭이 아닌 회복 |
| 종 간 연계 시너지 (2026-05-29) | 탱커·딜러 공존 조건 → 전체 DPS 버프 | 단일 Tank 종 재생·반사 — 공존 조건 없음 |
| 킬 카운터 처벌 (2026-06-09) | 처치 수 누적 → 강화 | 사망 이벤트 즉시 분열 — 누적이 아닌 즉발 |
| 위스프 방벽 포위 (2026-06-03) | 위스프 배치·포위 전략 | 위스프 사망 분열 — 배치 전략이 아닌 사망 트리거 |

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **우선순위 제안**:
  1. **Wraith Life Drain** (구현 비용 2) — BloodThirstService 패턴 직접 재활용, 즉시 착수 가능. `IHealth.Heal` 추가 시 Wisp Fission 분열체 HP 설정에도 공유.
  2. **Wisp Fission** (구현 비용 3) — OnDied + CHMPool.Pop. IBattleContext 캡 체크 API 신규 추가 후 착수.
  3. **Mirror Armor** (구현 비용 4) — 데미지 인터셉트 훅 인프라 선행 구현 후 착수. 한 번 만들면 v0.2 다른 "반사" 계열 카드에도 공통 인프라 재사용 예상.
- v0.2 진입 전까지 backlog 보관.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 위스프와 레이스(탱커 몬스터들)는 영웅에게 맞으면 HP가 줄고 결국 죽는, 아주 평범한 방식으로 버팁니다. 그런데 실제 판타지 던전의 언데드 몬스터라면 죽어도 다시 일어나거나, 피를 빨아 상처를 회복하거나, 몸에 닿으면 역으로 반격하는 특성이 있어야 더 무섭지 않을까요? 마치 양파처럼 껍질을 벗겨도 또 껍질이 나오는 느낌처럼, 탱커들을 아무리 두들겨도 쉽게 쓰러지지 않는 구조를 만드는 카드들입니다. 그래서 오늘 제안하는 카드 3장은: "위스프가 죽을 때 작은 위스프로 다시 태어나는 '위스프 분열'", "레이스가 영웅을 때릴 때마다 자신의 상처를 회복하는 '레이스 흡혈'", "탱커들이 맞을수록 그 충격이 영웅에게도 돌아오는 '거울 갑옷'"입니다.
