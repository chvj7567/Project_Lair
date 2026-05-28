# 슬라이스 C — 밸런싱 도구 구현 계획서

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 캐릭터 스탯·전투 상수를 데이터로 분리하고, 디버그 치트 윈도우와 한 판 결과 측정을 추가해 밸런싱 반복을 빠르게 만든다.

**Architecture:** 캐릭터 스탯 + 전투 상수를 `BalanceConfig` SO 한 곳에 두고 `BattleController`가 스폰 직후 런타임 적용한다 (프리팹 재빌드 불필요). 카드 효과값은 `RebuildAllCards`를 비파괴로 바꿔 .asset 직접 튜닝을 보호한다. 한 판 종료 시 결과를 jsonl 파일에 누적하고 `EditorWindow`에서 치트와 히스토리를 본다.

**Tech Stack:** Unity 6 (6000.0.68f1) / C# / ScriptableObject / NUnit (EditMode) / IMGUI EditorWindow / `ChvjUnityInfra` (CHMResource·CHMPool)

**설계서:** `docs/superpowers/specs/2026-05-21-slice-c-balance-tooling-design.md`

**룰 주의 (CLAUDE.md):**
- Rule 01 — `git commit` 직접 실행 금지. 마일스톤 끝 태스크에서 **관련 파일 `git add` (스테이징) + 커밋 메시지(안) 제시**까지만.
- Rule 02 — 모든 신규 주석은 `//#`.

---

## 파일 구조

| 파일 | 책임 | 마일스톤 |
|---|---|---|
| `Assets/_Lair/Scripts/Data/BalanceConfig.cs` | 캐릭터 스탯 + 전투 상수의 단일 진실 (SO) | C-M1 신규 |
| `Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs` | HP 임계점 — 생성자 주입 가능하게 | C-M1 수정 |
| `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs` | 캐릭터 프리팹 빌더 — 스탯 베이크 제거 | C-M1 수정 |
| `Assets/_Lair/Editor/LairBalanceConfigSetup.cs` | `BalanceConfig.asset` 1회 생성 메뉴 | C-M1 신규 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 스탯·상수 런타임 적용, 결과 기록, 디버그 API | C-M1·M3·M4 수정 |
| `Assets/_Lair/Tests/EditMode/Data/BalanceConfigTests.cs` | `GetMonster` 검증 | C-M1 신규 |
| `Assets/_Lair/Tests/EditMode/Battle/PassiveTriggerServiceTests.cs` | 임계점 주입 테스트 추가 | C-M1 수정 |
| `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` | 카드 빌더 — 비파괴 rebuild | C-M2 수정 |
| `Assets/_Lair/Scripts/Battle/RunRecord.cs` | 한 판 결과 스냅샷 (직렬화 POCO) | C-M3 신규 |
| `Assets/_Lair/Scripts/Battle/RunRecorder.cs` | 픽·결과 수집 + jsonl 누적 | C-M3 신규 |
| `Assets/_Lair/Tests/EditMode/Battle/RunRecordTests.cs` | JSON 왕복 검증 | C-M3 신규 |
| `.gitignore` | `/Logs/` 무시 추가 | C-M3 수정 |
| `Assets/_Lair/Editor/LairBalanceWindow.cs` | 디버그 치트 + 결과 히스토리 윈도우 | C-M4 신규 |
| `Assets/_Lair/Scenes/Battle.unity` | `BattleController._balance` 와이어 | C-M1 수정 (씬) |

**테스트 실행 방법:** Unity Test Runner (`Window > General > Test Runner`) > `EditMode` 탭에서 해당 테스트 실행. 코드 수정 후 Unity 재컴파일 완료를 먼저 기다린다.

---

## 마일스톤 C-M1 — BalanceConfig SO + 런타임 적용

### Task 1: `BalanceConfig` SO 클래스

**Files:**
- Create: `Assets/_Lair/Scripts/Data/BalanceConfig.cs`
- Test: `Assets/_Lair/Tests/EditMode/Data/BalanceConfigTests.cs`

- [ ] **Step 1: `BalanceConfig.cs` 작성**

```csharp
using System;
using UnityEngine;

namespace Lair.Data
{
    //# 캐릭터 스탯 + 전투 상수의 단일 진실. BattleController 가 씬에서 참조해 런타임 적용.
    [CreateAssetMenu(fileName = "BalanceConfig", menuName = "Lair/BalanceConfig")]
    public class BalanceConfig : ScriptableObject
    {
        //# 한 캐릭터의 튜닝 가능한 스탯.
        [Serializable]
        public class CharacterStat
        {
            public int   Hp;
            public int   Power;
            public float Range;
            public float Cooldown;
            public float MoveSpeed;
        }

        //# EMonster 키 ↔ 스탯 매핑 행.
        [Serializable]
        public class MonsterStatRow
        {
            public EMonster Key;
            public CharacterStat Stat;
        }

        [SerializeField] private CharacterStat _hero;
        [SerializeField] private MonsterStatRow[] _monsters;

        [SerializeField] private float _runDuration = 300f;
        [SerializeField] private float[] _passiveThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };
        [SerializeField] private float[] _activeThresholds =
            { 30f, 60f, 90f, 120f, 150f, 180f, 210f, 240f, 270f };

        public CharacterStat Hero => _hero;
        public float RunDuration => _runDuration;
        public float[] PassiveThresholds => _passiveThresholds;
        public float[] ActiveThresholds => _activeThresholds;

        //# EMonster 키로 스탯 행 조회. 미발견 시 null + 경고.
        public CharacterStat GetMonster(EMonster key)
        {
            if (_monsters != null)
            {
                foreach (var row in _monsters)
                {
                    if (row != null && row.Key == key) return row.Stat;
                }
            }
            Debug.LogWarning($"[BalanceConfig] 몬스터 스탯 미발견: {key}");
            return null;
        }
    }
}
```

- [ ] **Step 2: Unity 재컴파일 대기 후 콘솔 에러 0 확인**

`BalanceConfig.cs` 가 에러 없이 컴파일되어야 한다. `Lair.Data` 네임스페이스 — `EMonster` 가 같은 네임스페이스(`CommonEnum.cs`)에 있어 `using` 불필요.

- [ ] **Step 3: `BalanceConfigTests.cs` 작성**

`Data` 폴더가 없으면 `Assets/_Lair/Tests/EditMode/Data/` 를 만든다. (EditMode 테스트 asmdef 는 하위 폴더 전체를 포함하므로 별도 asmdef 불필요.)

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceConfig.GetMonster 키 조회 검증.
    //# private [SerializeField] 는 JsonUtility.FromJsonOverwrite 로 채운다 (UnityEditor 의존 회피).
    public class BalanceConfigTests
    {
        [Test]
        public void GetMonster_등록된키_스탯반환()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            //# EMonster.Golem == 1 (Slime=0, Golem=1, ...)
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":1,\"Stat\":{\"Hp\":500,\"Power\":20}}]}",
                config);

            var stat = config.GetMonster(EMonster.Golem);

            Assert.IsNotNull(stat);
            Assert.AreEqual(500, stat.Hp);
            Assert.AreEqual(20, stat.Power);
        }

        [Test]
        public void GetMonster_미등록키_null반환()
        {
            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            Assert.IsNull(config.GetMonster(EMonster.Bat));
        }
    }
}
```

- [ ] **Step 4: Test Runner EditMode 에서 `BalanceConfigTests` 실행**

Run: Unity Test Runner > EditMode > `BalanceConfigTests`
Expected: 2개 테스트 PASS. (`GetMonster_미등록키` 는 경고 로그를 남기지만 경고는 테스트를 실패시키지 않음.)

---

### Task 2: `PassiveTriggerService` 임계점 생성자 주입

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/PassiveTriggerServiceTests.cs`

- [ ] **Step 1: 임계점 주입 테스트 추가**

`PassiveTriggerServiceTests.cs` 클래스 안에 메서드 추가:

```csharp
        [Test]
        public void 커스텀_임계점_주입_시_그_임계점으로만_발동()
        {
            var hp = new FakeHealth();
            hp.SetMax(1000);
            //# 50% 단일 임계점만 주입
            var svc = new PassiveTriggerService(hp, new[] { 0.5f });
            var fired = new List<int>();
            svc.OnTriggered += i => fired.Add(i);

            hp.TakeDamage(100);   //# 900 (90%) — 50% 미통과 → 발동 X
            Assert.AreEqual(0, fired.Count);

            hp.TakeDamage(450);   //# 450 (45%) — 50% 통과 → 발동
            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }
```

- [ ] **Step 2: Test Runner 실행 — 컴파일 실패 확인**

Run: Unity Test Runner > EditMode
Expected: 테스트 어셈블리 컴파일 실패 — `PassiveTriggerService` 생성자가 `float[]` 인자를 받지 않음.

- [ ] **Step 3: `PassiveTriggerService.cs` 전체를 다음으로 교체**

```csharp
using System;
using Lair.Character;

namespace Lair.Battle
{
    //# Hero IHealth 의 OnChanged 를 구독해 HP 임계점 통과 1회 감지.
    //# 기본: 90%..10% (9개). 디버그/튜닝용으로 생성자에 다른 배열 주입 가능.
    public class PassiveTriggerService : IDisposable
    {
        //# 기본 임계점 — 90%, 80%, ..., 10%
        private static readonly float[] DefaultThresholds =
            { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private readonly float[] _thresholds;
        private readonly bool[] _fired;
        private readonly IHealth _hero;

        public event Action<int> OnTriggered;   //# 0=첫 임계점, ...

        //# thresholds 미지정 시 90%..10% 9개 사용.
        public PassiveTriggerService(IHealth hero, float[] thresholds = null)
        {
            _thresholds = thresholds ?? DefaultThresholds;
            _fired = new bool[_thresholds.Length];
            _hero = hero;
            _hero.OnChanged += HandleChanged;
        }

        public void Dispose()
        {
            if (_hero != null) _hero.OnChanged -= HandleChanged;
        }

        private void HandleChanged(int current, int max)
        {
            if (max <= 0) return;
            float ratio = (float)current / max;
            for (int i = 0; i < _thresholds.Length; ++i)
            {
                if (_fired[i]) continue;
                if (ratio <= _thresholds[i])
                {
                    _fired[i] = true;
                    OnTriggered?.Invoke(i);
                }
            }
        }
    }
}
```

- [ ] **Step 4: Test Runner EditMode 실행**

Run: Unity Test Runner > EditMode > `PassiveTriggerServiceTests`
Expected: 4개 테스트 전부 PASS (기존 3개 + 신규 1개). 기존 3개는 `new PassiveTriggerService(hp)` — 기본 인자로 그대로 동작.

---

### Task 3: 캐릭터 빌더 스탯 베이크 제거

**Files:**
- Modify: `Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs`

- [ ] **Step 1: `Spec` 클래스에서 스탯 5필드 제거**

`LairCharacterPrefabBuilder.cs` 의 `Spec` 클래스를 다음으로 교체 (`Hp/Power/Range/Cooldown/MoveSpeed` 제거, 구조·시각 필드만 유지):

```csharp
        //# 캐릭터 빌드 스펙 — 메시/색/스케일만. 스탯은 BalanceConfig 가 단일 진실 (Slice C).
        public class Spec
        {
            public string Name;
            public PrimitiveType Mesh;
            public string ColorHex;
            public float Scale;
            public bool IsHero;
        }
```

- [ ] **Step 2: `AllSpecs` 배열에서 스탯 값 제거**

`AllSpecs` 를 다음으로 교체:

```csharp
        public static readonly Spec[] AllSpecs = new[]
        {
            new Spec { Name = nameof(EHero.Knight),    Mesh = PrimitiveType.Capsule, ColorHex = "#3B82F6", Scale = 1.0f, IsHero = true  },
            new Spec { Name = nameof(EMonster.Slime),  Mesh = PrimitiveType.Sphere,  ColorHex = "#22C55E", Scale = 0.6f, IsHero = false },
            new Spec { Name = nameof(EMonster.Golem),  Mesh = PrimitiveType.Cube,    ColorHex = "#6B7280", Scale = 1.2f, IsHero = false },
            new Spec { Name = nameof(EMonster.Orc),    Mesh = PrimitiveType.Capsule, ColorHex = "#EF4444", Scale = 0.9f, IsHero = false },
            new Spec { Name = nameof(EMonster.Archer), Mesh = PrimitiveType.Capsule, ColorHex = "#EAB308", Scale = 0.8f, IsHero = false },
            new Spec { Name = nameof(EMonster.Spider), Mesh = PrimitiveType.Cube,    ColorHex = "#A855F7", Scale = 0.5f, IsHero = false },
            new Spec { Name = nameof(EMonster.Bat),    Mesh = PrimitiveType.Sphere,  ColorHex = "#1F2937", Scale = 0.3f, IsHero = false },
        };
```

- [ ] **Step 3: `BuildOne` 에서 스탯 주입 5줄 제거**

`BuildOne` 메서드의 `//# 4) 컴포넌트 부착` 블록에서 컴포넌트 추가 시 로컬 변수 할당을 제거하고, `//# 5) [SerializeField] private 필드 주입` 블록의 스탯 5줄을 삭제한다.

`//# 4)` 블록 첫 3줄을 다음으로 교체:

```csharp
            //# 4) 컴포넌트 부착 — 추가 순서가 Awake 호출 순서이므로 Health 를 의존 컴포넌트보다 먼저
            go.AddComponent<SimpleMover>();
            go.AddComponent<Health>();
            go.AddComponent<MeleeAttacker>();
```

그리고 `//# 5)` 블록 전체(아래 5줄 + 주석)를 삭제:

```csharp
            //# 5) [SerializeField] private 필드 주입 — SerializedObject 사용
            SetPrivateField(mover, "_speed", spec.MoveSpeed);
            SetPrivateField(health, "_max", spec.Hp);
            SetPrivateField(attacker, "_range", spec.Range);
            SetPrivateField(attacker, "_cooldown", spec.Cooldown);
            SetPrivateField(attacker, "_power", spec.Power);
```

주의: `SetPrivateField` 는 `EnsureHpBarPrefab` 의 `_fill` 주입에서 여전히 쓰이므로 메서드 자체는 남긴다.

- [ ] **Step 4: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0. `mover`/`health`/`attacker` 로컬 변수가 사라졌으므로 미사용 변수 경고도 없어야 한다.

- [ ] **Step 5: `M3 - Build Character Prefabs` 메뉴 실행**

Run: Unity 메뉴 `Lair/Setup/M3 - Build Character Prefabs`
Expected: 7개 프리팹 빌드 완료 로그. 빌드된 프리팹의 `Health`/`MeleeAttacker`/`SimpleMover` 는 C# 필드 기본값(`_max=100`, `_range=1.5`, `_cooldown=1.0`, `_power=50`, `_speed=3`)을 가진다 — 실제 스탯은 런타임에 적용되므로 정상.

---

### Task 4: `BalanceConfig.asset` 생성 메뉴

**Files:**
- Create: `Assets/_Lair/Editor/LairBalanceConfigSetup.cs`

- [ ] **Step 1: `LairBalanceConfigSetup.cs` 작성**

```csharp
using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Slice C-M1 — BalanceConfig.asset 1회 생성 + 기획서 §11.3 현재값 사전 채움.
    //# 이미 존재하면 보존 (튜닝값 덮어쓰기 방지).
    public static class LairBalanceConfigSetup
    {
        public const string ConfigPath = "Assets/_Lair/Data/BalanceConfig.asset";

        //# (키, HP, 공격력, 사거리, 쿨다운, 이속) — 기획서 §11.3
        private static readonly (EMonster Key, int Hp, int Power, float Range, float Cd, float Ms)[] Monsters =
        {
            (EMonster.Slime,  200, 10, 1.0f, 1.0f, 1.5f),
            (EMonster.Golem,  500, 20, 1.3f, 1.0f, 0.8f),
            (EMonster.Orc,    100, 20, 1.0f, 0.5f, 2.5f),
            (EMonster.Archer,  60, 30, 5.0f, 1.0f, 2.0f),
            (EMonster.Spider,  50,  5, 1.0f, 1.0f, 2.0f),
            (EMonster.Bat,     30,  5, 1.0f, 0.8f, 3.5f),
        };

        [MenuItem("Lair/Setup/C - Create BalanceConfig")]
        public static void CreateBalanceConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath) != null)
            {
                Debug.LogWarning($"[LairBalanceConfigSetup] 이미 존재 — 보존: {ConfigPath}");
                return;
            }

            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);

            var so = new SerializedObject(config);
            //# 영웅 — 기획서 §11.3 기사
            FillStat(so.FindProperty("_hero"), 1000, 50, 1.5f, 1.0f, 3.0f);

            var monsters = so.FindProperty("_monsters");
            monsters.arraySize = Monsters.Length;
            for (int i = 0; i < Monsters.Length; ++i)
            {
                var m = Monsters[i];
                var row = monsters.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Key").enumValueIndex = (int)m.Key;
                FillStat(row.FindPropertyRelative("Stat"), m.Hp, m.Power, m.Range, m.Cd, m.Ms);
            }
            //# _runDuration / _passiveThresholds / _activeThresholds 는 C# 필드 기본값 사용 — 별도 설정 불필요.

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LairBalanceConfigSetup] 생성 완료: {ConfigPath}");
        }

        private static void FillStat(SerializedProperty stat,
            int hp, int power, float range, float cd, float ms)
        {
            stat.FindPropertyRelative("Hp").intValue = hp;
            stat.FindPropertyRelative("Power").intValue = power;
            stat.FindPropertyRelative("Range").floatValue = range;
            stat.FindPropertyRelative("Cooldown").floatValue = cd;
            stat.FindPropertyRelative("MoveSpeed").floatValue = ms;
        }
    }
}
```

- [ ] **Step 2: 메뉴 실행**

Run: Unity 메뉴 `Lair/Setup/C - Create BalanceConfig`
Expected: `Assets/_Lair/Data/BalanceConfig.asset` 생성 + 로그. Inspector 에서 영웅(HP 1000 등)·몬스터 6행·런 길이 300 확인.

---

### Task 5: `BattleController` 런타임 스탯/상수 적용

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: `_balance` 필드 추가**

`BattleController` 클래스 상단 `[SerializeField]` 영역 — `_monsterSpawns` 다음 줄에 추가:

```csharp
        //# Slice C — 캐릭터 스탯 + 전투 상수의 단일 진실. 씬에서 직접 할당.
        [SerializeField] private BalanceConfig _balance;
```

(`using Lair.Data;` 는 이미 존재 — 추가 불필요.)

- [ ] **Step 2: `Start` — `_model` 생성 직후 런 길이 적용**

`Start` 의 `//# 2. MVVM` 블록을 다음으로 교체:

```csharp
            //# 2. MVVM
            _model = new BattleStateModel();
            //# Slice C — BalanceConfig 의 런 길이 적용
            if (_balance == null)
                Debug.LogError("[BattleController] BalanceConfig(_balance) 미할당 — 프리팹 기본 스탯으로 진행");
            else
                _model.TotalSeconds = _balance.RunDuration;
            _vm = new BattleViewModel(_model);
```

- [ ] **Step 3: `ApplyStats` 메서드 추가**

`SpawnHero` 메서드 바로 위에 추가:

```csharp
        //# Slice C — BalanceConfig 스탯을 스폰된 캐릭터에 적용. Pop 직후 호출.
        private void ApplyStats(GameObject character, BalanceConfig.CharacterStat stat)
        {
            if (character == null || stat == null) return;
            var health = character.GetComponent<Health>();
            if (health != null) health.SetMax(stat.Hp, resetCurrent: true);
            var attacker = character.GetComponent<MeleeAttacker>();
            if (attacker != null) attacker.Configure(stat.Range, stat.Cooldown, stat.Power);
            var mover = character.GetComponent<SimpleMover>();
            if (mover != null) mover.Speed = stat.MoveSpeed;
        }
```

- [ ] **Step 4: `SpawnHero` — 스탯 적용 호출**

`SpawnHero` 의 `_heroHealth = p.GetComponent<Health>();` 줄 바로 다음에 추가:

```csharp
            //# Slice C — 영웅 스탯 적용 (이후 UpdateHeroHp 가 올바른 값 반영)
            if (_balance != null) ApplyStats(p.gameObject, _balance.Hero);
```

- [ ] **Step 5: `SpawnMonsters` — 스탯 적용 호출**

`SpawnMonsters` 의 `_monsters.Add(p);` 줄 바로 다음에 추가:

```csharp
                //# Slice C — 몬스터 스탯 적용
                if (_balance != null) ApplyStats(p.gameObject, _balance.GetMonster(sp.Key));
```

- [ ] **Step 6: `SpawnMonsterRuntime` — 스탯 적용 호출**

`SpawnMonsterRuntime` 의 `p.transform.position = nearHero + offset;` 줄 바로 다음에 추가:

```csharp
            //# Slice C — 카드 소환 몬스터도 스탯 적용
            if (_balance != null) ApplyStats(p.gameObject, _balance.GetMonster(key));
```

- [ ] **Step 7: 패시브/액티브 트리거에 임계점 주입**

`Start` 의 패시브 트리거 생성 줄을 교체:

```csharp
                _passiveTriggers = new PassiveTriggerService(_heroHealth, _balance?.PassiveThresholds);
```

`Start` 의 액티브 트리거 생성 줄을 교체:

```csharp
            _activeTriggers = new ActiveTriggerService(_clock, _balance?.ActiveThresholds);
```

- [ ] **Step 8: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 6: 씬 와이어 + C-M1 검증 + 스테이징

**Files:**
- Modify (씬): `Assets/_Lair/Scenes/Battle.unity`

- [ ] **Step 1: `Battle.unity` 의 `BattleController._balance` 할당**

`Battle` 씬을 열고 `BattleController` 컴포넌트를 가진 GameObject 를 선택, Inspector 의 `Balance` 필드에 `Assets/_Lair/Data/BalanceConfig.asset` 를 할당한 뒤 씬 저장.
(UnityMCP 사용 시: `editor_open_scene` → `scene_find` 로 BattleController 오브젝트 탐색 → `component_set` 로 `_balance` 설정 → `editor_save_scene`.)

- [ ] **Step 2: EditMode 전체 테스트 실행**

Run: Unity Test Runner > EditMode > 전체 실행
Expected: 전체 PASS (`BalanceConfigTests` 2 + `PassiveTriggerServiceTests` 4 + 기존 테스트 전부).

- [ ] **Step 3: PlayMode 테스트 실행**

Run: Unity Test Runner > PlayMode > 전체 실행
Expected: `BattleSmokeTest`/`CardFlowSmokeTest` PASS. `Battle` 씬이 `_balance` 와이어돼 영웅 스탯이 정상 적용되어야 한다.

- [ ] **Step 4: 수동 검증 — 데이터 튜닝 반영**

`BalanceConfig.asset` 에서 영웅 HP 를 1000 → 200 으로 임시 변경 → Play → 영웅이 빨리 죽는지 확인 → 다시 1000 으로 복구. 프리팹 재빌드 없이 재시작만으로 반영되어야 한다.

- [ ] **Step 5: 관련 파일 스테이징 + 커밋 메시지(안) 제시 (Rule 01)**

`git add` 대상:
```
Assets/_Lair/Scripts/Data/BalanceConfig.cs
Assets/_Lair/Scripts/Data/BalanceConfig.cs.meta
Assets/_Lair/Data/BalanceConfig.asset
Assets/_Lair/Data/BalanceConfig.asset.meta
Assets/_Lair/Scripts/Battle/PassiveTriggerService.cs
Assets/_Lair/Scripts/Battle/BattleController.cs
Assets/_Lair/Editor/LairCharacterPrefabBuilder.cs
Assets/_Lair/Editor/LairBalanceConfigSetup.cs
Assets/_Lair/Editor/LairBalanceConfigSetup.cs.meta
Assets/_Lair/Tests/EditMode/Data/BalanceConfigTests.cs
Assets/_Lair/Tests/EditMode/Data/BalanceConfigTests.cs.meta
Assets/_Lair/Tests/EditMode/Data.meta
Assets/_Lair/Tests/EditMode/Battle/PassiveTriggerServiceTests.cs
Assets/_Lair/Scenes/Battle.unity
Assets/_Lair/Art/Characters/*.prefab   (M3 재빌드로 스탯 베이크 제거된 7종)
```

커밋 메시지(안):
```
# [feat] - 슬라이스 C-M1 BalanceConfig SO + 캐릭터 스탯/전투 상수 런타임 적용

- BalanceConfig SO 신규 — 캐릭터 스탯 + 전투 상수 단일 진실
- 캐릭터 빌더 스탯 베이크 제거, 런타임에 BattleController 가 적용
- PassiveTriggerService 임계점 생성자 주입 지원
```

`git commit` 은 사용자가 직접 — 직접 실행하지 않는다.

---

## 마일스톤 C-M2 — 비파괴 `RebuildAllCards`

### Task 7: 카드 빌더 비파괴 전환

**Files:**
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

- [ ] **Step 1: `RebuildAllCards` 에서 `ClearCardDir` → `RemoveStaleCards` 교체**

`RebuildAllCards` 메서드를 다음으로 교체:

```csharp
        //# 패시브 15 + 액티브 10 비파괴 재빌드. 기존 카드의 효과 튜닝값은 보존.
        [MenuItem("Lair/Setup/B3 - Rebuild All Cards")]
        public static void RebuildAllCards()
        {
            EnsureDir(CardDir);
            EnsureDir(PoolDir);
            RemoveStaleCards();
            BuildCardsAndPool(PassiveSpecs, EData.CardPool_Passive);
            BuildCardsAndPool(ActiveSpecs, EData.CardPool_Active);
            Debug.Log("[LairCardPrefabBuilder] 카드 25장 + 풀 2개 재빌드 완료 (비파괴)");
        }
```

- [ ] **Step 2: `ClearCardDir` 를 `RemoveStaleCards` 로 교체**

`ClearCardDir` 메서드 전체를 다음으로 교체:

```csharp
        //# spec 목록에 없는 .asset 만 삭제 (폐기 ECardId). 유효 카드는 보존.
        private static void RemoveStaleCards()
        {
            var valid = new HashSet<string>();
            foreach (var s in PassiveSpecs) valid.Add(s.Id.ToString());
            foreach (var s in ActiveSpecs)  valid.Add(s.Id.ToString());

            foreach (var path in Directory.GetFiles(CardDir, "*.asset"))
            {
                string name = Path.GetFileNameWithoutExtension(path);
                if (valid.Contains(name) == false)
                {
                    AssetDatabase.DeleteAsset(path.Replace('\\', '/'));
                    Debug.Log($"[LairCardPrefabBuilder] stale 카드 삭제: {name}");
                }
            }
        }
```

`System.Collections.Generic` 의 `HashSet` 사용 — 파일 상단 `using System.Collections.Generic;` 는 이미 존재.

- [ ] **Step 3: `BuildCardsAndPool` 의 카드 생성 루프를 비파괴로 변경**

`BuildCardsAndPool` 의 `foreach (var spec in specs)` 루프 전체를 다음으로 교체:

```csharp
            foreach (var spec in specs)
            {
                string path = $"{CardDir}/{spec.Id}.asset";
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                bool isNew = card == null;
                if (isNew) card = ScriptableObject.CreateInstance<CardData>();

                var so = new SerializedObject(card);
                so.FindProperty("_id").enumValueIndex       = (int)spec.Id;
                so.FindProperty("_category").enumValueIndex = (int)spec.Category;
                so.FindProperty("_displayName").stringValue = spec.DisplayName;
                so.FindProperty("_description").stringValue = spec.Description;

                //# 비파괴 — 기존 카드의 _effect(튜닝값) 보존. 신규/타입불일치 시에만 새 효과.
                var effectProp = so.FindProperty("_effect");
                var wanted = spec.EffectFactory();
                var existing = effectProp.managedReferenceValue;
                if (existing == null || existing.GetType() != wanted.GetType())
                    effectProp.managedReferenceValue = wanted;

                so.ApplyModifiedPropertiesWithoutUndo();

                if (isNew) AssetDatabase.CreateAsset(card, path);
                else       EditorUtility.SetDirty(card);
                RegisterAddressable(settings, group, path, spec.Id.ToString());

                createdCards.Add(card);
                Debug.Log($"[LairCardPrefabBuilder] CardData {(isNew ? "생성" : "갱신")}: {spec.Id}");
            }
```

- [ ] **Step 4: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

- [ ] **Step 5: 수동 검증 — 효과값 보존**

1. 카드 .asset 하나 선택 (예: `SlimeHpBoost.asset`), Inspector 에서 `_effect` 의 `_hpMul` 을 1.5 → 2.0 으로 변경.
2. `Lair/Setup/B3 - Rebuild All Cards` 실행.
3. `SlimeHpBoost.asset` 의 `_hpMul` 이 **2.0 으로 유지**되는지 확인 (비파괴 성공). 확인 후 1.5 로 복구.

- [ ] **Step 6: 스테이징 + 커밋 메시지(안)**

`git add`: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`

커밋 메시지(안):
```
# [refactor] - 슬라이스 C-M2 RebuildAllCards 비파괴 전환

- 유효 카드는 효과 튜닝값 보존, 폐기 ECardId 만 삭제
- 신규/효과 타입 불일치 카드만 새로 생성
```

---

## 마일스톤 C-M3 — 한 판 결과 측정

### Task 8: `RunRecord` POCO + JSON 왕복 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/RunRecord.cs`
- Test: `Assets/_Lair/Tests/EditMode/Battle/RunRecordTests.cs`

- [ ] **Step 1: `RunRecord.cs` 작성**

```csharp
using System;
using System.Collections.Generic;

namespace Lair.Battle
{
    //# 한 판의 결과 스냅샷. JsonUtility 직렬화 — enum 은 문자열로 저장.
    [Serializable]
    public class RunRecord
    {
        public string FinishedAt;          //# ISO 8601 시각 문자열
        public string Result;              //# "Win" / "Lose"
        public float  DeathTime;           //# 영웅 사망(또는 타임오버) 경과초
        public List<string> Picks;         //# 픽한 ECardId 문자열 목록 (선택 순서)
        public int    SurvivingMonsters;   //# 종료 시점 생존 몬스터 수
    }
}
```

- [ ] **Step 2: `RunRecordTests.cs` 작성**

```csharp
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# RunRecord 의 JsonUtility 직렬화/역직렬화 왕복 검증.
    public class RunRecordTests
    {
        [Test]
        public void Json_왕복_시_모든_필드_보존()
        {
            var original = new RunRecord
            {
                FinishedAt = "2026-05-21T10:00:00",
                Result = "Win",
                DeathTime = 184.5f,
                Picks = new List<string> { "SlimeHpBoost", "Frenzy" },
                SurvivingMonsters = 7,
            };

            string json = JsonUtility.ToJson(original);
            var restored = JsonUtility.FromJson<RunRecord>(json);

            Assert.AreEqual("2026-05-21T10:00:00", restored.FinishedAt);
            Assert.AreEqual("Win", restored.Result);
            Assert.AreEqual(184.5f, restored.DeathTime, 0.001f);
            Assert.AreEqual(2, restored.Picks.Count);
            Assert.AreEqual("Frenzy", restored.Picks[1]);
            Assert.AreEqual(7, restored.SurvivingMonsters);
        }
    }
}
```

- [ ] **Step 3: Test Runner EditMode 실행**

Run: Unity Test Runner > EditMode > `RunRecordTests`
Expected: 1개 테스트 PASS.

---

### Task 9: `RunRecorder` — 픽·결과 수집 + jsonl 누적

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/RunRecorder.cs`

- [ ] **Step 1: `RunRecorder.cs` 작성**

```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 한 판의 카드 픽과 결과를 수집해 jsonl 파일에 누적. 디버그/밸런싱 한정.
    //# BattleController 가 씬 로드마다 1개 생성 — 한 인스턴스 = 한 판.
    public class RunRecorder
    {
        //# 프로젝트 루트 Logs/lair_runs.jsonl (Application.dataPath 는 .../Assets)
        public static string LogPath => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, "Logs", "lair_runs.jsonl");

        private readonly List<string> _picks = new();

        //# 카드 선택 시 호출 — 픽 순서 누적.
        public void RecordPick(ECardId id) => _picks.Add(id.ToString());

        //# 전투 종료 시 1회 호출 — RunRecord 를 jsonl 한 줄로 append.
        public void FinishRun(BattleResult result, float deathTime, int survivingMonsters)
        {
            var record = new RunRecord
            {
                FinishedAt = DateTime.Now.ToString("o"),
                Result = result.ToString(),
                DeathTime = deathTime,
                Picks = new List<string>(_picks),
                SurvivingMonsters = survivingMonsters,
            };

            string path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, JsonUtility.ToJson(record) + "\n");
            Debug.Log($"[RunRecorder] 기록: {record.Result} / 사망 {record.DeathTime:0.0}s / 픽 {record.Picks.Count}");
        }
    }
}
```

- [ ] **Step 2: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 10: `BattleController` 결과 측정 연동 + `.gitignore`

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`
- Modify: `.gitignore`

- [ ] **Step 1: `_recorder` 필드 추가**

`BattleController` 의 `//# B3 신규` 서비스 필드 영역 다음에 추가:

```csharp
        //# Slice C — 한 판 결과 측정
        private readonly RunRecorder _recorder = new RunRecorder();
```

- [ ] **Step 2: `TryProcessNext` — 픽 기록**

`TryProcessNext` 의 `OnPicked` 람다를 다음으로 교체:

```csharp
                    OnPicked = card =>
                    {
                        //# Slice C — 픽 기록
                        if (card != null) _recorder.RecordPick(card.Id);
                        if (card?.Effect != null && _ctx != null) card.Effect.Apply(_ctx);
                        tcs.TrySetResult(true);
                    }
```

- [ ] **Step 3: `EndBattle` — 결과 기록**

`EndBattle` 의 `_clock.Stop();` 줄 바로 다음에 추가:

```csharp
            //# Slice C — 한 판 결과 기록 (생존 몬스터 수 집계)
            int aliveMonsters = 0;
            foreach (var e in CharacterRegistry.Monsters)
                if (e?.Health != null && e.Health.IsAlive) aliveMonsters++;
            _recorder.FinishRun(result, _clock.Elapsed, aliveMonsters);
```

(`CharacterRegistry` 는 `Lair.Character` — `using Lair.Character;` 는 이미 존재.)

- [ ] **Step 4: `.gitignore` 에 `/Logs/` 추가**

`.gitignore` 를 열어 `/Logs/` 줄이 없으면 파일 끝에 추가:

```
# Slice C — 밸런싱 결과 로그
/Logs/
```

- [ ] **Step 5: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

- [ ] **Step 6: 수동 검증 — 결과 파일 생성**

Play 로 한 판 진행 → 영웅 사망 또는 타임오버 → 프로젝트 루트 `Logs/lair_runs.jsonl` 에 JSON 한 줄이 추가됐는지 확인. `Result`/`DeathTime`/`Picks`/`SurvivingMonsters` 필드가 채워져야 한다.

- [ ] **Step 7: 스테이징 + 커밋 메시지(안)**

`git add`:
```
Assets/_Lair/Scripts/Battle/RunRecord.cs
Assets/_Lair/Scripts/Battle/RunRecord.cs.meta
Assets/_Lair/Scripts/Battle/RunRecorder.cs
Assets/_Lair/Scripts/Battle/RunRecorder.cs.meta
Assets/_Lair/Scripts/Battle/BattleController.cs
Assets/_Lair/Tests/EditMode/Battle/RunRecordTests.cs
Assets/_Lair/Tests/EditMode/Battle/RunRecordTests.cs.meta
.gitignore
```

커밋 메시지(안):
```
# [feat] - 슬라이스 C-M3 한 판 결과 측정 (RunRecorder + jsonl 로깅)

- RunRecord/RunRecorder 신규 — 카드 픽·사망 시각·생존 수 수집
- BattleController 가 종료 시 Logs/lair_runs.jsonl 에 누적
```

---

## 마일스톤 C-M4 — 디버그 에디터 윈도우

### Task 11: `BattleController` 디버그 API + 전체 카드 보관

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: `_allCards` 필드 추가**

`_recorder` 필드 다음에 추가:

```csharp
        //# Slice C-M4 — 디버그 카드픽용 전체 카드 (패시브 15 + 액티브 10)
        private readonly List<CardData> _allCards = new();
```

(`System.Collections.Generic` / `Lair.Card` 는 이미 `using` 존재.)

- [ ] **Step 2: 풀 로드 시 `_allCards` 채우기**

`Start` 의 패시브 풀 로드 줄을 교체:

```csharp
            var pool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Passive);
            if (pool != null)
            {
                _passiveDeck = new CardDeck(pool.Cards);
                _allCards.AddRange(pool.Cards);
            }
```

`Start` 의 액티브 풀 로드 줄을 교체:

```csharp
            var activePool = await CHMResource.Instance.LoadAsync<CardPool>(EData.CardPool_Active);
            if (activePool != null)
            {
                _activeDeck = new CardDeck(activePool.Cards);
                _allCards.AddRange(activePool.Cards);
            }
```

- [ ] **Step 3: 디버그 API 6종 추가**

`BattleController` 클래스 끝(마지막 `}` 직전)에 추가:

```csharp
#if UNITY_EDITOR
        //# ===== Slice C-M4 디버그 API — LairBalanceWindow 전용 =====

        //# 패시브 카드 선택을 즉시 큐에 넣음.
        public void DebugForcePassiveTrigger()
        {
            _queue.Enqueue(TriggerQueue.Source.Passive, 0);
            TryProcessNext();
        }

        //# 액티브 카드 선택을 즉시 큐에 넣음.
        public void DebugForceActiveTrigger()
        {
            _queue.Enqueue(TriggerQueue.Source.Active, 0);
            TryProcessNext();
        }

        //# 지정 카드의 효과를 팝업 없이 즉시 적용.
        public void DebugApplyCard(ECardId id)
        {
            foreach (var c in _allCards)
            {
                if (c != null && c.Id == id)
                {
                    if (c.Effect != null && _ctx != null) c.Effect.Apply(_ctx);
                    return;
                }
            }
            Debug.LogWarning($"[BattleController] 디버그 카드 미발견: {id}");
        }

        //# 영웅 HP 를 목표값으로 설정 (delta 만큼 데미지/회복).
        public void DebugSetHeroHp(int hp)
        {
            if (_heroHealth == null) return;
            int delta = hp - _heroHealth.Current;
            if (delta < 0)      _heroHealth.TakeDamage(-delta);
            else if (delta > 0) _heroHealth.Heal(delta);
        }

        //# 영웅 즉사.
        public void DebugKillHero()
        {
            if (_heroHealth != null) _heroHealth.TakeDamage(_heroHealth.Current);
        }

        //# 전투 즉시 종료.
        public void DebugEndBattle(BattleResult result) => EndBattle(result);
#endif
```

- [ ] **Step 4: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0.

---

### Task 12: `LairBalanceWindow` 윈도우

**Files:**
- Create: `Assets/_Lair/Editor/LairBalanceWindow.cs`

- [ ] **Step 1: `LairBalanceWindow.cs` 작성**

```csharp
using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 밸런싱 디버그 윈도우 — 플레이 중 치트 6종 + 한 판 결과 히스토리.
    //# Rule 11 예외: 에디터 전용 UI.
    public class LairBalanceWindow : EditorWindow
    {
        private int _hpField = 500;
        private ECardId _cardPick = ECardId.SlimeHpBoost;
        private Vector2 _scroll;
        private List<RunRecord> _history;

        [MenuItem("Lair/Balance Window")]
        public static void ShowWindow() => GetWindow<LairBalanceWindow>("Lair Balance");

        private void OnEnable() => ReloadHistory();

        private void OnInspectorUpdate()
        {
            //# 플레이 중 치트 패널(BattleController 발견 여부) 갱신
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            DrawCheatPanel();
            EditorGUILayout.Space(10);
            DrawHistoryPanel();
        }

        private void DrawCheatPanel()
        {
            EditorGUILayout.LabelField("치트", EditorStyles.boldLabel);

            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능", MessageType.Info);
                return;
            }

            var bc = Object.FindFirstObjectByType<BattleController>();
            if (bc == null)
            {
                EditorGUILayout.HelpBox("BattleController 미발견", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("강제 패시브 트리거")) bc.DebugForcePassiveTrigger();
            if (GUILayout.Button("강제 액티브 트리거")) bc.DebugForceActiveTrigger();

            using (new EditorGUILayout.HorizontalScope())
            {
                _cardPick = (ECardId)EditorGUILayout.EnumPopup(_cardPick);
                if (GUILayout.Button("카드 즉시 적용", GUILayout.Width(110)))
                    bc.DebugApplyCard(_cardPick);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _hpField = EditorGUILayout.IntField("영웅 HP", _hpField);
                if (GUILayout.Button("적용", GUILayout.Width(110)))
                    bc.DebugSetHeroHp(_hpField);
            }

            if (GUILayout.Button("영웅 즉사")) bc.DebugKillHero();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전투 종료 — 승리")) bc.DebugEndBattle(BattleResult.Win);
                if (GUILayout.Button("전투 종료 — 패배")) bc.DebugEndBattle(BattleResult.Lose);
            }
        }

        private void DrawHistoryPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("결과 히스토리", EditorStyles.boldLabel);
                if (GUILayout.Button("새로고침", GUILayout.Width(80))) ReloadHistory();
                if (GUILayout.Button("초기화", GUILayout.Width(80))) ClearHistory();
            }

            if (_history == null || _history.Count == 0)
            {
                EditorGUILayout.HelpBox("기록 없음", MessageType.None);
                return;
            }

            //# 직전 판 강조
            var last = _history[_history.Count - 1];
            EditorGUILayout.LabelField(
                $"직전: {last.Result} / 사망 {FormatTime(last.DeathTime)} / 픽 {Count(last.Picks)} / 생존 {last.SurvivingMonsters}",
                EditorStyles.helpBox);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            for (int i = _history.Count - 1; i >= 0; --i)
            {
                var r = _history[i];
                EditorGUILayout.LabelField(
                    $"#{i + 1}  {r.Result}  사망 {FormatTime(r.DeathTime)}  픽 {Count(r.Picks)}  생존 {r.SurvivingMonsters}");
            }
            EditorGUILayout.EndScrollView();
        }

        private static int Count(List<string> list) => list != null ? list.Count : 0;

        private static string FormatTime(float sec)
            => $"{(int)(sec / 60)}:{(int)(sec % 60):00}";

        private void ReloadHistory()
        {
            _history = new List<RunRecord>();
            string path = RunRecorder.LogPath;
            if (File.Exists(path) == false) return;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                _history.Add(JsonUtility.FromJson<RunRecord>(line));
            }
        }

        private void ClearHistory()
        {
            string path = RunRecorder.LogPath;
            if (File.Exists(path)) File.Delete(path);
            _history = new List<RunRecord>();
        }
    }
}
```

- [ ] **Step 2: Unity 재컴파일 — 콘솔 에러 0 확인**

Expected: 컴파일 에러 0. `Lair.EditorTools` asmdef 가 `Lair` 런타임 asmdef 를 참조하므로 `RunRecord`/`BattleController`/`ECardId` 접근 가능.

---

### Task 13: C-M4 검증 + 스테이징

- [ ] **Step 1: 윈도우 열기**

Run: Unity 메뉴 `Lair/Balance Window`
Expected: 윈도우가 뜨고 비-플레이 시 "플레이 모드에서만 사용 가능" 안내 + 결과 히스토리 표시.

- [ ] **Step 2: 수동 검증 — 치트 6종**

Play 진입 후 윈도우에서:
1. 강제 패시브 트리거 → 패시브 카드 선택 팝업 등장
2. 강제 액티브 트리거 → 액티브 카드 선택 팝업 등장
3. 카드 드롭다운에서 `SpawnSlimes` 선택 → 카드 즉시 적용 → 슬라임 소환 확인
4. 영웅 HP 100 입력 → 적용 → HUD HP 바 갱신 확인
5. 영웅 즉사 → 승리 결과 화면
6. 재시작 후 전투 종료 — 패배 → 패배 결과 화면

- [ ] **Step 3: 수동 검증 — 히스토리 표시**

한 판 종료 후 윈도우의 "새로고침" → 직전 판 요약 + 히스토리 리스트에 행 추가 확인. "초기화" → `Logs/lair_runs.jsonl` 삭제 + 리스트 비워짐 확인.

- [ ] **Step 4: EditMode + PlayMode 전체 테스트 재실행**

Run: Unity Test Runner > EditMode + PlayMode 전체
Expected: 전부 PASS.

- [ ] **Step 5: 스테이징 + 커밋 메시지(안)**

`git add`:
```
Assets/_Lair/Scripts/Battle/BattleController.cs
Assets/_Lair/Editor/LairBalanceWindow.cs
Assets/_Lair/Editor/LairBalanceWindow.cs.meta
```

커밋 메시지(안):
```
# [feat] - 슬라이스 C-M4 밸런싱 디버그 에디터 윈도우

- LairBalanceWindow 신규 — 플레이 중 치트 6종 + 결과 히스토리
- BattleController 디버그 API 6종 (#if UNITY_EDITOR)
```

---

## 완료 기준 (설계서 §8 대응)

- [ ] `BalanceConfig.asset` 수정 → 재시작만으로 캐릭터 스탯·전투 상수 반영 (재빌드 X)
- [ ] 캐릭터 빌더 재실행 후에도 스탯이 SO 기준 (프리팹은 구조만)
- [ ] 카드 .asset 효과값 튜닝이 `RebuildAllCards` 후 보존
- [ ] 디버그 윈도우 치트 6종 동작
- [ ] 한 판 종료 시 `Logs/lair_runs.jsonl` 기록 + 윈도우 누적 표시
- [ ] EditMode + PlayMode 테스트 전부 PASS
