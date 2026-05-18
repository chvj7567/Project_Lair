# Rule 05 — MVVM 패턴 적용

## 룰
UI/도메인 로직은 **MVVM 패턴**으로 구현한다.

## 계층 정의
| 계층 | 역할 | Unity 매핑 |
|------|------|------------|
| **Model** | 순수 데이터/상태 + 비즈니스 규칙. Unity 의존 금지 | POCO, ScriptableObject |
| **ViewModel** | 모델 상태를 뷰가 쓰기 좋게 가공, 명령(Command) 노출 | C# 클래스 (MonoBehaviour 비권장) |
| **View** | 표시 + 입력 수신만 담당. 로직 금지 | MonoBehaviour + UGUI/UI Toolkit |

## 규칙
- View → ViewModel **단방향 바인딩**, ViewModel → View 는 이벤트/Observable 로 통지
- View는 Model을 직접 참조하지 않는다
- ViewModel은 View를 모른다 (테스트 가능)
- 상태 변경은 ViewModel을 통해서만 발생

## 예시
```csharp
//# Model — 순수 데이터
public class PlayerModel
{
    public int Hp { get; set; }
    public int Gold { get; set; }
}

//# ViewModel — Model 가공 + Command
public class PlayerViewModel
{
    public event Action<int> OnHpChanged;
    public event Action<int> OnGoldChanged;
    public ICommand UseItemCommand { get; }
    //# ...
}

//# View — 바인딩/입력만
public class PlayerHud : MonoBehaviour
{
    [SerializeField] private Text _hpText;
    private PlayerViewModel _vm;

    public void Bind(PlayerViewModel vm)
    {
        _vm = vm;
        _vm.OnHpChanged += hp => _hpText.text = hp.ToString();
    }
}
```

## 참고
- 가벼운 화면은 MVP/MVC도 허용하나, 기본은 MVVM
- 바인딩 라이브러리 사용 가능 (UniRx, R3 등)
