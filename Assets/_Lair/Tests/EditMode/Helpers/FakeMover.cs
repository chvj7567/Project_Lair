using Lair.Character;
using UnityEngine;

namespace Lair.Tests.Helpers
{
    //# IMover 테스트 더블. Speed 변경 검증용.
    public class FakeMover : IMover
    {
        public float Speed { get; set; } = 5f;
        public Vector3 LastMoveTarget { get; private set; }
        public int MoveCallCount { get; private set; }
        public bool Stopped { get; private set; }

        public void MoveTo(Vector3 target)
        {
            LastMoveTarget = target;
            MoveCallCount++;
            Stopped = false;
        }

        public void Stop()
        {
            Stopped = true;
        }
    }
}
