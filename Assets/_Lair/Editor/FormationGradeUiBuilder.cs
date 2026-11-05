using System.Linq;
using ChvjUnityInfra;
using Lair.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Lair.EditorTools
{
    //# 일회용 에디터 툴(Rule 04 §3) — 종족·등급 개편의 UI 프리팹 배선(dungeon-formation §0.10, monster-grades §11).
    //# 둥지 칸(코스트 → 종족·등급 줄) · 몬스터 행(코스트/횟수 → 등급 줄 + 출전 외곽선) · 편성 팝업(코스트 게이지 제거 + 종족 정보 행 + 몬스터 행 5) · 상점 종족 행 5번째 승급 칸.
    //# 실행 후 정상 확인되면 이 스크립트는 삭제한다(단일 진실 = 수정된 프리팹).
    public static class FormationGradeUiBuilder
    {
        private const string MenuPath = "Lair/Build Formation Grade UI (Run Once, Then Delete)";
        private const string UiDir = "Assets/_Lair/Art/UI/";

        private static readonly Color RaceBg = new Color32(0x1c, 0x1f, 0x35, 0xff);

        [MenuItem(MenuPath)]
        public static void BuildAll()
        {
            GameObject outlineSource = BuildNestTile();
            BuildSpeciesRow(outlineSource);
            Object.DestroyImmediate(outlineSource);
            BuildPopup();
            BuildShopCell();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[FormationGradeUiBuilder] 완료 — FormationNestTile / FormationSpeciesRow / FormationPopup / ShopItemCell 수정. 정상 확인 후 이 스크립트를 삭제하세요(Rule 04 §3).");
        }

        //# 둥지 칸 136x200 — 이름(116) · 종족(144, 기존 코스트 줄 재사용) · 등급 줄(164, 기존 코스트 칸 자리). 선택 외곽선 사본을 돌려준다(행에서 재사용).
        private static GameObject BuildNestTile()
        {
            string path = UiDir + "FormationNestTile.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            FormationNestTile tile = root.GetComponent<FormationNestTile>();

            Transform raceText = root.transform.Find("CostText");
            raceText.name = "RaceText";
            Transform pips = root.transform.Find("CostPips");
            if (pips != null)
            {
                Object.DestroyImmediate(pips.gameObject);
            }
            GameObject gradeText = Object.Instantiate(raceText.gameObject, root.transform, false);
            gradeText.name = "GradeText";
            RectTransform gradeRect = (RectTransform)gradeText.transform;
            gradeRect.anchoredPosition = new Vector2(0f, -164f);
            ((RectTransform)raceText).anchoredPosition = new Vector2(0f, -140f);
            SetFontSize(gradeText, 11);

            SetRef(tile, "_raceText", raceText.GetComponent<CHText>());
            SetRef(tile, "_gradeText", gradeText.GetComponent<CHText>());

            GameObject outline = Object.Instantiate(root.transform.Find("SelectedOutline").gameObject);
            outline.name = "SelectedOutline";
            outline.SetActive(true);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            return outline;
        }

        //# 몬스터 행 332x72 — 코스트 줄 → 등급 줄(너비 확장) · 편성 횟수 줄 삭제 · 출전 중 외곽선 추가.
        private static void BuildSpeciesRow(GameObject outlineSource)
        {
            string path = UiDir + "FormationSpeciesRow.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            FormationSpeciesRow row = root.GetComponent<FormationSpeciesRow>();

            Transform grade = root.transform.Find("CostText");
            grade.name = "GradeText";
            ((RectTransform)grade).sizeDelta = new Vector2(250f, 16f);
            Transform count = root.transform.Find("CountText");
            if (count != null)
            {
                Object.DestroyImmediate(count.gameObject);
            }

            GameObject outline = Object.Instantiate(outlineSource, root.transform, false);
            outline.name = "SelectedOutline";
            outline.SetActive(false);
            outline.transform.SetAsFirstSibling();

            SetRef(row, "_gradeText", grade.GetComponent<CHText>());
            SetRef(row, "_selectedOutline", outline);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        //# 팝업 — 코스트 게이지 제거, 우측 패널 = 종족 정보 행(332x72) + 몬스터 행 5(각 72, 간격 8).
        private static void BuildPopup()
        {
            string path = UiDir + "FormationPopup.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            FormationPopup popup = root.GetComponent<FormationPopup>();
            Transform panel = root.transform.Find("Panel");

            Transform gauge = panel.Find("CostGauge");
            if (gauge != null)
            {
                Object.DestroyImmediate(gauge.gameObject);
            }

            FormationSpeciesRow[] rows = root.GetComponentsInChildren<FormationSpeciesRow>(true);
            for (int i = rows.Length - 1; i >= 5; --i)
            {
                Object.DestroyImmediate(rows[i].gameObject);
            }
            rows = root.GetComponentsInChildren<FormationSpeciesRow>(true);
            for (int i = 0; i < rows.Length; ++i)
            {
                RectTransform rect = (RectTransform)rows[i].transform;
                rect.anchoredPosition = new Vector2(788f, -(160f + 80f * i));
            }

            //# 종족 정보 행 — 종족색 테두리(뒤) + 배경 + 이름·성격 문구(요약 패널의 동적 라벨 복제).
            Transform summary = panel.Find("Summary");
            GameObject border = NewImage("RaceBorder", panel, new Vector2(785f, -77f), new Vector2(338f, 78f), Color.white);
            GameObject info = NewImage("RaceInfo", panel, new Vector2(788f, -80f), new Vector2(332f, 72f), RaceBg);
            GameObject nameText = Object.Instantiate(summary.Find("Hint").gameObject, info.transform, false);
            nameText.name = "RaceName";
            Place(nameText, new Vector2(12f, -6f), new Vector2(308f, 24f));
            SetFontSize(nameText, 17);
            GameObject descText = Object.Instantiate(summary.Find("Hint").gameObject, info.transform, false);
            descText.name = "RaceDesc";
            Place(descText, new Vector2(12f, -30f), new Vector2(308f, 40f));
            SetFontSize(descText, 12);

            SerializedObject so = new SerializedObject(popup);
            SerializedProperty array = so.FindProperty("_speciesRows");
            array.arraySize = rows.Length;
            for (int i = 0; i < rows.Length; ++i)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = rows[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            SetRef(popup, "_raceNameText", nameText.GetComponent<CHText>());
            SetRef(popup, "_raceDescText", descText.GetComponent<CHText>());
            SetRef(popup, "_raceBorder", border.GetComponent<Image>());

            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        //# 상점 종족 행(660x84) — 머리 80 뒤 칸 5개를 같은 폭으로: (660 - 84) = 576 = 5 x 112 + 4 x 4. 5번째 = 4번째 칸 복제(승급 칸).
        private static void BuildShopCell()
        {
            string path = UiDir + "ShopItemCell.prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            ShopItemCell cell = root.GetComponent<ShopItemCell>();
            Transform speciesRow = root.transform.Find("SpeciesRow");
            Transform slot3 = speciesRow.Find("AxisSlot3");

            GameObject slot4 = Object.Instantiate(slot3.gameObject, speciesRow, false);
            slot4.name = "AxisSlot4";
            ShopAxisSlotView[] slots = new ShopAxisSlotView[5];
            for (int i = 0; i < 4; ++i)
            {
                slots[i] = speciesRow.Find("AxisSlot" + i).GetComponent<ShopAxisSlotView>();
            }
            slots[4] = slot4.GetComponent<ShopAxisSlotView>();
            for (int i = 0; i < 5; ++i)
            {
                RectTransform rect = (RectTransform)slots[i].transform;
                rect.anchoredPosition = new Vector2(84f + 116f * i, -6f);
                rect.sizeDelta = new Vector2(112f, 72f);
            }

            SerializedObject so = new SerializedObject(cell);
            SerializedProperty array = so.FindProperty("_axisSlots");
            array.arraySize = slots.Length;
            for (int i = 0; i < slots.Length; ++i)
            {
                array.GetArrayElementAtIndex(i).objectReferenceValue = slots[i];
            }
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        //# ---- 헬퍼 ----

        private static GameObject NewImage(string name, Transform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            Place(go, pos, size);
            Image image = go.GetComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return go;
        }

        private static void Place(GameObject go, Vector2 pos, Vector2 size)
        {
            RectTransform rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
        }

        private static void SetFontSize(GameObject go, float size)
        {
            TMPro.TMP_Text text = go.GetComponent<TMPro.TMP_Text>();
            if (text != null)
            {
                text.fontSize = size;
            }
        }

        private static void SetRef(Object target, string field, Object value)
        {
            SerializedObject so = new SerializedObject(target);
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[FormationGradeUiBuilder] 필드 없음: {target.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
