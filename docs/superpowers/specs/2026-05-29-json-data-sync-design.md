# Spec — JSON Data Sync (양방향 SO ↔ JSON 동기화)

**날짜**: 2026-05-29  
**상태**: 승인됨

---

## 1. 목적 / 동기

- **편집 편의**: VSCode 등 텍스트 에디터로 카드·밸런스 수치 직접 수정
- **버전 관리 가독성**: git diff 에서 `.asset` 바이너리 대신 사람이 읽을 수 있는 JSON diff
- **외부 툴 연동**: 구글 시트 등 기획 툴 → JSON → Unity Import 파이프라인

---

## 2. 범위

대상 ScriptableObject:
- `CardData` (개별 카드 `.asset` × 25)
- `CardPool` (`CardPool_Passive`, `CardPool_Active`)
- `BalanceConfig`

대상 아님: Sprite·Prefab 등 Unity 에셋 참조, 런타임 로딩 경로 변경 없음.

---

## 3. 아키텍처

### 3.1 방향: 양방향 수동 동기화

SO가 테스트 편집 원본, JSON이 공식 저장·공유 원본. 어느 쪽에서 시작해도 동기화 가능.

```
[인스펙터 수정] → Export → JSON 파일  ← git add & commit (수동)
[외부 툴 편집] → JSON 파일 → Import → SO 덮어쓰기
```

### 3.2 JSON 파일 위치

```
Assets/_Lair/Data/Json/
  cards.json
  card_pools.json
  balance_config.json
```

에디터 전용 폴더 — Addressables 등록 불필요, 런타임 로딩 없음.

### 3.3 직렬화 라이브러리

`com.unity.nuget.newtonsoft-json` (Newtonsoft.Json)  
Unity `JsonUtility`는 `[SerializeReference]` 폴리모픽 타입 처리 불가.

---

## 4. JSON 스키마

### 4.1 cards.json

```json
[
  {
    "id": "Berserk",
    "category": "Active",
    "displayName": "폭주",
    "description": "모든 몬스터 HP 즉시 -50% + 데미지 ↑",
    "effect": {
      "$type": "BerserkEffect",
      "duration": 15.0
    }
  },
  {
    "id": "SpawnWisps",
    "category": "Active",
    "displayName": "위스프 증폭",
    "description": "위스프 Spawner 동시 출력 +1",
    "effect": {
      "$type": "SpawnWispsEffect"
    }
  }
]
```

- `icon` (Sprite) 제외 — `LairCardPrefabBuilder`가 ECardId 이름 PNG로 자동 배정
- `$type` = C# 구상 클래스명 (네임스페이스 제외)

### 4.2 card_pools.json

```json
{
  "passive": ["Bleed", "Weaken", "IronWill"],
  "active": ["Berserk", "SpawnWisps", "TimeStop"]
}
```

카드 ID 문자열 목록. Import 시 ID로 `CardData` `.asset` 참조를 연결.  
Import Cards 시: ID에 해당하는 `.asset`이 없으면 `Assets/_Lair/Art/Cards/Items/<id>.asset` 에 신규 생성, 있으면 덮어쓰기.

### 4.3 balance_config.json

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

---

## 5. ICardEffect 폴리모픽 처리

### 5.1 EffectConverter

Newtonsoft `JsonConverter<ICardEffect>` 구현체.

- **Export**: `effect.GetType().Name` → `$type` 필드에 기록
- **Import**: `$type` 값 → `Type.GetType("Lair.Card." + typeName)` → 역직렬화

```
"effect": { "$type": "BerserkEffect", "duration": 15.0 }
    ↓ Import
Type.GetType("Lair.Card.BerserkEffect") → new BerserkEffect { _duration = 15f }
```

새 Effect 클래스 추가 시 별도 등록 없이 자동 인식 (reflection 기반, `Lair.Card` 네임스페이스 고정).

> 구현 시 주의: Unity asmdef 환경에서 `Type.GetType`은 어셈블리 한정 이름 필요.  
> `Type.GetType("Lair.Card.BerserkEffect, Lair")` 형식 또는 `AppDomain.CurrentDomain.GetAssemblies()` 순회로 처리.

---

## 6. Editor UI — LairJsonSync 윈도우

**메뉴**: `Lair > JSON Sync`

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

- Import 후 `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()` 호출
- JSON 파일 미존재 시 Import 버튼 비활성화 + 안내 문구 표시
- asmdef: `Lair.Editor` (에디터 전용)

---

## 7. 새 카드 추가 워크플로

1. `CommonEnum.cs` 의 `ECardId` 에 새 값 추가 (Rule 02 §8) — Import 전에 먼저 필요
2. `cards.json` 에 카드 항목 추가 (`"id"` 값 = 1에서 추가한 Enum 값명)
3. `card_pools.json` 에 해당 ID 추가
4. `LairJsonSync` → Import Cards → Import Card Pools → `.asset` 신규 생성
5. 인스펙터에서 수치 테스트 → Export로 JSON 동기화

---

## 8. 제약 / 비범위

- 런타임 JSON 로딩 없음 — 에디터 툴 전용
- Sprite·Prefab 참조는 JSON 대상 아님
- Import 충돌 해결 로직 없음 — 덮어쓰기 단방향. 어느 쪽이 최신인지는 사용자가 관리
- 자동 동기화(AssetPostprocessor watch) 없음 — 항상 수동 버튼
