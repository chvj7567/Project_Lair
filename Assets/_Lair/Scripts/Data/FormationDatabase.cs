using System;
using System.Collections.Generic;

namespace Lair.Data
{
    //# 승급 비용 한 줄 — 재화 한 종과 수량 (Grade_Promotion 한 행).
    public struct GradeCost
    {
        public ECurrency Currency;
        public int Amount;
    }

    //# 던전 편성 데이터 검증 + 조회 (dungeon-formation §0.5, monster-grades §8). Unity 비의존 순수 클래스. 위반 시 ChapterDataException 으로 전부 모아 던진다.
    public class FormationDatabase
    {
        //# 슬롯 수 6 고정(= 씬 스포너 6개 = 종족 수, 슬롯 수 변경은 범위 밖).
        public const int SlotCount = 6;

        //# 로드 성공 시에만 채워진다. null 이면 "편성 기능 비활성" — 마을은 바로 출격, 전투는 씬 기본 배치(§0.5-8).
        public static FormationDatabase Current { get; set; }

        private readonly Dictionary<ERace, RaceRow> _races = new Dictionary<ERace, RaceRow>();
        private readonly Dictionary<int, GradeRow> _grades = new Dictionary<int, GradeRow>();
        //# 종족 → 등급순 몬스터 목록.
        private readonly Dictionary<ERace, List<EMonster>> _members = new Dictionary<ERace, List<EMonster>>();
        private readonly Dictionary<EMonster, ERace> _raceOf = new Dictionary<EMonster, ERace>();
        private readonly Dictionary<int, List<GradeCost>> _promotion = new Dictionary<int, List<GradeCost>>();
        private readonly ERace[] _slotRace = new ERace[SlotCount];
        private readonly Dictionary<int, int> _hintKeyByChapter = new Dictionary<int, int>();

        private FormationDatabase(FormationConfigRow config)
        {
            Config = config;
        }

        public FormationConfigRow Config { get; }

        //# 슬롯 index(0~5) 의 종족.
        public ERace SlotRace(int index) => _slotRace[index];

        //# 종족의 둥지 인덱스(0~5), 없으면 -1.
        public int SlotOf(ERace race) => Array.IndexOf(_slotRace, race);

        public bool HasMonster(EMonster monster) => _raceOf.ContainsKey(monster);

        public bool TryGetRace(EMonster monster, out ERace race) => _raceOf.TryGetValue(monster, out race);

        public RaceRow RaceRowOf(ERace race) => _races.TryGetValue(race, out RaceRow row) ? row : null;

        public GradeRow GradeRowOf(int grade) => _grades.TryGetValue(grade, out GradeRow row) ? row : null;

        //# 종족 소속 몬스터 — 등급 오름차순(1등급 항상 첫 원소).
        public IReadOnlyList<EMonster> MonstersOf(ERace race)
        {
            return _members.TryGetValue(race, out List<EMonster> list) ? list : (IReadOnlyList<EMonster>)Array.Empty<EMonster>();
        }

        //# 데이터에 있는 그 종족의 최고 등급.
        public int MaxGradeOf(ERace race)
        {
            IReadOnlyList<EMonster> list = MonstersOf(race);
            return list.Count > 0 ? MonsterGrades.GradeOf(list[list.Count - 1]) : MonsterGrades.MinGrade;
        }

        //# 보유 최고 등급 이하 몬스터 목록(등급순) — 편성 팝업 우측 목록.
        public List<EMonster> OwnedMonstersOf(ERace race, int ownedGrade)
        {
            List<EMonster> owned = new List<EMonster>();
            foreach (EMonster monster in MonstersOf(race))
            {
                if (MonsterGrades.GradeOf(monster) <= Math.Max(MonsterGrades.MinGrade, ownedGrade))
                {
                    owned.Add(monster);
                }
            }
            return owned;
        }

        //# 종족의 보유 최고 등급 몬스터 — 기본 편성·무효 슬롯 대체값.
        public EMonster HighestOwned(ERace race, int ownedGrade)
        {
            List<EMonster> owned = OwnedMonstersOf(race, ownedGrade);
            return owned[owned.Count - 1];
        }

        //# 기본 편성(슬롯 1~6 순) = 슬롯 종족마다 보유 최고 등급 몬스터 (dungeon-formation §0.3).
        public EMonster[] DefaultFormation(Func<ERace, int> ownedGradeOf)
        {
            EMonster[] formation = new EMonster[SlotCount];
            for (int i = 0; i < SlotCount; ++i)
            {
                formation[i] = HighestOwned(_slotRace[i], ownedGradeOf != null ? ownedGradeOf(_slotRace[i]) : MonsterGrades.MinGrade);
            }
            return formation;
        }

        //# 등급 HP·공격력 배율 — 행이 없으면 1.
        public float HpMulOf(EMonster monster)
        {
            GradeRow row = GradeRowOf(MonsterGrades.GradeOf(monster));
            return row != null ? row.hpMul : 1f;
        }

        public float AtkMulOf(EMonster monster)
        {
            GradeRow row = GradeRowOf(MonsterGrades.GradeOf(monster));
            return row != null ? row.atkMul : 1f;
        }

        //# 이 등급으로 올리는 비용(재화별) — 행이 없으면 빈 목록.
        public IReadOnlyList<GradeCost> PromotionCostOf(int grade)
        {
            return _promotion.TryGetValue(grade, out List<GradeCost> list) ? list : (IReadOnlyList<GradeCost>)Array.Empty<GradeCost>();
        }

        //# 챕터 영웅 성향 문구 id — 없으면 0.
        public int HintKeyOf(int chapterId)
        {
            return _hintKeyByChapter.TryGetValue(chapterId, out int key) ? key : 0;
        }

        //# chapterIds: Chapter.json 의 모든 chapterId(null 이면 힌트 챕터 덮임 검사 생략). stringExists: Strings_Ko id 존재 판정(null 이면 생략).
        //# promotions 가 null 이면 승급 비용 없음(승급 불가) — 테스트·부분 데이터용.
        public static FormationDatabase Build(
            FormationConfigRow config, RaceRow[] races, GradeRow[] grades, Race_MonsterRow[] raceMonsters,
            Grade_PromotionRow[] promotions, FormationSlotRow[] slots, Chapter_FormationHintRow[] hints,
            IEnumerable<int> chapterIds = null, Func<int, bool> stringExists = null)
        {
            if (config == null)
                throw new ChapterDataException("FormationConfig.json 이 비어 있음");

            List<string> errors = new List<string>();
            FormationDatabase db = new FormationDatabase(config);
            if (config.unlockClearedStage < 0)
            {
                errors.Add("FormationConfig unlockClearedStage 는 0 이상이어야 함");
            }
            db.LoadRaces(races, stringExists, errors);
            db.LoadGrades(grades, stringExists, errors);
            db.LoadRaceMonsters(raceMonsters, errors);
            db.LoadPromotions(promotions, errors);
            db.LoadSlots(slots, errors);
            db.LoadHints(hints, chapterIds, stringExists, errors);
            if (errors.Count > 0)
                throw new ChapterDataException(string.Join("\n", errors));
            return db;
        }

        private void LoadRaces(RaceRow[] rows, Func<int, bool> stringExists, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Race.json 이 비어 있음");
                return;
            }
            foreach (RaceRow row in rows)
            {
                if (row == null || TryParseEnum(row.race, out ERace race) == false)
                {
                    errors.Add($"Race.json race '{row?.race}' 가 ERace 값명이 아님");
                    continue;
                }
                if (_races.ContainsKey(race))
                {
                    errors.Add($"Race.json race {race} 중복");
                    continue;
                }
                if (stringExists != null && (stringExists(row.nameKey) == false || stringExists(row.descKey) == false))
                {
                    errors.Add($"Race.json {race} nameKey/descKey 가 Strings_Ko 에 없음");
                }
                _races[race] = row;
            }
            foreach (ERace race in (ERace[])Enum.GetValues(typeof(ERace)))
            {
                if (_races.ContainsKey(race) == false)
                {
                    errors.Add($"Race.json 에 {race} 행이 없음");
                }
            }
        }

        private void LoadGrades(GradeRow[] rows, Func<int, bool> stringExists, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Grade.json 이 비어 있음");
                return;
            }
            foreach (GradeRow row in rows)
            {
                if (row == null || row.grade < MonsterGrades.MinGrade || row.grade > MonsterGrades.MaxGrade)
                {
                    errors.Add($"Grade.json grade 는 {MonsterGrades.MinGrade}~{MonsterGrades.MaxGrade} 이어야 함");
                    continue;
                }
                if (_grades.ContainsKey(row.grade))
                {
                    errors.Add($"Grade.json grade {row.grade} 중복");
                    continue;
                }
                if (row.sizeMul > 0f == false)
                {
                    errors.Add($"Grade.json grade {row.grade} sizeMul 은 0 보다 커야 함");
                }
                if (row.hpMul > 0f == false || row.atkMul > 0f == false)
                {
                    errors.Add($"Grade.json grade {row.grade} hpMul·atkMul 은 0 보다 커야 함");
                }
                if (stringExists != null && stringExists(row.prefixKey) == false)
                {
                    errors.Add($"Grade.json grade {row.grade} prefixKey {row.prefixKey} 가 Strings_Ko 에 없음");
                }
                _grades[row.grade] = row;
            }
            for (int g = MonsterGrades.MinGrade; g <= MonsterGrades.MaxGrade; ++g)
            {
                if (_grades.ContainsKey(g) == false)
                {
                    errors.Add($"Grade.json 에 grade {g} 행이 없음");
                }
            }
        }

        //# 한 몬스터 = 한 종족·한 등급. 값명이 MonsterGrades 해석(<기준>G<등급>)과 어긋나면 오류 — 런타임 해석은 enum 순서가 담당한다.
        private void LoadRaceMonsters(Race_MonsterRow[] rows, List<string> errors)
        {
            if (rows == null)
            {
                errors.Add("Race_Monster.json 이 비어 있음");
                return;
            }
            HashSet<string> keys = new HashSet<string>();
            Dictionary<ERace, EMonster> baseOfRace = new Dictionary<ERace, EMonster>();
            foreach (Race_MonsterRow row in rows)
            {
                if (row == null)
                    continue;
                bool raceOk = TryParseEnum(row.race, out ERace race) && _races.ContainsKey(race);
                bool monsterOk = TryParseMonster(row.monster, out EMonster monster);
                if (raceOk == false)
                {
                    errors.Add($"Race_Monster.json race '{row.race}' 가 Race 에 없음");
                }
                if (monsterOk == false)
                {
                    errors.Add($"Race_Monster.json monster '{row.monster}' 가 EMonster 값명이 아님");
                }
                if (_grades.ContainsKey(row.grade) == false)
                {
                    errors.Add($"Race_Monster.json ({row.race}, {row.grade}) grade 가 Grade 에 없음");
                }
                if (keys.Add(row.race + "|" + row.grade) == false)
                {
                    errors.Add($"Race_Monster.json ({row.race}, {row.grade}) 중복");
                    continue;
                }
                if (raceOk == false || monsterOk == false)
                    continue;
                if (_raceOf.ContainsKey(monster))
                {
                    errors.Add($"Race_Monster.json monster {monster} 가 두 번 이상 나옴");
                    continue;
                }
                if (MonsterGrades.GradeOf(monster) != row.grade)
                {
                    errors.Add($"Race_Monster.json {monster} 의 등급 {row.grade} 가 값명 규칙(<기준>G<등급>)과 다름");
                    continue;
                }
                if (baseOfRace.TryGetValue(race, out EMonster existing) && MonsterGrades.SameSpecies(existing, monster) == false)
                {
                    errors.Add($"Race_Monster.json {race} 에 기준 몬스터가 다른 {monster} 가 섞임");
                    continue;
                }
                baseOfRace[race] = MonsterGrades.BaseOf(monster);
                _raceOf[monster] = race;
                if (_members.TryGetValue(race, out List<EMonster> list) == false)
                {
                    list = new List<EMonster>();
                    _members[race] = list;
                }
                list.Add(monster);
            }
            foreach (KeyValuePair<ERace, List<EMonster>> pair in _members)
            {
                pair.Value.Sort((a, b) => MonsterGrades.GradeOf(a).CompareTo(MonsterGrades.GradeOf(b)));
            }
            foreach (ERace race in _races.Keys)
            {
                if (_members.TryGetValue(race, out List<EMonster> list) == false || MonsterGrades.GradeOf(list[0]) != MonsterGrades.MinGrade)
                {
                    errors.Add($"Race_Monster.json {race} 에 1등급 몬스터가 없음");
                }
            }
        }

        private void LoadPromotions(Grade_PromotionRow[] rows, List<string> errors)
        {
            if (rows == null)
                return;
            HashSet<string> keys = new HashSet<string>();
            foreach (Grade_PromotionRow row in rows)
            {
                if (row == null)
                    continue;
                if (row.grade <= MonsterGrades.MinGrade || row.grade > MonsterGrades.MaxGrade)
                {
                    errors.Add($"Grade_Promotion.json grade {row.grade} 는 {MonsterGrades.MinGrade + 1}~{MonsterGrades.MaxGrade} 이어야 함");
                    continue;
                }
                if (TryParseEnum(row.currency, out ECurrency currency) == false)
                {
                    errors.Add($"Grade_Promotion.json grade {row.grade} currency '{row.currency}' 가 ECurrency 값명이 아님");
                    continue;
                }
                if (keys.Add(row.grade + "|" + row.currency) == false)
                {
                    errors.Add($"Grade_Promotion.json ({row.grade}, {row.currency}) 중복");
                    continue;
                }
                if (row.amount < 1)
                {
                    errors.Add($"Grade_Promotion.json ({row.grade}, {row.currency}) amount 는 1 이상이어야 함");
                    continue;
                }
                if (_promotion.TryGetValue(row.grade, out List<GradeCost> list) == false)
                {
                    list = new List<GradeCost>();
                    _promotion[row.grade] = list;
                }
                list.Add(new GradeCost { Currency = currency, Amount = row.amount });
            }
        }

        //# 6슬롯이 6종족을 정확히 한 번씩 (슬롯 수 = 종족 수 불변식).
        private void LoadSlots(FormationSlotRow[] rows, List<string> errors)
        {
            if (rows == null || rows.Length != SlotCount)
            {
                errors.Add($"FormationSlot.json 행이 정확히 {SlotCount}개여야 함(현재 {(rows == null ? 0 : rows.Length)})");
                return;
            }
            bool[] seen = new bool[SlotCount];
            HashSet<ERace> used = new HashSet<ERace>();
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
                if (TryParseEnum(row.race, out ERace race) == false || _races.ContainsKey(race) == false)
                {
                    errors.Add($"FormationSlot.json slot {row.slot} race '{row.race}' 가 Race 에 없음");
                    continue;
                }
                if (used.Add(race) == false)
                {
                    errors.Add($"FormationSlot.json race {race} 가 두 슬롯 이상에 있음");
                    continue;
                }
                _slotRace[row.slot - 1] = race;
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
        public static bool TryParseMonster(string name, out EMonster monster) => TryParseEnum(name, out monster);

        public static bool TryParseEnum<T>(string name, out T value) where T : struct, Enum
        {
            value = default;
            if (string.IsNullOrEmpty(name) || Array.IndexOf(Enum.GetNames(typeof(T)), name) < 0)
                return false;
            value = (T)Enum.Parse(typeof(T), name);
            return true;
        }
    }

    //# Card_Species.json 검증 + 조회 (dungeon-formation §0.5-6, §5.2). FormationDatabase 와 독립 — 이것만 실패하면 카드 제외만 꺼진다(E-2).
    public class FormationCardSpecies
    {
        //# null 이면 카드 후보 제외 비활성.
        public static FormationCardSpecies Current { get; set; }

        private readonly Dictionary<ECardId, List<EMonster>> _species = new Dictionary<ECardId, List<EMonster>>();

        private FormationCardSpecies() { }

        //# formation 이 있으면 monster 가 Race_Monster 에 있어야 한다(FK).
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
                bool cardOk = FormationDatabase.TryParseEnum(row.cardId, out ECardId card);
                if (cardOk == false)
                {
                    errors.Add($"Card_Species.json cardId '{row.cardId}' 가 ECardId 값명이 아님");
                }
                bool monsterOk = FormationDatabase.TryParseMonster(row.monster, out EMonster monster)
                    && (formation == null || formation.HasMonster(monster));
                if (monsterOk == false)
                {
                    errors.Add($"Card_Species.json monster '{row.monster}' 가 Race_Monster 에 없음");
                }
                if (seen.Add(row.cardId + "|" + row.monster) == false)
                {
                    errors.Add($"Card_Species.json ({row.cardId}, {row.monster}) 중복");
                }
                if (cardOk && monsterOk)
                {
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
        //# isOutputting 은 종족 단위로 판정해야 한다(등급 몬스터가 나가도 기준 몬스터 카드가 유효, monster-grades §6).
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
