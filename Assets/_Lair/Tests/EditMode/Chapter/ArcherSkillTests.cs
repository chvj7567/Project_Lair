using System.Collections.Generic;
using Lair.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 궁수 스킬 3종 런타임·SO 데이터 자체 스모크 (chapter-system §5.4). 본격 스위트는 test-engineer.
    public class ArcherSkillTests
    {
        private const string SkillDir = "Assets/_Lair/Art/Skills/";

        //# 호출 기록용 궁수 컨텍스트 더블.
        private class FakeArcherCtx : IArcherSkillContext
        {
            public Vector3 HeroPosition { get; set; } = Vector3.zero;
            public Vector3 Centroid = new Vector3(4f, 0f, 0f);
            public bool HasDensePoint = true;
            public Vector3 DensePoint = new Vector3(3f, 0f, 1f);
            public int NearbyCount;
            public int LineHits;
            public Vector3 LastLineDir;
            public int VolleyTicks;
            public int RingHits;
            public bool Leaped;
            public Vector3 LeapDestination;
            public float LeapDuration;

            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { RingHits++; return 0; }
            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { VolleyTicks++; return 0; }
            public Vector3 MonsterCentroid(float radius) { return Centroid; }
            public int DamageMonstersInLine(Vector3 direction, float length, float halfWidth, int amount, float knockbackStrength) { LineHits++; LastLineDir = direction; return 0; }
            public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point) { point = DensePoint; return HasDensePoint; }
            public int CountMonstersInRadius(float radius) { return NearbyCount; }
            public Vector3 ClampToArena(Vector3 position) { return KiteBehavior.ClampToRadius(position, 7f); }
            public void BeginLeap(Vector3 destination, float duration) { Leaped = true; LeapDestination = destination; LeapDuration = duration; }
        }

        [Test]
        public void 관통_사격은_첫_쿨다운_뒤_무게중심_방향_직선으로_발동하고_몬스터가_없으면_보류한다()
        {
            PiercingShotSkillData data = ScriptableObject.CreateInstance<PiercingShotSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeArcherCtx ctx = new FakeArcherCtx();

            runtime.Tick(ctx, 1f);
            Assert.AreEqual(0, ctx.LineHits, "활성 직후 즉발 방지(쿨다운 4초)");

            ctx.Centroid = Vector3.zero;   //# 몬스터 없음 → 방향 없음
            runtime.Tick(ctx, 4f);
            Assert.AreEqual(0, ctx.LineHits, "몬스터 없으면 보류");

            ctx.Centroid = new Vector3(4f, 0f, 0f);
            runtime.Tick(ctx, 0.1f);
            Assert.AreEqual(1, ctx.LineHits);
            Assert.AreEqual(1f, ctx.LastLineDir.x, 0.001f);

            runtime.Tick(ctx, 1f);
            Assert.AreEqual(1, ctx.LineHits, "발동 후 쿨다운 4초");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 화살_소나기는_발동_즉시_첫_타격하고_이후_간격마다_총_3타를_친다()
        {
            ArrowVolleySkillData data = ScriptableObject.CreateInstance<ArrowVolleySkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeArcherCtx ctx = new FakeArcherCtx();

            runtime.Tick(ctx, 8f);   //# 첫 쿨다운 8초 경과 → 발동 + 1타
            Assert.AreEqual(1, ctx.VolleyTicks);

            runtime.Tick(ctx, 0.5f);
            Assert.AreEqual(2, ctx.VolleyTicks);
            runtime.Tick(ctx, 0.5f);
            Assert.AreEqual(3, ctx.VolleyTicks);
            runtime.Tick(ctx, 0.5f);
            Assert.AreEqual(3, ctx.VolleyTicks, "3타 뒤엔 쿨다운(8초) 동안 추가 타격 없음");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 회피_도약은_몬스터가_3마리_이상일_때만_위협_반대로_도약하고_목적지는_아레나_안으로_클램프된다()
        {
            EvadeSkillData data = ScriptableObject.CreateInstance<EvadeSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeArcherCtx ctx = new FakeArcherCtx { HeroPosition = new Vector3(5f, 0f, 0f), Centroid = new Vector3(4f, 0f, 0f), NearbyCount = 2 };

            runtime.Tick(ctx, 10f);
            Assert.IsFalse(ctx.Leaped, "2마리는 트리거 미달");

            ctx.NearbyCount = 3;
            runtime.Tick(ctx, 0.25f);
            Assert.IsTrue(ctx.Leaped);
            Assert.AreEqual(1, ctx.RingHits, "출발 지점 폭발 1회");
            Assert.AreEqual(0.25f, ctx.LeapDuration, 0.0001f);
            //# 위협은 -X 쪽(4,0,0) → 도약은 +X, 5+4.5=9.5 → 소프트 반경 7 로 클램프(E-9).
            Assert.AreEqual(7f, ctx.LeapDestination.x, 0.001f);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 궁수_스킬은_궁수_컨텍스트가_아니면_발동하지_않는다()
        {
            PiercingShotSkillData data = ScriptableObject.CreateInstance<PiercingShotSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();

            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("IArcherSkillContext"));
            Assert.DoesNotThrow(() => runtime.Tick(new PlainCtx(), 10f));
            Object.DestroyImmediate(data);
        }

        private class PlainCtx : IHeroSkillContext
        {
            public Vector3 HeroPosition => Vector3.zero;
            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
            public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }
        }

        [Test]
        public void 실제_궁수_로드아웃_asset은_85_65_45_순서로_관통_소나기_도약을_가리킨다()
        {
            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(SkillDir + "HeroSkillLoadout_Archer.asset");
            Assert.IsNotNull(loadout, "HeroSkillLoadout_Archer.asset 로드 실패");
            Assert.AreEqual(3, loadout.Phases.Count);
            Assert.AreEqual(0.85f, loadout.Phases[0].HpFraction, 0.0001f);
            Assert.IsInstanceOf<PiercingShotSkillData>(loadout.Phases[0].Skill);
            Assert.AreEqual(0.65f, loadout.Phases[1].HpFraction, 0.0001f);
            Assert.IsInstanceOf<ArrowVolleySkillData>(loadout.Phases[1].Skill);
            Assert.AreEqual(0.45f, loadout.Phases[2].HpFraction, 0.0001f);
            Assert.IsInstanceOf<EvadeSkillData>(loadout.Phases[2].Skill);
        }

        [Test]
        public void 실제_스킬_asset의_수치는_기획서_5_4_표와_같다()
        {
            PiercingShotSkillData pierce = AssetDatabase.LoadAssetAtPath<PiercingShotSkillData>(SkillDir + "HeroSkill_PiercingShot.asset");
            ArrowVolleySkillData volley = AssetDatabase.LoadAssetAtPath<ArrowVolleySkillData>(SkillDir + "HeroSkill_ArrowVolley.asset");
            EvadeSkillData evade = AssetDatabase.LoadAssetAtPath<EvadeSkillData>(SkillDir + "HeroSkill_Evade.asset");

            Assert.AreEqual(70, pierce.Damage);
            Assert.AreEqual(9f, pierce.LineLength, 0.0001f);
            Assert.AreEqual(0.6f, pierce.LineHalfWidth, 0.0001f);
            Assert.AreEqual("관통 사격", pierce.DisplayName);
            Assert.AreEqual(40, volley.DamagePerTick);
            Assert.AreEqual(3, volley.TickCount);
            Assert.AreEqual(2.8f, volley.Radius, 0.0001f);
            Assert.AreEqual("화살 소나기", volley.DisplayName);
            Assert.AreEqual(4.5f, evade.LeapDistance, 0.0001f);
            Assert.AreEqual(3, evade.TriggerMonsterCount);
            Assert.AreEqual("회피 도약", evade.DisplayName);
        }
    }
}
