# Card Ideas — 2026-06-02 — 죽음의 메아리: 몬스터 사망 트리거 삼종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 몬스터 사망 시 발동되는 OnDeath 트리거 — 죽음이 전장의 또 다른 연료가 됨
- **목록**: PhantomBirth (팬텀 탄생) / SoulCurse (영혼의 저주) / WraithRemnant (레이스의 잔재)
- **기존 28장 + git log 과거 회차 중복 회피 확인됨**
  - 기존 28장 중 BloodThirst 가 "처치 시 몬스터 HP 회복" 으로 유일한 OnDeath 카드이나, 오늘 3장은 각각 "사망 위치 소환", "영웅 역류 피해", "종 교체 소환" 으로 메커니즘이 전혀 다름
  - 과거 5회차: 전장 상태 감지 / 종 간 연계 / 플레이그-독 / 낙인 / 리퍼·헥스 딜러 심화 — 모두 "사망 이벤트 반응" 테마 미포함

---

## 1. PhantomBirth — 팬텀 탄생

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - 필드 위 어떤 몬스터든 사망 시 25% 확률로 사망 위치 반경 1u 이내에 Phantom 1마리 즉시 소환.
  - 글로벌 캡(18마리) 에 포함됨 — 캡 포화 시 소환 불발 (자연 상한).
  - 중첩 픽 시 발동 확률 스택: 2픽 → 43.75% (1-(0.75²)), 3픽 → 57.8%.
- **구현 패턴**:
  - `IBattleContext.OnMonsterDied` 이벤트 구독 → `Random.value < 0.25f` 분기 → `CHMPool.Instance.Pop(EMonster.Phantom, deathPosition)`
  - 글로벌 캡 체크는 `IBattleContext.IsFieldCapReached()` 로 가드
- **시너지 후크**:
  - PhantomMoveSpeedBoost + PhantomBirth → 빠른 Phantom 이 죽을 때 또 낳음 (고속 자기증식 루프)
  - SwarmRush + PhantomBirth → 6마리 즉시 소환 후 전투, 사망하는 만큼 다시 Phantom 보충
  - Swarm Tier2 (스포너 주기 ×0.85) + PhantomBirth → 상시 Phantom 밀도 유지
- **구현 비용 추정**: 3 (OnDeath 이벤트 구독 신규 연동 필요, 단 이벤트 자체는 BloodThirst 가 이미 활용 중이라 패턴 재사용 가능)
- **중복 재검증**: BloodThirst 는 "처치 시 주변 몬스터 HP 회복" 으로 몬스터 유지 메커니즘. PhantomBirth 는 "사망 위치 신규 유닛 소환" 으로 풀 구성 변경 메커니즘 — 효과 방향이 다름 ✓

---

## 2. SoulCurse — 영혼의 저주

- **카테고리**: 액티브 / 저주 (Debuff 축)
- **효과 모델**:
  - 발동 후 15초간 영웅이 몬스터를 처치할 때마다 영웅 HP 즉시 -3% 역류.
  - 역류 피해는 최소 1HP — 영웅 HP 0 에 근접 시에도 약하게 작동.
  - 중첩 픽 시 지속시간 연장: 2픽 → +5s (총 20s), 3픽 → +5s (총 25s). 퍼센트는 불변.
  - 영웅 추정 처치 속도: ~0.5~1마리/s (Phantom 밀도 높을 때). 15s × 0.75마리/s × 3% = 약 33.75% HP 감소 기대값. 전략적 사용 필요.
- **구현 패턴**:
  - `IHeroAura` 에 `float KillReflectPercent` 속성 추가 (현재 없는 속성).
  - `HeroController` 의 OnKillMonster 콜백에서 `_hero.TakeDamage((int)(maxHp * killReflectPercent))` 처리.
  - 15s 타이머는 `MonsterBuffService` 아닌 `HeroAuraService` 에서 관리 (영웅 측 효과).
- **시너지 후크**:
  - Fear + SoulCurse 콤보: Fear 로 3s 도주 → 영웅 처치 0건 → SoulCurse 15s 중 12s 가 "위협 구간" — Fear 가 SoulCurse 의 가장 안전한 세팅 카드
  - Bleed + SoulCurse: 영웅이 이동 중 HP 감소 + 처치 시 HP 감소 → 행동 모두 패널티화
  - Slow (영웅 이속 ×0.5 + 몬스터 이속 ×1.3) + SoulCurse: 몬스터 빠름 + 영웅 느림 → 처치 횟수 급감 → SoulCurse 역류 위협 장기화
- **구현 비용 추정**: 3 (IHeroAura 속성 신규 추가, HeroController OnKill 훅 신규 연동)
- **중복 재검증**: Weaken (영웅 공격력 ×0.5, 10s) 은 딜 감소. SoulCurse 는 처치 자체를 역류 피해로 전환. 메커니즘 레이어가 다름 — Weaken 은 공격력 디버프, SoulCurse 는 처치 이벤트 페널티 ✓

---

## 3. WraithRemnant — 레이스의 잔재

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wraith 사망 시 사망 위치에 Wisp 1마리 즉시 소환.
  - 글로벌 캡(18마리) 에 포함됨.
  - 중첩 픽 시 소환 수 누적: 2픽 → Wisp 2마리, 3픽 → Wisp 3마리.
  - Wraith HP 500 기준, 평균 전투에서 2~4마리 사망 추정 → 런당 2~4 Wisp 보충 기대.
- **구현 패턴**:
  - `IBattleContext.OnMonsterDied` 이벤트 구독 → `monsterType == MonsterType.Wraith` 필터 → `CHMPool.Instance.Pop(EMonster.Wisp, deathPosition)`
  - PhantomBirth 와 동일 이벤트 채널, 필터 조건만 다름 → 구현 비용 최저
- **시너지 후크**:
  - ReplaceWispsToWraith + WraithRemnant: Wisp 스포너 → Wraith 출력 변경 후, 그 Wraith 가 죽으면 다시 Wisp 재생. "Wisp→Wraith→Wisp→..." 순환 생태계 형성
  - WispHpBoost (Wisp HP ×1.5) + WraithRemnant: 재생된 Wisp 가 탱킹력 강화 → 순환 효율 상승
  - Tank Tier3 (글로벌 캡 +6, 18→24) + WraithRemnant: 캡 여유로 소환 불발 빈도 감소 → 순환 안정화
- **구현 비용 추정**: 2 (PhantomBirth 구현 후 필터 조건 변경만으로 완성, 코드 재사용 최대)
- **중복 재검증**: SpawnWraith (Wraith 스포너 동시 출력 +1) 는 지속 스폰 증가. WraithRemnant 는 개별 Wraith 사망 시 1회 Wisp 전환. 스폰 메커니즘 vs OnDeath 전환 메커니즘으로 다름 ✓

---

## 4. 공통 테마 고찰

### 왜 "죽음의 메아리" 인가

현재 28장의 효과 트리거는 두 가지 레이어에만 집중되어 있다:
1. **카드 픽 순간** — 영구 글로벌 버프 (WispHpBoost, SpawnerHaste 등)
2. **발동 후 N초** — 지속 효과 (IronWill, Frenzy, MarkOfDeath 등)

몬스터 사망이라는 **전장 이벤트** 에 반응하는 카드는 BloodThirst 1장뿐이다.
v0.2 확장에서 OnDeath 트리거 계열을 3~5장 더 추가하면 "전장의 흐름을 읽는 빌드" 방향이 열린다.

### QA 연계

최신 QA 리포트(6차, 2026-05-26)에서 Debuff 축 카드 (Weaken 19% / Slow 18% / Bleed 18%) 가 픽률 하위권을 형성했다.  
SoulCurse 는 Debuff 축에서 "적극적 승부 카드" 로 기능하여 픽률 저조의 원인인 "수동적 디버프" 이미지를 탈피한다.  
또한 Swarm 축의 Phantom 특화 카드 (현재 PhantomMoveSpeedBoost 1장뿐) 를 PhantomBirth 로 보강한다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- PhantomBirth + WraithRemnant 는 같은 OnDeath 이벤트 채널을 공유하므로 1회 구현 스프린트에 묶어 진행 권장
- SoulCurse 는 IHeroAura 확장을 수반하므로 별도 스프린트 또는 IHeroAura 수정 작업에 편승
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터가 죽으면 그냥 사라진다. 오늘 제안하는 카드들은 몬스터가 죽는 순간을 새로운 사건의 시작으로 만든다. 예를 들어 팬텀이 죽으면 그 자리에 또 다른 팬텀이 태어나거나, 강한 레이스가 쓰러지면 작은 위스프가 그 자리를 지키는 식이다. 심지어 영웅이 몬스터를 죽이면 오히려 영웅 자신이 피를 흘리게 하는 저주 카드도 있어서, 영웅 입장에서는 "열심히 싸울수록 위험해지는" 상황이 만들어진다. 그래서 오늘 제안하는 카드 3장은: 팬텀 탄생(죽으면 팬텀 소환) / 영혼의 저주(영웅이 잡을수록 HP 감소) / 레이스의 잔재(레이스 사망 시 위스프 재생).
