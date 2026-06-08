using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode.Character
{
    //# 영웅 즉시 회전 스냅 회귀 — 통합 갭 재현 스위트(PlayMode).
    //# 근본원인: ApplyYaw 가 MoveRotation 을 쓰면 transform.eulerAngles 가 다음 물리 step 까지 stale.
    //# 공격 중 AutoCombatAI 가 IsAttacking 게이트로 여러 프레임 FaceDirection 을 안 부르는 사이,
    //# Update 보간 경로가 stale yaw(옛 0°) 를 9°/프레임으로 보간해 큐잉된 스냅을 덮어써 영웅이 천천히 회전했다.
    //#
    //# 기존 SimpleRotatorPlayTests / HeroRotationJitterFixPlayTests 가 이 갭을 못 잡은 이유 2가지:
    //#  (1) Rigidbody 없는 GameObject → ApplyYaw 가 transform.rotation 직접 쓰기 → stale read 없음 → 버그 미발현.
    //#      이 스위트는 isKinematic Rigidbody 를 붙여 MoveRotation 경로(실제 영웅 프리팹 구성)를 강제한다.
    //#  (2) FaceDirection 직후 바로 검증 → 보간이 덮어쓰기 전.
    //#
    //# 판별 구조(핵심): Physics.simulationMode=Script 로 물리 커밋을 수동 제어해, FaceDirection 1회 뒤
    //#  "여러 Update 를 커밋 없이 돌린 다음 단 1회 Physics.Simulate" 한다. 매 Update 마다 커밋하면(1:1) 스냅이
    //#  먼저 물리에 박혀 버그 코드에서도 통과해버리므로, 'N Update 후 1 커밋' 이 버그를 노출하는 유일한 구조다.
    //#   - 수정: 매 Update 가 ApplyYaw(target) 재큐잉 → 커밋 시 target 착지(green).
    //#   - 버그: 첫 프레임 skip 후 보간이 stale 0° 에서 ~10° 를 재큐잉 → 커밋 시 ~10° 착지(red, 천천히 회전 증상).
    public class HeroRotationSnapHoldPlayTests
    {
        //# 스냅 유지 허용 오차. 즉시 스냅이면 커밋 시 target. 보간이면 stale 0° 기준 한 step 만 진행해 이 임계를 크게 넘는다.
        private const float HoldTolerance = 2.0f;
        //# 의도적 저속(기본 540 보다 낮춤). 영웅 fixed 경로는 TurnSpeed 무시(매 프레임 _targetYaw 재큐)라 무영향이고,
        //# 버그 creep(한 step) 을 작게 유지해 dt 가 커도 한 프레임에 90° 를 넘지 못하게 → red 와 몬스터 중간각 단언이 fps 견고.
        private const float TurnSpeed = 180f;
        private const int UpdatesBeforeCommit = 5;

        private readonly List<GameObject> _spawned = new();
        private SimulationMode _prevSimulationMode;

        [SetUp]
        public void SetUp()
        {
            Time.timeScale = 1f;
            //# 물리 커밋(MoveRotation 반영)을 테스트가 명시적으로 제어 — 자동 FixedUpdate 가 1:1 로 끼어들어 갭을 못 만드는 것 방지.
            _prevSimulationMode = Physics.simulationMode;
            Physics.simulationMode = SimulationMode.Script;
        }

        [TearDown]
        public void TearDown()
        {
            Physics.simulationMode = _prevSimulationMode;
            foreach (GameObject go in _spawned)
            {
                if (go != null) Object.DestroyImmediate(go);
            }
            _spawned.Clear();
            Time.timeScale = 1f;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo f = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, $"{name} 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            f.SetValue(target, value);
        }

        private static float YawDelta(Quaternion rot, float expectedYaw)
            => Quaternion.Angle(rot, Quaternion.Euler(0f, expectedYaw, 0f));

        //# 실제 영웅/몬스터 프리팹과 동일하게 isKinematic Rigidbody 를 붙인 회전기.
        //# Rigidbody 가 있어야 ApplyYaw 가 MoveRotation 경로를 타고, transform.eulerAngles 가 물리 커밋까지 stale 해진다.
        private SimpleRotator NewRotatorWithKinematicBody(bool snapInstant)
        {
            GameObject go = new GameObject("rotator");
            Rigidbody rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            SimpleRotator r = go.AddComponent<SimpleRotator>();
            r.TurnSpeedDegPerSec = TurnSpeed;
            SetField(r, "_snapInstant", snapInstant);
            _spawned.Add(go);
            return r;
        }

        //# ===== 핵심 갭 재현 — 영웅(snapInstant) 은 FaceDirection 1회 후 보간 없이 target 유지 =====

        //# 영웅이 AttackAligned 로 1회 정렬한 뒤, FaceDirection 을 다시 부르지 않고 여러 Update 를 커밋 없이 진행한다.
        //# (공격 중 IsAttacking 게이트로 FaceDirection 이 끊긴 상황 모사.) 그 다음 단 1회 물리 커밋 후 yaw 를 읽는다.
        //# 스냅이 유지되면 target(+X 90°). 버그면 보간이 stale 0° 에서 ~9° 만 진행한 값이 커밋돼 편차가 크다 → FAIL.
        [UnityTest]
        public IEnumerator 영웅_AttackAligned_1회후_여러Update_커밋없이_진행해도_target에_머문다()
        {
            SimpleRotator r = NewRotatorWithKinematicBody(snapInstant: true);
            r.transform.rotation = Quaternion.identity;   //# 시작 0° — target 과 90° 떨어뜨려 보간이면 반드시 중간각 발생.

            //# 1회만 호출 — 이후 절대 다시 부르지 않는다(IsAttacking 게이트로 FaceDirection 끊긴 공격 상황 모사).
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);

            //# 커밋 없이 여러 Update 진행 — 버그 코드면 이 사이 보간이 큐잉된 스냅을 stale 0° 기준 ~9° 로 덮어쓴다.
            for (int i = 0; i < UpdatesBeforeCommit; ++i)
                yield return null;

            //# 단 1회 물리 커밋 — 마지막으로 큐잉된 회전이 transform 에 반영된다.
            Physics.Simulate(Time.fixedDeltaTime);

            Assert.Less(YawDelta(r.transform.rotation, 90f), HoldTolerance,
                $"영웅 즉시 스냅 — FaceDirection 1회 후 여러 Update 가 지나도 +X(90°) 유지해야. 편차 " +
                $"{YawDelta(r.transform.rotation, 90f):F1}° (버그면 보간이 stale yaw 로 큐를 덮어써 천천히 회전).");
        }

        //# ===== 몬스터 대조군 — _snapInstant=false 는 같은 시나리오에서 보간(중간각, 동작 불변) =====

        //# 몬스터는 어떤 모드든 보간 유지. 같은 1회-호출-후-여러Update-1커밋 시나리오에서 target 에 못 닿고(중간각 = 보간 증거),
        //# 커밋을 더 진행하면 편차가 줄어든다(멈추지 않고 계속 보간). 수정이 몬스터 회전을 안 건드렸음을 양방향으로 박는다.
        //# 절대 도달(deviation<tol)이 아니라 '진행'(나중 < 처음) 으로 검증 — fps 에 따라 60 커밋이 90° 누적에 부족해
        //# 정상 코드가 false-FAIL 하는 것 방지(머신 속도 무관).
        [UnityTest]
        public IEnumerator 몬스터_AttackAligned_1회후_보간으로_중간각거쳐_target으로_진행한다()
        {
            SimpleRotator r = NewRotatorWithKinematicBody(snapInstant: false);
            r.transform.rotation = Quaternion.identity;

            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);

            //# 첫 커밋 — 보간이면 stale 0° 기준 일부만 진행해 target(90°) 에 못 닿아야(중간각 = 보간 증거).
            yield return null;
            Physics.Simulate(Time.fixedDeltaTime);
            float earlyDeviation = YawDelta(r.transform.rotation, 90f);
            Assert.Greater(earlyDeviation, HoldTolerance,
                $"몬스터(_snapInstant=false) — 첫 커밋엔 보간 중이라 target 에 못 닿아야(중간각). 편차 {earlyDeviation:F1}° " +
                "(즉시 스냅이면 0° 라 이 단언이 깨짐 = 몬스터 동작이 바뀐 것).");

            //# 커밋을 더 진행 — 보간이 멈추지 않고 target 쪽으로 계속 줄어든다.
            for (int i = 0; i < 10; ++i)
            {
                yield return null;
                Physics.Simulate(Time.fixedDeltaTime);
            }
            float laterDeviation = YawDelta(r.transform.rotation, 90f);
            Assert.Less(laterDeviation, earlyDeviation,
                $"몬스터 — 보간이 멈추지 않고 target 쪽으로 진행(편차 {earlyDeviation:F1}° → {laterDeviation:F1}°). 동작 불변.");
        }

        //# ===== 모드 전환 후 보간 재개(잠금 해제) — 시간 진행이 핵심이라 PlayMode =====

        //# 영웅이 AttackAligned 로 스냅 잠금(_lastMode=AttackAligned)이던 상태에서 Smooth 로 새 방향을 호출하면,
        //# _lastMode 가 Smooth 로 풀려 Update 가 다시 보간 경로를 타야 한다. 전환 후 영웅은 몬스터처럼 '보간'으로
        //# 새 target(+Z 0°) 으로 점진 수렴해야 한다 — 그 점진성이 곧 잠금 해제의 증거다.
        //# 잠금이 안 풀리면(버그) Update 가 매 프레임 ApplyYaw(_targetYaw=0°) 로 즉시 점프 → 첫 커밋부터 target 도달.
        //#   판별: '새 target 에서의 편차'로 본다(옛 잠금값 기준이면 점프든 보간이든 둘 다 멀어져 버그를 못 잡음).
        //#   - 수정: 첫 커밋엔 중간각(편차 큼) → 진행하며 줄어듦(보간 수렴).
        //#   - 버그: 첫 커밋에 즉시 0° 점프(편차≈0) → early 단언 FAIL.
        //# EditMode 동등 테스트는 전환 직후 즉시-미적용 + _lastMode=Smooth 까지만 본다(Update 시간 진행은 여기서만 관찰).
        //# 비고: 새 방향은 잠금각(90°)과 90° 떨어진 +Z(0°) — 180° antipodal(MoveTowardsAngle 회전 방향 미정의)을 피한다.
        [UnityTest]
        public IEnumerator 영웅_AttackAligned로_스냅후_Smooth전환하면_보간으로_새target에_점진수렴한다()
        {
            SimpleRotator r = NewRotatorWithKinematicBody(snapInstant: true);
            r.transform.rotation = Quaternion.identity;

            //# AttackAligned 로 +X(90°) 스냅 잠금.
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Physics.Simulate(Time.fixedDeltaTime);
            Assert.Less(YawDelta(r.transform.rotation, 90f), HoldTolerance, "사전: AttackAligned 스냅으로 90° 착지");

            //# 새 방향(+Z, 0°)을 Smooth 로 전환 — 잠금 해제. 이제 영웅도 몬스터처럼 보간으로 0° 를 향해 점진 수렴해야.
            r.FaceDirection(Vector3.forward, FacingMode.Smooth);

            //# 첫 커밋 — 잠금이 풀려 보간 중이면 새 target(0°) 에 못 닿아야(중간각). 잠금 잔존이면 즉시 0° 점프 → 편차≈0 → FAIL.
            yield return null;
            Physics.Simulate(Time.fixedDeltaTime);
            float earlyDeviation = YawDelta(r.transform.rotation, 0f);
            Assert.Greater(earlyDeviation, HoldTolerance,
                $"AttackAligned→Smooth 전환 직후 보간 중 — 새 target(0°) 에 즉시 점프 X(중간각). 편차 {earlyDeviation:F1}° " +
                "(잠금 잔존이면 매 프레임 ApplyYaw(target) 로 1프레임에 0° 점프해 이 단언이 깨짐 = 잠금 미해제 버그).");

            //# 커밋을 더 진행 — 보간이 멈추지 않고 새 target 쪽으로 수렴(잠금 해제 + 보간 재개의 양방향 증거).
            for (int i = 0; i < UpdatesBeforeCommit; ++i)
            {
                yield return null;
                Physics.Simulate(Time.fixedDeltaTime);
            }
            float laterDeviation = YawDelta(r.transform.rotation, 0f);
            Assert.Less(laterDeviation, earlyDeviation,
                $"AttackAligned→Smooth 전환 — 잠금 해제로 Update 보간이 재개돼 새 target(0°) 으로 수렴(편차 " +
                $"{earlyDeviation:F1}° → {laterDeviation:F1}°). 변화 없으면 스냅 잠금 잔존(회전 멎음).");
        }

        //# ===== Smooth 모드 영웅 — 즉시 스냅 안 하고 보간 수렴(Smooth 경로 회귀 가드) =====

        //# 영웅(_snapInstant)이라도 Smooth 모드면 즉시 스냅하지 않고 Update 보간으로 target 에 수렴해야 한다.
        //# 첫 커밋엔 중간각(target 미도달 = 보간 증거), 커밋 진행하면 편차가 줄어든다.
        //# 영웅의 Smooth 분기가 AttackAligned 즉시 스냅으로 잘못 합쳐지면(회귀) 첫 커밋부터 0° 편차라 단언이 깨진다.
        [UnityTest]
        public IEnumerator 영웅_Smooth는_즉시스냅하지않고_보간으로_target에_수렴한다()
        {
            SimpleRotator r = NewRotatorWithKinematicBody(snapInstant: true);
            r.transform.rotation = Quaternion.identity;

            r.FaceDirection(Vector3.right, FacingMode.Smooth);

            //# 첫 커밋 — Smooth 면 보간이라 target(90°) 에 못 닿아야(중간각). 즉시 스냅이면 0° 편차 → 회귀.
            yield return null;
            Physics.Simulate(Time.fixedDeltaTime);
            float earlyDeviation = YawDelta(r.transform.rotation, 90f);
            Assert.Greater(earlyDeviation, HoldTolerance,
                $"영웅 Smooth — 첫 커밋엔 보간 중이라 target 에 못 닿아야(중간각). 편차 {earlyDeviation:F1}° " +
                "(즉시 스냅으로 회귀하면 0° 라 이 단언이 깨짐).");

            //# 커밋을 더 진행 — 보간이 target 쪽으로 계속 수렴.
            for (int i = 0; i < 10; ++i)
            {
                yield return null;
                Physics.Simulate(Time.fixedDeltaTime);
            }
            float laterDeviation = YawDelta(r.transform.rotation, 90f);
            Assert.Less(laterDeviation, earlyDeviation,
                $"영웅 Smooth — 보간이 멈추지 않고 target 쪽으로 수렴(편차 {earlyDeviation:F1}° → {laterDeviation:F1}°).");
        }
    }
}
