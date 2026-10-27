using System.Collections.Generic;
using System.IO;
using System.Linq;
using ChvjUnityInfra;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 슬라이스 B 핵심 로직 자체 테스트 — FormationViewModel (코스트 합·토스트·봉인 예고·저장 행). 엣지 망라는 test-engineer.
    public class FormationViewModelTests
    {
        private static FormationDatabase ShippedDb()
        {
            string dir = WorldMapTestSupport.JsonDir;
            return FormationDatabase.Build(
                JsonUtility.FromJson<FormationConfigRow>(File.ReadAllText(dir + "FormationConfig.json")),
                JsonArrayUtility.FromJsonArray<FormationMonsterRow>(File.ReadAllText(dir + "FormationMonster.json")),
                JsonArrayUtility.FromJsonArray<FormationSlotRow>(File.ReadAllText(dir + "FormationSlot.json")),
                JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(File.ReadAllText(dir + "Chapter_FormationHint.json")));
        }

        private static FormationViewModel Build(int stage, int cleared, List<FormationSlotEntry> saved = null)
        {
            MetaProfile profile = new MetaProfile { ClearedStage = cleared, Formation = saved ?? new List<FormationSlotEntry>() };
            return new FormationViewModel(ShippedDb(), WorldMapTestSupport.FullDb(), profile, stage, WorldMapTestSupport.Text, monster => 0);
        }

        private static List<FormationSlotEntry> Rows(params EMonster[] monsters)
        {
            List<FormationSlotEntry> rows = new List<FormationSlotEntry>();
            for (int i = 0; i < monsters.Length; ++i)
            {
                rows.Add(new FormationSlotEntry { Slot = i + 1, Monster = monsters[i].ToString() });
            }
            return rows;
        }

        [Test]
        public void 기본_편성은_코스트_13_13이고_배치하면_합과_선택이_갱신된다()
        {
            FormationViewModel vm = Build(1, 1);
            Assert.AreEqual(13, vm.CostSum);
            Assert.IsTrue(vm.CanSortie);
            Assert.AreEqual(0, vm.BuildSaveRows().Count, "기본 편성은 0행");

            Assert.IsNull(vm.Place(EMonster.Plague), "도깨비불(3) -> 역병귀(1)");
            Assert.AreEqual(11, vm.CostSum);
            Assert.AreEqual(1, vm.SelectedIndex, "배치 후 다음 둥지로 이동");
            Assert.AreEqual(6, vm.BuildSaveRows().Count, "기본과 다르면 6행");
        }

        [Test]
        public void 코스트_부족과_종당_최대는_토스트_문구를_돌려주고_상태를_바꾸지_않는다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.SelectNest(5);
            string cost = vm.Place(EMonster.Reaper);
            Assert.IsNotNull(cost);
            StringAssert.Contains("필요 3", cost);
            StringAssert.Contains("남은 코스트 1", cost);
            Assert.AreEqual(13, vm.CostSum);

            vm.SelectNest(0);
            vm.Place(EMonster.Plague);
            vm.Place(EMonster.Plague);
            string species = vm.Place(EMonster.Plague);
            StringAssert.Contains("역병귀", species);
            StringAssert.Contains("최대 2곳", species);
        }

        [Test]
        public void 상한을_넘긴_저장_편성은_무효로_표시되고_출격이_막힌다()
        {
            FormationViewModel vm = Build(1, 1, Rows(EMonster.Reaper, EMonster.Reaper, EMonster.Wisp, EMonster.Wraith, EMonster.Hex, EMonster.Phantom));
            Assert.IsTrue(vm.IsInvalid);
            Assert.IsFalse(vm.TrySortie(out string toast));
            StringAssert.Contains("코스트를 초과한 편성", toast);
            vm.ResetToDefault();
            Assert.IsTrue(vm.TrySortie(out _));
        }

        [Test]
        public void 봉인_예고는_봉인_종_최소_슬롯_없으면_코스트_최고_둥지를_가리킨다()
        {
            FormationViewModel vm = Build(4, 4);
            Assert.AreEqual(3, vm.SealedIndex, "1-4 기본 편성은 망령(슬롯 4) - 현행과 동일");
            StringAssert.Contains("둥지 4", vm.SealForecastText);
            StringAssert.Contains("망령", vm.SealForecastText);

            vm.SelectNest(3);
            vm.Place(EMonster.Hex);
            Assert.AreEqual(0, vm.SealedIndex, "망령이 없으면 코스트 3 중 슬롯 최소");

            Assert.IsFalse(Build(1, 1).HasSealForecast, "1-1 은 봉인 규칙 없음");
        }
    }
}
