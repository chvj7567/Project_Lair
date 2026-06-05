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
    public enum EMonster
    {
        Wisp,      //# 도깨비불 — 기본 잡몹 (구 Slime, 0)
        Wraith,    //# 망령 — 보스급 탱커 (구 Golem, 1)
        Reaper,    //# 사신 — 근접 광룡 (구 Orc, 2)
        Hex,       //# 저주술사 — 원거리 캐스터 (구 Archer, 3)
        Plague,    //# 역병귀 — 둔화 디버퍼 (구 Spider, 4)
        Phantom,   //# 환령 — 스웜 (구 Bat, 5)
    }

    //# CHMSound 채널 키. 값명 = 에셋 파일명(Bgm.mp3) 정확 일치(Rule 03 §2).
    //# None=0 은 CHMSound sentinel 로 AudioSource 미생성. Init<EAudio>(EAudio.Bgm) 으로 Bgm 만 loop 채널.
    public enum EAudio
    {
        None,
        Bgm,
    }

    //# CHMUI.ShowUI 로 UI 프리팹 로드.
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
        BuildModalPopup,       //# 스포너 상태 UI — BuildPanel 클릭 시 화면 중앙 모달
        SpawnerStatusTooltip,  //# (v0.6.4 폐기 — enum 자리 보존, int 직렬화 정합)
        SynergyModalPopup,     //# 시너지 패널 클릭 시 화면 중앙 모달 — 적용된 시너지 효과 목록
        SkillUnlockBanner,     //# 스킬 해금 컷인 배너 — 독립 팝업(빌더 생성)
    }

    //# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
    public enum EData
    {
        CardPool_Passive,
        CardPool_Active,    //# B2 신규
        Strings_Ko,         //# 게임 전체 CHText 문자열 — Art/Json/Strings_Ko.json
        LoadingStrings_Ko,  //# 로딩 설명 텍스트 — Art/Json/LoadingStrings_Ko.json
        HeroSkillLoadout,   //# 영웅 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout.asset (2026-06-04)
    }

    //# 카드 빌드 축 — 카드 리뉴얼(2026-05-31) 으로 구 카드 카테고리(4종 Enum) 를 대체.
    //# 순서 절대 변경 금지 — CardData._axis (int 직렬화) 와 1:1 대응.
    public enum EBuildAxis
    {
        Tank,    //# 탱커/포위 — Wisp + Wraith 중심
        Dps,     //# 순수 DPS — Reaper + Hex 중심
        Debuff,  //# 디버프 누적 — Plague + 액티브 저주 콤보 (둔화/속박 포함)
        Swarm,   //# 수적 압박 — Phantom 중심
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
        //# 영웅 디버프 상태 표시는 HP바 아래 아이콘 UI 로 교체(월드 visual 제거, 2026-06-04).
        //# 타격 피드백 (2026-06-01)
        HitImpact,    //# 피격 지점 프리미티브 버스트 파티클
        DamagePopup,  //# 부상+페이드 데미지 숫자 (월드스페이스 TMP+CHText)
        //# 영웅 스킬 FX (2026-06-04) — 프리미티브, CHMPool 대상.
        HeroDashFx,        //# 돌진 — 늘어난 큐브
        HeroOrbitBladeFx,  //# 회전 블레이드 — 궤도 큐브
        HeroNovaFx,        //# AOE 노바 — 팽창 반투명 실린더
        //# 가해자별 임팩트 분리 (2026-06-04) — 영웅이 몬스터를 때릴 때 전용 CFXR 임팩트. 맨 끝 추가(int 직렬화 정합).
        MonsterHitImpact,  //# 피격자=몬스터 전용 CFXR 임팩트 (피격자=영웅은 기존 HitImpact 유지)
        //# TimeStop 카드 발동 중 영웅을 감싸는 실드 FX (2026-06-05) — 맨 끝 추가(int 직렬화 정합).
        TimeStopShield,    //# TimeStopAura 가 5초 부착 동안 영웅 위치에 스폰, OnDetached 시 풀 반환
        //# Fear 카드 적용 순간 영웅 위에 띄우는 스컬 FX (2026-06-05) — 맨 끝 추가(int 직렬화 정합).
        FearSkull,         //# FearEffect 적용 시 1회성 스폰, ReturnToPoolAfter 가 자동 풀 반환
    }

    //# B3 신규 — 몬스터 글로벌 버프 종류 (MonsterBuffService 가 관리).
    //# 카드 리뉴얼 v0.6 — GuardianRage / SwarmSpeed / ToughHide 신규 추가.
    public enum EMonsterBuff
    {
        Frenzy,        //# 공격속도 ↑ (전체 종)
        IronWill,      //# 받는 데미지 ↓ (전체 종)
        BerserkPower,  //# 데미지 ↑ (전체 종) — v0.6 에서 Berserk 카드 폐기로 미사용, enum 자리 보존
        GuardianRage,  //# 카드 리뉴얼 v0.6 — Tank 한정 {Wisp, Wraith}: 받는 데미지 ×0.5
        SwarmSpeed,    //# 카드 리뉴얼 v0.6 — Slow 카드의 이중 효과: 모든 몬스터 이동속도 ×1.3 (시한)
        ToughHide,     //# 카드 리뉴얼 v0.6 — Tank 한정 {Wisp, Wraith}: 받는 데미지 ×0.75 영구 (단단한 살갗)
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

    //# 타격 피드백 피격자 종류 — DamageFeedback(Character) → HitFeedbackSpawner(Battle) 통신 계약.
    //# 임팩트 프리팹 분기 기준 (영웅=기존 HitImpact / 몬스터=MonsterHitImpact).
    public enum HitVictimKind
    {
        Hero,
        Monster,
    }
}
