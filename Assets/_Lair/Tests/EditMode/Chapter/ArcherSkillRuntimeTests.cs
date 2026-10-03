using System.Text.RegularExpressions;
using Lair.Character;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# 궁수 스킬 3종 런타임 본격 검증 — 쿨다운 경계, 인자 전달, 보류/재시도, 연타 타이밍, 트리거 검사 주기, 중복 로그 방지 (chapter-system §5.4).
    //# FX 에셋 부재 LogError(키별 1회 정적 캐시)는 ArcherFxLogGuard 로 격리한다.
    public class ArcherSkillRuntimeTests : ChapterStaticStateTestBase
    {
        private RecordingArcherContext _ctx;
        private PiercingShotSkillData _pierce;
        private ArrowVolleySkillData _volley;
        private EvadeSkillData _evade;

        [SetUp]
        public void 스킬_데이터와_컨텍스트를_만든다()
        {
            ArcherFxLogGuard.Suppress();
            _ctx = new RecordingArcherContext();
            _pierce = ScriptableObject.CreateInstance<PiercingShotSkillData>();
            _volley = ScriptableObject.CreateInstance<ArrowVolleySkillData>();
            _evade = ScriptableObject.CreateInstance<EvadeSkillData>();
        }

        [TearDown]
        public void 스킬_데이터를_정리한다()
        {
            Object.DestroyImmediate(_pierce);
            Object.DestroyImmediate(_volley);
            Object.DestroyImmediate(_evade);
            ArcherFxLogGuard.Restore();
        }

        //# ===== 기본 수치 (기획서 §5.4 표) =====

        [Test]
        public void 관통_사격_기본_수치는_70_4_9_0_6이다()
        {
            Assert.AreEqual(70, _pierce.Damage);
            Assert.AreEqual(4f, _pierce.Cooldown, 0.0001f);
            Assert.AreEqual(9f, _pierce.LineLength, 0.0001f);
            Assert.AreEqual(0.6f, _pierce.LineHalfWidth, 0.0001f);
            Assert.AreEqual(0.5f, _pierce.KnockbackStrength, 0.0001f);
            Assert.AreEqual(8f, _pierce.CentroidRadius, 0.0001f);
        }

        [Test]
        public void 화살_소나기_기본_수치는_40x3_0_5초_반경_2_8_쿨_8이다()
        {
            Assert.AreEqual(40, _volley.DamagePerTick);
            Assert.AreEqual(3, _volley.TickCount);
            Assert.AreEqual(0.5f, _volley.TickIntervalSec, 0.0001f);
            Assert.AreEqual(2.8f, _volley.Radius, 0.0001f);
            Assert.AreEqual(8f, _volley.Cooldown, 0.0001f);
            Assert.AreEqual(7f, _volley.TargetSearchRadius, 0.0001f);
        }

        [Test]
        public void 회피_도약_기본_수치는_30_쿨_10_거리_4_5_0_25초_반경_2_트리거_3마리_반경_2다()
        {
            Assert.AreEqual(30, _evade.Damage);
            Assert.AreEqual(10f, _evade.Cooldown, 0.0001f);
            Assert.AreEqual(4.5f, _evade.LeapDistance, 0.0001f);
            Assert.AreEqual(0.25f, _evade.LeapDurationSec, 0.0001f);
            Assert.AreEqual(2f, _evade.BlastRadius, 0.0001f);
            Assert.AreEqual(2.5f, _evade.KnockbackStrength, 0.0001f);
            Assert.AreEqual(3, _evade.TriggerMonsterCount);
            Assert.AreEqual(2f, _evade.TriggerRadius, 0.0001f);
            Assert.AreEqual(0.25f, EvadeRuntime.CheckIntervalSec, 0.0001f);
        }

        //# ===== 관통 사격 =====

        [Test]
        public void 관통_사격은_첫_쿨다운이_끝나는_틱에_발동한다()
        {
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            runtime.Tick(_ctx, 3.98f);
            Assert.AreEqual(0, _ctx.Lines.Count);
            runtime.Tick(_ctx, 0.02f);
            Assert.AreEqual(1, _ctx.Lines.Count);
        }

        [Test]
        public void 관통_사격은_데이터_수치를_그대로_직선_타격에_전달한다()
        {
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            runtime.Tick(_ctx, 4f);

            LineCall call = _ctx.Lines[0];
            Assert.AreEqual(70, call.Amount);
            Assert.AreEqual(9f, call.Length, 0.0001f);
            Assert.AreEqual(0.6f, call.HalfWidth, 0.0001f);
            Assert.AreEqual(0.5f, call.Knockback, 0.0001f);
            Assert.AreEqual(8f, _ctx.CentroidRadii[0], 0.0001f, "방향 결정용 무게중심 반경");
        }

        [Test]
        public void 관통_사격_방향은_영웅에서_무게중심까지의_정규화_평면_방향이다()
        {
            _ctx.HeroPosition = new Vector3(2f, 0f, 2f);
            _ctx.Centroid = new Vector3(2f, 7f, 8f);
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            runtime.Tick(_ctx, 4f);

            Vector3 dir = _ctx.Lines[0].Direction;
            Assert.AreEqual(0f, dir.x, 0.001f);
            Assert.AreEqual(0f, dir.y, 0.0001f, "높이 차는 무시");
            Assert.AreEqual(1f, dir.z, 0.001f);
        }

        [Test]
        public void 관통_사격_대각_방향도_단위벡터다()
        {
            _ctx.Centroid = new Vector3(3f, 0f, 4f);
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            runtime.Tick(_ctx, 4f);

            Assert.AreEqual(1f, _ctx.Lines[0].Direction.magnitude, 0.001f);
            Assert.AreEqual(0.6f, _ctx.Lines[0].Direction.x, 0.001f);
        }

        [Test]
        public void 관통_사격은_발동_후_쿨다운이_전체로_리셋된다()
        {
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();
            runtime.Tick(_ctx, 4f);

            runtime.Tick(_ctx, 3.5f);
            Assert.AreEqual(1, _ctx.Lines.Count);
            runtime.Tick(_ctx, 0.6f);
            Assert.AreEqual(2, _ctx.Lines.Count);
        }

        [Test]
        public void 관통_사격은_몬스터가_없는_동안_매_틱_재시도하고_나타나는_틱에_발동하며_쿨다운은_전체로_시작한다()
        {
            _ctx.Centroid = _ctx.HeroPosition;
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            runtime.Tick(_ctx, 10f);
            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(0, _ctx.Lines.Count);

            _ctx.Centroid = new Vector3(0f, 0f, 3f);
            runtime.Tick(_ctx, 0.016f);
            Assert.AreEqual(1, _ctx.Lines.Count, "대기 중이던 발동이 즉시");

            runtime.Tick(_ctx, 3.9f);
            Assert.AreEqual(1, _ctx.Lines.Count, "초과분 이월 없이 전체 4초 쿨다운");
        }

        [Test]
        public void 관통_사격은_궁수_컨텍스트가_아니면_오류를_한번만_남기고_발동하지_않는다()
        {
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();
            LogAssert.Expect(LogType.Error, new Regex("PiercingShotRuntime.*IArcherSkillContext"));

            runtime.Tick(new PlainHeroSkillContext(), 5f);
            runtime.Tick(new PlainHeroSkillContext(), 5f);
            runtime.Tick(new PlainHeroSkillContext(), 5f);

            Assert.AreEqual(0, _ctx.Lines.Count);
        }

        [Test]
        public void 관통_사격_OnDeactivate는_예외없이_호출된다()
        {
            IHeroSkillRuntime runtime = _pierce.CreateRuntime();

            Assert.DoesNotThrow(() => runtime.OnDeactivate());
        }

        [Test]
        public void 관통_사격_런타임_인스턴스는_서로_쿨다운_상태를_공유하지_않는다()
        {
            IHeroSkillRuntime first = _pierce.CreateRuntime();
            IHeroSkillRuntime second = _pierce.CreateRuntime();

            first.Tick(_ctx, 4f);
            second.Tick(_ctx, 1f);

            Assert.AreEqual(1, _ctx.Lines.Count);
        }

        //# ===== 화살 소나기 =====

        [Test]
        public void 소나기는_첫_쿨다운이_끝나는_틱에_발동하고_즉시_첫_타격한다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();

            runtime.Tick(_ctx, 7.98f);
            Assert.AreEqual(0, _ctx.Spheres.Count);
            runtime.Tick(_ctx, 0.02f);
            Assert.AreEqual(1, _ctx.Spheres.Count);
        }

        [Test]
        public void 소나기는_탐색_반경과_타격_수치를_정확히_전달한다()
        {
            _ctx.DensePoint = new Vector3(3f, 0f, 1f);
            IHeroSkillRuntime runtime = _volley.CreateRuntime();

            runtime.Tick(_ctx, 8f);

            Assert.AreEqual(7f, _ctx.DenseSearches[0].Key, 0.0001f, "탐색 반경");
            Assert.AreEqual(2.8f, _ctx.DenseSearches[0].Value, 0.0001f, "군집 반경");
            SphereCall call = _ctx.Spheres[0];
            Assert.AreEqual(40, call.Amount);
            Assert.AreEqual(2.8f, call.Radius, 0.0001f);
            Assert.AreEqual(0f, call.Knockback, 0.0001f);
            Assert.AreEqual(1, call.Centers.Count);
            Assert.AreEqual(new Vector3(3f, 0f, 1f), call.Centers[0]);
        }

        [Test]
        public void 소나기는_3타를_0_5초_간격으로_치고_이후_추가_타격이_없다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);

            runtime.Tick(_ctx, 0.25f);
            Assert.AreEqual(1, _ctx.Spheres.Count, "간격 0.5 전");
            runtime.Tick(_ctx, 0.25f);
            Assert.AreEqual(2, _ctx.Spheres.Count);
            runtime.Tick(_ctx, 0.5f);
            Assert.AreEqual(3, _ctx.Spheres.Count);
            runtime.Tick(_ctx, 1f);
            Assert.AreEqual(3, _ctx.Spheres.Count);
        }

        [Test]
        public void 소나기는_큰_dt_한번에도_남은_타를_모두_치되_총_3타를_넘지_않는다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);

            runtime.Tick(_ctx, 5f);

            Assert.AreEqual(3, _ctx.Spheres.Count);
        }

        [Test]
        public void 소나기의_낙하_지점은_발동_시점에_고정된다()
        {
            _ctx.DensePoint = new Vector3(3f, 0f, 1f);
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);

            _ctx.DensePoint = new Vector3(-9f, 0f, -9f);
            runtime.Tick(_ctx, 0.5f);
            runtime.Tick(_ctx, 0.5f);

            foreach (SphereCall call in _ctx.Spheres)
            {
                Assert.AreEqual(new Vector3(3f, 0f, 1f), call.Centers[0]);
            }
        }

        [Test]
        public void 소나기는_연타_중에는_새_탐색을_하지_않는다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);

            runtime.Tick(_ctx, 0.5f);
            runtime.Tick(_ctx, 0.5f);

            Assert.AreEqual(1, _ctx.DenseSearches.Count);
        }

        [Test]
        public void 소나기_쿨다운은_발동_시점부터_흘러_8초_뒤_다음_발동이_가능하다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);
            runtime.Tick(_ctx, 0.5f);
            runtime.Tick(_ctx, 0.5f);
            Assert.AreEqual(3, _ctx.Spheres.Count);

            runtime.Tick(_ctx, 6.9f);
            Assert.AreEqual(3, _ctx.Spheres.Count, "발동 후 7.9초");
            runtime.Tick(_ctx, 0.2f);
            Assert.AreEqual(4, _ctx.Spheres.Count, "발동 후 8.1초 — 두 번째 발동 첫 타");
        }

        [Test]
        public void 소나기는_몬스터가_없으면_발동을_보류하고_쿨다운을_소모하지_않는다()
        {
            _ctx.HasDensePoint = false;
            IHeroSkillRuntime runtime = _volley.CreateRuntime();

            runtime.Tick(_ctx, 20f);
            Assert.AreEqual(0, _ctx.Spheres.Count);

            _ctx.HasDensePoint = true;
            runtime.Tick(_ctx, 0.016f);
            Assert.AreEqual(1, _ctx.Spheres.Count);
        }

        [Test]
        public void 소나기_OnDeactivate는_진행_중_연타를_취소한다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            runtime.Tick(_ctx, 8f);

            runtime.OnDeactivate();
            runtime.Tick(_ctx, 1f);

            Assert.AreEqual(1, _ctx.Spheres.Count);
        }

        [Test]
        public void 소나기는_궁수_컨텍스트가_아니면_오류를_한번만_남기고_타격하지_않는다()
        {
            IHeroSkillRuntime runtime = _volley.CreateRuntime();
            LogAssert.Expect(LogType.Error, new Regex("ArrowVolleyRuntime.*IArcherSkillContext"));

            runtime.Tick(new PlainHeroSkillContext(), 9f);
            runtime.Tick(new PlainHeroSkillContext(), 9f);

            Assert.AreEqual(0, _ctx.Spheres.Count);
        }

        //# ===== 회피 도약 =====

        [Test]
        public void 도약은_쿨다운_10초_전에는_몬스터가_많아도_발동하지_않는다()
        {
            _ctx.NearbyCount = 8;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 9.9f);

            Assert.AreEqual(0, _ctx.Leaps.Count);
            Assert.AreEqual(0, _ctx.Rings.Count);
        }

        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(7, true)]
        public void 도약_트리거는_반경_안_몬스터가_3마리_이상일_때다(int nearby, bool leaps)
        {
            _ctx.NearbyCount = nearby;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(leaps ? 1 : 0, _ctx.Leaps.Count);
        }

        [Test]
        public void 도약은_트리거_반경과_폭발_수치를_정확히_전달한다()
        {
            _ctx.NearbyCount = 3;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(2f, _ctx.CountRadii[0], 0.0001f);
            Assert.AreEqual(2f, _ctx.CentroidRadii[0], 0.0001f);
            RingCall ring = _ctx.Rings[0];
            Assert.AreEqual(0f, ring.Inner, 0.0001f);
            Assert.AreEqual(2f, ring.Outer, 0.0001f);
            Assert.AreEqual(30, ring.Amount);
            Assert.AreEqual(2.5f, ring.Knockback, 0.0001f);
            Assert.AreEqual(0.25f, _ctx.Leaps[0].Duration, 0.0001f);
        }

        [Test]
        public void 도약_목적지는_위협_반대로_4_5_거리이고_아레나_안이면_클램프되지_않는다()
        {
            _ctx.HeroPosition = Vector3.zero;
            _ctx.Centroid = new Vector3(1f, 0f, 0f);
            _ctx.NearbyCount = 3;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(-4.5f, _ctx.Leaps[0].Destination.x, 0.001f);
            Assert.AreEqual(0f, _ctx.Leaps[0].Destination.z, 0.001f);
        }

        [Test]
        public void 도약_목적지는_아레나_밖이면_소프트_반경으로_클램프된다_E_9()
        {
            //# 영웅 (4,0,0), 위협 (3,0,0) -> 도약 +X, 4+4.5=8.5 -> 7. 실이동 3 >= leapDistance 의 절반(2.25) 이라 재계산 없이 클램프 결과 사용.
            _ctx.HeroPosition = new Vector3(4f, 0f, 0f);
            _ctx.Centroid = new Vector3(3f, 0f, 0f);
            _ctx.NearbyCount = 3;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(7f, _ctx.Leaps[0].Destination.x, 0.001f, "4+4.5=8.5 → 7");
        }

        [Test]
        public void 가장자리에서_바깥으로_도약하면_클램프_후_실이동이_부족해_원점_쪽으로_재계산한다()
        {
            //# 영웅 (6,0,0), 위협 (5,0,0): 클램프 결과 7 은 이동 1 < 2.25 -> 원점 쪽(-X) 으로 재계산 -> 6-4.5 = 1.5.
            _ctx.HeroPosition = new Vector3(6f, 0f, 0f);
            _ctx.Centroid = new Vector3(5f, 0f, 0f);
            _ctx.NearbyCount = 3;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(1.5f, _ctx.Leaps[0].Destination.x, 0.001f);
            Assert.AreEqual(0f, _ctx.Leaps[0].Destination.z, 0.001f);
        }

        //# --- ComputeLeapDestination (M1) ---

        private static Vector3 Clamp7(Vector3 position)
        {
            return KiteBehavior.ClampToRadius(position, 7f);
        }

        [Test]
        public void 최소_도약_비율_상수는_0_5다()
        {
            Assert.AreEqual(0.5f, EvadeRuntime.MinLeapFraction, 0.0001f);
        }

        [Test]
        public void 도약_목적지는_충분히_이동하면_클램프_결과를_그대로_쓴다()
        {
            Vector3 destination = EvadeRuntime.ComputeLeapDestination(Vector3.zero, new Vector3(1f, 0f, 0f), 4.5f, Clamp7);

            Assert.AreEqual(-4.5f, destination.x, 0.001f);
        }

        [TestCase(6f, 5f, 1.5f)]
        [TestCase(7f, 6f, 2.5f)]
        [TestCase(6.5f, 5.5f, 2f)]
        public void 도약_목적지는_가장자리에서_바깥_도약이면_원점_쪽으로_재계산한다(float heroX, float threatX, float expectedX)
        {
            Vector3 destination = EvadeRuntime.ComputeLeapDestination(new Vector3(heroX, 0f, 0f), new Vector3(threatX, 0f, 0f), 4.5f, Clamp7);

            Assert.AreEqual(expectedX, destination.x, 0.001f);
        }

        [Test]
        public void 도약_목적지의_실이동_경계는_leapDistance의_절반_이상이다()
        {
            //# 영웅 (5.25,0,0), 위협 (4.25,0,0): 클램프 7 -> 이동 1.75 < 2.25 -> 재계산. 영웅 (4.75,0,0): 7 -> 이동 2.25 = 경계(이상) -> 유지.
            Vector3 recalculated = EvadeRuntime.ComputeLeapDestination(new Vector3(5.25f, 0f, 0f), new Vector3(4.25f, 0f, 0f), 4.5f, Clamp7);
            Vector3 kept = EvadeRuntime.ComputeLeapDestination(new Vector3(4.75f, 0f, 0f), new Vector3(3.75f, 0f, 0f), 4.5f, Clamp7);

            Assert.AreEqual(0.75f, recalculated.x, 0.001f, "5.25-4.5");
            Assert.AreEqual(7f, kept.x, 0.001f);
        }

        [Test]
        public void 도약_목적지는_옆으로_도약해도_전장_안이고_충분히_이동한다()
        {
            Vector3 origin = new Vector3(7f, 0f, 0f);

            Vector3 destination = EvadeRuntime.ComputeLeapDestination(origin, new Vector3(7f, 0f, -2f), 4.5f, Clamp7);

            Assert.LessOrEqual(new Vector2(destination.x, destination.z).magnitude, 7.001f);
            Assert.GreaterOrEqual(Vector3.Distance(destination, origin), 4.5f * EvadeRuntime.MinLeapFraction);
        }

        [Test]
        public void 도약_목적지는_원점에_있으면_재계산하지_않고_클램프_결과를_돌려준다()
        {
            //# 어디로 가도 제자리인 클램프(전장 이탈 불가 상황)에서 원점에 있으면 원점 방향이 없어 그대로.
            Vector3 destination = EvadeRuntime.ComputeLeapDestination(Vector3.zero, new Vector3(1f, 0f, 0f), 4.5f, position => Vector3.zero);

            Assert.AreEqual(Vector3.zero, destination);
        }

        [Test]
        public void 재계산_목적지가_더_멀지_않으면_원래_클램프_결과를_유지한다()
        {
            Vector3 origin = new Vector3(6f, 0f, 0f);

            Vector3 destination = EvadeRuntime.ComputeLeapDestination(origin, new Vector3(5f, 0f, 0f), 4.5f, position => origin);

            Assert.AreEqual(origin, destination, "두 후보 모두 제자리 -> 원래 결과");
        }

        [Test]
        public void 도약_목적지는_전장_안_어느_위치에서도_전장_밖으로_나가지_않는다()
        {
            for (float x = -7f; x <= 7f; x += 1.75f)
            {
                for (float z = -7f; z <= 7f; z += 1.75f)
                {
                    Vector3 origin = new Vector3(x, 0f, z);
                    if (new Vector2(x, z).magnitude > 7f)
                        continue;

                    Vector3 destination = EvadeRuntime.ComputeLeapDestination(origin, origin + new Vector3(1f, 0f, 0.5f), 4.5f, Clamp7);

                    Assert.LessOrEqual(new Vector2(destination.x, destination.z).magnitude, 7.001f, $"({x},{z})");
                }
            }
        }

        [Test]
        public void 도약은_폭발이_도약보다_먼저_출발_지점_기준으로_일어난다()
        {
            _ctx.NearbyCount = 3;
            _ctx.HeroPosition = new Vector3(1f, 0f, 1f);
            IHeroSkillRuntime runtime = _evade.CreateRuntime();

            runtime.Tick(_ctx, 10f);

            Assert.AreEqual(1, _ctx.Rings.Count);
            Assert.AreEqual(1, _ctx.Leaps.Count);
        }

        [Test]
        public void 도약_트리거_검사는_0_25초_간격이라_그_사이_틱에서는_발동하지_않는다()
        {
            _ctx.NearbyCount = 0;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();
            runtime.Tick(_ctx, 10f);
            _ctx.NearbyCount = 3;

            runtime.Tick(_ctx, 0.1f);
            Assert.AreEqual(0, _ctx.Leaps.Count, "검사 후 0.1초");
            runtime.Tick(_ctx, 0.2f);
            Assert.AreEqual(1, _ctx.Leaps.Count, "누적 0.3초 ≥ 0.25 → 검사");
        }

        [Test]
        public void 도약_발동_후_쿨다운은_10초로_리셋된다()
        {
            _ctx.NearbyCount = 3;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();
            runtime.Tick(_ctx, 10f);

            runtime.Tick(_ctx, 9.7f);
            Assert.AreEqual(1, _ctx.Leaps.Count);
            runtime.Tick(_ctx, 0.4f);
            Assert.AreEqual(2, _ctx.Leaps.Count);
        }

        [Test]
        public void 도약은_트리거_미달이면_쿨다운을_소모하지_않고_충족되는_첫_검사에서_발동한다()
        {
            _ctx.NearbyCount = 1;
            IHeroSkillRuntime runtime = _evade.CreateRuntime();
            runtime.Tick(_ctx, 30f);
            Assert.AreEqual(0, _ctx.Leaps.Count);

            _ctx.NearbyCount = 4;
            runtime.Tick(_ctx, 0.25f);

            Assert.AreEqual(1, _ctx.Leaps.Count);
        }

        [Test]
        public void 도약은_궁수_컨텍스트가_아니면_오류를_한번만_남기고_발동하지_않는다()
        {
            IHeroSkillRuntime runtime = _evade.CreateRuntime();
            LogAssert.Expect(LogType.Error, new Regex("EvadeRuntime.*IArcherSkillContext"));

            runtime.Tick(new PlainHeroSkillContext(), 10f);
            runtime.Tick(new PlainHeroSkillContext(), 1f);
            runtime.Tick(new PlainHeroSkillContext(), 1f);

            Assert.AreEqual(0, _ctx.Leaps.Count);
        }

        //# --- ComputeLeapDirection ---

        [Test]
        public void 도약_방향은_위협_중심의_반대_평면_단위벡터다()
        {
            Vector3 dir = EvadeRuntime.ComputeLeapDirection(new Vector3(1f, 5f, 1f), new Vector3(4f, -2f, 5f));

            Assert.AreEqual(-0.6f, dir.x, 0.001f);
            Assert.AreEqual(0f, dir.y, 0.0001f);
            Assert.AreEqual(-0.8f, dir.z, 0.001f);
        }

        [Test]
        public void 도약_방향은_중심이_영웅과_같으면_원점_쪽이고_원점에서는_뒤쪽이다()
        {
            Vector3 towardOrigin = EvadeRuntime.ComputeLeapDirection(new Vector3(0f, 0f, 3f), new Vector3(0f, 0f, 3f));
            Vector3 atOrigin = EvadeRuntime.ComputeLeapDirection(Vector3.zero, Vector3.zero);

            Assert.AreEqual(-1f, towardOrigin.z, 0.001f);
            Assert.AreEqual(Vector3.back, atOrigin);
        }
    }
}
