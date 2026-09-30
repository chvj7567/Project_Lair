using System;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Character
{
    //# 프레임 계산 순수 함수 — Unity 비의존(EditMode 테스트 대상). 프레임 = floor(경과 × fps).
    public static class SpriteSheetFrames
    {
        //# float 오차(1/12×12 = 0.99999994) 로 프레임 경계가 한 칸 밀리지 않게 하는 보정.
        private const float Epsilon = 0.0001f;

        private static int RawFrame(float elapsed, float fps)
        {
            if (elapsed <= 0f || fps <= 0f)
                return 0;
            return (int)Mathf.Floor(elapsed * fps + Epsilon);
        }

        //# 루프 재생 — 경과 시간에서 [0, frames) 프레임.
        public static int LoopFrame(float elapsed, float fps, int frames)
        {
            if (frames <= 0)
                return 0;
            return RawFrame(elapsed, fps) % frames;
        }

        //# 1회 재생 — 마지막 프레임에서 고정(종료 판정은 IsFinishedOnce).
        public static int OnceFrame(float elapsed, float fps, int frames)
        {
            if (frames <= 0)
                return 0;
            return Mathf.Min(RawFrame(elapsed, fps), frames - 1);
        }

        //# 1회 재생 종료 여부 — 프레임 지수가 frames 에 도달하면 종료.
        public static bool IsFinishedOnce(float elapsed, float fps, int frames)
        {
            return RawFrame(elapsed, fps) >= frames;
        }

        //# 위상(0~1 순환) → 프레임. 음수·1 초과·NaN 입력도 [0, frames) 로 접는다.
        public static int PhaseFrame(float normalized, int frames)
        {
            if (frames <= 0)
                return 0;
            if (float.IsNaN(normalized) || float.IsInfinity(normalized))
                return 0;
            float frac = normalized - Mathf.Floor(normalized);
            return Mathf.Clamp((int)Mathf.Floor(frac * frames), 0, frames - 1);
        }
    }

    //# 도트 시트 FX 루트 파사드(Rule 02 §10). 코드 재생(12fps) + 루프/1회 + 종료 시 CHMPool 반환.
    //# 하위 SpriteRenderer(AuraFx)는 private 소유, 외부는 ISpriteSheetFx 로만 접근.
    [RequireComponent(typeof(CHPoolable))]
    public class SpriteSheetFx : MonoBehaviour, ISpriteSheetFx
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _fps = 12f;
        [SerializeField] private bool _loop;
        [SerializeField] private bool _billboard;
        [SerializeField] private bool _autoPlay = true;
        [SerializeField] private bool _returnToPoolOnFinish;
        [SerializeField] private CHPoolable _poolable;   //# 인스펙터 배선 — 런타임 GetComponent 금지(Rule 02 §5)

        private float _elapsed;
        private int _currentFrame;
        private bool _finished;
        private Camera _cam;

        public event Action Finished;

        public int CurrentFrame => _currentFrame;
        public bool IsFinished => _finished;

        //# 풀 재사용 리셋(Rule 03 §4) — 경과·프레임·종료 상태 초기화, 카메라 재캐시.
        private void OnEnable()
        {
            _elapsed = 0f;
            _finished = false;
            _cam = Camera.main;
            ShowFrame(0);
        }

        private void Update()
        {
            if (_autoPlay == false || _finished)
                return;

            _elapsed += Time.deltaTime;
            if (_loop)
            {
                ShowFrame(SpriteSheetFrames.LoopFrame(_elapsed, _fps, FrameCount));
                return;
            }

            if (SpriteSheetFrames.IsFinishedOnce(_elapsed, _fps, FrameCount))
            {
                Finish();
                return;
            }
            ShowFrame(SpriteSheetFrames.OnceFrame(_elapsed, _fps, FrameCount));
        }

        private void LateUpdate()
        {
            if (_billboard == false || _renderer == null || _cam == null)
                return;
            //# 카메라 회전 복사 — 화면 정렬 빌보드(MonsterVisual2D 와 동일 개념).
            _renderer.transform.rotation = _cam.transform.rotation;
        }

        public void SetLoopPhase(float normalized)
        {
            ShowFrame(SpriteSheetFrames.PhaseFrame(normalized, FrameCount));
        }

        private int FrameCount => _frames == null ? 0 : _frames.Length;

        private void ShowFrame(int index)
        {
            _currentFrame = index;
            if (_renderer == null || FrameCount == 0)
                return;
            _renderer.sprite = _frames[Mathf.Clamp(index, 0, FrameCount - 1)];
        }

        private void Finish()
        {
            _finished = true;
            Finished?.Invoke();
            if (_returnToPoolOnFinish == false || _poolable == null)
                return;
            if (CHMPool.Instance != null)
                CHMPool.Instance.Push(_poolable);
        }
    }
}
