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
        //# 출격 가능 챕터 수만큼 진행 범위(MaxStage)를 맞춘 실서비스 상태에서 검증하고 끝나면 원복한다.
        private int _prevChapterCount;

        [SetUp]
        public void 챕터_수를_출격_가능_챕터_수로_맞춘다()
        {
            _prevChapterCount = StageProgress.ChapterCount;
            StageProgress.ConfigureChapterCount(ChapterGate.PlayableChapterCount);
        }

        [TearDown]
        public void 챕터_수를_원복한다()
        {
            StageProgress.ConfigureChapterCount(_prevChapterCount);
        }

        //# 캐러셀을 원하는 시작 스테이지에 위치시킨다 — 초기값(최대 플레이 가능)에서 MoveStage(1~출격 가능 상한 클램프)로 이동.
        //# 초기 파생 로직과 무관하게 시작 위치를 고정하므로 이동/경계 테스트는 초기값 변경에 영향받지 않는다.
        private static VillageViewModel MakeVm(int startStage, int clearedStage)
        {
            MetaProfile profile = new MetaProfile { ClearedStage = clearedStage };
            VillageViewModel vm = new VillageViewModel(profile, null);
            vm.MoveStage(startStage - vm.SelectedStage);
            return vm;
        }

        //# 진입 초기 = 최대 플레이 가능 스테이지 = min(ClearedStage+1, 출격 가능 상한) (기획: 마지막 선택이 아니라 최고 도전 지점부터).
        [Test]
        public void 초기_SelectedStage는_최대_플레이가능_스테이지다()
        {
            Assert.AreEqual(1, new VillageViewModel(new MetaProfile { ClearedStage = 0 }, null).SelectedStage);   //# 신규 유저
            Assert.AreEqual(3, new VillageViewModel(new MetaProfile { ClearedStage = 2 }, null).SelectedStage);   //# 2 클리어 → 3
            Assert.AreEqual(5, new VillageViewModel(new MetaProfile { ClearedStage = 4 }, null).SelectedStage);   //# 4 클리어 → 5(1장 보스)
            Assert.AreEqual(6, new VillageViewModel(new MetaProfile { ClearedStage = 5 }, null).SelectedStage);   //# 1장 클리어 → 2-1(챕터 경계 통과)
            Assert.AreEqual(11, new VillageViewModel(new MetaProfile { ClearedStage = 10 }, null).SelectedStage);   //# 2장 클리어 → 3-1(챕터 경계 통과)
            Assert.AreEqual(ChapterGate.PlayableStageCount, new VillageViewModel(new MetaProfile { ClearedStage = ChapterGate.PlayableStageCount }, null).SelectedStage);   //# 전 클리어 → 출격 가능 상한
        }

        //# 저장된 SelectedStage 는 무시된다 — 진입은 항상 최대 플레이 가능으로 시작(출격 desync 방지 계약의 근거).
        [Test]
        public void 초기_SelectedStage는_저장된_SelectedStage를_무시한다()
        {
            MetaProfile profile = new MetaProfile { SelectedStage = 1, ClearedStage = 3 };
            VillageViewModel vm = new VillageViewModel(profile, null);
            Assert.AreEqual(4, vm.SelectedStage);   //# 저장값 1 이 아니라 min(3+1, 상한)=4
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
        public void MoveStage_출격_가능_상한에서_다음은_클램프되어_불변이고_통지없다()
        {
            VillageViewModel vm = MakeVm(ChapterGate.PlayableStageCount, 10);
            int notified = 0;
            vm.OnStageChanged += () => notified++;

            vm.MoveStage(1);

            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage); //# 상한 클램프 — 불변
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
        public void MoveStage_여러칸_delta도_1과_출격_가능_상한으로_클램프된다()
        {
            VillageViewModel vm = MakeVm(3, 10);
            vm.MoveStage(100);
            Assert.AreEqual(ChapterGate.PlayableStageCount, vm.SelectedStage);
            vm.MoveStage(-ChapterGate.PlayableStageCount - 10);
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
        public void CanMoveNext는_출격_가능_상한에서만_false다()
        {
            Assert.IsTrue(MakeVm(1, 10).CanMoveNext);
            Assert.IsTrue(MakeVm(4, 10).CanMoveNext);
            Assert.IsTrue(MakeVm(5, 10).CanMoveNext, "1장 보스에서는 챕터 경계를 넘어 다음(2-1)으로 갈 수 있다");
            Assert.IsFalse(MakeVm(ChapterGate.PlayableStageCount, 10).CanMoveNext);
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
