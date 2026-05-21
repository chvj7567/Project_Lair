using System;
using System.Collections.Generic;

namespace Lair.Battle
{
    //# 한 판의 결과 스냅샷. JsonUtility 직렬화 — enum 은 문자열로 저장.
    [Serializable]
    public class RunRecord
    {
        public string FinishedAt;          //# ISO 8601 시각 문자열
        public string Result;              //# "Win" / "Lose"
        public float  DeathTime;           //# 영웅 사망(또는 타임오버) 경과초
        public List<string> Picks;         //# 픽한 ECardId 문자열 목록 (선택 순서)
        public int    SurvivingMonsters;   //# 종료 시점 생존 몬스터 수
    }
}
