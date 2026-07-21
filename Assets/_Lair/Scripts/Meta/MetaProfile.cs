using System;
using System.Collections.Generic;

namespace Lair.Meta
{
    //# 메타 진행 세이브 모델 — JsonUtility 직렬화 (Dictionary 불가 → 엔트리 리스트).
    //# 스키마 변경 시 Version 증가 + Store 마이그레이션 분기 (spec §5.7).
    [Serializable]
    public class MetaProfile
    {
        public int Version = 2;
        public int Souls;
        //# 스테이지 진행(hero-stage-variant 기획서 §3) — SelectedStage=마지막 선택(1~5, 기본 1),
        //# ClearedStage=클리어한 최고 스테이지(0~5, 0=미클리어). 구버전 세이브는 필드 부재 → C# 초기값 자동 적용.
        public int SelectedStage = 1;
        public int ClearedStage = 0;
        public int LordXp;                                       //# 누적 XP — 레벨은 LordLevelService 가 계산
        //# 영주 보상 자동 수령의 멱등 가드 — 지급 완료된 최고 레벨 (기획서 §4.4, 초기값 1).
        public int LordRewardGrantedLevel = 1;
        public List<ShopLevelEntry> ShopLevels = new List<ShopLevelEntry>();
        public List<string> AchievedIds = new List<string>();    //# 달성한 도전과제 Id
        public List<string> SeenMonsters = new List<string>();   //# 도감 — EMonster.ToString()
        public List<string> PickedCards = new List<string>();    //# 도감 — ECardId.ToString() (distinct)
        public int TotalRuns;
        public int TotalWins;
        public float BestClearTime = -1f;                        //# 승리 최단 시간(초). 없으면 -1
        public string SelectedHero = "Knight";                   //# EHero.ToString()
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
            AchievedIds = other.AchievedIds ?? new List<string>();
            SeenMonsters = other.SeenMonsters ?? new List<string>();
            PickedCards = other.PickedCards ?? new List<string>();
            TotalRuns = other.TotalRuns;
            TotalWins = other.TotalWins;
            BestClearTime = other.BestClearTime;
            SelectedHero = other.SelectedHero;
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
}
