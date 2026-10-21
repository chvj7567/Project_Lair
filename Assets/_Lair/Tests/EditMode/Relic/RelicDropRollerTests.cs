using System;
using System.Collections.Generic;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# 유물 드랍 굴림 (relics §2, §12.6 "드랍 굴림") — 난수원 주입. Roll 은 rng 를 (확률 판정 → 등급 → 유물) 순으로 최대 3번 부른다.
    public class RelicDropRollerTests
    {
        private const float Tol = 1e-5f;
        private RelicDatabase _db;

        [SetUp]
        public void 출하_유물_DB를_만든다()
        {
            _db = RelicTestSupport.ShippedDb();
        }

        //# 첫 유물 보장(chance=1.0)을 피하려고 보유 유물 하나를 깔아 둔 프로필.
        private static MetaProfile Veteran(int streak = 0, string owned = "SoulPurse")
        {
            MetaProfile profile = new MetaProfile { RelicMissStreak = streak };
            profile.AddRelic(owned);
            return profile;
        }

        private static int StackSum(MetaProfile profile)
        {
            int sum = 0;
            foreach (RelicEntry entry in profile.Relics)
            {
                sum += entry.Stack;
            }
            return sum;
        }

        private RelicDropResult Roll(MetaProfile profile, string hero, int diff, bool win, params float[] rng)
        {
            ScriptedRng source = new ScriptedRng(rng);
            return RelicDropRoller.Roll(_db, profile, hero, 1, 1, diff, win, source.Next);
        }

        //# --- 확률 공식 ---

        [Test]
        public void 승리_확률은_스테이지_기본확률_x_난이도_배율이다()
        {
            MetaProfile p = Veteran();

            Assert.AreEqual(0.300f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 0, true), Tol);
            Assert.AreEqual(0.375f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 1, true), Tol);
            Assert.AreEqual(0.450f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 2, true), Tol);
            Assert.AreEqual(0.600f, RelicDropRoller.ComputeChance(_db, p, 1, 5, 0, true), Tol);
            Assert.AreEqual(0.750f, RelicDropRoller.ComputeChance(_db, p, 1, 5, 1, true), Tol);
            Assert.AreEqual(0.900f, RelicDropRoller.ComputeChance(_db, p, 1, 5, 2, true), Tol);
        }

        [Test]
        public void 패배_확률은_승리_확률의_0_25배이다()
        {
            MetaProfile p = Veteran();

            Assert.AreEqual(0.075f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 0, false), Tol);
            Assert.AreEqual(0.09375f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 1, false), Tol);
            Assert.AreEqual(0.1125f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 2, false), Tol);
            Assert.AreEqual(0.150f, RelicDropRoller.ComputeChance(_db, p, 1, 5, 0, false), Tol);
        }

        [Test]
        public void 연패_보정은_연패당_0_10이고_최대_0_40에서_멈춘다()
        {
            float[] expected = { 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.70f, 0.70f };
            int[] streaks = { 0, 1, 2, 3, 4, 5, 50 };

            for (int i = 0; i < streaks.Length; ++i)
            {
                Assert.AreEqual(expected[i], RelicDropRoller.ComputeChance(_db, Veteran(streaks[i]), 1, 1, 0, true), Tol, $"streak {streaks[i]}");
            }
        }

        [Test]
        public void 패배_확률은_연패_카운터를_읽지_않는다()
        {
            float cold = RelicDropRoller.ComputeChance(_db, Veteran(0), 1, 1, 1, false);
            float hot = RelicDropRoller.ComputeChance(_db, Veteran(4), 1, 1, 1, false);

            Assert.AreEqual(cold, hot, 0f);
        }

        [Test]
        public void 최종_확률은_maxChance_0_95로_절단된다()
        {
            Assert.AreEqual(0.95f, RelicDropRoller.ComputeChance(_db, Veteran(4), 1, 5, 2, true), Tol, "0.9+0.4=1.3");
            Assert.AreEqual(0.95f, RelicDropRoller.ComputeChance(_db, Veteran(1), 1, 5, 2, true), Tol, "0.9+0.1=1.0");
            Assert.AreEqual(0.90f, RelicDropRoller.ComputeChance(_db, Veteran(0), 1, 5, 2, true), Tol, "상한 아래는 절단 없음");
        }

        [Test]
        public void 보유_유물이_없으면_승리_확률은_1_0이고_패배에는_적용되지_않는다()
        {
            MetaProfile empty = new MetaProfile();

            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, empty, 1, 1, 0, true), Tol);
            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, empty, 1, 5, 2, true), Tol, "maxChance 보다 우선");
            Assert.AreEqual(0.075f, RelicDropRoller.ComputeChance(_db, empty, 1, 1, 0, false), Tol);
        }

        [Test]
        public void DB에_없는_id나_Stack_0_행만_있으면_보유_0개로_보고_첫_유물_보장이_적용된다()
        {
            MetaProfile ghost = new MetaProfile();
            SetStackRaw(ghost, "GhostRelic", 3);
            MetaProfile zero = new MetaProfile();
            SetStackRaw(zero, "HeartOfStone", 0);
            MetaProfile nulls = new MetaProfile();
            nulls.Relics.Add(null);

            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, ghost, 1, 1, 0, true), Tol);
            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, zero, 1, 1, 0, true), Tol);
            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, nulls, 1, 1, 0, true), Tol);
        }

        private static void SetStackRaw(MetaProfile profile, string id, int stack)
        {
            profile.Relics.Add(new RelicEntry { RelicId = id, Stack = stack });
        }

        [Test]
        public void Relics가_null이어도_보유_0개로_취급한다()
        {
            MetaProfile p = new MetaProfile { Relics = null };

            Assert.AreEqual(1f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 0, true), Tol);
        }

        [Test]
        public void 범위_밖_난이도는_양끝_배율로_클램프된다()
        {
            MetaProfile p = Veteran();

            Assert.AreEqual(0.45f, RelicDropRoller.ComputeChance(_db, p, 1, 1, 99, true), Tol);
            Assert.AreEqual(0.30f, RelicDropRoller.ComputeChance(_db, p, 1, 1, -1, true), Tol);
        }

        [Test]
        public void 없는_스테이지는_기본확률_0이라_연패_보정만_남는다()
        {
            Assert.AreEqual(0f, RelicDropRoller.ComputeChance(_db, Veteran(0), 9, 9, 0, true), Tol);
            Assert.AreEqual(0.2f, RelicDropRoller.ComputeChance(_db, Veteran(2), 9, 9, 0, true), Tol);
        }

        //# --- 굴림 결과 / 상태 갱신 ---

        [Test]
        public void 확률_경계_rng가_확률_미만이면_획득하고_같거나_크면_미획득이다()
        {
            MetaProfile hit = Veteran();
            MetaProfile miss = Veteran();

            RelicDropResult below = Roll(hit, "Knight", 0, true, 0.29f, 0f, 0f);
            RelicDropResult equal = Roll(miss, "Knight", 0, true, 0.30f);

            Assert.AreEqual(ERelicDropOutcome.Acquired, below.Outcome);
            Assert.AreEqual(ERelicDropOutcome.Missed, equal.Outcome, "rng >= chance 는 미획득");
        }

        [Test]
        public void 승리_미획득이면_연패가_1_늘고_다음_승리_확률을_알려준다()
        {
            MetaProfile p = Veteran(0);

            RelicDropResult result = Roll(p, "Knight", 0, true, 0.99f);

            Assert.AreEqual(ERelicDropOutcome.Missed, result.Outcome);
            Assert.AreEqual(1, p.RelicMissStreak);
            Assert.IsTrue(result.ShowNextChance);
            Assert.AreEqual(40, result.NextWinChancePercent, "0.30 + 0.10");
            Assert.IsNull(result.RelicId);
        }

        [Test]
        public void 다음_승리_확률은_난이도_보스_상한을_반영한_정수_퍼센트다()
        {
            MetaProfile boss = Veteran(0);
            RelicDropResult easyBoss = RelicDropRoller.Roll(_db, boss, "Knight", 1, 5, 0, true, new ScriptedRng(0.99f).Next);
            MetaProfile hard = Veteran(3);
            RelicDropResult hardNormal = RelicDropRoller.Roll(_db, hard, "Knight", 1, 1, 2, true, new ScriptedRng(0.99f).Next);
            MetaProfile capped = Veteran(3);
            RelicDropResult hardBoss = RelicDropRoller.Roll(_db, capped, "Knight", 1, 5, 2, true, new ScriptedRng(0.99f).Next);

            Assert.AreEqual(70, easyBoss.NextWinChancePercent, "0.60+0.10");
            Assert.AreEqual(85, hardNormal.NextWinChancePercent, "0.45+0.40 (streak 3→4)");
            Assert.AreEqual(95, hardBoss.NextWinChancePercent, "maxChance 0.95 절단");
        }

        [Test]
        public void 연속_미획득은_연패를_누적하고_확률이_오른다()
        {
            MetaProfile p = Veteran(0);

            Roll(p, "Knight", 0, true, 0.99f);
            Roll(p, "Knight", 0, true, 0.99f);
            RelicDropResult third = Roll(p, "Knight", 0, true, 0.99f);

            Assert.AreEqual(3, p.RelicMissStreak);
            Assert.AreEqual(60, third.NextWinChancePercent, "0.30 + 0.30");
        }

        [Test]
        public void 연패_보정이_굴림_판정에_실제로_쓰인다()
        {
            MetaProfile cold = Veteran(0);
            MetaProfile hot = Veteran(3);

            RelicDropResult coldResult = Roll(cold, "Knight", 0, true, 0.55f);
            RelicDropResult hotResult = Roll(hot, "Knight", 0, true, 0.55f, 0f, 0f);

            Assert.AreEqual(ERelicDropOutcome.Missed, coldResult.Outcome, "0.55 >= 0.30");
            Assert.AreEqual(ERelicDropOutcome.Acquired, hotResult.Outcome, "0.55 < 0.60");
        }

        [Test]
        public void 연패_보정_상한은_굴림에서도_0_40이다()
        {
            MetaProfile p = Veteran(10);

            RelicDropResult below = Roll(p, "Knight", 0, true, 0.69f, 0f, 0f);
            MetaProfile q = Veteran(10);
            RelicDropResult above = Roll(q, "Knight", 0, true, 0.71f);

            Assert.AreEqual(ERelicDropOutcome.Acquired, below.Outcome);
            Assert.AreEqual(ERelicDropOutcome.Missed, above.Outcome);
        }

        [Test]
        public void 승리_획득이면_연패_카운터가_0으로_초기화된다()
        {
            MetaProfile p = Veteran(4);

            RelicDropResult result = Roll(p, "Knight", 0, true, 0f, 0f, 0f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual(0, p.RelicMissStreak);
            Assert.IsFalse(result.ShowNextChance);
        }

        [Test]
        public void 패배_미획득은_연패_카운터를_바꾸지_않고_다음_확률을_안내하지_않는다()
        {
            MetaProfile p = Veteran(2);

            RelicDropResult result = Roll(p, "Knight", 0, false, 0.99f);

            Assert.AreEqual(ERelicDropOutcome.Missed, result.Outcome);
            Assert.AreEqual(2, p.RelicMissStreak);
            Assert.IsFalse(result.ShowNextChance);
            Assert.AreEqual(0, result.NextWinChancePercent);
        }

        [Test]
        public void 패배_굴림은_연패_보정_없이_원래_확률_0_075로_판정한다()
        {
            MetaProfile hot = Veteran(4);

            RelicDropResult miss = Roll(hot, "Knight", 0, false, 0.10f);
            MetaProfile hit = Veteran(4);
            RelicDropResult acquired = Roll(hit, "Knight", 0, false, 0.07f, 0f, 0f);

            Assert.AreEqual(ERelicDropOutcome.Missed, miss.Outcome, "pity 가 쓰였다면 0.7 이라 획득");
            Assert.AreEqual(ERelicDropOutcome.Acquired, acquired.Outcome);
        }

        [Test]
        public void 패배에서_획득하면_연패_카운터가_0으로_초기화된다()
        {
            MetaProfile p = Veteran(3);

            RelicDropResult result = Roll(p, "Knight", 0, false, 0.05f, 0f, 0f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual(0, p.RelicMissStreak, "기획서 §2.1/§2.5: 드랍 시에만 초기화");
        }

        [Test]
        public void 첫_유물은_승리_시_100퍼센트_보장되고_패배에는_적용되지_않는다()
        {
            MetaProfile winner = new MetaProfile();
            RelicDropResult win = Roll(winner, "Knight", 0, true, 0.999f, 0f, 0f);
            MetaProfile loser = new MetaProfile();
            RelicDropResult lose = Roll(loser, "Knight", 0, false, 0.5f);

            Assert.IsTrue(win.Acquired);
            Assert.IsTrue(win.IsNew);
            Assert.AreEqual(1, StackSum(winner));
            Assert.AreEqual(ERelicDropOutcome.Missed, lose.Outcome);
            Assert.AreEqual(0, loser.Relics.Count);
            Assert.AreEqual(0, loser.RelicMissStreak, "패배 미획득은 카운터 불변");
        }

        [Test]
        public void 첫_유물_보장도_등급과_유물은_평소대로_굴린다()
        {
            MetaProfile p = new MetaProfile();

            RelicDropResult result = Roll(p, "Knight", 0, true, 0.5f, 0.96875f, 0f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual(ERelicRarity.Legendary, _db.Get(result.RelicId).Rarity, "E-12: 첫 보장도 전설 가능");
        }

        //# --- 후보 / NoCandidates ---

        [Test]
        public void 후보가_0개면_난수를_쓰지_않고_프로필을_바꾸지_않는다()
        {
            MetaProfile p = new MetaProfile { RelicMissStreak = 2 };
            foreach (Relic_DropRow drop in _db.DropsOf("Archer"))
            {
                for (int i = 0; i < _db.Get(drop.relicId).MaxStack; ++i)
                {
                    p.AddRelic(drop.relicId);
                }
            }
            int before = StackSum(p);
            ScriptedRng rng = new ScriptedRng();

            RelicDropResult win = RelicDropRoller.Roll(_db, p, "Archer", 2, 1, 0, true, rng.Next);
            RelicDropResult lose = RelicDropRoller.Roll(_db, p, "Archer", 2, 1, 0, false, rng.Next);

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, win.Outcome);
            Assert.AreEqual(ERelicDropOutcome.NoCandidates, lose.Outcome);
            Assert.AreEqual(0, rng.Calls, "굴림 생략");
            Assert.AreEqual(2, p.RelicMissStreak);
            Assert.AreEqual(before, StackSum(p));
            Assert.IsFalse(win.ShowNextChance, "후보 전부 보유 시 결과창 줄 없음");
        }

        [Test]
        public void 후보가_1개만_남으면_그_유물만_나온다()
        {
            MetaProfile p = new MetaProfile();
            foreach (Relic_DropRow drop in _db.DropsOf("Paladin"))
            {
                if (drop.relicId == "KingsCrownShard")
                    continue;
                for (int i = 0; i < _db.Get(drop.relicId).MaxStack; ++i)
                {
                    p.AddRelic(drop.relicId);
                }
            }

            RelicDropResult result = Roll(p, "Paladin", 0, true, 0f, 0.999f, 0.999f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual("KingsCrownShard", result.RelicId);
            Assert.IsTrue(result.IsNew);
            Assert.AreEqual(1, result.StackAfter);
            Assert.AreEqual(1, result.MaxStack);
        }

        [Test]
        public void 알_수_없는_영웅은_후보가_없어_굴림을_생략한다()
        {
            MetaProfile p = Veteran(2);
            ScriptedRng rng = new ScriptedRng();

            RelicDropResult result = RelicDropRoller.Roll(_db, p, "Nobody", 1, 1, 0, true, rng.Next);

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, result.Outcome);
            Assert.AreEqual(0, rng.Calls);
            Assert.AreEqual(2, p.RelicMissStreak);
        }

        [Test]
        public void db_프로필_난수원이_null이면_NoCandidates를_돌려주고_예외가_없다()
        {
            MetaProfile p = Veteran();
            Func<float> rng = () => 0f;

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, RelicDropRoller.Roll(null, p, "Knight", 1, 1, 0, true, rng).Outcome);
            Assert.AreEqual(ERelicDropOutcome.NoCandidates, RelicDropRoller.Roll(_db, null, "Knight", 1, 1, 0, true, rng).Outcome);
            Assert.AreEqual(ERelicDropOutcome.NoCandidates, RelicDropRoller.Roll(_db, p, "Knight", 1, 1, 0, true, null).Outcome);
        }

        [Test]
        public void 최대_중첩에_도달한_유물은_후보에서_빠진다()
        {
            MetaProfile p = Veteran(0, "SoulPurse");
            for (int i = 0; i < 5; ++i)
            {
                p.AddRelic("HeartOfStone");
            }

            RelicDropResult result = Roll(p, "Knight", 0, true, 0f, 0f, 0f);

            Assert.AreEqual("RustyBlade", result.RelicId, "HeartOfStone(5/5)을 건너뛴 다음 후보");
            Assert.AreEqual(5, p.GetRelicStack("HeartOfStone"));
        }

        [Test]
        public void 등급_후보가_모두_사라지면_남은_등급으로_재정규화한다()
        {
            //# 기사의 Legendary 후보는 KingsCrownShard 하나 — 최대 중첩이면 rng 0.99 가 Legendary 가 아니라 Rare 가 돼야 한다.
            MetaProfile p = Veteran(0, "SoulPurse");
            p.AddRelic("KingsCrownShard");

            RelicDropResult result = Roll(p, "Knight", 0, true, 0f, 0.99f, 0f);

            Assert.IsTrue(result.Acquired);
            Assert.AreEqual(ERelicRarity.Rare, _db.Get(result.RelicId).Rarity);
            Assert.AreEqual("BrokenHourglass", result.RelicId, "Rare 풀의 첫 후보(weight 이진 경계 0)");
        }

        [Test]
        public void 영웅_테이블에_없는_유물은_다른_영웅_테이블의_전설이어도_나오지_않는다()
        {
            //# 기사는 WrathSpirit 행이 없고, 궁수는 KingsCrownShard 행이 없다.
            MetaProfile knight = Veteran();
            MetaProfile archer = Veteran();

            RelicDropResult k = Roll(knight, "Knight", 0, true, 0f, 0.96875f, 0.999f);
            RelicDropResult a = Roll(archer, "Archer", 0, true, 0f, 0.96875f, 0.999f);

            Assert.AreEqual("KingsCrownShard", k.RelicId);
            Assert.AreEqual("WrathSpirit", a.RelicId);
        }

        //# --- 2단계 추첨: 등급 → 유물 ---

        private ERelicRarity RarityOf(MetaProfile p, string hero, int diff, float rarityDraw)
        {
            RelicDropResult r = Roll(p, hero, diff, true, 0f, rarityDraw, 0f);
            Assert.IsTrue(r.Acquired);
            return _db.Get(r.RelicId).Rarity;
        }

        [Test]
        public void 등급_경계_Easy는_70_25_5로_나뉜다()
        {
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 0, 0.5f), "50 < 70");
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 0, 0.6875f), "68.75 < 70");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 0, 0.75f), "70 <= 75 < 95");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 0, 0.9375f), "93.75 < 95");
            Assert.AreEqual(ERelicRarity.Legendary, RarityOf(Veteran(), "Knight", 0, 0.96875f), "96.875 >= 95");
        }

        [Test]
        public void 등급_경계_Normal은_56_35_10이라_Easy보다_Rare가_넓다()
        {
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 1, 0.5f), "50.5 < 56");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 1, 0.5625f), "56.8 >= 56");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 1, 0.75f), "75.75 < 91");
            Assert.AreEqual(ERelicRarity.Legendary, RarityOf(Veteran(), "Knight", 1, 0.96875f), "97.8 >= 91");
        }

        [Test]
        public void 등급_경계_Hard는_42_45_20이라_같은_rng도_더_높은_등급이_나온다()
        {
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 0, 0.4375f), "Easy 43.75 < 70");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 2, 0.4375f), "Hard 46.8 >= 42");
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 2, 0.375f), "40.1 < 42");
            Assert.AreEqual(ERelicRarity.Rare, RarityOf(Veteran(), "Knight", 2, 0.8125f), "86.9 < 87");
            Assert.AreEqual(ERelicRarity.Legendary, RarityOf(Veteran(), "Knight", 2, 0.84375f), "90.3 >= 87");
        }

        [Test]
        public void 등급_굴림_난수_0은_Common_1에_가까운_값은_Legendary다()
        {
            Assert.AreEqual(ERelicRarity.Common, RarityOf(Veteran(), "Knight", 0, 0f));
            Assert.AreEqual(ERelicRarity.Legendary, RarityOf(Veteran(), "Knight", 0, 0.99999f));
        }

        private int[] RarityCounts(string hero, int diff, int samples, MetaProfile baseProfile)
        {
            int[] counts = new int[3];
            for (int i = 0; i < samples; ++i)
            {
                MetaProfile p = Clone(baseProfile);
                float rarityDraw = (i + 0.5f) / samples;
                RelicDropResult r = Roll(p, hero, diff, true, 0f, rarityDraw, 0f);
                counts[(int)_db.Get(r.RelicId).Rarity]++;
            }
            return counts;
        }

        private static MetaProfile Clone(MetaProfile src)
        {
            MetaProfile copy = new MetaProfile { RelicMissStreak = src.RelicMissStreak };
            foreach (RelicEntry entry in src.Relics)
            {
                copy.Relics.Add(new RelicEntry { RelicId = entry.RelicId, Stack = entry.Stack });
            }
            return copy;
        }

        [Test]
        public void 등급_분포_격자_Easy는_70_25_5퍼센트다()
        {
            int[] c = RarityCounts("Knight", 0, 1000, Veteran());

            Assert.AreEqual(700, c[0], 3);
            Assert.AreEqual(250, c[1], 3);
            Assert.AreEqual(50, c[2], 3);
        }

        [Test]
        public void 등급_분포_격자_Normal은_55_4_34_7_9_9퍼센트다()
        {
            int[] c = RarityCounts("Knight", 1, 1000, Veteran());

            Assert.AreEqual(554.5f, c[0], 3f);
            Assert.AreEqual(346.5f, c[1], 3f);
            Assert.AreEqual(99.0f, c[2], 3f);
        }

        [Test]
        public void 등급_분포_격자_Hard는_39_3_42_1_18_7퍼센트다()
        {
            int[] c = RarityCounts("Knight", 2, 1000, Veteran());

            Assert.AreEqual(392.5f, c[0], 3f);
            Assert.AreEqual(420.6f, c[1], 3f);
            Assert.AreEqual(186.9f, c[2], 3f);
        }

        [Test]
        public void 등급_분포는_난이도가_오를수록_Common이_줄고_Legendary가_는다()
        {
            int[] easy = RarityCounts("Archer", 0, 500, Veteran());
            int[] normal = RarityCounts("Archer", 1, 500, Veteran());
            int[] hard = RarityCounts("Archer", 2, 500, Veteran());

            Assert.Greater(easy[0], normal[0]);
            Assert.Greater(normal[0], hard[0]);
            Assert.Less(easy[2], normal[2]);
            Assert.Less(normal[2], hard[2]);
        }

        [Test]
        public void 전설_후보가_없으면_분포가_Common_Rare_70_25로_재정규화된다()
        {
            MetaProfile maxedLegend = Veteran();
            maxedLegend.AddRelic("KingsCrownShard");

            int[] c = RarityCounts("Knight", 0, 1000, maxedLegend);

            Assert.AreEqual(0, c[2]);
            Assert.AreEqual(1000f * 70f / 95f, c[0], 3f);
            Assert.AreEqual(1000f * 25f / 95f, c[1], 3f);
        }

        [Test]
        public void 등급_안에서_유물은_weight_비례_구간으로_선택된다()
        {
            //# 기사 Common 후보를 JSON 순서대로 누적 weight 구간으로 쪼개 각 구간 중앙 난수가 그 유물을 고르는지 확인한다.
            List<Relic_DropRow> commons = new List<Relic_DropRow>();
            float total = 0f;
            foreach (Relic_DropRow drop in _db.DropsOf("Knight"))
            {
                if (_db.Get(drop.relicId).Rarity == ERelicRarity.Common)
                {
                    commons.Add(drop);
                    total += drop.weight;
                }
            }
            float cumulative = 0f;

            foreach (Relic_DropRow expected in commons)
            {
                float mid = (cumulative + expected.weight * 0.5f) / total;
                RelicDropResult result = Roll(Veteran(0, "ZZZ_NotInDb"), "Knight", 0, true, 0f, 0f, mid);
                Assert.AreEqual(expected.relicId, result.RelicId, $"구간 중앙 {mid}");
                cumulative += expected.weight;
            }
            Assert.AreEqual(25f, total, "기사 Common weight 합 6+2+2+3+4+3+3+2");
        }

        [Test]
        public void 유물_weight_경계_정확히_누적_경계값이면_다음_후보로_넘어간다()
        {
            //# 광전사 Legendary: KingsCrownShard 5 / WrathSpirit 5 — 합 10, 난수 0.5(이진 정확)는 두 번째 구간의 시작.
            RelicDropResult first = Roll(Veteran(), "Berserker", 0, true, 0f, 0.96875f, 0.49f);
            RelicDropResult boundary = Roll(Veteran(), "Berserker", 0, true, 0f, 0.96875f, 0.5f);
            RelicDropResult last = Roll(Veteran(), "Berserker", 0, true, 0f, 0.96875f, 0.999f);

            Assert.AreEqual("KingsCrownShard", first.RelicId);
            Assert.AreEqual("WrathSpirit", boundary.RelicId);
            Assert.AreEqual("WrathSpirit", last.RelicId);
        }

        [Test]
        public void 유물_분포_격자는_weight_비율과_일치한다()
        {
            Dictionary<string, int> counts = new Dictionary<string, int>();
            int samples = 2500;
            for (int i = 0; i < samples; ++i)
            {
                RelicDropResult r = Roll(Veteran(0, "ZZZ_NotInDb"), "Knight", 0, true, 0f, 0f, (i + 0.5f) / samples);
                counts[r.RelicId] = counts.TryGetValue(r.RelicId, out int n) ? n + 1 : 1;
            }

            Assert.AreEqual(600, counts["HeartOfStone"], 2, "6/25");
            Assert.AreEqual(200, counts["RustyBlade"], 2, "2/25");
            Assert.AreEqual(300, counts["EmberBrand"], 2, "3/25");
            Assert.AreEqual(400, counts["AmbushHorn"], 2, "4/25");
        }

        [Test]
        public void 영웅별_테이블이_결과_분포에_반영된다()
        {
            //# HeartOfStone 은 기사 Common 6/25(24%), 궁수 2/25(8%) — 영웅 테이블 가중치가 분포에 반영된다.
            int knightHeart = CountOf("Knight", "HeartOfStone", 1000);
            int archerHeart = CountOf("Archer", "HeartOfStone", 1000);

            Assert.Greater(knightHeart, archerHeart);
        }

        private int CountOf(string hero, string relicId, int samples)
        {
            int count = 0;
            for (int i = 0; i < samples; ++i)
            {
                RelicDropResult r = Roll(Veteran(0, "ZZZ_NotInDb"), hero, 0, true, 0f, 0f, (i + 0.5f) / samples);
                count += r.RelicId == relicId ? 1 : 0;
            }
            return count;
        }

        //# --- 획득 결과 필드 / 판당 1개 ---

        [Test]
        public void 신규_획득은_IsNew_중복_획득은_중첩_증가와_MaxStack을_알려준다()
        {
            MetaProfile p = Veteran(0, "HeartOfStone");

            RelicDropResult dup = Roll(p, "Knight", 0, true, 0f, 0f, 0f);
            MetaProfile q = Veteran(0, "SoulPurse");
            RelicDropResult fresh = Roll(q, "Knight", 0, true, 0f, 0f, 0f);

            Assert.AreEqual("HeartOfStone", dup.RelicId);
            Assert.IsFalse(dup.IsNew);
            Assert.AreEqual(2, dup.StackAfter);
            Assert.AreEqual(5, dup.MaxStack);
            Assert.AreEqual(2, p.GetRelicStack("HeartOfStone"));
            Assert.IsTrue(fresh.IsNew);
            Assert.AreEqual(1, fresh.StackAfter);
            Assert.AreEqual(1, q.GetRelicStack("HeartOfStone"));
        }

        [Test]
        public void 한_판에_유물은_최대_1개만_늘고_난수는_정확히_3번_쓴다()
        {
            MetaProfile p = Veteran();
            int before = StackSum(p);
            ScriptedRng rng = new ScriptedRng(0f, 0f, 0f);

            RelicDropRoller.Roll(_db, p, "Knight", 1, 1, 0, true, rng.Next);

            Assert.AreEqual(before + 1, StackSum(p));
            Assert.AreEqual(3, rng.Calls);
        }

        [Test]
        public void 미획득은_난수를_1번만_쓴다()
        {
            ScriptedRng rng = new ScriptedRng(0.99f);

            RelicDropRoller.Roll(_db, Veteran(), "Knight", 1, 1, 0, true, rng.Next);

            Assert.AreEqual(1, rng.Calls);
        }

        //# --- 재현성 / 불변식 ---

        [Test]
        public void 같은_시드는_같은_결과_수열을_만든다()
        {
            List<string> first = Simulate(777, 200);
            List<string> second = Simulate(777, 200);

            CollectionAssert.AreEqual(first, second);
        }

        private List<string> Simulate(int seed, int rolls)
        {
            Random random = new Random(seed);
            MetaProfile p = new MetaProfile();
            List<string> log = new List<string>();
            for (int i = 0; i < rolls; ++i)
            {
                bool win = i % 5 != 4;
                RelicDropResult r = RelicDropRoller.Roll(_db, p, "Berserker", 1 + i % 5, 1 + i % 5, i % 3, win, () => (float)random.NextDouble());
                log.Add($"{r.Outcome}:{r.RelicId}:{r.StackAfter}:{p.RelicMissStreak}");
            }
            return log;
        }

        [Test]
        public void 승리만_반복하면_영웅_테이블을_전부_채우고_어떤_유물도_최대_중첩을_넘지_않는다()
        {
            Random random = new Random(20261019);
            MetaProfile p = new MetaProfile();
            RelicDropResult last = default;

            int guard = 0;
            while (guard++ < 5000)
            {
                last = RelicDropRoller.Roll(_db, p, "Knight", 1, 1, 0, true, () => (float)random.NextDouble());
                if (last.Outcome == ERelicDropOutcome.NoCandidates)
                    break;
            }

            Assert.AreEqual(ERelicDropOutcome.NoCandidates, last.Outcome, "5000판 안에 테이블이 가득 차야 함");
            foreach (Relic_DropRow drop in _db.DropsOf("Knight"))
            {
                Assert.AreEqual(_db.Get(drop.relicId).MaxStack, p.GetRelicStack(drop.relicId), drop.relicId);
            }
            foreach (RelicEntry entry in p.Relics)
            {
                Assert.LessOrEqual(entry.Stack, _db.Get(entry.RelicId).MaxStack, entry.RelicId);
            }
        }

        [Test]
        public void 승패_난이도_스테이지를_섞어도_연패_카운터는_음수가_되지_않는다()
        {
            Random random = new Random(5);
            MetaProfile p = new MetaProfile();

            for (int i = 0; i < 400; ++i)
            {
                RelicDropRoller.Roll(_db, p, "Assassin", 1 + i % 5, 1 + i % 5, i % 3, i % 3 != 0, () => (float)random.NextDouble());
                Assert.GreaterOrEqual(p.RelicMissStreak, 0);
            }
        }
    }
}
