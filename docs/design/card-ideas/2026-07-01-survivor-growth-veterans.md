# Card Ideas — 2026-07-01 — 생존자 보상: 오래 버틴 몬스터가 더 강해진다

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 개체 생존 성장 (Individual Survivor Growth) — 특정 몬스터 인스턴스가 일정 시간 살아남으면 그 개체에만 영구 버프 부여. "오래 버틴 개체가 베테랑이 된다" 는 완전히 새로운 성장 레이어.
- **목록**: 전장 고참 (VeteranReaper) / 불멸의 레이스 (UndyingWraith) / 생존의 피 (SurvivorBlood)
- **기존 28장 + 과거 32회차 중복 회피 확인됨**:
  - 기존 28장의 모든 강화 카드는 픽 시점에 **종(種) 전체**에 즉시 적용되는 글로벌 버프 — 개체별 시간 추적 없음.
  - 과거 32회차 전부 검토: 종 간 연계(05-29)/탱크 재생·분열(06-10)/몬스터 생존력(06-22)/선도자 오라(06-23)가 가장 유사하나, 이들은 전부 "살아있는 수" 또는 "픽 시점 발동" 기반. 오늘 3장은 **"같은 스폰에서 태어난 개별 개체의 생존 경과 시간"** 이 트리거 — 개체 시간 추적 개념 자체가 32회차 어디에도 등장 없음.

---

## 1. 전장 고참 (VeteranReaper) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - 리퍼(Reaper) 1마리가 스폰된 후 **15초 연속 생존** 시, 그 리퍼 개체의 공격 쿨다운 ×0.65 영구 적용 (공격속도 약 +54%). 다른 리퍼에게는 전이 없음.
  - 조건: 해당 개체가 15초 동안 한 번도 사망하지 않아야 한다. 15초 도달 순간 단 1회 판정, 이후 추가 성장 없음.
  - 중첩 픽 시 생존 조건 시간 단축: 2픽 → 10초, 3픽 → 6초. 효과 배율은 유지 (×0.65).
  - 밸런스 근거 (컨셉 §8): 리퍼 기본 HP 100, 영웅 DPS 50 → 기본 생존 시간 2초. 플레이어가 15초 생존하는 리퍼를 얻으려면 영웅의 어그로가 다른 곳에 분산되어야 함. 어그로 분산 조건 + 강한 개체 1마리 조합이 성립하면 공격속도 ×1.54 리퍼 — 강하지만 개체 수가 적어 DPS 상한 통제 가능. 2~4분 밴드 안에서 리퍼 2~3마리가 베테랑 상태 도달이 현실적 기대값.

- **구현 패턴**: 스폰 이벤트 → 개체별 타이머 코루틴
  ```
  //# VeteranGrowthService — 스포너에서 Reaper Pop 시 구독
  void OnReaperSpawned(IMonsterInstance reaper):
      StartCoroutine(CheckVeteranAfter(reaper, 15f))

  IEnumerator CheckVeteranAfter(IMonsterInstance reaper, float delay):
      yield return new WaitForSeconds(delay)
      if (reaper == null || reaper.IsAlive == false) yield break
      monsterBuffService.ApplyCooldownScale(reaper, 0.65f)   //# 개체 직접 참조
  ```
  - `IMonsterInstance` 에 `IsAlive` 상태 프로퍼티 필요 (또는 MonoBehaviour null 체크로 대체).
  - `MonsterBuffService.ApplyCooldownScale` 의 현재 시그니처가 종(EMonster) 단위라면, **개체(instance) 단위 오버로드** 1개 추가 필요 — 이것이 이 카드 도입의 핵심 인프라 확장 포인트.
  - 스폰 이벤트는 `IBattleContext.OnMonsterSpawned` 또는 각 Spawner 의 출력 콜백 활용.

- **시너지 후크**:
  - SpawnReapers (Dps P3, 스포너 출력 +1): 리퍼 유량 증가 → 베테랑 자격 도전 개체 수 증가.
  - Frenzy (A, 전체 공속 +50%, 10s): 베테랑 리퍼가 이미 고속인 상태에서 Frenzy 중첩 → 단기 폭발 DPS.
  - IronWill (A, 받는 데미지 ×0.7, 15s): 영웅의 DPS 감소 → 리퍼 생존 시간 연장 → 베테랑 달성 확률 상승. "IronWill로 리퍼를 15초 보호해 베테랑 만들기" 라는 명시적 2-카드 콤보 경로.

- **구현 비용 추정**: 3 (개체 타이머 코루틴 패턴 신규, MonsterBuffService 개체별 오버로드 1개 추가)
- **중복 재검증**: 기존 ReaperAtkSpeed = 픽 즉시 종 전체 쿨다운 ×0.7. VeteranReaper = 15초 생존한 개체 1마리에만 ×0.65 — 트리거(픽 vs 생존 경과), 적용 범위(전체 vs 단일 개체), 성장 조건(없음 vs 생존 시간)이 모두 다름. 과거 32회차 개체 시간 추적 미존재 확인 ✓

---

## 2. 불멸의 레이스 (UndyingWraith) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - 레이스(Wraith) 1마리가 스폰된 후 **30초 연속 생존** 시, 그 레이스 개체에 영구적으로 **HP 상한 +300 부여 및 즉시 회복** (HP 500 → 800). 동시에 이 레이스가 사망 시 다시 소환되지 않고 사망 위치에 Wisp 1마리 스폰.
  - 30초 도달 시 단 1회 판정. 이후 레이스 HP 800 기준으로 정상 교전.
  - "사망 시 Wisp 스폰" 부분은 06-02 WraithRemnant 와 의도적으로 연계 — 두 카드를 함께 픽하면 일반 레이스는 WraithRemnant 로, 베테랑 레이스는 UndyingWraith 내장 효과로 각각 Wisp 탄생.
  - 밸런스 근거 (컨셉 §8): 레이스 기본 HP 500, 영웅 DPS 50 → 기본 생존 10초. 30초 생존은 영웅 어그로를 9번 이상 회피해야 가능한 고난이도 조건. 달성 시 HP 800 레이스 1마리 → 추가 16s 버팀 + 사망 후 Wisp 1마리 보상. 강한 보상이지만 달성 빈도 낮아 밸런스 안정적.

- **구현 패턴**: VeteranReaper 와 동일한 `VeteranGrowthService` 내 Wraith 담당 코루틴 추가
  ```
  //# Wraith 베테랑 체크 — VeteranGrowthService 확장
  IEnumerator CheckUndyingWraithAfter(IMonsterInstance wraith, float delay):
      yield return new WaitForSeconds(delay)
      if (wraith == null || wraith.IsAlive == false) yield break
      monsterBuffService.SetMaxHpBonus(wraith, 300)   //# HP 상한 +300 + 즉시 회복
      wraith.OnDied += () => SpawnWispAt(wraith.LastPosition)
  ```
  - `SetMaxHpBonus(instance, bonus)` — 개체별 HP 상한 조정. MonsterBuffService 개체 오버로드 확장 (VeteranReaper 와 같은 인프라 공유).
  - `wraith.OnDied` 구독은 06-02 WraithRemnant 의 IBattleContext.OnMonsterDied 패턴과 동일.

- **시너지 후크**:
  - WraithDamageBoost (Tank P2, 레이스 HP ×1.5): HP 베이스가 올라가 베테랑 달성 조건(30초 생존)이 더 쉬워짐.
  - SpawnWraith (Tank P3, 스포너 출력 +1): 레이스 유량 증가 → 베테랑 도전 기회 증가.
  - WraithRemnant (06-02 제안 카드): 일반 레이스 사망 → Wisp, 베테랑 레이스 사망 → Wisp (UndyingWraith 내장). Tank 생태계의 죽음-재생 순환 완성.
  - GuardianRage (A, 위스프·레이스 HP ×2, 받는 데미지 ×0.5, 15s): 30초 조건을 GuardianRage 15s 보호로 "보호 구간 확보" 전략.

- **구현 비용 추정**: 3 (VeteranReaper 와 동일 서비스에 Wraith 코루틴 추가 + HP 상한 조정 API — 신규 인프라가 VeteranReaper 와 공유되어 2번째 카드는 추가 비용 낮음)
- **중복 재검증**: 06-10 Tank 재생·분열 회차의 Wisp Fission(위스프 분열)·Wraith Life Drain(흡혈)·Mirror Armor(반사)는 사망/공격 이벤트 기반. 이 카드는 "30초 생존 시 1회 판정 후 HP 확장" — 트리거 시점(사망/공격 vs 30초 경과), 효과 방향(즉발 vs 상태 변화 + 사후 효과)이 다름 ✓. 06-02 WraithRemnant(사망 시 Wisp 소환)와의 연계는 의도적 시너지이며 중복 아님 ✓

---

## 3. 생존의 피 (SurvivorBlood) — 가칭

- **카테고리**: 액티브 버프 (Swarm 축)
- **효과 모델**:
  - 발동 시 현재 필드에서 **가장 오래 살아있는 몬스터 1마리**를 자동 선정 — 해당 개체에 **20초간 공격속도 ×2.0 + 이동속도 ×1.4** 임시 버프.
  - 선정 기준: 스폰 시각 기준 가장 이른 개체(최장 생존). 동점 시 HP 높은 순.
  - 임시 버프 만료 후 개체 능력치는 기본값으로 복원. 베테랑 버프(VeteranReaper/UndyingWraith로 부여된 영구 성장)는 유지.
  - 중첩 픽 시 버프 지속 시간 연장: 2픽 → 30초, 3픽 → 45초.
  - 밸런스 근거 (컨셉 §8): 단일 개체 극단 강화 — 영웅이 그 1마리에 집중하도록 유도. 이 1마리를 빨리 처치하면 효과 만료. 영웅 입장에서 "가장 무서운 것부터 잡는" 기존 어그로 AI를 역이용 — 강화된 개체가 가장 가까이 오면 영웅이 집중 타깃팅. 20초 × 단일 개체 효과이므로 전체 DPS 상승폭은 Frenzy (전체 10s) 보다 좁고 길게.

- **구현 패턴**: IBattleContext.GetMonsters() 정렬 → 개체 버프 시한부 적용
  ```
  //# SurvivorBloodEffect.Apply(ctx)
  IMonsterInstance oldest = ctx.GetMonsters()
      .OrderBy(m => m.SpawnedAt)
      .FirstOrDefault()
  if (oldest == null) return
  monsterBuffService.ApplyTimedBuff(oldest, new MonsterBuff
  {
      CooldownScale = 0.5f,    //# 공격속도 ×2.0 (쿨다운 절반)
      MoveSpeedScale = 1.4f,
      Duration = 20f
  })
  ```
  - `IMonsterInstance.SpawnedAt` — 스폰 시각 저장 프로퍼티. 신규 추가 필요 (VeteranGrowthService 에서도 공유).
  - `MonsterBuffService.ApplyTimedBuff(instance, buff)` — 개체별 시한부 버프. 기존 종(種) 단위 버프와 달리 만료 후 원복 로직 필요. VeteranReaper/UndyingWraith 용 개체 오버로드와 같은 인프라 블록에서 구현.
  - 시한부 버프 만료는 `UniTask.Delay` 또는 MonoBehaviour coroutine 으로 처리.

- **시너지 후크**:
  - VeteranReaper (패시브, 이번 3장 中 1): 이미 베테랑 상태인 리퍼가 최장 생존 개체일 가능성 높음 → SurvivorBlood 선정 시 쿨다운 ×0.65 × ×0.5 = ×0.325 (기본 대비 공속 약 3배). "베테랑 리퍼에게 SurvivorBlood 발동" 콤보 루트.
  - UndyingWraith (패시브, 이번 3장 中 2): 30초 이상 생존한 레이스가 최장 생존 개체 → HP 800 + ×2.0 속도·공속 → 영웅의 집중 타깃이 되지만 HP 여유로 버팀.
  - TimeStop (A, 영웅 5초 정지) + SurvivorBlood: 정지 구간 동안 베테랑 개체가 무주공산에서 공격 — 5초 × ×2.0 공속 = 기본 10초분 DPS.

- **구현 비용 추정**: 3 (GetMonsters 정렬 쿼리 + SpawnedAt 프로퍼티 신규 + 개체별 시한부 버프 인프라)
- **중복 재검증**: 기존 Frenzy (전체 공속 +50%, 10s) / IronWill (전체 받는 데미지 ×0.7, 15s) 등 액티브 버프는 전체 또는 종 단위 적용. SurvivorBlood = 최장 생존 개체 1마리 선정 + 단일 개체 집중 강화 — "단일 개체 자동 선정 메커니즘" 과 "생존 시간 기반 선정 기준" 이 새로운 축 ✓. 06-23 선도자 오라(N마리 이상 필드에 있을 때 버프)와도 조건 기준(수량 vs 시간)이 다름 ✓

---

## 4. 공통 테마 고찰

### 왜 "개체 생존 성장" 인가

현재 28장의 모든 버프 카드 효과는 **픽 시점에 종(種) 전체에 즉시 적용되거나, 발동 후 N초간 전체에 적용**된다.

| 레이어 | 기존 예시 | 오늘 3장 |
|---|---|---|
| 픽 즉시 · 종 전체 | WispHpBoost, ReaperAtkSpeed | — |
| 발동 후 N초 · 전체 | Frenzy, IronWill | — |
| **생존 경과 시간 · 개체 단위** | *없음* | **VeteranReaper / UndyingWraith / SurvivorBlood** |

이 축이 없으면 "어떤 개체가 얼마나 오래 살아있었는가" 라는 정보가 게임에서 전혀 의미를 갖지 않는다. 개체 성장 카드를 도입하면 플레이어는 자연스럽게 "우리 팀의 베테랑 개체를 보호하자" 는 새로운 전략 목표를 갖게 된다.

### 이 테마가 열어주는 전략 공간

- **다분산 어그로 운영**: 영웅 어그로를 여러 종에 분산시켜 각 개체 생존 시간 확보 — Swarm/Debuff 축 카드와 자연 연계.
- **베테랑 보호 빌드**: IronWill/GuardianRage 로 특정 개체를 의도적으로 보호해 베테랑 달성 → 달성 후 강력한 개체 1마리가 핵심 딜러/탱커 역할.
- **시간 차 전략**: 초반에 소환 카드 다수 픽 → 개체 유량 확보 → 그 중 일부가 베테랑 달성 → 중후반 SurvivorBlood 로 베테랑에 집중 강화.

### QA 연계

QA 시뮬레이션 미실행 상태(2026-05-22 리포트 — 인프라 훅 미구축)이지만, 카드 구조 분석 기준:
- 현재 Dps 축의 액티브 카드 3장(Frenzy/BloodThirst/MarkOfDeath)은 모두 "넓게 퍼붓는" 방향. "특정 개체 집중 강화" 경로가 없어 Dps 축 내 전략 다양성이 제한됨 — VeteranReaper/SurvivorBlood 가 이 공백 보완.
- Tank 축 UndyingWraith 는 "레이스가 오래 살수록 더 강해진다" 는 롱런 탱킹 빌드를 처음 제시 — Tank 축 게임플레이 다변화 기여.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- v0.2 진입 전까지 backlog 보관
- **구현 공통 인프라**: 세 카드 모두 `IMonsterInstance.SpawnedAt` + `MonsterBuffService` 개체별 오버로드를 공유하므로 1회 인프라 스프린트에 묶어 처리 권장.
  - 1단계: `IMonsterInstance.SpawnedAt` 프로퍼티 추가 + `MonsterBuffService` 개체 오버로드 추가 (공통 인프라)
  - 2단계: `VeteranGrowthService` 구현 (VeteranReaper + UndyingWraith 담당 코루틴)
  - 3단계: `SurvivorBloodEffect` 구현 (정렬 쿼리 + 시한부 개체 버프)
- **밸런스 검증 우선순위**: VeteranReaper (조건 단순, 수치 검증 용이) → SurvivorBlood (단일 개체 강화 폭 확인) → UndyingWraith (30초 조건 달성 빈도 측정).

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터들은 모두 똑같이 태어나서 똑같이 싸우다 죽습니다. 아무리 용감하게 오래 싸워도 특별히 강해지는 몬스터는 없죠. 오늘 제안하는 카드들은 바로 그 부분을 바꿉니다 — 마치 RPG에서 레벨업을 하듯이, 오래 살아남은 몬스터가 "베테랑"이 되어 더 강해지는 것입니다. 리퍼가 15초 동안 영웅에게 안 죽고 버티면 공격이 훨씬 빨라지고, 레이스가 30초를 버티면 HP가 더 늘어나며, 가장 오래 살아있는 몬스터에게 특별한 강화를 몰아주는 카드도 있습니다. 그래서 오늘 제안하는 카드 3장은: "전장 고참"(오래 버틴 리퍼가 공격속도 대폭 증가) / "불멸의 레이스"(30초 생존 시 HP 확장 + 죽어도 위스프 탄생) / "생존의 피"(현재 가장 오래 살아있는 몬스터에게 일시적 특급 강화).
