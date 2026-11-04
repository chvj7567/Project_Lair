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

        //# 승급 — 전부 확인한 뒤에만 차감해 일부만 결제되는 일이 없다. 성공 시 보유 등급 +1 + 그 종족 둥지에 새 등급 자동 배치(§5.3).
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
            AutoPlace(profile, db, race, db.HighestOwned(race, next));
            return true;
        }

        //# 저장 편성이 6행이면 그 종족 슬롯 행만 새 몬스터로 교체. 0행(기본 편성)은 보유 최고 등급을 따라가므로 그대로 둔다.
        private static void AutoPlace(MetaProfile profile, FormationDatabase db, ERace race, EMonster monster)
        {
            if (profile.Formation == null || profile.Formation.Count != FormationDatabase.SlotCount)
                return;
            int slot = db.SlotOf(race) + 1;
            foreach (FormationSlotEntry entry in profile.Formation)
            {
                if (entry != null && entry.Slot == slot)
                {
                    entry.Monster = monster.ToString();
                }
            }
        }
    }
}
