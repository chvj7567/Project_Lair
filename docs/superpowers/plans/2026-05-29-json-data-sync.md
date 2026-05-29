# JSON Data Sync 구현 플랜

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CardData·CardPool·BalanceConfig ScriptableObject 를 JSON과 양방향 수동 동기화하는 에디터 툴(LairJsonSyncWindow)을 구현한다.

**Architecture:** `Assets/_Lair/Editor/JsonSync/` 서브폴더에 전용 `Lair.Editor.JsonSync.asmdef` 를 생성해 기존 에디터 스크립트와 격리. Newtonsoft.Json을 직렬화 엔진으로 사용하고, `[SerializeField]` private 필드를 존중하는 `UnitySerializeFieldContractResolver`로 ICardEffect 폴리모픽 타입을 처리. Import는 기존 LairCardPrefabBuilder와 동일한 SerializedObject + managedReferenceValue 패턴 사용.

**Tech Stack:** `com.unity.nuget.newtonsoft-json`, UnityEditor(SerializedObject/AssetDatabase), NUnit (EditMode)

---

## 파일 구조

**신규 생성:**
```
Packages/manifest.json                                        ← Newtonsoft.Json 추가
Assets/_Lair/Editor/JsonSync/
  Lair.Editor.JsonSync.asmdef                                 ← 격리된 에디터 asmdef
  EffectConverter.cs                                          ← JsonConverter<ICardEffect>
  UnitySerializeFieldContractResolver.cs                      ← [SerializeField] private 필드 직렬화
  JsonSyncSettings.cs                                         ← JsonSerializerSettings 팩토리
  Dto/
    CardDataDto.cs                                            ← CardData 직렬화 DTO
    CardPoolDto.cs                                            ← CardPool 직렬화 DTO
    BalanceConfigDto.cs                                       ← BalanceConfig 직렬화 DTO
  CardDataSyncer.cs                                           ← Export/Import CardData
  CardPoolSyncer.cs                                           ← Export/Import CardPool
  BalanceConfigSyncer.cs                                      ← Export/Import BalanceConfig
  LairJsonSyncWindow.cs                                       ← Lair > JSON Sync 에디터 창
Assets/_Lair/Tests/EditMode/JsonSync/
  EffectConverterTests.cs
  CardDataSyncerTests.cs
  BalanceConfigSyncerTests.cs
```

**수정:**
```
Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef        ← Lair.Editor.JsonSync 참조 추가
```

---

## Task 1: Newtonsoft.Json 패키지 추가 + asmdef 셋업

**Files:**
- Modify: `Packages/manifest.json`
- Create: `Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef`
- Modify: `Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef`

- [ ] **Step 1: manifest.json 에 Newtonsoft.Json 추가**

`Packages/manifest.json` 의 `dependencies` 에 다음 줄 추가:
```json
"com.unity.nuget.newtonsoft-json": "3.2.1",
```

- [ ] **Step 2: Lair.Editor.JsonSync.asmdef 생성**

`Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef`:
```json
{
  "name": "Lair.Editor.JsonSync",
  "rootNamespace": "Lair.EditorTools",
  "references": ["Lair"],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": false,
  "defineConstraints": [],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 3: Lair.Tests.EditMode.asmdef 에 참조 추가**

`Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef` 의 `references` 배열에 `"Lair.Editor.JsonSync"` 추가:
```json
{
  "name": "Lair.Tests.EditMode",
  "rootNamespace": "Lair.Tests",
  "references": [
    "Lair",
    "com.chvj.unityinfra",
    "Lair.Editor.JsonSync",
    "UnityEngine.TestRunner",
    "UnityEditor.TestRunner"
  ],
  "includePlatforms": ["Editor"],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": true,
  "precompiledReferences": [
    "nunit.framework.dll"
  ],
  "autoReferenced": false,
  "defineConstraints": ["UNITY_INCLUDE_TESTS"],
  "versionDefines": [],
  "noEngineReferences": false
}
```

- [ ] **Step 4: Unity 에디터에서 컴파일 에러 없음 확인**

Unity 에디터 Console 탭에서 빨간 에러가 없으면 OK. Newtonsoft.Json 패키지 다운로드까지 잠시 대기.

---

## Task 2: DTO 클래스 정의

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/Dto/CardDataDto.cs`
- Create: `Assets/_Lair/Editor/JsonSync/Dto/CardPoolDto.cs`
- Create: `Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs`

- [ ] **Step 1: CardDataDto 생성**

`Assets/_Lair/Editor/JsonSync/Dto/CardDataDto.cs`:
```csharp
using Newtonsoft.Json;
using Lair.Card;

namespace Lair.EditorTools
{
    public class CardDataDto
    {
        [JsonProperty("id")]          public string Id;
        [JsonProperty("category")]    public string Category;
        [JsonProperty("displayName")] public string DisplayName;
        [JsonProperty("description")] public string Description;
        [JsonProperty("effect")]      public ICardEffect Effect;
    }
}
```

- [ ] **Step 2: CardPoolDto 생성**

`Assets/_Lair/Editor/JsonSync/Dto/CardPoolDto.cs`:
```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lair.EditorTools
{
    public class CardPoolDto
    {
        [JsonProperty("passive")] public List<string> Passive = new List<string>();
        [JsonProperty("active")]  public List<string> Active  = new List<string>();
    }
}
```

- [ ] **Step 3: BalanceConfigDto 생성**

`Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs`:
```csharp
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Lair.EditorTools
{
    public class CharacterStatDto
    {
        [JsonProperty("hp")]        public int   Hp;
        [JsonProperty("power")]     public int   Power;
        [JsonProperty("range")]     public float Range;
        [JsonProperty("cooldown")]  public float Cooldown;
        [JsonProperty("moveSpeed")] public float MoveSpeed;
    }

    public class MonsterStatRowDto
    {
        [JsonProperty("key")]  public string         Key;
        [JsonProperty("stat")] public CharacterStatDto Stat;
    }

    public class BalanceConfigDto
    {
        [JsonProperty("hero")]              public CharacterStatDto       Hero;
        [JsonProperty("monsters")]          public List<MonsterStatRowDto> Monsters = new List<MonsterStatRowDto>();
        [JsonProperty("runDuration")]       public float                  RunDuration;
        [JsonProperty("passiveThresholds")] public float[]                PassiveThresholds;
        [JsonProperty("activeThresholds")]  public float[]                ActiveThresholds;
    }
}
```

---

## Task 3: EffectConverter + UnitySerializeFieldContractResolver + JsonSyncSettings (TDD)

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/UnitySerializeFieldContractResolver.cs`
- Create: `Assets/_Lair/Editor/JsonSync/EffectConverter.cs`
- Create: `Assets/_Lair/Editor/JsonSync/JsonSyncSettings.cs`
- Create: `Assets/_Lair/Tests/EditMode/JsonSync/EffectConverterTests.cs`

- [ ] **Step 1: 테스트 파일 작성 (실패 확인용)**

`Assets/_Lair/Tests/EditMode/JsonSync/EffectConverterTests.cs`:
```csharp
using System.Reflection;
using Newtonsoft.Json;
using NUnit.Framework;
using Lair.Card;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class EffectConverterTests
    {
        //# BerserkEffect 의 $type 필드가 JSON 에 기록되는지 확인
        [Test]
        public void BerserkEffect_Export_포함TypeField()
        {
            ICardEffect effect = new BerserkEffect();
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);

            Assert.IsTrue(json.Contains("\"$type\""), $"$type 필드 없음: {json}");
            Assert.IsTrue(json.Contains("BerserkEffect"), $"타입명 없음: {json}");
        }

        //# 직렬화 → 역직렬화 후 구상 타입 보존
        [Test]
        public void BerserkEffect_RoundTrip_타입보존()
        {
            ICardEffect effect = new BerserkEffect();
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsInstanceOf<BerserkEffect>(result);
        }

        //# [SerializeField] private float _duration 값이 JSON 을 거쳐도 보존
        [Test]
        public void BerserkEffect_RoundTrip_duration기본값보존()
        {
            ICardEffect effect = new BerserkEffect(); //# _duration 기본값 15f
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            float duration = (float)typeof(BerserkEffect)
                .GetField("_duration", BindingFlags.NonPublic | BindingFlags.Instance)
                .GetValue(result);
            Assert.AreEqual(15f, duration, 0.001f);
        }

        //# Effect 없는 카드 (null) 도 Export/Import 가능
        [Test]
        public void NullEffect_RoundTrip_null반환()
        {
            ICardEffect effect = null;
            JsonSerializerSettings settings = JsonSyncSettings.Build();

            string json = JsonConvert.SerializeObject(effect, typeof(ICardEffect), settings);
            ICardEffect result = JsonConvert.DeserializeObject<ICardEffect>(json, settings);

            Assert.IsNull(result);
        }
    }
}
```

- [ ] **Step 2: 테스트가 컴파일 에러로 실패하는지 확인**

Unity Test Runner (Window > General > Test Runner) > EditMode > EffectConverterTests 실행.  
Expected: `JsonSyncSettings` not found 등 컴파일 에러 또는 테스트 미발견.

- [ ] **Step 3: UnitySerializeFieldContractResolver 구현**

`Assets/_Lair/Editor/JsonSync/UnitySerializeFieldContractResolver.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace Lair.EditorTools
{
    //# [SerializeField] private 필드를 존중하는 ContractResolver.
    //# 필드명 앞의 _ 를 제거해 JSON 키를 깔끔하게 만든다 (예: _duration → "duration").
    public class UnitySerializeFieldContractResolver : DefaultContractResolver
    {
        protected override IList<JsonProperty> CreateProperties(Type type, MemberSerialization memberSerialization)
        {
            List<JsonProperty> props = new List<JsonProperty>();
            FieldInfo[] fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo field in fields)
            {
                bool isPublic = field.IsPublic;
                bool hasSerializeField = field.GetCustomAttribute<SerializeField>() != null;
                if (isPublic == false && hasSerializeField == false) continue;

                JsonProperty prop = base.CreateProperty(field, memberSerialization);
                prop.Readable = true;
                prop.Writable = true;
                prop.PropertyName = field.Name.TrimStart('_');
                props.Add(prop);
            }
            return props;
        }
    }
}
```

- [ ] **Step 4: EffectConverter 구현**

`Assets/_Lair/Editor/JsonSync/EffectConverter.cs`:
```csharp
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Lair.Card;

namespace Lair.EditorTools
{
    public class EffectConverter : JsonConverter<ICardEffect>
    {
        private readonly JsonSerializer _inner;

        public EffectConverter()
        {
            _inner = new JsonSerializer
            {
                ContractResolver = new UnitySerializeFieldContractResolver()
            };
        }

        public override ICardEffect ReadJson(JsonReader reader, Type objectType,
            ICardEffect existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string typeName = jo["$type"]?.Value<string>();
            if (string.IsNullOrEmpty(typeName))
                return null;

            Type type = FindEffectType(typeName);
            if (type == null)
                throw new JsonException($"[EffectConverter] 알 수 없는 Effect 타입: {typeName}");

            ICardEffect effect = (ICardEffect)Activator.CreateInstance(type);
            using (JsonReader jr = jo.CreateReader())
                _inner.Populate(jr, effect);
            return effect;
        }

        public override void WriteJson(JsonWriter writer, ICardEffect value, JsonSerializer serializer)
        {
            JObject jo = JObject.FromObject(value, _inner);
            jo.AddFirst(new JProperty("$type", value.GetType().Name));
            jo.WriteTo(writer);
        }

        private static Type FindEffectType(string typeName)
        {
            foreach (System.Reflection.Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type t = asm.GetType($"Lair.Card.{typeName}");
                if (t != null) return t;
            }
            return null;
        }
    }
}
```

- [ ] **Step 5: JsonSyncSettings 구현**

`Assets/_Lair/Editor/JsonSync/JsonSyncSettings.cs`:
```csharp
using Newtonsoft.Json;

namespace Lair.EditorTools
{
    public static class JsonSyncSettings
    {
        public static JsonSerializerSettings Build()
        {
            return new JsonSerializerSettings
            {
                Formatting    = Formatting.Indented,
                Converters    = { new EffectConverter() },
                NullValueHandling = NullValueHandling.Include
            };
        }
    }
}
```

- [ ] **Step 6: 테스트 재실행 — 전체 통과 확인**

Unity Test Runner > EditMode > EffectConverterTests 4개 모두 초록불 확인.

- [ ] **Step 7: 커밋**

```
git add Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef
git add Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef.meta
git add Assets/_Lair/Editor/JsonSync/UnitySerializeFieldContractResolver.cs
git add Assets/_Lair/Editor/JsonSync/UnitySerializeFieldContractResolver.cs.meta
git add Assets/_Lair/Editor/JsonSync/EffectConverter.cs
git add Assets/_Lair/Editor/JsonSync/EffectConverter.cs.meta
git add Assets/_Lair/Editor/JsonSync/JsonSyncSettings.cs
git add Assets/_Lair/Editor/JsonSync/JsonSyncSettings.cs.meta
git add Assets/_Lair/Editor/JsonSync/Dto/CardDataDto.cs
git add Assets/_Lair/Editor/JsonSync/Dto/CardDataDto.cs.meta
git add Assets/_Lair/Editor/JsonSync/Dto/CardPoolDto.cs
git add Assets/_Lair/Editor/JsonSync/Dto/CardPoolDto.cs.meta
git add Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs
git add Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs.meta
git add Assets/_Lair/Tests/EditMode/JsonSync/EffectConverterTests.cs
git add Assets/_Lair/Tests/EditMode/JsonSync/EffectConverterTests.cs.meta
git add Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef
git add Packages/manifest.json
```

커밋 메시지 안:
```
# [feat] - EffectConverter + JsonSyncSettings + DTO 클래스 + Newtonsoft.Json 추가
```

---

## Task 4: CardDataSyncer + 테스트 (TDD)

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/CardDataSyncer.cs`
- Create: `Assets/_Lair/Tests/EditMode/JsonSync/CardDataSyncerTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/_Lair/Tests/EditMode/JsonSync/CardDataSyncerTests.cs`:
```csharp
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Data;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class CardDataSyncerTests
    {
        private CardData _card;

        [SetUp]
        public void SetUp()
        {
            _card = ScriptableObject.CreateInstance<CardData>();
            SerializedObject so = new SerializedObject(_card);
            so.FindProperty("_id").enumValueIndex          = (int)ECardId.Berserk;
            so.FindProperty("_category").enumValueIndex    = (int)ECardCategory.Enhance;
            so.FindProperty("_displayName").stringValue    = "폭주";
            so.FindProperty("_description").stringValue    = "테스트 설명";
            so.FindProperty("_effect").managedReferenceValue = new BerserkEffect();
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_card);
        }

        //# Export → JSON 에 id 필드 포함
        [Test]
        public void ExportToJson_Id포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("Berserk", arr[0]["id"]?.Value<string>());
        }

        //# Export → JSON 에 displayName 한글 포함
        [Test]
        public void ExportToJson_DisplayName포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("폭주", arr[0]["displayName"]?.Value<string>());
        }

        //# Export → JSON 에 effect.$type 포함
        [Test]
        public void ExportToJson_EffectType포함()
        {
            string json = CardDataSyncer.ExportToJson(new List<CardData> { _card });
            JArray arr = JArray.Parse(json);

            Assert.AreEqual("BerserkEffect", arr[0]["effect"]?["$type"]?.Value<string>());
        }

        //# ApplyDto → displayName 갱신
        [Test]
        public void ApplyDto_DisplayName갱신()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Berserk",
                Category    = "Enhance",
                DisplayName = "새이름",
                Description = "새설명",
                Effect      = new BerserkEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.AreEqual("새이름", _card.DisplayName);
        }

        //# ApplyDto → effect 타입 갱신 (BerserkEffect → FrenzyEffect)
        [Test]
        public void ApplyDto_Effect타입갱신()
        {
            CardDataDto dto = new CardDataDto
            {
                Id          = "Frenzy",
                Category    = "Enhance",
                DisplayName = "광폭화",
                Description = "설명",
                Effect      = new FrenzyEffect()
            };

            CardDataSyncer.ApplyDto(dto, _card);

            Assert.IsInstanceOf<FrenzyEffect>(_card.Effect);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 컴파일 에러 확인**

Unity Test Runner > CardDataSyncerTests 실행. `CardDataSyncer` 미존재 컴파일 에러 예상.

- [ ] **Step 3: CardDataSyncer 구현**

`Assets/_Lair/Editor/JsonSync/CardDataSyncer.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Card;
using Lair.Data;

namespace Lair.EditorTools
{
    public static class CardDataSyncer
    {
        private const string JsonPath = "Assets/_Lair/Data/Json/cards.json";
        private const string CardDir  = "Assets/_Lair/Art/Cards/Items";

        //# CardData SO 목록 → JSON 문자열
        public static string ExportToJson(IEnumerable<CardData> cards)
        {
            List<CardDataDto> dtos = new List<CardDataDto>();
            foreach (CardData card in cards)
            {
                dtos.Add(new CardDataDto
                {
                    Id          = card.Id.ToString(),
                    Category    = card.Category.ToString(),
                    DisplayName = card.DisplayName,
                    Description = card.Description,
                    Effect      = card.Effect
                });
            }
            return JsonConvert.SerializeObject(dtos, JsonSyncSettings.Build());
        }

        //# DTO 를 기존 CardData SO 에 적용 (SerializedObject 사용, LairCardPrefabBuilder 동일 패턴)
        public static void ApplyDto(CardDataDto dto, CardData card)
        {
            SerializedObject so = new SerializedObject(card);
            so.FindProperty("_id").enumValueIndex          = (int)Enum.Parse(typeof(ECardId), dto.Id);
            so.FindProperty("_category").enumValueIndex    = (int)Enum.Parse(typeof(ECardCategory), dto.Category);
            so.FindProperty("_displayName").stringValue    = dto.DisplayName;
            so.FindProperty("_description").stringValue    = dto.Description;
            so.FindProperty("_effect").managedReferenceValue = dto.Effect;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(card);
        }

        //# AssetDatabase 에서 전체 CardData 로드 → JSON 파일 저장
        public static void Export()
        {
            string[] guids = AssetDatabase.FindAssets("t:CardData", new[] { CardDir });
            List<CardData> cards = new List<CardData>();
            foreach (string guid in guids)
            {
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(AssetDatabase.GUIDToAssetPath(guid));
                if (card != null) cards.Add(card);
            }

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, ExportToJson(cards), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[CardDataSyncer] {cards.Count}장 Export → {JsonPath}");
        }

        //# JSON 파일 → CardData SO 생성/갱신
        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            List<CardDataDto> dtos = JsonConvert.DeserializeObject<List<CardDataDto>>(json, JsonSyncSettings.Build());

            foreach (CardDataDto dto in dtos)
            {
                string assetPath = $"{CardDir}/{dto.Id}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(assetPath);
                bool isNew = card == null;
                if (isNew) card = ScriptableObject.CreateInstance<CardData>();

                ApplyDto(dto, card);

                if (isNew)
                {
                    AssetDatabase.CreateAsset(card, assetPath);
                    Debug.Log($"[CardDataSyncer] 신규 생성: {dto.Id}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardDataSyncer] {dtos.Count}장 Import ← {JsonPath}");
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행 — 전체 통과 확인**

Unity Test Runner > CardDataSyncerTests 5개 모두 초록불.

- [ ] **Step 5: 커밋**

```
git add Assets/_Lair/Editor/JsonSync/CardDataSyncer.cs
git add Assets/_Lair/Editor/JsonSync/CardDataSyncer.cs.meta
git add Assets/_Lair/Tests/EditMode/JsonSync/CardDataSyncerTests.cs
git add Assets/_Lair/Tests/EditMode/JsonSync/CardDataSyncerTests.cs.meta
```

커밋 메시지 안:
```
# [feat] - CardDataSyncer Export/Import + 테스트 추가
```

---

## Task 5: CardPoolSyncer 구현

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/CardPoolSyncer.cs`

(CardPool Export/Import는 AssetDatabase 의존적이라 단위 테스트 대신 Task 7 에서 수동 검증)

- [ ] **Step 1: CardPoolSyncer 구현**

`Assets/_Lair/Editor/JsonSync/CardPoolSyncer.cs`:
```csharp
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Card;

namespace Lair.EditorTools
{
    public static class CardPoolSyncer
    {
        private const string JsonPath   = "Assets/_Lair/Data/Json/card_pools.json";
        private const string PassivePath = "Assets/_Lair/Art/Cards/CardPool_Passive.asset";
        private const string ActivePath  = "Assets/_Lair/Art/Cards/CardPool_Active.asset";
        private const string CardDir    = "Assets/_Lair/Art/Cards/Items";

        public static void Export()
        {
            CardPool passive = AssetDatabase.LoadAssetAtPath<CardPool>(PassivePath);
            CardPool active  = AssetDatabase.LoadAssetAtPath<CardPool>(ActivePath);

            CardPoolDto dto = new CardPoolDto
            {
                Passive = passive?.Cards.Select(c => c.Id.ToString()).ToList() ?? new List<string>(),
                Active  = active?.Cards.Select(c => c.Id.ToString()).ToList()  ?? new List<string>()
            };

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(dto, JsonSyncSettings.Build()), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[CardPoolSyncer] Export → {JsonPath}");
        }

        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            CardPoolDto dto = JsonConvert.DeserializeObject<CardPoolDto>(json);

            ApplyPool(PassivePath, dto.Passive);
            ApplyPool(ActivePath,  dto.Active);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[CardPoolSyncer] Import ← {JsonPath}");
        }

        private static void ApplyPool(string poolPath, List<string> cardIds)
        {
            CardPool pool = AssetDatabase.LoadAssetAtPath<CardPool>(poolPath);
            if (pool == null)
            {
                Debug.LogWarning($"[CardPoolSyncer] 풀 없음: {poolPath}");
                return;
            }

            SerializedObject so = new SerializedObject(pool);
            SerializedProperty listProp = so.FindProperty("_cards");
            listProp.arraySize = cardIds.Count;

            for (int i = 0; i < cardIds.Count; ++i)
            {
                string cardPath = $"{CardDir}/{cardIds[i]}.asset";
                CardData card = AssetDatabase.LoadAssetAtPath<CardData>(cardPath);
                if (card == null)
                    Debug.LogWarning($"[CardPoolSyncer] 카드 없음: {cardIds[i]}");
                listProp.GetArrayElementAtIndex(i).objectReferenceValue = card;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pool);
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }
    }
}
```

- [ ] **Step 2: 커밋**

```
git add Assets/_Lair/Editor/JsonSync/CardPoolSyncer.cs
git add Assets/_Lair/Editor/JsonSync/CardPoolSyncer.cs.meta
```

커밋 메시지 안:
```
# [feat] - CardPoolSyncer Export/Import 추가
```

---

## Task 6: BalanceConfigSyncer + 테스트 (TDD)

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs`
- Create: `Assets/_Lair/Tests/EditMode/JsonSync/BalanceConfigSyncerTests.cs`

- [ ] **Step 1: 테스트 파일 작성**

`Assets/_Lair/Tests/EditMode/JsonSync/BalanceConfigSyncerTests.cs`:
```csharp
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Lair.Data;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class BalanceConfigSyncerTests
    {
        private BalanceConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<BalanceConfig>();
            SerializedObject so = new SerializedObject(_config);

            SerializedProperty heroProp = so.FindProperty("_hero");
            heroProp.FindPropertyRelative("Hp").intValue         = 500;
            heroProp.FindPropertyRelative("Power").intValue      = 10;
            heroProp.FindPropertyRelative("Range").floatValue    = 3f;
            heroProp.FindPropertyRelative("Cooldown").floatValue = 1f;
            heroProp.FindPropertyRelative("MoveSpeed").floatValue = 3f;

            so.FindProperty("_runDuration").floatValue = 300f;

            SerializedProperty passProp = so.FindProperty("_passiveThresholds");
            passProp.arraySize = 2;
            passProp.GetArrayElementAtIndex(0).floatValue = 0.9f;
            passProp.GetArrayElementAtIndex(1).floatValue = 0.8f;

            SerializedProperty activeProp = so.FindProperty("_activeThresholds");
            activeProp.arraySize = 2;
            activeProp.GetArrayElementAtIndex(0).floatValue = 30f;
            activeProp.GetArrayElementAtIndex(1).floatValue = 60f;

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        //# Export → hero.hp 필드 포함
        [Test]
        public void ExportToJson_HeroHp포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);

            Assert.AreEqual(500, obj["hero"]?["hp"]?.Value<int>());
        }

        //# Export → runDuration 포함
        [Test]
        public void ExportToJson_RunDuration포함()
        {
            string json = BalanceConfigSyncer.ExportToJson(_config);
            JObject obj = JObject.Parse(json);

            Assert.AreEqual(300f, obj["runDuration"]?.Value<float>(), 0.001f);
        }

        //# ApplyDto → hero.hp 갱신
        [Test]
        public void ApplyDto_HeroHp갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 999, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new System.Collections.Generic.List<MonsterStatRowDto>(),
                RunDuration = 300f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(999, _config.Hero.Hp);
        }

        //# ApplyDto → runDuration 갱신
        [Test]
        public void ApplyDto_RunDuration갱신()
        {
            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero = new CharacterStatDto { Hp = 500, Power = 10, Range = 3f, Cooldown = 1f, MoveSpeed = 3f },
                Monsters = new System.Collections.Generic.List<MonsterStatRowDto>(),
                RunDuration = 600f,
                PassiveThresholds = new float[] { 0.9f },
                ActiveThresholds  = new float[] { 30f }
            };

            BalanceConfigSyncer.ApplyDto(dto, _config);

            Assert.AreEqual(600f, _config.RunDuration, 0.001f);
        }
    }
}
```

- [ ] **Step 2: 테스트 실행 — 컴파일 에러 확인**

Unity Test Runner > BalanceConfigSyncerTests 실행. `BalanceConfigSyncer` 미존재 에러 예상.

- [ ] **Step 3: BalanceConfigSyncer 구현**

`Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs`:
```csharp
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Data;

namespace Lair.EditorTools
{
    public static class BalanceConfigSyncer
    {
        private const string JsonPath   = "Assets/_Lair/Data/Json/balance_config.json";
        private const string ConfigPath = "Assets/_Lair/Data/BalanceConfig.asset";

        //# BalanceConfig SO → JSON 문자열 (공개 프로퍼티 경유, Enum 순회로 monster 목록 구성)
        public static string ExportToJson(BalanceConfig config)
        {
            List<MonsterStatRowDto> monsters = new List<MonsterStatRowDto>();
            foreach (EMonster monster in Enum.GetValues(typeof(EMonster)))
            {
                BalanceConfig.CharacterStat stat = config.GetMonster(monster);
                if (stat == null) continue;
                monsters.Add(new MonsterStatRowDto
                {
                    Key  = monster.ToString(),
                    Stat = ToDto(stat)
                });
            }

            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero              = ToDto(config.Hero),
                Monsters          = monsters,
                RunDuration       = config.RunDuration,
                PassiveThresholds = config.PassiveThresholds,
                ActiveThresholds  = config.ActiveThresholds
            };

            return JsonConvert.SerializeObject(dto, JsonSyncSettings.Build());
        }

        //# DTO 를 BalanceConfig SO 에 적용
        public static void ApplyDto(BalanceConfigDto dto, BalanceConfig config)
        {
            SerializedObject so = new SerializedObject(config);

            ApplyStatDto(so.FindProperty("_hero"), dto.Hero);

            SerializedProperty monstersProp = so.FindProperty("_monsters");
            monstersProp.arraySize = dto.Monsters.Count;
            for (int i = 0; i < dto.Monsters.Count; ++i)
            {
                SerializedProperty row = monstersProp.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Key").enumValueIndex = (int)Enum.Parse(typeof(EMonster), dto.Monsters[i].Key);
                ApplyStatDto(row.FindPropertyRelative("Stat"), dto.Monsters[i].Stat);
            }

            so.FindProperty("_runDuration").floatValue = dto.RunDuration;

            SetFloatArray(so.FindProperty("_passiveThresholds"), dto.PassiveThresholds);
            SetFloatArray(so.FindProperty("_activeThresholds"),  dto.ActiveThresholds);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        //# AssetDatabase 에서 BalanceConfig 로드 → JSON 저장
        public static void Export()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[BalanceConfigSyncer] BalanceConfig 없음: {ConfigPath}");
                return;
            }

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, ExportToJson(config), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceConfigSyncer] Export → {JsonPath}");
        }

        //# JSON 파일 → BalanceConfig SO 갱신
        public static void Import()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[BalanceConfigSyncer] BalanceConfig 없음: {ConfigPath}");
                return;
            }

            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            BalanceConfigDto dto = JsonConvert.DeserializeObject<BalanceConfigDto>(json);

            ApplyDto(dto, config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[BalanceConfigSyncer] Import ← {JsonPath}");
        }

        private static CharacterStatDto ToDto(BalanceConfig.CharacterStat stat) =>
            new CharacterStatDto
            {
                Hp        = stat.Hp,
                Power     = stat.Power,
                Range     = stat.Range,
                Cooldown  = stat.Cooldown,
                MoveSpeed = stat.MoveSpeed
            };

        private static void ApplyStatDto(SerializedProperty prop, CharacterStatDto dto)
        {
            prop.FindPropertyRelative("Hp").intValue         = dto.Hp;
            prop.FindPropertyRelative("Power").intValue      = dto.Power;
            prop.FindPropertyRelative("Range").floatValue    = dto.Range;
            prop.FindPropertyRelative("Cooldown").floatValue = dto.Cooldown;
            prop.FindPropertyRelative("MoveSpeed").floatValue = dto.MoveSpeed;
        }

        private static void SetFloatArray(SerializedProperty prop, float[] values)
        {
            if (values == null) return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
                prop.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }
    }
}
```

- [ ] **Step 4: 테스트 재실행 — 전체 통과 확인**

Unity Test Runner > BalanceConfigSyncerTests 4개 모두 초록불.

- [ ] **Step 5: 커밋**

```
git add Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs
git add Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs.meta
git add Assets/_Lair/Tests/EditMode/JsonSync/BalanceConfigSyncerTests.cs
git add Assets/_Lair/Tests/EditMode/JsonSync/BalanceConfigSyncerTests.cs.meta
```

커밋 메시지 안:
```
# [feat] - BalanceConfigSyncer Export/Import + 테스트 추가
```

---

## Task 7: LairJsonSyncWindow 구현

**Files:**
- Create: `Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs`

- [ ] **Step 1: LairJsonSyncWindow 구현**

`Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs`:
```csharp
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    public class LairJsonSyncWindow : EditorWindow
    {
        private const string JsonDir = "Assets/_Lair/Data/Json";

        [MenuItem("Lair/JSON Sync")]
        public static void Open() => GetWindow<LairJsonSyncWindow>("Lair JSON Sync");

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export All → JSON", GUILayout.Height(30))) ExportAll();
            if (GUILayout.Button("Import All ← JSON", GUILayout.Height(30))) ImportAll();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            DrawSection("Cards",         "cards.json",         CardDataSyncer.Export,      CardDataSyncer.Import);
            DrawSection("Card Pools",    "card_pools.json",    CardPoolSyncer.Export,      CardPoolSyncer.Import);
            DrawSection("Balance Config","balance_config.json",BalanceConfigSyncer.Export, BalanceConfigSyncer.Import);
        }

        private static void DrawSection(string label, string fileName, Action export, Action import)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export")) export();

            string fullPath = Path.Combine(JsonDir, fileName);
            bool fileExists = File.Exists(fullPath);
            GUI.enabled = fileExists;
            if (GUILayout.Button("Import")) import();
            GUI.enabled = true;

            if (fileExists == false)
                EditorGUILayout.HelpBox($"{fileName} 없음 — Export 먼저", MessageType.None);

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private static void ExportAll()
        {
            CardDataSyncer.Export();
            CardPoolSyncer.Export();
            BalanceConfigSyncer.Export();
        }

        private static void ImportAll()
        {
            CardDataSyncer.Import();
            CardPoolSyncer.Import();
            BalanceConfigSyncer.Import();
        }
    }
}
```

- [ ] **Step 2: 커밋**

```
git add Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs
git add Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs.meta
```

커밋 메시지 안:
```
# [feat] - LairJsonSyncWindow (Lair > JSON Sync 메뉴) 추가
```

---

## Task 8: 초기 Export + 수동 검증

- [ ] **Step 1: Unity 에서 Lair > JSON Sync 메뉴 열기**

Unity 에디터 상단 메뉴 `Lair > JSON Sync` 클릭. 창이 열리면 OK.

- [ ] **Step 2: Export All → JSON 실행**

`Export All → JSON` 버튼 클릭.  
Console에 다음 3줄 로그 확인:
```
[CardDataSyncer] 25장 Export → Assets/_Lair/Data/Json/cards.json
[CardPoolSyncer] Export → Assets/_Lair/Data/Json/card_pools.json
[BalanceConfigSyncer] Export → Assets/_Lair/Data/Json/balance_config.json
```

- [ ] **Step 3: JSON 파일 내용 확인**

`Assets/_Lair/Data/Json/cards.json` 열어 Berserk 카드 항목에서:
- `"id": "Berserk"` 있음
- `"effect": { "$type": "BerserkEffect", "duration": 15.0 }` 있음

`balance_config.json` 에서 `"hero": { "hp": ... }` 있음.

- [ ] **Step 4: Import All ← JSON 실행 + 검증**

`Import All ← JSON` 버튼 클릭.  
Unity 인스펙터에서 임의 CardData 열어 값이 그대로인지 확인.  
Console에 에러 없음 확인.

- [ ] **Step 5: JSON 파일 스테이징**

```
git add Assets/_Lair/Data/Json/cards.json
git add Assets/_Lair/Data/Json/card_pools.json
git add Assets/_Lair/Data/Json/balance_config.json
git add Assets/_Lair/Data/Json.meta
```

커밋 메시지 안:
```
# [asset] - 초기 JSON 데이터 파일 Export (cards / card_pools / balance_config)
```

---

## 스펙 커버리지 확인

| 스펙 §       | 구현 태스크     |
|-------------|----------------|
| 3.1 양방향 수동 동기화 | Task 4–6 Export/Import |
| 3.2 JSON 파일 위치 | Task 4–6 JsonPath 상수 |
| 3.3 Newtonsoft.Json | Task 1 manifest.json |
| 4.1 cards.json 스키마 | Task 3–4 EffectConverter + CardDataSyncer |
| 4.2 card_pools.json | Task 5 CardPoolSyncer |
| 4.3 balance_config.json | Task 6 BalanceConfigSyncer |
| 5.1 EffectConverter | Task 3 |
| 6. LairJsonSyncWindow | Task 7 |
| 7. 새 카드 워크플로 | Task 8 Step 4 |
