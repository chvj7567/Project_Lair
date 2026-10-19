using System;
using System.Collections.Generic;
using Lair.Battle;

namespace Lair.Data
{
    //# 유물 효과 하나 — 종류 + 중첩 1 기준 파라미터 (relics §3.3).
    public class RelicEffectData
    {
        public ERelicEffectKind Kind;
        public IReadOnlyDictionary<string, float> Params;

        public float Get(string key) => Params[key];
    }

    //# 유물 하나의 검증된 런타임 뷰.
    public class RelicData
    {
        public string RelicId;
        public ERelicCategory Category;
        public ERelicRarity Rarity;
        public int MaxStack;
        public int NameKey;
        public int DescKey;
        public IReadOnlyList<RelicEffectData> Effects;
    }

    //# 유물 JSON 10종 검증 + 조회 (relics §3.5). Unity 비의존 순수 클래스. 위반 시 ChapterDataException 으로 전부 모아 던진다.
    public class RelicDatabase
    {
        //# 로드 성공 시에만 채워진다. null 이면 "유물 기능 비활성"(드랍·효과·도감 비어 있음, 전투는 정상) — relics §3.5-7.
        public static RelicDatabase Current { get; set; }

        //# 효과 파라미터 중 중첩 스케일 S(n) 이 곱해지는 키 (relics §2.3). countPerStack 은 x n 선형, 나머지는 고정.
        private static readonly HashSet<string> ScaledKeys = new HashSet<string> { "amount", "hpRatioPerSec", "hpRatio", "advanceSec" };
        public const string CountPerStackKey = "countPerStack";

        //# 효과 종류별 필수 파라미터 키 — 누락/중복이 오류.
        private static readonly Dictionary<ERelicEffectKind, string[]> RequiredParams = new Dictionary<ERelicEffectKind, string[]>
        {
            { ERelicEffectKind.MonsterStat, new[] { "statKind", "amount" } },
            { ERelicEffectKind.SpawnerPeriod, new[] { "amount" } },
            { ERelicEffectKind.HeroBurn, new[] { "hpRatioPerSec" } },
            { ERelicEffectKind.HeroLightning, new[] { "intervalSec", "hpRatio" } },
            { ERelicEffectKind.HeroAttackDown, new[] { "amount" } },
            { ERelicEffectKind.OpeningSummon, new[] { "monster", "countPerStack" } },
            { ERelicEffectKind.LowHpFrenzy, new[] { "hpFraction", "durationSec" } },
            { ERelicEffectKind.BattleSpeed, new[] { "speedScale" } },
            { ERelicEffectKind.SoulBonus, new[] { "amount" } },
            { ERelicEffectKind.XpBonus, new[] { "amount" } },
            { ERelicEffectKind.LoseSoulBonus, new[] { "amount" } },
            { ERelicEffectKind.SpawnPrelude, new[] { "advanceSec" } },
        };

        private const float Epsilon = 0.0001f;

        private readonly List<RelicData> _relics = new List<RelicData>();
        private readonly Dictionary<string, RelicData> _byId = new Dictionary<string, RelicData>();
        private readonly Dictionary<string, List<Relic_DropRow>> _dropsByHero = new Dictionary<string, List<Relic_DropRow>>();
        private readonly Dictionary<ERelicRarity, RelicRarityRow> _rarities = new Dictionary<ERelicRarity, RelicRarityRow>();
        private float[] _stackScale = Array.Empty<float>();
        private readonly Dictionary<long, float> _stageChance = new Dictionary<long, float>();
        private float[] _diffChanceMul = Array.Empty<float>();
        private readonly Dictionary<long, float> _rarityWeightMul = new Dictionary<long, float>();

        private RelicDatabase() { }

        public RelicConfigRow Config { get; private set; }

        //# JSON 순서(종류 묶음) 그대로 — 도감 정렬 기준 (relics §7.2, D-12).
        public IReadOnlyList<RelicData> Relics => _relics;

        //# 중첩 스케일표 최대 단계(= 허용 maxStack 상한).
        public int MaxScaleStack => _stackScale.Length;

        public RelicData Get(string relicId)
        {
            return relicId != null && _byId.TryGetValue(relicId, out RelicData data) ? data : null;
        }

        //# S(n) — 범위를 벗어나면 0(미보유)/마지막 단계로 클램프.
        public float EffectScale(int stack)
        {
            if (stack < 1 || _stackScale.Length == 0)
                return 0f;
            return _stackScale[Math.Min(stack, _stackScale.Length) - 1];
        }

        //# 중첩 stack 에서의 파라미터 실효값 — 스케일 키는 x S(n), countPerStack 은 x n, 나머지는 고정.
        public float ScaledParam(RelicEffectData effect, string key, int stack)
        {
            float baseValue = effect.Get(key);
            if (ScaledKeys.Contains(key))
                return baseValue * EffectScale(stack);
            if (key == CountPerStackKey)
                return baseValue * stack;
            return baseValue;
        }

        //# 이 영웅의 드랍 테이블 — 없으면 빈 리스트.
        public IReadOnlyList<Relic_DropRow> DropsOf(string heroId)
        {
            return heroId != null && _dropsByHero.TryGetValue(heroId, out List<Relic_DropRow> list) ? list : (IReadOnlyList<Relic_DropRow>)Array.Empty<Relic_DropRow>();
        }

        public RelicRarityRow RarityRow(ERelicRarity rarity)
        {
            return _rarities.TryGetValue(rarity, out RelicRarityRow row) ? row : null;
        }

        public int RarityBaseWeight(ERelicRarity rarity)
        {
            return _rarities.TryGetValue(rarity, out RelicRarityRow row) ? row.baseWeight : 0;
        }

        public float StageDropChance(int chapterId, int stageIndex)
        {
            return _stageChance.TryGetValue(Key(chapterId, stageIndex), out float chance) ? chance : 0f;
        }

        //# 범위를 벗어나면 마지막 난이도로 클램프 (DifficultyTable.Get 과 같은 규약).
        public float DifficultyChanceMul(int difficulty)
        {
            if (_diffChanceMul.Length == 0)
                return 1f;
            return _diffChanceMul[Math.Max(0, Math.Min(_diffChanceMul.Length - 1, difficulty))];
        }

        public float RarityWeightMul(int difficulty, ERelicRarity rarity)
        {
            int d = Math.Max(0, Math.Min(Math.Max(0, _diffChanceMul.Length - 1), difficulty));
            return _rarityWeightMul.TryGetValue(Key(d, (int)rarity), out float mul) ? mul : 1f;
        }

        private static long Key(int a, int b) => ((long)a << 32) | (uint)b;

        //# chapters 가 null 이면 영웅/스테이지 교차 검증을 건너뛴다. textKeyExists 가 null 이면 문구 키 검증 생략.
        public static RelicDatabase Build(
            RelicRow[] relics,
            Relic_EffectRow[] effects,
            Relic_Effect_ParamRow[] effectParams,
            Relic_DropRow[] drops,
            RelicRarityRow[] rarities,
            RelicStackRow[] stacks,
            RelicConfigRow config,
            Stage_RelicRow[] stageRelics,
            Difficulty_RelicRow[] difficultyRelics,
            Difficulty_Relic_RarityRow[] difficultyRarities,
            ChapterDatabase chapters = null,
            Func<int, bool> textKeyExists = null)
        {
            RelicDatabase db = new RelicDatabase();
            List<string> errors = new List<string>();

            db.AddStacks(stacks ?? Array.Empty<RelicStackRow>(), errors);
            db.AddRarities(rarities ?? Array.Empty<RelicRarityRow>(), textKeyExists, errors);
            db.AddConfig(config, errors);
            db.AddRelics(relics ?? Array.Empty<RelicRow>(), effects ?? Array.Empty<Relic_EffectRow>(),
                effectParams ?? Array.Empty<Relic_Effect_ParamRow>(), textKeyExists, errors);
            db.AddDrops(drops ?? Array.Empty<Relic_DropRow>(), chapters, errors);
            db.AddStageChances(stageRelics ?? Array.Empty<Stage_RelicRow>(), chapters, errors);
            db.AddDifficulty(difficultyRelics ?? Array.Empty<Difficulty_RelicRow>(),
                difficultyRarities ?? Array.Empty<Difficulty_Relic_RarityRow>(), errors);

            if (errors.Count > 0)
                throw new ChapterDataException("유물 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return db;
        }

        private void AddStacks(RelicStackRow[] rows, List<string> errors)
        {
            if (rows.Length == 0)
            {
                errors.Add("RelicStack.json 이 비어 있음");
                return;
            }
            float[] scale = new float[rows.Length];
            bool[] seen = new bool[rows.Length];
            foreach (RelicStackRow row in rows)
            {
                if (row == null || row.stack < 1 || row.stack > rows.Length || seen[row.stack - 1])
                {
                    errors.Add("RelicStack.json stack 은 1부터 연속이고 중복이 없어야 함");
                    return;
                }
                seen[row.stack - 1] = true;
                scale[row.stack - 1] = row.effectScale;
            }
            if (Math.Abs(scale[0] - 1f) > Epsilon)
            {
                errors.Add("RelicStack.json stack 1 의 effectScale 은 1.0 이어야 함");
            }
            for (int i = 1; i < scale.Length; ++i)
            {
                float step = scale[i] - scale[i - 1];
                if (step <= 0f)
                {
                    errors.Add($"RelicStack.json stack {i + 1} effectScale 이 단조 증가가 아님");
                }
                else if (i >= 2 && step > scale[i - 1] - scale[i - 2] + Epsilon)
                {
                    errors.Add($"RelicStack.json stack {i + 1} 한계 증가분이 이전보다 큼(체감 감소 위반)");
                }
            }
            _stackScale = scale;
        }

        private void AddRarities(RelicRarityRow[] rows, Func<int, bool> textKeyExists, List<string> errors)
        {
            foreach (RelicRarityRow row in rows)
            {
                if (row == null || TryParseEnum(row.rarity, out ERelicRarity rarity) == false)
                {
                    errors.Add($"RelicRarity.json rarity 가 ERelicRarity 에 없음: {(row != null ? row.rarity : "null")}");
                    continue;
                }
                if (_rarities.ContainsKey(rarity))
                {
                    errors.Add($"RelicRarity.json rarity {row.rarity} 중복");
                    continue;
                }
                if (row.baseWeight < 1)
                {
                    errors.Add($"RelicRarity.json {row.rarity} baseWeight 는 1 이상이어야 함");
                }
                if (textKeyExists != null && textKeyExists(row.nameKey) == false)
                {
                    errors.Add($"RelicRarity.json {row.rarity} nameKey {row.nameKey} 가 Strings_Ko 에 없음");
                }
                _rarities[rarity] = row;
            }
            foreach (ERelicRarity rarity in (ERelicRarity[])Enum.GetValues(typeof(ERelicRarity)))
            {
                if (_rarities.ContainsKey(rarity) == false)
                {
                    errors.Add($"RelicRarity.json 에 {rarity} 행이 없음(3행 필요)");
                }
            }
        }

        private void AddConfig(RelicConfigRow config, List<string> errors)
        {
            if (config == null)
            {
                errors.Add("RelicConfig.json 이 비어 있음");
                return;
            }
            if (config.lossChanceMul < 0f || config.lossChanceMul > 1f)
            {
                errors.Add("RelicConfig.json lossChanceMul 은 0~1 이어야 함");
            }
            if (config.pityPerMiss < 0f || config.pityMaxBonus < 0f)
            {
                errors.Add("RelicConfig.json pityPerMiss/pityMaxBonus 는 0 이상이어야 함");
            }
            if (config.maxChance <= 0f || config.maxChance > 1f)
            {
                errors.Add("RelicConfig.json maxChance 는 0 초과 1 이하여야 함");
            }
            Config = config;
        }

        private void AddRelics(RelicRow[] relics, Relic_EffectRow[] effects, Relic_Effect_ParamRow[] effectParams,
            Func<int, bool> textKeyExists, List<string> errors)
        {
            foreach (RelicRow row in relics)
            {
                if (row == null || TryParseEnum(row.relicId, out ERelic _) == false)
                {
                    errors.Add($"Relic.json relicId 가 ERelic 에 없음: {(row != null ? row.relicId : "null")}");
                    continue;
                }
                if (_byId.ContainsKey(row.relicId))
                {
                    errors.Add($"Relic.json relicId {row.relicId} 중복");
                    continue;
                }
                if (TryParseEnum(row.category, out ERelicCategory category) == false)
                {
                    errors.Add($"Relic.json {row.relicId} category 가 ERelicCategory 에 없음: {row.category}");
                    continue;
                }
                if (TryParseEnum(row.rarity, out ERelicRarity rarity) == false)
                {
                    errors.Add($"Relic.json {row.relicId} rarity 가 ERelicRarity 에 없음: {row.rarity}");
                    continue;
                }
                if (row.maxStack < 1 || row.maxStack > MaxScaleStack)
                {
                    errors.Add($"Relic.json {row.relicId} maxStack {row.maxStack} 은 1~{MaxScaleStack} 이어야 함");
                }
                if (textKeyExists != null && (textKeyExists(row.nameKey) == false || textKeyExists(row.descKey) == false))
                {
                    errors.Add($"Relic.json {row.relicId} nameKey/descKey 가 Strings_Ko 에 없음");
                }
                RelicData data = new RelicData
                {
                    RelicId = row.relicId,
                    Category = category,
                    Rarity = rarity,
                    MaxStack = row.maxStack,
                    NameKey = row.nameKey,
                    DescKey = row.descKey,
                    Effects = new List<RelicEffectData>(),
                };
                _relics.Add(data);
                _byId[row.relicId] = data;
            }

            //# (relicId, effectIndex) -> 파라미터 사전. 중복 키는 오류.
            Dictionary<string, Dictionary<string, float>> paramMap = new Dictionary<string, Dictionary<string, float>>();
            foreach (Relic_Effect_ParamRow row in effectParams)
            {
                if (row == null || _byId.ContainsKey(row.relicId) == false)
                {
                    errors.Add($"Relic_Effect_Param.json relicId 가 Relic 에 없음: {(row != null ? row.relicId : "null")}");
                    continue;
                }
                string mapKey = row.relicId + "#" + row.effectIndex;
                if (paramMap.TryGetValue(mapKey, out Dictionary<string, float> map) == false)
                {
                    map = new Dictionary<string, float>();
                    paramMap[mapKey] = map;
                }
                if (map.ContainsKey(row.paramKey))
                {
                    errors.Add($"Relic_Effect_Param.json {row.relicId}/{row.effectIndex}/{row.paramKey} 중복");
                    continue;
                }
                map[row.paramKey] = row.value;
            }

            Dictionary<string, List<Relic_EffectRow>> effectsByRelic = new Dictionary<string, List<Relic_EffectRow>>();
            foreach (Relic_EffectRow row in effects)
            {
                if (row == null || _byId.ContainsKey(row.relicId) == false)
                {
                    errors.Add($"Relic_Effect.json relicId 가 Relic 에 없음: {(row != null ? row.relicId : "null")}");
                    continue;
                }
                if (effectsByRelic.TryGetValue(row.relicId, out List<Relic_EffectRow> list) == false)
                {
                    list = new List<Relic_EffectRow>();
                    effectsByRelic[row.relicId] = list;
                }
                list.Add(row);
            }

            foreach (RelicData data in _relics)
            {
                if (effectsByRelic.TryGetValue(data.RelicId, out List<Relic_EffectRow> rows) == false || rows.Count == 0)
                {
                    errors.Add($"Relic_Effect.json 에 {data.RelicId} 효과가 1개 이상 필요");
                    continue;
                }
                rows.Sort((a, b) => a.effectIndex.CompareTo(b.effectIndex));
                List<RelicEffectData> built = (List<RelicEffectData>)data.Effects;
                for (int i = 0; i < rows.Count; ++i)
                {
                    Relic_EffectRow row = rows[i];
                    if (row.effectIndex != i + 1)
                    {
                        errors.Add($"Relic_Effect.json {data.RelicId} effectIndex 는 1부터 연속이고 중복이 없어야 함");
                        break;
                    }
                    if (TryParseEnum(row.effectKind, out ERelicEffectKind kind) == false)
                    {
                        errors.Add($"Relic_Effect.json {data.RelicId}/{row.effectIndex} effectKind 가 ERelicEffectKind 에 없음: {row.effectKind}");
                        continue;
                    }
                    paramMap.TryGetValue(data.RelicId + "#" + row.effectIndex, out Dictionary<string, float> ps);
                    bool complete = true;
                    foreach (string required in RequiredParams[kind])
                    {
                        if (ps == null || ps.ContainsKey(required) == false)
                        {
                            errors.Add($"Relic_Effect_Param.json {data.RelicId}/{row.effectIndex} 에 {kind} 필수 키 {required} 없음");
                            complete = false;
                        }
                    }
                    if (complete)
                    {
                        built.Add(new RelicEffectData { Kind = kind, Params = ps });
                    }
                }
            }
        }

        private void AddDrops(Relic_DropRow[] drops, ChapterDatabase chapters, List<string> errors)
        {
            HashSet<string> heroIds = new HashSet<string>();
            if (chapters != null)
            {
                foreach (ChapterRow chapter in chapters.Chapters)
                {
                    heroIds.Add(chapter.heroId);
                }
            }

            HashSet<string> seen = new HashSet<string>();
            HashSet<string> relicsWithDrop = new HashSet<string>();
            HashSet<string> heroesWithCommon = new HashSet<string>();
            foreach (Relic_DropRow row in drops)
            {
                if (row == null || _byId.ContainsKey(row.relicId) == false)
                {
                    errors.Add($"Relic_Drop.json relicId 가 Relic 에 없음: {(row != null ? row.relicId : "null")}");
                    continue;
                }
                if (chapters != null && heroIds.Contains(row.heroId) == false)
                {
                    errors.Add($"Relic_Drop.json {row.relicId} heroId {row.heroId} 가 Chapter.json 에 없음");
                    continue;
                }
                if (seen.Add(row.relicId + "#" + row.heroId) == false)
                {
                    errors.Add($"Relic_Drop.json ({row.relicId}, {row.heroId}) 중복");
                    continue;
                }
                if (row.weight < 1)
                {
                    errors.Add($"Relic_Drop.json ({row.relicId}, {row.heroId}) weight 는 1 이상이어야 함");
                    continue;
                }
                if (_dropsByHero.TryGetValue(row.heroId, out List<Relic_DropRow> list) == false)
                {
                    list = new List<Relic_DropRow>();
                    _dropsByHero[row.heroId] = list;
                }
                list.Add(row);
                relicsWithDrop.Add(row.relicId);
                if (_byId[row.relicId].Rarity == ERelicRarity.Common)
                {
                    heroesWithCommon.Add(row.heroId);
                }
            }
            foreach (RelicData data in _relics)
            {
                if (relicsWithDrop.Contains(data.RelicId) == false)
                {
                    errors.Add($"Relic_Drop.json 어느 영웅에도 {data.RelicId} 가 없음(드랍 불가능 유물)");
                }
            }
            foreach (string heroId in heroIds)
            {
                if (heroesWithCommon.Contains(heroId) == false)
                {
                    errors.Add($"Relic_Drop.json 영웅 {heroId} 에 Common 후보가 없음");
                }
            }
        }

        private void AddStageChances(Stage_RelicRow[] rows, ChapterDatabase chapters, List<string> errors)
        {
            foreach (Stage_RelicRow row in rows)
            {
                if (row == null)
                    continue;
                if (row.dropChance < 0f || row.dropChance > 1f)
                {
                    errors.Add($"Stage_Relic.json ({row.chapterId}, {row.stageIndex}) dropChance 는 0~1 이어야 함");
                    continue;
                }
                if (_stageChance.ContainsKey(Key(row.chapterId, row.stageIndex)))
                {
                    errors.Add($"Stage_Relic.json ({row.chapterId}, {row.stageIndex}) 중복");
                    continue;
                }
                _stageChance[Key(row.chapterId, row.stageIndex)] = row.dropChance;
            }
            if (chapters == null)
                return;
            foreach (ChapterRow chapter in chapters.Chapters)
            {
                for (int index = 1; index <= StageProgress.StagesPerChapter; ++index)
                {
                    if (chapters.GetStage(chapter.chapterId, index) != null && _stageChance.ContainsKey(Key(chapter.chapterId, index)) == false)
                    {
                        errors.Add($"Stage_Relic.json 에 ({chapter.chapterId}, {index}) 행이 없음");
                    }
                }
            }
        }

        private void AddDifficulty(Difficulty_RelicRow[] diffRows, Difficulty_Relic_RarityRow[] rarityRows, List<string> errors)
        {
            int count = DifficultyTable.RequiredCount;
            float[] mul = new float[count];
            bool[] seen = new bool[count];
            foreach (Difficulty_RelicRow row in diffRows)
            {
                if (row == null || row.difficulty < 0 || row.difficulty >= count || seen[row.difficulty])
                {
                    errors.Add("Difficulty_Relic.json difficulty 는 0~2 이고 중복이 없어야 함");
                    continue;
                }
                seen[row.difficulty] = true;
                mul[row.difficulty] = row.dropChanceMul;
            }
            for (int d = 0; d < count; ++d)
            {
                if (seen[d] == false)
                {
                    errors.Add($"Difficulty_Relic.json 에 difficulty {d} 행이 없음");
                    continue;
                }
                if (d == 0 && Math.Abs(mul[0] - 1f) > Epsilon)
                {
                    errors.Add("Difficulty_Relic.json Easy(0) 의 dropChanceMul 은 1.0 이어야 함");
                }
                if (d > 0 && seen[d - 1] && mul[d] < mul[d - 1])
                {
                    errors.Add($"Difficulty_Relic.json difficulty {d} dropChanceMul 이 이전 단계보다 작음(단조 비감소 위반)");
                }
            }
            _diffChanceMul = mul;

            ERelicRarity[] allRarities = (ERelicRarity[])Enum.GetValues(typeof(ERelicRarity));
            foreach (Difficulty_Relic_RarityRow row in rarityRows)
            {
                if (row == null || row.difficulty < 0 || row.difficulty >= count || TryParseEnum(row.rarity, out ERelicRarity rarity) == false)
                {
                    errors.Add("Difficulty_Relic_Rarity.json difficulty(0~2)/rarity 가 올바르지 않음");
                    continue;
                }
                long key = Key(row.difficulty, (int)rarity);
                if (_rarityWeightMul.ContainsKey(key))
                {
                    errors.Add($"Difficulty_Relic_Rarity.json ({row.difficulty}, {row.rarity}) 중복");
                    continue;
                }
                if (row.weightMul <= 0f)
                {
                    errors.Add($"Difficulty_Relic_Rarity.json ({row.difficulty}, {row.rarity}) weightMul 은 0 보다 커야 함");
                    continue;
                }
                _rarityWeightMul[key] = row.weightMul;
            }
            foreach (ERelicRarity rarity in allRarities)
            {
                for (int d = 0; d < count; ++d)
                {
                    if (_rarityWeightMul.TryGetValue(Key(d, (int)rarity), out float w) == false)
                    {
                        errors.Add($"Difficulty_Relic_Rarity.json 에 ({d}, {rarity}) 행이 없음");
                        continue;
                    }
                    if (d == 0 && Math.Abs(w - 1f) > Epsilon)
                    {
                        errors.Add($"Difficulty_Relic_Rarity.json Easy {rarity} weightMul 은 1.0 이어야 함");
                    }
                    if (d > 0 && _rarityWeightMul.TryGetValue(Key(d - 1, (int)rarity), out float prev))
                    {
                        bool violated = rarity == ERelicRarity.Common ? w > prev : w < prev;
                        if (violated)
                        {
                            errors.Add($"Difficulty_Relic_Rarity.json {rarity} 가 난이도 {d} 에서 단조 규칙 위반");
                        }
                    }
                }
            }
        }

        private static bool TryParseEnum<T>(string name, out T value) where T : struct
        {
            value = default;
            return string.IsNullOrEmpty(name) == false
                && Enum.TryParse(name, out value)
                && Enum.IsDefined(typeof(T), value)
                && int.TryParse(name, out int _) == false;
        }
    }
}
