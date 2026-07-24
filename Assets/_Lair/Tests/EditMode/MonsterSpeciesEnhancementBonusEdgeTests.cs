using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# MetaBattleBonus.From 종족 집계 경계·독립성·글로벌 혼재·하위호환 (monster-species-enhancement §2·§8).
    //# 기존 MonsterSpeciesEnhancementBonusTests(정상 케이스 3 + 곱연산 규약 1)와 비중복 — 실배수 1.18·복수종족·혼재·구세이브.
    public class MonsterSpeciesEnhancementBonusEdgeTests
    {
        private readonly List<MetaConfig> _configs = new List<MetaConfig>();

        private MetaConfig MakeConfig(params ShopItemDef[] items)
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.ShopItems = new List<ShopItemDef>(items);
            _configs.Add(cfg);
            return cfg;
        }

        [TearDown]
        public void 정리()
        {
            foreach (MetaConfig cfg in _configs)
            {
                if (cfg != null)
                    Object.DestroyImmediate(cfg);
            }
            _configs.Clear();
        }

        private static ShopItemDef Species(string id, EMonster species, int maxLevel = 5)
            => new ShopItemDef
            {
                Id = id,
                EffectKind = EShopEffectKind.MonsterSpecies,
                Species = species,
                PerLevelMul = 1.18f,
                MaxLevel = maxLevel,
            };

        private static ShopItemDef GlobalStat(string id, EMonsterStatKind stat, float perLevelMul)
            => new ShopItemDef
            {
                Id = id,
                EffectKind = EShopEffectKind.MonsterStat,
                StatKind = stat,
                PerLevelMul = perLevelMul,
                MaxLevel = 5,
            };

        //# §2.2 곡선 종점 — 실제 통일 배수 1.18 로 Lv5 = 1.18^5 ≈ 2.288.
        [Test]
        public void 실배수_1_18로_Lv5는_1_18의_5제곱이다()
        {
            MetaConfig cfg = MakeConfig(Species("Enhance_Wisp", EMonster.Wisp));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 5);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(Mathf.Pow(1.18f, 5), bonus.GetSpeciesMul(EMonster.Wisp), 1e-3f);
            Assert.AreEqual(2.288f, bonus.GetSpeciesMul(EMonster.Wisp), 0.01f);
        }

        //# 복수 종족 동시 등록 — 서로 독립적으로 집계, 미등록 종족은 1.
        [Test]
        public void 복수_종족은_서로_독립적으로_집계된다()
        {
            MetaConfig cfg = MakeConfig(
                Species("Enhance_Wisp", EMonster.Wisp),
                Species("Enhance_Reaper", EMonster.Reaper));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 2);
            profile.SetShopLevel("Enhance_Reaper", 3);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(Mathf.Pow(1.18f, 2), bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
            Assert.AreEqual(Mathf.Pow(1.18f, 3), bonus.GetSpeciesMul(EMonster.Reaper), 1e-4f);
            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wraith), 1e-4f);
            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Phantom), 1e-4f);
        }

        //# 글로벌 스탯강화와 종족강화가 같은 config 에 섞여도 서로 다른 표면에 집계 — 안 섞인다.
        [Test]
        public void 글로벌_스탯강화와_종족강화는_서로_섞이지_않는다()
        {
            MetaConfig cfg = MakeConfig(
                GlobalStat("MonsterHpUp", EMonsterStatKind.Hp, 1.1f),
                Species("Enhance_Wisp", EMonster.Wisp));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("MonsterHpUp", 2);
            profile.SetShopLevel("Enhance_Wisp", 2);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            //# 글로벌 Hp 는 종족 배수의 영향 없이 1.1^2, 종족 배수는 글로벌 영향 없이 1.18^2.
            Assert.AreEqual(Mathf.Pow(1.1f, 2), bonus.GetStatMul(EMonsterStatKind.Hp), 1e-4f);
            Assert.AreEqual(Mathf.Pow(1.18f, 2), bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
            //# 글로벌 Power 는 미등록 → 1. 종족은 Power 축을 GetStatMul 로 노출하지 않는다(폴딩은 ApplyMetaBonuses 소관).
            Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Power), 1e-4f);
        }

        //# Lv0 종족 항목(구매 안 함)은 배수 1 — From 의 level<=0 가드.
        [Test]
        public void Lv0_종족항목은_1배수로_스킵된다()
        {
            MetaConfig cfg = MakeConfig(Species("Enhance_Wisp", EMonster.Wisp));
            MetaBattleBonus bonus = MetaBattleBonus.From(new MetaProfile(), cfg);

            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        //# MaxLevel 0 항목은 클램프 후 level 0 → 스킵(비활성 스위치).
        [Test]
        public void MaxLevel_0_종족항목은_저장레벨과_무관하게_1배수다()
        {
            MetaConfig cfg = MakeConfig(Species("Enhance_Wisp", EMonster.Wisp, maxLevel: 0));
            MetaProfile profile = new MetaProfile();
            profile.SetShopLevel("Enhance_Wisp", 3);

            MetaBattleBonus bonus = MetaBattleBonus.From(profile, cfg);

            Assert.AreEqual(1f, bonus.GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        //# null 가드 — profile/cfg 중 하나라도 null 이면 전부 1배수(전투 정지 방지).
        [Test]
        public void null_profile나_config면_전부_1배수다()
        {
            MetaConfig cfg = MakeConfig(Species("Enhance_Wisp", EMonster.Wisp));

            Assert.AreEqual(1f, MetaBattleBonus.From(null, cfg).GetSpeciesMul(EMonster.Wisp), 1e-4f);
            Assert.AreEqual(1f, MetaBattleBonus.From(new MetaProfile(), null).GetSpeciesMul(EMonster.Wisp), 1e-4f);
        }

        //# 세이브 하위호환 — 종족 엔트리가 전혀 없는 구버전 프로필(글로벌 항목만 보유)은 6종 전부 Lv0 로딩 → 배수 1.
        [Test]
        public void 종족엔트리없는_구세이브는_여섯종_모두_1배수로_로딩된다()
        {
            MetaConfig cfg = MakeConfig(
                GlobalStat("MonsterHpUp", EMonsterStatKind.Hp, 1.1f),
                Species("Enhance_Wisp", EMonster.Wisp),
                Species("Enhance_Wraith", EMonster.Wraith),
                Species("Enhance_Reaper", EMonster.Reaper),
                Species("Enhance_Hex", EMonster.Hex),
                Species("Enhance_Plague", EMonster.Plague),
                Species("Enhance_Phantom", EMonster.Phantom));
            //# 구버전 프로필 — ShopLevels 에 글로벌 항목만 존재(종족 키 부재).
            MetaProfile oldProfile = new MetaProfile();
            oldProfile.SetShopLevel("MonsterHpUp", 3);

            MetaBattleBonus bonus = MetaBattleBonus.From(oldProfile, cfg);

            foreach (EMonster species in System.Enum.GetValues(typeof(EMonster)))
            {
                Assert.AreEqual(1f, bonus.GetSpeciesMul(species), 1e-4f, $"{species} 구세이브 기본 1배수");
            }
            //# 기존 글로벌 강화는 그대로 로딩된다(구세이브 회귀 없음).
            Assert.AreEqual(Mathf.Pow(1.1f, 3), bonus.GetStatMul(EMonsterStatKind.Hp), 1e-4f);
        }
    }
}
