# Firebase SDK 전환 (Spec 1.5 — 익명 인증 유지) — Design Spec

- **작성일**: 2026-07-28
- **단계**: Project Lair v0.3 (서버 연동 클라이언트)
- **선행 문서**: spec `2026-07-14-firebase-backend-pivot-design.md` · plan `2026-07-14-firebase-backend-pivot.md` · 기획서 `docs/design/firebase-backend-pivot.md`
- **UX 단일 진실**: `docs/design/unity-server-integration.md` (계승 — 본 전환은 UX 를 바꾸지 않는다)

---

## §1. 의도 · 범위

Firebase 연동 방식을 **REST(`CHMHttpNetwork`) → Firebase Unity SDK(Auth + Firestore)** 로 교체한다. 백엔드(Firebase 프로젝트 `lair-970fa`)는 그대로이며, **클라이언트가 그 백엔드에 말을 거는 방식만** 바뀐다.

**범위 내**
- Firebase Unity SDK 도입 + ChvjPackage 에 `Firebase` 모듈 신설(툴 토글 포함)
- `ILairApiClient` 7개 메서드를 SDK 로 재구현 (`FirebaseSdkApiClient`)
- REST 구현체 및 그 부산물 일괄 삭제
- 실제 Firebase 프로젝트에 붙어 **익명 인증으로 동작하는 것까지** 확인

**범위 밖 (Spec 2)**
- 구글 로그인 · 계정 연동 · 계정 병합 정책
- SHA-1 등록 후 `google-services.json` 재발급
- 서버 권위 안티치트(Cloud Functions 등)

### 왜 지금인가

REST 는 Spec 1 범위(익명 + 컬렉션 3개)에선 합리적이었으나, 비용이 이미 드러나 있다 — `CHMHttpNetwork` 에 PATCH 가 없어 `documents:commit` 으로 우회, 정규식 기반 JSON 파싱(plan 자체가 "취약하다" 경고), 토큰 만료 미처리(1시간 한계). 그리고 **구글 로그인이 손익분기점**이다: SDK 면 거의 공짜인 것을 네이티브 플러그인 래핑으로 직접 만들어야 한다. 전환은 **Spec 2 코드를 쓰기 전이 가장 싸다.**

전환 비용이 낮은 이유는 `ILairApiClient` 추상화다 — Spec 1 이 자체서버→Firebase 로 갈아탈 때 이미 증명됐다. 구현체 1개 + 조립 1줄.

---

## §2. 결정 락

| # | 질문 | 결정 | 근거 |
|---|---|---|---|
| Q1 | 사문화되는 REST 코드 처리 | **전부 삭제** | 보존 시 죽은 백엔드 2세트 + 죽은 테스트 264줄. EditMode 가 매 사이클 호출자 없는 파싱 계약을 고정한다. 복원은 git history |
| Q2 | `AuthTokenStore` 정리 범위 | **소비자 없는 것 전부 제거** — `RankingRowDto.accountId` · `RankingPopup` accountId 폴백 포함 | 사문화 0 원칙 일관. 항상 0인 값을 비교하는 분기를 남기지 않는다 |
| Q3 | SDK 설치 가능성 검증 시점 | **plan Task 1 = 설치·스모크 게이트** | spec·plan 작성과 설치를 병행. 막히면 그 지점에서 멈추고 REST 유지 롤백 판단 |
| Q4 | SDK 를 어디서 감싸는가 | **하이브리드 — 초기화만 인프라 모듈, 도메인 구현체는 게임** | 세이브 문서 구조·랭킹 쿼리는 Lair 도메인이지 공통 인프라가 아니다. `CHMGPGS` 가 패키지에 있는 건 GPGS 가 범용 플랫폼 서비스이기 때문 |
| Q5 | SDK 설치 형식 | 결정 시점엔 **UPM(`.tgz`)** 선호 → **2026-07-28 실제 진행은 `.unitypackage`**(`Assets/Firebase/`, SDK 13.14.0) | 패키지 모듈이 의존하는 SDK 가 `Packages/` 에 함께 있는 편이 배치·버전 관리가 깨끗하다는 이유로 UPM 을 선호했다. (`.unitypackage` 로 `Assets/Firebase/Plugins/` 에 깔아도 **Rule 03 §1 위반은 아니다** — 그 룰은 게임 코드↔패키지 방향에 대한 것이고 서드파티 precompiled DLL 참조는 별개다. UPM 선택은 위생 문제이지 룰 강제가 아니었다 — 실제 설치 방식이 달라져도 결정 자체는 무효화되지 않는다. plan §2 line 67 정정 배너 참조) |

---

## §3. 아키텍처

| 파일 | 위치 | 책임 | 유형 |
|---|---|---|---|
| `CHMFirebase.cs` | `Packages/com.chvj.unityinfra/Runtime/Firebase/` | `FirebaseApp` 초기화 · `CheckAndFixDependenciesAsync` · 준비 상태 노출. **도메인 무지** | 생성 |
| `com.chvj.unityinfra.firebase.asmdef` | 위와 동일 | `defineConstraints: ["UNITY_INFRA_FIREBASE"]` — Social 모듈과 동일 구조 | 생성 |
| `ChvjUnityInfraSettingsWindow.cs` | `Packages/.../Editor/` | **"Firebase" 탭 추가** — `DrawToggle("Use Firebase", FIREBASE_DEFINE)` + 사용 스텝 HelpBox | 수정 |
| `FirebaseSdkApiClient.cs` | `Assets/_Lair/Scripts/Net/` | `ILairApiClient` 7개 메서드를 Auth + Firestore SDK 로 구현 | 생성 |
| `AuthTokenStore.cs` | 기존 | `GetOrCreateDeviceId` + `Uid` 만 잔존 | 수정 |
| `MetaSession.Net.cs` | 기존 | 초기화 대기 + 조립 교체 | 수정 |

### 3.1 불변 조건 — Firebase 타입 격리 (컴파일이 강제)

`Firebase.*` 타입은 **`FirebaseSdkApiClient` 와 `CHMFirebase` 내부에만** 존재한다. `ILairApiClient` 와 그 DTO(`SaveResponseBody` · `RankingRowDto` · `CloudSaveResult` · `DisplayNameResult`)의 시그니처에 Firebase 타입이 하나라도 새면 안 된다.

이것은 스타일 권고가 아니라 **컴파일 제약**이다 — `Lair.Tests.EditMode.asmdef` 이 `overrideReferences: true` + 명시적 `precompiledReferences`(nunit, Newtonsoft only) 라 Firebase DLL 을 보지 못한다. 누출되는 순간 기존 EditMode 테스트 전체가 컴파일 실패한다. 즉 이 규칙이 **기존 테스트 생존을 지키는 실질 방어선**이다.

### 3.2 모듈 게이트가 게임 코드에 만드는 결과

`UNITY_INFRA_FIREBASE` 가 꺼지면 `CHMFirebase` 어셈블리가 컴파일에서 빠진다. 따라서:

- `FirebaseSdkApiClient.cs` **전체를 `#if UNITY_INFRA_FIREBASE` 로 감싼다**
- `MetaSession.EnsureNetworkAsync` 의 조립부가 갈린다:

```csharp
#if UNITY_INFRA_FIREBASE
    Api = new FirebaseSdkApiClient();
#else
    Debug.LogWarning("[MetaSession] Firebase 모듈 꺼짐 — 클라우드 비활성");
    return;
#endif
```

`Api == null` → `IsCloudConnected == false` 는 **이미 지원되는 오프라인 상태**다. 모듈을 끄면 게임이 깨지는 게 아니라 클라우드 기능만 사라진다. GPGS 가이드가 호출부를 `#if UNITY_INFRA_SOCIAL` 로 감싸라고 하는 것과 같은 규약이다.

---

## §4. 데이터 흐름 · 충돌 감지 재매핑

데이터 모델(`saves/{uid}` · `leaderboard/{uid}` · `displayNames/{name}`)과 Firestore 보안 규칙은 **불변**이다. `docs/design/firebase-security-rules.md` 가 그대로 유효하다.

### 4.1 충돌 감지 — 유일하게 메커니즘이 바뀌는 지점

```
[REST 현행]
GetSave  → 문서 updateTime 캐시
PutSave  → commit 에 currentDocument.updateTime precondition
         → 400 FAILED_PRECONDITION / 409 → Conflict

[SDK 전환 — 2026-07-28 구현 중 정정]
GetSave  → 문서의 serverVersion 필드 + 존재 여부 캐시
PutSave  → RunTransactionAsync 안에서 문서 재조회
         → serverVersion 이 캐시값과 다르면 플래그 → Conflict
         → 최초 생성은 snapshot.Exists == false 로 판정
         → 쓰기 시 serverVersion 을 FieldValue.ServerTimestamp 로 갱신
```

> **정정 사유**: 초안은 `DocumentSnapshot.UpdateTime` 비교를 전제했으나 **Firebase Unity SDK 13.14.0 에는 그 프로퍼티가 없다**(`UpdateTime`·`CreateTime`·`ReadTime` 모두 부재. REST API 에 있는 필드라 SDK 에도 있으리라 가정한 것이 오류). 대신 문서가 `serverVersion` 필드로 자기 버전을 들고 다니게 한다. 캐시 타입(`Timestamp?`)과 의미론은 동일하다.
>
> **레거시 문서 3-way 분기**: 기존 REST 경로가 쓴 문서에는 `serverVersion` 이 없어, 구분하지 않으면 기존 유저의 첫 SDK 백업이 영구 거짓 충돌을 낸다. (1) 문서 없음 = 최초 생성 (2) `serverVersion` 없음 = 레거시, 충돌 아님 (3) 있음 = 비교.
>
> **범위 축소**: `serverVersion` 을 건드리지 않는 외부 편집은 충돌로 잡히지 않는다. 실사용 쓰기는 전부 `PutSaveAsync` 를 거치므로 **기기 간 실제 충돌 감지는 유지**되며, 축소분은 수동 테스트 절차에만 영향을 준다.
>
> **데이터 모델**: `saves/{uid}` 에 `serverVersion`(timestamp) 추가 → §9 문서 갱신 대상에 포함.

의미론이 동일하다 — "내가 마지막으로 본 버전 이후에 누가 썼으면 실패". 반환 타입 `CloudSaveResult` 계약이 그대로라 **배지·복원 권유 UX(계승 기획서 §3)는 코드 한 줄 바뀌지 않는다.**

### 4.2 나머지 매핑 (직역)

| REST | SDK |
|---|---|
| `documents:commit` 배치 | `SetAsync` / `UpdateAsync` / `RunTransactionAsync` |
| `runQuery` + 정규식 파싱 | `OrderBy("clearTimeMs").Limit(top)` |
| `runAggregationQuery` COUNT | `Count()` 집계 — **가용성 미확인, §10 참조** |
| 표시명 잠금 2-write commit | 동일 의미의 트랜잭션 (`displayNames/{name}` 존재 확인 → 생성 + 리더보드 `displayName` 갱신) |
| `securetoken` 수동 갱신 | SDK 자동 |

**정규식 파서가 전부 사라진다.**

---

## §5. 정리 범위 (Q1 · Q2 반영)

### 삭제

| 대상 | 비고 |
|---|---|
| `Scripts/Net/LairApiClient.cs` | Spec 1 에서 이미 사문화 |
| `Tests/EditMode/LairApiClientParseRowsTests.cs` | 89줄 |
| `Scripts/Net/FirebaseApiClient.cs` | 322줄 |
| `Scripts/Net/FirestoreJson.cs` | |
| `Tests/EditMode/FirebaseApiClientParseTests.cs` | 137줄 |
| `Tests/EditMode/FirestoreJsonTests.cs` | 38줄 |
| `Scripts/Net/NetworkConfig.cs` + `Art/Net/NetworkConfig.asset` | apiKey·projectId·baseUrl 이 `google-services.json` 으로 대체. **`EData.NetworkConfig` Addressable 엔트리와 Enum 값도 함께 정리** |
| `AuthTokenStore` 의 `Token` · `HasToken` · `SaveToken` · `ClearToken` · `RefreshToken` · `SaveRefreshToken` · `AccountId` · `HasAccountId` · `SaveAccountId` | 삭제 후 소비자 0 |
| `RankingRowDto.accountId` · `NetDtos.cs:17` 의 인증 응답 `accountId` | 후자는 `LairApiClient` 와 함께 죽는다 |
| `RankingPopup` 의 `IsMyRow` · `PickMyRow` **2-arg 레거시 폼 전체** · `RankingArg.MyAccountId` · `VillageController.cs:206` | uid 매칭이 권위 키이며, accountId 는 항상 0인 죽은 폴백 |

**`RankingPopup` 매칭 로직 정리 후 형태**: `IsMyRowByUid` / `PickMyRowByUid` 가 유일한 진입점이 되고, 폴백 순서가 **uid → clearTimeMs 시간 → (Pick 한정) 첫 행** 3단으로 줄어든다. 현행 4단(uid → accountId → 시간 → 첫 행)에서 accountId 단만 빠지는 것이며, **uid 미식별 시 시간 폴백으로 내려가는 동작은 보존**된다. 이름은 `ByUid` 접미사를 떼고 `IsMyRow`/`PickMyRow` 로 되돌린다(레거시 폼이 사라져 접미사가 무의미해짐).

`ILairApiClient.AuthenticateAsync` 의 주석 "deviceId 로 계정 보장 + 토큰/accountId 저장" 도 실제 동작(SDK 익명 인증)에 맞게 리워드한다.

### 유지

- `ILairApiClient` + 결과 타입 + `SaveResponseBody` · `RankingRowDto`(accountId 제외)
- `CloudSaveService` · `RankingClient` · `FakeLairApiClient`
- `AuthTokenStore.GetOrCreateDeviceId()` — **인증과 무관해진다.** `VillageViewModel.cs:56` 이 자동 표시명 `영주 #xxxx` 의 시드로 쓴다. 주석을 그 용도로 리워드
- `AuthTokenStore.Uid` · `HasUid` · `SaveUid` — 랭킹 "내 행" 식별(`VillageController.cs:205`). SDK `CurrentUser.UserId` 로도 얻을 수 있으나, UI 계층이 Firebase 타입을 보면 §3.1 을 위반하므로 **PlayerPrefs 캐시를 유지한다**

---

## §6. 에러 처리

기존 계약을 그대로 재현한다 — **실패는 예외를 밖으로 던지지 않고** `null` / 빈 리스트 / `CloudSaveResult.Failed` / `DisplayNameStatus.Offline` 로 흡수한다. SDK 는 `FirebaseException`(비동기 경로에선 `AggregateException` 으로 래핑)을 던지므로, 각 op 를 try/catch 로 감싸 기존 결과 타입에 매핑한다.

| 상황 | 매핑 |
|---|---|
| 표시명 잠금 문서 이미 존재 | `DisplayNameStatus.Taken` |
| 트랜잭션 abort (UpdateTime 불일치) | `CloudSaveResult.Conflict` |
| 네트워크 실패 · 미인증 · 모듈 꺼짐 | `Offline` / `Failed` / 빈 리스트 |
| 세이브 문서 없음 | `GetSaveAsync` → `null` (= "세이브 없음", 기존과 동일) |

**해소되는 한계 1건** — REST 의 "1시간 넘기면 클라우드 op 가 조용히 실패"(`firebase-security-rules.md` 알려진 한계 4번)가 사라진다. SDK 가 토큰 갱신을 자동 처리한다.

---

## §7. 테스트 전략

- **EditMode 는 `FakeLairApiClient` 기반 계약 테스트만 남는다.** 정규식 파서가 사라지므로 파서 테스트도 함께 사라진다. `CloudSaveServiceTests` · `CloudSaveServiceEdgeTests` · `CloudSaveRoundTripTests` · `CloudConflictFlagTests` · `NetDtoMappingTests` · `RankingClientTests` · `RankingMyRowMatchTests` 는 **전부 그대로 통과해야 한다**(회귀 게이트).
- **예외 — `RankingMyRowMatchTests` 는 실질 재작성이다.** 161줄 중 절반가량(`IsMyRow_accountId일치…` · `IsMyRow_accountId불일치…` · `IsMyRow_row의accountId가0이면…` · `PickMyRow_accountId일치행을…` · `PickMyRow_accountId없으면…` · `PickMyRowByUid_uid일치없으면_accountId폴백으로…` 및 2-arg 리플렉션 헬퍼 2종)이 삭제되는 accountId 경로 전용이다. **남는 것**: uid 일치/불일치, uid 미식별 시 시간 폴백, 동률 시 첫 매칭만(중복 강조 방지), Pick 의 첫 행 폴백. 이 4개 축의 커버리지는 정리 후에도 유지해야 한다.
  - `NetDtoMappingTests` 가 `RankingRowDto.accountId` 를 참조하면 동일하게 정리한다.
  - **판정 기준**: 정리 후 `RankingPopup` 의 남은 분기(uid 일치 / 시간 폴백 / 첫 행)가 전부 테스트로 덮여 있어야 한다. 테스트 수가 줄어드는 것은 정상이나, 커버되지 않는 분기가 생기면 §5 를 잘못 적용한 것이다.
- `AuthTokenStoreTests` — 삭제된 멤버 테스트를 걷어내고 DeviceId · Uid 만 남긴다.
- **SDK 실통신은 EditMode 범위 밖이다.** 검증은 §8 게이트(실기기/에디터 플레이)가 담당한다.

### 7.1 PlayMode 상호작용 — 결정 필요

`VillageController.cs:46` 이 `MetaSession.EnsureNetworkAsync()` 를 await 하고, `VillageSmokePlayTests` 가 마을 진입을 구동한다. 현행에서는 이 경로가 무해하게 실패한다(Addressables 로드 + REST 실패 → `Api = null` → 오프라인). SDK 전환 후에는 **`CheckAndFixDependenciesAsync` 가 에디터 PlayMode 테스트 러너 안에서 실행된다.**

EditMode 스위트가 전부 통과하더라도 PlayMode 가 멈추거나 예외를 던질 수 있으므로, 구현 시 다음 중 하나를 **명시적으로 선택**한다:

| 안 | 내용 | 트레이드오프 |
|---|---|---|
| A | PlayMode 테스트에서 Firebase 초기화를 건너뛰도록 가드 (테스트 전용 플래그 또는 `Application.isEditor` 분기) | 스모크가 빨라지고 결정적. 실통신 경로는 테스트 밖에 남음 |
| B | 초기화를 그대로 태우되 타임아웃·실패를 오프라인으로 흡수 | 실제 부팅 경로에 가깝지만 테스트가 네트워크에 의존 |
| C | `VillageSmokePlayTests` 에서 마을 진입 전 `MetaSession.Api` 를 가짜로 선주입 | `Api != null` 조기 반환으로 초기화 자체를 회피. 기존 오프라인 계약 활용 |

**판정 기준**: PlayMode 스위트가 네트워크 없이 결정적으로 통과해야 한다. 게이트 B 이전에 이 선택이 확정되어야 한다.

---

## §8. 설치 · 검증 게이트 (2단 분리)

게이트는 **둘로 나눈다.** 하나로 두면 통과 기준이 그 시점에 존재하지 않는 코드를 요구하게 된다 — `saves/{uid}` 문서 생성은 `PutSaveAsync` 가, uid 안정성은 uid 영속 경로가 있어야 확인 가능한데, 설치 직후에는 둘 다 없다.

### 8.1 게이트 A — 설치 성립 (plan 첫 Task, 롤백 결정 지점)

**사용자가 수행** (에디터 GUI 작업):

1. Firebase Unity SDK 설치 — Auth + Firestore (계획은 UPM(`.tgz`) 을 전제했으나 **실제로는 `.unitypackage` 로 설치됨**, `Assets/Firebase/`, SDK 13.14.0 — §2 Q5 참조)
2. `google-services.json`(`lair-970fa`, `com.chvj.lair`) → `Assets/` 배치
3. `ProjectSettings` — 패키지명 `com.UnityTechnologies.com.unity.template.urpblank` → `com.chvj.lair`, companyName `DefaultCompany` → `chvj`
4. `Tools/ChvjUnityInfra/Settings` → Firebase 탭 → **Use Firebase 체크**

**통과 기준** — 설치 자체가 성립하는가만 본다:

- SDK 임포트 후 **프로젝트가 컴파일된다** (EDM4U gradle 템플릿 생성 포함)
- `CHMFirebase` 초기화(`CheckAndFixDependenciesAsync`)가 `Available` 로 끝난다
- **익명 로그인 1회 성공** — 일회용 에디터 스크립트 또는 최소 부트스트랩으로 확인. **`ILairApiClient` 경유가 아니다** (아직 구현체가 없다)

**실패 시**: 여기서 멈추고 사용자에게 보고. **REST 유지 롤백을 판단한다.** 이 시점엔 기존 코드를 아무것도 지우지 않았으므로 롤백 비용이 SDK 제거뿐이다.

### 8.2 게이트 B — 동작 검증 (`FirebaseSdkApiClient` 완성 후, §5 삭제 **전**)

- 마을 진입 시 `[MetaSession] 익명 인증 실패` 로그가 없다
- Firebase 콘솔 Firestore 에 **`saves/{uid}` 문서가 생성된다**
- **앱 재실행 후 uid 가 동일하다** — SDK 자격증명 영속화 확인
- 랭킹 팝업이 Top 목록과 내 순위를 표시한다

> **Task 순서 제약**: §5 의 삭제는 **게이트 B 통과 이후** Task 에 배치한다. 게이트 B 전에 지우면 롤백 시 되살릴 것이 늘어난다. 즉 순서는 `게이트 A → 인프라 모듈 → FirebaseSdkApiClient → 게이트 B → 삭제·정리 → 문서 갱신`.

---

## §9. 문서 갱신

| 문서 | 변경 |
|---|---|
| `CLAUDE.md` §8 | "REST over `CHMHttpNetwork`" → "Firebase Unity SDK(Auth + Firestore)". 백엔드가 Firebase BaaS 라는 취지·범위 규칙은 유지 |
| `CLAUDE.md` §9 | **변경 없음** — "백엔드(보안 규칙·Cloud Functions)를 이 레포에 쓰지 않는다" 는 그대로 유효 |
| `.claude/rules/03-chvjpackage.md` §1 | 모듈 목록에 `Firebase` 추가 |
| `docs/design/firebase-security-rules.md` | 데이터 모델·규칙 불변. "알려진 한계" 의 **1시간 토큰 항목 해소** 반영 |

---

## §10. 알려진 리스크 · 한계

- **SDK 설치 가능성 미확인** — Unity 6 (6000.0.68f1) / URP 17 에서의 임포트, EDM4U 가 생성하는 gradle 템플릿과 Addressables Android 빌드의 공존, APK 용량 증가폭이 전부 미검증. `Assets/Plugins/Android` 가 없어 EDM4U 가 템플릿을 새로 만든다. §8 게이트가 이 리스크를 흡수한다.
- **Firebase 어셈블리 참조 방식 미확정 — 신규 asmdef 와 `Lair.asmdef` 양쪽에 걸린다.** Firebase UPM 패키지가 **asmdef 를 제공**하면 이름으로 명시 참조해야 하고(`com.chvj.unityinfra.social.asmdef` 이 `"GooglePlayGames"` 를 명시하는 것과 동일), **자동 참조 precompiled DLL 로만** 배포되면 `overrideReferences: false` 인 어셈블리가 자동으로 본다.
  - `Lair.asmdef` 의 `overrideReferences: false` 는 **후자의 경우에만** 배선이 공짜라는 뜻이다. asmdef 배포라면 `Lair.asmdef` 의 `references` 에도 항목을 추가해야 하며, 그 경우 §3 파일 표에 `Lair.asmdef` (수정) 이 추가된다.
  - **해소 시점**: 게이트 A 직후(SDK 설치 실물 확인). 두 갈래 모두 구현 난이도는 같고 편집 지점만 다르다.
- **Firestore Unity SDK 의 집계 쿼리(`Count()`) 가용성 미확인** — `GetMyRankAsync` 의 절대 등수는 `COUNT(clearTimeMs < 내기록) + 1` 로 계산하는데, Unity Firestore SDK 는 네이티브 SDK 대비 집계 지원이 늦은 이력이 있어 REST 와의 기능 동등을 가정할 수 없다.
  - **가용하면**: §4.2 매핑대로 직역.
  - **없으면 폴백**: Top N 조회 결과로 클라이언트 계산. 내 기록이 Top N 밖이면 순위를 "N+위" 로 표기하거나 내 순위 행을 숨긴다 — **어느 쪽인지는 game-designer 단계에서 확정**(UX 결정). 읽기 비용도 함께 오른다(랭킹 1회 열람이 이미 ~102 reads, 무료 한도 5만/일 기준 하루 약 490회).
  - **해소 시점**: 게이트 A 직후. `FirebaseSdkApiClient` 의 `GetMyRankAsync` 를 쓰기 **전**에 확정한다.
- **익명 인증은 여전히 기기이전을 못 푼다** — 재설치·새 기기 = 새 uid. 본 전환은 연동 방식만 바꾸며 이 한계를 건드리지 않는다(Spec 2 소관).
- **랭킹 안티치트 없음** — 보안 규칙이 "본인 uid 문서에만 쓰기"까지만 강제한다. v0.3 수용 범위(계승 기획서 §2.2).
- **플레이어 대면 변화 0** — 본 전환은 UX 를 설계하지 않는다. 새 화면·새 문구가 생겼다면 범위 이탈이다.

---

## Self-Review

- **Placeholder 스캔**: TBD·TODO·빈 섹션 0건. §10 의 "미확정" 3건과 §7.1 의 선택 1건은 placeholder 가 아니라 **해소 시점을 명시적으로 지정한 미지수**다 — SDK 설치 가능성(게이트 A), asmdef 참조 방식(게이트 A 직후), 집계 쿼리 가용성(게이트 A 직후, `GetMyRankAsync` 착수 전), PlayMode 초기화 처리(게이트 B 이전).
- **검토 반영 — 게이트 분할**: 초안은 게이트를 하나로 두고 통과 기준에 `saves/{uid}` 생성·uid 안정성을 넣었는데, **그 시점엔 `PutSaveAsync` 도 uid 영속 경로도 없어 확인 불가능**했다. 게이트 A(설치 성립·롤백 결정)와 게이트 B(동작 검증·삭제 직전)로 분리하고 Task 순서를 명시했다.
- **검토 반영 — 근거 수정 2건**: (a) Q5 의 UPM 선택을 "Rule 03 §1 위반 회피" 로 정당화했으나 그 룰은 게임↔패키지 방향에 대한 것이라 서드파티 DLL 참조에 적용되지 않는다 → **위생 선택**으로 근거를 정정. (b) "`Lair.asmdef` 의 `overrideReferences: false` 라 게임 배선이 공짜" 는 **자동 참조 DLL 배포일 때만** 성립한다(asmdef 배포면 명시 참조 필요, `social.asmdef` 이 `"GooglePlayGames"` 를 명시하는 것이 반례) → §10 에서 양 갈래로 서술.
- **내부 일관성**: "UX 불변" 주장이 §1·§4.1·§10 에서 동일. §3.1 Firebase 타입 격리와 §5 의 `AuthTokenStore.Uid` 유지 근거가 서로를 지지한다(UI 가 SDK 타입을 못 보므로 캐시 필요). §5 삭제 목록과 §7 테스트 영향이 1:1 대응.
- **범위 체크**: 단일 plan 으로 분해 가능. 구글 로그인을 명시적으로 배제(§1)해 Spec 2 와 경계가 갈린다.
- **조건부 서술 제거**: 초안의 §7 은 "`RankingMyRowMatchTests` 가 accountId 폴백을 테스트하고 있으면 그 부분만 제거" 라는 조건문이었다. 실제 파일을 확인해 **161줄 중 절반가량이 accountId 전용이고 `IsMyRow`/`PickMyRow` 2-arg 폼 자체가 삭제 대상**임을 확인, 조건문을 단정 + 남겨야 할 커버리지 4축 + 판정 기준으로 교체했다. §5 에도 정리 후 매칭 로직의 최종 형태(3단 폴백)를 명시했다.
- **모호성 체크**: Q1~Q5 가 모두 한 갈래로 락. "사문화 코드 처리"·"정리 범위"·"검증 시점"·"래핑 위치"·"설치 형식" 이 각각 단정돼 구현자가 재해석할 여지가 없다. §8 의 Task 순서 제약(삭제는 게이트 이후)을 명시해 plan 작성 시 순서 오류를 차단했다.
- **Spec 1 전제 승계 여부**: Spec 1 plan 의 "`ILairApiClient` 시그니처 불변" 회귀 게이트는 **본 spec 에서도 유효**하다(메서드 추가 없음). 단 `RankingRowDto.accountId` 필드 제거가 DTO 변경이므로, 회귀 게이트는 "시그니처 불변"이 아니라 **"`FakeLairApiClient` 기반 테스트 전부 통과(accountId 참조부만 정리)"** 로 정확히 진술했다(§7).
