using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# MetaBattleBonus 엣지 — 프로필/설정 불일치, 비정상 레벨 (기존 MetaBattleBonusTests 의 정상 집계와 비중복).
    public class MetaBattleBonusEdgeTests
    {
        private MetaConfig _cfg;

        [SetUp]
        public void 준비()
        {
            _cfg = ScriptableObject.CreateInstance<MetaConfig>();
        }

        [TearDown]
        public void 정리()
        {
            Object.DestroyImmediate(_cfg);
        }

        [Test]
        public void 설정에_없는_품목_레벨은_무시되고_전부_항등이다()
        {
            //# 구버전 세이브에 폐기된 품목 id 가 남은 경우 — cfg.ShopItems 기준 순회라 안전 무시.
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("폐기된품목", 3);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, _cfg);
            Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);
            Assert.AreEqual(1f, bonus.SpawnerPeriodMul, 0.001f);
        }

        [Test]
        public void 만렙초과_비정상_레벨도_저장된_레벨_그대로_거듭제곱된다()
        {
            //# 현행 동작 박제 — From 은 MaxLevel 클램프 없음 (정상 경로는 ShopService 가 만렙 구매를 차단).
            //# 세이브 수동 변조 시 과적용 허용이 의도인지 gameplay-programmer 판단 필요 (보고서 참조).
            _cfg.ShopItems.Add(new ShopItemDef { Id = "MonsterHpUp", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f, MaxLevel = 5 });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("MonsterHpUp", 7);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, _cfg);
            Assert.AreEqual(Mathf.Pow(1.1f, 7), bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);
        }

        [Test]
        public void 같은_StatKind_품목_2개는_곱연산_누적된다()
        {
            _cfg.ShopItems.Add(new ShopItemDef { Id = "HpA", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.1f });
            _cfg.ShopItems.Add(new ShopItemDef { Id = "HpB", EffectKind = EShopEffectKind.MonsterStat, StatKind = EMonsterStatKind.Hp, PerLevelMul = 1.2f });
            MetaProfile p = new MetaProfile();
            p.SetShopLevel("HpA", 1);
            p.SetShopLevel("HpB", 1);

            MetaBattleBonus bonus = MetaBattleBonus.From(p, _cfg);
            Assert.AreEqual(1.1f * 1.2f, bonus.GetStatMul(EMonsterStatKind.Hp), 0.001f);
        }

        [Test]
        public void null_입력이면_전부_항등이다()
        {
            MetaBattleBonus fromNullProfile = MetaBattleBonus.From(null, _cfg);
            Assert.AreEqual(1f, fromNullProfile.GetStatMul(EMonsterStatKind.Hp), 0.001f);

            MetaBattleBonus fromNullConfig = MetaBattleBonus.From(new MetaProfile(), null);
            Assert.AreEqual(1f, fromNullConfig.SpawnerPeriodMul, 0.001f);
        }
    }
}
