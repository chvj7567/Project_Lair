using System.Collections.Generic;
using Lair.Data;

namespace Lair.Meta
{
    //# 한 판 요약 — EndBattle 시점 수집, 도전과제 판정 입력 (jsonl RunRecord 와 별개 — 빌드에서도 동작).
    public class RunSummary
    {
        public BattleResult Result;
        public float DeathTime;
        public float HeroDamagedRatio;    //# 0~1 — 영웅 최대 HP 대비 깎은 비율
        public int MaxSynergyTier;        //# 4축 중 최고 달성 Tier (0~3)
        public List<string> Picks = new List<string>();   //# 도감 기록용 — ECardId.ToString()
    }
}
