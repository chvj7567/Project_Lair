using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Lair.Character;
using Lair.Data;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Lair.EditorTools
{
    //# 일회용 에디터 툴(Rule 04 §3) — monster-grades.md §7.3-5·§9 규격대로 등급 몬스터 24종의 에셋을 1회 만든다.
    //# 시트 임포트·슬라이스 → 클립 120 → 오버라이드 컨트롤러 24 → Prefab Variant 24 → Addressable 등록(프리팹 24 + JSON 4).
    //# 입력 = Monsters2D/MonsterGrade_SheetSpec.json(굽기 도구가 기록). 실행 후 정상 확인되면 이 스크립트는 삭제한다(단일 진실 = 생성된 에셋).
    public static class MonsterGradeAssetBuilder
    {
        private const string MenuPath = "Lair/Build Monster Grade Assets (Run Once, Then Delete)";

        private const string SheetFolder = "Assets/_Lair/Art/Sprites/Monsters2D";
        private const string AnimFolder = "Assets/_Lair/Art/Animations/Monsters2D";
        private const string CharactersFolder = "Assets/_Lair/Art/Characters";
        private const string MaterialPath = "Assets/_Lair/Art/Materials/Mat_Monster2D.mat";
        private const string JsonFolder = "Assets/_Lair/Data/Json";
        private const string ResourceLabel = "Resource";
        private const float Pixel = 1f / 48f;
        private const float AuraSortingOrder = -2;
        private const float AuraHeight = 0.0055f;
        private const float HpBarMargin = 0.08f;

        private static readonly string[] StateNames = { "Idle", "Move", "Attack", "Hit", "Death" };
        private static readonly string[] NewJsonNames = { "Race", "Grade", "Race_Monster", "Grade_Promotion" };

        //# ---- SheetSpec 역직렬화 (JsonUtility) ----
        [Serializable] private class AnimState { public int frames; public int fps; public bool loop; }
        [Serializable] private class AnimSpec { public AnimState idle; public AnimState move; public AnimState attack; public AnimState hit; public AnimState death; }
        [Serializable] private class GradeSpec
        {
            public int grade; public string monster; public float scale; public float emissionFloor;
            public string glowColor; public string rimColor; public float auraOpacity; public int[] topY;
        }
        [Serializable] private class MonsterSpec
        {
            public string @base; public int cell; public int pad; public int gradeCell; public int[] bodyOffset;
            public int auraRadiusTexel; public AnimSpec anim; public GradeSpec[] grades;
        }
        [Serializable] private class SheetSpec { public MonsterSpec[] monsters; }

        [MenuItem(MenuPath)]
        public static void BuildAll()
        {
            string specPath = $"{SheetFolder}/MonsterGrade_SheetSpec.json";
            TextAsset specAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(specPath);
            if (specAsset == null)
            {
                Debug.LogError($"[MonsterGradeAssetBuilder] {specPath} 없음 — 굽기 도구(?export=1)를 먼저 실행하세요");
                return;
            }
            SheetSpec spec = JsonUtility.FromJson<SheetSpec>(specAsset.text);
            if (spec == null || spec.monsters == null || spec.monsters.Length != 6)
            {
                Debug.LogError("[MonsterGradeAssetBuilder] SheetSpec 파싱 실패 또는 종족 6개 아님");
                return;
            }

            EnsureFolder(AnimFolder);
            Sprite auraSprite = ImportAura();
            int clips = 0;
            int prefabs = 0;
            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (MonsterSpec monster in spec.monsters)
                {
                    foreach (GradeSpec grade in monster.grades)
                    {
                        ImportSheets(grade.monster, monster.gradeCell);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
            AssetDatabase.Refresh();

            foreach (MonsterSpec monster in spec.monsters)
            {
                EMonster baseMonster = (EMonster)Enum.Parse(typeof(EMonster), monster.@base);
                AnimatorOverrideController baseOverride = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>($"{AnimFolder}/{baseMonster}_2D.overrideController");
                if (baseOverride == null)
                {
                    Debug.LogError($"[MonsterGradeAssetBuilder] {baseMonster}_2D.overrideController 없음 — 1등급 에셋이 선행돼야 함");
                    return;
                }
                foreach (GradeSpec grade in monster.grades)
                {
                    Dictionary<string, Sprite> bodySprites = SpriteDict($"{SheetFolder}/{grade.monster}_Sheet.png");
                    Dictionary<string, AnimationClip> gradeClips = BuildClips(grade.monster, monster.anim, bodySprites);
                    clips += gradeClips.Count;
                    AnimatorOverrideController oc = BuildOverride(grade.monster, baseOverride, gradeClips);
                    BuildVariant(baseMonster, monster, grade, oc, bodySprites, auraSprite);
                    prefabs++;
                }
            }

            AssetDatabase.SaveAssets();
            RegisterAddressables(spec);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[MonsterGradeAssetBuilder] 완료 — Prefab Variant {prefabs}, 클립 {clips}, 오버라이드 컨트롤러 {prefabs}, Addressable(프리팹 {prefabs} + JSON {NewJsonNames.Length}). 정상 확인 후 이 스크립트를 삭제하세요(Rule 04 §3).");
        }

        //# ---- 텍스처 임포트 (monster-grades §7.1 — 현행 §7.3 과 같고 슬라이스 셀만 gradeCell) ----

        private static void ImportSheets(string monster, int cell)
        {
            Vector2 pivot = new Vector2(0.5f, 0f);
            ConfigureSheet($"{SheetFolder}/{monster}_Sheet_Emission.png", cell, pivot, false, false, null);
            ConfigureSheet($"{SheetFolder}/{monster}_Sheet.png", cell, pivot, true, true, $"{SheetFolder}/{monster}_Sheet_Emission.png");
            foreach (string tier in new[] { "T1", "T2", "T3" })
            {
                ConfigureSheet($"{SheetFolder}/{monster}_Sheet_{tier}_Emission.png", cell, pivot, false, false, null);
                ConfigureSheet($"{SheetFolder}/{monster}_Sheet_{tier}.png", cell, pivot, true, true, $"{SheetFolder}/{monster}_Sheet_{tier}_Emission.png");
            }
        }

        //# 6열 x 5행 그리드 슬라이스 — 이름 <파일명>_<행*6+열>, 행 0 = 이미지 상단(MonsterTierOverlay 가 끝 숫자로 몸 프레임과 매칭).
        private static void ConfigureSheet(string path, int cell, Vector2 pivot, bool srgb, bool alpha, string maskPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[MonsterGradeAssetBuilder] TextureImporter 없음: {path}");
                return;
            }
            string name = Path.GetFileNameWithoutExtension(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 48f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.sRGBTexture = srgb;
            importer.alphaIsTransparency = alpha;
            importer.maxTextureSize = 1024;

            List<SpriteMetaData> metas = new List<SpriteMetaData>();
            for (int r = 0; r < 5; r++)
            {
                for (int c = 0; c < 6; c++)
                {
#pragma warning disable 0618
                    metas.Add(new SpriteMetaData
                    {
                        name = $"{name}_{r * 6 + c}",
                        rect = new Rect(c * cell, (5 - r - 1) * cell, cell, cell),
                        pivot = pivot,
                        alignment = (int)SpriteAlignment.Custom,
                    });
#pragma warning restore 0618
                }
            }
#pragma warning disable 0618
            importer.spritesheet = metas.ToArray();
#pragma warning restore 0618

            if (string.IsNullOrEmpty(maskPath) == false)
            {
                Texture2D mask = AssetDatabase.LoadAssetAtPath<Texture2D>(maskPath);
                if (mask != null)
                {
                    importer.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_EmissionMask", texture = mask } };
                }
                else
                {
                    Debug.LogWarning($"[MonsterGradeAssetBuilder] 마스크 없음(보조 텍스처 미연결): {maskPath}");
                }
            }
            importer.SaveAndReimport();
        }

        private static Sprite ImportAura()
        {
            string path = $"{SheetFolder}/MonsterGradeAura.png";
            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogError($"[MonsterGradeAssetBuilder] {path} 없음");
                return null;
            }
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f;
            importer.spritePivot = new Vector2(0.5f, 0.5f);
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static Dictionary<string, Sprite> SpriteDict(string path)
        {
            Dictionary<string, Sprite> dict = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
            {
                if (asset is Sprite sprite)
                {
                    dict[sprite.name] = sprite;
                }
            }
            return dict;
        }

        //# ---- 클립 · 오버라이드 컨트롤러 ----

        private static Dictionary<string, AnimationClip> BuildClips(string monster, AnimSpec anim, Dictionary<string, Sprite> sprites)
        {
            AnimState[] states = { anim.idle, anim.move, anim.attack, anim.hit, anim.death };
            Dictionary<string, AnimationClip> clips = new Dictionary<string, AnimationClip>();
            for (int row = 0; row < StateNames.Length; ++row)
            {
                AnimState state = states[row];
                AnimationClip clip = new AnimationClip { frameRate = state.fps };
                AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = state.loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[state.frames];
                for (int i = 0; i < state.frames; ++i)
                {
                    sprites.TryGetValue($"{monster}_Sheet_{row * 6 + i}", out Sprite sprite);
                    keys[i] = new ObjectReferenceKeyframe { time = (float)i / state.fps, value = sprite };
                }
                AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite"), keys);
                CreateOrReplace(clip, $"{AnimFolder}/{monster}_{StateNames[row]}.anim");
                clips[StateNames[row]] = clip;
            }
            return clips;
        }

        //# 1등급 오버라이드의 부모 컨트롤러를 그대로 쓰고, 상태별 템플릿 클립(이름 끝 = 상태)만 등급 클립으로 바꾼다.
        private static AnimatorOverrideController BuildOverride(string monster, AnimatorOverrideController baseOverride, Dictionary<string, AnimationClip> clips)
        {
            AnimatorOverrideController oc = new AnimatorOverrideController(baseOverride.runtimeAnimatorController) { name = $"{monster}_2D" };
            List<KeyValuePair<AnimationClip, AnimationClip>> overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
            oc.GetOverrides(overrides);
            for (int i = 0; i < overrides.Count; ++i)
            {
                foreach (string state in StateNames)
                {
                    if (overrides[i].Key != null && overrides[i].Key.name.EndsWith(state, StringComparison.Ordinal))
                    {
                        overrides[i] = new KeyValuePair<AnimationClip, AnimationClip>(overrides[i].Key, clips[state]);
                        break;
                    }
                }
            }
            oc.ApplyOverrides(overrides);
            CreateOrReplace(oc, $"{AnimFolder}/{monster}_2D.overrideController");
            return oc;
        }

        //# ---- Prefab Variant (§9) ----

        private static void BuildVariant(EMonster baseMonster, MonsterSpec monster, GradeSpec grade, AnimatorOverrideController oc,
            Dictionary<string, Sprite> bodySprites, Sprite auraSprite)
        {
            string basePath = $"{CharactersFolder}/{baseMonster}.prefab";
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePath);
            if (basePrefab == null)
            {
                Debug.LogError($"[MonsterGradeAssetBuilder] 기준 프리팹 없음: {basePath}");
                return;
            }
            GameObject root = (GameObject)PrefabUtility.InstantiatePrefab(basePrefab);
            try
            {
                float rootScale = root.transform.localScale.x;
                float inverse = Mathf.Approximately(rootScale, 0f) ? 1f : 1f / rootScale;
                EMonster gradeMonster = (EMonster)Enum.Parse(typeof(EMonster), grade.monster);

                SetInt(root.GetComponent<MonsterTag>(), "_key", (int)gradeMonster);

                Transform visual = root.transform.Find("Visual2D");
                Animator animator = visual.GetComponent<Animator>();
                animator.runtimeAnimatorController = oc;
                SpriteRenderer body = visual.GetComponent<SpriteRenderer>();
                if (bodySprites.TryGetValue($"{grade.monster}_Sheet_0", out Sprite idle0))
                {
                    body.sprite = idle0;
                }
                //# 등급 크기는 시각만 — Visual2D·AuraSigil·AuraGrade 에만 곱한다(루트·콜라이더 불변).
                visual.localScale *= grade.scale;

                Transform enhance = visual.Find("Enhance");
                MonsterTierOverlay overlay = enhance.GetComponent<MonsterTierOverlay>();
                SetSprites(overlay, "_tier1Frames", SpriteList($"{SheetFolder}/{grade.monster}_Sheet_T1.png"));
                SetSprites(overlay, "_tier2Frames", SpriteList($"{SheetFolder}/{grade.monster}_Sheet_T2.png"));
                SetSprites(overlay, "_tier3Frames", SpriteList($"{SheetFolder}/{grade.monster}_Sheet_T3.png"));

                Transform sigil = root.transform.Find("AuraSigil");
                if (sigil != null)
                {
                    sigil.localScale *= grade.scale;
                }

                Color glow = ParseColor(grade.glowColor, SpeciesVisual.SpeciesGlowColor(baseMonster));
                BuildGradeAura(root.transform, auraSprite, glow, monster.auraRadiusTexel, grade, inverse);

                MonsterEnhancementVisual enhancement = root.GetComponent<MonsterEnhancementVisual>();
                SetFloat(enhancement, "_gradeEmissionFloor", grade.emissionFloor);
                SetBool(enhancement, "_useGradeGlowColor", grade.grade >= 3);
                SetColor(enhancement, "_gradeGlowColor", glow);
                //# HP바 하단 높이 = (S - topY + 1) / 48 x 등급 크기 + 0.08 — 강화 티어 0~3 (§3.2).
                float[] heights = new float[4];
                for (int t = 0; t < 4; ++t)
                {
                    heights[t] = (monster.gradeCell - grade.topY[t] + 1) * Pixel * grade.scale + HpBarMargin;
                }
                SetFloats(enhancement, "_hpBarHeightByTier", heights);

                PrefabUtility.SaveAsPrefabAsset(root, $"{CharactersFolder}/{grade.monster}.prefab");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        //# 바닥 등급 오라(§3.1) — 이름 "Aura" 접두라 피격·공격 색 플래시에서 제외된다. 불투명도 0 이면 그리지 않는다.
        private static void BuildGradeAura(Transform root, Sprite sprite, Color glow, int radiusTexel, GradeSpec grade, float inverse)
        {
            GameObject go = new GameObject("AuraGrade", typeof(SpriteRenderer));
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(0f, AuraHeight, 0f);
            go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            float diameter = radiusTexel * 2f * Pixel * grade.scale * inverse;
            go.transform.localScale = new Vector3(diameter, diameter, 1f);
            SpriteRenderer renderer = go.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            renderer.sortingOrder = (int)AuraSortingOrder;
            renderer.color = new Color(glow.r, glow.g, glow.b, grade.auraOpacity);
            renderer.enabled = grade.auraOpacity > 0f;
        }

        //# ---- Addressable 등록 (Rule 03 §2: 주소 = 파일명 = Enum 값명, 라벨 Resource) ----

        private static void RegisterAddressables(SheetSpec spec)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[MonsterGradeAssetBuilder] Addressables 미설정 — 등록 생략");
                return;
            }
            AddressableAssetGroup prefabGroup = GroupOf(settings, $"{CharactersFolder}/Wisp.prefab");
            AddressableAssetGroup jsonGroup = GroupOf(settings, $"{JsonFolder}/FormationSlot.json");
            if (prefabGroup == null || jsonGroup == null)
            {
                Debug.LogError("[MonsterGradeAssetBuilder] 기존 Wisp.prefab/FormationSlot.json 의 그룹을 찾지 못함 — 등록 생략");
                return;
            }
            foreach (MonsterSpec monster in spec.monsters)
            {
                foreach (GradeSpec grade in monster.grades)
                {
                    Register(settings, prefabGroup, $"{CharactersFolder}/{grade.monster}.prefab", grade.monster);
                }
            }
            foreach (string json in NewJsonNames)
            {
                Register(settings, jsonGroup, $"{JsonFolder}/{json}.json", json);
            }
        }

        private static AddressableAssetGroup GroupOf(AddressableAssetSettings settings, string assetPath)
        {
            AddressableAssetEntry entry = settings.FindAssetEntry(AssetDatabase.AssetPathToGUID(assetPath));
            return entry != null ? entry.parentGroup : null;
        }

        private static void Register(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogError($"[MonsterGradeAssetBuilder] 에셋 없음 — 등록 불가: {assetPath}");
                return;
            }
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, false, false);
            entry.address = address;
            entry.SetLabel(ResourceLabel, true, true, false);
        }

        //# ---- 공용 헬퍼 ----

        private static Sprite[] SpriteList(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToArray();
        }

        private static Color ParseColor(string hex, Color fallback)
        {
            return string.IsNullOrEmpty(hex) == false && ColorUtility.TryParseHtmlString(hex, out Color color) ? color : fallback;
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
                return;
            string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
            if (string.IsNullOrEmpty(parent))
                return;
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }

        private static void CreateOrReplace(Object asset, string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(asset, path);
        }

        private static SerializedProperty Prop(Object target, string field, out SerializedObject so)
        {
            so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[MonsterGradeAssetBuilder] 필드 없음: {target.GetType().Name}.{field}");
            }
            return prop;
        }

        private static void SetInt(Object target, string field, int value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.intValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloat(Object target, string field, float value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetBool(Object target, string field, bool value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.boolValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetColor(Object target, string field, Color value)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.colorValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetFloats(Object target, string field, float[] values)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
            {
                p.GetArrayElementAtIndex(i).floatValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetSprites(Object target, string field, Sprite[] values)
        {
            SerializedProperty p = Prop(target, field, out SerializedObject so);
            if (p == null)
                return;
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
            {
                p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
