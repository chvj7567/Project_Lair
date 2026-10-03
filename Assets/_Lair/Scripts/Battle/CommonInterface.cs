using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# Rule 10 — Battle 도메인의 공용 인터페이스 단일 파일.

    //# Spawner 가 스폰 요청을 위임하는 상위 호스트 계약 (Rule 06 — 자식은 부모를 인터페이스로 참조).
    //# BattleController 가 구현. 프리팹 로드·스탯 적용을 단일 경로에서 책임진다.
    public interface ISpawnerHost
    {
        //# Spawner 한 사이클 — exactPos 에 type 몬스터를 count 마리 스폰 (동시 캡 제거, spec §2.A).
        void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count);
    }

    //# Rule 10 — Spawner 쿨다운 진행도 노출 계약.
    //# SpawnerStatusCell 이 매 프레임 ISpawnerProgress.Progress 폴링 (기획서 §4.3 · §4.6).
    public interface ISpawnerProgress
    {
        //# 0~1. 초기 지연(firstSpawnDone==false) 국면에서는 0f 고정.
        float Progress { get; }

        //# 다음 스폰까지 남은 초. 초기 지연 국면이면 전체 주기.
        float RemainingSeconds { get; }
    }

    //# 스테이지 규칙(SealedNest) 봉인 상태 노출 계약 — 스포너 상태 UI 가 봉인 표기에 폴링. 소비자 UI 는 ISpawnerProgress 와 별도 타입 검사.
    public interface ISpawnerLockState
    {
        bool IsLocked { get; }
    }

    //# Rule 10 — Spawner 출력 종(EMonster) 변경 이벤트 + 동시 출력 수 노출 계약.
    //# SpawnerAltar2D 가 GetComponentInParent<ISpawnerOutputProvider>() 로 구독 (Rule 06).
    public interface ISpawnerOutputProvider
    {
        //# 현재 출력 중인 몬스터 종.
        EMonster CurrentType { get; }

        //# 동시 출력 수 — 기본 1, 추가소환 카드(IncrementSpawnerOutput)로 +1.
        int OutputCount { get; }

        //# ReplaceOutput 호출 시 또는 초기화(OnEnable) 시 발행.
        event System.Action<EMonster> OnOutputTypeChanged;

        //# IncrementOutput 호출 시 발행. OnEnable 시점엔 발행 안 함 (VM 이 초기값을 직접 폴링).
        event System.Action<int> OnOutputCountChanged;
    }

    //# Rule 02 §10 — 스포너 제단(SpawnerAltar2D) 루트 계약. 제단 색 = 종족 발광색(scene-2d-conversion §3.2).
    public interface ISpawnerAltar
    {
        //# 현재 제단 발광색 — 링·결정·반짝·조명에 적용 중인 색.
        Color GlowColor { get; }
    }

    //# ===== 스테이지 규칙 엔진 (chapter-system §3) =====

    //# 스테이지에 부착되는 전투 규칙 하나. 생명주기 = Activate → Tick(매 프레임) → Deactivate.
    //# 구현체는 구체 클래스가 아니라 IStageRuleContext 만 의존한다(테스트 더블 주입).
    public interface IStageRule
    {
        EStageRule Id { get; }

        //# Activate 후 Deactivate 전까지 true. 1회성 규칙(BossBulwark)은 만료되면 스스로 false.
        bool IsActive { get; }

        void Activate(IStageRuleContext ctx);

        //# elapsed = BattleClock.Elapsed(카드 선택 일시정지 중 정지), dt = 직전 Tick 이후 경과(항상 > 0).
        void Tick(float elapsed, float dt);

        //# 전투 종료/규칙 해제 — 부수효과 원복 + FX 풀 반환. 중복 호출 안전.
        void Deactivate();
    }

    //# 규칙이 띄운 시각 FX 한 개의 핸들. 구현은 CHMPool 로 풀링, 에셋이 없으면 생성 시 LogError.
    public interface IStageFx
    {
        void SetPosition(Vector3 position);

        //# 풀 반환. 로드 전이면 생성 취소. 중복 호출 안전.
        void Release();
    }

    //# 규칙이 전투에 가하는 모든 효과의 단일 seam (chapter-system §3.1 컨텍스트 인터페이스).
    //# 실구현 StageRuleContext, 테스트는 Fake 컨텍스트.
    public interface IStageRuleContext
    {
        //# 전장 원점 = 영웅 구역 중심 — 규칙의 centerX/centerZ 는 이 점 기준 오프셋.
        Vector3 ArenaCenter { get; }
        Vector3 HeroPosition { get; }
        int HeroMaxHp { get; }
        float HeroHpRatio { get; }

        void HealHero(int amount);

        //# 영웅 받는 피해 배율 곱연산 누적. 해제는 역수(1/mul)로 호출.
        void MultiplyHeroDamageTaken(float mul);

        //# 몬스터 공격 사거리 규칙 — 모든 사거리 보정 이후 마지막 곱. 필드 몬스터에 소급 적용.
        void SetMonsterRangeRule(float mul, float minRange);
        void ClearMonsterRangeRule();

        //# species 를 출력하는 스포너의 주기 스폰 봉인. 봉인된 스포너 위치 반환(0개면 빈 배열).
        Vector3[] LockSpawners(EMonster species);

        //# 봉인 해제 + 해당 스포너 즉시 1회 스폰.
        void UnlockSpawners(EMonster species);

        //# centers 각 구(XZ 반경 radius) union 안의 살아있는 교전 몬스터에 amount 피해(한 몬스터 1회). 피격 수 반환.
        int DamageMonstersInSpheres(IReadOnlyList<Vector3> centers, float radius, int amount);

        //# 영웅 중심 searchRadius 안에서 clusterRadius 안에 몬스터가 가장 많이 모인 지점. 몬스터 없으면 false.
        bool TryFindMonsterDensePoint(float searchRadius, float clusterRadius, out Vector3 point);

        //# centers 구 안 살아있는 몬스터 이동속도 배율 = scale, 밖으로 나가면 즉시 1 복구.
        void SetMonsterSpeedScaleInSpheres(IReadOnlyList<Vector3> centers, float radius, float scale);
        void ClearMonsterSpeedScale();

        //# 전장 안으로 클램프(덫 설치 범위 보정).
        Vector3 ClampToArena(Vector3 position);

        //# 풀링 FX 1개. 에셋 부재 시 조용히 건너뛰지 않고 LogError 후 빈 핸들 반환(null 아님).
        IStageFx ShowFx(EVisual key, Vector3 position, float scale);

        //# 알림 토스트 — stringId 는 Strings_Ko, args 는 string.Format 인자.
        void ShowNotice(int stringId, params object[] args);
    }

    //# 카메라 쉐이크 추상. 컷인이 BattleCamera 구체 대신 참조 → EditMode 모킹.
    public interface ICameraShake
    {
        //# duration 초 동안 magnitude(월드 유닛) 세기로 카메라를 흔든다. unscaled 진행.
        void Shake(float duration, float magnitude);
    }
}
