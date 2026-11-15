using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 마을 HUD GPT 아트 세트(ui-art-refresh §5.2, 게이트 UA2) - 35장 임포트 설정·원본 크기(화면 x3)·9-slice 보더 확인.
    public class VillageHudUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/VillageHudUI/";
        private const int Scale = 3;

        //# (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더). TopBar 는 장식 폭 때문에 문서(240x264·108)와 달리 408x264·192 (화면 136x88).
        private static readonly object[][] Cases =
        {
            new object[] { "VillageHudTopBar", 136, 88, 192f, 0f, 192f, 0f },
            new object[] { "VillageHudAvatarFrame", 60, 60, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudXpFrame", 170, 16, 24f, 20f, 24f, 20f },
            new object[] { "VillageHudXpFill", 170, 16, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudCurrencyPill", 232, 48, 72f, 60f, 72f, 60f },
            new object[] { "VillageHudMenuTile", 112, 112, 60f, 60f, 60f, 60f },
            new object[] { "VillageHudMenuTileOff", 112, 112, 60f, 60f, 60f, 60f },
            new object[] { "VillageHudMenuTileDailyNew", 112, 112, 60f, 60f, 60f, 60f },
            new object[] { "VillageHudAccountBtn", 112, 52, 48f, 48f, 48f, 48f },
            new object[] { "VillageHudSortieBtn", 232, 72, 90f, 90f, 90f, 90f },
            new object[] { "VillageHudSortieBtnOff", 232, 72, 90f, 90f, 90f, 90f },
            new object[] { "VillageHudSortieGlow", 280, 110, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudSortieGloss", 220, 28, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudHeroBtn", 100, 72, 60f, 60f, 60f, 60f },
            new object[] { "VillageHudStageArrowL", 48, 68, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudStageArrowR", 48, 68, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudStagePlate", 268, 31, 60f, 0f, 60f, 0f },
            new object[] { "VillageHudIntruderCard", 340, 324, 108f, 108f, 108f, 108f },
            new object[] { "VillageHudPortraitFrame", 196, 208, 90f, 90f, 90f, 90f },
            new object[] { "VillageHudStageDot", 14, 14, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudStageDotOff", 14, 14, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudStageDotBoss", 18, 18, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudRecordPanel", 256, 216, 90f, 90f, 90f, 90f },
            new object[] { "VillageHudRecordBullet", 8, 8, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconCastle", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconShop", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconCodex", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconQuest", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconRecords", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconRanking", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconRelic", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconTower", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconDaily", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconMap", 56, 56, 0f, 0f, 0f, 0f },
            new object[] { "VillageHudIconSoul", 24, 24, 0f, 0f, 0f, 0f },
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
        public void 마을_HUD_아트_35장이_모두_표에_있다()
        {
            Assert.AreEqual(35, Cases.Length);
        }

        [Test]
        public void 마을_HUD_아트_폴더에는_표의_35장_외_PNG가_없다()
        {
            string[] files = Directory.GetFiles(ProjectPath(ArtDir), "*.png");
            Assert.AreEqual(Cases.Length, files.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 마을_HUD_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
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
        public void 마을_HUD_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 마을_HUD_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }
    }
}
