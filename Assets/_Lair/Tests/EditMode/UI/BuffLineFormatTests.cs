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
    //# 스포너 상태 UI — 영역 F (BuffLine.FormatBody 스탯별 본문 포맷, 기획서 §2.5.5 v0.9).
    //#
    //# v0.8 이주 — 기존 SpawnerStatusTooltip.FormatBuffLine 의 스탯 분기 로직은
    //# BuffLine.FormatBody (private static) 로 옮겨짐 (Rule 11 CHPoolingScrollView 완전 적용).
    //# 본 테스트는 그 redirect 결과 — FormatBody 의 본문 텍스트만 검증한다.
    //#
    //# 스코프:
    //#  - FormatBody 는 본문(체력/공격력/사거리/이동속도/공격속도/둔화 효과) 텍스트만 반환.
    //#  - H/D/S/R/M/P 글자 prefix · ×N 배지는 BuffLine.Bind 가 별도 UI 컴포넌트에
    //#    세팅하므로 FormatBody 스코프 밖. 본 테스트 대상 아님 (별도 BindTests 필요 시 분리).
    //#
    //# 검증:
    //#  - Hp:        "체력 ×1.5 (200 → 300)"
    //#  - Power:     "공격력 ×1.5 (20 → 30)"
    //#  - Range:     "사거리 ×1.4 (4.0 → 5.6)"
    //#  - MoveSpeed: "이동속도 ×1.5 (2.0 → 3.0)"
    //#  - Cooldown:  "공격속도 ×1.43 (cd 1.0s → 0.7s)"   ← 역수 변환
    //#  - SlowFactor:"둔화 효과 (0.8 → 0.6) — 강화"     ← PlagueSlowOnHit.BaseSlowFactor 상수
    //#  - default fallback: "(알 수 없는 스탯)"
    //#  - 누적 배율 차이 (1픽 1.5 vs 2픽 2.25 → 결과값 변화)
    //#  - balance == null 안전 (base 0 → result 1 로 clamp 또는 0 표시)
    //#  - AggregateMultiplier 0 cooldown 보호 분기 (aspeed = 0)
    public class BuffLineFormatTests
    {
        private BalanceConfig _balance;
        private CultureInfo _previousCulture;

        [SetUp]
        public void SetUp()
        {
            //# Unity 의 string interpolation 이 culture 영향 받지 않게 invariant 강제.
            _previousCulture = CultureInfo.CurrentCulture;
            CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

            _balance = ScriptableObject.CreateInstance<BalanceConfig>();
            //# 컨셉서 §11.3 기본 스탯 (기획서 §2.5.5 표) 주입.
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
        }

        [TearDown]
        public void TearDown()
        {
            if (_balance != null) Object.DestroyImmediate(_balance);
            CultureInfo.CurrentCulture = _previousCulture;
        }

        //# private static FormatBody(AppliedBuff, EMonster, BalanceConfig) → string 리플렉션 호출.
        //# v0.8 이주 후 시그니처: (BuffLine 의 private static).
        private static string CallFormatBody(BattleViewModel.AppliedBuff buff, EMonster type, BalanceConfig balance)
        {
            MethodInfo mi = typeof(BuffLine).GetMethod("FormatBody",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.IsNotNull(mi, "BuffLine.FormatBody (private static) 시그니처 존재 — production 시그니처 변경 감지");
            return (string)mi.Invoke(null, new object[] { buff, type, balance });
        }

        private static BattleViewModel.AppliedBuff MakeBuff(
            ECardId sourceId, int pickCount, EMonsterStatKind stat, float aggregateMul,
            ECardCategory category = ECardCategory.Enhance)
        {
            return new BattleViewModel.AppliedBuff
            {
                Source = FakeCardData.Create(sourceId, category),
                PickCount = pickCount,
                Stat = stat,
                AggregateMultiplier = aggregateMul,
            };
        }

        //# ===== 스탯별 본문 포맷 (1픽 케이스) =====

        //# Hp — "체력 ×1.5 (200 → 300)" (Wisp Hp 200 × 1.5 = 300).
        [Test]
        public void FormatBody_Hp_체력_절대값_올바름()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, pickCount: 1, EMonsterStatKind.Hp, 1.5f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert — "0.##" 포맷 1.5 → "1.5", 정수 변환 후 Hp 표시.
            StringAssert.Contains("체력", line);
            StringAssert.Contains("×1.5", line, "곱연산 누적 배율 1.5");
            StringAssert.Contains("(200 → 300)", line, "Base 200 × 1.5 = 300");
        }

        //# Power — "공격력 ×1.5 (20 → 30)" (Wraith Power 20 × 1.5 = 30).
        [Test]
        public void FormatBody_Power_공격력_절대값_올바름()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WraithDamageBoost, 1, EMonsterStatKind.Power, 1.5f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wraith, _balance);

            //# Assert
            StringAssert.Contains("공격력", line);
            StringAssert.Contains("×1.5", line);
            StringAssert.Contains("(20 → 30)", line);
        }

        //# Range — "사거리 ×1.4 (4.0 → 5.6)" — float "0.0" 포맷 (Hex Range 4.0 × 1.4 = 5.6).
        [Test]
        public void FormatBody_Range_사거리_float_포맷()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.HexRangeBoost, 1, EMonsterStatKind.Range, 1.4f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Hex, _balance);

            //# Assert — Range 는 float "0.0" 포맷.
            StringAssert.Contains("사거리", line);
            StringAssert.Contains("×1.4", line);
            StringAssert.Contains("(4.0 → 5.6)", line);
        }

        //# MoveSpeed — "이동속도 ×1.5 (2.0 → 3.0)" — float "0.0" 포맷 (Phantom MS 2.0 × 1.5 = 3.0).
        [Test]
        public void FormatBody_MoveSpeed_이동속도_float_포맷()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.PhantomMoveSpeedBoost, 1, EMonsterStatKind.MoveSpeed, 1.5f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Phantom, _balance);

            //# Assert
            StringAssert.Contains("이동속도", line);
            StringAssert.Contains("×1.5", line);
            StringAssert.Contains("(2.0 → 3.0)", line);
        }

        //# Cooldown — "공격속도 ×1.43 (cd 1.0s → 0.7s)" — 역수 변환.
        //# CooldownMul 0.7 → 공격속도 ×(1/0.7) ≈ 1.43, 절대값은 cd 1.0s → 0.7s 단위 명시.
        [Test]
        public void FormatBody_Cooldown_공격속도_역수표시()
        {
            //# Arrange — Reaper Cooldown 1.0 × 0.7 = 0.7.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.ReaperAtkSpeed, 1, EMonsterStatKind.Cooldown, 0.7f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Reaper, _balance);

            //# Assert — 1/0.7 = 1.4285... → "0.##" → "1.43".
            StringAssert.Contains("공격속도", line);
            StringAssert.Contains("×1.43", line, "공격속도 = 1/cdMul = 1.43 (역수 변환)");
            StringAssert.Contains("cd 1.0s → 0.7s", line, "절대값은 cd 단위 표시");
        }

        //# SlowFactor — "둔화 효과 (0.8 → 0.6) — 강화" — PlagueSlowOnHit.BaseSlowFactor 상수.
        //# BalanceConfig 에 SlowFactor 필드 없음 — Lair.Character.PlagueSlowOnHit.BaseSlowFactor (0.8) 사용.
        [Test]
        public void FormatBody_SlowFactor_const_BaseSlowFactor_사용_강화_접미()
        {
            //# Arrange — Plague Slow 0.8 × 0.75 = 0.6.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.PlagueSlowBoost, 1, EMonsterStatKind.SlowFactor, 0.75f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Plague, _balance);

            //# Assert
            StringAssert.Contains("둔화 효과", line);
            StringAssert.Contains("(0.8 → 0.6)", line);
            StringAssert.EndsWith("— 강화", line, "SlowFactor 라벨 끝에 ' — 강화' 부가 (§2.5.5)");

            //# BaseSlowFactor 상수가 단일 진실 — production 코드가 BalanceConfig 참조로 바뀌면 본 테스트 실패.
            Assert.AreEqual(0.8f, PlagueSlowOnHit.BaseSlowFactor,
                "BaseSlowFactor 상수 변경 — 툴팁 표기 단일 진실 검토 필요");
        }

        //# ===== 누적 배율 (PickCount 차이가 AggregateMultiplier 에 반영된 본문 결과) =====

        //# 2픽 — AggregateMultiplier 2.25 (1.5 × 1.5) → Hp 200 → 450.
        //# 배지 ×2 는 Bind 영역이라 본 테스트 범위 외 — 본문의 누적 배율 차이만 검증.
        [Test]
        public void FormatBody_Hp_2픽_누적_배율_본문_반영()
        {
            //# Arrange — Wisp Hp 2픽 누적 2.25 배율.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, pickCount: 2, EMonsterStatKind.Hp, 2.25f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert
            StringAssert.Contains("×2.25", line, "곱연산 누적 2.25");
            StringAssert.Contains("(200 → 450)", line, "Base 200 × 2.25 = 450");
        }

        //# 3픽 — AggregateMultiplier 3.375 (1.5³) → Hp 200 → 675.
        [Test]
        public void FormatBody_Hp_3픽_누적_배율_본문_반영()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, 3, EMonsterStatKind.Hp, 3.375f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert — "0.##" 는 마지막 0 절삭 → "3.38" (반올림).
            //# 3.375 → 0.## (두 자리, 셋째 자리 반올림) → "3.38".
            //# Mathf.RoundToInt(200 × 3.375) = 675.
            StringAssert.Contains("(200 → 675)", line, "Base 200 × 3.375 = 675");
        }

        //# 배율 1.0 (변화 없음) — Base 와 Result 가 같음.
        [Test]
        public void FormatBody_누적_배율_1배_변화없음()
        {
            //# Arrange — Wraith Power 20 × 1.0 = 20.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WraithDamageBoost, 1, EMonsterStatKind.Power, 1.0f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wraith, _balance);

            //# Assert — "×1" (0.## 는 trailing 0 절삭).
            StringAssert.Contains("(20 → 20)", line, "1.0 배율 — Base = Result");
        }

        //# ===== 엣지 / 회귀 =====

        //# fallback — 알 수 없는 EMonsterStatKind 값이 들어와도 예외 없이 "(알 수 없는 스탯)".
        [Test]
        public void FormatBody_알수없는_스탯_fallback_문자열()
        {
            //# Arrange — enum 캐스팅으로 미정의 값 주입.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, 1, (EMonsterStatKind)999, 1.5f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert
            StringAssert.Contains("알 수 없는 스탯", line);
        }

        //# balance == null 안전 — baseStat = null → base 값 0 으로 fallback (예외 없음).
        //# Hp: Mathf.Max(1, 0 × 1.5) = 1 (Max(1, RoundToInt(0)) = 1).
        [Test]
        public void FormatBody_balance_null_안전_base0_fallback()
        {
            //# Arrange
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, 1, EMonsterStatKind.Hp, 1.5f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, balance: null);

            //# Assert — Base 0, Result Max(1, 0)=1.
            StringAssert.Contains("체력", line, "balance null 이어도 라벨은 살아있음");
            StringAssert.Contains("(0 → 1)", line, "Base 0, Result Mathf.Max(1, 0) = 1");
        }

        //# Cooldown 배율 0 보호 분기 — line 101: `aspeed = mul > 0 ? 1f/mul : 0f`.
        //# 0 으로 나누는 경우가 안전하게 처리되어 ×0 이 나옴.
        [Test]
        public void FormatBody_Cooldown_AggregateMultiplier_0_안전_aspeed_0()
        {
            //# Arrange — 비현실적 입력이지만 production 의 division-by-zero 보호 분기 회귀.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.ReaperAtkSpeed, 1, EMonsterStatKind.Cooldown, 0f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Reaper, _balance);

            //# Assert — aspeed = 0 → "×0", resultCd = Max(0.05, 1.0 × 0) = 0.05.
            StringAssert.Contains("공격속도", line);
            StringAssert.Contains("×0", line, "AggregateMultiplier=0 보호 분기 — aspeed 0 처리");
        }

        //# Cooldown resultCd 최소 clamp — Max(0.05f, baseCd × mul). 매우 작은 배율도 0.05s 로 floor.
        [Test]
        public void FormatBody_Cooldown_resultCd_최소_0_05s_clamp()
        {
            //# Arrange — Reaper Cooldown 1.0 × 0.01 = 0.01 → clamp 0.05s.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.ReaperAtkSpeed, 1, EMonsterStatKind.Cooldown, 0.01f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Reaper, _balance);

            //# Assert — "0.1s" 가 아닌 "0.1s" 표시 ("0.0" 포맷 — 0.05 → "0.1" 반올림, 0.045 → "0.0").
            //# 0.05 의 "0.0" 포맷은 "0.1" (1 자리 반올림). 본문은 "0.1s" 포함.
            StringAssert.Contains("공격속도", line);
            StringAssert.Contains("cd 1.0s → 0.1s", line, "resultCd Max(0.05, 0.01) = 0.05 → '0.0' 포맷 '0.1'");
        }

        //# Hp Mathf.Max(1, ...) clamp — base 가 0 이고 배율도 0 이어도 결과 1.
        [Test]
        public void FormatBody_Hp_result_최소_1_clamp()
        {
            //# Arrange — balance null 로 base 0, AggregateMultiplier 0 → Max(1, 0) = 1.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.WispHpBoost, 1, EMonsterStatKind.Hp, 0f);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert — Base 200, Result Max(1, RoundToInt(200 × 0)) = Max(1, 0) = 1.
            StringAssert.Contains("(200 → 1)", line, "Hp Result 최소 1 clamp");
        }

        //# ===== v1.1 — Spawn 카테고리 본문 분기 (§2.5.5 v1.0) =====

        //# v1.1 정상 — Spawn 카드의 본문은 "동시 출력 +{PickCount}" 단일 포맷.
        //# Stat 필드는 default Hp 지만 Category=Spawn 분기로 stat switch 진입 전에 단정 반환.
        [Test]
        public void FormatBody_Spawn카드_동시출력_단일_포맷_v1점1()
        {
            //# Arrange — SpawnWisps 1픽. Stat 은 default Hp (TrackSpawnPick 컨벤션).
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.SpawnWisps, pickCount: 1, EMonsterStatKind.Hp, 1f,
                category: ECardCategory.Spawn);

            //# Act
            string line = CallFormatBody(buff, EMonster.Wisp, _balance);

            //# Assert — "체력" 키워드가 들어가면 안 됨 (Spawn 분기는 stat switch 진입 안 함).
            StringAssert.Contains("동시 출력 +1", line, "Spawn 카드 본문 = 동시 출력 +PickCount");
            Assert.IsFalse(line.Contains("체력"), "Spawn 분기는 stat switch 진입 X");
        }

        //# v1.1 엣지 — Spawn 카드 2픽 시 PickCount=2 가 본문에 반영. Stat 이 SlowFactor 같은 값이어도
        //# Category 분기로 stat 필드 무시 (TrackSpawnPick 이 default Hp 로 set 하지만 방어 검증).
        [Test]
        public void FormatBody_Spawn카드_2픽_Stat_무관_PickCount_반영_v1점1()
        {
            //# Arrange — SpawnPhantoms 2픽. Stat 을 비-default 로 설정해도 분기로 무시되는지 확인.
            BattleViewModel.AppliedBuff buff = MakeBuff(ECardId.SpawnPhantoms, pickCount: 2, EMonsterStatKind.SlowFactor, 1f,
                category: ECardCategory.Spawn);

            //# Act
            string line = CallFormatBody(buff, EMonster.Phantom, _balance);

            //# Assert — Stat=SlowFactor 였어도 "둔화 효과" 안 나오고 Spawn 단일 포맷.
            StringAssert.Contains("동시 출력 +2", line, "PickCount 2 본문 반영");
            Assert.IsFalse(line.Contains("둔화 효과"), "Spawn 분기는 SlowFactor stat 분기 무시");
        }
    }
}
