using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Lair.Data;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 G (BuildModalPopup 카테고리 정렬 회귀, 기획서 §2.7.4).
    //#
    //# private static CategoryOrder(ECardCategory) → int 매핑 검증.
    //# 패시브 섹션 위→아래: Enhance(0) → Spawn(1) → Replace(2) → Environment(3).
    //# 정렬 알고리즘 자체보다 매핑 값의 안정성 회귀에 초점.
    public class BuildModalPopupTests
    {
        //# private static int CategoryOrder(ECardCategory) 리플렉션 호출.
        private static int CallCategoryOrder(ECardCategory category)
        {
            var mi = typeof(BuildModalPopup).GetMethod("CategoryOrder",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.IsNotNull(mi, "BuildModalPopup.CategoryOrder 메서드 존재");
            return (int)mi.Invoke(null, new object[] { category });
        }

        //# ===== 카테고리 순서 (기획서 §2.7.4) =====

        [Test]
        public void CategoryOrder_Enhance_0()
        {
            Assert.AreEqual(0, CallCategoryOrder(ECardCategory.Enhance),
                "Enhance 가 가장 위 (강화는 빌드 형상에서 먼저 보이는 영역)");
        }

        [Test]
        public void CategoryOrder_Spawn_1()
        {
            Assert.AreEqual(1, CallCategoryOrder(ECardCategory.Spawn));
        }

        [Test]
        public void CategoryOrder_Replace_2()
        {
            Assert.AreEqual(2, CallCategoryOrder(ECardCategory.Replace));
        }

        [Test]
        public void CategoryOrder_Environment_3()
        {
            Assert.AreEqual(3, CallCategoryOrder(ECardCategory.Environment),
                "Environment 가 가장 아래");
        }

        //# 회귀 — 카테고리 그룹 정렬: Sort 함수에 넣으면 기획서 순서대로 나와야.
        [Test]
        public void CategoryOrder_정렬_시_Enhance_Spawn_Replace_Environment_순()
        {
            //# 의도적 무작위 순서 입력.
            var input = new List<ECardCategory>
            {
                ECardCategory.Environment,
                ECardCategory.Spawn,
                ECardCategory.Enhance,
                ECardCategory.Replace,
                ECardCategory.Environment,
                ECardCategory.Enhance,
            };

            input.Sort((a, b) => CallCategoryOrder(a).CompareTo(CallCategoryOrder(b)));

            //# 기대: Enhance × 2, Spawn × 1, Replace × 1, Environment × 2.
            Assert.AreEqual(ECardCategory.Enhance,     input[0]);
            Assert.AreEqual(ECardCategory.Enhance,     input[1]);
            Assert.AreEqual(ECardCategory.Spawn,       input[2]);
            Assert.AreEqual(ECardCategory.Replace,     input[3]);
            Assert.AreEqual(ECardCategory.Environment, input[4]);
            Assert.AreEqual(ECardCategory.Environment, input[5]);
        }

        //# 엣지 — 정의 외 카테고리 캐스팅 시 99 fallback (정렬 끝으로 밀림).
        [Test]
        public void CategoryOrder_정의외_값은_99_fallback()
        {
            int order = CallCategoryOrder((ECardCategory)999);
            Assert.AreEqual(99, order, "정의 외 카테고리는 99 → 정렬 끝으로 밀림");
        }
    }
}
