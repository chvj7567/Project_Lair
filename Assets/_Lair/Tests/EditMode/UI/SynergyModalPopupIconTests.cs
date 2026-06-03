using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 2026-06-03 델타 — 헤더 행 아이콘. BuildRows(iconOf) 주입 시 헤더만 Icon 채움, 효과 null.
    //# 정상(헤더 Icon 채움 + 효과 null) + 엣지(iconOf 미지정 시 헤더 Icon null) 2케이스.
    public class SynergyModalPopupIconTests
    {
        private static Func<EBuildAxis, int> Only(EBuildAxis axis, int count)
        {
            return a => a == axis ? count : 0;
        }

        //# 축마다 다른 sentinel Sprite 를 반환하는 iconOf.
        private static Func<EBuildAxis, Sprite> IconMap(Dictionary<EBuildAxis, Sprite> map)
        {
            return a => map.GetValueOrDefault(a);
        }

        //# 정상 — iconOf 주입 시 헤더는 해당 축 Icon, 효과 행은 항상 null.
        [Test]
        public void iconOf_주입시_헤더만_Icon_채우고_효과는_null()
        {
            Sprite tankSprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.zero);
            Dictionary<EBuildAxis, Sprite> map = new Dictionary<EBuildAxis, Sprite>
            {
                { EBuildAxis.Tank, tankSprite },
            };

            //# Tank 7장 → 헤더1 + 효과3.
            List<SynergyModalCellData> rows =
                SynergyModalPopup.BuildRows(Only(EBuildAxis.Tank, 7), IconMap(map));

            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.AreSame(tankSprite, rows[0].Icon, "헤더 Icon 은 iconOf(axis) 결과");
            for (int i = 1; i < rows.Count; ++i)
            {
                Assert.AreEqual(SynergyModalCellData.Kind.Effect, rows[i].RowKind);
                Assert.IsNull(rows[i].Icon, $"효과 행 {i} 의 Icon 은 null 이어야 함 (기획서 §3.2)");
            }
        }

        //# 엣지 — iconOf 미지정(기존 1-arg 호출부 경로)이면 헤더 Icon 도 null (무회귀).
        [Test]
        public void iconOf_미지정시_헤더_Icon도_null()
        {
            List<SynergyModalCellData> rows =
                SynergyModalPopup.BuildRows(Only(EBuildAxis.Dps, 3));

            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[0].RowKind);
            Assert.IsNull(rows[0].Icon, "iconOf 미지정 시 헤더 Icon 은 null");
        }
    }
}
