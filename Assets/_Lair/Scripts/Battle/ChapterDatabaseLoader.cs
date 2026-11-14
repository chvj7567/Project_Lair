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
        //# 진행 중인 EnsureLoadedAsync 로드 — 동시 호출이 각자 LoadAsync 를 돌려 서로의 Current 를 비우지 않게 공유한다.
        private static Task<ChapterDatabase> _loading;

        //# 챕터·필수 묶음(밸런스·영웅 스킬·카드·메타)이 모두 로드됐으면 즉시 반환, 하나라도 없으면 다시 로드. 실패 시 null.
        public static async Task<ChapterDatabase> EnsureLoadedAsync()
        {
            if (ChapterDatabase.Current != null && BalanceConfig.Current != null && HeroSkillDatabase.Current != null
                && CardDatabase.Current != null && MetaConfig.Current != null)
                return ChapterDatabase.Current;
            if (_loading == null)
            {
                _loading = LoadAsync();
            }
            Task<ChapterDatabase> task = _loading;
            try
            {
                return await task;
            }
            finally
            {
                //# 끝나면(성공·실패·예외) 비운다 — 같은 Task 일 때만, 그 사이 시작된 새 로드를 지우지 않게.
                if (ReferenceEquals(_loading, task))
                {
                    _loading = null;
                }
            }
        }

        public static async Task<ChapterDatabase> LoadAsync()
        {
            //# 필수 묶음 1번(so-to-json-migration §6.2·§6.4) — 실패하면 챕터 로드 전체를 실패시킨다.
            if (await LoadBalanceAsync() == false)
            {
                ResetRequiredBundles();
                ChapterDatabase.Current = null;
                return null;
            }

            //# 필수 묶음 2번 — 영웅 스킬 JSON 3종 파싱(행만 보관). 검증은 영웅 직업이 확정되는 챕터 로드 뒤(4번)에 한다.
            HeroSkillDataSet skillData = await LoadHeroSkillDataAsync();
            if (skillData == null)
            {
                ResetRequiredBundles();
                ChapterDatabase.Current = null;
                return null;
            }

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
                    JsonArrayUtility.FromJsonArray<HeroArchetype_DebuffResistRow>(debuffResistJson),
                    BalanceConfig.Current.PassiveThresholds,
                    HeroSkillDatabase.GateFractionsOf(skillData));
                ChapterDatabase.Current = db;
                StageProgress.ConfigureChapterCount(db.ChapterCount);
                //# 필수 묶음 4번 — 영웅 스킬 행 검증. 실패해도 뒤 선택 묶음은 계속 로드하고 마지막에 필수 묶음을 한꺼번에 되돌린다.
                bool skillsOk = BuildHeroSkills(skillData, db);
                await LoadDifficultyAsync();
                await LoadRelicsAsync(db);
                await LoadFormationAsync(db);
                await LoadGradeFxAsync();
                await LoadCodexAsync();
                await LoadShowcaseAsync();
                await LoadTowerAsync(db);
                await LoadDailyAsync(db);
                await LoadGrowthAsync();
                //# 필수 묶음 6번(카드)·7번(메타) — 하나라도 실패하면 필수 묶음 Current 를 전부 되돌려 전투 진입 가드가 막게 한다(챕터 DB 는 마을이 쓰므로 유지, §6.4).
                bool cardsOk = await LoadCardsAsync();
                bool metaOk = await LoadMetaAsync();
                if (skillsOk == false || cardsOk == false || metaOk == false)
                {
                    ResetRequiredBundles();
                    return null;
                }
                return db;
            }
            catch (ChapterDataException e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return null;
            }
        }

        //# 밸런스 JSON 2종 → BalanceConfig.Current. 로드·파싱·검증 실패는 위반 전부를 LogError 하고 false (조용한 폴백 금지).
        private static async Task<bool> LoadBalanceAsync()
        {
            BalanceConfig.Current = null;
            string configJson = await LoadTextAsync(EData.BalanceConfig);
            string monsterJson = await LoadTextAsync(EData.MonsterStat);
            if (configJson == null || monsterJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 밸런스 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요.");
                return false;
            }
            try
            {
                BalanceConfig.Current = BalanceConfig.Build(
                    JsonUtility.FromJson<BalanceConfigRow>(configJson),
                    JsonArrayUtility.FromJsonArray<MonsterStatRow>(monsterJson));
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return false;
            }
        }

        //# 메타 JSON 9종 → MetaConfig.Current. 로드·파싱·검증 실패는 위반 전부를 LogError 하고 false (조용한 폴백 금지).
        //# 성공하면 육성 상점 항목 무결성을 1회 검증한다(§6.2) — 위반은 필수 묶음 실패가 아니라 육성 재화 기능만 끈다.
        private static async Task<bool> LoadMetaAsync()
        {
            MetaConfig.Current = null;
            string configJson = await LoadTextAsync(EData.MetaConfig);
            string itemJson = await LoadTextAsync(EData.ShopItem);
            string statJson = await LoadTextAsync(EData.ShopItem_Stat);
            string speciesJson = await LoadTextAsync(EData.ShopItem_Species);
            string mulJson = await LoadTextAsync(EData.ShopItem_Mul);
            string descJson = await LoadTextAsync(EData.ShopItem_Desc);
            string levelJson = await LoadTextAsync(EData.LordLevel);
            string rewardJson = await LoadTextAsync(EData.LordLevel_Reward);
            string achievementJson = await LoadTextAsync(EData.Achievement);
            if (configJson == null || itemJson == null || statJson == null || speciesJson == null || mulJson == null
                || descJson == null || levelJson == null || rewardJson == null || achievementJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 메타 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요.");
                return false;
            }
            try
            {
                MetaConfig.Current = MetaConfig.Build(
                    new MetaDataSet
                    {
                        Config = JsonUtility.FromJson<MetaConfigRow>(configJson),
                        ShopItems = JsonArrayUtility.FromJsonArray<ShopItemRow>(itemJson),
                        Stats = JsonArrayUtility.FromJsonArray<ShopItem_StatRow>(statJson),
                        Species = JsonArrayUtility.FromJsonArray<ShopItem_SpeciesRow>(speciesJson),
                        Muls = JsonArrayUtility.FromJsonArray<ShopItem_MulRow>(mulJson),
                        Descs = JsonArrayUtility.FromJsonArray<ShopItem_DescRow>(descJson),
                        LordLevels = JsonArrayUtility.FromJsonArray<LordLevelRow>(levelJson),
                        LordRewards = JsonArrayUtility.FromJsonArray<LordLevel_RewardRow>(rewardJson),
                        Achievements = JsonArrayUtility.FromJsonArray<AchievementRow>(achievementJson),
                    },
                    NameKeyExists, LookupString, CurrencyExists);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return false;
            }
            //# try 밖 — 무결성 검사의 문제가 필수 묶음 실패(전투 진입 차단)로 번지지 않게 한다.
            ValidateGrowthShop(MetaConfig.Current);
            return true;
        }

        //# 영웅 스킬 JSON 3종 로드·파싱 — 하나라도 없거나 파싱에 실패하면 null (필수 묶음, 조용한 폴백 금지).
        private static async Task<HeroSkillDataSet> LoadHeroSkillDataAsync()
        {
            HeroSkillDatabase.Current = null;
            string skillJson = await LoadTextAsync(EData.HeroSkill);
            string paramJson = await LoadTextAsync(EData.HeroSkill_Param);
            string phaseJson = await LoadTextAsync(EData.HeroArchetype_Skill);
            if (skillJson == null || paramJson == null || phaseJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 영웅 스킬 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요.");
                return null;
            }
            try
            {
                return new HeroSkillDataSet
                {
                    Skills = JsonArrayUtility.FromJsonArray<HeroSkillRow>(skillJson),
                    Params = JsonArrayUtility.FromJsonArray<HeroSkill_ParamRow>(paramJson),
                    Phases = JsonArrayUtility.FromJsonArray<HeroArchetype_SkillRow>(phaseJson),
                };
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return null;
            }
        }

        //# 영웅 스킬 검증·조립 → HeroSkillDatabase.Current. 위반은 전부 LogError 하고 false.
        private static bool BuildHeroSkills(HeroSkillDataSet data, ChapterDatabase chapters)
        {
            try
            {
                HeroSkillDatabase.Current = HeroSkillDatabase.Build(data, chapters, NameKeyExists, LookupString);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                HeroSkillDatabase.Current = null;
                return false;
            }
        }

        //# 카드 JSON 2종 → CardDatabase.Current. Card·Card_Param 자체의 로드·파싱·검증 실패만 필수 묶음 실패다 — 위반 전부를 LogError 하고 false.
        //# 카드 풀 SO·Card_Species 와의 대조는 런타임에서 관용하고 EditMode 테스트(SJ5)가 한다(§6.4).
        private static async Task<bool> LoadCardsAsync()
        {
            CardDatabase.Current = null;
            string cardJson = await LoadTextAsync(EData.Card);
            string paramJson = await LoadTextAsync(EData.Card_Param);
            if (cardJson == null || paramJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 카드 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요.");
                return false;
            }
            try
            {
                CardDatabase.Current = CardDatabase.Build(
                    new CardDataSet
                    {
                        Cards = JsonArrayUtility.FromJsonArray<CardRow>(cardJson),
                        Params = JsonArrayUtility.FromJsonArray<Card_ParamRow>(paramJson),
                    },
                    NameKeyExists, LookupString);
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message}");
                return false;
            }
        }

        //# 필수 묶음(밸런스·영웅 스킬·카드·메타)의 Current 를 한 곳에서 되돌린다 — 이후 단계의 묶음도 여기에 더한다.
        private static void ResetRequiredBundles()
        {
            BalanceConfig.Current = null;
            HeroSkillDatabase.Current = null;
            CardDatabase.Current = null;
            MetaConfig.Current = null;
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

        //# 도감 연출 JSON 2종(monster-codex §11) — 실패해도 챕터 로드는 성공. Current=null → 도감 이펙트·해금 연출만 꺼짐.
        private static async Task LoadCodexAsync()
        {
            CodexDatabase.Current = null;
            string configJson = await LoadTextAsync(EData.CodexConfig);
            string unlockJson = await LoadTextAsync(EData.Grade_CodexUnlock);
            if (configJson == null || unlockJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 도감 연출 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 도감 이펙트 비활성");
                return;
            }
            try
            {
                CodexDatabase.Current = CodexDatabase.Build(
                    JsonUtility.FromJson<CodexConfigRow>(configJson),
                    JsonArrayUtility.FromJsonArray<Grade_CodexUnlockRow>(unlockJson));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 도감 이펙트 비활성");
                CodexDatabase.Current = null;
            }
        }

        //# 마을·로딩 몬스터 쇼케이스 JSON 3종 — 실패해도 챕터 로드는 성공. Current=null → 쇼케이스 비활성(씬의 정적 몬스터 유지).
        private static async Task LoadShowcaseAsync()
        {
            ShowcaseDatabase.Current = null;
            string sceneJson = await LoadTextAsync(EData.Showcase_Scene);
            string actorJson = await LoadTextAsync(EData.Showcase_Actor);
            string waypointJson = await LoadTextAsync(EData.Showcase_Actor_Waypoint);
            if (sceneJson == null || actorJson == null || waypointJson == null)
            {
                Debug.LogError("[ChapterDatabaseLoader] 쇼케이스 JSON 로드 실패 — Addressables 등록(라벨 Resource)을 확인하세요. 쇼케이스 비활성");
                return;
            }
            try
            {
                ShowcaseDatabase.Current = ShowcaseDatabase.Build(
                    JsonArrayUtility.FromJsonArray<Showcase_SceneRow>(sceneJson),
                    JsonArrayUtility.FromJsonArray<Showcase_ActorRow>(actorJson),
                    JsonArrayUtility.FromJsonArray<Showcase_Actor_WaypointRow>(waypointJson));
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[ChapterDatabaseLoader] {e.Message} — 쇼케이스 비활성");
                ShowcaseDatabase.Current = null;
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

        //# Strings_Ko 미로드면 빈 문자열 — 표시 문구만 비고 검증은 NameKeyExists 규칙을 따른다(so-to-json-migration §9 E3).
        private static string LookupString(int id)
        {
            return CHText.StringProvider != null ? CHText.StringProvider.GetString(id) : string.Empty;
        }

        //# 육성 재화 DB 가 없으면(비필수 묶음 실패) 재화 존재 검사만 건너뛴다 — 값명 검사는 Build 가 한다.
        private static bool CurrencyExists(ECurrency currency)
        {
            return GrowthDatabase.Current == null || GrowthDatabase.Current.GetCurrency(currency) != null;
        }

        //# 상점 항목 무결성(§6.3-7)은 메타 로드 직후 로더가 1회 호출 — 위반이면 LogError + 육성 재화 기능 비활성(§6.3-8, 보유 재화·레벨 보존).
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
