using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# BattleHud.prefab 위젯 배치 회귀 — 기획서 scene-2d-conversion §4.2 (1280×720 캔버스).
    public class BattleHudPrefabLayoutTests
    {
        private const string Path = "Assets/_Lair/Art/UI/BattleHud.prefab";
        private GameObject _root;

        [OneTimeSetUp]
        public void Load()
        {
            _root = PrefabUtility.LoadPrefabContents(Path);
        }

        [OneTimeTearDown]
        public void Unload()
        {
            PrefabUtility.UnloadPrefabContents(_root);
        }

        private RectTransform Widget(string name)
        {
            foreach (RectTransform t in _root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name == name)
                    return t;
            }
            return null;
        }

        private void AssertRect(string name, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            RectTransform rt = Widget(name);
            Assert.IsNotNull(rt, name);
            Assert.AreEqual(anchor.x, rt.anchorMin.x, 1e-3f, name + " anchorMin.x");
            Assert.AreEqual(anchor.y, rt.anchorMin.y, 1e-3f, name + " anchorMin.y");
            Assert.AreEqual(anchor.x, rt.anchorMax.x, 1e-3f, name + " anchorMax.x");
            Assert.AreEqual(anchor.y, rt.anchorMax.y, 1e-3f, name + " anchorMax.y");
            Assert.AreEqual(pivot.x, rt.pivot.x, 1e-3f, name + " pivot.x");
            Assert.AreEqual(pivot.y, rt.pivot.y, 1e-3f, name + " pivot.y");
            Assert.AreEqual(pos.x, rt.anchoredPosition.x, 0.5f, name + " pos.x");
            Assert.AreEqual(pos.y, rt.anchoredPosition.y, 0.5f, name + " pos.y");
            Assert.AreEqual(size.x, rt.sizeDelta.x, 0.5f, name + " size.x");
            Assert.AreEqual(size.y, rt.sizeDelta.y, 0.5f, name + " size.y");
        }

        [Test]
        public void 타이머_판은_상단중앙_기준_왼쪽에_있다()
        {
            AssertRect("TimerPlate", new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-403f, -16f), new Vector2(104f, 69f));
        }

        [Test]
        public void 보스_바는_상단중앙에_557x89다()
        {
            AssertRect("BossHpBar", new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(-277f, -13f), new Vector2(557f, 89f));
        }

        [Test]
        public void 액티브_카운트다운은_보스_바_오른쪽에_있다()
        {
            AssertRect("ActiveCountdown", new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(301f, -16f), new Vector2(115f, 69f));
        }

        [Test]
        public void 스포너_행은_하단중앙_1006x64다()
        {
            AssertRect("SpawnerStatusPanel", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(1006f, 64f));
        }

        [Test]
        public void 시너지_패널은_우상단_펼침_165x247이다()
        {
            AssertRect("BuildSynergyPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-13f, -112f), new Vector2(165f, 247f));
        }

        [Test]
        public void 빌드_패널은_우상단_펼침_165x239다()
        {
            AssertRect("BuildPanel", new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-13f, -387f), new Vector2(165f, 239f));
        }

        [Test]
        public void 오른쪽_열_세로_배치가_서로_겹치지_않고_스포너_행과도_분리된다()
        {
            //# 시너지 끝 112+247 = 359 < 빌드 시작 387, 빌드 끝 387+239 = 626 < 스포너 행 시작 720−16−64 = 640
            Assert.Less(112f + 247f, 387f);
            Assert.Less(387f + 239f, 720f - 16f - 64f);
        }

        [Test]
        public void 보스_바_눈금_9개가_정적으로_있다()
        {
            int ticks = 0;
            foreach (RectTransform t in _root.GetComponentsInChildren<RectTransform>(true))
            {
                if (t.name.StartsWith("Tick_"))
                {
                    ticks++;
                }
            }
            Assert.AreEqual(9, ticks);
        }

        [Test]
        public void 눈금_이름은_Tick_0부터_Tick_8이다()
        {
            HashSet<string> names = new HashSet<string>();
            foreach (RectTransform t in _root.GetComponentsInChildren<RectTransform>(true))
            {
                names.Add(t.name);
            }
            for (int i = 0; i < 9; i++)
            {
                Assert.IsTrue(names.Contains("Tick_" + i), "Tick_" + i);
            }
        }

        [Test]
        public void 접기_탭이_시너지와_빌드_패널에_각각_정적으로_있다()
        {
            foreach (string panel in new[] { "BuildSynergyPanel", "BuildPanel" })
            {
                RectTransform p = Widget(panel);
                Assert.IsNotNull(p, panel);
                bool folded = false;
                foreach (RectTransform t in p.GetComponentsInChildren<RectTransform>(true))
                {
                    if (t.name == "FoldedTab")
                    {
                        folded = true;
                    }
                }
                Assert.IsTrue(folded, panel + " FoldedTab");
            }
        }

        [Test]
        public void 이전_배치_요소_HeroHpBar_가_남아있지_않다()
        {
            Assert.IsNull(Widget("HeroHpBar"));
        }

        [Test]
        public void 프리팹에_깨진_스크립트_참조가_없다()
        {
            foreach (Transform t in _root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject), t.name);
            }
        }
    }
}
