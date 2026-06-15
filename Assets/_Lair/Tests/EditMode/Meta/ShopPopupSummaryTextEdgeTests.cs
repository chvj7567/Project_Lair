using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup.BuildSummaryText — 최대 길이(7품목 만렙)·단일 항목·방어 (기획서 §2.2).
    //# TDD 본 테스트(ShopPopupSummaryTextTests, 2종)와 중복 금지 — 7품목 풀셋 문자열·null 가드만 보강.
    public class ShopPopupSummaryTextEdgeTests
    {
        private MetaConfig _cfg;

        //# 기획서 §2.1 마스터 표 7품목 — ShopItems 순서 그대로 (요약줄 join 순서 = ShopItems 순서).
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

        //# 기획서 §2.2 최대 길이 표기 — 전 7품목 만렙 줄. 글자 단위 일치 (접두 더블스페이스 + " · " U+00B7 구분자).
        [Test]
        public void 전_7품목_만렙은_기획서_최대길이_표기와_글자단위로_일치한다()
        {
            MetaProfile p = new MetaProfile();
            foreach (ShopItemDef item in _cfg.ShopItems)
                p.SetShopLevel(item.Id, item.MaxLevel);

            Assert.AreEqual(
                "현재 강화  HP +10% · 공격 +8% · 공속 +5% · 이동 +8% · 사거리 +8% · 둔화 +11% · 스폰률 +8%",
                ShopPopup.BuildSummaryText(p, _cfg));
        }

        //# 기획서 §2.2 표 — 단일 항목(HP 만렙만)은 구분자 없이 한 토큰만.
        [Test]
        public void 단일_항목은_구분자_없이_한_토큰만_표기한다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 5);
            Assert.AreEqual("현재 강화  HP +10%", ShopPopup.BuildSummaryText(p, _cfg));
        }

        //# 방어 — null profile / null cfg 는 강화 0건과 동일하게 "아직 없음" 베이스라인 (NRE 안전).
        [Test]
        public void null_인자는_예외없이_아직없음을_표기한다()
        {
            Assert.AreEqual("현재 강화  아직 없음", ShopPopup.BuildSummaryText(null, _cfg));
            Assert.AreEqual("현재 강화  아직 없음", ShopPopup.BuildSummaryText(new MetaProfile(), null));
        }
    }
}
