# 공통 UI 아트 교체 (UI Art Refresh) - 기능 기획서

> 입력: 사용자 결정("공통 UI 전체를 GPT 이미지 생성으로 새로 교체, 몬스터·영웅을 새 시트로 바꾼 것처럼") · `Assets/_Lair/Art/Sprites/`(루트 UI 스프라이트·`UiDot/`·`VillageIcons/`) 메타·프리팹 GUID 참조 조사 · `docs/design/build-modal-visual-polish.md`·`battle-hud-visual-polish.md`(Px_Solid 틴트·프레임 레시피) · `docs/design/monster-codex-ui-art.md` · `.mockups/monster-tier-v5.html`(몬스터 v5 화풍)
> **사용자용 가이드**: `docs/design/ui-art-refresh.html` · **비교 목업**: `.mockups/ui-art-refresh.html`
> 이 문서는 도감 개편(`monster-codex.md`)과 **별개의 새 작업**이다.

## 0. 헤더

- **목표**: 마을·전투·월드맵·모달 등 모든 화면이 공유하는 **공통 UI 스프라이트 117장**(인벤토리 128장 중 확정 제외 11장 뺀 수, §2.7~2.8)을 몬스터·영웅 새 시트와 어울리는 한 세트의 화풍으로 GPT 이미지 생성해 **제자리 덮어쓰기**한다. 프리팹·코드는 건드리지 않는다.
- **검증 가설**: (1) UI 아트만 바꿔도 새 몬스터·영웅 시트와 화면이 한 게임처럼 보이는가. (2) 틴트·9-slice·애니메이션을 쓰는 프리팹이 깨지지 않고 교체되는가. (3) GPT 생성물이 후처리(Nearest 축소·팔레트 스냅)만으로 픽셀 UI 품질을 유지하는가.
- **현재 단계 범위 적합성**: **범위 밖 -> 사용자 승인 후 승격 필요**. 현행 §8 은 "아트/에셋 작업 허용"이라 에셋 교체 자체는 허용이나 "기존 UI 스프라이트 전면 교체"를 명시한 항목이 없어 §12 의 승격 항목 초안으로 확정한다. 신규 화면·신규 기능·코드 변경·신규 스프라이트(이름이 없던 것)는 계속 금지.
- **핵심 메커니즘**: 같은 파일명·같은 `.meta`(GUID)·같은 픽셀 크기·같은 9-slice 보더·같은 PPU·같은 임포트 설정으로 PNG 만 덮어쓴다. 시안 6장 -> 사용자 승인 -> 나머지 일괄 생성 -> 화면별 육안 검수.

## 1. 결정 요약

| ID | 결정 | 근거·대안 |
|---|---|---|
| D1 | 교체 방식 = **제자리 덮어쓰기**(파일명·GUID·크기·보더·PPU·임포트 동일), 코드 변경 0 | 대안(신규 파일 + 프리팹 재배선)은 참조 수가 많아(Px_Solid 하나가 36개 프리팹 245곳) 위험 |
| D2 | 범위 = 루트 UI 스프라이트·`UiDot/`·`VillageIcons/` 의 **공통 UI 117장**(B1~B5 118장 - `Px_Solid` 1장). **확정 제외**: `Px_Solid`, B6 월드맵 일러스트성 10장(후속), 참조 0건 범위 밖 5장(`HpBar`·`HpBarBackground`·`Loading_Bar_Full`·`TitleImage`·`Game_Icon`). 몬스터/영웅 아트·폰트·이펙트 시트·카드 일러스트·콘텐츠 아이콘(유물 등)도 범위 밖 | 사용자 결정(2026-11-09, 추천안 3건 확정) |
| D3 | **틴트용 스프라이트는 회색조 유지**(§3 표의 "틴트" 표시). 색이 들어가면 런타임 색(등급색·소울/블러드·종족색)이 깨진다 | 코드가 `Image.color` 로 칠하는 곳이 40개 이상 파일 |
| D4 | 화풍 = 현행 팔레트의 **색상 계열은 유지**하고 명암 단계(2~3톤)·외곽선·베벨·금 포인트를 몬스터 v5 화풍(굵은 어두운 외곽선, 음영 2~3톤, 클린 픽셀)에 맞춰 다듬는다(§4). 변화 폭은 작게(명도 +-12% 이내) | 팔레트를 바꾸면 화면 분위기·가독성 재검수가 커짐 |
| D5 | 제작 = **파일럿 6장 시안 -> 사용자 승인 -> 묶음(B1~B5) 일괄**. 파일럿 6장 확정: `Px_PanelDark`·`Px_Btn`·`Px_BtnGold`·`Px_BarBg`·`WorldMapFrame`·`Px_Badge_Gold`(상세는 `ui-art-refresh-pilot.md`). B6 는 후속 | 사용자 요청·결정. 승인 전 일괄 생성 금지 |
| D6 | 크기·메타 일괄 덤프(Phase 0)는 일회용 에디터 스크립트로 먼저 수행해 인벤토리의 "현재 크기" 칸을 확정한다 | Unity `.meta` 에 단일 스프라이트 픽셀 크기가 없어 PNG 헤더를 읽어야 함 |
| D7 | 도감 개편(`monster-codex.md`)과 충돌 없음(§9). 도감 신규 3장(`Px_CodexCellFrame`/`Px_CodexGlow`/`Px_CodexSpark`)은 새 화풍 기준에 맞는지 점검 | 제자리 교체라 프리팹 참조가 유지됨 |

## 2. 인벤토리 (전수)

**범례**: 보더는 Unity 순서 `L,B,R,T`(`spriteBorder x,y,z,w`), 0 이면 `-`. 크기는 `.meta` 에 없어 **Phase 0 덤프로 확정**("대기") - 단 `WorldMapCornerGold` 는 기존 문서에 28x28(`build-modal-visual-polish.md` §3). 참조 = 프리팹/씬 GUID 매칭(Grep, `Assets/_Lair`의 `*.prefab`·`*.unity`) 결과이며 "건"은 매칭 횟수, "파일"은 프리팹·씬 수. 위험: **상**(틴트·9-slice 보더 비균일·애니메이션 등으로 깨질 수 있음) / **중** / **하**.
**참조 방식 주의**: 코드(`.cs`)에는 스프라이트 이름 문자열이 없다(Grep 확인). 모두 프리팹 직접 참조이고, 일부는 인스펙터 `[SerializeField] Sprite`(예: `CodexCell._normalSprite`)로 런타임에 `Image.sprite` 를 바꾼다.
공통 임포트(전 스프라이트): Point(`UiDot`, filterMode 0) / 루트 UI(필터 현행 유지), 압축 없음, `alphaIsTransparency` 켬 - 덮어쓰기 시 `.meta` 불변이므로 그대로 유지된다.

### 2.1 묶음 B1 - 패널·프레임류 (22장)

| 파일 | PPU | 보더 | 크기 | 참조(주요 화면) | 용도·틴트 | 위험 |
|---|---|---|---|---|---|---|
| `UiDot/Px_Panel` | 25 | 3,3,3,3 | 대기 | 8건·7파일: 월드맵(2) · 영웅선택 팝업/셀 · 편성 · 데일리 · 탑 · 마을 HUD | 석판 바탕. 9-slice | 중 |
| `UiDot/Px_PanelDark` | 25 | 3,3,3,3 | 대기 | 19건·12파일: 영웅선택 셀·데일리(3)·편성(팝업/종족행/둥지타일)·탑(팝업/층셀)·전투 HUD·빌드 시너지·스포너 상태 셀·마을 HUD·월드맵(3) | 어두운 석판. 9-slice | 중 |
| `UiDot/Px_PanelSunk` | 25 | 2,2,2,2 | 대기 | 8파일: 카드선택·데일리·탑·편성·월드맵·영웅선택 셀·빌드 카드셀·빌드 아이콘셀 | 움푹 파인 슬롯. 9-slice | 중 |
| `UiDot/Px_Plaque` | 25 | 8,0,8,0 | 대기 | (Plaque/CloseX/CardRing/Divider/Dot 합산 8파일: 데일리·탑·편성·월드맵·마을 HUD·탑 층셀·편성 둥지타일·영웅선택 팝업) | 제목판. 가로만 9-slice(위·아래 보더 0) | 상(세로 늘리면 왜곡) |
| `UiDot/Px_Divider` | 25 | 1,0,1,0 | 대기 | 위 합산 | 가로 구분선. 가로 9-slice | 중 |
| `WorldMapFrame` | 100 | 9,9,9,9 | 대기 | 35건·16파일: 거의 모든 모달(금테 이중 프레임) | 모달 금테. Sliced, fillCenter 끔 | 상(보더 9 비율 유지 필수) |
| `WorldMapInset` | 100 | 5,5,5,5 | 대기 | Inset/Badge/NamePlate 합산 16건·10파일 | 안쪽 파임 패널 | 중 |
| `WorldMapNamePlate` | 100 | 2,2,2,2 | 대기 | 위 합산 | 이름판 | 중 |
| `CardSelectFrame` | 100 | 7,7,7,7 | 대기 | 카드 선택 팝업(합산 B1 모달군 21파일) | 카드 프레임 | 상(보더 7, 카드 크기 고정) |
| `CardSelectKindInset` | 100 | 9,9,9,9 | 대기 | 카드 선택 | 종류 인셋 | 중 |
| `CardSelectArtRing` | 100 | 3,3,3,3 | 대기 | 카드 선택 | 카드 일러스트 링. 틴트 가능성 | 중 |
| `CardSelectChip` | 100 | 4,4,4,4 | 대기 | 카드 선택 | 칩 | 중 |
| `SynergyModalCellBg` | 100 | 3,6,3,5 | 대기 | 시너지 모달 셀 | 비균일 보더 | 상 |
| `BuildModalChipFrame` | 100 | 3,3,3,3 | 대기 | 빌드 모달 | 칩 프레임 | 중 |
| `BattleHudSmallFrame` | 100 | 3,3,3,3 | 대기 | 18건·10파일: 전투 HUD · 빌드 · 랭킹 · 결과 · 영주 레벨 · 퀘스트 · 클라우드 | 섹션 유리 테두리(흰색 알파 0.5 로 얹음) | 상(알파·틴트 전제) |
| `BattleHudTrackFrame` | 100 | 2,2,2,2 | 대기 | BattleHud 군 합산 32건·16파일(Loading 씬 포함) | 트랙 프레임 | 중 |
| `BattleHudBossFrame` | 100 | 5,5,5,5 | 대기 | 위 합산 | 보스 HP 프레임 | 중 |
| `BattleHudChipBar` | 100 | - | 대기 | 위 합산 | 칩 바 | 중 |
| `VillageHudBarFrame` | 100 | 3,3,3,3 | 대기 | 마을 HUD(합산 VillageHud 군 26파일) | 바 프레임 | 중 |
| `VillageHudPaperInset` | 100 | 12,12,12,12 | 대기 | 마을 HUD | 종이 인셋 | 상(보더 12) |
| `VillageHudChip` | 100 | 5,5,5,5 | 대기 | 마을 HUD | 칩 | 중 |
| `UiDot/Loading_BarFrame` | 25 | - | 대기 | 월드맵 팝업 1파일(DiffIcon·BossBar·Loading 합산) | 로딩 바 프레임 | 중 |

### 2.2 묶음 B2 - 버튼·탭·닫기 (22장)

| 파일 | PPU | 보더 | 크기 | 참조 | 용도·틴트 | 위험 |
|---|---|---|---|---|---|---|
| `UiDot/Px_Btn` | 25 | 2,3,2,2 | 대기 | 3파일: 편성 · 월드맵 · 월드맵 챕터탭 | 기본 버튼 | 중 |
| `UiDot/Px_BtnGold` | 25 | 2,3,2,2 | 대기 | (Blood/Gold/Off/Soul 합산 6파일: 데일리·탑·편성·월드맵·상점 아이템셀·월드맵 챕터탭) | 금 버튼 | 중 |
| `UiDot/Px_BtnBlood` | 25 | 2,3,2,2 | 대기 | 위 합산 | 붉은 버튼 | 중 |
| `UiDot/Px_BtnSoul` | 25 | 2,3,2,2 | 대기 | 위 합산 | 소울색 버튼 | 중 |
| `UiDot/Px_BtnOff` | 25 | 2,3,2,2 | 대기 | 위 합산 | 비활성 버튼 | 중 |
| `UiDot/Px_Tab` | 25 | 3,0,3,3 | 대기 | **0건**(프리팹 직접 참조 없음 - 코드/미래용 확인 필요) | 탭 | 하 |
| `UiDot/Px_TabOn` | 25 | 3,0,3,3 | 대기 | **0건** | 선택 탭 | 하 |
| `VillageHudBtn` | 100 | 6,7,6,6 | 대기 | VillageHud 군 26파일 | 마을 버튼 | 상(비균일 7) |
| `VillageHudBtnOff` | 100 | 6,7,6,6 | 대기 | 위 | 비활성 | 상 |
| `VillageHudCta` | 100 | 6,8,6,6 | 대기 | 위 | 출격 CTA | 상 |
| `CardSelectPickBtnGold` | 100 | 3,7,3,5 | 대기 | 카드 선택 | 선택 버튼 | 상(비균일) |
| `CardSelectPickBtnSoul` | 100 | 3,7,3,5 | 대기 | 카드 선택 | 선택 버튼 | 상 |
| `Button_Square_primary` / `_secondary` | 100 | - | 대기 | **0건** | 구형 버튼 | 하 |
| `Button_Wide_primary` / `_secondary` / `_disabled` | 100 | - | 대기 | **0건** | 구형 버튼 | 하 |
| `UiDot/Px_CloseX` | 25 | - | 대기 | 8파일 합산(Plaque 군) | 닫기 X | 중 |
| `BuildModalCloseGlyph` | 100 | - | 대기 | 빌드 모달 | 닫기 글리프 | 중 |
| `UiDot/Px_ArrowR` | 25 | - | 대기 | 10파일 합산(Hatch/Lock/Minus/Plus/Rays/ArrowR/SoulCoin 군) | 화살표 | 중 |
| `UiDot/Px_Plus` / `Px_Minus` | 25 | - | 대기 | 위 합산 | +/- | 중 |

### 2.3 묶음 B3 - 바·게이지·배지 (20장)

| 파일 | PPU | 보더 | 크기 | 참조 | 용도·틴트 | 위험 |
|---|---|---|---|---|---|---|
| `UiDot/Px_BarBg` | 25 | 2,2,2,2 | 대기 | 3파일: 편성 · 마을 HUD · HpBar | 바 배경 9-slice | 중 |
| `UiDot/Px_BarFill` | 25 | - | 대기 | 위 | 바 채움. **틴트·Filled 이미지** | 상 |
| `UiDot/Px_Badge_Gold` / `_Red` / `_Soul` | 25 | 2,2,2,2 | 대기 | **0건** | 배지 | 하 |
| `BattleHudBossFill` / `BossFillLow` | 100 | - | 대기 | 전투 HUD 군 16파일 | 보스 HP 채움(일반/저체력). Filled | 상 |
| `BattleHudGaugeFill` / `MiniFill` | 100 | - | 대기 | 위 | 게이지 채움. Filled | 상 |
| `VillageHudXpFill` | 100 | - | 대기 | 마을 HUD | 경험치 채움. Filled | 상 |
| `UiDot/Loading_BarFill` | 25 | - | 대기 | 월드맵 팝업(합산 1파일) | 로딩 채움 | 중 |
| `UiDot/BossBar_Fill` / `BossBar_TickGem` | 25 | - | 대기 | 위 합산 | 보스 바 채움/눈금 보석 | 중 |
| `BuildModalScrollThumb` | 100 | - | 대기 | 빌드 모달 | 스크롤 손잡이 | 중 |
| `WorldMapBadge` | 100 | 0,3,3,2 | 대기 | 16건·10파일 합산 | 배지(비균일 보더) | 상 |
| `WorldMapStarBadge` / `WorldMapChipGem` | 100 | - | 대기 | WorldMap 전용 군 18파일 58건 합산 | 별 배지 / 칩 보석 | 중 |
| `SynergyModalBadge` | 100 | 4,4,4,4 | 대기 | 시너지 모달 | 배지 | 중 |
| `VillageHudGem` | 100 | - | 대기 | 마을 HUD | 재화 보석 | 중 |
| `VillageIcons/RedDot` | 100 | - | 대기 | 마을 HUD(아이콘 군 1파일) | 알림 점 | 하 |

### 2.4 묶음 B4 - 링·장식·광원·그라데이션 (17장, 틴트 다수)

| 파일 | PPU | 보더 | 크기 | 참조 | 용도·틴트 | 위험 |
|---|---|---|---|---|---|---|
| `UiDot/Px_Solid` | 25 | - | 대기 | **245건·36파일**(거의 모든 UI 의 바탕·헤어라인·아이콘 틴트) | 순백 단색. **전부 `Image.color` 로 칠함** | **상 - 확정 제외(교체 금지, 순백 사각형 그대로)** |
| `UiDot/Px_Ring` | 25 | 1,1,1,1 | 대기 | 10파일: 결과 · 전투 HUD · HpBar · 상점 아이템셀 · 유물셀 · 스포너 상태셀 · 영웅선택 셀 · 도감 셀 · 빌드 카드/아이콘셀 | 종류색·종족색 링. **틴트**, 해금 연출 확대 애니 | 상 |
| `UiDot/Px_CardRing` | 25 | 3,3,3,3 | 대기 | Plaque 군 합산 | 선택 링. **틴트(금)** | 상 |
| `UiDot/Px_Dot` | 25 | - | 대기 | 위 합산 | 점. **틴트** | 상 |
| `UiDot/Px_Rays` | 25 | - | 대기 | Hatch 군 10파일 합산 | 햇살. **틴트·회전 가능** | 상 |
| `UiDot/Px_Hatch` | 25 | - | 대기 | 위 | 빗금. **타일링** 가능 | 상 |
| `WorldMapCornerGold` | 100 | - | **28x28**(문서) | 96건·17파일(모달 모서리 4개씩, 미러 스케일) | 모서리 장식. **좌우·상하 반전 사용** -> 좌우 대칭 가정 | 상 |
| `WorldMapGradientH` / `GradientV` | 100 | - | 대기 | 152건·27파일 | 그라데이션 바탕. **틴트·알파** | 상 |
| `UISoftGlow` | 100 | - | 대기 | 131건·28파일 | 부드러운 발광. **틴트·알파** | 상 |
| `SynergyModalHalo` | 100 | - | 대기 | 시너지 모달 | 후광 | 중 |
| `CardSelectFadeLine` | 100 | - | 대기 | 카드 선택 | 페이드 라인 | 중 |
| `VillageHudAvatarRing` | 100 | - | 대기 | 마을 HUD | 아바타 링 | 중 |
| `WorldMapNodeSelRing` | 100 | - | 대기 | WorldMap 전용 군 | 노드 선택 링 | 중 |
| `WorldMapPathDot` / `PathGlow` | 100 | `PathGlow` 12,12,12,12 | 대기 | WorldMap 전용 군 | 경로 점/발광 | 상(PathGlow 보더 12) |
| `WorldMapPointer` | 100 | - | 대기 | WorldMap 전용 군 | 현재 위치 화살표 | 중 |

### 2.5 묶음 B5 - 아이콘·글리프 (37장)

| 파일 | PPU | 보더 | 크기 | 참조 | 용도 | 위험 |
|---|---|---|---|---|---|---|
| `UiDot/Px_Glyph_Book` · `Castle` · `Crown` · `Scroll` · `Shop` · `Tomb` · `Tower` · `Daily` (8장) | 25 | - | 대기 | 마을 HUD 1파일 | 메뉴 글리프(틴트 가능) | 중 |
| `UiDot/Px_Star` | 25 | - | 대기 | **0건**(도감 신규 UI 가 참조 예정) | 별. **틴트** | 상(틴트) |
| `UiDot/Px_Lock` / `Px_SoulCoin` | 25 | - | 대기 | 10파일 합산(Hatch 군) | 잠금 / 소울 동전 | 중 |
| `UiDot/DiffIcon_Shield` · `Swords` · `Skull` · `Lock` (4장) | 25 | - | 대기 | 월드맵 팝업 1파일 | 난이도 아이콘 | 중 |
| `VillageIcons/Glyph_Account` · `Castle` · `Codex` · `Gem` · `Gold` · `Heroes` · `Quest` · `Rank` · `Records` · `Shield` · `Shop` · `Soul` (12장) | 100 | - | 대기 | 마을 HUD 1파일 | 메뉴 글리프(틴트 가능) | 중 |
| `VillageIcons/Icon_Account` · `Castle` · `Codex` · `Heroes` · `Quest` · `Rank` · `Records` · `Shield` · `Shop` · `Relic` (10장) | 100 | - | 대기 | 마을 HUD 1파일 | 메뉴 풀컬러 아이콘 | 중 |

### 2.6 묶음 B6 - 월드맵 일러스트성 스프라이트 (10장, **이번 범위 제외 - 후속**)

`WorldMapMedalBoss` · `MedalCleared` · `MedalLocked` · `MedalOpen` · `WorldMapMoon` · `WorldMapSilhouetteCh1` · `Ch2` · `Ch3` · `WorldMapFog` · `WorldMapVignette` (PPU 100, 보더 없음). 참조: WorldMap 전용 군(월드맵 팝업·노드·챕터탭 + 모달 일부, 18파일 58건 합산). 월드맵 배경 일러스트와 맞물려 크기·위치가 고정이라 **위험 상**. 공통 UI 가 아니라 월드맵 장면 아트이므로 **사용자 결정으로 이번 범위에서 제외, 후속 별도 승격**.

### 2.7 범위 밖·참조 0건 확인

| 파일 | 판단 |
|---|---|
| `HpBar` · `HpBarBackground` · `Loading_Bar_Full` | 프리팹/씬 직접 참조 0건(Button_*·TitleImage·Game_Icon 과 합산). **확정 제외**(Phase 0 에서 코드·SO 참조가 발견되면 사용자에게 재확인) |
| `TitleImage` · `Game_Icon` | 앱 아이콘/타이틀(공통 UI 아님). **확정 제외** |
| `Px_Solid` | 순백 단색(245건·36파일), 전부 `Image.color` 로 칠함. **확정 제외** |
| B6 월드맵 일러스트성 10장 | **확정 제외(후속)** - §2.6 |
| `Kid Ghost_*` · `Ghost Kid *` · `Axe` · `Witch Hat *` · `Skeleton_Emissive` | 구 캐릭터 잔재. 범위 밖 |
| 몬스터/영웅 아트·폰트·이펙트 시트·카드 일러스트·`Px_Codex*` 3장 | 범위 밖(도감 3장은 §9 점검) |

### 2.8 총계

| 묶음 | 장수 |
|---|---|
| B1 패널·프레임 | 22 |
| B2 버튼·탭·닫기 | 22 |
| B3 바·게이지·배지 | 20 |
| B4 링·장식·광원 | 17 - `Px_Solid` 1 = **16**(실생성) |
| B5 아이콘·글리프 | 37 |
| B6 월드맵 일러스트 | 10 - **이번 범위 제외(후속)** |
| **이번 교체 대상 합계** | **117** |

검산: 22 + 22 + 20 + 16 + 37 = 117. 인벤토리 전체 128 = 117(대상) + `Px_Solid` 1 + B6 10. 이와 별개로 인벤토리 밖 참조 0건 범위 밖 5장(`HpBar`·`HpBarBackground`·`Loading_Bar_Full`·`TitleImage`·`Game_Icon`)이 있다(§2.7).
참고: 대상 117장 중 프리팹 직접 참조 0건인 `Px_Tab`·`Px_TabOn`·`Px_Star`·`Px_Badge_*`(3)·`Button_*`(5) 10장은 교체 대상에 포함된다(화면 영향은 도감 개편 등 미래 사용처에서 확인).

## 3. 교체 원칙 (제자리 덮어쓰기)

| 항목 | 규칙 |
|---|---|
| 파일·메타 | **같은 파일명, 같은 `.meta`(GUID 불변)**. PNG 만 교체. `.meta` 수정·재임포트 설정 변경 금지 |
| 크기 | **현재 픽셀 크기와 동일**(Phase 0 덤프 값). 보더 영역 비율이 달라지면 9-slice 가 어긋난다 |
| 9-slice | 같은 보더 값. 보더 안(모서리) 디테일은 보더 픽셀 이내에 그린다. 가운데 늘어나는 영역은 균일 색/반복 가능한 패턴만(장식 금지) |
| PPU·필터 | 불변(`UiDot` PPU 25 Point, 루트 PPU 100) |
| 틴트용 | **회색조(흰색~회색 + 검정 외곽) 유지**. `Px_Solid`·`Px_Dot`·`Px_Ring`·`Px_CardRing`·`Px_Rays`·`Px_Hatch`·`Px_Star`·`Px_BarFill`·`UISoftGlow`·`WorldMapGradientH/V`·`BattleHudSmallFrame`(알파 얹음)·`Glyph_*` 중 틴트하는 것 |
| 반전 사용 | `WorldMapCornerGold` 는 좌우·상하 반전으로 4모서리를 만든다 -> **좌상단 기준으로 그리고 대칭 가정**(비대칭 조명 금지) |
| Filled 이미지 | `*Fill`(BarFill·BossFill·GaugeFill·MiniFill·XpFill·Loading_BarFill)은 가로 채움이 진행도와 일치해야 하므로 **좌우 끝 모양 단순**, 가운데 반복 가능 |
| 알파 | 투명 PNG 유지. 가장자리 번짐(헤일로) 금지 |
| 검수 | 교체 후 화면별 스크린샷 비교(마을 HUD·월드맵·상점·편성·결과·전투 HUD·카드 선택·도감 등)와 `UiLayoutAudit`(기존 에디터 점검) 통과 |

화면 영향: B1·B2·B4 는 거의 모든 화면에 영향(위 인벤토리 참조 수), B3 는 HUD·바 화면, B5 는 마을 HUD(메뉴 타일)·월드맵 난이도 선택, B6 는 월드맵.

## 4. 화풍 기준 (몬스터 v4/v5 시트와 어울리는 UI 세트)

몬스터 v5 시트의 특징(관찰: `.mockups/monster-tier-v5.html`, `monster-sheet-v4.html`): 굵은 어두운 외곽선, 음영 2~3톤, 치비/클린 픽셀, 채도 높은 포인트색 + 어두운 바탕.

| 항목 | 현행 UI | 제안(새 세트) | 변화 폭 |
|---|---|---|---|
| 외곽선 색 | `#07090e` | `#0a0810`(아주 약한 보라 기운으로 몬스터 외곽선과 톤 맞춤) | RGB 각 +3 이내(거의 동일) |
| 외곽선 두께 | 바깥 검정 2px | **2px 유지**(작은 요소는 1px) | 불변 |
| 면 음영 | 대체로 그라데이션 또는 2톤 | **면 3톤**: 밝은 `#2a3552` / 기본 `#1a2132` / 어두운 `#121827`(패널). 디더링·그라데이션 금지 | 톤 단계 +1 |
| 베벨 | 하이라이트 `#6a7aa0`/베벨 `#3a4764` 1~3px | 위·왼쪽 밝은 `#4a5a80`(1px) + 아래·오른쪽 어두운 `#0e1320`(1px) | 베벨 명도 +12%(상한) |
| 금 포인트 | `#f7c64a` / `#8a6a2c` | 3톤 `#f7c64a` / `#c8962a` / `#8a6a2c`, 하이라이트 점 `#ffe9a8` 1px | 톤 +1 |
| 포인트색(유지) | 소울 `#5ef0b4` · 블러드 `#e5484d` · 하늘 `#5aa9ff` | 동일(런타임 색이라 불변) | 0 |
| 모서리 | 직각 | 직각 + 1px 노치(깎은 모서리) 또는 리벳 점(보더 안) | 신규 디테일 소 |
| 질감 | 없음 | 석판 균열 1~2개(가운데 영역에는 금지, 보더 안만) | 신규 소 |

변화 폭 요약: **색상 계열·포인트색·외곽선 두께는 그대로**, 면 음영을 3톤으로 올리고 베벨·금 포인트를 한 단계 선명하게 한다. 화면 분위기 변경은 작고, 몬스터 외곽선·음영과의 이질감을 줄이는 방향이다. 팔레트 수치는 파일럿 시안(Phase 1)에서 사용자가 확정한다(결정 메트릭: 파일럿 6장을 몬스터 시트 옆에 둔 비교 목업 승인).

## 5. 제작 계획

### 5.1 단계

| 단계 | 내용 | 산출·게이트 |
|---|---|---|
| Phase 0 | 인벤토리 덤프: 일회용 에디터 스크립트로 대상 117장(+ 제외 확인용 `Px_Solid`)의 픽셀 크기·`.meta` 임포트값·프리팹 참조 목록(GUID)을 표로 출력해 §2 "대기" 칸 채움. 현재 스프라이트를 `.mockups/assets/ui/` 에 복사(비교 목업용) | 확정 인벤토리 + 비교 목업 갱신 (gameplay-programmer, Rule 04 §3 일회용 툴 - 사용 후 삭제) |
| Phase 1 | **파일럿 6장**: `Px_PanelDark`(패널) · `Px_Btn`(버튼) · `Px_BtnGold`(금 버튼) · `Px_BarBg`+`Px_BarFill` 쌍 중 `Px_BarBg`(바) · `WorldMapFrame`(금테 프레임) · `Px_Badge_Gold`(배지) 를 GPT 로 생성·후처리·**임시 폴더**(`.mockups/assets/ui-pilot/`)에만 두고 비교 목업에 적용 | **사용자 승인 게이트**(승인 전 `Assets` 덮어쓰기 금지) |
| Phase 2~6 | 승인 후 묶음 순서 B1 -> B2 -> B3 -> B4 -> B5 (B6 는 후속 별도 승격). 묶음마다 생성 -> 후처리 -> 덮어쓰기 -> 화면 스크린샷 검수 -> 이상 시 해당 장만 재생성 | 묶음별 검수 |
| Phase 7 | 전 화면 스크린샷 비교·`UiLayoutAudit`·EditMode 에셋 무결성 테스트(크기·보더·PPU 불변 확인) | test-engineer |

### 5.2 GPT 프롬프트 템플릿 - 공통 화풍 블록 (모든 프롬프트 앞에 붙임)

```
STYLE BLOCK: Dark-fantasy chunky pixel-art UI element, hard pixel edges, no anti-aliasing, no gradients, no dithering, no blur, no photo texture. A bold near-black outline (#0a0810) 2 pixels thick, flat colors with exactly 3 shading tones per surface (light, base, dark), a 1-pixel top-left highlight and 1-pixel bottom-right shade for a carved-stone-slab bevel. Palette: slab blues #2a3552 / #1a2132 / #121827, bevel #4a5a80 and #0e1320, gold accents #f7c64a / #c8962a / #8a6a2c with a #ffe9a8 sparkle pixel. Clean, centered, symmetrical where applicable, transparent PNG background (alpha 0 outside the object). Same visual language as a chibi pixel-art monster sheet: thick dark outlines, clean simple shapes.
```
틴트용 스프라이트는 팔레트 문장을 `Pure grayscale only (white to mid-gray with a black outline), no color, so it can be tinted in-engine.` 로 **교체**한다.

### 5.3 묶음별 항목 템플릿

```
[STYLE BLOCK]
TARGET: <파일명> - <용도 한 줄>. Canvas <현재 크기 x4> px (will be downscaled to <현재 크기> px with nearest-neighbor).
LAYOUT: <9-slice면> 9-slice: the outer <보더 x4>-pixel ring (at 4x scale) carries all corner details and the outline; the center area is a flat repeatable fill with no ornament. <아니면> Single sprite, centered with 1-pixel margin.
CONTENT: <항목별 서술>
NEGATIVE: no text, no drop shadow outside the outline, no glow, no background, no gradient, no anti-aliasing, no extra objects.
```

| 묶음 | CONTENT 서술 요령 |
|---|---|
| B1 패널·프레임 | 석판 바탕 면 3톤 + 보더 링에 노치 코너·베벨. 금테 프레임은 가운데 투명(fillCenter 끔), 안쪽 금 라인 1px + 바깥 검정 |
| B2 버튼·탭 | 위가 밝고 아래가 어두운 3톤, 눌림 상태는 베벨 반전. 색 변형(Btn/Gold/Blood/Soul/Off)은 같은 형태 + 면 색만 바꿔 5장을 한 세션에서 생성 |
| B3 바·배지 | 채움은 가로 반복 가능한 3톤 줄무늬, 끝 모양 단순. 배지는 9-slice 둥근 사각 |
| B4 링·장식·광원 | **틴트용 회색조**. 링은 두께 균일, 광원은 가운데 흰색에서 가장자리 알파 0 으로 자연 감쇠 |
| B5 아이콘 | 풀컬러 아이콘은 같은 팔레트 + 포인트색 1~2개, 글리프는 단색 실루엣(틴트용 회색조) |
| B6 월드맵 | 이번 범위 제외(후속 승격 시 별도 프롬프트) |

### 5.4 후처리 절차 (장마다)

1. GPT 출력(4배 크기) 저장. 투명 배경 PNG 인지 확인(흰 배경이면 마스크로 알파 복원).
2. **Nearest Neighbor** 로 현재 크기(Phase 0 값)까지 축소 - 보간 금지.
3. **팔레트 스냅**: §4 팔레트(3톤 x 3계열 + 금 3톤 + 외곽선)로 색 양자화. 틴트용은 회색 단계(`#ffffff`·`#c8ccd8`·`#8e98ad`·`#0a0810`)만 허용.
4. 알파 이진화(경계 반투명 제거. 단 광원류 `UISoftGlow`·그라데이션은 예외로 알파 그라데이션 유지).
5. 9-slice 검증: 보더 안쪽에서 장식이 끝나는지, 가운데가 균일한지 확인.
6. 대칭 가정 항목(`WorldMapCornerGold` 등)은 좌우 반전해도 어색하지 않은지 확인.
7. 어두운(`#10151f`)·밝은 배경 양쪽에서 가장자리 번짐 확인 후 덮어쓰기.

## 6. 위험 상위 요약

| 순위 | 위험 | 대상 | 대응 |
|---|---|---|---|
| 1 | **틴트 깨짐** - 색이 들어간 스프라이트를 칠하면 등급색·소울/블러드·종족색이 곱해져 의도와 다른 색이 됨 | `Px_Solid`(245건) · `Px_Ring` · `Px_CardRing` · `Px_Dot` · `Px_Rays` · `Px_Hatch` · `UISoftGlow` · `WorldMapGradient*` · `BarFill` 류 | 회색조 규칙(D3), `Px_Solid` 는 확정 제외, 틴트 사용 화면 스크린샷 검수 |
| 2 | **9-slice·Filled 어긋남** - 보더 값은 그대로인데 새 그림의 보더 영역 크기가 다르면 모서리가 늘어나거나 눌림. 비균일 보더(`VillageHudBtn` 6,7,6,6 · `VillageHudCta` 6,8,6,6 · `CardSelectPickBtn*` 3,7,3,5 · `SynergyModalCellBg` 3,6,3,5 · `WorldMapBadge` 0,3,3,2)와 큰 보더(`VillageHudPaperInset`·`WorldMapPathGlow` 12) | B1·B2 | 같은 크기 + 같은 보더 비율로 생성, 보더 안에 디테일 집중, 늘림 테스트 |
| 3 | **픽셀 격자 불일치** - GPT 출력은 격자가 정확하지 않아 Nearest 축소 후 뭉개지거나 외곽선 두께가 흔들림(특히 PPU 25 소형 스프라이트) | 전체(특히 `UiDot` 소형) | 4배 생성 + Nearest 축소 + 팔레트 스냅 + 파일럿 승인 게이트 |

## 7. 구현 요청사항 (gameplay-programmer·test-engineer 용)

- **코드 변경: 없음**(`.cs` 수정 금지). 프리팹·SO·`.meta` 수정 금지.
- **Phase 0 일회용 툴**(Rule 04 §3 - 실행 후 삭제): 128장의 PNG 크기·메타 값·프리팹 참조 목록 덤프, 현재 스프라이트 복사. 결과는 §2 표에 반영(문서 갱신은 game-designer).
- **에셋 교체**: 사용자 승인된 PNG 를 같은 경로에 덮어쓴다(작업자: 사람/에디터 지원 툴). `.meta` 변경이 생기면 되돌린다.
- **Enum·Interface·SO 스키마·JSON 스키마: 없음**(신규 데이터 구조 없음 - 구조 확인 게이트 비대상).
- **테스트(test-engineer)**: EditMode 에셋 무결성 - 교체 후 대상 117장의 ① 픽셀 크기 ② `spriteBorder` ③ PPU ④ filterMode ⑤ 프리팹 GUID 참조 개수가 Phase 0 기준표와 동일. 틴트용 목록의 PNG 가 회색조(채도 허용 한도 이내)인지.

## 8. 검증 게이트

| ID | 종류 | 내용 | 담당 |
|---|---|---|---|
| UA1 | 자동 | 교체 전후 크기·보더·PPU·필터·GUID 불변(§7) | test-engineer |
| UA2 | 자동 | 틴트용 PNG 채도 상한 이내(회색조), `Px_Solid` 미변경(파일 해시 동일) | test-engineer |
| UA3 | 육안 | 화면별 스크린샷 비교(마을 HUD·월드맵·상점·편성·결과·전투 HUD·카드 선택·도감·유물·탑·데일리) - 모서리 늘어남·색 곱 오류·가독성 | 사용자 |
| UA4 | 육안 | 몬스터·영웅 새 시트와 같은 화면에서 이질감 없음 | 사용자 |
| UA5 | 자동 | `UiLayoutAudit` 통과(기존 도구) | test-engineer |

## 9. 도감 개편과의 관계

- 도감 프리팹(`CodexPopup`·신규 `CodexMonsterCell`)은 기존 `Px_*`·`WorldMap*` 을 **참조**해 작성 중이다. 이 작업은 **제자리 교체**(같은 GUID)라 프리팹 참조가 유지되므로 **충돌 없다**. 도감 작업 완료 여부와 무관하게 진행 가능하고, 교체 후 도감 화면을 UA3 검수 대상에 포함한다.
- 도감 신규 3장(`Px_CodexCellFrame` 회색조 프레임 / `Px_CodexGlow` 흰색 알파 후광 / `Px_CodexSpark` 흰색 스파클)은 **새 화풍 기준 점검 항목**이다: ① §4 외곽선·3톤·베벨 규칙을 따르는가 ② 틴트용 회색조인가 ③ 크기·보더(`Px_CodexCellFrame` 24x24 보더 6 등)가 도감 기획서와 같은가. 이 3장은 `docs/design/monster-codex-ui-art.md` 의 프롬프트에 **STYLE BLOCK(§5.2)을 맞춰** 파일럿 승인 후 재생성/보정 여부를 결정한다.
- 도감 문서의 재사용 목록(`Px_Panel`·`Px_PanelDark`·`Px_PanelSunk`·`Px_Btn`·`Px_BtnGold`·`WorldMapFrame`·`Px_Badge_Gold`·`Px_CardRing`·`Px_Ring`·`Px_Dot`·`Px_Star`·`Px_BarBg/Fill`)은 모두 이 인벤토리에 있으며 교체 후에도 같은 이름으로 사용된다.

## 10. 엣지 케이스

| 상황 | 처리 |
|---|---|
| 새 PNG 크기가 다름 | 덮어쓰기 금지(UA1 실패), 크기를 맞춰 재생성 |
| `.meta` 가 자동으로 바뀜(재임포트) | 변경 사항 되돌림(필요한 경우만 허용: Compression/Mip 설정이 달라진 경우 원상 복구) |
| 파일럿 불합격 | §4 팔레트·프롬프트만 조정해 재생성(범위·원칙은 그대로) |
| 일부 묶음만 승인 | 묶음 단위로 덮어쓰기(혼재 기간은 화풍 불일치가 보일 수 있어 B1+B2 는 동시 교체 권장) |
| 참조 0건 스프라이트(`Px_Tab`·`Px_Star`·`Px_Badge_*`·`Button_*` 등) | 화면 영향 없음. 교체는 하되 UA3 확인 대상 아님(도감 개편이 `Px_Star`·`Px_Badge_Gold` 를 쓰므로 그 화면에서 확인) |

## 11. 열린 질문 (사용자 확인)

**확정(2026-11-09 사용자)**: (1) `Px_Solid` 교체 제외 (2) B6 월드맵 일러스트 10장 제외, B1~B5 먼저(후속 별도 승격) (3) 파일럿 6장 그대로. 남은 열린 질문 없음 - 파일럿 시안 승인은 §5.1 Phase 1 게이트에서 받는다.

## 12. §8 추가 승격 항목 초안 (CLAUDE.md §8 에 붙일 블록)

```
- **공통 UI 아트 교체 허용 (2026-11-09 v0.3 승격) - 단 기존 UI 스프라이트의 제자리 교체에 한정** - 마을·전투·월드맵·모달 등이 공유하는 기존 공통 UI 스프라이트(`Assets/_Lair/Art/Sprites/` 루트 UI 파일·`UiDot/`·`VillageIcons/` 중 117장. `Px_Solid`·월드맵 일러스트성 10장·참조 0건 범위 밖 5장 제외)를 몬스터·영웅 새 시트와 어울리는 화풍으로 GPT 이미지 생성해 교체한다. 교체는 **같은 파일명·같은 `.meta`(GUID)·같은 픽셀 크기·9-slice 보더·PPU·임포트 설정으로 PNG 를 덮어쓰는 방식**이라 프리팹·씬·코드·데이터 변경이 없다. 틴트용 스프라이트는 회색조를 유지하고, 파일럿 6장 시안을 사용자가 승인한 뒤 묶음 단위로 일괄 생성한다. 허용 범위는 ① 기존 UI 스프라이트 PNG 의 제자리 교체 ② 인벤토리·무결성 점검용 일회용 에디터 툴과 EditMode 에셋 무결성 테스트 ③ 비교 목업. **신규 화면·신규 UI 기능·신규 스프라이트(새 이름·새 GUID)·`Px_Solid` 교체·틴트용 스프라이트(링·점·햇살·광원·그라데이션 등)에 색을 넣는 것(회색조 유지 필수)·프리팹/씬 구조 변경·코드 변경·월드맵 일러스트성 스프라이트(후속 별도 승격)·몬스터/영웅 아트·폰트·이펙트 시트·카드 일러스트·콘텐츠 아이콘 교체·신규 몬스터·★ 목표는 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지. 세부는 `docs/design/ui-art-refresh.md`.
```

## 13. Self-Review

- Placeholder: 크기 칸의 "대기"는 `.meta` 에 크기 정보가 없어 Phase 0 덤프로 확정한다고 명시(결정 방법·담당 포함). 그 외 미정 마커 0건.
- 내부 일관성: 대상 117 = 22 + 22 + 20 + 16 + 37(인벤토리 128 = 117 + `Px_Solid` 1 + B6 10), 파일럿 6장(§1·§5.1·`ui-art-refresh-pilot.md` 동일), 제외(`Px_Solid`·B6·범위 밖 5장) 일관. 사용자 결정 3건 확정 반영.
- 참조 수치(건·파일)는 프리팹/씬 GUID Grep 결과이며 합산으로 표기한 칸은 묶음 단위 결과임을 명시. 개별 스프라이트 정밀 참조는 Phase 0 덤프로 확정.
- UI 목업: `.mockups/ui-art-refresh.html`(시안 자리 표시).
