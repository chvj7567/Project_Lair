using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Lair.Battle;
using Lair.Card;
using Lair.Data;
using Lair.Tests.Helpers;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Lair.Tests.EditMode
{
    //# 던전 편성 본격 스위트 - 카드 후보 제외(any-of) + 봉인된 둥지 규칙(StageRuleContext.LockSpawners · SealedNestInfo · SealedNestRule). dungeon-formation §5.1~5.2, §9.1.
    public class FormationCardAndSealTests
    {
        private FormationDatabase _db;
        private readonly List<Object> _spawned = new List<Object>();

        [SetUp]
        public void 준비()
        {
            _db = FormationTestKit.ShippedDb();
        }

        [TearDown]
        public void 정리()
        {
            foreach (Object o in _spawned)
            {
                if (o != null)
                {
                    Object.DestroyImmediate(o);
                }
            }
            _spawned.Clear();
        }

        //# --- 카드 후보 제외 (편성 독립 — 픽 상한만 본다, dungeon-formation-free §4) ---

        [Test]
        public void 패시브_9픽을_진행해도_매_드로우가_3장을_채우고_캡_도달_카드는_나오지_않는다()
        {
            for (int seed = 1; seed <= 60; ++seed)
            {
                List<CardData> cards = new List<CardData>();
                foreach (string name in FormationTestKit.Passives())
                {
                    CardData card = FakeCardData.Create(FormationTestKit.Card(name));
                    _spawned.Add(card);
                    cards.Add(card);
                }
                CardDeck deck = new CardDeck(cards, seed);
                CardPickCounter counter = new CardPickCounter();
                System.Random chooser = new System.Random(seed * 7919);
                Func<ECardId, bool> excluded = id => BattleController.IsCardExcluded(counter, id);

                for (int pick = 0; pick < 9; ++pick)
                {
                    IReadOnlyList<CardData> drawn = deck.Draw(3, excluded);
                    Assert.AreEqual(3, drawn.Count, $"seed {seed} 픽 {pick + 1} 후보 수");
                    foreach (CardData card in drawn)
                    {
                        Assert.IsFalse(counter.IsCapped(card.Id), $"캡 도달 카드 {card.Id} 가 후보로 나옴");
                    }
                    counter.RecordPick(drawn[chooser.Next(drawn.Count)].Id);
                }
            }
        }

        [Test]
        public void 캡_제외는_캡_도달_카드만_제외하고_종_의존_카드도_미캡이면_후보다()
        {
            CardPickCounter counter = new CardPickCounter();
            for (int i = 0; i < CardPickCounter.Cap; ++i)
            {
                counter.RecordPick(ECardId.HexRangeBoost);
            }

            Assert.IsTrue(BattleController.IsCardExcluded(counter, ECardId.HexRangeBoost), "캡이면 제외");
            Assert.IsFalse(BattleController.IsCardExcluded(counter, ECardId.WispHpBoost), "종 전용 카드도 편성과 무관하게 후보");
            Assert.IsFalse(BattleController.IsCardExcluded(counter, ECardId.IronWill), "무의존 + 미캡은 후보");
        }

        //# --- StageRuleContext.LockSpawners (봉인 1곳) ---

        private Spawner[] MakeSpawners(params EMonster[] outputs)
        {
            Spawner[] spawners = new Spawner[outputs.Length];
            for (int i = 0; i < outputs.Length; ++i)
            {
                GameObject go = new GameObject("SpawnerSeal" + i);
                _spawned.Add(go);
                go.transform.position = new Vector3(i, 0f, 0f);
                Spawner sp = go.AddComponent<Spawner>();
                FieldInfo field = typeof(Spawner).GetField("_outputType", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(field, "Spawner._outputType 필드 (production 시그니처 변경 감지)");
                field.SetValue(sp, outputs[i]);
                MethodInfo onEnable = typeof(Spawner).GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
                Assert.IsNotNull(onEnable, "Spawner.OnEnable");
                onEnable.Invoke(sp, null);
                spawners[i] = sp;
            }
            return spawners;
        }

        private static StageRuleContext MakeContext(IReadOnlyList<Spawner> spawners, Action<string> notify = null)
        {
            return new StageRuleContext(null, null, spawners, null, null, null, notify, new DifficultyTestSupport.RealStrings());
        }

        private static int LockedIndex(Spawner[] spawners)
        {
            int found = -1;
            for (int i = 0; i < spawners.Length; ++i)
            {
                if (spawners[i].IsLocked)
                {
                    Assert.AreEqual(-1, found, "봉인은 정확히 1곳");
                    found = i;
                }
            }
            return found;
        }

        [Test]
        public void 봉인_종이_있으면_그_종_둥지_1곳만_봉인하고_위치를_돌려준다()
        {
            Spawner[] spawners = MakeSpawners(_db.DefaultFormation(null));
            StageRuleContext ctx = MakeContext(spawners);

            Vector3[] positions = ctx.LockSpawners(EMonster.Wraith);

            Assert.AreEqual(1, positions.Length);
            Assert.AreEqual(3, LockedIndex(spawners), "기본 편성 망령 = 슬롯 4 (현행과 동일)");
            Assert.AreEqual(spawners[3].transform.position, positions[0]);
            Assert.AreEqual(EMonster.Wraith, ctx.SealedSpeciesOf(EMonster.Wraith));
        }

        [Test]
        public void 봉인_종이_여러_곳이면_슬롯_번호_최소_1곳만_봉인한다()
        {
            Spawner[] spawners = MakeSpawners(EMonster.Wraith, EMonster.Reaper, EMonster.Hex, EMonster.Wraith, EMonster.Plague, EMonster.Phantom);
            StageRuleContext ctx = MakeContext(spawners);

            Assert.AreEqual(1, ctx.LockSpawners(EMonster.Wraith).Length);
            Assert.AreEqual(0, LockedIndex(spawners));
            Assert.IsFalse(spawners[3].IsLocked);
        }

        [Test]
        public void 해제는_봉인한_그_둥지만_풀고_두_번째_해제는_무동작이다()
        {
            Spawner[] spawners = MakeSpawners(_db.DefaultFormation(null));
            StageRuleContext ctx = MakeContext(spawners);
            ctx.LockSpawners(EMonster.Wraith);

            ctx.UnlockSpawners(EMonster.Wraith);
            Assert.AreEqual(-1, LockedIndex(spawners));
            Assert.DoesNotThrow(() => ctx.UnlockSpawners(EMonster.Wraith));
            Assert.DoesNotThrow(() => ctx.UnlockSpawners(EMonster.Plague), "봉인한 적 없는 종");
        }

        [Test]
        public void 후보가_없으면_빈_위치_배열이고_이미_봉인된_둥지는_후보에서_빠진다()
        {
            Spawner[] spawners = MakeSpawners(EMonster.Wraith, EMonster.Hex);
            spawners[0].Lock();
            spawners[1].Lock();
            StageRuleContext ctx = MakeContext(spawners);
            Assert.AreEqual(0, ctx.LockSpawners(EMonster.Wraith).Length, "모든 둥지가 이미 봉인");

            Spawner[] partly = MakeSpawners(EMonster.Wraith, EMonster.Wraith, EMonster.Hex);
            partly[0].Lock();
            StageRuleContext second = MakeContext(partly);
            Assert.AreEqual(1, second.LockSpawners(EMonster.Wraith).Length);
            Assert.IsTrue(partly[1].IsLocked, "이미 봉인된 슬롯 1 대신 다음 망령 둥지");
            Assert.IsFalse(partly[2].IsLocked);
        }

        [Test]
        public void 스포너_목록이_null이거나_비면_빈_위치_배열이다()
        {
            Assert.AreEqual(0, MakeContext(null).LockSpawners(EMonster.Wraith).Length);
            Assert.AreEqual(0, MakeContext(new Spawner[0]).LockSpawners(EMonster.Wraith).Length);
        }

        [Test]
        public void 스포너_목록에_null_칸이_있어도_예외없이_다음_후보를_봉인한다()
        {
            Spawner[] spawners = MakeSpawners(EMonster.Hex, EMonster.Wraith);
            Spawner[] withHole = { null, spawners[0], spawners[1] };
            StageRuleContext ctx = MakeContext(withHole);

            Vector3[] positions = null;
            Assert.DoesNotThrow(() => positions = ctx.LockSpawners(EMonster.Wraith));
            Assert.AreEqual(1, positions.Length);
            Assert.IsTrue(spawners[1].IsLocked);
        }

        [Test]
        public void 봉인된_둥지도_카드_출력_종은_그대로이고_봉인은_종을_바꾸지_않는다()
        {
            Spawner[] spawners = MakeSpawners(_db.DefaultFormation(null));
            MakeContext(spawners).LockSpawners(EMonster.Wraith);
            for (int i = 0; i < spawners.Length; ++i)
            {
                Assert.AreEqual(_db.DefaultFormation(null)[i], spawners[i].CurrentType);
            }
        }

        [Test]
        public void 해제_알림_문구는_봉인된_둥지의_종_이름으로_채워진다()
        {
            List<string> notices = new List<string>();
            StageRuleContext ctx = MakeContext(new Spawner[0], notices.Add);
            ctx.ShowNotice(SealedNestRule.UnlockNoticeStringId, SpeciesVisual.SpeciesName(EMonster.Reaper));
            Assert.AreEqual(1, notices.Count);
            Assert.AreEqual("사신 둥지의 봉인이 풀렸다!", notices[0]);
        }

        //# --- SealedNestInfo ---

        private static StageRuleData SealData(EStageRule rule, float species, float unlock)
        {
            return new StageRuleData
            {
                RuleId = rule,
                Params = new Dictionary<string, float> { { "lockedSpecies", species }, { "unlockHpFraction", unlock } },
            };
        }

        [Test]
        public void AnyUnlockHpPercent는_봉인_종과_무관하게_해제_퍼센트를_돌려준다()
        {
            Assert.AreEqual(60, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.SealedNest, 1f, 0.6f)));
            Assert.AreEqual(50, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.SealedNest, 3f, 0.5f)));
            Assert.AreEqual(55, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.SealedNest, 4f, 0.55f)));
            Assert.AreEqual(70, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.SealedNest, 2f, 0.7f)));
        }

        [Test]
        public void AnyUnlockHpPercent는_봉인_규칙이_아니면_0이다()
        {
            Assert.AreEqual(0, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.DenseFog, 1f, 0.6f)));
            Assert.AreEqual(0, SealedNestInfo.AnyUnlockHpPercent(SealData(EStageRule.BossBulwark, 1f, 0.6f)));
        }

        [Test]
        public void UnlockHpPercent는_규칙의_봉인_종과_같을_때만_퍼센트를_돌려준다()
        {
            StageRuleData data = SealData(EStageRule.SealedNest, 1f, 0.6f);
            Assert.AreEqual(60, SealedNestInfo.UnlockHpPercent(data, EMonster.Wraith));
            Assert.AreEqual(0, SealedNestInfo.UnlockHpPercent(data, EMonster.Reaper), "대체로 봉인된 다른 종 둥지는 0");
            Assert.AreEqual(0, SealedNestInfo.UnlockHpPercent(SealData(EStageRule.DenseFog, 1f, 0.6f), EMonster.Wraith));
        }

        //# --- SealedNestRule ---

        private class SubstitutingContext : RecordingStageRuleContext, IStageRuleContext
        {
            public EMonster Substitute = EMonster.Reaper;

            EMonster IStageRuleContext.SealedSpeciesOf(EMonster requested)
            {
                return Substitute;
            }
        }

        [Test]
        public void 봉인_후보_둥지가_없으면_봉인_없이_조용히_넘어가고_해제를_시도하지_않는다()
        {
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { SpawnerPositions = new Vector3[0], HeroHpRatio = 1f };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));

            rule.Activate(ctx);

            ctx.HeroHpRatio = 0.1f;
            rule.Tick(5f, 1f);
            Assert.AreEqual(0, ctx.Notices.Count, "봉인하지 못했으니 해제 알림도 없다");
            Assert.AreEqual(0, ctx.Unlocks.Count);
            rule.Deactivate();
            Assert.AreEqual(0, ctx.Unlocks.Count, "봉인이 없으면 종료 정리에서도 해제 호출 없음");
        }

        [Test]
        public void 해제_알림은_대체로_봉인된_둥지의_종_이름을_쓴다()
        {
            SubstitutingContext ctx = new SubstitutingContext { HeroHpRatio = 0.9f };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(ctx);
            Assert.AreEqual(1, ctx.Locks.Count);
            Assert.AreEqual(EMonster.Wraith, ctx.Locks[0], "규칙이 요청하는 종은 데이터의 봉인 종");

            ctx.HeroHpRatio = 0.6f;
            rule.Tick(10f, 1f);

            Assert.AreEqual(1, ctx.Notices.Count);
            Assert.AreEqual(SealedNestRule.UnlockNoticeStringId, ctx.Notices[0].StringId);
            Assert.AreEqual("사신", ctx.Notices[0].Args[0], "요청 종(망령)이 아니라 실제 봉인된 사신");
            Assert.AreEqual(1, ctx.Unlocks.Count);
        }

        [Test]
        public void 임계값_정확히_같은_HP에서_해제되고_위에서는_해제되지_않는다()
        {
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HeroHpRatio = 0.61f };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(ctx);

            rule.Tick(1f, 1f);
            Assert.AreEqual(0, ctx.Unlocks.Count);

            ctx.HeroHpRatio = 0.6f;
            rule.Tick(2f, 1f);
            Assert.AreEqual(1, ctx.Unlocks.Count, "unlockHpFraction 이하(경계 포함)");
        }

        [Test]
        public void 알_수_없는_봉인_종_번호는_규칙_생성_오류다()
        {
            Assert.Throws<ArgumentException>(() => new SealedNestRule(StageRuleDataFactory.SealedNest(30f, 0.6f)));
        }

        [Test]
        public void 전투_종료_정리는_해제_알림_없이_봉인만_푼다()
        {
            RecordingStageRuleContext ctx = new RecordingStageRuleContext { HeroHpRatio = 0.9f };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(ctx);

            rule.Deactivate();
            rule.Deactivate();

            Assert.AreEqual(1, ctx.Unlocks.Count, "중복 정리에도 한 번");
            Assert.AreEqual(0, ctx.Notices.Count);
        }
    }
}
