using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Battle;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# 지속 스폰 — 카드 효과 × Spawner 상태 상호작용 통합 검증 (기획서 §3.5).
    //# 라우팅만 보는 ContinuousSpawnEffectTests 와 달리, 실제 Spawner 컴포넌트를 들고
    public class ContinuousSpawnInteractionTests
    {
        //# ISpawnerHost 더블 — 카드 적용 후 Spawner.Tick 결과를 받아 실제 스폰 종/수를 본다.
        private class FakeSpawnerHost : ISpawnerHost
        {
            public readonly List<(EMonster type, int count)> Spawns = new();
            public void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count)
                => Spawns.Add((type, count));
        }

        //# Spawner 집합을 보유하는 IBattleContext 더블.
        //# IncrementSpawnerOutput / ReplaceSpawnerOutput 을 BattleController 와 동일한 의미로 구현
        private class SpawnerAwareContext : IBattleContext
        {
            private readonly List<Spawner> _spawners;
            public SpawnerAwareContext(List<Spawner> spawners) => _spawners = spawners;

            public void IncrementSpawnerOutput(EMonster type)
            {
                foreach (Spawner sp in _spawners)
                    if (sp != null && sp.CurrentType == type) sp.IncrementOutput();
            }

            public void ReplaceSpawnerOutput(EMonster from, EMonster to)
            {
                foreach (Spawner sp in _spawners)
                    if (sp != null && sp.CurrentType == from) sp.ReplaceOutput(to);
            }

            //# 본 테스트 미사용 — 인터페이스 충족용 no-op.
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => new List<IHealth>();
            public IHealth GetHero() => null;
            public Transform GetHeroTransform() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            //# 카드 리뉴얼 v0.6 — IBattleContext 신규 표면 (Phase 1 Task 4). 본 테스트 미사용 stub.
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
            public void ScaleSpawnerPeriodForType(EMonster type, float mul) { }
            public float DeltaTime => 0f;
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Spawner CreateSpawner(EMonster outputType, ISpawnerHost host)
        {
            GameObject go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            //# 주기 9 / InitialDelay 0 — Tick(0) 으로 즉시 1발 발사 가능.
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            //# OnEnable 명시 호출 — EditMode 에서 SetActive 토글이 OnEnable 을 신뢰성 있게
            //# 트리거하지 못함. 직렬 _outputType 을 런타임 _currentType 에 반영하기 위해 필수.
            InvokeOnEnable(sp);
            sp.Bind(host, null);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# Spawner.OnEnable 을 리플렉션으로 직접 호출 — EditMode 테스트 라이프사이클 보정.
        private static void InvokeOnEnable(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Spawner.OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        //# 카드 리뉴얼 v0.6 patch — 융합 카드(ReplaceWispsToWraith/ReplaceReapersToHex) 폐기.
        //# 효과가 WispWraithPowerBoost/ReaperHexPowerBoost 로 교체되어 §3.5 케이스 1·3·4 동작 검증 의미 사라짐.

        //# ===== 추가소환 — 동일 종 Spawner 여러 개 동시 +1 (스타터 위스프 2개 §5.3) =====

        //# SpawnWisps 한 번 픽이 위스프 Spawner 2개를 각각 +1.
        [Test]
        public void 추가소환_동일종_Spawner_2개_모두_출력증가()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner wisp1 = CreateSpawner(EMonster.Wisp, host);
            Spawner wisp2 = CreateSpawner(EMonster.Wisp, host);
            Spawner phantom = CreateSpawner(EMonster.Phantom, host);
            SpawnerAwareContext ctx = new SpawnerAwareContext(new List<Spawner> { wisp1, wisp2, phantom });

            new SpawnWispsEffect().Apply(ctx);

            wisp1.Tick(0f);
            wisp2.Tick(0f);
            phantom.Tick(0f);

            Assert.AreEqual(2, host.Spawns[0].count, "위스프 Spawner 1 — 출력 2");
            Assert.AreEqual(2, host.Spawns[1].count, "위스프 Spawner 2 — 출력 2");
            Assert.AreEqual(1, host.Spawns[2].count, "팬텀 Spawner — 무관, 출력 1 유지");
        }

        //# (v0.6 patch — 융합_입력종_일치_Spawner만_변경 테스트 폐기. 효과 교체로 ReplaceSpawnerOutput 동작 검증 의미 사라짐.)

        //# ===== 추가소환 후 추가소환 — 선형 누적 =====

        //# SpawnWisps 두 번 픽 → 동시 출력 1→2→3 (선형, §3.2 C안).
        [Test]
        public void 추가소환_두번_픽_동시출력_선형_누적()
        {
            FakeSpawnerHost host = new FakeSpawnerHost();
            Spawner wisp = CreateSpawner(EMonster.Wisp, host);
            SpawnerAwareContext ctx = new SpawnerAwareContext(new List<Spawner> { wisp });

            new SpawnWispsEffect().Apply(ctx);
            new SpawnWispsEffect().Apply(ctx);

            wisp.Tick(0f);
            Assert.AreEqual(3, host.Spawns[0].count, "기본 1 + 2픽 = 3 (선형 누적)");
        }

        //# (v0.6 patch — 융합_두_카드_각자_입력종만_변경 테스트 폐기. 효과 교체로 의미 사라짐.)

        //# ===== 빈 Spawner 집합 — 모든 카드 no-op, 예외 없음 =====

        [Test]
        public void Spawner_0개일때_추가소환_강화_예외없이_noop()
        {
            SpawnerAwareContext ctx = new SpawnerAwareContext(new List<Spawner>());

            Assert.DoesNotThrow(() =>
            {
                new SpawnWispsEffect().Apply(ctx);
                new WispWraithPowerBoostEffect().Apply(ctx);
            }, "Spawner 0개 — 카드 적용이 예외 없이 no-op");
        }
    }
}
