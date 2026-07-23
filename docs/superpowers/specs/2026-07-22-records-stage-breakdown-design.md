# 기록 팝업 — 스테이지별 전적 (Spec)

- 작성일: 2026-07-22
- 단계: v0.3
- 목업: `.mockups/records-stage-list.html`
- 관련 spec: `docs/superpowers/specs/2026-07-21-hero-stage-variant-design.md` (스테이지 재스킨)

## 1. 의도

마을 기록 팝업이 지금은 총계 4항목(총 출격 / 승리 / 승률 / 최단 클리어)만 보여준다. 여기에 **스테이지 1~5 각각의 전적**을 스크롤 리스트로 덧붙여, 플레이어가 "어느 스테이지에서 몇 번 이겼는지 · 다음에 뭘 깨야 하는지"를 한 화면에서 읽게 한다.

각 행은 그 스테이지의 재스킨 영웅 초상 + 위협도 ★ + 승리 수를 보여준다. 잠긴 스테이지도 회색 실루엣으로 남겨 다음 목표를 드러낸다.

## 2. 범위

**포함**
- `MetaProfile` 에 스테이지별 전적(판수 / 승수 / 최단시간) 필드 추가 + `Version` 3
- `BattleController` 정산에서 스테이지별 집계
- `RecordsPopup` 을 `CHPoolingScrollView` 3-class 구조로 재구성 + 셀 프리팹 신규
- 클라우드 복원(`CopyFrom`) 에 신규 필드 반영

**제외**
- 상단 총계 산식 변경 — 기존 `TotalRuns` / `TotalWins` / `BestClearTime` 그대로
- 랭킹·클라우드 동기 UI 변경
- 신규 영웅·스테이지 리소스 제작 (CLAUDE.md §8)

## 3. 확정된 결정

| # | 결정 | 근거 |
|---|---|---|
| D1 | 스테이지별로 **판수·승수·최단시간** 3개를 저장 | 승률은 두 수로 계산 가능. 스테이지별 최단시간이 있어야 재도전 동기가 남는다 |
| D2 | 기존 유저는 **스테이지별 기록 전부 0부터 시작** | 과거 `RunRecord` 에 스테이지 필드가 없어 역산 불가 |
| D3 | 상단 총계는 **그대로 유지, 안내 문구 없음** | 화면을 깨끗하게 유지. 총계와 스테이지 합의 불일치는 감수 |
| D4 | 잠긴 스테이지도 **행으로 표시** — 어두운 실루엣(`Color.Lerp(tint, black, 0.55)` — 채도 제거가 아닌 명도 저하, 캐러셀·영웅 목록 잠금 톤과 동일) + "스테이지 N-1 클리어 필요" | 마을 캐러셀의 잠금 UX 와 일관. 다음 목표를 드러냄 |
| D5 | UI 는 `CHPoolingScrollView` **3-class 분리** | Rule 03 §3 필수 패턴. 참조 구현은 `BuildModalPopup` |
| D6 | 마이그레이션 **변환 코드 없음** | `JsonUtility` 가 없는 필드를 C# 초기값으로 채운다 — `SelectedStage`/`ClearedStage` 도입 때와 동일 |

## 4. 데이터 설계

`Assets/_Lair/Scripts/Meta/MetaProfile.cs`

```csharp
public int Version = 3;                     //# 2 → 3
public List<StageRecordEntry> StageRecords = new List<StageRecordEntry>();

[Serializable]
public class StageRecordEntry
{
    public int   Stage;                     //# 1~5
    public int   Runs;
    public int   Wins;
    public float BestClearTime = -1f;       //# 없으면 -1 (기존 BestClearTime 규약과 동일)
}
```

- **엔트리-리스트 패턴** — `JsonUtility` 가 `Dictionary` 를 직렬화하지 못하므로 기존 `ShopLevels` / `ShopLevelEntry` 와 같은 형태를 따른다.
- **조회/갱신 헬퍼** — `GetStageRecord(int stage)` 는 엔트리가 없으면 기본값(0/0/-1)을 돌려주고, 갱신 헬퍼는 없으면 엔트리를 추가한다. `GetShopLevel` / `SetShopLevel` 과 같은 구조.
- **`CopyFrom` 에 반드시 추가.** 이 메서드는 필드별 수동 복사라 빠뜨리면 클라우드 복원 시 스테이지 기록만 유실된다. (백업 경로는 `JsonUtility.ToJson(profile)` 통째라 자동으로 실린다)
- `Version` 3 은 클라우드 `schemaVersion` 필드로 나가 서버 쪽 스키마 구분에 쓰인다.

## 5. 집계

`Assets/_Lair/Scripts/Battle/BattleController.cs` — 기존 정산 블록(`profile.TotalRuns++` 부근) 한 곳에서만 갱신한다. 대상 스테이지는 `profile.SelectedStage`.

```
Runs++                                        //# 승패 무관
result == Win  → Wins++
               → BestClearTime 갱신 (없거나(-1) 더 빠를 때만)
```

- 순서 계약은 기존 그대로 — 런 보상 → 프로필 가산(**여기에 스테이지 집계 포함**) → 영주 보상 → 도전과제 → 저장.
- 도전과제 판정은 총계(`TotalWins`/`TotalRuns`) 기준 유지 — 스테이지별 수치는 판정에 쓰지 않는다.

## 6. UI 설계

### 6.1 프리팹

```
RecordsPopup.prefab
├ Dim (CHButton) / Title "기 록" / Close (CHButton)
├ Summary — 정적 4칸 (총 출격 / 승리 / 승률 / 최단 클리어)
└ ScrollView
   ├ ScrollRect + RecordsStagePoolingScrollView   (_origin → origin Cell 인스턴스)
   └ Viewport (Image + RectMask2D) → Content (RectTransform — LayoutGroup 없음)
        └ RecordsStageCell.prefab 인스턴스 (origin — 풀 prototype)

RecordsStageCell.prefab   ← 별도 파일 (재사용 가능한 단일 셀)
└ Portrait(Image) / STAGE N(CHText) / 위협도(CHText) / 최단(CHText)
  / 승리수(CHText) / 판수·승률(CHText) / LockHint(CHText) / SelectedBadge
```

셀 높이는 균일(약 82px). 모든 GameObject 는 프리팹에 정적 배치하고 인스펙터로 배선한다 — 코드 동적 생성 금지 (Rule 03 §3).

Content 에 `VerticalLayoutGroup` 을 붙이지 않는다 — `CHPoolingScrollView` 가 `_itemGap`/`_padding` 으로 셀 위치를 직접 계산하므로 LayoutGroup 과 서로 덮어쓴다. 선례 `BuildModalPopup.prefab`/`CodexPopup.prefab` 도 LayoutGroup 없이 구성돼 있다.

### 6.2 코드 3-class

| 클래스 | 책임 |
|---|---|
| `RecordsPopup : UIBase` | 요약 4칸 텍스트 갱신 + `_scrollView.SetItemList(BuildRows(profile, config))` |
| `RecordsStagePoolingScrollView : CHPoolingScrollView<RecordsStageCell, RecordsStageCellData>` | `InitItem` / `InitPoolingObject` 만 오버라이드 |
| `RecordsStageCell : MonoBehaviour` | `[SerializeField]` 자식 참조 + `Bind(data)` + `OnEnable` 풀 재사용 리셋 |

`RecordsStageCellData` 는 표시 확정값만 담는 POCO (스테이지 번호 / 틴트 / 잠금 여부 / 표시 문자열들 / 선택 중 여부). 셀은 계산하지 않는다.

### 6.3 행 구성 규칙

스테이지 1~5 전부, 번호 오름차순 고정.

- **해금** (`StageProgress.IsUnlocked(n, profile.ClearedStage)`) — 컬러 초상 + `{Wins}승` + `{Runs}판 · {승률}%` + `최단 {m:ss}`
- **잠금** — grayscale 초상 + "스테이지 N-1 클리어 필요", 전적 숨김
- **선택 중** — `profile.SelectedStage` 와 같고 해금 상태이면 금색 「선택 중」 배지
- 승률은 `Runs > 0` 일 때만 계산(반올림), 아니면 0%
- 최단시간이 -1 이면 "-"

### 6.4 초상

작업 D 에서 이미 쓰고 있는 방식을 그대로 재사용한다 — `Knight` 초상 스프라이트 1장에 `HeroStageVariantConfig.GetStage(n).TintColor` 를 곱해 스테이지 색을 낸다. 신규 이미지 리소스를 만들지 않는다.

### 6.5 순수 함수 분리

행 데이터 조립은 `static RecordsStageCellData[] BuildRows(MetaProfile, HeroStageVariantConfig)` 로 뺀다. MonoBehaviour 없이 EditMode 테스트가 가능해야 한다 — 현재 `RecordsPopup.BuildBody` 가 static 인 것과 같은 이유.

## 7. 테스트 (EditMode, 전부 순수 함수)

- `GetStageRecord` — 엔트리 없는 스테이지 조회 시 0/0/-1, 갱신 시 엔트리 추가·재갱신
- 집계 — 패배는 `Runs` 만 증가 / 승리는 `Wins`+최단 갱신 / 최단은 더 빠를 때만 교체
- `BuildRows` — 항상 5행, 잠금 판정 경계(`ClearedStage`=0·3·5), 승률 반올림, 최단 -1 → "-", 선택 중 배지는 해금일 때만
- `CopyFrom` 라운드트립 — 클라우드 복원 후 스테이지 기록 보존
- 구버전 JSON(`StageRecords` 필드 없음) 로드 → 빈 리스트 + 예외 없음

## 8. 리스크

| 리스크 | 대응 |
|---|---|
| `CopyFrom` 누락 시 클라우드 복원에서 기록 유실 | 라운드트립 테스트로 고정 |
| 기존 유저가 "총 40승인데 스테이지 합 0승"을 버그로 오해 | D3 으로 감수 확정. 문의가 실제로 들어오면 안내 문구를 후속 추가 |
| origin Cell 인스턴스에서 컴포넌트 제거 시 참조 null | Rule 03 §3 체크리스트 — `m_RemovedComponents` 금지 |
