using System.Collections.Generic;
using Lair.Data;

namespace Lair.Card
{
    //# 카드 3픽 캡 (전역) — 카드별 픽수를 한 런 동안 누적. 캡 도달 카드는 CardDeck.Draw 에서 제외.
    //# BattleController 가 보유. BuildSynergyService.Reset 과 동일 시점에 Reset.
    public class CardPickCounter
    {
        //# 카드 1장당 실효 중첩 상한. 도달 시 이후 후보 풀에서 제외.
        public const int Cap = 3;

        private readonly Dictionary<ECardId, int> _counts = new Dictionary<ECardId, int>();

        public void RecordPick(ECardId id)
        {
            int prev;
            _counts.TryGetValue(id, out prev);
            _counts[id] = prev + 1;
        }

        public int GetCount(ECardId id)
        {
            int v;
            return _counts.TryGetValue(id, out v) ? v : 0;
        }

        public bool IsCapped(ECardId id) => GetCount(id) >= Cap;

        //# 라운드(=런) 시작 / Restart 시 호출.
        public void Reset()
        {
            _counts.Clear();
        }
    }
}
