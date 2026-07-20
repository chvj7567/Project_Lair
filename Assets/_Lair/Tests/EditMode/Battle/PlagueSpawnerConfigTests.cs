using NUnit.Framework;
using UnityEditor;
using Lair.Data;

namespace Lair.Tests.Battle
{
    //# Plague 스폰 주기 정합 회귀 — 스폰 주기 SoT 가 BalanceConfig.asset 로 이관된 뒤 갱신.
    //# 씬 _spawnPeriod 는 원형 Arranger 재생성으로 기본값(9)이 되므로 더 이상 SoT 가 아니다.
    //# 본 스위트는 in-memory config(SpawnPeriodBalanceTests/BalanceConfigTests) 와 달리
    //# 실제 출하 asset 을 로드해 Plague=10 정합을 박제한다 (출하 데이터 회귀 감지).
    public class PlagueSpawnerConfigTests
    {
        private const string BalanceConfigPath = "Assets/_Lair/Data/BalanceConfig.asset";

        //# 실제 BalanceConfig.asset 의 Plague 주기가 10초로 정확한지 검증 (기획서 §5.1 / §5.3).
        [Test]
        public void Plague_스폰주기_10초_BalanceConfig_정합()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(BalanceConfigPath);
            Assert.IsNotNull(config, $"BalanceConfig.asset 로드 실패: {BalanceConfigPath}");

            Assert.AreEqual(10f, config.GetSpawnPeriod(EMonster.Plague), 0.001f,
                "출하 BalanceConfig.asset 의 Plague 스폰 주기 = 10.0s (스폰 주기 SoT 이관 후)");
        }
    }
}
