using UnityEngine;

namespace Lair.Character
{
    //# 스폰 시 현재 스테이지의 영웅 외형(Stage.heroScale·heroTier)을 영웅에 적용 (hero-stage-variant plan Task 5, hero-2d-conversion §6.4·§11).
    //# 몸 색/아웃라인은 원화 베이크(런타임 틴트 없음) — 여기서는 스케일·오버레이 티어 전환만 담당. 발광은 쓰인 적이 없어 제거(so-to-json-migration §7.6).
    public class HeroStageVariantApplier : MonoBehaviour
    {
        //# 강화 오버레이(Enhance) — 스테이지 2~5 완전 합성 원화 전환(§6.4).
        [SerializeField] private MonsterTierOverlay _tierOverlay;
        //# 몸(Visual2D) 스프라이트 렌더러 — 스테이지 1 에서만 활성(§6.4.1 부분 커버리지 회피).
        [SerializeField] private SpriteRenderer _body;

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

        //# scale: 크기 배율(Stage.heroScale), tier: 강화 외형 티어 0~4(Stage.heroTier) — 0 이면 몸만, 1 이상이면 오버레이만 보인다.
        public void Apply(float scale, int tier)
        {
            EnsureBaseScale();
            transform.localScale = _baseScale * scale;

            if (_tierOverlay != null)
            {
                _tierOverlay.SetTier(tier);
            }
            if (_body != null)
            {
                _body.enabled = tier == 0;
            }
        }
    }
}
