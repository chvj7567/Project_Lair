using System;
using System.Collections.Generic;
using System.IO;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 한 판의 카드 픽과 결과를 수집해 jsonl 파일에 누적. 디버그/밸런싱 한정.
    //# BattleController 가 씬 로드마다 1개 생성 — 한 인스턴스 = 한 판.
    public class RunRecorder
    {
        //# 프로젝트 루트 Logs/lair_runs.jsonl (Application.dataPath 는 .../Assets)
        public static string LogPath => Path.Combine(
            Directory.GetParent(Application.dataPath).FullName, "Logs", "lair_runs.jsonl");

        private readonly List<string> _picks = new();

        //# 카드 선택 시 호출 — 픽 순서 누적.
        public void RecordPick(ECardId id) => _picks.Add(id.ToString());

        //# 전투 종료 시 1회 호출 — RunRecord 를 jsonl 한 줄로 append.
        public void FinishRun(BattleResult result, float deathTime, int survivingMonsters)
        {
            RunRecord record = new RunRecord
            {
                FinishedAt = DateTime.Now.ToString("o"),
                Result = result.ToString(),
                DeathTime = deathTime,
                Picks = new List<string>(_picks),
                SurvivingMonsters = survivingMonsters,
            };

            string path = LogPath;
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            File.AppendAllText(path, JsonUtility.ToJson(record) + "\n");
            Debug.Log($"[RunRecorder] 기록: {record.Result} / 사망 {record.DeathTime:0.0}s / 픽 {record.Picks.Count}");
        }
    }
}
