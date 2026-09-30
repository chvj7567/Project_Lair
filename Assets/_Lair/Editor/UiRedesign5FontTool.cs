using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

namespace Lair.EditorTools
{
    //# UI 도트 던전 리디자인 5단계 — Galmuri11 폰트 에셋 생성 + 프로젝트 전역 참조 교체 일회용 툴 (Rule 04 §3, 실행 후 삭제).
    //# 1) "Galmuri11 SDF.asset" 생성(이미 있으면 재생성하지 않음 = 멱등) — 한글 11,172자 + 라틴/기호를 Static 으로 굽는다.
    //# 2) Assets/_Lair 의 모든 프리팹·씬과 TMP_Settings 의 TMP 폰트를 Galmuri11 로 교체(Noto 는 삭제하지 않고 폴백으로 연결).
    //# 3) 고정 크기 텍스트만 Galmuri 배수(11/22/33...)로 최소 보정. Galmuri11.ttf 임포트 설정은 건드리지 않는다.
    //# 렌더 모드는 RASTER(비트맵) + 아틀라스 Point 필터 — SDF 는 도트 폰트를 뭉개므로 쓰지 않는다. 이름의 "SDF" 는 기존 명명 관례일 뿐이다.
    public static class UiRedesign5FontTool
    {
        private const string FontPath = "Assets/_Lair/Data/Fonts/Galmuri11.ttf";
        private const string AssetPath = "Assets/_Lair/Data/Fonts/Galmuri11 SDF.asset";
        private const string NotoPath = "Assets/_Lair/Data/Fonts/NotoSansKR SDF.asset";
        private const string SearchRoot = "Assets/_Lair";

        //# Galmuri11 은 11px 그리드 폰트 — 샘플링 크기 11, 패딩 2(점 필터라 번짐 방지용 최소), 2048 아틀라스(11,172자 ≈ 2.5M px 로 1장에 들어감, 넘치면 다중 아틀라스).
        private const int SamplingPointSize = 11;
        private const int AtlasPadding = 2;
        private const int AtlasSize = 2048;

        //# 크기 보정 규칙 — 11 의 배수 중 원래 크기의 0.72~1.25 배 안에서 가장 가까운 값. 범위 밖이면 조정하지 않고 보고한다.
        private const float SnapMinRatio = 0.72f;
        private const float SnapMaxRatio = 1.25f;

        private class Stats
        {
            public int Prefabs;
            public int Scenes;
            public int FontChanged;
            public int SizeChanged;
            public int InputChanged;
            public readonly Dictionary<float, int> UnadjustedSizes = new Dictionary<float, int>();
        }

        [MenuItem("Lair/UI/Apply Galmuri11 Font")]
        public static void Apply()
        {
            Font source = AssetDatabase.LoadAssetAtPath<Font>(FontPath);
            if (source == null)
            {
                Debug.LogError($"[UiRedesign5FontTool] 폰트 파일 없음(또는 미임포트): {FontPath}");
                return;
            }

            TMP_FontAsset noto = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(NotoPath);
            TMP_FontAsset galmuri = EnsureGalmuriAsset(source, noto);
            if (galmuri == null)
                return;

            Stats stats = new Stats();
            ApplyTmpSettings(galmuri, noto);
            ApplyPrefabs(galmuri, stats);
            ApplyScenes(galmuri, stats);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            LogSummary(stats);
        }

        //# ---------------- 폰트 에셋 ----------------

        private static TMP_FontAsset EnsureGalmuriAsset(Font source, TMP_FontAsset noto)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetPath);
            if (existing != null)
            {
                Debug.Log("[UiRedesign5FontTool] Galmuri11 SDF 이미 있음 — 재생성 없이 참조 교체만 진행(다시 만들려면 에셋 삭제 후 재실행)");
                EnsureFallback(existing, noto);
                return existing;
            }

            TMP_FontAsset created = TMP_FontAsset.CreateFontAsset(
                source, SamplingPointSize, AtlasPadding, GlyphRenderMode.RASTER, AtlasSize, AtlasSize,
                AtlasPopulationMode.Dynamic, true);
            if (created == null)
            {
                Debug.LogError("[UiRedesign5FontTool] TMP_FontAsset.CreateFontAsset 실패");
                return null;
            }

            created.name = "Galmuri11 SDF";
            AssetDatabase.CreateAsset(created, AssetPath);

            string characters = BuildCharacterSet();
            EditorUtility.DisplayProgressBar("Galmuri11", "글리프 굽는 중 (11,172자 + 기호)…", 0.5f);
            try
            {
                created.TryAddCharacters(characters, out string missing);
                if (string.IsNullOrEmpty(missing) == false)
                {
                    Debug.LogWarning($"[UiRedesign5FontTool] Galmuri11 에 없는 글리프 {missing.Length}자 — Noto 폴백이 메운다: {Preview(missing)}");
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            //# 한글 전체를 미리 구웠으므로 Static — 런타임 동적 생성(히치·읽기/쓰기 텍스처)이 필요 없다.
            SerializedObject createdObject = new SerializedObject(created);
            SerializedProperty population = createdObject.FindProperty("m_AtlasPopulationMode");
            if (population != null)
            {
                population.intValue = (int)AtlasPopulationMode.Static;
                createdObject.ApplyModifiedPropertiesWithoutUndo();
            }

            EnsureFallback(created, noto);
            SaveSubAssets(created);
            EditorUtility.SetDirty(created);
            AssetDatabase.SaveAssets();

            Debug.Log($"[UiRedesign5FontTool] Galmuri11 SDF 생성 — 글리프 {created.glyphTable.Count}개, 아틀라스 {created.atlasTextures.Length}장 "
                + $"({created.atlasTextures[0].width}x{created.atlasTextures[0].height}), 셰이더 {created.material.shader.name}");
            return created;
        }

        //# 머티리얼·아틀라스 텍스처를 폰트 에셋의 서브 에셋으로 저장하고 아틀라스는 Point 필터로 둔다.
        private static void SaveSubAssets(TMP_FontAsset fontAsset)
        {
            if (fontAsset.material != null && AssetDatabase.Contains(fontAsset.material) == false)
            {
                fontAsset.material.name = "Galmuri11 SDF Material";
                AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            }

            Texture2D[] atlases = fontAsset.atlasTextures;
            for (int i = 0; i < atlases.Length; ++i)
            {
                if (atlases[i] == null)
                    continue;
                atlases[i].filterMode = FilterMode.Point;
                if (AssetDatabase.Contains(atlases[i]) == false)
                {
                    atlases[i].name = i == 0 ? "Galmuri11 SDF Atlas" : $"Galmuri11 SDF Atlas {i}";
                    AssetDatabase.AddObjectToAsset(atlases[i], fontAsset);
                }
            }
        }

        private static void EnsureFallback(TMP_FontAsset fontAsset, TMP_FontAsset noto)
        {
            if (noto == null)
                return;
            if (fontAsset.fallbackFontAssetTable == null)
            {
                fontAsset.fallbackFontAssetTable = new List<TMP_FontAsset>();
            }

            if (fontAsset.fallbackFontAssetTable.Contains(noto) == false)
            {
                fontAsset.fallbackFontAssetTable.Add(noto);
                EditorUtility.SetDirty(fontAsset);
            }
        }

        //# 한글 완성형 + 자모 + ASCII/Latin-1 + 문장부호·화살표·도형·별 등 UI 에서 쓰는 기호 범위.
        private static string BuildCharacterSet()
        {
            StringBuilder sb = new StringBuilder(12000);
            AppendRange(sb, 0x0020, 0x007E);   //# ASCII
            AppendRange(sb, 0x00A0, 0x00FF);   //# Latin-1 (× ÷ · 등)
            AppendRange(sb, 0x2010, 0x2027);   //# 문장부호 (— ‘ ’ “ ” … ‧)
            AppendRange(sb, 0x2030, 0x203A);   //# ‰ ′ ″ ‹ ›
            AppendRange(sb, 0x2190, 0x2199);   //# 화살표
            AppendRange(sb, 0x25A0, 0x25FF);   //# 도형 (▶ ◀ ■ □ ● ○ ◆ ◇)
            AppendRange(sb, 0x2605, 0x2606);   //# ★ ☆
            AppendRange(sb, 0x2660, 0x266F);   //# ♠ ♥ ♪ 등
            AppendRange(sb, 0x3000, 0x303F);   //# CJK 문장부호
            AppendRange(sb, 0x3131, 0x318E);   //# 호환 자모
            AppendRange(sb, 0xAC00, 0xD7A3);   //# 한글 완성형 11,172자
            return sb.ToString();
        }

        private static void AppendRange(StringBuilder sb, int from, int to)
        {
            for (int c = from; c <= to; ++c)
            {
                sb.Append((char)c);
            }
        }

        private static string Preview(string text)
        {
            return text.Length <= 40 ? text : text.Substring(0, 40) + "…";
        }

        //# ---------------- TMP_Settings ----------------

        private static void ApplyTmpSettings(TMP_FontAsset galmuri, TMP_FontAsset noto)
        {
            TMP_Settings settings = TMP_Settings.instance;
            if (settings == null)
            {
                Debug.LogWarning("[UiRedesign5FontTool] TMP_Settings 를 찾지 못함 — 기본 폰트는 그대로");
                return;
            }

            SerializedObject so = new SerializedObject(settings);
            SerializedProperty defaultFont = so.FindProperty("m_defaultFontAsset");
            if (defaultFont != null)
            {
                defaultFont.objectReferenceValue = galmuri;
            }

            SerializedProperty fallbacks = so.FindProperty("m_fallbackFontAssets");
            if (fallbacks != null && noto != null && ContainsReference(fallbacks, noto) == false)
            {
                fallbacks.arraySize++;
                fallbacks.GetArrayElementAtIndex(fallbacks.arraySize - 1).objectReferenceValue = noto;
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            Debug.Log("[UiRedesign5FontTool] TMP_Settings 기본 폰트 = Galmuri11, 전역 폴백에 Noto 추가");
        }

        private static bool ContainsReference(SerializedProperty array, UnityEngine.Object target)
        {
            for (int i = 0; i < array.arraySize; ++i)
            {
                if (array.GetArrayElementAtIndex(i).objectReferenceValue == target)
                    return true;
            }

            return false;
        }

        //# ---------------- 프리팹 / 씬 ----------------

        private static void ApplyPrefabs(TMP_FontAsset galmuri, Stats stats)
        {
            string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { SearchRoot });
            for (int i = 0; i < guids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                EditorUtility.DisplayProgressBar("Galmuri11 — 프리팹", path, (float)i / guids.Length);
                GameObject root = null;
                try
                {
                    root = PrefabUtility.LoadPrefabContents(path);
                    int changed = ProcessTexts(root.GetComponentsInChildren<TMP_Text>(true), galmuri, stats);
                    changed += ProcessInputs(root.GetComponentsInChildren<TMP_InputField>(true), galmuri, stats);
                    if (changed > 0)
                    {
                        PrefabUtility.SaveAsPrefabAsset(root, path);
                        stats.Prefabs++;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError($"[UiRedesign5FontTool] 프리팹 처리 실패 {path}: {e.Message}");
                }
                finally
                {
                    if (root != null)
                    {
                        PrefabUtility.UnloadPrefabContents(root);
                    }
                }
            }

            EditorUtility.ClearProgressBar();
        }

        private static void ApplyScenes(TMP_FontAsset galmuri, Stats stats)
        {
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo() == false)
            {
                Debug.LogWarning("[UiRedesign5FontTool] 씬 저장 취소 — 씬 교체 건너뜀");
                return;
            }

            string originalScene = SceneManager.GetActiveScene().path;
            string[] guids = AssetDatabase.FindAssets("t:Scene", new[] { SearchRoot });
            for (int i = 0; i < guids.Length; ++i)
            {
                string path = AssetDatabase.GUIDToAssetPath(guids[i]);
                Scene scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
                int changed = ProcessTexts(UnityEngine.Object.FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None), galmuri, stats);
                changed += ProcessInputs(UnityEngine.Object.FindObjectsByType<TMP_InputField>(FindObjectsInactive.Include, FindObjectsSortMode.None), galmuri, stats);
                if (changed > 0)
                {
                    EditorSceneManager.MarkSceneDirty(scene);
                    EditorSceneManager.SaveScene(scene);
                    stats.Scenes++;
                }
            }

            if (string.IsNullOrEmpty(originalScene) == false)
            {
                EditorSceneManager.OpenScene(originalScene, OpenSceneMode.Single);
            }
        }

        //# TMP_Text 의 폰트/머티리얼/고정 크기를 갱신하고 바뀐 개수를 돌려준다.
        //# 중첩 프리팹 인스턴스의 값은 override 된 것만 만진다(원본 프리팹이 따로 처리되므로 불필요한 override 를 만들지 않는다).
        private static int ProcessTexts(TMP_Text[] texts, TMP_FontAsset galmuri, Stats stats)
        {
            int changed = 0;
            for (int i = 0; i < texts.Length; ++i)
            {
                TMP_Text text = texts[i];
                if (text == null)
                    continue;

                bool nested = PrefabUtility.IsPartOfPrefabInstance(text);
                SerializedObject so = new SerializedObject(text);
                SerializedProperty fontProp = so.FindProperty("m_fontAsset");
                bool touched = false;

                bool fontEditable = nested == false || (fontProp != null && fontProp.prefabOverride);
                if (fontEditable && text.font != galmuri)
                {
                    text.font = galmuri;
                    text.fontSharedMaterial = galmuri.material;
                    stats.FontChanged++;
                    touched = true;
                }

                SerializedProperty sizeProp = so.FindProperty("m_fontSize");
                SerializedProperty autoProp = so.FindProperty("m_enableAutoSizing");
                bool sizeEditable = nested == false || (sizeProp != null && sizeProp.prefabOverride);
                bool autoSizing = autoProp != null && autoProp.boolValue;
                if (sizeEditable && autoSizing == false)
                {
                    float current = text.fontSize;
                    float snapped = SnapSize(current);
                    if (Mathf.Approximately(snapped, current))
                    {
                        if (IsMultipleOfPoint(current) == false)
                        {
                            CountUnadjusted(stats, current);
                        }
                    }
                    else
                    {
                        text.fontSize = snapped;
                        stats.SizeChanged++;
                        touched = true;
                    }
                }

                if (touched)
                {
                    EditorUtility.SetDirty(text);
                    changed++;
                }
            }

            return changed;
        }

        private static int ProcessInputs(TMP_InputField[] inputs, TMP_FontAsset galmuri, Stats stats)
        {
            int changed = 0;
            for (int i = 0; i < inputs.Length; ++i)
            {
                TMP_InputField input = inputs[i];
                if (input == null)
                    continue;

                SerializedObject so = new SerializedObject(input);
                SerializedProperty property = so.FindProperty("m_GlobalFontAsset");
                if (property == null || property.objectReferenceValue == null || property.objectReferenceValue == galmuri)
                    continue;
                if (PrefabUtility.IsPartOfPrefabInstance(input) && property.prefabOverride == false)
                    continue;

                property.objectReferenceValue = galmuri;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(input);
                stats.InputChanged++;
                changed++;
            }

            return changed;
        }

        //# 11 의 배수 중 0.72~1.25 배 안에서 가장 가까운 값. 없으면 원래 크기 그대로.
        private static float SnapSize(float size)
        {
            if (size <= 0f)
                return size;

            float best = size;
            float bestDiff = float.MaxValue;
            for (int k = 1; k <= 12; ++k)
            {
                float multiple = SamplingPointSize * k;
                float ratio = multiple / size;
                if (ratio < SnapMinRatio || ratio > SnapMaxRatio)
                    continue;
                float diff = Mathf.Abs(multiple - size);
                if (diff < bestDiff)
                {
                    best = multiple;
                    bestDiff = diff;
                }
            }

            return best;
        }

        private static bool IsMultipleOfPoint(float size)
        {
            return Mathf.Approximately(size % SamplingPointSize, 0f);
        }

        private static void CountUnadjusted(Stats stats, float size)
        {
            stats.UnadjustedSizes.TryGetValue(size, out int count);
            stats.UnadjustedSizes[size] = count + 1;
        }

        private static void LogSummary(Stats stats)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append($"[UiRedesign5FontTool] 완료 — 프리팹 {stats.Prefabs}개, 씬 {stats.Scenes}개 저장, 폰트 교체 {stats.FontChanged}개, ");
            sb.Append($"크기 보정 {stats.SizeChanged}개, InputField 폰트 {stats.InputChanged}개.");
            if (stats.UnadjustedSizes.Count > 0)
            {
                List<float> sizes = new List<float>(stats.UnadjustedSizes.Keys);
                sizes.Sort();
                sb.Append(" 11 배수가 아니지만 보정 범위 밖이라 유지한 크기: ");
                for (int i = 0; i < sizes.Count; ++i)
                {
                    sb.Append($"{sizes[i]}({stats.UnadjustedSizes[sizes[i]]}) ");
                }
            }

            Debug.Log(sb.ToString());
        }
    }
}
