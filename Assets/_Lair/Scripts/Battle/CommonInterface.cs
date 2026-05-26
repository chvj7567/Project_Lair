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
}
