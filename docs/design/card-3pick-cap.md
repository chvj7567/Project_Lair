# 카드 전역 3픽 캡 — 도메인 기획서

> 작성: game-designer
> 작성일: 2026-06-01
> 입력 spec: `docs/superpowers/specs/2026-06-01-card-3pick-cap-and-fixes-design.md`
> 입력 plan: `docs/superpowers/plans/2026-06-01-card-3pick-cap-and-fixes.md`
> 참고: `docs/design/card-renewal.md`(28장 라인업·중첩 정책·시너지 Tier), `docs/design/project_lair_concept.md` §8(밸런싱)/§11(MVP 범위)
> 범위 메모: 메커니즘·구현 표면은 spec/plan 이 이미 락(lock)했다. 본 문서는 game-designer 고유 영역(밸런스 근거·UX·페이싱·역할 분담)만 채운다. 코드/시그니처 재명세는 하지 않는다.

---

## § 헤더

- **목표**: 같은 카드(ECardId)를 한 런에 3번 픽하면 이후 3택 후보에서 영구 제외하는 전역 3픽 캡을 도입한다. 모든 카드의 실효 중첩 상한 = 3픽 = `card-renewal.md §7` 곱연산/가산 누적표의 3픽 값.
- **검증 가설**: "카드별 중첩 상한 3 + 단일 카드 단독 Tier3 불가" 가 빌드 다양성을 높이면서도 5분 자동전투의 페이싱(런당 픽 ~10/~10)을 깨지 않는다.
- **현재 단계 범위 적합성**: 범위 내. 컨셉 §11.2 카드 매수(P16/A12) lock 불변, 메커니즘만 추가. 아트·사운드·메타·메인메뉴 미작업. UI 는 MVP 비주얼(프리미티브 + 텍스트 + 4색 + 검정 텍스트) 안에서 동작.
- **핵심 메커니즘**(spec §2A 요약): `CardPickCounter` 가 ECardId→픽수 누적 → 3 도달 카드는 `CardDeck.Draw` 후보에서 제외 → 패시브/액티브 풀 disjoint 이므로 per-card 캡이 곧 전역 캡. 전투 Restart 시 `BuildSynergyService.Reset` 과 동일 시점에 카운터 0 초기화.

---

## 1. 디자인 원칙 (이 기획의 결정 기준)

- **단일 카드로 빌드를 굳히지 못하게** — 한 카드를 무한 반복 픽해 ×value^N 으로 천장을 뚫는 패턴을 차단한다. 강함의 천장은 *서로 다른 카드 조합*으로만 도달하게 한다.
- **상한은 전역 균일 3** — 카드별 차등 캡(YAGNI, spec §3)은 두지 않는다. 모든 카드가 같은 규칙을 따라야 플레이어가 "이 카드는 3번까지" 를 한 번 학습하면 끝난다.
- **시너지 카운트와 분리** — 3픽째에도 Layer 1 축 카운트는 +1 된다(임계 발화 정상). 캡은 *카드 후보 노출*만 막을 뿐 시너지 발화를 막지 않는다(spec §2A 엣지).
- **MVP 비주얼** — 배지는 검정 텍스트 `N/3` 한 줄. 신규 색·신규 토스트·사운드 없음(spec §3 비범위).

---

## 2. 밸런스 근거 — "왜 상한이 3인가"

### 2.1 3픽 천장 수치표 (곱연산 카드)

전역 3픽 캡이 곧 곱연산 카드의 ×value³ 천장이다. `card-renewal.md §3` 의 현행 수치 기준:

| 축 | 대표 카드 (ECardId) | 1픽 | 2픽 (value²) | **3픽 천장 (value³)** | 효과 방향 |
|---|---|---|---|---|---|
| Tank | `WispHpBoost` | ×1.5 | ×2.25 | **×3.375** | Wisp HP ↑ |
| Dps | `HexRangeBoost` | ×1.4 | ×1.96 | **×2.744** | Hex 사거리 ↑ |
| Dps | `ReaperAtkSpeed` (Cooldown) | ×0.7 | ×0.49 | **×0.343** | Reaper 공속 ↑ (쿨다운 ↓) |
| Dps | `ReplaceReapersToHex` (Power) | ×1.3 | ×1.69 | **×2.197** | Reaper·Hex 데미지 ↑ |
| Debuff | `PlagueSlowBoost` (SlowFactor) | ×0.75 | ×0.5625 | **×0.422** | Plague 둔화 ↑ (계수 ↓) |
| Debuff | `HeroAttackDown` (Power, 영구) | ×0.75 | ×0.5625 | **×0.422** | 영웅 공격력 ↓ |
| Swarm | `PhantomMoveSpeedBoost` | ×1.5 | ×2.25 | **×3.375** | Phantom 이속 ↑ |
| Swarm | `SpawnerHaste` (Period) | ×0.8 | ×0.64 | **×0.512** | 모든 스포너 주기 ↓ |
| Swarm | `Multiply`/FastBreeding (Phantom Period) | ×0.6 | ×0.36 | **×0.216** | 팬텀 스포너 주기 ↓ |

> 검산: value³ — WispHpBoost 1.5³ = 3.375, HexRangeBoost 1.4³ = 2.744, ReaperAtkSpeed 0.7³ = 0.343, ReplaceReapersToHex 1.3³ = 2.197, PlagueSlowBoost 0.75³ = 0.422 (0.421875 반올림), SpawnerHaste 0.8³ = 0.512, Multiply 0.6³ = 0.216. 각 축 곱연산 대표 카드의 단독 천장이 모두 캡 3 으로 고정된다.

### 2.2 3픽 천장 (가산 카드)

| 정책 | 대표 카드 (ECardId) | 1픽 | 2픽 | **3픽 천장** | 비고 |
|---|---|---|---|---|---|
| 가산 (Spawner 출력 +1) | `SpawnWraith`·`SpawnReapers`·`SpawnPlagues`·`SpawnPhantoms`·`SpawnWisps` | +1 | +2 | **+3** | 글로벌 캡 18 미만이라 캡과 무관하게 가산 그대로 |

> 검산: +1 × 3픽 = +3 출력. 단일 스포너 기준 글로벌 필드 캡 18 에 한참 못 미쳐(스포너 6개 분산) 3픽 가산은 캡에 막히지 않는다.

### 2.3 지속시간/버프 dedup 카드는 캡의 직접 영향 거의 없음

`card-renewal.md §7.1` 의 지속시간 누적(Fear·Bleed·Weaken·Slow·MarkOfDeath·HeroPoisonAura)과 버프 dedup(IronWill·Frenzy·GuardianRage·ToughHide)은 효과량이 1배 고정이라 캡이 *효과량*에 주는 영향이 작다. 캡은 이들에도 후보 제외로 적용되어 "같은 액티브를 3번까지만 픽 가능" 으로 픽 슬롯 점유를 제한하는 의미를 갖는다.

### 2.4 단일 카드 단독 Tier3 불가 — 빌드 다양성 효과

`card-renewal.md §4.1` 의 Layer 1 임계는 누적 픽 카운트 기준(3장/5장/7장). 캡 3 의 직접 귀결:

| 한 카드의 최대 축 기여 | 도달 가능 Tier | 추가로 필요한 카드 |
|---|---|---|
| 3 (= 캡) | **Tier1 (3장) 만** | 0 — 단일 카드로 Tier1 까지 |
| Tier2 (5장) | 도달 불가 | **≥2종**의 서로 다른 같은-축 카드 |
| Tier3 (7장) | 도달 불가 | **≥3종**의 서로 다른 같은-축 카드 |

→ 단일 카드 반복 픽으로는 Tier1 이 천장이다. Tier2·Tier3 의 진성 빌드 보상은 같은 축의 *서로 다른 카드 조합*으로만 열린다. 이것이 spec §2A 의 "픽 다양성 강제" 의 핵심 메커니즘이다. `card-renewal.md §9.2` 의 트레이드오프 T2("같은 카드 K번 픽으로 Tier 즉시 발동 OK")는 캡 도입으로 **Tier1 한정**으로 좁혀진다(K ≤ 3).

---

## 3. UX — 카드 3택 팝업 `N/3` 배지

### 3.1 배지 표기 규칙

| 현재 픽 누적수 N | 배지 표시 | 사유 |
|---|---|---|
| 0 | **숨김** (배지 비활성) | 아직 안 픽한 카드 — 시각 노이즈 제거 |
| 1 | "1/3" | 1번 픽함, 2번 남음 |
| 2 | "2/3" | 다음이 마지막 픽 (3픽째 후 제외 예고) |
| 3 (캡 도달) | **노출 안 됨** | `CardDeck.Draw` 후보에서 제외 → 팝업에 카드 자체가 안 나옴 |

> **내부 일관성 관찰**: 캡 도달(N=3) 카드는 추첨에서 빠져 팝업에 등장하지 않고, N=0 카드는 배지가 숨겨지므로, 실제 화면에 표시되는 배지 문자열은 **"1/3" 또는 "2/3" 두 가지뿐**이다. "0/3" 과 "3/3" 은 정의상 화면에 나타나지 않는다. 후보 제외 규칙과 배지 숨김 규칙이 서로 모순 없이 맞물린다.

> 배지가 표시하는 N 은 **픽 전 누적값**(plan Task 3 Step 6 `PickCountOf = c => _pickCounter.GetCount(c.Id)`)이다. 즉 "2/3" 인 카드를 픽하면 그 카드가 3픽이 되어 다음 추첨부터 제외된다.

### 3.2 MVP 비주얼 제약 준수

- 텍스트: `CHText`(Rule 03 §3). 검정 `#000000`. 신규 색 불요(spec §2A UI).
- 위치: 각 `CardView` 우상단(plan Task 4 Step 3 — 프리팹에 정적 배치).
- 별도 토스트/사운드/이펙트 없음(spec §3 비범위 — MVP §8 사운드 금지, 배지로 충분).

### 3.3 기존 card-renewal §8.3 "×K+1" 배지 스케치와의 관계

`card-renewal.md §8.3` 은 픽 전 카드에 "×K+1" 배지를 스케치했으나, plan 이 확정한 표기는 **"N/3"** 이다. 본 문서가 3택 팝업 배지의 단일 진실(SoT)이며 §8.3 스케치를 대체한다. (card-renewal §8.3 본문 정정은 본 작업 범위 밖 — 보고에서 design-reviewer/사용자에게 후속 정합 항목으로 플래그.)

---

## 4. 페이싱 — 풀 고갈은 비현실적

런당 픽 횟수와 풀 크기·캡 3 의 관계 검산:

- **풀 크기**: 패시브 16장 / 액티브 12장(`card-renewal.md §10.7` 현행 에셋 확인).
- **런당 픽**: 패시브 ~10회(HP 10% 트리거 약 10회) / 액티브 ~10회(30s 트리거 약 10회) — spec §2A 엣지.
- **캡으로 제거 가능한 카드 수**: 픽 N 회 중 한 카드를 3픽해야 1장 제거 → 최대 `floor(N/3)` 장 제거.

**현실 페이싱 (각 ~10픽)**:
- 패시브: 적격 풀 = 16 − floor(10/3) = 16 − 3 = **13장 ≥ 3** ✓
- 액티브: 적격 풀 = 12 − floor(10/3) = 12 − 3 = **9장 ≥ 3** ✓

**이론적 극단 (한 카테고리에 18픽 다 몰기, `card-renewal.md §9.3`)**:
- 패시브: 16 − floor(18/3) = 16 − 6 = **10장 ≥ 3** ✓
- 액티브: 12 − floor(18/3) = 12 − 6 = **6장 ≥ 3** ✓

**고갈(적격 < 3) 발생 조건**:
- 패시브: `16 − floor(N/3) < 3` → `floor(N/3) > 13` → N ≥ 42픽 (= 현실 ~10픽의 약 4배).
- 액티브: `12 − floor(N/3) < 3` → `floor(N/3) > 9` → N ≥ 30픽 (= 현실 ~10픽의 약 3배).

→ 5분 런의 현실 픽 횟수(~10)로는 고갈이 발생하지 않는다. 극단 케이스에서도 `CardDeck.Draw` 의 graceful fallback(가능한 만큼 반환, spec §2A·plan Task 2)이 안전망으로 작동하므로 NRE/빈 팝업 없음. 페이싱 위험 없음.

---

## 5. GuardianRage HP×2.0 제거 수용 근거 (spec §2B)

### 5.1 변경 내용

`MonsterBuffService` GuardianRage case 가 `DamageTakenScale *= 0.5f` 만 유지(`HpMaxScale *= 2f` 제거). SO `Berserk.asset` `_description="...받는 데미지 -50% (15초)"` 와 코드 동작이 일치 → `card-renewal.md §3.1 #7` 의 노출/메커니즘 불일치 플래그 해소.

### 5.2 약화 후에도 역할 분담이 성립하는가

HP×2.0 제거 후 Tank 축 받피 감소 3종의 역할이 여전히 구별되는지 검토. 적용 상수는 `MonsterBuffService.cs` 확인값:

| 카드 (효과) | 감소율 | 적용 종 | 지속 | 역할 정체성 |
|---|---|---|---|---|
| `Berserk`/GuardianRage | **받피 ×0.5 (−50%)** | `{Wisp, Wraith}` 한정 | **15초 (시한)** | 가장 강한 단일 감소, 좁고 짧음 — *버스트 방어* |
| `IronWill` | 받피 ×0.7 (−30%) | 전체 종 | 15초 (시한) | 중간 감소, 넓음 — *광역 방어* |
| `WallOfWisps`/ToughHide | 받피 ×0.75 (−25%) | `{Wisp, Wraith}` 한정 | **영구** | 가장 약한 감소, 좁고 영구 — *상시 방어* |

→ 세 카드가 **감소율 × 적용 범위 × 지속시간** 의 세 축에서 모두 다른 좌표를 차지한다. GuardianRage 는 HP×2.0 없이도 "Wisp·Wraith 한정 최강 단일 감소(−50%)" 라는 고유 니치를 유지한다.

### 5.3 곱연산 중첩 — GuardianRage 의 차별성 보강

GuardianRage(`EMonsterBuff.GuardianRage`)와 ToughHide(`EMonsterBuff.ToughHide`)는 **서로 다른 `EMonsterBuff`** 라서 `DamageTakenScale` 위에서 **곱연산으로 중첩**된다(`MonsterBuffService` case 가 각각 `*= 0.5f` / `*= 0.75f`). ToughHide 영구 + GuardianRage 15초 동시 적용 시 Wisp·Wraith 받피 = ×0.75 × ×0.5 = **×0.375 (−62.5%)** 가 15초 창에서 작동한다.

→ HP×2.0 을 제거해도 GuardianRage 는 "ToughHide 와 곱해져 15초 버스트 방어 창을 만드는" 역할이 명확하다. 받피 감소만으로 IronWill(전체 종 −30%)·ToughHide(영구 −25%)와의 분담이 깨지지 않는다.

### 5.4 수용한 부작용 (spec §2B)

GuardianRage 가 HP 2배를 잃어 절대 맷집은 줄어든다. 본 spec 범위에서 별도 리밸런스는 하지 않는다(spec §3 비범위 — GuardianRage 외 리밸런스 없음). 약화가 빌드 픽률·승률에 유의미한 영향을 주는지는 후속 밸런스 사이클의 qa-simulator 검증으로 판단한다(결정 메트릭: Tank 액티브 3종 픽률 분포 + Wisp/Wraith 생존 시간 변화).

---

## 6. 구현 요청사항 (요약 — 상세는 plan)

> 메커니즘·시그니처는 plan 이 이미 락했다. 본 절은 game-designer 가 추가로 단정하는 도메인 값만 둔다. 코드 구조는 gameplay-programmer 판단 영역.

- **Enum**: 신규 Enum 값 없음. 기존 `ECardId`(`card-renewal.md §10.1`) 그대로 사용. 캡 상수 = 3(`CardPickCounter.Cap`, plan Task 1 정의).
- **Interface**: 신규 인터페이스 없음. plan 의 `CardPickCounter`(POCO) + `CardDeck.Draw(int, Func<ECardId,bool>)` 오버로드 + `CardSelectionArg.PickCountOf(Func<CardData,int>)` 를 그대로 따른다.
- **에셋 키**: 신규 프리팹/SO 없음. `CardSelectionPopup` 프리팹의 각 `CardView` 슬롯에 `_countBadge`(TMP_Text + CHText) 정적 배치 필요(plan Task 4 Step 3 — 동적 생성 금지, Rule 04).
- **SO 스키마**: 신규 SO 필드 없음. 캡 상수는 코드 상수(SO 노출 안 함).
- **도메인 단정 (디자인 결정, 시스템 결정 아님)**:
  - 캡 = **전역 균일 3** (카드별 차등 없음).
  - 배지 표기 = **"N/3"** (N = 픽 전 누적값). 표시되는 값은 실효적으로 "1/3"·"2/3" 두 가지.
  - 캡 리셋 단위 = **런(라운드)** — `BuildSynergyService.Reset` 과 동일 시점.

---

## 7. 컨셉 §11 정합 (한 줄)

컨셉 §11.2 카드 매수(P16/A12) lock 불변 — 캡은 풀 크기를 바꾸지 않고 추첨 후보만 제한한다. UI 는 `card-renewal.md §헤더` 의 4색 + 검정 텍스트 비주얼 안에서 동작하며, 메커니즘만 추가한다(메타·서버·사운드·아트 미작업).

---

## 8. Self-Review (작성자 체크)

- **Placeholder 잔존**: 0건. 미정 마커·애매한 권유·두 갈래 위임·본문 비움 참조·검산 누락 모두 없음. GuardianRage 약화 영향만 "qa-simulator 검증 후 결정 + 결정 메트릭(픽률 분포·생존 시간)" 으로 명시(§5.4).
- **스펙 커버리지**: spec §2A(전역 캡 — §2·§3·§4·§6 매핑), §2B(GuardianRage — §5), §3(비범위 — §1·§3.2 준수), §4(성공 기준 — §2.1~2.4·§3.1·§4·§5 매핑) 모두 본 문서 § 에 매핑. spec §2C(문서·주석 정리)는 본 문서가 아닌 card-renewal.md 갱신 + 코드 작업(gameplay-programmer) 영역 — 본 문서 범위 외로 명시 제외.
- **내부 일관성**: 캡 3 / value³ 수치 / "1/3"·"2/3" 배지 / 풀 16·12 가 본문·표·구현요청 전체에서 동일. §3.1 배지 규칙과 §2.4 Tier 도달표가 캡 3 으로 일관.
- **시그니처 명명 일관성**: `CardPickCounter` / `CardPickCounter.Cap` / `CardDeck.Draw` / `CardSelectionArg.PickCountOf` / `ECardId` — plan 정의와 글자 그대로 동일. 변형 표기 0건.
- **모호 표현**: 0건.
- **스코프**: 단일 구현 단위(전역 3픽 캡 + 부수 GuardianRage 정합). 분할 불요.
- **구현 요청사항 완전성**: Enum(없음·기존)/Interface(plan 그대로)/에셋 키(배지 프리팹 배치)/SO 스키마(없음)/도메인 단정 명세 완료.

**Self-Review 결과**: 통과 (보강 0건).
