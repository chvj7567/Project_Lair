using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 퀘스트·영주·클라우드 화면 신규 스프라이트(QuestLordCloudUI 9장) - 임포트 설정·원본 크기(화면 x3)·9-slice 보더 확인.
    public class QuestLordCloudUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/QuestLordCloudUI/";
        private const int Scale = 3;

        //# (이름, 화면 너비, 화면 높이, 좌, 하, 우, 상 보더).
        private static readonly object[][] Cases =
        {
            new object[] { "QuestScrollIcon", 52, 52, 0f, 0f, 0f, 0f },
            new object[] { "QuestDoneChip", 76, 36, 60f, 45f, 60f, 45f },
            new object[] { "LordCrest", 92, 92, 0f, 0f, 0f, 0f },
            new object[] { "LordCheckBadge", 36, 36, 0f, 0f, 0f, 0f },
            new object[] { "LordLockBadge", 36, 36, 0f, 0f, 0f, 0f },
            new object[] { "CloudAccountIcon", 70, 56, 0f, 0f, 0f, 0f },
            new object[] { "CloudMiniIcon", 36, 36, 36f, 36f, 36f, 36f },
            new object[] { "LaptopMiniIcon", 36, 36, 36f, 36f, 36f, 36f },
            new object[] { "CloudStatusDot", 16, 16, 0f, 0f, 0f, 0f },
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

        [TestCaseSource(nameof(Cases))]
        public void 퀘스트_영주_클라우드_스프라이트_임포트_설정이_표와_일치한다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ArtDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
            Assert.AreEqual(300f, importer.spritePixelsPerUnit, 0.001f, name + " PPU");
            Assert.AreEqual(new Vector4(left, bottom, right, top), importer.spriteBorder, name + " 보더(L,B,R,T)");
        }

        [TestCaseSource(nameof(Cases))]
        public void 퀘스트_영주_클라우드_원본_크기는_화면_크기의_3배다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.AreEqual(screenW * Scale, width, name + " 너비");
            Assert.AreEqual(screenH * Scale, height, name + " 높이");
        }

        [TestCaseSource(nameof(Cases))]
        public void 퀘스트_영주_클라우드_보더는_원본_크기보다_작다(string name, int screenW, int screenH, float left, float bottom, float right, float top)
        {
            int width;
            int height;
            ReadPngSize(name, out width, out height);
            Assert.Less(left + right, (float)width, name + " L+R < 너비");
            Assert.Less(bottom + top, (float)height, name + " B+T < 높이");
        }
    }
}
