using System.IO;
using ChvjUnityInfra;
using TMPro;
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
        public const string PrefabDir = "Assets/_Lair/Art/UI";
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
                //# ScreenSpaceOverlay — 카메라 무관하게 항상 위에 그려짐 + GraphicRaycaster
                //# 클릭 안정성. screenshot_game 으로는 캡처 안 잡히지만 사용자 직접 Play 우선.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
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
                //# 기존 UICanvas — ScreenSpaceOverlay 로 정규화 (이전에 Camera 모드였을 수 있음)
                var canvas = canvasGo.GetComponent<Canvas>();
                if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.worldCamera = null;
                    canvas.sortingOrder = 100;
                    Debug.Log("[LairUIPrefabBuilder] UICanvas → ScreenSpaceOverlay 로 변경");
                }
                else
                {
                    Debug.Log("[LairUIPrefabBuilder] UICanvas 이미 존재");
                }
            }

            //# 3) EventSystem 보장 + Input System 호환 모듈 사용
            var existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                AddInputModule(es);
                Debug.Log("[LairUIPrefabBuilder] EventSystem 생성 완료 (InputSystemUIInputModule)");
            }
            else
            {
                //# 기존 EventSystem 의 InputModule 검사 — StandaloneInputModule 이면 교체
                var legacy = existingEventSystem.GetComponent<StandaloneInputModule>();
                if (legacy != null && existingEventSystem.GetComponent("UnityEngine.InputSystem.UI.InputSystemUIInputModule") == null)
                {
                    Object.DestroyImmediate(legacy);
                    AddInputModule(existingEventSystem.gameObject);
                    Debug.Log("[LairUIPrefabBuilder] EventSystem InputModule → InputSystemUIInputModule");
                }
                Debug.Log("[LairUIPrefabBuilder] EventSystem 이미 존재");
            }

            //# 4) 씬 dirty 마킹 (저장 가능 상태로)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        }

        //# Unity built-in 흰색 UI Sprite. Image.type=Filled 는 sprite 가 있어야 fillAmount 가 시각 적용됨.
        //# 다른 빌더(LairCharacterPrefabBuilder 의 몬스터 HP 바)도 재사용하므로 public.
        private static Sprite _uiSpriteCache;
        public static Sprite GetUISprite()
        {
            if (_uiSpriteCache != null) return _uiSpriteCache;
            //# 2022.3 표준 경로 — "UI/Skin/Background.psd" 가 9-slice white sprite
            _uiSpriteCache = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd")
                          ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            return _uiSpriteCache;
        }

        //# InputSystemUIInputModule 이 com.unity.inputsystem 패키지 어셈블리에 있음.
        //# 직접 type reference 대신 reflection 으로 attach — 패키지 의존성 부담 X.
        //# 실패 시 StandaloneInputModule 로 fallback.
        private static void AddInputModule(GameObject go)
        {
            //# 1순위: Input System Package 의 UI 모듈
            var type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type != null && typeof(BaseInputModule).IsAssignableFrom(type))
            {
                go.AddComponent(type);
                return;
            }
            //# 2순위: Legacy 모듈
            go.AddComponent<StandaloneInputModule>();
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
            BuildCardSelectionPopup(settings, group);   //# B1 추가

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
            //# Rule 11 — TMP_Text + CHText 래퍼 사용
            var timerTmp = timerGo.AddComponent<TextMeshProUGUI>();
            timerTmp.text = "5:00";
            timerTmp.font = TMP_Settings.defaultFontAsset;
            timerTmp.fontSize = 48f;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;
            var timerText = timerGo.AddComponent<CHText>();

            //# HpBg — TimerText 박스(top -40 ~ -120) 아래로 충분히 떨어뜨림 (간격 20)
            var hpBgGo = new GameObject("HpBg", typeof(RectTransform));
            hpBgGo.transform.SetParent(root.transform, false);
            var hpBgRt = (RectTransform)hpBgGo.transform;
            hpBgRt.anchorMin = new Vector2(0.5f, 1f);
            hpBgRt.anchorMax = new Vector2(0.5f, 1f);
            hpBgRt.pivot     = new Vector2(0.5f, 1f);
            hpBgRt.anchoredPosition = new Vector2(0f, -140f);
            hpBgRt.sizeDelta = new Vector2(400f, 24f);
            var hpBgImg = hpBgGo.AddComponent<Image>();
            hpBgImg.sprite = GetUISprite();
            hpBgImg.type = Image.Type.Sliced;
            hpBgImg.color = ParseColor("#374151");

            //# HpFill (HpBg 자식, full stretch)
            var hpFillGo = new GameObject("HpFill", typeof(RectTransform));
            hpFillGo.transform.SetParent(hpBgGo.transform, false);
            var hpFillRt = (RectTransform)hpFillGo.transform;
            SetFullStretch(hpFillRt);
            var hpFillImg = hpFillGo.AddComponent<Image>();
            hpFillImg.sprite = GetUISprite();
            hpFillImg.color = ParseColor("#DC2626");
            hpFillImg.type = Image.Type.Filled;
            hpFillImg.fillMethod = Image.FillMethod.Horizontal;
            hpFillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            hpFillImg.fillAmount = 1f;

            //# SerializeField 주입 — _timerText (CHText), _heroHpFill
            var so = new SerializedObject(hud);
            SetObjectField(so, "_timerText",  timerText);    //# CHText 컴포넌트
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
            dimImg.sprite = GetUISprite();
            dimImg.type = Image.Type.Sliced;
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
            //# Rule 11 — TMP_Text + CHText 래퍼 사용
            var resultTmp = resultGo.AddComponent<TextMeshProUGUI>();
            resultTmp.text = "결과";
            resultTmp.font = TMP_Settings.defaultFontAsset;
            resultTmp.fontSize = 96f;
            resultTmp.alignment = TextAlignmentOptions.Center;
            resultTmp.color = Color.white;
            var resultText = resultGo.AddComponent<CHText>();

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
            btnImg.sprite = GetUISprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            //# Rule 11 — CHButton 래퍼 추가 (사운드 hook + closeDisposable 친화 OnClick)
            var chBtn = btnGo.AddComponent<CHButton>();

            //# Button label — TMP_Text (CHButton.SetText 가 자식 TMP 자동 인식)
            var btnTextGo = new GameObject("ButtonText", typeof(RectTransform));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            SetFullStretch((RectTransform)btnTextGo.transform);
            var btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "다시 시작";
            btnTmp.font = TMP_Settings.defaultFontAsset;
            btnTmp.fontSize = 36f;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.black;

            //# SerializeField 주입 — _resultText (CHText), _restartButton (CHButton)
            var so = new SerializedObject(popup);
            SetObjectField(so, "_resultText",   resultText);
            SetObjectField(so, "_restartButton", chBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ---------- CardSelectionPopup 프리팹 ----------
        private static void BuildCardSelectionPopup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "CardSelectionPopup";

            //# 루트
            var root = new GameObject(PrefabName, typeof(RectTransform));
            SetFullStretch((RectTransform)root.transform);
            var popup = root.AddComponent<Lair.UI.CardSelectionPopup>();

            //# Dim
            var dimGo = new GameObject("Dim", typeof(RectTransform));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = GetUISprite();
            dimImg.type = Image.Type.Sliced;
            dimImg.color = new Color(0f, 0f, 0f, 0.65f);

            //# Title
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(root.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot     = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -60f);
            titleRt.sizeDelta = new Vector2(600f, 80f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "카드 선택";
            titleTmp.font = TMP_Settings.defaultFontAsset;
            titleTmp.fontSize = 48f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleGo.AddComponent<CHText>();

            //# CardsLayout — 3 slot 가로 배치
            var layoutGo = new GameObject("CardsLayout", typeof(RectTransform));
            layoutGo.transform.SetParent(root.transform, false);
            var layoutRt = (RectTransform)layoutGo.transform;
            layoutRt.anchorMin = new Vector2(0.5f, 0.5f);
            layoutRt.anchorMax = new Vector2(0.5f, 0.5f);
            layoutRt.pivot     = new Vector2(0.5f, 0.5f);
            layoutRt.anchoredPosition = Vector2.zero;
            layoutRt.sizeDelta = new Vector2(1080f, 460f);
            var hlg = layoutGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            //# 3개 CardView slot
            var slots = new Lair.UI.CardView[3];
            for (int i = 0; i < 3; ++i)
            {
                slots[i] = BuildCardViewSlot(layoutGo.transform, i);
            }

            //# SerializedObject 주입 — _slots 배열
            var so = new SerializedObject(popup);
            var slotsProp = so.FindProperty("_slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; ++i)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        private static Lair.UI.CardView BuildCardViewSlot(Transform parent, int index)
        {
            var slot = new GameObject($"CardView_{index}", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            var slotRt = (RectTransform)slot.transform;
            slotRt.sizeDelta = new Vector2(320f, 420f);

            //# Border (full stretch, 카테고리 색 — 런타임에 CardView 가 변경)
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(slot.transform, false);
            SetFullStretch((RectTransform)borderGo.transform);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = GetUISprite();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.gray;

            //# 카드 내부 흰 배경 (border 안쪽 약간 작게)
            var bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(slot.transform, false);
            var bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(8f, 8f);
            bgRt.offsetMax = new Vector2(-8f, -8f);
            var bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = GetUISprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            //# NameText
            var nameGo = new GameObject("NameText", typeof(RectTransform));
            nameGo.transform.SetParent(slot.transform, false);
            var nameRt = (RectTransform)nameGo.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot     = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -30f);
            nameRt.sizeDelta = new Vector2(0f, 60f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Name";
            nameTmp.font = TMP_Settings.defaultFontAsset;
            nameTmp.fontSize = 32f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.black;
            var nameText = nameGo.AddComponent<CHText>();

            //# DescText
            var descGo = new GameObject("DescText", typeof(RectTransform));
            descGo.transform.SetParent(slot.transform, false);
            var descRt = (RectTransform)descGo.transform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.7f);
            descRt.offsetMin = new Vector2(20f, 20f);
            descRt.offsetMax = new Vector2(-20f, -20f);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Description";
            descTmp.font = TMP_Settings.defaultFontAsset;
            descTmp.fontSize = 22f;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = Color.black;
            var descText = descGo.AddComponent<CHText>();

            //# PickButton (full stretch over the whole card)
            var btnGo = new GameObject("PickButton", typeof(RectTransform));
            btnGo.transform.SetParent(slot.transform, false);
            SetFullStretch((RectTransform)btnGo.transform);
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = GetUISprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(1, 1, 1, 0.001f);   //# 거의 투명, raycast 만 받음
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            var chBtn = btnGo.AddComponent<CHButton>();

            //# CardView 컴포넌트
            var cv = slot.AddComponent<Lair.UI.CardView>();
            var cvSo = new SerializedObject(cv);
            SetObjectField(cvSo, "_nameText", nameText);
            SetObjectField(cvSo, "_descText", descText);
            SetObjectField(cvSo, "_border", borderImg);
            SetObjectField(cvSo, "_pickButton", chBtn);
            cvSo.ApplyModifiedPropertiesWithoutUndo();

            return cv;
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

        private static void SetObjectField(SerializedObject so, string fieldName, UnityEngine.Object value)
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
