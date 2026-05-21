using System.IO;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# Slice C-M1 — BalanceConfig.asset 1회 생성 + 기획서 §11.3 현재값 사전 채움.
    //# 이미 존재하면 보존 (튜닝값 덮어쓰기 방지).
    public static class LairBalanceConfigSetup
    {
        public const string ConfigPath = "Assets/_Lair/Data/BalanceConfig.asset";

        //# (키, HP, 공격력, 사거리, 쿨다운, 이속) — 기획서 §11.3
        private static readonly (EMonster Key, int Hp, int Power, float Range, float Cd, float Ms)[] Monsters =
        {
            (EMonster.Slime,  200, 10, 1.0f, 1.0f, 1.5f),
            (EMonster.Golem,  500, 20, 1.3f, 1.0f, 0.8f),
            (EMonster.Orc,    100, 20, 1.0f, 0.5f, 2.5f),
            (EMonster.Archer,  60, 30, 5.0f, 1.0f, 2.0f),
            (EMonster.Spider,  50,  5, 1.0f, 1.0f, 2.0f),
            (EMonster.Bat,     30,  5, 1.0f, 0.8f, 3.5f),
        };

        [MenuItem("Lair/Setup/C - Create BalanceConfig")]
        public static void CreateBalanceConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath) != null)
            {
                Debug.LogWarning($"[LairBalanceConfigSetup] 이미 존재 — 보존: {ConfigPath}");
                return;
            }

            var dir = Path.GetDirectoryName(ConfigPath);
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            var config = ScriptableObject.CreateInstance<BalanceConfig>();
            AssetDatabase.CreateAsset(config, ConfigPath);

            var so = new SerializedObject(config);
            //# 영웅 — 기획서 §11.3 기사
            FillStat(so.FindProperty("_hero"), 1000, 50, 1.5f, 1.0f, 3.0f);

            var monsters = so.FindProperty("_monsters");
            monsters.arraySize = Monsters.Length;
            for (int i = 0; i < Monsters.Length; ++i)
            {
                var m = Monsters[i];
                var row = monsters.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Key").enumValueIndex = (int)m.Key;
                FillStat(row.FindPropertyRelative("Stat"), m.Hp, m.Power, m.Range, m.Cd, m.Ms);
            }
            //# _runDuration / _passiveThresholds / _activeThresholds 는 C# 필드 기본값 사용 — 별도 설정 불필요.

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            Debug.Log($"[LairBalanceConfigSetup] 생성 완료: {ConfigPath}");
        }

        private static void FillStat(SerializedProperty stat,
            int hp, int power, float range, float cd, float ms)
        {
            stat.FindPropertyRelative("Hp").intValue = hp;
            stat.FindPropertyRelative("Power").intValue = power;
            stat.FindPropertyRelative("Range").floatValue = range;
            stat.FindPropertyRelative("Cooldown").floatValue = cd;
            stat.FindPropertyRelative("MoveSpeed").floatValue = ms;
        }
    }
}
