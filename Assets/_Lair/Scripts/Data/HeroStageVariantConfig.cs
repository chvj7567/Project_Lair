using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Data
{
    //# 스테이지 한 개의 외형 변형 + 스탯 배수 (hero-stage-variant 기획서 §1.2/§2.1). 필드명 = plan Task 2.
    [Serializable]
    public class HeroStageVariant
    {
        public Color TintColor = Color.white;
        public bool UseOutline;
        public Color OutlineColor;
        public bool UseEmission;
        public Color EmissionColor;
        public float EmissionIntensity;
        public float ScaleMultiplier = 1f;
        public float HpMultiplier = 1f;
        public float PowerMultiplier = 1f;
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
