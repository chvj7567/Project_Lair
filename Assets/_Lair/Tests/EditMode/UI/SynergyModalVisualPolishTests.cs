using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Tests.EditMode
{
    //# 시너지 모달 비주얼 폴리시 스위트(synergy-modal-visual-polish §8.1) — 프리팹 연결·셀 Bind 틴트·스프라이트 임포트·장식 레이캐스트.
    //# ActiveTier 대입(BuildRows)과 빈 셀 가드 기본형은 SynergyModalCellActiveTierTests 가 이미 검증하므로 중복하지 않는다.
    public class SynergyModalVisualPolishTests
    {
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/";
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string CellPath = UiDir + "SynergyModalCell.prefab";
        private const string PopupPath = UiDir + "SynergyModalPopup.prefab";
        private const string GeneratorPath = "Assets/_Lair/Editor/SynergyModalPolishSpriteGenerator.cs";

        private static readonly string[] NewSprites = { "SynergyModalCellBg", "SynergyModalHalo", "SynergyModalBadge" };

        private static readonly Color AxisBlue = new Color(0.2f, 0.6f, 0.9f, 1f);
        private static readonly Color AxisRed = new Color(0.9f, 0.3f, 0.25f, 1f);
        private static readonly Color GemOff = new Color32(0x3A, 0x47, 0x64, 0xFF);
        private static readonly Color NextNormal = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color HandleGold = new Color32(0xE0, 0xA2, 0x1C, 0xFF);

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
            FieldInfo field = target.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);
            Assert.IsNotNull(field, $"{target.GetType().Name}.{name} 필드 존재 — 시그니처 변경 감지");
            return field;
        }

        private static object Field(object target, string name)
        {
            return FieldOf(target, name).GetValue(target);
        }

        private static void SetField(object target, string name, object value)
        {
            FieldOf(target, name).SetValue(target, value);
        }

        private static GameObject LoadPrefabAsset(string path)
        {
            GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Assert.IsNotNull(root, path);
            return root;
        }

        //# 에셋 자체를 건드리지 않도록 Bind 검증용 셀은 씬 인스턴스로 만든다.
        private SynergyModalCell NewCellInstance()
        {
            GameObject instance = PrefabUtility.InstantiatePrefab(LoadPrefabAsset(CellPath)) as GameObject;
            Assert.IsNotNull(instance, "셀 프리팹 인스턴스");
            _spawned.Add(instance);
            SynergyModalCell cell = instance.GetComponent<SynergyModalCell>();
            Assert.IsNotNull(cell, "SynergyModalCell 컴포넌트");
            return cell;
        }

        private static Transform FindChild(GameObject root, string name)
        {
            foreach (Transform t in root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == name)
                    return t;
            }
            Assert.Fail($"{root.name} 하위에 {name} 없음");
            return null;
        }

        private static RectTransform RectOf(GameObject root, string name)
        {
            RectTransform rect = FindChild(root, name) as RectTransform;
            Assert.IsNotNull(rect, name + " RectTransform");
            return rect;
        }

        private static TMP_Text TmpOf(CHText text)
        {
            return text.GetComponent<TMP_Text>();
        }

        private static void AssertRgb(Color expected, Color actual, string message)
        {
            Assert.AreEqual(expected.r, actual.r, 0.005f, message + " r");
            Assert.AreEqual(expected.g, actual.g, 0.005f, message + " g");
            Assert.AreEqual(expected.b, actual.b, 0.005f, message + " b");
        }

        private static void AssertColor(Color expected, Color actual, string message)
        {
            AssertRgb(expected, actual, message);
            Assert.AreEqual(expected.a, actual.a, 0.005f, message + " a");
        }

        private static SynergyModalCellData Header(Color axis, int activeTier, bool isMax)
        {
            return new SynergyModalCellData
            {
                RowKind = SynergyModalCellData.Kind.Header,
                AxisColor = axis,
                Label = "TANK (5장)",
                TierBadgeText = activeTier + "/3",
                IsMaxTier = isMax,
                DescText = "현재 효과",
                NextText = "다음: 효과",
                ActiveTier = activeTier,
            };
        }

        private static SynergyModalCellData Effect(Color axis)
        {
            return new SynergyModalCellData
            {
                RowKind = SynergyModalCellData.Kind.Effect,
                AxisColor = axis,
                Label = "Tier1  효과",
            };
        }

        private static Image[] Images(SynergyModalCell cell, string field)
        {
            Image[] images = (Image[])Field(cell, field);
            Assert.IsNotNull(images, field);
            return images;
        }

        private static TextureImporter Importer(string name)
        {
            return AssetImporter.GetAtPath(SpriteDir + name + ".png") as TextureImporter;
        }

        private static SpriteMeshType ReadMeshType(TextureImporter importer)
        {
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            return settings.spriteMeshType;
        }

        //# --- ① 셀 프리팹 신규 SerializeField 연결 ---

        [Test]
        public void 셀_프리팹_배지_면_필드가_연결되고_스프라이트가_SynergyModalBadge다()
        {
            SynergyModalCell cell = LoadPrefabAsset(CellPath).GetComponent<SynergyModalCell>();
            Image badgeBg = (Image)Field(cell, "_tierBadgeBg");

            Assert.IsTrue(badgeBg != null, "_tierBadgeBg 비어 있음");
            Assert.AreEqual("TierBadgeBg", badgeBg.name);
            Assert.IsTrue(badgeBg.sprite != null, "TierBadgeBg 스프라이트");
            Assert.AreEqual("SynergyModalBadge", badgeBg.sprite.name);
            Assert.AreEqual(Image.Type.Sliced, badgeBg.type);
        }

        [Test]
        public void 셀_프리팹_tierGems는_길이3이고_순서대로_VillageHudGem_스프라이트다()
        {
            SynergyModalCell cell = LoadPrefabAsset(CellPath).GetComponent<SynergyModalCell>();
            Image[] gems = Images(cell, "_tierGems");

            Assert.AreEqual(3, gems.Length);
            for (int i = 0; i < gems.Length; ++i)
            {
                Assert.IsTrue(gems[i] != null, $"_tierGems[{i}] 비어 있음");
                Assert.AreEqual("TierGem" + (i + 1), gems[i].name, $"_tierGems[{i}] 이름");
                Assert.IsTrue(gems[i].sprite != null, $"_tierGems[{i}] 스프라이트");
                Assert.AreEqual("VillageHudGem", gems[i].sprite.name, $"_tierGems[{i}] 스프라이트 이름");
            }
        }

        [Test]
        public void 셀_프리팹_axisTintImages는_길이3이고_StripGlow_IconGlow_IconSlot_순서다()
        {
            SynergyModalCell cell = LoadPrefabAsset(CellPath).GetComponent<SynergyModalCell>();
            Image[] tints = Images(cell, "_axisTintImages");

            Assert.AreEqual(3, tints.Length);
            string[] expected = { "StripGlow", "IconGlow", "IconSlot" };
            for (int i = 0; i < tints.Length; ++i)
            {
                Assert.IsTrue(tints[i] != null, $"_axisTintImages[{i}] 비어 있음");
                Assert.AreEqual(expected[i], tints[i].name, $"_axisTintImages[{i}] 이름");
            }
            Assert.AreEqual("SynergyModalHalo", tints[2].sprite.name, "IconSlot 스프라이트");
        }

        [Test]
        public void 셀_프리팹_루트_스프라이트는_SynergyModalCellBg고_Sliced다()
        {
            GameObject root = LoadPrefabAsset(CellPath);
            Image image = root.GetComponent<Image>();

            Assert.IsTrue(image != null, "셀 루트 Image");
            Assert.IsTrue(image.sprite != null, "셀 루트 스프라이트");
            Assert.AreEqual("SynergyModalCellBg", image.sprite.name);
            Assert.AreEqual(Image.Type.Sliced, image.type);
        }

        [Test]
        public void 셀_프리팹_기존_필드가_모두_연결돼_있다()
        {
            SynergyModalCell cell = LoadPrefabAsset(CellPath).GetComponent<SynergyModalCell>();
            string[] names = { "_axisStrip", "_icon", "_label", "_tierBadge", "_descText", "_nextText" };
            foreach (string name in names)
            {
                UnityEngine.Object widget = Field(cell, name) as UnityEngine.Object;
                Assert.IsTrue(widget != null, $"SynergyModalCell.{name} 비어 있음");
            }
        }

        [Test]
        public void 셀_프리팹_형제_순서가_기획서_2_3과_같다()
        {
            GameObject root = LoadPrefabAsset(CellPath);
            string[] expected =
            {
                "CellGradient", "CellGloss", "StripGlow", "AxisStrip", "IconGlow", "IconSlot", "Icon",
                "TierGem1", "TierGem2", "TierGem3", "InsetFill", "InsetTop", "InsetFrame", "InsetDivider",
                "Label", "TierBadgeBg", "TierBadge", "DescText", "NextText",
            };
            List<string> actual = new List<string>();
            for (int i = 0; i < root.transform.childCount; ++i)
            {
                actual.Add(root.transform.GetChild(i).name);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        //# --- ① 기존 위젯 좌표 회귀 (§1.1) ---

        //# 이름, anchorMin, anchorMax, anchoredPosition, sizeDelta
        [TestCase("SynergyModalCell", 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 392f, 136f)]
        [TestCase("AxisStrip", 0f, 0f, 0f, 1f, 4f, 0f, 8f, -8f)]
        [TestCase("IconSlot", 0f, 0.5f, 0f, 0.5f, 20f, 0f, 52f, 52f)]
        [TestCase("Icon", 0f, 0.5f, 0f, 0.5f, 46f, 0f, 36f, 36f)]
        [TestCase("Label", 0f, 1f, 0f, 1f, 84f, -8f, 200f, 30f)]
        [TestCase("TierBadgeBg", 1f, 1f, 1f, 1f, -14f, -8f, 64f, 28f)]
        [TestCase("TierBadge", 1f, 1f, 1f, 1f, -14f, -8f, 64f, 28f)]
        [TestCase("DescText", 0f, 1f, 1f, 1f, 84f, -42f, -98f, 44f)]
        [TestCase("NextText", 0f, 1f, 1f, 1f, 84f, -92f, -98f, 36f)]
        public void 셀_기존_위젯의_앵커_위치_크기가_변하지_않는다(string name, float minX, float minY, float maxX, float maxY, float posX, float posY, float sizeX, float sizeY)
        {
            RectTransform rect = RectOf(LoadPrefabAsset(CellPath), name);

            Assert.AreEqual(minX, rect.anchorMin.x, 0.0001f, name + " anchorMin.x");
            Assert.AreEqual(minY, rect.anchorMin.y, 0.0001f, name + " anchorMin.y");
            Assert.AreEqual(maxX, rect.anchorMax.x, 0.0001f, name + " anchorMax.x");
            Assert.AreEqual(maxY, rect.anchorMax.y, 0.0001f, name + " anchorMax.y");
            Assert.AreEqual(posX, rect.anchoredPosition.x, 0.0001f, name + " pos.x");
            Assert.AreEqual(posY, rect.anchoredPosition.y, 0.0001f, name + " pos.y");
            Assert.AreEqual(sizeX, rect.sizeDelta.x, 0.0001f, name + " size.x");
            Assert.AreEqual(sizeY, rect.sizeDelta.y, 0.0001f, name + " size.y");
        }

        //# --- ② 셀 Bind — Tier 보석 ---

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        public void Bind_헤더는_ActiveTier_개수만큼_보석을_축_색으로_켜고_나머지는_꺼짐색이다(int activeTier)
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, activeTier, false));

            Image[] gems = Images(cell, "_tierGems");
            int litCount = 0;
            for (int i = 0; i < gems.Length; ++i)
            {
                Assert.IsTrue(gems[i].gameObject.activeSelf, $"보석{i} 헤더에서는 항상 활성(켜짐/꺼짐은 색으로 구분)");
                if (i < activeTier)
                {
                    ++litCount;
                    AssertColor(AxisBlue, gems[i].color, $"보석{i} 켜짐");
                }
                else
                {
                    AssertColor(GemOff, gems[i].color, $"보석{i} 꺼짐");
                }
            }
            Assert.AreEqual(activeTier, litCount);
        }

        [Test]
        public void Bind_보석은_축이_바뀌면_새_축_색으로_다시_칠해진다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 3, false));
            cell.Bind(Header(AxisRed, 1, false));

            Image[] gems = Images(cell, "_tierGems");
            AssertColor(AxisRed, gems[0].color, "보석0 새 축 색");
            AssertColor(GemOff, gems[1].color, "보석1 이전 3단계의 켜짐 잔류 없음");
            AssertColor(GemOff, gems[2].color, "보석2 이전 3단계의 켜짐 잔류 없음");
        }

        [Test]
        public void Bind_효과_행은_보석_3개와_배지_면을_비활성화한다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 3, true));
            cell.Bind(Effect(AxisBlue));

            foreach (Image gem in Images(cell, "_tierGems"))
            {
                Assert.IsFalse(gem.gameObject.activeSelf, gem.name);
            }
            Image badgeBg = (Image)Field(cell, "_tierBadgeBg");
            CHText badge = (CHText)Field(cell, "_tierBadge");
            Assert.IsFalse(badgeBg.gameObject.activeSelf, "배지 면");
            Assert.IsFalse(badge.gameObject.activeSelf, "배지 글자");
        }

        [Test]
        public void Bind_효과_행에서_헤더로_재_Bind하면_보석과_배지가_다시_켜진다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Effect(AxisBlue));
            cell.Bind(Header(AxisBlue, 2, false));

            foreach (Image gem in Images(cell, "_tierGems"))
            {
                Assert.IsTrue(gem.gameObject.activeSelf, gem.name);
            }
            Assert.IsTrue(((Image)Field(cell, "_tierBadgeBg")).gameObject.activeSelf, "배지 면");
        }

        //# --- ② 셀 Bind — 배지·다음 문구 (최대 단계) ---

        [Test]
        public void Bind_일반_단계_배지_면과_글자는_축_색이고_다음_문구는_회청색이다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 2, false));

            Image badgeBg = (Image)Field(cell, "_tierBadgeBg");
            CHText badge = (CHText)Field(cell, "_tierBadge");
            CHText next = (CHText)Field(cell, "_nextText");
            AssertColor(AxisBlue, badgeBg.color, "배지 면");
            AssertColor(AxisBlue, TmpOf(badge).color, "배지 글자");
            AssertColor(NextNormal, TmpOf(next).color, "다음 문구");
            Assert.AreEqual("다음: 효과", next.GetString());
            Assert.AreEqual("2/3", badge.GetString());
        }

        [Test]
        public void Bind_최대_단계는_배지_면_배지_글자_다음_문구가_금색이다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 3, true));

            Image badgeBg = (Image)Field(cell, "_tierBadgeBg");
            CHText badge = (CHText)Field(cell, "_tierBadge");
            CHText next = (CHText)Field(cell, "_nextText");
            AssertColor(UiDotPalette.Gold, badgeBg.color, "배지 면");
            AssertColor(UiDotPalette.Gold, TmpOf(badge).color, "배지 글자");
            AssertColor(UiDotPalette.Gold, TmpOf(next).color, "다음 문구");
            Assert.AreEqual("· 최대 단계", next.GetString());
            foreach (Image gem in Images(cell, "_tierGems"))
            {
                AssertColor(AxisBlue, gem.color, gem.name + " 최대 단계에서도 보석은 축 색");
            }
        }

        [Test]
        public void Bind_최대_단계_후_일반_단계로_재_Bind하면_금색이_잔류하지_않는다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 3, true));
            cell.Bind(Header(AxisRed, 1, false));

            Image badgeBg = (Image)Field(cell, "_tierBadgeBg");
            CHText badge = (CHText)Field(cell, "_tierBadge");
            CHText next = (CHText)Field(cell, "_nextText");
            AssertColor(AxisRed, badgeBg.color, "배지 면 복귀");
            AssertColor(AxisRed, TmpOf(badge).color, "배지 글자 복귀");
            AssertColor(NextNormal, TmpOf(next).color, "다음 문구 복귀");
            Assert.AreEqual("다음: 효과", next.GetString(), "최대 단계 문구 잔류 없음");
        }

        [Test]
        public void Bind_일반_단계_후_최대_단계로_재_Bind하면_금색으로_전환된다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisRed, 1, false));
            cell.Bind(Header(AxisRed, 3, true));

            AssertColor(UiDotPalette.Gold, ((Image)Field(cell, "_tierBadgeBg")).color, "배지 면");
            AssertColor(UiDotPalette.Gold, TmpOf((CHText)Field(cell, "_nextText")).color, "다음 문구");
        }

        //# --- ② 셀 Bind — 축 색 틴트 ---

        [Test]
        public void Bind_틴트_대상은_rgb만_축_색으로_바뀌고_프리팹_알파를_유지한다()
        {
            SynergyModalCell cell = NewCellInstance();
            Image[] tints = Images(cell, "_axisTintImages");
            float[] alphas = new float[tints.Length];
            for (int i = 0; i < tints.Length; ++i)
            {
                alphas[i] = tints[i].color.a;
            }

            cell.Bind(Header(AxisBlue, 2, false));
            for (int i = 0; i < tints.Length; ++i)
            {
                AssertRgb(AxisBlue, tints[i].color, tints[i].name);
                Assert.AreEqual(alphas[i], tints[i].color.a, 0.0001f, tints[i].name + " 알파 유지");
            }

            cell.Bind(Header(AxisRed, 2, false));
            for (int i = 0; i < tints.Length; ++i)
            {
                AssertRgb(AxisRed, tints[i].color, tints[i].name + " 재 Bind");
                Assert.AreEqual(alphas[i], tints[i].color.a, 0.0001f, tints[i].name + " 재 Bind 알파 유지");
            }
        }

        [Test]
        public void Bind_틴트_알파는_AxisColor_알파와_무관하게_프리팹_알파를_따른다()
        {
            SynergyModalCell cell = NewCellInstance();
            Image[] tints = Images(cell, "_axisTintImages");
            float[] alphas = new float[tints.Length];
            for (int i = 0; i < tints.Length; ++i)
            {
                alphas[i] = tints[i].color.a;
            }

            Color translucent = new Color(AxisBlue.r, AxisBlue.g, AxisBlue.b, 0.3f);
            cell.Bind(Header(translucent, 1, false));

            for (int i = 0; i < tints.Length; ++i)
            {
                Assert.AreEqual(alphas[i], tints[i].color.a, 0.0001f, tints[i].name);
            }
        }

        [Test]
        public void Bind_효과_행도_틴트와_축_색_띠가_갱신된다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Effect(AxisRed));

            Image strip = (Image)Field(cell, "_axisStrip");
            AssertColor(AxisRed, strip.color, "축 색 띠");
            foreach (Image tint in Images(cell, "_axisTintImages"))
            {
                AssertRgb(AxisRed, tint.color, tint.name);
            }
        }

        //# 리뷰 권고 — 배지·보석 색의 알파는 AxisColor 알파를 그대로 쓰므로 실제 4축 색이 알파 1 이어야 한다.
        [Test]
        public void 실제_4축_AxisColor는_알파1이고_배지_면과_켜진_보석도_알파1이다()
        {
            foreach (EBuildAxis axis in Enum.GetValues(typeof(EBuildAxis)))
            {
                Color axisColor = BuildSynergyPanel.AxisColor[axis];
                Assert.AreEqual(1f, axisColor.a, 0.0001f, axis + " AxisColor 알파");

                SynergyModalCell cell = NewCellInstance();
                cell.Bind(Header(axisColor, 3, false));
                Assert.AreEqual(1f, ((Image)Field(cell, "_tierBadgeBg")).color.a, 0.0001f, axis + " 배지 면 알파");
                foreach (Image gem in Images(cell, "_tierGems"))
                {
                    Assert.AreEqual(1f, gem.color.a, 0.0001f, axis + " " + gem.name + " 알파");
                }
            }
        }

        //# --- ② 셀 Bind — 가드 ---

        [Test]
        public void Bind_배열_원소가_null이거나_길이가_3이_아니어도_예외가_없다()
        {
            GameObject go = new GameObject("PartialCell", typeof(RectTransform));
            _spawned.Add(go);
            SynergyModalCell cell = go.AddComponent<SynergyModalCell>();
            Image gem = new GameObject("Gem", typeof(RectTransform)).AddComponent<Image>();
            gem.transform.SetParent(go.transform, false);
            SetField(cell, "_tierGems", new Image[] { gem, null });
            SetField(cell, "_axisTintImages", new Image[] { null });

            Assert.DoesNotThrow(() => cell.Bind(Header(AxisBlue, 3, true)));
            AssertColor(AxisBlue, gem.color, "범위 안 보석은 처리");

            SetField(cell, "_tierGems", new Image[0]);
            SetField(cell, "_axisTintImages", new Image[0]);
            Assert.DoesNotThrow(() => cell.Bind(Header(AxisBlue, 3, true)));
        }

        [Test]
        public void Bind_null_데이터는_상태를_바꾸지_않고_예외가_없다()
        {
            SynergyModalCell cell = NewCellInstance();
            cell.Bind(Header(AxisBlue, 2, false));

            Assert.DoesNotThrow(() => cell.Bind(null));
            AssertColor(AxisBlue, Images(cell, "_tierGems")[0].color, "직전 상태 유지");
        }

        //# --- ③ 신규 스프라이트 임포트 (§3) ---

        [Test]
        public void 신규_스프라이트_3종이_존재하고_공통_임포트_설정을_따른다()
        {
            Assert.AreEqual(3, NewSprites.Length);
            foreach (string name in NewSprites)
            {
                Assert.IsTrue(File.Exists(SpriteDir + name + ".png"), $"{name}.png 없음");
                TextureImporter importer = Importer(name);
                Assert.IsNotNull(importer, $"{name} 임포터");
                Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, $"{name} textureType");
                Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, $"{name} spriteMode");
                Assert.AreEqual(100f, importer.spritePixelsPerUnit, $"{name} PPU");
                Assert.AreEqual(SpriteMeshType.FullRect, ReadMeshType(importer), $"{name} meshType");
                Assert.IsFalse(importer.mipmapEnabled, $"{name} mipmap");
                Assert.AreEqual(TextureImporterCompression.Uncompressed, importer.textureCompression, $"{name} compression");
                Assert.IsTrue(importer.sRGBTexture, $"{name} sRGB");
                Assert.IsTrue(importer.alphaIsTransparency, $"{name} alphaIsTransparency");
                Assert.AreEqual(TextureWrapMode.Clamp, importer.wrapMode, $"{name} wrap");
                Assert.IsNotNull(AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png"), $"{name} Sprite 로드");
            }
        }

        [TestCase("SynergyModalCellBg", 16, 16)]
        [TestCase("SynergyModalHalo", 64, 64)]
        [TestCase("SynergyModalBadge", 16, 16)]
        public void 신규_스프라이트_픽셀_크기가_기획서_표와_같다(string name, int width, int height)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");

            Assert.IsNotNull(sprite, name);
            Assert.AreEqual(width, (int)sprite.rect.width, $"{name} 폭");
            Assert.AreEqual(height, (int)sprite.rect.height, $"{name} 높이");
        }

        //# border 는 Unity 순서(좌,하,우,상).
        [TestCase("SynergyModalCellBg", 3, 6, 3, 5)]
        [TestCase("SynergyModalBadge", 4, 4, 4, 4)]
        [TestCase("SynergyModalHalo", 0, 0, 0, 0)]
        public void 신규_스프라이트_border가_기획서_표와_같다(string name, int left, int bottom, int right, int top)
        {
            Assert.AreEqual(new Vector4(left, bottom, right, top), Importer(name).spriteBorder, name);
        }

        [TestCase("SynergyModalCellBg", FilterMode.Point)]
        [TestCase("SynergyModalBadge", FilterMode.Point)]
        [TestCase("SynergyModalHalo", FilterMode.Bilinear)]
        public void 신규_스프라이트_필터_모드가_기획서_표와_같다(string name, FilterMode expected)
        {
            Assert.AreEqual(expected, Importer(name).filterMode, name);
        }

        //# --- ④ 장식 레이캐스트 ---

        [Test]
        public void 셀_프리팹의_모든_Graphic은_raycastTarget이_꺼져_있다()
        {
            int count = 0;
            foreach (Graphic graphic in LoadPrefabAsset(CellPath).GetComponentsInChildren<Graphic>(true))
            {
                ++count;
                Assert.IsFalse(graphic.raycastTarget, $"SynergyModalCell/{graphic.name} raycastTarget");
            }
            Assert.Greater(count, 14, "셀 Graphic 수(신규 장식 14장 이상 포함)");
        }

        [Test]
        public void 셀_프리팹에_신규_장식_14종이_모두_존재한다()
        {
            GameObject root = LoadPrefabAsset(CellPath);
            string[] names =
            {
                "CellGradient", "CellGloss", "StripGlow", "StripShine", "StripShade", "IconGlow",
                "TierGem1", "TierGem2", "TierGem3", "InsetFill", "InsetTop", "InsetFrame", "InsetDivider",
            };
            foreach (string name in names)
            {
                Transform t = FindChild(root, name);
                Assert.IsTrue(t.GetComponent<Image>() != null, name + " Image");
            }
        }

        //# 클릭 대상은 Dim · ModalBody · CloseButton · Viewport · VerticalScrollbar · Handle 뿐(§1.1).
        private static readonly HashSet<string> PopupRaycastTargets = new HashSet<string>
        {
            "Dim", "ModalBody", "CloseButton", "Viewport", "VerticalScrollbar", "Handle",
        };

        [Test]
        public void 팝업_프리팹의_장식_Image는_raycastTarget이_꺼지고_클릭_대상_6종만_켜져_있다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            HashSet<string> seenTargets = new HashSet<string>();
            int decoCount = 0;
            foreach (Image image in root.GetComponentsInChildren<Image>(true))
            {
                if (PopupRaycastTargets.Contains(image.name))
                {
                    Assert.IsTrue(image.raycastTarget, $"{image.name} 클릭 대상은 raycastTarget 유지");
                    seenTargets.Add(image.name);
                    continue;
                }
                ++decoCount;
                Assert.IsFalse(image.raycastTarget, $"SynergyModalPopup/{image.name} raycastTarget");
            }

            Assert.AreEqual(PopupRaycastTargets.Count, seenTargets.Count, "클릭 대상 6종이 모두 존재해야 한다");
            Assert.Greater(decoCount, 18, "팝업 장식 Image 수(신규 18종 + 셀 장식 포함)");
        }

        [Test]
        public void 팝업_프리팹에_신규_장식_오브젝트가_모두_존재한다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            string[] names =
            {
                "BodyGradient", "BodyGloss", "BodyFrame", "CornerTL", "CornerTR", "CornerBL", "CornerBR", "RuneL", "RuneR",
                "TitleGlow", "PlaqueGradient", "PlaqueFrame", "PlaqueDiamondL", "PlaqueDiamondR", "EmptyDiamond",
                "CloseGlow", "HandleShine", "HandleShade",
            };
            foreach (string name in names)
            {
                Transform t = FindChild(root, name);
                Assert.IsTrue(t.GetComponent<Image>() != null, name + " Image");
            }
        }

        //# --- 팝업 프리팹 마감 (§2.1·§2.2) ---

        [Test]
        public void 팝업_ModalBody_자식_형제_순서가_기획서_2_1과_같다()
        {
            Transform body = FindChild(LoadPrefabAsset(PopupPath), "ModalBody");
            string[] expected =
            {
                "BodyGradient", "BodyGloss", "BodyFrame", "CornerTL", "CornerTR", "CornerBL", "CornerBR",
                "RuneL", "RuneR", "TitleGlow", "TitlePlaque", "Title", "EmptyText", "ScrollView", "CloseGlow", "CloseButton",
            };
            List<string> actual = new List<string>();
            for (int i = 0; i < body.childCount; ++i)
            {
                actual.Add(body.GetChild(i).name);
            }
            CollectionAssert.AreEqual(expected, actual);
        }

        [Test]
        public void 팝업_CloseButton은_VillageHudCta_스프라이트고_X_글자가_활성이다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            Image close = FindChild(root, "CloseButton").GetComponent<Image>();
            Transform x = FindChild(root, "X");

            Assert.AreEqual("VillageHudCta", close.sprite.name);
            Assert.AreSame(close.transform, x.parent, "X 는 CloseButton 자식");
            Assert.IsTrue(x.gameObject.activeSelf, "X 글자 활성");
        }

        [Test]
        public void 팝업_EmptyDiamond는_EmptyText의_자식이다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);
            Transform diamond = FindChild(root, "EmptyDiamond");
            Transform empty = FindChild(root, "EmptyText");

            Assert.AreSame(empty, diamond.parent);
        }

        [Test]
        public void 팝업_스크롤_Handle_색은_금색_E0A21C다()
        {
            Image handle = FindChild(LoadPrefabAsset(PopupPath), "Handle").GetComponent<Image>();

            AssertRgb(HandleGold, handle.color, "Handle");
        }

        [Test]
        public void 팝업_ModalBody와_Dim은_raycastTarget이_유지된다()
        {
            GameObject root = LoadPrefabAsset(PopupPath);

            Assert.IsTrue(FindChild(root, "ModalBody").GetComponent<Image>().raycastTarget, "ModalBody");
            Assert.IsTrue(FindChild(root, "Dim").GetComponent<Image>().raycastTarget, "Dim");
        }

        //# 이름, anchorMin, anchorMax, anchoredPosition, sizeDelta
        [TestCase("ModalBody", 0.5f, 0.5f, 0.5f, 0.5f, 0f, 0f, 460f, 480f)]
        [TestCase("TitlePlaque", 0.5f, 1f, 0.5f, 1f, 0f, 0f, 320f, 52f)]
        [TestCase("CloseButton", 1f, 1f, 1f, 1f, 13f, 13f, 44f, 44f)]
        [TestCase("ScrollView", 0f, 0f, 1f, 1f, 0f, -22f, -56f, -100f)]
        [TestCase("EmptyText", 0f, 0f, 1f, 1f, 0f, -22f, -56f, -100f)]
        public void 팝업_기존_위젯의_앵커_위치_크기가_변하지_않는다(string name, float minX, float minY, float maxX, float maxY, float posX, float posY, float sizeX, float sizeY)
        {
            RectTransform rect = RectOf(LoadPrefabAsset(PopupPath), name);

            Assert.AreEqual(minX, rect.anchorMin.x, 0.0001f, name + " anchorMin.x");
            Assert.AreEqual(minY, rect.anchorMin.y, 0.0001f, name + " anchorMin.y");
            Assert.AreEqual(maxX, rect.anchorMax.x, 0.0001f, name + " anchorMax.x");
            Assert.AreEqual(maxY, rect.anchorMax.y, 0.0001f, name + " anchorMax.y");
            Assert.AreEqual(posX, rect.anchoredPosition.x, 0.0001f, name + " pos.x");
            Assert.AreEqual(posY, rect.anchoredPosition.y, 0.0001f, name + " pos.y");
            Assert.AreEqual(sizeX, rect.sizeDelta.x, 0.0001f, name + " size.x");
            Assert.AreEqual(sizeY, rect.sizeDelta.y, 0.0001f, name + " size.y");
        }

        //# --- ⑤ 일회용 생성기 삭제 (Rule 04 §3) ---

        [Test]
        public void 일회용_스프라이트_생성기_스크립트가_레포에_없다()
        {
            Assert.IsFalse(File.Exists(GeneratorPath), GeneratorPath);
            Assert.IsFalse(File.Exists(GeneratorPath + ".meta"), GeneratorPath + ".meta");
            foreach (string file in Directory.GetFiles("Assets/_Lair/Editor", "*.cs", SearchOption.AllDirectories))
            {
                StringAssert.DoesNotContain("SynergyModalPolish", Path.GetFileName(file), file);
            }
        }
    }
}
