# Rule 03 — ChvjPackage 인프라

> 구 Rule 07(패키지 기준), 08(Enum 키), 11(UI 컴포넌트), 12(CHMPool), 13(UIArg) 통합.

---

## 1. 패키지 기준 작업

모든 작업은 `com.chvj.unityinfra` 패키지를 기준 인프라로 두고 진행한다.

**위치**: `Packages/com.chvj.unityinfra/`

**모듈**: `Runtime/Core` · `Resource` · `Pool` · `Audio` · `UI` · `Ads` · `Iap` · `Social` / `Editor/` / `Tests/`

**가이드**:
- 신규 기능 작성 전 패키지 내 동일/유사 기능 존재 여부 먼저 확인
- 게임 코드(`Assets/`)에서 중복 구현 금지 — 패키지 API 사용
- 패키지에 없는 공통 기능은 패키지에 추가한 뒤 사용
- 외부 의존성 추가 시 `package.json` 의 `dependencies` 에 명시
- 버전 변경 시 `version` 필드 업데이트

**의존성 방향** (역방향 참조 금지):
```
Assets/ (게임 코드)  →  Packages/com.chvj.unityinfra  →  Unity 표준 패키지
```

체크리스트:
- [ ] 필요한 기능이 `com.chvj.unityinfra` 에 이미 있는가?
- [ ] 패키지 의존성 방향이 올바른가? (게임 → 패키지 ✅ / 패키지 → 게임 ❌)
- [ ] asmdef 참조가 올바르게 설정되어 있는가?

---

## 2. Enum 키 기반 로드 — 이름 일치 강제

`CHMResource` / `CHMUI` 는 `Enum.ToString()` 을 키로 에셋을 로드한다. **Enum 값 이름 = 에셋 파일명(확장자 제외)** 이 정확히 일치해야 한다.

**규칙**:
1. **대소문자 정확히 일치** — `EUI.BattleHud` ↔ `BattleHud.prefab` (`BattleHUD` X)
2. **Enum 은 카테고리별로 분리** — `EUI`, `EMonster`, `EHero` 등. 한 Enum 에 섞지 않음
3. **Enum 은 게임 코드의 `CommonEnum.cs` 에 정의** (Rule 02 §8)
4. **Addressables 주소 = 파일명** 으로 등록
5. **변경 시 Enum 과 파일명을 동시에 갱신** — 한쪽만 바뀌면 런타임 Load 실패
6. **씬 이름** 도 동일 정책

```csharp
//# (X) 하드코딩 문자열 키
CHMResource.Instance.Load<GameObject>("Slime", cb);

//# (X) Enum 값명과 파일명 불일치
public enum EMonster { Slime }  //# 파일은 slime.prefab → 로드 실패

//# (O)
CHMResource.Instance.LoadAsync<GameObject>(EMonster.Slime);
CHMUI.Instance.ShowUI(EUI.BattleHud);
```

에셋 명명 체크리스트:
- [ ] 프리팹 파일명 = Enum 값명 (대소문자 포함)
- [ ] Addressables 주소 = 파일명
- [ ] Addressables 라벨 = `CHMResource.Init(label)` 에 전달한 라벨 (기본 `"Resource"`)
- [ ] 같은 Enum 값에 두 개 이상 에셋 없음

---

## 3. UI 컴포넌트 — ChvjPackage 래퍼 우선

UI 컴포넌트는 ChvjPackage 래퍼를 우선 사용한다. Legacy `Text`, 단일 `Button`/`Toggle` 직접 사용 금지.

> **정적 라벨도 예외 없음** — 코드에서 참조하지 않는 섹션 헤더·장식 텍스트도 프리팹에 TMP_Text 가 있으면 반드시 CHText 를 함께 붙인다.

| UGUI 표준 | ChvjPackage 래퍼 |
|---|---|
| `UnityEngine.UI.Text` (Legacy) | `CHText` + `TMP_Text` |
| `Button` 단일 | `Button` + `CHButton` |
| `Toggle` 단일 | `Toggle` + `CHToggle` |
| `ScrollRect` + 수동 풀링 | `CHPoolingScrollView<TItem, TData>` |

```csharp
//# (X) Legacy Text
[SerializeField] private Text _timerText;

//# (O) CHText
[SerializeField] private CHText _timerText;
_timerText.SetText("5:00");

//# (O) CHButton + disposable
[SerializeField] private CHButton _restartButton;
_restartButton.OnClick(OnClickRestart, closeDisposable);
```

프리팹 구성 (YAML):
```
//# (X) TMP_Text 만 있고 CHText 없음
GameObject "Label": RectTransform / CanvasRenderer / TextMeshProUGUI

//# (O)
GameObject "Label": RectTransform / CanvasRenderer / TextMeshProUGUI / CHText(_stringID:-1)
```

부팅 시 hook 등록 (게임 측 1회):
```csharp
CHButton.ClickSoundHook  = () => CHMSound.Instance?.Play(EAudio.UIClick);
CHToggle.ChangeSoundHook = () => CHMSound.Instance?.Play(EAudio.UIClick);
```

예외: 에디터 전용 UI, WorldSpace 디버그 표시, 래퍼 미지원 컴포넌트(Slider 등).

---

## 4. 모든 런타임 스폰은 `CHMPool`

런타임에 GameObject 를 생성하는 모든 코드는 `CHMPool.Pop` / `Push` 로 처리한다. `Object.Instantiate` 및 `GameObject.CreatePrimitive` 직접 호출 금지.

**워크플로**:

```csharp
//# 1) 사전 워밍 (권장) — BattleController.Start 등 진입점에서
GameObject prefab = await CHMResource.Instance.LoadAsync<GameObject>(EMonster.Slime);
if (prefab != null)
{
    CHMPool.Instance.CreatePool(prefab, count: 5);
}

//# 2) Pop
CHPoolable poolable = CHMPool.Instance.Pop(prefab, parent);
poolable.gameObject.transform.position = somePosition;

//# 3) Push (사망/만료 시 Destroy 대신)
CHPoolable poolable = gameObject.GetComponent<CHPoolable>();
if (poolable != null)
{
    CHMPool.Instance.Push(poolable);
}
else
{
    Destroy(gameObject);   //# fallback
}
```

**State Reset 필수** — 풀링 대상 컴포넌트는 `OnEnable`/`OnDisable` 에서 이전 상태를 리셋한다. 재사용 시 이전 상태 누수 방지.

예외: 씬 사전 배치 정적 오브젝트, 에디터 전용 디버그 GameObject, 테스트 한정 `new GameObject("test")`.

워밍 권장 count:
| 대상 | count |
|---|---|
| 영웅 | 1 |
| 자연 스폰 몬스터 | 3 + α |
| 시각 이펙트 | 1~2 |
| 발사체 | 10~20 |

---

## 5. UIArg 는 UIBase 와 같은 파일에 정의

`UIArg` 를 상속하는 인자 클래스는 별도 파일을 만들지 않고, 해당 `UIBase` 파생 클래스 `.cs` 파일의 **상단** 에 함께 정의한다.

```csharp
//# (X) 별도 파일
//  CardSelectionPopup.cs + CardSelectionArg.cs

//# (O) 한 파일에 통합
namespace Lair.UI
{
    //# UIArg 는 UIBase 클래스 위에 정의
    public class CardSelectionArg : UIArg
    {
        public IReadOnlyList<CardData> Choices;
        public Action<CardData> OnPicked;
    }

    public class CardSelectionPopup : UIBase { ... }
}
```

예외: UIArg 가 여러 UI 에 공유되는 경우, UIArg 가 100줄 초과(→ ViewModel 후보 재설계 검토).
