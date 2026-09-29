using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 스테이지 한 개의 외형 변형 + 스탯 배수 (hero-stage-variant 기획서 §1.2/§2.1, hero-2d-conversion §11).
    //# 전투 몸 색/아웃라인은 2D 원화에 직접 베이크되어 전투 런타임 소비자가 없다 — UseOutline/OutlineColor 제거(§6.2·§6.3).
    [Serializable]
    public class HeroStageVariant
    {
        //# 정적 3D 렌더 초상(HeroIcons/Knight.png) 틴트 전용 — hero-select/records UI(HeroSelectPopup·RecordsPopup)만 소비.
        //# 전투 스프라이트(Visual2D)에는 적용 안 됨(§6.2) — 구 TintColor 를 UI 전용 용도로 좁혀 이름 명확화(hero-2d-conversion §10 후속 아이콘 재설계 전까지 유지).
        public Color PortraitTintColor = Color.white;
        public bool UseEmission;
        public Color EmissionColor;
        public float EmissionIntensity;
        public float ScaleMultiplier = 1f;
        public float HpMultiplier = 1f;
        public float PowerMultiplier = 1f;
        //# 강화 오버레이 티어(0~4) — MonsterTierOverlay.SetTier 에 그대로 전달. 0=오버레이 off(스테이지1 몸만).
        public int Tier;
    }

    //# 5스테이지 영웅 재스킨 정본 SO. int(1~5) 로 조회, HeroStageVariantApplier 가 스폰 시 적용(spec §4).
    [CreateAssetMenu(fileName = "HeroStageVariantConfig", menuName = "Lair/HeroStageVariantConfig")]
    public class HeroStageVariantConfig : ScriptableObject
    {
        [SerializeField] private HeroStageVariant[] _stages;

        public IReadOnlyList<HeroStageVariant> Stages => _stages;

        //# 1-based 스테이지 번호를 실제 목록 크기로 클램프해 반환. 빈 목록이면 기본 variant(NRE 방지).
        public HeroStageVariant GetStage(int stage1Based)
        {
            if (_stages == null || _stages.Length == 0)
                return new HeroStageVariant();
            int index = Mathf.Clamp(stage1Based - 1, 0, _stages.Length - 1);
            return _stages[index];
        }
    }
}
