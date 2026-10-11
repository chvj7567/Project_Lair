using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 비주얼 폴리시 묶음 A(ResultPopup · ConfirmPopup · ToastView · SkillUnlockBanner) 스위트(ui-polish-batch-a §8) —
    //# 프리팹 연결·Missing Script·승패 장식 토글·토스트 종류 틴트·레이캐스트·UILoopMotion 개수/파라미터·기존 필드 회귀.
    public class UiPolishBatchATests
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string ResultPath = UiDir + "ResultPopup.prefab";
        private const string ConfirmPath = UiDir + "ConfirmPopup.prefab";
        private const string ToastPath = UiDir + "ToastView.prefab";
        private const string BannerPath = UiDir + "SkillUnlockBanner.prefab";

        private static readonly string[] WinDecorNames = { "DecoWinBack", "DecoWinFront" };
        private static readonly string[] LoseDecorNames = { "DecoLoseBack", "DecoLoseFront" };
        private static readonly string[] ToastTintNames = { "DecoKindStrip", "DecoKindGlow", "DecoDotGlow" };
        private static readonly float[] ToastTintAlphas = { 1.0f, 0.45f, 0.55f };

        private readonly List<UnityEngine.Object> _spawned = new List<UnityEngine.Object>();

        [TearDown]
        public void 생성한_오브젝트를_정리한다()
        {
            foreach (UnityEngine.Object obj in _spawned)
            {
                if (obj != null)
                    UnityEngine.Object.DestroyImmediate(obj);
            }
            _spawned.Clear();
        }

        //# --- 공용 헬퍼 ---

        private static FieldInfo FieldOf(object target, string name)
        {
            const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly;
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, flags);
                if (field != null)
                    return field;
            }
            Assert.Fail($"{target.GetType().Name}.{name} 필드 없음 — 시그니처 변경 감지");
            return null;
        }

        private static object Field(object target, string name)
        {
            return FieldOf(target, name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldOf(target, name).SetValue(target, value);
        }

        //# 리플렉션 값이 null 이면 NRE 대신 Assert.NotNull 로 먼저 실패시킨다.
        private static T Required<T>(object target, string name) where T : class
        {
            object value = Field(target, name);
            if (value is UnityEngine.Object unityObj)
            {
                Assert.IsTrue(unityObj != null, $"{target.GetType().Name}.{name} 비어 있음");
            }
            else
            {
                Assert.IsNotNull(value, $"{target.GetType().Name}.{name} 비어 있음");
            }
            return (T)value;
        }

        private static void Invoke(object target, string method, params object[] args)
        {
            MethodInfo info = target.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(info, $"{target.GetType().Name}.{method} 메서드 없음");
            info.Invoke(target, args);
        }

        private static GameObject LoadPrefabAsset(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(root, path);
            return root;
        }

        private GameObject NewInstance(string path)
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(LoadPrefabAsset(path)) as GameObject;
            Assert.IsNotNull(instance, "프리팹 인스턴스 " + path);
            _spawned.Add(instance);
            return instance;
        }

        private static T Comp<T>(GameObject root) where T : Component
        {
            T comp = root.GetComponentInChildren<T>(true);
            Assert.IsTrue(comp != null, $"{root.name} 에 {typeof(T).Name} 없음");
            return comp;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            Assert.Fail($"{root.name} 하위에 {name} 없음");
            return null;
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.01f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.01f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.01f, message + " b");
        }

        private ResultPopup NewResultPopup()
        {
            return Comp<ResultPopup>(NewInstance(ResultPath));
        }

        private ToastView NewToast()
        {
            return Comp<ToastView>(NewInstance(ToastPath));
        }

        private static ResultPopupArg ResultArg(BattleResult result)
        {
            return new ResultPopupArg { Result = result };
        }

        private static void AssertDecorState(GameObject[] decor, bool expected, string label)
        {
            foreach (GameObject go in decor)
            {
                Assert.IsTrue(go != null, label + " 원소 비어 있음");
                Assert.AreEqual(expected, go.activeSelf, $"{label} {go.name}");
            }
        }

        //# --- ① Missing Script ---

        [TestCase(ResultPath)]
        [TestCase(ConfirmPath)]
        [TestCase(ToastPath)]
        [TestCase(BannerPath)]
        public void 프리팹_YAML에_Missing_Script_참조가_없다(string path)
        {
            string full = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
            Assert.IsTrue(File.Exists(full), full);
            MatchCollection missing = Regex.Matches(File.ReadAllText(full), @"m_Script:\s*\{fileID:\s*0\}");
            Assert.AreEqual(0, missing.Count, path + " 의 m_Script fileID 0 (Missing Script)");
        }

        [TestCase(ResultPath)]
        [TestCase(ConfirmPath)]
        [TestCase(ToastPath)]
        [TestCase(BannerPath)]
        public void 프리팹_모든_오브젝트에_Missing_Script와_null_컴포넌트가_없다(string path)
        {
            foreach (Transform t in LoadPrefabAsset(path).GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), $"{path} {t.name} Missing Script 수");
                foreach (Component c in t.GetComponents<Component>())
                {
                    Assert.IsTrue(c != null, $"{path} {t.name} 에 null 컴포넌트");
                }
            }
        }

        //# --- ② ResultPopup 장식 묶음 ---

        [Test]
        public void ResultPopup_장식_배열이_길이_2와_기획서_이름_순서로_연결된다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));

            GameObject[] win = Required<GameObject[]>(popup, "_winDecor");
            GameObject[] lose = Required<GameObject[]>(popup, "_loseDecor");
            Assert.AreEqual(2, win.Length, "_winDecor 길이");
            Assert.AreEqual(2, lose.Length, "_loseDecor 길이");
            for (int i = 0; i < 2; ++i)
            {
                Assert.IsTrue(win[i] != null, $"_winDecor[{i}] 비어 있음");
                Assert.IsTrue(lose[i] != null, $"_loseDecor[{i}] 비어 있음");
                Assert.AreEqual(WinDecorNames[i], win[i].name, $"_winDecor[{i}]");
                Assert.AreEqual(LoseDecorNames[i], lose[i].name, $"_loseDecor[{i}]");
            }
        }

        [Test]
        public void ResultPopup_장식_묶음_4개는_프리팹_기본_비활성이다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));

            AssertDecorState(Required<GameObject[]>(popup, "_winDecor"), false, "승리 묶음 기본");
            AssertDecorState(Required<GameObject[]>(popup, "_loseDecor"), false, "패배 묶음 기본");
        }

        [Test]
        public void ResultPopup_Back은_루트_직속이고_Front는_ResultPanel_자식이다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));
            Transform root = popup.transform;
            GameObject[] win = Required<GameObject[]>(popup, "_winDecor");
            GameObject[] lose = Required<GameObject[]>(popup, "_loseDecor");

            Assert.AreSame(root, win[0].transform.parent, "DecoWinBack 부모");
            Assert.AreSame(root, lose[0].transform.parent, "DecoLoseBack 부모");
            Assert.AreEqual("ResultPanel", win[1].transform.parent.name, "DecoWinFront 부모");
            Assert.AreEqual("ResultPanel", lose[1].transform.parent.name, "DecoLoseFront 부모");
            Assert.AreSame(root, win[1].transform.parent.parent, "ResultPanel 은 루트 직속");
        }

        [Test]
        public void ResultPopup_InitUI_Win이면_승리_묶음과_Rays만_켜진다()
        {
            ResultPopup popup = NewResultPopup();

            popup.InitUI(ResultArg(BattleResult.Win));

            AssertDecorState(Required<GameObject[]>(popup, "_winDecor"), true, "승리 묶음");
            AssertDecorState(Required<GameObject[]>(popup, "_loseDecor"), false, "패배 묶음");
            Assert.IsTrue(Required<GameObject>(popup, "_rays").activeSelf, "Rays");
        }

        [Test]
        public void ResultPopup_InitUI_Lose이면_패배_묶음만_켜지고_Rays는_꺼진다()
        {
            ResultPopup popup = NewResultPopup();

            popup.InitUI(ResultArg(BattleResult.Lose));

            AssertDecorState(Required<GameObject[]>(popup, "_winDecor"), false, "승리 묶음");
            AssertDecorState(Required<GameObject[]>(popup, "_loseDecor"), true, "패배 묶음");
            Assert.IsFalse(Required<GameObject>(popup, "_rays").activeSelf, "Rays");
        }

        [Test]
        public void ResultPopup_InitUI_None이면_두_묶음_모두_꺼진다()
        {
            ResultPopup popup = NewResultPopup();

            popup.InitUI(ResultArg(BattleResult.None));

            AssertDecorState(Required<GameObject[]>(popup, "_winDecor"), false, "승리 묶음");
            AssertDecorState(Required<GameObject[]>(popup, "_loseDecor"), false, "패배 묶음");
            Assert.IsFalse(Required<GameObject>(popup, "_rays").activeSelf, "Rays");
        }

        [Test]
        public void ResultPopup_승리_패배_승리_재호출에도_이전_결과_잔상이_없다()
        {
            ResultPopup popup = NewResultPopup();
            GameObject[] win = Required<GameObject[]>(popup, "_winDecor");
            GameObject[] lose = Required<GameObject[]>(popup, "_loseDecor");

            popup.InitUI(ResultArg(BattleResult.Win));
            AssertDecorState(win, true, "1차 승리 - 승리 묶음");
            AssertDecorState(lose, false, "1차 승리 - 패배 묶음");

            popup.InitUI(ResultArg(BattleResult.Lose));
            AssertDecorState(win, false, "2차 패배 - 승리 묶음 잔상");
            AssertDecorState(lose, true, "2차 패배 - 패배 묶음");

            popup.InitUI(ResultArg(BattleResult.Win));
            AssertDecorState(win, true, "3차 승리 - 승리 묶음");
            AssertDecorState(lose, false, "3차 승리 - 패배 묶음 잔상");

            popup.InitUI(ResultArg(BattleResult.None));
            AssertDecorState(win, false, "4차 None - 승리 묶음 잔상");
            AssertDecorState(lose, false, "4차 None - 패배 묶음 잔상");
        }

        [Test]
        public void ResultPopup_InitUI에_ResultPopupArg가_아닌_인자를_주면_묶음_상태를_건드리지_않는다()
        {
            ResultPopup popup = NewResultPopup();
            popup.InitUI(ResultArg(BattleResult.Win));

            Assert.DoesNotThrow(() => popup.InitUI(null));

            AssertDecorState(Required<GameObject[]>(popup, "_winDecor"), true, "승리 묶음 유지");
            AssertDecorState(Required<GameObject[]>(popup, "_loseDecor"), false, "패배 묶음 유지");
        }

        [Test]
        public void ResultPopup_장식_배열이_null_빈_배열_원소_null이어도_InitUI가_예외없다()
        {
            ResultPopup popup = NewResultPopup();
            GameObject[] originalWin = Required<GameObject[]>(popup, "_winDecor");
            GameObject[] originalLose = Required<GameObject[]>(popup, "_loseDecor");

            SetField(popup, "_winDecor", null);
            SetField(popup, "_loseDecor", null);
            Assert.DoesNotThrow(() => popup.InitUI(ResultArg(BattleResult.Win)), "배열 null");

            SetField(popup, "_winDecor", new GameObject[0]);
            SetField(popup, "_loseDecor", new GameObject[0]);
            Assert.DoesNotThrow(() => popup.InitUI(ResultArg(BattleResult.Lose)), "빈 배열");

            SetField(popup, "_winDecor", new GameObject[] { null, originalWin[1] });
            SetField(popup, "_loseDecor", new GameObject[] { originalLose[0], null });
            Assert.DoesNotThrow(() => popup.InitUI(ResultArg(BattleResult.Win)), "원소 null");
            Assert.IsTrue(originalWin[1].activeSelf, "null 원소를 건너뛰고 나머지는 토글");
            Assert.IsFalse(originalLose[0].activeSelf, "null 원소를 건너뛰고 나머지는 토글");
        }

        [Test]
        public void ResultPopup_기존_필드_14개와_버튼_2개가_유지된다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));
            string[] names =
            {
                "_resultText", "_rewardText", "_subText", "_rowClear", "_clearTimeText", "_newBadge", "_rowSouls", "_soulsText",
                "_rowXp", "_xpText", "_rowHeroHp", "_heroHpText", "_rays", "_retryButton", "_villageButton",
            };

            foreach (string name in names)
            {
                Required<UnityEngine.Object>(popup, name);
            }
        }

        //# --- ③ ToastView 종류 틴트 ---

        [Test]
        public void ToastView_kindTintImages가_길이_3과_기획서_이름_순서_프리팹_알파로_연결된다()
        {
            ToastView toast = Comp<ToastView>(LoadPrefabAsset(ToastPath));

            Image[] tint = Required<Image[]>(toast, "_kindTintImages");
            Assert.AreEqual(3, tint.Length);
            for (int i = 0; i < tint.Length; ++i)
            {
                Assert.IsTrue(tint[i] != null, $"_kindTintImages[{i}] 비어 있음");
                Assert.AreEqual(ToastTintNames[i], tint[i].name, $"_kindTintImages[{i}]");
                Assert.AreEqual(ToastTintAlphas[i], tint[i].color.a, 0.01f, tint[i].name + " 프리팹 알파");
            }
        }

        [Test]
        public void ToastView_기존_필드_3개가_유지된다()
        {
            ToastView toast = Comp<ToastView>(LoadPrefabAsset(ToastPath));

            Required<UnityEngine.Object>(toast, "_messageText");
            Required<UnityEngine.Object>(toast, "_canvasGroup");
            Required<UnityEngine.Object>(toast, "_dot");
        }

        [TestCase(EToastKind.Info)]
        [TestCase(EToastKind.Warning)]
        [TestCase(EToastKind.Error)]
        public void ToastView_SetKind는_3개_장식과_점을_종류색으로_칠하고_알파를_보존한다(EToastKind kind)
        {
            ToastView toast = NewToast();
            Image[] tint = Required<Image[]>(toast, "_kindTintImages");
            Image dot = Required<Image>(toast, "_dot");
            float[] alphas = new float[tint.Length];
            for (int i = 0; i < tint.Length; ++i)
            {
                alphas[i] = tint[i].color.a;
            }

            Invoke(toast, "SetKind", kind);

            Color expected = UiDotPalette.ToastDot(kind);
            for (int i = 0; i < tint.Length; ++i)
            {
                AssertRgb(expected, tint[i].color, tint[i].name);
                Assert.AreEqual(alphas[i], tint[i].color.a, 0.001f, tint[i].name + " 알파 보존");
            }
            AssertRgb(expected, dot.color, "Dot");
        }

        [Test]
        public void ToastView_종류를_바꿔_반복_호출해도_알파가_변하지_않고_색은_마지막_종류를_따른다()
        {
            ToastView toast = NewToast();
            Image[] tint = Required<Image[]>(toast, "_kindTintImages");
            float[] alphas = new float[tint.Length];
            for (int i = 0; i < tint.Length; ++i)
            {
                alphas[i] = tint[i].color.a;
            }

            EToastKind[] sequence = { EToastKind.Error, EToastKind.Info, EToastKind.Warning, EToastKind.Warning, EToastKind.Error, EToastKind.Info };
            foreach (EToastKind kind in sequence)
            {
                Invoke(toast, "SetKind", kind);
                for (int i = 0; i < tint.Length; ++i)
                {
                    AssertRgb(UiDotPalette.ToastDot(kind), tint[i].color, $"{kind} {tint[i].name}");
                    Assert.AreEqual(alphas[i], tint[i].color.a, 0.001f, $"{kind} {tint[i].name} 알파");
                }
            }
        }

        [Test]
        public void ToastView_dot이_비어_있어도_틴트는_수행된다()
        {
            ToastView toast = NewToast();
            Image[] tint = Required<Image[]>(toast, "_kindTintImages");
            SetField(toast, "_dot", null);

            Assert.DoesNotThrow(() => Invoke(toast, "SetKind", EToastKind.Error));

            foreach (Image image in tint)
            {
                AssertRgb(UiDotPalette.Blood, image.color, image.name);
            }
        }

        [Test]
        public void ToastView_kindTintImages가_null이거나_원소가_null이어도_SetKind가_예외없다()
        {
            ToastView toast = NewToast();
            Image dot = Required<Image>(toast, "_dot");
            Image[] original = Required<Image[]>(toast, "_kindTintImages");

            SetField(toast, "_kindTintImages", null);
            Assert.DoesNotThrow(() => Invoke(toast, "SetKind", EToastKind.Warning), "배열 null");
            AssertRgb(UiDotPalette.Gold, dot.color, "배열 null 이어도 점은 칠함");

            SetField(toast, "_kindTintImages", new Image[0]);
            Assert.DoesNotThrow(() => Invoke(toast, "SetKind", EToastKind.Info), "빈 배열");

            SetField(toast, "_kindTintImages", new Image[] { null, original[1], null });
            Assert.DoesNotThrow(() => Invoke(toast, "SetKind", EToastKind.Error), "원소 null");
            AssertRgb(UiDotPalette.Blood, original[1].color, "null 원소를 건너뛰고 나머지는 칠함");
        }

        [Test]
        public void ToastView_모든_필드가_비어_있어도_SetKind가_예외없다()
        {
            ToastView toast = NewToast();
            SetField(toast, "_dot", null);
            SetField(toast, "_kindTintImages", null);

            Assert.DoesNotThrow(() => Invoke(toast, "SetKind", EToastKind.Info));
        }

        //# --- ④ Deco* raycastTarget ---

        [TestCase(ResultPath)]
        [TestCase(ConfirmPath)]
        [TestCase(ToastPath)]
        [TestCase(BannerPath)]
        public void Deco로_시작하는_오브젝트의_Graphic은_raycastTarget이_꺼져_있다(string path)
        {
            int decoCount = 0;
            foreach (Graphic graphic in LoadPrefabAsset(path).GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.name.StartsWith("Deco") == false)
                    continue;

                ++decoCount;
                Assert.IsFalse(graphic.raycastTarget, $"{path} {graphic.name} raycastTarget");
            }
            Assert.Greater(decoCount, 0, path + " Deco Graphic 수집 확인");
        }

        //# --- ⑤ UILoopMotion ---

        private static UILoopMotion[] MotionsOf(string path)
        {
            return LoadPrefabAsset(path).GetComponentsInChildren<UILoopMotion>(true);
        }

        private static UILoopMotion MotionOn(string path, string objectName)
        {
            foreach (UILoopMotion motion in MotionsOf(path))
            {
                if (motion.name == objectName)
                    return motion;
            }
            Assert.Fail($"{path} 의 {objectName} 에 UILoopMotion 없음");
            return null;
        }

        private static void AssertPulse(UILoopMotion motion, float period, float alphaFrom, float alphaTo)
        {
            Assert.AreEqual(UILoopMotion.EMotionKind.Pulse, (UILoopMotion.EMotionKind)Field(motion, "_kind"), motion.name + " 종류");
            Assert.AreEqual(period, (float)Field(motion, "_period"), 0.0001f, motion.name + " 주기");
            Assert.AreEqual(alphaFrom, (float)Field(motion, "_alphaFrom"), 0.0001f, motion.name + " alphaFrom");
            Assert.AreEqual(alphaTo, (float)Field(motion, "_alphaTo"), 0.0001f, motion.name + " alphaTo");
            Assert.AreEqual(1f, (float)Field(motion, "_scaleFrom"), 0.0001f, motion.name + " scaleFrom");
            Assert.AreEqual(1f, (float)Field(motion, "_scaleTo"), 0.0001f, motion.name + " scaleTo");
            Graphic graphic = Required<Graphic>(motion, "_graphic");
            Assert.AreSame(motion.GetComponent<Image>(), graphic, motion.name + " _graphic 은 자기 Image");
        }

        private static void AssertNestedCanvasNoOverride(Component target)
        {
            Canvas canvas = target.GetComponent<Canvas>();
            Assert.IsTrue(canvas != null, target.name + " 중첩 Canvas 없음");
            Assert.IsFalse(canvas.overrideSorting, target.name + " Override Sorting");
        }

        [Test]
        public void ResultPopup_UILoopMotion은_정확히_2개이고_Rays_Rotate_와_TitleGlow_Pulse다()
        {
            UILoopMotion[] motions = MotionsOf(ResultPath);
            Assert.AreEqual(2, motions.Length, "UILoopMotion 개수");

            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));
            GameObject rays = Required<GameObject>(popup, "_rays");
            UILoopMotion raysMotion = rays.GetComponent<UILoopMotion>();
            Assert.IsTrue(raysMotion != null, "Rays 에 UILoopMotion");
            Assert.AreEqual(UILoopMotion.EMotionKind.Rotate, (UILoopMotion.EMotionKind)Field(raysMotion, "_kind"), "Rays 종류");
            Assert.AreEqual(-6f, (float)Field(raysMotion, "_degreesPerSecond"), 0.0001f, "Rays 회전 속도");

            UILoopMotion glow = MotionOn(ResultPath, "DecoWinTitleGlow");
            AssertPulse(glow, 2.4f, 0.40f, 0.70f);
            Assert.AreEqual("DecoWinFront", glow.transform.parent.name, "TitleGlow 는 DecoWinFront 자식");
        }

        [Test]
        public void ResultPopup_패배_TitleGlow에는_UILoopMotion이_없다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));
            GameObject[] lose = Required<GameObject[]>(popup, "_loseDecor");

            Assert.AreEqual(0, lose[0].GetComponentsInChildren<UILoopMotion>(true).Length, "DecoLoseBack");
            Assert.AreEqual(0, lose[1].GetComponentsInChildren<UILoopMotion>(true).Length, "DecoLoseFront");
        }

        [Test]
        public void ResultPopup_모션_대상_2곳은_중첩_Canvas가_있고_OverrideSorting이_꺼져_있다()
        {
            ResultPopup popup = Comp<ResultPopup>(LoadPrefabAsset(ResultPath));

            AssertNestedCanvasNoOverride(Required<GameObject>(popup, "_rays").transform);
            AssertNestedCanvasNoOverride(MotionOn(ResultPath, "DecoWinTitleGlow"));
        }

        [Test]
        public void SkillUnlockBanner_UILoopMotion은_정확히_1개이고_TagGlow_Pulse다()
        {
            UILoopMotion[] motions = MotionsOf(BannerPath);
            Assert.AreEqual(1, motions.Length, "UILoopMotion 개수");

            UILoopMotion glow = MotionOn(BannerPath, "DecoTagGlow");
            AssertPulse(glow, 0.9f, 0.45f, 0.75f);
        }

        [Test]
        public void ConfirmPopup과_ToastView에는_UILoopMotion이_없다()
        {
            Assert.AreEqual(0, MotionsOf(ConfirmPath).Length, "ConfirmPopup");
            Assert.AreEqual(0, MotionsOf(ToastPath).Length, "ToastView");
        }

        [Test]
        public void 네_프리팹의_모든_중첩_Canvas는_OverrideSorting이_꺼져_있다()
        {
            foreach (string path in new[] { ResultPath, ConfirmPath, ToastPath, BannerPath })
            {
                foreach (Canvas canvas in LoadPrefabAsset(path).GetComponentsInChildren<Canvas>(true))
                {
                    if (canvas.transform == canvas.transform.root)
                        continue;

                    Assert.IsFalse(canvas.overrideSorting, $"{path} {canvas.name} 중첩 Canvas Override Sorting");
                }
            }
        }

        //# --- ⑥ 기존 필드 회귀 · 배너 슬라이드 값 ---

        [Test]
        public void ConfirmPopup_기존_필드_4개가_유지된다()
        {
            ConfirmPopup popup = Comp<ConfirmPopup>(LoadPrefabAsset(ConfirmPath));

            foreach (string name in new[] { "_titleText", "_messageText", "_confirmButton", "_cancelButton" })
            {
                Required<UnityEngine.Object>(popup, name);
            }
        }

        [Test]
        public void SkillUnlockBanner_기존_필드_2개가_유지되고_슬라이드_값이_불변이다()
        {
            SkillUnlockBannerView banner = Comp<SkillUnlockBannerView>(LoadPrefabAsset(BannerPath));

            Required<UnityEngine.Object>(banner, "_root");
            Required<UnityEngine.Object>(banner, "_label");
            Assert.AreEqual(0.35f, (float)Field(banner, "_slideInDuration"), 0.0001f, "_slideInDuration");
            Assert.AreEqual(1.2f, (float)Field(banner, "_holdDuration"), 0.0001f, "_holdDuration");
            Assert.AreEqual(0.35f, (float)Field(banner, "_slideOutDuration"), 0.0001f, "_slideOutDuration");
            Assert.AreEqual(1300f, (float)Field(banner, "_offscreenX"), 0.0001f, "_offscreenX");
        }

        //# --- ⑦ 일회용 빌더 없음 ---

        [Test]
        public void 일회용_장식_빌더_스크립트가_레포에_없다()
        {
            string editorDir = Path.GetFullPath(Path.Combine(Application.dataPath, "_Lair", "Editor"));
            Assert.IsTrue(Directory.Exists(editorDir), editorDir);

            Assert.IsFalse(File.Exists(Path.Combine(editorDir, "BatchADecoBuilder.cs")), "BatchADecoBuilder.cs 잔존");
            string[] batchA = Directory.GetFiles(editorDir, "*BatchA*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, batchA.Length, "BatchA 에디터 스크립트 잔존: " + string.Join(", ", batchA));
            string[] decoBuilders = Directory.GetFiles(editorDir, "*Deco*Builder*.cs", SearchOption.AllDirectories);
            Assert.AreEqual(0, decoBuilders.Length, "Deco 빌더 잔존: " + string.Join(", ", decoBuilders));
        }
    }
}
