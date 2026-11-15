# 공통 UI 아트 교체 (UI Art Refresh) - 기능 기획서 (전면 개정 3판: 화면 단위 전용 고해상도 GPT 아트 세트)

> **개정 이력**: 1판(2026-11-09)은 "5~9px 소형 도트 117장을 같은 파일명·크기로 제자리 덮어쓰기" 계획이었다. 2026-11-10 사용자 방침("Claude 가 도트로 그린 UI 는 전부 GPT 아트로 바꾼다, 도감만 먼저")과 2026-11-15 결정("도감 세트 우선 확정, 세부는 다음에 수정 / 다른 UI 진행")으로 **1판은 폐기**하고, 도감과 같은 방식인 **화면(팝업) 단위 전용 고해상도 GPT 아트 세트**를 새로 만드는 계획으로 개정한다. 파일럿 6장(`ui-art-refresh-pilot.md`)·117장·`Px_Solid` 제외·"같은 파일명 제자리 교체" 전제는 모두 폐기.
> 입력: 도감 세트 `docs/design/monster-codex-ui-art.md`(확정) · S0 시트 `.mockups/assets/codex-s0/S0_popup.png`·`S0_elements.png` · 도감 스프라이트 36장 `Assets/_Lair/Art/Sprites/CodexUI/` · `Assets/_Lair/Art/UI/*.prefab` 43개(+ 씬 3)의 스프라이트 GUID 참조 조사 · 마을 HUD `Assets/_Lair/Scripts/UI/Village/VillageHud.cs`·`.mockups/village-hud-redesign.html`
> **사용자용 가이드**: `docs/design/ui-art-refresh.html` · **마을 HUD 목업**: `.mockups/village-hud-art.html`

## 0. 헤더

- **목표**: 게임의 모든 UI 화면을 도감 세트와 같은 화풍·같은 제작 파이프라인의 **고해상도 GPT 아트 세트**로 교체한다. 팝업 틀·버튼·칩·바·아이콘·화면 고유 장식까지 화면 단위로 전용 스프라이트를 신규 제작하고, 여러 화면이 공유하는 요소는 **공통 세트 `CommonUI*`** 로 한 번만 만든다.
- **검증 가설**: (1) 도감 세트의 화풍을 기준으로 삼으면 화면마다 따로 만들어도 한 세트로 보이는가. (2) 공통 세트를 먼저 만들어 공유하면 화면 전용 제작량이 줄어드는가. (3) GPT 호출 한도(과거 두 번 걸림) 안에서 화면 단위 순차 교체가 현실적인가.
- **현재 단계 범위 적합성**: **범위 밖 -> 사용자 승인으로 §8 "공통 UI 아트 교체 허용" 항목을 새 방침으로 수정**(§8 에 반영, 2026-11-15). 신규 화면·신규 UI 기능·프리팹/데이터 구조 변경은 계속 금지.
- **핵심 메커니즘**: ① 도감 세트(`Codex*` 36장)를 스타일 기준으로 확정 ② 공통 요소를 `CommonUI*` 세트로 1회 제작 ③ 화면 단위 전용 세트(`VillageHud*` 등)를 우선순위대로 제작 ④ 프리팹은 Image 의 `Sprite` 참조만 교체(데이터·프리팹 구조 불변, 코드는 `[SerializeField]` 슬롯 추가 수준까지).

## 1. 결정 요약

| ID | 결정 | 근거 |
|---|---|---|
| U1 | 방식 = **화면(팝업) 단위 전용 고해상도 신규 스프라이트 세트**. 기존 `Px_*`·`WorldMap*` 등은 **제자리 덮어쓰지 않고**, 세트가 완성된 화면의 프리팹 참조만 새 스프라이트로 바꾼다. 교체가 끝나 참조 0건이 된 구 스프라이트는 마지막에 일괄 삭제 | 사용자 방침. 소형 도트(5~9px) 조각을 늘려 쓰는 한계 탈피 |
| U2 | 스타일 기준 = **도감 세트**(§2). 화풍·해상도(화면 x 3, PPU 300)·후처리·키잉·생성 도구를 그대로 따른다 | 사용자 확정 |
| U3 | 공통 요소(탭·닫기·칩·별·진척 바·스크롤바·팝업 프레임·버튼·배지·체크/X/잠금·후광 등)는 **`CommonUI*` 세트**로 먼저 제작해 모든 팝업이 공유(§4). 화면 전용 요소만 `<화면>*` 세트로 분리 | 중복 제작 방지, 일관성 |
| U4 | 첫 세트 = **마을 HUD**(§5). 가장 눈에 띄는 메인 화면 | 사용자 지시 |
| U5 | 마을 HUD 세트는 **데이터 구조·프리팹 구조 변경 없이 스프라이트 참조만 변경**. 코드 변경은 `[SerializeField]` 슬롯 추가 수준까지 | 사용자 지시 |
| U6 | 도감 `Codex*` 중 공통화 가능한 것은 **이관 후보로 표시만 하고 지금은 이동·리네임 금지**(§4.2). 도감 세부 개선은 후속 | 사용자 지시 |
| U7 | 월드맵 일러스트성(메달·달·실루엣·안개·비네트)·몬스터/영웅 아트·폰트·카드 일러스트·유물 콘텐츠 아이콘은 이번 계획 범위 밖(별도 승격) | 1판 결정 유지 |

## 2. 도감 세트 = 스타일 기준 (확정, 세부는 다음에 수정)

| 항목 | 내용 |
|---|---|
| 스타일 시트 | S0: `.mockups/assets/codex-s0/S0_popup.png`(팝업 전체 시안) · `S0_elements.png`(대표 요소). 모든 후속 세트는 이 시트를 **참조 이미지로 첨부**해 생성 |
| 화풍 | 몬스터 v4/v5 시트와 같은 세계관의 클린 하이브리드 픽셀 아트: 굵은 near-black 외곽선(`#0a0810`, 화면 2px), 면 2~3톤, 위·왼쪽 밝은 베벨 + 아래·오른쪽 어두운 베벨, 석판 남색(`#2a3552`/`#1a2132`/`#121827`), 금 3톤(`#f7c64a`/`#c8962a`/`#8a6a2c`), 등급색 5(회·파랑·보라·금·주황), 모서리 장식은 보더 링 안쪽에만 |
| 생성 도구 | Codex CLI 이미지 생성, 모델 **gpt-6-sol** |
| 36장 구성 방식 | 요소 3~6개를 한 시트(스타일 참조 첨부)로 생성 -> 요소 단위로 크롭 -> 키잉 -> 후처리 -> 크기·9-slice 보더에 맞춰 개별 PNG. 도감 36장은 `Assets/_Lair/Art/Sprites/CodexUI/` 에 있음(틀 4 · 탭·버튼·칩 6 · 칸 12 · 배지·아이콘 6 · 바·미리보기 4 · 연출·스크롤 4) |
| 해상도·임포트 | 화면 표시 크기(1280x720 기준 px)의 **3배**, Sprite(2D and UI) Single, **PPU 300**, Bilinear, 밉맵 끔, 프레임류 압축 없음(후광·미리보기는 High Quality), alphaIsTransparency 켬, 9-slice 보더는 스프라이트별 지정 |
| 키잉 | 투명이 필요한 요소 = **마젠타 배경(`#FF00FF`) 크로마키 + defringe**. 후광·링·스파클 같은 알파 그라데이션 요소 = **흰색 on 순검정 생성 후 밝기 -> 알파 변환**(색은 순백, 런타임 틴트) |
| 후처리 | 크롭 -> 키잉 -> 화면 해상도(원본 /3)로 축소 -> 팔레트 스냅 -> 알파 이진화(광원 제외) -> 3배 Nearest 확대 -> 9-slice 검증(극단 크기 늘림) -> 틴트 검증 -> 어두운/밝은 배경 번짐 확인 -> 실제 크기 가독성 확인(상세는 `monster-codex-ui-art.md` §7) |
| 등급색 프레임 | 직접 그린 5장 + 미해금 1장(회색조 틴트 아님). 틴트는 해금 펄스·링·스파클·칩 색 띠·후광 등 "모양 차이가 필요 없는" 요소에만 |
| **알려진 개선 과제(후속)** | ① **스크롤바 폭**: `CodexScrollTrack`/`Thumb`(화면 12px)이 실제 스크롤뷰에서 너무 좁거나 어긋남 -> 폭 재조정 후 재생성 ② **조건 줄 좌표**: 상세 패널의 해금 조건 줄(체크/X 아이콘·글자)의 좌표·간격이 목업과 어긋남 -> 프리팹 좌표 수정 ③ 기타 세부(프레임 모서리 비율, 소형 아이콘 가독성, 늘림 테스트 결과)는 다음 세부 수정에서 처리. 도감 세트는 **"우선 확정, 세부는 다음에 수정"** 상태이며 이 과제들이 해결돼야 공통 세트 이관(§4.2)을 시작한다 |

## 3. 대상 화면 인벤토리 (Art/UI 프리팹 + 씬, 스프라이트 GUID 참조 기준)

조사: `Assets/_Lair/Art/UI/*.prefab` 43개 + `Assets/_Lair/Scenes/*.unity` 3개를 스프라이트 `.meta` GUID 로 Grep(`Px_*`·`WorldMap*`·`VillageHud*`·`BattleHud*`·`BuildModal*`·`CardSelect*`·`SynergyModal*`·`Button_*`·`VillageIcons/*` 등 UI 스프라이트 약 128종 대상).
표기 약어(현재 사용 스프라이트 군): **틀** = `WorldMapFrame`(금테, 16파일)+`WorldMapCornerGold`(모서리, 17파일, 팝업당 4)+`WorldMapGradientH/V`(27파일)+`UISoftGlow`(28파일) · **패널** = `Px_Panel`(7)·`Px_PanelDark`(12)·`Px_PanelSunk`(8) · **버튼** = `Px_Btn`(3)·`Px_BtnGold/Blood/Soul/Off`(6)·`VillageHudBtn/Off/Cta`·`CardSelectPickBtn*` · **바** = `Px_BarBg/Fill`·`BattleHud*Fill`·`VillageHudXpFill` · **링·배지** = `Px_Ring`(10)·`Px_CardRing`·`Px_Dot`·`VillageHudChip`·`WorldMapBadge/StarBadge` · **아이콘** = `VillageIcons/Icon_*·Glyph_*`·`Px_Glyph_*`·`Px_Lock`·`Px_SoulCoin` · **고유** = 화면 전용 스프라이트.

| # | 화면 | 프리팹(씬) | 현재 사용 스프라이트(군, 근거: GUID 매칭) | 세트 분류 |
|---|---|---|---|---|
| 1 | **마을 HUD** | `VillageHud.prefab` | **약 90건**: UISoftGlow(약 27)·VillageHudBtn(약 14)·VillageHudGem·GradientH/V·CornerGold(7)·Icon_*/Glyph_*·Px_Glyph_* 8·Px_PanelDark·Px_Panel·Px_Solid·VillageHudChip·CardRing·AvatarRing·WorldMapFrame·PaperInset·Cta·XpFill·BarBg·BarFrame·MedalOpen/Boss·Lock·SoulCoin·Hatch·Vignette·ArrowR·RedDot | **VillageHud\*** (전용) + CommonUI |
| 2 | 상점 | `ShopPopup` · `ShopItemCell` | 틀 + 버튼(BtnGold/Blood/Soul) + 링 + Px_SoulCoin·Hatch + 바 | Shop\* + CommonUI |
| 3 | 편성 | `FormationPopup` · `FormationSpeciesRow` · `FormationNestTile` | 패널(Px_Panel/Dark/Sunk)·버튼(Px_Btn/BtnGold)·Plaque·Divider·BarBg/Fill·Hatch | Formation\* + CommonUI |
| 4 | 월드맵·영웅 선택 | `WorldMapPopup` · `WorldMapNode` · `WorldMapChapterTab` · `HeroSelectPopup` · `HeroSelectCell` | 틀 + 월드맵 전용(Medal·Moon·Silhouette·Path·Pointer·NodeSelRing·Fog·Vignette 18파일 58건) + 난이도 아이콘(DiffIcon) + 로딩/보스 바 + 패널·버튼 | WorldMap\*(월드맵 일러스트성 제외) + CommonUI |
| 5 | 결과창 | `ResultPopup` | 틀 + 버튼 + 링 + 바(SmallFrame) + Hatch·Rays·SoulCoin·ArrowR | Result\* + CommonUI |
| 6 | 데일리 | `DailyPopup` | 패널·PanelSunk·버튼 4종·Plaque·CardRing | Daily\* + CommonUI |
| 7 | 무한의 탑 | `TowerPopup` · `TowerFloorCell` | 패널·버튼 4종·Plaque·Divider·Dot | Tower\* + CommonUI |
| 8 | 유물 | `RelicPopup` · `RelicCell` | 틀 + 링 + 칩 + Hatch·Lock·SoulCoin + 등급색 테두리 | Relic\* + CommonUI |
| 9 | 랭킹 | `RankingPopup` · `RankingCell` | 틀 + 칩 + 바 | Ranking\* + CommonUI |
| 10 | 기록 | `RecordsPopup` · `RecordsStageCell` | 틀 + 칩 + 바 | Records\* + CommonUI |
| 11 | 클라우드(계정) | `CloudPopup` | 틀 + 칩 + 버튼 | Cloud\* + CommonUI |
| 12 | 확인 팝업 | `ConfirmPopup` | 틀 + 칩 + 버튼 | CommonUI 만(전용 없음) |
| 13 | 카드 선택(전투) | `CardSelectionPopup` | CardSelectFrame·Chip·ArtRing·KindInset·FadeLine·PickBtnGold/Soul + PanelSunk + 틀 | CardSelect\* + CommonUI |
| 14 | 시너지 모달 | `SynergyModalPopup` · `SynergyModalCell` | SynergyModalBadge·CellBg·Halo + 틀 | Synergy\* + CommonUI |
| 15 | 빌드 모달 | `BuildModalPopup` · `BuildModalCardCell` · `BuildIconCell` · `BuildSynergyPanel` · `BuildSynergyCell` | BuildModalChipFrame·CloseGlyph·ScrollThumb + SmallFrame + 틀 + PanelSunk/Dark + 링 | Build\* + CommonUI |
| 16 | 전투 HUD | `BattleHud` · `HpBar` · `SpawnerStatusPanel`/`SpawnerStatusCell` (+`Loading.unity`) | BattleHud* 7종(BossFrame/Fill/FillLow·Gauge·Mini·Chip·Track)·SmallFrame·Px_BarBg/Fill·Ring·Hatch·PanelDark | BattleHud\* + CommonUI |
| 17 | 퀘스트 | `QuestPopup` · `QuestCell` | 틀 + 칩 + 바 | Quest\* + CommonUI |
| 18 | 영주 레벨 | `LordLevelPopup` · `LordRewardCell` | 틀 + 칩 + 바 | LordLevel\* + CommonUI |
| 19 | 도감 | `CodexPopup` · `CodexCell` · (`CodexMonsterCell` 예정) | **이미 `Codex*` 세트로 교체 완료(확정)** | Codex\*(완료) |
| 20 | 토스트·스킬 해금 배너 | `ToastView` · `SkillUnlockBanner` | 틀 + 칩 | Notice\* + CommonUI |

**미사용 후보**: `Button_Square/Wide_*` 5장·`Px_Tab`·`Px_TabOn`·`Px_Star`·`Px_Badge_*` 3장·`HpBar`·`HpBarBackground`·`Loading_Bar_Full`·`TitleImage`·`Game_Icon` 등은 프리팹/씬 GUID 참조가 0건이다(교체 대상 아님, 코드·SO 참조는 후속 확인). 교체 완료 후 참조 0건이 된 구 스프라이트는 정리 후보로 모아 **마지막에 일괄 삭제**한다(사용자 확인 후).

## 4. 세트 구조: 공통 세트 `CommonUI*` + 화면 전용 세트

### 4.1 공통 세트 `CommonUI*` (한 번 제작, 모든 팝업이 공유) - 32장(신규 생성 8장)

| 분류 | 스프라이트 | 장수 | 출처 |
|---|---|---|---|
| 팝업 틀 | `CommonUIPanelFrame`·`CommonUIPanel`·`CommonUITitlePlaque`·`CommonUIDivider`·`CommonUIPanelSunk`(파인 홈 패널) | 5 | 4 이관 후보(`CodexPanelFrame`·`CodexPanel`·`CodexTitlePlaque`·`CodexDivider`) + 1 신규 |
| 탭·닫기·칩 | `CommonUICloseBtn`·`CommonUITabOn`·`CommonUITabOff`·`CommonUIChip`·`CommonUIChipOn`·`CommonUIChipBand` | 6 | 6 이관 후보(`CodexCloseBtn`·`CodexTabOn/Off`·`CodexChip`·`CodexChipOn`·`CodexChipBand`) |
| 버튼 | `CommonUIBtn`·`CommonUIBtnGold`·`CommonUIBtnBlood`·`CommonUIBtnSoul`·`CommonUIBtnOff` | 5 | **신규 5** |
| 배지·표시 | `CommonUIStar`·`CommonUINewBadge`·`CommonUINewDot`·`CommonUIBadgeRed`·`CommonUIBadgeSoul`·`CommonUITag` | 6 | 4 이관 후보(`CodexStar`·`CodexNewBadge`·`CodexNewDot`·`CodexTag`) + 2 신규 |
| 아이콘 | `CommonUICheck`·`CommonUICross`·`CommonUILock` | 3 | 3 이관 후보(`CodexCheck`·`CodexCross`·`CodexLock`) |
| 바·스크롤 | `CommonUIProgressTrack`·`CommonUIProgressFill`·`CommonUIScrollTrack`·`CommonUIScrollThumb` | 4 | 4 이관 후보(`CodexProgressTrack`·`CodexProgressFill`·`CodexScrollTrack`·`CodexScrollThumb`) |
| 연출 | `CommonUIGlow`·`CommonUIRing`·`CommonUISpark` | 3 | 3 이관 후보(`CodexGlow`·`CodexUnlockRing`·`CodexSpark`) |
| 합계 | | **32** (표의 항목 수 5+6+5+6+3+4+3 = 32) | 이관 후보 24 + 신규 8 |

검산: 이관 후보 24 = 4 + 6 + 4 + 3 + 4 + 3. 신규 8 = 패널 파인홈 1 + 버튼 5 + 배지 2. **공통 세트 = 32장, 이 중 새로 생성하는 것은 8장**.

### 4.2 도감 `Codex*` 이관 후보 (표시만, **지금은 이동·리네임 금지**)

| 도감 스프라이트 | 이관 대상 | 상태 |
|---|---|---|
| `CodexPanelFrame`·`CodexPanel`·`CodexTitlePlaque`·`CodexDivider` | `CommonUIPanelFrame`·`CommonUIPanel`·`CommonUITitlePlaque`·`CommonUIDivider` | 이관 후보 |
| `CodexCloseBtn`·`CodexTabOn/Off`·`CodexChip`·`CodexChipOn`·`CodexChipBand` | `CommonUICloseBtn`·`CommonUITabOn/Off`·`CommonUIChip`·`CommonUIChipOn`·`CommonUIChipBand` | 이관 후보 |
| `CodexStar`·`CodexNewBadge`·`CodexNewDot`·`CodexTag` | `CommonUIStar`·`CommonUINewBadge`·`CommonUINewDot`·`CommonUITag` | 이관 후보 |
| `CodexCheck`·`CodexCross`·`CodexLock` | `CommonUICheck`·`CommonUICross`·`CommonUILock` | 이관 후보 |
| `CodexProgressTrack`·`CodexProgressFill`·`CodexScrollTrack`·`CodexScrollThumb` | `CommonUIProgressTrack`·`CommonUIProgressFill`·`CommonUIScrollTrack`·`CommonUIScrollThumb` | 이관 후보(스크롤바 폭 개선 후) |
| `CodexGlow`·`CodexUnlockRing`·`CodexSpark` | `CommonUIGlow`·`CommonUIRing`·`CommonUISpark` | 이관 후보 |
| `CodexCellFrameG1..G5`·`CodexCellFrameLocked`·`CodexCellFramePulse`·`CodexCellBg`·`CodexCellBgLocked`·`CodexRowHeader`·`CodexTierHeader`·`CodexPreviewBg` | (이관 없음 - 도감 전용 유지. 등급 프레임은 유물 등급 등 다른 화면 재사용 가능성만 후속 검토) | 도감 전용 |

**이관 시점**: 도감 후속 개선(§2 개선 과제) 완료 후. 이관은 **파일 이동·리네임(GUID 유지)** 으로 하면 도감 프리팹 참조가 보존되므로 그때 처리한다. 지금은 `CommonUI*` 신규 8장만 생성하고, 이관 후보 24장은 도감 이름 그대로 두며 다른 화면이 필요하면 **도감 파일을 직접 참조하지 않고** 이관 이후 이름을 쓴다(그래서 이관 전에는 다른 화면의 공통 요소 교체를 보류).

### 4.3 화면 전용 세트와 공통 세트 사용 원칙

- 한 화면 프리팹의 Image 는 ① `CommonUI*`(틀·탭·칩·버튼·배지 등) ② 그 화면 전용 스프라이트(`<화면>*`) 둘 중 하나만 참조한다. 같은 모양을 화면마다 따로 그리지 않는다.
- 전용 세트 이름 접두어 = 화면명(`VillageHud*`, `Shop*`, `Formation*` 등). 도감만 예외로 `Codex*`.
- 모든 세트 폴더: `Assets/_Lair/Art/Sprites/<세트명>/`(예: `CommonUI/`, `VillageHudUI/`).

## 5. 첫 세트: 마을 HUD (`VillageHud*`)

### 5.1 범위·원칙

- **프리팹 구조·데이터 구조 변경 없음**: `VillageHud.prefab` 의 Image `Sprite` 참조와 색을 바꾸는 것이 전부다. 코드는 기존 `[SerializeField]` 슬롯(예: `_lordXpFill` Image) 그대로, 필요하면 **`[SerializeField] Sprite` 슬롯 추가 수준까지**만 허용(예: 잠금 상태 스프라이트 스왑).
- 마을 HUD 가 쓰는 공통 요소(잠금·NEW 점·닫기·별·배지)는 `CommonUI*` 신규 제작 후 참조. 마을 HUD 에서만 쓰는 요소는 `VillageHud*` 로 제작.
- 레이아웃은 승인된 `village-hud-redesign.html` 규격 그대로: 1280x720, 상단바 h88 · 메뉴 112x112 2열 x 3행 (16,104) 피치 128 · 전적 256x216 (1008,104) · ◀▶ 48x68 (416,326)/(816,326) · 침입자 카드 340x324 (924,380) · 영웅 버튼 100x72 (470,632) · 방어하기 232x72 (578,632). 현재 프리팹에는 랭킹·계정·유물·탑·데일리 버튼이 추가되어 있다(`VillageHud.cs`).

### 5.2 스프라이트 목록 (35장, 크기 = 화면 x 3, PPU 300)

| # | 파일 | 화면 크기 -> 원본 | 9-slice 보더(L,B,R,T, 원본 px) | 용도 |
|---|---|---|---|---|
| 1 | `VillageHudTopBar` | 상단바 배경 1280x88 -> 늘림용 240x264(보더 108,0,108,0 가로 늘림) | 108,0,108,0 | 상단바 바탕 |
| 2 | `VillageHudAvatarFrame` | 60x60 -> 180x180 | 없음 | 닉네임 옆 아바타 링 |
| 3 | `VillageHudXpFrame` | 170x16 -> 510x48 | 24,24,24,24 | 영주 경험치 바 틀 |
| 4 | `VillageHudXpFill` | 170x16 -> 510x48 | 없음(Filled) | 경험치 채움 |
| 5 | `VillageHudCurrencyPill` | 232x48 -> 696x144 | 72,72,72,72 | 소울 재화 알약 |
| 6 | `VillageHudMenuTile` | 112x112 -> 336x336 | 60,60,60,60 | 좌 메뉴 타일(활성) |
| 7 | `VillageHudMenuTileOff` | 112x112 -> 336x336 | 60,60,60,60 | 메뉴 타일(잠금/비활성) |
| 8 | `VillageHudMenuTileDailyNew` | 112x112 -> 336x336 | 60,60,60,60 | 데일리 "오늘 도전 남음" 금색 테두리 |
| 9 | `VillageHudAccountBtn` | 112x52 -> 336x156 | 48,48,48,48 | 계정 버튼 |
| 10 | `VillageHudSortieBtn` | 232x72 -> 696x216 | 90,90,90,90 | 방어하기(붉은 CTA) |
| 11 | `VillageHudSortieBtnOff` | 232x72 -> 696x216 | 90,90,90,90 | 방어하기 잠금 |
| 12 | `VillageHudSortieGlow` | 280x110 -> 840x330 | 없음 | 방어하기 붉은 맥동 글로우(알파, 붉은색 틴트) |
| 13 | `VillageHudSortieGloss` | 220x28 -> 660x84 | 없음 | 방어하기 상단 광택(알파) |
| 14 | `VillageHudHeroBtn` | 100x72 -> 300x216 | 60,60,60,60 | 영웅 선택 버튼 |
| 15 | `VillageHudStageArrowL` | 48x68 -> 144x204 | 없음 | ◀ 이전 스테이지 |
| 16 | `VillageHudStageArrowR` | 48x68 -> 144x204 | 없음 | ▶ 다음 스테이지 |
| 17 | `VillageHudStagePlate` | 268x31 -> 804x93 | 60,0,60,0 | "STAGE N" 제목판 |
| 18 | `VillageHudIntruderCard` | 340x324 -> 1020x972 | 108,108,108,108 | 침입자 카드 틀 |
| 19 | `VillageHudPortraitFrame` | 196x208 -> 588x624 | 90,90,90,90 | 침입자 포스터/초상 틀 |
| 20 | `VillageHudStageDot` | 14x14 -> 42x42 | 없음 | 스테이지 점(클리어) |
| 21 | `VillageHudStageDotOff` | 14x14 -> 42x42 | 없음 | 스테이지 점(미클리어) |
| 22 | `VillageHudStageDotBoss` | 18x18 -> 54x54 | 없음 | 보스 스테이지 점 |
| 23 | `VillageHudRecordPanel` | 256x216 -> 768x648 | 90,90,90,90 | 전적 패널 |
| 24 | `VillageHudRecordBullet` | 8x8 -> 24x24 | 없음 | 전적 줄 불릿 |
| 25 | `VillageHudIconCastle` | 56x56 -> 168x168 | 없음 | 영주성 아이콘 |
| 26 | `VillageHudIconShop` | 56x56 -> 168x168 | 없음 | 상점 아이콘 |
| 27 | `VillageHudIconCodex` | 56x56 -> 168x168 | 없음 | 도감 아이콘 |
| 28 | `VillageHudIconQuest` | 56x56 -> 168x168 | 없음 | 퀘스트 아이콘 |
| 29 | `VillageHudIconRecords` | 56x56 -> 168x168 | 없음 | 기록 아이콘 |
| 30 | `VillageHudIconRanking` | 56x56 -> 168x168 | 없음 | 랭킹 아이콘 |
| 31 | `VillageHudIconRelic` | 56x56 -> 168x168 | 없음 | 유물 아이콘 |
| 32 | `VillageHudIconTower` | 56x56 -> 168x168 | 없음 | 탑 아이콘 |
| 33 | `VillageHudIconDaily` | 56x56 -> 168x168 | 없음 | 데일리 아이콘 |
| 34 | `VillageHudIconMap` | 56x56 -> 168x168 | 없음 | 지도(월드맵) 아이콘(영웅 버튼 옆) |
| 35 | `VillageHudIconSoul` | 24x24 -> 72x72 | 없음 | 소울 재화 아이콘 |

검산: #1~#5 상단바 5, #6~#9 메뉴·계정 4, #10~#14 출격·영웅 5, #15~#17 스테이지 3, #18~#22 침입자 카드·점 5, #23~#24 전적 2, #25~#34 메뉴 아이콘 10, #35 재화 아이콘 1 = 5 + 4 + 5 + 3 + 5 + 2 + 10 + 1 = 35.
`CommonUI*` 로 해결되는 것: 잠금 오버레이(`CommonUILock`)·NEW/알림 점(`CommonUINewDot`)·별(`CommonUIStar`, 위협도 ★)·글로우 일반형(`CommonUIGlow`).

### 5.3 프리팹 참조 교체 (VillageHud.prefab)

| 현재 스프라이트 | 위젯 | 교체 |
|---|---|---|
| `VillageHudBtn`(약 14건)·`VillageHudBtnOff` | 메뉴 타일·각종 버튼 배경 | `VillageHudMenuTile` / `VillageHudMenuTileOff`, 계정 = `VillageHudAccountBtn` |
| `VillageHudCta` | 방어하기 | `VillageHudSortieBtn`(+`Off`) |
| `VillageHudGem`·`VillageHudChip` | 소울 알약·칩 | `VillageHudCurrencyPill` |
| `Px_PanelDark`·`Px_Panel` | 전적·침입자 카드·상단 바탕 | `VillageHudRecordPanel`·`VillageHudIntruderCard`·`VillageHudTopBar` |
| `VillageHudBarFrame`·`BarBg`·`XpFill` | 경험치 바 | `VillageHudXpFrame`·`VillageHudXpFill` |
| `VillageHudAvatarRing`·`Px_CardRing` | 아바타 링 | `VillageHudAvatarFrame` |
| `VillageHudPaperInset` | 침입자 포스터 | `VillageHudPortraitFrame` |
| `WorldMapMedalOpen`·`WorldMapMedalBoss` | 스테이지 점 | `VillageHudStageDot*`(월드맵 팝업의 메달은 별도, 변경 없음) |
| `Px_ArrowR`·`ArrowL`류 | ◀▶ | `VillageHudStageArrowL/R` |
| `Icon_*`·`Glyph_*`·`Px_Glyph_*` | 메뉴 아이콘 | `VillageHudIcon*` 10종 + 재화 `VillageHudIconSoul` |
| `Px_Lock` | 잠금 오버레이 | `CommonUILock`(신규 공통 세트 이관 후) 또는 이관 전 임시 `Px_Lock` 유지 |
| `WorldMapFrame`·`CornerGold`·`GradientH/V`·`UISoftGlow` | 장식 틀·광원 | 마을 HUD 는 틀 장식이 거의 필요 없는 메인 화면 - 상단바·패널 스프라이트에 장식 포함시켜 개별 참조 제거. 광원은 `CommonUIGlow`(이관 후) 또는 기존 `UISoftGlow` 유지 |
| `Px_Solid` | 단색 바탕 | **변경 없음**(색만 칠하는 순백, 새 세트에서 제외) |

### 5.4 변경 범위

- **프리팹**: `VillageHud.prefab` 의 Image `m_Sprite` 참조 교체. 위젯 추가·삭제·구조 변경 없음. 위치·크기 변경이 필요한 경우 목업 승인 후 같은 위젯의 `RectTransform` 값 조정만(화풍 변경으로 9-slice 보더 때문에 필요할 때).
- **코드**: 기본 0 줄. 필요 시 `VillageHud.cs` 에 `[SerializeField] Sprite` 슬롯 추가 수준(예: 잠금 상태 스프라이트 스왑)까지.
- **데이터(JSON·SO·MetaProfile)**: 변경 없음.
- **교체 순서**: 신규 8장(`CommonUI*`) 제작 -> 마을 HUD 35장 제작 -> 프리팹 참조 교체 -> 스크린샷 비교.

## 6. 진행 순서·우선순위·공수·GPT 호출 수

### 6.1 추정 방법 (시작값, S0 이후 실측으로 보정)

- 한 번의 시트 생성에 **요소 5개**를 넣는다(도감 36장 제작 경험 기준 가정).
- **GPT 호출 수 = ceil(신규 장수 / 5) + 1(스타일 참조 확인) + 재생성 여유 30%**(올림).
- **공수**는 1인 기준 작업일(생성·후처리·프리팹 참조 교체·스크린샷 검수 포함): 세트당 장수 5~15장 = 1일, 16~25장 = 2일, 26~40장 = 2.5일.
- **사용량 한도**: 이전에 두 번 걸렸다. 한도 수치는 확인되지 않아 **세트당 호출 수를 10 이하로 설계**하고 한 세트씩 진행하며, 한도에 걸린 시점의 누적 호출 수를 기록해 이후 계획을 조정한다. 하루 호출 예산은 사용자 확인 필요(열린 질문 1).

### 6.2 우선순위 표

| 순위 | 세트 | 화면 | 우선 이유 | 신규 장수 | GPT 호출(추정) | 공수(일) |
|---|---|---|---|---|---|---|
| 1 | **VillageHud\*** | 마을 HUD | 가장 눈에 띄는 메인 화면, 사용자 지정 첫 세트 | 35 | 10 | 2.5 |
| 2 | **CommonUI\*(신규 8)** | 공통 | 이후 모든 팝업이 공유(버튼·파인 홈·배지). 이관 24장은 도감 개선 후 | 8 | 3 | 1 |
| 3 | Shop\* | 상점 | 마을에서 자주 방문, 재화 표시 많음 | 14 | 5 | 1 |
| 4 | WorldMap\* | 월드맵·영웅 선택 | 스테이지 진입 핵심 화면(월드맵 일러스트성은 제외) | 24 | 8 | 2 |
| 5 | Result\* | 결과창 | 매 판 마지막에 노출 | 12 | 5 | 1 |
| 6 | Formation\* | 편성 | 승급 후 열리는 화면 | 16 | 6 | 2 |
| 7 | CardSelect\* | 카드 선택(전투) | 전투 중 반복 노출, 고유 스프라이트 많음 | 16 | 6 | 2 |
| 8 | BattleHud\* | 전투 HUD·HpBar·스포너 상태 | 전투 내내 노출 | 28 | 9 | 2.5 |
| 9 | Relic\* | 유물 | 수집 화면 | 12 | 5 | 1 |
| 10 | Daily\* · Tower\* | 데일리·탑 | 엔드게임 콘텐츠 | 20 | 6 | 2 |
| 11 | Build\* · Synergy\* | 빌드·시너지 모달 | 전투 중 보조 화면 | 24 | 8 | 2 |
| 12 | Ranking\* · Records\* · Quest\* · LordLevel\* · Cloud\* | 기타 마을 팝업 | 노출 빈도 낮음, 공통 세트로 대부분 해결 | 38 | 14 | 4 |
| 13 | Notice\* · Confirm | 토스트·배너·확인 | 공통 세트로 대부분 해결 | 7 | 3 | 0.5 |
| - | Codex\* | 도감 | **완료(확정), 후속 개선만** | 36(완료) | - | 개선 0.5 |
| - | CommonUI\* 신규 8장 | 공통 버튼 5·파인 홈 패널 1·배지 2 | **제작·설치 완료(2026-11-15, Codex 3호출)**, 프리팹 참조 교체는 상점 세트와 함께 | 8(완료) | - | - |
| 합계(도감 제외) | | | | 약 254 | 약 88 | 약 24~25일 |

검산(장수): 35 + 8 + 14 + 24 + 12 + 16 + 16 + 28 + 12 + 20 + 24 + 38 + 7 = 254. 호출: 10 + 3 + 5 + 8 + 5 + 6 + 6 + 9 + 5 + 6 + 8 + 14 + 3 = 88.

### 6.3 단계

| 단계 | 내용 | 게이트 |
|---|---|---|
| P0 | 도감 후속 개선(스크롤바 폭·조건 줄 좌표) 및 도감 세트 확정 유지 | 사용자 |
| P1 | 마을 HUD 목업 승인(`.mockups/village-hud-art.html`) | 사용자 승인 |
| P2 | 마을 HUD 세트 + `CommonUI*` 신규 8장 제작(S0 참조 첨부, 한도 고려해 2일에 걸쳐) | 육안 검수 |
| P3 | 프리팹 참조 교체·스크린샷 비교 | 사용자 확인 |
| P4~ | 우선순위 표대로 세트별 반복(화면 목업 -> 승인 -> 생성 -> 교체) | 세트마다 사용자 승인 |
| P-last | 도감 이관(§4.2) -> 참조 0건 구 스프라이트 일괄 삭제 | 사용자 확인 |

## 7. 위험·검수

| 위험 | 대응 |
|---|---|
| 화풍 불일치(세트 간) | 모든 생성에 S0 참조 첨부, 세트마다 첫 장을 사용자 승인 |
| GPT 사용량 한도 | 세트당 호출 10 이하, 호출 누적 기록, 한도 걸린 시점에서 중단·재개 |
| 9-slice 보더·크기 불일치로 늘림 왜곡 | 도감과 같은 극단 크기 늘림 테스트(`monster-codex-ui-art.md` §7) |
| 공통 세트 이관 전 다른 화면 교체가 도감 파일을 직접 참조 | 이관 전에는 다른 화면의 공통 요소 교체를 보류(§4.2) |
| 기존 틴트 코드가 새 컬러 스프라이트에 곱해져 색이 어긋남 | 코드 `.color=` 사용 지점(약 185곳, UI 44파일)을 교체 화면별로 확인, 필요한 곳만 색 흰색으로 정리 |
| 레이아웃이 화풍 변경으로 어긋남 | 마을 HUD 는 승인 목업 좌표 유지, 프리팹 RectTransform 미세 조정만 |

검수 게이트: UA1 세트 스타일 시트 승인 · UA2 스프라이트 크기·보더·PPU 가 표와 일치(자동) · UA3 프리팹 GUID 참조(구 스프라이트 0건 확인, 세트 교체 화면 한정) · UA4 화면 스크린샷 비교(마을 HUD 우선) · UA5 한도·호출 수 기록.

## 8. 도감 개편과의 관계

- 도감 세트(`Codex*`)가 이 계획의 스타일 기준이며 **확정 상태**다. 도감 세부 개선은 후속(§2 개선 과제, `monster-codex-ui-art.md` 후속 개선 목록).
- 도감 프리팹은 이미 `Codex*` 를 참조하므로 이번 화면별 교체와 충돌하지 않는다.
- 도감 문서의 "도감 전용, 공통 `Px_*` 변경 금지" 원칙은 유지된다. 공통 세트로의 이관은 도감 개선 후 별도 단계(§4.2).

## 9. CLAUDE.md §8 "공통 UI 아트 교체 허용" 항목 (2026-11-15 개정, 반영 완료)

```
- **공통 UI 아트 교체 허용 (2026-11-09 v0.3 승격, 2026-11-15 방침 개정) - 단 기존 UI 화면의 스프라이트 참조 교체에 한정** - 마을 HUD·상점·편성·월드맵·결과창·데일리·탑·유물·랭킹·기록·클라우드·확인 팝업·카드 선택·시너지·빌드·전투 HUD 등 기존 UI 화면을 도감 세트(`Codex*`)와 같은 화풍·방식의 **화면(팝업) 단위 전용 고해상도 GPT 아트 세트**(화면 표시 크기의 3배·PPU 300, 마젠타/검정 키잉 후처리)로 새로 제작해 교체한다. 여러 화면이 공유하는 요소(탭·닫기·칩·별·진척 바·스크롤바·팝업 프레임·버튼·배지 계열)는 **공통 세트 `CommonUI*`** 를 먼저 한 번 제작해 모든 팝업이 공유하고, 화면 전용 요소만 `<화면>*` 세트(예: `VillageHud*`)로 분리한다. 기존 소형 도트 스프라이트의 같은 파일명·같은 크기 제자리 덮어쓰기 방식은 쓰지 않으며, 교체는 **프리팹 Image 의 `Sprite` 참조 변경**으로 하고 세트 교체가 끝나 참조 0건이 된 구 스프라이트는 마지막에 일괄 정리한다(사용자 확인 후). 첫 세트는 마을 HUD 이며 **데이터 구조·프리팹 구조 변경 없이 스프라이트 참조만 바꾼다**(코드 변경은 `[SerializeField] Sprite` 슬롯 추가 수준까지). 도감 `Codex*` 중 공통화 가능한 스프라이트는 도감 후속 개선 후 `CommonUI*` 로 이관한다(그 전에는 이동·리네임 금지). 허용 범위는 ① 화면별·공통 신규 UI 스프라이트 PNG(`Assets/_Lair/Art/Sprites/<세트명>/`)와 프리팹 스프라이트 참조 교체 ② 인벤토리·무결성 점검용 일회용 에디터 툴과 EditMode 에셋 무결성 테스트 ③ 비교 목업. **신규 화면·신규 UI 기능·프리팹/씬 구조 변경·데이터 구조 변경·월드맵 일러스트성 스프라이트(메달·달·실루엣·안개·비네트)·몬스터/영웅 아트·폰트·이펙트 시트·카드 일러스트·콘텐츠 아이콘 교체·신규 몬스터·★ 목표는 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지. 세부는 `docs/design/ui-art-refresh.md`.
```

## 10. 열린 질문 (사용자 확인)

1. **GPT 하루 호출 예산**: 사용량 한도 수치를 알려 주세요(이전에 두 번 걸림). 추천: 마을 HUD 세트(약 10호출)는 이틀에 나눠 진행.
2. 마을 HUD 목업(`.mockups/village-hud-art.html`) 승인 후 제작을 시작해도 되는가. 추천: 승인 후 시작.
3. `CommonUI*` 신규 8장(버튼 5·파인 홈 패널·배지 2)을 마을 HUD 직후(순위 2) 만드는 순서가 맞는가. 추천: 그대로.

## 11. Self-Review

- 개수 검산: 공통 세트 32장(이관 24 + 신규 8) - 제목 33 표기 오류는 §4.1 검산에 정정 명시, 마을 HUD 35장, 우선순위 합 254장·88호출.
- 호출·공수는 가정(요소 5개/호출, 재생성 30%)에서 계산한 시작값이며 S0 이후 실측으로 보정한다고 명시.
- 인벤토리 참조는 GUID Grep 의 군 단위 결과(개별 스프라이트 정밀 참조는 세트 제작 착수 시 해당 화면 프리팹에서 재확인).
- 코드 변경: 없음(마을 HUD 는 `[SerializeField] Sprite` 슬롯 추가 수준까지).
| - | Formation\* 16장 | 편성 | **아트 제작·설치 완료(2026-11-15, Codex 4호출), 프리팹 참조 교체 미적용** - 계획표 `.art-incoming/fm-set/APPLY_PLAN.md`, 무결성 테스트 `FormationUiArtTests`(미실행) | 16(완료) | - | 적용 1 |
