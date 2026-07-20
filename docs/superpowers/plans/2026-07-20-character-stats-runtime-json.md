# 캐릭터 스탯 런타임 JSON 로드 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax. (start-develop 파이프라인 route B 로 진행 시 이 플랜은 game-designer·gameplay-programmer 의 입력.)

**Goal:** `balance_config.json` 을 런타임에 직접 읽어 캐릭터 스탯을 적용, 빌드 없이(모든 플랫폼) 값 수정을 가능케 한다. JSON authoritative + SO fallback(오버레이).

**Architecture:** 기존 편집 툴의 DTO 를 런타임으로 옮기고, `BalanceJsonLoader` 가 (에디터=StreamingAssets 직접 / 플레이어=StreamingAssets→persistentDataPath 복사)에서 JSON 을 읽어 DTO 로 파싱한다. `BattleController.Start` 는 `Instantiate(_balance)` 복제본에 DTO 를 오버레이(있는·유효한 값만 덮고 나머지는 SO 값 유지)해 사용한다. 원본 asset 불변.

**Tech Stack:** Unity 6(6000.0.68f1), C#, Newtonsoft.Json(com.unity.nuget.newtonsoft-json, auto-referenced), Unity Test Framework(NUnit) EditMode.

## Global Constraints

- Rule 02: `//#` 주석, 가드절 개행, `var` 금지, `!` 금지(`== null`/`== false`), GetComponent Awake 1회.
- Rule 04 §2: `Resources/` 금지. 런타임 데이터는 StreamingAssets/persistentDataPath(File.IO·UnityWebRequest).
- Rule 01: 자동 커밋 금지. 각 Task "Commit" 은 스테이징 + 메시지(안)까지. 파이프라인 route B 면 최종 커밋은 6단계에서 일괄.
- namespace: 런타임 코드 `Lair.Data` / `Lair.Battle`. Editor 코드 `Lair.EditorTools`.
- **동작 보존**: JSON = 현재 SO 값이면 게임 수치·동작 불변 → 기존 EditMode/PlayMode 스위트 PASS 가 회귀 기준.
- IL2CPP(안드로이드) AOT: DTO 에 `[UnityEngine.Scripting.Preserve]`. 실제 IL2CPP 빌드 파싱 확인은 사용자/에디터 몫.
- 파일명 상수: `balance_config.json`.

---

### Task 1: DTO 를 런타임으로 이동 + [Preserve]

**Files:**
- Move: `Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs` → `Assets/_Lair/Scripts/Data/Dto/BalanceConfigDto.cs` (`git mv`, .meta 동행)
- Modify: 이동 파일의 `namespace Lair.EditorTools` → `namespace Lair.Data`, 멤버에 `[Preserve]`
- Modify: `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs` (DTO 가 `Lair.Data` 로 이동 — `using Lair.Data;` 이미 있음, 확인만)

**Interfaces:**
- Produces: `Lair.Data.BalanceConfigDto`(필드 `Hero:CharacterStatDto`, `Monsters:List<MonsterStatRowDto>`, `RunDuration:float`, `PassiveThresholds:float[]`, `ActiveThresholds:float[]`), `Lair.Data.CharacterStatDto`(`Hp:int,Power:int,Range:float,Cooldown:float,MoveSpeed:float`), `Lair.Data.MonsterStatRowDto`(`Key:string,Stat:CharacterStatDto,SpawnPeriod:float`). JsonProperty 키 불변(hp/power/range/cooldown/moveSpeed/key/stat/spawnPeriod/hero/monsters/runDuration/passiveThresholds/activeThresholds).

- [ ] **Step 1:** `git mv Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs Assets/_Lair/Scripts/Data/Dto/BalanceConfigDto.cs` (+ `.meta` 동행). `Assets/_Lair/Scripts/Data/Dto/` 폴더 없으면 생성(+폴더 .meta).
- [ ] **Step 2:** 이동 파일 편집 — namespace 변경 + `[Preserve]`:

```csharp
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Scripting;

namespace Lair.Data
{
    //# 타입 레벨 [Preserve] — IL2CPP AOT 에서 Newtonsoft 가 ctor+populate 하도록 전체 보존.
    //# DTO 는 매개변수 없는 기본 ctor 보유(암시적) — Newtonsoft construct 가능.
    [Preserve]
    public class CharacterStatDto
    {
        [Preserve] [JsonProperty("hp")]        public int   Hp;
        [Preserve] [JsonProperty("power")]     public int   Power;
        [Preserve] [JsonProperty("range")]     public float Range;
        [Preserve] [JsonProperty("cooldown")]  public float Cooldown;
        [Preserve] [JsonProperty("moveSpeed")] public float MoveSpeed;
    }

    [Preserve]
    public class MonsterStatRowDto
    {
        [Preserve] [JsonProperty("key")]         public string          Key;
        [Preserve] [JsonProperty("stat")]        public CharacterStatDto Stat;
        [Preserve] [JsonProperty("spawnPeriod")] public float           SpawnPeriod;
    }

    [Preserve]
    public class BalanceConfigDto
    {
        [Preserve] [JsonProperty("hero")]              public CharacterStatDto        Hero;
        [Preserve] [JsonProperty("monsters")]          public List<MonsterStatRowDto> Monsters = new List<MonsterStatRowDto>();
        [Preserve] [JsonProperty("runDuration")]       public float                   RunDuration;
        [Preserve] [JsonProperty("passiveThresholds")] public float[]                 PassiveThresholds;
        [Preserve] [JsonProperty("activeThresholds")]  public float[]                 ActiveThresholds;
    }
}
```

- [ ] **Step 3:** `BalanceConfigSyncer.cs` 가 여전히 컴파일되는지 확인 — `using Lair.Data;` 존재(L7)하므로 DTO 타입 해석 OK. 변경 불필요(확인만). `Lair.EditorTools` 네임스페이스 유지.
- [ ] **Step 4:** 런타임 Newtonsoft 접근성 확인 — `Lair.asmdef`(`Assets/_Lair/Scripts/Lair.asmdef`)가 Newtonsoft 를 auto-reference 로 잡는지. 이동한 DTO(`Lair.Data`, `using Newtonsoft.Json`)가 런타임 어셈블리에서 컴파일되면 OK. **컴파일 실패(Newtonsoft 미해석) 시에만** `Lair.asmdef` 의 `"overrideReferences": true` + `"precompiledReferences": ["Newtonsoft.Json.dll"]` 또는 `references` 에 Newtonsoft asmdef 추가. (JsonSync editor asmdef 이 명시 참조 없이 Newtonsoft 를 쓰므로 auto-reference 로 추정 — 실패 시에만 조치.)
- [ ] **Step 5: Commit(안)** — `# [refactor] - 밸런스 DTO 를 런타임에서 쓰도록 Data 로 이동`

---

### Task 2: `BalanceJsonLoader.Parse` 순수 파싱 함수 + 단위 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs`
- Test: `Assets/_Lair/Tests/EditMode/Data/BalanceJsonLoaderParseTests.cs`

**Interfaces:**
- Consumes: `Lair.Data.BalanceConfigDto` (Task 1).
- Produces: `public static BalanceConfigDto Parse(string json)` — 빈/공백/깨진 JSON → null(예외 삼킴). 유효 JSON → DTO.

- [ ] **Step 1: 실패 테스트 작성**

```csharp
using NUnit.Framework;
using Lair.Data;

namespace Lair.Tests.EditMode.Data
{
    public class BalanceJsonLoaderParseTests
    {
        [Test]
        public void 유효_JSON은_DTO로_파싱된다()
        {
            string json = "{\"hero\":{\"hp\":1000,\"power\":50,\"range\":1.5,\"cooldown\":1.0,\"moveSpeed\":3.0},\"monsters\":[{\"key\":\"Reaper\",\"stat\":{\"hp\":30,\"power\":5,\"range\":1.0,\"cooldown\":1.2,\"moveSpeed\":2.0},\"spawnPeriod\":9.0}],\"runDuration\":300.0,\"passiveThresholds\":[0.9,0.5],\"activeThresholds\":[30.0]}";
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto);
            Assert.AreEqual(1000, dto.Hero.Hp);
            Assert.AreEqual(1, dto.Monsters.Count);
            Assert.AreEqual("Reaper", dto.Monsters[0].Key);
            Assert.AreEqual(9.0f, dto.Monsters[0].SpawnPeriod);
            Assert.AreEqual(300.0f, dto.RunDuration);
        }

        [Test]
        public void 깨진_JSON은_null을_반환한다()
        {
            Assert.IsNull(BalanceJsonLoader.Parse("{ this is not json"));
        }

        [Test]
        public void 빈문자열은_null을_반환한다()
        {
            Assert.IsNull(BalanceJsonLoader.Parse(""));
            Assert.IsNull(BalanceJsonLoader.Parse("   "));
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — EditMode 실행. Expected: FAIL(`BalanceJsonLoader` 없음).
- [ ] **Step 3: 구현**

```csharp
using System;
using Newtonsoft.Json;
using UnityEngine;

namespace Lair.Data
{
    //# balance_config.json 런타임 로더. 파싱은 순수 함수로 분리(파일IO 없이 테스트 가능).
    public static class BalanceJsonLoader
    {
        public const string FileName = "balance_config.json";

        //# 빈/공백/깨진 JSON → null. 예외를 삼켜 호출부가 SO fallback 하도록.
        public static BalanceConfigDto Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                return null;
            try
            {
                return JsonConvert.DeserializeObject<BalanceConfigDto>(json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[BalanceJsonLoader] JSON 파싱 실패 — SO fallback: {e.Message}");
                return null;
            }
        }
    }
}
```

- [ ] **Step 4: 통과 확인** — EditMode 실행. Expected: PASS(3건).
- [ ] **Step 5: Commit(안)** — `# [feat] - 밸런스 JSON 파싱 순수함수 추가`

---

### Task 3: `BalanceConfig.OverlayFromDto` 오버레이+검증 + 단위 테스트

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/BalanceConfig.cs` (`OverlayFromDto` 메서드 추가)
- Test: `Assets/_Lair/Tests/EditMode/Data/BalanceConfigOverlayTests.cs`

**Interfaces:**
- Consumes: `BalanceConfigDto` (Task 1).
- Produces: `public void OverlayFromDto(BalanceConfigDto dto)` — dto 의 hero/monster/스칼라를 **검증 통과 시에만** 이 인스턴스 필드에 대입. 누락·불량(≤0 등)은 스킵(기존값 유지)+경고.

- [ ] **Step 1: 실패 테스트 작성** — `Instantiate` 로 SO 복제본 만들고(원본 불변), 부분 dto 오버레이 시 있는 값만 바뀌고 누락/불량은 유지되는지 검증.

```csharp
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.EditMode.Data
{
    public class BalanceConfigOverlayTests
    {
        private BalanceConfig MakeConfigWithHero(int hp, int power)
        {
            BalanceConfig c = ScriptableObject.CreateInstance<BalanceConfig>();
            //# 테스트 전용 시드 — 런타임 세터로 hero 기본값 주입
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = hp, Power = power, Range = 1.5f, Cooldown = 1f, MoveSpeed = 3f }
            });
            return c;
        }

        [Test]
        public void 유효_hero_dto는_hero스탯을_덮는다()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 2000, Power = 80, Range = 2f, Cooldown = 0.8f, MoveSpeed = 4f }
            });
            Assert.AreEqual(2000, c.Hero.Hp);
            Assert.AreEqual(80, c.Hero.Power);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void 불량값_hero_dto는_스킵되고_기존값_유지()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            //# Hp<=0 불량 → hero 오버레이 스킵, 기존 1000 유지
            c.OverlayFromDto(new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 0, Power = 80, Range = 2f, Cooldown = 0.8f, MoveSpeed = 4f }
            });
            Assert.AreEqual(1000, c.Hero.Hp);
            Assert.AreEqual(50, c.Hero.Power);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void hero가_null인_dto는_기존_hero를_유지()
        {
            BalanceConfig c = MakeConfigWithHero(1000, 50);
            c.OverlayFromDto(new BalanceConfigDto { Hero = null, RunDuration = 250f });
            Assert.AreEqual(1000, c.Hero.Hp);
            Assert.AreEqual(250f, c.RunDuration);
            Object.DestroyImmediate(c);
        }
    }
}
```

- [ ] **Step 2: 실패 확인** — Expected: FAIL(`OverlayFromDto` 없음).
- [ ] **Step 3: 구현** — `BalanceConfig.cs` 에 추가(기존 필드 `_hero`/`_monsters`/`_runDuration`/`_passiveThresholds`/`_activeThresholds` 대입). CharacterStat 검증 헬퍼 포함.

```csharp
//# JSON DTO 를 이 인스턴스(복제본)에 오버레이. 있고·검증 통과한 값만 덮고, 없거나 불량이면 기존 SO 값 유지.
//# 런타임 전용 — Editor 의 ApplyDto(SerializedObject)와 별개.
public void OverlayFromDto(BalanceConfigDto dto)
{
    if (dto == null)
        return;

    if (TryBuildStat(dto.Hero, out CharacterStat hero))
        _hero = hero;

    if (dto.Monsters != null)
    {
        foreach (MonsterStatRowDto row in dto.Monsters)
        {
            if (row == null)
                continue;
            if (Enum.TryParse(row.Key, out EMonster key) == false)
            {
                Debug.LogWarning($"[BalanceConfig] EMonster 파싱 실패 — skip: {row.Key}");
                continue;
            }
            OverlayMonster(key, row);
        }
    }

    if (dto.RunDuration > 0f)
        _runDuration = dto.RunDuration;
    if (dto.PassiveThresholds != null && dto.PassiveThresholds.Length > 0)
        _passiveThresholds = dto.PassiveThresholds;
    if (dto.ActiveThresholds != null && dto.ActiveThresholds.Length > 0)
        _activeThresholds = dto.ActiveThresholds;
}

//# dto 스탯이 유효(모든 값 > 0)하면 CharacterStat 로 빌드. 불량이면 false + 경고.
private static bool TryBuildStat(CharacterStatDto dto, out CharacterStat stat)
{
    stat = null;
    if (dto == null)
        return false;
    if (dto.Hp <= 0 || dto.Power <= 0 || dto.Range <= 0f || dto.Cooldown <= 0f || dto.MoveSpeed <= 0f)
    {
        Debug.LogWarning($"[BalanceConfig] 불량 스탯 — skip (hp={dto.Hp},power={dto.Power},range={dto.Range},cd={dto.Cooldown},spd={dto.MoveSpeed})");
        return false;
    }
    stat = new CharacterStat
    {
        Hp = dto.Hp, Power = dto.Power, Range = dto.Range, Cooldown = dto.Cooldown, MoveSpeed = dto.MoveSpeed
    };
    return true;
}

//# 기존 monster 행이 있으면 Stat/SpawnPeriod 를 유효할 때만 갱신.
//# SO 에 없는 키는 스탯이 유효할 때만 새 행 추가 — 불량 스탯으로 제로행(HP 0 등)을 주입하지 않는다(손편집 방어).
private void OverlayMonster(EMonster key, MonsterStatRowDto row)
{
    MonsterStatRow target = null;
    if (_monsters != null)
    {
        foreach (MonsterStatRow r in _monsters)
        {
            if (r != null && r.Key == key) { target = r; break; }
        }
    }

    bool statOk = TryBuildStat(row.Stat, out CharacterStat s);

    if (target == null)
    {
        //# 새 키: 유효 스탯이 있어야만 행 생성. 불량이면 아예 추가 안 함(제로행 방지).
        if (statOk == false)
            return;
        target = new MonsterStatRow { Key = key, Stat = s, SpawnPeriod = row.SpawnPeriod > 0f ? row.SpawnPeriod : 0f };
        List<MonsterStatRow> list = _monsters != null ? new List<MonsterStatRow>(_monsters) : new List<MonsterStatRow>();
        list.Add(target);
        _monsters = list.ToArray();
        return;
    }

    //# 기존 키: 유효할 때만 각각 갱신, 불량/누락은 기존값 유지.
    if (statOk)
        target.Stat = s;
    if (row.SpawnPeriod > 0f)
        target.SpawnPeriod = row.SpawnPeriod;
}
```

> `BalanceConfig.cs` 상단에 `using System;` · `using System.Collections.Generic;` 추가 필요(Enum.TryParse·List).

- [ ] **Step 4: 통과 확인** — Expected: PASS(3건).
- [ ] **Step 5: Commit(안)** — `# [feat] - 밸런스 SO 에 JSON 오버레이(검증+fallback) 메서드 추가`

---

### Task 4: `BalanceJsonLoader.LoadAsync` 파일 로드(경로 전략)

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs` (`LoadAsync` 추가)

**Interfaces:**
- Consumes: `Parse`(Task 2), `BalanceConfigDto`.
- Produces: `public static async Task<BalanceConfigDto> LoadAsync()` — 에디터=StreamingAssets 직접, 플레이어=persistentDataPath(없으면 StreamingAssets 복사). 파일 없음/읽기 실패 → null.

- [ ] **Step 1: 구현** (파일IO·플랫폼 분기라 순수 단위테스트 대상 아님 — 에디터 검증)

```csharp
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

//# (BalanceJsonLoader 클래스 내부에 추가)

//# 런타임 로드. 에디터: StreamingAssets 직접(그 파일을 편집). 플레이어: persistentDataPath(첫 실행 시 StreamingAssets 복사).
public static async Task<BalanceConfigDto> LoadAsync()
{
    string text = await ReadJsonAsync();
    return Parse(text);
}

private static async Task<string> ReadJsonAsync()
{
#if UNITY_EDITOR
    string editorPath = Path.Combine(Application.streamingAssetsPath, FileName);
    if (File.Exists(editorPath) == false)
    {
        Debug.LogWarning($"[BalanceJsonLoader] StreamingAssets 파일 없음 — SO fallback: {editorPath}");
        return null;
    }
    return File.ReadAllText(editorPath);
#else
    string persistent = Path.Combine(Application.persistentDataPath, FileName);
    if (File.Exists(persistent) == false)
    {
        //# 첫 실행 — StreamingAssets 원본을 persistentDataPath 로 복사(쓰기가능 경로).
        string src = Path.Combine(Application.streamingAssetsPath, FileName);
        string seed = await ReadStreamingAssetAsync(src);
        if (string.IsNullOrEmpty(seed))
        {
            Debug.LogWarning($"[BalanceJsonLoader] StreamingAssets 기본값 없음 — SO fallback: {src}");
            return null;
        }
        File.WriteAllText(persistent, seed);
    }
    return File.ReadAllText(persistent);
#endif
}

//# StreamingAssets 읽기 — 안드로이드는 APK 내부 URL 이라 UnityWebRequest, 그 외는 File.IO.
private static async Task<string> ReadStreamingAssetAsync(string path)
{
    if (path.Contains("://"))
    {
        using (UnityWebRequest req = UnityWebRequest.Get(path))
        {
            UnityWebRequestAsyncOperation op = req.SendWebRequest();
            while (op.isDone == false)
                await Task.Yield();
            if (req.result != UnityWebRequest.Result.Success)
                return null;
            return req.downloadHandler.text;
        }
    }
    return File.Exists(path) ? File.ReadAllText(path) : null;
}
```

- [ ] **Step 2: 컴파일 확인** — Unity 콘솔 에러 0.
- [ ] **Step 3: 에디터 수동 검증(사용자/test-engineer)** — StreamingAssets 에 balance_config.json 존재 시 `LoadAsync` 가 DTO 반환, 파일 지우면 null 반환 로그. (Task 6 이후 파일 위치 확정됨.)
- [ ] **Step 4: Commit(안)** — `# [feat] - 밸런스 JSON 런타임 로더(플랫폼별 경로) 추가`

---

### Task 5: `BattleController` 통합 — 클론 + 로드 + 오버레이

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs` (`Start` 최상단, `_balance` 최초 사용 L122 앞)

**Interfaces:**
- Consumes: `BalanceJsonLoader.LoadAsync`(Task 4), `BalanceConfig.OverlayFromDto`(Task 3).
- Produces: 없음(내부 통합). 이후 모든 `_balance` 참조가 JSON 오버레이된 복제본을 가리킴.

- [ ] **Step 1:** `async void Start()`(L110) 최상단, 기존 `if (_balance == null)`(L122) 검사 **앞**에 런타임 오버레이 삽입:

```csharp
//# 런타임 스탯 오버레이 — 원본 asset 클로버링 방지 위해 복제본에만 적용(에디터 Play 종료 후 asset 불변).
//# JSON authoritative + SO fallback: JSON 있고 유효하면 그 값, 없거나 깨지면 SO 기본값 유지.
if (_balance != null)
{
    BalanceConfig runtime = Instantiate(_balance);
    runtime.hideFlags = HideFlags.HideAndDontSave;   //# 클론은 asset 아님 — 인스펙터/세이브 오염 방지
    BalanceConfigDto dto = await BalanceJsonLoader.LoadAsync();
    if (dto != null)
        runtime.OverlayFromDto(dto);
    else
        Debug.LogWarning("[BattleController] 밸런스 JSON 없음/실패 — SO 기본값으로 진행");
    _balance = runtime;
    _balanceRuntimeClone = runtime;   //# OnDestroy 에서 Destroy — 씬 재시작 시 클론 누수 방지
}
```

> 필드 추가: `private BalanceConfig _balanceRuntimeClone;`. `OnDestroy`(없으면 추가)에서 `if (_balanceRuntimeClone != null) Destroy(_balanceRuntimeClone);` 로 정리(클론 누수 방지).

> `using Lair.Data;` 가 BattleController 에 있는지 확인(없으면 추가 — `BalanceConfigDto` 참조). `_balance` 를 복제본으로 재대입하므로 이후 `_balance.RunDuration`(L128)·`ApplyStats`·`Balance` 프로퍼티·Spawner 바인딩이 모두 오버레이본을 읽음. `_balance == null` 분기는 기존 fallback 로그 유지(회귀 없음).

- [ ] **Step 2: 컴파일 확인** — 콘솔 에러 0.
- [ ] **Step 3: 검증** — 배틀 진입 시 (a) JSON 존재+값 변경 → 그 값이 반영, (b) JSON 삭제 → SO 값으로 진행+경고, (c) 에디터 Play 종료 후 `BalanceConfig.asset` 원본 값 불변(클로버링 없음) 확인.
- [ ] **Step 4: Commit(안)** — `# [feat] - 전투 시작 시 밸런스 JSON 을 복제본에 오버레이해 적용`

---

### Task 6: `balance_config.json` StreamingAssets 정본화 (asset 재-Export) + 툴 게이팅 정합

> **BLOCKER 대응**: (1) stale `Data/Json/balance_config.json`(activeThresholds 9개)을 그대로 옮기면 5→9 밸런스 변경이 되므로 **asset 에서 재-Export** 해 일치시킨다. (2) `LairJsonSyncWindow` 의 경로는 표기가 아니라 Import 버튼 활성/스킵 **동작 게이팅**이라 balance 만 StreamingAssets 로 분기해야 한다.

**Files:**
- Modify: `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs` (`JsonPath` 상수 → StreamingAssets)
- Modify: `Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs` (balance 게이팅 경로 분기 — L47·L83)
- Generate: `Assets/StreamingAssets/balance_config.json` (asset→JSON Export 로 생성, activeThresholds 5개 = asset 일치)
- Delete: `Assets/_Lair/Data/Json/balance_config.json` (+ .meta) — stale 정본 제거

**Interfaces:**
- Produces: 없음. (에디터 툴이 쓰는 파일) = (빌드 실리는 기본값) = (런타임이 읽는 파일) 일원화 + asset 일치.

- [ ] **Step 1:** `BalanceConfigSyncer.cs` — `private const string JsonPath = "Assets/StreamingAssets/balance_config.json";` 로 갱신.
- [ ] **Step 2:** `LairJsonSyncWindow.cs` — balance 항목만 StreamingAssets 경로로 게이팅 분기. window 의 파일 존재검사(`OnGUI` L47 `Path.Combine(JsonDir, fileName)`, `ImportAll` L83 `Path.Combine(JsonDir, "balance_config.json")`) 두 곳 모두 balance 는 `Assets/StreamingAssets/balance_config.json` 를 보게 한다. 예: `DrawSection`/`ImportAll` 에서 balance 파일명일 때 경로를 StreamingAssets 로 치환(상수 `BalanceJsonPath = "Assets/StreamingAssets/balance_config.json"` 도입). cards/pools/hero_skills 는 `JsonDir` 유지.

```csharp
//# LairJsonSyncWindow — balance 만 StreamingAssets, 나머지는 JsonDir.
private const string BalanceJsonPath = "Assets/StreamingAssets/balance_config.json";

private static string PathFor(string fileName) =>
    fileName == "balance_config.json" ? BalanceJsonPath : Path.Combine(JsonDir, fileName);
//# DrawSection 의 Path.Combine(JsonDir, fileName) → PathFor(fileName)
//# ImportAll 의 balance 존재검사 File.Exists(Path.Combine(JsonDir,"balance_config.json")) → File.Exists(BalanceJsonPath)
```

- [ ] **Step 3:** 에디터에서 `Lair/JSON Sync → Balance Export` 실행 — asset(`BalanceConfig.asset`)을 기준으로 `Assets/StreamingAssets/balance_config.json` 생성. (Export 는 asset→JSON 이므로 activeThresholds 가 asset 의 5개로 기록됨.)
- [ ] **Step 4:** stale `Assets/_Lair/Data/Json/balance_config.json`(+ `.meta`) 삭제(`git rm`).
- [ ] **Step 5: 검증(동작 보존 게이트)** — 생성된 `Assets/StreamingAssets/balance_config.json` 의 `activeThresholds` 가 **5개 `[30,90,150,210,270]`** 로 asset 과 일치하는지 확인(9개면 실패 — 재Export). 런타임 `LoadAsync`(Task 4)가 이 파일을 읽어 오버레이해도 게임 동작 불변임을 보장.
- [ ] **Step 6: Commit(안)** — `# [chore] - balance_config.json 을 StreamingAssets 로 정본화(asset 일치, 런타임 로드)`

---

## Self-Review

**Spec coverage:**
- §4.1 DTO 런타임 이동 + 타입/멤버 Preserve + Newtonsoft(auto-ref) → Task 1. ✅
- §4.2 BalanceJsonLoader(Parse 순수 + LoadAsync 경로전략) → Task 2·4. ✅
- §4.3 SO 클론(+cleanup) + OverlayFromDto(오버레이·검증·제로행방지·fallback) + BattleController 통합 → Task 3·5. ✅
- §4.4 balance_config.json StreamingAssets 정본화 + 툴 게이팅 분기 → Task 6. ✅
- §2.4 출시 JSON == asset(activeThresholds 5개) → Task 6 재-Export + Step5 검증 게이트. ✅ (BLOCKER 1)
- §5 테스트(Parse 정상/누락/불량/깨짐, OverlayFromDto 오버레이/검증/fallback) → Task 2·3 단위테스트. ✅
- §5 동작 보존 → JSON=asset 이면 불변, 기존 스위트 회귀 기준 + Task 6 Step5 threshold 일치 검증. ✅
- §5.1 워크플로 함정(persistentDataPath 스테일·Inspector 가려짐) → spec 문서화(구현 영향 없음). ✅

**Placeholder scan:** 코드 스텝은 전량 실제 코드. 파일IO/통합(Task 4·5)·Export(Task 6 Step3)는 순수 단위테스트 부적합이라 에디터 검증 스텝 명시(플레이스홀더 아님). "적절히 처리" 류 없음. ✅

**Type consistency:** `BalanceConfigDto`/`CharacterStatDto`/`MonsterStatRowDto`(Lair.Data), `Parse(string)→DTO`, `LoadAsync()→Task<DTO>`, `OverlayFromDto(dto)`, `BalanceConfig.CharacterStat`/`MonsterStatRow` 기존 타입명 일치. JsonProperty 키 불변. `_balanceRuntimeClone` 필드 Task 5 내 일관. ✅

**의존성 주의:** Newtonsoft 런타임 접근은 auto-reference 가정(Task 1 Step 4 검증, 실패 시에만 asmdef 조치 — spec §4.1 과 일치). IL2CPP AOT 는 타입+멤버 `[Preserve]` + 매개변수없는 ctor, 실제 빌드 확인은 사용자 몫(§5).
