# Rule 11 — ChvjPackage UI 컴포넌트 우선 사용

## 룰
UI 컴포넌트는 **ChvjPackage 의 래퍼(CHText / CHButton / CHToggle / CHPoolingScrollView)** 를 우선 사용한다. Legacy `UnityEngine.UI.Text`, 단일 `Button`/`Toggle` 직접 사용을 지양.

> **정적 라벨도 예외 없음** — 코드에서 참조하지 않는 섹션 헤더·장식 텍스트도 프리팹에 TMP_Text 가 있으면 반드시 CHText 를 함께 붙인다. "코드 참조 없으면 CHText 불필요"는 잘못된 판단이다.

## 매핑 (필수 사용)
| UGUI 표준 | ChvjPackage 래퍼 | 이유 |
|---|---|---|
| `UnityEngine.UI.Text` (Legacy) | **`CHText`** + `TMP_Text` | TMP 기반 + `IStringProvider` 로컬라이제이션 + `IFontProvider` 동적 폰트 + `SetText(params object[])` 포맷팅 |
| `Button` 단일 | `Button` + **`CHButton`** | `ClickSoundHook` 으로 사운드 자동 + `CompositeDisposable` 친화 `OnClick(Action, disposable)` + `SetText` 자식 TMP 자동 인식 |
| `Toggle` 단일 | `Toggle` + **`CHToggle`** | `ChangeSoundHook` + Unity 첫 frame 콜백 자동 무시 |
| `ScrollRect` + 수동 풀링 | **`CHPoolingScrollView<TItem, TData>`** | 풀링·재사용 자동 (`SetItemList`, `SetScrollPosition`) |

## 사용 패턴

### CHText (timer/HP 텍스트 등)
```csharp
//# 프리팹 구성: GameObject + RectTransform + TMP_Text + CHText
//# 단순 표시 (stringID -1): SetText 가 args concat
public class BattleHud : UIBase
{
    [SerializeField] private CHText _timerText;

    private void HandleTimer(float elapsed, float total)
    {
        float remain = Mathf.Max(0f, total - elapsed);
        _timerText.SetText($"{(int)(remain / 60)}:{(int)(remain % 60):00}");
    }
}
```

### CHButton (UI 버튼)
```csharp
//# 프리팹 구성: GameObject + RectTransform + Image + Button + CHButton
//# 자식: TMP_Text (CHButton.SetText 가 자동으로 찾아감)
public class ResultPopup : UIBase
{
    [SerializeField] private CHButton _restartButton;

    public override void InitUI(UIArg arg)
    {
        //# disposable 오버로드 — closeDisposable.Clear() 시 listener 자동 해제
        _restartButton.OnClick(OnClickRestart, closeDisposable);
    }
}
```

### CHToggle
```csharp
//# 프리팹: GameObject + Toggle + CHToggle
[SerializeField] private CHToggle _muteToggle;
//# 사운드 hook 은 부팅 시 한 번 등록 — Rule 11 적용 어느 곳이든 자동 효과
```

## 부팅 시 hook 등록 (게임 측 1회)
```csharp
//# Bootstrap 또는 BattleSceneEntry 에서 1회
CHButton.ClickSoundHook  = () => CHMSound.Instance?.Play(EAudio.UIClick);
CHToggle.ChangeSoundHook = () => CHMSound.Instance?.Play(EAudio.UIClick);
CHText.StringProvider    = new LairStringProvider();   //# 다국어 도입 시
CHText.FontProvider      = new LairFontProvider();     //# 다국어 도입 시
```

Slice A 엔 사운드/다국어 없으므로 hook 등록 생략 가능. Hook 미등록 시 사운드만 안 나고 텍스트는 정상 표시.

## TextMeshPro 의존
- CHText 는 `[RequireComponent(typeof(TMP_Text))]`
- com.unity.ugui 2.0+ 에 TMP 통합 (Unity 2022.3+)
- 첫 프로젝트 셋업 시 **Window → TextMeshPro → Import TMP Essential Resources** 1회 필요 (폰트 SDF 등)

## 금지 예시
```csharp
//# (X) Legacy Text 직접 사용
[SerializeField] private Text _timerText;
_timerText.text = "5:00";

//# (X) Button onClick 직접 + 사운드 hook 누락
_restartButton.onClick.AddListener(OnClick);   //# CHButton 미사용 → 사운드 X
```

프리팹 구성 금지 (YAML 관점):
```
//# (X) 정적 라벨 — TMP_Text 만 있고 CHText 없음
GameObject "Label"
  ├ RectTransform
  ├ CanvasRenderer
  └ TextMeshProUGUI   ← CHText 래퍼 누락

//# (O) 올바른 구성
GameObject "Label"
  ├ RectTransform
  ├ CanvasRenderer
  ├ TextMeshProUGUI
  └ CHText (_stringID: -1)
```

## 권장 예시
```csharp
//# (O) CHText + SetText
[SerializeField] private CHText _timerText;
_timerText.SetText("5:00");

//# (O) CHButton + disposable OnClick
[SerializeField] private CHButton _restartButton;
_restartButton.OnClick(OnClickRestart, closeDisposable);
```

## 예외
다음 경우 Legacy 또는 raw `TMP_Text`/`Button` 직접 사용 허용:
- **에디터 전용 UI** (Inspector, EditorWindow) — UGUI 와 무관
- **WorldSpace 디버그 표시** — 캐릭터 머리 위 임시 HP 바 등 (단순 라벨)
- **ChvjPackage 의 래퍼가 지원하지 않는 컴포넌트** (Slider, Dropdown 등) — 단, 사운드 hook 이 필요하면 비슷한 래퍼 추가 검토

## Rule 07 과의 관계
- Rule 07: ChvjPackage 기존 API 사용, 패키지 수정 금지
- Rule 11: ChvjPackage UI 래퍼를 우선 채택 (Rule 07 의 UI 특화 적용)
