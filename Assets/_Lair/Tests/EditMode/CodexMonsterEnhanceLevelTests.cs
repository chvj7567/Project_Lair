using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 도감 몬스터 셀 — 강화 레벨 데이터 주입(BuildMonsterCellData) + 레벨→시각 매핑 상수 검증.
    //# 4채널 시각 적용(CodexCell.Bind)의 실제 픽셀 표현은 프리팹 배선 후 육안(§7.2) 담당.
    public class CodexMonsterEnhanceLevelTests
    {
        private static MetaConfig Cfg()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.CodexLockedSlots = 0;
            return cfg;
        }

        [Test]
        public void 몬스터셀은_강화레벨을_ShopLevels에서_읽는다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Enhance_Wisp", 3);
            p.SeenMonsters.Add("Wisp");

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);
            CodexCellData wisp = list.Find(c => c.Species == EMonster.Wisp);

            Assert.IsNotNull(wisp);
            Assert.AreEqual(3, wisp.EnhanceLevel);
            Assert.AreEqual(EMonster.Wisp, wisp.Species);
        }

        [Test]
        public void 미강화_종족은_레벨0이다()
        {
            MetaProfile p = new MetaProfile();
            p.SeenMonsters.Add("Wraith");

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);
            CodexCellData wraith = list.Find(c => c.Species == EMonster.Wraith);

            Assert.IsNotNull(wraith);
            Assert.AreEqual(0, wraith.EnhanceLevel);
        }

        //# 엣지 — 미발견 종족을 상점에서 먼저 강화한 케이스(§6 엣지). 데이터 계약: 레벨은 읽되 Unlocked=false.
        //# 실루엣 우선(4채널 off) 시각 게이팅은 CodexCell.Bind 소관 — 본 테스트 범위 밖(육안·code-review).
        [Test]
        public void 미해금_종족도_레벨은_읽되_Unlocked_false다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("Enhance_Reaper", 4);
            //# SeenMonsters 에 Reaper 미포함 = 미해금.

            List<CodexCellData> list = CodexPopup.BuildMonsterCellData(p, Cfg(), _ => null);
            CodexCellData reaper = list.Find(c => c.Species == EMonster.Reaper);

            Assert.IsNotNull(reaper);
            Assert.IsFalse(reaper.Unlocked);
            Assert.AreEqual(4, reaper.EnhanceLevel);
        }

        //# 배열 핀 — 레벨→틴트/발광α/스케일 곡선이 기획서 §9 SoT 값과 정확히 일치(전사 오타 방어).
        //# 육안·컴파일러가 못 잡는 유일한 검증(main 눈은 Lv3 0.40 vs 0.42 구분 불가).
        [Test]
        public void 레벨_시각매핑_상수는_기획서_SoT와_일치한다()
        {
            CollectionAssert.AreEqual(
                new[] { 0.00f, 0.16f, 0.28f, 0.40f, 0.52f, 0.64f },
                EnhanceLevelVisual.IconTintByLevel);
            CollectionAssert.AreEqual(
                new[] { 0.00f, 0.25f, 0.42f, 0.58f, 0.74f, 0.90f },
                EnhanceLevelVisual.GlowOverlayAlphaByLevel);
            CollectionAssert.AreEqual(
                new[] { 1.00f, 1.015f, 1.03f, 1.05f, 1.07f, 1.10f },
                EnhanceLevelVisual.ScaleByLevel);
        }
    }
}
