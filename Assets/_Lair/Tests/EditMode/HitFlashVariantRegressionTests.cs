using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Character;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# spec §5.1 색 채널 회귀(최우선) — SetBaselineColor 후 세 원복 경로 모두 variant 틴트로 복귀하는가.
    //# ①피격 flash 종료 ②공격(AttackJuice) flash 종료 ③OnEnable 풀 재사용. 연속 피격/재사용에도 불변.
    //# 기존 HitFlashVariantTests 는 ①/③ 단발만 커버 — 여기서 ②(공격) + 연속/누적 불변을 보강.
    public class HitFlashVariantRegressionTests
    {
        private GameObject _go;
        private HitFlash _flash;
        private Renderer _renderer;

        [SetUp]
        public void SetUp()
        {
            //# EditMode 에서 renderer.material 접근 시 Unity 가 [Error] 벤더 로그를 뱉는다(런타임엔 정상 API).
            //# 이 로그로 SetUp 이 실패하지 않도록 톨러런스 — TearDown 에서 원복해 다른 테스트로 누출 방지.
            LogAssert.ignoreFailingMessages = true;

            _go = new GameObject("Hero");
            _renderer = _go.AddComponent<MeshRenderer>();
            Material mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            mat.SetColor("_BaseColor", Color.white);
            _renderer.sharedMaterial = mat;

            _go.AddComponent<Health>();
            _flash = _go.AddComponent<HitFlash>();

            //# EditMode 에선 Awake 미실행 → CacheRenderers 직접 호출로 원본 색 캐시(.material 인스턴스화).
            Invoke("CacheRenderers");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            Object.DestroyImmediate(_go);
        }

        private void Invoke(string method)
        {
            MethodInfo m = typeof(HitFlash).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"메서드 미발견: {method}");
            m.Invoke(_flash, null);
        }

        private void Invoke(string method, params object[] args)
        {
            MethodInfo m = typeof(HitFlash).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, $"메서드 미발견: {method}");
            m.Invoke(_flash, args);
        }

        private Color ReadBaseColor()
        {
            Material mat = _renderer.material;
            return mat.HasProperty("_BaseColor") ? mat.GetColor("_BaseColor") : mat.color;
        }

        [Test]
        public void SetBaselineColor_후_공격flash_종료해도_틴트가_유지된다()
        {
            _flash.SetBaselineColor(Color.red);
            //# AttackFlashCo 수명 재현 — 밝기 lerp(공격 번쩍) 후 코루틴 종료 시 RestoreOriginalColors.
            Invoke("ApplyBrightenedColors", 0.5f);
            Invoke("RestoreOriginalColors");
            Assert.AreEqual(Color.red, ReadBaseColor()); //# 원복 타깃이 variant 틴트여야 한다
        }

        [Test]
        public void SetBaselineColor_후_피격을_5회_연속해도_틴트가_불변이다()
        {
            _flash.SetBaselineColor(Color.green);
            for (int i = 0; i < 5; i++)
            {
                Invoke("ApplyInvertedColors");  //# 피격 순간 반전
                Invoke("RestoreOriginalColors"); //# flash 종료 원복
                Assert.AreEqual(Color.green, ReadBaseColor(), $"{i + 1}회차 피격 후 틴트 유지");
            }
        }

        [Test]
        public void SetBaselineColor_후_풀재사용을_3회_반복해도_틴트가_불변이다()
        {
            _flash.SetBaselineColor(Color.blue);
            for (int i = 0; i < 3; i++)
            {
                //# OnEnable(풀 재사용)이 부르는 원복 경로 재현.
                Invoke("RestoreOriginalColors");
                Assert.AreEqual(Color.blue, ReadBaseColor(), $"{i + 1}회차 재사용 후 틴트 유지");
            }
        }

        [Test]
        public void SetBaselineColor를_다른_틴트로_다시_불러도_최신_틴트로_원복된다()
        {
            //# 풀 재사용으로 같은 인스턴스가 다른 스테이지(다른 틴트)로 재적용되는 시나리오.
            _flash.SetBaselineColor(Color.red);
            Invoke("ApplyInvertedColors");
            Invoke("RestoreOriginalColors");
            Assert.AreEqual(Color.red, ReadBaseColor());

            _flash.SetBaselineColor(Color.cyan); //# 스테이지 전환 — 새 틴트로 갱신
            Invoke("ApplyInvertedColors");
            Invoke("RestoreOriginalColors");
            Assert.AreEqual(Color.cyan, ReadBaseColor()); //# 이전 red 잔존 없이 최신 틴트로
        }

        [Test]
        public void SetBaselineColor는_내부_원본색_캐시_전체를_새_틴트로_덮는다()
        {
            //# 여러 원본색이 섞여 있어도(공격 도중 등) baseline 이 전부 통일되는지 화이트박스 확인.
            _flash.SetBaselineColor(Color.magenta);
            FieldInfo f = typeof(HitFlash).GetField("_originalColors", BindingFlags.NonPublic | BindingFlags.Instance);
            List<Color> colors = f.GetValue(_flash) as List<Color>;
            Assert.IsNotNull(colors);
            Assert.Greater(colors.Count, 0);
            foreach (Color c in colors)
            {
                Assert.AreEqual(Color.magenta, c);
            }
        }
    }
}
