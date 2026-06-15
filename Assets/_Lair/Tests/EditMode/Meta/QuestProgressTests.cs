using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# QuestPopup.BuildCellData — 누적형 도전과제 진행 필드 산출 검증 (기획서 §3.1/§3.2).
    public class QuestProgressTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins25", Condition = EAchievementCondition.TotalWins, Threshold = 25f, DisplayName = "영웅 학살자", RewardSouls = 100 });
            _cfg.Achievements.Add(new AchievementDef { Id = "Runs10", Condition = EAchievementCondition.TotalRuns, Threshold = 10f, DisplayName = "성실한 영주", RewardSouls = 50 });
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin", Condition = EAchievementCondition.FirstWin, Threshold = 1f, DisplayName = "첫 사냥감", RewardSouls = 30 });
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstRun", Condition = EAchievementCondition.TotalRuns, Threshold = 1f, DisplayName = "첫 출격", RewardSouls = 20 });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        private QuestCellData 셀(MetaProfile p, string id)
        {
            List<QuestCellData> cells = QuestPopup.BuildCellData(p, _cfg);
            for (int i = 0; i < _cfg.Achievements.Count; i++)
            {
                if (_cfg.Achievements[i].Id == id)
                    return cells[i];
            }
            return null;
        }

        [Test]
        public void 누적형_미달성은_현재값과_목표를_노출한다()
        {
            MetaProfile p = new MetaProfile { TotalWins = 12 };
            QuestCellData cell = 셀(p, "Wins25");
            Assert.IsTrue(cell.HasProgress);
            Assert.AreEqual(12, cell.Current);
            Assert.AreEqual(25, cell.Target);
        }

        [Test]
        public void 현재값은_목표를_넘지_않도록_클램프된다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 30 };   //# 미달성 가정(플래그 미보유) — 표시 클램프만 검증
            QuestCellData cell = 셀(p, "Runs10");
            Assert.AreEqual(10, cell.Current);
            Assert.AreEqual(10, cell.Target);
        }

        [Test]
        public void 이미_달성한_누적형은_진행도를_끈다()
        {
            MetaProfile p = new MetaProfile { TotalWins = 30 };
            p.AchievedIds.Add("Wins25");
            Assert.IsFalse(셀(p, "Wins25").HasProgress);
        }

        [Test]
        public void 비누적형_조건은_진행도가_없다()
        {
            Assert.IsFalse(셀(new MetaProfile(), "FirstWin").HasProgress);
        }

        [Test]
        public void 임계가_1인_누적형은_진행도가_없다()
        {
            //# 기획서 §3.1 — FirstRun(TotalRuns/1) carve-out. 1/1 진행 바는 노이즈라 비대상.
            Assert.IsFalse(셀(new MetaProfile(), "FirstRun").HasProgress);
        }
    }
}
