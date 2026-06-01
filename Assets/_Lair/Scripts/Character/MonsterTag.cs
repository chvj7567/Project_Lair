using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 프리팹에 부착되어 EMonster 값을 직렬화.
    //# BattleContext.GetMonsters(filter) 가 이를 통해 위스프/레이스/리퍼 구분.
    public class MonsterTag : MonoBehaviour
    {
        [SerializeField] private EMonster _key;
        public EMonster Key => _key;

        private Rigidbody _rigidbody;

        //# 빌더 또는 런타임 동적 설정
        public void Configure(EMonster k) => _key = k;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        //# 풀 Pop 시 자동 호출. CharacterRegistry 등록은 MonsterTargetProvider 가 담당하므로
        //# 여기선 IsEngaging 만 리셋. 등록 안 된 상태에서 호출돼도 SetMonsterEngaging 가 no-op.
        //# 방식 B — 몬스터 전용 경로에서 kinematic 강제. 영웅(MonsterTag 없음)·테스트 객체(미부착) 무영향.
        //# 몬스터끼리 물리 충돌 제거 — 자유 겹침 허용. zone trigger 는 kinematic 에서도 발화(검증됨).
        private void OnEnable()
        {
            if (_rigidbody != null)
            {
                _rigidbody.isKinematic = true;
            }
            CharacterRegistry.SetMonsterEngaging(transform, false);
        }
    }
}
