using System.Collections.Generic;
using Lair.Card;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 배틀 HUD 신규 로직 — 기획서 scene-2d-conversion §4.4 · §4.9 · §10.
    public class BattleHudLogicTests
    {
        private static readonly float[] Active = { 30f, 90f, 150f, 210f, 270f };
        private static readonly float[] Passive = { 0.9f, 0.8f, 0.7f, 0.6f, 0.5f, 0.4f, 0.3f, 0.2f, 0.1f };

        private static BattleViewModel NewVm()
        {
            BattleViewModel vm = new BattleViewModel(new BattleStateModel());
            vm.BindTriggerThresholds(Active, Passive);
            return vm;
        }

        [TestCase(0f, 30f, 0f, "0:30")]
        [TestCase(29.5f, 0.5f, 0.983f, "0:01")]
        [TestCase(30f, 60f, 0f, "1:00")]
        [TestCase(60f, 30f, 0.5f, "0:30")]
        [TestCase(269.9f, 0.1f, 0.998f, "0:01")]
        public void 액티브_카운트다운_표_값(float elapsed, float remain, float progress, string text)
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, elapsed);
            Assert.IsTrue(c.HasNext);
            Assert.AreEqual(remain, c.RemainingSeconds, 0.001f);
            Assert.AreEqual(progress, c.Progress, 0.001f);
            Assert.AreEqual(text, BattleViewModel.FormatCountdown(c));
        }

        [Test]
        public void 마지막_임계_이후는_다음_없음_대시다()
        {
            ActiveCountdown c = BattleViewModel.ComputeActiveCountdown(Active, 270f);
            Assert.IsFalse(c.HasNext);
            Assert.AreEqual(0f, c.Progress);
            Assert.AreEqual("—", BattleViewModel.FormatCountdown(c));
        }

        [Test]
        public void 임계가_없으면_다음_없음이다()
        {
            Assert.IsFalse(BattleViewModel.ComputeActiveCountdown(null, 10f).HasNext);
        }

        [Test]
        public void UpdateTimer가_카운트다운_이벤트를_발행한다()
        {
            BattleViewModel vm = NewVm();
            ActiveCountdown got = default;
            vm.OnActiveCountdownChanged += c => got = c;
            vm.UpdateTimer(60f);
            Assert.AreEqual(30f, got.RemainingSeconds, 0.001f);
        }

        [Test]
        public void 트리거_총수는_임계_개수다()
        {
            BattleViewModel vm = NewVm();
            Assert.AreEqual(5, vm.ActiveTriggerTotal);
            Assert.AreEqual(9, vm.PassiveTriggerTotal);
        }

        [Test]
        public void 트리거_인덱스_0은_90퍼센트_눈금을_획득한다()
        {
            BattleViewModel vm = NewVm();
            int fired = -1;
            vm.OnPassiveTickAcquired += i => fired = i;
            vm.MarkPassiveTriggered(0);
            Assert.AreEqual(0, fired);
            Assert.IsTrue(vm.IsPassiveTickAcquired(0));
            Assert.IsFalse(vm.IsPassiveTickAcquired(1));
        }

        [Test]
        public void 한_번에_두_임계를_넘으면_두_눈금_모두_획득()
        {
            BattleViewModel vm = NewVm();
            List<int> fired = new List<int>();
            vm.OnPassiveTickAcquired += fired.Add;
            vm.MarkPassiveTriggered(2);
            vm.MarkPassiveTriggered(3);
            CollectionAssert.AreEqual(new[] { 2, 3 }, fired);
            Assert.IsTrue(vm.IsPassiveTickAcquired(2) && vm.IsPassiveTickAcquired(3));
        }

        [Test]
        public void HP가_회복돼도_획득은_유지된다()
        {
            BattleViewModel vm = NewVm();
            vm.MarkPassiveTriggered(4);
            vm.UpdateHeroHp(1000, 1000);
            Assert.IsTrue(vm.IsPassiveTickAcquired(4));
        }

        [Test]
        public void 범위_밖_인덱스는_무시한다()
        {
            BattleViewModel vm = NewVm();
            Assert.DoesNotThrow(() => vm.MarkPassiveTriggered(99));
            Assert.IsFalse(vm.IsPassiveTickAcquired(-1));
        }

        [Test]
        public void 기사_3단계_표기()
        {
            Assert.AreEqual("기사 · 3단계", BattleViewModel.ComposeHeroTitle("기사", 3));
        }

        [Test]
        public void SetHeroTitle은_조회_속성에_반영된다()
        {
            BattleViewModel vm = NewVm();
            vm.SetHeroTitle("기사 · 1단계");
            Assert.AreEqual("기사 · 1단계", vm.HeroTitle);
        }

        [Test]
        public void 패시브_픽수는_카운트_합이다()
        {
            BattleViewModel vm = NewVm();
            CardData a = ScriptableObject.CreateInstance<CardData>();
            CardData b = ScriptableObject.CreateInstance<CardData>();
            CardData c = ScriptableObject.CreateInstance<CardData>();
            vm.AddPick(a, true);
            vm.AddPick(a, true);
            vm.AddPick(b, true);
            vm.AddPick(c, false);
            Assert.AreEqual(3, vm.PassivePickCount);
            Assert.AreEqual(1, vm.ActivePickCount);
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }
    }

    public class HudLayoutViewModelTests
    {
        private class FakePrefs : IHudLayoutPrefs
        {
            public readonly Dictionary<string, bool> Store = new Dictionary<string, bool>();
            public int Saves;
            public bool LoadCollapsed(string key) => Store.TryGetValue(key, out bool v) && v;
            public void SaveCollapsed(string key, bool collapsed) { Store[key] = collapsed; Saves++; }
        }

        [Test]
        public void 저장값이_없으면_둘_다_펼침이다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            Assert.IsFalse(vm.IsSynergyCollapsed);
            Assert.IsFalse(vm.IsBuildCollapsed);
        }

        [Test]
        public void 저장된_접힘_상태를_불러온다()
        {
            FakePrefs prefs = new FakePrefs();
            prefs.Store[HudLayoutViewModel.SynergyKey] = true;
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            Assert.IsTrue(vm.IsSynergyCollapsed);
            Assert.IsFalse(vm.IsBuildCollapsed);
        }

        [Test]
        public void ToggleSynergy는_상태를_뒤집고_저장하고_이벤트를_1회_낸다()
        {
            FakePrefs prefs = new FakePrefs();
            HudLayoutViewModel vm = new HudLayoutViewModel(prefs);
            int events = 0;
            bool last = false;
            vm.OnSynergyCollapsedChanged += v => { events++; last = v; };
            vm.ToggleSynergy();
            Assert.IsTrue(vm.IsSynergyCollapsed);
            Assert.AreEqual(1, events);
            Assert.IsTrue(last);
            Assert.IsTrue(prefs.Store[HudLayoutViewModel.SynergyKey]);
            Assert.AreEqual(1, prefs.Saves);
        }

        [Test]
        public void 시너지_토글은_빌드_상태를_바꾸지_않는다()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            int buildEvents = 0;
            vm.OnBuildCollapsedChanged += _ => buildEvents++;
            vm.ToggleSynergy();
            Assert.IsFalse(vm.IsBuildCollapsed);
            Assert.AreEqual(0, buildEvents);
        }

        [Test]
        public void ToggleBuild_두_번이면_원래_상태()
        {
            HudLayoutViewModel vm = new HudLayoutViewModel(new FakePrefs());
            vm.ToggleBuild();
            vm.ToggleBuild();
            Assert.IsFalse(vm.IsBuildCollapsed);
        }

        [Test]
        public void PlayerPrefs_구현은_0_1로_저장한다()
        {
            PlayerPrefsHudLayoutPrefs prefs = new PlayerPrefsHudLayoutPrefs();
            const string key = "Lair.Hud.TestKey";
            PlayerPrefs.DeleteKey(key);
            Assert.IsFalse(prefs.LoadCollapsed(key));
            prefs.SaveCollapsed(key, true);
            Assert.AreEqual(1, PlayerPrefs.GetInt(key, 0));
            Assert.IsTrue(prefs.LoadCollapsed(key));
            PlayerPrefs.DeleteKey(key);
        }
    }
}
