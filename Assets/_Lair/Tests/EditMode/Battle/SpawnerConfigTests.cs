using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Battle
{
    //# 카드 리뉴얼 Phase 1 Task 5 — Battle.unity 의 Spawner 6개 중 Plague Spawner ≥1 보장.
    //# 디버프 축 작동의 구조적 전제 (기획서 §5, continuous-spawn-round.md §3.1).
    //#
    //# SpawnerConfig 는 SO asset 이 아니라 씬 (Battle.unity) 의 Spawner 컴포넌트 인스펙터 값으로 직렬화.
    //# 본 테스트는 YAML 파일을 텍스트로 직접 읽어 Spawner 의 _outputType 값을 카운트 (EditMode 안전).
    //# EMonster 인덱스: Wisp=0, Wraith=1, Reaper=2, Hex=3, Plague=4, Phantom=5 (CommonEnum.cs).
    public class SpawnerConfigTests
    {
        private const string BattleScenePath = "Assets/_Lair/Scenes/Battle.unity";

        [Test]
        public void Battle_씬_Plague_스포너_최소_1개()
        {
            string fullPath = Path.Combine(
                Application.dataPath, "..", BattleScenePath.Replace("Assets/", "Assets/"));
            Assert.IsTrue(File.Exists(fullPath), $"Battle.unity 파일 부재: {fullPath}");

            string yaml = File.ReadAllText(fullPath);
            //# _outputType: 4 == EMonster.Plague.
            //# YAML 라인에서 정확히 `  _outputType: 4` 형태로 직렬화됨.
            int plagueCount = CountOutputType(yaml, 4);

            Assert.GreaterOrEqual(plagueCount, 1,
                "Plague Spawner ≥1 필요 — 디버프 축 작동의 구조 전제 (기획서 §5).");
        }

        [Test]
        public void Battle_씬_Spawner_총_6개()
        {
            string fullPath = Path.Combine(
                Application.dataPath, "..", BattleScenePath.Replace("Assets/", "Assets/"));
            string yaml = File.ReadAllText(fullPath);

            int total = 0;
            for (int monsterIdx = 0; monsterIdx <= 5; ++monsterIdx)
            {
                total += CountOutputType(yaml, monsterIdx);
            }

            Assert.AreEqual(6, total,
                "Spawner 6개 — continuous-spawn-round.md §3.1 정합 (Wisp1·Reaper1·Phantom1·Plague1·Wraith1·Hex1).");
        }

        //# YAML 본문에서 정확히 `_outputType: <value>` 라인 카운트.
        private static int CountOutputType(string yaml, int value)
        {
            //# 줄 단위 분리 후 trim 비교 — 다른 직렬화 컨텍스트(예: _outputType 이 다른 컴포넌트의 필드명) 와
            //# 충돌 가능성 적음 (Spawner.cs 만 _outputType 사용).
            string token = $"_outputType: {value}";
            int count = 0;
            int idx = 0;
            while ((idx = yaml.IndexOf(token, idx, System.StringComparison.Ordinal)) >= 0)
            {
                //# 부분일치 회피 — 다음 문자가 줄바꿈/공백/끝이어야 정확 매치.
                int next = idx + token.Length;
                if (next >= yaml.Length || yaml[next] == '\n' || yaml[next] == '\r')
                {
                    ++count;
                }
                idx = next;
            }
            return count;
        }
    }
}
