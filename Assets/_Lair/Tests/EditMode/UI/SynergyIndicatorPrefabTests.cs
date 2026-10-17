using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.UI
{
    //# 시너지 진행도 프리팹·에셋 정합성(기획서 card-synergy-indicator §3.1·§3.3·§3.4·§4.1, 수용 기준 A-5·A-6). 배선 끊김 회귀 가드.
    public class SynergyIndicatorPrefabTests
    {
        private const string PopupPath = "Assets/_Lair/Art/UI/CardSelectionPopup.prefab";
        private const string CellPath = "Assets/_Lair/Art/UI/BuildSynergyCell.prefab";
        private const string ConfigPath = "Assets/_Lair/Art/Synergy/SynergyVisualConfig.asset";

        private static GameObject LoadPrefab(string path)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(prefab, $"프리팹 부재: {path}");
            return prefab;
        }

        private static void AssertRef(SerializedObject so, string field, string owner)
        {
            SerializedProperty p = so.FindProperty(field);
            Assert.IsNotNull(p, $"{owner}.{field} 필드 부재");
            Assert.IsNotNull(p.objectReferenceValue, $"{owner}.{field} 배선 끊김");
        }

        private static void AssertCellWired(BuildSynergyCell cell, string owner)
        {
            SerializedObject so = new SerializedObject(cell);
            AssertRef(so, "_axisIcon", owner);
            AssertRef(so, "_axisStrip", owner);
            AssertRef(so, "_group", owner);
            AssertRef(so, "_text", owner);
            AssertRef(so, "_countText", owner);
            AssertRef(so, "_track", owner);

            SynergyTrack track = (SynergyTrack)so.FindProperty("_track").objectReferenceValue;
            SerializedObject tso = new SerializedObject(track);
            foreach (string arr in new[] { "_frames", "_fills" })
            {
                SerializedProperty ap = tso.FindProperty(arr);
                Assert.IsNotNull(ap, $"{owner}.track.{arr} 부재");
                Assert.AreEqual(7, ap.arraySize, $"{owner}.track.{arr} 칸 수");
                for (int i = 0; i < ap.arraySize; ++i)
                {
                    Assert.IsNotNull(ap.GetArrayElementAtIndex(i).objectReferenceValue, $"{owner}.track.{arr}[{i}] 배선 끊김");
                }
            }
        }

        //# 앵커가 늘어난(stretch) 경우 부모 크기에서 계산한 실제 표시 크기. 루트까지 stretch 면 판정 불가.
        private static Vector2 EffectiveSize(RectTransform rt)
        {
            Vector2 anchorSpan = rt.anchorMax - rt.anchorMin;
            Vector2 size = rt.sizeDelta;
            if (anchorSpan.x == 0f && anchorSpan.y == 0f)
                return size;

            RectTransform parent = rt.parent as RectTransform;
            Assert.IsNotNull(parent, $"{rt.name}: stretch 앵커인데 부모 RectTransform 없음");
            Vector2 parentSize = EffectiveSize(parent);
            return new Vector2(parentSize.x * anchorSpan.x + size.x, parentSize.y * anchorSpan.y + size.y);
        }

        [Test]
        public void BuildSynergyCell_프리팹_리디자인_위젯과_7칸_트랙_배선()
        {
            GameObject prefab = LoadPrefab(CellPath);
            BuildSynergyCell cell = prefab.GetComponent<BuildSynergyCell>();
            Assert.IsNotNull(cell, "루트에 BuildSynergyCell");

            AssertCellWired(cell, "BuildSynergyCell.prefab");
        }

        [Test]
        public void BuildSynergyCell_프리팹_행_크기_165x45()
        {
            GameObject prefab = LoadPrefab(CellPath);
            RectTransform rt = (RectTransform)prefab.transform;

            Vector2 size = EffectiveSize(rt);

            Assert.AreEqual(165f, size.x, 0.01f, "행 폭");
            Assert.AreEqual(45f, size.y, 0.01f, "행 높이");
        }

        [Test]
        public void CardSelectionPopup_프리팹_칩_4개_배선과_서로_다른_인스턴스()
        {
            GameObject prefab = LoadPrefab(PopupPath);
            CardSelectionPopup popup = prefab.GetComponent<CardSelectionPopup>();
            Assert.IsNotNull(popup, "루트에 CardSelectionPopup");

            SerializedProperty chips = new SerializedObject(popup).FindProperty("_synergyChips");
            Assert.IsNotNull(chips, "_synergyChips 부재");
            Assert.AreEqual(4, chips.arraySize, "칩 4개");

            System.Collections.Generic.HashSet<Object> seen = new System.Collections.Generic.HashSet<Object>();
            for (int i = 0; i < 4; ++i)
            {
                Object chip = chips.GetArrayElementAtIndex(i).objectReferenceValue;
                Assert.IsNotNull(chip, $"칩{i} 배선 끊김");
                Assert.IsTrue(seen.Add(chip), $"칩{i} 가 다른 칩과 같은 인스턴스");
                AssertCellWired((BuildSynergyCell)chip, $"CardSelectionPopup.칩{i}");
            }
        }

        [Test]
        public void CardSelectionPopup_프리팹_칩_크기_165x45_가로_중앙_배치_겹침_없음()
        {
            GameObject prefab = LoadPrefab(PopupPath);
            CardSelectionPopup popup = prefab.GetComponent<CardSelectionPopup>();
            SerializedProperty chips = new SerializedObject(popup).FindProperty("_synergyChips");

            float[] xs = new float[4];
            for (int i = 0; i < 4; ++i)
            {
                RectTransform rt = (RectTransform)((BuildSynergyCell)chips.GetArrayElementAtIndex(i).objectReferenceValue).transform;
                Vector2 size = EffectiveSize(rt);
                Assert.AreEqual(165f, size.x, 0.01f, $"칩{i} 폭");
                Assert.AreEqual(45f, size.y, 0.01f, $"칩{i} 높이");
                xs[i] = rt.anchoredPosition.x;
            }
            System.Array.Sort(xs);
            for (int i = 1; i < 4; ++i)
            {
                Assert.GreaterOrEqual(xs[i] - xs[i - 1], 165f, $"칩 {i - 1}-{i} 가로 겹침");
            }
        }

        [Test]
        public void CardSelectionPopup_프리팹_3슬롯_축_아이콘_슬롯_배선()
        {
            GameObject prefab = LoadPrefab(PopupPath);
            CardView[] views = prefab.GetComponentsInChildren<CardView>(true);
            Assert.AreEqual(3, views.Length, "CardView 슬롯 3개");

            for (int i = 0; i < views.Length; ++i)
            {
                SerializedObject so = new SerializedObject(views[i]);
                AssertRef(so, "_axisIconRoot", $"CardView{i}");
                AssertRef(so, "_axisIconBorder", $"CardView{i}");
                AssertRef(so, "_axisIconImage", $"CardView{i}");

                GameObject root = (GameObject)so.FindProperty("_axisIconRoot").objectReferenceValue;
                Image border = (Image)so.FindProperty("_axisIconBorder").objectReferenceValue;
                Image image = (Image)so.FindProperty("_axisIconImage").objectReferenceValue;
                Assert.IsTrue(image.transform.IsChildOf(root.transform), $"CardView{i} 아이콘 이미지는 루트 하위");
                Assert.IsTrue(border.transform.IsChildOf(root.transform), $"CardView{i} 테두리는 루트 하위");
                Assert.IsTrue(root.transform.IsChildOf(views[i].transform), $"CardView{i} 루트는 카드 하위");
            }
        }

        [Test]
        public void CardSelectionPopup_프리팹_카드_아이콘_표시크기는_PNG_해상도의_정수배()
        {
            SynergyVisualConfig config = AssetDatabase.LoadAssetAtPath<SynergyVisualConfig>(ConfigPath);
            Assert.IsNotNull(config, $"SO 부재: {ConfigPath}");
            GameObject prefab = LoadPrefab(PopupPath);
            CardView[] views = prefab.GetComponentsInChildren<CardView>(true);

            for (int i = 0; i < views.Length; ++i)
            {
                Image image = (Image)new SerializedObject(views[i]).FindProperty("_axisIconImage").objectReferenceValue;
                Vector2 size = EffectiveSize(image.rectTransform);
                foreach (EBuildAxis axis in BuildSynergyPanel.AllAxes)
                {
                    Sprite sprite = config.GetIcon(axis);
                    Assert.IsNotNull(sprite, axis.ToString());
                    float w = sprite.rect.width;
                    float h = sprite.rect.height;
                    Assert.Greater(size.x, 0f, $"CardView{i} 아이콘 폭");
                    Assert.AreEqual(0f, size.x % w, 0.001f, $"CardView{i} {axis} 폭 {size.x} 는 PNG 폭 {w} 의 정수배여야 함");
                    Assert.AreEqual(0f, size.y % h, 0.001f, $"CardView{i} {axis} 높이 {size.y} 는 PNG 높이 {h} 의 정수배여야 함");
                    Assert.AreEqual(size.x / w, size.y / h, 0.001f, $"CardView{i} {axis} 가로세로 배율 동일(왜곡 금지)");
                }
            }
        }

        [Test]
        public void SynergyVisualConfig_에셋_4축_전부_Sprite_할당_중복_없음()
        {
            SynergyVisualConfig config = AssetDatabase.LoadAssetAtPath<SynergyVisualConfig>(ConfigPath);
            Assert.IsNotNull(config, $"SO 부재: {ConfigPath}");
            Assert.AreEqual(4, config.Entries.Length, "항목은 축마다 정확히 1개");

            System.Collections.Generic.HashSet<EBuildAxis> axes = new System.Collections.Generic.HashSet<EBuildAxis>();
            System.Collections.Generic.HashSet<Sprite> sprites = new System.Collections.Generic.HashSet<Sprite>();
            foreach (SynergyVisualConfig.Entry entry in config.Entries)
            {
                Assert.IsNotNull(entry, "null 항목");
                Assert.IsTrue(axes.Add(entry.Axis), $"축 중복: {entry.Axis}");
                Assert.IsNotNull(entry.Icon, $"{entry.Axis} Sprite 누락");
                Assert.IsTrue(sprites.Add(entry.Icon), $"{entry.Axis} Sprite 가 다른 축과 동일");
            }
            foreach (EBuildAxis axis in BuildSynergyPanel.AllAxes)
            {
                Assert.IsNotNull(config.GetIcon(axis), $"{axis} 아이콘 누락");
            }
        }

        [Test]
        public void SynergyVisualConfig_에셋_아이콘은_12x12_도안_SynergyIcons_폴더()
        {
            SynergyVisualConfig config = AssetDatabase.LoadAssetAtPath<SynergyVisualConfig>(ConfigPath);
            Assert.IsNotNull(config);

            foreach (SynergyVisualConfig.Entry entry in config.Entries)
            {
                string path = AssetDatabase.GetAssetPath(entry.Icon);
                StringAssert.Contains("/Art/Sprites/SynergyIcons/", path, $"{entry.Axis} 아이콘 경로");
                Assert.AreEqual(12f, entry.Icon.rect.width, 0.001f, $"{entry.Axis} 도안 폭");
                Assert.AreEqual(12f, entry.Icon.rect.height, 0.001f, $"{entry.Axis} 도안 높이");
                Assert.AreEqual(FilterMode.Point, entry.Icon.texture.filterMode, $"{entry.Axis} Point 필터");
            }
        }

        [Test]
        public void EData_SynergyVisualConfig_값명이_에셋_파일명과_일치()
        {
            Assert.AreEqual("SynergyVisualConfig", EData.SynergyVisualConfig.ToString());
            Assert.AreEqual("SynergyVisualConfig", System.IO.Path.GetFileNameWithoutExtension(ConfigPath));
        }
    }
}
