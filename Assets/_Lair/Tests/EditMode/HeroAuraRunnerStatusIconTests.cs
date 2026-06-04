using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class HeroAuraRunnerStatusIconTests
    {
        private GameObject _heroGo;
        private HeroAuraRunner _runner;

        //# 테스트용 최소 Aura — IHeroAura + IStatusVisual.
        private class FakeStatusAura : IHeroAura, IStatusVisual
        {
            public ECardId IconCardId => ECardId.TimeStop;
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt) { }
            public void OnDetached(IHealth hero) { }
        }

        [SetUp]
        public void SetUp()
        {
            _heroGo = new GameObject("Hero");
            _heroGo.AddComponent<Health>();
            _runner = _heroGo.AddComponent<HeroAuraRunner>();
        }

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_heroGo);

        //# EditMode 의 AddComponent 는 OnEnable 미발화라 SetActive(false) 로도 OnDisable 이 안 뜬다.
        //# 결정적 트리거를 위해 private OnDisable 을 리플렉션으로 직접 호출한다.
        private void InvokeOnDisable()
        {
            MethodInfo onDisable = typeof(HeroAuraRunner)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable.Invoke(_runner, null);
        }

        [Test]
        public void Attach_상태Aura_OnStatusShown_발행()
        {
            List<ECardId> shown = new List<ECardId>();
            _runner.OnStatusShown += (key, id) => shown.Add(id);

            _runner.Attach(new FakeStatusAura(), 5f);

            Assert.AreEqual(1, shown.Count);
            Assert.AreEqual(ECardId.TimeStop, shown[0]);
        }

        [Test]
        public void OnDisable_상태Aura_OnStatusHidden_발행()
        {
            int hidden = 0;
            _runner.OnStatusHidden += _ => hidden++;
            _runner.Attach(new FakeStatusAura(), 5f);

            InvokeOnDisable();   //# OnDisable 유발

            Assert.AreEqual(1, hidden);
        }
    }
}
