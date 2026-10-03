using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.EditMode
{
    //# 스테이지 규칙 엔진 자체 스모크 (chapter-system §3·§9) — 정상 + 엣지. 본격 스위트는 test-engineer.
    public class StageRuleTests
    {
        private const string JsonDir = "Assets/_Lair/Data/Json/";

        //# 호출 기록용 컨텍스트 더블.
        private class FakeCtx : IStageRuleContext
        {
            public Vector3 ArenaCenter => Vector3.zero;
            public Vector3 HeroPosition { get; set; }
            public int HeroMaxHp { get; set; } = 1000;
            public float HeroHpRatio { get; set; } = 1f;
            public int Healed;
            public float DamageScale = 1f;
            public bool RangeRuleOn;
            public int LockCount;
            public int UnlockCount;
            public readonly List<string> Notices = new List<string>();
            public int FxShown;
            public int FxReleased;

            public void HealHero(int amount) { Healed += amount; }
            public void MultiplyHeroDamageTaken(float mul) { DamageScale *= mul; }
            public void SetMonsterRangeRule(float mul, float minRange) { RangeRuleOn = true; }
            public void ClearMonsterRangeRule() { RangeRuleOn = false; }
            public Vector3[] LockSpawners(EMonster species) { LockCount++; return new[] { Vector3.zero }; }
            public void UnlockSpawners(EMonster species) { UnlockCount++; }
            public int DamageMonstersInSpheres(IReadOnlyList<Vector3> centers, float radius, int amount) { return 0; }
            public bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point) { point = Vector3.zero; return false; }
            public void SetMonsterSpeedScaleInSpheres(IReadOnlyList<Vector3> centers, float radius, float scale) { }
            public void ClearMonsterSpeedScale() { }
            public Vector3 ClampToArena(Vector3 position) { return position; }
            public IStageFx ShowFx(EVisual key, Vector3 position, float scale) { FxShown++; return new FakeFx(this); }
            public void ShowNotice(int stringId, params object[] args) { Notices.Add(stringId.ToString()); }
        }

        private class FakeFx : IStageFx
        {
            private readonly FakeCtx _owner;
            public FakeFx(FakeCtx owner) { _owner = owner; }
            public void SetPosition(Vector3 position) { }
            public void Release() { _owner.FxReleased++; }
        }

        private class FakeHealth : IHealth
        {
            public int Max => 100;
            public int Current { get; set; } = 100;
            public float Ratio => (float)Current / Max;
            public bool IsAlive => Current > 0;
            public event System.Action<int, int> OnChanged;
            public event System.Action OnDied;
            public void TakeDamage(int amount) { Current -= amount; OnChanged?.Invoke(Current, Max); }
            public void Heal(int amount) { Current += amount; }
            public void SetMax(int max, bool resetCurrent = true) { }
        }

        private class RecordingRule : IStageRule
        {
            public readonly List<string> Log;
            public readonly string Name;
            public RecordingRule(string name, List<string> log) { Name = name; Log = log; }
            public EStageRule Id => EStageRule.BossBulwark;
            public bool IsActive { get; private set; }
            public void Activate(IStageRuleContext ctx) { IsActive = true; Log.Add("on:" + Name); }
            public void Tick(float elapsed, float dt) { Log.Add("tick:" + Name); }
            public void Deactivate() { IsActive = false; Log.Add("off:" + Name); }
        }

        private static StageRuleData Data(float? trigger, params (string key, float value)[] ps)
        {
            Dictionary<string, float> map = new Dictionary<string, float>();
            foreach ((string key, float value) in ps)
            {
                map[key] = value;
            }
            return new StageRuleData { RuleId = EStageRule.BossBulwark, Params = map, HasTrigger = trigger.HasValue, TriggerHpFraction = trigger ?? 0f };
        }

        [Test]
        public void 성수샘_개방구간은_20초부터_40초주기_8초간이고_마지막은_268초에_닫힌다()
        {
            Assert.IsFalse(HolySpringRule.IsOpen(19.9f, 20f, 40f, 8f));
            Assert.IsTrue(HolySpringRule.IsOpen(20f, 20f, 40f, 8f));
            Assert.IsFalse(HolySpringRule.IsOpen(28f, 20f, 40f, 8f));
            Assert.IsTrue(HolySpringRule.IsOpen(60f, 20f, 40f, 8f));
            Assert.IsTrue(HolySpringRule.IsOpen(267.9f, 20f, 40f, 8f));
            Assert.IsFalse(HolySpringRule.IsOpen(268f, 20f, 40f, 8f));
        }

        [Test]
        public void 성수샘은_개방중_반경안에서만_소수누적_정수회복한다()
        {
            FakeCtx ctx = new FakeCtx { HeroMaxHp = 4000, HeroPosition = Vector3.zero };
            HolySpringRule rule = new HolySpringRule(new StageRuleParams(new Dictionary<string, float>
            {
                { "centerX", 0f }, { "centerZ", 0f }, { "radius", 3f }, { "healPercentPerSec", 0.4f },
                { "activeSec", 8f }, { "periodSec", 40f }, { "firstActiveSec", 20f },
            }));
            rule.Activate(ctx);

            rule.Tick(10f, 0.5f);
            Assert.AreEqual(0, ctx.Healed, "닫힌 구간은 회복 없음");

            //# 개방 구간 1초 = 0.5초 틱 2회, 틱당 4000 x 0.4% x 0.5 = 8.
            rule.Tick(21f, 0.5f);
            rule.Tick(21.5f, 0.5f);
            Assert.AreEqual(16, ctx.Healed);

            ctx.HeroPosition = new Vector3(5f, 0f, 0f);
            rule.Tick(22f, 0.5f);
            Assert.AreEqual(16, ctx.Healed, "반경 밖이면 회복 없음");
            rule.Deactivate();
        }

        [Test]
        public void 러너는_트리거_규칙을_HP임계를_처음_내려갈때_한번만_활성하고_회복후_재활성하지_않는다()
        {
            FakeCtx ctx = new FakeCtx();
            FakeHealth hero = new FakeHealth();
            List<string> log = new List<string>();
            StageRuleData always = Data(null);
            StageRuleData phase = Data(0.55f);
            StageRuleRunner runner = new StageRuleRunner(ctx, hero, new[] { always, phase },
                d => new RecordingRule(d.HasTrigger ? "phase" : "always", log));
            runner.Start();
            Assert.AreEqual("on:always", log[0]);

            runner.Tick(1f);
            CollectionAssert.DoesNotContain(log, "on:phase");

            hero.Current = 50;
            runner.Tick(2f);
            Assert.AreEqual(1, log.FindAll(l => l == "on:phase").Count);

            hero.Current = 100;
            runner.Tick(3f);
            hero.Current = 50;
            runner.Tick(4f);
            Assert.AreEqual(1, log.FindAll(l => l == "on:phase").Count, "회복 후 재진입해도 재발동 없음(E-1)");
        }

        [Test]
        public void 러너는_일시정지_dt_0_동안_활성과_갱신을_미루고_종료시_전부_비활성한다()
        {
            FakeCtx ctx = new FakeCtx();
            FakeHealth hero = new FakeHealth { Current = 40 };
            List<string> log = new List<string>();
            StageRuleRunner runner = new StageRuleRunner(ctx, hero, new[] { Data(0.55f) }, d => new RecordingRule("phase", log));
            runner.Start();

            runner.Tick(0f);   //# 이전 elapsed(0) 과 같음 → dt 0 → 미룸
            Assert.IsEmpty(log);

            runner.Tick(0.1f);
            Assert.AreEqual("on:phase", log[0]);

            runner.Shutdown();
            runner.Shutdown();
            Assert.AreEqual(1, log.FindAll(l => l == "off:phase").Count, "중복 Shutdown 안전");
        }

        [Test]
        public void 수호결계는_받는피해_배율을_걸고_지속시간_뒤_역수로_원복한다()
        {
            FakeCtx ctx = new FakeCtx();
            BossBulwarkRule rule = new BossBulwarkRule(new StageRuleParams(new Dictionary<string, float>
            {
                { "damageTakenMul", 0.6f }, { "durationSec", 15f },
            }));
            rule.Activate(ctx);
            Assert.AreEqual(0.6f, ctx.DamageScale, 0.0001f);

            rule.Tick(10f, 14f);
            Assert.IsTrue(rule.IsActive);
            rule.Tick(11f, 1f);
            Assert.IsFalse(rule.IsActive);
            Assert.AreEqual(1f, ctx.DamageScale, 0.0001f);

            rule.Deactivate();
            Assert.AreEqual(1f, ctx.DamageScale, 0.0001f, "중복 해제로 이중 원복 없음");
        }

        [Test]
        public void 봉인된둥지는_HP임계_이하에서_한번만_해제하고_알림을_띄운다()
        {
            FakeCtx ctx = new FakeCtx { HeroHpRatio = 0.9f };
            SealedNestRule rule = new SealedNestRule(new StageRuleParams(new Dictionary<string, float>
            {
                { "lockedSpecies", 1f }, { "unlockHpFraction", 0.6f },
            }));
            rule.Activate(ctx);
            Assert.AreEqual(1, ctx.LockCount);

            rule.Tick(5f, 1f);
            Assert.AreEqual(0, ctx.UnlockCount);

            ctx.HeroHpRatio = 0.59f;
            rule.Tick(6f, 1f);
            ctx.HeroHpRatio = 1f;
            rule.Tick(7f, 1f);
            Assert.AreEqual(1, ctx.UnlockCount);
            Assert.AreEqual(1, ctx.Notices.Count);
            Assert.AreEqual(ctx.FxShown, ctx.FxReleased, "사슬 FX 전부 반환");
        }

        [Test]
        public void 안개_사거리규칙은_비활성이면_통과_활성이면_배율과_하한을_적용한다()
        {
            MonsterRangeModifier m = new MonsterRangeModifier();
            Assert.AreEqual(5f, m.Apply(5f), 0.0001f);

            m.Set(0.7f, 0.8f);
            Assert.AreEqual(3.5f, m.Apply(5f), 0.0001f);
            Assert.AreEqual(0.8f, m.Apply(1f), 0.0001f, "1.0 x 0.7 = 0.7 → 하한 0.8");
            Assert.AreEqual(0.5f, m.Apply(0.5f), 0.0001f, "원래 사거리가 하한보다 작으면 늘리지 않는다");

            //# 충돌 거리 1.0(반경 0.5+0.5) → 하한 1.1: 1.3 → 1.1, 원래 1.0 은 하한(1.1)보다 작아 늘리지 않는다(E-7).
            Assert.AreEqual(1.1f, m.Apply(1.3f, 1.0f), 0.0001f);
            Assert.AreEqual(1.0f, m.Apply(1.0f, 1.0f), 0.0001f);
            Assert.AreEqual(3.5f, m.Apply(5f, 1.0f), 0.0001f);

            m.Clear();
            Assert.AreEqual(5f, m.Apply(5f), 0.0001f);
        }

        [Test]
        public void 실제_JSON의_규칙_10종이_모두_팩토리로_생성된다()
        {
            ChapterDatabase db = ChapterDatabase.Build(
                JsonArrayUtility.FromJsonArray<ChapterRow>(File.ReadAllText(JsonDir + "Chapter.json")),
                JsonArrayUtility.FromJsonArray<StageRow>(File.ReadAllText(JsonDir + "Stage.json")),
                JsonArrayUtility.FromJsonArray<Stage_RuleRow>(File.ReadAllText(JsonDir + "Stage_Rule.json")),
                JsonArrayUtility.FromJsonArray<Stage_Rule_ParamRow>(File.ReadAllText(JsonDir + "Stage_Rule_Param.json")),
                JsonArrayUtility.FromJsonArray<Stage_Rule_TriggerRow>(File.ReadAllText(JsonDir + "Stage_Rule_Trigger.json")),
                JsonArrayUtility.FromJsonArray<HeroArchetypeRow>(File.ReadAllText(JsonDir + "HeroArchetype.json")),
                JsonArrayUtility.FromJsonArray<HeroArchetype_KiteRow>(File.ReadAllText(JsonDir + "HeroArchetype_Kite.json")));

            int created = 0;
            for (int chapter = 1; chapter <= 2; ++chapter)
            {
                for (int stage = 1; stage <= 5; ++stage)
                {
                    foreach (StageRuleData data in db.GetRules(chapter, stage))
                    {
                        Assert.AreEqual(data.RuleId, StageRuleFactory.Create(data).Id);
                        ++created;
                    }
                }
            }
            Assert.AreEqual(10, created);
        }
    }
}
