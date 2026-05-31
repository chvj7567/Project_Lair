using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 G (BuildModalPopup 카테고리 정렬 회귀, 기획서 §2.7.4).
    //#
    //# private static CategoryOrder(EBuildAxis) → int 매핑 검증.
    //# 패시브 섹션 위→아래: Tank(0) → Dps(1) → Debuff(2) → Swarm(3).
    //# 정렬 알고리즘 자체보다 매핑 값의 안정성 회귀에 초점.
    //# 카드 리뉴얼 v0.6 — 구 카드 카테고리(4종) → EBuildAxis (Phase 1 자리 치환: Enhance/Spawn/Replace/Environment = Tank/Dps/Debuff/Swarm).
    public class BuildModalPopupTests
    {
        //# private static int CategoryOrder(EBuildAxis) 리플렉션 호출.
        private static int CallCategoryOrder(EBuildAxis axis)
        {
            MethodInfo mi = typeof(BuildModalPopup).GetMethod("CategoryOrder",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "BuildModalPopup.CategoryOrder 메서드 존재");
            return (int)mi.Invoke(null, new object[] { axis });
        }

        //# ===== 카테고리 순서 (기획서 §2.7.4) =====

        [Test]
        public void CategoryOrder_Tank_0()
        {
            Assert.AreEqual(0, CallCategoryOrder(EBuildAxis.Tank),
                "Tank 가 가장 위 (강화/탱커 빌드가 먼저 보이는 영역)");
        }

        [Test]
        public void CategoryOrder_Dps_1()
        {
            Assert.AreEqual(1, CallCategoryOrder(EBuildAxis.Dps));
        }

        [Test]
        public void CategoryOrder_Debuff_2()
        {
            Assert.AreEqual(2, CallCategoryOrder(EBuildAxis.Debuff));
        }

        [Test]
        public void CategoryOrder_Swarm_3()
        {
            Assert.AreEqual(3, CallCategoryOrder(EBuildAxis.Swarm),
                "Swarm 이 가장 아래");
        }

        //# 회귀 — 카테고리 그룹 정렬: Sort 함수에 넣으면 기획서 순서대로 나와야.
        [Test]
        public void CategoryOrder_정렬_시_Tank_Dps_Debuff_Swarm_순()
        {
            //# 의도적 무작위 순서 입력.
            List<EBuildAxis> input = new List<EBuildAxis>
            {
                EBuildAxis.Swarm,
                EBuildAxis.Dps,
                EBuildAxis.Tank,
                EBuildAxis.Debuff,
                EBuildAxis.Swarm,
                EBuildAxis.Tank,
            };

            input.Sort((a, b) => CallCategoryOrder(a).CompareTo(CallCategoryOrder(b)));

            //# 기대: Tank × 2, Dps × 1, Debuff × 1, Swarm × 2.
            Assert.AreEqual(EBuildAxis.Tank,   input[0]);
            Assert.AreEqual(EBuildAxis.Tank,   input[1]);
            Assert.AreEqual(EBuildAxis.Dps,    input[2]);
            Assert.AreEqual(EBuildAxis.Debuff, input[3]);
            Assert.AreEqual(EBuildAxis.Swarm,  input[4]);
            Assert.AreEqual(EBuildAxis.Swarm,  input[5]);
        }

        //# 엣지 — 정의 외 카테고리 캐스팅 시 99 fallback (정렬 끝으로 밀림).
        [Test]
        public void CategoryOrder_정의외_값은_99_fallback()
        {
            int order = CallCategoryOrder((EBuildAxis)999);
            Assert.AreEqual(99, order, "정의 외 카테고리는 99 → 정렬 끝으로 밀림");
        }
    }
}
