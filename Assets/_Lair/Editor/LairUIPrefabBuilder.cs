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

                Canvas canvas = canvasGo.AddComponent<Canvas>();
                //# ScreenSpaceOverlay — 카메라 무관하게 항상 위에 그려짐 + GraphicRaycaster
                //# 클릭 안정성. screenshot_game 으로는 캡처 안 잡히지만 사용자 직접 Play 우선.
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;

                CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                //# 기획서 v0.7~v0.9 의 모든 sizeDelta 단정이 1280×720 직접 기준 → CanvasScaler ref 도 동일.
                //# 1920×1080 ref 였을 때 ScaleFactor ~0.667 로 인해 패널 850 이 화면 567 (44%, 2/3 미달) 만 차지하던 문제 해소.
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                canvasGo.AddComponent<GraphicRaycaster>();

                Debug.Log("[LairUIPrefabBuilder] UICanvas 생성 완료 (ScreenSpaceOverlay, 1280x720)");
            }
            else
            {
                //# 기존 UICanvas — ScreenSpaceOverlay 로 정규화 (이전에 Camera 모드였을 수 있음)
                Canvas canvas = canvasGo.GetComponent<Canvas>();
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
            EventSystem existingEventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (existingEventSystem == null)
            {
                GameObject es = new GameObject("EventSystem");
                es.AddComponent<EventSystem>();
                AddInputModule(es);
                Debug.Log("[LairUIPrefabBuilder] EventSystem 생성 완료 (InputSystemUIInputModule)");
            }
            else
            {
                //# 기존 EventSystem 의 InputModule 검사 — StandaloneInputModule 이면 교체
                StandaloneInputModule legacy = existingEventSystem.GetComponent<StandaloneInputModule>();
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
            System.Type type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
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

            //# Addressables 사전 확인
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairUIPrefabBuilder] Addressables 미설정 — Window > Asset Management > Addressables Groups 로 초기화 필요");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                Debug.LogError("[LairUIPrefabBuilder] Addressables 'Resource' 그룹 미발견");
                return;
            }

            //# 스포너 상태 UI 프리팹이 BattleHud nesting 의 의존성이므로 사전 빌드 (advisor §3).
            //# 이미 빌드되어 있다면 PrefabUtility.SaveAsPrefabAsset 가 덮어쓴다 — idempotent.
            if (AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SpawnerStatusPanel.prefab") == null)
            {
                Debug.Log("[LairUIPrefabBuilder] SpawnerStatusPanel.prefab 미발견 — Spawner Status UI 빌더 선행 호출");
                LairSpawnerStatusUIBuilder.BuildAll();
            }

            BuildBuildIconCell();
            BuildBattleHud(settings, group);
            BuildResultPopup(settings, group);
            BuildCardSelectionPopup(settings, group);   //# B1 추가

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairUIPrefabBuilder] UI 프리팹 4종 빌드 완료");
        }

        [MenuItem("Lair/Setup/M4 - Run All")]
        public static void RunAllM4Setup()
        {
            EnsureUICanvas();
            //# 스포너 상태 UI 프리팹(SpawnerStatusPanel 등) 을 먼저 빌드 — BattleHud 가 nesting 함.
            LairSpawnerStatusUIBuilder.BuildAll();
            BuildAllUIPrefabs();
            Debug.Log("[LairUIPrefabBuilder] M4 셋업 완료");
        }

        //# ---------- BuildIconCell 프리팹 ----------
        //# CHMPool 로 스폰되는 빌드 패널 셀. Addressable 등록 X — BuildPanel 이 직접 참조.
        private static void BuildBuildIconCell()
        {
            const string PrefabName = "BuildIconCell";

            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(72f, 72f);

            //# Frame — 카테고리 색 (런타임에 BuildIconCell 이 변경), full stretch
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform));
            frameGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)frameGo.transform);
            Image frameImg = frameGo.AddComponent<Image>();
            frameImg.sprite = GetUISprite();
            frameImg.type = Image.Type.Sliced;
            frameImg.color = Color.gray;

            //# Icon — frame 안쪽 약간 작게
            GameObject iconGo = new GameObject("Icon", typeof(RectTransform));
            iconGo.transform.SetParent(root.transform, false);
            RectTransform iconRt = (RectTransform)iconGo.transform;
            iconRt.anchorMin = Vector2.zero;
            iconRt.anchorMax = Vector2.one;
            iconRt.offsetMin = new Vector2(6f, 6f);
            iconRt.offsetMax = new Vector2(-6f, -6f);
            Image iconImg = iconGo.AddComponent<Image>();
            iconImg.preserveAspect = true;

            //# CountText — 우하단 ×N 배지
            GameObject countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(root.transform, false);
            RectTransform countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 0f);
            countRt.pivot     = new Vector2(1f, 0f);
            countRt.anchoredPosition = new Vector2(-2f, 2f);
            countRt.sizeDelta = new Vector2(44f, 30f);
            TextMeshProUGUI countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 24f;
            countTmp.fontStyle = FontStyles.Bold;
            countTmp.alignment = TextAlignmentOptions.BottomRight;
            countTmp.color = Color.white;
            CHText countText = countGo.AddComponent<CHText>();

            //# Button + CHButton — 셀 전체 클릭 (Rule 11)
            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = frameImg;
            CHButton chBtn = root.AddComponent<CHButton>();

            //# BuildIconCell 컴포넌트 + 필드 주입
            Lair.UI.BuildIconCell cell = root.AddComponent<Lair.UI.BuildIconCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObjectField(so, "_iconImage",  iconImg);
            SetObjectField(so, "_frameImage", frameImg);
            SetObjectField(so, "_countText",  countText);
            SetObjectField(so, "_button",     chBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            //# 프리팹 저장 (Addressable 등록 안 함)
            string prefabPath = $"{PrefabDir}/{PrefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[LairUIPrefabBuilder] {PrefabName} 빌드 완료");
        }

        //# 빌드 패널의 한 섹션(패시브/액티브) — 라벨 + CHPoolingScrollView<BuildIconCell, BuildEntry> (§2.8.5 v0.8).
        //# v0.4: 상하 50:50 세로 분할. v0.5: 섹션마다 ScrollRect 1세트. v0.8: CHPoolingScrollView 완전 적용.
        //# v0.9 B1: _columnCount = 1 직박이 (auto 시 viewport 419 / itemSize 72 = 5 멀티 컬럼화 위험).
        //# v0.9 P1: _itemSize 는 dead serialization — BuildIconCell.prefab sizeDelta (72, 72) 가 단일 진실.
        //# 반환값: BuildIconPoolingScrollView 컴포넌트 (BuildPanel.cs 의 _passiveScrollView/_activeScrollView 가 가리킴).
        private static Lair.UI.BuildIconPoolingScrollView BuildBuildSection(Transform parent, string name, string label, bool top)
        {
            //# 섹션 RectTransform — 상하 50:50.
            //# top=true → anchorMin (0, 0.5), anchorMax (1, 1)  : 상단 절반
            //# top=false → anchorMin (0, 0), anchorMax (1, 0.5) : 하단 절반
            GameObject sectionGo = new GameObject(name, typeof(RectTransform));
            sectionGo.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)sectionGo.transform;
            rt.anchorMin = new Vector2(0f, top ? 0.5f : 0f);
            rt.anchorMax = new Vector2(1f, top ? 1f   : 0.5f);
            //# 섹션 사이 0.5px 여유 (분할선 1px 의 절반씩).
            rt.offsetMin = new Vector2(0f, top ? 0.5f : 0f);
            rt.offsetMax = new Vector2(0f, top ? 0f   : -0.5f);

            //# 섹션 라벨 — 상단 32px (§3.2 v0.5).
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(sectionGo.transform, false);
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot     = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(0f, 32f);
            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = label;
            labelTmp.font = TMP_Settings.defaultFontAsset;
            labelTmp.fontSize = 20f;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = Color.white;

            //# ScrollRect — 라벨 영역 32px 제외. CHPoolingScrollView 와 함께 부착 (§2.8.5).
            GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(sectionGo.transform, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 0f);
            scrollRt.offsetMax = new Vector2(0f, -32f);   //# 라벨 영역 제외.
            ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.1f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 1f;
            sr.horizontalScrollbar = null;
            sr.verticalScrollbar = null;

            //# Viewport — RectMask2D + Image (alpha 0.001, raycast target). drag receiver.
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0.5f, 0.5f);
            viewportRt.anchoredPosition = Vector2.zero;
            viewportRt.sizeDelta = Vector2.zero;
            Image viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.sprite = GetUISprite();
            viewportImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImg.raycastTarget = true;

            //# Content — RectTransform 만. CHPoolingScrollView 가 anchor/pivot/sizeDelta 자동 설정.
            //# VerticalLayoutGroup / ContentSizeFitter 부착 X (CHPoolingScrollView 의 InitItemTransform 과 충돌).
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            //# BuildIconCell prefab 인스턴스를 Content 첫 자식 → CHPoolingScrollView 의 _origin.
            //# 깜빡임 방지로 사전 비활성.
            GameObject cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BuildIconCell.prefab");
            GameObject originInst = null;
            if (cellPrefab != null)
            {
                originInst = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab, contentGo.transform);
                originInst.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[LairUIPrefabBuilder] BuildIconCell.prefab 미발견 — BuildBuildIconCell 선행 필요");
            }

            //# ScrollRect 참조 묶기.
            sr.viewport = viewportRt;
            sr.content = contentRt;

            //# CHPoolingScrollView 파생 컴포넌트 + 직렬화 wire-up.
            //# _columnCount = 1 (B1 단정), _itemSize wire-up 안 함 (P1 — prefab sizeDelta = 단일 진실).
            Lair.UI.BuildIconPoolingScrollView iconScrollView = scrollGo.AddComponent<Lair.UI.BuildIconPoolingScrollView>();
            SerializedObject ssvSo = new SerializedObject(iconScrollView);
            if (originInst != null) SetObjectField(ssvSo, "_origin", originInst);
            SetVector2Field(ssvSo, "_itemGap", new Vector2(0f, 6f));
            SetRectOffsetField(ssvSo, "_padding", 4, 4, 4, 4);
            SetEnumField(ssvSo, "_scrollDirection", (int)ChvjUnityInfra.PoolingScrollViewDirection.Vertical);
            SetEnumField(ssvSo, "_align", (int)ChvjUnityInfra.PoolingScrollViewAlign.LeftOrTop);
            SetIntField(ssvSo, "_rowCount", 0);
            SetIntField(ssvSo, "_columnCount", 1);   //# v0.9 B1 — 단일 컬럼 강제.
            SetIntField(ssvSo, "_poolItemCount", 0);
            ssvSo.ApplyModifiedPropertiesWithoutUndo();

            return iconScrollView;
        }

        //# ---------- BattleHud 프리팹 ----------
        private static void BuildBattleHud(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "BattleHud";

            //# HpBar.prefab 이 없으면 생성 (M4 단독 실행 시 M3 미선행 상황 대비).
            //# Rule 04 — HpBar.prefab 1개를 몬스터·HUD 가 공유.
            LairCharacterPrefabBuilder.EnsureHpBarPrefab();
            GameObject hpBarPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(LairCharacterPrefabBuilder.HpBarPrefabPath);

            //# 루트
            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);

            //# BattleHud 컴포넌트 — Lair.UI.BattleHud (Lair 어셈블리)
            Lair.UI.BattleHud hud = root.AddComponent<Lair.UI.BattleHud>();

            //# TimerText
            GameObject timerGo = new GameObject("TimerText", typeof(RectTransform));
            timerGo.transform.SetParent(root.transform, false);
            RectTransform timerRt = (RectTransform)timerGo.transform;
            timerRt.anchorMin = new Vector2(0.5f, 1f);
            timerRt.anchorMax = new Vector2(0.5f, 1f);
            timerRt.pivot     = new Vector2(0.5f, 1f);
            timerRt.anchoredPosition = new Vector2(0f, -40f);
            timerRt.sizeDelta = new Vector2(400f, 80f);
            //# Rule 11 — TMP_Text + CHText 래퍼 사용
            TextMeshProUGUI timerTmp = timerGo.AddComponent<TextMeshProUGUI>();
            timerTmp.text = "5:00";
            timerTmp.font = TMP_Settings.defaultFontAsset;
            timerTmp.fontSize = 48f;
            timerTmp.alignment = TextAlignmentOptions.Center;
            timerTmp.color = Color.white;
            CHText timerText = timerGo.AddComponent<CHText>();

            //# HeroHpBar — HpBar.prefab 인스턴스를 nest. 기존 HpBg 위치·크기 유지.
            //# Rule 04 — 몬스터 머리 위와 동일 프리팹 공유.
            Image hpFillImg = null;
            if (hpBarPrefab != null)
            {
                GameObject hpBarInst = (GameObject)PrefabUtility.InstantiatePrefab(hpBarPrefab, root.transform);
                hpBarInst.name = "HeroHpBar";
                RectTransform hpBarRt = (RectTransform)hpBarInst.transform;
                //# 기존 HpBg 와 동일한 위치·크기
                hpBarRt.anchorMin = new Vector2(0.5f, 1f);
                hpBarRt.anchorMax = new Vector2(0.5f, 1f);
                hpBarRt.pivot     = new Vector2(0.5f, 1f);
                hpBarRt.anchoredPosition = new Vector2(0f, -140f);
                hpBarRt.sizeDelta = new Vector2(400f, 24f);
                hpBarRt.localScale = Vector3.one;
                hpBarRt.localRotation = Quaternion.identity;

                //# Fill Image — 결정론적 경로 Background/Fill 로 탐색.
                Transform fillTf = hpBarInst.transform.Find("Background/Fill");
                hpFillImg = fillTf != null ? fillTf.GetComponent<Image>() : null;
                if (hpFillImg == null)
                    Debug.LogWarning("[LairUIPrefabBuilder] HpBar.prefab 내 Background/Fill Image 미발견");
            }
            else
            {
                Debug.LogWarning("[LairUIPrefabBuilder] HpBar.prefab 로드 실패 — 영웅 HP 바 생략");
            }

            //# ----- 빌드 패널 (화면 우측 1/3 폭 — v0.7 §2.8) -----
            //# Anchor (1,0)~(1,1), Pivot (1, 0.5), 폭 427 (= 1280/3).
            //# offsetMin = (-427, 0), offsetMax = (0, 0) — 우측 1/3 전체 세로 사용.
            //# v0.6 (240×568, offset -264/120/-24/-32) → v0.7 (427×720, offset -427/0/0/0) (§2.8.1).
            //# 자식 셀은 raycast 차단(BuildIconCell.Bind(null) 시 처리), 루트 CHButton 이 클릭 잡음.
            GameObject buildPanelGo = new GameObject("BuildPanel", typeof(RectTransform), typeof(Image), typeof(Button));
            buildPanelGo.transform.SetParent(root.transform, false);
            RectTransform bpRt = (RectTransform)buildPanelGo.transform;
            bpRt.anchorMin = new Vector2(1f, 0f);
            bpRt.anchorMax = new Vector2(1f, 1f);
            bpRt.pivot     = new Vector2(1f, 0.5f);
            bpRt.offsetMin = new Vector2(-427f, 0f);
            bpRt.offsetMax = new Vector2(0f, 0f);
            //# 패널 배경 — raycast 받는 거의 투명 Image.
            Image bpBg = buildPanelGo.GetComponent<Image>();
            bpBg.sprite = GetUISprite();
            bpBg.type = Image.Type.Sliced;
            bpBg.color = new Color(0f, 0f, 0f, 0.001f);
            Button bpButton = buildPanelGo.GetComponent<Button>();
            bpButton.targetGraphic = bpBg;
            CHButton bpChButton = buildPanelGo.AddComponent<CHButton>();
            Lair.UI.BuildPanel buildPanel = buildPanelGo.AddComponent<Lair.UI.BuildPanel>();

            //# 패시브/액티브 섹션 — 상하 50:50 (v0.4). 각 섹션은 CHPoolingScrollView 1세트 (v0.8 §2.8.5).
            //# BuildPanel.cs 의 _passiveScrollView/_activeScrollView 는 각 섹션의 BuildIconPoolingScrollView 를 가리킴.
            Lair.UI.BuildIconPoolingScrollView passiveScrollView = BuildBuildSection(buildPanelGo.transform, "PassiveSection", "패시브", top: true);
            Lair.UI.BuildIconPoolingScrollView activeScrollView  = BuildBuildSection(buildPanelGo.transform, "ActiveSection",  "액티브", top: false);

            //# BuildPanel 필드 주입 (v0.8 — _passiveContainer/_activeContainer 제거, _passiveScrollView/_activeScrollView 신규).
            //# _cellPrefab 직렬화는 제거됨 (CHPoolingScrollView 의 _origin 으로 이주).
            SerializedObject bpSo = new SerializedObject(buildPanel);
            SetObjectField(bpSo, "_passiveScrollView", passiveScrollView);
            SetObjectField(bpSo, "_activeScrollView",  activeScrollView);
            SetObjectField(bpSo, "_rootButton",        bpChButton);
            bpSo.ApplyModifiedPropertiesWithoutUndo();

            //# ----- 스포너 상태 패널 (화면 하단 6셀) -----
            //# 신규 빌더(Lair/Setup/Spawner Status UI) 가 SpawnerStatusPanel.prefab 을 생성한 다음 이 메뉴를 다시 돌리면
            //# 본 BattleHud 빌더가 prefab 을 nested instance 로 끼워준다 (Addressable 등록 없음).
            GameObject spawnerPanelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SpawnerStatusPanel.prefab");
            Lair.UI.SpawnerStatusPanel spawnerPanelInst = null;
            if (spawnerPanelPrefab != null)
            {
                GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(spawnerPanelPrefab, root.transform);
                spawnerPanelInst = inst.GetComponent<Lair.UI.SpawnerStatusPanel>();
            }
            else
            {
                Debug.LogWarning("[LairUIPrefabBuilder] SpawnerStatusPanel.prefab 미발견 — 'Lair/Setup/Spawner Status UI' 먼저 실행 권장");
            }

            //# SerializeField 주입 — _timerText (CHText), _heroHpFill, _buildPanel, _spawnerStatusPanel
            SerializedObject so = new SerializedObject(hud);
            SetObjectField(so, "_timerText",  timerText);    //# CHText 컴포넌트
            SetObjectField(so, "_heroHpFill", hpFillImg);
            SetObjectField(so, "_buildPanel", buildPanel);
            if (spawnerPanelInst != null)
                SetObjectField(so, "_spawnerStatusPanel", spawnerPanelInst);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ---------- ResultPopup 프리팹 ----------
        private static void BuildResultPopup(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "ResultPopup";

            //# 루트
            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);
            Lair.UI.ResultPopup popup = root.AddComponent<Lair.UI.ResultPopup>();

            //# Dim (full-stretch 검정 50%)
            GameObject dimGo = new GameObject("Dim", typeof(RectTransform));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            Image dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = GetUISprite();
            dimImg.type = Image.Type.Sliced;
            dimImg.color = new Color(0f, 0f, 0f, 0.5f);

            //# ResultText (center)
            GameObject resultGo = new GameObject("ResultText", typeof(RectTransform));
            resultGo.transform.SetParent(root.transform, false);
            RectTransform resultRt = (RectTransform)resultGo.transform;
            resultRt.anchorMin = new Vector2(0.5f, 0.5f);
            resultRt.anchorMax = new Vector2(0.5f, 0.5f);
            resultRt.pivot     = new Vector2(0.5f, 0.5f);
            resultRt.anchoredPosition = new Vector2(0f, 50f);
            resultRt.sizeDelta = new Vector2(600f, 200f);
            //# Rule 11 — TMP_Text + CHText 래퍼 사용
            TextMeshProUGUI resultTmp = resultGo.AddComponent<TextMeshProUGUI>();
            resultTmp.text = "결과";
            resultTmp.font = TMP_Settings.defaultFontAsset;
            resultTmp.fontSize = 96f;
            resultTmp.alignment = TextAlignmentOptions.Center;
            resultTmp.color = Color.white;
            CHText resultText = resultGo.AddComponent<CHText>();

            //# RestartButton (center, below ResultText)
            GameObject btnGo = new GameObject("RestartButton", typeof(RectTransform));
            btnGo.transform.SetParent(root.transform, false);
            RectTransform btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.5f, 0.5f);
            btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot     = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = new Vector2(0f, -120f);
            btnRt.sizeDelta = new Vector2(240f, 80f);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = GetUISprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = Color.white;
            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            //# Rule 11 — CHButton 래퍼 추가 (사운드 hook + closeDisposable 친화 OnClick)
            CHButton chBtn = btnGo.AddComponent<CHButton>();

            //# Button label — TMP_Text (CHButton.SetText 가 자식 TMP 자동 인식)
            GameObject btnTextGo = new GameObject("ButtonText", typeof(RectTransform));
            btnTextGo.transform.SetParent(btnGo.transform, false);
            SetFullStretch((RectTransform)btnTextGo.transform);
            TextMeshProUGUI btnTmp = btnTextGo.AddComponent<TextMeshProUGUI>();
            btnTmp.text = "다시 시작";
            btnTmp.font = TMP_Settings.defaultFontAsset;
            btnTmp.fontSize = 36f;
            btnTmp.alignment = TextAlignmentOptions.Center;
            btnTmp.color = Color.black;

            //# SerializeField 주입 — _resultText (CHText), _restartButton (CHButton)
            SerializedObject so = new SerializedObject(popup);
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
            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            SetFullStretch((RectTransform)root.transform);
            Lair.UI.CardSelectionPopup popup = root.AddComponent<Lair.UI.CardSelectionPopup>();

            //# Dim
            GameObject dimGo = new GameObject("Dim", typeof(RectTransform));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            Image dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = GetUISprite();
            dimImg.type = Image.Type.Sliced;
            dimImg.color = new Color(0f, 0f, 0f, 0.65f);

            //# Title
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(root.transform, false);
            RectTransform titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot     = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -60f);
            titleRt.sizeDelta = new Vector2(600f, 80f);
            TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "카드 선택";
            titleTmp.font = TMP_Settings.defaultFontAsset;
            titleTmp.fontSize = 48f;
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleTmp.color = Color.white;
            titleGo.AddComponent<CHText>();

            //# CardsLayout — 3 slot 가로 배치
            GameObject layoutGo = new GameObject("CardsLayout", typeof(RectTransform));
            layoutGo.transform.SetParent(root.transform, false);
            RectTransform layoutRt = (RectTransform)layoutGo.transform;
            layoutRt.anchorMin = new Vector2(0.5f, 0.5f);
            layoutRt.anchorMax = new Vector2(0.5f, 0.5f);
            layoutRt.pivot     = new Vector2(0.5f, 0.5f);
            layoutRt.anchoredPosition = Vector2.zero;
            layoutRt.sizeDelta = new Vector2(1080f, 460f);
            HorizontalLayoutGroup hlg = layoutGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 40;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            //# 3개 CardView slot
            Lair.UI.CardView[] slots = new Lair.UI.CardView[3];
            for (int i = 0; i < 3; ++i)
            {
                slots[i] = BuildCardViewSlot(layoutGo.transform, i);
            }

            //# SerializedObject 주입 — _slots 배열
            SerializedObject so = new SerializedObject(popup);
            SerializedProperty slotsProp = so.FindProperty("_slots");
            slotsProp.arraySize = 3;
            for (int i = 0; i < 3; ++i)
                slotsProp.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        private static Lair.UI.CardView BuildCardViewSlot(Transform parent, int index)
        {
            GameObject slot = new GameObject($"CardView_{index}", typeof(RectTransform));
            slot.transform.SetParent(parent, false);
            RectTransform slotRt = (RectTransform)slot.transform;
            slotRt.sizeDelta = new Vector2(320f, 420f);

            //# Border (full stretch, 카테고리 색 — 런타임에 CardView 가 변경)
            GameObject borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(slot.transform, false);
            SetFullStretch((RectTransform)borderGo.transform);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = GetUISprite();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = Color.gray;

            //# 카드 내부 흰 배경 (border 안쪽 약간 작게)
            GameObject bgGo = new GameObject("Bg", typeof(RectTransform));
            bgGo.transform.SetParent(slot.transform, false);
            RectTransform bgRt = (RectTransform)bgGo.transform;
            bgRt.anchorMin = Vector2.zero;
            bgRt.anchorMax = Vector2.one;
            bgRt.offsetMin = new Vector2(8f, 8f);
            bgRt.offsetMax = new Vector2(-8f, -8f);
            Image bgImg = bgGo.AddComponent<Image>();
            bgImg.sprite = GetUISprite();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.white;

            //# NameText
            GameObject nameGo = new GameObject("NameText", typeof(RectTransform));
            nameGo.transform.SetParent(slot.transform, false);
            RectTransform nameRt = (RectTransform)nameGo.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot     = new Vector2(0.5f, 1f);
            nameRt.anchoredPosition = new Vector2(0f, -30f);
            nameRt.sizeDelta = new Vector2(0f, 60f);
            TextMeshProUGUI nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Name";
            nameTmp.font = TMP_Settings.defaultFontAsset;
            nameTmp.fontSize = 32f;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.color = Color.black;
            CHText nameText = nameGo.AddComponent<CHText>();

            //# DescText
            GameObject descGo = new GameObject("DescText", typeof(RectTransform));
            descGo.transform.SetParent(slot.transform, false);
            RectTransform descRt = (RectTransform)descGo.transform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0.7f);
            descRt.offsetMin = new Vector2(20f, 20f);
            descRt.offsetMax = new Vector2(-20f, -20f);
            TextMeshProUGUI descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Description";
            descTmp.font = TMP_Settings.defaultFontAsset;
            descTmp.fontSize = 22f;
            descTmp.alignment = TextAlignmentOptions.TopLeft;
            descTmp.color = Color.black;
            CHText descText = descGo.AddComponent<CHText>();

            //# PickButton (full stretch over the whole card)
            GameObject btnGo = new GameObject("PickButton", typeof(RectTransform));
            btnGo.transform.SetParent(slot.transform, false);
            SetFullStretch((RectTransform)btnGo.transform);
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.sprite = GetUISprite();
            btnImg.type = Image.Type.Sliced;
            btnImg.color = new Color(1, 1, 1, 0.001f);   //# 거의 투명, raycast 만 받음
            Button btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            CHButton chBtn = btnGo.AddComponent<CHButton>();

            //# CardView 컴포넌트
            Lair.UI.CardView cv = slot.AddComponent<Lair.UI.CardView>();
            SerializedObject cvSo = new SerializedObject(cv);
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
            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            //# Addressables 등록 — address = 파일명 (Rule 08)
            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
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
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        //# CHPoolingScrollView 직렬화 필드 wire-up용 헬퍼 (v0.8).
        private static void SetVector2Field(SerializedObject so, string fieldName, Vector2 value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.vector2Value = value;
        }

        private static void SetIntField(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.intValue = value;
        }

        private static void SetEnumField(SerializedObject so, string fieldName, int enumIndex)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.enumValueIndex = enumIndex;
        }

        private static void SetRectOffsetField(SerializedObject so, string fieldName, int left, int right, int top, int bottom)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairUIPrefabBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            SerializedProperty leftProp   = prop.FindPropertyRelative("m_Left");
            SerializedProperty rightProp  = prop.FindPropertyRelative("m_Right");
            SerializedProperty topProp    = prop.FindPropertyRelative("m_Top");
            SerializedProperty bottomProp = prop.FindPropertyRelative("m_Bottom");
            if (leftProp   != null) leftProp.intValue   = left;
            if (rightProp  != null) rightProp.intValue  = right;
            if (topProp    != null) topProp.intValue    = top;
            if (bottomProp != null) bottomProp.intValue = bottom;
        }

        private static Color ParseColor(string hex)
        {
            return ColorUtility.TryParseHtmlString(hex, out Color c) ? c : Color.magenta;
        }

        private static Font GetDefaultFont()
        {
            //# Unity 2022.3 빌트인 — LegacyRuntime.ttf 우선, fallback Arial.ttf
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font == null)
                font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (font == null)
                font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            return font;
        }

        private static void EnsureDir(string path)
        {
            if (Directory.Exists(path) == false)
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }

        private static void EnsureTagExists(string tag)
        {
            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[LairUIPrefabBuilder] TagManager.asset 로드 실패");
                return;
            }
            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
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
