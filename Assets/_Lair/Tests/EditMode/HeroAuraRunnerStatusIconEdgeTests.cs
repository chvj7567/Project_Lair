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
    //# HeroAuraRunner 상태 아이콘 이벤트 — 엣지/회귀 보강 (test-engineer).
    //# 기본 Attach→OnStatusShown / OnDisable→OnStatusHidden 정상 케이스는
    //# HeroAuraRunnerStatusIconTests 가 이미 커버 — 여기선 망라:
    //#   다중 동시(서로 다른 타입) / 재부착 중복 방지(회귀) / 무기한 상태 자동제거 안 됨.
    public class HeroAuraRunnerStatusIconEdgeTests
    {
        private GameObject _heroGo;
        private HeroAuraRunner _runner;

        //# 키(=aura 타입) 구분을 검증하려면 서로 다른 타입이 필요 — 공통 베이스 + 파생 2종.
        private abstract class FakeStatusAuraBase : IHeroAura, IStatusVisual
        {
            public abstract ECardId IconCardId { get; }
            public void OnAttached(IHealth hero) { }
            public void Tick(IHealth hero, float dt) { }
            public void OnDetached(IHealth hero) { }
        }

        private class FakeSlowAura : FakeStatusAuraBase
        {
            public override ECardId IconCardId => ECardId.Slow;
        }

        private class FakeFearAura : FakeStatusAuraBase
        {
            public override ECardId IconCardId => ECardId.Fear;
        }

        private class FakeBleedAura : FakeStatusAuraBase
        {
            public override ECardId IconCardId => ECardId.Bleed;
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

        private void InvokeUpdate()
        {
            MethodInfo update = typeof(HeroAuraRunner)
                .GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            update.Invoke(_runner, null);
        }

        //# EditMode 의 AddComponent 는 OnEnable 미발화라 SetActive(false) 로도 OnDisable 이 안 뜬다.
        //# 결정적 트리거를 위해 private OnDisable 을 리플렉션으로 직접 호출한다.
        private void InvokeOnDisable()
        {
            MethodInfo onDisable = typeof(HeroAuraRunner)
                .GetMethod("OnDisable", BindingFlags.NonPublic | BindingFlags.Instance);
            onDisable.Invoke(_runner, null);
        }

        //# 1. 다중 동시 상태 — 서로 다른 타입 3개 Attach → OnStatusShown 3번, key/ECardId 구분 발행.
        [Test]
        public void Attach_서로다른타입3개_OnStatusShown_key별로3번발행()
        {
            List<object> keys = new List<object>();
            List<ECardId> ids = new List<ECardId>();
            _runner.OnStatusShown += (key, id) => { keys.Add(key); ids.Add(id); };

            _runner.Attach(new FakeSlowAura(), 5f);
            _runner.Attach(new FakeFearAura(), 5f);
            _runner.Attach(new FakeBleedAura(), 5f);

            Assert.AreEqual(3, keys.Count);
            //# key 는 aura 타입 — 서로 달라야 한다.
            CollectionAssert.AllItemsAreUnique(keys);
            //# ECardId 도 표대로 구분 발행.
            CollectionAssert.AreEquivalent(
                new[] { ECardId.Slow, ECardId.Fear, ECardId.Bleed }, ids);
        }

        //# 1. 다중 동시 후 OnDisable(풀 반환) → 모든 활성 key 가 각각 OnStatusHidden 발행.
        //# (HeroAuraRunner 엔 개별 detach API 가 없고, 만료는 Time.deltaTime 의존이라 EditMode 비결정적 —
        //#  결정적 경로인 OnDisable 로 "다중 상태 전부 정확히 한 번씩 숨김"을 검증한다.)
        [Test]
        public void OnDisable_다중상태_각key당_OnStatusHidden_한번씩()
        {
            List<object> hiddenKeys = new List<object>();
            _runner.OnStatusHidden += key => hiddenKeys.Add(key);

            _runner.Attach(new FakeSlowAura(), 5f);
            _runner.Attach(new FakeFearAura(), 5f);
            _runner.Attach(new FakeBleedAura(), 5f);

            InvokeOnDisable();   //# OnDisable — 풀 반환 경로

            Assert.AreEqual(3, hiddenKeys.Count);
            CollectionAssert.AllItemsAreUnique(hiddenKeys);
            CollectionAssert.AreEquivalent(
                new[] { typeof(FakeSlowAura), typeof(FakeFearAura), typeof(FakeBleedAura) },
                hiddenKeys);
        }

        //# 4(회귀). 같은 타입 재부착(연장) → OnStatusShown 추가 발행 안 됨 (이미 표시 중).
        //# IDistinctHeroAura 가 아닌 plain fake 로 — dedup early-return 경로를 정확히 탄다.
        [Test]
        public void Attach_같은타입재부착_OnStatusShown_추가발행안됨_회귀()
        {
            int shown = 0;
            _runner.OnStatusShown += (key, id) => shown++;

            _runner.Attach(new FakeSlowAura(), 5f);
            _runner.Attach(new FakeSlowAura(), 5f);   //# 같은 타입 — 연장만, 신규 이벤트 X
            _runner.Attach(new FakeSlowAura(), 5f);

            Assert.AreEqual(1, shown);
        }

        //# 3. 무기한 상태(duration<0) — Attach 시 OnStatusShown 발행되고,
        //# Update(Tick) 가 여러 번 돌아도 자동 제거(OnStatusHidden)되지 않는다.
        [Test]
        public void Attach_무기한_OnStatusShown발행_Tick으로자동제거안됨()
        {
            int shown = 0;
            int hidden = 0;
            _runner.OnStatusShown += (key, id) => shown++;
            _runner.OnStatusHidden += key => hidden++;

            _runner.Attach(new FakeBleedAura(), -1f);   //# 무기한

            Assert.AreEqual(1, shown);
            Assert.AreEqual(0, hidden);

            //# Update 를 여러 번 — 무기한은 Remain 감소 분기를 통째로 건너뛴다.
            for (int i = 0; i < 20; i++)
            {
                InvokeUpdate();
            }

            //# 시간 경과로도 숨김 이벤트 발행 안 됨.
            Assert.AreEqual(0, hidden);
        }

        //# 3. 무기한 상태 — 명시 detach(OnDisable, 풀 반환) 시에만 OnStatusHidden 발행.
        [Test]
        public void OnDisable_무기한상태_OnStatusHidden_발행()
        {
            int hidden = 0;
            _runner.OnStatusHidden += key => hidden++;
            _runner.Attach(new FakeBleedAura(), -1f);

            //# Tick 으론 안 사라졌다가(위 테스트), OnDisable 에서 비로소 숨김.
            for (int i = 0; i < 5; i++)
            {
                InvokeUpdate();
            }
            Assert.AreEqual(0, hidden);

            InvokeOnDisable();   //# OnDisable 유발

            Assert.AreEqual(1, hidden);
        }

        //# 3. 실제 무기한 Aura(EternalBleed / HeroAttackDown) 로도 같은 무기한 동작 확인 —
        //# Fake 가 아닌 production aura 의 무기한 경로 회귀.
        [Test]
        public void Attach_실제무기한Aura_OnStatusShown_올바른ECardId()
        {
            List<ECardId> shown = new List<ECardId>();
            _runner.OnStatusShown += (key, id) => shown.Add(id);

            //# 의존성 null — IconCardId 는 상수라 null 안전. duration<0 무기한.
            _runner.Attach(new EternalBleedAura(null), -1f);
            _runner.Attach(new HeroAttackDownAura(null), -1f);

            CollectionAssert.AreEquivalent(
                new[] { ECardId.Bleed, ECardId.HeroAttackDown }, shown);
        }
    }
}
