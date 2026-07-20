# 밸런스 SO 롤백 + 문자열 JSON Data/Json 이동 — Design Spec

- **작성일**: 2026-07-21
- **성격**: 순수 엔지니어링 롤백 + 파일 이동 (신규 콘텐츠·밸런스 설계 없음)
- **관련 커밋**: `5d52d7e` (# [refactor] - 캐릭터 서비스 로케이터화 + 스탯 JSON 단일 정본 전환)
- **폐기되는 계획**: 메모리 큐 항목 "BalanceConfig SO 제거" (JSON 단일정본 방향) — 본 작업으로 역행·폐기

---

## 1. 배경 / 의도

프로젝트의 데이터는 두 부류로 로드된다:

1. **JSON을 런타임에 직접 읽는 것** — 문자열(Addressable TextAsset), 밸런스(File.IO).
2. **SO를 런타임에 읽고 JSON은 에디터 편집 미러인 것** — 카드, 영웅 스킬.

커밋 `5d52d7e` 가 밸런스를 ①(File.IO JSON 단일정본)으로 전환했으나, **런타임 튜닝 설계가 불필요** 하다는 판단에 따라 밸런스를 다시 SO(②) 로 되돌린다. 동시에, Art/ 하위에 어정쩡하게 있던 문자열 JSON 2개를 다른 JSON들과 같은 `Data/Json` 으로 이동해 위치를 정리한다.

**핵심 제약**: `5d52d7e` 는 두 가지를 한 커밋에 담았다 — ① 캐릭터 서비스 로케이터화, ② 밸런스 SO→JSON 전환. **①은 유지하고 ②만 선택적으로 역행** 한다. 커밋 전체 `git revert` 는 금지(①까지 되돌아감).

## 2. 범위

### 2.A 밸런스 완전 롤백 (5d52d7e 이전 SO 상태로)

목표 상태 = `5d52d7e~1` 의 밸런스 아키텍처:

- `BalanceConfig` 는 `ScriptableObject` (`[CreateAssetMenu(fileName="BalanceConfig", menuName="Lair/BalanceConfig")]`).
- `Assets/_Lair/Data/BalanceConfig.asset` 이 런타임 정본 — 인스펙터 손편집.
- `BattleController` 가 `[SerializeField] private BalanceConfig _balance;` 로 **Battle 씬에서 드래그 할당** 받아 사용.
- `balance_config.json` 은 **에디터 동기 미러** — `BalanceConfigSyncer` 가 JSON↔SO 양방향 동기(카드/스킬과 동일 패턴). 런타임은 JSON을 읽지 않는다.

되돌릴 항목:

| 파일 | 조치 |
|---|---|
| `Scripts/Data/BalanceConfig.cs` | 순수 클래스 → `ScriptableObject` 로 복원 (필드 `[SerializeField]`/`[Serializable]`, `CreateDefault`/`OverlayFromDto` 런타임 로직 제거 또는 에디터 동기용으로 정리) |
| `Data/BalanceConfig.asset` (+`.meta`) | `5d52d7e~1` 에서 복원 |
| `Scripts/Battle/BattleController.cs` | 밸런스 로딩부 롤백 — `[SerializeField] _balance` + `_model.TotalSeconds = _balance.RunDuration` 경로 복원, `CreateDefault()`+`BalanceJsonLoader` 오버레이 제거 |
| `Scenes/Battle.unity` | `BattleController._balance` 에 `BalanceConfig.asset` **재와이어링** |
| `Editor/JsonSync/BalanceConfigSyncer.cs` (+`.meta`) | 복원 |
| `Editor/JsonSync/Dto/BalanceConfigDto.cs` (+`.meta`) | 복원 (현재 `Scripts/Data/Dto/BalanceConfigDto.cs` 위치와 중복 해소 — §4 결정 필요) |
| `Editor/JsonSync/LairJsonSyncWindow.cs` | balance 동기 메뉴/버튼 복원 |
| `Scripts/Data/BalanceJsonLoader.cs` (+`.meta`) | **제거** |
| `Editor/BuildHooks/BalanceJsonBuildCopier.cs` (+`.meta`) | **제거** |
| `Editor/BuildHooks/Lair.Editor.BuildHooks.asmdef` (+`.meta`) | **제거** (다른 빌드훅이 없으면 폴더째) |
| `.gitignore` | StreamingAssets balance_config.json 항목 2줄 제거 |
| `Assets/StreamingAssets/balance_config.json*` | 산출물 — 존재 시 정리 |

### 2.B 문자열 JSON 이동

| 파일 | 조치 |
|---|---|
| `Art/Json/Strings_Ko.json` (+`.meta`) | → `Data/Json/Strings_Ko.json` (Addressable 유지, .meta 동행 → GUID 보존) |
| `Art/Json/LoadingStrings_Ko.json` (+`.meta`) | → `Data/Json/LoadingStrings_Ko.json` (동일) |
| `Art/Json/.gitkeep` + 폴더 | 빈 `Art/Json` 정리 |
| `Scripts/Data/CommonEnum.cs` | `EData.Strings_Ko`/`LoadingStrings_Ko` 주석의 "Art/Json" 경로 → "Data/Json" |
| `Scripts/Data/StringTableProvider.cs` | 클래스 주석의 "Art/Json/Strings_Ko.json" 경로 수정 |

**로딩 무영향 근거**: 두 파일은 Addressable 주소(`Strings_Ko`/`LoadingStrings_Ko`)로 로드된다. Addressable 엔트리는 GUID 기반이라 `.meta` 를 동반 이동하면 주소·라벨이 그대로 유지되어 `CHMResource.LoadAsync<TextAsset>` 이 계속 성공한다. 폴더 위치는 로드에 영향을 주지 않는다.

### 2.C 범위 밖 (변경 없음)

- 카드(`CardData`/`CardPool` SO) · 영웅 스킬(`HeroSkillData`/`HeroSkillLoadout` SO) 파이프라인 — 그대로 유지.
- 캐릭터 서비스 로케이터(`LairCharacter` 등 5d52d7e ① 부분) — 그대로 유지.

## 3. 테스트 영향

`5d52d7e` 이후 추가된 JSON-런타임-로드 전제 테스트를 정리한다:

- `Tests/EditMode/Data/BalanceJsonLoaderParseTests.cs` — `BalanceJsonLoader` 제거 시 컴파일 불가 → **제거**.
- `Tests/EditMode/Data/BalanceShippedJsonRegressionTests.cs` — `Data/Json` 정본 File.IO 읽기 전제 → SO 기반 검증으로 대체하거나 제거.
- `Tests/EditMode/Data/BalanceConfigTests.cs`, `BalanceConfigOverlayTests.cs` — `OverlayFromDto`/`CreateDefault` 를 검증. SO 롤백 후 이 API 의 존폐에 맞춰 갱신(§4 결정에 종속).
- SO 경로 기반 테스트(`CardPoolDistributionTests` 등)는 밸런스와 무관 — 영향 없음.

## 4. 열린 결정 (writing-plans / 구현 단계에서 확정)

1. **BalanceConfigDto 단일화**: 현재 `Scripts/Data/Dto/BalanceConfigDto.cs` 가 존재. 롤백 시 `5d52d7e~1` 의 `Editor/JsonSync/Dto/BalanceConfigDto.cs` 를 되살리면 중복. → **에디터 동기 전용 DTO 1벌** 로 통일(위치는 Editor 쪽 권장, 런타임 참조 제거).
2. **CreateDefault/OverlayFromDto 처치**: SO 가 정본이면 런타임 오버레이 API 는 불필요. 단, 에디터 Syncer 가 JSON→SO 반영에 쓰던 `ApplyDto(SerializedObject)` 경로는 유지. 순수-클래스용 `CreateDefault`/`OverlayFromDto` 는 제거 방향.
3. **BalanceConfigSyncer 복원 vs 신규**: `5d52d7e~1` 원본을 되살리되, 그 사이 balance_config.json 스키마가 바뀌었으면(현 6줄 diff) 동기 필드 정합성 확인.

## 5. 커밋 계획 (Rule 01)

기획 관점 한 줄 제목. 예:

```
# [refactor] - 밸런스를 다시 SO 정본으로 롤백 + 문자열 JSON을 Data/Json으로 정리
```

- 자동 커밋 금지. `git add` + 메시지(안)까지만.
- 신규/삭제 파일의 `.meta` 만 스테이징. 수정 파일 `.meta` 제외.

## 6. 검증 기준

- [ ] Battle 씬 재생 시 밸런스가 `BalanceConfig.asset` 값으로 적용됨 (런 길이·몬스터 스탯·threshold).
- [ ] `Lair/JSON Sync` 창에서 balance 양방향 동기 동작.
- [ ] 문자열 이동 후 로딩씬에서 `Strings_Ko`/`LoadingStrings_Ko` 로드 성공(CHText 문자열·로딩 설명 정상 표시).
- [ ] `BalanceJsonLoader`/`BalanceJsonBuildCopier` 참조 잔존 0건 (컴파일 통과).
- [ ] EditMode/PlayMode 테스트 그린.
