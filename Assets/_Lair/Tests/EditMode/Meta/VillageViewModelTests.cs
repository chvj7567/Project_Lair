using Lair.Data;
using Lair.Meta;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    public class VillageViewModelTests
    {
        [Test]
        public void 소울_변경시_이벤트가_발화한다()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            VillageViewModel vm = new VillageViewModel(new MetaProfile { Souls = 10 }, cfg);
            int seen = -1;
            vm.OnSoulsChanged += s => seen = s;
            vm.NotifyProfileChanged();
            Assert.AreEqual(10, seen);

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void 영주_레벨과_진행률을_노출한다()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.XpPerLevelBase = 100;
            cfg.XpGrowth = 1.25f;
            VillageViewModel vm = new VillageViewModel(new MetaProfile { LordXp = 100 }, cfg);
            Assert.AreEqual(2, vm.LordLevel);
            Assert.AreEqual(0f, vm.LordProgress, 0.01f);

            Object.DestroyImmediate(cfg);
        }

        [Test]
        public void 트랙_만렙_도달시_영주_게이지가_1로_고정된다()
        {
            MetaConfig cfg = ScriptableObject.CreateInstance<MetaConfig>();
            cfg.XpPerLevelBase = 100;
            cfg.XpGrowth = 1.25f;
            cfg.LordRewards.Add(new LordRewardDef { Level = 2, IsLockedDummy = false, RewardSouls = 50, DisplayName = "영주의 첫 봉급" });
            cfg.LordRewards.Add(new LordRewardDef { Level = 3, IsLockedDummy = false, RewardSouls = 80, DisplayName = "던전 운영비" });

            //# 트랙 만렙 Lv3 도달(XP 225) — 게이지 1f 고정 (기획서 §10.7).
            VillageViewModel vm = new VillageViewModel(new MetaProfile { LordXp = 225 }, cfg);
            Assert.AreEqual(1f, vm.LordProgress);

            Object.DestroyImmediate(cfg);
        }
    }
}
