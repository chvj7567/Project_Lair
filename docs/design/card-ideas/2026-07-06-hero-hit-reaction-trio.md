# Card Ideas — 2026-07-06 — 영웅이 얻어맞을수록 던전이 강해지는 피격 반응 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 피격 반응 (OnHeroHit Trigger) — 영웅이 맞는 순간 자체를 던전의 연료로 전환하는 카드 3종
- **목록**: 상처의 자부심 (WoundedPride) / 고통의 메아리 (PainEcho) / 피 끓는 분노 (BloodBoil)
- **기존 28장 + 과거 31회차 파일 + git log 17회차와의 중복 회피 확인됨**
  - 기존 28장 검토: OnHeroHit 이벤트를 트리거로 하는 카드 전무. 가장 유사한 것은 HeroAttackDown(패시브, 영구 공격력 감소)이지만 트리거 없는 즉시 적용 카드임.
  - 과거 회차 검토:
    - 06/02 death-echo-spawn: OnMonsterDeath 스폰 — 오늘과 다름 (처치 트리거 vs 피격 트리거)
    - 06/09 kill-echo-penalty: 처치 카운터 기반 패널티 — 오늘과 다름 (처치 수 vs 피격 수)
    - 06/15 attack-backfire-penalty: CounterThorns·RageCascade — "영웅이 공격하는 순간" 트리거. 오늘은 "영웅이 맞는 순간" 트리거로 반대 방향.
    - 06/26 spatial-control-position: 영웅 위치·거리 조건 — 오늘과 다름
    - 나머지 26회차: 모두 다른 트리거 레이어
  - OnHeroHit 이벤트 기반 카드는 지금까지 제안된 적 없음 ✅

---

## 1. 상처의 자부심 (WoundedPride) — 가칭

- **카테고리**: 패시브 추가 / Swarm 축
- **효과 모델**:
  - 영웅이 몬스터에게 피격당할 때마다 현재 활성 Spawner 중 랜덤 1개에서 즉시 보너스 몬스터 1마리 소환.
  - 글로벌 캡(18마리) 에 포함됨 — 캡 포화 시 소환 불발 (자연 상한).
  - 중첩 픽 시: 2픽 → 트리거마다 랜덤 Spawner 2개, 3픽 → 3개.
  - **밸런스 근거**: 영웅 공속 1초, 활성 몬스터 6종 × 다양 DPS 합산 → 평균 2~3s 간격으로 피격. 5분 런에서 약 100~150회 피격 추정. 글로벌 캡 포화(18마리)를 유지하면 추가 소환 대부분 스킵 — 실제 추가 공급은 +10~15% 수준. 캡이 자동 상한 역할을 하므로 화면 폭발 없음.
  - **수치 조정 포인트**: 피격당 소환 Spawner 수(1) / 추가 소환 비율 인스펙터 노출 권장.
- **구현 패턴**:
  - `WoundedPrideEffect.cs` — `IBattleContext.OnHeroHit` 이벤트 구독.
  - 피격 발생 시: `IBattleContext.GetActiveSpawners()` 에서 Random 1개 선택 → `CHMPool.Instance.Pop(spawner.MonsterPrefab, spawner.SpawnPoint)`.
  - 글로벌 캡 체크: `IBattleContext.IsFieldCapReached()` true 이면 소환 스킵 — SpawnWraithEffect·DesolationMarch 패턴 동일.
  - **공유 인프라**: `IBattleContext.OnHeroHit` 이벤트는 오늘 3장이 공유. HeroHealth 또는 HeroController 의 TakeDamage 콜백에서 발사 (기존 TakeDamage 패스 확인 후 추가). 첫 카드 구현 시 이벤트만 신설하면 나머지 2장은 구독만으로 완성.
- **시너지 후크**:
  - `SpawnerHaste` (모든 Spawner 주기 ×0.8) + WoundedPride: 정상 스폰이 빨라진 상태에서 보너스 스폰까지 → 필드 압박 극대화.
  - `GuardianRage` (Wisp·Wraith HP×2.0, 15s): 보너스 소환된 탱커들이 오래 버텨 영웅 피격 기회를 추가 확보 → WoundedPride 연쇄 가속.
  - Swarm Tier3 (글로벌 캡 +6, 18→24): 캡 여유로 보너스 소환 불발 빈도 감소 → WoundedPride 실효율 상승.
- **구현 비용 추정**: 3 — `IBattleContext.OnHeroHit` 이벤트 신설(1회) + 랜덤 Spawner 선택 + CHMPool.Pop. SpawnWraithEffect 패턴 재사용.
- **중복 재검증**: 기존 SpawnPhantoms/SpawnWisps (스포너 출력 +1 영구) 는 "스폰 주기당 출력 증가". WoundedPride 는 "피격 이벤트마다 랜덤 Spawner 보너스 팝". 트리거(피격)와 소환 출처(랜덤 Spawner)가 기존 모든 소환 카드와 다름 ✅

---

## 2. 고통의 메아리 (PainEcho) — 가칭

- **카테고리**: 패시브 강화 / Debuff 축
- **효과 모델**:
  - 영웅이 5회 피격당할 때마다 영웅 이동속도 영구 ×0.97 감소.
  - 영구 누적 캡: ×0.80 (= -20%) — 이 캡에 도달하면 이후 5회 카운터가 채워져도 추가 감소 없음.
  - 5분 런에서 약 100~150회 피격 → 20~30회 트리거 → 캡 도달 예상 구간: 약 2.5~3.5분.
  - **밸런스 근거**: 영웅 이동속도 20% 감소는 Slow 카드 (×0.5, 10s) 보다 온화하지만 영구. 탈출 능력이 제한되어 몬스터 포위 압박이 상시 유지됨. 캡이 있어 극단적 무력화 방지. 컨셉 §8: 평균 영웅 사망 2~4분 목표 — PainEcho 누적이 마무리 구간(3~4분) 압박을 강화하는 방향으로 설계.
  - **수치 조정 포인트**: 트리거 피격 수(5) / 이속 배율(0.97) / 캡(0.80) 인스펙터 노출.
- **구현 패턴**:
  - `PainEchoEffect.cs` — `IBattleContext.OnHeroHit` 구독 (WoundedPride 와 동일 채널).
  - 내부 hitCounter 유지. `hitCounter % 5 == 0` 시 `IBattleContext.Hero.ApplyPermMoveSpeedMult(0.97f)`.
  - 영구 이속 조작은 HeroAttackDownEffect (영구 공격력 ×0.75) 의 ApplyPermStat 패턴을 이동속도 레이어로 이식.
  - 캡 체크: 현재 누적 이속 배율이 0.80 이하이면 추가 적용 스킵.
- **시너지 후크**:
  - `Slow` (영웅 이속 ×0.5, 10s): 액티브로 일시 대폭 감속 + PainEcho 로 영구 감속 누적 → 이속 통제 이중 압박. Slow 10s 창 안에 영웅이 빠져나가기 어려워짐.
  - `HeroPoisonAura` (영웅 발 밑 독장판 5 DPS, 5s): PainEcho 로 느려진 영웅이 독장판에서 벗어나지 못해 지속 피해 체류.
  - `PhantomMoveSpeedBoost` (Phantom 이속 ×1.5): 영웅은 느려지고 팬텀은 빨라짐 → 포위 속도 역전.
- **구현 비용 추정**: 3 — HeroAttackDownEffect (영구 스탯 조작 패턴) + 히트 카운터. OnHeroHit 이벤트는 WoundedPride 와 공유.
- **중복 재검증**: 기존 Slow(×0.5 고정, 10s 일시) / Fear(3s 도주) / 06/09 HeroShackle(처치 카운터 기반 이속 감소, 8s 한정). PainEcho 는 "피격 5회마다 영구 이속 감소, 캡 있음" — 트리거(피격 수)와 영속성(영구·캡)이 기존 모든 이속 카드와 다름 ✅

---

## 3. 피 끓는 분노 (BloodBoil) — 가칭

- **카테고리**: 액티브 버프 / Dps 축
- **효과 모델**:
  - 발동 시 12초간 활성화.
  - 활성화 창 내 영웅이 피격당할 때마다 모든 몬스터 공격력(Power) +5% 즉시 임시 누적.
  - 12초 종료 시 누적된 임시 공격력 모두 원복.
  - 12초 내 예상 피격 수: 4~6회 → 공격력 +20~30% 임시 증폭.
  - **밸런스 근거**: Frenzy (공속 +50%, 10s) 와 비교 — Frenzy 는 공속 고정 증폭, BloodBoil 은 피격 수에 따라 가변. 영웅이 몬스터 공격을 더 많이 받는 전략(느린 영웅 + 대규모 포위)과 연계될수록 효과가 커짐. 단독 사용 시 보통, 시너지 빌드에서 폭발적. 12s 종료 후 원복으로 과영속 방지.
  - **수치 조정 포인트**: 지속시간(12s) / 피격당 배율(+5%) / 최대 누적 캡(+50% 권장) 인스펙터 노출.
- **구현 패턴**:
  - `BloodBoilEffect.cs` — IronWillEffect (지속시간 제한 전역 버프) 패턴 기반.
  - 활성화 시: `IBattleContext.OnHeroHit` 구독 시작, 12s 타이머 시작.
  - 피격마다: `MonsterBuffService.ApplyGlobalPowerMultiplier(1.05f)` (기존 FrenzyEffect 의 전역 공속 버프 패턴을 Power 레이어로 대체).
  - 12s 종료: `MonsterBuffService.RemoveSession(this)` — IronWillEffect 의 세션 기반 제거 패턴 재사용.
  - 캡 가드: 누적 배율 > 1.5 이면 추가 누적 스킵.
- **시너지 후크**:
  - `MarkOfDeath` (영웅 받는 데미지 ×1.5, 5s) + BloodBoil: BloodBoil 활성 12s 창에 MarkOfDeath 를 앞서 발동 → 영웅이 더 많이 맞아 BloodBoil 누적 가속 + 받는 피해 증폭 동시.
  - `WoundedPride` (피격 시 보너스 스폰): 피격마다 WoundedPride 로 몬스터 추가 소환 + BloodBoil 로 기존 몬스터 공격력 증폭 → 수와 강함을 동시 키우는 콤보.
  - `Slow` (영웅 이속 ×0.5, 10s): 느려진 영웅이 BloodBoil 창 안에서 더 많이 맞음 → 누적 공격력 최대치 도달 가능성 상승.
- **구현 비용 추정**: 3 — IronWillEffect (세션 기반 지속 버프) + FrenzyEffect (전역 Power 조작) + OnHeroHit 이벤트 조건부 구독. 세 패턴 모두 기존 코드 재조합.
- **중복 재검증**: 기존 Frenzy(공속 +50%, 10s, 조건 없음) / IronWill(받는 데미지 ×0.7, 15s). BloodBoil 은 "피격 이벤트마다 가변 공격력 임시 증폭, 12s" — 가변 누적과 피격 트리거 두 가지 모두 기존 액티브 카드에 없음 ✅

---

## 4. 공통 테마 고찰

### 왜 "피격 반응"인가

**트리거 레이어 공백 분석**: 기존 28장 + 31개 과거 제안 파일을 통해 현재 카드 트리거 구조를 정리하면:

| 트리거 | 기존 카드 예시 | 상태 |
|---|---|---|
| 픽 즉시 (영구) | WispHpBoost, SpawnerHaste | ✅ 다수 |
| 발동 후 N초 | IronWill, Frenzy, MarkOfDeath | ✅ 다수 |
| 처치 이벤트 | BloodThirst / BloodEcho(06/09) | ⚠ 적음 |
| 몬스터 사망 | PhantomBirth(06/02) | ⚠ 적음 |
| 영웅 피격 | **없음** | ❌ 공백 |
| 영웅 HP% | HpSurgeSpawn(06/25) | ⚠ 1회만 |
| 시간 주기 | TimeCurse(06/28) | ⚠ 1회만 |

**"영웅 피격" 트리거는 기존 28장은 물론 31개 제안 파일 전체에도 단 한 장도 없다.** v0.2 풀(패시브 30~40장, 액티브 20~30장)을 채울 때 트리거 다양성은 필수 — 영웅이 매번 비슷한 순간(픽·시간)만 반응하면 플레이어 의사결정이 단조로워진다.

**구조적 장점**: 오늘 3장은 `IBattleContext.OnHeroHit` 이벤트 하나를 공유한다. HeroHealth·HeroController 에 이미 TakeDamage 흐름이 있으므로 이벤트 발사 1회 추가만으로 인프라 완성. 이후 피격 트리거 계열 카드 추가 비용이 대폭 감소.

**플레이 감각**: 기존 카드들은 "영웅을 얼마나 약하게 만드느냐" 가 주된 축이었다. 오늘 카드들은 "영웅이 맞는 행위 자체가 던전을 키운다" 는 피드백 루프를 만든다 — 영웅이 강해서 잘 버티면 WoundedPride 로 몬스터 수가 늘고, PainEcho 로 발이 묶이고, BloodBoil 이 겹치면 공격력도 폭발적으로 오른다. 버티는 영웅이 오히려 더 위험해지는 역설.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **WoundedPride 우선 채택 권장**: Swarm 축 패시브 다양성 보강 + OnHeroHit 이벤트 인프라 최초 구축. 이 카드가 들어오는 순간 나머지 2장 구현 비용이 절반으로 감소.
- **PainEcho 다음 채택**: Debuff 축 영구 감속 라인 추가. HeroPoisonAura·Slow·Weaken 콤보와 자연스럽게 연결.
- **BloodBoil 은 독립 스프린트**: 액티브 카드라 Dps 축 액티브 검토와 묶어 진행.
- 3장 모두 OnHeroHit 이벤트 인프라 위에서 동작 → 이벤트 신설 PR 1개 + 효과 클래스 PR 3개 분리 권장.
- v0.2 진입 전까지 backlog 보관.

---

## 6. 쉬운 설명 (비개발자 요약)

지금까지 던전 카드들은 영웅이 약해지거나, 몬스터가 많아지거나, 빨라지는 방식이었습니다. 그런데 오늘 제안하는 카드들은 영웅이 한 대 맞는 순간, 던전이 반응하는 방식입니다. 영웅이 맞으면 새 몬스터가 툭 솟아나고, 맞을수록 발이 무거워지고, 심지어 모든 몬스터의 주먹이 더 세지기도 합니다. 마치 "때릴수록 더 강해지는 괴물 무리" 같은 느낌이라, 영웅이 오래 버틸수록 오히려 상황이 더 나빠집니다. 그래서 오늘 제안하는 카드 3장은: 영웅이 맞을 때마다 몬스터를 추가 소환하는 "상처의 자부심", 맞을수록 영웅 발이 느려지는 "고통의 메아리", 맞을수록 몬스터가 더 강하게 공격하는 "피 끓는 분노"입니다.
