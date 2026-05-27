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

        //# 셀 치수 (기획서 §3.2 v0.4 — 64×80 1.4× 확대).
        private const float CellWidth  = 64f;
        private const float CellHeight = 80f;
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

            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairSpawnerStatusUIBuilder] Addressables 미설정");
                return;
            }
            var group = settings.FindGroup(ResourceGroup);
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
            Debug.Log("[LairSpawnerStatusUIBuilder] 스포너 상태 UI 4종 + 모달 셀 빌드 완료");
        }

        //# ===== SpawnerStatusCell =====
        private static void BuildCellPrefab()
        {
            const string PrefabName = "SpawnerStatusCell";

            var root = new GameObject(PrefabName, typeof(RectTransform), typeof(Image), typeof(Button));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(CellWidth, CellHeight);

            var bg = root.GetComponent<Image>();
            bg.sprite = LairUIPrefabBuilder.GetUISprite();
            bg.type = Image.Type.Sliced;
            bg.color = CellBackground;
            var btn = root.GetComponent<Button>();
            btn.targetGraphic = bg;
            var chBtn = root.AddComponent<CHButton>();

            //# Border — 활성 시 노란 테두리 (#FBBF24). 비활성 시 alpha 0 으로 숨김.
            var borderGo = new GameObject("Border", typeof(RectTransform));
            borderGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)borderGo.transform);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.sprite = LairUIPrefabBuilder.GetUISprite();
            borderImg.type = Image.Type.Sliced;
            borderImg.color = new Color(0f, 0f, 0f, 0f);
            borderImg.raycastTarget = false;

            //# IconRow — 셀 상단 20px (v0.4). 상단 padding 4 → anchoredPosition.y = -4.
            var iconRowGo = new GameObject("IconRow", typeof(RectTransform));
            iconRowGo.transform.SetParent(root.transform, false);
            var iconRowRt = (RectTransform)iconRowGo.transform;
            iconRowRt.anchorMin = new Vector2(0f, 1f);
            iconRowRt.anchorMax = new Vector2(1f, 1f);
            iconRowRt.pivot     = new Vector2(0.5f, 1f);
            iconRowRt.anchoredPosition = new Vector2(0f, -4f);
            iconRowRt.sizeDelta = new Vector2(0f, 20f);

            //# IconCircle 안에 들어가는 원 (14×14 v0.4) — Image (sprite = UISprite, 색 = 종 배경).
            var circleGo = new GameObject("IconCircle", typeof(RectTransform), typeof(Image));
            circleGo.transform.SetParent(iconRowGo.transform, false);
            var circleRt = (RectTransform)circleGo.transform;
            circleRt.anchorMin = new Vector2(0f, 0.5f);
            circleRt.anchorMax = new Vector2(0f, 0.5f);
            circleRt.pivot     = new Vector2(0f, 0.5f);
            circleRt.anchoredPosition = new Vector2(6f, 0f);
            circleRt.sizeDelta = new Vector2(14f, 14f);
            var circleImg = circleGo.GetComponent<Image>();
            circleImg.sprite = LairUIPrefabBuilder.GetUISprite();
            circleImg.color = Color.gray;
            circleImg.raycastTarget = false;

            //# IconLetter — TMP_Text 1자. 11pt (v0.4).
            var letterGo = new GameObject("IconLetter", typeof(RectTransform));
            letterGo.transform.SetParent(circleGo.transform, false);
            SetFullStretch((RectTransform)letterGo.transform);
            var letterTmp = letterGo.AddComponent<TextMeshProUGUI>();
            letterTmp.text = "H";
            letterTmp.font = TMP_Settings.defaultFontAsset;
            letterTmp.fontSize = 11f;
            letterTmp.alignment = TextAlignmentOptions.Center;
            letterTmp.color = Color.black;
            letterTmp.raycastTarget = false;
            var letterText = letterGo.AddComponent<CHText>();

            //# IconBadge — 원 우측 하단 ×N (TMP_Text, outline). 10pt (v0.4).
            var badgeGo = new GameObject("IconBadge", typeof(RectTransform));
            badgeGo.transform.SetParent(circleGo.transform, false);
            var badgeRt = (RectTransform)badgeGo.transform;
            badgeRt.anchorMin = new Vector2(1f, 0f);
            badgeRt.anchorMax = new Vector2(1f, 0f);
            badgeRt.pivot     = new Vector2(0f, 1f);
            badgeRt.anchoredPosition = new Vector2(-2f, 1f);
            badgeRt.sizeDelta = new Vector2(16f, 10f);
            var badgeTmp = badgeGo.AddComponent<TextMeshProUGUI>();
            badgeTmp.text = "×2";
            badgeTmp.font = TMP_Settings.defaultFontAsset;
            badgeTmp.fontSize = 10f;
            badgeTmp.alignment = TextAlignmentOptions.Center;
            badgeTmp.color = YellowAccent;
            badgeTmp.outlineColor = Color.black;
            badgeTmp.outlineWidth = 0.2f;
            badgeTmp.raycastTarget = false;
            var badgeText = badgeGo.AddComponent<CHText>();

            //# 본체 row — 색칩 + 종명 + ×N. 높이 28 (v0.4).
            //# 세로 배치: 상 padding 4 + 아이콘 row 20 + 간격 4 = 28 → BodyRow anchoredPosition.y = -28.
            var bodyRowGo = new GameObject("BodyRow", typeof(RectTransform));
            bodyRowGo.transform.SetParent(root.transform, false);
            var bodyRowRt = (RectTransform)bodyRowGo.transform;
            bodyRowRt.anchorMin = new Vector2(0f, 1f);
            bodyRowRt.anchorMax = new Vector2(1f, 1f);
            bodyRowRt.pivot     = new Vector2(0.5f, 1f);
            bodyRowRt.anchoredPosition = new Vector2(0f, -28f);
            bodyRowRt.sizeDelta = new Vector2(0f, 28f);

            //# 색칩 14×14 (v0.4). 좌측 padding 6.
            var chipGo = new GameObject("ColorChip", typeof(RectTransform), typeof(Image));
            chipGo.transform.SetParent(bodyRowGo.transform, false);
            var chipRt = (RectTransform)chipGo.transform;
            chipRt.anchorMin = new Vector2(0f, 0.5f);
            chipRt.anchorMax = new Vector2(0f, 0.5f);
            chipRt.pivot     = new Vector2(0f, 0.5f);
            chipRt.anchoredPosition = new Vector2(6f, 0f);
            chipRt.sizeDelta = new Vector2(14f, 14f);
            var chipImg = chipGo.GetComponent<Image>();
            chipImg.sprite = LairUIPrefabBuilder.GetUISprite();
            chipImg.color = Color.green;
            chipImg.raycastTarget = false;

            //# 종명 (영문). 15pt (v0.4). 색칩 우측 gap 6 → offsetMin.x = 6 + 14 + 6 = 26.
            var speciesGo = new GameObject("SpeciesText", typeof(RectTransform));
            speciesGo.transform.SetParent(bodyRowGo.transform, false);
            var speciesRt = (RectTransform)speciesGo.transform;
            speciesRt.anchorMin = new Vector2(0f, 0f);
            speciesRt.anchorMax = new Vector2(1f, 1f);
            speciesRt.offsetMin = new Vector2(26f, 0f);
            speciesRt.offsetMax = new Vector2(-6f, 0f);
            var speciesTmp = speciesGo.AddComponent<TextMeshProUGUI>();
            speciesTmp.text = "Wisp";
            speciesTmp.font = TMP_Settings.defaultFontAsset;
            speciesTmp.fontSize = 15f;
            speciesTmp.alignment = TextAlignmentOptions.Left;
            speciesTmp.color = Color.white;
            speciesTmp.overflowMode = TextOverflowModes.Truncate;
            speciesTmp.raycastTarget = false;
            var speciesText = speciesGo.AddComponent<CHText>();

            //# ×N — 본체 row 우측 정렬. 14pt (v0.4).
            var countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(bodyRowGo.transform, false);
            var countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 0f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot     = new Vector2(1f, 0.5f);
            countRt.anchoredPosition = new Vector2(-4f, 0f);
            countRt.sizeDelta = new Vector2(22f, 0f);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 14f;
            countTmp.alignment = TextAlignmentOptions.Right;
            countTmp.color = YellowAccent;
            countTmp.raycastTarget = false;
            var countText = countGo.AddComponent<CHText>();
            countGo.SetActive(false);

            //# 진행 바 — Background + Fill. 8px 높이 (v0.4). 좌우 padding 6 → sizeDelta x = -12, 결과 폭 52.
            //# 세로 위치: BodyRow 하단(y=24) - gap 4 = ProgressBar top y=20 → ProgressBar bottom y=12.
            //# anchoredPosition.y = +12 (셀 하단으로부터 12, pivot.y=0). 진행 바 아래 잔여 padding 12px (기획서 §2.2.1 "잔여 padding 8" + 하 padding 4).
            var progressBgGo = new GameObject("ProgressBackground", typeof(RectTransform), typeof(Image));
            progressBgGo.transform.SetParent(root.transform, false);
            var progressBgRt = (RectTransform)progressBgGo.transform;
            progressBgRt.anchorMin = new Vector2(0f, 0f);
            progressBgRt.anchorMax = new Vector2(1f, 0f);
            progressBgRt.pivot     = new Vector2(0.5f, 0f);
            progressBgRt.anchoredPosition = new Vector2(0f, 12f);
            progressBgRt.sizeDelta = new Vector2(-12f, 8f);
            var progressBgImg = progressBgGo.GetComponent<Image>();
            progressBgImg.sprite = LairUIPrefabBuilder.GetUISprite();
            progressBgImg.color = BarBackground;
            progressBgImg.raycastTarget = false;

            var fillGo = new GameObject("Fill", typeof(RectTransform), typeof(Image));
            fillGo.transform.SetParent(progressBgGo.transform, false);
            SetFullStretch((RectTransform)fillGo.transform);
            var fillImg = fillGo.GetComponent<Image>();
            fillImg.sprite = LairUIPrefabBuilder.GetUISprite();
            fillImg.color = SpawnerStatusCell.CoolColor;
            fillImg.type = Image.Type.Filled;
            fillImg.fillMethod = Image.FillMethod.Horizontal;
            fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
            fillImg.fillAmount = 0f;
            fillImg.raycastTarget = false;

            //# SpawnerStatusCell 컴포넌트 부착 + 필드 주입.
            var cell = root.AddComponent<SpawnerStatusCell>();
            var so = new SerializedObject(cell);
            SetObjectField(so, "_border",       borderImg);
            SetObjectField(so, "_colorChip",    chipImg);
            SetObjectField(so, "_speciesText",  speciesText);
            SetObjectField(so, "_countText",    countText);
            SetObjectField(so, "_progressFill", fillImg);
            SetObjectField(so, "_button",       chBtn);
            SetObjectField(so, "_iconRow",      iconRowRt);
            SetObjectField(so, "_iconCircle",   circleImg);
            SetObjectField(so, "_iconLetter",   letterText);
            SetObjectField(so, "_iconBadge",    badgeText);
            so.ApplyModifiedPropertiesWithoutUndo();

            //# IconRow 는 기본 숨김 (강화 없음 시).
            iconRowGo.SetActive(false);

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== SpawnerStatusPanel =====
        private static void BuildPanelPrefab()
        {
            const string PrefabName = "SpawnerStatusPanel";

            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            //# 화면 하단 가운데 정렬, Pivot (0.5, 0), anchored Y = +24.
            rootRt.anchorMin = new Vector2(0.5f, 0f);
            rootRt.anchorMax = new Vector2(0.5f, 0f);
            rootRt.pivot     = new Vector2(0.5f, 0f);
            rootRt.anchoredPosition = new Vector2(0f, 24f);
            //# 430 = 6×64 + 5×6 + 2×8 (v0.4 기획서 §2.1).
            rootRt.sizeDelta = new Vector2(430f, CellHeight);

            //# Container — HorizontalLayoutGroup.
            var containerGo = new GameObject("Container", typeof(RectTransform));
            containerGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)containerGo.transform);
            var hlg = containerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = CellSpacing;
            hlg.padding = new RectOffset((int)PanelHorizontalPadding, (int)PanelHorizontalPadding, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;

            //# SpawnerStatusPanel 컴포넌트 부착 + _container/_cellPrefab 주입.
            var panel = root.AddComponent<SpawnerStatusPanel>();
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/SpawnerStatusCell.prefab");
            var so = new SerializedObject(panel);
            SetObjectField(so, "_container",  containerGo.transform);
            SetObjectField(so, "_cellPrefab", cellPrefab);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAsPrefab(root, PrefabName);
        }

        //# ===== SpawnerStatusTooltip =====
        private static void BuildTooltipPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            const string PrefabName = "SpawnerStatusTooltip";

            //# 루트 — UIBase 의 SetActive(true) 기반, full-stretch overlay 가능.
            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            SetFullStretch(rootRt);
            var tooltip = root.AddComponent<SpawnerStatusTooltip>();

            //# Body — 180 가변높이. 위치는 런타임 PositionAboveAnchor 가 결정.
            //# pivot/anchor 는 SpawnerStatusTooltip.PositionAboveAnchor 가 런타임에 갱신 (anchor 0.5,0.5 / pivot 0.5,0).
            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot     = new Vector2(0.5f, 0f);
            bodyRt.anchoredPosition = new Vector2(0f, 0f);
            bodyRt.sizeDelta = new Vector2(180f, 90f);
            var bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = TooltipBg;

            //# 노란 테두리 (#FBBF24 1px) — Outline 은 Image 와 같은 GameObject 에 부착 (advisor BLOCKER 2).
            var bodyOutline = bodyGo.AddComponent<Outline>();
            bodyOutline.effectColor = YellowAccent;
            bodyOutline.effectDistance = new Vector2(1f, -1f);

            //# Header.
            var headerGo = new GameObject("Header", typeof(RectTransform));
            headerGo.transform.SetParent(bodyGo.transform, false);
            var headerRt = (RectTransform)headerGo.transform;
            headerRt.anchorMin = new Vector2(0f, 1f);
            headerRt.anchorMax = new Vector2(1f, 1f);
            headerRt.pivot     = new Vector2(0.5f, 1f);
            headerRt.anchoredPosition = new Vector2(0f, -8f);
            headerRt.sizeDelta = new Vector2(-16f, 24f);
            var headerTmp = headerGo.AddComponent<TextMeshProUGUI>();
            headerTmp.text = "Spawner #0";
            headerTmp.font = TMP_Settings.defaultFontAsset;
            headerTmp.fontSize = 11f;
            headerTmp.alignment = TextAlignmentOptions.Left;
            headerTmp.color = Color.white;
            var headerText = headerGo.AddComponent<CHText>();

            //# Buff line.
            var buffGo = new GameObject("BuffText", typeof(RectTransform));
            buffGo.transform.SetParent(bodyGo.transform, false);
            var buffRt = (RectTransform)buffGo.transform;
            buffRt.anchorMin = new Vector2(0f, 0f);
            buffRt.anchorMax = new Vector2(1f, 1f);
            buffRt.offsetMin = new Vector2(8f, 8f);
            buffRt.offsetMax = new Vector2(-8f, -36f);
            var buffTmp = buffGo.AddComponent<TextMeshProUGUI>();
            buffTmp.text = "적용된 강화 없음";
            buffTmp.font = TMP_Settings.defaultFontAsset;
            buffTmp.fontSize = 10f;
            buffTmp.alignment = TextAlignmentOptions.TopLeft;
            buffTmp.color = GrayLabel;
            var buffText = buffGo.AddComponent<CHText>();

            //# SerializedObject 주입.
            var so = new SerializedObject(tooltip);
            SetObjectField(so, "_root",       bodyRt);
            SetObjectField(so, "_headerText", headerText);
            SetObjectField(so, "_buffText",   buffText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# ===== BuildModalCardCell =====
        private static void BuildModalCardCellPrefab()
        {
            const string PrefabName = "BuildModalCardCell";

            var root = new GameObject(PrefabName, typeof(RectTransform));
            var rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(280f, 56f);

            //# 프레임 — 좌측 세로 막대.
            var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(Image));
            frameGo.transform.SetParent(root.transform, false);
            var frameRt = (RectTransform)frameGo.transform;
            frameRt.anchorMin = new Vector2(0f, 0.5f);
            frameRt.anchorMax = new Vector2(0f, 0.5f);
            frameRt.pivot     = new Vector2(0f, 0.5f);
            frameRt.anchoredPosition = new Vector2(4f, 0f);
            frameRt.sizeDelta = new Vector2(8f, 40f);
            var frameImg = frameGo.GetComponent<Image>();
            frameImg.sprite = LairUIPrefabBuilder.GetUISprite();
            frameImg.color = Color.gray;
            frameImg.raycastTarget = false;

            //# 카드 이름.
            var nameGo = new GameObject("NameText", typeof(RectTransform));
            nameGo.transform.SetParent(root.transform, false);
            var nameRt = (RectTransform)nameGo.transform;
            nameRt.anchorMin = new Vector2(0f, 1f);
            nameRt.anchorMax = new Vector2(1f, 1f);
            nameRt.pivot     = new Vector2(0f, 1f);
            nameRt.anchoredPosition = new Vector2(20f, -4f);
            nameRt.sizeDelta = new Vector2(-30f, 22f);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            nameTmp.text = "Name";
            nameTmp.font = TMP_Settings.defaultFontAsset;
            nameTmp.fontSize = 12f;
            nameTmp.alignment = TextAlignmentOptions.Left;
            nameTmp.color = Color.white;
            nameTmp.raycastTarget = false;
            var nameText = nameGo.AddComponent<CHText>();

            //# ×N.
            var countGo = new GameObject("CountText", typeof(RectTransform));
            countGo.transform.SetParent(root.transform, false);
            var countRt = (RectTransform)countGo.transform;
            countRt.anchorMin = new Vector2(1f, 1f);
            countRt.anchorMax = new Vector2(1f, 1f);
            countRt.pivot     = new Vector2(1f, 1f);
            countRt.anchoredPosition = new Vector2(-4f, -4f);
            countRt.sizeDelta = new Vector2(40f, 22f);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            countTmp.text = "×2";
            countTmp.font = TMP_Settings.defaultFontAsset;
            countTmp.fontSize = 10f;
            countTmp.alignment = TextAlignmentOptions.Right;
            countTmp.color = YellowAccent;
            countTmp.raycastTarget = false;
            var countText = countGo.AddComponent<CHText>();
            countGo.SetActive(false);

            //# 설명.
            var descGo = new GameObject("DescText", typeof(RectTransform));
            descGo.transform.SetParent(root.transform, false);
            var descRt = (RectTransform)descGo.transform;
            descRt.anchorMin = new Vector2(0f, 0f);
            descRt.anchorMax = new Vector2(1f, 0f);
            descRt.pivot     = new Vector2(0f, 0f);
            descRt.anchoredPosition = new Vector2(20f, 4f);
            descRt.sizeDelta = new Vector2(-24f, 22f);
            var descTmp = descGo.AddComponent<TextMeshProUGUI>();
            descTmp.text = "Description";
            descTmp.font = TMP_Settings.defaultFontAsset;
            descTmp.fontSize = 10f;
            descTmp.alignment = TextAlignmentOptions.Left;
            descTmp.color = new Color(0.820f, 0.835f, 0.859f, 1f);
            descTmp.enableWordWrapping = false;
            descTmp.overflowMode = TextOverflowModes.Ellipsis;
            descTmp.raycastTarget = false;
            var descText = descGo.AddComponent<CHText>();

            //# BuildModalCardCell 부착 + 필드 주입.
            var cell = root.AddComponent<BuildModalCardCell>();
            var so = new SerializedObject(cell);
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
            var root = new GameObject(PrefabName, typeof(RectTransform));
            SetFullStretch((RectTransform)root.transform);
            var popup = root.AddComponent<BuildModalPopup>();

            //# Dim — 전체 화면 + Button.
            var dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(root.transform, false);
            SetFullStretch((RectTransform)dimGo.transform);
            var dimImg = dimGo.GetComponent<Image>();
            dimImg.sprite = LairUIPrefabBuilder.GetUISprite();
            dimImg.color = ModalDim;
            var dimBtn = dimGo.GetComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            var dimChButton = dimGo.AddComponent<CHButton>();

            //# Modal body 640×480 center.
            var bodyGo = new GameObject("ModalBody", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            var bodyRt = (RectTransform)bodyGo.transform;
            bodyRt.anchorMin = new Vector2(0.5f, 0.5f);
            bodyRt.anchorMax = new Vector2(0.5f, 0.5f);
            bodyRt.pivot     = new Vector2(0.5f, 0.5f);
            bodyRt.anchoredPosition = Vector2.zero;
            bodyRt.sizeDelta = new Vector2(640f, 480f);
            var bodyImg = bodyGo.GetComponent<Image>();
            bodyImg.sprite = LairUIPrefabBuilder.GetUISprite();
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = ModalBg;

            //# 노란 테두리 2px — Outline 은 Image 와 같은 GameObject 에 (기획서 §3.1).
            var bodyOutline = bodyGo.AddComponent<Outline>();
            bodyOutline.effectColor = YellowAccent;
            bodyOutline.effectDistance = new Vector2(2f, -2f);

            //# Header — 타이틀 + X 버튼.
            var titleGo = new GameObject("Title", typeof(RectTransform));
            titleGo.transform.SetParent(bodyGo.transform, false);
            var titleRt = (RectTransform)titleGo.transform;
            titleRt.anchorMin = new Vector2(0f, 1f);
            titleRt.anchorMax = new Vector2(1f, 1f);
            titleRt.pivot     = new Vector2(0f, 1f);
            titleRt.anchoredPosition = new Vector2(16f, -16f);
            titleRt.sizeDelta = new Vector2(-80f, 24f);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            titleTmp.text = "빌드 상세";
            titleTmp.font = TMP_Settings.defaultFontAsset;
            titleTmp.fontSize = 14f;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleTmp.color = Color.white;
            titleGo.AddComponent<CHText>();

            var closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(bodyGo.transform, false);
            var closeRt = (RectTransform)closeGo.transform;
            closeRt.anchorMin = new Vector2(1f, 1f);
            closeRt.anchorMax = new Vector2(1f, 1f);
            closeRt.pivot     = new Vector2(1f, 1f);
            closeRt.anchoredPosition = new Vector2(-16f, -16f);
            closeRt.sizeDelta = new Vector2(24f, 24f);
            var closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = LairUIPrefabBuilder.GetUISprite();
            closeImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            var closeBtn = closeGo.GetComponent<Button>();
            closeBtn.targetGraphic = closeImg;
            var closeChButton = closeGo.AddComponent<CHButton>();

            var closeXGo = new GameObject("X", typeof(RectTransform));
            closeXGo.transform.SetParent(closeGo.transform, false);
            SetFullStretch((RectTransform)closeXGo.transform);
            var closeXTmp = closeXGo.AddComponent<TextMeshProUGUI>();
            closeXTmp.text = "×";
            closeXTmp.font = TMP_Settings.defaultFontAsset;
            closeXTmp.fontSize = 16f;
            closeXTmp.alignment = TextAlignmentOptions.Center;
            closeXTmp.color = Color.white;
            closeXTmp.raycastTarget = false;

            //# Divider (구분선).
            var dividerGo = new GameObject("Divider", typeof(RectTransform), typeof(Image));
            dividerGo.transform.SetParent(bodyGo.transform, false);
            var dividerRt = (RectTransform)dividerGo.transform;
            dividerRt.anchorMin = new Vector2(0.5f, 0f);
            dividerRt.anchorMax = new Vector2(0.5f, 1f);
            dividerRt.pivot     = new Vector2(0.5f, 0.5f);
            dividerRt.anchoredPosition = Vector2.zero;
            dividerRt.sizeDelta = new Vector2(1f, -64f);
            var dividerImg = dividerGo.GetComponent<Image>();
            dividerImg.sprite = LairUIPrefabBuilder.GetUISprite();
            dividerImg.color = DividerColor;
            dividerImg.raycastTarget = false;

            //# 좌(패시브) 섹션 ScrollRect.
            var passiveContent = BuildModalSection(bodyGo.transform, "PassiveSection", true, out var passiveEmpty);
            //# 우(액티브) 섹션 ScrollRect.
            var activeContent  = BuildModalSection(bodyGo.transform, "ActiveSection",  false, out var activeEmpty);

            //# 모달 카드 셀 프리팹 로드.
            var cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/BuildModalCardCell.prefab");

            //# 필드 주입.
            var so = new SerializedObject(popup);
            SetObjectField(so, "_dimButton",        dimChButton);
            SetObjectField(so, "_closeButton",      closeChButton);
            SetObjectField(so, "_passiveContent",   passiveContent);
            SetObjectField(so, "_activeContent",    activeContent);
            SetObjectField(so, "_cellPrefab",       cellPrefab);
            SetObjectField(so, "_passiveEmptyText", passiveEmpty);
            SetObjectField(so, "_activeEmptyText",  activeEmpty);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, PrefabName, settings, group);
        }

        //# 모달 한 섹션 — ScrollRect + Content + 빈 상태 텍스트. Content Transform 반환.
        private static Transform BuildModalSection(Transform parent, string name, bool left, out CHText emptyText)
        {
            var sectionGo = new GameObject(name, typeof(RectTransform));
            sectionGo.transform.SetParent(parent, false);
            var rt = (RectTransform)sectionGo.transform;
            rt.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            rt.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            rt.offsetMin = new Vector2(left ? 16f : 8f, 16f);
            rt.offsetMax = new Vector2(left ? -8f : -16f, -52f);

            //# 라벨.
            var labelGo = new GameObject("Label", typeof(RectTransform));
            labelGo.transform.SetParent(sectionGo.transform, false);
            var labelRt = (RectTransform)labelGo.transform;
            labelRt.anchorMin = new Vector2(0f, 1f);
            labelRt.anchorMax = new Vector2(1f, 1f);
            labelRt.pivot     = new Vector2(0f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(0f, 20f);
            var labelTmp = labelGo.AddComponent<TextMeshProUGUI>();
            labelTmp.text = left ? "패시브" : "액티브";
            labelTmp.font = TMP_Settings.defaultFontAsset;
            labelTmp.fontSize = 12f;
            labelTmp.alignment = TextAlignmentOptions.Left;
            labelTmp.color = Color.white;

            //# ScrollRect viewport.
            var scrollGo = new GameObject("Scroll", typeof(RectTransform), typeof(ScrollRect), typeof(Image));
            scrollGo.transform.SetParent(sectionGo.transform, false);
            var scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 0f);
            scrollRt.offsetMax = new Vector2(0f, -24f);
            var scrollBg = scrollGo.GetComponent<Image>();
            scrollBg.sprite = LairUIPrefabBuilder.GetUISprite();
            scrollBg.color = new Color(0f, 0f, 0f, 0.2f);

            var sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;

            //# Mask 안에 Content.
            scrollGo.AddComponent<Mask>();

            var contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(scrollGo.transform, false);
            var contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = new Vector2(0f, 0f);
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.padding = new RectOffset(4, 4, 4, 4);
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            var fitter = contentGo.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = contentRt;

            //# Empty state — content 옆이 아니라 ScrollRect 위에 떠 있어야 한다.
            var emptyGo = new GameObject("EmptyText", typeof(RectTransform));
            emptyGo.transform.SetParent(scrollGo.transform, false);
            SetFullStretch((RectTransform)emptyGo.transform);
            var emptyTmp = emptyGo.AddComponent<TextMeshProUGUI>();
            emptyTmp.text = "아직 픽한 카드가 없습니다";
            emptyTmp.font = TMP_Settings.defaultFontAsset;
            emptyTmp.fontSize = 10f;
            emptyTmp.alignment = TextAlignmentOptions.Center;
            emptyTmp.color = GrayLabel;
            emptyTmp.raycastTarget = false;
            emptyText = emptyGo.AddComponent<CHText>();

            return contentRt;
        }

        //# ===== 공통 헬퍼 =====

        private static void SaveAsPrefab(GameObject root, string prefabName)
        {
            var prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);
            Debug.Log($"[LairSpawnerStatusUIBuilder] {prefabName} 빌드 완료 (Addressable X)");
        }

        private static void SaveAndRegisterPrefab(GameObject root, string prefabName,
            AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            var prefabPath = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Object.DestroyImmediate(root);

            var guid = AssetDatabase.AssetPathToGUID(prefabPath);
            var entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
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
            var prop = so.FindProperty(fieldName);
            if (prop == null)
            {
                Debug.LogWarning($"[LairSpawnerStatusUIBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{fieldName}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void EnsureDir(string path)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                AssetDatabase.Refresh();
            }
        }
    }
}
