using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# M4 — UICanvas 씬 배치 + BattleHud/ResultPopup 프리팹 자동 생성 + Addressables 등록.
    //# Rule 04 (프리팹화), Rule 08 (파일명 = Enum 값명) 자동 충족.
    public static class LairUIPrefabBuilder
    {
        public const string PrefabDir = "Assets/_Lair/Prefabs/UI";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";
        public const string UICanvasTag = "UICanvas";

        [MenuItem("Lair/Setup/M4 - Ensure UICanvas In Scene")]
        public static void EnsureUICanvas()
        {
            //# 1) "UICanvas" 태그가 TagManager 에 등록돼 있는지 보장
            EnsureTagExists(UICanvasTag);

            //# 2) 씬에서 Tag=UICanvas GameObject 검색
            GameObject canvasGo = GameObject.FindWithTag(UICanvasTag);
            if (canvasGo == null)
            {
                canvasGo = new GameObject("UICanvas");
                canvasGo.tag = UICanvasTag;
                canvasGo.layer = LayerMask.NameToLayer("UI");

                var canvas = canvasGo.AddComponent<Canvas>();
                //# ScreenSpaceCamera — 카메라 렌더 파이프라인 안에 캡처 가능. Camera.main 이
                //# 늦게 생성될 수 있으므로 호환 fallback: Camera.main 없으면 Overlay 로.
                var mainCam = Camera.main;
                if (mainCam != null)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = mainCam;
                    canvas.planeDistance = 10f;
                }
                else
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                }
                canvas.sortingOrder = 100;

                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();

                Debug.Log("[LairUIPrefabBuilder] UICanvas 생성 완료 (ScreenSpaceOverlay, 1920x1080)");
            }
            else
            {
                //# 기존 UICanvas — renderMode 를 ScreenSpaceCamera 로 갱신 (screenshot 캡처용)
                var canvas = canvasGo.GetComponent<Canvas>();
                var mainCam = Camera.main;
                if (canvas != null && mainCam != null && canvas.renderMode != RenderMode.ScreenSpaceCamera)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceCamera;
                    canvas.worldCamera = mainCam;
                    canvas.planeDistance = 10f;
                    canvas.sortingOrder = 100;
                    Debug.Log("[LairUIPrefabBuilder] UICanvas → ScreenSpaceCamera 로 변경");
                }
                else
                {
                    Debug.Log("[LairUIPrefabBuilder] UICanvas 이미 존재");
                }
            }

            //# 3) EventSystem 보장
            var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                es.AddComponent<StandaloneInputModule>();
                Debug.Log("[LairUIPrefabBuilder] EventSystem 생성 완료");
            }
            else
            {
                Debug.Log("[LairUIPrefabBuilder] EventSystem 이미 존재");
            }

            //# 4) 씬 dirty 마킹 (저장 가능 상태로)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        [MenuItem("Lair/Setup/M4 - Build UI Prefabs")]
        public static void BuildAllUIPrefabs()
        {
            EnsureDir(PrefabDir);

            //# Addressables 사전 확인 (M1 셋업이 안 됐을 경우 자동 호출)
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                LairSetup.EnsureAddressablesSetup();
                settings = AddressableAssetSettingsDefaultObject.Settings;
            }
            var group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                LairSetup.EnsureAddressablesSetup();
                group = settings.FindGroup(ResourceGroup);
            }

            BuildBattleHud(settings, group);
            BuildResultPopup(settings, group);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairUIPrefabBuilder] UI 프리팹 2종 빌드 완료");
        }

        [MenuItem("Lair/Setup/M4 - Run All")]
        public static void RunAllM4Setup()
        {
            EnsureUICanvas();
            BuildAllUIPrefabs();
            Debug.Log("[LairUIPrefabBuilder] M4 셋업 완료");
        }

        //# ---------- BattleHud 프리팹 ----------
        private static void BuildBattleHud(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "BattleHud";

            //# 루트
            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);

            //# BattleHud 컴포넌트 — Lair.UI.BattleHud (Lair 어셈블리)
            var hud = root.AddComponent<Lair.UI.BattleHud>();

            //# TimerText
            var timerGo = new GameObject("TimerText", typeof(RectTransform));
            timerGo.transform.SetParent(root.transform, false);
            var timerRt = (RectTransform)timerGo.transform;
            timerRt.anchorMin = new Vector2(0.5f, 1f);
            timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.pivot     = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0f, -40f);
            timerRt.sizeDelta = new Vector2(400f, 80f);
            var timerText = timerGo.AddComponent<Text>();
            timerText.text = "5:00";
            timerText.font = GetDefaultFont();
            timerText.fontSize = 48;
            timerText.alignment = TextAnchor.MiddleCenter;
            timerText.color = Color.white;

            //# HpBg
            var hpBgGo = new GameObject("HpBg", typeof(RectTransform));
            hpBgGo.transform.SetParent(root.transform, false);
            var hpBgRt = (RectTransform)hpBgGo.transform;
            hpBgRt.anchorMin = new Vector2(0.5f, 1f);
            hpBgRt.anchorMax = new Vector2(0.5f, 1f);
            hpBgRt.pivot     = new Vector2(0.5f, 1f);
            hpBgRt.anchoredPosition = new Vector2(0f, -90f);
            hpBgRt.sizeDelta = new Vector2(300f, 20f);
            var hpBgImg = hpBgGo.AddComponent<Image>();
            hpBgImg.color = ParseColor("#374151");

            //# HpFill (HpBg 자식, full stretch)
            var hpFillGo = new GameObject("HpFill", typeof(RectTransform));
            hpFillGo.transform.SetParent(hpBgGo.transform, false);
            var hpFillRt = (RectTransform)hpFillGo.transform;
            SetFullStretch(hpFillRt);
            var hpFillImg = hpFillGo.AddComponent<Image>();
            hpFillImg.color = ParseColor("#DC2626");
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFillImg.fillAmount = 1f;

            //# SerializeField 주입 — _timerText, _heroHpFill
            var so = new SerializedObject(hud);
            SetObjectField(so, "_timerText",  timerText);
            SetObjectField(so, "_heroHpFill", hpFillImg);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ---------- ResultPopup 프리팹 ----------
        private static void BuildResultPopup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "ResultPopup";

            //# 루트
            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);
            var popup = root.AddComponent<Lair.UI.ResultPopup>();

            //# Dim (full-stretch 검정 50%)
            var dimGo = new GameObject("Dim", typeof(RectTransform));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.color = new Color(0f, 0f, 0f, 0.5f);

            //# ResultText (center)
            var resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(root.transform, false);
            var resultRt = (RectTransform)resultGo.transform;
            resultRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultRt.pivot     = new Vector2(0.5f, 0.5f);
            resultRt.anchoredPosition = new Vector2(0f, 50f);
            resultRt.sizeDelta = new Vector2(600f, 200f);
            var resultText = resultGo.AddComponent<Text>();
            resultText.text = "결과";
            resultText.font = GetDefaultFont();
            resultText.fontSize = 96;
            resultText.alignment = TextAnchor.MiddleCenter;
            resultText.color = Color.white;

            //# RestartButton (center, below ResultText)
            var btnGo = new GameObject("RestartButton", typeof(RectTransform));
            btnGo.transform.SetParent(root.transform, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot     = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, -120f);
            btnRt.sizeDelta = new Vector2(240f, 80f);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;

            //# Button label
            var btnTextGo = new GameObject("ButtonText", typeof(RectTransform));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            SetFullStretch((RectTransform)btnTextGo.transform);
            var btnText = btnTextGo.AddComponent<Text>();
            btnText.text = "다시 시작";
            btnText.font = GetDefaultFont();
            btnText.fontSize = 36;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.black;

            //# SerializeField 주입 — _resultText, _restartButton
            var so = new SerializedObject(popup);
            SetObjectField(so, "_resultText",   resultText);
            SetObjectField(so, "_restartButton", btn);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ---------- 공통 헬퍼 ----------

        private static void SaveAndRegisterPrefab(GameObject root, string prefabName,
            AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            //# Addressables 등록 — address = 파일명 (Rule 08)
            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairUIPrefabBuilder] {prefabName} 빌드 완료 (address={entry.address}, label={ResourceLabel})");
        }

        private static void SetFullStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void SetObjectField(SerializedObject so, string fieldName, Object value)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out var c) ? c : Color.magenta;
        }

        private static Font GetDefaultFont()
        {
            //# Unity 2022.3 빌트인 — LegacyRuntime.ttf 우선, fallback Arial.ttf
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureTagExists(string tag)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[LairUIPrefabBuilder] TagManager.asset 로드 실패");
                return;
            }
            var tagManager = new SerializedObject(assets[0]);
            var tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
            {
                Debug.LogWarning("[LairUIPrefabBuilder] tags 프로퍼티 미발견");
                return;
            }

            for (int i = 0; i < tagsProp.arraySize; ++i)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }

            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
            Debug.Log($"[LairUIPrefabBuilder] Tag '{tag}' 추가");
        }
    }
}
