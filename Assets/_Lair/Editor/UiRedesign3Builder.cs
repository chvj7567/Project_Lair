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
    //# UI 도트 던전 리디자인 3단계 — 전투 HUD 그룹 프리팹을 "열어서 수정"하는 일회용 authoring 툴 (Rule 04 §3, 실행 후 삭제).
    //# 대상: BuildIconCell, HpBar, SpawnerStatusCell/Panel, BuildSynergyCell/Panel, SkillUnlockBanner, BattleHud (마지막 — 중첩 인스턴스 배치).
    //# 전투 HUD 는 CHMUI 가 만든 ScreenSpaceOverlay Canvas(CanvasScaler 1280x720, Match 0.5) 위에 그려지는 UI 다 — 월드 캔버스 아님.
    //# 멱등 — 위젯은 이름으로 찾아 재사용/갱신한다. HpBar 는 몬스터 프리팹도 공유하므로 루트 크기는 건드리지 않는다.
    public static class UiRedesign3Builder
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/UiDot/";

        private static readonly Color Bone = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private static readonly Color Sub = new Color32(0x8E, 0x98, 0xAD, 0xFF);
        private static readonly Color Ink = new Color32(0x07, 0x09, 0x0E, 0xFF);
        private static readonly Color Curse = new Color32(0xB5, 0x8C, 0xFF, 0xFF);
        private static readonly Color Curse2 = new Color32(0x5B, 0x3A, 0xA6, 0xFF);
        private static readonly Color MarkerOff = new Color32(0x0B, 0x0E, 0x14, 0xFF);
        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopMid = new Vector2(0.5f, 1f);
        private static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MidRight = new Vector2(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomMid = new Vector2(0.5f, 0f);

        [MenuItem("Lair/UI/Build 3")]
        public static void Build()
        {
            Process("BuildIconCell", BuildBuildIconCell);
            Process("HpBar", BuildHpBar);
            Process("SpawnerStatusCell", BuildSpawnerStatusCell);
            Process("SpawnerStatusPanel", BuildSpawnerStatusPanel);
            Process("BuildSynergyCell", BuildBuildSynergyCell);
            Process("BuildSynergyPanel", BuildBuildSynergyPanel);
            Process("SkillUnlockBanner", BuildSkillUnlockBanner);
            Process("BattleHud", BuildBattleHud);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiRedesign3Builder] 3단계 프리팹 8종 수정 완료");
        }

        private static void Process(string prefabName, Action<GameObject> build)
        {
            string path = UiDir + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                build(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[UiRedesign3Builder] {prefabName} 저장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiRedesign3Builder] {prefabName} 실패: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ---------------- BuildIconCell ----------------

        private static void BuildBuildIconCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(52f, 52f);

            RectTransform frame = Need(root, "Frame");
            SetStretch(frame, 0f, 0f, 0f, 0f);
            SetImage(frame, Spr("Px_PanelSunk"), Color.white, false);
            frame.SetSiblingIndex(0);

            RectTransform hatch = Child(rootRt, "EmptyHatch");
            SetStretch(hatch, 4f, 4f, 4f, 4f);
            Image hatchImage = SetImage(hatch, Spr("Px_Hatch"), Color.white, false);
            hatchImage.type = Image.Type.Tiled;
            hatch.gameObject.SetActive(false);
            hatch.SetSiblingIndex(1);

            RectTransform icon = Need(root, "Icon");
            SetStretch(icon, 6f, 6f, 6f, 6f);
            icon.GetComponent<Image>().preserveAspect = true;
            icon.SetSiblingIndex(2);

            RectTransform ring = Child(rootRt, "KindRing");
            SetStretch(ring, 0f, 0f, 0f, 0f);
            Image ringImage = SetImage(ring, Spr("Px_Ring"), UiDotPalette.Soul, false);
            ringImage.fillCenter = false;
            ring.SetSiblingIndex(3);

            RectTransform countBg = Child(rootRt, "CountBg");
            SetRect(countBg, BottomRight, BottomRight, BottomRight, Vector2.zero, new Vector2(30f, 20f));
            SetImage(countBg, Spr("Px_Badge_Gold"), Color.white, false);
            countBg.gameObject.SetActive(false);
            RectTransform count = Need(root, "CountText");
            SetRect(count, BottomRight, BottomRight, BottomRight, Vector2.zero, new Vector2(30f, 20f));
            StyleText(count, font, 15f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);
            count.SetAsLastSibling();

            BuildIconCell cell = root.GetComponent<BuildIconCell>();
            SetRef(cell, "_kindRing", ringImage);
            SetRef(cell, "_emptyHatch", hatch.gameObject);
            SetRef(cell, "_countBg", countBg.gameObject);
        }

        //# ---------------- HpBar ----------------

        private static void BuildHpBar(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);

            RectTransform background = Need(root, "Background");
            SetStretch(background, 0f, 0f, 0f, 0f);
            SetImage(background, Spr("Px_BarBg"), Color.white, false);

            RectTransform fill = Need(root, "Fill");
            SetStretch(fill, 4f, 4f, 4f, 4f);
            Image fillImage = SetImage(fill, Spr("Px_BarFill"), UiDotPalette.Blood, false);
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fill.SetSiblingIndex(0);

            //# 10칸 눈금 — 정적 9개(SegDivider1~9), 앵커 x = n/10.
            for (int i = 1; i <= 9; ++i)
            {
                RectTransform seg = Child(background, "SegDivider" + i);
                SetRect(seg, new Vector2(i / 10f, 0f), new Vector2(i / 10f, 1f), Half, Vector2.zero, new Vector2(4f, -8f));
                SetImage(seg, Spr("Px_Solid"), new Color(0f, 0f, 0f, 0.6f), false);
            }

            RectTransform hpText = Need(root, "txtHp");
            StyleText(hpText, font, 14f, Bone, TextAlignmentOptions.Center, null);

            //# 상태 아이콘 행 — 최대 8칸, 24 ref(=시안 18px) 도트 프레임. 슬롯 이미지는 코드가 sprite 를 채운다.
            RectTransform row = Need(root, "StatusIconRow");
            SetRect(row, BottomLeft, BottomRight, TopMid, new Vector2(0f, -6f), new Vector2(0f, 28f));
            HorizontalLayoutGroup layout = row.GetComponent<HorizontalLayoutGroup>();
            if (layout != null)
            {
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.MiddleCenter;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;
            }

            for (int i = 0; i < 8; ++i)
            {
                RectTransform slot = Need(row, "Icon" + i);
                slot.sizeDelta = new Vector2(24f, 24f);
                RectTransform slotFrame = Child(slot, "Frame");
                SetStretch(slotFrame, 0f, 0f, 0f, 0f);
                Image frameImage = SetImage(slotFrame, Spr("Px_Ring"), Ink, false);
                frameImage.fillCenter = false;
            }
        }

        //# ---------------- SpawnerStatus ----------------

        private static void BuildSpawnerStatusCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(276f, 80f);
            SetImage(rootRt, Spr("Px_Panel"), Color.white, true);

            HideChild(rootRt, "InnerBackground");

            //# 종 대표색 테두리는 코드가 color 만 칠한다 — 리디자인은 왼쪽 색띠(ColorChip)가 대신하므로 Border 는 그리지 않는다.
            RectTransform border = Need(root, "Border");
            Image borderImage = SetImage(border, null, new Color(0f, 0f, 0f, 0f), false);
            borderImage.enabled = false;

            RectTransform chip = Need(root, "ColorChip");
            Reparent(chip, rootRt);
            SetRect(chip, BottomLeft, TopLeft, MidLeft, new Vector2(6f, 0f), new Vector2(8f, -16f));
            SetImage(chip, Spr("Px_Solid"), Color.white, false);

            RectTransform slot = Child(rootRt, "IconSlot");
            SetRect(slot, MidLeft, MidLeft, MidLeft, new Vector2(22f, 0f), new Vector2(60f, 60f));
            SetImage(slot, Spr("Px_PanelSunk"), Color.white, false);
            slot.SetSiblingIndex(1);

            RectTransform glow = Need(root, "GlowOverlay");
            SetRect(glow, MidLeft, MidLeft, Half, new Vector2(52f, 0f), new Vector2(68f, 68f));
            Image glowImage = glow.GetComponent<Image>();
            glowImage.sprite = Spr("Px_Ring");
            glowImage.type = Image.Type.Sliced;
            glowImage.fillCenter = false;
            glowImage.raycastTarget = false;
            glow.SetSiblingIndex(2);

            RectTransform maxRing = Child(rootRt, "MaxLevelRing");
            SetRect(maxRing, MidLeft, MidLeft, Half, new Vector2(52f, 0f), new Vector2(68f, 68f));
            Image maxRingImage = SetImage(maxRing, Spr("Px_Ring"), new Color32(0xF7, 0xC6, 0x4A, 0xFF), false);
            maxRingImage.fillCenter = false;
            maxRing.gameObject.SetActive(false);
            maxRing.SetSiblingIndex(3);

            RectTransform icon = Need(root, "Icon");
            SetRect(icon, MidLeft, MidLeft, Half, new Vector2(52f, 0f), new Vector2(48f, 48f));
            icon.GetComponent<Image>().preserveAspect = true;

            RectTransform badge = Need(root, "LevelBadge");
            SetRect(badge, BottomLeft, BottomLeft, BottomLeft, new Vector2(16f, 6f), new Vector2(40f, 22f));
            SetImage(badge, Spr("Px_Badge_Gold"), Color.white, false);
            RectTransform badgeText = Need(badge, "BadgeText");
            SetStretch(badgeText, 0f, 0f, 0f, 0f);
            StyleText(badgeText, font, 13f, UiDotPalette.Gold, TextAlignmentOptions.Center, null);

            //# 이름 줄 — BodyRow 의 HorizontalLayoutGroup 을 걷어내고 종명을 줄 전체에 둔다.
            RectTransform bodyRow = Need(root, "BodyRow");
            HorizontalLayoutGroup bodyLayout = bodyRow.GetComponent<HorizontalLayoutGroup>();
            if (bodyLayout != null)
            {
                UnityEngine.Object.DestroyImmediate(bodyLayout, true);
            }

            SetRect(bodyRow, MidLeft, new Vector2(1f, 0.5f), Half, new Vector2(40f, 16f), new Vector2(-104f, 30f));
            RectTransform species = Need(root, "SpeciesText");
            SetStretch(species, 0f, 0f, 0f, 0f);
            StyleText(species, font, 18f, Bone, TextAlignmentOptions.Left, null);

            RectTransform count = Need(root, "CountText");
            SetRect(count, MidRight, MidRight, MidRight, new Vector2(-12f, 16f), new Vector2(56f, 24f));
            StyleText(count, font, 17f, UiDotPalette.Gold, TextAlignmentOptions.Right, null);

            //# 다음 스폰 진행 바 + 남은 초.
            RectTransform barBg = Need(root, "ProgressBackground");
            SetRect(barBg, MidLeft, new Vector2(1f, 0.5f), Half, new Vector2(11f, -14f), new Vector2(-162f, 16f));
            SetImage(barBg, Spr("Px_BarBg"), Color.white, false);
            RectTransform fill = Need(root, "Fill");
            SetStretch(fill, 4f, 4f, 4f, 4f);
            Image fillImage = fill.GetComponent<Image>();
            fillImage.sprite = Spr("Px_BarFill");
            fillImage.type = Image.Type.Filled;
            fillImage.fillMethod = Image.FillMethod.Horizontal;
            fillImage.fillOrigin = 0;
            fillImage.raycastTarget = false;
            RectTransform time = Need(root, "txtSpawnTime");
            SetRect(time, MidRight, MidRight, MidRight, new Vector2(-12f, -14f), new Vector2(56f, 20f));
            StyleText(time, font, 14f, Sub, TextAlignmentOptions.Right, null);

            SetRef(root.GetComponent<SpawnerStatusCell>(), "_maxLevelRing", maxRingImage);
        }

        private static void BuildSpawnerStatusPanel(GameObject root)
        {
            RectTransform rootRt = (RectTransform)root.transform;
            SetRect(rootRt, TopLeft, TopLeft, TopLeft, new Vector2(16f, -16f), new Vector2(276f, 540f));

            //# 6셀 세로 스택 — 가로 배치(HorizontalLayoutGroup)를 세로(VerticalLayoutGroup, 간격 12)로 교체.
            RectTransform container = Need(root, "Container");
            SetStretch(container, 0f, 0f, 0f, 0f);
            HorizontalLayoutGroup horizontal = container.GetComponent<HorizontalLayoutGroup>();
            if (horizontal != null)
            {
                UnityEngine.Object.DestroyImmediate(horizontal, true);
            }

            VerticalLayoutGroup vertical = container.GetComponent<VerticalLayoutGroup>();
            if (vertical == null)
            {
                vertical = container.gameObject.AddComponent<VerticalLayoutGroup>();
            }

            vertical.spacing = 12f;
            vertical.padding = new RectOffset(0, 0, 0, 0);
            vertical.childAlignment = TextAnchor.UpperLeft;
            vertical.childControlWidth = false;
            vertical.childControlHeight = false;
            vertical.childForceExpandWidth = false;
            vertical.childForceExpandHeight = false;
        }

        //# ---------------- BuildSynergy ----------------

        private static void BuildBuildSynergyCell(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(228f, 48f);
            SetImage(rootRt, Spr("Px_PanelDark"), Color.white, false);
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            if (group == null)
            {
                group = root.AddComponent<CanvasGroup>();
            }

            RectTransform strip = Child(rootRt, "AxisStripLeft");
            SetRect(strip, BottomLeft, TopLeft, MidLeft, new Vector2(4f, 0f), new Vector2(8f, -8f));
            Image stripImage = SetImage(strip, Spr("Px_Solid"), Color.white, false);
            strip.SetSiblingIndex(0);

            RectTransform icon = Child(rootRt, "AxisIcon");
            SetRect(icon, MidLeft, MidLeft, MidLeft, new Vector2(18f, 0f), new Vector2(36f, 36f));
            Image iconImage = SetImage(icon, null, Color.white, false);
            iconImage.preserveAspect = true;
            iconImage.enabled = false;
            icon.SetSiblingIndex(1);

            RectTransform text = Need(root, "Text");
            SetStretch(text, 62f, 0f, 84f, 0f);
            StyleText(text, font, 18f, Bone, TextAlignmentOptions.Left, null);

            //# 티어 마커 3칸 — 사각 점(축색 = 도달 / 어두움 = 미도달). 색은 셀 코드가 칠한다.
            Image[] markers = new Image[3];
            for (int i = 0; i < markers.Length; ++i)
            {
                RectTransform marker = Need(root, "Marker" + i);
                SetRect(marker, MidRight, MidRight, MidRight, new Vector2(-12f - (2 - i) * 18f, 0f), new Vector2(14f, 14f));
                markers[i] = SetImage(marker, Spr("Px_Solid"), MarkerOff, false);
                marker.gameObject.SetActive(true);
            }

            BuildSynergyCell cell = root.GetComponent<BuildSynergyCell>();
            SetRef(cell, "_axisIcon", iconImage);
            SetRef(cell, "_axisStrip", stripImage);
            SetRef(cell, "_group", group);
        }

        private static void BuildBuildSynergyPanel(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;
            SetRect(rootRt, TopRight, TopRight, TopRight, new Vector2(-16f, -16f), new Vector2(228f, 252f));
            SetImage(rootRt, null, new Color(0f, 0f, 0f, 0f), true);

            RectTransform header = Child(rootRt, "HeaderRow");
            SetRect(header, TopLeft, TopRight, TopMid, Vector2.zero, new Vector2(0f, 28f));
            RectTransform title = Child(header, "HeaderTitle");
            SetRect(title, MidLeft, MidLeft, MidLeft, new Vector2(4f, 0f), new Vector2(120f, 24f));
            StyleText(title, font, 15f, Sub, TextAlignmentOptions.Left, "시너지");
            RectTransform more = Child(header, "HeaderMore");
            SetRect(more, MidRight, MidRight, MidRight, new Vector2(-4f, 0f), new Vector2(120f, 24f));
            StyleText(more, font, 14f, Sub, TextAlignmentOptions.Right, "상세 >");

            RectTransform scroll = Need(root, "ScrollView");
            SetStretch(scroll, 0f, 0f, 0f, 32f);
            Component pooling = scroll.GetComponent<BuildSynergyCardPoolingScrollView>();
            SetPooling(pooling, 1, new Vector2(0f, 8f));
        }

        //# ---------------- SkillUnlockBanner ----------------

        private static void BuildSkillUnlockBanner(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform band = Need(root, "Band");
            band.sizeDelta = new Vector2(0f, 84f);
            SetImage(band, null, new Color(0.047f, 0.02f, 0.078f, 0.92f), false);

            RectTransform top = Need(root, "AccentTop");
            SetRect(top, new Vector2(0.12f, 1f), new Vector2(0.88f, 1f), TopMid, Vector2.zero, new Vector2(0f, 4f));
            SetImage(top, Spr("Px_Solid"), Curse, false);
            RectTransform bottom = Need(root, "AccentBottom");
            SetRect(bottom, new Vector2(0.12f, 0f), new Vector2(0.88f, 0f), BottomMid, Vector2.zero, new Vector2(0f, 4f));
            SetImage(bottom, Spr("Px_Solid"), Curse, false);

            RectTransform label = Need(root, "Label");
            SetStretch(label, 200f, 0f, 200f, 0f);
            StyleText(label, font, 26f, Bone, TextAlignmentOptions.Center, null);

            RectTransform tag = Child(band, "SkillTag");
            SetRect(tag, Half, Half, Half, new Vector2(-330f, 0f), new Vector2(92f, 34f));
            Image tagImage = SetImage(tag, Spr("Px_Ring"), Curse2, false);
            tagImage.fillCenter = false;
            RectTransform tagLabel = Child(tag, "TagLabel");
            SetStretch(tagLabel, 0f, 0f, 0f, 0f);
            StyleText(tagLabel, font, 15f, Curse, TextAlignmentOptions.Center, "SKILL");
        }

        //# ---------------- BattleHud ----------------

        private static void BuildBattleHud(GameObject root)
        {
            TMP_FontAsset font = FindFont(root);
            RectTransform rootRt = (RectTransform)root.transform;

            //# 중앙 타이머 — dark 석판 + "남은 시간". 30초 이하 붉은색은 BattleHud 코드(_timerNormalColor 로 복귀색 지정).
            RectTransform plate = Child(rootRt, "TimerPlate");
            SetRect(plate, TopMid, TopMid, TopMid, new Vector2(0f, -12f), new Vector2(220f, 68f));
            SetImage(plate, Spr("Px_PanelDark"), Color.white, false);
            plate.SetSiblingIndex(0);

            RectTransform timer = Need(root, "TimerText");
            SetRect(timer, TopMid, TopMid, TopMid, new Vector2(0f, -18f), new Vector2(200f, 40f));
            StyleText(timer, font, 34f, Bone, TextAlignmentOptions.Center, null);
            RectTransform caption = Child(rootRt, "TimerCaption");
            SetRect(caption, TopMid, TopMid, TopMid, new Vector2(0f, -58f), new Vector2(200f, 16f));
            StyleText(caption, font, 12f, Sub, TextAlignmentOptions.Center, "남은 시간");

            //# 빌드 바 — 하단 중앙. 패시브(소울) 6칸 | 구분선 | 액티브(금) 3칸 최소, 넘으면 다음 줄로 늘어난다.
            RectTransform build = Need(root, "BuildPanel");
            SetRect(build, BottomMid, BottomMid, BottomMid, new Vector2(0f, 12f), new Vector2(576f, 156f));
            SetImage(build, Spr("Px_PanelDark"), Color.white, true);
            SetButtonTint(build);

            BuildSection(build, font, "PassiveSection", "패시브", UiDotPalette.Soul, true, 6);
            BuildSection(build, font, "ActiveSection", "액티브", UiDotPalette.Gold, false, 3);

            RectTransform separator = Child(build, "BuildSeparator");
            SetRect(separator, Half, Half, Half, new Vector2(90f, 0f), new Vector2(4f, 116f));
            SetImage(separator, Spr("Px_Solid"), Ink, false);

            //# origin 셀(빌드 바 스크롤뷰의 중첩 인스턴스) 크기를 52 로 맞춘다 — 풀링 스크롤뷰가 origin 크기로 배치한다.
            foreach (RectTransform cell in FindAll(rootRt, "BuildIconCell"))
            {
                cell.sizeDelta = new Vector2(52f, 52f);
            }

            //# 중첩 인스턴스 배치 — 좌상단 스포너 현황 / 우상단 시너지 / 타이머 아래 영웅 HP 바.
            RectTransform spawner = Need(root, "SpawnerStatusPanel");
            SetRect(spawner, TopLeft, TopLeft, TopLeft, new Vector2(16f, -16f), new Vector2(276f, 540f));
            RectTransform synergy = Need(root, "BuildSynergyPanel");
            SetRect(synergy, TopRight, TopRight, TopRight, new Vector2(-16f, -16f), new Vector2(228f, 252f));
            RectTransform hp = Need(root, "HeroHpBar");
            SetRect(hp, TopMid, TopMid, TopMid, new Vector2(0f, -90f), new Vector2(480f, 28f));
            hp.localScale = Vector3.one;

            SerializedObject hud = new SerializedObject(root.GetComponent<BattleHud>());
            SerializedProperty normal = hud.FindProperty("_timerNormalColor");
            if (normal == null)
            {
                Debug.LogError("[UiRedesign3Builder] 필드 없음: BattleHud._timerNormalColor");
            }
            else
            {
                normal.colorValue = Bone;
                hud.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        //# 빌드 바 한 구획 — 종류색 사각 + 라벨 + 아이콘 그리드(열 수 고정, 간격 8). 패시브는 왼쪽, 액티브는 오른쪽에 붙는다.
        private static void BuildSection(RectTransform build, TMP_FontAsset font, string sectionName, string label, Color kindColor, bool left, int columns)
        {
            RectTransform section = Need(build, sectionName);
            if (left)
            {
                SetRect(section, BottomLeft, TopLeft, MidLeft, new Vector2(12f, 0f), new Vector2(352f, -24f));
            }
            else
            {
                SetRect(section, BottomRight, TopRight, MidRight, new Vector2(-12f, 0f), new Vector2(172f, -24f));
            }

            RectTransform marker = Need(section, "Background");
            SetRect(marker, TopLeft, TopLeft, TopLeft, new Vector2(2f, -6f), new Vector2(10f, 10f));
            SetImage(marker, Spr("Px_Solid"), kindColor, false);
            RectTransform text = Need(section, "Label");
            SetRect(text, TopLeft, TopLeft, TopLeft, new Vector2(18f, -2f), new Vector2(200f, 20f));
            StyleText(text, font, 14f, Sub, TextAlignmentOptions.Left, label);

            RectTransform scroll = Need(section, "ScrollView");
            SetStretch(scroll, 0f, 0f, 0f, 24f);
            SetPooling(scroll.GetComponent<BuildIconPoolingScrollView>(), columns, new Vector2(8f, 8f));
        }

        //# ---------------- 공용 헬퍼 ----------------

        //# CHPoolingScrollView 파생의 격자 설정 — 열 수·간격·패딩 0. 필드명이 없으면 에러 로그로 드러낸다.
        private static void SetPooling(Component pooling, int columns, Vector2 gap)
        {
            if (pooling == null)
            {
                Debug.LogError("[UiRedesign3Builder] 풀링 스크롤뷰 컴포넌트 없음");
                return;
            }

            SerializedObject so = new SerializedObject(pooling);
            SerializedProperty column = so.FindProperty("_columnCount");
            SerializedProperty itemGap = so.FindProperty("_itemGap");
            if (column == null || itemGap == null)
            {
                Debug.LogError($"[UiRedesign3Builder] 풀링 필드 없음: {pooling.GetType().Name}");
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
                Debug.LogError($"[UiRedesign3Builder] 스프라이트 없음: {SpriteDir}{name}.png");
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
                Debug.LogError($"[UiRedesign3Builder] 소유 컴포넌트 없음: {field}");
                return;
            }

            SerializedObject so = new SerializedObject(owner);
            SerializedProperty property = so.FindProperty(field);
            if (property == null)
            {
                Debug.LogError($"[UiRedesign3Builder] 필드 없음: {owner.GetType().Name}.{field}");
                return;
            }

            property.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
