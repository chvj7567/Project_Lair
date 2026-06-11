using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# ResultPopup.BuildRewardText — 기획서 §9.1/§9.2 줄 포맷 회귀 박제 (생략 키 = LordLevel == 0, 부분 생략, 3줄 상한).
    public class ResultPopupRewardTextTests
    {
        private static AchievementDef 과제(string name, int souls)
            => new AchievementDef { Id = name, DisplayName = name, RewardSouls = souls };

        [Test]
        public void 레벨업_없으면_보상_줄만_표기된다()
        {
            ResultPopupArg arg = new ResultPopupArg { SoulsGained = 212, XpGained = 100, LordLevel = 0 };
            Assert.AreEqual("보상  소울 +212 · XP +100", ResultPopup.BuildRewardText(arg));
        }

        [Test]
        public void 패배_0값도_그대로_표기된다()
        {
            //# 기획서 §9.2 — 패배 페널티의 정직한 노출 + XP 전진 확인.
            ResultPopupArg arg = new ResultPopupArg { SoulsGained = 0, XpGained = 40, LordLevel = 0 };
            Assert.AreEqual("보상  소울 +0 · XP +40", ResultPopup.BuildRewardText(arg));
        }

        [Test]
        public void 레벨업_보상이_있으면_영주_줄이_추가된다()
        {
            ResultPopupArg arg = new ResultPopupArg { SoulsGained = 212, XpGained = 100, LordLevel = 2, LordRewardSouls = 50 };
            Assert.AreEqual("보상  소울 +212 · XP +100\n영주 레벨 업!  Lv 2  +50 소울", ResultPopup.BuildRewardText(arg));
        }

        [Test]
        public void 잠금더미_레벨만_오르면_소울_부분만_생략된다()
        {
            //# 기획서 §9.2 — LordRewardSouls == 0 은 줄 생략 키가 아님. "+N 소울" 부분만 생략.
            ResultPopupArg arg = new ResultPopupArg { SoulsGained = 100, XpGained = 100, LordLevel = 4, LordRewardSouls = 0 };
            Assert.AreEqual("보상  소울 +100 · XP +100\n영주 레벨 업!  Lv 4", ResultPopup.BuildRewardText(arg));
        }

        [Test]
        public void 도전과제는_최대_3줄이고_초과분은_외N건으로_접힌다()
        {
            ResultPopupArg arg = new ResultPopupArg
            {
                SoulsGained = 220,
                XpGained = 100,
                LordLevel = 0,
                NewlyAchieved = new List<AchievementDef> { 과제("던전 개장", 20), 과제("첫 사냥감", 30), 과제("신속한 처형", 40), 과제("전광석화", 60), 과제("찰나의 죽음", 100) },
            };

            string text = ResultPopup.BuildRewardText(arg);
            string[] lines = text.Split('\n');

            Assert.AreEqual(5, lines.Length);   //# 보상 1 + 과제 3 + 외N건 1.
            Assert.AreEqual("도전과제 달성!  던전 개장  +20 소울", lines[1]);
            Assert.AreEqual("도전과제 달성!  신속한 처형  +40 소울", lines[3]);
            Assert.AreEqual("외 2건 달성", lines[4]);
        }

        [Test]
        public void 도전과제_정확히_3건이면_외N건_줄이_없다()
        {
            ResultPopupArg arg = new ResultPopupArg
            {
                NewlyAchieved = new List<AchievementDef> { 과제("a", 1), 과제("b", 2), 과제("c", 3) },
            };
            StringAssert.DoesNotContain("외 ", ResultPopup.BuildRewardText(arg));
        }

        [Test]
        public void null이_섞인_도전과제_목록도_건수를_정확히_센다()
        {
            ResultPopupArg arg = new ResultPopupArg
            {
                NewlyAchieved = new List<AchievementDef> { 과제("a", 1), null, 과제("b", 2), 과제("c", 3), null, 과제("d", 4) },
            };

            string text = ResultPopup.BuildRewardText(arg);
            Assert.IsTrue(text.Contains("외 1건 달성"), $"null 제외 4건 중 3줄 + 외 1건이어야 함: {text}");
        }
    }
}
