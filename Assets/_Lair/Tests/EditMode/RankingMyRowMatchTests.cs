using System.Collections.Generic;
using System.Reflection;
using Lair.Net;
using Lair.UI;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# "내 행" 식별 — uid 1차, uid 미식별이면 clearTimeMs 시간 폴백. (accountId 경로는 2026-07-28 제거)
    public class RankingMyRowMatchTests
    {
        private static bool IsMyRow(RankingRowDto row, string myUid, int myClearMs, bool alreadyFound)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("IsMyRow", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "RankingPopup.IsMyRow(4-arg) 를 찾을 수 없다");
            return (bool)m.Invoke(null, new object[] { row, myUid, myClearMs, alreadyFound });
        }

        private static RankingRowDto PickMyRow(List<RankingRowDto> rows, string myUid, int myClearMs)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("PickMyRow", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "RankingPopup.PickMyRow(3-arg) 를 찾을 수 없다");
            return (RankingRowDto)m.Invoke(null, new object[] { rows, myUid, myClearMs });
        }

        //# 축1 — uid 일치: 시간 무관하게 내 행.
        [Test]
        public void IsMyRow_uid일치하면_시간무관하게_내행이다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1", clearTimeMs = 999999 };
            Assert.IsTrue(IsMyRow(row, "u1", 123018, false));
        }

        //# 축1 — uid 불일치: 시간이 같아도 내 행이 아니다(권위 키 우선).
        [Test]
        public void IsMyRow_uid불일치면_시간이같아도_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u2", clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, "u1", 123018, false));
        }

        //# 축2 — row.uid 없음(구데이터): uid 게이트 미진입 → 시간 폴백으로 매칭.
        [Test]
        public void IsMyRow_row의uid가없으면_시간폴백으로_매칭한다()
        {
            RankingRowDto row = new RankingRowDto { uid = null, clearTimeMs = 123018 };
            Assert.IsTrue(IsMyRow(row, "u1", 123018, false));
        }

        //# 축2 — myUid 없음(미인증): 시간 폴백.
        [Test]
        public void IsMyRow_myUid없으면_시간폴백으로_매칭한다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u9", clearTimeMs = 50000 };
            Assert.IsTrue(IsMyRow(row, null, 50000, false));
            Assert.IsFalse(IsMyRow(row, null, 50001, false));
        }

        //# 축2 — 시간 폴백인데 내 기록이 없음(-1): 매칭하지 않는다.
        [Test]
        public void IsMyRow_내기록이없으면_시간폴백도_매칭하지않는다()
        {
            RankingRowDto row = new RankingRowDto { uid = null, clearTimeMs = 50000 };
            Assert.IsFalse(IsMyRow(row, null, -1, false));
        }

        //# 축3 — 이미 찾았으면 이후 행은 무조건 false(중복 강조 방지).
        [Test]
        public void IsMyRow_이미찾았으면_더는_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1", clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, "u1", 123018, true));
        }

        //# 축1 — Pick: uid 일치 행을 우선 선택.
        [Test]
        public void PickMyRow_uid일치행을_우선선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u2", clearTimeMs = 123018 },
                new RankingRowDto { uid = "u1", clearTimeMs = 999999 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 123018);
            Assert.AreEqual("u1", picked.uid);
        }

        //# 축2 — Pick: uid 일치가 없으면 시간 일치 행.
        [Test]
        public void PickMyRow_uid일치없으면_시간폴백으로_선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u3", clearTimeMs = 50000 },
                new RankingRowDto { uid = "u4", clearTimeMs = 123018 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 123018);
            Assert.AreEqual(123018, picked.clearTimeMs);
        }

        //# 축4 — Pick: uid·시간 모두 못 찾으면 첫 행.
        [Test]
        public void PickMyRow_아무것도_못찾으면_첫행을_반환한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u3", clearTimeMs = 50000 },
                new RankingRowDto { uid = "u4", clearTimeMs = 60000 },
            };
            RankingRowDto picked = PickMyRow(rows, "u1", 999999);
            Assert.AreEqual("u3", picked.uid);
        }

        //# 엣지 — 빈 목록이면 null.
        [Test]
        public void PickMyRow_빈목록이면_null이다()
        {
            Assert.IsNull(PickMyRow(new List<RankingRowDto>(), "u1", 123018));
        }
    }
}
