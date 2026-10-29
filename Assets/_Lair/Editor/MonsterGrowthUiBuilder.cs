using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# 일회용 프리팹 빌더 (Rule 04 §3) — 몬스터 육성 재화 UI(monster-growth-currencies §8) 위젯을 기존 프리팹에 추가·배선한다.
    //# 1회 실행 후 생성된 프리팹을 커밋하고 이 파일은 삭제한다(재실행 시 같은 이름 자식이 있으면 건너뛴다).
    public static class MonsterGrowthUiBuilder
    {
        private const string UiRoot = "Assets/_Lair/Art/UI/";
        private const string SoulCoinPath = "Assets/_Lair/Art/Sprites/UiDot/Px_SoulCoin.png";
        private const string LockPath = "Assets/_Lair/Art/Sprites/UiDot/Px_Lock.png";

        private static readonly Color SlotBg = new Color32(0x14, 0x18, 0x24, 0xFF);
        private static readonly Color SubText = new Color32(0x8E, 0x98, 0xAD, 0xFF);

        [MenuItem("Lair/Build/Monster Growth UI (once)")]
        public static void BuildAll()
        {
            BuildShopItemCell();
            BuildShopPopup();
            BuildRelicPopup();
            AssetDatabase.SaveAssets();
            Debug.Log("[MonsterGrowthUiBuilder] 완료 — 프리팹 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# 종족 행 660x84 = 머리 80x84 + 칸 140x72 x4 (x 84/228/372/516, y 6) — §8.1 좌표표.
        private static void BuildShopItemCell()
        {
            string path = UiRoot + "ShopItemCell.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (root.transform.Find("SpeciesRow") != null)
                {
                    Debug.LogWarning("[MonsterGrowthUiBuilder] ShopItemCell 에 SpeciesRow 가 이미 있음 — 건너뜀");
                    return;
                }
                ShopItemCell cell = root.GetComponent<ShopItemCell>();
                TextMeshProUGUI fontSource = root.transform.Find("NameText").GetComponent<TextMeshProUGUI>();
                Image pipSource = root.transform.Find("LevelPips/LevelPip0").GetComponent<Image>();
                Image ringSource = root.transform.Find("StateRing").GetComponent<Image>();
                Sprite soulCoin = AssetDatabase.LoadAssetAtPath<Sprite>(SoulCoinPath);
                Sprite lockSprite = AssetDatabase.LoadAssetAtPath<Sprite>(LockPath);

                RectTransform row = NewRect("SpeciesRow", root.transform, 0f, 0f, 660f, 84f);
                Image headGlow = NewImage("HeadGlow", row, 10f, 4f, 60f, 60f, ringSource.sprite, Color.clear);
                Image headIcon = NewImage("HeadIcon", row, 12f, 6f, 56f, 56f, null, Color.white);
                headIcon.preserveAspect = true;
                CHText headName = NewText("HeadName", row, 0f, 64f, 80f, 16f, fontSource, 12f, Color.white, TextAlignmentOptions.Center);

                ShopAxisSlotView[] slots = new ShopAxisSlotView[4];
                for (int i = 0; i < 4; ++i)
                {
                    slots[i] = NewSlot(row, i, 84f + i * 144f, fontSource, pipSource.sprite, ringSource.sprite, soulCoin, lockSprite);
                }
                row.gameObject.SetActive(false);

                SerializedObject so = new SerializedObject(cell);
                so.FindProperty("_speciesRow").objectReferenceValue = row.gameObject;
                so.FindProperty("_speciesHeadIcon").objectReferenceValue = headIcon;
                so.FindProperty("_speciesHeadGlow").objectReferenceValue = headGlow;
                so.FindProperty("_speciesHeadName").objectReferenceValue = headName;
                SetArray(so, "_axisSlots", slots);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# 칸 140x72 — 줄1 축 이름 + 눈금(y 4), 줄2 효과(y 22, 2줄), 줄3 가격 띠(y 48).
        private static ShopAxisSlotView NewSlot(RectTransform parent, int index, float x, TextMeshProUGUI font, Sprite pip, Sprite ring, Sprite coin, Sprite lockSprite)
        {
            RectTransform slot = NewRect("AxisSlot" + index, parent, x, 6f, 140f, 72f);
            Image bg = slot.gameObject.AddComponent<Image>();
            bg.color = SlotBg;
            Button button = slot.gameObject.AddComponent<Button>();
            button.targetGraphic = bg;
            CHButton chButton = slot.gameObject.AddComponent<CHButton>();
            CanvasGroup group = slot.gameObject.AddComponent<CanvasGroup>();
            ShopAxisSlotView view = slot.gameObject.AddComponent<ShopAxisSlotView>();

            Image border = NewImage("Border", slot, 0f, 0f, 140f, 72f, ring, Color.white);
            border.type = Image.Type.Sliced;
            border.raycastTarget = false;
            CHText axis = NewText("AxisText", slot, 8f, 4f, 70f, 16f, font, 13f, Color.white, TextAlignmentOptions.Left);
            Image[] pips = new Image[5];
            for (int i = 0; i < pips.Length; ++i)
            {
                pips[i] = NewImage("Pip" + i, slot, 92f + i * 8f, 9f, 6f, 6f, pip, Color.white);
                pips[i].raycastTarget = false;
            }
            CHText effect = NewText("EffectText", slot, 8f, 22f, 124f, 24f, font, 11f, SubText, TextAlignmentOptions.TopLeft);
            effect.GetComponent<TextMeshProUGUI>().enableWordWrapping = true;
            Image icon = NewImage("CurrencyIcon", slot, 8f, 52f, 12f, 12f, coin, Color.white);
            icon.raycastTarget = false;
            CHText price = NewText("PriceText", slot, 24f, 48f, 108f, 20f, font, 13f, Color.white, TextAlignmentOptions.Left);
            Image lockIcon = NewImage("LockIcon", slot, 112f, 4f, 20f, 20f, lockSprite, Color.white);
            lockIcon.raycastTarget = false;
            lockIcon.gameObject.SetActive(false);

            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_button").objectReferenceValue = chButton;
            so.FindProperty("_axisText").objectReferenceValue = axis;
            so.FindProperty("_effectText").objectReferenceValue = effect;
            so.FindProperty("_priceText").objectReferenceValue = price;
            so.FindProperty("_currencyIcon").objectReferenceValue = icon;
            SetArray(so, "_pips", pips);
            so.FindProperty("_border").objectReferenceValue = border;
            so.FindProperty("_lockIcon").objectReferenceValue = lockIcon.gameObject;
            so.FindProperty("_canvasGroup").objectReferenceValue = group;
            so.ApplyModifiedPropertiesWithoutUndo();
            return view;
        }

        //# 상점 지갑 — 소울 알약 왼쪽에 해금된 특수 재화 문구(§8.1). 소울 코인 아이콘도 함께 배선.
        private static void BuildShopPopup()
        {
            string path = UiRoot + "ShopPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ShopPopup popup = root.GetComponent<ShopPopup>();
                SerializedObject so = new SerializedObject(popup);
                CHText soul = so.FindProperty("_soulText").objectReferenceValue as CHText;
                if (so.FindProperty("_walletText").objectReferenceValue == null && soul != null)
                {
                    RectTransform soulRect = (RectTransform)soul.transform;
                    GameObject go = new GameObject("WalletText", typeof(RectTransform));
                    RectTransform rect = (RectTransform)go.transform;
                    rect.SetParent(soulRect.parent, false);
                    rect.anchorMin = soulRect.anchorMin;
                    rect.anchorMax = soulRect.anchorMax;
                    rect.pivot = new Vector2(1f, 0.5f);
                    rect.sizeDelta = new Vector2(360f, soulRect.sizeDelta.y);
                    rect.anchoredPosition = soulRect.anchoredPosition - new Vector2(soulRect.rect.width * soulRect.pivot.x + 8f, 0f);
                    TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
                    TextMeshProUGUI src = soul.GetComponent<TextMeshProUGUI>();
                    tmp.font = src.font;
                    tmp.fontSharedMaterial = src.fontSharedMaterial;
                    tmp.fontSize = 14f;
                    tmp.alignment = TextAlignmentOptions.Right;
                    tmp.richText = true;
                    tmp.enableWordWrapping = false;
                    tmp.overflowMode = TextOverflowModes.Ellipsis;
                    tmp.raycastTarget = false;
                    CHText ch = go.AddComponent<CHText>();
                    so.FindProperty("_walletText").objectReferenceValue = ch;
                }
                if (so.FindProperty("_soulIcon").objectReferenceValue == null)
                {
                    so.FindProperty("_soulIcon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(SoulCoinPath);
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# 유물 도감 잔해 교환 줄 — 슬라이스 E 에서 채운다.
        private static void BuildRelicPopup()
        {
        }

        private static RectTransform NewRect(string name, Transform parent, float x, float y, float w, float h)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, -y);
            return rect;
        }

        private static Image NewImage(string name, Transform parent, float x, float y, float w, float h, Sprite sprite, Color color)
        {
            RectTransform rect = NewRect(name, parent, x, y, w, h);
            Image image = rect.gameObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        //# TMP + CHText 쌍 (Rule 03 §3 — 정적 라벨 포함 CHText 필수).
        private static CHText NewText(string name, Transform parent, float x, float y, float w, float h, TextMeshProUGUI font, float size, Color color, TextAlignmentOptions align)
        {
            RectTransform rect = NewRect(name, parent, x, y, w, h);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            tmp.font = font.font;
            tmp.fontSharedMaterial = font.fontSharedMaterial;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.richText = true;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return rect.gameObject.AddComponent<CHText>();
        }

        private static void SetArray(SerializedObject so, string field, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(field);
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
