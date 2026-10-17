using Lair.Tests.EditMode;
using System.Reflection;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lair.Tests.Battle
{
    //# 제단 색·결정 흔들림 동작 — 기획서 scene-2d-conversion §3.2 · §7.4 (순수 함수는 SpawnerAltar2DTests).
    public class SpawnerAltar2DBehaviourTests
    {
        //# 가짜 스포너 — ISpawnerOutputProvider 전 멤버 구현(Rule 02 §5 · §9)
        private class FakeSpawnerOutputProvider : MonoBehaviour, ISpawnerOutputProvider
        {
            public EMonster Type = EMonster.Wisp;
            public int Count = 1;

            public EMonster CurrentType => Type;
            public int OutputCount => Count;
            public event System.Action<EMonster> OnOutputTypeChanged;
            public event System.Action<int> OnOutputCountChanged;

            public void ChangeType(EMonster type)
            {
                Type = type;
                OnOutputTypeChanged?.Invoke(type);
            }

            public void ChangeCount(int count)
            {
                Count = count;
                OnOutputCountChanged?.Invoke(count);
            }

            public int TypeSubscriberCount => OnOutputTypeChanged == null ? 0 : OnOutputTypeChanged.GetInvocationList().Length;
        }

        private GameObject _spawner;
        private GameObject _altarGo;
        private FakeSpawnerOutputProvider _provider;
        private SpawnerAltar2D _altar;
        private SpriteRenderer _ringDim;
        private SpriteRenderer _ring;
        private SpriteRenderer _crystal;
        private SpriteRenderer _core;
        private SpriteRenderer _sparkle;
        private Light2D _light;

        private static SpriteRenderer NewSr(Transform parent, string name, float alpha)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.color = new Color(1f, 1f, 1f, alpha);
            return sr;
        }

        private static void Call(Component c, string method)
        {
            MethodInfo m = c.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(m, method);
            m.Invoke(c, null);
        }

        [SetUp]
        public void SetUp()
        {
            _spawner = new GameObject("spawner");
            _provider = _spawner.AddComponent<FakeSpawnerOutputProvider>();
            Assert.IsNotNull(_provider, "FakeSpawnerOutputProvider AddComponent 실패");
            _altarGo = new GameObject("altar");
            _altarGo.transform.SetParent(_spawner.transform);
            _altar = _altarGo.AddComponent<SpawnerAltar2D>();
            _ringDim = NewSr(_altarGo.transform, "ringDim", 0.6f);
            _ring = NewSr(_altarGo.transform, "ring", 0.5f);
            _crystal = NewSr(_altarGo.transform, "crystal", 0.9f);
            _core = NewSr(_altarGo.transform, "core", 1f);
            _sparkle = NewSr(_altarGo.transform, "sparkle", 0.8f);
            GameObject lightGo = new GameObject("light");
            lightGo.transform.SetParent(_altarGo.transform);
            _light = lightGo.AddComponent<Light2D>();
            TestReflection.SetField(_altar, "_ringDim", _ringDim);
            TestReflection.SetField(_altar, "_ring", _ring);
            TestReflection.SetField(_altar, "_crystal", _crystal);
            TestReflection.SetField(_altar, "_crystalCore", _core);
            TestReflection.SetField(_altar, "_sparkle", _sparkle);
            TestReflection.SetField(_altar, "_light", _light);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_spawner);
        }

        [Test]
        public void OnEnable이_초기_종_색을_즉시_적용한다()
        {
            _provider.Type = EMonster.Reaper;
            Call(_altar, "OnEnable");
            Color glow = SpeciesVisual.SpeciesGlowColor(EMonster.Reaper);
            Assert.AreEqual(glow, _altar.GlowColor);
            Assert.AreEqual(glow.r, _crystal.color.r, 1e-5f);
            Assert.AreEqual(glow.g, _ringDim.color.g, 1e-5f);
            Assert.AreEqual(glow.b, _sparkle.color.b, 1e-5f);
            Assert.AreEqual(glow.r, _light.color.r, 1e-5f);
        }

        [Test]
        public void 융합으로_종이_바뀌면_색이_즉시_바뀐다()
        {
            Call(_altar, "OnEnable");
            _provider.ChangeType(EMonster.Plague);
            Assert.AreEqual(SpeciesVisual.SpeciesGlowColor(EMonster.Plague), _altar.GlowColor);
            Assert.AreEqual(SpeciesVisual.SpeciesGlowColor(EMonster.Plague).g, _ring.color.g, 1e-5f);
        }

        [Test]
        public void 색을_바꿔도_각_스프라이트의_알파는_유지된다()
        {
            Call(_altar, "OnEnable");
            _provider.ChangeType(EMonster.Hex);
            Assert.AreEqual(0.6f, _ringDim.color.a, 1e-5f);
            Assert.AreEqual(0.5f, _ring.color.a, 1e-5f);
            Assert.AreEqual(0.9f, _crystal.color.a, 1e-5f);
            Assert.AreEqual(0.8f, _sparkle.color.a, 1e-5f);
        }

        [Test]
        public void 결정_심지는_종족색으로_물들지_않는다()
        {
            Call(_altar, "OnEnable");
            _provider.ChangeType(EMonster.Reaper);
            Assert.AreEqual(Color.white, _core.color);
        }

        [Test]
        public void 여섯_종_발광색은_모두_서로_다르다()
        {
            EMonster[] all = { EMonster.Wisp, EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Plague, EMonster.Phantom };
            for (int i = 0; i < all.Length; i++)
            {
                for (int j = i + 1; j < all.Length; j++)
                {
                    Assert.AreNotEqual(SpeciesVisual.SpeciesGlowColor(all[i]), SpeciesVisual.SpeciesGlowColor(all[j]), all[i] + " vs " + all[j]);
                }
            }
        }

        [Test]
        public void OnDisable이_구독을_해제한다()
        {
            Call(_altar, "OnEnable");
            Assert.AreEqual(1, _provider.TypeSubscriberCount);
            Call(_altar, "OnDisable");
            Assert.AreEqual(0, _provider.TypeSubscriberCount);
        }

        [Test]
        public void 해제_후_재구독하면_구독은_1개다()
        {
            Call(_altar, "OnEnable");
            Call(_altar, "OnDisable");
            Call(_altar, "OnEnable");
            Assert.AreEqual(1, _provider.TypeSubscriberCount);
        }

        [Test]
        public void 해제된_뒤_종이_바뀌어도_색은_그대로다()
        {
            Call(_altar, "OnEnable");
            Call(_altar, "OnDisable");
            Color before = _altar.GlowColor;
            _provider.ChangeType(EMonster.Phantom);
            Assert.AreEqual(before, _altar.GlowColor);
        }

        [Test]
        public void 상위_스포너가_없어도_예외없이_동작한다()
        {
            GameObject orphan = new GameObject("orphan");
            SpawnerAltar2D altar = orphan.AddComponent<SpawnerAltar2D>();
            Assert.DoesNotThrow(() => Call(altar, "OnEnable"));
            Assert.DoesNotThrow(() => Call(altar, "OnDisable"));
            Object.DestroyImmediate(orphan);
        }

        [Test]
        public void Update가_결정_심지_반짝을_흔들림_높이에_맞춰_배치한다()
        {
            Call(_altar, "OnEnable");
            Call(_altar, "Update");
            int bob = SpawnerAltar2D.CrystalBobDots(Time.time, 0f);
            Assert.AreEqual((27 - bob) / 48f, _crystal.transform.localPosition.y, 1e-5f);
            Assert.AreEqual((25 - bob) / 48f, _core.transform.localPosition.y, 1e-5f);
            Assert.AreEqual((30 - bob) / 48f, _sparkle.transform.localPosition.y, 1e-5f);
            Assert.AreEqual(3f / 48f, _sparkle.transform.localPosition.x, 1e-5f);
        }

        [Test]
        public void Update가_링_알파를_맥동_범위_안에_둔다()
        {
            Call(_altar, "OnEnable");
            Call(_altar, "Update");
            Assert.GreaterOrEqual(_ring.color.a, 0.2f - 1e-4f);
            Assert.LessOrEqual(_ring.color.a, 0.7f + 1e-4f);
        }

        [Test]
        public void Update가_조명_세기를_맥동_범위_안에_둔다()
        {
            Call(_altar, "OnEnable");
            Call(_altar, "Update");
            Assert.GreaterOrEqual(_light.intensity, 0.88f - 1e-4f);
            Assert.LessOrEqual(_light.intensity, 1.12f + 1e-4f);
        }

        [Test]
        public void CrystalBobDots_위상이_다르면_같은_시각에도_값이_갈린다()
        {
            bool differ = false;
            for (int i = 1; i < 6; i++)
            {
                if (SpawnerAltar2D.CrystalBobDots(1.0f, i * 1.0472f) != SpawnerAltar2D.CrystalBobDots(1.0f, 0f))
                {
                    differ = true;
                }
            }
            Assert.IsTrue(differ);
        }

        [Test]
        public void CrystalBobDots_사인_최소면_마이너스1이다()
        {
            //# sin = −1 → floor(−1.5 + 0.5) = −1
            float t = (3f * Mathf.PI / 2f) / 2.4f;
            Assert.AreEqual(-1, SpawnerAltar2D.CrystalBobDots(t, 0f));
        }
    }
}
