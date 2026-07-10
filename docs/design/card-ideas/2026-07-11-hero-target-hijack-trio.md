# Card Ideas — 2026-07-11 — 영웅의 자동 타겟 AI를 교란해 엉뚱한 곳을 공격하게 만드는 납치 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 영웅 타겟 납치 (Hero Target Hijack) — 영웅이 "가장 가까운 몬스터"를 쫓는 기본 AI를 카드로 재정의해, 엉뚱한 곳을 향해 달려가거나 죽이기 어려운 목표만 맹목적으로 공격하게 만드는 3종
- **목록**: MazeWhisper (미로의 속삭임) / AnchorBait (철벽 미끼) / ShadowCall (그림자의 부름)
- **기존 28장 + 과거 43회차와의 중복 회피 확인됨**
  - 기존 28장 검토: IHeroAura 계열은 FleshWound·HeroAttackDown 등 스탯 조작에 국한. 타겟 AI 자체를 바꾸는 카드는 전무.
  - 과거 43회차 검토:
    - 06-08 escape-punishment: 도주(이탈) 패턴 처벌 — 오늘과 다름: 오늘은 타겟 선택 로직 치환, 도망이 아님
    - 06-04 dps-debuff-prey-hunt: DPS+Debuff 연계로 영웅을 빠르게 처치 — 오늘과 다름: 오늘은 몬스터 강화 아닌 영웅 AI 납치
    - 07-04 phantom-blind-pressure: 팬텀 시야 차단 상태 활용 — 오늘과 다름: 오늘은 팬텀이 아닌 강제 이동 목표
    - 06-20 wounded-hero-punisher, 06-19 영웅 저체력 포식자: HP 비율 조건부 스탯/스폰 — 오늘과 다름: 오늘은 AI 이동 목표 강제 설정
    - 나머지 39회차 전부 확인: "영웅의 FindTarget 로직을 Override 하는 카드" 없음 ✅

---

## 1. MazeWhisper (미로의 속삭임) — 가칭

- **카테고리**: 액티브 저주 / Debuff 축
- **효과 모델**:
  - 즉발: **10초간** 영웅의 자동 타겟 우선순위를 "가장 가까운 몬스터" → **"현재 필드에서 HP가 가장 높은 몬스터"** 로 강제 전환.
  - 영웅이 HP 500짜리 Wraith를 향해 맹목적으로 달려가는 동안 HP 30~100짜리 Reaper·Phantom·Hex는 아무 방해 없이 DPS를 꽂아넣는다.
  - 중첩 픽 시 지속 시간 +4s씩 누적 (2픽→14s, 3픽→18s). 배율 불변.
  - **밸런스 검증**: 영웅 기본 ATK 50/s. Wraith HP 500 → 10타에 처치, 약 10s 소요. 즉 MazeWhisper 10s 구간 동안 영웅은 Wraith 하나에 전념 → 나머지 몬스터 10s 자유 DPS. 평균 필드 DPS(Reaper ×2 + Hex ×1 + Plague 둔화) ≈ 30×10s = 300 HP 추가 압박. 영웅 HP 1000 기준 의미 있는 기여.
- **구현 패턴**:
  - `IHeroAura` 에 `ETargetPriority TargetOverride` 프로퍼티 추가 (기본 `ETargetPriority.Nearest`).
  - `MazeWhisperEffect.Apply()` → `heroAura.TargetOverride = ETargetPriority.MaxHP`, 10s 코루틴 후 `Nearest` 복귀.
  - `HeroController.FindTarget()` 내부에서 `heroAura.TargetOverride` 분기: `MaxHP` 이면 `IBattleContext.GetAllMonsters().MaxBy(m => m.Hp)` 반환.
  - 기존 `FearEffect` (도주 5s) 와 구조 유사 — 영웅 행동을 일시 치환하는 동일 패턴.
- **시너지 후크**:
  - `WraithDamageBoost` (Wraith HP ×1.5) + MazeWhisper: Wraith HP 750으로 늘어나면 영웅이 15s 넘게 Wraith만 쳐야 처치 가능 → 저주 지속과 무관하게 기회 창 연장.
  - `IronWill` (받는 데미지 ×0.7, 15s) + MazeWhisper: 영웅이 Wraith를 맹공하는 구간에 IronWill로 Wraith 생존율↑ → 영웅이 더 오래 Wraith에 묶임.
  - Debuff 축 Tier2 자동 발동 (`HeroAttackDown` ×0.85 영구) 이후 MazeWhisper 추가: 약해진 영웅이 더 긴 시간 Wraith에 맞물림.
- **구현 비용 추정**: 3 — `IHeroAura.TargetOverride` enum 신규 + `HeroController.FindTarget` 분기. FearEffect 패턴 참고 시 핵심 로직 자체는 단순. 동일 API를 Card 2·3도 재사용하므로 1회 구축 비용 공유.
- **중복 재검증**: 기존 Fear(3s 도주)·TimeStop(5s 정지)·Slow(이속 ×0.5) 모두 "영웅 기동" 조작이지, "타겟 FindTarget 로직 치환" 아님. 과거 43회차 어디에도 MaxHP-priority override 없음 ✅

---

## 2. AnchorBait (철벽 미끼) — 가칭

- **카테고리**: 패시브 강화 / Tank 축
- **효과 모델**:
  - Wraith 가 스폰될 때마다 해당 Wraith 에게 **루어 마커(Lure Marker)** 를 자동 부착.
  - 루어 마커가 있는 Wraith 는 영웅의 FindTarget 에서 "거리 가중치" 를 0 으로 덮어써 항상 최우선 타겟이 됨 — 실제 거리와 무관하게 영웅이 맹추격.
  - 마커된 Wraith 가 처치되면 마커가 다음으로 스폰된 Wraith 에게 자동 이동. Wraith 가 필드에 없으면 마커 비활성, 다음 스폰 시 재활성.
  - 중첩 픽 시 마커 동시 보유 수 +1 (2픽 = Wraith 2마리에 마커 → 영웅이 두 마리를 번갈아 쫓음).
  - **밸런스 검증**: Wraith HP 500 → 영웅 1:1 집중 공격 10s 처치. SpawnWraith 로 Wraith 스포너 2개 이상이면 Wraith 상시 필드 유지 가능 → 영웅이 계속 Wraith에 묶임. Reaper·Hex·Phantom 이 그 10s 동안 자유 DPS 30~50 누적.
- **구현 패턴**:
  - `MonsterBuffService.SetLureMarker(IMonster wraith)` — Wraith 컴포넌트에 bool 플래그 부착.
  - `HeroController.FindTarget()`: `IBattleContext.GetAllMonsters()` 순회 중 `IsLureMarked == true` 인 것이 있으면 즉시 반환 (거리 무시). 없으면 기존 Nearest 로직.
  - `AnchorBaitEffect` 는 `IBattleContext.OnMonsterSpawned` 이벤트 구독 → `EMonster.Wraith` 이면 마커 부착. `OnMonsterDied` 이벤트 → 마커 이전.
  - `IHeroAura.TargetOverride` 와 달리 "전역 정책" 대신 "개별 유닛 태그" 방식 — 시스템이 별도로 더 가벼움.
- **시너지 후크**:
  - `SpawnWraith` (Wraith 스포너 동시 출력 +1) + AnchorBait: Wraith 공급이 늘수록 루어 연속 이어짐.
  - `GuardianRage` (Wraith HP ×2.0, 15s) + AnchorBait: 루어된 Wraith 가 HP 1000으로 두꺼워지면 영웅이 20s 가까이 한 마리에 묶임.
  - `ReplaceWispsToWraith` + AnchorBait: Wisp 스포너까지 Wraith 로 전환하면 링 전체에서 루어 소스 증가.
- **구현 비용 추정**: 2 — 개별 유닛 bool 플래그 + FindTarget 최우선 분기. `OnMonsterSpawned`/`OnMonsterDied` 이벤트는 BloodThirstEffect·AshSpawn 계열이 이미 활용 중인 패턴 재사용. `IHeroAura` 수정 불필요.
- **중복 재검증**: 기존 25회차 파일 중 07-07(WispFlood 등 종별 밀도 보너스)은 "필드 마릿수 임계 → 종 강화" — 오늘은 "스폰 이벤트 → 개별 유닛 루어 마커 → 영웅 AI 우선순위 치환". 메커니즘·축 모두 다름 ✅

---

## 3. ShadowCall (그림자의 부름) — 가칭

- **카테고리**: 액티브 와일드 / Swarm 축
- **효과 모델**:
  - 즉발: **5초간** 영웅의 강제 이동 목표를 "현재 필드에서 영웅과 가장 멀리 떨어진 Phantom" 의 위치로 고정.
  - 영웅이 그 Phantom 을 향해 링 외곽으로 달려나가고, 모든 다른 몬스터들이 영웅 뒤를 쫓아가는 역추격 구도.
  - 5초 후 강제 이동 해제, 영웅은 즉시 기존 "가장 가까운 몬스터" 로 타겟 복귀.
  - 필드에 Phantom 이 없으면 효과 발동 안 함(스킵) — 안전장치.
  - 중첩 픽 불가 (발동 후 즉시 소진). 이후 픽은 독립 발동.
  - **밸런스 검증**: 영웅 이속 × 1.0 기준 5s 동안 이동 거리 ≈ 링 반지름(약 8u) × 0.6 = 4.8u. 영웅이 실질적으로 링 중간~외곽까지 달려나감. 이동 중 공격 정지 → 5s 완전 DPS 차단 + 이동 중 옆에 있는 몬스터들에게 피격 가능 = 유효 HP 감소 30~80 추산.
- **구현 패턴**:
  - `IHeroAura` 에 `IMonster ForcedChaseTarget` 프로퍼티 추가 (null 이면 기본 동작).
  - `ShadowCallEffect.Apply()`:
    - `IBattleContext.GetAllMonsters().Where(EMonster.Phantom).MaxBy(m => dist(hero, m))` 로 타겟 탐색.
    - null 이면 skip; 아니면 `heroAura.ForcedChaseTarget = farthestPhantom`.
    - 5s 코루틴 후 `ForcedChaseTarget = null`.
  - `HeroController.FindTarget()`: `ForcedChaseTarget != null && target.IsAlive` 이면 해당 반환. 죽으면 null 처리 후 기본 복귀.
  - Card 1(MazeWhisper) 과 동일 `IHeroAura` 확장 신호를 활용하므로 구축 비용 공유 (1회 추가 후 재사용).
- **시너지 후크**:
  - `PhantomMoveSpeedBoost` (Phantom 이속 ×1.5) + ShadowCall: 타겟 Phantom 이 빠르게 움직이면 영웅이 더 오래, 더 멀리 쫓아감.
  - `SpawnPhantoms` (Phantom 스포너 출력 +1) + ShadowCall: Phantom 공급 증가 → ShadowCall 발동 보장 + 잦은 재사용 기대.
  - Swarm 축 Tier2 (스포너 주기 ×0.85) 이후 ShadowCall: 빠르게 보충되는 Phantom 군단 + ShadowCall 로 영웅이 외곽을 쫓다가 포위됨.
- **구현 비용 추정**: 3 — `ForcedChaseTarget` 추가 + `HeroController` 분기. MazeWhisper 의 `IHeroAura.TargetOverride` 작업과 동시 진행 시 실효 비용 2.
- **중복 재검증**: 07-04(ShadowVeil·DarkPursuit·PanicCloud) — 팬텀 시야 차단 상태를 "조건 트리거"로 활용. 오늘 ShadowCall 은 팬텀을 "강제 이동 목표"로 지정하는 행동 납치. 발동 메커니즘·효과 방향 모두 다름. 기존 Slow(영웅 이속 ×0.5)·TimeStop(5s 정지) 과도 구분: 오늘은 방향성 있는 "지정 목표 강제 추격", 정지나 단순 감속 아님 ✅

---

## 4. 공통 테마 고찰

### 왜 "영웅 타겟 납치" 인가

기존 28장 + 43회차 누적 아이디어를 통틀어 영웅 강화/약화는 항상 **스탯(공격력·이속·방어력·출혈HP 감소)** 또는 **상태이상(도주·정지·시야차단)** 에 국한됐다. 영웅의 AI 판단 — "누구를 치러 갈 것인가" — 을 건드린 제안은 없었다.

세 카드는 영웅 AI 의 `FindTarget()` 레이어에 개입한다는 공통 골격을 갖지만, 방식은 각각 다르다:

| 카드 | 개입 방식 | 지속 형태 |
|---|---|---|
| MazeWhisper | 전역 타겟 정책 치환 (MaxHP 우선) | 액티브, 10s 타이머 |
| AnchorBait | 개별 유닛 루어 마커 → 최우선 반환 | 패시브, 상시 (Wraith 생존 동안) |
| ShadowCall | 특정 유닛으로 강제 이동 목표 고정 | 액티브, 5s 타이머 |

### 왜 지금 이 테마인가

QA 리포트가 BLOCKED 상태(자동 픽 훅 미구현)라 픽률 데이터가 없다. 따라서 기존 카드의 **구조적 공백**을 기준으로 삼았다. 현재 덱은 "몬스터를 더 강하게/더 많이" 또는 "영웅 스탯을 약하게"라는 두 레이어만 있다. "영웅이 어디를 공격하는가"를 바꾸는 레이어가 완전히 비어 있으므로 v0.2 풀 확장 시 필연적으로 필요한 공간이다.

---

## 5. 채택 흐름 제안

- 채택 시 **game-designer** 호출 입력으로 이 문서를 전달 + `docs/design/card-renewal.md` §3·§4 참조.
- `IHeroAura.TargetOverride` 및 `ForcedChaseTarget` 는 MazeWhisper·ShadowCall 공동 구축 — **gameplay-programmer** 에게 두 카드를 동일 Task 로 묶어 의뢰 권장.
- AnchorBait 는 독립 구현 가능 (IHeroAura 미수정, OnMonsterSpawned 훅만 필요).
- v0.2 진입 전까지 backlog 보관.

---

## 6. 쉬운 설명 (비개발자 요약)

보통 게임 속 영웅은 "가장 가까이 있는 적을 자동으로 공격"한다. 그 영리한 기본 규칙을 오늘 제안하는 카드 세 장이 각각 다르게 훼방 놓는다. 미로의 속삭임은 영웅에게 마법을 걸어 "무조건 가장 체력이 많은 덩치 큰 놈"만 쫓게 만든다. 철벽 미끼는 레이스 몬스터 하나에 표적 딱지를 붙여 영웅이 그 몬스터만 집착하게 한다. 그림자의 부름은 영웅을 던전 구석 팬텀 하나를 향해 전력 질주하게 만들어 5초 동안 나머지 몬스터들이 마음껏 공격하도록 한다. 그래서 오늘 제안하는 카드 3장은: 영웅이 아무리 "자동으로 잘 싸운다"고 해도 던전 주인이 방향을 꺾어버리면 엉뚱한 곳을 공격하다가 역포위당하게 만드는 "납치 3종 세트"다.
