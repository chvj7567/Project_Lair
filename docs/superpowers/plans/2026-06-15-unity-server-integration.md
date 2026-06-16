# Unity ↔ 서버 연동 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Unity 클라이언트에서 Project Lair 서버에 붙는 연동 코드 — 익명 인증·클라우드 세이브(자동 백업+수동 복원)·최단클리어 리더보드(제출+조회 화면).

**Architecture:** ChvjPackage 에 범용 `CHMHttpNetwork`(UnityWebRequest async HTTP)를 추가하고, 게임 측 `Lair.Net` 에 도메인 API 클라이언트·서비스를 둔다. 서비스는 `ILairApiClient` 추상화 뒤에서 동작해 EditMode 에서 가짜 client 로 단위 테스트한다. 기존 `MetaSession`/`VillageController`/`BattleController.EndBattle` 에 best-effort 훅을 끼운다.

**Tech Stack:** Unity 6 (.NET), C#, UnityWebRequest, JsonUtility, ChvjPackage(CHMHttpNetwork/CHMUI/CHMResource/CHPoolingScrollView/CHText/CHButton), NUnit EditMode.

**Spec:** `docs/superpowers/specs/2026-06-15-unity-server-integration-design.md`
**기획서(도메인 SoT):** `docs/design/unity-server-integration.md` — UX·문구·레이아웃은 기획서가 단일 진실.

> ## ⚠️ 기획서 Delta — 우선 적용 (아래 Task 본문보다 우선)
>
> 이 plan 은 brainstorm 직후 작성됐고, 이후 game-designer 기획서가 도메인을 확정했다. **아래 항목은 Task 본문의 구버전 값을 대체한다. 충돌 시 기획서(특히 §7)가 우선.** 전체 명세는 기획서 §1~§7 참조.
>
> 1. **표시명**: plan 의 임시값 `"Lord-" + deviceId.Substring(0,4)`(Task 6 Step 3) → **`MetaProfile.DisplayName` 우선, 없으면 `"영주 #" + deviceId.Substring(0,4).ToUpperInvariant()`**. `MetaProfile` 에 `public string DisplayName;` 신규 필드 추가(로컬 전용 — 서버 `MetaProfileDto` 에 없어 클라우드로 안 돌아옴, 기획서 §1·§7).
> 2. **EUI append 추가**(Task 7 Step 1 의 LeaderboardPopup 에 더해): `CloudPopup`, `ConfirmPopup`, `ToastView` — 순서대로 맨 끝 append.
> 3. **클라우드 복원**: plan 의 "단독 복원 버튼"(Task 7 Step 6) → **신규 `CloudPopup`** 으로 통합(복원·표시명 변경·충돌 권유 흡수, 기획서 §5). VillageHud 메뉴 6→8(`순위표`·`클라우드`).
> 4. **409 충돌**: plan 의 "로그만"(Task 6 Step 2) → **`MetaSession.CloudConflictPending=true` set + 클라우드 메뉴 빨간 dot 배지 + 진입 시 복원 권유(ConfirmPopup)**. 세션 가드 `MetaSession.CloudConflictPromptShownThisSession`. (기획서 §3)
> 5. **공용 팝업 신규**: `ConfirmPopup`(+`ConfirmPopupArg{Title,Message,ConfirmLabel,CancelLabel,OnConfirm,OnCancel}`) + `ToastView`(정적 `ToastView.Show(string)`, 하단중앙 1.8초 페이드, 입력통과). 프리팹 5종(LeaderboardPopup·LeaderboardCell·CloudPopup·ConfirmPopup·ToastView) 빌더 반영. (기획서 §2·§6·§7)
> 6. **내 순위 식별**: 리더보드 "내 행"은 displayName 매칭이 아니라 **accountId 매칭**. → `AuthTokenStore` 가 인증 응답의 `accountId` 도 저장해야 함(현재 토큰만 저장). `LairApiClient.AuthenticateAsync` 에서 `AuthTokenStore.SaveAccountId(parsed.accountId)` 추가. fallback: BestClearTime 일치 행. (기획서 §4·§8)
> 7. **복원 검사 순서**: `복원` 시 먼저 `GET /save` 존재 확인 → 없으면(404/null) 확인 다이얼로그 없이 "데이터 없음" 토스트만 → 있을 때만 ConfirmPopup. (기획서 §2·§7)
> 8. **문구**: 모든 사용자 노출 문구는 기획서 §7 "문구 일람" 이 단일 진실(Task 7 의 `_emptyText` 등 plan 문구 대체).
> 9. **CLAUDE.md §8**: "리더보드 UI 신규 화면은 이후 단계" → "리더보드 조회 화면은 v0.3 포함"(기획서 §0). 구현 시 함께 정리.

> **네이밍 갱신 (2026-06-15, 구현 후)**: 아래 본문의 `Leaderboard*` 클라 식별자·파일·`EUI.LeaderboardPopup`·플레이어 "순위표/리더보드" 표기는 모두 **`Ranking*` / "랭킹"** 으로 리네임됨(`RankingClient`·`RankingPopup`·`RankingCell`·`RankingPoolingScrollView`·`RankingRowDto`·`RankingRowListWrapper`·`MetaSession.Ranking`·`_rankingButton`·`EUI.RankingPopup`, 프리팹 `RankingPopup.prefab`/`RankingCell.prefab` + Addressable 주소). **단 HTTP 경로 `/leaderboard/...` 는 서버 계약이라 불변.** Cloud 버튼/팝업의 플레이어 라벨만 "계정"(Cloud 코드 식별자는 유지).

**준수:** Rule 00~04 — `//#` 한글 주석, `var` 금지·명시 타입, 가드절(중괄호 없이 개행), `!` 금지(`== false`/`== null`), MVVM, 에셋 Enum 키, UI 래퍼(CHText/CHButton), CHMPool, 한글 테스트 메서드명. 서버 구현 변경 금지(§9).

---

## File Structure

```
Packages/com.chvj.unityinfra/Runtime/Network/
  CHMHttpNetwork.cs            범용 async HTTP (GET/POST/PUT JSON, Bearer, 타임아웃) + CHHttpResult

Assets/_Lair/Scripts/Net/
  NetworkConfig.cs         ScriptableObject — baseUrl/timeout
  NetDtos.cs               서버 계약 DTO (AnonymousAuthResponse, PutSaveRequestBody, SaveResponseBody, SubmitScoreRequestBody, SubmitScoreResponseBody, LeaderboardRowDto, LeaderboardRowListWrapper)
  AuthTokenStore.cs        deviceId(GUID 1회)+JWT PlayerPrefs 저장
  ILairApiClient.cs        엔드포인트 추상화 (CommonInterface 성격, 단일 파일)
  LairApiClient.cs         CHMHttpNetwork 기반 구현
  CloudSaveService.cs      백업/복원/409 (MetaProfile ↔ JSON)
  LeaderboardClient.cs     제출 + Top/내순위 조회

Assets/_Lair/Scripts/UI/Village/
  LeaderboardPopup.cs            UIBase + LeaderboardPopupArg
  LeaderboardCell.cs             셀 (순위/이름/시간/영웅)
  LeaderboardPoolingScrollView.cs  CHPoolingScrollView<LeaderboardCell, LeaderboardRowDto>

Assets/_Lair/Scripts/Data/CommonEnum.cs        (EUI.LeaderboardPopup, EData.NetworkConfig append)
Assets/_Lair/Scripts/Meta/MetaSession.cs       (clientUpdatedAt 보관 + 동기 헬퍼 훅)
Assets/_Lair/Scripts/Village/VillageController.cs  (자동 백업 + 클라우드 복원 메뉴 + ensure-auth)
Assets/_Lair/Scripts/Battle/BattleController.cs    (EndBattle Win 시 리더보드 제출)

Assets/_Lair/Tests/EditMode/
  NetDtoMappingTests.cs    MetaProfile↔JSON 라운드트립
  AuthTokenStoreTests.cs   deviceId/JWT 저장
  CloudSaveServiceTests.cs 백업/복원/409 (FakeLairApiClient)
  LeaderboardClientTests.cs 제출/조회 (FakeLairApiClient)
  FakeLairApiClient.cs     테스트용 ILairApiClient 구현
```

Spec → 태스크 매핑: §3 CHMHttpNetwork→T1 · §3 Config/Auth/Client→T2,T3 · §5 인증→T3,T6 · §6 세이브→T4,T6 · §7 리더보드→T5,T6,T7 · §4 DTO→T2 · §10 테스트→T2,T4,T5,T8.

---

## Task 1: ChvjPackage CHMHttpNetwork (범용 async HTTP)

**Files:**
- Create: `Packages/com.chvj.unityinfra/Runtime/Network/CHMHttpNetwork.cs`

CHMHttpNetwork 는 UnityWebRequest 를 감싼 범용 HTTP 다. 게임 타입을 모른다(Rule 03 §1 의존 방향 준수). 실제 서버 호출이라 EditMode 단위 테스트 대상이 아니다 — 상위 서비스가 `ILairApiClient` 모킹으로 테스트된다. 빌드 컴파일로만 검증.

- [ ] **Step 1: CHMHttpNetwork + 결과 타입 작성**

Create `Packages/com.chvj.unityinfra/Runtime/Network/CHMHttpNetwork.cs`:
```csharp
using System;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace ChvjUnityInfra
{
    //# HTTP 응답 결과 — 예외 throw 대신 값으로 성공/상태/본문 전달.
    public struct CHHttpResult
    {
        public bool IsSuccess;       //# 2xx 여부
        public long StatusCode;      //# HTTP 상태 (네트워크 에러면 0)
        public string Body;          //# 응답 본문(텍스트)
        public string Error;         //# 네트워크/프로토콜 에러 메시지(없으면 null)

        public bool IsConflict => StatusCode == 409;
        public bool IsNotFound => StatusCode == 404;
        public bool IsUnauthorized => StatusCode == 401;
    }

    //# 범용 async HTTP 래퍼. 게임 도메인 비종속(Rule 03 §1). UnityWebRequest 기반.
    public static class CHMHttpNetwork
    {
        public static int DefaultTimeoutSec = 10;

        public static Task<CHHttpResult> GetAsync(string url, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbGET, url, null, bearer, timeoutSec);

        public static Task<CHHttpResult> PostAsync(string url, string jsonBody, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbPOST, url, jsonBody, bearer, timeoutSec);

        public static Task<CHHttpResult> PutAsync(string url, string jsonBody, string bearer = null, int? timeoutSec = null)
            => SendAsync(UnityWebRequest.kHttpVerbPUT, url, jsonBody, bearer, timeoutSec);

        private static Task<CHHttpResult> SendAsync(string verb, string url, string jsonBody, string bearer, int? timeoutSec)
        {
            TaskCompletionSource<CHHttpResult> tcs = new TaskCompletionSource<CHHttpResult>();

            UnityWebRequest request = new UnityWebRequest(url, verb);
            if (string.IsNullOrEmpty(jsonBody) == false)
            {
                byte[] payload = Encoding.UTF8.GetBytes(jsonBody);
                request.uploadHandler = new UploadHandlerRaw(payload);
            }
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            if (string.IsNullOrEmpty(bearer) == false)
                request.SetRequestHeader("Authorization", $"Bearer {bearer}");
            request.timeout = timeoutSec ?? DefaultTimeoutSec;

            UnityWebRequestAsyncOperation op = request.SendWebRequest();
            op.completed += _ =>
            {
                CHHttpResult result = new CHHttpResult
                {
                    StatusCode = request.responseCode,
                    Body = request.downloadHandler != null ? request.downloadHandler.text : null,
                };
                //# ConnectionError/DataProcessingError 는 네트워크 실패. ProtocolError(4xx/5xx)는 상태코드로 전달.
                if (request.result == UnityWebRequest.Result.ConnectionError
                    || request.result == UnityWebRequest.Result.DataProcessingError)
                {
                    result.IsSuccess = false;
                    result.Error = request.error;
                }
                else
                {
                    result.IsSuccess = request.responseCode >= 200 && request.responseCode < 300;
                    if (result.IsSuccess == false)
                        result.Error = request.error;
                }
                request.Dispose();
                tcs.TrySetResult(result);
            };

            return tcs.Task;
        }
    }
}
```

- [ ] **Step 2: 컴파일 확인**

UnityMCP/에디터에서 컴파일 0 에러 확인 (`Packages/.../Network/CHMHttpNetwork.cs` 인식). asmdef 변경 불필요(패키지 Runtime asmdef 에 포함).

- [ ] **Step 3: Commit**

```bash
git add Packages/com.chvj.unityinfra/Runtime/Network/CHMHttpNetwork.cs Packages/com.chvj.unityinfra/Runtime/Network/CHMHttpNetwork.cs.meta
git commit -m "# [infra] - CHMHttpNetwork 범용 async HTTP 래퍼 추가"
```

---

## Task 2: NetworkConfig SO + DTO + AuthTokenStore (+ 테스트)

**Files:**
- Create: `Assets/_Lair/Scripts/Net/NetworkConfig.cs`, `Assets/_Lair/Scripts/Net/NetDtos.cs`, `Assets/_Lair/Scripts/Net/AuthTokenStore.cs`
- Create: `Assets/_Lair/Tests/EditMode/NetDtoMappingTests.cs`, `Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs`

- [ ] **Step 1: NetworkConfig ScriptableObject**

Create `Assets/_Lair/Scripts/Net/NetworkConfig.cs`:
```csharp
using UnityEngine;

namespace Lair.Net
{
    //# 서버 접속 설정 SO — Addressable(EData.NetworkConfig) 로 로드.
    [CreateAssetMenu(fileName = "NetworkConfig", menuName = "Lair/NetworkConfig")]
    public class NetworkConfig : ScriptableObject
    {
        [SerializeField] private string _baseUrl = "http://localhost:8080";
        [SerializeField] private int _timeoutSec = 10;

        public string BaseUrl => _baseUrl.TrimEnd('/');
        public int TimeoutSec => _timeoutSec;
    }
}
```

- [ ] **Step 2: DTO (서버 계약) — JsonUtility 직렬화용**

Create `Assets/_Lair/Scripts/Net/NetDtos.cs`:
```csharp
using System;
using System.Collections.Generic;
using Lair.Meta;

namespace Lair.Net
{
    //# 서버 응답/요청 본문 — 필드명은 서버 JSON 과 정확히 일치해야 한다(JsonUtility 대소문자 그대로).
    [Serializable]
    public class AnonymousAuthRequestBody
    {
        public string deviceId;
    }

    [Serializable]
    public class AnonymousAuthResponse
    {
        public long accountId;
        public string token;
    }

    //# PUT /save 본문 — profile 은 MetaProfile 을 그대로 직렬화(필드명 일치, spec §4).
    [Serializable]
    public class PutSaveRequestBody
    {
        public MetaProfile profile;
        public int schemaVersion;
        public string clientUpdatedAt;   //# ISO8601 UTC
    }

    [Serializable]
    public class SaveResponseBody
    {
        public MetaProfile profile;
        public int schemaVersion;
        public string updatedAt;
    }

    [Serializable]
    public class SubmitScoreRequestBody
    {
        public int clearTimeMs;
        public string hero;
        public string displayName;
    }

    [Serializable]
    public class SubmitScoreResponseBody
    {
        public bool accepted;
        public long rank;
    }

    [Serializable]
    public class LeaderboardRowDto
    {
        public long rank;
        public string displayName;
        public int clearTimeMs;
        public string hero;
    }

    //# JsonUtility 는 최상위 배열을 못 읽으므로 래퍼로 감싼다.
    [Serializable]
    public class LeaderboardRowListWrapper
    {
        public List<LeaderboardRowDto> rows;
    }
}
```

- [ ] **Step 3: AuthTokenStore 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Net;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class AuthTokenStoreTests
    {
        [TearDown]
        public void 정리()
        {
            PlayerPrefs.DeleteKey("Lair.Net.DeviceId");
            PlayerPrefs.DeleteKey("Lair.Net.Token");
        }

        [Test]
        public void DeviceId_없으면_생성하고_재호출시_동일하다()
        {
            string first = AuthTokenStore.GetOrCreateDeviceId();
            string second = AuthTokenStore.GetOrCreateDeviceId();
            Assert.IsFalse(string.IsNullOrEmpty(first));
            Assert.AreEqual(first, second);
        }

        [Test]
        public void 토큰_저장후_읽으면_같은값이다()
        {
            AuthTokenStore.SaveToken("abc.def.ghi");
            Assert.AreEqual("abc.def.ghi", AuthTokenStore.Token);
            Assert.IsTrue(AuthTokenStore.HasToken);
        }
    }
}
```

- [ ] **Step 4: 테스트 실패 확인**

EditMode 러너 실행 → `AuthTokenStore` 미존재로 컴파일 실패.

- [ ] **Step 5: AuthTokenStore 구현**

Create `Assets/_Lair/Scripts/Net/AuthTokenStore.cs`:
```csharp
using System;
using UnityEngine;

namespace Lair.Net
{
    //# deviceId(GUID 1회 생성)와 JWT 를 PlayerPrefs 에 저장. Application.dataPath 쓰기 금지(과거 사고 회피).
    public static class AuthTokenStore
    {
        private const string DeviceIdKey = "Lair.Net.DeviceId";
        private const string TokenKey = "Lair.Net.Token";

        public static string GetOrCreateDeviceId()
        {
            string id = PlayerPrefs.GetString(DeviceIdKey, string.Empty);
            if (string.IsNullOrEmpty(id))
            {
                id = Guid.NewGuid().ToString("N");
                PlayerPrefs.SetString(DeviceIdKey, id);
                PlayerPrefs.Save();
            }
            return id;
        }

        public static string Token => PlayerPrefs.GetString(TokenKey, string.Empty);

        public static bool HasToken => string.IsNullOrEmpty(Token) == false;

        public static void SaveToken(string token)
        {
            PlayerPrefs.SetString(TokenKey, token ?? string.Empty);
            PlayerPrefs.Save();
        }

        public static void ClearToken()
        {
            PlayerPrefs.DeleteKey(TokenKey);
            PlayerPrefs.Save();
        }
    }
}
```

- [ ] **Step 6: DTO 라운드트립 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/NetDtoMappingTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Meta;
using Lair.Net;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class NetDtoMappingTests
    {
        [Test]
        public void MetaProfile_을_JsonUtility로_왕복하면_보존된다()
        {
            MetaProfile profile = new MetaProfile { Souls = 42, LordXp = 7, SelectedHero = "Knight", BestClearTime = 123.5f };
            profile.SetShopLevel("HpUp", 3);
            profile.AddDistinct(profile.AchievedIds, "FirstWin");
            profile.AddDistinct(profile.SeenMonsters, "Wisp");

            string json = JsonUtility.ToJson(profile);
            MetaProfile back = JsonUtility.FromJson<MetaProfile>(json);

            Assert.AreEqual(42, back.Souls);
            Assert.AreEqual(3, back.GetShopLevel("HpUp"));
            Assert.Contains("FirstWin", back.AchievedIds);
            Assert.Contains("Wisp", back.SeenMonsters);
            Assert.AreEqual(123.5f, back.BestClearTime);
        }

        [Test]
        public void PutSaveRequestBody_가_profile을_품고_직렬화된다()
        {
            PutSaveRequestBody body = new PutSaveRequestBody
            {
                profile = new MetaProfile { Souls = 5 },
                schemaVersion = 1,
                clientUpdatedAt = "2026-06-15T00:00:00Z",
            };
            string json = JsonUtility.ToJson(body);
            Assert.IsTrue(json.Contains("\"souls\":5") || json.Contains("\"Souls\":5"));
            Assert.IsTrue(json.Contains("schemaVersion"));
        }
    }
}
```

> 참고: `MetaProfile` 필드는 public 이고 `[Serializable]` 이라 JsonUtility 가 그대로 직렬화한다. 서버 DTO 필드명(C# PascalCase)과 Unity MetaProfile 필드명이 동일하므로 JSON 키도 동일.

- [ ] **Step 7: 테스트 통과 확인**

EditMode 러너 → `NetDtoMappingTests`, `AuthTokenStoreTests` 전부 통과.

- [ ] **Step 8: EData 에 NetworkConfig 키 추가 + SO 에셋 생성**

`Assets/_Lair/Scripts/Data/CommonEnum.cs` 의 `EData` enum 맨 끝에 append (int 직렬화 정합 — 순서 변경 금지):
```csharp
        HeroSkillLoadout,   //# 영웅 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout.asset (2026-06-04)
        NetworkConfig,      //# 서버 접속 설정 SO — Art/Net/NetworkConfig.asset (2026-06-15)
```
에디터에서 `Assets/_Lair/Art/Net/NetworkConfig.asset` 생성(메뉴 Lair/NetworkConfig), baseUrl 기본값 확인, Addressable 등록(주소 = `NetworkConfig`, 라벨 = `Resource`).

- [ ] **Step 9: Commit**

```bash
git add Assets/_Lair/Scripts/Net/NetworkConfig.cs Assets/_Lair/Scripts/Net/NetworkConfig.cs.meta Assets/_Lair/Scripts/Net/NetDtos.cs Assets/_Lair/Scripts/Net/NetDtos.cs.meta Assets/_Lair/Scripts/Net/AuthTokenStore.cs Assets/_Lair/Scripts/Net/AuthTokenStore.cs.meta Assets/_Lair/Tests/EditMode/NetDtoMappingTests.cs Assets/_Lair/Tests/EditMode/NetDtoMappingTests.cs.meta Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs Assets/_Lair/Tests/EditMode/AuthTokenStoreTests.cs.meta Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Art/Net
git commit -m "# [feat] - 서버 접속 설정·DTO·토큰 저장소 (계정 귀속 토대)"
```

---

## Task 3: ILairApiClient + LairApiClient

**Files:**
- Create: `Assets/_Lair/Scripts/Net/ILairApiClient.cs`, `Assets/_Lair/Scripts/Net/LairApiClient.cs`

엔드포인트 추상화. 실제 HTTP 는 CHMHttpNetwork 사용. EditMode 테스트는 다음 태스크에서 가짜 구현으로 검증하므로 여기선 인터페이스+구현+컴파일.

- [ ] **Step 1: 인터페이스 작성**

Create `Assets/_Lair/Scripts/Net/ILairApiClient.cs`:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;

namespace Lair.Net
{
    //# 서버 엔드포인트 추상화 — 서비스가 이 인터페이스에만 의존(테스트 시 가짜 주입, Rule 02 §5).
    public interface ILairApiClient
    {
        //# 인증 — deviceId 로 계정 보장 + 토큰 저장. 성공 여부 반환.
        Task<bool> AuthenticateAsync();
        //# 클라우드 세이브 조회 — 없으면 null, 통신 실패면 null.
        Task<SaveResponseBody> GetSaveAsync();
        //# 클라우드 세이브 저장 — 결과(성공/409충돌/실패) 반환.
        Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt);
        //# 리더보드 제출.
        Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName);
        //# Top N 조회 — 실패면 빈 리스트.
        Task<List<LeaderboardRowDto>> GetTopAsync(int top);
        //# 내 순위 ±주변 — 실패면 빈 리스트.
        Task<List<LeaderboardRowDto>> GetMyRankAsync();
    }

    //# PutSave 결과 — 409(서버가 더 최신)를 호출부가 구분하도록.
    public enum CloudSaveResult
    {
        Success,
        Conflict,
        Failed,
    }
}
```

- [ ] **Step 2: LairApiClient 구현**

Create `Assets/_Lair/Scripts/Net/LairApiClient.cs`:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Meta;
using UnityEngine;

namespace Lair.Net
{
    //# CHMHttpNetwork 로 서버 엔드포인트를 호출하는 구현. 토큰은 AuthTokenStore.
    public class LairApiClient : ILairApiClient
    {
        private readonly NetworkConfig _config;

        public LairApiClient(NetworkConfig config)
        {
            _config = config;
        }

        private string Url(string path) => $"{_config.BaseUrl}{path}";
        private int Timeout => _config.TimeoutSec;

        public async Task<bool> AuthenticateAsync()
        {
            AnonymousAuthRequestBody req = new AnonymousAuthRequestBody { deviceId = AuthTokenStore.GetOrCreateDeviceId() };
            CHHttpResult res = await CHMHttpNetwork.PostAsync(Url("/auth/anonymous"), JsonUtility.ToJson(req), null, Timeout);
            if (res.IsSuccess == false)
            {
                Debug.LogWarning($"[LairApiClient] 인증 실패: {res.StatusCode} {res.Error}");
                return false;
            }
            AnonymousAuthResponse parsed = JsonUtility.FromJson<AnonymousAuthResponse>(res.Body);
            if (parsed == null || string.IsNullOrEmpty(parsed.token))
                return false;
            AuthTokenStore.SaveToken(parsed.token);
            return true;
        }

        public async Task<SaveResponseBody> GetSaveAsync()
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url("/save"), AuthTokenStore.Token, Timeout);
            if (res.IsSuccess == false)
                return null;
            return JsonUtility.FromJson<SaveResponseBody>(res.Body);
        }

        public async Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            PutSaveRequestBody body = new PutSaveRequestBody
            {
                profile = profile,
                schemaVersion = profile.Version,
                clientUpdatedAt = clientUpdatedAt,
            };
            CHHttpResult res = await CHMHttpNetwork.PutAsync(Url("/save"), JsonUtility.ToJson(body), AuthTokenStore.Token, Timeout);
            if (res.IsConflict)
                return CloudSaveResult.Conflict;
            if (res.IsSuccess == false)
                return CloudSaveResult.Failed;
            return CloudSaveResult.Success;
        }

        public async Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            SubmitScoreRequestBody body = new SubmitScoreRequestBody { clearTimeMs = clearTimeMs, hero = hero, displayName = displayName };
            CHHttpResult res = await CHMHttpNetwork.PostAsync(Url("/leaderboard/submit"), JsonUtility.ToJson(body), AuthTokenStore.Token, Timeout);
            return res.IsSuccess;
        }

        public async Task<List<LeaderboardRowDto>> GetTopAsync(int top)
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url($"/leaderboard?top={top}"), AuthTokenStore.Token, Timeout);
            return ParseRows(res);
        }

        public async Task<List<LeaderboardRowDto>> GetMyRankAsync()
        {
            CHHttpResult res = await CHMHttpNetwork.GetAsync(Url("/leaderboard/me"), AuthTokenStore.Token, Timeout);
            return ParseRows(res);
        }

        //# 서버가 최상위 JSON 배열을 반환하므로 래퍼로 감싸 JsonUtility 파싱.
        private static List<LeaderboardRowDto> ParseRows(CHHttpResult res)
        {
            if (res.IsSuccess == false || string.IsNullOrEmpty(res.Body))
                return new List<LeaderboardRowDto>();
            string wrapped = "{\"rows\":" + res.Body + "}";
            LeaderboardRowListWrapper parsed = JsonUtility.FromJson<LeaderboardRowListWrapper>(wrapped);
            return parsed != null && parsed.rows != null ? parsed.rows : new List<LeaderboardRowDto>();
        }
    }
}
```

- [ ] **Step 3: 컴파일 확인 + Commit**

EditMode 컴파일 0 에러 확인.
```bash
git add Assets/_Lair/Scripts/Net/ILairApiClient.cs Assets/_Lair/Scripts/Net/ILairApiClient.cs.meta Assets/_Lair/Scripts/Net/LairApiClient.cs Assets/_Lair/Scripts/Net/LairApiClient.cs.meta
git commit -m "# [feat] - 서버 API 클라이언트(인증·세이브·리더보드 호출)"
```

---

## Task 4: CloudSaveService (백업/복원/409) + 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Net/CloudSaveService.cs`, `Assets/_Lair/Tests/EditMode/FakeLairApiClient.cs`, `Assets/_Lair/Tests/EditMode/CloudSaveServiceTests.cs`

- [ ] **Step 1: 가짜 client 작성**

Create `Assets/_Lair/Tests/EditMode/FakeLairApiClient.cs`:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    //# ILairApiClient 의 인메모리 가짜 — 호출 기록과 반환값을 테스트가 제어.
    public class FakeLairApiClient : ILairApiClient
    {
        public bool AuthResult = true;
        public SaveResponseBody SaveToReturn;
        public CloudSaveResult PutResultToReturn = CloudSaveResult.Success;
        public MetaProfile LastPutProfile;
        public bool SubmitResult = true;
        public int LastSubmittedMs = -1;
        public List<LeaderboardRowDto> TopToReturn = new List<LeaderboardRowDto>();

        public Task<bool> AuthenticateAsync() => Task.FromResult(AuthResult);
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult(SaveToReturn);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt)
        {
            LastPutProfile = profile;
            return Task.FromResult(PutResultToReturn);
        }
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName)
        {
            LastSubmittedMs = clearTimeMs;
            return Task.FromResult(SubmitResult);
        }
        public Task<List<LeaderboardRowDto>> GetTopAsync(int top) => Task.FromResult(TopToReturn);
        public Task<List<LeaderboardRowDto>> GetMyRankAsync() => Task.FromResult(TopToReturn);
    }
}
```

- [ ] **Step 2: CloudSaveService 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/CloudSaveServiceTests.cs`:
```csharp
using NUnit.Framework;
using Lair.Meta;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    public class CloudSaveServiceTests
    {
        [Test]
        public void 백업_성공이면_프로필을_업로드한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Success };
            CloudSaveService svc = new CloudSaveService(fake);
            MetaProfile profile = new MetaProfile { Souls = 10 };

            CloudSaveResult result = svc.BackupAsync(profile).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Success, result);
            Assert.AreSame(profile, fake.LastPutProfile);
        }

        [Test]
        public void 백업_409면_충돌을_그대로_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { PutResultToReturn = CloudSaveResult.Conflict };
            CloudSaveService svc = new CloudSaveService(fake);

            CloudSaveResult result = svc.BackupAsync(new MetaProfile()).GetAwaiter().GetResult();

            Assert.AreEqual(CloudSaveResult.Conflict, result);
        }

        [Test]
        public void 복원_서버데이터있으면_프로필을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient
            {
                SaveToReturn = new SaveResponseBody { profile = new MetaProfile { Souls = 99 }, schemaVersion = 1 },
            };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile restored = svc.RestoreAsync().GetAwaiter().GetResult();

            Assert.IsNotNull(restored);
            Assert.AreEqual(99, restored.Souls);
        }

        [Test]
        public void 복원_서버데이터없으면_null을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SaveToReturn = null };
            CloudSaveService svc = new CloudSaveService(fake);

            MetaProfile restored = svc.RestoreAsync().GetAwaiter().GetResult();

            Assert.IsNull(restored);
        }
    }
}
```

- [ ] **Step 3: 테스트 실패 확인**

EditMode 러너 → `CloudSaveService` 미존재로 컴파일 실패.

- [ ] **Step 4: CloudSaveService 구현**

Create `Assets/_Lair/Scripts/Net/CloudSaveService.cs`:
```csharp
using System;
using System.Threading.Tasks;
using Lair.Meta;

namespace Lair.Net
{
    //# 클라우드 세이브 오케스트레이션 — 백업/복원. 충돌(409)은 호출부가 프롬프트로 처리.
    public class CloudSaveService
    {
        private readonly ILairApiClient _api;

        public CloudSaveService(ILairApiClient api)
        {
            _api = api;
        }

        //# 자동 백업 — best-effort. 결과(성공/충돌/실패) 반환.
        public async Task<CloudSaveResult> BackupAsync(MetaProfile profile)
        {
            if (profile == null)
                return CloudSaveResult.Failed;
            string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return await _api.PutSaveAsync(profile, nowIso);
        }

        //# 수동 복원 — 서버 프로필 반환(없거나 실패면 null). 로컬 교체는 호출부 책임.
        public async Task<MetaProfile> RestoreAsync()
        {
            SaveResponseBody res = await _api.GetSaveAsync();
            if (res == null || res.profile == null)
                return null;
            return res.profile;
        }
    }
}
```

- [ ] **Step 5: 테스트 통과 확인**

EditMode 러너 → `CloudSaveServiceTests` 4개 전부 통과.

- [ ] **Step 6: Commit**

```bash
git add Assets/_Lair/Scripts/Net/CloudSaveService.cs Assets/_Lair/Scripts/Net/CloudSaveService.cs.meta Assets/_Lair/Tests/EditMode/FakeLairApiClient.cs Assets/_Lair/Tests/EditMode/FakeLairApiClient.cs.meta Assets/_Lair/Tests/EditMode/CloudSaveServiceTests.cs Assets/_Lair/Tests/EditMode/CloudSaveServiceTests.cs.meta
git commit -m "# [feat] - 클라우드 세이브 백업/복원 서비스 (기기이전 보존)"
```

---

## Task 5: LeaderboardClient (제출/조회) + 테스트

**Files:**
- Create: `Assets/_Lair/Scripts/Net/LeaderboardClient.cs`, `Assets/_Lair/Tests/EditMode/LeaderboardClientTests.cs`

- [ ] **Step 1: 실패 테스트 작성**

Create `Assets/_Lair/Tests/EditMode/LeaderboardClientTests.cs`:
```csharp
using System.Collections.Generic;
using NUnit.Framework;
using Lair.Net;

namespace Lair.Tests.EditMode
{
    public class LeaderboardClientTests
    {
        [Test]
        public void 제출은_클리어타임을_그대로_전달한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient { SubmitResult = true };
            LeaderboardClient client = new LeaderboardClient(fake);

            bool ok = client.SubmitAsync(120000, "Knight", "Alice").GetAwaiter().GetResult();

            Assert.IsTrue(ok);
            Assert.AreEqual(120000, fake.LastSubmittedMs);
        }

        [Test]
        public void Top조회는_서버목록을_반환한다()
        {
            FakeLairApiClient fake = new FakeLairApiClient
            {
                TopToReturn = new List<LeaderboardRowDto> { new LeaderboardRowDto { rank = 1, displayName = "Bob", clearTimeMs = 60000, hero = "Knight" } },
            };
            LeaderboardClient client = new LeaderboardClient(fake);

            List<LeaderboardRowDto> rows = client.GetTopAsync(100).GetAwaiter().GetResult();

            Assert.AreEqual(1, rows.Count);
            Assert.AreEqual("Bob", rows[0].displayName);
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인**

EditMode 러너 → `LeaderboardClient` 미존재로 컴파일 실패.

- [ ] **Step 3: LeaderboardClient 구현**

Create `Assets/_Lair/Scripts/Net/LeaderboardClient.cs`:
```csharp
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Lair.Net
{
    //# 리더보드 제출/조회 — best-effort. 실패해도 게임 흐름 차단 금지.
    public class LeaderboardClient
    {
        private readonly ILairApiClient _api;

        public LeaderboardClient(ILairApiClient api)
        {
            _api = api;
        }

        public Task<bool> SubmitAsync(int clearTimeMs, string hero, string displayName)
            => _api.SubmitScoreAsync(clearTimeMs, hero, displayName);

        public Task<List<LeaderboardRowDto>> GetTopAsync(int top)
            => _api.GetTopAsync(top);

        public Task<List<LeaderboardRowDto>> GetMyRankAsync()
            => _api.GetMyRankAsync();
    }
}
```

- [ ] **Step 4: 테스트 통과 확인 + Commit**

EditMode 러너 → `LeaderboardClientTests` 통과.
```bash
git add Assets/_Lair/Scripts/Net/LeaderboardClient.cs Assets/_Lair/Scripts/Net/LeaderboardClient.cs.meta Assets/_Lair/Tests/EditMode/LeaderboardClientTests.cs Assets/_Lair/Tests/EditMode/LeaderboardClientTests.cs.meta
git commit -m "# [feat] - 리더보드 제출/조회 클라이언트 (최단클리어 경쟁)"
```

---

## Task 6: 훅 연결 — 인증·자동백업(Village) + 제출(Battle)

**Files:**
- Modify: `Assets/_Lair/Scripts/Meta/MetaSession.cs`
- Modify: `Assets/_Lair/Scripts/Village/VillageController.cs:23-40` (Start), `:171-176` (HandleProfileChanged)
- Modify: `Assets/_Lair/Scripts/Battle/BattleController.cs:884-958` (EndBattle)

연동 진입을 한곳에 모으기 위해 `MetaSession` 에 API client/서비스 핸들을 둔다(static holder 와 일관).

- [ ] **Step 1: MetaSession 에 연동 핸들 추가**

`Assets/_Lair/Scripts/Meta/MetaSession.cs` 에 필드/헬퍼 추가 (기존 내용 유지하고 아래를 클래스 본문에 추가):
```csharp
using Lair.Net;
using UnityEngine;

namespace Lair.Meta
{
    public static partial class MetaSession
    {
        //# 연동 핸들 — Village 진입 시 1회 구성. null 이면 클라우드 기능 비활성(오프라인).
        public static ILairApiClient Api;
        public static CloudSaveService Cloud;
        public static LeaderboardClient Leaderboard;

        //# NetworkConfig 로드 후 client/서비스 구성 + 익명 인증 보장. best-effort.
        public static async System.Threading.Tasks.Task EnsureNetworkAsync()
        {
            if (Api != null)
                return;
            NetworkConfig config = await ChvjUnityInfra.CHMResource.Instance.LoadAsync<NetworkConfig>(Lair.Data.EData.NetworkConfig);
            if (config == null)
            {
                Debug.LogWarning("[MetaSession] NetworkConfig 로드 실패 — 클라우드 비활성");
                return;
            }
            Api = new LairApiClient(config);
            Cloud = new CloudSaveService(Api);
            Leaderboard = new LeaderboardClient(Api);
            bool authed = await Api.AuthenticateAsync();
            if (authed == false)
            {
                Debug.LogWarning("[MetaSession] 익명 인증 실패 — 클라우드 비활성");
                Api = null;
                Cloud = null;
                Leaderboard = null;
            }
        }
    }
}
```
> `MetaSession` 을 `partial` 로 바꾼다 — 기존 `MetaSession.cs` 의 `public static class MetaSession` 를 `public static partial class MetaSession` 로 수정(한 단어 추가).

- [ ] **Step 2: VillageController.Start 에서 인증 보장 + HandleProfileChanged 자동 백업**

`Assets/_Lair/Scripts/Village/VillageController.cs` `Start()` 의 프로필 로드 직후에 추가:
```csharp
            MetaProfile profile = MetaSession.GetOrLoad();
            _vm = new VillageViewModel(profile, _metaConfig);

            //# 클라우드 연동 보장(best-effort) — 실패해도 마을은 정상 동작.
            await MetaSession.EnsureNetworkAsync();
```
`HandleProfileChanged()` 를 다음으로 교체(로컬 저장 직후 best-effort 백업 추가):
```csharp
        //# 상점 구매 등 프로필 변경 시 — 즉시 로컬 저장 + 상단바 갱신 + 클라우드 백업(best-effort).
        private void HandleProfileChanged()
        {
            MetaSession.Store?.Save(MetaSession.Profile);
            _vm?.NotifyProfileChanged();
            BackupToCloud();
        }

        //# fire-and-forget 백업 — 실패/충돌은 로그만(게임 흐름 차단 금지). 충돌 UX 는 클라우드 메뉴에서.
        private async void BackupToCloud()
        {
            if (MetaSession.Cloud == null)
                return;
            CloudSaveResult result = await MetaSession.Cloud.BackupAsync(MetaSession.Profile);
            if (result == CloudSaveResult.Conflict)
                Debug.Log("[VillageController] 클라우드가 더 최신 — 복원 메뉴에서 처리 가능");
        }
```
파일 상단 `using Lair.Net;` 추가.

- [ ] **Step 3: BattleController.EndBattle 에서 승리 시 리더보드 제출**

`Assets/_Lair/Scripts/Battle/BattleController.cs` `EndBattle` 의 승리 분기(`if (result == BattleResult.Win)` 부근, line 918)에 추가:
```csharp
                if (result == BattleResult.Win)
                {
                    //# 최단 클리어 제출(best-effort) — 클리어타임 = 영웅 사망까지 경과(낮을수록 상위).
                    SubmitLeaderboard();
                }
```
같은 클래스에 메서드 추가:
```csharp
        //# 승리 시 리더보드 제출 — fire-and-forget. 표시명은 임시(deviceId 파생), 기획서 확정값으로 대체 가능.
        private async void SubmitLeaderboard()
        {
            if (MetaSession.Leaderboard == null)
                return;
            int clearMs = Mathf.RoundToInt(_clock.Elapsed * 1000f);
            string hero = MetaSession.Profile != null ? MetaSession.Profile.SelectedHero : EHero.Knight.ToString();
            string name = "Lord-" + Lair.Net.AuthTokenStore.GetOrCreateDeviceId().Substring(0, 4);
            await MetaSession.Leaderboard.SubmitAsync(clearMs, hero, name);
        }
```
파일 상단에 `using Lair.Net;` 가 없으면 추가. (`Mathf`, `_clock` 은 기존 존재.)

> 주의: Battle 씬에 직접 진입한 경우 `MetaSession.Leaderboard` 가 null 일 수 있다 — 가드로 안전(제출 생략). 정식 흐름(Village→Battle)에선 EnsureNetworkAsync 가 선행돼 구성됨.

- [ ] **Step 4: 컴파일 + 기존 EditMode 회귀 확인**

EditMode 러너 전체 실행 → 신규 4 파일 테스트 + 기존 테스트 모두 통과, 컴파일 0 에러.

- [ ] **Step 5: Commit**

```bash
git add Assets/_Lair/Scripts/Meta/MetaSession.cs Assets/_Lair/Scripts/Village/VillageController.cs Assets/_Lair/Scripts/Battle/BattleController.cs
git commit -m "# [feat] - 마을 진입 시 클라우드 연결·자동 백업, 승리 시 리더보드 자동 제출"
```

---

## Task 7: 리더보드 조회 UI (BuildModalPopup 패턴) + 마을 진입

**Files:**
- Modify: `Assets/_Lair/Scripts/Data/CommonEnum.cs` (EUI.LeaderboardPopup append)
- Create: `Assets/_Lair/Scripts/UI/Village/LeaderboardPopup.cs`, `LeaderboardCell.cs`, `LeaderboardPoolingScrollView.cs`
- Modify: 마을 메뉴 진입부(`VillageController.OpenMenu` + VillageHud 버튼) — 클라우드/리더보드 진입
- Prefab: `Assets/_Lair/Art/UI/LeaderboardPopup.prefab` + `LeaderboardCell.prefab` (에디터 프리팹 빌더에 추가)

Rule 03 의 BuildModalPopup 3-class 패턴을 그대로 따른다 (Panel/ScrollView/Cell). 코드 동적 GameObject 생성 금지 — prefab 정적 배치 + 인스펙터 참조.

- [ ] **Step 1: EUI 에 LeaderboardPopup append**

`CommonEnum.cs` `EUI` enum 맨 끝에 (순서 변경 금지 — append):
```csharp
            LordLevelPopup,        //# 영주 레벨 보상 트랙
            LeaderboardPopup,      //# 최단클리어 리더보드 조회 (2026-06-15 v0.3)
```

- [ ] **Step 2: LeaderboardCell 작성**

Create `Assets/_Lair/Scripts/UI/Village/LeaderboardCell.cs`:
```csharp
using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;

namespace Lair.UI
{
    //# 리더보드 한 행 — 순위/이름/시간/영웅. 풀 재사용 셀(Rule 03 §3 CHText).
    public class LeaderboardCell : MonoBehaviour
    {
        [SerializeField] private CHText _rankText;
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _timeText;
        [SerializeField] private CHText _heroText;

        public void Bind(LeaderboardRowDto data)
        {
            if (data == null)
                return;
            _rankText.SetText(data.rank.ToString());
            _nameText.SetText(data.displayName);
            _timeText.SetText(FormatMs(data.clearTimeMs));
            _heroText.SetText(data.hero);
        }

        //# ms → m:ss.f 표기.
        private static string FormatMs(int ms)
        {
            float sec = ms / 1000f;
            int m = (int)(sec / 60f);
            float s = sec - m * 60f;
            return $"{m}:{s:00.0}";
        }
    }
}
```

- [ ] **Step 3: LeaderboardPoolingScrollView 작성**

Create `Assets/_Lair/Scripts/UI/Village/LeaderboardPoolingScrollView.cs`:
```csharp
using ChvjUnityInfra;
using Lair.Net;

namespace Lair.UI
{
    //# 리더보드 풀링 스크롤뷰 — InitItem 만 오버라이드(Rule 03 BuildModal 패턴).
    public class LeaderboardPoolingScrollView : CHPoolingScrollView<LeaderboardCell, LeaderboardRowDto>
    {
        public override void InitItem(LeaderboardCell item, LeaderboardRowDto data, int index)
            => item.Bind(data);

        public override void InitPoolingObject(LeaderboardCell item) { }
    }
}
```

- [ ] **Step 4: LeaderboardPopup 작성**

Create `Assets/_Lair/Scripts/UI/Village/LeaderboardPopup.cs`:
```csharp
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Net;
using UnityEngine;

namespace Lair.UI
{
    //# UIArg 는 UIBase 와 같은 파일(Rule 03 §5).
    public class LeaderboardPopupArg : UIArg
    {
        public LeaderboardClient Leaderboard;
    }

    //# 최단클리어 리더보드 조회 — Top N + 내 순위. 통신 실패 시 빈 목록 + 안내.
    public class LeaderboardPopup : UIBase
    {
        [SerializeField] private LeaderboardPoolingScrollView _scrollView;
        [SerializeField] private CHText _emptyText;   //# 빈 목록/실패 안내

        public override void InitUI(UIArg arg)
        {
            if (arg is LeaderboardPopupArg lbArg)
                Load(lbArg.Leaderboard);
        }

        private async void Load(LeaderboardClient leaderboard)
        {
            if (_emptyText != null)
                _emptyText.SetText(string.Empty);

            if (leaderboard == null)
            {
                ShowEmpty("오프라인 — 리더보드를 불러올 수 없습니다.");
                return;
            }

            List<LeaderboardRowDto> rows = await leaderboard.GetTopAsync(100);
            if (rows == null || rows.Count == 0)
            {
                ShowEmpty("아직 기록이 없습니다.");
                return;
            }
            _scrollView.SetItemList(rows);
        }

        private void ShowEmpty(string message)
        {
            _scrollView.SetItemList(new List<LeaderboardRowDto>());
            if (_emptyText != null)
                _emptyText.SetText(message);
        }
    }
}
```

- [ ] **Step 5: 프리팹 생성 — 에디터 프리팹 빌더에 추가**

`Assets/_Lair/Editor/` 의 UI 프리팹 빌더(BuildModalPopup 을 만드는 빌더)에 LeaderboardPopup/LeaderboardCell 생성 메서드를 추가한다. **손으로만 만든 프리팹은 빌더 재실행 시 사라지므로 반드시 빌더 코드에 반영**한다(BuildModalPopup + BuildModalCardCell 을 복제·개조).
- `LeaderboardCell.prefab`: RectTransform + 배경 Image + CHText 4개(_rankText/_nameText/_timeText/_heroText) + `LeaderboardCell`(인스펙터 참조 연결).
- `LeaderboardPopup.prefab`: UIBase(_backgroundButton/_backButton) + 제목 CHText + ScrollView(ScrollRect/Viewport(Image+RectMask2D)/Content(VerticalLayout)/origin Cell 인스턴스) + `LeaderboardPoolingScrollView`(_origin = origin Cell) + `_emptyText` CHText. `_scrollView` 인스펙터 연결.
- Addressable 등록: 주소 `LeaderboardPopup` (라벨 `Resource`). (Cell 은 Popup 내부 PrefabInstance — 별도 Addressable 불필요하나 파일은 분리.)

체크리스트(Rule 03): origin Cell 컴포넌트 제거 금지, `_scrollView`/`_origin`/셀 CHText 인스펙터 참조 전부 연결.

- [ ] **Step 6: 마을 메뉴에 리더보드 + 클라우드 복원 진입 추가**

`VillageController.OpenMenu` switch 에 LeaderboardPopup 케이스 추가:
```csharp
                case EUI.LeaderboardPopup:
                    await CHMUI.Instance.ShowUIAsync(EUI.LeaderboardPopup, new LeaderboardPopupArg
                    {
                        Leaderboard = MetaSession.Leaderboard,
                    });
                    break;
```
VillageHud 에 리더보드 버튼을 추가하고 `BindMenuButton(_leaderboardButton, hudArg, EUI.LeaderboardPopup)` 로 연결(기존 메뉴 버튼 패턴 그대로). 버튼/참조는 VillageHud 프리팹 빌더에 반영.

**클라우드 복원** — VillageHud 에 "클라우드 복원" 버튼 추가, 클릭 시 확인 후 복원:
```csharp
        //# 수동 복원 — 로컬 덮어쓰기라 확인 후. 성공 시 프로필 교체+저장+VM 갱신.
        public async void RestoreFromCloud()
        {
            if (MetaSession.Cloud == null)
                return;
            MetaProfile cloud = await MetaSession.Cloud.RestoreAsync();
            if (cloud == null)
            {
                Debug.Log("[VillageController] 클라우드 세이브 없음");
                return;
            }
            MetaSession.Profile = cloud;
            MetaSession.Store?.Save(cloud);
            _vm?.NotifyProfileChanged();
        }
```
> 확인 프롬프트 UI(예/아니오)는 기존 모달 패턴 또는 간단 확인 팝업으로. 구체 UX 는 game-designer 기획서에서 확정하되, 최소 구현은 위 메서드 + 확인 1단계.

- [ ] **Step 7: 컴파일 + EditMode 회귀 + 에디터에서 팝업 열림 확인**

EditMode 전체 통과, 컴파일 0 에러. 에디터 Play(Village) → 리더보드 메뉴 열림(서버 띄운 상태면 목록, 아니면 빈 안내) 확인.

- [ ] **Step 8: Commit**

```bash
git add Assets/_Lair/Scripts/Data/CommonEnum.cs Assets/_Lair/Scripts/UI/Village/LeaderboardPopup.cs Assets/_Lair/Scripts/UI/Village/LeaderboardPopup.cs.meta Assets/_Lair/Scripts/UI/Village/LeaderboardCell.cs Assets/_Lair/Scripts/UI/Village/LeaderboardCell.cs.meta Assets/_Lair/Scripts/UI/Village/LeaderboardPoolingScrollView.cs Assets/_Lair/Scripts/UI/Village/LeaderboardPoolingScrollView.cs.meta Assets/_Lair/Scripts/Village/VillageController.cs Assets/_Lair/Editor Assets/_Lair/Art/UI/LeaderboardPopup.prefab Assets/_Lair/Art/UI/LeaderboardPopup.prefab.meta Assets/_Lair/Art/UI/LeaderboardCell.prefab Assets/_Lair/Art/UI/LeaderboardCell.prefab.meta
git commit -m "# [feat] - 마을 리더보드 조회 화면 + 클라우드 복원 버튼"
```

---

## Task 8: 전체 EditMode 그린 + 마무리

- [ ] **Step 1: EditMode 전체 실행**

EditMode 러너 전체 → 신규(NetDtoMapping 2 · AuthTokenStore 2 · CloudSaveService 4 · LeaderboardClient 2) + 기존 회귀 전부 통과.

- [ ] **Step 2: 수동 연동 스모크(선택)**

서버를 `docker compose up`(별도 레포)로 띄우고 NetworkConfig.baseUrl 을 맞춘 뒤, 에디터 Play → 마을 진입(인증) → 상점 구매(자동 백업) → 승리(제출) → 리더보드 조회. 실패해도 게임 흐름 안 끊기는지 확인.

- [ ] **Step 3: Commit (필요 시)**

남은 변경 스테이징 + 커밋 메시지(안).

---

## Self-Review (author)

- **Spec 커버리지:** §3 CHMHttpNetwork→T1 · Config/Auth/Client→T2,T3 · §5 인증→T3(AuthenticateAsync)+T6(EnsureNetworkAsync) · §6 세이브(자동백업/수동복원/409)→T4+T6(HandleProfileChanged/BackupToCloud)+T7(RestoreFromCloud) · §7 리더보드(제출/조회/화면)→T5+T6(SubmitLeaderboard)+T7(LeaderboardPopup) · §4 DTO(MetaProfile 직렬화)→T2 · §8 오프라인(null 가드/빈목록)→전반 · §10 테스트→T2,T4,T5,T8. 미커버 0.
- **플레이스홀더:** 표시명 임시값("Lord-"+deviceId 4자)은 동작 가능한 구체값(기획서가 확정값으로 대체 가능 명시). 복원 확인 프롬프트는 최소 구현 명시 + 기획 위임. 그 외 TBD 없음.
- **타입 일관성:** `ILairApiClient`(AuthenticateAsync/GetSaveAsync/PutSaveAsync/SubmitScoreAsync/GetTopAsync/GetMyRankAsync) ↔ LairApiClient/FakeLairApiClient/CloudSaveService/LeaderboardClient 시그니처 일치. `CloudSaveResult`(Success/Conflict/Failed) 일관. `LeaderboardRowDto` 필드(rank/displayName/clearTimeMs/hero) 셀·테스트 일치. `MetaProfile`(Version/Souls/...) 기존 정의 사용.
- **알려진 한계(범위 밖):** 서버 권위 안티치트·소셜 로그인·표시명 입력 UX·복원 충돌 자동 머지 — spec §2 제외 항목.
```
