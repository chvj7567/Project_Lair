using ChvjUnityInfra;
using Lair.Character;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# 일회용 프리팹 빌더 (Rule 04 §3) — 육성 리뷰 반영 UI: 상점 지갑 알약(112x32) · 결과창 재화 칩 · 상태 아이콘 중첩 "xN" 라벨.
    //# 1회 실행 후 생성된 프리팹을 커밋하고 이 파일은 삭제한다(재실행 시 같은 이름 자식이 있으면 건너뛴다).
    public static class MonsterGrowthUiBuilder2
    {
        private const string UiRoot = "Assets/_Lair/Art/UI/";
        private const string SoulCoinPath = "Assets/_Lair/Art/Sprites/UiDot/Px_SoulCoin.png";
        private static readonly Color PillBg = new Color32(0x14, 0x18, 0x24, 0xE6);

        [MenuItem("Lair/Build/Monster Growth UI Review Fix (once)")]
        public static void BuildAll()
        {
            BuildShopWallet();
            BuildResultChips();
            BuildStatusCounts();
            AssetDatabase.SaveAssets();
            Debug.Log("[MonsterGrowthUiBuilder2] 완료 — 프리팹 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# 지갑 — 소울 알약 왼쪽에 알약 3개(각성석·달빛 인장·심연 결정), 오른쪽 정렬 HorizontalLayoutGroup(간격 8). 기존 WalletText 는 제거.
        private static void BuildShopWallet()
        {
            string path = UiRoot + "ShopPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ShopPopup popup = root.GetComponent<ShopPopup>();
                SerializedObject so = new SerializedObject(popup);
                CHText soul = so.FindProperty("_soulText").objectReferenceValue as CHText;
                if (soul == null || FindDeep(root.transform, "WalletPills") != null)
                {
                    Debug.LogWarning("[MonsterGrowthUiBuilder2] ShopPopup — 소울 문구가 없거나 지갑 알약이 이미 있음, 건너뜀");
                    return;
                }
                Transform oldWallet = FindDeep(root.transform, "WalletText");
                if (oldWallet != null)
                {
                    Object.DestroyImmediate(oldWallet.gameObject);
                }
                RectTransform soulRect = (RectTransform)soul.transform;
                //# 소울 문구가 알약 배경 안에 있으면 그 배경을 기준으로 바로 왼쪽에 둔다.
                RectTransform anchor = soulRect.parent is RectTransform parentRect && parentRect.GetComponent<Image>() != null ? parentRect : soulRect;
                RectTransform group = NewRect("WalletPills", anchor.parent);
                group.anchorMin = anchor.anchorMin;
                group.anchorMax = anchor.anchorMax;
                group.pivot = new Vector2(1f, 0.5f);
                group.sizeDelta = new Vector2(3 * 112f + 2 * 8f, 32f);
                group.anchoredPosition = anchor.anchoredPosition + new Vector2(-(anchor.rect.width * anchor.pivot.x) - 8f, (0.5f - anchor.pivot.y) * anchor.rect.height);
                HorizontalLayoutGroup layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleRight;
                layout.spacing = 8f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                TextMeshProUGUI font = soul.GetComponent<TextMeshProUGUI>();
                Sprite coin = AssetDatabase.LoadAssetAtPath<Sprite>(SoulCoinPath);
                CurrencyPillView[] pills = new CurrencyPillView[3];
                for (int i = 0; i < pills.Length; ++i)
                {
                    pills[i] = NewPill("WalletPill" + i, group, 112f, 32f, font, coin, 16f, 14f, false);
                }
                SetArray(so, "_walletPills", pills);
                if (so.FindProperty("_soulIcon").objectReferenceValue == null)
                {
                    so.FindProperty("_soulIcon").objectReferenceValue = coin;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# 결과창 소울 줄 — SoulsText 자리(오른쪽 정렬)에 칩 2개(아이콘 14 + "+N" 16 + 이름 12), 간격 12.
        private static void BuildResultChips()
        {
            string path = UiRoot + "ResultPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                ResultPopup popup = root.GetComponent<ResultPopup>();
                SerializedObject so = new SerializedObject(popup);
                CHText souls = so.FindProperty("_soulsText").objectReferenceValue as CHText;
                if (souls == null || FindDeep(root.transform, "RewardChips") != null)
                {
                    Debug.LogWarning("[MonsterGrowthUiBuilder2] ResultPopup — 소울 값 문구가 없거나 칩이 이미 있음, 건너뜀");
                    return;
                }
                RectTransform soulsRect = (RectTransform)souls.transform;
                RectTransform group = NewRect("RewardChips", soulsRect.parent);
                group.anchorMin = soulsRect.anchorMin;
                group.anchorMax = soulsRect.anchorMax;
                group.pivot = new Vector2(1f, 0.5f);
                group.sizeDelta = new Vector2(300f, soulsRect.rect.height);
                group.anchoredPosition = soulsRect.anchoredPosition + new Vector2((1f - soulsRect.pivot.x) * soulsRect.rect.width, (0.5f - soulsRect.pivot.y) * soulsRect.rect.height);
                HorizontalLayoutGroup layout = group.gameObject.AddComponent<HorizontalLayoutGroup>();
                layout.childAlignment = TextAnchor.MiddleRight;
                layout.spacing = 12f;
                layout.childControlWidth = false;
                layout.childControlHeight = false;
                layout.childForceExpandWidth = false;
                layout.childForceExpandHeight = false;

                TextMeshProUGUI font = souls.GetComponent<TextMeshProUGUI>();
                Sprite coin = AssetDatabase.LoadAssetAtPath<Sprite>(SoulCoinPath);
                CurrencyPillView[] chips = new CurrencyPillView[2];
                chips[0] = NewPill("RewardChip0", group, 120f, 28f, font, coin, 14f, 16f, true);
                chips[1] = NewPill("RewardChip1", group, 140f, 28f, font, coin, 14f, 16f, true);
                SetArray(so, "_rewardChips", chips);
                if (so.FindProperty("_soulIcon").objectReferenceValue == null)
                {
                    so.FindProperty("_soulIcon").objectReferenceValue = coin;
                }
                so.ApplyModifiedPropertiesWithoutUndo();
                group.gameObject.SetActive(true);
                foreach (CurrencyPillView chip in chips)
                {
                    chip.gameObject.SetActive(false);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# 전투 HUD 영웅 바 상태 아이콘 슬롯마다 우하단 "xN" 라벨 — BossHpBarView._iconCounts (슬롯과 같은 인덱스).
        private static void BuildStatusCounts()
        {
            string path = UiRoot + "BattleHud.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                BossHpBarView bar = root.GetComponentInChildren<BossHpBarView>(true);
                if (bar == null)
                {
                    Debug.LogError("[MonsterGrowthUiBuilder2] BattleHud 에 BossHpBarView 가 없음");
                    return;
                }
                SerializedObject so = new SerializedObject(bar);
                SerializedProperty slots = so.FindProperty("_iconSlots");
                if (so.FindProperty("_iconCounts").arraySize > 0)
                {
                    Debug.LogWarning("[MonsterGrowthUiBuilder2] 상태 아이콘 xN 라벨이 이미 있음 — 건너뜀");
                    return;
                }
                TextMeshProUGUI font = FindAnyTmp(root.transform);
                CHText[] counts = new CHText[slots.arraySize];
                for (int i = 0; i < slots.arraySize; ++i)
                {
                    Image slot = slots.GetArrayElementAtIndex(i).objectReferenceValue as Image;
                    if (slot == null)
                        continue;
                    RectTransform label = NewRect("Count", slot.transform);
                    label.anchorMin = new Vector2(1f, 0f);
                    label.anchorMax = new Vector2(1f, 0f);
                    label.pivot = new Vector2(1f, 0f);
                    label.sizeDelta = new Vector2(28f, 14f);
                    label.anchoredPosition = new Vector2(4f, -2f);
                    TextMeshProUGUI tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
                    if (font != null)
                    {
                        tmp.font = font.font;
                        tmp.fontSharedMaterial = font.fontSharedMaterial;
                    }
                    tmp.fontSize = 11f;
                    tmp.alignment = TextAlignmentOptions.BottomRight;
                    tmp.enableWordWrapping = false;
                    tmp.raycastTarget = false;
                    counts[i] = label.gameObject.AddComponent<CHText>();
                    label.gameObject.SetActive(false);
                }
                SetArray(so, "_iconCounts", counts);
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# 알약 = 배경 + 아이콘 + 숫자 (+ 선택 이름). 레이아웃 그룹 자식이라 크기만 정한다.
        private static CurrencyPillView NewPill(string name, Transform parent, float w, float h, TextMeshProUGUI font, Sprite coin, float iconSize, float amountSize, bool withName)
        {
            RectTransform pill = NewRect(name, parent);
            pill.sizeDelta = new Vector2(w, h);
            Image bg = pill.gameObject.AddComponent<Image>();
            bg.color = withName ? Color.clear : PillBg;
            bg.raycastTarget = false;

            RectTransform icon = NewRect("Icon", pill);
            icon.anchorMin = new Vector2(0f, 0.5f);
            icon.anchorMax = new Vector2(0f, 0.5f);
            icon.pivot = new Vector2(0f, 0.5f);
            icon.sizeDelta = new Vector2(iconSize, iconSize);
            icon.anchoredPosition = new Vector2(8f, 0f);
            Image iconImage = icon.gameObject.AddComponent<Image>();
            iconImage.sprite = coin;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;

            float textLeft = 8f + iconSize + 6f;
            CHText amount = NewLabel("Amount", pill, textLeft, withName ? 44f : w - textLeft - 8f, h, font, amountSize, TextAlignmentOptions.Left);
            CHText nameText = withName ? NewLabel("Name", pill, textLeft + 46f, w - textLeft - 50f, h, font, 12f, TextAlignmentOptions.Left) : null;

            CurrencyPillView view = pill.gameObject.AddComponent<CurrencyPillView>();
            SerializedObject so = new SerializedObject(view);
            so.FindProperty("_icon").objectReferenceValue = iconImage;
            so.FindProperty("_amountText").objectReferenceValue = amount;
            so.FindProperty("_nameText").objectReferenceValue = nameText;
            so.ApplyModifiedPropertiesWithoutUndo();
            pill.gameObject.SetActive(false);
            return view;
        }

        private static CHText NewLabel(string name, Transform parent, float x, float w, float h, TextMeshProUGUI font, float size, TextAlignmentOptions align)
        {
            RectTransform rect = NewRect(name, parent);
            rect.anchorMin = new Vector2(0f, 0.5f);
            rect.anchorMax = new Vector2(0f, 0.5f);
            rect.pivot = new Vector2(0f, 0.5f);
            rect.sizeDelta = new Vector2(w, h);
            rect.anchoredPosition = new Vector2(x, 0f);
            TextMeshProUGUI tmp = rect.gameObject.AddComponent<TextMeshProUGUI>();
            if (font != null)
            {
                tmp.font = font.font;
                tmp.fontSharedMaterial = font.fontSharedMaterial;
            }
            tmp.fontSize = size;
            tmp.alignment = align;
            tmp.richText = true;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            return rect.gameObject.AddComponent<CHText>();
        }

        private static RectTransform NewRect(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            RectTransform rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            return rect;
        }

        private static TextMeshProUGUI FindAnyTmp(Transform root) => root.GetComponentInChildren<TextMeshProUGUI>(true);

        private static Transform FindDeep(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name == name)
                    return child;
                Transform found = FindDeep(child, name);
                if (found != null)
                    return found;
            }
            return null;
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
