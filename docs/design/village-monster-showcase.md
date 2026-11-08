# 마을·로딩 씬 내 몬스터 쇼케이스 (Village Monster Showcase) - 기능 기획서

> 작성: game-designer · 날짜: 2026-11-08 · 단계: v0.3 · 승격: CLAUDE.md §8 (개정안 §12)
> 사용자 요청: "현재 내가 가진 몬스터 외형이 로딩씬이랑 마을씬에서 그 티어 등급에 맞게 나오게 해줘"
> 입력(코드·문서 조사): `VillageController`(`SpawnIdleHero`·`DisableBattleComponents`·프로필 변경 통지) · `VillageWanderer` · `LoadingController`(Start 흐름) · `BattleController`(`LoadMonsterTierSheets`·몬스터 구성부 817~838행) · `MonsterGradeVisual` · `MonsterVisual2D` · `MonsterTierSheetLoader` · `GradeFxBudget` · `ChapterDatabaseLoader`(`GradeFxDatabase`·`FormationDatabase` 로드) · `FormationSave.Resolve` · `scene-2d-conversion.md` §5.6·§6.2 · `village-hud-layout.md` §3D · `monster-grades.md` · `monster-tier-sheets.md`
> **사용자용 가이드**: `docs/design/village-monster-showcase.html`
> **UI 목업**: `.mockups/village-monster-showcase.html` (마을 보행 구역·HUD 가림·로딩 배치·티어/등급 조합 미리보기, 근사. 메인이 `node .mockups/server.js` 로 제공 요청)
> **데이터 구조표**: 본 문서 §8 (Rule 00 데이터 구조 확인 게이트 대상 - `Showcase_Scene`·`Showcase_Actor`·`Showcase_Actor_Waypoint` 신설)

---

## § 헤더

- **목표**: 마을 씬과 로딩 씬의 월드(배경 위)에, 플레이어가 **지금 가진 몬스터 6마리**(편성된 둥지별 종족)를 **전투와 같은 외형**(강화 티어 T1~T3 전신 시트 + 등급 1~5 연출)으로 보여 준다.
- **검증 가설**: (1) 내가 키운 몬스터가 마을·로딩에서 보이면 강화·승급 투자가 체감되어 상점·승급 동기가 커지는가. (2) 전투용 프리팹을 그대로 쓰는 순수 비주얼 모드로 전투 로직 오염 없이 안전하게 재사용되는가. (3) 6마리 x 최대 이펙트 6개가 마을·로딩에서 성능·가독성을 해치지 않는가.
- **현재 단계 범위 적합성**: **범위 안**(CLAUDE.md §8 "몬스터 등급 2~5 허용"·"던전 편성 허용"·"아트/에셋 작업 허용"의 기존 몬스터 표현 재사용). 신규 몬스터·신규 UI 화면·별도 메인 메뉴·전투 로직 연결·몬스터 상호작용(탭 반응)은 **금지 유지**. §8 어느 항목에도 "씬 월드 연출" 문구가 없어 개정안 §12 를 제안한다.
- **핵심 메커니즘**: 씬 진입(로딩은 데이터 준비 직후, 마을은 시작 직후)에 유효 편성 6마리의 (기준 몬스터, 등급, 강화 Lv → 티어)를 계산하고, **전투와 같은 등급 몬스터 프리팹**(`EMonster` 30값, Addressables)을 `CHMPool` 로 불러 **쇼케이스 모드**(전투 컴포넌트·HP바·충돌 끔)로 배치한다. 티어는 전투와 같은 로더(`MonsterTierSheetLoader`)로 T 시트를 로드해 몸 스프라이트를 치환하고, 등급 연출은 `GradeFxDatabase` 의 같은 명세(`SpecOf`)로 적용한다. 마을은 보행 구역 안에서 걷고, 로딩은 마법진 위 고정 위치에서 대기한다.

---

## 0. 현황 조사 결과 (요청 전제와 다른 점)

| # | 조사 사실 | 기획 영향 |
|---|---|---|
| F1 | **마을에는 이미 장식 배회 몬스터 6마리가 있다**(`Village.unity` 의 `VillageWanderer` 6, 종·경유점 고정, 풋내기 외형, `scene-2d-conversion.md` §5.6). **로딩에도 마법진 위 장식 몬스터 6마리와 그림자 6개가 있다**(`Loading.unity`, §6.2). 둘 다 **보유 상태와 무관한 고정 풋내기**다 | 신규 배치가 아니라 **장식 6마리를 "내 몬스터 6마리"로 교체**하는 기능이다. 위치·종 배정은 기존 6종 구성을 그대로 따른다 |
| F2 | 기존 마을 경유점의 상당수가 HUD 뒤다(예: Wisp (60,206)·(160,238)는 좌 메뉴 타일·버튼 뒤, `village-hud-layout.md` §5 "범위 밖·육안 확인"으로 보류돼 있었음) | 이번에 **HUD 에 가리지 않는 보행 구역**을 확정해 경유점을 재배치(§4) |
| F3 | 마을의 영웅 쇼케이스(`SpawnIdleHero`)는 전투 프리팹을 `CHMPool.Pop` + 전투 컴포넌트 비활성으로 쓰고 풀 반환 전 복구하는 선례가 있다 | 같은 방식을 몬스터에 적용(§6) |
| F4 | `GradeFxDatabase.Current`·`FormationDatabase.Current` 는 `LoadingController` 4.5단계(`ChapterDatabaseLoader.LoadAsync`)에서 이미 로드되고, `MetaSession.GetOrLoad()` 는 어디서든 로컬 프로필을 읽는다 | 로딩에서도 데이터가 준비되는 시점이 명확(§5) |
| F5 | **Loading 씬과 전투 시작 T 시트 로드 구간은 서로 다른 씬이라 겹치지 않는다**(흐름: Loading(앱 시작 1회) → Village → Battle). 겹치는 것은 같은 Addressables 키의 **핸들 캐시 공유**뿐 | §7 에서 해제·재로드 규칙 정의 |
| F6 | 기존 문서 `monster-grades.md` §9.5 "편성·상점·도감 UI 미리보기에서 이펙트 표시 안 함"은 **UI 아이콘(`MonsterIcons` 6장) 한정**이다 | 씬 월드의 몬스터 배치는 UI 가 아니라 **충돌 없음**. 문구 명확화는 §11 |

---

## 1. 확정 결정 / 추천안 (열린 질문은 §13)

| # | 항목 | 추천안(설계 기준값) | 대안(기록) |
|---|---|---|---|
| S1 | 마을 | 장식 6마리를 **내 6마리**로 교체해 보행 구역에서 돌아다니게(대기·이동만) | 제자리 대기 |
| S2 | 로딩 | **현행 마법진 위 6개 고정 위치**에서 Idle(현행 배치·연출 승인본 유지). 처음엔 풋내기 자리표시자 → 준비되면 내 외형으로 교체 | 로딩 바 위를 걷는 순찰(신규 레이아웃, 바·문구와 겹침 관리 필요) |
| S3 | 표시 기준 | **유효 편성 6마리**(둥지별 종족, `FormationSave.Resolve` 결과) | 종족별 보유 최고 등급 |
| S4 | 연출 범위 | **티어 T 시트 + 등급 이펙트 전부**(크기·후광·오라·기둥·후광·맥동) | 마을은 오라·후광(몸 외곽 발광)만 |
| S5 | 동작 | Idle·Move 만. 공격·피격·사망·등장 연출(씬 트윈 제외) 없음 | |
| S6 | 순수 비주얼 | 전투 프리팹 + **쇼케이스 모드**(전투 컴포넌트·HP바·충돌·바닥 원판/강화 링 끔) | 전용 경량 프리팹(등급 이펙트·티어 연출 중복 구현 필요) |
| S7 | 표시 크기 | 전투 월드 크기 x 등급 크기 배율, 단 **표시 높이 상한 94도트**(화면 높이 270의 35%)를 넘으면 배율을 줄임(§4.2) | 상한 없음 |
| S8 | 갱신 | 마을은 구매·승급·클라우드 복원 직후 **즉시** 해당 종족만 교체. 편성 확정은 출격(전투 진입)과 한 몸이라 다음 마을 진입 때 반영 | 팝업 닫을 때 일괄 |
| S9 | 이펙트 상한 | 전투와 **같은 상한 카운터 공유**(`GradeFxBudget` 오라 40·기둥 24·후광 8). 쇼케이스 6마리 최대 사용량은 오라 6·기둥 24·후광 6 | 마을 전용 낮은 상한 |

---

## 2. 표시 대상 계산 (유효 편성 -> 6개 "외형 한 벌")

씬 진입 시 `MonsterLook` 6개를 계산한다. 계산은 순수 함수 1개로 두어 **전투와 쇼케이스가 같은 결과**를 쓴다(구현은 gameplay-programmer, 전투 `CacheSpeciesLevels`·`LoadMonsterTierSheets` 로직과 중복 구현 금지).

| 필드 | 값 | 출처 |
|---|---|---|
| `Monster` | 둥지 i 의 편성 몬스터(`EMonster`, 등급 몬스터 값 포함) | `FormationSave.Resolve(FormationDatabase.Current, profile.Formation, profile.ClearedStage, profile.GetRaceGrade)` — 1-1 클리어 전·편성 미저장·보유 등급 초과 저장값은 이 함수가 기본 편성/최고 보유 등급으로 보정 |
| `Base` | 기준 몬스터(= 종족 1등급) | `MonsterGrades.BaseOf(Monster)` |
| `Grade` | 1~5 | `MonsterGrades.GradeOf(Monster)` |
| `Level` | 상점 강화 Lv 0~5 | `MetaConfig.ShopItems`(`EffectKind == MonsterSpecies`)의 `profile.GetShopLevel` (전투 `CacheSpeciesLevels` 와 같은 규칙) |
| `Tier` | `[0,1,1,2,2,3][Level]` | `MonsterEnhancementVisual.TierOfLevel` |

- 마을·로딩의 6자리는 종족 고정 슬롯이다(둥지 6곳이 각각 한 종족 자리). 종족 → 자리 대응: 마을 §4.3 표, 로딩 §5.2 표.
- 봉인 규칙·카드 제외·전투 수치는 쇼케이스에서 무시한다(전투 한정).
- 1-1 클리어 전(편성 잠김)에도 같은 함수 결과(= 기본 편성)를 쓴다.
- 오프라인/클라우드 없음: 로컬 프로필 기준으로 동작한다(네트워크 불필요). 마을에서 클라우드 복원이 프로필을 바꾸면 §8 갱신 경로로 반영.

---

## 3. 쇼케이스 모드 (순수 비주얼, 전투 오염 방지) — S6

**원칙**: 마을·로딩의 몬스터는 `MonsterBrain`·스포너·`BattleController` 어느 쪽에도 등록되지 않는 **순수 시각 객체**다. 전투 프리팹을 그대로 쓰되, 아래 상태로 만든다.

| 구분 | 쇼케이스 모드에서 | 복구 시점 |
|---|---|---|
| `AutoCombatAI`·`MeleeAttacker`·`SimpleMover`·`SimpleRotator`·`CharacterAnimationDriver`·`DespawnOnDeath`·피격/공격 연출(`AttackJuice`)·`PlagueToxinOnHit` 등 전투 컴포넌트 | **끔**(영웅 쇼케이스의 `SetBattleComponents(false)` 선례를 몬스터 목록으로 확장) | 풀 반환 전 켬 |
| `Health` 이벤트 구독자(`HitFlash`·`MonsterGradeVisual` 사망 처리)·`MonsterHpBar`·충돌체·`Rigidbody` | HP바 **숨김**, 충돌체·`Rigidbody` **끔**(마을 UI 레이캐스트·영웅 홀로그램과 무관하게) | 풀 반환 전 켬 |
| 바닥 원판 `Aura`·강화 링 `AuraSigil` | **숨김**(씬의 그림자 스프라이트가 대신) | 풀 반환 전 켬 |
| 몸 스프라이트·`Animator`·`MonsterVisual2D`·`MonsterEnhancementVisual`·`MonsterGradeVisual` | **켬**(티어·등급 표시의 본체) | - |
| 애니메이션 | `Animator` 의 `Speed` 파라미터를 **쇼케이스 이동 로직이 직접 기록**(`CharacterAnimationDriver` 를 껐으므로 충돌 없음). Idle ↔ Move 외 상태 전이 없음 | - |
| 방향 | 루트 yaw 로 좌우 결정(`MonsterVisual2D` 의 `forward.x` 데드존 규칙 그대로) | - |

- 모드 on/off 는 **루트 파사드의 단일 의도 API**(예: `SetShowcaseMode(bool)`)로 노출한다(Rule 02 §10, 외부가 하위 컴포넌트를 직접 만지지 않음). 컴포넌트 목록·구현은 gameplay-programmer 판단이며, **끈 모든 것은 `SetShowcaseMode(false)` 한 번으로 풀 Pop 직후와 동일 상태로 복구**돼야 한다(게이트 T-S1).
- **풀 오염 방지**: 쇼케이스 인스턴스는 전투와 같은 프리팹 풀을 쓴다(`CHMPool` 영속). 반환 전 복구 + 기존 `OnEnable` 리셋(티어 0·등급 연출 0)으로 이중 안전망. 전투 시작 시 `GradeFxBudget.Reset()` 이 이미 호출되므로 쇼케이스가 남긴 슬롯이 전투로 새지 않는다(그 전에 씬 종료로 전부 반환됨).
- **전투 정보 누수 금지**: 쇼케이스는 `MonsterDied` 발행·도감 처치 기록·`BattleController` 리스트 등록·카드/유물 효과 적용을 하지 않는다(해당 코드는 전투 컴포넌트·`BattleController` 경로라 모드에서 호출되지 않음).

---

## 4. 마을 씬

### 4.1 레이어·HUD 가림

- 월드 레이어: 배경 < 영웅 홀로그램·소품 < **쇼케이스 몬스터(발 y 로 깊이 정렬: 기존 배회자·영웅과 같은 Z/`Sprite Sort Point = Pivot` 규칙)** < 이펙트(기둥·후광·오라는 몸 뒤/아래 정렬, `monster-grades.md` §2.3) < **HUD(스크린 오버레이 캔버스) < 팝업**. HUD·팝업은 항상 몬스터 위에 그려진다.
- 몬스터는 클릭·터치에 반응하지 않는다(레이캐스트 대상 아님, 충돌체 끔).

### 4.2 크기 비율 (S7)

- 마을 카메라 직교 크기 2.8125 → 세로 5.625u = 270도트(PPU 48, 1도트 = 1/48u). 전투 카메라(세로 14u)와 달리 같은 월드 크기가 화면에서 2.49배 크게 보인다.
- 표시 높이(도트) `H = (티어 시트의 hpBarHeightWorld − 0.08) x 48 x sizeMul`(풋내기 티어 0 은 프리팹 티어 0 표값 − 0.08, 대략 몸 px + 3). 예: Wisp T0 G1 = 32, Wraith T0 G1 = 65.
- **표시 상한**: `FitMaxHeightDots = 94`(화면 높이 270의 35%). `H > 94` 이면 쇼케이스 루트 스케일에 `fit = 94 ÷ H` 를 곱한다(이펙트도 같이 줄어듦). 검산: Wraith T3(높이비 약 1.5, 육안 추정) G5(x1.30) = 65 x 1.5 x 1.30 = 127 → fit = 94 ÷ 127 = 0.74. Wisp T3 G5 = 32 x 1.3 x 1.3 = 54 → fit 1.
- 이는 **쇼케이스 표시 배율**이며 전투 월드 크기·콜라이더를 바꾸지 않는다("월드 크기 불변" 제약은 전투 기준).
- 상한값은 `Showcase_Scene.fitMaxHeightDots`(JSON)로 튜닝한다.

### 4.3 보행 구역과 경유점 (1280x720 기준 HUD 좌표 `village-hud-layout.md` §3D, 도트 = 화면 px ÷ 2.667)

HUD 에 가리지 않는 **3개 레인**(도트 좌표, 화면 px 병기):

| 레인 | 도트 범위 | 화면 px | 근거(가림 검사) |
|---|---|---|---|
| 좌 레인 L | x 112~190, y 154~232 | x 299~507, y 411~619 | 좌 메뉴 타일 x ≤ 256px · ◀ 화살표 y 326~394px(= 도트 122~148) 아래 · 홀로그램 기둥 x 520~760px(= 도트 195~285) 왼쪽 |
| 우 레인 R | x 290~340, y 154~232 | x 773~907, y 411~619 | 우 패널 x ≥ 924px(= 도트 346) 왼쪽 · ▶ 화살표 아래 · 홀로그램 기둥 오른쪽 |
| 전방 레인 F | x 112~340, y 200~232 | x 299~907, y 533~619 | 홀로그램 투영진 링 하단 y 464px(= 도트 174) 아래 · 영웅/방어하기 버튼 상단 y 632px(= 도트 237) 위 |

검산: 232 x 2.667 = 618.7 < 632(버튼 상단), 200 x 2.667 = 533 > 464(투영진 하단), 112 x 2.667 = 299 > 256(메뉴 타일 우단), 340 x 2.667 = 907 < 924(우 패널 좌단).

종족별 동작·경유점(`Showcase_Actor` / `Showcase_Actor_Waypoint` 값, 모든 점은 L/R/F 레인 안):

| 종족(기준) | 동작 | 속도(도트/s) | 비행 | 경유점(도트) |
|---|---|---|---|---|
| Wisp 도깨비불 | 배회 | 26 | - | (140,222) (232,228) (320,224) (190,230) |
| Wraith 망령 | 제자리(고정, 오른쪽 보기) | 0 | - | (124,230) |
| Reaper 사신 | 배회 | 14 | - | (300,214) (330,226) (312,204) |
| Hex 저주술사 | 배회 | 12 | - | (150,206) (178,214) |
| Plague 역병귀 | 배회 | 10 | - | (210,226) (248,230) (286,224) |
| Phantom 환령 | 배회 | 34 | 비행(화면 위 10도트 + sin 흔들림) | (130,196) (170,206) (300,196) (330,210) |

- 규칙은 기존 `VillageWanderer` 와 같다: 시작 대기 [0, 2)초 무작위, 도착(거리 < 1도트) 시 다음 경유점 = (현재 + 1 + [0, 개수−2]) mod 개수, 대기 1 + [0, 2.5)초, 등속 직선 이동, `|dx| > 0.5` 일 때만 방향 갱신, 위치는 매 프레임 도트 반올림 후 `DotStageMapping.VillageDotToGround`. 기존 정적 순수 함수(`StepToward`·`NextWaypointIndex`)를 재사용한다.
- 키가 큰 몬스터(Wraith·Reaper)는 홀로그램 기둥을 가리지 않게 레인 바깥쪽에 둔다. 머리가 ◀▶·메뉴 타일 뒤로 들어가는 것은 허용(HUD 가 위). 최대 크기(T3·G5)에서 홀로그램 영웅 몸을 가리는지는 육안 게이트 V-S2.
- 위 좌표는 시안 기준 초안이며 **구현 후 육안으로 ±8도트 보정**(V-S1). 갱신 시 `Showcase_Actor_Waypoint` 값만 바꾼다.
- 6마리 서로의 겹침은 허용(발 y 깊이 정렬).
- 그림자: 현행 규칙 유지(`Shadow_R6`/`Shadow_R11`, 몸 셀 폭 > 60 이면 R11, 알파 0.4, 몸 order − 1). 쇼케이스 루트 스케일(fit·sizeMul)에 맞춰 그림자 크기를 키운다.

---

## 5. 로딩 씬

### 5.1 순서·시점 (첫 프레임·로드 순서·충돌)

`LoadingController.Start` 현행 흐름에 **"내 외형 준비"**를 끼워 넣는다.

| 단계 | 현행 | 변경 |
|---|---|---|
| 첫 프레임 | 씬 정적 몬스터 6(풋내기, 현행 외형) 보임 | **그대로 보임 = 자리표시자**(비어 보이지 않음) |
| 1~4 | Init · 패키지 Init · BGM · 문구 · 문자열 | 변경 없음 |
| 4.5 | `ChapterDatabaseLoader.LoadAsync` → `FormationDatabase`·`GradeFxDatabase` 준비 | 변경 없음. **이 직후가 "내 외형 계산" 가능 시점** |
| **4.6(신규)** | - | `MonsterLook` 6개 계산(§2) → 필요한 프리팹(최대 6) · T 시트(티어 > 0 인 종족) 로드를 **비동기로 시작(5단계와 병렬)** |
| 5 | `PreloadByLabelAsync`(진행률 바) | 변경 없음(병렬로 진행). 바 진행률은 5단계 기준 그대로 |
| **5.5(신규)** | - | 5단계 종료 후 4.6 로드 완료를 **대기(최대 5초, 로더 타임아웃과 동일)**. 완료분부터 자리표시자를 **내 몬스터로 교체**(등장 트윈 0.3초). 실패·시간 초과 종족은 자리표시자(풋내기) 유지 |
| 6 | "준비 완료 - 클릭하여 시작" | **교체 완료 여부와 무관하게 표시 가능**(몬스터는 장식이라 시작을 막지 않음). 단 5.5 대기는 5초까지만 |

- **첫 프레임에 내 외형은 보일 수 없다**(프로필·편성·Addressables 가 준비되기 전). 자리표시자 → 교체 방식이 유일한 안전 경로다. 초반(전부 T0·G1)은 자리표시자와 내 외형이 같아 **교체 연출 생략**(같으면 그대로 둠).
- 로딩에는 별도 로딩 화면 구간이 없다(이 씬 자체가 로딩). 전투 시작 T 시트 로드와는 **씬이 달라 겹치지 않는다**(F5).
- 교체 대상은 씬 정적 몬스터와 같은 위치·그림자·축 색 고리를 쓴다. 정적 몬스터(스프라이트·Animator 컨트롤러 직결 오브젝트)는 교체 후 비활성화하고, 교체 인스턴스를 같은 발 위치에 놓는다.

### 5.2 배치 (현행 `scene-2d-conversion.md` §6.2 유지)

| 종족 | 발 (x, y) 도트 | 방향 | 정렬 order |
|---|---|---|---|
| Phantom | (99, 210) | 오른쪽 | 20 |
| Hex | (140, 220) | 오른쪽 | 30 |
| Wisp | (194, 225) | 오른쪽 | 35 |
| Wraith | (271, 225) | 왼쪽 | 35 |
| Reaper | (332, 221) | 왼쪽 | 31 |
| Plague | (379, 211) | 왼쪽 | 21 |

- 동작: Idle 만(시작 위상 `i x 0.37s`). 이동 없음.
- 레이어: 배경 < 그림자 < 몬스터(order 10 + (발 y − 200)) < 축 색 고리·발광 요소 < **LoadingHud(캔버스)**. 제목(y ≤ 약 56도트)·진행 바(y ≈ 254)·문구(y ≈ 237)와 겹치지 않는다: 몬스터 발 y ≤ 225, 최대 표시 높이 94 → 머리 y ≥ 131 > 제목 하단.
- 크기: §4.2 와 같은 상한 94도트·fit 적용(로딩도 카메라 직교 크기 2.8125).
- 대안 "로딩 바 위를 걷는 순찰"(S2 대안)은 신규 레이아웃·이동 구역·바 가림 검사가 필요해 이번엔 채택하지 않는다(§13 Q2).

---

## 6. 풀링·State Reset·씬 전환 (Rule 03 §4)

| 항목 | 규칙 |
|---|---|
| 획득 | 프리팹 로드(`CHMResource.LoadAsync<GameObject>(EMonster)`) → `CHMPool.CreatePool(prefab, count: 1)`(이미 있으면 재사용) → `CHMPool.Pop`. `Instantiate` 직접 호출 금지 |
| 배치 | 쇼케이스 슬롯 루트(그림자 + 위치 홀더)의 자식으로 Pop, 위치·yaw 지정 후 `SetShowcaseMode(true)` → `ApplyOwnedLook(Look)`(§7) |
| 반환 | `SetShowcaseMode(false)`로 전부 복구 → `CHMPool.Push`. 풀 Pop 시 기존 `OnEnable` 리셋(티어 0·등급 연출 0·이펙트 슬롯 반납)이 한 번 더 보장 |
| 씬 전환 | 마을 → 전투(출격)·로딩 → 마을: **씬 언로드 전**(`Sortie` 경로·`OnDestroy` 이전에 호출되는 정리 지점)에 모든 쇼케이스 인스턴스를 Push 하고 로더를 `Release()`한다(부모가 씬과 함께 파괴돼 풀 인스턴스가 고아가 되는 것을 방지) |
| 교체 중 재교체 | 종족별 "교체 중" 플래그 + 대기 요청 1개(영웅 쇼케이스 `_showcaseSwapPending` 패턴) — 연속 구매에도 중복 배치 없음 |
| 등장 | 풀 Pop 시 기존 `Visual2D` 등장 트윈(0.3s: 알파 0 → 1, 스케일 0.6 → 1.0)을 그대로 사용 → 교체 연출 별도 구현 없음 |
| 중복 방지 | 같은 종족 자리에는 인스턴스 하나만(교체 시 이전 것을 Push 후 새로 Pop) |

---

## 7. 로드·메모리·이펙트 상한

### 7.1 로드 규칙

| 대상 | 로드 방식 | 해제 |
|---|---|---|
| 몬스터 프리팹(등급 Variant 포함, 30값) | `CHMResource.LoadAsync<GameObject>(EMonster)` — 키는 기존. **같은 키의 핸들은 `CHMResource` 가 캐시**하므로 로딩에서 로드한 프리팹은 마을·전투가 재사용(중복 로드 없음). 등급 이펙트 시트·풋내기 몸 시트는 프리팹 직접 참조라 함께 로드 | 프리팹은 현행처럼 해제하지 않음(기존 정책) |
| T 시트(`MonsterTierSheetSet`) | **`MonsterTierSheetLoader`(전투와 같은 클래스·같은 규칙: 병렬·1회 재시도·5초 타임아웃·풋내기 폴백)** 를 씬마다 인스턴스로 쓴다. 필요 키: 티어 > 0 인 종족(최대 6) | 씬 정리 지점에서 `Release()`(마을 → 전투 전, 로딩 → 마을 전) |
| 풀 워밍 | 쇼케이스는 종족당 1개만 필요 → `CreatePool(count: 1)`. 전투의 워밍(count 10 등)은 전투 씬이 따로 수행 | - |

- **키 캐시 공유 주의**: `CHMResource` 는 키당 핸들 1개라 한 씬이 `Unload(key)`하면 다음 로드는 다시 읽는다. 씬 전환은 이전 씬 정리 → 새 씬 로드 순서가 보장되므로(단일 로드) **이중 해제·누수 없이 재로드**된다. 이는 의도된 동작이며 추가 핸드오프 코드는 두지 않는다(로컬 번들이라 재로드 비용 미미). 게이트 T-S5.

### 7.2 메모리(추정, 임포트 후 실측으로 덮어씀)

| 씬 | 상주 | 최악 합계(밉맵 포함) |
|---|---|---|
| 로딩·마을 | 풋내기 몸 시트·마스크 6종(6.1MB) + 티어 시트 종족당 1장(최대 6.1MB) + 이펙트 시트(최악 6종 모두 5등급 16.9MB) | **29.1MB**(= 전투 최악과 같은 구성, `monster-grades.md` §9.4·`monster-tier-sheets.md` §7.6) |
| 상한 | 몬스터 시트 + 이펙트 합계 ≤ 35MB(기존 U8) | 마을·로딩에서도 같은 상한 적용 |

- 마을은 상점·UI 등으로 메모리가 따로 쓰이지만 몬스터 쇼케이스 몫은 위 한도 안이다. 초반(전부 T0·G1)은 이펙트 0·티어 0 이라 거의 늘지 않는다.

### 7.3 이펙트 동시 상한 공유 (S9)

- `GradeFxBudget`(오라 40·기둥 24·후광 8, 정적 카운터)를 **그대로 공유**한다. 씬이 하나만 살아 있으므로 마을·로딩·전투가 동시에 쓰지 않는다.
- 쇼케이스 6마리 최대 사용: 오라 6(≤ 40), 기둥 6x4 = 24(= 상한 24, 전부 허용), 후광 6(≤ 8) → **상한 안**. 다만 5등급 6마리 + 기둥이 상한과 정확히 같아 여유가 없으므로 상한을 줄이는 변경 시 마을 표시가 영향받는다(문서화).
- 마을·로딩 진입 시 `GradeFxBudget.Reset()` 호출(이전 씬 잔여 슬롯 제거), 씬 정리 지점에서 인스턴스 Push 가 슬롯을 반납한다.
- 마을·로딩 성능(저사양): 6마리 x 최대 이펙트 6개 = 스프라이트 애니메이션 36개. 별도 저사양 토글(설정 화면 금지)은 두지 않고 **측정 게이트 V-S4**(마을 p95 프레임 시간)로 확인한다. 초과 시 완화 순서: ① 마을·로딩에서 5등급 후광 생략(`Showcase_Scene.fxMask` 로 조정 가능한 후속, 지금은 필드 없음) ② 오라 프레임 절반. 필요하면 후속 기획.

---

## 8. 갱신 경로 (마을, S8)

| 이벤트 | 반영 | 비고 |
|---|---|---|
| 마을 진입 | `MonsterLook` 6개 계산 → 6마리 배치 | |
| 상점 강화 구매(Lv 변화) | 해당 종족의 `Level`·`Tier` 재계산 → 티어가 바뀌면 T 시트 로드(필요 시) 후 `ApplyOwnedLook` 재적용(프리팹 교체 없음, 티어만 치환) | 강화 팝업이 열린 채여도 즉시(몬스터는 팝업·HUD 아래라 보이지 않을 수 있음) |
| 승급(등급 변화) | 해당 종족의 새 등급 몬스터 프리팹으로 **교체**(Push + Pop, 등장 트윈 0.3초). 승급은 둥지에 자동 배치되므로 유효 편성도 바뀜 | |
| 편성 변경 | 편성 팝업은 **출격(전투 진입)과 한 몸**이라 마을에서 저장 후 머물 수 없다. 다음 마을 진입 때 반영 | 현행 `SortieWithFormation` 흐름 유지 |
| 클라우드 복원·충돌 해결로 프로필 교체 | 프로필 변경 통지 훅에서 6개 전부 재계산·변경분만 갱신 | 로컬만 쓰는 오프라인에서는 발생 안 함 |

- 갱신은 프로필 변경 통지(기존 `OpenMenu` 의 "저장 + VM 통지 일원화") 한 곳에서 **프레임당 1회 합쳐서(coalesce)** 처리한다.

---

## 9. 로드 실패·엣지 케이스

| 상황 | 처리 |
|---|---|
| 등급 프리팹 로드 실패 | 그 종족의 **기준 프리팹(풋내기)** 로 폴백 → 그것도 실패하면 자리(마을은 비움 / 로딩은 정적 자리표시자 유지), 경고 로그 1줄 |
| T 시트 로드 실패·시간 초과 | 티어 0(풋내기 외형), 경고 로그(`MonsterTierSheetLoader` 규칙 그대로) |
| `GradeFxDatabase`·`FormationDatabase` 로드 실패(null) | 편성 = 기본 편성 폴백(`DefaultFormation`), 등급 연출 없음(전투와 같은 가드) |
| 프로필 로드 실패·신규 프로필 | 기본 프로필(전부 Lv0·1등급) → 풋내기 6마리 |
| 승급했는데 등급 프리팹이 아직 없는 빌드(현재 `*G[2-5].prefab` 은 빌드 전) | 풋내기 폴백 + 경고(승급 기능 자체의 선행 조건은 `monster-grades.md` 구현 소관) |
| 씬 진입 직후 연속 이벤트 | 종족별 교체 플래그·대기 1개(§6) |
| 홀로그램 영웅이 챕터 변경으로 교체될 때 | 몬스터와 무관(영웅 쇼케이스 로직 불변) |
| 쇼케이스 비활성 요구(성능 이슈) | 마을·로딩 쇼케이스 전체를 끄는 스위치는 두지 않음(YAGNI). 문제 시 후속 |

---

## 10. 데이터 구조표 (Rule 00 데이터 구조 확인 게이트 · Rule 02 §11-1)

위치·속도·표시 상한은 육안 튜닝으로 자주 바뀌므로 JSON 이다(씬 오브젝트에 직렬화하면 6 x 2 씬 인스턴스를 일일이 고쳐야 함).

```
Showcase_Scene (엔티티, 씬 하나)                             <- 2행 (Village, Loading)
 |-- scene              PK  EScene 값명
 |-- fitMaxHeightDots   float  표시 높이 상한(94)
 |-- startWaitMaxSec    float  마을 시작 대기 상한(2.0)
 |-- waitMinSec, waitMaxSec  float  경유점 도착 후 대기(1.0, 3.5)
 +-- Showcase_Actor     (관계, 복합키 scene + monster)         <- 12행 (씬 2 x 종족 6)
        scene  -> Showcase_Scene.scene
        monster -> 기준 EMonster 값명 (종족)
        behavior       "Wander" | "Stand"
        speedDots      float  도트/초 (Stand 는 0)
        flying         bool
        startFacingLeft bool
        orderBase      int    로딩 정렬 order(마을은 발 y 로 계산)
        +-- Showcase_Actor_Waypoint (관계, 복합키 scene + monster + index)   <- 마을 16행 + 로딩 6행 = 22행
               scene, monster -> Showcase_Actor
               index  int
               x, y   float  도트 좌표 (배경 480x270 기준)
```

**§11-1 판정**: 모든 (씬, 종족)이 항상 하나씩 가지는 값(`behavior`·`speedDots` 등)은 필수 1:1 → `Showcase_Actor` 직접 필드. 경유점은 한 몬스터에 여러 개라 복합키(`scene + monster + index`)의 별도 관계 `Showcase_Actor_Waypoint`(이름 `<부모>_<관계명>`). sentinel 없음(로딩의 `Stand` 도 경유점 1개 = 고정 위치).

| 데이터 | 필드 | 값 |
|---|---|---|
| `Showcase_Scene.json` (`EData.Showcase_Scene`) | `scene` · `fitMaxHeightDots` · `startWaitMaxSec` · `waitMinSec` · `waitMaxSec` | Village: 94, 2.0, 1.0, 3.5 / Loading: 94, 0, 0, 0 |
| `Showcase_Actor.json` (`EData.Showcase_Actor`) | `scene` · `monster` · `behavior` · `speedDots` · `flying` · `startFacingLeft` · `orderBase` | 마을: Wisp Wander 26 / Wraith Stand 0 / Reaper Wander 14 / Hex Wander 12 / Plague Wander 10 / Phantom Wander 34 flying(전부 startFacingLeft false, orderBase 0) · 로딩: Phantom Stand orderBase 20, Hex 30, Wisp 35, Wraith 35(왼쪽), Reaper 31(왼쪽), Plague 21(왼쪽) |
| `Showcase_Actor_Waypoint.json` (`EData.Showcase_Actor_Waypoint`) | `scene` · `monster` · `index` · `x` · `y` | 마을: §4.3 표 16점 · 로딩: §5.2 표 6점 |

- **세이브·클라우드 영향 없음**(읽기 전용 연출 데이터). `MetaProfile` 변경 0.
- 쇼케이스는 기존 `GradeFxDatabase`(등급 연출 수치)·`FormationDatabase`·`MonsterTierSheetSet` 를 읽기만 한다.

---

## 11. 기존 문서와의 정합 (충돌 정리)

| 기존 문구 | 충돌 여부 | 정리 |
|---|---|---|
| `monster-grades.md` §9.5 "편성·상점·도감 UI 미리보기에서 이펙트·후광·맥동을 표시하지 않는다" | **충돌 없음** — 이 문구는 `MonsterIcons`(종족 아이콘 6장) 한정이다. 마을·로딩 씬 월드의 몬스터는 UI 가 아니다 | 문구를 "UI 아이콘(상점·편성·도감 카드)에는 표시하지 않는다. 마을·로딩 씬 월드의 몬스터는 전투와 같은 연출을 표시한다"로 명확화 제안(메인 반영) |
| `monster-grades.md` §9.3 "전장 동시 활성 상한" | 확장 | "전장" → "씬(전투·마을·로딩)"으로 표기 확장 |
| `monster-tier-sheets.md` §7.5 "마을(상점·편성)에서는 T 시트를 로드하지 않는다" | **갱신 필요** | "상점·편성 UI 는 아이콘만 쓰므로 T 시트를 로드하지 않는다. 마을·로딩 쇼케이스는 별도 로더로 필요한 티어만 로드하고 씬 정리 지점에서 해제한다" |
| `scene-2d-conversion.md` §5.6(마을 배회자 6, 정적 씬 배치)·§6.2(로딩 몬스터 6) | 대체 | 마을 정적 배회자 6은 쇼케이스로 대체(경유점 §4.3). 로딩 정적 몬스터 6은 자리표시자로 유지 |
| `village-hud-layout.md` §5 "배회 범위 수정은 범위 밖" | 해소 | 보행 구역(§4.3)이 HUD 가림을 피해 확정 |
| 쇼케이스 vs "별도 메인 메뉴/세팅 화면 금지" | 충돌 없음 | 신규 화면 없음, 월드 연출만 |
| 쇼케이스 vs "신규 몬스터 금지" | 충돌 없음 | 기존 30개 몬스터 프리팹 재사용 |

---

## 12. CLAUDE.md §8 개정안 (직접 수정 금지 - 메인 반영)

"몬스터 등급 2~5 허용" 항목 뒤에 아래 항목을 신설 제안한다.

> - **마을·로딩 몬스터 쇼케이스 허용 (2026-11-08 v0.3 승격) - 단 마을·로딩 씬 월드에 보유 몬스터 6마리를 전투와 같은 외형(강화 티어 전신 시트 + 등급 연출)으로 보여 주는 순수 비주얼에 한정** - 유효 편성(둥지 6곳의 종족별 몬스터)의 기준 몬스터·등급·상점 강화 Lv(→티어)를 읽어 기존 등급 몬스터 프리팹을 쇼케이스 모드(전투 컴포넌트·HP바·충돌 끔)로 마을은 HUD 에 가리지 않는 보행 구역에서 대기·이동, 로딩은 현행 마법진 위 고정 위치에서 대기시킨다. 구매·승급 직후 즉시 갱신하며 등급 이펙트 동시 상한 카운터를 전투와 공유한다. 허용 범위는 ① 연출 JSON 3종(`Showcase_Scene`·`Showcase_Actor`·`Showcase_Actor_Waypoint`) ② 마을·로딩 씬 오브젝트·그림자·쇼케이스 로직 ③ 기존 `MonsterTierSheetLoader` 재사용. **신규 몬스터·신규 UI 화면·별도 메인 메뉴·몬스터 탭/상호작용·전투 로직·카드·도감 연결·저장 데이터 변경은 계속 금지.** UI 아이콘(상점·편성·도감 카드)에는 등급 이펙트를 표시하지 않는다(기존 규칙 유지).

---

## 13. 열린 질문 (사용자 결정, 쉬운 말 + 추천안)

| ID | 질문 | 추천안 |
|---|---|---|
| Q1 | 마을에서 내 몬스터 6마리를 돌아다니게(대기·이동) 할까요, 가만히 서 있게 할까요? | 돌아다니게. 지금도 6마리가 배회하므로 같은 느낌을 유지하되 HUD 에 안 가리는 자리에서만 움직입니다. |
| Q2 | 로딩 화면은 지금처럼 마법진 위에 서 있게 할까요, 로딩 바 위를 걷게 할까요? | 마법진 위(현행). 걷게 하면 바·문구와 겹침 계산이 새로 필요합니다. 처음엔 풋내기가 서 있다가 준비되면 내 몬스터로 바뀝니다. |
| Q3 | 보여 줄 몬스터 기준은 "출격 편성 6마리"로 할까요, "종족별 가진 최고 등급"으로 할까요? | 출격 편성 6마리(전투에서 실제 나가는 모습 그대로). 승급하면 둥지에 자동 배치되므로 보통 같습니다. |
| Q4 | 등급 이펙트(불꽃 기둥·후광 등)도 마을·로딩에서 다 보여 줄까요, 마을은 몸 빛·바닥 오라만 보여 줄까요? | 전부 보여 줍니다(최대 약 29MB, 상한 안). 휴대폰에서 버벅이면 후광부터 줄입니다. |
| Q5 | 너무 큰 몬스터(예: 최종 강화 + 군주 망령)는 화면 높이의 35%까지만 보이게 줄여도 될까요? | 줄입니다. 안 줄이면 HUD·홀로그램 영웅을 크게 가립니다. 전투 크기는 그대로입니다. |
| Q6 | 마을에서 몬스터를 눌러도 아무 반응이 없게 둘까요? | 반응 없음(이번 범위는 보여 주기만). 상호작용은 별도 기획. |
| Q7 | 마을·로딩 몬스터 바닥의 강화 링·바닥 원판은 숨기고 그림자만 보일까요? | 숨깁니다(전투 바닥 표시이고 마을 배경과 어울리지 않음). 강화 정도는 몸 모습 자체로 보입니다. |

---

## 14. 검증 게이트

### 14.1 불변식·자동 (test-engineer)

| ID | 항목 | 통과 기준 |
|---|---|---|
| T-S1 | 모드 복구 | `SetShowcaseMode(true)` 후 전투 컴포넌트·충돌·HP바·바닥 표시가 모두 꺼지고, `false` 후 **새로 Pop 한 상태와 동일**(컴포넌트 enabled·표시 상태 비교) |
| T-S2 | 외형 계산 | `MonsterLook` 6개가 전투 `CacheSpeciesLevels`·`LoadMonsterTierSheets` 와 같은 (기준, 등급, 티어) 결과(공유 함수) |
| T-S3 | 폴백 | 등급 프리팹 실패 → 기준 프리팹, T 시트 실패 → 티어 0, 모두 NRE 0 |
| T-S4 | 풀 | Pop → Push 후 전투 풀에서 Pop 한 인스턴스가 티어 0·등급 연출 0·전투 컴포넌트 enabled |
| T-S5 | 핸들 | 로딩 → 마을 → 전투 전환에서 티어 시트·프리팹 핸들 누수 0, 이중 해제 0, 전투 시작 `GradeFxBudget` 카운터 0 |
| T-S6 | 이펙트 상한 | 5등급 6마리 쇼케이스에서 오라 6·기둥 24·후광 6 사용, Push 후 0 |
| T-S7 | 갱신 | 강화 구매 → 해당 종족 티어만 교체, 승급 → 해당 종족 프리팹 교체(중복 인스턴스 0), 연속 구매 합쳐 처리 |
| T-S8 | 로딩 교체 | 자리표시자가 데이터 준비 후 같은 위치에서 교체, 외형이 같으면 교체 연출 생략, 5.5 대기 5초 초과 시 시작 가능 |
| T-S9 | 전투 오염 | 쇼케이스 인스턴스는 `MonsterDied`·도감·`BattleController` 목록에 영향 0 |
| T-S10 | 데이터 | JSON 3종 로드·스키마·경유점이 보행 레인 안(§4.3 검산 자동 검사) |

### 14.2 육안·성능 (사용자 / gameplay-programmer)

| ID | 항목 | 통과 기준 |
|---|---|---|
| V-S1 | 가림 | 마을 6마리의 발·몸이 HUD 에 거의 가려지지 않고(머리가 ◀▶·메뉴 뒤로 일부 들어가는 것은 허용) 이동 중에도 발이 레인 밖으로 안 나감 |
| V-S2 | 홀로그램 | 최대 크기(T3·G5)에서도 홀로그램 영웅 몸통을 크게 가리지 않음 |
| V-S3 | 일치 | 전투에서 보이는 같은 몬스터와 티어·등급 모습이 일치(크기 배율 fit 제외) |
| V-S4 | 성능 | 마을 6마리 T3·G5 상태 p95 프레임 시간 ≤ 16.6ms(기준 기기는 사용자 지정), 로딩도 동일 |
| V-S5 | 로딩 | 자리표시자 → 내 외형 교체가 자연스럽고 로딩 완료 문구·바·제목을 가리지 않음 |
| V-S6 | 메모리 | 마을·로딩 쇼케이스 포함 몬스터 시트+이펙트 메모리 ≤ 35MB |
| V-S7 | 재질 | 전투 재질을 쓴 몬스터가 마을 조명·배경 팔레트에서 이질적이지 않음(마을 조명은 안 받음 - 허용 여부 육안 판단) |

---

## 15. 구현 요청사항 (gameplay-programmer 용)

### Enum / Interface / 에셋 키 / 스키마
- **Enum**: `EData` 에 `Showcase_Scene`·`Showcase_Actor`·`Showcase_Actor_Waypoint` 추가. (`EMonster`·`EMonsterTierSheet`·`EScene` 기존)
- **Interface**: 몬스터 루트 파사드에 쇼케이스 모드 의도 API(`SetShowcaseMode(bool)`) 1개(신규, Rule 02 §10·§9 `CommonInterface.cs` 의 기존 몬스터 인터페이스에 추가 또는 신규 `IMonsterShowcase`는 gameplay-programmer 판단).
- **에셋 키**: 신규 Addressable JSON 3개(값명 = 파일명). 쇼케이스 슬롯(그림자 + 위치 홀더) 프리팹 1개(비-Addressable 또는 Addressable 은 판단, Rule 04 §1·§3 빌더 일회용).
- **스키마**: §10 표 그대로. 승인 게이트는 메인이 §10 구조표로 받는다.

### 할 일
1. 공유 순수 함수: 유효 편성 + 강화 Lv → `MonsterLook[6]`(§2), 전투 `LoadMonsterTierSheets`·`CacheSpeciesLevels` 와 공유(중복 구현 금지).
2. 쇼케이스 모드(§3), 전투 구성부(817~838행)의 시각 부분(등급 `Configure` + `ApplyLevel` + `SetTierSheetProvider`)을 공용 함수로 추출해 쇼케이스가 호출.
3. 마을: 정적 `VillageWanderer` 6 제거 → 런타임 쇼케이스 6 생성(경유점 JSON, 기존 `StepToward`·`NextWaypointIndex` 재사용), 프로필 변경 통지 갱신(§8), 씬 정리 지점 반환(§6).
4. 로딩: `LoadingController` 4.6·5.5 단계(§5.1), 정적 몬스터를 자리표시자로 유지·교체.
5. JSON 3종 로더·데이터 파일, `GradeFxBudget.Reset()` 호출 지점(마을·로딩 진입).
6. 일회용 에디터 툴(필요 시): 슬롯 프리팹 생성 → 실행 후 삭제(Rule 04 §3).

### 코드 연결점(읽기 전용 조사 결과)
`VillageController.SpawnIdleHero/ReleaseShowcase/SetBattleComponents` · `VillageWanderer.StepToward/NextWaypointIndex` · `LoadingController.Start` · `BattleController` 구성부 817~838·`LoadMonsterTierSheets` · `MonsterTierSheetLoader` · `MonsterGradeVisual.Configure` · `MonsterEnhancementVisual.ApplyLevel/SetTierSheetProvider/SetGradeVisual` · `GradeFxBudget` · `FormationSave.Resolve` · `GradeFxDatabase.SpecOf` · `DotStageMapping.VillageDotToGround`.

### 후속 문서 개정(메인 전달)
§11 표의 4건(`monster-grades.md` §9.5·§9.3, `monster-tier-sheets.md` §7.5, `scene-2d-conversion.md` §5.6) + CLAUDE.md §8(§12).
