using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 암살자(Skirmisher) AI 상태기계·표적 점수·스킬 3종·데이터 자체 스모크 (chapter-3-hero §3.3·§4). 본격 스위트는 test-engineer.
    public class AssassinTests
    {
        private const string SkillDir = "Assets/_Lair/Art/Skills/";
        private const string JsonPath = "Assets/_Lair/Data/Json/hero_skills.json";

        //# 암살자 FX 프리팹은 아트 슬라이스 소관 - 에셋 부재 LogError(키별 1회 정적 캐시)가 실행 순서에 따라 새지 않게 격리한다.
        [SetUp]
        public void 암살자_FX_로그_캐시를_격리한다()
        {
            ArcherFxLogGuard.Suppress();
        }

        [TearDown]
        public void 암살자_FX_로그_캐시를_원복한다()
        {
            ArcherFxLogGuard.Restore();
        }

        private static SkirmishSettings Settings()
        {
            return new SkirmishSettings(9f, 1.5f, 2.8f, 3, 2f, 3.5f, 0.2f, 0.2f, 5f, 0.4f, 7f);
        }

        private static SkirmishInput Engaged(int crowd = 0, bool targetChanged = false)
        {
            return new SkirmishInput { Position = Vector3.zero, HasTarget = true, TargetChanged = targetChanged, TargetDist = 1f, Range = 1.6f, EngageBuffer = 0.5f, CrowdCount = crowd };
        }

        private static SkirmishInput Far()
        {
            return new SkirmishInput { Position = Vector3.zero, HasTarget = true, TargetDist = 5f, Range = 1.6f, EngageBuffer = 0.5f };
        }

        //# ===== 상태기계 =====

        [Test]
        public void 사거리_밖이면_추적_안이면_교전이고_교전은_버퍼를_넘어야_풀린다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());

            b.Update(0f, Far());
            Assert.AreEqual(ESkirmishState.Chase, b.State);
            b.Update(0.1f, Engaged());
            Assert.AreEqual(ESkirmishState.Engage, b.State);
            Assert.IsTrue(b.CanAttack);

            SkirmishInput edge = Engaged();
            edge.TargetDist = 1.9f;   //# range 1.6 < 1.9 <= range+buffer 2.1
            b.Update(0.2f, edge);
            Assert.AreEqual(ESkirmishState.Engage, b.State, "히스테리시스 안");
            edge.TargetDist = 2.2f;
            b.Update(0.3f, edge);
            Assert.AreEqual(ESkirmishState.Chase, b.State);
            Assert.IsFalse(b.CanAttack);
        }

        [Test]
        public void 한_표적과_2_8초_교전하면_슬립을_시작한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            bool slipped = false;
            float now = 0f;
            b.Update(now, Engaged(0, true));
            while (now < 2.7f)
            {
                now += 0.1f;
                slipped |= b.Update(now, Engaged());
            }
            Assert.IsFalse(slipped, "2.7초까지는 교전 유지");

            now += 0.2f;
            slipped = b.Update(now, Engaged());
            Assert.IsTrue(slipped);
            Assert.AreEqual(ESkirmishState.Slip, b.State);
        }

        [Test]
        public void 군중_3마리면_슬립하되_최소_유지시간_0_4초_전에는_진동하지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged());   //# 교전 진입 시각 0

            Assert.IsFalse(b.Update(0.1f, Engaged(3)), "minDwell 미경과");
            Assert.IsTrue(b.Update(0.5f, Engaged(3)));
            Assert.AreEqual(ESkirmishState.Slip, b.State);
        }

        [Test]
        public void 군중_2마리는_슬립_조건이_아니다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged());

            Assert.IsFalse(b.Update(1f, Engaged(2)));
            Assert.AreEqual(ESkirmishState.Engage, b.State);
        }

        [Test]
        public void 슬립이_끝나면_추적으로_돌아가고_쿨다운_5초_안에는_다시_슬립하지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(0.5f, Engaged(3)));

            b.Update(0.8f, Far());   //# AI 가 도약 뒤 처음 부르는 호출 — 슬립 종료
            Assert.AreEqual(ESkirmishState.Chase, b.State);

            b.Update(1.0f, Engaged());
            Assert.IsFalse(b.Update(2.0f, Engaged(4)), "슬립 후 1.5초 — 쿨다운 중");
            Assert.IsFalse(b.Update(5.4f, Engaged(4)), "슬립 시작(0.5)부터 4.9초");
            Assert.IsTrue(b.Update(5.6f, Engaged(4)), "5.1초 경과");
        }

        [Test]
        public void 표적이_바뀌면_교전_시간을_새로_센다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            float now = 0f;
            b.Update(now, Engaged(0, true));
            for (int i = 0; i < 20; ++i)
            {
                now += 0.1f;
                b.Update(now, Engaged());   //# 2.0초 누적
            }

            now += 0.1f;
            b.Update(now, Engaged(0, true));   //# 표적 교체 → 리셋
            now += 0.9f;
            Assert.IsFalse(b.Update(now, Engaged()), "교체 후 0.9초 — 2.8초 미달");
        }

        [Test]
        public void 일시정지처럼_프레임_간격이_1초를_넘으면_그_구간은_교전_시간에_더하지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged(0, true));
            b.Update(0.1f, Engaged());
            b.Update(5.1f, Engaged());   //# 5초 정지 뒤 재개
            Assert.IsFalse(b.Update(5.2f, Engaged()), "정지 구간은 미산입");
            Assert.AreEqual(ESkirmishState.Engage, b.State);
        }

        [Test]
        public void 공격_모션_0_6초_정지_구간은_교전_시간에_산입된다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            float now = 0f;
            b.Update(now, Engaged(0, true));
            bool slipped = false;
            for (int i = 0; i < 6 && slipped == false; ++i)
            {
                now += 0.75f;   //# 공격 모션 때문에 0.75초 간격으로만 AI 가 호출되는 최악 상황
                slipped = b.Update(now, Engaged());
            }
            Assert.IsTrue(slipped, "4번째 호출(누적 3.0초)에 슬립");
        }

        [Test]
        public void 강제_슬립_요청은_쿨다운과_최소_유지시간을_무시한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(0.5f, Engaged(3)));
            b.Update(0.8f, Far());

            b.RequestForcedSlip();
            Assert.IsTrue(b.Update(0.9f, Engaged()), "쿨다운 중이어도 1회 발동");
            b.Update(1.2f, Far());
            Assert.IsFalse(b.Update(1.3f, Engaged(4)), "요청은 소비됐고 쿨다운은 다시 유효");
        }

        [Test]
        public void 표적이_없으면_추적_상태이고_슬립하지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            SkirmishInput none = new SkirmishInput { Position = Vector3.zero, HasTarget = false };

            Assert.IsFalse(b.Update(0f, none));
            Assert.AreEqual(ESkirmishState.Chase, b.State);
        }

        [Test]
        public void Reset은_상태_쿨다운_강제슬립을_초기화한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Settings());
            b.Update(0f, Engaged());
            b.Update(0.5f, Engaged(3));
            b.RequestForcedSlip();

            b.Reset();

            Assert.AreEqual(ESkirmishState.Chase, b.State);
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(0.5f, Engaged(3)), "쿨다운 기록 초기화");
        }

        //# ===== 슬립 목적지 =====

        [Test]
        public void 슬립_목적지는_군중_무게중심_반대로_slipDistance만큼이다()
        {
            Vector3 dest = SkirmishBehavior.ComputeSlipDestination(new Vector3(1f, 0f, 0f), true, new Vector3(2f, 0f, 0f), 3.5f, p => p);

            Assert.AreEqual(-2.5f, dest.x, 0.001f);
            Assert.AreEqual(0f, dest.z, 0.001f);
        }

        [Test]
        public void 군중_중심이_없으면_원점_쪽으로_이탈한다()
        {
            Vector3 dest = SkirmishBehavior.ComputeSlipDestination(new Vector3(4f, 0f, 0f), false, Vector3.zero, 3.5f, p => p);

            Assert.AreEqual(0.5f, dest.x, 0.001f);
        }

        [Test]
        public void 가장자리에서_바깥으로_밀리면_원점_방향을_섞어_더_멀리_간다()
        {
            Vector3 position = new Vector3(7f, 0f, 0f);
            Vector3 centroid = new Vector3(4f, 0f, 0f);   //# 반대 방향 = 바깥(+x)
            System.Func<Vector3, Vector3> clamp = p => KiteBehavior.ClampToRadius(p, 7f);

            Vector3 dest = SkirmishBehavior.ComputeSlipDestination(position, true, centroid, 3.5f, clamp);

            Assert.GreaterOrEqual(Vector3.Distance(dest, position), 3.5f * 0.5f);
            Assert.LessOrEqual(dest.magnitude, 7.0001f);
        }

        //# ===== 위협 점수 / 표적 선정 =====

        private static GameObject MakeMonster(float power, float cooldown, int hp, Vector3 position)
        {
            GameObject go = new GameObject("m");
            go.transform.position = position;
            Health h = go.AddComponent<Health>();
            h.SetMax(hp, true);
            MeleeAttacker a = go.AddComponent<MeleeAttacker>();
            a.Configure(1f, cooldown, (int)power);
            return go;
        }

        private static float Score(GameObject go)
        {
            return SkirmishBehavior.ThreatScore(go.GetComponent<MeleeAttacker>(), go.GetComponent<Health>());
        }

        [Test]
        public void 기본_수치_위협_점수_순위는_Hex_Reaper_Phantom_Plague_Wisp_Wraith다()
        {
            //# 기획서 §1.3 표 — (공격력, 쿨다운, HP) = Hex 9/1/60 · Reaper 12/1/100 · Phantom 2.5→5/2/30(DPS 2.5) · Plague 2/1/50 · Wisp 5/1/200 · Wraith 10/1/500
            GameObject hex = MakeMonster(9, 1f, 60, Vector3.zero);
            GameObject reaper = MakeMonster(12, 1f, 100, Vector3.zero);
            GameObject phantom = MakeMonster(5, 2f, 30, Vector3.zero);
            GameObject plague = MakeMonster(2, 1f, 50, Vector3.zero);
            GameObject wisp = MakeMonster(5, 1f, 200, Vector3.zero);
            GameObject wraith = MakeMonster(10, 1f, 500, Vector3.zero);

            Assert.AreEqual(0.150f, Score(hex), 0.0005f);
            Assert.AreEqual(0.120f, Score(reaper), 0.0005f);
            Assert.AreEqual(0.0833f, Score(phantom), 0.0005f);
            Assert.AreEqual(0.040f, Score(plague), 0.0005f);
            Assert.AreEqual(0.025f, Score(wisp), 0.0005f);
            Assert.AreEqual(0.020f, Score(wraith), 0.0005f);
            Assert.Greater(Score(hex), Score(reaper));
            Assert.Greater(Score(reaper), Score(phantom));
            Assert.Greater(Score(phantom), Score(plague));
            Assert.Greater(Score(plague), Score(wisp));
            Assert.Greater(Score(wisp), Score(wraith));

            foreach (GameObject go in new[] { hex, reaper, phantom, plague, wisp, wraith })
            {
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void HP를_올리면_점수가_내려가고_공격력을_올리면_올라간다()
        {
            GameObject m = MakeMonster(10, 1f, 100, Vector3.zero);
            float baseScore = Score(m);

            m.GetComponent<Health>().SetMax(200, true);
            Assert.Less(Score(m), baseScore, "Tank 축 — 표적 순위 하락");

            m.GetComponent<Health>().SetMax(100, true);
            m.GetComponent<MeleeAttacker>().Configure(1f, 1f, 20);
            Assert.Greater(Score(m), baseScore, "Dps 축 — 표적 순위 상승");
            Object.DestroyImmediate(m);
        }

        [Test]
        public void 공격자가_없거나_HP가_0이어도_점수는_안전하다()
        {
            Assert.AreEqual(0f, SkirmishBehavior.ThreatScore(null, null));
            GameObject m = MakeMonster(10, 1f, 100, Vector3.zero);
            m.GetComponent<Health>().TakeDamage(1000);
            Assert.AreEqual(10f, Score(m), 0.001f, "HP 분모 하한 1");
            Object.DestroyImmediate(m);
        }

        private static SkirmishCandidate Cand(Transform t, float score, Vector3 pos)
        {
            return new SkirmishCandidate { Transform = t, Position = pos, Score = score };
        }

        [Test]
        public void 표적은_점수_최상위이고_동점이면_가까운_쪽이다()
        {
            GameObject a = new GameObject("a");
            GameObject b = new GameObject("b");
            GameObject c = new GameObject("c");
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Cand(a.transform, 0.05f, new Vector3(1f, 0f, 0f)),
                Cand(b.transform, 0.10f, new Vector3(6f, 0f, 0f)),
                Cand(c.transform, 0.10f, new Vector3(3f, 0f, 0f)),
            };

            Assert.AreEqual(2, SkirmishBehavior.SelectTarget(list, Vector3.zero, null, 1.5f));
            Assert.AreEqual(-1, SkirmishBehavior.SelectTarget(new List<SkirmishCandidate>(), Vector3.zero, null, 1.5f));
            Object.DestroyImmediate(a);
            Object.DestroyImmediate(b);
            Object.DestroyImmediate(c);
        }

        [Test]
        public void 현재_표적은_다른_후보_점수가_1_5배를_넘을_때만_교체된다()
        {
            GameObject cur = new GameObject("cur");
            GameObject other = new GameObject("other");
            Vector3 p = Vector3.zero;

            List<SkirmishCandidate> within = new List<SkirmishCandidate> { Cand(cur.transform, 0.10f, p), Cand(other.transform, 0.14f, p) };
            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(within, p, cur.transform, 1.5f), "1.4배 — 유지");

            List<SkirmishCandidate> over = new List<SkirmishCandidate> { Cand(cur.transform, 0.10f, p), Cand(other.transform, 0.16f, p) };
            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(over, p, cur.transform, 1.5f), "1.6배 — 교체");

            List<SkirmishCandidate> gone = new List<SkirmishCandidate> { Cand(other.transform, 0.01f, p) };
            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(gone, p, cur.transform, 1.5f), "현재 표적이 후보에 없으면(사망·봉인) 최상위");
            Object.DestroyImmediate(cur);
            Object.DestroyImmediate(other);
        }

        [Test]
        public void 쿨다운_버프는_위협_점수에_반영된다()
        {
            GameObject m = MakeMonster(10, 1f, 100, Vector3.zero);
            float baseScore = Score(m);

            m.GetComponent<MeleeAttacker>().CooldownScale = 0.67f;   //# 프렌지 — 공격이 빨라짐

            Assert.AreEqual(baseScore / 0.67f, Score(m), 0.0001f);
            Object.DestroyImmediate(m);
        }

        [Test]
        public void 보류_중인_strike를_취소하면_타격이_헛스윙이_된다()
        {
            GameObject hero = MakeMonster(10, 1f, 100, Vector3.zero);
            GameObject target = MakeMonster(1, 1f, 100, new Vector3(1f, 0f, 0f));
            MeleeAttacker attacker = hero.GetComponent<MeleeAttacker>();
            Assert.IsTrue(attacker.TryBeginAttack(target.GetComponent<Health>(), Vector3.zero, new Vector3(1f, 0f, 0f), 10f));

            (attacker as IPendingStrikeCancel).CancelPendingStrike();

            Assert.IsFalse(attacker.TryApplyStrike(10.2f));
            Assert.AreEqual(100, target.GetComponent<Health>().Current);
            Object.DestroyImmediate(hero);
            Object.DestroyImmediate(target);
        }

        //# ===== 레지스트리 후보 =====

        [Test]
        public void 레지스트리_후보는_반경_안_교전_몬스터만_점수와_함께_돌려준다()
        {
            List<CharacterRegistry.Entry> saved = new List<CharacterRegistry.Entry>(CharacterRegistry.Monsters);
            CharacterRegistry.Monsters.Clear();
            GameObject near = MakeMonster(12, 1f, 100, new Vector3(2f, 0f, 0f));
            GameObject far = MakeMonster(12, 1f, 100, new Vector3(20f, 0f, 0f));
            GameObject marching = MakeMonster(12, 1f, 100, new Vector3(3f, 0f, 0f));
            try
            {
                CharacterRegistry.RegisterMonster(near.transform, near.GetComponent<Health>(), near.GetComponent<MeleeAttacker>());
                CharacterRegistry.RegisterMonster(far.transform, far.GetComponent<Health>(), far.GetComponent<MeleeAttacker>());
                CharacterRegistry.RegisterMonster(marching.transform, marching.GetComponent<Health>(), marching.GetComponent<MeleeAttacker>());
                CharacterRegistry.SetMonsterEngaging(near.transform, true);
                CharacterRegistry.SetMonsterEngaging(far.transform, true);

                List<SkirmishCandidate> list = new List<SkirmishCandidate>();
                bool found = CharacterRegistry.TryGetThreatCandidates(Vector3.zero, 9f, list);

                Assert.IsTrue(found);
                Assert.AreEqual(1, list.Count, "반경 밖·미교전(행군 중) 제외");
                Assert.AreEqual(near.transform, list[0].Transform);
                Assert.AreEqual(0.12f, list[0].Score, 0.0005f);
            }
            finally
            {
                CharacterRegistry.Monsters.Clear();
                CharacterRegistry.Monsters.AddRange(saved);
                Object.DestroyImmediate(near);
                Object.DestroyImmediate(far);
                Object.DestroyImmediate(marching);
            }
        }

        //# ===== 무적 / 사거리 보정 =====

        [Test]
        public void 무적을_부여하면_데미지를_무시한다()
        {
            GameObject go = new GameObject("hero");
            Health h = go.AddComponent<Health>();
            h.SetMax(100, true);

            h.GrantInvulnerability(0.2f);
            h.TakeDamage(50);
            Assert.AreEqual(100, h.Current);
            Assert.IsTrue(h.IsInvulnerable);

            Health plain = new GameObject("p").AddComponent<Health>();
            plain.SetMax(100, true);
            plain.GrantInvulnerability(0f);
            plain.TakeDamage(10);
            Assert.AreEqual(90, plain.Current, "0초 무적은 무효");
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(plain.gameObject);
        }

        [Test]
        public void 사거리_보정은_하한_아래로_줄지_않고_원래_사거리가_하한보다_작으면_늘리지_않는다()
        {
            Assert.AreEqual(0.8f, MonsterRangeModifier.Compute(1.5f, 0.5f, 0.8f), 0.0001f);
            Assert.AreEqual(1.0f, MonsterRangeModifier.Compute(2.0f, 0.5f, 0.8f), 0.0001f);
            Assert.AreEqual(0.5f, MonsterRangeModifier.Compute(0.5f, 0.5f, 0.8f), 0.0001f);
        }

        [Test]
        public void 공격자_사거리_보정은_스케일_1이면_원래_값이고_해제하면_복구된다()
        {
            GameObject go = new GameObject("m");
            MeleeAttacker a = go.AddComponent<MeleeAttacker>();
            a.Configure(2.0f, 1f, 5);
            Assert.AreEqual(2.0f, a.Range, 0.0001f);

            a.SetRangeScale(0.5f, 0.8f);
            Assert.AreEqual(1.0f, a.Range, 0.0001f);

            a.SetRangeScale(1f, 0f);
            Assert.AreEqual(2.0f, a.Range, 0.0001f);
            Object.DestroyImmediate(go);
        }

        //# ===== 스킬 =====

        private class FakeAssassinCtx : IAssassinSkillContext
        {
            public Vector3 HeroPosition { get; set; } = Vector3.zero;
            public bool IsHeroActive { get; set; } = true;
            public int Nearby;
            public bool HasThreat = true;
            public Vector3 ThreatPosition = new Vector3(6f, 0f, 0f);
            public bool KillOnDamage;
            public float LastTopThreatRange;
            public readonly List<int> MonsterDamage = new List<int>();
            public readonly List<Vector3> Teleports = new List<Vector3>();
            public readonly List<float> Invulns = new List<float>();
            public readonly List<float> TakenMuls = new List<float>();
            public int RingTicks;
            public int SlipRequests;
            public int ZoneApplies;
            public int ZoneClears;
            public Vector3 LastZoneCenter;
            public float LastZoneSpeed;
            public float LastZoneRange;
            public float LastZoneMinRange;

            public int DamageMonstersInRing(float innerRadius, float outerRadius, int amount, float knockbackStrength) { RingTicks++; return 0; }
            public int DamageMonstersInCone(Vector3 direction, float length, float halfAngleDeg, int amount, float knockbackStrength) { return 0; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> sphereCenters, float sphereRadius, int amount, float knockbackStrength) { return 0; }
            public Vector3 MonsterCentroid(float radius) { return Vector3.zero; }
            public int CountMonstersInRadius(float radius) { return Nearby; }
            public bool TryFindTopThreat(float maxRange, out Vector3 position, out IHealth health)
            {
                LastTopThreatRange = maxRange;
                position = ThreatPosition;
                health = null;
                return HasThreat;
            }
            public bool DamageMonster(IHealth target, int amount) { MonsterDamage.Add(amount); return KillOnDamage; }
            public void TeleportHero(Vector3 destination) { Teleports.Add(destination); HeroPosition = destination; }
            public Vector3 ClampToArena(Vector3 position) { return KiteBehavior.ClampToRadius(position, 7f); }
            public void GrantHeroInvulnerability(float seconds) { Invulns.Add(seconds); }
            public void MultiplyHeroDamageTaken(float mul) { TakenMuls.Add(mul); }
            public void RequestHeroSlip() { SlipRequests++; }
            public void ApplyMonsterSlowZone(Vector3 center, float radius, float speedMul, float rangeMul, float minRange)
            {
                ZoneApplies++;
                LastZoneCenter = center;
                LastZoneSpeed = speedMul;
                LastZoneRange = rangeMul;
                LastZoneMinRange = minRange;
            }
            public void ClearMonsterSlowZone() { ZoneClears++; }
        }

        [Test]
        public void 그림자_습격은_첫_쿨다운_뒤_표적_앞으로_순간이동하고_120_피해_0_3초_무적이다()
        {
            ShadowStrikeSkillData data = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx();

            runtime.Tick(ctx, 3f);
            Assert.AreEqual(0, ctx.Teleports.Count, "활성 직후 즉발 방지(쿨다운 6초)");

            runtime.Tick(ctx, 3.1f);
            Assert.AreEqual(1, ctx.Teleports.Count);
            Assert.AreEqual(4.8f, ctx.Teleports[0].x, 0.001f, "표적 x=6 에서 영웅 쪽 1.2 앞");
            Assert.AreEqual(120, ctx.MonsterDamage[0]);
            Assert.AreEqual(0.3f, ctx.Invulns[0], 0.0001f);
            Assert.AreEqual(9f, ctx.LastTopThreatRange, 0.0001f);

            runtime.Tick(ctx, 3f);
            Assert.AreEqual(1, ctx.Teleports.Count, "시전 후 쿨다운 6초");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 그림자_습격은_처치하면_쿨다운이_절반_환급되어_3초_뒤_다시_발동한다()
        {
            ShadowStrikeSkillData data = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { KillOnDamage = true };
            runtime.Tick(ctx, 6.1f);
            Assert.AreEqual(1, ctx.Teleports.Count);

            ctx.HeroPosition = Vector3.zero;
            runtime.Tick(ctx, 3.1f);
            Assert.AreEqual(2, ctx.Teleports.Count, "환급 — 6.0 → 3.0");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 그림자_습격은_표적이_2_5보다_가깝거나_없거나_정지_중이면_보류한다()
        {
            ShadowStrikeSkillData data = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { ThreatPosition = new Vector3(2f, 0f, 0f) };

            runtime.Tick(ctx, 7f);
            Assert.AreEqual(0, ctx.Teleports.Count, "거리 2 < 2.5");

            ctx.ThreatPosition = new Vector3(6f, 0f, 0f);
            ctx.IsHeroActive = false;
            runtime.Tick(ctx, 1f);
            Assert.AreEqual(0, ctx.Teleports.Count, "TimeStop 중 순간이동 금지");

            ctx.IsHeroActive = true;
            ctx.HasThreat = false;
            runtime.Tick(ctx, 1f);
            Assert.AreEqual(0, ctx.Teleports.Count, "표적 없음");

            ctx.HasThreat = true;
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.Teleports.Count, "조건 충족 즉시 발동(쿨다운은 소진된 상태)");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 도착_지점은_표적에서_영웅_방향_offset이고_도착이_전장_밖이면_클램프된다()
        {
            Vector3 arrival = ShadowStrikeRuntime.ComputeArrival(Vector3.zero, new Vector3(8f, 0f, 0f), 1.2f);
            Assert.AreEqual(6.8f, arrival.x, 0.001f);

            Vector3 clamped = KiteBehavior.ClampToRadius(new Vector3(9f, 0f, 0f), 7f);
            Assert.AreEqual(7f, clamped.x, 0.001f);
            Assert.AreEqual(3f, ShadowStrikeRuntime.ComputeCooldownAfterCast(6f, true, 0.5f), 0.0001f);
            Assert.AreEqual(6f, ShadowStrikeRuntime.ComputeCooldownAfterCast(6f, false, 0.5f), 0.0001f);
        }

        [Test]
        public void 칼날_선풍은_몬스터_2마리_이상일_때만_시전하고_4틱_피해_배율_0_7을_걸었다_푼다()
        {
            BladeWhirlSkillData data = ScriptableObject.CreateInstance<BladeWhirlSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { Nearby = 1 };

            runtime.Tick(ctx, 9.5f);
            Assert.AreEqual(0, ctx.RingTicks, "1마리 — 미시전");

            ctx.Nearby = 2;
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.RingTicks, "첫 타격은 시전 즉시");
            Assert.AreEqual(1, ctx.TakenMuls.Count);
            Assert.AreEqual(0.7f, ctx.TakenMuls[0], 0.0001f);

            runtime.Tick(ctx, 0.3f);
            runtime.Tick(ctx, 0.3f);
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(4, ctx.RingTicks, "총 4틱");
            Assert.AreEqual(2, ctx.TakenMuls.Count);
            Assert.AreEqual(1f / 0.7f, ctx.TakenMuls[1], 0.0001f, "종료 시 역수로 원복");

            runtime.Tick(ctx, 5f);
            Assert.AreEqual(4, ctx.RingTicks, "쿨다운 9초 — 재시전 없음");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 칼날_선풍은_중간에_비활성되면_피해_배율을_원복하고_정지_중엔_진행하지_않는다()
        {
            BladeWhirlSkillData data = ScriptableObject.CreateInstance<BladeWhirlSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { Nearby = 3 };
            runtime.Tick(ctx, 9.5f);
            Assert.AreEqual(1, ctx.RingTicks);

            ctx.IsHeroActive = false;
            runtime.Tick(ctx, 5f);
            Assert.AreEqual(1, ctx.RingTicks, "TimeStop 중 진행 정지");

            runtime.OnDeactivate();
            runtime.OnDeactivate();
            Assert.AreEqual(2, ctx.TakenMuls.Count, "원복 1회만(중복 안전)");
            Assert.AreEqual(1f / 0.7f, ctx.TakenMuls[1], 0.0001f);
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 연막_탄은_4마리_이상일_때_지대를_만들고_슬립을_요청하며_3초_뒤_해제한다()
        {
            SmokeBombSkillData data = ScriptableObject.CreateInstance<SmokeBombSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { Nearby = 3 };

            runtime.Tick(ctx, 12.5f);
            Assert.AreEqual(0, ctx.ZoneApplies, "3마리 — 미시전");

            ctx.Nearby = 4;
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.SlipRequests);
            Assert.AreEqual(1, ctx.ZoneApplies);
            Assert.AreEqual(0.6f, ctx.LastZoneSpeed, 0.0001f);
            Assert.AreEqual(0.5f, ctx.LastZoneRange, 0.0001f);
            Assert.AreEqual(0.8f, ctx.LastZoneMinRange, 0.0001f);

            runtime.Tick(ctx, 1f);
            Assert.AreEqual(2, ctx.ZoneApplies, "지대 유지 중 매 프레임 갱신");
            Assert.AreEqual(0, ctx.ZoneClears);

            runtime.Tick(ctx, 2.1f);
            Assert.AreEqual(1, ctx.ZoneClears, "3초 경과 — 원복");

            runtime.Tick(ctx, 5f);
            Assert.AreEqual(1, ctx.SlipRequests, "쿨다운 12초");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 연막_탄은_지대_중_비활성되면_원복하고_영웅_정지_중엔_시전하지_않는다()
        {
            SmokeBombSkillData data = ScriptableObject.CreateInstance<SmokeBombSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            FakeAssassinCtx ctx = new FakeAssassinCtx { Nearby = 5, IsHeroActive = false };

            runtime.Tick(ctx, 13f);
            Assert.AreEqual(0, ctx.SlipRequests, "TimeStop 중 시전 금지");

            ctx.IsHeroActive = true;
            runtime.Tick(ctx, 0.3f);
            Assert.AreEqual(1, ctx.ZoneApplies);

            runtime.OnDeactivate();
            runtime.OnDeactivate();
            Assert.AreEqual(1, ctx.ZoneClears, "원복 1회만(중복 안전)");
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 스킬_컨텍스트가_암살자용이_아니면_보류한다()
        {
            ShadowStrikeSkillData data = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            IHeroSkillRuntime runtime = data.CreateRuntime();
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex("IAssassinSkillContext"));

            Assert.DoesNotThrow(() => runtime.Tick(new PlainHeroSkillContext(), 7f));
            Object.DestroyImmediate(data);
        }

        [Test]
        public void 동시_조건_우선순위는_연막_탄_그림자_습격_칼날_선풍_순이다()
        {
            ShadowStrikeSkillData shadow = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            BladeWhirlSkillData whirl = ScriptableObject.CreateInstance<BladeWhirlSkillData>();
            SmokeBombSkillData smoke = ScriptableObject.CreateInstance<SmokeBombSkillData>();

            Assert.Greater(smoke.CastPriority, shadow.CastPriority);
            Assert.Greater(shadow.CastPriority, whirl.CastPriority);
            Assert.AreEqual(0, ScriptableObject.CreateInstance<DashStrikeSkillData>().CastPriority, "기존 스킬은 기본 우선순위");
            Object.DestroyImmediate(shadow);
            Object.DestroyImmediate(whirl);
            Object.DestroyImmediate(smoke);
        }

        //# ===== 데이터 일관성 =====

        private static JObject SkillJson(string fileName)
        {
            foreach (JToken token in (JArray)JObject.Parse(File.ReadAllText(JsonPath))["skills"])
            {
                if ((string)token["fileName"] == fileName)
                    return (JObject)token;
            }
            Assert.Fail($"hero_skills.json 에 {fileName} 없음");
            return null;
        }

        [Test]
        public void 암살자_스킬_asset은_JSON_수치와_일치한다()
        {
            ShadowStrikeSkillData shadow = AssetDatabase.LoadAssetAtPath<ShadowStrikeSkillData>(SkillDir + "HeroSkill_ShadowStrike.asset");
            JObject j = SkillJson("HeroSkill_ShadowStrike");
            Assert.IsNotNull(shadow);
            Assert.AreEqual((int)j["damage"], shadow.Damage);
            Assert.AreEqual((float)j["cooldown"], shadow.Cooldown, 0.0001f);
            Assert.AreEqual((float)j["blinkRange"], shadow.BlinkRange, 0.0001f);
            Assert.AreEqual((float)j["minBlinkDistance"], shadow.MinBlinkDistance, 0.0001f);
            Assert.AreEqual((float)j["arriveOffset"], shadow.ArriveOffset, 0.0001f);
            Assert.AreEqual((float)j["invulnerableSec"], shadow.InvulnerableSec, 0.0001f);
            Assert.AreEqual((float)j["killRefundFraction"], shadow.KillRefundFraction, 0.0001f);
            Assert.AreEqual((string)j["displayName"], shadow.DisplayName);

            BladeWhirlSkillData whirl = AssetDatabase.LoadAssetAtPath<BladeWhirlSkillData>(SkillDir + "HeroSkill_BladeWhirl.asset");
            j = SkillJson("HeroSkill_BladeWhirl");
            Assert.IsNotNull(whirl);
            Assert.AreEqual((int)j["damagePerTick"], whirl.DamagePerTick);
            Assert.AreEqual((int)j["tickCount"], whirl.TickCount);
            Assert.AreEqual((float)j["tickIntervalSec"], whirl.TickIntervalSec, 0.0001f);
            Assert.AreEqual((float)j["radius"], whirl.Radius, 0.0001f);
            Assert.AreEqual((float)j["cooldown"], whirl.Cooldown, 0.0001f);
            Assert.AreEqual((float)j["damageTakenMul"], whirl.DamageTakenMul, 0.0001f);
            Assert.AreEqual((int)j["triggerMonsterCount"], whirl.TriggerMonsterCount);
            Assert.AreEqual((float)j["triggerRadius"], whirl.TriggerRadius, 0.0001f);
            Assert.AreEqual((string)j["displayName"], whirl.DisplayName);

            SmokeBombSkillData smoke = AssetDatabase.LoadAssetAtPath<SmokeBombSkillData>(SkillDir + "HeroSkill_SmokeBomb.asset");
            j = SkillJson("HeroSkill_SmokeBomb");
            Assert.IsNotNull(smoke);
            Assert.AreEqual((float)j["radius"], smoke.Radius, 0.0001f);
            Assert.AreEqual((float)j["durationSec"], smoke.DurationSec, 0.0001f);
            Assert.AreEqual((float)j["monsterSpeedMul"], smoke.MonsterSpeedMul, 0.0001f);
            Assert.AreEqual((float)j["monsterRangeMul"], smoke.MonsterRangeMul, 0.0001f);
            Assert.AreEqual((float)j["monsterMinRange"], smoke.MonsterMinRange, 0.0001f);
            Assert.AreEqual((float)j["cooldown"], smoke.Cooldown, 0.0001f);
            Assert.AreEqual((int)j["triggerMonsterCount"], smoke.TriggerMonsterCount);
            Assert.AreEqual((float)j["triggerRadius"], smoke.TriggerRadius, 0.0001f);
            Assert.AreEqual((string)j["displayName"], smoke.DisplayName);
        }

        [Test]
        public void 암살자_로드아웃_asset은_85_65_45_스킬_순서이고_JSON과_일치한다()
        {
            HeroSkillLoadout asset = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(SkillDir + "HeroSkillLoadout_Assassin.asset");
            Assert.IsNotNull(asset);

            List<JToken> json = new List<JToken>();
            foreach (JToken token in (JArray)JObject.Parse(File.ReadAllText(JsonPath))["loadout"])
            {
                if ((string)token["heroId"] == "Assassin")
                {
                    json.Add(token);
                }
            }

            string[] skills = { "HeroSkill_ShadowStrike", "HeroSkill_BladeWhirl", "HeroSkill_SmokeBomb" };
            float[] gates = { 0.85f, 0.65f, 0.45f };
            Assert.AreEqual(3, asset.Phases.Count);
            Assert.AreEqual(3, json.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(gates[i], asset.Phases[i].HpFraction, 0.0001f);
                Assert.AreEqual(skills[i], asset.Phases[i].Skill.name);
                Assert.AreEqual(gates[i], (float)json[i]["hpFraction"], 0.0001f);
                Assert.AreEqual(skills[i], (string)json[i]["skill"]);
            }
        }

        [Test]
        public void 암살자_enum_키가_정의돼_있다()
        {
            foreach (string name in new[] { "AssassinSlashFx", "HeroSlipFx", "HeroShadowStrikeFx", "HeroBladeWhirlFx", "HeroSmokeBombFx" })
            {
                Assert.IsTrue(System.Enum.IsDefined(typeof(EVisual), name), name);
            }
            Assert.IsTrue(System.Enum.IsDefined(typeof(EHero), "Assassin"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EHeroAi), "Skirmisher"));
            Assert.IsTrue(System.Enum.IsDefined(typeof(EData), "HeroSkillLoadout_Assassin"));
        }
    }
}
