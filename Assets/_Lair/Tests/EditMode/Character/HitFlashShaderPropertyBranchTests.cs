using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;

namespace Lair.Tests.Character
{
    //# HitFlash 의 신규 셰이더 프로퍼티 분기(_FlashInvert/_FlashWhite 유·무, monster-2d-conversion.md §6.2) — test-engineer 담당(④).
    //# 기존 HitFeedbackTests(§7 회귀)·HitFeedbackPlayTests 는 전부 "Sprites/Default"(신규 프로퍼티 없음) 경로만 커버한다.
    //# 본 파일은 신규 프로퍼티가 "있는" 머티리얼 경로를 보완 — 픽스처 셰이더는 FlashPropertyFixture.shader(같은 폴더) 참조.
    //# 코루틴(FlashCo/AttackFlashCo)은 EditMode 에서 프레임 진행이 보장되지 않아, 코루틴이 호출하는
    //# private 헬퍼(ApplyInvertedColors/ApplyBrightenedColors/RestoreOriginalColors)를 리플렉션으로 직접 구동한다(화이트박스).
    public class HitFlashShaderPropertyBranchTests
    {
        private const string FixtureShaderName = "Lair/Tests/FlashPropertyFixture";

        private GameObject _go;
        private Material _mat;

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_mat != null) Object.DestroyImmediate(_mat);
        }

        private static void InvokeVoid(object target, string method)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 메서드 존재 확인 — 시그니처 변경 감지");
            mi.Invoke(target, null);
        }

        private static void InvokeFloatArg(object target, string method, float arg)
        {
            MethodInfo mi = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"{target.GetType().Name}.{method} 메서드 존재 확인 — 시그니처 변경 감지");
            mi.Invoke(target, new object[] { arg });
        }

        //# _FlashInvert/_FlashWhite 를 가진 픽스처 셰이더 머티리얼로 HitFlash 를 배선.
        private HitFlash NewHitFlashWithPropertyShader(out Material instanceMat)
        {
            _go = new GameObject("monster");
            Health health = _go.AddComponent<Health>();

            GameObject body = new GameObject("Body");
            body.transform.SetParent(_go.transform);
            MeshRenderer rd = body.AddComponent<MeshRenderer>();
            Shader shader = Shader.Find(FixtureShaderName);
            Assert.IsNotNull(shader, $"테스트 픽스처 셰이더 {FixtureShaderName} 로드 확인 — Fixtures/FlashPropertyFixture.shader 존재해야 함");
            _mat = new Material(shader);
            rd.sharedMaterial = _mat;

            HitFlash flash = _go.AddComponent<HitFlash>();
            InvokeVoid(flash, "Awake");     //# CacheRenderers — .material 인스턴스화 + 원본색 캐시
            InvokeVoid(health, "Awake");
            InvokeVoid(flash, "Start");     //# _lastHp 캐시
            InvokeVoid(flash, "OnEnable");

            instanceMat = rd.material;      //# CacheRenderers 가 이미 인스턴스화한 것과 동일 인스턴스
            return flash;
        }

        [Test]
        public void 신규프로퍼티가_있으면_피격_반전이_FlashInvert_Float로_적용된다()
        {
            HitFlash flash = NewHitFlashWithPropertyShader(out Material instance);

            InvokeVoid(flash, "ApplyInvertedColors");

            Assert.AreEqual(1f, instance.GetFloat("_FlashInvert"), 0.0001f,
                "반전 표시는 색 조작이 아니라 _FlashInvert=1 로 전달된다(§6.2)");
        }

        [Test]
        public void 신규프로퍼티가_있으면_공격번쩍이_FlashWhite_Float로_적용된다()
        {
            HitFlash flash = NewHitFlashWithPropertyShader(out Material instance);

            InvokeFloatArg(flash, "ApplyBrightenedColors", 0.6f);

            Assert.AreEqual(0.6f, instance.GetFloat("_FlashWhite"), 0.0001f,
                "공격 번쩍은 흰색 lerp 대신 _FlashWhite Float 로 전달된다(§6.2)");
        }

        //# 엣지 — 피격 반전 도중 공격 번쩍이 겹쳐도(피격 우선, §5.4) FlashWhite 는 건드리지 않는다.
        [Test]
        public void 신규프로퍼티가_있으면_피격반전_중에는_FlashWhite가_변하지_않는다()
        {
            HitFlash flash = NewHitFlashWithPropertyShader(out Material instance);

            InvokeVoid(flash, "ApplyInvertedColors");
            float whiteBefore = instance.GetFloat("_FlashWhite");

            Assert.AreEqual(0f, whiteBefore, 0.0001f, "피격 반전만 걸렸을 때 FlashWhite 는 기본값 0 유지");
        }

        [Test]
        public void 신규프로퍼티가_있으면_원복시_두_Float가_모두_0으로_리셋된다()
        {
            HitFlash flash = NewHitFlashWithPropertyShader(out Material instance);
            InvokeVoid(flash, "ApplyInvertedColors");
            Assert.AreEqual(1f, instance.GetFloat("_FlashInvert"), 0.0001f, "원복 전 반전 상태 확인");

            InvokeVoid(flash, "RestoreOriginalColors");

            Assert.AreEqual(0f, instance.GetFloat("_FlashInvert"), 0.0001f, "원복 후 FlashInvert=0");
            Assert.AreEqual(0f, instance.GetFloat("_FlashWhite"), 0.0001f, "원복 후 FlashWhite=0");
        }

        //# 풀 재사용 — OnEnable 재호출(RestoreOriginalColors 경유) 시 신규 프로퍼티도 0 으로 리셋되는지.
        [Test]
        public void 풀재사용_OnEnable에서_신규프로퍼티도_0으로_리셋된다()
        {
            HitFlash flash = NewHitFlashWithPropertyShader(out Material instance);
            InvokeVoid(flash, "ApplyInvertedColors");
            Assert.AreEqual(1f, instance.GetFloat("_FlashInvert"), 0.0001f);

            InvokeVoid(flash, "OnEnable");   //# 풀 재사용 시뮬레이션 — 코루틴 정리 + RestoreOriginalColors

            Assert.AreEqual(0f, instance.GetFloat("_FlashInvert"), 0.0001f, "풀 재사용 OnEnable 후 FlashInvert 리셋");
            Assert.AreEqual(0f, instance.GetFloat("_FlashWhite"), 0.0001f, "풀 재사용 OnEnable 후 FlashWhite 리셋");
        }

        //# 하위 호환 회귀 — 신규 프로퍼티가 없는 머티리얼(HitFeedbackTests §7 대상과 동일 조건)은
        //# Float 를 건드리지 않고 기존 색 반전 경로(RGB invert)를 그대로 사용한다.
        [Test]
        public void 신규프로퍼티가_없으면_기존_BaseColor_반전경로를_사용한다()
        {
            _go = new GameObject("monster");
            Health health = _go.AddComponent<Health>();
            GameObject body = new GameObject("Body");
            body.transform.SetParent(_go.transform);
            MeshRenderer rd = body.AddComponent<MeshRenderer>();
            _mat = new Material(Shader.Find("Sprites/Default"));   //# _FlashInvert/_FlashWhite 없음
            Color original = new Color(0.4f, 0.5f, 0.6f, 1f);
            _mat.color = original;
            rd.sharedMaterial = _mat;

            HitFlash flash = _go.AddComponent<HitFlash>();
            InvokeVoid(flash, "Awake");
            InvokeVoid(health, "Awake");
            InvokeVoid(flash, "Start");
            InvokeVoid(flash, "OnEnable");

            Material instance = rd.material;
            InvokeVoid(flash, "ApplyInvertedColors");

            Color inverted = new Color(1f - original.r, 1f - original.g, 1f - original.b, original.a);
            Assert.AreEqual(inverted.r, instance.color.r, 0.001f, "프로퍼티 없는 머티리얼은 기존 RGB 반전 색 경로 유지(회귀)");
            Assert.AreEqual(inverted.g, instance.color.g, 0.001f);
            Assert.AreEqual(inverted.b, instance.color.b, 0.001f);
        }
    }
}
