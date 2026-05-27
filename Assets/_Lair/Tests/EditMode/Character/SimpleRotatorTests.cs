using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# SimpleRotator 의 public API 단위 테스트 (EditMode).
    //# Update 의 시간 기반 보간 / OnEnable 통합 흐름은 PlayMode (SimpleRotatorPlayTests / AutoCombatAIRotationTests).
    public class SimpleRotatorTests
    {
        private const float YawTolerance = 0.05f;

        //# Euler 비교 — Quaternion 왕복 오차(예: 0° ↔ 359.9°) 회피 위해 Quaternion.Angle 사용.
        private static float YawDelta(Quaternion rot, float expectedYaw)
        {
            return Quaternion.Angle(rot, Quaternion.Euler(0f, expectedYaw, 0f));
        }

        private static SimpleRotator NewRotator(out GameObject go, float speed = 540f)
        {
            go = new GameObject("rotator_test");
            var r = go.AddComponent<SimpleRotator>();
            r.TurnSpeedDegPerSec = speed;
            return r;
        }

        [Test]
        public void SnapToDirection_지정_방향으로_즉시_적용()
        {
            var r = NewRotator(out var go);

            //# +X 방향 → yaw 90°
            r.SnapToDirection(new Vector3(1f, 0f, 0f));

            Assert.Less(YawDelta(go.transform.rotation, 90f), YawTolerance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SnapToDirection_Y성분_무시_XZ_평면만_사용()
        {
            var r = NewRotator(out var go);

            //# Y 성분이 커도 결과 yaw 는 +Z 방향(0°).
            r.SnapToDirection(new Vector3(0f, 99f, 1f));

            Assert.Less(YawDelta(go.transform.rotation, 0f), YawTolerance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SnapToDirection_X축과_Z축은_0으로_강제된다()
        {
            var r = NewRotator(out var go);
            //# 외부에서 잘못된 X/Z 회전 선설정.
            go.transform.rotation = Quaternion.Euler(45f, 0f, 30f);

            //# +Z 방향 — yaw 0°.
            r.SnapToDirection(new Vector3(0f, 0f, 1f));

            var euler = go.transform.rotation.eulerAngles;
            //# X / Z 정규화 — 359.x 같은 음수 표현이 양수 wrap 으로 돌아옴.
            float xNorm = Mathf.DeltaAngle(0f, euler.x);
            float zNorm = Mathf.DeltaAngle(0f, euler.z);
            Assert.Less(Mathf.Abs(xNorm), YawTolerance, "X 축이 0 으로 강제되어야 함");
            Assert.Less(Mathf.Abs(zNorm), YawTolerance, "Z 축이 0 으로 강제되어야 함");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SnapToDirection_제로벡터_no_op_회전_변경_없음()
        {
            var r = NewRotator(out var go);
            //# 사전 yaw 30° 설정.
            go.transform.rotation = Quaternion.Euler(0f, 30f, 0f);

            r.SnapToDirection(Vector3.zero);

            Assert.Less(YawDelta(go.transform.rotation, 30f), YawTolerance,
                "zero 벡터 입력 시 회전 변경 없음");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SnapToDirection_미세한_벡터_magnitude_가드로_no_op()
        {
            var r = NewRotator(out var go);
            go.transform.rotation = Quaternion.Euler(0f, 45f, 0f);

            //# sqrMagnitude < 1e-6 → no-op
            r.SnapToDirection(new Vector3(0.0005f, 0f, 0f));

            Assert.Less(YawDelta(go.transform.rotation, 45f), YawTolerance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FaceDirection_제로벡터_회전_즉시_변화_없음()
        {
            var r = NewRotator(out var go);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            //# FaceDirection 은 transform 을 즉시 바꾸지 않음 (목표만 저장).
            //# zero 면 목표도 저장하지 않으므로 이후 Update 가 와도 변경 없음.
            r.FaceDirection(Vector3.zero);

            Assert.Less(YawDelta(go.transform.rotation, 0f), YawTolerance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void FaceDirection_즉시_transform을_바꾸지_않는다()
        {
            var r = NewRotator(out var go);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);

            //# FaceDirection 은 목표만 저장 — Update 가 와야 보간 시작.
            r.FaceDirection(new Vector3(1f, 0f, 0f));

            //# 호출 직후엔 yaw 변화 0 (SnapToDirection 과의 차이점).
            Assert.Less(YawDelta(go.transform.rotation, 0f), YawTolerance,
                "FaceDirection 은 다음 Update 에서 보간 시작 — 호출 즉시는 변화 없음");

            Object.DestroyImmediate(go);
        }

        [Test]
        public void TurnSpeedDegPerSec_get_set_왕복()
        {
            var r = NewRotator(out var go, speed: 540f);

            Assert.AreEqual(540f, r.TurnSpeedDegPerSec);

            r.TurnSpeedDegPerSec = 360f;
            Assert.AreEqual(360f, r.TurnSpeedDegPerSec);

            r.TurnSpeedDegPerSec = 0f;
            Assert.AreEqual(0f, r.TurnSpeedDegPerSec);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void IRotator_인터페이스로_접근_가능_Rule10()
        {
            var r = NewRotator(out var go);

            //# Rule 06/10 — 외부는 인터페이스로 접근해야 함.
            IRotator iface = r;
            iface.SnapToDirection(new Vector3(0f, 0f, -1f));   //# yaw 180°

            Assert.Less(YawDelta(go.transform.rotation, 180f), YawTolerance);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void SnapToDirection_네_방향_정렬_검증()
        {
            //# 방향-yaw 매핑 정합성. atan2(x, z) 기반.
            //# +Z → 0°, +X → 90°, -Z → 180°, -X → 270°(=-90°).
            (Vector3 dir, float expectedYaw)[] cases =
            {
                (new Vector3(0f, 0f, 1f), 0f),
                (new Vector3(1f, 0f, 0f), 90f),
                (new Vector3(0f, 0f, -1f), 180f),
                (new Vector3(-1f, 0f, 0f), 270f),
            };

            foreach (var (dir, expected) in cases)
            {
                var r = NewRotator(out var go);
                r.SnapToDirection(dir);

                Assert.Less(YawDelta(go.transform.rotation, expected), YawTolerance,
                    $"방향 {dir} → yaw {expected}° 예상");

                Object.DestroyImmediate(go);
            }
        }
    }
}
