using System.Collections.Generic;
using Lair.Data;
using UnityEditor;
using UnityEngine;

namespace Lair.EditorTools
{
    //# MetaConfig.asset 의 상점/도전과제/영주 보상 편집 윈도우. Rule 03 §3 예외 — 에디터 전용 UI (IMGUI).
    //# 편집은 Undo.RecordObject + SetDirty, 디스크 반영은 [저장] (SaveAssetIfDirty — MetaConfig 한정).
    public class LairMetaEditorWindow : EditorWindow
    {
        private const string ConfigPath = "Assets/_Lair/Data/MetaConfig.asset";
        private const string UndoLabel = "Meta Editor 편집";

        //# 단일 윈도우 내부 탭 — 단일 시스템 내부 enum 이므로 CommonEnum.cs 미이동 (Rule 02 §8).
        private enum ETab { Shop, Achievement, LordReward }

        private static readonly string[] TabNames = { "상점 품목", "도전과제", "영주 보상" };

        private MetaConfig _config;
        private ETab _tab;
        private Vector2 _scroll;
        //# 모달 확인 후 루프 밖에서 제거 — IMGUI 루프 중 리스트 변형 방지.
        private int _pendingDeleteIndex = -1;

        [MenuItem("Lair/Meta Editor")]
        public static void Open() => GetWindow<LairMetaEditorWindow>("Meta Editor");

        private void OnEnable() => LoadConfig();
        private void OnFocus() => LoadConfig();

        private void LoadConfig()
        {
            if (_config != null)
                return;
            _config = AssetDatabase.LoadAssetAtPath<MetaConfig>(ConfigPath);
        }

        private void OnGUI()
        {
            if (_config == null)
            {
                EditorGUILayout.HelpBox($"{ConfigPath} 를 찾지 못했습니다. git 저장소에서 복원하거나 CreateAssetMenu(Lair/MetaConfig)로 생성하세요.", MessageType.Error);
                return;
            }

            _tab = (ETab)GUILayout.Toolbar((int)_tab, TabNames);
            EditorGUILayout.Space(4);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            if (_tab == ETab.Shop)
            {
                DrawShopTab();
            }
            else if (_tab == ETab.Achievement)
            {
                DrawAchievementTab();
            }
            else
            {
                DrawLordRewardTab();
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            DrawDashboard();
            DrawSaveBar();
        }

        #region 상점 품목

        private void DrawShopTab()
        {
            List<string> ids = new List<string>();
            foreach (ShopItemDef item in _config.ShopItems)
            {
                ids.Add(item != null ? item.Id : null);
            }
            List<string> duplicates = MetaEditorCalc.FindDuplicateIds(ids);

            _pendingDeleteIndex = -1;
            for (int i = 0; i < _config.ShopItems.Count; ++i)
            {
                DrawShopItem(_config.ShopItems[i], i, duplicates);
            }
            ApplyPendingDelete(_config.ShopItems, "상점 품목");

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 품목 추가"))
            {
                Undo.RecordObject(_config, UndoLabel);
                _config.ShopItems.Add(new ShopItemDef());
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawShopItem(ShopItemDef item, int index, List<string> duplicates)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawItemHeader($"#{index + 1}  {DisplayId(item.Id)}", index);
                DrawIdWarnings(item.Id, duplicates);

                EditorGUI.BeginChangeCheck();
                string id = EditorGUILayout.TextField("Id", item.Id);
                string displayName = EditorGUILayout.TextField("표시명", item.DisplayName);
                string description = EditorGUILayout.TextField("효과 설명문", item.Description);
                EShopEffectKind effectKind = (EShopEffectKind)EditorGUILayout.EnumPopup("EffectKind", item.EffectKind);
                EMonsterStatKind statKind = item.StatKind;
                if (effectKind == EShopEffectKind.MonsterStat)
                {
                    statKind = (EMonsterStatKind)EditorGUILayout.EnumPopup("StatKind", item.StatKind);
                }
                float perLevelMul = EditorGUILayout.FloatField("PerLevelMul (레벨당 배율)", item.PerLevelMul);
                int maxLevel = EditorGUILayout.IntField("MaxLevel", item.MaxLevel);
                int basePrice = EditorGUILayout.IntField("BasePrice", item.BasePrice);
                float priceGrowth = EditorGUILayout.FloatField("PriceGrowth (레벨당 가격 배율)", item.PriceGrowth);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_config, UndoLabel);
                    item.Id = id;
                    item.DisplayName = displayName;
                    item.Description = description;
                    item.EffectKind = effectKind;
                    item.StatKind = statKind;
                    item.PerLevelMul = perLevelMul;
                    item.MaxLevel = maxLevel;
                    item.BasePrice = basePrice;
                    item.PriceGrowth = priceGrowth;
                    EditorUtility.SetDirty(_config);
                }

                DrawShopPreview(item);
            }
        }

        //# 가격표 미리보기 — 산식은 MetaEditorCalc(=ShopService.PriceOf) 재사용, 기획서 §3.5 와 동일.
        private static void DrawShopPreview(ShopItemDef item)
        {
            int[] rows = MetaEditorCalc.PriceRows(item);
            string priceLine = string.Join(" / ", rows);
            EditorGUILayout.LabelField($"레벨별 가격 (Lv0→{item.MaxLevel}): {priceLine}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField(
                $"만렙 배율 ×{MetaEditorCalc.MaxLevelMultiplier(item):F3}  ·  만렙 누적 비용 {MetaEditorCalc.CumulativeMaxCost(item):N0} 소울",
                EditorStyles.miniLabel);
        }

        #endregion

        #region 도전과제

        private void DrawAchievementTab()
        {
            List<string> ids = new List<string>();
            foreach (AchievementDef item in _config.Achievements)
            {
                ids.Add(item != null ? item.Id : null);
            }
            List<string> duplicates = MetaEditorCalc.FindDuplicateIds(ids);

            _pendingDeleteIndex = -1;
            for (int i = 0; i < _config.Achievements.Count; ++i)
            {
                DrawAchievement(_config.Achievements[i], i, duplicates);
            }
            ApplyPendingDelete(_config.Achievements, "도전과제");

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 도전과제 추가"))
            {
                Undo.RecordObject(_config, UndoLabel);
                _config.Achievements.Add(new AchievementDef());
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawAchievement(AchievementDef item, int index, List<string> duplicates)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                DrawItemHeader($"#{index + 1}  {DisplayId(item.Id)}", index);
                DrawIdWarnings(item.Id, duplicates);

                EditorGUI.BeginChangeCheck();
                string id = EditorGUILayout.TextField("Id", item.Id);
                string displayName = EditorGUILayout.TextField("표시명", item.DisplayName);
                string description = EditorGUILayout.TextField("설명 (셀 표기)", item.Description);
                EAchievementCondition condition = (EAchievementCondition)EditorGUILayout.EnumPopup("Condition", item.Condition);
                float threshold = EditorGUILayout.FloatField("Threshold", item.Threshold);
                int rewardSouls = EditorGUILayout.IntField("RewardSouls", item.RewardSouls);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_config, UndoLabel);
                    item.Id = id;
                    item.DisplayName = displayName;
                    item.Description = description;
                    item.Condition = condition;
                    item.Threshold = threshold;
                    item.RewardSouls = rewardSouls;
                    EditorUtility.SetDirty(_config);
                }

                EditorGUILayout.LabelField(MetaEditorCalc.ThresholdUnitHint(item.Condition), EditorStyles.miniLabel);
            }
        }

        #endregion

        #region 영주 보상

        private void DrawLordRewardTab()
        {
            List<string> warnings = MetaEditorCalc.LordLevelWarnings(_config.LordRewards);
            foreach (string warning in warnings)
            {
                EditorGUILayout.HelpBox(warning, MessageType.Error);
            }

            _pendingDeleteIndex = -1;
            for (int i = 0; i < _config.LordRewards.Count; ++i)
            {
                DrawLordReward(_config.LordRewards[i], i);
            }
            ApplyPendingDelete(_config.LordRewards, "영주 보상");

            EditorGUILayout.Space(4);
            if (GUILayout.Button("+ 보상 추가"))
            {
                Undo.RecordObject(_config, UndoLabel);
                _config.LordRewards.Add(new LordRewardDef());
                EditorUtility.SetDirty(_config);
            }
        }

        private void DrawLordReward(LordRewardDef item, int index)
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                string title = item.IsLockedDummy ? $"#{index + 1}  Lv {item.Level} (잠금 더미)" : $"#{index + 1}  Lv {item.Level}";
                DrawItemHeader(title, index);

                EditorGUI.BeginChangeCheck();
                int level = EditorGUILayout.IntField("Level", item.Level);
                bool isLockedDummy = EditorGUILayout.Toggle("IsLockedDummy", item.IsLockedDummy);
                int rewardSouls = item.RewardSouls;
                using (new EditorGUI.DisabledScope(isLockedDummy))
                {
                    rewardSouls = EditorGUILayout.IntField("RewardSouls", item.RewardSouls);
                }
                string displayName = EditorGUILayout.TextField("표시명", item.DisplayName);
                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(_config, UndoLabel);
                    item.Level = level;
                    item.IsLockedDummy = isLockedDummy;
                    item.RewardSouls = rewardSouls;
                    item.DisplayName = displayName;
                    EditorUtility.SetDirty(_config);
                }

                if (item.IsLockedDummy)
                {
                    EditorGUILayout.LabelField("잠금 더미 — RewardSouls 지급 안 됨, 셀 표기 \"??? — 추후 해금\"", EditorStyles.miniLabel);
                }
            }
        }

        #endregion

        #region 공통

        //# 항목 헤더 — 제목 + [삭제] (모달 확인 후 _pendingDeleteIndex 예약, 제거는 루프 밖).
        private void DrawItemHeader(string title, int index)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
                //# 진입 시 값 저장-복원 — 고정 Color.white 복원은 상위 틴트를 덮어쓴다.
                Color prevBackground = GUI.backgroundColor;
                GUI.backgroundColor = new Color(1f, 0.6f, 0.6f);
                if (GUILayout.Button("삭제", GUILayout.Width(50)))
                {
                    bool ok = EditorUtility.DisplayDialog("항목 삭제", $"{title} 을(를) 삭제합니다.", "삭제", "취소");
                    if (ok)
                    {
                        _pendingDeleteIndex = index;
                    }
                }
                GUI.backgroundColor = prevBackground;
            }
        }

        private void ApplyPendingDelete<T>(List<T> list, string what)
        {
            if (_pendingDeleteIndex < 0)
                return;
            Undo.RecordObject(_config, UndoLabel);
            list.RemoveAt(_pendingDeleteIndex);
            EditorUtility.SetDirty(_config);
            _pendingDeleteIndex = -1;
            Debug.Log($"[MetaEditor] {what} 항목 삭제");
            //# 모달을 띄운 프레임은 GUILayout group 카운트가 어긋날 수 있어 즉시 중단 (CardEditor 관례).
            GUIUtility.ExitGUI();
        }

        //# Id 공백/중복 검증 — 빨간 경고 (기획서 §3.2/§5.2 — Id 는 MetaProfile 저장 키라 유일·비공백 필수).
        private static void DrawIdWarnings(string id, List<string> duplicates)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                EditorGUILayout.HelpBox("Id 가 공백입니다 — MetaProfile 저장 키로 쓰여 비공백 필수.", MessageType.Error);
                return;
            }
            if (MetaEditorCalc.HasSurroundingWhitespace(id))
            {
                EditorGUILayout.HelpBox($"Id \"{id}\" 에 앞뒤 공백 포함 — 세이브 키 불일치 위험, 공백 제거 필요.", MessageType.Warning);
            }
            if (duplicates.Contains(id.Trim()))
            {
                EditorGUILayout.HelpBox($"Id 중복: \"{id}\" — 같은 Id 가 2개 이상이면 첫 항목만 조회됩니다.", MessageType.Error);
            }
        }

        private static string DisplayId(string id) => string.IsNullOrWhiteSpace(id) ? "(Id 없음)" : id;

        //# 합계 대시보드 — 회귀 테스트 기대값(11,849 / 880 / 600) 갱신 시 참고.
        private void DrawDashboard()
        {
            using (new EditorGUILayout.VerticalScope("box"))
            {
                EditorGUILayout.LabelField("합계 대시보드", EditorStyles.boldLabel);
                EditorGUILayout.LabelField($"상점 전 품목 만렙 총비용: {MetaEditorCalc.TotalShopMaxCost(_config.ShopItems):N0} 소울");
                EditorGUILayout.LabelField($"도전과제 보상 합: {MetaEditorCalc.TotalAchievementSouls(_config.Achievements):N0} 소울");
                EditorGUILayout.LabelField($"영주 보상 합 (잠금 더미 제외): {MetaEditorCalc.TotalLordSouls(_config.LordRewards):N0} 소울");
                EditorGUILayout.LabelField("회귀 테스트(MetaConfigAssetRegressionTests) 기대값 갱신 참고용", EditorStyles.miniLabel);
            }
        }

        private void DrawSaveBar()
        {
            bool dirty = EditorUtility.IsDirty(_config);
            if (dirty)
            {
                EditorGUILayout.LabelField("● 저장되지 않은 변경 (Ctrl+Z 되돌리기 가능)", EditorStyles.boldLabel);
            }
            using (new EditorGUI.DisabledScope(dirty == false))
            {
                if (GUILayout.Button("저장"))
                {
                    //# 전역 SaveAssets 대신 MetaConfig 한정 — 무관 dirty 에셋 동반 저장 방지.
                    AssetDatabase.SaveAssetIfDirty(_config);
                    Debug.Log("[MetaEditor] MetaConfig.asset 저장 완료");
                }
            }
        }

        #endregion
    }
}
