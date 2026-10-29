using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 유물 전투 스냅샷 집계 (relics §3.3, §6.1~6.4, §12.6 "중첩 계산 / 보상 가산 / 읽기 방어").
    public class RelicBattleBonusTests
    {
        private const float Tol = 1e-4f;
        private RelicDatabase _db;

        [SetUp]
        public void 출하_유물_DB를_만든다()
        {
            _db = RelicTestSupport.ShippedDb();
        }

        private static MetaProfile Own(params (string id, int stack)[] relics)
        {
            MetaProfile profile = new MetaProfile();
            foreach ((string id, int stack) in relics)
            {
                profile.Relics.Add(new RelicEntry { RelicId = id, Stack = stack });
            }
            return profile;
        }

        private RelicBattleBonus Bonus(params (string id, int stack)[] relics)
        {
            return RelicBattleBonus.From(Own(relics), _db);
        }

        //# --- 능력치 곱 / S(n) ---

        [Test]
        public void HeartOfStone_HP_배율은_1_더하기_0_03_x_S_n이다()
        {
            float[] expected = { 1.03f, 1.048f, 1.063f, 1.075f, 1.084f };

            for (int n = 1; n <= 5; ++n)
            {
                Assert.AreEqual(expected[n - 1], Bonus(("HeartOfStone", n)).GetStatMul(EMonsterStatKind.Hp), Tol, $"n={n}");
            }
        }

        [Test]
        public void 능력치_유물은_각자_자기_축에만_곱해진다()
        {
            RelicBattleBonus b = Bonus(("RustyBlade", 5), ("SwiftBoots", 5), ("HeartOfStone", 1));

            Assert.AreEqual(1.056f, b.GetStatMul(EMonsterStatKind.Power), Tol);
            Assert.AreEqual(1.056f, b.GetStatMul(EMonsterStatKind.MoveSpeed), Tol);
            Assert.AreEqual(1.03f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
            Assert.AreEqual(1f, b.GetStatMul(EMonsterStatKind.Cooldown), Tol);
            Assert.AreEqual(1f, b.GetStatMul(EMonsterStatKind.Range), Tol, "상점 축 — 유물 미사용");
            Assert.AreEqual(1f, b.GetStatMul(EMonsterStatKind.PoisonDamage), Tol, "상점 축 — 유물 미사용");
        }

        [Test]
        public void BrokenHourglass_Cooldown은_음수_amount라_배율이_1_미만이다()
        {
            Assert.AreEqual(0.98f, Bonus(("BrokenHourglass", 1)).GetStatMul(EMonsterStatKind.Cooldown), Tol);
            Assert.AreEqual(0.958f, Bonus(("BrokenHourglass", 3)).GetStatMul(EMonsterStatKind.Cooldown), Tol, "1-0.02x2.1");
        }

        [Test]
        public void KingsCrownShard는_HP와_공격력을_같이_5퍼센트_올린다()
        {
            RelicBattleBonus b = Bonus(("KingsCrownShard", 1));

            Assert.AreEqual(1.05f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
            Assert.AreEqual(1.05f, b.GetStatMul(EMonsterStatKind.Power), Tol);
        }

        [Test]
        public void 같은_축_유물은_곱연산으로_누적된다()
        {
            RelicBattleBonus b = Bonus(("HeartOfStone", 5), ("KingsCrownShard", 1));

            Assert.AreEqual(1.084f * 1.05f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
            Assert.AreEqual(1.1382f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
        }

        [Test]
        public void BreedingCharm_스포너_주기_배율은_1_더하기_음수_amount_x_S_n이다()
        {
            Assert.AreEqual(0.97f, Bonus(("BreedingCharm", 1)).SpawnerPeriodMul, Tol);
            Assert.AreEqual(0.937f, Bonus(("BreedingCharm", 3)).SpawnerPeriodMul, Tol, "1-0.03x2.1");
            Assert.AreEqual(1f, Bonus(("HeartOfStone", 5)).SpawnerPeriodMul, Tol);
        }

        [Test]
        public void SpawnPrelude_첫_스폰_단축은_3초_x_S_n이고_중첩상한_1이다()
        {
            Assert.AreEqual(3f, Bonus(("SpawnPrelude", 1)).SpawnPreludeAdvanceSec, Tol);
            Assert.AreEqual(3f, Bonus(("SpawnPrelude", 4)).SpawnPreludeAdvanceSec, Tol, "maxStack 1 로 클램프");
            Assert.AreEqual(0f, Bonus().SpawnPreludeAdvanceSec, Tol);
        }

        //# --- 전투 효과 파라미터 ---

        [Test]
        public void EmberBrand_화상_비율은_0_0002_x_S_n이다()
        {
            Assert.AreEqual(0.0002f, Bonus(("EmberBrand", 1)).HeroBurnRatioPerSec, 1e-7f);
            Assert.AreEqual(0.00056f, Bonus(("EmberBrand", 5)).HeroBurnRatioPerSec, 1e-7f);
            Assert.AreEqual(0f, Bonus().HeroBurnRatioPerSec);
        }

        [Test]
        public void ThunderShard_간격은_고정_10초_비율만_S_n으로_커진다()
        {
            RelicBattleBonus b = Bonus(("ThunderShard", 3));

            Assert.AreEqual(1, b.Lightnings.Count);
            Assert.AreEqual(10f, b.Lightnings[0].IntervalSec, Tol);
            Assert.AreEqual(0.0063f, b.Lightnings[0].HpRatio, 1e-6f);
        }

        [Test]
        public void CursedBanner_영웅_공격력_배율은_1_빼기_amount_x_S_n이다()
        {
            RelicBattleBonus none = Bonus();
            RelicBattleBonus b = Bonus(("CursedBanner", 3));

            Assert.IsFalse(none.HasHeroAttackDown);
            Assert.AreEqual(1f, none.HeroAttackFactor, Tol);
            Assert.IsTrue(b.HasHeroAttackDown);
            Assert.AreEqual(0.916f, b.HeroAttackFactor, Tol);
        }

        [Test]
        public void AmbushHorn_소환_마릿수는_countPerStack_x_n으로_S_n과_무관한_선형이다()
        {
            for (int n = 1; n <= 3; ++n)
            {
                RelicBattleBonus b = Bonus(("AmbushHorn", n));
                Assert.AreEqual(1, b.OpeningSummons.Count);
                Assert.AreEqual(EMonster.Wisp, b.OpeningSummons[0].Monster);
                Assert.AreEqual(2 * n, b.OpeningSummons[0].Count, $"n={n}");
            }
        }

        [Test]
        public void WrathSpirit_광폭화는_HP_50퍼센트_10초_고정이다()
        {
            RelicBattleBonus b = Bonus(("WrathSpirit", 1));

            Assert.AreEqual(1, b.Frenzies.Count);
            Assert.AreEqual(0.5f, b.Frenzies[0].HpFraction, Tol);
            Assert.AreEqual(10f, b.Frenzies[0].DurationSec, Tol);
        }

        [Test]
        public void HasteHourglass는_배속_2를_켜고_미보유면_배속_없음이다()
        {
            RelicBattleBonus on = Bonus(("HasteHourglass", 1));
            RelicBattleBonus off = Bonus(("HeartOfStone", 1));

            Assert.IsTrue(on.HasBattleSpeed);
            Assert.AreEqual(2f, on.BattleSpeedScale, Tol);
            Assert.IsFalse(off.HasBattleSpeed);
            Assert.AreEqual(0f, off.BattleSpeedScale);
        }

        //# --- 보상 가산 ---

        [Test]
        public void 승리_소울은_SoulPurse로_가산되고_XP는_그대로다()
        {
            RelicBattleBonus b = Bonus(("SoulPurse", 1));

            SoulReward reward = b.ApplyReward(new SoulReward { Souls = 100, Xp = 50 }, true);

            Assert.AreEqual(104, reward.Souls, "부동소수 오차로 103 이 되지 않도록 보정");
            Assert.AreEqual(50, reward.Xp);
        }

        [Test]
        public void 승리_XP는_LordsTome으로_가산되고_소울은_그대로다()
        {
            RelicBattleBonus b = Bonus(("LordsTome", 1));

            SoulReward reward = b.ApplyReward(new SoulReward { Souls = 100, Xp = 25 }, true);

            Assert.AreEqual(100, reward.Souls);
            Assert.AreEqual(26, reward.Xp, "25 x 1.04 = 26.0");
        }

        [Test]
        public void 보상은_내림이다()
        {
            RelicBattleBonus b = Bonus(("SoulPurse", 5), ("LordsTome", 5));

            SoulReward reward = b.ApplyReward(new SoulReward { Souls = 100, Xp = 50 }, true);

            Assert.AreEqual(111, reward.Souls, "100 x 1.112 = 111.2");
            Assert.AreEqual(55, reward.Xp, "50 x 1.112 = 55.6 → 내림");
        }

        [Test]
        public void 패배_소울은_SoulBonus와_LoseSoulBonus를_합산해_한번만_내림한다()
        {
            RelicBattleBonus b = Bonus(("SoulPurse", 1), ("ConsolationFeather", 1));

            SoulReward reward = b.ApplyReward(new SoulReward { Souls = 10, Xp = 0 }, false);

            Assert.AreEqual(12, reward.Souls, "10 x 1.29 = 12.9 → 12 (개별 내림 후 합산이면 10)");
        }

        [Test]
        public void ConsolationFeather는_패배에서만_소울에_작용하고_XP에는_작용하지_않는다()
        {
            RelicBattleBonus b = Bonus(("ConsolationFeather", 1));

            SoulReward lose = b.ApplyReward(new SoulReward { Souls = 200, Xp = 40 }, false);
            SoulReward win = b.ApplyReward(new SoulReward { Souls = 200, Xp = 40 }, true);

            Assert.AreEqual(250, lose.Souls, "200 x (1+0.25)");
            Assert.AreEqual(40, lose.Xp);
            Assert.AreEqual(200, win.Souls);
            Assert.AreEqual(40, win.Xp);
        }

        //# 회귀 박제(의심 결함): ConsolationFeather n=3 의 float 곱(0.25f x 2.1f = 0.52499998)이 정수 경계 200 x 1.525 = 305.0 에서
        //# 오차 -4.8e-6 이라 +1e-6 보정으로 부족해 304 로 내림될 수 있다. 수학적 기대값 305 를 고정한다 — 실패하면 production 보정폭 문제.
        [Test]
        public void 부동소수_경계_패배_소울_200은_수학적_기대값_305로_내림된다()
        {
            RelicBattleBonus b = Bonus(("ConsolationFeather", 3));

            SoulReward lose = b.ApplyReward(new SoulReward { Souls = 200, Xp = 0 }, false);

            Assert.AreEqual(305, lose.Souls, "200 x (1 + 0.25 x 2.1)");
        }

        [Test]
        public void 패배에서도_XP는_LordsTome만_적용된다()
        {
            RelicBattleBonus b = Bonus(("LordsTome", 1), ("ConsolationFeather", 3));

            SoulReward lose = b.ApplyReward(new SoulReward { Souls = 0, Xp = 50 }, false);

            Assert.AreEqual(52, lose.Xp, "50 x 1.04");
        }

        [Test]
        public void 보상이_0이거나_유물이_없으면_값이_변하지_않는다()
        {
            SoulReward zero = Bonus(("SoulPurse", 5)).ApplyReward(new SoulReward(), true);
            SoulReward none = RelicBattleBonus.None.ApplyReward(new SoulReward { Souls = 77, Xp = 33 }, false);

            Assert.AreEqual(0, zero.Souls);
            Assert.AreEqual(0, zero.Xp);
            Assert.AreEqual(77, none.Souls);
            Assert.AreEqual(33, none.Xp);
        }

        [Test]
        public void 전투_시작_스냅샷은_이후_드랍을_반영하지_않는다()
        {
            MetaProfile profile = Own(("SoulPurse", 1));
            RelicBattleBonus snapshot = RelicBattleBonus.From(profile, _db);

            profile.AddRelic("SoulPurse");
            profile.AddRelic("LordsTome");
            SoulReward stale = snapshot.ApplyReward(new SoulReward { Souls = 100, Xp = 50 }, true);
            SoulReward fresh = RelicBattleBonus.From(profile, _db).ApplyReward(new SoulReward { Souls = 100, Xp = 50 }, true);

            Assert.AreEqual(104, stale.Souls, "이번 판 드랍은 이번 판 보상에 미반영");
            Assert.AreEqual(50, stale.Xp);
            Assert.AreEqual(106, fresh.Souls, "다음 전투(Retry)는 새로 집계 (E-8)");
            Assert.AreEqual(52, fresh.Xp);
        }

        //# --- 읽기 방어 ---

        [Test]
        public void DB에_없는_id는_무시하고_프로필을_수정하지_않는다()
        {
            MetaProfile profile = Own(("GhostRelic", 5), ("SoulPurse", 1));

            RelicBattleBonus b = RelicBattleBonus.From(profile, _db);

            Assert.AreEqual(104, b.ApplyReward(new SoulReward { Souls = 100 }, true).Souls);
            Assert.AreEqual(5, profile.GetRelicStack("GhostRelic"));
            Assert.AreEqual(2, profile.Relics.Count);
        }

        [Test]
        public void maxStack_초과_중첩은_읽을_때_maxStack으로_클램프되고_세이브는_그대로다()
        {
            MetaProfile profile = Own(("HeartOfStone", 99), ("KingsCrownShard", 3), ("AmbushHorn", 7), ("EmberBrand", 6));

            RelicBattleBonus b = RelicBattleBonus.From(profile, _db);

            Assert.AreEqual(1.084f * 1.05f, b.GetStatMul(EMonsterStatKind.Hp), Tol, "HeartOfStone 99 → 5, Kings 3 → 1");
            Assert.AreEqual(1.05f, b.GetStatMul(EMonsterStatKind.Power), Tol, "Kings 는 S(1) 로 클램프");
            Assert.AreEqual(6, b.OpeningSummons[0].Count, "AmbushHorn 7 → maxStack 3 → 6마리");
            Assert.AreEqual(0.00056f, b.HeroBurnRatioPerSec, 1e-7f, "EmberBrand 6 → 5");
            Assert.AreEqual(99, profile.GetRelicStack("HeartOfStone"), "세이브 비수정");
            Assert.AreEqual(7, profile.GetRelicStack("AmbushHorn"));
        }

        [Test]
        public void Stack_0이하와_null_행은_무시한다()
        {
            MetaProfile profile = Own(("HeartOfStone", 0), ("RustyBlade", -2));
            profile.Relics.Add(null);

            RelicBattleBonus b = RelicBattleBonus.From(profile, _db);

            Assert.AreEqual(1f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
            Assert.AreEqual(1f, b.GetStatMul(EMonsterStatKind.Power), Tol);
        }

        [Test]
        public void null_프로필_DB_Relics는_빈_보너스를_돌려준다()
        {
            MetaProfile nullList = new MetaProfile { Relics = null };

            RelicBattleBonus a = RelicBattleBonus.From(null, _db);
            RelicBattleBonus b = RelicBattleBonus.From(new MetaProfile(), null);
            RelicBattleBonus c = RelicBattleBonus.From(nullList, _db);

            foreach (RelicBattleBonus bonus in new[] { a, b, c })
            {
                Assert.AreEqual(1f, bonus.GetStatMul(EMonsterStatKind.Hp));
                Assert.AreEqual(1f, bonus.SpawnerPeriodMul);
                Assert.AreEqual(1f, bonus.HeroAttackFactor);
                Assert.IsFalse(bonus.HasBattleSpeed);
                Assert.AreEqual(0, bonus.OpeningSummons.Count);
            }
        }

        [Test]
        public void None은_모든_효과가_중립이다()
        {
            RelicBattleBonus none = RelicBattleBonus.None;

            Assert.AreEqual(1f, none.GetStatMul(EMonsterStatKind.Power));
            Assert.AreEqual(1f, none.SpawnerPeriodMul);
            Assert.AreEqual(0f, none.SoulBonus);
            Assert.AreEqual(0f, none.XpBonus);
            Assert.AreEqual(0f, none.LoseSoulBonus);
            Assert.AreEqual(0, none.Lightnings.Count);
            Assert.AreEqual(0, none.Frenzies.Count);
        }

        //# --- 하한 가드 (수치를 비틀어 도달시킨다) ---

        [Test]
        public void 능력치_곱은_0_5_미만으로_내려가지_않는다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Param("RustyBlade", 1, "amount").value = -0.9f;
            RelicDatabase harsh = set.Build();

            RelicBattleBonus b = RelicBattleBonus.From(Own(("RustyBlade", 5)), harsh);

            Assert.AreEqual(RelicBattleBonus.MulFloor, b.GetStatMul(EMonsterStatKind.Power), Tol);
        }

        [Test]
        public void 스포너_주기_배율도_0_5_미만으로_내려가지_않는다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Param("BreedingCharm", 1, "amount").value = -0.9f;
            RelicDatabase harsh = set.Build();

            RelicBattleBonus b = RelicBattleBonus.From(Own(("BreedingCharm", 3)), harsh);

            Assert.AreEqual(0.5f, b.SpawnerPeriodMul, Tol);
        }

        [Test]
        public void 영웅_공격력_배율은_0_1_미만으로_내려가지_않는다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Param("CursedBanner", 1, "amount").value = 0.5f;
            RelicDatabase harsh = set.Build();

            RelicBattleBonus b = RelicBattleBonus.From(Own(("CursedBanner", 3)), harsh);

            Assert.IsTrue(b.HasHeroAttackDown);
            Assert.AreEqual(RelicBattleBonus.AttackFactorFloor, b.HeroAttackFactor, Tol);
        }

        [Test]
        public void 하한_경계_정확히_0_5와_0_1은_그대로다()
        {
            RelicDataSet set = RelicDataSet.Shipped();
            set.Param("RustyBlade", 1, "amount").value = -0.5f;
            set.Param("CursedBanner", 1, "amount").value = 0.9f;
            RelicDatabase edge = set.Build();

            RelicBattleBonus b = RelicBattleBonus.From(Own(("RustyBlade", 1), ("CursedBanner", 1)), edge);

            Assert.AreEqual(0.5f, b.GetStatMul(EMonsterStatKind.Power), Tol);
            Assert.AreEqual(0.1f, b.HeroAttackFactor, Tol);
        }

        //# --- 풀셋 회귀 고정 (기획서 §1.2~1.3) ---

        [Test]
        public void 전_유물_최대_중첩_풀셋의_집계값이_기획서와_같다()
        {
            MetaProfile profile = new MetaProfile();
            foreach (RelicData data in _db.Relics)
            {
                profile.Relics.Add(new RelicEntry { RelicId = data.RelicId, Stack = data.MaxStack });
            }

            RelicBattleBonus b = RelicBattleBonus.From(profile, _db);

            Assert.AreEqual(1.056f * 1.05f, b.GetStatMul(EMonsterStatKind.Power), Tol, "1.1088");
            Assert.AreEqual(1.084f * 1.05f, b.GetStatMul(EMonsterStatKind.Hp), Tol);
            Assert.AreEqual(0.958f, b.GetStatMul(EMonsterStatKind.Cooldown), Tol);
            Assert.AreEqual(1.056f, b.GetStatMul(EMonsterStatKind.MoveSpeed), Tol);
            Assert.AreEqual(0.937f, b.SpawnerPeriodMul, Tol);
            Assert.AreEqual(3f, b.SpawnPreludeAdvanceSec, Tol);
            Assert.AreEqual(0.00056f, b.HeroBurnRatioPerSec, 1e-7f);
            Assert.AreEqual(0.0063f, b.Lightnings[0].HpRatio, 1e-6f);
            Assert.AreEqual(0.916f, b.HeroAttackFactor, Tol);
            Assert.AreEqual(6, b.OpeningSummons[0].Count);
            Assert.AreEqual(1, b.Frenzies.Count);
            Assert.AreEqual(2f, b.BattleSpeedScale, Tol);
            Assert.AreEqual(0.112f, b.SoulBonus, Tol);
            Assert.AreEqual(0.112f, b.XpBonus, Tol);
            Assert.AreEqual(0.525f, b.LoseSoulBonus, Tol);
        }
    }
}
