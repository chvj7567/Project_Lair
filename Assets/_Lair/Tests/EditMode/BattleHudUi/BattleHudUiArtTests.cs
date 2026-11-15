using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 전투 HUD GPT 아트 세트(BattleHudUI 22장) - 임포트 설정·원본 크기(화면 x3)·9-slice 보더 확인.
    public class BattleHudUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/BattleHudUI/";
        private const int Scale = 3;

        //# 수정 지점: (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더). 보더는 생성 후 조정될 수 있다.
        private static readonly object[][] Cases =
        {
            new object[] { "BattleHudUIBossFrame", 557, 24, 24f, 24f, 24f, 24f },
            new object[] { "BattleHudUIBossFill", 64, 24, 0f, 0f, 0f, 0f },
            new object[] { "BattleHudUIBossFillLow", 64, 24, 0f, 0f, 0f, 0f },
            new object[] { "BattleHudUIBuffChipFrame", 27, 27, 30f, 30f, 30f, 30f },
            new object[] { "BattleHudUIGaugeFrame", 48, 8, 6f, 6f, 6f, 6f },
            new object[] { "BattleHudUIGaugeFill", 32, 8, 0f, 0f, 0f, 0f },
            new object[] { "BattleHudUIBossPlate", 573, 104, 48f, 48f, 48f, 48f },
            new object[] { "BattleHudUIPlate", 108, 69, 54f, 54f, 54f, 54f },
            new object[] { "BattleHudUISidePanel", 165, 243, 66f, 66f, 66f, 66f },
            new object[] { "BattleHudUIFoldTab", 165, 31, 36f, 36f, 36f, 36f },
            new object[] { "BattleHudUIHeaderLine", 139, 4, 24f, 0f, 24f, 0f },
            new object[] { "BattleHudUISpawnerCellBody", 156, 64, 36f, 36f, 36f, 36f },
            new object[] { "BattleHudUISpawnerCellFrame", 156, 64, 36f, 36f, 36f, 36f },
            new object[] { "BattleHudUIRaceBand", 10, 62, 0f, 0f, 0f, 0f },
            new object[] { "BattleHudUISpawnerIconSlot", 48, 48, 45f, 45f, 45f, 45f },
            new object[] { "BattleHudUICountBadge", 30, 20, 24f, 24f, 24f, 24f },
            new object[] { "BattleHudUIBackBtn", 56, 56, 45f, 45f, 45f, 45f },
            new object[] { "BattleHudUISpeedBtnOff", 56, 56, 45f, 45f, 45f, 45f },
            new object[] { "BattleHudUISpeedBtnOn", 56, 56, 45f, 45f, 45f, 45f },
            new object[] { "BattleHudUIIconCellRing", 40, 40, 30f, 30f, 30f, 30f },
            new object[] { "BattleHudUIHpBarTrack", 120, 20, 18f, 18f, 18f, 18f },
            new object[] { "BattleHudUIHpBarFill", 112, 12, 0f, 0f, 0f, 0f },
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
        public void 전투_HUD_아트_22장이_모두_표에_있다()
        {
            Assert.AreEqual(22, Cases.Length);
        }

        [Test]
        public void 전투_HUD_아트_폴더에는_표의_22장_외_PNG가_없다()
        {
            string[] files = Directory.GetFiles(ProjectPath(ArtDir), "*.png");
            Assert.AreEqual(Cases.Length, files.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 전투_HUD_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
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
        public void 전투_HUD_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 전투_HUD_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }
    }
}
