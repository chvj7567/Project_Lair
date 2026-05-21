# Slice C — 밸런싱 도구 설계서

> Project Lair MVP 의 다섯 번째 수직 슬라이스 — 기획서 §11.5 의 6·7단계(플레이테스트 + 튜닝) 진입 도구.
> 작성일: 2026-05-21
> 상태: Draft v0.1 — 사용자 검토 대기

---

## 0. 목적과 범위

### 0.1 목적
MVP 콘텐츠(영웅 1 / 몬스터 6종 / 패시브 15장 / 액티브 10장)는 Slice A~B3 으로 완성됐다.
그러나 MVP 의 존재 이유인 **"5분 자동전투 + 트리거 선택지가 재미있는가"** 가설은 미검증이다.
이 슬라이스는 그 검증(기획서 §8 — 영웅이 2~4분 사이 사망하도록 튜닝)을 **빠르게 반복할 수 있는 상태**를 만든다.

### 0.2 In Scope (Slice C)
- **수치 데이터화** — 캐릭터 스탯 + 전투 상수를 `BalanceConfig` SO 한 곳으로 분리, 런타임 적용
- **카드 효과값 보호** — `RebuildAllCards` 를 비파괴로 변경 (hand-edit 소실 방지)
- **디버그 에디터 윈도우** — 플레이 중 치트 6종 + 한 판 결과 히스토리
- **한 판 결과 측정** — 종료 시 결과 레코드를 파일에 누적

### 0.3 Out of Scope
- 시간 배율 가속 / 강제 스킵
- 메타 진행 / 서버 / 사운드 / 아트
- 카드 효과값의 BalanceConfig 통합 (사용자 결정 — .asset 직접 튜닝 유지)
- 인게임 런타임 디버그 오버레이 (사용자 결정 — 에디터 윈도우 채택)

### 0.4 검증 가설
"수치를 코드 수정·재빌드 없이 데이터만 고쳐 재시작으로 튜닝할 수 있고, 한 판 결과를 측정해
영웅 사망 시각이 목표 구간(2~4분)에 드는지 판단할 수 있는가."

---

## 1. 프로젝트 룰 매핑

| 룰 | 본 설계에서의 적용 |
|---|---|
| 01 자동 커밋 + 한글 포맷 | 마일스톤별 커밋 메시지(안)만, 관련 파일 `git add` 까지 |
| 02 주석 `//#` | 모든 신규 주석 |
| 03 종속성 최소화 | `BalanceConfig` 는 `BattleController` 가 주입 — 캐릭터 컴포넌트는 config 를 모름 |
| 05 MVVM | HUD/VM 변경 없음 |
| 07 ChvjPackage | `CHMResource`/`CHMPool` 재사용. 디버그 윈도우는 `EditorWindow` (에디터 전용) |
| 08 Enum 키 | `BalanceConfig` 는 비-Addressable(씬 직접 참조)이라 Enum 키 무관. 디버그 카드픽 드롭다운은 `ECardId` |
| 09 CommonEnum | 신규 공용 enum 없음 — `RunRecord` 는 enum 을 문자열로 직렬화 |
| 11 CHText/CHButton | 디버그 윈도우는 IMGUI `EditorWindow` — Rule 11 의 "에디터 전용 UI" 예외에 해당 |
| 12 CHMPool 스폰 | 스폰 경로 변경 없음. 스탯 적용은 Pop 직후 |
| 14 에셋 폴더 | `BalanceConfig.asset` 은 `Assets/_Lair/Data/` (비-Addressable — Rule 14 비대상, `Data/Fonts/` 와 동급) |

---

## 2. Part 1 — `BalanceConfig` SO + 런타임 적용

### 2.1 현재 수치의 진실 위치 (문제)

| 수치 | 현재 진실 위치 | 변경 비용 |
|---|---|---|
| 캐릭터 스탯 | `LairCharacterPrefabBuilder.AllSpecs[]` C# 배열 → 프리팹 베이크 | 코드 수정 + `M3` 메뉴 재실행 |
| 런 길이 (300초) | `BattleStateModel.TotalSeconds` 필드 기본값 | 코드 수정 |
| 패시브 임계점 | `PassiveTriggerService.Thresholds` `static readonly` | 코드 수정 |
| 액티브 임계점 | `ActiveTriggerService.DefaultThresholds` `static readonly` | 코드 수정 |
| 초기 몬스터 배치 | `BattleController._monsterSpawns` (씬) | 이미 데이터 — 인스펙터 |

핵심 문제: 튜닝 가능한 숫자의 진실이 C# 코드에 박혀 있어 변경 시 코드 수정 + (캐릭터의 경우) 메뉴 재빌드가 필요하다.

### 2.2 신규 `BalanceConfig : ScriptableObject`

- 클래스: `Assets/_Lair/Scripts/Data/BalanceConfig.cs` (namespace `Lair.Data`)
- 에셋: `Assets/_Lair/Data/BalanceConfig.asset` — 비-Addressable, 씬에서 직접 참조

```csharp
[CreateAssetMenu(fileName = "BalanceConfig", menuName = "Lair/BalanceConfig")]
public class BalanceConfig : ScriptableObject
{
    [Serializable]
    public class CharacterStat
    {
        public int   Hp;
        public int   Power;
        public float Range;
        public float Cooldown;
        public float MoveSpeed;
    }

    [Serializable]
    public class MonsterStatRow
    {
        public EMonster Key;
        public CharacterStat Stat;
    }

    [SerializeField] private CharacterStat   _hero;
    [SerializeField] private MonsterStatRow[] _monsters;          //# 6행
    [SerializeField] private float   _runDuration       = 300f;
    [SerializeField] private float[] _passiveThresholds = { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
    [SerializeField] private float[] _activeThresholds  = { 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f };

    public CharacterStat Hero => _hero;
    public CharacterStat GetMonster(EMonster key);   //# 행 lookup, 미발견 시 null + 경고 로그
    public float   RunDuration       => _runDuration;
    public float[] PassiveThresholds => _passiveThresholds;
    public float[] ActiveThresholds  => _activeThresholds;
}
```

### 2.3 런타임 적용 — `BattleController`

- `[SerializeField] private BalanceConfig _balance;` — 씬에서 직접 참조 (유일 소비자라 Addressable 불필요)
- 신규 private 메서드 `ApplyStats(GameObject character, BalanceConfig.CharacterStat stat)`:
  - `Health.SetMax(stat.Hp, resetCurrent: true)`
  - `MeleeAttacker.Configure(stat.Range, stat.Cooldown, stat.Power)` — **기존 메서드, 신규 작업 없음**
  - `SimpleMover.Speed = stat.MoveSpeed`
- 호출 지점:
  - `SpawnHero()` 직후 — `ApplyStats(hero, _balance.Hero)`
  - `SpawnMonsters()` 각 몬스터 Pop 직후 — `ApplyStats(go, _balance.GetMonster(sp.Key))`
  - `SpawnMonsterRuntime()` (카드 소환) Pop 직후 — 동일
- 풀 재사용 안전: 스탯이 매 Pop 직후 재적용되므로 이전 판 값 잔존 없음. `Health.OnEnable`/`MeleeAttacker.OnEnable` 의 오버레이 리셋(B3)은 그대로.

### 2.4 전투 상수 적용

- 런 길이: `_model.TotalSeconds = _balance.RunDuration;` → `_clock = new BattleClock(_model.TotalSeconds);`
- 패시브: `new PassiveTriggerService(_heroHealth, _balance.PassiveThresholds)`
- 액티브: `new ActiveTriggerService(_clock, _balance.ActiveThresholds)` — 생성자가 이미 `float[]` 수용

### 2.5 필요 변경 요약

| 대상 | 변경 |
|---|---|
| `BalanceConfig.cs` | 신규 |
| `MeleeAttacker` | **변경 없음** — `Configure(range, cooldown, power)` 이미 존재 |
| `Health` / `SimpleMover` | **변경 없음** — `SetMax` / `Speed` 이미 존재 |
| `PassiveTriggerService` | 생성자에 `float[] thresholds = null` 추가 (`ActiveTriggerService` 와 동일 패턴). `Thresholds` 를 `static readonly` → 인스턴스 필드 + 기본값 |
| `LairCharacterPrefabBuilder` | `Spec` 에서 스탯 5필드(`Hp/Power/Range/Cooldown/MoveSpeed`) 제거 — 메시/색/스케일/IsHero 만 유지. `BuildOne` 의 스탯 `SetPrivateField` 5줄 제거. 프리팹은 구조만, 스탯의 진실은 SO |
| `BattleController` | `_balance` 필드 + `ApplyStats` + 호출 3곳 + 전투 상수 3곳 |

### 2.6 마이그레이션 — 생성 메뉴

신규 메뉴 `Lair/Setup/C - Create BalanceConfig`:
- `Assets/_Lair/Data/BalanceConfig.asset` 이 없으면 생성, 기획서 §11.3 현재 값으로 사전 채움 (값 손실 0)
- 메뉴는 현재 값을 자체 보유 (빌더의 `Spec` 배열에 의존하지 않음 — 빌더 스탯 필드 제거와 무관하게 동작)
- 생성 후 사용자가 `Battle.unity` 의 `BattleController._balance` 필드에 수동 연결 (1회)

---

## 3. Part 2 — 디버그 에디터 윈도우 + 치트

### 3.1 신규 `LairBalanceWindow : EditorWindow`

- `Assets/_Lair/Editor/LairBalanceWindow.cs`, 메뉴 `Lair/Balance Window`
- IMGUI (`OnGUI`). 플레이 중에만 치트 활성 — `Object.FindObjectOfType<BattleController>()` 로 실행 세션 접근
- 비-플레이 시 안내 문구 + 결과 히스토리만 표시

### 3.2 치트 패널 (6종)

| 치트 | 동작 |
|---|---|
| 강제 패시브 트리거 | 패시브 카드 선택을 큐에 즉시 enqueue |
| 강제 액티브 트리거 | 액티브 카드 선택을 큐에 즉시 enqueue |
| 강제 카드픽 | `ECardId` 드롭다운 + 적용 버튼 → 효과를 팝업 없이 즉시 `Apply` |
| 영웅 HP 설정 | 정수 필드 + 적용 → 목표 HP 로 `TakeDamage`/`Heal` 보정 |
| 영웅 즉사 | `_heroHealth` 현재 HP 만큼 데미지 → 승리 종료 |
| 전투 종료 | 승/패 버튼 → `EndBattle` |

### 3.3 `BattleController` 디버그 API

`#if UNITY_EDITOR` 영역의 public 메서드 6종 (asmdef 경계상 public 필요 — 기존 `AddMonsterBuff` 등과 동일 스타일):

```csharp
#if UNITY_EDITOR
public void DebugForcePassiveTrigger();   //# _queue.Enqueue(Passive, 0) + TryProcessNext
public void DebugForceActiveTrigger();    //# _queue.Enqueue(Active, 0) + TryProcessNext
public void DebugApplyCard(ECardId id);   //# _allCards 에서 find → effect.Apply(_ctx)
public void DebugSetHeroHp(int hp);       //# 현재 HP 와의 delta 로 TakeDamage/Heal
public void DebugKillHero();              //# _heroHealth.TakeDamage(Current)
public void DebugEndBattle(BattleResult result);
#endif
```

- 강제 카드픽용으로 로드된 25장 `CardData` 를 `_allCards` 리스트로 보관 — 현재 코드는 `CardPool` 로드 후 `CardDeck` 만 만들고 풀 참조를 폐기하므로, 풀의 `Cards` 를 `_allCards` 에 합쳐 동기 lookup 가능하게 한다.

### 3.4 결과 패널

- `Logs/lair_runs.jsonl` 을 읽어 표시 (§4)
- 직전 판 요약 강조 + 누적 히스토리 리스트 — 각 행: 결과 / 사망시각 / 픽 수 / 생존 몬스터 수
- "히스토리 초기화" 버튼 — 파일 삭제

---

## 4. Part 3 — 한 판 결과 측정

### 4.1 신규 `RunRecord` (`[Serializable]` POCO)

`Assets/_Lair/Scripts/Battle/RunRecord.cs` (namespace `Lair.Battle`):

```csharp
[Serializable]
public class RunRecord
{
    public string FinishedAt;          //# ISO 8601 시각 문자열
    public string Result;              //# "Win" / "Lose"
    public float  DeathTime;           //# 영웅 사망(또는 타임오버) 경과초
    public List<string> Picks;         //# 픽한 ECardId 문자열 목록 (선택 순서)
    public int    SurvivingMonsters;   //# 종료 시점 생존 몬스터 수
}
```

enum 을 문자열로 직렬화 — 가독성 + `ECardId`/`BattleResult` 재정렬에 강건. `JsonUtility` 로 직렬화 (`List<string>` 지원).

### 4.2 신규 `RunRecorder`

`Assets/_Lair/Scripts/Battle/RunRecorder.cs` — 런타임 POCO (디버그 한정):
- `RecordPick(ECardId id)` — 현재 판의 픽 목록에 누적
- `FinishRun(BattleResult result, float elapsed, int survivingMonsters)` — `RunRecord` 생성 → `Logs/lair_runs.jsonl` 에 한 줄 append (JSON Lines)
- 파일 경로: 프로젝트 루트 `Logs/lair_runs.jsonl` (`Application.dataPath` 의 상위). 디렉터리 없으면 생성
- `.gitignore` 에 `/Logs/` 추가

### 4.3 `BattleController` 연동

- `private readonly RunRecorder _recorder = new();`
- `TryProcessNext` 의 `OnPicked` 콜백 — 카드 선택 시 `_recorder.RecordPick(card.Id)`
- `EndBattle(result)` — `_recorder.FinishRun(result, _clock.Elapsed, 생존수)`
  - `_clock.Elapsed` = 영웅 사망 시각(승리) 또는 ≈`RunDuration`(타임오버 패배)
  - 생존수 = `CharacterRegistry.Monsters` 중 `Health.IsAlive == true` 인 항목 수

---

## 5. 테스트

### 5.1 EditMode (TDD — POCO 위주)
- `RunRecord` — `JsonUtility` 직렬화/역직렬화 왕복 (`List<string>` 포함, 필드 보존)
- `PassiveTriggerService` — 생성자 주입 임계점(커스텀 배열)으로 발동 인덱스 검증
- `BalanceConfig.GetMonster` — `EMonster` 키 lookup 성공/미발견

`MeleeAttacker.Configure` 는 기존 메서드 — 신규 테스트 불필요 (기존 `MeleeAttackerTests` 가 커버).

### 5.2 PlayMode
- 기존 `BattleSmokeTest`/`CardFlowSmokeTest` 는 실제 `Battle` 씬을 로드한다.
  `Battle.unity` 의 `BattleController._balance` 만 와이어하면 자동으로 통과 — 별도 테스트 씬 없음.
- C-M1 완료 후 두 PlayMode 테스트가 그대로 PASS 하는지 확인.

### 5.3 수동 검증
- 빌더 재실행 → 프리팹에 스탯이 베이크되지 않음 확인 (구조만)
- `BalanceConfig.asset` 수정 → 재시작만으로 반영 (프리팹 재빌드 X)
- 카드 .asset 효과값 수정 → `RebuildAllCards` 후에도 값 보존
- 디버그 윈도우 치트 6종 동작
- 한 판 종료 → `Logs/lair_runs.jsonl` 레코드 1줄 + 윈도우에 표시

---

## 6. 마일스톤

각 마일스톤은 컴파일·테스트 통과하는 동작 상태를 유지한다.

| MS | 산출물 | 검증 |
|---|---|---|
| C-M1 | `BalanceConfig` SO + 생성 메뉴 + `PassiveTriggerService` 임계점 주입 + 빌더 스탯 베이크 제거 + `BattleController` 런타임 적용 + `Battle.unity` 와이어 + EditMode 테스트 | SO 수정 → 재시작 반영, 재빌드해도 스탯 안 깨짐, PlayMode 테스트 PASS |
| C-M2 | 비파괴 `RebuildAllCards` (기존 효과값 보존, 누락 카드만 생성, stale 카드만 제거) | 카드 .asset 효과값 수정 후 rebuild → 값 보존 |
| C-M3 | `RunRecord` + `RunRecorder`(`FinishRun`) + `BattleController` 연동 + jsonl 로깅 + `.gitignore` + EditMode 직렬화 테스트 | 한 판 종료 후 `Logs/lair_runs.jsonl` 에 레코드 1줄 |
| C-M4 | `LairBalanceWindow` — 치트 패널 + 결과 히스토리 + `BattleController` 디버그 API + `_allCards` 보관 | 플레이 중 치트 6종 동작, 윈도우에 직전 판 + 히스토리 표시 |

---

## 7. 위험 요소

| 위험 | 영향 | 완화 |
|---|---|---|
| 빌더 스탯 베이크 제거 → 프리팹 단독 사용 시 default 스탯 | 씬 직접 배치 시 스탯 부정확 | 캐릭터는 항상 `BattleController` 풀로만 스폰 → 비이슈. C# 필드 default(`Health _max=100` 등)는 비-degenerate 유지 |
| 풀 재사용 시 이전 판 스탯 잔존 | 다음 판 캐릭터가 튜닝 전 값으로 시작 | 매 Pop 직후 `ApplyStats` 재적용 |
| `Configure` 가 B3 오버레이(`PowerScale`/`CooldownScale`) 침범 | 글로벌 버프와 충돌 | `Configure` 는 base 3필드만 설정(기존 구현 확인 완료). 오버레이는 `OnEnable` 이 1.0 리셋 — 분리 유지 |
| `_balance` 미와이어 (null) | 런타임 NRE | `BattleController.Start` 진입 시 null 체크 + 명확한 `Debug.LogError` |
| EditorWindow ↔ Play 세션 통신 | 비-플레이 시 치트 무효 | `FindObjectOfType`, 치트는 플레이 중에만 활성. 디버그 API 는 `#if UNITY_EDITOR` 가드 |
| `RebuildAllCards` 비파괴 전환 중 신구 카드 혼선 | stale 카드 잔존 또는 신규 누락 | 기존 카드는 효과값 보존, 폐기 ECardId 만 삭제, 누락 ECardId 만 신규 생성 |

---

## 8. 성공 기준 (사용자 검증)

- [ ] `BalanceConfig.asset` 수정 → 재시작만으로 캐릭터 스탯·전투 상수 반영 (프리팹 재빌드 불필요)
- [ ] 캐릭터 빌더 재실행 후에도 스탯이 SO 기준으로 유지
- [ ] 카드 .asset 효과값 튜닝이 `RebuildAllCards` 후에도 보존
- [ ] 디버그 윈도우 치트 6종 동작 (강제 트리거/카드픽/HP/즉사/종료)
- [ ] 한 판 종료 시 결과가 `Logs/lair_runs.jsonl` 에 기록되고 윈도우에 누적 표시
- [ ] EditMode + PlayMode 테스트 전부 PASS
