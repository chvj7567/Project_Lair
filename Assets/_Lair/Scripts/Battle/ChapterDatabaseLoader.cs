using System.Threading.Tasks;
using ChvjUnityInfra;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 챕터 JSON 12종을 Enum 키로 로드해 ChapterDatabase.Current 를 채운다 (chapter-system §7.3, Rule 02 §11 로드 패턴).
    //# Loading 씬이 선행 로드하고, Battle 직행(에디터)은 EnsureLoadedAsync 로 보충한다. 실패 시 Current 는 null 유지 + LogError.
    public static class ChapterDatabaseLoader
    {
        //# 이미 로드됐으면 즉시 반환, 아니면 로드. 실패 시 null.
        public static async Task<ChapterDatabase> EnsureLoadedAsync()
        {
            if (ChapterDatabase.Current != null)
                return ChapterDatabase.Current;
            return await LoadAsync();
        }

        public static async Task<ChapterDatabase> LoadAsync()
        {
            string chapterJson = await LoadTextAsync(EData.Chapter);
            string stageJson = await LoadTextAsync(EData.Stage);
            string ruleJson = await LoadTextAsync(EData.Stage_Rule);
            string paramJson = await LoadTextAsync(EData.Stage_Rule_Param);
            string triggerJson = await LoadTextAsync(EData.Stage_Rule_Trigger);
            string heroJson = await LoadTextAsync(EData.HeroArchetype);
            string kiteJson = await LoadTextAsync(EData.HeroArchetype_Kite);
            string skirmishJson = await LoadTextAsync(EData.HeroArchetype_Skirmish);
            string chargeJson = await LoadTextAsync(EData.HeroArchetype_Charge);
            string cleaveJson = await LoadTextAsync(EData.HeroArchetype_Cleave);
            string guardJson = await LoadTextAsync(EData.HeroArchetype_Guard);
            string debuffResistJson = await LoadTextAsync(EData.HeroArchetype_DebuffResist);
            if (chapterJson == null || stageJson == null || ruleJson == null || paramJson == null
                || triggerJson == null || heroJson == null || kiteJson == null || skirmishJson == null
                || chargeJson == null || cleaveJson == null || guardJson == null || debuffResistJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 챕터 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요.");
                return null;
            }

            try
            {
                ChapterDatabase db = ChapterDatabase.Build(
                    JsonArrayUtility.FromJsonArray<ChapterRow>(chapterJson),
                    JsonArrayUtility.FromJsonArray<StageRow>(stageJson),
                    JsonArrayUtility.FromJsonArray<Stage_RuleRow>(ruleJson),
                    JsonArrayUtility.FromJsonArray<Stage_Rule_ParamRow>(paramJson),
                    JsonArrayUtility.FromJsonArray<Stage_Rule_TriggerRow>(triggerJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetypeRow>(heroJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_KiteRow>(kiteJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_SkirmishRow>(skirmishJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_ChargeRow>(chargeJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_CleaveRow>(cleaveJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_GuardRow>(guardJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_DebuffResistRow>(debuffResistJson));
                ChapterDatabase.Current = db;
                StageProgress.ConfigureChapterCount(db.ChapterCount);
                return db;
            }
            catch (ChapterDataException e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return null;
            }
        }

        private static async Task<string> LoadTextAsync(EData key)
        {
            TextAsset asset = await CHMResource.Instance.LoadAsync<TextAsset>(key);
            return asset != null ? asset.text : null;
        }
    }
}
