using System;
using System.Collections.Generic;
using Lair.Battle;

namespace Lair.Meta
{
    //# 메타 진행 세이브 모델 — JsonUtility 직렬화 (Dictionary 불가 → 엔트리 리스트).
    //# 스키마 변경 시 Version 증가 + Store 마이그레이션 분기 (spec §5.7).
    [Serializable]
    public class MetaProfile
    {
        public int Version = 10;
        public int Souls;
        //# 스테이지 진행(chapter-system §2.1) — 선형 스테이지 번호 = (챕터-1) x 5 + 인덱스.
        //# SelectedStage=마지막 선택(1~MaxStage, 기본 1), ClearedStage=클리어한 최고 스테이지(0~MaxStage, 0=미클리어). 구버전 세이브는 필드 부재 → C# 초기값 자동 적용.
        public int SelectedStage = 1;
        public int ClearedStage = 0;
        public int LordXp;                                       //# 누적 XP — 레벨은 LordLevelService 가 계산
        //# 영주 보상 자동 수령의 멱등 가드 — 지급 완료된 최고 레벨 (기획서 §4.4, 초기값 1).
        public int LordRewardGrantedLevel = 1;
        public List<ShopLevelEntry> ShopLevels = new List<ShopLevelEntry>();
        //# 스테이지별 전적(spec §4) — JsonUtility 가 Dictionary 를 못 다뤄 ShopLevels 와 같은 엔트리-리스트.
        //# 구버전 세이브엔 이 키가 없어 빈 리스트로 로드된다 = 기존 유저는 전 스테이지 0부터.
        public List<StageRecordEntry> StageRecords = new List<StageRecordEntry>();
        public List<string> AchievedIds = new List<string>();    //# 달성한 도전과제 Id
        public List<string> SeenMonsters = new List<string>();   //# 도감 — EMonster.ToString()
        public List<string> PickedCards = new List<string>();    //# 도감 — ECardId.ToString() (distinct)
        //# 카드별 누적 픽 횟수(v4) — 픽한 카드만 행이 존재(sentinel 없음). 기록 팝업 "가장 많이 픽한 카드"용.
        //# 구버전 세이브엔 키가 없어 빈 리스트로 로드 → 이후 판부터 집계.
        public List<CardPickCountEntry> CardPickCounts = new List<CardPickCountEntry>();
        public int TotalRuns;
        public int TotalWins;
        public float BestClearTime = -1f;                        //# 승리 최단 시간(초). 없으면 -1
        public string SelectedHero = "Knight";                   //# EHero.ToString()
        //# 난이도(v7, difficulty-select §2) — 0=Easy. 구버전 세이브엔 키가 없어 Easy/빈 리스트로 로드.
        public int SelectedDifficulty;
        //# 스테이지별 클리어한 최고 난이도 — Normal 이상을 클리어한 스테이지만 행 존재(MaxDifficulty >= 1 불변식).
        public List<DifficultyClearEntry> DifficultyClears = new List<DifficultyClearEntry>();
        //# 유물(v8, relics §3.2) — 보유한 유물만 행 존재(Stack >= 1 불변식). 구버전 세이브엔 키가 없어 빈 리스트로 로드.
        public List<RelicEntry> Relics = new List<RelicEntry>();
        //# 승리 굴림 연속 미획득 수(소프트 pity). 0 = 연패 0 이라는 실제 값.
        public int RelicMissStreak;
        //# 던전 편성(v9, dungeon-formation §3.2) — 0행 = 기본 편성(바꾼 적 없음), 6행 = 사용자 편성. 구버전 세이브엔 키가 없어 빈 리스트로 로드.
        public List<FormationSlotEntry> Formation = new List<FormationSlotEntry>();
        //# 무한의 탑(v10, infinite-tower §4.2) — TowerFloorClears 가 기록된 시즌. 리스트가 비어 있으면 값의 의미가 없다(진행 없음 = 빈 리스트로 판정).
        public int TowerSeason;
        //# 이번 시즌에 클리어한 층만 행 존재(Floor >= 1, ClearTime > 0 불변식). 구버전 세이브엔 키가 없어 빈 리스트로 로드.
        public List<TowerFloorClearEntry> TowerFloorClears = new List<TowerFloorClearEntry>();
        //# 전투 2배속 토글 마지막 상태 — 로컬 선호값이라 CopyFrom(클라우드 복원)이 복사하지 않는다(DisplayName 선례).
        public bool BattleSpeedOn;
        //# 사용자 지정 표시명. 빈 값이면 기본명("영주 #"+deviceId 앞4자 대문자) 사용. 로컬 전용 — 클라우드 동기 대상 아님(기획서 §1).
        public string DisplayName;

        //# 리스트 탐색 — 없으면 0 (미구매).
        public int GetShopLevel(string itemId)
        {
            foreach (ShopLevelEntry entry in ShopLevels)
            {
                if (entry != null && entry.ItemId == itemId)
                    return entry.Level;
            }
            return 0;
        }

        //# 있으면 갱신, 없으면 추가.
        public void SetShopLevel(string itemId, int level)
        {
            foreach (ShopLevelEntry entry in ShopLevels)
            {
                if (entry != null && entry.ItemId == itemId)
                {
                    entry.Level = level;
                    return;
                }
            }
            ShopLevels.Add(new ShopLevelEntry { ItemId = itemId, Level = level });
        }

        //# 스테이지 전적 조회 — 없으면 기본값(0/0/-1) 엔트리를 반환하되 리스트에 넣지 않는다(조회는 부수효과 0).
        public StageRecordEntry GetStageRecord(int stage)
        {
            foreach (StageRecordEntry entry in StageRecords)
            {
                if (entry != null && entry.Stage == stage)
                    return entry;
            }
            return new StageRecordEntry { Stage = stage };
        }

        //# 한 판 결과를 스테이지 전적에 반영 — 엔트리 없으면 생성. 최단은 승리 시에만, 더 빠를 때만 갱신.
        //# recordBest=false(Normal/Hard 판)면 Runs/Wins 만 올리고 최단 기록은 건드리지 않는다(difficulty-select §1.6).
        public void RecordStageRun(int stage, bool win, float clearTime, bool recordBest = true)
        {
            StageRecordEntry entry = null;
            foreach (StageRecordEntry e in StageRecords)
            {
                if (e != null && e.Stage == stage)
                {
                    entry = e;
                    break;
                }
            }
            if (entry == null)
            {
                entry = new StageRecordEntry { Stage = stage };
                StageRecords.Add(entry);
            }

            entry.Runs++;
            if (win == false)
                return;

            entry.Wins++;
            if (recordBest && (entry.BestClearTime < 0f || clearTime < entry.BestClearTime))
            {
                entry.BestClearTime = clearTime;
            }
        }

        //# 클리어한 최고 난이도 — 행 없으면 0(조회는 부수효과 0). 0 은 "Easy 까지" 의미이며 저장되는 값이 아니다.
        public int GetMaxClearedDifficulty(int stage)
        {
            if (DifficultyClears == null)
                return 0;
            foreach (DifficultyClearEntry entry in DifficultyClears)
            {
                if (entry != null && entry.Stage == stage)
                    return entry.MaxDifficulty;
            }
            return 0;
        }

        //# 스테이지 stage 의 선택 가능 최대 난이도 — 규칙은 StageProgress 단일 소유, 여기선 프로필 값만 공급.
        public int MaxSelectableDifficulty(int stage, int maxDifficulty)
        {
            return StageProgress.MaxSelectableDifficulty(stage, ClearedStage, GetMaxClearedDifficulty(stage), maxDifficulty);
        }

        //# 읽기 전용 보정 — 프로필을 수정하지 않는다.
        public int ClampDifficulty(int difficulty, int stage, int maxDifficulty)
        {
            return StageProgress.ClampDifficulty(difficulty, stage, ClearedStage, GetMaxClearedDifficulty(stage), maxDifficulty);
        }

        //# 승리 시 최고 난이도 기록(§1.5) — difficulty >= 1 이고 기존보다 높을 때만 갱신/추가.
        public void RecordDifficultyClear(int stage, int difficulty)
        {
            if (difficulty < 1)
                return;
            if (DifficultyClears == null)
            {
                DifficultyClears = new List<DifficultyClearEntry>();
            }
            foreach (DifficultyClearEntry entry in DifficultyClears)
            {
                if (entry != null && entry.Stage == stage)
                {
                    entry.MaxDifficulty = Math.Max(entry.MaxDifficulty, difficulty);
                    return;
                }
            }
            DifficultyClears.Add(new DifficultyClearEntry { Stage = stage, MaxDifficulty = difficulty });
        }

        //# 보유 유물 중첩 — 없으면 0(조회는 부수효과 0). 상한 클램프는 읽는 쪽(RelicBattleBonus 등)이 담당(relics §3.6).
        public int GetRelicStack(string relicId)
        {
            if (Relics == null)
                return 0;
            foreach (RelicEntry entry in Relics)
            {
                if (entry != null && entry.RelicId == relicId)
                    return entry.Stack;
            }
            return 0;
        }

        //# 유물 중첩 +1 — 없으면 Stack 1 로 추가. 상한 판정은 호출부(RelicDropRoller)가 한다. 반환 = 추가 후 중첩.
        public int AddRelic(string relicId)
        {
            if (string.IsNullOrEmpty(relicId))
                return 0;
            if (Relics == null)
            {
                Relics = new List<RelicEntry>();
            }
            foreach (RelicEntry entry in Relics)
            {
                if (entry != null && entry.RelicId == relicId)
                {
                    entry.Stack++;
                    return entry.Stack;
                }
            }
            Relics.Add(new RelicEntry { RelicId = relicId, Stack = 1 });
            return 1;
        }

        //# 탑 최고 클리어 층 — 빈 리스트면 0. 도전 층 = 최고 층 + 1 (층 구멍은 보정하지 않는다, infinite-tower E-7). 조회는 부수효과 0.
        public int GetTowerHighestFloor()
        {
            int highest = 0;
            if (TowerFloorClears == null)
                return highest;
            foreach (TowerFloorClearEntry entry in TowerFloorClears)
            {
                if (entry != null && entry.Floor > highest)
                {
                    highest = entry.Floor;
                }
            }
            return highest;
        }

        //# 층 클리어 시간 — 행이 없으면 0 (불변식 ClearTime > 0 이라 0 은 "기록 없음"). 조회는 부수효과 0.
        public float GetTowerFloorClearTime(int floor)
        {
            if (TowerFloorClears == null)
                return 0f;
            foreach (TowerFloorClearEntry entry in TowerFloorClears)
            {
                if (entry != null && entry.Floor == floor)
                    return entry.ClearTime;
            }
            return 0f;
        }

        //# 탑 승리 기록 — 행이 없으면 추가, 있으면 더 짧을 때만 갱신. 시즌을 함께 기록한다. 입력이 불변식(Floor >= 1, ClearTime > 0)을 어기면 무시.
        public void RecordTowerClear(int floor, float clearTime, int seasonId)
        {
            if (floor < 1 || clearTime > 0f == false)
                return;
            if (TowerFloorClears == null)
            {
                TowerFloorClears = new List<TowerFloorClearEntry>();
            }
            TowerSeason = seasonId;
            foreach (TowerFloorClearEntry entry in TowerFloorClears)
            {
                if (entry != null && entry.Floor == floor)
                {
                    entry.ClearTime = Math.Min(entry.ClearTime, clearTime);
                    return;
                }
            }
            TowerFloorClears.Add(new TowerFloorClearEntry { Floor = floor, ClearTime = clearTime });
        }

        //# 시즌 정리 (infinite-tower §4.6) — 기록이 있고 시즌이 다르면 리스트를 비우고 새 시즌으로. 반환 = 지웠는가. TowerConfig 로드 성공 시에만 호출한다.
        public bool NormalizeTowerSeason(int seasonId)
        {
            if (TowerFloorClears == null || TowerFloorClears.Count == 0 || TowerSeason == seasonId)
                return false;
            TowerFloorClears = new List<TowerFloorClearEntry>();
            TowerSeason = seasonId;
            return true;
        }

        //# 카드 픽 횟수 조회 — 픽한 적 없으면 0(조회는 부수효과 0).
        public int GetCardPickCount(string cardId)
        {
            foreach (CardPickCountEntry entry in CardPickCounts)
            {
                if (entry != null && entry.CardId == cardId)
                    return entry.Count;
            }
            return 0;
        }

        //# 픽 횟수 누적 — 엔트리 없으면 생성. count 는 1 이상만 반영.
        public void AddCardPick(string cardId, int count = 1)
        {
            if (string.IsNullOrEmpty(cardId) || count < 1)
                return;
            foreach (CardPickCountEntry entry in CardPickCounts)
            {
                if (entry != null && entry.CardId == cardId)
                {
                    entry.Count += count;
                    return;
                }
            }
            CardPickCounts.Add(new CardPickCountEntry { CardId = cardId, Count = count });
        }

        //# 가장 많이 픽한 카드 — 동률이면 카드 ID 사전순(ordinal) 첫 번째로 고정. 기록 없으면 null.
        public CardPickCountEntry GetMostPickedCard()
        {
            CardPickCountEntry best = null;
            foreach (CardPickCountEntry entry in CardPickCounts)
            {
                if (entry == null || entry.Count < 1 || string.IsNullOrEmpty(entry.CardId))
                    continue;
                if (best == null
                    || entry.Count > best.Count
                    || (entry.Count == best.Count && string.CompareOrdinal(entry.CardId, best.CardId) < 0))
                {
                    best = entry;
                }
            }
            return best;
        }

        //# 이번 판이 스테이지 최단 신기록인가 — 승리이면서 이전 최단이 없거나 더 빠를 때. RecordStageRun 호출 "전에" 판정해야 한다.
        public bool IsNewStageBest(int stage, bool win, float clearTime)
        {
            if (win == false)
                return false;
            float previous = GetStageRecord(stage).BestClearTime;
            return previous < 0f || clearTime < previous;
        }

        //# 클라우드 복원 — 서버 프로필 값을 이 인스턴스에 in-place 복사(참조 유지 → VM/View 가 보던 객체 그대로).
        //# DisplayName 은 로컬 전용이라 복원으로 덮지 않는다(기획서 §1 — 새 기기는 기본명 시작).
        public void CopyFrom(MetaProfile other)
        {
            if (other == null)
                return;
            Version = other.Version;
            Souls = other.Souls;
            //# 진행 데이터 — 클라우드 복원 대상. SelectedStage 는 로컬 preference 이나 일관성 위해 함께 복사(plan Task 1).
            SelectedStage = other.SelectedStage;
            ClearedStage = other.ClearedStage;
            LordXp = other.LordXp;
            LordRewardGrantedLevel = other.LordRewardGrantedLevel;
            ShopLevels = other.ShopLevels ?? new List<ShopLevelEntry>();
            //# 스테이지 전적도 클라우드 복원 대상 — 빠뜨리면 복원 시 이 기록만 유실된다(spec §8).
            StageRecords = other.StageRecords ?? new List<StageRecordEntry>();
            AchievedIds = other.AchievedIds ?? new List<string>();
            SeenMonsters = other.SeenMonsters ?? new List<string>();
            PickedCards = other.PickedCards ?? new List<string>();
            //# 카드 픽 횟수도 복원 대상 — 빠뜨리면 복원 시 "가장 많이 픽한 카드" 만 유실된다.
            CardPickCounts = other.CardPickCounts ?? new List<CardPickCountEntry>();
            TotalRuns = other.TotalRuns;
            TotalWins = other.TotalWins;
            BestClearTime = other.BestClearTime;
            SelectedHero = other.SelectedHero;
            //# 난이도 진행도 복원 대상 — 빠뜨리면 복원 시 난이도 진행만 유실된다.
            SelectedDifficulty = other.SelectedDifficulty;
            DifficultyClears = other.DifficultyClears ?? new List<DifficultyClearEntry>();
            //# 유물은 수집 노력이 큰 자산 — 빠뜨리면 복원 시 유물 전부 유실. BattleSpeedOn 은 로컬 선호값이라 복사하지 않는다.
            Relics = other.Relics ?? new List<RelicEntry>();
            RelicMissStreak = other.RelicMissStreak;
            //# 편성은 플레이어의 계획 — 빠뜨리면 복원 시 편성만 유실된다.
            Formation = CopyFormation(other.Formation);
            //# 탑 진행은 시즌 기록 — 빠뜨리면 복원 시 탑 진행만 유실된다. 행은 새 인스턴스로(원본 공유 방지).
            TowerSeason = other.TowerSeason;
            TowerFloorClears = CopyTowerClears(other.TowerFloorClears);
        }

        //# 탑 클리어 행 복사 — null 이면 빈 리스트, 행은 새 인스턴스로.
        private static List<TowerFloorClearEntry> CopyTowerClears(List<TowerFloorClearEntry> source)
        {
            List<TowerFloorClearEntry> copy = new List<TowerFloorClearEntry>();
            if (source == null)
                return copy;
            foreach (TowerFloorClearEntry entry in source)
            {
                copy.Add(entry == null ? null : new TowerFloorClearEntry { Floor = entry.Floor, ClearTime = entry.ClearTime });
            }
            return copy;
        }

        //# 편성 행 복사 — null 이면 빈 리스트(기본 편성), 행은 새 인스턴스로(원본 공유 방지).
        private static List<FormationSlotEntry> CopyFormation(List<FormationSlotEntry> source)
        {
            List<FormationSlotEntry> copy = new List<FormationSlotEntry>();
            if (source == null)
                return copy;
            foreach (FormationSlotEntry entry in source)
            {
                copy.Add(entry == null ? null : new FormationSlotEntry { Slot = entry.Slot, Monster = entry.Monster });
            }
            return copy;
        }

        //# 랭킹 제출 표시명 결정(기획서 §1) — DisplayName 우선, 빈 값이면 기본명("영주 #"+deviceId 앞4자 대문자).
        //# static 헬퍼라 PlayerPrefs 의존 없이 단위 테스트 가능(deviceId 는 호출부 주입).
        public static string ResolveDisplayName(string displayName, string deviceId)
        {
            if (string.IsNullOrEmpty(displayName) == false)
                return displayName;
            string prefix = string.IsNullOrEmpty(deviceId) ? string.Empty
                : deviceId.Substring(0, Math.Min(4, deviceId.Length)).ToUpperInvariant();
            return "영주 #" + prefix;
        }

        //# 표시명 입력 검증/절삭(기획서 §1) — null/빈값(trim 후)=거부(null 반환). 그 외 trim + 12자 절삭한 확정 표시명 반환. 부수효과 0(순수).
        //# ResolveDisplayName 과 구분 — 저쪽은 랭킹 제출용 기본명 폴백, 이쪽은 사용자 입력 검증.
        public static string NormalizeDisplayName(string raw)
        {
            string trimmed = raw != null ? raw.Trim() : string.Empty;
            if (string.IsNullOrEmpty(trimmed))
                return null;
            if (trimmed.Length > 12)
            {
                trimmed = trimmed.Substring(0, 12);
            }
            return trimmed;
        }

        //# 도감 누적용 — 중복 없이 추가.
        public void AddDistinct(List<string> list, string value)
        {
            if (list == null || string.IsNullOrEmpty(value))
                return;
            if (list.Contains(value))
                return;
            list.Add(value);
        }
    }

    [Serializable]
    public class ShopLevelEntry
    {
        public string ItemId;
        public int Level;
    }

    //# 카드 하나의 누적 픽 횟수 — MetaProfile 소속 선택적 관계 데이터(픽한 카드만 존재). JsonUtility 직렬화 대상.
    [Serializable]
    public class CardPickCountEntry
    {
        public string CardId;    //# ECardId.ToString() — PickedCards 와 같은 키
        public int Count;        //# 누적 픽 횟수 (>= 1)
    }

    //# 스테이지별 클리어한 최고 난이도 — Normal 이상 클리어한 스테이지만 행 존재. JsonUtility 직렬화 대상.
    [Serializable]
    public class DifficultyClearEntry
    {
        public int Stage;            //# 선형 스테이지 번호 1~MaxStage
        public int MaxDifficulty;    //# 클리어한 최고 난이도 인덱스 (>= 1)
    }

    //# 보유 유물 하나 — 보유한 유물만 행 존재(Stack >= 1). JsonUtility 직렬화 대상.
    [Serializable]
    public class RelicEntry
    {
        public string RelicId;   //# Relic.relicId (ERelic 값명)
        public int Stack;        //# 현재 중첩 (>= 1)
    }

    //# 편성 한 둥지의 종 — Formation 은 0행 또는 정확히 6행(Slot 1~6 한 번씩). JsonUtility 직렬화 대상.
    [Serializable]
    public class FormationSlotEntry
    {
        public int Slot;         //# FormationSlot.slot 1~6
        public string Monster;   //# EMonster 값명
    }

    //# 탑 클리어한 층 하나 — 클리어한 층만 행 존재. JsonUtility 직렬화 대상.
    [Serializable]
    public class TowerFloorClearEntry
    {
        public int Floor;        //# 1 이상, PK
        public float ClearTime;  //# 그 층 클리어 시간(게임초). 불변식 > 0
    }

    //# 스테이지 한 칸의 누적 전적. JsonUtility 직렬화 대상.
    [Serializable]
    public class StageRecordEntry
    {
        public int Stage;                    //# 선형 스테이지 번호 1~MaxStage
        public int Runs;
        public int Wins;
        public float BestClearTime = -1f;    //# 승리 기록 없으면 -1 (MetaProfile.BestClearTime 과 같은 규약)
    }
}
