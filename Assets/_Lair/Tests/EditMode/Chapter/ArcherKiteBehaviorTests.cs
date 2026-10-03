using System.IO;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# KiteBehavior 본격 검증 — 전 상태 전이 경로, minDwellSec 진동 방지, 해제 우선, 갇힘 재시도, 후퇴 방향, 아레나 클램프 (chapter-system §5.3·§9 E-8·E-9).
    public class ArcherKiteBehaviorTests : ChapterStaticStateTestBase
    {
        //# 기획서 §1.2 Archer 값: trigger 3.0 / resume 4.5 / threatRadius 4.0 / soft 7.0 / check 0.5 / minMove 0.3 / dwell 0.4.
        private static KiteSettings Settings()
        {
            return new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f);
        }

        private static KiteInput In(Vector3 pos, float nearest, float targetDist, bool hasThreat = true)
        {
            return new KiteInput
            {
                Position = pos,
                HasThreat = hasThreat,
                NearestThreatPos = pos + new Vector3(nearest, 0f, 0f),
                NearestThreatDist = nearest,
                HasCentroid = hasThreat,
                ThreatCentroid = pos + new Vector3(nearest, 0f, 0f),
                TargetDist = targetDist,
                Range = 6f,
                EngageBuffer = 0.5f,
            };
        }

        private static KiteBehavior Retreating(float at = 0f)
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(at, In(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "전제: 후퇴 진입");
            return kite;
        }

        //# 실제 프레임처럼 ObservationGapSec(0.25) 이하 간격(0.125)으로 Update 한다 - 한 번에 크게 건너뛰면 관찰 구간이 리셋돼 갇힘이 성립하지 않는다.
        private const float Frame = 0.125f;

        //# from -> to 를 Frame 간격으로 진행. 중간 프레임은 startPos, 마지막 프레임만 endPos 에 있다고 본다(판정은 구간 끝에서만 이동량을 본다).
        private static void Run(KiteBehavior kite, float from, float to, Vector3 startPos, Vector3 endPos, float nearest = 2f, float target = 5f)
        {
            int steps = Mathf.RoundToInt((to - from) / Frame);
            for (int i = 1; i <= steps; ++i)
            {
                Vector3 pos = i == steps ? endPos : startPos;
                kite.Update(from + i * Frame, In(pos, nearest, target));
            }
        }

        private static KiteBehavior Cornered()
        {
            KiteBehavior kite = Retreating(0f);
            Run(kite, 0f, 0.5f, Vector3.zero, Vector3.zero);
            Assert.AreEqual(EKiteState.Cornered, kite.State, "전제: 갇힘 진입(t=0.5, 프레임 간격 0.125)");
            return kite;
        }

        //# --- 설정 ---

        [Test]
        public void KiteSettings_From은_row의_7개_필드를_그대로_옮긴다()
        {
            HeroArchetype_KiteRow row = new HeroArchetype_KiteRow
            {
                heroId = "Archer", triggerDist = 1f, resumeDist = 2f, threatRadius = 3f, arenaSoftRadius = 4f,
                corneredCheckSec = 5f, corneredMinMove = 6f, minDwellSec = 7f,
            };

            KiteSettings settings = KiteSettings.From(row);

            Assert.AreEqual(1f, settings.TriggerDist);
            Assert.AreEqual(2f, settings.ResumeDist);
            Assert.AreEqual(3f, settings.ThreatRadius);
            Assert.AreEqual(4f, settings.ArenaSoftRadius);
            Assert.AreEqual(5f, settings.CorneredCheckSec);
            Assert.AreEqual(6f, settings.CorneredMinMove);
            Assert.AreEqual(7f, settings.MinDwellSec);
        }

        [Test]
        public void 실제_HeroArchetype_Kite_json은_기획서_Archer_값이다()
        {
            HeroArchetype_KiteRow[] rows = JsonArrayUtility.FromJsonArray<HeroArchetype_KiteRow>(
                File.ReadAllText(WorldMapTestSupport.JsonDir + "HeroArchetype_Kite.json"));

            Assert.AreEqual(1, rows.Length);
            KiteSettings s = KiteSettings.From(rows[0]);
            Assert.AreEqual("Archer", rows[0].heroId);
            Assert.AreEqual(3f, s.TriggerDist);
            Assert.AreEqual(4.5f, s.ResumeDist);
            Assert.AreEqual(4f, s.ThreatRadius);
            Assert.AreEqual(7f, s.ArenaSoftRadius);
            Assert.AreEqual(0.5f, s.CorneredCheckSec);
            Assert.AreEqual(0.3f, s.CorneredMinMove);
            Assert.AreEqual(0.4f, s.MinDwellSec);
        }

        [Test]
        public void 해제_거리는_진입_거리보다_커서_히스테리시스_구간이_존재한다()
        {
            KiteSettings s = Settings();

            Assert.Greater(s.ResumeDist, s.TriggerDist);
        }

        [Test]
        public void 생성_직후_상태는_접근이고_사격_불가이며_후퇴_방향은_영벡터다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            Assert.AreEqual(EKiteState.Approach, kite.State);
            Assert.IsFalse(kite.CanShoot);
            Assert.AreEqual(Vector3.zero, kite.RetreatDirection);
            Assert.AreEqual(3f, kite.Settings.TriggerDist);
        }

        //# --- 접근 / 교전 ---

        [Test]
        public void 접근에서_타깃이_사거리_이내가_되는_경계는_사거리_이하_포함이다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, In(Vector3.zero, 20f, 6.01f));
            Assert.AreEqual(EKiteState.Approach, kite.State);
            kite.Update(0.1f, In(Vector3.zero, 20f, 6f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 교전에서_접근으로_돌아가는_경계는_사거리_더하기_버퍼_초과다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(0f, In(Vector3.zero, 20f, 5f));

            kite.Update(0.1f, In(Vector3.zero, 20f, 6.5f));
            Assert.AreEqual(EKiteState.Engage, kite.State, "정확히 사거리+버퍼는 유지");
            kite.Update(0.2f, In(Vector3.zero, 20f, 6.51f));
            Assert.AreEqual(EKiteState.Approach, kite.State);
        }

        [Test]
        public void 접근과_교전_사이_전이는_최소_유지_시간의_영향을_받지_않는다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, In(Vector3.zero, 20f, 5f));
            kite.Update(0.01f, In(Vector3.zero, 20f, 7f));
            kite.Update(0.02f, In(Vector3.zero, 20f, 5f));

            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 위협이_없으면_후퇴하지_않고_타깃_거리로만_접근_교전이_갈린다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, In(Vector3.zero, 0.1f, 5f, hasThreat: false));

            Assert.AreEqual(EKiteState.Engage, kite.State, "HasThreat=false 면 NearestThreatDist 는 무시");
        }

        //# --- 후퇴 진입 ---

        [TestCase(3.01f, false)]
        [TestCase(3f, false)]
        [TestCase(2.99f, true)]
        [TestCase(0f, true)]
        public void 후퇴_진입_경계는_triggerDist_미만이다(float nearest, bool retreats)
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, In(Vector3.zero, nearest, 5f));

            Assert.AreEqual(retreats ? EKiteState.Retreat : EKiteState.Engage, kite.State);
        }

        [Test]
        public void 접근_상태에서도_위협이_가까우면_교전을_거치지_않고_바로_후퇴한다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(0f, In(Vector3.zero, 20f, 10f));
            Assert.AreEqual(EKiteState.Approach, kite.State);

            kite.Update(0.1f, In(Vector3.zero, 2f, 10f));

            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }

        [Test]
        public void 생성_직후_첫_프레임은_최소_유지_시간_제약_없이_즉시_후퇴할_수_있다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, In(Vector3.zero, 1f, 5f));

            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }

        [Test]
        public void 후퇴_진입_순간_후퇴_방향이_위협_반대로_정해진다()
        {
            KiteBehavior kite = Retreating();

            Assert.AreEqual(-1f, kite.RetreatDirection.x, 0.001f);
            Assert.AreEqual(0f, kite.RetreatDirection.z, 0.001f);
            Assert.IsFalse(kite.CanShoot);
        }

        //# --- 최소 유지 시간(진동 방지) ---

        [Test]
        public void 후퇴는_해제_거리를_넘어도_최소_유지_시간_전에는_풀리지_않는다()
        {
            KiteBehavior kite = Retreating(1f);

            kite.Update(1.1f, In(new Vector3(-1f, 0f, 0f), 6f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "0.1초 경과 < 0.4");
            kite.Update(1.3f, In(new Vector3(-2f, 0f, 0f), 6f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "0.3초 경과 < 0.4");
            kite.Update(1.5f, In(new Vector3(-3f, 0f, 0f), 6f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 후퇴에서_복귀한_직후에는_위협이_다시_가까워도_최소_유지_시간_동안_재후퇴하지_않는다()
        {
            KiteBehavior kite = Retreating(1f);
            kite.Update(1.5f, In(new Vector3(-3f, 0f, 0f), 6f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);

            kite.Update(1.6f, In(new Vector3(-3f, 0f, 0f), 2f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State, "복귀 후 0.1초 — 진동 방지");

            kite.Update(2.0f, In(new Vector3(-3f, 0f, 0f), 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "0.5초 경과 후엔 재후퇴");
        }

        //# --- 해제 조건 ---

        [TestCase(4.4f, EKiteState.Retreat)]
        [TestCase(4.5f, EKiteState.Engage)]
        [TestCase(4.6f, EKiteState.Engage)]
        public void 후퇴_해제_경계는_resumeDist_이상이다(float nearest, EKiteState expected)
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(1f, In(new Vector3(-3f, 0f, 0f), nearest, 5f));

            Assert.AreEqual(expected, kite.State);
        }

        [Test]
        public void 후퇴_중_위협이_사라지면_최소_유지_시간_뒤_해제된다()
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(0.2f, In(new Vector3(-1f, 0f, 0f), 0f, 5f, hasThreat: false));
            Assert.AreEqual(EKiteState.Retreat, kite.State);
            kite.Update(0.6f, In(new Vector3(-2f, 0f, 0f), 0f, 5f, hasThreat: false));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 해제_시_기본_상태는_타깃_거리로_결정된다_사거리_이내면_교전_밖이면_접근()
        {
            KiteBehavior engage = Retreating(0f);
            engage.Update(1f, In(new Vector3(-3f, 0f, 0f), 6f, 6f));
            KiteBehavior approach = Retreating(0f);
            approach.Update(1f, In(new Vector3(-3f, 0f, 0f), 6f, 6.1f));

            Assert.AreEqual(EKiteState.Engage, engage.State);
            Assert.AreEqual(EKiteState.Approach, approach.State);
        }

        //# --- 해제 우선 / 갇힘 ---

        [Test]
        public void 해제_조건과_갇힘_조건이_같은_프레임에_성립하면_해제가_우선한다()
        {
            KiteBehavior kite = Retreating(0f);

            //# t=0.6: 이동 0 (갇힘 조건) + 위협 6 (해제 조건) → 갇힘이 아니라 복귀.
            kite.Update(0.6f, In(Vector3.zero, 6f, 5f));

            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 갇힘_판정은_관찰_구간_이동이_minMove_미만일_때만이고_경계는_미만이다()
        {
            KiteBehavior atBoundary = Retreating(0f);
            Run(atBoundary, 0f, 0.5f, Vector3.zero, new Vector3(0.3f, 0f, 0f));
            KiteBehavior below = Retreating(0f);
            Run(below, 0f, 0.5f, Vector3.zero, new Vector3(0.29f, 0f, 0f));

            Assert.AreEqual(EKiteState.Retreat, atBoundary.State, "이동 0.3 은 갇힘 아님");
            Assert.AreEqual(EKiteState.Cornered, below.State);
        }

        [Test]
        public void 관찰_구간이_끝나기_전에는_이동이_없어도_갇힘이_아니다()
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(0.49f, In(Vector3.zero, 2f, 5f));

            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }

        [Test]
        public void 이동이_충분하면_관찰_구간이_재시작되어_다음_구간의_이동으로_판정한다()
        {
            KiteBehavior kite = Retreating(0f);

            Run(kite, 0f, 0.5f, Vector3.zero, new Vector3(-1f, 0f, 0f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);
            Run(kite, 0.5f, 1.0f, new Vector3(-1f, 0f, 0f), new Vector3(-1.1f, 0f, 0f));

            Assert.AreEqual(EKiteState.Cornered, kite.State, "두 번째 구간 이동 0.1 < 0.3");
        }

        [Test]
        public void 갇힘_판정의_이동량은_XZ_평면_기준이라_높이_변화는_이동으로_치지_않는다()
        {
            KiteBehavior kite = Retreating(0f);

            Run(kite, 0f, 0.5f, Vector3.zero, new Vector3(0f, 5f, 0f));

            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        [Test]
        public void 갇힘에서는_사격이_허용되고_후퇴_방향_이동은_유지되지_않는다()
        {
            KiteBehavior kite = Cornered();

            Assert.IsTrue(kite.CanShoot);
        }

        //# --- 갇힘 중 ---

        [Test]
        public void 갇힘_중_위협이_멀어져도_최소_유지_시간_전에는_갇힘을_유지한다()
        {
            KiteBehavior kite = Cornered();

            kite.Update(0.6f, In(Vector3.zero, 6f, 5f));
            Assert.AreEqual(EKiteState.Cornered, kite.State);
            kite.Update(1.0f, In(Vector3.zero, 6f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 갇힘_중_위협이_사라져도_최소_유지_시간_뒤에_해제된다()
        {
            KiteBehavior kite = Cornered();

            kite.Update(1.0f, In(Vector3.zero, 0f, 5f, hasThreat: false));

            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 갇힘_중_관찰_구간_이동이_minMove_이상이면_후퇴를_재시도한다()
        {
            KiteBehavior kite = Cornered();

            Run(kite, 0.5f, 1.0f, Vector3.zero, new Vector3(0.3f, 0f, 0f));

            Assert.AreEqual(EKiteState.Retreat, kite.State);
            Assert.IsFalse(kite.CanShoot);
        }

        [Test]
        public void 갇힘_중_구간_이동이_minMove_미만이면_구간만_재시작하고_갇힘을_유지한다()
        {
            KiteBehavior kite = Cornered();

            Run(kite, 0.5f, 1.0f, Vector3.zero, new Vector3(0.2f, 0f, 0f));
            Assert.AreEqual(EKiteState.Cornered, kite.State);

            //# 진입 지점 기준 누적은 0.4 지만 구간 기준 이동은 0.2 - 구간이 재시작됐으므로 갇힘 유지.
            Run(kite, 1.0f, 1.5f, new Vector3(0.2f, 0f, 0f), new Vector3(0.4f, 0f, 0f));
            Assert.AreEqual(EKiteState.Cornered, kite.State);

            Run(kite, 1.5f, 2.0f, new Vector3(0.4f, 0f, 0f), new Vector3(0.8f, 0f, 0f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "마지막 구간 이동 0.4");
        }

        [Test]
        public void 갇힘_관찰_구간_전에는_이동해도_후퇴를_재시도하지_않는다()
        {
            KiteBehavior kite = Cornered();

            Run(kite, 0.5f, 0.75f, Vector3.zero, new Vector3(2f, 0f, 0f));

            Assert.AreEqual(EKiteState.Cornered, kite.State, "관찰 0.25초 < 0.5");
        }

        [Test]
        public void 재시도한_후퇴도_다시_막히면_다시_갇힘이_되는_순환이_가능하다()
        {
            KiteBehavior kite = Cornered();
            Run(kite, 0.5f, 1.0f, Vector3.zero, new Vector3(0.5f, 0f, 0f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            Run(kite, 1.0f, 1.5f, new Vector3(0.5f, 0f, 0f), new Vector3(0.5f, 0f, 0f));

            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        //# --- 후퇴 중 방향 갱신 / 복귀 ---

        [Test]
        public void 후퇴_중_위협_무게중심이_움직이면_후퇴_방향이_매_프레임_갱신된다()
        {
            KiteBehavior kite = Retreating(0f);
            Assert.AreEqual(-1f, kite.RetreatDirection.x, 0.001f);

            KiteInput input = In(new Vector3(-1f, 0f, 0f), 2f, 5f);
            input.ThreatCentroid = new Vector3(-1f, 0f, -2f);
            kite.Update(0.1f, input);

            Assert.AreEqual(0f, kite.RetreatDirection.x, 0.001f);
            Assert.AreEqual(1f, kite.RetreatDirection.z, 0.001f, "중심이 -Z 쪽이면 +Z 로 후퇴");
        }

        [Test]
        public void 후퇴_중_이동이_계속되면_위협이_가까운_동안_후퇴를_유지한다()
        {
            KiteBehavior kite = Retreating(0f);

            for (int i = 1; i <= 80; ++i)
            {
                float t = i * Frame;
                kite.Update(t, In(new Vector3(-t, 0f, 0f), 2f, 5f));

                Assert.AreEqual(EKiteState.Retreat, kite.State, $"{t}초");
            }
        }

        [Test]
        public void 후퇴_후_복귀_뒤_타깃이_사거리_밖이면_접근으로_이어진다()
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(1f, In(new Vector3(-3f, 0f, 0f), 6f, 9f));

            Assert.AreEqual(EKiteState.Approach, kite.State);
        }

        //# --- 관찰 간격(ObservationGapSec) / 재시도 각도(CorneredRetryAngleDeg) ---

        [Test]
        public void 관찰_간격과_재시도_각도_상수는_0_25초_60도다()
        {
            Assert.AreEqual(0.25f, KiteBehavior.ObservationGapSec, 0.0001f);
            Assert.AreEqual(60f, KiteBehavior.CorneredRetryAngleDeg, 0.0001f);
        }

        [Test]
        public void 프레임_간격이_정확히_관찰_간격이면_구간이_유지되어_갇힘이_성립한다()
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(0.25f, In(Vector3.zero, 2f, 5f));
            kite.Update(0.5f, In(Vector3.zero, 2f, 5f));

            Assert.AreEqual(EKiteState.Cornered, kite.State, "간격 0.25 는 초과가 아니므로 리셋 없음");
        }

        [Test]
        public void 프레임_간격이_관찰_간격을_넘으면_건너뛴_구간으로_보고_허위_갇힘이_생기지_않는다()
        {
            KiteBehavior kite = Retreating(0f);

            //# AI 가 멈춰 있던 구간(시간 정지, 공격 모션 등) 뒤 첫 프레임 - 이동 없음이어도 갇힘이 아니다.
            kite.Update(5f, In(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            kite.Update(5.25f, In(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "새 구간 0.25초");
            kite.Update(5.5f, In(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Cornered, kite.State, "새 구간 0.5초 동안 실제로 못 움직임");
        }

        [Test]
        public void 한_번에_크게_건너뛰면_이동이_없어도_갇힘이_되지_않는다()
        {
            KiteBehavior kite = Retreating(0f);

            kite.Update(0.5f, In(Vector3.zero, 2f, 5f));

            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }

        [Test]
        public void 갇힘_중_건너뛴_구간_뒤의_이동은_재시도_근거가_아니다()
        {
            KiteBehavior kite = Cornered();

            kite.Update(3f, In(new Vector3(2f, 0f, 0f), 2f, 5f));
            Run(kite, 3f, 3.5f, new Vector3(2f, 0f, 0f), new Vector3(2f, 0f, 0f));

            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        [Test]
        public void 갇힘_중_후퇴_방향이_재시도_각도를_넘게_바뀌면_이동이_없어도_후퇴를_재시도한다()
        {
            KiteBehavior kite = Cornered();
            Assert.AreEqual(-1f, kite.RetreatDirection.x, 0.001f, "전제: 갇힐 때 방향 -X");

            //# 위협 중심이 -Z 쪽으로 옮겨 후퇴 방향이 +Z (-X 와 90도) - 새 탈출로.
            for (int i = 1; i <= 4; ++i)
            {
                KiteInput input = In(Vector3.zero, 2f, 5f);
                input.ThreatCentroid = new Vector3(0f, 0f, -3f);
                kite.Update(0.5f + i * Frame, input);
            }

            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }

        [Test]
        public void 갇힘_중_후퇴_방향_변화가_재시도_각도_이내면_이동이_없는_동안_갇힘을_유지한다()
        {
            KiteBehavior kite = Cornered();

            //# 중심이 (1,0,-1) -> 후퇴 방향 (-1,0,1)/sqrt2, -X 와 45도 < 60도.
            for (int i = 1; i <= 4; ++i)
            {
                KiteInput input = In(Vector3.zero, 2f, 5f);
                input.ThreatCentroid = new Vector3(1f, 0f, -1f);
                kite.Update(0.5f + i * Frame, input);
            }

            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        [Test]
        public void Reset_뒤_첫_프레임은_관찰_구간을_새로_시작한다()
        {
            KiteBehavior kite = Retreating(0f);
            kite.Reset();
            kite.Update(0.1f, In(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            Run(kite, 0.125f, 0.625f, Vector3.zero, Vector3.zero);

            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        //# --- 리셋 ---

        [Test]
        public void Reset은_상태_방향을_초기화하고_첫_프레임_즉시_후퇴가_다시_가능하다()
        {
            KiteBehavior kite = Retreating(5f);
            Assert.AreNotEqual(Vector3.zero, kite.RetreatDirection);

            kite.Reset();

            Assert.AreEqual(EKiteState.Approach, kite.State);
            Assert.AreEqual(Vector3.zero, kite.RetreatDirection);
            kite.Update(5.01f, In(Vector3.zero, 1f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "최소 유지 시간 잔존 없음");
        }

        [Test]
        public void Reset은_갇힘_상태도_접근으로_되돌린다()
        {
            KiteBehavior kite = Cornered();

            kite.Reset();

            Assert.AreEqual(EKiteState.Approach, kite.State);
            Assert.IsFalse(kite.CanShoot);
        }

        //# --- ComputeRetreatDirection ---

        private static KiteInput Dir(Vector3 pos, bool hasCentroid, Vector3 centroid, bool hasThreat, Vector3 nearest)
        {
            return new KiteInput
            {
                Position = pos, HasCentroid = hasCentroid, ThreatCentroid = centroid, HasThreat = hasThreat, NearestThreatPos = nearest,
            };
        }

        [Test]
        public void 후퇴_방향_단일_위협은_중심의_정반대_단위벡터다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, true, new Vector3(3f, 0f, 4f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(-0.6f, dir.x, 0.001f);
            Assert.AreEqual(-0.8f, dir.z, 0.001f);
            Assert.AreEqual(1f, dir.magnitude, 0.001f);
        }

        [Test]
        public void 후퇴_방향은_높이_차이를_무시한다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(0f, 3f, 0f), true, new Vector3(2f, -5f, 0f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(0f, dir.y, 0.0001f);
            Assert.AreEqual(-1f, dir.x, 0.001f);
        }

        [Test]
        public void 대칭_포위면_직전_방향을_정규화해_유지한다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, true, Vector3.zero, true, new Vector3(1f, 0f, 0f)), new Vector3(0f, 0f, -2f), 7f);

            Assert.AreEqual(0f, dir.x, 0.001f);
            Assert.AreEqual(-1f, dir.z, 0.001f);
        }

        [Test]
        public void 대칭_포위이고_직전_방향도_없으면_최근접_위협의_반대로_후퇴한다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, true, Vector3.zero, true, new Vector3(0f, 0f, 2f)), Vector3.zero, 7f);

            Assert.AreEqual(-1f, dir.z, 0.001f);
        }

        [Test]
        public void 무게중심이_없으면_직전_방향이_최근접_반대보다_우선한다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, false, Vector3.zero, true, new Vector3(5f, 0f, 0f)), new Vector3(0f, 0f, 1f), 7f);

            Assert.AreEqual(1f, dir.z, 0.001f, "현행 우선순위: 무게중심 → 직전 방향 → 최근접");
        }

        [Test]
        public void 무게중심이_없고_직전도_없으면_최근접_반대다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(1f, 0f, 0f), false, Vector3.zero, true, new Vector3(4f, 0f, 0f)), Vector3.zero, 7f);

            Assert.AreEqual(-1f, dir.x, 0.001f);
        }

        [Test]
        public void 위협_정보가_전혀_없으면_원점_쪽으로_후퇴한다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(3f, 0f, 0f), false, Vector3.zero, false, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(-1f, dir.x, 0.001f);
        }

        [Test]
        public void 원점에서_정보가_전혀_없으면_영벡터를_돌려주고_NaN이_없다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, false, Vector3.zero, false, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(Vector3.zero, dir);
            Assert.IsFalse(float.IsNaN(dir.x) || float.IsNaN(dir.z));
        }

        [Test]
        public void 아레나_안쪽과_경계_위에서는_원점_보정이_없다()
        {
            Vector3 inside = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(5f, 0f, 0f), true, new Vector3(3f, 0f, 0f), true, Vector3.zero), Vector3.zero, 7f);
            Vector3 onEdge = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(7f, 0f, 0f), true, new Vector3(5f, 0f, 0f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(1f, inside.x, 0.001f);
            Assert.AreEqual(1f, onEdge.x, 0.001f, "반경과 같으면 보정 없음(초과일 때만)");
        }

        [Test]
        public void 아레나_바깥에서_바깥쪽으로_후퇴하면_원점_방향으로_뒤집힌다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(8f, 0f, 0f), true, new Vector3(6f, 0f, 0f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(-1f, dir.x, 0.001f, "후퇴 +X 와 원점 -X 가 상쇄 → 원점 방향");
            Assert.AreEqual(0f, dir.z, 0.001f);
        }

        [Test]
        public void 아레나_바깥에서_옆으로_후퇴하면_원점_방향과_합성한_단위벡터다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(8f, 0f, 0f), true, new Vector3(8f, 0f, -3f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(-0.7071f, dir.x, 0.001f);
            Assert.AreEqual(0.7071f, dir.z, 0.001f);
            Assert.AreEqual(1f, dir.magnitude, 0.001f);
        }

        [TestCase(0f)]
        [TestCase(-1f)]
        public void 소프트_반경이_0_이하면_원점_보정을_하지_않는다(float soft)
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(new Vector3(20f, 0f, 0f), true, new Vector3(18f, 0f, 0f), true, Vector3.zero), Vector3.zero, soft);

            Assert.AreEqual(1f, dir.x, 0.001f);
        }

        [Test]
        public void 원점에_있으면_소프트_반경이_양수여도_보정_없이_위협_반대다()
        {
            Vector3 dir = KiteBehavior.ComputeRetreatDirection(Dir(Vector3.zero, true, new Vector3(2f, 0f, 0f), true, Vector3.zero), Vector3.zero, 7f);

            Assert.AreEqual(-1f, dir.x, 0.001f);
        }

        //# --- ClampToRadius (E-9) ---

        [Test]
        public void 클램프는_반경_안이나_경계_위의_위치를_그대로_돌려준다()
        {
            Vector3 inside = new Vector3(3f, 1f, 3f);

            Assert.AreEqual(inside, KiteBehavior.ClampToRadius(inside, 7f));
            Assert.AreEqual(new Vector3(7f, 0f, 0f), KiteBehavior.ClampToRadius(new Vector3(7f, 0f, 0f), 7f));
        }

        [Test]
        public void 클램프는_반경_밖을_같은_방향의_반경_위로_당기고_높이를_보존한다()
        {
            Vector3 clamped = KiteBehavior.ClampToRadius(new Vector3(-6f, 2.5f, -8f), 5f);

            Assert.AreEqual(-3f, clamped.x, 0.001f);
            Assert.AreEqual(-4f, clamped.z, 0.001f);
            Assert.AreEqual(2.5f, clamped.y, 0.0001f);
            Assert.AreEqual(5f, new Vector2(clamped.x, clamped.z).magnitude, 0.001f);
        }

        [Test]
        public void 클램프의_반경_판정은_XZ_평면만_본다()
        {
            Vector3 high = new Vector3(3f, 100f, 0f);

            Assert.AreEqual(high, KiteBehavior.ClampToRadius(high, 5f));
        }

        [TestCase(0f)]
        [TestCase(-3f)]
        public void 클램프_반경이_0_이하면_무제한이다(float radius)
        {
            Vector3 far = new Vector3(1000f, 0f, -1000f);

            Assert.AreEqual(far, KiteBehavior.ClampToRadius(far, radius));
        }

        [Test]
        public void 클램프는_원점과_반경_바로_바깥_경계에서_안전하다()
        {
            Assert.AreEqual(Vector3.zero, KiteBehavior.ClampToRadius(Vector3.zero, 7f));
            Vector3 justOutside = KiteBehavior.ClampToRadius(new Vector3(7.001f, 0f, 0f), 7f);
            Assert.AreEqual(7f, justOutside.x, 0.0001f);
        }
    }
}
