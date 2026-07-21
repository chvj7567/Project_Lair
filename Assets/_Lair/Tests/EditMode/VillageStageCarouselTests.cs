using NUnit.Framework;
using Lair.Battle;
using Lair.Meta;
using Lair.Village;

namespace Lair.Tests.EditMode
{
    //# 마을 캐러셀 스테이지 이동 로직 (hero-stage-variant §4·§5). VillageViewModel 은 POCO 라 씬 없이 순수 검증.
    //# MetaConfig 는 소울/영주 게이지 전용 — 캐러셀 API 는 미사용이므로 null 주입.
    public class VillageStageCarouselTests
    {
        private static VillageViewModel MakeVm(int selectedStage, int clearedStage)
        {
            MetaProfile profile = new MetaProfile { SelectedStage = selectedStage, ClearedStage = clearedStage };
            return new VillageViewModel(profile, null);
        }

        [Test]
        public void 초기_SelectedStage는_profile값으로_초기화된다()
        {
            VillageViewModel vm = MakeVm(3, 4);
            Assert.AreEqual(3, vm.SelectedStage);
        }

        [Test]
        public void 초기_SelectedStage는_범위밖_profile값을_1_5로_클램프한다()
        {
            Assert.AreEqual(5, MakeVm(9, 5).SelectedStage);  //# 상한 클램프
            Assert.AreEqual(1, MakeVm(0, 0).SelectedStage);  //# 하한 클램프
            Assert.AreEqual(1, MakeVm(-3, 0).SelectedStage); //# 음수 클램프
        }

        [Test]
        public void null_profile이면_SelectedStage는_1로_초기화된다()
        {
            VillageViewModel vm = new VillageViewModel(null, null);
            Assert.AreEqual(1, vm.SelectedStage);
            Assert.AreEqual(0, vm.ClearedStage);
        }

        [Test]
        public void MoveStage_다음으로_이동하면_증가하고_OnStageChanged가_통지된다()
        {
            VillageViewModel vm = MakeVm(2, 5);
            int notified = 0;
            vm.OnStageChanged += () => notified++;

            vm.MoveStage(1);

            Assert.AreEqual(3, vm.SelectedStage);
            Assert.AreEqual(1, notified);
        }

        [Test]
        public void MoveStage_이전으로_이동하면_감소하고_통지된다()
        {
            VillageViewModel vm = MakeVm(4, 5);
            int notified = 0;
            vm.OnStageChanged += () => notified++;

            vm.MoveStage(-1);

            Assert.AreEqual(3, vm.SelectedStage);
            Assert.AreEqual(1, notified);
        }

        [Test]
        public void MoveStage_5에서_다음은_클램프되어_불변이고_통지없다()
        {
            VillageViewModel vm = MakeVm(5, 5);
            int notified = 0;
            vm.OnStageChanged += () => notified++;

            vm.MoveStage(1);

            Assert.AreEqual(5, vm.SelectedStage); //# 상한 클램프 — 불변
            Assert.AreEqual(0, notified);         //# 실변경 없으면 통지 안 함
        }

        [Test]
        public void MoveStage_1에서_이전은_클램프되어_불변이고_통지없다()
        {
            VillageViewModel vm = MakeVm(1, 5);
            int notified = 0;
            vm.OnStageChanged += () => notified++;

            vm.MoveStage(-1);

            Assert.AreEqual(1, vm.SelectedStage);
            Assert.AreEqual(0, notified);
        }

        [Test]
        public void MoveStage_여러칸_delta도_1_5로_클램프된다()
        {
            VillageViewModel vm = MakeVm(3, 5);
            vm.MoveStage(10);
            Assert.AreEqual(5, vm.SelectedStage);
            vm.MoveStage(-10);
            Assert.AreEqual(1, vm.SelectedStage);
        }

        [Test]
        public void CanMovePrev는_스테이지1에서만_false다()
        {
            Assert.IsFalse(MakeVm(1, 5).CanMovePrev);
            Assert.IsTrue(MakeVm(2, 5).CanMovePrev);
            Assert.IsTrue(MakeVm(5, 5).CanMovePrev);
        }

        [Test]
        public void CanMoveNext는_스테이지5에서만_false다()
        {
            Assert.IsTrue(MakeVm(1, 5).CanMoveNext);
            Assert.IsTrue(MakeVm(4, 5).CanMoveNext);
            Assert.IsFalse(MakeVm(5, 5).CanMoveNext);
        }

        [Test]
        public void IsCurrentStageUnlocked는_StageProgress_판정과_일치한다()
        {
            //# ClearedStage=2 → stage3 해금, stage4 잠금. 캐러셀 이동으로 잠긴 스테이지 도달 시 false.
            VillageViewModel vm = MakeVm(3, 2);
            Assert.IsTrue(vm.IsCurrentStageUnlocked);
            Assert.AreEqual(StageProgress.IsUnlocked(vm.SelectedStage, vm.ClearedStage), vm.IsCurrentStageUnlocked);

            vm.MoveStage(1); //# stage 4 — 잠금
            Assert.AreEqual(4, vm.SelectedStage);
            Assert.IsFalse(vm.IsCurrentStageUnlocked);
            Assert.AreEqual(StageProgress.IsUnlocked(vm.SelectedStage, vm.ClearedStage), vm.IsCurrentStageUnlocked);
        }

        [Test]
        public void ClearedStage는_라이브_프로필값을_반영한다()
        {
            VillageViewModel vm = MakeVm(1, 3);
            Assert.AreEqual(3, vm.ClearedStage);
        }
    }
}
