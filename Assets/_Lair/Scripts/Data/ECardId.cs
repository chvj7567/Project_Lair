namespace Lair.Data
{
    //# 카드 식별자 — 카드 리뉴얼 v0.6 (2026-05-31) — 28장 (패시브 16 + 액티브 12).
    //# 종(種) 이름이 들어간 카드 ID 는 LittleGhost 테마로 동기화 (Wisp/Wraith/Reaper/Hex/Plague/Phantom).
    //# Rule 02 §8 의도적 예외 — 이 enum 은 Lair > Card Editor 툴이 codegen 으로 관리하므로 CommonEnum.cs 에서 분리한다.
    //# 신규 값 추가는 툴의 [Enum 추가] 사용 권장. 순서/정수값 변경 금지 (CardData._id int 직렬화 정합).
    public enum ECardId
    {
        //# 패시브 15장 (값 0~14 보존 — v0.6 에서 일부는 축 이동 + 효과 리뉴얼)
        WispHpBoost,                   //# 구 SlimeHpBoost (0) — Tank P
        WraithDamageBoost,             //# 구 GolemDamageBoost (1) — Tank P (v0.6 효과 HP 로 리뉴얼)
        ReaperAtkSpeed,                //# 구 OrcAtkSpeed (2) — Dps P
        HexRangeBoost,                 //# 구 ArcherRangeBoost (3) — Dps P
        PlagueSlowBoost,               //# 구 SpiderSlowBoost (4) — Debuff P
        PhantomMoveSpeedBoost,         //# 구 BatMoveSpeedBoost (5) — Swarm P
        SpawnWisps,                    //# 구 SpawnSlimes (6) — Swarm P (v0.6 Tank→Swarm 축 이동)
        SpawnWraith,                   //# 구 SpawnGolem (7) — Tank P
        SpawnReapers,                  //# 구 SpawnOrcs (8) — Dps P
        SpawnPlagues,                  //# 구 SpawnSpiders (9) — Debuff P
        SpawnPhantoms,                 //# 구 SpawnBats (10) — Swarm P
        ReplaceWispsToWraith,          //# 구 ReplaceSlimesToGolem (11) — Tank P
        ReplaceReapersToHex,           //# 구 ReplaceOrcsToArchers (12) — Dps P
        HeroPoisonAura,                //# (13) — Debuff P
        HeroAttackDown,                //# (14) — Debuff P

        //# 액티브 10장 (값 15~24 보존)
        Fear,                          //# (15) — Debuff A
        Bleed,                         //# (16) — Debuff A
        Weaken,                        //# (17) — Debuff A
        Slow,                          //# (18) — Swarm A (v0.6 Debuff→Swarm 축 이동 + 효과 리뉴얼)
        Frenzy,                        //# (19) — Dps A
        //# 폐기 (카드 리뉴얼 v0.6 — SO/풀 ref 제거, enum 자리만 보존. 실제 효과는 FastBreedingEffect/"빠른 번식")
        Multiply,                      //# (20) — Swarm A (실제 SO: Multiply.asset / FastBreedingEffect, 팬텀 스포너 주기 ×0.6)
        BloodThirst,                   //# (21) — Dps A (v0.6 Swarm→Dps 축 이동)
        IronWill,                      //# (22) — Tank A
        TimeStop,                      //# (23) — Swarm A
        //# GuardianRage (구 Berserk 자리 — 카드 리뉴얼 v0.6 으로 효과·displayName 교체, enum 값명만 보존)
        Berserk,                       //# (24) — Tank A (효과 클래스 = GuardianRageEffect)

        //# 카드 리뉴얼 v0.6 신규 3장 (값 25~27 — int 직렬화 정합).
        WallOfWisps,                   //# (25) — Tank A
        MarkOfDeath,                   //# (26) — Dps A
        SpawnerHaste,                  //# (27) — Swarm P
        //# <card-editor:insert> — [Enum 추가] 가 신규 ID 를 이 줄 바로 위에 삽입한다. 이 줄을 삭제하지 말 것.
    }
}
