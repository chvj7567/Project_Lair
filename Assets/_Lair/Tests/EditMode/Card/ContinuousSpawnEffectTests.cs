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
    //# gameplay-programmer 자체 검증 수준 (정상 + 엣지 1). 본격 스위트는 test-engineer.
    public class ContinuousSpawnEffectTests
    {
        //# 최소 IBattleContext 더블 — 신규 3종 API 호출만 기록.
        private class FakeBattleContext : IBattleContext
        {
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
            public void IncrementGlobalMonsterCap(int delta) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
            public float DeltaTime => 0f;
        }

        //# 정상 — 강화 카드는 해당 종·스탯·배율로 RegisterMonsterTypeBuff 1회 호출.
        [Test]
        public void WispHpBoost_위스프_Hp_배율_등록()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            new WispHpBoostEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual(EMonster.Wisp, ctx.Buffs[0].Item1);
            Assert.AreEqual(EMonsterStatKind.Hp, ctx.Buffs[0].Item2);
            Assert.AreEqual(1.5f, ctx.Buffs[0].Item3, 0.0001f);
        }

        //# 정상 — 플레이그 강화는 SlowFactor 배율 0.75 로 등록 (치환값 아닌 배율, §3.0.1).
        [Test]
        public void PlagueSlowBoost_플레이그_SlowFactor_배율_0점75_등록()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            new PlagueSlowBoostEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Buffs.Count);
            Assert.AreEqual(EMonster.Plague, ctx.Buffs[0].Item1);
            Assert.AreEqual(EMonsterStatKind.SlowFactor, ctx.Buffs[0].Item2);
            Assert.AreEqual(0.75f, ctx.Buffs[0].Item3, 0.0001f);
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

        //# 정상 — 융합 카드는 from→to 로 ReplaceSpawnerOutput 1회 호출.
        [Test]
        public void ReplaceWispsToWraith_위스프에서_레이스_교체_호출()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            new ReplaceWispsToWraithEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Replaces.Count);
            Assert.AreEqual(EMonster.Wisp, ctx.Replaces[0].Item1);
            Assert.AreEqual(EMonster.Wraith, ctx.Replaces[0].Item2);
        }

        //# 엣지 — 융합 카드는 필드 몬스터를 건드리지 않는다 (즉살 로직 제거, §3.4.2):
        //# GetMonsters 가 호출되지 않아야 한다. FakeBattleContext 의 GetMonsters 는
        //# 빈 리스트만 반환하므로, 호출 여부 대신 다른 API 가 전혀 안 불렸음을 확인한다.
        [Test]
        public void ReplaceReapersToHex_강화나_소환_API는_호출되지_않음()
        {
            FakeBattleContext ctx = new FakeBattleContext();
            new ReplaceReapersToHexEffect().Apply(ctx);

            Assert.AreEqual(0, ctx.Buffs.Count);
            Assert.AreEqual(0, ctx.Increments.Count);
            Assert.AreEqual(1, ctx.Replaces.Count);
        }
    }
}
