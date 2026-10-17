using System.Reflection;
using Lair.Stage;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lair.Tests.EditMode.Stage
{
    //# 도트 무대 파형 컴포넌트(알파 맥동·조명 깜빡임)와 조명 격자 원점 — 기획서 scene-2d-conversion §7.3 · §7.6.
    public class DotStageLightingComponentsTests
    {
        private GameObject _go;

        [TearDown]
        public void TearDown()
        {
            if (_go != null)
            {
                Object.DestroyImmediate(_go);
            }
        }

        private static void CallUpdate(Component c)
        {
            MethodInfo m = c.GetType().GetMethod("Update", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "Update 미발견");
            m.Invoke(c, null);
        }

        private static void CallAwake(Component c)
        {
            MethodInfo m = c.GetType().GetMethod("Awake", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, "Awake 미발견");
            m.Invoke(c, null);
        }

        private DotAlphaPulse NewPulse(float baseValue, float amp, float omega, float phase, out SpriteRenderer sr)
        {
            _go = new GameObject("pulse");
            sr = _go.AddComponent<SpriteRenderer>();
            sr.color = new Color(0.2f, 0.4f, 0.6f, 1f);
            DotAlphaPulse p = _go.AddComponent<DotAlphaPulse>();
            TestReflection.SetField(p, "_renderer", sr);
            TestReflection.SetField(p, "_base", baseValue);
            TestReflection.SetField(p, "_amps", new[] { amp });
            TestReflection.SetField(p, "_omegas", new[] { omega });
            TestReflection.SetField(p, "_phases", new[] { phase });
            return p;
        }

        private DotLightFlicker NewFlicker(float baseValue, float amp, float omega, float phase, out Light2D light)
        {
            _go = new GameObject("flicker");
            light = _go.AddComponent<Light2D>();
            DotLightFlicker f = _go.AddComponent<DotLightFlicker>();
            TestReflection.SetField(f, "_light", light);
            TestReflection.SetField(f, "_base", baseValue);
            TestReflection.SetField(f, "_amps", new[] { amp });
            TestReflection.SetField(f, "_omegas", new[] { omega });
            TestReflection.SetField(f, "_phases", new[] { phase });
            return f;
        }

        [Test]
        public void DotAlphaPulse_알파는_기준값_더하기_사인이다()
        {
            //# ω 0 이라 시간 무관: 0.5 + 0.2·sin(π/2) = 0.7
            DotAlphaPulse p = NewPulse(0.5f, 0.2f, 0f, Mathf.PI / 2f, out SpriteRenderer sr);
            CallUpdate(p);
            Assert.AreEqual(0.7f, sr.color.a, 1e-5f);
        }

        [Test]
        public void DotAlphaPulse_알파는_1로_클램프된다()
        {
            DotAlphaPulse p = NewPulse(0.9f, 0.5f, 0f, Mathf.PI / 2f, out SpriteRenderer sr);
            CallUpdate(p);
            Assert.AreEqual(1f, sr.color.a, 1e-6f);
        }

        [Test]
        public void DotAlphaPulse_알파는_0으로_클램프된다()
        {
            DotAlphaPulse p = NewPulse(0.1f, 0.5f, 0f, -Mathf.PI / 2f, out SpriteRenderer sr);
            CallUpdate(p);
            Assert.AreEqual(0f, sr.color.a, 1e-6f);
        }

        [Test]
        public void DotAlphaPulse_RGB는_건드리지_않는다()
        {
            DotAlphaPulse p = NewPulse(0.5f, 0.2f, 0f, 0f, out SpriteRenderer sr);
            CallUpdate(p);
            Assert.AreEqual(0.2f, sr.color.r, 1e-6f);
            Assert.AreEqual(0.4f, sr.color.g, 1e-6f);
            Assert.AreEqual(0.6f, sr.color.b, 1e-6f);
        }

        [Test]
        public void DotAlphaPulse_렌더러가_없어도_예외가_없다()
        {
            DotAlphaPulse p = NewPulse(0.5f, 0.2f, 0f, 0f, out SpriteRenderer _);
            TestReflection.SetField(p, "_renderer", null);
            Assert.DoesNotThrow(() => CallUpdate(p));
        }

        [Test]
        public void DotLightFlicker_세기는_기준값_더하기_사인이다()
        {
            DotLightFlicker f = NewFlicker(0.8f, 0.07f, 0f, Mathf.PI / 2f, out Light2D light);
            CallUpdate(f);
            Assert.AreEqual(0.87f, light.intensity, 1e-5f);
        }

        [Test]
        public void DotLightFlicker_세기는_음수가_되지_않는다()
        {
            DotLightFlicker f = NewFlicker(0.05f, 0.5f, 0f, -Mathf.PI / 2f, out Light2D light);
            CallUpdate(f);
            Assert.AreEqual(0f, light.intensity, 1e-6f);
        }

        [Test]
        public void DotLightFlicker_조명이_없어도_예외가_없다()
        {
            DotLightFlicker f = NewFlicker(0.8f, 0.07f, 0f, 0f, out Light2D _);
            TestReflection.SetField(f, "_light", null);
            Assert.DoesNotThrow(() => CallUpdate(f));
        }

        [Test]
        public void DotWave_배열_길이가_다르면_짧은_쪽까지만_더한다()
        {
            float v = DotWave.Evaluate(1f, new[] { 0.5f, 9f }, new[] { 0f }, new[] { Mathf.PI / 2f, 0f }, 0f);
            Assert.AreEqual(1.5f, v, 1e-5f);
        }

        [Test]
        public void DotWave_주기만큼_지나면_같은_값이다()
        {
            float[] a = { 0.3f };
            float[] w = { 2f };
            float[] p = { 0.7f };
            float v0 = DotWave.Evaluate(0.5f, a, w, p, 1.3f);
            float v1 = DotWave.Evaluate(0.5f, a, w, p, 1.3f + Mathf.PI);
            Assert.AreEqual(v0, v1, 1e-4f);
        }

        [Test]
        public void DotLightingSettings_Awake가_격자_원점_전역값을_설정한다()
        {
            Vector4 saved = Shader.GetGlobalVector("_DotGridOrigin");
            try
            {
                _go = new GameObject("settings");
                DotLightingSettings s = _go.AddComponent<DotLightingSettings>();
                TestReflection.SetField(s, "_gridOrigin", new Vector2(240f, 135f));
                CallAwake(s);
                Vector4 v = Shader.GetGlobalVector("_DotGridOrigin");
                Assert.AreEqual(240f, v.x, 1e-4f);
                Assert.AreEqual(135f, v.y, 1e-4f);
            }
            finally
            {
                Shader.SetGlobalVector("_DotGridOrigin", saved);
            }
        }

        [Test]
        public void DotLightingSettings_기본_원점은_배틀_화면_중앙이다()
        {
            Vector4 saved = Shader.GetGlobalVector("_DotGridOrigin");
            try
            {
                _go = new GameObject("settings");
                DotLightingSettings s = _go.AddComponent<DotLightingSettings>();
                CallAwake(s);
                Vector4 v = Shader.GetGlobalVector("_DotGridOrigin");
                Assert.AreEqual(DotStageMapping.BattleScreenCenterDot.x, v.x, 1e-4f);
                Assert.AreEqual(DotStageMapping.BattleScreenCenterDot.y, v.y, 1e-4f);
            }
            finally
            {
                Shader.SetGlobalVector("_DotGridOrigin", saved);
            }
        }

        [Test]
        public void DotOrbitRing_같은_링의_점들은_위상이_균등하게_벌어진다()
        {
            //# N=4, 위상 0, ω 0 → 0°, 90°, 180°, 270° → (rx,0) (0,ry) (−rx,0) (0,−ry)
            Assert.AreEqual(new Vector2Int(10, 0), DotOrbitRing.DotOffset(0, 4, 0f, 0f, 0f, 10f, 4f));
            Assert.AreEqual(new Vector2Int(0, 4), DotOrbitRing.DotOffset(1, 4, 0f, 0f, 0f, 10f, 4f));
            Assert.AreEqual(new Vector2Int(-10, 0), DotOrbitRing.DotOffset(2, 4, 0f, 0f, 0f, 10f, 4f));
            Assert.AreEqual(new Vector2Int(0, -4), DotOrbitRing.DotOffset(3, 4, 0f, 0f, 0f, 10f, 4f));
        }

        [Test]
        public void DotOrbitRing_개수가_0이하면_원점이다()
        {
            Assert.AreEqual(Vector2Int.zero, DotOrbitRing.DotOffset(0, 0, 1f, 1f, 0f, 10f, 4f));
            Assert.AreEqual(Vector2Int.zero, DotOrbitRing.DotOffset(0, -3, 1f, 1f, 0f, 10f, 4f));
        }

        [Test]
        public void DotOrbitRing_시간이_지나면_ω곱만큼_회전한다()
        {
            //# ω 1 rad/s, t = π/2 → 0번 점이 90° 위치
            Vector2Int o = DotOrbitRing.DotOffset(0, 1, Mathf.PI / 2f, 1f, 0f, 10f, 4f);
            Assert.AreEqual(0, o.x);
            Assert.AreEqual(4, o.y);
        }

        [Test]
        public void DotOrbitRing_음수_좌표도_floor_더하기_0_5로_반올림한다()
        {
            //# cos π · 2.5 = −2.5 → floor(−2.5 + 0.5) = −2
            Assert.AreEqual(-2, DotOrbitRing.DotOffset(0, 1, 0f, 0f, Mathf.PI, 2.5f, 0f).x);
        }
    }
}
