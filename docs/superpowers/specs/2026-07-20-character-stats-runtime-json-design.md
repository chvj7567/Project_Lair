# 캐릭터 스탯 런타임 JSON 로드 — Design Spec

- 날짜: 2026-07-20
- 상태: 확정 (사용자 승인)
- 유형: 데이터 출처 방식 변경 (게임 스탯 수치·동작은 JSON=현재 SO 값일 때 불변)

## 1. 배경 / 문제

캐릭터 스탯(영웅·몬스터별 Hp/Power/Range/Cooldown/MoveSpeed + 스폰주기·런길이·카드 임계값)은 현재 `BalanceConfig.asset`(ScriptableObject)에 있고, `BattleController._balance`(인스펙터 참조)를 `ApplyStats()` 가 런타임에 컴포넌트(`Health.SetMax`·`MeleeAttacker.Configure`·`SimpleMover.Speed`)에 적용한다.

이미 **`Lair/JSON Sync` 에디터 창**(`LairJsonSyncWindow` + `BalanceConfigSyncer` + DTO)이 SO ↔ `Assets/_Lair/Data/Json/balance_config.json` 를 양방향 동기화하지만, **에디터 전용**이다 — 값을 바꾸려면 여전히 에디터 Import + 빌드가 필요하다.

**목표**: 런타임이 JSON 을 **직접 읽어** 스탯을 적용해, **빌드 없이(모든 플랫폼) 값 수정**을 가능케 한다.

## 2. 결정 사항 (사용자 확정)

1. **범위**: 기존 `balance_config.json` **전체**(hero/monsters 스탯 + spawnPeriod + runDuration + passive/activeThresholds)를 런타임 로드. 기존 DTO 재사용.
2. **편집 가능 범위**: **모든 플랫폼**(안드로이드 포함). StreamingAssets 기본값 → 첫 실행 시 `persistentDataPath` 로 복사 → 거기서 로드/편집.
3. **기준 강도**: **JSON authoritative + SO fallback (오버레이)**. JSON 값이 항상 이김. JSON 없음/깨짐/필드누락 시 그 부분만 SO 기본값으로 안전하게 메꿈.
4. **출시 JSON == 현재 asset (동작 보존)**: 현재 `Data/Json/balance_config.json`(손유지)은 `activeThresholds` 가 asset(5개 `[30,90,150,210,270]`)과 다른 9개(`[30,...,270]`)로 **드리프트**돼 있다. 그대로 실으면 액티브 카드 선택이 5→9 로 바뀌는 밸런스 변경이 된다. → 출시 StreamingAssets JSON 은 **asset 에서 재-Export** 해 asset 과 정확히 일치시킨다(순수 리팩터 보장). 5→9 등 밸런스 변경은 이 기능 도입 후 JSON 편집으로 별도 수행(이 spec 범위 밖).

## 3. 비목표 (YAGNI)

- 신규 스탯 필드·신규 캐릭터/콘텐츠 추가 — 없음 (v0.3 §8 준수, 데이터 출처 방식만 변경).
- 원격/서버(Firebase) 스탯 다운로드 — 이번 범위 아님(후속).
- cards/pools/hero_skills JSON 의 런타임 로드 — 이번 범위 아님(balance 만). 이들 편집 툴은 그대로.
- 런타임 중 hot-reload(플레이 도중 재적용) — 없음. 로드는 배틀 시작 시 1회.

## 4. 설계

### 4.1 DTO 를 런타임으로 이동

- `BalanceConfigDto`/`CharacterStatDto`/`MonsterStatRowDto` 를 `Lair.EditorTools`(Editor asmdef) → **런타임** `Assets/_Lair/Scripts/Data/Dto/BalanceConfigDto.cs`(namespace `Lair.Data`)로 이동.
- IL2CPP(안드로이드) AOT 스트리핑으로 리플렉션 역직렬화가 깨지지 않도록 **DTO 타입 레벨 + 멤버에 `[UnityEngine.Scripting.Preserve]`** (DTO 는 매개변수 없는 기본 ctor 보유 — Newtonsoft 가 construct+populate 가능). 필요 시 런타임 어셈블리를 덮는 `link.xml` 병행.
- 기존 `BalanceConfigSyncer`(Editor)는 이동된 런타임 DTO 를 참조(using 갱신). Editor asmdef → Runtime asmdef 참조는 정상.
- **Newtonsoft 런타임 접근은 auto-reference 로 추정** — 기존 `Lair.Editor.JsonSync` asmdef 이 Newtonsoft 를 명시 참조 없이(`references:["Lair"]`, `precompiledReferences:[]`) 사용해 컴파일되므로 `com.unity.nuget.newtonsoft-json` 이 auto-referenced 다. 따라서 `Lair.asmdef` **변경 불필요**가 기본. 런타임 DTO 컴파일이 Newtonsoft 미해석으로 실패하는 경우에만 `Lair.asmdef` 에 참조 추가. (JsonUtility 는 camelCase `[JsonProperty]` 매핑 불가라 Newtonsoft 유지.)

### 4.2 런타임 로더 `BalanceJsonLoader` (`Lair.Data`)

위치: `Assets/_Lair/Scripts/Data/BalanceJsonLoader.cs`.

- **순수 파싱 함수** — 파일 IO 와 분리(테스트 용이):
  ```csharp
  //# 실패(빈 문자열·깨진 JSON) 시 null. 예외를 삼켜 null 반환.
  public static BalanceConfigDto Parse(string json);
  ```
- **로드(경로 전략)**:
  ```csharp
  //# 에디터: StreamingAssets 파일 직접 읽기(그 파일이 곧 편집 대상).
  //# 플레이어: 첫 실행 시 StreamingAssets→persistentDataPath 복사(없을 때만) 후 persistentDataPath 읽기.
  public static async Task<BalanceConfigDto> LoadAsync();
  ```
  - 파일명 상수: `balance_config.json`.
  - **에디터**(`UNITY_EDITOR`): `Path.Combine(Application.streamingAssetsPath, "balance_config.json")` 를 `File.ReadAllText` — git 추적 파일을 바로 편집(밸런싱).
  - **플레이어**: 대상 `Path.Combine(Application.persistentDataPath, "balance_config.json")`. 없으면 StreamingAssets 원본 복사:
    - 안드로이드(StreamingAssets 가 APK 내부 URL): `UnityWebRequest.Get(streamingAssetsPath/...)` 로 바이트 읽어 persistentDataPath 에 기록.
    - 그 외(데스크톱 등, 실제 경로): `File.Copy`.
    - 복사 후(또는 이미 존재 시) persistentDataPath 를 `File.ReadAllText`.
  - 초기 복사만 async, 이후 로드는 File.IO. 대상 파일 없음/읽기 실패 → null 반환(호출부가 fallback).

### 4.3 SO 클론 + 오버레이

- **원본 asset 클로버링 방지** — 런타임에 `[SerializeField] BalanceConfig` 를 직접 수정하면 에디터 Play 종료 후에도 asset 값이 영구 변경된다. 따라서 `BattleController` 는 시작 시 **`Instantiate(_balance)` 복제본**을 만들어 거기에만 오버레이하고, 이후 모든 소비자가 복제본을 참조한다. 원본 asset 불변.
- **오버레이 메서드**(런타임) — `BalanceConfig` 에 추가:
  ```csharp
  //# JSON DTO 를 이 인스턴스(복제본)에 오버레이. 있는 값만 덮고, 없거나 검증 실패한 필드는 기존값 유지.
  public void OverlayFromDto(BalanceConfigDto dto);
  ```
  - **오버레이 규칙**: dto 의 hero/각 monster row 가 존재하고 값이 **검증 통과**(Hp>0, Power>0, Range>0, Cooldown>0, MoveSpeed>0; spawnPeriod>0; runDuration>0; thresholds 비어있지 않음)할 때만 해당 필드 대입. 불량/누락은 스킵(기존 SO 값 유지) + `Debug.LogWarning`.
  - dto 에 없는 monster 키는 SO 기존 행 유지(전체 교체 아님).
  - Editor 의 `ApplyDto`(SerializedObject 기반)는 런타임 불가 API → 이 런타임 메서드와 별개로 둘 다 유지.
- **BattleController 흐름**(`Start`, 기존 `_balance` 사용 지점 앞):
  1. `BalanceConfig runtime = Instantiate(_balance);`
  2. `BalanceConfigDto dto = await BalanceJsonLoader.LoadAsync();`
  3. `if (dto != null) runtime.OverlayFromDto(dto); else Debug.LogWarning(...fallback...);`
  4. 이후 `_balance` 대신 `runtime` 를 사용(필드 재대입 또는 별도 필드). `Balance` 프로퍼티·`ApplyStats`·Spawner 바인딩·BattleHud 표시가 모두 `runtime` 을 읽음.
  - `_balance == null`(미할당) 시 기존처럼 프리팹 기본 스탯 fallback 로그 후 진행(회귀 없음).

### 4.4 편집 툴 정합

- `balance_config.json` **정본을 `Assets/StreamingAssets/` 로 이동**하고 `BalanceConfigSyncer.JsonPath` 를 갱신. 그래야 (에디터 툴이 쓰는 파일) = (빌드에 실리는 기본값) = (런타임이 읽는 파일)로 일원화된다.
  - **드리프트 제거(§2.4)**: 손유지된 `Data/Json/balance_config.json`(9개 activeThresholds)을 그대로 옮기지 않는다. `JsonPath` 를 StreamingAssets 로 바꾼 뒤 `BalanceConfigSyncer.Export()`(asset→JSON) 를 실행해 **asset 기준으로 재생성**한다(activeThresholds 5개로 일치). 기존 stale `Data/Json/balance_config.json` 은 제거.
  - **`LairJsonSyncWindow` 는 표기가 아니라 동작 게이팅 수정**: `Path.Combine(JsonDir, fileName)` 이 Import 버튼 활성/`ImportAll` 스킵 여부를 결정한다(L47·L83). balance 만 StreamingAssets 경로로 분기해야 Import 버튼이 비활성/무음스킵되지 않는다. cards/pools/hero_skills 는 `Data/Json` 유지(런타임 로드 대상 아님).

## 5. 리스크 / 검증

- **동작 보존**: JSON = 현재 SO 값이면 게임 동작·수치 불변 → 기존 EditMode/PlayMode 스위트 PASS 가 회귀 기준. (balance 소비 경로 `ApplyStats` 등은 미변경.)
- **IL2CPP 안드로이드**: `[Preserve]`/link.xml 로 DTO 역직렬화 보존. **실제 IL2CPP 빌드에서 파싱 확인 필요**(에디터 통과만으로 불충분).
- **경로/플랫폼**: 안드로이드 StreamingAssets 는 UnityWebRequest 경로, persistentDataPath 는 쓰기가능. 데스크톱은 File.Copy. 에디터는 StreamingAssets 직접.
- **오버레이 안전**: 손편집 JSON 의 필드 누락·오타가 스탯을 0 으로 만들지 않음(검증+스킵). 파싱 실패 시 SO 전체 fallback.
- **테스트**: `Parse`(정상/필드누락/불량값/깨진 JSON/빈 문자열) + `OverlayFromDto`(오버레이·검증 스킵·전체 fallback) 단위 테스트. 로더 파일IO 는 순수 파싱과 분리돼 있어 파싱 로직은 StreamingAssets 없이 테스트 가능.

### 5.1 워크플로 함정 (문서화 필요)

- **persistentDataPath 스테일**: 플레이어에서 한번 persistentDataPath 로 복사되면, 이후 새 빌드의 StreamingAssets 기본값이 그 기기에는 반영되지 않는다(복사가 "없을 때만"). 기기에서 최신 기본값을 받으려면 persistentDataPath 파일을 지우거나 덮어야 한다. (밸런싱은 persistentDataPath 파일을 직접 편집하는 게 정상 흐름.)
- **에디터 Inspector 가려짐**: 이 기능 도입 후 `BalanceConfig.asset` 은 사실상 fallback 전용이 된다. 에디터 Play 중에는 StreamingAssets JSON 이 모든 필드를 오버레이하므로, asset Inspector 에서 값을 바꿔도 JSON 이 있으면 게임에 반영되지 않는다(JSON 우선). 밸런싱은 asset 이 아니라 JSON 을 편집(또는 asset 편집 후 `Lair/JSON Sync → Export` 로 JSON 갱신)해야 한다.

## 6. 산출물

- 신규: `BalanceJsonLoader.cs`, 단위 테스트(`BalanceJsonLoaderParseTests`·`BalanceConfigOverlayTests`).
- 이동: `Data/Dto/BalanceConfigDto.cs` (Editor→Runtime, namespace `Lair.Data`, 타입/멤버 `[Preserve]`).
- 수정: `BalanceConfig.cs`(+`OverlayFromDto`), `BattleController.cs`(로드/클론/오버레이/클론정리), `BalanceConfigSyncer.cs`(`JsonPath`→StreamingAssets), `LairJsonSyncWindow.cs`(balance 게이팅 경로 분기 — L47·L83).
- 정본화: `Assets/StreamingAssets/balance_config.json` 을 asset 에서 재-Export 로 생성(activeThresholds 5개 = asset 일치), stale `Data/Json/balance_config.json` 삭제.
- `Lair.asmdef`: Newtonsoft auto-reference 로 **변경 불필요 기본**(컴파일 실패 시에만 참조 추가).

## 7. 미해결 / 후속

- 원격(Firebase) 스탯 동기화, 플레이 중 hot-reload, cards/skills 런타임 로드는 별도 spec.
