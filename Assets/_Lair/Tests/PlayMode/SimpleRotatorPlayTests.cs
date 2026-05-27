using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;

namespace Lair.Tests.PlayMode
{
    //# SimpleRotator 의 Update 시간 기반 보간 검증. EditMode 에서는 Time.deltaTime 이 0 이라
    //# Update 효과를 측정 못 함 — PlayMode 로 분리.
    public class SimpleRotatorPlayTests
    {
        //# 회전 보간은 프레임 누적 오차가 발생할 수 있어 약간 넉넉한 톨러런스.
        private const float YawTolerance = 1.0f;

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = 1f;
        }

        private static float YawDelta(Quaternion rot, float expectedYaw)
        {
            return Quaternion.Angle(rot, Quaternion.Euler(0f, expectedYaw, 0f));
        }

        private static SimpleRotator NewRotator(out GameObject go, float speed = 540f, float initialYaw = 0f)
        {
            go = new GameObject("rotator_play_test");
            go.transform.rotation = Quaternion.Euler(0f, initialYaw, 0f);
            var r = go.AddComponent<SimpleRotator>();
            r.TurnSpeedDegPerSec = speed;
            return r;
        }

        [UnityTest]
        public IEnumerator FaceDirection_540deg_per_sec_으로_목표에_도달한다()
        {
            //# 90° 회전 — 540 deg/s 면 약 0.167초. 충분한 1초 대기.
            var r = NewRotator(out var go, speed: 540f, initialYaw: 0f);

            r.FaceDirection(new Vector3(1f, 0f, 0f));   //# 목표 yaw 90°

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(YawDelta(go.transform.rotation, 90f), YawTolerance,
                "540 deg/s 로 1초 후 목표 yaw 90° 도달");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator 회전_시_X축과_Z축이_매_프레임_0으로_강제된다()
        {
            //# 외부에서 잘못된 X/Z 회전이 들어와도 Update 한 번만 돌면 0 으로 복원.
            var r = NewRotator(out var go, speed: 540f, initialYaw: 0f);
            r.FaceDirection(new Vector3(0f, 0f, 1f));   //# 목표 yaw 0°

            //# Update 전에 외부에서 X/Z 회전 손상.
            go.transform.rotation = Quaternion.Euler(45f, 0f, 30f);

            //# 몇 프레임 진행.
            for (int i = 0; i < 5; i++) yield return null;

            var euler = go.transform.rotation.eulerAngles;
            float xNorm = Mathf.DeltaAngle(0f, euler.x);
            float zNorm = Mathf.DeltaAngle(0f, euler.z);
            Assert.Less(Mathf.Abs(xNorm), YawTolerance, "X 축이 0 으로 복원되어야 함");
            Assert.Less(Mathf.Abs(zNorm), YawTolerance, "Z 축이 0 으로 복원되어야 함");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator MoveTowardsAngle_최단경로_회전_350도에서_10도()
        {
            //# 350° → 10° 는 +20° (시계방향 wrap) 가 최단. -340° 로 도는 게 아님.
            //# 540 deg/s 면 20° 는 약 0.037초. 충분한 0.3초 대기.
            var r = NewRotator(out var go, speed: 540f, initialYaw: 350f);

            //# 10° 방향 = sin(10°), cos(10°) 의 (x, z).
            float rad10 = 10f * Mathf.Deg2Rad;
            r.FaceDirection(new Vector3(Mathf.Sin(rad10), 0f, Mathf.Cos(rad10)));

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(YawDelta(go.transform.rotation, 10f), YawTolerance,
                "350° 에서 10° 로 최단경로 +20° 회전");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TurnSpeed_느림_180_deg_per_sec_부분_회전만()
        {
            //# 180 deg/s × 0.5초 = 90° 회전. 목표 180° 인데 90° 만 도달.
            var r = NewRotator(out var go, speed: 180f, initialYaw: 0f);

            r.FaceDirection(new Vector3(0f, 0f, -1f));   //# 목표 yaw 180°

            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            //# 0.5초에 약 90° 도달 (180 deg/s × 0.5s). 프레임 누적 오차로 ±5° 톨러런스.
            float actual = Quaternion.Angle(Quaternion.identity, go.transform.rotation);
            Assert.That(actual, Is.InRange(70f, 110f),
                $"180 deg/s × 0.5초 → 약 90° 회전 기대, 실제 {actual}°");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator TurnSpeed_0_회전하지_않는다()
        {
            //# Mathf.MoveTowardsAngle 의 maxDelta = 0 → current 유지. Update 가 hang 하지 않음.
            var r = NewRotator(out var go, speed: 0f, initialYaw: 30f);

            r.FaceDirection(new Vector3(1f, 0f, 0f));   //# 목표 yaw 90°

            for (int i = 0; i < 10; i++) yield return null;

            Assert.Less(YawDelta(go.transform.rotation, 30f), YawTolerance,
                "TurnSpeed = 0 일 때 회전 없음 (시작 yaw 유지)");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator FaceDirection_매_프레임_변경_최신_목표를_따라간다()
        {
            //# 첫 목표 90° 명령 후 즉시 -90° 로 변경 — 누적 아니라 덮어쓰기.
            var r = NewRotator(out var go, speed: 540f, initialYaw: 0f);

            r.FaceDirection(new Vector3(1f, 0f, 0f));   //# 목표 90°
            yield return null;

            //# 1프레임 후 곧장 반대 방향 명령.
            r.FaceDirection(new Vector3(-1f, 0f, 0f));   //# 목표 -90° (=270°)

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                yield return null;
            }

            Assert.Less(YawDelta(go.transform.rotation, 270f), YawTolerance,
                "최신 FaceDirection 명령(270°)이 따라잡힘 — 누적/평균 아님");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator OnEnable_이전_목표_잔존_없이_리셋된다()
        {
            //# 풀 재사용 시 _hasTarget / _targetYaw 가 OnEnable 에서 리셋되는지.
            //# (transform.rotation 복원은 AutoCombatAI.OnEnable 의 SnapToDirection 책임 — 여기선
            //# SimpleRotator 단독으로 "이전 FaceDirection 목표가 안 따라옴" 만 검증.)
            var r = NewRotator(out var go, speed: 540f, initialYaw: 0f);

            //# 1) 목표 설정.
            r.FaceDirection(new Vector3(1f, 0f, 0f));   //# 목표 90°

            //# 2) 비활성화 → 재활성화 (풀 Push/Pop 모사).
            go.SetActive(false);
            yield return null;
            //# 비활성 사이 transform 을 외부에서 0° 로 복원했다고 가정.
            go.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            go.SetActive(true);

            //# 3) FaceDirection 추가 명령 없이 시간 진행 → yaw 변화 없어야.
            for (int i = 0; i < 30; i++) yield return null;

            Assert.Less(YawDelta(go.transform.rotation, 0f), YawTolerance,
                "OnEnable 후 이전 목표(90°) 자동 추적 X — 명시 FaceDirection 없으면 회전 무");

            Object.DestroyImmediate(go);
        }

        [UnityTest]
        public IEnumerator FaceDirection_호출_없으면_yaw_고정_유지()
        {
            //# 회전 명령이 아예 없을 때 — yaw 가 변하지 않아야.
            var r = NewRotator(out var go, speed: 540f, initialYaw: 123f);

            for (int i = 0; i < 30; i++) yield return null;

            Assert.Less(YawDelta(go.transform.rotation, 123f), YawTolerance,
                "FaceDirection 호출 없으면 초기 yaw 유지");

            Object.DestroyImmediate(go);
        }
    }
}
