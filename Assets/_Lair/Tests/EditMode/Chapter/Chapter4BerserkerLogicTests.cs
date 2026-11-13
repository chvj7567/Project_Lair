using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 광전사(챕터 4) 순수 로직 검증 - 군집 점수·목표 선정, 돌진 상태기계, 범위 베기 타격, 스킬 3종 (chapter-4 §3.3·§4·§9.1).
    //# 월드 의존(공포 FleeMode 우선, 봉인 종족 제외, 전장 이탈)은 AutoCombatAI/레지스트리 영역이라 PlayMode(G8)에서 확인한다.
    public class Chapter4BerserkerLogicTests
    {
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void FX_로그를_격리한다()
        {
            ArcherFxLogGuard.Suppress();
        }

        [TearDown]
        public void 생성물을_정리한다()
        {
            foreach (GameObject go in _spawned)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _spawned.Clear();
            ArcherFxLogGuard.Restore();
        }

        private T NewAsset<T>() where T : HeroSkillData
        {
            return HeroSkillTestKit.Make<T>();
        }

        //# ===== 설정 변환 =====

        //# 기획서 §2.1 Berserker 값.
        private static ChargeSettings Shipped()
        {
            return new ChargeSettings(10f, 2.2f, 2, 1f, 0.4f, 7f);
        }

        [Test]
        public void ChargeSettings_From은_row의_6개_필드를_그대로_옮긴다()
        {
            HeroArchetype_ChargeRow row = new HeroArchetype_ChargeRow
            {
                heroId = "Berserker", huntRadius = 1f, clusterRadius = 2f, retargetCountMargin = 3, retargetIntervalSec = 4f, minDwellSec = 5f, arenaSoftRadius = 6f,
            };

            ChargeSettings s = ChargeSettings.From(row);

            Assert.AreEqual(1f, s.HuntRadius);
            Assert.AreEqual(2f, s.ClusterRadius);
            Assert.AreEqual(3, s.RetargetCountMargin);
            Assert.AreEqual(4f, s.RetargetIntervalSec);
            Assert.AreEqual(5f, s.MinDwellSec);
            Assert.AreEqual(6f, s.ArenaSoftRadius);
        }

        [Test]
        public void 실제_JSON의_돌진_베기_설정이_설정_구조체로_변환된다()
        {
            ChapterDatabase db = WorldMapTestSupport.FullDb();

            ChargeSettings charge = ChargeSettings.From(db.GetCharge("Berserker"));
            CleaveSettings cleave = CleaveSettings.From(db.GetCleave("Berserker"));

            Assert.AreEqual(10f, charge.HuntRadius, 0.0001f);
            Assert.AreEqual(2.2f, charge.ClusterRadius, 0.0001f);
            Assert.AreEqual(2, charge.RetargetCountMargin);
            Assert.AreEqual(0.4f, charge.MinDwellSec, 0.0001f);
            Assert.AreEqual(7f, charge.ArenaSoftRadius, 0.0001f);
            Assert.AreEqual(2.2f, cleave.Radius, 0.0001f);
            Assert.AreEqual(4, cleave.MaxTargets);
            Assert.AreEqual(0.5f, cleave.SecondaryMul, 0.0001f);
        }

        //# ===== 군집 점수 / 목표 선정 =====

        private ClusterCandidate Cand(float x, float z)
        {
            GameObject go = new GameObject("candidate");
            _spawned.Add(go);
            go.transform.position = new Vector3(x, 0f, z);
            return new ClusterCandidate { Transform = go.transform, Health = new FakeHealth(), Position = go.transform.position };
        }

        [Test]
        public void clusterScore는_반경_안_후보_수이며_자신을_포함하고_반경_밖은_세지_않는다()
        {
            List<ClusterCandidate> list = new List<ClusterCandidate> { Cand(0f, 5f), Cand(1f, 5f), Cand(1.5f, 5f), Cand(-5f, 0f) };

            Assert.AreEqual(3, ChargeBehavior.ClusterScore(list, 0, 2.2f));
            Assert.AreEqual(3, ChargeBehavior.ClusterScore(list, 1, 2.2f));
            Assert.AreEqual(1, ChargeBehavior.ClusterScore(list, 3, 2.2f), "혼자면 자신만");
        }

        [Test]
        public void 군집_수가_많은_쪽이_가까운_외톨이보다_우선한다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            List<ClusterCandidate> list = new List<ClusterCandidate> { Cand(1f, 0f), Cand(0f, 8f), Cand(1f, 8f), Cand(0.5f, 9f) };

            int picked = charge.SelectTarget(list, Vector3.zero, null);

            Assert.AreEqual(1, picked, "군집 3마리 중 영웅에 가장 가까운 (0,8)");
        }

        [Test]
        public void 군집_수가_같으면_영웅에_가까운_군집을_고른다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            List<ClusterCandidate> list = new List<ClusterCandidate> { Cand(-6f, 0f), Cand(-6f, 1f), Cand(3f, 0f), Cand(3f, 1f) };

            int picked = charge.SelectTarget(list, Vector3.zero, null);

            Assert.AreEqual(2, picked, "(3,0) 군집이 더 가깝고 그 안에서도 (3,0)이 가장 가깝다");
        }

        [Test]
        public void 후보가_없으면_목표는_없다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());

            Assert.AreEqual(-1, charge.SelectTarget(new List<ClusterCandidate>(), Vector3.zero, null));
            Assert.AreEqual(-1, charge.SelectTarget(null, Vector3.zero, null));
        }

        [Test]
        public void 현재_목표는_군집_수가_margin_2_이상_앞설_때만_교체된다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            ClusterCandidate lone = Cand(0f, 1f);
            ClusterCandidate pairA = Cand(0f, 8f);
            ClusterCandidate pairB = Cand(1f, 8f);
            List<ClusterCandidate> pair = new List<ClusterCandidate> { lone, pairA, pairB };

            Assert.AreEqual(0, charge.SelectTarget(pair, Vector3.zero, lone.Transform), "2 < 1 + 2 라 유지");

            ClusterCandidate tripleC = Cand(0.5f, 9f);
            List<ClusterCandidate> triple = new List<ClusterCandidate> { lone, pairA, pairB, tripleC };

            Assert.AreEqual(1, charge.SelectTarget(triple, Vector3.zero, lone.Transform), "3 >= 1 + 2 라 교체");
        }

        [Test]
        public void 현재_목표가_후보에_없으면_최고_점수로_즉시_선택한다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            GameObject gone = new GameObject("gone");
            _spawned.Add(gone);
            List<ClusterCandidate> list = new List<ClusterCandidate> { Cand(0f, 1f), Cand(5f, 5f), Cand(5.5f, 5f) };

            Assert.AreEqual(1, charge.SelectTarget(list, Vector3.zero, gone.transform));
        }

        //# ===== 재평가 주기 =====

        [Test]
        public void 재평가는_주기마다이고_목표가_무효면_즉시이며_갈고리_직후에는_유효_목표_교체를_막는다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());

            Assert.IsTrue(charge.ShouldReevaluate(0f, false), "첫 호출");
            Assert.IsFalse(charge.ShouldReevaluate(0.5f, false), "주기 1초 미만");
            Assert.IsTrue(charge.ShouldReevaluate(0.5f, true), "목표 무효는 즉시");
            Assert.IsTrue(charge.ShouldReevaluate(2.0f, false));

            charge.SuspendRetarget(3f, ChainPullRuntime.RetargetLockSec);
            Assert.IsFalse(charge.ShouldReevaluate(3.2f, false), "차단 중");
            Assert.IsFalse(charge.ShouldReevaluate(3.5f, false), "차단 중 - 0.6초");
            Assert.IsTrue(charge.ShouldReevaluate(3.7f, false), "차단 해제");
        }

        //# ===== 돌진 상태기계 =====

        private static ChargeInput Frame(float dist, bool changed = false)
        {
            return new ChargeInput { HasTarget = true, TargetChanged = changed, TargetDist = dist, Range = 1.8f, EngageBuffer = 0.5f };
        }

        [Test]
        public void 목표가_없으면_Seek이고_멀면_Move_사거리_안이면_Engage다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            Assert.AreEqual(EChargeState.Seek, charge.State);

            charge.Update(0f, new ChargeInput { HasTarget = false });
            Assert.AreEqual(EChargeState.Seek, charge.State);

            charge.Update(0f, Frame(5f, true));
            Assert.AreEqual(EChargeState.Move, charge.State);
            charge.Update(1f, Frame(1.5f));
            Assert.AreEqual(EChargeState.Engage, charge.State);
        }

        [Test]
        public void Seek에서_사거리_안_목표를_만나면_최소_유지_시간_없이_바로_Engage다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());

            charge.Update(0f, Frame(1f));

            Assert.AreEqual(EChargeState.Engage, charge.State);
        }

        [Test]
        public void Move에서_Engage_전이는_minDwellSec_0_4초가_지나야_하고_새_목표는_면제다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            charge.Update(0f, Frame(5f, true));

            charge.Update(0.1f, Frame(1.5f));
            Assert.AreEqual(EChargeState.Move, charge.State, "0.1초 - 진동 방지");
            charge.Update(0.5f, Frame(1.5f));
            Assert.AreEqual(EChargeState.Engage, charge.State, "0.5초 경과");
        }

        [Test]
        public void Engage는_사거리_플러스_버퍼_0_5를_넘고_유지_시간이_지나야_Move로_돌아간다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            charge.Update(0f, Frame(1f));

            charge.Update(0.1f, Frame(3f));
            Assert.AreEqual(EChargeState.Engage, charge.State, "유지 시간 미경과");
            charge.Update(1f, Frame(2.2f));
            Assert.AreEqual(EChargeState.Engage, charge.State, "버퍼(2.3) 안");
            charge.Update(1.1f, Frame(2.4f));
            Assert.AreEqual(EChargeState.Move, charge.State, "버퍼 밖 + 유지 시간 경과");
        }

        [Test]
        public void 새_목표는_유지_시간을_무시하고_Engage에서_바로_Move가_된다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            charge.Update(0f, Frame(1f));

            charge.Update(0.1f, Frame(5f, true));

            Assert.AreEqual(EChargeState.Move, charge.State);
        }

        [Test]
        public void 목표를_잃으면_Seek이고_Reset은_상태를_초기화한다()
        {
            ChargeBehavior charge = new ChargeBehavior(Shipped());
            charge.Update(0f, Frame(1f));

            charge.Update(0.1f, new ChargeInput { HasTarget = false });
            Assert.AreEqual(EChargeState.Seek, charge.State);

            charge.Update(0.2f, Frame(1f));
            charge.Reset();
            Assert.AreEqual(EChargeState.Seek, charge.State);
            Assert.IsTrue(charge.ShouldReevaluate(0f, false), "Reset 후 평가 타이머도 초기화");
        }

        [Test]
        public void 목적지는_arenaSoftRadius_안으로_클램프된다()
        {
            Vector3 clamped = KiteBehavior.ClampToRadius(new Vector3(20f, 0f, 0f), Shipped().ArenaSoftRadius);

            Assert.AreEqual(7f, new Vector3(clamped.x, 0f, clamped.z).magnitude, 0.001f);
        }

        //# ===== 범위 베기 =====

        private class RecordingCleaveSource : ICleaveTargetSource
        {
            public readonly List<IHealth> Targets = new List<IHealth>();
            public float LastRadius;
            public int LastMax;
            public int Calls;

            public int CollectNearest(Vector3 center, float radius, int maxTargets, List<IHealth> results)
            {
                ++Calls;
                LastRadius = radius;
                LastMax = maxTargets;
                results.Clear();
                results.AddRange(Targets);
                return results.Count;
            }
        }

        private MeleeAttacker NewAttacker(int power)
        {
            GameObject go = new GameObject("BerserkerHost");
            _spawned.Add(go);
            MeleeAttacker attacker = go.AddComponent<MeleeAttacker>();
            attacker.Configure(2f, 1f, power);
            return attacker;
        }

        private static void Strike(MeleeAttacker attacker, IHealth primary)
        {
            Assert.IsTrue(attacker.TryBeginAttack(primary, Vector3.zero, new Vector3(1f, 0f, 0f), 0f));
            Assert.IsTrue(attacker.TryApplyStrike(0.2f));
        }

        [Test]
        public void DamageMulOf는_첫_대상_100퍼센트_나머지_SecondaryMul이다()
        {
            CleaveSettings cleave = new CleaveSettings(2.2f, 4, 0.5f);

            Assert.AreEqual(1f, cleave.DamageMulOf(0));
            Assert.AreEqual(0.5f, cleave.DamageMulOf(1));
            Assert.AreEqual(0.5f, cleave.DamageMulOf(3));
        }

        [Test]
        public void 범위_베기는_반경_2_2_최대_4마리를_조회하고_첫_대상_100퍼센트_나머지_50퍼센트를_준다()
        {
            MeleeAttacker attacker = NewAttacker(100);
            RecordingCleaveSource source = new RecordingCleaveSource();
            FakeHealth[] mobs = { new FakeHealth(), new FakeHealth(), new FakeHealth(), new FakeHealth() };
            source.Targets.AddRange(mobs);
            attacker.ConfigureCleave(new CleaveSettings(2.2f, 4, 0.5f), source);
            int hits = 0;
            attacker.OnHit += _ => ++hits;

            Strike(attacker, mobs[0]);

            Assert.AreEqual(2.2f, source.LastRadius, 0.0001f);
            Assert.AreEqual(4, source.LastMax);
            Assert.AreEqual(100, mobs[0].LastDamage);
            for (int i = 1; i < 4; ++i)
            {
                Assert.AreEqual(50, mobs[i].LastDamage, $"대상 {i}");
                Assert.AreEqual(1, mobs[i].DamageCallCount);
            }
            Assert.AreEqual(1, hits, "OnHit 은 베기 1회당 1번");
        }

        [Test]
        public void 범위_안_대상이_없으면_목표_1마리에게_단일_판정으로_돌아간다()
        {
            MeleeAttacker attacker = NewAttacker(100);
            RecordingCleaveSource source = new RecordingCleaveSource();
            attacker.ConfigureCleave(new CleaveSettings(2.2f, 4, 0.5f), source);
            FakeHealth primary = new FakeHealth();

            Strike(attacker, primary);

            Assert.AreEqual(1, source.Calls);
            Assert.AreEqual(100, primary.LastDamage);
            Assert.AreEqual(1, primary.DamageCallCount);
        }

        [Test]
        public void 베기_설정이_없는_근접_공격자는_조회_없이_단일_판정이다()
        {
            MeleeAttacker attacker = NewAttacker(100);
            FakeHealth primary = new FakeHealth();

            Strike(attacker, primary);

            Assert.AreEqual(100, primary.LastDamage);
            Assert.AreEqual(1, primary.DamageCallCount);
        }

        //# ===== 스킬 3종 =====

        //# IBerserkerSkillContext 호출 기록 더블 - 모든 멤버 구현(부분 더블 금지).
        private class RecordingBerserkerContext : IBerserkerSkillContext
        {
            public Vector3 HeroPosition { get; set; } = Vector3.zero;
            public Transform HeroTransform { get; set; }
            public bool IsHeroActive { get; set; } = true;
            public int Nearby;
            public bool HasDensePoint = true;
            public Vector3 DensePoint = new Vector3(5f, 0f, 0f);
            public float ArenaRadius = 7f;

            public readonly List<Vector3> Leaps = new List<Vector3>();
            public readonly List<float> Invulns = new List<float>();
            public readonly List<float> TakenMuls = new List<float>();
            public readonly List<float> CooldownMuls = new List<float>();
            public readonly List<float> RetargetLocks = new List<float>();
            public readonly List<int> PullDamage = new List<int>();
            public readonly List<int> PullMaxTargets = new List<int>();
            public readonly List<float> CountRadii = new List<float>();
            public readonly List<float[]> Rings = new List<float[]>();
            public int PullTicks;

            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength)
            {
                Rings.Add(new[] { innerRadius, outerRadius, amount, knockbackStrength });
                return 0;
            }

            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
            public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }

            public int CountMonstersInRadius(float radius)
            {
                CountRadii.Add(radius);
                return Nearby;
            }

            public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point)
            {
                point = DensePoint;
                return HasDensePoint;
            }

            public Vector3 ClampToArena(Vector3 position) { return KiteBehavior.ClampToRadius(position, ArenaRadius); }
            public void BeginLeap(Vector3 destination, float duration) { Leaps.Add(destination); }
            public void GrantHeroInvulnerability(float seconds) { Invulns.Add(seconds); }
            public void MultiplyHeroDamageTaken(float mul) { TakenMuls.Add(mul); }
            public void MultiplyHeroAttackCooldown(float mul) { CooldownMuls.Add(mul); }
            public void SuspendHeroRetarget(float seconds) { RetargetLocks.Add(seconds); }

            public int BeginMonsterPull(float radius, int maxTargets, float pullToDistance, float durationSec, int damage)
            {
                PullMaxTargets.Add(maxTargets);
                PullDamage.Add(damage);
                return maxTargets;
            }

            public void TickMonsterPull(float dt) { ++PullTicks; }
        }

        [Test]
        public void 스킬_기본_수치와_우선순위는_갈고리_광란_도약_순이다()
        {
            CrushingLeapSkillData leap = NewAsset<CrushingLeapSkillData>();
            ChainPullSkillData pull = NewAsset<ChainPullSkillData>();
            BloodFrenzySkillData frenzy = NewAsset<BloodFrenzySkillData>();

            Assert.AreEqual(90, leap.Damage);
            Assert.AreEqual(7f, leap.Cooldown, 0.0001f);
            Assert.AreEqual(3f, leap.MinLeapDistance, 0.0001f);
            Assert.AreEqual(0.3f, leap.InvulnerableSec, 0.0001f);
            Assert.AreEqual(40, pull.Damage);
            Assert.AreEqual(6, pull.MaxTargets);
            Assert.AreEqual(4, pull.TriggerMonsterCount);
            Assert.AreEqual(0.6f, frenzy.AttackCooldownMul, 0.0001f);
            Assert.AreEqual(1.2f, frenzy.DamageTakenMul, 0.0001f);
            Assert.AreEqual(6f, frenzy.DurationSec, 0.0001f);
            Assert.Greater(pull.CastPriority, frenzy.CastPriority);
            Assert.Greater(frenzy.CastPriority, leap.CastPriority);
        }

        [Test]
        public void 분쇄_도약은_3_0_이상_8_0_이하일_때만_무적과_함께_도약하고_착지에서_링_피해를_준다()
        {
            CrushingLeapSkillData data = NewAsset<CrushingLeapSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { DensePoint = new Vector3(5f, 0f, 0f) };

            runtime.Tick(ctx, 6.9f);
            Assert.AreEqual(0, ctx.Leaps.Count, "쿨다운 7초 미경과");

            runtime.Tick(ctx, 0.2f);
            Assert.AreEqual(1, ctx.Leaps.Count);
            Assert.AreEqual(5f, ctx.Leaps[0].x, 0.001f);
            Assert.AreEqual(0.3f, ctx.Invulns[0], 0.0001f);

            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.Rings.Count, "착지");
            Assert.AreEqual(2.6f, ctx.Rings[0][1], 0.0001f);
            Assert.AreEqual(90f, ctx.Rings[0][2]);
        }

        [TestCase(2.9f, false)]
        [TestCase(3.0f, true)]
        [TestCase(8.0f, true)]
        [TestCase(8.5f, false)]
        public void 분쇄_도약은_최소_거리_3_0_미만이거나_도약_사거리_8_0_밖이면_시전하지_않는다(float distance, bool expectedCast)
        {
            CrushingLeapSkillData data = NewAsset<CrushingLeapSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { DensePoint = new Vector3(distance, 0f, 0f), ArenaRadius = 20f };

            runtime.Tick(ctx, 7.5f);

            Assert.AreEqual(expectedCast, ctx.Leaps.Count == 1);
            Assert.AreEqual(expectedCast ? 1 : 0, ctx.Invulns.Count);
        }

        [Test]
        public void 분쇄_도약_목적지는_전장_반경_안으로_클램프된다()
        {
            CrushingLeapSkillData data = NewAsset<CrushingLeapSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { DensePoint = new Vector3(7.8f, 0f, 0f), ArenaRadius = 7f };

            runtime.Tick(ctx, 7.5f);

            Assert.AreEqual(1, ctx.Leaps.Count);
            Assert.AreEqual(7f, ctx.Leaps[0].magnitude, 0.001f);
        }

        [Test]
        public void 사슬_갈고리는_반경_5_안_4마리_이상일_때만_최대_6마리에_40_피해로_시전하고_재평가를_0_6초_막는다()
        {
            ChainPullSkillData data = NewAsset<ChainPullSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { Nearby = 3 };

            runtime.Tick(ctx, 10.5f);
            Assert.AreEqual(0, ctx.PullDamage.Count, "3마리는 미시전");
            Assert.AreEqual(5f, ctx.CountRadii[0], 0.0001f);

            ctx.Nearby = 4;
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.PullDamage.Count);
            Assert.AreEqual(40, ctx.PullDamage[0]);
            Assert.AreEqual(6, ctx.PullMaxTargets[0]);
            Assert.AreEqual(0.6f, ctx.RetargetLocks[0], 0.0001f);
            Assert.AreEqual(2, ctx.PullTicks, "당김 진행은 매 Tick");
        }

        [Test]
        public void 피의_광란은_쿨다운_0_6배_받는_피해_1_2배를_걸고_6초_뒤_역수로_복구한다()
        {
            BloodFrenzySkillData data = NewAsset<BloodFrenzySkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { Nearby = 3 };

            runtime.Tick(ctx, 18.5f);
            Assert.AreEqual(1, ctx.CooldownMuls.Count);
            Assert.AreEqual(0.6f, ctx.CooldownMuls[0], 0.0001f);
            Assert.AreEqual(1.2f, ctx.TakenMuls[0], 0.0001f);

            runtime.Tick(ctx, 5.9f);
            Assert.AreEqual(1, ctx.CooldownMuls.Count, "6초 미경과");

            runtime.Tick(ctx, 0.2f);
            Assert.AreEqual(2, ctx.CooldownMuls.Count);
            Assert.AreEqual(1f / 0.6f, ctx.CooldownMuls[1], 0.0001f);
            Assert.AreEqual(1f / 1.2f, ctx.TakenMuls[1], 0.0001f);
            Assert.AreEqual(1f, ctx.CooldownMuls[0] * ctx.CooldownMuls[1], 0.0001f);
        }

        [Test]
        public void 피의_광란은_광란_도중_비활성화되어도_배율을_한번만_원복한다()
        {
            BloodFrenzySkillData data = NewAsset<BloodFrenzySkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { Nearby = 3 };
            runtime.Tick(ctx, 18.5f);

            runtime.OnDeactivate();
            runtime.OnDeactivate();

            Assert.AreEqual(2, ctx.CooldownMuls.Count, "시전 1 + 원복 1");
            Assert.AreEqual(2, ctx.TakenMuls.Count);
        }

        [Test]
        public void 영웅이_행동_불가면_세_스킬_모두_시전도_진행도_하지_않는다()
        {
            RecordingBerserkerContext ctx = new RecordingBerserkerContext { IsHeroActive = false, Nearby = 10 };
            IHeroSkillRuntime leap = NewAsset<CrushingLeapSkillData>().CreateRuntime();
            IHeroSkillRuntime pull = NewAsset<ChainPullSkillData>().CreateRuntime();
            IHeroSkillRuntime frenzy = NewAsset<BloodFrenzySkillData>().CreateRuntime();

            leap.Tick(ctx, 30f);
            pull.Tick(ctx, 30f);
            frenzy.Tick(ctx, 30f);

            Assert.AreEqual(0, ctx.Leaps.Count);
            Assert.AreEqual(0, ctx.PullDamage.Count);
            Assert.AreEqual(0, ctx.CooldownMuls.Count);
            Assert.AreEqual(0, ctx.PullTicks);
        }
    }
}
