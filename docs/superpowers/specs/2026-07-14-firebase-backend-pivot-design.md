# Firebase 백엔드 피벗 (Spec 1 — 익명 인증) — spec

- **작성일**: 2026-07-14
- **단계**: Project Lair v0.3 (서버 연동 클라이언트)
- **분해**: 이 작업은 두 spec 으로 순차 진행한다.
  - **Spec 1 (본 문서)** — Firebase 백엔드 피벗 + 익명 인증. 지금 착수·테스트 가능. "서버 운영 0" 이득 실현.
  - **Spec 2 (후속, 별도 브레인스토밍)** — 구글 로그인 + 계정 연동. 기기이전을 실제로 푸는 단위. 플랫폼 설정(SHA-1 등)·실기기 테스트 복잡도를 따로 짊어짐.
- **선행 문서**: 기존 자체 서버 연동 spec/plan/기획서(`docs/superpowers/specs/2026-06-15-unity-server-integration-design.md` 외)의 **인터페이스 계약·UX 는 그대로 계승**하고, 백엔드 구현만 교체한다.

---

## §1. 의도 / 목적

v0.3 서버 연동 방향을 **자체 서버(`Project_Lair_Server`, ASP.NET Core+MySQL+Redis)에서 Firebase BaaS 로 전환**한다.

- **동기**: 백엔드 서버 운영/관리 부담 제거(머신·배포·DB 인프라). 사용자 결정.
- **핵심 통찰 (이 피벗을 저위험으로 만드는 근거)**: 모든 소비자(UI·`CloudSaveService`·`MetaSession`·`RankingClient`)와 전체 테스트가 **`ILairApiClient` 인터페이스에만 의존**한다. 테스트는 `FakeLairApiClient` 를 주입한다. 따라서 인터페이스를 그대로 두고 **구현체 1개를 새로 만들어 조립 지점만 교체**하면 변경 반경이 최소화된다.
- **범위 승격**: 이 전환은 CLAUDE.md §8 을 "자체 서버"→"Firebase BaaS" 로 재작성하며, **계정 연동(구글 로그인)을 "범위 밖"에서 "범위 내(Spec 2)"로 승격 예고**한다.

## §2. 확정 결정 (브레인스토밍 2026-07-14)

| # | 결정 | 값 | 근거 |
|---|---|---|---|
| Q1 | Firebase 접근 방식 | **REST over `CHMHttpNetwork`** (네이티브 SDK 안 씀) | 기존 HTTP 인프라 재사용, 네이티브 플러그인 0, `google-services.json` 불필요. Rule 03(ChvjPackage 우선) + §8("연동 클라이언트 코드만") 부합. Firestore REST 가 8개 메서드 전부 커버(`runAggregationQuery` COUNT, `updateTime` precondition 포함) |
| Q2 | 리더보드 "내 순위" | **절대 순위 유지** | COUNT 쿼리 1회 + 범위 쿼리로 ±주변. "내가 정확히 몇 등" = v0.3 가설(경쟁 동기)에 직접 기여. v0.3 규모에선 읽기 비용 미미 |
| Q3 | 표시명 유일성 | **유지 (잠금 문서 패턴)** | `displayNames/{이름}` 문서 + 원자 커밋 + 보안 규칙. 기존 `DisplayNameStatus.Taken` UX 보존 |
| Q4 | 기존 자체 서버 | **폐기 — Firebase 일원화** | 조립 지점을 Firebase 구현체로 교체. `LairApiClient`(HTTP) 코드는 남되 사문화. `Project_Lair_Server` 아카이브 |
| Q5 | 분해 | **두 spec 순차** | Spec 1 = 본 문서(익명 인증). Spec 2 = 구글 로그인+연동 |

## §3. 범위

### 이번 단계 (Spec 1)

- 신규 `FirebaseApiClient : ILairApiClient` — Firebase Auth REST + Firestore REST 로 8개 메서드 재구현.
- `MetaSession.Net.cs` 조립 지점 교체 (`LairApiClient` → `FirebaseApiClient`).
- `NetworkConfig` 를 Firebase 설정(apiKey·projectId)으로 전환.
- `AuthTokenStore` 를 Firebase 자격증명(idToken·refreshToken·uid) 저장으로 전환.
- 리더보드 "내 행" 식별을 `string uid` 기준으로 (uid 가 문자열이라 필수).
- Firestore 데이터 모델(문서 구조) + 보안 규칙 설계 문서화.
- 얇은 `FirestoreJson` 빌드/파싱 헬퍼(REST 타입 JSON ↔ 값).
- CLAUDE.md §8 재작성.

### 제외 (이번 단계 밖)

- **구글 로그인 / 계정 연동** → Spec 2.
- **서버 권위 안티치트** → 리더보드는 클라가 clearTimeMs 를 직접 쓸 수 있어 치팅 가능. Firestore-only 의 직접적 귀결로 명시(§8). 미래 안티치트는 Cloud Functions(서버리스지만 코드) 필요 — 범위 밖.
- **필드 단위 세이브 머지** → 전체 MetaProfile 단위 백업/복원 유지, 충돌은 `updateTime` precondition.
- 신규 영웅/몬스터/카드 리소스.
- 리더보드/클라우드 **UX·문구·레이아웃 신규 설계** → 기존 `unity-server-integration.md` 기획서 계승. 이번엔 백엔드 교체만.

## §4. 아키텍처 / 변경 반경

| 변경 유형 | 대상 | 내용 |
|---|---|---|
| **신규** | `FirebaseApiClient : ILairApiClient` | 8개 메서드를 Firestore/Auth REST 로 재구현 (`CHMHttpNetwork`) |
| **신규** | `FirestoreJson` (헬퍼) | REST 타입 JSON(`{"stringValue":...}`) 빌드/파싱 |
| **수정** | `MetaSession.Net.cs` (조립 지점, 현 45행) | `new LairApiClient(config)` → `new FirebaseApiClient(config)` |
| **수정** | `NetworkConfig` | `_baseUrl` → `_firebaseApiKey` + `_firebaseProjectId` (TimeoutSec 유지) |
| **수정** | `AuthTokenStore` | `string Uid` + `RefreshToken` 저장 **추가**. 기존 `long AccountId` 는 **제거하지 않고 사문화로 잔존** — 제거 시 `VillageController`·기존 `AuthTokenStoreTests`·`RankingMyRowMatchTests` 컴파일 파손(실측). deviceId 는 기본 표시명 생성용 잔존 |
| **미세수정** | `RankingRowDto` + `RankingPopup` + `VillageController` | "내 행" 식별에 `string uid` 매칭을 **1순위로 추가**. 기존 `long accountId`·`BestClearTime` 폴백은 잔존(제거 안 함). `VillageController` 는 RankingArg 에 `MyUid = AuthTokenStore.Uid` 추가 전달 |
| **불변** | `ILairApiClient` · `CloudSaveService` · `MetaSession` 로직 · 전 UI · 전 테스트(`FakeLairApiClient`) | 재구현으로 흡수 |
| **사문화** | `LairApiClient` (HTTP REST) | 코드 잔존(수정 없음 — `AccountId` 유지되므로 컴파일 정상), 조립에서 제외 (Q4) |

> **인터페이스 계약 주의**: `ILairApiClient` 시그니처 자체는 불변이다. `AuthTokenStore` 에 `Uid`·`RefreshToken` 추가, `RankingRowDto` 에 `uid` 추가, `RankingPopup`/`VillageController` 에 uid 매칭 경로 추가 — 모두 **additive(제거 없음)**. 순수 "무변경 드롭인"은 아니지만 기존 식별 경로(accountId·시간)를 남긴 채 uid 를 1순위로 얹을 뿐이라 컴파일 파손이 없다.

## §5. Firestore 데이터 모델 (DB 구조)

```
saves/{uid}                         유저별 세이브 1건
  profile:       string    JsonUtility.ToJson(MetaProfile) 통짜 문자열 — 기존 직렬화 무손실
  schemaVersion: int       마이그레이션 대비
  updatedAt:     timestamp

leaderboard/{uid}                   유저별 최고기록 1건 (doc id = uid → 자연 upsert)
  uid:         string       (== doc id)
  displayName: string
  clearTimeMs: int          정렬·순위 대상 (단일 필드 인덱스 자동)
  hero:        string
  createdAt:   timestamp

displayNames/{정규화이름}            표시명 유일성 잠금 문서 (Q3)
  uid: string               이 이름을 점유한 계정
```

- **`saves.profile` 를 통짜 문자열 1필드로**: MetaProfile 을 Firestore 타입별로 분해하지 않고 기존 JsonUtility 직렬화 그대로 저장. 스키마 진화에 강함.
- **`leaderboard` doc id = uid**: 유저당 1건 강제 + 제출 = upsert(PATCH). `clearTimeMs` 만 실수형 필드라 orderBy/count 가능.
- **인덱스**: `leaderboard.clearTimeMs` 오름차순 단일 필드(Firestore 자동 인덱싱) 으로 orderBy + `count(clearTimeMs < 내기록)` 충족.

## §6. 엔드포인트 매핑 (8개 메서드)

> **쓰기 동사 제약 (확정 사실)**: `CHMHttpNetwork` 는 `GetAsync`/`PostAsync`/`PutAsync` 만 제공하며 **PATCH·DELETE 가 없다.** Firestore 문서 수정/삭제는 원래 PATCH·DELETE 를 쓰지만, 본 설계는 **모든 쓰기를 `documents:commit`(POST) 배치로 처리**한다 — precondition·다중 write·delete 를 한 POST 로 원자 실행. 패키지(ChvjPackage) 를 건드리지 않는다.

| `ILairApiClient` 메서드 | Firebase REST |
|---|---|
| `AuthenticateAsync` | refreshToken 있으면 `securetoken.googleapis.com/v1/token` 갱신; 없으면 `identitytoolkit.googleapis.com/v1/accounts:signUp`(익명). idToken(1h)·refreshToken·localId(uid) 저장. 401 시 refresh 후 1회 재시도 |
| `GetSaveAsync` | `GET saves/{uid}` → profile 문자열 파싱 → MetaProfile. 없으면 null. 응답의 `updateTime` 을 로컬 캐시(충돌용) |
| `PutSaveAsync` | `documents:commit`(POST) — `update saves/{uid}` + `currentDocument.updateTime`(캐시값) precondition. 불일치 → `CloudSaveResult.Conflict`. 최초 저장은 `exists=false` precondition 으로 생성 |
| `SubmitScoreAsync` | `documents:commit`(POST) — `update leaderboard/{uid}` upsert (기존보다 빠를 때만 — BattleController 기존 best 로직 유지) |
| `GetTopAsync(top)` | `documents:runQuery`(POST) orderBy `clearTimeMs` asc, limit top |
| `GetMyRankAsync` | `documents:runAggregationQuery`(POST) COUNT(`clearTimeMs < 내기록`) → 등수 = count+1; + `runQuery` 범위 쿼리 2회로 ±주변 행. `List<RankingRowDto>` |
| `ChangeDisplayNameAsync` | `documents:commit`(POST) 원자 배치 — `displayNames/{새이름}` 생성(`exists=false`, 실패 시 Taken) + `update leaderboard/{uid}.displayName`. **옛 이름 잠금 삭제는 생략**(아래 주) |

> **옛 이름 잠금 삭제 생략 (spec↔plan 정합, 의식적 결정)**: 이론상 이름 변경 시 옛 `displayNames/{옛이름}` 을 delete 해 네임스페이스를 회수해야 하나, 클라가 직전 이름을 신뢰성 있게 알지 못할 수 있어 **생략한다.** 결과로 옛 이름 잠금이 고아로 남아 그 이름은 (같은 uid 재점유 외엔) 다시 못 쓰게 되지만, 유일성 보장 자체는 `exists=false` 생성이 담당하므로 **깨지지 않는다.** v0.3 프리론치에서 네임스페이스 영구 점유는 YAGNI 로 수용. plan Task 6 이 이 생략을 구현한다.

### 충돌 재현 상세

- 복원/로드(`GetSaveAsync`) 시 문서 `updateTime` 을 로컬 캐시.
- `PutSaveAsync` 시 그 값을 `currentDocument.updateTime` precondition 으로 `:commit` 전송.
- 다른 기기가 그 사이 수정 → precondition 실패 → `Conflict`. 기존 §3 충돌 UX(배지·복원 권유)가 코드 변경 없이 작동.
- **상태코드 매핑 (방어적)**: precondition 실패는 Firestore 가 `FAILED_PRECONDITION` 으로 응답하며 HTTP 코드는 버전/경로에 따라 400 또는 409 로 관측될 수 있다. `Conflict` 판정을 **`StatusCode == 409` 또는 (`StatusCode == 400` 이고 본문에 `"FAILED_PRECONDITION"` 포함)** 둘 다로 커버해 어느 쪽이든 정확히 충돌로 분류한다.

### "내 행" 식별 상세

- `RankingRowDto` 에 `string uid` 추가, `AuthTokenStore.Uid` 와 매칭. Firebase uid 가 문자열이라 기존 `long accountId` 매칭은 대체된다.
- 폴백: uid 부재 시 `MetaProfile.BestClearTime` 시간 일치(유저당 1건이라 유일 식별) — 기존 기획서 §4 폴백 유지.

## §7. 보안 규칙 (Firestore Security Rules — 서버 부재의 권위 계층)

```
match /saves/{uid}         { allow read, write: if request.auth.uid == uid; }
match /leaderboard/{uid}   { allow read: if true;
                             allow write: if request.auth.uid == uid; }
match /displayNames/{name} { allow read: if true;
                             allow create: if request.auth.uid == request.resource.data.uid;
                             allow delete: if request.auth.uid == resource.data.uid; }
```

- **치팅 가능 명시**: `leaderboard` 쓰기가 본인 uid 에 한정될 뿐 `clearTimeMs` 값은 클라가 임의로 쓸 수 있다. 안티치트는 v0.3 범위 밖(§3). 서버 없는 BaaS 의 직접 귀결이며, 미래 안티치트 경로는 Cloud Functions(서버리스지만 코드)뿐임을 기록.

## §8. 설정 / 문서 정리

- `NetworkConfig.asset`: `_firebaseApiKey`, `_firebaseProjectId` 채움. Firebase 웹 API 키는 클라 노출 전제(설계상 정상) — 보안은 규칙이 담당.
- **CLAUDE.md §8 재작성** (구현 단계에서 반영):
  - 서버 연동 근거 "자체 서버(ASP.NET Core+MySQL+Redis)" → "Firebase BaaS(Auth + Firestore, REST 연동)".
  - `Project_Lair_Server` 폐기 명시.
  - 계정 연동(구글 로그인)을 범위 내로 승격 예고(Spec 2).

## §9. 알려진 한계 (정직 명시)

- **익명 인증은 기기이전을 못 푼다** — 재설치/새 기기 = 새 uid = 옛 세이브 접근 불가. Firebase 가 새로 망가뜨리진 않지만 고치지도 않는다. 진짜 기기이전은 Spec 2(구글 로그인+계정 연동) 소관.
- v0.3 "클라우드 세이브"가 지키는 실가치 = **같은 설치본 내 백업/복원 + 리더보드 경쟁**.

## §10. 테스트 방향 (개요 — 상세는 plan)

- `FakeLairApiClient` 기반 기존 테스트는 **그대로 통과해야 한다**(인터페이스 불변 회귀 게이트).
- 신규: `FirestoreJson` 헬퍼 단위 테스트(타입 JSON 빌드/파싱 라운드트립).
- 신규: `FirebaseApiClient` 의 응답 파싱 분기(성공/404/FAILED_PRECONDITION/malformed) 테스트 — HTTP 는 `CHMHttpNetwork` 경유라 결과 객체 주입 가능 여부에 따라 파서 함수를 정적 분리해 테스트.
- Firestore 실통신은 EditMode 범위 밖(실 프로젝트 키 필요) — 파싱/매핑 로직을 순수 함수로 분리해 커버.

## Self-Review

- **Placeholder 잔존**: 0건. 8개 메서드 전부 REST 매핑 확정(§6). 충돌·내행식별·유일성 커밋 절차 단정.
- **내부 일관성**: `string uid` 식별이 §4·§5·§6 에서 동일. `saves.profile` 통짜 문자열이 §5·§6 일치. 치팅 가능 명시가 §3·§7 동일.
- **스코프**: Spec 1 단일 구현 단위(FirebaseApiClient + 조립/저장소 인접 변경). 구글 로그인은 Spec 2 로 분해 완료.
- **모호 표현**: "적절히/유연하게" 0건. 폴백(시간 일치)·precondition 분기 단정.
- **인터페이스 계약**: `ILairApiClient` 불변 단정 + `AuthTokenStore`/`RankingRowDto` 인접 변경 2지점 명시(순수 드롭인 아님을 정직 기록, §4).
