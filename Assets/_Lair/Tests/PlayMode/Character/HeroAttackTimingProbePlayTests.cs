using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;
using Object = UnityEngine.Object;

namespace Lair.Tests.PlayMode.Character
{
    //# 애니 이벤트 수신 프로브 — Animator 와 같은 GO 에 붙으면 실제 relay 와 나란히 같은 이벤트를 받는다.
    public class HeroAnimEventProbe : MonoBehaviour
    {
        public float StrikeTime = -1f;
        public float EndTime = -1f;
        public float SpawnEndTime = -1f;

        public void ResetAttack()
        {
            StrikeTime = -1f;
            EndTime = -1f;
        }

        public void OnAttackStrike() => StrikeTime = Time.time;
        public void OnAttackEnd() => EndTime = Time.time;
        public void OnSpawnAnimEnd() => SpawnEndTime = Time.time;
    }

    //# hero-2d-conversion §9 "선행 확인" — 실제 Knight.prefab(3D)에서 공격 개시→IsAttacking 해제까지 실측.
    //# 해제 원인(OnAttackEnd 이벤트 / _attackEndFallback)을 Library/lair-hero-attack-timing.json 에 기록. 실패해도 TearDown 이 부분 결과를 남긴다.
    public class HeroAttackTimingProbePlayTests
    {
        private const string Tag = "[HeroTimingProbe]";
        private const string PrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";
        private const string ResultPath = "Library/lair-hero-attack-timing.json";
        private const string ProbeSceneName = "HeroTimingProbeScene";
        private const int CyclesPerStep = 3;
        private const float WindowSec = 2.5f;
        private static readonly string[] VariantStates = { "Slash01", "Slash02", "Stab" };
        private static readonly int[] FrameRates = { 60, 30 };

        [Serializable]
        private class Row
        {
            public int fps;
            public int cycle;
            public string clip;
            public bool stateEntered;
            public float stateEnteredT = -1f;
            public float strikeT = -1f;
            public float endEventT = -1f;
            public float releaseT = -1f;
            public string releaseCause;
        }

        [Serializable]
        private class Report
        {
            public string measuredAt;
            public bool completed;
            public string stage;
            public string abortReason;
            public string unloadedScenes;
            public float attackEndFallback = -1f;
            public float spawnEndEventT = -1f;
            public float heroDiedAt = -1f;
            public List<Row> rows = new List<Row>();
        }

        private GameObject _holder;
        private Report _report;
        private Health _heroHealth;

        //# 앞선 스모크 테스트가 남긴 Battle/Village 씬 격리 — 잔존 전투가 영웅을 공격해 DespawnOnDeath 가 Destroy 하던 원인 차단.
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _report = new Report { measuredAt = DateTime.Now.ToString("o"), stage = "setup" };
            Time.timeScale = 1f;

            Scene probeScene = SceneManager.CreateScene(ProbeSceneName + "_" + DateTime.Now.Ticks);
            SceneManager.SetActiveScene(probeScene);
            List<Scene> others = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != probeScene)
                {
                    others.Add(s);
                }
            }
            StringBuilder names = new StringBuilder();
            foreach (Scene s in others)
            {
                names.Append(s.name).Append(';');
                AsyncOperation op = SceneManager.UnloadSceneAsync(s);
                if (op != null)
                {
                    yield return op;
                }
            }
            _report.unloadedScenes = names.ToString();
            Debug.Log($"{Tag} setup — 격리 씬 활성, 언로드: {_report.unloadedScenes}");

            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (_heroHealth != null)
            {
                _heroHealth.OnDied -= HandleHeroDied;
            }
            WriteReport();
            if (_holder != null)
            {
                Object.DestroyImmediate(_holder);
            }
            _holder = null;
            _heroHealth = null;
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        private void HandleHeroDied()
        {
            _report.heroDiedAt = Time.time;
            Debug.LogWarning($"{Tag} 영웅 사망 감지 at {Time.time:F3} (stage={_report.stage}) — 측정 오염");
        }

        private void WriteReport()
        {
            if (_report == null)
                return;
            try
            {
                File.WriteAllText(ResultPath, JsonUtility.ToJson(_report, true), new UTF8Encoding(false));
                Debug.Log($"{Tag} 결과 기록 → {ResultPath} (completed={_report.completed}, stage={_report.stage}, rows={_report.rows.Count})");
            }
            catch (Exception ex)
            {
                Debug.LogError($"{Tag} 결과 기록 실패: {ex}");
            }
        }

        private void Stage(string stage)
        {
            _report.stage = stage;
            Debug.Log($"{Tag} stage={stage}");
        }

        //# 측정 불가 상황은 예외 대신 사유를 남기고 실패 — TearDown 이 부분 결과를 JSON 에 기록.
        private void Abort(string reason)
        {
            _report.abortReason = reason;
            Debug.LogError($"{Tag} 중단 — stage={_report.stage}, {reason}");
            Assert.Fail($"{Tag} stage={_report.stage}: {reason}");
        }

        [UnityTest]
        [Category("Probe")]
        public IEnumerator 실측_3D_Knight_공격종료_해제원인_이벤트_or_fallback()
        {
#if UNITY_EDITOR
            Stage("load-prefab");
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (prefab == null)
            {
                Abort($"prefab 로드 실패: {PrefabPath}");
            }

            //# 비활성 부모 아래 생성 → Awake 전에 AI/스킬/사망 반환 비활성화(게이트·애니 경로만 격리).
            Stage("instantiate");
            _holder = new GameObject("HeroTimingProbeHolder");
            _holder.SetActive(false);
            GameObject hero = Object.Instantiate(prefab, _holder.transform, false);
            DisableIfPresent(hero.GetComponent<AutoCombatAI>());
            DisableIfPresent(hero.GetComponent<HeroSkillRunner>());
            DisableIfPresent(hero.GetComponent<DespawnOnDeath>());

            HeroAttackGate gate = hero.GetComponent<HeroAttackGate>();
            CharacterAttackStrikeRelay relay = hero.GetComponentInChildren<CharacterAttackStrikeRelay>(true);
            if (gate == null || relay == null)
            {
                Abort($"컴포넌트 누락 gate={gate != null} relay={relay != null}");
            }
            Animator animator = relay.GetComponent<Animator>();
            if (animator == null)
            {
                Abort("relay 와 같은 GO 에 Animator 없음");
            }
            //# 카메라 없는 씬에서 컬링으로 애니가 멈춰 이벤트 미발화 → 오판하는 것 방지.
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            HeroAnimEventProbe probe = relay.gameObject.AddComponent<HeroAnimEventProbe>();
            _report.attackEndFallback = ReadFallback(gate);

            _heroHealth = hero.GetComponent<Health>();
            if (_heroHealth != null)
            {
                _heroHealth.OnDied += HandleHeroDied;
            }

            Stage("activate");
            Time.captureDeltaTime = 1f / FrameRates[0];
            _holder.SetActive(true);
            if (_heroHealth != null)
            {
                _heroHealth.SetMax(1000000);
            }

            //# Spawn 종료 대기 — 상한 3s.
            Stage("wait-spawn");
            float spawnWait = 0f;
            float spawnBegin = Time.time;
            while (spawnWait < 3f)
            {
                if (animator == null)
                {
                    Abort($"Spawn 대기 중 Animator 파괴 (heroDiedAt={_report.heroDiedAt:F3})");
                }
                if (IsIdle(animator))
                    break;
                spawnWait += Time.deltaTime;
                yield return null;
            }
            _report.spawnEndEventT = probe != null && probe.SpawnEndTime >= 0f ? probe.SpawnEndTime - spawnBegin : -1f;
            if (IsIdle(animator) == false)
            {
                Abort("Spawn→Idle 진입 실패");
            }

            foreach (int fps in FrameRates)
            {
                Time.captureDeltaTime = 1f / fps;
                yield return null;
                for (int cycle = 0; cycle < CyclesPerStep; cycle++)
                {
                    for (int v = 0; v < VariantStates.Length; v++)
                    {
                        Row row = new Row { fps = fps, cycle = cycle, clip = VariantStates[v] };
                        Stage($"measure fps={fps} cycle={cycle} {row.clip}");
                        _report.rows.Add(row);
                        IEnumerator measure = MeasureOne(gate, animator, probe, row);
                        while (measure.MoveNext())
                        {
                            yield return measure.Current;
                        }
                        if (animator == null)
                        {
                            Abort($"측정 중 Animator 파괴 (heroDiedAt={_report.heroDiedAt:F3})");
                        }
                        Debug.Log($"{Tag} fps={fps} cycle={cycle} {row.clip}: entered={row.stateEntered} " +
                                  $"strike={row.strikeT:F3} endEvent={row.endEventT:F3} release={row.releaseT:F3} cause={row.releaseCause}");
                    }
                }
            }

            Stage("done");
            _report.completed = true;

            //# 측정 유효성만 검사 — 해제 원인(event/fallback)은 결과값이지 실패 사유가 아니다.
            foreach (Row r in _report.rows)
            {
                Assert.IsTrue(r.stateEntered, $"fps={r.fps} cycle={r.cycle} {r.clip} 상태 미진입 — variant 순서 가정 붕괴");
                Assert.IsTrue(r.strikeT >= 0f, $"fps={r.fps} cycle={r.cycle} {r.clip} strike 미발화 — 애니 정지(측정 무효)");
                Assert.IsTrue(r.releaseT >= 0f, $"fps={r.fps} cycle={r.cycle} {r.clip} IsAttacking 미해제");
            }
            Assert.IsTrue(_report.heroDiedAt < 0f, "측정 중 영웅 사망 — 결과 오염");
#else
            Assert.Ignore("AssetDatabase 경로 — in-editor 전용 프로브");
            yield break;
#endif
        }

        private static void DisableIfPresent(Behaviour b)
        {
            if (b != null)
            {
                b.enabled = false;
            }
        }

        //# 공격 1회 — 개시 시각 기준 상대초. WindowSec 동안 늦은 이벤트까지 관찰. 파괴 감지 시 즉시 종료(호출부가 Abort).
        private static IEnumerator MeasureOne(HeroAttackGate gate, Animator animator, HeroAnimEventProbe probe, Row row)
        {
            probe.ResetAttack();
            float begin = Time.time;
            gate.BeginAttack();

            while (Time.time - begin < WindowSec)
            {
                yield return null;
                if (animator == null || gate == null || probe == null)
                    yield break;
                float t = Time.time - begin;
                if (row.stateEntered == false && IsState(animator, row.clip))
                {
                    row.stateEntered = true;
                    row.stateEnteredT = t;
                }
                if (row.releaseT < 0f && gate.IsAttacking == false)
                {
                    row.releaseT = t;
                }
            }

            row.strikeT = probe.StrikeTime >= 0f ? probe.StrikeTime - begin : -1f;
            row.endEventT = probe.EndTime >= 0f ? probe.EndTime - begin : -1f;
            //# 이벤트 경로면 해제는 이벤트 프레임(관찰은 다음 프레임) — 이벤트가 관찰 해제 이전이면 event.
            bool byEvent = row.endEventT >= 0f && row.releaseT >= 0f && row.endEventT <= row.releaseT + 0.0001f;
            row.releaseCause = row.releaseT < 0f ? "none" : (byEvent ? "OnAttackEnd" : "fallback");
            if (byEvent)
            {
                row.releaseT = row.endEventT;
            }

            //# 다음 공격 전 Idle 복귀 확인 — 상한 1s.
            float wait = 0f;
            while (wait < 1f)
            {
                if (animator == null || IsIdle(animator))
                    yield break;
                wait += Time.deltaTime;
                yield return null;
            }
        }

        private static bool IsIdle(Animator animator)
            => animator.IsInTransition(0) == false && animator.GetCurrentAnimatorStateInfo(0).IsName("Idle");

        private static bool IsState(Animator animator, string state)
            => animator.GetCurrentAnimatorStateInfo(0).IsName(state) || animator.GetNextAnimatorStateInfo(0).IsName(state);

        private static float ReadFallback(HeroAttackGate gate)
        {
            System.Reflection.FieldInfo f = typeof(HeroAttackGate)
                .GetField("_attackEndFallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return f != null ? (float)f.GetValue(gate) : -1f;
        }
    }
}
