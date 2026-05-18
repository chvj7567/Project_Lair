using System.Collections;
using UnityEngine;

namespace Lair.Character
{
    //# 피격 시 머티리얼 BaseColor 반전 → 짧게 깜빡 → 원복.
    //# Health.OnChanged 구독으로 데미지 감지 (회복은 무시).
    [RequireComponent(typeof(Health))]
    [RequireComponent(typeof(Renderer))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.1f;

        private Health _health;
        private Renderer _renderer;
        private Material _matInstance;
        private Color _originalColor;
        private int _lastHp = -1;
        private Coroutine _co;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _renderer = GetComponent<Renderer>();
            //# .material 접근 시 자동으로 sharedMaterial 의 인스턴스 생성 — 다른 캐릭터 영향 X
            _matInstance = _renderer.material;
            _originalColor = ReadColor(_matInstance);
        }

        private void Start()
        {
            //# Health.Awake 가 Current = Max 로 초기화한 후 캐시
            if (_health != null) _lastHp = _health.Current;
        }

        private void OnEnable()
        {
            if (_health != null) _health.OnChanged += HandleChanged;
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= HandleChanged;
        }

        private void HandleChanged(int current, int max)
        {
            //# 첫 호출에서 _lastHp 미초기화 상태면 단순 캐시
            if (_lastHp < 0)
            {
                _lastHp = current;
                return;
            }
            //# 데미지(감소)인 경우에만 플래시. 회복(증가)은 무시.
            if (current < _lastHp)
            {
                if (_co != null) StopCoroutine(_co);
                _co = StartCoroutine(FlashCo());
            }
            _lastHp = current;
        }

        private IEnumerator FlashCo()
        {
            WriteColor(_matInstance, InvertColor(_originalColor));
            yield return new WaitForSeconds(_duration);
            WriteColor(_matInstance, _originalColor);
            _co = null;
        }

        private static Color ReadColor(Material mat)
        {
            if (mat.HasProperty("_BaseColor")) return mat.GetColor("_BaseColor");
            return mat.color;
        }

        private static void WriteColor(Material mat, Color c)
        {
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", c);
            mat.color = c;
        }

        private static Color InvertColor(Color c) => new Color(1f - c.r, 1f - c.g, 1f - c.b, c.a);
    }
}
