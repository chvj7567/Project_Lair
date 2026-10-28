using System;
using System.Collections.Generic;
using Lair.Battle;

namespace Lair.Data
{
    //# DailyConfig.json (단일 객체) — 핵심 엔티티: 데일리 모드 전역 규칙 수치 (daily-challenge §4.2).
    [Serializable]
    public class DailyConfigRow
    {
        public int utcOffsetMinutes;
        public int unlockClearedStage;
        public int attemptsPerDay;
        public int baseStageIndex;
        public int difficulty;
        public int commonRelicStack;
        public int rareRelicStack;
        public int firstWinSouls;
        public float streakBonusPerDay;
        public int streakBonusMaxDays;
        public int leaderboardTop;
    }

    //# 규칙 프리셋 하나 = 기존 Stage_Rule 행 하나 (chapterId, stageIndex, ruleIndex) + 그 파라미터·트리거 (§2.5). 신규 수치 0.
    public sealed class DailyRulePreset
    {
        public int ChapterId;
        public int StageIndex;
        public int RuleIndex;
        public StageRuleData Data;

        //# comboKey 조각 "2-4#1".
        public string Key => $"{ChapterId}-{StageIndex}#{RuleIndex}";
    }

    //# dayIndex 하루의 조합 — 저장하지 않고 매일 다시 계산하는 순수 함수 결과 (§4.1).
    public sealed class DailyChallenge
    {
        public int DayIndex;
        public string HeroId;
        public int HeroChapterId;
        public int BaseStageIndex;
        public int Difficulty;
        public DailyRulePreset AlwaysRule;
        public DailyRulePreset PhaseRule;
        public string RelicAId;
        public int RelicAStack;
        public string RelicBId;
        public int RelicBStack;
        public int PassiveSeed;
        public int ActiveSeed;

        //# 전투 규칙 목록 — [상시, 페이즈] 순 (StageRuleRunner 에 그대로 전달).
        public IReadOnlyList<StageRuleData> Rules => new[] { AlwaysRule.Data, PhaseRule.Data };

        //# 조합 지문 "Archer|2-4#1|3-5#2|RustyBladex3|ThunderShardx1" (§6.3) — 사후 버전 차이 확인용.
        public string ComboKey => $"{HeroId}|{AlwaysRule.Key}|{PhaseRule.Key}|{RelicAId}x{RelicAStack}|{RelicBId}x{RelicBStack}";
    }

    //# 데일리 설정 검증 + 조합 생성 (daily-challenge §2). 풀은 기존 Chapter/Stage_Rule/Relic 데이터에서 자동 도출한다(D-12). Unity 비의존.
    public class DailyDatabase
    {
        //# 로드·검증 성공 시에만 채워진다. null 이면 데일리 비활성 — 마을 타일을 숨긴다 (E-9).
        public static DailyDatabase Current { get; set; }

        //# 출시 후 변경 금지 — 바꾸면 같은 날 조합이 클라이언트 버전마다 갈린다 (§2.3). 탑은 시즌 시드 + 자체 salt 를 써 스트림이 겹치지 않는다(D-10).
        public const ulong DailySalt = 0x4C41495244414C59UL;
        private const ulong HeroBlockSalt = 0xB10CUL;
        private const float FractionEpsilon = 0.0001f;
        private const string UnlockParamKey = "unlockHpFraction";

        private readonly List<string> _heroIds = new List<string>();
        private readonly Dictionary<string, int> _heroChapter = new Dictionary<string, int>();
        private readonly List<DailyRulePreset> _alwaysPool = new List<DailyRulePreset>();
        private readonly List<DailyRulePreset> _phasePool = new List<DailyRulePreset>();
        private readonly List<RelicData> _commonRelics = new List<RelicData>();
        private readonly List<RelicData> _rareRelics = new List<RelicData>();

        private DailyDatabase(DailyConfigRow config)
        {
            Config = config;
        }

        public DailyConfigRow Config { get; }

        public IReadOnlyList<string> HeroIds => _heroIds;
        public IReadOnlyList<DailyRulePreset> AlwaysPool => _alwaysPool;
        public IReadOnlyList<DailyRulePreset> PhasePool => _phasePool;
        public IReadOnlyList<RelicData> CommonRelics => _commonRelics;
        public IReadOnlyList<RelicData> RareRelics => _rareRelics;

        public bool IsUnlocked(int clearedStage)
        {
            return clearedStage >= Config.unlockClearedStage;
        }

        //# maxDifficulty: Difficulty.json 최대 인덱스(표 미로드면 0). 위반은 전부 모아 ChapterDataException.
        public static DailyDatabase Build(DailyConfigRow config, ChapterDatabase chapters, RelicDatabase relics, int maxDifficulty)
        {
            if (config == null)
                throw new ChapterDataException("DailyConfig.json 이 비어 있음");
            if (chapters == null)
                throw new ChapterDataException("데일리 검증에 챕터 데이터가 필요함");
            if (relics == null)
                throw new ChapterDataException("데일리 오늘의 유물에 유물 데이터가 필요함");

            List<string> errors = new List<string>();
            ValidateConfig(config, maxDifficulty, errors);
            DailyDatabase db = new DailyDatabase(config);
            db.LoadHeroes(chapters, errors);
            db.LoadRules(chapters);
            db.LoadRelics(relics);
            if (db._alwaysPool.Count == 0 || db._phasePool.Count == 0)
            {
                errors.Add($"데일리 규칙 풀이 비어 있음(상시 {db._alwaysPool.Count}, 페이즈 {db._phasePool.Count})");
            }
            if (db._commonRelics.Count == 0 || db._rareRelics.Count == 0)
            {
                errors.Add($"데일리 오늘의 유물 풀이 비어 있음(일반 {db._commonRelics.Count}, 희귀·전설 {db._rareRelics.Count})");
            }
            if (errors.Count > 0)
                throw new ChapterDataException("데일리 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return db;
        }

        private static void ValidateConfig(DailyConfigRow c, int maxDifficulty, List<string> errors)
        {
            if (c.utcOffsetMinutes < -720 || c.utcOffsetMinutes > 840)
            {
                errors.Add("DailyConfig utcOffsetMinutes 는 -720~840 이어야 함");
            }
            if (c.unlockClearedStage < 1 || c.unlockClearedStage > StageProgress.MaxStage)
            {
                errors.Add($"DailyConfig unlockClearedStage 는 1~{StageProgress.MaxStage} 이어야 함");
            }
            if (c.attemptsPerDay < 1)
            {
                errors.Add("DailyConfig attemptsPerDay 는 1 이상이어야 함");
            }
            if (c.baseStageIndex < 1 || c.baseStageIndex > StageProgress.StagesPerChapter)
            {
                errors.Add($"DailyConfig baseStageIndex 는 1~{StageProgress.StagesPerChapter} 이어야 함");
            }
            if (c.difficulty < 0 || c.difficulty > maxDifficulty)
            {
                errors.Add($"DailyConfig difficulty 는 0~{maxDifficulty} 이어야 함(Difficulty.json 범위)");
            }
            if (c.commonRelicStack < 1 || c.rareRelicStack < 1)
            {
                errors.Add("DailyConfig 오늘의 유물 중첩은 1 이상이어야 함");
            }
            if (c.firstWinSouls < 0)
            {
                errors.Add("DailyConfig firstWinSouls 는 0 이상이어야 함");
            }
            if (c.streakBonusPerDay >= 0f == false)
            {
                errors.Add("DailyConfig streakBonusPerDay 는 0 이상이어야 함");
            }
            if (c.streakBonusMaxDays < 1)
            {
                errors.Add("DailyConfig streakBonusMaxDays 는 1 이상이어야 함");
            }
            if (c.leaderboardTop < 1 || c.leaderboardTop > 100)
            {
                errors.Add("DailyConfig leaderboardTop 은 1~100 이어야 함");
            }
        }

        //# 영웅 풀 = Chapter.heroId 를 chapterId 오름차순으로 (§2.4). 파일 행 순서 무관. 기준 스테이지가 없는 영웅은 오류.
        private void LoadHeroes(ChapterDatabase chapters, List<string> errors)
        {
            List<ChapterRow> sorted = new List<ChapterRow>(chapters.Chapters);
            sorted.Sort((a, b) => a.chapterId.CompareTo(b.chapterId));
            foreach (ChapterRow chapter in sorted)
            {
                if (string.IsNullOrEmpty(chapter.heroId) || _heroChapter.ContainsKey(chapter.heroId))
                    continue;
                if (chapters.GetHero(chapter.heroId) == null)
                {
                    errors.Add($"데일리 영웅 {chapter.heroId} 가 HeroArchetype 에 없음");
                    continue;
                }
                if (chapters.GetStage(chapter.chapterId, Config.baseStageIndex) == null)
                {
                    errors.Add($"데일리 기준 스테이지 {chapter.chapterId}-{Config.baseStageIndex} 가 Stage 에 없음");
                    continue;
                }
                _heroIds.Add(chapter.heroId);
                _heroChapter[chapter.heroId] = chapter.chapterId;
            }
            if (_heroIds.Count == 0)
            {
                errors.Add("데일리 영웅 풀이 비어 있음");
            }
        }

        //# 트리거 유무로 상시/페이즈 자동 분류 — (chapterId, stageIndex, ruleIndex) 오름차순.
        private void LoadRules(ChapterDatabase chapters)
        {
            List<ChapterRow> sorted = new List<ChapterRow>(chapters.Chapters);
            sorted.Sort((a, b) => a.chapterId.CompareTo(b.chapterId));
            foreach (ChapterRow chapter in sorted)
            {
                for (int stage = 1; stage <= StageProgress.StagesPerChapter; ++stage)
                {
                    foreach (StageRuleData data in chapters.GetRules(chapter.chapterId, stage))
                    {
                        DailyRulePreset preset = new DailyRulePreset
                        {
                            ChapterId = chapter.chapterId,
                            StageIndex = stage,
                            RuleIndex = data.RuleIndex,
                            Data = data,
                        };
                        if (data.HasTrigger)
                        {
                            _phasePool.Add(preset);
                        }
                        else
                        {
                            _alwaysPool.Add(preset);
                        }
                    }
                }
            }
            _alwaysPool.Sort(ComparePreset);
            _phasePool.Sort(ComparePreset);
        }

        //# A = 일반·능력치/전투, B = 희귀·전설·능력치/전투. 편의 유물 제외 (§2.6). relicId 서수 정렬.
        private void LoadRelics(RelicDatabase relics)
        {
            foreach (RelicData data in relics.Relics)
            {
                if (data.Category != ERelicCategory.Stat && data.Category != ERelicCategory.Combat)
                    continue;
                if (data.Rarity == ERelicRarity.Common)
                {
                    _commonRelics.Add(data);
                }
                else
                {
                    _rareRelics.Add(data);
                }
            }
            _commonRelics.Sort((a, b) => string.CompareOrdinal(a.RelicId, b.RelicId));
            _rareRelics.Sort((a, b) => string.CompareOrdinal(a.RelicId, b.RelicId));
        }

        private static int ComparePreset(DailyRulePreset a, DailyRulePreset b)
        {
            int byChapter = a.ChapterId.CompareTo(b.ChapterId);
            if (byChapter != 0)
                return byChapter;
            int byStage = a.StageIndex.CompareTo(b.StageIndex);
            return byStage != 0 ? byStage : a.RuleIndex.CompareTo(b.RuleIndex);
        }

        public int HeroChapterOf(string heroId)
        {
            return heroId != null && _heroChapter.TryGetValue(heroId, out int chapterId) ? chapterId : 0;
        }

        //# 하루 조합 — dayIndex 만 받는 순수 함수(서버 시각·세이브 비의존, §12.1). 뽑기 순서 고정(§2.3): 영웅 → 상시 → 페이즈 → 유물 A → 유물 B → 패시브 시드 → 액티브 시드.
        public DailyChallenge Generate(int dayIndex)
        {
            int day = Math.Max(0, dayIndex);
            string heroId = HeroOfDay(day);
            TowerRandom rng = new TowerRandom(DailySalt ^ (ulong)day);

            DailyRulePreset always = _alwaysPool[NextInt(ref rng, _alwaysPool.Count)];
            List<DailyRulePreset> phaseCandidates = PhaseCandidates(always);
            DailyRulePreset phase = phaseCandidates[NextInt(ref rng, phaseCandidates.Count)];
            RelicData relicA = _commonRelics[NextInt(ref rng, _commonRelics.Count)];
            RelicData relicB = _rareRelics[NextInt(ref rng, _rareRelics.Count)];
            int passiveSeed = NextSeed(ref rng);
            int activeSeed = NextSeed(ref rng);

            return new DailyChallenge
            {
                DayIndex = day,
                HeroId = heroId,
                HeroChapterId = _heroChapter[heroId],
                BaseStageIndex = Config.baseStageIndex,
                Difficulty = Config.difficulty,
                AlwaysRule = always,
                PhaseRule = phase,
                RelicAId = relicA.RelicId,
                RelicAStack = Math.Min(Config.commonRelicStack, relicA.MaxStack),
                RelicBId = relicB.RelicId,
                RelicBStack = Math.Min(Config.rareRelicStack, relicB.MaxStack),
                PassiveSeed = passiveSeed,
                ActiveSeed = activeSeed,
            };
        }

        //# 영웅 블록 순열 (§2.4) — 블록마다 영웅 수만큼의 날에 전원 1회. 직전 블록 끝 영웅과 같으면 앞 두 칸을 맞바꾼다(직전 1블록만 계산).
        public string HeroOfDay(int dayIndex)
        {
            int count = _heroIds.Count;
            int day = Math.Max(0, dayIndex);
            int block = day / count;
            List<string> perm = BlockPermutation(block);
            if (block > 0 && count >= 2)
            {
                List<string> prev = BlockPermutation(block - 1);
                if (perm[0] == prev[count - 1])
                {
                    string swap = perm[0];
                    perm[0] = perm[1];
                    perm[1] = swap;
                }
            }
            return perm[day % count];
        }

        private List<string> BlockPermutation(int block)
        {
            List<string> perm = new List<string>(_heroIds);
            TowerFloorGenerator.Shuffle(perm, new TowerRandom(DailySalt ^ ((ulong)(uint)block << 32) ^ HeroBlockSalt));
            return perm;
        }

        //# HP 임계 충돌 방지 (§2.5) — 상시가 봉인된 둥지이고 해제 임계가 페이즈 트리거 임계와 같으면 그 페이즈를 후보에서 뺀다. 전부 빠지면 원래 풀.
        private List<DailyRulePreset> PhaseCandidates(DailyRulePreset always)
        {
            List<DailyRulePreset> candidates = new List<DailyRulePreset>();
            bool sealedNest = always.Data.RuleId == EStageRule.SealedNest
                && always.Data.Params != null && always.Data.Params.ContainsKey(UnlockParamKey);
            float unlock = sealedNest ? always.Data.Params[UnlockParamKey] : 0f;
            foreach (DailyRulePreset phase in _phasePool)
            {
                if (sealedNest && Math.Abs(phase.Data.TriggerHpFraction - unlock) < FractionEpsilon)
                    continue;
                candidates.Add(phase);
            }
            return candidates.Count > 0 ? candidates : new List<DailyRulePreset>(_phasePool);
        }

        private static int NextInt(ref TowerRandom rng, int n)
        {
            return (int)(rng.Next() % (ulong)n);
        }

        //# CardDeck(seed) 는 0 을 "무작위" 로 해석하므로 0 이면 1 (§2.3 순번 5·6).
        private static int NextSeed(ref TowerRandom rng)
        {
            int seed = (int)(rng.Next() & 0x7FFFFFFFUL);
            return seed == 0 ? 1 : seed;
        }
    }
}
