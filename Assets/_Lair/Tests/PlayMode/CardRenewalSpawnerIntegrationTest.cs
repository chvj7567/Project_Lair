using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 카드 리뉴얼 v0.6 본격 스위트 (PlayMode) — Spawner 주기·출력 변경 시너지 통합 검증.
    //# ContinuousSpawnIntegrationTest 는 ApplyMonsterStats / RegisterMonsterTypeBuff / Plague Spawner 분포 / Debuff 5장 카운트만 다룸.
    //# 본 스위트는 SpawnerHaste · Swarm Tier2 · Swarm Tier3 · Tank Tier3 의 *Spawner 직접 변경* 표면을 라이브 BattleController 위에서 검증.
    public class CardRenewalSpawnerIntegrationTest : BattlePlayTestBase
    {
        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
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

        //# ===== 4. Tank Tier3 — 글로벌 캡 18 → 24 =====

        [UnityTest]
        public IEnumerator Tank_Tier3_IncrementGlobalMonsterCap_6_캡_24로_상승()
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

            int before = bc.MonsterCap;

            new TankSynergyTier3().Apply(ctx);

            Assert.AreEqual(before + 6, bc.MonsterCap,
                $"Tank Tier3 — 캡 {before} → {before + 6} (영구 +6)");
        }

        //# ===== 5. Tank Tier3 + Swarm Tier3 동시 적용 (디자인상 불가지만 안전망 회귀) =====

        //# 기획서 §9.5: "원리상 불가" 단정. 안전 가드로 두 Tier3 가 동시 발동되어도 예외 없이 누적.
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

            int capBefore = bc.MonsterCap;
            Spawner[] spawners = GetPrivate<Spawner[]>(bc, "_spawners");
            int[] outputBefore = new int[spawners.Length];
            for (int i = 0; i < spawners.Length; ++i)
                outputBefore[i] = spawners[i].OutputCount;

            Assert.DoesNotThrow(() =>
            {
                new TankSynergyTier3().Apply(ctx);
                new SwarmSynergyTier3().Apply(ctx);
            }, "두 Tier3 동시 적용해도 예외 없음 (§9.5 안전 가드)");

            Assert.AreEqual(capBefore + 6, bc.MonsterCap, "캡 +6");
            for (int i = 0; i < spawners.Length; ++i)
                Assert.AreEqual(outputBefore[i] + 1, spawners[i].OutputCount, "Output +1");
        }
    }
}
