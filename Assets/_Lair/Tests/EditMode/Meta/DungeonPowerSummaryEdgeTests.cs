using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# DungeonPowerSummary.Build — 7품목 풀셋·반올림 경계·혼합 레벨·방어 (기획서 §2.1/§2.4).
    //# TDD 본 테스트(DungeonPowerSummaryTests, 3종)와 중복 금지 — 만렙 7품목 풀셋·둔화·반올림 가드만 보강.
    public class DungeonPowerSummaryEdgeTests
    {
        private MetaConfig _cfg;

        //# 기획서 §2.1 마스터 표 7품목 — ShopItems 순서 그대로 (HP·공격·공속·이동·사거리·둔화·스폰률).
        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp",        EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.Hp,        PerLevelMul = 1.02f,  MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterPowerUp",     EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.Power,     PerLevelMul = 1.015f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterAtkSpeedUp",  EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.Cooldown,  PerLevelMul = 0.99f,  MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterMoveSpeedUp", EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.MoveSpeed, PerLevelMul = 1.015f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterRangeUp",     EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.Range,     PerLevelMul = 1.015f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "PlagueVenomUp",      EffectKind = EShopEffectKind.MonsterStat,   StatKind = EMonsterStatKind.SlowFactor, PerLevelMul = 0.98f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnerHasteUp",     EffectKind = EShopEffectKind.SpawnerPeriod, PerLevelMul = 0.985f, MaxLevel = 5 });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        private MetaProfile 만렙프로필()
        {
            MetaProfile p = new MetaProfile();
            foreach (ShopItemDef item in _cfg.ShopItems)
                p.SetShopLevel(item.Id, item.MaxLevel);
            return p;
        }

        //# 기획서 §2.1 — 7품목 만렙 % 검산표 (HP+10·공격+8·공속+5·이동+8·사거리+8·둔화+11·스폰률+8) 전건 일치.
        [Test]
        public void 전_7품목_만렙은_검산표_라벨과_퍼센트_순서대로_나온다()
        {
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(만렙프로필(), _cfg);

            Assert.AreEqual(7, lines.Count);
            Assert.AreEqual("HP",   lines[0].Label); Assert.AreEqual(10, lines[0].Percent);
            Assert.AreEqual("공격", lines[1].Label); Assert.AreEqual(8,  lines[1].Percent);
            Assert.AreEqual("공속", lines[2].Label); Assert.AreEqual(5,  lines[2].Percent);
            Assert.AreEqual("이동", lines[3].Label); Assert.AreEqual(8,  lines[3].Percent);
            Assert.AreEqual("사거리", lines[4].Label); Assert.AreEqual(8, lines[4].Percent);
            Assert.AreEqual("둔화", lines[5].Label); Assert.AreEqual(11, lines[5].Percent);
            Assert.AreEqual("스폰률", lines[6].Label); Assert.AreEqual(8, lines[6].Percent);
        }

        //# 기획서 §2.1 — 둔화(SlowFactor)는 감소형 mul<1 → (1/0.98^5-1)=+11%. 만렙 7종 중 최대 강화폭이라 단독 박제.
        [Test]
        public void 둔화는_감소형이라_역수로_플러스_11퍼센트로_환산된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("PlagueVenomUp", 5);
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("둔화", lines[0].Label);
            Assert.AreEqual(11, lines[0].Percent);
        }

        //# 기획서 §2.4 검산 — 공속 Lv1 = 1/0.99-1 = +1.01% → 반올림 +1% (최소 강화도 ≥1% 노출, 체감 끊김 없음).
        [Test]
        public void 공속_레벨1_최소강화도_반올림_1퍼센트로_노출된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterAtkSpeedUp", 1);
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("공속", lines[0].Label);
            Assert.AreEqual(1, lines[0].Percent);
        }

        //# 기획서 §2.4 — 반올림 0% 항목 제외 가드. 현 7품목엔 미발화라 미세 mul(1.001 Lv1 = +0.1%)로 가드 분기 직접 발화.
        [Test]
        public void 반올림_0퍼센트_항목은_요약줄에서_제외된다()
        {
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MicroHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.001f, MaxLevel = 5 });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MicroHpUp", 1);   //# 1.001^1-1 = 0.1% → 반올림 0% → 제외
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(0, lines.Count, "반올림 0% 는 강화 체감 0 → 노출 제외 (기획서 §2.4)");
        }

        //# 혼합 레벨 — 일부 만렙·일부 중간·일부 레벨0 → 표시 항목/순서/%가 정확히 가려진다.
        [Test]
        public void 혼합_레벨은_레벨0을_빼고_ShopItems_순서로_각각_환산된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 5);            //# 만렙 → +10%
            //# MonsterPowerUp 레벨0 → 제외
            p.SetShopLevel("MonsterAtkSpeedUp", 3);      //# 1/0.99^3-1 = 3.06% → +3%
            //# MoveSpeed/Range/SlowFactor 레벨0 → 제외
            p.SetShopLevel("SpawnerHasteUp", 5);         //# 만렙 → +8%

            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);

            Assert.AreEqual(3, lines.Count);
            Assert.AreEqual("HP",   lines[0].Label); Assert.AreEqual(10, lines[0].Percent);
            Assert.AreEqual("공속", lines[1].Label); Assert.AreEqual(3,  lines[1].Percent);
            Assert.AreEqual("스폰률", lines[2].Label); Assert.AreEqual(8, lines[2].Percent);
        }

        //# 세이브 변조 방어 — MaxLevel 초과 저장값은 MetaBattleBonus 가 MaxLevel 로 클램프 → 만렙 % 와 동일.
        [Test]
        public void MaxLevel_초과_저장값은_만렙으로_클램프되어_환산된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 99);   //# 변조값 — MaxLevel 5 로 클램프 → +10%
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count);
            Assert.AreEqual("HP", lines[0].Label);
            Assert.AreEqual(10, lines[0].Percent);
        }

        //# 방어 — null profile / null cfg 는 예외 없이 빈 목록.
        [Test]
        public void null_인자는_예외없이_빈_목록을_반환한다()
        {
            Assert.AreEqual(0, DungeonPowerSummary.Build(null, _cfg).Count);
            Assert.AreEqual(0, DungeonPowerSummary.Build(new MetaProfile(), null).Count);
            Assert.AreEqual(0, DungeonPowerSummary.Build(null, null).Count);
        }

        //# 방어 — ShopItems 에 null / 빈 Id 항목이 섞여도 skip (NRE 안전).
        [Test]
        public void null이거나_빈Id_상점항목은_건너뛴다()
        {
            _cfg.ShopItems.Add(null);
            _cfg.ShopItems.Add(new ShopItemDef { Id = "", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.02f, MaxLevel = 5 });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 5);
            List<DungeonPowerLine> lines = DungeonPowerSummary.Build(p, _cfg);
            Assert.AreEqual(1, lines.Count, "null/빈 Id 항목은 skip — 정상 HP 항목만 남음");
            Assert.AreEqual("HP", lines[0].Label);
        }

        //# 방어 — ShopItems 가 비어 있으면 빈 목록.
        [Test]
        public void 빈_상점목록은_빈_요약을_반환한다()
        {
            _cfg.ShopItems.Clear();
            Assert.AreEqual(0, DungeonPowerSummary.Build(만렙프로필(), _cfg).Count);
        }
    }
}
