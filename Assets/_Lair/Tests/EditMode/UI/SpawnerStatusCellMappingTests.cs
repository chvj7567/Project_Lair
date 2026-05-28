using NUnit.Framework;
using UnityEngine;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 E 보강 (SpawnerStatusCell 6종 색칩 매핑 + 강화 카드 6종 배경색 매핑).
    //#
    //# gameplay-programmer 가 추가한 SpawnerStatusCellTests 는 SpeciesName / IconLetterFor.letter / fgColor 위주.
    //# 본 테스트는 다음을 추가 검증:
    //#  - SpeciesColor 6종 (#22C55E / #6B7280 / #EF4444 / #EAB308 / #A855F7 / #1F2937 — 컨셉 §11.4).
    //#  - IconLetterFor.bgColor 6종 (강화 카드 — 종 색과 동일해야 함, 기획서 §2.3.3).
    //#  - IconLetterFor.bgColor 비강화는 fallback 색 (gray).
    public class SpawnerStatusCellMappingTests
    {
        //# Hex → RGB(0~1) float — Unity Color 비교용. 정밀도 1/255.
        private static Color HexToColor(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >>  8) & 0xFF) / 255f;
            float b = ( hex        & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        //# 두 Color 가 1/255 이하 차이로 일치하는지.
        private static void AssertColorEquals(Color expected, Color actual, string msg)
        {
            //# RGB 각 채널 ±1/255 허용 (production 코드가 float 으로 직접 적었으므로 표기 차 ≤ 1/255).
            float eps = 1.5f / 255f;
            Assert.AreEqual(expected.r, actual.r, eps, $"{msg} (R)");
            Assert.AreEqual(expected.g, actual.g, eps, $"{msg} (G)");
            Assert.AreEqual(expected.b, actual.b, eps, $"{msg} (B)");
        }

        //# ===== 6종 종 색칩 (컨셉 §11.4 매핑 회귀) =====

        [TestCase(EMonster.Wisp,    0x22C55E, "Wisp 초록")]
        [TestCase(EMonster.Wraith,  0x6B7280, "Wraith 회색")]
        [TestCase(EMonster.Reaper,  0xEF4444, "Reaper 빨강")]
        [TestCase(EMonster.Hex,     0xEAB308, "Hex 노랑")]
        [TestCase(EMonster.Plague,  0xA855F7, "Plague 보라")]
        [TestCase(EMonster.Phantom, 0x1F2937, "Phantom 검정")]
        public void SpeciesColor_6종_컨셉_11점4_매핑(EMonster type, int hex, string label)
        {
            Color actual = SpawnerStatusCell.SpeciesColor(type);
            Color expected = HexToColor(hex);
            AssertColorEquals(expected, actual, label);
        }

        //# ===== 6 강화 카드 배경색 = 종 색 (기획서 §2.3.3) =====

        //# 강화 카드 배경색은 출력 종 색상과 동일해야 — 색칩과 같은 색이라 직관적으로 "어느 종 강화인지" 인지.
        [TestCase(ECardId.WispHpBoost,            EMonster.Wisp)]
        [TestCase(ECardId.WraithDamageBoost,      EMonster.Wraith)]
        [TestCase(ECardId.ReaperAtkSpeed,         EMonster.Reaper)]
        [TestCase(ECardId.HexRangeBoost,          EMonster.Hex)]
        [TestCase(ECardId.PhantomMoveSpeedBoost,  EMonster.Phantom)]
        [TestCase(ECardId.PlagueSlowBoost,        EMonster.Plague)]
        public void IconLetterFor_강화카드_배경색_종_색상과_일치(ECardId cardId, EMonster matchingSpecies)
        {
            Color iconBg     = SpawnerStatusCell.IconLetterFor(cardId).bgColor;
            Color speciesCol = SpawnerStatusCell.SpeciesColor(matchingSpecies);
            AssertColorEquals(speciesCol, iconBg,
                $"{cardId} 배경 = {matchingSpecies} 색칩");
        }

        //# ===== 비강화 카드 — fallback =====

        //# Frenzy 등 비강화 카드는 (' ', gray, white) fallback.
        [Test]
        public void IconLetterFor_비강화_카드는_fallback_color()
        {
            (char letter, Color bgColor, Color fgColor) info = SpawnerStatusCell.IconLetterFor(ECardId.Frenzy);
            Assert.AreEqual(' ', info.letter);
            Assert.AreEqual(Color.gray, info.bgColor, "비강화 fallback 배경 = gray");
            Assert.AreEqual(Color.white, info.fgColor, "비강화 fallback 글자 = white");
        }

        //# ===== SpeciesName fallback =====

        //# 정의 외 EMonster 값(캐스팅)은 "?" fallback.
        [Test]
        public void SpeciesName_정의외_값은_물음표_fallback()
        {
            //# 캐스팅으로 정의 외 값 주입 — switch default 분기.
            Assert.AreEqual("?", SpawnerStatusCell.SpeciesName((EMonster)999));
        }

        //# ===== 진행 바 임계값 상수 =====

        //# Cool/Warm 경계 0.70 — 기획서 §3.1 락. WarmThreshold 가 변경되면 시각 피드백 깨짐.
        [Test]
        public void WarmThreshold_0점70_고정()
        {
            Assert.AreEqual(0.7f, SpawnerStatusCell.WarmThreshold, 0.0001f,
                "Cool→Warm 임계값 0.70 — 기획서 §3.1");
        }
    }
}
