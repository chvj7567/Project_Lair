using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# B (영웅 중앙 끌림) 본격 스위트 — plan §6 B #5~#8.
    //# 위치/시간/사이클 관측이 필요해 전부 PlayMode.
    //#
    //# 핵심 판별 — 모든 B 테스트는 live·등록·Engaging 몬스터(타겟)를 동반해야 한다.
    //#   타겟 없으면 no-target 가드(AutoCombatAI.cs:141)가 중앙끌림 분기(:187) 전에 return → 끌림 미발동.
    //#   끌림과 평소 추격을 구분하려면 중앙 방향을 전투/도주 방향과 직교/반대로 배치한다.
    //#
    //# 가정(AddComponent C# 필드 이니셜라이저, 프리팹 오버라이드 없음):
    //#   _centerPullEnabled=false(기본) → 영웅 테스트는 리플렉션으로 true 주입.
    //#   _centerPullInterval=3 / _centerPullDuration=1 / _centerPullDeadzone=3.
    //#   MeleeAttacker.DeferStrike=false(기본) → 스폰게이트 즉시 통과 + IAttackGate 미부착 → 끌림 미게이팅.
    public class CenterPullPlayTests
    {
        private const float Interval = 3f;
        private const float Duration = 1f;
        private const float Deadzone = 3f;

        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        private static void SetField(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        //# 영웅 — 중앙 끌림 게이트(_centerPullEnabled) 를 인자로 받아 주입.
        private GameObject SpawnHero(Vector3 pos, bool centerPullEnabled)
        {
            GameObject hero = new GameObject("cp_hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);
            hero.AddComponent<SimpleMover>().Speed = 3f;
            hero.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            hero.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            hero.AddComponent<HeroTargetProvider>();
            AutoCombatAI ai = hero.AddComponent<AutoCombatAI>();
            SetField(ai, "_centerPullEnabled", centerPullEnabled);
            _spawned.Add(hero);
            return hero;
        }

        //# 위협/타겟 몬스터 — Engaging 등록. 위치 고정(MoveTo 안 씀).
        private GameObject SpawnEngagingMonster(Vector3 pos)
        {
            GameObject m = new GameObject("cp_monster");
            m.transform.position = pos;
            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            CharacterRegistry.RegisterMonster(m.transform, hp);
            CharacterRegistry.SetMonsterEngaging(m.transform, true);
            _spawned.Add(m);
            return m;
        }

        //# ===== B #5 — 영웅: 3초 주기 1초 중앙이동 창 =====

        //# 영웅 (8,0,0) deadzone(3) 밖. 타겟 몬스터 (11,0,0) — 영웅 사거리 밖이라 평소엔 +X 추격.
        //# 창[0,1): 중앙(-X)으로 이동 → x 감소. 비창[1,3): 평소 추격(+X) → x 증가.
        //# OnEnable 에서 타이머 0 리셋 → 활성 직후가 창 시작.
        [UnityTest]
        public IEnumerator 영웅_3초주기_1초중앙창에_중앙으로_이동한다()
        {
            GameObject hero = SpawnHero(new Vector3(8, 0, 0), centerPullEnabled: true);
            SpawnEngagingMonster(new Vector3(11, 0, 0));

            float startX = hero.transform.position.x;

            //# 창[0,1) 중간(~0.5s) 샘플 — 중앙(-X) 이동으로 x 감소 중.
            yield return RunFor(0.5f, hero);
            float xInWindow = hero.transform.position.x;
            Assert.Less(xInWindow, startX - 0.5f,
                $"중앙이동 창[0,1) — 중앙(-X)으로 이동해 x 감소. {startX:F2} → {xInWindow:F2}");

            //# 비창[1,3) 진입까지 진행 후(~2s 시점) 샘플 — 평소 추격(+X) 으로 x 증가(직전 창 끝점 대비).
            yield return RunFor(1.2f, hero);   //# 누적 ~1.7s → 비창.
            float xMidNonWindow = hero.transform.position.x;
            yield return RunFor(0.6f, hero);   //# 누적 ~2.3s → 여전히 비창.
            float xLateNonWindow = hero.transform.position.x;
            Assert.Greater(xLateNonWindow, xMidNonWindow + 0.1f,
                $"비창[1,3) — 평소 추격으로 +X(타겟 11) 방향 이동. {xMidNonWindow:F2} → {xLateNonWindow:F2}");
        }

        //# B #5 보강 — 다음 사이클에서 중앙창이 다시 열린다(주기성).
        //# 한 사이클(3s) 뒤 다음 창에서 또 중앙(-X)으로 끌린다.
        [UnityTest]
        public IEnumerator 영웅_다음사이클에_중앙창이_다시_열린다()
        {
            GameObject hero = SpawnHero(new Vector3(8, 0, 0), centerPullEnabled: true);
            SpawnEngagingMonster(new Vector3(11, 0, 0));

            //# 첫 사이클 비창 끝(~2.9s)까지 진행 — 추격으로 x 가 다시 늘어난 상태.
            yield return RunFor(2.9f, hero);
            float xBeforeNextWindow = hero.transform.position.x;

            //# 다음 사이클 창[3,4) 중간(~3.5s) — 다시 중앙(-X) 이동.
            yield return RunFor(0.6f, hero);
            float xInNextWindow = hero.transform.position.x;

            Assert.Less(xInNextWindow, xBeforeNextWindow - 0.3f,
                $"다음 사이클 창 — 중앙(-X)으로 재차 끌림(주기성). {xBeforeNextWindow:F2} → {xInNextWindow:F2}");
        }

        //# ===== B #6 — deadzone 내에서는 창 미발동 =====

        //# 영웅 (2,0,0) — 중앙거리 2 < deadzone 3. 타겟을 (2,0,5) 직교 배치 →
        //# 끌림이 발동하면 중앙(-X)으로 갈 텐데, deadzone 안이라 발동 안 함 → x≈2 유지(추격은 +Z).
        [UnityTest]
        public IEnumerator deadzone_내에서는_중앙창_미발동()
        {
            GameObject hero = SpawnHero(new Vector3(2, 0, 0), centerPullEnabled: true);
            SpawnEngagingMonster(new Vector3(2, 0, 5));   //# +Z 방향 타겟(직교) — 추격은 z 증가, x 불변.

            float startX = hero.transform.position.x;

            //# 창 구간(~0.5s) 동안 관측 — deadzone 안이라 중앙(-X) 이동 없음.
            yield return RunFor(0.5f, hero);
            float x = hero.transform.position.x;

            //# 끌림 발동이면 x 가 원점(-X)으로 끌려 2 미만으로 떨어짐. deadzone 미발동이면 x≈2(추격은 z축).
            Assert.Greater(x, startX - 0.3f,
                $"deadzone(3) 내(거리2) — 중앙창 미발동 → x 가 중앙(-X)으로 안 끌림. start={startX:F2} now={x:F2}");
        }

        //# ===== B #7 — FleeMode 중 중앙끌림 미발생(도주 우선) =====

        //# 영웅 (8,0,0) deadzone 밖. 위협(타겟) (8,0,2) → 도주는 -Z. 중앙끌림이면 -X.
        //# FleeMode 분기가 끌림보다 앞(:150 vs :187) → 도주(-Z) 만 일어나고 중앙(-X) 끌림은 안 일어남.
        [UnityTest]
        public IEnumerator FleeMode_중에는_중앙끌림_미발생()
        {
            GameObject hero = SpawnHero(new Vector3(8, 0, 0), centerPullEnabled: true);
            SpawnEngagingMonster(new Vector3(8, 0, 2));   //# +Z 위협 → 도주는 -Z.

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.FleeMode = true;

            float startX = hero.transform.position.x;
            float startZ = hero.transform.position.z;

            //# 창 구간(~0.5s) — FleeMode 면 도주(-Z) 만, 중앙(-X) 끌림 없음.
            yield return RunFor(0.5f, hero);
            float x = hero.transform.position.x;
            float z = hero.transform.position.z;

            //# 도주: z 가 -Z 로 감소. 중앙끌림 미발동: x 는 거의 불변(8 근처) — 중앙끌림이 작동했다면 x 가 크게 줄었을 것.
            Assert.Less(z, startZ - 0.3f,
                $"FleeMode — 위협(+Z) 반대인 -Z 로 도주. {startZ:F2} → {z:F2}");
            Assert.Greater(x, startX - 0.5f,
                $"FleeMode 중 중앙(-X) 끌림 미발생 — x 거의 불변. start={startX:F2} now={x:F2}");
        }

        //# ===== B #8 — 몬스터(_centerPullEnabled=false): 동작 불변(회귀) =====

        //# 몬스터는 끌림 게이트 false → deadzone 밖이어도 중앙끌림 분기 진입 안 함.
        //# 몬스터 (8,0,0) + 타겟 영웅 (11,0,0) → 평소 추격(+X) 만. 중앙(-X) 끌림은 절대 없음.
        [UnityTest]
        public IEnumerator 몬스터_중앙끌림_미적용_평소추격만_한다()
        {
            //# 몬스터 풀스택 — MonsterTargetProvider + _centerPullEnabled 기본 false(주입 안 함).
            GameObject monster = new GameObject("regr_monster");
            monster.transform.position = new Vector3(8, 0, 0);
            Health mhp = monster.AddComponent<Health>();
            mhp.SetMax(100000);
            monster.AddComponent<SimpleMover>().Speed = 3f;
            monster.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            monster.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            monster.AddComponent<MonsterTargetProvider>();
            monster.AddComponent<AutoCombatAI>();
            _spawned.Add(monster);

            //# 타겟 영웅 — Heroes 레지스트리에 등록(MonsterTargetProvider 가 검색).
            //# 타겟을 멀리(20) 둬 관측 구간(~1.5s) 내내 사거리 밖 → 교전정지 없이 +X 추격이 단조 유지(엔게이지 x≈18.5).
            GameObject heroTarget = new GameObject("regr_hero_target");
            heroTarget.transform.position = new Vector3(20, 0, 0);
            Health hhp = heroTarget.AddComponent<Health>();
            hhp.SetMax(100000);
            CharacterRegistry.RegisterHero(heroTarget.transform, hhp);
            _spawned.Add(heroTarget);

            float startX = monster.transform.position.x;

            //# 끌림이 영웅에게만 작동하면 몬스터는 항상 +X(타겟20) 추격 — x 증가, 중앙(-X) 으로 절대 안 끌림.
            //# 중앙창[0,1) 구간을 포함해 관측 — 영웅이라면 이 구간에 -X 로 끌렸을 것.
            yield return RunFor(0.5f, monster);
            float xInWouldBeWindow = monster.transform.position.x;
            Assert.Greater(xInWouldBeWindow, startX + 0.1f,
                $"몬스터 — 중앙끌림 미적용. 창 구간에도 +X(타겟20) 추격만. {startX:F2} → {xInWouldBeWindow:F2} " +
                $"(영웅이면 -X 로 끌렸을 구간).");

            //# 계속 추격해 사거리 밖 단조 +X — 중앙(-X) 으로 뒤집히지 않음.
            yield return RunFor(1f, monster);
            Assert.Greater(monster.transform.position.x, xInWouldBeWindow,
                "몬스터 — 사이클 전구간 +X 추격 단조(중앙 끌림으로 뒤집힘 없음).");
        }

        //# B #8 보강 — 몬스터는 도주(FleeMode) 외엔 중앙끌림 무관하게 기존 교전 로직 유지.
        //# deadzone 밖 몬스터가 타겟 사거리에 들어가면 정지·교전(끌림이 끼어들지 않음).
        [UnityTest]
        public IEnumerator 몬스터_사거리_진입시_교전정지한다_회귀()
        {
            GameObject monster = new GameObject("regr_monster2");
            monster.transform.position = new Vector3(5, 0, 0);
            Health mhp = monster.AddComponent<Health>();
            mhp.SetMax(100000);
            SimpleMover mover = monster.AddComponent<SimpleMover>();
            mover.Speed = 3f;
            monster.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            monster.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            monster.AddComponent<MonsterTargetProvider>();
            monster.AddComponent<AutoCombatAI>();
            _spawned.Add(monster);

            //# 타겟 영웅 — 몬스터 바로 옆 사거리 안(0.5).
            GameObject heroTarget = new GameObject("regr_hero_target2");
            heroTarget.transform.position = new Vector3(5.5f, 0, 0);
            Health hhp = heroTarget.AddComponent<Health>();
            hhp.SetMax(100000);
            CharacterRegistry.RegisterHero(heroTarget.transform, hhp);
            _spawned.Add(heroTarget);

            //# 진입 — dist 0.5 <= Range 1.5 → 교전 → Stop. 중앙끌림이 끼면 -X 로 움직였을 것.
            yield return RunFor(0.5f, monster);

            Assert.IsFalse(mover.IsMoving,
                $"몬스터 사거리 안 — 교전(Stop) 유지(중앙끌림 미개입). IsMoving={mover.IsMoving}");
            Assert.Greater(monster.transform.position.x, 5f - 0.3f,
                "몬스터 — 중앙(-X)으로 끌리지 않고 교전 위치(5 근처) 유지.");
        }

        //# 지정 시간만큼 프레임 진행 — 매 프레임 후 대상이 파괴되지 않았는지 가드.
        private static IEnumerator RunFor(float seconds, GameObject guard)
        {
            float elapsed = 0f;
            while (elapsed < seconds && guard != null)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
        }
    }
}
