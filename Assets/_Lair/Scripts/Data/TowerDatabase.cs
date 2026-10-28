using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 한 층의 확정 사양 — 생성 함수 출력 + 능력치 배율 + 유물 등급 단계. 데이터 엔티티가 아니라 (시즌 시드, 층) 의 순수 함수 결과다 (infinite-tower §4.1).
    public sealed class TowerFloor
    {
        public int Floor;
        public int Band;
        public int Position;          //# 구간 내 위치 1~5 — 영웅 외형 단계로도 쓴다
        public bool IsBoss;
        public string HeroId;
        public int HeroChapterId;     //# 영웅을 쓰는 챕터 — 배경·외형·유물 키의 출처
        public int TemplateChapterId; //# 규칙 묶음 출처 스테이지
        public int TemplateStageIndex;
        public float RuleFactor;
        public decimal FloorHp;
        public decimal FloorPower;
        public int RelicTier;         //# 탑 유물 등급 단계 0/1/2 (Difficulty_Relic 인덱스)
    }

    //# 탑 데이터 3종 검증 + 조회 (infinite-tower §4.5). Unity 비의존 순수 클래스. 위반 시 ChapterDataException 으로 전부 모아 던진다.
    public class TowerDatabase
    {
        //# 로드 성공 시에만 채워진다. null 이면 "탑 비활성" — 마을 탑 버튼은 보이되 누르면 "준비 중" (E-1).
        public static TowerDatabase Current { get; set; }

        private readonly Dictionary<string, HeroArchetype_TowerRow> _heroes = new Dictionary<string, HeroArchetype_TowerRow>();
        private readonly Dictionary<string, int> _heroChapter = new Dictionary<string, int>();
        private readonly Dictionary<TowerTemplateKey, Stage_TowerRow> _templates = new Dictionary<TowerTemplateKey, Stage_TowerRow>();
        private readonly List<string> _heroIds = new List<string>();
        private readonly List<KeyValuePair<TowerTemplateKey, int>> _templateFloors = new List<KeyValuePair<TowerTemplateKey, int>>();
        private readonly List<TowerBand> _bands = new List<TowerBand>();

        private TowerDatabase(TowerConfigRow config)
        {
            Config = config;
        }

        public TowerConfigRow Config { get; }

        //# 탑 등장 영웅 수 — 구간 5층 배정에 5명 이상 필요.
        public int HeroCount => _heroIds.Count;

        //# 5-5 클리어 등 해금 판정 — 챕터 추가와 무관하게 고정값 (D-3).
        public bool IsUnlocked(int clearedStage)
        {
            return clearedStage >= Config.unlockClearedStage;
        }

        public HeroArchetype_TowerRow GetHeroRow(string heroId)
        {
            return heroId != null && _heroes.TryGetValue(heroId, out HeroArchetype_TowerRow row) ? row : null;
        }

        public Stage_TowerRow GetTemplate(int chapterId, int stageIndex)
        {
            return _templates.TryGetValue(new TowerTemplateKey(chapterId, stageIndex), out Stage_TowerRow row) ? row : null;
        }

        //# 탑 유물 등급 단계 — 1~(relicNormalFromFloor-1) Easy(0), 그 이상 Normal(1), relicHardFromFloor 이상 Hard(2). maxIndex 로 클램프 (infinite-tower §3.8).
        public int RelicTierOf(int floor, int maxIndex)
        {
            int tier = floor >= Config.relicHardFromFloor ? 2 : floor >= Config.relicNormalFromFloor ? 1 : 0;
            return Math.Max(0, Math.Min(Math.Max(0, maxIndex), tier));
        }

        //# floor(1~) 층의 확정 사양. 구간은 b-1 의 결과를 참조해 순서대로 계산·메모이즈. floor < 1 은 1층으로 취급.
        //# relicMaxIndex: Difficulty.json 최대 인덱스(표 미로드면 0).
        public TowerFloor GetFloor(int floor, int relicMaxIndex)
        {
            floor = Math.Max(1, floor);
            int bandIndex = TowerFloorGenerator.BandOf(floor);
            while (_bands.Count < bandIndex)
            {
                TowerBand prev = _bands.Count > 0 ? _bands[_bands.Count - 1] : null;
                _bands.Add(TowerFloorGenerator.ComputeBand(Config.seed, _bands.Count + 1, prev, _heroIds, _templateFloors));
            }

            TowerBand band = _bands[bandIndex - 1];
            int position = TowerFloorGenerator.PositionOf(floor);
            bool isBoss = position == TowerFloorGenerator.FloorsPerBand;
            TowerTemplateKey template = isBoss ? band.Boss : band.Normals[position - 1];
            string heroId = band.Heroes[position - 1];
            return new TowerFloor
            {
                Floor = floor,
                Band = bandIndex,
                Position = position,
                IsBoss = isBoss,
                HeroId = heroId,
                HeroChapterId = _heroChapter[heroId],
                TemplateChapterId = template.ChapterId,
                TemplateStageIndex = template.StageIndex,
                RuleFactor = _templates[template].ruleFactor,
                FloorHp = TowerFloorGenerator.FloorMultiplier(Config.hpBase, Config.hpGrowthPerFloor, Config.bossHpMul, Config.multiplierCap, floor, isBoss),
                FloorPower = TowerFloorGenerator.FloorMultiplier(1f, Config.powerGrowthPerFloor, Config.bossPowerMul, Config.multiplierCap, floor, isBoss),
                RelicTier = RelicTierOf(floor, relicMaxIndex),
            };
        }

        public static TowerDatabase Build(TowerConfigRow config, HeroArchetype_TowerRow[] heroes, Stage_TowerRow[] templates, ChapterDatabase chapters)
        {
            if (config == null)
                throw new ChapterDataException("TowerConfig.json 이 비어 있음");
            if (chapters == null)
                throw new ChapterDataException("탑 검증에 챕터 데이터가 필요함");

            List<string> errors = new List<string>();
            ValidateConfig(config, errors);
            TowerDatabase db = new TowerDatabase(config);
            db.LoadHeroes(heroes, chapters, errors);
            db.LoadTemplates(templates, chapters, errors);
            if (errors.Count > 0)
                throw new ChapterDataException("탑 데이터 검증 실패:\n - " + string.Join("\n - ", errors));
            return db;
        }

        private static void ValidateConfig(TowerConfigRow c, List<string> errors)
        {
            if (c.seasonId < 1)
            {
                errors.Add("TowerConfig seasonId 는 1 이상이어야 함");
            }
            if (c.seed < 0)
            {
                errors.Add("TowerConfig seed 는 0 이상이어야 함");
            }
            if (c.unlockClearedStage < 1)
            {
                errors.Add("TowerConfig unlockClearedStage 는 1 이상이어야 함");
            }
            if (c.hpBase <= 0f)
            {
                errors.Add("TowerConfig hpBase 는 0 보다 커야 함");
            }
            if (c.hpGrowthPerFloor < 0f || c.powerGrowthPerFloor < 0f)
            {
                errors.Add("TowerConfig 층당 성장률은 0 이상이어야 함");
            }
            if (c.bossHpMul < 1f || c.bossPowerMul < 1f)
            {
                errors.Add("TowerConfig 보스 배율은 1 이상이어야 함");
            }
            if (c.multiplierCap < 1f)
            {
                errors.Add("TowerConfig multiplierCap 은 1 이상이어야 함");
            }
            if (c.relicNormalFromFloor < 1 || c.relicNormalFromFloor > c.relicHardFromFloor)
            {
                errors.Add("TowerConfig 는 1 <= relicNormalFromFloor <= relicHardFromFloor 이어야 함");
            }
            if (c.previewFloors < 0)
            {
                errors.Add("TowerConfig previewFloors 는 0 이상이어야 함");
            }
            if (c.leaderboardTop < 1 || c.leaderboardTop > 50)
            {
                errors.Add("TowerConfig leaderboardTop 은 1~50 이어야 함");
            }
        }

        private void LoadHeroes(HeroArchetype_TowerRow[] rows, ChapterDatabase chapters, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("HeroArchetype_Tower.json 이 비어 있음");
                return;
            }
            foreach (HeroArchetype_TowerRow row in rows)
            {
                if (row == null || string.IsNullOrEmpty(row.heroId))
                {
                    errors.Add("HeroArchetype_Tower.json heroId 가 비어 있음");
                    continue;
                }
                if (_heroes.ContainsKey(row.heroId))
                {
                    errors.Add($"HeroArchetype_Tower.json heroId {row.heroId} 중복");
                    continue;
                }
                if (chapters.GetHero(row.heroId) == null)
                {
                    errors.Add($"HeroArchetype_Tower.json heroId {row.heroId} 가 HeroArchetype 에 없음");
                    continue;
                }
                int owner = OwnerChapter(chapters, row.heroId, errors);
                if (row.hpMultiplier <= 0f || row.powerMultiplier <= 0f)
                {
                    errors.Add($"HeroArchetype_Tower.json {row.heroId} 배율은 0 보다 커야 함");
                }
                _heroes[row.heroId] = row;
                _heroIds.Add(row.heroId);
                _heroChapter[row.heroId] = owner;
            }
            if (_heroIds.Count < TowerFloorGenerator.FloorsPerBand)
            {
                errors.Add($"HeroArchetype_Tower.json 행이 {TowerFloorGenerator.FloorsPerBand} 개 이상이어야 함(현재 {_heroIds.Count})");
            }
        }

        //# 그 영웅을 쓰는 챕터가 정확히 1개여야 배경·외형·유물 키의 출처가 하나로 정해진다.
        private static int OwnerChapter(ChapterDatabase chapters, string heroId, List<string> errors)
        {
            int owner = 0;
            int count = 0;
            foreach (ChapterRow chapter in chapters.Chapters)
            {
                if (chapter.heroId == heroId)
                {
                    owner = chapter.chapterId;
                    ++count;
                }
            }
            if (count != 1)
            {
                errors.Add($"HeroArchetype_Tower.json {heroId} 를 쓰는 챕터가 정확히 1개여야 함(현재 {count})");
            }
            return owner;
        }

        private void LoadTemplates(Stage_TowerRow[] rows, ChapterDatabase chapters, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Stage_Tower.json 이 비어 있음");
                return;
            }
            int openNormals = 0;
            int openBosses = 0;
            foreach (Stage_TowerRow row in rows)
            {
                if (row == null)
                    continue;
                TowerTemplateKey key = new TowerTemplateKey(row.chapterId, row.stageIndex);
                if (_templates.ContainsKey(key))
                {
                    errors.Add($"Stage_Tower.json ({row.chapterId}, {row.stageIndex}) 중복");
                    continue;
                }
                if (chapters.GetStage(row.chapterId, row.stageIndex) == null)
                {
                    errors.Add($"Stage_Tower.json ({row.chapterId}, {row.stageIndex}) 가 Stage 에 없음");
                    continue;
                }
                if (row.minFloor < 1)
                {
                    errors.Add($"Stage_Tower.json ({row.chapterId}, {row.stageIndex}) minFloor 는 1 이상이어야 함");
                }
                if (row.ruleFactor < 1f)
                {
                    errors.Add($"Stage_Tower.json ({row.chapterId}, {row.stageIndex}) ruleFactor 는 1.0 이상이어야 함");
                }
                _templates[key] = row;
                _templateFloors.Add(new KeyValuePair<TowerTemplateKey, int>(key, row.minFloor));
                if (row.stageIndex == TowerFloorGenerator.FloorsPerBand)
                {
                    if (row.minFloor <= TowerFloorGenerator.FloorsPerBand)
                    {
                        ++openBosses;
                    }
                }
                else if (row.minFloor <= 1)
                {
                    ++openNormals;
                }
            }
            if (openNormals < TowerFloorGenerator.FloorsPerBand - 1)
            {
                errors.Add($"Stage_Tower.json minFloor 1 인 일반 템플릿이 {TowerFloorGenerator.FloorsPerBand - 1} 개 이상이어야 함(현재 {openNormals})");
            }
            if (openBosses < 1)
            {
                errors.Add("Stage_Tower.json minFloor 5 이하 보스 템플릿이 1개 이상이어야 함");
            }
        }
    }
}
