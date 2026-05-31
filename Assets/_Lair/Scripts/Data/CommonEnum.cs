namespace Lair.Data
{
    //# Rule 09 — 여러 시스템에서 참조되는 공용 Enum 단일 파일.
    //# Rule 08 — 값명은 에셋(프리팹/씬) 파일명과 정확히 일치해야 함.

    //# === Asset Keys (Rule 08) ===

    //# CHMResource 로 영웅 프리팹 로드.
    public enum EHero
    {
        Knight,
    }

    //# CHMResource 로 몬스터 프리팹 로드. LittleGhost 비주얼 테마(영혼/유령) 이름.
    //# 순서 절대 변경 금지 — BalanceConfig.MonsterStatRow.Key (int 직렬화) 와 1:1 대응.
    //# Wisp=0, Wraith=1, Reaper=2, Hex=3, Plague=4, Phantom=5.
    public enum EMonster
    {
        Wisp,      //# 도깨비불 — 기본 잡몹 (구 Slime, 0)
        Wraith,    //# 망령 — 보스급 탱커 (구 Golem, 1)
        Reaper,    //# 사신 — 근접 광룡 (구 Orc, 2)
        Hex,       //# 저주술사 — 원거리 캐스터 (구 Archer, 3)
        Plague,    //# 역병귀 — 둔화 디버퍼 (구 Spider, 4)
        Phantom,   //# 환령 — 스웜 (구 Bat, 5)
    }

    //# CHMUI.ShowUI 로 UI 프리팹 로드.
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
        BuildModalPopup,       //# 스포너 상태 UI — BuildPanel 클릭 시 화면 중앙 모달
        SpawnerStatusTooltip,  //# 스포너 상태 UI — 셀 클릭 시 셀 위 floating 툴팁
    }

    //# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
    public enum EData
    {
        CardPool_Passive,
        CardPool_Active,    //# B2 신규
        Strings_Ko,         //# 게임 전체 CHText 문자열 — Art/Json/Strings_Ko.json
        LoadingStrings_Ko,  //# 로딩 설명 텍스트 — Art/Json/LoadingStrings_Ko.json
    }

    //# 카드 빌드 축 — 카드 리뉴얼(2026-05-31) 으로 구 카드 카테고리(4종 Enum) 를 대체.
    //# 순서 절대 변경 금지 — CardData._axis (int 직렬화) 와 1:1 대응.
    //# Phase 1 마이그레이션 정책: 구 카테고리 0/1/2/3 자리에 그대로 1:1 치환 (값 위치 보존).
    //# 실제 카드의 의미적 축 매핑은 Phase 2 (SO 마이그레이션) 에서 game-designer 기획서대로 재할당.
    public enum EBuildAxis
    {
        Tank,    //# 탱커/포위 — Wisp + Wraith 중심
        Dps,     //# 순수 DPS — Reaper + Hex 중심
        Debuff,  //# 디버프 누적 — Plague + 액티브 저주 콤보 (둔화/속박 포함)
        Swarm,   //# 수적 압박 — Phantom 중심
    }

    //# 카드 식별자 — 카드 리뉴얼 v0.6 (2026-05-31) — 28장 (패시브 16 + 액티브 12).
    //# 종(種) 이름이 들어간 카드 ID 는 LittleGhost 테마로 동기화 (Wisp/Wraith/Reaper/Hex/Plague/Phantom).
    //# 순서 절대 변경 금지 — CardData._id (int 직렬화) 와 1:1 대응.
    //# Multiply (값 20) / Berserk (값 24) 는 enum 값명 보존, 효과/displayName 리뉴얼 (Berserk → GuardianRage, Multiply → SwarmRush 효과).
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
        //# SwarmRush (구 Multiply 자리 — 카드 리뉴얼 v0.6 으로 효과·displayName 교체, enum 값명만 보존)
        Multiply,                      //# (20) — Swarm A (효과 클래스 = SwarmRushEffect, displayName "스웜 러시")
        BloodThirst,                   //# (21) — Dps A (v0.6 Swarm→Dps 축 이동)
        IronWill,                      //# (22) — Tank A
        TimeStop,                      //# (23) — Swarm A
        //# GuardianRage (구 Berserk 자리 — 카드 리뉴얼 v0.6 으로 효과·displayName 교체, enum 값명만 보존)
        Berserk,                       //# (24) — Tank A (효과 클래스 = GuardianRageEffect)

        //# 카드 리뉴얼 v0.6 신규 3장 (값 25~27 — int 직렬화 정합).
        //# SwarmRush 는 별도 enum 값을 두지 않고 Multiply enum 자리(값 20) + SO 파일명 Multiply.asset 을 그대로 사용
        //# — Berserk → GuardianRage 패턴과 동일 (enum 값명·SO 파일명 보존, 효과/displayName 만 리뉴얼).
        WallOfWisps,                   //# (25) — Tank A
        MarkOfDeath,                   //# (26) — Dps A
        SpawnerHaste,                  //# (27) — Swarm P
    }

    //# SceneManager.LoadScene(EScene.X.ToString()).
    public enum EScene
    {
        Loading,   //# Build Settings index 0
        Battle,
    }

    //# B1 신규 — 시각 이펙트 프리팹 키 (Rule 12 — CHMPool 사용).
    public enum EVisual
    {
        PoisonAura,
        //# 영웅 디버프 상태 표시 (영웅 추적 부착물)
        SlowStatus,
        FearStatus,
        WeakenStatus,
        AttackDownStatus,
        TimeStopStatus,
        BleedStatus,
    }

    //# B3 신규 — 몬스터 글로벌 버프 종류 (MonsterBuffService 가 관리).
    //# 카드 리뉴얼 v0.6 — GuardianRage / SwarmSpeed 신규 추가.
    public enum EMonsterBuff
    {
        Frenzy,        //# 공격속도 ↑ (전체 종)
        IronWill,      //# 받는 데미지 ↓ (전체 종)
        BerserkPower,  //# 데미지 ↑ (전체 종) — v0.6 에서 Berserk 카드 폐기로 미사용, enum 자리 보존
        GuardianRage,  //# 카드 리뉴얼 v0.6 — Tank 한정 {Wisp, Wraith}: 받는 데미지 ×0.5
        SwarmSpeed,    //# 카드 리뉴얼 v0.6 — Slow 카드의 이중 효과: 모든 몬스터 이동속도 ×1.3 (시한)
    }

    //# 지속 스폰 — 강화 카드가 RegisterMonsterTypeBuff 호출 시 "어느 스탯 배율인지" 지정.
    //# 에셋 로드 키가 아닌 시스템 간 통신 계약. StatMultiplier 의 6개 필드와 1:1 대응.
    public enum EMonsterStatKind
    {
        Hp,
        Power,
        Cooldown,
        Range,
        MoveSpeed,
        SlowFactor,
    }

    //# === Cross-System Communication ===

    //# 전투 결과 — BattleStateModel / BattleViewModel / BattleController / ResultPopup 공용.
    public enum BattleResult
    {
        None,
        Win,
        Lose,
    }
}
