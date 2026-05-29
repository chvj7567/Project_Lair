using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Lair > JSON Sync 에디터 창 — CardData · CardPool · BalanceConfig 양방향 동기화 트리거.
    public class LairJsonSyncWindow : EditorWindow
    {
        private const string JsonDir = "Assets/_Lair/Data/Json";

        [MenuItem("Lair/JSON Sync")]
        public static void Open() => GetWindow<LairJsonSyncWindow>("Lair JSON Sync");

        private void OnGUI()
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Export All → JSON", GUILayout.Height(30)))
            {
                ExportAll();
            }
            if (GUILayout.Button("Import All ← JSON", GUILayout.Height(30)))
            {
                ImportAll();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(12);
            DrawSection("Cards",          "cards.json",          CardDataSyncer.Export,       CardDataSyncer.Import);
            DrawSection("Card Pools",     "card_pools.json",     CardPoolSyncer.Export,       CardPoolSyncer.Import);
            DrawSection("Balance Config", "balance_config.json", BalanceConfigSyncer.Export,  BalanceConfigSyncer.Import);
        }

        private static void DrawSection(string label, string fileName, Action export, Action import)
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Export"))
            {
                export();
            }

            string fullPath = Path.Combine(JsonDir, fileName);
            bool fileExists = File.Exists(fullPath);
            GUI.enabled = fileExists;
            if (GUILayout.Button("Import"))
            {
                import();
            }
            GUI.enabled = true;

            if (fileExists == false)
            {
                EditorGUILayout.HelpBox($"{fileName} 없음 — Export 먼저", MessageType.None);
            }

            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(4);
        }

        private static void ExportAll()
        {
            CardDataSyncer.Export();
            CardPoolSyncer.Export();
            BalanceConfigSyncer.Export();
        }

        private static void ImportAll()
        {
            if (File.Exists(Path.Combine(JsonDir, "cards.json")))
            {
                CardDataSyncer.Import();
            }
            if (File.Exists(Path.Combine(JsonDir, "card_pools.json")))
            {
                CardPoolSyncer.Import();
            }
            if (File.Exists(Path.Combine(JsonDir, "balance_config.json")))
            {
                BalanceConfigSyncer.Import();
            }
        }
    }
}
