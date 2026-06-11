using System.Collections.Generic;
using System.IO;
using ChvjUnityInfra;
using Lair.Data;
using Lair.UI;
using Lair.Village;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# v0.2 마을 허브 — MetaConfig.asset + UI 프리팹 7종(+셀 5종) + Village.unity 일괄 생성 빌더.
    //# 모든 수치는 기획서 docs/design/village-meta-hub.md 가 단일 진실 (§2.1/§3.2/§4/§5.2/§6/§8).
    public static class LairVillageBuilder
    {
        private const string PrefabDir = "Assets/_Lair/Art/UI";
        private const string MetaConfigPath = "Assets/_Lair/Data/MetaConfig.asset";
        private const string VillageScenePath = "Assets/_Lair/Scenes/Village.unity";
        private const string LoadingScenePath = "Assets/_Lair/Scenes/Loading.unity";
        private const string BattleScenePath = "Assets/_Lair/Scenes/Battle.unity";
        private const string GroundMaterialPath = "Assets/_Lair/Art/Materials/Mat_VillageGround.mat";
        private const string WallMaterialPath = "Assets/_Lair/Art/Materials/Mat_VillageWall.mat";
        private const string FloorTexturePath = "Assets/_Lair/Art/Sprites/Village_Floor.png";
        private const string WallTexturePath = "Assets/_Lair/Art/Sprites/Village_Wall.png";
        private const string ResultBackgroundSpritePath = "Assets/_Lair/Art/Sprites/Result_Screen.png";
        private const string ResourceGroup = "Resource";
        private const string ResourceLabel = "Resource";
        private const string UICanvasTag = "UICanvas";

        //# 공용 색 — 기존 UI 톤 (#1F2937 계열) 유지.
        private static readonly Color ModalDim = new Color(0f, 0f, 0f, 0.6f);
        private static readonly Color ModalBg = new Color(0.122f, 0.161f, 0.216f, 0.98f);
        private static readonly Color CellBg = new Color(0.122f, 0.161f, 0.216f, 0.95f);
        private static readonly Color YellowAccent = new Color(0.984f, 0.749f, 0.141f, 1f);
        private static readonly Color GrayLabel = new Color(0.612f, 0.639f, 0.686f, 1f);
        private static readonly Color BarBg = new Color(0.216f, 0.255f, 0.318f, 1f);
        private static readonly Color TopBarBg = new Color(0.082f, 0.082f, 0.110f, 0.85f);   //# #15151C
        private static readonly Color ButtonBg = new Color(0.216f, 0.255f, 0.318f, 1f);

        private static Sprite UISprite =>
            AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd")
            ?? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

        //# ===================================================================
        //# V0 — 전체 일괄 실행
        //# ===================================================================
        [MenuItem("Lair/Setup/V0 - Build Village All (V1+V2+V3)")]
        public static void BuildAll()
        {
            BuildMetaConfigAsset();
            BuildVillageUIPrefabs();
            BuildVillageScene();
            Debug.Log("[LairVillageBuilder] V1+V2+V3 일괄 완료 — Lair/Build Addressables 재빌드 필요");
        }

        //# ===================================================================
        //# V1 — MetaConfig.asset (기획서 §2.1 / §3.2 / §4.1 / §4.3 / §5.2 / §6 — Gate G4)
        //# ===================================================================
        [MenuItem("Lair/Setup/V1 - Build MetaConfig Asset")]
        public static void BuildMetaConfigAsset()
        {
            MetaConfig cfg = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<MetaConfig>();
                AssetDatabase.CreateAsset(cfg, MetaConfigPath);
            }

            //# 소울 보상 (§2.1)
            cfg.WinBaseSouls = 100;
            cfg.WinTimeBonusPerSec = 0.5f;
            cfg.LoseMaxSouls = 60;
            //# 영주 XP (§4.1)
            cfg.WinXp = 100;
            cfg.LoseXp = 40;
            cfg.XpPerLevelBase = 100;
            cfg.XpGrowth = 1.25f;
            //# 잠금 더미 (§6)
            cfg.HeroLockedSlots = 3;
            cfg.CodexLockedSlots = 2;

            //# 상점 7품목 (§3.2) — MaxLevel 5 / PriceGrowth 1.6 전 품목 공통.
            cfg.ShopItems.Clear();
            cfg.ShopItems.Add(ShopItem("MonsterHpUp", "강골 군세", "모든 몬스터 HP +2%/Lv", EShopEffectKind.MonsterStat, EMonsterStatKind.Hp, 1.02f, 80));
            cfg.ShopItems.Add(ShopItem("MonsterPowerUp", "흉포한 발톱", "모든 몬스터 공격력 +1.5%/Lv", EShopEffectKind.MonsterStat, EMonsterStatKind.Power, 1.015f, 100));
            cfg.ShopItems.Add(ShopItem("MonsterAtkSpeedUp", "신속한 손놀림", "모든 몬스터 공격속도 +1%/Lv", EShopEffectKind.MonsterStat, EMonsterStatKind.Cooldown, 0.99f, 100));
            cfg.ShopItems.Add(ShopItem("MonsterMoveSpeedUp", "유령 걸음", "모든 몬스터 이동속도 +1.5%/Lv", EShopEffectKind.MonsterStat, EMonsterStatKind.MoveSpeed, 1.015f, 80));
            cfg.ShopItems.Add(ShopItem("MonsterRangeUp", "긴 손아귀", "모든 몬스터 사거리 +1.5%/Lv", EShopEffectKind.MonsterStat, EMonsterStatKind.Range, 1.015f, 120));
            cfg.ShopItems.Add(ShopItem("PlagueVenomUp", "짙은 역병", "역병귀 둔화 +2%/Lv 강화", EShopEffectKind.MonsterStat, EMonsterStatKind.SlowFactor, 0.98f, 120));
            cfg.ShopItems.Add(ShopItem("SpawnerHasteUp", "깨어나는 둥지", "모든 스포너 주기 -1.5%/Lv", EShopEffectKind.SpawnerPeriod, EMonsterStatKind.Hp, 0.985f, 150));

            //# 영주 보상 트랙 Lv2~10 (§4.3) — 잠금 더미 4·6·8·10.
            cfg.LordRewards.Clear();
            cfg.LordRewards.Add(LordReward(2, false, 50, "영주의 첫 봉급"));
            cfg.LordRewards.Add(LordReward(3, false, 80, "던전 운영비"));
            cfg.LordRewards.Add(LordReward(4, true, 0, "???"));
            cfg.LordRewards.Add(LordReward(5, false, 120, "영혼 곳간"));
            cfg.LordRewards.Add(LordReward(6, true, 0, "???"));
            cfg.LordRewards.Add(LordReward(7, false, 150, "깊어진 금고"));
            cfg.LordRewards.Add(LordReward(8, true, 0, "???"));
            cfg.LordRewards.Add(LordReward(9, false, 200, "영주의 위엄"));
            cfg.LordRewards.Add(LordReward(10, true, 0, "???"));

            //# 도전과제 13개 (§5.2).
            cfg.Achievements.Clear();
            cfg.Achievements.Add(Achievement("FirstRun", "던전 개장", "첫 출격을 마친다", EAchievementCondition.TotalRuns, 1f, 20));
            cfg.Achievements.Add(Achievement("FirstWin", "첫 사냥감", "영웅을 처음으로 처치한다", EAchievementCondition.FirstWin, 1f, 30));
            cfg.Achievements.Add(Achievement("Win120", "신속한 처형", "2분 안에 영웅을 처치한다", EAchievementCondition.WinUnderSeconds, 120f, 40));
            cfg.Achievements.Add(Achievement("Win90", "전광석화", "90초 안에 영웅을 처치한다", EAchievementCondition.WinUnderSeconds, 90f, 60));
            cfg.Achievements.Add(Achievement("Win60", "찰나의 죽음", "60초 안에 영웅을 처치한다", EAchievementCondition.WinUnderSeconds, 60f, 100));
            cfg.Achievements.Add(Achievement("Wins5", "다섯 영혼", "누적 5승을 달성한다", EAchievementCondition.TotalWins, 5f, 40));
            cfg.Achievements.Add(Achievement("Wins10", "열 개의 무덤", "누적 10승을 달성한다", EAchievementCondition.TotalWins, 10f, 60));
            cfg.Achievements.Add(Achievement("Wins25", "영웅 학살자", "누적 25승을 달성한다", EAchievementCondition.TotalWins, 25f, 100));
            cfg.Achievements.Add(Achievement("Wins50", "던전의 전설", "누적 50승을 달성한다", EAchievementCondition.TotalWins, 50f, 150));
            cfg.Achievements.Add(Achievement("Runs10", "성실한 영주", "누적 10회 출격한다", EAchievementCondition.TotalRuns, 10f, 50));
            cfg.Achievements.Add(Achievement("Runs30", "끈질긴 영주", "누적 30회 출격한다", EAchievementCondition.TotalRuns, 30f, 100));
            cfg.Achievements.Add(Achievement("SynergyTier2", "빌드의 맛", "한 런에서 한 축 Tier2(5장)에 도달한다", EAchievementCondition.SynergyTierReached, 2f, 50));
            cfg.Achievements.Add(Achievement("SynergyTier3", "진성 빌드", "한 런에서 한 축 Tier3(7장)에 도달한다", EAchievementCondition.SynergyTierReached, 3f, 80));

            EditorUtility.SetDirty(cfg);
            AssetDatabase.SaveAssets();
            Debug.Log("[LairVillageBuilder] V1 — MetaConfig.asset 생성/갱신 완료 (상점 7 / 영주 9 / 도전과제 13)");
        }

        private static ShopItemDef ShopItem(string id, string name, string desc, EShopEffectKind kind, EMonsterStatKind stat, float perLevelMul, int basePrice)
            => new ShopItemDef
            {
                Id = id,
                DisplayName = name,
                Description = desc,
                EffectKind = kind,
                StatKind = stat,
                PerLevelMul = perLevelMul,
                MaxLevel = 5,
                BasePrice = basePrice,
                PriceGrowth = 1.6f,
            };

        private static LordRewardDef LordReward(int level, bool locked, int souls, string name)
            => new LordRewardDef { Level = level, IsLockedDummy = locked, RewardSouls = souls, DisplayName = name };

        private static AchievementDef Achievement(string id, string name, string desc, EAchievementCondition cond, float threshold, int souls)
            => new AchievementDef { Id = id, DisplayName = name, Description = desc, Condition = cond, Threshold = threshold, RewardSouls = souls };

        //# ===================================================================
        //# V2 — UI 프리팹 7종 + 셀 5종 + ResultPopup 메타 노드(_rewardText/2버튼) (Rule 03 §2/§3)
        //# ===================================================================
        [MenuItem("Lair/Setup/V2 - Build Village UI Prefabs")]
        public static void BuildVillageUIPrefabs()
        {
            EnsureDir(PrefabDir);
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[LairVillageBuilder] Addressables 미설정");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(ResourceGroup);
            if (group == null)
            {
                Debug.LogError($"[LairVillageBuilder] Addressables '{ResourceGroup}' 그룹 미발견");
                return;
            }

            //# 1) 셀 — 팝업이 origin 으로 nesting 하므로 선행.
            BuildShopItemCellPrefab();
            BuildQuestCellPrefab();
            BuildLordRewardCellPrefab();
            BuildHeroSelectCellPrefab();
            BuildCodexCellPrefab();

            //# 2) 팝업 7종 — Addressable 등록 (주소 = 파일명 = EUI 값명).
            BuildVillageHudPrefab(settings, group);
            BuildShopPopupPrefab(settings, group);
            BuildQuestPopupPrefab(settings, group);
            BuildCodexPopupPrefab(settings, group);
            BuildRecordsPopupPrefab(settings, group);
            BuildHeroSelectPopupPrefab(settings, group);
            BuildLordLevelPopupPrefab(settings, group);

            //# 3) ResultPopup — 보상 텍스트 + 다시 도전/마을로 2버튼 주입 (기획서 §9).
            InjectResultPopupMetaNodes();

            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[LairVillageBuilder] V2 — 마을 UI 프리팹 빌드 완료 (팝업 7 + 셀 5 + ResultPopup 갱신)");
        }

        //# ---------- ShopItemCell ----------
        private static void BuildShopItemCellPrefab()
        {
            GameObject root = NewCellRoot("ShopItemCell", new Vector2(660f, 84f), out Image bg);

            CHText nameText = AddTmp(root.transform, "NameText", "품목명", 16f, TextAlignmentOptions.Left, Color.white);
            SetRect(nameText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -10f), new Vector2(290f, 24f));

            CHText levelText = AddTmp(root.transform, "LevelText", "Lv 0/5", 13f, TextAlignmentOptions.Left, YellowAccent);
            SetRect(levelText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(312f, -12f), new Vector2(90f, 22f));

            CHText descText = AddTmp(root.transform, "DescText", "효과 설명", 12f, TextAlignmentOptions.Left, GrayLabel);
            SetRect(descText.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 10f), new Vector2(380f, 20f));

            CHText priceText = AddTmp(root.transform, "PriceText", "0 소울", 14f, TextAlignmentOptions.Right, Color.white);
            SetRect(priceText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-140f, 0f), new Vector2(130f, 24f));

            CHButton buyButton = AddButton(root.transform, "BuyButton", ButtonBg, out GameObject buyGo);
            SetRect(buyGo.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(110f, 44f));
            //# 상태 치환 라벨(구매/만렙/소울 부족)은 stringID 미지정 — _stringID 가 박히면 SetText 가
            //# string.Format(GetString(id), args) 로 동작해 인자가 버려진다 (CHText 충돌, 코드 조립 일원화).
            CHText buyLabel = AddTmp(buyGo.transform, "Label", "구매", 14f, TextAlignmentOptions.Center, YellowAccent);
            FullStretch(buyLabel.transform);

            ShopItemCell cell = root.AddComponent<ShopItemCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObj(so, "_nameText", nameText);
            SetObj(so, "_levelText", levelText);
            SetObj(so, "_descText", descText);
            SetObj(so, "_priceText", priceText);
            SetObj(so, "_buyButton", buyButton);
            SetObj(so, "_buyLabel", buyLabel);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCellPrefab(root, "ShopItemCell");
        }

        //# ---------- QuestCell ----------
        private static void BuildQuestCellPrefab()
        {
            GameObject root = NewCellRoot("QuestCell", new Vector2(660f, 64f), out Image bg);

            CHText nameText = AddTmp(root.transform, "NameText", "과제명", 14f, TextAlignmentOptions.Left, Color.white);
            SetRect(nameText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -8f), new Vector2(260f, 22f));

            CHText descText = AddTmp(root.transform, "DescText", "설명", 11f, TextAlignmentOptions.Left, GrayLabel);
            SetRect(descText.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(16f, 8f), new Vector2(380f, 18f));

            CHText rewardText = AddTmp(root.transform, "RewardText", "+0 소울", 13f, TextAlignmentOptions.Right, Color.white);
            SetRect(rewardText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-100f, 0f), new Vector2(110f, 22f));

            //# 셀이 SetText("달성") 로 갱신하는 라벨 — stringID 미지정 (buyLabel 과 동일한 충돌 차단).
            CHText badge = AddTmp(root.transform, "AchievedBadge", "달성", 14f, TextAlignmentOptions.Center, YellowAccent);
            SetRect(badge.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(70f, 24f));

            QuestCell cell = root.AddComponent<QuestCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObj(so, "_background", bg);
            SetObj(so, "_nameText", nameText);
            SetObj(so, "_descText", descText);
            SetObj(so, "_rewardText", rewardText);
            SetObj(so, "_achievedBadge", badge);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCellPrefab(root, "QuestCell");
        }

        //# ---------- LordRewardCell ----------
        private static void BuildLordRewardCellPrefab()
        {
            GameObject root = NewCellRoot("LordRewardCell", new Vector2(660f, 56f), out Image bg);

            CHText levelText = AddTmp(root.transform, "LevelText", "Lv 2", 15f, TextAlignmentOptions.Left, YellowAccent);
            SetRect(levelText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(70f, 24f));

            CHText nameText = AddTmp(root.transform, "NameText", "보상명", 14f, TextAlignmentOptions.Left, Color.white);
            SetRect(nameText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(96f, 0f), new Vector2(320f, 24f));

            CHText rewardText = AddTmp(root.transform, "RewardText", "+0 소울", 13f, TextAlignmentOptions.Right, Color.white);
            SetRect(rewardText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-100f, 0f), new Vector2(110f, 22f));

            //# 셀이 SetText("달성") 로 갱신하는 라벨 — stringID 미지정 (buyLabel 과 동일한 충돌 차단).
            CHText badge = AddTmp(root.transform, "ReachedBadge", "달성", 14f, TextAlignmentOptions.Center, YellowAccent);
            SetRect(badge.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 0f), new Vector2(70f, 24f));

            LordRewardCell cell = root.AddComponent<LordRewardCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObj(so, "_background", bg);
            SetObj(so, "_levelText", levelText);
            SetObj(so, "_nameText", nameText);
            SetObj(so, "_rewardText", rewardText);
            SetObj(so, "_reachedBadge", badge);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCellPrefab(root, "LordRewardCell");
        }

        //# ---------- HeroSelectCell ----------
        private static void BuildHeroSelectCellPrefab()
        {
            GameObject root = NewCellRoot("HeroSelectCell", new Vector2(150f, 170f), out Image bg);
            //# 셀 전체가 선택 버튼 — root 에 Button + CHButton.
            Button btn = root.AddComponent<Button>();
            btn.targetGraphic = bg;
            CHButton chBtn = root.AddComponent<CHButton>();

            GameObject borderGo = new GameObject("Border", typeof(RectTransform), typeof(Image));
            borderGo.transform.SetParent(root.transform, false);
            FullStretch(borderGo.transform);
            Image borderImg = borderGo.GetComponent<Image>();
            borderImg.sprite = UISprite;
            borderImg.type = Image.Type.Sliced;
            borderImg.fillCenter = false;
            borderImg.color = new Color(0f, 0f, 0f, 0f);
            borderImg.raycastTarget = false;

            CHText nameText = AddTmp(root.transform, "NameText", "Knight", 14f, TextAlignmentOptions.Center, Color.white);
            SetRect(nameText.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(0f, 24f));

            HeroSelectCell cell = root.AddComponent<HeroSelectCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObj(so, "_background", bg);
            SetObj(so, "_border", borderImg);
            SetObj(so, "_nameText", nameText);
            SetObj(so, "_selectButton", chBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCellPrefab(root, "HeroSelectCell");
        }

        //# ---------- CodexCell ----------
        private static void BuildCodexCellPrefab()
        {
            GameObject root = NewCellRoot("CodexCell", new Vector2(106f, 128f), out Image bg);

            GameObject iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGo.transform.SetParent(root.transform, false);
            SetRect(iconGo.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(84f, 84f));
            Image iconImg = iconGo.GetComponent<Image>();
            iconImg.sprite = UISprite;
            iconImg.preserveAspect = true;
            iconImg.raycastTarget = false;

            CHText nameText = AddTmp(root.transform, "NameText", "???", 11f, TextAlignmentOptions.Center, Color.white);
            SetRect(nameText.transform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(0f, 22f));

            CodexCell cell = root.AddComponent<CodexCell>();
            SerializedObject so = new SerializedObject(cell);
            SetObj(so, "_background", bg);
            SetObj(so, "_icon", iconImg);
            SetObj(so, "_nameText", nameText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveCellPrefab(root, "CodexCell");
        }

        //# ---------- VillageHud ----------
        private static void BuildVillageHudPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = new GameObject("VillageHud", typeof(RectTransform));
            FullStretch(root.transform);
            VillageHud hud = root.AddComponent<VillageHud>();

            //# 상단 바 — 좌 소울 / 중 마을 이름 / 우 영주 Lv + XP 게이지 (기획서 §8.4).
            GameObject topBar = new GameObject("TopBar", typeof(RectTransform), typeof(Image));
            topBar.transform.SetParent(root.transform, false);
            RectTransform topRt = (RectTransform)topBar.transform;
            topRt.anchorMin = new Vector2(0f, 1f);
            topRt.anchorMax = new Vector2(1f, 1f);
            topRt.pivot = new Vector2(0.5f, 1f);
            topRt.anchoredPosition = Vector2.zero;
            topRt.sizeDelta = new Vector2(0f, 48f);
            Image topImg = topBar.GetComponent<Image>();
            topImg.sprite = UISprite;
            topImg.type = Image.Type.Sliced;
            topImg.color = TopBarBg;

            CHText soulText = AddTmp(topBar.transform, "SoulText", "0 소울", 16f, TextAlignmentOptions.Left, YellowAccent);
            SetRect(soulText.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(16f, 0f), new Vector2(240f, 28f));

            CHText nameText = AddTmp(topBar.transform, "VillageNameText", "영주의 둥지", 18f, TextAlignmentOptions.Center, Color.white, stringId: 7);
            SetRect(nameText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280f, 28f));

            CHText lordText = AddTmp(topBar.transform, "LordLevelText", "영주 Lv 1", 14f, TextAlignmentOptions.Right, Color.white);
            SetRect(lordText.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, 8f), new Vector2(160f, 22f));

            GameObject xpBg = new GameObject("XpBarBg", typeof(RectTransform), typeof(Image));
            xpBg.transform.SetParent(topBar.transform, false);
            SetRect(xpBg.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-16f, -12f), new Vector2(160f, 8f));
            Image xpBgImg = xpBg.GetComponent<Image>();
            xpBgImg.sprite = UISprite;
            xpBgImg.type = Image.Type.Sliced;
            xpBgImg.color = BarBg;

            GameObject xpFill = new GameObject("XpBarFill", typeof(RectTransform), typeof(Image));
            xpFill.transform.SetParent(xpBg.transform, false);
            FullStretch(xpFill.transform);
            Image xpFillImg = xpFill.GetComponent<Image>();
            xpFillImg.sprite = UISprite;
            xpFillImg.type = Image.Type.Filled;
            xpFillImg.fillMethod = Image.FillMethod.Horizontal;
            xpFillImg.fillAmount = 0f;
            xpFillImg.color = YellowAccent;
            xpFillImg.raycastTarget = false;

            //# 좌측 메뉴 — 상점/도감/기록 (id 8/9/10). 우측 — 영웅/퀘스트/영주성 (id 11/12/13).
            CHButton shopBtn = AddMenuButton(root.transform, "ShopButton", "상점", 8, left: true, slot: 0);
            CHButton codexBtn = AddMenuButton(root.transform, "CodexButton", "도감", 9, left: true, slot: 1);
            CHButton recordsBtn = AddMenuButton(root.transform, "RecordsButton", "기록", 10, left: true, slot: 2);
            CHButton heroBtn = AddMenuButton(root.transform, "HeroButton", "영웅", 11, left: false, slot: 0);
            CHButton questBtn = AddMenuButton(root.transform, "QuestButton", "퀘스트", 12, left: false, slot: 1);
            CHButton lordBtn = AddMenuButton(root.transform, "LordButton", "영주성", 13, left: false, slot: 2);

            //# 하단 출격 — 마을에서 가장 큰 단일 버튼 (기획서 §8.4 위계).
            CHButton sortieBtn = AddButton(root.transform, "SortieButton", YellowAccent, out GameObject sortieGo);
            SetRect(sortieGo.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(280f, 80f));
            CHText sortieLabel = AddTmp(sortieGo.transform, "Label", "출격", 30f, TextAlignmentOptions.Center, Color.black, stringId: 14);
            FullStretch(sortieLabel.transform);

            SerializedObject so = new SerializedObject(hud);
            SetObj(so, "_soulText", soulText);
            SetObj(so, "_lordLevelText", lordText);
            SetObj(so, "_lordXpFill", xpFillImg);
            SetObj(so, "_shopButton", shopBtn);
            SetObj(so, "_codexButton", codexBtn);
            SetObj(so, "_recordsButton", recordsBtn);
            SetObj(so, "_heroButton", heroBtn);
            SetObj(so, "_questButton", questBtn);
            SetObj(so, "_lordButton", lordBtn);
            SetObj(so, "_sortieButton", sortieBtn);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "VillageHud", settings, group);
        }

        private static CHButton AddMenuButton(Transform parent, string name, string label, int stringId, bool left, int slot)
        {
            CHButton btn = AddButton(parent, name, ButtonBg, out GameObject go);
            float x = left ? 16f : -16f;
            Vector2 anchor = left ? new Vector2(0f, 0.5f) : new Vector2(1f, 0.5f);
            float y = 70f - slot * 70f;   //# 슬롯 0/1/2 → +70 / 0 / -70
            SetRect(go.transform, anchor, anchor, anchor, new Vector2(x, y), new Vector2(120f, 52f));
            CHText labelText = AddTmp(go.transform, "Label", label, 16f, TextAlignmentOptions.Center, Color.white, stringId: stringId);
            FullStretch(labelText.transform);
            return btn;
        }

        //# ---------- ShopPopup ----------
        private static void BuildShopPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("ShopPopup", "상점", 8, out GameObject body, out CHButton dim, out CHButton close);
            ShopPopup popup = root.AddComponent<ShopPopup>();

            CHText soulText = AddTmp(body.transform, "SoulText", "0 소울", 14f, TextAlignmentOptions.Right, YellowAccent);
            SetRect(soulText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -14f), new Vector2(200f, 24f));

            ShopItemPoolingScrollView scroll = AddScrollView<ShopItemPoolingScrollView>(
                body.transform, "ShopItemCell", columnCount: 1, itemGap: new Vector2(0f, 6f));

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_soulText", soulText);
            SetObj(so, "_scrollView", scroll);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "ShopPopup", settings, group);
        }

        //# ---------- QuestPopup ----------
        private static void BuildQuestPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("QuestPopup", "도전과제", 16, out GameObject body, out CHButton dim, out CHButton close);
            QuestPopup popup = root.AddComponent<QuestPopup>();

            QuestPoolingScrollView scroll = AddScrollView<QuestPoolingScrollView>(
                body.transform, "QuestCell", columnCount: 1, itemGap: new Vector2(0f, 6f));

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_scrollView", scroll);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "QuestPopup", settings, group);
        }

        //# ---------- CodexPopup ----------
        private static void BuildCodexPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("CodexPopup", "도감", 9, out GameObject body, out CHButton dim, out CHButton close);
            CodexPopup popup = root.AddComponent<CodexPopup>();

            //# 탭 2개 — 몬스터(id 29) / 카드(id 30). Toggle + CHToggle (Rule 03 §3).
            GameObject tabRow = new GameObject("TabRow", typeof(RectTransform), typeof(ToggleGroup));
            tabRow.transform.SetParent(body.transform, false);
            SetRect(tabRow.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -44f), new Vector2(260f, 32f));
            ToggleGroup tabGroup = tabRow.GetComponent<ToggleGroup>();

            Toggle monsterTab = AddTabToggle(tabRow.transform, "MonsterTab", "몬스터", 29, 0f, tabGroup, isOn: true);
            Toggle cardTab = AddTabToggle(tabRow.transform, "CardTab", "카드", 30, 130f, tabGroup, isOn: false);

            //# 스크롤뷰 2개 — 몬스터 4열 / 카드 6열 (기획서 §6). 셀 origin sizeDelta 만 다르게 override.
            CodexPoolingScrollView monsterScroll = AddScrollView<CodexPoolingScrollView>(
                body.transform, "CodexCell", columnCount: 4, itemGap: new Vector2(8f, 8f),
                scrollName: "MonsterScrollView", topOffset: -84f, originSize: new Vector2(158f, 150f));
            CodexPoolingScrollView cardScroll = AddScrollView<CodexPoolingScrollView>(
                body.transform, "CodexCell", columnCount: 6, itemGap: new Vector2(6f, 6f),
                scrollName: "CardScrollView", topOffset: -84f);
            cardScroll.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_monsterTab", monsterTab);
            SetObj(so, "_cardTab", cardTab);
            SetObj(so, "_monsterScrollView", monsterScroll);
            SetObj(so, "_cardScrollView", cardScroll);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "CodexPopup", settings, group);
        }

        private static Toggle AddTabToggle(Transform parent, string name, string label, int stringId, float x, ToggleGroup tabGroup, bool isOn)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Toggle));
            go.transform.SetParent(parent, false);
            SetRect(go.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(x, 0f), new Vector2(120f, 32f));
            Image bg = go.GetComponent<Image>();
            bg.sprite = UISprite;
            bg.type = Image.Type.Sliced;
            bg.color = ButtonBg;

            GameObject checkGo = new GameObject("Checkmark", typeof(RectTransform), typeof(Image));
            checkGo.transform.SetParent(go.transform, false);
            FullStretch(checkGo.transform);
            Image check = checkGo.GetComponent<Image>();
            check.sprite = UISprite;
            check.type = Image.Type.Sliced;
            check.color = new Color(YellowAccent.r, YellowAccent.g, YellowAccent.b, 0.35f);
            check.raycastTarget = false;

            CHText labelText = AddTmp(go.transform, "Label", label, 14f, TextAlignmentOptions.Center, Color.white, stringId: stringId);
            FullStretch(labelText.transform);

            Toggle toggle = go.GetComponent<Toggle>();
            toggle.targetGraphic = bg;
            toggle.graphic = check;
            toggle.group = tabGroup;
            toggle.isOn = isOn;
            go.AddComponent<CHToggle>();
            return toggle;
        }

        //# ---------- RecordsPopup ----------
        private static void BuildRecordsPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("RecordsPopup", "기록", 10, out GameObject body, out CHButton dim, out CHButton close, bodySize: new Vector2(480f, 360f));
            RecordsPopup popup = root.AddComponent<RecordsPopup>();

            CHText bodyText = AddTmp(body.transform, "BodyText", "총 출격  0\n승리  0\n승률  0%\n최단 클리어  -", 20f, TextAlignmentOptions.Left, Color.white);
            SetRect(bodyText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(360f, 220f));
            TMP_Text bodyTmp = bodyText.GetComponent<TMP_Text>();
            bodyTmp.lineSpacing = 24f;

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_bodyText", bodyText);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "RecordsPopup", settings, group);
        }

        //# ---------- HeroSelectPopup ----------
        private static void BuildHeroSelectPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("HeroSelectPopup", "영웅 선택", 15, out GameObject body, out CHButton dim, out CHButton close);
            HeroSelectPopup popup = root.AddComponent<HeroSelectPopup>();

            HeroSelectPoolingScrollView scroll = AddScrollView<HeroSelectPoolingScrollView>(
                body.transform, "HeroSelectCell", columnCount: 4, itemGap: new Vector2(12f, 12f));

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_scrollView", scroll);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "HeroSelectPopup", settings, group);
        }

        //# ---------- LordLevelPopup ----------
        private static void BuildLordLevelPopupPrefab(AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            GameObject root = BuildModalSkeleton("LordLevelPopup", "영주성", 13, out GameObject body, out CHButton dim, out CHButton close);
            LordLevelPopup popup = root.AddComponent<LordLevelPopup>();

            CHText lordLevelText = AddTmp(body.transform, "LordLevelText", "영주 Lv 1", 14f, TextAlignmentOptions.Right, YellowAccent);
            SetRect(lordLevelText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -14f), new Vector2(200f, 24f));

            LordRewardPoolingScrollView scroll = AddScrollView<LordRewardPoolingScrollView>(
                body.transform, "LordRewardCell", columnCount: 1, itemGap: new Vector2(0f, 6f));

            SerializedObject so = new SerializedObject(popup);
            SetObj(so, "_dimButton", dim);
            SetObj(so, "_closeButton", close);
            SetObj(so, "_lordLevelText", lordLevelText);
            SetObj(so, "_scrollView", scroll);
            so.ApplyModifiedPropertiesWithoutUndo();

            SaveAndRegisterPrefab(root, "LordLevelPopup", settings, group);
        }

        //# ---------- ResultPopup — _rewardText + 다시 도전/마을로 2버튼 주입 (기획서 §9) ----------
        //# ResultPopup.prefab 원본은 빌더 생성물이 아니므로 노드 단위 주입으로만 갱신 (수작업 클로버 방지). 멱등.
        [MenuItem("Lair/Setup/V2a - Update ResultPopup Meta Nodes")]
        public static void InjectResultPopupMetaNodes()
        {
            string path = $"{PrefabDir}/ResultPopup.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null)
            {
                Debug.LogError("[LairVillageBuilder] ResultPopup.prefab 미발견 — 메타 노드 주입 스킵");
                return;
            }

            try
            {
                ResultPopup popup = contents.GetComponent<ResultPopup>();
                if (popup == null)
                {
                    Debug.LogError("[LairVillageBuilder] ResultPopup 컴포넌트 미발견");
                    return;
                }

                //# 배경 — Result_Screen 스프라이트 full-stretch (Dim 위·콘텐츠 아래). 멱등.
                EnsureResultPopupBackground(contents.transform);

                Transform existing = contents.transform.Find("RewardText");
                CHText rewardText;
                if (existing != null)
                {
                    rewardText = existing.GetComponent<CHText>();
                }
                else
                {
                    rewardText = AddTmp(contents.transform, "RewardText", string.Empty, 22f, TextAlignmentOptions.Center, Color.white);
                    SetRect(rewardText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(640f, 150f));
                }

                //# 구 단일 버튼(RestartButton) 제거 — 다시 도전/마을로 2버튼으로 대체.
                Transform legacy = contents.transform.Find("RestartButton");
                if (legacy != null)
                {
                    Object.DestroyImmediate(legacy.gameObject);
                }

                //# 정적 라벨 — Strings_Ko.json id 바인딩 (기획서 §7 rev 5 ①표: 34 다시 도전 / 35 마을로).
                CHButton retryButton = EnsureResultPopupButton(contents.transform, "RetryButton", new Vector2(-130f, -120f), 34, "다시 도전");
                CHButton villageButton = EnsureResultPopupButton(contents.transform, "VillageButton", new Vector2(130f, -120f), 35, "마을로");

                SerializedObject so = new SerializedObject(popup);
                SetObj(so, "_rewardText", rewardText);
                SetObj(so, "_retryButton", retryButton);
                SetObj(so, "_villageButton", villageButton);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[LairVillageBuilder] ResultPopup.prefab — _rewardText + 2버튼(RetryButton/VillageButton) 주입 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        //# 기존 Background 노드가 있으면 재사용 (멱등) — sibling 1(Dim 위·콘텐츠 아래) 고정 + 스프라이트 재주입.
        private static void EnsureResultPopupBackground(Transform root)
        {
            Transform found = root.Find("Background");
            Image image;
            if (found != null)
            {
                image = found.GetComponent<Image>();
            }
            else
            {
                GameObject backgroundGo = new GameObject("Background", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                backgroundGo.transform.SetParent(root, false);
                FullStretch(backgroundGo.transform);
                image = backgroundGo.GetComponent<Image>();
                image.raycastTarget = false;
            }

            image.transform.SetSiblingIndex(1);

            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ResultBackgroundSpritePath);
            if (sprite == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 배경 스프라이트 미발견: {ResultBackgroundSpritePath}");
                return;
            }
            image.sprite = sprite;
        }

        //# 기존 노드가 있으면 그대로 재사용 (멱등) — 없을 때만 흰 배경 + 검정 라벨(구 RestartButton 시각 동일) 생성.
        private static CHButton EnsureResultPopupButton(Transform root, string name, Vector2 anchoredPos, int labelStringId, string labelFallback)
        {
            Transform found = root.Find(name);
            if (found != null)
            {
                return found.GetComponent<CHButton>();
            }

            CHButton button = AddButton(root, name, Color.white, out GameObject buttonGo);
            SetRect(buttonGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), anchoredPos, new Vector2(240f, 80f));

            CHText label = AddTmp(buttonGo.transform, "ButtonText", labelFallback, 36f, TextAlignmentOptions.Center, Color.black, labelStringId);
            FullStretch(label.transform);
            return button;
        }

        //# ===================================================================
        //# V3 — Village.unity 씬 + Build Settings + Battle 씬 MetaConfig 와이어링
        //# ===================================================================
        [MenuItem("Lair/Setup/V3 - Build Village Scene")]
        public static void BuildVillageScene()
        {
            if (AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath) == null)
            {
                Debug.LogError("[LairVillageBuilder] MetaConfig.asset 미발견 — V1 먼저 실행");
                return;
            }

            EnsureTagExists(UICanvasTag);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            //# 씬 전환(NewScene/OpenScene)이 미사용 에셋을 unload 하므로 SO 참조는 전환 "후" 로드해야 한다.
            //# 전환 전 로드 참조를 쓰면 destroyed 객체가 silent-null 직렬화됨 ({fileID: 0} — 본 빌더 1차 실행에서 확인).
            MetaConfig metaConfig = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);

            //# 카메라 — 22° 로우앵글 근접 쇼케이스 (기획서 §8.2 — plan 의 45° 를 대체).
            GameObject camGo = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2.6f, -3.8f);
            camGo.transform.rotation = Quaternion.Euler(22f, 0f, 0f);
            Camera cam = camGo.GetComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.082f, 0.082f, 0.110f, 1f);   //# #15151C
            cam.fieldOfView = 40f;

            //# 라이트 (기획서 §8.3).
            GameObject dirLightGo = new GameObject("Directional Light", typeof(Light));
            Light dirLight = dirLightGo.GetComponent<Light>();
            dirLight.type = LightType.Directional;
            dirLight.intensity = 0.85f;
            dirLight.color = Color.white;
            dirLightGo.transform.rotation = Quaternion.Euler(45f, -30f, 0f);

            GameObject pointLightGo = new GameObject("Point Light", typeof(Light));
            Light pointLight = pointLightGo.GetComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = new Color(0.541f, 0.482f, 1f, 1f);   //# #8A7BFF — 영혼 테마 림 라이트
            pointLight.intensity = 12f;
            pointLight.range = 6f;
            pointLightGo.transform.position = new Vector3(0f, 2.5f, -1.5f);

            //# 바닥 — Plane 10×10 + Village_Floor 텍스처 (미발견 시 구 단색 #23232B fallback).
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.position = Vector3.zero;
            ground.GetComponent<MeshRenderer>().sharedMaterial = EnsureGroundMaterial();

            //# 마을 외벽 — Village_Wall 텍스처 Quad 3면 (후/좌/우).
            BuildVillageWalls();

            //# 영웅 앵커 (기획서 §8.1 — 위치 0,0,0 / 회전은 VillageController 가 Y180 적용).
            GameObject anchor = new GameObject("HeroAnchor");
            anchor.transform.position = Vector3.zero;

            //# UICanvas — CHMUI 가 Tag 로 탐색 (기존 Battle 씬과 동일 정책).
            BuildUICanvas();
            BuildEventSystem();

            //# VillageController 와이어링.
            GameObject controllerGo = new GameObject("VillageController");
            VillageController controller = controllerGo.AddComponent<VillageController>();
            SerializedObject so = new SerializedObject(controller);
            SetObj(so, "_metaConfig", metaConfig);
            SetObj(so, "_heroAnchor", anchor.transform);
            so.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, VillageScenePath);
            if (VerifySceneHasMetaConfig(VillageScenePath) == false)
            {
                Debug.LogError("[LairVillageBuilder] Village 씬 _metaConfig 직렬화 검증 실패 — 수동 연결 필요");
            }
            Debug.Log($"[LairVillageBuilder] V3 — {VillageScenePath} 생성 완료");

            RegisterBuildScenes();
            WireBattleSceneMetaConfig();
        }

        //# 저장된 씬 파일에 MetaConfig.asset guid 참조가 실제로 박혔는지 디스크 레벨 검증 (silent-null 재발 방지).
        private static bool VerifySceneHasMetaConfig(string scenePath)
        {
            string guid = AssetDatabase.AssetPathToGUID(MetaConfigPath);
            if (string.IsNullOrEmpty(guid))
                return false;
            return File.ReadAllText(scenePath).Contains(guid);
        }

        private static Material EnsureGroundMaterial()
        {
            Material mat = LoadOrCreateMaterial(GroundMaterialPath);
            Texture2D floorTex = AssetDatabase.LoadAssetAtPath<Texture2D>(FloorTexturePath);
            if (floorTex != null)
            {
                //# 중앙 포탈 1장 디자인 — 반복 타일링 부적합. 정사각 Plane 에 종횡비 보존 중앙 크롭 매핑.
                float cropU = Mathf.Min(1f, (float)floorTex.height / floorTex.width);
                mat.SetTexture("_BaseMap", floorTex);
                mat.SetTextureScale("_BaseMap", new Vector2(cropU, 1f));
                mat.SetTextureOffset("_BaseMap", new Vector2((1f - cropU) * 0.5f, 0f));
                mat.SetColor("_BaseColor", Color.white);
            }
            else
            {
                //# 텍스처 미발견 fallback — #23232B 단색 (구 기획서 §8.3 더미 바닥).
                mat.SetColor("_BaseColor", new Color(0.137f, 0.137f, 0.169f, 1f));
            }
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material EnsureWallMaterial()
        {
            Material mat = LoadOrCreateMaterial(WallMaterialPath);
            Texture2D wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WallTexturePath);
            if (wallTex != null)
            {
                mat.SetTexture("_BaseMap", wallTex);
                mat.SetColor("_BaseColor", Color.white);
            }
            //# 성문 외곽 알파 투과 — URP Lit Transparent 전환.
            mat.SetFloat("_Surface", 1f);
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material LoadOrCreateMaterial(string path)
        {
            Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null)
                return mat;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        //# 바닥(10×10) 가장자리 3면 외벽 — 텍스처 종횡비 보존 높이, 그림자 캐스팅 차단(투명 Quad 전면 그림자 방지).
        private static void BuildVillageWalls()
        {
            Material wallMat = EnsureWallMaterial();
            Texture2D wallTex = AssetDatabase.LoadAssetAtPath<Texture2D>(WallTexturePath);
            float height = wallTex != null ? 10f * wallTex.height / wallTex.width : 3f;

            CreateWallQuad("Wall_Back", new Vector3(0f, height * 0.5f, 5f), Quaternion.identity, wallMat, height);
            CreateWallQuad("Wall_Left", new Vector3(-5f, height * 0.5f, 0f), Quaternion.Euler(0f, -90f, 0f), wallMat, height);
            CreateWallQuad("Wall_Right", new Vector3(5f, height * 0.5f, 0f), Quaternion.Euler(0f, 90f, 0f), wallMat, height);
        }

        private static void CreateWallQuad(string name, Vector3 position, Quaternion rotation, Material mat, float height)
        {
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = name;
            wall.transform.SetPositionAndRotation(position, rotation);
            wall.transform.localScale = new Vector3(10f, height, 1f);
            MeshRenderer renderer = wall.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = mat;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        private static void BuildUICanvas()
        {
            GameObject canvasGo = new GameObject("UICanvas");
            canvasGo.tag = UICanvasTag;
            canvasGo.layer = LayerMask.NameToLayer("UI");

            Canvas canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasGo.AddComponent<GraphicRaycaster>();
        }

        private static void BuildEventSystem()
        {
            GameObject esGo = new GameObject("EventSystem", typeof(EventSystem));
            //# InputSystemUIInputModule 은 패키지 어셈블리 — reflection attach (LairUIPrefabBuilder 패턴).
            System.Type type = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (type != null && typeof(BaseInputModule).IsAssignableFrom(type))
            {
                esGo.AddComponent(type);
                return;
            }
            esGo.AddComponent<StandaloneInputModule>();
        }

        //# Build Settings — Loading 0 · Village 1 · Battle 2 (기획서 §11.2).
        private static void RegisterBuildScenes()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>
            {
                new EditorBuildSettingsScene(LoadingScenePath, true),
                new EditorBuildSettingsScene(VillageScenePath, true),
                new EditorBuildSettingsScene(BattleScenePath, true),
            };
            EditorBuildSettings.scenes = scenes.ToArray();
            Debug.Log("[LairVillageBuilder] Build Settings — Loading 0 / Village 1 / Battle 2 등록");
        }

        //# Battle 씬의 BattleController._metaConfig 와이어링 — 메타 보너스/정산 활성화.
        private static void WireBattleSceneMetaConfig()
        {
            Scene battle = EditorSceneManager.OpenScene(BattleScenePath, OpenSceneMode.Single);

            //# 씬 전환 후 재로드 — 전환 전 참조는 unload 로 destroyed 일 수 있음 (BuildVillageScene 동일 사유).
            MetaConfig metaConfig = AssetDatabase.LoadAssetAtPath<MetaConfig>(MetaConfigPath);
            if (metaConfig == null)
            {
                Debug.LogError("[LairVillageBuilder] MetaConfig.asset 재로드 실패 — _metaConfig 수동 연결 필요");
                return;
            }

            Lair.Battle.BattleController controller = Object.FindFirstObjectByType<Lair.Battle.BattleController>();
            if (controller == null)
            {
                Debug.LogError("[LairVillageBuilder] Battle 씬에서 BattleController 미발견 — _metaConfig 수동 연결 필요");
                return;
            }

            SerializedObject so = new SerializedObject(controller);
            SetObj(so, "_metaConfig", metaConfig);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(battle);
            EditorSceneManager.SaveScene(battle);

            if (VerifySceneHasMetaConfig(BattleScenePath) == false)
            {
                Debug.LogError("[LairVillageBuilder] Battle 씬 _metaConfig 직렬화 검증 실패 — 수동 연결 필요");
                return;
            }
            Debug.Log("[LairVillageBuilder] Battle 씬 BattleController._metaConfig 연결 완료");
        }

        //# ===================================================================
        //# 공통 헬퍼
        //# ===================================================================

        //# 모달 골격 — full-stretch 루트 + Dim(CHButton) + 본체 + 타이틀(CHText stringId) + X 버튼.
        private static GameObject BuildModalSkeleton(string prefabName, string title, int titleStringId,
            out GameObject body, out CHButton dimButton, out CHButton closeButton, Vector2 bodySize = default)
        {
            if (bodySize == default)
            {
                bodySize = new Vector2(720f, 480f);
            }

            GameObject root = new GameObject(prefabName, typeof(RectTransform));
            FullStretch(root.transform);

            GameObject dimGo = new GameObject("Dim", typeof(RectTransform), typeof(Image), typeof(Button));
            dimGo.transform.SetParent(root.transform, false);
            FullStretch(dimGo.transform);
            Image dimImg = dimGo.GetComponent<Image>();
            dimImg.sprite = UISprite;
            dimImg.color = ModalDim;
            dimGo.GetComponent<Button>().targetGraphic = dimImg;
            dimButton = dimGo.AddComponent<CHButton>();

            body = new GameObject("ModalBody", typeof(RectTransform), typeof(Image));
            body.transform.SetParent(root.transform, false);
            SetRect(body.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, bodySize);
            Image bodyImg = body.GetComponent<Image>();
            bodyImg.sprite = UISprite;
            bodyImg.type = Image.Type.Sliced;
            bodyImg.color = ModalBg;
            Outline outline = body.AddComponent<Outline>();
            outline.effectColor = YellowAccent;
            outline.effectDistance = new Vector2(2f, -2f);

            CHText titleText = AddTmp(body.transform, "Title", title, 16f, TextAlignmentOptions.Left, Color.white, stringId: titleStringId);
            SetRect(titleText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(16f, -12f), new Vector2(240f, 26f));

            GameObject closeGo = new GameObject("CloseButton", typeof(RectTransform), typeof(Image), typeof(Button));
            closeGo.transform.SetParent(body.transform, false);
            SetRect(closeGo.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -12f), new Vector2(28f, 28f));
            Image closeImg = closeGo.GetComponent<Image>();
            closeImg.sprite = UISprite;
            closeImg.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            closeGo.GetComponent<Button>().targetGraphic = closeImg;
            closeButton = closeGo.AddComponent<CHButton>();

            GameObject xGo = new GameObject("X", typeof(RectTransform));
            xGo.transform.SetParent(closeGo.transform, false);
            FullStretch(xGo.transform);
            TextMeshProUGUI xTmp = xGo.AddComponent<TextMeshProUGUI>();
            xTmp.text = "×";
            xTmp.font = TMP_Settings.defaultFontAsset;
            xTmp.fontSize = 18f;
            xTmp.alignment = TextAlignmentOptions.Center;
            xTmp.color = Color.white;
            xTmp.raycastTarget = false;
            xGo.AddComponent<CHText>();   //# 정적 라벨도 CHText 동행 (Rule 03 §3)

            return root;
        }

        //# 스크롤뷰 — ScrollRect + Viewport(RectMask2D) + Content + 셀 prefab origin 인스턴스 + CHPoolingScrollView<T>.
        private static T AddScrollView<T>(Transform body, string cellPrefabName, int columnCount, Vector2 itemGap,
            string scrollName = "ScrollView", float topOffset = -48f, Vector2 originSize = default) where T : MonoBehaviour
        {
            GameObject scrollGo = new GameObject(scrollName, typeof(RectTransform), typeof(ScrollRect));
            scrollGo.transform.SetParent(body, false);
            RectTransform scrollRt = (RectTransform)scrollGo.transform;
            scrollRt.anchorMin = new Vector2(0f, 0f);
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(16f, 16f);
            scrollRt.offsetMax = new Vector2(-16f, topOffset);
            ScrollRect sr = scrollGo.GetComponent<ScrollRect>();
            sr.horizontal = false;
            sr.vertical = true;
            sr.movementType = ScrollRect.MovementType.Elastic;
            sr.elasticity = 0.1f;
            sr.inertia = true;
            sr.decelerationRate = 0.135f;
            sr.scrollSensitivity = 12f;

            GameObject viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            FullStretch(viewportGo.transform);
            Image viewportImg = viewportGo.GetComponent<Image>();
            viewportImg.sprite = UISprite;
            viewportImg.color = new Color(0f, 0f, 0f, 0.001f);
            viewportImg.raycastTarget = true;

            GameObject contentGo = new GameObject("Content", typeof(RectTransform));
            contentGo.transform.SetParent(viewportGo.transform, false);
            RectTransform contentRt = (RectTransform)contentGo.transform;
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.anchoredPosition = Vector2.zero;
            contentRt.sizeDelta = Vector2.zero;

            sr.viewport = (RectTransform)viewportGo.transform;
            sr.content = contentRt;

            //# origin — 셀 prefab 인스턴스를 Content 첫 자식으로 (Rule 03 §3 — 컴포넌트 제거 금지).
            GameObject cellPrefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{PrefabDir}/{cellPrefabName}.prefab");
            GameObject originInst = null;
            if (cellPrefab != null)
            {
                originInst = (GameObject)PrefabUtility.InstantiatePrefab(cellPrefab, contentGo.transform);
                originInst.SetActive(false);
                if (originSize != default)
                {
                    ((RectTransform)originInst.transform).sizeDelta = originSize;
                }
            }
            else
            {
                Debug.LogError($"[LairVillageBuilder] {cellPrefabName}.prefab 미발견 — 셀 빌드 선행 필요");
            }

            T scrollView = scrollGo.AddComponent<T>();
            SerializedObject so = new SerializedObject(scrollView);
            if (originInst != null)
            {
                SetObj(so, "_origin", originInst);
            }
            SetVector2(so, "_itemGap", itemGap);
            SetRectOffset(so, "_padding", 0, 0, 0, 0);
            SetEnum(so, "_scrollDirection", 0);   //# Vertical
            SetEnum(so, "_align", columnCount == 1 ? 1 : 0);   //# 단일 열은 Center, 그리드는 LeftOrTop
            SetInt(so, "_rowCount", 0);
            SetInt(so, "_columnCount", columnCount);
            SetInt(so, "_poolItemCount", 0);
            so.ApplyModifiedPropertiesWithoutUndo();

            return scrollView;
        }

        private static GameObject NewCellRoot(string name, Vector2 size, out Image bg)
        {
            GameObject root = new GameObject(name, typeof(RectTransform), typeof(Image));
            ((RectTransform)root.transform).sizeDelta = size;
            bg = root.GetComponent<Image>();
            bg.sprite = UISprite;
            bg.type = Image.Type.Sliced;
            bg.color = CellBg;
            return root;
        }

        //# TMP + CHText 생성 (Rule 03 §3 — 모든 TMP_Text 에 CHText 동행). stringId >= 0 이면 직렬화.
        private static CHText AddTmp(Transform parent, string name, string text, float fontSize,
            TextAlignmentOptions align, Color color, int stringId = -1)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = TMP_Settings.defaultFontAsset;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = color;
            tmp.raycastTarget = false;
            CHText ch = go.AddComponent<CHText>();
            if (stringId >= 0)
            {
                SerializedObject so = new SerializedObject(ch);
                SetInt(so, "_stringID", stringId);
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return ch;
        }

        private static CHButton AddButton(Transform parent, string name, Color bgColor, out GameObject go)
        {
            go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            Image img = go.GetComponent<Image>();
            img.sprite = UISprite;
            img.type = Image.Type.Sliced;
            img.color = bgColor;
            go.GetComponent<Button>().targetGraphic = img;
            return go.AddComponent<CHButton>();
        }

        private static void SetRect(Transform t, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPos, Vector2 size)
        {
            RectTransform rt = (RectTransform)t;
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = pivot;
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
        }

        private static void FullStretch(Transform t)
        {
            RectTransform rt = (RectTransform)t;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            rt.localScale = Vector3.one;
        }

        private static void SaveCellPrefab(GameObject root, string prefabName)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            Debug.Log($"[LairVillageBuilder] {prefabName}.prefab 빌드 완료 (Addressable X — origin nesting)");
        }

        private static void SaveAndRegisterPrefab(GameObject root, string prefabName,
            AddressableAssetSettings settings, AddressableAssetGroup group)
        {
            string path = $"{PrefabDir}/{prefabName}.prefab";
            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);

            string guid = AssetDatabase.AssetPathToGUID(path);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group, readOnly: false, postEvent: false);
            entry.address = prefabName;   //# 주소 = 파일명 = EUI 값명 (Rule 03 §2)
            entry.SetLabel(ResourceLabel, enable: true, force: true, postEvent: false);
            Debug.Log($"[LairVillageBuilder] {prefabName}.prefab 빌드 + Addressable 등록 (address={entry.address})");
        }

        private static void SetObj(SerializedObject so, string field, Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetInt(SerializedObject so, string field, int value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.intValue = value;
        }

        private static void SetVector2(SerializedObject so, string field, Vector2 value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.vector2Value = value;
        }

        private static void SetEnum(SerializedObject so, string field, int enumIndex)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.enumValueIndex = enumIndex;
        }

        private static void SetRectOffset(SerializedObject so, string field, int left, int right, int top, int bottom)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairVillageBuilder] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.FindPropertyRelative("m_Left").intValue = left;
            prop.FindPropertyRelative("m_Right").intValue = right;
            prop.FindPropertyRelative("m_Top").intValue = top;
            prop.FindPropertyRelative("m_Bottom").intValue = bottom;
        }

        private static void EnsureTagExists(string tag)
        {
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
            if (assets == null || assets.Length == 0)
            {
                Debug.LogWarning("[LairVillageBuilder] TagManager.asset 로드 실패");
                return;
            }
            SerializedObject tagManager = new SerializedObject(assets[0]);
            SerializedProperty tagsProp = tagManager.FindProperty("tags");
            if (tagsProp == null)
                return;

            for (int i = 0; i < tagsProp.arraySize; ++i)
            {
                if (tagsProp.GetArrayElementAtIndex(i).stringValue == tag)
                    return;
            }
            tagsProp.InsertArrayElementAtIndex(tagsProp.arraySize);
            tagsProp.GetArrayElementAtIndex(tagsProp.arraySize - 1).stringValue = tag;
            tagManager.ApplyModifiedPropertiesWithoutUndo();
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
