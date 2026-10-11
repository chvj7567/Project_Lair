using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.UI
{
    //# 시너지 모달 비주얼 폴리시 — ActiveTier 대입과 셀 Bind 의 null 가드 최소 검증.
    public class SynergyModalCellActiveTierTests
    {
        [Test]
        public void 헤더_ActiveTier는_임계_도달_개수_효과행은_0()
        {
            Func<EBuildAxis, int> counts = a => a == EBuildAxis.Tank ? 5 : (a == EBuildAxis.Dps ? 7 : 3);
            List<SynergyModalCellData> rows = SynergyModalTestFakes.BuildRows(counts);
            Assert.AreEqual(2, rows[0].ActiveTier);
            Assert.AreEqual(0, rows[1].ActiveTier);
            Assert.AreEqual(SynergyModalCellData.Kind.Header, rows[3].RowKind);
            Assert.AreEqual(3, rows[3].ActiveTier);
        }

        [Test]
        public void 신규_필드를_비운_셀도_Bind에서_예외가_없다()
        {
            GameObject go = new GameObject("cell");
            try
            {
                SynergyModalCell cell = go.AddComponent<SynergyModalCell>();
                Assert.DoesNotThrow(() => cell.Bind(new SynergyModalCellData
                {
                    RowKind = SynergyModalCellData.Kind.Header,
                    AxisColor = Color.red,
                    ActiveTier = 2,
                    IsMaxTier = true,
                }));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }
    }
}
