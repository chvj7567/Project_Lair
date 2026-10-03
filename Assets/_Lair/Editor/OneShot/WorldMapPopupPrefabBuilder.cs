using ChvjUnityInfra;
using Lair.UI;
using TMPro;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.EditorTools.OneShot
{
    //# 일회용 프리팹 빌더 (Rule 04 §3) — 챕터 월드맵 프리팹 3종을 한 번 생성한 뒤 삭제한다. 기획서 chapter-system §6 · 승인 목업 .mockups/chapter-worldmap.html.
    //# 생성: WorldMapNode.prefab(노드 1칸) / WorldMapChapterTab.prefab(탭 1칸) / WorldMapPopup.prefab(EUI.WorldMapPopup, 노드·탭 인스턴스를 정적 배치).
    //# 기존 에셋은 절대 덮어쓰지 않는다 — 대상 경로에 이미 파일이 있으면 그 프리팹은 건너뛰고 LogError. WorldMapPopup 은 Addressables(라벨 Resource) 등록까지 수행.
    public static class WorldMapPopupPrefabBuilder
    {
        private const string ArtUiDir = "Assets/_Lair/Art/UI/";
        private const string NodePath = ArtUiDir + "WorldMapNode.prefab";
        private const string TabPath = ArtUiDir + "WorldMapChapterTab.prefab";
        private const string PopupPath = ArtUiDir + "WorldMapPopup.prefab";
        private const string SpriteDir = "Assets/_Lair/Art/Sprites/UiDot/";
        private const string FontGuid = "12e8e80fec3a8554fa63474df537b505";   //# NotoSansKR SDF
        private const string AddressableGroup = "Default Local Group";
        private const string AddressableLabel = "Resource";

        //# 팔레트 (목업 :root).
        private static readonly Color Bone = new Color(0.910f, 0.882f, 0.812f, 1f);
        private static readonly Color Sub = new Color(0.557f, 0.596f, 0.678f, 1f);
        private static readonly Color Gold = new Color(0.969f, 0.776f, 0.290f, 1f);
        private static readonly Color Blood = new Color(0.898f, 0.282f, 0.302f, 1f);
        private static readonly Color Txt = new Color(0.933f, 0.945f, 0.965f, 1f);

        [MenuItem("Lair/OneShot/Build WorldMap Prefabs")]
        public static void Run()
        {
            TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(AssetDatabase.GUIDToAssetPath(FontGuid));
            if (font == null)
            {
                Debug.LogError("[WorldMapPopupPrefabBuilder] NotoSansKR SDF 폰트를 찾지 못함 - 중단");
                return;
            }

            GameObject nodePrefab = BuildNode(font);
            GameObject tabPrefab = BuildTab(font);
            if (nodePrefab == null || tabPrefab == null)
            {
                Debug.LogError("[WorldMapPopupPrefabBuilder] 노드/탭 프리팹 확보 실패 - 팝업 생성 중단");
                return;
            }
            BuildPopup(font, nodePrefab, tabPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WorldMapPopupPrefabBuilder] 완료 - 프리팹 확인 후 이 빌더 파일을 삭제하세요(Rule 04 §3).");
        }

        //# ---- 노드 ----
        private static GameObject BuildNode(TMP_FontAsset font)
        {
            if (Exists(NodePath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(NodePath);

            GameObject root = NewUi("WorldMapNode", null);
            RectTransform rt = Rt(root);
            Place(rt, 0, 0, 72, 72);
            Image bg = AddImage(root, LoadSprite("Px_Btn"), Color.white);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = bg;
            CHButton chButton = root.AddComponent<CHButton>();

            CHText label = AddText(root.transform, "Label", font, "1-1", 22, Txt, TextAlignmentOptions.Center, -1);
            Stretch(Rt(label.gameObject), 0, 0, 0, 0);

            GameObject bossTag = NewUi("BossTag", root.transform);
            AnchorTopCenter(Rt(bossTag), 96, 16, 0, 4, 0f);
            CHText bossText = AddText(bossTag.transform, "Text", font, "BOSS", 12, Gold, TextAlignmentOptions.Center, -1);
            Stretch(Rt(bossText.gameObject), 0, 0, 0, 0);

            GameObject marker = NewUi("SelectedMarker", root.transform);
            Stretch(Rt(marker), -6, -6, -6, -6);
            Image outline = AddImage(marker, LoadSprite("Px_CardRing"), Gold);
            outline.raycastTarget = false;
            GameObject arrowGo = NewUi("Arrow", marker.transform);
            AnchorTopCenter(Rt(arrowGo), 40, 16, 0, 22, 0f);
            CHText arrow = AddText(arrowGo.transform, "Text", font, "▼", 14, Gold, TextAlignmentOptions.Center, -1);
            Stretch(Rt(arrow.gameObject), 0, 0, 0, 0);

            CHText nameText = AddText(root.transform, "NameText", font, "???", 13, Txt, TextAlignmentOptions.Top, -1);
            AnchorBottomCenter(Rt(nameText.gameObject), 144, 32, 0, -2);
            nameText.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;

            CHText clearedTag = AddText(root.transform, "ClearedTag", font, "클리어", 12, Gold, TextAlignmentOptions.Top, 1308);
            AnchorBottomCenter(Rt(clearedTag.gameObject), 144, 16, 0, -36);

            WorldMapNodeView view = root.AddComponent<WorldMapNodeView>();
            SerializedObject so = new SerializedObject(view);
            SetRef(so, "_button", chButton);
            SetRef(so, "_background", bg);
            SetRef(so, "_labelText", label);
            SetRef(so, "_nameText", nameText);
            SetRef(so, "_clearedTagText", clearedTag);
            SetRef(so, "_bossTag", bossTag);
            SetRef(so, "_selectedMarker", marker);
            SetRef(so, "_openSprite", LoadSprite("Px_Btn"));
            SetRef(so, "_bossSprite", LoadSprite("Px_BtnBlood"));
            SetRef(so, "_clearedSprite", LoadSprite("Px_BtnGold"));
            SetRef(so, "_lockedSprite", LoadSprite("Px_BtnOff"));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, NodePath);
        }

        //# ---- 챕터 탭 ----
        private static GameObject BuildTab(TMP_FontAsset font)
        {
            if (Exists(TabPath))
                return AssetDatabase.LoadAssetAtPath<GameObject>(TabPath);

            GameObject root = NewUi("WorldMapChapterTab", null);
            Place(Rt(root), 0, 0, 200, 44);
            Image bg = AddImage(root, LoadSprite("Px_Btn"), Color.white);
            Button button = root.AddComponent<Button>();
            button.targetGraphic = bg;
            CHButton chButton = root.AddComponent<CHButton>();

            CHText label = AddText(root.transform, "Label", font, "1장 지하 묘지", 17, Sub, TextAlignmentOptions.Center, -1);
            Stretch(Rt(label.gameObject), 6, 0, 6, 8);

            CHText lockTag = AddText(root.transform, "LockTag", font, "잠금", 12, Sub, TextAlignmentOptions.BottomRight, -1);
            Stretch(Rt(lockTag.gameObject), 6, 2, 8, 0);

            WorldMapChapterTabView view = root.AddComponent<WorldMapChapterTabView>();
            SerializedObject so = new SerializedObject(view);
            SetRef(so, "_button", chButton);
            SetRef(so, "_background", bg);
            SetRef(so, "_labelText", label);
            SetRef(so, "_lockTagText", lockTag);
            SetRef(so, "_selectedSprite", LoadSprite("Px_BtnGold"));
            SetRef(so, "_normalSprite", LoadSprite("Px_Btn"));
            SetRef(so, "_lockedSprite", LoadSprite("Px_BtnOff"));
            so.ApplyModifiedPropertiesWithoutUndo();

            return SavePrefab(root, TabPath);
        }

        //# ---- 팝업 ----
        private static void BuildPopup(TMP_FontAsset font, GameObject nodePrefab, GameObject tabPrefab)
        {
            if (Exists(PopupPath))
                return;

            //# 기준 좌표 1280x720, 좌상단 원점 (목업과 동일). 모든 자식은 루트 좌상단 기준 절대 배치.
            GameObject root = NewUi("WorldMapPopup", null);
            Stretch(Rt(root), 0, 0, 0, 0);
            WorldMapPopup popup = root.AddComponent<WorldMapPopup>();

            //# 딤 (탭하면 닫기)
            GameObject dim = NewUi("Dim", root.transform);
            Stretch(Rt(dim), 0, 0, 0, 0);
            Image dimImage = AddImage(dim, null, new Color(0f, 0f, 0f, 0.62f));
            dim.AddComponent<Button>().targetGraphic = dimImage;
            CHButton dimButton = dim.AddComponent<CHButton>();

            const float PX = 96f, PY = 48f, PW = 1088f, PH = 624f;
            GameObject panel = NewUi("Panel", root.transform);
            Place(Rt(panel), PX, PY, PW, PH);
            Image panelImage = AddImage(panel, LoadSprite("Px_Panel"), Color.white);
            panelImage.type = Image.Type.Sliced;
            //# 패널 클릭이 딤으로 새지 않게 raycast 를 받는다(Image 기본 raycastTarget=true).

            //# 타이틀 바
            CHText title = AddText(panel.transform, "Title", font, "침입 지도", 30, Bone, TextAlignmentOptions.Left, 1301);
            Place(Rt(title.gameObject), 24, 12, 200, 40);
            CHText tagline = AddText(panel.transform, "Tagline", font, "챕터를 고르고 도전할 스테이지를 선택합니다", 13, Sub, TextAlignmentOptions.Left, 1326);
            Place(Rt(tagline.gameObject), 230, 24, 600, 20);

            GameObject closeGo = NewUi("CloseButton", panel.transform);
            Place(Rt(closeGo), PW - 64f, 8, 48, 48);
            Image closeImage = AddImage(closeGo, LoadSprite("Px_CloseX"), Color.white);
            closeGo.AddComponent<Button>().targetGraphic = closeImage;
            CHButton closeButton = closeGo.AddComponent<CHButton>();

            //# 챕터 탭 5개 — 목업: x=PX+16+k*208, y=PY+80
            WorldMapChapterTabView[] tabs = new WorldMapChapterTabView[5];
            for (int k = 0; k < tabs.Length; ++k)
            {
                GameObject tab = PrefabUtility.InstantiatePrefab(tabPrefab, panel.transform) as GameObject;
                tab.name = "ChapterTab" + (k + 1);
                Place(Rt(tab), 16f + k * 208f, 80f, 200, 44);
                tabs[k] = tab.GetComponent<WorldMapChapterTabView>();
            }

            //# 지도 영역(좌): panel-relative (16, 140), 640 x 392
            GameObject map = NewUi("MapArea", panel.transform);
            Place(Rt(map), 16, 140, 640, 392);
            AddImage(map, LoadSprite("Px_PanelDark"), Color.white).type = Image.Type.Sliced;
            CHText chapterTitle = AddText(map.transform, "ChapterTitle", font, "1장 지하 묘지", 20, Bone, TextAlignmentOptions.Left, -1);
            Place(Rt(chapterTitle.gameObject), 16, 8, 420, 28);
            CHText chapterTheme = AddText(map.transform, "ChapterTheme", font, "", 13, Sub, TextAlignmentOptions.Left, -1);
            Place(Rt(chapterTheme.gameObject), 16, 36, 420, 20);

            //# 노드 5개 — 목업 좌표 [[32,200],[150,120],[268,200],[386,120],[504,164]] (+40). 보스(5번째)는 96px.
            float[,] pos = { { 32, 240 }, { 150, 160 }, { 268, 240 }, { 386, 160 }, { 504, 204 } };
            WorldMapNodeView[] nodes = new WorldMapNodeView[5];
            for (int i = 0; i < nodes.Length; ++i)
            {
                GameObject node = PrefabUtility.InstantiatePrefab(nodePrefab, map.transform) as GameObject;
                node.name = "Node" + (i + 1);
                float size = i == 4 ? 96f : 72f;
                Place(Rt(node), pos[i, 0], pos[i, 1], size, size);
                nodes[i] = node.GetComponent<WorldMapNodeView>();
            }

            //# 상세 패널(우): panel-relative (672, 140), 400 x 392
            GameObject detail = NewUi("DetailPanel", panel.transform);
            Place(Rt(detail), 672, 140, 400, 392);
            AddImage(detail, LoadSprite("Px_PanelSunk"), Color.white).type = Image.Type.Sliced;

            GameObject portraitFrame = NewUi("PortraitFrame", detail.transform);
            Place(Rt(portraitFrame), 16, 16, 112, 112);
            AddImage(portraitFrame, LoadSprite("Px_PanelDark"), Color.white).type = Image.Type.Sliced;
            GameObject portraitGo = NewUi("Portrait", portraitFrame.transform);
            Stretch(Rt(portraitGo), 8, 8, 8, 8);
            Image portrait = AddImage(portraitGo, null, Color.white);
            portrait.preserveAspect = true;
            portrait.raycastTarget = false;
            GameObject lockOverlay = NewUi("LockOverlay", portraitFrame.transform);
            Stretch(Rt(lockOverlay), 0, 0, 0, 0);
            AddImage(lockOverlay, null, new Color(0f, 0f, 0f, 0.65f)).raycastTarget = false;
            CHText lockLabel = AddText(lockOverlay.transform, "Text", font, "잠금", 20, Bone, TextAlignmentOptions.Center, 1309);
            Stretch(Rt(lockLabel.gameObject), 0, 0, 0, 0);

            CHText number = AddText(detail.transform, "Number", font, "1-1", 16, Gold, TextAlignmentOptions.Left, -1);
            Place(Rt(number.gameObject), 144, 12, 150, 20);
            GameObject bossBadge = NewUi("BossBadge", detail.transform);
            Place(Rt(bossBadge), 300, 12, 84, 20);
            CHText bossBadgeText = AddText(bossBadge.transform, "Text", font, "BOSS", 14, Blood, TextAlignmentOptions.Left, -1);
            Stretch(Rt(bossBadgeText.gameObject), 0, 0, 0, 0);
            CHText stageName = AddText(detail.transform, "StageName", font, "", 22, Bone, TextAlignmentOptions.TopLeft, -1);
            Place(Rt(stageName.gameObject), 144, 34, 240, 40);
            stageName.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;
            CHText stars = AddText(detail.transform, "Stars", font, "", 18, Gold, TextAlignmentOptions.Left, -1);
            Place(Rt(stars.gameObject), 144, 78, 240, 24);
            CHText hero = AddText(detail.transform, "Hero", font, "", 13, Sub, TextAlignmentOptions.Left, -1);
            Place(Rt(hero.gameObject), 144, 104, 240, 20);

            CHText recordLabel = AddText(detail.transform, "RecordLabel", font, "내 기록", 13, Sub, TextAlignmentOptions.Left, 1305);
            Place(Rt(recordLabel.gameObject), 16, 140, 368, 18);
            CHText record = AddText(detail.transform, "Record", font, "", 16, Txt, TextAlignmentOptions.Left, -1);
            Place(Rt(record.gameObject), 16, 158, 368, 24);

            CHText rulesLabel = AddText(detail.transform, "RulesLabel", font, "스테이지 규칙", 13, Sub, TextAlignmentOptions.Left, 1303);
            Place(Rt(rulesLabel.gameObject), 16, 190, 368, 18);
            CHText rules = AddText(detail.transform, "Rules", font, "", 13, Txt, TextAlignmentOptions.TopLeft, -1);
            Place(Rt(rules.gameObject), 16, 210, 368, 112);
            rules.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;

            GameObject bossPhaseRoot = NewUi("BossPhaseRoot", detail.transform);
            Place(Rt(bossPhaseRoot), 16, 270, 368, 116);
            CHText bossPhaseLabel = AddText(bossPhaseRoot.transform, "Label", font, "보스 페이즈", 13, Blood, TextAlignmentOptions.Left, 1304);
            Place(Rt(bossPhaseLabel.gameObject), 0, 0, 368, 18);
            CHText bossPhase = AddText(bossPhaseRoot.transform, "Text", font, "", 13, Txt, TextAlignmentOptions.TopLeft, -1);
            Place(Rt(bossPhase.gameObject), 0, 20, 368, 96);
            bossPhase.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;

            //# 하단: 안내 문구(좌) + 선택 버튼(우, 상세 폭 x 56)
            CHText bottom = AddText(panel.transform, "BottomText", font, "", 13, Sub, TextAlignmentOptions.TopLeft, -1);
            Place(Rt(bottom.gameObject), 24, 566, 640, 44);
            bottom.GetComponent<TMP_Text>().textWrappingMode = TextWrappingModes.Normal;

            GameObject selectGo = NewUi("SelectButton", panel.transform);
            Place(Rt(selectGo), 688, 552, 368, 56);
            Image selectImage = AddImage(selectGo, LoadSprite("Px_BtnBlood"), Color.white);
            selectGo.AddComponent<Button>().targetGraphic = selectImage;
            CHButton selectButton = selectGo.AddComponent<CHButton>();
            CHText selectLabel = AddText(selectGo.transform, "Label", font, "이 스테이지로 선택", 22, Txt, TextAlignmentOptions.Center, 1307);
            Stretch(Rt(selectLabel.gameObject), 0, 0, 0, 0);

            //# 와이어링 — WorldMapPopup 의 [SerializeField] private 전부
            SerializedObject so = new SerializedObject(popup);
            SetRef(so, "_dimButton", dimButton);
            SetRef(so, "_closeButton", closeButton);
            SetRefArray(so, "_tabs", tabs);
            SetRefArray(so, "_nodes", nodes);
            SetRef(so, "_chapterTitleText", chapterTitle);
            SetRef(so, "_chapterThemeText", chapterTheme);
            SetRef(so, "_detailPortrait", portrait);
            SetRef(so, "_portraitLockOverlay", lockOverlay);
            SetRef(so, "_detailNumberText", number);
            SetRef(so, "_detailBossBadge", bossBadge);
            SetRef(so, "_detailNameText", stageName);
            SetRef(so, "_detailStarsText", stars);
            SetRef(so, "_detailHeroText", hero);
            SetRef(so, "_detailRecordText", record);
            SetRef(so, "_detailRulesText", rules);
            SetRef(so, "_bossPhaseRoot", bossPhaseRoot);
            SetRef(so, "_bossPhaseText", bossPhase);
            SetRef(so, "_bottomText", bottom);
            SetRef(so, "_selectButton", selectButton);
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject saved = SavePrefab(root, PopupPath);
            RegisterAddressable(saved);
        }

        //# ---- 헬퍼 ----
        private static bool Exists(string path)
        {
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            if (exists)
            {
                Debug.LogError($"[WorldMapPopupPrefabBuilder] 이미 존재 - 덮어쓰지 않고 건너뜀: {path}");
            }
            return exists;
        }

        private static GameObject SavePrefab(GameObject root, string path)
        {
            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
            return saved;
        }

        private static void RegisterAddressable(GameObject prefab)
        {
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("[WorldMapPopupPrefabBuilder] Addressables 설정 없음 - WorldMapPopup 수동 등록 필요");
                return;
            }
            AddressableAssetGroup group = settings.FindGroup(AddressableGroup);
            if (group == null)
            {
                Debug.LogError($"[WorldMapPopupPrefabBuilder] Addressables 그룹 '{AddressableGroup}' 없음 - 수동 등록 필요");
                return;
            }
            string guid = AssetDatabase.AssetPathToGUID(PopupPath);
            AddressableAssetEntry entry = settings.CreateOrMoveEntry(guid, group);
            entry.address = "WorldMapPopup";
            entry.SetLabel(AddressableLabel, true);
            EditorUtility.SetDirty(settings);
        }

        private static Sprite LoadSprite(string name)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpriteDir + name + ".png");
            if (sprite == null)
            {
                Debug.LogError($"[WorldMapPopupPrefabBuilder] 스프라이트 없음: {name}");
            }
            return sprite;
        }

        private static GameObject NewUi(string name, Transform parent)
        {
            GameObject go = new GameObject(name, typeof(RectTransform));
            if (parent != null)
            {
                go.transform.SetParent(parent, false);
            }
            return go;
        }

        private static RectTransform Rt(GameObject go)
        {
            return go.GetComponent<RectTransform>();
        }

        //# 좌상단 앵커/피벗 기준 절대 배치 (x → 오른쪽, y → 아래).
        private static void Place(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        //# 부모 전체 채움 + 좌/상/우/하 안쪽 여백(음수 = 바깥).
        private static void Stretch(RectTransform rt, float left, float top, float right, float bottom)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }

        private static void AnchorTopCenter(RectTransform rt, float w, float h, float x, float y, float pivotY)
        {
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, pivotY);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static void AnchorBottomCenter(RectTransform rt, float w, float h, float x, float y)
        {
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = new Vector2(w, h);
        }

        private static Image AddImage(GameObject go, Sprite sprite, Color color)
        {
            go.AddComponent<CanvasRenderer>();
            Image image = go.AddComponent<Image>();
            image.sprite = sprite;
            image.color = color;
            return image;
        }

        //# TMP + CHText. stringId != -1 이면 CHText._stringID 를 세팅(런타임에 Strings_Ko 로 덮음), 에디터 미리보기용 literal 도 함께 넣는다. 정적 라벨도 CHText 필수(Rule 03 §3).
        private static CHText AddText(Transform parent, string name, TMP_FontAsset font, string literal, float size, Color color, TextAlignmentOptions align, int stringId)
        {
            GameObject go = NewUi(name, parent);
            go.AddComponent<CanvasRenderer>();
            TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.font = font;
            tmp.text = literal;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = align;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;

            CHText chText = go.AddComponent<CHText>();
            if (stringId != -1)
            {
                SerializedObject so = new SerializedObject(chText);
                so.FindProperty("_stringID").intValue = stringId;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            return chText;
        }

        private static void SetRef(SerializedObject so, string field, Object value)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[WorldMapPopupPrefabBuilder] 필드 없음: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.objectReferenceValue = value;
        }

        private static void SetRefArray(SerializedObject so, string field, Object[] values)
        {
            SerializedProperty prop = so.FindProperty(field);
            if (prop == null)
            {
                Debug.LogError($"[WorldMapPopupPrefabBuilder] 필드 없음: {so.targetObject.GetType().Name}.{field}");
                return;
            }
            prop.arraySize = values.Length;
            for (int i = 0; i < values.Length; ++i)
            {
                prop.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            }
        }
    }
}
