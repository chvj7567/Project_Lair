using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    public class HeroSkillRunnerPlayTests
    {
        [UnityTest]
        public IEnumerator HP90이하_Dash활성_부채꼴몬스터_피격()
        {
            GameObject hero = new GameObject("Hero");
            hero.transform.position = Vector3.zero;
            Health hh = hero.AddComponent<Health>();
            hh.SetMax(1000, true);
            HeroSkillRunner runner = hero.AddComponent<HeroSkillRunner>();

            //# 로드아웃 — Dash 1페이즈(90%). 쿨다운 0 → 활성 직후 첫 Tick 에서 결정적 발동
            //# (wall-clock 프레임 dt 합에 의존하지 않음). DashStrikeRuntime: _cooldownRemain=0 → 첫 Tick -=dt <0.
            DashStrikeSkillData dash = ScriptableObject.CreateInstance<DashStrikeSkillData>();
            SetField(dash, "_damage", 50);
            SetField(dash, "_cooldown", 0f);
            SetField(dash, "_dashLength", 10f);
            SetField(dash, "_coneHalfAngle", 45f);
            SetField(dash, "_centroidRadius", 20f);
            HeroSkillLoadout loadout = ScriptableObject.CreateInstance<HeroSkillLoadout>();
            AddPhase(loadout, 0.9f, dash);
            runner.Bind(loadout);

            //# 부채꼴(정면) 안 몬스터.
            GameObject mon = new GameObject("Mon");
            mon.transform.position = new Vector3(0f, 0f, 5f);
            Health mh = mon.AddComponent<Health>();
            mh.SetMax(1000, true);
            CharacterRegistry.RegisterMonster(mon.transform, mh);
            CharacterRegistry.SetMonsterEngaging(mon.transform, true);

            //# HP 89% 로 하락 → Dash 페이즈 활성.
            hh.TakeDamage(110);
            int before = mh.Current;

            //# 몇 프레임 진행 — 활성 폴링 + 첫 Tick 발동 여유. 쿨다운 0 이라 dt 크기 무관 결정적.
            for (int i = 0; i < 10; ++i)
                yield return null;

            Assert.Less(mh.Current, before, "Dash 가 부채꼴 안 몬스터에 데미지를 줘야 한다");

            CharacterRegistry.UnregisterMonster(mon.transform);
            Object.DestroyImmediate(mon);
            Object.DestroyImmediate(hero);
        }

        private static void SetField(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        //# PlayMode asmdef 는 UnityEditor 미참조 → SerializedObject 사용 불가.
        //# 비공개 _phases(List<Phase>) 에 reflection 으로 Phase 인스턴스를 직접 add.
        private static void AddPhase(HeroSkillLoadout loadout, float frac, HeroSkillData skill)
        {
            FieldInfo phasesField = typeof(HeroSkillLoadout)
                .GetField("_phases", BindingFlags.NonPublic | BindingFlags.Instance);
            IList list = (IList)phasesField.GetValue(loadout);

            HeroSkillLoadout.Phase phase = new HeroSkillLoadout.Phase
            {
                HpFraction = frac,
                Skill = skill
            };
            list.Add(phase);
        }
    }
}
