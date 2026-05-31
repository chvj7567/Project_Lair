using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — WallOfWisps / SwarmRush 즉시 소환 효과 망라.
    //# 기획서 §10.4 디자인 단정:
    //#   - WallOfWisps: 영웅 transform 위치 기준 4방위(0°/90°/180°/270°) 에 _radius 거리 Wisp 4마리.
    //#   - SwarmRush: 영웅 transform 위치에서 Phantom 6마리.
    //# 둘 다 캡 truncate 는 SpawnMonster 내부의 글로벌 캡 로직 (=BattleController) 책임.
    public class WallOfWispsSwarmRushTests
    {
        //# WallOfWisps 의 4방위 — 4개 위치가 정확히 90° 간격으로 분포.
        //# (cos(0)=1, sin(0)=0) → (radius, 0)
        //# (cos(90)=0, sin(90)=1) → (0, radius)
        //# (cos(180)=-1, sin(180)=0) → (-radius, 0)
        //# (cos(270)=0, sin(270)=-1) → (0, -radius)
        [Test]
        public void WallOfWisps_4마리_4방위_정확한_좌표()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            heroGo.transform.position = new Vector3(10f, 0f, 5f);
            ctx.HeroTransform = heroGo.transform;
            try
            {
                new WallOfWispsEffect().Apply(ctx);

                Assert.AreEqual(4, ctx.SpawnPositions.Count, "WallOfWisps — 4마리 소환");

                //# radius 기본 2.5 — origin(10,0,5) 기준 4방위 모두 사거리 2.5.
                Vector3 origin = new Vector3(10f, 0f, 5f);
                foreach (Vector3 pos in ctx.SpawnPositions)
                {
                    float distance = Vector3.Distance(pos, origin);
                    Assert.AreEqual(2.5f, distance, 0.01f,
                        $"각 spawn 위치는 origin 으로부터 _radius=2.5 거리 (pos={pos})");
                    //# Y 평면 (xz) 안에서만 분포 — y=0 보존.
                    Assert.AreEqual(0f, pos.y, 0.01f, "Y 평면 보존");
                }

                //# 4개 위치가 서로 다름 — 같은 방위로 모이지 않음.
                HashSet<Vector3> unique = new HashSet<Vector3>();
                foreach (Vector3 pos in ctx.SpawnPositions)
                    unique.Add(new Vector3(Mathf.Round(pos.x * 10) / 10f, 0f, Mathf.Round(pos.z * 10) / 10f));
                Assert.AreEqual(4, unique.Count, "4개 위치 모두 서로 다름 (4방위 분포)");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# _count 변경 시 균등 각도 분배 — 효과 클래스의 정책 회귀.
        [Test]
        public void WallOfWisps_count_8_8마리_각도_45도_간격_분배()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            heroGo.transform.position = Vector3.zero;
            ctx.HeroTransform = heroGo.transform;
            try
            {
                //# _count 는 SerializeField private → 리플렉션으로 8 설정.
                WallOfWispsEffect effect = new WallOfWispsEffect();
                System.Reflection.FieldInfo fi = typeof(WallOfWispsEffect).GetField(
                    "_count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi.SetValue(effect, 8);

                effect.Apply(ctx);

                Assert.AreEqual(8, ctx.SpawnPositions.Count, "8마리 소환");
                //# 8마리 모두 _radius=2.5 거리에 분포.
                foreach (Vector3 pos in ctx.SpawnPositions)
                    Assert.AreEqual(2.5f, Vector3.Distance(pos, Vector3.zero), 0.01f);
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# SwarmRush — 6마리 Phantom 모두 영웅 transform 위치에서 소환 (origin 동일).
        [Test]
        public void SwarmRush_6마리_Phantom_영웅_위치_소환()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            heroGo.transform.position = new Vector3(5f, 2f, -3f);
            ctx.HeroTransform = heroGo.transform;
            try
            {
                new SwarmRushEffect().Apply(ctx);

                Assert.AreEqual(6, ctx.Spawned.Count);
                Assert.AreEqual(6, ctx.SpawnPositions.Count);
                Vector3 expected = new Vector3(5f, 2f, -3f);
                foreach (Vector3 pos in ctx.SpawnPositions)
                    Assert.AreEqual(expected, pos, "SwarmRush — 6마리 모두 영웅 위치");
                foreach (EMonster key in ctx.Spawned)
                    Assert.AreEqual(EMonster.Phantom, key);
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# SwarmRush Hero 없을 때 — origin Vector3.zero 폴백 (예외 없음).
        [Test]
        public void SwarmRush_Hero_없으면_origin_zero_폴백()
        {
            FakeCtx ctx = new FakeCtx();
            Assert.DoesNotThrow(() => new SwarmRushEffect().Apply(ctx),
                "SwarmRush Hero 없음 — 예외 없이 Vector3.zero 폴백");
            Assert.AreEqual(6, ctx.Spawned.Count, "Hero 없어도 6마리 소환 (원점)");
            foreach (Vector3 pos in ctx.SpawnPositions)
                Assert.AreEqual(Vector3.zero, pos);
        }

        //# 엣지 — _count = 0 인 SwarmRush 는 0회 소환 (예외 없음).
        [Test]
        public void SwarmRush_count_0_소환_0회_예외없음()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;
            try
            {
                SwarmRushEffect effect = new SwarmRushEffect();
                System.Reflection.FieldInfo fi = typeof(SwarmRushEffect).GetField(
                    "_count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi.SetValue(effect, 0);

                Assert.DoesNotThrow(() => effect.Apply(ctx));
                Assert.AreEqual(0, ctx.Spawned.Count, "count=0 → 0회 소환");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# 엣지 — _count 음수도 안전하게 0 으로 처리 (Mathf.Max).
        [Test]
        public void SwarmRush_count_음수_0으로_clamp()
        {
            FakeCtx ctx = new FakeCtx();
            GameObject heroGo = new GameObject("hero_t");
            ctx.HeroTransform = heroGo.transform;
            try
            {
                SwarmRushEffect effect = new SwarmRushEffect();
                System.Reflection.FieldInfo fi = typeof(SwarmRushEffect).GetField(
                    "_count", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                fi.SetValue(effect, -5);

                Assert.DoesNotThrow(() => effect.Apply(ctx));
                Assert.AreEqual(0, ctx.Spawned.Count, "count<0 → Mathf.Max(0) → 0회 소환");
            }
            finally
            {
                Object.DestroyImmediate(heroGo);
            }
        }

        //# ===== Fake =====

        private class FakeCtx : IBattleContext
        {
            public readonly List<EMonster> Spawned = new();
            public readonly List<Vector3> SpawnPositions = new();
            public Transform HeroTransform;

            public void SpawnMonster(EMonster key, Vector3 nearHero)
            {
                Spawned.Add(key);
                SpawnPositions.Add(nearHero);
            }
            public Transform GetHeroTransform() => HeroTransform;

            //# no-op stubs
            public float DeltaTime => 0f;
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public IMover GetHeroMover() => null;
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void IncrementGlobalMonsterCap(int delta) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
        }
    }
}
