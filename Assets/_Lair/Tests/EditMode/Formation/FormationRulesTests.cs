using System;
using System.Collections.Generic;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - FormationRules 순수 규칙 (코스트 합 · 종당 최대 · IsValid 경계 · CanPlace 전 분기 · 해금 · 봉인 슬롯). dungeon-formation §2.5, §9.1.
    public class FormationRulesTests
    {
        private FormationDatabase _db;
        private Func<EMonster, int> _cost;

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
            _cost = _db.CostOf;
        }

        //# --- CostSum / CountOf ---

        [Test]
        public void 코스트_합은_기본_편성_13이고_단일_종_6곳은_종별_곱이다()
        {
            Assert.AreEqual(13, FormationRules.CostSum(_db.DefaultFormation(), _cost));
            Assert.AreEqual(18, FormationRules.CostSum(new[] { EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Wisp }, _cost));
            Assert.AreEqual(6, FormationRules.CostSum(new[] { EMonster.Phantom, EMonster.Phantom, EMonster.Phantom, EMonster.Plague, EMonster.Plague, EMonster.Plague }, _cost));
            Assert.AreEqual(0, FormationRules.CostSum(new EMonster[0], _cost), "빈 편성");
        }

        [Test]
        public void 코스트_합은_인자_costOf를_쓴다()
        {
            Assert.AreEqual(6, FormationRules.CostSum(_db.DefaultFormation(), m => 1));
        }

        [Test]
        public void 종별_개수는_해당_종만_센다()
        {
            EMonster[] f = { EMonster.Plague, EMonster.Plague, EMonster.Hex, EMonster.Plague, EMonster.Phantom, EMonster.Hex };
            Assert.AreEqual(3, FormationRules.CountOf(f, EMonster.Plague));
            Assert.AreEqual(2, FormationRules.CountOf(f, EMonster.Hex));
            Assert.AreEqual(1, FormationRules.CountOf(f, EMonster.Phantom));
            Assert.AreEqual(0, FormationRules.CountOf(f, EMonster.Wisp));
        }

        //# --- IsValid / Describe ---

        [Test]
        public void 코스트_상한_경계는_13_유효_14_무효다()
        {
            Assert.IsTrue(FormationRules.IsValid(_db.DefaultFormation(), 13, 2, _cost), "13 = 상한");
            Assert.AreEqual(14, FormationRules.CostSum(FormationTestKit.Cost14(), _cost));
            Assert.IsFalse(FormationRules.IsValid(FormationTestKit.Cost14(), 13, 2, _cost), "14 > 13");
            Assert.IsTrue(FormationRules.IsValid(FormationTestKit.Cost14(), 14, 2, _cost), "상한 인자가 쓰인다(상수 13 비의존)");
            Assert.IsFalse(FormationRules.IsValid(_db.DefaultFormation(), 12, 2, _cost));
        }

        [Test]
        public void 종당_최대_경계는_2곳_유효_3곳_무효이고_인자가_3이면_유효다()
        {
            EMonster[] two = { EMonster.Plague, EMonster.Plague, EMonster.Hex, EMonster.Hex, EMonster.Phantom, EMonster.Phantom };
            EMonster[] three = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Hex, EMonster.Hex, EMonster.Phantom };
            Assert.IsTrue(FormationRules.IsValid(two, 13, 2, _cost));
            Assert.IsFalse(FormationRules.IsValid(three, 13, 2, _cost));
            Assert.IsTrue(FormationRules.IsValid(three, 13, 3, _cost));
            EMonster[] four = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Hex, EMonster.Hex };
            Assert.IsFalse(FormationRules.IsValid(four, 13, 3, _cost), "3 최대에서 4곳은 무효");
        }

        [Test]
        public void 종당_최대는_상수가_아니라_인자다()
        {
            EMonster[] six = { EMonster.Phantom, EMonster.Phantom, EMonster.Phantom, EMonster.Phantom, EMonster.Phantom, EMonster.Phantom };
            Assert.IsFalse(FormationRules.IsValid(six, 13, 5, _cost));
            Assert.IsTrue(FormationRules.IsValid(six, 13, 6, _cost));
        }

        [Test]
        public void 슬롯_수가_6이_아니거나_null이면_무효다()
        {
            Assert.IsNotNull(FormationRules.Describe(null, 13, 2, _cost));
            Assert.IsNotNull(FormationRules.Describe(new EMonster[0], 13, 2, _cost));
            Assert.IsNotNull(FormationRules.Describe(new[] { EMonster.Wisp, EMonster.Wisp, EMonster.Hex, EMonster.Hex, EMonster.Plague }, 13, 2, _cost), "5곳(슬롯 누락)");
            Assert.IsNotNull(FormationRules.Describe(new[] { EMonster.Wisp, EMonster.Wisp, EMonster.Hex, EMonster.Hex, EMonster.Plague, EMonster.Plague, EMonster.Phantom }, 13, 2, _cost), "7곳");
            StringAssert.Contains("슬롯 수", FormationRules.Describe(null, 13, 2, _cost));
        }

        [Test]
        public void 알_수_없는_종은_편성할_수_없어_무효다()
        {
            EMonster[] f = _db.DefaultFormation();
            f[2] = (EMonster)99;
            string issue = FormationRules.Describe(f, 99, 6, _cost);
            StringAssert.Contains("슬롯 3", issue);
            StringAssert.Contains("편성할 수 없음", issue);
        }

        [Test]
        public void Describe는_유효하면_null이고_위반마다_원인을_담는다()
        {
            Assert.IsNull(FormationRules.Describe(_db.DefaultFormation(), 13, 2, _cost));
            StringAssert.Contains("14", FormationRules.Describe(FormationTestKit.Cost14(), 13, 2, _cost));
            StringAssert.Contains("상한 13", FormationRules.Describe(FormationTestKit.Cost14(), 13, 2, _cost));
            EMonster[] three = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Hex, EMonster.Hex, EMonster.Phantom };
            StringAssert.Contains("Plague", FormationRules.Describe(three, 13, 2, _cost));
            StringAssert.Contains("종당 최대 2", FormationRules.Describe(three, 13, 2, _cost));
        }

        [Test]
        public void 코스트와_종당_최대를_동시에_어기면_코스트_원인을_먼저_보고한다()
        {
            EMonster[] f = { EMonster.Wisp, EMonster.Wisp, EMonster.Wisp, EMonster.Reaper, EMonster.Reaper, EMonster.Reaper };
            StringAssert.Contains("상한", FormationRules.Describe(f, 13, 2, _cost));
        }

        [Test]
        public void 코스트_인자가_상수_1이면_6곳은_상한_6에서_유효_5에서_무효다()
        {
            EMonster[] f = _db.DefaultFormation();
            Assert.IsTrue(FormationRules.IsValid(f, 6, 2, m => 1));
            Assert.IsFalse(FormationRules.IsValid(f, 5, 2, m => 1));
        }

        [Test]
        public void IsValid와_Describe는_편성을_바꾸지_않는다()
        {
            EMonster[] f = FormationTestKit.Cost14();
            EMonster[] snapshot = (EMonster[])f.Clone();
            FormationRules.IsValid(f, 13, 2, _cost);
            FormationRules.CanPlace(f, 0, EMonster.Plague, 13, 2, _cost);
            CollectionAssert.AreEqual(snapshot, f);
        }

        //# --- CanPlace 전 분기 ---

        [Test]
        public void 같은_종을_넣으면_Same이고_상한_초과_편성이어도_Same이다()
        {
            Assert.AreEqual(EFormationPlace.Same, FormationRules.CanPlace(_db.DefaultFormation(), 0, EMonster.Wisp, 13, 2, _cost));
            Assert.AreEqual(EFormationPlace.Same, FormationRules.CanPlace(FormationTestKit.Cost14(), 2, EMonster.Wraith, 13, 2, _cost), "무효 편성에서도 무동작");
        }

        [Test]
        public void 교체_후_합이_상한과_같으면_Ok_하나라도_넘으면_OverCost다()
        {
            EMonster[] f = _db.DefaultFormation();
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 3, EMonster.Reaper, 13, 3, _cost), "망령 3 -> 사신 3 = 13 (경계)");
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(f, 2, EMonster.Wraith, 13, 3, _cost), "저주술사 2 -> 망령 3 = 14");
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(f, 4, EMonster.Wisp, 13, 3, _cost), "역병귀 1 -> 도깨비불 3 = 15");
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 0, EMonster.Plague, 13, 2, _cost), "코스트가 줄면 항상 Ok");
        }

        [Test]
        public void 종당_최대에_이미_도달한_종을_더_넣으면_OverSpecies다()
        {
            EMonster[] f = { EMonster.Phantom, EMonster.Phantom, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Wisp };
            Assert.AreEqual(EFormationPlace.OverSpecies, FormationRules.CanPlace(f, 5, EMonster.Phantom, 13, 2, _cost));
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 5, EMonster.Phantom, 13, 3, _cost), "최대 3 이면 3번째 가능");
        }

        [Test]
        public void 종당_최대_직전의_개수에서_하나_더는_Ok다()
        {
            EMonster[] f = { EMonster.Phantom, EMonster.Wraith, EMonster.Hex, EMonster.Wisp, EMonster.Plague, EMonster.Plague };
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 1, EMonster.Phantom, 13, 2, _cost), "1곳 -> 2곳 = 최대 경계");
        }

        [Test]
        public void 코스트와_종당_최대를_모두_어기면_OverCost가_우선이다()
        {
            EMonster[] f = { EMonster.Wisp, EMonster.Wisp, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(f, 4, EMonster.Wisp, 13, 2, _cost));
        }

        [Test]
        public void CanPlace_결과는_선택_둥지의_기존_종을_코스트에서_뺀_값으로_계산한다()
        {
            EMonster[] f = _db.DefaultFormation();
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 0, EMonster.Reaper, 13, 3, _cost), "도깨비불 3 을 사신 3 으로 - 13 유지");
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(f, 5, EMonster.Wisp, 13, 3, _cost), "환령 1 -> 도깨비불 3 = 15");
        }

        [Test]
        public void CanPlace는_상한_인자를_쓴다()
        {
            EMonster[] f = _db.DefaultFormation();
            Assert.AreEqual(EFormationPlace.OverCost, FormationRules.CanPlace(f, 3, EMonster.Reaper, 12, 3, _cost));
            Assert.AreEqual(EFormationPlace.Ok, FormationRules.CanPlace(f, 4, EMonster.Wisp, 15, 3, _cost));
        }

        [Test]
        public void CanPlace는_모든_슬롯_인덱스에서_범위_안이면_예외없이_판정한다()
        {
            for (int i = 0; i < FormationDatabase.SlotCount; ++i)
            {
                foreach (EMonster m in Enum.GetValues(typeof(EMonster)))
                {
                    Assert.DoesNotThrow(() => FormationRules.CanPlace(_db.DefaultFormation(), i, m, 13, 2, _cost));
                }
            }
        }

        //# --- 해금 / 종당 최대 단계 ---

        [Test]
        public void 해금은_ClearedStage_0에서_잠기고_1부터_열린다()
        {
            FormationConfigRow c = _db.Config;
            Assert.IsFalse(FormationRules.IsUnlocked(c, 0), "1-1 클리어 전");
            Assert.IsTrue(FormationRules.IsUnlocked(c, 1));
            Assert.IsTrue(FormationRules.IsUnlocked(c, 25));
        }

        [Test]
        public void 해금_단계는_설정값을_따른다()
        {
            FormationConfigRow c = new FormationConfigRow { costCap = 13, unlockClearedStage = 3, maxPerSpeciesBase = 2, maxPerSpeciesExpanded = 3, expandClearedStage = 5 };
            Assert.IsFalse(FormationRules.IsUnlocked(c, 2));
            Assert.IsTrue(FormationRules.IsUnlocked(c, 3));
        }

        [Test]
        public void 종당_최대는_ClearedStage_4까지_2_5부터_3이다()
        {
            FormationConfigRow c = _db.Config;
            Assert.AreEqual(2, FormationRules.MaxPerSpecies(c, 0));
            Assert.AreEqual(2, FormationRules.MaxPerSpecies(c, 1));
            Assert.AreEqual(2, FormationRules.MaxPerSpecies(c, 4));
            Assert.AreEqual(3, FormationRules.MaxPerSpecies(c, 5));
            Assert.AreEqual(3, FormationRules.MaxPerSpecies(c, 6));
            Assert.AreEqual(3, FormationRules.MaxPerSpecies(c, 25));
        }

        //# --- PickSealedSlot ---

        [Test]
        public void 봉인_종이_있으면_그_종의_최소_슬롯을_고른다()
        {
            EMonster[] f = { EMonster.Hex, EMonster.Plague, EMonster.Plague, EMonster.Wisp, EMonster.Wraith, EMonster.Wraith };
            Assert.AreEqual(1, FormationRules.PickSealedSlot(f, EMonster.Plague, _cost), "역병귀 슬롯 2·3 중 2");
            Assert.AreEqual(4, FormationRules.PickSealedSlot(f, EMonster.Wraith, _cost), "망령 슬롯 5·6 중 5 (코스트 3 도깨비불이 먼저여도 봉인 종 우선)");
        }

        [Test]
        public void 봉인_종이_마지막_슬롯에만_있어도_코스트_최고_둥지보다_우선한다()
        {
            EMonster[] f = { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(5, FormationRules.PickSealedSlot(f, EMonster.Phantom, _cost));
            Assert.AreEqual(0, FormationRules.PickSealedSlot(f, EMonster.Wisp, _cost));
        }

        [Test]
        public void 봉인_종이_없으면_코스트_최고_둥지_동률이면_슬롯_최소를_고른다()
        {
            EMonster[] hexFirst = { EMonster.Hex, EMonster.Plague, EMonster.Reaper, EMonster.Reaper, EMonster.Phantom, EMonster.Plague };
            Assert.AreEqual(2, FormationRules.PickSealedSlot(hexFirst, EMonster.Wraith, _cost), "코스트 3 은 사신 슬롯 3·4 -> 3");
            EMonster[] allSame = { EMonster.Plague, EMonster.Plague, EMonster.Plague, EMonster.Phantom, EMonster.Phantom, EMonster.Phantom };
            Assert.AreEqual(0, FormationRules.PickSealedSlot(allSame, EMonster.Wraith, _cost), "전부 코스트 1 동률 -> 슬롯 1");
            EMonster[] costTwoBest = { EMonster.Plague, EMonster.Hex, EMonster.Phantom, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(1, FormationRules.PickSealedSlot(costTwoBest, EMonster.Wraith, _cost), "최고 코스트가 2 일 때 슬롯 2");
        }

        [Test]
        public void 기본_편성의_봉인_결과는_종별로_현행과_같다()
        {
            EMonster[] d = _db.DefaultFormation();
            Assert.AreEqual(3, FormationRules.PickSealedSlot(d, EMonster.Wraith, _cost), "1-4 망령 = 슬롯 4");
            Assert.AreEqual(5, FormationRules.PickSealedSlot(d, EMonster.Phantom, _cost));
            Assert.AreEqual(4, FormationRules.PickSealedSlot(d, EMonster.Plague, _cost));
            Assert.AreEqual(2, FormationRules.PickSealedSlot(d, EMonster.Hex, _cost));
            Assert.AreEqual(0, FormationRules.PickSealedSlot(d, EMonster.Wisp, _cost));
            Assert.AreEqual(1, FormationRules.PickSealedSlot(d, EMonster.Reaper, _cost));
        }

        [Test]
        public void 후보_필터는_이미_봉인된_둥지를_제외하고_봉인_종_슬롯도_제외할_수_있다()
        {
            EMonster[] f = { EMonster.Wraith, EMonster.Wraith, EMonster.Hex, EMonster.Reaper, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(1, FormationRules.PickSealedSlot(f, EMonster.Wraith, _cost, i => i != 0), "슬롯 1 이 이미 봉인 -> 다음 망령");
            Assert.AreEqual(3, FormationRules.PickSealedSlot(f, EMonster.Wraith, _cost, i => i == 2 || i == 3 || i == 4), "망령 둥지가 모두 제외 -> 코스트 최고(사신 3)");
            Assert.AreEqual(-1, FormationRules.PickSealedSlot(f, EMonster.Wraith, _cost, i => false), "후보 없음");
        }

        [Test]
        public void 후보_필터가_null이면_전부_후보다()
        {
            EMonster[] f = _db.DefaultFormation();
            Assert.AreEqual(3, FormationRules.PickSealedSlot(f, EMonster.Wraith, _cost, null));
        }

        [Test]
        public void 빈_출력_목록은_봉인할_둥지가_없어_음수_1이다()
        {
            Assert.AreEqual(-1, FormationRules.PickSealedSlot(new List<EMonster>(), EMonster.Wraith, _cost));
        }

        [Test]
        public void costOf가_null이면_전부_동률이라_첫_후보_슬롯이다()
        {
            EMonster[] f = { EMonster.Plague, EMonster.Wisp, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            Assert.AreEqual(0, FormationRules.PickSealedSlot(f, EMonster.Wraith, null));
            Assert.AreEqual(1, FormationRules.PickSealedSlot(f, EMonster.Wraith, null, i => i >= 1));
        }

        //# --- Describe (기본 편성) ---

        [Test]
        public void 기본_편성_Describe는_기본_해금_단계_설정에서_null이다()
        {
            Assert.IsNull(FormationRules.Describe(_db.DefaultFormation(), _db.Config.costCap, FormationRules.MaxPerSpecies(_db.Config, 1), _cost));
            Assert.IsNull(FormationRules.Describe(_db.DefaultFormation(), _db.Config.costCap, FormationRules.MaxPerSpecies(_db.Config, 5), _cost));
        }
    }
}
