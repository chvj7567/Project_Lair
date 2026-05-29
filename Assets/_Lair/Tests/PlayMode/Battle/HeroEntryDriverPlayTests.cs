using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Character;

namespace Lair.Tests.PlayMode.Battle
{
    //# BattleZone + HeroEntryDriver + SimpleMover + Health + SimpleRotator 의 통합 시퀀스 검증.
    //# Fake POCO 더블은 GetComponent<I*>() 가 잡지 못하므로 (HeroEntryDriver.Awake 의 의존성),
    //# real 컴포넌트를 그대로 부착한 GameObject 로 구성.
    //# (AutoCombatAIRotationTests.Spawn 패턴 차용.)
    public class HeroEntryDriverPlayTests
    {
        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void Setup()
        {
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        //# BattleZone 셋업 — center, size 입력. spawn points / hero entry 는 옵션.
        private BattleZone CreateZone(Vector3 center, Vector3 size)
        {
            GameObject go = new GameObject("BattleZoneUT");
            go.transform.position = center;
            BoxCollider col = go.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size = size;
            BattleZone zone = go.AddComponent<BattleZone>();
            _spawned.Add(go);
            return zone;
        }

        //# 영웅 GameObject — Health + SimpleMover + SimpleRotator + HeroEntryDriver. AutoCombatAI 미부착.
        private GameObject CreateHero(Vector3 startPos, BattleZone clampZone)
        {
            GameObject go = new GameObject("HeroUT");
            go.transform.position = startPos;
            Health h = go.AddComponent<Health>();
            h.SetMax(1000);
            SimpleMover mover = go.AddComponent<SimpleMover>();
            mover.Speed = 5f;   //# 기획서 §10 기본 영웅 이동 속도 근사.
            mover.BindClampZone(clampZone);
            go.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 540f;
            _spawned.Add(go);
            return go;
        }

        //# 정상 — 영웅이 zone 중심에 도달하면 OnHeroReachedCenter 가 정확히 1회 발행되고
        //# HeroEntryDriver.enabled = false 가 된다 (spec §5.1 단계 3).
        [UnityTest]
        public IEnumerator 영웅_entry_시퀀스_Center도달시_이벤트_1회_및_driver_비활성()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            //# 영웅 — zone 중심 가까이 배치 (도달 임계 0.5m 위). 짧은 시간 안에 도달.
            GameObject hero = CreateHero(new Vector3(2f, 0f, 0f), zone);
            HeroEntryDriver driver = hero.AddComponent<HeroEntryDriver>();
            driver.Bind(zone);

            int eventCount = 0;
            zone.OnHeroReachedCenter += () => eventCount++;

            //# 충분히 진행 — Speed=5 / dist 2m → 약 0.4초.
            float elapsed = 0f;
            while (elapsed < 1.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(1, eventCount,
                "Center 도달 후 OnHeroReachedCenter 정확히 1회");
            Assert.IsFalse(driver.enabled,
                "도달 후 HeroEntryDriver.enabled = false (1회 동작 후 비활성)");

            //# 추가 진행해도 이벤트 재발행 없음.
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.AreEqual(1, eventCount, "추가 시간 진행해도 이벤트 재발행 없음");
        }

        //# 엣지 — 영웅이 entry 중 사망 → SimpleMover.Stop() 호출 + NotifyHeroReachedCenter 미호출.
        //# spec §11.4 안전망.
        [UnityTest]
        public IEnumerator 영웅이_entry중_사망시_NotifyHeroReachedCenter_미호출()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            //# 멀리서 출발 — 도달 전에 사망시키기 위해.
            GameObject hero = CreateHero(new Vector3(20f, 0f, 0f), null);
            HeroEntryDriver driver = hero.AddComponent<HeroEntryDriver>();
            driver.Bind(zone);

            int eventCount = 0;
            zone.OnHeroReachedCenter += () => eventCount++;

            //# 한 프레임 후 사망 — Update 가 IsAlive=false 분기 진입.
            yield return null;
            Health hp = hero.GetComponent<Health>();
            hp.TakeDamage(hp.Current);
            Assert.IsFalse(hp.IsAlive);

            //# 사망 후 1초 진행.
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(0, eventCount,
                "영웅 사망 → driver.Update 가 _mover.Stop 후 return — NotifyHeroReachedCenter 미호출");
            //# IMover.Stop() 이 호출됐는지 — IsMoving=false 검증.
            SimpleMover mover = hero.GetComponent<SimpleMover>();
            Assert.IsFalse(mover.IsMoving, "사망 후 SimpleMover.Stop 호출 → IsMoving=false");
        }

        //# 엣지 — driver.Bind 호출 전(_zone null) Update 는 early return — 아무 동작 없음.
        [UnityTest]
        public IEnumerator driver_Bind_안하면_Update_early_return()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            GameObject hero = CreateHero(new Vector3(2f, 0f, 0f), zone);
            HeroEntryDriver driver = hero.AddComponent<HeroEntryDriver>();
            //# driver.Bind 호출 안 함.

            int eventCount = 0;
            zone.OnHeroReachedCenter += () => eventCount++;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.AreEqual(0, eventCount, "Bind 전엔 Update 가 _zone null 가드로 early return");
            //# 영웅 위치 변동 없음 — _mover.MoveTo 호출 안 됨.
            Assert.AreEqual(2f, hero.transform.position.x, 0.01f);
        }

        //# 통합 — 몬스터 GameObject 가 BattleZone 본체의 BoxCollider 안으로 진입하면
        //# BattleZone.OnTriggerEnter (Unity 물리) 가 발화 → CharacterRegistry.IsEngaging=true 자동 전환.
        //# 실제 Rigidbody + Collider 충돌로 검증.
        [UnityTest]
        public IEnumerator 몬스터가_zone_진입시_IsEngaging_true_자동전환()
        {
            BattleZone zone = CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            //# 몬스터 GameObject — MonsterTag + BoxCollider + Rigidbody(kinematic) + Health.
            GameObject monster = new GameObject("MonsterUT");
            monster.transform.position = new Vector3(20f, 0f, 0f);   //# zone 밖 시작.
            monster.AddComponent<MonsterTag>();
            BoxCollider mc = monster.AddComponent<BoxCollider>();
            mc.size = Vector3.one;
            Rigidbody rb = monster.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.isKinematic = true;   //# kinematic 이라도 trigger 발화는 정상 (단, trigger 쪽이 isTrigger=true).
            Health hp = monster.AddComponent<Health>();
            hp.SetMax(100);
            _spawned.Add(monster);

            CharacterRegistry.RegisterMonster(monster.transform, hp);
            Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging, "초기 Marching");

            //# 한 프레임 yield — Awake/OnEnable 호출 완료.
            yield return null;

            //# 몬스터를 zone 안으로 이동 — kinematic Rigidbody 의 trigger 발화는 MovePosition + SyncTransforms 가 안전.
            //# transform.position 직접 대입은 일부 Unity 버전·물리 스텝 설정에서 누락될 수 있음.
            rb.MovePosition(Vector3.zero);
            Physics.SyncTransforms();

            //# 물리 시뮬 — Unity 가 OnTriggerEnter 발화하려면 FixedUpdate 가 돌아야.
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging,
                "몬스터가 zone 안으로 진입 → OnTriggerEnter 발화 → IsEngaging=true 자동 전환");
        }

        //# 풀 재사용 안전성 — 몬스터를 Engaging 상태로 만든 후 SetActive(false) → SetActive(true).
        //# MonsterTag.OnEnable 이 IsEngaging=false 로 리셋해 Marching 상태 보장.
        //# 풀 Push → Pop 의 핵심 invariant.
        [UnityTest]
        public IEnumerator 풀_Push_Pop_시뮬레이션시_IsEngaging_false_복귀()
        {
            CreateZone(Vector3.zero, new Vector3(10, 1, 10));

            GameObject monster = new GameObject("MonsterUT");
            monster.AddComponent<MonsterTag>();
            Health hp = monster.AddComponent<Health>();
            hp.SetMax(100);
            _spawned.Add(monster);

            //# Register + Engaging 진입 (전 사이클의 잔존 시뮬).
            CharacterRegistry.RegisterMonster(monster.transform, hp);
            CharacterRegistry.SetMonsterEngaging(monster.transform, true);
            Assert.IsTrue(CharacterRegistry.Monsters[0].IsEngaging, "Engaging 진입");

            //# 풀 Push — SetActive(false).
            monster.SetActive(false);
            yield return null;

            //# 풀 Pop — SetActive(true). MonsterTag.OnEnable 이 자동 호출되어 IsEngaging=false 리셋.
            monster.SetActive(true);
            yield return null;

            Assert.IsFalse(CharacterRegistry.Monsters[0].IsEngaging,
                "풀 Pop(SetActive(true)) 후 MonsterTag.OnEnable → IsEngaging=false 자동 리셋");
        }
    }
}
