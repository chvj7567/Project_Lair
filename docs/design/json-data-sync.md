# JSON Data Sync — 에디터 툴 기획서

---

## § 헤더

**목표**: CardData · CardPool · BalanceConfig ScriptableObject 를 JSON과 양방향 수동 동기화하는 에디터 툴 `LairJsonSyncWindow` 를 만든다.

**검증 가설**: JSON 직접 편집 + git diff 가 인스펙터 편집 + .asset 바이너리 대비 데이터 변경 추적·외부 편집 워크플로를 실제로 단순화하는가.

**현재 단계 범위 적합성**: MVP §11 게임 콘텐츠 범위(영웅 1·몬스터 6·카드 25) 외부. 단, MVP 단계의 밸런스 반복을 가속하는 개발 워크플로 도구이며, 사용자가 명시 승인한 추가 작업으로 진행한다.

**핵심 메커니즘**:
- 인스펙터 수정 후 Export 버튼 → JSON 3파일 생성(SO 원본 → JSON 공유본)
- JSON 직접 편집 또는 외부 툴 작성 후 Import 버튼 → SO 덮어쓰기(JSON 공유본 → SO 원본)
- 동기화 방향은 항상 수동 버튼 조작으로 결정. 자동 동기화 없음.

---

## 1. 기능 개요 및 배경

### 1.1 현재 문제

현재 카드 수치와 밸런스 수치는 Unity 인스펙터에서만 편집한다. 이 방식에는 세 가지 제약이 있다.

| 제약 | 내용 |
|---|---|
| 편집 편의 | VSCode 등 텍스트 에디터로 수치를 한눈에 비교하거나 일괄 편집할 수 없다 |
| 버전 관리 | `.asset` 바이너리 파일은 `git diff` 에서 의미 있는 diff 를 생산하지 않아 수치 변경 이력 추적이 어렵다 |
| 외부 툴 연동 | 구글 시트 등 기획 툴로 수치를 관리하고 Unity 에 반영하는 파이프라인이 없다 |

### 1.2 해결 방향

ScriptableObject 와 JSON 파일을 양방향으로 수동 동기화한다. JSON 파일은 `Assets/_Lair/Data/Json/` 폴더에 저장되고 git 추적 대상이 된다. 이로써 수치 변경은 사람이 읽을 수 있는 텍스트 diff 로 기록된다.

### 1.3 이 기능이 변경하지 않는 것

- 런타임 에셋 로딩 경로 — 게임 빌드는 여전히 SO 를 Addressables 로 로드한다
- SO 의 구조(필드·타입) — 기존 그대로
- 기획 원본의 위치 — SO 인스펙터가 여전히 테스트 편집 원본, JSON 은 공유·버전관리 원본

---

## 2. 대상 데이터

동기화 대상 ScriptableObject 는 다음 세 종류다.

| SO 타입 | 에셋 경로 | JSON 파일 |
|---|---|---|
| `CardData` | `Assets/_Lair/Art/Cards/Items/<id>.asset` × 25장 | `cards.json` |
| `CardPool` (Passive) | `Assets/_Lair/Art/Cards/CardPool_Passive.asset` | `card_pools.json` |
| `CardPool` (Active) | `Assets/_Lair/Art/Cards/CardPool_Active.asset` | `card_pools.json` |
| `BalanceConfig` | `Assets/_Lair/Data/BalanceConfig.asset` | `balance_config.json` |

**대상 아님**:
- Sprite, Prefab 등 Unity 에셋 레퍼런스 — JSON 으로 표현 불가
- `CardData._icon` (Sprite) — `LairCardPrefabBuilder` 가 `ECardId` 이름 PNG 로 자동 배정하므로 동기화 불필요

---

## 3. 동기화 방향 및 트리거

### 3.1 양방향 수동 동기화

```
[인스펙터 수정] → Export → JSON 파일  ← git add & commit (수동)
[외부 툴 편집] → JSON 파일 → Import → SO 덮어쓰기
```

- **Export**: SO 를 읽어 JSON 파일에 쓴다. 기존 JSON 파일이 있으면 덮어쓴다.
- **Import**: JSON 파일을 읽어 SO 에 쓴다. 대상 SO `.asset` 이 없으면 신규 생성, 있으면 덮어쓴다.
- **트리거**: `LairJsonSyncWindow` 의 버튼 클릭. 자동 동기화(AssetPostprocessor watch) 없음.

### 3.2 충돌 처리

Import 는 단방향 덮어쓰기다. SO 와 JSON 을 동시에 수정한 뒤 Import 하면 JSON 의 내용이 SO 를 덮어쓴다. 어느 쪽이 최신인지는 사용자가 git 으로 관리한다. 툴이 충돌을 감지하거나 merge 하지 않는다.

### 3.3 Import 후 처리

Import 완료 시 `AssetDatabase.SaveAssets()` 와 `AssetDatabase.Refresh()` 를 호출해 Unity 에디터가 변경을 즉시 인식하게 한다.

---

## 4. JSON 스키마 정의

JSON 파일 저장 위치: `Assets/_Lair/Data/Json/`

이 폴더는 에디터 전용이다. Addressables 등록 불필요, 런타임 로딩 없음.

### 4.1 cards.json

카드 전체 목록. 배열 형태. `icon` (Sprite) 은 포함하지 않는다.

```json
[
  {
    "id": "WispHpBoost",
    "category": "Enhance",
    "displayName": "위스프 HP 강화",
    "description": "위스프 HP 영구 +50%",
    "effect": {
      "$type": "WispHpBoostEffect"
    }
  },
  {
    "id": "SpawnWisps",
    "category": "Spawn",
    "displayName": "위스프 증폭",
    "description": "위스프 Spawner 동시 출력 +1",
    "effect": {
      "$type": "SpawnWispsEffect"
    }
  },
  {
    "id": "Fear",
    "category": "Environment",
    "displayName": "공포",
    "description": "영웅 이동속도 -30%",
    "effect": {
      "$type": "FearEffect"
    }
  },
  {
    "id": "Berserk",
    "category": "Enhance",
    "displayName": "광분",
    "description": "몬스터 전체 공격력 +30%",
    "effect": {
      "$type": "BerserkEffect",
      "duration": 15.0
    }
  }
]
```

**카드 25장 category 분류 확정**: `ECardCategory` 4종(`Enhance`/`Spawn`/`Replace`/`Environment`)으로 전체 25장이 분류된다. 신규 Enum 값 추가 없음. 실제 `.asset` 직렬화 기준 분포는 다음과 같다.

| ECardCategory | 패시브 (15장) | 액티브 (10장) |
|---|---|---|
| `Enhance` | WispHpBoost, WraithDamageBoost, ReaperAtkSpeed, HexRangeBoost, PlagueSlowBoost, PhantomMoveSpeedBoost | Frenzy, BloodThirst, IronWill, Berserk |
| `Spawn` | SpawnWisps, SpawnWraith, SpawnReapers, SpawnPlagues, SpawnPhantoms | Multiply |
| `Replace` | ReplaceWispsToWraith, ReplaceReapersToHex | (없음) |
| `Environment` | HeroPoisonAura, HeroAttackDown | Fear, Bleed, Weaken, Slow, TimeStop |

검산: Enhance 10 + Spawn 6 + Replace 2 + Environment 7 = 25장.

**필드 설명**:
- `id`: `ECardId` Enum 값명 (대소문자 일치 필수)
- `category`: `ECardCategory` Enum 값명
- `displayName`: 카드 표시 이름 (한글 가능)
- `description`: 카드 설명 텍스트
- `effect.$type`: C# 구상 클래스명 (네임스페이스 `Lair.Card` 제외, 클래스명만)
- `effect` 나머지 필드: 해당 Effect 클래스의 `[SerializeField]` private 필드 (필드명 앞 `_` 제거)

### 4.2 card_pools.json

풀 ID 목록. `passive` 와 `active` 키를 갖는 객체.

```json
{
  "passive": ["Bleed", "Weaken", "IronWill"],
  "active": ["Berserk", "SpawnWisps", "TimeStop"]
}
```

- 각 값은 `ECardId` Enum 값명 문자열
- Import 시 ID 에 대응하는 `Assets/_Lair/Art/Cards/Items/<id>.asset` 을 찾아 SO 레퍼런스로 연결

### 4.3 balance_config.json

영웅·몬스터 기본 스탯 및 트리거 타이밍.

```json
{
  "hero": { "hp": 500, "power": 10, "range": 3.0, "cooldown": 1.0, "moveSpeed": 3.0 },
  "monsters": [
    { "key": "Wisp", "stat": { "hp": 50, "power": 5, "range": 2.0, "cooldown": 1.0, "moveSpeed": 2.0 } }
  ],
  "runDuration": 300.0,
  "passiveThresholds": [0.9, 0.8, 0.7, 0.6, 0.5, 0.4, 0.3, 0.2, 0.1],
  "activeThresholds": [30, 60, 90, 120, 150, 180, 210, 240, 270]
}
```

- `monsters[].key`: `EMonster` Enum 값명
- `runDuration`: 초 단위 (300.0 = 5분)
- `passiveThresholds`: 영웅 HP 비율 (0.0~1.0)
- `activeThresholds`: 경과 초 (float)

---

## 5. ICardEffect 폴리모픽 처리

Unity `JsonUtility` 는 `[SerializeReference]` 폴리모픽 타입을 처리할 수 없다. `com.unity.nuget.newtonsoft-json` (Newtonsoft.Json) 을 사용한다.

### 5.1 직렬화 라이브러리

`Packages/manifest.json` 의 `dependencies` 에 `"com.unity.nuget.newtonsoft-json": "3.2.1"` 을 추가한다.

### 5.2 EffectConverter 동작

`JsonConverter<ICardEffect>` 구현체(`EffectConverter`)가 폴리모픽 직렬화를 담당한다.

- **Export**: `effect.GetType().Name` → JSON 의 `$type` 필드에 기록
- **Import**: `$type` 값 → `AppDomain.CurrentDomain.GetAssemblies()` 순회로 `Lair.Card.<typeName>` 타입을 찾아 역직렬화

```
"effect": { "$type": "BerserkEffect", "duration": 15.0 }
    ↓ Import
Type 검색 "Lair.Card.BerserkEffect" → new BerserkEffect { _duration = 15f }
```

새 Effect 클래스를 `Lair.Card` 네임스페이스에 추가하면 별도 등록 없이 자동 인식된다.

### 5.3 UnitySerializeFieldContractResolver 동작

`DefaultContractResolver` 파생 클래스(`UnitySerializeFieldContractResolver`)가 `[SerializeField]` private 필드를 Newtonsoft.Json 직렬화 대상으로 포함시킨다. 필드명 앞 `_` 는 제거해 JSON 키를 생성한다 (예: `_duration` → `"duration"`).

---

## 6. Editor UI 사양 — LairJsonSyncWindow

**메뉴 경로**: `Lair > JSON Sync`

**asmdef**: `Lair.Editor.JsonSync` (`Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef`)

**창 레이아웃**:

```
┌─ Lair JSON Sync ──────────────────────────────┐
│                                               │
│  [Export All → JSON]   [Import All ← JSON]   │
│                                               │
│  ─ Cards ───────────────────────────────────  │
│  [Export]  [Import]   cards.json              │
│                                               │
│  ─ Card Pools ──────────────────────────────  │
│  [Export]  [Import]   card_pools.json         │
│                                               │
│  ─ Balance Config ──────────────────────────  │
│  [Export]  [Import]   balance_config.json     │
│                                               │
└───────────────────────────────────────────────┘
```

**버튼 동작**:

| 버튼 | 동작 |
|---|---|
| `Export All → JSON` | Cards · Card Pools · Balance Config 순서로 Export 일괄 실행 |
| `Import All ← JSON` | Cards · Card Pools · Balance Config 순서로 Import 일괄 실행 |
| 각 섹션 `Export` | 해당 데이터만 Export |
| 각 섹션 `Import` | 해당 데이터만 Import |

**JSON 파일 미존재 시 동작**:
- 해당 섹션의 `Import` 버튼을 비활성화(`GUI.enabled = false`)한다
- 버튼 옆에 `"{파일명} 없음 — Export 먼저"` 안내 문구를 표시한다
- `Export All` / `Import All` 도 해당 항목의 JSON 이 없으면 그 항목의 Import 를 건너뛴다

---

## 7. 새 카드 추가 워크플로

새 카드를 추가할 때 다음 순서를 반드시 지킨다. **1번 단계(Enum 추가)는 JSON Import 보다 선행해야 한다**. Import 시 `ECardId` Enum 파싱이 실패하면 해당 카드가 생성되지 않는다.

| 단계 | 담당 | 상세 |
|---|---|---|
| 1 | gameplay-programmer | `CommonEnum.cs` 의 `ECardId` 에 새 카드 ID 값 추가. 이 단계는 본 도구 범위 밖 선행 코드 작업이다. |
| 2 | 기획자 | `Assets/_Lair/Data/Json/cards.json` 에 카드 항목 추가. `"id"` 값은 1에서 추가한 Enum 값명과 글자 그대로 일치해야 한다. |
| 3 | 기획자 | `Assets/_Lair/Data/Json/card_pools.json` 의 `"passive"` 또는 `"active"` 배열에 해당 ID 를 추가한다. |
| 4 | 기획자 | `LairJsonSyncWindow` 에서 Cards → Import 실행. `Assets/_Lair/Art/Cards/Items/<id>.asset` 이 없으면 신규 생성, 있으면 덮어쓴다. `ECardId` Enum 파싱에 실패한 카드는 해당 카드만 skip 하고 `Debug.LogWarning` 경고 로그를 출력한다. 나머지 카드 Import 는 계속 진행한다(전체 abort 하지 않음). |
| 5 | 기획자 | `LairJsonSyncWindow` 에서 Card Pools → Import 실행. 신규 생성된 CardData SO 를 풀에 연결한다. |
| 6 | 기획자 | Unity 인스펙터에서 생성된 CardData 를 열어 수치를 확인·조정한 뒤, Export 로 JSON 에 반영한다. |
| 7 | 기획자 (선택) | 아이콘 PNG 를 추가하거나 프리팹을 갱신해야 하면 `LairCardPrefabBuilder` 를 별도 실행한다. 본 도구는 Sprite 를 다루지 않는다. |

---

## 8. 제약 및 비범위

| 항목 | 내용 |
|---|---|
| 런타임 로딩 없음 | 이 도구는 에디터 전용. 빌드에 포함되지 않으며 게임 런타임은 여전히 SO 를 Addressables 로 로드한다. |
| 자동 동기화 없음 | AssetPostprocessor watch 방식의 자동 동기화를 구현하지 않는다. 모든 동기화는 수동 버튼 클릭으로만 발생한다. |
| Sprite·Prefab 미지원 | Unity 에셋 레퍼런스(`UnityEngine.Object` 파생)는 JSON 직렬화 대상이 아니다. `CardData._icon` 은 `LairCardPrefabBuilder` 가 관리한다. Import 시 SO 의 기존 `_icon` (Sprite 레퍼런스) 값은 건드리지 않고 그대로 보존한다. |
| 충돌 해결 없음 | Import 는 덮어쓰기만 한다. SO 와 JSON 을 동시에 수정한 경우 어느 쪽이 최신인지 툴이 판단하지 않으며 사용자가 git 으로 관리한다. |
| 자동 Prefab 빌드 없음 | Import 후 `LairCardPrefabBuilder` 자동 실행은 하지 않는다. 카드 프리팹 갱신이 필요하면 사용자가 별도로 `LairCardPrefabBuilder` 를 실행한다. |
| 신규 SO 타입 확장 불필요 | 이 도구는 신규 ScriptableObject 타입을 만들지 않는다. 기존 `CardData`·`CardPool`·`BalanceConfig` 를 `SerializedObject` 로 읽고 쓴다. |

---

## 9. 구현 요청사항

### 9.1 신규 Enum

신규 Enum 없음. 기존 `ECardId`·`ECardCategory`·`EMonster` 를 JSON 키로 사용한다.

새 카드 추가 시 사용자가 `ECardId` 에 값을 추가하는 것은 본 기획의 신규 Enum 추가가 아니라 §7 워크플로 1단계에 해당한다.

### 9.2 신규 Interface

신규 Interface 없음. 기존 `ICardEffect` (`Lair.Card` 네임스페이스) 를 `EffectConverter` 가 그대로 사용한다.

### 9.3 파일 경로 및 에셋 키

| 항목 | 값 |
|---|---|
| asmdef 이름 | `Lair.Editor.JsonSync` |
| asmdef 경로 | `Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef` |
| 메뉴 경로 | `Lair > JSON Sync` |
| JSON 저장 폴더 | `Assets/_Lair/Data/Json/` |
| cards.json 경로 | `Assets/_Lair/Data/Json/cards.json` |
| card_pools.json 경로 | `Assets/_Lair/Data/Json/card_pools.json` |
| balance_config.json 경로 | `Assets/_Lair/Data/Json/balance_config.json` |
| CardData 폴더 | `Assets/_Lair/Art/Cards/Items/` |
| CardPool_Passive 경로 | `Assets/_Lair/Art/Cards/CardPool_Passive.asset` |
| CardPool_Active 경로 | `Assets/_Lair/Art/Cards/CardPool_Active.asset` |
| BalanceConfig 경로 | `Assets/_Lair/Data/BalanceConfig.asset` |

### 9.4 신규 SO 스키마

신규 ScriptableObject 없음.

기존 SO 가 JSON 을 통해 쓰이는 필드 목록 (gameplay-programmer 가 `SerializedObject.FindProperty` 에서 사용하는 직렬화 필드명):

**CardData**:
- `_id` (ECardId Enum)
- `_category` (ECardCategory Enum)
- `_displayName` (string)
- `_description` (string)
- `_effect` (ICardEffect, `[SerializeReference]`)

**CardPool**:
- `_cards` (List\<CardData\>)

**BalanceConfig**:
- `_hero` (CharacterStat — 하위: `Hp`, `Power`, `Range`, `Cooldown`, `MoveSpeed`)
- `_monsters` (MonsterStatRow[] — 하위: `Key` (EMonster Enum), `Stat` (CharacterStat))
- `_runDuration` (float)
- `_passiveThresholds` (float[])
- `_activeThresholds` (float[])

### 9.5 신규 파일 목록 (plan 참조)

| 파일 | 역할 |
|---|---|
| `Assets/_Lair/Editor/JsonSync/Lair.Editor.JsonSync.asmdef` | 에디터 전용 격리 asmdef |
| `Assets/_Lair/Editor/JsonSync/EffectConverter.cs` | `JsonConverter<ICardEffect>` |
| `Assets/_Lair/Editor/JsonSync/UnitySerializeFieldContractResolver.cs` | `[SerializeField]` private 필드 직렬화 |
| `Assets/_Lair/Editor/JsonSync/JsonSyncSettings.cs` | `JsonSerializerSettings` 팩토리 |
| `Assets/_Lair/Editor/JsonSync/Dto/CardDataDto.cs` | CardData 직렬화 DTO |
| `Assets/_Lair/Editor/JsonSync/Dto/CardPoolDto.cs` | CardPool 직렬화 DTO |
| `Assets/_Lair/Editor/JsonSync/Dto/BalanceConfigDto.cs` | BalanceConfig 직렬화 DTO |
| `Assets/_Lair/Editor/JsonSync/CardDataSyncer.cs` | CardData Export/Import |
| `Assets/_Lair/Editor/JsonSync/CardPoolSyncer.cs` | CardPool Export/Import |
| `Assets/_Lair/Editor/JsonSync/BalanceConfigSyncer.cs` | BalanceConfig Export/Import |
| `Assets/_Lair/Editor/JsonSync/LairJsonSyncWindow.cs` | `Lair > JSON Sync` 에디터 창 |

---

## 스펙 커버리지

| 스펙 § | 기획서 § |
|---|---|
| §1 목적/동기 | §1 기능 개요 및 배경 |
| §2 범위 — 대상 SO | §2 대상 데이터 |
| §2 범위 — 대상 아님 | §8 제약 및 비범위 |
| §3.1 양방향 수동 동기화 | §3 동기화 방향 및 트리거 |
| §3.2 JSON 파일 위치 | §4 JSON 스키마 정의 + §9.3 |
| §3.3 직렬화 라이브러리 | §5.1 직렬화 라이브러리 |
| §4.1 cards.json 스키마 | §4.1 |
| §4.2 card_pools.json 스키마 | §4.2 |
| §4.3 balance_config.json 스키마 | §4.3 |
| §5.1 EffectConverter | §5.2 EffectConverter 동작 |
| §6 LairJsonSyncWindow UI | §6 Editor UI 사양 |
| §7 새 카드 추가 워크플로 | §7 새 카드 추가 워크플로 |
| §8 제약/비범위 | §8 제약 및 비범위 |
