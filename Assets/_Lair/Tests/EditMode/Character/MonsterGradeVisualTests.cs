using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;
using Lair.Tests.EditMode;
using Lair.Tests.Helpers;

namespace Lair.Tests.Character
{
    //# MonsterGradeVisual(monster-grades §2·§9, 게이트 T1·T9·T10·T11) - 풀 Pop/Push 리셋, 사망, 이펙트 상한 초과 생략, 후광 폭 clamp, 맥동.
    //# EditMode 라 Awake/OnEnable/OnDisable/Update 는 자동 호출되지 않는다 - 리플렉션으로 직접 부르고 private 필드는 TestReflection 으로 주입한다.
    //# 몸 머티리얼은 Monster2DSprite 셰이더 인스턴스를 직접 만든다(.material 접근이 EditMode 에서 에러 로그를 남기므로 ignoreFailingMessages 로 허용).
    public class MonsterGradeVisualTests
    {
        private const float CellWorld = 0.7205f;
        //# ApplyFx 는 cellWorld x GradeFxSheet.RefRatio(0.8) / 루트 스케일(1)로 배치한다.
        private const float RefCellWorld = CellWorld * 0.8f;
        private static readonly Vector3 BaseVisualScale = new Vector3(2f, 2f, 1f);
        private static readonly Vector3 BaseSigilScale = new Vector3(1.5f, 1.5f, 1f);

        private readonly List<Object> _objects = new List<Object>();
        private GameObject _root;
        private SpriteRenderer _body;
        private Material _originalMaterial;
        private Transform _visualRoot;
        private Transform _sigil;
        private GradeFxSheet _aura;
        private GradeFxSheet[] _tornados;
        private GradeFxSheet _sunburst;
        private MonsterGradeVisual _visual;
        private FakeHealth _health;

        [SetUp]
        public void SetUp()
        {
            LogAssert.ignoreFailingMessages = true;
            GradeFxBudget.Reset();
            _root = new GameObject("GradeMonster");
            _objects.Add(_root);

            GameObject visualGo = new GameObject("Visual2D");
            visualGo.transform.SetParent(_root.transform, false);
            visualGo.transform.localScale = BaseVisualScale;
            _visualRoot = visualGo.transform;
            _body = visualGo.AddComponent<SpriteRenderer>();
            Shader shader = Shader.Find("Lair/Monster2DSprite");
            Assert.IsNotNull(shader, "Lair/Monster2DSprite 셰이더 - 이름 변경 감지");
            _originalMaterial = new Material(shader);
            _objects.Add(_originalMaterial);
            _body.sharedMaterial = _originalMaterial;
            Assert.IsTrue(_originalMaterial.HasProperty("_GlowWidth") && _originalMaterial.HasProperty("_Brightness"), "셰이더 속성 - 이름 변경 감지");

            GameObject sigilGo = new GameObject("AuraSigil");
            sigilGo.transform.SetParent(_root.transform, false);
            sigilGo.transform.localScale = BaseSigilScale;
            _sigil = sigilGo.transform;

            _aura = NewSheet("AuraGrade");
            _tornados = new[] { NewSheet("AuraFxTornado0"), NewSheet("AuraFxTornado1"), NewSheet("AuraFxTornado2"), NewSheet("AuraFxTornado3") };
            _sunburst = NewSheet("AuraFxSunburst");

            _visual = _root.AddComponent<MonsterGradeVisual>();
            TestReflection.SetField(_visual, "_cellWorld", CellWorld);
            TestReflection.SetField(_visual, "_body", _body);
            TestReflection.SetField(_visual, "_visualRoot", _visualRoot);
            TestReflection.SetField(_visual, "_sigil", _sigil);
            TestReflection.SetField(_visual, "_aura", _aura);
            TestReflection.SetField(_visual, "_tornados", _tornados);
            TestReflection.SetField(_visual, "_sunburst", _sunburst);

            Invoke("Awake");
            _health = new FakeHealth();
            TestReflection.SetField(_visual, "_health", _health);
            Invoke("OnEnable");
        }

        [TearDown]
        public void TearDown()
        {
            LogAssert.ignoreFailingMessages = false;
            GradeFxBudget.Reset();
            if (_body != null && _body.sharedMaterial != null && _body.sharedMaterial != _originalMaterial)
            {
                Object.DestroyImmediate(_body.sharedMaterial);
            }
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        private GradeFxSheet NewSheet(string childName)
        {
            GameObject go = new GameObject(childName);
            go.transform.SetParent(_root.transform, false);
            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            GradeFxSheet sheet = go.AddComponent<GradeFxSheet>();
            Texture2D tex = new Texture2D(4, 4);
            _objects.Add(tex);
            Sprite[] frames = new Sprite[2];
            for (int i = 0; i < frames.Length; i++)
            {
                frames[i] = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
                _objects.Add(frames[i]);
            }
            sheet.SetForTest(renderer, frames, 30f);
            go.SetActive(false);
            return sheet;
        }

        private void Invoke(string method)
        {
            MethodInfo mi = typeof(MonsterGradeVisual).GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, $"MonsterGradeVisual.{method} 존재 - 시그니처 변경 감지");
            mi.Invoke(_visual, null);
        }

        //# .material 접근 에러 로그는 SetUp 에서 켠 플래그가 테스트 본문까지 유지되지 않아 호출 직전에 켠다(TearDown 에서 복원).
        private void Configure(GradeFxSpec spec)
        {
            LogAssert.ignoreFailingMessages = true;
            _visual.Configure(spec);
        }

        private Material BodyMaterial => _body.sharedMaterial;

        private static Grade_FxRow Tornado(float x, int frameOffset)
        {
            return new Grade_FxRow { grade = 4, index = 0, kind = GradeFxDatabase.KindTornado, offsetX = x, offsetY = 0.0875f, scale = 0.75f, opacity = 0.95f, frameOffset = frameOffset };
        }

        private static GradeFxSpec Spec(float sizeMul, bool glow, float glowWidthCell, bool aura, int tornados, bool sunburst, bool pulse)
        {
            GradeFxSpec spec = new GradeFxSpec { SizeMul = sizeMul, GlowColor = Color.red };
            if (glow)
            {
                spec.HasGlow = true;
                spec.GlowWidthCell = glowWidthCell;
            }
            if (aura)
            {
                spec.HasAura = true;
                spec.AuraOpacity = 0.85f;
                spec.AuraScale = 1.7f;
                spec.AuraOffsetY = 0.075f;
            }
            for (int i = 0; i < tornados; i++)
            {
                spec.Fx.Add(Tornado(i % 2 == 0 ? -0.325f : 0.325f, i * 7));
            }
            if (sunburst)
            {
                spec.Fx.Add(new Grade_FxRow { grade = 5, index = 9, kind = GradeFxDatabase.KindSunburst, offsetY = 0.43f, scale = 2.4f, opacity = 0.85f });
            }
            if (pulse)
            {
                spec.Pulse = new Grade_PulseRow { grade = 5, brightnessMin = 1.15f, brightnessMax = 1.45f, saturationMin = 1.5f, saturationMax = 1.8f, periodSec = 1.2f };
            }
            return spec;
        }

        private static GradeFxSpec Spec4() => Spec(1.225f, true, 0.04375f, true, 2, false, false);
        private static GradeFxSpec Spec5() => Spec(1.3f, true, 0.075f, true, 4, true, true);

        //# 남은 슬롯 수 - 가득 찰 때까지 획득했다가 전부 반납(측정이 상태를 바꾸지 않게).
        private static int RemainingTornado()
        {
            int n = 0;
            while (GradeFxBudget.TryAcquireTornado())
            {
                n++;
            }
            for (int i = 0; i < n; i++)
            {
                GradeFxBudget.ReleaseTornado();
            }
            return n;
        }

        private static int RemainingSunburst()
        {
            int n = 0;
            while (GradeFxBudget.TryAcquireSunburst())
            {
                n++;
            }
            for (int i = 0; i < n; i++)
            {
                GradeFxBudget.ReleaseSunburst();
            }
            return n;
        }

        private static void Fill(System.Func<bool> acquire, int count)
        {
            for (int i = 0; i < count; i++)
            {
                Assert.IsTrue(acquire(), "사전 채우기");
            }
        }

        //# ───────── 정상 / 1등급 ─────────

        [Test]
        public void 한등급_사양은_이펙트를_켜지_않고_머티리얼도_복제하지_않는다()
        {
            Configure(Spec(1f, false, 0f, false, 0, false, false));

            Assert.IsFalse(_visual.AuraOnForTest);
            Assert.AreEqual(0, _visual.TornadoOnForTest);
            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(1f, _visual.SizeMul);
            Assert.AreEqual(BaseVisualScale, _visualRoot.localScale, "크기 x1.0");
            Assert.AreSame(_originalMaterial, BodyMaterial, "1등급은 .material 접근 없음(인스턴스 복제 0)");
            Assert.IsFalse(_aura.PlayingForTest || _sunburst.PlayingForTest);
        }

        [Test]
        public void Configure_null_은_상태를_바꾸지_않는다()
        {
            Configure(Spec4());

            Configure(null);

            Assert.IsTrue(_visual.AuraOnForTest, "이전 적용 유지");
            Assert.AreEqual(1.225f, _visual.SizeMul);
        }

        [Test]
        public void 정예_사양은_크기_오라_기둥_2개를_켜고_후광은_켜지_않는다()
        {
            Configure(Spec4());

            Assert.AreEqual(1.225f, _visual.SizeMul);
            Assert.AreEqual(BaseVisualScale * 1.225f, _visualRoot.localScale, "Visual2D = 기본 스케일 x sizeMul(T1)");
            Assert.AreEqual(BaseSigilScale * 1.225f, _sigil.localScale, "AuraSigil 도 x sizeMul");
            Assert.IsTrue(_visual.AuraOnForTest);
            Assert.AreEqual(2, _visual.TornadoOnForTest);
            Assert.IsTrue(_aura.PlayingForTest);
            Assert.AreEqual(0f, _aura.AlphaForTest, 0.0001f, "등장 페이드는 알파 0 에서 시작(T9)");
            Assert.IsTrue(_tornados[0].PlayingForTest && _tornados[1].PlayingForTest);
            Assert.IsFalse(_tornados[2].PlayingForTest || _sunburst.PlayingForTest);
            Assert.AreEqual(1, GradeFxBudget.AuraCountForTest);
        }

        [Test]
        public void 이펙트_위치는_셀_비율_x_cellWorld_x_RefRatio_이고_sizeMul_을_곱하지_않는다()
        {
            Configure(Spec4());

            Assert.AreEqual(-0.325f * RefCellWorld, _tornados[0].transform.localPosition.x, 0.0001f, "x = offsetX x cellWorld (x1.225 아님, 루트 스케일 1)");
            Assert.AreEqual(0.0875f * RefCellWorld, _tornados[0].transform.localPosition.y, 0.0001f);
            Assert.AreEqual(0.325f * RefCellWorld, _tornados[1].transform.localPosition.x, 0.0001f);
            Assert.AreEqual(0.075f * RefCellWorld, _aura.transform.localPosition.y, 0.0001f, "오라 y = offsetY x cellWorld");
        }

        [Test]
        public void 군주_사양은_오라_기둥_4개_후광_맥동을_모두_켠다()
        {
            Configure(Spec5());

            Assert.IsTrue(_visual.AuraOnForTest);
            Assert.AreEqual(4, _visual.TornadoOnForTest);
            Assert.IsTrue(_sunburst.PlayingForTest);
            foreach (GradeFxSheet tornado in _tornados)
            {
                Assert.IsTrue(tornado.PlayingForTest);
            }
        }

        [Test]
        public void Configure_를_두번_호출해도_슬롯_누수와_크기_이중_곱셈이_없다()
        {
            Configure(Spec5());
            Configure(Spec5());

            Assert.AreEqual(BaseVisualScale * 1.3f, _visualRoot.localScale, "x1.3 이지 x1.69 가 아님");
            Assert.AreEqual(BaseSigilScale * 1.3f, _sigil.localScale);
            Assert.AreEqual(1, GradeFxBudget.AuraCountForTest, "재적용 전에 반납");
            Assert.AreEqual(24 - 4, RemainingTornado());
            Assert.AreEqual(8 - 1, RemainingSunburst());
        }

        [Test]
        public void 낮은_등급으로_다시_Configure_하면_이전_높은_등급의_이펙트가_꺼진다()
        {
            Configure(Spec5());

            Configure(Spec(1.075f, true, 0.0125f, true, 0, false, false));

            Assert.AreEqual(0, _visual.TornadoOnForTest);
            Assert.IsFalse(_sunburst.PlayingForTest);
            Assert.IsFalse(_tornados[0].PlayingForTest);
            Assert.AreEqual(24, RemainingTornado());
            Assert.AreEqual(8, RemainingSunburst());
            Assert.AreEqual(BaseVisualScale * 1.075f, _visualRoot.localScale);
        }

        //# ───────── 이펙트 상한 (T10) ─────────

        [Test]
        public void 기둥_상한에_가까우면_가능한_만큼만_켜고_나머지는_생략한다_오라와_크기는_유지()
        {
            Fill(GradeFxBudget.TryAcquireTornado, 23);

            Configure(Spec4());

            Assert.AreEqual(1, _visual.TornadoOnForTest, "24 상한 - 23 사용 중 → 1개만");
            Assert.IsTrue(_tornados[0].PlayingForTest);
            Assert.IsFalse(_tornados[1].PlayingForTest);
            Assert.IsTrue(_visual.AuraOnForTest, "오라는 유지");
            Assert.AreEqual(BaseVisualScale * 1.225f, _visualRoot.localScale, "크기는 유지");
        }

        [Test]
        public void 기둥_슬롯을_반납하면_다음_몬스터가_다시_켠다()
        {
            Fill(GradeFxBudget.TryAcquireTornado, 23);
            Configure(Spec4());
            Assert.AreEqual(0, RemainingTornado(), "상한 도달");

            Invoke("OnDisable");

            Assert.AreEqual(1, RemainingTornado(), "이 몬스터가 쓰던 1슬롯이 반납돼 사전 채운 23 만 남음");
            Assert.IsFalse(_visual.AuraOnForTest);
            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);

            Invoke("OnEnable");
            Configure(Spec4());

            Assert.AreEqual(1, _visual.TornadoOnForTest, "반납 후 재사용된 몬스터가 다시 슬롯 1개를 얻음(2번째는 상한)");
        }

        [Test]
        public void 오라_상한이_차면_오라만_생략하고_기둥과_크기는_유지한다()
        {
            Fill(GradeFxBudget.TryAcquireAura, GradeFxBudget.AuraCap);

            Configure(Spec4());

            Assert.IsFalse(_visual.AuraOnForTest);
            Assert.IsFalse(_aura.PlayingForTest);
            Assert.AreEqual(GradeFxBudget.AuraCap, GradeFxBudget.AuraCountForTest, "생략한 몬스터는 슬롯을 늘리지 않음");
            Assert.AreEqual(2, _visual.TornadoOnForTest);
            Assert.AreEqual(BaseVisualScale * 1.225f, _visualRoot.localScale);
        }

        [Test]
        public void 후광_상한이_차면_후광만_생략하고_오라와_기둥은_유지한다()
        {
            Fill(GradeFxBudget.TryAcquireSunburst, GradeFxBudget.SunburstCap);

            Configure(Spec5());

            Assert.IsFalse(_sunburst.PlayingForTest);
            Assert.IsTrue(_visual.AuraOnForTest);
            Assert.AreEqual(4, _visual.TornadoOnForTest);
            Assert.AreEqual(0, RemainingSunburst(), "생략한 몬스터가 슬롯을 늘리지 않음");
        }

        [Test]
        public void 생략한_후광_몬스터가_Disable_돼도_다른_몬스터의_후광_슬롯을_반납하지_않는다()
        {
            Fill(GradeFxBudget.TryAcquireSunburst, GradeFxBudget.SunburstCap);
            Configure(Spec5());

            Invoke("OnDisable");

            Assert.AreEqual(0, RemainingSunburst(), "상한 8 이 그대로 점유 중");
        }

        [Test]
        public void 기둥_슬롯이_부족하거나_시트가_없으면_예외없이_건너뛴다()
        {
            TestReflection.SetField(_visual, "_tornados", new[] { _tornados[0] });
            Assert.DoesNotThrow(() => Configure(Spec(1.225f, false, 0f, false, 4, false, false)));
            Assert.AreEqual(1, _visual.TornadoOnForTest, "시트 슬롯 1개 → 1개만");

            TestReflection.SetField(_visual, "_tornados", null);
            Assert.DoesNotThrow(() => Configure(Spec(1.225f, false, 0f, false, 4, false, false)));
            Assert.AreEqual(0, _visual.TornadoOnForTest);
            Assert.AreEqual(24, RemainingTornado());
        }

        [Test]
        public void 오라_시트가_없으면_슬롯을_쓰지_않는다()
        {
            TestReflection.SetField(_visual, "_aura", null);

            Configure(Spec4());

            Assert.IsFalse(_visual.AuraOnForTest);
            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
        }

        //# ───────── 풀 Pop/Push 리셋 (T9) ─────────

        [Test]
        public void Push_OnDisable_은_슬롯을_반납하고_모든_시트를_멈춘다()
        {
            Configure(Spec5());

            Invoke("OnDisable");

            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(24, RemainingTornado());
            Assert.AreEqual(8, RemainingSunburst());
            Assert.IsFalse(_aura.PlayingForTest || _sunburst.PlayingForTest);
            foreach (GradeFxSheet tornado in _tornados)
            {
                Assert.IsFalse(tornado.PlayingForTest);
            }
        }

        [Test]
        public void Pop_OnEnable_은_크기_후광_밝기_맥동을_기본값으로_되돌린다()
        {
            Configure(Spec5());
            Invoke("OnDisable");

            Invoke("OnEnable");

            Assert.AreEqual(BaseVisualScale, _visualRoot.localScale);
            Assert.AreEqual(BaseSigilScale, _sigil.localScale);
            Assert.AreEqual(1f, _visual.SizeMul);
            Assert.AreEqual(0f, BodyMaterial.GetFloat("_GlowWidth"), "후광 폭 0");
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Brightness"));
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Saturation"));
        }

        [Test]
        public void Pop_후_맥동은_정지_상태라_Update_가_밝기를_바꾸지_않는다()
        {
            Configure(Spec5());
            Invoke("OnDisable");
            Invoke("OnEnable");

            Invoke("Update");

            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Brightness"), "다음 Configure 전까지 맥동 없음");
        }

        [Test]
        public void OnDisable_이후_사망_이벤트가_와도_이중_반납하지_않는다()
        {
            Configure(Spec5());
            Invoke("OnDisable");
            Fill(GradeFxBudget.TryAcquireAura, 3);

            _health.TakeDamage(_health.Max);

            Assert.AreEqual(3, GradeFxBudget.AuraCountForTest, "구독 해제 후 사망 이벤트는 무시");
        }

        [Test]
        public void 같은_인스턴스를_Disable_Enable_Configure_순환해도_슬롯이_새지_않는다()
        {
            for (int i = 0; i < 5; i++)
            {
                Configure(Spec5());
                Invoke("OnDisable");
                Invoke("OnEnable");
            }

            Assert.AreEqual(0, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(24, RemainingTornado());
            Assert.AreEqual(8, RemainingSunburst());
        }

        //# ───────── 사망 (T11) ─────────

        [Test]
        public void 사망_순간_이펙트가_숨겨지고_슬롯이_반납되며_맥동이_멈춘다()
        {
            Fill(GradeFxBudget.TryAcquireAura, 5);
            Configure(Spec5());
            Invoke("Update");

            _health.TakeDamage(_health.Max);

            Assert.IsFalse(_visual.AuraOnForTest);
            Assert.AreEqual(0, _visual.TornadoOnForTest);
            Assert.IsFalse(_aura.PlayingForTest || _sunburst.PlayingForTest);
            Assert.AreEqual(5, GradeFxBudget.AuraCountForTest, "자기 슬롯만 반납");
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Brightness"), "맥동 정지 - 밝기 1.0");
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Saturation"), "맥동 정지 - 채도 1.0");

            Invoke("Update");
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Brightness"), "사망 후 Update 가 다시 쓰지 않음");
        }

        [Test]
        public void 사망_후_풀로_반환돼_OnDisable_이_불려도_슬롯을_이중_반납하지_않는다()
        {
            Fill(GradeFxBudget.TryAcquireAura, 5);
            Configure(Spec5());
            _health.TakeDamage(_health.Max);

            Invoke("OnDisable");

            Assert.AreEqual(5, GradeFxBudget.AuraCountForTest);
            Assert.AreEqual(24, RemainingTornado(), "기둥도 정확히 24 로 복귀");
            Assert.AreEqual(8, RemainingSunburst());
        }

        [Test]
        public void 사망한_몬스터의_몸_후광_폭은_리셋하지_않고_몸_알파를_따라가게_둔다()
        {
            Configure(Spec5());
            float width = BodyMaterial.GetFloat("_GlowWidth");
            Assert.Greater(width, 0f);

            _health.TakeDamage(_health.Max);

            Assert.AreEqual(width, BodyMaterial.GetFloat("_GlowWidth"), 0.0001f, "후광은 사망 클립 동안 몸 알파로 사라짐(§9.5)");
        }

        //# ───────── 후광 폭 clamp / 맥동 ─────────

        private void SetBodySprite(float ppu)
        {
            Texture2D tex = new Texture2D(4, 4);
            _objects.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0f), ppu);
            _objects.Add(sprite);
            _body.sprite = sprite;
        }

        [Test]
        public void 군주_후광_폭은_셀_가장자리_여백_8_texel_로_제한된다()
        {
            SetBodySprite(266.483f);

            Configure(Spec5());

            Assert.AreEqual(8f, BodyMaterial.GetFloat("_GlowWidth"), 0.0001f, "0.075 x 0.7205 x 266.483 ÷ 1.3 = 11.08 → 8 로 clamp");
            Assert.AreEqual(0.5f, BodyMaterial.GetFloat("_GlowInner"), 0.0001f);
        }

        [Test]
        public void 견습_후광_폭은_clamp_없이_환산_값을_쓴다()
        {
            SetBodySprite(266.483f);

            Configure(Spec(1.075f, true, 0.0125f, true, 0, false, false));

            Assert.AreEqual(0.0125f * CellWorld * 266.483f / 1.075f, BodyMaterial.GetFloat("_GlowWidth"), 0.001f, "약 2.23 texel");
        }

        [Test]
        public void 스프라이트가_없으면_기본_PPU_100_으로_환산한다()
        {
            _body.sprite = null;

            Configure(Spec5());

            Assert.AreEqual(0.075f * CellWorld * 100f / 1.3f, BodyMaterial.GetFloat("_GlowWidth"), 0.001f);
        }

        [Test]
        public void 후광_색은_사양의_종족_색으로_들어간다()
        {
            GradeFxSpec spec = Spec4();
            spec.GlowColor = new Color(0.25f, 0.5f, 0.75f, 1f);

            Configure(spec);

            Color applied = BodyMaterial.GetColor("_GlowColor");
            Assert.AreEqual(0.25f, applied.r, 0.001f);
            Assert.AreEqual(0.5f, applied.g, 0.001f);
            Assert.AreEqual(0.75f, applied.b, 0.001f);
        }

        [Test]
        public void 맥동은_밝기_채도_범위_안에서만_움직인다()
        {
            Configure(Spec5());

            for (int i = 0; i < 30; i++)
            {
                Invoke("Update");
                float brightness = BodyMaterial.GetFloat("_Brightness");
                float saturation = BodyMaterial.GetFloat("_Saturation");
                Assert.GreaterOrEqual(brightness, 1.15f - 0.0001f);
                Assert.LessOrEqual(brightness, 1.45f + 0.0001f);
                Assert.GreaterOrEqual(saturation, 1.5f - 0.0001f);
                Assert.LessOrEqual(saturation, 1.8f + 0.0001f);
            }
        }

        [Test]
        public void 후광_없이_맥동만_있는_데이터도_맥동하고_후광_폭은_0_이다()
        {
            Configure(Spec(1.3f, false, 0f, false, 0, false, true));

            Invoke("Update");

            Assert.AreEqual(0f, BodyMaterial.GetFloat("_GlowWidth"), "후광 행 없음 → 폭 0");
            Assert.GreaterOrEqual(BodyMaterial.GetFloat("_Brightness"), 1.15f - 0.0001f);
            Assert.AreNotSame(_originalMaterial, BodyMaterial, "맥동은 머티리얼 인스턴스에 쓴다");
        }

        [Test]
        public void 맥동_없는_사양은_Update_가_밝기를_건드리지_않는다()
        {
            Configure(Spec4());

            Invoke("Update");

            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Brightness"));
            Assert.AreEqual(1f, BodyMaterial.GetFloat("_Saturation"));
        }

        [Test]
        public void 몸_렌더러가_없어도_Configure_는_예외없이_크기와_이펙트를_적용한다()
        {
            TestReflection.SetField(_visual, "_body", null);

            Assert.DoesNotThrow(() => Configure(Spec5()));

            Assert.AreEqual(BaseVisualScale * 1.3f, _visualRoot.localScale);
            Assert.IsTrue(_visual.AuraOnForTest);
        }
    }
}
