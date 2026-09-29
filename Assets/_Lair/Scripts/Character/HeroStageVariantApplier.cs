using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 스폰 시 현재 스테이지 variant 를 영웅에 적용 (hero-stage-variant plan Task 5, hero-2d-conversion §6.2·§6.4·§11).
    //# 몸 색/아웃라인은 원화 베이크(런타임 틴트 없음, §6.2·§6.3) — 여기서는 발광·스케일·오버레이 티어 전환만 담당.
    public class HeroStageVariantApplier : MonoBehaviour
    {
        //# 강화 오버레이(Enhance) — 스테이지 2~5 완전 합성 원화 전환(§6.4).
        [SerializeField] private MonsterTierOverlay _tierOverlay;
        //# 몸(Visual2D) 스프라이트 렌더러 — 스테이지 1 에서만 활성(§6.4.1 부분 커버리지 회피).
        [SerializeField] private SpriteRenderer _body;
        //# 발광(_EmissionColor) 적용 대상 — [Visual2D 몸, Visual2D/Enhance].
        [SerializeField] private SpriteRenderer[] _emissionTargets;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private const string EmissionKeyword = "_EMISSION";

        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        private void Awake()
        {
            EnsureBaseScale();
        }

        //# 프리팹 원본 스케일을 1회 캐시 — 풀 재사용 시 배수 복리 누적 방지(항상 base × mul).
        private void EnsureBaseScale()
        {
            if (_baseScaleCaptured)
                return;
            _baseScale = transform.localScale;
            _baseScaleCaptured = true;
        }

        public void Apply(HeroStageVariant variant)
        {
            if (variant == null)
                return;

            EnsureBaseScale();
            ApplyEmission(variant);
            ApplyScale(variant);

            if (_tierOverlay != null)
            {
                _tierOverlay.SetTier(variant.Tier);
            }
            if (_body != null)
            {
                _body.enabled = variant.Tier == 0;
            }
        }

        //# 발광 — 사용 스테이지는 색×intensity + 키워드 활성, 미사용은 검정·키워드 비활성으로 잔존 발광 차단(기획서 §1.5).
        private void ApplyEmission(HeroStageVariant variant)
        {
            if (_emissionTargets == null)
                return;
            Color emission = variant.UseEmission
                ? variant.EmissionColor * Mathf.Max(0f, variant.EmissionIntensity)
                : Color.black;
            for (int i = 0; i < _emissionTargets.Length; i++)
            {
                SpriteRenderer rd = _emissionTargets[i];
                if (rd == null)
                    continue;
                Material mat = rd.material;
                if (mat == null)
                    continue;
                if (variant.UseEmission)
                {
                    mat.EnableKeyword(EmissionKeyword);
                }
                else
                {
                    mat.DisableKeyword(EmissionKeyword);
                }
                if (mat.HasProperty(EmissionColorId))
                {
                    mat.SetColor(EmissionColorId, emission);
                }
            }
        }

        private void ApplyScale(HeroStageVariant variant)
        {
            float mul = variant.ScaleMultiplier <= 0f ? 1f : variant.ScaleMultiplier;
            transform.localScale = _baseScale * mul;
        }
    }
}
