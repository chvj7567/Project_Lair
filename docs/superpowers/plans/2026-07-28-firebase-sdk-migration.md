# Firebase SDK 전환 (Spec 1.5 — 익명 인증) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Firebase 연동을 REST(`CHMHttpNetwork`) 에서 Firebase Unity SDK(Auth + Firestore) 로 교체하되, `ILairApiClient` 를 불변으로 유지해 UI·서비스·기존 테스트가 그대로 흡수되게 한다.

**Architecture:** 초기화만 인프라 패키지의 신규 `Firebase` 모듈(`CHMFirebase`, define 게이트 `UNITY_INFRA_FIREBASE`)로 올리고, 도메인 구현체 `FirebaseSdkApiClient : ILairApiClient` 는 게임 코드에 둔다. `Firebase.*` 타입은 이 두 곳 밖으로 새지 않는다 — `Lair.Tests.EditMode` asmdef 이 Firebase DLL 을 보지 못하므로 누출 시 기존 테스트 전체가 컴파일 실패하며, 이것이 회귀 방어선이다. 세이브 충돌은 `updateTime` precondition 에서 `RunTransactionAsync` + `DocumentSnapshot.UpdateTime` 비교로 재매핑하되 `CloudSaveResult` 계약은 유지한다.

**Tech Stack:** Unity 6 (6000.0.68f1) / URP 17.0.4 / C# / ChvjPackage(`com.chvj.unityinfra`) / Firebase Unity SDK — Auth + Firestore (UPM `.tgz`) / EDM4U / Unity Test Framework (NUnit, EditMode·PlayMode)

## Global Constraints

- **Rule 02 (C# 스타일)**: 주석은 `//#` · 가드절은 중괄호 없이 개행 · `var` 금지(명시 타입) · `!` 금지(`== false` / `== null`) · MVVM · View 에 비즈니스 로직 금지.
- **Rule 03 (인프라)**: 의존 방향은 게임 → 패키지 단방향. UI 위젯은 `CHText`/`CHButton`/`CHToggle`. 에셋 로드는 Enum 키. 런타임 스폰은 `CHMPool`.
- **Rule 01 (커밋)**: 자동 커밋 **금지**. 각 Task 는 `git add` + 한글 커밋 메시지(안) `# [주제] - 요약` 까지만. 신규 파일은 `.meta` 동반 스테이징, **수정 파일의 `.meta` 는 제외**.
- **Firebase 타입 격리 (불변 조건)**: `Firebase.*` 는 `CHMFirebase` 와 `FirebaseSdkApiClient` 내부에만 존재한다. `ILairApiClient` · `SaveResponseBody` · `RankingRowDto` · `CloudSaveResult` · `DisplayNameResult` 의 시그니처에 누출 금지.
- **회귀 게이트**: `FakeLairApiClient` 기반 EditMode 테스트(`CloudSaveServiceTests` · `CloudSaveServiceEdgeTests` · `CloudSaveRoundTripTests` · `CloudConflictFlagTests` · `NetDtoMappingTests` · `RankingClientTests` · `RankingMyRowMatchTests`)가 **전부 통과**해야 한다. Task 7 의 accountId 정리는 예외로 명시 처리.
- **모듈 게이트**: `FirebaseSdkApiClient.cs` 전체와 조립부는 `#if UNITY_INFRA_FIREBASE` 로 감싼다. 꺼진 상태 = `Api == null` = 기존 오프라인 계약.
- **namespace**: 인프라는 `ChvjUnityInfra`, 게임 Net 은 `Lair.Net`, 테스트는 `Lair.Tests.EditMode`.
- **테스트 메서드명**: 한글.
- **Firebase 프로젝트**: `lair-970fa` · Android 패키지명 `com.chvj.lair`.

---

## 파일 구조 (생성/수정 맵)

| 파일 | 책임 | 유형 | Task |
|---|---|---|---|
| `Packages/com.chvj.unityinfra/Runtime/Firebase/CHMFirebase.cs` | `FirebaseApp` 초기화 · 의존성 체크 · 준비 상태 노출. 도메인 무지 | 생성 | 2 |
| `Packages/com.chvj.unityinfra/Runtime/Firebase/com.chvj.unityinfra.firebase.asmdef` | define 게이트 `UNITY_INFRA_FIREBASE` | 생성 | 2 |
| `Packages/com.chvj.unityinfra/Editor/ChvjUnityInfraSettingsWindow.cs` | Firebase 탭 추가 (토글 + 가이드) | 수정 | 2 |
| `Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs` | `ILairApiClient` 8개 메서드 SDK 구현 | 생성 | 3·4·5 |
| `Assets/_Lair/Scripts/Meta/MetaSession.Net.cs` | 초기화 대기 + 조립 교체 | 수정 | 3 |
| `Assets/_Lair/Scripts/Village/VillageController.cs` | PlayMode 가드 · `MyAccountId` 제거 | 수정 | 6·7 |
| `Assets/_Lair/Tests/PlayMode/VillageSmokePlayTests.cs` | Firebase 초기화 우회 가드 | 수정 | 6 |
| `Assets/_Lair/Scripts/UI/Village/RankingPopup.cs` | accountId 폴백 제거 → 3단 폴백 | 수정 | 7 |
| `Assets/_Lair/Scripts/Net/NetDtos.cs` | `RankingRowDto.accountId` · 인증 DTO 제거 | 수정 | 7 |
| `Assets/_Lair/Scripts/Net/AuthTokenStore.cs` | DeviceId · Uid 만 잔존 | 수정 | 7 |
| `Assets/_Lair/Scripts/Net/ILairApiClient.cs` | 주석 리워드(시그니처 불변) | 수정 | 7 |
| `Assets/_Lair/Scripts/Data/CommonEnum.cs` | `EData.NetworkConfig` 제거 | 수정 | 7 |
| `Assets/_Lair/Tests/EditMode/RankingMyRowMatchTests.cs` | accountId 케이스 제거 · 4축 커버리지 유지 | 수정 | 7 |
| `Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs` | 삭제 멤버 테스트 제거 | 수정 | 7 |
| **삭제** — `LairApiClient.cs` · `FirebaseApiClient.cs` · `FirestoreJson.cs` · `NetworkConfig.cs` · `Art/Net/NetworkConfig.asset` · `LairApiClientParseRowsTests.cs` · `FirebaseApiClientParseTests.cs` · `FirestoreJsonTests.cs` | | 삭제 | 7 |
| `CLAUDE.md` §8 · `.claude/rules/03-chvjpackage.md` §1 · `docs/design/firebase-security-rules.md` | 문서 정합 | 수정 | 8 |

---

### Task 1: 게이트 A — SDK 설치 성립 (롤백 결정 지점)

**이 Task 는 코드를 쓰지 않는다.** 사용자가 에디터 GUI 작업을 수행하고, 그 결과를 확인한 뒤 미해소 항목 2건을 확정한다. 실패하면 **여기서 멈추고 REST 유지 롤백을 판단한다** — 아직 아무것도 지우지 않았으므로 롤백 비용은 SDK 제거뿐이다.

**Files:**
- Modify: `ProjectSettings/ProjectSettings.asset` (패키지명 · companyName)
- Create: `Assets/google-services.json` (사용자가 배치)
- Create (임시): `Assets/_Lair/Editor/FirebaseGateCheck.cs` — **Task 1 종료 시 삭제** (Rule 04 §3 일회용 툴 규약)

**Interfaces:**
- Consumes: 없음
- Produces: 없음 (검증 결과만)

- [ ] **Step 1: 사용자 수행 — SDK 설치 및 프로젝트 설정**

사용자에게 다음을 요청하고 완료를 기다린다:

1. Firebase Unity SDK 설치 — **Auth + Firestore 둘 다**
   - **2026-07-28 실제 진행**: `.unitypackage` 형식으로 설치됨(SDK 13.14.0, `Assets/Firebase/`). spec Q5 는 UPM `.tgz` 를 전제했으나 그 항목은 위생 선택이지 룰 강제가 아니므로 그대로 진행한다.
   - **`FirebaseAuth.unitypackage` 는 적용 완료. `FirebaseFirestore.unitypackage` 가 아직 없다** — Task 4 이후가 전부 Firestore 를 쓰므로 Task 3 착수 전까지 임포트되어야 한다.
2. `google-services.json`(프로젝트 `lair-970fa`, 패키지명 `com.chvj.lair`) 를 `Assets/` 직하에 배치
3. Edit > Project Settings > Player — Package Name 을 `com.chvj.lair` 로, Company Name 을 `chvj` 로 변경
4. Android 플랫폼으로 스위치 (EDM4U 가 gradle 템플릿을 생성하도록)

- [ ] **Step 2: 컴파일 성립 확인**

Unity 에디터 콘솔에 컴파일 에러가 0건인지 확인한다.
Expected: 에러 없음. EDM4U 가 `Assets/Plugins/Android/` 에 gradle 템플릿을 생성했을 수 있다(정상).

에러가 나면 **여기서 멈추고 사용자에게 보고**한다.

- [ ] **Step 3: 미해소 항목 ① — Firebase 어셈블리 참조 방식 확정**

설치된 SDK 를 조사해 다음을 판정한다:

```
1. Packages/ (또는 Assets/) 의 Firebase 패키지 안에 *.asmdef 가 있는가?
   - 있다  → asmdef 배포. 어셈블리 이름을 기록한다(예: "Firebase.Auth", "Firebase.Firestore").
             Task 2 의 firebase asmdef "references" 에 그 이름들을 넣는다.
             Lair.asmdef 의 "references" 에도 동일하게 추가해야 한다 → 파일 구조 표에 Lair.asmdef(수정) 추가.
   - 없다  → 자동 참조 precompiled DLL 배포. overrideReferences: false 인 어셈블리가 자동으로 본다.
             Task 2 의 firebase asmdef 는 references 없이 overrideReferences: false 로 둔다.
             Lair.asmdef 는 수정 불요.
```

판정 결과를 이 plan 파일의 Task 2 Step 2 주석에 기록한다.

- [ ] **Step 4: 미해소 항목 ② — 집계 쿼리(`Count()`) 가용성 확정**

`Firebase.Firestore` 어셈블리에 집계 API 가 있는지 확인한다. 확인 방법 — 임시 에디터 스크립트에 다음 한 줄을 넣고 컴파일되는지 본다:

```csharp
//# 컴파일되면 집계 가용, CS1061 이면 미가용.
Firebase.Firestore.AggregateQuery q = Firebase.Firestore.FirebaseFirestore.DefaultInstance
    .Collection("leaderboard").Count;
```

```
- 컴파일됨   → Task 5 의 GetMyRankAsync 를 집계 경로로 구현한다.
- 컴파일 실패 → Task 5 의 GetMyRankAsync 는 폴백 경로(Top N 클라이언트 계산)로 구현한다.
                이 경우 "Top N 밖일 때 내 순위를 어떻게 표기할지" 는 UX 결정이므로
                여기서 멈추고 사용자/game-designer 에게 에스컬레이션한다(임의 결정 금지).
```

판정 결과를 Task 5 Step 1 주석에 기록한다.

- [ ] **Step 5: 익명 로그인 1회 성공 확인**

`Assets/_Lair/Editor/FirebaseGateCheck.cs` 를 만든다. **`ILairApiClient` 를 경유하지 않는다** — 아직 구현체가 없다.

```csharp
using Firebase;
using Firebase.Auth;
using Firebase.Extensions;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Task 1 게이트 A 전용 일회용 확인 툴. 게이트 통과 후 삭제한다(Rule 04 §3).
    public static class FirebaseGateCheck
    {
        [MenuItem("Lair/Gate/Firebase 익명 로그인 확인")]
        public static void Check()
        {
            FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
            {
                if (task.Result != DependencyStatus.Available)
                {
                    Debug.LogError($"[GateA] 의존성 실패: {task.Result}");
                    return;
                }
                Debug.Log("[GateA] 의존성 Available");
                FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync().ContinueWithOnMainThread(auth =>
                {
                    if (auth.IsFaulted || auth.IsCanceled)
                    {
                        Debug.LogError($"[GateA] 익명 로그인 실패: {auth.Exception}");
                        return;
                    }
                    Debug.Log($"[GateA] 익명 로그인 성공 uid={FirebaseAuth.DefaultInstance.CurrentUser.UserId}");
                });
            });
        }
    }
}
```

> **버전 주의**: `SignInAnonymouslyAsync()` 의 반환 타입이 SDK 버전에 따라 `Task<FirebaseUser>` 또는 `Task<AuthResult>` 로 갈린다. 위 코드는 반환값을 쓰지 않고 `CurrentUser` 로 uid 를 읽어 **양쪽 버전에서 컴파일된다.** Task 3 도 이 패턴을 따른다.

- [ ] **Step 6: 게이트 A 통과 판정**

Unity 메뉴 `Lair/Gate/Firebase 익명 로그인 확인` 실행.
Expected: 콘솔에 `[GateA] 의존성 Available` → `[GateA] 익명 로그인 성공 uid=...` 2줄.

**3종 통과 기준**: (1) 컴파일 성립 (2) 의존성 `Available` (3) 익명 로그인 성공.
하나라도 실패하면 **멈추고 사용자에게 보고 + 롤백 판단**.

- [ ] **Step 7: 임시 툴 삭제**

`Assets/_Lair/Editor/FirebaseGateCheck.cs` 와 `.meta` 를 삭제한다 (Rule 04 §3 — 일회용 authoring/검증 툴은 레포에 남기지 않는다).

- [ ] **Step 8: 커밋(안)**

```
git add ProjectSettings/ProjectSettings.asset Assets/google-services.json Assets/google-services.json.meta Packages/manifest.json Packages/packages-lock.json
```
(EDM4U 가 `Assets/Plugins/Android/` 를 생성했다면 그 파일들도 함께 add)

커밋 메시지(안): `# [chore] - Firebase Unity SDK 도입 및 앱 패키지명을 com.chvj.lair 로 변경`

---

### Task 2: 인프라 Firebase 모듈 (`CHMFirebase` + asmdef + Settings 탭)

**Files:**
- Create: `Packages/com.chvj.unityinfra/Runtime/Firebase/CHMFirebase.cs`
- Create: `Packages/com.chvj.unityinfra/Runtime/Firebase/com.chvj.unityinfra.firebase.asmdef`
- Modify: `Packages/com.chvj.unityinfra/Editor/ChvjUnityInfraSettingsWindow.cs`

**Interfaces:**
- Consumes: Firebase SDK (`FirebaseApp`, `DependencyStatus`)
- Produces:
  - `ChvjUnityInfra.CHMFirebase` (싱글턴, `CHSingletonStatic<CHMFirebase>` 상속 — `CHMGPGS` 와 동일 패턴)
  - `Task<bool> CHMFirebase.Instance.EnsureReadyAsync()` — 초기화 1회 보장. 성공 true.
  - `bool CHMFirebase.Instance.IsReady` — 준비 완료 여부

- [ ] **Step 1: asmdef 생성**

`com.chvj.unityinfra.firebase.asmdef` — `com.chvj.unityinfra.social.asmdef` 와 동일 구조. `references` 는 **Task 1 Step 3 판정 결과**에 따른다.

```json
{
  "name": "com.chvj.unityinfra.firebase",
  "rootNamespace": "ChvjUnityInfra",
  "references": [
    "com.chvj.unityinfra"
  ],
  "includePlatforms": [],
  "excludePlatforms": [],
  "allowUnsafeCode": false,
  "overrideReferences": false,
  "precompiledReferences": [],
  "autoReferenced": true,
  "defineConstraints": [
    "UNITY_INFRA_FIREBASE"
  ],
  "versionDefines": [],
  "noEngineReferences": false
}
```

> **Task 1 Step 3 판정 결과 (2026-07-28 확정)**: SDK 13.14.0 이 `.unitypackage` 로 `Assets/Firebase/` 에 설치됐고, `Firebase.App.dll` · `Firebase.Auth.dll` 은 **asmdef 없는 순수 precompiled DLL**(유일한 asmdef 은 내부용 `Firebase.App.Internal.asmdef`). → **자동 참조 방식**이므로 위 asmdef 을 그대로 쓰고 `Lair.asmdef` 는 수정하지 않는다.
>
> **단 미검증 리스크 1건**: `Assets/` 의 자동 참조 플러그인을 `Packages/` 의 asmdef 이 자동으로 참조하는지는 확인되지 않았다. Step 4 컴파일에서 `Firebase` 네임스페이스를 못 찾으면(CS0246) **폴백**한다 —
> ```json
> "overrideReferences": true,
> "precompiledReferences": ["Firebase.App.dll", "Firebase.Auth.dll", "Firebase.Firestore.dll"]
> ```
> 이 폴백은 DLL 위치와 무관하게 동작한다. 폴백을 쓰게 되면 이 plan 의 해당 줄을 확정 값으로 갱신한다.
>
> `includePlatforms` 는 비워 둔다 — Social 과 달리 Firebase 는 에디터 플레이에서도 동작해야 한다.

- [ ] **Step 2: `CHMFirebase.cs` 구현**

```csharp
#if UNITY_INFRA_FIREBASE
using System.Threading.Tasks;
using Firebase;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// Firebase 초기화 매니저. FirebaseApp 의존성 체크를 1회 보장한다.
    /// 도메인(세이브/랭킹 스키마)을 알지 못한다 — 그건 게임 코드 소관.
    /// Tools/ChvjUnityInfra/Settings > Firebase 탭에서 모듈 토글.
    /// </summary>
    public class CHMFirebase : CHSingletonStatic<CHMFirebase>
    {
        private Task<bool> _initTask;

        /// <summary>초기화가 끝나고 Firebase 사용 가능한 상태인지.</summary>
        public bool IsReady { get; private set; }

        /// <summary>의존성 체크 1회 보장. 중복 호출은 같은 Task 를 공유한다. 실패 시 false.</summary>
        public Task<bool> EnsureReadyAsync()
        {
            if (_initTask == null)
            {
                _initTask = InitAsync();
            }
            return _initTask;
        }

        private async Task<bool> InitAsync()
        {
            DependencyStatus status = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (status != DependencyStatus.Available)
            {
                Debug.LogWarning($"[CHMFirebase] 의존성 사용 불가: {status}");
                IsReady = false;
                return false;
            }
            IsReady = true;
            return true;
        }
    }
}
#endif
```

- [ ] **Step 3: Settings 윈도우에 Firebase 탭 추가**

`ChvjUnityInfraSettingsWindow.cs` 를 4지점 편집한다.

**(a)** define 상수 추가 — `SOCIAL_DEFINE` 선언 아래:

```csharp
        private const string FIREBASE_DEFINE = "UNITY_INFRA_FIREBASE";
```

**(b)** 탭 라벨 배열에 추가:

```csharp
        private static readonly string[] TabLabels = { "Ads", "IAP", "Social", "Firebase" };
```

**(c)** `OnGUI` 의 switch 에 case 추가:

```csharp
                case 3: DrawFirebaseTab(); break;
```

**(d)** `DrawSocialTab()` 아래에 탭 메서드 추가:

```csharp
        // ────────── Firebase ──────────

        private void DrawFirebaseTab()
        {
            EditorGUILayout.LabelField("Firebase (Auth + Firestore)", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            DrawToggle("Use Firebase", FIREBASE_DEFINE);

            EditorGUILayout.Space();

#if UNITY_INFRA_FIREBASE
            EditorGUILayout.HelpBox(
                "사용 스텝:\n" +
                "1. 'Use Firebase' 체크 (이미 켜져 있음)\n" +
                "2. Firebase 콘솔에서 앱 등록 후 google-services.json 을 Assets/ 에 배치\n" +
                "   (Android 는 Player Settings 의 Package Name 과 일치해야 함)\n" +
                "3. 게임 부팅 코드에 추가:\n" +
                "   #if UNITY_INFRA_FIREBASE\n" +
                "   bool ready = await CHMFirebase.Instance.EnsureReadyAsync();\n" +
                "   #endif\n" +
                "4. 준비 확인 후 Auth/Firestore SDK 직접 사용:\n" +
                "   FirebaseAuth.DefaultInstance.SignInAnonymouslyAsync();\n" +
                "   FirebaseFirestore.DefaultInstance.Collection(\"...\");\n" +
                "\n" +
                "범위: 이 모듈은 초기화만 담당한다.\n" +
                "컬렉션 구조·쿼리 등 도메인 로직은 게임 코드에 둔다.\n" +
                "보안 규칙은 Firebase 콘솔 소관 — 레포에 넣지 않는다.",
                MessageType.Info);
#else
            EditorGUILayout.HelpBox(
                "Firebase 모듈이 꺼져 있습니다.\n" +
                "'Use Firebase' 체크 → 컴파일 완료 후 사용 가이드가 표시됩니다.\n" +
                "전제: Firebase Unity SDK(Auth + Firestore) UPM 설치 필요.",
                MessageType.Warning);
#endif
        }
```

- [ ] **Step 4: 토글 동작 확인**

Unity 메뉴 `Tools/ChvjUnityInfra/Settings` → Firebase 탭.
Expected:
- 탭이 4개(Ads / IAP / Social / Firebase) 보인다
- "Use Firebase" 체크 → 컴파일 후 Info HelpBox 로 바뀐다
- 체크 해제 → 컴파일 후 Warning HelpBox 로 바뀌고, `CHMFirebase` 어셈블리가 빠져도 **프로젝트가 여전히 컴파일된다** (아직 게임 코드가 참조하지 않으므로)
- 다시 체크해 켜 둔 상태로 마무리한다

- [ ] **Step 5: 전체 EditMode 회귀**

Unity Test Runner EditMode 전체 실행.
Expected: 전부 PASS (인프라 모듈 추가는 게임 코드에 영향 없음).

- [ ] **Step 6: 커밋(안)**

```
git add Packages/com.chvj.unityinfra/Runtime/Firebase Packages/com.chvj.unityinfra/Editor/ChvjUnityInfraSettingsWindow.cs
```
(신규 폴더 전체를 add — `.cs`/`.asmdef` 와 각 `.meta` 포함. `ChvjUnityInfraSettingsWindow.cs` 는 수정 파일이라 `.meta` 제외)

커밋 메시지(안): `# [infra] - ChvjPackage 에 Firebase 초기화 모듈 추가 (Settings 윈도우 토글 포함)`

---

### Task 3: `FirebaseSdkApiClient` — 인증 + 조립 교체

이 Task 가 끝나면 **마을 진입 시 익명 인증이 SDK 경로로 성공**한다. 나머지 6개 메서드는 컴파일 가능한 스텁으로 두고 Task 4·5 에서 채운다.

**Files:**
- Create: `Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs`
- Modify: `Assets/_Lair/Scripts/Meta/MetaSession.Net.cs`

**Interfaces:**
- Consumes: `CHMFirebase.Instance.EnsureReadyAsync()`, `AuthTokenStore.SaveUid/Uid`, `ILairApiClient`
- Produces:
  - `Lair.Net.FirebaseSdkApiClient : ILairApiClient` — **매개변수 없는 생성자** (`NetworkConfig` 를 받지 않는다. 설정은 `google-services.json` 이 담당)
  - `Task<bool> AuthenticateAsync()` — 초기화 보장 → 익명 로그인 → uid 저장

- [ ] **Step 1: 구현 — `FirebaseSdkApiClient.cs` (인증 + 스텁)**

```csharp
#if UNITY_INFRA_FIREBASE
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Firebase.Auth;
using Firebase.Firestore;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# Firebase Auth + Firestore SDK 로 ILairApiClient 를 구현. Firebase.* 타입은 이 클래스 밖으로 나가지 않는다.
    //# 접속 설정은 google-services.json 이 담당 — 별도 설정 SO 없음.
    public class FirebaseSdkApiClient : ILairApiClient
    {
        private const string SavesCollection = "saves";
        private const string LeaderboardCollection = "leaderboard";
        private const string DisplayNamesCollection = "displayNames";

        //# GetSave 시 캐시 — PutSave 트랜잭션의 충돌 판정 기준(마지막으로 본 버전).
        private Timestamp? _saveUpdateTime;

        private static FirebaseFirestore Db => FirebaseFirestore.DefaultInstance;

        public async Task<bool> AuthenticateAsync()
        {
            bool ready = await CHMFirebase.Instance.EnsureReadyAsync();
            if (ready == false)
                return false;

            FirebaseAuth auth = FirebaseAuth.DefaultInstance;
            //# 이미 로그인돼 있으면(SDK 가 자격증명을 영속화) 재로그인하지 않는다 — uid 유지가 핵심.
            if (auth.CurrentUser == null)
            {
                try
                {
                    //# 반환값을 쓰지 않는다 — SDK 버전에 따라 Task<FirebaseUser>/Task<AuthResult> 로 갈리므로
                    //# CurrentUser 로 읽어야 양쪽에서 컴파일된다.
                    await auth.SignInAnonymouslyAsync();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[FirebaseSdkApiClient] 익명 인증 실패: {e.Message}");
                    return false;
                }
            }

            if (auth.CurrentUser == null)
                return false;
            AuthTokenStore.SaveUid(auth.CurrentUser.UserId);
            return true;
        }

        //# 현재 로그인 uid — 미인증이면 빈 문자열.
        private static string Uid
        {
            get
            {
                FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
                return user == null ? string.Empty : user.UserId;
            }
        }

        //# --- 이하 Task 4·5 에서 구현. 스텁으로 컴파일 유지. ---
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult<SaveResponseBody>(null);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt) => Task.FromResult(CloudSaveResult.Failed);
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(false);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(DisplayNameResult.Of(DisplayNameStatus.Offline));
    }
}
#endif
```

- [ ] **Step 2: 조립 교체 — `MetaSession.Net.cs`**

`EnsureNetworkAsync()` 전체를 교체한다. `NetworkConfig` Addressable 로드가 사라지는 것이 핵심 — 설정은 `google-services.json` 이 담당한다.

```csharp
        //# client/서비스 구성 + 익명 인증 보장. best-effort. 설정은 google-services.json.
        public static async Task EnsureNetworkAsync()
        {
            if (Api != null)
                return;
#if UNITY_INFRA_FIREBASE
            FirebaseSdkApiClient client = new FirebaseSdkApiClient();
            bool authed = await client.AuthenticateAsync();
            if (authed == false)
            {
                Debug.LogWarning("[MetaSession] 익명 인증 실패 — 클라우드 비활성");
                return;
            }
            Api = client;
            Cloud = new CloudSaveService(Api);
            Ranking = new RankingClient(Api);
#else
            Debug.LogWarning("[MetaSession] Firebase 모듈 꺼짐 — 클라우드 비활성");
            await Task.CompletedTask;
#endif
        }
```

> **주의**: 인증 성공 후에만 `Api` 를 대입한다(실패 시 부분 구성 방지). 기존 코드는 먼저 대입하고 실패 시 null 로 되돌렸는데, 그 사이에 다른 코드가 `IsCloudConnected` 를 읽으면 잘못된 true 를 본다.
> 상단 `using` 에서 이제 쓰지 않는 `ChvjUnityInfra`(`CHMResource`)·`Lair.Data`(`EData`) 가 남으면 제거한다.

- [ ] **Step 3: 전체 EditMode 회귀**

Unity Test Runner EditMode 전체 실행.
Expected: **전부 PASS.** 이것이 "Firebase 타입이 `ILairApiClient` 밖으로 새지 않았다" 는 증명이다 — 누출됐다면 `Lair.Tests.EditMode` 가 Firebase DLL 을 못 봐 컴파일 실패한다.

- [ ] **Step 4: 인증 동작 수동 확인**

에디터에서 Village 씬 플레이 → 마을 진입.
Expected:
- 콘솔에 `[MetaSession] 익명 인증 실패` 로그가 **없다**
- Firebase 콘솔 Authentication > Users 에 익명 사용자가 1명 생겨 있다

- [ ] **Step 5: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs.meta Assets/_Lair/Scripts/Meta/MetaSession.Net.cs
```

커밋 메시지(안): `# [feat] - 익명 로그인을 Firebase SDK 경로로 전환`

---

### Task 4: 세이브 — 조회 + 백업 + 트랜잭션 충돌 감지

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs` (`GetSaveAsync` · `PutSaveAsync` 실구현)

**Interfaces:**
- Consumes: `MetaProfile`, `SaveResponseBody`, `CloudSaveResult`, `Timestamp`
- Produces: `GetSaveAsync` · `PutSaveAsync` 실동작 (시그니처 불변)

- [ ] **Step 1: `GetSaveAsync` 구현 — 스텁 줄을 교체**

```csharp
        public async Task<SaveResponseBody> GetSaveAsync()
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return null;
            try
            {
                DocumentSnapshot snap = await Db.Collection(SavesCollection).Document(uid).GetSnapshotAsync();
                if (snap.Exists == false)
                {
                    //# 문서 없음 = "세이브 없음". 최초 생성 경로를 위해 캐시를 비운다.
                    _saveUpdateTime = null;
                    return null;
                }
                _saveUpdateTime = snap.UpdateTime;
                string profileJson = snap.ContainsField("profile") ? snap.GetValue<string>("profile") : null;
                if (string.IsNullOrEmpty(profileJson))
                    return null;
                MetaProfile profile = JsonUtility.FromJson<MetaProfile>(profileJson);
                if (profile == null)
                    return null;
                return new SaveResponseBody
                {
                    profile = profile,
                    schemaVersion = snap.ContainsField("schemaVersion") ? (int)snap.GetValue<long>("schemaVersion") : 0,
                    updatedAt = snap.ContainsField("updatedAt") ? snap.GetValue<string>("updatedAt") : null,
                };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 조회 실패: {e.Message}");
                _saveUpdateTime = null;
                return null;
            }
        }
```

- [ ] **Step 2: `PutSaveAsync` 구현 — 스텁 줄을 교체**

```csharp
        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            if (profile == null)
                return CloudSaveResult.Failed;
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return CloudSaveResult.Failed;

            //# 첫 백업 전 서버 기준시각(base version)을 시딩 — 복귀 유저의 오충돌 방지.
            //# 문서가 없으면 캐시는 null 로 남아 "최초 생성" 경로가 된다.
            if (_saveUpdateTime.HasValue == false)
            {
                await GetSaveAsync();
            }

            Timestamp? expected = _saveUpdateTime;
            DocumentReference doc = Db.Collection(SavesCollection).Document(uid);
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "profile", JsonUtility.ToJson(profile) },
                { "schemaVersion", profile.Version },
                { "updatedAt", clientUpdatedAt },
            };

            try
            {
                bool conflict = false;
                await Db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot snap = await transaction.GetSnapshotAsync(doc);
                    //# 충돌 판정 — "내가 마지막으로 본 버전 이후에 누가 썼는가".
                    if (expected.HasValue)
                    {
                        //# 기대: 문서가 존재하고 UpdateTime 이 캐시와 같다.
                        if (snap.Exists == false || snap.UpdateTime.Equals(expected.Value) == false)
                        {
                            conflict = true;
                            return;
                        }
                    }
                    else
                    {
                        //# 기대: 최초 생성 — 문서가 없어야 한다.
                        if (snap.Exists)
                        {
                            conflict = true;
                            return;
                        }
                    }
                    transaction.Set(doc, fields);
                });

                if (conflict)
                    return CloudSaveResult.Conflict;

                //# 성공 시 새 버전시각을 재캐시 — 세션 내 2번째+ 백업의 자기충돌 방지.
                DocumentSnapshot after = await doc.GetSnapshotAsync();
                _saveUpdateTime = after.Exists ? after.UpdateTime : (Timestamp?)null;
                return CloudSaveResult.Success;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 세이브 저장 실패: {e.Message}");
                return CloudSaveResult.Failed;
            }
        }
```

> **왜 트랜잭션 안에서 플래그를 세우고 밖에서 판정하는가**: 트랜잭션 델리게이트에서 예외를 던지면 SDK 가 재시도하거나 `AggregateException` 으로 감싸 원인 구분이 어려워진다. 플래그 + `return` 으로 쓰기 없이 빠져나오면 트랜잭션이 무해하게 커밋되고, 충돌 여부를 정확히 구분할 수 있다.

- [ ] **Step 3: 전체 EditMode 회귀**

Expected: 전부 PASS (특히 `CloudSaveServiceTests` · `CloudSaveRoundTripTests` · `CloudConflictFlagTests`).

- [ ] **Step 4: 세이브 동작 수동 확인**

에디터 플레이 → 마을 진입 → 클라우드 메뉴에서 백업 수행.
Expected:
- Firebase 콘솔 Firestore 에 `saves/{uid}` 문서 생성
- 필드 3종(`profile` 문자열 · `schemaVersion` 정수 · `updatedAt` 문자열) 확인
- 한 번 더 백업 → **거짓 충돌 없이 성공** (재캐시 동작 확인)
- 콘솔에서 문서를 직접 수정 후 백업 → **충돌 배지 노출** (충돌 감지 동작 확인)

- [ ] **Step 5: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs
```

커밋 메시지(안): `# [feat] - 클라우드 세이브 백업·복원과 충돌 감지를 Firebase SDK 트랜잭션으로 전환`

---

### Task 5: 리더보드 + 표시명

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs` (남은 4개 메서드)

**Interfaces:**
- Consumes: `RankingRowDto`, `DisplayNameResult`, `DisplayNameStatus`
- Produces: `SubmitScoreAsync` · `GetTopAsync` · `GetMyRankAsync` · `ChangeDisplayNameAsync` 실동작

- [ ] **Step 1: `SubmitScoreAsync` + `GetTopAsync` 구현 — 스텁 줄을 교체**

```csharp
        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return false;
            Dictionary<string, object> fields = new Dictionary<string, object>
            {
                { "uid", uid },
                { "displayName", displayName ?? string.Empty },
                { "clearTimeMs", clearTimeMs },
                { "hero", hero ?? string.Empty },
            };
            try
            {
                await Db.Collection(LeaderboardCollection).Document(uid).SetAsync(fields);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 랭킹 제출 실패: {e.Message}");
                return false;
            }
        }

        public async Task<List<RankingRowDto>> GetTopAsync(int top)
        {
            List<RankingRowDto> rows = new List<RankingRowDto>();
            try
            {
                QuerySnapshot snap = await Db.Collection(LeaderboardCollection)
                    .OrderBy("clearTimeMs")
                    .Limit(top)
                    .GetSnapshotAsync();
                int rank = 1;
                foreach (DocumentSnapshot doc in snap.Documents)
                {
                    RankingRowDto row = ToRow(doc);
                    if (row == null)
                        continue;
                    //# 쿼리가 rank 를 내려주지 않는다 — clearTimeMs 오름차순 순서 = 순위(1부터).
                    row.rank = rank;
                    rank++;
                    rows.Add(row);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 랭킹 조회 실패: {e.Message}");
            }
            return rows;
        }

        //# 리더보드 문서 → 행 DTO. 필드 누락은 기본값으로 흡수(흐름을 막지 않는다).
        private static RankingRowDto ToRow(DocumentSnapshot doc)
        {
            if (doc == null || doc.Exists == false)
                return null;
            return new RankingRowDto
            {
                uid = doc.ContainsField("uid") ? doc.GetValue<string>("uid") : null,
                displayName = doc.ContainsField("displayName") ? doc.GetValue<string>("displayName") : null,
                clearTimeMs = doc.ContainsField("clearTimeMs") ? (int)doc.GetValue<long>("clearTimeMs") : 0,
                hero = doc.ContainsField("hero") ? doc.GetValue<string>("hero") : null,
            };
        }
```

- [ ] **Step 2: `GetMyRankAsync` 구현 — Task 1 Step 4 판정에 따라 분기**

> **Task 1 Step 4 판정이 "집계 가용" 이면 아래 (A) 를, "미가용" 이면 (B) 를 쓴다. 판정 없이 진행하지 않는다.**

**(A) 집계 가용 — 절대 등수**

```csharp
        public async Task<List<RankingRowDto>> GetMyRankAsync()
        {
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return new List<RankingRowDto>();
            try
            {
                DocumentSnapshot mine = await Db.Collection(LeaderboardCollection).Document(uid).GetSnapshotAsync();
                RankingRowDto myRow = ToRow(mine);
                //# clearTimeMs<=0 은 유효한 클리어 기록이 아님(유령 문서) — 거짓 "1위 00:00" 방지.
                if (myRow == null || IsRankedClearTime(myRow.clearTimeMs) == false)
                    return new List<RankingRowDto>();

                AggregateQuerySnapshot agg = await Db.Collection(LeaderboardCollection)
                    .WhereLessThan("clearTimeMs", myRow.clearTimeMs)
                    .Count
                    .GetSnapshotAsync(AggregateSource.Server);
                myRow.rank = agg.Count + 1;
                return new List<RankingRowDto> { myRow };
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 내 순위 조회 실패: {e.Message}");
                return new List<RankingRowDto>();
            }
        }

        //# 유효 클리어 시간 판정 — clearTimeMs 는 소요시간(ms)이라 0/음수는 실제 클리어일 수 없다.
        public static bool IsRankedClearTime(long ms) => ms > 0;
```

**(B) 집계 미가용 — 폴백**

이 경로는 "Top N 밖일 때 내 순위를 어떻게 표기할지" 라는 **UX 결정이 선행**되어야 한다. Task 1 Step 4 에서 이미 에스컬레이션했어야 하며, 결정 없이 이 Step 을 구현하지 않는다. 결정이 내려온 뒤 그 내용을 이 Step 에 코드로 채워 넣고 진행한다.

- [ ] **Step 3: `ChangeDisplayNameAsync` 구현 — 스텁 줄을 교체**

```csharp
        public async Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName)
        {
            string norm = displayName == null ? string.Empty : displayName.Trim();
            if (string.IsNullOrEmpty(norm))
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            //# 문서ID 파손 문자 차단 — / 는 경로 분리자. 제품 charset 정책이 아니라 기술 제약.
            if (norm.IndexOf('/') >= 0)
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            string uid = Uid;
            if (string.IsNullOrEmpty(uid))
                return DisplayNameResult.Of(DisplayNameStatus.Offline);

            DocumentReference lockDoc = Db.Collection(DisplayNamesCollection).Document(norm);
            DocumentReference lbDoc = Db.Collection(LeaderboardCollection).Document(uid);

            try
            {
                bool taken = false;
                await Db.RunTransactionAsync(async transaction =>
                {
                    DocumentSnapshot lockSnap = await transaction.GetSnapshotAsync(lockDoc);
                    //# 이미 존재하고 소유자가 내가 아니면 중복. 내 것이면 재점유 허용(멱등).
                    if (lockSnap.Exists)
                    {
                        string owner = lockSnap.ContainsField("uid") ? lockSnap.GetValue<string>("uid") : null;
                        if (owner != uid)
                        {
                            taken = true;
                            return;
                        }
                    }
                    transaction.Set(lockDoc, new Dictionary<string, object> { { "uid", uid } });
                    //# 리더보드는 displayName 만 병합 — Set 전체 치환이면 clearTimeMs/hero 가 증발한다.
                    transaction.Set(lbDoc, new Dictionary<string, object> { { "displayName", norm } }, SetOptions.MergeAll);
                });

                if (taken)
                    return DisplayNameResult.Of(DisplayNameStatus.Taken);
                return new DisplayNameResult(DisplayNameStatus.Success, norm);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[FirebaseSdkApiClient] 표시명 변경 실패: {e.Message}");
                return DisplayNameResult.Of(DisplayNameStatus.Offline);
            }
        }
```

> **옛 이름 잠금 삭제는 생략한다** — 로컬이 직전 이름을 모를 수 있고, 잔여 잠금은 무해하다(그 이름만 못 쓸 뿐). 유일성은 잠금 생성이 담당한다. Spec 1 기획서 §2.3 이 이미 이 한계를 감수 결정했다.

- [ ] **Step 4: 전체 EditMode 회귀**

Expected: 전부 PASS (`RankingClientTests` · `NetDtoMappingTests` 포함).

- [ ] **Step 5: 리더보드·표시명 수동 확인**

에디터 플레이:
- 한 판 클리어 → Firestore `leaderboard/{uid}` 문서 생성 확인
- 랭킹 팝업 → Top 목록 표시 + 내 행 강조 확인
- 마을에서 표시명 변경 → 성공 → Firestore `displayNames/{이름}` 문서 생성 + `leaderboard/{uid}.displayName` 갱신 확인, **`clearTimeMs`/`hero` 가 남아 있는지** 확인(MergeAll 동작)
- 같은 이름을 다시 변경 시도 → 본인 소유이므로 성공(멱등)
- 콘솔에서 다른 uid 소유의 잠금 문서를 만들고 그 이름으로 변경 시도 → **"이미 사용 중인 이름입니다."** 토스트

- [ ] **Step 6: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseSdkApiClient.cs
```

커밋 메시지(안): `# [feat] - 랭킹 제출·조회와 표시명 중복 차단을 Firebase SDK 로 전환`

---

### Task 6: PlayMode 가드 + 게이트 B (동작 검증)

spec §7.1 의 선택을 확정하고 적용한다. **안 C(가짜 선주입)를 채택**한다 — 기존 오프라인 계약(`Api != null` 조기 반환)을 그대로 활용해 초기화 자체를 회피하므로, 프로덕션 코드에 테스트 전용 분기를 넣지 않아도 된다. 안 A 는 런타임 코드를 오염시키고, 안 B 는 테스트를 네트워크에 의존시킨다.

**Files:**
- Modify: `Assets/_Lair/Tests/PlayMode/VillageSmokePlayTests.cs`

**Interfaces:**
- Consumes: `MetaSession.Api`, `ILairApiClient`
- Produces: 없음 (테스트 격리만)

- [ ] **Step 1: 현재 PlayMode 스모크 실패 확인**

Unity Test Runner PlayMode → `VillageSmokePlayTests` 실행.
Expected: 마을 진입에서 Firebase 초기화가 돌면서 **느려지거나 멈추거나 예외 로그가 뜬다.** (환경에 따라 통과할 수도 있는데, 그렇더라도 네트워크 의존이 생긴 것이므로 Step 2 는 그대로 진행한다.)

- [ ] **Step 2: 가짜 클라이언트 선주입 가드 추가**

`VillageSmokePlayTests` 에 `[SetUp]`/`[TearDown]` 을 추가한다. 클래스에 이미 있으면 본문에 아래 줄을 합친다.

```csharp
        //# 마을 진입은 MetaSession.EnsureNetworkAsync 를 await 한다(VillageController.cs:46).
        //# Api 를 미리 채워 두면 조기 반환하므로 Firebase 초기화가 돌지 않는다 — 스모크를 네트워크에서 격리.
        [SetUp]
        public void 클라우드_격리()
        {
            MetaSession.Api = new PlayModeStubApiClient();
        }

        [TearDown]
        public void 클라우드_격리_해제()
        {
            MetaSession.Api = null;
            MetaSession.Cloud = null;
            MetaSession.Ranking = null;
        }
```

같은 파일 하단(namespace 안)에 스텁을 정의한다. `Lair.Tests.PlayMode` asmdef 은 `Lair` 를 참조하므로 `ILairApiClient` 가 보인다.

```csharp
    //# PlayMode 스모크 전용 — 모든 op 가 즉시 "아무것도 없음" 을 반환한다(네트워크 미사용).
    public class PlayModeStubApiClient : ILairApiClient
    {
        public Task<bool> AuthenticateAsync() => Task.FromResult(true);
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult<SaveResponseBody>(null);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt) => Task.FromResult(CloudSaveResult.Success);
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(true);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(new DisplayNameResult(DisplayNameStatus.Success, displayName));
    }
```

필요한 `using` 을 파일 상단에 추가한다: `System.Collections.Generic`, `System.Threading.Tasks`, `Lair.Meta`, `Lair.Net`.

- [ ] **Step 3: PlayMode 재실행**

Expected: `VillageSmokePlayTests` **네트워크 없이 결정적으로 PASS.** Firebase 초기화 로그가 뜨지 않는다.

- [ ] **Step 4: PlayMode 전체 회귀**

PlayMode 스위트 전체 실행.
Expected: 전부 PASS. 다른 PlayMode 테스트가 마을을 거치면 동일한 격리가 필요할 수 있다 — 실패하는 테스트가 있으면 같은 `[SetUp]` 패턴을 적용한다.

- [ ] **Step 5: 게이트 B — 동작 검증 4종**

**여기가 삭제를 승인하는 관문이다.** 실기기 또는 에디터 플레이로 다음을 모두 확인한다:

```
[ ] 마을 진입 시 "[MetaSession] 익명 인증 실패" 로그가 없다
[ ] Firestore 에 saves/{uid} 문서가 생성된다
[ ] 앱을 껐다 켠 뒤 uid 가 동일하다   ← SDK 자격증명 영속화. 실패 시 세이브가 고아가 된다
[ ] 랭킹 팝업이 Top 목록과 내 순위를 표시한다
```

**uid 확인 방법**: 마을 진입 후 `AuthTokenStore.Uid`(PlayerPrefs `Lair.Net.Uid`) 값을 로그로 찍거나 Firebase 콘솔 Authentication > Users 의 사용자 수가 늘지 않는지 본다. **사용자가 2명으로 늘면 실패다.**

하나라도 실패하면 **Task 7 로 진행하지 않고 멈춰 사용자에게 보고한다.** 이 시점까지는 REST 코드가 그대로 남아 있어 롤백이 싸다.

- [ ] **Step 6: 커밋(안)**

```
git add Assets/_Lair/Tests/PlayMode/VillageSmokePlayTests.cs
```

커밋 메시지(안): `# [test] - 마을 스모크 테스트를 클라우드 연동에서 격리`

---

### Task 7: 사문화 코드 삭제 · accountId 경로 정리

**게이트 B 통과 후에만 시작한다.** 테스트를 먼저 고쳐 실패시키고(=삭제 후의 계약을 먼저 고정), 그다음 코드를 지운다.

**Files:**
- Delete: `Assets/_Lair/Scripts/Net/LairApiClient.cs` · `FirebaseApiClient.cs` · `FirestoreJson.cs` · `NetworkConfig.cs` (+ 각 `.meta`)
- Delete: `Assets/_Lair/Art/Net/NetworkConfig.asset` (+ `.meta`)
- Delete: `Assets/_Lair/Tests/EditMode/LairApiClientParseRowsTests.cs` · `FirebaseApiClientParseTests.cs` · `FirestoreJsonTests.cs` (+ 각 `.meta`)
- Modify: `Assets/_Lair/Tests/EditMode/RankingMyRowMatchTests.cs` · `AuthTokenStoreTests.cs`
- Modify: `Assets/_Lair/Scripts/UI/Village/RankingPopup.cs` · `Net/NetDtos.cs` · `Net/AuthTokenStore.cs` · `Net/ILairApiClient.cs` · `Village/VillageController.cs` · `Data/CommonEnum.cs`

**Interfaces:**
- Consumes: 없음
- Produces:
  - `RankingPopup.IsMyRow(RankingRowDto row, string myUid, int myClearMs, bool alreadyFound)` — private static, 4-arg (accountId 제거, `ByUid` 접미사 제거)
  - `RankingPopup.PickMyRow(List<RankingRowDto> rows, string myUid, int myClearMs)` — private static, 3-arg
  - `RankingPopupArg` — `MyAccountId` 제거, `MyUid` · `MyBestClearTime` 유지
  - `RankingRowDto` — `accountId` 제거, `rank` · `displayName` · `clearTimeMs` · `hero` · `uid` 유지
  - `AuthTokenStore` — `GetOrCreateDeviceId()` · `Uid` · `HasUid` · `SaveUid(string)` 만

- [ ] **Step 1: `RankingMyRowMatchTests.cs` 를 새 계약으로 재작성**

파일 전체를 교체한다. accountId 케이스와 2-arg 리플렉션 헬퍼가 사라지고, **남겨야 할 4축**(uid 일치/불일치 · uid 미식별 시 시간 폴백 · 동률 시 첫 매칭만 · Pick 의 첫 행 폴백)만 남는다.

```csharp
using System.Collections.Generic;
using System.Reflection;
using Lair.Net;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# "내 행" 식별 — uid 1차, uid 미식별이면 clearTimeMs 시간 폴백. (accountId 경로는 2026-07-28 제거)
    public class RankingMyRowMatchTests
    {
        private static bool IsMyRow(RankingRowDto row, string myUid, int myClearMs, bool alreadyFound)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("IsMyRow", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "RankingPopup.IsMyRow(4-arg) 를 찾을 수 없다");
            return (bool)m.Invoke(null, new object[] { row, myUid, myClearMs, alreadyFound });
        }

        private static RankingRowDto PickMyRow(List<RankingRowDto> rows, string myUid, int myClearMs)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("PickMyRow", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "RankingPopup.PickMyRow(3-arg) 를 찾을 수 없다");
            return (RankingRowDto)m.Invoke(null, new object[] { rows, myUid, myClearMs });
        }

        //# 축1 — uid 일치: 시간 무관하게 내 행.
        [Test]
        public void IsMyRow_uid일치하면_시간무관하게_내행이다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1", clearTimeMs = 999999 };
            Assert.IsTrue(IsMyRow(row, "u1", 123018, false));
        }

        //# 축1 — uid 불일치: 시간이 같아도 내 행이 아니다(권위 키 우선).
        [Test]
        public void IsMyRow_uid불일치면_시간이같아도_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u2", clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, "u1", 123018, false));
        }

        //# 축2 — row.uid 없음(구데이터): uid 게이트 미진입 → 시간 폴백으로 매칭.
        [Test]
        public void IsMyRow_row의uid가없으면_시간폴백으로_매칭한다()
        {
            RankingRowDto row = new RankingRowDto { uid = null, clearTimeMs = 123018 };
            Assert.IsTrue(IsMyRow(row, "u1", 123018, false));
        }

        //# 축2 — myUid 없음(미인증): 시간 폴백.
        [Test]
        public void IsMyRow_myUid없으면_시간폴백으로_매칭한다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u9", clearTimeMs = 50000 };
            Assert.IsTrue(IsMyRow(row, null, 50000, false));
            Assert.IsFalse(IsMyRow(row, null, 50001, false));
        }

        //# 축2 — 시간 폴백인데 내 기록이 없음(-1): 매칭하지 않는다.
        [Test]
        public void IsMyRow_내기록이없으면_시간폴백도_매칭하지않는다()
        {
            RankingRowDto row = new RankingRowDto { uid = null, clearTimeMs = 50000 };
            Assert.IsFalse(IsMyRow(row, null, -1, false));
        }

        //# 축3 — 이미 찾았으면 이후 행은 무조건 false(중복 강조 방지).
        [Test]
        public void IsMyRow_이미찾았으면_더는_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1", clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, "u1", 123018, true));
        }

        //# 축1 — Pick: uid 일치 행을 우선 선택.
        [Test]
        public void PickMyRow_uid일치행을_우선선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u2", clearTimeMs = 123018 },
                new RankingRowDto { uid = "u1", clearTimeMs = 999999 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 123018);
            Assert.AreEqual("u1", picked.uid);
        }

        //# 축2 — Pick: uid 일치가 없으면 시간 일치 행.
        [Test]
        public void PickMyRow_uid일치없으면_시간폴백으로_선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u3", clearTimeMs = 50000 },
                new RankingRowDto { uid = "u4", clearTimeMs = 123018 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 123018);
            Assert.AreEqual(123018, picked.clearTimeMs);
        }

        //# 축4 — Pick: uid·시간 모두 못 찾으면 첫 행.
        [Test]
        public void PickMyRow_아무것도_못찾으면_첫행을_반환한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u3", clearTimeMs = 50000 },
                new RankingRowDto { uid = "u4", clearTimeMs = 60000 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 999999);
            Assert.AreEqual("u3", picked.uid);
        }

        //# 엣지 — 빈 목록이면 null.
        [Test]
        public void PickMyRow_빈목록이면_null이다()
        {
            Assert.IsNull(PickMyRow(new List<RankingRowDto>(), "u1", 123018));
        }
    }
}
```

- [ ] **Step 2: 실패 확인**

Unity Test Runner EditMode 실행.
Expected: `RankingMyRowMatchTests` **전부 실패** — `IsMyRow(4-arg)` / `PickMyRow(3-arg)` 가 아직 없어 `Assert.IsNotNull(m, ...)` 에서 걸린다.

- [ ] **Step 3: `RankingPopup.cs` 정리**

`RankingPopupArg` 에서 `MyAccountId` 필드와 그 주석 2줄을 제거하고, `MyBestClearTime` 주석의 "accountId 미식별 시" 를 "uid 미식별 시" 로 고친다.

`IsMyRowByUid` / `PickMyRowByUid` / `IsMyRow` / `PickMyRow` **4개 메서드를 아래 2개로 교체**한다:

```csharp
        //# "내 행" 식별 — uid 1차(양쪽 존재 시 권위 키, 유일 매칭). 동률 시 첫 매칭만(중복 강조 방지).
        //# uid 미식별(내 uid 없음 또는 행에 uid 없음)이면 clearTimeMs 시간 폴백.
        private static bool IsMyRow(RankingRowDto row, string myUid, int myClearMs, bool alreadyFound)
        {
            if (alreadyFound || row == null)
                return false;
            if (string.IsNullOrEmpty(myUid) == false && string.IsNullOrEmpty(row.uid) == false)
                return row.uid == myUid;
            if (myClearMs < 0)
                return false;
            return row.clearTimeMs == myClearMs;
        }

        //# 내 순위 응답에서 내 행 1개 선택 — uid 일치 우선, 없으면 시간 일치, 그래도 없으면 첫 행.
        private static RankingRowDto PickMyRow(List<RankingRowDto> rows, string myUid, int myClearMs)
        {
            if (rows == null || rows.Count == 0)
                return null;
            if (string.IsNullOrEmpty(myUid) == false)
            {
                foreach (RankingRowDto row in rows)
                {
                    if (row != null && string.IsNullOrEmpty(row.uid) == false && row.uid == myUid)
                        return row;
                }
            }
            if (myClearMs >= 0)
            {
                foreach (RankingRowDto row in rows)
                {
                    if (row != null && row.clearTimeMs == myClearMs)
                        return row;
                }
            }
            return rows[0];
        }
```

`Load()` 안의 호출부 3지점을 고친다:

```csharp
            //# Top 100 행 매핑 — "내 행" 표시(uid 1차 → BestClearTime 시간 fallback).
            ...
            long myAccountId = arg.MyAccountId;          //# ← 이 줄 삭제
            ...
                bool isMine = IsMyRow(row, myUid, myClearMs, foundMineInTop);
            ...
                RankingRowDto myRow = PickMyRow(mine, myUid, myClearMs);
```

- [ ] **Step 4: 통과 확인**

EditMode 실행.
Expected: `RankingMyRowMatchTests` 10개 전부 PASS.

- [ ] **Step 5: `NetDtos.cs` 정리**

- `RankingRowDto` 에서 `public long accountId;` 와 그 주석 2줄 제거. `uid` 주석의 "사문화된 accountId 보다 우선" 을 "내 행 매칭 키" 로 정리
- `AnonymousAuthRequestBody` · `AnonymousAuthResponse` 클래스 제거 (REST 자체서버 전용, 소비자 0)
- `PutSaveRequestBody` · `SubmitScoreRequestBody` · `SubmitScoreResponseBody` · `DisplayNameRequestBody` · `DisplayNameResponseBody` · `RankingRowListWrapper` 제거 (전부 REST 요청/응답 전용, SDK 경로는 `Dictionary<string, object>` 를 쓴다)
- **`SaveResponseBody` 와 `RankingRowDto` 는 유지** — `ILairApiClient` 시그니처에 있다
- 파일 상단 주석 "서버 응답/요청 본문 — 필드명은 서버 JSON 과 정확히 일치" 를 남은 타입에 맞게 리워드

> 제거 대상 중 하나라도 다른 곳에서 참조되면 컴파일이 깨진다. `NetDtoMappingTests` 가 참조하면 그 테스트도 함께 정리한다(Step 8 에서 회귀로 잡힌다).

- [ ] **Step 6: `AuthTokenStore.cs` 정리**

`Token` · `HasToken` · `SaveToken` · `ClearToken` · `AccountId` · `HasAccountId` · `SaveAccountId` · `RefreshToken` · `SaveRefreshToken` 과 관련 키 상수(`TokenKey` · `AccountIdKey` · `RefreshTokenKey`) 를 제거한다. 파일 상단 주석을 교체한다:

```csharp
    //# deviceId(GUID 1회 생성)와 Firebase uid 를 PlayerPrefs 에 저장. Application.dataPath 쓰기 금지(과거 사고 회피).
    //# deviceId 는 인증과 무관하다 — 자동 표시명 "영주 #xxxx" 의 시드로만 쓰인다(VillageViewModel).
    //# 자격증명(idToken·refreshToken)은 Firebase SDK 가 영속화한다 — 여기서 관리하지 않는다.
```

`Uid` 프로퍼티 주석도 `//# Firebase 계정 식별자 — 랭킹 "내 행" 매칭 키.` 로 정리한다.

- [ ] **Step 7: `AuthTokenStoreTests.cs` 정리**

`토큰_저장후_읽으면_같은값이다` · `AccountId_저장후_읽으면_같은값이다` · `AccountId_미설정이면_0이고_HasAccountId는_false다` · `RefreshToken_저장후_조회된다` 4개 테스트를 삭제한다. `[TearDown]` 에서 `Lair.Net.Token` · `Lair.Net.AccountId` · `Lair.Net.RefreshToken` 키 삭제 줄도 제거한다.

남는 것: `DeviceId_없으면_생성하고_재호출시_동일하다` · `Uid_저장후_조회된다` · `미설정_Uid_는_빈문자열이고_HasUid_false` 3개.

- [ ] **Step 8: `VillageController.cs` · `ILairApiClient.cs` · `CommonEnum.cs` 정리**

- `VillageController.cs:206` 의 `MyAccountId = AuthTokenStore.AccountId,` 줄 삭제
- `ILairApiClient.cs:10` 주석을 `//# 인증 — Firebase 익명 로그인으로 계정 보장 + uid 저장. 성공 여부 반환.` 으로 교체
- `CommonEnum.cs:79` 의 `NetworkConfig,` enum 값과 주석 삭제

- [ ] **Step 9: 파일 삭제**

```
Assets/_Lair/Scripts/Net/LairApiClient.cs (+ .meta)
Assets/_Lair/Scripts/Net/FirebaseApiClient.cs (+ .meta)
Assets/_Lair/Scripts/Net/FirestoreJson.cs (+ .meta)
Assets/_Lair/Scripts/Net/NetworkConfig.cs (+ .meta)
Assets/_Lair/Art/Net/NetworkConfig.asset (+ .meta)
Assets/_Lair/Tests/EditMode/LairApiClientParseRowsTests.cs (+ .meta)
Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs (+ .meta)
Assets/_Lair/Tests/EditMode/FirestoreJsonTests.cs (+ .meta)
```

`NetworkConfig.asset` 삭제 후 **Addressables 그룹에서 엔트리가 사라졌는지 확인**한다 (Window > Asset Management > Addressables > Groups). 남아 있으면 수동 제거.

- [ ] **Step 10: 전체 회귀 (EditMode + PlayMode)**

Expected:
- 컴파일 에러 0건
- EditMode 전부 PASS — `RankingMyRowMatchTests` 10개, `AuthTokenStoreTests` 3개, `FakeLairApiClient` 기반 테스트 전부
- PlayMode 전부 PASS
- **`RankingPopup` 의 남은 분기 3종(uid 일치 / 시간 폴백 / 첫 행)이 모두 테스트로 덮여 있는지 확인** — 덮이지 않은 분기가 있으면 spec §5 를 잘못 적용한 것이다

- [ ] **Step 11: 랭킹 동작 재확인**

에디터 플레이 → 랭킹 팝업.
Expected: 내 행 강조가 정리 전과 동일하게 동작한다(uid 매칭이 권위 키였으므로 표시 결과는 안 바뀌어야 한다).

- [ ] **Step 12: 커밋(안)**

```
git add -u Assets/_Lair/Scripts/Net Assets/_Lair/Tests/EditMode Assets/_Lair/Art/Net
git add Assets/_Lair/Scripts/UI/Village/RankingPopup.cs Assets/_Lair/Scripts/Village/VillageController.cs Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/AddressableAssetsData
```
(`git add -u` 로 삭제를 스테이징 — 삭제 파일은 `.meta` 도 함께 잡힌다. 수정 파일의 `.meta` 는 포함하지 않는다)

커밋 메시지(안): `# [refactor] - REST 백엔드 잔재와 사문화된 계정 식별 경로 제거`

---

### Task 8: 문서 정합

**Files:**
- Modify: `CLAUDE.md` (§8)
- Modify: `.claude/rules/03-chvjpackage.md` (§1 모듈 목록)
- Modify: `docs/design/firebase-security-rules.md` (알려진 한계)

**Interfaces:**
- Consumes: 없음
- Produces: 없음

- [ ] **Step 1: `CLAUDE.md` §8 갱신**

"서버 연동 허용" bullet 에서 REST 지칭을 교체한다. **백엔드가 Firebase BaaS 이고 이 레포엔 연동 클라이언트만 둔다는 취지·범위 규칙은 그대로 유지**하고, 연동 수단 서술만 바꾼다.

```
- **서버 연동 허용 (2026-06-15 v0.3 승격 · 2026-07-14 Firebase 로 피벗 · 2026-07-28 SDK 로 전환) — 단 "Unity 클라이언트 ↔ 백엔드 연동 코드"만 허용** — 익명 인증→uid 로컬 캐시, MetaProfile 클라우드 백업/복원, 최단클리어 랭킹 제출/조회 등 클라이언트 측 연동 코드. **백엔드는 Firebase BaaS(Firebase Auth + Cloud Firestore, Firebase Unity SDK)를 사용하며 운영할 자체 서버가 없다.** 자격증명은 SDK 가 영속화하며 접속 설정은 `google-services.json` 이 담당한다. 폐기된 자체 서버(`Project_Lair_Server`, ASP.NET Core+MySQL+Redis)는 아카이브하며 그 데이터는 마이그레이션하지 않는다. 서버측 검증·안티치트는 Firestore 보안 규칙이 담당하는 범위까지이며, 그 이상(Cloud Functions 등 서버리스 코드)은 이 레포 범위 밖이다.
```

`§9 절대 금지` 는 **변경하지 않는다** — "백엔드(Firebase 보안 규칙·Cloud Functions 등) 구현을 이 Unity 레포에서 작성" 금지는 전환 후에도 그대로 유효하다.

- [ ] **Step 2: `.claude/rules/03-chvjpackage.md` §1 모듈 목록 갱신**

```
**모듈**: `Runtime/Core` · `Resource` · `Pool` · `Audio` · `UI` · `Ads` · `Iap` · `Social` · `Firebase` / `Editor/` / `Tests/`
```

- [ ] **Step 3: `docs/design/firebase-security-rules.md` 알려진 한계 갱신**

"장시간 세션 중 토큰 재인증 부재" bullet 을 **해소로 교체**한다:

```markdown
- **~~장시간 세션 중 토큰 재인증 부재~~ (2026-07-28 SDK 전환으로 해소)** — REST 시절에는 세션당 1회만 인증해 idToken TTL 1시간을 넘기면 클라우드 op 가 조용히 실패했다. Firebase Unity SDK 가 토큰 갱신을 자동 처리하므로 이 한계는 사라졌다.
```

나머지 3개 한계(랭킹 조작 가능 · 익명 인증 기기이전 불가 · 표시명 재점유 불가)는 **그대로 유지**한다 — 전환과 무관하다.

데이터 모델 섹션도 그대로 유지한다 — 컬렉션·필드 구조는 바뀌지 않았다.

- [ ] **Step 4: 문서 간 모순 확인**

`CHMHttpNetwork` 로 Firebase 를 호출한다는 서술이 남아 있는지 레포 전체를 검색한다.

```
grep -rn "CHMHttpNetwork" CLAUDE.md .claude/ docs/
```

Expected: `docs/superpowers/specs/2026-07-14-*` · `docs/superpowers/plans/2026-07-14-*` · `docs/design/firebase-backend-pivot.md` 의 **과거 기록**에만 남는다(이력 문서이므로 수정하지 않는다). `CLAUDE.md` · `.claude/rules/` 에는 Firebase 맥락의 `CHMHttpNetwork` 언급이 남지 않아야 한다.

- [ ] **Step 5: 커밋(안)**

```
git add CLAUDE.md .claude/rules/03-chvjpackage.md docs/design/firebase-security-rules.md
```

커밋 메시지(안): `# [docs] - 백엔드 연동 수단을 Firebase SDK 로 문서 정합`

---

## Self-Review

**1. Spec coverage** (spec 각 절 → task 매핑):

| spec | task |
|---|---|
| §1 범위 (SDK 도입 · 익명까지) | T1(설치) · T3(인증) · T4·T5(도메인 op) ✓ |
| §2 Q1 사문화 전부 삭제 | T7 Step 9 ✓ |
| §2 Q2 AuthTokenStore + accountId 정리 | T7 Step 3·5·6·7·8 ✓ |
| §2 Q3 설치 검증 게이트 | T1 (게이트 A) · T6 Step 5 (게이트 B) ✓ |
| §2 Q4 하이브리드 배치 | T2(인프라 초기화) · T3(게임 도메인) ✓ |
| §2 Q5 UPM 설치 | T1 Step 1 ✓ |
| §3.1 Firebase 타입 격리 | T3 Step 3 · T4 Step 3 · T5 Step 4 의 EditMode 회귀가 컴파일로 검증 ✓ |
| §3.2 모듈 게이트 `#if` | T2 Step 4(토글 확인) · T3 Step 1·2 ✓ |
| §4.1 충돌 재매핑 | T4 Step 2 ✓ |
| §4.2 나머지 직역 매핑 | T5 Step 1·2·3 ✓ |
| §5 삭제/유지 목록 | T7 전체 ✓ |
| §6 에러 처리 매핑 | T4·T5 의 각 try/catch + 결과 타입 매핑 ✓ |
| §7 테스트 전략 (회귀 게이트) | T3·T4·T5·T7 의 EditMode 회귀 Step ✓ |
| §7.1 PlayMode 상호작용 | T6 (안 C 채택, 근거 명시) ✓ |
| §8.1 게이트 A | T1 Step 6 ✓ |
| §8.2 게이트 B | T6 Step 5 ✓ |
| §9 문서 갱신 3종 | T8 ✓ |
| §10 미해소 ① asmdef 참조 | T1 Step 3 → T2 Step 1 ✓ |
| §10 미해소 ② 집계 가용성 | T1 Step 4 → T5 Step 2 ✓ |

갭 0.

**2. Placeholder 스캔**: "TBD/TODO/나중에/적절히" 0건. 모든 code step 에 실제 코드. T5 Step 2 의 (B) 분기만 코드가 비어 있는데, 이는 placeholder 가 아니라 **UX 결정 없이 구현하면 안 되는 지점을 의도적으로 게이트로 만든 것**이며 에스컬레이션 조건(T1 Step 4)과 함께 명시했다. T1 은 코드를 쓰지 않는 Task 임을 명시했다.

**3. Type consistency**:
- `CHMFirebase.EnsureReadyAsync()` → `Task<bool>` — T2 정의, T3 사용 일치
- `FirebaseSdkApiClient` 매개변수 없는 생성자 — T3 정의, T3 Step 2 조립 사용 일치 (`NetworkConfig` 를 받지 않음이 T7 의 `NetworkConfig` 삭제와 정합)
- `Uid` private static 프로퍼티 — T3 정의, T4·T5 사용 일치
- `ToRow(DocumentSnapshot)` → `RankingRowDto` — T5 Step 1 정의, Step 2 사용 일치
- `IsRankedClearTime(long)` — T5 Step 2(A) 에서 정의·사용. (B) 분기를 타면 이 헬퍼도 그쪽에서 정의해야 함을 (B) 서술이 포함
- `IsMyRow(row, myUid, myClearMs, alreadyFound)` 4-arg / `PickMyRow(rows, myUid, myClearMs)` 3-arg — T7 Step 1 테스트가 리플렉션으로 기대하는 시그니처와 Step 3 구현이 정확히 일치
- `_saveUpdateTime` 타입 `Timestamp?` — T3 선언, T4 두 메서드에서 일관 사용
- `PlayModeStubApiClient` — T6 에서만 정의·사용, `ILairApiClient` 8개 메서드 전부 구현

**4. 순서 제약 검증**: spec §8 의 `게이트 A → 인프라 → 클라이언트 → 게이트 B → 삭제 → 문서` 가 T1~T8 순서와 일치한다. 삭제(T7)가 게이트 B(T6 Step 5) 뒤에 있어 롤백 가능 구간이 보존된다. T7 Step 1·2 가 테스트를 먼저 실패시키고 Step 3 이 구현을 맞추는 TDD 순서다.

**5. 위험 지점 (구현자 유의)**:
- Firebase Unity SDK 는 버전별 API 편차가 있다(`SignInAnonymouslyAsync` 반환 타입, 집계 쿼리 유무). T1 이 두 지점을 사전 판정하도록 설계했으나, 그 밖의 편차가 나오면 **추측으로 메우지 말고 보고**한다.
- `RunTransactionAsync` 델리게이트 안에서 예외를 던지지 않는다(재시도·래핑으로 원인 구분이 어려워짐). 플래그 + `return` 패턴을 유지한다.
- `SetOptions.MergeAll` 없이 `Set` 하면 문서가 전체 치환된다 — 표시명 변경에서 `clearTimeMs`/`hero` 가 증발한다. T5 Step 5 의 수동 확인이 이걸 잡는다.
