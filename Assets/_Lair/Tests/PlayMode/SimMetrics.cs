using System.Collections.Generic;
using System.IO;
using System.Text;
using Lair.Battle;
using Lair.Data;
using UnityEngine;

namespace Lair.Tests.PlayMode
{
    //# 시뮬레이션 인프라 — RunRecorder 가 누적한 jsonl(Logs/lair_runs.jsonl)에서
    //# 한 캠페인에 해당하는 RunRecord 만 잘라내 밸런스 메트릭으로 집계한다.
    //# 게임 로직을 건드리지 않고 기존 기록 파이프라인(RunRecorder)을 그대로 재사용.

    public static class SimMetrics
    {
        //# 캠페인 시작 시점의 jsonl 줄 수. 이전 판 기록과 이번 캠페인을 구분하는 기준선.
        public static int CountExistingLines()
        {
            string path = RunRecorder.LogPath;
            if (File.Exists(path) == false) return 0;
            int count = 0;
            foreach (string line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line) == false) count++;
            }
            return count;
        }

        //# baseline 이후에 추가된 RunRecord 만 파싱해 반환. 손상된 줄은 건너뜀.
        public static List<RunRecord> ReadSince(int baselineLineCount)
        {
            List<RunRecord> result = new List<RunRecord>();
            string path = RunRecorder.LogPath;
            if (File.Exists(path) == false) return result;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; ++i)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                //# baseline 이전 줄(이전 판)은 메트릭에서 제외.
                if (i < baselineLineCount) continue;
                try
                {
                    RunRecord rec = JsonUtility.FromJson<RunRecord>(lines[i]);
                    if (rec != null) result.Add(rec);
                }
                catch (System.Exception)
                {
                    //# 손상된 줄은 무시.
                }
            }
            return result;
        }

        //# RunRecord 목록 -> 밸런스 메트릭 요약 문자열. 컨셉 §8 기준 메트릭 포함.
        public static string Summarize(IReadOnlyList<RunRecord> records, string label)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"=== 시뮬레이션 메트릭 [{label}] ===");

            if (records == null || records.Count == 0)
            {
                sb.AppendLine("표본 0 — 집계할 RunRecord 없음.");
                return sb.ToString();
            }

            int n = records.Count;
            int wins = 0;
            float deathTimeSum = 0f;
            int survivorSum = 0;
            //# 카드별 픽 횟수 / 픽이 포함된 판의 승리 횟수.
            Dictionary<string, int> pickCount = new Dictionary<string, int>();
            Dictionary<string, int> pickWin = new Dictionary<string, int>();

            foreach (RunRecord r in records)
            {
                bool isWin = r.Result == BattleResult.Win.ToString();
                if (isWin) wins++;
                deathTimeSum += r.DeathTime;
                survivorSum += r.SurvivingMonsters;

                if (r.Picks == null) continue;
                //# 한 판에서 같은 카드를 여러 번 픽할 수 있으나, 픽률은 등장 횟수 기준으로 합산.
                HashSet<string> seenThisRun = new HashSet<string>();
                foreach (string pid in r.Picks)
                {
                    if (string.IsNullOrEmpty(pid)) continue;
                    pickCount.TryGetValue(pid, out int c);
                    pickCount[pid] = c + 1;
                    seenThisRun.Add(pid);
                }
                if (isWin)
                {
                    foreach (string pid in seenThisRun)
                    {
                        pickWin.TryGetValue(pid, out int w);
                        pickWin[pid] = w + 1;
                    }
                }
            }

            float avgDeath = deathTimeSum / n;
            float clearRate = (float)wins / n * 100f;
            float avgSurvivors = (float)survivorSum / n;

            sb.AppendLine($"표본(N): {n}판");
            sb.AppendLine($"평균 영웅 사망 시각: {FormatTime(avgDeath)} ({avgDeath:0.0}s) — 목표 2~4분");
            sb.AppendLine($"클리어율(승리): {clearRate:0.0}% ({wins}/{n})");
            sb.AppendLine($"평균 종료 시 생존 몬스터: {avgSurvivors:0.0}마리");
            sb.AppendLine($"빌드 다양성(서로 다른 픽 카드 종): {pickCount.Count}종");

            sb.AppendLine("카드별 픽 횟수 / 픽 포함 판 승리 수:");
            foreach (KeyValuePair<string, int> kv in pickCount)
            {
                pickWin.TryGetValue(kv.Key, out int w);
                sb.AppendLine($"  - {kv.Key}: 픽 {kv.Value}회, 픽-판 승리 {w}");
            }

            return sb.ToString();
        }

        private static string FormatTime(float sec)
            => $"{(int)(sec / 60)}:{(int)(sec % 60):00}";
    }
}
