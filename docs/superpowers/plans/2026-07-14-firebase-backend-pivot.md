# Firebase 백엔드 피벗 (Spec 1 — 익명 인증) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 자체 서버(ASP.NET) 연동을 Firebase BaaS(Auth + Firestore, REST) 연동으로 교체하되, `ILairApiClient` 인터페이스를 불변으로 유지해 변경 반경을 구현체 1개 + 조립/저장소 인접 2지점으로 최소화한다.

**Architecture:** 신규 `FirebaseApiClient : ILairApiClient` 가 Firebase Auth REST + Firestore REST 를 `CHMHttpNetwork` 로 호출해 8개 메서드를 재구현한다. 모든 Firestore 쓰기는 `documents:commit`(POST) 배치로 처리한다(`CHMHttpNetwork` 에 PATCH/DELETE 없음). 순수 파싱/빌드 로직은 정적 함수(`FirestoreJson`, `FirebaseApiClient` 의 static 파서)로 분리해 EditMode 로 테스트하고, 실 HTTP 통신은 테스트 범위 밖으로 둔다.

**Tech Stack:** Unity 6 / C# / ChvjPackage(`CHMHttpNetwork`, `CHMResource`) / Firebase Auth REST(`identitytoolkit`, `securetoken`) / Firestore REST(`documents:commit` · `runQuery` · `runAggregationQuery`) / Unity Test Framework(NUnit, EditMode).

## Global Constraints

- 코드 스타일 Rule 02: 주석 `//#` · 가드절 중괄호 없이 개행 · `var` 금지(명시 타입) · `!` 금지(`== false`/`== null`) · MVVM.
- 인프라 Rule 03: 게임→패키지 단방향. HTTP 는 `CHMHttpNetwork` 만. Enum 키 로드. 패키지 역참조 금지 — `CHMHttpNetwork` 에 새 동사 추가 대신 `documents:commit` 로 우회.
- 커밋 Rule 01: 자동 커밋 금지 — 각 Task 는 `git add` + 한글 커밋 메시지(안) `# [주제] - 요약` 까지만. 신규 파일은 `.cs.meta` 동반 스테이징, 수정 파일 meta 는 제외.
- 인터페이스 계약: `ILairApiClient`(`Assets/_Lair/Scripts/Net/ILairApiClient.cs`) 시그니처 불변. 기존 `FakeLairApiClient` 기반 EditMode 테스트가 **전부 그대로 통과**해야 한다(회귀 게이트).
- 테스트 asmdef: `Lair.Tests.EditMode`. 테스트 위치 `Assets/_Lair/Tests/EditMode/`. 메서드명 한글 규약.
- namespace: `Lair.Net`(구현) / 테스트는 기존 파일 관례 따름.
- Firebase 엔드포인트 상수(확정):
  - 인증 signUp: `https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={apiKey}`
  - 토큰 갱신: `https://securetoken.googleapis.com/v1/token?key={apiKey}`
  - Firestore 문서 베이스: `https://firestore.googleapis.com/v1/projects/{projectId}/databases/(default)/documents`

---

## 파일 구조 (생성/수정 맵)

| 파일 | 책임 | 유형 |
|---|---|---|
| `Assets/_Lair/Scripts/Net/FirestoreJson.cs` | Firestore REST 타입 JSON(`{"stringValue":...}`) 빌드/파싱 정적 헬퍼 | 생성 |
| `Assets/_Lair/Scripts/Net/FirebaseApiClient.cs` | `ILairApiClient` 8개 메서드 Firebase REST 구현 + 정적 파서 | 생성 |
| `Assets/_Lair/Scripts/Net/NetworkConfig.cs` | `_baseUrl` → `_firebaseApiKey`+`_firebaseProjectId` | 수정 |
| `Assets/_Lair/Scripts/Net/AuthTokenStore.cs` | `long AccountId` → `string Uid` + `RefreshToken` 저장 | 수정 |
| `Assets/_Lair/Scripts/Net/NetDtos.cs` | `RankingRowDto.uid`(string) 추가 + Firebase auth 응답 DTO | 수정 |
| `Assets/_Lair/Scripts/Meta/MetaSession.Net.cs` | 조립 지점 교체 (`LairApiClient`→`FirebaseApiClient`) | 수정 |
| `Assets/_Lair/Scripts/UI/Village/RankingPopup.cs` | "내 행" 식별을 uid(string) 기준으로 | 수정 |
| `docs/design/firebase-security-rules.md` | Firestore 보안 규칙 + 데이터 모델 문서 | 생성 |
| `Assets/_Lair/Tests/EditMode/FirestoreJsonTests.cs` | `FirestoreJson` 라운드트립 테스트 | 생성 |
| `Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs` | 응답 파서 분기 테스트 | 생성 |
| CLAUDE.md §8 | 서버 근거 재작성(자체 서버→Firebase) | 수정 |

> `LairApiClient.cs` 는 삭제하지 않는다(Q4 — 사문화, 조립에서만 제외).

---

### Task 1: FirestoreJson 헬퍼 (타입 JSON 빌드/파싱)

Firestore REST 는 필드를 타입 래핑(`{"stringValue":"x"}`, `{"integerValue":"5"}`) 한다. `JsonUtility` 는 이 구조를 직접 다루기 번거로워 얇은 정적 헬퍼로 분리한다.

**Files:**
- Create: `Assets/_Lair/Scripts/Net/FirestoreJson.cs`
- Test: `Assets/_Lair/Tests/EditMode/FirestoreJsonTests.cs`

**Interfaces:**
- Produces:
  - `static string FirestoreJson.StringField(string value)` → `"{\"stringValue\":\"...\"}"` (이스케이프 처리)
  - `static string FirestoreJson.IntField(long value)` → `"{\"integerValue\":\"5\"}"`
  - `static string FirestoreJson.Document(params (string name, string valueJson)[] fields)` → `{"fields":{...}}`
  - `static string FirestoreJson.ExtractString(string documentJson, string fieldName)` → 필드의 stringValue (없으면 null)
  - `static long FirestoreJson.ExtractInt(string documentJson, string fieldName)` → integerValue (없으면 0)
  - `static string FirestoreJson.ExtractUpdateTime(string documentJson)` → 문서 `updateTime` (없으면 null)

- [ ] **Step 1: 실패 테스트 작성** — `FirestoreJsonTests.cs`

```csharp
using NUnit.Framework;
using Lair.Net;

public class FirestoreJsonTests
{
    [Test]
    public void 문자열_필드를_타입_JSON_으로_감싼다()
    {
        string json = FirestoreJson.StringField("영주 #A3F9");
        Assert.AreEqual("{\"stringValue\":\"영주 #A3F9\"}", json);
    }

    [Test]
    public void 정수_필드는_문자열_integerValue_로_직렬화된다()
    {
        Assert.AreEqual("{\"integerValue\":\"92500\"}", FirestoreJson.IntField(92500));
    }

    [Test]
    public void 문서에서_stringValue_를_추출한다()
    {
        string doc = "{\"name\":\"...\",\"fields\":{\"profile\":{\"stringValue\":\"HELLO\"}},\"updateTime\":\"2026-07-14T00:00:00Z\"}";
        Assert.AreEqual("HELLO", FirestoreJson.ExtractString(doc, "profile"));
        Assert.AreEqual("2026-07-14T00:00:00Z", FirestoreJson.ExtractUpdateTime(doc));
    }

    [Test]
    public void 없는_필드_추출은_null_또는_0()
    {
        Assert.IsNull(FirestoreJson.ExtractString("{\"fields\":{}}", "nope"));
        Assert.AreEqual(0, FirestoreJson.ExtractInt("{\"fields\":{}}", "nope"));
    }

    [Test]
    public void 큰따옴표와_역슬래시를_이스케이프한다()
    {
        string json = FirestoreJson.StringField("a\"b\\c");
        Assert.AreEqual("{\"stringValue\":\"a\\\"b\\\\c\"}", json);
    }
}
```

- [ ] **Step 2: 실패 확인** — Unity Test Runner(EditMode) 또는 `Lair/Test/RunEditMode` 에디터 메뉴 실행. Expected: 컴파일 실패(`FirestoreJson` 없음).

- [ ] **Step 3: 최소 구현** — `FirestoreJson.cs`

```csharp
using System.Text;
using System.Text.RegularExpressions;

namespace Lair.Net
{
    //# Firestore REST 타입 JSON(stringValue/integerValue) 빌드·파싱 헬퍼. HTTP 는 CHMHttpNetwork 담당.
    public static class FirestoreJson
    {
        public static string StringField(string value) => "{\"stringValue\":\"" + Escape(value) + "\"}";

        public static string IntField(long value) => "{\"integerValue\":\"" + value + "\"}";

        public static string Document(params (string name, string valueJson)[] fields)
        {
            StringBuilder sb = new StringBuilder("{\"fields\":{");
            for (int i = 0; i < fields.Length; i++)
            {
                if (i > 0)
                    sb.Append(',');
                sb.Append('"').Append(fields[i].name).Append("\":").Append(fields[i].valueJson);
            }
            sb.Append("}}");
            return sb.ToString();
        }

        //# "fieldName":{"stringValue":"..."} 패턴에서 값 추출. 없으면 null.
        public static string ExtractString(string documentJson, string fieldName)
        {
            Match m = Regex.Match(documentJson ?? string.Empty,
                "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\\{\\s*\"stringValue\"\\s*:\\s*\"((?:[^\"\\\\]|\\\\.)*)\"");
            return m.Success ? Unescape(m.Groups[1].Value) : null;
        }

        public static long ExtractInt(string documentJson, string fieldName)
        {
            Match m = Regex.Match(documentJson ?? string.Empty,
                "\"" + Regex.Escape(fieldName) + "\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"?(-?\\d+)\"?");
            return m.Success && long.TryParse(m.Groups[1].Value, out long v) ? v : 0;
        }

        public static string ExtractUpdateTime(string documentJson)
        {
            Match m = Regex.Match(documentJson ?? string.Empty, "\"updateTime\"\\s*:\\s*\"([^\"]+)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        private static string Escape(string s) => (s ?? string.Empty).Replace("\\", "\\\\").Replace("\"", "\\\"");
        private static string Unescape(string s) => (s ?? string.Empty).Replace("\\\"", "\"").Replace("\\\\", "\\");
    }
}
```

- [ ] **Step 4: 통과 확인** — EditMode 실행. Expected: 5 테스트 PASS.

- [ ] **Step 5: 커밋(안)** — Rule 01 준수, `git add` 까지.

```
git add Assets/_Lair/Scripts/Net/FirestoreJson.cs Assets/_Lair/Scripts/Net/FirestoreJson.cs.meta Assets/_Lair/Tests/EditMode/FirestoreJsonTests.cs Assets/_Lair/Tests/EditMode/FirestoreJsonTests.cs.meta
```
커밋 메시지(안): `# [feat] - Firestore 타입 JSON 빌드·파싱 헬퍼 추가`

---

### Task 2: NetworkConfig · AuthTokenStore Firebase 전환

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/NetworkConfig.cs`
- Modify: `Assets/_Lair/Scripts/Net/AuthTokenStore.cs` (**추가만** — 기존 `AccountId` 계열 제거 금지)
- Test: `Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs` (**기존 파일 — append**. 이미 `AccountId` 테스트가 있으니 아래 케이스만 추가)

**Interfaces:**
- Consumes: 없음
- Produces:
  - `NetworkConfig.FirebaseApiKey` (string), `NetworkConfig.FirebaseProjectId` (string), `NetworkConfig.TimeoutSec` (유지)
  - `AuthTokenStore.Uid` (string, get), `AuthTokenStore.SaveUid(string)`, `AuthTokenStore.HasUid` (bool)
  - `AuthTokenStore.RefreshToken` (string, get), `AuthTokenStore.SaveRefreshToken(string)`
  - `AuthTokenStore.Token`/`SaveToken`/`ClearToken`/`GetOrCreateDeviceId` (기존 유지 — Token 은 idToken 보관)
  - **`AuthTokenStore.AccountId`/`SaveAccountId`/`HasAccountId` 는 제거하지 않고 잔존**(사문화). `VillageController.cs:158`·기존 테스트·`LairApiClient` 가 참조하므로 제거 시 컴파일 파손.

- [ ] **Step 1: 실패 테스트 작성** — `AuthTokenStoreTests.cs` **에 아래 3개 메서드 append**(기존 클래스 본문에 추가; 기존 `AccountId` 테스트는 그대로 둔다)

```csharp
    [Test]
    public void Uid_저장후_조회된다()
    {
        AuthTokenStore.SaveUid("kZ9xAbC");
        Assert.AreEqual("kZ9xAbC", AuthTokenStore.Uid);
        Assert.IsTrue(AuthTokenStore.HasUid);
    }

    [Test]
    public void RefreshToken_저장후_조회된다()
    {
        AuthTokenStore.SaveRefreshToken("r-token-123");
        Assert.AreEqual("r-token-123", AuthTokenStore.RefreshToken);
    }

    [Test]
    public void 미설정_Uid_는_빈문자열이고_HasUid_false()
    {
        Assert.AreEqual(string.Empty, AuthTokenStore.Uid);
        Assert.IsFalse(AuthTokenStore.HasUid);
    }
```

> 기존 `AuthTokenStoreTests` 의 `[TearDown]` 이 `PlayerPrefs` 키를 지우는지 확인하고, 아니면 `Lair.Net.Uid`·`Lair.Net.RefreshToken` 키도 정리 대상에 추가.

- [ ] **Step 2: 실패 확인** — EditMode. Expected: 컴파일 실패(`Uid` 없음).

- [ ] **Step 3: 구현 — NetworkConfig.cs**

```csharp
using UnityEngine;

namespace Lair.Net
{
    //# Firebase 접속 설정 SO — Addressable(EData.NetworkConfig) 로 로드. (2026-07-14 Firebase 피벗)
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Lair/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [SerializeField] private string _firebaseApiKey = "";
        [SerializeField] private string _firebaseProjectId = "";
        [SerializeField] private int _timeoutSec = 10;

        public string FirebaseApiKey => _firebaseApiKey;
        public string FirebaseProjectId => _firebaseProjectId;
        public int TimeoutSec => _timeoutSec;
    }
}
```

- [ ] **Step 4: 구현 — AuthTokenStore.cs** (기존 멤버 **전부 유지** + `Uid`·`RefreshToken` **추가만**. `AccountId`/`SaveAccountId`/`HasAccountId` 는 그대로 둔다)

```csharp
        //# --- 아래 3블록을 기존 AuthTokenStore 클래스에 추가. 기존 DeviceId/Token/AccountId 멤버는 건드리지 않는다. ---
        private const string RefreshTokenKey = "Lair.Net.RefreshToken";
        private const string UidKey = "Lair.Net.Uid";               //# Firebase localId

        public static string RefreshToken => PlayerPrefs.GetString(RefreshTokenKey, string.Empty);
        public static void SaveRefreshToken(string t) { PlayerPrefs.SetString(RefreshTokenKey, t ?? string.Empty); PlayerPrefs.Save(); }

        public static string Uid => PlayerPrefs.GetString(UidKey, string.Empty);
        public static bool HasUid => string.IsNullOrEmpty(Uid) == false;
        public static void SaveUid(string uid) { PlayerPrefs.SetString(UidKey, uid ?? string.Empty); PlayerPrefs.Save(); }
```

> `Token` 은 이제 Firebase idToken 을 보관한다(의미만 바뀌고 시그니처 동일). 기존 `AccountId` 계열은 Firebase 에선 항상 0(아무도 set 안 함) 이지만 컴파일·기존 테스트 보존을 위해 잔존시킨다. 클래스 상단 주석에 `(2026-07-14 Firebase: idToken/uid/refreshToken 추가, AccountId 사문화)` 한 줄 보강.

- [ ] **Step 5: 통과 확인** — EditMode. Expected: 신규 3 테스트 PASS + 기존 `AccountId` 테스트 PASS + 전 회귀 PASS. `AccountId` 를 남겼으므로 `LairApiClient`·`VillageController` 임시 수정 불요(컴파일 정상).

- [ ] **Step 6: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/NetworkConfig.cs Assets/_Lair/Scripts/Net/AuthTokenStore.cs Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs
```
커밋 메시지(안): `# [refactor] - 접속 설정·자격증명 저장에 Firebase(apiKey·uid·refreshToken) 추가`
(주의: `AuthTokenStoreTests.cs` 는 기존 파일 수정이라 `.meta` 스테이징 제외 — Rule 01)

---

### Task 3: FirebaseApiClient — 익명 인증

**Files:**
- Create: `Assets/_Lair/Scripts/Net/FirebaseApiClient.cs` (auth 부분 + 골격)
- Modify: `Assets/_Lair/Scripts/Net/NetDtos.cs` (Firebase auth 응답 DTO 추가)
- Test: `Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs`

**Interfaces:**
- Consumes: `NetworkConfig.FirebaseApiKey/ProjectId/TimeoutSec`, `AuthTokenStore.*`, `FirestoreJson.*`
- Produces:
  - `class FirebaseApiClient : ILairApiClient`, 생성자 `FirebaseApiClient(NetworkConfig config)`
  - `static string FirebaseApiClient.ParseSignUpUid(string body)` / `ParseSignUpIdToken` / `ParseSignUpRefreshToken` (null 안전)
  - `Task<bool> AuthenticateAsync()` (Firebase 익명)

- [ ] **Step 1: 실패 테스트 작성** — `FirebaseApiClientParseTests.cs`

```csharp
using NUnit.Framework;
using Lair.Net;

public class FirebaseApiClientParseTests
{
    private const string SignUpBody =
        "{\"idToken\":\"eyJ.a.b\",\"refreshToken\":\"r123\",\"localId\":\"kZ9xAbC\",\"expiresIn\":\"3600\"}";

    [Test]
    public void signUp_응답에서_uid_idToken_refreshToken_추출()
    {
        Assert.AreEqual("kZ9xAbC", FirebaseApiClient.ParseSignUpUid(SignUpBody));
        Assert.AreEqual("eyJ.a.b", FirebaseApiClient.ParseSignUpIdToken(SignUpBody));
        Assert.AreEqual("r123", FirebaseApiClient.ParseSignUpRefreshToken(SignUpBody));
    }

    [Test]
    public void 빈_본문은_null_반환()
    {
        Assert.IsNull(FirebaseApiClient.ParseSignUpUid(""));
        Assert.IsNull(FirebaseApiClient.ParseSignUpUid(null));
    }
}
```

- [ ] **Step 2: 실패 확인** — EditMode. Expected: 컴파일 실패.

- [ ] **Step 3: DTO 추가 — NetDtos.cs** (파일 하단 append)

```csharp
    //# Firebase Auth REST accounts:signUp / securetoken 응답. JsonUtility 로 파싱.
    [Serializable]
    public class FirebaseAuthResponse
    {
        public string idToken;
        public string refreshToken;
        public string localId;   //# == uid (signUp)
        public string user_id;   //# == uid (securetoken 갱신 응답)
    }
```

- [ ] **Step 4: 구현 — FirebaseApiClient.cs** (auth + 골격; 나머지 메서드는 후속 Task 에서 채움 — 이 Step 은 미구현 메서드를 `throw new NotImplementedException()` 로 두지 않고, `ILairApiClient` 전체를 컴파일 가능한 최소 스텁으로 채운다)

```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# Firebase Auth + Firestore REST 로 ILairApiClient 를 구현. 모든 쓰기는 documents:commit(POST).
    public class FirebaseApiClient : ILairApiClient
    {
        private readonly NetworkConfig _config;
        private string _saveUpdateTime;   //# GetSave 시 캐시 — PutSave precondition(충돌 감지)용.

        public FirebaseApiClient(NetworkConfig config) { _config = config; }

        private int Timeout => _config.TimeoutSec;
        private string Key => _config.FirebaseApiKey;
        private string DocBase => $"https://firestore.googleapis.com/v1/projects/{_config.FirebaseProjectId}/databases/(default)/documents";

        public async Task<bool> AuthenticateAsync()
        {
            //# refreshToken 있으면 갱신 우선.
            if (string.IsNullOrEmpty(AuthTokenStore.RefreshToken) == false)
            {
                if (await RefreshAsync())
                    return true;
            }
            string url = $"https://identitytoolkit.googleapis.com/v1/accounts:signUp?key={Key}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, "{\"returnSecureToken\":true}", null, Timeout);
            if (res.IsSuccess == false)
            {
                Debug.LogWarning($"[FirebaseApiClient] 익명 인증 실패: {res.StatusCode} {res.Error}");
                return false;
            }
            string uid = ParseSignUpUid(res.Body);
            string idToken = ParseSignUpIdToken(res.Body);
            string refresh = ParseSignUpRefreshToken(res.Body);
            if (string.IsNullOrEmpty(uid) || string.IsNullOrEmpty(idToken))
                return false;
            AuthTokenStore.SaveUid(uid);
            AuthTokenStore.SaveToken(idToken);
            AuthTokenStore.SaveRefreshToken(refresh);
            return true;
        }

        private async Task<bool> RefreshAsync()
        {
            string url = $"https://securetoken.googleapis.com/v1/token?key={Key}";
            string body = $"grant_type=refresh_token&refresh_token={AuthTokenStore.RefreshToken}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, body, null, Timeout);
            if (res.IsSuccess == false)
                return false;
            FirebaseAuthResponse parsed = JsonUtility.FromJson<FirebaseAuthResponse>(res.Body);
            if (parsed == null || string.IsNullOrEmpty(parsed.idToken))
                return false;
            AuthTokenStore.SaveToken(parsed.idToken);
            if (string.IsNullOrEmpty(parsed.refreshToken) == false)
                AuthTokenStore.SaveRefreshToken(parsed.refreshToken);
            if (string.IsNullOrEmpty(parsed.user_id) == false)
                AuthTokenStore.SaveUid(parsed.user_id);
            return true;
        }

        public static string ParseSignUpUid(string body) => Field(body, "localId");
        public static string ParseSignUpIdToken(string body) => Field(body, "idToken");
        public static string ParseSignUpRefreshToken(string body) => Field(body, "refreshToken");

        private static string Field(string body, string name)
        {
            if (string.IsNullOrEmpty(body))
                return null;
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                body, "\"" + name + "\"\\s*:\\s*\"([^\"]*)\"");
            return m.Success ? m.Groups[1].Value : null;
        }

        //# --- 이하 후속 Task 에서 구현. 스텁(빈/기본 반환)으로 컴파일 유지. ---
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult<SaveResponseBody>(null);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt) => Task.FromResult(CloudSaveResult.Failed);
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(false);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(DisplayNameResult.Of(DisplayNameStatus.Offline));
    }
}
```

- [ ] **Step 5: 통과 확인** — EditMode. Expected: 2 신규 테스트 PASS + 기존 전 테스트 PASS(스텁이라 회귀 없음).

- [ ] **Step 6: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseApiClient.cs Assets/_Lair/Scripts/Net/FirebaseApiClient.cs.meta Assets/_Lair/Scripts/Net/NetDtos.cs Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs.meta
```
커밋 메시지(안): `# [feat] - Firebase 익명 인증(signUp·토큰갱신) 연동 추가`

---

### Task 4: FirebaseApiClient — 세이브 + 충돌(commit precondition)

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/FirebaseApiClient.cs` (`GetSaveAsync`/`PutSaveAsync` 실구현)
- Test: `Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs` (충돌 판정·profile 파싱 케이스 추가)

**Interfaces:**
- Consumes: `FirestoreJson.*`, `AuthTokenStore.Uid/Token`, `MetaProfile`
- Produces:
  - `static CloudSaveResult FirebaseApiClient.ClassifyCommit(long statusCode, string body)` — 충돌/성공/실패 분류
  - `static MetaProfile FirebaseApiClient.ParseSaveProfile(string documentJson)` — profile 문자열→MetaProfile (없으면 null)

- [ ] **Step 1: 실패 테스트 작성** (append)

```csharp
    [Test]
    public void commit_409_는_충돌()
        => Assert.AreEqual(CloudSaveResult.Conflict, FirebaseApiClient.ClassifyCommit(409, ""));

    [Test]
    public void commit_400_FAILED_PRECONDITION_은_충돌()
        => Assert.AreEqual(CloudSaveResult.Conflict,
            FirebaseApiClient.ClassifyCommit(400, "{\"error\":{\"status\":\"FAILED_PRECONDITION\"}}"));

    [Test]
    public void commit_200_은_성공()
        => Assert.AreEqual(CloudSaveResult.Success, FirebaseApiClient.ClassifyCommit(200, "{}"));

    [Test]
    public void commit_500_은_실패()
        => Assert.AreEqual(CloudSaveResult.Failed, FirebaseApiClient.ClassifyCommit(500, ""));

    [Test]
    public void 세이브_문서에서_profile_문자열을_MetaProfile_로_복원한다()
    {
        //# MetaProfile 최소 JSON 을 stringValue 로 감싼 Firestore 문서.
        string inner = "{\\\"Version\\\":3}";
        string doc = "{\"fields\":{\"profile\":{\"stringValue\":\"" + inner + "\"}}}";
        MetaProfile p = FirebaseApiClient.ParseSaveProfile(doc);
        Assert.IsNotNull(p);
        Assert.AreEqual(3, p.Version);
    }
```

- [ ] **Step 2: 실패 확인** — EditMode. Expected: 컴파일 실패.

- [ ] **Step 3: 구현** — `FirebaseApiClient.cs` 의 세이브 메서드 + 정적 분류기 교체

```csharp
        public async Task<SaveResponseBody> GetSaveAsync()
        {
            string url = $"{DocBase}/saves/{AuthTokenStore.Uid}";
            CHHttpResult res = await CHMHttpNetwork.GetAsync(url, AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
            {
                _saveUpdateTime = null;
                return null;
            }
            _saveUpdateTime = FirestoreJson.ExtractUpdateTime(res.Body);
            MetaProfile profile = ParseSaveProfile(res.Body);
            if (profile == null)
                return null;
            return new SaveResponseBody { profile = profile, schemaVersion = (int)FirestoreJson.ExtractInt(res.Body, "schemaVersion"), updatedAt = _saveUpdateTime };
        }

        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            string docPath = $"projects/{_config.FirebaseProjectId}/databases/(default)/documents/saves/{AuthTokenStore.Uid}";
            string fields = FirestoreJson.Document(
                ("profile", FirestoreJson.StringField(JsonUtility.ToJson(profile))),
                ("schemaVersion", FirestoreJson.IntField(profile.Version)),
                ("updatedAt", FirestoreJson.StringField(clientUpdatedAt)));
            //# precondition: 캐시된 updateTime 있으면 그 시점 기준, 없으면 최초 생성(exists=false).
            string precond = string.IsNullOrEmpty(_saveUpdateTime)
                ? "{\"exists\":false}"
                : "{\"updateTime\":\"" + _saveUpdateTime + "\"}";
            string commit = "{\"writes\":[{\"update\":{\"name\":\"" + docPath + "\"," + fields.Substring(1) + ",\"currentDocument\":" + precond + "}]}";
            //# 위 한 줄은 가독성이 낮으므로 실제 구현은 아래 BuildCommitBody 헬퍼로 분리(Step 3b).
            string url = $"{DocBase}:commit";
            CHHttpResult res = await CHMHttpNetwork.PostAsync(url, BuildSaveCommit(docPath, fields, precond), AuthTokenStore.Token, Timeout);
            return ClassifyCommit(res.StatusCode, res.Body);
        }

        //# :commit 본문 조립 — write 항목의 currentDocument precondition 은 write 형제로 들어간다.
        private static string BuildSaveCommit(string docPath, string fieldsJson, string precondJson)
        {
            //# fieldsJson = {"fields":{...}} → update 객체에 name + fields 병합.
            string fieldsInner = fieldsJson.Substring(1, fieldsJson.Length - 2); //# 겉 중괄호 제거 → "fields":{...}
            return "{\"writes\":[{\"update\":{\"name\":\"" + docPath + "\"," + fieldsInner + "},\"currentDocument\":" + precondJson + "}]}";
        }

        public static CloudSaveResult ClassifyCommit(long statusCode, string body)
        {
            if (statusCode == 409)
                return CloudSaveResult.Conflict;
            if (statusCode == 400 && (body ?? string.Empty).Contains("FAILED_PRECONDITION"))
                return CloudSaveResult.Conflict;
            if (statusCode >= 200 && statusCode < 300)
                return CloudSaveResult.Success;
            return CloudSaveResult.Failed;
        }

        public static MetaProfile ParseSaveProfile(string documentJson)
        {
            string profileJson = FirestoreJson.ExtractString(documentJson, "profile");
            if (string.IsNullOrEmpty(profileJson))
                return null;
            try { return JsonUtility.FromJson<MetaProfile>(profileJson); }
            catch (System.Exception e) { Debug.LogWarning($"[FirebaseApiClient] profile 파싱 실패: {e.Message}"); return null; }
        }
```

> **Step 3 주의**: 위 `PutSaveAsync` 본문의 첫 `commit`/주석 줄은 설명용이다. 구현 시 그 줄을 제거하고 `BuildSaveCommit(...)` 결과만 `PostAsync` 에 넘긴다(중복 조립 금지, DRY).

- [ ] **Step 4: 통과 확인** — EditMode. Expected: 신규 5 테스트 PASS + 기존 전 PASS.

- [ ] **Step 5: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseApiClient.cs Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs
```
커밋 메시지(안): `# [feat] - Firebase 세이브 백업/복원 + updateTime 충돌 감지 연동`

---

### Task 5: FirebaseApiClient — 리더보드 + uid 내행 식별

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/FirebaseApiClient.cs` (`SubmitScoreAsync`/`GetTopAsync`/`GetMyRankAsync`)
- Modify: `Assets/_Lair/Scripts/Net/NetDtos.cs` (`RankingRowDto.uid` 추가)
- Modify: `Assets/_Lair/Scripts/UI/Village/RankingPopup.cs` (uid 식별 — `RankingArg.MyUid` + `IsMyRow`/`PickMyRow`)
- Modify: `Assets/_Lair/Scripts/Village/VillageController.cs:158` (RankingArg 조립부에 `MyUid = AuthTokenStore.Uid` 추가)
- Test: `FirebaseApiClientParseTests.cs` (runQuery/aggregation 파싱 케이스)

**Interfaces:**
- Consumes: `FirestoreJson.*`, `AuthTokenStore.Uid`
- Produces:
  - `static List<RankingRowDto> FirebaseApiClient.ParseRunQueryRows(string body)` — runQuery 응답 배열→행 리스트
  - `static long FirebaseApiClient.ParseAggregationCount(string body)` — runAggregationQuery COUNT 결과
  - `RankingRowDto.uid` (string) 필드

- [ ] **Step 1: 실패 테스트 작성** (append)

```csharp
    [Test]
    public void runQuery_응답을_행리스트로_파싱한다()
    {
        string body =
          "[{\"document\":{\"fields\":{\"uid\":{\"stringValue\":\"u1\"},\"displayName\":{\"stringValue\":\"영주 #A3F9\"},\"clearTimeMs\":{\"integerValue\":\"92500\"},\"hero\":{\"stringValue\":\"Knight\"}}}}," +
          "{\"document\":{\"fields\":{\"uid\":{\"stringValue\":\"u2\"},\"displayName\":{\"stringValue\":\"영주 #B1C2\"},\"clearTimeMs\":{\"integerValue\":\"93000\"},\"hero\":{\"stringValue\":\"Knight\"}}}}]";
        var rows = FirebaseApiClient.ParseRunQueryRows(body);
        Assert.AreEqual(2, rows.Count);
        Assert.AreEqual("u1", rows[0].uid);
        Assert.AreEqual(92500, rows[0].clearTimeMs);
        Assert.AreEqual("영주 #A3F9", rows[0].displayName);
    }

    [Test]
    public void aggregation_COUNT_결과를_파싱한다()
    {
        string body = "[{\"result\":{\"aggregateFields\":{\"count\":{\"integerValue\":\"7\"}}}}]";
        Assert.AreEqual(7, FirebaseApiClient.ParseAggregationCount(body));
    }
```

- [ ] **Step 2: 실패 확인** — EditMode.

- [ ] **Step 3: DTO — NetDtos.cs `RankingRowDto` 에 uid 추가**

```csharp
    [Serializable]
    public class RankingRowDto
    {
        public long rank;
        public string displayName;
        public int clearTimeMs;
        public string hero;
        public long accountId;   //# (사문화 — 구서버 하위호환. Firebase 는 uid 사용)
        public string uid;       //# Firebase 계정 식별자 — "내 행" 매칭 키(2026-07-14)
    }
```

- [ ] **Step 4: 구현 — 리더보드 메서드**

```csharp
        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            string docPath = $"projects/{_config.FirebaseProjectId}/databases/(default)/documents/leaderboard/{AuthTokenStore.Uid}";
            string fields = FirestoreJson.Document(
                ("uid", FirestoreJson.StringField(AuthTokenStore.Uid)),
                ("displayName", FirestoreJson.StringField(displayName)),
                ("clearTimeMs", FirestoreJson.IntField(clearTimeMs)),
                ("hero", FirestoreJson.StringField(hero)));
            string commit = "{\"writes\":[{\"update\":{\"name\":\"" + docPath + "\"," + fields.Substring(1, fields.Length - 2) + "}}]}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:commit", commit, AuthTokenStore.Token, Timeout);
            return res.IsSuccess;
        }

        public async Task<List<RankingRowDto>> GetTopAsync(int top)
        {
            string query = "{\"structuredQuery\":{\"from\":[{\"collectionId\":\"leaderboard\"}],\"orderBy\":[{\"field\":{\"fieldPath\":\"clearTimeMs\"},\"direction\":\"ASCENDING\"}],\"limit\":" + top + "}}";
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:runQuery", query, AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
                return new List<RankingRowDto>();
            return ParseRunQueryRows(res.Body);
        }

        public async Task<List<RankingRowDto>> GetMyRankAsync()
        {
            //# 내 기록 조회 → COUNT(clearTimeMs < 내기록) → 등수 = count+1. 실패 시 빈 리스트.
            CHHttpResult mine = await CHMHttpNetwork.GetAsync($"{DocBase}/leaderboard/{AuthTokenStore.Uid}", AuthTokenStore.Token, Timeout);
            if (mine.IsSuccess == false)
                return new List<RankingRowDto>();
            long myMs = FirestoreJson.ExtractInt(mine.Body, "clearTimeMs");
            string agg = "{\"structuredAggregationQuery\":{\"aggregations\":[{\"count\":{},\"alias\":\"count\"}],\"structuredQuery\":{\"from\":[{\"collectionId\":\"leaderboard\"}],\"where\":{\"fieldFilter\":{\"field\":{\"fieldPath\":\"clearTimeMs\"},\"op\":\"LESS_THAN\",\"value\":{\"integerValue\":\"" + myMs + "\"}}}}}}";
            CHHttpResult cnt = await CHMHttpNetwork.PostAsync($"{DocBase}:runAggregationQuery", agg, AuthTokenStore.Token, Timeout);
            long rank = ParseAggregationCount(cnt.Body) + 1;
            RankingRowDto myRow = new RankingRowDto
            {
                rank = rank,
                uid = AuthTokenStore.Uid,
                displayName = FirestoreJson.ExtractString(mine.Body, "displayName"),
                clearTimeMs = (int)myMs,
                hero = FirestoreJson.ExtractString(mine.Body, "hero"),
            };
            return new List<RankingRowDto> { myRow };
        }

        public static List<RankingRowDto> ParseRunQueryRows(string body)
        {
            List<RankingRowDto> rows = new List<RankingRowDto>();
            if (string.IsNullOrEmpty(body))
                return rows;
            //# runQuery 는 [{document:{fields:{...}}}, ...] 배열. document 블록별로 분해.
            foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(body, "\"document\"\\s*:\\s*(\\{.*?\\}\\s*\\}\\s*\\})"))
            {
                string doc = m.Groups[1].Value;
                rows.Add(new RankingRowDto
                {
                    uid = FirestoreJson.ExtractString(doc, "uid"),
                    displayName = FirestoreJson.ExtractString(doc, "displayName"),
                    clearTimeMs = (int)FirestoreJson.ExtractInt(doc, "clearTimeMs"),
                    hero = FirestoreJson.ExtractString(doc, "hero"),
                });
            }
            return rows;
        }

        public static long ParseAggregationCount(string body)
        {
            System.Text.RegularExpressions.Match m = System.Text.RegularExpressions.Regex.Match(
                body ?? string.Empty, "\"count\"\\s*:\\s*\\{\\s*\"integerValue\"\\s*:\\s*\"?(\\d+)\"?");
            return m.Success && long.TryParse(m.Groups[1].Value, out long v) ? v : 0;
        }
```

> **Step 4 주의**: `ParseRunQueryRows` 의 정규식은 단순 케이스용이다. 중첩 `}` 로 취약할 수 있으므로, 구현 시 문서 경계는 `"document":` 오프셋부터 매칭하되 실패해도 빈 리스트로 흐름을 막지 않는다(기획서 §6). 테스트(Step 1)의 2행 케이스를 반드시 통과시키고, 통합 검증은 실기기 단계로 미룬다.

- [ ] **Step 5: 구현 — RankingPopup.cs uid 식별** (기존 `long MyAccountId` 경로 앞에 uid 1순위 추가, 폴백 보존)

정확한 편집 3지점:
1. `RankingArg`(RankingPopup.cs 상단, 현 `MyAccountId`/`MyBestClearTime` 옆)에 `public string MyUid;` 추가.
2. `IsMyRow`/`PickMyRow` 시그니처에 `string myUid` 파라미터 추가하고, 매칭 최상단에 uid 1순위 분기:
```csharp
        //# uid 1순위(양쪽 존재 시 권위 키). 없으면 기존 accountId → clearTimeMs 폴백 순서 유지.
        if (string.IsNullOrEmpty(myUid) == false && string.IsNullOrEmpty(row.uid) == false)
            return row.uid == myUid;
```
   `PickMyRow` 도 동일하게 uid 일치 우선 → accountId → 시간 → 첫 행 순.
3. 호출부(`RankingPopup` 의 매핑 루프, 현 `IsMyRow(row, myAccountId, myClearMs, ...)`)에 `arg.MyUid` 를 넘기도록 인자 추가.
4. **VillageController.cs:158** — RankingArg 조립부에 `MyUid = AuthTokenStore.Uid,` 한 줄 추가(기존 `MyAccountId = AuthTokenStore.AccountId,` 유지).

- [ ] **Step 6: 통과 확인** — EditMode. Expected: 신규 테스트 PASS + 기존 RankingPopup 관련 테스트 PASS(폴백 보존).

- [ ] **Step 7: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseApiClient.cs Assets/_Lair/Scripts/Net/NetDtos.cs Assets/_Lair/Scripts/UI/Village/RankingPopup.cs Assets/_Lair/Scripts/Village/VillageController.cs Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs
```
커밋 메시지(안): `# [feat] - Firebase 리더보드 제출·Top·내순위(절대등수) + uid 내행 식별`

---

### Task 6: FirebaseApiClient — 표시명 유일성(commit 트랜잭션)

**Files:**
- Modify: `Assets/_Lair/Scripts/Net/FirebaseApiClient.cs` (`ChangeDisplayNameAsync`)
- Test: `FirebaseApiClientParseTests.cs` (상태코드→DisplayNameStatus 분류)

**Interfaces:**
- Produces: `static DisplayNameStatus FirebaseApiClient.ClassifyDisplayName(long statusCode, string body)`

- [ ] **Step 1: 실패 테스트 작성** (append)

```csharp
    [Test]
    public void 표시명_commit_409_또는_400FAILEDPRECONDITION_는_Taken()
    {
        Assert.AreEqual(DisplayNameStatus.Taken, FirebaseApiClient.ClassifyDisplayName(409, ""));
        Assert.AreEqual(DisplayNameStatus.Taken, FirebaseApiClient.ClassifyDisplayName(400, "FAILED_PRECONDITION"));
    }

    [Test]
    public void 표시명_commit_200_은_Success()
        => Assert.AreEqual(DisplayNameStatus.Success, FirebaseApiClient.ClassifyDisplayName(200, "{}"));

    [Test]
    public void 표시명_commit_기타_400_은_Invalid_5xx0_은_Offline()
    {
        Assert.AreEqual(DisplayNameStatus.Invalid, FirebaseApiClient.ClassifyDisplayName(400, "{\"error\":{\"status\":\"INVALID_ARGUMENT\"}}"));
        Assert.AreEqual(DisplayNameStatus.Offline, FirebaseApiClient.ClassifyDisplayName(0, ""));
    }
```

- [ ] **Step 2: 실패 확인** — EditMode.

- [ ] **Step 3: 구현** — `ChangeDisplayNameAsync` + 분류기

```csharp
        public async Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName)
        {
            string uid = AuthTokenStore.Uid;
            string norm = displayName == null ? "" : displayName.Trim();
            string prj = _config.FirebaseProjectId;
            string newLock = $"projects/{prj}/databases/(default)/documents/displayNames/{norm}";
            string lbPath  = $"projects/{prj}/databases/(default)/documents/leaderboard/{uid}";
            //# writes: (1) 새 이름 잠금 생성(exists=false → 중복이면 실패) (2) 리더보드 displayName 갱신.
            //# 옛 이름 삭제는 로컬에 직전 이름을 모를 수 있어 생략 가능(잔여 잠금은 무해) — 유일성 보장은 (1)이 담당.
            string body =
                "{\"writes\":[" +
                "{\"update\":{\"name\":\"" + newLock + "\"," + FirestoreJson.Document(("uid", FirestoreJson.StringField(uid))).Substring(1, FirestoreJson.Document(("uid", FirestoreJson.StringField(uid))).Length - 2) + "},\"currentDocument\":{\"exists\":false}}," +
                "{\"update\":{\"name\":\"" + lbPath + "\"," + FirestoreJson.Document(("displayName", FirestoreJson.StringField(norm))).Substring(1, FirestoreJson.Document(("displayName", FirestoreJson.StringField(norm))).Length - 2) + "}}" +
                "]}";
            if (string.IsNullOrEmpty(norm))
                return DisplayNameResult.Of(DisplayNameStatus.Invalid);
            CHHttpResult res = await CHMHttpNetwork.PostAsync($"{DocBase}:commit", body, AuthTokenStore.Token, Timeout);
            DisplayNameStatus status = ClassifyDisplayName(res.StatusCode, res.Body);
            return status == DisplayNameStatus.Success ? new DisplayNameResult(status, norm) : DisplayNameResult.Of(status);
        }

        public static DisplayNameStatus ClassifyDisplayName(long statusCode, string body)
        {
            if (statusCode == 409 || (statusCode == 400 && (body ?? "").Contains("FAILED_PRECONDITION")))
                return DisplayNameStatus.Taken;
            if (statusCode >= 200 && statusCode < 300)
                return DisplayNameStatus.Success;
            if (statusCode == 400)
                return DisplayNameStatus.Invalid;
            return DisplayNameStatus.Offline;
        }
```

> **Step 3 주의(DRY)**: 위 body 조립의 `FirestoreJson.Document(...).Substring(...)` 중복 호출은 가독성용 인라인이다. 구현 시 지역 변수(`string lockFields = ...; string lbFields = ...;`)로 1회 계산해 재사용한다.

- [ ] **Step 4: 통과 확인** — EditMode. Expected: 신규 4 assert PASS + 기존 전 PASS.

- [ ] **Step 5: 커밋(안)**

```
git add Assets/_Lair/Scripts/Net/FirebaseApiClient.cs Assets/_Lair/Tests/EditMode/FirebaseApiClientParseTests.cs
```
커밋 메시지(안): `# [feat] - Firebase 표시명 변경(잠금 문서로 유일성 보장) 연동`

---

### Task 7: 조립 교체 + 회귀 게이트 + 문서화

**Files:**
- Modify: `Assets/_Lair/Scripts/Meta/MetaSession.Net.cs:45`
- Create: `docs/design/firebase-security-rules.md`
- Modify: `CLAUDE.md` (§8)

**Interfaces:**
- Consumes: `FirebaseApiClient(NetworkConfig)`

- [ ] **Step 1: 조립 교체** — `MetaSession.Net.cs`

```csharp
            //# (기존) Api = new LairApiClient(config);
            Api = new FirebaseApiClient(config);
```

- [ ] **Step 2: 전체 EditMode 회귀 실행** — Unity Test Runner EditMode 전체. Expected: **기존 `FakeLairApiClient` 기반 테스트(CloudSaveServiceTests·NetDtoMappingTests·MetaProfile* 등) 전부 PASS** + 신규 테스트 전부 PASS. 인터페이스 불변이므로 회귀 0 이어야 한다. 실패 시 해당 Task 로 돌아가 수정.

- [ ] **Step 3: 보안 규칙 문서 작성** — `docs/design/firebase-security-rules.md`

```markdown
# Firestore 보안 규칙 · 데이터 모델 (Spec 1)

## 데이터 모델
- saves/{uid}: profile(string, MetaProfile JSON), schemaVersion(int), updatedAt(timestamp)
- leaderboard/{uid}: uid, displayName, clearTimeMs(int), hero, createdAt
- displayNames/{name}: uid

## 규칙 (Firebase 콘솔에 등록 — 이 레포는 문서만 보관)
service cloud.firestore {
  match /databases/{database}/documents {
    match /saves/{uid}        { allow read, write: if request.auth.uid == uid; }
    match /leaderboard/{uid}  { allow read: if true;
                                allow write: if request.auth.uid == uid; }
    match /displayNames/{name}{ allow read: if true;
                                allow create: if request.auth.uid == request.resource.data.uid;
                                allow delete: if request.auth.uid == resource.data.uid; }
  }
}

## 알려진 한계
- leaderboard 는 본인 uid 문서에 임의 clearTimeMs 를 쓸 수 있어 치팅 가능(v0.3 범위 밖, spec §7).
- 익명 인증은 기기이전 불가 — 계정 연동은 Spec 2.
```

- [ ] **Step 4: CLAUDE.md §8 + §2 + §9 재작성** (기획서 `docs/design/firebase-backend-pivot.md` §3 의 확정 문안 그대로 반영) — §8 만 바꾸면 §2(현재 단계)·§9(절대 금지)가 옛 백엔드를 근거로 남아 자기모순이 되므로 3곳을 함께 손댄다:
  - **§8** — 서버 연동 근거를 "자체 서버(ASP.NET Core+MySQL+Redis)"→"Firebase BaaS(Firebase Auth+Cloud Firestore, REST over `CHMHttpNetwork`)"로 교체. `Project_Lair_Server` 폐기·데이터 마이그레이션 없음 명시. 세이브 충돌을 "클라이언트가 감지, 기존 충돌 UX 로 처리"로 트림. 구글 로그인(계정 연동) 승격 예고(Spec 2, 본 Spec 1 미포함) 1줄.
  - **§2 (현재 단계)** — v0.3 서버 구현이 "별도 레포 `Project_Lair_Server` 소관"이라는 지칭을 Firebase BaaS 근거로 리워드. (연동 클라 코드만 이 레포에 둔다는 원칙 자체는 유지 — Firebase 규칙은 콘솔, Cloud Functions 는 범위 밖.)
  - **§9 (절대 금지)** — "서버(백엔드) 구현을 이 Unity 레포에서 작성 금지 — 별도 레포 `Project_Lair_Server` 소관" 의 **레포 지칭만** 리워드(백엔드=Firebase 콘솔/규칙·Cloud Functions). **금지 규칙 자체는 유지**(이 레포엔 여전히 연동 클라 코드만).
  - > 이 3곳 동시 리워드가 기획서 §3 이 플래그한 "인접 정합" 요구다 — plan delta 반영 완료 표시.

- [ ] **Step 5: 커밋(안)**

```
git add Assets/_Lair/Scripts/Meta/MetaSession.Net.cs docs/design/firebase-security-rules.md docs/design/firebase-security-rules.md.meta CLAUDE.md
```
커밋 메시지(안): `# [feat] - 백엔드를 Firebase 로 일원화(조립 교체) + 보안규칙 문서·§8 갱신`

---

## Self-Review

**1. Spec coverage** (spec 각 절 → task 매핑):
- §4 아키텍처/변경반경 → Task 3(신규 client)·Task 7(조립)·Task 2(config/store)·Task 5(RankingRowDto/Popup). ✓
- §5 데이터 모델 → Task 4(saves)·Task 5(leaderboard)·Task 6(displayNames) + Task 7 문서. ✓
- §6 8개 메서드 매핑 → 인증 T3 / 세이브 T4 / 리더보드 T5 / 표시명 T6. `:commit` 쓰기 우회 = T4·T5·T6 공통. ✓
- §6 충돌 상태코드 방어 매핑 → Task 4 `ClassifyCommit`(409 ‖ 400+FAILED_PRECONDITION). ✓
- §7 보안 규칙 → Task 7 문서. ✓
- §8 설정/§8 재작성 → Task 2(config)·Task 7(CLAUDE.md). ✓
- §9 한계·§10 테스트 방향(FakeLairApiClient 회귀) → Task 7 Step 2 회귀 게이트 + 각 Task 정적 파서 테스트. ✓

**2. Placeholder scan**: "적절히/TODO/나중에" 없음. 각 code step 에 실제 코드. `:commit`/precondition/aggregation JSON 은 리터럴로 기재. RankingPopup uid 편집은 정확 지점(IsMyRow/PickMyRow/RankingArg)·순서(uid→accountId→시간) 명시. `PutSaveAsync`/`ChangeDisplayNameAsync` 의 인라인 조립은 "구현 시 지역변수로 DRY" 주의 명시.

**3. Type consistency**: `FirestoreJson.StringField/IntField/Document/ExtractString/ExtractInt/ExtractUpdateTime` — Task1 정의, Task4·5·6 사용 일치. `FirebaseApiClient.ClassifyCommit`(CloudSaveResult)·`ClassifyDisplayName`(DisplayNameStatus)·`ParseRunQueryRows`(List<RankingRowDto>)·`ParseAggregationCount`(long) — 반환 타입 정의=사용 일치. `AuthTokenStore.Uid/SaveUid/HasUid/RefreshToken/SaveRefreshToken` — Task2 정의, Task3·4·5·6 사용 일치. `RankingRowDto.uid`(string) — Task5 정의, RankingPopup 사용 일치.

**주의/리스크(구현자 유의)**: 정규식 기반 파싱은 Firestore 응답의 실제 형태에 따라 취약할 수 있다. 각 파서는 실패해도 빈/기본값으로 흐름을 막지 않으며(기획서 §6), 실 통신 통합 검증은 실기기/에뮬레이터 단계(Spec 1 범위 밖의 QA)로 미룬다. EditMode 는 파싱 계약만 고정한다.
