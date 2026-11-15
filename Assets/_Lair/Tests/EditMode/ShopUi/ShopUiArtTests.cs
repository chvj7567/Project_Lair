using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 상점 화면 GPT 아트 세트(ShopUI 13장) - 임포트 설정·원본 크기(화면 x3)·9-slice 보더·프리팹 참조/깨진 스프라이트 GUID 확인.
    public class ShopUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/ShopUI/";
        private const string CommonDir = "Assets/_Lair/Art/Sprites/CommonUI/";
        private const string PopupPrefab = "Assets/_Lair/Art/UI/ShopPopup.prefab";
        private const string CellPrefab = "Assets/_Lair/Art/UI/ShopItemCell.prefab";
        private const int Scale = 3;

        //# (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더).
        private static readonly object[][] Cases =
        {
            new object[] { "ShopPanel", 720, 480, 192f, 192f, 192f, 192f },
            new object[] { "ShopTitlePlate", 320, 52, 120f, 60f, 120f, 60f },
            new object[] { "ShopCloseBtn", 44, 44, 0f, 0f, 0f, 0f },
            new object[] { "ShopBar", 232, 44, 60f, 48f, 60f, 48f },
            new object[] { "ShopPill", 112, 32, 36f, 36f, 36f, 36f },
            new object[] { "ShopDivider", 664, 12, 60f, 0f, 60f, 0f },
            new object[] { "ShopScrollTrack", 10, 100, 0f, 30f, 0f, 30f },
            new object[] { "ShopScrollThumb", 8, 40, 0f, 12f, 0f, 12f },
            new object[] { "ShopCellBg", 664, 108, 36f, 36f, 36f, 36f },
            new object[] { "ShopCellRing", 664, 108, 30f, 30f, 30f, 30f },
            new object[] { "ShopHeaderBand", 664, 34, 60f, 30f, 60f, 30f },
            new object[] { "ShopIconSlot", 68, 68, 60f, 60f, 60f, 60f },
            new object[] { "ShopPip", 24, 24, 0f, 0f, 0f, 0f },
        };

        private static readonly string[] PopupSprites =
        {
            "ShopPanel", "ShopTitlePlate", "ShopCloseBtn", "ShopBar", "ShopPill", "ShopDivider", "ShopScrollTrack", "ShopScrollThumb",
        };

        private static readonly string[] CellSprites =
        {
            "ShopCellBg", "ShopCellRing", "ShopHeaderBand", "ShopIconSlot", "ShopPip",
        };

        private static readonly string[] CellCommonSprites =
        {
            "CommonUIBtnSoul", "CommonUIBtnOff", "CommonUIBtnGold", "CommonUIPanelSunk",
        };

        private static readonly Regex InlineSpriteRef = new Regex(@"m_Sprite:\s*\{[^}]*guid:\s*([0-9a-f]{32})");
        private static readonly Regex OverrideSpriteRef = new Regex(@"propertyPath:\s*m_Sprite\s+value:[^\n]*\n\s*objectReference:\s*\{[^}]*guid:\s*([0-9a-f]{32})");

        private static string ProjectPath(string assetPath)
        {
            return Path.Combine(Directory.GetParent(Application.dataPath).FullName, assetPath);
        }

        //# PNG IHDR(오프셋 16/20, 빅엔디언)에서 원본 픽셀 크기를 직접 읽는다 - 임포터 max size 와 무관.
        private static void ReadPngSize(string name, out int width, out int height)
        {
            byte[] header = new byte[24];
            using (FileStream stream = File.OpenRead(ProjectPath(ArtDir + name + ".png")))
            {
                Assert.AreEqual(24, stream.Read(header, 0, 24), name + " PNG 헤더");
            }

            width = (header[16] << 24) | (header[17] << 16) | (header[18] << 8) | header[19];
            height = (header[20] << 24) | (header[21] << 16) | (header[22] << 8) | header[23];
        }

        private static string ReadPrefab(string prefabPath)
        {
            return File.ReadAllText(ProjectPath(prefabPath));
        }

        private static void AssertReferences(string prefabPath, string dir, string[] names)
        {
            string text = ReadPrefab(prefabPath);
            foreach (string name in names)
            {
                string guid = AssetDatabase.AssetPathToGUID(dir + name + ".png");
                Assert.IsNotEmpty(guid, name + " GUID");
                StringAssert.Contains(guid, text, prefabPath + " 가 " + name + " 를 참조해야 한다");
            }
        }

        private static void AssertNoMissingSprite(string prefabPath)
        {
            string text = ReadPrefab(prefabPath);
            List<string> missing = new List<string>();
            foreach (Regex regex in new[] { InlineSpriteRef, OverrideSpriteRef })
            {
                foreach (Match match in regex.Matches(text))
                {
                    string guid = match.Groups[1].Value;
                    //# 0000... 으로 시작하는 GUID 는 유니티 내장 리소스(기본 UI 스프라이트 등)라 .meta 가 없다.
                    if (guid.StartsWith("0000000000000000"))
                        continue;

                    if (string.IsNullOrEmpty(AssetDatabase.GUIDToAssetPath(guid)))
                    {
                        missing.Add(guid);
                    }
                }
            }

            Assert.IsEmpty(missing, prefabPath + " 깨진 스프라이트 GUID: " + string.Join(",", missing));
        }

        [Test]
        public void 상점_아트_13장이_모두_표에_있다()
        {
            Assert.AreEqual(13, Cases.Length);
        }

        [Test]
        public void 상점_아트_폴더에는_표의_13장_외_PNG가_없다()
        {
            string[] files = Directory.GetFiles(ProjectPath(ArtDir), "*.png");
            Assert.AreEqual(Cases.Length, files.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 상점_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ArtDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, name + " Single");
            Assert.AreEqual(300f, importer.spritePixelsPerUnit, 0.001f, name + " PPU");
            Assert.IsFalse(importer.mipmapEnabled, name + " 밉맵");
            Assert.IsTrue(importer.alphaIsTransparency, name + " alphaIsTransparency");
            Assert.AreEqual(new Vector4(left, bottom, right, top), importer.spriteBorder, name + " 보더(L,B,R,T)");
        }

        [TestCaseSource(nameof(Cases))]
        public void 상점_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 상점_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }

        [Test]
        public void ShopPopup_프리팹이_상점_스프라이트_8장을_모두_참조한다()
        {
            AssertReferences(PopupPrefab, ArtDir, PopupSprites);
        }

        [Test]
        public void ShopItemCell_프리팹이_상점_스프라이트_5장을_모두_참조한다()
        {
            AssertReferences(CellPrefab, ArtDir, CellSprites);
        }

        [Test]
        public void ShopItemCell_프리팹이_공통_UI_버튼_패널_스프라이트를_참조한다()
        {
            AssertReferences(CellPrefab, CommonDir, CellCommonSprites);
        }

        [Test]
        public void ShopPopup_프리팹에_깨진_스프라이트_GUID가_없다()
        {
            AssertNoMissingSprite(PopupPrefab);
        }

        [Test]
        public void ShopItemCell_프리팹에_깨진_스프라이트_GUID가_없다()
        {
            AssertNoMissingSprite(CellPrefab);
        }
    }
}
