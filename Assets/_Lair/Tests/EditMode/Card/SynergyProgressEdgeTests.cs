using Lair.Card;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# 시너지 진행도 산식 경계 망라(기획서 card-synergy-indicator §2·§6, 수용 기준 A-1~A-3). 기존 SynergyProgressTests 와 겹치지 않는 값 위주.
    public class SynergyProgressEdgeTests
    {
        [TestCase(-3, 0)]
        [TestCase(0, 0)]
        [TestCase(2, 0)]
        [TestCase(3, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(6, 2)]
        [TestCase(7, 3)]
        [TestCase(8, 3)]
        [TestCase(999, 3)]
        public void ActiveTier_임계_경계(int count, int expected)
        {
            Assert.AreEqual(expected, SynergyProgress.ActiveTier(count));
        }

        [TestCase(-1, 3)]
        [TestCase(0, 3)]
        [TestCase(2, 3)]
        [TestCase(3, 5)]
        [TestCase(4, 5)]
        [TestCase(5, 7)]
        [TestCase(6, 7)]
        [TestCase(7, -1)]
        [TestCase(8, -1)]
        public void NextThreshold_다음_임계_또는_없음(int count, int expected)
        {
            Assert.AreEqual(expected, SynergyProgress.NextThreshold(count));
        }

        [TestCase(1, "1/3")]
        [TestCase(4, "4/5 T1")]
        [TestCase(8, "8+ T3")]
        [TestCase(100, "100+ T3")]
        public void CountText_추가_경계(int count, string expected)
        {
            Assert.AreEqual(expected, SynergyProgress.CountText(count));
        }

        [Test]
        public void CountText_임계_직전직후_Tier_접미사_전환()
        {
            StringAssert.DoesNotContain("T", SynergyProgress.CountText(2));
            StringAssert.EndsWith("T1", SynergyProgress.CountText(3));
            StringAssert.EndsWith("T1", SynergyProgress.CountText(4));
            StringAssert.EndsWith("T2", SynergyProgress.CountText(5));
            StringAssert.EndsWith("T2", SynergyProgress.CountText(6));
            StringAssert.EndsWith("T3", SynergyProgress.CountText(7));
        }

        [TestCase(-5)]
        [TestCase(0)]
        public void Opacity_0이하는_흐림(int count)
        {
            Assert.AreEqual(SynergyProgress.DimOpacity, SynergyProgress.Opacity(count), 0.0001f);
            Assert.AreEqual(0.55f, SynergyProgress.DimOpacity, 0.0001f);
        }

        [TestCase(1)]
        [TestCase(2)]
        [TestCase(3)]
        [TestCase(7)]
        [TestCase(50)]
        public void Opacity_1장_이상은_불투명(int count)
        {
            Assert.AreEqual(1f, SynergyProgress.Opacity(count), 0.0001f);
        }

        [TestCase(0, false)]
        [TestCase(1, false)]
        [TestCase(2, false)]
        [TestCase(3, true)]
        [TestCase(4, true)]
        [TestCase(7, true)]
        [TestCase(20, true)]
        public void HasStrip_Tier1_이상만(int count, bool expected)
        {
            Assert.AreEqual(expected, SynergyProgress.HasStrip(count));
        }

        [TestCase(-10, 0)]
        [TestCase(-1, 0)]
        [TestCase(0, 0)]
        [TestCase(1, 1)]
        [TestCase(6, 6)]
        [TestCase(7, 7)]
        [TestCase(8, 7)]
        [TestCase(int.MaxValue, 7)]
        public void FilledCells_음수_0_상한(int count, int expected)
        {
            Assert.AreEqual(expected, SynergyProgress.FilledCells(count));
            Assert.AreEqual(7, SynergyProgress.TrackLength);
        }

        [Test]
        public void IsTierCell_0부터_6까지_3_5_7번째만()
        {
            bool[] expected = { false, false, true, false, true, false, true };
            for (int i = 0; i < expected.Length; ++i)
            {
                Assert.AreEqual(expected[i], SynergyProgress.IsTierCell(i), $"칸 index {i}");
            }
        }

        [TestCase(-1)]
        [TestCase(7)]
        [TestCase(8)]
        public void IsTierCell_범위_밖은_false(int index)
        {
            Assert.IsFalse(SynergyProgress.IsTierCell(index));
        }

        [Test]
        public void IsTierCell_개수_정확히_Tier_수만큼()
        {
            int tierCells = 0;
            for (int i = 0; i < SynergyProgress.TrackLength; ++i)
            {
                if (SynergyProgress.IsTierCell(i))
                    ++tierCells;
            }
            Assert.AreEqual(3, tierCells);
        }

        [TestCase(2, 3, true)]
        [TestCase(0, 3, true)]
        [TestCase(4, 5, true)]
        [TestCase(6, 7, true)]
        [TestCase(2, 5, true)]
        [TestCase(0, 9, true)]
        [TestCase(7, 8, false)]
        [TestCase(3, 4, false)]
        [TestCase(5, 6, false)]
        [TestCase(0, 2, false)]
        [TestCase(1, 2, false)]
        [TestCase(3, 3, false)]
        [TestCase(0, 0, false)]
        public void CrossedThreshold_임계_새로_넘음(int prev, int count, bool expected)
        {
            Assert.AreEqual(expected, SynergyProgress.CrossedThreshold(prev, count));
        }

        [TestCase(3, 2)]
        [TestCase(7, 0)]
        [TestCase(5, 4)]
        public void CrossedThreshold_감소_리셋은_false(int prev, int count)
        {
            Assert.IsFalse(SynergyProgress.CrossedThreshold(prev, count));
        }

        [Test]
        public void 임계_상수_원본은_BuildSynergyService_3_5_7()
        {
            Assert.AreEqual(3, BuildSynergyService.Tier1Threshold);
            Assert.AreEqual(5, BuildSynergyService.Tier2Threshold);
            Assert.AreEqual(7, BuildSynergyService.Tier3Threshold);
            Assert.AreEqual(BuildSynergyService.Tier3Threshold, SynergyProgress.TrackLength);
        }

        [Test]
        public void 모든_장수에서_파생값_일관성_0부터_12()
        {
            for (int c = 0; c <= 12; ++c)
            {
                Assert.AreEqual(SynergyProgress.ActiveTier(c) >= 1, SynergyProgress.HasStrip(c), $"c={c} 띠");
                Assert.AreEqual(SynergyProgress.NextThreshold(c) < 0, SynergyProgress.ActiveTier(c) == 3, $"c={c} 다음임계 없음 == Tier3");
                Assert.LessOrEqual(SynergyProgress.FilledCells(c), SynergyProgress.TrackLength);
            }
        }
    }

    //# SynergyVisualConfig.GetIcon — 4축 할당·누락·null Sprite·중복·null 배열(수용 기준 A-5).
    public class SynergyVisualConfigGetIconTests
    {
        private SynergyVisualConfig _config;
        private Sprite[] _sprites;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<SynergyVisualConfig>();
            _sprites = new Sprite[4];
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _sprites.Length; ++i)
            {
                if (_sprites[i] != null)
                {
                    Object.DestroyImmediate(_sprites[i].texture);
                    Object.DestroyImmediate(_sprites[i]);
                }
            }
            Object.DestroyImmediate(_config);
        }

        private Sprite MakeSprite(int slot)
        {
            Texture2D tex = new Texture2D(12, 12);
            _sprites[slot] = Sprite.Create(tex, new Rect(0, 0, 12, 12), new Vector2(0.5f, 0.5f));
            return _sprites[slot];
        }

        private static SynergyVisualConfig.Entry Entry(EBuildAxis axis, Sprite icon)
        {
            return new SynergyVisualConfig.Entry { Axis = axis, Icon = icon };
        }

        private void SetEntries(params SynergyVisualConfig.Entry[] entries)
        {
            Lair.Tests.EditMode.TestReflection.SetField(_config, "_entries", entries);
        }

        [Test]
        public void GetIcon_4축_전부_할당되면_각자_다른_Sprite()
        {
            SetEntries(
                Entry(EBuildAxis.Tank, MakeSprite(0)),
                Entry(EBuildAxis.Dps, MakeSprite(1)),
                Entry(EBuildAxis.Debuff, MakeSprite(2)),
                Entry(EBuildAxis.Swarm, MakeSprite(3)));

            Assert.AreSame(_sprites[0], _config.GetIcon(EBuildAxis.Tank));
            Assert.AreSame(_sprites[1], _config.GetIcon(EBuildAxis.Dps));
            Assert.AreSame(_sprites[2], _config.GetIcon(EBuildAxis.Debuff));
            Assert.AreSame(_sprites[3], _config.GetIcon(EBuildAxis.Swarm));
            Assert.AreEqual(4, _config.Entries.Length);
        }

        [Test]
        public void GetIcon_일부_축_누락이면_그_축만_null()
        {
            SetEntries(Entry(EBuildAxis.Tank, MakeSprite(0)), Entry(EBuildAxis.Swarm, MakeSprite(3)));

            Assert.IsNotNull(_config.GetIcon(EBuildAxis.Tank));
            Assert.IsNotNull(_config.GetIcon(EBuildAxis.Swarm));
            Assert.IsNull(_config.GetIcon(EBuildAxis.Dps));
            Assert.IsNull(_config.GetIcon(EBuildAxis.Debuff));
        }

        [Test]
        public void GetIcon_항목은_있으나_Sprite_null이면_null()
        {
            SetEntries(Entry(EBuildAxis.Dps, null));

            Assert.IsNull(_config.GetIcon(EBuildAxis.Dps));
        }

        [Test]
        public void GetIcon_배열에_null_항목이_섞여도_예외없이_다음_항목_탐색()
        {
            SetEntries(null, Entry(EBuildAxis.Debuff, MakeSprite(2)));

            Assert.IsNotNull(_config.GetIcon(EBuildAxis.Debuff));
            Assert.IsNull(_config.GetIcon(EBuildAxis.Tank));
        }

        [Test]
        public void GetIcon_Entries_배열이_null이면_null()
        {
            Lair.Tests.EditMode.TestReflection.SetField(_config, "_entries", null);

            Assert.IsNull(_config.GetIcon(EBuildAxis.Tank));
        }

        [Test]
        public void GetIcon_빈_배열이면_전_축_null()
        {
            SetEntries();

            foreach (EBuildAxis axis in BuildSynergyAxes())
            {
                Assert.IsNull(_config.GetIcon(axis), axis.ToString());
            }
        }

        [Test]
        public void GetIcon_같은_축_중복이면_먼저_나온_항목()
        {
            SetEntries(Entry(EBuildAxis.Tank, MakeSprite(0)), Entry(EBuildAxis.Tank, MakeSprite(1)));

            Assert.AreSame(_sprites[0], _config.GetIcon(EBuildAxis.Tank));
        }

        private static EBuildAxis[] BuildSynergyAxes()
        {
            return new[] { EBuildAxis.Tank, EBuildAxis.Dps, EBuildAxis.Debuff, EBuildAxis.Swarm };
        }
    }
}
