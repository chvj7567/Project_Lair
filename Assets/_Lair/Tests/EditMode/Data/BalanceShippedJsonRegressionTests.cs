using System.IO;
using NUnit.Framework;
using UnityEngine;
using Lair.Data;

namespace Lair.Tests.Data
{
    //# 출시 JSON == asset 동작 보존 고정. spec §2.4 의 activeThresholds 5→9 드리프트 회귀를 박제한다.
    //# Data/Json 의 실제 정본 파일을 Parse → 구조/드리프트 필드를 검증(에디터 File.IO).
    public class BalanceShippedJsonRegressionTests
    {
        //# 정본 = Assets/_Lair/Data/Json (Application.dataPath = Assets). 플레이어의 StreamingAssets 사본이 아닌 git 추적 정본을 읽는다.
        private static string ShippedPath =>
            Path.Combine(Application.dataPath, "_Lair/Data/Json", BalanceJsonLoader.FileName);

        private static BalanceConfigDto LoadShipped()
        {
            string path = ShippedPath;
            Assert.IsTrue(File.Exists(path), $"출시 정본 JSON 이 없다(빌드 기본값 누락 회귀): {path}");
            string json = File.ReadAllText(path);
            BalanceConfigDto dto = BalanceJsonLoader.Parse(json);
            Assert.IsNotNull(dto, "출시 JSON 이 파싱되지 않는다(정본 손상)");
            return dto;
        }

        //# 핵심 드리프트 고정 — activeThresholds 는 asset 기준 정확히 [30,90,150,210,270] 5개여야 한다.
        [Test]
        public void 출시JSON_activeThresholds는_asset과_동일한_5개다()
        {
            BalanceConfigDto dto = LoadShipped();
            Assert.IsNotNull(dto.ActiveThresholds);
            Assert.AreEqual(5, dto.ActiveThresholds.Length, "activeThresholds 개수 드리프트(5→N) — 밸런스 변경 회귀");
            float[] expected = { 30f, 90f, 150f, 210f, 270f };
            for (int i = 0; i < expected.Length; i++)
                Assert.AreEqual(expected[i], dto.ActiveThresholds[i], $"activeThresholds[{i}] 불일치");
        }

        //# 나머지 정본 구조 고정 — passive 9개, runDuration 양수, monster 6종, 키 전부 EMonster 로 파싱.
        [Test]
        public void 출시JSON_구조가_기본값_규격과_일치한다()
        {
            BalanceConfigDto dto = LoadShipped();
            Assert.IsNotNull(dto.PassiveThresholds);
            Assert.AreEqual(9, dto.PassiveThresholds.Length);
            Assert.Greater(dto.RunDuration, 0f);
            Assert.IsNotNull(dto.Hero);
            Assert.Greater(dto.Hero.Hp, 0);
            Assert.IsNotNull(dto.Monsters);
            Assert.AreEqual(6, dto.Monsters.Count);
            foreach (MonsterStatRowDto row in dto.Monsters)
            {
                Assert.IsNotNull(row);
                Assert.IsTrue(System.Enum.TryParse(row.Key, out EMonster _), $"미지의 몬스터 키: {row.Key}");
                Assert.Greater(row.Stat.Hp, 0, $"{row.Key} Hp 비양수(제로행 회귀)");
            }
        }

        //# 종단 동작 보존 — 출시 JSON 을 코드 기본값에 오버레이해도 activeThresholds 가 5개로 유지된다.
        //# (Parse + OverlayFromDto 합성 경로가 밸런스를 바꾸지 않음을 코드로 고정.)
        [Test]
        public void 출시JSON을_기본값에_오버레이해도_activeThresholds_5개_유지()
        {
            BalanceConfigDto dto = LoadShipped();
            BalanceConfig c = new BalanceConfig();
            c.OverlayFromDto(dto);
            Assert.AreEqual(5, c.ActiveThresholds.Length);
            Assert.AreEqual(9, c.PassiveThresholds.Length);
        }

        //# 출하 데이터 회귀 — Plague 스폰 주기 = 10초 (구 PlagueSpawnerConfigTests 에서 이관, asset→JSON 정본화 후).
        //# 오버레이 후 GetSpawnPeriod 로 종단 검증 — JSON row 의 spawnPeriod 가 보존되는지 고정.
        [Test]
        public void 출시JSON_Plague_스폰주기_10초()
        {
            BalanceConfigDto dto = LoadShipped();
            BalanceConfig c = new BalanceConfig();
            c.OverlayFromDto(dto);
            Assert.AreEqual(10f, c.GetSpawnPeriod(EMonster.Plague), 0.001f,
                "출시 JSON 의 Plague 스폰 주기 = 10.0s (스폰 주기 SoT = Data/Json JSON)");
        }
    }
}
