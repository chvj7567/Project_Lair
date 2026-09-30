using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;

namespace Lair.UI
{
    //# Rule 13 — UIArg 는 페어 UIBase 와 같은 파일.
    public class CardSelectionArg : UIArg
    {
        public IReadOnlyList<CardData> Choices;
        public Action<CardData> OnPicked;
        //# 3픽 캡 배지 — 카드별 현재 픽 누적수 공급 (null 이면 0 처리).
        public Func<CardData, int> PickCountOf;
        //# 종류·트리거 근거 — 부제("패시브 · 영웅 HP 60% 도달" / "액티브 · N초 주기")와 카드 종류 색의 입력.
        public bool IsPassive = true;
        public int TriggerHpPercent;        //# 패시브: 도달한 HP 구간 %(예: 60)
        public int ActivePeriodSeconds;     //# 액티브: 트리거 주기(초)
    }

    //# CHMUI 로 띄워지는 카드 선택 팝업. 3장 표시 → 1장 선택 → OnPicked → Close.
    public class CardSelectionPopup : UIBase
    {
        [SerializeField] private CardView[] _slots = new CardView[3];
        [SerializeField] private CHText _subtitleText;   //# 제목 아래 부제 — 종류 + 트리거 근거

        //# 부제 문구 — 패시브는 도달한 HP 구간, 액티브는 트리거 주기.
        public static string BuildSubtitle(bool isPassive, int triggerHpPercent, int activePeriodSeconds)
        {
            if (isPassive)
                return $"패시브 · 영웅 HP {triggerHpPercent}% 도달";
            return $"액티브 · {activePeriodSeconds}초 주기";
        }

        public override void InitUI(UIArg arg)
        {
            if (arg is not CardSelectionArg sa) return;

            //# 카드 선택창이 뜨는 순간 1회 — InitUI 호출 시점에 재생.
            CHMSound.Instance?.Play(EAudio.CardSelect);

            if (_subtitleText != null)
            {
                _subtitleText.SetText(BuildSubtitle(sa.IsPassive, sa.TriggerHpPercent, sa.ActivePeriodSeconds));
            }

            for (int i = 0; i < _slots.Length; ++i)
            {
                if (_slots[i] == null) continue;

                if (i < sa.Choices.Count)
                {
                    CardData card = sa.Choices[i];
                    _slots[i].gameObject.SetActive(true);
                    _slots[i].SetKind(sa.IsPassive);
                    int pickCount = sa.PickCountOf != null ? sa.PickCountOf(card) : 0;
                    _slots[i].Bind(card, () =>
                    {
                        sa.OnPicked?.Invoke(card);
                        //# reuse=false — 매번 새 인스턴스로 띄워 CHButton listener 누적 방지
                        Close(reuse: false);
                    }, pickCount);
                }
                else
                {
                    _slots[i].gameObject.SetActive(false);
                }
            }
        }
    }
}
