using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    //# 상점 「몬스터 강화」 승급 (monster-grades §5) — 종족마다 보유 최고 등급 +1. 비용은 Grade_Promotion(기존 재화만), 되돌리기·환급 없음.
    //# 저장은 호출자(상점 → VillageController) 책임. Unity 비의존.
    public static class GradePromotion
    {
        //# 다음 승급 등급 — 이미 데이터 최고 등급이면 0.
        public static int NextGrade(MetaProfile profile, FormationDatabase db, ERace race)
        {
            if (profile == null || db == null)
                return 0;
            int next = profile.GetRaceGrade(race) + 1;
            return next <= db.MaxGradeOf(race) ? next : 0;
        }

        //# 재화가 모두 충분한가 — 하나라도 모자라면 그 재화를 lacking 으로 돌려준다.
        public static bool CanAfford(MetaProfile profile, IReadOnlyList<GradeCost> costs, out ECurrency lacking)
        {
            lacking = ECurrency.Soul;
            foreach (GradeCost cost in costs)
            {
                if (profile.GetCurrency(cost.Currency) < cost.Amount)
                {
                    lacking = cost.Currency;
                    return false;
                }
            }
            return true;
        }

        public static bool CanPromote(MetaProfile profile, FormationDatabase db, ERace race)
        {
            int next = NextGrade(profile, db, race);
            return next > 0 && db.PromotionCostOf(next).Count > 0 && CanAfford(profile, db.PromotionCostOf(next), out ECurrency _);
        }

        //# 승급은 보유 등급만 올린다. 기본 편성은 조회 시 보유 최고 등급을 사용한다.
        public static bool TryPromote(MetaProfile profile, FormationDatabase db, ERace race)
        {
            if (CanPromote(profile, db, race) == false)
                return false;
            int next = NextGrade(profile, db, race);
            foreach (GradeCost cost in db.PromotionCostOf(next))
            {
                profile.TrySpendCurrency(cost.Currency, cost.Amount);
            }
            profile.SetRaceGrade(race, next);
            return true;
        }
    }
}
