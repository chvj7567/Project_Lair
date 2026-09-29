using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Character;
using Object = UnityEngine.Object;

namespace Lair.Tests.PlayMode.Character
{
    //# hero-2d-conversion §9·§11 "테스트 포인트" — 실제 Knight.prefab(2D, Visual2D+Knight_2D.controller)으로
    //# 3D 라이브 프로브(HeroAttackTimingProbePlayTests)와 같은 방식으로 strike/락해제/Spawn전이를 실측한다.
    //# 실측 결과(60fps) — strike(BeginAttack 기준) Slash 0.500s·Stab 0.800s 정확히(프레임 편차 0).
    //# §9 원안의 예측치(0.517/0.817 = 3D 라이브의 "+1프레임" 지연을 2D 에도 그대로 가정)는 실측과 다르다 —
    //# §9 표의 ±1프레임 허용오차 안에는 들지만 실제 중심값이 다르므로 아래 테스트는 실측값(0.500/0.800)을 기준으로 한다.
    //# §5.4 는 "상태 진입이 BeginAttack 1프레임 뒤인 것도 같은 Animator 파이프라 동일"이라 명시했는데 이 실측은 그 서술과
    //# 어긋난다 — 2D Any State 전이(Duration0·ExitTime off, §5.4)가 3D 보다 지연이 짧을 가능성은 있으나 미검증 가설이며,
    //# §5.4·§9 문서 수정 여부는 game-designer 판단 영역(본 파일은 실측값 고정만 담당). 30fps 는 본 스위트에서 측정하지
    //# 않았다 — 3D 라이브는 30fps 에서 0.533s(=0.500+1프레임) 였으므로 2D 30fps 재측정은 별도 확인 필요.
    //# 60fps 1개 프레임레이트만 다룬다 — 3D 프로브의 60/30fps 이중 비교는 "발견" 목적이었고 여기는 "회귀 고정" 목적이라 축소.
    public class HeroKnight2DAttackTimingPlayTests
    {
        private const string PrefabPath = "Assets/_Lair/Art/Characters/Knight.prefab";
        private const string SceneName = "HeroKnight2DTimingScene";
        private const float Fps = 60f;
        private const float FrameSec = 1f / Fps;

        private GameObject _holder;
        private Health _heroHealth;

        public class Probe : MonoBehaviour
        {
            public float StrikeTime = -1f;
            public float EndTime = -1f;
            public void OnAttackStrike() => StrikeTime = Time.time;
            public void OnAttackEnd() => EndTime = Time.time;
            public void OnSpawnAnimEnd() { }
        }

        //# 앞선 테스트가 남긴 Battle/Village 씬 격리 — HeroAttackTimingProbePlayTests 와 동일 이유(§ 그 파일 주석).
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Time.timeScale = 1f;
            Scene scene = SceneManager.CreateScene(SceneName + "_" + DateTime.Now.Ticks);
            SceneManager.SetActiveScene(scene);
            List<Scene> others = new List<Scene>();
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                Scene s = SceneManager.GetSceneAt(i);
                if (s != scene) others.Add(s);
            }
            foreach (Scene s in others)
            {
                AsyncOperation op = SceneManager.UnloadSceneAsync(s);
                if (op != null) yield return op;
            }
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Time.captureDeltaTime = 0f;
            Time.timeScale = 1f;
            if (_holder != null) Object.DestroyImmediate(_holder);
            _holder = null;
            _heroHealth = null;
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
        }

        private static void DisableIfPresent(Behaviour b) { if (b != null) b.enabled = false; }
        private static bool IsIdle(Animator a) => a.IsInTransition(0) == false && a.GetCurrentAnimatorStateInfo(0).IsName("Idle");

        private static IEnumerator WaitForIdle(Animator animator, float timeoutSec)
        {
            float t = 0f;
            while (t < timeoutSec)
            {
                if (IsIdle(animator)) yield break;
                t += Time.deltaTime;
                yield return null;
            }
            Assert.Fail($"{timeoutSec}s 안에 Idle 진입 실패");
        }

        //# 한 공격의 IsAttacking 해제(락 fallback)까지 대기 — 다음 attackVariant 로 넘어가기 전 상태 정리.
        private static IEnumerator WaitForLockRelease(HeroAttackGate gate, float timeoutSec)
        {
            float t = 0f;
            while (t < timeoutSec)
            {
                if (gate.IsAttacking == false) yield break;
                t += Time.deltaTime;
                yield return null;
            }
            Assert.Fail($"{timeoutSec}s 안에 공격 락 해제 실패");
        }

        private (GameObject hero, Animator animator, HeroAttackGate gate, Probe probe) SpawnHero()
        {
#if UNITY_EDITOR
            GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            Assert.IsNotNull(prefab, $"prefab 로드 실패: {PrefabPath}");

            _holder = new GameObject("Knight2DTimingHolder");
            _holder.SetActive(false);
            GameObject hero = Object.Instantiate(prefab, _holder.transform, false);
            DisableIfPresent(hero.GetComponent<AutoCombatAI>());
            DisableIfPresent(hero.GetComponent<HeroSkillRunner>());
            DisableIfPresent(hero.GetComponent<DespawnOnDeath>());

            HeroAttackGate gate = hero.GetComponent<HeroAttackGate>();
            CharacterAttackStrikeRelay relay = hero.GetComponentInChildren<CharacterAttackStrikeRelay>(true);
            Assert.IsNotNull(gate, "HeroAttackGate 존재");
            Assert.IsNotNull(relay, "CharacterAttackStrikeRelay 존재(Visual2D, §7.4)");
            Animator animator = relay.GetComponent<Animator>();
            Assert.IsNotNull(animator, "relay 와 같은 GO(Visual2D) 에 Animator(Knight_2D.controller)");
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;

            Probe probe = relay.gameObject.AddComponent<Probe>();
            _heroHealth = hero.GetComponent<Health>();

            Time.captureDeltaTime = FrameSec;
            _holder.SetActive(true);
            _heroHealth?.SetMax(1000000);

            return (hero, animator, gate, probe);
#else
            Assert.Ignore("AssetDatabase 경로 — in-editor 전용");
            return default;
#endif
        }

        //# §9 "선행 확인" 잔여 — Spawn 클립엔 OnSpawnAnimEnd 이벤트가 없다(HeroClip2DEventBakeTests).
        //# Animator 전이 설정만으로(Duration0·ExitTime1.0, §5.4) Spawn→Idle 이 클립 길이(1.333s) 근방에 자동 전이하는지 확인.
        [UnityTest]
        public IEnumerator Spawn클립_종료후_이벤트없이_Idle로_자동_전이한다()
        {
#if UNITY_EDITOR
            (GameObject _, Animator animator, HeroAttackGate _, Probe probe) = SpawnHero();

            float t = 0f;
            while (t < 2.5f && IsIdle(animator) == false)
            {
                t += Time.deltaTime;
                yield return null;
            }

            Assert.IsTrue(IsIdle(animator), "Spawn 클립 종료 후 Idle 진입(§5.4 전이 설정)");
            Assert.GreaterOrEqual(t, 1.333f - FrameSec * 2f, "클립 길이(1.333s)보다 일찍 끝나면 안 됨");
            Assert.LessOrEqual(t, 1.333f + 0.3f, "클립 종료 직후 지체 없이 전이");
            Assert.Less(probe.EndTime, 0f, "OnSpawnAnimEnd 미배선 — EndTime 은 계속 -1(무관 이벤트 프로브 기본값)");
#else
            yield break;
#endif
        }

        //# §9 불변식 "strike 발행 초(2D)" — Slash01 은 첫 BeginAttack(AttackVariant 0).
        //# 실측(60fps, frame-exact, 편차 0): BeginAttack 기준 0.500s — §5.4 서술과 달리 +1프레임 지연이 없다(위 클래스 주석).
        //# captureDeltaTime 고정-스텝이라 결과가 프레임 단위로 정확히 떨어진다 — 허용오차를 반 프레임으로 좁혀 1프레임
        //# 밀림(0.483/0.517)이 나면 확실히 fail 하도록 한다.
        [UnityTest]
        public IEnumerator Slash01_strike는_BeginAttack_기준_0_5초에_발화하고_OnAttackEnd는_없다()
        {
#if UNITY_EDITOR
            (GameObject _, Animator animator, HeroAttackGate gate, Probe probe) = SpawnHero();
            yield return WaitForIdle(animator, 2.5f);

            float begin = Time.time;
            gate.BeginAttack();
            float t = 0f;
            while (t < 2f)
            {
                t = Time.time - begin;
                if (probe.StrikeTime >= 0f && t > 0.7f) break;
                yield return null;
            }

            Assert.GreaterOrEqual(probe.StrikeTime, 0f, "Slash01 strike 미발화");
            Assert.AreEqual(0.5f, probe.StrikeTime - begin, FrameSec * 0.5f, "Slash01 strike — 실측 중심값(§9 표의 예측 0.517 이 아니라 실측 0.500)");
            Assert.Less(probe.EndTime, 0f, "OnAttackEnd 는 절대 발화하지 않는다(§5.3.1)");
#else
            yield break;
#endif
        }

        //# §9 불변식 — Stab 은 세 번째 BeginAttack(AttackVariant 0→1→2 순환).
        //# 실측(60fps, frame-exact, 편차 0): BeginAttack 기준 0.800s — 위 Slash01 과 동일 이유.
        [UnityTest]
        public IEnumerator Stab_strike는_BeginAttack_기준_0_8초에_발화한다()
        {
#if UNITY_EDITOR
            (GameObject _, Animator animator, HeroAttackGate gate, Probe probe) = SpawnHero();
            yield return WaitForIdle(animator, 2.5f);

            //# variant0(Slash01)·variant1(Slash02) 을 순서대로 소모 — 각 락 해제까지 기다려 다음 공격을 깨끗이 개시.
            for (int i = 0; i < 2; i++)
            {
                gate.BeginAttack();
                yield return WaitForLockRelease(gate, 2.5f);
                yield return WaitForIdle(animator, 1f);
            }

            float begin = Time.time;
            gate.BeginAttack();
            float t = 0f;
            while (t < 2f)
            {
                t = Time.time - begin;
                if (probe.StrikeTime >= 0f && t > 1f) break;
                yield return null;
            }

            Assert.GreaterOrEqual(probe.StrikeTime, 0f, "Stab strike 미발화");
            Assert.AreEqual(0.8f, probe.StrikeTime - begin, FrameSec * 0.5f, "Stab strike — 실측 중심값(§9 표의 예측 0.817 이 아니라 실측 0.800)");
            Assert.Less(probe.EndTime, 0f, "OnAttackEnd 는 절대 발화하지 않는다(§5.3.1)");
#else
            yield break;
#endif
        }

        //# §9 불변식 "공격 락·스폰 게이트(2D)" — OnAttackEnd 미발화이므로 IsAttacking 은 fallback 1.800s(60fps)에만 해제된다.
        [UnityTest]
        public IEnumerator 공격락은_OnAttackEnd_없이_1_8초_fallback으로만_해제된다()
        {
#if UNITY_EDITOR
            (GameObject _, Animator animator, HeroAttackGate gate, Probe probe) = SpawnHero();
            yield return WaitForIdle(animator, 2.5f);

            float begin = Time.time;
            gate.BeginAttack();
            float t = 0f;
            while (t < 2.5f)
            {
                t = Time.time - begin;
                if (gate.IsAttacking == false) break;
                yield return null;
            }

            Assert.IsFalse(gate.IsAttacking, "2.5s 이내 해제돼야 함(락 미해제 회귀)");
            Assert.AreEqual(1.8f, t, FrameSec * 2f, "락 해제는 fallback 1.8s(§5.3.1) — 조기 해제되면 이벤트가 숨어 굽힌 것");
            Assert.Less(probe.EndTime, 0f, "OnAttackEnd 미발화로 fallback 이 유일한 해제 경로였음을 재확인");
#else
            yield break;
#endif
        }
    }
}
