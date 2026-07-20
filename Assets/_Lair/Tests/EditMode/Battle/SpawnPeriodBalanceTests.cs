using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Battle;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# 스폰 주기 BalanceConfig 이관 (docs/design/spawn-period-balance.md) 본격 스위트.
    //# gameplay-programmer 스모크(BalanceConfigTests / SpawnerProgressTests / BalanceConfigSyncerTests) 위에
    //# 6종 전수 / setter 클램프 / 적용 순서 회귀 / ReplaceOutput 주기 유지 / BindSpawners 주입 로직을 망라한다.
    public class SpawnPeriodBalanceTests
    {
        //# ===== 공용 헬퍼 =====

        //# §2 기획서 확정 기본값 — EMonster 전수.
        private static readonly (EMonster key, int enumIndex, float period)[] _expected =
        {
            (EMonster.Wisp,    0,  9f),
            (EMonster.Wraith,  1, 20f),
            (EMonster.Reaper,  2, 12f),
            (EMonster.Hex,     3, 15f),
            (EMonster.Plague,  4, 10f),
            (EMonster.Phantom, 5,  6f),
        };

        //# 6종 전수 행을 가진 BalanceConfig 를 JsonUtility 로 구성 (UnityEditor 비의존, BalanceConfigTests 패턴).
        //# EMonster int 순서대로 _monsters 배열을 채운다.
        private static BalanceConfig CreateFullConfig()
        {
            BalanceConfig config = ScriptableObject.CreateInstance<BalanceConfig>();
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("{\"_monsters\":[");
            for (int i = 0; i < _expected.Length; ++i)
            {
                if (i > 0)
                {
                    sb.Append(",");
                }
                sb.Append($"{{\"Key\":{_expected[i].enumIndex},\"Stat\":{{\"Hp\":100}},\"SpawnPeriod\":{_expected[i].period}}}");
            }
            sb.Append("]}");
            JsonUtility.FromJsonOverwrite(sb.ToString(), config);
            return config;
        }

        private readonly List<Object> _created = new();

        private BalanceConfig TrackConfig(BalanceConfig c) { _created.Add(c); return c; }

        [TearDown]
        public void TearDown()
        {
            foreach (Object o in _created)
                if (o != null) Object.DestroyImmediate(o);
            _created.Clear();
        }

        //# 리플렉션 — Spawner private 필드 주입 (EditMode 직렬화 우회, SpawnerProgressTests 패턴).
        private static void SetPrivate(object target, string field, object value)
        {
            FieldInfo fi = target.GetType().GetField(field, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, $"{target.GetType().Name}.{field} 필드 존재 확인 (production 시그니처 변경 감지)");
            fi.SetValue(target, value);
        }

        private static void InvokeOnEnable(Component c)
        {
            MethodInfo mi = c.GetType().GetMethod("OnEnable", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(mi, "OnEnable 메서드 존재 확인");
            mi.Invoke(c, null);
        }

        private Spawner CreateSpawner(EMonster outputType, float spawnPeriod = 9f)
        {
            GameObject go = new GameObject("SpawnerUT");
            _created.Add(go);
            Spawner sp = go.AddComponent<Spawner>();
            SetPrivate(sp, "_outputType", outputType);
            SetPrivate(sp, "_spawnPeriod", spawnPeriod);
            SetPrivate(sp, "_initialDelay", 0f);
            InvokeOnEnable(sp);   //# _currentType = _outputType 초기화
            return sp;
        }

        //# ===== 1. GetSpawnPeriod — 6종 전수 정확값 =====

        [Test]
        public void GetSpawnPeriod_6종전수_정확값()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            foreach ((EMonster key, int _, float period) in _expected)
                Assert.AreEqual(period, config.GetSpawnPeriod(key), 0.001f, $"{key} 주기 = {period}");
        }

        [Test]
        public void GetSpawnPeriod_Wisp_9초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Wisp), 0.001f);
        }

        [Test]
        public void GetSpawnPeriod_Reaper_12초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(12f, config.GetSpawnPeriod(EMonster.Reaper), 0.001f);
        }

        [Test]
        public void GetSpawnPeriod_Phantom_6초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(6f, config.GetSpawnPeriod(EMonster.Phantom), 0.001f);
        }

        [Test]
        public void GetSpawnPeriod_Plague_10초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(10f, config.GetSpawnPeriod(EMonster.Plague), 0.001f);
        }

        [Test]
        public void GetSpawnPeriod_Wraith_20초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(20f, config.GetSpawnPeriod(EMonster.Wraith), 0.001f);
        }

        [Test]
        public void GetSpawnPeriod_Hex_15초()
        {
            BalanceConfig config = TrackConfig(CreateFullConfig());
            Assert.AreEqual(15f, config.GetSpawnPeriod(EMonster.Hex), 0.001f);
        }

        //# 일부 행만 등록된 config 에서 미등록 종 조회 → fallback 9f (+ 경고).
        [Test]
        public void GetSpawnPeriod_일부등록_미등록종_fallback9f()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig config = TrackConfig(ScriptableObject.CreateInstance<BalanceConfig>());
            //# Wisp(0) 행만 등록. Phantom(5) 은 미등록.
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":0,\"Stat\":{\"Hp\":100},\"SpawnPeriod\":9.0}]}",
                config);

            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Phantom), 0.001f);
        }

        //# _monsters 미초기화(null) — 빈 SO 조회 시 fallback 9f.
        [Test]
        public void GetSpawnPeriod_monsters_null_fallback9f()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig config = TrackConfig(ScriptableObject.CreateInstance<BalanceConfig>());
            //# CreateInstance 직후 _monsters == null.
            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Wisp), 0.001f);
        }

        //# _monsters 빈 배열 — fallback 9f.
        [Test]
        public void GetSpawnPeriod_monsters_빈배열_fallback9f()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig config = TrackConfig(ScriptableObject.CreateInstance<BalanceConfig>());
            JsonUtility.FromJsonOverwrite("{\"_monsters\":[]}", config);

            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Reaper), 0.001f);
        }

        //# ===== 2. SetBasePeriod — 절대 대입 / 클램프 / 큰 값 =====

        //# 인스펙터 9f 위에 절대 대입 — 곱연산 아님.
        [Test]
        public void SetBasePeriod_절대대입_기존값무관()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(20f);
            Assert.AreEqual(20f, sp.SpawnPeriod, 0.001f, "9 → 20 절대 대입 (9×20 아님)");
        }

        //# 0 입력 — period <= 0 가드로 무시, 기존 값 유지.
        [Test]
        public void SetBasePeriod_0입력_무시_기존값유지()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(0f);
            Assert.AreEqual(9f, sp.SpawnPeriod, 0.001f, "0 입력은 가드로 무시 — 기존 9 유지");
        }

        //# 음수 입력 — 무시, 기존 값 유지.
        [Test]
        public void SetBasePeriod_음수입력_무시_기존값유지()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 12f);
            sp.SetBasePeriod(-100f);
            Assert.AreEqual(12f, sp.SpawnPeriod, 0.001f, "음수 입력은 가드로 무시 — 기존 12 유지");
        }

        //# 양수지만 하한(0.05) 미만 — Mathf.Max(0.05f) 클램프.
        //# 0 < period < 0.05 는 가드(period<=0)를 통과하므로 클램프가 동작한다.
        [Test]
        public void SetBasePeriod_미세양수_0점05로_클램프()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(0.01f);
            Assert.AreEqual(0.05f, sp.SpawnPeriod, 0.0001f, "0.01 → Mathf.Max(0.05) 클램프");
        }

        //# 정확히 하한 0.05 — 그대로 유지.
        [Test]
        public void SetBasePeriod_정확히_0점05_유지()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(0.05f);
            Assert.AreEqual(0.05f, sp.SpawnPeriod, 0.0001f, "0.05 는 하한과 동일 — 유지");
        }

        //# 큰 값 (Wraith 20 보다 큰 임의값) — 클램프 없이 그대로.
        [Test]
        public void SetBasePeriod_큰값_그대로대입()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(999f);
            Assert.AreEqual(999f, sp.SpawnPeriod, 0.001f, "큰 값은 클램프 없이 그대로");
        }

        //# 연속 SetBasePeriod — 마지막 값만 남는다 (절대 대입 누적 아님).
        [Test]
        public void SetBasePeriod_연속호출_마지막값만()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(6f);
            sp.SetBasePeriod(15f);
            Assert.AreEqual(15f, sp.SpawnPeriod, 0.001f, "절대 대입은 누적 안 함 — 마지막 15");
        }

        //# ===== 3. 적용 순서 회귀 (핵심) — base 주입 → 카드 ScalePeriod 곱연산 =====

        //# 기획서 §3.3: SetBasePeriod(12) → ScalePeriod(0.5) = 6 (base 위에 곱연산).
        [Test]
        public void 적용순서_base12_곱0점5_결과6()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(12f);
            sp.ScalePeriod(0.5f);
            Assert.AreEqual(6f, sp.SpawnPeriod, 0.001f, "base 12 × 0.5 = 6 (기획서 §3.3)");
        }

        //# 기획서 §3.3 표 전체 — base 6 → ×0.8 → ×0.6 = 2.88 (Phantom 카드 누적 예시).
        [Test]
        public void 적용순서_base6_곱0점8_곱0점6_결과2점88()
        {
            Spawner sp = CreateSpawner(EMonster.Phantom, 9f);
            sp.SetBasePeriod(6f);
            sp.ScalePeriod(0.8f);
            Assert.AreEqual(4.8f, sp.SpawnPeriod, 0.001f, "6 × 0.8 = 4.8");
            sp.ScalePeriod(0.6f);
            Assert.AreEqual(2.88f, sp.SpawnPeriod, 0.001f, "4.8 × 0.6 = 2.88 (카드 2장 누적)");
        }

        //# 역순 회귀 박제 — ScalePeriod 먼저 후 SetBasePeriod 면 곱연산이 절대 대입으로 소실된다.
        //# 기획서 §3.3 "회귀 주의": base 주입이 카드 곱연산 이후 호출되면 누적 배율이 날아간다.
        //# 이 테스트는 잘못된 호출 순서의 동작을 박제해 두어, 향후 SetBasePeriod 가 곱연산화되는 등의
        //# 회귀를 감지한다.
        [Test]
        public void 적용순서_역순_곱연산먼저면_base절대대입에_소실()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.ScalePeriod(0.5f);          //# 9 × 0.5 = 4.5
            Assert.AreEqual(4.5f, sp.SpawnPeriod, 0.001f, "곱연산 선적용 = 4.5");
            sp.SetBasePeriod(12f);         //# 절대 대입 — 4.5 배율 소실, 12 로 덮어씀
            Assert.AreEqual(12f, sp.SpawnPeriod, 0.001f,
                "역순: SetBasePeriod 가 곱연산 결과(4.5)를 절대 대입(12)으로 덮어씀 — 배율 소실");
        }

        //# 정순/역순 결과가 다름을 명시 — 순서 의존성 박제.
        [Test]
        public void 적용순서_정순과_역순_결과상이()
        {
            Spawner forward = CreateSpawner(EMonster.Wisp, 9f);
            forward.SetBasePeriod(12f);
            forward.ScalePeriod(0.5f);     //# 정순 = 6

            Spawner reverse = CreateSpawner(EMonster.Wisp, 9f);
            reverse.ScalePeriod(0.5f);
            reverse.SetBasePeriod(12f);    //# 역순 = 12

            Assert.AreEqual(6f,  forward.SpawnPeriod, 0.001f, "정순 6");
            Assert.AreEqual(12f, reverse.SpawnPeriod, 0.001f, "역순 12");
            Assert.AreNotEqual(forward.SpawnPeriod, reverse.SpawnPeriod,
                "정순/역순 결과 상이 — 적용 순서가 BindSpawners(전투시작) → 카드픽 으로 보장돼야 함");
        }

        //# 곱연산 결과가 0.05 하한에 닿는 극단 — base 6 × 0.001 → Mathf.Max(0.05).
        [Test]
        public void 적용순서_과도한곱연산_0점05_클램프()
        {
            Spawner sp = CreateSpawner(EMonster.Phantom, 9f);
            sp.SetBasePeriod(6f);
            sp.ScalePeriod(0.001f);        //# 6 × 0.001 = 0.006 → 클램프 0.05
            Assert.AreEqual(0.05f, sp.SpawnPeriod, 0.0001f, "과도 곱연산도 0.05 하한 (폭주 스폰 방지)");
        }

        //# ===== 4. ReplaceOutput(융합) 시 주기 유지 회귀 =====

        //# 기획서 §3.4: ReplaceOutput 은 _spawnPeriod 를 건드리지 않는다 — 주기는 슬롯에 귀속 유지.
        [Test]
        public void ReplaceOutput_주입된base주기_유지()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(6f);              //# Phantom 주기 가정 주입
            sp.ReplaceOutput(EMonster.Wraith); //# 출력 종만 Wraith 로 변경
            Assert.AreEqual(EMonster.Wraith, sp.CurrentType, "출력 종은 변경");
            Assert.AreEqual(6f, sp.SpawnPeriod, 0.001f,
                "기획서 §3.4: 종 변경해도 주기는 슬롯에 귀속 유지 (Wraith 20 으로 재조회 안 함)");
        }

        //# 카드 곱연산까지 누적된 주기도 ReplaceOutput 후 유지 (슬롯 누적 효과 보존).
        [Test]
        public void ReplaceOutput_곱연산누적주기_유지()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(6f);
            sp.ScalePeriod(0.5f);              //# 6 × 0.5 = 3
            sp.ReplaceOutput(EMonster.Hex);
            Assert.AreEqual(3f, sp.SpawnPeriod, 0.001f,
                "종 변경 후에도 누적 곱연산 주기(3) 유지 — 슬롯 카드 효과 보존");
        }

        //# 연속 ReplaceOutput 중에도 주기 불변.
        [Test]
        public void ReplaceOutput_연속변경_주기불변()
        {
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.SetBasePeriod(10f);
            sp.ReplaceOutput(EMonster.Reaper);
            sp.ReplaceOutput(EMonster.Phantom);
            sp.ReplaceOutput(EMonster.Plague);
            Assert.AreEqual(EMonster.Plague, sp.CurrentType, "최종 종 = Plague");
            Assert.AreEqual(10f, sp.SpawnPeriod, 0.001f, "연속 종 변경에도 주기 10 불변");
        }

        //# ===== 5. BindSpawners 주입 로직 (EditMode 단위 — 루프 본문 재현) =====
        //# BattleController.BindSpawners 는 MonoBehaviour 의존(_vm/_zone/CHMUI)이 무거워 EditMode 에서
        //# 인스턴스화하지 않는다. 대신 §3.1 의사코드의 주입 루프 본문(_balance!=null 가드 + GetSpawnPeriod→SetBasePeriod)
        //# 을 실제 Spawner + BalanceConfig 로 그대로 재현해 계약을 검증한다. 씬 전체 BindSpawners 통합은
        //# PlayMode/수동 검증 범위.

        //# 주입 루프 — 각 스포너의 CurrentType 으로 종별 주기가 주입된다.
        [Test]
        public void BindSpawners주입_CurrentType별_종별주기적용()
        {
            BalanceConfig balance = TrackConfig(CreateFullConfig());
            Spawner wisp    = CreateSpawner(EMonster.Wisp, 9f);
            Spawner phantom = CreateSpawner(EMonster.Phantom, 9f);
            Spawner wraith  = CreateSpawner(EMonster.Wraith, 9f);

            //# §3.1 주입 루프 본문 재현.
            foreach (Spawner sp in new[] { wisp, phantom, wraith })
                if (balance != null)
                    sp.SetBasePeriod(balance.GetSpawnPeriod(sp.CurrentType));

            Assert.AreEqual(9f,  wisp.SpawnPeriod,    0.001f, "Wisp 9 주입");
            Assert.AreEqual(6f,  phantom.SpawnPeriod, 0.001f, "Phantom 6 주입");
            Assert.AreEqual(20f, wraith.SpawnPeriod,  0.001f, "Wraith 20 주입");
        }

        //# ReplaceOutput 으로 종이 바뀐 스포너도 BindSpawners 시점엔 그 시점 CurrentType 으로 조회.
        //# (BindSpawners 는 전투 시작 1회 — 일반적으로 OnEnable 직후 _outputType. 본 케이스는 조회 키 확인용.)
        [Test]
        public void BindSpawners주입_변경된CurrentType으로_조회()
        {
            BalanceConfig balance = TrackConfig(CreateFullConfig());
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);
            sp.ReplaceOutput(EMonster.Hex);     //# CurrentType = Hex(15)

            sp.SetBasePeriod(balance.GetSpawnPeriod(sp.CurrentType));

            Assert.AreEqual(15f, sp.SpawnPeriod, 0.001f, "변경된 CurrentType(Hex)로 주기 15 조회·주입");
        }

        //# _balance == null 가드 — 주입 생략, 인스펙터 기본값(9f) 유지 (기획서 §3.1 기존 동작 보전).
        [Test]
        public void BindSpawners주입_balance_null이면_주입생략_기본값유지()
        {
            BalanceConfig balance = null;
            Spawner sp = CreateSpawner(EMonster.Wisp, 9f);

            //# §3.1: _balance == null 이면 SetBasePeriod 호출 자체를 건너뜀.
            if (balance != null)
                sp.SetBasePeriod(balance.GetSpawnPeriod(sp.CurrentType));

            Assert.AreEqual(9f, sp.SpawnPeriod, 0.001f,
                "_balance null → 주입 생략 → 인스펙터 기본값 9 유지 (기존 동작 보전)");
        }

        //# config 에 미등록인 종을 출력하는 스포너 — fallback 9f 주입 (+ 경고).
        [Test]
        public void BindSpawners주입_미등록종_fallback9f주입()
        {
            UnityEngine.TestTools.LogAssert.Expect(LogType.Warning,
                new System.Text.RegularExpressions.Regex("스폰 주기 미발견"));

            BalanceConfig balance = TrackConfig(ScriptableObject.CreateInstance<BalanceConfig>());
            //# Wisp(0) 행만 등록 — Wraith 미등록.
            JsonUtility.FromJsonOverwrite(
                "{\"_monsters\":[{\"Key\":0,\"Stat\":{\"Hp\":100},\"SpawnPeriod\":9.0}]}",
                balance);
            Spawner sp = CreateSpawner(EMonster.Wraith, 5f);

            sp.SetBasePeriod(balance.GetSpawnPeriod(sp.CurrentType));

            Assert.AreEqual(9f, sp.SpawnPeriod, 0.001f, "미등록종 → fallback 9f 주입");
        }
    }
}
