using System;

namespace Lair.Battle
{
    //# 전투 경과 시간 관리. POCO, Unity 의존성 0.
    public class BattleClock
    {
        public float Elapsed { get; private set; }
        public float TotalSeconds { get; }
        public bool IsRunning { get; private set; }

        public event Action<float> OnTick;
        public event Action OnTimeUp;

        public BattleClock(float totalSeconds)
        {
            TotalSeconds = totalSeconds;
        }

        public void Start()
        {
            Elapsed = 0f;
            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;
        }

        public void Tick(float dt)
        {
            if (IsRunning == false) return;
            Elapsed += dt;
            OnTick?.Invoke(Elapsed);
            if (Elapsed >= TotalSeconds)
            {
                IsRunning = false;
                OnTimeUp?.Invoke();
            }
        }
    }
}
