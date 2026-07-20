using System.Collections.Generic;
using NUnit.Framework;
using Lair.Battle;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 임계점 {30,90,150,210,270}초 (5개) 통과 시 OnTriggered 발행 검증 (분단위 제거, spec §2.B).
    public class ActiveTriggerServiceTests
    {
        //# 제거된 분단위 임계 (회귀 박제 — 이 시점에서 발화하면 안 됨).
        private static readonly float[] RemovedMinuteThresholds = { 60f, 120f, 180f, 240f };
        //# 신 임계 집합 (정상 — 정확히 이 5개에서만 발화).
        private static readonly float[] ExpectedThresholds = { 30f, 90f, 150f, 210f, 270f };

        private static (BattleClock clock, ActiveTriggerService svc, List<int> fired) Setup()
        {
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            ActiveTriggerService svc = new ActiveTriggerService(clock);
            List<int> fired = new List<int>();
            svc.OnTriggered += idx => fired.Add(idx);
            return (clock, svc, fired);
        }

        [Test]
        public void Tick_30초_미만은_미발동()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(29.9f);
            Assert.AreEqual(0, fired.Count);
        }

        [Test]
        public void Tick_30초_도달_시_Index0_발동()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(30f);
            Assert.AreEqual(1, fired.Count);
            Assert.AreEqual(0, fired[0]);
        }

        [Test]
        public void 한_임계점_1회만_발동()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(30f);
            clock.Tick(0.1f);
            Assert.AreEqual(1, fired.Count);
        }

        [Test]
        public void 큰_dt_로_여러_임계점_동시_통과_시_순차_발동()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(95f);    //# 30, 90 통과 (60 임계점 제거됨)
            CollectionAssert.AreEqual(new[] { 0, 1 }, fired);
        }

        [Test]
        public void Dispose_후_미발동()
        {
            (BattleClock clock, ActiveTriggerService svc, List<int> fired) = Setup();
            svc.Dispose();
            clock.Tick(30f);
            Assert.AreEqual(0, fired.Count);
        }

        //# 분단위 제거 후 정상 케이스 — 0→270 경과 시 정확히 5회({30,90,150,210,270}) 발화.
        [Test]
        public void 분단위_제거_후_5회만_발화()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            for (int t = 1; t <= 270; ++t)
                clock.Tick(1f);
            Assert.AreEqual(5, fired.Count, "{30,90,150,210,270} 5개만 발화");
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, fired);
        }

        //# 엣지 — 270초 이후 추가 발화 없음 (마지막 임계 통과 후 끝).
        [Test]
        public void 마지막_임계_270초_이후_추가_발화_없음()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(270f);
            int afterLast = fired.Count;
            clock.Tick(20f);   //# 290초 — 새 임계점 없음
            Assert.AreEqual(afterLast, fired.Count, "270초 이후 신규 발화 없음");
            Assert.AreEqual(5, fired.Count);
        }

        //# ===== test-engineer 확장 — 회귀·엣지·fallback =====

        //# 회귀 (고가치) — 제거된 분단위(60/120/180/240)에서 발화하지 않는다.
        //# 각 임계 직전·직후 1초를 정밀 통과시켜 분단위에서 fired 증가가 없음을 박제.
        [Test]
        public void 제거된_분단위_임계_60_120_180_240에서_무발화_회귀()
        {
            foreach (float removed in RemovedMinuteThresholds)
            {
                (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
                clock.Tick(removed - 1f);   //# 분단위 직전
                int before = fired.Count;
                clock.Tick(1f);             //# 분단위 정확히 통과 — 제거됐으면 증가 없어야
                Assert.AreEqual(before, fired.Count,
                    $"제거된 분단위 {removed}초에서 추가 발화 발생 — 분단위가 살아있음 (회귀)");
            }
        }

        //# 정상 — 0→300 전 구간 1초 dt 누적 시 발화 시점이 정확히 {30,90,150,210,270}.
        //# 발화 순간의 elapsed 를 기록해 시점까지 검증 (인덱스만이 아니라 시간축 일치).
        [Test]
        public void 발화_시점이_정확히_5개_임계와_일치()
        {
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            ActiveTriggerService svc = new ActiveTriggerService(clock);
            List<float> firedAt = new List<float>();
            svc.OnTriggered += _ => firedAt.Add(clock.Elapsed);

            for (int t = 1; t <= 300; ++t)
                clock.Tick(1f);

            Assert.AreEqual(5, firedAt.Count, "정확히 5회 발화");
            for (int i = 0; i < ExpectedThresholds.Length; ++i)
                Assert.GreaterOrEqual(firedAt[i], ExpectedThresholds[i],
                    $"인덱스 {i} 발화 시점은 임계 {ExpectedThresholds[i]} 이상이어야");
            //# 첫 발화는 30~31초 사이(임계 도달 직후 첫 tick) — 30 미만 발화 없음.
            Assert.Less(firedAt[0], 31f, "첫 발화는 30초 직후 (조기 발화 없음)");
        }

        //# 엣지 — 한 번의 큰 dt 점프로 5개 임계 전부 통과해도 각 1회씩(중복 없음) 순차 발화.
        [Test]
        public void 큰_dt_300초_단번_점프_시_5개_각1회씩_중복없이_발화()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(300f);   //# 0 → 300 한 번에
            CollectionAssert.AreEqual(new[] { 0, 1, 2, 3, 4 }, fired,
                "단번 점프 — 5개 인덱스 순차 1회씩, 중복 없음");
        }

        //# 엣지 — 95f 큰 dt 후 추가 tick 해도 이미 발화한 30/90 재발화 없음 (멱등성).
        [Test]
        public void 큰_dt_후_재tick_시_이미_발화한_임계_재발화_없음()
        {
            (BattleClock clock, ActiveTriggerService _, List<int> fired) = Setup();
            clock.Tick(95f);    //# 30, 90 통과
            Assert.AreEqual(2, fired.Count);
            clock.Tick(0.5f);   //# 95.5 — 새 임계 없음
            clock.Tick(0.5f);   //# 96.0 — 여전히 없음
            CollectionAssert.AreEqual(new[] { 0, 1 }, fired,
                "재tick 해도 30/90 재발화 없음 — _fired 플래그 멱등");
        }

        //# fallback — BalanceConfig 미주입(thresholds=null) 시 DefaultThresholds(5개)로 동작.
        //# 생성자 null 경로가 실제로 5개 집합을 쓰는지 — 0→300 발화 카운트로 검증.
        [Test]
        public void thresholds_null_생성_시_Default_5개_fallback()
        {
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            //# 명시적으로 null 주입 — BattleController 가 _balance?.ActiveThresholds(null 가능)를 넘기는 경로 재현.
            ActiveTriggerService svc = new ActiveTriggerService(clock, null);
            int count = 0;
            svc.OnTriggered += _ => count++;

            for (int t = 1; t <= 300; ++t)
                clock.Tick(1f);

            Assert.AreEqual(5, count, "thresholds=null → DefaultThresholds(5개) fallback");
        }

        //# fallback 정합 — BalanceConfig.ActiveThresholds 기본 직렬화값이 신 집합(5개)과 일치.
        //# 런타임 진실원(BalanceConfig.asset)과 코드 기본값이 desync 되지 않았는지 박제 (spec §2.B 진실원 동기화).
        [Test]
        public void BalanceConfig_기본_ActiveThresholds_가_신_집합_5개와_일치()
        {
            //# BalanceConfig 는 순수 클래스 — new 로 코드 기본값 인스턴스(teardown 불필요).
            BalanceConfig cfg = new BalanceConfig();
            float[] thresholds = cfg.ActiveThresholds;
            Assert.IsNotNull(thresholds, "ActiveThresholds null 아님");
            CollectionAssert.AreEqual(ExpectedThresholds, thresholds,
                "BalanceConfig 기본 _activeThresholds 가 {30,90,150,210,270} 와 일치");
            //# 제거된 분단위가 코드 기본값에 잔존하지 않는지 명시 박제.
            foreach (float removed in RemovedMinuteThresholds)
            {
                CollectionAssert.DoesNotContain(thresholds, removed,
                    $"분단위 {removed} 가 코드 기본값에 잔존 (회귀)");
            }
        }

        //# 엣지 — 주입 thresholds 가 우선한다 (튜닝/디버그용 커스텀 배열 경로).
        [Test]
        public void 커스텀_thresholds_주입_시_Default_대신_사용()
        {
            BattleClock clock = new BattleClock(300f);
            clock.Start();
            ActiveTriggerService svc = new ActiveTriggerService(clock, new[] { 10f, 20f });
            int count = 0;
            svc.OnTriggered += _ => count++;

            clock.Tick(25f);   //# 10, 20 통과 — Default(30..)는 아직
            Assert.AreEqual(2, count, "주입 배열 {10,20} 사용 — Default 무시");
        }

        //# 엣지 — clock=null 생성 시 예외 없이 생성 (구독 가드). 발화는 당연히 없음.
        [Test]
        public void clock_null_생성도_예외없음()
        {
            ActiveTriggerService svc = null;
            Assert.DoesNotThrow(() => svc = new ActiveTriggerService(null));
            Assert.DoesNotThrow(() => svc.Dispose());
        }
    }
}
