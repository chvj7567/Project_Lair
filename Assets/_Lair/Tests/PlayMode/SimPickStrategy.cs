using System.Collections.Generic;
using Lair.Battle;
using Lair.Card;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# 시뮬레이션 인프라 — 카드 픽 전략 모음. 게임 로직 아님(테스트 전용).
    //# BattleController.DebugAutoPicker 델리게이트 시그니처에 맞춘 Func 5종을 제공.
    //# delegate: (제시된 3장 choices, 트리거 출처 source) -> 고른 CardData (null 이면 스킵).
    //#
    //# 카드 리뉴얼 v0.6 (2026-05-31, Phase 3 Task 16) 갱신:
    //# - 기존 4 전략(AoEPriority / DealerPriority / Random / TankerPriority) → 4축 우선 픽 전략으로 갱신.
    //# - 5 전략: TankAxisPriority / DpsAxisPriority / DebuffAxisPriority / SwarmAxisPriority / Random.
    //# - 픽 로직: choices.Where(c => c.Axis == targetAxis).FirstOrDefault() ?? choices[0].
    //# - 기존 archetype 매핑 dict 은 회귀 안전을 위해 보존(SwarmRushEffect 의미로 Multiply 유지).

    //# 5종 픽 전략 식별자. 캠페인 실행 시 전략별로 N판씩 돌린다.
    public enum ESimStrategy
    {
        Random,              //# 무작위 픽
        TankAxisPriority,    //# Tank 축 카드 우선 (Wisp·Wraith 묶어두기)
        DpsAxisPriority,     //# Dps 축 카드 우선 (Reaper·Hex 깡딜)
        DebuffAxisPriority,  //# Debuff 축 카드 우선 (Plague + 액티브 저주)
        SwarmAxisPriority,   //# Swarm 축 카드 우선 (Phantom + 머릿수)
    }

    //# 카드 1장의 시뮬레이션용 분류(legacy). 카드 리뉴얼 v0.6 이후 4축 전략은 CardData.Axis 를 직접 사용하지만,
    //# 본 archetype 매핑 dict 은 기존 시뮬·로깅 회귀 안전을 위해 보존한다 (PlayMode 시뮬레이션 전용 분류).
    public enum ECardArchetype
    {
        Tanker,    //# 위스프/레이스(탱커) 관련 카드
        Dealer,    //# 리퍼/헥스(딜러) 관련 카드
        Aoe,       //# 광역 성격 카드 (다수 소환/증식/광역 회복 등)
        Other,     //# 위 어디에도 강하게 속하지 않는 카드 (유틸·영웅 디버프 등)
    }

    public static class SimPickStrategy
    {
        //# ECardId -> archetype 매핑 (legacy, 회귀 안전용 보존).
        //# 컨셉 §11.3 기준 탱커/딜러/유틸/광역 분류 — 카드 리뉴얼 v0.6 의 4축(EBuildAxis) 과는 별개.
        //# 4축 전략은 CardData.Axis 를 직접 참조하므로 본 dict 은 시뮬 로깅·기존 테스트 호환용.
        private static readonly Dictionary<ECardId, ECardArchetype> _archetypes = new()
        {
            //# === 패시브 16장 ===
            { ECardId.WispHpBoost,             ECardArchetype.Tanker }, //# 위스프 HP 강화
            { ECardId.WraithDamageBoost,       ECardArchetype.Tanker }, //# 레이스 HP 강화 (v0.6 효과 데미지→HP 리뉴얼)
            { ECardId.ReaperAtkSpeed,          ECardArchetype.Dealer }, //# 리퍼 공속 강화
            { ECardId.HexRangeBoost,           ECardArchetype.Dealer }, //# 헥스 사거리 강화
            { ECardId.PlagueSlowBoost,         ECardArchetype.Other  }, //# 플레이그 둔화 강화(유틸)
            { ECardId.PhantomMoveSpeedBoost,   ECardArchetype.Other  }, //# 팬텀 이속 강화(유틸)
            { ECardId.SpawnWisps,              ECardArchetype.Aoe    }, //# 위스프 출력 +1(다수, v0.6 Swarm 축)
            { ECardId.SpawnWraith,             ECardArchetype.Tanker }, //# 레이스 출력 +1
            { ECardId.SpawnReapers,            ECardArchetype.Dealer }, //# 리퍼 출력 +1
            { ECardId.SpawnPlagues,            ECardArchetype.Other  }, //# 플레이그 출력 +1(유틸)
            { ECardId.SpawnPhantoms,           ECardArchetype.Aoe    }, //# 팬텀 출력 +1(다수)
            { ECardId.ReplaceWispsToWraith,    ECardArchetype.Tanker }, //# 위스프 -> 레이스
            { ECardId.ReplaceReapersToHex,     ECardArchetype.Dealer }, //# 리퍼 -> 헥스
            { ECardId.HeroPoisonAura,          ECardArchetype.Other  }, //# 영웅 독 장판(환경)
            { ECardId.HeroAttackDown,          ECardArchetype.Other  }, //# 영웅 공격력 감소(환경)
            { ECardId.SpawnerHaste,            ECardArchetype.Aoe    }, //# v0.6 신규 — 모든 스포너 주기 ×0.8 (Swarm P)

            //# === 액티브 12장 ===
            { ECardId.Fear,        ECardArchetype.Other  }, //# 영웅 도망(저주)
            { ECardId.Bleed,       ECardArchetype.Other  }, //# 영웅 출혈(저주)
            { ECardId.Weaken,      ECardArchetype.Other  }, //# 영웅 데미지 감소(저주)
            { ECardId.Slow,        ECardArchetype.Other  }, //# 영웅 둔화 + 몬스터 가속 (v0.6 Swarm 축 이중 효과)
            { ECardId.Frenzy,      ECardArchetype.Aoe    }, //# 전 몬스터 공속 +50%(전체 버프)
            //# 카드 리뉴얼 v0.6 — Multiply enum 자리는 보존, 효과는 SwarmRushEffect (팬텀 6마리 즉시 소환) 로 교체.
            //# 신규 효과도 광역 다수 소환 → Aoe 분류 그대로 정합 (구 Multiply 의 "최다 종 2배" 와 동일한 광역 성격).
            { ECardId.Multiply,    ECardArchetype.Aoe    }, //# 팬텀 6마리 즉시 소환(SwarmRush 효과)
            { ECardId.BloodThirst, ECardArchetype.Aoe    }, //# 처치 시 주변 회복(광역, v0.6 Dps 축)
            { ECardId.IronWill,    ECardArchetype.Other  }, //# 전 몬스터 받는 데미지 -30%
            { ECardId.TimeStop,    ECardArchetype.Other  }, //# 영웅 정지(와일드)
            //# 카드 리뉴얼 v0.6 — Berserk enum 자리는 보존, 효과는 GuardianRageEffect (Wisp·Wraith 한정 HP×2 + 받는데미지×0.5) 로 교체.
            { ECardId.Berserk,     ECardArchetype.Tanker }, //# v0.6 GuardianRage — Tank 보호 액티브 (자살 구조 폐기)
            { ECardId.WallOfWisps, ECardArchetype.Tanker }, //# v0.6 신규 — 영웅 주변 Wisp 4마리 즉시 소환 (Tank A)
            { ECardId.MarkOfDeath, ECardArchetype.Other  }, //# v0.6 신규 — 영웅 받는 데미지 ×1.5 5s (Dps A 보조)
        };

        //# 카드 1장의 archetype 조회. 미등록이면 Other.
        public static ECardArchetype ArchetypeOf(CardData card)
        {
            if (card == null) return ECardArchetype.Other;
            return _archetypes.TryGetValue(card.Id, out ECardArchetype a) ? a : ECardArchetype.Other;
        }

        //# 전략 enum -> 픽 함수. BattleController.DebugAutoPicker 에 그대로 대입 가능.
        //# 카드 리뉴얼 v0.6 (Phase 3 Task 16) — 4축 전략 4종 + Random.
        public static System.Func<IReadOnlyList<CardData>, TriggerQueue.Source, CardData> Get(ESimStrategy strategy)
        {
            return strategy switch
            {
                ESimStrategy.TankAxisPriority   => MakeAxisPicker(EBuildAxis.Tank),
                ESimStrategy.DpsAxisPriority    => MakeAxisPicker(EBuildAxis.Dps),
                ESimStrategy.DebuffAxisPriority => MakeAxisPicker(EBuildAxis.Debuff),
                ESimStrategy.SwarmAxisPriority  => MakeAxisPicker(EBuildAxis.Swarm),
                _                              => RandomPick,
            };
        }

        //# 무작위 픽 — 제시된 3장 중 균등 확률 1장. choices 비면 null.
        private static CardData RandomPick(IReadOnlyList<CardData> choices, TriggerQueue.Source source)
        {
            if (choices == null || choices.Count == 0) return null;
            //# 시뮬레이션 전용 RNG — 결정성 불요(여러 판 분산 확보 목적).
            return choices[UnityEngine.Random.Range(0, choices.Count)];
        }

        //# 특정 빌드 축 우선 픽: 우선 축이 있으면 그중 첫 장, 없으면 choices 의 첫 장으로 폴백 (스킵하지 않음).
        //# 카드 리뉴얼 v0.6 — CardData.Axis (EBuildAxis) 를 직접 사용.
        private static System.Func<IReadOnlyList<CardData>, TriggerQueue.Source, CardData>
            MakeAxisPicker(EBuildAxis preferred)
        {
            return (choices, source) =>
            {
                if (choices == null || choices.Count == 0) return null;
                foreach (CardData c in choices)
                {
                    if (c != null && c.Axis == preferred) return c;
                }
                //# 우선 축 부재 — 제시된 첫 장으로 폴백.
                return choices[0];
            };
        }
    }
}
