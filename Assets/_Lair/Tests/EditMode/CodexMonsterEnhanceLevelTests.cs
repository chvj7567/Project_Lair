using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 도감 몬스터 셀 — 레벨→시각 매핑 상수 검증(몬스터 탭 데이터는 CodexViewModel 로 이관).
    //# 4채널 시각 적용(CodexCell.Bind)의 실제 픽셀 표현은 프리팹 배선 후 육안(§7.2) 담당.
    public class CodexMonsterEnhanceLevelTests
    {
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
