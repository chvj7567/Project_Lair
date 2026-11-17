# 봉인된 둥지 자물쇠 표시 + 해제 조건 팝업 (sealed-nest-lock)

> **사용자용 가이드**: `docs/design/sealed-nest-lock.html` (수치·규칙 SoT 는 이 md)
> **UI 목업**: `.mockups/sealed-nest-lock.html` (메인이 `node .mockups/server.js` 로 제공)
> 관련 문서: `docs/design/chapter-system.md` §3.4(봉인된 둥지 규칙), `docs/design/dungeon-formation.md` §0A.2(봉인 = 종족 둥지 1곳), `docs/design/battle-hud-visual-polish.md` §2.6(스포너 셀), `docs/design/ui-art-refresh.md` §4(아트 세트·팝업 규격)

---

## 0. 헤더

- **목표**: 전투 HUD 하단 둥지 6셀 중 봉인된 둥지를 한눈에 "잠겨 있다"고 보이게(자물쇠 아이콘 + 몬스터 아이콘 어둡게) 하고, 그 셀을 누르면 전투가 멈춘 채 "봉인 해제 조건" 작은 팝업이 뜨게 한다.
- **검증 가설**: 지금은 봉인 상태가 작은 글자 한 줄("봉인 - 영웅 HP 50% 이하 해제")로만 전달된다. 자물쇠 + 탭 설명이 있으면 (1) 플레이어가 "어느 둥지가 왜 안 나오는지"를 전투 중 놓치지 않고, (2) 해제 시점을 기다리는 긴장(HP 구간 목표)이 생기는가. 수치·밸런스는 건드리지 않으므로 qa-simulator 대상이 아니다(§9).
- **현재 단계 범위 적합성**: 승격 전에는 범위 밖이었다(신규 UI 기능·프리팹 구조 변경, §1 판정). **확정(사용자 승인 2026-11-17)**: §1.3 문안이 CLAUDE.md §8 "봉인 둥지 자물쇠·해제 조건 안내 허용" 항목으로 추가되어 범위 안이다.
- **사용자 결정 상태**: 4건 모두 확정(사용자 승인 2026-11-17) — §11.
- **핵심 메커니즘**: ① 봉인 중인 셀에만 자물쇠 아이콘을 얹고 몬스터 아이콘을 어둡게 한다(셀 하단 문구는 짧은 "탭하여 확인" 으로 바꾸고 해제 조건 상세는 팝업에서만 보인다 — 결정 3 확정). ② 봉인 중인 셀만 눌리며, 누르면 전투를 정지하고 기존 공용 확인 팝업(`ConfirmPopup`)으로 "{종족} 둥지가 봉인되어 있다 / 영웅 HP {x}% 이하가 되면 풀린다 / 현재 영웅 HP {y}%" 를 보여 준다. ③ 해제되는 순간 자물쇠가 터지듯 사라지는 0.45초 연출.

---

## 1. 단계 범위 판정과 §8 승격 제안

### 1.1 판정

| 요소 | §8 기존 항목 | 판정 |
|---|---|---|
| (a) 봉인 셀 자물쇠 표시 | "공통 UI 아트 교체" 는 **스프라이트 참조 교체에 한정**, 프리팹 구조 변경·신규 UI 기능 금지 | **범위 밖** — 셀 프리팹에 자식 Image 1개 추가(구조 변경) + 상태 표현 로직(신규 UI 기능) |
| (b) 셀 탭 → 정보 팝업 | "신규 UI 화면" 금지. `SpawnerStatusPanel` 은 v0.6.4 에서 툴팁을 폐기해 셀 클릭 동작이 없다 | **범위 밖** — 폐기한 셀 클릭을 되살리는 신규 상호작용 |
| 팝업 수단 | — | **신규 팝업 프리팹 불필요**. 기존 공용 `ConfirmPopup`(560x320, 팝업 규격표 밖 "확인 팝업 기존 크기 유지") 재사용. 새 크기 규격 결정도 불필요 |

신규 팝업 프리팹이 필요한가: **아니오**. 단, `ConfirmPopup` 은 2버튼 구조라 정보용 1버튼으로 쓰려면 소규모 코드 + 레이아웃 보강이 필요하다(§2.4 옵션 B).

### 1.2 팝업 수단 대안

| 안 | 방법 | 장점 | 단점 |
|---|---|---|---|
| A | `ConfirmPopup` 그대로 재사용, 두 버튼 모두 "닫기" 동작(확인/닫기) | 코드·프리팹 변경 0 | 정보 팝업에 버튼 2개가 같은 동작 — 어색 |
| **B (확정, 사용자 승인 2026-11-17)** | `ConfirmPopup` 에 1버튼 모드 추가(`CancelLabel` 이 비면 취소 버튼 숨기고 확인 버튼을 가운데로) | 새 프리팹·새 크기 없음. 기존 2버튼 호출부(전투 종료·클라우드 복원)는 그대로. 정보 팝업에 맞는 모양 | 공용 팝업 코드·레이아웃 소폭 변경 |
| C | 토스트(`ToastView`) | 전투가 멈추지 않음 | 탭마다 토스트가 쌓이고, 해제 알림("{0} 둥지의 봉인이 풀렸다!")과 같은 알림 통로라 문구가 겹침. 조건·현재 HP 를 읽을 시간이 없다 |

B 채택 이유: 제약이 "새 프리팹·새 크기 규격 없음" 이고, 전투 종료 확인(`HandleExitRequested`)이 이미 같은 패턴(정지 + `ConfirmPopup`)으로 검증돼 있다. B 는 그 패턴을 1버튼으로만 바꾼다.

### 1.3 §8 추가 승격 항목 문안 (확정, 사용자 승인 2026-11-17 — CLAUDE.md §8 에 같은 문안으로 반영됨)

> - **봉인 둥지 자물쇠·해제 조건 안내 허용 (2026-11-17 v0.3 승격) - 단 전투 HUD 하단 스포너 셀의 봉인 표시와 셀 탭 정보 팝업에 한정** - 봉인된 둥지 규칙(`SealedNest`)으로 봉인된 스포너 셀에 자물쇠 아이콘을 얹고 몬스터 아이콘을 어둡게 하며 셀 하단 문구를 짧은 "탭하여 확인" 으로 바꾸고, 봉인 중인 셀을 누르면 전투를 일시정지하고 기존 공용 확인 팝업(`ConfirmPopup`)으로 봉인 둥지(종족)·해제 영웅 HP%·현재 영웅 HP% 를 보여 준다. 해제 순간에는 0.45초 연출을 재생한다. 허용 범위는 ① `SpawnerStatusCell.prefab` 에 자물쇠 Image 자식 1개 추가(기존 위젯 이름·위치·크기 불변)와 셀 봉인 상태 표현 ② `SpawnerStatusPanel` 셀 클릭 재도입(v0.6.4 툴팁 폐기 결정의 부분 번복 — 봉인 셀 한정) ③ `ConfirmPopup` 1버튼 모드 ④ `Strings_Ko` 문구 ⑤ 자물쇠 스프라이트(먼저 임시 `Px_Lock` 참조, 후속으로 전용 1장 `BattleHudUISpawnerLock` 으로 참조 교체). 데이터 스키마·`MetaProfile`·스테이지 규칙·밸런스 수치는 바꾸지 않는다. **신규 팝업 프리팹, 봉인 아닌 셀의 탭 동작(툴팁·정보창), 해제 보상·해제 수단(탭으로 봉인 풀기), 신규 스테이지 규칙, 신규 몬스터, ★ 목표는 계속 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지. 세부는 `docs/design/sealed-nest-lock.md`.

---

## 2. 플레이 경험

### 2.1 봉인 둥지 시각 (하단 문구 = "탭하여 확인", 결정 3 확정)

대상: `Spawner.IsLocked == true` 인 셀(스테이지 규칙 `SealedNest` 가 봉인한 종족 둥지 1곳). 셀 크기는 프리팹 현재값 **188x102** 기준이다(`battle-hud-visual-polish.md` §2.6 의 156x64 는 구값).

| 요소 | 봉인 중 | 봉인 아님(기본·해제 후) |
|---|---|---|
| 자물쇠 | 몬스터 아이콘 슬롯 중심에 40x40 자물쇠 아이콘 표시 | 숨김 |
| 몬스터 아이콘 | 틴트 `#666670`(RGB 0.40/0.40/0.44, 알파 1) — 어둡게 | 기존 흰색 틴트 |
| 아이콘 뒤 발광(`GlowOverlay`) | 숨김(잠긴 느낌 유지) | 기존 강화 표현 그대로 |

**봉인 표현은 `RebindSnapshot` 의 `EnhanceLevelVisual.Apply(...)` 호출 뒤에 덧씌운다**(Apply 가 틴트를 흰색으로 되돌리고 발광을 다시 켜므로). 패시브 픽(영웅 HP 90/80/70/60%)이 봉인 구간(해제 50~70%) 안에서 봉인된 종의 강화 카드를 고르면 스냅샷 갱신 → `RebindSnapshot` 이 불리는 것이 흔한 경로이기 때문이다. 즉 `RebindSnapshot` 끝에서 "현재 봉인 중이면 틴트·발광 끔·자물쇠 켬을 다시 적용" 한다. 테스트 SL11.
| 종명(2줄) | 기존 그대로(어느 종인지 알아야 하므로 유지) | 기존 |
| 하단 문구 | "탭하여 확인"(신규 Strings 1691), 글자색 금색 `#F7C64A`, 글자 크기 14pt, 셀 하단 문구 영역(§2.1-1) 1줄 | 남은 초 "Ns", 기존 글자색 |
| 진행 바·테두리 | 기존 그대로 | 기존 |

**문구와의 관계 = 대체(결정 3 확정)**. 기존 봉인 문구 "봉인 - 영웅 HP {x}% 이하 해제"(Strings 1319)는 셀에 너무 길어 글자가 작아져 읽히지 않는다. 그래서 봉인 중 셀 하단 문구를 짧은 "탭하여 확인" 으로 바꾸고, 해제 조건 상세(해제 HP %·현재 HP)는 팝업(§3)에서만 보여 준다. 대신 해제 % 는 팝업을 열어야만 알 수 있다.

**§2.1-1 하단 문구 영역 재활용**: 새 위젯을 만들지 않고 기존 `txtSpawnTime`(104x36, 글자 자동 크기 12~14pt, 시간 문구가 쓰는 2줄 영역)을 그대로 쓴다. "탭하여 확인" 은 6글자라 14pt 에서 폭 약 84px(14 x 6) < 104px 로 **1줄에 14pt 로 들어가** 자동 크기가 줄지 않는다. 프리팹 변경 없음.

**`PeriodLabel` 변경점**: 봉인 분기(`봉인 중 && SealUnlockHpPercent > 0`)의 반환값을 `string.Format(1319, percent)` 에서 Strings 1691 문구(서식 인자 없음)로 바꾼다. 분기 조건은 그대로 둬 봉인 규칙이 없는 판의 동작을 보존한다. 클래스 상수 `SealTextStringId` 는 1319 → 1691. **Strings 1319 행은 삭제·수정하지 않는다**(`StageRuleTextTests`(1319 문구 `봉인 - 영웅 HP 60%/70% 이하 해제` 검증)와 `WorldMapStringsAndPrefabTests`(1319 존재 검증)가 참조 — 셀은 더 이상 쓰지 않지만 두 테스트는 무수정 통과). 1319 의 정리는 이 기획 범위 밖이다.

### 2.2 해제 시점 전환 연출

기존 해제 연출(영웅 HP 가 해제 % 이하가 되면 사슬 FX 해제 + 즉시 스폰 버스트 + "{종족} 둥지의 봉인이 풀렸다!" 알림)은 그대로다. 셀은 `IsLocked` 가 true→false 로 바뀐 프레임에 아래를 재생한다(비정지 상태에서만 발생 — §6).

| 항목 | 값 |
|---|---|
| 길이 | 0.45초, `Time.unscaledDeltaTime` 기준(컷인 정지 중이어도 끝까지 재생) |
| 자물쇠 | 크기 1.00 → 1.35 배, 알파 1 → 0, 끝나면 숨김 |
| 몬스터 아이콘 틴트 | `#666670` → 흰색 선형 보간 |
| 하단 문구 | 해제 즉시 "Ns" 로 교체 + 글자색 기본 복귀 |
| 소리 | 신규 없음(기존 해제 알림만) |

해제 판정은 래치(`SealedNestRule` E-1)라 HP 회복 후 재봉인되지 않는다 → 셀 연출도 1회만.

### 2.3 탭 동작

| 항목 | 결정 |
|---|---|
| 탭 대상 | 셀 전체(기존 `SpawnerStatusCell` 의 `CHButton` 영역) |
| 봉인 셀 탭 | 즉시 정지 → 정보 팝업(§3) |
| **봉인 아닌 셀 탭** | **무반응, 클릭음도 없음.** 셀 버튼의 상호작용 가능 상태를 `IsLocked` 에 맞춰 두어(봉인 중에만 켬) `CHButton.ClickSoundHook` 이 울리지 않게 한다 |
| 정지 | 팝업이 열려 있는 동안 `Time.timeScale = 0`(`PauseService.Pause`). 전투 타이머·영웅·몬스터·쿨다운·규칙 틱 모두 정지(전투 종료 확인 팝업과 동일) |
| 닫는 방법 | 확인 버튼 · 배경(딤) 탭 · ESC/안드로이드 뒤로가기. 모두 정지 해제 + 전투 재개 |
| 배속 | `PauseService` 가 목표 배속(2배속 on 이면 2.0)을 보관하므로 닫으면 **직전 배속 그대로** 복귀 |

### 2.4 팝업 구현 형태 (옵션 B 기준)

- `EUI.ConfirmPopup` 재사용(새 프리팹 없음), 본체 560x320 그대로.
- `ConfirmPopup` 은 `Close(reuse: true)` 로 인스턴스를 재사용하므로 **`InitUI` 가 매번 두 상태를 모두 명시적으로 설정**한다(이전 호출의 상태가 남지 않게).
  - 1버튼 모드(`ConfirmPopupArg.CancelLabel` 이 null 이거나 빈 문자열): `CancelButton` 숨김, `ConfirmButton`(220x64)과 `DecoConfirmGlow`(248x92)를 X=0.
  - 2버튼 모드(`CancelLabel` 이 채워짐): `CancelButton` 표시, `ConfirmButton`·`DecoConfirmGlow` 를 프리팹 기본 위치 X=-130 으로 복귀. 기존 호출부(전투 종료·클라우드 복원)가 1버튼 호출 뒤에도 원래 모양으로 열리는지는 테스트 SL10 이 검증한다.
- 확인 버튼 문구 "확인"(Strings 1690).

---

## 3. 팝업 내용

| 칸 | 값 |
|---|---|
| 제목 | "봉인된 둥지" (Strings 1688) |
| 본문 | 3줄(Strings 1689, `\n` 구분): ① `{0} 둥지가 봉인되어 있다.` ② `영웅 HP가 {1}% 이하가 되면 봉인이 풀린다.` ③ `현재 영웅 HP {2}% ({3}%p 더 깎으면 해제)` |
| 버튼 | "확인" 1개 |

- `{0}` = 봉인된 둥지의 종족 이름 = `SpeciesVisual.SpeciesName(스포너 CurrentType)`(해제 알림 1231 과 같은 SoT, 등급 접두어 없는 기존 이름. 예: "저주술사").
- `{1}` = 해제 HP% = `SealedNestInfo.UnlockHpPercent(규칙 데이터, 종)`, 해당 종 규칙이 없으면 `SealUnlockHpPercent`(스냅샷, `AnyUnlockHpPercent`) 로 대체.
- `{2}` = 현재 영웅 HP% = `ceil(영웅 HP 비율 x 100)`(정수). **영웅 HP 비율의 출처는 `SealedNestRule` 이 해제 판정에 쓰는 값과 같은 것**(`StageRuleContext.HeroHpRatio` 의 원천인 영웅 `Health.Ratio`)이다. `BattleViewModel.HeroHpRatio` 는 `_model` 기반의 별도 값이라 이벤트 지연으로 어긋날 수 있어 쓰지 않는다. **올림** 이유: 봉인 중에는 `영웅 HP 비율 > 해제 비율` 이므로 올림값은 항상 `{1}` 보다 1 이상 커서 "현재 49% / 해제 50%" 같은 모순 표시가 생기지 않는다.
- `{3}` = `max(1, {2} - {1})`. 현재 데이터의 해제 비율은 0.01 단위(0.50~0.70)라 `{2} - {1}` 이 항상 1 이상이지만, 소수 3자리 비율(예: 0.555)이 생겨도 "0%p" 가 나오지 않도록 1 로 바닥을 둔다.
- **진행도 표시 = 숫자 한 줄만.** 막대는 넣지 않는다(`ConfirmPopup` 은 텍스트 위젯만 있어 막대를 얹으면 신규 UI 가 된다. 영웅 HP 는 정지 상태라 팝업 도중 변하지 않으므로 실시간 갱신도 불필요). 영웅 HP 바(보스 바)는 팝업 딤 뒤에서 그대로 보인다.
- 어느 둥지인지는 `{0}`(종족 이름)으로 표시한다. 둥지 번호(1~6)는 쓰지 않는다 — 전투 HUD 셀에는 번호 표기가 없어 플레이어가 대응시킬 수 없다.

예시 (3-4 봉인된 서고, 저주술사 봉인, 영웅 HP 72.3%):
```
봉인된 둥지
저주술사 둥지가 봉인되어 있다.
영웅 HP가 50% 이하가 되면 봉인이 풀린다.
현재 영웅 HP 73% (23%p 더 깎으면 해제)
[확인]
```

---

## 4. 데이터

**신규 JSON 스키마 없음 → 데이터 구조 확인 게이트(Rule 00) N/A.** 기존 데이터로 충분하다.

| 필요한 값 | 기존 출처 |
|---|---|
| 해제 HP% | `Stage_Rule`/`Stage_Rule_Param` 의 `unlockHpFraction` → `SealedNestInfo.UnlockHpPercent`·`AnyUnlockHpPercent`, 스냅샷 `SealUnlockHpPercent` |
| 봉인 여부 | `Spawner.IsLocked`(`ISpawnerLockState`) |
| 둥지 종족 | 스냅샷 `CurrentType` → `SpeciesVisual.SpeciesName` |
| 현재 영웅 HP 비율 | `SealedNestRule` 이 판정에 쓰는 영웅 `Health.Ratio`(`StageRuleContext.HeroHpRatio` 의 원천, 0~1) — `BattleController` 가 직접 읽는다 |

추가는 `Assets/_Lair/Data/Json/Strings_Ko.json` 문구 4행뿐(현재 마지막 id 1687):

| id | text |
|---|---|
| 1688 | 봉인된 둥지 |
| 1689 | {0} 둥지가 봉인되어 있다.\n영웅 HP가 {1}% 이하가 되면 봉인이 풀린다.\n현재 영웅 HP {2}% ({3}%p 더 깎으면 해제) |
| 1690 | 확인 |
| 1691 | 탭하여 확인 |

---

## 5. 아트: 자물쇠 스프라이트

| 후보 | 위치 | 판정 |
|---|---|---|
| `CodexLock` | `Art/Sprites/CodexUI/` | **사용 불가** — ui-art-refresh §4.2: 도감 파일은 `CommonUILock` 으로 이관되기 전 다른 화면이 직접 참조 금지 |
| `FormationLockBig` | `Art/Sprites/FormationUI/` | **사용 불가** — §4.3: 한 화면 프리팹의 Image 는 `CommonUI*` 또는 그 화면 전용 세트만 참조(편성 세트는 다른 화면) |
| `CommonUILock` | `Art/Sprites/CommonUI/` | **아직 없음**(CommonUI 는 버튼·패널·배지 8장만 존재. `CodexLock` 이관 후보) |
| `Px_Lock` | `Art/Sprites/UiDot/` | 이관 전 임시 사용 허용(ui-art-refresh §5 의 교체 표 "이관 전 임시 `Px_Lock` 유지"). 소형 도트라 40x40 로 확대하면 화풍이 도감·HUD 신규 아트와 어긋남 |

**결정안**:
**확정(사용자 승인 2026-11-17)**: 전용 스프라이트를 새로 그리되, 개발은 임시 `Px_Lock` 으로 먼저 시작하고 완성되면 참조만 교체한다.

- **A 안(후속 교체 대상)**: 전용 스프라이트 **1장 신규** `BattleHudUISpawnerLock`(화면 표시 40x40 의 3배 = 120x120 px, PPU 300, 마젠타/검정 키잉 후처리 — 기존 `BattleHudUI*` 세트 방식). 화풍은 `CodexLock`(어두운 강철 자물쇠, 리벳)과 같은 계열로 제작. 위치 `Assets/_Lair/Art/Sprites/BattleHudUI/BattleHudUISpawnerLock.png`.
- B 안(개발 시작용): 임시로 `Px_Lock` 을 꽂아 기능을 먼저 검증하고, A 안 아트가 준비되면 참조만 교체. 아트 대기 없이 구현이 진행되지만 중간 빌드의 화풍이 어긋난다.
- 아트 제작 자체는 이 기획의 범위가 아니다(제작 파이프라인 `.art-incoming/tools` 담당, 사용자·메인 소관). 구현은 B 안으로 시작하고 A 안 PNG 가 오면 `Sprite` 참조만 바꾼다(프리팹 구조 동일).

---

## 6. 엣지 케이스

| # | 상황 | 처리 |
|---|---|---|
| E-1 | **ESC 한 번에 팝업이 연달아 뜸**: ESC 로 봉인 팝업을 닫은 같은 프레임에 `BattleController.LateUpdate` 가 `RequestExit` 호출 → 전투 종료 팝업이 이어서 열림 | 봉인 팝업의 닫힘 처리가 전투 종료 팝업과 **같은 재개 처리**(`ResumeFromExit` — `_exitCancelFrame = Time.frameCount` 기록 + 재개 + 뒤로가기 버튼 복구)를 호출한다. 닫히는 경로(확인·배경·ESC) 모두 `ConfirmPopup.OnDisable` 의 `OnCancel` 로 수렴 |
| E-2 | 팝업 도중 봉인 해제 | 불가능 — 정지 중 `BattleClock` 이 멈춰 `StageRuleRunner` 가 dt <= 0 으로 틱을 건너뛴다(`SealedNestRule` 은 틱에서만 해제 판정). 정지하지 않으면 팝업 내용이 낡을 수 있어 **정지가 정책** |
| E-3 | 탭 직후 `ShowUIAsync` 가 끝나기 전 더블 탭 | 첫 탭에서 `PauseService.Pause` 와 `SetExitAvailable(false)` 를 먼저 호출하고, 탭 허용 조건에 "정지 중이 아님(`IsPaused == false`)" 을 넣어 두 번째 탭을 무시 |
| E-4 | 카드 선택 팝업·스킬 해금 컷인·전투 종료 확인 팝업·결과 화면 중 셀 탭 | 무시. 허용 조건 = `뒤로가기 버튼 활성 상태(ExitAvailable)` AND `PauseService.IsPaused == false` AND 전투 진행 중. 카드 선택·종료 확인은 이미 `SetExitAvailable(false)`, 컷인은 `Pause()` 를 호출하므로 두 조건이 모두 막는다 |
| E-5 | 탭 순간 이미 해제됨(`IsLocked == false`) | 탭 시점에 `IsLocked` 재확인 후 아니면 아무 것도 안 함(셀 버튼도 이미 비활성) |
| E-6 | **여러 둥지 봉인** | 셀마다 독립 상태·독립 탭. 현재 콘텐츠는 한 판에 `SealedNest` 규칙이 최대 1개(챕터 스테이지 8곳 모두 1규칙, 데일리는 상시 규칙 1 + 페이즈 규칙 1 구성이며 봉인은 상시 쪽만 해당) → 봉인 셀은 1개이고 `dungeon-formation.md` §0A.2 도 규칙 하나당 정확히 1곳을 봉인한다고 정한다. 다수가 되더라도 팝업의 `{1}` 은 **그 셀 종 기준**(`UnlockHpPercent(규칙, 종)`)으로 구해 둥지마다 다른 임계도 맞게 표시 |
| E-7 | 2배속 중 탭 | 정지 시 `Time.timeScale = 0`, 닫으면 `PauseService.TargetScale`(2.0) 복원. 배속 토글 버튼은 팝업 딤 뒤라 눌리지 않음 |
| E-8 | 탭 후 팝업 로드 실패(`ShowUIAsync` null) | 전투 종료 확인과 같게 즉시 재개 처리 호출(영구 정지 방지) |
| E-9 | 해제 연출 도중 카드 선택·컷인으로 정지 | 연출은 unscaled 시간이라 정지 중에도 0.45초 안에 끝난다 |
| E-10 | 전투 종료(`SealedNestRule.Deactivate`)로 일괄 해제 | 해제 연출 재생 가능(결과창이 곧 덮음), 무해. 팝업이 열려 있으면 결과 흐름이 이미 정지 상태라 발생하지 않음 |
| E-11 | 풀 재사용 셀 | `OnEnable` 에서 자물쇠 숨김·틴트·버튼 상태·이전 봉인 상태 플래그 리셋(Rule 03 §4) |
| E-12 | 챕터·데일리·탑 전 모드 | 동일 동작(`BattleController` 공통 경로). 일시정지는 게임 시간만 멈추므로 최단 기록·랭킹 시간에 영향 없음, 정지 중 얻는 새 정보 없음(해제 % 는 셀 문구에 이미 표시) |

---

## 7. 코드 연결점 (구현 담당이 사용할 기존 심볼)

| 용도 | 기존 심볼 |
|---|---|
| 봉인 폴링 | `SpawnerStatusCell` 의 `_lockSource`(`ISpawnerLockState.IsLocked`), `Update` 의 `PeriodLabel()` |
| 해제 HP% | `BattleViewModel.SpawnerSnapshot.SealUnlockHpPercent`, `SealedNestInfo` |
| 영웅 HP | 영웅 `Health.Ratio`(규칙 판정과 같은 값, §3). `BattleViewModel.HeroHpRatio` 는 쓰지 않는다 |
| 셀 클릭 | `SpawnerStatusCell.Bind(..., Action<int> onClick)` — `SpawnerStatusPanel` 이 현재 `null` 을 넘김(툴팁 폐기 주석 갱신 필요) |
| 정지·팝업 | `BattleController.HandleExitRequested` / `ResumeFromExit` 패턴, `PauseService`, `ConfirmPopupArg` |
| 아이콘 틴트 | `EnhanceLevelVisual.Apply(..., baseColor)` 의 마지막 인자 |

**의도 수준 흐름**(구조 결정은 gameplay-programmer): 셀 탭 → 패널 → `BattleHud` → `BattleViewModel.RequestSealInfo(index)` → 이벤트 `OnSealInfoRequested` → `BattleController` 가 정지 + `ConfirmPopup` 표시 → 닫힘 시 재개. 셀은 `IsLocked` 의 true→false 에지를 `Update` 폴링으로 감지해 연출(이벤트 신규 불필요).

---

## 8. 구현 요청사항 (gameplay-programmer 용)

- **Enum**: 신규 없음. 기존 `EUI.ConfirmPopup` 재사용. (자물쇠는 프리팹 인스펙터 참조 Sprite 라 Enum 키 불필요)
- **Interface**: 신규 없음. 기존 `ISpawnerLockState` 사용.
- **에셋 키**: 신규 Addressable 없음. 자물쇠 스프라이트는 프리팹 직접 참조(Addressable 등록 불필요 — 기존 `BattleHudUI*` 와 동일). 1차는 임시 `Px_Lock`, 후속으로 전용 `BattleHudUISpawnerLock`(PNG) 으로 `Sprite` 참조만 교체.
- **SO/JSON 스키마**: 없음. `Strings_Ko.json` 1688·1689·1690·1691 4행 추가(1319 은 유지).
- **코드 변경 목록**:
  1. `SpawnerStatusCell`: `[SerializeField] private Image _sealLockIcon` 슬롯, 봉인 상태 표현(자물쇠 on/off, 아이콘 틴트 `#666670`, `_glowOverlay` 숨김, 하단 문구를 "탭하여 확인"(1691)으로 바꾸고 글자색 `#F7C64A`, `PeriodLabel` 봉인 분기 변경은 §2.1), `IsLocked` 에지 해제 연출(§2.2), 버튼 상호작용 가능 = `IsLocked`, `OnEnable` 리셋. 고정 수치는 클래스 상수로 둔다(`SealTintColor`, `SealTextColor`, `UnlockAnimSeconds = 0.45f`, `UnlockLockScale = 1.35f`).
  2. `SpawnerStatusPanel`: 셀 `onClick` 을 `BattleHud` 가 준 콜백으로 연결(현재 `null`). 주석 "Tooltip 제거 — 클릭 동작 없음" 갱신.
  3. `BattleHud`/`BattleViewModel`: `RequestSealInfo(int index)` 와 `OnSealInfoRequested` 전달.
  4. `BattleController`: `HandleSealInfoRequested(int index)` — 탭 허용 조건(E-3·E-4·E-5) 검사 → 정지 + `SetExitAvailable(false)` → `ConfirmPopupArg`(제목 1688, 본문 1689 서식, 확인 1690, `CancelLabel` 비움, `OnCancel`·`OnConfirm` 모두 재개 처리) 표시 → null 이면 즉시 재개(E-8). 재개 처리는 `ResumeFromExit` 재사용(E-1). 확인 버튼 경로는 `ConfirmPopup` 이 `_pendingCancel` 을 먼저 null 로 만든 뒤 `OnConfirm` 만 호출하고 그 밖의 닫힘은 `OnDisable` 이 `OnCancel` 1회만 호출하므로, 두 콜백에 같은 재개 처리를 넣어도 `Resume` 은 정확히 1회다(이중 재개 아님 — 고치지 말 것).
  5. 팝업 본문 서식은 **순수 정적 함수**로 분리해 테스트 가능하게 한다(입력: 종족 이름, 해제 %, 영웅 HP 비율 → 출력 문자열, §3 올림 규칙).
  6. `ConfirmPopup`: `InitUI` 가 호출마다 1버튼 상태(취소 숨김 + 확인 버튼·글로우 X=0)와 2버튼 상태(취소 표시 + 확인 버튼·글로우 X=-130)를 모두 명시 설정(§2.4, 재사용 인스턴스 오염 방지). 글로우용 `[SerializeField] RectTransform` 슬롯 1개 추가.
- 코드 주석은 `//#`, `var`·`!` 금지, 가드 절 개행(Rule 02).

### 구현 담당 분할

| 담당 | 작업 |
|---|---|
| **gameplay-programmer** | 위 코드 변경 1~6, `Strings_Ko.json` 4행, 슬롯(`_sealLockIcon`·글로우 RectTransform) 추가 |
| **ui-layout-engineer** | `SpawnerStatusCell.prefab` 에 `SealLockIcon`(Image, 40x40, 몬스터 아이콘 슬롯 중심, 렌더 순서 = 몬스터 아이콘·GlowOverlay 위 / 종명·하단 문구 아래, Raycast Target 끔, 기본 비활성) 추가 및 `_sealLockIcon` 연결, `ConfirmPopup.prefab` 글로우 슬롯 연결, 스프라이트 임포트(PPU 300). 기존 위젯 이름·위치·크기 불변. **셀 `Button` 의 Transition 을 None 으로 변경**(현재 프리팹 값은 ColorTint — 그대로 두면 `interactable` 을 끄는 비봉인 셀 5곳이 모든 전투에서 `disabledColor` 로 어두워진다). **`_button` 이 가리키는 오브젝트의 레이캐스트 영역이 셀 188x102 전체를 덮는지 확인**하고 아니면 맞춘다 |
| 사용자/메인 | 자물쇠 PNG 제작(A 안) 또는 B 안 임시 `Px_Lock` 확정 |

---

## 9. 검증 게이트 / 테스트 항목

밸런스·페이싱·스폰 수치를 바꾸지 않으므로 **qa-simulator 시뮬은 불필요**(표시·입력만 변경). 게이트는 아래 EditMode/PlayMode 로 확정한다.

| ID | 구분 | 항목 | 통과 기준 |
|---|---|---|---|
| SL1 | EditMode | 본문 서식 | 비율 0.723·해제 50 → `{2}`=73, `{3}`=23 / 비율 0.5001 → 51, 1 / 비율 1.0 → 100, 50 / 종족 이름 치환 |
| SL2 | EditMode | 올림 불변식 | 해제 비율이 0.01 단위(0.50~0.70)이고 봉인 중(비율 > 해제 비율)인 모든 입력에서 `{2} > {1}`. 0.555 같은 3자리 비율은 `{3}` 이 1 이상으로 바닥 처리되는지만 검증 |
| SL3 | PlayMode | 봉인 셀 시각 | `IsLocked` 일 때 자물쇠 활성·틴트 `#666670`·버튼 상호작용 켬 / 비봉인 셀은 자물쇠 비활성·버튼 끔 |
| SL4 | PlayMode | 탭 → 정지 | 봉인 셀 탭 → `Time.timeScale == 0` + `ConfirmPopup` 열림 + 단일 버튼 |
| SL5 | PlayMode | 닫기 → 재개 | 확인·배경·ESC 각각 → `timeScale == TargetScale`(1.0 과 2배속 2.0 두 경우), 뒤로가기 버튼 복구 |
| SL6 | PlayMode | ESC 이중 팝업 없음 | ESC 로 봉인 팝업을 닫은 직후 전투 종료 확인 팝업이 열리지 않음 |
| SL7 | PlayMode | 가드 | 카드 선택 중·컷인 중 봉인 셀 탭 무시 / 더블 탭에 정지 깊이 1(`Resume` 1회로 완전 복원) |
| SL8 | PlayMode | 해제 연출 | `IsLocked` true→false 후 0.45초 내 자물쇠 비활성·틴트 흰색 복귀·문구 "Ns" 복귀 |
| SL10 | PlayMode | 공용 팝업 재사용 | 봉인 팝업(1버튼)을 열고 닫은 뒤 전투 종료 확인 팝업을 열면 취소 버튼 표시 + 확인 버튼·글로우 X=-130 (2버튼 원래 모양) |
| SL11 | PlayMode | 강화 카드 후 봉인 표현 유지 | 봉인된 종의 강화 카드 픽(스냅샷 갱신) 후에도 자물쇠 on·틴트 `#666670`·발광 off 유지 |
| SL12 | PlayMode | 비봉인 셀 색 | 비봉인 셀 5곳의 아이콘·셀이 전투 내내 `disabledColor` 로 어두워지지 않음(Transition None) |
| SL13 | PlayMode | 봉인 하단 문구 | 봉인 중 하단 문구 == Strings 1691 "탭하여 확인", 색 `#F7C64A`, 1줄(자동 크기 14pt 유지) / 해제 후 "Ns" |
| SL9 | 회귀 | 기존 테스트 | `BattleHudPrefabLayoutTests`·`SpawnerStatusUIPlayTests`·`StageRuleTextTests`(1319 문구)·`WorldMapStringsAndPrefabTests`(1319 존재) **무수정 통과**. `SpawnerStatusUIPlayTests` 가 봉인 셀의 하단 문구를 1319 서식으로 단언하는지 구현 시 확인하고, 단언하면 1691 로 갱신한다(유일한 허용 수정)(신규 자식은 기존 이름 `Tick_*` 등을 쓰지 않고, 기존 위젯을 이동·개명하지 않는다). `ConfirmPopup` 2버튼 호출부(전투 종료·클라우드 복원) 동작 불변 |

---

## 10. Self-Review 메모

- 신규 데이터 스키마 없음(§4), 승격 문안 §1.3, 두 벌(md + HTML)과 목업 작성.
- 사용자 결정 사항은 HTML 맨 아래 "결정해 주세요" 와 동일(§11).

## 11. 사용자 결정 (4건 모두 확정, 사용자 승인 2026-11-17)

| # | 질문 | 추천 |
|---|---|---|
| Q1 | §8 에 §1.3 문안으로 승격할까요? | **확정** — 승격(CLAUDE.md §8 항목 추가) |
| Q2 | 팝업 버튼을 1개로(옵션 B)? 2개 그대로(옵션 A)? | **확정** — B(1버튼 "확인") |
| Q3 | 셀 하단 봉인 문구 | **확정**: "탭하여 확인" 으로 줄이고 조건 상세는 팝업에서만(글자가 작아 읽히지 않기 때문) |
| Q4 | 자물쇠 그림을 새로 만들까요(A), 임시 도트 자물쇠로 먼저 갈까요(B)? | **확정** — 새로 그리되(`BattleHudUISpawnerLock`) 먼저 임시 `Px_Lock` 으로 개발 시작 |
