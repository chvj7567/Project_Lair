using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 암살자 스킬 3종 런타임 본격 검증 - 기본 수치, 쿨다운/검사 주기 경계, 인자 전달, 환급, 배율 원복, 연막 지대 수명, 시전 우선순위 (chapter-3-hero §4).
    //# 스모크(AssassinTests)와 겹치지 않는 경계·정지·중복 해제 위주. FX 에셋 부재 LogError 는 ArcherFxLogGuard 로 격리한다.
    public class AssassinSkillRuntimeTests : ChapterStaticStateTestBase
    {
        private RecordingAssassinContext _ctx;
        private ShadowStrikeSkillData _shadow;
        private BladeWhirlSkillData _whirl;
        private SmokeBombSkillData _smoke;
        private readonly List<GameObject> _spawned = new List<GameObject>();

        [SetUp]
        public void 스킬_데이터와_컨텍스트를_만든다()
        {
            ArcherFxLogGuard.Suppress();
            _ctx = new RecordingAssassinContext();
            _shadow = ScriptableObject.CreateInstance<ShadowStrikeSkillData>();
            _whirl = ScriptableObject.CreateInstance<BladeWhirlSkillData>();
            _smoke = ScriptableObject.CreateInstance<SmokeBombSkillData>();
        }

        [TearDown]
        public void 스킬_데이터를_정리한다()
        {
            Object.DestroyImmediate(_shadow);
            Object.DestroyImmediate(_whirl);
            Object.DestroyImmediate(_smoke);
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

        //# ===== 기본 수치 (기획서 §4) =====

        [Test]
        public void 그림자_습격_기본_수치는_120_쿨_6_사거리_9_최소_2_5_무적_0_3_환급_절반이다()
        {
            Assert.AreEqual(120, _shadow.Damage);
            Assert.AreEqual(6f, _shadow.Cooldown, 0.0001f);
            Assert.AreEqual(9f, _shadow.BlinkRange, 0.0001f);
            Assert.AreEqual(2.5f, _shadow.MinBlinkDistance, 0.0001f);
            Assert.AreEqual(1.2f, _shadow.ArriveOffset, 0.0001f);
            Assert.AreEqual(0.3f, _shadow.InvulnerableSec, 0.0001f);
            Assert.AreEqual(0.5f, _shadow.KillRefundFraction, 0.0001f);
        }

        [Test]
        public void 칼날_선풍_기본_수치는_35x4_0_3초_반경_2_4_쿨_9_배율_0_7_트리거_2마리다()
        {
            Assert.AreEqual(35, _whirl.DamagePerTick);
            Assert.AreEqual(4, _whirl.TickCount);
            Assert.AreEqual(0.3f, _whirl.TickIntervalSec, 0.0001f);
            Assert.AreEqual(2.4f, _whirl.Radius, 0.0001f);
            Assert.AreEqual(9f, _whirl.Cooldown, 0.0001f);
            Assert.AreEqual(0.7f, _whirl.DamageTakenMul, 0.0001f);
            Assert.AreEqual(2, _whirl.TriggerMonsterCount);
            Assert.AreEqual(2.4f, _whirl.TriggerRadius, 0.0001f);
        }

        [Test]
        public void 연막_탄_기본_수치는_반경_3_3초_속도_0_6_사거리_0_5_하한_0_8_쿨_12_트리거_4마리다()
        {
            Assert.AreEqual(3f, _smoke.Radius, 0.0001f);
            Assert.AreEqual(3f, _smoke.DurationSec, 0.0001f);
            Assert.AreEqual(0.6f, _smoke.MonsterSpeedMul, 0.0001f);
            Assert.AreEqual(0.5f, _smoke.MonsterRangeMul, 0.0001f);
            Assert.AreEqual(0.8f, _smoke.MonsterMinRange, 0.0001f);
            Assert.AreEqual(12f, _smoke.Cooldown, 0.0001f);
            Assert.AreEqual(4, _smoke.TriggerMonsterCount);
            Assert.AreEqual(2.4f, _smoke.TriggerRadius, 0.0001f);
        }

        [Test]
        public void 트리거_검사_주기는_세_스킬_모두_0_25초다()
        {
            Assert.AreEqual(0.25f, ShadowStrikeRuntime.CheckIntervalSec, 0.0001f);
            Assert.AreEqual(0.25f, BladeWhirlRuntime.CheckIntervalSec, 0.0001f);
            Assert.AreEqual(0.25f, SmokeBombRuntime.CheckIntervalSec, 0.0001f);
        }

        [Test]
        public void 시전_우선순위는_연막_2_습격_1_선풍_0이고_기존_스킬은_0이다()
        {
            Assert.AreEqual(2, _smoke.CastPriority);
            Assert.AreEqual(1, _shadow.CastPriority);
            Assert.AreEqual(0, _whirl.CastPriority);
            foreach (HeroSkillData legacy in new HeroSkillData[]
                     {
                         ScriptableObject.CreateInstance<DashStrikeSkillData>(),
                         ScriptableObject.CreateInstance<AoeNovaSkillData>(),
                         ScriptableObject.CreateInstance<OrbitingBladeSkillData>(),
                         ScriptableObject.CreateInstance<PiercingShotSkillData>(),
                         ScriptableObject.CreateInstance<ArrowVolleySkillData>(),
                         ScriptableObject.CreateInstance<EvadeSkillData>(),
                     })
            {
                Assert.AreEqual(0, legacy.CastPriority, legacy.GetType().Name);
                Object.DestroyImmediate(legacy);
            }
        }

        //# ===== 그림자 습격 =====

        [Test]
        public void 그림자_습격은_첫_쿨다운_6초_전에는_위협이_있어도_시전하지_않는다()
        {
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();

            runtime.Tick(_ctx, 5.9f);

            Assert.AreEqual(0, _ctx.Teleports.Count);
            Assert.AreEqual(0, _ctx.TopThreatRanges.Count, "쿨다운 중엔 탐색도 하지 않는다");
        }

        [Test]
        public void 그림자_습격은_사거리_9로_탐색하고_표적_앞_1_2에_도착하며_120_피해와_0_3초_무적을_준다()
        {
            _ctx.HeroPosition = Vector3.zero;
            _ctx.ThreatPosition = new Vector3(6f, 0f, 0f);
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();

            runtime.Tick(_ctx, 6f);

            Assert.AreEqual(9f, _ctx.TopThreatRanges[0], 0.0001f);
            Assert.AreEqual(4.8f, _ctx.Teleports[0].x, 0.001f, "6 - 1.2");
            Assert.AreEqual(120, _ctx.MonsterDamage[0]);
            Assert.AreEqual(0.3f, _ctx.Invulns[0], 0.0001f);
        }

        [Test]
        public void 도착_지점이_전장_밖이면_순간이동_직전에_클램프된다()
        {
            _ctx.HeroPosition = new Vector3(-3f, 0f, 0f);
            _ctx.ThreatPosition = new Vector3(8.5f, 0f, 0f);
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();

            runtime.Tick(_ctx, 6f);

            Assert.AreEqual(7f, _ctx.Teleports[0].x, 0.001f, "8.5 - 1.2 = 7.3 -> 소프트 반경 7");
        }

        [TestCase(2.49f, false)]
        [TestCase(2.5f, true)]
        [TestCase(2.51f, true)]
        public void 표적_거리가_최소_순간이동_거리_2_5_이상일_때만_시전한다(float distance, bool casts)
        {
            _ctx.ThreatPosition = new Vector3(distance, 0f, 0f);
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();

            runtime.Tick(_ctx, 6f);

            Assert.AreEqual(casts ? 1 : 0, _ctx.Teleports.Count);
        }

        [Test]
        public void 거리_미달_보류는_쿨다운을_소모하지_않아_표적이_멀어지는_첫_검사에서_시전한다()
        {
            _ctx.ThreatPosition = new Vector3(1f, 0f, 0f);
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();
            runtime.Tick(_ctx, 6f);
            Assert.AreEqual(0, _ctx.Teleports.Count);

            _ctx.ThreatPosition = new Vector3(5f, 0f, 0f);
            runtime.Tick(_ctx, 0.25f);

            Assert.AreEqual(1, _ctx.Teleports.Count);
        }

        [Test]
        public void 표적이_없으면_보류하고_쿨다운은_그대로라_나타나는_즉시_시전한다()
        {
            _ctx.HasThreat = false;
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();
            runtime.Tick(_ctx, 30f);
            Assert.AreEqual(0, _ctx.Teleports.Count);

            _ctx.HasThreat = true;
            runtime.Tick(_ctx, 0.25f);

            Assert.AreEqual(1, _ctx.Teleports.Count);
        }

        [Test]
        public void 처치하지_못하면_쿨다운_6초_처치하면_3초로_다음_시전이_갈린다()
        {
            _ctx.KillOnDamage = false;
            IHeroSkillRuntime miss = _shadow.CreateRuntime();
            miss.Tick(_ctx, 6f);
            miss.Tick(_ctx, 5.5f);
            Assert.AreEqual(1, _ctx.Teleports.Count, "6초 쿨다운 중 5.5초");
            miss.Tick(_ctx, 0.5f);
            Assert.AreEqual(2, _ctx.Teleports.Count);

            RecordingAssassinContext killCtx = new RecordingAssassinContext { KillOnDamage = true };
            IHeroSkillRuntime kill = _shadow.CreateRuntime();
            kill.Tick(killCtx, 6f);
            kill.Tick(killCtx, 2.75f);
            Assert.AreEqual(1, killCtx.Teleports.Count, "환급 후 쿨다운 3초 중 2.75초");
            kill.Tick(killCtx, 0.25f);
            Assert.AreEqual(2, killCtx.Teleports.Count);
        }

        [TestCase(true, 0.5f, 3f)]
        [TestCase(false, 0.5f, 6f)]
        [TestCase(true, 0f, 6f)]
        [TestCase(true, 1f, 0f)]
        [TestCase(true, 2f, 0f)]
        [TestCase(true, -1f, 6f)]
        public void 시전_후_쿨다운_계산은_환급_비율을_0에서_1로_clamp한다(bool killed, float refund, float expected)
        {
            Assert.AreEqual(expected, ShadowStrikeRuntime.ComputeCooldownAfterCast(6f, killed, refund), 0.0001f);
        }

        [Test]
        public void 도착_지점은_표적에서_영웅_쪽으로_오프셋_앞이고_높이는_영웅_높이다()
        {
            Vector3 arrival = ShadowStrikeRuntime.ComputeArrival(new Vector3(10f, 2f, 0f), new Vector3(4f, 0f, 0f), 1.2f);

            Assert.AreEqual(5.2f, arrival.x, 0.001f);
            Assert.AreEqual(2f, arrival.y, 0.0001f);
        }

        [Test]
        public void 도착_계산에서_영웅과_표적이_겹치면_표적_위치다()
        {
            Vector3 arrival = ShadowStrikeRuntime.ComputeArrival(new Vector3(3f, 0f, 3f), new Vector3(3f, 0f, 3f), 1.2f);

            Assert.AreEqual(new Vector3(3f, 0f, 3f), arrival);
        }

        [Test]
        public void 도착_계산은_대각_방향에서도_오프셋_길이를_유지한다()
        {
            Vector3 target = new Vector3(3f, 0f, 4f);

            Vector3 arrival = ShadowStrikeRuntime.ComputeArrival(Vector3.zero, target, 1.2f);

            Assert.AreEqual(1.2f, Vector3.Distance(arrival, target), 0.001f);
        }

        [Test]
        public void 영웅이_정지_중이면_순간이동하지_않고_쿨다운도_소모되지_않는다()
        {
            _ctx.IsHeroActive = false;
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();
            runtime.Tick(_ctx, 20f);
            Assert.AreEqual(0, _ctx.Teleports.Count);
            Assert.AreEqual(0, _ctx.TopThreatRanges.Count);

            _ctx.IsHeroActive = true;
            runtime.Tick(_ctx, 0.25f);

            Assert.AreEqual(1, _ctx.Teleports.Count);
        }

        [Test]
        public void 검사_주기_0_25초_안에서는_쿨다운이_끝나도_시전하지_않는다()
        {
            _ctx.HasThreat = false;
            IHeroSkillRuntime runtime = _shadow.CreateRuntime();
            runtime.Tick(_ctx, 6f);
            _ctx.HasThreat = true;

            runtime.Tick(_ctx, 0.1f);
            Assert.AreEqual(0, _ctx.Teleports.Count);
            runtime.Tick(_ctx, 0.2f);
            Assert.AreEqual(1, _ctx.Teleports.Count);
        }

        //# ===== 칼날 선풍 =====

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, true)]
        [TestCase(5, true)]
        public void 칼날_선풍은_반경_안_몬스터가_2마리_이상일_때만_시전한다(int nearby, bool casts)
        {
            _ctx.Nearby = nearby;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();

            runtime.Tick(_ctx, 9f);

            Assert.AreEqual(casts ? 1 : 0, _ctx.Rings.Count > 0 ? 1 : 0);
            Assert.AreEqual(2.4f, _ctx.CountRadii[0], 0.0001f);
        }

        [Test]
        public void 칼날_선풍은_즉시_첫_타격하고_0_3초_간격으로_총_4타_링_반경_2_4_피해_35다()
        {
            _ctx.Nearby = 3;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();

            runtime.Tick(_ctx, 9f);
            Assert.AreEqual(1, _ctx.Rings.Count);
            Assert.AreEqual(0f, _ctx.Rings[0].Inner, 0.0001f);
            Assert.AreEqual(2.4f, _ctx.Rings[0].Outer, 0.0001f);
            Assert.AreEqual(35, _ctx.Rings[0].Amount);
            Assert.AreEqual(0f, _ctx.Rings[0].Knockback, 0.0001f);

            runtime.Tick(_ctx, 0.15f);
            Assert.AreEqual(1, _ctx.Rings.Count);
            runtime.Tick(_ctx, 0.15f);
            Assert.AreEqual(2, _ctx.Rings.Count);
            runtime.Tick(_ctx, 0.3f);
            runtime.Tick(_ctx, 0.3f);
            Assert.AreEqual(4, _ctx.Rings.Count);
            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(4, _ctx.Rings.Count, "4타 뒤 추가 타격 없음");
        }

        [Test]
        public void 칼날_선풍의_피해_배율은_시전에_0_7을_곱하고_마지막_타격에서_역수로_한번만_원복한다()
        {
            _ctx.Nearby = 3;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();

            runtime.Tick(_ctx, 9f);
            Assert.AreEqual(1, _ctx.TakenMuls.Count);
            Assert.AreEqual(0.7f, _ctx.TakenMuls[0], 0.0001f);

            runtime.Tick(_ctx, 0.3f);
            runtime.Tick(_ctx, 0.3f);
            Assert.AreEqual(1, _ctx.TakenMuls.Count, "진행 중에는 원복 없음");
            runtime.Tick(_ctx, 0.3f);
            Assert.AreEqual(2, _ctx.TakenMuls.Count);
            Assert.AreEqual(1f / 0.7f, _ctx.TakenMuls[1], 0.0001f);

            runtime.OnDeactivate();
            Assert.AreEqual(2, _ctx.TakenMuls.Count, "종료 후 비활성은 이중 원복하지 않는다");
        }

        [Test]
        public void 칼날_선풍은_회전_중_재시전하지_않고_쿨다운은_시전_시점부터_9초다()
        {
            _ctx.Nearby = 3;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();
            runtime.Tick(_ctx, 9f);

            runtime.Tick(_ctx, 1.0f);
            Assert.AreEqual(4, _ctx.Rings.Count, "회전 종료");
            runtime.Tick(_ctx, 7.9f);
            Assert.AreEqual(4, _ctx.Rings.Count, "시전 후 8.9초");
            runtime.Tick(_ctx, 0.2f);
            Assert.AreEqual(5, _ctx.Rings.Count, "시전 후 9.1초 - 재시전 첫 타");
        }

        [Test]
        public void 칼날_선풍은_영웅_정지_중_쿨다운도_진행하지_않는다()
        {
            _ctx.Nearby = 3;
            _ctx.IsHeroActive = false;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();
            runtime.Tick(_ctx, 100f);
            _ctx.IsHeroActive = true;

            runtime.Tick(_ctx, 8.7f);
            Assert.AreEqual(0, _ctx.Rings.Count);
            runtime.Tick(_ctx, 0.5f);
            Assert.AreEqual(1, _ctx.Rings.Count);
        }

        [Test]
        public void 칼날_선풍_진행_중_비활성이면_남은_타격을_취소하고_배율을_원복한다()
        {
            _ctx.Nearby = 3;
            IHeroSkillRuntime runtime = _whirl.CreateRuntime();
            runtime.Tick(_ctx, 9f);

            runtime.OnDeactivate();
            runtime.Tick(_ctx, 5f);

            Assert.AreEqual(1, _ctx.Rings.Count);
            Assert.AreEqual(2, _ctx.TakenMuls.Count);
        }

        //# ===== 연막 탄 =====

        [TestCase(3, false)]
        [TestCase(4, true)]
        [TestCase(9, true)]
        public void 연막_탄은_반경_안_몬스터가_4마리_이상일_때만_시전한다(int nearby, bool casts)
        {
            _ctx.Nearby = nearby;
            IHeroSkillRuntime runtime = _smoke.CreateRuntime();

            runtime.Tick(_ctx, 12f);

            Assert.AreEqual(casts ? 1 : 0, _ctx.Zones.Count);
            Assert.AreEqual(casts ? 1 : 0, _ctx.SlipRequests);
        }

        [Test]
        public void 연막_탄은_시전_위치에_고정된_지대를_수치대로_적용하고_슬립을_한번_요청한다()
        {
            _ctx.Nearby = 4;
            _ctx.HeroPosition = new Vector3(2f, 0f, 1f);
            IHeroSkillRuntime runtime = _smoke.CreateRuntime();

            runtime.Tick(_ctx, 12f);

            ZoneCall zone = _ctx.Zones[0];
            Assert.AreEqual(new Vector3(2f, 0f, 1f), zone.Center);
            Assert.AreEqual(3f, zone.Radius, 0.0001f);
            Assert.AreEqual(0.6f, zone.SpeedMul, 0.0001f);
            Assert.AreEqual(0.5f, zone.RangeMul, 0.0001f);
            Assert.AreEqual(0.8f, zone.MinRange, 0.0001f);
            Assert.AreEqual(1, _ctx.SlipRequests);
            Assert.AreEqual(2.4f, _ctx.CountRadii[0], 0.0001f);
        }

        [Test]
        public void 연막_지대는_매_틱_갱신되고_영웅이_이동해도_중심이_고정이며_3초_뒤_한번만_해제된다()
        {
            _ctx.Nearby = 4;
            IHeroSkillRuntime runtime = _smoke.CreateRuntime();
            runtime.Tick(_ctx, 12f);

            _ctx.HeroPosition = new Vector3(5f, 0f, 5f);
            runtime.Tick(_ctx, 1f);
            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(3, _ctx.Zones.Count, "시전 1 + 갱신 2");
            Assert.AreEqual(Vector3.zero, _ctx.Zones[2].Center);
            Assert.AreEqual(0, _ctx.ZoneClears);

            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(1, _ctx.ZoneClears);
            Assert.AreEqual(3, _ctx.Zones.Count, "해제 틱에는 갱신 호출 없음");

            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(1, _ctx.ZoneClears, "해제 후 중복 해제 없음");
        }

        [Test]
        public void 연막_탄의_쿨다운은_시전_시점부터_12초이고_지대_중에는_재시전하지_않는다()
        {
            _ctx.Nearby = 4;
            IHeroSkillRuntime runtime = _smoke.CreateRuntime();
            runtime.Tick(_ctx, 12f);

            runtime.Tick(_ctx, 3f);
            runtime.Tick(_ctx, 8.5f);
            Assert.AreEqual(1, _ctx.SlipRequests, "시전 후 11.5초");
            runtime.Tick(_ctx, 0.75f);
            Assert.AreEqual(2, _ctx.SlipRequests, "시전 후 12.25초");
        }

        [Test]
        public void 영웅이_정지_중이면_연막_탄을_시전하지_않지만_이미_깔린_지대는_계속_진행하고_끝난다()
        {
            _ctx.Nearby = 4;
            IHeroSkillRuntime runtime = _smoke.CreateRuntime();
            runtime.Tick(_ctx, 12f);

            _ctx.IsHeroActive = false;
            runtime.Tick(_ctx, 1f);
            runtime.Tick(_ctx, 1f);
            runtime.Tick(_ctx, 1f);

            Assert.AreEqual(1, _ctx.ZoneClears, "지대는 월드 효과라 영웅 정지와 무관하게 만료");

            RecordingAssassinContext frozen = new RecordingAssassinContext { Nearby = 4, IsHeroActive = false };
            IHeroSkillRuntime idle = _smoke.CreateRuntime();
            idle.Tick(frozen, 50f);
            Assert.AreEqual(0, frozen.Zones.Count);
        }

        [Test]
        public void 연막_탄_비활성은_지대가_있을_때만_한번_해제한다()
        {
            _ctx.Nearby = 4;
            IHeroSkillRuntime never = _smoke.CreateRuntime();
            never.OnDeactivate();
            Assert.AreEqual(0, _ctx.ZoneClears, "한 번도 시전하지 않으면 해제 호출 없음");

            IHeroSkillRuntime active = _smoke.CreateRuntime();
            active.Tick(_ctx, 12f);
            active.OnDeactivate();
            active.OnDeactivate();
            Assert.AreEqual(1, _ctx.ZoneClears);
        }

        //# ===== 컨텍스트 / 우선순위 =====

        [Test]
        public void 암살자_컨텍스트가_아니면_세_스킬_모두_오류를_한번씩만_남기고_동작하지_않는다()
        {
            LogAssert_ExpectError("ShadowStrikeRuntime");
            LogAssert_ExpectError("BladeWhirlRuntime");
            LogAssert_ExpectError("SmokeBombRuntime");
            IHeroSkillRuntime[] runtimes = { _shadow.CreateRuntime(), _whirl.CreateRuntime(), _smoke.CreateRuntime() };

            foreach (IHeroSkillRuntime runtime in runtimes)
            {
                runtime.Tick(new PlainHeroSkillContext(), 20f);
                runtime.Tick(new PlainHeroSkillContext(), 20f);
            }
        }

        private static void LogAssert_ExpectError(string runtimeName)
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Error, new System.Text.RegularExpressions.Regex(runtimeName + ".*IAssassinSkillContext"));
        }

        private static FieldInfo Field(object target, string name)
        {
            FieldInfo fi = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{name} 필드 존재 - 시그니처 변경 감지");
            return fi;
        }

        [Test]
        public void 러너는_동시_해금된_스킬을_우선순위_연막_습격_선풍_순으로_활성한다()
        {
            GameObject hero = new GameObject("RunnerHero");
            _spawned.Add(hero);
            HeroSkillRunner runner = hero.AddComponent<HeroSkillRunner>();
            Health health = hero.GetComponent<Health>();
            health.SetMax(100, true);
            InvokeNonPublic(runner, "Awake");
            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>("Assets/_Lair/Art/Skills/HeroSkillLoadout_Assassin.asset");
            Assert.IsNotNull(loadout);
            runner.Bind(loadout);
            List<string> unlocked = new List<string>();
            runner.OnSkillUnlocked += data => unlocked.Add(data.GetType().Name);
            health.TakeDamage(70);

            InvokeNonPublic(runner, "Update");

            CollectionAssert.AreEqual(new[] { "ShadowStrikeSkillData", "BladeWhirlSkillData", "SmokeBombSkillData" }, unlocked, "해금 통지는 페이즈 순서");
            IList<IHeroSkillRuntime> active = (IList<IHeroSkillRuntime>)Field(runner, "_active").GetValue(runner);
            Assert.AreEqual(3, active.Count);
            Assert.IsInstanceOf<SmokeBombRuntime>(active[0]);
            Assert.IsInstanceOf<ShadowStrikeRuntime>(active[1]);
            Assert.IsInstanceOf<BladeWhirlRuntime>(active[2]);
        }

        [Test]
        public void 러너는_우선순위가_모두_같은_기존_로드아웃에서_활성화_순서를_유지한다()
        {
            GameObject hero = new GameObject("RunnerKnight");
            _spawned.Add(hero);
            HeroSkillRunner runner = hero.AddComponent<HeroSkillRunner>();
            Health health = hero.GetComponent<Health>();
            health.SetMax(100, true);
            InvokeNonPublic(runner, "Awake");
            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>("Assets/_Lair/Art/Skills/HeroSkillLoadout.asset");
            runner.Bind(loadout);
            health.TakeDamage(70);

            InvokeNonPublic(runner, "Update");

            IList<IHeroSkillRuntime> active = (IList<IHeroSkillRuntime>)Field(runner, "_active").GetValue(runner);
            Assert.AreEqual(3, active.Count);
            for (int i = 0; i < 3; ++i)
            {
                Assert.AreEqual(loadout.Phases[i].Skill.CreateRuntime().GetType(), active[i].GetType(), $"phase {i}");
            }
        }

        private static void InvokeNonPublic(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 존재 - 시그니처 변경 감지");
            mi.Invoke(target, null);
        }
    }
}
