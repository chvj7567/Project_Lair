using System.Collections;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Diagnostics
{
    //# 진단 전용 — "다가오는 몬스터 stutter(멈췄다-갔다)" 현상의 원인을 데이터로 규명.
    //# 수정 아님. AutoCombatAI 의 Move/Stop 토글을 프레임 단위로 계측·로깅.
    //#
    //# 핵심 가설(코드 사실로 추론): SimpleMover.FixedUpdate 는 _moving 동안 무조건 전진(충돌 정지 없음).
    //# 몬스터 멈춤은 AutoCombatAI 가 _moving=false(Stop) 로 만들 때뿐 — ① dist<=Range(공격) ② 타겟 없음 ③ FleeMode.
    //# 런타임 몬스터는 kinematic 이라 물리 정지 불가 → stutter = dist↔Range 경계의 _moving 토글 진동(히스테리시스 부재) 으로 추정.
    //#
    //# 시나리오 A(정지 영웅) = 대조군: dist 단조 감소 → Move→Stop 1회뿐, 토글 0 (정상 baseline).
    //# 시나리오 B(영웅이 몬스터로부터 멀어지며 이동) = 재현: 추격 중 dist 가 Range 근처를 들락 → Move/Stop 진동.
    //# 모든 분기 신호는 public API (IsMoving / MeleeAttacker.Range / TryFindNearest) — 리플렉션 불필요.
    public class MonsterApproachDiagTests
    {
        private const float MonsterSpeed = 3f;
        private const float MonsterRange = 1.5f;
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

        //# 영웅 역할 타겟 — Health + CharacterRegistry.RegisterHero 로 등록.
        //# AutoCombatAI 미부착 → 스스로 추격 안 함. 이동은 테스트가 SimpleMover 로 수동 구동.
        private GameObject SpawnHeroTarget(Vector3 pos, bool addMover)
        {
            GameObject hero = new GameObject("diag_hero");
            hero.transform.position = pos;
            Health hp = hero.AddComponent<Health>();
            hp.SetMax(100000);   //# 진단 중 사망 방지 — dist/이동만 관찰.
            if (addMover)
                hero.AddComponent<SimpleMover>().Speed = 0f;   //# 속도는 시나리오에서 설정.
            //# 직접 등록 — MonsterTargetProvider 가 TryFindNearestHero 로 찾는다.
            CharacterRegistry.RegisterHero(hero.transform, hp);
            _spawned.Add(hero);
            return hero;
        }

        //# 몬스터 — 런타임 구성 모사(kinematic). SimpleMover+AutoCombatAI+MeleeAttacker+MonsterTargetProvider+Health+SimpleRotator.
        //# Rigidbody 를 kinematic 으로 두어 MonsterTag.OnEnable 의 런타임 강제(isKinematic=true) 와 동일 물리 조건 재현.
        private GameObject SpawnMonster(Vector3 pos)
        {
            GameObject m = new GameObject("diag_monster");
            m.transform.position = pos;

            //# kinematic Rigidbody — 런타임 몬스터와 동일. MovePosition 경로 사용.
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

        //# 한 틱의 계측 스냅샷.
        private struct Sample
        {
            public int Tick;
            public float MonsterX;
            public float Dist;
            public bool IsMoving;
            public bool InRange;        //# dist <= Range — Update 가 Stop+attack 분기로 갈 조건.
            public bool TargetFound;    //# TryFindNearest 성공 여부(타겟 없음 분기 식별).
        }

        //# IsMoving 의 true→false / false→true 전환 횟수.
        private static int CountToggles(List<Sample> log)
        {
            int toggles = 0;
            for (int i = 1; i < log.Count; ++i)
            {
                if (log[i].IsMoving != log[i - 1].IsMoving)
                    ++toggles;
            }
            return toggles;
        }

        //# "빠른 플리커" 토글 쌍 개수 — 직전 토글로부터 maxGapTicks 미만 간격으로 또 토글한 횟수.
        //# 히스테리시스 부재(per-frame 진동)는 dist 가 Range 경계를 1~2 틱마다 횡단 → 인접 토글 간격이 매우 짧다(다수).
        //# 히스테리시스 정상(느린 band-cycle)은 영웅이 밴드 폭(0.5)을 통과(speed 2.1 기준 ≈12틱)해야 토글 → 간격이 수십 틱(빠른 쌍 0).
        //# 이 메트릭이 "per-frame 플리커=버그" 와 "느린 band-cycle=정상" 을 간격으로 구분한다.
        private static int CountFastToggles(List<Sample> log, int maxGapTicks)
        {
            int fast = 0;
            int lastToggleTick = int.MinValue;
            for (int i = 1; i < log.Count; ++i)
            {
                if (log[i].IsMoving == log[i - 1].IsMoving)
                    continue;

                if (lastToggleTick != int.MinValue && (log[i].Tick - lastToggleTick) < maxGapTicks)
                    ++fast;
                lastToggleTick = log[i].Tick;
            }
            return fast;
        }

        //# 접근 구간(dist>Range) 인데 정지한 프레임 수 — stutter 의 핵심 지표.
        private static int CountStuckWhileApproaching(List<Sample> log)
        {
            int stuck = 0;
            foreach (Sample s in log)
            {
                if (s.Dist > MonsterRange && s.IsMoving == false && s.TargetFound)
                    ++stuck;
            }
            return stuck;
        }

        //# 시계열 + 요약을 Debug.Log 로 출력. 메인이 editor_read_log 로 읽는다.
        //# greppable prefix: [DIAG-LINE] (틱별) / [DIAG-SUMMARY] (요약).
        private static (int toggles, int stuck, float firstStopDist) DumpAndSummarize(
            string label, List<Sample> log)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"[DIAG-{label}] time-series (tick | monX | dist | IsMoving | inRange | targetFound):");
            foreach (Sample s in log)
            {
                sb.AppendLine(
                    $"[DIAG-{label}-LINE] t={s.Tick,3} x={s.MonsterX,7:F3} dist={s.Dist,7:F3} " +
                    $"moving={(s.IsMoving ? 1 : 0)} inRange={(s.InRange ? 1 : 0)} target={(s.TargetFound ? 1 : 0)}");
            }
            Debug.Log(sb.ToString());

            int toggles = CountToggles(log);
            int stuck = CountStuckWhileApproaching(log);

            //# 최초 멈춤(IsMoving false 첫 발생) 시점의 dist.
            float firstStopDist = -1f;
            foreach (Sample s in log)
            {
                if (s.IsMoving == false)
                {
                    firstStopDist = s.Dist;
                    break;
                }
            }

            float startX = log.Count > 0 ? log[0].MonsterX : 0f;
            float endX = log.Count > 0 ? log[log.Count - 1].MonsterX : 0f;
            float minDist = float.MaxValue;
            float maxDist = float.MinValue;
            foreach (Sample s in log)
            {
                if (s.Dist < minDist) minDist = s.Dist;
                if (s.Dist > maxDist) maxDist = s.Dist;
            }

            Debug.Log(
                $"[DIAG-SUMMARY-{label}] toggles={toggles} stuckWhileApproaching={stuck} " +
                $"firstStopDist={firstStopDist:F3} monsterX:{startX:F3}->{endX:F3} " +
                $"dist[min={minDist:F3},max={maxDist:F3}] range={MonsterRange} ticks={log.Count}");

            return (toggles, stuck, firstStopDist);
        }

        //# 시나리오 A — 정지 영웅에 단일 몬스터 접근 (대조군).
        //# 기대: dist 단조 감소 → Move 연속 유지 → Stop 1회 진입 후 정착. 토글 0 (또는 1).
        //# A 가 깨끗한 건 정상. A 가 stutter 면 가설이 틀린 것 → 원인 재탐색 신호.
        [UnityTest]
        public IEnumerator A_정지영웅에_접근하는_단일몬스터는_매끄럽게_이동한다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(0f, 0f, 0f), addMover: false);
            GameObject monster = SpawnMonster(new Vector3(10f, 0f, 0f));

            MonsterTargetProvider provider = monster.GetComponent<MonsterTargetProvider>();
            SimpleMover mover = monster.GetComponent<SimpleMover>();
            MeleeAttacker attacker = monster.GetComponent<MeleeAttacker>();

            //# OnEnable / 첫 Update 안정화.
            yield return new WaitForFixedUpdate();

            List<Sample> log = new List<Sample>();
            for (int tick = 0; tick < 150; ++tick)
            {
                yield return new WaitForFixedUpdate();

                bool targetFound = provider.TryFindNearest(
                    monster.transform.position, out _, out _);
                float dist = Vector3.Distance(monster.transform.position, hero.transform.position);
                log.Add(new Sample
                {
                    Tick = tick,
                    MonsterX = monster.transform.position.x,
                    Dist = dist,
                    IsMoving = mover.IsMoving,
                    InRange = dist <= attacker.Range,
                    TargetFound = targetFound,
                });
            }

            (int toggles, int stuck, float firstStopDist) = DumpAndSummarize("A", log);

            //# 대조군 — 접근 중 멈춘 프레임이 거의 없어야(단조 접근). 1~2 는 경계 진입 프레임 허용.
            Assert.LessOrEqual(stuck, 2,
                $"[A 대조군] 정지 영웅 접근은 매끄러워야 한다. " +
                $"접근중(dist>Range)인데 정지한 프레임={stuck}, IsMoving 토글={toggles}, 최초멈춤 dist={firstStopDist:F3}. " +
                $"A 가 stutter 면 stutter 원인이 '영웅 이동' 이 아니라는 신호 — 가설 재검토.");
        }

        //# 시나리오 B — 영웅이 몬스터로부터 멀어지며 이동 (재현군).
        //# 영웅 +X 진행, 몬스터는 그 뒤(-X 쪽)에서 추격. 몬스터 속도가 영웅보다 약간 빨라 dist 가 Range 근처 hover.
        //# 기대(stutter 가설 참): 매 틱 dist<=Range→Stop+attack, 영웅 전진→dist>Range→Move 반복 → Move/Stop 진동.
        [UnityTest]
        public IEnumerator B_멀어지는영웅을_추격하는_몬스터의_MoveStop_진동을_계측한다()
        {
            //# 영웅 origin, 몬스터는 뒤(-X). 영웅은 +X 로 도주.
            GameObject hero = SpawnHeroTarget(new Vector3(0f, 0f, 0f), addMover: true);
            GameObject monster = SpawnMonster(new Vector3(-3f, 0f, 0f));

            SimpleMover heroMover = hero.GetComponent<SimpleMover>();
            //# 영웅 속도 < 몬스터 속도 → 몬스터가 따라잡아 dist 가 Range 근처에서 hover (catch-up 추격).
            heroMover.Speed = MonsterSpeed * 0.7f;
            //# 영웅을 멀리 +X 로 계속 이동 — 추격 구도 유지.
            heroMover.MoveTo(new Vector3(1000f, 0f, 0f));

            MonsterTargetProvider provider = monster.GetComponent<MonsterTargetProvider>();
            SimpleMover monMover = monster.GetComponent<SimpleMover>();
            MeleeAttacker attacker = monster.GetComponent<MeleeAttacker>();

            yield return new WaitForFixedUpdate();

            List<Sample> log = new List<Sample>();
            //# catch-up 구간이 안정 hover 에 들도록 워밍 후 계측 — 초반 거리 좁히는 구간은 정상 접근.
            int warmupTicks = 40;
            for (int tick = 0; tick < warmupTicks + 150; ++tick)
            {
                yield return new WaitForFixedUpdate();
                if (tick < warmupTicks)
                    continue;

                bool targetFound = provider.TryFindNearest(
                    monster.transform.position, out _, out _);
                float dist = Vector3.Distance(monster.transform.position, hero.transform.position);
                log.Add(new Sample
                {
                    Tick = tick - warmupTicks,
                    MonsterX = monster.transform.position.x,
                    Dist = dist,
                    IsMoving = monMover.IsMoving,
                    InRange = dist <= attacker.Range,
                    TargetFound = targetFound,
                });
            }

            (int toggles, int stuck, float firstStopDist) = DumpAndSummarize("B", log);

            //# 회귀 잠금 (재보정 2026-06-01) — "빠른 플리커" 기준으로 단언.
            //# 기존 `toggles<=2` 는 정상 거동인 느린 band-cycle(밴드 밖 영웅 재추격, ≈8회)까지 오탐했다.
            //# 진짜 버그는 per-frame 진동(인접 토글 간격 1~2틱) — 이를 CountFastToggles 로만 잡는다.
            //#   수정 전: toggles≈63, 인접 간격 ~2틱 → fast pairs ≈60 → FAIL (버그 박제).
            //#   수정 후: toggles≈8, 느린 band-cycle 간격 수십 틱 → fast pairs 0 → PASS.
            int fast = CountFastToggles(log, maxGapTicks: 5);
            Assert.LessOrEqual(fast, 1,
                $"[B 재현군] 멀어지는 영웅 추격 — per-frame Move/Stop 플리커(stutter) 검출. " +
                $"빠른토글쌍(간격<5틱)={fast} (히스테리시스 부재 시 다수), " +
                $"총토글={toggles}(느린 band-cycle 은 정상이므로 총량은 단언 안 함), " +
                $"접근중 정지 프레임={stuck}, 최초멈춤 dist={firstStopDist:F3}, Range={MonsterRange}. " +
                $"빠른토글쌍 다수 = dist 가 Range 경계를 매 틱 횡단 = 히스테리시스 부재.");
        }

        //# 시나리오 B2 — 영웅 원호(circular) 이동. dist 가 Range 경계를 부드럽게 들락날락하는 또 다른 재현.
        //# 직선 추격과 다른 경로로도 동일 토글 진동이 재현되는지 교차 확인.
        [UnityTest]
        public IEnumerator B2_원호이동영웅_주변_몬스터의_경계진동을_계측한다()
        {
            GameObject hero = SpawnHeroTarget(new Vector3(0f, 0f, 0f), addMover: false);
            //# 몬스터를 Range 경계 바로 바깥에 배치.
            GameObject monster = SpawnMonster(new Vector3(MonsterRange + 0.3f, 0f, 0f));
            monster.GetComponent<SimpleMover>().Speed = MonsterSpeed;

            MonsterTargetProvider provider = monster.GetComponent<MonsterTargetProvider>();
            SimpleMover monMover = monster.GetComponent<SimpleMover>();
            MeleeAttacker attacker = monster.GetComponent<MeleeAttacker>();

            yield return new WaitForFixedUpdate();

            List<Sample> log = new List<Sample>();
            float angle = 0f;
            const float radius = 1.6f;          //# Range(1.5) 부근 — 경계를 스치도록.
            const float angularSpeed = 90f;     //# deg/s — 영웅이 원점 둘레를 돈다.
            for (int tick = 0; tick < 150; ++tick)
            {
                //# 영웅을 원호로 수동 이동 — dist 가 Range 근처에서 진동하도록.
                angle += angularSpeed * Time.fixedDeltaTime;
                float rad = angle * Mathf.Deg2Rad;
                hero.transform.position = new Vector3(
                    Mathf.Cos(rad) * radius, 0f, Mathf.Sin(rad) * radius);

                yield return new WaitForFixedUpdate();

                bool targetFound = provider.TryFindNearest(
                    monster.transform.position, out _, out _);
                float dist = Vector3.Distance(monster.transform.position, hero.transform.position);
                log.Add(new Sample
                {
                    Tick = tick,
                    MonsterX = monster.transform.position.x,
                    Dist = dist,
                    IsMoving = monMover.IsMoving,
                    InRange = dist <= attacker.Range,
                    TargetFound = targetFound,
                });
            }

            (int toggles, int stuck, float firstStopDist) = DumpAndSummarize("B2", log);

            //# 회귀 잠금 (재보정 2026-06-01) — B 와 동일하게 "빠른 플리커" 간격 기준.
            //# 원호 이동은 radius(1.6) 가 Range(1.5) 를 스치므로 수정 전엔 경계를 매 틱 횡단(per-frame 플리커).
            //# 수정 후 밴드(1.5~2.0) 안에 영웅이 들어오면 _engaged 유지 → 빠른 토글 소멸. 느린 사이클만 잔존 가능.
            int fast = CountFastToggles(log, maxGapTicks: 5);
            Assert.LessOrEqual(fast, 1,
                $"[B2 재현군] 원호 이동 영웅 주변 — per-frame Range 경계 플리커(stutter). " +
                $"빠른토글쌍(간격<5틱)={fast}, 총토글={toggles}(band-cycle 은 정상), " +
                $"접근중 정지 프레임={stuck}, 최초멈춤 dist={firstStopDist:F3}. " +
                $"빠른토글쌍 다수 = dist 가 Range 경계를 반복 횡단 = 히스테리시스 부재.");
        }
    }
}
