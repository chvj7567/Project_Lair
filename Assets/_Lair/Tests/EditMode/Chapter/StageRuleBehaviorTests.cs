using System.Collections.Generic;
using System.Text.RegularExpressions;
using Lair.Battle;
using Lair.Data;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Lair.Tests.EditMode
{
    //# 스테이지 규칙 6종 개별 동작 — 활성/비활성 시각, 경계, 중복 해제, FX 반환 (chapter-system §3.4·§9). Fake 컨텍스트만 사용.
    public class StageRuleBehaviorTests : ChapterStaticStateTestBase
    {
        private RecordingStageRuleContext _ctx;

        [SetUp]
        public void 컨텍스트_더블을_초기화한다()
        {
            _ctx = new RecordingStageRuleContext();
        }

        //# ===== HolySpring 성수 샘 =====

        [Test]
        public void 성수샘_개방구간_시작은_300초_이전_7회뿐이다()
        {
            int opens = 0;
            bool wasOpen = false;
            for (float t = 0f; t < 300f; t += 0.5f)
            {
                bool open = HolySpringRule.IsOpen(t, 20f, 40f, 8f);
                if (open && wasOpen == false)
                {
                    Assert.AreEqual(20f + opens * 40f, t, 0.0001f, $"{opens + 1} 번째 개방 시작");
                    ++opens;
                }
                wasOpen = open;
            }
            Assert.AreEqual(7, opens, "20·60·...·260 초 7회");
        }

        [TestCase(0f, false)]
        [TestCase(19.99f, false)]
        [TestCase(20f, true)]
        [TestCase(27.99f, true)]
        [TestCase(28f, false)]
        [TestCase(59.99f, false)]
        [TestCase(60f, true)]
        [TestCase(260f, true)]
        [TestCase(267.99f, true)]
        [TestCase(268f, false)]
        public void 성수샘_개방_판정은_구간_양끝이_반열린이다(float elapsed, bool expected)
        {
            Assert.AreEqual(expected, HolySpringRule.IsOpen(elapsed, 20f, 40f, 8f));
        }

        [TestCase(0f)]
        [TestCase(-5f)]
        public void 성수샘_주기가_0이하이면_항상_닫혀_있다(float period)
        {
            Assert.IsFalse(HolySpringRule.IsOpen(100f, 20f, period, 8f));
        }

        private HolySpringRule NewSpring(float healPercent = 0.4f, float radius = 3f, float cx = 0f, float cz = 0f)
        {
            return new HolySpringRule(StageRuleDataFactory.HolySpring(healPercent, radius, cx, cz));
        }

        [Test]
        public void 성수샘은_Activate_전과_Deactivate_후에는_회복하지_않는다()
        {
            HolySpringRule rule = NewSpring();

            Assert.DoesNotThrow(() => rule.Tick(21f, 0.5f));
            rule.Activate(_ctx);
            rule.Deactivate();
            rule.Tick(21f, 0.5f);

            Assert.AreEqual(0, _ctx.Heals.Count);
            Assert.IsFalse(rule.IsActive);
        }

        [Test]
        public void 성수샘_틱_길이_0_5초_미만은_누적되고_dt가_크면_틱을_몰아서_처리한다()
        {
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);

            rule.Tick(21f, 0.25f);
            Assert.AreEqual(0, _ctx.Heals.Count, "0.25초는 아직 틱 아님");
            rule.Tick(21.25f, 0.25f);
            Assert.AreEqual(1, _ctx.Heals.Count);

            rule.Tick(23.25f, 2f);
            Assert.AreEqual(5, _ctx.Heals.Count, "2초 = 4틱 추가");
            Assert.AreEqual(5 * 8, _ctx.TotalHealed);
        }

        [Test]
        public void 성수샘_회복량은_최대HP_퍼센트_곱_틱길이를_소수_누적해_정수만_반영한다()
        {
            _ctx.HeroMaxHp = 100;
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);

            //# 틱당 100 x 0.4% x 0.5 = 0.2 → 5틱째에 누적 1.0.
            for (int i = 0; i < 4; ++i)
            {
                rule.Tick(21f + i * 0.5f, 0.5f);
            }
            Assert.AreEqual(0, _ctx.TotalHealed, "4틱 누적 0.8");

            rule.Tick(23f, 0.5f);
            Assert.AreEqual(1, _ctx.TotalHealed);
            Assert.AreEqual(1, _ctx.Heals.Count);
        }

        [Test]
        public void 성수샘_닫힌_구간_틱은_소수_누적에도_기여하지_않는다()
        {
            _ctx.HeroMaxHp = 4000;
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);

            rule.Tick(10f, 5f);
            Assert.AreEqual(0, _ctx.Heals.Count);
            rule.Tick(21f, 0.5f);

            Assert.AreEqual(8, _ctx.TotalHealed, "닫힌 구간 5초 동안 쌓인 몫 없이 한 틱 분량만");
        }

        [TestCase(3f, 0f, true)]
        [TestCase(3.01f, 0f, false)]
        [TestCase(0f, -3f, true)]
        [TestCase(2.2f, 2.2f, false)]
        public void 성수샘_회복_반경_판정은_반경_이하가_안쪽이다(float x, float z, bool inside)
        {
            _ctx.HeroPosition = new Vector3(x, 0f, z);
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);

            rule.Tick(21f, 0.5f);

            Assert.AreEqual(inside ? 1 : 0, _ctx.Heals.Count);
        }

        [Test]
        public void 성수샘_중심은_전장_원점_기준_오프셋이고_높이는_무시한다()
        {
            _ctx.ArenaCenter = new Vector3(10f, 0f, 10f);
            _ctx.HeroPosition = new Vector3(12f, 7f, 9f);
            HolySpringRule rule = NewSpring(0.4f, 1f, 2f, -1f);
            rule.Activate(_ctx);

            rule.Tick(21f, 0.5f);

            Assert.AreEqual(1, _ctx.Heals.Count, "중심 (12,·,9) 위에 서 있음 — y 차이 무시");
            Assert.AreEqual(new Vector3(12f, 0f, 9f), _ctx.Fxs[0].Position);
        }

        [Test]
        public void 성수샘_FX는_개방_동안_하나만_유지되고_닫히면_반환되며_다음_개방에_새로_뜬다()
        {
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);

            rule.Tick(19f, 0.5f);
            Assert.AreEqual(0, _ctx.Fxs.Count, "개방 전 FX 없음");

            rule.Tick(20f, 0.5f);
            rule.Tick(21f, 0.5f);
            rule.Tick(27f, 0.5f);
            Assert.AreEqual(1, _ctx.Fxs.Count);
            Assert.AreEqual(EVisual.StageSpringFx, _ctx.Fxs[0].Key);
            Assert.AreEqual(6f, _ctx.Fxs[0].Scale, 0.0001f, "지름 = 반경 x 2");

            rule.Tick(28f, 0.5f);
            Assert.AreEqual(1, _ctx.Fxs[0].ReleaseCount);

            rule.Tick(60f, 0.5f);
            Assert.AreEqual(2, _ctx.Fxs.Count);
            rule.Deactivate();
            rule.Deactivate();
            Assert.AreEqual(1, _ctx.Fxs[1].ReleaseCount, "중복 Deactivate 로 이중 반환 없음");
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 성수샘_재활성하면_이전_소수_누적이_초기화된다()
        {
            _ctx.HeroMaxHp = 100;
            HolySpringRule rule = NewSpring();
            rule.Activate(_ctx);
            rule.Tick(21f, 0.5f);
            rule.Tick(21.5f, 0.5f);
            rule.Deactivate();

            rule.Activate(_ctx);
            for (int i = 0; i < 3; ++i)
            {
                rule.Tick(22f + i * 0.5f, 0.5f);
            }

            Assert.AreEqual(0, _ctx.TotalHealed, "남은 0.4 가 이월됐다면 3틱(0.6)째에 1 회복");
        }

        //# ===== DenseFog 짙은 안개 =====

        [Test]
        public void 안개_Activate는_사거리_규칙과_FX를_걸고_Deactivate는_한번만_푼다()
        {
            DenseFogRule rule = new DenseFogRule(StageRuleDataFactory.DenseFog());
            _ctx.ArenaCenter = new Vector3(1f, 0f, 2f);

            rule.Activate(_ctx);
            rule.Tick(5f, 1f);

            Assert.IsTrue(rule.IsActive);
            Assert.AreEqual(1, _ctx.RangeRuleSets.Count);
            Assert.AreEqual(0.7f, _ctx.RangeRuleSets[0].Key, 0.0001f);
            Assert.AreEqual(0.8f, _ctx.RangeRuleSets[0].Value, 0.0001f);
            Assert.AreEqual(EVisual.StageFogFx, _ctx.Fxs[0].Key);
            Assert.AreEqual(new Vector3(1f, 0f, 2f), _ctx.Fxs[0].Position);

            rule.Deactivate();
            rule.Deactivate();
            Assert.AreEqual(1, _ctx.RangeRuleClears);
            Assert.AreEqual(1, _ctx.Fxs[0].ReleaseCount);
            Assert.IsFalse(rule.IsActive);
        }

        [Test]
        public void 안개_Activate_전_Deactivate는_아무것도_해제하지_않는다()
        {
            DenseFogRule rule = new DenseFogRule(StageRuleDataFactory.DenseFog());

            Assert.DoesNotThrow(() => rule.Deactivate());
            Assert.AreEqual(0, _ctx.RangeRuleClears);
        }

        //# ===== SealedNest 봉인된 둥지 =====

        [Test]
        public void 봉인_Activate는_종족을_봉인하고_스포너마다_사슬_FX를_띄운다()
        {
            _ctx.SpawnerPositions = new[] { new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f) };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));

            rule.Activate(_ctx);

            CollectionAssert.AreEqual(new[] { EMonster.Wraith }, _ctx.Locks);
            Assert.AreEqual(2, _ctx.Fxs.Count);
            Assert.AreEqual(EVisual.StageSealChainFx, _ctx.Fxs[0].Key);
            Assert.AreEqual(new Vector3(-1f, 0f, 0f), _ctx.Fxs[1].Position);
        }

        [TestCase(0.61f, false)]
        [TestCase(0.6f, true)]
        [TestCase(0.01f, true)]
        public void 봉인_해제_판정은_HP가_임계_이하일_때다(float ratio, bool unlocked)
        {
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(_ctx);

            _ctx.HeroHpRatio = ratio;
            rule.Tick(5f, 1f);

            Assert.AreEqual(unlocked ? 1 : 0, _ctx.Unlocks.Count);
        }

        [Test]
        public void 봉인_해제_알림은_문구_id와_종족명을_담고_사슬은_전부_반환된다()
        {
            _ctx.SpawnerPositions = new[] { Vector3.zero, Vector3.one };
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(_ctx);

            _ctx.HeroHpRatio = 0.5f;
            rule.Tick(5f, 1f);

            Assert.AreEqual(1, _ctx.Notices.Count);
            Assert.AreEqual(SealedNestRule.UnlockNoticeStringId, _ctx.Notices[0].StringId);
            Assert.AreEqual(SpeciesVisual.SpeciesName(EMonster.Wraith), _ctx.Notices[0].Args[0]);
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 봉인은_해제_후_HP가_회복돼도_다시_봉인되지_않고_알림도_한번뿐이다()
        {
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(_ctx);
            _ctx.HeroHpRatio = 0.5f;
            rule.Tick(5f, 1f);

            _ctx.HeroHpRatio = 1f;
            rule.Tick(6f, 1f);
            _ctx.HeroHpRatio = 0.1f;
            rule.Tick(7f, 1f);

            Assert.AreEqual(1, _ctx.Locks.Count);
            Assert.AreEqual(1, _ctx.Unlocks.Count);
            Assert.AreEqual(1, _ctx.Notices.Count);
        }

        [Test]
        public void 봉인_중_Deactivate는_알림과_버스트_없이_봉인만_풀고_사슬을_반환한다()
        {
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(_ctx);

            rule.Deactivate();
            rule.Deactivate();

            Assert.AreEqual(1, _ctx.Unlocks.Count, "중복 Deactivate 로 이중 해제 없음");
            Assert.AreEqual(0, _ctx.Notices.Count);
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 이미_해제된_봉인은_Deactivate에서_다시_해제하지_않는다()
        {
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));
            rule.Activate(_ctx);
            _ctx.HeroHpRatio = 0.3f;
            rule.Tick(5f, 1f);

            rule.Deactivate();

            Assert.AreEqual(1, _ctx.Unlocks.Count);
        }

        [Test]
        public void 봉인할_스포너가_없으면_오류를_남기고_규칙은_무효로_동작하지_않는다()
        {
            _ctx.SpawnerPositions = new Vector3[0];
            LogAssert.Expect(LogType.Error, new Regex("봉인할"));
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));

            rule.Activate(_ctx);
            _ctx.HeroHpRatio = 0.1f;
            rule.Tick(5f, 1f);
            rule.Deactivate();

            Assert.AreEqual(0, _ctx.Fxs.Count);
            Assert.AreEqual(0, _ctx.Unlocks.Count, "봉인된 적이 없으니 해제 호출도 없음");
            Assert.AreEqual(0, _ctx.Notices.Count);
        }

        [Test]
        public void 봉인_Activate_전_Tick과_Deactivate는_예외없이_무시된다()
        {
            SealedNestRule rule = new SealedNestRule(StageRuleDataFactory.SealedNest(1f, 0.6f));

            Assert.DoesNotThrow(() => rule.Tick(1f, 1f));
            Assert.DoesNotThrow(() => rule.Deactivate());
        }

        //# ===== BossBulwark 수호 결계 =====

        [Test]
        public void 결계_Activate는_배율_알림_FX를_한번씩_발동하고_Tick은_FX를_영웅에_붙인다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());
            _ctx.HeroPosition = new Vector3(1f, 0f, 1f);

            rule.Activate(_ctx);
            _ctx.HeroPosition = new Vector3(4f, 0f, 2f);
            rule.Tick(1f, 1f);

            Assert.AreEqual(0.6f, _ctx.DamageScale, 0.0001f);
            Assert.AreEqual(BossBulwarkRule.NoticeStringId, _ctx.Notices[0].StringId);
            Assert.AreEqual(EVisual.StageBulwarkFx, _ctx.Fxs[0].Key);
            Assert.AreEqual(new Vector3(4f, 0f, 2f), _ctx.Fxs[0].Position, "FX 가 영웅을 따라간다");
        }

        [Test]
        public void 결계는_지속시간_경계에서_만료되고_그_전에는_유지된다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());
            rule.Activate(_ctx);

            rule.Tick(1f, 14.99f);
            Assert.IsTrue(rule.IsActive);
            Assert.AreEqual(0.6f, _ctx.DamageScale, 0.0001f);

            rule.Tick(2f, 0.02f);
            Assert.IsFalse(rule.IsActive, "남은 시간이 정확히 0 이하가 되면 만료");
            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f);
        }

        [Test]
        public void 결계는_여러_틱의_합이_지속시간에_닿을_때_만료된다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());
            rule.Activate(_ctx);

            for (int i = 0; i < 14; ++i)
            {
                rule.Tick(i, 1f);
            }
            Assert.IsTrue(rule.IsActive, "14초 경과");

            rule.Tick(14f, 1f);
            Assert.IsFalse(rule.IsActive);
            Assert.AreEqual(0, _ctx.LiveFxCount);
        }

        [Test]
        public void 결계_만료_후_Tick과_Deactivate는_추가_원복도_FX_반환도_하지_않는다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());
            rule.Activate(_ctx);
            rule.Tick(1f, 20f);

            rule.Tick(2f, 1f);
            rule.Deactivate();

            Assert.AreEqual(2, _ctx.DamageMultiplies.Count);
            Assert.AreEqual(1, _ctx.Fxs[0].ReleaseCount);
            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f);
        }

        [Test]
        public void 결계_활성_중_Deactivate는_역수로_정확히_한번_원복한다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());
            rule.Activate(_ctx);

            rule.Deactivate();
            rule.Deactivate();

            Assert.AreEqual(1f / 0.6f, _ctx.DamageMultiplies[1], 0.0001f);
            Assert.AreEqual(2, _ctx.DamageMultiplies.Count);
            Assert.AreEqual(1f, _ctx.DamageScale, 0.0001f);
        }

        [Test]
        public void 결계_Activate_전_Deactivate와_Tick은_예외없이_무시된다()
        {
            BossBulwarkRule rule = new BossBulwarkRule(StageRuleDataFactory.BossBulwark());

            Assert.DoesNotThrow(() => rule.Tick(1f, 1f));
            Assert.DoesNotThrow(() => rule.Deactivate());
            Assert.AreEqual(0, _ctx.DamageMultiplies.Count);
        }

        //# ===== ArrowRain 화살비 =====

        private ArrowRainRule NewRain()
        {
            return new ArrowRainRule(StageRuleDataFactory.ArrowRain());
        }

        [Test]
        public void 화살비_Activate는_알림과_탐색_후_예고_FX를_띄우고_탐색_반경을_전달한다()
        {
            _ctx.HasDensePoint = true;
            _ctx.DensePoint = new Vector3(2f, 0f, 1f);
            ArrowRainRule rule = NewRain();

            rule.Activate(_ctx);

            Assert.AreEqual(ArrowRainRule.NoticeStringId, _ctx.Notices[0].StringId);
            Assert.AreEqual(8f, _ctx.LastSearchRadius, 0.0001f);
            Assert.AreEqual(3.5f, _ctx.LastClusterRadius, 0.0001f);
            Assert.AreEqual(EVisual.StageArrowRainFx, _ctx.Fxs[0].Key);
            Assert.AreEqual(new Vector3(2f, 0f, 1f), _ctx.Fxs[0].Position);
            Assert.AreEqual(7f, _ctx.Fxs[0].Scale, 0.0001f);
            Assert.AreEqual(0, _ctx.Damages.Count, "예고 중에는 피해 없음");
        }

        [Test]
        public void 화살비는_예고_1초_뒤부터_0_5초_간격으로_4틱_타격하고_마지막_틱에서_FX를_반환한다()
        {
            _ctx.HasDensePoint = true;
            _ctx.DensePoint = new Vector3(2f, 0f, 1f);
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);

            rule.Tick(0.5f, 0.5f);
            Assert.AreEqual(0, _ctx.Damages.Count, "예고 0.5초");
            rule.Tick(1f, 0.5f);
            Assert.AreEqual(1, _ctx.Damages.Count, "예고 1.0초에 첫 타격");
            rule.Tick(1.5f, 0.5f);
            rule.Tick(2f, 0.5f);
            Assert.AreEqual(3, _ctx.Damages.Count);
            Assert.AreEqual(1, _ctx.LiveFxCount, "마지막 타격 전까지 FX 유지");
            rule.Tick(2.5f, 0.5f);

            Assert.AreEqual(4, _ctx.Damages.Count);
            Assert.AreEqual(0, _ctx.LiveFxCount);
            Assert.AreEqual(50, _ctx.Damages[0].Amount);
            Assert.AreEqual(3.5f, _ctx.Damages[0].Radius, 0.0001f);
            Assert.AreEqual(new Vector3(2f, 0f, 1f), _ctx.Damages[0].Centers[0]);
        }

        [Test]
        public void 화살비는_한번의_큰_dt에도_남은_틱을_모두_순서대로_처리하되_4회를_넘지_않는다()
        {
            _ctx.HasDensePoint = true;
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);

            rule.Tick(5f, 5f);

            Assert.AreEqual(4, _ctx.Damages.Count);
        }

        [Test]
        public void 화살비는_타격_지점을_예고_시작에_고정한다()
        {
            _ctx.HasDensePoint = true;
            _ctx.DensePoint = new Vector3(2f, 0f, 1f);
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);

            _ctx.DensePoint = new Vector3(-9f, 0f, -9f);
            rule.Tick(1f, 1f);

            Assert.AreEqual(new Vector3(2f, 0f, 1f), _ctx.Damages[0].Centers[0]);
        }

        [Test]
        public void 화살비는_대상이_없으면_그_주기를_건너뛰고_다음_주기에_다시_탐색한다()
        {
            _ctx.HasDensePoint = false;
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);

            rule.Tick(3f, 3f);
            Assert.AreEqual(0, _ctx.Damages.Count);
            Assert.AreEqual(0, _ctx.Fxs.Count, "대상 없으면 FX 도 없음");
            Assert.AreEqual(1, _ctx.DenseFindCalls);

            _ctx.HasDensePoint = true;
            _ctx.DensePoint = new Vector3(1f, 0f, 1f);
            rule.Tick(10f, 7f);
            Assert.AreEqual(2, _ctx.DenseFindCalls, "10초 주기에서 재탐색");
            Assert.AreEqual(1, _ctx.Fxs.Count);

            rule.Tick(11f, 1f);
            Assert.AreEqual(1, _ctx.Damages.Count);
        }

        [Test]
        public void 화살비는_10초마다_새_주기를_시작해_타격을_반복한다()
        {
            _ctx.HasDensePoint = true;
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);
            rule.Tick(2.5f, 2.5f);
            Assert.AreEqual(4, _ctx.Damages.Count);

            rule.Tick(10f, 7.5f);
            Assert.AreEqual(2, _ctx.DenseFindCalls);
            rule.Tick(12.5f, 2.5f);

            Assert.AreEqual(8, _ctx.Damages.Count);
        }

        [Test]
        public void 화살비_Deactivate_후에는_타격하지_않고_FX는_반환되며_중복_해제도_안전하다()
        {
            _ctx.HasDensePoint = true;
            ArrowRainRule rule = NewRain();
            rule.Activate(_ctx);

            rule.Deactivate();
            rule.Deactivate();
            rule.Tick(3f, 3f);

            Assert.AreEqual(0, _ctx.Damages.Count);
            Assert.AreEqual(1, _ctx.Fxs[0].ReleaseCount);
            Assert.IsFalse(rule.IsActive);
        }

        //# ===== HunterTraps 사냥꾼의 덫 =====

        private HunterTrapsRule NewTraps()
        {
            return new HunterTrapsRule(StageRuleDataFactory.HunterTraps());
        }

        [Test]
        public void 덫_Activate는_영웅_둘레_링에_6개를_60도_간격으로_설치한다()
        {
            _ctx.HeroPosition = new Vector3(1f, 0f, 2f);
            HunterTrapsRule rule = NewTraps();

            rule.Activate(_ctx);

            Assert.AreEqual(HunterTrapsRule.NoticeStringId, _ctx.Notices[0].StringId);
            Assert.AreEqual(6, _ctx.Fxs.Count);
            foreach (StageFxRecord fx in _ctx.Fxs)
            {
                Assert.AreEqual(EVisual.StageTrapFx, fx.Key);
                Assert.AreEqual(2.4f, fx.Scale, 0.0001f);
                Assert.AreEqual(3.5f, Vector3.Distance(fx.Position, _ctx.HeroPosition), 0.001f, "링 반경");
            }
            Assert.AreEqual(4.5f, _ctx.Fxs[0].Position.x, 0.001f);
            Assert.AreEqual(2f, _ctx.Fxs[0].Position.z, 0.001f);
            Assert.AreEqual(-2.5f, _ctx.Fxs[3].Position.x, 0.001f, "180도 반대편");
        }

        [Test]
        public void 덫은_전장_밖으로_나가는_위치를_ClampToArena로_당겨_설치한다()
        {
            _ctx.HeroPosition = new Vector3(4f, 0f, 0f);
            _ctx.ArenaClampRadius = 5f;
            HunterTrapsRule rule = NewTraps();

            rule.Activate(_ctx);

            foreach (StageFxRecord fx in _ctx.Fxs)
            {
                Assert.LessOrEqual(Vector3.Distance(fx.Position, _ctx.ArenaCenter), 5.001f);
            }
            Assert.AreEqual(5f, Vector3.Distance(_ctx.Fxs[0].Position, _ctx.ArenaCenter), 0.001f, "(7.5,0,0) 은 5 로 당겨짐");
        }

        [Test]
        public void 덫_Tick은_매_프레임_감속을_걸고_0_5초마다_초당_피해의_절반을_준다()
        {
            HunterTrapsRule rule = NewTraps();
            rule.Activate(_ctx);

            rule.Tick(0.25f, 0.25f);
            Assert.AreEqual(0, _ctx.Damages.Count);
            Assert.AreEqual(1, _ctx.SpeedSets.Count, "감속은 매 프레임 판정");

            rule.Tick(0.5f, 0.25f);
            Assert.AreEqual(1, _ctx.Damages.Count);
            Assert.AreEqual(5, _ctx.Damages[0].Amount, "10 x 0.5");
            Assert.AreEqual(1.2f, _ctx.Damages[0].Radius, 0.0001f);
            Assert.AreEqual(6, _ctx.Damages[0].Centers.Count);
            Assert.AreEqual(0.5f, _ctx.SpeedSets[1].Scale, 0.0001f);
            Assert.AreEqual(1.2f, _ctx.SpeedSets[1].Radius, 0.0001f);
        }

        [Test]
        public void 덫은_12초에_소멸해_감속을_복구하고_FX를_반환하며_이후_15초에_재설치된다()
        {
            _ctx.HeroPosition = new Vector3(0f, 0f, 0f);
            HunterTrapsRule rule = NewTraps();
            rule.Activate(_ctx);

            for (int i = 1; i <= 23; ++i)
            {
                rule.Tick(i * 0.5f, 0.5f);
            }
            Assert.AreEqual(23, _ctx.Damages.Count, "11.5초까지 0.5초마다 피해");
            Assert.AreEqual(0, _ctx.SpeedClears);

            rule.Tick(12f, 0.5f);
            Assert.AreEqual(1, _ctx.SpeedClears, "소멸 즉시 감속 복구");
            Assert.AreEqual(0, _ctx.LiveFxCount);
            Assert.AreEqual(23, _ctx.Damages.Count, "소멸 틱엔 피해 없음");

            for (int i = 25; i <= 29; ++i)
            {
                rule.Tick(i * 0.5f, 0.5f);
            }
            Assert.AreEqual(23, _ctx.Damages.Count, "12~15초 공백");
            Assert.AreEqual(6, _ctx.Fxs.Count);

            _ctx.HeroPosition = new Vector3(-3f, 0f, 0f);
            rule.Tick(15f, 0.5f);
            Assert.AreEqual(12, _ctx.Fxs.Count, "15초에 새 덫 6개");
            Assert.AreEqual(-3f + 3.5f, _ctx.Fxs[6].Position.x, 0.001f, "재설치는 그 시점 영웅 위치 기준");
        }

        [Test]
        public void 덫_Deactivate는_감속을_한번만_복구하고_FX를_반환하며_이후_Tick은_무시된다()
        {
            HunterTrapsRule rule = NewTraps();
            rule.Activate(_ctx);

            rule.Deactivate();
            rule.Deactivate();
            rule.Tick(1f, 1f);

            Assert.AreEqual(1, _ctx.SpeedClears);
            Assert.AreEqual(0, _ctx.LiveFxCount);
            Assert.AreEqual(0, _ctx.Damages.Count);
            Assert.AreEqual(0, _ctx.SpeedSets.Count);
            Assert.IsFalse(rule.IsActive);
        }

        [Test]
        public void 덫_Activate_전_Deactivate는_감속_복구를_부르지_않는다()
        {
            HunterTrapsRule rule = NewTraps();

            Assert.DoesNotThrow(() => rule.Deactivate());
            Assert.AreEqual(0, _ctx.SpeedClears);
        }

        //# ===== 전 규칙 공통 =====

        [Test]
        public void 모든_규칙은_Activate_후_즉시_Deactivate하면_FX를_남기지_않는다()
        {
            _ctx.HasDensePoint = true;
            IStageRule[] rules =
            {
                new HolySpringRule(StageRuleDataFactory.HolySpring()),
                new DenseFogRule(StageRuleDataFactory.DenseFog()),
                new SealedNestRule(StageRuleDataFactory.SealedNest()),
                new BossBulwarkRule(StageRuleDataFactory.BossBulwark()),
                new ArrowRainRule(StageRuleDataFactory.ArrowRain()),
                new HunterTrapsRule(StageRuleDataFactory.HunterTraps()),
            };

            foreach (IStageRule rule in rules)
            {
                rule.Activate(_ctx);
                rule.Tick(21f, 0.5f);
                rule.Deactivate();
                rule.Deactivate();
            }

            Assert.AreEqual(0, _ctx.LiveFxCount);
            foreach (StageFxRecord fx in _ctx.Fxs)
            {
                Assert.LessOrEqual(fx.ReleaseCount, 1, "FX 이중 반환 없음");
            }
        }
    }
}
