# Card Ideas — 2026-06-18 — 스포너 다양성 보상: 6종을 고루 유지한 빌드만 받는 혜택 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 스포너 다양성 보상 — 현재 28장 + 과거 20회차 제안 어디에도 "활성 스포너 종류 수" 자체를 보상 자원으로 활용하는 카드가 없다. ReplaceXxx 카드들이 "단일 종 집중"을 유도하는 것과 정반대로, 6종 스포너를 고루 유지할수록 강력해지는 3종 세트.
- **목록**: DiverseHaste (다종 군속) / HarmonyHeal (조화 치유) / CombinedDeploy (합동 배치)
- **기존 28장 + git log 과거 20회차와의 중복 회피 확인됨**
  - 기존 28장: SpawnerHaste(스포너 주기 ×0.8, 다양성 무관), WallOfWisps(단일 종 즉시 소환), ReplaceXxx(교체로 집중 유도) — 스포너 다양성을 보상하는 카드 없음 ✅
  - 과거 20회차 검토:
    - 5/28 전장 상태 감지 (Horde Roar): **필드 생존** 몬스터 종류 수 기반 공속 버프 — 오늘 3장은 **스포너 구성 설정**(생산 설정값) 기반. 데이터 소스가 다름.
    - 5/29 종 간 연계: 몬스터 공존 조건 ON/OFF — 필드 상태 조건, 스포너 구성 아님.
    - 6/11 크로스 축 스포너 전환: 스포너 종 **교체 행위** 자체가 카드 효과 — 오늘 카드는 교체 없이 **다양성 유지**가 보상 조건 (반대 방향).
    - 6/13 즉시 소환 전술: SwarmRush=Phantom, PlagueCloud=Plague, ReaperStrike=Reaper (단일 종) — CombinedDeploy는 활성 스포너 **모든 종**에서 소환.
    - 나머지 16회차: HP·Power·Speed 강화, 도주/킬 처벌, 낙인, 와일드, 딜러 내구도 등 — 스포너 구성 다양성과 무관.
  - 오늘 3장: "활성 스포너 종류 수"를 보상 자원으로 삼는 첫 제안 ✅

---

## 1. DiverseHaste (다종 군속) — 가칭

- **카테고리**: 패시브 환경 (Swarm 축)
- **효과 모델**:
  - 활성 스포너 종류가 **4가지 이상**일 때: 전 종 몬스터 이동속도 영구 ×1.15 적용.
  - ReplaceXxx 등으로 스포너 종류가 3가지 이하로 줄면: 조건 미충족 → 효과 자동 비활성 (MoveSpeed 배율 1.0 환원).
  - 조건 실시간 재평가: 스포너 교체 카드 픽 시마다 재체크.
  - 중첩 픽: 2픽 시 배율 ×1.15 → ×1.30 누적 (픽마다 +0.15), 임계치 4종 유지.
  - **밸런스 근거 (컨셉 §8)**: PhantomMoveSpeedBoost(Phantom만 ×1.5 영구) 대비 — 이 카드는 전 종 ×1.15로 폭이 넓지만 강도 낮음. 4종 유지 조건이 붙어 ReplaceXxx와 역방향 트레이드오프. Swarm Tier1 시너지(×1.3)와 중첩 시 Phantom ×1.3×1.15≈×1.495 — 단일 집중(×1.5)에 근접하면서 다른 종도 함께 가속.
- **구현 패턴**: `DiverseHasteEffect.cs` → Apply 시 `DiverseHasteService` (런 내 1회 Add) 등록.
  - `DiverseHasteService`: `IBattleContext.OnSpawnerConfigChanged` 구독 → `GetActiveSpawnerKindCount()` 체크 → 조건 충족 시 `MonsterBuffService.SetGlobalMoveSpeedAll(1.0f + stackCount * 0.15f)`, 미충족 시 `SetGlobalMoveSpeedAll(1.0f)`.
  - `IBattleContext.GetActiveSpawnerKindCount()` 신규 API 필요 (미존재 시): SpawnerRegistry 에서 활성 스포너 종 Distinct count 반환 — 소규모 추가.
  - 기존 참조: SpawnerHasteEffect.cs (스포너 대상 수정), PlagueSlowBoostEffect.cs (MonsterBuffService 패턴).
- **시너지 후크**:
  - **SpawnX 계열 (SpawnWisps·SpawnPhantoms·SpawnReapers·SpawnPlagues·SpawnWraith)**: 스포너 다양화 유지 → 4종 조건 달성 용이. 이 카드들과 세트로 "다양성 보존 빌드" 완성.
  - **ReplaceXxx 교체 계열 (ReplaceWispsToWraith·ReplaceReapersToHex)**: DiverseHaste 효과 조건과 직접 충돌 → 교체할수록 효과 소멸. 전략적 갈림길.
  - **Swarm Tier1 시너지**: Phantom+Wisp 이동속도 ×1.3 + DiverseHaste 조건 충족 시 추가 ×1.15 중첩 → Swarm 빌드 심화.
- **구현 비용 추정**: 3 (IBattleContext 신규 API 1개 + 동적 조건 구독 서비스 + MonsterBuffService 전 종 배율 적용)
- **중복 재검증**:
  - SpawnerHaste(기존): 스포너 주기 ×0.8 고정 수치, 다양성 조건 없음, 효과 대상 스포너 주기. 이 카드: 4종 이상 조건, 몬스터 이동속도 — 조건·대상·효과 모두 다름 ✅
  - Horde Roar (5/28): 필드 생존 몬스터 종류 수 기반 공속 버프. 이 카드: 스포너 구성 설정 기반 이속 버프 ✅

---

## 2. HarmonyHeal (조화 치유) — 가칭

- **카테고리**: 패시브 강화 (축 카운트 미포함 — 모든 축 호환)
- **효과 모델**:
  - **픽 즉시 스냅샷**: 현재 활성 스포너 종류 수 × **5%** 만큼 현재 필드 전 몬스터 HP 즉시 회복.
  - 6종 활성: 6 × 5% = **+30% HP** 즉각 회복 (필드 전체)
  - 4종 활성: +20% HP 회복
  - 1종 활성: +5% HP 회복 (단일 집중 빌드 페널티)
  - 회복 상한: 최대 HP 초과 불가 (IHealth.Heal 기본 동작).
  - 중첩 픽: 각 픽 시점 스포너 종류 수 재스냅샷 → 독립 발동 (픽할 때마다 그 시점 종류 수 × 5% 재적용).
  - **밸런스 근거 (컨셉 §8)**: BloodThirst(처치마다 주변 몬스터 HP +30, 30s 창)와 비교 — 이 카드는 한 번에 전 필드 몬스터를 일괄 회복 (6종 시 최대 30%). 교전 중 지속 보충(BloodThirst) vs 픽 시점 일괄 회복(이 카드). 다종 빌드에서만 효율적이라 밸런스 자기 제한.
- **구현 패턴**: `HarmonyHealEffect.cs` → Apply 즉시 실행 (구독 불필요, apply-once 패턴):
  ```csharp
  int kindCount = ctx.GetActiveSpawnerKindCount();
  float healRatio = kindCount * 0.05f;
  foreach (IHealth monster in ctx.GetAllAliveMonsters())
  {
      monster.Heal(monster.MaxHp * healRatio);
  }
  ```
  - `IBattleContext.GetActiveSpawnerKindCount()` — DiverseHaste와 동일 API 공유.
  - `IBattleContext.GetAllAliveMonsters()` — 기존 `GetMonsters(EMonster.X)` 를 전 종 순회하는 wrapper (소규모 추가) 또는 6종 개별 순회.
  - `IHealth.Heal(float)` — BloodThirstEffect 사용 패턴 동일.
- **시너지 후크**:
  - **WispHpBoost + WraithDamageBoost (HP ×1.5)**: 최대 HP 상승 → Heal 절대값 증가. 탱커 빌드에서 회복 극대화.
  - **GuardianRage (HP ×2.0, 15s 창)**: 창 도중 픽 시 대폭 상승된 MaxHp 기준 30% 회복 → 창 활용 극대화.
  - **DiverseHaste (오늘 카드 1번)**: 두 카드 모두 스포너 다양성 조건 공유 → "6종 유지 빌드"의 핵심 패시브 쌍.
- **구현 비용 추정**: 2 (DiverseHaste API 공유 + IHealth.Heal 기존 패턴 — 신규 시스템 없음)
- **중복 재검증**:
  - BloodThirst(기존): 처치 이벤트마다 주변 몬스터 +30 HP, 30s 창. 이 카드: 픽 즉시 전 필드 몬스터 MaxHp 비율 회복, 다양성 조건. 트리거·대상·지속·수치 모두 다름 ✅
  - Wraith Life Drain (6/10 제안): Wraith 공격 시 자가 흡혈 +20. 이 카드: 전 종 일괄 즉발 회복, 다양성 조건. 무관 ✅

---

## 3. CombinedDeploy (합동 배치) — 가칭

- **카테고리**: 액티브 와일드
- **효과 모델**:
  - 발동 시 **활성 스포너 각 종에서 몬스터 2마리 즉시 소환** (CHMPool.Pop, 글로벌 캡 체크 후).
  - 3종 활성: 6마리 / 4종: 8마리 / 5종: 10마리 / 6종: **12마리** 즉시 배치.
  - 글로벌 캡(기본 18) 초과분 억제: 캡 잔여 슬롯 내에서만 소환 (자연 제한).
  - 단일 종 집중 빌드(1~2종): 2~4마리만 — 약한 발동. 다양성 보상이 자연스럽게 차등화됨.
  - 소환 위치: 스포너 반경 기준 랜덤 배치 (기존 SpawnWraithEffect 패턴 동일).
  - **밸런스 근거 (컨셉 §8)**: WallOfWisps(Wisp 4마리 즉시, 액티브 Tank) 대비 — 6종 시 최대 12마리지만 Phantom(HP 30) 등 약한 종도 포함되어 실효 위협은 WallOfWisps 3배보다 낮음. 글로벌 캡이 실질 상한. 다양성 빌드에서만 WallOfWisps를 초과.
- **구현 패턴**: `CombinedDeployEffect.cs`:
  ```csharp
  List<EMonster> activeKinds = ctx.GetActiveSpawnerKinds();
  int remainCap = ctx.GetGlobalCap() - ctx.GetTotalMonsterCount();
  int totalToSpawn = Mathf.Min(activeKinds.Count * 2, remainCap);
  int spawned = 0;
  foreach (EMonster kind in activeKinds)
  {
      if (spawned >= totalToSpawn) break;
      for (int i = 0; i < 2 && spawned < totalToSpawn; i++, spawned++)
      {
          GameObject prefab = ctx.GetMonsterPrefab(kind);
          if (prefab == null) continue;
          CHPoolable p = CHMPool.Instance.Pop(prefab, parent);
          p.transform.position = ctx.GetSpawnPosition(kind);
      }
  }
  ```
  - `IBattleContext.GetActiveSpawnerKinds()` — GetActiveSpawnerKindCount의 List 버전 (동일 API 확장).
  - `IBattleContext.GetGlobalCap()` / `GetTotalMonsterCount()` — WispFission(6/10 제안)에서도 필요 API.
  - `CHMPool.Instance.Pop` — Rule 03 §4 준수 (Object.Instantiate 금지).
  - `ctx.GetMonsterPrefab(kind)` — 기존 SpawnWraithEffect 에서 프리팹 조회 패턴 참조.
- **시너지 후크**:
  - **SpawnX 계열 5장**: 스포너 다양화 유지·확장 → CombinedDeploy 소환 수 증가 → 확장 투자의 "폭발 보상".
  - **Swarm Tier3 (글로벌 캡 +6, 18→24)**: 캡 여유 증가 → CombinedDeploy 최대 12마리 캡 제한 완화 → 전원 소환 자주 가능.
  - **DiverseHaste + HarmonyHeal (오늘 1·2번)**: 셋 모두 "스포너 다양성" 조건 공유 → 6종 유지 빌드의 완성 패키지. 30초마다 CombinedDeploy로 전군 투입, DiverseHaste로 전군 가속, HarmonyHeal로 다음 픽마다 HP 일괄 회복.
- **구현 비용 추정**: 3 (GetActiveSpawnerKinds 신규 API + 반복 CHMPool.Pop + 캡 체크 + 위치 계산)
- **중복 재검증**:
  - WallOfWisps(기존): Wisp 단일 종 4마리 즉시 소환. 이 카드: 활성 스포너 모든 종 각 2마리, 다양성 조건. 종 구성·소환 로직·발동 조건 모두 다름 ✅
  - SwarmRush (6/13 제안): Phantom 6마리 즉시 소환 (단일 종). 이 카드: 활성 모든 종 × 2 ✅
  - 6/11 크로스 전환: 스포너 종류를 교체하는 패시브 카드. 이 카드: 현재 구성에서 즉시 소환하는 액티브 ✅

---

## 4. 공통 테마 고찰

**왜 "스포너 다양성 보상" 테마인가?**

현재 28장 카드 구조는 전략적으로 "한 축 집중"을 암묵적으로 장려한다:
- SpawnX 계열: 특정 종 스포너 증설
- ReplaceXxx 계열: 약한 종을 강한 종으로 전환
- 4축 시너지 (Tier 1-3): 같은 축 카드를 많이 픽할수록 보상

"다양성 유지" 전략은 현재 아무 보상 없이 존재한다. 오늘 3장이 이 전략에 처음으로 명확한 인센티브를 부여한다:
- DiverseHaste: 4종 이상 유지 시 전군 가속 (단일 집중 빌드와 경쟁 가능한 속도 버프)
- HarmonyHeal: 픽마다 스포너 다양도 비례 HP 회복 (집중 빌드는 약하게 발동)
- CombinedDeploy: 6종 유지 빌드에서만 WallOfWisps를 뛰어넘는 즉시 배치

**QA 공백 연결**: 유일한 QA 리포트(2026-05-22)는 자동 픽 후크 미구현으로 시뮬이 차단돼 픽률 데이터 없음. 그러나 컨셉 §5.2 4축 시너지 설계 의도("빌드 다양성 검증")를 보면 현재 카드 풀이 단일 축 특화를 구조적으로 유도한다. 오늘 카드들은 "다양성 유지" 라는 대안 전략의 첫 기반을 제공한다.

**신규 API 공유**: 오늘 3장 모두 `IBattleContext.GetActiveSpawnerKindCount()` / `GetActiveSpawnerKinds()` 에 의존 → API 1회 추가로 3장 동시 착수 가능. 시너지 높은 일괄 채택 후보.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- `IBattleContext.GetActiveSpawnerKinds()` / `GetActiveSpawnerKindCount()` API 미존재 시 gameplay-programmer에게 IBattleContext 확장 의뢰 (소규모)
- 세 카드 모두 동일 신규 API 의존 → 한 번 API 추가 후 3장 동시 착수 가능
- v0.2 풀 확장(패시브 30~40장, 액티브 20~30장) 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 이 게임에서 카드를 고를 때, 대부분의 선택은 특정 몬스터를 집중 육성하는 쪽으로 흘러갑니다. 위스프를 잔뜩 뽑거나, 팬텀만 쏟아내거나, 리퍼만 강화하거나 하는 식이죠. 오늘 제안하는 카드 3장은 반대 방향을 봅니다 — "여러 종류의 몬스터를 골고루 유지하면 특별한 보너스가 생긴다"는 전략입니다. 마치 축구팀에 골키퍼·미드필더·스트라이커가 골고루 있을 때 팀 전체가 더 잘 움직이는 것처럼요. 그래서 오늘 제안하는 카드 3장은: 다양한 종류의 몬스터들이 모두 더 빠르게 달리게 해주는 카드, 싸우다가 HP가 깎인 몬스터들을 일제히 회복시켜 주는 카드, 그리고 현재 활동 중인 모든 종류의 몬스터를 동시에 2마리씩 즉시 소환하는 카드입니다.
