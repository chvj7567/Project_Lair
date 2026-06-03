using NUnit.Framework;
using UnityEngine;
using Lair.Battle;

namespace Lair.Tests.EditMode.Battle
{
    //# 순수 정적 헬퍼(AngleStep / PositionOnCircle / ComputePositions) 망라 테스트.
    //# 기획서 §3 검증표(N=1~6) + §5 엣지 + spec §7 정확도 항목 대조.
    public class CircularSpawnerArrangerTests
    {
        private const float Eps = 1e-3f;

        //# ===== AngleStep — 기획서 §3 검증표 N=1~6 대조 =====

        [Test]
        public void 한개일때_각간격은_360도()
            => Assert.AreEqual(360f, CircularSpawnerArranger.AngleStep(1), Eps);

        [Test]
        public void 두개일때_각간격은_180도()
            => Assert.AreEqual(180f, CircularSpawnerArranger.AngleStep(2), Eps);

        [Test]
        public void 세개일때_각간격은_120도()
            => Assert.AreEqual(120f, CircularSpawnerArranger.AngleStep(3), Eps);

        [Test]
        public void 네개일때_각간격은_90도()
            => Assert.AreEqual(90f, CircularSpawnerArranger.AngleStep(4), Eps);

        [Test]
        public void 여섯개일때_각간격은_60도()
            => Assert.AreEqual(60f, CircularSpawnerArranger.AngleStep(6), Eps);

        [Test]
        public void 개수가_0이면_각간격_0()
            => Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(0), Eps);

        [Test]
        public void 개수가_음수면_각간격_0()
        {
            Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(-1), Eps);
            Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(-3), Eps);
            Assert.AreEqual(0f, CircularSpawnerArranger.AngleStep(int.MinValue), Eps);
        }

        //# ===== PositionOnCircle — 대표각 0/90/180/270 좌표 정확도 (원점 center) =====

        [Test]
        public void 위치_0도면_플러스X축()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, 0f);
            Assert.AreEqual(13f, p.x, Eps);
            Assert.AreEqual(0f, p.y, Eps);
            Assert.AreEqual(0f, p.z, Eps);
        }

        [Test]
        public void 위치_90도면_플러스Z축_탑다운위()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, 90f);
            Assert.AreEqual(0f, p.x, Eps);
            Assert.AreEqual(0f, p.y, Eps);
            Assert.AreEqual(13f, p.z, Eps);
        }

        [Test]
        public void 위치_180도면_마이너스X축()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, 180f);
            Assert.AreEqual(-13f, p.x, Eps);
            Assert.AreEqual(0f, p.y, Eps);
            Assert.AreEqual(0f, p.z, Eps);
        }

        [Test]
        public void 위치_270도면_마이너스Z축_탑다운아래()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, 270f);
            Assert.AreEqual(0f, p.x, Eps);
            Assert.AreEqual(0f, p.y, Eps);
            Assert.AreEqual(-13f, p.z, Eps);
        }

        //# 비-원점 center — 좌표는 center 오프셋만큼 평행이동, y 는 center.y 보존.
        [Test]
        public void 위치_비원점center_오프셋과_y보존()
        {
            Vector3 center = new Vector3(5f, 7f, -3f);
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(center, 10f, 90f);
            Assert.AreEqual(5f, p.x, Eps);
            //# y 는 center.y 그대로 보존 (원은 XZ 평면)
            Assert.AreEqual(7f, p.y, Eps);
            Assert.AreEqual(7f, p.z, Eps);
        }

        //# radius 곱 — 같은 각이라도 반지름에 비례.
        [Test]
        public void 위치_radius배율_적용()
        {
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 5f, 0f);
            Assert.AreEqual(5f, p.x, Eps);
            Vector3 q = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 20f, 0f);
            Assert.AreEqual(20f, q.x, Eps);
        }

        //# radius 0 → center 자기 자신.
        [Test]
        public void 위치_radius0이면_center자신()
        {
            Vector3 center = new Vector3(2f, 1f, 4f);
            Vector3 p = CircularSpawnerArranger.PositionOnCircle(center, 0f, 137f);
            Assert.AreEqual(center.x, p.x, Eps);
            Assert.AreEqual(center.y, p.y, Eps);
            Assert.AreEqual(center.z, p.z, Eps);
        }

        //# ===== ComputePositions — 개수 / radius 거리 / 균등 간격 / 시작각 =====

        [Test]
        public void 개수일치_요청한count만큼_생성()
        {
            Assert.AreEqual(1, CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 1, 90f).Length);
            Assert.AreEqual(4, CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 4, 90f).Length);
            Assert.AreEqual(6, CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 6, 90f).Length);
        }

        [Test]
        public void 모든점은_center에서_radius거리_원점()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 5, 90f);
            Assert.AreEqual(5, pts.Length);
            foreach (Vector3 p in pts)
                Assert.AreEqual(13f, new Vector3(p.x, 0f, p.z).magnitude, Eps);
        }

        //# 비-원점 center 에서도 모든 점이 center 에서 정확히 radius 거리.
        [Test]
        public void 모든점은_center에서_radius거리_비원점()
        {
            Vector3 center = new Vector3(8f, 0f, -6f);
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(center, 13f, 6, 90f);
            Assert.AreEqual(6, pts.Length);
            foreach (Vector3 p in pts)
            {
                Vector3 fromCenter = new Vector3(p.x - center.x, 0f, p.z - center.z);
                Assert.AreEqual(13f, fromCenter.magnitude, Eps);
            }
        }

        //# 인접 점 각거리 = 360/N 균등 (원점). 모든 인접 쌍이 동일.
        [Test]
        public void 인접점_각거리_360분의N_균등_원점()
        {
            int count = 6;
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 10f, count, 90f);
            float expected = 360f / count;
            for (int i = 0; i < count; ++i)
            {
                Vector3 a = pts[i];
                Vector3 b = pts[(i + 1) % count];
                float ang = Vector3.Angle(a, b);
                Assert.AreEqual(expected, ang, 1e-2f);
            }
        }

        //# 비-원점 center 의 균등성 — 각은 center 기준 벡터로 측정해야 정확.
        [Test]
        public void 인접점_각거리_360분의N_균등_비원점()
        {
            int count = 4;
            Vector3 center = new Vector3(3f, 0f, 9f);
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(center, 12f, count, 90f);
            float expected = 360f / count;
            for (int i = 0; i < count; ++i)
            {
                Vector3 a = pts[i] - center;
                Vector3 b = pts[(i + 1) % count] - center;
                float ang = Vector3.Angle(a, b);
                Assert.AreEqual(expected, ang, 1e-2f);
            }
        }

        //# 시작각 반영 — 첫 점은 정확히 startDeg 위치 (= PositionOnCircle(center, r, startDeg)).
        [Test]
        public void 첫점은_시작각위치()
        {
            float startDeg = 90f;
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 3, startDeg);
            Vector3 expected = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, startDeg);
            Assert.AreEqual(expected.x, pts[0].x, Eps);
            Assert.AreEqual(expected.y, pts[0].y, Eps);
            Assert.AreEqual(expected.z, pts[0].z, Eps);
        }

        //# 시작각 90° 기본 — 첫 점이 +Z(탑다운 위). 기획서 §1 직관 기준점.
        [Test]
        public void 기본시작각90도면_첫점은_플러스Z()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 4, 90f);
            Assert.AreEqual(0f, pts[0].x, Eps);
            Assert.AreEqual(13f, pts[0].z, Eps);
        }

        //# 임의 시작각 반영 — startDeg 변경 시 전체가 그만큼 회전.
        [Test]
        public void 시작각0도면_첫점은_플러스X()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 4, 0f);
            Assert.AreEqual(13f, pts[0].x, Eps);
            Assert.AreEqual(0f, pts[0].z, Eps);
        }

        //# 기획서 §3 검증표 N=4 정확 좌표 박제 — 90,180,270,0 → 위/좌/아래/우 (정사각형).
        [Test]
        public void 검증표_N4_시작각90도_네점_좌표_고정()
        {
            float r = 13f;
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, r, 4, 90f);
            //# i=0 → 90° → +Z (위)
            Assert.AreEqual(0f, pts[0].x, Eps);
            Assert.AreEqual(r, pts[0].z, Eps);
            //# i=1 → 180° → -X (좌)
            Assert.AreEqual(-r, pts[1].x, Eps);
            Assert.AreEqual(0f, pts[1].z, Eps);
            //# i=2 → 270° → -Z (아래)
            Assert.AreEqual(0f, pts[2].x, Eps);
            Assert.AreEqual(-r, pts[2].z, Eps);
            //# i=3 → 360°(=0°) → +X (우)
            Assert.AreEqual(r, pts[3].x, Eps);
            Assert.AreEqual(0f, pts[3].z, Eps);
        }

        //# 기획서 §3 검증표 N=2 — 90°/270° (위/아래 180° 대향).
        [Test]
        public void 검증표_N2_시작각90도_두점_위아래_대향()
        {
            float r = 13f;
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, r, 2, 90f);
            Assert.AreEqual(0f, pts[0].x, Eps);
            Assert.AreEqual(r, pts[0].z, Eps);
            Assert.AreEqual(0f, pts[1].x, Eps);
            Assert.AreEqual(-r, pts[1].z, Eps);
        }

        //# 모든 점은 center.y 평면에 있다 (y 보존).
        [Test]
        public void 모든점은_center의_y평면()
        {
            Vector3 center = new Vector3(1f, 4.5f, 2f);
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(center, 13f, 6, 90f);
            foreach (Vector3 p in pts)
                Assert.AreEqual(center.y, p.y, Eps);
        }

        //# ===== 엣지 — 기획서 §5 =====

        //# count 0 → 빈 배열 (null 아님). 빈 리스트 Rebuild 의 헬퍼 계약.
        [Test]
        public void 개수0이면_빈배열()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 0, 90f);
            Assert.IsNotNull(pts);
            Assert.AreEqual(0, pts.Length);
        }

        //# 음수 count → 빈 배열.
        [Test]
        public void 음수개수면_빈배열()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, -5, 90f);
            Assert.IsNotNull(pts);
            Assert.AreEqual(0, pts.Length);
        }

        //# count 1 → 1개, 시작각 위치 (AngleStep(1)=360 이라도 1점만).
        [Test]
        public void 개수1이면_시작각에_한점()
        {
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 1, 90f);
            Assert.AreEqual(1, pts.Length);
            Assert.AreEqual(0f, pts[0].x, Eps);
            Assert.AreEqual(13f, pts[0].z, Eps);
        }

        //# spec §5 중복 몬스터 = 독립 스포너. 헬퍼 차원에선 같은 개수면 같은 좌표 분포 —
        //# 종 무관하게 count 만 좌표를 결정함을 회귀로 박제.
        [Test]
        public void 좌표는_종무관_count만_의존()
        {
            //# [Wisp, Wisp, Reaper] 같은 중복 리스트(count=3)든 [A,B,C]든 좌표는 동일.
            Vector3[] a = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 3, 90f);
            Vector3[] b = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, 3, 90f);
            Assert.AreEqual(a.Length, b.Length);
            for (int i = 0; i < a.Length; ++i)
            {
                Assert.AreEqual(a[i].x, b[i].x, Eps);
                Assert.AreEqual(a[i].z, b[i].z, Eps);
            }
        }

        //# idempotent — 같은 입력 반복 호출 시 동일 결과 (Rebuild idempotency 의 헬퍼 토대, 기획서 §5).
        [Test]
        public void 같은입력_반복호출_동일결과()
        {
            Vector3 center = new Vector3(5f, 0f, 5f);
            Vector3[] first = CircularSpawnerArranger.ComputePositions(center, 11f, 5, 47f);
            Vector3[] second = CircularSpawnerArranger.ComputePositions(center, 11f, 5, 47f);
            Assert.AreEqual(first.Length, second.Length);
            for (int i = 0; i < first.Length; ++i)
            {
                Assert.AreEqual(first[i].x, second[i].x, Eps);
                Assert.AreEqual(first[i].y, second[i].y, Eps);
                Assert.AreEqual(first[i].z, second[i].z, Eps);
            }
        }

        //# ComputePositions 의 step 이 AngleStep 과 일치 — 헬퍼 합성 정합성.
        [Test]
        public void 두번째점은_시작각_더하기_AngleStep()
        {
            int count = 5;
            float startDeg = 90f;
            Vector3[] pts = CircularSpawnerArranger.ComputePositions(Vector3.zero, 13f, count, startDeg);
            float step = CircularSpawnerArranger.AngleStep(count);
            Vector3 expected = CircularSpawnerArranger.PositionOnCircle(Vector3.zero, 13f, startDeg + step);
            Assert.AreEqual(expected.x, pts[1].x, Eps);
            Assert.AreEqual(expected.z, pts[1].z, Eps);
        }
    }
}
