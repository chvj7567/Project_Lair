# 카드 일러스트 표시 — 기획서

> 작성일: 2026-06-02 · 단계: MVP · 파이프라인: start-develop (2단계 game-designer)
> 입력: spec `docs/superpowers/specs/2026-06-02-card-illustrations-design.md` · plan `docs/superpowers/plans/2026-06-02-card-illustrations.md` · `docs/design/card-art-prompts.md` · 컨셉서 §11
> 단일 진실: 본 기획서가 **아트:텍스트 비율·마진·폴백 UX·§8 경계 판정**의 단일 진실(SoT). plan Task 5 의 앵커 수치는 본 §3 으로 대체된다.

> ⚠️ **레이아웃 개정 (2026-06-02, 사용자 지시 — 본문 §3/§6 의 "상단 아트 60% + 하단 텍스트" 를 대체)**
> 카드 레이아웃을 **풀블리드 아트 + 하단 스크림 + 흰 텍스트 오버레이**로 변경. 구현 단일 진실은 `LairUIPrefabBuilder.BuildCardViewSlot` + spec §6.1.
> - CardArt: 카드 전체(Border 안쪽), `preserveAspect=false`(cover). 비율 거의 동일(≈3:4)이라 왜곡 무시.
> - Scrim: 하단 0~42% 반투명 검정(α 0.6), 아트 위·텍스트 아래.
> - 텍스트: Name/Desc 흰색·스크림 영역으로, CountBadge 흰색·상단 유지.
> - 본문 §3.1~3.4·§6.5 의 60/40·레터박스 수치는 이 개정으로 무효(역사 기록으로 남김). §8 판정·폴백·네이밍 등 나머지는 유효.

---

## § 헤더

- **목표**: 외부 생성 카드 일러스트(3:4) 28장을 3택1 카드 선택 팝업의 각 `CardView`(320×420px)에 상단 아트 + 하단 텍스트로 표시한다.
- **검증 가설**: 카드 일러스트가 "고르는 순간의 비주얼 임팩트"를 높이면서 이름·설명 가독성을 해치지 않는가. (텍스트 가독성 ≥ 현행 수준 유지가 합격선.)
- **현재 단계 범위 적합성**: **범위 내**(승인). 근거는 §1. 컨셉서 §11.4 "카드/UI" 라인을 3택1 CardView 한정으로 amend — doc-sync 노트(§1.3), BLOCKER 아님.
- **핵심 메커니즘**: CardView 슬롯 상단 60%에 아트 영역(`_artImage`), 하단 40%에 이름·설명·픽 버튼. preserveAspect로 3:4 일러스트를 fit. 일러스트 null이면 아트 영역 숨김(텍스트는 고정). 빌드 모달·덱 셀은 불변.

---

## 1. MVP §8 경계 판정 — **승인 (BLOCKER 아님)**

### 1.1 판정

본 작업은 MVP §8 "비주얼은 프리미티브 도형 고정 / 아트 작업 금지" 를 **위반하지 않는다.** spec §4 의 입장을 **승인**한다.

### 1.2 근거 (컨셉서 점검)

1. **§8 의 대상은 인게임 3D 엔티티 비주얼이다.** 컨셉서 §11.4 "프리미티브 매핑 (MVP 고정)" 표는 영웅·몬스터 6종·이펙트의 **메쉬(Capsule/Cube/Sphere)·색·스케일**을 규정한다. "프리미티브 도형 고정"이 가리키는 대상은 이 3D 엔티티 매핑이다. 본 작업은 3D 엔티티를 일절 건드리지 않는다.
2. **카드 페이스는 별도로 이미 설계된 MVP 표면이다.** 컨셉서 §11.4 "카드/UI" 서브섹션은 카드 UI를 명시적으로 MVP 범위에 넣고("카드: 흰 배경 + 검정 텍스트, 테두리만 축 색"), §11.2 표의 "톤/비주얼 ❌"와 별개로 다룬다. 카드 일러스트는 이 카드 UI 표면의 비주얼 보강이다.
3. **신규 아트 제작이 아니라 기존 에셋 배선이다.** 일러스트 28장은 이미 외부 생성되어 있다(`png/cards/`). 본 작업은 PNG 가져오기 + 필드 배선 + 레이아웃이며, 아트 "제작" 작업이 아니다.
4. **§8 의 자체 escape clause 충족.** §8 은 "사용자가 명시적으로 승격하기 전까지 착수하지 않는다"고 둔다. 표시 위치·레이아웃·기술 접근은 2026-06-02 사용자 합의로 락됨(spec §6). 명시적 승격 요건 충족.

### 1.3 doc-sync 노트 (컨셉서 §11.4 amend)

본 기획 승인으로 컨셉서 §11.4 "카드/UI" 의 카드 라인("흰 배경 + 검정 텍스트, 테두리만 축 색")은 다음과 같이 **부분 amend** 된다:

- **3택1 CardView 한정**: 상단 60% 아트 일러스트 추가. 하단 40%는 기존 흰 배경 + 검정 텍스트 + 축색 테두리 유지.
- **빌드 모달 / 덱 셀**: 변경 없음. "흰 배경 + 검정 텍스트 + 테두리만 축색 + 아이콘" 컨벤션 그대로.

> 이 amend 는 컨셉서 §11.4 갱신을 트리거하나 코드 BLOCKER 가 아니다. 컨셉서 반영은 별도 docs 작업(메인 오케스트레이터 판단).

---

## 2. 카드 슬롯 기준 치수 (확정 — 검산 포함)

| 항목 | 값 | 출처 / 검산 |
|---|---|---|
| 슬롯 크기 | 320 × 420 px | `LairUIPrefabBuilder.BuildCardViewSlot` 현행 `slotRt.sizeDelta` |
| Bg(흰 배경) 인셋 | 슬롯에서 8px 인셋 → 304 × 404 px | 현행 `bgRt.offsetMin/Max = ±8` |
| 이름 폰트 | 32 pt | 현행 `nameTmp.fontSize` |
| 설명 폰트 | 22 pt (기본) → auto-size 16~22pt (§3.3) | 현행 `descTmp.fontSize` |
| 아트:텍스트 분할선 | 슬롯 높이의 0.4 지점 (= 하단 168px / 상단 252px) | §3.1 확정 비율 |

> 한글 글리프 폭 ≈ fontSize(px) 근사 — NotoSansKR 기준 한글 1자 ≈ 1em. 줄당 수용 글자 수 = 텍스트 폭(px) / fontSize(px) 로 검산.

---

## 3. 아트:텍스트 비율 · 앵커 · fit (확정)

### 3.1 분할 비율 — 상단 아트 60% / 하단 텍스트 40% (락 준수)

결정 락(spec §6)대로 **상단 60% 아트 / 하단 40% 텍스트**. 분할선 = 슬롯 높이의 `0.4` 앵커.

- 아트 밴드(앵커 기준): 슬롯의 y 0.4~1.0 = 높이 252px (= 420 × 0.6)
- 텍스트 밴드: 슬롯의 y 0.0~0.4 = 높이 168px (= 420 × 0.4)

### 3.2 fit 방식 — **A. 레터박스(preserveAspect) 채택** + 흰 Bg show-through

3:4 세로 일러스트를 가로가 넓은 아트 밴드에 넣을 때 두 안을 검토했다.

**검산 (아트 밴드 실측 rect — 슬롯 320×420 직속 기준)**:
- 아트 rect 폭 = 320 − (좌우 마진 12×2) = **296px** (§3.4 좌표계 동일 출처)
- 아트 rect 높이 = 252 − (상 12 + 하 4) = **236px**
- 3:4 일러스트는 높이 제약 → 그려지는 폭 = 236 × 0.75 = **177px**, 중앙 정렬
- 좌우 필러박스 = (296 − 177) / 2 = **약 59px 씩** (밴드 폭의 약 40%가 빈 공간 = (296−177)/296)

| 안 | 방식 | 장점 | 단점 (trade-off) |
|---|---|---|---|
| **A. 레터박스 (채택)** | `preserveAspect = true`. 3:4 원본 무크롭, 좌우 빈 공간은 카드 흰 Bg(`#FFFFFF`)가 비침 | 크롭 0 — 일러스트 의도(중앙 단일 피사체) 온전 보존. 프리팹에 마스크 추가 불필요(plan 구조 그대로) | 밴드 폭의 약 36%가 흰 여백 → 일러스트가 다소 작게 보임 |
| B. 크롭-필 | RectMask2D + 아트를 밴드 폭 채우게(280px 폭 → 3:4면 높이 373px) 띄우고 236px로 마스크 | 밴드를 가득 채워 임팩트 큼 | 일러스트 세로 약 37% 크롭. 마스크 컴포넌트 추가로 plan Task 5 프리팹 구조 변경 |

**권장 안 = A (레터박스)**. 이유:
1. **YAGNI / 락 정합**: B는 RectMask2D 추가로 프리팹 구조가 락된 plan 미러 구조를 벗어난다. A는 plan Task 5 의 `preserveAspect = true` 기본값 그대로다.
2. **무크롭이 일러스트 의도와 일치**: 프롬프트 §0.2가 "single focal subject centered" 를 명시 — 중앙 피사체가 크롭 없이 온전히 보이는 편이 안전하다.
3. **흰 여백이 깨짐이 아니다**: 좌우 여백은 카드 흰 Bg(`#FFFFFF`)가 비치는 의도된 배경. 별도 배경색·플레이트 추가 안 함(YAGNI).

> 픽률·임팩트 데이터가 누적되어 "일러스트가 작게 느껴진다"가 확인되면 B(크롭-필)로의 전환을 후속 검토. 결정 메트릭: qa 또는 사용자 플레이 피드백상 "아트 시인성 불만" 발생 시. 현 시점은 A 확정.

### 3.3 필러박스 / 배경색 (명시)

- 아트 Image 의 빈 영역(레터박스 좌우)에는 **별도 배경 그래픽을 넣지 않는다.** 카드 흰 Bg(`#FFFFFF`)가 그대로 비친다.
- 아트 Image `color = Color.white` (틴트 없음 — 원본 색 그대로).
- 아트 Image `raycastTarget = false` (장식 — §4 참조. 픽 버튼 클릭을 막지 않게).

### 3.4 앵커 / 마진 수치 (plan Task 5 대체 — 확정)

좌표계: 모든 RectTransform 은 **슬롯(320×420) 직속 자식**, anchorMin/Max 는 슬롯 정규화 좌표, offset 은 px.

| 요소 | anchorMin | anchorMax | offsetMin (L, B) | offsetMax (R, T) | 결과 rect (px) |
|---|---|---|---|---|---|
| **CardArt** | (0, 0.4) | (1, 1) | (12, 4) | (−12, −12) | 296 × 236 |
| **NameText** | (0, 0.28) | (1, 0.4) | (8, 0) | (−8, 0) | 304 × 50.4 |
| **DescText** | (0, 0) | (1, 0.28) | (16, 14) | (−16, −2) | 288 × 101.6 |

> CardArt rect 폭 검산: 320 − 12 − 12 = 296px (§3.2 와 동일 값 — 필러박스 ≈ 59px/측, 밴드 폭의 40%).
> CardArt rect 높이 검산: 420×0.6 − 4 − 12 = 252 − 16 = 236px.

**plan Task 5 대비 조정 사항**:
- NameText 앵커 하단을 `0.3` → **`0.28`** 로 내림. 이유: 이름 밴드 높이를 50.4px(= 420×0.12)로 확보 — 32pt 단일 라인(글리프 ≈ 32px + 여유)에 안전. plan 의 `0.3~0.4`(42px)는 32pt 한 줄에 빠듯.
- **NameText 의 현행 pivot=(0.5,1)·anchoredPosition=(0,−30) 잔류 리셋 필수**(§6.5). stretch 앵커로 바꾸면서 pivot=(0.5,0.5)·anchoredPosition=0·sizeDelta=0 으로 함께 리셋하지 않으면 이름이 위로 30px 어긋난다.
- DescText 앵커 상단을 `0.3` → **`0.28`** 로 맞춤(이름과 경계 정렬). 하단 텍스트 밴드 = y 0~0.4 안에서 이름(0.28~0.4) + 설명(0~0.28) 으로 분배.

### 3.4.1 텍스트 밴드 내부 분배 검산

텍스트 밴드 총 168px(y 0~0.4) 을 다음으로 나눈다:
- 이름: y 0.28~0.4 = 50.4px (32pt 1줄)
- 설명: y 0~0.28 = 117.6px → offset 적용 후 rect 높이 101.6px (= 117.6 − 14 − 2)
- 픽 버튼은 슬롯 full-stretch(투명) — 밴드 분배와 무관.

### 3.5 설명 가독성 — auto-size 하한 적용 (확정)

**가장 긴 실제 Description** 을 검산했다(`LairCardPrefabBuilder` 의 28개 spec.Description):
- 최장: S7 "던전의 점성" = `영웅 -50% 이동속도, 모든 몬스터 +30% 이동속도 (10초)` → 한글·숫자·기호 포함 약 30자
- DescText rect 폭 288px (= 320 − 16 − 16, 슬롯 직속 기준), 22pt 줄당 수용 ≈ 288 / 22 ≈ 13자 → 30자 = **약 3줄**
- 22pt line-height ≈ 26px → 3줄 = 78px ≤ rect 높이 101.6px → **22pt 고정으로도 수용 가능** (3.9줄 한계 내)

다만 안전 마진 확보를 위해 **TMP Auto-Size 적용**: `enableAutoSizing = true`, `fontSizeMin = 16`, `fontSizeMax = 22`.
- 짧은 설명(대부분, 예 "영웅 5초 멈춤")은 22pt 로 표시(현행 가독성 동일).
- 최장 설명도 101.6px 밴드 안에서 자동 축소(최저 16pt) — 잘림 없이 전량 표시 보장.
- 16pt 하한: 288px 폭 / 16 = 18자/줄 → 30자 = 2줄, line-height ≈ 19px × 2 = 38px ≪ 101.6px. 하한에서도 충분.

> 결정 근거: 현행 단일 폰트 고정 대비 auto-size 가 락된 60/40 분할을 유지하면서 per-card 튜닝 없이 전 카드 가독성을 보장. 이름(32pt)은 짧으므로 auto-size 미적용(고정).

### 3.6 NameText / DescText 그 외 속성 (현행 유지)

- NameText: 32pt, `alignment = Center`, `color = black`, 현행 그대로(폰트 크기·정렬·색 불변). 앵커만 §3.4 로 변경.
- DescText: `alignment = TopLeft`, `color = black` 유지. 폰트는 §3.5(auto-size 16~22), 앵커는 §3.4 로 변경.

---

## 4. 픽 버튼 raycast — 문제 없음 (1줄 코멘트)

픽 버튼은 슬롯 full-stretch 투명 Image(`alpha 0.001`, raycast 만 수신)이며 빌더에서 **마지막에 추가** = 형제 순서 최상위 → 아트/텍스트 위에 얹혀 카드 전체 클릭을 정상 수신한다. 아트 Image 는 `raycastTarget = false`(§3.3)로 두어 클릭 가로채기를 원천 차단 — 동작 문제 없음.

---

## 5. 일러스트 누락(null) 폴백 UX (확정)

| 항목 | 결정 |
|---|---|
| `CardImage == null` 일 때 | 아트 영역(`_artImage.gameObject`)을 **비활성(숨김)** |
| 텍스트 위치 | **고정** — 위로 차오르지 않음 |
| 빈 상단 영역 | 카드 흰 Bg(`#FFFFFF`)가 그대로 노출 |
| 플레이스홀더 | **두지 않음** |

**근거**:
- 슬롯 자식들은 앵커 기반 RectTransform 이고 내부에 LayoutGroup 이 없다 → 아트 GameObject 를 SetActive(false) 해도 NameText/DescText 위치는 reflow 되지 않고 고정. reflow 엔지니어링 불필요(YAGNI).
- 28장 전부 일러스트가 들어올 예정이므로 null 폴백은 **안전 경로**(누락 카드도 깨지지 않게)일 뿐, 정상 플레이 경로가 아니다. 플레이스홀더 이미지를 별도 제작/배선하는 것은 MVP 범위 밖 추가 작업.
- 폴백 시에도 이름·설명·테두리·픽 버튼은 정상 표시 → 카드 기능은 100% 동작.

---

## 6. 구현 요청사항 (gameplay-programmer 용)

> 데이터·빌더·CardView 로직·테스트는 plan Task 1~4 와 동일. 본 기획서는 **레이아웃 수치(Task 5)와 폴백·fit 정책**을 확정한다.

### 6.1 Enum
- **추가 없음.** 기존 `ECardId`(28값) 그대로 사용. 에셋 키 = `ECardId` 값명 ↔ `CardArt/{ECardId}.png` (Rule 03 §2). T7 = `Berserk`.

### 6.2 Interface
- **추가 없음.**

### 6.3 에셋 키 / 폴더
- 신규 폴더: `Assets/_Lair/Art/Sprites/CardArt/` (아이콘 `CardIcons/` 의 형제 구조, Rule 04 §2).
- 파일명 = `{ECardId}.png` ×28, 대소문자 정확 일치 (Rule 03 §2).

### 6.4 SO 스키마 / 수치 필드
- `CardData._cardImage` (`Sprite`, `[SerializeField]`) + 게터 `CardImage` — plan Task 1. 기존 `_icon` 과 별개 공존.
- `CardView._artImage` (`Image`, `[SerializeField]`) — plan Task 4.

### 6.5 CardView 레이아웃 (LairUIPrefabBuilder.BuildCardViewSlot — plan Task 5 대체 수치)

| 요소 | 속성 | 값 |
|---|---|---|
| CardArt(신규 Image) | anchorMin / Max | (0, 0.4) / (1, 1) |
| | offsetMin / Max | (12, 4) / (−12, −12) |
| | preserveAspect | `true` |
| | color | `Color.white` |
| | raycastTarget | `false` |
| NameText | anchorMin / Max | (0, 0.28) / (1, 0.4) |
| | offsetMin / Max | (8, 0) / (−8, 0) |
| | pivot | (0.5, 0.5) — **현행 (0.5,1) 에서 리셋 필수** |
| | anchoredPosition | (0, 0) — **현행 (0,−30) 에서 리셋 필수** |
| | sizeDelta | (0, 0) — stretch 앵커이므로 0 |
| | fontSize | 32 (고정, auto-size 미적용) |
| DescText | anchorMin / Max | (0, 0) / (1, 0.28) |
| | offsetMin / Max | (16, 14) / (−16, −2) |
| | enableAutoSizing | `true` |
| | fontSizeMin / Max | 16 / 22 |

- CardArt 생성 위치: `Bg` 블록 다음, `NameText` 앞 (형제 순서상 Bg 위 / 텍스트·픽버튼 아래).
- `SetObjectField(cvSo, "_artImage", artImg)` 와이어링 추가.
- 픽 버튼은 현행대로 **마지막에 추가**(형제 최상위 유지) — §4.

### 6.6 CardView.ApplyArt 폴백 (plan Task 4 와 동일, 정책 명시)
- `card.CardImage == null` → `_artImage.gameObject.SetActive(false)`, 텍스트 reflow 없음.
- non-null → `SetActive(true)` + `sprite` 설정.

---

## 7. 불변 / 제외 (락 준수)

| 대상 | 처리 |
|---|---|
| 아이콘 파이프라인(`_icon` / `CardIcons/` / `BuildIconCell`) | 불변 |
| 빌드 모달 / 덱 셀(`BuildModalCardCell`) | 불변 (아이콘 유지, 일러스트 표시 안 함) |
| `CardSelectionPopup` 로직 | 변경 없음 (슬롯 3개 Bind 그대로) |
| 카드 효과·풀 구성·`_effect` 튜닝값 | 비파괴 보존 |
| 새 UI 화면 / 애니메이션 / 카드 뒷면 | 기획 안 함 (YAGNI, 락 범위 밖) |

---

## Self-Review

- **락된 결정 준수**: 표시 위치(3택1 CardView 만) ✅ / 60-40 상단아트-하단텍스트 ✅ / 아이콘 파이프라인 미러 ✅ / 빌드·덱 셀 불변 ✅ / 아이콘 파이프라인 불변 ✅.
- **MVP 범위 준수**: §1 에서 §8 경계 명시 승인 + 컨셉서 §11.4 amend doc-sync 노트. 새 화면·애니·뒷면 기획 안 함(§7).
- **plan 정합**: Task 1~4 그대로. Task 5 앵커는 본 §3.4·§6.5 가 대체(plan↔기획서 sync — NameText/DescText 앵커 0.3→0.28, NameText pivot/anchoredPosition 리셋 명시 보존, DescText auto-size 추가). plan 의 "game-designer override" 표기 경로대로 갱신.
- **Placeholder 잔존 0**: TBD/추후/적절히/또는 없음. 미확정이던 fit·비율·폴백 전부 확정값 + 검산. 후속 전환(B안)은 결정 메트릭 명시.
- **시그니처/명명 일관**: `_cardImage`/`CardImage`/`_artImage`/`ApplyArt`/`CardArt`/`LoadCardImage`/`CardArtDir` — spec·plan·본문 전체 동일.
- **검산**: 아트 rect(296×236)·필러박스(≈59px/측, 40%)·텍스트 밴드 분배(이름 50.4 + 설명 288×101.6)·최장 설명(30자→3줄@22pt, 16pt 하한 2줄) 전부 산식 명시. 모든 폭은 슬롯(320) 직속 좌표계 단일 출처(§3.2↔§3.4↔§6.5 동일).
- **내부 일관성**: §3.2 ↔ §3.4 ↔ §6.5 수치 동일. NameText pivot/anchoredPosition 리셋이 §3.4·§6.5 양쪽에 명시.
