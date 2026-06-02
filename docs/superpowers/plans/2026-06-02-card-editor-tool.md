# Card Editor Tool Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `Lair > Card Editor` 커스텀 EditorWindow — 한 화면에서 카드 조회·추가(enum codegen 포함)·삭제·전 필드 편집·풀 소속 관리.

**Architecture:** `ECardId` enum을 툴이 관리하는 전용 파일로 분리하고, 전용 에디터 asmdef(`Lair.Editor.CardTool`) 아래에 pure codegen 유틸 + EditorWindow를 둔다. 편집 패인은 `SerializedObject` + `PropertyField`로 Unity 네이티브 드로어를 임베드해 Sprite·Effect 폴리모픽 편집을 공짜로 얻는다. 런타임 스키마(`CardData`/`CardPool`) 변경 없음.

**Tech Stack:** Unity 6 / C# / UnityEditor IMGUI / NUnit EditMode / 기존 `CardData`·`CardPool`·`AssetDatabase`.

**관련 문서:** spec `docs/superpowers/specs/2026-06-02-card-editor-tool-design.md`

---

## File Structure

| 파일 | 책임 | 비고 |
|---|---|---|
| `Assets/_Lair/Scripts/Data/ECardId.cs` (Create) | `ECardId` enum 단독 정의 + 툴 삽입 마커 | 런타임 `Lair` asmdef. `namespace Lair.Data` 유지 |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` (Modify) | `ECardId` 정의 제거 (다른 enum 잔류) | 라인 55~95 삭제 |
| `Assets/_Lair/Editor/CardTool/Lair.Editor.CardTool.asmdef` (Create) | 카드 툴 전용 에디터 어셈블리 | `references: ["Lair"]`, Editor 전용 |
| `Assets/_Lair/Editor/CardTool/CardEnumCodegen.cs` (Create) | `ECardId.cs` 텍스트에 신규 ID append (pure) + 파일 IO 래퍼 | 단위 테스트 대상 |
| `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (Create) | 윈도우 셸 — 목록·편집·CRUD·풀 토글·안내 | IMGUI, 수동 검증 |
| `Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef` (Modify) | `"Lair.Editor.CardTool"` 참조 추가 | |
| `Assets/_Lair/Tests/EditMode/CardTool/CardEnumCodegenTests.cs` (Create) | codegen pure 함수 단위 테스트 | NUnit |

**asmdef 주의:** top-level `Assets/_Lair/Editor/*.cs`(LairBalanceWindow 등)는 `Assembly-CSharp-Editor` 소속이라 EditMode 테스트가 참조 불가. 그래서 신규 툴은 JsonSync처럼 **자체 asmdef를 가진 서브폴더**(`Editor/CardTool/`)에 둔다.

**검증 환경 주의:** Unity 컴파일/테스트는 UnityMCP로 트리거한다 — `editor_focus` → `editor_recompile` → `editor_read_log`(에러 0 확인) → EditMode 테스트는 Unity Test Runner. 자동화 한계는 사용자에게 수동 실행을 요청.

---

## Task 1: ECardId 전용 파일로 분리

**Files:**
- Create: `Assets/_Lair/Scripts/Data/ECardId.cs`
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs:55-95` (ECardId 블록 삭제)

이 작업은 순수 리팩터링(이동) — 동작/직렬화 무변경이 검증 기준. 단위 테스트 없음, 컴파일 + 기존 테스트 통과로 검증.

- [ ] **Step 1: `ECardId.cs` 생성 — CommonEnum.cs의 ECardId 블록을 그대로 옮기고 삽입 마커 추가**

```csharp
namespace Lair.Data
{
    //# 카드 식별자 — 카드 리뉴얼 v0.6 (2026-05-31) — 28장 (패시브 16 + 액티브 12).
    //# 종(種) 이름이 들어간 카드 ID 는 LittleGhost 테마로 동기화 (Wisp/Wraith/Reaper/Hex/Plague/Phantom).
    //# Rule 02 §8 의도적 예외 — 이 enum 은 Lair > Card Editor 툴이 codegen 으로 관리하므로 CommonEnum.cs 에서 분리한다.
    //# 신규 값 추가는 툴의 [Enum 추가] 사용 권장. 순서/정수값 변경 금지 (CardData._id int 직렬화 정합).
    public enum ECardId
    {
        //# 패시브 15장 (값 0~14 보존 — v0.6 에서 일부는 축 이동 + 효과 리뉴얼)
        WispHpBoost,                   //# 구 SlimeHpBoost (0) — Tank P
        WraithDamageBoost,             //# 구 GolemDamageBoost (1) — Tank P (v0.6 효과 HP 로 리뉴얼)
        ReaperAtkSpeed,                //# 구 OrcAtkSpeed (2) — Dps P
        HexRangeBoost,                 //# 구 ArcherRangeBoost (3) — Dps P
        PlagueSlowBoost,               //# 구 SpiderSlowBoost (4) — Debuff P
        PhantomMoveSpeedBoost,         //# 구 BatMoveSpeedBoost (5) — Swarm P
        SpawnWisps,                    //# 구 SpawnSlimes (6) — Swarm P (v0.6 Tank→Swarm 축 이동)
        SpawnWraith,                   //# 구 SpawnGolem (7) — Tank P
        SpawnReapers,                  //# 구 SpawnOrcs (8) — Dps P
        SpawnPlagues,                  //# 구 SpawnSpiders (9) — Debuff P
        SpawnPhantoms,                 //# 구 SpawnBats (10) — Swarm P
        ReplaceWispsToWraith,          //# 구 ReplaceSlimesToGolem (11) — Tank P
        ReplaceReapersToHex,           //# 구 ReplaceOrcsToArchers (12) — Dps P
        HeroPoisonAura,                //# (13) — Debuff P
        HeroAttackDown,                //# (14) — Debuff P

        //# 액티브 10장 (값 15~24 보존)
        Fear,                          //# (15) — Debuff A
        Bleed,                         //# (16) — Debuff A
        Weaken,                        //# (17) — Debuff A
        Slow,                          //# (18) — Swarm A (v0.6 Debuff→Swarm 축 이동 + 효과 리뉴얼)
        Frenzy,                        //# (19) — Dps A
        //# 폐기 (카드 리뉴얼 v0.6 — SO/풀 ref 제거, enum 자리만 보존. 실제 효과는 FastBreedingEffect/"빠른 번식")
        Multiply,                      //# (20) — Swarm A (실제 SO: Multiply.asset / FastBreedingEffect, 팬텀 스포너 주기 ×0.6)
        BloodThirst,                   //# (21) — Dps A (v0.6 Swarm→Dps 축 이동)
        IronWill,                      //# (22) — Tank A
        TimeStop,                      //# (23) — Swarm A
        //# GuardianRage (구 Berserk 자리 — 카드 리뉴얼 v0.6 으로 효과·displayName 교체, enum 값명만 보존)
        Berserk,                       //# (24) — Tank A (효과 클래스 = GuardianRageEffect)

        //# 카드 리뉴얼 v0.6 신규 3장 (값 25~27 — int 직렬화 정합).
        WallOfWisps,                   //# (25) — Tank A
        MarkOfDeath,                   //# (26) — Dps A
        SpawnerHaste,                  //# (27) — Swarm P
        //# <card-editor:insert> — [Enum 추가] 가 신규 ID 를 이 줄 바로 위에 삽입한다. 이 줄을 삭제하지 말 것.
    }
}
```

- [ ] **Step 2: `CommonEnum.cs`에서 ECardId 블록 삭제**

`CommonEnum.cs`의 라인 55~95 (`//# 카드 식별자 …` 주석부터 ECardId 닫는 `}` 까지) 전체를 삭제한다. 앞(EBuildAxis)과 뒤(EScene) 사이의 빈 줄 1개만 남긴다. 다른 enum은 건드리지 않는다.

- [ ] **Step 3: 컴파일 검증**

UnityMCP: `editor_focus` → `editor_recompile` → `editor_read_log`.
Expected: 컴파일 에러 0. `ECardId` 미정의/중복정의 에러 없음. (`LairBalanceWindow`, `CardDataSyncer`, `EffectConverter` 등 기존 참조가 `Lair.Data.ECardId`로 그대로 해소.)

- [ ] **Step 4: 기존 EditMode 테스트 회귀 확인**

Unity Test Runner로 기존 EditMode 스위트 실행 (특히 `CardDataSyncerTests`, `CardEffectSignatureRegressionTests`).
Expected: 이전과 동일하게 전부 통과 (ECardId 값/순서 무변경이므로 회귀 없음).

- [ ] **Step 5: 커밋**

```bash
git add Assets/_Lair/Scripts/Data/ECardId.cs Assets/_Lair/Scripts/Data/ECardId.cs.meta Assets/_Lair/Scripts/Data/CommonEnum.cs
# 커밋 메시지(안): # [refactor] - ECardId 를 전용 파일로 분리 (카드 에디터 툴 codegen 관리용)
```

---

## Task 2: CardTool asmdef + codegen pure 함수 (TDD)

**Files:**
- Create: `Assets/_Lair/Editor/CardTool/Lair.Editor.CardTool.asmdef`
- Create: `Assets/_Lair/Editor/CardTool/CardEnumCodegen.cs`
- Modify: `Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef` (references에 `"Lair.Editor.CardTool"` 추가)
- Test: `Assets/_Lair/Tests/EditMode/CardTool/CardEnumCodegenTests.cs`

- [ ] **Step 1: CardTool asmdef 생성** (JsonSync 미러링)

```json
{
  "name": "Lair.Editor.CardTool",
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

- [ ] **Step 2: 실패 테스트 작성** — `CardEnumCodegenTests.cs`

```csharp
using NUnit.Framework;
using Lair.EditorTools;

namespace Lair.Tests
{
    public class CardEnumCodegenTests
    {
        private const string Sample =
            "namespace Lair.Data\n" +
            "{\n" +
            "    public enum ECardId\n" +
            "    {\n" +
            "        WispHpBoost,\n" +
            "        SpawnerHaste,\n" +
            "        //# <card-editor:insert>\n" +
            "    }\n" +
            "}\n";

        //# 신규 ID 가 마커 줄 바로 위에 삽입된다.
        [Test]
        public void InsertCardId_마커_위에_삽입()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "NewCard");
            int insertIdx = result.IndexOf("NewCard,");
            int markerIdx = result.IndexOf("//# <card-editor:insert>");
            Assert.Greater(insertIdx, 0);
            Assert.Less(insertIdx, markerIdx, "신규 ID 는 마커 줄 위에 와야 한다");
        }

        //# 기존 값은 그대로 보존된다 (인덱스 시프트 없음).
        [Test]
        public void InsertCardId_기존값_보존()
        {
            string result = CardEnumCodegen.InsertCardId(Sample, "NewCard");
            Assert.Less(result.IndexOf("WispHpBoost,"), result.IndexOf("SpawnerHaste,"));
            Assert.Less(result.IndexOf("SpawnerHaste,"), result.IndexOf("NewCard,"));
        }

        //# 중복 ID 는 예외.
        [Test]
        public void InsertCardId_중복_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, "WispHpBoost"));
        }

        //# 유효하지 않은 C# 식별자는 예외.
        [Test]
        public void InsertCardId_잘못된_식별자_예외()
        {
            Assert.Throws<System.ArgumentException>(
                () => CardEnumCodegen.InsertCardId(Sample, "1Bad Name"));
        }

        //# 마커가 없으면 예외 (파일 구조 손상 방지).
        [Test]
        public void InsertCardId_마커없음_예외()
        {
            string noMarker = "public enum ECardId { WispHpBoost, }";
            Assert.Throws<System.InvalidOperationException>(
                () => CardEnumCodegen.InsertCardId(noMarker, "NewCard"));
        }
    }
}
```

- [ ] **Step 3: EditMode asmdef에 참조 추가**

`Lair.Tests.EditMode.asmdef`의 `references` 배열에 `"Lair.Editor.CardTool"`를 추가:
```json
    "references": [
        "Lair",
        "com.chvj.unityinfra",
        "Lair.Editor.JsonSync",
        "Lair.Editor.CardTool",
        "UnityEngine.TestRunner",
        "UnityEditor.TestRunner"
    ],
```

- [ ] **Step 4: 테스트 실패 확인**

Unity Test Runner EditMode 실행.
Expected: `CardEnumCodegenTests` 5개 전부 FAIL/컴파일에러 — `CardEnumCodegen` 미정의.

- [ ] **Step 5: `CardEnumCodegen.cs` 최소 구현 — pure 함수**

```csharp
using System;
using System.Text.RegularExpressions;

namespace Lair.EditorTools
{
    //# ECardId.cs 텍스트에 신규 카드 ID 를 append 하는 codegen. pure 함수는 단위 테스트 대상.
    public static class CardEnumCodegen
    {
        public const string Marker = "//# <card-editor:insert>";
        private static readonly Regex IdentifierRx = new Regex(@"^[A-Za-z_][A-Za-z0-9_]*$");

        //# fileText 의 마커 줄 바로 위에 "        {newId}," 한 줄을 삽입한 새 텍스트를 반환한다.
        //# 잘못된 식별자/중복/마커없음이면 예외 — 파일을 절대 깨지 않는다.
        public static string InsertCardId(string fileText, string newId)
        {
            if (string.IsNullOrEmpty(newId) || IdentifierRx.IsMatch(newId) == false)
                throw new ArgumentException($"유효한 C# 식별자가 아님: '{newId}'");

            if (ContainsMember(fileText, newId))
                throw new ArgumentException($"이미 존재하는 ECardId: '{newId}'");

            int markerIdx = fileText.IndexOf(Marker, StringComparison.Ordinal);
            if (markerIdx < 0)
                throw new InvalidOperationException($"삽입 마커({Marker})를 찾지 못함 — ECardId.cs 구조 확인 필요");

            //# 마커가 있는 줄의 시작 위치
            int lineStart = fileText.LastIndexOf('\n', markerIdx) + 1;
            string indent = fileText.Substring(lineStart, markerIdx - lineStart);
            string insertion = $"{indent}{newId},\n";
            return fileText.Insert(lineStart, insertion);
        }

        //# enum 멤버로 이미 선언된 식별자인지 (주석/타 토큰 무시, 멤버 라인 패턴 매칭).
        private static bool ContainsMember(string fileText, string id)
        {
            Regex memberRx = new Regex($@"(?m)^\s*{Regex.Escape(id)}\s*(,|=|$)");
            return memberRx.IsMatch(fileText);
        }
    }
}
```

- [ ] **Step 6: 테스트 통과 확인**

Unity Test Runner EditMode 실행.
Expected: `CardEnumCodegenTests` 5개 전부 PASS.

- [ ] **Step 7: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/ Assets/_Lair/Tests/EditMode/CardTool/ Assets/_Lair/Tests/EditMode/Lair.Tests.EditMode.asmdef
# 커밋 메시지(안): # [test] - CardEnumCodegen.InsertCardId pure 함수 + asmdef (TDD)
```

---

## Task 3: codegen 파일 IO 래퍼 + 신규 ID 영속

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/CardEnumCodegen.cs`

ECardId.cs 실파일을 읽어 `InsertCardId` 적용 후 다시 쓰고 `AssetDatabase.Refresh`. 파일 IO/리로드라 단위 테스트 비대상 — 수동 검증.

- [ ] **Step 1: `AppendCardId` 래퍼 추가**

```csharp
//# CardEnumCodegen.cs 상단 using 에 추가:
//   using System.IO;
//   using UnityEditor;
//   using UnityEngine;

//# 클래스 내부에 추가:
public const string EnumFilePath = "Assets/_Lair/Scripts/Data/ECardId.cs";

//# 실제 ECardId.cs 에 신규 ID 를 append 하고 컴파일을 트리거한다.
//# 성공 시 true. 실패(예외) 시 파일 미변경 + false, 에러 다이얼로그.
public static bool AppendCardId(string newId)
{
    try
    {
        string text = File.ReadAllText(EnumFilePath, System.Text.Encoding.UTF8);
        string updated = InsertCardId(text, newId);
        File.WriteAllText(EnumFilePath, updated, new System.Text.UTF8Encoding(false));
        AssetDatabase.ImportAsset(EnumFilePath, ImportAssetOptions.ForceUpdate);
        AssetDatabase.Refresh();
        Debug.Log($"[CardEnumCodegen] ECardId 추가: {newId} — 재컴파일 대기");
        return true;
    }
    catch (Exception e)
    {
        EditorUtility.DisplayDialog("Enum 추가 실패", e.Message, "확인");
        return false;
    }
}
```

- [ ] **Step 2: 컴파일 검증**

UnityMCP: `editor_recompile` → `editor_read_log`. Expected: 에러 0.

- [ ] **Step 3: 수동 통합 검증 (임시)**

임시로 메뉴 항목이나 테스트 코드에서 `CardEnumCodegen.AppendCardId("TmpProbe")` 호출 → `ECardId.cs`에 `TmpProbe,`가 마커 위에 추가되고 컴파일 통과 확인 → 확인 후 해당 줄/임시 호출 제거. (Task 6에서 정식 UI 연결.)
Expected: 파일에 한 줄 추가, 인덱스 시프트 없음, 컴파일 정상.

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/CardEnumCodegen.cs
# 커밋 메시지(안): # [feat] - CardEnumCodegen.AppendCardId — ECardId.cs 파일 append + 재컴파일
```

---

## Task 4: LairCardEditorWindow — 목록 패인

**Files:**
- Create: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs`

IMGUI EditorWindow — 수동 검증. ECardId 전 값 순회, asset 존재/미생성 판별, 풀 소속 뱃지.

- [ ] **Step 1: 윈도우 골격 + 목록 렌더 구현**

```csharp
using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 카드 조회/추가/삭제/편집 통합 윈도우. Rule 11 예외 — 에디터 전용 UI.
    public class LairCardEditorWindow : EditorWindow
    {
        private const string CardDir = "Assets/_Lair/Art/Cards/Items";

        private Vector2 _listScroll;
        private Vector2 _editScroll;
        private ECardId _selected;
        private bool _hasSelection;
        private string _search = "";
        private string _newCardId = "";

        [MenuItem("Lair/Card Editor")]
        public static void Open() => GetWindow<LairCardEditorWindow>("Card Editor");

        private static string AssetPath(ECardId id) => $"{CardDir}/{id}.asset";
        private static CardData Load(ECardId id) =>
            AssetDatabase.LoadAssetAtPath<CardData>(AssetPath(id));

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "ECardId enum 에 값을 추가하면 이 툴에 (미생성) 슬롯으로 나타납니다. " +
                "[Enum 추가] 로 새 카드 종류를 만들 수 있습니다.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPane();
                DrawEditPane();
            }
        }

        private void DrawListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                _search = EditorGUILayout.TextField("검색", _search);
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                foreach (ECardId id in System.Enum.GetValues(typeof(ECardId)))
                {
                    string name = id.ToString();
                    if (string.IsNullOrEmpty(_search) == false &&
                        name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    DrawListRow(id, name);
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(6);
                DrawAddEnumRow();
            }
        }

        private void DrawListRow(ECardId id, string name)
        {
            CardData card = Load(id);
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (card == null)
                {
                    EditorGUILayout.LabelField($"{name} (미생성)", EditorStyles.miniLabel);
                    if (GUILayout.Button("생성하기", GUILayout.Width(70)))
                        CreateCard(id);
                }
                else
                {
                    if (GUILayout.Button($"{name}  [{card.Axis}]", EditorStyles.label))
                    {
                        _selected = id;
                        _hasSelection = true;
                    }
                }
            }
        }

        //# Task 6 에서 구현 — 임시 stub.
        private void DrawAddEnumRow() { }
        private void CreateCard(ECardId id) { }
        private void DrawEditPane() { }
    }
}
```

- [ ] **Step 2: 컴파일 + 수동 검증**

UnityMCP `editor_recompile` → 에러 0. `Lair > Card Editor` 메뉴로 윈도우를 열고:
Expected: 좌측에 28장이 `이름 [Axis]` 버튼으로 보임. 검색 입력 시 필터됨. (편집 패인은 다음 태스크에서 채움.)

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs.meta
# 커밋 메시지(안): # [feat] - 카드 에디터 윈도우 목록 패인 (조회 + 검색)
```

---

## Task 5: 편집 패인 — 네이티브 인스펙터 임베드

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (`DrawEditPane` 구현)

- [ ] **Step 0: (조기 리스크 검증 — 구현 전 먼저 확인) `[SerializeReference]` 타입 피커가 임베드 IMGUI에서 null effect에 렌더되는가**

이 플로의 핵심 가정: `EditorGUILayout.PropertyField(_effect, true)`가 **타입 선택 드롭다운**까지 그려서, `_effect == null`인 신규 카드에도 Effect 타입을 고를 수 있어야 한다. 임시로 새 CardData를 만들어(혹은 effect가 null인 상태로) 편집 패인에서 `_effect` 필드에 타입 드롭다운이 보이는지 확인.
- **보이면**: 기존 Step 1 그대로 진행.
- **안 보이면 (fallback)**: `_effect` 위에 수동 타입 드롭다운을 둔다 — 리플렉션으로 `ICardEffect` 구현체(`Lair.Card` 어셈블리)를 모아 `EditorGUILayout.Popup`으로 선택 시 `sp.managedReferenceValue = System.Activator.CreateInstance(type)` 설정 후 `PropertyField`로 파라미터 렌더. 이 fallback 코드 블록은 검증 결과 필요 시 Step 1에 병합.

신규 카드(null effect)로 검증할 것 — 기존 28장은 이미 effect가 있어 버그가 가려진다.

- [ ] **Step 1: `DrawEditPane` 구현 — SerializedObject + PropertyField**

```csharp
//# DrawEditPane stub 를 아래로 교체:
private SerializedObject _editSo;
private ECardId _editSoId;

private void DrawEditPane()
{
    using (new EditorGUILayout.VerticalScope())
    {
        if (_hasSelection == false)
        {
            EditorGUILayout.HelpBox("좌측에서 카드를 선택하세요.", MessageType.None);
            return;
        }

        CardData card = Load(_selected);
        if (card == null)
        {
            EditorGUILayout.HelpBox($"{_selected} 는 미생성 상태입니다. 좌측 [생성하기] 후 편집하세요.", MessageType.Warning);
            return;
        }

        //# 선택 변경 시에만 SerializedObject 재생성
        if (_editSo == null || _editSo.targetObject != card || _editSoId != _selected)
        {
            _editSo = new SerializedObject(card);
            _editSoId = _selected;
        }
        _editSo.Update();

        EditorGUILayout.HelpBox(
            "Icon/CardImage 는 Build UI Prefabs(LairCardPrefabBuilder) 재실행 시 " +
            "ECardId 이름 PNG 컨벤션으로 덮어쓰일 수 있습니다.", MessageType.Warning);

        _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
        //# _id 는 정체성(파일명=ECardId) 고정값 — 편집 불가 read-only. 변경 시 loader/list/CardDataSyncer 가 desync 됨.
        EditorGUILayout.LabelField("Id", _selected.ToString());
        foreach (string prop in new[]
            { "_axis", "_displayName", "_description", "_icon", "_cardImage", "_effect" })
        {
            SerializedProperty sp = _editSo.FindProperty(prop);
            if (sp != null)
                EditorGUILayout.PropertyField(sp, true);
        }
        EditorGUILayout.EndScrollView();

        if (_editSo.ApplyModifiedProperties())
            EditorUtility.SetDirty(card);

        EditorGUILayout.Space(8);
        DrawPoolToggles(card);   //# Task 7
        EditorGUILayout.Space(8);
        DrawDeleteButton(card);  //# Task 8
    }
}

//# Task 7·8 에서 구현 — 임시 stub.
private void DrawPoolToggles(CardData card) { }
private void DrawDeleteButton(CardData card) { }
```

- [ ] **Step 2: 컴파일 + 수동 검증**

`editor_recompile` → 에러 0. 윈도우에서 카드 선택 →
Expected: 우측에 Id·Axis·DisplayName·Description(TextArea)·Icon/CardImage(Sprite 피커)·Effect(타입 드롭다운+파라미터) 표시. 값 수정 시 해당 `.asset`이 dirty 표시되고 저장(Ctrl+S) 후 인스펙터에 반영.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs
# 커밋 메시지(안): # [feat] - 카드 편집 패인 (네이티브 SerializedObject 임베드 — 전 필드 편집)
```

---

## Task 6: 카드 생성 + Enum 추가 UI

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (`CreateCard`, `DrawAddEnumRow` 구현)

- [ ] **Step 1: `CreateCard` + `DrawAddEnumRow` 구현**

```csharp
//# CreateCard stub 교체 — 미생성 enum 슬롯에 대해 .asset 생성.
private void CreateCard(ECardId id)
{
    if (Directory.Exists(CardDir) == false)
        Directory.CreateDirectory(CardDir);

    CardData card = ScriptableObject.CreateInstance<CardData>();
    SerializedObject so = new SerializedObject(card);
    so.FindProperty("_id").enumValueIndex = (int)id;
    so.FindProperty("_displayName").stringValue = id.ToString();
    so.ApplyModifiedPropertiesWithoutUndo();

    AssetDatabase.CreateAsset(card, AssetPath(id));
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    _selected = id;
    _hasSelection = true;
    Debug.Log($"[CardEditor] 생성: {id}");
}

//# DrawAddEnumRow stub 교체 — 신규 카드 종류(enum 값) 추가.
private void DrawAddEnumRow()
{
    EditorGUILayout.LabelField("새 카드 종류 (Enum)", EditorStyles.boldLabel);
    using (new EditorGUILayout.HorizontalScope())
    {
        _newCardId = EditorGUILayout.TextField(_newCardId);
        if (GUILayout.Button("Enum 추가", GUILayout.Width(90)))
        {
            string id = _newCardId.Trim();
            if (CardEnumCodegen.AppendCardId(id))
            {
                _newCardId = "";
                //# 재컴파일 후 (미생성) 슬롯으로 등장 — 그때 [생성하기] 로 .asset 생성.
                Debug.Log($"[CardEditor] '{id}' enum 추가 요청 — 재컴파일 후 목록에 (미생성) 으로 표시됩니다.");
            }
            GUIUtility.ExitGUI();
        }
    }
}
```

- [ ] **Step 2: 컴파일 + 수동 검증 — 미생성 슬롯 생성**

기존 enum 중 .asset이 없는 슬롯이 있으면(없으면 Step 3에서 새로 만든 뒤 확인) [생성하기] 클릭 →
Expected: `Items/{id}.asset` 생성, 좌측 행이 편집 가능 버튼으로 전환, 우측 편집 패인 활성.

- [ ] **Step 3: 수동 검증 — Enum 추가 → 재컴파일 → 생성**

상단 입력란에 `TestNewCard` 입력 → [Enum 추가] → Unity 재컴파일 대기 →
Expected: `ECardId.cs`에 `TestNewCard,`가 마커 위에 추가, 재컴파일 후 좌측 목록에 `TestNewCard (미생성)` 등장 → [생성하기] → asset 생성. (검증 후 정리: 만든 asset 삭제 + `ECardId.cs`의 `TestNewCard,` 줄 수동 제거 가능.)

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs
# 커밋 메시지(안): # [feat] - 카드 생성 + Enum 추가 UI (미생성 슬롯 → 생성하기)
```

---

## Task 7: 풀 소속 토글

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (`DrawPoolToggles` 구현)

CardPool의 private `_cards` 리스트를 SerializedObject로 편집. 풀 에셋은 `t:CardPool` 검색으로 로드.

- [ ] **Step 1: `DrawPoolToggles` 구현**

```csharp
//# DrawPoolToggles stub 교체.
private CardPool LoadPool(EData key)
{
    string[] guids = AssetDatabase.FindAssets($"t:CardPool {key}");
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        if (Path.GetFileNameWithoutExtension(path) == key.ToString())
            return AssetDatabase.LoadAssetAtPath<CardPool>(path);
    }
    return null;
}

private void DrawPoolToggles(CardData card)
{
    EditorGUILayout.LabelField("풀 소속", EditorStyles.boldLabel);
    DrawOnePoolToggle(card, EData.CardPool_Passive, "Passive");
    DrawOnePoolToggle(card, EData.CardPool_Active, "Active");
}

private void DrawOnePoolToggle(CardData card, EData key, string label)
{
    CardPool pool = LoadPool(key);
    if (pool == null)
    {
        EditorGUILayout.LabelField($"{label}: 풀 에셋 없음");
        return;
    }

    int idx = IndexInPool(pool, card);
    bool isIn = idx >= 0;
    bool next = EditorGUILayout.ToggleLeft(label, isIn);
    if (next == isIn)
        return;

    SerializedObject so = new SerializedObject(pool);
    SerializedProperty list = so.FindProperty("_cards");
    if (next)
    {
        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = card;
    }
    else
    {
        RemoveAt(list, idx);
    }
    so.ApplyModifiedProperties();
    EditorUtility.SetDirty(pool);
    AssetDatabase.SaveAssets();
}

private static int IndexInPool(CardPool pool, CardData card)
{
    for (int i = 0; i < pool.Cards.Count; i++)
        if (pool.Cards[i] == card)
            return i;
    return -1;
}

//# object-reference 배열에서 DeleteArrayElementAtIndex 는 비-null 원소를 먼저 null 로만 만들고
//# 실제 제거는 두 번째 호출에서 일어난다. arraySize 가 안 줄면 한 번 더 호출해 null 잔존을 막는다.
private static void RemoveAt(SerializedProperty list, int idx)
{
    int before = list.arraySize;
    list.DeleteArrayElementAtIndex(idx);
    if (list.arraySize == before)
        list.DeleteArrayElementAtIndex(idx);
}
```

- [ ] **Step 2: 컴파일 + 수동 검증**

`editor_recompile` → 에러 0. 카드 선택 후 Passive/Active 토글 →
Expected: 토글 ON 시 해당 CardPool 에셋의 `_cards`에 카드 추가, OFF 시 제거. CardPool 인스펙터에서 확인. 중복 추가 안 됨. **OFF 후 `_cards`에 null 엔트리가 남지 않는지** 반드시 확인(null 잔존 시 런타임 카드 픽 NRE) — `RemoveAt` 헬퍼가 막아야 함.

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs
# 커밋 메시지(안): # [feat] - 풀(Passive/Active) 소속 토글 관리
```

---

## Task 8: 카드 삭제

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (`DrawDeleteButton` 구현)

삭제 = 양 풀에서 제거 + .asset 삭제. enum 값은 유지 → (미생성) 슬롯으로 재노출.

- [ ] **Step 1: `DrawDeleteButton` 구현**

```csharp
//# DrawDeleteButton stub 교체.
private void DrawDeleteButton(CardData card)
{
    GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
    if (GUILayout.Button("카드 삭제"))
    {
        bool ok = EditorUtility.DisplayDialog(
            "카드 삭제",
            $"{_selected} 의 .asset 을 삭제합니다.\n" +
            "ECardId enum 값은 유지되어 (미생성) 슬롯으로 다시 보이고, [생성하기] 로 재생성할 수 있습니다.",
            "삭제", "취소");
        if (ok)
            DeleteCard(card);
    }
    GUI.backgroundColor = Color.white;
}

private void DeleteCard(CardData card)
{
    RemoveFromPool(EData.CardPool_Passive, card);
    RemoveFromPool(EData.CardPool_Active, card);

    string path = AssetPath(_selected);
    AssetDatabase.DeleteAsset(path);
    AssetDatabase.SaveAssets();
    AssetDatabase.Refresh();

    _hasSelection = false;
    _editSo = null;
    Debug.Log($"[CardEditor] 삭제: {_selected} (enum 값 유지)");
}

private void RemoveFromPool(EData key, CardData card)
{
    CardPool pool = LoadPool(key);
    if (pool == null)
        return;
    int idx = IndexInPool(pool, card);
    if (idx < 0)
        return;

    SerializedObject so = new SerializedObject(pool);
    RemoveAt(so.FindProperty("_cards"), idx);   //# Task 7 의 안전 제거 헬퍼 재사용 (null 잔존 방지)
    so.ApplyModifiedProperties();
    EditorUtility.SetDirty(pool);
}
```

- [ ] **Step 2: 컴파일 + 수동 검증**

Task 6 Step 3에서 만든 `TestNewCard` 카드를 선택 → [카드 삭제] → 확인 →
Expected: `.asset` 삭제, 양 풀에서 제거, 좌측 목록에서 `TestNewCard (미생성)`로 재노출, [생성하기]로 재생성 가능. (검증 후 `ECardId.cs`의 `TestNewCard,` 줄은 수동 정리.)

- [ ] **Step 3: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs
# 커밋 메시지(안): # [feat] - 카드 삭제 (asset+풀 제거, enum 유지 → 재생성 가능)
```

---

## Task 9: 마감 — 아이콘 썸네일 + 최종 회귀

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs` (목록 행에 아이콘 썸네일)

- [ ] **Step 1: 목록 행 아이콘 썸네일 추가**

`DrawListRow`의 편집 가능 분기(`card != null`)에서 버튼 앞에 아이콘 프리뷰를 그린다:
```csharp
//# card != null 분기 버튼 직전에:
if (card.Icon != null)
{
    Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
    GUI.DrawTexture(r, AssetPreview.GetAssetPreview(card.Icon) ?? Texture2D.grayTexture, ScaleMode.ScaleToFit);
}
```

- [ ] **Step 1b (delta 2026-06-02 — spec §9 수용조건 2 / 기획서 §3.1 정합 보강): 목록 행에 풀 뱃지 렌더**

> **delta 사유:** 기존 Task 4/9 는 목록 행에 아이콘·이름·축만 렌더하고 풀 뱃지를 누락했다. 그러나 spec §9 수용조건 2("아이콘·이름·축·**풀 뱃지**와 함께 보인다") + 기획서 §3.1("아이콘 썸네일 + 이름 + `[Axis]` + **풀 뱃지**") + spec §5.6 이 풀 뱃지를 목록 행 필수 표시 요소로 명시한다. §7 YAGNI 비포함은 "축/풀 **필터 UI**"(인터랙티브 필터링)이지 수동 뱃지가 아니다. → code-reviewer BLOCKER 로 delta 보강.

`DrawListRow` 의 편집 가능 분기(`card != null`)에서 이름·축 버튼 라벨에 풀 뱃지를 합친다. 풀 소속 조회는 Task 7 의 `LoadPool(EData)`·`IndexInPool(CardPool,CardData)` 헬퍼를 재사용한다. Passive 소속 ` [P]`, Active 소속 ` [A]`, 둘 다면 ` [P] [A]`(각 뱃지 앞에 구분 공백), 미소속이면 뱃지 없음.

```csharp
//# 풀 뱃지 헬퍼 — Passive [P], Active [A], 둘 다 [P] [A], 미소속 빈 문자열 (기획서 §3.1).
private string BuildPoolBadge(CardData card)
{
    string badge = "";
    CardPool passive = LoadPool(EData.CardPool_Passive);
    if (passive != null && IndexInPool(passive, card) >= 0)
    {
        badge += " [P]";
    }
    CardPool active = LoadPool(EData.CardPool_Active);
    if (active != null && IndexInPool(active, card) >= 0)
    {
        badge += " [A]";
    }
    return badge;
}

//# card != null 분기 버튼 라벨에 뱃지 합치기:
string badge = BuildPoolBadge(card);
if (GUILayout.Button($"{name}  [{card.Axis}]{badge}", EditorStyles.label))
{
    _selected = id;
    _hasSelection = true;
}
```

> 매 행 풀 2회 로드는 AssetDatabase 캐시로 부담 적음 — 과한 캐시 최적화는 YAGNI. 풀 토글/삭제 후에도 다음 OnGUI 에서 즉시 갱신되는 이점.

- [ ] **Step 2: 최종 회귀 — 전체 EditMode 테스트 + 컴파일**

UnityMCP `editor_recompile` → 에러 0. Unity Test Runner 전체 EditMode 실행.
Expected: `CardEnumCodegenTests` 포함 전부 PASS, 기존 스위트 회귀 없음.

- [ ] **Step 3: 수동 수용 검증 (spec §9 체크리스트)**

1. `Lair > Card Editor` 열림 ✓
2. 28장 목록(아이콘·이름·축·풀 뱃지 [P]/[A]) ✓
3. 카드 선택 → 전 필드 편집 → 에셋 반영 ✓
4. Enum 추가 → 재컴파일 → (미생성) → 생성하기 → 편집 ✓
5. 풀 토글 → CardPool 반영 ✓
6. 삭제 → 제거, enum 유지, 재생성 ✓
7. 컴파일·기존 직렬화 무손상 ✓

- [ ] **Step 4: 커밋**

```bash
git add Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs
# 커밋 메시지(안): # [feat] - 카드 에디터 목록 아이콘 썸네일 + 최종 회귀 통과
```

---

## Task 10 (delta 2026-06-02 — 명시 [저장] + 풀 단일선택)

> **delta 사유:** 사용자 요청 2건 + design-reviewer "저장 UX 비대칭" 지적 해소. Task 5 는 필드를 매 프레임 `ApplyModifiedProperties` 로 즉시 반영하고 Ctrl+S 에 저장을 일임, Task 7 은 풀 토글을 토글 즉시 `SaveAssets` 했다 — 두 저장 경로가 비대칭. 이를 **단일 [저장] 버튼**으로 통일하고, 풀 소속을 **Passive ⊻ Active 단일 선택**으로 바꾼다. 런타임 스키마·codegen·삭제 동작 무변경.

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs`

- [ ] **Step 1: pending 풀 모델 도입**
  - 윈도우 내부 enum `EPoolKind { None, Passive, Active }` (단일 시스템 내부 enum — Rule 02 §8 예외로 CommonEnum.cs 미이동) + 필드 `private EPoolKind _pendingPool`.

- [ ] **Step 2: `DrawEditPane` 명시 저장 모델로 전환**
  - 매 프레임 `_editSo.Update()`(구 180) 와 `_editSo.ApplyModifiedProperties()`(구 200~203) 제거.
  - `Update()` 는 `SerializedObject` 재생성 블록(선택 전환 시) 안에서만 1회 호출 — 매 프레임 Update 는 pending 키스트로크를 덮어써 편집 불가가 된다.
  - 재생성 블록 진입 직전 `GuardUnsaved()`(이전 카드 기준 미저장 가드) 호출, 재생성 직후 `_pendingPool = GetCurrentPool(card)` 초기화.
  - 패인 하단에 `DrawPoolSelect()` → `DrawSaveBar(card)` → `DrawDeleteButton(card)` 순.

- [ ] **Step 3: 저장/가드/dirty 메서드 추가**
  - `IsDirty(card)` = `_editSo.hasModifiedProperties || _pendingPool != GetCurrentPool(card)`.
  - `DrawSaveBar(card)` — dirty 면 "● 저장되지 않은 변경" 표시, `EditorGUI.DisabledScope(dirty == false)` 로 [저장] 활성/비활성.
  - `CommitSave(card)` — `ApplyModifiedProperties` + `SetDirty(card)` + `SetPoolMembership(Passive/Active)` + 단일 `SaveAssets()`.
  - `GuardUnsaved()` — 이전 카드(`_editSo.targetObject`)에 미저장 변경 있으면 `DisplayDialog("저장","버리기")`. 저장 선택 시 `CommitSave(outgoing)`. 첫 선택(`_editSo == null`)은 다이얼로그 스킵.

- [ ] **Step 4: 풀 토글 → 단일 선택으로 교체**
  - 구 `DrawPoolToggles`/`DrawOnePoolToggle`(토글 즉시 저장) 제거.
  - `DrawPoolSelect()` — `GUILayout.Toolbar`(없음/Passive/Active) 로 `_pendingPool` 만 갱신(즉시 저장 안 함).
  - `GetCurrentPool(card)` — 실제 풀 소속 읽기(둘 다면 Passive 우선).
  - `SetPoolMembership(key, card, shouldBeIn)` — 중복 방지 추가 / 안전 제거(`RemoveAt`), 저장은 호출부 일괄.
  - `DeleteCard` 에 `_pendingPool = EPoolKind.None` 리셋 추가. `DrawDeleteButton`/`DeleteCard`/`CreateCard` 즉시 동작 유지.

- [ ] **Step 5: 검증** — UnityMCP `editor_recompile` → 에러 0 / EditMode 전체 회귀(codegen 18케이스 등) 0 FAIL. 수동: 필드 편집 시 "● 저장되지 않은 변경" 이 떠서 **유지**되고(매 프레임 사라지지 않음) 저장 전엔 .asset 미변경, [저장] 후 인스펙터 반영 + 풀 단일 소속 갱신.

---

## Task 11 (delta 2026-06-02 — 목록/풀 캐시로 cold-start 렉 제거)

> **delta 사유:** `Lair > Card Editor` 윈도우가 열려 있을 때 마우스 이동·스크롤·hover 마다 `OnGUI` 가 다회 호출되는데, `DrawListRow` 가 행마다 `Load`(LoadAssetAtPath) + `BuildPoolBadge`→`LoadPool`×2(각 `FindAssets`) + `AssetPreview` 를, `DrawEditPane` 도 `Load(_selected)` + `IsDirty`→`GetCurrentPool`→`LoadPool`×2 를 매 프레임 호출 → 프레임당 약 56회 FindAssets + 28회 LoadAssetAtPath. `FindAssets` 가 렉 주원인. **동작·UI·저장 모델 무변경, 성능만 개선.**

**Files:**
- Modify: `Assets/_Lair/Editor/CardTool/LairCardEditorWindow.cs`

- [ ] **Step 1: 스냅샷 캐시 구조 도입**
  - 윈도우 내부 `private struct RowCache { ECardId Id; CardData Card; EBuildAxis Axis; EPoolKind Pool; Sprite Icon; }` + `private readonly List<RowCache> _rows`. 풀 2개 캐시 필드 `_passivePool` / `_activePool` (Rule 02 §8 내부 타입 — 에셋 키 아님).
  - `RebuildCache()` — 풀 2개를 1회 로드 후 ECardId 전 행을 순회하며 `Card`/`Axis`/`Pool`(캐시된 풀로 판정)/`Icon`(Sprite 참조) 채움.

- [ ] **Step 2: `LoadPool` 고정 경로 직접 로드**
  - `Assets/_Lair/Art/Cards/CardPool_Passive.asset` / `CardPool_Active.asset` 고정 경로 `LoadAssetAtPath` → null 이면 기존 `FindAssets` 1회 폴백. `RebuildCache` 에서만 호출.

- [ ] **Step 3: 매 프레임 경로에서 AssetDatabase 제거**
  - `DrawListRow(RowCache, name)` — 캐시 행만 읽음. 풀 뱃지는 캐시된 `Pool` 값(`BuildPoolBadge(EPoolKind)`). 아이콘은 캐시된 Sprite 로 `AssetPreview.GetAssetPreview` 호출(AssetPreview 자체 캐시 — 비 AssetDatabase, 비동기 프리뷰 self-heal).
  - `DrawEditPane` — `Load(_selected)` → `GetCachedCard(_selected)`. `GetCurrentPool`/`GetPoolMembership` 가 캐시된 풀 참조 사용 → `IsDirty` 매 프레임 호출에도 AssetDatabase 접근 0.
  - `SetPoolMembership`/`RemoveFromPool` — 캐시된 풀 참조를 직접 변형.

- [ ] **Step 4: 캐시 무효화 시점**
  - `OnEnable`(열기/도메인 리로드) · `OnFocus`(외부 변경 복귀 + 비동기 프리뷰 재시도) · `CreateCard`/`CommitSave`/`DeleteCard` 끝에서 `RebuildCache()`. 상단 [새로고침] 버튼 1개.

- [ ] **Step 5: 검증** — UnityMCP `editor_recompile` → 에러 0 / EditMode 전체 회귀 0 FAIL. 코드 근거: `OnGUI` 도달 경로(버튼 클릭 제외) 의 `AssetDatabase.`/`LoadPool`/`FindAssets`/`Load(` grep = `RebuildCache`·버튼 핸들러 외 0 hit. 수동: 윈도우 열고 마우스 이동/스크롤 시 렉 체감 제거.

---

## Self-Review 결과

**Spec coverage:**
- §5.1 ECardId 분리 → Task 1 ✓
- §5.2 추가(enum codegen 2단계 + 미생성 생성) → Task 2·3·6 ✓
- §5.3 편집(네이티브 임베드) → Task 5 ✓
- §5.4 풀 소속 → Task 7 ✓
- §5.5 삭제(enum 유지) → Task 8 ✓
- §5.6 목록/필터(검색) + 풀 뱃지 [P]/[A] → Task 4·9(Step 1b delta) ✓
- §7 안내 문구 → Task 4(상단)·Task 5(편집 패인) ✓
- §6 컴포넌트 구조(전용 asmdef) → Task 2 ✓
- §9 수용 조건 → Task 9 Step 3 ✓

**Placeholder scan:** 코드 단계는 전부 실제 코드 포함. `DrawAddEnumRow`/`CreateCard`/`DrawPoolToggles`/`DrawDeleteButton`은 Task 4·5에서 명시적 stub로 도입되고 Task 6·7·8에서 실제 구현으로 교체됨(전진 의존, 누락 아님).

**Type consistency:** `AssetPath(ECardId)`·`Load(ECardId)`·`LoadPool(EData)`·`IndexInPool(CardPool,CardData)`·`CardEnumCodegen.InsertCardId/AppendCardId`·`Marker`/`EnumFilePath` 상수 — 정의처와 사용처 시그니처 일치 확인.

**Rule 02 준수:** `//#` 주석, 가드 절 중괄호 없음, `var` 미사용(명시 타입), `== null`/`== false` 사용, `!` 미사용 — 전 코드 블록 적용.

**Unity 런타임 시맨틱 리스크 (advisor 반영):**
- **R5 — `_id` 정체성**: 편집 패인에서 `_id`를 편집 가능하게 두면 파일명(=ECardId)과 `_id`가 어긋나 loader/list/CardDataSyncer desync. → Task 5에서 `_id`는 read-only LabelField, PropertyField 루프에서 제외.
- **R6 — SerializeReference 타입 피커**: null effect 신규 카드에 타입 드롭다운이 임베드 IMGUI에서 안 그려지면 "맨바닥 카드 생성"이 무의미. → Task 5 Step 0에서 신규 카드(null effect)로 조기 검증 + 리플렉션 수동 드롭다운 fallback.
- **R7 — `DeleteArrayElementAtIndex` null 잔존**: object-ref 배열은 첫 호출이 null 화만, 둘째 호출이 제거. → `RemoveAt` 헬퍼(arraySize 비교 후 재호출)로 Task 7·8 통일, 검증 단계에서 null 잔존 없음 확인.
