using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 던전 편성 데이터 4종 검증 + 조회 (dungeon-formation §3.3). Unity 비의존 순수 클래스. 위반 시 ChapterDataException 으로 전부 모아 던진다.
    public class FormationDatabase
    {
        //# 슬롯 수 6 고정(= 씬 스포너 6개, 슬롯 수 변경은 범위 밖).
        public const int SlotCount = 6;

        //# 로드 성공 시에만 채워진다. null 이면 "편성 기능 비활성" — 마을은 바로 출격, 전투는 씬 기본 배치(§3.3-8).
        public static FormationDatabase Current { get; set; }

        private readonly Dictionary<EMonster, int> _cost = new Dictionary<EMonster, int>();
        private readonly List<EMonster> _monsters = new List<EMonster>();
        private readonly EMonster[] _default = new EMonster[SlotCount];
        private readonly Dictionary<int, int> _hintKeyByChapter = new Dictionary<int, int>();

        private FormationDatabase(FormationConfigRow config)
        {
            Config = config;
        }

        public FormationConfigRow Config { get; }

        //# JSON 순서 그대로 — 종 팔레트 표시 순서.
        public IReadOnlyList<EMonster> Monsters => _monsters;

        //# 코스트 — 알 수 없는 종은 0(= 무효로 판정되는 값).
        public int CostOf(EMonster monster)
        {
            return _cost.TryGetValue(monster, out int cost) ? cost : 0;
        }

        //# 기본 편성 복사본(슬롯 1~6 순서).
        public EMonster[] DefaultFormation()
        {
            return (EMonster[])_default.Clone();
        }

        //# 챕터 영웅 성향 문구 id — 없으면 0.
        public int HintKeyOf(int chapterId)
        {
            return _hintKeyByChapter.TryGetValue(chapterId, out int key) ? key : 0;
        }

        //# chapterIds: Chapter.json 의 모든 chapterId(null 이면 힌트 챕터 덮임 검사 생략). hintKeyExists: Strings_Ko id 존재 판정(null 이면 생략).
        public static FormationDatabase Build(
            FormationConfigRow config, FormationMonsterRow[] monsters, FormationSlotRow[] slots, Chapter_FormationHintRow[] hints,
            IEnumerable<int> chapterIds = null, Func<int, bool> hintKeyExists = null)
        {
            if (config == null)
                throw new ChapterDataException("FormationConfig.json 이 비어 있음");

            List<string> errors = new List<string>();
            FormationDatabase db = new FormationDatabase(config);
            db.LoadMonsters(monsters, errors);
            db.LoadSlots(slots, errors);
            ValidateConfig(config, errors);
            db.ValidateDefaultFormation(errors);
            db.LoadHints(hints, chapterIds, hintKeyExists, errors);
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return db;
        }

        private void LoadMonsters(FormationMonsterRow[] rows, List<string> errors)
        {
            int required = Enum.GetValues(typeof(EMonster)).Length;
            if (rows == null || rows.Length != required)
            {
                errors.Add($"FormationMonster.json 행이 EMonster 전 종({required})이어야 함(현재 {(rows == null ? 0 : rows.Length)})");
                return;
            }
            foreach (FormationMonsterRow row in rows)
            {
                if (row == null || TryParseMonster(row.monster, out EMonster monster) == false)
                {
                    errors.Add($"FormationMonster.json monster '{row?.monster}' 가 EMonster 값명이 아님");
                    continue;
                }
                if (_cost.ContainsKey(monster))
                {
                    errors.Add($"FormationMonster.json monster {monster} 중복");
                    continue;
                }
                if (row.cost < 1)
                {
                    errors.Add($"FormationMonster.json {monster} cost 는 1 이상이어야 함");
                }
                _cost[monster] = row.cost;
                _monsters.Add(monster);
            }
        }

        private void LoadSlots(FormationSlotRow[] rows, List<string> errors)
        {
            if (rows == null || rows.Length != SlotCount)
            {
                errors.Add($"FormationSlot.json 행이 정확히 {SlotCount}개여야 함(현재 {(rows == null ? 0 : rows.Length)})");
                return;
            }
            bool[] seen = new bool[SlotCount];
            foreach (FormationSlotRow row in rows)
            {
                if (row == null || row.slot < 1 || row.slot > SlotCount)
                {
                    errors.Add("FormationSlot.json slot 은 1~6 이어야 함");
                    continue;
                }
                if (seen[row.slot - 1])
                {
                    errors.Add($"FormationSlot.json slot {row.slot} 중복");
                    continue;
                }
                seen[row.slot - 1] = true;
                if (TryParseMonster(row.defaultMonster, out EMonster monster) == false || _cost.ContainsKey(monster) == false)
                {
                    errors.Add($"FormationSlot.json slot {row.slot} defaultMonster '{row.defaultMonster}' 가 FormationMonster 에 없음");
                    continue;
                }
                _default[row.slot - 1] = monster;
            }
        }

        private static void ValidateConfig(FormationConfigRow c, List<string> errors)
        {
            if (c.costCap < SlotCount)
            {
                errors.Add($"FormationConfig costCap {c.costCap} 는 {SlotCount}(최저 코스트 1 x 슬롯 수) 이상이어야 함");
            }
            if (c.maxPerSpeciesBase < 1 || c.maxPerSpeciesBase > c.maxPerSpeciesExpanded || c.maxPerSpeciesExpanded > SlotCount)
            {
                errors.Add("FormationConfig 는 1 <= maxPerSpeciesBase <= maxPerSpeciesExpanded <= 6 이어야 함");
            }
            if (c.unlockClearedStage < 0 || c.unlockClearedStage > c.expandClearedStage)
            {
                errors.Add("FormationConfig 는 0 <= unlockClearedStage <= expandClearedStage 이어야 함");
            }
            if (c.maxPerSpeciesBase * Enum.GetValues(typeof(EMonster)).Length < SlotCount)
            {
                errors.Add("FormationConfig maxPerSpeciesBase x 종 수 가 슬롯 수보다 작아 6곳을 채울 수 없음");
            }
        }

        //# 기본 편성이 기본 해금 단계(종당 base)·상한 안이어야 한다 — 아니면 편성을 안 바꾼 유저가 출격 불가가 된다.
        private void ValidateDefaultFormation(List<string> errors)
        {
            if (errors.Count > 0)
                return;
            string issue = FormationRules.Describe(_default, Config.costCap, Config.maxPerSpeciesBase, CostOf);
            if (issue != null)
            {
                errors.Add($"기본 편성이 유효하지 않음: {issue}");
            }
        }

        private void LoadHints(Chapter_FormationHintRow[] rows, IEnumerable<int> chapterIds, Func<int, bool> hintKeyExists, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Chapter_FormationHint.json 이 비어 있음");
                return;
            }
            foreach (Chapter_FormationHintRow row in rows)
            {
                if (row == null)
                    continue;
                if (_hintKeyByChapter.ContainsKey(row.chapterId))
                {
                    errors.Add($"Chapter_FormationHint.json chapterId {row.chapterId} 중복");
                    continue;
                }
                if (hintKeyExists != null && hintKeyExists(row.hintKey) == false)
                {
                    errors.Add($"Chapter_FormationHint.json chapterId {row.chapterId} hintKey {row.hintKey} 가 Strings_Ko 에 없음");
                }
                _hintKeyByChapter[row.chapterId] = row.hintKey;
            }
            if (chapterIds == null)
                return;
            HashSet<int> chapters = new HashSet<int>(chapterIds);
            foreach (int chapterId in chapters)
            {
                if (_hintKeyByChapter.ContainsKey(chapterId) == false)
                {
                    errors.Add($"Chapter_FormationHint.json 에 chapterId {chapterId} 행이 없음");
                }
            }
            foreach (int hintChapter in _hintKeyByChapter.Keys)
            {
                if (chapters.Contains(hintChapter) == false)
                {
                    errors.Add($"Chapter_FormationHint.json chapterId {hintChapter} 가 Chapter.json 에 없음");
                }
            }
        }

        //# Enum.TryParse 는 숫자 문자열도 받으므로 값명 정확 일치만 허용한다.
        public static bool TryParseMonster(string name, out EMonster monster)
        {
            monster = default;
            if (string.IsNullOrEmpty(name) || Array.IndexOf(Enum.GetNames(typeof(EMonster)), name) < 0)
                return false;
            monster = (EMonster)Enum.Parse(typeof(EMonster), name);
            return true;
        }
    }

    //# Card_Species.json 검증 + 조회 (dungeon-formation §3.3-6, §5.2). FormationDatabase 와 독립 — 이것만 실패하면 카드 제외만 꺼진다(E-2).
    public class FormationCardSpecies
    {
        //# null 이면 카드 후보 제외 비활성.
        public static FormationCardSpecies Current { get; set; }

        private readonly Dictionary<ECardId, List<EMonster>> _species = new Dictionary<ECardId, List<EMonster>>();

        private FormationCardSpecies() { }

        public static FormationCardSpecies Build(Card_SpeciesRow[] rows, FormationDatabase formation)
        {
            if (rows == null)
                throw new ChapterDataException("Card_Species.json 이 비어 있음");

            List<string> errors = new List<string>();
            FormationCardSpecies table = new FormationCardSpecies();
            HashSet<string> seen = new HashSet<string>();
            foreach (Card_SpeciesRow row in rows)
            {
                if (row == null)
                    continue;
                bool cardOk = row.cardId != null && Array.IndexOf(Enum.GetNames(typeof(ECardId)), row.cardId) >= 0;
                if (cardOk == false)
                {
                    errors.Add($"Card_Species.json cardId '{row.cardId}' 가 ECardId 값명이 아님");
                }
                bool monsterOk = FormationDatabase.TryParseMonster(row.monster, out EMonster monster)
                    && (formation == null || formation.CostOf(monster) > 0);
                if (monsterOk == false)
                {
                    errors.Add($"Card_Species.json monster '{row.monster}' 가 FormationMonster 에 없음");
                }
                if (seen.Add(row.cardId + "|" + row.monster) == false)
                {
                    errors.Add($"Card_Species.json ({row.cardId}, {row.monster}) 중복");
                }
                if (cardOk && monsterOk)
                {
                    ECardId card = (ECardId)Enum.Parse(typeof(ECardId), row.cardId);
                    if (table._species.TryGetValue(card, out List<EMonster> list) == false)
                    {
                        list = new List<EMonster>();
                        table._species[card] = list;
                    }
                    list.Add(monster);
                }
            }
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return table;
        }

        //# 행이 없는 카드는 항상 후보. 행이 있으면 any-of — 하나라도 지금 출력 중이면 후보(§5.2-C).
        public bool IsAvailable(ECardId card, Func<EMonster, bool> isOutputting)
        {
            if (_species.TryGetValue(card, out List<EMonster> list) == false)
                return true;
            foreach (EMonster monster in list)
            {
                if (isOutputting(monster))
                    return true;
            }
            return false;
        }
    }
}
