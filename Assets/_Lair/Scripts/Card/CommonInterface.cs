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

        //# 동적 스폰 — "슬라임 3마리 소환" 같은 카드용
        void SpawnMonster(EMonster key, Vector3 nearHero);

        //# 환경 카드 (예: 독 장판) — duration < 0 이면 무제한
        void ApplyHeroAura(IHeroAura aura, float durationSeconds = -1f);

        //# B3 — 몬스터 글로벌 버프 (광폭화/강철 의지/폭주)
        void AddMonsterBuff(EMonsterBuff type, float duration);

        //# B3 — 피의 갈증 활성화
        void ActivateBloodThirst(float duration);

        //# B3 — 폭주 즉발: 모든 몬스터 현재 HP 절반
        void HalveAllMonsterHp();

        float DeltaTime { get; }
    }

    //# 영웅에 붙는 일시/영구 효과
    public interface IHeroAura
    {
        void OnAttached(IHealth hero);
        void Tick(IHealth hero, float dt);
        void OnDetached(IHealth hero);
    }
}
