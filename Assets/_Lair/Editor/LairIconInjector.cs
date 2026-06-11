using Lair.UI;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools
{
    //# 도감 몬스터 아이콘 + 영웅 초상 선별 주입 — V2 전체 재생성 없이 프리팹 3종만 갱신 (V2a ResultPopup 관례).
    //# V2(Build Village UI Prefabs) 재실행 시 본 주입이 덮어써지므로, V2 이후 본 메뉴를 재실행해야 한다.
    public static class LairIconInjector
    {
        private const string PrefabDir = "Assets/_Lair/Art/UI";
        private const string MonsterIconDir = "Assets/_Lair/Art/Sprites/MonsterIcons";
        private const string KnightPortraitPath = "Assets/_Lair/Art/Sprites/HeroIcons/Knight.png";

        [MenuItem("Lair/Setup/V2b - Inject Codex Monster Icons + Knight Portrait")]
        public static void InjectAll()
        {
            InjectCodexMonsterIcons();
            InjectHeroSelectCellPortraitNode();
            InjectHeroSelectPopupPortrait();
            AssetDatabase.SaveAssets();
            Debug.Log("[LairIconInjector] V2b 완료 — CodexPopup 아이콘 6종 + HeroSelectCell Portrait 노드 + HeroSelectPopup Knight 초상 주입");
        }

        //# CodexPopup — 종별 아이콘 6종 직렬화 참조 주입 (SpawnerStatusCell 관례, Addressables 키 아님).
        private static void InjectCodexMonsterIcons()
        {
            string path = $"{PrefabDir}/CodexPopup.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null)
            {
                Debug.LogError("[LairIconInjector] CodexPopup.prefab 미발견 — 아이콘 주입 스킵");
                return;
            }

            try
            {
                CodexPopup popup = contents.GetComponent<CodexPopup>();
                if (popup == null)
                {
                    Debug.LogError("[LairIconInjector] CodexPopup 컴포넌트 미발견");
                    return;
                }

                SerializedObject so = new SerializedObject(popup);
                SetSprite(so, "_wispIcon", $"{MonsterIconDir}/Wisp.png");
                SetSprite(so, "_wraithIcon", $"{MonsterIconDir}/Wraith.png");
                SetSprite(so, "_reaperIcon", $"{MonsterIconDir}/Reaper.png");
                SetSprite(so, "_hexIcon", $"{MonsterIconDir}/Hex.png");
                SetSprite(so, "_plagueIcon", $"{MonsterIconDir}/Plague.png");
                SetSprite(so, "_phantomIcon", $"{MonsterIconDir}/Phantom.png");
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[LairIconInjector] CodexPopup.prefab — 몬스터 아이콘 6종 주입 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        //# HeroSelectCell — Portrait Image 노드 추가(멱등) + _portrait 참조 주입.
        //# 다른 노드(Border/NameText)는 건드리지 않는다 — 잠금 더미 표시는 셀 코드가 Portrait 숨김으로 유지.
        private static void InjectHeroSelectCellPortraitNode()
        {
            string path = $"{PrefabDir}/HeroSelectCell.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null)
            {
                Debug.LogError("[LairIconInjector] HeroSelectCell.prefab 미발견 — Portrait 노드 주입 스킵");
                return;
            }

            try
            {
                HeroSelectCell cell = contents.GetComponent<HeroSelectCell>();
                if (cell == null)
                {
                    Debug.LogError("[LairIconInjector] HeroSelectCell 컴포넌트 미발견");
                    return;
                }

                Transform existing = contents.transform.Find("Portrait");
                Image portrait;
                if (existing != null)
                {
                    portrait = existing.GetComponent<Image>();
                }
                else
                {
                    GameObject portraitGo = new GameObject("Portrait", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    portraitGo.transform.SetParent(contents.transform, false);
                    portraitGo.transform.SetSiblingIndex(0);   //# 배경 위·Border/NameText 아래 그리기
                    RectTransform rt = (RectTransform)portraitGo.transform;
                    rt.anchorMin = new Vector2(0.5f, 1f);
                    rt.anchorMax = new Vector2(0.5f, 1f);
                    rt.pivot = new Vector2(0.5f, 1f);
                    rt.anchoredPosition = new Vector2(0f, -12f);
                    rt.sizeDelta = new Vector2(110f, 110f);
                    portrait = portraitGo.GetComponent<Image>();
                    portrait.preserveAspect = true;
                    portrait.raycastTarget = false;
                }

                SerializedObject so = new SerializedObject(cell);
                SerializedProperty prop = so.FindProperty("_portrait");
                if (prop != null)
                {
                    prop.objectReferenceValue = portrait;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    Debug.LogWarning("[LairIconInjector] HeroSelectCell._portrait 필드 미발견");
                }

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[LairIconInjector] HeroSelectCell.prefab — Portrait 노드 + _portrait 참조 주입 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        //# HeroSelectPopup — Knight 초상 직렬화 참조 주입.
        private static void InjectHeroSelectPopupPortrait()
        {
            string path = $"{PrefabDir}/HeroSelectPopup.prefab";
            GameObject contents = PrefabUtility.LoadPrefabContents(path);
            if (contents == null)
            {
                Debug.LogError("[LairIconInjector] HeroSelectPopup.prefab 미발견 — 초상 주입 스킵");
                return;
            }

            try
            {
                HeroSelectPopup popup = contents.GetComponent<HeroSelectPopup>();
                if (popup == null)
                {
                    Debug.LogError("[LairIconInjector] HeroSelectPopup 컴포넌트 미발견");
                    return;
                }

                SerializedObject so = new SerializedObject(popup);
                SetSprite(so, "_knightPortrait", KnightPortraitPath);
                so.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(contents, path);
                Debug.Log("[LairIconInjector] HeroSelectPopup.prefab — Knight 초상 주입 완료");
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private static void SetSprite(SerializedObject so, string field, string assetPath)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogWarning($"[LairIconInjector] 필드 미발견: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null)
            {
                Debug.LogWarning($"[LairIconInjector] 스프라이트 미발견: {assetPath}");
                return;
            }
            prop.objectReferenceValue = sprite;
        }
    }
}
