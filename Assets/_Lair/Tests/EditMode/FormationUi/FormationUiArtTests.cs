using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 화면 GPT 아트 세트(FormationUI 16장) - 임포트 설정·원본 크기(화면 x3)·9-slice 보더 확인.
    public class FormationUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/FormationUI/";
        private const int Scale = 3;

        //# (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더).
        private static readonly object[][] Cases =
        {
            new object[] { "FormationPanel", 1136, 640, 180f, 180f, 180f, 180f },
            new object[] { "FormationTitlePlate", 336, 48, 108f, 54f, 108f, 54f },
            new object[] { "FormationCloseBtn", 48, 48, 0f, 0f, 0f, 0f },
            new object[] { "FormationTileBg", 136, 200, 60f, 60f, 60f, 60f },
            new object[] { "FormationTileRing", 136, 200, 60f, 60f, 60f, 60f },
            new object[] { "FormationSummaryPanel", 300, 472, 72f, 72f, 72f, 72f },
            new object[] { "FormationHeaderBand", 1072, 56, 60f, 0f, 60f, 0f },
            new object[] { "FormationFooterBar", 480, 56, 72f, 48f, 72f, 48f },
            new object[] { "FormationSummaryRow", 332, 72, 60f, 60f, 60f, 60f },
            new object[] { "FormationDivider", 276, 12, 60f, 0f, 60f, 0f },
            new object[] { "FormationIconFrame", 72, 72, 60f, 60f, 60f, 60f },
            new object[] { "FormationRaceBand", 128, 14, 36f, 12f, 36f, 12f },
            new object[] { "FormationStar", 14, 14, 0f, 0f, 0f, 0f },
            new object[] { "FormationArrowR", 24, 24, 0f, 0f, 0f, 0f },
            new object[] { "FormationArrowL", 24, 24, 0f, 0f, 0f, 0f },
            new object[] { "FormationChip", 76, 20, 30f, 18f, 30f, 18f },
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
        public void 편성_아트_16장이_모두_표에_있다()
        {
            Assert.AreEqual(16, Cases.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 편성_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ArtDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
            Assert.AreEqual(SpriteImportMode.Single, importer.spriteImportMode, name + " Single");
            Assert.AreEqual(300f, importer.spritePixelsPerUnit, 0.001f, name + " PPU");
            Assert.AreEqual(new Vector4(left, bottom, right, top), importer.spriteBorder, name + " 보더(L,B,R,T)");
        }

        [TestCaseSource(nameof(Cases))]
        public void 편성_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 편성_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }
    }
}
