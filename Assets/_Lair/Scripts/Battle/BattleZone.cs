using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 씬 단일 인스턴스. 전장 경계(BoxCollider isTrigger) + spawn point pool + hero entry 지점.
    //# 영웅 차단은 SimpleMover._clampZone 의 ClampInside 호출로 처리 (인비저블 벽 자동 생성 안 함 — design-reviewer B1).
    [RequireComponent(typeof(BoxCollider))]
    public class BattleZone : MonoBehaviour
    {
        //# 가시영역 안쪽 사각형. isTrigger=true. 본체 GameObject 에 직접 부착 — OnTriggerEnter 직수신.
        [SerializeField] private BoxCollider _zoneTrigger;
        //# 4 edge 분산 spawn point. 기획서 §3.2 — 수동 배치 (zone 밖, 거리 = moveSpeed × 1.0초).
        [SerializeField] private Transform[] _spawnPoints;
        //# 영웅이 zone 진입 전 머무는 한 고정 위치 (zone 밖).
        [SerializeField] private Transform _heroEntryPoint;

        //# 영웅이 zone 중심 도달 시 1회 발행. BattleController 가 구독해 BattleClock + Spawner Tick 활성화.
        public event Action OnHeroReachedCenter;

        public Vector3 Center => _zoneTrigger != null ? _zoneTrigger.bounds.center : transform.position;
        public Transform HeroEntryPoint => _heroEntryPoint;

        //# bounds.Contains — XYZ 모든 축. 단순 사각형 판정.
        public bool IsInside(Vector3 worldPos)
        {
            if (_zoneTrigger == null) return false;
            return _zoneTrigger.bounds.Contains(worldPos);
        }

        //# 영웅 SimpleMover 가 매 FixedUpdate next 좌표 클램프에 사용. zone 밖이면 bounds 안쪽 가장자리로.
        //# Y 평면 (X/Z) 만 클램프 — Y 는 입력 그대로 (SimpleMover 가 어차피 0 으로 고정).
        public Vector3 ClampInside(Vector3 worldPos)
        {
            if (_zoneTrigger == null) return worldPos;
            Bounds b = _zoneTrigger.bounds;
            float x = Mathf.Clamp(worldPos.x, b.min.x, b.max.x);
            float z = Mathf.Clamp(worldPos.z, b.min.z, b.max.z);
            return new Vector3(x, worldPos.y, z);
        }

        //# _spawnPoints 가 비어있으면 null. 비-null 배열 안에서 균등 랜덤 픽.
        public Transform GetRandomSpawn()
        {
            if (_spawnPoints == null || _spawnPoints.Length == 0) return null;
            int idx = UnityEngine.Random.Range(0, _spawnPoints.Length);
            return _spawnPoints[idx];
        }

        //# HeroEntryDriver 가 Center 도달 시 호출 — 이벤트 발행.
        public void NotifyHeroReachedCenter()
        {
            OnHeroReachedCenter?.Invoke();
        }

        //# RequireComponent 보장 — Awake 시점에 BoxCollider 존재. _zoneTrigger 미할당이면 GetComponent 로 자동 픽업.
        private void Awake()
        {
            if (_zoneTrigger == null) _zoneTrigger = GetComponent<BoxCollider>();
        }

        //# zone 본체의 BoxCollider(isTrigger) 가 OnTriggerEnter 발행.
        //# MonsterTag 있는 Collider 만 Engaging 으로 전환. 영웅은 MonsterTag 가 없어 자동 무시.
        private void OnTriggerEnter(Collider other)
        {
            if (other == null) return;
            MonsterTag tag = other.GetComponent<MonsterTag>();
            if (tag == null) return;
            CharacterRegistry.SetMonsterEngaging(other.transform, true);
        }
    }
}
