using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 시뮬레이션 인프라 — 카드 픽 전략 모음. 게임 로직 아님(테스트 전용).
    //# BattleController.DebugAutoPicker 델리게이트 시그니처에 맞춘 Func 4종을 제공.
    //# delegate: (제시된 3장 choices, 트리거 출처 source) -> 고른 CardData (null 이면 스킵).

    //# 4종 픽 전략 식별자. 캠페인 실행 시 전략별로 N판씩 돌린다.
    public enum ESimStrategy
    {
        Random,           //# 무작위 픽
        TankerPriority,   //# 탱커 강화/소환 우선
        DealerPriority,   //# 딜러 강화/소환 우선
        AoEPriority,      //# 광역(AoE)성 카드 우선
    }

    //# 카드 1장의 시뮬레이션용 분류. CardData.Category(Enhance/Spawn/Replace/Environment)는
    //# 탱커/딜러/AoE 축이 아니므로, ECardId 별 archetype 을 시뮬 코드에서 직접 매핑한다(브리프 허용).
    public enum ECardArchetype
    {
        Tanker,    //# 슬라임/골렘(탱커) 관련 카드
        Dealer,    //# 오크/궁수(딜러) 관련 카드
        Aoe,       //# 광역 성격 카드 (다수 소환/증식/광역 회복 등)
        Other,     //# 위 어디에도 강하게 속하지 않는 카드 (유틸·영웅 디버프 등)
    }

    public static class SimPickStrategy
    {
        //# ECardId -> archetype 매핑. 컨셉 §11.3 기준:
        //#   탱커 = 슬라임/골렘, 딜러 = 오크/궁수, 유틸 = 거미/박쥐.
        //#   다수 소환·증식은 광역(AoE) 성격으로 분류.
        private static readonly Dictionary<ECardId, ECardArchetype> _archetypes = new()
        {
            //# === 패시브 15장 ===
            { ECardId.SlimeHpBoost,          ECardArchetype.Tanker }, //# 슬라임 HP 강화
            { ECardId.GolemDamageBoost,      ECardArchetype.Tanker }, //# 골렘 데미지 강화
            { ECardId.OrcAtkSpeed,           ECardArchetype.Dealer }, //# 오크 공속 강화
            { ECardId.ArcherRangeBoost,      ECardArchetype.Dealer }, //# 궁수 사거리 강화
            { ECardId.SpiderSlowBoost,       ECardArchetype.Other  }, //# 거미 둔화 강화(유틸)
            { ECardId.BatMoveSpeedBoost,     ECardArchetype.Other  }, //# 박쥐 이속 강화(유틸)
            { ECardId.SpawnSlimes,           ECardArchetype.Aoe    }, //# 슬라임 3마리 소환(다수)
            { ECardId.SpawnGolem,            ECardArchetype.Tanker }, //# 골렘 1마리 소환
            { ECardId.SpawnOrcs,             ECardArchetype.Dealer }, //# 오크 2마리 소환
            { ECardId.SpawnSpiders,          ECardArchetype.Other  }, //# 거미 2마리 소환(유틸)
            { ECardId.SpawnBats,             ECardArchetype.Aoe    }, //# 박쥐 5마리 소환(다수)
            { ECardId.ReplaceSlimesToGolem,  ECardArchetype.Tanker }, //# 슬라임 전부 -> 골렘
            { ECardId.ReplaceOrcsToArchers,  ECardArchetype.Dealer }, //# 오크 전부 -> 궁수
            { ECardId.HeroPoisonAura,        ECardArchetype.Other  }, //# 영웅 독 장판(환경)
            { ECardId.HeroAttackDown,        ECardArchetype.Other  }, //# 영웅 공격력 감소(환경)

            //# === 액티브 10장 ===
            { ECardId.Fear,        ECardArchetype.Other  }, //# 영웅 도망(저주)
            { ECardId.Bleed,       ECardArchetype.Other  }, //# 영웅 출혈(저주)
            { ECardId.Weaken,      ECardArchetype.Other  }, //# 영웅 데미지 감소(저주)
            { ECardId.Slow,        ECardArchetype.Other  }, //# 영웅 둔화(저주)
            { ECardId.Frenzy,      ECardArchetype.Aoe    }, //# 전 몬스터 공속 +50%(전체 버프)
            { ECardId.Multiply,    ECardArchetype.Aoe    }, //# 가장 많은 몬스터 2배(다수)
            { ECardId.BloodThirst, ECardArchetype.Aoe    }, //# 처치 시 주변 회복(광역)
            { ECardId.IronWill,    ECardArchetype.Other  }, //# 전 몬스터 받는 데미지 -30%
            { ECardId.TimeStop,    ECardArchetype.Other  }, //# 영웅 정지(와일드)
            { ECardId.Berserk,     ECardArchetype.Other  }, //# 전 몬스터 HP-50% 데미지+200%
        };

        //# 카드 1장의 archetype 조회. 미등록이면 Other.
        public static ECardArchetype ArchetypeOf(CardData card)
        {
            if (card == null) return ECardArchetype.Other;
            return _archetypes.TryGetValue(card.Id, out var a) ? a : ECardArchetype.Other;
        }

        //# 전략 enum -> 픽 함수. BattleController.DebugAutoPicker 에 그대로 대입 가능.
        public static System.Func<IReadOnlyList<CardData>, TriggerQueue.Source, CardData> Get(ESimStrategy strategy)
        {
            return strategy switch
            {
                ESimStrategy.TankerPriority => MakePriorityPicker(ECardArchetype.Tanker),
                ESimStrategy.DealerPriority => MakePriorityPicker(ECardArchetype.Dealer),
                ESimStrategy.AoEPriority    => MakePriorityPicker(ECardArchetype.Aoe),
                _                          => RandomPick,
            };
        }

        //# 무작위 픽 — 제시된 3장 중 균등 확률 1장. choices 비면 null.
        private static CardData RandomPick(IReadOnlyList<CardData> choices, TriggerQueue.Source source)
        {
            if (choices == null || choices.Count == 0) return null;
            //# 시뮬레이션 전용 RNG — 결정성 불요(여러 판 분산 확보 목적).
            return choices[UnityEngine.Random.Range(0, choices.Count)];
        }

        //# 특정 archetype 우선 픽: 우선 archetype 이 있으면 그중 첫 장, 없으면 첫 장.
        private static System.Func<IReadOnlyList<CardData>, TriggerQueue.Source, CardData>
            MakePriorityPicker(ECardArchetype preferred)
        {
            return (choices, source) =>
            {
                if (choices == null || choices.Count == 0) return null;
                foreach (var c in choices)
                {
                    if (ArchetypeOf(c) == preferred) return c;
                }
                //# 우선 archetype 부재 — 제시된 첫 장으로 폴백(스킵하지 않음).
                return choices[0];
            };
        }
    }
}
