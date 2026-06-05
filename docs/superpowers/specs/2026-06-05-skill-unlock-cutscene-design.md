# 영웅 스킬 해금 컷인 연출 — 설계 (spec)

- 작성일: 2026-06-05
- 흐름: start-develop (표준 — design-reviewer / ⛔승인 게이트 / code-reviewer / test-engineer 포함)
- 대상 시스템: `Assets/_Lair/Scripts/Character/Skills/HeroSkillRunner.cs`, `Assets/_Lair/Scripts/Battle/BattleCamera.cs`, `BattleController.cs`, 신규 컷인/배너, `Assets/_Lair/Editor/LairUIPrefabBuilder.cs`

## 1. 의도 / 범위

영웅 HP가 스킬 해금 임계값(HeroSkillLoadout 의 Phase HpFraction — 예 90/60/30%)에 **처음 도달하는 순간**, 그 스킬이 해금됐음을 알리는 격겜 컷인 연출을 넣는다. 플레이어가 "방금 영웅이 무슨 스킬을 얻었는지"를 확실히 인지하게 하는 게 목적.

**동작 시퀀스 (결정 락)**:
1. 스킬 해금 임계 도달 → 게임 시간 정지 (`PauseService.Pause`)
2. 카메라 쉐이크 진입
3. HUD 배너가 **왼쪽 화면 밖 → 중앙**으로 부드럽게 슬라이드 인
4. 중앙에서 적당한 시간 머무름 (스킬명을 읽을 시간)
5. **오른쪽 화면 밖**으로 슬라이드 아웃
6. 게임 시간 재개 (`PauseService.Resume`)

정지 중 배너·쉐이크는 전부 `unscaledDeltaTime` 으로 구동(timeScale=0 무관하게 진행).

**배너 텍스트**: `영웅의 '{DisplayName}' 스킬 해제` — 한글, 아이콘 없음. 예: `영웅의 '회전 블레이드' 스킬 해제`.

**범위 밖**:
- 사운드 — **이번 작업 미포함**(나중에 추가). 단, 컷인 시작 지점에 `CHMSound.Play(EAudio.xxx)` 한 줄만 끼우면 되도록 **호출 seam(주석/빈 메서드)만 남긴다**. CLAUDE.md §8 사운드 금지 규칙 문서는 수정하지 않는다.
- 스킬별 아이콘 이미지 (HeroSkillData 에 Sprite 필드 추가 안 함).
- 스킬 발동 타이밍/데미지 변경 없음 — 컷인은 관측만. (정지 중엔 스킬도 Tick 안 함)
- 밸런스 시뮬(qa-simulator) 미포함.

## 2. 현재 구조 (조사 완료)

- **해금 트리거**: `HeroSkillRunner.Update()` (`Skills/HeroSkillRunner.cs:51-59`) — `_gate.Poll(_health.Ratio, _newly)` 후 `_newly` 인덱스마다 `_loadout.Phases[idx].Skill` 이 `data != null` 이면 `_active.Add(data.CreateRuntime())`. **이 지점이 "스킬을 얻는 순간"**.
- **스킬 표시명**: `HeroSkillData.DisplayName`(string) 존재. 아이콘 없음.
- **정지 인프라**: `Battle/PauseService.cs` — `Pause`/`Resume`/`ForcePause`, **depth 카운터로 중첩 안전**. `Time.timeScale` 제어.
- **정지 사용처**: `BattleController.cs:675/700` — 카드 3택1 팝업(`CardSelectionPopup`)이 `_pause.Pause()`↔`_pause.Resume()` 로 감싼다. 패시브 카드 픽도 HP%에서 트리거(`PassiveTriggerService`) → **스킬 해금과 같은 HP 구간에서 겹칠 수 있음**.
- **카메라**: `Battle/BattleCamera.cs` — Main Camera 부착, `ApplyZoom()` 이 `transform.position = _worldAnchor + (-_forward) * _currentDist`. **주의**: `ApplyZoom` 은 `if (Mathf.Approximately(_currentDist, _targetDist)) return;` 로 줌이 안정되면 early-return → 그 경로에 쉐이크를 넣으면 평소(줌 안 할 때) 안 돈다. 이미 `unscaledDeltaTime` 사용.
- **HUD/팝업 빌더**: `Editor/LairUIPrefabBuilder.cs` — `M4 - Build UI Prefabs`(`BuildAllUIPrefabs`)가 `BattleHud`/`CardSelectionPopup`/`ResultPopup` 프리팹을 **코드로 재생성**. ⚠ 손-편집 프리팹은 다음 빌더 실행 때 덮어써진다 → 신규 UI 는 **빌더 코드에 추가**해야 영속.
- **CHMUI 팝업 선례**: `CardSelectionPopup` 은 독립 `EUI` 팝업이며 정지 중 표시·애니메이션된다 — 배너의 정확한 패턴 선례.
- **EUI**: `Data/CommonEnum.cs:27` — `BattleHud, ResultPopup, CardSelectionPopup, BuildModalPopup, SpawnerStatusTooltip, SynergyModalPopup`.
- **배선**: `BattleController` 가 composition root — `skillRunner.Bind(loadout)`(`:356-361`), HUD 표시(`:111`), `_pause = new PauseService()`(`:115`).

## 3. 설계

### 3-1. 배너 = 독립 EUI 팝업 (빌더 생성)

배너를 BattleHud 에 박지 않고 **독립 `EUI.SkillUnlockBanner` CHMUI 팝업**으로 만든다 (CardSelectionPopup 선례).
- `CommonEnum.cs` 의 `EUI` 에 `SkillUnlockBanner` 추가 (enum 끝에 — int 직렬화 정합).
- `LairUIPrefabBuilder` 에 `BuildSkillUnlockBanner(...)` 추가하고 `BuildAllUIPrefabs` 에서 호출 → 빌더 재실행해도 영속. **BattleHud 빌더 코드는 건드리지 않는다.**
- 프리팹 구성: 풀폭 가로 밴드(RectTransform `_root`) + 중앙 `CHText _label`(Rule 03 §3, TMP 동반). 초기 위치 = 화면 왼쪽 밖.

### 3-2. 컴포넌트 (신규 3 + 수정 2 + enum/builder)

| 구분 | 대상 | 역할 |
|---|---|---|
| 수정 | `HeroSkillRunner` | 해금 순간 `event Action<HeroSkillData> OnSkillUnlocked` 발행. `_active.Add` 직후 `OnSkillUnlocked?.Invoke(data)` 1줄. |
| 수정 | `BattleCamera` | `ICameraShake` 구현. `Shake(float duration, float magnitude)` 추가. **매 unscaled 프레임 무조건** `pos = (anchor 기준 base 줌 위치) + shakeOffset` 합성 (early-return 경로 밖에서). 감쇠 랜덤 오프셋, 종료 시 offset=0 복원(드리프트 없음). |
| 신규 | `ISkillUnlockBanner` (인터페이스) | `IEnumerator PlayCo(string text)` + `void HideImmediate()`. 컨트롤러가 View 구체 대신 인터페이스 참조 → EditMode 모킹. CommonInterface 또는 배너 파일에 정의. |
| 신규 | `SkillUnlockBannerView : UIBase` (or MonoBehaviour) | 순수 View. `[SerializeField] private RectTransform _root; [SerializeField] private CHText _label;`. `PlayCo(text)` = 라벨 세팅 + 왼쪽밖→중앙(인) → 홀드 → 중앙→오른쪽밖(아웃) 슬라이드, **unscaled lerp**. `ISkillUnlockBanner` 구현. Rule 02 §6.1 위젯 캡슐화 — 외부엔 의도 API 만. |
| 신규 | `SkillUnlockCutsceneController` (plain C# class) | 오케스트레이터. 보유: `PauseService`, `ICameraShake`, `ISkillUnlockBanner`, 코루틴 host(`MonoBehaviour`). `Queue<string> _pending`. `Enqueue(string skillName)` → 진행 중 아니면 host.StartCoroutine(RunQueueCo). `IEnumerator RunQueueCo()`: `Pause()` 1회 → 큐 빌 때까지 [`Shake(...)` + `yield return _banner.PlayCo(format(name))`] → `Resume()` 1회. 텍스트 포맷(`영웅의 '{0}' 스킬 해제`) 여기서. **A안 순차 처리**. |
| enum | `CommonEnum.cs` | `EUI.SkillUnlockBanner` 추가. |
| builder | `LairUIPrefabBuilder.cs` | `BuildSkillUnlockBanner` 추가 + `BuildAllUIPrefabs` 등록. |

### 3-3. 배선 (BattleController)

- HUD 표시 후, `EUI.SkillUnlockBanner` 팝업을 1회 표시(또는 인스턴스 확보)해 `SkillUnlockBannerView` 핸들 확보 → 초기 `HideImmediate`.
- `SkillUnlockCutsceneController` 생성: **기존 `_pause`** + `BattleCamera`(ICameraShake) + 배너 View + 코루틴 host 주입. (카드 픽과 같은 PauseService depth 공유 → timeScale 충돌 없음.)
- `skillRunner.Bind(loadout)` 직후 구독: `skillRunner.OnSkillUnlocked += d => _cutscene.Enqueue(d.DisplayName)`.
- **이중 구독 가드**: 영웅은 풀 객체(count 1) → 라운드 재시작 시 재구독 중복 방지(구독 전 해제 또는 1회 보장).

### 3-4. 기본 수치 (인스펙터 튜닝)

- 슬라이드 인 0.35s / 홀드 1.2s / 아웃 0.35s → 스킬당 정지 ~1.9s.
- 카메라 쉐이크 0.4s, 세기 0.3 유닛.
- 화면 밖 X 오프셋: 배너 폭 + 여유(완전히 화면 밖).

## 4. 결정 락 (Locked)

- 시퀀스: 정지 → 쉐이크 → 왼쪽밖→중앙 인 → 홀드 → 중앙→오른쪽밖 아웃 → 재개. 전부 unscaled.
- 배너 텍스트: `영웅의 '{DisplayName}' 스킬 해제` (한글, 아이콘 없음).
- 동시 해금(HP 급락): **A안 순차** — 큐에 쌓아 정지·유지한 채 하나씩 재생, 큐 드레인 후 1회 Resume.
- 배너는 **독립 `EUI.SkillUnlockBanner` 팝업** + 빌더 생성(`LairUIPrefabBuilder`). BattleHud 무수정.
- 정지는 **기존 PauseService 인스턴스 공유**(카드 픽과 depth 공유).
- 카메라 쉐이크는 **매 unscaled 프레임 무조건 합성**(ApplyZoom early-return 경로 밖), base+offset, 종료 시 offset 복원.
- 컨트롤러는 plain C# class — IEnumerator 시퀀스, MonoBehaviour host 가 StartCoroutine.
- 사운드 미포함, 호출 seam 만 남김.

## 5. 엣지 케이스

- **동시 해금(A안)**: 큐 순차. 진행 중 Enqueue 는 큐에 누적, 코루틴 1개만 구동.
- **카드 픽 중첩**: 같은 PauseService depth 공유 → timeScale 충돌 없음. 드물게 배너+카드팝업 시각 중첩 허용(배너 ~1.9s 후 사라짐).
- **전투 종료 중 컷인**: 사망/승리 시 `ForcePause` 가 depth 를 크게 잡음 → 컷인 Resume 이 timeScale 못 풀음(종료 정지 유지, 안전). 진행 코루틴은 무해. 라운드 리셋 시 큐 비움.
- **풀 재사용/재시작**: 컨트롤러 큐·진행 코루틴 리셋. OnSkillUnlocked 재구독 중복 방지.
- **빈 DisplayName**: `영웅의 '' 스킬 해제` 방지 — 빈/null 이면 fallback 문구(예: `영웅의 새 스킬 해제`) 또는 컷인 스킵. 구현에서 가드.
- **카메라 쉐이크 미발동 함정**: ApplyZoom early-return 때문에 줌 안 할 때 안 도는 버그 → base+offset 합성을 무조건 경로로.

## 6. 테스트 (test-engineer)

- 컨트롤러 큐: 진행 중 Enqueue → 순차 재생(코루틴 1개), 큐 드레인 후 **Resume 정확히 1회**(Pause:Resume = 1:1).
- 동시 다중 Enqueue → 모든 항목 순차 PlayCo 호출.
- 스킬당 `ICameraShake.Shake` 1회 호출.
- 빈/null DisplayName → fallback 또는 스킵 가드 검증.
- 모킹: `PauseService`(plain)·mock `ICameraShake`·mock `ISkillUnlockBanner`(PlayCo 즉시 완료 IEnumerator). `RunQueueCo()` 수동 펌핑으로 EditMode 검증.
- `HeroSkillRunner.OnSkillUnlocked` — 해금 시 발행, 미구독(null) 시 NRE 없음.

## 7. 영향 / 안전

- 게임플레이 데미지/밸런스 무변경 — 컷인은 정지·관측만. 단 스킬당 ~1.9s 정지가 5분 타이머 페이싱에 미세 영향(정지 중 타이머도 멈춤 → 실질 무영향). 밸런스 의심 시 별도 qa-simulator 권장.
- 코딩 룰(Rule 00~04) 준수: CHText(Rule 03 §3), 빌더 생성 프리팹(Rule 04 / 메모리 M4 clobber 회피), MVVM View 캡슐화(Rule 02 §6.1), 인터페이스 의존(Rule 02 §5/§9), CHMPool/CHMResource 경유.
