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
    //# IncrementSpawnerOutput / ReplaceSpawnerOutput 의 BattleController 구현 의미
    //# (Spawner 배열 순회 + CurrentType 매칭) 를 재현해 카드 간 순서 의존을 검증한다.
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
        //# (CurrentType 매칭 후 Spawner 런타임 상태 변경) — §3.5 상호작용을 정확히 재현.
        private class SpawnerAwareContext : IBattleContext
        {
            private readonly List<Spawner> _spawners;
            public SpawnerAwareContext(List<Spawner> spawners) => _spawners = spawners;

            public void IncrementSpawnerOutput(EMonster type)
            {
                foreach (var sp in _spawners)
                    if (sp != null && sp.CurrentType == type) sp.IncrementOutput();
            }

            public void ReplaceSpawnerOutput(EMonster from, EMonster to)
            {
                foreach (var sp in _spawners)
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
            public float DeltaTime => 0f;
        }

        private readonly List<GameObject> _spawned = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();
        }

        private Spawner CreateSpawner(EMonster outputType, ISpawnerHost host)
        {
            var go = new GameObject("SpawnerUT");
            _spawned.Add(go);
            var sp = go.AddComponent<Spawner>();
            //# 주기 9 / InitialDelay 0 — Tick(0) 으로 즉시 1발 발사 가능.
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", 9f);
            SetPrivate(sp, "_initialDelay", 0f);
            //# OnEnable 명시 호출 — EditMode 에서 SetActive 토글이 OnEnable 을 신뢰성 있게
            //# 트리거하지 못함. 직렬 _outputType 을 런타임 _currentType 에 반영하기 위해 필수.
            InvokeOnEnable(sp);
            sp.Bind(host);
            return sp;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"Spawner.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# Spawner.OnEnable 을 리플렉션으로 직접 호출 — EditMode 테스트 라이프사이클 보정.
        private static void InvokeOnEnable(Component c)
        {
            var mi = c.GetType().GetMethod("OnEnable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "Spawner.OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        //# ===== §3.5 케이스 1 — 융합 후 추가소환 no-op =====

        //# 융합(슬라임→골렘) 픽 후 SpawnSlimes 픽 → 슬라임 Spawner 0개라 no-op (죽은 픽).
        [Test]
        public void 융합후_추가소환_매칭_Spawner_없으면_noop()
        {
            var host = new FakeSpawnerHost();
            //# 슬라임 Spawner 1개만.
            var slime = CreateSpawner(EMonster.Slime, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime });

            //# 융합 카드 — 슬라임 Spawner → 골렘 출력.
            new ReplaceSlimesToGolemEffect().Apply(ctx);
            Assert.AreEqual(EMonster.Golem, slime.CurrentType, "융합 후 출력 종 골렘");

            //# SpawnSlimes — "슬라임 출력 Spawner" 를 찾는데 이미 0개 → 동시 출력 변화 없음.
            new SpawnSlimesEffect().Apply(ctx);

            slime.Tick(0f);
            Assert.AreEqual(1, host.Spawns[0].count,
                "융합 후 SpawnSlimes 는 죽은 픽 — 동시 출력 1 유지");
            Assert.AreEqual(EMonster.Golem, host.Spawns[0].type);
        }

        //# ===== §3.5 케이스 3 — 추가소환 후 융합: 출력 수 보너스 유지 =====

        //# SpawnSlimes(출력+1) 픽 후 융합(슬라임→골렘) 픽 → 그 Spawner 는 골렘을 2마리씩 뱉음.
        //# 동시 출력 수는 Spawner 슬롯에 종속, 출력 종만 바뀐다.
        [Test]
        public void 추가소환후_융합_동시출력_보너스_Spawner에_유지()
        {
            var host = new FakeSpawnerHost();
            var slime = CreateSpawner(EMonster.Slime, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime });

            //# SpawnSlimes — 슬라임 Spawner 동시 출력 1→2.
            new SpawnSlimesEffect().Apply(ctx);
            //# 융합 — 출력 종만 골렘으로. 동시 출력 2 는 유지.
            new ReplaceSlimesToGolemEffect().Apply(ctx);

            slime.Tick(0f);
            Assert.AreEqual(EMonster.Golem, host.Spawns[0].type, "출력 종은 골렘으로 변경");
            Assert.AreEqual(2, host.Spawns[0].count,
                "동시 출력 +1 보너스는 Spawner 슬롯에 귀속 — 종 변경 후에도 유지");
        }

        //# ===== §3.5 케이스 4 — 융합 두 번 픽: 두 번째는 no-op =====

        [Test]
        public void 융합_두번_픽_두번째는_매칭없어_noop()
        {
            var host = new FakeSpawnerHost();
            var slime = CreateSpawner(EMonster.Slime, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime });

            new ReplaceSlimesToGolemEffect().Apply(ctx);
            Assert.AreEqual(EMonster.Golem, slime.CurrentType);

            //# 두 번째 융합 — 슬라임 출력 Spawner 0개 → 변화 없음 (골렘이 슬라임으로 되돌지 않음).
            new ReplaceSlimesToGolemEffect().Apply(ctx);
            Assert.AreEqual(EMonster.Golem, slime.CurrentType, "두 번째 융합은 no-op — 골렘 유지");
        }

        //# ===== 추가소환 — 동일 종 Spawner 여러 개 동시 +1 (스타터 슬라임 2개 §5.3) =====

        //# SpawnSlimes 한 번 픽이 슬라임 Spawner 2개를 각각 +1.
        [Test]
        public void 추가소환_동일종_Spawner_2개_모두_출력증가()
        {
            var host = new FakeSpawnerHost();
            var slime1 = CreateSpawner(EMonster.Slime, host);
            var slime2 = CreateSpawner(EMonster.Slime, host);
            var bat = CreateSpawner(EMonster.Bat, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime1, slime2, bat });

            new SpawnSlimesEffect().Apply(ctx);

            slime1.Tick(0f);
            slime2.Tick(0f);
            bat.Tick(0f);

            Assert.AreEqual(2, host.Spawns[0].count, "슬라임 Spawner 1 — 출력 2");
            Assert.AreEqual(2, host.Spawns[1].count, "슬라임 Spawner 2 — 출력 2");
            Assert.AreEqual(1, host.Spawns[2].count, "박쥐 Spawner — 무관, 출력 1 유지");
        }

        //# ===== 융합 — 입력 종 일치 Spawner 만 변경 (§3.4.1 완화안) =====

        //# 슬라임 2개 / 오크 1개 — ReplaceSlimesToGolem 은 슬라임 2개만 골렘으로, 오크는 불변.
        [Test]
        public void 융합_입력종_일치_Spawner만_변경_나머지_불변()
        {
            var host = new FakeSpawnerHost();
            var slime1 = CreateSpawner(EMonster.Slime, host);
            var slime2 = CreateSpawner(EMonster.Slime, host);
            var orc = CreateSpawner(EMonster.Orc, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime1, slime2, orc });

            new ReplaceSlimesToGolemEffect().Apply(ctx);

            Assert.AreEqual(EMonster.Golem, slime1.CurrentType, "슬라임 Spawner 1 → 골렘");
            Assert.AreEqual(EMonster.Golem, slime2.CurrentType, "슬라임 Spawner 2 → 골렘");
            Assert.AreEqual(EMonster.Orc, orc.CurrentType, "오크 Spawner — 입력 종 불일치, 불변");
        }

        //# ===== 추가소환 후 추가소환 — 선형 누적 =====

        //# SpawnSlimes 두 번 픽 → 동시 출력 1→2→3 (선형, §3.2 C안).
        [Test]
        public void 추가소환_두번_픽_동시출력_선형_누적()
        {
            var host = new FakeSpawnerHost();
            var slime = CreateSpawner(EMonster.Slime, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime });

            new SpawnSlimesEffect().Apply(ctx);
            new SpawnSlimesEffect().Apply(ctx);

            slime.Tick(0f);
            Assert.AreEqual(3, host.Spawns[0].count, "기본 1 + 2픽 = 3 (선형 누적)");
        }

        //# ===== 융합 후 다음 융합이 새 종 체인 — ReplaceOrcsToArchers 후 슬라임 융합 독립 =====

        //# 융합 카드 2장은 서로 독립. ReplaceOrcsToArchers 는 오크만, ReplaceSlimesToGolem 은 슬라임만.
        [Test]
        public void 융합_두_카드_각자_입력종만_변경_상호_독립()
        {
            var host = new FakeSpawnerHost();
            var slime = CreateSpawner(EMonster.Slime, host);
            var orc = CreateSpawner(EMonster.Orc, host);
            var ctx = new SpawnerAwareContext(new List<Spawner> { slime, orc });

            new ReplaceOrcsToArchersEffect().Apply(ctx);
            Assert.AreEqual(EMonster.Slime, slime.CurrentType, "슬라임 — 오크 융합에 영향 없음");
            Assert.AreEqual(EMonster.Archer, orc.CurrentType, "오크 → 궁수");

            new ReplaceSlimesToGolemEffect().Apply(ctx);
            Assert.AreEqual(EMonster.Golem, slime.CurrentType, "슬라임 → 골렘");
            Assert.AreEqual(EMonster.Archer, orc.CurrentType, "궁수 — 슬라임 융합에 영향 없음");
        }

        //# ===== 빈 Spawner 집합 — 모든 카드 no-op, 예외 없음 =====

        [Test]
        public void Spawner_0개일때_추가소환_융합_예외없이_noop()
        {
            var ctx = new SpawnerAwareContext(new List<Spawner>());

            Assert.DoesNotThrow(() =>
            {
                new SpawnSlimesEffect().Apply(ctx);
                new ReplaceSlimesToGolemEffect().Apply(ctx);
            }, "Spawner 0개 — 카드 적용이 예외 없이 no-op");
        }
    }
}
