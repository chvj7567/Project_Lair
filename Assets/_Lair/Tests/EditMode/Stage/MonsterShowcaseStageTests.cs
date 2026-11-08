using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Character;
using Lair.Data;
using Lair.Showcase;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# MonsterShowcaseStage 의 EditMode 가능한 로직(village-monster-showcase §4.2·§8, T-S7·T-S8 일부) - fit 배율, 갱신 경합(_busy/_dirty), ReleaseAll 멱등.
    //# CHMResource/CHMPool(Addressables·풀) 의존 경로(프리팹 로드·Pop/Push·자리표시자 교체)는 EditMode 로 만들 수 없어 이 스위트 밖이다.
    //# private 상태는 리플렉션으로 읽고 쓴다(production 수정 금지).
    public class MonsterShowcaseStageTests
    {
        private sealed class FakeShowcase : IMonsterShowcase
        {
            public float Height;
            public float DisplayHeightWorld => Height;
            public void SetShowcaseMode(bool on) { }
            public void SetMoving(bool moving) { }
            public void SetVisualOffset(Vector3 worldOffset) { }
            public void SetSorting(int sortingLayerId, int bodyOrder) { }
            public readonly List<float> IdlePhases = new List<float>();
            public void SetIdlePhase(float seconds) => IdlePhases.Add(seconds);
        }

        [Serializable] private class SceneWrap { public Showcase_SceneRow[] items; }
        [Serializable] private class ActorWrap { public Showcase_ActorRow[] items; }
        [Serializable] private class PointWrap { public Showcase_Actor_WaypointRow[] items; }

        private const BindingFlags Private = BindingFlags.NonPublic | BindingFlags.Instance;

        private readonly List<UnityEngine.Object> _objects = new List<UnityEngine.Object>();
        private GradeFxDatabase _savedGrade;
        private ShowcaseDatabase _savedShowcase;
        private MonsterShowcaseStage _stage;

        [SetUp]
        public void SetUp()
        {
            _savedGrade = GradeFxDatabase.Current;
            _savedShowcase = ShowcaseDatabase.Current;
            ShowcaseDatabase.Current = null;
            GameObject go = new GameObject("Stage");
            _objects.Add(go);
            _stage = go.AddComponent<MonsterShowcaseStage>();
        }

        [TearDown]
        public void TearDown()
        {
            GradeFxDatabase.Current = _savedGrade;
            ShowcaseDatabase.Current = _savedShowcase;
            foreach (UnityEngine.Object obj in _objects)
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
            _objects.Clear();
        }

        private static string Read(string name) => "{\"items\":" + File.ReadAllText(Path.Combine(Application.dataPath, "_Lair/Data/Json", name + ".json")) + "}";

        private static ShowcaseDatabase RealDb()
        {
            return ShowcaseDatabase.Build(
                JsonUtility.FromJson<SceneWrap>(Read("Showcase_Scene")).items,
                JsonUtility.FromJson<ActorWrap>(Read("Showcase_Actor")).items,
                JsonUtility.FromJson<PointWrap>(Read("Showcase_Actor_Waypoint")).items);
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

        private T Get<T>(string field) => (T)typeof(MonsterShowcaseStage).GetField(field, Private).GetValue(_stage);

        private void Set(string field, object value)
        {
            FieldInfo info = typeof(MonsterShowcaseStage).GetField(field, Private);
            Assert.IsNotNull(info, $"필드 미발견: {field}");
            info.SetValue(_stage, value);
        }

        private void Call(string method)
        {
            MethodInfo mi = typeof(MonsterShowcaseStage).GetMethod(method, Private);
            Assert.IsNotNull(mi, $"메서드 미발견: {method} - 시그니처 변경 감지");
            mi.Invoke(_stage, null);
        }

        //# ───────── fit 배율 (§4.2) ─────────

        private sealed class FitCase
        {
            public GameObject Instance;
            public GameObject ShadowGo;
            public object State;
        }

        private FitCase NewFit(float heightDots, Vector3 prefabScale, bool withShowcase = true, bool withShadow = true)
        {
            FitCase fit = new FitCase { Instance = new GameObject("Monster") };
            _objects.Add(fit.Instance);
            CHPoolable poolable = fit.Instance.AddComponent<CHPoolable>();
            ShowcaseSlot slot = new ShowcaseSlot();
            if (withShadow)
            {
                fit.ShadowGo = new GameObject("Shadow");
                _objects.Add(fit.ShadowGo);
                fit.ShadowGo.transform.localScale = new Vector3(2f, 2f, 1f);
                slot.Shadow = fit.ShadowGo.AddComponent<SpriteRenderer>();
            }

            Type type = typeof(MonsterShowcaseStage).GetNestedType("SlotState", BindingFlags.NonPublic);
            Assert.IsNotNull(type, "SlotState - 시그니처 변경 감지");
            fit.State = Activator.CreateInstance(type, true);
            type.GetField("Slot").SetValue(fit.State, slot);
            type.GetField("Instance").SetValue(fit.State, poolable);
            type.GetField("PrefabScale").SetValue(fit.State, prefabScale);
            type.GetField("ShadowBaseScale").SetValue(fit.State, new Vector3(2f, 2f, 1f));
            if (withShowcase)
            {
                type.GetField("Showcase").SetValue(fit.State, new FakeShowcase { Height = heightDots / DotHeight + 0.08f });
            }
            return fit;
        }

        private const float DotHeight = 48f;

        private void RunFit(FitCase fit, EMonster key, ShowcaseDatabase db)
        {
            MethodInfo mi = typeof(MonsterShowcaseStage).GetMethod("FitToHeight", Private);
            Assert.IsNotNull(mi, "FitToHeight - 시그니처 변경 감지");
            mi.Invoke(_stage, new object[] { fit.State, key, db });
        }

        [Test]
        public void 망령_T3_군주_표시_높이_127도트는_94도트로_줄이는_배율_0_74를_곱한다()
        {
            GradeFxDatabase.Current = GradeDb();
            FitCase fit = NewFit(127f, Vector3.one);

            RunFit(fit, EMonster.WraithG5, RealDb());

            float expected = 94f / 127f;
            Assert.AreEqual(0.74f, expected, 0.001f, "기획서 검산값");
            Assert.AreEqual(expected, fit.Instance.transform.localScale.x, 0.0001f);
            Assert.AreEqual(expected, fit.Instance.transform.localScale.y, 0.0001f);
            Assert.AreEqual(2f * expected * 1.3f, fit.ShadowGo.transform.localScale.x, 0.0001f, "그림자 = 기본 x fit x 등급 sizeMul(1.3)");
        }

        [Test]
        public void 정확히_94도트는_줄이지_않고_작은_몬스터는_키우지_않는다()
        {
            GradeFxDatabase.Current = GradeDb();
            FitCase atLimit = NewFit(94f, Vector3.one);
            FitCase small = NewFit(60f, Vector3.one);

            RunFit(atLimit, EMonster.Wisp, RealDb());
            RunFit(small, EMonster.Wisp, RealDb());

            Assert.AreEqual(1f, atLimit.Instance.transform.localScale.x, 0.0001f, "상한과 같으면 배율 1");
            Assert.AreEqual(1f, small.Instance.transform.localScale.x, 0.0001f, "확대 금지");
        }

        [Test]
        public void 상한을_1도트라도_넘으면_그만큼만_줄인다()
        {
            FitCase fit = NewFit(95f, Vector3.one);

            RunFit(fit, EMonster.Wisp, RealDb());

            Assert.AreEqual(94f / 95f, fit.Instance.transform.localScale.x, 0.0001f);
        }

        [Test]
        public void 프리팹_루트_스케일은_fit_배율과_곱해진다()
        {
            FitCase fit = NewFit(188f, new Vector3(1.5f, 1.5f, 1.5f));

            RunFit(fit, EMonster.Wisp, RealDb());

            Assert.AreEqual(0.75f, fit.Instance.transform.localScale.x, 0.0001f, "1.5 x (94/188=0.5)");
        }

        [Test]
        public void 쇼케이스_컴포넌트나_표시_높이가_없으면_배율_1_이다()
        {
            FitCase noShowcase = NewFit(300f, Vector3.one, withShowcase: false);
            FitCase zeroHeight = NewFit(0f, Vector3.one);

            RunFit(noShowcase, EMonster.Wisp, RealDb());
            RunFit(zeroHeight, EMonster.Wisp, RealDb());

            Assert.AreEqual(1f, noShowcase.Instance.transform.localScale.x, 0.0001f);
            Assert.AreEqual(1f, zeroHeight.Instance.transform.localScale.x, 0.0001f, "높이 0 이하는 줄이지 않음(0 나눗셈 방지)");
        }

        [Test]
        public void 등급_DB_가_없거나_1등급이면_그림자는_sizeMul_1_로_계산한다()
        {
            GradeFxDatabase.Current = null;
            FitCase noDb = NewFit(188f, Vector3.one);
            RunFit(noDb, EMonster.WispG5, RealDb());
            Assert.AreEqual(2f * 0.5f, noDb.ShadowGo.transform.localScale.x, 0.0001f);

            GradeFxDatabase.Current = GradeDb();
            FitCase rookie = NewFit(188f, Vector3.one);
            RunFit(rookie, EMonster.Wisp, RealDb());
            Assert.AreEqual(2f * 0.5f, rookie.ShadowGo.transform.localScale.x, 0.0001f);
        }

        [Test]
        public void 그림자가_없는_슬롯도_예외없이_본체만_줄인다()
        {
            FitCase fit = NewFit(188f, Vector3.one, withShadow: true);
            fit.State.GetType().GetField("Slot").SetValue(fit.State, new ShowcaseSlot());

            Assert.DoesNotThrow(() => RunFit(fit, EMonster.Wisp, RealDb()));
            Assert.AreEqual(0.5f, fit.Instance.transform.localScale.x, 0.0001f);
        }

        [Test]
        public void 로딩은_자기_씬_행의_상한을_쓴다()
        {
            ShowcaseDatabase db = RealDb();
            db.SceneRowOf(EScene.Loading).fitMaxHeightDots = 47f;
            Set("_scene", EScene.Loading);
            FitCase fit = NewFit(94f, Vector3.one);

            RunFit(fit, EMonster.Wisp, db);

            Assert.AreEqual(0.5f, fit.Instance.transform.localScale.x, 0.0001f);
        }

        //# ───────── 갱신 경합 (_busy / _dirty) ─────────

        [Test]
        public void RequestRefresh_는_dirty_만_세우고_연속_호출은_한_번으로_합쳐진다()
        {
            _stage.RequestRefresh();
            _stage.RequestRefresh();
            _stage.RequestRefresh();

            Assert.IsTrue(Get<bool>("_dirty"));
            Set("_begun", true);
            Call("LateUpdate");
            Assert.IsFalse(Get<bool>("_dirty"), "한 프레임에 한 번 처리하고 플래그가 내려감");
            Call("LateUpdate");
            Assert.IsFalse(Get<bool>("_dirty"), "추가 요청 없으면 재처리 없음");
        }

        [Test]
        public void 갱신_중에_온_요청은_보류되고_끝난_다음_프레임에_처리된다()
        {
            Set("_begun", true);
            Set("_busy", true);
            _stage.RequestRefresh();

            Call("LateUpdate");
            Assert.IsTrue(Get<bool>("_dirty"), "진행 중이면 dirty 유지(연속 승급 경합)");

            Set("_busy", false);
            Call("LateUpdate");
            Assert.IsFalse(Get<bool>("_dirty"));
        }

        [Test]
        public void 시작_전이거나_해제_후에는_갱신_요청을_처리하지_않는다()
        {
            _stage.RequestRefresh();
            Call("LateUpdate");
            Assert.IsTrue(Get<bool>("_dirty"), "PrepareAsync 전");

            Set("_begun", true);
            _stage.ReleaseAll();
            Call("LateUpdate");
            Assert.IsTrue(Get<bool>("_dirty"), "ReleaseAll 후에는 풀·시트를 다시 잡지 않음");
        }

        [Test]
        public async Task 이미_갱신_중인데_RefreshAsync_가_다시_불리면_dirty_만_세우고_바로_끝난다()
        {
            Set("_busy", true);
            MethodInfo mi = typeof(MonsterShowcaseStage).GetMethod("RefreshAsync", Private);
            Assert.IsNotNull(mi, "RefreshAsync - 시그니처 변경 감지");

            await (Task)mi.Invoke(_stage, null);

            Assert.IsTrue(Get<bool>("_dirty"));
            Assert.IsTrue(Get<bool>("_busy"), "진행 중이던 갱신의 busy 를 건드리지 않음");
        }

        [Test]
        public async Task 쇼케이스_DB_가_없으면_PrepareAsync_는_예외없이_끝나고_두번째_호출은_무시된다()
        {
            await _stage.PrepareAsync();
            Assert.IsTrue(Get<bool>("_begun"));
            Assert.IsFalse(Get<bool>("_busy"));

            Set("_busy", true);
            await _stage.PrepareAsync();

            Assert.IsFalse(Get<bool>("_dirty"), "두 번째 PrepareAsync 는 갱신을 시도하지 않음(_begun 가드)");
        }

        //# ───────── ReleaseAll ─────────

        [Test]
        public void ReleaseAll_은_상태가_비어_있어도_여러번_불러도_예외가_없다()
        {
            Assert.DoesNotThrow(() => _stage.ReleaseAll());
            Assert.DoesNotThrow(() => _stage.ReleaseAll());
            Assert.IsTrue(Get<bool>("_released"));
        }

        [Test]
        public async Task ReleaseAll_은_티어_시트를_한번만_해제하고_로더를_비운다()
        {
            int unloadCount = 0;
            MonsterTierSheetSet sheet = ScriptableObject.CreateInstance<MonsterTierSheetSet>();
            _objects.Add(sheet);
            MonsterTierSheetLoader loader = new MonsterTierSheetLoader(key => Task.FromResult(sheet), key => unloadCount++, msg => { });
            await loader.LoadAsync(new List<(EMonster, int)> { (EMonster.Wisp, 1), (EMonster.Hex, 3) });
            Set("_loader", loader);

            _stage.ReleaseAll();
            _stage.ReleaseAll();

            Assert.AreEqual(2, unloadCount, "로드한 키 2개를 각각 한 번씩만");
            Assert.IsNull(Get<MonsterTierSheetLoader>("_loader"));
            Assert.IsNull(loader.Find(EMonster.Wisp, 1), "해제 후 조회 불가");
        }

        //# ───────── 슬롯 상태 구성 (BuildStates) - Index 는 로딩 Idle 위상(i x 0.37s)의 입력 ─────────

        private void BuildStates(ShowcaseDatabase db)
        {
            MethodInfo mi = typeof(MonsterShowcaseStage).GetMethod("BuildStates", Private);
            Assert.IsNotNull(mi, "BuildStates - 시그니처 변경 감지");
            mi.Invoke(_stage, new object[] { db });
        }

        private ShowcaseSlot NewSlot(EMonster species, bool withHolder)
        {
            ShowcaseSlot slot = new ShowcaseSlot { Species = species };
            if (withHolder)
            {
                GameObject holder = new GameObject("Slot_" + species);
                _objects.Add(holder);
                slot.Holder = holder.transform;
            }
            return slot;
        }

        private static int IndexOf(object state) => (int)state.GetType().GetField("Index").GetValue(state);

        [Test]
        public void 슬롯_Index_는_건너뛴_슬롯을_세지_않고_0부터_연속이다()
        {
            Set("_scene", EScene.Loading);
            Set("_slots", new[] { NewSlot(EMonster.Phantom, true), NewSlot(EMonster.Hex, false), NewSlot(EMonster.Wisp, true), NewSlot(EMonster.Wraith, true) });
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("슬롯·데이터 누락"));

            BuildStates(RealDb());

            System.Collections.IList states = Get<System.Collections.IList>("_states");
            Assert.AreEqual(3, states.Count);
            for (int i = 0; i < states.Count; i++)
            {
                Assert.AreEqual(i, IndexOf(states[i]), "위상 = Index x 0.37s - 구멍이 있으면 간격이 어긋남");
            }
        }

        [Test]
        public void 슬롯_상태는_첫_경유점과_시작_방향으로_초기화된다()
        {
            Set("_scene", EScene.Loading);
            Set("_slots", new[] { NewSlot(EMonster.Wraith, true), NewSlot(EMonster.Wisp, true) });

            BuildStates(RealDb());

            System.Collections.IList states = Get<System.Collections.IList>("_states");
            Type type = states[0].GetType();
            Assert.AreEqual(new Vector2(271f, 225f), (Vector2)type.GetField("PosDots").GetValue(states[0]), "로딩 망령 경유점 0");
            Assert.IsTrue((bool)type.GetField("FlipLeft").GetValue(states[0]), "로딩 망령은 왼쪽을 본다");
            Assert.IsFalse((bool)type.GetField("FlipLeft").GetValue(states[1]), "로딩 도깨비불은 오른쪽");
        }

        [Test]
        public void 슬롯_배열이_비면_상태가_비어_있다()
        {
            Set("_slots", new ShowcaseSlot[0]);
            BuildStates(RealDb());

            Assert.AreEqual(0, Get<System.Collections.IList>("_states").Count);
        }
    }
}
