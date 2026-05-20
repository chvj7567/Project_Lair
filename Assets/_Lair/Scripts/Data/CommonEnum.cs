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

    //# CHMResource 로 몬스터 프리팹 로드.
    public enum EMonster
    {
        Slime,
        Golem,
        Orc,
        Archer,    //# B3 신규 — 원거리
        Spider,    //# B3 신규 — 공격 시 영웅 둔화
        Bat,       //# B3 신규 — 저비용 다수
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

    //# 카드 식별자 — 패시브 15장 + 액티브 10장 (B3 에서 최종 재정의).
    public enum ECardId
    {
        //# 패시브 15장
        SlimeHpBoost,
        GolemDamageBoost,
        OrcAtkSpeed,
        ArcherRangeBoost,
        SpiderSlowBoost,
        BatMoveSpeedBoost,
        SpawnSlimes,
        SpawnGolem,
        SpawnOrcs,
        SpawnSpiders,
        SpawnBats,
        ReplaceSlimesToGolem,
        ReplaceOrcsToArchers,
        HeroPoisonAura,
        HeroAttackDown,

        //# 액티브 10장
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

    //# === Cross-System Communication ===

    //# 전투 결과 — BattleStateModel / BattleViewModel / BattleController / ResultPopup 공용.
    public enum BattleResult
    {
        None,
        Win,
        Lose,
    }
}
