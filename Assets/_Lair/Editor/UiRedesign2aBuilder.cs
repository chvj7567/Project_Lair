using System;
using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# UI 도트 던전 리디자인 2a단계 — 공용 팝업 + 마을 HUD 프리팹을 "열어서 수정"하는 일회용 authoring 툴 (Rule 04 §3, 실행 후 삭제).
    //# 대상: ToastView, ConfirmPopup, HeroSelectCell, HeroSelectPopup, VillageHud. GUID·GameObject 이름·기존 [SerializeField] 참조는 유지한다.
    //# 멱등 — 위젯은 이름으로 찾아 재사용/갱신하므로 재실행해도 중복 생성되지 않는다. 1280x720 기준, 도트 1칸 = 4 ref unit.
    public static class UiRedesign2aBuilder
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/UiDot/";

        private static readonly Color Bone = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private static readonly Color Sub = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color Txt = new Color32(0xEE, 0xF1, 0xF6, 0xFF);
        private static readonly Color LockHint = new Color32(0xFF, 0x9A, 0x9D, 0xFF);
        private static readonly Color Dim = new Color32(4, 6, 10, 184);
        private static readonly Color LockDim = new Color32(5, 7, 11, 199);
        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);

        [MenuItem("Lair/UI/Build 2a")]
        public static void Build()
        {
            Process("ToastView", BuildToast);
            Process("ConfirmPopup", BuildConfirm);
            Process("HeroSelectCell", BuildHeroSelectCell);
            Process("HeroSelectPopup", BuildHeroSelectPopup);
            Process("VillageHud", BuildVillageHud);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiRedesign2aBuilder] 2a 프리팹 5종 수정 완료");
        }

        private static void Process(string prefabName, Action<GameObject> build)
        {
            string path = UiDir + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[UiRedesign2aBuilder] {prefabName} 저장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiRedesign2aBuilder] {prefabName} 실패: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ---------------- ToastView ----------------

        private static void BuildToast(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform panel = Need(root, "Panel");
            SetImage(panel, Spr("Px_PanelDark"), Color.white, false);

            RectTransform dot = Child(panel, "Dot");
            SetRect(dot, Half, Half, Half, new Vector2(28f, 0f), new Vector2(20f, 20f));
            Image dotImage = SetImage(dot, Spr("Px_Dot"), UiDotPalette.Soul, false);

            RectTransform message = Need(root, "Message");
            SetStretch(message, 52f, 8f, 24f, 8f);
            StyleText(message, font, 24f, Txt, TextAlignmentOptions.Left, null);

            SetRef(root.GetComponent<ToastView>(), "_dot", dotImage);
        }

        //# ---------------- ConfirmPopup ----------------

        private static void BuildConfirm(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;

            //# 루트 Image 는 투명 유지(자식 Dim 이 위에 그려져 패널을 덮으므로 패널은 별도 Panel 자식) — 루트 Button 은 그대로.
            Image rootImage = root.GetComponent<Image>();
            if (rootImage != null)
            {
                rootImage.sprite = null;
                rootImage.color = new Color(0f, 0f, 0f, 0f);
            }

            RectTransform dim = Child(rootRt, "Dim");
            SetStretch(dim, -2400f, -1400f, -2400f, -1400f);
            SetImage(dim, null, Dim, true);
            dim.SetSiblingIndex(0);

            RectTransform panel = Child(rootRt, "Panel");
            SetStretch(panel, 0f, 0f, 0f, 0f);
            SetImage(panel, Spr("Px_Panel"), Color.white, true);
            panel.SetSiblingIndex(1);

            RectTransform plaque = Child(rootRt, "TitlePlaque");
            SetRect(plaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            SetImage(plaque, Spr("Px_Plaque"), Color.white, false);
            plaque.SetSiblingIndex(2);

            RectTransform title = Need(root, "Title");
            SetRect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            StyleText(title, font, 26f, Bone, TextAlignmentOptions.Center, null);

            StyleText(Need(root, "Message"), font, 22f, Txt, TextAlignmentOptions.Center, null);

            SkinButton(Need(root, "CancelButton"), Spr("Px_Btn"), font, 22f, Bone);
            SkinButton(Need(root, "ConfirmButton"), Spr("Px_BtnSoul"), font, 22f, Color.white);
        }

        //# ---------------- HeroSelectCell / HeroSelectPopup ----------------

        private static void BuildHeroSelectCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(152f, 192f);
            SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform slot = Child(rootRt, "PortraitSlot");
            SetRect(slot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(120f, 120f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(0);

            RectTransform portrait = Need(root, "Portrait");
            SetRect(portrait, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(112f, 112f));
            Image portraitImage = portrait.GetComponent<Image>();
            portraitImage.preserveAspect = true;
            portraitImage.raycastTarget = false;

            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 34f), new Vector2(0f, 24f));
            StyleText(nameText, font, 20f, Bone, TextAlignmentOptions.Center, null);

            RectTransform subText = Child(rootRt, "SubText");
            SetRect(subText, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(0f, 22f));
            StyleText(subText, font, 16f, Sub, TextAlignmentOptions.Center, "1단계");

            RectTransform border = Need(root, "Border");
            SetStretch(border, 0f, 0f, 0f, 0f);
            Image borderImage = SetImage(border, Spr("Px_Ring"), new Color(0f, 0f, 0f, 0f), false);
            borderImage.fillCenter = false;
            border.SetAsLastSibling();

            HeroSelectCell cell = root.GetComponent<HeroSelectCell>();
            SetRef(cell, "_subText", subText.GetComponent<CHText>());
            SetRef(cell, "_normalSprite", Spr("Px_Panel"));
            SetRef(cell, "_lockedSprite", Spr("Px_PanelDark"));
        }

        private static void BuildHeroSelectPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform dim = Need(root, "Dim");
            SetImage(dim, null, Dim, true);
            dim.SetAsFirstSibling();

            RectTransform body = Need(root, "ModalBody");
            RemoveOutline(body);
            SetImage(body, Spr("Px_Panel"), Color.white, true);
            BuildModalHeader(body, font, "TitlePlaque", "Title", "CloseButton");

            RectTransform scroll = Need(root, "ScrollView");
            SetStretch(scroll, 28f, 28f, 28f, 64f);
        }

        //# 팝업 공통 머리 — 명판(Px_Plaque) 위에 Title, 우상단 핏빛 X(Px_CloseX). 명판은 Title 바로 뒤(아래)에 그려진다.
        private static void BuildModalHeader(RectTransform body, TMP_FontAsset font, string plaqueName, string titleName, string closeName)
        {
            RectTransform plaque = Child(body, plaqueName);
            SetRect(plaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            SetImage(plaque, Spr("Px_Plaque"), Color.white, false);
            plaque.SetSiblingIndex(0);

            RectTransform title = Need(body, titleName);
            SetRect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            StyleText(title, font, 26f, Bone, TextAlignmentOptions.Center, null);

            RectTransform close = Need(body, closeName);
            SetRect(close, Vector2.one, Vector2.one, Vector2.one, new Vector2(13f, 13f), new Vector2(44f, 44f));
            SetImage(close, Spr("Px_CloseX"), Color.white, true);
            SetButtonTint(close);
            RectTransform x = FindDeep(close, "X");
            if (x != null)
            {
                x.gameObject.SetActive(false);
            }
            close.SetAsLastSibling();
        }

        //# ---------------- VillageHud ----------------

        private static void BuildVillageHud(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            Sprite btn = Spr("Px_Btn");

            //# 상단바 — dark 석판 88 ref.
            RectTransform top = Need(root, "TopBar");
            SetRect(top, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 88f));
            SetImage(top, Spr("Px_PanelDark"), Color.white, false);
            top.SetAsFirstSibling();

            RectTransform face = Child(top, "LordFace");
            SetRect(face, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(60f, 60f));
            SetImage(face, Spr("Px_Panel"), Color.white, false);
            RectTransform faceIcon = Child(face, "LordFaceIcon");
            SetStretch(faceIcon, 8f, 8f, 8f, 8f);
            Image faceIconImage = SetImage(faceIcon, Spr("Px_Glyph_Crown"), Color.white, false);
            faceIconImage.preserveAspect = true;

            LeftMid(Need(root, "DisplayNameText"), new Vector2(92f, 24f), new Vector2(220f, 26f));
            StyleText(Need(root, "DisplayNameText"), font, 20f, Txt, TextAlignmentOptions.Left, null);
            LeftMid(Need(root, "LordLevelText"), new Vector2(92f, 2f), new Vector2(220f, 22f));
            StyleText(Need(root, "LordLevelText"), font, 17f, UiDotPalette.Gold, TextAlignmentOptions.Left, null);

            RectTransform xpBg = Need(root, "XpBarBg");
            LeftMid(xpBg, new Vector2(92f, -22f), new Vector2(160f, 16f));
            SetImage(xpBg, Spr("Px_BarBg"), Color.white, false);
            RectTransform xpFill = Need(root, "XpBarFill");
            SetStretch(xpFill, 4f, 4f, 4f, 4f);
            Image xpFillImage = SetImage(xpFill, Spr("Px_BarFill"), UiDotPalette.Gold, false);
            xpFillImage.type = Image.Type.Filled;
            xpFillImage.fillMethod = Image.FillMethod.Horizontal;
            xpFillImage.fillOrigin = 0;

            RectTransform villageName = Need(root, "VillageNameText");
            SetRect(villageName, Half, Half, Half, new Vector2(0f, 8f), new Vector2(320f, 34f));
            StyleText(villageName, font, 30f, Bone, TextAlignmentOptions.Center, null);
            RectTransform villageSub = Child(top, "VillageSubText");
            SetRect(villageSub, Half, Half, Half, new Vector2(0f, -16f), new Vector2(320f, 20f));
            StyleText(villageSub, font, 14f, Sub, TextAlignmentOptions.Center, "LAIR OF THE LORD");

            //# 소울 알약 — SoulText 를 알약 안으로 옮긴다(참조는 오브젝트라 유지).
            RectTransform pill = Child(top, "SoulPill");
            SetRect(pill, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-148f, 0f), new Vector2(232f, 48f));
            SetImage(pill, Spr("Px_PanelSunk"), Color.white, false);
            RectTransform coin = Child(pill, "SoulCoin");
            SetRect(coin, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(20f, 20f));
            SetImage(coin, Spr("Px_SoulCoin"), Color.white, false);
            RectTransform soul = Need(root, "SoulText");
            Reparent(soul, pill);
            SetStretch(soul, 44f, 4f, 12f, 4f);
            StyleText(soul, font, 20f, UiDotPalette.Soul, TextAlignmentOptions.Left, null);

            //# 계정 버튼 — 상단바 우측으로 이동, 빨간 점은 우상단 모서리.
            RectTransform cloud = Need(root, "CloudButton");
            Reparent(cloud, top);
            SetRect(cloud, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-20f, 0f), new Vector2(112f, 52f));
            SkinButton(cloud, btn, font, 20f, Bone);
            HideChild(cloud, "Icon");
            RectTransform redDot = Need(root, "RedDot");
            SetRect(redDot, Vector2.one, Vector2.one, Half, new Vector2(-2f, -2f), new Vector2(24f, 24f));
            SetImage(redDot, Spr("Px_Dot"), UiDotPalette.Blood, false);
            redDot.SetAsLastSibling();

            //# 메뉴 6종 2열 그리드 — 왼쪽, 도트 글리프 위 + 라벨 아래.
            MenuButton(root, font, "LordButton", "Px_Glyph_Castle", 0, 0);
            MenuButton(root, font, "ShopButton", "Px_Glyph_Shop", 1, 0);
            MenuButton(root, font, "CodexButton", "Px_Glyph_Book", 0, 1);
            MenuButton(root, font, "QuestButton", "Px_Glyph_Scroll", 1, 1);
            MenuButton(root, font, "RecordsButton", "Px_Glyph_Tomb", 0, 2);
            MenuButton(root, font, "RankingButton", "Px_Glyph_Crown", 1, 2);

            //# 하단 — 영웅(기본) + 방어하기(blood).
            RectTransform hero = Need(root, "HeroButton");
            SetRect(hero, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-184f, 20f), new Vector2(160f, 68f));
            SkinButton(hero, btn, font, 22f, Bone);
            HideChild(hero, "Icon");
            RectTransform sortie = Need(root, "SortieButton");
            SetRect(sortie, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(90f, 20f), new Vector2(348f, 84f));
            SkinButton(sortie, Spr("Px_BtnBlood"), font, 30f, Color.white);
            HideChild(sortie, "Icon");

            BuildStageCard(root, font, btn);
            BuildRecordPanel(root, font, rootRt);
        }

        private static void MenuButton(GameObject root, TMP_FontAsset font, string name, string glyph, int col, int row)
        {
            RectTransform rt = Need(root, name);
            SetRect(rt, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(20f + col * 128f, -(116f + row * 128f)), new Vector2(112f, 112f));
            SkinButton(rt, Spr("Px_Btn"), font, 18f, Bone);

            RectTransform icon = FindDeep(rt, "Icon");
            SetRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(48f, 48f));
            Image iconImage = SetImage(icon, Spr(glyph), Color.white, false);
            iconImage.preserveAspect = true;
            icon.gameObject.SetActive(true);

            RectTransform label = FindDeep(rt, "Label");
            SetRect(label, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(0f, 28f));
        }

        private static void BuildStageCard(GameObject root, TMP_FontAsset font, Sprite btn)
        {
            RectTransform carousel = Need(root, "StageCarousel");

            RectTransform card = Child(carousel, "StageCard");
            SetRect(card, Half, Half, Half, new Vector2(0f, 4f), new Vector2(508f, 484f));
            SetImage(card, Spr("Px_Panel"), Color.white, false);
            card.SetSiblingIndex(0);

            RectTransform intruder = Child(carousel, "StageIntruderLabel");
            SetRect(intruder, Half, Half, Half, new Vector2(0f, 214f), new Vector2(300f, 24f));
            StyleText(intruder, font, 18f, Sub, TextAlignmentOptions.Center, "침입자");

            RectTransform indicator = Need(root, "StageIndicatorText");
            SetRect(indicator, Half, Half, Half, new Vector2(0f, 180f), new Vector2(400f, 44f));
            StyleText(indicator, font, 32f, Bone, TextAlignmentOptions.Center, null);

            RectTransform threat = Need(root, "StageThreatText");
            SetRect(threat, Half, Half, Half, new Vector2(0f, 140f), new Vector2(300f, 34f));
            StyleText(threat, font, 26f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);

            RectTransform portrait = Child(carousel, "StageHeroPortrait");
            SetRect(portrait, Half, Half, Half, new Vector2(0f, -40f), new Vector2(256f, 256f));
            Image portraitImage = SetImage(portrait, null, Color.white, false);
            portraitImage.preserveAspect = true;
            portraitImage.enabled = false;   //# 스프라이트가 정해지기 전엔 흰 사각형이 보이지 않게 — VillageHud 가 스테이지별로 켠다.

            RectTransform dots = Child(carousel, "StageDots");
            SetRect(dots, Half, Half, Half, new Vector2(0f, -208f), new Vector2(140f, 20f));
            Image[] dotImages = new Image[5];
            for (int i = 0; i < dotImages.Length; ++i)
            {
                RectTransform dot = Child(dots, "StageDot" + (i + 1));
                SetRect(dot, Half, Half, Half, new Vector2((i - 2) * 28f, 0f), new Vector2(16f, 16f));
                dotImages[i] = SetImage(dot, Spr("Px_Dot"), UiDotPalette.Stone4, false);
            }

            ArrowButton(Need(root, "StagePrevButton"), btn, new Vector2(-304f, 4f), -1f);
            ArrowButton(Need(root, "StageNextButton"), btn, new Vector2(304f, 4f), 1f);

            //# 잠금 오버레이 — 카드 안쪽을 덮는다(기본 꺼짐 유지).
            RectTransform overlay = Need(root, "StageLockOverlay");
            SetRect(overlay, Half, Half, Half, new Vector2(0f, 4f), new Vector2(488f, 464f));
            SetImage(Need(root, "StageLockDim"), null, LockDim, true);
            SetStretch(Need(root, "StageLockDim"), 0f, 0f, 0f, 0f);
            RectTransform lockIcon = Child(overlay, "LockIcon");
            SetRect(lockIcon, Half, Half, Half, new Vector2(0f, 56f), new Vector2(32f, 36f));
            SetImage(lockIcon, Spr("Px_Lock"), Color.white, false);
            RectTransform lockLabel = Need(root, "StageLockLabel");
            SetRect(lockLabel, Half, Half, Half, new Vector2(0f, 8f), new Vector2(300f, 36f));
            StyleText(lockLabel, font, 26f, Txt, TextAlignmentOptions.Center, null);
            RectTransform lockHint = Need(root, "StageLockHintText");
            SetRect(lockHint, Half, Half, Half, new Vector2(0f, -32f), new Vector2(420f, 28f));
            StyleText(lockHint, font, 18f, LockHint, TextAlignmentOptions.Center, null);
            overlay.SetAsLastSibling();

            VillageHud hud = root.GetComponent<VillageHud>();
            SetRef(hud, "_stageHeroPortrait", portraitImage);
            SetRefArray(hud, "_stageDots", dotImages);
        }

        private static void ArrowButton(RectTransform rt, Sprite btn, Vector2 pos, float scaleX)
        {
            SetRect(rt, Half, Half, Half, pos, new Vector2(60f, 84f));
            SetImage(rt, btn, Color.white, true);
            SetButtonTint(rt);
            HideChild(rt, "Label");
            RectTransform arrow = Child(rt, "Arrow");
            SetRect(arrow, Half, Half, Half, Vector2.zero, new Vector2(20f, 28f));
            SetImage(arrow, Spr("Px_ArrowR"), Color.white, false);
            arrow.localScale = new Vector3(scaleX, 1f, 1f);
        }

        private static void BuildRecordPanel(GameObject root, TMP_FontAsset font, RectTransform rootRt)
        {
            RectTransform panel = Child(rootRt, "RecordPanel");
            SetRect(panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(-20f, -116f), new Vector2(256f, 216f));
            SetImage(panel, Spr("Px_PanelDark"), Color.white, false);

            string[] labelNames = { "RecordIntruderLabel", "RecordBestLabel", "RecordWinsLabel" };
            string[] labels = { "이번 침입자", "최단 방어", "이 스테이지 전적" };
            string[] valueNames = { "RecordIntruderText", "RecordBestText", "RecordWinsText" };
            Color[] valueColors = { Txt, UiDotPalette.Soul, Txt };
            CHText[] valueTexts = new CHText[3];
            for (int i = 0; i < 3; ++i)
            {
                RectTransform label = Child(panel, labelNames[i]);
                SetRect(label, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(16f, -14f - i * 64f), new Vector2(-32f, 22f));
                StyleText(label, font, 16f, Sub, TextAlignmentOptions.Left, labels[i]);

                RectTransform value = Child(panel, valueNames[i]);
                SetRect(value, new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f), new Vector2(16f, -36f - i * 64f), new Vector2(-32f, 28f));
                StyleText(value, font, 22f, valueColors[i], TextAlignmentOptions.Left, null);
                valueTexts[i] = value.GetComponent<CHText>();
            }

            VillageHud hud = root.GetComponent<VillageHud>();
            SetRef(hud, "_recordIntruderText", valueTexts[0]);
            SetRef(hud, "_recordBestText", valueTexts[1]);
            SetRef(hud, "_recordWinsText", valueTexts[2]);
        }

        //# ---------------- 공용 헬퍼 ----------------

        private static Sprite Spr(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            if (sprite == null)
            {
                Debug.LogError($"[UiRedesign2aBuilder] 스프라이트 없음: {SpriteDir}{name}.png");
            }

            return sprite;
        }

        //# 이름으로 찾은 직속/하위 오브젝트 — 없으면 프리팹 구조가 바뀐 것이므로 예외.
        private static RectTransform Need(GameObject root, string name)
        {
            RectTransform found = FindDeep((RectTransform)root.transform, name);
            if (found == null)
            {
                throw new InvalidOperationException($"기존 오브젝트 없음: {name}");
            }

            return found;
        }

        private static RectTransform Need(RectTransform parent, string name)
        {
            RectTransform found = FindDeep(parent, name);
            if (found == null)
            {
                throw new InvalidOperationException($"기존 오브젝트 없음: {name}");
            }

            return found;
        }

        private static RectTransform FindDeep(RectTransform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return (RectTransform)child;
                RectTransform deeper = FindDeep((RectTransform)child, name);
                if (deeper != null)
                    return deeper;
            }

            return null;
        }

        //# 직속 자식을 이름으로 찾고 없으면 만든다(멱등). 프리팹 안에 정적으로 남는 GameObject.
        private static RectTransform Child(RectTransform parent, string name)
        {
            Transform existing = parent.Find(name);
            if (existing != null)
                return (RectTransform)existing;
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        private static void Reparent(RectTransform rt, RectTransform parent)
        {
            if (rt.parent != parent)
            {
                rt.SetParent(parent, false);
            }
        }

        private static void HideChild(RectTransform parent, string name)
        {
            RectTransform child = FindDeep(parent, name);
            if (child != null)
            {
                child.gameObject.SetActive(false);
            }
        }

        private static void RemoveOutline(RectTransform rt)
        {
            Outline outline = rt.GetComponent<Outline>();
            if (outline != null)
            {
                UnityEngine.Object.DestroyImmediate(outline, true);
            }
        }

        private static void SetRect(RectTransform rt, Vector2 aMin, Vector2 aMax, Vector2 pivot, Vector2 pos, Vector2 size)
        {
            rt.anchorMin = aMin;
            rt.anchorMax = aMax;
            rt.pivot = pivot;
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;
        }

        private static void LeftMid(RectTransform rt, Vector2 pos, Vector2 size)
        {
            SetRect(rt, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), pos, size);
        }

        //# 부모를 꽉 채우고 좌/하/우/상 여백(ref unit)을 준다.
        private static void SetStretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Half;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        //# Image 가져오기/추가 + 스프라이트·색 지정. 9-slice border 가 있으면 Sliced, 없으면 Simple.
        private static Image SetImage(RectTransform rt, Sprite sprite, Color color, bool raycast)
        {
            Image image = rt.GetComponent<Image>();
            if (image == null)
            {
                image = rt.gameObject.AddComponent<Image>();
            }

            image.sprite = sprite;
            image.color = color;
            image.raycastTarget = raycast;
            image.preserveAspect = false;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.enabled = true;
            return image;
        }

        //# TMP_Text + CHText 를 함께 보장(Rule 03 §3 — 정적 라벨 포함)하고 스타일을 입힌다. 새 텍스트는 font 를 물려받는다.
        private static void StyleText(RectTransform rt, TMP_FontAsset font, float size, Color color, TextAlignmentOptions align, string text)
        {
            TextMeshProUGUI tmp = rt.GetComponent<TextMeshProUGUI>();
            if (tmp == null)
            {
                tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
                if (font != null)
                {
                    tmp.font = font;
                }
            }

            if (rt.GetComponent<CHText>() == null)
            {
                rt.gameObject.AddComponent<CHText>();
            }

            tmp.enableAutoSizing = false;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            if (text != null)
            {
                tmp.text = text;
            }
        }

        private static void SetButtonTint(RectTransform rt)
        {
            Button button = rt.GetComponent<Button>();
            if (button == null)
                return;
            button.transition = Selectable.Transition.ColorTint;
            button.targetGraphic = rt.GetComponent<Image>();
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.9f, 0.9f, 0.9f, 1f);
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.75f, 0.75f, 0.75f, 1f);
            colors.selectedColor = Color.white;
            colors.disabledColor = new Color(0.55f, 0.55f, 0.55f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        //# 버튼 = 도트 버튼 스프라이트 + Color Tint 전환 + Label 스타일. Label 은 CHButton 이 SetText 로 갱신하는 대상이라 유지.
        private static void SkinButton(RectTransform rt, Sprite sprite, TMP_FontAsset font, float labelSize, Color labelColor)
        {
            SetImage(rt, sprite, Color.white, true);
            SetButtonTint(rt);
            RectTransform label = FindDeep(rt, "Label");
            if (label == null)
                return;
            SetStretch(label, 0f, 0f, 0f, 0f);
            StyleText(label, font, labelSize, labelColor, TextAlignmentOptions.Center, null);
        }

        private static TMP_FontAsset FindFont(GameObject root)
        {
            TextMeshProUGUI any = root.GetComponentInChildren<TextMeshProUGUI>(true);
            return any != null ? any.font : null;
        }

        //# 직렬화 참조 연결 — 필드명이 없으면(스크립트 변경 누락) 에러 로그로 드러낸다.
        private static void SetRef(Component owner, string field, UnityEngine.Object value)
        {
            if (owner == null)
            {
                Debug.LogError($"[UiRedesign2aBuilder] 소유 컴포넌트 없음: {field}");
                return;
            }

            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign2aBuilder] 필드 없음: {owner.GetType().Name}.{field}");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Component owner, string field, UnityEngine.Object[] values)
        {
            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign2aBuilder] 필드 없음: {owner.GetType().Name}.{field}");
                return;
            }

            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
            {
                property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
