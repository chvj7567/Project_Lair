using UnityEngine;

namespace Lair.Character
{
    //# 등급 이펙트 시트 한 개(오라·불꽃 기둥·후광) — 코드 재생(루프) + 카메라 정면 빌보드 + 등장 페이드(monster-grades §2.3·§9.2).
    //# 시작 프레임 = frameOffset + 랜덤 위상(Play 마다 재설정). 하위 렌더러는 private 소유, 외부는 Play/Stop 만 쓴다.
    public class GradeFxSheet : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        [SerializeField] private Sprite[] _frames;
        [SerializeField] private float _fps = 30f;

        private const float FadeInSeconds = 0.3f;

        private float _elapsed;
        private float _startOffsetSec;
        private float _opacity;
        private bool _playing;
        private Transform _cam;

        //# 풀 Pop/Push — 꺼진 채로 시작(재생은 Play 가 켠다).
        private void OnEnable()
        {
            Camera mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;
        }

        //# frameOffset: Grade_Fx 의 시작 프레임 어긋남. 랜덤 위상은 호출자가 아닌 여기서 부여(풀 Pop 마다 재설정).
        public void Play(float opacity, int frameOffset)
        {
            _opacity = opacity;
            _elapsed = 0f;
            _startOffsetSec = (frameOffset + Random.value * Mathf.Max(1, _frames != null ? _frames.Length : 1)) / Mathf.Max(1f, _fps);
            _playing = true;
            gameObject.SetActive(true);
            Apply();
        }

        public void Stop()
        {
            _playing = false;
            gameObject.SetActive(false);
        }

        //# 월드 크기·위치 지정 — 크기 s 는 호출자(MonsterGradeVisual)가 cellWorld 로 계산.
        public void Place(Vector3 localPosition, float uniformScale)
        {
            transform.localPosition = localPosition;
            transform.localScale = new Vector3(uniformScale, uniformScale, 1f);
        }

        private void Update()
        {
            if (_playing == false)
                return;
            _elapsed += Time.deltaTime;
            Apply();
        }

        private void LateUpdate()
        {
            if (_playing && _cam != null)
            {
                transform.rotation = _cam.rotation;
            }
        }

        private void Apply()
        {
            if (_renderer == null || _frames == null || _frames.Length == 0)
                return;
            int frame = SpriteSheetFrames.LoopFrame(_elapsed + _startOffsetSec, _fps, _frames.Length);
            _renderer.sprite = _frames[frame];
            float fade = Mathf.Clamp01(_elapsed / FadeInSeconds);
            Color c = _renderer.color;
            c.a = _opacity * fade;
            _renderer.color = c;
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public bool PlayingForTest => _playing;
        public float AlphaForTest => _renderer != null ? _renderer.color.a : 0f;
        public void SetForTest(SpriteRenderer renderer, Sprite[] frames, float fps)
        {
            _renderer = renderer;
            _frames = frames;
            _fps = fps;
        }
#endif
    }
}
