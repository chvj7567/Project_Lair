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

        //# 빌더 또는 런타임 동적 설정
        public void Configure(EMonster k) => _key = k;
    }
}
