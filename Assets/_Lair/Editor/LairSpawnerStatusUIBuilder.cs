using System.IO;
using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# 스포너 상태 UI 신규 프리팹 4종 빌드 (기획서 §4.11).
    //# - SpawnerStatusPanel.prefab  (Addressable X — BattleHud nested)
    //# - SpawnerStatusCell.prefab   (Addressable X — CHMPool 풀링, panel 직참)
    //# - SpawnerStatusTooltip.prefab(Addressable O — CHMUI.ShowUI 키)
    //# - BuildModalPopup.prefab     (Addressable O — CHMUI.ShowUI 키)
    //# 파일명 = Enum 값명 (Rule 08). Art/UI 폴더 (Rule 14).
    public static class LairSpawnerStatusUIBuilder
    {
        public const string PrefabDir = "Assets/_Lair/Art/UI";
        public const string ResourceGroup = "Resource";
        public const string ResourceLabel = "Resource";

        //# 셀 치수 (기획서 §3.2 v0.7 — 134×168 2.094× 확대).
        private const float CellWidth  = 134f;
        private const float CellHeight = 168f;
        private const float CellSpacing = 6f;
        private const float PanelHorizontalPadding = 8f;

        //# 활성 테두리 색 (#FBBF24).
        private static readonly Color YellowAccent = new Color(0.984f, 0.749f, 0.141f, 1f);
        //# 셀 배경 (#1F2937 α 0.85).
        private static readonly Color CellBackground = new Color(0.122f, 0.161f, 0.216f, 0.85f);
        //# 모달 배경 dim (#000 α 0.6).
        private static readonly Color ModalDim = new Color(0f, 0f, 0f, 0.6f);
        //# 모달 본체 (#1F2937 α 0.98).
        private static readonly Color ModalBg = new Color(0.122f, 0.161f, 0.216f, 0.98f);
        //# 섹션 구분선 (#374151).
        private static readonly Color DividerColor = new Color(0.216f, 0.255f, 0.318f, 1f);
        //# 진행 바 배경 (#374151).
        private static readonly Color BarBackground = new Color(0.216f, 0.255f, 0.318f, 1f);
        //# 툴팁 배경 (#1F2937 α 0.95).
        private static readonly Color TooltipBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        //# 빈 상태 회색 (#9CA3AF).
        private static readonly Color GrayLabel = new Color(0.612f, 0.639f, 0.686f, 1f);

        [MenuItem("Lair/Setup/Spawner Status UI")]
        public static void BuildAll()
        {
            EnsureDir(PrefabDir);

            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairSpawnerStatusUIBuilder] Addressables 미설정");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                Debug.LogError($"[LairSpawnerStatusUIBuilder] Addressables '{ResourceGroup}' 그룹 미발견");
                return;
            }

            //# 1) Cell 부터 — Panel 이 _cellPrefab 으로 참조.
            BuildCellPrefab();
            //# 2) Panel — Cell 프리팹 + Spawner 직참은 BattleHud 빌드 시 nesting 이후 인스펙터 또는 빌드 후처리로.
            BuildPanelPrefab();
            //# 3) Tooltip — Addressable 등록.
            BuildTooltipPrefab(settings, group);
            //# 4) Modal — BuildModalCardCell 셀 먼저, 그 다음 모달 본체.
            BuildModalCardCellPrefab();
            BuildModalPrefab(settings, group);

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairSpawnerStatusUIBuilder] 스포너 상태 UI 4종 + 모달 셀 + BuffLine 빌드 완료");
        }

        //# ===== SpawnerStatusCell =====
        private static void BuildCellPrefab()
        {
            const string PrefabName = "SpawnerStatusCell";

            GameObject root = new GameObject(PrefabName, typeof(RectTransform), typeof(Image), typeof(Button));
            RectTransform rootRt = (RectTransform)root.transform;
            //# 셀 pivot (0, 0) 좌측 하단 — 본체/아이콘/진행 바 row 의 anchoredPosition 단일 진실 기준 (v0.8 §2.2.1).
            rootRt.pivot = new Vector2(0f, 0f);
            rootRt.sizeDelta = new Vector2(CellWidth, CellHeight);

            Image bg = root.GetComponent<Image>();
            bg.sprite = LairUIPrefabBuilder.GetUISprite();
            bg.type = Image.Type.Sliced;
            bg.color = CellBackground;
            Button btn = root.GetComponent<Button>();
            btn.targetGraphic = bg;
            CHButton chBtn = root.AddComponent<CHButton>();

            //# Border — 활성 시 노란 테두리 (#FBBF24). 비활성 시 alpha 0 으로 숨김.
            GameObject borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)borderGo.transform);
            Image borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = LairUIPrefabBuilder.GetUISprite();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(0f, 0f, 0f, 0f);
            borderImg.raycastTarget = false;

            //# IconRow — 셀 상단 42px (v0.7). 셀 pivot (0,0) — anchoredPosition.y = 118 (§2.2.1).
            GameObject iconRowGo = new GameObject("IconRow", typeof(RectTransform));
            iconRowGo.transform.SetParent(root.transform, false);
            RectTransform iconRowRt = (RectTransform)iconRowGo.transform;
            iconRowRt.anchorMin = new Vector2(0f, 0f);
            iconRowRt.anchorMax = new Vector2(1f, 0f);
            iconRowRt.pivot     = new Vector2(0f, 0f);
            iconRowRt.anchoredPosition = new Vector2(0f, 118f);
            iconRowRt.sizeDelta = new Vector2(0f, 42f);

            //# v1.0 — IconRow 는 2 슬롯 (좌 Enhance x=12 / 우 Spawn x=68). 슬롯 간 spacing 4 (§2.3.1 v1.0 검산).
            //# 슬롯 1 — Enhance: anchoredPosition.x = 12.
            (Image circle, CHText letter, CHText badge) enhSlot = BuildIconSlot(iconRowGo.transform, "IconCircleEnhance", "IconLetterEnhance", "IconBadgeEnhance",
                xPos: 12f, defaultLetter: "H");
            Image circleImgEnhance = enhSlot.circle;
            CHText letterTextEnhance = enhSlot.letter;
            CHText badgeTextEnhance = enhSlot.badge;

            //# 슬롯 2 — Spawn: anchoredPosition.x = 68 (= 슬롯 1 배지 우측 끝 64 + spacing 4).
            (Image circle, CHText letter, CHText badge) spnSlot = BuildIconSlot(iconRowGo.transform, "IconCircleSpawn", "IconLetterSpawn", "IconBadgeSpawn",
                xPos: 68f, defaultLetter: "+");
            Image circleImgSpawn = spnSlot.circle;
            CHText letterTextSpawn = spnSlot.letter;
            CHText badgeTextSpawn = spnSlot.badge;

            //# 본체 row — 색칩 + 종명 + ×N. 높이 58 (v0.7).
            //# 셀 pivot (0,0) — anchoredPosition.y = 52 (§2.2.1).
            GameObject bodyRowGo = new GameObject("BodyRow", typeof(RectTransform));
            bodyRowGo.transform.SetParent(root.transform, false);
            RectTransform bodyRowRt = (RectTransform)bodyRowGo.transform;
            bodyRowRt.anchorMin = new Vector2(0f, 0f);
            bodyRowRt.anchorMax = new Vector2(1f, 0f);
            bodyRowRt.pivot     = new Vector2(0f, 0f);
            bodyRowRt.anchoredPosition = new Vector2(0f, 52f);
            bodyRowRt.sizeDelta = new Vector2(0f, 58f);

            //# 색칩 30×30 (v0.7). 좌측 padding 12.
            GameObject chipGo = new GameObject("ColorChip", typeof(RectTransform), typeof(Image));
            chipGo.transform.SetParent(bodyRowGo.transform, false);
            RectTransform chipRt = (RectTransform)chipGo.transform;
            chipRt.anchorMin = new Vector2(0f, 0.5f);
            chipRt.anchorMax = new Vector2(0f, 0.5f);
            chipRt.pivot     = new Vector2(0f, 0.5f);
            chipRt.anchoredPosition = new Vector2(12f, 0f);
            chipRt.sizeDelta = new Vector2(30f, 30f);
            Image chipImg = chipGo.GetComponent<Image>();
            chipImg.sprite = LairUIPrefabBuilder.GetUISprite();
            chipImg.color = Color.green;
            chipImg.raycastTarget = false;

            //# 종명 (영문). 22pt (v0.7 cap 적용). 색칩 우측 gap 14 → offsetMin.x = 12 + 30 + 14 = 56.
            GameObject speciesGo = new GameObject("SpeciesText", typeof(RectTransform));
            speciesGo.transform.SetParent(bodyRowGo.transform, false);
            RectTransform speciesRt = (RectTransform)speciesGo.transform;
            speciesRt.anchorMin = new Vector2(0f, 0f);
            speciesRt.anchorMax = new Vector2(1f, 1f);
            speciesRt.offsetMin = new Vector2(56f, 0f);
            speciesRt.offsetMax = new Vector2(-12f, 0f);
            TextMeshProUGUI speciesTmp = speciesGo.AddComponent<TextMeshProUGUI>();
            speciesTmp.text = "Wisp";
            speciesTmp.font = TMP_Settings.defaultFontAsset;
            speciesTmp.fontSize = 22f;
            speciesTmp.alignment = TextAlignmentOptions.Left;
            speciesTmp.color = Color.white;
            speciesTmp.overflowMode = TextOverflowModes.Truncate;
            speciesTmp.raycastTarget = false;
            CHText speciesText = speciesGo.AddComponent<CHText>();

            //# ×N — 본체 row 우측 정렬. 20pt (v0.7 cap 적용).
            GameObject countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(bodyRowGo.transform, false);
            RectTransform countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot     = new Vector2(1f, 0.5f);
            countRt.anchoredPosition = new Vector2(-8f, 0f);
            countRt.sizeDelta = new Vector2(36f, 0f);
            TextMeshProUGUI countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 20f;
            countTmp.alignment = TextAlignmentOptions.Right;
            countTmp.color = YellowAccent;
            countTmp.raycastTarget = false;
            CHText countText = countGo.AddComponent<CHText>();
            countGo.SetActive(false);

            //# 진행 바 — Background + Fill. 17px 높이 (v0.7). 좌우 padding 12 → sizeDelta x = -24, 결과 폭 110.
            //# 진행 바 anchoredPosition.y = 27 (§2.2.1 단일 진실, v0.8 BLOCKER B1).
            //# pivot.y=0 으로 셀 하단 기준. anchor X 는 (0,1) stretch + pivot.x=0.5 → 가로 가운데, sizeDelta.x=-24 가 좌우 12px 여백.
            GameObject progressBgGo = new GameObject("ProgressBackground", typeof(RectTransform), typeof(Image));
            progressBgGo.transform.SetParent(root.transform, false);
            RectTransform progressBgRt = (RectTransform)progressBgGo.transform;
            progressBgRt.anchorMin = new Vector2(0f, 0f);
            progressBgRt.anchorMax = new Vector2(1f, 0f);
            progressBgRt.pivot     = new Vector2(0.5f, 0f);
            progressBgRt.anchoredPosition = new Vector2(0f, 27f);
            progressBgRt.sizeDelta = new Vector2(-24f, 17f);
            Image progressBgImg = progressBgGo.GetComponent<Image>();
            progressBgImg.sprite = LairUIPrefabBuilder.GetUISprite();
            progressBgImg.color = BarBackground;
            progressBgImg.raycastTarget = false;

            GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(progressBgGo.transform, false);
            SetFullStretch((RectTransform)fillGo.transform);
            Image fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = LairUIPrefabBuilder.GetUISprite();
            fillImg.color = SpawnerStatusCell.CoolColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            //# SpawnerStatusCell 컴포넌트 부착 + 필드 주입.
            //# v1.0 — 단일 아이콘 셋(_iconCircle/_iconLetter/_iconBadge) → 2 슬롯 (Enhance + Spawn) 6 필드로 분리.
            SpawnerStatusCell cell = root.AddComponent<SpawnerStatusCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObjectField(so, "_border",              borderImg);
            SetObjectField(so, "_colorChip",           chipImg);
            SetObjectField(so, "_speciesText",         speciesText);
            SetObjectField(so, "_countText",           countText);
            SetObjectField(so, "_progressFill",        fillImg);
            SetObjectField(so, "_button",              chBtn);
            SetObjectField(so, "_iconRow",             iconRowRt);
            SetObjectField(so, "_iconCircleEnhance",   circleImgEnhance);
            SetObjectField(so, "_iconLetterEnhance",   letterTextEnhance);
            SetObjectField(so, "_iconBadgeEnhance",    badgeTextEnhance);
            SetObjectField(so, "_iconCircleSpawn",     circleImgSpawn);
            SetObjectField(so, "_iconLetterSpawn",     letterTextSpawn);
            SetObjectField(so, "_iconBadgeSpawn",      badgeTextSpawn);
            so.ApplyModifiedPropertiesWithoutUndo();

            //# IconRow 는 기본 숨김 (강화 없음 시).
            iconRowGo.SetActive(false);

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== SpawnerStatusPanel =====
        private static void BuildPanelPrefab()
        {
            const string PrefabName = "SpawnerStatusPanel";

            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            //# 화면 좌측 하단 정렬 (v0.7 §2.1). Pivot (0, 0), anchoredPosition (0, 0).
            rootRt.anchorMin = new Vector2(0f, 0f);
            rootRt.anchorMax = new Vector2(0f, 0f);
            rootRt.pivot     = new Vector2(0f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 0f);
            //# 850 = 6×134 + 5×6 + 2×8 (v0.7 기획서 §2.1).
            rootRt.sizeDelta = new Vector2(850f, CellHeight);

            //# Container — HorizontalLayoutGroup.
            GameObject containerGo = new GameObject("Container", typeof(RectTransform));
            containerGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)containerGo.transform);
            HorizontalLayoutGroup hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CellSpacing;
            hlg.padding = new RectOffset((int)PanelHorizontalPadding, (int)PanelHorizontalPadding, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            //# SpawnerStatusPanel 컴포넌트 부착 + _container/_cellPrefab 주입.
            SpawnerStatusPanel panel = root.AddComponent<SpawnerStatusPanel>();
            GameObject cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SpawnerStatusCell.prefab");
            SerializedObject so = new SerializedObject(panel);
            SetObjectField(so, "_container",  containerGo.transform);
            SetObjectField(so, "_cellPrefab", cellPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== SpawnerStatusTooltip =====
        //# v0.8 — Body 201×252 (셀 1개 134×168 의 1.5배), CHPoolingScrollView<BuffLine, AppliedBuff>.
        //# v0.9 — _itemSize 는 dead serialization, BuffLine prefab sizeDelta = 단일 진실 (§4.11 P1).
        private static void BuildTooltipPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "SpawnerStatusTooltip";

            //# 사전 — BuffLine.prefab 빌드 (CHPoolingScrollView 의 _origin).
            BuildBuffLinePrefab();

            //# 루트 — UIBase 의 SetActive(true) 기반, full-stretch overlay 가능.
            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);
            SpawnerStatusTooltip tooltip = root.AddComponent<SpawnerStatusTooltip>();

            //# Body — 201×252 (v0.7). 위치는 런타임 PositionAboveAnchor 가 결정.
            //# pivot/anchor 는 SpawnerStatusTooltip.PositionAboveAnchor 가 런타임에 갱신 (anchor 0.5,0.5 / pivot 0.5,0).
            GameObject bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            RectTransform bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot     = new Vector2(0.5f, 0f);
            bodyRt.anchoredPosition = new Vector2(0f, 0f);
            bodyRt.sizeDelta = new Vector2(201f, 252f);
            Image bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = TooltipBg;

            //# 노란 테두리 (#FBBF24 1px) — Outline 은 Image 와 같은 GameObject 에 부착.
            Outline bodyOutline = bodyGo.AddComponent<Outline>();
            bodyOutline.effectColor = YellowAccent;
            bodyOutline.effectDistance = new Vector2(1f, -1f);

            //# Header — CHPoolingScrollView 외부 고정 (위쪽 32px 영역, padding 8 안쪽).
            GameObject headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(bodyGo.transform, false);
            RectTransform headerRt = (RectTransform)headerGo.transform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot     = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -8f);
            headerRt.sizeDelta = new Vector2(-16f, 24f);
            TextMeshProUGUI headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "Spawner #0";
            headerTmp.font = TMP_Settings.defaultFontAsset;
            headerTmp.fontSize = 11f;
            headerTmp.alignment = TextAlignmentOptions.Left;
            headerTmp.color = Color.white;
            CHText headerText = headerGo.AddComponent<CHText>();

            //# Empty state — CHPoolingScrollView 외부에서 buffs.Count==0 일 때만 SetActive(true).
            GameObject emptyGo = new GameObject("EmptyText", typeof(RectTransform));
            emptyGo.transform.SetParent(bodyGo.transform, false);
            RectTransform emptyRt = (RectTransform)emptyGo.transform;
            emptyRt.anchorMin = new Vector2(0f, 0f);
            emptyRt.anchorMax = new Vector2(1f, 1f);
            emptyRt.offsetMin = new Vector2(8f, 8f);
            emptyRt.offsetMax = new Vector2(-8f, -40f);
            TextMeshProUGUI emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyTmp.text = "적용된 강화 없음";
            emptyTmp.font = TMP_Settings.defaultFontAsset;
            emptyTmp.fontSize = 10f;
            emptyTmp.alignment = TextAlignmentOptions.TopLeft;
            emptyTmp.color = GrayLabel;
            emptyTmp.raycastTarget = false;
            CHText emptyText = emptyGo.AddComponent<CHText>();
            emptyGo.SetActive(false);

            //# BuffScrollView — CHPoolingScrollView<BuffLine, AppliedBuff> 구조 (§2.5.5 v0.8).
            //# RectTransform : anchor (0,0)~(1,1), offsetMin (8,8) / offsetMax (-8,-40) → 폭 185 / 세로 204.
            GameObject scrollGo = new GameObject("BuffScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(bodyGo.transform, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(8f, 8f);
            scrollRt.offsetMax = new Vector2(-8f, -40f);
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

            //# Viewport — RectMask2D + Image (alpha 0.001, raycast target). drag input receiver.
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0.5f, 0.5f);
            viewportRt.anchoredPosition = Vector2.zero;
            viewportRt.sizeDelta = Vector2.zero;
            Image viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.sprite = LairUIPrefabBuilder.GetUISprite();
            viewportImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImg.raycastTarget = true;

            //# Content — RectTransform 만. CHPoolingScrollView 가 anchor/pivot/sizeDelta 자동 설정.
            //# VerticalLayoutGroup / ContentSizeFitter 부착 안 함 (CHPoolingScrollView 의 InitItemTransform 과 충돌).
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            //# BuffLine prefab 인스턴스를 Content 첫 자식으로 nest → CHPoolingScrollView 의 _origin.
            //# Start 에서 CHPoolingScrollView 가 SetActive(false) 자동 처리하지만, 깜빡임 방지로 사전 비활성.
            GameObject buffLinePrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BuffLine.prefab");
            GameObject originInst = null;
            if (buffLinePrefab != null)
            {
                originInst = (GameObject)PrefabUtility.InstantiatePrefab(buffLinePrefab, contentGo.transform);
                originInst.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[LairSpawnerStatusUIBuilder] BuffLine.prefab 미발견 — BuildBuffLinePrefab 선행 필요");
            }

            //# ScrollRect viewport/content 참조 묶기.
            sr.viewport = viewportRt;
            sr.content = contentRt;

            //# CHPoolingScrollView 파생 컴포넌트 부착 + 직렬화 필드 wire-up.
            //# _itemSize 는 wire-up 안 함 (dead serialization, v0.9 P1) — BuffLine prefab sizeDelta = 단일 진실.
            BuffPoolingScrollView buffScrollView = scrollGo.AddComponent<BuffPoolingScrollView>();
            SerializedObject bsvSo = new SerializedObject(buffScrollView);
            if (originInst != null) SetObjectField(bsvSo, "_origin", originInst);
            SetVector2Field(bsvSo, "_itemGap", new Vector2(0f, 4f));
            SetRectOffsetField(bsvSo, "_padding", 2, 2, 2, 2);
            SetEnumField(bsvSo, "_scrollDirection", (int)PoolingScrollViewDirection.Vertical);
            SetEnumField(bsvSo, "_align", (int)PoolingScrollViewAlign.LeftOrTop);
            SetIntField(bsvSo, "_rowCount", 0);
            SetIntField(bsvSo, "_columnCount", 0);
            SetIntField(bsvSo, "_poolItemCount", 0);
            bsvSo.ApplyModifiedPropertiesWithoutUndo();

            //# SpawnerStatusTooltip 직렬화 필드 주입 — _buffText 제거됨, _buffScrollView / _emptyText 신규.
            SerializedObject so = new SerializedObject(tooltip);
            SetObjectField(so, "_root",           bodyRt);
            SetObjectField(so, "_headerText",     headerText);
            SetObjectField(so, "_buffScrollView", buffScrollView);
            SetObjectField(so, "_emptyText",      emptyText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ===== BuffLine prefab =====
        //# 툴팁 본문 강화 줄 1개 — sizeDelta (185, 24).
        //# 자식: IconCircle(16×16 원) + IconLetter(12pt) + Badge(10pt ×N) + Body(10pt 본문).
        //# CHPoolingScrollView<BuffLine, AppliedBuff> 의 _origin 으로 nesting (§4.11 v0.8).
        private static void BuildBuffLinePrefab()
        {
            const string PrefabName = "BuffLine";

            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(185f, 24f);
            //# v0.9 P1 — prefab sizeDelta 가 BuffPoolingScrollView 의 itemSize 단일 진실.

            //# IconCircle — 16×16 원형 (sprite = UISprite, color = 종 6색 매핑은 Bind 시 결정).
            GameObject circleGo = new GameObject("IconCircle", typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(root.transform, false);
            RectTransform circleRt = (RectTransform)circleGo.transform;
            circleRt.anchorMin = new Vector2(0f, 0.5f);
            circleRt.anchorMax = new Vector2(0f, 0.5f);
            circleRt.pivot     = new Vector2(0f, 0.5f);
            circleRt.anchoredPosition = new Vector2(2f, 0f);
            circleRt.sizeDelta = new Vector2(16f, 16f);
            Image circleImg = circleGo.GetComponent<Image>();
            circleImg.sprite = LairUIPrefabBuilder.GetUISprite();
            circleImg.color = Color.gray;
            circleImg.raycastTarget = false;

            //# IconLetter — 12pt (v0.8 cap 적용, 셀의 16pt 와 별도 정책).
            GameObject letterGo = new GameObject("IconLetter", typeof(RectTransform));
            letterGo.transform.SetParent(circleGo.transform, false);
            SetFullStretch((RectTransform)letterGo.transform);
            TextMeshProUGUI letterTmp = letterGo.AddComponent<TextMeshProUGUI>();
            letterTmp.text = "H";
            letterTmp.font = TMP_Settings.defaultFontAsset;
            letterTmp.fontSize = 12f;
            letterTmp.alignment = TextAlignmentOptions.Center;
            letterTmp.color = Color.black;
            letterTmp.raycastTarget = false;
            CHText letterText = letterGo.AddComponent<CHText>();

            //# Badge — ×N 10pt 노랑 (v0.8 cap), IconCircle 우측.
            GameObject badgeGo = new GameObject("Badge", typeof(RectTransform));
            badgeGo.transform.SetParent(root.transform, false);
            RectTransform badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(0f, 0.5f);
            badgeRt.anchorMax = new Vector2(0f, 0.5f);
            badgeRt.pivot     = new Vector2(0f, 0.5f);
            badgeRt.anchoredPosition = new Vector2(20f, 0f);
            badgeRt.sizeDelta = new Vector2(24f, 12f);
            TextMeshProUGUI badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "×2";
            badgeTmp.font = TMP_Settings.defaultFontAsset;
            badgeTmp.fontSize = 10f;
            badgeTmp.alignment = TextAlignmentOptions.Left;
            badgeTmp.color = YellowAccent;
            badgeTmp.outlineColor = Color.black;
            badgeTmp.outlineWidth = 0.2f;
            badgeTmp.raycastTarget = false;
            CHText badgeText = badgeGo.AddComponent<CHText>();
            badgeGo.SetActive(false);

            //# BodyText — 본문 10pt 흰색. IconCircle + Badge 영역 44px 제외 후 stretch.
            GameObject bodyGo = new GameObject("BodyText", typeof(RectTransform));
            bodyGo.transform.SetParent(root.transform, false);
            RectTransform bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0f, 0f);
            bodyRt.anchorMax = new Vector2(1f, 1f);
            bodyRt.offsetMin = new Vector2(44f, 0f);
            bodyRt.offsetMax = new Vector2(-2f, 0f);
            TextMeshProUGUI bodyTmp = bodyGo.AddComponent<TextMeshProUGUI>();
            bodyTmp.text = "체력 ×1.5 (200 → 300)";
            bodyTmp.font = TMP_Settings.defaultFontAsset;
            bodyTmp.fontSize = 10f;
            bodyTmp.alignment = TextAlignmentOptions.Left;
            bodyTmp.color = Color.white;
            bodyTmp.overflowMode = TextOverflowModes.Truncate;
            bodyTmp.raycastTarget = false;
            CHText bodyText = bodyGo.AddComponent<CHText>();

            //# BuffLine 컴포넌트 부착 + 자식 4개 wire-up.
            BuffLine line = root.AddComponent<BuffLine>();
            SerializedObject so = new SerializedObject(line);
            SetObjectField(so, "_iconCircle", circleImg);
            SetObjectField(so, "_iconLetter", letterText);
            SetObjectField(so, "_badge",      badgeText);
            SetObjectField(so, "_bodyText",   bodyText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== BuildModalCardCell =====
        //# v0.9 P1 — sizeDelta (303, 56) (기존 280×56 갱신). prefab sizeDelta 가 CHPoolingScrollView 의
        //# itemSize 단일 진실이므로 빌더가 박는다 (모달 가용 폭 절반 303).
        private static void BuildModalCardCellPrefab()
        {
            const string PrefabName = "BuildModalCardCell";

            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(303f, 56f);

            //# 프레임 — 좌측 세로 막대.
            GameObject frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(root.transform, false);
            RectTransform frameRt = (RectTransform)frameGo.transform;
            frameRt.anchorMin = new Vector2(0f, 0.5f);
            frameRt.anchorMax = new Vector2(0f, 0.5f);
            frameRt.pivot     = new Vector2(0f, 0.5f);
            frameRt.anchoredPosition = new Vector2(4f, 0f);
            frameRt.sizeDelta = new Vector2(8f, 40f);
            Image frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = LairUIPrefabBuilder.GetUISprite();
            frameImg.color = Color.gray;
            frameImg.raycastTarget = false;

            //# 카드 이름.
            GameObject nameGo = new GameObject("NameText", typeof(RectTransform));
            nameGo.transform.SetParent(root.transform, false);
            RectTransform nameRt = (RectTransform)nameGo.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot     = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(20f, -4f);
            nameRt.sizeDelta = new Vector2(-30f, 22f);
            TextMeshProUGUI nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Name";
            nameTmp.font = TMP_Settings.defaultFontAsset;
            nameTmp.fontSize = 12f;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;
            CHText nameText = nameGo.AddComponent<CHText>();

            //# ×N.
            GameObject countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(root.transform, false);
            RectTransform countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 1f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot     = new Vector2(1f, 1f);
            countRt.anchoredPosition = new Vector2(-4f, -4f);
            countRt.sizeDelta = new Vector2(40f, 22f);
            TextMeshProUGUI countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 10f;
            countTmp.alignment = TextAlignmentOptions.Right;
            countTmp.color = YellowAccent;
            countTmp.raycastTarget = false;
            CHText countText = countGo.AddComponent<CHText>();
            countGo.SetActive(false);

            //# 설명.
            GameObject descGo = new GameObject("DescText", typeof(RectTransform));
            descGo.transform.SetParent(root.transform, false);
            RectTransform descRt = (RectTransform)descGo.transform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0f);
            descRt.pivot     = new Vector2(0f, 0f);
            descRt.anchoredPosition = new Vector2(20f, 4f);
            descRt.sizeDelta = new Vector2(-24f, 22f);
            TextMeshProUGUI descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Description";
            descTmp.font = TMP_Settings.defaultFontAsset;
            descTmp.fontSize = 10f;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.820f, 0.835f, 0.859f, 1f);
            descTmp.enableWordWrapping = false;
            descTmp.overflowMode = TextOverflowModes.Ellipsis;
            descTmp.raycastTarget = false;
            CHText descText = descGo.AddComponent<CHText>();

            //# BuildModalCardCell 부착 + 필드 주입.
            BuildModalCardCell cell = root.AddComponent<BuildModalCardCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObjectField(so, "_frame",     frameImg);
            SetObjectField(so, "_nameText",  nameText);
            SetObjectField(so, "_countText", countText);
            SetObjectField(so, "_descText",  descText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== BuildModalPopup =====
        private static void BuildModalPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "BuildModalPopup";

            //# 루트 — full-stretch overlay.
            GameObject root = new GameObject(PrefabName, typeof(RectTransform));
            SetFullStretch((RectTransform)root.transform);
            BuildModalPopup popup = root.AddComponent<BuildModalPopup>();

            //# Dim — 전체 화면 + Button.
            GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            Image dimImg = dimGo.GetComponent<Image>();
            dimImg.sprite = LairUIPrefabBuilder.GetUISprite();
            dimImg.color = ModalDim;
            Button dimBtn = dimGo.GetComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            CHButton dimChButton = dimGo.AddComponent<CHButton>();

            //# Modal body 640×480 center.
            GameObject bodyGo = new GameObject("ModalBody", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            RectTransform bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot     = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = new Vector2(640f, 480f);
            Image bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = ModalBg;

            //# 노란 테두리 2px — Outline 은 Image 와 같은 GameObject 에 (기획서 §3.1).
            Outline bodyOutline = bodyGo.AddComponent<Outline>();
            bodyOutline.effectColor = YellowAccent;
            bodyOutline.effectDistance = new Vector2(2f, -2f);

            //# Header — 타이틀 + X 버튼.
            GameObject titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(bodyGo.transform, false);
            RectTransform titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot     = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(16f, -16f);
            titleRt.sizeDelta = new Vector2(-80f, 24f);
            TextMeshProUGUI titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "빌드 상세";
            titleTmp.font = TMP_Settings.defaultFontAsset;
            titleTmp.fontSize = 14f;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = Color.white;
            titleGo.AddComponent<CHText>();

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(bodyGo.transform, false);
            RectTransform closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot     = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-16f, -16f);
            closeRt.sizeDelta = new Vector2(24f, 24f);
            Image closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = LairUIPrefabBuilder.GetUISprite();
            closeImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Button closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            CHButton closeChButton = closeGo.AddComponent<CHButton>();

            GameObject closeXGo = new GameObject("X", typeof(RectTransform));
            closeXGo.transform.SetParent(closeGo.transform, false);
            SetFullStretch((RectTransform)closeXGo.transform);
            TextMeshProUGUI closeXTmp = closeXGo.AddComponent<TextMeshProUGUI>();
            closeXTmp.text = "×";
            closeXTmp.font = TMP_Settings.defaultFontAsset;
            closeXTmp.fontSize = 16f;
            closeXTmp.alignment = TextAlignmentOptions.Center;
            closeXTmp.color = Color.white;
            closeXTmp.raycastTarget = false;

            //# Divider (구분선).
            GameObject dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(bodyGo.transform, false);
            RectTransform dividerRt = (RectTransform)dividerGo.transform;
            dividerRt.anchorMin = new Vector2(0.5f, 0f);
            dividerRt.anchorMax = new Vector2(0.5f, 1f);
            dividerRt.pivot     = new Vector2(0.5f, 0.5f);
            dividerRt.anchoredPosition = Vector2.zero;
            dividerRt.sizeDelta = new Vector2(1f, -64f);
            Image dividerImg = dividerGo.GetComponent<Image>();
            dividerImg.sprite = LairUIPrefabBuilder.GetUISprite();
            dividerImg.color = DividerColor;
            dividerImg.raycastTarget = false;

            //# 좌(패시브) / 우(액티브) 섹션 — CHPoolingScrollView<BuildModalCardCell, BuildEntry> (§2.7.2 v0.8).
            BuildModalCardPoolingScrollView passiveScroll = BuildModalSection(bodyGo.transform, "PassiveSection", true,  out CHText passiveEmpty);
            BuildModalCardPoolingScrollView activeScroll  = BuildModalSection(bodyGo.transform, "ActiveSection",  false, out CHText activeEmpty);

            //# 필드 주입 — v0.8: _passiveContent/_activeContent (Transform) → _passiveScrollView/_activeScrollView (BuildModalCardPoolingScrollView).
            //# _cellPrefab 직렬화는 제거됨 (CHPoolingScrollView 의 _origin 으로 이주).
            SerializedObject so = new SerializedObject(popup);
            SetObjectField(so, "_dimButton",         dimChButton);
            SetObjectField(so, "_closeButton",       closeChButton);
            SetObjectField(so, "_passiveScrollView", passiveScroll);
            SetObjectField(so, "_activeScrollView",  activeScroll);
            SetObjectField(so, "_passiveEmptyText",  passiveEmpty);
            SetObjectField(so, "_activeEmptyText",   activeEmpty);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# 모달 한 섹션 — CHPoolingScrollView<BuildModalCardCell, BuildEntry> (§2.7.2 v0.8).
        //# 라벨 + ScrollRect[Viewport(RectMask2D + raycast Image) + Content(RectTransform 만)] + 빈 상태 텍스트.
        //# 반환값: BuildModalCardPoolingScrollView 컴포넌트 (BuildModalPopup 이 _passiveScrollView/_activeScrollView 로 가리킴).
        private static BuildModalCardPoolingScrollView BuildModalSection(Transform parent, string name, bool left, out CHText emptyText)
        {
            GameObject sectionGo = new GameObject(name, typeof(RectTransform));
            sectionGo.transform.SetParent(parent, false);
            RectTransform rt = (RectTransform)sectionGo.transform;
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.offsetMin = new Vector2(left ? 16f : 8f, 16f);
            rt.offsetMax = new Vector2(left ? -8f : -16f, -52f);

            //# 라벨.
            GameObject labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(sectionGo.transform, false);
            RectTransform labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot     = new Vector2(0f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(0f, 20f);
            TextMeshProUGUI labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = left ? "패시브" : "액티브";
            labelTmp.font = TMP_Settings.defaultFontAsset;
            labelTmp.fontSize = 12f;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = Color.white;

            //# ScrollRect — CHPoolingScrollView 와 함께 (§2.7.2 v0.8). Elastic 0.1 / Inertia / Scrollbar 없음.
            GameObject scrollGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(sectionGo.transform, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 0f);
            scrollRt.offsetMax = new Vector2(0f, -24f);
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

            //# Viewport — RectMask2D + Image (alpha 0.001, raycast). drag receiver.
            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            RectTransform viewportRt = (RectTransform)viewportGo.transform;
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.pivot = new Vector2(0.5f, 0.5f);
            viewportRt.anchoredPosition = Vector2.zero;
            viewportRt.sizeDelta = Vector2.zero;
            Image viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.sprite = LairUIPrefabBuilder.GetUISprite();
            viewportImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImg.raycastTarget = true;

            //# Content — RectTransform 만. VLG/CSF 부착 X.
            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);

            //# BuildModalCardCell prefab 인스턴스를 Content 첫 자식 → CHPoolingScrollView 의 _origin.
            GameObject cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BuildModalCardCell.prefab");
            GameObject originInst = null;
            if (cellPrefab != null)
            {
                originInst = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab, contentGo.transform);
                originInst.SetActive(false);
            }
            else
            {
                Debug.LogWarning("[LairSpawnerStatusUIBuilder] BuildModalCardCell.prefab 미발견 — BuildModalCardCellPrefab 선행 필요");
            }

            //# ScrollRect 참조 묶기.
            sr.viewport = viewportRt;
            sr.content = contentRt;

            //# CHPoolingScrollView 파생 컴포넌트 + 직렬화 필드 wire-up.
            //# _itemSize 는 wire-up 안 함 (v0.9 P1) — BuildModalCardCell prefab sizeDelta (303, 56) 가 단일 진실.
            //# _columnCount = 0 auto — viewport 303 / itemSize 303 = 1 자동 (BuildPanel B1 위험 없음).
            BuildModalCardPoolingScrollView modalScrollView = scrollGo.AddComponent<BuildModalCardPoolingScrollView>();
            SerializedObject msvSo = new SerializedObject(modalScrollView);
            if (originInst != null) SetObjectField(msvSo, "_origin", originInst);
            SetVector2Field(msvSo, "_itemGap", new Vector2(0f, 4f));
            SetRectOffsetField(msvSo, "_padding", 0, 0, 0, 0);
            SetEnumField(msvSo, "_scrollDirection", (int)PoolingScrollViewDirection.Vertical);
            SetEnumField(msvSo, "_align", (int)PoolingScrollViewAlign.LeftOrTop);
            SetIntField(msvSo, "_rowCount", 0);
            SetIntField(msvSo, "_columnCount", 0);
            SetIntField(msvSo, "_poolItemCount", 0);
            msvSo.ApplyModifiedPropertiesWithoutUndo();

            //# Empty state — ScrollRect 위에 떠 있음.
            GameObject emptyGo = new GameObject("EmptyText", typeof(RectTransform));
            emptyGo.transform.SetParent(sectionGo.transform, false);
            RectTransform emptyRt = (RectTransform)emptyGo.transform;
            emptyRt.anchorMin = Vector2.zero;
            emptyRt.anchorMax = Vector2.one;
            emptyRt.offsetMin = new Vector2(0f, 0f);
            emptyRt.offsetMax = new Vector2(0f, -24f);
            TextMeshProUGUI emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyTmp.text = "아직 픽한 카드가 없습니다";
            emptyTmp.font = TMP_Settings.defaultFontAsset;
            emptyTmp.fontSize = 10f;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.color = GrayLabel;
            emptyTmp.raycastTarget = false;
            emptyText = emptyGo.AddComponent<CHText>();

            return modalScrollView;
        }

        //# ===== 공통 헬퍼 =====

        //# v1.0 — IconRow 의 한 슬롯 (Enhance 또는 Spawn) 빌드 헬퍼.
        //# - circle: 30×30 원 (anchor (0, 0.5) / pivot (0, 0.5) — 기존 단일 슬롯 패턴 유지).
        //# - letter: 16pt 1자 (circle 자식 full-stretch).
        //# - badge:  14pt ×N (circle 우하단 모서리 — anchor (1, 0) / pivot (0, 1) / anchoredPos (-2, 1) / sizeDelta (24, 14)).
        //# anchoredPosition.x 가 슬롯 간 유일한 차이 (12 = Enhance / 68 = Spawn — §2.3.1 v1.0).
        private static (Image circle, CHText letter, CHText badge) BuildIconSlot(
            Transform rowParent, string circleName, string letterName, string badgeName,
            float xPos, string defaultLetter)
        {
            //# Circle.
            GameObject circleGo = new GameObject(circleName, typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(rowParent, false);
            RectTransform circleRt = (RectTransform)circleGo.transform;
            circleRt.anchorMin = new Vector2(0f, 0.5f);
            circleRt.anchorMax = new Vector2(0f, 0.5f);
            circleRt.pivot     = new Vector2(0f, 0.5f);
            circleRt.anchoredPosition = new Vector2(xPos, 0f);
            circleRt.sizeDelta = new Vector2(30f, 30f);
            Image circleImg = circleGo.GetComponent<Image>();
            circleImg.sprite = LairUIPrefabBuilder.GetUISprite();
            circleImg.color = Color.gray;
            circleImg.raycastTarget = false;

            //# Letter (circle 자식, full-stretch 16pt).
            GameObject letterGo = new GameObject(letterName, typeof(RectTransform));
            letterGo.transform.SetParent(circleGo.transform, false);
            SetFullStretch((RectTransform)letterGo.transform);
            TextMeshProUGUI letterTmp = letterGo.AddComponent<TextMeshProUGUI>();
            letterTmp.text = defaultLetter;
            letterTmp.font = TMP_Settings.defaultFontAsset;
            letterTmp.fontSize = 16f;
            letterTmp.alignment = TextAlignmentOptions.Center;
            letterTmp.color = Color.black;
            letterTmp.raycastTarget = false;
            CHText letterText = letterGo.AddComponent<CHText>();

            //# Badge (circle 자식, 우하단 14pt ×N + 노랑 + outline). 배지 우측 끝이 circle 우측 +22.
            GameObject badgeGo = new GameObject(badgeName, typeof(RectTransform));
            badgeGo.transform.SetParent(circleGo.transform, false);
            RectTransform badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(1f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot     = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(-2f, 1f);
            badgeRt.sizeDelta = new Vector2(24f, 14f);
            TextMeshProUGUI badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "×2";
            badgeTmp.font = TMP_Settings.defaultFontAsset;
            badgeTmp.fontSize = 14f;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = YellowAccent;
            badgeTmp.outlineColor = Color.black;
            badgeTmp.outlineWidth = 0.2f;
            badgeTmp.raycastTarget = false;
            CHText badgeText = badgeGo.AddComponent<CHText>();
            //# 슬롯은 기본 비활성 (강화/생산 카드 없을 때) — Cell 의 RebindIconRow / OnEnable 이 토글.
            circleGo.SetActive(false);

            return (circleImg, letterText, badgeText);
        }

        private static void SaveAsPrefab(GameObject root, string prefabName)
        {
            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[LairSpawnerStatusUIBuilder] {prefabName} 빌드 완료 (Addressable X)");
        }

        private static void SaveAndRegisterPrefab(GameObject root, string prefabName,
            AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            string prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            string guid = AssetDatabase.AssetPathToGUID(prefabPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);

            Debug.Log($"[LairSpawnerStatusUIBuilder] {prefabName} 빌드 + Addressable 등록 (address={entry.address})");
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
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
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
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.vector2Value = value;
        }

        private static void SetIntField(SerializedObject so, string fieldName, int value)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.intValue = value;
        }

        private static void SetEnumField(SerializedObject so, string fieldName, int enumIndex)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.enumValueIndex = enumIndex;
        }

        //# RectOffset 은 SerializedProperty 의 하위 child (m_Left/m_Right/m_Top/m_Bottom).
        private static void SetRectOffsetField(SerializedObject so, string fieldName, int left, int right, int top, int bottom)
        {
            SerializedProperty prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
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
