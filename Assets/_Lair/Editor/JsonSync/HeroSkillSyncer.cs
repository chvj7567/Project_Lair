using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEditor;
using UnityEngine;
using Lair.Character;

namespace Lair.EditorTools
{
    //# 영웅 스킬 SO 3종 + HeroSkillLoadout ↔ hero_skills.json 양방향 동기화.
    public static class HeroSkillSyncer
    {
        private const string JsonPath    = "Assets/_Lair/Data/Json/hero_skills.json";
        private const string SkillDir    = "Assets/_Lair/Art/Skills";
        private const string LoadoutPath = "Assets/_Lair/Art/Skills/HeroSkillLoadout.asset";

        private static JsonSerializerSettings Settings()
        {
            JsonSerializerSettings s = JsonSyncSettings.Build();
            s.Converters.Add(new HeroSkillDataConverter());
            return s;
        }

        public static void Export()
        {
            //# 모든 HeroSkillData .asset 수집 (Art/Skills).
            List<HeroSkillData> skills = new List<HeroSkillData>();
            foreach (string guid in AssetDatabase.FindAssets("t:HeroSkillData", new[] { SkillDir }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                HeroSkillData s = AssetDatabase.LoadAssetAtPath<HeroSkillData>(path);
                if (s != null)
                    skills.Add(s);
            }

            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(LoadoutPath);
            List<HeroSkillPhaseDto> phases = new List<HeroSkillPhaseDto>();
            if (loadout != null)
            {
                foreach (HeroSkillLoadout.Phase p in loadout.Phases)
                    phases.Add(new HeroSkillPhaseDto { HpFraction = p.HpFraction, Skill = p.Skill != null ? p.Skill.name : null });
            }

            HeroSkillsDto dto = new HeroSkillsDto { Skills = skills, Loadout = phases };
            EnsureDir(Path.GetDirectoryName(JsonPath));
            File.WriteAllText(JsonPath, JsonConvert.SerializeObject(dto, Settings()), System.Text.Encoding.UTF8);
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkillSyncer] Export → {JsonPath}");
        }

        public static void Import()
        {
            string json = File.ReadAllText(JsonPath, System.Text.Encoding.UTF8);
            //# 컨버터가 만든 in-memory SO 를 .asset 으로 반영하기 위해 JObject 로 fileName 도 읽는다.
            JObject root = JObject.Parse(json);
            JsonSerializer ser = JsonSerializer.Create(Settings());

            //# 스킬 — fileName 으로 기존 .asset 에 필드 적용(없으면 생성).
            foreach (JObject sj in root["skills"].Cast<JObject>())
            {
                string fileName = sj["fileName"]?.Value<string>();
                HeroSkillData parsed = (HeroSkillData)ser.Deserialize(sj.CreateReader(), typeof(HeroSkillData));
                if (parsed == null || string.IsNullOrEmpty(fileName))
                    continue;
                //# CopySerialized 는 m_Name 까지 복사 — parsed 의 빈 이름이 .asset 이름을 덮어 round-trip 손상.
                //# fileName 으로 강제 세팅 (CreateAsset 경로는 path 로 이미 세팅, 무해).
                parsed.name = fileName;

                string assetPath = $"{SkillDir}/{fileName}.asset";
                HeroSkillData existing = AssetDatabase.LoadAssetAtPath<HeroSkillData>(assetPath);
                if (existing == null || existing.GetType() != parsed.GetType())
                {
                    if (existing != null)
                    {
                        AssetDatabase.DeleteAsset(assetPath);   //# 타입 변경 시 교체
                    }
                    AssetDatabase.CreateAsset(parsed, assetPath);
                }
                else
                {
                    EditorUtility.CopySerialized(parsed, existing);   //# 필드 일괄 복사 → 기존 GUID 보존
                }
            }

            //# 로드아웃 — fileName ref 로 스킬 연결.
            HeroSkillsDto dto = JsonConvert.DeserializeObject<HeroSkillsDto>(json, Settings());
            HeroSkillLoadout loadout = AssetDatabase.LoadAssetAtPath<HeroSkillLoadout>(LoadoutPath);
            if (loadout != null)
            {
                SerializedObject so = new SerializedObject(loadout);
                SerializedProperty phases = so.FindProperty("_phases");
                phases.ClearArray();
                for (int i = 0; i < dto.Loadout.Count; ++i)
                {
                    phases.InsertArrayElementAtIndex(i);
                    SerializedProperty el = phases.GetArrayElementAtIndex(i);
                    el.FindPropertyRelative("HpFraction").floatValue = dto.Loadout[i].HpFraction;
                    HeroSkillData s = AssetDatabase.LoadAssetAtPath<HeroSkillData>($"{SkillDir}/{dto.Loadout[i].Skill}.asset");
                    el.FindPropertyRelative("Skill").objectReferenceValue = s;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(loadout);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[HeroSkillSyncer] Import ← {JsonPath}");
        }

        private static void EnsureDir(string dir)
        {
            if (Directory.Exists(dir) == false)
                Directory.CreateDirectory(dir);
        }
    }
}
