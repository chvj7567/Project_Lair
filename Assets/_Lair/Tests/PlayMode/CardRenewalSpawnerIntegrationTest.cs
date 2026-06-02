using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 카드 리뉴얼 v0.6 본격 스위트 (PlayMode) — Spawner 주기·출력 변경 시너지 통합 검증.
    //# ContinuousSpawnIntegrationTest 는 ApplyMonsterStats / RegisterMonsterTypeBuff / Plague Spawner 분포 / Debuff 5장 카운트만 다룸.
    public class CardRenewalSpawnerIntegrationTest : BattlePlayTestBase
    {
        [SetUp]
        public void SetUp()
        {
            //# 정적 레지스트리 — 테스트 간 잔존 차단 (Tier3 격리 테스트의 소급 순회 격리).
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
            foreach (GameObject go in _isolated)
                if (go != null) Object.Destroy(go);
            _isolated.Clear();
            CharacterRegistry.Monsters.Clear();
            CharacterRegistry.Heroes.Clear();
        }

        private static T GetPrivate<T>(object target, string field)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            return (T)fi.GetValue(target);
        }

        //# ===== 1. SpawnerHaste 1픽 — 모든 Spawner 의 SpawnPeriod ×0.8 =====

        [UnityTest]
        public IEnumerator SpawnerHaste_1픽_모든_Spawner_주기_0점8_곱연산()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 존재");

            //# 비동기 Start 완료 대기 — _ctx 가 인스턴스화될 때까지.
            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<IBattleContext>(bc, "_ctx") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx, "_ctx 인스턴스화");

            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");

            //# 1픽 전 주기 스냅샷.
            float[] before = new float[spawners.Length];
            for (int i = 0; i < spawners.Length; ++i)
                before[i] = spawners[i].SpawnPeriod;

            //# SpawnerHaste 1픽 — ctx.ScaleAllSpawnerPeriods(0.8) 1회 호출.
            new SpawnerHasteEffect().Apply(ctx);

            //# 모든 Spawner 주기 ×0.8.
            for (int i = 0; i < spawners.Length; ++i)
            {
                float expected = Mathf.Max(0.05f, before[i] * 0.8f);
                Assert.AreEqual(expected, spawners[i].SpawnPeriod, 0.01f,
                    $"Spawner[{i}] 주기 — {before[i]} × 0.8 = {expected}");
            }
        }

        //# ===== 2. SpawnerHaste 3픽 + Swarm Tier2 — 곱연산 누적 회귀 =====

        //# 기획서 §9.6: SpawnerHaste 3픽 + Swarm Tier2 = ×0.8³ × ×0.85 = ×0.435. Phantom 6.0s → 2.61s.
        //# 본 회귀는 *시스템상 곱연산이 정확히 누적되는지* (×0.435 의 fp 정합) 검증.
        [UnityTest]
        public IEnumerator SpawnerHaste_3픽_Swarm_Tier2_곱연산_누적_0점435()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc, "BattleController 존재");

            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<IBattleContext>(bc, "_ctx") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx);

            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            float[] before = new float[spawners.Length];
            for (int i = 0; i < spawners.Length; ++i)
                before[i] = spawners[i].SpawnPeriod;

            //# SpawnerHaste 3픽 — ×0.8³ = ×0.512.
            SpawnerHasteEffect haste = new SpawnerHasteEffect();
            haste.Apply(ctx);
            haste.Apply(ctx);
            haste.Apply(ctx);
            //# Swarm Tier2 — 추가 ×0.85 → 총 ×0.435.
            new SwarmSynergyTier2().Apply(ctx);

            float totalMul = 0.8f * 0.8f * 0.8f * 0.85f;   //# ≈ 0.4352

            for (int i = 0; i < spawners.Length; ++i)
            {
                //# Spawner.ScalePeriod 의 floor 0.05s 클램프 고려.
                float expected = Mathf.Max(0.05f, before[i] * totalMul);
                Assert.AreEqual(expected, spawners[i].SpawnPeriod, 0.05f,
                    $"Spawner[{i}] — {before[i]} × {totalMul:F4} = {expected:F3}");
            }
        }

        //# ===== 3. Swarm Tier3 — 모든 Spawner OutputCount +1 =====

        [UnityTest]
        public IEnumerator Swarm_Tier3_IncrementAllSpawnerOutputs_1_모든_Spawner_출력_플러스1()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc);

            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<IBattleContext>(bc, "_ctx") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx);

            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            int[] before = new int[spawners.Length];
            for (int i = 0; i < spawners.Length; ++i)
                before[i] = spawners[i].OutputCount;

            new SwarmSynergyTier3().Apply(ctx);

            for (int i = 0; i < spawners.Length; ++i)
                Assert.AreEqual(before[i] + 1, spawners[i].OutputCount,
                    $"Spawner[{i}] OutputCount {before[i]} → {before[i] + 1}");
        }

        //# ===== 4. Tank Tier3 — Wisp·Wraith HP ×1.4 추가 내구 버프 (캡 +6 교체, 기획서 §2) =====

        //# 캡 제거 리뉴얼: 구 캡_24로_상승 테스트를 신효과(HP ×1.4 등록)로 대체.
        [UnityTest]
        public IEnumerator Tank_Tier3_발화_시_Wisp_Wraith_HP_1점4_등록()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc);

            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<IBattleContext>(bc, "_ctx") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx);

            float wispBefore = TypeHpMul(bc, EMonster.Wisp);
            float wraithBefore = TypeHpMul(bc, EMonster.Wraith);

            new TankSynergyTier3().Apply(ctx);

            Assert.AreEqual(wispBefore * 1.4f, TypeHpMul(bc, EMonster.Wisp), 0.001f,
                "Tier3 후 Wisp HP 배율 ×1.4 곱연산");
            Assert.AreEqual(wraithBefore * 1.4f, TypeHpMul(bc, EMonster.Wraith), 0.001f,
                "Tier3 후 Wraith HP 배율 ×1.4 곱연산");
        }

        //# ===== 5. Tank Tier3 + Swarm Tier3 동시 적용 (디자인상 불가지만 안전망 회귀) =====

        //# 기획서 §9.5: "원리상 불가" 단정. 안전 가드로 두 Tier3 가 동시 발동되어도 예외 없이 누적.
        //# 캡 제거 후 Tank Tier3 는 HP 표면, Swarm Tier3 는 Output 표면 — 서로 독립 누적.
        [UnityTest]
        public IEnumerator Tank_Tier3_와_Swarm_Tier3_동시_적용도_안전_누적()
        {
            yield return EnsureCHMReady();
            yield return SceneManager.LoadSceneAsync("Battle");
            yield return null;

            BattleController bc = null;
            float wait = 0f;
            while (wait < 4f)
            {
                bc = Object.FindFirstObjectByType<BattleController>();
                if (bc != null)
                    break;
                wait += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(bc);

            float elapsed = 0f;
            while (elapsed < 4f && GetPrivate<IBattleContext>(bc, "_ctx") == null)
            {
                elapsed += Time.unscaledDeltaTime;
                yield return null;
            }
            IBattleContext ctx = GetPrivate<IBattleContext>(bc, "_ctx");
            Assert.IsNotNull(ctx);

            float wispBefore = TypeHpMul(bc, EMonster.Wisp);
            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            int[] outputBefore = new int[spawners.Length];
            for (int i = 0; i < spawners.Length; ++i)
                outputBefore[i] = spawners[i].OutputCount;

            Assert.DoesNotThrow(() =>
            {
                new TankSynergyTier3().Apply(ctx);
                new SwarmSynergyTier3().Apply(ctx);
            }, "두 Tier3 동시 적용해도 예외 없음 (§9.5 안전 가드)");

            Assert.AreEqual(wispBefore * 1.4f, TypeHpMul(bc, EMonster.Wisp), 0.001f, "Wisp HP ×1.4");
            for (int i = 0; i < spawners.Length; ++i)
                Assert.AreEqual(outputBefore[i] + 1, spawners[i].OutputCount, "Output +1");
        }

        //# ===== 6. Tank Tier3 — 단독 / 곱연산 누적 / 종 격리 (기획서 §2.2 곱연산 검산) =====

        //# 엣지 — Tier3 단독 발화(Tier1 미발화) 시 Wisp·Wraith HP 정확히 ×1.4 단독.
        //# 격리 컨트롤러로 _typeModifiers 깨끗한 상태에서 Tier3 만 적용.
        [UnityTest]
        public IEnumerator Tank_Tier3_단독_발화_시_Wisp_Wraith_정확히_1점4()
        {
            BattleController bc = CreateIsolatedController();
            yield return null;

            new TankSynergyTier3().Apply(BuildCtx(bc));

            Assert.AreEqual(1.4f, TypeHpMul(bc, EMonster.Wisp), 0.0001f, "Tier1 미발화 — Wisp ×1.4 단독");
            Assert.AreEqual(1.4f, TypeHpMul(bc, EMonster.Wraith), 0.0001f, "Tier1 미발화 — Wraith ×1.4 단독");
        }

        //# 회귀 (고가치) — Tier1(×1.3) 발화 후 Tier3(×1.4) 발화 시 동일 HP 표면 곱연산 = ×1.82 (기획서 §2.2(a)).
        [UnityTest]
        public IEnumerator Tank_Tier1_후_Tier3_HP_표면_곱연산_1점82()
        {
            BattleController bc = CreateIsolatedController();
            yield return null;

            IBattleContext ctx = BuildCtx(bc);
            new TankSynergyTier1().Apply(ctx);   //# Wisp·Wraith HP ×1.3
            new TankSynergyTier3().Apply(ctx);   //# 같은 HP 표면에 ×1.4 곱

            Assert.AreEqual(1.82f, TypeHpMul(bc, EMonster.Wisp), 0.001f, "Tier1×Tier3 = 1.3×1.4 = 1.82 (Wisp)");
            Assert.AreEqual(1.82f, TypeHpMul(bc, EMonster.Wraith), 0.001f, "Tier1×Tier3 = 1.82 (Wraith)");
        }

        //# 회귀 — Tier3 적용 순서 무관(곱셈 가환): Tier3 먼저 → Tier1 도 ×1.82 동일.
        [UnityTest]
        public IEnumerator Tank_Tier3_먼저_Tier1_나중도_곱연산_1점82_순서무관()
        {
            BattleController bc = CreateIsolatedController();
            yield return null;

            IBattleContext ctx = BuildCtx(bc);
            new TankSynergyTier3().Apply(ctx);
            new TankSynergyTier1().Apply(ctx);

            Assert.AreEqual(1.82f, TypeHpMul(bc, EMonster.Wisp), 0.001f, "순서 무관 — Wisp ×1.82");
        }

        //# 엣지 — Tier3 HP 버프는 Reaper 등 Wisp·Wraith 외 종의 HP 배율을 건드리지 않는다 (종 격리).
        [UnityTest]
        public IEnumerator Tank_Tier3_Wisp_Wraith_외_종_HP_배율_불변()
        {
            BattleController bc = CreateIsolatedController();
            yield return null;

            new TankSynergyTier3().Apply(BuildCtx(bc));

            Assert.AreEqual(1f, TypeHpMul(bc, EMonster.Reaper), 0.0001f, "Reaper HP 배율 불변 (미등록 → 1.0)");
            Assert.AreEqual(1f, TypeHpMul(bc, EMonster.Hex), 0.0001f, "Hex HP 배율 불변");
            Assert.AreEqual(1f, TypeHpMul(bc, EMonster.Phantom), 0.0001f, "Phantom HP 배율 불변");
            Assert.AreEqual(1f, TypeHpMul(bc, EMonster.Plague), 0.0001f, "Plague HP 배율 불변");
        }

        //# 비활성 BattleController — Start(async void) 미실행. _ctx 없이 RegisterMonsterTypeBuff 직접 호출 가능.
        private BattleController CreateIsolatedController()
        {
            GameObject go = new GameObject("BattleControllerUT_Tier3");
            go.SetActive(false);
            _isolated.Add(go);
            return go.AddComponent<BattleController>();
        }

        //# 격리 컨트롤러용 ctx — BattleContext 가 _owner 로 RegisterMonsterTypeBuff 를 위임.
        //# synergy=null — RegisterMonsterTypeBuff 경로는 synergy 미사용 (RegisterCardPick 만 null 가드).
        private static IBattleContext BuildCtx(BattleController bc)
        {
            return new BattleContext(bc, null);
        }

        private readonly System.Collections.Generic.List<GameObject> _isolated = new();

        //# BattleController._typeModifiers 의 종별 HP 배율 조회 (미등록 종이면 1f). 리플렉션 헬퍼.
        private static float TypeHpMul(BattleController bc, EMonster type)
        {
            System.Collections.IDictionary dict =
                GetPrivate<System.Collections.IDictionary>(bc, "_typeModifiers");
            if (dict == null || dict.Contains(type) == false) return 1f;
            object mul = dict[type];
            FieldInfo fi = mul.GetType().GetField("HpMul", BindingFlags.Public | BindingFlags.Instance);
            return fi != null ? (float)fi.GetValue(mul) : 1f;
        }
    }
}
