using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 시너지 진행도 산식(기획서 card-synergy-indicator §2, 수용 기준 A-1~A-3, A-5). 정상 + 엣지 1개 수준.
    public class SynergyProgressTests
    {
        [TestCase(0, "0/3")]
        [TestCase(2, "2/3")]
        [TestCase(3, "3/5 T1")]
        [TestCase(5, "5/7 T2")]
        [TestCase(6, "6/7 T2")]
        [TestCase(7, "7+ T3")]
        [TestCase(9, "9+ T3")]
        public void CountText_장수별_문구(int count, string expected)
        {
            Assert.AreEqual(expected, SynergyProgress.CountText(count));
        }

        [Test]
        public void Opacity_0장만_흐림_1장부터_선명()
        {
            Assert.AreEqual(0.55f, SynergyProgress.Opacity(0), 0.0001f);
            Assert.AreEqual(1f, SynergyProgress.Opacity(1), 0.0001f);
            Assert.IsFalse(SynergyProgress.HasStrip(2));
            Assert.IsTrue(SynergyProgress.HasStrip(3));
        }

        [Test]
        public void FilledCells_7칸_상한_및_Tier칸_위치()
        {
            Assert.AreEqual(4, SynergyProgress.FilledCells(4));
            Assert.AreEqual(7, SynergyProgress.FilledCells(12));
            Assert.IsTrue(SynergyProgress.IsTierCell(2));
            Assert.IsTrue(SynergyProgress.IsTierCell(4));
            Assert.IsTrue(SynergyProgress.IsTierCell(6));
            Assert.IsFalse(SynergyProgress.IsTierCell(3));
        }

        [Test]
        public void SynergyVisualConfig_누락_축은_null()
        {
            SynergyVisualConfig config = ScriptableObject.CreateInstance<SynergyVisualConfig>();
            Assert.IsNull(config.GetIcon(EBuildAxis.Tank));
            Object.DestroyImmediate(config);
        }
    }
}
