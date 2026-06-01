using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    //# 버그 수정 회귀 박제 — 몬스터 depenetration 폭발(#1) + 공포 포위 도주(#3).
    public class PhysicsAndFleeTests
    {
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
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
        }

        //# Wisp 프리팹 구성 모사 — Dynamic Rigidbody + CapsuleCollider + SimpleMover.
        private GameObject SpawnDynamicMonster(Vector3 pos)
        {
            GameObject go = new GameObject("dyn_monster");
            go.transform.position = pos;
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotation | RigidbodyConstraints.FreezePositionY;
            go.AddComponent<CapsuleCollider>().radius = 0.5f;
            go.AddComponent<SimpleMover>().Speed = 3f;
            _spawned.Add(go);
            return go;
        }

        //# #1 — 두 몬스터를 같은 지점으로 MovePosition 으로 밀어넣어도 분리 속도가 bounded.
        //# maxDepenetrationVelocity 캡(1f) 이 없으면 PhysX 가 멀리 팅겨내 거리가 폭발.
        [UnityTest]
        public IEnumerator 겹친_두_몬스터_분리거리가_bounded()
        {
            GameObject a = SpawnDynamicMonster(new Vector3(0f, 0f, 0f));
            GameObject b = SpawnDynamicMonster(new Vector3(0.05f, 0f, 0f));

            //# 같은 지점으로 계속 밀어 깊은 겹침 재주입.
            a.GetComponent<SimpleMover>().MoveTo(Vector3.zero);
            b.GetComponent<SimpleMover>().MoveTo(Vector3.zero);

            for (int i = 0; i < 30; ++i)
                yield return new WaitForFixedUpdate();

            float separation = Vector3.Distance(a.transform.position, b.transform.position);
            //# 캡(1f) 적용 시 소프트 분리로 콜라이더 지름(~1) 안팎. 폭발이면 수십 단위로 벌어짐.
            Assert.Less(separation, 3f,
                $"분리 거리 bounded (캡 적용). 실제 {separation:F2}");
        }

        //# #1 검증 — 캡이 0 이 아니라 소프트 분리는 유지 (몬스터끼리 겹친 채 굳지 않음).
        [UnityTest]
        public IEnumerator 겹친_두_몬스터_소프트_분리는_유지()
        {
            GameObject a = SpawnDynamicMonster(new Vector3(0f, 0f, 0f));
            GameObject b = SpawnDynamicMonster(new Vector3(0.05f, 0f, 0f));

            for (int i = 0; i < 30; ++i)
                yield return new WaitForFixedUpdate();

            float separation = Vector3.Distance(a.transform.position, b.transform.position);
            Assert.Greater(separation, 0.2f,
                $"소프트 분리 유지 — 캡이 0 이 아니라 부드럽게 밀려남. 실제 {separation:F2}");
        }

        //# 영웅 1명 + 몬스터 N마리 풀스택 셋업.
        private GameObject SpawnHero(Vector3 pos)
        {
            GameObject hero = new GameObject("hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(1000);
            hero.AddComponent<SimpleMover>().Speed = 3f;
            hero.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            hero.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            hero.AddComponent<HeroTargetProvider>();
            hero.AddComponent<AutoCombatAI>();
            _spawned.Add(hero);
            return hero;
        }

        private GameObject SpawnEngagingMonster(Vector3 pos)
        {
            GameObject m = new GameObject("monster");
            m.transform.position = pos;
            Health hp = m.AddComponent<Health>();
            hp.SetMax(1000);
            m.AddComponent<SimpleMover>().Speed = 0.001f;   //# 사실상 정지 — 포위 위치 안정.
            m.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            m.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            m.AddComponent<MonsterTargetProvider>();
            m.AddComponent<AutoCombatAI>();
            _spawned.Add(m);
            //# Engaging 전환 — TryFindNearest/centroid 후보가 되도록.
            CharacterRegistry.SetMonsterEngaging(m.transform, true);
            return m;
        }

        //# #3 — 사방 포위된 영웅이 공포 도주 시 무리 바깥으로 순이동(진동 아님).
        [UnityTest]
        public IEnumerator 포위된_영웅_공포도주_무리바깥으로_빠져나간다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            SpawnEngagingMonster(new Vector3(2, 0, 0));
            SpawnEngagingMonster(new Vector3(-2, 0, 0));
            SpawnEngagingMonster(new Vector3(0, 0, 2));
            SpawnEngagingMonster(new Vector3(0, 0, -2));
            //# 비대칭 한 마리 — centroid 가 +X 쪽으로 살짝 치우쳐 -X 도주 방향 결정.
            SpawnEngagingMonster(new Vector3(2.5f, 0, 0));

            hero.GetComponent<AutoCombatAI>().FleeMode = true;
            yield return null;
            Vector3 startPos = hero.transform.position;

            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float netDisplacement = Vector3.Distance(startPos, hero.transform.position);
            //# centroid 가 +X 치우침 → 영웅은 -X 로 도주. 진동이면 netDisplacement ≈ 0.
            Assert.Greater(netDisplacement, 1.0f,
                $"포위 도주 시 무리 바깥으로 순이동(진동 아님). 실제 {netDisplacement:F2}");
            Assert.Less(hero.transform.position.x, startPos.x,
                "centroid(+X) 반대 방향(-X)으로 도주");
        }

        //# #3 엣지 — 단일 적 도주(포위 아님)도 정상: 적 반대 방향으로 멀어진다.
        [UnityTest]
        public IEnumerator 단일적_공포도주_적반대로_멀어진다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            GameObject monster = SpawnEngagingMonster(new Vector3(2, 0, 0));

            hero.GetComponent<AutoCombatAI>().FleeMode = true;
            yield return null;
            float startDist = Vector3.Distance(hero.transform.position, monster.transform.position);

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float endDist = Vector3.Distance(hero.transform.position, monster.transform.position);
            Assert.Greater(endDist, startDist + 0.5f,
                $"단일 적 도주 — 적과의 거리 증가. {startDist:F2} → {endDist:F2}");
        }
    }
}
