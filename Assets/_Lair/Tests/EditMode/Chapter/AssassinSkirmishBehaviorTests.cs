using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SkirmishBehavior 본격 검증 - 경계값, 슬립 조건 조합, 쿨다운/최소 유지/관찰 간격, 강제 슬립, 표적 점수·선정, 슬립 목적지 (chapter-3-hero §3.3).
    //# 스모크(AssassinTests)가 다루는 기본 전이는 중복하지 않는다. 경계는 이진 정확 값(0.5 배수)을 쓰는 커스텀 설정으로 고정한다.
    public class AssassinSkirmishBehaviorTests : ChapterStaticStateTestBase
    {
        //# 이진 정확 경계용: strikeWindow 2.0 / crowd 3 / cooldown 4.0 / minDwell 0.5.
        private static SkirmishSettings Exact()
        {
            return new SkirmishSettings(9f, 1.5f, 2.0f, 3, 2f, 3.5f, 0.25f, 0.25f, 4.0f, 0.5f, 7f);
        }

        //# 기획서 §2.2 Assassin 값.
        private static SkirmishSettings Shipped()
        {
            return new SkirmishSettings(9f, 1.5f, 2.8f, 3, 2f, 3.5f, 0.2f, 0.2f, 5f, 0.4f, 7f);
        }

        private static SkirmishInput Engaged(int crowd = 0, bool targetChanged = false)
        {
            return new SkirmishInput { Position = Vector3.zero, HasTarget = true, TargetChanged = targetChanged, TargetDist = 1f, Range = 1.6f, EngageBuffer = 0.5f, CrowdCount = crowd };
        }

        private static SkirmishInput Far(int crowd = 0)
        {
            return new SkirmishInput { Position = Vector3.zero, HasTarget = true, TargetDist = 5f, Range = 1.6f, EngageBuffer = 0.5f, CrowdCount = crowd };
        }

        private static SkirmishInput NoTarget(int crowd = 0)
        {
            return new SkirmishInput { Position = Vector3.zero, HasTarget = false, CrowdCount = crowd };
        }

        //# --- 상수 / 설정 ---

        [Test]
        public void 명세_고정_상수는_중심_집계_반경_3과_관찰_한계_1초다()
        {
            Assert.AreEqual(3f, SkirmishBehavior.CentroidRadius, 0.0001f);
            Assert.AreEqual(1f, SkirmishBehavior.ObservationGapSec, 0.0001f);
        }

        [Test]
        public void SkirmishSettings_From은_row의_11개_필드를_그대로_옮긴다()
        {
            HeroArchetype_SkirmishRow row = new HeroArchetype_SkirmishRow
            {
                heroId = "Assassin", huntRadius = 1f, retargetScoreMul = 2f, strikeWindowSec = 3f, crowdCount = 4, crowdRadius = 5f,
                slipDistance = 6f, slipDurationSec = 7f, slipInvulnSec = 8f, slipCooldownSec = 9f, minDwellSec = 10f, arenaSoftRadius = 11f,
            };

            SkirmishSettings s = SkirmishSettings.From(row);

            Assert.AreEqual(1f, s.HuntRadius);
            Assert.AreEqual(2f, s.RetargetScoreMul);
            Assert.AreEqual(3f, s.StrikeWindowSec);
            Assert.AreEqual(4, s.CrowdCount);
            Assert.AreEqual(5f, s.CrowdRadius);
            Assert.AreEqual(6f, s.SlipDistance);
            Assert.AreEqual(7f, s.SlipDurationSec);
            Assert.AreEqual(8f, s.SlipInvulnSec);
            Assert.AreEqual(9f, s.SlipCooldownSec);
            Assert.AreEqual(10f, s.MinDwellSec);
            Assert.AreEqual(11f, s.ArenaSoftRadius);
        }

        [Test]
        public void 실제_JSON의_습격_설정이_SkirmishSettings로_변환된다()
        {
            ChapterDatabase db = WorldMapTestSupport.ShippedDb();

            SkirmishSettings s = SkirmishSettings.From(db.GetSkirmish("Assassin"));

            Assert.AreEqual(2.8f, s.StrikeWindowSec, 0.0001f);
            Assert.AreEqual(3, s.CrowdCount);
            Assert.AreEqual(5f, s.SlipCooldownSec, 0.0001f);
            Assert.AreEqual(0.4f, s.MinDwellSec, 0.0001f);
            Assert.AreEqual(7f, s.ArenaSoftRadius, 0.0001f);
        }

        [Test]
        public void 생성_직후는_추적_상태이고_공격_불가다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Shipped());

            Assert.AreEqual(ESkirmishState.Chase, b.State);
            Assert.IsFalse(b.CanAttack);
            Assert.AreEqual(2.8f, b.Settings.StrikeWindowSec, 0.0001f);
        }

        //# --- 추적 / 교전 경계 ---

        [Test]
        public void 추적에서_교전_진입은_사거리_이하_포함이고_교전_해제는_사거리_더하기_버퍼_초과다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Shipped());
            SkirmishInput atRange = Far();
            atRange.TargetDist = 1.6f;
            SkirmishInput overRange = Far();
            overRange.TargetDist = 1.61f;

            b.Update(0f, overRange);
            Assert.AreEqual(ESkirmishState.Chase, b.State);
            b.Update(0.25f, atRange);
            Assert.AreEqual(ESkirmishState.Engage, b.State);
            Assert.IsTrue(b.CanAttack);

            SkirmishInput edge = Far();
            float limit = edge.Range + edge.EngageBuffer;
            edge.TargetDist = limit;
            b.Update(0.5f, edge);
            Assert.AreEqual(ESkirmishState.Engage, b.State, "정확히 사거리+버퍼는 유지");
            edge.TargetDist = limit + 0.01f;
            b.Update(0.75f, edge);
            Assert.AreEqual(ESkirmishState.Chase, b.State);
            Assert.IsFalse(b.CanAttack);
        }

        [Test]
        public void 추적과_교전_사이_전이는_최소_유지_시간의_영향을_받지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Shipped());

            b.Update(0f, Engaged());
            b.Update(0.01f, Far());
            b.Update(0.02f, Engaged());

            Assert.AreEqual(ESkirmishState.Engage, b.State);
        }

        [Test]
        public void 첫_프레임은_시간_간격_0으로_취급해_어떤_절대_시각에서도_교전을_시작한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());

            bool slipped = b.Update(500f, Engaged());

            Assert.IsFalse(slipped);
            Assert.AreEqual(ESkirmishState.Engage, b.State);
        }

        //# --- 교전 시간 초과 ---

        [Test]
        public void 교전_시간_누적이_strikeWindow에_정확히_닿는_프레임에_슬립한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());

            Assert.IsFalse(b.Update(0.5f, Engaged()));
            Assert.IsFalse(b.Update(1.0f, Engaged()));
            Assert.IsFalse(b.Update(1.5f, Engaged()), "누적 1.5 < 2.0");
            bool slipped = b.Update(2.0f, Engaged());

            Assert.IsTrue(slipped);
            Assert.AreEqual(ESkirmishState.Slip, b.State);
            Assert.IsFalse(b.CanAttack);
        }

        [Test]
        public void 슬립_시작_프레임에서만_true를_돌려주고_그_다음_프레임에서_추적으로_돌아간다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            b.Update(1f, Engaged());
            Assert.IsTrue(b.Update(2f, Engaged()));

            bool next = b.Update(2.25f, Far());

            Assert.IsFalse(next);
            Assert.AreEqual(ESkirmishState.Chase, b.State);
        }

        [Test]
        public void 프레임_간격이_정확히_1초면_교전_시간에_더해지고_1초를_넘으면_제외된다()
        {
            SkirmishBehavior included = new SkirmishBehavior(Exact());
            included.Update(0f, Engaged());
            included.Update(1f, Engaged());
            bool includedSlip = included.Update(2f, Engaged());

            SkirmishBehavior skipped = new SkirmishBehavior(Exact());
            skipped.Update(0f, Engaged());
            skipped.Update(1.5f, Engaged());
            bool skippedSlip = skipped.Update(3f, Engaged());

            Assert.IsTrue(includedSlip, "간격 1.0 x 2 = 2.0");
            Assert.IsFalse(skippedSlip, "간격 1.5 구간은 누적에서 제외");
        }

        [Test]
        public void 교전_누적은_추적_상태에서는_쌓이지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());

            for (int i = 0; i <= 40; ++i)
            {
                Assert.IsFalse(b.Update(i * 0.5f, Far()), $"{i * 0.5f}초");
            }
            Assert.AreEqual(ESkirmishState.Chase, b.State);
        }

        [Test]
        public void 슬립_뒤_교전_누적은_0에서_다시_시작한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            b.Update(1f, Engaged());
            Assert.IsTrue(b.Update(2f, Engaged()));
            b.Update(2.5f, Far());
            b.Update(3f, Engaged());

            //# 재진입 후 2.0 누적(5.0)은 쿨다운(슬립 2.0 + 4.0 = 6.0) 안이라 슬립하지 않는다.
            Assert.IsFalse(b.Update(4f, Engaged()));
            Assert.IsFalse(b.Update(5f, Engaged()));
            Assert.IsFalse(b.Update(5.5f, Engaged()), "교전 누적 2.5 이지만 쿨다운 3.5초 경과");
            Assert.IsTrue(b.Update(6f, Engaged()), "쿨다운 4.0 경과 - 누적은 슬립 이후 새로 쌓인 것");
        }

        //# --- 군중 ---

        [TestCase(0, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(7, true)]
        public void 군중_수가_crowdCount_이상일_때만_슬립한다(int crowd, bool slips)
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());

            bool slipped = b.Update(0.5f, Engaged(crowd));

            Assert.AreEqual(slips, slipped);
        }

        [Test]
        public void 군중_슬립은_추적_상태에서도_발동한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Far());

            bool slipped = b.Update(0.25f, Far(crowd: 3));

            Assert.IsTrue(slipped);
        }

        [Test]
        public void 군중_슬립은_표적이_없어도_발동한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, NoTarget());

            bool slipped = b.Update(0.25f, NoTarget(crowd: 4));

            Assert.IsTrue(slipped);
            Assert.AreEqual(ESkirmishState.Slip, b.State);
        }

        [Test]
        public void 교전_진입_직후_군중은_최소_유지_시간_경계_0_5초_이상에서만_슬립한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());

            Assert.IsFalse(b.Update(0.25f, Engaged(3)), "유지 0.25 < 0.5");
            Assert.IsTrue(b.Update(0.5f, Engaged(3)), "유지 정확히 0.5");
        }

        [Test]
        public void 슬립_쿨다운_경계_4초_미만에서는_군중이어도_슬립하지_않고_정확히_4초에_다시_슬립한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(2f, Engaged(3)));
            b.Update(2.25f, Far());
            b.Update(2.5f, Engaged());

            Assert.IsFalse(b.Update(5.75f, Engaged(3)), "슬립 후 3.75초");
            Assert.IsTrue(b.Update(6f, Engaged(3)), "슬립 후 정확히 4.0초");
        }

        [Test]
        public void 실제_수치로도_쿨다운_5초_경계가_성립한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Shipped());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(1f, Engaged(3)));
            b.Update(1.25f, Far());
            b.Update(1.5f, Engaged());

            Assert.IsFalse(b.Update(5.9f, Engaged(3)), "슬립 후 4.9초");
            Assert.IsTrue(b.Update(6f, Engaged(3)), "슬립 후 5.0초");
        }

        //# --- 강제 슬립 ---

        [Test]
        public void 강제_슬립은_표적이_없고_쿨다운_최소_유지_중이어도_다음_프레임에_발동한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, NoTarget());

            b.RequestForcedSlip();
            bool slipped = b.Update(0.01f, NoTarget());

            Assert.IsTrue(slipped);
        }

        [Test]
        public void 강제_슬립은_슬립_중_요청하면_복귀_프레임_다음에_발동한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(1f, Engaged(3)));

            b.RequestForcedSlip();
            bool returning = b.Update(1.25f, Far());
            bool forced = b.Update(1.5f, Far());

            Assert.IsFalse(returning, "슬립 이동 종료 프레임 - 추적 복귀");
            Assert.IsTrue(forced, "쿨다운 중이지만 강제");
        }

        [Test]
        public void 강제_슬립은_한번만_소모되고_이후_쿨다운이_다시_적용된다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            b.RequestForcedSlip();
            Assert.IsTrue(b.Update(0.25f, Engaged()));
            b.Update(0.5f, Far());
            b.Update(0.75f, Engaged());

            Assert.IsFalse(b.Update(1.5f, Engaged(3)), "강제 슬립도 쿨다운 기준 시각을 갱신");
            Assert.IsFalse(b.Update(4.0f, Engaged(3)), "0.25 + 4.0 = 4.25 전");
            Assert.IsTrue(b.Update(4.25f, Engaged(3)));
        }

        [Test]
        public void 강제_슬립_요청을_중복해도_한번만_발동한다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            b.RequestForcedSlip();
            b.RequestForcedSlip();
            Assert.IsTrue(b.Update(0.25f, Engaged()));
            b.Update(0.5f, Far());

            Assert.IsFalse(b.Update(0.75f, Far()));
        }

        //# --- 표적 변경 / 리셋 ---

        [Test]
        public void 표적이_바뀌는_프레임은_누적을_0으로_만들고_그_프레임_dt도_더하지_않는다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            b.Update(1.5f - 0.5f, Engaged());
            b.Update(1.5f, Engaged(0, targetChanged: true));

            Assert.IsFalse(b.Update(2.5f, Engaged()), "변경 후 1.0 누적");
            Assert.IsTrue(b.Update(3.5f, Engaged()), "변경 후 2.0 누적");
        }

        [Test]
        public void Reset_뒤에는_쿨다운_교전_누적_강제_요청이_모두_초기화된다()
        {
            SkirmishBehavior b = new SkirmishBehavior(Exact());
            b.Update(0f, Engaged());
            Assert.IsTrue(b.Update(2f, Engaged(3)));
            b.RequestForcedSlip();

            b.Reset();

            Assert.AreEqual(ESkirmishState.Chase, b.State);
            b.Update(2.1f, Engaged());
            Assert.IsFalse(b.Update(2.6f, Engaged()), "강제 요청 소거");
            Assert.IsTrue(b.Update(2.7f, Engaged(3)), "쿨다운 소거 - 최소 유지 0.5 경과(교전 진입 2.1)");
        }

        //# --- 위협 점수 ---

        private GameObject _spawned;
        private readonly List<GameObject> _all = new List<GameObject>();

        [TearDown]
        public void 생성물을_정리한다()
        {
            foreach (GameObject go in _all)
            {
                if (go != null)
                {
                    Object.DestroyImmediate(go);
                }
            }
            _all.Clear();
        }

        private Transform NewTransform(string name, Vector3 position)
        {
            GameObject go = new GameObject(name);
            go.transform.position = position;
            _all.Add(go);
            _spawned = go;
            return go.transform;
        }

        private MeleeAttacker NewAttacker(float cooldown, int power, out Health health, int hp)
        {
            GameObject go = new GameObject("monster");
            _all.Add(go);
            MeleeAttacker attacker = go.AddComponent<MeleeAttacker>();
            attacker.Configure(1f, cooldown, power);
            health = go.AddComponent<Health>();
            health.SetMax(hp, true);
            return attacker;
        }

        [Test]
        public void 위협_점수는_공격력_곱_배율_나누기_쿨다운_나누기_현재_HP다()
        {
            MeleeAttacker attacker = NewAttacker(2f, 10, out Health health, 50);

            Assert.AreEqual(0.1f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f, "10/2/50");

            attacker.PowerScale = 2f;
            Assert.AreEqual(0.2f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f);
        }

        [Test]
        public void 위협_점수의_HP는_현재_HP이고_0이어도_1로_나눈다()
        {
            MeleeAttacker attacker = NewAttacker(1f, 10, out Health health, 100);
            health.TakeDamage(50);
            Assert.AreEqual(0.2f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f, "현재 HP 50");

            health.TakeDamage(1000);
            Assert.AreEqual(10f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f, "HP 0 -> max(1, 0) = 1");
        }

        [Test]
        public void 쿨다운이_0에_가까우면_0_05로_하한을_둬서_무한대가_되지_않는다()
        {
            MeleeAttacker attacker = NewAttacker(0.0001f, 10, out Health health, 100);

            float score = SkirmishBehavior.ThreatScore(attacker, health);

            Assert.AreEqual(2f, score, 0.0001f, "10 / 0.05 / 100");
            Assert.IsFalse(float.IsInfinity(score));
        }

        [Test]
        public void 위협_점수는_공격자나_체력이_없으면_0이다()
        {
            NewAttacker(1f, 10, out Health health, 100);

            Assert.AreEqual(0f, SkirmishBehavior.ThreatScore(null, health));
            Assert.AreEqual(0f, SkirmishBehavior.ThreatScore(NewAttacker(1f, 10, out Health _, 10), null));
        }

        [Test]
        public void 쿨다운_버프는_실효_쿨다운으로_위협_점수에_반영된다()
        {
            MeleeAttacker attacker = NewAttacker(1f, 10, out Health health, 100);
            float before = SkirmishBehavior.ThreatScore(attacker, health);

            attacker.CooldownScale = 0.5f;

            Assert.AreEqual(before * 2f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f, "공격이 2배 빨라지면 점수 2배");
            Assert.AreEqual(0.5f, attacker.EffectiveCooldown, 0.0001f);
        }

        [Test]
        public void 쿨다운_하한_0_05는_실효_쿨다운에도_적용된다()
        {
            MeleeAttacker attacker = NewAttacker(1f, 10, out Health health, 100);
            attacker.CooldownScale = 0.001f;

            Assert.AreEqual(2f, SkirmishBehavior.ThreatScore(attacker, health), 0.0001f, "10 / 0.05 / 100");
        }

        private class PlainAttacker : IAttacker
        {
            public float Range => 1f;
            public float Cooldown { get; set; } = 2f;
            public int Power { get; set; } = 10;
            public bool Enabled { get; set; } = true;
            public float PowerScale { get; set; } = 1f;
            public bool DeferStrike => false;
            public event System.Action<IHealth> OnHit;
            public bool TryAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now) { OnHit?.Invoke(target); return false; }
            public bool TryBeginAttack(IHealth target, Vector3 selfPos, Vector3 targetPos, float now) { return false; }
            public bool TryApplyStrike(float now) { return false; }
        }

        [Test]
        public void 실효_쿨다운_인터페이스가_없는_공격자는_기본_쿨다운으로_점수를_계산한다()
        {
            StageRuleFakeHero health = new StageRuleFakeHero { Current = 50 };

            float score = SkirmishBehavior.ThreatScore(new PlainAttacker(), health);

            Assert.AreEqual(0.1f, score, 0.0001f, "10 / 2 / 50");
        }

        //# --- 표적 선정 ---

        private SkirmishCandidate Candidate(string name, float score, Vector3 position)
        {
            return new SkirmishCandidate { Transform = NewTransform(name, position), Position = position, Score = score };
        }

        [Test]
        public void 후보가_null이거나_비어_있으면_음수_1을_돌려준다()
        {
            Assert.AreEqual(-1, SkirmishBehavior.SelectTarget(null, Vector3.zero, null, 1.5f));
            Assert.AreEqual(-1, SkirmishBehavior.SelectTarget(new List<SkirmishCandidate>(), Vector3.zero, null, 1.5f));
        }

        [Test]
        public void 후보가_하나면_현재_표적과_무관하게_그_후보다()
        {
            List<SkirmishCandidate> list = new List<SkirmishCandidate> { Candidate("a", 0.1f, Vector3.right) };

            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(list, Vector3.zero, null, 1.5f));
            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(list, Vector3.zero, list[0].Transform, 1.5f));
        }

        [Test]
        public void 점수가_모두_같으면_영웅에_가장_가까운_후보다()
        {
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Candidate("far", 0.5f, new Vector3(6f, 0f, 0f)),
                Candidate("mid", 0.5f, new Vector3(3f, 0f, 0f)),
                Candidate("near", 0.5f, new Vector3(1f, 0f, 0f)),
            };

            Assert.AreEqual(2, SkirmishBehavior.SelectTarget(list, Vector3.zero, null, 1.5f));
        }

        [Test]
        public void 점수가_모두_0이면_가까운_후보를_고른다()
        {
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Candidate("a", 0f, new Vector3(5f, 0f, 0f)),
                Candidate("b", 0f, new Vector3(2f, 0f, 0f)),
            };

            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(list, Vector3.zero, null, 1.5f));
        }

        [Test]
        public void 점수가_높으면_멀어도_우선한다()
        {
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Candidate("near", 0.1f, new Vector3(1f, 0f, 0f)),
                Candidate("danger", 0.9f, new Vector3(8f, 0f, 0f)),
            };

            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(list, Vector3.zero, null, 1.5f));
        }

        [Test]
        public void 현재_표적은_최고점이_현재_점수의_배수와_같으면_유지하고_초과하면_교체한다()
        {
            Transform current = NewTransform("cur", new Vector3(2f, 0f, 0f));
            List<SkirmishCandidate> keep = new List<SkirmishCandidate>
            {
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0.5f },
                Candidate("best", 0.75f, new Vector3(3f, 0f, 0f)),
            };
            List<SkirmishCandidate> swap = new List<SkirmishCandidate>
            {
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0.5f },
                Candidate("best2", 0.76f, new Vector3(3f, 0f, 0f)),
            };

            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(keep, Vector3.zero, current, 1.5f), "0.75 == 0.5 x 1.5 -> 유지");
            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(swap, Vector3.zero, current, 1.5f), "0.76 > 0.75 -> 교체");
        }

        [Test]
        public void 배수가_1이면_엄밀히_높은_점수의_후보로만_교체한다()
        {
            Transform current = NewTransform("cur", new Vector3(2f, 0f, 0f));
            List<SkirmishCandidate> equal = new List<SkirmishCandidate>
            {
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0.5f },
                Candidate("same", 0.5f, new Vector3(1f, 0f, 0f)),
            };
            List<SkirmishCandidate> higher = new List<SkirmishCandidate>
            {
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0.5f },
                Candidate("higher", 0.51f, new Vector3(9f, 0f, 0f)),
            };

            Assert.AreEqual(0, SkirmishBehavior.SelectTarget(equal, Vector3.zero, current, 1f), "최고점 0.5 > 현재 0.5 x 1 가 거짓이라 현재 표적 유지");
            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(higher, Vector3.zero, current, 1f));
        }

        [Test]
        public void 현재_표적이_이미_최고점이면_그_후보를_돌려준다()
        {
            Transform current = NewTransform("cur", new Vector3(2f, 0f, 0f));
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Candidate("low", 0.1f, new Vector3(1f, 0f, 0f)),
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0.9f },
            };

            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(list, Vector3.zero, current, 1.5f));
        }

        [Test]
        public void 현재_표적의_점수가_0이면_양수_점수_후보로_바로_교체한다()
        {
            Transform current = NewTransform("cur", new Vector3(1f, 0f, 0f));
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                new SkirmishCandidate { Transform = current, Position = current.position, Score = 0f },
                Candidate("any", 0.01f, new Vector3(5f, 0f, 0f)),
            };

            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(list, Vector3.zero, current, 1.5f));
        }

        [Test]
        public void 현재_표적이_후보에_없으면_사망이나_이탈로_보고_최고점을_고른다()
        {
            Transform gone = NewTransform("gone", new Vector3(1f, 0f, 0f));
            List<SkirmishCandidate> list = new List<SkirmishCandidate>
            {
                Candidate("a", 0.2f, new Vector3(2f, 0f, 0f)),
                Candidate("b", 0.4f, new Vector3(3f, 0f, 0f)),
            };

            Assert.AreEqual(1, SkirmishBehavior.SelectTarget(list, Vector3.zero, gone, 1.5f));
        }

        //# --- 슬립 목적지 ---

        [Test]
        public void 슬립_목적지는_이동_경계_클램프를_적용하고_충분히_이동했으면_그대로다()
        {
            //# x in [-2, 2] 로 막힌 경계 - 3.5 이동은 -2 로 줄지만 1.75(절반) 이상이라 유지.
            Vector3 destination = SkirmishBehavior.ComputeSlipDestination(Vector3.zero, true, new Vector3(1f, 0f, 0f), 3.5f,
                p => new Vector3(Mathf.Clamp(p.x, -2f, 2f), p.y, p.z));

            Assert.AreEqual(-2f, destination.x, 0.001f);
        }

        [Test]
        public void 슬립_목적지가_경계에_막혀_절반_미만이면_원점_쪽을_섞고_원점이면_그대로다()
        {
            Vector3 blockedAtOrigin = SkirmishBehavior.ComputeSlipDestination(Vector3.zero, true, new Vector3(1f, 0f, 0f), 3.5f,
                p => new Vector3(Mathf.Clamp(p.x, -1f, 1f), p.y, p.z));
            Assert.AreEqual(-1f, blockedAtOrigin.x, 0.001f, "원점에서는 섞을 원점 방향이 없다");

            Vector3 edge = SkirmishBehavior.ComputeSlipDestination(new Vector3(6f, 0f, 0f), true, new Vector3(5f, 0f, 0f), 3.5f,
                p => KiteBehavior.ClampToRadius(p, 7f));
            Assert.AreEqual(2.5f, edge.x, 0.001f, "가장자리 바깥 이탈 -> 원점 쪽 6-3.5");
        }

        [Test]
        public void 슬립_목적지는_군중_중심이_없으면_원점_쪽이고_원점에서는_뒤쪽이다()
        {
            Vector3 fromSide = SkirmishBehavior.ComputeSlipDestination(new Vector3(4f, 0f, 0f), false, Vector3.zero, 3.5f, p => KiteBehavior.ClampToRadius(p, 7f));
            Vector3 fromOrigin = SkirmishBehavior.ComputeSlipDestination(Vector3.zero, false, Vector3.zero, 3.5f, p => KiteBehavior.ClampToRadius(p, 7f));

            Assert.AreEqual(0.5f, fromSide.x, 0.001f);
            Assert.AreEqual(-3.5f, fromOrigin.z, 0.001f);
        }

        [Test]
        public void 슬립_목적지는_소프트_반경_7_안에_항상_머문다()
        {
            for (float x = -6f; x <= 6f; x += 1.5f)
            {
                for (float z = -6f; z <= 6f; z += 1.5f)
                {
                    Vector3 origin = new Vector3(x, 0f, z);
                    if (new Vector2(x, z).magnitude > 7f)
                        continue;

                    Vector3 destination = SkirmishBehavior.ComputeSlipDestination(origin, true, origin + new Vector3(0.5f, 0f, 1f), 3.5f, p => KiteBehavior.ClampToRadius(p, 7f));

                    Assert.LessOrEqual(new Vector2(destination.x, destination.z).magnitude, 7.001f, $"({x},{z})");
                }
            }
        }
    }
}
