using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;

namespace Lair.Tests.UI
{
    //# 카드 리뉴얼 v0.6 — BattleHud 좌측 BuildSynergyPanel 의 데이터 표면 회귀.
    //# UI 동적 생성(Image/CHText)은 PlayMode 영역. EditMode 는 VM.GetBuildCount 누적만 검증.
    public class BuildSynergyPanelCountsTests
    {
        private const string CardItemsFolder = "Assets/_Lair/Art/Cards/Items";

        private static CardData LoadCard(string fileName)
        {
            string path = $"{CardItemsFolder}/{fileName}.asset";
            CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
            Assert.IsNotNull(card, $"카드 SO 로드 실패: {path}");
            return card;
        }

        [Test]
        public void 초기_4축_카운트_모두_0()
        {
            BattleStateModel m = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(m);

            Assert.AreEqual(0, vm.GetBuildCount(EBuildAxis.Tank));
            Assert.AreEqual(0, vm.GetBuildCount(EBuildAxis.Dps));
            Assert.AreEqual(0, vm.GetBuildCount(EBuildAxis.Debuff));
            Assert.AreEqual(0, vm.GetBuildCount(EBuildAxis.Swarm));
        }

        [Test]
        public void AddPick_Tank카드_3장_Tank카운트_3()
        {
            BattleStateModel m = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(m);

            //# WispHpBoost (Tank) 3장 픽 — 같은 카드 K번 픽도 시너지 카운트 누적 (T2 정책).
            CardData wisp = LoadCard("WispHpBoost");
            vm.AddPick(wisp, isPassive: true);
            vm.AddPick(wisp, isPassive: true);
            vm.AddPick(wisp, isPassive: true);

            Assert.AreEqual(3, vm.GetBuildCount(EBuildAxis.Tank), "같은 카드 3픽 → Tank 카운트 3");
            Assert.AreEqual(0, vm.GetBuildCount(EBuildAxis.Dps),  "다른 축 침범 없음");
        }

        [Test]
        public void AddPick_4축_분산_각_축_카운트_정합()
        {
            BattleStateModel m = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(m);

            vm.AddPick(LoadCard("WispHpBoost"),       isPassive: true);
            vm.AddPick(LoadCard("ReaperAtkSpeed"),    isPassive: true);
            vm.AddPick(LoadCard("PlagueSlowBoost"),   isPassive: true);
            vm.AddPick(LoadCard("PhantomMoveSpeedBoost"), isPassive: true);
            vm.AddPick(LoadCard("PhantomMoveSpeedBoost"), isPassive: true);

            Assert.AreEqual(1, vm.GetBuildCount(EBuildAxis.Tank));
            Assert.AreEqual(1, vm.GetBuildCount(EBuildAxis.Dps));
            Assert.AreEqual(1, vm.GetBuildCount(EBuildAxis.Debuff));
            Assert.AreEqual(2, vm.GetBuildCount(EBuildAxis.Swarm));
        }

        [Test]
        public void AddPick_OnBuildChanged_매픽마다_발화()
        {
            BattleStateModel m = new BattleStateModel();
            BattleViewModel vm = new BattleViewModel(m);
            int count = 0;
            vm.OnBuildChanged += () => ++count;

            CardData c = LoadCard("WispHpBoost");
            vm.AddPick(c, isPassive: true);
            vm.AddPick(c, isPassive: true);
            vm.AddPick(c, isPassive: true);

            Assert.AreEqual(3, count, "픽 횟수만큼 OnBuildChanged 발화");
        }
    }
}
