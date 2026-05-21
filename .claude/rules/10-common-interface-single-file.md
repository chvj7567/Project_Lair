# Rule 10 — 공용 Interface 단일 파일 통합

## 룰
**여러 시스템에서 참조되는 공용 Interface** 는 단일 파일 `CommonInterface.cs` 에 모아 정의한다.

## 위치
시스템 namespace 별로 한 파일:
- `Assets/_Project/Scripts/Character/CommonInterface.cs` — `Project.Character` 공용 인터페이스 (IMover, IHealth, IAttacker, ITargetProvider 등)
- 향후 다른 도메인이 생기면 그 namespace 하위에 동일 패턴 (`Battle/CommonInterface.cs` 등)

## "공용 Interface" 정의
다음 중 하나에 해당하면 `CommonInterface.cs` 로 이동:
1. **컴포지션 계약** — 동일 도메인의 여러 구현체가 implement (IMover ↔ SimpleMover/NavMover, IHealth ↔ Health/FakeHealth)
2. **2개 이상 시스템/namespace** 에서 참조 — Production + Tests + Editor 등
3. **MonoBehaviour 컴포넌트 계약** — `GetComponent<IFoo>()` 패턴

## 단일 시스템 내부 Interface 는 분리 유지
다음은 `CommonInterface.cs` 에 넣지 **않음**:
- 단일 구현체만 있는 internal 추상화 (yagni)
- 단일 시스템의 구현 디테일 — 시스템 내부에서만 사용
- Adapter / Bridge 등 패턴 특화 interface — 해당 패턴 파일 옆에 둠

→ 해당 시스템 클래스 파일 안 또는 옆에 둠.

## 파일 구조 예시
```csharp
//# Assets/_Project/Scripts/Character/CommonInterface.cs
using System;
using UnityEngine;

namespace Project.Character
{
    //# 위치 이동 추상 — SimpleMover, (향후) NavMover 가 구현
    public interface IMover
    {
        float Speed { get; set; }
        void MoveTo(Vector3 target);
        void Stop();
    }

    //# HP 추상 — Health (MB), FakeHealth (테스트 더블) 가 구현
    public interface IHealth
    {
        int Max { get; }
        int Current { get; }
        float Ratio { get; }
        bool IsAlive { get; }
        event Action<int, int> OnChanged;
        event Action OnDied;
        void TakeDamage(int amount);
        void SetMax(int max, bool resetCurrent = true);
    }

    //# 공격 추상
    public interface IAttacker { ... }

    //# 타겟 탐색 전략
    public interface ITargetProvider { ... }
}
```

## 추가 시점
- 새 공용 Interface 도입 시 → `CommonInterface.cs` 에 추가, 별도 파일 X
- 카테고리 주석 `//# ===== ... =====` 으로 그룹화

## 분리해야 할 시점 (예외)
`CommonInterface.cs` 가 다음 중 하나에 해당되면 분할 검토:
- 200 줄 초과
- Interface 종류가 6개 이상이거나 카테고리가 명확히 다름
- 한 인터페이스가 자체 partial 파일이 필요할 정도로 복잡 (default impl, helper extensions 등)

분할 시 명명: `CommonInterface.Movement.cs`, `CommonInterface.Combat.cs` 등 — 단일 파일 정신을 유지하기 위해 파일명 prefix `CommonInterface.` 으로 통일.

## 금지 예시
```
//# (X) 인터페이스마다 한 파일씩
Assets/_Project/Scripts/Character/IMover.cs
Assets/_Project/Scripts/Character/IHealth.cs
Assets/_Project/Scripts/Character/IAttacker.cs
Assets/_Project/Scripts/Character/ITargetProvider.cs
```

```csharp
//# (X) 공용 인터페이스를 구현 파일 안에 묶기
public class Health : MonoBehaviour
{
    public interface IHealth { ... }   //# 다른 시스템도 쓰는데 내부 nested
}
```

## 권장 예시
```csharp
//# (O) CommonInterface.cs 단일 파일에 모두 정의
namespace Project.Character
{
    public interface IMover { ... }
    public interface IHealth { ... }
    public interface IAttacker { ... }
    public interface ITargetProvider { ... }
}
```

## Rule 09 와의 관계
- Rule 09 — 공용 **Enum** 단일 파일 (`CommonEnum.cs`)
- Rule 10 — 공용 **Interface** 단일 파일 (`CommonInterface.cs`)
- 동일 원칙: 카테고리별 분리 X, 도메인별 단일 파일에 모음
