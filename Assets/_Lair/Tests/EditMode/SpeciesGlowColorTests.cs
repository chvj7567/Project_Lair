using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# SpeciesGlowColor 발광색 단일 SoT 회귀 (monster-species-enhancement §4.2).
    //# 정규화 규칙(색조 유지 + 최대 RGB 0.90)·표값 정확 일치를 박제 — 색 편집이 규칙을 깨면 즉시 FAIL.
    public class SpeciesGlowColorTests
    {
        //# §4.2 표 — 각 종족 원본 SpeciesColor(평면 식별색) : 정규화된 SpeciesGlowColor(발광 SoT).
        private struct Row
        {
            public EMonster Species;
            public Color Source;   //# 원본 SpeciesColor(§4.2 표 좌열) — 색조 보존 검증 기준
            public Color Glow;     //# 기대 SpeciesGlowColor(§4.2 표 우열)
        }

        private static readonly Row[] Table =
        {
            new Row { Species = EMonster.Wisp,    Source = new Color(0.133f, 0.773f, 0.369f), Glow = new Color(0.155f, 0.900f, 0.430f) },
            new Row { Species = EMonster.Wraith,  Source = new Color(0.420f, 0.447f, 0.502f), Glow = new Color(0.753f, 0.801f, 0.900f) },
            new Row { Species = EMonster.Reaper,  Source = new Color(0.937f, 0.267f, 0.267f), Glow = new Color(0.900f, 0.256f, 0.256f) },
            new Row { Species = EMonster.Hex,     Source = new Color(0.918f, 0.702f, 0.031f), Glow = new Color(0.900f, 0.688f, 0.030f) },
            new Row { Species = EMonster.Plague,  Source = new Color(0.659f, 0.333f, 0.969f), Glow = new Color(0.612f, 0.309f, 0.900f) },
            new Row { Species = EMonster.Phantom, Source = new Color(0.122f, 0.161f, 0.216f), Glow = new Color(0.508f, 0.671f, 0.900f) },
        };

        [Test]
        public void 여섯종족_발광색이_기획서_표값과_정확히_일치한다()
        {
            foreach (Row r in Table)
            {
                Color c = SpeciesVisual.SpeciesGlowColor(r.Species);
                Assert.AreEqual(r.Glow.r, c.r, 1e-3f, $"{r.Species} R");
                Assert.AreEqual(r.Glow.g, c.g, 1e-3f, $"{r.Species} G");
                Assert.AreEqual(r.Glow.b, c.b, 1e-3f, $"{r.Species} B");
            }
        }

        [Test]
        public void 여섯종족_최대_RGB성분이_0_90으로_정규화된다()
        {
            foreach (Row r in Table)
            {
                Color c = SpeciesVisual.SpeciesGlowColor(r.Species);
                Assert.AreEqual(0.90f, c.maxColorComponent, 1e-3f, $"{r.Species} max=0.90");
            }
        }

        //# 색조 보존 — 발광색 = 원본 SpeciesColor 를 (0.90/max) 로 균일 스케일한 값(§4.2). 특히 정규화된 Wraith·Phantom.
        [Test]
        public void 발광색이_원본_식별색의_색조_비율을_보존한다()
        {
            foreach (Row r in Table)
            {
                float scale = 0.90f / r.Source.maxColorComponent;
                Color expected = new Color(r.Source.r * scale, r.Source.g * scale, r.Source.b * scale);
                Color c = SpeciesVisual.SpeciesGlowColor(r.Species);
                Assert.AreEqual(expected.r, c.r, 0.01f, $"{r.Species} 색조 R");
                Assert.AreEqual(expected.g, c.g, 0.01f, $"{r.Species} 색조 G");
                Assert.AreEqual(expected.b, c.b, 0.01f, $"{r.Species} 색조 B");
            }
        }

        [Test]
        public void 어두운_두_종족도_다크배경_가시_하한을_넘는다()
        {
            //# Wraith·Phantom 은 원본이 어두워 정규화 대상 — 정규화 후 최대 성분이 다크 배경(#262626=0.149) 위 프레임 하한을 확실히 넘어야 한다(§4.2 BLOCKER 해소).
            Assert.Greater(SpeciesVisual.SpeciesGlowColor(EMonster.Wraith).maxColorComponent, 0.6f);
            Assert.Greater(SpeciesVisual.SpeciesGlowColor(EMonster.Phantom).maxColorComponent, 0.6f);
        }

        [Test]
        public void 발광색_알파는_1이다()
        {
            foreach (Row r in Table)
            {
                Assert.AreEqual(1f, SpeciesVisual.SpeciesGlowColor(r.Species).a, 1e-4f, $"{r.Species} alpha");
            }
        }

        //# 가드 — 정의되지 않은 종족 값은 흰색 폴백(발광 파이프가 예외 없이 동작).
        [Test]
        public void 정의되지_않은_종족은_흰색_폴백이다()
        {
            Assert.AreEqual(Color.white, SpeciesVisual.SpeciesGlowColor((EMonster)999));
        }
    }
}
