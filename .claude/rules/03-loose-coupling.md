# Rule 03 — 클래스 종속성 최소화

## 룰
클래스는 다른 클래스와의 종속성을 **최대한 배제**하고 작성한다.

## 가이드
- 구체 클래스 직접 참조 대신 **인터페이스/추상화**에 의존 (DIP)
- 정적 싱글톤 호출 최소화 — 필요한 의존성은 생성자/메서드 인자로 주입
- 의존이 늘어나면 책임 분리(SRP) 또는 이벤트 기반(pub/sub) 으로 분해
- 양방향 참조 금지 — 단방향 데이터/이벤트 흐름 유지

## 체크리스트
- [ ] 이 클래스가 알아야 하는 외부 타입은 몇 개인가? (이상적: 0~3)
- [ ] `FindObjectOfType`, `GameObject.Find` 사용을 피했는가?
- [ ] 매니저 직접 참조 대신 인터페이스/이벤트로 분리 가능한가?
- [ ] 테스트할 때 모킹이 가능한 구조인가?

## 예시
```csharp
//# Bad — 구체 매니저 직접 참조
public class Player : MonoBehaviour
{
    void Hit() => AudioManager.Instance.Play("hit");
}

//# Good — 인터페이스 주입
public class Player : MonoBehaviour
{
    private IAudioService _audio;
    public void Inject(IAudioService audio) => _audio = audio;
    void Hit() => _audio.Play("hit");
}
```
