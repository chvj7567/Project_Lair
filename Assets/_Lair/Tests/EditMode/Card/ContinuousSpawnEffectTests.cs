using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Lair.Card;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.Card
{
    //# 지속 스폰 — 재작성된 카드 효과(강화/추가소환/융합)가 IBattleContext 의
    //# 올바른 신규 API 를 올바른 인자로 호출하는지 검증.
    public class ContinuousSpawnEffectTests
    {
        //# 최소 IBattleContext 더블 — 신규 3종 API 호출만 기록.
        private class FakeBattleContext : IBattleContext
        {
            //# 끈질긴 저주·Debuff Tier1 호출 기록 (monster-growth-currencies §15).
            public readonly System.Collections.Generic.List<float> DebuffDurationBonuses = new System.Collections.Generic.List<float>();
            public readonly System.Collections.Generic.List<int> ToxinMaxStackBonuses = new System.Collections.Generic.List<int>();
            public void AddHeroDebuffDurationBonus(float bonus) => DebuffDurationBonuses.Add(bonus);
            public void AddPlagueToxinMaxStack(int bonus) => ToxinMaxStackBonuses.Add(bonus);
            public readonly List<(EMonster, EMonsterStatKind, float)> Buffs = new();
            public readonly List<EMonster> Increments = new();
            public readonly List<(EMonster, EMonster)> Replaces = new();

            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier)
                => Buffs.Add((type, stat, multiplier));
            public void IncrementSpawnerOutput(EMonster type) => Increments.Add(type);
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) => Replaces.Add((from, to));

            //# 본 테스트에서 미사용 — 인터페이스 충족용 no-op.
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => new List<IHealth>();
            public IHealth GetHero() => null;
            public Transform GetHeroTransform() => null;
            public IMover GetHeroMover() => null;
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            //# 카드 리뉴얼 v0.6 — IBattleContext 신규 표면 (Phase 1 Task 4). 본 테스트 미사용 stub.
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
            public void ScaleSpawnerPeriodForType(EMonster type, float mul) { }
            public float DeltaTime => 0f;
        }

        //# 정상 — 강화 카드는 해당 종·스탯·배율로 RegisterMonsterTypeBuff 1회 호출.
        [Test]
        public void WispHpBoost_위스프_Hp_배율_등록()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            CardEffectTestKit.Make<WispHpBoostEffect>().Apply(ctx);

            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual(EMonster.Wisp, ctx.Buffs[0].Item1);
            Assert.AreEqual(EMonsterStatKind.Hp, ctx.Buffs[0].Item2);
            Assert.AreEqual(1.5f, ctx.Buffs[0].Item3, 0.0001f);
        }

        //# 정상 — 끈질긴 저주는 픽마다 영웅 디버프 지속 +0.25 를 등록하고 스탯 배율은 건드리지 않는다 (§15.3).
        [Test]
        public void LingeringCurse_픽마다_디버프_지속_보너스_0점25_등록()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            CardEffectTestKit.Make<LingeringCurseEffect>().Apply(ctx);
            CardEffectTestKit.Make<LingeringCurseEffect>().Apply(ctx);

            CollectionAssert.AreEqual(new[] { 0.25f, 0.25f }, ctx.DebuffDurationBonuses);
            Assert.AreEqual(0, ctx.Buffs.Count);
        }

        //# 정상 — 역병의 손길은 역병귀 독 피해 배율 1.4 로 등록 (monster-growth-currencies §15.0).
        [Test]
        public void PlagueSlowBoost_역병귀_독피해_배율_1점4_등록()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            CardEffectTestKit.Make<PlagueToxinBoostEffect>().Apply(ctx);

            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual(EMonster.Plague, ctx.Buffs[0].Item1);
            Assert.AreEqual(EMonsterStatKind.PoisonDamage, ctx.Buffs[0].Item2);
            Assert.AreEqual(1.4f, ctx.Buffs[0].Item3, 0.0001f);
        }

        //# 정상 — 추가소환 카드는 해당 종으로 IncrementSpawnerOutput 1회 호출.
        [Test]
        public void SpawnWisps_위스프_출력증가_호출()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            new SpawnWispsEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Increments.Count);
            Assert.AreEqual(EMonster.Wisp, ctx.Increments[0]);
        }

        //# 카드 리뉴얼 v0.6 patch — 융합 카드 2장 (ReplaceWispsToWraith/ReplaceReapersToHex) 폐기.
        //# 효과가 WispWraithPowerBoost/ReaperHexPowerBoost 로 교체되어 ReplaceSpawnerOutput 동작 검증 의미 사라짐.

        //# 신규 — 공포의 군세: 위스프·레이스 Power ×1.3 RegisterMonsterTypeBuff 2회 호출.
        [Test]
        public void WispWraithPowerBoost_Apply_RegisterMonsterTypeBuff_Wisp_Wraith_Power_2회_호출()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            CardEffectTestKit.Make<WispWraithPowerBoostEffect>().Apply(ctx);

            Assert.AreEqual(2, ctx.Buffs.Count, "위스프+레이스 2회 호출");
            Assert.IsTrue(ctx.Buffs.Contains((EMonster.Wisp,   EMonsterStatKind.Power, 1.3f)));
            Assert.IsTrue(ctx.Buffs.Contains((EMonster.Wraith, EMonsterStatKind.Power, 1.3f)));
        }

        //# 신규 — 처형 명령: 리퍼·헥스 Power ×1.3.
        [Test]
        public void ReaperHexPowerBoost_Apply_RegisterMonsterTypeBuff_Reaper_Hex_Power_2회_호출()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            CardEffectTestKit.Make<ReaperHexPowerBoostEffect>().Apply(ctx);

            Assert.AreEqual(2, ctx.Buffs.Count, "리퍼+헥스 2회 호출");
            Assert.IsTrue(ctx.Buffs.Contains((EMonster.Reaper, EMonsterStatKind.Power, 1.3f)));
            Assert.IsTrue(ctx.Buffs.Contains((EMonster.Hex,    EMonsterStatKind.Power, 1.3f)));
        }
    }
}
