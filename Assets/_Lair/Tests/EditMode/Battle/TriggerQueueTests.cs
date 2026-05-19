using NUnit.Framework;
using Lair.Battle;

namespace Lair.Tests.Battle
{
    //# TriggerQueue FIFO + 빈 큐 안전성 검증.
    public class TriggerQueueTests
    {
        [Test]
        public void Enqueue_후_Dequeue_순서_FIFO()
        {
            var q = new TriggerQueue();
            q.Enqueue(TriggerQueue.Source.Passive, 0);
            q.Enqueue(TriggerQueue.Source.Passive, 1);

            Assert.AreEqual(2, q.Count);

            Assert.IsTrue(q.TryDequeue(out var e1));
            Assert.AreEqual(TriggerQueue.Source.Passive, e1.SourceType);
            Assert.AreEqual(0, e1.Index);

            Assert.IsTrue(q.TryDequeue(out var e2));
            Assert.AreEqual(1, e2.Index);
        }

        [Test]
        public void 빈_큐_TryDequeue_false()
        {
            var q = new TriggerQueue();
            Assert.IsFalse(q.TryDequeue(out _));
            Assert.AreEqual(0, q.Count);
        }
    }
}
