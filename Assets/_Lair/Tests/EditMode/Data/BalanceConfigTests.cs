using System.Reflection;
using System.Text.RegularExpressions;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# BalanceConfig(순수 C# 클래스) 기본 조회 + CreateDefault 검증.
    //# private _monsters 는 리플렉션으로 직접 주입한다(OverlayFromDto 검증 우회 — Hp-only 시드 허용).
    public class BalanceConfigTests
    {
        //# 리플렉션 — private _monsters 직접 주입 (SO/JsonUtility 비의존).
        private static void SetMonsters(BalanceConfig config, params BalanceConfig.MonsterStatRow[] rows)
        {
            FieldInfo fi = typeof(BalanceConfig).GetField("_monsters", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(fi, "BalanceConfig._monsters 필드 존재 확인 (production 시그니처 변경 감지)");
            fi.SetValue(config, rows);
        }

        [Test]
        public void GetMonster_등록된키_스탯반환()
        {
            BalanceConfig config = new BalanceConfig();
            //# EMonster.Wraith == 1 (Wisp=0, Wraith=1, ...)
            SetMonsters(config, new BalanceConfig.MonsterStatRow
            {
                Key = EMonster.Wraith,
                Stat = new BalanceConfig.CharacterStat { Hp = 500, Power = 20 }
            });

            BalanceConfig.CharacterStat stat = config.GetMonster(EMonster.Wraith);

            Assert.IsNotNull(stat);
            Assert.AreEqual(500, stat.Hp);
            Assert.AreEqual(20, stat.Power);
        }

        [Test]
        public void GetMonster_미등록키_null반환()
        {
            BalanceConfig config = new BalanceConfig();
            Assert.IsNull(config.GetMonster(EMonster.Phantom));
        }

        //# GetSpawnPeriod — 등록 행의 SpawnPeriod 값을 정확히 반환.
        [Test]
        public void GetSpawnPeriod_등록된키_주기반환()
        {
            BalanceConfig config = new BalanceConfig();
            //# EMonster.Wraith == 1. SpawnPeriod = 20.
            SetMonsters(config, new BalanceConfig.MonsterStatRow
            {
                Key = EMonster.Wraith,
                Stat = new BalanceConfig.CharacterStat { Hp = 500 },
                SpawnPeriod = 20f
            });

            Assert.AreEqual(20f, config.GetSpawnPeriod(EMonster.Wraith), 0.001f);
        }

        //# GetSpawnPeriod — 미등록 키는 fallback 9f 반환 + 경고.
        [Test]
        public void GetSpawnPeriod_미등록키_fallback9f()
        {
            LogAssert.Expect(LogType.Warning, new Regex("스폰 주기 미발견"));

            BalanceConfig config = new BalanceConfig();

            Assert.AreEqual(9f, config.GetSpawnPeriod(EMonster.Phantom), 0.001f);
        }

        //# CreateDefault — JSON 부재 크래시넷. 유효 hero + runDuration>0 (플레이 가능 최소값).
        [Test]
        public void CreateDefault_hero와_runDuration은_유효하다()
        {
            BalanceConfig c = BalanceConfig.CreateDefault();
            Assert.IsNotNull(c.Hero);
            Assert.Greater(c.Hero.Hp, 0);
            Assert.Greater(c.Hero.Power, 0);
            Assert.Greater(c.RunDuration, 0f);
        }

        //# CreateDefault — monsters 는 비어 있어 GetMonster null → BattleController 가 프리팹 기본 스탯 사용.
        //# (전체 밸런스표를 코드에 박지 않음 — JSON 이 정본.)
        [Test]
        public void CreateDefault_monsters는_비어있다()
        {
            LogAssert.Expect(LogType.Warning, new Regex("몬스터 스탯 미발견"));

            BalanceConfig c = BalanceConfig.CreateDefault();

            Assert.IsNull(c.GetMonster(EMonster.Wisp));
        }

        //# CreateDefault — thresholds 크래시넷도 유효해야 한다(카드 트리거 타이밍 fallback). active 5 / passive 9, 전부 양수.
        [Test]
        public void CreateDefault_thresholds_fallback가_유효하다()
        {
            BalanceConfig c = BalanceConfig.CreateDefault();
            Assert.AreEqual(5, c.ActiveThresholds.Length);
            Assert.AreEqual(9, c.PassiveThresholds.Length);
            foreach (float t in c.ActiveThresholds)
                Assert.Greater(t, 0f);
            foreach (float t in c.PassiveThresholds)
                Assert.Greater(t, 0f);
        }

        //# degraded-path — JSON 파싱 실패(null)면 CreateDefault 크래시넷이 그대로 서서 부팅 유지.
        //# BattleController 흐름 재현: c = CreateDefault(); dto = Parse(...); if (dto != null) c.OverlayFromDto(dto).
        [Test]
        public void JSON파싱실패시_CreateDefault가_유효hero로_부팅유지()
        {
            BalanceConfigDto dto = BalanceJsonLoader.Parse("{ 손상된 json");
            Assert.IsNull(dto, "손상 JSON → null (SO fallback 신호)");

            BalanceConfig c = BalanceConfig.CreateDefault();
            if (dto != null)
                c.OverlayFromDto(dto);

            Assert.IsNotNull(c.Hero, "파싱 실패해도 hero 는 non-null (부팅 계속)");
            Assert.Greater(c.Hero.Hp, 0);
            Assert.Greater(c.RunDuration, 0f);
        }

        //# degraded-path — 빈 JSON 도 null → CreateDefault 유지(부팅 계속).
        [Test]
        public void 빈JSON시_CreateDefault가_유효hero로_부팅유지()
        {
            BalanceConfigDto dto = BalanceJsonLoader.Parse("");
            BalanceConfig c = BalanceConfig.CreateDefault();
            if (dto != null)
                c.OverlayFromDto(dto);

            Assert.IsNotNull(c.Hero);
            Assert.Greater(c.Hero.Hp, 0);
        }
    }
}
