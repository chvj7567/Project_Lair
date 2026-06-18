using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Net;
using Lair.UI;

namespace Lair.Tests.EditMode
{
    //# RankingPopup.IsMyRow/PickMyRow(private static) 의 "내 행" 매칭 분기 박제.
    //# 핵심: accountId 1차, accountId 0/미식별이면 clearTimeMs 시간 폴백(구서버·익명 안전).
    //# 리플렉션 호출 — RankingTimeFormatTests 와 동일 패턴(static 순수 함수, 인스턴스 불필요).
    public class RankingMyRowMatchTests
    {
        private static bool IsMyRow(RankingRowDto row, long myAccountId, int myClearMs, bool alreadyFound)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("IsMyRow",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "IsMyRow 미발견 — RankingPopup 시그니처 변경 시 갱신");
            return (bool)m.Invoke(null, new object[] { row, myAccountId, myClearMs, alreadyFound });
        }

        private static RankingRowDto PickMyRow(List<RankingRowDto> rows, long myAccountId, int myClearMs)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("PickMyRow",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "PickMyRow 미발견 — RankingPopup 시그니처 변경 시 갱신");
            return (RankingRowDto)m.Invoke(null, new object[] { rows, myAccountId, myClearMs });
        }

        //# 정상 — 신서버+로그인: accountId 일치 행이 내 행이다(시간 무관).
        [Test]
        public void IsMyRow_accountId일치하면_시간무관하게_내행이다()
        {
            RankingRowDto row = new RankingRowDto { accountId = 12, clearTimeMs = 999999 };
            Assert.IsTrue(IsMyRow(row, 12, 123018, false));
        }

        //# 정상 — accountId 불일치면 시간이 같아도 내 행이 아니다(권위 키 우선).
        [Test]
        public void IsMyRow_accountId불일치면_시간이같아도_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { accountId = 7, clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, 12, 123018, false));
        }

        //# 엣지 — 신클라+구서버(row.accountId=0): accountId 게이트 미진입 → 시간 폴백으로 내 행 판정.
        [Test]
        public void IsMyRow_row의accountId가0이면_시간폴백으로_매칭한다()
        {
            RankingRowDto row = new RankingRowDto { accountId = 0, clearTimeMs = 123018 };
            Assert.IsTrue(IsMyRow(row, 12, 123018, false));
        }

        //# 엣지 — 익명(myAccountId=0)+구서버행(accountId=0): 0==0 오매칭 방지, 시간만으로 판정.
        [Test]
        public void IsMyRow_익명이고_시간불일치면_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { accountId = 0, clearTimeMs = 50000 };
            Assert.IsFalse(IsMyRow(row, 0, 123018, false));
        }

        //# 엣지 — 이미 찾았으면 중복 강조 방지.
        [Test]
        public void IsMyRow_이미찾았으면_false다()
        {
            RankingRowDto row = new RankingRowDto { accountId = 12, clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRow(row, 12, 123018, true));
        }

        //# 정상 — /me 응답: accountId 일치 행을 우선 선택.
        [Test]
        public void PickMyRow_accountId일치행을_우선선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { accountId = 7, clearTimeMs = 123018 },
                new RankingRowDto { accountId = 12, clearTimeMs = 999999 },
            };
            RankingRowDto picked = PickMyRow(rows, 12, 123018);
            Assert.AreEqual(12, picked.accountId);
        }

        //# 엣지 — 구서버(accountId 모두 0): 시간 폴백으로 일치 행 선택.
        [Test]
        public void PickMyRow_accountId없으면_시간폴백으로_선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { accountId = 0, clearTimeMs = 50000 },
                new RankingRowDto { accountId = 0, clearTimeMs = 123018 },
            };
            RankingRowDto picked = PickMyRow(rows, 12, 123018);
            Assert.AreEqual(123018, picked.clearTimeMs);
        }
    }
}
