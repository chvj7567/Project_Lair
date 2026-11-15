using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 월드맵 화면 GPT 아트 세트(WorldMapUI 21장) - 임포트 설정·원본 크기(화면 x3)·9-slice 보더 확인. 프리팹 참조는 적용 후 별도 추가.
    public class WorldMapUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/WorldMapUI/";
        private const int Scale = 3;

        //# (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더).
        private static readonly object[][] Cases =
        {
            new object[] { "WorldMapPanel", 1136, 640, 63f, 63f, 63f, 63f },
            new object[] { "WorldMapTitlePlate", 320, 52, 120f, 60f, 120f, 60f },
            new object[] { "WorldMapCloseBtn", 48, 48, 0f, 0f, 0f, 0f },
            new object[] { "WorldMapHeroPanel", 720, 480, 63f, 63f, 63f, 63f },
            new object[] { "WorldMapCellBg", 152, 192, 45f, 45f, 45f, 45f },
            new object[] { "WorldMapCellRing", 152, 192, 36f, 36f, 36f, 36f },
            new object[] { "WorldMapPortraitSlot", 120, 120, 54f, 54f, 54f, 54f },
            new object[] { "WorldMapTabOn", 200, 84, 60f, 60f, 60f, 60f },
            new object[] { "WorldMapTabOff", 200, 84, 60f, 60f, 60f, 60f },
            new object[] { "WorldMapTabLock", 200, 84, 60f, 60f, 60f, 60f },
            new object[] { "WorldMapTabBadge", 52, 84, 36f, 36f, 36f, 36f },
            new object[] { "WorldMapStar", 16, 16, 0f, 0f, 0f, 0f },
            new object[] { "WorldMapMapFrame", 520, 472, 54f, 54f, 54f, 54f },
            new object[] { "WorldMapDetailPanel", 352, 472, 60f, 60f, 60f, 60f },
            new object[] { "WorldMapPortraitFrame", 328, 120, 60f, 60f, 60f, 60f },
            new object[] { "WorldMapRulesBox", 328, 208, 54f, 54f, 54f, 54f },
            new object[] { "WorldMapRecordBox", 328, 48, 48f, 48f, 48f, 48f },
            new object[] { "WorldMapNamePlate", 60, 18, 24f, 24f, 24f, 24f },
            new object[] { "WorldMapDiffEasy", 70, 44, 45f, 45f, 45f, 45f },
            new object[] { "WorldMapDiffNormal", 70, 44, 45f, 45f, 45f, 45f },
            new object[] { "WorldMapDiffHard", 70, 44, 45f, 45f, 45f, 45f },
        };

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

        [Test]
        public void 월드맵_아트_21장이_모두_표에_있다()
        {
            Assert.AreEqual(21, Cases.Length);
        }

        [Test]
        public void 월드맵_아트_폴더에는_표의_21장_외_PNG가_없다()
        {
            string[] files = Directory.GetFiles(ProjectPath(ArtDir), "*.png");
            Assert.AreEqual(Cases.Length, files.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 월드맵_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
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
        public void 월드맵_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 월드맵_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }
    }
}
