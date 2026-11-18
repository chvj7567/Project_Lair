using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using UnityEditor;

namespace Lair.EditorTools
{
    //# Play 중 결과창(ResultPopup) 미리보기 진입점 — 표시용 Arg 만 만들며 MetaProfile 은 건드리지 않는다. 인자 없음.
    public static partial class PlayDriver
    {
        public static string ShowResultWinPreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            CHMUI.Instance.ShowUI(EUI.ResultPopup, new ResultPopupArg
            {
                Result = BattleResult.Win,
                HasMeta = true,
                SoulsGained = 180,
                XpGained = 60,
                ClearTime = 222.1f,
                IsNewBest = true,
                Difficulty = 2,
                CurrencyGains = new List<CurrencyGain>
                {
                    new CurrencyGain { Currency = ECurrency.AwakenStone, Amount = 25 },
                },
                HasRelicDrop = true,
                RelicId = ERelic.HeartOfStone.ToString(),
                RelicStackAfter = 1,
                RelicMaxStack = 5,
                RelicIsNew = true,
            });
            return "OK result win";
        }

        public static string ShowResultLosePreview()
        {
            if (EditorApplication.isPlaying == false)
                return NotPlaying;

            CHMUI.Instance.ShowUI(EUI.ResultPopup, new ResultPopupArg
            {
                Result = BattleResult.Lose,
                HasMeta = true,
                SoulsGained = 40,
                XpGained = 15,
                ClearTime = 150f,
                HeroHpRatio = 0.18f,
                Difficulty = 1,
                HasRelicDrop = false,
            });
            return "OK result lose";
        }
    }
}
