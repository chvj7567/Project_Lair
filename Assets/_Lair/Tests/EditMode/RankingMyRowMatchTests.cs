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

        private static bool IsMyRowByUid(RankingRowDto row, string myUid, long myAccountId, int myClearMs, bool alreadyFound)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("IsMyRowByUid",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "IsMyRowByUid 미발견 — RankingPopup 시그니처 변경 시 갱신");
            return (bool)m.Invoke(null, new object[] { row, myUid, myAccountId, myClearMs, alreadyFound });
        }

        private static RankingRowDto PickMyRowByUid(List<RankingRowDto> rows, string myUid, long myAccountId, int myClearMs)
        {
            MethodInfo m = typeof(RankingPopup).GetMethod("PickMyRowByUid",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(m, "PickMyRowByUid 미발견 — RankingPopup 시그니처 변경 시 갱신");
            return (RankingRowDto)m.Invoke(null, new object[] { rows, myUid, myAccountId, myClearMs });
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

        //# === uid 1순위 식별(2026-07-14 Firebase 피벗) — IsMyRowByUid ===

        //# 정상 — 양쪽 uid 존재 + 일치: 시간·accountId 무관하게 내 행.
        [Test]
        public void IsMyRowByUid_양쪽uid존재하고_일치하면_내행이다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1", accountId = 0, clearTimeMs = 999999 };
            Assert.IsTrue(IsMyRowByUid(row, "u1", 0, 123018, false));
        }

        //# 정상 — 양쪽 uid 존재 + 불일치: 시간이 같아도 시간 폴백으로 새지 않고 내 행이 아니다(uid 권위).
        [Test]
        public void IsMyRowByUid_양쪽uid존재하고_불일치면_시간같아도_내행이아니다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u2", accountId = 0, clearTimeMs = 123018 };
            Assert.IsFalse(IsMyRowByUid(row, "u1", 0, 123018, false));
        }

        //# 엣지 — myUid 만 있고 row.uid 없음: uid 게이트 미진입 → 기존 accountId/시간 폴백 위임.
        [Test]
        public void IsMyRowByUid_한쪽만uid존재하면_기존폴백으로_위임한다()
        {
            RankingRowDto row = new RankingRowDto { uid = null, accountId = 12, clearTimeMs = 50000 };
            Assert.IsTrue(IsMyRowByUid(row, "u1", 12, 123018, false));
        }

        //# 엣지 — 이미 찾았으면 uid 일치여도 false(중복 강조 방지).
        [Test]
        public void IsMyRowByUid_이미찾았으면_uid일치여도_false다()
        {
            RankingRowDto row = new RankingRowDto { uid = "u1" };
            Assert.IsFalse(IsMyRowByUid(row, "u1", 0, -1, true));
        }

        //# 엣지 — row null 가드.
        [Test]
        public void IsMyRowByUid_row가null이면_false다()
        {
            Assert.IsFalse(IsMyRowByUid(null, "u1", 0, -1, false));
        }

        //# === uid 1순위 선택 — PickMyRowByUid ===

        //# 정상 — uid 일치 행이 있으면 그 행 선택(accountId/시간 무관).
        [Test]
        public void PickMyRowByUid_uid일치행을_우선선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u2", accountId = 7, clearTimeMs = 123018 },
                new RankingRowDto { uid = "u1", accountId = 99, clearTimeMs = 999999 },
            };
            RankingRowDto picked = PickMyRowByUid(rows, "u1", 12, 123018);
            Assert.AreEqual("u1", picked.uid);
        }

        //# 엣지 — uid 일치 없으면 기존 PickMyRow(accountId→시간→첫행) 폴백.
        [Test]
        public void PickMyRowByUid_uid일치없으면_accountId폴백으로_선택한다()
        {
            List<RankingRowDto> rows = new List<RankingRowDto>
            {
                new RankingRowDto { uid = "u3", accountId = 7, clearTimeMs = 50000 },
                new RankingRowDto { uid = "u4", accountId = 12, clearTimeMs = 999999 },
            };
            RankingRowDto picked = PickMyRowByUid(rows, "u1", 12, 123018);
            Assert.AreEqual(12, picked.accountId);
        }
    }
}
