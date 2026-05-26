using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# SpawnerCooldownBar 컴포넌트 본격 스위트.
    //# FakeSpawnerProgress 테스트 더블로 ISpawnerProgress 를 주입,
    //# Update() 리플렉션 호출로 fillAmount + 색상 갱신 로직을 검증한다.
    //# _host null / _fill null 방어 케이스 망라.
    public class SpawnerCooldownBarTests
    {
        //# ISpawnerProgress 테스트 더블 — Progress 를 외부에서 자유롭게 설정 가능.
        private class FakeSpawnerProgress : ISpawnerProgress
        {
            public float Progress { get; set; }
        }

        //# SpawnerCooldownBar 의 쿨 색 (#60A5FA).
        private static readonly Color ExpectedCoolColor = new Color(0.376f, 0.647f, 0.980f, 1f);
        //# SpawnerCooldownBar 의 웜 색 (#F97316).
        private static readonly Color ExpectedWarmColor = new Color(0.976f, 0.451f, 0.086f, 1f);
        //# 색상 채널 비교 허용 오차.
        private const float ColorTolerance = 0.01f;

        private readonly List<GameObject> _spawned = new();
        private readonly List<Object> _assets = new();

        [TearDown]
        public void TearDown()
        {
            foreach (var go in _spawned)
                if (go != null) Object.DestroyImmediate(go);
            _spawned.Clear();

            foreach (var a in _assets)
                if (a != null) Object.DestroyImmediate(a);
            _assets.Clear();
        }

        //# 리플렉션 헬퍼 — 직렬화/비공개 필드 강제 주입.
        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# SpawnerCooldownBar.Update 리플렉션 직접 호출.
        private static void InvokeUpdate(SpawnerCooldownBar bar)
        {
            var mi = typeof(SpawnerCooldownBar).GetMethod("Update",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerCooldownBar.Update 메서드 존재 확인");
            mi.Invoke(bar, null);
        }

        //# SpawnerCooldownBar + Image(Fill) 을 구성하고 FakeSpawnerProgress 를 주입한 설정 생성.
        private (SpawnerCooldownBar bar, Image fill, FakeSpawnerProgress host)
            CreateBarSetup(float initialProgress = 0f)
        {
            var barGo = new GameObject("CooldownBar");
            _spawned.Add(barGo);
            var bar = barGo.AddComponent<SpawnerCooldownBar>();

            //# Image 는 RectTransform 을 요구.
            var fillGo = new GameObject("Fill", typeof(RectTransform));
            _spawned.Add(fillGo);
            var fill = fillGo.AddComponent<Image>();
            fill.fillAmount = 0f;

            var host = new FakeSpawnerProgress { Progress = initialProgress };

            SetPrivate(bar, "_fill", fill);
            SetPrivate(bar, "_host", host);

            return (bar, fill, host);
        }

        //# 색상 채널별 단언 — Color AreEqual 은 정밀도 문제 있을 수 있어 허용 오차 사용.
        private static void AssertColorEqual(Color expected, Color actual, string message)
        {
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(ColorTolerance), $"{message} (R)");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(ColorTolerance), $"{message} (G)");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(ColorTolerance), $"{message} (B)");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(ColorTolerance), $"{message} (A)");
        }

        //# ===== fillAmount 동기화 =====

        //# Progress = 0 → fillAmount = 0.
        [Test]
        public void Progress_0_fillAmount_0()
        {
            var (bar, fill, host) = CreateBarSetup(0f);
            InvokeUpdate(bar);
            Assert.AreEqual(0f, fill.fillAmount, 0.0001f, "Progress=0 → fillAmount=0");
        }

        //# Progress = 1 → fillAmount = 1.
        [Test]
        public void Progress_1_fillAmount_1()
        {
            var (bar, fill, host) = CreateBarSetup(1f);
            InvokeUpdate(bar);
            Assert.AreEqual(1f, fill.fillAmount, 0.0001f, "Progress=1 → fillAmount=1");
        }

        //# Progress = 0.5 → fillAmount = 0.5.
        [Test]
        public void Progress_0점5_fillAmount_0점5()
        {
            var (bar, fill, host) = CreateBarSetup(0.5f);
            InvokeUpdate(bar);
            Assert.AreEqual(0.5f, fill.fillAmount, 0.0001f, "Progress=0.5 → fillAmount=0.5");
        }

        //# fillAmount 는 Progress 변화에 따라 갱신됨.
        [Test]
        public void Progress_변경_후_Update_fillAmount_갱신()
        {
            var (bar, fill, host) = CreateBarSetup(0.2f);
            InvokeUpdate(bar);
            Assert.AreEqual(0.2f, fill.fillAmount, 0.0001f, "1차 Update");

            host.Progress = 0.8f;
            InvokeUpdate(bar);
            Assert.AreEqual(0.8f, fill.fillAmount, 0.0001f, "Progress 변경 후 2차 Update");
        }

        //# ===== 색상 임계값 (WarmThreshold = 0.7) =====

        //# Progress = 0.69 → Cool 색 (#60A5FA). 0.69 < 0.7 이면 쿨.
        [Test]
        public void Progress_0점69_Cool색()
        {
            var (bar, fill, host) = CreateBarSetup(0.69f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedCoolColor, fill.color, "Progress=0.69 → Cool 색");
        }

        //# Progress = 0.70 → Warm 색 (#F97316). 0.70 은 threshold 경계 → 웜.
        [Test]
        public void Progress_0점70_Warm색()
        {
            var (bar, fill, host) = CreateBarSetup(0.70f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedWarmColor, fill.color, "Progress=0.70 → Warm 색 (경계 = 웜)");
        }

        //# Progress = 1.0 → Warm 색 유지.
        [Test]
        public void Progress_1_Warm색_유지()
        {
            var (bar, fill, host) = CreateBarSetup(1.0f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedWarmColor, fill.color, "Progress=1.0 → Warm 색");
        }

        //# Progress = 0 → Cool 색.
        [Test]
        public void Progress_0_Cool색()
        {
            var (bar, fill, host) = CreateBarSetup(0f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedCoolColor, fill.color, "Progress=0 → Cool 색");
        }

        //# Progress = 0.5 → Cool 색 (threshold 미만).
        [Test]
        public void Progress_0점5_Cool색()
        {
            var (bar, fill, host) = CreateBarSetup(0.5f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedCoolColor, fill.color, "Progress=0.5 → Cool 색");
        }

        //# Cool→Warm 전환 — Progress 가 0.7 미만에서 0.7 이상으로 변경 시 색상 교체.
        [Test]
        public void 색상_Cool에서_Warm으로_전환()
        {
            var (bar, fill, host) = CreateBarSetup(0.5f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedCoolColor, fill.color, "전환 전 Cool");

            host.Progress = 0.75f;
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedWarmColor, fill.color, "전환 후 Warm");
        }

        //# Warm→Cool 전환 — Progress 가 0.7 이상에서 0.7 미만으로 변경 시 색상 교체.
        [Test]
        public void 색상_Warm에서_Cool로_전환()
        {
            var (bar, fill, host) = CreateBarSetup(0.8f);
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedWarmColor, fill.color, "전환 전 Warm");

            host.Progress = 0.3f;
            InvokeUpdate(bar);
            AssertColorEqual(ExpectedCoolColor, fill.color, "전환 후 Cool");
        }

        //# ===== 방어 케이스 — null 주입 =====

        //# _fill null 이면 Update 는 예외 없이 early return.
        [Test]
        public void _fill_null_Update_예외없음()
        {
            var barGo = new GameObject("CooldownBar");
            _spawned.Add(barGo);
            var bar = barGo.AddComponent<SpawnerCooldownBar>();

            SetPrivate(bar, "_host", new FakeSpawnerProgress { Progress = 0.5f });
            //# _fill 미주입 — null 상태.

            Assert.DoesNotThrow(() => InvokeUpdate(bar), "_fill null 이면 Update 예외 없음");
        }

        //# _host null 이면 Update 는 예외 없이 early return.
        [Test]
        public void _host_null_Update_예외없음()
        {
            var barGo = new GameObject("CooldownBar");
            _spawned.Add(barGo);
            var bar = barGo.AddComponent<SpawnerCooldownBar>();

            var fillGo = new GameObject("Fill", typeof(RectTransform));
            _spawned.Add(fillGo);
            var fill = fillGo.AddComponent<Image>();
            SetPrivate(bar, "_fill", fill);
            //# _host 미주입 — null 상태.

            Assert.DoesNotThrow(() => InvokeUpdate(bar), "_host null 이면 Update 예외 없음");
        }

        //# _fill + _host 모두 null — Update 예외 없음.
        [Test]
        public void _fill_host_모두_null_Update_예외없음()
        {
            var barGo = new GameObject("CooldownBar");
            _spawned.Add(barGo);
            var bar = barGo.AddComponent<SpawnerCooldownBar>();

            Assert.DoesNotThrow(() => InvokeUpdate(bar), "_fill/_host 모두 null → Update 예외 없음");
        }

        //# _fill null 시 fillAmount 가 변경되지 않는 것을 간접 확인 — fillGo 가 별도이므로 충돌 없음.
        [Test]
        public void _fill_null_fillAmount_변경안됨()
        {
            //# _fill 이 null 이면 내부에서 fillAmount 에 접근하지 않으므로 예외도 없고 side-effect 도 없음.
            var barGo = new GameObject("CooldownBar");
            _spawned.Add(barGo);
            var bar = barGo.AddComponent<SpawnerCooldownBar>();
            SetPrivate(bar, "_host", new FakeSpawnerProgress { Progress = 1f });

            //# 예외 없이 완료되면 테스트 통과 — _fill null guard 가 동작함을 의미.
            Assert.DoesNotThrow(() => InvokeUpdate(bar));
        }

        //# ===== Update 멱등성 — 동일 Progress 로 여러 번 Update 해도 결과 일관 =====

        [Test]
        public void 동일_Progress_다회_Update_결과_일관()
        {
            var (bar, fill, host) = CreateBarSetup(0.4f);

            for (int i = 0; i < 10; i++)
                InvokeUpdate(bar);

            Assert.AreEqual(0.4f, fill.fillAmount, 0.0001f, "동일 Progress 다회 Update — fillAmount 일관");
            AssertColorEqual(ExpectedCoolColor, fill.color, "동일 Progress 다회 Update — 색상 일관");
        }
    }
}
