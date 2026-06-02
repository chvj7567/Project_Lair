# Card Ideas — 2026-06-03 — 위스프 벽 전술: 느리고 질긴 장벽으로 특화

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 위스프 벽 전술 (Wisp Wall) — 현재 위스프는 "HP가 많은 탱커" 역할에 그치지만, 밀도·근접 조건에 반응하는 카드 3장으로 "살아있는 장벽" 서브 전략을 가능하게 한다. Tank 축 안에서 위스프만의 독립 빌드 경로를 개설한다.
- **목록**: WispContactSlow (접촉 봉인) / WispLink (위스프 연맹) / WispWall (위스프 방진)
- **기존 28장 + git log 과거 회차와의 중복 회피 확인됨**
  - 기존 28장: 위스프 관련 카드는 WispHpBoost(무조건 HP×1.5), SpawnWisps(출력+1, Swarm축), ReplaceWispsToWraith(종 교체)뿐. 위스프 근접 조건 / 마릿수 조건 / 포위 조건 카드 전무.
  - 과거 6회차 테마:
    - 전장 상태 감지 (픽 시점 스냅샷 스케일링)
    - 종 간 연계 (다종 공존 조건 버프)
    - 플레이그-독 생태계 (Plague 사망 이벤트 기반)
    - 낙인 트리오 (임시 + 영구 스택 이중 효과)
    - 리퍼·헥스 딜러 심화 (Dps 축 공백 보완)
    - 죽음의 메아리 (OnDeath 소환 트리거)
  - 오늘 3장: "영웅 반경 내 실시간 위스프 밀도 감시" — 어느 과거 회차와도 메커니즘 축이 다름 ✓

---

## 1. WispContactSlow — 접촉 봉인

- **카테고리**: 패시브 강화 (Tank 축, 위스프 특화)
- **효과 모델**:
  - 영웅 반경 **0.8u** 이내에 살아있는 Wisp 1마리당 영웅 이동속도에 **-8% 중첩 디버프** 적용.
  - 최대 5중첩 → 최대 **-40% 이동속도**.
  - 매 tick 실시간 갱신: Wisp가 범위에서 벗어나거나 사망 시 해당 중첩 즉시 해제.
  - 중첩 픽 2회: 마리당 -16% (최대 -80%까지 이론값이나, 실전 5마리 동시 근접 구현 난이도로 실질 최대 -40~60%).
  - 밸런싱 근거 (컨셉 §8): 기존 Slow(액티브 저주) = 영웅 이속 ×0.5(-50%) 10초. 이 카드는 영구 유지되지만 위스프가 밀착해야 하므로 실전 가동률이 60~70%로 추산. 평균 기대 효과 -24~28% ≈ Slow의 지속형 약화 버전.
- **구현 패턴**:
  - `WispContactSlowEffect.cs` — IHeroAura 파이프라인 활용.
  - `IBattleContext.GetMonstersInRange(EMonster.Wisp, heroPos, 0.8f)` 매 tick 호출 → count 계산.
  - `IHeroAura.MoveSpeedMultiplier` 에 `1f - (count * 0.08f)` 를 곱연산 적용 (HeroAttackDown 패턴 재사용, stat만 MoveSpeed로 교체).
  - 기존 `SlowEffect.cs` 와 stat 대상이 겹치므로 곱연산 누적 처리 확인 필요 (별도 배율 레이어).
- **시너지 후크**:
  - **SpawnWisps + WispContactSlow**: 필드 Wisp 수 증가 → 영웅 주변 동시 근접 확률 상승 → 자동으로 이속 -40% 근접.
  - **WispHpBoost + WispContactSlow**: HP 높아진 Wisp가 오래 근접 유지 → 둔화 중첩 지속 시간 연장.
  - **WispLink(아래)**: Wisp 를 3마리 이상 유지할 인센티브가 생겨 이 카드의 중첩 조건도 함께 충족.
  - **Bleed(기존 Debuff 저주)**: 영웅이 느려진 상태에서 이동하면 HP 감소 → 느림 = 이동 = 더 큰 출혈 손실.
- **구현 비용 추정**: 2 (GetMonstersInRange 는 IBattleContext에 추가 필요하나, 기존 GetMonsters+LINQ로 유도 가능. IHeroAura 패턴 재사용 → 코드 신규 15~30줄 수준)
- **중복 재검증**: PlagueSlowBoost = Plague 공격 시 SlowFactor 강화(공격 이벤트 트리거). Slow 카드 = 10초 임시 저주. WispContactSlow = Wisp 근접 존재 조건 실시간 디버프. 세 카드 트리거 축이 각각 다름 ✓

---

## 2. WispLink — 위스프 연맹

- **카테고리**: 패시브 강화 (Tank 축, 위스프 특화)
- **효과 모델**:
  - 필드에 Wisp가 **3마리 이상** 살아있는 동안 모든 Wisp HP **×1.3** 추가 보너스.
  - 조건 충족 → 즉시 적용 / 조건 해제 → 즉시 복원 (현재 HP도 비례 조정).
  - **WispHpBoost 와 곱연산 누적**: WispHpBoost(×1.5) + WispLink(×1.3) = **×1.95** (Wisp 기본 HP 200 → 390).
  - 중첩 픽 2회: ×1.3 × 1.3 = ×1.69 추가 (WispHpBoost 포함 시 ×2.535 → 기본 200 HP → 507).
  - 밸런싱 근거 (컨셉 §8): WispHpBoost(무조건 ×1.5) pick 최다. WispLink는 조건부로 더 강하지만 3마리 유지 비용(필드 캡 소모)이 있음. 예상 평균 가동률 75% → 기대 배율 ×1.22. WispHpBoost 대비 조건부 프리미엄.
- **구현 패턴**:
  - `WispLinkEffect.cs` — MonsterBuffService.Tick() 내 분기 추가.
  - `IBattleContext.GetMonsterCount(EMonster.Wisp) >= 3` 조건 매 tick 감시.
  - 조건 True: `MonsterBuffService.AddConditionalBuff(EMonster.Wisp, EMonsterStat.HP, 1.3f)` / False: 해제.
  - 현재 Wisp HP 비례 조정 (조건 해제 시 HP가 최대치의 같은 비율로 감소) — ToughHideEffect.cs 패턴 참조.
- **시너지 후크**:
  - **SpawnWisps(기존)**: 출력 +1 → 3마리 유지 조건 달성 용이.
  - **WispContactSlow(위)**: 3마리 이상 유지 조건이 WispContactSlow의 최대 중첩과 함께 달성됨 — 두 카드가 같은 "3마리" 목표 공유.
  - **WispWall(아래)**: WispLink로 Wisp가 오래 살면 WispWall의 포위 조건도 더 오래 유지.
  - **ReplaceWispsToWraith(기존)**: 반대 전략 — Wisp를 없애므로 이 카드와 배타적. 의사결정 포인트 생성.
- **구현 비용 추정**: 2 (WispHpBoostEffect.cs 패턴에 조건부 분기만 추가. MonsterBuffService.Tick() 수정 15~25줄)
- **중복 재검증**: WispHpBoost = 무조건 영구 ×1.5. WispLink = 3마리 이상 조건부 ×1.3. 전자는 "항상 강한 Wisp", 후자는 "뭉쳐야 강해지는 Wisp" — 발동 조건과 전략 의도가 다름 ✓

---

## 3. WispWall — 위스프 방진

- **카테고리**: 패시브 환경 (Tank 축, 위스프 특화)
- **효과 모델**:
  - 영웅 반경 **3u** 이내에 Wisp가 **3마리 이상** 동시에 살아있을 때, 영웅 공격력 **×0.8** (−20%) 자동 적용.
  - 조건 해제 시 즉시 복원.
  - 중첩 픽 2회: ×0.8 × 0.8 = ×0.64 (−36%최대).
  - 밸런싱 근거 (컨셉 §8): HeroAttackDown(기존 패시브, 영구 ×0.75 = −25%). WispWall은 조건부 ×0.8(−20%)로 더 약하지만 비영구적·회복 가능 — SpawnWisps로 Wisp를 계속 보충해 조건을 유지하는 "살아있는 압박"을 설계. 영웅 기본 DPS 50 → 40. 필드 생존 시간 증가 기대 +15~20%.
- **구현 패턴**:
  - `WispWallEffect.cs` — WispContactSlow와 유사한 GetMonstersInRange 호출 (반경 3u, 조건 3마리).
  - `IBattleContext.GetMonstersInRange(EMonster.Wisp, heroPos, 3f).Count >= 3` 조건 매 tick 감시.
  - IHeroAura.AttackPowerMultiplier 에 `0.8f` 곱연산. HeroAttackDown(기존, 영구 ×0.75)과 곱연산 누적 → 최대 ×0.8 × 0.75 = ×0.6 (−40% 공격력).
- **시너지 후크**:
  - **WispHpBoost + WispLink + WispWall**: 3장 모두 픽 시 Wisp HP ×1.95, 3마리+ 유지 쉬워짐 → WispWall 상시 가동.
  - **HeroAttackDown(기존 Debuff 패시브)**: WispWall과 곱연산 누적 → 영웅 공격력 ×0.6. 딜 감소와 Tank 유지 시너지.
  - **IronWill(기존 Tank 액티브, 받는 데미지 ×0.7, 15s)**: 위스프가 약해진 영웅 딜을 15초간 더 버팀 → WispWall 포위 유지 시간 연장.
  - **WispContactSlow(위)**: 영웅이 느려져 포위 범위 이탈이 어려워짐 → WispWall 조건 더 쉽게 유지.
- **구현 비용 추정**: 2 (GetMonstersInRange 재사용, IHeroAura 패턴 재사용. 신규 20~30줄)
- **중복 재검증**: HeroAttackDown = 영구 무조건 −25%. WispWall = 조건부(위스프 근접 3마리) −20%, 조건 해제 시 복원. 영구 vs 조건부, 무조건 vs 밀도 감시 — 전략 설계가 다름 ✓

---

## 4. 공통 테마 고찰

**왜 오늘 이 테마인가:**

위스프는 MVP 28장 중 HP 탱커 역할만 수행하며, WispHpBoost + SpawnWisps만으로 "더 많은 HP, 더 많은 수" 전략이 전부다. Tank Tier 1~3 시너지(Wisp·Wraith HP×1.3 → Power×1.2 → 글로벌 캡+6)도 Wisp와 Wraith를 묶어서 취급해 Wisp 단독 빌드 경로가 없다.

QA 시뮬레이션이 블록된 상태지만, 현재 구조에서 명백한 설계 공백이 있다: **"위스프를 많이 유지하면 무엇이 좋은가?"** WispHpBoost는 "유지해서 오래 버팀", SpawnWisps는 "더 많이 나옴" — 하지만 위스프 밀도 자체가 전략적 의미를 갖는 카드가 없다.

오늘 3장이 채우는 공백:
- WispContactSlow: 위스프 밀도 → 영웅 이속 압박 (포위 전술)
- WispLink: 마릿수 조건 → HP 보너스 (유지 보상)
- WispWall: 근접 밀도 조건 → 영웅 딜 저하 (장벽 효과)

3장 조합은 "3마리 이상 유지 + 영웅 주변에 배치 = 이속 감소 + 딜 감소 + 위스프 내구성 증가"의 복합 압박 세트가 된다. 기존 Tank 축(Wraith 중심 vs Wisp 중심)의 선택지가 생긴다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- WispContactSlow + WispWall의 GetMonstersInRange API는 IBattleContext에 신규 추가가 필요 — gameplay-programmer 단계에서 IBattleContext 확장 포함
- 3장 모두 구현 비용 2로 상대적으로 낮음 → v0.2 초기 배치에 적합
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 위스프는 그냥 "체력 많은 장애물" 역할이에요. 영웅이 때리면 맞고, 천천히 죽는 것 말고는 딱히 하는 게 없죠. 오늘 제안하는 카드 3장은 위스프를 진짜 "장벽"으로 만들어 주는 아이디어예요. 위스프가 영웅에게 달라붙으면 영웅이 느려지고, 위스프가 3마리 이상 영웅 주변에 있으면 영웅이 약해지고, 위스프가 많을수록 서로 결속해서 더 안 죽어요. 비유하자면 "귀찮고 끈질긴 벌레 떼가 사방에서 달라붙어 꼼짝 못 하게 만드는 것"이라고 할 수 있어요. 그래서 오늘 제안하는 카드 3장은: 위스프를 쏟아내서 영웅 주변을 포위하는 "살아있는 장벽 빌드"를 가능하게 해 주는 카드들입니다.
