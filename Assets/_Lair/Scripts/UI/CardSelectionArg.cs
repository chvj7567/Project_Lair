using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;

namespace Lair.UI
{
    //# CardSelectionPopup 의 UIArg — 카드 3장 + 선택 콜백.
    public class CardSelectionArg : UIArg
    {
        public IReadOnlyList<CardData> Choices;
        public Action<CardData> OnPicked;
    }
}
