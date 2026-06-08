using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.EditMode
{
    //# 회전 모드 분기 본격 스위트 — 수용기준 5(영웅 즉시/보간 · 몬스터 전모드 보간) + _snappedThisFrame 누수 가드.
    //# 즉시 스냅 여부는 FaceDirection 호출 직후 transform.eulerAngles 로 관찰(Rigidbody 없는 GameObject → transform 직접 적용).
    //# 시간 기반 보간 수렴/스냅-후-1프레임-skip 은 PlayMode(SimpleRotatorPlayTests / AutoCombatAIRotationTests) 가 담당.
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

        private static bool ReadSnappedThisFrame(SimpleRotator r)
        {
            FieldInfo f = typeof(SimpleRotator).GetField(
                "_snappedThisFrame", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(f, "_snappedThisFrame 필드 리플렉션 실패 — production 시그니처 변경 의심.");
            return (bool)f.GetValue(r);
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

        //# 영웅 AttackAligned + 즉시 스냅 시 _snappedThisFrame=true (같은 프레임 Update 보간 1회 skip 신호).
        [Test]
        public void 영웅_AttackAligned는_snappedThisFrame를_세운다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.IsTrue(ReadSnappedThisFrame(r),
                "영웅 즉시 스냅 프레임 — _snappedThisFrame=true 여야 같은 프레임 보간 skip");
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

        //# 누수 가드 — 영웅 Smooth 프레임엔 _snappedThisFrame 이 서지 않아야(스냅 1프레임-skip 이 비전투에 누수 X).
        [Test]
        public void 영웅_Smooth는_snappedThisFrame를_세우지않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.FaceDirection(Vector3.right, FacingMode.Smooth);
            Assert.IsFalse(ReadSnappedThisFrame(r),
                "Smooth 프레임 — _snappedThisFrame=false (스냅 skip 이 보간 프레임에 누수되면 보간 첫 프레임을 먹음)");
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

        //# 몬스터는 어떤 모드든 _snappedThisFrame 이 서지 않는다(스냅 자체가 영웅 한정).
        [Test]
        public void 몬스터_AttackAligned도_snappedThisFrame를_세우지않는다()
        {
            SimpleRotator r = NewRotator(snapInstant: false);
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.IsFalse(ReadSnappedThisFrame(r),
                "몬스터(_snapInstant=false) — AttackAligned 라도 스냅 안 함 → _snappedThisFrame=false");
            Object.DestroyImmediate(r.gameObject);
        }

        //# ===== 공통 가드 =====

        //# 제로벡터/미세벡터는 모드 무관 no-op(목표 미설정, 스냅도 안 함).
        [Test]
        public void 제로벡터는_모드무관_no_op이다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            r.transform.rotation = Quaternion.Euler(0f, 30f, 0f);
            r.FaceDirection(Vector3.zero, FacingMode.AttackAligned);
            Assert.AreEqual(30f, r.transform.eulerAngles.y, YawTolerance,
                "zero 벡터 — AttackAligned 라도 가드로 no-op(스냅 안 함)");
            Assert.IsFalse(ReadSnappedThisFrame(r), "no-op 이면 _snappedThisFrame 도 안 섬");
            Object.DestroyImmediate(r.gameObject);
        }

        //# OnEnable(풀 재사용) 후 _snappedThisFrame 잔존 없음(수용기준 6 의 회전기 측면).
        [Test]
        public void OnEnable후_snappedThisFrame가_리셋된다()
        {
            SimpleRotator r = NewRotator(snapInstant: true);
            //# 스냅해서 _snappedThisFrame=true 로 만든 뒤 OnEnable 리셋 확인.
            r.FaceDirection(Vector3.right, FacingMode.AttackAligned);
            Assert.IsTrue(ReadSnappedThisFrame(r), "사전: 스냅으로 _snappedThisFrame=true");

            MethodInfo onEnable = typeof(SimpleRotator).GetMethod(
                "OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onEnable, "OnEnable 리플렉션 실패 — production 시그니처 변경 의심.");
            onEnable.Invoke(r, null);

            Assert.IsFalse(ReadSnappedThisFrame(r),
                "OnEnable 후 _snappedThisFrame 잔존 없음(풀 재사용 격리)");
            Object.DestroyImmediate(r.gameObject);
        }
    }
}
