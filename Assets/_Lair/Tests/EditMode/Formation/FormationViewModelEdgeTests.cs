using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - FormationViewModel 엣지 (배치/교체 흐름 · 토스트 문구 · 봉인 예고 · 무효 편성 · 성향/규칙 문구 · 표시값). dungeon-formation §7, §9.1 "편성 VM".
    //# 기본 VM 4건은 FormationViewModelTests - 여기서는 중복 없이 확장한다. 난이도 정적상태(DifficultyTable.Current)를 베이스가 격리한다.
    public class FormationViewModelEdgeTests : DifficultyStaticStateTestBase
    {
        private FormationDatabase _db;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
        }

        private FormationViewModel Build(int stage, int cleared, List<FormationSlotEntry> saved = null, Func<EMonster, int> awaken = null, int difficulty = 0)
        {
            MetaProfile profile = new MetaProfile
            {
                ClearedStage = cleared,
                SelectedDifficulty = difficulty,
                Formation = saved ?? new List<FormationSlotEntry>(),
            };
            return new FormationViewModel(_db, WorldMapTestSupport.FullDb(), profile, stage, WorldMapTestSupport.Text, awaken);
        }

        private static string Text(int id)
        {
            return WorldMapTestSupport.Text(id);
        }

        private static List<FormationSlotEntry> Rows(params EMonster[] monsters)
        {
            return FormationTestKit.Rows(monsters);
        }

        //# --- 코스트 합 / 게이지 ---

        [Test]
        public void 코스트_게이지_문구와_칸_수는_합과_상한을_따른다()
        {
            FormationViewModel vm = Build(1, 1);
            Assert.AreEqual(13, vm.CostCap);
            Assert.AreEqual("코스트 13 / 13", vm.CostGaugeText);
            Assert.AreEqual(13, vm.PipCount);

            vm.Place(EMonster.Plague);
            Assert.AreEqual("코스트 11 / 13", vm.CostGaugeText);
            Assert.AreEqual(13, vm.PipCount, "합이 상한보다 작아도 칸은 상한만큼");
        }

        [Test]
        public void 상한을_넘긴_저장_편성은_칸_수가_합까지_늘고_코스트_표시가_합을_보여준다()
        {
            FormationViewModel vm = Build(1, 1, Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Plague, EMonster.Phantom));
            Assert.AreEqual(14, vm.CostSum);
            Assert.AreEqual(14, vm.PipCount);
            Assert.AreEqual("코스트 14 / 13", vm.CostGaugeText);
            Assert.IsTrue(vm.IsInvalid);
            Assert.IsFalse(vm.CanSortie);
        }

        [Test]
        public void 종당_최대는_ClearedStage로_2에서_3으로_늘고_VM이_그_값을_노출한다()
        {
            Assert.AreEqual(2, Build(1, 1).MaxPerSpecies);
            Assert.AreEqual(2, Build(5, 4).MaxPerSpecies);
            Assert.AreEqual(3, Build(6, 5).MaxPerSpecies);
        }

        //# --- 배치 / 교체 흐름 ---

        [Test]
        public void 선택은_둥지_1에서_시작하고_배치_성공마다_다음으로_가며_6_다음은_1이다()
        {
            FormationViewModel vm = Build(1, 5);
            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.IsNull(vm.Place(EMonster.Phantom));
            Assert.AreEqual(1, vm.SelectedIndex);
            Assert.IsNull(vm.Place(EMonster.Phantom));
            Assert.AreEqual(2, vm.SelectedIndex);

            vm.SelectNest(5);
            Assert.IsNull(vm.Place(EMonster.Plague));
            Assert.AreEqual(0, vm.SelectedIndex, "6 다음은 1");
        }

        [Test]
        public void 같은_종을_배치하면_null이고_선택이_이동하지_않고_이벤트도_없다()
        {
            FormationViewModel vm = Build(1, 1);
            int changed = 0;
            vm.OnChanged += () => ++changed;

            Assert.IsNull(vm.Place(EMonster.Wisp), "둥지 1 은 이미 도깨비불");

            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
        }

        [Test]
        public void 배치_실패는_상태를_바꾸지_않고_이벤트도_없다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.SelectNest(4);
            int changed = 0;
            vm.OnChanged += () => ++changed;

            Assert.IsNotNull(vm.Place(EMonster.Wisp));

            Assert.AreEqual(4, vm.SelectedIndex);
            Assert.AreEqual(0, changed);
            Assert.AreEqual(13, vm.CostSum);
            Assert.AreEqual(EMonster.Plague, vm.GetNest(4).Monster);
        }

        [Test]
        public void OnChanged는_배치_성공_둥지_선택_되돌리기에서만_발행된다()
        {
            FormationViewModel vm = Build(1, 1);
            int changed = 0;
            vm.OnChanged += () => ++changed;

            vm.SelectNest(2);
            Assert.AreEqual(1, changed);
            vm.SelectNest(2);
            vm.SelectNest(-1);
            vm.SelectNest(6);
            Assert.AreEqual(1, changed, "같은 둥지/범위 밖 선택은 무시");
            Assert.AreEqual(2, vm.SelectedIndex);

            vm.Place(EMonster.Plague);
            Assert.AreEqual(2, changed);
            vm.ResetToDefault();
            Assert.AreEqual(3, changed);
        }

        [Test]
        public void 교체하면_코스트가_즉시_반영되고_원래_종으로_되돌리면_0행_저장으로_수렴한다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.Place(EMonster.Plague);
            Assert.AreEqual(6, vm.BuildSaveRows().Count);
            Assert.IsFalse(vm.GetNest(0).IsDefault);

            vm.SelectNest(0);
            vm.Place(EMonster.Wisp);
            Assert.AreEqual(13, vm.CostSum);
            Assert.AreEqual(0, vm.BuildSaveRows().Count, "기본 편성과 같아지면 기본 추적(0행)");
            Assert.IsTrue(vm.GetNest(0).IsDefault);
        }

        [Test]
        public void 저장된_유효_편성으로_열면_그_편성이_표시되고_기본과_다른_칸은_IsDefault가_거짓이다()
        {
            FormationViewModel vm = Build(1, 1, Rows(FormationTestKit.AlteredValid()));
            Assert.AreEqual(11, vm.CostSum);
            Assert.AreEqual(EMonster.Reaper, vm.GetNest(0).Monster);
            Assert.IsFalse(vm.GetNest(0).IsDefault);
            Assert.IsTrue(vm.GetNest(2).IsDefault, "저주술사는 기본 편성 슬롯 3 과 같다");
            Assert.AreEqual(6, vm.BuildSaveRows().Count);
        }

        [Test]
        public void 기본_편성과_같은_6행_저장은_열면_0행으로_정규화되어_저장된다()
        {
            FormationViewModel vm = Build(1, 1, Rows(_db.DefaultFormation()));
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
            Assert.IsFalse(vm.IsInvalid);
        }

        [Test]
        public void 모양이_틀린_저장_행은_기본_편성으로_열린다()
        {
            FormationViewModel vm = Build(1, 5, Rows(EMonster.Wisp, EMonster.Reaper));
            Assert.AreEqual(13, vm.CostSum);
            Assert.IsFalse(vm.IsInvalid);
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
        }

        [Test]
        public void 되돌리기는_선택을_둥지_1로_옮기고_저장_행을_0행으로_만든다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.SelectNest(3);
            vm.Place(EMonster.Hex);
            vm.ResetToDefault();
            Assert.AreEqual(0, vm.SelectedIndex);
            Assert.AreEqual(13, vm.CostSum);
            Assert.AreEqual(0, vm.BuildSaveRows().Count);
        }

        [Test]
        public void BuildSaveRows는_VM_상태를_바꾸지_않고_호출마다_새_리스트다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.Place(EMonster.Plague);
            List<FormationSlotEntry> a = vm.BuildSaveRows();
            a.Clear();
            Assert.AreEqual(6, vm.BuildSaveRows().Count);
        }

        [Test]
        public void VM의_편성_변경은_프로필_행을_수정하지_않는다()
        {
            List<FormationSlotEntry> saved = Rows(FormationTestKit.AlteredValid());
            MetaProfile profile = new MetaProfile { ClearedStage = 1, Formation = saved };
            FormationViewModel vm = new FormationViewModel(_db, WorldMapTestSupport.FullDb(), profile, 1, WorldMapTestSupport.Text, null);
            vm.Place(EMonster.Plague);
            vm.ResetToDefault();
            Assert.AreEqual("Reaper", profile.Formation[0].Monster, "저장은 출격 콜백 소유자 (X 로 닫으면 미저장)");
            Assert.AreEqual(6, profile.Formation.Count);
        }

        //# --- 토스트 문구 id ---

        [Test]
        public void 코스트_부족_토스트는_1402_포맷에_필요_코스트와_남은_코스트를_채운다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.SelectNest(2);
            Assert.AreEqual(string.Format(Text(FormationViewModel.StrToastCost), 3, 2), vm.Place(EMonster.Wraith), "저주술사(2) 자리: 남은 2, 필요 3");
        }

        [Test]
        public void 종당_최대_토스트는_1403_포맷에_종_이름과_최대를_채운다_5_이후는_3곳이다()
        {
            FormationViewModel vm = Build(1, 1);
            Assert.IsNull(vm.Place(EMonster.Plague), "둥지 1: 역병귀 2곳째");
            Assert.AreEqual(string.Format(Text(FormationViewModel.StrToastSpecies), "역병귀", 2), vm.Place(EMonster.Plague), "둥지 2: 3곳째");

            FormationViewModel expanded = Build(6, 5);
            Assert.IsNull(expanded.Place(EMonster.Plague), "역병귀 2곳째");
            Assert.IsNull(expanded.Place(EMonster.Plague), "종당 3 이면 세 번째 성공");
            Assert.AreEqual(string.Format(Text(FormationViewModel.StrToastSpecies), "역병귀", 3), expanded.Place(EMonster.Plague), "네 번째 실패");
        }

        [Test]
        public void 코스트와_종당_최대를_모두_어기면_코스트_토스트가_우선이다()
        {
            FormationViewModel vm = Build(1, 1, Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom));
            vm.SelectNest(4);
            StringAssert.Contains("코스트가 부족", vm.Place(EMonster.Wisp));
        }

        [Test]
        public void 토스트_문구_id_상수는_Strings_Ko_1397에서_1405에_일치한다()
        {
            Assert.AreEqual(1397, FormationViewModel.StrCostGauge);
            Assert.AreEqual(1398, FormationViewModel.StrCostOf);
            Assert.AreEqual(1399, FormationViewModel.StrCountOf);
            Assert.AreEqual(1400, FormationViewModel.StrAwaken);
            Assert.AreEqual(1401, FormationViewModel.StrNest);
            Assert.AreEqual(1402, FormationViewModel.StrToastCost);
            Assert.AreEqual(1403, FormationViewModel.StrToastSpecies);
            Assert.AreEqual(1404, FormationViewModel.StrSealForecast);
            Assert.AreEqual(1405, FormationViewModel.StrToastInvalid);
        }

        //# --- 무효 편성 / 출격 ---

        [Test]
        public void 유효_편성의_출격은_토스트가_null이고_true다()
        {
            FormationViewModel vm = Build(1, 1);
            Assert.IsTrue(vm.TrySortie(out string toast));
            Assert.IsNull(toast);
        }

        [Test]
        public void 종당_최대를_넘긴_저장_편성도_무효이고_단계가_올라가면_유효하다()
        {
            List<FormationSlotEntry> triple = Rows(EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Phantom, EMonster.Hex, EMonster.Hex);
            FormationViewModel early = Build(1, 4, triple);
            Assert.IsTrue(early.IsInvalid);
            Assert.IsFalse(early.TrySortie(out string toast));
            Assert.AreEqual(Text(FormationViewModel.StrToastInvalid), toast);

            Assert.IsFalse(Build(6, 5, triple).IsInvalid);
        }

        [Test]
        public void 무효_편성은_교체로_고치면_출격이_다시_열린다()
        {
            FormationViewModel vm = Build(1, 1, Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Plague, EMonster.Phantom));
            Assert.IsFalse(vm.CanSortie);
            vm.SelectNest(3);
            vm.Place(EMonster.Hex);
            Assert.IsTrue(vm.CanSortie);
            Assert.IsFalse(vm.IsInvalid);
        }

        [Test]
        public void 무효_편성에서는_코스트를_더_늘리는_교체가_막힌다()
        {
            FormationViewModel vm = Build(1, 1, Rows(EMonster.Wisp, EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Plague, EMonster.Phantom));
            vm.SelectNest(5);
            Assert.IsNotNull(vm.Place(EMonster.Wraith), "합 14 에서 환령 -> 망령 은 16");
            Assert.AreEqual(14, vm.CostSum);
        }

        //# --- 봉인 예고 ---

        [Test]
        public void 봉인된_둥지_8개_스테이지의_예고는_기본_편성에서_봉인_종_슬롯과_해제_퍼센트를_가리킨다()
        {
            //# (선형 스테이지, 봉인 종, 기본 편성에서의 둥지 인덱스, 해제 HP%) - Stage_Rule_Param 의 8개 SealedNest.
            object[][] cases =
            {
                new object[] { 4, EMonster.Wraith, 3, 60 },
                new object[] { 7, EMonster.Phantom, 5, 70 },
                new object[] { 12, EMonster.Plague, 4, 60 },
                new object[] { 14, EMonster.Hex, 2, 50 },
                new object[] { 17, EMonster.Wisp, 0, 60 },
                new object[] { 19, EMonster.Reaper, 1, 70 },
                new object[] { 22, EMonster.Hex, 2, 60 },
                new object[] { 24, EMonster.Plague, 4, 55 },
            };
            foreach (object[] c in cases)
            {
                FormationViewModel vm = Build((int)c[0], 25);
                string label = $"스테이지 {c[0]}";
                Assert.IsTrue(vm.HasSealForecast, label);
                Assert.AreEqual((int)c[2], vm.SealedIndex, label);
                Assert.AreEqual(string.Format(Text(FormationViewModel.StrSealForecast), (int)c[2] + 1, SpeciesVisual.SpeciesName((EMonster)c[1]), (int)c[3]), vm.SealForecastText, label);
                for (int i = 0; i < 6; ++i)
                {
                    Assert.AreEqual(i == (int)c[2], vm.GetNest(i).IsSealed, $"{label} 둥지 {i + 1}");
                }
            }
        }

        [Test]
        public void 봉인_규칙이_없는_스테이지는_예고가_비고_어느_둥지도_봉인되지_않는다()
        {
            foreach (int stage in new[] { 1, 2, 3, 5, 6, 25 })
            {
                FormationViewModel vm = Build(stage, 25);
                Assert.IsFalse(vm.HasSealForecast, "스테이지 " + stage);
                Assert.AreEqual(-1, vm.SealedIndex);
                Assert.AreEqual(string.Empty, vm.SealForecastText);
                for (int i = 0; i < 6; ++i)
                {
                    Assert.IsFalse(vm.GetNest(i).IsSealed);
                }
            }
        }

        [Test]
        public void 봉인_종_망령을_둘로_늘리면_예고가_슬롯_번호_최소_망령_둥지로_옮겨간다()
        {
            FormationViewModel vm = Build(4, 4);
            Assert.AreEqual(3, vm.SealedIndex);
            vm.Place(EMonster.Wraith);
            Assert.AreEqual(0, vm.SealedIndex, "망령 둥지 1·4 중 1");
            StringAssert.Contains("둥지 1", vm.SealForecastText);
            Assert.IsTrue(vm.GetNest(0).IsSealed);
            Assert.IsFalse(vm.GetNest(3).IsSealed);
        }

        [Test]
        public void 봉인_종이_없어지면_코스트_최고_둥지로_예고하고_교체마다_갱신된다()
        {
            FormationViewModel vm = Build(4, 4);
            vm.SelectNest(3);
            vm.Place(EMonster.Reaper);
            Assert.AreEqual(0, vm.SealedIndex, "코스트 3: 도깨비불(1)·사신(2·4) 중 슬롯 최소");
            StringAssert.Contains("도깨비불", vm.SealForecastText);

            vm.ResetToDefault();
            Assert.AreEqual(3, vm.SealedIndex);
        }

        //# --- 성향 / 규칙 / 표기 ---

        [Test]
        public void 챕터_성향_문구는_스테이지가_속한_챕터의_힌트다()
        {
            Assert.AreEqual(Text(1408), Build(1, 1).HintText);
            Assert.AreEqual(Text(1409), Build(6, 6).HintText);
            Assert.AreEqual(Text(1410), Build(11, 11).HintText);
            Assert.AreEqual(Text(1411), Build(16, 16).HintText);
            Assert.AreEqual(Text(1412), Build(25, 25).HintText);
            Assert.AreEqual(Text(1412), Build(21, 21).HintText, "챕터 경계 21 = 5장 첫 스테이지");
            Assert.AreEqual(Text(1411), Build(20, 20).HintText, "20 = 4장 마지막");
        }

        [Test]
        public void 스테이지_표기와_이름_영웅_문구가_채워진다()
        {
            FormationViewModel vm = Build(7, 7);
            Assert.AreEqual("2-2", vm.StageNumberText);
            Assert.IsNotEmpty(vm.StageNameText);
            Assert.IsNotEmpty(vm.HeroText);
            Assert.AreEqual(7, vm.Stage);
            Assert.AreEqual("1-1", Build(1, 1).StageNumberText);
            Assert.AreEqual("5-5", Build(25, 25).StageNumberText);
        }

        [Test]
        public void 규칙_문구는_규칙이_없으면_규칙_없음_있으면_규칙_이름을_담는다()
        {
            Assert.AreEqual(Text(WorldMapViewModel.StrNoRules), Build(1, 1).RulesText);
            Assert.AreNotEqual(Text(WorldMapViewModel.StrNoRules), Build(4, 4).RulesText);
            Assert.IsNotEmpty(Build(4, 4).RulesText);
        }

        [Test]
        public void 규칙이_둘인_스테이지는_줄바꿈으로_구분한_두_항목이다()
        {
            StringAssert.Contains("\n", Build(14, 14).RulesText);
            Assert.GreaterOrEqual(Build(14, 14).RulesText.Split('\n').Length, 2);
        }

        [Test]
        public void 난이도_Easy는_칩이_숨겨져_빈_문자열이다()
        {
            Assert.AreEqual(0, Build(1, 1).Difficulty);
            Assert.AreEqual(string.Empty, Build(1, 1).DifficultyText);
        }

        [Test]
        public void 난이도_테이블이_없으면_선택_난이도가_있어도_Easy로_본다()
        {
            FormationViewModel vm = Build(1, 25, difficulty: 2);
            Assert.AreEqual(0, vm.Difficulty);
            Assert.AreEqual(string.Empty, vm.DifficultyText);
        }

        [Test]
        public void 난이도_테이블이_있고_해금된_난이도면_라벨이_표시된다()
        {
            DifficultyTable.Current = DifficultyTestSupport.Table();
            MetaProfile profile = new MetaProfile { ClearedStage = 25, SelectedDifficulty = 1 };
            FormationViewModel vm = new FormationViewModel(_db, WorldMapTestSupport.FullDb(), profile, 1, WorldMapTestSupport.Text, null);
            Assert.AreEqual(1, vm.Difficulty);
            Assert.AreEqual(Text(1328), vm.DifficultyText);
        }

        //# --- 표시값 (둥지 칸 / 종 행) ---

        [Test]
        public void 둥지_칸은_제목_종_이름_코스트_선택_표식을_확정값으로_돌려준다()
        {
            FormationViewModel vm = Build(1, 1);
            FormationNestData first = vm.GetNest(0);
            Assert.AreEqual(0, first.Index);
            Assert.AreEqual("둥지 1", first.TitleText);
            Assert.AreEqual(EMonster.Wisp, first.Monster);
            Assert.AreEqual("도깨비불", first.NameText);
            Assert.AreEqual("코스트 3", first.CostText);
            Assert.AreEqual(3, first.Cost);
            Assert.AreEqual(SpeciesVisual.SpeciesGlowColor(EMonster.Wisp), first.SpeciesColor);
            Assert.IsTrue(first.IsSelected);
            Assert.IsTrue(first.IsDefault);
            Assert.IsFalse(first.IsSealed);

            FormationNestData last = vm.GetNest(5);
            Assert.AreEqual("둥지 6", last.TitleText);
            Assert.AreEqual("환령", last.NameText);
            Assert.AreEqual(1, last.Cost);
            Assert.IsFalse(last.IsSelected, "선택은 정확히 한 칸");
        }

        [Test]
        public void 종_행은_JSON_순서_6개이고_선택_둥지에_넣으면_무효가_되는_종만_흐려진다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.SelectNest(4);
            IReadOnlyList<FormationSpeciesData> rows = vm.BuildSpecies();

            Assert.AreEqual(6, rows.Count);
            CollectionAssert.AreEqual(
                new[] { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom },
                new[] { rows[0].Monster, rows[1].Monster, rows[2].Monster, rows[3].Monster, rows[4].Monster, rows[5].Monster });
            CollectionAssert.AreEqual(
                new[] { true, true, true, true, false, false },
                new[] { rows[0].Dimmed, rows[1].Dimmed, rows[2].Dimmed, rows[3].Dimmed, rows[4].Dimmed, rows[5].Dimmed },
                "역병귀(1) 자리: 코스트 2 이상 종은 상한 초과, 같은 종·환령은 가능");
        }

        [Test]
        public void 종당_최대에_이른_종은_흐려지고_편성_개수_문구가_갱신된다()
        {
            FormationViewModel vm = Build(1, 1);
            vm.Place(EMonster.Plague);
            IReadOnlyList<FormationSpeciesData> rows = vm.BuildSpecies();
            FormationSpeciesData plague = rows[4];
            Assert.AreEqual("편성 2 / 2", plague.CountText);
            Assert.IsTrue(plague.Dimmed, "선택 둥지(2)에 세 번째 역병귀 불가");
            Assert.AreEqual("편성 0 / 2", rows[0].CountText, "도깨비불은 교체돼 0곳");
        }

        [Test]
        public void 종_행의_체급_표기는_상점_레벨이_0이면_숨겨지고_축_칩과_코스트를_담는다()
        {
            FormationViewModel vm = Build(1, 1, awaken: m => m == EMonster.Reaper ? 2 : 0);
            IReadOnlyList<FormationSpeciesData> rows = vm.BuildSpecies();

            FormationSpeciesData reaper = rows[2];
            Assert.IsTrue(reaper.ShowAwaken);
            Assert.AreEqual("체급 Lv 2", reaper.AwakenText, "표시명 각성 → 체급 (monster-growth-currencies §3.1 U-6)");
            Assert.IsFalse(rows[0].ShowAwaken);
            Assert.AreEqual(EBuildAxis.Dps, reaper.Axis);
            Assert.AreEqual("코스트 3", reaper.CostText);
            Assert.AreEqual("코스트 2", rows[3].CostText);
            Assert.AreEqual("사신", reaper.NameText);
        }

        [Test]
        public void 각성_조회가_null이면_전_종이_Lv_0으로_숨겨진다()
        {
            FormationViewModel vm = Build(1, 1, awaken: null);
            foreach (FormationSpeciesData row in vm.BuildSpecies())
            {
                Assert.IsFalse(row.ShowAwaken);
            }
        }

        [Test]
        public void 해금_전_ClearedStage_0도_VM은_기본_상태로_만들어지고_해금_판정은_규칙이_소유한다()
        {
            Assert.IsFalse(FormationRules.IsUnlocked(_db.Config, 0), "1-1 클리어 전 팝업 잠김은 마을이 FormationRules 로 판정");
            FormationViewModel vm = Build(1, 0);
            Assert.AreEqual(13, vm.CostSum);
            Assert.AreEqual(2, vm.MaxPerSpecies);
        }
    }
}
