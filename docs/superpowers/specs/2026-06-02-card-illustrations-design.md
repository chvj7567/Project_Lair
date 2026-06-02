# 카드 일러스트 표시 — 설계 (spec)

> 작성일: 2026-06-02 · 단계: MVP · 파이프라인: start-develop
> 입력: 사용자 합의(2026-06-02) · `docs/design/card-art-prompts.md`(28카드 프롬프트/매핑) · `docs/design/card-renewal.md`(라인업)

## 1. 의도 / 한 줄 목표

외부에서 생성된 **카드 일러스트(3:4 세로 아트) 28장**을 프로젝트로 가져와, **3택1 카드 선택 팝업**의 각 카드(`CardView`)에 표시한다. 카드를 고르는 순간의 비주얼 임팩트를 높인다.

## 2. 범위

### 포함
- `CardData`에 카드 일러스트용 `Sprite` 필드(`_cardImage`) 추가 — 기존 `_icon`과 별개로 공존.
- `png\cards\` 28장을 `Assets/_Lair/Art/Sprites/CardArt/{ECardId}.png`로 가져오기(복사·리네임).
- `LairCardPrefabBuilder` 확장 — 일러스트를 `_cardImage`에 자동 배정(아이콘 파이프라인 미러).
- `CardView`(3택1 팝업 슬롯) UI — 상단 아트 + 하단 텍스트 레이아웃으로 일러스트 표시.

### 제외 (이번 작업 아님)
- **아이콘 파이프라인 일절 변경 금지** — `_icon` / `Art/Sprites/CardIcons/` / `BuildIconCell` 는 직전 작업으로 완료됨. 건드리지 않는다.
- **빌드 모달 / 덱 셀**(`BuildModalCardCell`) — 일러스트 표시 안 함. 아이콘 그대로 유지.
- 메타 진행 / 사운드 / 메인 메뉴 (MVP §8).

## 3. 핵심 메커니즘

### 3.1 데이터 (`CardData`)
- `[SerializeField] private Sprite _cardImage;` + `public Sprite CardImage => _cardImage;`
- `_icon`(빌드 패널 아이콘)과 의미·필드 모두 분리. 둘 다 비파괴 보존.

### 3.2 에셋 파이프라인 (`LairCardPrefabBuilder`)
- 신규 폴더 `Assets/_Lair/Art/Sprites/CardArt/` (아이콘의 `CardIcons/`와 형제 구조).
- `png\cards\` 28장 → `{ECardId}.png`로 복사 (접두어 `Tn_`/접미어 `_card` 제거, T7=`Berserk`).
- 빌더에 `CardArtDir` 상수 + `LoadCardImage(ECardId)` 추가 — `LoadCardIcon` 미러: 파일 존재 시 텍스처 임포트 설정(Sprite/Single) 보정 후 `Sprite` 로드, 미존재 시 `null`.
- `BuildCardsAndPool`에서 `_cardImage` 필드를 매 실행 재배정 (`_icon` 처리와 동일 위치). **비파괴** — 기존 `_effect` 튜닝값·`_icon` 보존.
- PNG는 SO 직접 참조이므로 개별 Addressables 엔트리 불필요(아이콘 선례 일치).

### 3.3 UI (`CardView` — 3택1 팝업 전용)
- 카드 상단 ~60%: 아트 영역(`Image _artImage`). 하단 ~40%: 기존 이름·설명·픽 버튼·캡 배지.
- `Bind`에서 `_artImage.sprite = card.CardImage`; `CardImage == null`이면 아트 영역 비활성(폴백) — 누락 카드도 깨지지 않게.
- 프리팹 구조 변경은 `LairUIPrefabBuilder`의 CardView/CardSelectionPopup 빌드 섹션에 반영(코드 동적 생성 금지, 빌더가 정적 배치).
- `CardSelectionPopup` 로직은 변경 없음(슬롯 3개 Bind 그대로).

## 4. MVP §8 경계 판정

§8 "비주얼은 프리미티브 도형 고정 / 아트 작업 금지"는 **인게임 3D 캐릭터·환경 비주얼**을 대상으로 한다. 본 작업은 (a) 2D 카드 페이스 UI 아트이고, (b) 일러스트가 이미 외부 생성되어 있어 **신규 아트 제작이 아니라 기존 에셋 배선**이다. 따라서 §8 위반이 아니라는 것이 본 spec의 입장이며, **game-designer가 기획서에서 이 경계를 명시적으로 확정**한다(노출 위치·크기·구도 도메인 결정 포함).

## 5. 성공 기준

- 3택1 팝업의 3개 카드에 각 카드 ID에 맞는 일러스트가 상단에 표시된다.
- 텍스트(이름·설명)는 하단에서 가독성 유지.
- `ECardId` 28개 ↔ `CardArt/*.png` 28장 이름 정확 일치(Rule 03 §2), 빌더 실행 후 28장 `_cardImage` 충전.
- 아이콘·빌드 패널·카드 효과/풀 구성 무변화(비파괴).
- 일러스트 누락 카드도 `CardView`가 폴백으로 안전하게 표시.

## 6. 결정 락 (사용자 합의 2026-06-02)

| 항목 | 결정 |
|---|---|
| 표시 위치 | **3택1 카드 선택 팝업(`CardView`)만** |
| 카드 레이아웃 | ~~상단 아트(~60%) + 하단 텍스트~~ → **개정(2026-06-02): 풀블리드 아트 + 하단 스크림 + 흰 텍스트 오버레이** (아래 개정 참조) |
| 빌드/덱 셀 | 변경 없음(아이콘 유지) |
| 기술 접근 | 아이콘 파이프라인 미러(별도 폴더 + 빌더 자동 배정) |
| 아이콘 파이프라인 | 불변 |

### 6.1 레이아웃 개정 (2026-06-02, 사용자 지시)

초기 "상단 아트 60% + 하단 텍스트 40% / 레터박스 A" 결정을 **사용자 요청으로 변경**:

- **CardArt = 풀블리드**: 카드 전체(Border 안쪽)를 일러스트로 덮음. `preserveAspect=false`(cover) — 슬롯 320×420(≈3:4)과 일러스트 3:4 비율이 거의 같아 왜곡 ~1.6% 무시.
- **하단 스크림**: 카드 하단 0~42% 에 반투명 검정(α 0.6) Image 를 아트 위·텍스트 아래에 깔아 가독성 확보.
- **텍스트 = 흰색**: NameText/DescText/CountBadge 색을 흰색으로. Name/Desc 는 스크림 영역(하단)으로 재배치, CountBadge 는 상단 앵커 유지(흰색).
- 구현: `LairUIPrefabBuilder.BuildCardViewSlot`. 형제순서 Border→Bg→CardArt→Scrim→Name→Desc→CountBadge→PickButton(마지막 raycast).
- (레터박스 흰여백 40% trade-off 우려는 풀블리드로 전환되며 해소.)

## 7. 미결 / 후속 단계 위임

- 정확한 아트:텍스트 비율, 마진, 폴백 시 레이아웃 — game-designer 도메인 결정.
- 파일 경로·시그니처·TDD 단계 — writing-plans.
- 테스트(네이밍 정합성·28장 충전·null 폴백) — test-engineer.
