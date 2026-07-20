using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode.Character
{
    //# 영웅 애니↔게임플레이 타이밍 동기화(hero-animation-timing-sync) 통합/회귀 — Addressable 비의존, GameObject 직접 합성.
    //# 검증 축:
    //#   (1) strike relay 위임 — Visual 자식 relay.OnAttackStrike/OnAttackEnd/OnSpawnAnimEnd → GetComponentInParent 로 루트 도달
    //#   (2) HeroAttackGate _attackEndFallback 타임아웃 + OnEnable 리셋 (Time.time/Update 의존 → PlayMode)
    //#   (3) 풀 재사용 회귀 — 사망→OnDisable→OnEnable 시 게이트 락/스폰게이트/march게이트 잔존 0
    //#   (4) 영웅/몬스터 분기 — 게이트 미부착 시 즉시 경로 보존
    //# 모든 루프 유한(명시 프레임/시간 상한). 무한루프/긴 폴링 없음 — 에디터 프리즈 방지.
    public class HeroAnimationTimingSyncPlayTests
    {
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
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
            CharacterRegistry.Heroes.Clear();
            CharacterRegistry.Monsters.Clear();
            Time.timeScale = 1f;
        }

        //# 루트(Knight) — DeferStrike=true MeleeAttacker + HeroAttackGate. AutoCombatAI 는 케이스별 선택 부착.
        //# RequireComponent 충족 위해 Health/SimpleMover/SimpleRotator 도 항상 부착(AutoCombatAI 부착 대비).
        private GameObject CreateHeroRoot(out MeleeAttacker attacker, out HeroAttackGate gate)
        {
            GameObject root = new GameObject("HeroRootUT");
            //# 프로덕션 영웅 루트와 동형 — relay 가 부모 Character 로케이터로 IAttackGate 를 해석하므로 부착 필수.
            //# (AutoCombatAI 등 RequireComponent(Character) 소비자를 붙이지 않는 케이스도 커버.)
            root.AddComponent<LairCharacter>();
            Health hp = root.AddComponent<Health>();
            hp.SetMax(10000);
            root.AddComponent<SimpleMover>();
            root.AddComponent<SimpleRotator>();
            attacker = root.AddComponent<MeleeAttacker>();
            attacker.Configure(2.0f, 1.0f, 50);
            SetDeferStrike(attacker, true);
            gate = root.AddComponent<HeroAttackGate>();
            _spawned.Add(root);
            return root;
        }

        //# Visual 자식 — Animator + relay. relay.Awake 가 GetComponentInParent 로 루트 컴포넌트 잡음.
        private CharacterAttackStrikeRelay AddVisualWithRelay(GameObject root)
        {
            GameObject visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform, false);
            visual.AddComponent<Animator>();
            return visual.AddComponent<CharacterAttackStrikeRelay>();
        }

        //# 영웅 한정 DeferStrike SerializeField 토글 — private 라 reflection.
        private static void SetDeferStrike(MeleeAttacker attacker, bool value)
        {
            System.Reflection.FieldInfo f = typeof(MeleeAttacker)
                .GetField("_deferStrike", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f.SetValue(attacker, value);
        }

        //# HeroAttackGate._attackEndFallback 단축 — 타임아웃 케이스를 짧게(테스트 시간 상한 준수).
        private static void SetAttackEndFallback(HeroAttackGate gate, float value)
        {
            System.Reflection.FieldInfo f = typeof(HeroAttackGate)
                .GetField("_attackEndFallback", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            f.SetValue(gate, value);
        }

        //# ===== (1) strike relay 위임 =====

        //# relay.OnAttackStrike() → GetComponentInParent<MeleeAttacker>().TryApplyStrike → 캐싱 타겟에 데미지 도달.
        [UnityTest]
        public IEnumerator Relay_OnAttackStrike_루트_TryApplyStrike_데미지도달()
        {
            GameObject root = CreateHeroRoot(out MeleeAttacker attacker, out _);
            CharacterAttackStrikeRelay relay = AddVisualWithRelay(root);
            yield return null;   //# Awake/OnEnable 완료.

            //# 타겟 — IHealth 역할 Health.
            GameObject targetGo = new GameObject("StrikeTarget");
            targetGo.transform.position = new Vector3(1f, 0, 0);
            Health targetHp = targetGo.AddComponent<Health>();
            targetHp.SetMax(500);
            _spawned.Add(targetGo);

            //# 개시 — 캐싱.
            bool began = attacker.TryBeginAttack(targetHp, root.transform.position, targetGo.transform.position, now: Time.time);
            Assert.IsTrue(began, "사거리 내 개시 성공");
            Assert.AreEqual(500, targetHp.Current, "개시 시점엔 데미지 없음");

            //# strike 이벤트 — relay 가 루트 TryApplyStrike 위임.
            relay.OnAttackStrike();

            Assert.AreEqual(450, targetHp.Current, "relay→루트 TryApplyStrike→데미지 50 적용");
            yield return null;
        }

        //# relay.OnAttackEnd() → GetComponentInParent<IAttackGate>().EndAttack → IsAttacking=false.
        [UnityTest]
        public IEnumerator Relay_OnAttackEnd_루트게이트_EndAttack_IsAttacking해제()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            CharacterAttackStrikeRelay relay = AddVisualWithRelay(root);
            yield return null;

            gate.BeginAttack();
            Assert.IsTrue(gate.IsAttacking, "개시 후 공격 중");

            relay.OnAttackEnd();

            Assert.IsFalse(gate.IsAttacking, "relay→루트 게이트 EndAttack→IsAttacking 해제");
            yield return null;
        }

        //# relay.OnSpawnAnimEnd() → 루트 AutoCombatAI 스폰게이트 open. AutoCombatAI 부착 구성으로 검증.
        [UnityTest]
        public IEnumerator Relay_OnSpawnAnimEnd_AutoCombatAI_스폰게이트_open()
        {
            GameObject root = CreateHeroRoot(out _, out _);
            AutoCombatAI ai = root.AddComponent<AutoCombatAI>();
            CharacterAttackStrikeRelay relay = AddVisualWithRelay(root);
            yield return null;   //# OnEnable → _spawnGateOpen=false.

            Assert.IsFalse(GetSpawnGateOpen(ai), "스폰 직후 게이트 닫힘");

            relay.OnSpawnAnimEnd();

            Assert.IsTrue(GetSpawnGateOpen(ai), "relay→AutoCombatAI.OpenSpawnGate → 게이트 open");
            yield return null;
        }

        private static bool GetSpawnGateOpen(AutoCombatAI ai)
        {
            System.Reflection.FieldInfo f = typeof(AutoCombatAI)
                .GetField("_spawnGateOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)f.GetValue(ai);
        }

        //# relay 가 게이트/공격자 없는 루트(몬스터)여도 NRE 없이 무시(?. 위임) — 공통 컴포넌트 안전성.
        [UnityTest]
        public IEnumerator Relay_루트에_게이트없어도_예외없음()
        {
            GameObject root = new GameObject("BareRootUT");
            _spawned.Add(root);
            CharacterAttackStrikeRelay relay = AddVisualWithRelay(root);
            yield return null;

            Assert.DoesNotThrow(() => relay.OnAttackStrike());
            Assert.DoesNotThrow(() => relay.OnAttackEnd());
            Assert.DoesNotThrow(() => relay.OnSpawnAnimEnd());
            yield return null;
        }

        //# ===== (2) HeroAttackGate fallback 타임아웃 + OnEnable 리셋 =====

        //# _attackEndFallback 경과 시 OnAttackEnd 누락이어도 IsAttacking 강제 해제(안전망 §3.4).
        [UnityTest]
        public IEnumerator HeroAttackGate_fallback경과시_IsAttacking_강제해제()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            SetAttackEndFallback(gate, 0.2f);   //# 짧게 — 테스트 시간 상한.
            yield return null;   //# OnEnable.

            gate.BeginAttack();
            Assert.IsTrue(gate.IsAttacking);

            //# fallback(0.2s) 초과까지 유한 대기 — 상한 1.0s 보호.
            float elapsed = 0f;
            while (gate.IsAttacking && elapsed < 1.0f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.IsFalse(gate.IsAttacking, "fallback 0.2s 경과 → Update 가 IsAttacking 강제 해제");
        }

        //# OnEnable 리셋 — 공격 중(IsAttacking=true) 상태로 비활성→재활성 시 false 복귀(풀 재사용 락 잔존 0).
        [UnityTest]
        public IEnumerator HeroAttackGate_OnEnable_IsAttacking_리셋()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            yield return null;

            gate.BeginAttack();
            Assert.IsTrue(gate.IsAttacking, "공격 중 상태로 풀 반환되는 상황 시뮬");

            root.SetActive(false);
            yield return null;
            root.SetActive(true);
            yield return null;

            Assert.IsFalse(gate.IsAttacking, "OnEnable 리셋 → 공격 락 잔존 0");
        }

        //# ===== (3) 풀 재사용 회귀 — 게이트 3종 잔존 0 =====

        //# 사망→비활성→재활성 반복 시 AutoCombatAI 스폰게이트가 매 OnEnable 닫힘으로 리셋(누수 0).
        [UnityTest]
        public IEnumerator 풀재사용_반복_AutoCombatAI_스폰게이트_매번_리셋()
        {
            GameObject root = CreateHeroRoot(out _, out _);
            AutoCombatAI ai = root.AddComponent<AutoCombatAI>();
            AddVisualWithRelay(root);
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                //# open 시킨 뒤(전 사이클 잔존 시뮬).
                ai.OpenSpawnGate();
                Assert.IsTrue(GetSpawnGateOpen(ai), $"{i}회차 open");

                root.SetActive(false);
                yield return null;
                root.SetActive(true);
                yield return null;

                Assert.IsFalse(GetSpawnGateOpen(ai), $"{i}회차 재활성 후 스폰게이트 닫힘 리셋 — 잔존 0");
            }
        }

        //# HeroEntryDriver._marchGateOpen 풀 재사용 리셋 — 반복 enable 시 매번 닫힘(march 락 잔존 0).
        [UnityTest]
        public IEnumerator 풀재사용_반복_HeroEntryDriver_march게이트_매번_리셋()
        {
            GameObject root = CreateHeroRoot(out _, out _);
            HeroEntryDriver driver = root.AddComponent<HeroEntryDriver>();
            yield return null;

            for (int i = 0; i < 3; i++)
            {
                driver.OpenMarchGate();
                Assert.IsTrue(GetMarchGateOpen(driver), $"{i}회차 open");

                root.SetActive(false);
                yield return null;
                root.SetActive(true);
                yield return null;

                Assert.IsFalse(GetMarchGateOpen(driver), $"{i}회차 재활성 후 march게이트 닫힘 리셋 — 잔존 0");
            }
        }

        private static bool GetMarchGateOpen(HeroEntryDriver driver)
        {
            System.Reflection.FieldInfo f = typeof(HeroEntryDriver)
                .GetField("_marchGateOpen", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)f.GetValue(driver);
        }

        //# 사망 후 재사용 — Health 풀 복원 + 게이트 IsAttacking 리셋 동시 정상(누수 0).
        [UnityTest]
        public IEnumerator 사망후_풀재사용_게이트_IsAttacking_리셋()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            Health hp = root.GetComponent<Health>();
            yield return null;

            gate.BeginAttack();
            hp.TakeDamage(hp.Current);   //# 사망.
            Assert.IsFalse(hp.IsAlive);

            root.SetActive(false);
            yield return null;
            root.SetActive(true);
            yield return null;

            Assert.IsFalse(gate.IsAttacking, "재사용 시 게이트 공격 락 잔존 0");
            Assert.IsTrue(hp.IsAlive, "Health 풀 재사용으로 HP 복원");
        }

        //# ===== (4) 영웅/몬스터 분기 — 게이트 미부착 즉시 경로 보존 =====

        //# 몬스터(DeferStrike=false, 게이트 미부착) 합성 — AutoCombatAI 가 게이트 null 로 잡아 보류 로직 미적용.
        //# 검증: 게이트 미부착 시 relay.OnAttackEnd 가 NRE 없이 no-op(몬스터에 게이트 없음).
        [UnityTest]
        public IEnumerator 몬스터구성_게이트미부착_relay_위임_no_op()
        {
            GameObject root = new GameObject("MonsterRootUT");
            Health hp = root.AddComponent<Health>();
            hp.SetMax(100);
            root.AddComponent<SimpleMover>();
            root.AddComponent<SimpleRotator>();
            MeleeAttacker attacker = root.AddComponent<MeleeAttacker>();
            attacker.Configure(1.5f, 1.0f, 10);
            SetDeferStrike(attacker, false);   //# 몬스터 — 즉시 경로.
            _spawned.Add(root);

            CharacterAttackStrikeRelay relay = AddVisualWithRelay(root);
            yield return null;

            //# 게이트 미부착이라 relay 의 _gate 는 null → OnAttackEnd no-op(예외 없음).
            Assert.DoesNotThrow(() => relay.OnAttackEnd());

            //# DeferStrike=false 이므로 즉시 경로 TryAttack 이 데미지 적용(현행 보존).
            GameObject targetGo = new GameObject("MonTarget");
            targetGo.transform.position = new Vector3(1f, 0, 0);
            Health targetHp = targetGo.AddComponent<Health>();
            targetHp.SetMax(100);
            _spawned.Add(targetGo);

            bool hit = attacker.TryAttack(targetHp, root.transform.position, targetGo.transform.position, now: Time.time);
            Assert.IsTrue(hit, "몬스터 즉시 경로 데미지 적용");
            Assert.AreEqual(90, targetHp.Current, "TryAttack 즉시 10 데미지 — 현행 보존");
            yield return null;
        }

        //# ===== (5) 공격 애니 중 이동 잠금 회귀 (사용자 요구: 공격 중 정지 / 끝나고 이동) =====

        //# 등록 헬퍼 — 사거리 밖 Engaging 몬스터 1마리. 영웅이 평소엔 MoveTo 로 추적할 대상.
        //# AutoCombatAI 가 IsAttacking 동안엔 이 대상이 있어도 이동을 시작하지 않아야 함.
        private GameObject AddEngagingMonster(Vector3 pos, int hp)
        {
            GameObject go = new GameObject("EngagingMonsterUT");
            go.transform.position = pos;
            Health h = go.AddComponent<Health>();
            h.SetMax(hp);
            _spawned.Add(go);
            CharacterRegistry.RegisterMonster(go.transform, h);
            CharacterRegistry.SetMonsterEngaging(go.transform, true);
            return go;
        }

        //# 정상 케이스 — IsAttacking 동안 AutoCombatAI 가 _mover.Stop 유지(사거리 밖 타겟 있어도 이동 시작 안 함).
        //# 사용자 증상("공격 직후 애니 없이 미끄러짐")의 게임플레이측 잠금 회귀 박제.
        [UnityTest]
        public IEnumerator IsAttacking_동안_AutoCombatAI_이동잠금_사거리밖타겟있어도_정지()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            root.transform.position = Vector3.zero;
            root.AddComponent<HeroTargetProvider>();
            AutoCombatAI ai = root.AddComponent<AutoCombatAI>();
            SimpleMover mover = root.GetComponent<SimpleMover>();
            yield return null;   //# Awake/OnEnable.

            //# 스폰 게이트 open — 스폰 모션 보류가 이동잠금 검증을 가리지 않도록.
            ai.OpenSpawnGate();

            //# 사거리(2.0) 밖 Engaging 몬스터 — 평소라면 MoveTo 추적 대상.
            AddEngagingMonster(new Vector3(5f, 0f, 0f), 500);

            //# 공격 개시 상태 강제.
            gate.BeginAttack();
            Assert.IsTrue(gate.IsAttacking, "공격 중 상태 진입");

            //# 여러 프레임 경과 — IsAttacking 동안 이동이 시작되면 안 됨.
            for (int i = 0; i < 5; i++)
                yield return null;

            Assert.IsFalse(mover.IsMoving, "IsAttacking 동안 사거리 밖 타겟이 있어도 이동 시작 안 함(이동 잠금)");
        }

        //# 엣지 케이스 — 공격 종료(EndAttack) 후엔 잠금 해제되어 사거리 밖 타겟으로 이동 재개.
        //# "끝나고 움직이도록" 의 해제 시점 박제.
        [UnityTest]
        public IEnumerator EndAttack_후_AutoCombatAI_이동잠금해제_사거리밖타겟으로_이동재개()
        {
            GameObject root = CreateHeroRoot(out _, out HeroAttackGate gate);
            root.transform.position = Vector3.zero;
            root.AddComponent<HeroTargetProvider>();
            AutoCombatAI ai = root.AddComponent<AutoCombatAI>();
            SimpleMover mover = root.GetComponent<SimpleMover>();
            yield return null;

            ai.OpenSpawnGate();
            AddEngagingMonster(new Vector3(5f, 0f, 0f), 500);

            gate.BeginAttack();
            yield return null;
            Assert.IsFalse(mover.IsMoving, "공격 중 정지");

            //# 클립 종료 신호 → 잠금 해제.
            gate.EndAttack();
            Assert.IsFalse(gate.IsAttacking, "EndAttack 후 공격 락 해제");
            yield return null;

            Assert.IsTrue(mover.IsMoving, "EndAttack 후 사거리 밖 타겟으로 이동 재개");
        }
    }
}
