using System.Globalization;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Lair.Card;
using Lair.Character;
using Lair.Data;
using Lair.Tests.Helpers;
using Lair.UI;

namespace Lair.Tests.UI
{
    //# 스포너 상태 UI — 영역 F (SpawnerStatusTooltip 스탯별 줄 포맷, 기획서 §2.5.5).
    //#
    //# private FormatBuffLine 을 리플렉션으로 호출해 6 스탯 템플릿 검증:
    //#  - Hp:        "H 체력 ×1.5 (200 → 300)"
    //#  - Power:     "D 공격력 ×1.5 (200 → 300)"
    //#  - Range:     "R 사거리 ×1.4 (4.0 → 5.6)"
    //#  - MoveSpeed: "M 이동속도 ×1.5 (2.0 → 3.0)"
    //#  - Cooldown:  "S 공격속도 ×1.43 (cd 1.0s → 0.7s)"     ← 역수 변환
    //#  - SlowFactor:"P 둔화 효과 (0.8 → 0.6) — 강화"       ← const 사용
    //#  - PickCount ≥ 2 일 때만 " ×N" 배지.
    //#  - AppliedBuffs 비어있을 때 "적용된 강화 없음".
    public class SpawnerStatusTooltipFormatTests
    {
        private GameObject _go;
        private SpawnerStatusTooltip _tooltip;
        private BalanceConfig _balance;
        //# 부모(_root.parent) 접근 회피 — PositionAboveAnchor 우회. 본 테스트는 FormatBuffLine 만 검증.

        //# 본 테스트 — 부동소수 표기를 invariant 로 강제 (한국어 로캘 콤마 회피 위해 0.## 포맷이지만 안전).
        //# Unity 의 string interpolation 은 culture 영향을 받을 수 있어 invariant 명시.
        private CultureInfo _previousCulture;

        [SetUp]
        public void SetUp()
        {
            _previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;
            _go = new GameObject("Tooltip_UT");
            _tooltip = _go.AddComponent<SpawnerStatusTooltip>();
            _balance = ScriptableObject.CreateInstance<BalanceConfig>();
            //# 컨셉서 §11.3 의 기본 스탯 (스펙 §2.5.5 표) 주입.
            //# EMonster: Wisp=0, Wraith=1, Reaper=2, Hex=3, Plague=4, Phantom=5.
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[" +
                "{\"Key\":0,\"Stat\":{\"Hp\":200,\"Power\":10,\"Range\":1.5,\"Cooldown\":1.0,\"MoveSpeed\":2.0}}," +
                "{\"Key\":1,\"Stat\":{\"Hp\":500,\"Power\":20,\"Range\":1.5,\"Cooldown\":2.0,\"MoveSpeed\":1.0}}," +
                "{\"Key\":2,\"Stat\":{\"Hp\":100,\"Power\":40,\"Range\":1.5,\"Cooldown\":1.0,\"MoveSpeed\":1.5}}," +
                "{\"Key\":3,\"Stat\":{\"Hp\":60,\"Power\":30,\"Range\":4.0,\"Cooldown\":1.5,\"MoveSpeed\":1.5}}," +
                "{\"Key\":4,\"Stat\":{\"Hp\":50,\"Power\":5,\"Range\":1.2,\"Cooldown\":1.2,\"MoveSpeed\":3.0}}," +
                "{\"Key\":5,\"Stat\":{\"Hp\":30,\"Power\":5,\"Range\":1.5,\"Cooldown\":1.0,\"MoveSpeed\":2.0}}" +
                "]}",
                _balance);

            //# _arg 주입 — FormatBuffLine 이 _arg.Balance 를 사용.
            var arg = new SpawnerStatusTooltipArg { Balance = _balance };
            SetPrivate(_tooltip, "_arg", arg);
        }

        [TearDown]
        public void TearDown()
        {
            if (_go != null) Object.DestroyImmediate(_go);
            if (_balance != null) Object.DestroyImmediate(_balance);
            CultureInfo.CurrentCulture = _previousCulture;
        }

        private static void SetPrivate(object target, string field, object value)
        {
            var fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인");
            fi.SetValue(target, value);
        }

        //# private FormatBuffLine(EMonster, AppliedBuff) → string 리플렉션 호출.
        private string CallFormatBuffLine(EMonster type, BattleViewModel.AppliedBuff buff)
        {
            var mi = typeof(SpawnerStatusTooltip).GetMethod("FormatBuffLine",
                BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "SpawnerStatusTooltip.FormatBuffLine 메서드 존재 확인 (production 시그니처 변경 감지)");
            return (string)mi.Invoke(_tooltip, new object[] { type, buff });
        }

        private static BattleViewModel.AppliedBuff MakeBuff(
            ECardId sourceId, int pickCount, EMonsterStatKind stat, float aggregateMul)
        {
            return new BattleViewModel.AppliedBuff
            {
                Source = FakeCardData.Create(sourceId),
                PickCount = pickCount,
                Stat = stat,
                AggregateMultiplier = aggregateMul,
            };
        }

        //# ===== 스탯별 줄 포맷 (1픽, 배지 없음) =====

        //# Hp — H 글자 + "체력" 라벨 + 절대값.
        [Test]
        public void FormatBuffLine_Hp_1픽_H_체력_절대값_올바름()
        {
            //# 1픽: 1.5 배율. Wisp Hp 200 → 300.
            var buff = MakeBuff(ECardId.WispHpBoost, pickCount: 1, EMonsterStatKind.Hp, 1.5f);

            string line = CallFormatBuffLine(EMonster.Wisp, buff);

            //# "0.##" 포맷 — 1.5 는 "1.5" (소수 셋째 자리 절삭).
            StringAssert.StartsWith("H", line, "H 글자로 시작");
            StringAssert.Contains("체력", line);
            StringAssert.Contains("×1.5", line, "곱연산 배율 1.5");
            StringAssert.Contains("(200 → 300)", line, "Base 200 → Result 300");
            //# 배지 없음 — " ×2" 같은 prefix 가 없음.
            StringAssert.DoesNotContain("×2", line, "1픽 — 배지 없음");
        }

        //# Power — D 글자 + "공격력".
        [Test]
        public void FormatBuffLine_Power_1픽_D_공격력_절대값()
        {
            //# Wraith Power 20 × 1.5 = 30.
            var buff = MakeBuff(ECardId.WraithDamageBoost, 1, EMonsterStatKind.Power, 1.5f);

            string line = CallFormatBuffLine(EMonster.Wraith, buff);

            StringAssert.StartsWith("D", line);
            StringAssert.Contains("공격력", line);
            StringAssert.Contains("×1.5", line);
            StringAssert.Contains("(20 → 30)", line);
        }

        //# Range — R 글자 + "사거리" + float 표시.
        [Test]
        public void FormatBuffLine_Range_1픽_R_사거리_float표시()
        {
            //# Hex Range 4.0 × 1.4 = 5.6.
            var buff = MakeBuff(ECardId.HexRangeBoost, 1, EMonsterStatKind.Range, 1.4f);

            string line = CallFormatBuffLine(EMonster.Hex, buff);

            StringAssert.StartsWith("R", line);
            StringAssert.Contains("사거리", line);
            StringAssert.Contains("×1.4", line);
            //# float 포맷 "0.0" — 4.0 → 5.6.
            StringAssert.Contains("(4.0 → 5.6)", line);
        }

        //# MoveSpeed — M 글자 + "이동속도".
        [Test]
        public void FormatBuffLine_MoveSpeed_1픽_M_이동속도()
        {
            //# Phantom MoveSpeed 2.0 × 1.5 = 3.0.
            var buff = MakeBuff(ECardId.PhantomMoveSpeedBoost, 1, EMonsterStatKind.MoveSpeed, 1.5f);

            string line = CallFormatBuffLine(EMonster.Phantom, buff);

            StringAssert.StartsWith("M", line);
            StringAssert.Contains("이동속도", line);
            StringAssert.Contains("×1.5", line);
            StringAssert.Contains("(2.0 → 3.0)", line);
        }

        //# Cooldown — S 글자 + "공격속도" + 역수 표시.
        //# CooldownMul 0.7 → 공격속도 ×(1/0.7) ≈ 1.43. 절대값은 cd 1.0s → 0.7s 단위 명시.
        [Test]
        public void FormatBuffLine_Cooldown_1픽_S_공격속도_역수표시()
        {
            //# Reaper Cooldown 1.0 × 0.7 = 0.7.
            var buff = MakeBuff(ECardId.ReaperAtkSpeed, 1, EMonsterStatKind.Cooldown, 0.7f);

            string line = CallFormatBuffLine(EMonster.Reaper, buff);

            StringAssert.StartsWith("S", line);
            StringAssert.Contains("공격속도", line);
            //# 1/0.7 = 1.4285... → "0.##" → "1.43".
            StringAssert.Contains("×1.43", line, "공격속도 = 1/cdMul = 1.43 (역수 변환)");
            StringAssert.Contains("cd 1.0s → 0.7s", line, "절대값은 cd 단위로 표시");
        }

        //# SlowFactor — P 글자 + "둔화 효과" + 코드 상수 (BaseSlowFactor) 사용 + "— 강화" 접미.
        //# BalanceConfig 에 SlowFactor 필드 없음 — Lair.Character.PlagueSlowOnHit.BaseSlowFactor (0.8) 사용.
        [Test]
        public void FormatBuffLine_SlowFactor_const_BaseSlowFactor_사용_강화_접미()
        {
            //# Plague Slow 0.8 × 0.75 = 0.6.
            var buff = MakeBuff(ECardId.PlagueSlowBoost, 1, EMonsterStatKind.SlowFactor, 0.75f);

            string line = CallFormatBuffLine(EMonster.Plague, buff);

            StringAssert.StartsWith("P", line);
            StringAssert.Contains("둔화 효과", line);
            //# Base 0.8 → Result 0.6.
            StringAssert.Contains("(0.8 → 0.6)", line);
            //# 강화 접미.
            StringAssert.EndsWith("— 강화", line, "SlowFactor 라벨 끝에 ' — 강화' 부가 (§2.5.5)");
            //# BaseSlowFactor const 가 단일 진실 — production 코드가 BalanceConfig 참조로 바뀌면 본 테스트 실패.
            Assert.AreEqual(0.8f, PlagueSlowOnHit.BaseSlowFactor,
                "BaseSlowFactor 상수가 변경됨 — 툴팁 표기 단일 진실 검토 필요");
        }

        //# ===== PickCount ≥ 2 배지 =====

        //# 2픽 — " ×2" 배지가 글자 다음에 표시.
        [Test]
        public void FormatBuffLine_2픽_배지_노출()
        {
            //# Hp 2픽: 1.5 × 1.5 = 2.25. Wisp Hp 200 → 450.
            var buff = MakeBuff(ECardId.WispHpBoost, pickCount: 2, EMonsterStatKind.Hp, 2.25f);

            string line = CallFormatBuffLine(EMonster.Wisp, buff);

            //# "H ×2 체력 ..." 또는 "H ×2 체력..." — prefix + 공백 + ×N + 공백 + 라벨.
            StringAssert.Contains("H ×2", line, "PickCount=2 일 때 'H ×2' prefix");
            StringAssert.Contains("×2.25", line, "곱연산 누적 2.25");
            StringAssert.Contains("(200 → 450)", line, "Base 200 × 2.25 = 450");
        }

        //# 3픽 — " ×3" 배지.
        [Test]
        public void FormatBuffLine_3픽_배지_3()
        {
            var buff = MakeBuff(ECardId.WispHpBoost, 3, EMonsterStatKind.Hp, 3.375f);
            string line = CallFormatBuffLine(EMonster.Wisp, buff);
            StringAssert.Contains("H ×3", line);
        }

        //# 1픽 — 배지 미노출 (확인용 negative).
        [Test]
        public void FormatBuffLine_1픽_배지_없음()
        {
            var buff = MakeBuff(ECardId.WraithDamageBoost, 1, EMonsterStatKind.Power, 1.5f);
            string line = CallFormatBuffLine(EMonster.Wraith, buff);

            //# 글자 직후 공백 + "공격력" — 배지 없음 확인 (배지 있으면 "D ×N 공격력").
            StringAssert.StartsWith("D 공격력", line.TrimStart(), "배지 없으면 글자 다음 바로 라벨");
        }

        //# ===== 비강화 카드가 들어와도 안전 (방어) =====

        //# 강화 외 카드 (예: Frenzy) — IconLetterFor 가 ' ' 반환. prefix 가 빈 문자열로 처리되어 라벨부터 시작.
        [Test]
        public void FormatBuffLine_비강화_카드는_글자_prefix_없이_라벨로_시작()
        {
            //# Source = Frenzy (ECardId, 강화 아님). IconLetterFor 반환값 letter ' '.
            var buff = MakeBuff(ECardId.Frenzy, 1, EMonsterStatKind.Hp, 1.5f);
            string line = CallFormatBuffLine(EMonster.Wisp, buff);

            //# 글자 prefix 빈 → " 체력 ..." (앞 공백 없으면 "체력" 으로 시작).
            //# production: prefix 빈 문자열 + pickBadge 빈 문자열 + " 체력 ..." → 첫 글자가 공백.
            StringAssert.Contains("체력", line);
            StringAssert.DoesNotContain("H", line, "강화 외 카드라 H 글자 없음");
        }

        //# ===== 알 수 없는 스탯 (default 분기) =====

        //# 회귀 — 알 수 없는 EMonsterStatKind 값이 들어와도 예외 없이 fallback 문자열 반환.
        [Test]
        public void FormatBuffLine_알수없는_스탯_fallback_문자열()
        {
            //# enum 캐스팅으로 미정의 값 주입.
            var buff = MakeBuff(ECardId.WispHpBoost, 1, (EMonsterStatKind)999, 1.5f);
            string line = CallFormatBuffLine(EMonster.Wisp, buff);

            StringAssert.Contains("알 수 없는 스탯", line);
        }
    }
}
