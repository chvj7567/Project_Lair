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

        //# B3 — 출혈 카드 테스트용 수동 토글.
        public bool IsMoving { get; set; }

        public void MoveTo(Vector3 target)
        {
            LastMoveTarget = target;
            MoveCallCount++;
            Stopped = false;
            IsMoving = true;
        }

        public void Stop()
        {
            Stopped = true;
            IsMoving = false;
        }
    }
}
