using System;
using Lair.Data;
using Lair.Meta;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 세이브 · 클라우드 복원 본격 스위트 (monster-growth-currencies §7) — CopyFrom 재화 복사, v11 → v12 로드 왕복, §7.3 정리 규칙, 잔액 헬퍼 불변식.
    public class GrowthProfileSuiteTests : ChapterStaticStateTestBase
    {
        //# ===== CopyFrom (클라우드 복원) =====

        [Test]
        public void CopyFrom_은_특수_재화를_독립_인스턴스로_복사해_원본_변경이_새지_않는다()
        {
            MetaProfile cloud = new MetaProfile();
            cloud.AddCurrency(ECurrency.AwakenStone, 12);
            cloud.AddCurrency(ECurrency.RelicShard, 5);
            cloud.TrySpendCurrency(ECurrency.RelicShard, 3);
            MetaProfile local = new MetaProfile();

            local.CopyFrom(cloud);
            cloud.AddCurrency(ECurrency.AwakenStone, 100);
            cloud.Currencies.Clear();

            Assert.AreEqual(12, local.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(2, local.GetCurrency(ECurrency.RelicShard));
            Assert.AreEqual(5, local.GetCurrencyTotalEarned(ECurrency.RelicShard), "누적 획득도 복사 - 잔해 해금 유지");
        }

        [Test]
        public void CopyFrom_은_로컬_재화를_클라우드_재화로_전체_대체한다()
        {
            MetaProfile cloud = new MetaProfile();
            cloud.AddCurrency(ECurrency.MoonSeal, 3);
            MetaProfile local = new MetaProfile();
            local.AddCurrency(ECurrency.AwakenStone, 50);

            local.CopyFrom(cloud);

            Assert.AreEqual(0, local.GetCurrency(ECurrency.AwakenStone), "병합 아님 - 전체 대체 (§7.4)");
            Assert.AreEqual(3, local.GetCurrency(ECurrency.MoonSeal));
        }

        [Test]
        public void CopyFrom_원본_재화_리스트가_null_이면_빈_리스트가_된다()
        {
            MetaProfile cloud = new MetaProfile { Currencies = null };
            MetaProfile local = new MetaProfile();
            local.AddCurrency(ECurrency.AwakenStone, 4);

            local.CopyFrom(cloud);

            Assert.IsNotNull(local.Currencies);
            Assert.AreEqual(0, local.Currencies.Count);
        }

        [Test]
        public void CopyFrom_은_각성석_지급_기록과_탑_결정_지급_기록을_보존해_재지급이_없다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile cloud = new MetaProfile { ClearedStage = 1 };
            cloud.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 2, AwakenPaid = 2 });
            cloud.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 3, ClearTime = 20f, CrystalPaid = true });
            MetaProfile local = new MetaProfile();

            local.CopyFrom(cloud);
            cloud.TowerFloorClears[0].CrystalPaid = false;

            Assert.AreEqual(2, local.GetAwakenPaid(1));
            Assert.IsTrue(local.TowerFloorClears[0].CrystalPaid, "탑 행은 새 인스턴스로 복사");
            Assert.AreEqual(0, CurrencySettlement.SettleAwakenFirstClears(local, growth), "복원 후 마을 진입 소급이 각성석을 다시 주지 않는다");
        }

        //# ===== v11 → v12 로드 왕복 =====

        private const string V11Json = "{\"Version\":11,\"Souls\":321,\"ClearedStage\":3,\"SelectedStage\":2," +
            "\"DifficultyClears\":[{\"Stage\":1,\"MaxDifficulty\":2},{\"Stage\":2,\"MaxDifficulty\":1}]," +
            "\"TowerSeason\":1,\"TowerFloorClears\":[{\"Floor\":1,\"ClearTime\":40.5}]}";

        [Test]
        public void v11_세이브를_읽어_마이그레이션하면_Version_12_빈_재화_지급기록_기본값이다()
        {
            MetaProfile p = JsonUtility.FromJson<MetaProfile>(V11Json);

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(13, p.Version);
            Assert.AreEqual(321, p.Souls, "소울은 Souls 그대로 - 재화 리스트로 옮기지 않는다");
            Assert.IsNotNull(p.Currencies);
            Assert.AreEqual(0, p.Currencies.Count);
            Assert.AreEqual(0, p.GetAwakenPaid(1));
            Assert.AreEqual(0, p.GetAwakenPaid(2));
            Assert.IsFalse(p.TowerFloorClears[0].CrystalPaid, "필드 부재 = false → 정산 함수가 소급");
            Assert.AreEqual(0, p.GetCurrency(ECurrency.AwakenStone), "마이그레이션은 소급 지급을 하지 않는다");
        }

        [Test]
        public void v11_에서_올린_세이브를_소급_정산_후_저장_로드해도_재화와_지급기록이_유지된다()
        {
            GrowthDatabase growth = GrowthTestKit.Shipped();
            MetaProfile p = JsonUtility.FromJson<MetaProfile>(V11Json);
            MetaProfileMigration.Apply(p, (Func<int, string>)null);
            int paid = CurrencySettlement.SettleAwakenFirstClears(p, growth);

            MetaProfile reloaded = JsonUtility.FromJson<MetaProfile>(JsonUtility.ToJson(p));
            MetaProfileMigration.Apply(reloaded, (Func<int, string>)null);

            Assert.AreEqual(45, paid, "1-1 Normal+Hard 35 + 1-2 Normal 10");
            Assert.AreEqual(45, reloaded.GetCurrency(ECurrency.AwakenStone));
            Assert.AreEqual(45, reloaded.GetCurrencyTotalEarned(ECurrency.AwakenStone));
            Assert.AreEqual(2, reloaded.GetAwakenPaid(1));
            Assert.AreEqual(1, reloaded.GetAwakenPaid(2));
            Assert.AreEqual(0, CurrencySettlement.SettleAwakenFirstClears(reloaded, growth), "재로드 후 재지급 없음");
        }

        //# ===== §7.3 정리 규칙 =====

        [Test]
        public void 마이그레이션은_AwakenPaid_를_0_이상_MaxDifficulty_이하로_자른다()
        {
            MetaProfile p = new MetaProfile { Version = 11, ClearedStage = 3 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 1, AwakenPaid = 5 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 2, MaxDifficulty = 2, AwakenPaid = -1 });

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(1, p.GetAwakenPaid(1));
            Assert.AreEqual(0, p.GetAwakenPaid(2));
        }

        [Test]
        public void 같은_스테이지_중복_행_병합은_AwakenPaid_최댓값을_남겨_재지급을_만들지_않는다()
        {
            MetaProfile p = new MetaProfile { Version = 11, ClearedStage = 3 };
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 2, AwakenPaid = 2 });
            p.DifficultyClears.Add(new DifficultyClearEntry { Stage = 1, MaxDifficulty = 1, AwakenPaid = 0 });

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(1, p.DifficultyClears.Count);
            Assert.AreEqual(2, p.GetAwakenPaid(1));
        }

        [Test]
        public void 같은_층_중복_행_병합은_한쪽이라도_지급됐으면_지급된_층으로_남는다()
        {
            MetaProfile p = new MetaProfile { Version = 11 };
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 4, ClearTime = 50f, CrystalPaid = false });
            p.TowerFloorClears.Add(new TowerFloorClearEntry { Floor = 4, ClearTime = 30f, CrystalPaid = true });

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(1, p.TowerFloorClears.Count);
            Assert.IsTrue(p.TowerFloorClears[0].CrystalPaid);
        }

        [Test]
        public void ECurrency_에_없는_재화_id_는_지우지_않고_조회에서_무시한다()
        {
            MetaProfile p = new MetaProfile { Version = 11 };
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "FutureGem", Amount = 9, TotalEarned = 9 });
            p.Currencies.Add(new CurrencyEntry { CurrencyId = string.Empty, Amount = 1, TotalEarned = 1 });
            p.Currencies.Add(null);

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(1, p.Currencies.Count, "빈 id·null 행만 제거");
            Assert.AreEqual("FutureGem", p.Currencies[0].CurrencyId);
            foreach (ECurrency c in (ECurrency[])Enum.GetValues(typeof(ECurrency)))
            {
                if (c != ECurrency.Soul)
                {
                    Assert.AreEqual(0, p.GetCurrency(c), c.ToString());
                }
            }
        }

        [Test]
        public void 잔액만_양수이고_누적이_모자란_행은_누적이_잔액으로_올라간다()
        {
            MetaProfile p = new MetaProfile { Version = 11 };
            p.Currencies.Add(new CurrencyEntry { CurrencyId = "MoonSeal", Amount = 6, TotalEarned = 0 });

            MetaProfileMigration.Apply(p, (Func<int, string>)null);

            Assert.AreEqual(6, p.GetCurrency(ECurrency.MoonSeal));
            Assert.AreEqual(6, p.GetCurrencyTotalEarned(ECurrency.MoonSeal));
        }

        //# ===== 잔액 헬퍼 =====

        [Test]
        public void 재화_차감은_모자라면_아무것도_바꾸지_않고_누적_획득은_차감해도_줄지_않는다()
        {
            MetaProfile p = new MetaProfile { Souls = 10 };
            p.AddCurrency(ECurrency.MoonSeal, 5);

            bool over = p.TrySpendCurrency(ECurrency.MoonSeal, 6);
            bool exact = p.TrySpendCurrency(ECurrency.MoonSeal, 5);
            bool soulOver = p.TrySpendCurrency(ECurrency.Soul, 11);
            bool missing = p.TrySpendCurrency(ECurrency.AbyssCrystal, 1);

            Assert.IsFalse(over);
            Assert.IsTrue(exact);
            Assert.IsFalse(soulOver);
            Assert.IsFalse(missing, "행 없는 재화 차감 실패");
            Assert.AreEqual(0, p.GetCurrency(ECurrency.MoonSeal));
            Assert.AreEqual(5, p.GetCurrencyTotalEarned(ECurrency.MoonSeal));
            Assert.AreEqual(10, p.Souls);
        }

        [Test]
        public void 재화_가산은_0_이하를_무시하고_소울은_Souls_로_가며_행을_만들지_않는다()
        {
            MetaProfile p = new MetaProfile();

            p.AddCurrency(ECurrency.AwakenStone, 0);
            p.AddCurrency(ECurrency.AwakenStone, -4);
            p.AddCurrency(ECurrency.Soul, 7);

            Assert.AreEqual(0, p.Currencies.Count, "Soul 행 금지 · 0 획득은 행을 만들지 않음 (TotalEarned >= 1 불변식)");
            Assert.AreEqual(7, p.Souls);
            Assert.AreEqual(0, p.GetCurrencyTotalEarned(ECurrency.Soul));
        }
    }
}
