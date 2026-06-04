using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    //# [보강] HeroSkillRunner 페이즈 통합 (test-engineer).
    //# 기존 HeroSkillRunnerPlayTests(1) 는 HP89% → Dash 1페이즈 → 몬스터 피격 1케이스만.
    //# 본 스위트는 누적 활성(P1→P1+P2→...)·급락 동시활성·사망 후 정지·풀 재사용 리셋·
    //# Pause(timeScale=0) 무피해를 _active(reflection) 카운트로 검증한다 (Gap 2·3).
    public class HeroSkillRunnerPhasePlayTests
    {
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
            //# Health 를 HeroSkillRunner(RequireComponent) 보다 먼저 add.
            _heroHealth = _hero.AddComponent<Health>();
            _heroHealth.SetMax(1000, true);
            _runner = _hero.AddComponent<HeroSkillRunner>();
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

        //# 3페이즈(90/60/30) 로드아웃 — 스킬은 Dash 3개로 충분(활성 카운트만 검증, 종류 무관).
        private HeroSkillLoadout MakePhaseLoadout()
        {
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, MakeDash());
            AddPhase(loadout, 0.6f, MakeDash());
            AddPhase(loadout, 0.3f, MakeDash());
            return loadout;
        }

        private DashStrikeSkillData MakeDash()
        {
            DashStrikeSkillData dash = ScriptableObject.CreateInstance<DashStrikeSkillData>();
            SetField(dash, "_damage", 10);
            SetField(dash, "_cooldown", 100f);   //# 발동 안 하도록 긴 쿨 — 활성 카운트만 본다.
            SetField(dash, "_dashLength", 5f);
            SetField(dash, "_coneHalfAngle", 45f);
            SetField(dash, "_centroidRadius", 20f);
            return dash;
        }

        private int ActiveCount()
        {
            FieldInfo f = typeof(HeroSkillRunner).GetField("_active", BindingFlags.NonPublic | BindingFlags.Instance);
            IList list = (IList)f.GetValue(_runner);
            return list.Count;
        }

        //# ── Gap 2: 누적 활성 ─────────────────────────────────────────────

        [UnityTest]
        public IEnumerator HP_순차하향_스킬_누적활성()
        {
            _runner.Bind(MakePhaseLoadout());

            //# 100% → 활성 0.
            yield return null;
            Assert.AreEqual(0, ActiveCount(), "100% 면 활성 없음");

            //# 89% → P1 활성(1).
            _heroHealth.TakeDamage(110);
            yield return null;
            Assert.AreEqual(1, ActiveCount(), "90% 돌파 → 1개");

            //# 59% → P1+P2(2).
            _heroHealth.TakeDamage(300);
            yield return null;
            Assert.AreEqual(2, ActiveCount(), "60% 돌파 → 2개 누적");

            //# 29% → P1+P2+P3(3).
            _heroHealth.TakeDamage(300);
            yield return null;
            Assert.AreEqual(3, ActiveCount(), "30% 돌파 → 3개 누적");
        }

        [UnityTest]
        public IEnumerator HP_급락_한프레임에_3개_동시활성()
        {
            _runner.Bind(MakePhaseLoadout());
            yield return null;
            Assert.AreEqual(0, ActiveCount());

            //# 100% → 25% 급락. 한 프레임 Poll 에서 90/60/30 모두 돌파 → 3개.
            _heroHealth.TakeDamage(750);
            yield return null;
            Assert.AreEqual(3, ActiveCount(), "급락 시 누락 페이즈 모두 한 번에 활성");
        }

        //# ── Gap 2: 사망 후 정지 ──────────────────────────────────────────

        [UnityTest]
        public IEnumerator 영웅사망후_Tick_중지_몬스터HP_고정()
        {
            DashStrikeSkillData dash = MakeDash();
            SetField(dash, "_cooldown", 0.01f);   //# 살아있을 땐 발동하도록 짧은 쿨.
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, dash);
            _runner.Bind(loadout);

            GameObject mon = MakeMonster("Mon", new Vector3(0f, 0f, 3f));
            Health mh = mon.GetComponent<Health>();

            //# 89% → Dash 활성, 몇 프레임 피격 발생.
            _heroHealth.TakeDamage(110);
            for (int i = 0; i < 5; ++i)
                yield return null;
            Assert.Less(mh.Current, 1000, "살아있는 동안엔 Dash 가 몬스터 피격");

            //# 영웅 사망 → Update 가드(_health.IsAlive == false)로 Tick 중지.
            _heroHealth.TakeDamage(10000);
            int frozen = mh.Current;
            for (int i = 0; i < 5; ++i)
                yield return null;
            Assert.AreEqual(frozen, mh.Current, "영웅 사망 후 스킬 Tick 중지 — 몬스터 HP 고정");

            CharacterRegistry.UnregisterMonster(mon.transform);
            Object.DestroyImmediate(mon);
        }

        //# ── Gap 2: 풀 재사용 리셋 ────────────────────────────────────────

        [UnityTest]
        public IEnumerator 풀재사용_OnDisable_OnEnable후_게이트리셋_active비움()
        {
            _runner.Bind(MakePhaseLoadout());

            //# 29% 까지 떨어뜨려 3개 활성.
            _heroHealth.TakeDamage(750);
            yield return null;
            Assert.AreEqual(3, ActiveCount(), "사전 조건 — 3개 활성");

            //# 풀 반환 시뮬레이트 — OnDisable → ResetActive(_active clear).
            //# 영웅을 다시 만조 HP 로 되돌려 재폴링 시 즉시 재활성 안 되게 한다.
            _heroHealth.SetMax(1000, true);
            _hero.SetActive(false);
            Assert.AreEqual(0, ActiveCount(), "OnDisable → _active 비워짐");

            //# 풀 Pop 재사용 — OnEnable → _gate.Reset(). 100% 상태라 활성 0 유지.
            _hero.SetActive(true);
            yield return null;
            Assert.AreEqual(0, ActiveCount(), "재사용 직후 100% — 이전 라운드 스킬 잔존 없음");

            //# 게이트가 Reset 되었는지 — 다시 89% 로 떨어뜨려 P1 이 새로 활성되는지.
            _heroHealth.TakeDamage(110);
            yield return null;
            Assert.AreEqual(1, ActiveCount(), "게이트 Reset 확인 — 재하향 시 P1 재활성");
        }

        //# ── Gap 3: Pause(timeScale=0) 무피해 ─────────────────────────────

        [UnityTest]
        public IEnumerator Pause_timeScale0_스킬데미지_없음()
        {
            DashStrikeSkillData dash = MakeDash();
            SetField(dash, "_cooldown", 0.01f);
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, dash);
            _runner.Bind(loadout);

            GameObject mon = MakeMonster("Mon", new Vector3(0f, 0f, 3f));
            Health mh = mon.GetComponent<Health>();

            //# 89% → Dash 활성 1프레임(timeScale=1) — 활성 확보.
            _heroHealth.TakeDamage(110);
            yield return null;
            Assert.AreEqual(1, ActiveCount(), "Dash 활성 사전 조건");

            //# Pause — PauseService 와 동일하게 timeScale=0. deltaTime 0 → 쿨다운 미경과 → 무피해.
            Time.timeScale = 0f;
            int before = mh.Current;
            for (int i = 0; i < 10; ++i)
                yield return null;
            Assert.AreEqual(before, mh.Current, "timeScale 0 동안 스킬 데미지 없음");

            CharacterRegistry.UnregisterMonster(mon.transform);
            Object.DestroyImmediate(mon);
        }

        //# ── Gap 6: OnDeactivate 풀 회수 (인프라 미부팅 — null-safe만) ─────

        [UnityTest]
        public IEnumerator OnDisable_OrbitingBlade활성후_예외없이_정리()
        {
            //# 인프라(CHMPool) 미부팅 → 궤도 블레이드 비주얼 미스폰. OnDeactivate 경로가
            //# null-safe 한지 + _active 가 비워지는지만 검증. 실제 Push 검증은 보류(인프라 필요).
            OrbitingBladeSkillData orbit = ScriptableObject.CreateInstance<OrbitingBladeSkillData>();
            SetField(orbit, "_damage", 15);
            SetField(orbit, "_hitInterval", 0.1f);
            SetField(orbit, "_orbitRadius", 1.4f);
            SetField(orbit, "_bladeSphereRadius", 0.9f);
            SetField(orbit, "_bladeCount", 3);
            SetField(orbit, "_rotationSpeedDeg", 180f);
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, orbit);
            _runner.Bind(loadout);

            _heroHealth.TakeDamage(110);
            for (int i = 0; i < 3; ++i)
                yield return null;
            Assert.AreEqual(1, ActiveCount(), "궤도 블레이드 활성 사전 조건");

            //# 풀 반환 시뮬 — OnDisable → ResetActive → 각 런타임 OnDeactivate.
            Assert.DoesNotThrow(() => _hero.SetActive(false));
            Assert.AreEqual(0, ActiveCount(), "OnDeactivate 후 _active 비워짐");
        }

        //# ── 헬퍼 ─────────────────────────────────────────────────────────

        private GameObject MakeMonster(string name, Vector3 pos)
        {
            GameObject go = new GameObject(name);
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        private static void SetField(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        //# PlayMode asmdef 는 UnityEditor 미참조 → SerializedObject 불가. _phases 에 직접 add.
        private static void AddPhase(HeroSkillLoadout loadout, float frac, HeroSkillData skill)
        {
            FieldInfo phasesField = typeof(HeroSkillLoadout)
                .GetField("_phases", BindingFlags.NonPublic | BindingFlags.Instance);
            IList list = (IList)phasesField.GetValue(loadout);
            list.Add(new HeroSkillLoadout.Phase { HpFraction = frac, Skill = skill });
        }
    }
}
