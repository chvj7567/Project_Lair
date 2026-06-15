using Lair.Data;
using Lair.Meta;
using Lair.UI;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# ShopPopup.BuildSummaryText — 요약줄 문구 조립 검증 (기획서 §2.2 표시 SoT).
    public class ShopPopupSummaryTextTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.02f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterAtkSpeedUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Cooldown, PerLevelMul = 0.99f, MaxLevel = 5 });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "SpawnerHasteUp", EffectKind = EShopEffectKind.SpawnerPeriod, PerLevelMul = 0.985f, MaxLevel = 5 });
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 강화_0건이면_아직_없음_베이스라인을_표기한다()
        {
            //# 기획서 §2.2 — 접두 + 더블 스페이스 + "아직 없음"
            Assert.AreEqual("현재 강화  아직 없음", ShopPopup.BuildSummaryText(new MetaProfile(), _cfg));
        }

        [Test]
        public void 여러_항목은_가운뎃점_구분자로_조립된다()
        {
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 5);                 //# +10%
            p.SetShopLevel("MonsterAtkSpeedUp", 5);           //# +5%
            p.SetShopLevel("SpawnerHasteUp", 5);              //# +8%
            //# 기획서 §2.2 조립 예시 — 접두 + 더블 스페이스 + 토큰 " · " join
            Assert.AreEqual("현재 강화  HP +10% · 공속 +5% · 스폰률 +8%", ShopPopup.BuildSummaryText(p, _cfg));
        }
    }
}
