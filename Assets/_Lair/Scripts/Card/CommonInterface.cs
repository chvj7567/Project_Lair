using System;
using System.Collections.Generic;
using Lair.Character;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# Rule 10 — Card 도메인의 공용 인터페이스 단일 파일.

    //# 카드 효과 Strategy. SerializeReference 로 CardData 에 직렬화.
    public interface ICardEffect
    {
        void Apply(IBattleContext ctx);
    }

    //# 카드 효과 ↔ BattleController 표면 (Rule 06).
    public interface IBattleContext
    {
        IEnumerable<IHealth> GetMonsters(EMonster? filter = null);
        IHealth GetHero();
        Transform GetHeroTransform();

        //# 액티브 카드 — 영웅 슬로우 효과용. 영웅 Transform 의 IMover 컴포넌트 반환.
        IMover GetHeroMover();

        //# 동적 스폰 — "위스프 3마리 소환" 같은 카드용
        void SpawnMonster(EMonster key, Vector3 nearHero);

        //# 환경 카드 (예: 독 장판) — duration < 0 이면 무제한
        void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f);

        //# B3 — 몬스터 글로벌 버프 (광폭화/강철 의지/폭주)
        void AddMonsterBuff(EMonsterBuff type, float duration);

        //# B3 — 피의 갈증 활성화
        void ActivateBloodThirst(float duration);

        //# B3 — 폭주 즉발: 모든 몬스터 현재 HP 절반
        void HalveAllMonsterHp();

        //# 지속 스폰 강화 카드 — 종별 스탯 배율을 글로벌 dict 에 곱연산 누적 +
        //# 필드 동일 종 소급 적용 (resetCurrent:false). 강화 6장이 이 한 줄만 호출.
        void RegisterMonsterTypeBuff(EMonster type, EMonsterStatKind stat, float multiplier);

        //# 지속 스폰 추가소환 카드 — 해당 종을 출력 중인 모든 Spawner 의 동시 출력 +1.
        //# 매칭 Spawner 0개면 no-op.
        void IncrementSpawnerOutput(EMonster type);

        //# 지속 스폰 융합 카드 — 출력 종이 from 인 모든 Spawner 의 출력 종을 to 로 영구 변경.
        //# 매칭 Spawner 0개면 no-op.
        void ReplaceSpawnerOutput(EMonster from, EMonster to);

        //# 카드 리뉴얼 v0.6 — 카드 픽 시 호출. BuildSynergyService 에 카운트 등록 + 임계 도달 시 시너지 발화.
        //# 호출 시점: 패시브/액티브 카드 선택 직후, ICardEffect.Apply 직전 (BattleController.ApplyCardEffect).
        void RegisterCardPick(EBuildAxis axis);

        //# 카드 리뉴얼 v0.6 — 빌드 카운트 조회 (UI 카운트 바 / 시너지 카드 효과 내부 조건문에서 사용).
        int GetBuildCount(EBuildAxis axis);

        //# 카드 리뉴얼 v0.6 — Tank Tier3 표면.
        //# TODO: Phase 2 카드 효과에서 호출. 본 구현은 다음 사이클 (Task 11 — TankSynergyTier3).
        void IncrementGlobalMonsterCap(int delta);

        //# 카드 리뉴얼 v0.6 — Swarm Tier2 / SpawnerHaste 카드 공용 표면.
        //# TODO: Phase 2 카드 효과에서 호출. 본 구현은 다음 사이클 (Task 11 — SwarmSynergyTier2).
        void ScaleAllSpawnerPeriods(float mul);

        //# 카드 리뉴얼 v0.6 — Swarm Tier3 표면.
        //# TODO: Phase 2 카드 효과에서 호출. 본 구현은 다음 사이클 (Task 11 — SwarmSynergyTier3).
        void IncrementAllSpawnerOutputs(int delta);

        float DeltaTime { get; }
    }

    //# 영웅에 붙는 일시/영구 효과
    public interface IHeroAura
    {
        void OnAttached(IHealth hero);
        void Tick(IHealth hero, float dt);
        void OnDetached(IHealth hero);
    }

    //# 영웅 추적 상태 visual 을 노출하는 sibling 인터페이스 (IHeroAura 와 별개).
    //# HeroAuraRunner 가 이 값으로 visual 을 Pop/추적/Push.
    public interface IStatusVisual
    {
        EVisual VisualKey { get; }   //# CHMResource 로 로드할 프리팹 키
        Vector3 Offset { get; }      //# 영웅 위치 기준 상대 오프셋
    }

    //# 카드 리뉴얼 v0.6 [B3] — 동일 type Aura 가드 완화 marker.
    //# HeroAuraRunner.Attach 는 기본적으로 같은 type Aura 가 이미 부착돼 있으면 신규 인스턴스를 무시한다 (Fear·Bleed·Slow 등 single-instance 정책 보존).
    //# 단 IDistinctHeroAura 를 구현한 Aura 는 ShouldStackAsNew 가 true 를 반환하면 신규 인스턴스로 부착되어 OnAttached 가 다시 호출된다 — HeroAttackDownAura 의 factor 다른 중복 부착 시 PowerScale 곱연산 누적용.
    //# 구현체: HeroAttackDownAura (카드 픽 ×0.75 + Debuff Tier2 ×0.85 누적), MarkOfDeathAura 등 향후 factor 누적이 필요한 Aura.
    public interface IDistinctHeroAura
    {
        //# existing = 이미 부착되어 있는 같은 type 의 Aura. 비교 후 신규 인스턴스로 부착해야 하면 true.
        //# false 면 기존 가드 동작 (Remain 연장 + 신규 무시).
        bool ShouldStackAsNew(IHeroAura existing);
    }
}
