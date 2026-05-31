using System;
using Lair.Data;
using UnityEngine;

namespace Lair.Card
{
    //# 지속 스폰 — 레이스 종 최대 HP 글로벌 영구 버프 (×_hpMul).
    //# 카드 리뉴얼 v0.6 (2026-05-31) — 구 효과(Power ×1.5) 폐기, Tank 정체성에 맞춰 HP 강화로 리뉴얼.
    //# 이전 SO 의 `_mul` 필드는 FormerlySerializedAs 로 `_hpMul` 슬롯에 자동 이전 (둘 다 1.5 기본값).
    //# 클래스명 (WraithDamageBoostEffect) 은 SerializeReference type 안정성을 위해 유지 — displayName/description 으로 의미 갱신.
    [Serializable]
    public class WraithDamageBoostEffect : ICardEffect
    {
        [UnityEngine.Serialization.FormerlySerializedAs("_mul")]
        [SerializeField] private float _hpMul = 1.5f;

        public void Apply(IBattleContext ctx)
            => ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, _hpMul);
    }
}
