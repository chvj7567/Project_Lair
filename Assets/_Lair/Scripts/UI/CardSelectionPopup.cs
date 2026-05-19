using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;

namespace Lair.UI
{
    //# CHMUI 로 띄워지는 카드 선택 팝업. 3장 표시 → 1장 선택 → OnPicked → Close.
    public class CardSelectionPopup : UIBase
    {
        [SerializeField] private CardView[] _slots = new CardView[3];

        public override void InitUI(UIArg arg)
        {
            if (arg is not CardSelectionArg sa) return;

            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i] == null) continue;

                if (i < sa.Choices.Count)
                {
                    var card = sa.Choices[i];
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].Bind(card, () =>
                    {
                        sa.OnPicked?.Invoke(card);
                        //# reuse=false — 매번 새 인스턴스로 띄워 CHButton listener 누적 방지
                        Close(reuse: false);
                    });
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
