using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 레벨 → 전투 시작 배율 집계 (기획서 §3.2). mul = PerLevelMul^level 을 스탯별 곱연산 누적.
    public class MetaBattleBonus
    {
        private readonly Dictionary<EMonsterStatKind, float> _statMuls = new Dictionary<EMonsterStatKind, float>();

        public float SpawnerPeriodMul { get; private set; } = 1f;

        public float GetStatMul(EMonsterStatKind kind)
            => _statMuls.TryGetValue(kind, out float mul) ? mul : 1f;

        public static MetaBattleBonus From(MetaProfile profile, MetaConfig cfg)
        {
            MetaBattleBonus bonus = new MetaBattleBonus();
            if (profile == null || cfg == null)
                return bonus;

            foreach (ShopItemDef item in cfg.ShopItems)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                int level = profile.GetShopLevel(item.Id);
                if (level <= 0)
                    continue;

                float mul = Mathf.Pow(item.PerLevelMul, level);
                switch (item.EffectKind)
                {
                    case EShopEffectKind.MonsterStat:
                        bonus._statMuls[item.StatKind] = bonus.GetStatMul(item.StatKind) * mul;
                        break;
                    case EShopEffectKind.SpawnerPeriod:
                        bonus.SpawnerPeriodMul *= mul;
                        break;
                }
            }
            return bonus;
        }
    }
}
