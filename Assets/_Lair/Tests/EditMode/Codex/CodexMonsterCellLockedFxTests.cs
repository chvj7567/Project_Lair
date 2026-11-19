using System.Collections.Generic;
using System.Reflection;
using Lair.Character;
using Lair.Data;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 도감 미해금 칸 등급 이펙트 실루엣(codex-locked-grade-fx-silhouette §5.1 CL1~CL4) - 이펙트 Image 의 머티리얼·색·활성 구성만 본다(내부 구현 비결합).
    //# 셀 Awake 가 이펙트 머티리얼을 1회 캐싱하므로 루트를 비활성으로 조립한 뒤 활성화한다. CL5(상세 미리보기)는 CHMResource/CHMPool/RenderTexture 의존이라 EditMode 불가.
    public class CodexMonsterCellLockedFxTests
    {
        private const float Eps = 1e-3f;

        private GameObject _root;
        private CodexMonsterCell _cell;
        private Image _aura;
        private Image _halo;
        private Image[] _pillars;
        private Material _fxMaterial;
        private CodexRaceArt _art;
        private GradeFxDatabase _prevDb;
        private int _grade = 5;
        private readonly List<Object> _garbage = new List<Object>();

        [SetUp]
        public void SetUp()
        {
            _prevDb = GradeFxDatabase.Current;
            GradeFxDatabase.Current = BuildDb();
            GradeFxBudget.Reset();

            _fxMaterial = new Material(Shader.Find("UI/Default"));
            _garbage.Add(_fxMaterial);
            _art = new CodexRaceArt
            {
                Race = EMonster.Wisp,
                Aura = new[] { MakeSprite() },
                Pillar = new[] { MakeSprite() },
                Halo = new[] { MakeSprite() },
            };

            _root = new GameObject("CodexMonsterCellLockedFxTest");
            _root.SetActive(false);
            _cell = _root.AddComponent<CodexMonsterCell>();
            _aura = MakeFxImage("aura");
            _halo = MakeFxImage("halo");
            _pillars = new[] { MakeFxImage("p0"), MakeFxImage("p1"), MakeFxImage("p2"), MakeFxImage("p3") };
            SetField("_aura", _aura);
            SetField("_halo", _halo);
            SetField("_pillars", _pillars);
            _root.SetActive(true);
        }

        [TearDown]
        public void TearDown()
        {
            GradeFxDatabase.Current = _prevDb;
            GradeFxBudget.Reset();
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
            foreach (Object o in _garbage)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }
            _garbage.Clear();
        }

        //# ---------- CL1 ----------

        [TestCase(2, 1, 0, 0)]
        [TestCase(3, 1, 0, 0)]
        [TestCase(4, 1, 0, 2)]
        [TestCase(5, 1, 1, 4)]
        public void 미해금_2에서5등급_칸은_해금칸과_같은_구성을_검은_실루엣으로_켠다(int grade, int aura, int halo, int pillars)
        {
            _cell.Bind(Data(grade, open: false));

            AssertConfig(aura, halo, pillars);
            AssertActiveFx(silhouette: true);
        }

        [TestCase(2, 1, 0, 0)]
        [TestCase(4, 1, 0, 2)]
        [TestCase(5, 1, 1, 4)]
        public void 해금_칸은_같은_구성을_원본_머티리얼_흰색으로_켠다(int grade, int aura, int halo, int pillars)
        {
            _cell.Bind(Data(grade, open: true));

            AssertConfig(aura, halo, pillars);
            AssertActiveFx(silhouette: false);
        }

        [Test]
        public void 미해금_1등급_칸은_이펙트가_없다()
        {
            _cell.Bind(Data(1, open: false));

            AssertConfig(0, 0, 0);
        }

        //# ---------- CL2 ----------

        [Test]
        public void 미해금_칸_20개를_바인딩해도_슬롯을_쓰지_않고_후보도_아니다()
        {
            int before = GradeFxBudget.AuraCountForTest;
            for (int grade = 1; grade <= 5; ++grade)
            {
                for (int tier = 0; tier <= 3; ++tier)
                {
                    CodexMonsterCellData data = Data(grade, open: false);
                    data.Model.Tier = tier;
                    _cell.Bind(data);
                    Assert.IsFalse(_cell.IsFxCandidate, $"g{grade} t{tier} 후보 아님");
                }
            }

            Assert.AreEqual(before, GradeFxBudget.AuraCountForTest, "Aura 슬롯 카운터 변화 0");
        }

        //# ---------- CL3 ----------

        [Test]
        public void 풀_재사용_미해금에서_해금으로_재바인딩하면_실루엣_잔상이_없다()
        {
            _cell.Bind(Data(5, open: false));
            _cell.Bind(Data(5, open: true));

            AssertActiveFx(silhouette: false);
        }

        [Test]
        public void 풀_재사용_해금에서_미해금으로_재바인딩하면_실루엣이다()
        {
            _cell.Bind(Data(5, open: true));
            _cell.Bind(Data(5, open: false));

            AssertActiveFx(silhouette: true);
        }

        [Test]
        public void 풀_재사용_미해금에서_더_낮은_등급_해금으로_바뀌어도_켜진_이펙트는_원본이다()
        {
            _cell.Bind(Data(5, open: false));
            _cell.Bind(Data(2, open: true));

            AssertConfig(1, 0, 0);
            AssertActiveFx(silhouette: false);
        }

        [Test]
        public void 비활성_재활성_후에도_상태가_유지된다()
        {
            _cell.Bind(Data(4, open: false));
            _root.SetActive(false);
            _root.SetActive(true);
            AssertActiveFx(silhouette: true);

            _cell.Bind(Data(4, open: true));
            _root.SetActive(false);
            _root.SetActive(true);
            AssertActiveFx(silhouette: false);
        }

        //# ---------- CL4 ----------

        [Test]
        public void 해금연출_실루엣_알파가_단조_감소해_경계에서_가산_머티리얼로_1회_교체된다()
        {
            CodexConfigRow cfg = new CodexConfigRow();
            _cell.Bind(UnlockData(5, cfg));
            AssertActiveFx(silhouette: true);

            float[] times = { 0f, 0.35f, 0.7f, 1.05f, 1.39f };
            float prev = float.MaxValue;
            foreach (float t in times)
            {
                Tick(t);
                Assert.AreNotSame(_fxMaterial, _aura.material, $"t={t} 아직 실루엣 머티리얼");
                Assert.Less(_aura.color.a, prev, $"t={t} 알파 감소");
                Assert.AreEqual(0f, _aura.color.r, Eps);
                prev = _aura.color.a;
            }

            Tick(cfg.cellFxFadeDelaySec);
            Assert.AreSame(_fxMaterial, _aura.material, "경계에서 가산 머티리얼");
            Assert.AreEqual(0f, _aura.color.a, Eps, "교체 순간 알파 0");

            Tick(cfg.cellFxFadeDelaySec + cfg.cellFxFadeSec * 0.5f);
            Assert.AreEqual(0.5f, _aura.color.a, Eps);
            Tick(cfg.cellFxFadeDelaySec + cfg.cellFxFadeSec);
            AssertActiveFx(silhouette: false);
        }

        [Test]
        public void 해금연출_머티리얼_교체는_한번만_일어난다()
        {
            CodexConfigRow cfg = new CodexConfigRow();
            _cell.Bind(UnlockData(4, cfg));
            Tick(cfg.cellFxFadeDelaySec);
            Assert.AreSame(_fxMaterial, _aura.material);

            //# 교체가 다시 일어나면 센티널이 원본으로 덮인다.
            Material sentinel = new Material(Shader.Find("UI/Default"));
            _garbage.Add(sentinel);
            _aura.material = sentinel;
            Tick(cfg.cellFxFadeDelaySec + 0.1f);
            Tick(cfg.cellFxFadeDelaySec + 0.3f);

            Assert.AreSame(sentinel, _aura.material, "교체 1회 - 이후 재설정 없음");
        }

        [Test]
        public void 해금연출_지연이_0이면_시작_즉시_교체한다()
        {
            CodexConfigRow cfg = new CodexConfigRow { cellFxFadeDelaySec = 0f };
            _cell.Bind(UnlockData(5, cfg));

            Tick(0f);

            Assert.AreSame(_fxMaterial, _aura.material);
        }

        [Test]
        public void 해금연출_스태거_대기_중에는_실루엣_상태를_유지한다()
        {
            _cell.Bind(UnlockData(5, new CodexConfigRow()));

            Tick(-0.5f);

            AssertActiveFx(silhouette: true);
        }

        [Test]
        public void 해금연출_중단_후_재바인딩하면_해금_상태로_복구된다()
        {
            CodexConfigRow cfg = new CodexConfigRow();
            _cell.Bind(UnlockData(5, cfg));
            Tick(0.7f);

            _cell.Bind(Data(5, open: true));

            AssertActiveFx(silhouette: false);
        }

        //# ---------- helpers ----------

        //# Grade_Aura.json·Grade_Fx.json 실값과 같은 등급별 opacity - 컬러 이펙트 알파는 이 값이다.
        private const float TornadoOpacity = 0.95f;
        private const float HaloOpacity = 0.85f;

        private static float AuraOpacity(int grade)
        {
            return grade == 2 ? 0.45f : grade == 3 ? 0.65f : grade == 4 ? 0.85f : 1f;
        }

        private float ExpectedOpacity(Image image)
        {
            return image == _aura ? AuraOpacity(_grade) : image == _halo ? HaloOpacity : TornadoOpacity;
        }

        private static GradeFxDatabase BuildDb()
        {
            GradeRow[] grades = new GradeRow[5];
            float[] size = { 1f, 1.075f, 1.15f, 1.225f, 1.3f };
            for (int i = 0; i < 5; ++i)
            {
                grades[i] = new GradeRow { grade = i + 1, sizeMul = size[i], hpMul = 1f, atkMul = 1f };
            }
            Grade_AuraRow[] auras = new Grade_AuraRow[4];
            for (int g = 2; g <= 5; ++g)
            {
                auras[g - 2] = new Grade_AuraRow { grade = g, opacity = AuraOpacity(g), scale = 2f };
            }
            List<Grade_FxRow> fx = new List<Grade_FxRow>();
            for (int i = 0; i < 2; ++i)
            {
                fx.Add(new Grade_FxRow { grade = 4, index = i, kind = "Tornado", scale = 1f, opacity = TornadoOpacity });
            }
            for (int i = 0; i < 4; ++i)
            {
                fx.Add(new Grade_FxRow { grade = 5, index = i, kind = "Tornado", scale = 1f, opacity = TornadoOpacity });
            }
            fx.Add(new Grade_FxRow { grade = 5, index = 4, kind = "Sunburst", scale = 2f, opacity = HaloOpacity });
            Monster_GradeFxColorRow[] colors = new Monster_GradeFxColorRow[6];
            for (int i = 0; i < 6; ++i)
            {
                colors[i] = new Monster_GradeFxColorRow { monster = ((EMonster)i).ToString(), glowColor = "#3FCF86" };
            }
            return GradeFxDatabase.Build(grades, null, auras, null, fx.ToArray(), colors);
        }

        private CodexMonsterCellData Data(int grade, bool open)
        {
            _grade = grade;
            return new CodexMonsterCellData
            {
                Model = new CodexCellModel { Race = EMonster.Wisp, Monster = EMonster.Wisp, Index = grade, Grade = grade, Tier = 0, Open = open, Name = "t" },
                Art = _art,
                Config = new CodexConfigRow(),
                Unlock = new Grade_CodexUnlockRow { grade = grade },
                RaceColor = Color.white,
                GradeColor = Color.white,
            };
        }

        private CodexMonsterCellData UnlockData(int grade, CodexConfigRow cfg)
        {
            CodexMonsterCellData data = Data(grade, open: true);
            data.Config = cfg;
            data.ConsumeUnlock = model => true;
            return data;
        }

        private Image MakeFxImage(string name)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            go.transform.SetParent(_root.transform, false);
            Image image = go.AddComponent<Image>();
            image.material = _fxMaterial;
            return image;
        }

        private Sprite MakeSprite()
        {
            Texture2D tex = new Texture2D(4, 4);
            _garbage.Add(tex);
            Sprite sprite = Sprite.Create(tex, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            _garbage.Add(sprite);
            return sprite;
        }

        private void SetField(string name, object value)
        {
            FieldInfo field = typeof(CodexMonsterCell).GetField(name, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{name} 필드 존재(리네임 시 이 테스트가 붉게 알림)");
            field.SetValue(_cell, value);
        }

        private void Tick(float t)
        {
            MethodInfo method = typeof(CodexMonsterCell).GetMethod("TickUnlock", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(method, "TickUnlock 존재");
            method.Invoke(_cell, new object[] { t });
        }

        private void AssertConfig(int aura, int halo, int pillars)
        {
            Assert.AreEqual(aura == 1, _aura.gameObject.activeSelf, "오라");
            Assert.AreEqual(halo == 1, _halo.gameObject.activeSelf, "후광");
            int active = 0;
            foreach (Image pillar in _pillars)
            {
                if (pillar.gameObject.activeSelf)
                {
                    active++;
                }
            }
            Assert.AreEqual(pillars, active, "기둥 수");
        }

        private void AssertActiveFx(bool silhouette)
        {
            List<Image> all = new List<Image> { _aura, _halo };
            all.AddRange(_pillars);
            int checkedCount = 0;
            foreach (Image image in all)
            {
                if (image.gameObject.activeSelf == false)
                    continue;
                checkedCount++;
                if (silhouette)
                {
                    Assert.AreNotSame(_fxMaterial, image.material, $"{image.name} 가산 머티리얼 아님");
                    Assert.AreEqual(0f, image.color.r, Eps, $"{image.name} R");
                    Assert.AreEqual(0f, image.color.g, Eps, $"{image.name} G");
                    Assert.AreEqual(0f, image.color.b, Eps, $"{image.name} B");
                    Assert.AreEqual(0.7f, image.color.a, Eps, $"{image.name} A");
                }
                else
                {
                    Assert.AreSame(_fxMaterial, image.material, $"{image.name} 원본 가산 머티리얼");
                    Assert.AreEqual(1f, image.color.r, Eps, $"{image.name} R");
                    Assert.AreEqual(1f, image.color.g, Eps, $"{image.name} G");
                    Assert.AreEqual(1f, image.color.b, Eps, $"{image.name} B");
                    Assert.AreEqual(ExpectedOpacity(image), image.color.a, Eps, $"{image.name} A(등급별 opacity)");
                }
            }
            Assert.Greater(checkedCount, 0, "검증 대상 이펙트가 하나 이상 켜져 있어야 함");
        }
    }
}
