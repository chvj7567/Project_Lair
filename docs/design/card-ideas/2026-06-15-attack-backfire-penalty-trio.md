# Card Ideas — 2026-06-15 — 공격 반격 패널티: 싸울수록 역풍을 맞는 영웅 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: 영웅의 공격 행위 자체가 반격 트리거·자기 소진·군단 가속으로 돌아온다. "싸우지 않으면 안 되지만, 싸울수록 불리해진다"는 역설적 압박 라인.
- 목록: 가시 반격 (CounterThorns) / 분노 연쇄 (RageCascade) / 고갈 저주 (ExhaustionCurse)
- 기존 28장 + git log 과거 17회차 (2026-05-28 ~ 2026-06-13) 와의 중복 회피 확인됨

---

## 1. 가시 반격 (CounterThorns) — 가칭

- **카테고리**: 패시브 강화
- **축**: Tank
- **효과 모델**: 글로벌 히트 카운터를 두어 영웅이 Wisp·Wraith 를 타격할 때마다 +1 누적. **8회마다** 현재 필드의 Tank 계열(Wisp·Wraith) 중 HP 가 가장 높은 개체가 1회 즉발 반격 (해당 개체 공격력 × 200%). 중첩 픽 시 임계 -1 감소 (2픽 = 7회, 3픽 = 6회, 최소 5회). 반격 후 카운터는 0 초기화.
  - 기준 수치 검증 — 영웅 ATK 50, Wisp HP 200 (4타에 처치), Wraith HP 500 (10타에 처치), Wisp ATK 10 × 200% = 20, Wraith ATK 20 × 200% = 40. 3분 런에서 Tank 타격 약 25~30회/분 → 약 3~4회 반격/분 × 30~40 avg dmg = 90~160 bonus HP/분 → 런 전체 약 300~500 HP 추가 압박. 영웅 HP 1000 기준 의미 있는 기여.
- **구현 패턴**: `IBattleContext` 에 `OnHeroHitMonster(EMonsterType, damage)` 이벤트 신규 추가 또는 기존 MonsterHealth OnDamaged 이벤트를 역으로 활용. `CounterThornsEffect` 가 Tank 타입에만 카운트 → 임계 도달 시 `IBattleContext.FindStrongestMonster(EMonsterType.Wisp | EMonsterType.Wraith)` 조회 → `monster.Attack(hero, multiplier: 2.0f)`.
- **시너지 후크**: GuardianRage (Wisp·Wraith HP×2 + 받는 데미지 ×0.5, 15s) — 두꺼운 탱커를 공략하는 영웅이 더 많은 반격을 받음; IronWill (모든 몬스터 받는 데미지 ×0.7) — 탱커가 오래 버텨 누적 카운터 속도가 증가하여 반격 빈도 상승
- **구현 비용 추정**: 3 (`OnHeroHitMonster` 이벤트가 `IBattleContext` 에 없으면 신규 추가 필요; 있으면 2)
- **중복 재검증**: 기존 28장 중 히트 카운터 기반 반격 없음; 06-09 `BloodEcho`·`SoulCurse`(06-02) 는 킬 트리거 기반 — CounterThorns 는 **타격(hit) 횟수 기반**이며 처치 없이도 발동. 06-08 `WoundedPursuit`(HP 비율 → 속도)와도 트리거·효과 모두 다름. ✓

---

## 2. 분노 연쇄 (RageCascade) — 가칭

- **카테고리**: 패시브 강화
- **축**: Dps
- **효과 모델**: 영웅이 Reaper·Hex(DPS 계열)를 **처치할 때마다**, 처치 위치 반경 **2m** 내 아군 모든 몬스터 이동속도 × 1.35, **5초** 적용. 범위 안에 같은 종·다른 종 혼재 시 전부 적용. 중첩 가능 (2회 처치가 동시에 범위 안에 있으면 ×1.35 × 1.35). 픽 중첩 시 범위 +0.5m (2픽 = 2.5m, 3픽 = 3m).
  - 기준 수치 검증 — Reaper HP 100 (영웅 ATK 50 → 2타에 처치), Hex HP 60 (2타 미만). 처치 사이클 빠름 → 범위 스피드 버프 발동이 잦음. 팬텀(×1.5 이속)과 조합 시 Phantom 이속 = 기본 × 1.5 (PhantomMoveSpeedBoost) × 1.35 = ×2.03 순간 폭발. 5초 지속이므로 연속 처치 시 실질적으로 이속 버프 유지.
- **구현 패턴**: `MonsterDeathEvent` 구독 (EMonsterType.Reaper | EMonsterType.Hex 필터) → `IBattleContext.GetMonstersInRadius(position, radius)` → `MonsterBuffService.ApplyMoveSeedBuff(list, 1.35f, 5f)`. 기존 `BloodThirstEffect`(처치 → 인근 HP 회복)와 동일 구조, 효과만 이속으로 교체.
- **시너지 후크**: PhantomMoveSpeedBoost (Phantom 이속 ×1.5) + RageCascade → 딜러 처치 시 인근 Phantom 이속이 최대 ×2.0 이상으로 순간 폭발; SpawnReapers / SpawnHexes 계열로 DPS 스포너 수를 늘리면 처치 사이클 = 분노 연쇄 발동 주기도 빨라짐 (자기 강화 루프)
- **구현 비용 추정**: 2 (기존 `BloodThirstEffect` 패턴 재사용)
- **중복 재검증**: 기존 `BloodThirst`(처치 시 인근 몬스터 HP 회복)와 트리거(DPS 종 한정 처치)·효과(이속 버프)가 다름; 06-02 `WraithRemnant`(Wraith 사망 → Wisp 소환)은 Tank 사망·소환 계열; 06-09의 킬 카운터들은 카운터 누적 방식 — RageCascade 는 즉발 범위 이속이라 개념적으로 구분됨. ✓

---

## 3. 고갈 저주 (ExhaustionCurse) — 가칭

- **카테고리**: 액티브 저주
- **축**: Debuff
- **효과 모델**: 픽 즉시 **12초간**, 영웅이 공격할 때마다 영웅 ATK -3 (하한선 ATK 20, 이후 감소 중단). 12초 종료 시 ATK 픽 직전 값으로 복구. 예시: 영웅 ATK 50, 12s에 1.0s 쿨다운 → 12회 공격 → ATK 50→20 (10타 후 floor) → 마지막 2타는 ATK 20으로. 고갈 상태 평균 ATK ≈ 32 (정상의 64%). 중첩 픽 불가 (각 픽 독립 발동, 시간 연장 없음).
  - 기준 수치 검증 — 정상 12s DPS: 50×12=600 총 피해 vs 고갈 12s: (50+47+...+20+20+20)≈440 총 피해. 약 160HP 기여 감소. 12s는 Frenzy(10s)·MarkOfDeath(5s)보다 길어 지속 유효. 픽 타이밍이 영웅 HP 높을 때일수록 이미 강한 영웅이 12s간 약화됨.
- **구현 패턴**: `ExhaustionCurseEffect.Apply()` → 12s 코루틴 시작 → `IBattleContext` 의 `OnHeroAttack` 이벤트 구독 → `hero.ModifyAttackPower(-3, floor: 20)`. 12s 후 `hero.RestoreAttackPower()`. 기존 `WeakenEffect`(ATK ×0.5, 10s 즉발)와 달리 **"공격 횟수 × 감소"** 라 플레이어가 공격 빈도를 느끼게 됨.
- **시너지 후크**: Weaken (ATK ×0.5, 10s) + ExhaustionCurse → 동시 구간 영웅 ATK = (50×0.5)→25에서 추가 -3씩. Weaken 끝난 뒤 ExhaustionCurse 복구 전이면 ATK 최저치 유지; HeroAttackDown (영구 ×0.75) + ExhaustionCurse → 기저 ATK가 이미 낮아 고갈 진행이 더 빨리 floor 에 닿음 → 전반 압박 강도 높임
- **구현 비용 추정**: 2 (`OnHeroAttack` 이벤트 있으면 2, 없으면 3)
- **중복 재검증**: `Weaken`(ATK ×0.5 고정, 10s)과 달리 **"공격할 때마다 점진 ATK 감소"** 방식 — 동일 유형 디버프이나 메커니즘·게임감이 다름. 06-04 `DespairFrenzy`(HP 비율 → 버프 강도)와도 무관. 12회차(06-12) 이전 파일 포함 17회차 전 범위에서 "액티브, 공격 횟수 기반 ATK 감소" 없음. ✓

---

## 4. 공통 테마 고찰

세 카드는 **"영웅이 공격하는 행위 자체가 역풍이 된다"** 는 테마를 공유한다.

- CounterThorns — 탱커를 때리면 탱커가 반격한다 (공격 행위 → 피격)
- RageCascade — DPS 몬스터를 죽이면 주변 군단이 빨라진다 (처치 행위 → 위협 증가)
- ExhaustionCurse — 영웅이 공격할수록 공격력이 떨어진다 (공격 행위 → 자기 소진)

기존 28장의 디버프 카드들(Weaken, HeroAttackDown, Fear, Bleed 등)은 대부분 영웅의 현재 상태나 이동 행위를 노린다. 이 3장은 "전투 행위(공격·처치)를 직접 페널티 트리거로 삼는다"는 차별점을 가진다. QA 시뮬레이션이 아직 가동되지 않아 구체적 픽률 공백은 확인 불가지만, "덜 싸우면 몬스터 압박이 약해지는" 지속 스폰 구조(§4.1) 안에서 이 카드들은 "영웅이 어느 방향을 선택해도 손해"인 새 딜레마를 만든다. Tank·Dps·Debuff 각 축에 1장씩 배분되어 단일 축 집중 빌드를 보강하는 유연성도 갖춘다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- CounterThorns 는 `IBattleContext.OnHeroHitMonster` 이벤트 존재 여부를 gameplay-programmer 와 사전 확인 권장 (없으면 구현 비용 3→4 상승 가능)
- ExhaustionCurse 도 `OnHeroAttack` 이벤트 여부 동일하게 사전 확인
- v0.2 진입 전까지 backlog 보관 — 풀 확장 시 Tank·Dps·Debuff 각 1장 추가로 자연스럽게 편입 가능

---

## 6. 쉬운 설명 (비개발자 요약)

보통 게임에서는 많이 싸울수록 이기는 게 당연합니다. 하지만 오늘 제안하는 카드들은 반대입니다. 철벽 같은 몬스터를 계속 두드리면 그 몬스터가 "이제 됐다" 하고 역으로 한 방 날리고, 빠른 딜러 몬스터를 죽이면 옆에 있던 동료들이 분노해서 더 빠르게 달려오고, 공격을 많이 할수록 영웅의 팔이 지쳐 점점 약한 한 방씩만 나옵니다. 그래서 오늘 제안하는 카드 3장은: 영웅이 "열심히 싸우는 것" 자체를 무기로 뒤집어, 공격할수록 스스로 불리해지는 새로운 압박 전략입니다.
