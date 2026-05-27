using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# Rule 10 — Battle 도메인의 공용 인터페이스 단일 파일.

    //# Spawner 가 스폰 요청을 위임하는 상위 호스트 계약 (Rule 06 — 자식은 부모를 인터페이스로 참조).
    //# BattleController 가 구현. 캡 검사·프리팹 로드·스탯 적용을 단일 경로에서 책임진다.
    public interface ISpawnerHost
    {
        //# Spawner 한 사이클 — exactPos 에 type 몬스터를 count 마리 스폰.
        //# 캡(글로벌 15) 이상이면 사이클 전량 skip (§4.2/§4.3).
        void SpawnFromSpawner(EMonster type, Vector3 exactPos, int count);
    }

    //# Rule 10 — Spawner 쿨다운 진행도 노출 계약.
    //# SpawnerStatusCell 이 매 프레임 ISpawnerProgress.Progress 폴링 (기획서 §4.3 · §4.6).
    //# Spawner.cs 가 구현. 초기 지연 국면 = 0f, 주기 국면 = _timer / _spawnPeriod 클램프.
    public interface ISpawnerProgress
    {
        //# 0~1. 초기 지연(firstSpawnDone==false) 국면에서는 0f 고정.
        float Progress { get; }
    }

    //# Rule 10 — Spawner 출력 종(EMonster) 변경 이벤트 + 동시 출력 수 노출 계약.
    //# SpawnerBody 가 GetComponentInParent<ISpawnerOutputProvider>() 로 구독 (Rule 06).
    //# Spawner.cs 가 구현. ReplaceOutput 호출 시 + OnEnable 시 OnOutputTypeChanged 발행.
    //# IncrementOutput 호출 시 OnOutputCountChanged 발행 (OnEnable 시점에는 발행 안 함 — VM 폴링).
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
}
