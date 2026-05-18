# Rule 06 — 상위 스크립트는 인터페이스로 사용

## 룰
하이어라키 구조 상 **상위 GameObject의 스크립트 기능**을 사용해야 할 경우, 구체 클래스 참조 대신 **인터페이스**를 통해 접근한다.

## 적용 시점
- 자식이 `GetComponentInParent<T>()` 를 호출할 때
- 상위 컨테이너의 동작(이벤트 알림, 콜백)을 자식이 트리거해야 할 때
- 자식 → 부모 방향의 참조가 필요한 모든 경우

## 동작 방식
1. 부모가 제공할 기능을 인터페이스로 선언
2. 부모 스크립트가 인터페이스를 구현
3. 자식은 인터페이스로만 부모를 가져옴

## 예시
```csharp
//# 1. 인터페이스 선언
public interface IInventoryHost
{
    void NotifyItemPicked(ItemId id);
}

//# 2. 부모 구현
public class InventoryPanel : MonoBehaviour, IInventoryHost
{
    public void NotifyItemPicked(ItemId id) { /* ... */ }
}

//# 3. 자식 — 인터페이스로 참조
public class ItemSlot : MonoBehaviour
{
    private IInventoryHost _host;

    void Awake()
    {
        //# 구체 클래스 InventoryPanel을 모름
        _host = GetComponentInParent<IInventoryHost>();
    }

    public void OnClick() => _host.NotifyItemPicked(_id);
}
```

## 금지
```csharp
//# Bad — 부모 구체 클래스 직접 참조
var panel = GetComponentInParent<InventoryPanel>();
panel.NotifyItemPicked(id);
```

## 이유
- 부모 교체/리팩터링 시 자식 영향 최소화
- 테스트 시 부모 모킹 용이
- Rule 03 (종속성 최소화) 와 동일 맥락
