using System.Collections.Generic;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.UI
{
    //# 배틀 HUD 로직 엣지·경계 — 기획서 scene-2d-conversion §4.3.1 · §4.4 · §4.9 · §4.10 (기본 케이스는 BattleHudLogicTests).
    public class BattleHudLogicEdgeTests
    {
        private static readonly float[] Active = { 30f, 90f, 150f, 210f, 270f };
        private static readonly float[] Passive = { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private static BattleViewModel NewVm()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            vm.BindTriggerThresholds(Active, Passive);
            return vm;
        }

        //# 임계 정각(elapsed == 임계)에서는 그 임계가 이미 지난 것으로 보고 다음 임계(+60s)를 가리킨다.
        [TestCase(30f)]
        [TestCase(90f)]
        [TestCase(150f)]
        [TestCase(210f)]
        public void 임계_정각이면_다음_임계를_가리키고_진행률은_0이다(float elapsed)
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, elapsed);
            Assert.IsTrue(c.HasNext);
            Assert.AreEqual(60f, c.RemainingSeconds, 1e-4f);
            Assert.AreEqual(0f, c.Progress, 1e-4f);
            Assert.AreEqual("1:00", BattleViewModel.FormatCountdown(c));
        }

        //# 임계 직전(0.01s 전)은 같은 구간의 마지막 순간이다.
        [TestCase(29.99f, 30f)]
        [TestCase(89.99f, 90f)]
        [TestCase(149.99f, 150f)]
        [TestCase(209.99f, 210f)]
        [TestCase(269.99f, 270f)]
        public void 임계_직전은_같은_구간이고_진행률이_거의_1이다(float elapsed, float next)
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, elapsed);
            Assert.IsTrue(c.HasNext);
            Assert.AreEqual(next - elapsed, c.RemainingSeconds, 1e-3f);
            Assert.Greater(c.Progress, 0.99f);
            Assert.LessOrEqual(c.Progress, 1f);
        }

        [Test]
        public void 마지막_임계_이후_어떤_시각이어도_다음_없음이다()
        {
            foreach (float t in new[] { 270f, 270.01f, 299.9f, 300f, 1000f })
            {
                ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, t);
                Assert.IsFalse(c.HasNext, "t " + t);
                Assert.AreEqual(0f, c.Progress, "t " + t);
                Assert.AreEqual("—", BattleViewModel.FormatCountdown(c), "t " + t);
            }
        }

        [Test]
        public void 첫_구간은_이전_임계를_0으로_본다()
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, 15f);
            Assert.AreEqual(0.5f, c.Progress, 1e-4f);
            Assert.AreEqual(15f, c.RemainingSeconds, 1e-4f);
        }

        [Test]
        public void 빈_임계_배열이면_다음_없음이다()
        {
            Assert.IsFalse(BattleViewModel.ComputeActiveCountdown(new float[0], 0f).HasNext);
        }

        [Test]
        public void 임계_하나뿐이어도_동작한다()
        {
            float[] one = { 30f };
            Assert.IsTrue(BattleViewModel.ComputeActiveCountdown(one, 10f).HasNext);
            Assert.IsFalse(BattleViewModel.ComputeActiveCountdown(one, 30f).HasNext);
        }

        [Test]
        public void 음수_경과도_첫_임계를_가리키고_진행률은_0으로_클램프된다()
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, -5f);
            Assert.IsTrue(c.HasNext);
            Assert.AreEqual(35f, c.RemainingSeconds, 1e-4f);
            Assert.AreEqual(0f, c.Progress, 1e-6f);
        }

        [TestCase(60f, "1:00")]
        [TestCase(59.99f, "1:00")]
        [TestCase(59f, "0:59")]
        [TestCase(61f, "1:01")]
        [TestCase(60.01f, "1:01")]
        [TestCase(120f, "2:00")]
        [TestCase(0.001f, "0:01")]
        [TestCase(30f, "0:30")]
        [TestCase(9f, "0:09")]
        [TestCase(1f, "0:01")]
        public void 남은_초는_올림_m_ss로_표기하고_0_60은_나오지_않는다(float remain, string text)
        {
            ActiveCountdown c = new ActiveCountdown { HasNext = true, RemainingSeconds = remain, Progress = 0f };
            Assert.AreEqual(text, BattleViewModel.FormatCountdown(c));
        }

        [Test]
        public void 어떤_남은_초도_0_60_형태로_표기되지_않는다()
        {
            for (float r = 0.05f; r <= 125f; r += 0.37f)
            {
                ActiveCountdown c = new ActiveCountdown { HasNext = true, RemainingSeconds = r };
                string s = BattleViewModel.FormatCountdown(c);
                StringAssert.DoesNotMatch(@"^\d+:(6\d|[7-9]\d)$", s, "r " + r);
            }
        }

        [Test]
        public void 다음_없으면_남은_초와_무관하게_대시다()
        {
            ActiveCountdown c = new ActiveCountdown { HasNext = false, RemainingSeconds = 99f };
            Assert.AreEqual("—", BattleViewModel.FormatCountdown(c));
        }

        [Test]
        public void 임계_정각을_넘기는_순간_이벤트_값이_다음_구간으로_넘어간다()
        {
            BattleViewModel vm = NewVm();
            List<ActiveCountdown> got = new List<ActiveCountdown>();
            vm.OnActiveCountdownChanged += got.Add;
            vm.UpdateTimer(29.99f);
            vm.UpdateTimer(30f);
            Assert.AreEqual(2, got.Count);
            Assert.AreEqual(0.01f, got[0].RemainingSeconds, 1e-3f);
            Assert.AreEqual(60f, got[1].RemainingSeconds, 1e-4f);
            Assert.AreEqual("1:00", BattleViewModel.FormatCountdown(got[1]));
        }

        [Test]
        public void 임계를_바인딩하지_않으면_카운트다운은_다음_없음이다()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            ActiveCountdown got = new ActiveCountdown { HasNext = true };
            vm.OnActiveCountdownChanged += c => got = c;
            vm.UpdateTimer(10f);
            Assert.IsFalse(got.HasNext);
        }

        //# --- 패시브 눈금 ---

        [Test]
        public void 임계_9개_모두_순서대로_획득할_수_있다()
        {
            BattleViewModel vm = NewVm();
            List<int> fired = new List<int>();
            vm.OnPassiveTickAcquired += fired.Add;
            for (int i = 0; i < 9; i++)
            {
                vm.MarkPassiveTriggered(i);
            }
            Assert.AreEqual(9, fired.Count);
            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(vm.IsPassiveTickAcquired(i), "tick " + i);
            }
        }

        [Test]
        public void 획득은_HP가_임계_위로_회복돼도_유지된다_여러_번_회복()
        {
            BattleViewModel vm = NewVm();
            vm.UpdateHeroHp(850, 1000);
            vm.MarkPassiveTriggered(0);
            vm.UpdateHeroHp(1000, 1000);
            vm.UpdateHeroHp(950, 1000);
            vm.UpdateHeroHp(1000, 1000);
            Assert.IsTrue(vm.IsPassiveTickAcquired(0));
            Assert.IsFalse(vm.IsPassiveTickAcquired(1));
        }

        [Test]
        public void HP가_임계_아래여도_발화_기록이_없으면_획득이_아니다()
        {
            BattleViewModel vm = NewVm();
            vm.UpdateHeroHp(100, 1000);
            for (int i = 0; i < 9; i++)
            {
                Assert.IsFalse(vm.IsPassiveTickAcquired(i), "tick " + i);
            }
        }

        [Test]
        public void 임계를_다시_바인딩하면_획득_기록이_초기화된다()
        {
            BattleViewModel vm = NewVm();
            vm.MarkPassiveTriggered(3);
            vm.BindTriggerThresholds(Active, Passive);
            Assert.IsFalse(vm.IsPassiveTickAcquired(3));
        }

        [Test]
        public void 음수와_초과_인덱스는_이벤트를_내지_않는다()
        {
            BattleViewModel vm = NewVm();
            int events = 0;
            vm.OnPassiveTickAcquired += _ => events++;
            vm.MarkPassiveTriggered(-1);
            vm.MarkPassiveTriggered(9);
            Assert.AreEqual(0, events);
            Assert.IsFalse(vm.IsPassiveTickAcquired(9));
        }

        [Test]
        public void 임계_바인딩_전에는_눈금_총수가_0이다()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            Assert.AreEqual(0, vm.ActiveTriggerTotal);
            Assert.AreEqual(0, vm.PassiveTriggerTotal);
            Assert.DoesNotThrow(() => vm.MarkPassiveTriggered(0));
        }

        [Test]
        public void null로_바인딩해도_총수가_0이다()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            vm.BindTriggerThresholds(null, null);
            Assert.AreEqual(0, vm.ActiveTriggerTotal);
            Assert.AreEqual(0, vm.PassiveTriggerTotal);
        }

        //# --- 타이머 경고색(≤ 30s) ---

        [TestCase(269.99f, 300f, false)]
        [TestCase(270f, 300f, true)]
        [TestCase(270.01f, 300f, true)]
        [TestCase(300f, 300f, true)]
        [TestCase(0f, 300f, false)]
        [TestCase(0f, 0f, false)]
        [TestCase(10f, -1f, false)]
        public void 남은_30초_이하면_타이머_경고(float elapsed, float total, bool expected)
        {
            Assert.AreEqual(expected, BattleViewModel.IsTimerWarning(elapsed, total));
        }

        [Test]
        public void 경고_임계_상수는_30초다()
        {
            Assert.AreEqual(30f, BattleViewModel.TimerWarnRemainSeconds);
        }

        //# --- 영웅 제목 ---

        [Test]
        public void 영웅_제목은_이름_가운뎃점_단계_형식이다()
        {
            Assert.AreEqual("기사 · 1단계", BattleViewModel.ComposeHeroTitle("기사", 1));
            Assert.AreEqual("기사 · 5단계", BattleViewModel.ComposeHeroTitle("기사", 5));
        }

        [Test]
        public void 영웅_제목_초기값은_비어있다()
        {
            BattleViewModel vm = NewVm();
            Assert.IsTrue(string.IsNullOrEmpty(vm.HeroTitle));
        }

        [Test]
        public void SetHeroTitle을_다시_부르면_덮어쓴다()
        {
            BattleViewModel vm = NewVm();
            vm.SetHeroTitle("기사 · 1단계");
            vm.SetHeroTitle("기사 · 2단계");
            Assert.AreEqual("기사 · 2단계", vm.HeroTitle);
        }

        //# --- 보스 바 잔상 ---

        [Test]
        public void 잔상_경과가_음수면_시작값이다()
        {
            Assert.AreEqual(0.8f, BossHpBarView.EvaluateLag(0.8f, 0.4f, -1f, 0.7f), 1e-6f);
        }

        [Test]
        public void 잔상_시간이_0이하면_즉시_목표다()
        {
            Assert.AreEqual(0.4f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 0.1f, 0f), 1e-6f);
            Assert.AreEqual(0.4f, BossHpBarView.EvaluateLag(0.8f, 0.4f, 0.1f, -1f), 1e-6f);
        }

        [Test]
        public void 시작과_목표가_같으면_값이_그대로다()
        {
            Assert.AreEqual(0.5f, BossHpBarView.EvaluateLag(0.5f, 0.5f, 0.3f, 0.7f), 1e-6f);
        }

        [Test]
        public void 잔상은_시간에_대해_단조_감소한다()
        {
            float prev = 1f;
            for (float t = 0f; t <= 0.7f; t += 0.05f)
            {
                float v = BossHpBarView.EvaluateLag(1f, 0.2f, t, 0.7f);
                Assert.LessOrEqual(v, prev + 1e-6f);
                prev = v;
            }
        }

        [Test]
        public void 잔상은_목표_아래로_내려가지_않는다()
        {
            Assert.AreEqual(0.2f, BossHpBarView.EvaluateLag(1f, 0.2f, 100f, 0.7f), 1e-6f);
        }
    }

    //# 패널 접힘 ViewModel 엣지 — 기획서 §4.10 (기본 케이스는 HudLayoutViewModelTests). 가짜 prefs 사용 — PlayerPrefs 부작용 없음.
    public class HudLayoutViewModelEdgeTests
    {
        private class FakePrefs : IHudLayoutPrefs
        {
            public readonly Dictionary<string, bool> Store = new Dictionary<string, bool>();
            public readonly List<string> SavedKeys = new List<string>();
            public bool LoadCollapsed(string key) => Store.TryGetValue(key, out bool v) && v;
            public void SaveCollapsed(string key, bool collapsed)
            {
                Store[key] = collapsed;
                SavedKeys.Add(key);
            }
        }

        [Test]
        public void 키_문자열은_기획서_값이다()
        {
            Assert.AreEqual("Lair.Hud.SynergyCollapsed", HudLayoutViewModel.SynergyKey);
            Assert.AreEqual("Lair.Hud.BuildCollapsed", HudLayoutViewModel.BuildKey);
        }

        [Test]
        public void 생성_시_저장하지_않는다()
        {
            FakePrefs prefs = new FakePrefs();
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            Assert.AreEqual(0, prefs.SavedKeys.Count);
            Assert.IsFalse(vm.IsSynergyCollapsed);
        }

        [Test]
        public void 두_패널은_각자의_저장값을_불러온다()
        {
            FakePrefs prefs = new FakePrefs();
            prefs.Store[HudLayoutViewModel.SynergyKey] = false;
            prefs.Store[HudLayoutViewModel.BuildKey] = true;
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            Assert.IsFalse(vm.IsSynergyCollapsed);
            Assert.IsTrue(vm.IsBuildCollapsed);
        }

        [Test]
        public void ToggleBuild는_빌드_키로_저장하고_시너지는_그대로다()
        {
            FakePrefs prefs = new FakePrefs();
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            vm.ToggleBuild();
            CollectionAssert.AreEqual(new[] { HudLayoutViewModel.BuildKey }, prefs.SavedKeys);
            Assert.IsTrue(prefs.Store[HudLayoutViewModel.BuildKey]);
            Assert.IsFalse(vm.IsSynergyCollapsed);
        }

        [Test]
        public void ToggleBuild는_이벤트를_1회_내고_값은_새_상태다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            List<bool> got = new List<bool>();
            vm.OnBuildCollapsedChanged += got.Add;
            vm.ToggleBuild();
            CollectionAssert.AreEqual(new[] { true }, got);
        }

        [Test]
        public void 연속_토글은_매번_뒤집고_매번_저장하고_매번_통지한다()
        {
            FakePrefs prefs = new FakePrefs();
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            List<bool> got = new List<bool>();
            vm.OnSynergyCollapsedChanged += got.Add;
            vm.ToggleSynergy();
            vm.ToggleSynergy();
            vm.ToggleSynergy();
            CollectionAssert.AreEqual(new[] { true, false, true }, got);
            Assert.AreEqual(3, prefs.SavedKeys.Count);
            Assert.IsTrue(prefs.Store[HudLayoutViewModel.SynergyKey]);
        }

        [Test]
        public void 저장된_접힘에서_토글하면_펼침이_저장된다()
        {
            FakePrefs prefs = new FakePrefs();
            prefs.Store[HudLayoutViewModel.SynergyKey] = true;
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            vm.ToggleSynergy();
            Assert.IsFalse(vm.IsSynergyCollapsed);
            Assert.IsFalse(prefs.Store[HudLayoutViewModel.SynergyKey]);
        }

        [Test]
        public void 빌드_토글은_시너지_이벤트를_내지_않는다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            int synergyEvents = 0;
            vm.OnSynergyCollapsedChanged += _ => synergyEvents++;
            vm.ToggleBuild();
            Assert.AreEqual(0, synergyEvents);
            Assert.IsFalse(vm.IsSynergyCollapsed);
        }

        [Test]
        public void 두_패널을_모두_접을_수_있다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            vm.ToggleSynergy();
            vm.ToggleBuild();
            Assert.IsTrue(vm.IsSynergyCollapsed);
            Assert.IsTrue(vm.IsBuildCollapsed);
        }

        [Test]
        public void 새_인스턴스가_이전_저장값을_이어받는다()
        {
            FakePrefs prefs = new FakePrefs();
            HudLayoutViewModel first = new HudLayoutViewModel(prefs);
            first.ToggleSynergy();
            HudLayoutViewModel second = new HudLayoutViewModel(prefs);
            Assert.IsTrue(second.IsSynergyCollapsed);
            Assert.IsFalse(second.IsBuildCollapsed);
        }

        [Test]
        public void prefs가_null이어도_토글은_예외없이_동작한다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(null);
            Assert.IsFalse(vm.IsSynergyCollapsed);
            Assert.DoesNotThrow(() => vm.ToggleSynergy());
            Assert.IsTrue(vm.IsSynergyCollapsed);
        }

        [Test]
        public void 구독자가_없어도_토글은_예외가_없다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            Assert.DoesNotThrow(() => vm.ToggleBuild());
        }
    }
}
