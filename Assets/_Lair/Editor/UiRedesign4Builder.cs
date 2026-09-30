using System;
using System.Collections.Generic;
using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# UI 도트 던전 리디자인 4단계 — 전투 팝업 4종(+셀 2종)을 "열어서 수정"하는 일회용 authoring 툴 (Rule 04 §3, 실행 후 삭제).
    //# 대상: CardSelectionPopup, BuildModalPopup/BuildModalCardCell, SynergyModalPopup/SynergyModalCell, ResultPopup.
    //# GUID·GameObject 이름·기존 [SerializeField] 참조 유지, 멱등(이름으로 찾아 재사용/갱신). ResultPopup.Background(결과 화면 스프라이트)는 건드리지 않는다.
    public static class UiRedesign4Builder
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/UiDot/";

        private static readonly Color Bone = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private static readonly Color Sub = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color Txt = new Color32(0xEE, 0xF1, 0xF6, 0xFF);
        private static readonly Color DescGray = new Color32(0xCF, 0xD5, 0xE2, 0xFF);
        private static readonly Color Ink = new Color32(0x07, 0x09, 0x0E, 0xFF);
        private static readonly Color Stone0 = new Color32(0x10, 0x14, 0x1D, 0xFF);
        private static readonly Color HeroHpRed = new Color32(0xFF, 0x8A, 0x8D, 0xFF);
        private static readonly Color Dim = new Color32(4, 6, 10, 204);
        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopMid = new Vector2(0.5f, 1f);
        private static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MidRight = new Vector2(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomMid = new Vector2(0.5f, 0f);

        [MenuItem("Lair/UI/Build 4")]
        public static void Build()
        {
            Process("CardSelectionPopup", BuildCardSelectionPopup);
            Process("BuildModalCardCell", BuildBuildModalCardCell);
            Process("BuildModalPopup", BuildBuildModalPopup);
            Process("SynergyModalCell", BuildSynergyModalCell);
            Process("SynergyModalPopup", BuildSynergyModalPopup);
            Process("ResultPopup", BuildResultPopup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiRedesign4Builder] 4단계 프리팹 6종 수정 완료");
        }

        private static void Process(string prefabName, Action<GameObject> build)
        {
            string path = UiDir + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[UiRedesign4Builder] {prefabName} 저장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiRedesign4Builder] {prefabName} 실패: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ---------------- CardSelectionPopup ----------------

        //# 카드 폭 계산(1280x720, Match 0.5): 시안 210 css → 280 ref, 간격 28 css → 36 ref. 3장 = 3*280 + 2*36 = 912 < 1280.
        //# 4:3 기기(캔버스 가로 ≈ 1108 ref)에서도 912 로 안 넘친다. 높이 440: 중앙 y -40 이라 위 180 ~ 아래 620(720 기준).
        private const float CardWidth = 280f;
        private const float CardHeight = 440f;
        private const float CardGap = 36f;

        private static void BuildCardSelectionPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;

            RectTransform dim = Need(root, "Dim");
            SetImage(dim, null, Dim, true);
            dim.SetAsFirstSibling();

            RectTransform title = Need(root, "Title");
            SetRect(title, TopMid, TopMid, TopMid, new Vector2(0f, -30f), new Vector2(600f, 44f));
            StyleText(title, font, 26f, Bone, TextAlignmentOptions.Center, null);

            RectTransform subtitle = Child(rootRt, "Subtitle");
            SetRect(subtitle, TopMid, TopMid, TopMid, new Vector2(0f, -78f), new Vector2(600f, 26f));
            StyleText(subtitle, font, 18f, UiDotPalette.Soul, TextAlignmentOptions.Center, "패시브 · 영웅 HP 90% 도달");

            RectTransform layout = Need(root, "CardsLayout");
            SetRect(layout, Half, Half, Half, new Vector2(0f, -40f), new Vector2(3f * CardWidth + 2f * CardGap, CardHeight));
            HorizontalLayoutGroup group = layout.GetComponent<HorizontalLayoutGroup>();
            if (group != null)
            {
                group.spacing = CardGap;
                group.childAlignment = TextAnchor.MiddleCenter;
                group.childControlWidth = false;
                group.childControlHeight = false;
                group.childForceExpandWidth = false;
                group.childForceExpandHeight = false;
            }

            for (int i = 0; i < 3; ++i)
            {
                BuildCard(Need(root, "CardView_" + i), font);
            }

            SetRef(root.GetComponent<CardSelectionPopup>(), "_subtitleText", subtitle.GetComponent<CHText>());
        }

        private static void BuildCard(RectTransform card, TMP_FontAsset font)
        {
            card.sizeDelta = new Vector2(CardWidth, CardHeight);

            RectTransform bg = Need(card, "Bg");
            SetStretch(bg, 0f, 0f, 0f, 0f);
            SetImage(bg, Spr("Px_PanelDark"), Color.white, false);
            bg.SetSiblingIndex(0);

            //# 기존 Border 는 카드 종(種) 색을 칠하는 코드가 있어 그리지 않는다 — 프레임은 종류색 KindRing 이 담당한다.
            RectTransform border = Need(card, "Border");
            Image borderImage = SetImage(border, null, new Color(0f, 0f, 0f, 0f), false);
            borderImage.enabled = false;
            border.SetSiblingIndex(1);

            RectTransform kindRing = Child(card, "KindRing");
            SetStretch(kindRing, 0f, 0f, 0f, 0f);
            Image kindRingImage = SetImage(kindRing, Spr("Px_CardRing"), UiDotPalette.Soul, false);
            kindRingImage.fillCenter = false;
            kindRing.SetSiblingIndex(2);

            RectTransform slot = Child(card, "ArtSlot");
            SetRect(slot, TopLeft, TopRight, TopMid, new Vector2(0f, -14f), new Vector2(-28f, 250f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(3);

            RectTransform art = Need(card, "CardArt");
            SetRect(art, TopLeft, TopRight, TopMid, new Vector2(0f, -18f), new Vector2(-36f, 242f));
            Image artImage = art.GetComponent<Image>();
            artImage.type = Image.Type.Simple;
            artImage.preserveAspect = true;
            artImage.raycastTarget = false;
            art.SetSiblingIndex(4);

            RectTransform scrim = Need(card, "Scrim");
            SetRect(scrim, TopLeft, TopRight, TopMid, new Vector2(0f, -160f), new Vector2(-36f, 100f));
            SetImage(scrim, Spr("Px_Solid"), new Color32(8, 10, 16, 217), false);
            scrim.SetSiblingIndex(5);

            RectTransform nameText = Need(card, "NameText");
            SetRect(nameText, TopLeft, TopRight, TopMid, new Vector2(0f, -222f), new Vector2(-36f, 34f));
            StyleText(nameText, font, 24f, Bone, TextAlignmentOptions.Center, null);

            RectTransform descText = Need(card, "DescText");
            SetRect(descText, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 80f), new Vector2(-36f, 92f));
            StyleText(descText, font, 17f, DescGray, TextAlignmentOptions.Center, null);

            RectTransform kindBg = Child(card, "KindLabelBg");
            SetRect(kindBg, TopLeft, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(84f, 28f));
            SetImage(kindBg, Spr("Px_Solid"), Ink, false);
            RectTransform kindLabel = Child(card, "KindLabel");
            SetRect(kindLabel, TopLeft, TopLeft, TopLeft, new Vector2(24f, -24f), new Vector2(84f, 28f));
            StyleText(kindLabel, font, 16f, UiDotPalette.Soul, TextAlignmentOptions.Center, "패시브");

            RectTransform countBg = Child(card, "CountBadgeBg");
            SetRect(countBg, TopRight, TopRight, TopRight, new Vector2(-24f, -24f), new Vector2(72f, 28f));
            SetImage(countBg, Spr("Px_Badge_Gold"), Color.white, false);
            countBg.gameObject.SetActive(false);
            RectTransform countBadge = Need(card, "CountBadge");
            SetRect(countBadge, TopRight, TopRight, TopRight, new Vector2(-24f, -24f), new Vector2(72f, 28f));
            StyleText(countBadge, font, 16f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);

            //# PickButton 은 카드 전체를 덮는 투명 클릭 영역 그대로 두고(카드 어디를 눌러도 선택), 시각 버튼(PickVisual)만 하단에 그린다.
            RectTransform pick = Need(card, "PickButton");
            SetStretch(pick, 0f, 0f, 0f, 0f);
            SetImage(pick, null, new Color(0f, 0f, 0f, 0f), true);
            Button pickButton = pick.GetComponent<Button>();
            if (pickButton != null)
            {
                pickButton.transition = Selectable.Transition.None;
                pickButton.targetGraphic = pick.GetComponent<Image>();
            }

            RectTransform visual = Child(pick, "PickVisual");
            SetRect(visual, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 14f), new Vector2(-36f, 56f));
            Image visualImage = SetImage(visual, Spr("Px_BtnSoul"), Color.white, false);
            RectTransform pickLabel = Child(visual, "Label");
            SetStretch(pickLabel, 0f, 0f, 0f, 0f);
            StyleText(pickLabel, font, 22f, Color.white, TextAlignmentOptions.Center, "선택");
            pick.SetAsLastSibling();

            CardView view = card.GetComponent<CardView>();
            SetRef(view, "_kindRing", kindRingImage);
            SetRef(view, "_kindLabel", kindLabel.GetComponent<CHText>());
            SetRef(view, "_pickButtonImage", visualImage);
            SetRef(view, "_passiveButtonSprite", Spr("Px_BtnSoul"));
            SetRef(view, "_activeButtonSprite", Spr("Px_BtnGold"));
            SetRef(view, "_countBadgeBg", countBg.gameObject);
        }

        //# ---------------- BuildModal ----------------

        private static void BuildBuildModalCardCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(280f, 64f);
            RectTransform back = Child(rootRt, "RowBg");
            SetStretch(back, 0f, 0f, 0f, 0f);
            SetImage(back, Spr("Px_PanelDark"), Color.white, false);
            back.SetSiblingIndex(0);

            RectTransform frame = Need(root, "Frame");
            SetRect(frame, MidLeft, MidLeft, MidLeft, new Vector2(6f, 0f), new Vector2(52f, 52f));
            SetImage(frame, Spr("Px_PanelSunk"), Color.white, false);
            RectTransform icon = Need(root, "Icon");
            SetRect(icon, MidLeft, MidLeft, MidLeft, new Vector2(12f, 0f), new Vector2(40f, 40f));
            icon.GetComponent<Image>().preserveAspect = true;

            RectTransform ring = Child(rootRt, "KindRing");
            SetRect(ring, MidLeft, MidLeft, MidLeft, new Vector2(6f, 0f), new Vector2(52f, 52f));
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Soul, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(icon.GetSiblingIndex() + 1);

            RectTransform nameText = Need(root, "NameText");
            SetRect(nameText, TopLeft, TopRight, TopLeft, new Vector2(68f, -6f), new Vector2(-120f, 24f));
            StyleText(nameText, font, 18f, Bone, TextAlignmentOptions.Left, null);
            RectTransform count = Need(root, "CountText");
            SetRect(count, TopRight, TopRight, TopRight, new Vector2(-8f, -6f), new Vector2(44f, 24f));
            StyleText(count, font, 16f, UiDotPalette.Gold, TextAlignmentOptions.Right, null);
            RectTransform desc = Need(root, "DescText");
            SetRect(desc, BottomLeft, BottomRight, BottomLeft, new Vector2(68f, 8f), new Vector2(-76f, 22f));
            StyleText(desc, font, 14f, DescGray, TextAlignmentOptions.Left, null);

            SetRef(root.GetComponent<BuildModalCardCell>(), "_kindRing", ringImage);
        }

        private static void BuildBuildModalPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform divider = Need(root, "Divider");
            divider.anchorMin = new Vector2(0.5f, 0f);
            divider.anchorMax = new Vector2(0.5f, 1f);
            divider.pivot = Half;
            divider.offsetMin = new Vector2(-2f, 28f);
            divider.offsetMax = new Vector2(2f, -72f);
            SetImage(divider, Spr("Px_Solid"), Stone0, false);

            BuildSection(body, font, "PassiveSection", "PassiveHeaderSquare", UiDotPalette.Soul, true);
            BuildSection(body, font, "ActiveSection", "ActiveHeaderSquare", UiDotPalette.Gold, false);
        }

        //# 좌(패시브)/우(액티브) 한 단 — 종류색 사각 + 라벨 + 카드 목록(열 1, 간격 8).
        private static void BuildSection(RectTransform body, TMP_FontAsset font, string sectionName, string squareName, Color kind, bool left)
        {
            RectTransform section = Need(body, sectionName);
            section.anchorMin = new Vector2(left ? 0f : 0.5f, 0f);
            section.anchorMax = new Vector2(left ? 0.5f : 1f, 1f);
            section.pivot = Half;
            section.offsetMin = new Vector2(left ? 28f : 10f, 28f);
            section.offsetMax = new Vector2(left ? -10f : -28f, -72f);

            RectTransform square = Child(section, squareName);
            SetRect(square, TopLeft, TopLeft, TopLeft, new Vector2(2f, -8f), new Vector2(10f, 10f));
            SetImage(square, Spr("Px_Solid"), kind, false);
            RectTransform label = Need(section, "Label");
            SetRect(label, TopLeft, TopRight, TopLeft, new Vector2(18f, 0f), new Vector2(-18f, 26f));
            StyleText(label, font, 18f, Bone, TextAlignmentOptions.Left, null);

            RectTransform scroll = Need(section, "ScrollView");
            SetStretch(scroll, 0f, 0f, 0f, 32f);
            SetPooling(scroll.GetComponent<BuildModalCardPoolingScrollView>(), 1, new Vector2(0f, 8f));
            RectTransform empty = Need(section, "EmptyText");
            SetStretch(empty, 0f, 0f, 0f, 32f);
            StyleText(empty, font, 16f, Sub, TextAlignmentOptions.Center, null);
        }

        //# ---------------- SynergyModal ----------------

        private static void BuildSynergyModalCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(392f, 96f);
            SetImage(rootRt, Spr("Px_PanelDark"), Color.white, false);

            RectTransform strip = Need(root, "AxisStrip");
            SetRect(strip, BottomLeft, TopLeft, MidLeft, new Vector2(4f, 0f), new Vector2(8f, -8f));
            SetImage(strip, Spr("Px_Solid"), Color.white, false);
            strip.SetSiblingIndex(0);

            RectTransform slot = Child(rootRt, "IconSlot");
            SetRect(slot, MidLeft, MidLeft, MidLeft, new Vector2(20f, 0f), new Vector2(52f, 52f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(1);

            RectTransform icon = Need(root, "Icon");
            SetRect(icon, MidLeft, MidLeft, Half, new Vector2(46f, 0f), new Vector2(36f, 36f));
            icon.GetComponent<Image>().preserveAspect = true;

            RectTransform label = Need(root, "Label");
            SetRect(label, TopLeft, TopLeft, TopLeft, new Vector2(84f, -10f), new Vector2(200f, 26f));
            StyleText(label, font, 20f, Bone, TextAlignmentOptions.Left, null);

            RectTransform badgeBg = Child(rootRt, "TierBadgeBg");
            SetRect(badgeBg, TopRight, TopRight, TopRight, new Vector2(-14f, -10f), new Vector2(64f, 28f));
            SetImage(badgeBg, Spr("Px_Badge_Soul"), Color.white, false);
            RectTransform badge = Child(rootRt, "TierBadge");
            SetRect(badge, TopRight, TopRight, TopRight, new Vector2(-14f, -10f), new Vector2(64f, 28f));
            StyleText(badge, font, 15f, UiDotPalette.Soul, TextAlignmentOptions.Center, "1/3");

            RectTransform desc = Child(rootRt, "DescText");
            SetRect(desc, TopLeft, TopRight, TopLeft, new Vector2(84f, -40f), new Vector2(-98f, 22f));
            StyleText(desc, font, 16f, Txt, TextAlignmentOptions.Left, null);
            RectTransform next = Child(rootRt, "NextText");
            SetRect(next, TopLeft, TopRight, TopLeft, new Vector2(84f, -64f), new Vector2(-98f, 22f));
            StyleText(next, font, 15f, Sub, TextAlignmentOptions.Left, null);

            SynergyModalCell cell = root.GetComponent<SynergyModalCell>();
            SetRef(cell, "_tierBadge", badge.GetComponent<CHText>());
            SetRef(cell, "_descText", desc.GetComponent<CHText>());
            SetRef(cell, "_nextText", next.GetComponent<CHText>());
        }

        private static void BuildSynergyModalPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform body = SkinModal(root, font);

            RectTransform scroll = Need(root, "ScrollView");
            SetStretch(scroll, 28f, 28f, 28f, 72f);
            SetPooling(scroll.GetComponent<SynergyModalCardPoolingScrollView>(), 1, new Vector2(0f, 10f));

            RectTransform scrollbar = Need(root, "VerticalScrollbar");
            SetImage(scrollbar, null, new Color(0f, 0f, 0f, 0f), true);
            RectTransform handle = Need(root, "Handle");
            SetImage(handle, Spr("Px_Solid"), UiDotPalette.Stone4, true);

            RectTransform empty = Need(root, "EmptyText");
            SetStretch(empty, 28f, 28f, 28f, 72f);
            StyleText(empty, font, 18f, Sub, TextAlignmentOptions.Center, null);

            SerializedObject so = new SerializedObject(root.GetComponent<SynergyModalPopup>());
            SerializedProperty headerOnly = so.FindProperty("_headerRowsOnly");
            if (headerOnly == null)
            {
                Debug.LogError("[UiRedesign4Builder] 필드 없음: SynergyModalPopup._headerRowsOnly");
            }
            else
            {
                headerOnly.boolValue = true;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        //# ---------------- ResultPopup ----------------

        private static void BuildResultPopup(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;

            //# Background(결과 화면 스프라이트)는 그대로 — ResultPopupBackgroundTests 가 지킨다.
            Need(root, "Background").SetAsFirstSibling();
            RectTransform dim = Need(root, "Dim");
            SetImage(dim, null, Dim, true);
            dim.SetSiblingIndex(1);

            RectTransform rays = Child(rootRt, "Rays");
            SetRect(rays, Half, Half, Half, new Vector2(0f, 40f), new Vector2(640f, 640f));
            SetImage(rays, Spr("Px_Rays"), Color.white, false);
            rays.gameObject.SetActive(false);
            rays.SetSiblingIndex(2);

            RectTransform panel = Child(rootRt, "ResultPanel");
            SetRect(panel, Half, Half, Half, Vector2.zero, new Vector2(600f, 480f));
            SetImage(panel, Spr("Px_Panel"), Color.white, true);
            panel.SetSiblingIndex(3);

            RectTransform result = Need(root, "ResultText");
            SetRect(result, Half, Half, Half, new Vector2(0f, 158f), new Vector2(560f, 72f));
            StyleText(result, font, 60f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);
            RectTransform sub = Child(rootRt, "SubText");
            SetRect(sub, Half, Half, Half, new Vector2(0f, 102f), new Vector2(560f, 26f));
            StyleText(sub, font, 17f, Sub, TextAlignmentOptions.Center, "영웅을 처치했다");

            RectTransform rowClear = ResultRow(rootRt, font, "RowClear", "클리어 시간", "ClearTimeText", 48f, UiDotPalette.Soul, out CHText clearValue);
            RectTransform newBadge = Child(rowClear, "NewBadge");
            SetRect(newBadge, MidRight, MidRight, MidRight, new Vector2(-140f, 0f), new Vector2(56f, 28f));
            SetImage(newBadge, Spr("Px_Badge_Soul"), Color.white, false);
            RectTransform newLabel = Child(newBadge, "Label");
            SetStretch(newLabel, 0f, 0f, 0f, 0f);
            StyleText(newLabel, font, 14f, UiDotPalette.Soul, TextAlignmentOptions.Center, "NEW");
            newBadge.gameObject.SetActive(false);

            RectTransform rowHeroHp = ResultRow(rootRt, font, "RowHeroHp", "영웅 남은 HP", "HeroHpText", 48f, HeroHpRed, out CHText heroHpValue);
            RectTransform rowSouls = ResultRow(rootRt, font, "RowSouls", "획득 소울", "SoulsText", -4f, UiDotPalette.Soul, out CHText soulsValue);
            RectTransform rowXp = ResultRow(rootRt, font, "RowXp", "영주 XP", "XpText", -56f, Txt, out CHText xpValue);

            //# 기존 RewardText 는 유지 — 영주 레벨 업·도전과제 달성 줄이 여기에 표시된다(작게, 항목 줄 아래).
            RectTransform reward = Need(root, "RewardText");
            SetRect(reward, Half, Half, Half, new Vector2(0f, -116f), new Vector2(520f, 54f));
            StyleText(reward, font, 14f, Sub, TextAlignmentOptions.Center, null);

            //# 버튼: 마을로(기본, 왼쪽) / 다시 도전(blood, 오른쪽).
            ResultButton(Need(root, "VillageButton"), font, Spr("Px_Btn"), new Vector2(-118f, -190f), Bone);
            ResultButton(Need(root, "RetryButton"), font, Spr("Px_BtnBlood"), new Vector2(118f, -190f), Color.white);

            ResultPopup popup = root.GetComponent<ResultPopup>();
            SetRef(popup, "_subText", sub.GetComponent<CHText>());
            SetRef(popup, "_rowClear", rowClear.gameObject);
            SetRef(popup, "_clearTimeText", clearValue);
            SetRef(popup, "_newBadge", newBadge.gameObject);
            SetRef(popup, "_rowSouls", rowSouls.gameObject);
            SetRef(popup, "_soulsText", soulsValue);
            SetRef(popup, "_rowXp", rowXp.gameObject);
            SetRef(popup, "_xpText", xpValue);
            SetRef(popup, "_rowHeroHp", rowHeroHp.gameObject);
            SetRef(popup, "_heroHpText", heroHpValue);
            SetRef(popup, "_rays", rays.gameObject);
        }

        //# 결과 항목 줄 — sunk 패널 + 왼쪽 라벨 + 오른쪽 값. 값 CHText 를 돌려준다.
        private static RectTransform ResultRow(RectTransform parent, TMP_FontAsset font, string name, string label, string valueName, float y, Color valueColor, out CHText value)
        {
            RectTransform row = Child(parent, name);
            SetRect(row, Half, Half, Half, new Vector2(0f, y), new Vector2(520f, 44f));
            SetImage(row, Spr("Px_PanelSunk"), Color.white, false);
            RectTransform labelRt = Child(row, "Label");
            SetRect(labelRt, MidLeft, MidLeft, MidLeft, new Vector2(16f, 0f), new Vector2(220f, 26f));
            StyleText(labelRt, font, 17f, Sub, TextAlignmentOptions.Left, label);
            RectTransform valueRt = Child(row, valueName);
            SetRect(valueRt, MidRight, MidRight, MidRight, new Vector2(-16f, 0f), new Vector2(150f, 30f));
            StyleText(valueRt, font, 21f, valueColor, TextAlignmentOptions.Right, "-");
            value = valueRt.GetComponent<CHText>();
            return row;
        }

        private static void ResultButton(RectTransform rt, TMP_FontAsset font, Sprite sprite, Vector2 pos, Color labelColor)
        {
            SetRect(rt, Half, Half, Half, pos, new Vector2(220f, 68f));
            SetImage(rt, sprite, Color.white, true);
            SetButtonTint(rt);
            RectTransform label = Need(rt, "ButtonText");
            SetStretch(label, 0f, 0f, 0f, 0f);
            StyleText(label, font, 24f, labelColor, TextAlignmentOptions.Center, null);
        }

        //# ---------------- 공용 팝업 프레임 ----------------

        //# Dim + 석판 본체(Outline 제거) + 명판 제목 + 핏빛 X. 본체 RectTransform 을 돌려준다.
        private static RectTransform SkinModal(GameObject root, TMP_FontAsset font)
        {
            RectTransform dim = Need(root, "Dim");
            SetImage(dim, null, Dim, true);
            dim.SetAsFirstSibling();

            RectTransform body = Need(root, "ModalBody");
            Outline outline = body.GetComponent<Outline>();
            if (outline != null)
            {
                UnityEngine.Object.DestroyImmediate(outline, true);
            }

            SetImage(body, Spr("Px_Panel"), Color.white, true);

            RectTransform plaque = Child(body, "TitlePlaque");
            SetRect(plaque, TopMid, TopMid, Half, Vector2.zero, new Vector2(320f, 52f));
            SetImage(plaque, Spr("Px_Plaque"), Color.white, false);
            plaque.SetSiblingIndex(0);

            RectTransform title = Need(body, "Title");
            SetRect(title, TopMid, TopMid, Half, Vector2.zero, new Vector2(320f, 52f));
            StyleText(title, font, 26f, Bone, TextAlignmentOptions.Center, null);

            RectTransform close = Need(body, "CloseButton");
            SetRect(close, TopRight, TopRight, TopRight, new Vector2(13f, 13f), new Vector2(44f, 44f));
            SetImage(close, Spr("Px_CloseX"), Color.white, true);
            SetButtonTint(close);
            HideChild(close, "X");
            close.SetAsLastSibling();
            return body;
        }

        //# ---------------- 공용 헬퍼 ----------------

        //# CHPoolingScrollView 파생의 격자 설정 — 열 수·간격·패딩 0. 필드명이 없으면 에러 로그로 드러낸다.
        private static void SetPooling(Component pooling, int columns, Vector2 gap)
        {
            if (pooling == null)
            {
                Debug.LogError("[UiRedesign4Builder] 풀링 스크롤뷰 컴포넌트 없음");
                return;
            }

            SerializedObject so = new SerializedObject(pooling);
            SerializedProperty column = so.FindProperty("_columnCount");
            SerializedProperty itemGap = so.FindProperty("_itemGap");
            if (column == null || itemGap == null)
            {
                Debug.LogError($"[UiRedesign4Builder] 풀링 필드 없음: {pooling.GetType().Name}");
                return;
            }

            column.intValue = columns;
            itemGap.vector2Value = gap;
            string[] paddingKeys = { "_padding.m_Left", "_padding.m_Right", "_padding.m_Top", "_padding.m_Bottom" };
            for (int i = 0; i < paddingKeys.Length; ++i)
            {
                SerializedProperty padding = so.FindProperty(paddingKeys[i]);
                if (padding != null)
                {
                    padding.intValue = 0;
                }
            }

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static List<RectTransform> FindAll(RectTransform parent, string name)
        {
            List<RectTransform> found = new List<RectTransform>();
            CollectAll(parent, name, found);
            return found;
        }

        private static void CollectAll(RectTransform parent, string name, List<RectTransform> found)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                {
                    found.Add((RectTransform)child);
                }

                CollectAll((RectTransform)child, name, found);
            }
        }

        private static Sprite Spr(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            if (sprite == null)
            {
                Debug.LogError($"[UiRedesign4Builder] 스프라이트 없음: {SpriteDir}{name}.png");
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
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            colors.selectedColor = Color.white;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.05f;
            button.colors = colors;
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
                Debug.LogError($"[UiRedesign4Builder] 소유 컴포넌트 없음: {field}");
                return;
            }

            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign4Builder] 필드 없음: {owner.GetType().Name}.{field}");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
