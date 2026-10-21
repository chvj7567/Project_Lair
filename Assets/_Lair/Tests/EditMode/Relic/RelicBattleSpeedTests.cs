using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 유물 2배속 — PauseService 목표 속도 보관/복원 (relics §5.2, §12.6 "PauseService"). Time.timeScale 은 정적이라 SetUp/TearDown 에서 1 로 복원한다.
    public class RelicPauseServiceTests
    {
        private const float Tol = 0.0001f;

        [SetUp]
        public void 시간_배율을_1로_초기화한다()
        {
            Time.timeScale = 1f;
        }

        [TearDown]
        public void 시간_배율을_1로_원복한다()
        {
            Time.timeScale = 1f;
        }

        [Test]
        public void 기본_목표_속도는_1이다()
        {
            PauseService ps = new PauseService();

            Assert.AreEqual(1f, ps.TargetScale, Tol);
            Assert.AreEqual(1f, Time.timeScale, Tol);
        }

        [Test]
        public void 정지_중이_아니면_SetTargetScale이_즉시_timeScale에_반영된다()
        {
            PauseService ps = new PauseService();

            ps.SetTargetScale(2f);

            Assert.AreEqual(2f, ps.TargetScale, Tol);
            Assert.AreEqual(2f, Time.timeScale, Tol);
        }

        [Test]
        public void 배속_2에서_Pause하면_0이고_Resume하면_1이_아니라_2로_복원된다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);

            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, Tol);
            Assert.IsTrue(ps.IsPaused);
            ps.Resume();

            Assert.AreEqual(2f, Time.timeScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void 정지_중_SetTargetScale은_보관만_하고_Resume때_반영된다()
        {
            PauseService ps = new PauseService();
            ps.Pause();

            ps.SetTargetScale(2f);
            Assert.AreEqual(0f, Time.timeScale, Tol, "정지 중에는 timeScale 불변");
            Assert.AreEqual(2f, ps.TargetScale, Tol);
            ps.Resume();

            Assert.AreEqual(2f, Time.timeScale, Tol);
        }

        [Test]
        public void 정지_중_배속을_껐다_켜면_마지막_값이_Resume에_반영된다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);
            ps.Pause();

            ps.SetTargetScale(1f);
            ps.SetTargetScale(2f);
            ps.SetTargetScale(1f);
            ps.Resume();

            Assert.AreEqual(1f, Time.timeScale, Tol);
        }

        [Test]
        public void 중첩_Pause는_마지막_Resume에서만_목표_속도로_복원된다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);

            ps.Pause();
            ps.Pause();
            ps.Pause();
            ps.Resume();
            Assert.AreEqual(0f, Time.timeScale, Tol, "depth 3 → 2");
            ps.Resume();
            Assert.AreEqual(0f, Time.timeScale, Tol, "depth 2 → 1");
            ps.Resume();

            Assert.AreEqual(2f, Time.timeScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void 중첩_Pause_중_SetTargetScale도_마지막_Resume에서만_반영된다()
        {
            PauseService ps = new PauseService();
            ps.Pause();
            ps.Pause();

            ps.SetTargetScale(2f);
            ps.Resume();
            Assert.AreEqual(0f, Time.timeScale, Tol);
            ps.Resume();

            Assert.AreEqual(2f, Time.timeScale, Tol);
        }

        [Test]
        public void Pause_없이_Resume하면_아무것도_바꾸지_않는다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);

            ps.Resume();
            ps.Resume();

            Assert.AreEqual(2f, Time.timeScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void 초과_Resume_뒤의_Pause는_깊이_1부터_다시_센다()
        {
            PauseService ps = new PauseService();
            ps.Resume();
            ps.Pause();
            Assert.AreEqual(0f, Time.timeScale, Tol);

            ps.Resume();

            Assert.AreEqual(1f, Time.timeScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void ResetAll은_배속과_정지를_모두_풀어_timeScale_1_목표_1로_되돌린다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);
            ps.Pause();
            ps.Pause();

            ps.ResetAll();

            Assert.AreEqual(1f, Time.timeScale, Tol);
            Assert.AreEqual(1f, ps.TargetScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void ResetAll_뒤의_Pause_Resume은_옛_배속이_아닌_1로_복원된다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);
            ps.ResetAll();

            ps.Pause();
            ps.Resume();

            Assert.AreEqual(1f, Time.timeScale, Tol, "E-13: 씬 이탈 뒤 배속이 새지 않는다");
        }

        [Test]
        public void ForcePause는_배속_중에도_즉시_0이고_Resume으로는_풀리지_않는다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);

            ps.ForcePause();
            Assert.AreEqual(0f, Time.timeScale, Tol);
            Assert.IsTrue(ps.IsPaused);
            ps.Resume();

            Assert.AreEqual(0f, Time.timeScale, Tol, "결과창 — 강제 정지는 depth 무시");
            Assert.IsTrue(ps.IsPaused);
        }

        [Test]
        public void ForcePause_뒤_ResetAll로만_풀린다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);
            ps.ForcePause();

            ps.ResetAll();

            Assert.AreEqual(1f, Time.timeScale, Tol);
            Assert.IsFalse(ps.IsPaused);
        }

        [Test]
        public void ForcePause_중_SetTargetScale은_보관만_한다()
        {
            PauseService ps = new PauseService();
            ps.ForcePause();

            ps.SetTargetScale(2f);

            Assert.AreEqual(0f, Time.timeScale, Tol);
            Assert.AreEqual(2f, ps.TargetScale, Tol);
        }

        [Test]
        public void 목표_속도는_0_1_미만으로_내려가지_않는다()
        {
            PauseService ps = new PauseService();

            ps.SetTargetScale(0f);
            Assert.AreEqual(0.1f, ps.TargetScale, Tol);
            ps.SetTargetScale(-3f);
            Assert.AreEqual(0.1f, ps.TargetScale, Tol);
            Assert.AreEqual(0.1f, Time.timeScale, Tol);
        }

        [Test]
        public void 배속_복귀_1로_되돌리면_Resume도_1이다()
        {
            PauseService ps = new PauseService();
            ps.SetTargetScale(2f);
            ps.SetTargetScale(1f);

            ps.Pause();
            ps.Resume();

            Assert.AreEqual(1f, Time.timeScale, Tol);
        }
    }

    //# BattleViewModel 2배속 토글 계약 (relics §5.4) — 가드·이벤트·초기화. 프로필 저장은 BattleController 가 구독으로 수행(여기서는 이벤트까지만 검증).
    public class RelicBattleSpeedViewModelTests
    {
        private BattleStateModel _model;
        private BattleViewModel _vm;
        private List<bool> _events;

        [SetUp]
        public void VM과_이벤트_기록기를_만든다()
        {
            _model = new BattleStateModel();
            _vm = new BattleViewModel(_model);
            _events = new List<bool>();
            _vm.OnBattleSpeedChanged += on => _events.Add(on);
        }

        [Test]
        public void 기본값은_미보유_꺼짐이다()
        {
            Assert.IsFalse(_vm.BattleSpeedAvailable);
            Assert.IsFalse(_vm.BattleSpeedOn);
        }

        [Test]
        public void Configure는_보유_여부와_초기_상태를_이벤트_없이_설정한다()
        {
            _vm.ConfigureBattleSpeed(true, true);

            Assert.IsTrue(_vm.BattleSpeedAvailable);
            Assert.IsTrue(_vm.BattleSpeedOn);
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void 미보유면_초기_상태가_켜짐으로_와도_항상_꺼짐이다()
        {
            _vm.ConfigureBattleSpeed(false, true);

            Assert.IsFalse(_vm.BattleSpeedAvailable);
            Assert.IsFalse(_vm.BattleSpeedOn);
        }

        [Test]
        public void 미보유면_토글_입력을_무시한다()
        {
            _vm.ConfigureBattleSpeed(false, false);

            _vm.SetBattleSpeedOn(true);

            Assert.IsFalse(_vm.BattleSpeedOn);
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void 켜고_끄면_상태가_바뀌고_이벤트가_값과_함께_한번씩_발행된다()
        {
            _vm.ConfigureBattleSpeed(true, false);

            _vm.SetBattleSpeedOn(true);
            _vm.SetBattleSpeedOn(false);

            Assert.IsFalse(_vm.BattleSpeedOn);
            CollectionAssert.AreEqual(new[] { true, false }, _events);
        }

        [Test]
        public void 같은_값을_다시_입력하면_이벤트가_발행되지_않는다()
        {
            _vm.ConfigureBattleSpeed(true, true);

            _vm.SetBattleSpeedOn(true);

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void 전투가_끝난_뒤_토글_입력은_무시된다_승리()
        {
            _vm.ConfigureBattleSpeed(true, false);
            _vm.EndBattle(BattleResult.Win);

            _vm.SetBattleSpeedOn(true);

            Assert.IsFalse(_vm.BattleSpeedOn);
            Assert.AreEqual(0, _events.Count, "전투 종료 후 속도 되살림 방지");
        }

        [Test]
        public void 전투가_끝난_뒤_토글_입력은_무시된다_패배_그리고_끄기도_무시()
        {
            _vm.ConfigureBattleSpeed(true, true);
            _vm.EndBattle(BattleResult.Lose);

            _vm.SetBattleSpeedOn(false);

            Assert.IsTrue(_vm.BattleSpeedOn, "상태를 바꾸지 않는다");
            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void 모델_Result가_None이_아니면_EndBattle_경유_없이도_무시된다()
        {
            _vm.ConfigureBattleSpeed(true, false);
            _model.Result = BattleResult.Win;

            _vm.SetBattleSpeedOn(true);

            Assert.AreEqual(0, _events.Count);
        }

        [Test]
        public void 전투_중에는_여러_번_토글해도_매번_통지된다()
        {
            _vm.ConfigureBattleSpeed(true, false);

            for (int i = 0; i < 4; ++i)
            {
                _vm.SetBattleSpeedOn(i % 2 == 0);
            }

            CollectionAssert.AreEqual(new[] { true, false, true, false }, _events);
        }

        [Test]
        public void 재Configure는_토글_상태를_초기화한다_Retry()
        {
            _vm.ConfigureBattleSpeed(true, false);
            _vm.SetBattleSpeedOn(true);

            _vm.ConfigureBattleSpeed(true, false);

            Assert.IsFalse(_vm.BattleSpeedOn);
            Assert.AreEqual(1, _events.Count, "재구성은 이벤트를 만들지 않는다");
        }
    }
}
