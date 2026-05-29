using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# 몬스터 프리팹에 부착되어 EMonster 값을 직렬화.
    //# BattleContext.GetMonsters(filter) 가 이를 통해 위스프/레이스/리퍼 구분.
    //# 풀 재사용 시 자기 Transform 의 IsEngaging 를 false 로 리셋 — Marching 상태 보장.
    public class MonsterTag : MonoBehaviour
    {
        [SerializeField] private EMonster _key;
        public EMonster Key => _key;

        //# 빌더 또는 런타임 동적 설정
        public void Configure(EMonster k) => _key = k;

        //# 풀 Pop 시 자동 호출. CharacterRegistry 등록은 MonsterTargetProvider 가 담당하므로
        //# 여기선 IsEngaging 만 리셋. 등록 안 된 상태에서 호출돼도 SetMonsterEngaging 가 no-op.
        private void OnEnable()
        {
            CharacterRegistry.SetMonsterEngaging(transform, false);
        }
    }
}
