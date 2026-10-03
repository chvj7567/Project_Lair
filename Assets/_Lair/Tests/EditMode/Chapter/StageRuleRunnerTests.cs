using System;
using System.Collections.Generic;
using Lair.Battle;
using Lair.Character;
using Lair.Data;
using NUnit.Framework;

namespace Lair.Tests.EditMode
{
    //# StageRuleRunner 본격 검증 — 상시/트리거 분리, 래치(E-1), 다중 임계 순서(E-2), dt=0 지연(E-4·E-5), Shutdown(E-6).
    public class StageRuleRunnerTests : ChapterStaticStateTestBase
    {
        private RecordingStageRuleContext _ctx;
        private StageRuleFakeHero _hero;
        private List<string> _log;
        private List<StageProbeRule> _made;

        [SetUp]
        public void 러너_더블을_초기화한다()
        {
            _ctx = new RecordingStageRuleContext();
            _hero = new StageRuleFakeHero();
            _log = new List<string>();
            _made = new List<StageProbeRule>();
        }

        private static StageRuleData Always()
        {
            return StageRuleDataFactory.Make(EStageRule.BossBulwark, null);
        }

        private static StageRuleData Phase(float fraction)
        {
            return StageRuleDataFactory.Make(EStageRule.BossBulwark, fraction);
        }

        private StageRuleRunner Build(IStageRuleContext ctx, IHealth hero, params StageRuleData[] data)
        {
            return new StageRuleRunner(ctx, hero, data, d =>
            {
                string name = d.HasTrigger ? "phase" + d.TriggerHpFraction.ToString("0.00") : "always" + _made.Count;
                StageProbeRule rule = new StageProbeRule(name, _log);
                _made.Add(rule);
                return rule;
            });
        }

        private StageRuleRunner Build(params StageRuleData[] data)
        {
            return Build(_ctx, _hero, data);
        }

        private int Count(string entry)
        {
            return _log.FindAll(l => l == entry).Count;
        }

        //# --- 생성 / 시작 ---

        [Test]
        public void 규칙_목록이_비어도_Start_Tick_Shutdown이_예외없이_동작한다()
        {
            StageRuleRunner runner = Build();

            Assert.DoesNotThrow(() =>
            {
                runner.Start();
                runner.Tick(1f);
                runner.Shutdown();
            });
            Assert.AreEqual(0, runner.AlwaysRuleCount);
            Assert.AreEqual(0, runner.TriggeredRuleCount);
            Assert.IsEmpty(_log);
        }

        [Test]
        public void 상시와_트리거_규칙_수가_트리거_row_유무로_분류된다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f), Always(), Phase(0.35f));

            Assert.AreEqual(2, runner.AlwaysRuleCount);
            Assert.AreEqual(2, runner.TriggeredRuleCount);
        }

        [Test]
        public void 상시_규칙은_Start에서_활성되고_트리거_규칙은_활성되지_않는다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f));

            runner.Start();

            Assert.AreEqual(1, Count("on:always0"));
            Assert.AreEqual(0, Count("on:phase0.55"));
        }

        [Test]
        public void Start_전의_Tick은_무시된다()
        {
            _hero.Current = 10;
            StageRuleRunner runner = Build(Always(), Phase(0.55f));

            runner.Tick(5f);

            Assert.IsEmpty(_log);
        }

        [Test]
        public void Start를_두번_불러도_상시_규칙은_한번만_활성된다()
        {
            StageRuleRunner runner = Build(Always());

            runner.Start();
            runner.Start();

            Assert.AreEqual(1, Count("on:always0"));
        }

        [Test]
        public void Shutdown_뒤에_Start를_불러도_활성되지_않는다()
        {
            StageRuleRunner runner = Build(Always());

            runner.Shutdown();
            runner.Start();

            Assert.IsEmpty(_log);
        }

        [Test]
        public void 규칙_생성은_팩토리가_던진_예외를_그대로_전파한다()
        {
            StageRuleData unknown = StageRuleDataFactory.Make((EStageRule)99, null);

            Assert.Throws<ArgumentOutOfRangeException>(() => new StageRuleRunner(_ctx, _hero, new[] { unknown }));
        }

        //# --- 트리거 래치 ---

        [Test]
        public void HP가_임계_바로_위면_활성되지_않고_임계와_같으면_활성된다()
        {
            StageRuleRunner runner = Build(Phase(0.55f));
            runner.Start();

            _hero.Current = 56;
            runner.Tick(1f);
            Assert.AreEqual(0, Count("on:phase0.55"), "0.56 > 0.55");

            _hero.Current = 55;
            runner.Tick(2f);
            Assert.AreEqual(1, Count("on:phase0.55"), "0.55 <= 0.55");
        }

        [Test]
        public void 트리거_규칙은_회복과_재하락을_반복해도_한번만_활성된다()
        {
            StageRuleRunner runner = Build(Phase(0.55f));
            runner.Start();

            for (int cycle = 0; cycle < 4; ++cycle)
            {
                _hero.Current = 40;
                runner.Tick(cycle * 2 + 1f);
                _hero.Current = 100;
                runner.Tick(cycle * 2 + 2f);
            }

            Assert.AreEqual(1, Count("on:phase0.55"));
        }

        [Test]
        public void 한_프레임에_여러_임계를_건너뛰면_입력_순서와_무관하게_높은_값부터_활성된다()
        {
            StageRuleRunner runner = Build(Phase(0.35f), Phase(0.75f));
            runner.Start();

            _hero.Current = 20;
            runner.Tick(1f);

            Assert.Less(_log.IndexOf("on:phase0.75"), _log.IndexOf("on:phase0.35"));
            Assert.AreEqual(1, Count("on:phase0.75"));
            Assert.AreEqual(1, Count("on:phase0.35"));
        }

        [Test]
        public void 임계를_차례로_내려가면_각_규칙이_자기_임계에서만_활성된다()
        {
            StageRuleRunner runner = Build(Phase(0.75f), Phase(0.35f));
            runner.Start();

            _hero.Current = 50;
            runner.Tick(1f);
            Assert.AreEqual(1, Count("on:phase0.75"));
            Assert.AreEqual(0, Count("on:phase0.35"));

            _hero.Current = 30;
            runner.Tick(2f);
            Assert.AreEqual(1, Count("on:phase0.35"));
        }

        [Test]
        public void 영웅이_사망하면_트리거_규칙은_평가되지_않는다()
        {
            StageRuleRunner runner = Build(Phase(0.55f));
            runner.Start();

            _hero.Current = 0;
            runner.Tick(1f);

            Assert.AreEqual(0, Count("on:phase0.55"));
        }

        [Test]
        public void 영웅이_null이면_트리거_규칙은_활성되지_않고_상시_규칙은_틱된다()
        {
            StageRuleRunner runner = Build(_ctx, null, Always(), Phase(0.55f));
            runner.Start();

            Assert.DoesNotThrow(() => runner.Tick(1f));

            Assert.AreEqual(0, Count("on:phase0.55"));
            Assert.AreEqual(1, _made[0].TickCount);
        }

        //# --- 틱 / dt ---

        [Test]
        public void 활성_규칙은_elapsed와_직전_틱_이후_dt를_받는다()
        {
            StageRuleRunner runner = Build(Always());
            runner.Start();

            runner.Tick(0.5f);
            runner.Tick(2f);

            Assert.AreEqual(2f, _made[0].LastElapsed, 0.0001f);
            Assert.AreEqual(1.5f, _made[0].LastDt, 0.0001f);
            Assert.AreEqual(2, _made[0].TickCount);
        }

        [Test]
        public void 트리거로_방금_활성된_규칙도_같은_프레임에_틱된다()
        {
            StageRuleRunner runner = Build(Phase(0.55f));
            runner.Start();

            _hero.Current = 40;
            runner.Tick(0.25f);

            Assert.AreEqual(1, _made[0].TickCount);
            Assert.AreEqual(0.25f, _made[0].LastDt, 0.0001f);
        }

        [Test]
        public void 일시정지로_dt가_0이면_활성과_틱을_모두_미루고_재개_후_활성된다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f));
            runner.Start();
            runner.Tick(1f);
            int alwaysTicks = _made[0].TickCount;

            _hero.Current = 30;
            runner.Tick(1f);
            runner.Tick(1f);

            Assert.AreEqual(0, Count("on:phase0.55"), "카드 선택 중에는 활성 지연");
            Assert.AreEqual(alwaysTicks, _made[0].TickCount, "상시 규칙도 갱신되지 않음");

            runner.Tick(1.1f);
            Assert.AreEqual(1, Count("on:phase0.55"));
        }

        [Test]
        public void elapsed가_거꾸로_가면_틱하지_않고_기준시각만_갱신한다()
        {
            StageRuleRunner runner = Build(Always());
            runner.Start();
            runner.Tick(5f);
            int ticks = _made[0].TickCount;

            runner.Tick(3f);
            Assert.AreEqual(ticks, _made[0].TickCount);

            runner.Tick(4f);
            Assert.AreEqual(1f, _made[0].LastDt, 0.0001f, "기준이 3 으로 갱신돼 dt = 1");
        }

        [Test]
        public void 첫_Tick이_elapsed_0이면_dt가_0이라_무시된다()
        {
            StageRuleRunner runner = Build(Always());
            runner.Start();

            runner.Tick(0f);

            Assert.AreEqual(0, _made[0].TickCount);
        }

        //# --- Shutdown ---

        [Test]
        public void Shutdown은_활성된_규칙만_활성_역순으로_비활성한다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f), Phase(0.10f));
            runner.Start();
            _hero.Current = 40;
            runner.Tick(1f);

            runner.Shutdown();

            int offAlways = _log.IndexOf("off:always0");
            int offPhase = _log.IndexOf("off:phase0.55");
            Assert.Less(offPhase, offAlways, "나중에 켠 규칙이 먼저 꺼진다");
            Assert.AreEqual(0, Count("off:phase0.10"), "한 번도 활성되지 않은 규칙은 비활성 호출 없음");
        }

        [Test]
        public void Shutdown은_중복_호출해도_각_규칙을_한번만_비활성한다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f));
            runner.Start();
            _hero.Current = 40;
            runner.Tick(1f);

            runner.Shutdown();
            runner.Shutdown();

            Assert.AreEqual(1, _made[0].DeactivateCount);
            Assert.AreEqual(1, _made[1].DeactivateCount);
        }

        [Test]
        public void Shutdown_후_Tick은_활성도_갱신도_하지_않는다()
        {
            StageRuleRunner runner = Build(Always(), Phase(0.55f));
            runner.Start();
            runner.Tick(1f);
            runner.Shutdown();
            int ticks = _made[0].TickCount;

            _hero.Current = 10;
            runner.Tick(5f);

            Assert.AreEqual(ticks, _made[0].TickCount);
            Assert.AreEqual(0, _made[1].ActivateCount);
        }

        [Test]
        public void Start_없이_Shutdown하면_아무_규칙도_건드리지_않는다()
        {
            StageRuleRunner runner = Build(Always());

            runner.Shutdown();

            Assert.AreEqual(0, _made[0].ActivateCount);
            Assert.AreEqual(0, _made[0].DeactivateCount);
        }

        //# --- 실제 규칙과의 통합 (수호 결계) ---

        private static StageRuleData BulwarkData()
        {
            return StageRuleDataFactory.Make(EStageRule.BossBulwark, 0.55f, ("damageTakenMul", 0.6f), ("durationSec", 15f));
        }

        [Test]
        public void 실제_수호결계는_HP_55퍼센트에서_발동하고_15초_뒤_원복되며_Shutdown이_이중_원복하지_않는다()
        {
            StageRuleRunner runner = new StageRuleRunner(_ctx, _hero, new[] { BulwarkData() });
            runner.Start();
            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f, "발동 전 영향 없음");

            _hero.Current = 55;
            runner.Tick(1f);
            Assert.AreEqual(0.6f, _ctx.DamageScale, 0.0001f);
            Assert.AreEqual(1, _ctx.Notices.Count);

            runner.Tick(16f);
            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f, "지속시간 만료 시 역수로 원복");

            runner.Shutdown();
            Assert.AreEqual(2, _ctx.DamageMultiplies.Count, "곱 호출은 발동 1 + 원복 1 뿐");
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 실제_수호결계_활성_중_Shutdown하면_즉시_원복되고_FX가_반환된다()
        {
            StageRuleRunner runner = new StageRuleRunner(_ctx, _hero, new[] { BulwarkData() });
            runner.Start();
            _hero.Current = 50;
            runner.Tick(1f);
            Assert.AreEqual(0.6f, _ctx.DamageScale, 0.0001f);

            runner.Shutdown();

            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f);
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 실제_수호결계는_HP가_임계_위에_머물면_끝까지_발동하지_않는다()
        {
            StageRuleRunner runner = new StageRuleRunner(_ctx, _hero, new[] { BulwarkData() });
            runner.Start();

            _hero.Current = 90;
            for (int i = 1; i <= 20; ++i)
            {
                runner.Tick(i);
            }
            runner.Shutdown();

            Assert.AreEqual(0, _ctx.DamageMultiplies.Count);
            Assert.AreEqual(0, _ctx.Notices.Count);
        }
    }
}
