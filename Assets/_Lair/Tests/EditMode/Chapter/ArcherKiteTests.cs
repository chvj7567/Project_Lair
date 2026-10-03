using Lair.Character;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 궁수 카이팅 상태기계·후퇴 방향·갇힘 판정·도약 클램프·관통 기하 자체 스모크 (chapter-system §5.3). 본격 스위트는 test-engineer.
    public class ArcherKiteTests
    {
        //# 기획서 §1.2 Archer 값.
        private static KiteSettings Settings()
        {
            return new KiteSettings(3f, 4.5f, 4f, 7f, 0.5f, 0.3f, 0.4f);
        }

        private static KiteInput Input(Vector3 pos, float nearest, float targetDist, bool hasCentroid = true, Vector3? centroid = null)
        {
            return new KiteInput
            {
                Position = pos,
                HasThreat = true,
                NearestThreatPos = pos + new Vector3(nearest, 0f, 0f),
                NearestThreatDist = nearest,
                HasCentroid = hasCentroid,
                ThreatCentroid = centroid ?? (pos + new Vector3(nearest, 0f, 0f)),
                TargetDist = targetDist,
                Range = 6f,
                EngageBuffer = 0.5f,
            };
        }

        [Test]
        public void 타깃이_사거리_밖이면_접근하고_안이면_교전하며_교전에선_사격이_허용된다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());

            kite.Update(0f, Input(Vector3.zero, 20f, 10f));
            Assert.AreEqual(EKiteState.Approach, kite.State);
            Assert.IsFalse(kite.CanShoot);

            kite.Update(0.1f, Input(Vector3.zero, 20f, 5.5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
            Assert.IsTrue(kite.CanShoot);

            kite.Update(0.2f, Input(Vector3.zero, 20f, 6.3f));
            Assert.AreEqual(EKiteState.Engage, kite.State, "사거리+버퍼(6.5) 안이면 교전 유지(히스테리시스)");
            kite.Update(0.3f, Input(Vector3.zero, 20f, 6.6f));
            Assert.AreEqual(EKiteState.Approach, kite.State);
        }

        [Test]
        public void 가장_가까운_몬스터가_3_안이면_후퇴하고_4_5_이상_벌어져_최소유지시간이_지나면_복귀한다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(0f, Input(Vector3.zero, 20f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);

            kite.Update(1f, Input(Vector3.zero, 2.9f, 5f, true, new Vector3(2.9f, 0f, 0f)));
            Assert.AreEqual(EKiteState.Retreat, kite.State);
            Assert.IsFalse(kite.CanShoot, "후퇴 중엔 사격 억제");
            Assert.AreEqual(-1f, kite.RetreatDirection.x, 0.001f, "위협 중심 반대 방향");

            //# 4.5 이상이어도 최소 유지 0.4초 전에는 해제하지 않는다.
            kite.Update(1.1f, Input(Vector3.zero, 5f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);
            kite.Update(1.5f, Input(new Vector3(-3f, 0f, 0f), 5f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 후퇴_중_관찰구간_동안_거의_움직이지_못하면_갇힘이_되어_사격이_허용된다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(0f, Input(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            kite.Update(0.2f, Input(Vector3.zero, 2f, 5f));
            kite.Update(0.4f, Input(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State, "관찰 0.5초 전");

            kite.Update(0.6f, Input(new Vector3(0.1f, 0f, 0f), 2f, 5f));
            Assert.AreEqual(EKiteState.Cornered, kite.State);
            Assert.IsTrue(kite.CanShoot);
        }

        [Test]
        public void 갇힘_중_이동이_재개되면_후퇴를_다시_시도하고_위협이_멀어지면_기본_상태로_돌아간다()
        {
            KiteBehavior kite = new KiteBehavior(Settings());
            kite.Update(0f, Input(Vector3.zero, 2f, 5f));
            kite.Update(0.2f, Input(Vector3.zero, 2f, 5f));
            kite.Update(0.4f, Input(Vector3.zero, 2f, 5f));
            kite.Update(0.6f, Input(Vector3.zero, 2f, 5f));
            Assert.AreEqual(EKiteState.Cornered, kite.State);

            //# 넉백 등으로 0.3 이상 이동 → 다음 관찰 구간에 후퇴 재시도.
            kite.Update(0.8f, Input(Vector3.zero, 2f, 5f));
            kite.Update(1.0f, Input(Vector3.zero, 2f, 5f));
            kite.Update(1.2f, Input(new Vector3(0.5f, 0f, 0f), 2f, 5f));
            Assert.AreEqual(EKiteState.Retreat, kite.State);

            for (float t = 1.4f; t <= 2.2f; t += 0.2f)
            {
                kite.Update(t, Input(Vector3.zero, 2f, 5f));
            }
            kite.Update(2.4f, Input(Vector3.zero, 6f, 5f));
            Assert.AreEqual(EKiteState.Engage, kite.State);
        }

        [Test]
        public void 후퇴_방향은_대칭_포위면_직전_방향을_유지하고_아레나_바깥이면_원점_쪽으로_보정된다()
        {
            //# 위협 중심 = 자기 위치(대칭 포위) → 직전 방향 유지.
            KiteInput symmetric = Input(Vector3.zero, 2f, 5f, true, Vector3.zero);
            Vector3 kept = KiteBehavior.ComputeRetreatDirection(symmetric, Vector3.back, 7f);
            Assert.AreEqual(-1f, kept.z, 0.001f);

            //# 원점에서 8 떨어진 X 끝에서 위협이 안쪽(원점 쪽)에서 오면 후퇴가 바깥(+X)이지만 원점 방향이 가산돼 줄어든다.
            Vector3 edge = new Vector3(8f, 0f, 0f);
            KiteInput atEdge = Input(edge, 2f, 5f, true, new Vector3(6f, 0f, 0f));
            Vector3 raw = KiteBehavior.ComputeRetreatDirection(atEdge, Vector3.zero, 0f);
            Vector3 blended = KiteBehavior.ComputeRetreatDirection(atEdge, Vector3.zero, 7f);
            Assert.AreEqual(1f, raw.x, 0.001f);
            Assert.AreEqual(0f, blended.magnitude - 1f, 0.001f, "정규화");
            Assert.Less(blended.x, raw.x);
        }

        [Test]
        public void 도약_목적지는_아레나_소프트_반경_안으로_클램프된다()
        {
            Vector3 clamped = KiteBehavior.ClampToRadius(new Vector3(9f, 0f, 0f), 7f);
            Assert.AreEqual(7f, clamped.x, 0.001f);
            Vector3 inside = KiteBehavior.ClampToRadius(new Vector3(3f, 0f, 3f), 7f);
            Assert.AreEqual(3f, inside.x, 0.001f);
            Assert.AreEqual(new Vector3(20f, 0f, 0f), KiteBehavior.ClampToRadius(new Vector3(20f, 0f, 0f), 0f), "반경 0 은 무제한");
        }

        [Test]
        public void 관통_사격_직선은_길이와_반폭_안만_포함한다()
        {
            Vector3 origin = Vector3.zero;
            Vector3 dir = Vector3.right;
            Assert.IsTrue(SkillGeometry.InLine(new Vector3(5f, 0f, 0.5f), origin, dir, 9f, 0.6f));
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(5f, 0f, 0.7f), origin, dir, 9f, 0.6f), "반폭 밖");
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(9.5f, 0f, 0f), origin, dir, 9f, 0.6f), "길이 밖");
            Assert.IsFalse(SkillGeometry.InLine(new Vector3(-1f, 0f, 0f), origin, dir, 9f, 0.6f), "뒤쪽");
        }

        [Test]
        public void 회피_도약_방향은_위협_중심의_반대이고_대칭이면_원점_쪽이다()
        {
            Vector3 away = EvadeRuntime.ComputeLeapDirection(new Vector3(2f, 0f, 0f), new Vector3(3f, 0f, 0f));
            Assert.AreEqual(-1f, away.x, 0.001f);

            Vector3 towardOrigin = EvadeRuntime.ComputeLeapDirection(new Vector3(4f, 0f, 0f), new Vector3(4f, 0f, 0f));
            Assert.AreEqual(-1f, towardOrigin.x, 0.001f);
        }
    }
}
