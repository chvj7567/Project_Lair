using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 3축 합성 통합 — 글로벌 스탯강화 × 종족강화가 _typeModifiers(StatMultiplier) 표면에 곱으로 접히는 규약 검증
    //# (monster-species-enhancement §2·§8.1). BattleController.ApplyMetaBonuses 의 폴딩 2-루프를 실제 MetaBattleBonus.From
    //# 위에서 재현한다(live ApplyMetaBonuses 는 _metaConfig/_balance/_spawners 의존 PlayMode 경로 — 여기선 계약 수준 재현).
    //# 최종 = 기본(raw) × 글로벌 × 종족 이며, raw 배는 ApplyMonsterStats(raw×mul) 소관 — 본 테스트는 mul 표면만 검증.
    public class MonsterSpeciesEnhancement3AxisFoldTests
    {
        private MetaConfig _cfg;

        [TearDown]
        public void 정리()
        {
            if (_cfg != null)
                UnityEngine.Object.DestroyImmediate(_cfg);
            _cfg = null;
        }

        //# ApplyMetaBonuses 폴딩 규약 재현 — 글로벌 스탯(전종 동일 배수) + 종족(Hp·Power 동일 단일배수)을 _typeModifiers 에 곱연산 접기.
        private static Dictionary<EMonster, StatMultiplier> FoldLikeApplyMetaBonuses(MetaBattleBonus bonus)
        {
            Dictionary<EMonster, StatMultiplier> typeMods = new Dictionary<EMonster, StatMultiplier>();

            //# 1) 글로벌 스탯 — 모든 종에 같은 배수 적용(ApplyMetaBonuses 전반 루프).
            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                foreach (EMonsterStatKind kind in (EMonsterStatKind[])Enum.GetValues(typeof(EMonsterStatKind)))
                {
                    float mul = bonus.GetStatMul(kind);
                    if (Mathf.Approximately(mul, 1f))
                        continue;
                    Get(typeMods, type).Multiply(kind, mul);
                }
            }

            //# 2) 종족 강화 — Hp·Power 두 축에 단일 배수(ApplyMetaBonuses 후반 루프).
            foreach (EMonster type in (EMonster[])Enum.GetValues(typeof(EMonster)))
            {
                float speciesMul = bonus.GetSpeciesMul(type);
                if (Mathf.Approximately(speciesMul, 1f))
                    continue;
                Get(typeMods, type).Multiply(EMonsterStatKind.Hp, speciesMul);
                Get(typeMods, type).Multiply(EMonsterStatKind.Power, speciesMul);
            }
            return typeMods;
        }

        private static StatMultiplier Get(Dictionary<EMonster, StatMultiplier> map, EMonster type)
        {
            if (map.TryGetValue(type, out StatMultiplier m) == false)
            {
                m = new StatMultiplier();
                map[type] = m;
            }
            return m;
        }

        private static StatMultiplier Modifier(Dictionary<EMonster, StatMultiplier> map, EMonster type)
            => map.TryGetValue(type, out StatMultiplier m) ? m : StatMultiplier.Identity;

        //# 글로벌 Hp × 종족 : 대상 종족은 Hp 가 두 축의 곱, Power 는 종족 배수만.
        [Test]
        public void 글로벌Hp와_종족강화가_대상종족_Hp에서_곱으로_맞물린다()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f, MaxLevel = 5 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, PerLevelMul = 1.18f, MaxLevel = 5 },
            };
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("MonsterHpUp", 1);   //# 글로벌 Hp ×1.1
            profile.SetShopLevel("Enhance_Wisp", 2);  //# 종족 ×1.18^2

            Dictionary<EMonster, StatMultiplier> mods = FoldLikeApplyMetaBonuses(MetaBattleBonus.From(profile, _cfg));

            float species = Mathf.Pow(1.18f, 2);
            StatMultiplier wisp = Modifier(mods, EMonster.Wisp);
            Assert.AreEqual(1.1f * species, wisp.HpMul, 1e-4f);   //# 글로벌 × 종족
            Assert.AreEqual(species, wisp.PowerMul, 1e-4f);        //# 종족 단일배수만(글로벌 Power 미등록)
        }

        //# 비대상 종족은 글로벌 배수만 받고 종족 배수는 안 받는다(종족 강화의 국소성).
        [Test]
        public void 비대상_종족은_글로벌만_받고_종족배수는_안_받는다()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f, MaxLevel = 5 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, PerLevelMul = 1.18f, MaxLevel = 5 },
            };
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("MonsterHpUp", 1);
            profile.SetShopLevel("Enhance_Wisp", 2);

            Dictionary<EMonster, StatMultiplier> mods = FoldLikeApplyMetaBonuses(MetaBattleBonus.From(profile, _cfg));

            StatMultiplier reaper = Modifier(mods, EMonster.Reaper);
            Assert.AreEqual(1.1f, reaper.HpMul, 1e-4f);   //# 글로벌 Hp 만
            Assert.AreEqual(1f, reaper.PowerMul, 1e-4f);   //# 종족 미강화 → Power 항등
        }

        //# 종족 단일 배수는 Hp·Power 를 같은 값으로 함께 키운다(§2.1 단일 배수 모델).
        [Test]
        public void 종족강화는_Hp와_Power를_같은_배수로_함께_키운다()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems = new List<ShopItemDef>
            {
                new ShopItemDef { Id = "Enhance_Reaper", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Reaper, PerLevelMul = 1.18f, MaxLevel = 5 },
            };
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Reaper", 5);

            Dictionary<EMonster, StatMultiplier> mods = FoldLikeApplyMetaBonuses(MetaBattleBonus.From(profile, _cfg));

            StatMultiplier reaper = Modifier(mods, EMonster.Reaper);
            float species = Mathf.Pow(1.18f, 5);
            Assert.AreEqual(species, reaper.HpMul, 1e-3f);
            Assert.AreEqual(species, reaper.PowerMul, 1e-3f);
            Assert.AreEqual(reaper.HpMul, reaper.PowerMul, 1e-4f);   //# 두 축 동일
        }

        //# §8.1 파워 상한 — 만렙 글로벌(Hp 1.10)+종족(1.18^5) 3축 곱이 ×2.52 근사(회귀 상한선 박제).
        [Test]
        public void 만렙_3축_곱이_기획서_파워상한_2_52에_근사한다()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems = new List<ShopItemDef>
            {
                //# 글로벌 Hp 만렙 총배 ≈ 1.10 을 PerLevelMul 로 근사(1.10^1). §8.1 은 글로벌 만렙 HP ×1.10.
                new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.10f, MaxLevel = 1 },
                new ShopItemDef { Id = "Enhance_Wisp", EffectKind = EShopEffectKind.MonsterSpecies, Species = EMonster.Wisp, PerLevelMul = 1.18f, MaxLevel = 5 },
            };
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("MonsterHpUp", 1);
            profile.SetShopLevel("Enhance_Wisp", 5);

            Dictionary<EMonster, StatMultiplier> mods = FoldLikeApplyMetaBonuses(MetaBattleBonus.From(profile, _cfg));

            float hp = Modifier(mods, EMonster.Wisp).HpMul;
            Assert.AreEqual(2.52f, hp, 0.02f);   //# 1.10 × 2.288 = 2.517
        }
    }
}
