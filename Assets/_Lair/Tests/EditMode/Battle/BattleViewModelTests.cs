using NUnit.Framework;
using Lair.UI;
using Lair.Data;

namespace Lair.Tests.UI
{
    public class BattleViewModelTests
    {
        [Test]
        public void UpdateTimer_이벤트_발행과_값_노출()
        {
            BattleStateModel model = new BattleStateModel { TotalSeconds = 300f };
            BattleViewModel vm = new BattleViewModel(model);
            float captured = -1f;
            float capturedTotal = -1f;
            vm.OnTimerChanged += (e, t) => { captured = e; capturedTotal = t; };

            vm.UpdateTimer(42.5f);

            Assert.AreEqual(42.5f, captured, 0.0001f);
            Assert.AreEqual(300f, capturedTotal, 0.0001f);
            Assert.AreEqual(42.5f, vm.ElapsedSeconds, 0.0001f);
        }

        [Test]
        public void UpdateHeroHp_비율_계산_및_이벤트()
        {
            BattleStateModel model = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(model);
            float capturedRatio = -1f;
            vm.OnHeroHpRatioChanged += r => capturedRatio = r;

            vm.UpdateHeroHp(250, 1000);

            Assert.AreEqual(0.25f, capturedRatio, 0.0001f);
            Assert.AreEqual(0.25f, vm.HeroHpRatio, 0.0001f);
        }

        [Test]
        public void UpdateHeroHp_max_0이면_비율_0_안전()
        {
            BattleStateModel model = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(model);
            float capturedRatio = -1f;
            vm.OnHeroHpRatioChanged += r => capturedRatio = r;

            vm.UpdateHeroHp(0, 0);

            Assert.AreEqual(0f, capturedRatio, 0.0001f);
        }

        [Test]
        public void UpdateHeroHp_정수값_이벤트_current_max_발행()
        {
            BattleStateModel model = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(model);
            int capturedCurrent = -1;
            int capturedMax = -1;
            vm.OnHeroHpValuesChanged += (c, m) => { capturedCurrent = c; capturedMax = m; };

            vm.UpdateHeroHp(250, 1000);

            Assert.AreEqual(250, capturedCurrent);
            Assert.AreEqual(1000, capturedMax);
            Assert.AreEqual(250, vm.HeroHp);
            Assert.AreEqual(1000, vm.HeroMaxHp);
        }

        [Test]
        public void UpdateHeroHp_max_0_엣지에서도_정수값_이벤트_발행()
        {
            BattleStateModel model = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(model);
            int capturedCurrent = -1;
            int capturedMax = -1;
            bool fired = false;
            vm.OnHeroHpValuesChanged += (c, m) => { capturedCurrent = c; capturedMax = m; fired = true; };

            vm.UpdateHeroHp(0, 0);

            Assert.IsTrue(fired, "max=0 엣지에서도 이벤트 발행");
            Assert.AreEqual(0, capturedCurrent);
            Assert.AreEqual(0, capturedMax);
        }

        [Test]
        public void EndBattle_Result_저장_이벤트_발행()
        {
            BattleStateModel model = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(model);
            BattleResult captured = BattleResult.None;
            vm.OnBattleEnded += r => captured = r;

            vm.EndBattle(BattleResult.Win);

            Assert.AreEqual(BattleResult.Win, captured);
            Assert.AreEqual(BattleResult.Win, vm.Result);
        }

        [Test]
        public void AddPick_중복_픽은_Count_누적_분류_구분()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            Lair.Card.CardData cardA = Lair.Tests.Helpers.FakeCardData.Create(ECardId.WispHpBoost);
            Lair.Card.CardData cardB = Lair.Tests.Helpers.FakeCardData.Create(ECardId.Frenzy);
            int changed = 0;
            vm.OnBuildChanged += () => changed++;

            vm.AddPick(cardA, isPassive: true);
            vm.AddPick(cardA, isPassive: true);   //# 같은 카드 재픽
            vm.AddPick(cardB, isPassive: false);

            Assert.AreEqual(2, vm.Build.Count, "고유 카드 2종");
            Assert.AreEqual(2, vm.Build[0].Count, "cardA 2회 누적");
            Assert.IsTrue(vm.Build[0].IsPassive);
            Assert.AreEqual(1, vm.Build[1].Count, "cardB 1회");
            Assert.IsFalse(vm.Build[1].IsPassive);
            Assert.AreEqual(3, changed, "AddPick 마다 OnBuildChanged 발행");
        }
    }
}
