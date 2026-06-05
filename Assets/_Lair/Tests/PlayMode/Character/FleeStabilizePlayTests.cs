using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# A (도주 안정화) 본격 스위트 — plan §6 A-1/A-2/A-3.
    //# 감쇠(1-exp(-rate·dt))·스냅 로직은 AutoCombatAI.Update 인라인 + Time.deltaTime 직접 의존 →
    //# EditMode 추출 seam 없음(production 미주입). 따라서 plan 의 FleeStabilizeTests.cs(EditMode) 위치에서
    //# PlayMode 로 이관해 실제 Update 거동을 관측한다(advisor 합의). centroid(#4)만 EditMode(ThreatCentroidTests).
    //#
    //# 관측 메커니즘: SimpleRotator 가 _snapInstant 이라 hero.forward 가 _fleeDirSmoothed 를 즉시 추종 →
    //# 프레임간 forward yaw 변화량이 곧 감쇠 유계의 관찰 가능한 증거.
    //# 가정: AddComponent 의 C# 필드 이니셜라이저 — _fleeTurnSmoothing=5, _fleeThreatRadius=4 (프리팹 오버라이드 없음).
    public class FleeStabilizePlayTests
    {
        private const float TurnSmoothing = 5f;   //# AutoCombatAI._fleeTurnSmoothing 기본값과 일치.

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

        //# 영웅 풀스택 — real 컴포넌트. 영웅 프리팹은 SimpleRotator._snapInstant=true 라 forward 가
        //# _fleeDirSmoothed 를 같은 프레임에 즉시 추종(보간 지연 없음). 테스트도 동일하게 주입해야
        //# 진입 1프레임 스냅 관측(#3/#4)이 성립 — 미주입 시 720°/s 보간이라 1프레임에 ~12°만 돌아 forward 가 stale.
        private GameObject SpawnHero(Vector3 pos)
        {
            GameObject hero = new GameObject("flee_hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);
            hero.AddComponent<SimpleMover>().Speed = 3f;
            hero.AddComponent<MeleeAttacker>().Configure(1.5f, 1f, 1);
            SimpleRotator rotator = hero.AddComponent<SimpleRotator>();
            rotator.TurnSpeedDegPerSec = 720f;
            SetField(rotator, "_snapInstant", true);   //# 영웅 프리팹 동등 — forward 즉시 스냅.
            hero.AddComponent<HeroTargetProvider>();
            hero.AddComponent<AutoCombatAI>();
            _spawned.Add(hero);
            return hero;
        }

        //# 위협 몬스터 — Engaging 등록. 위치는 테스트가 직접 teleport(MoveTo 안 씀 → 정지).
        private GameObject SpawnEngagingMonster(Vector3 pos)
        {
            GameObject m = new GameObject("flee_monster");
            m.transform.position = pos;
            Health hp = m.AddComponent<Health>();
            hp.SetMax(100000);
            CharacterRegistry.RegisterMonster(m.transform, hp);
            CharacterRegistry.SetMonsterEngaging(m.transform, true);
            _spawned.Add(m);
            return m;
        }

        private static void SetField(object t, string f, object v)
            => t.GetType().GetField(f, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(t, v);

        //# ===== A-2 #1 — 진동 입력에 대해 출력 도주방향이 유계(감쇠) =====

        //# 위협을 +X 쪽에 둔 채 Z 로만 매 프레임 흔든다(±1.5) → raw 도주방향이 -X 둘레에서 ~74° 진동.
        //# 입력의 X 성분은 항상 같은 부호(-2) → smoothed 가 0을 가로지르지 않아 각도 진동을 감쇠가 실제로 유계화한다.
        //# (순수 180° 반전은 평균 방향이 없어 어떤 감쇠도 facing 을 못 묶는다 — 그건 production 버그가 아니라
        //#  병리적 입력이므로 의도적으로 한쪽 bias + 직교 jitter 로 구성. 실제 떨림 원인도 안정 bias 둘레의 진동.)
        [UnityTest]
        public IEnumerator 진동하는_위협입력에도_도주방향_프레임변화가_유계다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            GameObject threat = SpawnEngagingMonster(new Vector3(2, 0, 1.5f));
            //# 영웅 위치 고정(도주 1프레임 이동을 매 프레임 0 으로 되돌림) — 방향 진동만 관측.
            hero.GetComponent<SimpleMover>().Speed = 0f;

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.FleeMode = true;

            //# 진입 프레임 — 스냅. 첫 forward 확보.
            yield return null;
            float prevYaw = hero.transform.eulerAngles.y;

            float maxDelta = 0f;
            for (int i = 0; i < 30; ++i)
            {
                //# 위협을 (2,0,+1.5)/(2,0,-1.5) 로 매 프레임 점프 → raw 도주방향이 -X 둘레 ±37° 진동(미감쇠 ~74°/프레임).
                threat.transform.position = (i % 2 == 0)
                    ? new Vector3(2, 0, 1.5f)
                    : new Vector3(2, 0, -1.5f);
                hero.transform.position = Vector3.zero;
                yield return null;

                float yaw = hero.transform.eulerAngles.y;
                float delta = Mathf.Abs(Mathf.DeltaAngle(prevYaw, yaw));
                if (delta > maxDelta) maxDelta = delta;
                prevYaw = yaw;
            }

            //# 60fps 기준 1프레임 감쇠 반영율 ~8% → ~74° 입력 진동이 forward 에는 프레임당 몇 도로만 반영.
            //# 상한 30°: 감쇠 없으면 매 프레임 ~74° 가 찍힌다.
            Assert.Less(maxDelta, 30f,
                $"진동 위협 입력에도 도주 forward 프레임간 변화가 유계여야 한다(감쇠). 실제 max={maxDelta:F1}° " +
                $"(감쇠 미작동이면 ~74°/프레임).");
        }

        //# ===== A-2 #2 — 시간 경과 시 위협 반대 방향으로 수렴(도주 기능 보존) =====

        //# 위협을 한쪽(+X)에 고정 → 도주방향은 -X. 감쇠가 ~0.6s 안에 95% 추종해야(τ=0.2).
        [UnityTest]
        public IEnumerator 위협_고정시_반대방향으로_수렴한다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            SpawnEngagingMonster(new Vector3(2, 0, 0));
            hero.GetComponent<SimpleMover>().Speed = 0f;   //# 위치 고정 — forward 수렴만 관측.

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.FleeMode = true;

            //# 0.7s 경과(τ=0.2 → 95% 추종은 0.6s). hero.position 고정이라 raw 도주방향은 항상 -X.
            float elapsed = 0f;
            while (elapsed < 0.7f)
            {
                hero.transform.position = Vector3.zero;
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# forward 가 -X(yaw 270°) 로 수렴 — 위협(+X) 반대.
            Vector3 forward = hero.transform.forward;
            Assert.Less(forward.x, -0.9f,
                $"위협 고정 시 도주 forward 가 -X 로 수렴(반대 방향). forward.x={forward.x:F3}");
        }

        //# A-2 #2 보강 — 도주 기능 보존: 실제 이동으로 적과의 거리가 증가한다(forward 뿐 아니라 변위).
        [UnityTest]
        public IEnumerator 도주시_적과의_거리가_증가한다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            GameObject threat = SpawnEngagingMonster(new Vector3(2, 0, 0));

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();
            ai.FleeMode = true;
            yield return null;
            float startDist = Vector3.Distance(hero.transform.position, threat.transform.position);

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            float endDist = Vector3.Distance(hero.transform.position, threat.transform.position);
            Assert.Greater(endDist, startDist + 0.5f,
                $"도주 보존 — 적과의 거리 증가. {startDist:F2} → {endDist:F2}");
        }

        //# ===== A-3 #3 — FleeMode 진입 시 초기 방향 스냅(stale 미사용) =====

        //# 시나리오: 먼저 +X 위협으로 도주(forward -X 수렴) → FleeMode off → 위협을 -X 로 이동 →
        //# FleeMode 재진입. 스냅이 정상이면 첫 프레임에 새 raw(+X) 로 즉시 점프, stale(-X)에서 lerp 안 함.
        [UnityTest]
        public IEnumerator FleeMode_재진입시_초기방향이_raw로_스냅된다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            GameObject threat = SpawnEngagingMonster(new Vector3(2, 0, 0));
            hero.GetComponent<SimpleMover>().Speed = 0f;   //# 위치 고정 — 방향만.

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();

            //# 1단계 — +X 위협으로 도주 → forward -X(yaw270) 수렴.
            ai.FleeMode = true;
            float elapsed = 0f;
            while (elapsed < 0.7f)
            {
                hero.transform.position = Vector3.zero;
                elapsed += Time.deltaTime;
                yield return null;
            }
            Assert.Less(hero.transform.forward.x, -0.9f, "사전: +X 위협 도주 → forward -X 수렴.");

            //# 2단계 — 도주 해제. 비도주 프레임에서 _wasFleeing=false 로 떨어짐.
            ai.FleeMode = false;
            yield return null;
            yield return null;

            //# 3단계 — 위협을 반대편(-X)으로 이동 → 새 raw 도주방향은 +X(yaw90).
            threat.transform.position = new Vector3(-2, 0, 0);

            //# 4단계 — FleeMode 재진입. 스냅 정상이면 첫 Update 에서 forward 가 +X 로 즉시 점프.
            ai.FleeMode = true;
            hero.transform.position = Vector3.zero;
            yield return null;   //# 진입 1프레임만 — lerp 누적 전.

            //# 스냅: 첫 프레임에 raw(+X) 로 점프 → forward.x > 0.9. stale(-X) lerp 면 -X 근처에서 8%만 움직여 음수 유지.
            Assert.Greater(hero.transform.forward.x, 0.9f,
                $"FleeMode 재진입 첫 프레임 — 새 raw(+X)로 스냅(stale -X lerp 아님). forward.x={hero.transform.forward.x:F3}");
        }

        //# A-3 #3 보강 — 풀 재사용(OnDisable→OnEnable) 후 도주 감쇠 상태 리셋.
        //# 비활성 전 -X 도주 상태를 만들고, 재활성 후 +X 위협으로 재진입 → 첫 프레임 +X 스냅.
        //# 리셋 누락(잔존 _fleeDirSmoothed=-X)이면 첫 진입 스냅이 새 raw 로 덮어쓰므로 스냅 자체는 동일하나,
        //# OnEnable 의 _wasFleeing=false / _fleeDirSmoothed=0 리셋이 깨지면 진입 판정이 lerp 로 빠질 수 있다.
        [UnityTest]
        public IEnumerator 풀_재사용후_도주진입시_raw로_스냅된다()
        {
            GameObject hero = SpawnHero(Vector3.zero);
            GameObject threat = SpawnEngagingMonster(new Vector3(2, 0, 0));
            hero.GetComponent<SimpleMover>().Speed = 0f;

            AutoCombatAI ai = hero.GetComponent<AutoCombatAI>();

            //# 1단계 — +X 위협 도주로 -X 도주상태 형성.
            ai.FleeMode = true;
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                hero.transform.position = Vector3.zero;
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# 2단계 — 풀 Push 모사(비활성). OnDisable→재활성 OnEnable 이 _fleeDirSmoothed/_wasFleeing/FleeMode 리셋.
            hero.SetActive(false);
            yield return null;

            //# 3단계 — 위협을 -X 로 옮김 → 재진입 raw 도주방향은 +X.
            threat.transform.position = new Vector3(-2, 0, 0);

            //# 4단계 — 풀 Pop 모사(재활성). OnEnable: FleeMode=false, _fleeDirSmoothed=0, _wasFleeing=false.
            hero.SetActive(true);
            hero.transform.position = Vector3.zero;

            //# 5단계 — 재진입. 첫 Update 에서 새 raw(+X) 스냅.
            ai.FleeMode = true;
            yield return null;

            Assert.Greater(hero.transform.forward.x, 0.9f,
                $"풀 재사용 후 도주 진입 — 새 raw(+X) 스냅(이전 -X 상태 잔존 아님). forward.x={hero.transform.forward.x:F3}");
        }
    }
}
