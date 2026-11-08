using System.Collections.Generic;
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
                await LoadDifficultyAsync();
                await LoadRelicsAsync(db);
                await LoadFormationAsync(db);
                await LoadGradeFxAsync();
                await LoadTowerAsync(db);
                await LoadDailyAsync(db);
                await LoadGrowthAsync();
                return db;
            }
            catch (ChapterDataException e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return null;
            }
        }

        //# 난이도 배율표 — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → "Easy 만 존재"로 동작(difficulty-select E-3·E-4).
        private static async Task LoadDifficultyAsync()
        {
            DifficultyTable.Current = null;
            string json = await LoadTextAsync(EData.Difficulty);
            if (json == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] Difficulty.json 로드 실패 — 난이도 기능 비활성(Easy 만)");
                return;
            }
            try
            {
                DifficultyTable.Current = DifficultyTable.Build(JsonArrayUtility.FromJsonArray<DifficultyRow>(json), NameKeyExists);
            }
            catch (System.Exception e)
            {
                //# 검증 오류뿐 아니라 JSON 파싱 등 일반 예외도 폴백 — 챕터 로드는 성공 유지.
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 난이도 기능 비활성(Easy 만)");
                DifficultyTable.Current = null;
            }
        }

        //# 유물 JSON 10종 — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → 유물 기능 비활성(드랍·효과 없음, 보유 데이터는 보존) — relics §3.5-7.
        private static async Task LoadRelicsAsync(ChapterDatabase chapters)
        {
            RelicDatabase.Current = null;
            string relicJson = await LoadTextAsync(EData.Relic);
            string effectJson = await LoadTextAsync(EData.Relic_Effect);
            string paramJson = await LoadTextAsync(EData.Relic_Effect_Param);
            string dropJson = await LoadTextAsync(EData.Relic_Drop);
            string rarityJson = await LoadTextAsync(EData.RelicRarity);
            string stackJson = await LoadTextAsync(EData.RelicStack);
            string configJson = await LoadTextAsync(EData.RelicConfig);
            string stageJson = await LoadTextAsync(EData.Stage_Relic);
            string diffJson = await LoadTextAsync(EData.Difficulty_Relic);
            string diffRarityJson = await LoadTextAsync(EData.Difficulty_Relic_Rarity);
            if (relicJson == null || effectJson == null || paramJson == null || dropJson == null || rarityJson == null
                || stackJson == null || configJson == null || stageJson == null || diffJson == null || diffRarityJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 유물 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 유물 기능 비활성");
                return;
            }
            try
            {
                RelicDatabase.Current = RelicDatabase.Build(
                    JsonArrayUtility.FromJsonArray<RelicRow>(relicJson),
                    JsonArrayUtility.FromJsonArray<Relic_EffectRow>(effectJson),
                    JsonArrayUtility.FromJsonArray<Relic_Effect_ParamRow>(paramJson),
                    JsonArrayUtility.FromJsonArray<Relic_DropRow>(dropJson),
                    JsonArrayUtility.FromJsonArray<RelicRarityRow>(rarityJson),
                    JsonArrayUtility.FromJsonArray<RelicStackRow>(stackJson),
                    JsonUtility.FromJson<RelicConfigRow>(configJson),
                    JsonArrayUtility.FromJsonArray<Stage_RelicRow>(stageJson),
                    JsonArrayUtility.FromJsonArray<Difficulty_RelicRow>(diffJson),
                    JsonArrayUtility.FromJsonArray<Difficulty_Relic_RarityRow>(diffRarityJson),
                    chapters,
                    NameKeyExists);
            }
            catch (System.Exception e)
            {
                //# 검증 오류뿐 아니라 JSON 파싱 등 일반 예외도 폴백 — 챕터 로드는 성공 유지.
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 유물 기능 비활성");
                RelicDatabase.Current = null;
            }
        }

        //# 몬스터 등급 연출 JSON 6종(Grade.sizeMul + 신규 5종, monster-grades §8) — 실패해도 챕터 로드는 성공. Current=null → 연출 없음(등급 몬스터도 크기 1·이펙트 없음).
        private static async Task LoadGradeFxAsync()
        {
            GradeFxDatabase.Current = null;
            string gradeJson = await LoadTextAsync(EData.Grade);
            string glowJson = await LoadTextAsync(EData.Grade_Glow);
            string auraJson = await LoadTextAsync(EData.Grade_Aura);
            string pulseJson = await LoadTextAsync(EData.Grade_Pulse);
            string fxJson = await LoadTextAsync(EData.Grade_Fx);
            string colorJson = await LoadTextAsync(EData.Monster_GradeFxColor);
            if (gradeJson == null || glowJson == null || auraJson == null || pulseJson == null || fxJson == null || colorJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 등급 연출 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 등급 연출 비활성");
                return;
            }
            try
            {
                GradeFxDatabase.Current = GradeFxDatabase.Build(
                    JsonArrayUtility.FromJsonArray<GradeRow>(gradeJson),
                    JsonArrayUtility.FromJsonArray<Grade_GlowRow>(glowJson),
                    JsonArrayUtility.FromJsonArray<Grade_AuraRow>(auraJson),
                    JsonArrayUtility.FromJsonArray<Grade_PulseRow>(pulseJson),
                    JsonArrayUtility.FromJsonArray<Grade_FxRow>(fxJson),
                    JsonArrayUtility.FromJsonArray<Monster_GradeFxColorRow>(colorJson));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 등급 연출 비활성");
                GradeFxDatabase.Current = null;
            }
        }

        //# 던전 편성 JSON 8종(종족·등급·승급 비용 포함) — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → 편성 기능 비활성(마을 바로 출격, 전투 씬 기본 배치, 저장 편성 보존) — dungeon-formation §3.3-8.
        //# Card_Species 만 실패하면 카드 후보 제외만 꺼진다(E-2).
        private static async Task LoadFormationAsync(ChapterDatabase chapters)
        {
            FormationDatabase.Current = null;
            FormationCardSpecies.Current = null;
            string configJson = await LoadTextAsync(EData.FormationConfig);
            string raceJson = await LoadTextAsync(EData.Race);
            string gradeJson = await LoadTextAsync(EData.Grade);
            string raceMonsterJson = await LoadTextAsync(EData.Race_Monster);
            string promotionJson = await LoadTextAsync(EData.Grade_Promotion);
            string slotJson = await LoadTextAsync(EData.FormationSlot);
            string hintJson = await LoadTextAsync(EData.Chapter_FormationHint);
            string cardJson = await LoadTextAsync(EData.Card_Species);
            if (configJson == null || raceJson == null || gradeJson == null || raceMonsterJson == null || promotionJson == null
                || slotJson == null || hintJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 던전 편성 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 편성 기능 비활성");
                return;
            }
            try
            {
                List<int> chapterIds = new List<int>();
                foreach (ChapterRow chapter in chapters.Chapters)
                {
                    chapterIds.Add(chapter.chapterId);
                }
                FormationDatabase.Current = FormationDatabase.Build(
                    JsonUtility.FromJson<FormationConfigRow>(configJson),
                    JsonArrayUtility.FromJsonArray<RaceRow>(raceJson),
                    JsonArrayUtility.FromJsonArray<GradeRow>(gradeJson),
                    JsonArrayUtility.FromJsonArray<Race_MonsterRow>(raceMonsterJson),
                    JsonArrayUtility.FromJsonArray<Grade_PromotionRow>(promotionJson),
                    JsonArrayUtility.FromJsonArray<FormationSlotRow>(slotJson),
                    JsonArrayUtility.FromJsonArray<Chapter_FormationHintRow>(hintJson),
                    chapterIds,
                    NameKeyExists);
            }
            catch (System.Exception e)
            {
                //# 검증 오류뿐 아니라 JSON 파싱 등 일반 예외도 폴백 — 챕터 로드는 성공 유지.
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 편성 기능 비활성");
                FormationDatabase.Current = null;
                return;
            }

            if (cardJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] Card_Species.json 로드 실패 — 카드 후보 제외 비활성");
                return;
            }
            try
            {
                FormationCardSpecies.Current = FormationCardSpecies.Build(JsonArrayUtility.FromJsonArray<Card_SpeciesRow>(cardJson), FormationDatabase.Current);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 카드 후보 제외 비활성");
                FormationCardSpecies.Current = null;
            }
        }

        //# 무한의 탑 JSON 3종 — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → 탑 비활성(마을 탑 버튼은 "준비 중", 저장된 탑 진행은 보존) — infinite-tower §4.5-4.
        private static async Task LoadTowerAsync(ChapterDatabase chapters)
        {
            TowerDatabase.Current = null;
            string configJson = await LoadTextAsync(EData.TowerConfig);
            string heroJson = await LoadTextAsync(EData.HeroArchetype_Tower);
            string stageJson = await LoadTextAsync(EData.Stage_Tower);
            if (configJson == null || heroJson == null || stageJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 탑 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 탑 기능 비활성");
                return;
            }
            try
            {
                TowerDatabase.Current = TowerDatabase.Build(
                    JsonUtility.FromJson<TowerConfigRow>(configJson),
                    JsonArrayUtility.FromJsonArray<HeroArchetype_TowerRow>(heroJson),
                    JsonArrayUtility.FromJsonArray<Stage_TowerRow>(stageJson),
                    chapters);
            }
            catch (System.Exception e)
            {
                //# 검증 오류뿐 아니라 JSON 파싱 등 일반 예외도 폴백 — 챕터 로드는 성공 유지.
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 탑 기능 비활성");
                TowerDatabase.Current = null;
            }
        }

        //# 데일리 설정 JSON — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → 데일리 비활성(마을 타일 숨김, 저장된 데일리 기록 보존) — daily-challenge E-9.
        //# 유물 DB 가 오늘의 유물 풀의 출처라 유물 로드 뒤에 둔다.
        private static async Task LoadDailyAsync(ChapterDatabase chapters)
        {
            DailyDatabase.Current = null;
            string configJson = await LoadTextAsync(EData.DailyConfig);
            if (configJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] DailyConfig.json 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 데일리 기능 비활성");
                return;
            }
            try
            {
                DailyDatabase.Current = DailyDatabase.Build(JsonUtility.FromJson<DailyConfigRow>(configJson), chapters,
                    RelicDatabase.Current, DifficultyTable.CurrentMaxIndex);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 데일리 기능 비활성");
                DailyDatabase.Current = null;
            }
        }

        //# 몬스터 육성 재화 JSON — 실패(로드/검증)해도 챕터 로드는 성공시킨다. Current=null → 육성 재화 비활성(지급·효과·잔해 변환 없음, 보유 재화·레벨 보존) — monster-growth-currencies §6.3-8.
        //# 축별 JSON(각성·둥지·특성·잔해)은 슬라이스마다 붙는다 — 없으면 null 로 넘겨 그 축만 꺼진다.
        private static async Task LoadGrowthAsync()
        {
            GrowthDatabase.Current = null;
            _growthShopValidatedFor = null;
            string currencyJson = await LoadTextAsync(EData.Currency);
            string exchangeJson = await LoadTextAsync(EData.Currency_Exchange);
            string awakenRewardJson = await LoadTextAsync(EData.Difficulty_Awaken);
            string crystalJson = await LoadTextAsync(EData.Difficulty_TowerCrystal);
            //# 축별 JSON — 없으면 그 축만 비활성(오류 로그).
            string awakenJson = await LoadOptionalTextAsync(EData.AwakenStat);
            string nestJson = await LoadOptionalTextAsync(EData.NestMastery);
            string nestParamJson = await LoadOptionalTextAsync(EData.NestMastery_Param);
            string traitJson = await LoadOptionalTextAsync(EData.MonsterTrait);
            string traitParamJson = await LoadOptionalTextAsync(EData.MonsterTrait_Param);
            string shardJson = await LoadOptionalTextAsync(EData.RelicRarity_Shard);
            if (currencyJson == null || exchangeJson == null || awakenRewardJson == null || crystalJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 육성 재화 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 육성 재화 기능 비활성");
                return;
            }
            try
            {
                GrowthDataSet set = new GrowthDataSet
                {
                    Currencies = JsonArrayUtility.FromJsonArray<CurrencyRow>(currencyJson),
                    Exchanges = JsonArrayUtility.FromJsonArray<Currency_ExchangeRow>(exchangeJson),
                    DifficultyAwakens = JsonArrayUtility.FromJsonArray<Difficulty_AwakenRow>(awakenRewardJson),
                    TowerCrystals = JsonArrayUtility.FromJsonArray<Difficulty_TowerCrystalRow>(crystalJson),
                    AwakenStats = ParseOrNull<AwakenStatRow>(awakenJson),
                    NestMasteries = ParseOrNull<NestMasteryRow>(nestJson),
                    NestParams = ParseOrNull<NestMastery_ParamRow>(nestParamJson),
                    Traits = ParseOrNull<MonsterTraitRow>(traitJson),
                    TraitParams = ParseOrNull<MonsterTrait_ParamRow>(traitParamJson),
                    Shards = ParseOrNull<RelicRarity_ShardRow>(shardJson),
                };
                GrowthDatabase.Current = GrowthDatabase.Build(set, DifficultyTable.CurrentMaxIndex, NameKeyExists);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 육성 재화 기능 비활성");
                GrowthDatabase.Current = null;
            }
        }

        //# Strings_Ko 미로드(provider null)면 검증을 통과시킨다 — 문구 테이블 준비 순서에 의존하지 않기 위해.
        private static bool NameKeyExists(int id)
        {
            return CHText.StringProvider == null || string.IsNullOrEmpty(CHText.StringProvider.GetString(id)) == false;
        }

        //# 상점 항목 무결성(§6.3-7)은 MetaConfig 를 가진 진입점(마을·전투)이 1회 호출 — 위반이면 LogError + 육성 재화 기능 비활성(§6.3-8, 보유 재화·레벨 보존).
        private static MetaConfig _growthShopValidatedFor;

        public static void ValidateGrowthShop(MetaConfig config)
        {
            if (config == null || GrowthDatabase.Current == null || ReferenceEquals(_growthShopValidatedFor, config))
                return;
            _growthShopValidatedFor = config;
            List<string> errors = GrowthDatabase.Current.ValidateShopItems(config);
            if (errors.Count == 0)
                return;
            Debug.LogError("[ChapterDatabaseLoader] 육성 상점 항목 오류 — 육성 재화 기능 비활성:\n" + string.Join("\n", errors));
            GrowthDatabase.Current = null;
        }

        private static async Task<string> LoadOptionalTextAsync(EData key)
        {
            string text = await LoadTextAsync(key);
            if (text == null)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {key}.json 로드 실패 — 해당 육성 축 비활성");
            }
            return text;
        }

        private static T[] ParseOrNull<T>(string json) => json != null ? JsonArrayUtility.FromJsonArray<T>(json) : null;

        private static async Task<string> LoadTextAsync(EData key)
        {
            TextAsset asset = await CHMResource.Instance.LoadAsync<TextAsset>(key);
            return asset != null ? asset.text : null;
        }
    }
}
