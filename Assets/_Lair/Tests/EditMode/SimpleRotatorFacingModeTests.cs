using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 회전 모드 분기 본격 스위트 — 수용기준 5(영웅 즉시/보간 · 몬스터 전모드 보간) + 모드 추적 누수 가드.
    //# 즉시 스냅 여부는 FaceDirection 호출 직후 transform.eulerAngles 로 관찰(Rigidbody 없는 GameObject → transform 직접 적용).
    //# 시간 기반 보간 수렴/모드 추적 보간 차단은 PlayMode(SimpleRotatorPlayTests / HeroRotationSnapHoldPlayTests) 가 담당.
    public class SimpleRotatorFacingModeTests
    {
        private const float YawTolerance = 0.01f;

        private SimpleRotator NewRotator(bool snapInstant)
        {
            GameObject go = new GameObject("rotator");
            SimpleRotator r = go.AddComponent<SimpleRotator>();
            TestReflection.SetField(r, "_snapInstant", snapInstant);
            return r;
        }

        //# 보간 차단 신호를 _snappedThisFrame(1프레임 skip) 에서 _lastMode(모드 추적) 로 대체.
        //# 영웅(_snapInstant)+AttackAligned 인 동안 Update 가 보간 경로를 아예 안 타게 하는 게이트.
        private static FacingMode ReadLastMode(SimpleRotator r)
        {
            FieldInfo f = typeof(SimpleRotator).GetField(
                "_lastMode", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "_lastMode 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            return (FacingMode)f.GetValue(r);
        }

        //# ===== 영웅(_snapInstant=true) =====

        //# 정상 — 영웅 교전(AttackAligned): 즉시 정렬.
        [Test]
        public void 영웅_AttackAligned는_즉시정렬한다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(90f, r.transform.eulerAngles.y, YawTolerance);   //# Atan2(x=1,z=0)=90°
            Object.DestroyImmediate(r.gameObject);
        }

        //# 영웅 AttackAligned 호출 시 _lastMode=AttackAligned 로 기록 (Update 보간 차단 게이트 진입 신호).
        [Test]
        public void 영웅_AttackAligned는_lastMode를_AttackAligned로_기록한다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r),
                "영웅 AttackAligned — _lastMode=AttackAligned 여야 Update 가 보간 대신 목표 yaw 재확정");
            Object.DestroyImmediate(r.gameObject);
        }

        //# 엣지 — 영웅이라도 Smooth 면 즉시 적용 X(보간은 Update).
        [Test]
        public void 영웅_Smooth는_즉시적용하지_않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.transform.rotation = Quaternion.identity;
            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance);
            Object.DestroyImmediate(r.gameObject);
        }

        //# 누수 가드 — 영웅 Smooth 호출 시 _lastMode=Smooth 여야(AttackAligned 잔존이면 비전투에도 보간이 차단돼 회전이 멎음).
        [Test]
        public void 영웅_Smooth는_lastMode를_Smooth로_기록한다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.AreEqual(FacingMode.Smooth, ReadLastMode(r),
                "Smooth — _lastMode=Smooth (AttackAligned 잔존이면 비전투 프레임에 보간 차단 누수)");
            Object.DestroyImmediate(r.gameObject);
        }

        //# 인자 없는 FaceDirection 은 AttackAligned 위임 — 영웅이면 즉시 정렬(기존 동작 보존).
        [Test]
        public void 영웅_인자없는_FaceDirection은_AttackAligned로_즉시정렬한다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right);
            Assert.AreEqual(90f, r.transform.eulerAngles.y, YawTolerance,
                "인자 없는 오버로드는 AttackAligned 위임 → 영웅 즉시 스냅");
            Object.DestroyImmediate(r.gameObject);
        }

        //# ===== 몬스터(_snapInstant=false) — 전 모드 보간(불변, 회귀 가드) =====

        //# gameplay-programmer 미커버 갭 — 몬스터는 AttackAligned 라도 즉시 스냅 X(보간 유지).
        [Test]
        public void 몬스터_AttackAligned도_즉시적용하지_않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: false);
            r.transform.rotation = Quaternion.identity;
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance,
                "_snapInstant=false → AttackAligned 라도 보간(즉시 적용 X). 몬스터 6종 동작 불변");
            Object.DestroyImmediate(r.gameObject);
        }

        //# 몬스터 Smooth 도 당연히 즉시 적용 X.
        [Test]
        public void 몬스터_Smooth도_즉시적용하지_않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: false);
            r.transform.rotation = Quaternion.identity;
            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance);
            Object.DestroyImmediate(r.gameObject);
        }

        //# 몬스터(_snapInstant=false)는 AttackAligned 로 _lastMode 가 기록돼도 즉시 적용 안 함(보간 차단은 _snapInstant 동시 충족 필요).
        [Test]
        public void 몬스터_AttackAligned는_lastMode기록해도_즉시적용하지않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: false);
            r.transform.rotation = Quaternion.identity;
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r),
                "_lastMode 는 모드 무관 기록");
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance,
                "몬스터(_snapInstant=false) — AttackAligned 라도 즉시 적용 X(보간 차단은 _snapInstant 충족 시만). 몬스터 6종 불변");
            Object.DestroyImmediate(r.gameObject);
        }

        //# ===== 공통 가드 =====

        //# 제로벡터/미세벡터는 모드 무관 no-op(목표 미설정, 스냅도 안 함, _lastMode 기록 전 가드 return).
        [Test]
        public void 제로벡터는_모드무관_no_op이다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            r.FaceDirection(Vector3.zero, FacingMode.AttackAligned);
            Assert.AreEqual(30f, r.transform.eulerAngles.y, YawTolerance,
                "zero 벡터 — AttackAligned 라도 가드로 no-op(스냅 안 함)");
            Assert.AreEqual(FacingMode.Smooth, ReadLastMode(r),
                "no-op 이면 _lastMode 도 기록 안 됨(가드가 기록 라인 앞에서 return)");
            Object.DestroyImmediate(r.gameObject);
        }

        //# OnEnable(풀 재사용) 후 _lastMode 가 Smooth 로 리셋(수용기준 6 의 회전기 측면).
        //# AttackAligned 잔존이면 재사용된 몬스터/비전투 영웅이 첫 프레임부터 보간 차단돼 회전이 멎음.
        [Test]
        public void OnEnable후_lastMode가_Smooth로_리셋된다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            //# AttackAligned 로 _lastMode 를 세운 뒤 OnEnable 리셋 확인.
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r), "사전: _lastMode=AttackAligned");

            MethodInfo onEnable = typeof(SimpleRotator).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onEnable, "OnEnable 리플렉션 실패 — production 시그니처 변경 의심.");
            onEnable.Invoke(r, null);

            Assert.AreEqual(FacingMode.Smooth, ReadLastMode(r),
                "OnEnable 후 _lastMode=Smooth 잔존 없음(풀 재사용 격리)");
            Object.DestroyImmediate(r.gameObject);
        }

        //# ===== 모드 전환 — _lastMode 추적이 호출마다 갱신되는지 (수정의 핵심 신규 가드) =====

        //# 전환 Smooth→AttackAligned(영웅) — 보간 중이던(즉시 미적용) 상태에서 AttackAligned 호출 시 즉시 스냅 + _lastMode 갱신.
        //# 보간→스냅 전환의 즉시 효과는 시간 진행 없이 FaceDirection 직후 transform 으로 바로 관찰된다(EditMode 적격).
        [Test]
        public void 영웅_Smooth로_보간중이다가_AttackAligned호출하면_즉시스냅한다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.transform.rotation = Quaternion.identity;

            //# 먼저 Smooth — 즉시 적용 안 함(0° 유지), _lastMode=Smooth.
            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance, "사전: Smooth 는 즉시 미적용(0° 유지)");
            Assert.AreEqual(FacingMode.Smooth, ReadLastMode(r), "사전: _lastMode=Smooth");

            //# 같은 방향을 AttackAligned 로 — 영웅이면 즉시 스냅으로 전환되어야(보간 대기 없이 target 착지).
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(90f, r.transform.eulerAngles.y, YawTolerance,
                "Smooth→AttackAligned 전환 — 영웅은 즉시 스냅으로 전환돼 +X(90°) 착지(전환 미반영이면 0° 잔존)");
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r),
                "전환 후 _lastMode=AttackAligned (호출마다 갱신)");
            Object.DestroyImmediate(r.gameObject);
        }

        //# 전환 AttackAligned→Smooth(영웅) — 즉시 효과 부분.
        //# AttackAligned 로 스냅 잠금이던 영웅이 Smooth 로 새 방향 호출하면, 그 호출은 즉시 적용하지 않고(보간 대기)
        //# _lastMode=Smooth 로 풀린다. transform 은 직전 스냅값(90°)에 머문다(보간은 Update 가 — PlayMode 가 검증).
        //# 핵심: Smooth 호출이 즉시 새 target 으로 스냅하지 않는다 + 잠금이 _lastMode 레벨에서 해제된다.
        [Test]
        public void 영웅_AttackAligned로_스냅후_Smooth호출하면_즉시적용하지않고_lastMode가_풀린다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.transform.rotation = Quaternion.identity;

            //# AttackAligned 로 +X(90°) 스냅 잠금.
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(90f, r.transform.eulerAngles.y, YawTolerance, "사전: AttackAligned 즉시 스냅(90°)");
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r), "사전: _lastMode=AttackAligned(스냅 잠금)");

            //# 새 방향(-X, 270°)을 Smooth 로 — 즉시 적용 X(직전 90° 유지), _lastMode 가 Smooth 로 풀려 Update 보간 경로 복귀.
            r.FaceDirection(Vector3.left, FacingMode.Smooth);
            Assert.AreEqual(90f, r.transform.eulerAngles.y, YawTolerance,
                "AttackAligned→Smooth — Smooth 호출은 즉시 적용 안 함(직전 스냅 90° 유지, 270° 로 점프 X)");
            Assert.AreEqual(FacingMode.Smooth, ReadLastMode(r),
                "전환 후 _lastMode=Smooth — 스냅 잠금 해제(잠금 잔존이면 Update 가 영영 보간 안 함 = 회전 멎음)");
            Object.DestroyImmediate(r.gameObject);
        }

        //# 몬스터 Smooth→AttackAligned — _snapInstant=false 라 전환해도 즉시 스냅 안 함(보간 유지).
        //# 대조군: 모드 전환 자체가 스냅 잠금을 걸지 않으며, 잠금은 _snapInstant 동시 충족이 필요함을 전환 케이스로 박는다.
        [Test]
        public void 몬스터_Smooth에서_AttackAligned로_전환해도_즉시적용하지않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: false);
            r.transform.rotation = Quaternion.identity;

            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance, "사전: 몬스터 Smooth 즉시 미적용");

            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.AreEqual(FacingMode.AttackAligned, ReadLastMode(r), "_lastMode 는 모드 무관 갱신");
            Assert.AreEqual(0f, r.transform.eulerAngles.y, YawTolerance,
                "몬스터(_snapInstant=false) — AttackAligned 로 전환해도 즉시 스냅 X(보간 유지). 몬스터 6종 불변");
            Object.DestroyImmediate(r.gameObject);
        }
    }
}
