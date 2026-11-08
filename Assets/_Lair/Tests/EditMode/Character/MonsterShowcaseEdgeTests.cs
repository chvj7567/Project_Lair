using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;
using Lair.Tests.EditMode;

namespace Lair.Tests.Character
{
    //# 쇼케이스 모드 엣지·회귀(village-monster-showcase §3, T-S1·T-S4) - 시드(MonsterShowcaseTests)의 켜기/복구/원래 꺼진 것 유지와 중복하지 않는다.
    //# 정렬(SetSorting) 상대 order·복구, 몸 오프셋 복구, 이중 켜기, OnEnable 안전망, 표시 높이, 공유 외형 적용기(MonsterLookApplier).
    public class MonsterShowcaseEdgeTests
    {
        private readonly List<Object> _objects = new List<Object>();
        private GradeFxDatabase _savedGrade;

        [SetUp]
        public void SetUp()
        {
            _savedGrade = GradeFxDatabase.Current;
            LogAssert.ignoreFailingMessages = false;
        }

        [TearDown]
        public void TearDown()
        {
            GradeFxDatabase.Current = _savedGrade;
            LogAssert.ignoreFailingMessages = false;
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }
            _objects.Clear();
        }

        private GameObject NewRoot(string name = "MonsterRoot")
        {
            GameObject go = new GameObject(name);
            _objects.Add(go);
            return go;
        }

        private SpriteRenderer NewRenderer(GameObject parent, string name, int layerId, int order)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
            sr.sortingLayerID = layerId;
            sr.sortingOrder = order;
            return sr;
        }

        private static int OtherLayerId()
        {
            SortingLayer[] layers = SortingLayer.layers;
            return layers[layers.Length - 1].id;
        }

        //# ───────── 모드 토글 ─────────

        [Test]
        public void 켜기를_두번_호출해도_복구는_원래_상태로_돌아간다()
        {
            GameObject root = NewRoot();
            Light light = root.AddComponent<Light>();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[] { light }, new Collider[0], new GameObject[0]);

            showcase.SetShowcaseMode(true);
            showcase.SetShowcaseMode(true);
            showcase.SetShowcaseMode(false);

            Assert.IsTrue(light.enabled, "두 번째 켜기가 '꺼진 상태'를 원래 값으로 덮어쓰면 영구히 꺼진다");
            Assert.IsFalse(showcase.IsOnForTest);
        }

        [Test]
        public void 켜지_않고_끄거나_여러번_꺼도_예외없고_상태가_변하지_않는다()
        {
            GameObject root = NewRoot();
            Light light = root.AddComponent<Light>();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[] { light }, new Collider[0], new GameObject[0]);

            Assert.DoesNotThrow(() => showcase.SetShowcaseMode(false));
            Assert.DoesNotThrow(() => showcase.SetShowcaseMode(false));

            Assert.IsTrue(light.enabled);
        }

        [Test]
        public void 켜기_복구를_반복해도_매번_같은_상태로_돌아온다()
        {
            GameObject root = NewRoot();
            Light on = root.AddComponent<Light>();
            AudioListener off = root.AddComponent<AudioListener>();
            off.enabled = false;
            BoxCollider box = root.AddComponent<BoxCollider>();
            GameObject hiddenOn = new GameObject("HpBarWrapper");
            hiddenOn.transform.SetParent(root.transform, false);
            GameObject hiddenOff = new GameObject("AuraSigil");
            hiddenOff.transform.SetParent(root.transform, false);
            hiddenOff.SetActive(false);
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[] { on, off }, new Collider[] { box }, new[] { hiddenOn, hiddenOff });

            for (int i = 0; i < 4; i++)
            {
                showcase.SetShowcaseMode(true);
                Assert.IsFalse(on.enabled || box.enabled || hiddenOn.activeSelf || hiddenOff.activeSelf, $"{i}회차 켜짐");
                showcase.SetShowcaseMode(false);
                Assert.IsTrue(on.enabled, $"{i}회차 복구");
                Assert.IsFalse(off.enabled, $"{i}회차 원래 꺼진 컴포넌트");
                Assert.IsTrue(box.enabled);
                Assert.IsTrue(hiddenOn.activeSelf);
                Assert.IsFalse(hiddenOff.activeSelf, $"{i}회차 원래 비활성이던 오브젝트");
            }
        }

        [Test]
        public void 콜라이더나_전투_컴포넌트가_원래_꺼져_있었으면_복구_뒤에도_꺼져_있다()
        {
            GameObject root = NewRoot();
            BoxCollider box = root.AddComponent<BoxCollider>();
            box.enabled = false;
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[] { box }, new GameObject[0]);

            showcase.SetShowcaseMode(true);
            showcase.SetShowcaseMode(false);

            Assert.IsFalse(box.enabled, "풀에 반환된 몬스터가 원래 꺼져 있던 충돌을 켜면 오염");
        }

        [Test]
        public void 배열에_null_이_섞여_있어도_켜기_복구가_예외없이_동작한다()
        {
            GameObject root = NewRoot();
            Light light = root.AddComponent<Light>();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[] { null, light }, new Collider[] { null }, new GameObject[] { null });

            Assert.DoesNotThrow(() => showcase.SetShowcaseMode(true));
            Assert.DoesNotThrow(() => showcase.SetShowcaseMode(false));
            Assert.IsTrue(light.enabled);
        }

        [Test]
        public void 복구를_빼먹고_풀에서_다시_켜져도_OnEnable_안전망이_복구한다()
        {
            GameObject root = NewRoot();
            Light light = root.AddComponent<Light>();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[] { light }, new Collider[0], new GameObject[0]);
            showcase.SetShowcaseMode(true);
            MethodInfo onEnable = typeof(MonsterShowcase).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(onEnable, "MonsterShowcase.OnEnable - 시그니처 변경 감지");

            onEnable.Invoke(showcase, null);

            Assert.IsFalse(showcase.IsOnForTest);
            Assert.IsTrue(light.enabled, "Pop 직후 상태(전투 컴포넌트 켜짐)");
        }

        //# ───────── 정렬 / 몸 오프셋 ─────────

        private MonsterShowcase NewSortable(out SpriteRenderer body, out SpriteRenderer aura, out SpriteRenderer fx, int baseLayer)
        {
            GameObject root = NewRoot();
            body = NewRenderer(root, "Visual2D", baseLayer, 10);
            aura = NewRenderer(root, "Aura", baseLayer, 8);
            fx = NewRenderer(root, "AuraGrade", baseLayer, 12);
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[0], new GameObject[0]);
            TestReflection.SetField(showcase, "_renderers", new[] { body, aura, fx });
            return showcase;
        }

        [Test]
        public void 정렬은_몸_order_기준으로_이동하고_다른_렌더러의_상대_순서를_유지한다()
        {
            int layer = OtherLayerId();
            MonsterShowcase showcase = NewSortable(out SpriteRenderer body, out SpriteRenderer aura, out SpriteRenderer fx, 0);

            showcase.SetSorting(layer, 35);

            Assert.AreEqual(35, body.sortingOrder);
            Assert.AreEqual(33, aura.sortingOrder, "몸보다 2 낮은 오라");
            Assert.AreEqual(37, fx.sortingOrder, "몸보다 2 높은 이펙트");
            Assert.AreEqual(layer, body.sortingLayerID);
            Assert.AreEqual(layer, aura.sortingLayerID);
            Assert.AreEqual(layer, fx.sortingLayerID);
        }

        [Test]
        public void 정렬을_반복_호출해도_기준값에서_계산해_누적되지_않는다()
        {
            MonsterShowcase showcase = NewSortable(out SpriteRenderer body, out SpriteRenderer aura, out SpriteRenderer fx, 0);

            showcase.SetSorting(0, 35);
            showcase.SetSorting(0, 35);
            showcase.SetSorting(0, 20);

            Assert.AreEqual(20, body.sortingOrder);
            Assert.AreEqual(18, aura.sortingOrder);
            Assert.AreEqual(22, fx.sortingOrder);
        }

        [Test]
        public void 모드를_끄면_정렬_레이어와_order_가_원래대로_돌아간다()
        {
            int layer = OtherLayerId();
            MonsterShowcase showcase = NewSortable(out SpriteRenderer body, out SpriteRenderer aura, out SpriteRenderer fx, 0);
            showcase.SetShowcaseMode(true);
            showcase.SetSorting(layer, 35);

            showcase.SetShowcaseMode(false);

            Assert.AreEqual(0, body.sortingLayerID);
            Assert.AreEqual(10, body.sortingOrder);
            Assert.AreEqual(8, aura.sortingOrder);
            Assert.AreEqual(12, fx.sortingOrder);
        }

        [Test]
        public void 렌더러가_없거나_null_이_섞여도_정렬은_예외없이_동작한다()
        {
            GameObject root = NewRoot();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[0], new GameObject[0]);
            Assert.DoesNotThrow(() => showcase.SetSorting(0, 5), "_renderers 미배선");

            SpriteRenderer body = NewRenderer(root, "Visual2D", 0, 10);
            TestReflection.SetField(showcase, "_renderers", new SpriteRenderer[] { body, null });
            Assert.DoesNotThrow(() => showcase.SetSorting(0, 5));
            Assert.AreEqual(5, body.sortingOrder);
        }

        [Test]
        public void 몸_오프셋은_루트_위치_기준이고_모드를_끄면_원래_로컬_위치로_복구된다()
        {
            GameObject root = NewRoot();
            root.transform.position = new Vector3(3f, 0f, 4f);
            GameObject visual = new GameObject("Visual2D");
            visual.transform.SetParent(root.transform, false);
            visual.transform.localPosition = new Vector3(0f, 0.2f, 0f);
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[0], new GameObject[0]);
            TestReflection.SetField(showcase, "_visualRoot", visual.transform);
            showcase.SetShowcaseMode(true);

            showcase.SetVisualOffset(new Vector3(0f, 0.25f, 0f));
            Assert.AreEqual(new Vector3(3f, 0.25f, 4f), visual.transform.position, "비행 흔들림은 루트 + 오프셋(이펙트는 발 위치 유지)");

            showcase.SetShowcaseMode(false);
            Assert.AreEqual(new Vector3(0f, 0.2f, 0f), visual.transform.localPosition, "복구 후 풀 Pop 직후 위치");
        }

        [Test]
        public void 애니메이터가_없어도_이동_표시는_예외가_없다()
        {
            GameObject root = NewRoot();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[0], new GameObject[0]);

            Assert.DoesNotThrow(() => showcase.SetMoving(true));
            Assert.DoesNotThrow(() => showcase.SetVisualOffset(Vector3.up));
        }

        //# ───────── 표시 높이 ─────────

        [Test]
        public void 표시_높이는_강화_시각의_적용_높이를_그대로_돌려주고_없으면_0_이다()
        {
            GameObject root = NewRoot();
            MonsterShowcase showcase = root.AddComponent<MonsterShowcase>();
            showcase.SetForTest(new Behaviour[0], new Collider[0], new GameObject[0]);
            Assert.AreEqual(0f, showcase.DisplayHeightWorld, "_enhancement 미배선");

            MonsterEnhancementVisual enhancement = root.AddComponent<MonsterEnhancementVisual>();
            enhancement.SetHpBarHeightByTierForTest(new[] { 0.7467f, 0.8f, 0.9f, 1f });
            TestReflection.SetField(showcase, "_enhancement", enhancement);
            Assert.AreEqual(0f, showcase.DisplayHeightWorld, "ApplyLevel 전");

            enhancement.ApplyLevel(0, EMonster.Wisp);
            Assert.AreEqual(0.7467f, showcase.DisplayHeightWorld, 0.0001f);
        }

        //# ───────── MonsterLookApplier (전투·쇼케이스 공유 적용) ─────────

        private sealed class FakeGrade : MonoBehaviour, IMonsterGradeVisual
        {
            public float SizeMul { get; private set; } = 1f;
            public int ConfigureCount;
            public void Configure(GradeFxSpec spec)
            {
                ConfigureCount++;
                SizeMul = spec.SizeMul;
            }
        }

        private GameObject NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar, bool withEnhancement = true)
        {
            GameObject root = NewRoot("Character");
            root.AddComponent<LairCharacter>();
            grade = root.AddComponent<FakeGrade>();
            GameObject hpGo = new GameObject("HpBarWrapper");
            hpGo.transform.SetParent(root.transform, false);
            hpBar = hpGo.AddComponent<MonsterHpBar>();
            if (withEnhancement)
            {
                MonsterEnhancementVisual enhancement = root.AddComponent<MonsterEnhancementVisual>();
                enhancement.SetHpBarForTest(hpBar);
                enhancement.SetHpBarHeightByTierForTest(new[] { 0.7467f, 0.8f, 0.9f, 1f });
            }
            return root;
        }

        private static GradeFxDatabase GradeDb()
        {
            GradeRow[] grades = new GradeRow[5];
            float[] size = { 1f, 1.075f, 1.15f, 1.225f, 1.3f };
            for (int i = 0; i < 5; i++)
            {
                grades[i] = new GradeRow { grade = i + 1, sizeMul = size[i], hpMul = 1f, atkMul = 1f };
            }
            Monster_GradeFxColorRow[] colors = new Monster_GradeFxColorRow[6];
            for (int i = 0; i < 6; i++)
            {
                colors[i] = new Monster_GradeFxColorRow { monster = ((EMonster)i).ToString(), glowColor = "#FFFFFF" };
            }
            return GradeFxDatabase.Build(grades, null, null, null, null, colors);
        }

        [Test]
        public void 등급_연출을_먼저_적용하고_그_크기를_HP바_높이에_합성한다_군주_Wisp()
        {
            GradeFxDatabase.Current = GradeDb();
            GameObject root = NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar);

            MonsterLookApplier.Apply(root, EMonster.WispG5, EMonster.Wisp, 0, true, null);

            Assert.AreEqual(1, grade.ConfigureCount);
            Assert.AreEqual(1.3f, grade.SizeMul, 0.0001f);
            Assert.AreEqual((0.7467f - 0.08f) * 1.3f + 0.08f, hpBar.transform.localPosition.y, 0.0001f, "Configure → ApplyLevel 순서 - 뒤바뀌면 배율 1 로 계산");
        }

        [Test]
        public void 재적용에서는_등급_연출을_다시_Configure_하지_않는다_이펙트_위상_유지()
        {
            GradeFxDatabase.Current = GradeDb();
            GameObject root = NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar);
            MonsterLookApplier.Apply(root, EMonster.WispG3, EMonster.Wisp, 0, true, null);

            MonsterLookApplier.Apply(root, EMonster.WispG3, EMonster.Wisp, 3, false, null);

            Assert.AreEqual(1, grade.ConfigureCount, "강화 구매 재적용에서 이펙트가 튀지 않음");
            Assert.AreEqual((0.9f - 0.08f) * 1.15f + 0.08f, hpBar.transform.localPosition.y, 0.0001f, "재적용도 sizeMul 을 합성(주입자 없음 → Lv3 = 티어 2 표 0.9 기준)");
        }

        [Test]
        public void 일등급_키는_등급_연출을_찾지도_Configure_하지도_않는다()
        {
            GradeFxDatabase.Current = GradeDb();
            GameObject root = NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar);

            MonsterLookApplier.Apply(root, EMonster.Wisp, EMonster.Wisp, 0, true, null);

            Assert.AreEqual(0, grade.ConfigureCount);
            Assert.AreEqual(0.7467f, hpBar.transform.localPosition.y, 0.0001f, "배율 1");
        }

        [Test]
        public void 등급_DB_가_없으면_등급_키여도_Configure_없이_예외도_없다()
        {
            GradeFxDatabase.Current = null;
            GameObject root = NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar);

            Assert.DoesNotThrow(() => MonsterLookApplier.Apply(root, EMonster.WispG5, EMonster.Wisp, 0, true, null));

            Assert.AreEqual(0, grade.ConfigureCount);
            Assert.AreEqual(0.7467f, hpBar.transform.localPosition.y, 0.0001f);
        }

        [Test]
        public void 등급_연출_컴포넌트가_없는_Variant_나_강화_시각이_없는_프리팹도_예외가_없다()
        {
            GradeFxDatabase.Current = GradeDb();
            GameObject noGrade = NewRoot("NoGrade");
            noGrade.AddComponent<LairCharacter>();
            noGrade.AddComponent<MonsterEnhancementVisual>();
            Assert.DoesNotThrow(() => MonsterLookApplier.Apply(noGrade, EMonster.WispG4, EMonster.Wisp, 2, true, null), "IMonsterGradeVisual 없음");

            GameObject noEnhance = NewCharacter(out FakeGrade grade, out MonsterHpBar hpBar, withEnhancement: false);
            Assert.DoesNotThrow(() => MonsterLookApplier.Apply(noEnhance, EMonster.WispG4, EMonster.Wisp, 2, true, null), "MonsterEnhancementVisual 없음");
            Assert.AreEqual(1, grade.ConfigureCount, "등급 연출은 강화 시각과 독립");
        }

        [Test]
        public void LairCharacter_가_없는_오브젝트도_등급_연출만_건너뛰고_동작한다()
        {
            GradeFxDatabase.Current = GradeDb();
            GameObject root = NewRoot("Bare");
            root.AddComponent<MonsterEnhancementVisual>();

            Assert.DoesNotThrow(() => MonsterLookApplier.Apply(root, EMonster.WispG2, EMonster.Wisp, 1, true, null));
        }
    }
}
