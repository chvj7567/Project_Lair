using UnityEngine;

namespace Lair.Character
{
    //# 바닥 강화 문장(AuraSigil) — 레벨→문장 단계([0,0,1,1,2,3]) 표시(기획서 §6.3.1·§6.3.6).
    //# 이름 접두 "Aura" 로 시작 → HitFlash/AttackJuice 색 플래시 제외 대상(기존 규칙 재사용).
    public class MonsterEnhanceSigil : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer _renderer;
        //# index0=링1, 1=링2, 2=링2+파편(회전)
        [SerializeField] private Sprite[] _stageSprites;
        [SerializeField] private float _rotationSpeedDegPerSec = 45f;

        private const float SigilAlpha = 0.85f;
        private const int RotatingStage = 3;

        private IHealth _health;
        private Quaternion _restLocalRotation;
        private int _stage;

        private void Awake()
        {
            if (_renderer == null) _renderer = GetComponent<SpriteRenderer>();
            _restLocalRotation = transform.localRotation;
            LairCharacter character = GetComponentInParent<LairCharacter>();
            _health = character != null ? character.Get<IHealth>() : null;
        }

        //# 풀 재사용 — 숨김 + 회전 원위치 복귀 + 사망 구독(Rule 03 §4).
        private void OnEnable()
        {
            SetStage(0, Color.white);
            transform.localRotation = _restLocalRotation;
            if (_health != null) _health.OnDied += HandleDied;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnDied -= HandleDied;
        }

        //# stage: 0=숨김, 1=링1, 2=링2, 3=링2+파편. tint = SpeciesGlowColor(species), α 0.85 고정.
        public void SetStage(int stage, Color tint)
        {
            _stage = stage;
            if (_renderer == null)
                return;
            if (stage <= 0 || _stageSprites == null || stage > _stageSprites.Length)
            {
                _renderer.enabled = false;
                return;
            }
            Sprite sprite = _stageSprites[stage - 1];
            if (sprite == null)
            {
                _renderer.enabled = false;
                return;
            }
            _renderer.sprite = sprite;
            Color c = tint;
            c.a = SigilAlpha;
            _renderer.color = c;
            _renderer.enabled = true;
        }

        //# 사망 순간 즉시 숨김(기획서 §5.5 불변식 3) — 풀 반환 지연과 무관.
        private void HandleDied()
        {
            if (_renderer != null) _renderer.enabled = false;
        }

        private void Update()
        {
            if (_stage < RotatingStage)
                return;
            transform.Rotate(Vector3.up, _rotationSpeedDegPerSec * Time.deltaTime, Space.Self);
        }

#if UNITY_EDITOR || UNITY_INCLUDE_TESTS
        public void SetRendererForTest(SpriteRenderer renderer) => _renderer = renderer;
        public void SetStageSpritesForTest(Sprite[] sprites) => _stageSprites = sprites;
        public int StageForTest => _stage;
#endif
    }
}
