using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# Backdrop2D 스프라이트 임포트 설정·SheetSpec 정합 — 기획서 scene-2d-conversion §2.3 · §2.4.
    public class Backdrop2DImportTests
    {
        private const string SpecPath = "Assets/_Lair/Art/Sprites/Backdrop2D/Backdrop2D_SheetSpec.json";
        private const string Folder = "Assets/_Lair/Art/Sprites/Backdrop2D/";

        private static JObject LoadFiles()
        {
            JObject root = JObject.Parse(File.ReadAllText(SpecPath));
            return (JObject)root["files"];
        }

        private static IEnumerable<KeyValuePair<string, JToken>> Backdrop2DEntries()
        {
            foreach (KeyValuePair<string, JToken> kv in LoadFiles())
            {
                if ((string)kv.Value["folder"] == "Backdrop2D")
                    yield return kv;
            }
        }

        private static TextureImporter ImporterOf(string folder, string file)
        {
            return (TextureImporter)AssetImporter.GetAtPath(folder + file);
        }

        [Test]
        public void SheetSpec의_Backdrop2D_파일이_전부_존재한다()
        {
            int n = 0;
            foreach (KeyValuePair<string, JToken> kv in Backdrop2DEntries())
            {
                n++;
                Assert.IsTrue(File.Exists(Folder + kv.Key), kv.Key);
            }
            Assert.Greater(n, 20);
        }

        [Test]
        public void 폴더의_PNG는_전부_SheetSpec에_기록돼_있다()
        {
            JObject files = LoadFiles();
            foreach (string path in Directory.GetFiles(Folder, "*.png"))
            {
                string name = Path.GetFileName(path);
                //# 공용 1×1·2×1 점은 추출 도구가 같은 JSON 에 기록한다
                Assert.IsTrue(files.ContainsKey(name), name + " 이 SheetSpec 에 없음");
            }
        }

        [Test]
        public void 텍스처_실제_크기가_SheetSpec_크기와_같다()
        {
            foreach (KeyValuePair<string, JToken> kv in Backdrop2DEntries())
            {
                TextureImporter imp = ImporterOf(Folder, kv.Key);
                Assert.IsNotNull(imp, kv.Key);
                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                Assert.AreEqual((int)kv.Value["size"][0], w, kv.Key + " width");
                Assert.AreEqual((int)kv.Value["size"][1], h, kv.Key + " height");
            }
        }

        [Test]
        public void 쿠키를_제외한_스프라이트는_Point_무압축_밉맵_없음_PPU_48이다()
        {
            foreach (KeyValuePair<string, JToken> kv in Backdrop2DEntries())
            {
                if (kv.Key == "Light2D_Falloff.png")
                    continue;
                TextureImporter imp = ImporterOf(Folder, kv.Key);
                Assert.AreEqual(TextureImporterType.Sprite, imp.textureType, kv.Key);
                Assert.AreEqual(FilterMode.Point, imp.filterMode, kv.Key);
                Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.textureCompression, kv.Key);
                Assert.IsFalse(imp.mipmapEnabled, kv.Key);
                Assert.AreEqual(48f, imp.spritePixelsPerUnit, 1e-4f, kv.Key);
                Assert.AreEqual(TextureWrapMode.Clamp, imp.wrapMode, kv.Key);
            }
        }

        [Test]
        public void 조명_쿠키는_Bilinear_sRGB_꺼짐_PPU_128이다()
        {
            TextureImporter imp = ImporterOf(Folder, "Light2D_Falloff.png");
            Assert.AreEqual(FilterMode.Bilinear, imp.filterMode);
            Assert.IsFalse(imp.sRGBTexture);
            Assert.AreEqual(128f, imp.spritePixelsPerUnit, 1e-4f);
            Assert.AreEqual(TextureImporterCompression.Uncompressed, imp.textureCompression);
        }

        [Test]
        public void 배경_텍스처_최대_크기는_2048_이상이다()
        {
            foreach (string name in new[] { "Battle_Backdrop.png", "Village_Backdrop.png", "Loading_Backdrop.png" })
            {
                TextureImporter imp = ImporterOf(Folder, name);
                Assert.GreaterOrEqual(imp.maxTextureSize, 2048, name);
            }
        }

        [Test]
        public void 와이드_배경_크기는_기획서_편차_기록과_같다()
        {
            JObject files = LoadFiles();
            Assert.AreEqual(1600, (int)files["Battle_Backdrop.png"]["size"][0]);
            Assert.AreEqual(704, (int)files["Battle_Backdrop.png"]["size"][1]);
            Assert.AreEqual(640, (int)files["Village_Backdrop.png"]["size"][0]);
            Assert.AreEqual(270, (int)files["Village_Backdrop.png"]["size"][1]);
            Assert.AreEqual(640, (int)files["Loading_Backdrop.png"]["size"][0]);
            Assert.AreEqual(270, (int)files["Loading_Backdrop.png"]["size"][1]);
        }

        [Test]
        public void 시트는_프레임_수만큼_슬라이스되고_셀_크기가_같다()
        {
            foreach (KeyValuePair<string, JToken> kv in Backdrop2DEntries())
            {
                if (kv.Value["cell"] == null || kv.Value["cell"].Type == JTokenType.Null)
                    continue;
                int frames = (int)kv.Value["frames"];
                int cw = (int)kv.Value["cell"][0];
                int ch = (int)kv.Value["cell"][1];
                Assert.AreEqual(cw * frames, (int)kv.Value["size"][0], kv.Key + " 시트 폭");
                Assert.AreEqual(ch, (int)kv.Value["size"][1], kv.Key + " 시트 높이");
                Object[] assets = AssetDatabase.LoadAllAssetRepresentationsAtPath(Folder + kv.Key);
                int sprites = 0;
                foreach (Object a in assets)
                {
                    Sprite s = a as Sprite;
                    if (s == null)
                        continue;
                    sprites++;
                    Assert.AreEqual(cw, s.rect.width, kv.Key + " 셀 폭");
                    Assert.AreEqual(ch, s.rect.height, kv.Key + " 셀 높이");
                }
                Assert.AreEqual(frames, sprites, kv.Key + " 슬라이스 수");
            }
        }

        [Test]
        public void 불꽃_시트는_모두_24프레임_12fps다()
        {
            foreach (KeyValuePair<string, JToken> kv in Backdrop2DEntries())
            {
                if (kv.Key.StartsWith("Flame_") == false)
                    continue;
                Assert.AreEqual(24, (int)kv.Value["frames"], kv.Key);
                Assert.AreEqual(12, (int)kv.Value["fps"], kv.Key);
            }
        }

        [Test]
        public void UiDot_접기_아이콘은_PPU_25_Point다()
        {
            foreach (string name in new[] { "Px_Minus.png", "Px_Plus.png" })
            {
                TextureImporter imp = ImporterOf("Assets/_Lair/Art/Sprites/UiDot/", name);
                Assert.IsNotNull(imp, name);
                Assert.AreEqual(25f, imp.spritePixelsPerUnit, 1e-4f, name);
                Assert.AreEqual(FilterMode.Point, imp.filterMode, name);
                imp.GetSourceTextureWidthAndHeight(out int w, out int h);
                Assert.AreEqual(5, w, name);
                Assert.AreEqual(5, h, name);
            }
        }
    }
}
