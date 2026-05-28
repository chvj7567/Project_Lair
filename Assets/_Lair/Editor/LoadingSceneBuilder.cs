using ChvjUnityInfra;
using Lair.Battle;
using Lair.UI;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Lair.Editor
{
    public static class LoadingSceneBuilder
    {
        [MenuItem("Lair/Build Loading Scene UI")]
        public static void BuildLoadingSceneUI()
        {
            //# Canvas
            GameObject canvasGo = new GameObject("Canvas");
            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();

            //# Panel (배경)
            GameObject panelGo = new GameObject("Panel");
            panelGo.transform.SetParent(canvasGo.transform, false);
            Image panelImg = panelGo.AddComponent<Image>();
            panelImg.color = new Color(0f, 0f, 0f, 0.8f);
            RectTransform panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.sizeDelta = Vector2.zero;

            //# ProgressBarBg
            GameObject barBgGo = new GameObject("ProgressBarBg");
            barBgGo.transform.SetParent(panelGo.transform, false);
            Image barBgImg = barBgGo.AddComponent<Image>();
            barBgImg.color = new Color(0.2f, 0.2f, 0.2f, 1f);
            RectTransform barBgRect = barBgGo.GetComponent<RectTransform>();
            barBgRect.anchoredPosition = new Vector2(0f, -30f);
            barBgRect.sizeDelta = new Vector2(600f, 30f);

            //# ProgressFill
            GameObject fillGo = new GameObject("ProgressFill");
            fillGo.transform.SetParent(barBgGo.transform, false);
            Image fillImg = fillGo.AddComponent<Image>();
            fillImg.color = new Color(0.2f, 0.7f, 1f, 1f);
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = 0;
            fillImg.fillAmount = 0f;
            RectTransform fillRect = fillGo.GetComponent<RectTransform>();
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.sizeDelta = Vector2.zero;

            //# PercentText
            GameObject percentGo = new GameObject("PercentText");
            percentGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI percentTmp = percentGo.AddComponent<TextMeshProUGUI>();
            percentTmp.text = "0%";
            percentTmp.alignment = TextAlignmentOptions.Center;
            percentTmp.fontSize = 24;
            CHText percentCHText = percentGo.AddComponent<CHText>();
            RectTransform percentRect = percentGo.GetComponent<RectTransform>();
            percentRect.anchoredPosition = new Vector2(0f, 10f);
            percentRect.sizeDelta = new Vector2(200f, 40f);

            //# DescText
            GameObject descGo = new GameObject("DescText");
            descGo.transform.SetParent(panelGo.transform, false);
            TextMeshProUGUI descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "로딩 중...";
            descTmp.alignment = TextAlignmentOptions.Center;
            descTmp.fontSize = 18;
            CHText descCHText = descGo.AddComponent<CHText>();
            RectTransform descRect = descGo.GetComponent<RectTransform>();
            descRect.anchoredPosition = new Vector2(0f, -70f);
            descRect.sizeDelta = new Vector2(500f, 40f);

            //# LoadingHud에 필드 연결
            LoadingHud hud = Object.FindObjectOfType<LoadingHud>();
            if (hud != null)
            {
                SerializedObject so = new SerializedObject(hud);
                so.FindProperty("_progressFill").objectReferenceValue = fillImg;
                so.FindProperty("_percentText").objectReferenceValue = percentCHText;
                so.FindProperty("_descText").objectReferenceValue = descCHText;
                so.ApplyModifiedProperties();
            }

            //# LoadingController에 _hud 연결
            LoadingController lc = Object.FindObjectOfType<LoadingController>();
            if (lc != null)
            {
                SerializedObject so = new SerializedObject(lc);
                so.FindProperty("_hud").objectReferenceValue = hud;
                so.ApplyModifiedProperties();
            }

            EditorUtility.SetDirty(canvasGo);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());

            Debug.Log("[LoadingSceneBuilder] Canvas 계층 생성 및 필드 연결 완료");
        }

        [MenuItem("Lair/Register JSON Addressables")]
        public static void RegisterJsonAddressables()
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LoadingSceneBuilder] Addressables Settings 없음 — Window > Asset Management > Addressables > Groups 에서 초기화 필요");
                return;
            }

            string defaultGroupName = "Default Local Group";
            AddressableAssetGroup group = settings.FindGroup(defaultGroupName)
                ?? settings.DefaultGroup;

            RegisterAsset(settings, group, "Assets/_Lair/Art/Json/LoadingStrings_Ko.json", "LoadingStrings_Ko");
            RegisterAsset(settings, group, "Assets/_Lair/Art/Json/Strings_Ko.json",         "Strings_Ko");

            AssetDatabase.SaveAssets();
            Debug.Log("[LoadingSceneBuilder] Addressables 등록 완료 — LoadingStrings_Ko, Strings_Ko");
        }

        private static void RegisterAsset(AddressableAssetSettings settings, AddressableAssetGroup group, string assetPath, string address)
        {
            string guid = AssetDatabase.AssetPathToGUID(assetPath);
            if (string.IsNullOrEmpty(guid))
            {
                Debug.LogWarning($"[LoadingSceneBuilder] GUID 없음: {assetPath} — 파일이 존재하는지 확인");
                return;
            }

            AddressableAssetEntry entry = settings.FindAssetEntry(guid)
                ?? settings.CreateOrMoveEntry(guid, group, false, false);

            entry.address = address;

            //# "Resource" 라벨 추가
            settings.AddLabel("Resource");
            entry.SetLabel("Resource", true, true, false);

            Debug.Log($"[LoadingSceneBuilder] 등록: {assetPath} → {address}");
        }

        [MenuItem("Lair/Register Loading Scene in Build Settings")]
        public static void AddToBuildSettings()
        {
            EditorBuildSettingsScene[] existing = EditorBuildSettings.scenes;
            string loadingPath = "Assets/_Lair/Scenes/Loading.unity";
            string battlePath  = "Assets/_Lair/Scenes/Battle.unity";

            System.Collections.Generic.List<EditorBuildSettingsScene> scenes =
                new System.Collections.Generic.List<EditorBuildSettingsScene>
                {
                    new EditorBuildSettingsScene(loadingPath, true),
                    new EditorBuildSettingsScene(battlePath,  true),
                };

            //# 기존 씬 중 Loading·Battle 이 아닌 것은 뒤에 유지
            foreach (EditorBuildSettingsScene s in existing)
            {
                if (s.path != loadingPath && s.path != battlePath)
                {
                    scenes.Add(s);
                }
            }

            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[LoadingSceneBuilder] Build Settings 등록 완료 — Loading(0), Battle(1)");
        }
    }
}
