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
    }

    //# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
    public enum EData
    {
        CardPool_Passive,
        CardPool_Active,    //# B2 신규
    }

    //# B1 신규 — 카드 카테고리
    public enum ECardCategory
    {
        Enhance,        //# 강화
        Spawn,          //# 추가 소환
        Replace,        //# 교체
        Environment,    //# 환경 (영웅 디버프)
    }

    //# 카드 식별자 — 패시브 15장 + 액티브 10장.
    //# 종(種) 이름이 들어간 카드 ID 는 LittleGhost 테마로 동기화 (Wisp/Wraith/Reaper/Hex/Plague/Phantom).
    //# 액티브 10장은 종 비종속이라 이름 변경 없음.
    //# 순서 절대 변경 금지 — CardData._id (int 직렬화) 와 1:1 대응.
    public enum ECardId
    {
        //# 패시브 15장
        WispHpBoost,                   //# 구 SlimeHpBoost (0)
        WraithDamageBoost,             //# 구 GolemDamageBoost (1)
        ReaperAtkSpeed,                //# 구 OrcAtkSpeed (2)
        HexRangeBoost,                 //# 구 ArcherRangeBoost (3)
        PlagueSlowBoost,               //# 구 SpiderSlowBoost (4)
        PhantomMoveSpeedBoost,         //# 구 BatMoveSpeedBoost (5)
        SpawnWisps,                    //# 구 SpawnSlimes (6)
        SpawnWraith,                   //# 구 SpawnGolem (7)
        SpawnReapers,                  //# 구 SpawnOrcs (8)
        SpawnPlagues,                  //# 구 SpawnSpiders (9)
        SpawnPhantoms,                 //# 구 SpawnBats (10)
        ReplaceWispsToWraith,          //# 구 ReplaceSlimesToGolem (11)
        ReplaceReapersToHex,           //# 구 ReplaceOrcsToArchers (12)
        HeroPoisonAura,                //# (13)
        HeroAttackDown,                //# (14)

        //# 액티브 10장 — 종 비종속이라 이름 그대로
        Fear,
        Bleed,
        Weaken,
        Slow,
        Frenzy,
        Multiply,
        BloodThirst,
        IronWill,
        TimeStop,
        Berserk,
    }

    //# SceneManager.LoadScene(EScene.X.ToString()).
    public enum EScene
    {
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
    public enum EMonsterBuff
    {
        Frenzy,        //# 공격속도 ↑
        IronWill,      //# 받는 데미지 ↓
        BerserkPower,  //# 데미지 ↑
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
