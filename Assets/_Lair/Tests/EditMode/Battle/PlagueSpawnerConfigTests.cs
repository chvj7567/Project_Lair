using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 카드 리뉴얼 v0.6 본격 스위트 — Plague Spawner #4 의 세부 정합 회귀.
    //# 기존 SpawnerConfigTests 는 Plague ≥1 / 총 6개만 검증. 본 스위트는 #4 슬롯의
    //#   - _outputType = Plague(4)
    //#   - _spawnPeriod = 10.0s
    //#   - _initialDelay = 1.5s
    //# 까지 정확 검증해 game-designer 기획서 §5.1 ↔ 씬 ↔ 컨셉서 §3.1 (continuous-spawn-round.md) 동기화 보장.
    //# 회귀 의도: spec D9 / 기획서 §5 의 결정 락이 미래 씬 편집으로 깨지지 않도록 박제.
    public class PlagueSpawnerConfigTests
    {
        private const string BattleScenePath = "Assets/_Lair/Scenes/Battle.unity";

        //# Battle.unity YAML 안에서 Spawner MonoBehaviour 블록의 _outputType=4 (Plague) 인 슬롯의
        //# _spawnPeriod / _initialDelay 두 필드를 함께 추출 — 한 블록 안에서 3개 필드가 같이 떠야 정합.
        [Test]
        public void Plague_Spawner_period_10초_initialDelay_1점5초_정합()
        {
            string fullPath = Path.Combine(Application.dataPath, "..", BattleScenePath);
            Assert.IsTrue(File.Exists(fullPath), $"Battle.unity 부재: {fullPath}");

            string yaml = File.ReadAllText(fullPath);

            //# YAML 라인 단위로 분리하고, _outputType: 4 발견 위치 주변 30줄 안에서 _spawnPeriod / _initialDelay 추출.
            string[] lines = yaml.Split('\n');
            List<(float period, float initialDelay)> plagueSlots = new();
            for (int i = 0; i < lines.Length; ++i)
            {
                if (lines[i].TrimEnd('\r', '\n').EndsWith("_outputType: 4") == false)
                    continue;
                //# 동일 MonoBehaviour 블록 안 — 위·아래 50줄 윈도우 안에서 두 필드 탐색.
                float period = -1f;
                float initial = -1f;
                int from = System.Math.Max(0, i - 30);
                int to = System.Math.Min(lines.Length - 1, i + 30);
                for (int j = from; j <= to; ++j)
                {
                    string ln = lines[j].TrimEnd('\r', '\n').TrimStart(' ');
                    if (ln.StartsWith("_spawnPeriod:"))
                    {
                        float.TryParse(ln.Substring("_spawnPeriod:".Length).Trim(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out period);
                    }
                    else if (ln.StartsWith("_initialDelay:"))
                    {
                        float.TryParse(ln.Substring("_initialDelay:".Length).Trim(),
                            System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out initial);
                    }
                }
                plagueSlots.Add((period, initial));
            }

            Assert.AreEqual(1, plagueSlots.Count,
                "Plague Spawner 슬롯 정확히 1개 (기획서 §5.1)");
            Assert.AreEqual(10f, plagueSlots[0].period, 0.001f,
                "Plague Spawner #4 의 _spawnPeriod = 10.0s (기획서 §5.1 / §5.3)");
            Assert.AreEqual(1.5f, plagueSlots[0].initialDelay, 0.001f,
                "Plague Spawner #4 의 _initialDelay = 1.5s (Wisp #4 자리 보존)");
        }
    }
}
