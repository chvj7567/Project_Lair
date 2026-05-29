using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using Lair.Data;

namespace Lair.EditorTools
{
    //# BalanceConfig SO ↔ balance_config.json 양방향 동기화.
    public static class BalanceConfigSyncer
    {
        private const string JsonPath   = "Assets/_Lair/Data/Json/balance_config.json";
        private const string ConfigPath = "Assets/_Lair/Data/BalanceConfig.asset";

        //# BalanceConfig SO → JSON 문자열 (공개 프로퍼티 경유, Enum 순회로 monster 목록 구성)
        public static string ExportToJson(BalanceConfig config)
        {
            List<MonsterStatRowDto> monsters = new List<MonsterStatRowDto>();
            foreach (EMonster monster in Enum.GetValues(typeof(EMonster)))
            {
                BalanceConfig.CharacterStat stat = config.GetMonster(monster);
                if (stat == null)
                    continue;
                monsters.Add(new MonsterStatRowDto
                {
                    Key  = monster.ToString(),
                    Stat = ToDto(stat)
                });
            }

            BalanceConfigDto dto = new BalanceConfigDto
            {
                Hero              = ToDto(config.Hero),
                Monsters          = monsters,
                RunDuration       = config.RunDuration,
                PassiveThresholds = config.PassiveThresholds,
                ActiveThresholds  = config.ActiveThresholds
            };

            return JsonConvert.SerializeObject(dto, JsonSyncSettings.Build());
        }

        //# DTO 를 BalanceConfig SO 에 적용. EMonster 파싱 실패 행은 skip + LogWarning.
        public static void ApplyDto(BalanceConfigDto dto, BalanceConfig config)
        {
            SerializedObject so = new SerializedObject(config);

            ApplyStatDto(so.FindProperty("_hero"), dto.Hero);

            //# 유효한 monsters 행만 필터링해 배열 크기를 결정
            List<MonsterStatRowDto> validRows = new List<MonsterStatRowDto>();
            foreach (MonsterStatRowDto row in dto.Monsters)
            {
                if (Enum.TryParse(row.Key, out EMonster _) == false)
                {
                    Debug.LogWarning($"[BalanceConfigSyncer] EMonster 파싱 실패 — skip: {row.Key}");
                    continue;
                }
                validRows.Add(row);
            }

            SerializedProperty monstersProp = so.FindProperty("_monsters");
            monstersProp.arraySize = validRows.Count;
            for (int i = 0; i < validRows.Count; ++i)
            {
                MonsterStatRowDto rowDto = validRows[i];
                Enum.TryParse(rowDto.Key, out EMonster monsterKey);
                SerializedProperty row = monstersProp.GetArrayElementAtIndex(i);
                row.FindPropertyRelative("Key").enumValueIndex = (int)monsterKey;
                ApplyStatDto(row.FindPropertyRelative("Stat"), rowDto.Stat);
            }

            so.FindProperty("_runDuration").floatValue = dto.RunDuration;

            SetFloatArray(so.FindProperty("_passiveThresholds"), dto.PassiveThresholds);
            SetFloatArray(so.FindProperty("_activeThresholds"),  dto.ActiveThresholds);

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        //# AssetDatabase 에서 BalanceConfig 로드 → JSON 저장
        public static void Export()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[BalanceConfigSyncer] BalanceConfig 없음: {ConfigPath}");
                return;
            }

            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, ExportToJson(config), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceConfigSyncer] Export → {JsonPath}");
        }

        //# JSON 파일 → BalanceConfig SO 갱신
        public static void Import()
        {
            BalanceConfig config = AssetDatabase.LoadAssetAtPath<BalanceConfig>(ConfigPath);
            if (config == null)
            {
                Debug.LogError($"[BalanceConfigSyncer] BalanceConfig 없음: {ConfigPath}");
                return;
            }

            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            BalanceConfigDto dto = JsonConvert.DeserializeObject<BalanceConfigDto>(json);

            ApplyDto(dto, config);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[BalanceConfigSyncer] Import ← {JsonPath}");
        }

        private static CharacterStatDto ToDto(BalanceConfig.CharacterStat stat) =>
            new CharacterStatDto
            {
                Hp        = stat.Hp,
                Power     = stat.Power,
                Range     = stat.Range,
                Cooldown  = stat.Cooldown,
                MoveSpeed = stat.MoveSpeed
            };

        private static void ApplyStatDto(SerializedProperty prop, CharacterStatDto dto)
        {
            prop.FindPropertyRelative("Hp").intValue          = dto.Hp;
            prop.FindPropertyRelative("Power").intValue       = dto.Power;
            prop.FindPropertyRelative("Range").floatValue     = dto.Range;
            prop.FindPropertyRelative("Cooldown").floatValue  = dto.Cooldown;
            prop.FindPropertyRelative("MoveSpeed").floatValue = dto.MoveSpeed;
        }

        private static void SetFloatArray(SerializedProperty prop, float[] values)
        {
            if (values == null)
                return;
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
                prop.GetArrayElementAtIndex(i).floatValue = values[i];
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
            {
                Directory.CreateDirectory(dir);
            }
        }
    }
}
