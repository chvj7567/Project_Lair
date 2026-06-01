using System.Collections;
using Lair.Data;
using Lair.UI;
using UnityEngine;

namespace Lair.Character
{
    //# 공격자 측 — MeleeAttacker.OnHit 구독. 스케일 펀치 + 색 플래시 + 대표색 주입.
    //# 색 플래시는 HitFlash.FlashAttack 에 위임 — 몸체 머티리얼 단일 소유로 피격 flash 와 충돌 방지(Rule 02 §5).
    [RequireComponent(typeof(MeleeAttacker))]
    public class AttackJuice : MonoBehaviour
    {
        [SerializeField] private float _punchScale = 1.15f;    //# 기획서 §2 — ×1.15
        [SerializeField] private float _punchDuration = 0.12f; //# 기획서 §2 — 0.12초
        [SerializeField] private float _flashWhiteLerp = 0.6f; //# 기획서 §3 — 흰색 60% lerp
        //# 영웅 한정 — 데미지 숫자 대표색을 몸체 파랑 대신 흰색으로 오버라이드(기획서 §4.3). 빌더가 설정.
        [SerializeField] private bool _heroWhiteDamageColor;

        private MeleeAttacker _attacker;
        private HitFlash _hitFlash;
        private Vector3 _baseScale;
        private Color _repColor = Color.white;
        private bool _repColorCached;
        private Coroutine _co;

        //# 색 플래시 제외 prefix — HitFlash 와 동일 규칙(Aura/HpBar).
        private static readonly string[] ExcludeNamePrefixes = { "Aura", "HpBar" };

        private void Awake()
        {
            _attacker = GetComponent<MeleeAttacker>();
            _hitFlash = GetComponent<HitFlash>();
            _baseScale = transform.localScale;
            CacheRepColor();
        }

        private void OnEnable()
        {
            if (_attacker != null)
            {
                _attacker.OnHit += HandleHit;
                _attacker.DamageColor = _repColor;
            }
            transform.localScale = _baseScale;   //# 풀 재사용 리셋
        }

        private void OnDisable()
        {
            if (_attacker != null)
                _attacker.OnHit -= HandleHit;
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
            transform.localScale = _baseScale;
        }

        public void SetRepresentativeColorForTest(Color c)
        {
            _repColor = c;
            _repColorCached = true;
            if (_attacker != null)
                _attacker.DamageColor = c;
        }

        //# 대표색 — ① 영웅 흰색 오버라이드(§4.3), ② 몬스터는 종 색 정식 출처(SpeciesColor),
        //# ③ 폴백으로 첫 비제외 Renderer 의 _BaseColor (테스트/비몬스터 안전).
        private void CacheRepColor()
        {
            if (_repColorCached) return;
            if (_heroWhiteDamageColor)
            {
                _repColor = Color.white;
                _repColorCached = true;
                return;
            }
            //# 몬스터 visual(LittleGhost) 의 창백색이 아니라 종(species) 색을 잡는다.
            MonsterTag tag = GetComponent<MonsterTag>();
            if (tag != null)
            {
                _repColor = SpawnerStatusCell.SpeciesColor(tag.Key);
                _repColorCached = true;
                return;
            }
            Renderer[] rds = GetComponentsInChildren<Renderer>(includeInactive: true);
            foreach (Renderer rd in rds)
            {
                if (rd == null) continue;
                if (IsExcluded(rd.gameObject.name)) continue;
                Material m = rd.sharedMaterial;
                if (m == null) continue;
                _repColor = m.HasProperty("_BaseColor") ? m.GetColor("_BaseColor") : m.color;
                _repColorCached = true;
                return;
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

        private void HandleHit(IHealth target)
        {
            if (_co != null) StopCoroutine(_co);
            _co = StartCoroutine(PunchCo());
            //# 색 플래시 — 몸체 머티리얼 단일 소유자인 HitFlash 가 처리.
            if (_hitFlash != null)
                _hitFlash.FlashAttack(_flashWhiteLerp, _punchDuration);
        }

        private IEnumerator PunchCo()
        {
            float t = 0f;
            while (t < _punchDuration)
            {
                t += Time.deltaTime;
                float k = t / _punchDuration;
                //# 0→1 동안 baseScale → punch → baseScale (sin 반원).
                float s = 1f + (_punchScale - 1f) * Mathf.Sin(k * Mathf.PI);
                transform.localScale = _baseScale * s;
                yield return null;
            }
            transform.localScale = _baseScale;
            _co = null;
        }
    }
}
