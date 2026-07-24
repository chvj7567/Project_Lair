using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Meta
{
    //# 상점 레벨 → 전투 시작 배율 집계 (기획서 §3.2). mul = PerLevelMul^level 을 스탯별 곱연산 누적.
    public class MetaBattleBonus
    {
        private readonly Dictionary<EMonsterStatKind, float> _statMuls = new Dictionary<EMonsterStatKind, float>();
        //# 종족 개별 강화 배수 — HP·공격력 공용 단일 배수 (monster-species-enhancement §2.1).
        private readonly Dictionary<EMonster, float> _speciesMuls = new Dictionary<EMonster, float>();

        public float SpawnerPeriodMul { get; private set; } = 1f;

        public float GetStatMul(EMonsterStatKind kind)
            => _statMuls.TryGetValue(kind, out float mul) ? mul : 1f;

        //# 미등록/Lv0 종족은 1.0 배수.
        public float GetSpeciesMul(EMonster species)
            => _speciesMuls.TryGetValue(species, out float mul) ? mul : 1f;

        public static MetaBattleBonus From(MetaProfile profile, MetaConfig cfg)
        {
            MetaBattleBonus bonus = new MetaBattleBonus();
            if (profile == null || cfg == null)
                return bonus;

            foreach (ShopItemDef item in cfg.ShopItems)
            {
                if (item == null || string.IsNullOrEmpty(item.Id))
                    continue;
                //# 세이브 변조 방어 — 저장 레벨을 MaxLevel 로 클램프 (정상 경로는 ShopService 가 만렙 구매 차단).
                //# MaxLevel 0 이면 level 0 으로 클램프 → 아래 가드에서 skip — 항목 비활성 스위치로 동작.
                int level = Mathf.Min(profile.GetShopLevel(item.Id), item.MaxLevel);
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
                    case EShopEffectKind.MonsterSpecies:
                        bonus._speciesMuls[item.Species] = bonus.GetSpeciesMul(item.Species) * mul;
                        break;
                }
            }
            return bonus;
        }
    }
}
