# Loading Scene 설계 스펙

**날짜**: 2026-05-28  
**단계**: MVP  
**담당**: LoadingController + LoadingHud (경량 MonoBehaviour — Rule 02 §6 가벼운 화면 예외)

---

## 1. 목표 및 범위

Addressables 비동기 로딩 전용 씬(`Loading.unity`)을 추가한다. 로딩 진행률(%)과 현재 로딩 중인 에셋 설명 텍스트를 HUD에 표시하고, 100% 완료 시 자동으로 `Battle.unity`로 전환한다.

**포함**:
- `Loading.unity` 신규 씬 (Build Settings 0번)
- `LoadingController` MonoBehaviour — 초기화 오케스트레이션 + 씬 전환
- `LoadingHud` MonoBehaviour — 진행 바 + 퍼센트 텍스트 + 설명 텍스트
- `Art/Json/LoadingStrings_Ko.json` — Addressable, 로딩 설명 텍스트
- `Art/Json/Strings_Ko.json` — Addressable, 기존 StreamingAssets/strings_ko.json 이전본
- `StringTableProvider` 리팩터링 — StreamingAssets → TextAsset 파라미터 방식
- `CommonEnum` — `EScene.Loading`, `EData.Strings_Ko`, `EData.LoadingStrings_Ko` 추가

**제외 (MVP 범위 밖)**:
- 다국어 언어 선택 UI
- 최소 표시 시간 / 탭 투 스타트
- 로딩 씬 배경 아트

---

## 2. 씬 전환 순서

```
[앱 시작] → Loading.unity → (PreloadByLabelAsync 100%) → Battle.unity
```

Unity Build Settings: `Loading.unity` = index 0, `Battle.unity` = index 1.

---

## 3. 로딩 시퀀스 (LoadingController.Start)

```
① CHMResource.Instance.Init()
   — Addressables 카탈로그 초기화. 실패 시 LogError 후 중단.

② CHMUI.Instance.Init()
   CHMPool.Instance.Init()

③ CHMResource.Instance.LoadAsync<TextAsset>(EData.LoadingStrings_Ko)
   — 로딩 설명 딕셔너리(key→text) 구축.
   — 실패해도 폴백(__default) 사용, 중단하지 않음.

④ CHMResource.Instance.LoadAsync<TextAsset>(EData.Strings_Ko)
   — StringTableProvider 초기화 → CHText.StringProvider 등록.
   — 실패 시 LogError, StringProvider 미등록 상태로 계속.

⑤ CHMResource.Instance.PreloadByLabelAsync(
       label: null,   // CHMResource.DefaultLabel("Resource")
       onProgress: (ratio, key) =>
           _hud.SetProgress(ratio, desc[key] ?? desc["__default"])
   )

⑥ SceneManager.LoadScene(EScene.Battle.ToString())
```

---

## 4. 데이터 구조

### 4.1 LoadingStrings_Ko.json

`Assets/_Lair/Art/Json/LoadingStrings_Ko.json`  
Addressable 키: `LoadingStrings_Ko` (Rule 03 §2 — 파일명 = Enum값명)

```json
[
  {"key": "Knight",         "text": "기사 프리팹 로딩 중..."},
  {"key": "Wisp",           "text": "도깨비불 몬스터 로딩 중..."},
  {"key": "Wraith",         "text": "망령 몬스터 로딩 중..."},
  {"key": "Reaper",         "text": "사신 몬스터 로딩 중..."},
  {"key": "Hex",            "text": "저주술사 몬스터 로딩 중..."},
  {"key": "Plague",         "text": "역병귀 몬스터 로딩 중..."},
  {"key": "Phantom",        "text": "환령 몬스터 로딩 중..."},
  {"key": "BattleHud",      "text": "전투 UI 로딩 중..."},
  {"key": "CardSelectionPopup", "text": "카드 선택 UI 로딩 중..."},
  {"key": "ResultPopup",    "text": "결과 화면 로딩 중..."},
  {"key": "CardPool_Passive","text": "패시브 카드 풀 로딩 중..."},
  {"key": "CardPool_Active", "text": "액티브 카드 풀 로딩 중..."},
  {"key": "__default",      "text": "리소스 로딩 중..."}
]
```

### 4.2 Strings_Ko.json

`Assets/_Lair/Art/Json/Strings_Ko.json`  
Addressable 키: `Strings_Ko`  
내용: 기존 `StreamingAssets/strings_ko.json` 그대로 이전.

### 4.3 신규 직렬화 클래스

`LoadingController.cs` 내부 private nested class로 정의.

```csharp
[Serializable]
private class LoadingStringEntry { public string key; public string text; }
```

`StringEntry`(int id 기반)와 별개 — 로딩 전용 string→string 매핑.

### 4.4 CommonEnum 변경

```csharp
public enum EScene  { Loading, Battle }   //# Loading 추가 (Build index 0)

public enum EData
{
    CardPool_Passive,
    CardPool_Active,
    Strings_Ko,          //# 신규 — 게임 전체 CHText 문자열
    LoadingStrings_Ko,   //# 신규 — 로딩 설명 텍스트
}
```

---

## 5. LoadingHud

씬에 직접 배치된 Canvas. CHMUI 미사용. Rule 03 §3 — TMP_Text 있는 오브젝트에 CHText 필수.

### 5.1 계층 구조

```
Canvas (Screen Space - Overlay)
└─ Panel (full-screen, 단색 배경 #000000 80% alpha)
   ├─ ProgressBarBg   (Image, 흰색 테두리용 배경)
   │  └─ ProgressFill (Image, fillMethod: Horizontal, fillAmount 0→1)
   ├─ PercentText     (TMP_Text + CHText, _stringID: -1)
   └─ DescText        (TMP_Text + CHText, _stringID: -1)
```

### 5.2 LoadingHud 공개 API

```csharp
public void SetProgress(float ratio, string desc);
//# ratio: 0~1, desc: 로딩 설명 텍스트
//# _progressFill.fillAmount = ratio
//# _percentText.SetText($"{Mathf.RoundToInt(ratio * 100)}%")
//# _descText.SetText(desc)
```

---

## 6. StringTableProvider 변경

```csharp
//# 기존: public void Load(string fileName = "strings_ko.json")
//# → File.ReadAllText(StreamingAssets path) 방식 제거

//# 변경 후
public void Load(TextAsset asset)
{
    StringEntry[] entries = JsonArrayUtility.FromJsonArray<StringEntry>(asset.text);
    foreach (StringEntry e in entries) _table[e.id] = e.text;
}
```

기존 `Load(string fileName)` 오버로드는 완전히 제거. 호출지점(BattleController.Start)도 제거.

---

## 7. BattleController 변경

`Start()` 상단에서 제거:
```csharp
//# 제거 — LoadingController 가 처리
StringTableProvider strTable = new StringTableProvider();
strTable.Load();
CHText.StringProvider = strTable;

if (await CHMResource.Instance.Init() == false) { ... }
CHMUI.Instance.Init();
CHMPool.Instance.Init();
```

`PrewarmPools()`는 유지 — PreloadByLabelAsync는 번들 캐시만 워밍, 메모리 로드는 BattleController가 담당.

---

## 8. 파일 목록 요약

| 경로 | 변경 종류 |
|---|---|
| `Assets/_Lair/Scenes/Loading.unity` | 신규 |
| `Assets/_Lair/Scripts/Battle/LoadingController.cs` | 신규 |
| `Assets/_Lair/Scripts/UI/LoadingHud.cs` | 신규 |
| `Assets/_Lair/Art/Json/LoadingStrings_Ko.json` | 신규 (Addressable) |
| `Assets/_Lair/Art/Json/Strings_Ko.json` | 신규 (기존 strings_ko.json 이전) |
| `Assets/_Lair/Scripts/Data/StringTableProvider.cs` | 수정 |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | 수정 (EScene, EData) |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 수정 (초기화 3줄 제거) |
| `Assets/StreamingAssets/strings_ko.json` | 삭제 |

---

## 9. 테스트 포인트

- `CHMResource.Init()` 실패 시 — LogError 출력, Battle 씬 미전환
- `LoadingStrings_Ko.json` 로드 실패 시 — `__default` 폴백 텍스트로 계속 진행
- `Strings_Ko.json` 로드 실패 시 — CHText StringProvider 미등록, BattleHud 문자열 표시 안 됨 (LogError)
- `PreloadByLabelAsync` 완료 — ratio=1.0f, 씬 전환 확인
- 로딩 중 ratio 0→1 — ProgressFill.fillAmount 연속 갱신, DescText 변경 확인
