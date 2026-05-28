# Loading Scene 구현 계획

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Addressables 비동기 로딩 씬을 추가해 진행률(%) + 설명 텍스트를 표시하고, 완료 시 Battle 씬으로 자동 전환한다.

**Architecture:** `Loading.unity` 씬에서 `LoadingController`(MonoBehaviour)가 CHMResource 초기화 → JSON 로드 → PreloadByLabelAsync 순서로 오케스트레이션. `LoadingHud`(MonoBehaviour)는 씬에 직접 배치된 Canvas 컴포넌트로 진행 바 + 텍스트 갱신만 담당. MVVM 없음 — Rule 02 §6 "가벼운 화면 MVC 허용".

**Tech Stack:** Unity 6 (6000.0.68f1) / C# / Addressables / ChvjPackage (CHMResource, CHMUI, CHMPool, CHText, JsonArrayUtility) / Unity Test Framework (NUnit)

---

## 파일 구조

| 경로 | 종류 | 책임 |
|---|---|---|
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | 수정 | EScene.Loading, EData.Strings_Ko/LoadingStrings_Ko 추가 |
| `Assets/_Lair/Scripts/Data/StringTableProvider.cs` | 수정 | Load(TextAsset) 방식으로 교체, StreamingAssets 제거 |
| `Assets/_Lair/Scripts/Battle/BattleController.cs` | 수정 | Start() 초기화 3블록 제거 |
| `Assets/_Lair/Scripts/UI/LoadingHud.cs` | 신규 | 진행 바 + 퍼센트 + 설명 텍스트 갱신 |
| `Assets/_Lair/Scripts/Battle/LoadingController.cs` | 신규 | 로딩 오케스트레이션 + 씬 전환 |
| `Assets/_Lair/Art/Json/LoadingStrings_Ko.json` | 신규 | Addressable, 에셋 키 → 한국어 설명 매핑 |
| `Assets/_Lair/Art/Json/Strings_Ko.json` | 신규 | Addressable, 기존 strings_ko.json 이전본 |
| `Assets/_Lair/Scenes/Loading.unity` | 신규 | 로딩 씬 (Build index 0) |
| `Assets/_Lair/Tests/EditMode/StringTableProviderTests.cs` | 신규 | TDD — StringTableProvider |
| `Assets/_Lair/Tests/EditMode/LoadingHudTests.cs` | 신규 | TDD — LoadingHud |
| `Assets/StreamingAssets/strings_ko.json` | 삭제 | Strings_Ko.json(Addressable)으로 대체 |

---

### Task 1: CommonEnum — EScene, EData 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs`

- [ ] **Step 1: EScene에 Loading 추가**

`CommonEnum.cs`의 `EScene` 을 다음으로 교체:

```csharp
//# SceneManager.LoadScene(EScene.X.ToString()).
public enum EScene
{
    Loading,   //# Build Settings index 0
    Battle,
}
```

- [ ] **Step 2: EData에 Strings_Ko, LoadingStrings_Ko 추가**

`EData` 를 다음으로 교체:

```csharp
//# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
public enum EData
{
    CardPool_Passive,
    CardPool_Active,    //# B2 신규
    Strings_Ko,         //# 게임 전체 CHText 문자열 — Art/Json/Strings_Ko.json
    LoadingStrings_Ko,  //# 로딩 설명 텍스트 — Art/Json/LoadingStrings_Ko.json
}
```

- [ ] **Step 3: 컴파일 오류 없음 확인**

Unity 콘솔에 컴파일 에러 없는지 확인.

- [ ] **Step 4: 커밋**

```
git add Assets/_Lair/Scripts/Data/CommonEnum.cs
```
커밋 메시지(안):
```
# [feat] - EScene.Loading, EData.Strings_Ko/LoadingStrings_Ko 추가
```

---

### Task 2: StringTableProvider 리팩터링 (TDD)

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/StringTableProvider.cs`
- Create: `Assets/_Lair/Tests/EditMode/StringTableProviderTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/_Lair/Tests/EditMode/StringTableProviderTests.cs` 생성:

```csharp
using NUnit.Framework;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class StringTableProviderTests
    {
        [Test]
        public void Load_유효한_TextAsset_등록된_id로_문자열_반환()
        {
            string json = "[{\"id\":1,\"text\":\"안녕\"},{\"id\":2,\"text\":\"세계\"}]";
            TextAsset asset = new TextAsset(json);
            StringTableProvider provider = new StringTableProvider();

            provider.Load(asset);

            Assert.AreEqual("안녕", provider.GetString(1));
            Assert.AreEqual("세계", provider.GetString(2));
        }

        [Test]
        public void Load_null_asset_예외없이_처리()
        {
            StringTableProvider provider = new StringTableProvider();
            Assert.DoesNotThrow(() => provider.Load(null));
        }

        [Test]
        public void GetString_없는_id_빈문자열_반환()
        {
            StringTableProvider provider = new StringTableProvider();
            Assert.AreEqual(string.Empty, provider.GetString(999));
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

Unity Test Runner (Window > General > Test Runner) > EditMode 탭 > `StringTableProviderTests` 실행.  
`Load_유효한_TextAsset_등록된_id로_문자열_반환` — `Load(TextAsset)` 오버로드 미존재로 컴파일 에러 또는 실패 확인.

- [ ] **Step 3: StringTableProvider.cs 수정**

기존 `Load(string fileName)` 메서드 전체를 삭제하고 `Load(TextAsset asset)` 로 교체. 파일 전체:

```csharp
using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Data
{
    [Serializable]
    public class StringEntry
    {
        public int id;
        public string text;
    }

    //# CHText.StringProvider 에 등록하는 구현체.
    //# Art/Json/Strings_Ko.json (Addressable) 을 JsonArrayUtility 로 파싱해 id → text 테이블 구축.
    public class StringTableProvider : IStringProvider
    {
        private readonly Dictionary<int, string> _table = new();

        public void Load(TextAsset asset)
        {
            if (asset == null)
            {
                Debug.LogError("[StringTableProvider] asset 이 null");
                return;
            }
            StringEntry[] entries = JsonArrayUtility.FromJsonArray<StringEntry>(asset.text);
            foreach (StringEntry entry in entries)
                _table[entry.id] = entry.text;
        }

        public string GetString(int stringID)
        {
            if (_table.TryGetValue(stringID, out string text))
                return text;
            Debug.LogWarning($"[StringTableProvider] ID {stringID} 없음");
            return string.Empty;
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Unity Test Runner > EditMode > `StringTableProviderTests` 전체 3개 PASS 확인.

- [ ] **Step 5: 커밋**

```
git add Assets/_Lair/Scripts/Data/StringTableProvider.cs Assets/_Lair/Tests/EditMode/StringTableProviderTests.cs Assets/_Lair/Tests/EditMode/StringTableProviderTests.cs.meta
```
커밋 메시지(안):
```
# [refactor] - StringTableProvider Load() StreamingAssets → TextAsset 파라미터 방식으로 변경 및 테스트 추가
```

---

### Task 3: BattleController 초기화 코드 제거

**Files:**
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs`

- [ ] **Step 1: Start() 최상단 초기화 3블록 삭제**

`BattleController.Start()` 에서 다음 블록을 삭제한다:

```csharp
//# 0. 스트링 테이블 — CHText.StringProvider 등록 (CHMResource.Init 이전)
StringTableProvider strTable = new StringTableProvider();
strTable.Load();
CHText.StringProvider = strTable;

//# 1. ChvjPackage 초기화
if (await CHMResource.Instance.Init() == false)
{
    Debug.LogError("[BattleController] CHMResource.Init 실패");
    return;
}
CHMUI.Instance.Init();
CHMPool.Instance.Init();
```

삭제 후 `Start()`는 `//# 1.5 풀 사전 워밍` 주석 줄부터 시작된다.

- [ ] **Step 2: 컴파일 오류 없음 확인**

`StringTableProvider` 타입 참조가 BattleController 내 다른 곳에 없는지 확인. Unity 콘솔 에러 없음.

- [ ] **Step 3: 커밋**

```
git add Assets/_Lair/Scripts/Battle/BattleController.cs
```
커밋 메시지(안):
```
# [refactor] - BattleController 초기화 코드 제거 — LoadingController 이전으로 처리
```

---

### Task 4: JSON 데이터 파일 생성 및 Addressables 등록

**Files:**
- Create: `Assets/_Lair/Art/Json/LoadingStrings_Ko.json`
- Create: `Assets/_Lair/Art/Json/Strings_Ko.json`
- Delete: `Assets/StreamingAssets/strings_ko.json`

- [ ] **Step 1: Art/Json 폴더 생성**

Unity Editor > Project 창 > `Assets/_Lair/Art/` 우클릭 > Create > Folder > 이름 `Json`.

- [ ] **Step 2: LoadingStrings_Ko.json 생성**

`Assets/_Lair/Art/Json/LoadingStrings_Ko.json` 생성 (내용):

```json
[
  {"key":"Knight","text":"기사 프리팹 로딩 중..."},
  {"key":"Wisp","text":"도깨비불 몬스터 로딩 중..."},
  {"key":"Wraith","text":"망령 몬스터 로딩 중..."},
  {"key":"Reaper","text":"사신 몬스터 로딩 중..."},
  {"key":"Hex","text":"저주술사 몬스터 로딩 중..."},
  {"key":"Plague","text":"역병귀 몬스터 로딩 중..."},
  {"key":"Phantom","text":"환령 몬스터 로딩 중..."},
  {"key":"BattleHud","text":"전투 UI 로딩 중..."},
  {"key":"CardSelectionPopup","text":"카드 선택 UI 로딩 중..."},
  {"key":"ResultPopup","text":"결과 화면 로딩 중..."},
  {"key":"BuildModalPopup","text":"빌드 모달 UI 로딩 중..."},
  {"key":"SpawnerStatusPanel","text":"스포너 상태 패널 로딩 중..."},
  {"key":"SpawnerStatusTooltip","text":"스포너 툴팁 로딩 중..."},
  {"key":"SpawnerStatusCell","text":"스포너 상태 셀 로딩 중..."},
  {"key":"BuildIconCell","text":"빌드 아이콘 셀 로딩 중..."},
  {"key":"BuildModalCardCell","text":"빌드 카드 셀 로딩 중..."},
  {"key":"BuffLine","text":"버프 라인 UI 로딩 중..."},
  {"key":"HpBar","text":"HP 바 로딩 중..."},
  {"key":"CardPool_Passive","text":"패시브 카드 풀 로딩 중..."},
  {"key":"CardPool_Active","text":"액티브 카드 풀 로딩 중..."},
  {"key":"Strings_Ko","text":"언어 데이터 로딩 중..."},
  {"key":"PoisonAura","text":"독 오라 이펙트 로딩 중..."},
  {"key":"SlowStatus","text":"둔화 상태 이펙트 로딩 중..."},
  {"key":"FearStatus","text":"공포 상태 이펙트 로딩 중..."},
  {"key":"WeakenStatus","text":"약화 상태 이펙트 로딩 중..."},
  {"key":"AttackDownStatus","text":"공격력 감소 이펙트 로딩 중..."},
  {"key":"TimeStopStatus","text":"시간 정지 이펙트 로딩 중..."},
  {"key":"BleedStatus","text":"출혈 이펙트 로딩 중..."},
  {"key":"__default","text":"리소스 로딩 중..."}
]
```

- [ ] **Step 3: Strings_Ko.json 생성**

`Assets/_Lair/Art/Json/Strings_Ko.json` 생성. 내용은 `Assets/StreamingAssets/strings_ko.json` 에서 그대로 복사.

- [ ] **Step 4: Addressables 등록 — LoadingStrings_Ko.json**

Project 창에서 `LoadingStrings_Ko.json` 선택 → Inspector > Addressable 체크박스 ON → 주소를 `LoadingStrings_Ko` 로 수정 → Addressables Groups 창에서 해당 항목의 Labels 에 `Resource` 추가.

Rule 03 §2: 주소 = Enum 값명 = 파일명(확장자 제외).

- [ ] **Step 5: Addressables 등록 — Strings_Ko.json**

`Strings_Ko.json` 선택 → Inspector > Addressable 체크박스 ON → 주소를 `Strings_Ko` 로 수정 → Labels 에 `Resource` 추가.

- [ ] **Step 6: StreamingAssets/strings_ko.json 삭제**

Unity Editor > `Assets/StreamingAssets/strings_ko.json` 우클릭 > Delete.  
`.meta` 파일도 함께 삭제됨 확인.

- [ ] **Step 7: 커밋**

```
git add Assets/_Lair/Art/Json/ Assets/_Lair/Art/Json.meta
git add -u Assets/StreamingAssets/strings_ko.json Assets/StreamingAssets/strings_ko.json.meta
```
커밋 메시지(안):
```
# [asset] - Art/Json 폴더 신설, Strings_Ko·LoadingStrings_Ko Addressable 등록, StreamingAssets strings_ko.json 제거
```

---

### Task 5: LoadingHud (TDD)

**Files:**
- Create: `Assets/_Lair/Scripts/UI/LoadingHud.cs`
- Create: `Assets/_Lair/Tests/EditMode/LoadingHudTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

`Assets/_Lair/Tests/EditMode/LoadingHudTests.cs` 생성:

```csharp
using System.Reflection;
using NUnit.Framework;
using Lair.UI;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    public class LoadingHudTests
    {
        private GameObject _hudGo;
        private LoadingHud _hud;
        private Image _fill;

        [SetUp]
        public void SetUp()
        {
            _hudGo = new GameObject("TestHud");
            _hud = _hudGo.AddComponent<LoadingHud>();
            _fill = new GameObject("Fill").AddComponent<Image>();
            typeof(LoadingHud)
                .GetField("_progressFill", BindingFlags.NonPublic | BindingFlags.Instance)
                ?.SetValue(_hud, _fill);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_fill.gameObject);
            Object.DestroyImmediate(_hudGo);
        }

        [Test]
        public void SetProgress_ratio_0점5_fillAmount_0점5()
        {
            _hud.SetProgress(0.5f, "테스트");

            Assert.AreEqual(0.5f, _fill.fillAmount, 0.001f);
        }

        [Test]
        public void SetProgress_ratio_1_fillAmount_1()
        {
            _hud.SetProgress(1f, "완료");

            Assert.AreEqual(1f, _fill.fillAmount, 0.001f);
        }

        [Test]
        public void SetProgress_null필드_예외없이_처리()
        {
            //# SerializeField 미연결 상태 — 씬 배치 전 방어 검증
            LoadingHud emptyHud = new GameObject("Empty").AddComponent<LoadingHud>();

            Assert.DoesNotThrow(() => emptyHud.SetProgress(0.5f, "테스트"));

            Object.DestroyImmediate(emptyHud.gameObject);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

Unity Test Runner > EditMode > `LoadingHudTests` 실행 — `LoadingHud` 미정의로 컴파일 에러 또는 실패 확인.

- [ ] **Step 3: LoadingHud.cs 구현**

`Assets/_Lair/Scripts/UI/LoadingHud.cs` 생성:

```csharp
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    public class LoadingHud : MonoBehaviour
    {
        [SerializeField] private Image _progressFill;
        [SerializeField] private CHText _percentText;
        [SerializeField] private CHText _descText;

        public void SetProgress(float ratio, string desc)
        {
            if (_progressFill != null)
                _progressFill.fillAmount = ratio;
            if (_percentText != null)
                _percentText.SetText($"{Mathf.RoundToInt(ratio * 100)}%");
            if (_descText != null)
                _descText.SetText(desc);
        }
    }
}
```

- [ ] **Step 4: 테스트 통과 확인**

Unity Test Runner > EditMode > `LoadingHudTests` 전체 3개 PASS 확인.

- [ ] **Step 5: 커밋**

```
git add Assets/_Lair/Scripts/UI/LoadingHud.cs Assets/_Lair/Scripts/UI/LoadingHud.cs.meta Assets/_Lair/Tests/EditMode/LoadingHudTests.cs Assets/_Lair/Tests/EditMode/LoadingHudTests.cs.meta
```
커밋 메시지(안):
```
# [feat] - LoadingHud 구현 및 EditMode 테스트 추가
```

---

### Task 6: LoadingController 구현

**Files:**
- Create: `Assets/_Lair/Scripts/Battle/LoadingController.cs`

LoadingController는 async MonoBehaviour 오케스트레이터로 단위 테스트가 어렵다. 핵심 로직(StringTableProvider, LoadingHud)은 Task 2·5에서 TDD 완료. 통합 검증은 Task 7 씬 실행으로 한다.

- [ ] **Step 1: LoadingController.cs 생성**

`Assets/_Lair/Scripts/Battle/LoadingController.cs` 생성:

```csharp
using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Lair.Battle
{
    public class LoadingController : MonoBehaviour
    {
        [SerializeField] private LoadingHud _hud;

        [Serializable]
        private class LoadingStringEntry { public string key; public string text; }

        async void Start()
        {
            //# 1. Addressables 카탈로그 초기화
            bool ok = await CHMResource.Instance.Init();
            if (ok == false)
            {
                Debug.LogError("[LoadingController] CHMResource.Init 실패");
                return;
            }

            //# 2. 패키지 초기화
            CHMUI.Instance.Init();
            CHMPool.Instance.Init();

            //# 3. 로딩 설명 JSON 로드
            Dictionary<string, string> descs = new Dictionary<string, string>();
            TextAsset loadingJson = await CHMResource.Instance.LoadAsync<TextAsset>(EData.LoadingStrings_Ko);
            if (loadingJson != null)
            {
                LoadingStringEntry[] entries = JsonArrayUtility.FromJsonArray<LoadingStringEntry>(loadingJson.text);
                foreach (LoadingStringEntry e in entries)
                    descs[e.key] = e.text;
            }
            else
            {
                Debug.LogWarning("[LoadingController] LoadingStrings_Ko 로드 실패 — 폴백 텍스트 사용");
            }

            descs.TryGetValue("__default", out string defaultDesc);
            if (string.IsNullOrEmpty(defaultDesc)) defaultDesc = "로딩 중...";

            //# 4. 게임 문자열 JSON 로드 → CHText.StringProvider 등록
            TextAsset stringsJson = await CHMResource.Instance.LoadAsync<TextAsset>(EData.Strings_Ko);
            if (stringsJson != null)
            {
                StringTableProvider strTable = new StringTableProvider();
                strTable.Load(stringsJson);
                CHText.StringProvider = strTable;
            }
            else
            {
                Debug.LogError("[LoadingController] Strings_Ko 로드 실패 — CHText 문자열 미등록");
            }

            //# 5. Addressables 번들 워밍 + 진행률 표시
            await CHMResource.Instance.PreloadByLabelAsync(null, (ratio, key) =>
            {
                string desc = descs.TryGetValue(key, out string text) ? text : defaultDesc;
                _hud.SetProgress(ratio, desc);
            });

            //# 6. Battle 씬 자동 전환
            SceneManager.LoadScene(EScene.Battle.ToString());
        }
    }
}
```

- [ ] **Step 2: 컴파일 오류 없음 확인**

Unity 콘솔에 에러 없는지 확인. `EData.LoadingStrings_Ko`, `EData.Strings_Ko`, `EScene.Battle` 모두 Task 1에서 추가됨.

- [ ] **Step 3: 커밋**

```
git add Assets/_Lair/Scripts/Battle/LoadingController.cs Assets/_Lair/Scripts/Battle/LoadingController.cs.meta
```
커밋 메시지(안):
```
# [feat] - LoadingController 구현 — Init·JSON 로드·PreloadByLabelAsync·씬 전환 오케스트레이션
```

---

### Task 7: Loading.unity 씬 설정 (Unity Editor)

**Files:**
- Create: `Assets/_Lair/Scenes/Loading.unity`

이 Task는 Unity Editor에서 직접 수행하는 작업이다.

- [ ] **Step 1: Loading 씬 생성**

File > New Scene > Empty → File > Save As → `Assets/_Lair/Scenes/Loading.unity`.

- [ ] **Step 2: Build Settings 등록**

File > Build Settings > Add Open Scenes.  
목록에서 `Loading.unity`를 index 0, `Battle.unity`를 index 1로 순서 조정 (드래그).

- [ ] **Step 3: LoadingController GameObject 추가**

Hierarchy 빈 곳 우클릭 > Create Empty → 이름 `LoadingController`.  
`LoadingController` 스크립트 컴포넌트 추가.

- [ ] **Step 4: Canvas 계층 구축**

Hierarchy 우클릭 > UI > Canvas → 이름 `Canvas`.  
Canvas 설정: Render Mode = Screen Space - Overlay.

```
Canvas
└─ Panel          (UI > Panel, Color: 0,0,0,200)
   ├─ ProgressBarBg   (UI > Image, W=600 H=30, Color: 흰색)
   │  └─ ProgressFill (UI > Image, Image Type=Filled,
   │                   Fill Method=Horizontal, Fill Origin=Left,
   │                   Fill Amount=0, Color: 하늘색 또는 흰색)
   ├─ PercentText  (UI > Text - TextMeshPro, 이름 PercentText)
   └─ DescText     (UI > Text - TextMeshPro, 이름 DescText)
```

`PercentText`, `DescText` 각각에 `CHText` 컴포넌트 추가 → `_stringID = -1` 확인 (Rule 03 §3).

- [ ] **Step 5: LoadingHud GameObject 추가 및 연결**

Hierarchy 우클릭 > Create Empty → 이름 `LoadingHud`.  
`LoadingHud` 스크립트 컴포넌트 추가.  
Inspector에서 필드 연결:
- `_progressFill` ← `ProgressFill` (Image 컴포넌트)
- `_percentText` ← `PercentText` (CHText 컴포넌트)
- `_descText` ← `DescText` (CHText 컴포넌트)

- [ ] **Step 6: LoadingController에 LoadingHud 연결**

`LoadingController` GameObject 선택 → Inspector `_hud` 필드에 `LoadingHud` GameObject 드래그.

- [ ] **Step 7: 씬 저장**

Ctrl+S.

- [ ] **Step 8: 동작 검증**

Play Mode 실행 (Loading.unity 열린 상태):
- 콘솔에 `CHMResource.Init 실패` 에러 없음
- `ProgressFill.fillAmount` 가 0 → 1 로 채워짐
- `DescText` 에 에셋별 설명 텍스트 표시됨 (예: "기사 프리팹 로딩 중...")
- `PercentText` 에 `0% → 100%` 표시됨
- 완료 후 Battle.unity 로 자동 전환됨

- [ ] **Step 9: 커밋**

```
git add Assets/_Lair/Scenes/Loading.unity Assets/_Lair/Scenes/Loading.unity.meta
```
커밋 메시지(안):
```
# [asset] - Loading.unity 씬 생성 및 Build Settings index 0 등록, LoadingController·LoadingHud 연결
```
