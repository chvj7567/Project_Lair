using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Lair.Battle;
using Lair.Data;
using Lair.Tests.EditMode;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Lair.Tests.UI
{
    //# 전 씬 전 UI 해상도 대응 검증 게이트 T1~T19 — 기획서 resolution-adaptation.md §11.1.
    //# 씬·프리팹의 직렬화 값을 읽어 M1~M7 + 조건 사례(16:9, 하단 인셋 63px)에서의 배치를 계산한다. 저장·Undo 없음.
    public class ResolutionAdaptationTests
    {
        private const string UiDir = "Assets/_Lair/Art/UI";
        private const float Eps = 0.01f;

        private enum Kind { Village, Battle, Loading }

        private readonly struct Device
        {
            public readonly string Name;
            public readonly int W;
            public readonly int H;
            public readonly float InsetL;
            public readonly float InsetB;

            public Device(string name, int w, int h, float insetL, float insetB)
            {
                Name = name;
                W = w;
                H = h;
                InsetL = insetL;
                InsetB = insetB;
            }
        }

        //# 캔버스 논리 좌표(좌하단 원점)의 박스 크기와 안전영역.
        private readonly struct Frame
        {
            public readonly float Cw;
            public readonly float Ch;
            public readonly Rect Safe;

            public Frame(float cw, float ch, Rect safe)
            {
                Cw = cw;
                Ch = ch;
                Safe = safe;
            }
        }

        //# §3.1 M1~M7 + 조건 사례(마지막). 인셋은 px(좌, 아래).
        private static readonly Device[] Devices =
        {
            new Device("M1 1920x1080", 1920, 1080, 0f, 0f),
            new Device("M2 2340x1080", 2340, 1080, 132f, 63f),
            new Device("M3 2400x1080", 2400, 1080, 132f, 63f),
            new Device("M4 2520x1080", 2520, 1080, 132f, 63f),
            new Device("M5 2640x1080", 2640, 1080, 132f, 63f),
            new Device("M6 1920x1200", 1920, 1200, 0f, 30f),
            new Device("M7 2048x1536", 2048, 1536, 0f, 40f),
            new Device("조건사례 1920x1080 하단63", 1920, 1080, 0f, 63f),
        };

        private static readonly Vector2[] VillageLogical =
        {
            new Vector2(1280f, 720f), new Vector2(1560f, 720f), new Vector2(1600f, 720f), new Vector2(1680f, 720f),
            new Vector2(1760f, 720f), new Vector2(1280f, 800f), new Vector2(1280f, 960f), new Vector2(1280f, 720f),
        };

        private static readonly Vector2[] BattleLogical =
        {
            new Vector2(1280f, 720f), new Vector2(1560f, 720f), new Vector2(1600f, 720f), new Vector2(1680f, 720f),
            new Vector2(1680f, 720f), new Vector2(1280f, 720f), new Vector2(1280f, 720f), new Vector2(1280f, 720f),
        };

        private static readonly string[] MenuNames =
        {
            "LordButton", "ShopButton", "CodexButton", "QuestButton", "RecordsButton", "RankingButton", "RelicButton", "DailyButton",
        };

        private static readonly string[] VillageZoneNames =
        {
            "TopBar", "LordButton", "ShopButton", "CodexButton", "QuestButton", "RecordsButton", "RankingButton", "RelicButton", "DailyButton",
            "RecordPanel", "StagePanel", "HeroButton", "SortieButton", "TowerButton", "StagePrevButton", "StageNextButton",
        };

        private static readonly string[] BattleZoneNames =
        {
            "SpeedToggle", "ExitButton", "TimerPlate", "BossHpBar", "ActiveCountdown", "BuildSynergyPanel", "BuildPanel", "SpawnerStatusPanel",
        };

        private static readonly string[] LoadingElementNames =
        {
            "TitleSmall", "TitleMain", "Tagline", "DescText", "ProgressBarBg", "PercentText",
        };

        private static readonly string[] VillagePopups =
        {
            "ShopPopup", "QuestPopup", "CodexPopup", "RecordsPopup", "HeroSelectPopup", "LordLevelPopup", "RankingPopup",
            "CloudPopup", "WorldMapPopup", "RelicPopup", "FormationPopup", "TowerPopup", "DailyPopup",
        };

        private static readonly string[] BattlePopups =
        {
            "ResultPopup", "CardSelectionPopup", "BuildModalPopup", "SynergyModalPopup",
        };

        private const string ConfirmPopup = "ConfirmPopup";

        //# §2 인벤토리 43개 — EUI 22 + 보조 3 + 셀 18.
        private static readonly string[] Inventory =
        {
            "VillageHud", "ShopPopup", "QuestPopup", "CodexPopup", "RecordsPopup", "HeroSelectPopup", "LordLevelPopup", "RankingPopup",
            "CloudPopup", "WorldMapPopup", "RelicPopup", "FormationPopup", "TowerPopup", "DailyPopup", "BattleHud", "ResultPopup",
            "CardSelectionPopup", "BuildModalPopup", "SynergyModalPopup", "SkillUnlockBanner", "ConfirmPopup", "ToastView",
            "BuildSynergyPanel", "SpawnerStatusPanel", "HpBar",
            "BuildIconCell", "BuildModalCardCell", "BuildSynergyCell", "SpawnerStatusCell", "SynergyModalCell", "CodexCell",
            "HeroSelectCell", "LordRewardCell", "QuestCell", "RankingCell", "RecordsStageCell", "RelicCell", "ShopItemCell",
            "WorldMapChapterTab", "WorldMapNode", "FormationSpeciesRow", "FormationNestTile", "TowerFloorCell",
        };

        private Scene _village;
        private Scene _battle;
        private Scene _loading;
        private readonly List<Scene> _opened = new List<Scene>();
        private GameObject _villageHud;
        private GameObject _battleHud;
        private readonly Dictionary<string, GameObject> _popups = new Dictionary<string, GameObject>();

        [OneTimeSetUp]
        public void Open()
        {
            //# 씬 여러 개를 동시에 열면 URP 2D 전역 라이트 중복 에러 로그가 나므로 이 구간만 로그 실패를 끈다.
            LogAssert.ignoreFailingMessages = true;
            _village = OpenOrReuse(DotSceneTestUtil.VillageScene);
            _battle = OpenOrReuse(DotSceneTestUtil.BattleScene);
            _loading = OpenOrReuse(DotSceneTestUtil.LoadingScene);
            LogAssert.ignoreFailingMessages = false;
            _villageHud = PrefabUtility.LoadPrefabContents(UiDir + "/VillageHud.prefab");
            _battleHud = PrefabUtility.LoadPrefabContents(UiDir + "/BattleHud.prefab");
            foreach (string n in AllPopups())
            {
                _popups[n] = PrefabUtility.LoadPrefabContents(UiDir + "/" + n + ".prefab");
            }
        }

        [OneTimeTearDown]
        public void Close()
        {
            PrefabUtility.UnloadPrefabContents(_villageHud);
            PrefabUtility.UnloadPrefabContents(_battleHud);
            foreach (GameObject p in _popups.Values)
            {
                PrefabUtility.UnloadPrefabContents(p);
            }
            _popups.Clear();
            //# 우리가 연 씬만 닫는다 — 이미 열려 있던 사용자의 씬은 건드리지 않는다.
            foreach (Scene s in _opened)
            {
                DotSceneTestUtil.Close(s);
            }
            _opened.Clear();
        }

        private Scene OpenOrReuse(string path)
        {
            Scene s = SceneManager.GetSceneByPath(path);
            if (s.isLoaded)
                return s;

            s = DotSceneTestUtil.Open(path);
            _opened.Add(s);
            return s;
        }

        private static IEnumerable<string> AllPopups()
        {
            foreach (string n in VillagePopups)
            {
                yield return n;
            }
            foreach (string n in BattlePopups)
            {
                yield return n;
            }
            yield return ConfirmPopup;
        }

        //# 팝업이 뜨는 씬 종류 — 마을 13 / 배틀 4 / 공용 Confirm 은 둘 다.
        private static Kind[] KindsOf(string popup)
        {
            if (Array.IndexOf(VillagePopups, popup) >= 0)
                return new[] { Kind.Village };
            if (Array.IndexOf(BattlePopups, popup) >= 0)
                return new[] { Kind.Battle };
            return new[] { Kind.Village, Kind.Battle };
        }

        //# ---------- 계산 헬퍼 ----------

        private static Frame ComputeFrame(Device d, Kind kind)
        {
            Vector2 reference = kind == Kind.Loading ? new Vector2(1920f, 1080f) : new Vector2(1280f, 720f);
            Rect box = new Rect(0f, 0f, d.W, d.H);
            if (kind == Kind.Battle)
            {
                box = ToPixels(LetterboxFramer.ComputeViewportRect(d.W, d.H, 16f / 9f, 7f / 3f), d);
            }
            else if (kind == Kind.Loading)
            {
                box = ToPixels(LetterboxFramer.ComputeViewportRect(d.W, d.H, 16f / 9f, 0f), d);
            }

            float scale = Mathf.Min(box.width / reference.x, box.height / reference.y);
            float cw = box.width / scale;
            float ch = box.height / scale;
            Rect safePx = new Rect(d.InsetL, d.InsetB, d.W - d.InsetL, d.H - d.InsetB);
            SafeAreaFitter.ComputeAnchors(safePx, box, out Vector2 aMin, out Vector2 aMax);
            Rect safe = new Rect(aMin.x * cw, aMin.y * ch, (aMax.x - aMin.x) * cw, (aMax.y - aMin.y) * ch);
            return new Frame(cw, ch, safe);
        }

        private static Rect ToPixels(Rect n, Device d)
        {
            return new Rect(n.x * d.W, n.y * d.H, n.width * d.W, n.height * d.H);
        }

        //# 앵커·피벗·scale 을 반영해 자식 사각형을 부모 사각형(좌하단 원점) 안에서 푼다.
        private static Rect ResolveInParent(RectTransform rt, Rect parent)
        {
            Vector2 aMin = rt.anchorMin;
            Vector2 aMax = rt.anchorMax;
            Vector2 pivot = rt.pivot;
            Vector2 size = new Vector2((aMax.x - aMin.x) * parent.width + rt.sizeDelta.x, (aMax.y - aMin.y) * parent.height + rt.sizeDelta.y);
            Vector2 refPoint = new Vector2(parent.x + Mathf.Lerp(aMin.x, aMax.x, pivot.x) * parent.width, parent.y + Mathf.Lerp(aMin.y, aMax.y, pivot.y) * parent.height);
            Vector2 pivotPos = refPoint + rt.anchoredPosition;
            Vector2 scaled = new Vector2(size.x * Mathf.Abs(rt.localScale.x), size.y * Mathf.Abs(rt.localScale.y));
            return new Rect(pivotPos.x - scaled.x * pivot.x, pivotPos.y - scaled.y * pivot.y, scaled.x, scaled.y);
        }

        private static Rect ResolveChain(RectTransform rt, Transform root, Rect rootRect)
        {
            if (rt == root)
                return rootRect;

            Rect parent = ResolveChain((RectTransform)rt.parent, root, rootRect);
            return ResolveInParent(rt, parent);
        }

        private static Transform FindDeep(Transform t, string name)
        {
            if (t.name == name)
                return t;

            foreach (Transform c in t)
            {
                Transform f = FindDeep(c, name);
                if (f != null)
                    return f;
            }
            return null;
        }

        private static RectTransform FindRectInScene(Scene scene, string name)
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                Transform t = FindDeep(root.transform, name);
                if (t != null)
                    return t as RectTransform;
            }
            return null;
        }

        private static Dictionary<string, Rect> MeasureZones(GameObject root, string[] names, Vector2 hostSize)
        {
            Dictionary<string, Rect> result = new Dictionary<string, Rect>();
            Rect host = new Rect(0f, 0f, hostSize.x, hostSize.y);
            foreach (string n in names)
            {
                RectTransform rt = FindDeep(root.transform, n) as RectTransform;
                Assert.IsNotNull(rt, root.name + " 에 " + n + " 없음");
                result[n] = ResolveChain(rt, root.transform, host);
            }
            return result;
        }

        private static bool Overlaps(Rect a, Rect b)
        {
            return Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin) > Eps
                && Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin) > Eps;
        }

        private static bool Contains(Rect outer, Rect inner)
        {
            return inner.xMin >= outer.xMin - Eps && inner.xMax <= outer.xMax + Eps
                && inner.yMin >= outer.yMin - Eps && inner.yMax <= outer.yMax + Eps;
        }

        private static void CheckPairs(Dictionary<string, Rect> rects, string[] sameGroup, string ctx, List<string> failures)
        {
            List<string> keys = new List<string>(rects.Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                for (int j = i + 1; j < keys.Count; j++)
                {
                    if (sameGroup != null && Array.IndexOf(sameGroup, keys[i]) >= 0 && Array.IndexOf(sameGroup, keys[j]) >= 0)
                        continue;

                    if (Overlaps(rects[keys[i]], rects[keys[j]]))
                    {
                        failures.Add(ctx + ": " + keys[i] + " x " + keys[j] + " 겹침 " + rects[keys[i]] + " / " + rects[keys[j]]);
                    }
                }
            }
        }

        private static void AssertNone(List<string> failures)
        {
            Assert.AreEqual(0, failures.Count, string.Join("\n", failures));
        }

        private static Dictionary<string, Rect> Shift(Dictionary<string, Rect> local, Vector2 offset)
        {
            Dictionary<string, Rect> r = new Dictionary<string, Rect>();
            foreach (KeyValuePair<string, Rect> kv in local)
            {
                r[kv.Key] = new Rect(kv.Value.x + offset.x, kv.Value.y + offset.y, kv.Value.width, kv.Value.height);
            }
            return r;
        }

        //# 인셋 띠 4개(좌·우·하·상) — 안전영역 바깥 캔버스 영역. 폭/높이 0 이하는 교차하지 않는다.
        private static Rect[] BandsOf(Frame f)
        {
            return new[]
            {
                new Rect(0f, 0f, f.Safe.xMin, f.Ch),
                new Rect(f.Safe.xMax, 0f, f.Cw - f.Safe.xMax, f.Ch),
                new Rect(0f, 0f, f.Cw, f.Safe.yMin),
                new Rect(0f, f.Safe.yMax, f.Cw, f.Ch - f.Safe.yMax),
            };
        }

        //# UI 사각형 x 인셋 띠 쌍 검사(T16) — 교차하는 쌍을 문자열로 돌려준다.
        private static List<string> BandHits(Dictionary<string, Rect> rects, Frame f, string ctx)
        {
            string[] bandNames = { "좌띠", "우띠", "하띠", "상띠" };
            Rect[] bands = BandsOf(f);
            List<string> hits = new List<string>();
            foreach (KeyValuePair<string, Rect> kv in rects)
            {
                for (int b = 0; b < bands.Length; b++)
                {
                    if (Overlaps(kv.Value, bands[b]))
                    {
                        hits.Add(ctx + ": " + kv.Key + " x " + bandNames[b] + " " + kv.Value + " / " + bands[b]);
                    }
                }
            }
            return hits;
        }

        private static bool IsStretch(RectTransform rt)
        {
            return Vector2.Distance(rt.anchorMin, rt.anchorMax) > Eps;
        }

        private static bool IsDeco(Transform t)
        {
            return t.name.StartsWith("Deco");
        }

        //# 루트 직계 자식 중 스트레치가 아니고 Deco 가 아니며 켜져 있는 것 중 면적이 가장 큰 것 = 패널 본체.
        private static RectTransform LargestPanel(GameObject popup)
        {
            RectTransform best = null;
            float bestArea = -1f;
            foreach (Transform c in popup.transform)
            {
                RectTransform rt = c as RectTransform;
                if (rt == null || IsStretch(rt) || IsDeco(rt) || rt.gameObject.activeSelf == false)
                    continue;

                float area = rt.sizeDelta.x * rt.sizeDelta.y;
                if (area > bestArea)
                {
                    bestArea = area;
                    best = rt;
                }
            }
            return best;
        }

        private static RectTransform FindDim(GameObject popup)
        {
            foreach (MonoBehaviour mb in popup.GetComponents<MonoBehaviour>())
            {
                if (mb == null)
                    continue;

                for (Type ty = mb.GetType(); ty != null; ty = ty.BaseType)
                {
                    foreach (string field in new[] { "_dimButton", "_backgroundButton" })
                    {
                        FieldInfo fi = ty.GetField(field, BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly);
                        if (fi == null)
                            continue;

                        Component c = fi.GetValue(mb) as Component;
                        if (c != null && c.transform != popup.transform)
                            return c.transform as RectTransform;
                    }
                }
            }
            return popup.transform.Find("Dim") as RectTransform;
        }

        private static object Field(object target, string name)
        {
            return PopupWiringAsserts.GetPrivateField(target, name);
        }

        private static CanvasScaler FindScaler(Scene scene, string goName)
        {
            foreach (CanvasScaler cs in DotSceneTestUtil.FindAll<CanvasScaler>(scene))
            {
                if (cs.gameObject.name == goName)
                    return cs;
            }
            return null;
        }

        private Dictionary<string, Rect> LoadingRects(Rect panelRect)
        {
            RectTransform panel = FindRectInScene(_loading, "Panel");
            Assert.IsNotNull(panel, "Loading 에 Panel 없음");
            Dictionary<string, Rect> r = new Dictionary<string, Rect>();
            foreach (string n in LoadingElementNames)
            {
                RectTransform rt = FindDeep(panel, n) as RectTransform;
                Assert.IsNotNull(rt, "Loading Panel 에 " + n + " 없음");
                r[n] = ResolveInParent(rt, panelRect);
            }
            return r;
        }

        //# ---------- T1 ----------

        [Test]
        public void T1_스케일러_3씬_모두_Expand이고_기준_해상도가_맞다()
        {
            CanvasScaler v = FindScaler(_village, "UICanvas");
            CanvasScaler b = FindScaler(_battle, "UICanvas");
            CanvasScaler l = FindScaler(_loading, "Canvas");
            Assert.IsNotNull(v, "Village UICanvas CanvasScaler");
            Assert.IsNotNull(b, "Battle UICanvas CanvasScaler");
            Assert.IsNotNull(l, "Loading Canvas CanvasScaler");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, v.screenMatchMode, "Village");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, b.screenMatchMode, "Battle");
            Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, l.screenMatchMode, "Loading");
            Assert.AreEqual(new Vector2(1280f, 720f), v.referenceResolution, "Village 기준");
            Assert.AreEqual(new Vector2(1280f, 720f), b.referenceResolution, "Battle 기준");
            Assert.AreEqual(new Vector2(1920f, 1080f), l.referenceResolution, "Loading 기준");
        }

        //# ---------- T2 ----------

        [Test]
        public void T2_논리_크기_마을_배틀_로딩이_3_1_매트릭스와_일치한다()
        {
            List<string> f = new List<string>();
            for (int i = 0; i < Devices.Length; i++)
            {
                Frame v = ComputeFrame(Devices[i], Kind.Village);
                Frame b = ComputeFrame(Devices[i], Kind.Battle);
                Frame l = ComputeFrame(Devices[i], Kind.Loading);
                if (Mathf.Abs(v.Cw - VillageLogical[i].x) > 0.5f || Mathf.Abs(v.Ch - VillageLogical[i].y) > 0.5f)
                {
                    f.Add(Devices[i].Name + " 마을 논리 " + v.Cw + "x" + v.Ch + " != " + VillageLogical[i]);
                }
                if (v.Cw < 1280f - 0.5f || v.Ch < 720f - 0.5f)
                {
                    f.Add(Devices[i].Name + " 마을 논리가 1280x720 미만 " + v.Cw + "x" + v.Ch);
                }
                if (Mathf.Abs(b.Cw - BattleLogical[i].x) > 0.5f || Mathf.Abs(b.Ch - BattleLogical[i].y) > 0.5f)
                {
                    f.Add(Devices[i].Name + " 배틀 논리 " + b.Cw + "x" + b.Ch + " != " + BattleLogical[i]);
                }
                if (Mathf.Abs(l.Cw - 1920f) > 0.1f || Mathf.Abs(l.Ch - 1080f) > 0.1f)
                {
                    f.Add(Devices[i].Name + " 로딩 논리 " + l.Cw + "x" + l.Ch + " != 1920x1080");
                }
            }
            AssertNone(f);
        }

        [Test]
        public void T2_인셋의_박스_안_논리값이_3_1_1_표와_일치한다()
        {
            //# (마을 좌, 마을 아래, 배틀 좌, 배틀 아래, 로딩 좌, 로딩 아래) — M1~M7, 조건 사례.
            //# 배틀 M2~M4 는 풀 rect(띠 없음)라 좌 인셋 88 이 박스 안으로 들어온다(기획서 §3.1.1 표의 "좌 0 흡수"와 다름).
            float[][] expected =
            {
                new[] { 0f, 0f, 0f, 0f, 0f, 0f },
                new[] { 88f, 42f, 88f, 42f, 0f, 63f },
                new[] { 88f, 42f, 88f, 42f, 0f, 63f },
                new[] { 88f, 42f, 88f, 42f, 0f, 63f },
                new[] { 88f, 42f, 48f, 42f, 0f, 63f },
                new[] { 0f, 20f, 0f, 0f, 0f, 0f },
                new[] { 0f, 25f, 0f, 0f, 0f, 0f },
                new[] { 0f, 42f, 0f, 42f, 0f, 63f },
            };
            string[] kinds = { "마을", "배틀", "로딩" };
            List<string> f = new List<string>();
            for (int i = 0; i < Devices.Length; i++)
            {
                Rect[] safes =
                {
                    ComputeFrame(Devices[i], Kind.Village).Safe,
                    ComputeFrame(Devices[i], Kind.Battle).Safe,
                    ComputeFrame(Devices[i], Kind.Loading).Safe,
                };
                for (int k = 0; k < 3; k++)
                {
                    if (Mathf.Abs(safes[k].xMin - expected[i][k * 2]) > 0.5f || Mathf.Abs(safes[k].yMin - expected[i][k * 2 + 1]) > 0.5f)
                    {
                        f.Add(Devices[i].Name + " " + kinds[k] + " 인셋 (" + safes[k].xMin + "," + safes[k].yMin + ") != (" + expected[i][k * 2] + "," + expected[i][k * 2 + 1] + ")");
                    }
                }
            }
            AssertNone(f);
        }

        //# ---------- T3 ----------

        [Test]
        public void T3_마을_HudRoot_영역이_하한_1160x660_이상이다()
        {
            List<string> f = new List<string>();
            foreach (Device d in Devices)
            {
                Frame v = ComputeFrame(d, Kind.Village);
                if (v.Safe.width < 1160f - 0.5f || v.Safe.height < 660f - 0.5f)
                {
                    f.Add(d.Name + " HudRoot " + v.Safe.width + "x" + v.Safe.height);
                }
            }
            AssertNone(f);
        }

        //# ---------- T4 ----------

        [Test]
        public void T4_마을_HUD_존이_M1_M7_조건사례_하한에서_겹치지_않는다()
        {
            List<string> f = new List<string>();
            foreach (Device d in Devices)
            {
                Frame v = ComputeFrame(d, Kind.Village);
                CheckPairs(MeasureZones(_villageHud, VillageZoneNames, v.Safe.size), MenuNames, d.Name, f);
            }
            CheckPairs(MeasureZones(_villageHud, VillageZoneNames, new Vector2(1160f, 660f)), MenuNames, "하한 1160x660", f);
            AssertNone(f);
        }

        //# ---------- T5 ----------

        [Test]
        public void T5_하단_버튼_순서가_탑_지도_방어하기다()
        {
            Dictionary<string, Rect> r = MeasureZones(_villageHud, new[] { "TowerButton", "HeroButton", "SortieButton" }, new Vector2(1280f, 720f));
            Assert.Less(r["TowerButton"].center.x, r["HeroButton"].center.x, "TowerButton < HeroButton");
            Assert.Less(r["HeroButton"].center.x, r["SortieButton"].center.x, "HeroButton < SortieButton");
        }

        //# ---------- T6 ----------

        [Test]
        public void T6_인벤토리_순회_UI_프리팹이_분류_목록과_정확히_일치한다()
        {
            HashSet<string> expected = new HashSet<string>(Inventory);
            Assert.AreEqual(43, expected.Count, "분류 목록은 43개(EUI 22 + 보조 3 + 셀 18)");

            HashSet<string> actual = new HashSet<string>();
            foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { UiDir }))
            {
                actual.Add(Path.GetFileNameWithoutExtension(AssetDatabase.GUIDToAssetPath(guid)));
            }

            List<string> f = new List<string>();
            foreach (string a in actual)
            {
                if (expected.Contains(a) == false)
                {
                    f.Add("분류 안 된 UI 프리팹: " + a + " (resolution-adaptation §2 인벤토리에 추가 필요)");
                }
            }
            foreach (string e in expected)
            {
                if (actual.Contains(e) == false)
                {
                    f.Add("목록에만 있고 프리팹 없음: " + e);
                }
            }
            foreach (string n in Enum.GetNames(typeof(EUI)))
            {
                if (n != "SpawnerStatusTooltip" && expected.Contains(n) == false)
                {
                    f.Add("EUI 값이 분류 목록에 없음: " + n);
                }
            }
            AssertNone(f);
        }

        //# ---------- T7 ~ T10 ----------

        [Test]
        public void T7_팝업_앵커_순회_스트레치가_아닌_루트_직계_자식은_중앙_앵커다()
        {
            List<string> f = new List<string>();
            Vector2 half = new Vector2(0.5f, 0.5f);
            foreach (string n in AllPopups())
            {
                GameObject popup = _popups[n];
                RectTransform panel = LargestPanel(popup);
                if (panel == null)
                {
                    f.Add(n + ": 패널 후보 없음");
                    continue;
                }
                foreach (Transform c in popup.transform)
                {
                    RectTransform rt = (RectTransform)c;
                    if (IsStretch(rt))
                        continue;

                    if (Vector2.Distance(rt.anchorMin, half) > Eps || Vector2.Distance(rt.anchorMax, half) > Eps)
                    {
                        f.Add(n + "/" + rt.name + " 앵커 " + rt.anchorMin + rt.anchorMax + " != (0.5,0.5)");
                    }
                }
                if (Vector2.Distance(panel.pivot, half) > Eps)
                {
                    f.Add(n + "/" + panel.name + " 패널 피벗 " + panel.pivot + " != (0.5,0.5)");
                }
            }
            AssertNone(f);
        }

        [Test]
        public void T7_결과창_패널_y는_보정값_마이너스10_카드선택_y는_마이너스40이다()
        {
            Assert.AreEqual(-10f, LargestPanel(_popups["ResultPopup"]).anchoredPosition.y, Eps, "ResultPopup 패널 y");
            Assert.AreEqual(-40f, LargestPanel(_popups["CardSelectionPopup"]).anchoredPosition.y, Eps, "CardSelectionPopup 패널 y");
        }

        [Test]
        public void T8_팝업_패널_크기_순회_마을은_1136x640_배틀_공용은_1280x720_이하다()
        {
            List<string> f = new List<string>();
            foreach (string n in AllPopups())
            {
                bool village = Array.IndexOf(VillagePopups, n) >= 0;
                float maxW = village ? 1136f : 1280f;
                float maxH = village ? 640f : 720f;
                foreach (Transform c in _popups[n].transform)
                {
                    RectTransform rt = (RectTransform)c;
                    if (IsStretch(rt) || IsDeco(rt))
                        continue;

                    if (rt.sizeDelta.x > maxW + Eps || rt.sizeDelta.y > maxH + Eps)
                    {
                        f.Add(n + "/" + rt.name + " " + rt.sizeDelta + " > " + maxW + "x" + maxH);
                    }
                }
            }
            AssertNone(f);
        }

        [Test]
        public void T9_Dim_순회_마을12_배틀4는_스트레치_800x400이고_Confirm은_4800x2800_이상이다()
        {
            List<string> f = new List<string>();
            foreach (string n in AllPopups())
            {
                if (n == "HeroSelectPopup")
                    continue;

                RectTransform dim = FindDim(_popups[n]);
                if (dim == null)
                {
                    f.Add(n + ": Dim 없음");
                    continue;
                }
                if (n == ConfirmPopup)
                {
                    if (dim.sizeDelta.x < 4800f - Eps || dim.sizeDelta.y < 2800f - Eps)
                    {
                        f.Add(n + " Dim " + dim.sizeDelta + " < 4800x2800");
                    }
                    continue;
                }
                if (dim.anchorMin != Vector2.zero || dim.anchorMax != Vector2.one)
                {
                    f.Add(n + " Dim 앵커 " + dim.anchorMin + dim.anchorMax + " != (0,0)~(1,1)");
                }
                if (Vector2.Distance(dim.sizeDelta, new Vector2(800f, 400f)) > Eps)
                {
                    f.Add(n + " Dim sizeDelta " + dim.sizeDelta + " != (800,400)");
                }
            }
            AssertNone(f);
        }

        [Test]
        public void T10_패널_안전_순회_모든_패널이_PopupRoot_안전영역_안에_들어온다()
        {
            List<string> f = new List<string>();
            foreach (string n in AllPopups())
            {
                RectTransform panel = LargestPanel(_popups[n]);
                foreach (Kind k in KindsOf(n))
                {
                    foreach (Device d in Devices)
                    {
                        Frame fr = ComputeFrame(d, k);
                        Rect r = ResolveInParent(panel, fr.Safe);
                        if (Contains(fr.Safe, r) == false)
                        {
                            f.Add(n + "/" + panel.name + " " + k + " " + d.Name + " 패널 " + r + " 가 안전영역 " + fr.Safe + " 밖");
                        }
                    }
                }
            }
            AssertNone(f);
        }

        [Test]
        public void T10_M2_하단_인셋42에서_유물_본체_659_장식_글로우_670_7_결과창_675다()
        {
            Frame fr = ComputeFrame(Devices[1], Kind.Village);
            Frame fb = ComputeFrame(Devices[1], Kind.Battle);
            Assert.AreEqual(720f, fr.Ch, 0.5f, "마을 M2 캔버스 높이");
            Assert.AreEqual(42f, fr.Safe.yMin, 0.5f, "마을 M2 하단 인셋");

            GameObject relic = _popups["RelicPopup"];
            RectTransform body = FindDeep(relic.transform, "ModalBody") as RectTransform;
            RectTransform glow = FindDeep(relic.transform, "DecoModalGlow") as RectTransform;
            Assert.IsNotNull(body, "ModalBody");
            Assert.IsNotNull(glow, "DecoModalGlow");
            Assert.AreEqual(659f, fr.Ch - ResolveInParent(body, fr.Safe).yMin, 0.5f, "Relic ModalBody 아래 끝");
            float glowBottom = fr.Ch - ResolveInParent(glow, fr.Safe).yMin;
            Assert.AreEqual(670.7f, glowBottom, 0.1f, "Relic DecoModalGlow 아래 끝(scale 0.97)");
            Assert.LessOrEqual(glowBottom, 678f, "Relic DecoModalGlow ≤ 678");

            RectTransform result = LargestPanel(_popups["ResultPopup"]);
            Assert.AreEqual(675f, fb.Ch - ResolveInParent(result, fb.Safe).yMin, 0.5f, "ResultPopup 패널 아래 끝");
        }

        [Test]
        public void T10_유물_장식_글로우는_모든_비율에서_안전영역_안이다()
        {
            RectTransform glow = FindDeep(_popups["RelicPopup"].transform, "DecoModalGlow") as RectTransform;
            List<string> f = new List<string>();
            foreach (Device d in Devices)
            {
                Frame fr = ComputeFrame(d, Kind.Village);
                Rect r = ResolveInParent(glow, fr.Safe);
                if (Contains(fr.Safe, r) == false)
                {
                    f.Add(d.Name + " 글로우 " + r + " 가 안전영역 " + fr.Safe + " 밖");
                }
            }
            AssertNone(f);
        }

        //# ---------- T11 ----------

        [Test]
        public void T11_배틀_HUD_존이_M1_M7_조건사례에서_겹치지_않는다()
        {
            List<string> f = new List<string>();
            foreach (Device d in Devices)
            {
                Frame b = ComputeFrame(d, Kind.Battle);
                CheckPairs(MeasureZones(_battleHud, BattleZoneNames, b.Safe.size), null, d.Name, f);
            }
            AssertNone(f);
        }

        //# ---------- T12 ----------

        [Test]
        public void T12_마을_루트_배선_HudRoot_PopupRoot_컨트롤러_필드가_연결돼_있다()
        {
            VillageController ctrl = DotSceneTestUtil.Find<VillageController>(_village);
            Assert.IsNotNull(ctrl, "VillageController");
            RectTransform hud = Field(ctrl, "_hudRoot") as RectTransform;
            RectTransform popup = Field(ctrl, "_popupRoot") as RectTransform;
            Assert.IsTrue(hud != null, "_hudRoot null");
            Assert.IsTrue(popup != null, "_popupRoot null");
            Assert.AreEqual("HudRoot", hud.name);
            Assert.AreEqual("PopupRoot", popup.name);
            Assert.AreSame(hud.parent, popup.parent, "같은 부모(UICanvas) 아래");
            Assert.Less(hud.GetSiblingIndex(), popup.GetSiblingIndex(), "HudRoot 가 PopupRoot 앞 형제");
            AssertFitterWired(hud);
        }

        private static void AssertFitterWired(RectTransform root)
        {
            SafeAreaFitter fitter = root.GetComponent<SafeAreaFitter>();
            Assert.IsNotNull(fitter, root.name + " SafeAreaFitter");
            PopupWiringAsserts.AssertWired(fitter, "_gameCamera", root.name);
            Assert.IsTrue((Object)Field(fitter, "_target") == root, root.name + " _target 이 자신이 아님");
        }

        //# ---------- T13 / T14 ----------

        [Test]
        public void T13_로딩_레터박스는_타깃_16대9_맥스_0으로_불변이다()
        {
            LetterboxFramer lb = DotSceneTestUtil.Find<LetterboxFramer>(_loading);
            Assert.IsNotNull(lb, "Loading LetterboxFramer");
            Assert.AreEqual(1.7777778f, (float)Field(lb, "_targetAspect"), 1e-4f);
            Assert.AreEqual(0f, (float)Field(lb, "_maxAspect"), 1e-6f);
        }

        [Test]
        public void T14_배틀_레터박스는_타깃_16대9_맥스_7대3으로_불변이다()
        {
            LetterboxFramer lb = DotSceneTestUtil.Find<LetterboxFramer>(_battle);
            Assert.IsNotNull(lb, "Battle LetterboxFramer");
            Assert.AreEqual(1.7777778f, (float)Field(lb, "_targetAspect"), 1e-4f);
            Assert.AreEqual(2.3333333f, (float)Field(lb, "_maxAspect"), 1e-4f);
        }

        //# ---------- T16 ----------

        [Test]
        public void T16_인셋_띠_x_UI_사각형_쌍_검사_전_씬_모든_비율에서_교차_0이다()
        {
            List<string> f = new List<string>();
            foreach (Device d in Devices)
            {
                Frame v = ComputeFrame(d, Kind.Village);
                Frame b = ComputeFrame(d, Kind.Battle);
                Frame l = ComputeFrame(d, Kind.Loading);

                Dictionary<string, Rect> village = Shift(MeasureZones(_villageHud, VillageZoneNames, v.Safe.size), v.Safe.position);
                Dictionary<string, Rect> battle = Shift(MeasureZones(_battleHud, BattleZoneNames, b.Safe.size), b.Safe.position);
                Dictionary<string, Rect> loading = LoadingRects(l.Safe);
                foreach (string n in AllPopups())
                {
                    RectTransform panel = LargestPanel(_popups[n]);
                    foreach (Kind k in KindsOf(n))
                    {
                        if (k == Kind.Village)
                        {
                            village[n + "/" + panel.name] = ResolveInParent(panel, v.Safe);
                        }
                        else
                        {
                            battle[n + "/" + panel.name] = ResolveInParent(panel, b.Safe);
                        }
                    }
                }

                f.AddRange(BandHits(village, v, d.Name + " 마을"));
                f.AddRange(BandHits(battle, b, d.Name + " 배틀"));
                f.AddRange(BandHits(loading, l, d.Name + " 로딩"));
            }
            AssertNone(f);
        }

        [Test]
        public void T16_검사기_회귀_보정_전_앵커_로딩_Panel_전체_스트레치는_인셋_있는_비율에서_실패한다()
        {
            //# 보정 전 상태 = Panel 이 박스 전체(안전영역 무시). M2~M5·조건 사례는 하단 인셋 63 이 하단 요소를 덮어야 한다.
            int[] withInset = { 1, 2, 3, 4, 7 };
            foreach (int i in withInset)
            {
                Frame l = ComputeFrame(Devices[i], Kind.Loading);
                Dictionary<string, Rect> uncorrected = LoadingRects(new Rect(0f, 0f, l.Cw, l.Ch));
                List<string> hits = BandHits(uncorrected, l, Devices[i].Name);
                Assert.Greater(hits.Count, 0, Devices[i].Name + " 보정 전 앵커에서 검사기가 실패해야 함");
                Assert.IsTrue(hits.Exists(h => h.Contains("ProgressBarBg x 하띠")), Devices[i].Name + " ProgressBarBg 하단 띠 침범 미검출");
                Assert.IsTrue(hits.Exists(h => h.Contains("PercentText x 하띠")), Devices[i].Name + " PercentText 하단 띠 침범 미검출");
                Assert.IsFalse(hits.Exists(h => h.Contains("DescText")), Devices[i].Name + " DescText(104)는 인셋 63 위라 침범 없음");
            }
        }

        //# ---------- T17 ----------

        [Test]
        public void T17_로딩_하단_3요소가_C2_보정_후_인셋_위에_있다()
        {
            float[] insetBottoms = { 0f, 63f, 63f, 63f, 63f, 0f, 0f, 63f };
            string[] names = { "ProgressBarBg", "PercentText", "DescText" };
            float[] bases = { 36f, 40f, 104f };
            List<string> f = new List<string>();
            for (int i = 0; i < Devices.Length; i++)
            {
                Frame l = ComputeFrame(Devices[i], Kind.Loading);
                Dictionary<string, Rect> r = LoadingRects(l.Safe);
                float inset = insetBottoms[i];
                for (int k = 0; k < names.Length; k++)
                {
                    float bottom = r[names[k]].yMin;
                    if (Mathf.Abs(bottom - (bases[k] + inset)) > 0.5f)
                    {
                        f.Add(Devices[i].Name + " " + names[k] + " 아래 끝 " + bottom + " != " + (bases[k] + inset));
                    }
                    if (bottom < inset - Eps)
                    {
                        f.Add(Devices[i].Name + " " + names[k] + " 아래 끝 " + bottom + " < 인셋 " + inset);
                    }
                }
            }
            AssertNone(f);
        }

        //# ---------- T18 / T19 ----------

        [Test]
        public void T18_로딩_배선_Panel_SafeAreaFitter_카메라가_LetterboxFramer_캔버스와_같다()
        {
            RectTransform panel = FindRectInScene(_loading, "Panel");
            Assert.IsNotNull(panel, "Panel");
            AssertFitterWired(panel);
            SafeAreaFitter fitter = panel.GetComponent<SafeAreaFitter>();
            LetterboxFramer lb = DotSceneTestUtil.Find<LetterboxFramer>(_loading);
            Assert.IsNotNull(lb, "LetterboxFramer");
            Object fitterCam = (Object)Field(fitter, "_gameCamera");
            Object framerCam = (Object)Field(lb, "_gameCamera");
            Canvas canvas = panel.GetComponentInParent<Canvas>();
            Assert.IsNotNull(canvas, "Loading Canvas");
            Assert.IsTrue(fitterCam == framerCam, "SafeAreaFitter._gameCamera != LetterboxFramer._gameCamera");
            Assert.IsTrue(canvas.worldCamera == (Camera)framerCam, "캔버스 worldCamera != LetterboxFramer._gameCamera");
        }

        [Test]
        public void T19_팝업_루트_배선_마을_배틀_PopupRoot_에_SafeAreaFitter_와_스트레치_앵커가_있다()
        {
            foreach (Scene s in new[] { _village, _battle })
            {
                RectTransform popupRoot = FindRectInScene(s, "PopupRoot");
                Assert.IsNotNull(popupRoot, s.name + " PopupRoot");
                AssertFitterWired(popupRoot);
                Assert.AreEqual(Vector2.zero, popupRoot.anchorMin, s.name + " anchorMin");
                Assert.AreEqual(Vector2.one, popupRoot.anchorMax, s.name + " anchorMax");
                Assert.AreEqual(Vector2.zero, popupRoot.offsetMin, s.name + " offsetMin");
                Assert.AreEqual(Vector2.zero, popupRoot.offsetMax, s.name + " offsetMax");
            }
        }
    }
}
