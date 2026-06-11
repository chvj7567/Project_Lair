using System;
using System.Collections.Generic;

namespace Lair.Meta
{
    //# 메타 진행 세이브 모델 — JsonUtility 직렬화 (Dictionary 불가 → 엔트리 리스트).
    //# 스키마 변경 시 Version 증가 + Store 마이그레이션 분기 (spec §5.7).
    [Serializable]
    public class MetaProfile
    {
        public int Version = 1;
        public int Souls;
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
