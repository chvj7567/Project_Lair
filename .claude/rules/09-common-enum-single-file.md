# Rule 09 — 공용 Enum 단일 파일 통합

## 룰
**여러 시스템에서 참조되는 공용 Enum** 은 게임 코드의 단일 파일 `CommonEnum.cs` 에 모아 정의한다.

## 위치
`Assets/_Project/Scripts/Data/CommonEnum.cs`

## "공용 Enum" 정의
다음 중 하나에 해당하면 `CommonEnum.cs` 로 이동:
1. **에셋 키** 역할 (Addressables 로드 키, 씬 이름 등) — `EUI`, `EMonster`, `EHero`, `EScene`, `EStats`
2. **2개 이상의 시스템/namespace** 에서 참조 — 예: `BattleResult` (UI, Battle, Controller 모두 사용)
3. **시스템 간 통신 계약** — 결과 상태, 메시지 타입 등

## 단일 시스템 내부 Enum 은 분리 유지
다음은 `CommonEnum.cs` 에 넣지 **않음**:
- 단일 시스템의 implementation detail 인 Enum
- 외부에 노출되지 않는 상태값
- 예: `class Card { private enum Phase { Idle, Playing, Discard } ... }` 같은 nested/private enum

→ 해당 시스템의 클래스 파일 안 또는 옆에 둠.

## 파일 구조 예시
```csharp
//# Assets/_Project/Scripts/Data/CommonEnum.cs
namespace Project.Data
{
    //# Rule 08 — CHMResource 로 영웅 프리팹 로드 키
    public enum EHero { Knight }

    //# Rule 08 — 몬스터 프리팹 로드 키
    public enum EMonster { Slime, Golem, Orc }

    //# Rule 08 — UI 프리팹 로드 키
    public enum EUI { BattleHud, ResultPopup }

    //# Rule 08 — 씬 로드 키
    public enum EScene { Battle }

    //# 시스템 간 결과 통신
    public enum BattleResult { None, Win, Lose }
}
```

## 추가 시점
- 새 공용 Enum 도입 시 → `CommonEnum.cs` 에 값 추가, 별도 파일 X
- 분류 주석은 `#region` 또는 한 줄 `//#` 으로 구분

## 분리해야 할 시점 (예외)
`CommonEnum.cs` 가 다음 중 하나에 해당되면 **카테고리별 파일로 분할** 검토:
- 200 줄 초과
- Enum 값이 너무 많아 정의 끝까지 스크롤이 한 화면 안 들어옴
- 카테고리가 6개 이상

분할 시 명명: `CommonEnum.Asset.cs`, `CommonEnum.Battle.cs` 등 — Rule 09 의 단일 파일 정신을 유지하기 위해 파일명 prefix `CommonEnum.` 으로 통일.

## 금지 예시
```
//# (X) 카테고리별 한 파일씩
Assets/_Project/Scripts/Data/Enums/EHero.cs
Assets/_Project/Scripts/Data/Enums/EMonster.cs
Assets/_Project/Scripts/Data/Enums/EUI.cs
Assets/_Project/Scripts/Data/Enums/EScene.cs
```

```csharp
//# (X) 시스템 코드 안에 공용 Enum 정의
namespace Project.Battle
{
    public class BattleStateModel
    {
        public enum BattleResult { ... }  //# 공용인데 시스템 내부에 숨김
    }
}
```

## 권장 예시
```csharp
//# (O) CommonEnum.cs 단일 파일에 모두 정의
namespace Project.Data
{
    public enum EHero { Knight }
    public enum EMonster { Slime, Golem, Orc }
    public enum BattleResult { None, Win, Lose }
}
```

## Rule 08 과의 관계
- Rule 08 #2(카테고리별 Enum 분리) — *Enum 자체*는 카테고리별로 나눈다 (`EUI`와 `EMonster` 를 하나에 섞지 않음)
- Rule 09 — *파일*은 하나로 모은다
- 즉 **한 파일에 여러 Enum 정의** 가 정답
