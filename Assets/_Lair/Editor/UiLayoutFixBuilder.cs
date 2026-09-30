using System;
using ChvjUnityInfra;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# UI 레이아웃 보정 일회용 빌더 (Rule 04 §3, 실행 후 삭제) — Galmuri 적용 후 감사(Library/ui-layout-audit.txt)에서 드러난 텍스트 박스 높이 부족·겹침·꺾임을 고친다.
    //# 기준: 텍스트 rect 높이 = ceil(fontSize×1.3)×줄 수 + 여백(4~6), 세로로 이웃한 텍스트는 4 ref 이상 간격, 짧은 단어·이름·수치는 wrap 끄고
    //# autoSizing(min/max 는 11 배수)으로 꺾임 방지, 여러 줄 설명은 lineSpacing 소폭. 폰트 크기는 11 배수 유지(비배수는 기존 값 유지).
    //# 기존 프리팹을 열어 수정하며 GUID·GameObject 이름은 유지, 멱등(같은 값을 다시 대입). origin 셀 인스턴스 크기도 함께 맞춘다.
    public static class UiLayoutFixBuilder
    {
        private const string UiDir = "Assets/_Lair/Art/UI/";

        private static readonly Vector2 Half = new Vector2(0.5f, 0.5f);
        private static readonly Vector2 TopLeft = new Vector2(0f, 1f);
        private static readonly Vector2 TopRight = new Vector2(1f, 1f);
        private static readonly Vector2 TopMid = new Vector2(0.5f, 1f);
        private static readonly Vector2 MidLeft = new Vector2(0f, 0.5f);
        private static readonly Vector2 MidRight = new Vector2(1f, 0.5f);
        private static readonly Vector2 BottomLeft = new Vector2(0f, 0f);
        private static readonly Vector2 BottomRight = new Vector2(1f, 0f);
        private static readonly Vector2 BottomMid = new Vector2(0.5f, 0f);

        [MenuItem("Lair/UI/Fix Layout")]
        public static void Build()
        {
            Process("BuildSynergyCell", FixBuildSynergyCell);
            Process("BuildSynergyPanel", FixBuildSynergyPanel);
            Process("SpawnerStatusCell", FixSpawnerStatusCell);
            Process("SpawnerStatusPanel", FixSpawnerStatusPanel);
            Process("HpBar", FixHpBar);
            Process("BattleHud", FixBattleHud);
            Process("VillageHud", FixVillageHud);
            Process("HeroSelectCell", FixHeroSelectCell);
            Process("ToastView", FixToast);
            Process("BuildModalCardCell", FixBuildModalCardCell);
            Process("BuildModalPopup", FixBuildModalPopup);
            Process("SynergyModalCell", FixSynergyModalCell);
            Process("SynergyModalPopup", FixSynergyModalPopup);
            Process("CardSelectionPopup", FixCardSelectionPopup);
            Process("QuestCell", FixQuestCell);
            Process("QuestPopup", FixQuestPopup);
            Process("LordRewardCell", FixLordRewardCell);
            Process("LordLevelPopup", FixLordLevelPopup);
            Process("ShopItemCell", FixShopItemCell);
            Process("ShopPopup", FixShopPopup);
            Process("CodexCell", FixCodexCell);
            Process("RecordsStageCell", FixRecordsStageCell);
            Process("RecordsPopup", FixRecordsPopup);
            Process("RankingCell", FixRankingCell);
            Process("RankingPopup", FixRankingPopup);
            Process("CloudPopup", FixCloudPopup);
            Process("ResultPopup", FixResultPopup);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[UiLayoutFixBuilder] 레이아웃 보정 완료 (프리팹 27종)");
        }

        private static void Process(string prefabName, Action<GameObject> fix)
        {
            string path = UiDir + prefabName + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                fix(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
                Debug.Log($"[UiLayoutFixBuilder] {prefabName} 저장");
            }
            catch (Exception e)
            {
                Debug.LogError($"[UiLayoutFixBuilder] {prefabName} 실패: {e}");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ================= 전투 HUD =================

        private static void FixBuildSynergyCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(260f, 48f);
            //# 축 이름(DEBUFF/SWARM)이 꺾이지 않게 wrap 끄고 폭에 맞춰 줄인다(22 → 최소 11).
            RectTransform text = Need(root, "Text");
            SetStretch(text, 62f, 0f, 84f, 0f);
            Opt(text, false, 0f, 11f, 22f);
        }

        private static void FixBuildSynergyPanel(GameObject root)
        {
            RectTransform rootRt = (RectTransform)root.transform;
            rootRt.sizeDelta = new Vector2(260f, 252f);
            SyncOrigin(Need(root, "ScrollView"), "BuildSynergyCell", new Vector2(260f, 48f));
        }

        private static void FixSpawnerStatusCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(276f, 88f);

            RectTransform slot = Need(root, "IconSlot");
            SetRect(slot, MidLeft, MidLeft, MidLeft, new Vector2(22f, 0f), new Vector2(64f, 64f));
            SetRect(Need(root, "Icon"), MidLeft, MidLeft, Half, new Vector2(54f, 0f), new Vector2(52f, 52f));
            SetRect(Need(root, "GlowOverlay"), MidLeft, MidLeft, Half, new Vector2(54f, 0f), new Vector2(72f, 72f));
            SetRect(Need(root, "MaxLevelRing"), MidLeft, MidLeft, Half, new Vector2(54f, 0f), new Vector2(72f, 72f));
            SetRect(Need(root, "LevelBadge"), BottomLeft, BottomLeft, BottomLeft, new Vector2(18f, 6f), new Vector2(40f, 22f));

            //# 이름 줄(y +20) / 진행 바 줄(y -20) — 두 줄 사이 8 이상. 종명은 폭에 맞춰 줄이고, ×N 은 오른쪽 64 폭.
            RectTransform body = Need(root, "BodyRow");
            SetBand(body, 96f, 80f, 20f, 32f);
            RectTransform species = Need(root, "SpeciesText");
            SetStretch(species, 0f, 0f, 0f, 0f);
            Opt(species, false, 0f, 11f, 22f);

            RectTransform count = Need(root, "CountText");
            SetRect(count, MidRight, MidRight, MidRight, new Vector2(-12f, 20f), new Vector2(64f, 32f));
            Opt(count, false, 0f, 0f, 0f);

            SetBand(Need(root, "ProgressBackground"), 96f, 96f, -20f, 16f);
            //# 남은 초는 80 폭 오른쪽 정렬 — 왼쪽이 잘리지 않게 wrap 끔.
            RectTransform time = Need(root, "txtSpawnTime");
            SetRect(time, MidRight, MidRight, MidRight, new Vector2(-12f, -20f), new Vector2(80f, 20f));
            Opt(time, false, 0f, 0f, 0f);
        }

        private static void FixSpawnerStatusPanel(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(276f, 588f);
        }

        private static void FixHpBar(GameObject root)
        {
            //# 텍스트 rect 가 6 높이로 눌려 있었다 — 바 전체를 덮게 한다(몬스터 바는 텍스트 숨김이라 영향 없음).
            RectTransform text = Need(root, "txtHp");
            SetStretch(text, 0f, 0f, 0f, 0f);
            Opt(text, false, 0f, 0f, 0f);
        }

        private static void FixBattleHud(GameObject root)
        {
            //# 타이머 — 숫자(33 → LH 43) + 캡션(11) 이 겹치지 않게 석판을 84 로 키운다.
            RectTransform plate = Need(root, "TimerPlate");
            SetRect(plate, TopMid, TopMid, TopMid, new Vector2(0f, -12f), new Vector2(220f, 84f));
            RectTransform timer = Need(root, "TimerText");
            SetRect(timer, TopMid, TopMid, TopMid, new Vector2(0f, -16f), new Vector2(200f, 48f));
            Opt(timer, false, 0f, 0f, 0f);
            RectTransform caption = Need(root, "TimerCaption");
            SetRect(caption, TopMid, TopMid, TopMid, new Vector2(0f, -68f), new Vector2(200f, 18f));
            Opt(caption, false, 0f, 0f, 0f);

            //# 중첩 인스턴스 — 스포너 588 / 시너지 260x252(+origin 셀 260x48) / 영웅 HP 바 480x32(텍스트 22).
            SetRect(Need(root, "SpawnerStatusPanel"), TopLeft, TopLeft, TopLeft, new Vector2(16f, -16f), new Vector2(276f, 588f));
            RectTransform synergy = Need(root, "BuildSynergyPanel");
            SetRect(synergy, TopRight, TopRight, TopRight, new Vector2(-16f, -16f), new Vector2(260f, 252f));
            SyncOrigin(synergy, "BuildSynergyCell", new Vector2(260f, 48f));

            RectTransform hp = Need(root, "HeroHpBar");
            SetRect(hp, TopMid, TopMid, TopMid, new Vector2(0f, -104f), new Vector2(480f, 32f));
            RectTransform hpText = FindDeep(hp, "txtHp");
            if (hpText != null)
            {
                TMP_Text tmp = hpText.GetComponent<TMP_Text>();
                tmp.enableAutoSizing = false;
                tmp.fontSize = 22f;
            }
        }

        //# ================= 마을 =================

        private static void FixVillageHud(GameObject root)
        {
            //# 상단바 — 이름(22) / 영주 Lv(11) / XP 바를 4 이상 간격으로 쌓는다.
            LeftMid(Need(root, "DisplayNameText"), new Vector2(92f, 29f), new Vector2(220f, 30f));
            Opt(Need(root, "DisplayNameText"), false, 0f, 11f, 22f);
            RectTransform level = Need(root, "LordLevelText");
            LeftMid(level, new Vector2(92f, 2f), new Vector2(220f, 18f));
            level.GetComponent<TMP_Text>().fontSize = 11f;
            Opt(level, false, 0f, 0f, 0f);
            LeftMid(Need(root, "XpBarBg"), new Vector2(92f, -24f), new Vector2(160f, 16f));

            SetRect(Need(root, "VillageNameText"), Half, Half, Half, new Vector2(0f, 14f), new Vector2(320f, 46f));
            Opt(Need(root, "VillageNameText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "VillageSubText"), Half, Half, Half, new Vector2(0f, -28f), new Vector2(320f, 18f));
            Opt(Need(root, "VillageSubText"), false, 0f, 0f, 0f);
            Opt(Need(root, "SoulText"), false, 0f, 11f, 22f);

            //# 메뉴 6종 — 라벨(22 → LH 29 + 여백)을 아이콘(48) 아래 8 위치, 높이 32 로.
            string[] menus = { "LordButton", "ShopButton", "CodexButton", "QuestButton", "RecordsButton", "RankingButton" };
            for (int i = 0; i < menus.Length; ++i)
            {
                RectTransform label = FindDeep(Need(root, menus[i]), "Label");
                SetRect(label, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 8f), new Vector2(0f, 32f));
                Opt(label, false, 0f, 11f, 22f);
            }

            //# 스테이지 카드 — 침입자(22) / STAGE(33) / 별(22) 를 6~8 간격으로.
            SetRect(Need(root, "StageIntruderLabel"), Half, Half, Half, new Vector2(0f, 216f), new Vector2(300f, 30f));
            Opt(Need(root, "StageIntruderLabel"), false, 0f, 0f, 0f);
            SetRect(Need(root, "StageIndicatorText"), Half, Half, Half, new Vector2(0f, 172f), new Vector2(400f, 46f));
            Opt(Need(root, "StageIndicatorText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "StageThreatText"), Half, Half, Half, new Vector2(0f, 126f), new Vector2(300f, 30f));
            Opt(Need(root, "StageThreatText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "StageLockLabel"), Half, Half, Half, new Vector2(0f, 14f), new Vector2(300f, 34f));
            SetRect(Need(root, "StageLockHintText"), Half, Half, Half, new Vector2(0f, -40f), new Vector2(420f, 34f));
            Opt(Need(root, "StageLockHintText"), false, 0f, 11f, 22f);

            //# 전적 패널 — 라벨(11, 높이 18) 아래 4 띄우고 값(22, 높이 32). 행 간격 64.
            string[] labels = { "RecordIntruderLabel", "RecordBestLabel", "RecordWinsLabel" };
            string[] values = { "RecordIntruderText", "RecordBestText", "RecordWinsText" };
            for (int i = 0; i < labels.Length; ++i)
            {
                RectTransform label = Need(root, labels[i]);
                SetRect(label, TopLeft, TopRight, TopLeft, new Vector2(16f, -12f - i * 64f), new Vector2(-32f, 18f));
                label.GetComponent<TMP_Text>().fontSize = 11f;
                Opt(label, false, 0f, 0f, 0f);
                RectTransform value = Need(root, values[i]);
                SetRect(value, TopLeft, TopRight, TopLeft, new Vector2(16f, -34f - i * 64f), new Vector2(-32f, 32f));
                Opt(value, false, 0f, 11f, 22f);
            }
        }

        private static void FixHeroSelectCell(GameObject root)
        {
            SetRect(Need(root, "NameText"), BottomLeft, BottomRight, BottomMid, new Vector2(0f, 34f), new Vector2(-12f, 32f));
            SetRect(Need(root, "SubText"), BottomLeft, BottomRight, BottomMid, new Vector2(0f, 8f), new Vector2(-12f, 22f));
        }

        private static void FixToast(GameObject root)
        {
            //# 22 폰트 두 줄(≈58)이 들어가게 위아래 여백 6.
            SetStretch(Need(root, "Message"), 52f, 6f, 24f, 6f);
            Opt(Need(root, "Message"), true, 3f, 0f, 0f);
        }

        //# ================= 전투 팝업 =================

        private static void FixBuildModalCardCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(280f, 80f);
            RectTransform name = Need(root, "NameText");
            SetRect(name, TopLeft, TopRight, TopLeft, new Vector2(68f, -6f), new Vector2(-120f, 30f));
            Opt(name, false, 0f, 11f, 22f);
            RectTransform count = Need(root, "CountText");
            SetRect(count, TopRight, TopRight, TopRight, new Vector2(-8f, -8f), new Vector2(44f, 26f));
            Opt(count, false, 0f, 0f, 0f);
            //# 설명은 11 폰트 2줄(≈34)까지 — 이름과 4 이상 간격.
            RectTransform desc = Need(root, "DescText");
            SetRect(desc, BottomLeft, BottomRight, BottomLeft, new Vector2(68f, 6f), new Vector2(-76f, 34f));
            Opt(desc, true, 2f, 0f, 0f);
        }

        private static void FixBuildModalPopup(GameObject root)
        {
            string[] sections = { "PassiveSection", "ActiveSection" };
            for (int i = 0; i < sections.Length; ++i)
            {
                RectTransform section = Need(root, sections[i]);
                SetRect(Need(section, "Label"), TopLeft, TopRight, TopLeft, new Vector2(18f, 0f), new Vector2(-18f, 32f));
                Opt(Need(section, "Label"), false, 0f, 0f, 0f);
                RectTransform scroll = Need(section, "ScrollView");
                SetStretch(scroll, 0f, 0f, 0f, 38f);
                SyncOrigin(scroll, "BuildModalCardCell", new Vector2(280f, 80f));
                SetStretch(Need(section, "EmptyText"), 0f, 0f, 0f, 38f);
            }
        }

        private static void FixSynergyModalCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(392f, 136f);
            LayoutElement element = root.GetComponent<LayoutElement>();
            if (element == null)
            {
                element = root.AddComponent<LayoutElement>();
            }

            element.minHeight = 136f;
            element.preferredHeight = 136f;

            RectTransform label = Need(root, "Label");
            SetRect(label, TopLeft, TopLeft, TopLeft, new Vector2(84f, -8f), new Vector2(200f, 30f));
            Opt(label, false, 0f, 11f, 22f);
            SetRect(Need(root, "TierBadgeBg"), TopRight, TopRight, TopRight, new Vector2(-14f, -8f), new Vector2(64f, 28f));
            SetRect(Need(root, "TierBadge"), TopRight, TopRight, TopRight, new Vector2(-14f, -8f), new Vector2(64f, 28f));
            //# 설명 2줄(16 → 44) / 다음 단계 2줄(11 → 36) — 이름 아래로 4~8 간격.
            RectTransform desc = Need(root, "DescText");
            SetRect(desc, TopLeft, TopRight, TopLeft, new Vector2(84f, -42f), new Vector2(-98f, 44f));
            Opt(desc, true, 3f, 0f, 0f);
            RectTransform next = Need(root, "NextText");
            SetRect(next, TopLeft, TopRight, TopLeft, new Vector2(84f, -92f), new Vector2(-98f, 36f));
            Opt(next, true, 3f, 0f, 0f);
        }

        private static void FixSynergyModalPopup(GameObject root)
        {
            SyncOrigin(Need(root, "ScrollView"), "SynergyModalCell", new Vector2(392f, 136f));
        }

        private static void FixCardSelectionPopup(GameObject root)
        {
            //# 카드 높이 460: 설명 3줄(17 + lineSpacing)을 위해 설명 영역 104, 버튼과 8 간격.
            RectTransform layout = Need(root, "CardsLayout");
            SetRect(layout, Half, Half, Half, new Vector2(0f, -40f), new Vector2(912f, 460f));
            for (int i = 0; i < 3; ++i)
            {
                RectTransform card = Need(layout, "CardView_" + i);
                card.sizeDelta = new Vector2(280f, 460f);
                RectTransform desc = Need(card, "DescText");
                SetRect(desc, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 78f), new Vector2(-36f, 104f));
                Opt(desc, true, 4f, 0f, 0f);
                Opt(Need(card, "NameText"), false, 0f, 11f, 22f);
            }

            SetRect(Need(root, "Title"), Half, Half, Half, new Vector2(0f, 258f), new Vector2(600f, 44f));
            SetRect(Need(root, "Subtitle"), Half, Half, Half, new Vector2(0f, 220f), new Vector2(600f, 30f));
            Opt(Need(root, "Subtitle"), false, 0f, 0f, 0f);
        }

        private static void FixResultPopup(GameObject root)
        {
            SetRect(Need(root, "ResultText"), Half, Half, Half, new Vector2(0f, 210f), new Vector2(560f, 80f));
            Opt(Need(root, "ResultText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "SubText"), Half, Half, Half, new Vector2(0f, 150f), new Vector2(560f, 26f));
            Opt(Need(root, "SubText"), false, 0f, 0f, 0f);
            Opt(Need(root, "RewardText"), true, 3f, 0f, 0f);
        }

        //# ================= 마을 팝업 =================

        private static void FixQuestCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(664f, 112f);
            SetRect(Need(root, "NameText"), TopLeft, TopLeft, TopLeft, new Vector2(20f, -10f), new Vector2(380f, 32f));
            Opt(Need(root, "NameText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "DescText"), TopLeft, TopLeft, TopLeft, new Vector2(20f, -50f), new Vector2(520f, 24f));
            Opt(Need(root, "DescText"), false, 0f, 11f, 16f);
            SetRect(Need(root, "ProgressRoot"), BottomLeft, BottomLeft, BottomLeft, new Vector2(20f, 12f), new Vector2(220f, 20f));
            SetRect(Need(root, "RewardText"), BottomRight, BottomRight, BottomRight, new Vector2(-20f, 10f), new Vector2(160f, 32f));
            Opt(Need(root, "RewardText"), false, 0f, 11f, 22f);
        }

        private static void FixQuestPopup(GameObject root)
        {
            SyncOrigin(Need(root, "ScrollView"), "QuestCell", new Vector2(664f, 112f));
        }

        private static void FixLordRewardCell(GameObject root)
        {
            SetRect(Need(root, "LevelText"), MidLeft, MidLeft, MidLeft, new Vector2(20f, 0f), new Vector2(80f, 32f));
            Opt(Need(root, "LevelText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "NameText"), MidLeft, MidLeft, MidLeft, new Vector2(108f, 16f), new Vector2(300f, 32f));
            Opt(Need(root, "NameText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "SubText"), MidLeft, MidLeft, MidLeft, new Vector2(108f, -20f), new Vector2(300f, 18f));
            Opt(Need(root, "SubText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "RewardText"), MidRight, MidRight, MidRight, new Vector2(-118f, 0f), new Vector2(190f, 32f));
            Opt(Need(root, "RewardText"), false, 0f, 11f, 22f);
        }

        private static void FixLordLevelPopup(GameObject root)
        {
            Opt(Need(root, "LordLevelText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "LordLevelText"), TopLeft, TopLeft, TopLeft, new Vector2(28f, -60f), new Vector2(220f, 46f));
            Opt(Need(root, "LordXpNextText"), false, 0f, 0f, 0f);
            SyncOrigin(Need(root, "ScrollView"), "LordRewardCell", new Vector2(664f, 72f));
        }

        private static void FixShopItemCell(GameObject root)
        {
            ((RectTransform)root.transform).sizeDelta = new Vector2(664f, 108f);
            //# 이름(32) → 눈금(12) → 설명 2줄(44) 을 세로로 4~6 간격 — 텍스트 좌측 여백은 셀 코드가 아이콘 유무로 옮긴다.
            SetRect(Need(root, "NameText"), TopLeft, TopLeft, TopLeft, new Vector2(96f, -6f), new Vector2(250f, 32f));
            Opt(Need(root, "NameText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "LevelPips"), TopLeft, TopLeft, TopLeft, new Vector2(96f, -44f), new Vector2(130f, 12f));
            SetRect(Need(root, "DescText"), TopLeft, TopLeft, TopLeft, new Vector2(96f, -62f), new Vector2(258f, 44f));
            Opt(Need(root, "DescText"), true, 3f, 0f, 0f);
            SetRect(Need(root, "PriceText"), MidRight, MidRight, MidRight, new Vector2(-156f, 0f), new Vector2(150f, 32f));
            Opt(Need(root, "PriceText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "HeaderText"), MidLeft, MidLeft, MidLeft, new Vector2(36f, 0f), new Vector2(400f, 34f));
        }

        private static void FixShopPopup(GameObject root)
        {
            SyncOrigin(Need(root, "ScrollView"), "ShopItemCell", new Vector2(664f, 108f));
        }

        private static void FixCodexCell(GameObject root)
        {
            RectTransform name = Need(root, "NameText");
            SetRect(name, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 6f), new Vector2(-8f, 30f));
            Opt(name, false, 0f, 11f, 17f);
        }

        private static void FixRecordsStageCell(GameObject root)
        {
            SetRect(Need(root, "StageText"), TopLeft, TopLeft, TopLeft, new Vector2(92f, -6f), new Vector2(130f, 32f));
            Opt(Need(root, "StageText"), false, 0f, 0f, 0f);
            SetRect(Need(root, "ThreatText"), TopLeft, TopLeft, TopLeft, new Vector2(92f, -42f), new Vector2(140f, 24f));
            SetRect(Need(root, "BestText"), TopRight, TopRight, TopRight, new Vector2(-12f, -6f), new Vector2(150f, 32f));
            Opt(Need(root, "BestText"), false, 0f, 11f, 22f);
            SetRect(Need(root, "WinText"), TopRight, TopRight, TopRight, new Vector2(-12f, -42f), new Vector2(90f, 24f));
            SetRect(Need(root, "RunRateText"), TopRight, TopRight, TopRight, new Vector2(-104f, -42f), new Vector2(150f, 24f));
            SetRect(Need(root, "SelectedBadge"), TopLeft, TopLeft, TopLeft, new Vector2(190f, -10f), new Vector2(76f, 30f));
        }

        private static void FixRecordsPopup(GameObject root)
        {
            RectTransform body = Need(root, "ModalBody");
            body.sizeDelta = new Vector2(480f, 560f);
            string[] tiles = { "StatTileRuns", "StatTileWins", "StatTileRate", "StatTileBest" };
            for (int i = 0; i < tiles.Length; ++i)
            {
                RectTransform tile = Need(body, tiles[i]);
                SetRect(tile, TopLeft, TopLeft, TopLeft, new Vector2(28f + i * 108f, -64f), new Vector2(98f, 72f));
                FixStatTile(tile);
            }

            RectTransform top = Need(body, "StatTileTopCard");
            SetRect(top, TopLeft, TopLeft, TopLeft, new Vector2(28f, -144f), new Vector2(424f, 72f));
            FixStatTile(top);

            RectTransform scroll = Need(root, "StageScrollView");
            SetStretch(scroll, 28f, 28f, 28f, 224f);
            SyncOrigin(scroll, "RecordsStageCell", new Vector2(440f, 84f));
        }

        //# 통계 타일 72 높이 — 라벨(11, 18) 위 6 / 값(22, 32) 아래 6 → 라벨과 값 사이 10.
        private static void FixStatTile(RectTransform tile)
        {
            RectTransform label = Need(tile, "Label");
            SetRect(label, TopLeft, TopRight, TopMid, new Vector2(0f, -6f), new Vector2(-8f, 18f));
            Opt(label, false, 0f, 0f, 0f);
            RectTransform value = Need(tile, "Value");
            SetRect(value, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 6f), new Vector2(-8f, 32f));
            Opt(value, false, 0f, 11f, 22f);
        }

        private static void FixRankingCell(GameObject root)
        {
            Opt(Need(root, "RankText"), false, 0f, 11f, 22f);
            Opt(Need(root, "NameText"), false, 0f, 11f, 22f);
            Opt(Need(root, "TimeText"), false, 0f, 11f, 22f);
            Opt(Need(root, "HeroText"), false, 0f, 11f, 16f);
        }

        private static void FixRankingPopup(GameObject root)
        {
            SyncOrigin(Need(root, "ScrollView"), "LeaderboardCell", new Vector2(648f, 52f));
        }

        private static void FixCloudPopup(GameObject root)
        {
            RectTransform conflict = Need(root, "ConflictGroup");
            SetRect(conflict, TopLeft, TopRight, TopMid, new Vector2(0f, -82f), new Vector2(-40f, 252f));
            Opt(Need(root, "ConflictText"), true, 3f, 0f, 0f);
            RectTransform compare = Need(root, "ConflictCompare");
            SetRect(compare, TopLeft, TopRight, TopMid, new Vector2(0f, -96f), new Vector2(-32f, 72f));
            string[] boxes = { "ConflictLocalBox", "ConflictCloudBox" };
            string[] values = { "ConflictLocalText", "ConflictCloudText" };
            for (int i = 0; i < boxes.Length; ++i)
            {
                RectTransform box = Need(compare, boxes[i]);
                SetRect(Need(box, "Label"), TopLeft, TopRight, TopMid, new Vector2(0f, -6f), new Vector2(-16f, 18f));
                RectTransform value = Need(box, values[i]);
                SetRect(value, BottomLeft, BottomRight, BottomMid, new Vector2(0f, 6f), new Vector2(-16f, 32f));
                Opt(value, false, 0f, 11f, 22f);
            }
        }

        //# ================= 공용 헬퍼 =================

        //# 팝업 안 ScrollView 의 origin 셀(중첩 프리팹 인스턴스)은 크기 override 를 따로 들고 있어 원본 크기 변경이 반영되지 않는다 — 직접 맞춘다.
        private static void SyncOrigin(RectTransform scope, string cellName, Vector2 size)
        {
            RectTransform cell = FindDeep(scope, cellName);
            if (cell == null)
            {
                Debug.LogWarning($"[UiLayoutFixBuilder] origin 셀 없음: {cellName}");
                return;
            }

            cell.sizeDelta = size;
        }

        //# 텍스트 옵션 — autoMin > 0 이면 자동 축소(min~max, wrap 끔). 아니면 wrap 여부와 lineSpacing 만 지정한다. 폰트 크기 자체는 건드리지 않는다.
        private static void Opt(RectTransform rt, bool wrap, float lineSpacing, float autoMin, float autoMax)
        {
            TMP_Text text = rt.GetComponent<TMP_Text>();
            if (text == null)
            {
                Debug.LogWarning($"[UiLayoutFixBuilder] TMP 없음: {rt.name}");
                return;
            }

            if (autoMin > 0f)
            {
                text.enableAutoSizing = true;
                text.fontSizeMin = autoMin;
                text.fontSizeMax = autoMax;
                wrap = false;
            }

#pragma warning disable 618
            text.enableWordWrapping = wrap;
#pragma warning restore 618
            text.lineSpacing = lineSpacing;
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
            SetRect(rt, MidLeft, MidLeft, MidLeft, pos, size);
        }

        private static void SetStretch(RectTransform rt, float left, float bottom, float right, float top)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = Half;
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        //# 가로는 부모 폭에서 좌/우 여백, 세로는 부모 중앙 기준 yCenter 에 height 인 띠.
        private static void SetBand(RectTransform rt, float left, float right, float yCenter, float height)
        {
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = Half;
            rt.offsetMin = new Vector2(left, yCenter - height * 0.5f);
            rt.offsetMax = new Vector2(-right, yCenter + height * 0.5f);
        }
    }
}
