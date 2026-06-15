using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# QuestPopup.BuildCellData — 13개 도전과제 진행 바 매핑·fill 비율·N/M 텍스트·방어 (기획서 §3.1/§3.2).
    //# TDD 본 테스트(QuestProgressTests, 5종)와 중복 금지 — 13개 풀셋 6/7 분류·fill·방어만 보강.
    public class QuestProgressEdgeTests
    {
        private MetaConfig _cfg;

        //# 기획서 §3.1 적용 결과 표 + 상위 기획서 §5.2 — 고정 도전과제 13개 (진행 바 대상 6 / 비대상 7).
        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstRun",     Condition = EAchievementCondition.TotalRuns,         Threshold = 1f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Runs10",       Condition = EAchievementCondition.TotalRuns,         Threshold = 10f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Runs30",       Condition = EAchievementCondition.TotalRuns,         Threshold = 30f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins5",        Condition = EAchievementCondition.TotalWins,         Threshold = 5f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins10",       Condition = EAchievementCondition.TotalWins,         Threshold = 10f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins25",       Condition = EAchievementCondition.TotalWins,         Threshold = 25f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins50",       Condition = EAchievementCondition.TotalWins,         Threshold = 50f });
            _cfg.Achievements.Add(new AchievementDef { Id = "FirstWin",     Condition = EAchievementCondition.FirstWin,          Threshold = 1f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win120",       Condition = EAchievementCondition.WinUnderSeconds,   Threshold = 120f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win90",        Condition = EAchievementCondition.WinUnderSeconds,   Threshold = 90f });
            _cfg.Achievements.Add(new AchievementDef { Id = "Win60",        Condition = EAchievementCondition.WinUnderSeconds,   Threshold = 60f });
            _cfg.Achievements.Add(new AchievementDef { Id = "SynergyTier2", Condition = EAchievementCondition.SynergyTierReached, Threshold = 2f });
            _cfg.Achievements.Add(new AchievementDef { Id = "SynergyTier3", Condition = EAchievementCondition.SynergyTierReached, Threshold = 3f });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        private static readonly HashSet<string> 진행대상6 = new HashSet<string>
        {
            "Runs10", "Runs30", "Wins5", "Wins10", "Wins25", "Wins50",
        };

        //# 기획서 §3.1 — 13개 중 진행 바 대상은 정확히 6개(Runs10/30·Wins5/10/25/50), 나머지 7개는 HasProgress=false.
        [Test]
        public void 도전과제_13개중_진행바_대상은_정확히_6개다()
        {
            MetaProfile p = new MetaProfile();   //# 누적값 0 — 미달성 상태
            List<QuestCellData> cells = QuestPopup.BuildCellData(p, _cfg);

            Assert.AreEqual(13, cells.Count);
            int progressCount = 0;
            //# null 항목 없음 → Achievements 순서 == cells 순서로 위치 기반 검증.
            for (int i = 0; i < _cfg.Achievements.Count; i++)
            {
                string id = _cfg.Achievements[i].Id;
                bool expected = 진행대상6.Contains(id);
                Assert.AreEqual(expected, cells[i].HasProgress, $"{id} 진행 바 여부 (기획서 §3.1)");
                if (expected)
                    progressCount++;
            }
            Assert.AreEqual(6, progressCount, "진행 바 대상은 6개 (기획서 §3.1)");
        }

        //# 기획서 §3.1 — FirstRun(TotalRuns/1)·시간형(Win120/90/60)·시너지·첫승은 비누적 취급 → 진행 바 없음.
        [Test]
        public void 비대상_7개는_누적값이_있어도_진행바가_없다()
        {
            //# 누적값을 채워도 비대상 항목엔 영향 없음.
            MetaProfile p = new MetaProfile { TotalRuns = 5, TotalWins = 3 };
            List<QuestCellData> cells = QuestPopup.BuildCellData(p, _cfg);

            string[] 비대상 = { "FirstRun", "FirstWin", "Win120", "Win90", "Win60", "SynergyTier2", "SynergyTier3" };
            foreach (string id in 비대상)
            {
                int idx = _cfg.Achievements.FindIndex(d => d.Id == id);
                Assert.IsFalse(cells[idx].HasProgress, $"{id} 는 진행 바 비대상 (기획서 §3.1)");
            }
        }

        //# 기획서 §3.2 — fill 비율 = Current/Target. 12/25 미달성은 Current=12·Target=25 (fill 0.48).
        [Test]
        public void 미달성_누적형은_현재값과_목표를_그대로_노출한다()
        {
            MetaProfile p = new MetaProfile { TotalWins = 12 };
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Wins25");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            Assert.IsTrue(cell.HasProgress);
            Assert.AreEqual(12, cell.Current);
            Assert.AreEqual(25, cell.Target);
            //# fill = 12/25 = 0.48 (UI 가 (float)Current/Target 로 산출, 기획서 §3.3)
            Assert.AreEqual(0.48f, (float)cell.Current / cell.Target, 0.0001f);
        }

        //# 기획서 §3.2 검산 — 최대 목표 Wins50 의 49/50 (달성 직전, 콤마 미진입 상한 케이스).
        [Test]
        public void 달성직전_Wins50은_49_50으로_진행한다()
        {
            MetaProfile p = new MetaProfile { TotalWins = 49 };
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Wins50");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            Assert.IsTrue(cell.HasProgress);
            Assert.AreEqual(49, cell.Current);
            Assert.AreEqual(50, cell.Target);
        }

        //# 기획서 §3.2 — 진행 0(누적값 0)도 진행 바 표시(미달성 누적형). Current=0·Target=10 → fill 0.
        [Test]
        public void 누적값_0인_진행대상은_0_목표로_진행바를_켠다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 0 };
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Runs10");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            Assert.IsTrue(cell.HasProgress);
            Assert.AreEqual(0, cell.Current);
            Assert.AreEqual(10, cell.Target);
        }

        //# 기획서 §3.2 — 세이브 변조 방어: 누적값이 목표를 넘으면 Current 는 목표로 상한 클램프(10/10).
        [Test]
        public void 목표_초과_누적값은_목표로_상한_클램프된다()
        {
            MetaProfile p = new MetaProfile { TotalRuns = 999 };   //# 미달성 플래그 가정 — 표시 클램프만
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Runs10");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            Assert.AreEqual(10, cell.Current);
            Assert.AreEqual(10, cell.Target);
        }

        //# 기획서 §3.3 상호 배타 — 달성 플래그 있으면 HasProgress=false (진행 바 ↔ 달성 뱃지 동시 노출 불가).
        [Test]
        public void 달성_플래그가_있으면_진행대상이라도_진행바를_끈다()
        {
            MetaProfile p = new MetaProfile { TotalWins = 50 };
            p.AchievedIds.Add("Wins50");
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Wins50");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            Assert.IsTrue(cell.Achieved);
            Assert.IsFalse(cell.HasProgress, "달성 셀은 진행 바 숨김 — 뱃지로 대체 (기획서 §3.3)");
        }

        //# 보상 텍스트 포맷 — "+{N} 소울" (기획서 §3 셀 표기).
        [Test]
        public void 보상_텍스트는_소울_단위로_조립된다()
        {
            _cfg.Achievements[1].RewardSouls = 50;   //# Runs10
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Runs10");
            QuestCellData cell = QuestPopup.BuildCellData(new MetaProfile(), _cfg)[idx];
            Assert.AreEqual("+50 소울", cell.RewardText);
        }

        //# 방어 — null profile / null cfg 는 예외 없이 빈 목록.
        [Test]
        public void null_인자는_예외없이_빈_목록을_반환한다()
        {
            Assert.AreEqual(0, QuestPopup.BuildCellData(null, _cfg).Count);
            Assert.AreEqual(0, QuestPopup.BuildCellData(new MetaProfile(), null).Count);
            Assert.AreEqual(0, QuestPopup.BuildCellData(null, null).Count);
        }

        //# 방어 — Achievements 가 비어 있으면 빈 목록.
        [Test]
        public void 빈_도전과제_목록은_빈_셀목록을_반환한다()
        {
            _cfg.Achievements.Clear();
            Assert.AreEqual(0, QuestPopup.BuildCellData(new MetaProfile(), _cfg).Count);
        }

        //# 방어 — Achievements 에 null 항목이 섞이면 skip (셀 개수가 그만큼 줄고 예외 없음).
        [Test]
        public void null_도전과제_항목은_건너뛴다()
        {
            _cfg.Achievements.Clear();
            _cfg.Achievements.Add(new AchievementDef { Id = "Wins5", Condition = EAchievementCondition.TotalWins, Threshold = 5f });
            _cfg.Achievements.Add(null);
            _cfg.Achievements.Add(new AchievementDef { Id = "Runs10", Condition = EAchievementCondition.TotalRuns, Threshold = 10f });

            List<QuestCellData> cells = QuestPopup.BuildCellData(new MetaProfile { TotalWins = 2, TotalRuns = 4 }, _cfg);

            Assert.AreEqual(2, cells.Count, "null 항목 skip — 유효 2개만 셀로");
            Assert.IsTrue(cells[0].HasProgress);
            Assert.AreEqual(2, cells[0].Current);
            Assert.IsTrue(cells[1].HasProgress);
            Assert.AreEqual(4, cells[1].Current);
        }

        //# 방어 — 음수 누적값(세이브 변조)은 상한 클램프만 적용(하한 없음, 기획서 §3.2 = min(raw,target)).
        //# 의도된 동작 박제 — 예외 없이 음수 그대로 노출됨을 특성화 (production 버그 아님).
        [Test]
        public void 음수_누적값은_예외없이_그대로_노출된다()
        {
            MetaProfile p = new MetaProfile { TotalWins = -5 };
            int idx = _cfg.Achievements.FindIndex(d => d.Id == "Wins25");
            QuestCellData cell = QuestPopup.BuildCellData(p, _cfg)[idx];
            //# min(-5, 25) = -5. 하한 클램프는 §3.2 명세에 없음 → 음수 그대로가 의도된 동작.
            Assert.AreEqual(-5, cell.Current);
            Assert.AreEqual(25, cell.Target);
        }
    }
}
