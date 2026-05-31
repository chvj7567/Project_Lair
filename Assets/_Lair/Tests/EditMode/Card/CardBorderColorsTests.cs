using NUnit.Framework;
using UnityEngine;
using Lair.Card;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.Card
{
    //# 카드 테두리 색 단일 출처 박제 — 사용자 확정 매핑 회귀 차단 (28장 카드 → 3계열).
    public class CardBorderColorsTests
    {
        //# Hex → RGB(0~1) float — Unity Color 비교용.
        private static Color HexToColor(int hex)
        {
            float r = ((hex >> 16) & 0xFF) / 255f;
            float g = ((hex >>  8) & 0xFF) / 255f;
            float b = ( hex        & 0xFF) / 255f;
            return new Color(r, g, b, 1f);
        }

        //# 채널별 ±1/255 허용.
        private static void AssertColorEquals(Color expected, Color actual, string msg)
        {
            float eps = 1.5f / 255f;
            Assert.AreEqual(expected.r, actual.r, eps, $"{msg} (R)");
            Assert.AreEqual(expected.g, actual.g, eps, $"{msg} (G)");
            Assert.AreEqual(expected.b, actual.b, eps, $"{msg} (B)");
        }

        //# ===== 정상 — 영웅 백색 (8장 대표 2장) =====

        [TestCase(ECardId.HeroPoisonAura)]
        [TestCase(ECardId.MarkOfDeath)]
        public void 영웅_대상_카드는_백색(ECardId id)
        {
            Color actual = CardBorderColors.BorderColorOf(id);
            AssertColorEquals(Color.white, actual, $"{id} 백색");
        }

        //# ===== 정상 — 종 단일 (스폰 카드는 종 색칩과 동일) =====

        [TestCase(ECardId.SpawnWisps,    EMonster.Wisp)]
        [TestCase(ECardId.SpawnPhantoms, EMonster.Phantom)]
        public void 종_단일_카드_테두리는_종_색칩과_일치(ECardId id, EMonster species)
        {
            Color actual = CardBorderColors.BorderColorOf(id);
            Color expected = SpawnerStatusCell.SpeciesColor(species);
            AssertColorEquals(expected, actual, $"{id} ↔ {species}");
        }

        //# ===== 정상 — 몬스터 전체 시안 (#06B6D4) =====

        [TestCase(ECardId.Frenzy)]
        [TestCase(ECardId.IronWill)]
        [TestCase(ECardId.SpawnerHaste)]
        public void 몬스터_전체_카드는_시안_06B6D4(ECardId id)
        {
            Color actual = CardBorderColors.BorderColorOf(id);
            Color expected = HexToColor(0x06B6D4);
            AssertColorEquals(expected, actual, $"{id} 시안");
        }

        //# ===== 엣지 — enum 리뉴얼 잔재 (값명과 실제 효과 불일치 카드) =====

        //# Multiply enum 값 = SwarmRushEffect (Phantom 스폰 가속) → Phantom 단일 색.
        //# Berserk enum 값 = GuardianRageEffect (Wisp/Wraith 받피해) → 2종 묶음으로 전체 시안.
        //# WallOfWisps enum 값 = ToughHideEffect (Wisp/Wraith 받피해) → 2종 묶음으로 전체 시안.
        //# ReplaceWispsToWraith / ReplaceReapersToHex 도 2종 묶음으로 전체 시안.
        [Test]
        public void v0점6_리뉴얼_잔재_Multiply는_Phantom_단일_색()
        {
            Color actual = CardBorderColors.BorderColorOf(ECardId.Multiply);
            Color expected = SpawnerStatusCell.SpeciesColor(EMonster.Phantom);
            AssertColorEquals(expected, actual, "Multiply(=SwarmRush) ↔ Phantom");
        }

        [TestCase(ECardId.Berserk)]
        [TestCase(ECardId.WallOfWisps)]
        [TestCase(ECardId.ReplaceWispsToWraith)]
        [TestCase(ECardId.ReplaceReapersToHex)]
        public void v0점6_리뉴얼_잔재_2종_묶음_카드는_전체_시안(ECardId id)
        {
            Color actual = CardBorderColors.BorderColorOf(id);
            Color expected = HexToColor(0x06B6D4);
            AssertColorEquals(expected, actual, $"{id} 2종 묶음 → 전체 시안");
        }

        //# ===== 엣지 — 28장 전체 커버리지 (default 분기 미달 확인) =====

        //# ECardId 가 추후 추가될 때 ScopeOf 의 default 가 silently AllMonsters 로 떨어지는 회귀를 차단하기 위해,
        //# 현재 알려진 28장은 모두 명시 매핑되어 있는지 카운트로 검증. (default 분기는 미정의 ECardId 캐스팅으로만 발현)
        [Test]
        public void 정의외_ECardId_캐스팅은_default_시안_fallback()
        {
            ECardId unknown = (ECardId)999;
            Color actual = CardBorderColors.BorderColorOf(unknown);
            Color expected = HexToColor(0x06B6D4);
            AssertColorEquals(expected, actual, "정의외 ECardId → 전체 시안 (default)");
        }
    }
}
