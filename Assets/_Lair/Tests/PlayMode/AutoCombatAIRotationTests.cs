using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    //# 기획서 §7 권장 케이스 — AutoCombatAI 상태별 회전 통합 검증.
    //# Real 컴포넌트 (Health, SimpleMover, MeleeAttacker, SimpleRotator) 조합 + CharacterRegistry.
    public class AutoCombatAIRotationTests
    {
        private const float YawTolerance = 2.0f;
        private const float HighSpeed = 1080f;   //# 통합 테스트 — 빠르게 정렬되도록 충분한 회전 속도.

        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            //# CharacterRegistry 는 static — 이전 테스트 잔존 방지.
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        private static float YawDelta(Quaternion rot, float expectedYaw)
        {
            return Quaternion.Angle(rot, Quaternion.Euler(0f, expectedYaw, 0f));
        }

        //# 영웅 1명 + 몬스터 1마리 셋업. 둘 다 real 컴포넌트로 풀스택 구성.
        //# range / 위치를 통해 "사정거리 안" / "사정거리 밖" 시나리오 선택.
        private (GameObject hero, GameObject monster) Spawn(
            Vector3 heroPos, Vector3 monsterPos, float monsterRange = 1.5f)
        {
            var hero = new GameObject("hero");
            hero.transform.position = heroPos;
            var heroHp = hero.AddComponent<Health>();
            heroHp.SetMax(1000);
            hero.AddComponent<SimpleMover>();
            hero.AddComponent<MeleeAttacker>().Configure(1.5f, 1.0f, 1);
            hero.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = HighSpeed;
            hero.AddComponent<HeroTargetProvider>();
            hero.AddComponent<AutoCombatAI>();
            _spawned.Add(hero);

            var monster = new GameObject("monster");
            monster.transform.position = monsterPos;
            var monHp = monster.AddComponent<Health>();
            monHp.SetMax(1000);
            monster.AddComponent<SimpleMover>().Speed = 0.001f;   //# 사실상 정지 — 위치 안정.
            monster.AddComponent<MeleeAttacker>().Configure(monsterRange, 1.0f, 1);
            monster.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = HighSpeed;
            monster.AddComponent<MonsterTargetProvider>();
            monster.AddComponent<AutoCombatAI>();
            _spawned.Add(monster);

            return (hero, monster);
        }

        [UnityTest]
        public IEnumerator 공격_중_타겟_방향으로_회전한다()
        {
            //# 몬스터(0,0,0) 가 영웅(+X 방향, range 안)을 사정거리 안에서 공격.
            //# 기대: 몬스터의 yaw 가 +X 방향(90°) 정렬.
            var (hero, monster) = Spawn(
                heroPos: new Vector3(1f, 0f, 0f),
                monsterPos: Vector3.zero,
                monsterRange: 2f);   //# range > 1 → 즉시 공격 모드.

            //# 충분히 회전·정렬 시간.
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(YawDelta(monster.transform.rotation, 90f), YawTolerance,
                "사정거리 안 — Attacking 상태에서 +X 영웅 방향(yaw 90°)으로 정렬");
        }

        [UnityTest]
        public IEnumerator 이동_중_이동_방향으로_회전한다()
        {
            //# 몬스터가 영웅(0,0,5) 을 추적. range 1.5 << 거리 5 → Moving 상태.
            //# 기대: 몬스터 yaw 가 +Z 방향(0°) 정렬.
            //# 영웅 이동 차단을 위해 SimpleMover.Speed 도 0 으로.
            var (hero, monster) = Spawn(
                heroPos: new Vector3(0f, 0f, 5f),
                monsterPos: Vector3.zero,
                monsterRange: 1.5f);

            //# 영웅도 거의 정지 — 위치 흔들림 차단.
            hero.GetComponent<SimpleMover>().Speed = 0.001f;
            //# 몬스터 이동 속도도 매우 작게 — 위치 변화 거의 없도록.
            //# (회전만 검증, 거리 유지 위함.)
            monster.GetComponent<SimpleMover>().Speed = 0.01f;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(YawDelta(monster.transform.rotation, 0f), YawTolerance,
                "사정거리 밖 — Moving 상태에서 +Z 영웅 방향(yaw 0°)으로 정렬");
        }

        [UnityTest]
        public IEnumerator 타겟_없을_때_마지막_방향을_유지한다()
        {
            //# 기획서 §7 #3 — "타겟이 있다가 사라진 상황" 에서 마지막 yaw 유지.
            //# 시나리오: 몬스터가 영웅을 향해 회전 완료 → 영웅 제거 → 이후 yaw 변화 없어야.
            var (hero, monster) = Spawn(
                heroPos: new Vector3(1f, 0f, 0f),
                monsterPos: Vector3.zero,
                monsterRange: 2f);

            //# 1초 동안 정렬 — 몬스터 yaw 가 +X(90°) 로 수렴.
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.Less(YawDelta(monster.transform.rotation, 90f), YawTolerance,
                "사전 정렬: 영웅이 있는 동안 몬스터 yaw 90° 수렴");

            //# 영웅 제거 — Heroes 레지스트리에서도 빠짐 (HeroTargetProvider.OnDisable).
            float yawAtHeroLoss = monster.transform.eulerAngles.y;
            Object.DestroyImmediate(hero);
            _spawned.Remove(hero);

            //# 이후 1초간 yaw 변화 없어야 — TryFindNearest false → FaceDirection 호출 안 옴.
            elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(yawAtHeroLoss, monster.transform.eulerAngles.y)),
                YawTolerance, "타겟 소멸 후 마지막 yaw 유지 (회전 명령 X)");
        }

        [UnityTest]
        public IEnumerator 풀_재사용_시_초기_방향으로_스냅된다()
        {
            //# 몬스터를 (3, 0, 0) 에 스폰 → 비활성 → yaw 외부 변형 (90°) → 재활성.
            //# AutoCombatAI.OnEnable 의 SnapToDirection(Vector3.zero - (3,0,0)) = (-X)
            //# → yaw 즉시 270° 로 스냅 (90° 의 이전 잔존이 사라져야).
            var monster = new GameObject("recycled_monster");
            monster.transform.position = new Vector3(3f, 0f, 0f);
            var hp = monster.AddComponent<Health>();
            hp.SetMax(1000);
            monster.AddComponent<SimpleMover>().Speed = 0.001f;
            monster.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            monster.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = HighSpeed;
            monster.AddComponent<MonsterTargetProvider>();
            monster.AddComponent<AutoCombatAI>();
            _spawned.Add(monster);

            //# 첫 OnEnable — 270° 스냅 완료.
            yield return null;

            //# 풀 Push 모사 — 비활성화.
            monster.SetActive(false);
            yield return null;

            //# 외부에서 yaw 90° 로 강제 (잔존 시뮬레이션).
            monster.transform.rotation = Quaternion.Euler(0f, 90f, 0f);
            //# 위치 유지 — (3, 0, 0).

            //# 풀 Pop 모사 — 재활성화.
            monster.SetActive(true);
            yield return null;

            //# OnEnable 의 SnapToDirection 이 즉시 270° 로 덮어써야 함.
            Assert.Less(YawDelta(monster.transform.rotation, 270f), YawTolerance,
                "재활성화 시 AutoCombatAI.OnEnable 의 SnapToDirection 으로 ring 중심 방향(270°) 즉시 적용");
        }

        [UnityTest]
        public IEnumerator 사망_후_회전하지_않는다()
        {
            //# 영웅 + 몬스터 셋업 → 몬스터를 충전 회전시킨 후 사망 → 영웅 위치를 옮겨도 회전 X.
            var (hero, monster) = Spawn(
                heroPos: new Vector3(1f, 0f, 0f),
                monsterPos: Vector3.zero,
                monsterRange: 2f);

            //# 1초 — yaw 90° 로 정렬.
            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# 몬스터 사망.
            var monHp = monster.GetComponent<Health>();
            monHp.TakeDamage(monHp.Current);
            Assert.IsFalse(monHp.IsAlive);

            float yawAtDeath = monster.transform.eulerAngles.y;

            //# 영웅을 -X 방향으로 옮김 — 살아있었다면 회전했어야 할 위치.
            hero.transform.position = new Vector3(-1f, 0f, 0f);

            //# 시간 진행.
            elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(Mathf.Abs(Mathf.DeltaAngle(yawAtDeath, monster.transform.eulerAngles.y)),
                YawTolerance, "사망 후 영웅 위치 변경에도 yaw 변화 없음 (회전 명령 안 옴)");
        }

        [UnityTest]
        public IEnumerator 도주_모드에서_적의_반대_방향을_본다()
        {
            //# 보충 케이스 — FleeMode = true 시 회전이 (-) 적 방향, 즉 적의 반대.
            //# 몬스터 (0,0,0), 영웅 (1,0,0) → away 는 (-X) 방향 → yaw 270°.
            var (hero, monster) = Spawn(
                heroPos: new Vector3(1f, 0f, 0f),
                monsterPos: Vector3.zero,
                monsterRange: 2f);

            monster.GetComponent<AutoCombatAI>().FleeMode = true;
            //# Flee 중 위치 변화 최소화.
            monster.GetComponent<SimpleMover>().Speed = 0.001f;
            hero.GetComponent<SimpleMover>().Speed = 0.001f;

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# 적 방향 dot product < 0 — 적의 반대를 보고 있어야.
            Vector3 toHero = (hero.transform.position - monster.transform.position).normalized;
            float dot = Vector3.Dot(monster.transform.forward, toHero);
            Assert.Less(dot, 0f,
                $"FleeMode 시 적 방향과 forward 의 dot < 0 (반대 방향), 실제 {dot:F3}");
        }
    }
}
