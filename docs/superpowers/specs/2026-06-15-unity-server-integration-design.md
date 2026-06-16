# Unity 클라이언트 ↔ 서버 연동 설계 (Spec)

- **작성일**: 2026-06-15
- **단계**: Project Lair v0.3 (서버 연동 클라이언트)
- **상태**: 확정 (브레인스토밍 승인)
- **서버**: 별도 레포 `Project_Lair_Server` (github.com/chvj7567/Project_Lair_Server, ASP.NET Core+MySQL+Redis) — 구현·테스트 완료
- **범위**: 이 Unity 레포에는 **연동 클라이언트 코드만** 들어온다. 서버 구현은 손대지 않는다 (CLAUDE.md §8/§9).

> **네이밍 갱신(2026-06-15 구현 후)**: 클라 "Leaderboard/리더보드/순위표" → "Ranking/랭킹"(코드·EUI·프리팹·라벨), Cloud 버튼/팝업 라벨 → "계정"(Cloud 코드 식별자·서버 `/leaderboard` 경로는 불변).

> 이 문서는 **무엇을** 만들지의 골격(의도·범위·결정 락)이다. 파일별 단계·시그니처는 plan, 수치·UX·표시명 등 도메인은 game-designer 기획서가 담당.

---

## 1. 목적 / 가설

v0.2의 로컬 메타 진행을 계정 기반 클라우드로 확장한다. 검증 가설(v0.3):
**"클라우드 계정·세이브·리더보드(서버 연동)가 재방문·기기이전·경쟁 동기를 강화하는가."**

제공 가치:
- 기기 분실/교체에도 진행 보존 (클라우드 세이브)
- 진행이 한 계정에 귀속 (익명 기기ID 인증)
- 최단 클리어 경쟁 (리더보드 제출 + 조회)

## 2. 범위

### 포함
1. **익명 인증** — deviceId(GUID, 1회 생성) → `POST /auth/anonymous` → JWT 로컬 저장 → 이후 Bearer.
2. **클라우드 세이브 (하이브리드)** — 자동 백업(로컬 저장 직후 best-effort `PUT /save`) + 수동 복원(마을 버튼 `GET /save`). 409 충돌은 복원 프롬프트.
3. **리더보드** — 승리 시 자동 제출(`POST /leaderboard/submit`) + 마을 조회 화면(`GET /leaderboard?top=N`, `GET /leaderboard/me`).
4. **인프라** — ChvjPackage 에 경량 `CHMHttpNetwork`(HTTP) 모듈 신규 추가.

### 제외 (이번 단계 밖)
- 소셜 로그인(서버 스키마 자리만 존재) · 서버 권위 안티치트 · 서버 구현 변경
- 필드 단위 세이브 머지(충돌은 전체 프로필 단위)

### §8 메모
방금 v0.3 승격된 §8에 "리더보드 UI 신규 화면은 이후 단계"로 적혀 있으나, **사용자 결정으로 조회 화면을 이번 범위에 포함**한다. game-designer 가 기획서 작성 시 §8 문구를 이 결정에 맞게 정리한다.

## 3. 아키텍처 (계층)

```
ChvjPackage (인프라)
└─ CHMHttpNetwork            UnityWebRequest 기반 async HTTP 래퍼
                         (GET/POST/PUT JSON · Bearer 헤더 · 타임아웃 · 성공/HTTP상태/에러 결과 타입)

Assets/_Lair (게임)  — namespace Lair.Net
├─ NetworkConfig (SO)    baseUrl · timeout
├─ AuthTokenStore        deviceId(GUID 1회) + JWT 저장 (PlayerPrefs)
├─ ILairApiClient        엔드포인트 추상화 (테스트용 가짜 주입 가능)
│   └─ LairApiClient     CHMHttpNetwork + NetworkConfig + token 묶음 구현
├─ CloudSaveService      백업 / 복원 / 409 충돌 표면화 (MetaProfile ↔ JSON)
└─ LeaderboardClient     제출 + Top N / 내 순위 조회
```

설계 원칙: 서비스는 Model/ViewModel 계층, 팝업은 View. `ILairApiClient` 추상화로 네트워크 없이 서비스 단위 테스트 가능 (Rule 02 §5·§6).

## 4. DTO 매핑

- 서버 `MetaProfileDto` 필드명이 Unity `MetaProfile`(`Version`·`Souls`·`LordXp`·`ShopLevels`·`AchievedIds`·`SeenMonsters`·`PickedCards`·`TotalRuns`·`TotalWins`·`BestClearTime`·`SelectedHero`·`LordRewardGrantedLevel`)과 동일.
- → **`MetaProfile`을 `JsonUtility`로 그대로 직렬화**해 동기 본문에 사용. 별도 클라이언트 DTO 클래스 불필요.
- 동기 요청 래퍼: `{ profile, schemaVersion, clientUpdatedAt }` (JsonUtility 직렬화 가능한 wrapper 클래스).

## 5. 인증 흐름

1. 마을 진입 시 토큰 확인. 없으면:
2. `AuthTokenStore`가 deviceId(없으면 GUID 생성·저장) 확보 → `POST /auth/anonymous {deviceId}`.
3. 응답 `{accountId, token}` 저장. 이후 모든 요청 `Authorization: Bearer <token>`.
4. **지연 인증** — 첫 클라우드 작업 직전 보장. 실패 시 게임 진행은 그대로(로컬), 클라우드 기능만 비활성.

## 6. 클라우드 세이브 흐름

- **자동 백업** — `VillageController.HandleProfileChanged`(로컬 `Save` 직후) 훅에 best-effort `PUT /save` 추가. fire-and-forget, 실패는 로그만(게임 흐름 차단 금지).
- **수동 복원** — 마을 메뉴에 클라우드 항목 → 복원 버튼. 복원은 로컬 덮어쓰기라 **확인 프롬프트** 후 `GET /save` → `MetaProfile` 교체 → 로컬 Save → VM 갱신.
- **409 충돌** — 백업 중 서버가 더 최신(409)이면 "서버 데이터로 복원할까요?" 프롬프트. 예 → 복원, 아니오 → 무시(로컬 유지).
- `clientUpdatedAt` — 로컬에 마지막 동기 시각 보관해 요청에 포함.

## 7. 리더보드 흐름

- **제출** — 승리 종료 지점(BattleController/ResultPopup)에서 클리어타임(ms)·영웅·표시명으로 `POST /leaderboard/submit`. best-only 판정은 서버. best-effort.
- **조회** — 마을 `LeaderboardPopup`(UIBase): `GET /leaderboard?top=100` + `GET /leaderboard/me`(내 순위 ±주변). Rule 03 BuildModalPopup 패턴 — `LeaderboardCell` + `LeaderboardPoolingScrollView`.
- **표시명** — 기본값(예: deviceId 파생) 또는 간단 입력. 구체안은 기획서에서.

## 8. 에러 / 오프라인 정책

- `CHMHttpNetwork`는 성공·HTTP상태·네트워크에러를 결과 타입으로 반환(예외 throw 지양).
- 서버 불통/타임아웃 → **조용히 로컬 유지**, 게임 진행 차단 금지. 사용자엔 가벼운 안내(토스트/로그) 수준.
- 리더보드 조회 실패 → 빈 목록 + 안내 문구.
- Android: 네트워크 권한 확인. 파일/토큰은 PlayerPrefs(또는 persistentDataPath) — `Application.dataPath` 쓰기 금지(과거 사고 회피).

## 9. ChvjPackage 영향 (Rule 03)

- `CHMHttpNetwork`는 인프라에 신규 추가하는 **재사용 공통 기능**(Rule 03 §1). 게임 코드 역참조 금지 — CHMHttpNetwork는 Lair 타입을 모른다(범용 HTTP만).
- 게임 측 `LairApiClient`가 CHMHttpNetwork를 사용해 도메인 엔드포인트를 구성.

## 10. 테스트 전략

- **EditMode**: MetaProfile↔JSON 라운드트립, `AuthTokenStore` 저장/로드, `CloudSaveService` 충돌/백업/복원 로직(가짜 `ILairApiClient`), `LeaderboardClient` 제출/조회 매핑.
- 네트워크 실호출은 단위 테스트에서 제외(인터페이스 모킹). 실제 서버 연동 수동 검증은 별도.
- 한글 테스트 메서드명(project.md `test_method_naming: korean`).

## 11. 마일스톤 (개략 — 상세는 plan)

1. ChvjPackage `CHMHttpNetwork` (async HTTP + 결과 타입)
2. `NetworkConfig` SO + `AuthTokenStore` + `ILairApiClient`/`LairApiClient`
3. 익명 인증 흐름
4. `CloudSaveService` 백업/복원/409 + VillageController 훅
5. 리더보드 제출(전투 종료 훅) + `LeaderboardClient`
6. `LeaderboardPopup` UI (BuildModalPopup 패턴) + 마을 메뉴 진입
7. EditMode 테스트 스위트

## 12. 준수 룰

Rule 00~04 전부. 특히: 에셋 로드 Enum 키(§Rule03 §2) · UI 래퍼 CHText/CHButton(§Rule03 §3) · 스폰 CHMPool(§Rule03 §4) · `//#` 한글 주석 · `var` 금지 · 가드절 · MVVM. 서버 구현 변경 금지(§9).
