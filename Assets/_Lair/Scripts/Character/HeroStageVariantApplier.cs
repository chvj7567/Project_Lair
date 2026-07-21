using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 스폰 시 현재 스테이지 variant 를 영웅에 적용 (hero-stage-variant plan Task 5).
    //# 틴트는 HitFlash 로 위임(spec §5.1 색 채널 단일화) — 피격/공격 flash·풀 재사용 후에도 유지.
    //# 발광/아웃라인/스케일은 여기서 직접 적용. 참조는 [SerializeField] 인스펙터 와이어링(Rule 02 §5).
    public class HeroStageVariantApplier : MonoBehaviour
    {
        //# 틴트 baseline 위임 대상 — 미할당이면 Awake 에서 1회 캐싱.
        [SerializeField] private HitFlash _hitFlash;
        //# 발광(_EmissionColor) 적용 대상 스켈레톤 메시 렌더러.
        [SerializeField] private Renderer[] _skeletonRenderers;
        //# 아웃라인 서브머터리얼(materials[_outlineSubMaterialIndex])을 보유한 렌더러.
        [SerializeField] private SkinnedMeshRenderer _outlineRenderer;
        [SerializeField] private int _outlineSubMaterialIndex = 1;
        //# 아웃라인 활성 시 셰이더 _OutlineWidth (오브젝트공간). 비활성은 0 으로 접어 숨김.
        [SerializeField] private float _outlineWidth = 0.02f;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
        private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
        private static readonly int OutlineWidthId = Shader.PropertyToID("_OutlineWidth");
        private const string EmissionKeyword = "_EMISSION";

        private Vector3 _baseScale = Vector3.one;
        private bool _baseScaleCaptured;

        private void Awake()
        {
            EnsureBaseScale();
            if (_hitFlash == null)
            {
                _hitFlash = GetComponent<HitFlash>();
            }
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
            ApplyOutline(variant);
            ApplyScale(variant);

            //# 틴트는 마지막에 — HitFlash 원본을 variant 색으로 (재)설정(spec §5.1). 미할당 시 baseColor 직접 기록.
            if (_hitFlash != null)
            {
                _hitFlash.SetBaselineColor(variant.TintColor);
            }
            else
            {
                WriteBaseColorFallback(variant.TintColor);
            }
        }

        //# 발광 — 사용 스테이지는 색×intensity + 키워드 활성, 미사용은 검정·키워드 비활성으로 잔존 발광 차단(기획서 §1.5).
        private void ApplyEmission(HeroStageVariant variant)
        {
            if (_skeletonRenderers == null)
                return;
            Color emission = variant.UseEmission
                ? variant.EmissionColor * Mathf.Max(0f, variant.EmissionIntensity)
                : Color.black;
            for (int i = 0; i < _skeletonRenderers.Length; i++)
            {
                Renderer rd = _skeletonRenderers[i];
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

        //# 아웃라인 — 서브머터리얼[index] 인스턴스에 색 주입 + 너비 토글(머터리얼 배열 재할당 없이 0 으로 숨김).
        private void ApplyOutline(HeroStageVariant variant)
        {
            if (_outlineRenderer == null)
                return;
            Material[] mats = _outlineRenderer.materials;
            if (mats == null || _outlineSubMaterialIndex < 0 || _outlineSubMaterialIndex >= mats.Length)
                return;
            Material outline = mats[_outlineSubMaterialIndex];
            if (outline == null)
                return;
            if (outline.HasProperty(OutlineColorId))
            {
                outline.SetColor(OutlineColorId, variant.OutlineColor);
            }
            if (outline.HasProperty(OutlineWidthId))
            {
                outline.SetFloat(OutlineWidthId, variant.UseOutline ? _outlineWidth : 0f);
            }
        }

        private void ApplyScale(HeroStageVariant variant)
        {
            float mul = variant.ScaleMultiplier <= 0f ? 1f : variant.ScaleMultiplier;
            transform.localScale = _baseScale * mul;
        }

        //# HitFlash 미할당(예외 구성)일 때만 — 스켈레톤 렌더러에 직접 baseColor 기록.
        private void WriteBaseColorFallback(Color tint)
        {
            if (_skeletonRenderers == null)
                return;
            for (int i = 0; i < _skeletonRenderers.Length; i++)
            {
                Renderer rd = _skeletonRenderers[i];
                if (rd == null)
                    continue;
                Material mat = rd.material;
                if (mat == null)
                    continue;
                if (mat.HasProperty(BaseColorId))
                {
                    mat.SetColor(BaseColorId, tint);
                }
                mat.color = tint;
            }
        }
    }
}
