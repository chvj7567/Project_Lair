# Rule 02 — C# 코딩 스타일

> 구 Rule 02(주석), 03(종속성), 05(MVVM), 06(인터페이스), 09(Enum 파일), 10(Interface 파일), 15(스타일) 통합.

---

## 1. 주석 접두어 `//#`

모든 단일 라인 주석은 `//#` 으로 시작한다.

```csharp
//# (O) 올바른 주석
//# DontDestroyOnLoad는 Root에서만 호출
if (transform.parent != null)
    return;

// (X) 일반 주석 금지
/* (X) 블록 주석 금지 */
```

- XML doc 주석 `///` 은 예외 (IDE 인텔리센스용)
- `#region` 등 컴파일러 디렉티브는 그대로
- 기존 주석은 해당 줄 수정 시 함께 변환

---

## 2. 가드 절 — 중괄호 없이 개행

`null` 체크·조건 체크로 즉시 리턴하는 가드 절은 중괄호 없이 개행+들여쓰기로 작성한다. 가드 절이 아닌 모든 분기는 한 줄이어도 중괄호 필수.

```csharp
//# (O) 가드 절
if (_vm == null)
    return;

//# (X) 가드 절에 중괄호
if (_vm == null) { return; }

//# (O) 일반 분기 — 한 줄이어도 중괄호
if (isAlive == false)
{
    HandleDeath();
}

//# (X) 일반 분기 중괄호 없음
if (isAlive == false)
    HandleDeath();
```

---

## 3. `var` 금지 — 명시적 타입 표기

로컬 변수 선언 시 `var` 대신 명시적 타입을 쓴다.

```csharp
//# (X)
var poolable = CHMPool.Instance.Pop(prefab, parent);
var cards = new List<CardData>();

//# (O)
CHPoolable poolable = CHMPool.Instance.Pop(prefab, parent);
List<CardData> cards = new List<CardData>();
```

---

## 4. `!` 부정 연산자 금지

`!` 대신 `== false` 또는 `== null` 을 사용한다. `!=` 는 허용.

```csharp
//# (X)
if (!isAlive) ...
if (!_spawner) ...

//# (O)
if (isAlive == false) ...
if (_spawner == null) ...
```

---

## 5. 클래스 종속성 최소화

클래스는 다른 클래스와의 종속성을 최대한 배제하고 작성한다.

- 구체 클래스 직접 참조 대신 **인터페이스/추상화**에 의존 (DIP)
- 정적 싱글톤 호출 최소화 — 의존성은 생성자/메서드 인자로 주입
- 의존이 늘어나면 책임 분리(SRP) 또는 이벤트 기반(pub/sub) 으로 분해
- 양방향 참조 금지 — 단방향 데이터/이벤트 흐름 유지
- `FindObjectOfType`, `GameObject.Find` 사용 금지

```csharp
//# (X) 구체 매니저 직접 참조
public class Player : MonoBehaviour
{
    void Hit() => AudioManager.Instance.Play("hit");
}

//# (O) 인터페이스 주입
public class Player : MonoBehaviour
{
    private IAudioService _audio;
    public void Inject(IAudioService audio) => _audio = audio;
    void Hit() => _audio.Play("hit");
}
```

체크리스트:
- [ ] 알아야 하는 외부 타입 0~3개 이내인가?
- [ ] 매니저 직접 참조 대신 인터페이스/이벤트로 분리 가능한가?
- [ ] 테스트 시 모킹이 가능한 구조인가?

---

## 6. MVVM 패턴

UI/도메인 로직은 MVVM 패턴으로 구현한다.

| 계층 | 역할 | Unity 매핑 |
|---|---|---|
| **Model** | 순수 데이터/상태 + 비즈니스 규칙. Unity 의존 금지 | POCO, ScriptableObject |
| **ViewModel** | 모델 상태를 뷰가 쓰기 좋게 가공, Command 노출 | C# 클래스 (MonoBehaviour 비권장) |
| **View** | 표시 + 입력 수신만. 로직 금지 | MonoBehaviour + UGUI |

- View → ViewModel 단방향 바인딩, ViewModel → View 는 이벤트/Observable 로 통지
- View 는 Model 을 직접 참조하지 않는다
- ViewModel 은 View 를 모른다 (테스트 가능)
- 상태 변경은 ViewModel 을 통해서만 발생

```csharp
//# ViewModel — Model 가공 + Command
public class PlayerViewModel
{
    public event Action<int> OnHpChanged;
    public ICommand UseItemCommand { get; }
}

//# View — 바인딩/입력만
public class PlayerHud : MonoBehaviour
{
    private PlayerViewModel _vm;
    public void Bind(PlayerViewModel vm)
    {
        _vm = vm;
        _vm.OnHpChanged += hp => _hpText.SetText(hp.ToString());
    }
}
```

---

## 7. 상위 스크립트는 인터페이스로 참조

하이어라키 구조 상 상위 GameObject 의 스크립트 기능을 사용해야 할 경우 구체 클래스 참조 대신 인터페이스를 통해 접근한다.

```csharp
//# (X) 부모 구체 클래스 직접 참조
InventoryPanel panel = GetComponentInParent<InventoryPanel>();
panel.NotifyItemPicked(id);

//# (O) 인터페이스로 참조
public interface IInventoryHost { void NotifyItemPicked(ItemId id); }

public class ItemSlot : MonoBehaviour
{
    private IInventoryHost _host;
    void Awake() => _host = GetComponentInParent<IInventoryHost>();
    public void OnClick() => _host.NotifyItemPicked(_id);
}
```

---

## 8. 공용 Enum — `CommonEnum.cs` 단일 파일

여러 시스템에서 참조되는 공용 Enum 은 `CommonEnum.cs` 에 모아 정의한다.

**공용 Enum 기준** (하나라도 해당하면 CommonEnum.cs):
1. 에셋 키 역할 — `EUI`, `EMonster`, `EHero`, `EScene`, `EStats`
2. 2개 이상 시스템/namespace 에서 참조
3. 시스템 간 통신 계약

단일 시스템 내부 Enum(private·nested·impl detail) 은 해당 파일에 둔다.

```csharp
//# (X) 카테고리별 한 파일씩
//  Assets/_Lair/Scripts/Data/EHero.cs, EMonster.cs, EUI.cs ...

//# (O) 한 파일에 카테고리별 Enum 정의
namespace Lair.Data
{
    public enum EHero   { Knight }
    public enum EMonster { Slime, Golem, Orc }
    public enum EUI     { BattleHud, ResultPopup }
    public enum EScene  { Battle }
    public enum BattleResult { None, Win, Lose }
}
```

파일이 200줄 초과 or Enum 카테고리 6개 이상이면 `CommonEnum.Asset.cs`, `CommonEnum.Battle.cs` 등 prefix 통일로 분할.

---

## 9. 공용 Interface — `CommonInterface.cs` 단일 파일

여러 시스템에서 참조되는 공용 Interface 는 도메인 namespace 별 `CommonInterface.cs` 에 모아 정의한다.

**공용 Interface 기준**:
1. 동일 도메인의 여러 구현체가 implement
2. 2개 이상 시스템/namespace 에서 참조
3. `GetComponent<IFoo>()` 패턴

단일 구현체만 있는 internal 추상화는 해당 파일 옆에 둔다.

```csharp
//# (X) 인터페이스마다 한 파일씩
//  IMover.cs, IHealth.cs, IAttacker.cs ...

//# (O) 도메인 단일 파일
namespace Lair.Character
{
    public interface IMover   { void MoveTo(Vector3 target); void Stop(); }
    public interface IHealth  { int Current { get; } bool IsAlive { get; } void TakeDamage(int amount); }
    public interface IAttacker { ... }
}
```

파일이 200줄 초과 or 카테고리 6개 이상이면 `CommonInterface.Movement.cs` 등 prefix 통일로 분할.

---

## 적용 범위

- 신규 작성 코드 전체
- 기존 코드는 해당 줄 수정 시 함께 변환

## 예외

§1~4(문법 스타일): 예외 없음.
§5~9(설계 원칙): 가벼운 화면은 MVP/MVC 허용(§6), 단일 구현체 internal 인터페이스 분리 유지(§9).
