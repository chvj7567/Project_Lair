using System.Collections.Generic;

namespace Lair.Battle
{
    //# 트리거된 카드 선택 이벤트 큐. BattleController 가 한 건씩 순차 처리.
    public class TriggerQueue
    {
        public enum Source { Passive, Active }

        public readonly struct Entry
        {
            public readonly Source SourceType;
            public readonly int Index;
            public Entry(Source s, int i) { SourceType = s; Index = i; }
        }

        private readonly Queue<Entry> _q = new();
        public int Count => _q.Count;

        public void Enqueue(Source s, int i) => _q.Enqueue(new Entry(s, i));
        public bool TryDequeue(out Entry e) => _q.TryDequeue(out e);
    }
}
