# 동시 몬스터 캡 제거 + 액티브 트리거 분단위 축소 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.
>
> **Project Lair 특수:** 본 plan 은 start-develop 파이프라인(game-designer → design-reviewer → ⛔승인 → gameplay-programmer → code-reviewer → test-engineer)의 입력으로도 쓰인다. **Rule 01 준수** — 각 Task 의 commit 스텝은 `git add`(스테이징)까지만, 실제 `git commit` 은 마무리 단계에서 사용자 승인 후. 테스트 실행은 Unity Test Framework(EditMode/PlayMode 러너)로 수행.

**Goal:** `BattleController` 의 동시 몬스터 캡(`_monsterCap`)을 개념째 제거하고, 액티브 카드 트리거를 분단위 4개를 뺀 5개({30,90,150,210,270}초)로 축소한다.

**Architecture:** (A) 캡 enforcement·필드·증가 API 를 전량 삭제하고 그에 종속된 Tank Tier3 시너지를 Wisp+Wraith 추가 내구 버프로 교체. (B) 액티브 임계점 배열을 런타임 진실원(`BalanceConfig.asset`)과 코드 기본값 2곳에서 동기화 갱신.

**Tech Stack:** Unity 6 (6000.0.68f1), C#, NUnit (Unity Test Framework), ScriptableObject(BalanceConfig), MVVM.

---

## 파일 구조 맵

**A. 캡 제거**
- `Assets/_Lair/Scripts/Battle/BattleController.cs` — 필드/프로퍼티/메서드/스폰검사 제거
- `Assets/_Lair/Scripts/Battle/BattleContext.cs` — 위임 제거
- `Assets/_Lair/Scripts/Card/CommonInterface.cs` — 인터페이스 멤버 제거
- `Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier3.cs` — 효과 교체
- `Assets/_Lair/Scripts/Battle/Spawner.cs` — 캡 언급 주석 정리

**B. 트리거 축소**
- `Assets/_Lair/Scripts/Data/BalanceConfig.cs` — 기본 배열
- `Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs` — DefaultThresholds
- `Assets/_Lair/Data/BalanceConfig.asset` — 직렬화 값(런타임 진실원)

**테스트**
- `Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs` — Tier3 테스트 재작성
- `Assets/_Lair/Tests/EditMode/` — ActiveTriggerService 5개 발화 / Tank Tier3 신효과 테스트

---

## Task 0 (선행): game-designer — Tank Tier3 신효과 수치 설계

**산출물:** `docs/design/` 기획서 또는 patch note — Tank Tier3 대체 효과의 구체 스탯·배율.

- [ ] **Step 1:** game-designer 가 컨셉서 §8 밸런스 + Tank Tier1(HP ×1.3)/Tier2(Power ×1.2) 맥락에서 Tier3 신효과를 결정. 권장 형태: `RegisterMonsterTypeBuff(Wisp/Wraith, EMonsterStatKind.Hp, <배율>)` 또는 동등한 내구 계열. 배율 숫자를 확정해 아래 Task 3 에 주입.

> 이하 Task 들은 Task 0 에서 확정된 배율을 `<TIER3_HP_MUL>` 로 표기한다. 구현 전 반드시 실제 값으로 치환.

---

## Task 1: 캡 enforcement 제거 — 스폰 경로

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Test: `Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs`

- [ ] **Step 1: 실패 테스트 작성** — 캡(구 18) 초과 스폰 회귀 테스트. PlayMode 테스트로 스포너 출력을 19+ 까지 누적시켜 truncate 가 없음을 확인.

```csharp
[UnityTest]
public IEnumerator 캡_제거_후_18마리_초과_스폰_truncate_없음()
{
    //# Arrange — BattleController 부팅 후 스포너 출력 충분히 누적
    //# (테스트 헬퍼는 기존 CardRenewalSpawnerIntegrationTest 패턴 따름)
    BattleController bc = BuildBootedController();
    SetSpawnerOutputCount(bc, 25);   //# 한 사이클 25마리 의도

    //# Act — 한 스폰 사이클 강제 발사
    yield return ForceOneSpawnCycle(bc);

    //# Assert — 18 캡에 막히지 않고 25마리 모두 생성
    Assert.GreaterOrEqual(AliveMonsterCount(bc), 25, "캡 제거 후 truncate 없어야 함");
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인** — 현재는 캡 18로 막혀 18에서 멈춤 → FAIL. (Unity Test Runner PlayMode)

- [ ] **Step 3: enforcement 제거** — `SpawnFromSpawner` 의 캡 검사 2곳, `SpawnMonsterRuntime` 의 캡 검사 2곳을 삭제.

```csharp
//# SpawnFromSpawner — 삭제 대상
//   if (AliveMonsterCount() >= _monsterCap) return;   // L384 선검사
//   if (AliveMonsterCount() >= _monsterCap) break;    // L394 마리단위
//# 결과: 종료 검사(_model.Result)만 남기고 count 전량 스폰

public async void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
{
    if (_model != null && _model.Result != BattleResult.None) return;

    GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(type);
    if (prefab == null) return;
    if (_model != null && _model.Result != BattleResult.None) return;
    for (int i = 0; i < count; ++i)
    {
        CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
        if (p == null) continue;
        p.transform.position = exactPos;
        ApplyMonsterStats(p.gameObject, type, resetCurrent: true);
    }
}
```

```csharp
//# SpawnMonsterRuntime — 캡 선검사·재검사 2곳 삭제, 나머지 로직 보존
public async void SpawnMonsterRuntime(Lair.Data.EMonster key, Vector3 nearHero)
{
    if (_model != null && _model.Result != BattleResult.None) return;

    GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(key);
    if (prefab == null) return;
    if (_model != null && _model.Result != BattleResult.None) return;
    CHPoolable p = CHMPool.Instance.Pop(prefab, transform);
    if (p == null) return;
    //# 이하 기존 offset/배치 로직 유지
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인** — PASS.

- [ ] **Step 5: 스테이징** (Rule 01 — 커밋은 마무리에서)

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs
```

---

## Task 2: 캡 필드·프로퍼티·증가 API 삭제

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Modify: `Assets/_Lair/Scripts/Battle/BattleContext.cs`
- Modify: `Assets/_Lair/Scripts/Card/CommonInterface.cs`

- [ ] **Step 1:** `BattleController` 에서 다음 삭제:

```csharp
//# 삭제: private int _monsterCap = 18;            (필드)
//# 삭제: public int MonsterCap => _monsterCap;     (프로퍼티)
//# 삭제: public void IncrementGlobalMonsterCap(int delta) { ... }   (메서드 전체)
```

- [ ] **Step 2:** `Card/CommonInterface.cs` 의 `IBattleContext` 에서 멤버 삭제:

```csharp
//# 삭제: void IncrementGlobalMonsterCap(int delta);
```

- [ ] **Step 3:** `BattleContext.cs` 의 위임 삭제:

```csharp
//# 삭제: public void IncrementGlobalMonsterCap(int delta) => _owner.IncrementGlobalMonsterCap(delta);
//# (해당 주석 라인 L118 포함 정리)
```

- [ ] **Step 4: 컴파일 확인** — 이 시점엔 `TankSynergyTier3` 가 삭제된 API 를 참조해 컴파일 에러 발생 예정 → Task 3 에서 해소. (Unity recompile)

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/BattleController.cs Assets/_Lair/Scripts/Battle/BattleContext.cs Assets/_Lair/Scripts/Card/CommonInterface.cs
```

---

## Task 3: Tank Tier3 효과 교체 (Wisp+Wraith 내구 버프)

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier3.cs`
- Test: `Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs`

- [ ] **Step 1: 실패 테스트 작성** — Tier3 발화 시 Wisp/Wraith HP 배율이 적용되는지 검증 (구 "캡 24로 상승" 테스트를 대체).

```csharp
[UnityTest]
public IEnumerator Tank_Tier3_발화_시_Wisp_Wraith_HP_추가버프_적용()
{
    BattleController bc = BuildBootedController();
    //# Tier3 임계(7장) 발화
    yield return FireBuildSynergy(bc, BuildArchetype.Tank, tier: 3);

    float wispHp = GetTypeMaxHp(bc, EMonster.Wisp);
    Assert.Greater(wispHp, BaselineMaxHp(EMonster.Wisp), "Tier3 후 Wisp 최대 HP 증가");
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인** — FAIL (구 효과는 캡 증가였음 / 신효과 미구현).

- [ ] **Step 3: 효과 교체** — `<TIER3_HP_MUL>` 는 Task 0 확정값으로 치환.

```csharp
using Lair.Data;

namespace Lair.Card
{
    //# 카드 리뉴얼: Tank Tier3 (7장 임계) — Wisp+Wraith 추가 내구 버프 (글로벌 영구).
    //# 구 캡 +6 효과를 캡 제거에 따라 테마 일관 내구 강화로 교체. 기획서 §4.2·§8.
    public class TankSynergyTier3 : IBuildSynergyTier
    {
        private const float HpMul = <TIER3_HP_MUL>f;

        public void Apply(IBattleContext ctx)
        {
            ctx.RegisterMonsterTypeBuff(EMonster.Wisp,   EMonsterStatKind.Hp, HpMul);
            ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, HpMul);
        }
    }
}
```

- [ ] **Step 4: 테스트 실행해 통과 확인** — PASS. 전체 컴파일도 정상화(Task 2 의 에러 해소).

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Card/Synergy/TankSynergyTier3.cs Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs
```

---

## Task 4: 캡 언급 주석 정리

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/Spawner.cs`

- [ ] **Step 1:** `Spawner.cs` Tick 내 "캡 검사는 사이클 단위 (§4.3) — 호스트가 캡 이상이면 사이클 전량 skip." 주석을 캡 제거 사실에 맞게 갱신 (예: "스폰 위치 산정 후 호스트에 사이클 위임").

```csharp
//# (수정 후 예시)
//# 스폰 위치 — 각 스포너 자기 위치 우선: _spawnPoint > transform.position (zone 픽 미사용).
Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : transform.position;
_host.SpawnFromSpawner(_currentType, spawnPos, _outputCount);
```

- [ ] **Step 2:** 코드베이스에서 "캡 18"/"_monsterCap" 잔여 주석 검색해 정리. (Grep `캡|monsterCap`)

- [ ] **Step 3: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/Spawner.cs
```

---

## Task 5: 액티브 트리거 코드 기본값 축소

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs`
- Modify: `Assets/_Lair/Scripts/Data/BalanceConfig.cs`
- Test: `Assets/_Lair/Tests/EditMode/` (신규 또는 기존 트리거 테스트 파일)

- [ ] **Step 1: 실패 테스트 작성** — ActiveTriggerService 가 5회만 발화하는지 검증 (EditMode, BattleClock mock).

```csharp
[Test]
public void 액티브트리거_분단위_제거_후_5회만_발화()
{
    FakeClock clock = new FakeClock();
    ActiveTriggerService svc = new ActiveTriggerService(clock);   //# DefaultThresholds 사용
    int count = 0;
    svc.OnTriggered += _ => count++;

    //# 0→300초 경과 시뮬레이션
    for (float t = 0f; t <= 300f; t += 1f) clock.Tick(t);

    Assert.AreEqual(5, count, "분단위 제거 후 5회만 발화");
}
```

- [ ] **Step 2: 테스트 실행해 실패 확인** — 현재 DefaultThresholds 9개 → 9회 발화 → FAIL.

- [ ] **Step 3: DefaultThresholds 갱신**

```csharp
//# ActiveTriggerService.cs
private static readonly float[] DefaultThresholds =
    { 30f, 90f, 150f, 210f, 270f };   //# 분단위(60/120/180/240) 제거 — 5개
```

```csharp
//# BalanceConfig.cs
[SerializeField] private float[] _activeThresholds =
    { 30f, 90f, 150f, 210f, 270f };
```

- [ ] **Step 4: 테스트 실행해 통과 확인** — PASS (5회).

- [ ] **Step 5: 스테이징**

```bash
git add Assets/_Lair/Scripts/Battle/ActiveTriggerService.cs Assets/_Lair/Scripts/Data/BalanceConfig.cs Assets/_Lair/Tests/EditMode/
```

---

## Task 6: BalanceConfig.asset 직렬화 값 갱신 (런타임 진실원)

**Files:**
- Modify: `Assets/_Lair/Data/BalanceConfig.asset`

- [ ] **Step 1:** `BalanceConfig.asset` 의 `_activeThresholds` YAML 배열을 `[30, 90, 150, 210, 270]` 로 수정. (Unity 인스펙터 편집 또는 YAML 직접 편집 후 에디터 refresh)

```yaml
# (수정 후)
_activeThresholds:
- 30
- 90
- 150
- 210
- 270
```

- [ ] **Step 2: 검증** — PlayMode 부팅 후 한 판에 액티브 카드가 정확히 5회({30,90,150,210,270}초)만 뜨는지 확인. (gameplay-programmer 자체 스모크 또는 통합 테스트)

- [ ] **Step 3: 스테이징**

```bash
git add Assets/_Lair/Data/BalanceConfig.asset
```

---

## Task 7: 회귀·통합 테스트 (test-engineer)

**Files:**
- Modify: `Assets/_Lair/Tests/PlayMode/CardRenewalSpawnerIntegrationTest.cs`
- Modify/Create: `Assets/_Lair/Tests/EditMode/` 트리거 테스트

- [ ] **Step 1:** 구 `Tank_Tier3_IncrementGlobalMonsterCap_*` / `MonsterCap` 단언 테스트가 모두 제거/대체됐는지 확인 (컴파일 깨짐 방지).
- [ ] **Step 2:** 다음 케이스 커버 확인:
  - 캡 제거: 18 초과 스폰 truncate 없음 (Task 1)
  - Tank Tier3 신효과: Wisp/Wraith HP 증가 (Task 3)
  - 액티브 트리거: 5회 발화 + 정확한 시점({30,90,150,210,270}) (Task 5)
  - 경계: 270초 이후 추가 발화 없음 / 30초 이전 무발화
- [ ] **Step 3:** EditMode + PlayMode 전체 그린 확인.
- [ ] **Step 4: 스테이징**

```bash
git add Assets/_Lair/Tests/
```

---

## Self-Review 결과

- **Spec coverage:** §2.A(캡 제거)=Task1-2-4, Tank Tier3=Task0+3, §2.B(트리거)=Task5-6, 테스트=Task1/3/5/7. 전 항목 매핑됨.
- **Placeholder scan:** `<TIER3_HP_MUL>` 는 Task 0(game-designer) 산출물로 치환되는 의도적 변수 — 구현 전 확정. 그 외 placeholder 없음.
- **Type consistency:** `RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, float)` 시그니처는 기존 Tier1/2 와 동일 사용. `IncrementGlobalMonsterCap` 은 인터페이스·구현·호출처(Task2,3)에서 일괄 제거되어 잔존 참조 없음.
