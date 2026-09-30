using System;
using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# UI 도트 던전 리디자인 2b단계 — 마을 팝업 7종 + 셀을 "열어서 수정"하는 일회용 authoring 툴 (Rule 04 §3, 실행 후 삭제).
    //# 대상: ShopPopup/ShopItemCell, LordLevelPopup/LordRewardCell, QuestPopup/QuestCell, CodexPopup/CodexCell,
    //#       RecordsPopup/RecordsStageCell, RankingPopup/RankingCell, CloudPopup. GUID·GameObject 이름·기존 [SerializeField] 참조 유지.
    //# 멱등 — 위젯은 이름으로 찾아 재사용/갱신한다. ScrollView 의 origin 셀(중첩 프리팹 인스턴스)은 컴포넌트 제거 없이 건드리지 않는다.
    public static class UiRedesign2bBuilder
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/UiDot/";

        private static readonly Color Bone = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private static readonly Color Sub = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color Txt = new Color32(0xEE, 0xF1, 0xF6, 0xFF);
        private static readonly Color Ink = new Color32(0x07, 0x09, 0x0E, 0xFF);
        private static readonly Color Warn = new Color32(0xFF, 0x9A, 0x9D, 0xFF);
        private static readonly Color Dim = new Color32(4, 6, 10, 184);
        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MidRight = new Vector2(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);

        [MenuItem("Lair/UI/Build 2b")]
        public static void Build()
        {
            Process("ShopItemCell", BuildShopItemCell);
            Process("ShopPopup", BuildShopPopup);
            Process("LordRewardCell", BuildLordRewardCell);
            Process("LordLevelPopup", BuildLordLevelPopup);
            Process("QuestCell", BuildQuestCell);
            Process("QuestPopup", BuildQuestPopup);
            Process("CodexCell", BuildCodexCell);
            Process("CodexPopup", BuildCodexPopup);
            Process("RecordsStageCell", BuildRecordsStageCell);
            Process("RecordsPopup", BuildRecordsPopup);
            Process("RankingCell", BuildRankingCell);
            Process("RankingPopup", BuildRankingPopup);
            Process("CloudPopup", BuildCloudPopup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiRedesign2bBuilder] 2b 프리팹 13종 수정 완료");
        }

        private static void Process(string prefabName, Action<GameObject> build)
        {
            string path = UiDir + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[UiRedesign2bBuilder] {prefabName} 저장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiRedesign2bBuilder] {prefabName} 실패: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ---------------- Shop ----------------

        private static void BuildShopItemCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(664f, 88f);
            SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform headerBg = Need(root, "HeaderBg");
            //# 헤더 행은 셀 전체를 dark 석판으로 덮는다 — 일부만 덮으면 위쪽에 밝은 줄이 남는다.
            SetStretch(headerBg, 0f, 0f, 0f, 0f);
            SetImage(headerBg, Spr("Px_PanelDark"), Color.white, false);
            headerBg.SetSiblingIndex(0);
            RectTransform divider = Need(root, "HeaderDivider");
            SetRect(divider, BottomLeft, BottomRight, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 8f));
            SetImage(divider, Spr("Px_Divider"), Color.white, false);
            RectTransform accent = Need(root, "HeaderAccent");
            SetRect(accent, MidLeft, MidLeft, MidLeft, new Vector2(16f, 0f), new Vector2(8f, 28f));
            SetImage(accent, Spr("Px_Solid"), UiDotPalette.Soul, false);
            RectTransform headerText = Need(root, "HeaderText");
            SetRect(headerText, MidLeft, MidLeft, MidLeft, new Vector2(36f, 0f), new Vector2(400f, 30f));
            StyleText(headerText, font, 22f, Bone, TextAlignmentOptions.Left, null);

            RectTransform ring = Child(rootRt, "StateRing");
            SetStretch(ring, 0f, 0f, 0f, 0f);
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Soul, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(1);

            RectTransform slot = Child(rootRt, "IconSlot");
            SetRect(slot, MidLeft, MidLeft, MidLeft, new Vector2(12f, 0f), new Vector2(68f, 68f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(2);

            RectTransform icon = Need(root, "IconImage");
            SetRect(icon, MidLeft, MidLeft, Half, new Vector2(46f, 0f), new Vector2(52f, 52f));
            icon.GetComponent<Image>().preserveAspect = true;
            RectTransform glowFrame = Need(root, "GlowFrame");
            SetRect(glowFrame, MidLeft, MidLeft, Half, new Vector2(46f, 0f), new Vector2(76f, 76f));
            SetSpriteKeepColor(glowFrame, Spr("Px_Ring"), false);
            RectTransform glowHint = Need(root, "GlowHintRing");
            SetRect(glowHint, MidLeft, MidLeft, Half, new Vector2(46f, 0f), new Vector2(84f, 84f));
            SetSpriteKeepColor(glowHint, Spr("Px_Ring"), false);

            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, TopLeft, TopLeft, TopLeft, new Vector2(96f, -10f), new Vector2(300f, 26f));
            StyleText(nameText, font, 20f, Bone, TextAlignmentOptions.Left, null);
            RectTransform descText = Need(root, "DescText");
            SetRect(descText, BottomLeft, BottomLeft, BottomLeft, new Vector2(96f, 10f), new Vector2(360f, 22f));
            StyleText(descText, font, 16f, Sub, TextAlignmentOptions.Left, null);
            RectTransform levelText = Need(root, "LevelText");
            SetRect(levelText, TopLeft, TopLeft, TopLeft, new Vector2(400f, -12f), new Vector2(90f, 22f));
            StyleText(levelText, font, 16f, Sub, TextAlignmentOptions.Left, null);

            //# 레벨 5칸 눈금 — 정적 5칸(전 아이템 MaxLevel 5), 채움은 셀 코드가 금색/stone4 로 칠한다.
            RectTransform pips = Child(rootRt, "LevelPips");
            SetRect(pips, TopLeft, TopLeft, TopLeft, new Vector2(96f, -40f), new Vector2(130f, 12f));
            Image[] pipImages = new Image[5];
            for (int i = 0; i < pipImages.Length; ++i)
            {
                RectTransform pip = Child(pips, "LevelPip" + i);
                SetRect(pip, MidLeft, MidLeft, MidLeft, new Vector2(i * 26f, 0f), new Vector2(20f, 12f));
                pipImages[i] = SetImage(pip, Spr("Px_Solid"), UiDotPalette.Stone4, false);
            }

            RectTransform price = Need(root, "PriceText");
            SetRect(price, MidRight, MidRight, MidRight, new Vector2(-156f, 0f), new Vector2(150f, 26f));
            StyleText(price, font, 18f, UiDotPalette.Soul, TextAlignmentOptions.Right, null);

            RectTransform buy = Need(root, "BuyButton");
            SetRect(buy, MidRight, MidRight, MidRight, new Vector2(-16f, 0f), new Vector2(128f, 52f));
            Image buyImage = SetImage(buy, Spr("Px_BtnSoul"), Color.white, true);
            SetButtonTint(buy, true);
            RectTransform buyLabel = Need(buy, "Label");
            SetStretch(buyLabel, 0f, 0f, 0f, 0f);
            StyleText(buyLabel, font, 20f, Color.white, TextAlignmentOptions.Center, null);

            ShopItemCell cell = root.GetComponent<ShopItemCell>();
            SetRefArray(cell, "_levelPips", pipImages);
            SetRef(cell, "_stateRing", ringImage);
            SetRef(cell, "_iconSlot", slot.gameObject);
            SetRefArray(cell, "_textBlock", new UnityEngine.Object[] { nameText, descText, pips });
            SetRef(cell, "_buyButtonImage", buyImage);
            SetRef(cell, "_buySoulSprite", Spr("Px_BtnSoul"));
            SetRef(cell, "_buyOffSprite", Spr("Px_BtnOff"));
            SetRef(cell, "_buyGoldSprite", Spr("Px_BtnGold"));
        }

        private static void BuildShopPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform summary = Need(root, "BonusSummaryText");
            RectTransform summaryBg = Child(body, "BonusSummaryBg");
            SetRect(summaryBg, TopLeft, TopRight, TopLeft, new Vector2(28f, -60f), new Vector2(-300f, 44f));
            SetImage(summaryBg, Spr("Px_PanelSunk"), Color.white, false);
            summaryBg.SetSiblingIndex(summary.GetSiblingIndex());
            SetRect(summary, TopLeft, TopRight, TopLeft, new Vector2(40f, -60f), new Vector2(-324f, 44f));
            StyleText(summary, font, 16f, Sub, TextAlignmentOptions.Left, null);

            RectTransform pill = Child(body, "SoulPill");
            SetRect(pill, TopRight, TopRight, TopRight, new Vector2(-28f, -60f), new Vector2(232f, 44f));
            SetImage(pill, Spr("Px_PanelDark"), Color.white, false);
            RectTransform coin = Child(pill, "SoulCoin");
            SetRect(coin, MidLeft, MidLeft, MidLeft, new Vector2(14f, 0f), new Vector2(20f, 20f));
            SetImage(coin, Spr("Px_SoulCoin"), Color.white, false);
            RectTransform soul = Need(root, "SoulText");
            Reparent(soul, pill);
            SetStretch(soul, 42f, 4f, 12f, 4f);
            StyleText(soul, font, 20f, UiDotPalette.Soul, TextAlignmentOptions.Left, null);

            SetStretch(Need(root, "ScrollView"), 28f, 28f, 28f, 116f);
        }

        //# ---------------- LordLevel ----------------

        private static void BuildLordRewardCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(664f, 72f);
            Image background = SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform ring = Child(rootRt, "StateRing");
            SetStretch(ring, 0f, 0f, 0f, 0f);
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Gold, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(0);

            RectTransform level = Need(root, "LevelText");
            SetRect(level, MidLeft, MidLeft, MidLeft, new Vector2(20f, 0f), new Vector2(80f, 28f));
            StyleText(level, font, 22f, UiDotPalette.Gold, TextAlignmentOptions.Left, null);
            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, MidLeft, MidLeft, MidLeft, new Vector2(108f, 15f), new Vector2(300f, 26f));
            StyleText(nameText, font, 20f, Txt, TextAlignmentOptions.Left, null);
            RectTransform subText = Child(rootRt, "SubText");
            SetRect(subText, MidLeft, MidLeft, MidLeft, new Vector2(108f, -17f), new Vector2(300f, 20f));
            StyleText(subText, font, 15f, Sub, TextAlignmentOptions.Left, "수령 완료");
            RectTransform reward = Need(root, "RewardText");
            SetRect(reward, MidRight, MidRight, MidRight, new Vector2(-118f, 0f), new Vector2(190f, 26f));
            StyleText(reward, font, 18f, UiDotPalette.Soul, TextAlignmentOptions.Right, null);

            RectTransform badgeBg = Child(rootRt, "BadgeBg");
            SetRect(badgeBg, MidRight, MidRight, MidRight, new Vector2(-20f, 0f), new Vector2(76f, 36f));
            Image badgeImage = SetImage(badgeBg, Spr("Px_Badge_Soul"), Color.white, false);
            RectTransform badge = Need(root, "ReachedBadge");
            SetRect(badge, MidRight, MidRight, MidRight, new Vector2(-20f, 0f), new Vector2(76f, 36f));
            StyleText(badge, font, 16f, UiDotPalette.Soul, TextAlignmentOptions.Center, null);
            badgeBg.SetSiblingIndex(badge.GetSiblingIndex());

            LordRewardCell cell = root.GetComponent<LordRewardCell>();
            SetRef(cell, "_background", background);
            SetRef(cell, "_subText", subText.GetComponent<CHText>());
            SetRef(cell, "_stateRing", ringImage);
            SetRef(cell, "_normalSprite", Spr("Px_Panel"));
            SetRef(cell, "_pendingSprite", Spr("Px_PanelDark"));
            SetRef(cell, "_badgeBg", badgeImage);
            SetRef(cell, "_badgeSoulSprite", Spr("Px_Badge_Soul"));
            SetRef(cell, "_badgeGoldSprite", Spr("Px_Badge_Gold"));
        }

        private static void BuildLordLevelPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform level = Need(root, "LordLevelText");
            SetRect(level, TopLeft, TopLeft, TopLeft, new Vector2(28f, -62f), new Vector2(220f, 40f));
            StyleText(level, font, 30f, UiDotPalette.Gold, TextAlignmentOptions.Left, null);

            RectTransform bar = Child(body, "LordXpBar");
            SetRect(bar, TopLeft, TopRight, TopLeft, new Vector2(260f, -66f), new Vector2(-288f, 24f));
            SetImage(bar, Spr("Px_BarBg"), Color.white, false);
            RectTransform fill = Child(bar, "LordXpFill");
            SetStretch(fill, 4f, 4f, 4f, 4f);
            Image fillImage = SetImage(fill, Spr("Px_BarFill"), UiDotPalette.Gold, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            for (int i = 1; i <= 9; ++i)
            {
                RectTransform seg = Child(bar, "XpSeg" + i);
                SetRect(seg, new Vector2(i / 10f, 0f), new Vector2(i / 10f, 1f), Half, Vector2.zero, new Vector2(4f, -8f));
                SetImage(seg, Spr("Px_Solid"), new Color(0f, 0f, 0f, 0.6f), false);
            }

            RectTransform next = Child(body, "LordXpNextText");
            SetRect(next, TopLeft, TopRight, TopLeft, new Vector2(260f, -96f), new Vector2(-288f, 22f));
            StyleText(next, font, 15f, Sub, TextAlignmentOptions.Right, "다음 레벨까지 0 XP");

            SetStretch(Need(root, "ScrollView"), 28f, 28f, 28f, 128f);

            LordLevelPopup popup = root.GetComponent<LordLevelPopup>();
            SetRef(popup, "_xpFill", fillImage);
            SetRef(popup, "_xpNextText", next.GetComponent<CHText>());
        }

        //# ---------------- Quest ----------------

        private static void BuildQuestCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(664f, 104f);
            Image background = SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, TopLeft, TopLeft, TopLeft, new Vector2(20f, -12f), new Vector2(380f, 28f));
            StyleText(nameText, font, 22f, Bone, TextAlignmentOptions.Left, null);

            RectTransform badgeBg = Child(rootRt, "BadgeBg");
            SetRect(badgeBg, TopRight, TopRight, TopRight, new Vector2(-20f, -10f), new Vector2(76f, 36f));
            SetImage(badgeBg, Spr("Px_Badge_Soul"), Color.white, false);
            RectTransform badge = Need(root, "AchievedBadge");
            SetRect(badge, TopRight, TopRight, TopRight, new Vector2(-20f, -10f), new Vector2(76f, 36f));
            StyleText(badge, font, 16f, UiDotPalette.Soul, TextAlignmentOptions.Center, null);
            badgeBg.SetSiblingIndex(badge.GetSiblingIndex());

            RectTransform desc = Need(root, "DescText");
            SetRect(desc, TopLeft, TopLeft, TopLeft, new Vector2(20f, -48f), new Vector2(520f, 22f));
            StyleText(desc, font, 16f, Sub, TextAlignmentOptions.Left, null);

            RectTransform progress = Need(root, "ProgressRoot");
            SetRect(progress, BottomLeft, BottomLeft, BottomLeft, new Vector2(20f, 10f), new Vector2(220f, 20f));
            RectTransform track = Need(root, "Track");
            SetStretch(track, 0f, 0f, 0f, 0f);
            SetImage(track, Spr("Px_BarBg"), Color.white, false);
            track.SetSiblingIndex(0);
            RectTransform fill = Need(root, "Fill");
            SetStretch(fill, 4f, 4f, 4f, 4f);
            Image fillImage = SetImage(fill, Spr("Px_BarFill"), UiDotPalette.Soul, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fill.SetSiblingIndex(1);
            RectTransform progressText = Need(root, "ProgressText");
            SetRect(progressText, MidRight, MidRight, MidLeft, new Vector2(12f, 0f), new Vector2(90f, 24f));
            StyleText(progressText, font, 16f, Txt, TextAlignmentOptions.Left, null);

            RectTransform reward = Need(root, "RewardText");
            SetRect(reward, BottomRight, BottomRight, BottomRight, new Vector2(-20f, 10f), new Vector2(160f, 26f));
            StyleText(reward, font, 18f, UiDotPalette.Soul, TextAlignmentOptions.Right, null);

            QuestCell cell = root.GetComponent<QuestCell>();
            SetRef(cell, "_background", background);
            SetRef(cell, "_progressFill", fillImage);
            SetRef(cell, "_normalSprite", Spr("Px_Panel"));
            SetRef(cell, "_badgeBg", badgeBg.gameObject);
        }

        private static void BuildQuestPopup(GameObject root)
        {
            RectTransform body = SkinModal(root, FindFont(root));
            SetStretch(Need(body, "ScrollView"), 28f, 28f, 28f, 72f);
        }

        //# ---------------- Codex ----------------

        private static void BuildCodexCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(108f, 136f);
            Image background = SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform slot = Child(rootRt, "IconSlot");
            SetRect(slot, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(92f, 92f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(0);

            RectTransform glow = Need(root, "GlowOverlay");
            SetRect(glow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(100f, 100f));
            SetSpriteKeepColor(glow, Spr("Px_Ring"), false);
            glow.SetSiblingIndex(1);

            RectTransform icon = Need(root, "Icon");
            SetRect(icon, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(84f, 84f));

            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, BottomLeft, BottomRight, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-8f, 26f));
            StyleText(nameText, font, 17f, Bone, TextAlignmentOptions.Center, null);

            RectTransform levelBadge = Need(root, "LevelBadge");
            SetRect(levelBadge, TopRight, TopRight, TopRight, new Vector2(-6f, -6f), new Vector2(48f, 24f));
            StyleText(levelBadge, font, 16f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);
            levelBadge.SetAsLastSibling();

            CodexCell cell = root.GetComponent<CodexCell>();
            SetRef(cell, "_background", background);
            SetRef(cell, "_normalSprite", Spr("Px_Panel"));
            SetRef(cell, "_dummySprite", Spr("Px_PanelDark"));
        }

        private static void BuildCodexPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform row = Need(root, "TabRow");
            SetRect(row, TopLeft, TopLeft, TopLeft, new Vector2(28f, -64f), new Vector2(260f, 40f));
            SkinTab(Need(root, "MonsterTab"), font, 120f);
            SkinTab(Need(root, "CardTab"), font, 120f);

            RectTransform collected = Child(body, "CollectedText");
            SetRect(collected, TopRight, TopRight, TopRight, new Vector2(-28f, -72f), new Vector2(220f, 24f));
            StyleText(collected, font, 16f, Sub, TextAlignmentOptions.Right, "수집 0 / 0");

            SetStretch(Need(root, "MonsterScrollView"), 28f, 28f, 28f, 116f);
            SetStretch(Need(root, "CardScrollView"), 28f, 28f, 28f, 116f);

            SetRef(root.GetComponent<CodexPopup>(), "_collectedText", collected.GetComponent<CHText>());
        }

        //# ---------------- Records ----------------

        private static void BuildRecordsStageCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(440f, 84f);
            Image background = SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            RectTransform ring = Child(rootRt, "SelectedRing");
            SetStretch(ring, 0f, 0f, 0f, 0f);
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Gold, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(0);

            RectTransform slot = Child(rootRt, "PortraitSlot");
            SetRect(slot, MidLeft, MidLeft, MidLeft, new Vector2(10f, 0f), new Vector2(68f, 68f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(1);

            RectTransform portrait = Need(root, "Portrait");
            SetRect(portrait, MidLeft, MidLeft, MidLeft, new Vector2(12f, 0f), new Vector2(64f, 64f));
            portrait.GetComponent<Image>().preserveAspect = true;

            TopText(Need(root, "StageText"), font, TopLeft, new Vector2(92f, -8f), new Vector2(130f, 26f), 20f, Bone, TextAlignmentOptions.Left);
            TopText(Need(root, "ThreatText"), font, TopLeft, new Vector2(92f, -40f), new Vector2(140f, 24f), 16f, UiDotPalette.Gold, TextAlignmentOptions.Left);
            TopText(Need(root, "BestText"), font, TopRight, new Vector2(-12f, -8f), new Vector2(180f, 26f), 18f, UiDotPalette.Soul, TextAlignmentOptions.Right);
            TopText(Need(root, "WinText"), font, TopRight, new Vector2(-12f, -40f), new Vector2(90f, 24f), 16f, UiDotPalette.Gold, TextAlignmentOptions.Right);
            TopText(Need(root, "RunRateText"), font, TopRight, new Vector2(-104f, -40f), new Vector2(150f, 24f), 15f, Sub, TextAlignmentOptions.Right);
            RectTransform hint = Need(root, "LockHintText");
            SetRect(hint, MidRight, MidRight, MidRight, new Vector2(-12f, 0f), new Vector2(300f, 28f));
            StyleText(hint, font, 16f, Warn, TextAlignmentOptions.Right, null);

            RectTransform badge = Need(root, "SelectedBadge");
            SetRect(badge, TopLeft, TopLeft, TopLeft, new Vector2(226f, -10f), new Vector2(76f, 30f));
            SetImage(badge, Spr("Px_Badge_Gold"), Color.white, false);
            RectTransform badgeLabel = Need(badge, "Label");
            SetStretch(badgeLabel, 0f, 0f, 0f, 0f);
            StyleText(badgeLabel, font, 15f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);

            RecordsStageCell cell = root.GetComponent<RecordsStageCell>();
            SetRef(cell, "_background", background);
            SetRef(cell, "_normalSprite", Spr("Px_Panel"));
            SetRef(cell, "_lockedSprite", Spr("Px_PanelDark"));
            SetRef(cell, "_selectedRing", ringImage);
        }

        private static void BuildRecordsPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);
            //# 본체 높이 540 — 20:9 기기(캔버스 세로 ≈ 644 ref)에서도 명판 돌출(+26)까지 화면 안에 들어온다(620 은 잘림).
            body.sizeDelta = new Vector2(480f, 540f);

            //# 통계 한 덩어리(BodyText)를 숫자 타일 5칸으로 교체 — 스크립트의 _bodyText 필드는 제거됐다.
            RectTransform bodyText = FindDeep((RectTransform)root.transform, "BodyText");
            if (bodyText != null)
            {
                UnityEngine.Object.DestroyImmediate(bodyText.gameObject, true);
            }

            RecordsPopup popup = root.GetComponent<RecordsPopup>();
            string[] names = { "StatTileRuns", "StatTileWins", "StatTileRate", "StatTileBest" };
            string[] labels = { "총 출격", "승리", "승률", "최단" };
            string[] fields = { "_statRunsText", "_statWinsText", "_statRateText", "_statBestText" };
            Color[] colors = { Txt, UiDotPalette.Gold, Txt, UiDotPalette.Soul };
            for (int i = 0; i < names.Length; ++i)
            {
                RectTransform tile = Child(body, names[i]);
                SetRect(tile, TopLeft, TopLeft, TopLeft, new Vector2(28f + i * 108f, -64f), new Vector2(98f, 64f));
                CHText value = StatTile(tile, font, labels[i], colors[i], 26f);
                SetRef(popup, fields[i], value);
            }

            RectTransform topTile = Child(body, "StatTileTopCard");
            SetRect(topTile, TopLeft, TopLeft, TopLeft, new Vector2(28f, -136f), new Vector2(424f, 56f));
            CHText topValue = StatTile(topTile, font, "가장 많이 픽한 카드", Bone, 20f);
            SetRef(popup, "_statTopCardText", topValue);

            SetStretch(Need(root, "StageScrollView"), 28f, 28f, 28f, 208f);
        }

        //# 통계 타일 — sunk 패널 + 위 라벨 + 아래 값. 값 CHText 를 돌려준다.
        private static CHText StatTile(RectTransform tile, TMP_FontAsset font, string label, Color valueColor, float valueSize)
        {
            SetImage(tile, Spr("Px_PanelSunk"), Color.white, false);
            RectTransform labelRt = Child(tile, "Label");
            SetRect(labelRt, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-8f, 20f));
            StyleText(labelRt, font, 14f, Sub, TextAlignmentOptions.Center, label);
            RectTransform valueRt = Child(tile, "Value");
            SetRect(valueRt, BottomLeft, BottomRight, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-8f, 30f));
            StyleText(valueRt, font, valueSize, valueColor, TextAlignmentOptions.Center, "-");
            return valueRt.GetComponent<CHText>();
        }

        //# ---------------- Ranking ----------------

        private static void BuildRankingCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(648f, 52f);
            Image background = SetImage(rootRt, Spr("Px_PanelDark"), Color.white, true);

            RectTransform ring = Child(rootRt, "MineRing");
            SetStretch(ring, 0f, 0f, 0f, 0f);
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Soul, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(0);

            RankColumn(Need(root, "RankText"), font, 0, 20f, Txt);
            RankColumn(Need(root, "NameText"), font, 1, 18f, Txt);
            RankColumn(Need(root, "TimeText"), font, 2, 18f, UiDotPalette.Soul);
            RankColumn(Need(root, "HeroText"), font, 3, 16f, Sub);

            RankingCell cell = root.GetComponent<RankingCell>();
            SetRef(cell, "_background", background);
            SetRef(cell, "_normalSprite", Spr("Px_PanelDark"));
            SetRef(cell, "_mineSprite", Spr("Px_Panel"));
            SetRef(cell, "_mineRing", ringImage);
        }

        private static void BuildRankingPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            //# 스테이지 탭 5개 + 최단 클리어(전체) 탭 — CHToggle + ToggleGroup, 선택 표시는 Checkmark(Px_TabOn) 그래픽.
            RectTransform row = Child(body, "StageTabRow");
            SetRect(row, TopLeft, TopLeft, TopLeft, new Vector2(28f, -60f), new Vector2(664f, 40f));
            ToggleGroup group = row.GetComponent<ToggleGroup>();
            if (group == null)
            {
                group = row.gameObject.AddComponent<ToggleGroup>();
            }

            group.allowSwitchOff = false;
            Toggle[] stageTabs = new Toggle[5];
            for (int i = 0; i < stageTabs.Length; ++i)
            {
                stageTabs[i] = MakeTab(row, font, "StageTab" + (i + 1), "STAGE " + (i + 1), new Vector2(i * 98f, 0f), 92f, group);
            }

            Toggle overall = MakeTab(row, font, "OverallTab", "최단 클리어", new Vector2(490f, 0f), 130f, group);

            //# 헤더와 셀이 같은 열 표를 쓴다 — 순위 / 이름 / 시간 / 영웅 (RankingCell 과 동일 앵커).
            RectTransform header = Need(root, "Header");
            SetRect(header, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -108f), new Vector2(-56f, 36f));
            SetImage(header, Spr("Px_PanelDark"), Color.white, false);
            RankColumn(Need(root, "HRank"), font, 0, 16f, Sub);
            RankColumn(Need(root, "HName"), font, 1, 16f, Sub);
            RankColumn(Need(root, "HTime"), font, 2, 16f, Sub);
            RankColumn(Need(root, "HHero"), font, 3, 16f, Sub);

            RectTransform myRow = Need(root, "MyRankRow");
            SetRect(myRow, BottomLeft, BottomRight, new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(-56f, 52f));
            RectTransform divider = Need(root, "Divider");
            SetRect(divider, BottomLeft, BottomRight, new Vector2(0.5f, 0f), new Vector2(0f, 72f), new Vector2(-56f, 8f));
            SetImage(divider, Spr("Px_Divider"), Color.white, false);

            StyleText(Need(root, "EmptyText"), font, 18f, Sub, TextAlignmentOptions.Center, null);
            SetStretch(Need(root, "ScrollView"), 28f, 84f, 28f, 148f);

            RankingPopup popup = root.GetComponent<RankingPopup>();
            SetRefArray(popup, "_stageTabs", stageTabs);
            SetRef(popup, "_overallTab", overall);
        }

        //# 랭킹 열 표 — 헤더·셀 공용. 순위 / 이름 / 시간 / 영웅 (앵커 x 구간). 이름만 좌측 정렬.
        private static readonly float[] RankColumnEdges = { 0f, 0.14f, 0.58f, 0.84f, 1f };

        private static void RankColumn(RectTransform rt, TMP_FontAsset font, int column, float size, Color color)
        {
            SetRect(rt, new Vector2(RankColumnEdges[column], 0f), new Vector2(RankColumnEdges[column + 1], 1f), Half, Vector2.zero, new Vector2(-12f, 0f));
            StyleText(rt, font, size, color, column == 1 ? TextAlignmentOptions.Left : TextAlignmentOptions.Center, null);
        }

        private static Toggle MakeTab(RectTransform row, TMP_FontAsset font, string name, string label, Vector2 pos, float width, ToggleGroup group)
        {
            RectTransform tab = Child(row, name);
            SetRect(tab, MidLeft, MidLeft, MidLeft, pos, new Vector2(width, 40f));
            return SetupTab(tab, font, label, group);
        }

        //# 탭 위젯 공통 — 배경 Px_Tab, 켜졌을 때만 보이는 Checkmark(Px_TabOn), Label. Toggle 은 색 전환 없음(스프라이트로 구분).
        private static Toggle SetupTab(RectTransform tab, TMP_FontAsset font, string label, ToggleGroup group)
        {
            Image bg = SetImage(tab, Spr("Px_Tab"), Color.white, true);
            Toggle toggle = tab.GetComponent<Toggle>();
            if (toggle == null)
            {
                toggle = tab.gameObject.AddComponent<Toggle>();
            }

            if (tab.GetComponent<CHToggle>() == null)
            {
                tab.gameObject.AddComponent<CHToggle>();
            }

            RectTransform on = Child(tab, "Checkmark");
            SetStretch(on, 0f, 0f, 0f, 0f);
            Image onImage = SetImage(on, Spr("Px_TabOn"), Color.white, false);
            on.SetSiblingIndex(0);

            toggle.targetGraphic = bg;
            toggle.transition = Selectable.Transition.None;
            toggle.graphic = onImage;
            if (group != null)
            {
                toggle.group = group;
            }

            RectTransform labelRt = Child(tab, "Label");
            SetStretch(labelRt, 0f, 0f, 0f, 0f);
            if (label != null)
            {
                StyleText(labelRt, font, 17f, Bone, TextAlignmentOptions.Center, label);
            }
            else
            {
                StyleText(labelRt, font, 17f, Bone, TextAlignmentOptions.Center, null);
            }

            labelRt.SetAsLastSibling();
            return toggle;
        }

        private static void SkinTab(RectTransform tab, TMP_FontAsset font, float width)
        {
            tab.sizeDelta = new Vector2(width, 40f);
            Toggle existing = tab.GetComponent<Toggle>();
            SetupTab(tab, font, null, existing != null ? existing.group : null);
        }

        //# ---------------- Cloud ----------------

        private static void BuildCloudPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform connection = Need(root, "ConnectionText");
            SetRect(connection, TopLeft, TopRight, TopLeft, new Vector2(68f, -92f), new Vector2(-96f, 40f));
            StyleText(connection, font, 20f, Txt, TextAlignmentOptions.Left, null);
            RectTransform dot = Child(body, "ConnectionDot");
            SetRect(dot, TopLeft, TopLeft, Half, new Vector2(44f, -112f), new Vector2(20f, 20f));
            Image dotImage = SetImage(dot, Spr("Px_Dot"), UiDotPalette.Soul, false);

            RectTransform displayName = Need(root, "DisplayNameText");
            StyleText(displayName, font, 22f, Bone, TextAlignmentOptions.Left, null);

            SkinButton(Need(root, "ChangeNameButton"), Spr("Px_Btn"), font, 20f, Bone);
            SkinButton(Need(root, "RestoreButton"), Spr("Px_Btn"), font, 20f, Bone);

            //# 이름 편집 — 어두운 석판 + 파인 입력칸 + 확인(soul)/취소(기본).
            RectTransform editGroup = Need(root, "NameEditGroup");
            RemoveOutline(editGroup);
            SetImage(editGroup, Spr("Px_PanelDark"), Color.white, true);
            RectTransform input = Need(root, "NameInput");
            SetImage(input, Spr("Px_PanelSunk"), Color.white, true);
            RectTransform inputText = FindDeep(input, "Text");
            if (inputText != null)
            {
                TextMeshProUGUI tmp = inputText.GetComponent<TextMeshProUGUI>();
                if (tmp != null)
                {
                    tmp.color = Txt;
                    tmp.fontSize = 22f;
                }
            }

            SkinButton(Need(root, "NameConfirmButton"), Spr("Px_BtnSoul"), font, 20f, Color.white);
            SkinButton(Need(root, "NameCancelButton"), Spr("Px_Btn"), font, 20f, Bone);

            //# 충돌 — 붉은 경고 패널 + 이 기기/클라우드 비교 칸 + 버튼(나중에 기본 / 복원 gold).
            RectTransform conflict = Need(root, "ConflictGroup");
            RemoveOutline(conflict);
            SetRect(conflict, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -82f), new Vector2(-40f, 240f));
            SetImage(conflict, Spr("Px_Panel"), new Color(1f, 0.62f, 0.62f, 1f), true);
            RectTransform conflictRing = Child(conflict, "ConflictRing");
            SetStretch(conflictRing, 0f, 0f, 0f, 0f);
            Image conflictRingImage = SetImage(conflictRing, Spr("Px_Ring"), UiDotPalette.Blood, false);
            conflictRingImage.fillCenter = false;
            conflictRing.SetSiblingIndex(0);

            RectTransform conflictText = Need(root, "ConflictText");
            SetRect(conflictText, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(-32f, 76f));
            StyleText(conflictText, font, 17f, Warn, TextAlignmentOptions.Left, null);

            RectTransform compare = Child(conflict, "ConflictCompare");
            SetRect(compare, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -96f), new Vector2(-32f, 60f));
            CHText localValue = CompareBox(compare, font, "ConflictLocalBox", "이 기기", "ConflictLocalText", true);
            CHText cloudValue = CompareBox(compare, font, "ConflictCloudBox", "클라우드", "ConflictCloudText", false);

            SkinButton(Need(root, "ConflictRestoreButton"), Spr("Px_BtnGold"), font, 20f, Color.white);
            SkinButton(Need(root, "ConflictLaterButton"), Spr("Px_Btn"), font, 20f, Bone);

            RectTransform conflictDot = Need(root, "ConflictDot");
            SetImage(conflictDot, Spr("Px_Dot"), UiDotPalette.Blood, false);

            CloudPopup popup = root.GetComponent<CloudPopup>();
            SetRef(popup, "_connectionDot", dotImage);
            SetRef(popup, "_conflictCompare", compare.gameObject);
            SetRef(popup, "_conflictLocalText", localValue);
            SetRef(popup, "_conflictCloudText", cloudValue);
        }

        private static CHText CompareBox(RectTransform compare, TMP_FontAsset font, string boxName, string label, string valueName, bool left)
        {
            RectTransform box = Child(compare, boxName);
            box.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            box.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            box.pivot = Half;
            box.offsetMin = new Vector2(left ? 0f : 6f, 0f);
            box.offsetMax = new Vector2(left ? -6f : 0f, 0f);
            SetImage(box, Spr("Px_PanelSunk"), Color.white, false);

            RectTransform labelRt = Child(box, "Label");
            SetRect(labelRt, TopLeft, TopRight, new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(-16f, 18f));
            StyleText(labelRt, font, 14f, Sub, TextAlignmentOptions.Left, label);
            RectTransform value = Child(box, valueName);
            SetRect(value, BottomLeft, BottomRight, new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(-16f, 26f));
            StyleText(value, font, 18f, Bone, TextAlignmentOptions.Left, "-");
            return value.GetComponent<CHText>();
        }

        //# ---------------- 공용 팝업 프레임 ----------------

        //# Dim + 석판 본체(Outline 제거) + 명판 제목 + 핏빛 X. 본체 RectTransform 을 돌려준다.
        private static RectTransform SkinModal(GameObject root, TMP_FontAsset font)
        {
            RectTransform dim = Need(root, "Dim");
            SetImage(dim, null, Dim, true);
            dim.SetAsFirstSibling();

            RectTransform body = Need(root, "ModalBody");
            RemoveOutline(body);
            SetImage(body, Spr("Px_Panel"), Color.white, true);

            RectTransform plaque = Child(body, "TitlePlaque");
            SetRect(plaque, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            SetImage(plaque, Spr("Px_Plaque"), Color.white, false);
            plaque.SetSiblingIndex(0);

            RectTransform title = Need(body, "Title");
            SetRect(title, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Half, Vector2.zero, new Vector2(320f, 52f));
            StyleText(title, font, 26f, Bone, TextAlignmentOptions.Center, null);

            RectTransform close = Need(body, "CloseButton");
            SetRect(close, TopRight, TopRight, TopRight, new Vector2(13f, 13f), new Vector2(44f, 44f));
            SetImage(close, Spr("Px_CloseX"), Color.white, true);
            SetButtonTint(close, false);
            HideChild(close, "X");
            HideChild(close, "Label");
            close.SetAsLastSibling();
            return body;
        }

        //# ---------------- 공용 헬퍼 ----------------

        private static void TopText(RectTransform rt, TMP_FontAsset font, Vector2 anchor, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
        {
            SetRect(rt, anchor, anchor, anchor, pos, size);
            StyleText(rt, font, fontSize, color, align, null);
        }

        private static Sprite Spr(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            if (sprite == null)
            {
                Debug.LogError($"[UiRedesign2bBuilder] 스프라이트 없음: {SpriteDir}{name}.png");
            }

            return sprite;
        }

        private static RectTransform Need(GameObject root, string name)
        {
            return Need((RectTransform)root.transform, name);
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

        private static void SetStretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Half;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

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

        //# 스프라이트만 교체 — 색은 런타임 코드(발광색 등)가 정하므로 건드리지 않는다.
        private static void SetSpriteKeepColor(RectTransform rt, Sprite sprite, bool raycast)
        {
            Image image = rt.GetComponent<Image>();
            image.sprite = sprite;
            image.raycastTarget = raycast;
            image.type = sprite != null && sprite.border != Vector4.zero ? Image.Type.Sliced : Image.Type.Simple;
            image.fillCenter = false;
        }

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

        private static void SetButtonTint(RectTransform rt, bool disabledUntinted)
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
            //# 상점 구매 버튼처럼 비활성 상태를 스프라이트(off/gold)로 구분하는 버튼은 비활성 tint 를 끈다.
            colors.disabledColor = disabledUntinted ? Color.white : new Color(0.55f, 0.55f, 0.55f, 1f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
        }

        private static void SkinButton(RectTransform rt, Sprite sprite, TMP_FontAsset font, float labelSize, Color labelColor)
        {
            SetImage(rt, sprite, Color.white, true);
            SetButtonTint(rt, false);
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

        private static void SetRef(Component owner, string field, UnityEngine.Object value)
        {
            if (owner == null)
            {
                Debug.LogError($"[UiRedesign2bBuilder] 소유 컴포넌트 없음: {field}");
                return;
            }

            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign2bBuilder] 필드 없음: {owner.GetType().Name}.{field}");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetRefArray(Component owner, string field, UnityEngine.Object[] values)
        {
            if (owner == null)
            {
                Debug.LogError($"[UiRedesign2bBuilder] 소유 컴포넌트 없음: {field}");
                return;
            }

            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign2bBuilder] 필드 없음: {owner.GetType().Name}.{field}");
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
