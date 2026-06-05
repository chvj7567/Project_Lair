using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Character
{
    //# [보강] HeroSkillRunner.OnSkillUnlocked 이벤트 발행 검증 (test-engineer).
    //# 컷인 트리거의 발화원 — 임계 하향 돌파 시 발행 / 미구독 NRE 없음 / Skill==null 페이즈 미발행.
    //#
    //# 설계 노트:
    //#   - HeroSkillRunner 는 RequireComponent(Health) + Awake 에서 GetComponent<IHealth>() →
    //#     실제 Health MonoBehaviour 를 Runner 보다 먼저 AddComponent. (PlayMode 페이즈 스위트 패턴 동일)
    //#   - EditMode 라 Update 가 프레임 펌핑되지 않음 → 발행 루프가 도는 private Update 를 reflection 으로 1회 호출.
    //#   - 발행 인자는 HeroSkillData 참조 그 자체 → DisplayName 세팅 불필요. no-op 런타임 FakeSkillData 로
    //#     스킬 Tick 의존(몬스터/레지스트리)을 격리.
    public class HeroSkillRunnerUnlockEventTests
    {
        //# 가변 상태 없는 no-op 런타임 — Tick/OnDeactivate 무동작. 격리용.
        private class FakeRuntime : IHeroSkillRuntime
        {
            public void Tick(IHeroSkillContext ctx, float dt) { }
            public void OnDeactivate() { }
        }

        //# HeroSkillData 추상 구현 — CreateRuntime 만. 발행 인자 동일성 단언용 참조.
        private class FakeSkillData : HeroSkillData
        {
            public override IHeroSkillRuntime CreateRuntime() => new FakeRuntime();
        }

        private GameObject _hero;
        private Health _heroHealth;
        private HeroSkillRunner _runner;
        private float _prevTimeScale;

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            _prevTimeScale = Time.timeScale;
            Time.timeScale = 1f;

            _hero = new GameObject("Hero");
            _hero.transform.position = Vector3.zero;
            //# Health 를 RequireComponent 대상보다 먼저 add.
            _heroHealth = _hero.AddComponent<Health>();
            _heroHealth.SetMax(1000, true);
            _runner = _hero.AddComponent<HeroSkillRunner>();
            //# EditMode 의 AddComponent 는 Awake 를 호출하지 않음 → _health/_ctx 미초기화.
            //# private Awake 를 reflection 으로 1회 호출해 PlayMode 라이프사이클을 재현.
            InvokePrivate(_runner, "Awake");
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _prevTimeScale;
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            if (_hero != null)
                Object.DestroyImmediate(_hero);
        }

        private static FakeSkillData MakeSkill()
            => ScriptableObject.CreateInstance<FakeSkillData>();

        //# EditMode 는 Update 가 안 돌므로 private Update 를 reflection 으로 직접 호출.
        private void TickUpdate() => InvokePrivate(_runner, "Update");

        //# private 인스턴스 메서드(Awake/Update) reflection 호출 공통 헬퍼.
        private static void InvokePrivate(HeroSkillRunner runner, string method)
        {
            MethodInfo m = typeof(HeroSkillRunner)
                .GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            m.Invoke(runner, null);
        }

        private HeroSkillLoadout MakeLoadout(params (float frac, HeroSkillData skill)[] phases)
        {
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            FieldInfo phasesField = typeof(HeroSkillLoadout)
                .GetField("_phases", BindingFlags.NonPublic | BindingFlags.Instance);
            System.Collections.IList list = (System.Collections.IList)phasesField.GetValue(loadout);
            foreach ((float frac, HeroSkillData skill) p in phases)
                list.Add(new HeroSkillLoadout.Phase { HpFraction = p.frac, Skill = p.skill });
            return loadout;
        }

        [Test]
        public void 임계_하향돌파시_OnSkillUnlocked_정확한data로_발행()
        {
            FakeSkillData skill = MakeSkill();
            _runner.Bind(MakeLoadout((0.9f, skill)));

            List<HeroSkillData> fired = new();
            _runner.OnSkillUnlocked += d => fired.Add(d);

            //# 100% → 발행 없음.
            TickUpdate();
            Assert.AreEqual(0, fired.Count, "100% 면 발행 없음");

            //# 89% 로 하향 → 90% 페이즈 돌파 → 발행 1회, 인자 = 그 스킬 참조.
            _heroHealth.TakeDamage(110);
            TickUpdate();
            Assert.AreEqual(1, fired.Count, "90% 돌파 시 1회 발행");
            Assert.AreSame(skill, fired[0], "발행 인자는 해금된 스킬 데이터 참조");
        }

        [Test]
        public void 한프레임_2페이즈_동시돌파시_각각_발행()
        {
            FakeSkillData s1 = MakeSkill();
            FakeSkillData s2 = MakeSkill();
            _runner.Bind(MakeLoadout((0.9f, s1), (0.6f, s2)));

            List<HeroSkillData> fired = new();
            _runner.OnSkillUnlocked += d => fired.Add(d);

            //# 100% → 55% 급락 → 한 Poll 에서 90/60 모두 돌파 → 2회 발행.
            _heroHealth.TakeDamage(450);
            TickUpdate();

            Assert.AreEqual(2, fired.Count, "동시 돌파 시 페이즈마다 발행");
            CollectionAssert.AreEquivalent(new HeroSkillData[] { s1, s2 }, fired);
        }

        [Test]
        public void 미구독_null이어도_NRE_없음()
        {
            //# OnSkillUnlocked 구독자 없음 → ?.Invoke 가드로 예외 없어야.
            _runner.Bind(MakeLoadout((0.9f, MakeSkill())));

            _heroHealth.TakeDamage(110);
            Assert.DoesNotThrow(() => TickUpdate(), "미구독 시 발행 경로 NRE 없음");
        }

        [Test]
        public void Skill이_null인_페이즈는_미발행()
        {
            //# data == null 가드 → Add/Invoke 둘 다 건너뜀.
            _runner.Bind(MakeLoadout((0.9f, null)));

            List<HeroSkillData> fired = new();
            _runner.OnSkillUnlocked += d => fired.Add(d);

            _heroHealth.TakeDamage(110);
            TickUpdate();

            Assert.AreEqual(0, fired.Count, "Skill null 페이즈는 해금 발행 없음");
        }

        [Test]
        public void 같은페이즈_재Tick시_중복발행없음()
        {
            FakeSkillData skill = MakeSkill();
            _runner.Bind(MakeLoadout((0.9f, skill)));

            List<HeroSkillData> fired = new();
            _runner.OnSkillUnlocked += d => fired.Add(d);

            _heroHealth.TakeDamage(110);   //# 89%
            TickUpdate();
            Assert.AreEqual(1, fired.Count);

            //# 추가 하향 없이 두 번 더 Tick — 게이트가 이미 활성 페이즈 재발행 안 함.
            TickUpdate();
            _heroHealth.TakeDamage(10);    //# 88%, 같은 페이즈 구간
            TickUpdate();
            Assert.AreEqual(1, fired.Count, "같은 페이즈 재폴링 시 재발행 없음");
        }
    }
}
