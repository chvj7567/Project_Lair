using System.IO;
using Lair.Character;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 영웅 스킬 SO 3종 + 로드아웃 .asset 생성 + 로드아웃 Addressable 등록(EData.HeroSkillLoadout).
    //# 기본 수치는 기획서 §8 (단일 진실) 을 하드코딩 — 재실행 시 재현 가능. 이후 JSON Sync(Phase E)로도 튜닝.
    public static class LairHeroSkillAssetBuilder
    {
        public const string Dir = "Assets/_Lair/Art/Skills";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        [MenuItem("Lair/Setup/Build Hero Skill Assets")]
        public static void BuildAll()
        {
            EnsureDir(Dir);

            //# P1 Dash — 기획서 §2.1 / §8.
            DashStrikeSkillData dash = LoadOrCreate<DashStrikeSkillData>($"{Dir}/HeroSkill_DashStrike.asset");
            SetField(dash, "_displayName", "돌진 강타");
            SetField(dash, "_damage", 80);
            SetField(dash, "_cooldown", 3.0f);
            SetField(dash, "_dashLength", 7.0f);
            SetField(dash, "_coneHalfAngle", 35f);
            SetField(dash, "_knockbackStrength", 2.0f);
            SetField(dash, "_centroidRadius", 8.0f);
            EditorUtility.SetDirty(dash);

            //# P2 Orbit — 기획서 §2.2 / §8.
            OrbitingBladeSkillData orbit = LoadOrCreate<OrbitingBladeSkillData>($"{Dir}/HeroSkill_OrbitingBlade.asset");
            SetField(orbit, "_displayName", "궤도 블레이드");
            SetField(orbit, "_damage", 15);
            SetField(orbit, "_hitInterval", 0.3f);
            SetField(orbit, "_orbitRadius", 1.4f);
            SetField(orbit, "_bladeSphereRadius", 0.9f);
            SetField(orbit, "_bladeCount", 3);
            SetField(orbit, "_rotationSpeedDeg", 180f);
            EditorUtility.SetDirty(orbit);

            //# P3 Nova — 기획서 §2.3 / §8.
            AoeNovaSkillData nova = LoadOrCreate<AoeNovaSkillData>($"{Dir}/HeroSkill_AoeNova.asset");
            SetField(nova, "_displayName", "파멸의 노바");
            SetField(nova, "_damage", 100);
            SetField(nova, "_cooldown", 7.0f);
            SetField(nova, "_radius", 3.5f);
            SetField(nova, "_knockbackStrength", 3.0f);
            EditorUtility.SetDirty(nova);

            //# 로드아웃 — 페이즈 3개(90/60/30). 기획서 §8 순서.
            HeroSkillLoadout loadout = LoadOrCreate<HeroSkillLoadout>($"{Dir}/HeroSkillLoadout.asset");
            SerializedObject so = new SerializedObject(loadout);
            SerializedProperty phases = so.FindProperty("_phases");
            phases.ClearArray();
            AddPhase(phases, 0.9f, dash);
            AddPhase(phases, 0.6f, orbit);
            AddPhase(phases, 0.3f, nova);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(loadout);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            RegisterAddressable($"{Dir}/HeroSkillLoadout.asset", "HeroSkillLoadout");
            Debug.Log("[LairHeroSkillAssetBuilder] Hero Skill assets 빌드 완료 (Dash/Orbit/Nova + Loadout)");
        }

        private static void AddPhase(SerializedProperty phases, float hpFraction, Object skill)
        {
            int i = phases.arraySize;
            phases.InsertArrayElementAtIndex(i);
            SerializedProperty el = phases.GetArrayElementAtIndex(i);
            el.FindPropertyRelative("HpFraction").floatValue = hpFraction;
            el.FindPropertyRelative("Skill").objectReferenceValue = skill;
        }

        private static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            T a = AssetDatabase.LoadAssetAtPath<T>(path);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<T>();
                AssetDatabase.CreateAsset(a, path);
            }
            return a;
        }

        //# private [SerializeField] 필드 주입 (SerializedObject 경로 — GUID/직렬화 정합).
        private static void SetField(Object target, string fieldName, object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairHeroSkillAssetBuilder] 필드 미발견: {target.GetType().Name}.{fieldName}");
                return;
            }
            switch (value)
            {
                case int i: prop.intValue = i; break;
                case float f: prop.floatValue = f; break;
                case bool b: prop.boolValue = b; break;
                case string s: prop.stringValue = s; break;
                default: prop.objectReferenceValue = value as Object; break;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void RegisterAddressable(string assetPath, string address)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairHeroSkillAssetBuilder] Addressables 미설정");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, true, true, false);
            EditorUtility.SetDirty(settings);
        }

        private static void EnsureDir(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
