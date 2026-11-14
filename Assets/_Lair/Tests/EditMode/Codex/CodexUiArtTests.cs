using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 도감 GPT 아트 세트(monster-codex-ui-art §2·§3) - 36장 임포트 설정과 구 스프라이트 참조 제거 확인.
    public class CodexUiArtTests
    {
        private const string ArtDir = "Assets/_Lair/Art/Sprites/CodexUI/";
        private const string UiDir = "Assets/_Lair/Art/UI/";

        //# (이름, 좌, 하, 우, 상 보더)
        private static readonly object[][] Cases =
        {
            new object[] { "CodexPanelFrame", 108f, 108f, 108f, 108f },
            new object[] { "CodexPanel", 48f, 48f, 48f, 48f },
            new object[] { "CodexTitlePlaque", 90f, 0f, 90f, 0f },
            new object[] { "CodexDivider", 30f, 0f, 30f, 0f },
            new object[] { "CodexTabOff", 36f, 36f, 36f, 36f },
            new object[] { "CodexTabOn", 36f, 36f, 36f, 36f },
            new object[] { "CodexCloseBtn", 0f, 0f, 0f, 0f },
            new object[] { "CodexChip", 48f, 48f, 48f, 48f },
            new object[] { "CodexChipOn", 48f, 48f, 48f, 48f },
            new object[] { "CodexChipBand", 0f, 0f, 0f, 0f },
            new object[] { "CodexCellBg", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellBgLocked", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameG1", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameG2", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameG3", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameG4", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameG5", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFrameLocked", 36f, 36f, 36f, 36f },
            new object[] { "CodexCellFramePulse", 36f, 36f, 36f, 36f },
            new object[] { "CodexRowHeader", 36f, 36f, 36f, 36f },
            new object[] { "CodexTierHeader", 24f, 24f, 24f, 24f },
            new object[] { "CodexTag", 24f, 24f, 24f, 24f },
            new object[] { "CodexStar", 0f, 0f, 0f, 0f },
            new object[] { "CodexNewBadge", 12f, 12f, 12f, 12f },
            new object[] { "CodexNewDot", 0f, 0f, 0f, 0f },
            new object[] { "CodexCheck", 0f, 0f, 0f, 0f },
            new object[] { "CodexCross", 0f, 0f, 0f, 0f },
            new object[] { "CodexLock", 0f, 0f, 0f, 0f },
            new object[] { "CodexProgressTrack", 21f, 21f, 21f, 21f },
            new object[] { "CodexProgressFill", 0f, 0f, 0f, 0f },
            new object[] { "CodexPreviewBg", 0f, 0f, 0f, 0f },
            new object[] { "CodexGlow", 0f, 0f, 0f, 0f },
            new object[] { "CodexUnlockRing", 0f, 0f, 0f, 0f },
            new object[] { "CodexSpark", 0f, 0f, 0f, 0f },
            new object[] { "CodexScrollTrack", 18f, 18f, 18f, 18f },
            new object[] { "CodexScrollThumb", 18f, 18f, 18f, 18f },
        };

        [Test]
        public void 도감_아트_36장이_모두_표에_있다()
        {
            Assert.AreEqual(36, Cases.Length);
        }

        [TestCaseSource(nameof(Cases))]
        public void 도감_스프라이트_임포트_설정이_표와_일치한다(string name, float left, float bottom, float right, float top)
        {
            TextureImporter importer = AssetImporter.GetAtPath(ArtDir + name + ".png") as TextureImporter;
            Assert.IsNotNull(importer, name);
            Assert.AreEqual(TextureImporterType.Sprite, importer.textureType, name);
            Assert.AreEqual(300f, importer.spritePixelsPerUnit, 0.001f, name + " PPU");
            Assert.AreEqual(FilterMode.Bilinear, importer.filterMode, name + " 필터");
            Assert.IsFalse(importer.mipmapEnabled, name + " 밉맵");
            Assert.IsTrue(importer.alphaIsTransparency, name + " alphaIsTransparency");
            Assert.AreEqual(new Vector4(left, bottom, right, top), importer.spriteBorder, name + " 보더(L,B,R,T)");
        }

        [TestCase("CodexPopup")]
        [TestCase("CodexMonsterCell")]
        public void 도감_프리팹은_구_Px_Codex_스프라이트를_참조하지_않는다(string prefab)
        {
            foreach (string dependency in AssetDatabase.GetDependencies(UiDir + prefab + ".prefab", true))
            {
                StringAssert.DoesNotContain("Px_CodexCellFrame", dependency);
                StringAssert.DoesNotContain("Px_CodexGlow", dependency);
                StringAssert.DoesNotContain("Px_CodexSpark", dependency);
            }
        }
    }
}
