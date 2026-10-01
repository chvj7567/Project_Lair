using Lair.Data;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Lair.Battle
{
    //# 스포너 제단 루트 파사드(Rule 02 §10) — scene-2d-conversion §3.2 · §7.4. 스포너 자식, 씬 정적 배치(풀 아님).
    //# 출력 종이 바뀌면 발광색으로 즉시 재틴트하고, 링 맥동·결정 흔들림·조명 세기를 매 프레임 갱신한다.
    public class SpawnerAltar2D : MonoBehaviour, ISpawnerAltar
    {
        private const float DotPpu = 48f;
        private const float PhaseStep = 1.0472f;

        [SerializeField] private SpriteRenderer _ringDim;
        [SerializeField] private SpriteRenderer _ring;
        [SerializeField] private SpriteRenderer _crystal;
        [SerializeField] private SpriteRenderer _crystalCore;
        [SerializeField] private SpriteRenderer _sparkle;
        [SerializeField] private Light2D _light;
        //# 스포너 인덱스 i — 위상 φ = i × 60°
        [SerializeField] private int _phaseIndex;

        private ISpawnerOutputProvider _provider;
        private Color _glow = Color.white;

        public Color GlowColor => _glow;

        //# 결정 흔들림(도트, 화면 아래 +) = floor(sin(2.4t + φ)·1.5 + 0.5)
        public static int CrystalBobDots(float t, float phase)
        {
            return Mathf.FloorToInt(Mathf.Sin(2.4f * t + phase) * 1.5f + 0.5f);
        }

        //# Rule 02 §7 — 상위 스포너는 인터페이스로 1회 캐싱 참조(SpawnerBody 와 같은 구독 방식).
        private void OnEnable()
        {
            _provider = GetComponentInParent<ISpawnerOutputProvider>();
            if (_provider == null)
                return;
            _provider.OnOutputTypeChanged += HandleTypeChanged;
            HandleTypeChanged(_provider.CurrentType);
        }

        private void OnDisable()
        {
            if (_provider != null)
            {
                _provider.OnOutputTypeChanged -= HandleTypeChanged;
            }
        }

        //# 카메라는 회전하지 않으므로 빌보드 회전은 1회 복사
        private void Start()
        {
            Camera cam = Camera.main;
            if (cam == null)
                return;
            transform.rotation = cam.transform.rotation;
        }

        private void Update()
        {
            float t = Time.time;
            float phase = _phaseIndex * PhaseStep;
            int bob = CrystalBobDots(t, phase);

            if (_ring != null)
            {
                Color c = _glow;
                c.a = Mathf.Clamp01(0.45f + 0.25f * Mathf.Sin(2f * t + phase));
                _ring.color = c;
            }
            SetLocalY(_crystal, 0f, 27 - bob);
            SetLocalY(_crystalCore, 0f, 25 - bob);
            SetLocalY(_sparkle, 3f, 30 - bob);
            if (_light != null)
            {
                _light.intensity = 1f + 0.12f * Mathf.Sin(2f * t + phase);
            }
        }

        private void HandleTypeChanged(EMonster type)
        {
            _glow = SpeciesVisual.SpeciesGlowColor(type);
            Tint(_ringDim);
            Tint(_ring);
            Tint(_crystal);
            Tint(_sparkle);
            if (_light != null)
            {
                _light.color = _glow;
            }
        }

        private void Tint(SpriteRenderer r)
        {
            if (r == null)
                return;
            Color c = _glow;
            c.a = r.color.a;
            r.color = c;
        }

        private static void SetLocalY(SpriteRenderer r, float xDots, int upDots)
        {
            if (r == null)
                return;
            r.transform.localPosition = new Vector3(xDots / DotPpu, upDots / DotPpu, 0f);
        }
    }
}
