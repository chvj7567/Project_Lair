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
        private const float DesignCell = 160f;
        //# 이펙트 월드 크기 ÷ 몸 프레임 기준 비율(종족 공통) — 이펙트가 프리팹 루트(스케일 r) 자식이라 r 만큼 이중 축소/확대되던 것을 MonsterGradeVisual 이 r 로 나눠 보정한다.
        public const float RefRatio = 0.8f;
        private const float DefaultPpu = 100f;

        private float _elapsed;
        private float _startOffsetSec;
        private float _opacity;
        private bool _playing;
        private Transform _cam;
        //# 몸 기준 오프셋(루트 yaw 0 가정의 좌우·위·깊이). 루트가 yaw 로 돌아가도 화면 좌우로 가도록 AlignOffset 이 부모 회전을 상쇄한다.
        private Vector3 _baseOffset;

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

        //# 월드 크기·위치 지정 — 월드 가로 = 프레임 가로 px x sheetScale ÷ 160 x cellWorld(monster-grades §2.1).
        //# 스프라이트 자연 가로 = 프레임 px ÷ PPU 이므로 localScale = sheetScale x cellWorld x PPU ÷ 160(자기 Sprite.pixelsPerUnit 로 환산, 임포트 PPU 비의존).
        public void Place(Vector3 localPosition, float sheetScale, float cellWorld)
        {
            float ppu = _frames != null && _frames.Length > 0 && _frames[0] != null ? _frames[0].pixelsPerUnit : DefaultPpu;
            float uniform = sheetScale * cellWorld * ppu / DesignCell;
            _baseOffset = localPosition;
            transform.localScale = new Vector3(uniform, uniform, 1f);
            AlignOffset();
        }

        //# 부모(몬스터 루트)가 yaw ±90° 로 돌아 있으면 로컬 ±X 가 카메라 깊이축이 되어 측면 기둥이 몸 앞뒤에 겹친다 — 부모 회전을 상쇄해 좌우로 보낸다. 부모가 없으면 기존 동작.
        public void AlignOffset()
        {
            Transform parent = transform.parent;
            transform.localPosition = parent != null ? Quaternion.Inverse(parent.rotation) * _baseOffset : _baseOffset;
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
            if (_playing == false)
                return;
            AlignOffset();
            if (_cam != null)
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
