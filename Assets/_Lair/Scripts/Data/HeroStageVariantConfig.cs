using System;
using System.Collections.Generic;
using Lair.Battle;
using UnityEngine;

namespace Lair.Data
{
    //# 스테이지 한 개의 외형 변형 (chapter-system §1.2 SO 변경, hero-stage-variant §1.2, hero-2d-conversion §11).
    //# 스탯 배수(HP/Power)는 Stage.json(StageRow)로 이전 — 이 SO 는 Object 참조·외형만 가진다(이중 진실 방지).
    //# 전투 몸 색/아웃라인은 2D 원화에 직접 베이크되어 전투 런타임 소비자가 없다 — UseOutline/OutlineColor 제거(§6.2·§6.3).
    [Serializable]
    public class HeroStageVariant
    {
        //# 엔트리 키 — (ChapterId, StageIndex). 선형 번호는 StageProgress.ToLinear 로 도출.
        public int ChapterId;
        public int StageIndex;
        //# 스테이지별 2D 영웅 초상(HeroIcons/Knight*.png, 시트 idle frame 0) — HeroSelectPopup·RecordsPopup 만 소비. 틴트 재활용 없음.
        //# Sprite 참조라 JSON 이 아닌 SO 에 둔다(Rule 02 §11 예외).
        public Sprite Portrait;
        public bool UseEmission;
        public Color EmissionColor;
        public float EmissionIntensity;
        public float ScaleMultiplier = 1f;
        //# 강화 오버레이 티어(0~4) — MonsterTierOverlay.SetTier 에 그대로 전달. 0=오버레이 off(스테이지1 몸만).
        public int Tier;
    }

    //# 챕터별 스테이지 영웅 재스킨 정본 SO (10엔트리: 1-1~1-5, 2-1~2-5). 선형 번호 또는 (챕터, 인덱스) 로 조회, HeroStageVariantApplier 가 스폰 시 적용.
    [CreateAssetMenu(fileName = "HeroStageVariantConfig", menuName = "Lair/HeroStageVariantConfig")]
    public class HeroStageVariantConfig : ScriptableObject
    {
        [SerializeField] private HeroStageVariant[] _stages;

        public IReadOnlyList<HeroStageVariant> Stages => _stages;

        //# 선형 스테이지 번호(1~)로 조회 — (챕터, 인덱스) 키가 일치하는 엔트리. 범위 밖은 가장 가까운 엔트리로 클램프, 빈 목록이면 기본 variant(NRE 방지).
        public HeroStageVariant GetStage(int stageLinear)
        {
            if (_stages == null || _stages.Length == 0)
                return new HeroStageVariant();

            //# 목표 이하 중 가장 큰 키를 고르고, 목표가 최소 키보다 작으면 최소 키 엔트리를 고른다(양방향 클램프).
            HeroStageVariant atOrBelow = null;
            int atOrBelowLinear = int.MinValue;
            HeroStageVariant lowest = null;
            int lowestLinear = int.MaxValue;
            foreach (HeroStageVariant variant in _stages)
            {
                if (variant == null)
                    continue;
                int linear = StageProgress.ToLinear(variant.ChapterId, variant.StageIndex);
                if (linear <= stageLinear && linear > atOrBelowLinear)
                {
                    atOrBelow = variant;
                    atOrBelowLinear = linear;
                }
                if (linear < lowestLinear)
                {
                    lowest = variant;
                    lowestLinear = linear;
                }
            }
            return atOrBelow ?? lowest ?? new HeroStageVariant();
        }

        //# (챕터, 인덱스) 로 조회 — 선형 번호 변환 후 GetStage 와 동일 규칙.
        public HeroStageVariant GetStage(int chapterId, int stageIndex)
        {
            return GetStage(StageProgress.ToLinear(chapterId, stageIndex));
        }
    }
}
