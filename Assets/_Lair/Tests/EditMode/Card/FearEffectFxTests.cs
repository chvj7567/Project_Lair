using System.Collections.Generic;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    //# Fear 카드 — 적용 시 영웅 위 스컬 FX 추가(시각). gameplay-programmer 자체 검증(정상 + 엣지 1).
    //# EditMode 는 인프라(CHMResource/CHMPool) 없음 → FX 스폰은 무동작. 검증 가능한 건
    //# (1) Fear 디버프(오라) 적용이 그대로 유지되는가, (2) FX 호출 추가 후에도 Apply 가 throw 하지 않는가.
    public class FearEffectFxTests
    {
        //# 최소 stub — Fear 가 쓰는 표면만 기록.
        private class FakeCtx : IBattleContext
        {
            public Transform HeroTransform;
            public readonly List<IHeroAura> Auras = new();

            public void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f) => Auras.Add(aura);
            public Transform GetHeroTransform() => HeroTransform;

            //# no-op stubs
            public IEnumerable<IHealth> GetMonsters(EMonster? filter = null) => System.Array.Empty<IHealth>();
            public IHealth GetHero() => null;
            public IMover GetHeroMover() => null;
            public void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier) { }
            public void SpawnMonster(EMonster key, Vector3 nearHero) { }
            public void AddMonsterBuff(EMonsterBuff type, float duration) { }
            public void ScaleAllSpawnerPeriods(float mul) { }
            public void IncrementAllSpawnerOutputs(int delta) { }
            public void ScaleSpawnerPeriodForType(EMonster type, float mul) { }
            public void ActivateBloodThirst(float duration) { }
            public void HalveAllMonsterHp() { }
            public void IncrementSpawnerOutput(EMonster type) { }
            public void ReplaceSpawnerOutput(EMonster from, EMonster to) { }
            public void RegisterCardPick(EBuildAxis axis) { }
            public int GetBuildCount(EBuildAxis axis) => 0;
            public float DeltaTime => 0f;
        }

        //# 정상 — 영웅이 있으면 FearAura 가 적용된다(디버프 로직 불변). FX 호출 추가 후에도 throw 없음.
        [Test]
        public void FearEffect_영웅_있음_FearAura_적용_FX호출에도_예외없음()
        {
            GameObject hero = new GameObject("hero");
            hero.AddComponent<AutoCombatAI>();
            FakeCtx ctx = new FakeCtx { HeroTransform = hero.transform };

            new FearEffect().Apply(ctx);

            Assert.AreEqual(1, ctx.Auras.Count);
            Assert.IsInstanceOf<FearAura>(ctx.Auras[0]);

            Object.DestroyImmediate(hero);
        }

        //# 엣지 — 영웅 Transform 이 null 이면 가드로 즉시 리턴(오라 미적용, FX 미스폰, 예외 없음).
        [Test]
        public void FearEffect_영웅_없음_오라_미적용_예외없음()
        {
            FakeCtx ctx = new FakeCtx { HeroTransform = null };

            new FearEffect().Apply(ctx);

            Assert.AreEqual(0, ctx.Auras.Count);
        }
    }
}
