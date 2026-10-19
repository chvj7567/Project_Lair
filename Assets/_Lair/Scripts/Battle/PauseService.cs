using UnityEngine;

namespace Lair.Battle
{
    //# Time.timeScale 으로 게임 정지. 중첩 호출 안전 (depth 카운터).
    //# UI 입력/애니메이션은 unscaledDeltaTime 사용 시 계속 작동.
    //# 유물 2배속(relics §5.2) — "목표 게임 속도"를 이 서비스가 보관해 Resume 이 1 로 덮어쓰지 않는다.
    public class PauseService
    {
        private int _depth;
        private float _targetScale = 1f;
        public bool IsPaused => _depth > 0;

        //# 정지 해제 시 복원될 게임 속도(기본 1, 배속 on 이면 speedScale).
        public float TargetScale => _targetScale;

        public void Pause()
        {
            _depth++;
            if (_depth == 1) Time.timeScale = 0f;
        }

        public void Resume()
        {
            if (_depth == 0) return;
            _depth--;
            if (_depth == 0) Time.timeScale = _targetScale;
        }

        //# 목표 게임 속도 변경 — 정지 중(depth > 0)이면 보관만 하고 Resume 때 반영, 아니면 즉시 반영.
        public void SetTargetScale(float scale)
        {
            _targetScale = Mathf.Max(0.1f, scale);
            if (_depth == 0) Time.timeScale = _targetScale;
        }

        //# 씬 이탈 직전 전부 해제 — timeScale 은 static 이라 다음 씬에 정지·배속이 새지 않게. 목표 속도도 1 로 되돌린다.
        public void ResetAll()
        {
            _depth = 0;
            _targetScale = 1f;
            Time.timeScale = 1f;
        }

        //# Slice A 의 EndBattle 같은 강제 정지 — depth 무시
        public void ForcePause()
        {
            _depth = int.MaxValue / 2;
            Time.timeScale = 0f;
        }
    }
}
