using System.Collections.Generic;
using System.IO;
using Lair.Card;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# 카드 조회/추가/삭제/편집 통합 윈도우. Rule 03 §3 예외 — 에디터 전용 UI (IMGUI).
    public class LairCardEditorWindow : EditorWindow
    {
        private const string CardDir = "Assets/_Lair/Art/Cards/Items";
        private const string PoolDir = "Assets/_Lair/Art/Cards";

        //# 카드의 풀 소속 — 최대 한 풀(없음 ⊻ Passive ⊻ Active). 단일 시스템 내부 enum 이므로 CommonEnum.cs 미이동 (Rule 02 §8).
        private enum EPoolKind { None, Passive, Active }

        //# 목록 한 행의 스냅샷 — RebuildCache 가 1회 채운다. OnGUI 매 프레임 AssetDatabase 접근 제거용 (Rule 02 §8 내부 타입).
        private struct RowCache
        {
            public ECardId Id;
            public CardData Card;
            public EBuildAxis Axis;
            public EPoolKind Pool;
            public Sprite Icon;
        }

        private Vector2 _listScroll;
        private Vector2 _editScroll;
        private ECardId _selected;
        private bool _hasSelection;
        private string _search = "";
        private string _newCardId = "";

        private SerializedObject _editSo;
        private ECardId _editSoId;
        //# 저장 전까지 풀 소속을 들고 있는 pending 값 — [저장] 시 실제 풀 에셋에 커밋.
        private EPoolKind _pendingPool;

        //# 매 프레임 AssetDatabase 접근을 없애는 스냅샷 캐시 — RebuildCache 에서만 채운다.
        private readonly List<RowCache> _rows = new List<RowCache>();
        private CardPool _passivePool;
        private CardPool _activePool;

        [MenuItem("Lair/Card Editor")]
        public static void Open() => GetWindow<LairCardEditorWindow>("Card Editor");

        private static string AssetPath(ECardId id) => $"{CardDir}/{id}.asset";
        private static CardData Load(ECardId id) =>
            AssetDatabase.LoadAssetAtPath<CardData>(AssetPath(id));

        private void OnEnable() => RebuildCache();

        //# 다른 데서 에셋(카드/풀)을 바꾸고 돌아왔을 때 최신화 + 비동기 아이콘 프리뷰 재시도.
        private void OnFocus() => RebuildCache();

        //# 풀 2개 + ECardId 전 행을 1회 로드해 스냅샷 캐시를 채운다. 무효화 시점(생성/삭제/저장)에서만 호출.
        private void RebuildCache()
        {
            _passivePool = LoadPool(EData.CardPool_Passive);
            _activePool = LoadPool(EData.CardPool_Active);

            _rows.Clear();
            foreach (ECardId id in System.Enum.GetValues(typeof(ECardId)))
            {
                CardData card = Load(id);
                RowCache row = new RowCache();
                row.Id = id;
                row.Card = card;
                if (card != null)
                {
                    row.Axis = card.Axis;
                    row.Pool = GetPoolMembership(card);
                    row.Icon = card.Icon;
                }
                _rows.Add(row);
            }
        }

        //# 캐시된 풀 참조로 카드의 현재 소속을 판정 — 둘 다면 Passive 우선 (AssetDatabase 접근 없음).
        private EPoolKind GetPoolMembership(CardData card)
        {
            if (_passivePool != null && IndexInPool(_passivePool, card) >= 0)
                return EPoolKind.Passive;
            if (_activePool != null && IndexInPool(_activePool, card) >= 0)
                return EPoolKind.Active;
            return EPoolKind.None;
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "ECardId enum 에 값을 추가하면 이 툴에 (미생성) 슬롯으로 나타납니다. " +
                "[Enum 추가] 로 새 카드 종류를 만들 수 있습니다.", MessageType.Info);

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawListPane();
                DrawEditPane();
            }
        }

        private void DrawListPane()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(280)))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    _search = EditorGUILayout.TextField("검색", _search);
                    if (GUILayout.Button("새로고침", GUILayout.Width(70)))
                    {
                        RebuildCache();
                    }
                }
                _listScroll = EditorGUILayout.BeginScrollView(_listScroll);
                for (int i = 0; i < _rows.Count; i++)
                {
                    RowCache row = _rows[i];
                    string name = row.Id.ToString();
                    if (string.IsNullOrEmpty(_search) == false &&
                        name.IndexOf(_search, System.StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    DrawListRow(row, name);
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(6);
                DrawAddEnumRow();
            }
        }

        //# 캐시된 행만 읽어 그린다 — 매 프레임 AssetDatabase 접근 0. 프리뷰는 AssetPreview 내부 캐시(비-AssetDatabase) 사용.
        private void DrawListRow(RowCache row, string name)
        {
            using (new EditorGUILayout.HorizontalScope("box"))
            {
                if (row.Card == null)
                {
                    EditorGUILayout.LabelField($"{name} (미생성)", EditorStyles.miniLabel);
                    if (GUILayout.Button("생성하기", GUILayout.Width(70)))
                    {
                        CreateCard(row.Id);
                    }
                }
                else
                {
                    if (row.Icon != null)
                    {
                        Rect r = GUILayoutUtility.GetRect(20, 20, GUILayout.Width(20), GUILayout.Height(20));
                        //# 비동기 프리뷰 — 첫 프레임 null 이면 다음 OnFocus/새로고침 때 채워진다. AssetPreview 는 자체 캐시.
                        GUI.DrawTexture(r, AssetPreview.GetAssetPreview(row.Icon) ?? Texture2D.grayTexture, ScaleMode.ScaleToFit);
                    }
                    //# 풀 뱃지 — 캐시된 Pool 값으로 그린다 (기획서 §3.1).
                    string badge = BuildPoolBadge(row.Pool);
                    if (GUILayout.Button($"{name}  [{row.Axis}]{badge}", EditorStyles.label))
                    {
                        _selected = row.Id;
                        _hasSelection = true;
                    }
                }
            }
        }

        //# 캐시된 풀 소속을 뱃지 문자열로 — Passive [P], Active [A], 미소속 빈 문자열 (기획서 §3.1).
        private static string BuildPoolBadge(EPoolKind pool)
        {
            if (pool == EPoolKind.Passive)
                return " [P]";
            if (pool == EPoolKind.Active)
                return " [A]";
            return "";
        }

        //# 신규 카드 종류(enum 값) 추가 — 좌측 하단.
        private void DrawAddEnumRow()
        {
            EditorGUILayout.LabelField("새 카드 종류 (Enum)", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                _newCardId = EditorGUILayout.TextField(_newCardId);
                if (GUILayout.Button("Enum 추가", GUILayout.Width(90)))
                {
                    string id = _newCardId.Trim();
                    if (CardEnumCodegen.AppendCardId(id))
                    {
                        _newCardId = "";
                        //# 재컴파일 후 (미생성) 슬롯으로 등장 — 그때 [생성하기] 로 .asset 생성.
                        Debug.Log($"[CardEditor] '{id}' enum 추가 요청 — 재컴파일 후 목록에 (미생성) 으로 표시됩니다.");
                    }
                    GUIUtility.ExitGUI();
                }
            }
        }

        //# 미생성 enum 슬롯에 대해 .asset 생성.
        private void CreateCard(ECardId id)
        {
            if (Directory.Exists(CardDir) == false)
            {
                Directory.CreateDirectory(CardDir);
            }

            CardData card = ScriptableObject.CreateInstance<CardData>();
            SerializedObject so = new SerializedObject(card);
            so.FindProperty("_id").enumValueIndex = (int)id;
            so.FindProperty("_displayName").stringValue = id.ToString();
            so.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(card, AssetPath(id));
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _selected = id;
            _hasSelection = true;
            RebuildCache();
            Debug.Log($"[CardEditor] 생성: {id}");
        }

        private void DrawEditPane()
        {
            using (new EditorGUILayout.VerticalScope())
            {
                if (_hasSelection == false)
                {
                    EditorGUILayout.HelpBox("좌측에서 카드를 선택하세요.", MessageType.None);
                    return;
                }

                CardData card = GetCachedCard(_selected);
                if (card == null)
                {
                    EditorGUILayout.HelpBox($"{_selected} 는 미생성 상태입니다. 좌측 [생성하기] 후 편집하세요.", MessageType.Warning);
                    return;
                }

                //# 선택 변경 시에만 SerializedObject 재생성 — 미저장 변경이 있으면 먼저 가드 (이전 카드 기준).
                if (_editSo == null || _editSo.targetObject != card || _editSoId != _selected)
                {
                    bool dialogShown = GuardUnsaved();
                    _editSo = new SerializedObject(card);
                    _editSoId = _selected;
                    _editSo.Update();
                    _pendingPool = GetCurrentPool(card);
                    //# 모달을 띄운 프레임은 GUILayout group 카운트가 어긋날 수 있어 즉시 중단 — 다음 OnGUI 에서 깨끗이 재렌더.
                    if (dialogShown)
                    {
                        GUIUtility.ExitGUI();
                    }
                }

                EditorGUILayout.HelpBox(
                    "Icon/CardImage 는 Build UI Prefabs(LairCardPrefabBuilder) 재실행 시 " +
                    "ECardId 이름 PNG 컨벤션으로 덮어쓰일 수 있습니다.", MessageType.Warning);

                _editScroll = EditorGUILayout.BeginScrollView(_editScroll);
                //# _id 는 정체성(파일명=ECardId) 고정값 — 편집 불가 read-only. 변경 시 loader/list/CardDataSyncer 가 desync 됨.
                EditorGUILayout.LabelField("Id", _selected.ToString());
                foreach (string prop in new string[]
                    { "_axis", "_displayName", "_description", "_icon", "_cardImage", "_effect" })
                {
                    SerializedProperty sp = _editSo.FindProperty(prop);
                    if (sp != null)
                    {
                        EditorGUILayout.PropertyField(sp, true);
                    }
                }
                EditorGUILayout.EndScrollView();

                EditorGUILayout.Space(8);
                DrawPoolSelect();
                EditorGUILayout.Space(8);
                DrawSaveBar(card);
                EditorGUILayout.Space(8);
                DrawDeleteButton(card);
            }
        }

        //# 필드(_editSo) 또는 풀 소속이 에셋과 달라 커밋 대기 중인지.
        private bool IsDirty(CardData card)
        {
            if (_editSo == null)
                return false;
            return _editSo.hasModifiedProperties || _pendingPool != GetCurrentPool(card);
        }

        //# 편집 패인 하단 — 미저장 표시 + [저장] 버튼. 변경 없으면 버튼 비활성.
        private void DrawSaveBar(CardData card)
        {
            bool dirty = IsDirty(card);
            if (dirty)
            {
                EditorGUILayout.LabelField("● 저장되지 않은 변경", EditorStyles.boldLabel);
            }
            using (new EditorGUI.DisabledScope(dirty == false))
            {
                if (GUILayout.Button("저장"))
                {
                    CommitSave(card);
                }
            }
        }

        //# 필드 + 풀 소속을 에셋에 한 번에 커밋.
        private void CommitSave(CardData card)
        {
            _editSo.ApplyModifiedProperties();
            EditorUtility.SetDirty(card);
            SetPoolMembership(EData.CardPool_Passive, card, _pendingPool == EPoolKind.Passive);
            SetPoolMembership(EData.CardPool_Active, card, _pendingPool == EPoolKind.Active);
            AssetDatabase.SaveAssets();
            RebuildCache();
            //# 가드 경로에서는 _selected 가 이미 다음 카드라 card.name 으로 로깅.
            Debug.Log($"[CardEditor] 저장: {card.name} (풀={_pendingPool})");
        }

        //# 선택 전환 시 미저장 변경 가드 — 저장(커밋 후 전환) / 버리기(그냥 전환). 모달을 띄웠으면 true 반환.
        private bool GuardUnsaved()
        {
            if (_editSo == null)
                return false;
            CardData outgoing = _editSo.targetObject as CardData;
            if (outgoing == null || IsDirty(outgoing) == false)
                return false;

            bool save = EditorUtility.DisplayDialog(
                "저장하지 않은 변경",
                $"{_editSoId} 에 저장하지 않은 변경이 있습니다.",
                "저장", "버리기");
            if (save)
            {
                CommitSave(outgoing);
            }
            return true;
        }

        //# 고정 경로 직접 로드 — FindAssets 회피. 경로가 다를 때만 FindAssets 1회 폴백. RebuildCache 에서만 호출.
        private CardPool LoadPool(EData key)
        {
            string directPath = $"{PoolDir}/{key}.asset";
            CardPool direct = AssetDatabase.LoadAssetAtPath<CardPool>(directPath);
            if (direct != null)
                return direct;

            string[] guids = AssetDatabase.FindAssets($"t:CardPool {key}");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (Path.GetFileNameWithoutExtension(path) == key.ToString())
                    return AssetDatabase.LoadAssetAtPath<CardPool>(path);
            }
            return null;
        }

        //# 풀 소속 단일 선택 — 저장 전까지 pending 값만 갱신 (커밋은 CommitSave).
        private void DrawPoolSelect()
        {
            EditorGUILayout.LabelField("풀 소속 (한 풀만)", EditorStyles.boldLabel);
            string[] options = new string[] { "없음", "Passive", "Active" };
            int next = GUILayout.Toolbar((int)_pendingPool, options);
            _pendingPool = (EPoolKind)next;
        }

        //# 캐시된 행에서 카드를 찾는다 — 매 프레임 AssetDatabase 접근 제거 (없으면 null).
        private CardData GetCachedCard(ECardId id)
        {
            for (int i = 0; i < _rows.Count; i++)
            {
                if (_rows[i].Id == id)
                    return _rows[i].Card;
            }
            return null;
        }

        //# 카드의 현재 실제 풀 소속을 읽는다 — 캐시된 풀 참조 사용 (둘 다면 Passive 우선).
        private EPoolKind GetCurrentPool(CardData card) => GetPoolMembership(card);

        //# 카드를 해당 풀에 넣거나(중복 방지) 뺀다 (null 잔존 방지). 저장은 호출부에서 일괄.
        //# 캐시된 풀 참조를 직접 변형 — RebuildCache 가 호출부(CommitSave)에서 캐시를 최신화한다.
        private void SetPoolMembership(EData key, CardData card, bool shouldBeIn)
        {
            CardPool pool = key == EData.CardPool_Passive ? _passivePool : _activePool;
            if (pool == null)
                return;

            int idx = IndexInPool(pool, card);
            bool isIn = idx >= 0;
            if (shouldBeIn == isIn)
                return;

            SerializedObject so = new SerializedObject(pool);
            SerializedProperty list = so.FindProperty("_cards");
            if (shouldBeIn)
            {
                list.arraySize++;
                list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = card;
            }
            else
            {
                RemoveAt(list, idx);
            }
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pool);
        }

        private static int IndexInPool(CardPool pool, CardData card)
        {
            for (int i = 0; i < pool.Cards.Count; i++)
            {
                if (pool.Cards[i] == card)
                    return i;
            }
            return -1;
        }

        //# object-reference 배열에서 DeleteArrayElementAtIndex 는 비-null 원소를 먼저 null 로만 만들고
        //# 실제 제거는 두 번째 호출에서 일어난다. arraySize 가 안 줄면 한 번 더 호출해 null 잔존을 막는다.
        private static void RemoveAt(SerializedProperty list, int idx)
        {
            int before = list.arraySize;
            list.DeleteArrayElementAtIndex(idx);
            if (list.arraySize == before)
            {
                list.DeleteArrayElementAtIndex(idx);
            }
        }

        private void DrawDeleteButton(CardData card)
        {
            GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
            if (GUILayout.Button("카드 삭제"))
            {
                bool ok = EditorUtility.DisplayDialog(
                    "카드 삭제",
                    $"{_selected} 의 .asset 을 삭제합니다.\n" +
                    "ECardId enum 값은 유지되어 (미생성) 슬롯으로 다시 보이고, [생성하기] 로 재생성할 수 있습니다.",
                    "삭제", "취소");
                if (ok)
                {
                    DeleteCard(card);
                }
            }
            GUI.backgroundColor = Color.white;
        }

        private void DeleteCard(CardData card)
        {
            RemoveFromPool(EData.CardPool_Passive, card);
            RemoveFromPool(EData.CardPool_Active, card);

            string path = AssetPath(_selected);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            _hasSelection = false;
            _editSo = null;
            _pendingPool = EPoolKind.None;
            RebuildCache();
            Debug.Log($"[CardEditor] 삭제: {_selected} (enum 값 유지)");
        }

        private void RemoveFromPool(EData key, CardData card)
        {
            CardPool pool = key == EData.CardPool_Passive ? _passivePool : _activePool;
            if (pool == null)
                return;
            int idx = IndexInPool(pool, card);
            if (idx < 0)
                return;

            SerializedObject so = new SerializedObject(pool);
            RemoveAt(so.FindProperty("_cards"), idx);   //# 안전 제거 헬퍼 재사용 (null 잔존 방지)
            so.ApplyModifiedProperties();
            EditorUtility.SetDirty(pool);
        }
    }
}
