using System.Collections.Generic;
using System.IO;
using Lair.Battle;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 밸런싱 디버그 윈도우 — 플레이 중 치트 6종 + 한 판 결과 히스토리.
    //# Rule 11 예외: 에디터 전용 UI.
    public class LairBalanceWindow : EditorWindow
    {
        private int _hpField = 500;
        private ECardId _cardPick = ECardId.SlimeHpBoost;
        private Vector2 _scroll;
        private List<RunRecord> _history;

        [MenuItem("Lair/Balance Window")]
        public static void ShowWindow() => GetWindow<LairBalanceWindow>("Lair Balance");

        private void OnEnable() => ReloadHistory();

        private void OnInspectorUpdate()
        {
            //# 플레이 중 치트 패널(BattleController 발견 여부) 갱신
            if (Application.isPlaying) Repaint();
        }

        private void OnGUI()
        {
            DrawCheatPanel();
            EditorGUILayout.Space(10);
            DrawHistoryPanel();
        }

        private void DrawCheatPanel()
        {
            EditorGUILayout.LabelField("치트", EditorStyles.boldLabel);

            if (Application.isPlaying == false)
            {
                EditorGUILayout.HelpBox("플레이 모드에서만 사용 가능", MessageType.Info);
                return;
            }

            var bc = Object.FindFirstObjectByType<BattleController>();
            if (bc == null)
            {
                EditorGUILayout.HelpBox("BattleController 미발견", MessageType.Warning);
                return;
            }

            if (GUILayout.Button("강제 패시브 트리거")) bc.DebugForcePassiveTrigger();
            if (GUILayout.Button("강제 액티브 트리거")) bc.DebugForceActiveTrigger();

            using (new EditorGUILayout.HorizontalScope())
            {
                _cardPick = (ECardId)EditorGUILayout.EnumPopup(_cardPick);
                if (GUILayout.Button("카드 즉시 적용", GUILayout.Width(110)))
                    bc.DebugApplyCard(_cardPick);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                _hpField = EditorGUILayout.IntField("영웅 HP", _hpField);
                if (GUILayout.Button("적용", GUILayout.Width(110)))
                    bc.DebugSetHeroHp(_hpField);
            }

            if (GUILayout.Button("영웅 즉사")) bc.DebugKillHero();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("전투 종료 — 승리")) bc.DebugEndBattle(BattleResult.Win);
                if (GUILayout.Button("전투 종료 — 패배")) bc.DebugEndBattle(BattleResult.Lose);
            }
        }

        private void DrawHistoryPanel()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField("결과 히스토리", EditorStyles.boldLabel);
                if (GUILayout.Button("새로고침", GUILayout.Width(80))) ReloadHistory();
                if (GUILayout.Button("초기화", GUILayout.Width(80))) ClearHistory();
            }

            if (_history == null || _history.Count == 0)
            {
                EditorGUILayout.HelpBox("기록 없음", MessageType.None);
                return;
            }

            //# 직전 판 강조
            var last = _history[_history.Count - 1];
            EditorGUILayout.LabelField(
                $"직전: {last.Result} / 사망 {FormatTime(last.DeathTime)} / 픽 {Count(last.Picks)} / 생존 {last.SurvivingMonsters}",
                EditorStyles.helpBox);

            _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.Height(200));
            for (int i = _history.Count - 1; i >= 0; --i)
            {
                var r = _history[i];
                EditorGUILayout.LabelField(
                    $"#{i + 1}  {r.Result}  사망 {FormatTime(r.DeathTime)}  픽 {Count(r.Picks)}  생존 {r.SurvivingMonsters}");
            }
            EditorGUILayout.EndScrollView();
        }

        private static int Count(List<string> list) => list != null ? list.Count : 0;

        private static string FormatTime(float sec)
            => $"{(int)(sec / 60)}:{(int)(sec % 60):00}";

        private void ReloadHistory()
        {
            _history = new List<RunRecord>();
            string path = RunRecorder.LogPath;
            if (File.Exists(path) == false) return;
            foreach (var line in File.ReadAllLines(path))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    _history.Add(JsonUtility.FromJson<RunRecord>(line));
                }
                catch (System.Exception)
                {
                    //# 손상된 줄은 건너뜀
                }
            }
        }

        private void ClearHistory()
        {
            string path = RunRecorder.LogPath;
            if (File.Exists(path)) File.Delete(path);
            _history = new List<RunRecord>();
        }
    }
}
