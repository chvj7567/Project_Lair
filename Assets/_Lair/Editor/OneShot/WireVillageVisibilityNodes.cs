using System.IO;
using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools.OneShot
{
    //# 일회성·멱등 빌더 — 마을 메타 가시화 M5. ShopPopup 요약줄 + QuestCell 진행 바 노드를
    //# 두 프리팹에만 추가하고 SerializeField 와이어링한다 (기획서 village-meta-visibility.md §5).
    //# PrefabUtility API 로만 편집 (손-YAML 금지) — fileID 무손실. 이미 연결돼 있으면 skip.
    public static class WireVillageVisibilityNodes
    {
        private const string ShopPopupPath = "Assets/_Lair/Art/UI/ShopPopup.prefab";
        private const string QuestCellPath = "Assets/_Lair/Art/UI/QuestCell.prefab";
        private const string FontGuid = "12e8e80fec3a8554fa63474df537b505";

        [MenuItem("Lair/OneShot/Wire Village Visibility Nodes")]
        public static void Run()
        {
            TMP_FontAsset font = LoadFont();
            if (font == null)
            {
                Debug.LogError("[WireVillageVisibilityNodes] NotoSansKR SDF 폰트를 찾지 못함 — 중단");
                return;
            }

            WireShopPopup(font);
            WireQuestCell(font);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WireVillageVisibilityNodes] 노드 추가/연결 완료");
        }

        //# ── ShopPopup 요약줄 ───────────────────────────────────────────────
        private static void WireShopPopup(TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(ShopPopupPath);
            if (root == null)
            {
                Debug.LogError($"[WireVillageVisibilityNodes] 로드 실패: {ShopPopupPath}");
                return;
            }

            try
            {
                ShopPopup popup = root.GetComponent<ShopPopup>();
                if (popup == null)
                {
                    Debug.LogError("[WireVillageVisibilityNodes] ShopPopup 컴포넌트 없음 — 중단");
                    return;
                }

                SerializedObject so = new SerializedObject(popup);
                SerializedProperty prop = so.FindProperty("_bonusSummaryText");
                if (prop == null)
                {
                    Debug.LogError("[WireVillageVisibilityNodes] _bonusSummaryText 필드 없음 — 코드 미머지?");
                    return;
                }
                if (prop.objectReferenceValue != null)
                {
                    Debug.Log("[WireVillageVisibilityNodes] ShopPopup._bonusSummaryText 이미 연결됨 — skip");
                    return;
                }

                //# 소울 잔액(_soulText) 의 부모 = ModalBody. 그 아래(요약줄)를 형제로 추가.
                RectTransform soulRt = popup.transform.Find("ModalBody/SoulText") as RectTransform;
                Transform parent = soulRt != null ? soulRt.parent : root.transform;

                CHText chText = CreateLabel(parent, "BonusSummaryText", font);
                RectTransform rt = chText.transform as RectTransform;
                //# 상단 가로 전폭 — 소울 잔액 아래. 좌우 패딩 16, 소울줄(높이 24, top -14) 아래에 배치.
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(1f, 1f);
                rt.pivot = new Vector2(0.5f, 1f);
                rt.anchoredPosition = new Vector2(0f, -44f);
                rt.offsetMin = new Vector2(16f, rt.offsetMin.y);
                rt.offsetMax = new Vector2(-16f, rt.offsetMax.y);
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, 40f);

                TMP_Text tmp = chText.GetComponent<TMP_Text>();
                tmp.text = "현재 강화  아직 없음";
                tmp.fontSize = 13f;
                tmp.color = new Color(0.78f, 0.82f, 0.88f, 1f);
                tmp.alignment = TextAlignmentOptions.TopLeft;
                //# §2.3 — word-wrap 1~2줄 가변, ellipsis 금지(trailing 잘림 = 의도 깨짐).
                tmp.textWrappingMode = TextWrappingModes.Normal;
                tmp.overflowMode = TextOverflowModes.Overflow;

                prop.objectReferenceValue = chText;
                so.ApplyModifiedPropertiesWithoutUndo();

                Save(root, ShopPopupPath);
                Debug.Log("[WireVillageVisibilityNodes] ShopPopup._bonusSummaryText 노드 추가 + 연결 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ── QuestCell 진행 바 ──────────────────────────────────────────────
        private static void WireQuestCell(TMP_FontAsset font)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(QuestCellPath);
            if (root == null)
            {
                Debug.LogError($"[WireVillageVisibilityNodes] 로드 실패: {QuestCellPath}");
                return;
            }

            try
            {
                QuestCell cell = root.GetComponent<QuestCell>();
                if (cell == null)
                {
                    Debug.LogError("[WireVillageVisibilityNodes] QuestCell 컴포넌트 없음 — 중단");
                    return;
                }

                SerializedObject so = new SerializedObject(cell);
                SerializedProperty rootProp = so.FindProperty("_progressRoot");
                SerializedProperty fillProp = so.FindProperty("_progressFill");
                SerializedProperty textProp = so.FindProperty("_progressText");
                if (rootProp == null || fillProp == null || textProp == null)
                {
                    Debug.LogError("[WireVillageVisibilityNodes] 진행 바 필드 없음 — 코드 미머지?");
                    return;
                }
                if (rootProp.objectReferenceValue != null)
                {
                    Debug.Log("[WireVillageVisibilityNodes] QuestCell._progressRoot 이미 연결됨 — skip");
                    return;
                }

                //# _progressRoot: 달성 뱃지(AchievedBadge)와 동일 영역 — 런타임 토글 상호 배타라 충돌 무관.
                GameObject rootGo = new GameObject("ProgressRoot", typeof(RectTransform));
                RectTransform progRt = rootGo.GetComponent<RectTransform>();
                progRt.SetParent(cell.transform, false);
                progRt.anchorMin = new Vector2(1f, 0.5f);
                progRt.anchorMax = new Vector2(1f, 0.5f);
                progRt.pivot = new Vector2(1f, 0.5f);
                progRt.anchoredPosition = new Vector2(-16f, 0f);
                progRt.sizeDelta = new Vector2(120f, 16f);

                //# 배경 트랙 — 진행 바 미충전 부분.
                GameObject trackGo = new GameObject("Track", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform trackRt = trackGo.GetComponent<RectTransform>();
                trackRt.SetParent(progRt, false);
                StretchFill(trackRt);
                Image trackImg = trackGo.GetComponent<Image>();
                trackImg.color = new Color(0.122f, 0.161f, 0.216f, 1f);
                trackImg.raycastTarget = false;

                //# _progressFill — Image type=Filled / Horizontal / Origin=Left.
                GameObject fillGo = new GameObject("Fill", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                RectTransform fillRt = fillGo.GetComponent<RectTransform>();
                fillRt.SetParent(progRt, false);
                StretchFill(fillRt);
                Image fillImg = fillGo.GetComponent<Image>();
                fillImg.color = new Color(0.984f, 0.749f, 0.141f, 1f);
                fillImg.raycastTarget = false;
                fillImg.type = Image.Type.Filled;
                fillImg.fillMethod = Image.FillMethod.Horizontal;
                fillImg.fillOrigin = (int)Image.OriginHorizontal.Left;
                fillImg.fillAmount = 0f;
                //# 스프라이트 없는 Filled Image 는 fillAmount 가 무시돼 항상 꽉 차 보임 — 기본 UISprite 할당 필수.
                fillImg.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");

                //# _progressText — "N/M" CHText 동행.
                CHText progText = CreateLabel(progRt, "ProgressText", font);
                RectTransform textRt = progText.transform as RectTransform;
                StretchFill(textRt);
                TMP_Text tmp = progText.GetComponent<TMP_Text>();
                tmp.text = "0/0";
                tmp.fontSize = 11f;
                tmp.color = Color.white;
                tmp.alignment = TextAlignmentOptions.Center;

                rootProp.objectReferenceValue = rootGo;
                fillProp.objectReferenceValue = fillImg;
                textProp.objectReferenceValue = progText;
                so.ApplyModifiedPropertiesWithoutUndo();

                //# 기본은 숨김 — 런타임 Bind 가 HasProgress 로 토글.
                rootGo.SetActive(false);

                Save(root, QuestCellPath);
                Debug.Log("[WireVillageVisibilityNodes] QuestCell 진행 바 노드 3종 추가 + 연결 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        //# ── 헬퍼 ───────────────────────────────────────────────────────────
        //# TMP_Text 먼저, CHText 나중 — CHText 는 [RequireComponent(typeof(TMP_Text))]. _stringID = -1 동적.
        private static CHText CreateLabel(Transform parent, string name, TMP_FontAsset font)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            (go.transform as RectTransform).SetParent(parent, false);

            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.raycastTarget = false;

            CHText chText = go.AddComponent<CHText>();
            SerializedObject so = new SerializedObject(chText);
            SerializedProperty stringId = so.FindProperty("_stringID");
            if (stringId != null)
            {
                stringId.intValue = -1;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return chText;
        }

        private static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static TMP_FontAsset LoadFont()
        {
            string path = AssetDatabase.GUIDToAssetPath(FontGuid);
            if (string.IsNullOrEmpty(path))
                return null;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
        }

        private static void Save(GameObject root, string path)
        {
            bool success = false;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);
            if (success == false)
            {
                Debug.LogError($"[WireVillageVisibilityNodes] 저장 실패: {Path.GetFileName(path)}");
            }
        }
    }
}
