using NUnit.Framework;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 지속 스폰 — 종별 누적 스탯 배율 POCO (StatMultiplier) 검증.
    //# gameplay-programmer 자체 검증 수준 (정상 + 엣지 1). 본격 스위트는 test-engineer.
    public class StatMultiplierTests
    {
        //# 정상 — 강화 카드 1픽이 해당 스탯 필드를 곱연산 갱신.
        [Test]
        public void Multiply_Hp_단일픽_필드에_배율_적용()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(1.5f, m.HpMul, 0.0001f);
            //# 다른 필드는 항등값 1.0 유지.
            Assert.AreEqual(1f, m.PowerMul, 0.0001f);
        }

        //# 정상 — 중첩 픽은 곱연산 누적 (§3.1: ×1.5 두 번 = ×2.25).
        [Test]
        public void Multiply_같은_스탯_2회_곱연산_누적()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 1.5f);
            m.Multiply(EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(2.25f, m.HpMul, 0.0001f);
        }

        //# 엣지 — Identity 항등값은 모든 필드 1.0 (매 호출 새 인스턴스 — 오염 불가).
        [Test]
        public void Identity_모든_필드_1점0_불변()
        {
            var id = StatMultiplier.Identity;

            Assert.AreEqual(1f, id.HpMul, 0.0001f);
            Assert.AreEqual(1f, id.PowerMul, 0.0001f);
            Assert.AreEqual(1f, id.CooldownMul, 0.0001f);
            Assert.AreEqual(1f, id.RangeMul, 0.0001f);
            Assert.AreEqual(1f, id.MoveSpeedMul, 0.0001f);
            Assert.AreEqual(1f, id.SlowFactorMul, 0.0001f);
        }

        //# 엣지 — 플레이그 둔화 배율: BaseSlowFactor 0.8 × 1픽 0.75 = 0.6 (기존 1차 수치, §3.0.1).
        [Test]
        public void SlowFactor_1픽_baseline_0점8과_곱하면_0점6()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.SlowFactor, 0.75f);

            float applied = Lair.Character.PlagueSlowOnHit.BaseSlowFactor * m.SlowFactorMul;
            Assert.AreEqual(0.6f, applied, 0.0001f);
        }

        //# === test-engineer 본격 스위트 — 엣지·회귀 보강 ===

        //# 엣지 — 플레이그 둔화 2픽 누적: SlowFactorMul 0.75²=0.5625, baseline 곱하면 0.45 (§3.0.1).
        [Test]
        public void SlowFactor_2픽_곱연산_baseline과_곱하면_0점45()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.SlowFactor, 0.75f);
            m.Multiply(EMonsterStatKind.SlowFactor, 0.75f);

            Assert.AreEqual(0.5625f, m.SlowFactorMul, 0.0001f);
            float applied = Lair.Character.PlagueSlowOnHit.BaseSlowFactor * m.SlowFactorMul;
            Assert.AreEqual(0.45f, applied, 0.0001f);
        }

        //# 엣지 — 1 미만 배율(공속 카드 쿨다운 ×0.7) 2픽 곱연산: 0.7²=0.49.
        [Test]
        public void Cooldown_1미만_배율_2픽_곱연산_0점49()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Cooldown, 0.7f);
            m.Multiply(EMonsterStatKind.Cooldown, 0.7f);

            Assert.AreEqual(0.49f, m.CooldownMul, 0.0001f);
        }

        //# 회귀 — 한 스탯 갱신이 다른 스탯 필드를 오염시키지 않는다 (스탯 독립성).
        [Test]
        public void Multiply_한_스탯_갱신은_다른_스탯에_영향없음()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 1.5f);
            m.Multiply(EMonsterStatKind.Power, 1.5f);
            m.Multiply(EMonsterStatKind.MoveSpeed, 1.5f);

            //# 건드린 3개만 1.5, 나머지 3개는 항등값 1.0 유지.
            Assert.AreEqual(1.5f, m.HpMul, 0.0001f);
            Assert.AreEqual(1.5f, m.PowerMul, 0.0001f);
            Assert.AreEqual(1.5f, m.MoveSpeedMul, 0.0001f);
            Assert.AreEqual(1f, m.CooldownMul, 0.0001f, "Cooldown 미갱신 — 1.0 유지");
            Assert.AreEqual(1f, m.RangeMul, 0.0001f, "Range 미갱신 — 1.0 유지");
            Assert.AreEqual(1f, m.SlowFactorMul, 0.0001f, "SlowFactor 미갱신 — 1.0 유지");
        }

        //# 회귀 — 6종 스탯 종류 전부 Multiply 가 올바른 필드로 라우팅된다.
        [Test]
        public void Multiply_6종_스탯_각자_올바른_필드로_라우팅()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 2f);
            m.Multiply(EMonsterStatKind.Power, 3f);
            m.Multiply(EMonsterStatKind.Cooldown, 4f);
            m.Multiply(EMonsterStatKind.Range, 5f);
            m.Multiply(EMonsterStatKind.MoveSpeed, 6f);
            m.Multiply(EMonsterStatKind.SlowFactor, 7f);

            Assert.AreEqual(2f, m.HpMul, 0.0001f);
            Assert.AreEqual(3f, m.PowerMul, 0.0001f);
            Assert.AreEqual(4f, m.CooldownMul, 0.0001f);
            Assert.AreEqual(5f, m.RangeMul, 0.0001f);
            Assert.AreEqual(6f, m.MoveSpeedMul, 0.0001f);
            Assert.AreEqual(7f, m.SlowFactorMul, 0.0001f);
        }

        //# 스포너 상태 UI — Get(stat) 정상: 누적 곱연산 결과를 그대로 읽어온다.
        [Test]
        public void Get_누적_배율_값_반환()
        {
            var m = new StatMultiplier();
            m.Multiply(EMonsterStatKind.Hp, 1.5f);
            m.Multiply(EMonsterStatKind.Hp, 1.5f);

            Assert.AreEqual(2.25f, m.Get(EMonsterStatKind.Hp), 0.0001f, "Hp 2픽 곱연산 결과 = 2.25");
            Assert.AreEqual(1f,   m.Get(EMonsterStatKind.Power), 0.0001f, "Power 미픽 — 1.0 유지");
        }

        //# 회귀 — Identity 는 매 호출 새 인스턴스. 한 Identity 를 변경해도 다음 Identity 는 오염 없음.
        //# (StatMultiplier.Identity 가 공유 정적 인스턴스로 바뀌면 이 테스트가 깨진다.)
        [Test]
        public void Identity_매_호출_새_인스턴스_오염_불가()
        {
            var a = StatMultiplier.Identity;
            var b = StatMultiplier.Identity;

            //# 서로 다른 인스턴스여야 — 공유 가변 인스턴스면 오염 위험.
            Assert.AreNotSame(a, b, "Identity 는 호출마다 새 인스턴스");

            //# a 를 변경해도 b 는 영향 없음.
            a.Multiply(EMonsterStatKind.Hp, 9f);
            Assert.AreEqual(9f, a.HpMul, 0.0001f);
            Assert.AreEqual(1f, b.HpMul, 0.0001f, "b 는 a 변경에 오염되지 않음");

            //# 새로 꺼낸 Identity 도 항등값.
            Assert.AreEqual(1f, StatMultiplier.Identity.HpMul, 0.0001f);
        }
    }
}
