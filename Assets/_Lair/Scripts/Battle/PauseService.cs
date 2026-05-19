using UnityEngine;

namespace Lair.Battle
{
    //# Time.timeScale 으로 게임 정지. 중첩 호출 안전 (depth 카운터).
    //# UI 입력/애니메이션은 unscaledDeltaTime 사용 시 계속 작동.
    public class PauseService
    {
        private int _depth;
        public bool IsPaused => _depth > 0;

        public void Pause()
        {
            _depth++;
            if (_depth == 1) Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (_depth == 0) return;
            _depth--;
            if (_depth == 0) Time.timeScale = 1f;
        }

        //# Slice A 의 EndBattle 같은 강제 정지 — depth 무시
        public void ForcePause() { _depth = int.MaxValue / 2; Time.timeScale = 0f; }
    }
}
