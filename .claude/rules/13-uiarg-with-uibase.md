# Rule 13 — UIArg 파생 클래스는 같은 .cs 파일에 정의

## 룰
`UIArg` 를 상속하는 인자 클래스(예: `BattleHudArg`, `ResultPopupArg`, `CardSelectionArg`)는
**별도 파일을 만들지 않고**, 해당 `UIBase` 파생 클래스가 있는 `.cs` 파일의 **상단**에 함께 정의한다.

## 이유
- UIArg 는 한 UIBase 와 1:1 페어. 함께 봐야 흐름 이해 빠름
- 파일 개수 절반 감소 — 폴더 가독성 향상
- 변경 영향 범위가 명확 — 같은 파일 안에서 인자 ↔ View 동시 수정

## 파일 구조 예시
```csharp
//# Assets/_Lair/Scripts/UI/CardSelectionPopup.cs

using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;

namespace Lair.UI
{
    //# UIArg 는 같은 파일 안에서 UIBase 클래스 위에 정의 (Rule 13).
    public class CardSelectionArg : UIArg
    {
        public IReadOnlyList<CardData> Choices;
        public Action<CardData> OnPicked;
    }

    public class CardSelectionPopup : UIBase
    {
        // ...
    }
}
```

## 적용 대상 (Slice B1 현재)
- `BattleHud.cs` ← `BattleHudArg` 통합 (기존 `BattleHudArg.cs` 삭제)
- `ResultPopup.cs` ← `ResultPopupArg` 통합 (기존 `ResultPopupArg.cs` 삭제)
- `CardSelectionPopup.cs` ← `CardSelectionArg` 통합 (기존 `CardSelectionArg.cs` 삭제)

## 추가 시점
새 `UIBase` 파생 클래스 작성 시 **같은 파일 안에 UIArg 도 정의**. 별도 `XxxArg.cs` 파일 생성 금지.

## 예외
다음 경우 별도 파일 허용:
- **UIArg 가 다른 UI 와 공유** 되는 경우 (예: 여러 팝업이 같은 인자 구조 사용 — 드물지만 가능)
- **UIArg 가 매우 큼** (100+ 줄) — 단 그 정도면 Arg 가 아니라 ViewModel 후보로 재설계

## 금지 예시
```
//# (X) 별도 파일
Assets/_Lair/Scripts/UI/CardSelectionPopup.cs
Assets/_Lair/Scripts/UI/CardSelectionArg.cs    ← 별도 파일 금지
```

## 권장 예시
```
//# (O) 한 파일에 통합
Assets/_Lair/Scripts/UI/CardSelectionPopup.cs    ← CardSelectionArg + CardSelectionPopup 함께
```

## Rule 09/10 과의 관계
- Rule 09: 공용 Enum 단일 파일 (CommonEnum.cs)
- Rule 10: 공용 Interface 단일 파일 (CommonInterface.cs)
- Rule 13: UIArg 는 페어인 UIBase 파일에 통합
- 공통 원칙: **관련된 코드는 같이 둠** — 파편화 방지
