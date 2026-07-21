using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 피격 시 자식 모든 Renderer 의 BaseColor 반전 → 짧게 깜빡 → 원복.
    //# Health.OnChanged 구독으로 데미지 감지 (회복은 무시).
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
        //# 공격자 색 플래시(AttackJuice) 전용 — 피격 flash 와 별도 코루틴/우선순위.
        private Coroutine _attackCo;

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
            if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
            RestoreOriginalColors();
        }

        private void OnDisable()
        {
            if (_health != null) _health.OnChanged -= HandleChanged;
            //# 코루틴은 GameObject 비활성화 시 자동 중단되지만 참조 정리 + 색상 원복
            _co = null;
            _attackCo = null;
            RestoreOriginalColors();
        }

        //# 공격자 색 플래시 (AttackJuice 호출) — 원색에서 흰색으로 lerp 만큼 밝게 번쩍 후 원복.
        //# 피격 flash(_co) 가 진행 중이면 양보 — 피격 표시가 우선.
        public void FlashAttack(float whiteLerp, float duration)
        {
            if (isActiveAndEnabled == false) return;
            if (_co != null) return;
            if (_attackCo != null)
            {
                StopCoroutine(_attackCo);
            }
            _attackCo = StartCoroutine(AttackFlashCo(whiteLerp, duration));
        }

        private IEnumerator AttackFlashCo(float whiteLerp, float duration)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = t / duration;
                //# sin 반원: 0→peak→0. peak 에서 원색→흰색 whiteLerp 만큼.
                float amt = whiteLerp * Mathf.Sin(k * Mathf.PI);
                //# 피격 flash 가 도중에 시작되면 양보하고 원복.
                if (_co != null)
                {
                    break;
                }
                ApplyBrightenedColors(amt);
                yield return null;
            }
            RestoreOriginalColors();
            _attackCo = null;
        }

        private void ApplyBrightenedColors(float toWhite)
        {
            for (int i = 0; i < _matInstances.Count; i++)
            {
                Material mat = _matInstances[i];
                if (mat == null) continue;
                Color c = _originalColors[i];
                Color lit = Color.Lerp(c, Color.white, toWhite);
                lit.a = c.a;
                WriteColor(mat, lit);
            }
        }

        //# 스테이지 variant 틴트를 "원본" 색으로 (재)설정 — 이후 모든 원복(피격/공격 flash 종료·풀 재사용 OnEnable)이
        //# 이 색으로 되돌아가 틴트가 유지된다(spec §5.1 색 채널 단일화). HeroStageVariantApplier 가 마지막에 호출.
        public void SetBaselineColor(Color baseline)
        {
            for (int i = 0; i < _originalColors.Count; i++)
            {
                _originalColors[i] = baseline;
            }
            //# 진행 중 flash 정리 후 새 원본으로 즉시 원복.
            if (_co != null) { StopCoroutine(_co); _co = null; }
            if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; }
            RestoreOriginalColors();
        }

        //# 자식의 모든 Renderer 를 수집해 .material 로 인스턴스화하고 원본 색 캐시.
        //# 이름이 ExcludeNamePrefixes 로 시작하는 Renderer 는 제외 (Aura 등 색이 정체성).
        private void CacheRenderers()
        {
            _matInstances.Clear();
            _originalColors.Clear();
            Renderer[] renderers = GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (Renderer rd in renderers)
            {
                if (rd == null) continue;
                if (IsExcluded(rd.gameObject.name)) continue;
                //# .material 접근 시 sharedMaterial 의 인스턴스가 생성됨 → 다른 캐릭터 영향 X
                Material mat = rd.material;
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
                Material mat = _matInstances[i];
                if (mat == null) continue;
                WriteColor(mat, _originalColors[i]);
            }
        }

        private void ApplyInvertedColors()
        {
            for (int i = 0; i < _matInstances.Count; i++)
            {
                Material mat = _matInstances[i];
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
                //# 피격 표시 우선 — 진행 중인 공격 플래시 중단 + 원복.
                if (_attackCo != null) { StopCoroutine(_attackCo); _attackCo = null; RestoreOriginalColors(); }
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
