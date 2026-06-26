# Card Ideas — 2026-06-27 — 영웅의 위치가 곧 약점: 공간 압박 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 공간 압박 — 영웅의 필드 내 **위치(중앙/경계)** 및 **몬스터와의 거리**를 실시간 조건으로 삼는 카드 3장.
  "어디에 있느냐"가 곧 페널티가 되는 신규 조건 축을 탐색한다.
- **목록**: 중앙 함정 (CenterTrap) / 경계 공황 (EdgePanic) / 거리 응징자 (DistancePunisher)
- **기존 28장 + 28회차 과거 기록 중복 회피 확인됨**
  - 기존 28장: 영웅 위치 또는 몬스터와의 거리를 조건으로 사용하는 카드 전무.
    가장 유사한 HeroPoisonAura는 "영웅 발 밑 독장판이 따라다니는" 추종형이지, 특정 구역 진입 조건이 아님.
  - 과거 28회차 전체:
    - 06-06 DensityTide: 총 생존 수(≥12마리) 임계치 기반 — **몬스터 수**가 조건, 위치 무관
    - 05-29 CrossSpeciesSynergy: 종 조합 공존 기반 — 종 포트폴리오가 조건, 위치 무관
    - 06-03 WispWallEncirclement: 위스프 밀도 기반 영웅 디버프 — **특정 종 근접 밀도**가 조건, 영웅 절대 위치 무관
    - 06-26 EventBurstSpawn: HP 변화/액티브 픽 이벤트 기반 즉발 스폰 — **게임 이벤트** 트리거, 좌표 무관
    - 나머지 회차 전부: 패시브 픽 누적·시간·처치 수·저주 상태 기반 — 위치/거리 조건 없음
  - 오늘 3장: **IBattleContext.GetHeroPosition() / GetHeroDistanceFromCenter() / GetNearestMonsterDistance()** 를 조건 축으로 사용 — 28회차 전체와 메커니즘 축 비중복 ✅

---

## 1. 중앙 함정 (CenterTrap) — 가칭

- **카테고리**: 패시브 강화 / Debuff 축 추가
- **효과 모델**:
  - 영구 조건부 패시브. 던전 중앙 반경 **5m 이내**를 "덫 구역"으로 영구 지정.
  - 영웅이 덫 구역 안에 있는 동안: 모든 Plague 종의 SlowFactor **×0.75 추가** 적용 (곱연산 누적).
  - 구역 이탈(5m 초과) 즉시 추가 둔화 해제. 영웅이 중앙으로 돌아오면 다음 Plague 공격부터 재적용.
  - 수치 근거 (컨셉 §8, 밸런싱 기준):
    - 기본 Plague SlowFactor: 20% 둔화 (SpeedRatio 0.8)
    - PlagueSlowBoost(패시브) 픽 후: ×0.75 → SpeedRatio 0.6 (40% 둔화)
    - CenterTrap 발동 시 추가 ×0.75 → SpeedRatio 0.45 (**55% 둔화**)
    - Slow 카드(액티브 10s, 이속 ×0.5)와 비교: 동급 강도이나 **영구 패시브 + 구역 조건**으로 균형.
    - 영웅 AI ("가장 가까운 몬스터에게 자동 이동")는 몬스터가 중앙에 집중할수록 영웅을 중앙으로 유인 → 덫 구역이 자연히 활성화됨.
- **구현 패턴**:
  - `CenterTrapEffect.Apply(IBattleContext ctx)`:
    - IBattleContext에 `GetHeroPosition()` 신규 노출 (Hero Transform 참조, 약 5줄)
    - MonsterBuffService.Tick() 내 조건 분기 추가:
      ```
      if (Vector3.Distance(ctx.GetHeroPosition(), ctx.DungeonCenter) <= 5f)
          plague.ApplyAdditionalSlowMultiplier(0.75f); //# 기존 Plague SlowFactor 위에 곱산
      ```
    - DungeonCenter는 IBattleContext 기존 상수 (Spawner ring 중심) — 신규 의존 없음
- **시너지 후크**:
  - **PlagueSlowBoost** 와 이중 둔화 → 중앙 진입 시 영웅 55% 이속 감소, Swarm 수렴 속도 대비 압도적 느려짐
  - **SpawnPhantoms + SpawnerHaste** 조합: 팬텀이 빠르게 중앙으로 수렴 → 영웅을 중앙으로 유인하는 몬스터 밀도 확보
  - Debuff Tier2 (HeroAttackDown 자동 등록)와 조합 시: 중앙에 발 묶임 + 공격력 감소 이중 압박
- **구현 비용 추정**: 2 (GetHeroPosition API 노출 5줄 + MonsterBuffService 조건 분기)
- **중복 재검증**: 기존 PlagueSlowBoost는 "무조건 SlowFactor 강화(영구)", CenterTrap은 "위치 조건부 추가 강화" — 발동 조건 축이 다름 ✅

---

## 2. 경계 공황 (EdgePanic) — 가칭

- **카테고리**: 패시브 추가 / Swarm 축 추가
- **효과 모델**:
  - 영구 반응형 패시브. 영웅이 던전 중앙으로부터 **스포너 링 반지름의 70% 이상** 거리로 이동할 때마다,
    Phantom 스포너 1기에서 Phantom **2마리를 즉시 소환**. 쿨다운 **5초** (연속 경계 탐색 남발 방지).
  - 의미: 영웅이 "몬스터 무리를 뚫고 외곽으로 도망"하는 생존 전략을 취할 때 오히려 추가 Phantom이 배후에서 솟아나 퇴로를 막는다.
  - 수치 근거 (컨셉 §8):
    - Phantom: HP 30, 이동속도 빠름, 필드 캡 공유. 2마리 즉시 소환 = WallOfWisps(4마리, 액티브)의 절반이지만 패시브·반복 발동.
    - 평균 영웅 2~4분 기준 외곽 이동 시도 약 3~6회 추정 → 총 Phantom 6~12마리 추가 기여.
    - Swarm Tier2(모든 스포너 주기 ×0.85) 대비: Tier2는 전체 주기 단축이고, EdgePanic은 "도주 반응 즉발"이므로 성격이 다름.
    - PhantomMoveSpeedBoost 픽 시 추가 소환 팬텀도 이속 ×1.5 → 빠른 포위 완성.
- **구현 패턴**:
  - `EdgePanicEffect.Apply(IBattleContext ctx)`:
    - IBattleContext에 `GetHeroDistanceFromCenter()` 신규 노출 (GetHeroPosition()에서 단순 계산 래핑, 2줄)
    - `ctx.SpawnerRingRadius` 상수 참조 (continuous-spawn-round.md §2 기준값)
    - `PassiveTriggerService` 또는 `BattleController.Update` 루프에 위치 감시 추가 (쿨다운 타이머 포함):
      ```
      if (ctx.GetHeroDistanceFromCenter() >= ctx.SpawnerRingRadius * 0.7f
          && _cooldown <= 0f)
      {
          ctx.SpawnImmediately(EMonster.Phantom, count: 2);
          _cooldown = 5f;
      }
      ```
    - SpawnPhantoms의 "스포너 출력 +1(영구)" 패턴과 달리, 1회성 즉발 스폰 — SpawnWraith/WallOfWisps 즉발 패턴 재사용.
- **시너지 후크**:
  - **PhantomMoveSpeedBoost**: EdgePanic 소환 Phantom도 이속 버프 적용 → 즉각 포위 완성
  - **SpawnPhantoms**: 스포너 상시 출력 증가 + EdgePanic 즉발 소환 → Swarm Tier 카운터 빠르게 축적
  - **Slow (액티브)**: 영웅 이속 ×0.5 + 팬텀 이속 ×1.3 → EdgePanic 소환 팬텀이 영웅을 따라잡는 시간 단축
- **구현 비용 추정**: 2 (GetHeroDistanceFromCenter 래핑 + Update 루프 위치 감시 + 쿨다운 타이머)
- **중복 재검증**: 06-26 EventBurstSpawn(HP 변화/액티브 픽 이벤트 트리거) vs EdgePanic(영웅 위치 변화 트리거) — 트리거 타입 완전히 다름 ✅

---

## 3. 거리 응징자 (DistancePunisher) — 가칭

- **카테고리**: 액티브 버프 / Dps 축 추가
- **효과 모델**:
  - 발동 시 영웅과 **현재 가장 가까운 몬스터와의 거리**를 즉시 스냅샷. 그 거리에 비례하여
    **10초간 모든 Reaper·Hex의 Power 증폭**: `Bonus = min(distance / 5m, 3) × 10%` → 최대 +30%.
  - 예: 영웅이 가장 가까운 몬스터와 15m 떨어져 있으면 (15/5=3) × 10% = +30% Power 증폭.
    영웅이 이미 몬스터에 붙어 있으면(거리 < 5m) 보너스 0.
  - 의미: 영웅이 Reaper·Hex 몬스터들을 뚫고 거리를 벌리는 데 성공해도, 그 순간 더 강해진 딜러가 돌진한다.
    "플레이어가 딜러를 회피할수록 딜러가 강해지는" 역설적 압박.
  - 수치 근거 (컨셉 §8):
    - Frenzy(공속 +50%, 10s, 전체): 최강 단기 버프 기준선.
    - DistancePunisher 최대 +30% Power는 Frenzy 대비 약 60% 효과이지만, **조건부 + 타이밍 의존** — 전략적 관리 가능.
    - ReaperAtkSpeed(공격 쿨다운 ×0.7, 패시브 영구) 대비: DistancePunisher는 거리 관리 실패 시 발동 강도 급등 → 위협 수준 가변.
    - 평균 영웅 이동 속도와 Reaper 이동속도 차이 기준, 거리 15m는 약 3~4초 내 좁혀짐 → 10초 버프 효과 대부분 소진 전에 딜 가능.
- **구현 패턴**:
  - `DistancePunisherEffect.Apply(IBattleContext ctx)`:
    - 발동 시점 스냅샷: `float dist = ctx.GetNearestMonsterDistance()` (IBattleContext 신규 노출 1메서드, 기존 몬스터 리스트에서 Vector3.Distance 최소값 탐색)
    - `float bonus = Mathf.Min(dist / 5f, 3f) * 0.1f;`
    - `MonsterBuffService.ApplyTimed(EMonster.Reaper, EMonster.Hex, powerScale: 1f + bonus, duration: 10f)` — 기존 Frenzy / IronWill 의 `ApplyTimed` 패턴 재사용
- **시너지 후크**:
  - **MarkOfDeath (액티브)**: DistancePunisher로 강해진 Reaper·Hex에 마크 추가 → 5초간 영웅 받는 데미지 ×1.5 중첩으로 버스트
  - **ReaperAtkSpeed**: 패시브 쿨다운 ×0.7 + 액티브 Power +30% → Reaper가 빠르고 강하게 타격
  - **HexRangeBoost**: 거리 벌어진 상황에서 Hex 사거리 ×1.4 → 원거리에서도 Hex 공격 도달
- **구현 비용 추정**: 2 (GetNearestMonsterDistance 신규 1메서드 + ApplyTimed 기존 패턴 재사용)
- **중복 재검증**: Frenzy(전체 공속 +50%, 조건 없음), MarkOfDeath(데미지 배율, 조건 없음) — DistancePunisher는 "현재 거리 비례 Power 스냅샷" 조건 구조로 차별화 ✅

---

## 4. 공통 테마 고찰

세 카드 공통 핵심: **"영웅이 있는 곳"을 실시간으로 조건 축으로 삼는다**.

- CenterTrap: 중앙(상수 위치 조건) → Debuff 강화
- EdgePanic: 외곽 이탈(거리 임계 조건) → Swarm 즉발
- DistancePunisher: 몬스터와의 이격(상대 거리 조건) → Dps 증폭

오늘 이 테마를 선택한 이유:
1. **QA 데이터 공백 대응**: 유일한 QA 리포트 `2026-05-22.md` 는 시뮬레이션 실행 불가 상태 — 픽률 데이터 없음.
   그러나 게임 구조 분석에서 현재 **모든 28장이 "종 스탯·스포너 출력·영웅 상태"를 조건으로** 사용하며,
   **영웅의 절대 위치/상대 거리를 조건으로 삼는 카드가 전무**함을 발견.
2. **시너지 공백**: 영웅 AI("가장 가까운 몬스터 추적")는 몬스터 배치 전략에 영향을 주는 핵심 요소지만,
   이 AI 행동 패턴에 반응하는 카드가 없어 "배치 → 영웅 유도"라는 상위 전략이 아직 카드 레벨에서 표현 안 됨.
3. **3축 분산**: 각 카드가 Debuff/Swarm/Dps 축 하나씩 담당 → 세 가지 빌드 방향 모두에서 활용 가능.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- **IBattleContext 확장 우선 결정 필요**: `GetHeroPosition()` · `GetHeroDistanceFromCenter()` · `GetNearestMonsterDistance()`
  세 메서드는 별도 카드 채택 유무와 무관하게 "시뮬레이션 훅" (QA 리포트 §3 참조)과도 호환 가능한 인프라.
  먼저 해당 API를 BattleController/IBattleContext에 추가하고 이 카드들을 그 위에 올리는 순서 권장.
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금까지 우리 게임의 카드들은 "몬스터를 더 강하게" 또는 "영웅을 더 약하게" 만드는 방식이었습니다. 그런데 영웅이 싸우는 "장소"나 "거리"는 아직 카드에 활용되지 않았어요. 오늘 제안하는 카드들은 "영웅이 중앙에 있으면 더 많이 묶이고, 구석으로 도망가면 새 몬스터가 솟아나고, 멀리 떨어져 있을수록 딜러가 더 세진다"는 아이디어입니다. 마치 어둠 속에서 움직일수록 더 불리해지는 공포 영화처럼, 영웅이 살아남으려 움직이는 행동 자체가 새로운 위협을 만드는 구조입니다.

그래서 오늘 제안하는 카드 3장은: 던전 가운데 있으면 플레이그의 둔화가 두 배로 강해지는 **중앙 함정**, 외곽으로 도망치면 팬텀이 뒤에서 쏟아지는 **경계 공황**, 그리고 몬스터와 거리를 벌릴수록 리퍼와 헥스가 더 강해지는 **거리 응징자**입니다.
