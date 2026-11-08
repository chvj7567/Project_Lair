using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Character;
using Lair.Data;
using Lair.Tests.EditMode;

namespace Lair.Tests.Character
{
    //# MonsterEnhancementVisual 의 티어 시트 주입·HP바 높이 합성(monster-tier-sheets §6·§7.3, monster-grades §4 / U-T5·U-T8, T1).
    //# HP바 높이 = (저장값 - 0.08) x sizeMul + 0.08. 시트가 있으면 시트 값, 주입자는 있는데 세트가 없거나 높이 0 이면 티어 0 표 값.
    public class MonsterEnhancementVisualProviderTests
    {
        private static readonly float[] HeightByTier = { 0.7467f, 0.809f, 0.872f, 0.934f };

        private sealed class FakeProvider : IMonsterTierSheetProvider
        {
            public readonly Dictionary<(EMonster, int), MonsterTierSheetSet> Sets = new Dictionary<(EMonster, int), MonsterTierSheetSet>();
            public readonly List<(EMonster, int)> Queries = new List<(EMonster, int)>();

            public MonsterTierSheetSet Find(EMonster baseSpecies, int tier)
            {
                Queries.Add((baseSpecies, tier));
                return Sets.TryGetValue((baseSpecies, tier), out MonsterTierSheetSet set) ? set : null;
            }
        }

        private sealed class FakeGradeVisual : IMonsterGradeVisual
        {
            public float SizeMul { get; set; } = 1f;
            public void Configure(GradeFxSpec spec) { }
        }

        private readonly List<Object> _objects = new List<Object>();
        private GameObject _root;
        private MonsterEnhancementVisual _visual;
        private MonsterHpBar _hpBar;
        private MonsterVisual2D _visual2D;
        private FakeProvider _provider;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("MonsterRoot");

            GameObject hpGo = new GameObject("HpBarWrapper");
            hpGo.transform.SetParent(_root.transform, false);
            _hpBar = hpGo.AddComponent<MonsterHpBar>();

            GameObject v2dGo = new GameObject("Visual2D");
            v2dGo.transform.SetParent(_root.transform, false);
            v2dGo.AddComponent<SpriteRenderer>();
            _visual2D = v2dGo.AddComponent<MonsterVisual2D>();

            _visual = _root.AddComponent<MonsterEnhancementVisual>();
            _visual.SetEmissionByLevelForTest(new[] { 1.5f, 1.9f, 2.3f, 2.7f, 3.2f });
            _visual.SetHpBarForTest(_hpBar);
            _visual.SetHpBarHeightByTierForTest(HeightByTier);
            TestReflection.SetField(_visual, "_visual2D", _visual2D);

            _provider = new FakeProvider();
            _visual.SetTierSheetProvider(_provider);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
            foreach (Object obj in _objects)
            {
                Object.DestroyImmediate(obj);
            }
            _objects.Clear();
        }

        private MonsterTierSheetSet MakeSheet(float hpBarHeight)
        {
            MonsterTierSheetSet sheet = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            sheet.SetForTest(new Sprite[MonsterTierSheetSet.FrameCount], hpBarHeight);
            _objects.Add(sheet);
            return sheet;
        }

        private float HpY => _hpBar.transform.localPosition.y;

        [Test]
        public void 시트가_로드돼_있으면_Visual2D_에_주입하고_시트의_HP바_높이를_쓴다()
        {
            MonsterTierSheetSet sheet = MakeSheet(0.9f);
            _provider.Sets[(EMonster.Wisp, 2)] = sheet;

            _visual.ApplyLevel(3, EMonster.Wisp);

            Assert.AreSame(sheet, _visual2D.TierSheetForTest, "Lv3 → 티어 2 세트");
            Assert.AreEqual(0.9f, HpY, 0.0001f, "표(0.872)가 아니라 시트 값");
        }

        [Test]
        public void 등급_몬스터를_넘겨도_기준_몬스터와_티어로_조회한다()
        {
            _visual.ApplyLevel(5, EMonster.PlagueG4);

            Assert.AreEqual(new[] { (EMonster.Plague, 3) }, _provider.Queries.ToArray(), "등급은 시트를 바꾸지 않는다(monster-grades G3)");
        }

        [Test]
        public void 주입자는_있는데_세트가_없으면_풋내기_HP바_높이로_폴백한다()
        {
            _visual.ApplyLevel(5, EMonster.Wisp);

            Assert.IsNull(_visual2D.TierSheetForTest, "로드 실패 → 치환 없음");
            Assert.AreEqual(HeightByTier[0], HpY, 0.0001f, "티어 3 표 값(0.934)이 아니라 티어 0 값");
        }

        [Test]
        public void 시트의_HP바_높이가_0이면_시트는_쓰되_높이는_티어0_값으로_폴백한다()
        {
            MonsterTierSheetSet sheet = MakeSheet(0f);
            _provider.Sets[(EMonster.Wisp, 1)] = sheet;

            _visual.ApplyLevel(1, EMonster.Wisp);

            Assert.AreSame(sheet, _visual2D.TierSheetForTest);
            Assert.AreEqual(HeightByTier[0], HpY, 0.0001f);
        }

        [Test]
        public void 레벨_0_은_세트를_조회하지_않고_시트를_비운다()
        {
            _visual.ApplyLevel(0, EMonster.Wisp);

            Assert.AreEqual(0, _provider.Queries.Count, "티어 0 종족은 로드 0건 → 조회도 0건");
            Assert.IsNull(_visual2D.TierSheetForTest);
            Assert.AreEqual(HeightByTier[0], HpY, 0.0001f);
        }

        [Test]
        public void 강화_레벨을_내리면_이전_시트_치환이_해제된다()
        {
            _provider.Sets[(EMonster.Wisp, 3)] = MakeSheet(1f);
            _visual.ApplyLevel(5, EMonster.Wisp);
            Assert.IsNotNull(_visual2D.TierSheetForTest);

            _visual.ApplyLevel(0, EMonster.Wisp);

            Assert.IsNull(_visual2D.TierSheetForTest);
        }

        [Test]
        public void 등급_배율은_표_값에_합성된다_군주_Wisp_티어0_0_947()
        {
            _visual.SetGradeVisual(new FakeGradeVisual { SizeMul = 1.3f });

            _visual.ApplyLevel(0, EMonster.Wisp);

            Assert.AreEqual(0.9467f, HpY, 0.001f, "(0.7467 - 0.08) x 1.3 + 0.08 (monster-grades §4 검산 0.947)");
        }

        [Test]
        public void 등급_배율은_시트_높이에도_합성된다()
        {
            _provider.Sets[(EMonster.Wisp, 1)] = MakeSheet(0.8804f);
            _visual.SetGradeVisual(new FakeGradeVisual { SizeMul = 1.15f });

            _visual.ApplyLevel(1, EMonster.Wisp);

            Assert.AreEqual((0.8804f - 0.08f) * 1.15f + 0.08f, HpY, 0.0001f);
        }

        [Test]
        public void 배율_1_이면_저장값_그대로다_1등급_회귀()
        {
            _visual.SetGradeVisual(new FakeGradeVisual { SizeMul = 1f });
            _provider.Sets[(EMonster.Wisp, 2)] = MakeSheet(0.872f);

            _visual.ApplyLevel(3, EMonster.Wisp);

            Assert.AreEqual(0.872f, HpY, 0.0001f);
        }

        [Test]
        public void 등급_연출이_없으면_배율_1_로_취급한다()
        {
            _visual.SetGradeVisual(null);

            _visual.ApplyLevel(0, EMonster.Wisp);

            Assert.AreEqual(HeightByTier[0], HpY, 0.0001f);
        }

        [Test]
        public void ApplyLevel_을_반복해도_배율이_누적되지_않는다()
        {
            _visual.SetGradeVisual(new FakeGradeVisual { SizeMul = 1.3f });

            _visual.ApplyLevel(0, EMonster.Wisp);
            float first = HpY;
            _visual.ApplyLevel(0, EMonster.Wisp);
            _visual.ApplyLevel(0, EMonster.Wisp);

            Assert.AreEqual(first, HpY, 0.0001f, "저장값에서 매번 계산 - 이중 곱셈 없음");
        }

        [Test]
        public void 풀_재사용_OnEnable_에서_시트_치환이_해제되고_티어0_높이로_돌아간다()
        {
            _provider.Sets[(EMonster.Wisp, 3)] = MakeSheet(1f);
            _visual.ApplyLevel(5, EMonster.Wisp);
            Assert.IsNotNull(_visual2D.TierSheetForTest);

            MethodInfo mi = typeof(MonsterEnhancementVisual).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "MonsterEnhancementVisual.OnEnable 존재 - 시그니처 변경 감지");
            mi.Invoke(_visual, null);

            Assert.IsNull(_visual2D.TierSheetForTest, "Pop 시 티어 0(U-T8)");
            Assert.AreEqual(HeightByTier[0], HpY, 0.0001f);
        }

        [Test]
        public void HP바와_Visual2D_가_없어도_시트_주입은_예외없이_동작한다()
        {
            TestReflection.SetField(_visual, "_visual2D", null);
            _visual.SetHpBarForTest(null);
            _provider.Sets[(EMonster.Wisp, 1)] = MakeSheet(0.8f);

            Assert.DoesNotThrow(() => _visual.ApplyLevel(1, EMonster.Wisp));
        }

        [Test]
        public void 주입자를_해제하면_기존_티어_표를_쓴다_2D_전환_전_호환()
        {
            _visual.SetTierSheetProvider(null);

            _visual.ApplyLevel(5, EMonster.Wisp);

            Assert.AreEqual(HeightByTier[3], HpY, 0.0001f);
            Assert.IsNull(_visual2D.TierSheetForTest);
        }
    }
}
