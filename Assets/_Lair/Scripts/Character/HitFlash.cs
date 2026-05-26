using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 피격 시 자식 모든 Renderer 의 BaseColor 반전 → 짧게 깜빡 → 원복.
    //# Health.OnChanged 구독으로 데미지 감지 (회복은 무시).
    //# root Renderer 가 없을 수도 있는 구조(LittleGhost nested 등) 를 지원하기 위해
    //# GetComponentsInChildren<Renderer> 로 모든 Renderer 를 수집해 인스턴스 머티리얼을 적용한다.
    //# Aura(자식 Cylinder primitive) 는 이름 기반으로 제외해 깜빡임에서 빠진다.
    [RequireComponent(typeof(Health))]
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] private float _duration = 0.1f;
        //# 자식 Renderer 이름이 이 prefix 로 시작하면 플래시 대상에서 제외 (오오라/HP바 트랙 등)
        private static readonly string[] ExcludeNamePrefixes = { "Aura", "HpBar" };

        private Health _health;
        private readonly List<Material> _matInstances = new List<Material>();
        private readonly List<Color> _originalColors = new List<Color>();
        private int _lastHp = -1;
        private Coroutine _co;

        private void Awake()
        {
            _health = GetComponent<Health>();
            CacheRenderers();
        }

        private void Start()
        {
            //# Health.Awake 가 Current = Max 로 초기화한 후 캐시
            if (_health != null) _lastHp = _health.Current;
        }

        private void OnEnable()
        {
            if (_health != null)
            {
                _health.OnChanged += HandleChanged;
                //# Rule 12 — 풀 재사용 시 _lastHp 재캐시 (Health.OnEnable 가 Current 복원 후)
                _lastHp = _health.Current;
            }
            //# 진행 중이던 flash 정리 + 색상 원복
            if (_co != null) { StopCoroutine(_co); _co = null; }
            RestoreOriginalColors();
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= HandleChanged;
            //# 코루틴은 GameObject 비활성화 시 자동 중단되지만 _co 참조 정리 + 색상 원복
            _co = null;
            RestoreOriginalColors();
        }

        //# 자식의 모든 Renderer 를 수집해 .material 로 인스턴스화하고 원본 색 캐시.
        //# 이름이 ExcludeNamePrefixes 로 시작하는 Renderer 는 제외 (Aura 등 색이 정체성).
        private void CacheRenderers()
        {
            _matInstances.Clear();
            _originalColors.Clear();
            var renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (var rd in renderers)
            {
                if (rd == null) continue;
                if (IsExcluded(rd.gameObject.name)) continue;
                //# .material 접근 시 sharedMaterial 의 인스턴스가 생성됨 → 다른 캐릭터 영향 X
                var mat = rd.material;
                if (mat == null) continue;
                _matInstances.Add(mat);
                _originalColors.Add(ReadColor(mat));
            }
        }

        private static bool IsExcluded(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            for (int i = 0; i < ExcludeNamePrefixes.Length; i++)
            {
                if (name.StartsWith(ExcludeNamePrefixes[i])) return true;
            }
            return false;
        }

        private void RestoreOriginalColors()
        {
            for (int i = 0; i < _matInstances.Count; i++)
            {
                var mat = _matInstances[i];
                if (mat == null) continue;
                WriteColor(mat, _originalColors[i]);
            }
        }

        private void ApplyInvertedColors()
        {
            for (int i = 0; i < _matInstances.Count; i++)
            {
                var mat = _matInstances[i];
                if (mat == null) continue;
                WriteColor(mat, InvertColor(_originalColors[i]));
            }
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
            ApplyInvertedColors();
            yield return new WaitForSeconds(_duration);
            RestoreOriginalColors();
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
