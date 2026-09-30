# UI 도트 던전 리디자인 (Art/UI 프리팹 32종)

## § 헤더

- **목표**: `Assets/_Lair/Art/UI/` 프리팹 32종 전체를 승인된 시안(`.mockups/ui-redesign.src.html`, 결과 `.mockups/ui-redesign.html`)의 "도트 던전" 스타일로 재디자인하고, 시안에서 승인된 "제안" 요소 9건을 신규 기능으로 넣는다.
- **검증 가설**: 하나의 도트 시각 언어(석판 패널·명판·버튼 5종·팔레트)로 통일하면 (1) 패시브/액티브·시너지 축 같은 정보 구분이 색만으로 읽히는가, (2) 마을 전적·랭킹 스테이지 탭·신기록 표시가 재도전·경쟁 동기(v0.3 가설)를 높이는가.
- **현재 단계 범위 적합성**: 범위 안. CLAUDE.md §8 이 아트/에셋 작업을 허용한다. 제안 요소 중 "스테이지별 랭킹"은 서버 연동 범위(클라이언트 코드만)에 속하며 Firestore 보안 규칙·인덱스 작업은 이 레포 범위 밖이다(§4.2). 메인 메뉴/세팅 화면은 만들지 않는다.
- **핵심 메커니즘**: 계단형 픽셀 테두리를 코드 없이 9-slice 도트 스프라이트로 재현하고(§2), 기존 프리팹의 GameObject 이름과 `[SerializeField]` 참조를 유지한 채 스프라이트·색·배치만 교체한다. 제안 요소는 대부분 기존 데이터로 계산되며, 새 저장 구조가 필요한 것은 2건이다(§4).

---

## 1. 프리팹 32종 전수 표

전제 (표 공통)
- 시안 화면 열의 "셀"은 시안에서 소속 팝업 안에 그려진 셀 프리팹이다.
- 유지 열: 기존 GameObject 이름은 모두 유지한다. 삭제는 표에 명시한 항목만 하며, 그 외에는 삭제하지 않고 스프라이트/색만 바꾼다(스크립트 `[SerializeField]` 참조 보존).
- 32종 모두 시안의 어느 화면에 대응한다. 대응이 없는 프리팹은 0건이다. 대응이 불명확한 지점은 표 아래 "불명확 지점"에 따로 적는다.
- 상태 목록은 시안에 그려진 상태만 적는다.

### 1.1 마을

| # | 프리팹 | 시안 화면 | 신규·변경 위젯 | 추가 GameObject (기존 이름은 전부 유지) | 상태 |
|---|---|---|---|---|---|
| 1 | VillageHud | 마을 허브 HUD | 상단바 석판(TopBar), 영주 얼굴 패널·이름·Lv·금색 XP 바, 소울 알약(소울 코인), 계정 버튼, 메뉴 6개 2열 그리드(도트 글리프 아이콘), 스테이지 카드 석판, ◀▶ 버튼, 방어하기(blood 버튼)·영웅(기본 버튼) | `LordFace`(영주 얼굴), `VillageSubText`("LAIR OF THE LORD"), `StageCard`(석판), `StageHeroPortrait`(침입자 초상), `StageDots`(5칸 점), `StageIntruderLabel`("침입자"), `RecordPanel`(오른쪽 전적 패널, 제안 1) + `RecordIntruderText`·`RecordBestText`·`RecordWinsText` | 스테이지 잠김(StageLockOverlay 자물쇠+"스테이지 N-1 클리어 필요"), 계정 빨간 점(충돌 대기), 스테이지 카드 별점 1~5 |
| 2 | HeroSelectPopup | 영웅 선택 | 명판 제목, 핏빛 X, 석판 본체, 가로 스크롤 | 없음 | - |
| 3 | HeroSelectCell | 영웅 선택의 셀 | 석판 셀, 초상, 이름, 보조 줄 | `SubText`(보조 줄 "N단계") | 기본 / 선택 중(소울 링 3px 상당) / 잠김(실루엣 + "???" + "잠김") |
| 4 | ShopPopup | 상점 | 명판, 현재 강화 요약(sunk 패널), 소울 알약 | 없음 | - |
| 5 | ShopItemCell | 상점의 셀 | 섹션 머리(HeaderBg/Accent/Text/Divider 를 시안 `shhd` 로 재스킨), 아이콘 sunk 패널, 레벨 5칸 눈금, 구매 버튼(soul/off/MAX) | `LevelPip0`~`LevelPip4`(정적 5칸; 전 아이템 MaxLevel 5 — `MetaConfig.asset` 확인) | 구매 가능(소울 링+glow) / 소울 부족(off 버튼) / 최대 레벨(금색 MAX) |
| 6 | LordLevelPopup | 영주성 | 명판, 영주 Lv 큰 글씨, 금색 seg XP 바(10칸), "다음 레벨까지 N XP" | `LordXpBar`(seg 바), `LordXpNextText`(제안이 아닌 신규, §4.1 계산) | - |
| 7 | LordRewardCell | 영주성의 셀 | 석판 행, 보상 소울 코인, 달성 배지 | `SubText`("수령 완료" / "N XP 남음") | 받은 보상(alpha 0.65 + 달성 배지) / 현재 레벨(금 링) / 미수령(dark 석판, 회색 코인) |
| 8 | QuestPopup | 도전과제 | 명판, X, 스크롤 | 없음 | - |
| 9 | QuestCell | 도전과제의 셀 | 3줄 카드(이름·배지 / 설명 / 진행 바·수치·보상) | 없음 | 진행 중 / 달성(초록 틴트 `#18251f` + 달성 배지) |
| 10 | CodexPopup | 도감 | 명판, 석판 탭 2개(금색 상단 줄), 수집 N/M 텍스트 | `CollectedText`("수집 5 / 6") | 몬스터 탭 / 카드 탭 |
| 11 | CodexCell | 도감의 셀 | 아이콘, 이름, Lv 배지 | 없음 | 발견 / 최대 강화(GlowOverlay 금 링+금 글로우) / 미발견(검은 실루엣 + "???") |
| 12 | RecordsPopup | 기록 | 명판, 통계 숫자 타일 5개(제안 7), 스테이지 셀 가로 스크롤 | `StatTileRuns`·`StatTileWins`·`StatTileRate`·`StatTileBest`·`StatTileTopCard`(가장 많이 픽한 카드, 제안 7). **삭제**: `BodyText`(타일이 대체, 스크립트 `_bodyText` 필드 제거 동반) | - |
| 13 | RecordsStageCell | 기록의 셀 | STAGE 라벨, 초상, 별점, 승수, 판수·승률, 최단 | 없음 | 기본 / 선택 중(금 링 + "선택 중" 배지) / 잠금(dark 석판 + 실루엣 + 해금 조건) |
| 14 | RankingPopup | 랭킹 | 명판, 스테이지 탭(제안 2), 헤더 행, 내 순위 행 소울 링 + "나" 배지 | `StageTabRow`·`StageTab1`~`StageTab5`·`OverallTab`("최단 클리어") | 스테이지 탭 선택 / 최단 클리어 탭 / 빈 목록(EmptyText) |
| 15 | RankingCell | 랭킹의 셀(루트 이름 `LeaderboardCell`) | 순위 숫자 금·은·동, 영웅 아이콘 | `HeroIcon` | 1위·2위·3위(금·은·동) / 4위 이하(dark 석판) / 내 행(MyRankRow 가 사용) |
| 16 | CloudPopup | 계정 | 명판, 표시명 입력(소울 링 입력칸), 연결 상태 점, 충돌 패널(빨강) + 두 세이브 비교 칸(제안 3) | `ConflictCompare`·`ConflictLocalText`("이 기기")·`ConflictCloudText`("클라우드") | 이름 편집 닫힘(기본) / 편집 중 / 클라우드 연결됨 / 충돌 대기(ConflictGroup 표시) |

### 1.2 전투 HUD

| # | 프리팹 | 시안 화면 | 신규·변경 위젯 | 추가 GameObject | 상태 |
|---|---|---|---|---|---|
| 17 | BattleHud | 전투 HUD | 타이머 dark 석판, 시너지 패널 위치(오른쪽 위), 빌드 바 dark 석판(하단 중앙, 패시브 소울색·액티브 금색 구획) | `TimerCaption`("남은 시간"), `BuildSeparator`(패시브/액티브 구분 3px 선) | 타이머 정상 / 경고(붉은 글씨, 임계는 §4.5 결정) |
| 18 | HpBar | 전투 HUD의 영웅 HP 바 (+ 트레이 단독) | 붉은 seg 바 10칸 눈금(제안 9), HP 텍스트 바 중앙, 상태 아이콘 최대 8칸 | `SegDivider1`~`SegDivider9`(정적 자식, 앵커 n/10) | 상태 아이콘 0~8칸 |
| 19 | SpawnerStatusPanel | 전투 HUD 왼쪽 위 | 세로 스택(간격 36 ref), 폭 206 css | 없음 | - |
| 20 | SpawnerStatusCell | 스포너 셀 | 석판, 왼쪽 ColorChip(시너지 축색), 아이콘 sunk 패널 + Lv 배지, 다음 스폰 진행 바 | 없음 | 기본 / 최대 레벨(GlowOverlay 금 링 + 금 글로우) |
| 21 | BuildSynergyPanel | 전투 HUD 오른쪽 위 | 머리 줄("시너지 / 상세 ›") + 셀 4개 세로 | `HeaderRow`(머리 줄) | - |
| 22 | BuildSynergyCell | 시너지 패널 셀 | dark 석판, 왼쪽 도트 문장 아이콘(22css), 축 이름, Tier 마커 3칸(사각 점) | `AxisIcon`, `AxisStripLeft`. Marker0~2 는 유지하되 모양을 축 아이콘에서 사각 점으로 변경. 축 이름 텍스트 `Text` 에서 "N/임계" 를 뺀다 | 활성(왼쪽 색띠 + 축색 점) / 흐림(alpha 0.55) |
| 23 | BuildIconCell | 빌드 바의 아이콘 셀 | 44px 슬롯 + 종류색 링, 중첩 ×N 배지 | `KindRing`(종류색 Px_Ring), `EmptyHatch`(빈 슬롯 빗금) | 패시브(소울 링) / 액티브(금 링) / 빈 슬롯(빗금) |
| 24 | SkillUnlockBanner | 전투 HUD 스킬 해제 배너 | 보라 그라데이션 띠(`Band`) + 상하 저주색 선 + "SKILL" 태그 | `SkillTag`("SKILL" 태그, curse 링) | 표시 / 숨김 |

### 1.3 전투 팝업

| # | 프리팹 | 시안 화면 | 신규·변경 위젯 | 추가 GameObject | 상태 |
|---|---|---|---|---|---|
| 25 | CardSelectionPopup | 카드 선택 | 어두운 Dim, 큰 제목, 카드 3장, **부제**(제안 9) | `Subtitle`("패시브 · 영웅 HP 60% 도달" / "액티브 · 30초 주기") | 패시브(소울 프레임·soul 버튼) / 액티브(금 프레임·gold 버튼); 카드 호버(위 10px + 소울 글로우) |
| 25a | (CardSelectionPopup 안) CardView_0~2 | 카드 1장 | 프레임 링(종류색 tint), 아트, 하단 Scrim, 이름, 설명, CountBadge 오른쪽 위, PickButton | `KindLabel`(카드당 1개, "패시브"/"액티브") | 위와 동일 |
| 26 | BuildModalPopup | 빌드 상세 | 명판, X, 좌우 2단(패시브 / 액티브) | `PassiveHeader`, `ActiveHeader`(색 사각 + "패시브 · N") | 카드 있음 / 빈 섹션(EmptyText 회색 가운데) |
| 27 | BuildModalCardCell | 빌드 상세의 셀 | 48px 아이콘 슬롯 + 종류색 링, 이름, 설명, ×N | 없음 | 패시브(소울 링) / 액티브(금 링) |
| 28 | SynergyModalPopup | 적용 중인 시너지 | 명판, X, 스크롤 | 없음 | 시너지 없음(EmptyText) |
| 29 | SynergyModalCell | 시너지 모달의 셀 | 축 색띠, 아이콘 패널(도트 문장), 제목 + 단계 배지(2/3), 설명 + "다음: …" 미리보기(제안 5) | `TierBadge`, `DescText`, `NextText` | 진행 중 단계 / 최대 단계("· 최대 단계") |
| 30 | ResultPopup | 결과 | 큰 결과 글씨, 부제, 항목 줄(sunk 패널), 버튼 2개, 승리 방사광(`Rays`) | `SubText`, `RowClear`("클리어 시간" + `NewBadge`), `RowSouls`, `RowXp`(제안 4), `RowHeroHp`(패배 전용, 제안 4), `Rays` | 승리 / 패배. `RewardText` 는 유지하되 영주 레벨 업·도전과제 달성 줄 전용으로 축소(시안에 없음 — §6 열린 질문 5) |
| 31 | ConfirmPopup | 확인 팝업 | 명판, 메시지, 취소(기본)/확인(soul) | Dim 이 프리팹에 없다. 루트가 전체화면 Image 인지 확인 후 없으면 `Dim` 추가 | - |
| 32 | ToastView | 토스트 | dark 석판 알약 | `Dot`(색 점, 제안 6) | 정보(소울 점) / 경고(금 점) / 오류(피 점) |

행 25a 는 32종에 포함되지 않는다. 25 의 내부 GameObject 이며, 프리팹 파일 수는 표 1.1(16) + 1.2(8) + 1.3(8) = 32 이다.

### 1.4 불명확 지점 (프리팹 대응은 있으나 시안과 프리팹 차이가 큰 것)

1. VillageHud 의 빨간 점: 프리팹은 `RedDot` 1개(계정 충돌 점, 스크립트의 `_cloudConflictDot`)뿐이다. 시안은 퀘스트 버튼에도 빨간 점을 그렸지만 기준 데이터(미수령 과제 등)가 없다. **시안 장식으로 보고 구현하지 않는다.** 필요하면 열린 질문 7.
2. VillageHud 스테이지 카드: 시안의 침입자 초상·점 5칸·"침입자" 라벨은 제안 목록(9건)에 없는 추가 위젯이다. 초상 스프라이트는 기록/영웅 선택이 이미 쓰는 영웅 초상 데이터로 충당한다.
3. HeroSelectCell 의 보조 줄: 시안의 "견습/수련/숙련" 단계명은 샘플 문구이며 데이터가 없다. "N단계"만 표시한다.
4. ConfirmPopup: 시안의 회색 보조 문장("진행 중인 판이 없습니다")은 샘플이다. 구현하지 않는다.
5. BuildSynergyCell: 시안은 축 이름과 Tier 마커만 있고 "N/임계" 개수가 없다. 시안을 따라 개수를 뺀다.

---

## 2. 공통 스킨 시스템

### 2.1 단위 규칙
- **도트 1칸(art px)** = 시안 3 css px = 실제 Canvas 기준 4 ref unit (Canvas 기준 해상도 1280x720, 시안 960x540 의 4/3 배; 검산: 3 css × 4/3 = 4 ref).
- 스프라이트는 **Filter Point / Compression None / Mipmap 꺼짐 / Pixels Per Unit 25**로 가져온다. Image Type = Sliced, PPU 25 이므로 Canvas `referencePixelsPerUnit 100` 에서 1 art px = 100/25 = 4 ref unit 이 된다.
- 시안의 3 css 미만 세부(2px 링, 1px 그림자)는 **1 art px 로 스냅**한다.
- 시안 layout 값(css px)은 **ref unit = css × 4/3** 로 환산하되 3 css 배수는 art px 배수로 맞춘다. 예: 버튼 최소 높이 40 css = 53.3 → 13 art px = 52 ref; 명판 높이 13 art px = 52 ref.
- 저작 위치: `Assets/_Lair/Art/Sprites/UiDot/` (Rule 04 §2 의 `Sprites/`). 파일명 접두어 `Px_`. UI 프리팹이 직접 참조하는 스프라이트이므로 **Enum 키 없음**(Rule 03 §2 는 `CHMResource` 로 로드하는 에셋만 대상. 프리팹 내부 직렬화 참조는 대상 아님).
- 생성 방식: 픽셀 배열 → PNG 를 만드는 에디터 스크립트 1회 실행 후 **스크립트 삭제**(Rule 04 §3, 스프라이트 authoring 툴도 동일 취급). 글리프 픽셀 데이터는 이미 `.mockups/build-ui-redesign.js` 의 `GLYPHS`(12x12 도트, 10종)에 있으므로 그대로 옮긴다.
- 색: 기본 Image 색은 흰색(1,1,1,1). 종류·상태 색은 **스프라이트 흰 픽셀 + Image.color tint** 로 얻는다(검정 윤곽은 tint 해도 검정 유지).

### 2.2 팔레트 (시안 토큰 그대로)

| 토큰 | hex | 용도 |
|---|---|---|
| ink | #07090e | 윤곽·그림자 |
| stone0 | #10141d | 어두운 하단 베벨 |
| stone1 | #171d2a | dark 석판 본체, 탭 |
| stone2 | #222a3b | 석판 본체 |
| stone3 | #313b52 | 버튼 본체, 좌측 베벨 |
| stone4 | #48546e | 상단 베벨, 스크롤바 |
| bone / bone2 | #e8e1cf / #b9b09a | 제목·이름 글씨 |
| txt / sub | #eef1f6 / #8e98ad | 본문 / 보조 글씨 |
| soul / soul2 | #5ef0b4 / #1f9e74 | 패시브·소울·확정 |
| gold / gold2 | #f7c64a / #b9832a | 액티브·XP·보상·MAX |
| blood / blood2 | #e5484d / #8e1f2a | 출격·X·오류·적 HP |
| curse / curse2 | #b58cff / #5b3aa6 | 스킬 해제 배너 |
| tank / dps / swarm / debuff | #5aa9ff / #ff6b5a / #7be36a / #c08bff | 시너지 축 |
| 나무 명판 | #3a2f22 (하이라이트 #6b563b, 그림자 #231a11) | 명판 |
| sunk 배경 | #0c1017 | 아이콘·통계 슬롯 |
| 바 배경 | #0b0e14 | 진행 바 |

### 2.3 스프라이트 목록 (9-slice 는 border 를 art px 로 표기: 좌,우,상,하)

| 파일명 | 크기 (art px) | Sliced border | 내용 |
|---|---|---|---|
| Px_Panel | 8x8 | 3,3,3,3 | 석판. 윤곽 ink 1칸, 상단 stone4·좌 stone3·하/우 stone0 베벨 1칸, 본체 stone2. 바깥 모서리 1칸은 투명(계단형 코너) |
| Px_PanelDark | 8x8 | 3,3,3,3 | dark 석판. 본체 stone1, 상단 ink + 어두운 안쪽 그림자, 하단 stone3 베벨 |
| Px_PanelSunk | 6x6 | 2,2,2,2 | 파인 슬롯. 본체 #0c1017, 상/좌 stone0, 하/우 stone3, 코너 투명 |
| Px_Btn | 8x9 | 2,2,2,3 | 기본 버튼. 본체 stone3, 상단 베벨 stone4, 하단 베벨 2칸 stone1 |
| Px_BtnSoul | 8x9 | 2,2,2,3 | 본체 soul2, 상단 soul, 하단 #0e5a41 |
| Px_BtnGold | 8x9 | 2,2,2,3 | 본체 #d89a2e, 상단 #ffe08a, 하단 #8a5a14 |
| Px_BtnBlood | 8x9 | 2,2,2,3 | 본체 #b8323a, 상단 #ff7b7f, 하단 #6b141d |
| Px_BtnOff | 8x9 | 2,2,2,3 | 본체 #2a2f3a, 상단 베벨 없음, 하단 #1b1f27 |
| Px_Plaque | 20x13 | 8,8,0,0 | 나무 명판. 높이 13칸(52 ref)로 고정, 좌우 8칸 안에 금색 리벳(1칸 + ink 링). 가로만 늘어남 |
| Px_CloseX | 11x11 | 없음(단일) | 핏빛 × 버튼. × 모양을 스프라이트에 굽는다(폰트 의존 제거). 표시 크기 44x44 ref |
| Px_Tab | 8x8 | 3,3,3,0 | 비선택 탭. 본체 stone1, 상/좌/우 ink 윤곽, 하단은 패널과 이어지도록 열림 |
| Px_TabOn | 8x8 | 3,3,3,0 | 선택 탭. 본체 stone3 + 상단 1칸 금색 줄 |
| Px_Ring | 4x4 | 1,1,1,1 (Fill Center 끔) | **흰색 1칸 링**. tint 로 선택 표시(소울/금/커스/흰)·카드 프레임·슬롯 종류 링에 공통 사용 |
| Px_CardRing | 8x8 | 3,3,3,3 (Fill Center 끔) | 카드 프레임: 바깥 ink 1칸 + 흰 링 1칸(tint) + 안쪽 ink 1칸 |
| Px_BarBg | 6x6 | 2,2,2,2 | 진행 바 배경 #0b0e14 + ink 윤곽 |
| Px_BarFill | 4x4 | 없음 | 채움: 위 1행 밝은색, 중간 1행 본색, 아래 2행 어두운색. **흰색 기준으로 그려 tint**(소울/금/피) — 밝은/어두운 행도 명도만 다른 회색 계열로 그려 tint 시 자연스럽게 색이 나온다 |
| Px_Solid | 1x1 | 없음 | 흰색 1픽셀. 색 점·SegDivider·구분선·진행 눈금에 tint 로 사용 |
| Px_Divider | 4x2 | 1,1,0,0 | 위 1행 ink, 아래 1행 stone3 |
| Px_Badge_Gold | 5x5 | 2,2,2,2 | ink 본체 + gold2 링 |
| Px_Badge_Soul | 5x5 | 2,2,2,2 | ink 본체 + soul2 링 |
| Px_Badge_Red | 5x5 | 2,2,2,2 | blood 본체 + ink 링 |
| Px_Hatch | 4x4 | 없음(Tiled) | 빈 슬롯 45도 빗금 (#11151d / #0c1017) |
| Px_SoulCoin | 5x5 | 없음 | 소울 코인(팔각형, soul + 우하 soul2). 부족 표시는 tint #556 |
| Px_Star | 5x5 | 없음 | 별. 활성 tint gold, 비활성 tint #3a4256 (폰트 ★ 글리프 의존 제거) |
| Px_Dot | 3x3 | 없음 | 흰 1칸 + ink 링. 스테이지 점(활성 gold, 비활성 stone4)·토스트 색 점에 tint |
| Px_Lock | 8x9 | 없음 | 자물쇠(stone4 + ink) |
| Px_ArrowR | 5x7 | 없음 | ▶ 화살표. ◀ 는 X 스케일 -1 |
| Px_Rays | 160x160 | 없음 | 승리 방사광(금색 알파 낮은 부채꼴 8도 간격 22도 주기, 중앙 페이드). 640 ref = 160 x 4 |
| Px_Glyph_Castle / Shop / Book / Scroll / Tomb / Crown | 12x12 | 없음 | 마을 메뉴 6종 (`GLYPHS` 이식) |

`SynergyIcons/TANK.png · DPS.png · SWARM.png · DEBUFF.png` 4종은 파일명·경로를 유지한 채 12x12 도트 문장(`GLYPHS` 의 shield/sword/swarm/curse)으로 **덮어쓴다**(GUID 보존 → 프리팹·코드 참조 무손실, 제안 8). 현재 340~470KB 이므로 교체 후 1KB 미만이 되는 것이 정상. Point 필터 설정을 이 4종에 함께 적용한다.

기존 `Button_Wide_*`, `Button_Square_*` 스프라이트는 프리팹 참조가 모두 `Px_Btn*` 으로 바뀐 것을 확인한 뒤 삭제한다(현재 프리팹은 ConfirmPopup 처럼 Sprite 없는 단색 Image 를 쓰는 곳이 많으므로 실제 참조 수는 1단계에서 Grep 으로 확인).

### 2.4 컴포넌트별 스킨 적용 규칙 (프리팹 32종 공통)

| 요소 | 스프라이트 | 색 / 설정 |
|---|---|---|
| 팝업 본체(ModalBody) | Px_Panel | 패딩: 상 46 css(명판 아래) / 좌우 22 css / 하 20 css를 art px 반올림 |
| 팝업 제목(Title) | Px_Plaque 배경 Image + 기존 Title CHText | 글씨 bone, 그림자 2px ink |
| 닫기(X, CloseButton) | Px_CloseX | 우상단 -10 css 오프셋. `CloseButton`(기존 클릭 영역)은 유지 |
| Dim | 기존 단색 Image 유지(스프라이트 없음) | 색 #04060a (rgb 4,6,10), alpha 0.72. 마을·전투 팝업 동일(시안 마을 팝업의 사선 패턴은 구현하지 않는다) |
| 버튼(CHButton) | Px_Btn 계열 5종 | Transition = Color Tint. Normal (0.9,0.9,0.9), Highlighted (1,1,1), Pressed (0.75,0.75,0.75), Disabled = Px_BtnOff 스프라이트 스왑 대신 Interactable 끔 + 위 Off 스킨 |
| 목록 행·셀 | Px_Panel(기본) / Px_PanelDark(비활성·미수령) | 링은 Px_Ring 자식 Image + tint |
| 슬롯(아이콘 배경) | Px_PanelSunk | - |
| 진행 바 | Px_BarBg + Px_BarFill (tint) | 일반 바 fill soul, 영주 XP·상점 금, HP 피 |
| 바 10칸 눈금(seg) | Px_Solid 정적 자식 9개(SegDivider1~9), 앵커 x = n/10 | tint (0,0,0,0.6), 폭 1 art px |
| 배지 | Px_Badge_Gold/Soul/Red | 글씨색은 배지 종류별(gold / soul / 흰색) |
| 색 점(충돌·연결·토스트) | Px_Dot | tint soul / gold / blood |
| 스크롤바 | Px_Panel 아닌 Px_Solid 로 핸들만(stone4, 폭 2 art px) | 기존 SynergyModalPopup VerticalScrollbar 유지, 나머지 스크롤뷰는 시안처럼 핸들만 |

---

## 3. 폰트

현재: `Assets/_Lair/Data/Fonts/NotoSansKR SDF.asset` (모든 프리팹의 TMP 가 GUID `12e8e80f…` 로 참조 — ConfirmPopup 에서 확인).
시안: Galmuri11(11px 비트맵 계열 도트 폰트, Galmuri9 폴백). 시안의 스타일 일관성(도트 테두리 + 도트 글씨)은 폰트가 결정한다.

**이 결정은 사용자 선택이 필요하다(열린 질문 1).** 스킨(§2)과 폰트 교체는 서로 독립이라 어느 쪽을 골라도 나머지 작업은 그대로 진행한다.

| 안 | 내용 | 장점 | 단점·비용 |
|---|---|---|---|
| A (권장 검토안) | 전 프리팹 폰트를 Galmuri11 로 교체 | 시안과 동일한 룩. 도트 UI 와 글씨 톤 일치 | Galmuri11 은 SIL OFL 1.1 — 상용 배포·번들 허용, **폰트 파일과 라이선스 문구 동봉 필요**, 폰트 자체를 단독 판매하는 것만 금지. TMP 에셋은 SDF 가 아니라 **Point 샘플링 래스터 아틀라스**로 새로 생성해야 하며 한글 완성형 11,172자를 전부 넣으면 아틀라스가 커진다(한 벌 11px 기준 1024~2048 텍스처 여러 장). 사용 문자만 추려 static 으로 만들면 작지만, 문자열이 새로 늘면(스트링 테이블 추가) 재생성이 필요하다. 글씨 크기는 11의 정수배(11/22/33)에서만 선명 |
| B | Galmuri11 을 dynamic TMP 폰트 에셋으로 사용 | 재생성 불필요, 신규 문자 자동 추가 | 런타임 아틀라스 확장 비용. 모바일 첫 프레임 히치. Dynamic 폰트는 Point 필터·정수 배 크기를 프리팹마다 보장해야 함 |
| C | Noto Sans KR 유지 | 추가 작업 0, 한글 글리프 이미 검증됨 | 도트 테두리와 매끈한 폰트가 어긋나 "반쯤 도트" 룩. 시안과 다른 결과 |

결정에 필요한 확인: (1) 라이선스 동봉을 감수하는가, (2) 정적(A) 대 동적(B) 아틀라스, (3) 크기 11 배수 제약(현 프리팹 글씨 크기를 재조정해야 함) 수용 여부.

기본 진행: 사용자가 답하기 전까지 **C안(Noto 유지)으로 나머지 전 단계를 진행**하고, 폰트 교체는 마지막 별도 단계(§5 단계 7)로 분리한다.

---

## 4. 새 데이터·로직 필요 항목 (제안 요소별)

저장 구조 확인 결과 (Grep):
- `MetaProfile`(`Assets/_Lair/Scripts/Meta/MetaProfile.cs`, Version 3, JsonUtility, 리스트 엔트리 방식): `TotalRuns`, `TotalWins`, `BestClearTime`(전체 최단), `StageRecords`(스테이지별 `Runs`/`Wins`/`BestClearTime`), `PickedCards`(**distinct 목록 — 횟수 없음**), `LordXp`, `SelectedStage`, `SelectedHero`.
- 리더보드(`FirebaseSdkApiClient`): 컬렉션 `leaderboard`, 문서 ID = uid 하나, 필드 `uid/displayName/clearTimeMs/hero`. **스테이지 축이 없다.** 조회는 `clearTimeMs > 0` 오름차순 Top N + 내 순위 Count 집계.
- 승리 시 랭킹 제출(`BattleController.SubmitRanking`)은 `MetaProfile.SelectedHero` 만 보낸다(스테이지 미포함).

| # | 제안 요소 | 필요한 것 | 기존 데이터로 가능? | 범위(§8) |
|---|---|---|---|---|
| 1 | 마을 전적 패널 | "이번 침입자"(영웅 이름·SelectedStage), "최단 방어"(`GetStageRecord(SelectedStage).BestClearTime`, 없으면 "-"), "이 스테이지 전적"(`Wins`승 · `Runs`판) | **가능** (신규 저장 없음). 스테이지 변경(◀▶) 시 패널 즉시 갱신 이벤트가 필요 | 안 |
| 2 | 랭킹 스테이지 탭 | 스테이지별 리더보드 | **불가 — 새 서버 구조 필요 (§4.2, 구조 확인 게이트 대상)** | 클라이언트 코드는 안, 보안 규칙·인덱스는 밖 |
| 3 | 세이브 충돌 비교 칸 | 클라우드 프로필의 영주 Lv·소울 표시 | 충돌 감지 시 서버 프로필을 이미 받아오는지 미확인 — 받으면 `LordXp`(→Lv)·`Souls` 로 가능. 이 기기 값은 로컬 `MetaProfile`. 서버 프로필을 저장해 두지 않으면 충돌 시점에 1회 읽기를 추가해야 한다. 신규 저장 없음 | 안 |
| 4 | 결과 팝업 NEW / XP / 영웅 HP | `ResultPopupArg` 에 `IsNewBest`(승리이면서 이번 스테이지 이전 최단보다 빠르거나 이전 기록이 없을 때 true, **`RecordStageRun` 호출 전에 판정**), 클리어 시간, `HeroHpRatio`(남은 HP 비율, 패배 시 `1 - GetHeroDamagedRatio()`). `XpGained` 는 이미 있음 | **가능** (신규 저장 없음) | 안 |
| 5 | 시너지 단계·다음 단계 | 현재 단계(Thresholds 3/5/7 로 이미 계산), 다음 단계 설명(`tierOf(axis, Thresholds[tier])` 이미 있는 경로), 최대 단계 여부 | **가능** (신규 저장 없음) | 안 |
| 6 | 토스트 색 점 | `ShowToast` 호출에 종류(정보/경고/오류) 인자 추가. 호출부 분류 기준: 소울 부족·이름 규칙 위반 = 경고, 통신 실패·저장 실패 = 오류, 그 외 = 정보 | **가능** (저장 없음) | 안 |
| 7 | 기록 "가장 많이 픽한 카드" | 카드별 누적 픽 횟수 | **불가 — 새 저장 필드 필요 (§4.1, 구조 확인 게이트 대상)** | 안 |
| 8 | 시너지 아이콘 | 이미지 교체만 | 가능 | 안 |
| 9 | HP 바 10칸 눈금 / 카드 부제 | 눈금은 UI 만. 부제는 `CardSelectionPopupArg` 에 종류(패시브/액티브)와 트리거 근거(패시브: 도달한 HP 구간 %, 액티브: 30초 주기)를 추가. 트리거 서비스가 임계값을 이미 알고 있어 계산 가능 | **가능** (신규 저장 없음) | 안 |

그 외 신규 표시 값(전부 기존 데이터): 영주성의 "다음 레벨까지 N XP"(`LordXp`·`LordLevelService`), 도감 "수집 N/M"(`SeenMonsters`/`PickedCards` 대 전체 수), 셀 SubText 들.

### 4.1 카드 픽 횟수 (제안 7) — 구조 확인 필요

핵심 엔티티: **"카드 하나의 누적 픽 횟수"**. `MetaProfile` 에 관계 데이터 리스트를 추가한다(기존 `ShopLevels`/`StageRecords` 와 같은 엔트리 리스트 방식). 모든 카드가 아니라 **한 번이라도 픽한 카드만** 행이 존재하므로 sentinel(0/-1) 없이 존재 여부로 표현한다(Rule 02 §11-1).

| 데이터 | 종류 | 필드 | 관계 |
|---|---|---|---|
| MetaProfile (기존) | 엔티티 | 기존 필드 + `CardPickCounts` (신규 리스트) | - |
| CardPickCountEntry (신규 row) | 선택적 관계 데이터 (MetaProfile 소속, 픽한 카드만 존재) | `CardId`(string, `ECardId.ToString()`), `Count`(int, 누적 픽 횟수 ≥1) | `CardId` → 카드 정의(기존 `PickedCards` 와 동일 키) |

- 기존 저장 구조에 새 필드 1개 추가로 해결된다(새 JSON 파일 아님). 구버전 세이브는 필드 부재 → 빈 리스트로 로드(기존 `StageRecords` 와 동일 방식이며 "가장 많이 픽한 카드" 는 이후 판부터 집계). `MetaProfile.Version` 은 4 로 올린다.
- 클라우드 백업/복원 대상에 포함(`CopyFrom` 에서 복사). 서버 문서 스키마는 프로필 통째 저장이라 Firestore 쪽 변경 없음.
- 집계 시점: 판 종료 시 `_runPicks` 의 각 카드를 +1(같은 판에서 중첩 픽은 픽한 횟수만큼 +N). 표시: 최대 Count 카드(동률이면 최근 픽 우선이 아니라 **카드 ID 사전순 첫 번째**로 고정) "이름 ×Count", 기록 없으면 "-".
- 대안: (b) `PickedCards` 를 `"CardId:Count"` 문자열로 확장 — 기존 도감 로직 전부 수정 필요, 마이그레이션 위험 → 기각. (c) 제안 7 자체를 보류 — 저장 변경 없이 5칸 타일 중 4칸만 구현(가장 안전, 기능 1개 손실). **권장: 위 표의 신규 필드 추가안.**

### 4.2 스테이지별 랭킹 (제안 2) — 구조 확인 필요

핵심 엔티티: **"한 유저의 한 스테이지 최단 클리어 기록"**. 현재 `leaderboard/{uid}` 는 스테이지 축이 없어 스테이지 탭을 만들 수 없다.

| 안 | 구조 | 장단점 |
|---|---|---|
| A (권장) | 새 컬렉션 `stageLeaderboard`, 문서 ID `"{stage}_{uid}"`, 필드 `uid`, `stage`(int 1~5), `displayName`, `clearTimeMs`, `hero`. 기존 `leaderboard` 는 그대로 두고 "최단 클리어" 탭이 계속 사용 | 기존 데이터·기존 쿼리 무손상. 조회는 `stage == N` + `clearTimeMs` 오름차순 → **Firestore 복합 인덱스 필요**(콘솔). 제출은 스테이지 최단만 갱신 (기존 승리 시 `RecordStageRun` 결과 사용) |
| B | 기존 `leaderboard/{uid}` 문서에 `stage` 필드 추가하고 스테이지별 문서로 분리 | 기존 문서와 스키마 섞임(구문서는 `stage` 없음) → 기각 |
| C | 스테이지 탭 없이 "최단 클리어" 단일 랭킹 유지, 시안의 스테이지 탭은 구현 보류 | 서버 변경 0. 제안 2 손실 |

| 데이터 | 종류 | 필드 |
|---|---|---|
| stageLeaderboard/{stage}_{uid} (신규) | 엔티티 (한 유저의 한 스테이지 기록) | `uid`, `stage`, `displayName`, `clearTimeMs`, `hero` |
| leaderboard/{uid} (기존, 유지) | 엔티티 (한 유저의 전체 최단) | `uid`, `displayName`, `clearTimeMs`, `hero` |

- 이 레포 범위: `FirebaseSdkApiClient`/`RankingClient` 에 스테이지 제출·조회 메서드 추가, 랭킹 UI 탭. **범위 밖(Firebase 콘솔 소관)**: `stageLeaderboard` 보안 규칙(본인 문서만 쓰기·`clearTimeMs` 범위 검증)과 복합 인덱스 생성. 콘솔 작업이 끝나기 전에는 스테이지 탭 조회가 빈 목록으로 폴백된다(기존 실패 폴백 규약: 빈 리스트).
- 기존 유저 백필: 이미 있는 `StageRecords[].BestClearTime` 을 마을 진입 시 1회 제출하는 방법이 있으나 안티치트 근거가 없으므로 **하지 않고**, 다음 승리부터 쌓는다(열린 질문 3).
- 스테이지 탭 UI: 시안의 탭 2개(STAGE N / 최단 클리어)를 `StageTab1~5` + `OverallTab` 6개로 확장한다. 기본 선택은 현재 `SelectedStage`.

---

## 5. 구현 순서 제안 (단계 = 커밋 1개 기본)

프리팹은 Rule 04 §3 대로 **빌더 스크립트로 생성 → 실행 → 스크립트 삭제**, 텍스트는 `CHText`(정적 라벨 포함 필수), 버튼 `CHButton`, 탭 `CHToggle`, 목록 `CHPoolingScrollView`(Rule 03 §3 BuildModalPopup 패턴: 코드 동적 생성 금지, 정적 배치). Sprite 는 Rule 03 §4 의 풀링·`Instantiate` 금지에 해당 없음(에디터 저작).

UI 프리팹 배선(UnityMCP) 착수 전에는 Rule 00 "UI 작업 — 목업 승인 게이트"가 걸린다. 이 기획의 목업은 이미 승인되었으므로 그 목업(`.mockups/ui-redesign.html`)을 근거로 하되 **제안 요소 화면(계정 충돌 비교, 랭킹 탭 등)의 구조 변경이 승인 목업과 달라질 때만** 재승인한다.

| 단계 | 내용 | 대상 프리팹 | 검증 |
|---|---|---|---|
| 1 | 공통 스프라이트 생성(§2.3 전부) + 임포트 설정(Point/PPU 25/border) + SynergyIcons 4종 교체 | 프리팹 없음 | 스프라이트 각각을 UnityMCP 로 빈 Canvas(1280x720)에 배치한 스크린샷을 시안 캡처와 대조: 계단 코너 존재, 4 ref 단위 픽셀 격자 정렬, 늘렸을 때 모서리 왜곡 없음 |
| 2 | 공용 팝업·토스트 | ConfirmPopup, ToastView (+ 토스트 종류 인자, 제안 6) | 두 프리팹을 각각 열어 스크린샷. 버튼 5종(기본/soul/gold/blood/off) 스크린샷이 시안 트레이와 일치 |
| 3 | 마을 HUD | VillageHud (+ 전적 패널 제안 1) | Village 씬 재생 스크린샷: 상단바·메뉴 2열·스테이지 카드·전적 패널·잠금 오버레이(잠긴 스테이지로 이동해서) |
| 4 | 마을 팝업 | HeroSelect(+Cell), Shop(+Cell), LordLevel(+RewardCell), Quest(+Cell), Codex(+Cell), Records(+StageCell, 제안 7), Ranking(+Cell, 제안 2), CloudPopup(제안 3) — 팝업 1~2개씩 별도 커밋 | 팝업마다 열어 스크린샷. 셀 상태(구매 가능/부족/MAX, 달성/현재/미수령 등)는 상태를 만드는 테스트 데이터로 각각 1장 |
| 5 | 전투 HUD | BattleHud, HpBar, SpawnerStatusPanel/Cell, BuildSynergyPanel/Cell, BuildIconCell, SkillUnlockBanner | Battle 씬 재생 중 스크린샷: HP 눈금, 시너지 활성/흐림, 빌드 바 종류색, 스킬 해제 배너 |
| 6 | 전투 팝업 | CardSelectionPopup(제안 9), BuildModalPopup(+Cell), SynergyModalPopup(+Cell, 제안 5), ResultPopup(제안 4) | 승리/패배 결과 각 1장, 패시브/액티브 카드 선택 각 1장, 시너지 단계 2/3 및 3/3 |
| 7 | 폰트(사용자 결정 후) | 전 프리팹의 TMP 폰트 참조 교체 | 폰트 교체 전후 동일 화면 스크린샷 비교, 한글 깨짐(□) 0건 |

데이터·로직 작업(제안 1·2·3·4·5·6·7·9 의 ViewModel/Arg 변경, `MetaProfile.CardPickCounts`, 스테이지 랭킹 클라이언트)은 각 UI 단계 직전에 gameplay-programmer 가 구현하고 test-engineer 가 EditMode 테스트를 추가한다. §4.1·§4.2 의 구조 표는 승인된 뒤에만 구현한다.

각 단계 완료 기준: UnityMCP `editor_execute_menu` 로 EditMode 테스트(`Lair/Tests/Run EditMode Tests`) 통과 + 단계 표의 스크린샷 확인. 프리팹 재생성 빌더는 단계마다 삭제.

---

## 6. 위험·열린 질문

### 6.1 위험

| 위험 | 완화 |
|---|---|
| 기존 프리팹의 `[SerializeField]` 참조 깨짐 | GameObject 이름 변경·삭제 금지(§1 표의 삭제 항목 `BodyText` 만 예외이며 코드 필드 제거와 한 세트). 프리팹을 새로 찍을 때는 같은 GUID 를 유지하도록 **기존 프리팹 파일을 덮어쓰지 말고 열어 수정**(빌더가 새 프리팹을 만들면 GUID 가 바뀌어 씬·Addressables 참조가 끊긴다). 각 단계 후 Missing Reference 0건 확인 |
| 규모 (프리팹 32종) | §5 로 7단계 분할. 각 단계는 독립 커밋·독립 검증. 1단계(스프라이트)가 끝나면 나머지는 서로 의존이 약해 병렬 가능 |
| 해상도 가정: 시안 960x540 대 실제 Canvas | Village·Battle 씬 CanvasScaler = Scale With Screen Size, 기준 **1280x720**, Match 0.5 (`Village.unity`, `Battle.unity`). Loading 씬만 1920x1080. 시안 배율은 4/3(§2.1). 1280x720 과 1920x1080(×1.5)에서는 도트 1칸이 정수 물리 픽셀(4 / 6)이지만 그 외 해상도(예 1366x768)는 배율이 정수가 아니라 도트 폭이 1픽셀 차이로 들쭉날쭉할 수 있다. Point 필터로 흐림은 막지만 완벽하지 않다. Loading 씬은 이번 32종에 UI 프리팹이 없으면 영향 없음(ConfirmPopup/ToastView 가 Loading 에서 표시되는지 구현 시 확인) |
| 9-slice 코너 계단 | 스프라이트를 늘렸을 때 바깥 코너 투명 픽셀은 border 안쪽이라 늘어나지 않는다. 다만 Sliced 로 표시하는 Image 크기가 border 합(예: 패널 6칸=24 ref)보다 작으면 깨진다 — 최소 크기 규칙: 패널 ≥ 8칸(32 ref), 버튼 폭 ≥ 8칸 |
| 글로우 표현 | 시안의 blur 글로우(box-shadow blur)는 스프라이트로 재현하지 않는다. 글로우 대상(구매 가능·선택 카드·최대 레벨·호버)은 **Px_Ring 링을 1칸 바깥에 alpha 0.35 로 한 겹 더** 얹는 방식으로 대체한다 |
| 호버 카드 이동 | 카드 위 10px 이동 + 글로우는 스프라이트가 아니라 연출이다. 기존 카드 선택 연출이 있으면 유지, 없으면 이번 범위에서 링 tint 강조만 하고 위 이동은 제외한다 |
| 사선 패턴 Dim | 시안의 마을 팝업 Dim 사선 패턴은 구현하지 않는다(§2.4) |

### 6.2 사용자 결정이 필요한 열린 질문

| # | 질문 | 선택지 (권장) |
|---|---|---|
| 1 | **폰트 교체 범위** (§3) | A Galmuri11 정적 / B Galmuri11 동적 / C Noto 유지. 라이선스(OFL, 파일·문구 동봉)·TMP 아틀라스 재생성·크기 11배수 제약을 수용하는가. 미답 시 C 로 진행 |
| 2 | **카드 픽 횟수 저장** (§4.1) | 권장: `MetaProfile.CardPickCounts` 추가(구조 표 승인 필요) / 제안 7 보류 |
| 3 | **스테이지별 랭킹** (§4.2) | 권장: A 새 컬렉션 `stageLeaderboard`(콘솔에서 규칙·복합 인덱스 준비 필요, 백필 안 함) / C 스테이지 탭 보류. 콘솔 작업 담당·시점 확인 필요 |
| 4 | 카드 프리팹 `CardView_0~2` 의 종류 색 | 패시브=소울 프레임·soul 버튼, 액티브=금 프레임·gold 버튼(시안 그대로). 이의 없으면 확정 |
| 5 | 결과 팝업의 영주 레벨 업·도전과제 달성 줄 (시안에 없음) | 권장: 기존 `RewardText` 를 항목 줄 아래에 남겨 이 두 줄만 표시(기능 손실 없음). 시안 순수 형태를 원하면 삭제(도전과제 달성 알림 사라짐) |
| 6 | 전투 HUD 타이머 경고 색 | 시안 `timer.warn` 은 붉은 글씨 클래스만 있고 임계가 없다. 권장: 남은 시간 30초 이하일 때 붉은 글씨(`#ff8a8d`) — 검산: 30초 = 액티브 카드 1주기 |
| 7 | 마을 퀘스트 버튼 빨간 점 | 데이터 근거 없음 — 권장: 구현하지 않음(계정 충돌 점만 유지) |
| 8 | 빌드 바의 빈 슬롯 개수 | 권장: 시안대로 패시브 최소 6칸 / 액티브 최소 3칸을 빈 빗금 슬롯으로 채우고, 획득 시 채워져 최소치를 넘으면 늘어남 |

---

## 7. 구현 요청사항 (gameplay-programmer 용)

### Enum
- 신규 없음. 스프라이트는 프리팹 직접 참조이며 `CHMResource` 키 로드가 아니다(Rule 03 §2 범위 밖). 토스트 종류는 `CommonEnum.cs` 의 `EToastKind { Info, Warning, Error }` 로 추가(단일 시스템 내부 사용이면 해당 파일 내부 Enum, 여러 호출부에서 참조하므로 공용 Enum 이 맞다).

### Interface
- 신규 필수 없음. 마을 전적 패널·결과 팝업 값은 기존 ViewModel/Arg 확장으로 충당한다.

### 에셋 키 / 파일명
- 스프라이트: `Assets/_Lair/Art/Sprites/UiDot/Px_*.png` (§2.3 파일명 그대로, 대소문자 일치). 12x12 글리프 6종은 `Px_Glyph_Castle`, `Px_Glyph_Shop`, `Px_Glyph_Book`, `Px_Glyph_Scroll`, `Px_Glyph_Tomb`, `Px_Glyph_Crown`.
- 덮어쓰기: `Assets/_Lair/Art/Sprites/SynergyIcons/{TANK,DPS,SWARM,DEBUFF}.png`
- 프리팹은 기존 32종 파일명 유지(신규 프리팹 파일 없음).

### 데이터 스키마 (승인 게이트 대상)
- `MetaProfile.CardPickCounts : List<CardPickCountEntry>` (`CardId` string, `Count` int), `MetaProfile.Version` 3 → 4, `CopyFrom` 에 복사 추가. (§4.1)
- Firestore `stageLeaderboard/{stage}_{uid}` : `uid`, `stage`, `displayName`, `clearTimeMs`, `hero`. (§4.2, 콘솔 규칙·인덱스는 이 레포 범위 밖)

### Arg / ViewModel 필드 (신규 저장 없음)
- `ResultPopupArg`: `ClearTime`(float 초), `IsNewBest`(bool), `HeroHpRatio`(float 0~1)
- `CardSelectionPopupArg`: 종류(패시브/액티브), 패시브 트리거 HP 구간 %(정수, 예: 60)
- `CloudPopupArg`: 클라우드 프로필의 영주 Lv, 소울 (충돌 대기일 때)
- 마을 전적 패널용: 선택 스테이지의 `Wins`, `Runs`, `BestClearTime`, 영웅 이름·단계
- `ShowToast` 호출에 `EToastKind` 인자 추가 (기본값 Info)
- 랭킹: 스테이지 제출·조회(`SubmitStageScoreAsync`, `GetStageTopAsync(stage, top)`, `GetMyStageRankAsync(stage)`) — 실패 시 기존 규약대로 빈 리스트/false

---

## 8. Self-Review 메모

- 미정 마커·애매한 권유·두 갈래 위임: §3 폰트와 §6.2 는 "사용자 선택 필요" 로 명시했고, 미답 시 기본 진행(C안, 각 권장안)을 적었다.
- 명명 일관성: `CardPickCounts`/`CardPickCountEntry`, `stageLeaderboard`, `StageTab1~5`/`OverallTab`, `SegDivider1~9`, `EToastKind` 를 본문·표·구현 요청사항에서 동일 표기로 사용.
