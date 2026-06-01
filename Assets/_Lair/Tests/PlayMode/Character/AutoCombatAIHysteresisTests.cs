using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# plan Task 4 — AutoCombatAI 사거리 히스테리시스 전이 검증 (기획서 autocombatai-hysteresis.md).
    //# 핵심: 미교전→dist<=Range 진입 / 교전→dist>Range+버퍼(0.5) 여야 해제 / 밴드(Range, Range+0.5] 안에선 _engaged 유지.
    //# 행위(IsMoving) 로 _engaged 를 검증 — 밴드 안 IsMoving=false 유지가 히스테리시스의 관찰 가능한 증거.
    //# _engaged 리플렉션은 풀 재사용 리셋 케이스의 보강 단언으로만 사용.
    //# 테스트는 _engageBuffer 기본값 0.5 를 가정한다 (AddComponent 의 C# 필드 이니셜라이저 — 프리팹 오버라이드 없음).
    //# 몬스터 구성은 MonsterApproachDiagTests.SpawnMonster 패턴 재사용 (kinematic Rigidbody + MovePosition 경로).
    public class AutoCombatAIHysteresisTests
    {
        private const float EngageBuffer = 0.5f;   //# AutoCombatAI._engageBuffer 기본값과 일치해야 함.
        private const float MonsterRange = 1.5f;
        private const float MonsterSpeed = 3f;
        private const float MonsterCooldown = 1f;
        private const int MonsterPower = 10;

        private readonly List<GameObject> _spawned = new();

        [SetUp]
        public void SetUp()
        {
            //# CharacterRegistry 는 static — 이전 테스트 잔존 격리.
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

        //# 영웅 역할 타겟 — Health + RegisterHero. AutoCombatAI 미부착 → 위치는 테스트가 직접 teleport.
        private GameObject SpawnHeroTarget(Vector3 pos)
        {
            GameObject hero = new GameObject("hyst_hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);   //# 테스트 중 사망 방지.
            CharacterRegistry.RegisterHero(hero.transform, hp);
            _spawned.Add(hero);
            return hero;
        }

        //# 몬스터 — 런타임 구성 모사(kinematic). MonsterApproachDiagTests 와 동형.
        private GameObject SpawnMonster(Vector3 pos)
        {
            GameObject m = new GameObject("hyst_monster");
            m.transform.position = pos;

            Rigidbody rb = m.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;

            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            m.AddComponent<SimpleMover>().Speed = MonsterSpeed;
            m.AddComponent<MeleeAttacker>().Configure(MonsterRange, MonsterCooldown, MonsterPower);
            m.AddComponent<SimpleRotator>().TurnSpeedDegPerSec = 720f;
            m.AddComponent<MonsterTargetProvider>();
            m.AddComponent<AutoCombatAI>();
            _spawned.Add(m);
            return m;
        }

        //# private bool _engaged 직접 읽기 — 보강 단언용 (행위 검증이 주, 리플렉션은 종).
        private static bool ReadEngaged(GameObject monster)
        {
            AutoCombatAI ai = monster.GetComponent<AutoCombatAI>();
            FieldInfo field = typeof(AutoCombatAI).GetField(
                "_engaged", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, "_engaged 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            return (bool)field.GetValue(ai);
        }

        //# 여러 FixedUpdate 틱 대기 — kinematic MovePosition / Update 분기 안정화.
        private static IEnumerator WaitTicks(int ticks)
        {
            for (int i = 0; i < ticks; ++i)
                yield return new WaitForFixedUpdate();
        }

        //# Step 1 — 전이: 미교전→진입→밴드 유지→밴드 밖 해제. 영웅을 teleport 로 거리 제어(몬스터 정지 유도).
        [UnityTest]
        public IEnumerator 진입_밴드유지_해제_전이가_올바르다()
        {
            //# 영웅을 사거리 안(0.5)에 두고 시작 — 몬스터는 origin.
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            SimpleMover monMover = monster.GetComponent<SimpleMover>();

            //# 진입 — dist(0.5) <= Range(1.5) → _engaged=true → Stop. IsMoving=false 수렴 대기.
            yield return WaitTicks(10);
            Assert.IsFalse(monMover.IsMoving,
                $"진입: dist=0.5 <= Range={MonsterRange} → 교전(Stop) 이어야 한다. IsMoving={monMover.IsMoving}");
            Assert.IsTrue(ReadEngaged(monster), "진입 후 _engaged=true 여야 한다.");

            //# 밴드 안으로 영웅 이동 — dist = Range+0.3 = 1.8 ∈ (1.5, 2.0]. 교전 유지 → 여전히 Stop.
            hero.transform.position = new Vector3(MonsterRange + 0.3f, 0f, 0f);
            yield return WaitTicks(10);
            Assert.IsFalse(monMover.IsMoving,
                $"밴드 유지: dist={MonsterRange + 0.3f} ∈ (Range, Range+버퍼] → _engaged 유지 → Move 복귀 안 함. " +
                $"IsMoving={monMover.IsMoving} (true 면 히스테리시스 미작동).");
            Assert.IsTrue(ReadEngaged(monster), "밴드 안에서 _engaged 유지여야 한다.");

            //# 밴드 밖으로 영웅 이동 — dist = Range+0.8 = 2.3 > Range+버퍼(2.0) → 해제 → Move 재개.
            hero.transform.position = new Vector3(MonsterRange + EngageBuffer + 0.3f, 0f, 0f);
            yield return WaitTicks(10);
            Assert.IsTrue(monMover.IsMoving,
                $"해제: dist={MonsterRange + EngageBuffer + 0.3f} > Range+버퍼({MonsterRange + EngageBuffer}) → 교전 해제 → Move 재개. " +
                $"IsMoving={monMover.IsMoving} (false 면 영원히 못 따라감).");
            Assert.IsFalse(ReadEngaged(monster), "밴드 밖에서 _engaged=false 여야 한다.");
        }

        //# Step 1b — 경계 진동 부재: 영웅을 Range 경계를 가로질러 미세 진동시켜도 빠른 Move/Stop 토글이 안 생긴다.
        //# 히스테리시스 핵심 — 일단 교전하면 밴드 안(<=Range+버퍼) 미동에는 상태가 안 뒤집힌다.
        [UnityTest]
        public IEnumerator 경계진동에도_빠른토글이_없다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            SimpleMover monMover = monster.GetComponent<SimpleMover>();

            //# 먼저 교전 진입 안정화.
            yield return WaitTicks(10);
            Assert.IsFalse(monMover.IsMoving, "사전 교전 진입 — IsMoving=false 여야 한다.");

            //# 영웅을 Range(1.5) 아래(1.4)와 밴드 안(1.9) 사이에서 매 틱 진동 — 경계를 가로지른다.
            //# 두 값 모두 밴드(<=Range+버퍼 2.0) 안이라 교전 유지 → Move 복귀 없음.
            //# 수정 전(히스테리시스 부재)이라면: 1.4 → Stop(dist<=Range), 1.9 → Move(dist>Range) 매 틱 뒤집힘 = 토글 다수.
            int toggles = 0;
            bool prevMoving = monMover.IsMoving;
            for (int i = 0; i < 60; ++i)
            {
                float dist = (i % 2 == 0) ? (MonsterRange - 0.1f) : (MonsterRange + 0.4f);
                hero.transform.position = new Vector3(dist, 0f, 0f);
                yield return new WaitForFixedUpdate();

                if (monMover.IsMoving != prevMoving)
                    ++toggles;
                prevMoving = monMover.IsMoving;
            }

            //# 경계 진동 → _engaged 유지(둘 다 밴드 안) → IsMoving 항상 false → 토글 0.
            Assert.LessOrEqual(toggles, 1,
                $"경계(Range) 가로지르는 진동에도 Move/Stop 토글이 거의 없어야 한다(히스테리시스). 실제 토글={toggles}. " +
                $"다수면 dist>Range 마다 Move 복귀 = 히스테리시스 미작동.");
            Assert.IsFalse(monMover.IsMoving,
                $"진동 종료 시점에도 교전(Stop) 유지여야 한다. IsMoving={monMover.IsMoving}");
        }

        //# Step 3a — 비간섭: FleeMode 활성 시 교전 무시하고 도주(Move 로 적 반대 방향).
        [UnityTest]
        public IEnumerator FleeMode_활성시_교전무시하고_도주한다()
        {
            //# 영웅을 사거리 안(0.5)에 — FleeMode 가 아니면 Stop(교전) 했을 위치.
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            SimpleMover monMover = monster.GetComponent<SimpleMover>();
            monster.GetComponent<AutoCombatAI>().FleeMode = true;

            yield return WaitTicks(10);

            //# FleeMode → 사거리 안이어도 Stop(교전) 이 아니라 Move(도주) 여야 한다.
            Assert.IsTrue(monMover.IsMoving,
                $"FleeMode 시 사거리 안이어도 교전 무시하고 도주(Move). IsMoving={monMover.IsMoving}");
            //# 적 반대 방향으로 이동 — 몬스터 x 가 영웅(+x) 의 반대인 음수로 이동했어야.
            Assert.Less(monster.transform.position.x, 0f,
                $"도주 — 적(+x) 반대 방향(-x) 이동. monsterX={monster.transform.position.x:F3}");
        }

        //# Step 3b — 비간섭: 타겟 상실 시 Idle(Stop).
        [UnityTest]
        public IEnumerator 타겟_상실시_정지한다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            SimpleMover monMover = monster.GetComponent<SimpleMover>();

            //# dist 5 > Range → 추격(Move) 시작.
            yield return WaitTicks(5);
            Assert.IsTrue(monMover.IsMoving, "타겟 추격 중 — IsMoving=true.");

            //# 타겟 제거 — SpawnHeroTarget 은 HeroTargetProvider 미부착이라 자동 unregister 안 됨.
            //# TryFindNearest 가 Transform==null 가드로 죽은 엔트리를 건너뛰긴 하나, 명시적으로 레지스트리 정리.
            CharacterRegistry.UnregisterHero(hero.transform);
            Object.DestroyImmediate(hero);
            _spawned.Remove(hero);

            yield return WaitTicks(5);
            Assert.IsFalse(monMover.IsMoving,
                $"타겟 상실 → Idle(Stop). IsMoving={monMover.IsMoving}");
        }

        //# Step 3c — 풀 재사용: OnDisable→OnEnable 후 _engaged 리셋. 재사용 첫 프레임은 미교전.
        //# 판별 핵심(advisor) — 재활성 시 영웅을 밴드 안(Range<dist<=Range+버퍼)에 둔다.
        //#   리셋 정상: _engaged=false → dist>Range 라 진입 안 함 → Move(추격) → IsMoving=true.
        //#   리셋 누락(잔존 _engaged=true): dist<=Range+버퍼 라 유지 → Stop → IsMoving=false.
        //# 즉 IsMoving=true 가 리셋이 실제로 일어났다는 관찰 가능한 증거.
        [UnityTest]
        public IEnumerator 풀_재사용시_교전상태가_리셋된다()
        {
            //# 1단계 — 사거리 안에서 교전 상태(_engaged=true) 를 만든다.
            GameObject hero = SpawnHeroTarget(new Vector3(0.5f, 0f, 0f));
            GameObject monster = SpawnMonster(Vector3.zero);
            SimpleMover monMover = monster.GetComponent<SimpleMover>();

            yield return WaitTicks(10);
            Assert.IsTrue(ReadEngaged(monster), "사전: 사거리 안에서 _engaged=true 성립.");

            //# 2단계 — 풀 Push 모사(비활성).
            monster.SetActive(false);
            yield return null;

            //# 3단계 — 영웅을 밴드 안(Range+0.3=1.8 ∈ (1.5,2.0]) 으로 이동. 잔존 교전이면 Stop, 리셋이면 Move.
            hero.transform.position = new Vector3(MonsterRange + 0.3f, 0f, 0f);
            //# 몬스터도 origin 유지 — 거리 1.8.
            monster.transform.position = Vector3.zero;

            //# 4단계 — 풀 Pop 모사(재활성). OnEnable 에서 _engaged=false.
            monster.SetActive(true);

            //# 2틱만 진행 후 검증 — 몬스터 이속 3 × dt 0.02 = 0.06/틱. 2틱이면 dist≈1.68 로 아직 밴드 안.
            //# 5틱 이상 기다리면 빠른 몬스터가 dist<=Range 까지 재진입해 Stop 으로 뒤집혀 판별 불가.
            yield return WaitTicks(2);

            Assert.IsTrue(monMover.IsMoving,
                $"풀 재사용 후 _engaged 리셋 → 밴드 안(dist={MonsterRange + 0.3f})이어도 미교전이라 추격(Move) 시작. " +
                $"IsMoving={monMover.IsMoving} (false 면 교전 상태가 잔존해 리셋 실패).");
            Assert.IsFalse(ReadEngaged(monster),
                "재사용 첫 프레임 — 밴드 안에서는 미교전(_engaged=false) 유지여야 한다.");
        }
    }
}
