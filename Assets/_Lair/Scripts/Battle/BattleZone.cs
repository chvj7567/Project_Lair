using System;
using Lair.Character;
using UnityEngine;

namespace Lair.Battle
{
    //# 씬 단일 인스턴스. 전장 경계(BoxCollider isTrigger) + hero entry 지점.
    //# 영웅 차단은 SimpleMover._clampZone 의 ClampInside 호출로 처리 (인비저블 벽 자동 생성 안 함 — design-reviewer B1).
    [RequireComponent(typeof(BoxCollider))]
    public class BattleZone : MonoBehaviour
    {
        //# 가시영역 안쪽 사각형. isTrigger=true. 본체 GameObject 에 직접 부착 — OnTriggerEnter 직수신.
        [SerializeField] private BoxCollider _zoneTrigger;
        //# 영웅 이동 클램프 정사각 반-extent (X/Z 공통). 교전 trigger 와 분리 — 클램프만 축소(기획서 §4.2).
        [SerializeField] private float _clampHalfExtent = 7.0f;
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

        //# 영웅 SimpleMover 가 매 FixedUpdate next 좌표 클램프에 사용. 교전 trigger 와 분리된 작은 정사각(Center ± _clampHalfExtent)으로.
        //# Y 평면 (X/Z) 만 클램프 — Y 는 입력 그대로 (SimpleMover 가 어차피 0 으로 고정).
        public Vector3 ClampInside(Vector3 worldPos)
        {
            Vector3 center = Center;
            float x = Mathf.Clamp(worldPos.x, center.x - _clampHalfExtent, center.x + _clampHalfExtent);
            float z = Mathf.Clamp(worldPos.z, center.z - _clampHalfExtent, center.z + _clampHalfExtent);
            return new Vector3(x, worldPos.y, z);
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

        //# 영웅 클램프 정사각(Center ± _clampHalfExtent)을 에디터에서 시각화 — 교전 trigger 와 구분(노란색).
        private void OnDrawGizmosSelected()
        {
            Vector3 center = _zoneTrigger != null ? _zoneTrigger.bounds.center : transform.position;
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(center, new Vector3(_clampHalfExtent * 2f, 1f, _clampHalfExtent * 2f));
        }
    }
}
