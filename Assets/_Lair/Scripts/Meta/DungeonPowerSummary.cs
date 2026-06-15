using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 누적 효과 한 줄 — 라벨 + 강화 퍼센트 (양수 = 강해짐). 기획서 §2.1.
    public struct DungeonPowerLine
    {
        public string Label;
        public int Percent;
    }

    //# 상점 레벨 → "현재 던전 강화" 표시 라인 (기획서 §2.1). MetaBattleBonus 집계 배율 재사용 — 전투 적용과 단일 출처.
    //# 라벨은 동적 표시 문구 → 코드 리터럴 (마을+메타 기획서 §7 ②표 규칙). 문구 변경 시 기획서가 SoT.
    public static class DungeonPowerSummary
    {
        public static List<DungeonPowerLine> Build(MetaProfile profile, MetaConfig cfg)
        {
            List<DungeonPowerLine> lines = new List<DungeonPowerLine>();
            if (profile == null || cfg == null)
                return lines;

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);
            foreach (ShopItemDef item in cfg.ShopItems)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                if (profile.GetShopLevel(item.Id) <= 0)
                    continue;

                float mul;
                bool inverse;
                if (item.EffectKind == EShopEffectKind.SpawnerPeriod)
                {
                    mul = bonus.SpawnerPeriodMul;
                    inverse = true;                       //# 주기 단축 → 스폰률 상승
                }
                else
                {
                    mul = bonus.GetStatMul(item.StatKind);
                    inverse = item.StatKind == EMonsterStatKind.Cooldown
                           || item.StatKind == EMonsterStatKind.SlowFactor;
                }

                float ratio = inverse ? (1f / mul - 1f) : (mul - 1f);
                int percent = Mathf.RoundToInt(ratio * 100f);
                //# 반올림 0% 항목 제외 — 강화 체감 0 (기획서 §2.4 방어적 가드, 현 7품목 수치에선 미발화).
                if (percent == 0)
                    continue;

                lines.Add(new DungeonPowerLine { Label = LabelOf(item), Percent = percent });
            }
            return lines;
        }

        //# 라벨 7종 확정값 (기획서 §2.1 표시 SoT). 부호는 항상 "강해짐 = 양수".
        private static string LabelOf(ShopItemDef item)
        {
            if (item.EffectKind == EShopEffectKind.SpawnerPeriod)
                return "스폰률";
            switch (item.StatKind)
            {
                case EMonsterStatKind.Hp:        return "HP";
                case EMonsterStatKind.Power:     return "공격";
                case EMonsterStatKind.Cooldown:  return "공속";
                case EMonsterStatKind.Range:     return "사거리";
                case EMonsterStatKind.MoveSpeed: return "이동";
                case EMonsterStatKind.SlowFactor: return "둔화";
                default:                         return "?";
            }
        }
    }
}
