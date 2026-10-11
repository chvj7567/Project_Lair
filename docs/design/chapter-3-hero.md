# 챕터 3 "그림자 성채" + 신규 영웅 암살자 (Assassin) - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-08 · 단계: v0.3
> 입력: `docs/design/chapter-system.md`(§1 데이터 구조, §3 규칙 6종, §4 챕터/스테이지 표·톱니형 곡선·`ruleFactor`, §5 궁수 영웅, §10 결정 항목) · `hero-2d-conversion.md` · `hero-stage-variant.md` · `hero-skills.md` · `project_lair_concept.md`(빌드 4축, 몬스터 목록) · 시안 `hero-archer-2d.html` / `hero-2d-conversion.html`
> **시안(합의용)**: `.mockups/hero-ch3-2d.html` (메인이 `node .mockups/server.js` 로 `http://localhost:8777/hero-ch3-2d.html` 제공)
> **데이터 구조표**: 본 문서 §2 (신규 스키마 `HeroArchetype_Skirmish` 1종 - 사용자 승인 게이트 대상)

---

## § 헤더

- **목표**: 챕터 3(5스테이지)과 챕터 영웅 **암살자**(근접 습격 + 이탈 반복 AI, 스킬 3종, 5단계 성장 외형)를 기획한다. 기사(정면 근접)·궁수(원거리 카이팅)와 다른 세 번째 플레이 압박을 만들고, 규칙은 기존 6종만 재사용한다(신규 규칙 0종).
- **검증 가설**: (1) 영웅이 "DPS÷현재HP 가 가장 높은 몬스터"를 쫓는 사냥꾼일 때, 플레이어가 고DPS 저HP 몬스터(Dps 축) 대신 두꺼운 몬스터와 Debuff 로 빌드를 바꾸는가. (2) 기사 -> 궁수 -> 암살자 순으로 영웅 직업만으로 "다른 판" 체감이 유지되는가. (3) 톱니형 곡선(Ch2 보스 -> Ch3 S1 하강)이 3번째 챕터에서도 진행 동기를 유지하는가.
- **현재 단계 범위 적합성**: **범위 밖 - 사용자 승인 필요.** CLAUDE.md §8 은 챕터 구조를 1단계(Ch1+Ch2)로 한정하고 **Ch3~Ch5 는 개별 승격 전까지 구현 금지**로 둔다. 이 기획서와 시안(`.mockups/hero-ch3-2d.html`)은 합의용이며, **사용자 승인 후 §8 승격(챕터 3 한정, §9 문안)이 반영되기 전에는 코드·에셋 임포트·데이터 파일 추가에 착수하지 않는다.** 서버 연동 범위(Firebase 클라이언트 코드만)·메인 메뉴/세팅 화면 금지는 변경 없음. 월드맵은 마을 HUD 안 팝업을 유지한다.
- **핵심 메커니즘**: (a) 암살자는 `threatScore`(몬스터 DPS ÷ 현재 HP)가 최상위인 몬스터를 사냥한다. 몬스터 HP 를 올리는 카드(Tank 축)는 표적 순위를 낮추고, 몬스터 공격력을 올리는 카드(Dps 축)는 표적 순위를 높인다 - 별도 수치 없이 카드 효과가 그대로 가위바위보가 된다. (b) 한 표적과 2.8초 교전했거나 주변에 3마리 이상 몰리면 0.2초 무적 이동(슬립, 쿨다운 5.0초)으로 빠졌다가 다시 사냥한다. (c) 스킬 3종(그림자 습격 / 칼날 선풍 / 연막 탄)은 기존 영웅 스킬 시스템(HP 85/65/45% 게이트)을 재사용한다.

---

## 1. 챕터 3 콘셉트

### 1.1 테마와 서사

- 챕터명 **그림자 성채** (`nameKey` 1003), 테마 문구 **자객단의 침투** (`themeTextKey` 1013).
- 서사: 1장 지하 묘지(기사단 침공) -> 2장 어둠의 숲(숲속 정찰대) -> 3장 던전 입구 위쪽 성채에서 영웅 측 첩자가 던전 안으로 직접 침투한다. 정면 돌파(기사), 원거리 정찰(궁수) 다음의 **은밀 침투**.
- 스테이지명(Strings_Ko): 1121 성채의 외벽 / 1122 그림자 회랑 / 1123 달빛 없는 정원 / 1124 봉인된 서고 / 1125 자객 수장의 은신처.

### 1.2 영웅 직업 선택 - 대안 3개와 근거

기사(정면 근접, 중앙 고정)와 궁수(사거리 6.0 카이팅)에 이어 세 번째 직업은 **이동 패턴·표적 선택·플레이어가 잡아야 하는 빌드 축**이 앞의 둘과 모두 달라야 한다.

| 안 | 플레이 방식 | 빌드 압박 | 장점 | 단점 |
|---|---|---|---|---|
| **A. 암살자 (권장)** | 우선 표적을 쫓아 근접 습격 후 슬립으로 이탈. 표적 = `threatScore` 최상위 | Dps 축(고DPS 저HP 몬스터) 약, Tank·Debuff 강 | 기사와 같은 근접이지만 이동·표적 규칙이 완전히 달라 "다른 판". 궁수(Tank 약)와 압박 축이 반대라 챕터마다 해답이 바뀜. 해골 세계관에 쌍단검·복면 실루엣이 기사/궁수와 명확히 구분됨 | 신규 AI(Skirmisher)와 스킬 3종 구현 필요 |
| B. 사제 | 제자리에서 자가 회복 + 신성 피해 오라 | Debuff 의 회복 감소가 정답 | 구현이 가장 쉬움(정지형) | 성수 샘(`HolySpring`)이 이미 영웅 회복 규칙이라 개성이 겹침. 이동 없는 영웅은 3번째 챕터에서 기사보다 변화가 작음 |
| C. 마법사 | 원거리 광역 캐스팅 | Swarm 약 | 시각적 화려함 | 궁수와 원거리라는 축이 겹침. 광역기는 `ArrowRain`/`ArrowVolley` 와 필드 점유율이 겹쳐 G4 검증 부담 |

**결정(권장): A. 암살자.** 사용자가 다른 가지를 택하면 §3 이후(스탯·AI·스킬)를 해당 직업으로 교체해야 한다(결정 항목 D-C3-2).

### 1.3 4축 빌드 가위바위보

암살자의 표적은 `threatScore = (몬스터 power ÷ cooldown) ÷ max(1, 현재 HP)` 가 가장 높은 몬스터다. 기본 몬스터 수치에서 점수는 다음과 같다(`balance_config.json` 기준, chapter-system §4.1 dps-share 표와 동일 출처).

| 종 | DPS | HP | threatScore | 순위 |
|---|---|---|---|---|
| Hex | 9.0 | 60 | 0.150 | 1 |
| Reaper | 12.0 | 100 | 0.120 | 2 |
| Phantom | 2.5 | 30 | 0.083 | 3 |
| Plague | 2.0 | 50 | 0.040 | 4 |
| Wisp | 5.0 | 200 | 0.025 | 5 |
| Wraith | 10.0 | 500 | 0.020 | 6 |

검산: 9.0/60 = 0.150, 12/100 = 0.120, 2.5/30 = 0.0833, 2/50 = 0.040, 5/200 = 0.025, 10/500 = 0.020.

| 축 | 암살자 상대 | 이유 |
|---|---|---|
| **Tank** | **강** | HP 증가 카드가 점수 분모를 키워 두꺼운 몬스터는 표적 순위 밖으로 밀리고, 영웅의 기본 공격(스테이지당 1타 35~61)으로는 HP 200/500 몬스터를 빨리 못 뚫는다. 벽 역할 몬스터가 영웅을 막는다 |
| **Dps** | **약** | Hex·Reaper 같은 고DPS 저HP 몬스터가 최우선 표적이다. 공격력 증가 카드는 점수 분자를 키워 표적 순위를 더 올린다. 딜러를 지키려면 Tank 가 필요해 빌드 단일화가 막힌다 |
| **Debuff** | **강** | 영웅 유효 체력이 낮아(`hpScale` 0.70) 지속 피해·%피해·약화 카드 효율이 높다. 슬립으로 군중을 빠져나가므로 둔화(Plague)로 이동속도 4.2 를 깎으면 습격 주기가 늘어난다 |
| **Swarm** | 중립 | Phantom(점수 3위)은 저HP 라 쉽게 처치되지만 군중 조건(2.0 안 3마리)이 슬립을 발동시켜 습격을 끊는다. 수로 압박하되 단일 개체 질로는 이기지 못한다 |

궁수 챕터가 "Tank 약"이었다면 암살자 챕터는 "Dps 약 / Tank 강" - 챕터마다 정답 빌드가 바뀐다. 이 가위바위보는 qa-simulator 게이트 G7(§8.3)로 검증하며, 어긋나면 §3 AI 표적 규칙 수치만 조정한다.

---

## 2. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 챕터 3 스테이지는 **기존 스키마에 row 만 추가**한다(스키마 변경 없음). 신규 스키마는 `HeroArchetype_Skirmish` 1종과 `hero_skills.json` 스킬 3종뿐이다.

### 2.1 엔티티 / 관계 트리 (변경분만)

```
Chapter (기존)        ChapterRow      <- row 3 추가
 +-- Stage (기존)     StageRow        <- (3,1)~(3,5) 5 row 추가
 |     +-- Stage_Rule / _Param / _Trigger (기존)  <- 3-2~3-5 row 추가 (§5.2)
 +-- (Chapter.heroId) --> HeroArchetype (기존)  <- row "Assassin" 추가
                           +-- HeroArchetype_Kite (기존, Archer 만)
                           +-- HeroArchetype_Skirmish (신규, 선택적 관계)  HeroArchetype_SkirmishRow
                           |        <- aiType=Skirmisher 인 영웅만 존재 (Assassin 1 row)
                           +-- (스킬 로드아웃) hero_skills.json loadout row (heroId="Assassin")
SO (Object 참조): HeroStageVariantConfig <- ChapterId=3 엔트리 5개 추가
```

핵심 엔티티 한 문장: `HeroArchetype_SkirmishRow` = "습격-이탈 AI 를 쓰는 영웅 직업 하나의 AI 튜닝 값". 영웅의 고유 스탯은 `HeroArchetypeRow` 에, AI 종류별 튜닝은 선택적 관계 데이터에 두는 기존 방식(`HeroArchetype_Kite`)과 같다. sentinel(-1/0/null)을 쓰지 않는다.

### 2.2 신규 row 필드 표

**HeroArchetype_SkirmishRow** - `HeroArchetype_Skirmish.json` (EData `HeroArchetype_Skirmish`)

| 필드 | 타입 | 키/관계 | 설명 | Assassin 초안값 |
|---|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow.heroId` | | "Assassin" |
| `huntRadius` | float | | 표적 후보를 찾는 반경 | 9.0 |
| `retargetScoreMul` | float | | 현재 표적을 유지하다가 다른 몬스터 점수가 현재 표적 점수의 이 배수를 넘으면 교체 (진동 방지) | 1.5 |
| `strikeWindowSec` | float | | 한 표적과 교전을 이어가는 최대 시간. 초과 시 슬립 | 2.8 |
| `crowdCount` | int | | 이 수 이상의 몬스터가 `crowdRadius` 안에 있으면 슬립 | 3 |
| `crowdRadius` | float | | 군중 판정 반경 | 2.0 |
| `slipDistance` | float | | 슬립 이동 거리 | 3.5 |
| `slipDurationSec` | float | | 슬립 지속 시간 | 0.2 |
| `slipInvulnSec` | float | | 슬립 시작부터의 무적 시간 | 0.2 |
| `slipCooldownSec` | float | | 슬립 재사용 대기 | 5.0 |
| `minDwellSec` | float | | 상태 전이 후 최소 유지(진동 방지) | 0.4 |
| `arenaSoftRadius` | float | | 슬립 목적지를 이 반경 안으로 클램프 (궁수 `HeroArchetype_Kite` 와 같은 값이지만 테이블 간 의존을 피하려고 별도 보관) | 7.0 |

**HeroArchetypeRow 추가 row** (기존 스키마, chapter-system §1.2)

| heroId | nameKey | hpScale | powerScale | range | cooldown | moveSpeed | aiType |
|---|---|---|---|---|---|---|---|
| Assassin | 1023 (암살자) | 0.70 | 0.70 | 1.6 | 0.7 | 4.2 | Skirmisher |

**ChapterRow 추가 row**

| chapterId | nameKey | themeTextKey | heroId | backdropKey |
|---|---|---|---|---|
| 3 | 1003 | 1013 | Assassin | Battle_Backdrop_ShadowCitadel |

**StageRow 추가 5 row**: §5.3 표.

**신규 스킬 `$type` 3종** (기존 `hero_skills.json` `skills[]` 확장, 필드는 §4)

### 2.3 Strings_Ko id 배정 (현행 최대 id 는 chapter-system §1.5 기준 1318)

| id | 값 |
|---|---|
| 1003 | 그림자 성채 |
| 1013 | 자객단의 침투 |
| 1023 | 암살자 |
| 1121~1125 | 성채의 외벽 / 그림자 회랑 / 달빛 없는 정원 / 봉인된 서고 / 자객 수장의 은신처 |

규칙명·설명 문구(1201~1226)는 기존 id 를 그대로 쓴다. 단 보스 규칙 `HunterTraps` 의 표시명 1206 "사냥꾼의 덫"은 3-5 에서도 쓰이므로 **"덫"으로 일반화**한다(결정 항목 D-C3-6).

---

## 3. 암살자 영웅 (Assassin)

### 3.1 직업 개요

- 콘셉트: 성채 첩자 출신의 뼈 암살자. 복면 + 쌍단검 + 길게 날리는 목도리. 정면에서 버티지 않고 **가장 위험하고 약한 몬스터**부터 순서대로 지운다.
- 기사와 같은 근접이지만 중앙에 서지 않고(`_centerPullEnabled` off), 궁수처럼 거리를 유지하지도 않는다. 대신 **돌진 - 교전 - 이탈** 사이클을 반복한다.

### 3.2 스탯 틀 (초안 - G0/G1 검증 후 확정)

| 항목 | 값 | 기사 / 궁수 | 근거 |
|---|---|---|---|
| hpScale / powerScale | 0.70 / 0.70 | 1.00 / 1.00, 0.80 / 0.80 | 가장 얇은 영웅. 이동·무적으로 보완 |
| range / cooldown / moveSpeed | 1.6 / 0.7 / 4.2 | 1.5 / 1.0 / 3.0, 6.0 / 1.0 / 3.2 | 근접(사거리 +0.1), 빠른 연타, 가장 빠른 이동 |
| aiType | Skirmisher (신규) | Melee / Kiter | §3.3 |
| 기본 공격 | 근접 즉발 판정. 공격 모션 총 0.6초, `OnAttackStrike` 0.200s(F3), `OnAttackEnd` 0.580s(F8) | | 쿨다운 0.7초 중 0.1초 여유. 기사의 fallback 1.8초(`_attackEndFallback`)를 쓰면 공격 주기가 1.8초로 늘어 DPS 가 61% 하락(0.7/1.8 = 0.39)하므로 궁수와 같이 종료 이벤트를 클립에 굽는다(D-C3-8) |

**스테이지별 실효 공격력** (앵커 Power 50 x 0.70 = 35, 1회 반올림): `powerMultiplier` 1.20 / 1.30 / 1.45 / 1.60 / 1.75 -> 42 / 46 / 51 / 56 / 61. 검산: 35 x 1.20 = 42.0, x 1.30 = 45.5 -> 46, x 1.45 = 50.75 -> 51, x 1.60 = 56.0, x 1.75 = 61.25 -> 61. 이론 DPS(교전 100% 가정) = 타격 ÷ 0.7 = 60.0 / 65.7 / 72.9 / 80.0 / 87.1. 교전 시간 비율 55~75% 가정 시 실효 33.0~65.3(= 60.0 x 0.55 ~ 87.1 x 0.75). 기사 같은 인덱스 스테이지(50~75)의 70~100% 범위를 G5 게이트로 확인한다.

### 3.3 AI - Skirmisher 사양

**상태**

| 상태 | 진입 | 행동 | 해제 |
|---|---|---|---|
| 추적 | 표적이 있고 거리 > `range`(1.6) | 표적 방향으로 `moveSpeed`(4.2) 이동 | 거리 <= 1.6 -> 교전 |
| 교전 | 표적 거리 <= 1.6 | 정지 + 표적 응시 + 기본 공격(쿨다운 0.7). 교전 시간 누적 | 표적 사망 -> 표적 재선택(추적). `strikeWindowSec` 초과 또는 군중 판정 -> 슬립 |
| 슬립 | (교전 시간 >= `strikeWindowSec`) 또는 (`crowdRadius` 안 몬스터 >= `crowdCount`), 그리고 슬립 쿨다운 경과, `minDwellSec` 경과 | 군중(반경 3.0 안) 무게중심 반대 방향으로 `slipDistance` 이동, `slipInvulnSec` 무적. 무게중심이 자기 위치와 같으면 원점 방향 | 이동 완료 -> 추적 |

- **표적 선택**(threatScore = 공격력 x 공격 배율 ÷ 쿨다운 ÷ max(1, 현재 HP), 쿨다운은 버프(프렌지 등) 반영 실효값): `huntRadius` 안 모든 몬스터 중 `threatScore` 최대, 동점이면 가까운 몬스터. 봉인(`SealedNest`)된 종족이 필드에 없으면 후보에서 자연 제외. 후보가 없으면 가장 가까운 몬스터.
- **표적 유지**: 다른 몬스터 점수가 현재 표적 점수의 `retargetScoreMul`(1.5)배를 넘을 때만 교체.
- 슬립은 스킬이 아니라 **AI 기본 행동**이다(HP 게이트 없음, 처음부터 사용). 스킬 `연막 탄`은 시전 직후 슬립을 쿨다운 무시로 1회 발동한다.
- 중앙 끌림(`_centerPullEnabled`)은 암살자 프리팹에서 끈다.

**`AutoCombatAI` 충돌 점검 포인트 (gameplay-programmer 필독)**

| # | 기존 로직 | 충돌/주의 | 처리 방침 |
|---|---|---|---|
| 1 | Update 게이트 순서: 사망 -> TimeStop -> 스폰 게이트 -> `IsAttacking` -> 타깃 없음 -> 고정 타깃 -> FleeMode -> 중앙 끌림 -> 교전/이동 | Skirmisher 분기는 궁수 Kiter 와 같은 자리(FleeMode 뒤, 교전/이동 앞) | `aiType == Skirmisher` 일 때만 활성(`SkirmishSettings` 주입). 기사·궁수 경로는 동작 불변 |
| 2 | `IsAttacking` 동안 이동 정지 | 공격 모션 0.6초 동안 슬립이 막힘 | 군중 판정은 공격 모션이 끝난 뒤 평가(위험 노출 최대 0.6초). 슬립 무적 0.2초가 보완 |
| 3 | 고정 타깃 `_retargetMargin 1.5`(거리 기준) | 암살자 표적은 점수 기준 | 암살자는 점수 기준 히스테리시스(`retargetScoreMul`)로 대체하고 거리 기준 고정 타깃은 쓰지 않음 |
| 4 | `TryGetFleeCentroid` | 슬립 방향 계산에 재사용 가능 | 군중 무게중심 집계에 재사용 |
| 5 | 스킬 이동(그림자 습격 순간이동) | AI 이동 명령과 충돌 | 스킬이 이동 소유권을 가지는 구간(순간이동 시전 시점 1프레임)과 슬립 구간은 AI 이동 중단. 궁수 `회피 도약`과 같은 방식 |
| 6 | Fear 카드(FleeMode) | 공포 걸리면 슬립과 겹침 | FleeMode 우선(기존 순서). 카드 수치 불변, qa 에서 Fear 픽률 모니터링 |
| 7 | `HeroHologramMode`(마을 쇼케이스) | 암살자도 쇼케이스 영웅 | 마을에서는 전투·AI 컴포넌트 off(기사/궁수 쇼케이스와 같은 경로) |

### 3.4 클립 사양 (시트 stab 행)

- 공격(`stab`): 9프레임 x 15fps = 0.600s. F0~F1 웅크림 -> F2 돌진 -> **F3 앞 단검 찌름 = strike**(0.200~0.267s 구간, 이벤트 0.200s: 0.200 x 15 = 3.0 -> F3) -> F4~F5 뒷 단검 베기(연출만, 데미지 없음) -> F6~F8 복귀(F8 = 대기 F0 와 같은 포즈). `OnAttackEnd` 0.580s(F8 구간 0.533~0.600s).
- 이동: `walk`(8프레임 10fps) / `run`(8프레임 14fps). 슬립과 그림자 습격은 `run` 클립 + 잔상 FX 로 표현하며 별도 클립은 만들지 않는다.
- 시트 규격은 기사·궁수와 같다: 셀 72px, 20열 x 9행, 행 순서 idle/walk/run/slash01/slash02/stab/hit/death/spawn, pivot (0.4236, 0.2917), PPU 31. `slash01`·`slash02` 행은 `stab` 행 프레임 복제(궁수 D-15 와 같은 방식, D-C3-8).

### 3.5 5단계 외형 (`HeroStageVariantConfig` ChapterId=3)

기사·궁수와 같은 규약(단계별 완성형을 독립 시트로 베이크, 2단계 림 `#F8FAFC`, 5단계 림 `#FDE047`, 발광 3~5단계, 5단계만 루트 스케일). 시트 파일: `Assassin_Sheet.png`(3-1) + `Assassin_Sheet_S2.png` ~ `Assassin_Sheet_S5.png` 5장과 각 `_Emission.png` 5장, `Assassin_SheetSpec.json` 1개. `tiers` = `{Assassin_Sheet:0, Assassin_Sheet_S2:1, Assassin_Sheet_S3:2, Assassin_Sheet_S4:3, Assassin_Sheet_S5:4}`.

| 스테이지 | 외형 | `Tier` | `UseEmission` | `EmissionIntensity` | `ScaleMultiplier` | A안 몸 색 / 림 / 발광 | B안 몸 색 / 림 / 발광 |
|---|---|---|---|---|---|---|---|
| 3-1 성채의 외벽 | 둥근 두건 · 복면 · 단검(날 4.6) · 짧은 목도리 · 맨 뼈 팔 | 0 | false | 0 | 1.00 | `#A1A1AA` 철회색 / 흑(#0A0D14) / 없음 | `#5B5F97` 먹구름 인디고 / 흑 / 없음 |
| 3-2 그림자 회랑 | 뾰족 두건 · 제비꼬리 목도리 · 곡도(날 5.8) · 앞 어깨 보호대 · 등 칼집 1 | 1 | false | 0 | 1.00 | `#14B8A0` 청록 / `#F8FAFC` / 없음 | `#6B70B8` 짙은 인디고 / `#F8FAFC` / 없음 |
| 3-3 달빛 없는 정원 | 쌍 뾰족 두건 + 이마 보석 · 찢어진 외투 목도리 · 발광 곡도(날 6.8) · 양 어깨 보호대 | 2 | true | 1.5 | 1.00 | `#6366F1` 인디고 / 흑 / `#818CF8` | `#6366F1` 형광 인디고 / 흑 / `#818CF8` |
| 3-4 봉인된 서고 | 양뿔(끝 발광) · 이중 목도리 · 대형 단검(날 8.0) · 가시 어깨 보호대 · 교차 칼집 | 3 | true | 2.2 | 1.00 | `#D946D9` 마젠타 / `#F5D0FE` / `#E879F9` | `#818CF8` 연청 인디고 / `#C7D2FE` / `#A5B4FC` |
| 3-5 자객 수장의 은신처 | 칼날 왕관 + 금 띠 · 거대 이중 외투 · 흑요 낫칼(날 10.5, 금 날밑) · 대형 교차 칼집 · 바닥 오라 | 4 | true | 3.2 | **1.20** | `#3F3F46` 흑요석(몸 `#18181B`) / `#FDE047` / `#FDE047` | `#BCC4FF`(몸 `#12122B`) / `#FDE047` / `#BCC4FF` |

- **스케일 1.20 근거**: 기사 보스 1.4(근접 콜라이더 포함 기승인), 궁수 1.25(후퇴 경로 보호). 암살자는 슬립·순간이동으로 좁은 틈을 오가므로 콜라이더가 커질수록 이탈 경로가 좁아진다. 1.25 보다 낮은 1.20 으로 두고 qa G5 의 슬립 실패(군중에 갇힘) 횟수로 확인한다.
- **실루엣**: 기사 `hero-2d-conversion.md` §3.3 의 R2'(돌출 >= 4/6/9/14 텍셀)·R3'(증가율 >= 10/18/30/45%)를 같은 값으로 적용한다. 시안 3장 표가 원화를 실측한다(미달 시 빨강). 설계 의도 돌출: 3-2 두건 끝 +4.8셀, 3-3 +5.6셀, 3-4 양뿔 +8.6셀, 3-5 칼날 왕관 +16.5셀·목도리 -16셀(셀 -> 48px 환산 x1.55).
- **세 직업 구분**: 기사 = 대각 직선 검 + 낮은 캡, 궁수 = 세로 호 활 + 뾰족 후드 + 등 화살통, 암살자 = 양손 쌍단검(가로로 짧고 낮은 자세) + 복면 + 가로로 길게 날리는 목도리. 시안 6장이 세 직업을 같은 도트 크기·발 기준으로 나란히 비교한다.

**팔레트 대안 (사용자 선택 필요, 결정 항목 D-C3-3)**

기사 유채색(그린 142° · 시안 199° · 퍼플 271° · 크림슨 0°)과 궁수 A/B 대표색(36°~84°, 329°)이 이미 색상환을 차지해 **양쪽에서 31° 이상 떨어지는 구간은 인디고 230°~240° 대역뿐**이다(다른 빈 구간의 폭은 56°~59° 라 한가운데도 28°~29°). 따라서 "31° 이상 전 단계 충족"은 단색조 안에서만 가능하다.

| 안 | 내용 | 기사·궁수와의 최소 색상각 | 장점 | 단점 |
|---|---|---|---|---|
| **A. 색상 점프형 - 권장** | 철회색 -> 청록 -> 인디고 -> 마젠타 -> 흑요석+금 | 청록 27°, 인디고 32°, 마젠타 29°(궁수 장미 29° / 기사 퍼플 29°), 철회색·흑요석은 무채색 판정 제외. 기존 규약 25° 는 모두 충족, 31° 충족은 인디고 1단계 | 기사·궁수와 같은 문법(단계마다 색상각 점프)이라 5단계가 색만으로도 구분. 궁수 A안이 승인된 선례 | 청록·마젠타가 기사 유채색/궁수 장미와 28° 전후라 31° 규약에는 못 미침 |
| B. 인디고 단일 색조 | 인디고 한 색조 명도 램프(233°~239°) + 포인트 발광 | 전 단계 31° 이상(최소 34°) | 기사·궁수와 색으로 혼동될 여지가 가장 낮음 | 인접 단계 색상각 0°·3°·4°·2°(시안 0장 표 실측) 라 단계 구분을 실루엣·발광에 의존(궁수 B안과 같은 약점) |

권장 이유: 색 + 실루엣 이중 신호 설계(`hero-stage-variant.md` §1.3)를 유지하려면 색 신호가 강한 A 가 유리하고, 궁수 A안이 같은 이유로 확정된 선례다. 31° 규약을 엄격히 우선하면 B 를 택한다. 두 안의 수치는 시안 0장 표가 hex 에서 직접 계산한다.

---

## 4. 스킬 3종 (영웅 스킬 시스템 `hero_skills.json` 확장)

게이트: 기사·궁수와 같은 HP **85% / 65% / 45%**(패시브 임계와 0.05 오프셋 유지). 기사 스킬 구조(`HeroSkillRunner`, 쿨다운 자동 시전, 컷인)를 그대로 재사용한다. 모든 수치는 초안이며 G0/G1 검증 후 확정한다.

| 스킬 (`$type`, `fileName`) | 게이트 | 필드 / 값 | 효과 · 처치 타수 |
|---|---|---|---|
| **그림자 습격** `ShadowStrikeSkillData`, `HeroSkill_ShadowStrike` ("그림자 습격") | 85% | `damage` 120 / `cooldown` 6.0 / `blinkRange` 9.0 / `minBlinkDistance` 2.5 / `arriveOffset` 1.2 / `invulnerableSec` 0.3 / `killRefundFraction` 0.5 | `threatScore` 최상위 몬스터(§3.3 표적 규칙)가 `minBlinkDistance` 보다 멀고 `blinkRange` 안이면 표적 1.2 앞으로 순간이동하며 120 피해. 처치 시 남은 쿨다운의 50% 환급(쿨다운 6.0 -> 3.0). 처치 타수: Phantom 30·Plague 50·Hex 60·Reaper 100 = 1타, Wisp 200 = 2타, Wraith 500 = 5타 |
| **칼날 선풍** `BladeWhirlSkillData`, `HeroSkill_BladeWhirl` ("칼날 선풍") | 65% | `damagePerTick` 35 / `tickCount` 4 / `tickIntervalSec` 0.3 / `radius` 2.4 / `cooldown` 9.0 / `damageTakenMul` 0.7 / `triggerMonsterCount` 2 / `triggerRadius` 2.4 | 반경 2.4 안에 몬스터 2마리 이상이면 1.2초(0.3초 x 4) 동안 회전 베기. 총 140/대상(1틱 1회). Phantom·Plague·Hex·Reaper 처치, Wisp·Wraith 면역(140 < 200). 시전 중 영웅이 받는 피해 x0.7 |
| **연막 탄** `SmokeBombSkillData`, `HeroSkill_SmokeBomb` ("연막 탄") | 45% | `radius` 3.0 / `durationSec` 3.0 / `monsterSpeedMul` 0.6 / `monsterRangeMul` 0.5 / `monsterMinRange` 0.8 / `cooldown` 12.0 / `triggerMonsterCount` 4 / `triggerRadius` 2.4 | 반경 2.4 안에 몬스터 4마리 이상이면 시전. 현재 위치에 3.0초 연막 지대 생성(안의 몬스터 이동속도 x0.6, 공격 사거리 x0.5, 하한 0.8 - `DenseFog` 의 곱 적용 방식 재사용). 시전 직후 슬립을 쿨다운 무시로 1회 발동해 군중을 빠져나간다 |

- `loadout` row: `{heroId: "Assassin", hpFraction: 0.85, skill: "HeroSkill_ShadowStrike"}`, `{.., 0.65, "HeroSkill_BladeWhirl"}`, `{.., 0.45, "HeroSkill_SmokeBomb"}`.
- 자동 시전 우선순위(여러 조건이 동시에 참일 때): 연막 탄 > 그림자 습격 > 칼날 선풍. 근거: 군중 탈출이 가장 위급하고, 표적 처치가 그다음이다.
- 이론 상한 보조 DPS(밀집 가정, 참고): 그림자 습격 120/6.0 = 20, 칼날 선풍 140 x 2.5마리/9.0 = 38.9. 합 58.9 로 기본 DPS(33.0~65.3 실효)와 같은 규모이며, 궁수(45 + 44)·기사 대비 낮지 않다. 실효 값은 qa G5 에서 확인.
- 스킬 컷인(시간정지 1.9초 x 3회)은 기사·궁수와 같은 방식으로 재사용한다.
- 신규 효과 로직은 기존 컨텍스트 재사용이 원칙이다: 이동속도 배율(`HunterTraps`), 사거리 배율(`DenseFog`), 구형 범위 피해(`IHeroSkillContext`), 영웅 받는 피해 배율(`BossBulwark`). 순간이동(그림자 습격)만 신규 이동 능력이다.

---

## 5. 챕터 3 스테이지와 난이도 곡선

### 5.1 규칙 배정 (신규 규칙 0종, 기존 6종 재사용)

콘셉트: 성채의 자객단이 방어 장치를 가동한다. 암살자 특성상 **영웅 측 규칙(회복·보호·함정)** 과 **봉인(공급 차단)** 을 섞어 "쫓는 영웅과 쫓기는 몬스터" 서사를 만든다.

| 스테이지 | 규칙 | 의도 |
|---|---|---|
| 3-1 성채의 외벽 | 없음 | 톱니 골. 암살자 AI(표적 사냥/슬립) 학습 |
| 3-2 그림자 회랑 | `SealedNest` (Plague 봉인, 해제 0.60) | 둔화(Plague)가 막힌 채 시작 - Debuff 해답이 늦게 열리는 압박. 해제 후 Plague 합류 |
| 3-3 달빛 없는 정원 | `HolySpring` | 영웅이 슬립·추적으로 샘 밖에 머물러(체류율 0.3 가정) 회복 효율이 낮다. 샘 위치를 노려 몬스터를 모으는 선택지 |
| 3-4 봉인된 서고 | `SealedNest` (Hex 봉인, 해제 0.50) + `DenseFog` | 최우선 표적(Hex)이 반이 지날 때까지 없다 - 표적 규칙이 Reaper/Phantom 으로 밀리는 변주. 안개로 Hex 사거리 5.0 -> 3.5 |
| 3-5 자객 수장의 은신처 | `BossBulwark` (0.75) + `HunterTraps` (0.35) | 보스 영웅의 결계(받는 피해 x0.6, 15초)로 습격 중 처치가 어려워지고, 후반 덫 6곳이 몬스터를 느리게 만들어 군중 판정이 더 자주 걸린다 |

### 5.2 규칙 row (`Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`)

| chapterId | stageIndex | ruleIndex | ruleId | 트리거 hpFraction | 파라미터 |
|---|---|---|---|---|---|
| 3 | 2 | 1 | SealedNest | (row 없음 = 상시) | `lockedSpecies` 4 (Plague), `unlockHpFraction` 0.60 |
| 3 | 3 | 1 | HolySpring | (없음) | `centerX` 0.0, `centerZ` 0.0, `radius` 3.0, `healPercentPerSec` 0.4, `activeSec` 8, `periodSec` 40, `firstActiveSec` 20 |
| 3 | 4 | 1 | SealedNest | (없음) | `lockedSpecies` 3 (Hex), `unlockHpFraction` 0.50 |
| 3 | 4 | 2 | DenseFog | (없음) | `monsterRangeMul` 0.7, `minRange` 0.8 |
| 3 | 5 | 1 | BossBulwark | 0.75 | `damageTakenMul` 0.6, `durationSec` 15 |
| 3 | 5 | 2 | HunterTraps | 0.35 | `trapCount` 6, `ringDistance` 3.5, `trapRadius` 1.2, `lifeSec` 12, `rearmSec` 15, `monsterSpeedMul` 0.5, `damagePerSec` 10 |

- `EMonster` 정수값: Wisp 0 · Wraith 1 · Reaper 2 · Hex 3 · Plague 4 · Phantom 5 (chapter-system §1.2 규약).
- HP 임계 중복 방지(§3.2 규칙): 보스 페이즈 0.75 / 0.35 는 패시브(0.9~0.1)·스킬(0.85/0.65/0.45)과 0.05 이상 이격. 봉인 해제 0.60 / 0.50 은 패시브 임계와 일치하므로 해제 효과는 카드 팝업이 닫힌 뒤 처리(chapter-system E-4).
- 3-5 에서 암살자 스킬 `칼날 선풍`(받는 피해 x0.7)과 `BossBulwark`(x0.6)가 겹치면 곱연산(0.42)이다. 하한 정책은 두지 않으며 qa G7 에서 보스 구간 영웅 생존 시간을 확인한다(엣지 E-C3-5).

### 5.3 스테이지 표 (`Stage.json`, T1 = 130초 기준 도출, 모두 G0/G1 검증 후 확정)

도출식(chapter-system §0.4): `hpMultiplier = round2(targetClearSec x M_ref / T1 / (hpScale x ruleFactor))`, `eq 배수 = targetClearSec x M_ref / T1`. `hpScale` = 0.70. `M_ref` Ch3 = 1.17 / 1.19 / 1.21 / 1.23 / 1.25(Ch2 1.16 에서 스테이지당 +0.02 연장 가정, 상점 최대 효과 0.35 대비 Ch3 보스 71% 소진 가정).

| 스테이지 | 스테이지명(id) | 규칙 | targetClearSec | M_ref | eq 배수 | ruleFactor | **hpMultiplier** | **powerMultiplier** | `ScaleMultiplier` |
|---|---|---|---|---|---|---|---|---|---|
| 3-1 | 성채의 외벽 (1121) | 없음 | 160 | 1.17 | 1.44 | 1.000 | **2.06** | **1.20** | 1.00 |
| 3-2 | 그림자 회랑 (1122) | SealedNest(Plague) | 180 | 1.19 | 1.65 | 1.025 | **2.30** | **1.30** | 1.00 |
| 3-3 | 달빛 없는 정원 (1123) | HolySpring | 200 | 1.21 | 1.86 | 1.048 | **2.54** | **1.45** | 1.00 |
| 3-4 | 봉인된 서고 (1124) | SealedNest(Hex) + DenseFog | 220 | 1.23 | 2.08 | 1.101 | **2.70** | **1.60** | 1.00 |
| 3-5 | 자객 수장의 은신처 (1125) | BossBulwark + HunterTraps | 255 | 1.25 | 2.45 | 1.093 | **3.20** | **1.75** | 1.20 |

**`ruleFactor` 산식** (dps-share 비중은 chapter-system §4.1: Wisp 17.0% · Wraith 15.3% · Reaper 30.6% · Hex 18.3% · Plague 6.1% · Phantom 12.7%)

| 스테이지 | 산식 | 값 |
|---|---|---|
| 3-2 | Plague 비중 6.1% x 봉인 시간 비율(1 - 0.60 = 40%) = 2.44% 압력 감소 -> 1 / (1 - 0.0244) | 1.025 |
| 3-3 | 개방 횟수 floor((200 - 20) / 40) + 1 = 5회. 5 x (0.4% x 8s = 3.2%) x 영웅 체류율 0.3(슬립·추적으로 샘 밖) = 4.8% -> 1 + 0.048 | 1.048 |
| 3-4 | Hex 비중 18.3% x (1 - 0.50 = 50%) = 9.17% -> 1 / (1 - 0.0917). 안개는 미보정(Hex 위협 감소는 영웅 측 이득이나 정량 모델 없음) | 1.101 |
| 3-5 | BossBulwark: 15s x 40% 경감 x 후반 피격 가중 1.3 / 목표 255s = 3.06% -> 1.031. HunterTraps 추정 6% -> 1.06. 곱 1.031 x 1.06 | 1.093 |

> 3-5 의 ruleFactor 1.093 은 **추정치**다(정량 모델 없음, Ch2 S5 의 1.120 과 같은 방식). qa-simulator 게이트 G6/G7 실측 후 도출식으로 재산출한다.

**검산** (T1 = 130, 소수 셋째 자리 미만 절사 전 값에서 round2)

| 스테이지 | eq 비반올림 | 분모 hpScale x ruleFactor | hpMultiplier 비반올림 | 역검산(0.70 x hpMult x ruleFactor) |
|---|---|---|---|---|
| 3-1 | 160 x 1.17 / 130 = 1.4400 | 0.70 x 1.000 = 0.7000 | 2.0571 -> 2.06 | 0.70 x 2.06 x 1.000 = 1.442 |
| 3-2 | 180 x 1.19 / 130 = 1.6477 | 0.70 x 1.025 = 0.7175 | 2.2964 -> 2.30 | 0.70 x 2.30 x 1.025 = 1.650 |
| 3-3 | 200 x 1.21 / 130 = 1.8615 | 0.70 x 1.048 = 0.7336 | 2.5375 -> 2.54 | 0.70 x 2.54 x 1.048 = 1.863 |
| 3-4 | 220 x 1.23 / 130 = 2.0815 | 0.70 x 1.101 = 0.7707 | 2.7008 -> 2.70 | 0.70 x 2.70 x 1.101 = 2.081 |
| 3-5 | 255 x 1.25 / 130 = 2.4519 | 0.70 x 1.093 = 0.7651 | 3.2048 -> 3.20 | 0.70 x 3.20 x 1.093 = 2.448 |

표의 eq 배수 열은 위 비반올림 값의 round2(1.44 / 1.65 / 1.86 / 2.08 / 2.45)다. 절대 HP = `A x 0.70 x hpMultiplier` (`A` = G0 확정 HP 앵커). 절대 공격력 = 앵커 Power 50 x 0.70 x `powerMultiplier`(§3.2).

### 5.4 톱니형 곡선과 상한 점검

| 구간 | eq 배수 | 비고 |
|---|---|---|
| Ch2 S5 -> Ch3 S1 | 2.28 -> **1.44** | **-37% 하강** (톱니 골). (2.28 - 1.44) / 2.28 = 0.368. Ch1->Ch2 와 같은 -37% |
| Ch3 S1 -> S5 | 1.44 -> 1.65 -> 1.86 -> 2.08 -> 2.45 | 단조 증가 (+0.21 / +0.21 / +0.22 / +0.37: 보스 도약) |
| 챕터 S1 끼리 | 1.00 -> 1.25 -> 1.44 | 챕터를 거듭할수록 골의 높이가 상승 |

- 상한 점검: 최대 eq 2.45 <= chapter-system §0.4 의 Ch3 참고 상한 2.45, 절대 상한 `C(130)` 2.80 이내. 분자 `hpScale x hpMultiplier` 최대 = 0.70 x 3.20 = 2.24 로 3.5 미만.
- **난이도 방향**: 3-1 은 Ch2 3~4번째 스테이지 수준에서 재시작해 암살자 AI 학습, 3-2~3-4 는 규칙 1~2개씩 늘려 표적 규칙이 흔들리는 변주(Plague 봉인 -> 샘 -> Hex 봉인+안개), 3-5 는 보스 페이즈 2종으로 마감. 모든 수치는 **G0 -> G1 -> G6 -> G7 순 검증 후 확정**하며 어긋나면 위 도출식으로 `Stage.json` 을 일괄 재산출한다(규칙 구조·파라미터 불변).

---

## 6. 에셋 요구 (승인·§8 승격 후 Claude 제작)

| 에셋 | 사양 |
|---|---|
| `Assassin_Sheet.png` + `Assassin_Sheet_S2~S5.png` (+ 각 `_Emission.png`) + `Assassin_SheetSpec.json` | `Knight_SheetSpec.json`/`Archer_SheetSpec.json` 동일 규격(§3.4). 시안 `hero-ch3-2d.html` 의 부위 리그를 시트로 베이크(시트 추출은 승인 후 별도 작업, 시안에는 추출 기능 없음) |
| `HeroIcons/Assassin.png`, `Assassin_S2~S5.png` | 스테이지 초상 5장 (기사·궁수 초상 규격 동일) |
| FX (프리팹 = `EVisual` 값명) | `AssassinSlashFx` `HeroSlipFx` `HeroShadowStrikeFx` `HeroBladeWhirlFx` `HeroSmokeBombFx` (규칙 FX 는 기존 `StageSpringFx` `StageFogFx` `StageSealChainFx` `StageBulwarkFx` `StageTrapFx` 재사용) |
| 배경 `Battle_Backdrop_ShadowCitadel.png` | 성채 내부 도트 배경(`Battle_Backdrop.png` 해상도·레이어 규격 동일). 색 톤은 팔레트 A/B 선택에 맞춰 영웅 대비를 확인 후 확정(시안의 보라 계열 배경은 임시) |
| 사운드 | 이번 범위 제외(후속) |

---

## 7. 구현 요청사항 (gameplay-programmer 용 - 승인·§8 승격 후)

### 7.1 Enum 값 (`CommonEnum.cs`, 카테고리별 분리 - Rule 02 §8, Rule 03 §2)

| Enum | 추가 값 | 비고 |
|---|---|---|
| `EHero` | `Assassin` | `Archer` 뒤에 추가(기존 직렬화 정합) |
| `EHeroAi` | `Skirmisher` | `Melee`, `Kiter` 뒤 |
| `EData` | `HeroArchetype_Skirmish`, `HeroSkillLoadout_Assassin` | JSON/SO 파일명 = 값명 |
| `EBackdrop` | `Battle_Backdrop_ShadowCitadel` | `ChapterRow.backdropKey` 값명 |
| `EVisual` | `AssassinSlashFx`, `HeroSlipFx`, `HeroShadowStrikeFx`, `HeroBladeWhirlFx`, `HeroSmokeBombFx` | 기존 값 뒤에 추가(int 직렬화 정합), 프리팹 `CHMPool` 대상 |

`EStageRule`·`EUI` 신규 값 없음.

### 7.2 Interface (`CommonInterface*.cs`, Rule 02 §9)

- 영웅 AI 설정 주입: 궁수 `KiteSettings` 와 같은 방식으로 `SkirmishSettings`(`HeroArchetype_SkirmishRow` 로부터 주입). 인터페이스 신설 여부는 gameplay-programmer 판단.
- 표적 점수 질의(`threatScore` 계산에 몬스터 power·cooldown·현재 HP 접근): 기존 몬스터 컨텍스트 인터페이스에 읽기 전용 질의로 추가하는지는 gameplay-programmer 판단. 카드로 변동된 현재 HP/공격력을 반영해야 한다는 요구만 고정한다.
- 신규 스킬 효과 컨텍스트: 순간이동(영웅 위치 이동), 연막 지대(몬스터 속도·사거리 배율 임시 적용). 기존 `IHeroSkillContext` 확장 여부는 gameplay-programmer 판단.

### 7.3 에셋 키 / 데이터 파일 (파일명 = Enum 값명)

- JSON(`Assets/_Lair/Data/Json/`): `HeroArchetype_Skirmish.json`(1 row), `Chapter.json`(row 3 추가), `Stage.json`(5 row 추가), `Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`(§5.2 row 추가), `HeroArchetype.json`(Assassin 추가), `hero_skills.json`(스킬 3종 + loadout 3 row 추가), `Strings_Ko.json`(1003 / 1013 / 1023 / 1121~1125 추가, 1206 문구 일반화).
- SO: `HeroSkillLoadout_Assassin.asset`, 스킬 SO `HeroSkill_ShadowStrike.asset` `HeroSkill_BladeWhirl.asset` `HeroSkill_SmokeBomb.asset`(`Art/Skills/`), `HeroStageVariantConfig.asset`(ChapterId=3 엔트리 5개: `Tier` 0~4, `UseEmission` false/false/true/true/true, `EmissionIntensity` 0/0/1.5/2.2/3.2, `ScaleMultiplier` 1/1/1/1/1.20).
- 프리팹: `Assassin.prefab`(`Art/Characters/`, Knight 프리팹과 동일 컴포넌트 구성, `_centerPullEnabled` off), `EVisual` 5종 FX 프리팹(`Art/FX/`).
- 스프라이트·배경: §6.
- 진행·랭킹: `StageProgress.MaxStage` 는 `Chapter.json` row 수 기반 런타임 값(chapter-system §2.1)이므로 row 3 추가로 15 가 된다. `StageLeaderboard.IsValidStage` = 1~15. `MetaProfile.Version` 은 의미 변경(ClearedStage 0~10 -> 0~15)이므로 5 -> 6 으로 올리고 마이그레이션 규칙은 chapter-system §2.2 와 같은 형태(Clamp 상한만 `MaxStage`). 클라우드 복원 후 마이그레이션 재적용도 동일(D-C3-10).
- 로드 검증: 챕터당 스테이지 5개, 복합키 중복 0, FK(`ChapterRow.heroId` -> `HeroArchetype`, `HeroArchetype_Skirmish.heroId` -> `HeroArchetype` 의 aiType=Skirmisher), 규칙 필수 키 집합 일치, 트리거 hpFraction 이 스킬 게이트/패시브 임계와 불일치.

### 7.4 코드 변경 요약

| 대상 | 변경 |
|---|---|
| `AutoCombatAI` | §3.3 Skirmisher 분기(기사·궁수 경로 불변) |
| `HeroSkillRunner` / 스킬 데이터 | `ShadowStrikeSkillData` `BladeWhirlSkillData` `SmokeBombSkillData` 신규 `$type` |
| `BattleController` | 챕터 3 영웅 프리팹 선택(`EHero.Assassin`), 로드아웃 영웅별 로드(기존 확장 경로) |
| `HeroStageVariantApplier` | 조회 키 (챕터, 인덱스) - 변경 없음, 엔트리만 추가 |
| `StageProgress` / `MetaProfileStore` / `StageLeaderboard` / `RankingPopup` / `RecordsPopup` | MaxStage 15, Version 6 마이그레이션, 스테이지 선택 컨트롤 1~15 확장 |
| `HeroSelectPopup` | 3번째 챕터 탭 (UI 목업 별도, §10 D-C3-11) |

### 7.5 스코프 분해 제안

단일 구현으로 묶기엔 크므로 승인 후 아래 순서로 분할 구현을 권장한다.

| 단위 | 내용 | 의존 |
|---|---|---|
| (a) 데이터/진행 | Chapter 3 row·Stage·규칙 row, `MaxStage` 15, Version 6 | 없음 |
| (b) 영웅 AI/스킬 | Skirmisher AI, 스킬 3종 | (a) |
| (c) 아트 | 시트 5장·초상·FX 5종·배경 | 시안 승인 |
| (d) UI | 월드맵 3번째 챕터 탭 | (a), 목업 승인 |

---

## 8. 테스트 / QA 검증 항목

### 8.1 test-engineer (EditMode)

| 영역 | 항목 |
|---|---|
| `StageProgress` | 선형 <-> (챕터, 인덱스) 경계(10<->11, 15), `IsUnlocked` 챕터 경계(cleared 10 -> 11 해금, 12 잠금), `ResolveClearedStage` 상한(MaxStage=15), `ScaleStat` 단일 반올림(Assassin 0.70 x 2.06, 소수 경계 0.5) |
| 마이그레이션 | v5(Cleared 0/5/10)->v6 값 보존·`Version == 6`, `StageRecords` 범위 밖 제거, 클라우드 복원 후 재적용 |
| JSON 검증 | `HeroArchetype_Skirmish` FK·aiType 일치, 챕터 3 스테이지 5개, 트리거 hpFraction 0.75 / 0.35 가 스킬 게이트/패시브 임계와 불일치 |
| 표적 점수 | `threatScore` 순위(Hex > Reaper > Phantom > Plague > Wisp > Wraith 기본 수치), HP 증가 카드 후 순위 하락, 공격력 증가 카드 후 순위 상승, 동점 시 가까운 몬스터, `retargetScoreMul` 히스테리시스, 봉인 종족 제외 |
| Skirmisher 상태 | 추적->교전->슬립 전이, `strikeWindowSec` 2.8 경과 슬립, 군중 3마리 슬립, 슬립 쿨다운 5.0, `minDwellSec` 진동 방지, 슬립 목적지 `arenaSoftRadius` 클램프, 공포(FleeMode) 우선, 기사·궁수 경로 회귀(동작 불변) |
| 스킬 | 그림자 습격 처치 시 쿨다운 환급 50%, 거리 < 2.5 면 미시전, 칼날 선풍 4틱·피해 배율 0.7, 연막 탄 몬스터 속도/사거리 배율·하한·지속 후 복구, 시전 직후 슬립 1회 |

### 8.2 PlayMode / UnityMCP

- 월드맵 플로우: 마을 -> 지도 -> 챕터 3 탭 -> 3-1 선택 -> 출격 -> 규칙 발동 -> 결과 -> 해금(3-2).
- 3-5 보스까지 실제 플레이: 보스 페이즈 75% / 35% 발동, 스킬 게이트 85/65/45%, 카드 일시정지와 비중첩.

### 8.3 qa-simulator 게이트 (리포트 `docs/qa-reports/`)

| 게이트 | 대상 | 측정 | 통과 기준 |
|---|---|---|---|
| **G5** | 암살자 AI | 교전 시간 비율, 우선 표적 처치 비중, 군중(2.0 안 3마리 이상) 노출 시간 비율, 전장 이탈, 기본 공격 실효 DPS | 교전 시간 비율 55~75%(생존 시간 대비), 처치된 몬스터 중 `threatScore` 상위 2종 비율 50% 이상, 군중 노출 비율 25% 이하, 전장 밖(`arenaSoftRadius` + 1) 프레임 0, 실효 DPS(스킬 제외) 가 같은 인덱스 스테이지 기사 DPS 의 70~100% |
| **G6** | Ch3 곡선 | 스테이지별 평균 처치 시간 | 각 `targetClearSec` +-15%, 톱니(3-1 기대 시간 < 2-5 기대 시간) 성립, 4축 빌드별 편차 +-30초 |
| **G7** | Ch3 보스 | 3-5 첫 도전 승률, 보스 구간 시간(보스 밴드 210~270초), 페이즈 발동 후 필드 평균 몬스터 수, 빌드별 승률 | 승률 40~50%, 필드 점유 평균 12마리 이상, **Tank 빌드 승률 >= Dps 빌드 승률**(가위바위보 의도). 어긋나면 §3.3 표적 규칙 수치만 조정 |
| 보조 | 성수 샘(3-3) | 샘 개방 중 영웅 체류율 | 0.3 +-0.15 (`ruleFactor` 가정 검증) |
| 보조 | 스킬 | 그림자 습격 처치율, 칼날 선풍 적중 수, 연막 후 슬립 성공률 | 습격 대상 처치율 70% 이상, 선풍 평균 적중 2마리 이상, 연막 시전 후 군중 이탈 성공 80% 이상 |
| 보조 | Fear 카드 | 암살자 스테이지 Fear 픽률/효과 | 기사 스테이지 대비 효과 저하가 확인되면 카드 재조정안은 별도 기획 |

---

## 9. CLAUDE.md §8 승격 문안 (사용자 승인 후 메인이 반영 - 이 기획서는 CLAUDE.md 를 수정하지 않는다)

CLAUDE.md §8 "챕터 구조 허용" 항목을 다음과 같이 개정하는 것을 제안한다.

> - **챕터 구조 허용 (2026-10-03 v0.3 승격, 2026-10-08 챕터 3 추가) - 단 1단계(Ch1 + Ch2)에 챕터 3 을 더한 범위에 한정** - (기존 ①~④ 유지) ⑤ 챕터 3 "그림자 성채": 영웅 암살자(신규 AI `Skirmisher` + 스킬 3종 + 5단계 외형), 기존 규칙 6종 재사용(신규 규칙 없음). **Ch4~Ch5, ★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 개별 승격 전까지 금지.** 별도 메인 메뉴/세팅 화면 금지는 유지.

또한 컨셉서 §11 범위 표에 "챕터 3(암살자) - v0.3 승격 2026-10-08" 한 줄을 추가한다.

---

## 10. 엣지 케이스

| # | 케이스 | 처리 |
|---|---|---|
| E-C3-1 | 필드에 후보 몬스터가 없음(전부 처치/봉인) | 후보 없음 -> 중앙으로 복귀하지 않고 제자리에서 대기. 몬스터 등장 즉시 추적 재개 |
| E-C3-2 | 우선 표적이 연막 지대·덫 안에서 느려져 영웅이 앞질러 가 버림 | 표적 유지 규칙(점수 1.5배 초과 시에만 교체)대로 추적 계속. 반대로 표적이 멀어지면 추적 시간 증가는 의도(둔화=Debuff 강점) |
| E-C3-3 | 교전 중 표적이 사망하고 군중 조건이 이미 참 | 슬립 우선(쿨다운 가용 시). 쿨다운 중이면 새 표적 추적 |
| E-C3-4 | 그림자 습격 순간이동 목적지가 전장 밖 | 목적지를 `arenaSoftRadius` 7.0 안으로 클램프 |
| E-C3-5 | `BossBulwark`(x0.6)와 `칼날 선풍`(x0.7) 동시 | 곱연산 0.42. 하한 없음, G7 에서 영웅 생존 시간 확인 |
| E-C3-6 | 연막 지대 `monsterMinRange` 0.8 이 영웅 충돌 거리보다 작음 | `DenseFog` 와 같이 충돌 거리 + 0.1 로 상향하고 보고(chapter-system E-7) |
| E-C3-7 | 3-4 `SealedNest`(Hex) 중 `threatScore` 1위 종이 필드에 없음 | 봉인 종족은 후보 부재로 자연 제외, 2위(Reaper) 이하로 표적 |
| E-C3-8 | 클라우드 복원 시 구버전(MaxStage 10) 클라이언트가 ClearedStage 11~15 문서를 복원 | Clamp 로 10 까지 보존되는 데이터 손실 가능. 챕터 3 출시 시 신버전 동시 배포(chapter-system §2.2 알려진 제약과 같은 성격) |
| E-C3-9 | 성수 샘(3-3) 위에서 슬립이 발동 | 샘 체류는 보장하지 않음(체류율 0.3 가정). 의도된 비효율 |
| E-C3-10 | `HunterTraps` 덫 안으로 슬립 목적지가 들어감 | 덫은 몬스터 대상(영웅 무해) |

---

## 11. 미결 사항 · 사용자 승인 항목

| # | 항목 | 추천안 | 상태 |
|---|---|---|---|
| **D-C3-1** | CLAUDE.md §8 챕터 3 승격 (문안 §9). 승인 전 구현 착수 금지 | 승격 | **승인 완료 (추천안, 2026-10-08)** |
| **D-C3-2** | 챕터 3 영웅 직업 (§1.2): A 암살자 / B 사제 / C 마법사 | **A. 암살자** | **승인 완료 (추천안, 2026-10-08)** |
| **D-C3-3** | 암살자 팔레트 (§3.5): A 색상 점프형 / B 인디고 단일 색조 | **A** (31° 규약 우선 시 B) | **승인 완료 (A안 색상 점프형, 2026-10-08)** |
| D-C3-4 | 신규 AI `Skirmisher` + 선택적 관계 스키마 `HeroArchetype_Skirmish` (§2·§3.3) - 데이터 구조표 승인 | 제안 구조 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-5 | 스킬 3종 신규 `$type` (그림자 습격 / 칼날 선풍 / 연막 탄, §4) | 제안안 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-6 | 규칙 배정 (§5.1: 신규 규칙 0종) + 1206 표시명 "사냥꾼의 덫" -> "덫" 일반화 | 채택 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-7 | 수치 초안 (스탯 §3.2, 스킬 §4, 스테이지 §5.3) - G0/G1 → G5/G6/G7 검증 후 확정 | 초안 채택, 검증 후 확정 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-8 | 공격 클립 이벤트: `OnAttackStrike` 0.200s + `OnAttackEnd` 0.580s 베이크(기사 fallback 1.8s 미적용), `slash01`/`slash02` = `stab` 행 복제 (§3.4) | 제안안 채택 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-9 | 5단계 `ScaleMultiplier` 1.20 (§3.5) | 채택 | **승인 완료 (추천안, 2026-10-08)** |
| D-C3-10 | `MaxStage` 15 · `MetaProfile.Version` 5 -> 6 · 랭킹 스테이지 1~15 (§7.3). 사용자 작업(Firebase 콘솔): Firestore 보안 규칙 `stage` 허용 범위를 1~15 로 확장(복합 인덱스 `(stage, clearTimeMs)` 는 그대로 유효) | 채택 | 사용자 작업 포함 |
| D-C3-11 | 월드맵 팝업 3번째 챕터 탭 UI 목업 (`chapter-worldmap.html` 확장) - 이번 범위 밖, 승격 후 별도 목업 승인 | 승격 후 별도 | 이월 |
| 이월 | 월드맵 상세 3-5(규칙 없음 + 페이즈 2건)가 기존 프리팹 고정 좌표에 들어가나 규칙/페이즈 사이 ~40px 빈틈, 페이즈 영역 여유 2px — 문구가 늘면 넘칠 수 있어 가변 높이 프리팹 개선은 후속(목업 6번 승인 항목) | 후속 | 이월 |
| D-C3-12 | 챕터 3 배경 `Battle_Backdrop_ShadowCitadel` 시안 - 팔레트 확정 후 영웅 대비 확인 | 승격 후 별도 | 이월 |

**수치 확정 방식**: 본 문서 모든 배수·`ruleFactor`·`M_ref`·AI·스킬 수치는 초안이다. 확정 순서는 G0(T1 확정, chapter-system §0.3) -> G1(Ch1) -> G3(Ch2) -> G5(암살자 AI) -> G6(Ch3 곡선) -> G7(Ch3 보스). 결정 메트릭은 §8.3 표, 재산출 규칙은 chapter-system §0.4 도출식.

---

## 12. Self-Review

- **Placeholder 잔존**: 미정 마커·애매한 권유·두 갈래 위임(디자인 결정 영역)·어림 표현 없음. 미확정 수치는 "초안 - G0/G1 검증 후 확정" 과 결정 메트릭(§8.3 G5/G6/G7)으로 명시. 구현 판단 영역(인터페이스 신설 여부)만 "gameplay-programmer 판단"으로 위임(코드 구조 지정 금지 원칙).
- **내부 일관성**: 스탯(0.70/0.70/1.6/0.7/4.2), 스킬 수치, 스테이지 표(hp 2.06/2.30/2.54/2.70/3.20, power 1.20/1.30/1.45/1.60/1.75, Scale 1.20), 규칙 파라미터가 §2·§3·§4·§5·§7·시안 `hero-ch3-2d.html` 에서 동일. 검산표 §5.3 에 산식 병기.
- **명명 일관성**: `Assassin` / `Skirmisher` / `HeroArchetype_Skirmish`(`HeroArchetype_SkirmishRow`) / `SkirmishSettings` / `HeroSkill_ShadowStrike` / `HeroSkill_BladeWhirl` / `HeroSkill_SmokeBomb`(`ShadowStrikeSkillData` / `BladeWhirlSkillData` / `SmokeBombSkillData`) / `Battle_Backdrop_ShadowCitadel` / `threatScore` 일관. 규칙·테이블 이름은 chapter-system 과 동일.
- **스코프**: 기획 + 시안까지만. §7.5 에 4분할 구현 제안. 구현·에셋 임포트·CLAUDE.md 수정은 하지 않았고 §8 승격 필요를 헤더·§9·D-C3-1 에 명시.
- **구현 요청사항 완전성**: Enum / Interface / 에셋 키 / JSON·SO 스키마 §2·§7 수록.
- **UI 목업**: 이번 기획은 UI 화면을 새로 만들지 않는다(월드맵 3번째 탭은 D-C3-11 로 이월). 영웅 외형 시안 `.mockups/hero-ch3-2d.html` 은 기사·궁수 시안과 같은 도트 톤을 따랐고 배경색만 임시.

**Self-Review: 통과** (시안 JS 는 실행 환경 부재로 브라우저 렌더링·`node --check` 를 이 에이전트가 수행하지 못함 - 메인의 문법 점검과 브라우저 확인 필요).
