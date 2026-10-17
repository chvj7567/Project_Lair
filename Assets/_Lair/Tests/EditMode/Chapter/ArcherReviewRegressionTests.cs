using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# (d1) 리뷰 지적 회귀 박제 — M1 회피 도약 가장자리 보정 / M3 건너뛴 프레임 허위 갇힘·갇힘 고착. 본격 스위트는 test-engineer.
    //# (M2 TimeStop 중 도약 정지는 AutoCombatAI 게이트 순서(MonoBehaviour)라 EditMode 단위 검증 불가 — PlayMode/코드 검토 대상.)
    public class ArcherReviewRegressionTests
    {
        private static KiteSettings Settings()
        {
            return new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f);
        }

        private static KiteInput Input(Vector3 pos, float nearest, Vector3 centroid)
        {
            return new KiteInput
            {
                Position = pos,
                HasThreat = true,
                NearestThreatPos = centroid,
                NearestThreatDist = nearest,
                HasCentroid = true,
                ThreatCentroid = centroid,
                TargetDist = 5f,
                Range = 6f,
                EngageBuffer = 0.5f,
            };
        }

        private static Vector3 Clamp7(Vector3 p)
        {
            return KiteBehavior.ClampToRadius(p, 7f);
        }

        [Test]
        public void M1_가장자리에서_바깥으로_도약하면_클램프로_제자리가_되므로_원점_쪽으로_재계산한다()
        {
            //# 영웅 (7,0,0) = 소프트 반경 끝, 위협은 안쪽(6,0,0) → 순진한 도약은 +X 로 클램프되어 이동 0.
            Vector3 destination = EvadeRuntime.ComputeLeapDestination(new Vector3(7f, 0f, 0f), new Vector3(6f, 0f, 0f), 4.5f, Clamp7);

            Assert.AreEqual(2.5f, destination.x, 0.001f, "원점 쪽으로 4.5 이동");
            Assert.GreaterOrEqual(Vector3.Distance(destination, new Vector3(7f, 0f, 0f)), 4.5f * EvadeRuntime.MinLeapFraction);
        }

        [Test]
        public void M1_가장자리가_아니면_기존처럼_위협_반대로_도약한다()
        {
            Vector3 destination = EvadeRuntime.ComputeLeapDestination(new Vector3(2f, 0f, 0f), new Vector3(1f, 0f, 0f), 4.5f, Clamp7);

            Assert.AreEqual(6.5f, destination.x, 0.001f);
        }

        [Test]
        public void M3_AI_가_건너뛴_구간_뒤에는_관찰_구간이_다시_시작되어_허위_갇힘이_되지_않는다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            Vector3 threat = new Vector3(2f, 0f, 0f);
            kite.Update(0f, Input(Vector3.zero, 2f, threat));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            //# 5초 건너뜀(TimeStop 등) 후 첫 프레임 — 옛 관찰 시작(0초)과 비교하면 즉시 갇힘이 되던 상황.
            kite.Update(5f, Input(Vector3.zero, 2f, threat));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            //# 새 관찰 구간(5초~) 0.5초 동안 실제로 못 움직이면 그때 갇힘.
            kite.Update(5.2f, Input(Vector3.zero, 2f, threat));
            kite.Update(5.4f, Input(Vector3.zero, 2f, threat));
            kite.Update(5.6f, Input(Vector3.zero, 2f, threat));
            Assert.AreEqual(EKiteState.Cornered, kite.State);
        }

        [Test]
        public void M3_갇힘_중_위협_배치가_바뀌어_후퇴_방향이_크게_달라지면_후퇴를_재시도한다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            Vector3 east = new Vector3(2f, 0f, 0f);
            for (float t = 0f; t <= 0.6f; t += 0.2f)
            {
                kite.Update(t, Input(Vector3.zero, 2f, east));
            }
            Assert.AreEqual(EKiteState.Cornered, kite.State);

            //# 같은 배치 — 계속 갇힘. 위협이 반대(서쪽)로 이동하면 새 탈출로가 열린 것으로 보고 재시도.
            kite.Update(0.8f, Input(Vector3.zero, 2f, east));
            kite.Update(1.0f, Input(Vector3.zero, 2f, east));
            kite.Update(1.2f, Input(Vector3.zero, 2f, east));
            Assert.AreEqual(EKiteState.Cornered, kite.State, "방향 불변·이동 없음 → 유지");

            kite.Update(1.4f, Input(Vector3.zero, 2f, new Vector3(-2f, 0f, 0f)));
            kite.Update(1.6f, Input(Vector3.zero, 2f, new Vector3(-2f, 0f, 0f)));
            kite.Update(1.8f, Input(Vector3.zero, 2f, new Vector3(-2f, 0f, 0f)));
            Assert.AreEqual(EKiteState.Retreat, kite.State);
        }
    }
}
