namespace Lair.Data
{
    //# Rule 09 — 여러 시스템에서 참조되는 공용 Enum 단일 파일.
    //# Rule 08 — 값명은 에셋(프리팹/씬) 파일명과 정확히 일치해야 함.

    //# === Asset Keys (Rule 08) ===

    //# CHMResource 로 영웅 프리팹 로드.
    public enum EHero
    {
        Knight,
        Archer,   //# 챕터 2 영웅 직업 (chapter-system §5) — 값명 = HeroArchetype.json heroId
        Assassin, //# 챕터 3 영웅 직업 (chapter-3-hero §3) — 값명 = HeroArchetype.json heroId
        Berserker, //# 챕터 4 영웅 직업 (chapter-4 §3) — 값명 = HeroArchetype.json heroId
        Paladin,   //# 챕터 5 영웅 직업 (chapter-5 §3) — 값명 = HeroArchetype.json heroId
    }

    //# 영웅 AI 종류 — HeroArchetype.json aiType 값명 (chapter-system §1.2).
    public enum EHeroAi
    {
        Melee,
        Kiter,
        Skirmisher,   //# 습격-이탈 AI (chapter-3-hero §3.3)
        Charger,      //# 군집 돌진 AI (chapter-4 §3.3)
        Guardian,     //# 응징 표적 AI (chapter-5 §3.3)
    }

    //# 스테이지 규칙 종류 — Stage_Rule.json ruleId 값명 (chapter-system §3.3). 규칙 실행 코드는 슬라이스 (b).
    public enum EStageRule
    {
        HolySpring,
        DenseFog,
        SealedNest,
        BossBulwark,
        ArrowRain,
        HunterTraps,
    }

    //# 챕터 배경 에셋 키 — Chapter.json backdropKey 값명 (chapter-system §7.1). 에셋 등록·교체는 슬라이스 (c)/(d).
    public enum EBackdrop
    {
        Battle_Backdrop,
        Battle_Backdrop_DarkForest,
        Battle_Backdrop_ShadowCitadel,   //# 챕터 3 — 에셋은 슬라이스 (c)
        Battle_Backdrop_EmberForge,      //# 챕터 4 — 에셋은 슬라이스 (c)
        Battle_Backdrop_FrostCathedral,  //# 챕터 5 — 에셋은 슬라이스 (c)
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
    //# None=0 은 CHMSound sentinel 로 AudioSource 미생성. Init bgmKeys(Bgm·VillageBgm) 만 loop 채널.
    public enum EAudio
    {
        None,
        Bgm,
        //# 효과음 8종 — 값명 = 에셋 파일명(확장자 제외) 정확 일치(Rule 03 §2). Init 이 값마다 PlayOneShot 채널 자동 생성.
        Slash01,       //# 영웅 기본공격 스윙 variant 0
        Slash02,       //# 영웅 기본공격 스윙 variant 1
        Stab,          //# 영웅 기본공격 스윙 variant 2
        Hit,           //# 근접 데미지 임팩트 전반
        P1Skill,       //# 영웅 스킬 DashStrike(P1/HP90%) 시전 — 매 쿨다운 발동
        P2Skill,       //# 영웅 스킬 AoeNova(쿨다운형) 시전 — 매 쿨다운 발동
        P3Skill,       //# 영웅 스킬 OrbitingBlade(공전 블레이드, 지속형) 활성화 1회
        AcquireSkill,  //# 영웅 스킬 해금 컷인
        CardSelect,    //# 카드 3택1 선택 확정
        VillageBgm,    //# 마을(Village) 씬 루프 BGM — append-only (int 직렬화 정합)
        RelicAcquire,  //# 결과창 유물 획득 연출 1회 (relics §6.5) — 맨 끝 append
        RelicThunder,  //# 뇌운 조각 낙뢰 타격 1회 (relics §6.5)
    }

    //# CHMUI.ShowUI 로 UI 프리팹 로드.
    //# 순서 절대 변경 금지 — 신규 값은 반드시 맨 끝 append (int 직렬화 정합).
    public enum EUI
    {
        BattleHud,
        ResultPopup,
        CardSelectionPopup,    //# B1 신규
        BuildModalPopup,       //# 스포너 상태 UI — BuildPanel 클릭 시 화면 중앙 모달
        SpawnerStatusTooltip,  //# (v0.6.4 폐기 — enum 자리 보존, int 직렬화 정합)
        SynergyModalPopup,     //# 시너지 패널 클릭 시 화면 중앙 모달 — 적용된 시너지 효과 목록
        SkillUnlockBanner,     //# 스킬 해금 컷인 배너 — 독립 팝업(빌더 생성)
        //# v0.2 마을 허브 — 7종 (기획서 village-meta-hub.md §11.1). 값명 = 프리팹 파일명 (Rule 03 §2).
        VillageHud,            //# 마을 상단바/좌우 메뉴/출격
        ShopPopup,             //# 소울 상점
        QuestPopup,            //# 고정 도전과제
        CodexPopup,            //# 도감 (몬스터/카드 탭)
        RecordsPopup,          //# 전적 기록
        HeroSelectPopup,       //# 영웅 선택
        LordLevelPopup,        //# 영주 레벨 보상 트랙
        //# v0.3 서버 연동 — 맨 끝 append (int 직렬화 정합, 순서 변경 금지). (2026-06-15)
        RankingPopup,          //# 최단클리어 랭킹 조회
        CloudPopup,            //# 클라우드 — 복원·표시명·충돌 권유 통합
        ConfirmPopup,          //# 공용 2버튼 확인 다이얼로그 (복원·충돌 재사용)
        ToastView,             //# 공용 토스트 (하단중앙 1.8초 페이드, 입력통과)
        //# 챕터 시스템 — 맨 끝 append. 월드맵 팝업(HeroSelectPopup 대체, chapter-system §6). 값명 = 프리팹 파일명.
        WorldMapPopup,         //# 침입 지도 — 챕터 탭 + 스테이지 노드 + 상세
        RelicPopup,            //# 유물 도감 — 마을 HUD 버튼 (relics §7.2). 값명 = 프리팹 파일명.
        FormationPopup,        //# 던전 편성 — 마을 "방어하기" 가 여는 출격 전 편성 팝업 (dungeon-formation §7). 값명 = 프리팹 파일명.
        TowerPopup,            //# 무한의 탑 — 마을 하단 "탑" 버튼이 여는 팝업 (infinite-tower §6.2). 값명 = 프리팹 파일명.
        DailyPopup,            //# 데일리(일일 도전) — 마을 HUD 데일리 타일이 여는 팝업 (daily-challenge §9.2). 값명 = 프리팹 파일명.
    }

    //# B1 신규 — 데이터 SO 로드 키 (예: CardPool)
    public enum EData
    {
        CardPool_Passive,
        CardPool_Active,    //# B2 신규
        Strings_Ko,         //# 게임 전체 CHText 문자열 — Data/Json/Strings_Ko.json
        LoadingStrings_Ko,  //# 로딩 설명 텍스트 — Data/Json/LoadingStrings_Ko.json
        HeroSkillLoadout,   //# 영웅 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout.asset (2026-06-04)
        SynergyVisualConfig, //# 시너지 축 아이콘 SO — Art/Synergy/SynergyVisualConfig.asset
        //# 챕터 시스템 JSON 7종 (chapter-system §1·§7.3) — 값명 = Data/Json/<값명>.json 파일명. append-only.
        Chapter,
        Stage,
        Stage_Rule,
        Stage_Rule_Param,
        Stage_Rule_Trigger,
        HeroArchetype,
        HeroArchetype_Kite,
        HeroSkillLoadout_Archer,   //# 궁수 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout_Archer.asset (chapter-system §1.4)
        HeroArchetype_Skirmish,    //# 암살자 습격-이탈 AI 튜닝 JSON — Data/Json/HeroArchetype_Skirmish.json (chapter-3-hero §2)
        HeroSkillLoadout_Assassin, //# 암살자 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout_Assassin.asset (에셋은 슬라이스 (b))
        HeroArchetype_Charge,      //# 광전사 군집 돌진 AI 튜닝 JSON — Data/Json/HeroArchetype_Charge.json (chapter-4 §2)
        HeroArchetype_Cleave,      //# 광전사 범위 베기 설정 JSON — Data/Json/HeroArchetype_Cleave.json (chapter-4 §2)
        HeroSkillLoadout_Berserker, //# 광전사 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout_Berserker.asset (에셋은 슬라이스 (b))
        HeroArchetype_Guard,        //# 성기사 응징 표적 AI 튜닝 JSON — Data/Json/HeroArchetype_Guard.json (chapter-5 §2)
        HeroArchetype_DebuffResist, //# 성기사 디버프 저항 패시브 JSON — Data/Json/HeroArchetype_DebuffResist.json (chapter-5 §2)
        HeroSkillLoadout_Paladin,   //# 성기사 스킬 로드아웃 SO — Art/Skills/HeroSkillLoadout_Paladin.asset (에셋은 슬라이스 (b))
        Difficulty,                 //# 난이도 배율표 JSON — Data/Json/Difficulty.json (difficulty-select §2)
        //# 유물 2차 JSON 10종 (relics §3) — 값명 = Data/Json/<값명>.json 파일명. append-only.
        Relic,
        Relic_Effect,
        Relic_Effect_Param,
        Relic_Drop,
        RelicRarity,
        RelicStack,
        RelicConfig,
        Stage_Relic,
        Difficulty_Relic,
        Difficulty_Relic_Rarity,
        //# 던전 편성 JSON 5종 (dungeon-formation §3) — 값명 = Data/Json/<값명>.json 파일명. append-only.
        FormationConfig,
        FormationMonster,          //# 폐기(2026-11-01 종족 개편, JSON 삭제) — 정수 직렬화 안전을 위해 자리만 보존
        FormationSlot,
        Card_Species,
        Chapter_FormationHint,
        //# 무한의 탑 JSON 3종 (infinite-tower §4) — 값명 = Data/Json/<값명>.json 파일명. append-only.
        TowerConfig,
        HeroArchetype_Tower,
        Stage_Tower,
        DailyConfig,               //# 데일리 전역 설정 JSON (단일 객체) — Data/Json/DailyConfig.json (daily-challenge §4.2)
        //# 몬스터 육성 재화 JSON 10종 (monster-growth-currencies §6) — 값명 = Data/Json/<값명>.json 파일명. append-only.
        Currency,
        Currency_Exchange,
        AwakenStat,
        NestMastery,
        NestMastery_Param,
        MonsterTrait,
        MonsterTrait_Param,
        Difficulty_Awaken,
        Difficulty_TowerCrystal,
        RelicRarity_Shard,
        //# 던전 편성 종족·등급 개편 JSON (dungeon-formation §0.5) + 승급 비용 (monster-grades §8). append-only.
        Race,
        Grade,
        Race_Monster,
        Grade_Promotion,
    }

    //# 재화 (monster-growth-currencies §1) — 값명 = Currency.currencyId = 아이콘 파일명. Soul=0 이라 기존 ShopItemDef 직렬화 기본값과 호환. append-only.
    public enum ECurrency
    {
        Soul,
        AwakenStone,
        MoonSeal,
        AbyssCrystal,
        RelicShard,
    }

    //# 잔해 교환 결과 (monster-growth-currencies §8.3) — 교환 로직(Meta) → 유물 도감 VM·View 통신 계약.
    public enum EExchangeResult
    {
        Ok,
        Insufficient,   //# 잔해 부족 (1479)
        Locked,         //# 목표 재화 미해금 (1480)
        Unavailable,    //# 교환 행 없음 · 육성 데이터 미로드
    }

    //# 둥지 숙련 단계 효과 — NestMastery.effectKind 값명 (monster-growth-currencies §3.3).
    public enum ENestMasteryEffect
    {
        FirstSpawnAdvance,
        SpawnPeriod,
        SpawnGuard,
    }

    //# 종족 고유 특성 — MonsterTrait.trait 값명 (monster-growth-currencies §3.4).
    public enum EMonsterTrait
    {
        ThornShell,
        Grudge,
        Execute,
        CurseStack,
        Contagion,
        Afterimage,
    }

    //# 이번 전투가 어떤 모드인가 (infinite-tower §11.3) — 메모리 전용(MetaSession.BattleMode). 일일 도전이 Daily 를 맨 끝에 append 한다.
    public enum EBattleMode
    {
        Chapter,
        Tower,
        Daily,
    }

    //# 유물 2차 (relics §12.2) — 값명 = 아이콘 파일명(Art/Sprites/Relic/<값명>.png) = Relic.relicId. append-only.
    public enum ERelic
    {
        HeartOfStone,
        RustyBlade,
        SwiftBoots,
        BrokenHourglass,
        BreedingCharm,
        KingsCrownShard,
        EmberBrand,
        AmbushHorn,
        ThunderShard,
        CursedBanner,
        WrathSpirit,
        HasteHourglass,
        SoulPurse,
        LordsTome,
        ConsolationFeather,
        SpawnPrelude,
    }

    //# 유물 종류 — Relic.category 값명.
    public enum ERelicCategory
    {
        Stat,
        Combat,
        Convenience,
    }

    //# 유물 등급 — Relic.rarity / RelicRarity.rarity 값명.
    public enum ERelicRarity
    {
        Common,
        Rare,
        Legendary,
    }

    //# 유물 효과 종류 — Relic_Effect.effectKind 값명 (relics §3.3).
    public enum ERelicEffectKind
    {
        MonsterStat,
        SpawnerPeriod,
        HeroBurn,
        HeroLightning,
        HeroAttackDown,
        OpeningSummon,
        LowHpFrenzy,
        BattleSpeed,
        SoulBonus,
        XpBonus,
        LoseSoulBonus,
        SpawnPrelude,
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
    //# 순서 절대 변경 금지 — 신규 값은 맨 끝 append (int 직렬화 정합). 빌드 인덱스와 enum 순서는 무관.
    public enum EScene
    {
        Loading,   //# Build Settings index 0
        Battle,    //# Build Settings index 2
        Village,   //# Build Settings index 1 — v0.2 마을 허브 (Loading → Village → Battle)
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
        HeroDashConeFx,        //# 돌진 — 부채꼴(cone) 궤적
        HeroOrbitBladeFx,  //# 회전 블레이드 — 궤도 큐브
        HeroNovaFx,        //# AOE 노바 — 팽창 반투명 실린더
        //# 가해자별 임팩트 분리 (2026-06-04) — 영웅이 몬스터를 때릴 때 전용 CFXR 임팩트. 맨 끝 추가(int 직렬화 정합).
        MonsterHitImpact,  //# 피격자=몬스터 전용 CFXR 임팩트 (피격자=영웅은 기존 HitImpact 유지)
        //# TimeStop 카드 발동 중 영웅을 감싸는 실드 FX (2026-06-05) — 맨 끝 추가(int 직렬화 정합).
        TimeStopShield,    //# TimeStopAura 가 5초 부착 동안 영웅 위치에 스폰, OnDetached 시 풀 반환
        //# Fear 카드 적용 순간 영웅 위에 띄우는 스컬 FX (2026-06-05) — 맨 끝 추가(int 직렬화 정합).
        FearSkull,         //# FearEffect 적용 시 1회성 스폰, ReturnToPoolAfter 가 자동 풀 반환
        //# 스테이지 규칙 FX 6종 (chapter-system §7.1) — 프리팹 = 값명, CHMPool 대상. 프리팹은 아트 슬라이스에서 제작(append-only).
        StageSpringFx,      //# 성수 샘 개방 링
        StageFogFx,         //# 짙은 안개 오버레이
        StageSealChainFx,   //# 봉인된 둥지 사슬
        StageBulwarkFx,     //# 수호 결계 링
        StageArrowRainFx,   //# 화살비 예고/낙하
        StageTrapFx,        //# 사냥꾼의 덫
        //# 궁수 FX 4종 (chapter-system §7.1) — 프리팹 = 값명, CHMPool 대상. 프리팹은 아트 슬라이스(d2)에서 제작(append-only).
        ArcherArrowFx,         //# 궁수 기본 공격 화살 궤적(시각 전용, 판정 없음)
        HeroPiercingShotFx,    //# 관통 사격 직선
        HeroArrowVolleyFx,     //# 화살 소나기 낙하 지점
        HeroEvadeFx,           //# 회피 도약 폭발
        //# 암살자 FX 5종 (chapter-3-hero §7.1) — 프리팹 = 값명, CHMPool 대상. 프리팹은 아트 슬라이스(c)에서 제작(append-only).
        AssassinSlashFx,       //# 암살자 기본 공격 베기 궤적(시각 전용)
        HeroSlipFx,            //# 슬립(이탈) 잔상
        HeroShadowStrikeFx,    //# 그림자 습격 순간이동 출발/도착
        HeroBladeWhirlFx,      //# 칼날 선풍 회전 베기
        HeroSmokeBombFx,       //# 연막 탄 지대
        //# 광전사 FX 4종 (chapter-4 §7.1) — 프리팹 = 값명, CHMPool 대상. 프리팹은 아트 슬라이스(c)에서 제작(append-only).
        BerserkerSlashFx,      //# 광전사 범위 베기 원호(시각 전용)
        HeroCrushingLeapFx,    //# 분쇄 도약 착지 충격
        HeroChainPullFx,       //# 사슬 갈고리 당김
        HeroBloodFrenzyFx,     //# 피의 광란 오라
        //# 성기사 FX 3종 (chapter-5 §7.1) — 프리팹 = 값명, CHMPool 대상. 프리팹은 아트 슬라이스(c)에서 제작(append-only).
        HeroJudgmentHammerFx,  //# 심판의 망치 낙하 충격
        HeroPurifyingLightFx,  //# 정화의 빛 기둥
        HeroSanctuaryFx,       //# 성역 바닥 구역
        RelicLightningFx,      //# 유물 뇌운 조각 — 영웅 머리 위 낙뢰 (relics §6.5)
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
    //# 에셋 로드 키가 아닌 시스템 간 통신 계약. StatMultiplier 의 필드와 1:1 대응. 정수 직렬화(MetaConfig.StatKind) — 순서 변경 금지, append-only.
    public enum EMonsterStatKind
    {
        Hp,
        Power,
        Cooldown,
        Range,
        MoveSpeed,
        PoisonDamage,   //# 역병귀 독 피해 배율 (구 SlowFactor 자리 5, monster-growth-currencies §15.4)
        //# 각성 축 (monster-growth-currencies §3.2) — 상점 전종 강화가 아닌 종족 각성(AwakenStat.json)이 쓴다.
        DamageTaken,    //# 받는 피해 배율 (1 미만이 강화)
        KnockbackTaken, //# 받는 넉백 세기 배율 (1 미만이 강화)
        PoisonDuration, //# 역병귀 독 지속 배율
    }

    //# v0.2 마을 — 상점 영구 업그레이드 효과 종류 (기획서 §11.1). v0.3 — 종족 개별 강화 MonsterSpecies 추가.
    public enum EShopEffectKind
    {
        MonsterStat,     //# 몬스터 전종 글로벌 스탯 배율 (StatKind 지정)
        SpawnerPeriod,   //# 모든 스포너 주기 배율
        MonsterSpecies,  //# 종족(Species) 개별 HP·공격력 배율 (monster-species-enhancement §2)
        //# 몬스터 육성 3축 (monster-growth-currencies §3.6) — 결제 재화는 ShopItemDef.Currency, 수치는 JSON.
        MonsterAwaken,   //# 각성 — AwakenStat 의 종족 역할 스탯 배율
        NestMastery,     //# 둥지 — NestMastery_Param 의 단계 누적 효과
        MonsterTrait,    //# 특성 — MonsterTrait_Param 의 (종족, 단계) 수치
    }

    //# v0.2 마을 — 고정 도전과제 판정 조건 종류 (기획서 §5, RunSummary 데이터만으로 판정).
    public enum EAchievementCondition
    {
        FirstWin,           //# 첫 승리 (TotalWins >= 1)
        WinUnderSeconds,    //# 승리 && 사망시각 < Threshold
        TotalWins,          //# 누적 승수 >= Threshold
        TotalRuns,          //# 누적 출격 >= Threshold
        SynergyTierReached, //# 한 런 최고 시너지 Tier >= Threshold
    }

    //# === Cross-System Communication ===

    //# 회전 의도 — 로테이터가 즉시 스냅 여부를 결정하는 기준.
    //# Smooth: 항상 보간(이동/중앙복귀/도주). AttackAligned: _snapInstant 로테이터(영웅)만 즉시 스냅(공격 정렬).
    public enum FacingMode
    {
        Smooth,
        AttackAligned,
    }

    //# 전투 결과 — BattleStateModel / BattleViewModel / BattleController / ResultPopup 공용.
    public enum BattleResult
    {
        None,
        Win,
        Lose,
    }

    //# 토스트 종류 — 색 점(소울/금/피)으로 구분. ToastView.Show 의 인자이자 호출부 분류 계약.
    //# 분류 기준: 소울 부족·이름 규칙 위반 = Warning, 통신 실패·저장 실패 = Error, 그 외 = Info.
    public enum EToastKind
    {
        Info,
        Warning,
        Error,
    }

    //# 타격 피드백 피격자 종류 — DamageFeedback(Character) → HitFeedbackSpawner(Battle) 통신 계약.
    //# 임팩트 프리팹 분기 기준 (영웅=기존 HitImpact / 몬스터=MonsterHitImpact).
    public enum HitVictimKind
    {
        Hero,
        Monster,
    }

    //# 몬스터 종족 (dungeon-formation §0.2) — 둥지 6곳이 종족 하나씩의 자리. Race.json race 값명 = 이 값명. append-only.
    public enum ERace
    {
        Ignis,     //# 이그니스 — 불 (도깨비불)
        Lemur,     //# 레무르 — 떠도는 영혼 (망령)
        Nox,       //# 눅스 — 밤 (사신)
        Hexia,     //# 헥시아 — 저주 (저주술사)
        Miar,      //# 미아르 — 독기 (역병귀)
        Phantas,   //# 판타스 — 환영 (환령)
    }
}
