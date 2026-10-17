# 챕터 4 "불꽃의 용광로" + 신규 영웅 광전사 (Berserker) - 기능 기획서

> 작성: game-designer · 날짜: 2026-10-17 · 단계: v0.3
> 입력: `CLAUDE.md` §8(챕터 4 승격 2026-10-17) · `chapter-system.md`(§0.4 도출식·상한, §1 데이터 구조, §3 규칙 6종, §4 곡선, §6 월드맵) · `chapter-3-hero.md`(선례 형식·§7 구현 요청·§8 테스트) · `hero-skills.md` · `project_lair_concept.md`(§5.2 4축 시너지, §8 밸런싱) · 실제 코드/데이터(`ChapterGate.cs` · `MetaProfileMigration.cs` · `ChapterDatabase.cs` · `BattleController.cs` · `Chapter.json` · `Stage.json` · `Stage_Rule*.json` · `HeroArchetype*.json` · `hero_skills.json` · `Strings_Ko.json`)
> **시안(합의용)**: `.mockups/scene-bg-ch4-ember.html`(배경, 3장 대비 신규) · `.mockups/hero-ch4-berserker.html`(영웅 5단계 시안 - 암살자 `hero-ch3-2d.html` 리그 재사용, 실루엣 R2'/R3' 실측 표 + 팔레트 색상각 실측). 메인이 `node .mockups/server.js` 로 `http://localhost:8777/` 제공.
> **데이터 구조표**: 본 문서 §2 (신규 스키마 2종 `HeroArchetype_Charge` · `HeroArchetype_Cleave` - 사용자 승인 게이트 대상)

---

## § 헤더

- **목표**: 챕터 4(5스테이지)와 챕터 영웅 **광전사**(가장 밀집한 몬스터 무리로 돌진해 범위 베기를 하는 근접 영웅, 신규 AI `Charger`, 스킬 3종, 5단계 성장 외형), 신규 맵 배경 "불꽃의 용광로"를 기획한다. 규칙은 기존 6종만 재사용한다(신규 규칙 0종).
- **검증 가설**: (1) 영웅이 "몬스터 밀집 지점"을 사냥하면, 암살자 챕터(고DPS 저HP 사냥)와 반대로 **Swarm(소형 다수) 빌드가 약해지고 Debuff·Tank·Dps 균형 빌드가 요구되는가**. (2) 기사(중앙 고정) -> 궁수(카이팅) -> 암살자(약자 사냥+이탈) -> 광전사(군중 돌진)로 4개 챕터 모두 "다른 판" 체감이 유지되는가. (3) 톱니형 곡선(Ch3 보스 -> Ch4 S1 하강)이 4번째 챕터에서도 진행 동기를 유지하는가.
- **현재 단계 범위 적합성**: **범위 안.** `CLAUDE.md` §8 ⑥ "챕터 4(4-1~4-5): 신규 영웅 직업·맵 배경·스테이지 데이터·규칙 배치(신규 규칙 필요 시 기획서에서 사용자 승인)"에 해당한다. 본 기획서는 신규 규칙 0종이라 별도 승인이 필요 없다. Ch5, ★ 목표, 악몽(Heat), 던전 편성·신규 몬스터 6종, 유물, 무한 탑·데일리는 계속 금지이며 본 문서에 포함하지 않는다. 서버 연동(Firebase 클라이언트 코드만) · 메인 메뉴/세팅 화면 금지는 변경 없음. 월드맵은 마을 HUD 안 팝업을 유지하며 **신규 UI 위젯은 없다**(4번째 챕터 탭은 `Chapter.json` row 4 로 구동되는 기존 탭 위젯 재사용).
- **핵심 메커니즘**: (a) 광전사는 `clusterScore`(후보 몬스터 `clusterRadius` 2.2 안 몬스터 수)가 최대인 지점으로 돌진해 정지하고, 기본 공격이 **범위 베기**(주변 반경 2.2 안 최대 4마리, 2번째 대상부터 피해 x0.5)다. 모인 몬스터를 쓸어내는 영웅이므로 소형 다수(Swarm)가 약해지고, 큰 몬스터(Tank)와 한 방이 센 몬스터(Dps)가 해답이 된다. (b) 스킬 3종(분쇄 도약 / 사슬 갈고리 / 피의 광란)은 기존 영웅 스킬 시스템(HP 85/65/45% 게이트)을 재사용한다. (c) 난이도는 eq 배수로 4-1 1.60 -> 4-5 2.55(상한 2.60 이내)이고, 규칙은 기존 6종 재배치(봉인 x2, 샘, 안개, 보스 페이즈 결계+화살비)다.

---

## 1. 테마 / 영웅 후보 3안과 추천

### 1.1 직업 선택 기준

현재 챕터: 1 지하 묘지(기사: 중앙에 서서 근접) / 2 어둠의 숲(궁수: 카이팅) / 3 그림자 성채(암살자: 약자 사냥 + 군중 이탈). 4장 후보는 아래 3가지를 모두 만족해야 한다.

1. **이동·표적 규칙이 앞 3종과 다르다** (같은 근접이라도 "무엇을 향해 움직이는가"가 달라야 한다).
2. **빌드 압박 축이 앞 챕터와 겹치지 않는다**: Ch2 = Tank 약, Ch3 = Dps 약 / Tank·Debuff 강. 4장은 Swarm 이나 Debuff 같이 아직 약점이 된 적 없는 축을 약하게 만드는 직업이어야 챕터마다 정답 빌드가 바뀐다.
3. 구현 단위가 3장(신규 AI 1 + 스킬 3 + 외형 5단계)을 넘지 않는다 (YAGNI).

### 1.2 후보 비교표

| 항목 | **A. 방패병 "철벽 수문장" (Warden)** | **B. 룬 마법사 (Runemage)** | **C. 광전사 (Berserker) - 추천** |
|---|---|---|---|
| 전투 역할 | 느리고 단단한 정면 버티기. 받는 피해를 고정치로 깎음 | 제자리 차징 후 광역 폭발(비이동 캐스터) | 가장 밀집한 무리로 돌진해 범위 베기 |
| 이동·표적 규칙 | 기사와 같은 중앙 고정 근접 | 중앙 후방 고정, 밀집 지점에 광역 시전 | 밀집도(`clusterScore`) 최대 지점으로 이동 후 정지 |
| 맵 테마 / 발광색 | 서리 요새 / 얼음 청백 `127,227,255` | 달무리 룬 신전 / 장미 마젠타 `255,110,200` | **불꽃의 용광로 / 엠버 주홍 `255,106,61`** |
| 빌드 압박(가설) | Swarm 약(고정 감쇄로 저피해 무력화), Dps·Debuff 강 | Swarm 약, Tank 강 | **Swarm 약, Tank·Dps·Debuff 균형** (Ch2 Tank 약 / Ch3 Dps 약과 모두 다름) |
| AI 재사용 | 기존 `Melee` 재사용 (신규 AI 0) | 신규 AI `Channeler` | 신규 AI `Charger` |
| 신규 스키마 | `HeroArchetype_Armor` 1종 + 받는 피해 고정 감쇄 메커니즘 | `HeroArchetype_Channel` 1종 | `HeroArchetype_Charge` + `HeroArchetype_Cleave` 2종 |
| 신규 스킬 | 3종 | 3종 | 3종 |
| 앞 영웅과 겹침 | **기사와 겹침이 가장 큼** (둘 다 중앙 고정 정면 근접, 차이는 수치뿐) | 3장 §1.2 기각 사유가 남음(아래) | 겹침 없음 (기사 고정 / 궁수 이탈 / 암살자 약자 사냥 / 광전사 군중 진입) |
| 구현 위험 | 낮음 (AI 재사용). 단 "고정 감쇄"가 지속 피해·보스 결계 곱연산과 얽힘 | 중간 (차징 텔레그래프) | 중간 (밀집도 질의 + 다중 대상 기본 공격. 밀집 무게중심 질의는 `TryGetFleeCentroid` 재사용) |

**B 의 3장 기각 사유 점검**: 3장 §1.2 는 마법사를 "궁수와 원거리 축이 겹침 + 광역기가 `ArrowRain`/`ArrowVolley` 와 필드 점유율이 겹침"으로 기각했다. B 는 궁수의 카이팅과 달리 **후퇴하지 않는 고정형**이라 이동 패턴은 다르지만, 원거리 광역이라는 전투 축과 광역기 필드 점유 충돌(보스 `ArrowRain` 재사용 시 필드가 비는 문제)은 **그대로 남아** 반박되지 않는다. 사제는 `HolySpring` 과 겹쳐 후보에서 제외한다. 소환사는 아군 유닛(소환물) 시스템이 새로 필요해 3장 규모를 넘으므로(YAGNI) 후보에서 제외한다.

**추천: C. 광전사.** 이유: (1) 이동 규칙이 앞 3종과 모두 다르고, 암살자(약자·고DPS를 쫓고 군중을 피함)와 정반대(군중을 찾아감)라 "같은 근접인데 다른 판"이 가장 선명하다. (2) 빌드 압박이 Swarm 약으로 Ch2·Ch3 과 겹치지 않는다. (3) 규칙 6종 재배치만으로 보스까지 닫힌다. A 는 구현이 가장 싸지만 기사와 역할이 겹치고, B 는 기각 사유가 해소되지 않는다. 사용자가 A/B 를 택하면 §3 이후(스탯·AI·스킬·외형·맵 색)를 해당 직업으로 교체해야 한다(결정 항목 D-C4-2). 이하 모든 상세는 C 기준이다.

### 1.3 4축 빌드 가위바위보 (가설, G8~G10 으로 검증)

광전사 기본 공격은 반경 2.2 안 최대 4마리를 벤다(첫 대상 100%, 나머지 50%). 스테이지 실효 공격력 39~56(§3.2)에 대한 몬스터 처치 타수(HP: Phantom 30 · Plague 50 · Hex 60 · Reaper 100 · Wisp 200 · Wraith 500, 스테이지 1 기준 39 피해)는 다음과 같다.

| 종 | HP | 첫 대상 타수(39) | 부차 대상 타수(19.5) |
|---|---|---|---|
| Phantom | 30 | 1 | 2 |
| Plague | 50 | 2 | 3 |
| Hex | 60 | 2 | 4 |
| Reaper | 100 | 3 | 6 |
| Wisp | 200 | 6 | 11 |
| Wraith | 500 | 13 | 26 |

검산: 30/39 -> 1, 50/39 = 1.28 -> 2, 60/39 = 1.54 -> 2, 100/39 = 2.56 -> 3, 200/39 = 5.13 -> 6, 500/39 = 12.8 -> 13. 부차 대상 피해 39 x 0.5 = 19.5: 30/19.5 = 1.54 -> 2, 50/19.5 = 2.56 -> 3, 60/19.5 = 3.08 -> 4, 100/19.5 = 5.13 -> 6, 200/19.5 = 10.3 -> 11, 500/19.5 = 25.6 -> 26.

| 축 | 광전사 상대 | 이유 |
|---|---|---|
| **Swarm** | **약** | Phantom·Plague 등 저HP 다수는 한 번의 베기에 연쇄 처치된다. 몰려 있을수록 `clusterScore` 가 올라 광전사가 정확히 그쪽으로 달려온다 |
| **Tank** | 강 | Wisp(6타)·Wraith(13타)는 베기를 버텨 영웅을 붙잡고, 영웅은 군중 한가운데 서 있어 접근 몬스터에 둘러싸이는 시간이 길다 |
| **Dps** | 강(중립 이상) | Reaper·Hex 의 단발 피해가 높아 영웅이 둘러싸여 있는 동안 HP 를 빠르게 깎는다. 단 Reaper 는 3타에 죽어 군집에서 빨리 사라짐 |
| **Debuff** | 강 | 둔화(Plague)로 이동속도 3.4 의 접근 주기를 늘리고, `HeroAttackDown`(영웅 공격력 x0.85)이 범위 베기 총량을 직접 줄인다. 출혈(이동 시 피해)은 돌진 이동이 많아 효율이 높다 |

Ch2 = Tank 약, Ch3 = Dps 약 / Tank 강, **Ch4 = Swarm 약** - 챕터마다 약점 축이 다르다. 어긋나면 §3 베기 수치(`cleaveMaxTargets`, `cleaveSecondaryMul`)만 조정한다.

---

## 2. 데이터 구조표 (사용자 승인 게이트 - 필수 확인)

> Rule 02 §11-1 적용. 챕터 4 의 스테이지·규칙·챕터 row 는 **기존 스키마에 값(row)만 추가**한다(스키마 변경 없음). 신규 스키마는 `HeroArchetype_Charge` · `HeroArchetype_Cleave` 2종뿐이다. 기존 `HeroArchetype.json` / `Chapter.json` / `Stage*.json` / `hero_skills.json` / `Strings_Ko.json` 은 필드 변경 없이 row 만 늘어난다.

### 2.1 엔티티 / 관계 트리 (변경분만)

```
Chapter (기존)         ChapterRow         <- row 4 추가
 +-- Stage (기존)      StageRow           <- (4,1)~(4,5) 5 row 추가
 |     +-- Stage_Rule / _Param / _Trigger (기존) <- 4-2~4-5 row 추가 (§5.2)
 +-- (Chapter.heroId) --> HeroArchetype (기존)  <- row "Berserker" 추가 (aiType = Charger)
                           +-- HeroArchetype_Kite (기존, Archer 만)
                           +-- HeroArchetype_Skirmish (기존, Assassin 만)
                           +-- HeroArchetype_Charge (신규, 선택적 관계)  HeroArchetype_ChargeRow
                           |        <- aiType=Charger 인 영웅만 존재 (Berserker 1 row)
                           +-- HeroArchetype_Cleave (신규, 선택적 관계)  HeroArchetype_CleaveRow
                           |        <- 기본 공격이 범위 베기인 영웅만 존재 (Berserker 1 row)
                           +-- (스킬 로드아웃) hero_skills.json loadout row (heroId="Berserker")
SO (Object 참조): HeroStageVariantConfig <- ChapterId=4 엔트리 5개 추가
```

**신규 스키마를 둘로 가른 이유 (§11-1 단계 3)**: "돌진 AI 튜닝"(`Charge`)과 "범위 베기 공격 설정"(`Cleave`)은 서로 독립 관계다. 베기는 AI 가 달라져도 유지될 수 있는 **공격 방식**이고, 돌진은 **이동 방식**이다. 둘 다 일부 영웅에만 존재하는 선택적 관계라 한 row 에 합치면 다른 영웅에서 sentinel(-1/0)이 생긴다. 두 관계 모두 필수 1:1 이 아니므로 `HeroArchetypeRow` 에 필드를 직접 얹지 않는다. 이름은 `HeroArchetype_<관계명>` 규칙(§11-1-5)을 따른다. 핵심 엔티티 한 문장: `HeroArchetype_ChargeRow` = "군중 돌진 AI 를 쓰는 영웅 직업 하나의 이동 튜닝 값", `HeroArchetype_CleaveRow` = "범위 베기 기본 공격을 쓰는 영웅 직업 하나의 베기 설정".

### 2.2 신규 row 필드 표

**HeroArchetype_ChargeRow** - `HeroArchetype_Charge.json` (EData `HeroArchetype_Charge`)

| 필드 | 타입 | 키/관계 | 설명 | Berserker 초안값 |
|---|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow.heroId` (aiType=Charger) | | "Berserker" |
| `huntRadius` | float | | 돌진 목표 후보 몬스터를 찾는 영웅 중심 반경 | 10.0 |
| `clusterRadius` | float | | `clusterScore` 집계 반경 (후보 몬스터 중심, 후보 자신 포함) | 2.2 |
| `retargetCountMargin` | int | | 현재 목표 군집 수보다 이 값 이상 큰 군집이 생겨야 목표 교체 (진동 방지) | 2 |
| `retargetIntervalSec` | float | | 목표 재평가 주기 | 1.0 |
| `minDwellSec` | float | | 상태 전이(이동 <-> 교전) 후 최소 유지 | 0.4 |
| `arenaSoftRadius` | float | | 이동 목적지를 이 반경 안으로 클램프 (Kite/Skirmish 과 같은 값이지만 테이블 간 의존을 피하려고 별도 보관) | 7.0 |

**HeroArchetype_CleaveRow** - `HeroArchetype_Cleave.json` (EData `HeroArchetype_Cleave`)

| 필드 | 타입 | 키/관계 | 설명 | Berserker 초안값 |
|---|---|---|---|---|
| `heroId` | string | PK -> `HeroArchetypeRow.heroId` | | "Berserker" |
| `cleaveRadius` | float | | 영웅 중심 기준 베기 판정 반경 | 2.2 |
| `cleaveMaxTargets` | int | | 한 번의 베기가 맞추는 최대 몬스터 수 (가까운 순) | 4 |
| `cleaveSecondaryMul` | float | | 2번째 대상부터의 피해 배율 (첫 대상은 100%) | 0.5 |

**HeroArchetypeRow 추가 row** (기존 스키마)

| heroId | nameKey | hpScale | powerScale | range | cooldown | moveSpeed | aiType |
|---|---|---|---|---|---|---|---|
| Berserker | 1024 (광전사) | 1.25 | 0.60 | 1.8 | 1.2 | 3.4 | Charger |

**ChapterRow 추가 row**

| chapterId | nameKey | themeTextKey | heroId | backdropKey |
|---|---|---|---|---|
| 4 | 1004 | 1014 | Berserker | Battle_Backdrop_EmberForge |

**StageRow 추가 5 row**: §5.3 표. **Stage_Rule / Param / Trigger 추가 row**: §5.2 표. **신규 스킬 `$type` 3종**: §4.

### 2.3 Strings_Ko id 배정 (현행 `Strings_Ko.json` 최대 id 1326, 규칙 키 규칙: 챕터명 100x · 테마 101x · 영웅명 102x · 스테이지명 11x1~11x5)

| id | 값 |
|---|---|
| 1004 | 불꽃의 용광로 |
| 1014 | 용병단의 난입 |
| 1024 | 광전사 |
| 1131 | 식어가는 화덕 |
| 1132 | 사슬 작업장 |
| 1133 | 연기 낀 제련소 |
| 1134 | 불타는 병기고 |
| 1135 | 용병대장의 용광로 |

신규 규칙이 없어 규칙명(1201~1206)·설명(1221~1226)·UI 문구(1301~1326)는 모두 기존 id 를 재사용한다. 보스 `ArrowRain` 의 표시명 1205 "화살비"는 2장과 공유된다(용병 궁수대의 지원 사격이라는 서사로 4-5 에서도 어색하지 않아 문구를 바꾸지 않는다). 스킬 표시명은 규칙명과 충돌하지 않게 "분쇄 도약 / 사슬 갈고리 / 피의 광란"으로 정했다(`hero_skills.json` `displayName`, `Strings_Ko` id 불필요).

---

## 3. 광전사 영웅 (Berserker)

### 3.1 직업 개요

- 콘셉트: 용병단의 해골 투사. 쇠사슬 투구 · 양날 도끼 · 두꺼운 어깨 철판. 몬스터가 가장 많이 몰려 있는 곳을 향해 걸어 들어가 제자리에서 도끼를 돌려 쓸어낸다.
- 기사처럼 중앙에 서지 않고(`_centerPullEnabled` off), 궁수처럼 물러서지 않으며, 암살자처럼 빠지지도 않는다. **돌진 - 교전(군집 안에서 정지 + 범위 베기) - 군집 소멸/재평가 시 다음 군집 이동** 사이클이다.

### 3.2 스탯 틀 (초안 - G0/G1 검증 후 확정)

| 항목 | 값 | 기사 / 궁수 / 암살자 | 근거 |
|---|---|---|---|
| hpScale / powerScale | 1.25 / 0.60 | 1.00/1.00, 0.80/0.80, 0.70/0.70 | 군중 한가운데 서므로 가장 두껍게(HP), 대신 첫 대상 피해는 가장 낮게(범위 베기 보상) |
| range / cooldown / moveSpeed | 1.8 / 1.2 / 3.4 | 1.5/1.0/3.0, 6.0/1.0/3.2, 1.6/0.7/4.2 | 무거운 도끼(사거리 +0.3, 느린 쿨다운), 이동은 기사보다 빠르고 암살자보다 느림 |
| aiType | Charger (신규) | Melee / Kiter / Skirmisher | §3.3 |
| 기본 공격 | 근접 즉발 범위 베기: 목표 거리 <= 1.8 에서 시전, 판정은 영웅 중심 반경 `cleaveRadius`(2.2) 안 가까운 순 최대 4마리, 첫 대상 100% · 나머지 x0.5 | | 공격 모션 총 0.6초, `OnAttackStrike` 0.200s(F3), `OnAttackEnd` 0.580s(F8) |

**스테이지별 실효 첫 대상 공격력** (앵커 Power 50 x 0.60 = 30, 1회 반올림): `powerMultiplier` 1.30 / 1.40 / 1.55 / 1.70 / 1.85 -> 39 / 42 / 47 / 51 / 56. 검산: 30 x 1.30 = 39.0, x 1.40 = 42.0, x 1.55 = 46.5 -> 47, x 1.70 = 51.0, x 1.85 = 55.5 -> 56. 단일 대상 DPS = 피해 / 1.2 = 32.5 / 35.0 / 39.2 / 42.5 / 46.7. 대상 수 n 일 때 총 피해 배율 = 1 + 0.5 x (n-1): n=1 1.0, n=2 1.5, n=3 2.0, n=4 2.5. 평균 3마리 가정(배율 2.0)의 실효 DPS = 65.0 / 70.0 / 78.4 / 85.0 / 93.3. 기사 같은 인덱스 스테이지(50 / 55 / 60 / 67.5 / 75)와 비교하면 130% / 127% / 131% / 126% / 124%. 군집이 부족할 때(n=1)는 기사의 65% / 64% / 65% / 63% / 62% 로 떨어지는 것이 의도된 약점이다. G8 통과 범위는 이 설계 중심값(124~131%)을 감싸도록 기사 DPS 의 115~145% 로 둔다(중심 130% +-15%p).

### 3.3 AI - Charger 사양

**상태**

| 상태 | 진입 | 행동 | 해제 |
|---|---|---|---|
| 탐색 | 전투 시작 / 필드에 후보 몬스터 없음 | 정지 대기 (후보가 없으면 제자리, 중앙으로 복귀하지 않음) | 후보 몬스터 등장 -> 이동 |
| 이동 | 목표 군집이 있고 목표 몬스터 거리 > `range`(1.8) | 목표 몬스터 방향으로 `moveSpeed`(3.4) 이동. 목적지는 `arenaSoftRadius` 안으로 클램프 | 거리 <= 1.8 -> 교전 |
| 교전 | 목표 몬스터 거리 <= 1.8 | 정지 + 범위 베기(쿨다운 1.2). 군집이 베기 반경 안에서 소멸하는 동안 같은 자리 유지 | 목표 사망/소멸 -> 목표 재선택. 재평가(1.0초 주기)에서 더 큰 군집(현재 목표 군집 수 + 2 이상)이 생기면 -> 이동 |

- **목표 선택**: `huntRadius`(10.0) 안 모든 몬스터 중 `clusterScore` = (그 몬스터 중심 `clusterRadius`(2.2) 안 몬스터 수, 자신 포함) 최대인 몬스터. 동점이면 영웅에게 가까운 몬스터. 봉인(`SealedNest`)된 종족은 필드에 없으므로 자연 제외. 후보가 1마리뿐이면 그 몬스터.
- **목표 유지**: 다른 군집 수가 현재 목표 군집 수 + `retargetCountMargin`(2) 이상일 때만 교체. 상태 전이 후 `minDwellSec`(0.4) 동안 재전이 금지.
- 슬립·후퇴 같은 이탈 행동 없음. 공포(`FleeMode`) 카드가 걸리면 기존 순서대로 FleeMode 가 우선한다.

**`AutoCombatAI` 충돌 점검 포인트 (gameplay-programmer 필독)**

| # | 기존 로직 | 충돌/주의 | 처리 방침 |
|---|---|---|---|
| 1 | Update 게이트 순서: 사망 -> TimeStop -> 스폰 게이트 -> `IsAttacking` -> 타깃 없음 -> 고정 타깃 -> FleeMode -> 중앙 끌림 -> 교전/이동 | Charger 분기는 암살자 Skirmisher·궁수 Kiter 와 같은 자리(FleeMode 뒤, 교전/이동 앞) | `aiType == Charger` 일 때만 활성(`ChargeSettings` 주입). 기사·궁수·암살자 경로는 동작 불변 |
| 2 | 고정 타깃 `_retargetMargin 1.5`(거리 기준) | 광전사 목표는 군집 수 기준 | 광전사는 군집 수 기준 히스테리시스(`retargetCountMargin`)로 대체, 거리 기준 고정 타깃은 쓰지 않음 |
| 3 | 단일 대상 기본 공격 판정 | 범위 베기가 신규 | `HeroArchetype_Cleave` row 가 있는 영웅만 범위 판정(반경·최대 대상·부차 배율). row 없는 영웅(기사·궁수·암살자)은 기존 단일 판정 그대로 |
| 4 | `TryGetFleeCentroid` / 밀집 질의 | 군집 집계에 재사용 가능 | 군집 수 집계 신규 질의의 후보. 판단은 gameplay-programmer |
| 5 | 스킬 이동(분쇄 도약) | AI 이동 명령과 충돌 | 스킬이 도약 지속시간(0.3초) 동안 AI 이동을 잠금(궁수 `회피 도약` / 암살자 `그림자 습격`과 같은 방식) |
| 6 | 스킬 `사슬 갈고리`로 몬스터가 당겨진 직후 | 군집 수가 갑자기 커져 재평가가 목표를 바꿀 수 있음 | 갈고리 시전 중·직후 0.6초 동안 재평가 중단 (`minDwellSec` 0.4 로 부족하므로 스킬 쪽에서 보장) |
| 7 | `HeroHologramMode`(마을 쇼케이스) | 광전사도 쇼케이스 영웅 | 마을에서는 전투·AI 컴포넌트 off(기사/궁수/암살자와 같은 경로) |
| 8 | 챕터 3 에서 확인된 `BattleController` 영웅별 분기 | 로드아웃 맵(`EHero` -> `EData`), `WarmVisual` 영웅별 FX 워밍 | §8 체크리스트 |

### 3.4 클립 사양 (시트 slash01 행)

- 대기(`idle`): 한 손으로 도끼를 세워 들어 날이 머리 오른쪽 위로 솟는다(자루 각 -72°).
- 공격(`slash01`): 9프레임 x 15fps = 0.600s. F0 양손으로 도끼 들기 -> F1~F2 어깨 뒤로 감기 -> **F3 앞으로 수평 휘두름 = strike**(0.200~0.267s 구간, 이벤트 0.200s: 0.200 x 15 = 3.0 -> F3) -> F4~F6 회전 궤적(연출만, 추가 데미지 없음) -> F7 한 손으로 내림 -> F8 = 대기 F0 와 같은 포즈. `OnAttackEnd` 0.580s(F8 구간 0.533~0.600s). 쿨다운 1.2 중 0.6초 여유, 피의 광란(공격 쿨다운 x0.6 = 0.72)에서도 0.6 <= 0.72 로 클립이 쿨다운을 넘지 않는다.
- 이동: `walk`(8프레임 10fps) / `run`(8프레임 14fps). 분쇄 도약·사슬 갈고리는 `run` 클립 + FX 로 표현하며 별도 클립은 만들지 않는다.
- 시트 규격은 기사·궁수·암살자와 같다: 셀 72px, 20열 x 9행, 행 순서 idle/walk/run/slash01/slash02/stab/hit/death/spawn, pivot (0.4236, 0.2917), PPU 31. `slash02`·`stab` 행은 `slash01` 행 프레임 복제(궁수 D-15 · 암살자 D-C3-8 와 같은 방식, 결정 항목 D-C4-6).

### 3.5 5단계 외형 (`HeroStageVariantConfig` ChapterId=4)

기사·궁수·암살자와 같은 규약(단계별 완성형을 독립 시트로 베이크, 2단계 림 `#F8FAFC`, 5단계 림 `#FDE047`, 발광 3~5단계, 5단계만 루트 스케일). 시트 파일: `Berserker_Sheet.png`(4-1) + `Berserker_Sheet_S2.png` ~ `Berserker_Sheet_S5.png` 5장과 각 `_Emission.png` 5장, `Berserker_SheetSpec.json` 1개. `tiers` = `{Berserker_Sheet:0, Berserker_Sheet_S2:1, Berserker_Sheet_S3:2, Berserker_Sheet_S4:3, Berserker_Sheet_S5:4}`.

| 스테이지 | 외형 | `Tier` | `UseEmission` | `EmissionIntensity` | `ScaleMultiplier` | A안 몸 색 / 림 / 발광 | B안 몸 색 / 림 / 발광 |
|---|---|---|---|---|---|---|---|
| 4-1 식어가는 화덕 | 맨 두개골 + 가죽으로 감은 상투 + 가죽 머리띠 · 가죽 어깨띠 · 외날 손도끼(자루 12.0 / 날 7.0) · 맨 뼈 팔 | 0 | false | 0 | 1.00 | `#A8A29E` 석회철(무채색) / 흑(#0A0D14) / 없음 | `#A1A1AA` 철 램프 1 / 흑 / 없음 |
| 4-2 사슬 작업장 | 철 투구(코가드) · 한쪽 어깨 철판 · 쇠사슬 허리띠 · 모피 망토 · 양날 도끼(자루 14.0 / 날 9.0) | 1 | false | 0 | 1.00 | `#4CC93B` 이끼 철록 / `#F8FAFC` / 없음 | `#8B8B94` 철 램프 2 / `#F8FAFC` / 없음 |
| 4-3 연기 낀 제련소 | 짧은 쌍뿔 투구 · 양 어깨 철판 · 사슬 가슴띠 · 모피 망토 · 발광 날 도끼(자루 15.0 / 날 10.0) | 2 | true | 1.5 | 1.00 | `#3E7BF5` 강청 / 흑 / `#60A5FA` | `#71717A` 철 램프 3 / 흑 / `#FB923C` |
| 4-4 불타는 병기고 | 긴 뿔 · 가시 견갑 · 대형 양날 도끼(자루 16.2 / 날 11.5) · 모피 망토 · 등 사슬 다발 · 발광 허리띠 | 3 | true | 2.2 | 1.00 | `#D4D4D8` 강철 백(무채색) / `#FDBA74` / `#FB923C` | `#52525B` 철 램프 4 / `#FDBA74` / `#F97316` |
| 4-5 용병대장의 용광로 | 3쌍 왕관 뿔 + 금 띠 · 거대 겹 견갑 · 초대형 양날 도끼(흑철 + 금, 자루 17.6 / 날 13.5, 금 날밑) · 사슬 망토 + 흑모피 · 바닥 용암 오라 | 4 | true | 3.2 | **1.35** | `#27272A` 흑철(몸 `#18181B`) / `#FDE047` / `#FF6A3D` | `#3F3F46` 흑철(몸 `#18181B`) / `#FDE047` / `#FF6A3D` |

- **스케일 1.35 근거**: 기사 보스 1.4(근접 콜라이더 기승인), 궁수 1.25, 암살자 1.20. 광전사는 군집 안에 들어가는 근접이라 몸이 커도 이동 경로 보호 손해가 없고 베기 판정은 영웅 중심 반경(2.2)이 쓰이므로 콜라이더 영향이 적다. 기사(1.4)보다 낮게 1.35 로 두고 G8 의 "군집 도달 시간"으로 확인한다.
- **시안 리그**: `hero-ch4-berserker.html` 은 암살자 `hero-ch3-2d.html` 리그를 그대로 재사용한다(셀 72, CS 4, 외곽선·발광 규약 동일). 도끼 수치는 자루 길이 L / 날 크기 bl (리그 단위)이며 위 표 값과 시안이 같다.
- **몸 높이**: 4-1 의 상투는 PPU 31 키 확보용이다(시안 실측 몸 높이 33도트).
- **실루엣**: 기사 `hero-2d-conversion.md` §3.3 의 R2'(돌출 >= 4/6/9/14 텍셀)·R3'(증가율 >= 10/18/30/45%)를 같은 값으로 적용한다. 단계별 실측값은 시안 3장 표에 있다. 시안 기준으로 4-2~4-4 R2' 가 미달로 보일 수 있으므로 **판정은 시트 베이크 후 최종 실측으로 한다**(미달 시 투구·뿔·망토 돌출 보완).
- **네 직업 구분**: 기사 = 대각 직선 검 + 낮은 캡, 궁수 = 세로 호 활 + 뾰족 후드 + 등 화살통, 암살자 = 쌍단검 + 복면 + 날리는 목도리, 광전사 = **한 손으로 세워 든 도끼(날이 머리 오른쪽 위로 솟음) + 투구·뿔 + 두꺼운 어깨 철판 + 모피/사슬 망토**. 몸통이 가장 두껍고 무기 실루엣이 머리 위로 가장 높다.

**팔레트 대안 (사용자 선택 필요, 결정 항목 D-C4-3)** - 시안 `hero-ch4-berserker.html` 의 색상각 표가 hex 에서 직접 계산한다.

기존 영웅이 이미 점유한 유채색 색상각(문서 기록값): 기사 0° · 142° · 199° · 271°, 궁수 36.4° · 37.7° · 82.7° · 328.6°, 암살자 171.2° · 238.7° · 300°. 이 11개 점 사이의 가장 큰 빈 구간이 82.7° -> 142°(폭 59.3°)이고, 그다음이 199° -> 238.7°(폭 39.7°)라 **25° 이상 이격되는 유채색 단계는 1~2개뿐**이다.

| 안 | 내용 | 기존 영웅과의 최소 색상각 | 장점 | 단점 |
|---|---|---|---|---|
| **A. 무채색-유채색 혼합 점프형 - 권장** | 석회철(무채) -> 이끼 철록 112.8° -> 강청 220.0° -> 강철 백(무채) + 오렌지 발광 -> 흑철 + 금 림 + 엠버 발광 | 112.8° = 29.2° (충족), 220.0° = 18.7° (**25° 미달, 인디고 238.7° 와 근접**), 발광: 4-3 `#60A5FA` 213.1° = 14.1° / 4-4 `#FB923C` 27.0° = 10.7° / 4-5 `#FF6A3D` 13.9° = 13.9° (발광은 면적이 작아 판정 완화) | 무채색 철 몸 + 유채색 포인트라 "금속 광전사" 정체성이 뚜렷하고 5단계 점프가 색만으로도 구분 | 4-3 강청이 암살자 인디고와 18.7°로 가까움. 보완: 4-3 은 몸 색을 `#3E7BF5` 대신 무채색 철로 두고 발광만 유채색으로 하는 변형을 미리 준비 |
| B. 무채색 철 램프 + 발광 단일 색조 | 몸은 명도 램프 5단계(`#A1A1AA` -> `#3F3F46`), 3~5단계 발광만 주홍 계열(27.0° / 24.6° / 13.9°) | 몸 색은 무채색이라 판정 제외, 발광 최소 10.7° (4-3 `#FB923C` 27.0°, 4-4 `#F97316` 24.6° = 13.1°, 4-5 `#FF6A3D` 13.9°) | 기사·궁수·암살자와 몸 색 혼동 가능성이 가장 낮음 | 인접 단계 색 차이가 없어 단계 구분을 실루엣·발광에 의존(궁수 B안 / 암살자 B안과 같은 약점) |

권장 이유: 색 + 실루엣 이중 신호(`hero-stage-variant.md` §1.3)를 유지하려면 색 신호가 있는 A 가 유리하다. 단 4-3 의 18.7° 가 눈에 띄면 A 의 4-3 무채색 변형(몸 철 + 발광만 유채색)을 쓰고, 그래도 부족하면 B 로 바꾼다. 4-5 발광 `#FF6A3D` 는 배경 발광색(엠버 `255,106,61`)과 같은 계열이라 **영웅이 배경에 묻히지 않는지**를 시안 `hero-ch4-berserker.html` 에서 확인한다.

---

## 4. 스킬 3종 (영웅 스킬 시스템 `hero_skills.json` 확장)

게이트: 기사·궁수·암살자와 같은 HP **85% / 65% / 45%**(패시브 임계와 0.05 오프셋 유지). `HeroSkillRunner`, 쿨다운 자동 시전, 컷인을 그대로 재사용한다. 모든 수치는 초안이며 G0/G1 검증 후 확정한다.

| 스킬 (`$type`, `fileName`) | 게이트 | 필드 / 값 | 효과 · 처치 타수 |
|---|---|---|---|
| **분쇄 도약** `CrushingLeapSkillData`, `HeroSkill_CrushingLeap` ("분쇄 도약") | 85% | `damage` 90 / `cooldown` 7.0 / `leapRange` 8.0 / `minLeapDistance` 3.0 / `leapDurationSec` 0.3 / `invulnerableSec` 0.3 / `slamRadius` 2.6 / `knockbackStrength` 1.5 | `clusterScore` 최대 군집(`clusterRadius` 2.2 기준)의 무게중심이 `minLeapDistance` 보다 멀고 `leapRange` 안이면 그 지점으로 0.3초 도약 후 반경 2.6 에 90 피해 + 넉백 1.5. 처치 타수: Phantom·Plague·Hex 1타, Reaper 2타, Wisp 3타, Wraith 6타. 도약 중 무적 0.3초 |
| **사슬 갈고리** `ChainPullSkillData`, `HeroSkill_ChainPull` ("사슬 갈고리") | 65% | `damage` 40 / `cooldown` 10.0 / `pullRadius` 5.0 / `maxTargets` 6 / `pullToDistance` 1.5 / `pullDurationSec` 0.4 / `triggerMonsterCount` 4 / `triggerRadius` 5.0 | 반경 5.0 안에 몬스터 4마리 이상이면, 가까운 순 최대 6마리를 영웅 1.5 거리까지 0.4초에 당기며 각 40 피해. Phantom 1타 처치. 당겨진 몬스터가 베기 반경(2.2) 안으로 모여 다음 베기가 최대 효율이 된다 |
| **피의 광란** `BloodFrenzySkillData`, `HeroSkill_BloodFrenzy` ("피의 광란") | 45% | `durationSec` 6.0 / `attackCooldownMul` 0.6 / `damageTakenMul` 1.2 / `cooldown` 18.0 / `triggerMonsterCount` 3 / `triggerRadius` 2.2 | 반경 2.2 안에 몬스터 3마리 이상이면 6.0초 동안 영웅 공격 쿨다운 x0.6(1.2 -> 0.72) · 받는 피해 x1.2. 영웅 DPS 가 1/0.6 = 1.67배가 되는 대신 더 아프게 맞는 도박 |

- `loadout` row: `{heroId: "Berserker", hpFraction: 0.85, skill: "HeroSkill_CrushingLeap"}`, `{.., 0.65, "HeroSkill_ChainPull"}`, `{.., 0.45, "HeroSkill_BloodFrenzy"}`.
- 자동 시전 우선순위(여러 조건이 동시에 참일 때): 사슬 갈고리 > 피의 광란 > 분쇄 도약. 근거: 모으기(갈고리)가 베기 효율의 전제이고, 모인 뒤 광란이 이득이며, 도약은 군집이 먼 곳에 있을 때의 이동기다.
- 이론 상한 보조 DPS(밀집 가정, 참고): 분쇄 도약 90 x 3마리 / 7.0 = 38.6, 사슬 갈고리 40 x 4마리 / 10.0 = 16.0, 합 54.6. 피의 광란은 별도 피해가 없고 기본 DPS 를 1.67배로 올린다(스테이지 1 평균 3마리 가정 65.0 -> 108.3). 광란은 6.0초 / 18.0초 = 33% 가동률이라 평균 증가분은 기본 DPS x0.67 x 0.33 = x0.22 이다(4-3 기준 78.4 -> 95.7, 기사 같은 인덱스 60 대비 160%). 실효 값은 G8 에서 확인한다.
- 받는 피해 곱연산: 광란 x1.2 와 `BossBulwark` x0.6 이 겹치면 0.72(하한 없음, 엣지 E-C4-4).
- 스킬 컷인(시간정지 1.9초 x 3회)은 기존과 같은 방식으로 재사용한다.
- 신규 효과 로직은 기존 컨텍스트 재사용이 원칙이다: 구형 범위 피해·넉백(`IHeroSkillContext`), 영웅 받는 피해 배율(`BossBulwark`/`BladeWhirl`), 도약 이동(궁수 `Evade`). 신규는 "몬스터를 영웅 쪽으로 당기는 강제 이동"(`ChainPull`)과 "영웅 공격 쿨다운 배율"(`BloodFrenzy`)뿐이다.

---

## 5. 챕터 4 스테이지와 난이도 곡선

### 5.1 규칙 배정 (신규 규칙 0종, 기존 6종 재사용)

콘셉트: 용병단이 용광로 요새 곳곳에서 장치를 가동한다. 광전사 특성상 **군집 형성에 영향을 주는 규칙**(봉인: 군집 재료 차단, 샘: 군중 안 체류를 흔듦)과 **보스 전의 결계·지원 사격**으로 구성한다. 앞 챕터에서 아직 봉인되지 않은 종(Wisp, Reaper)을 봉인 대상으로 쓴다.

| 스테이지 | 규칙 | 의도 |
|---|---|---|
| 4-1 식어가는 화덕 | 없음 | 톱니 골. 광전사 AI(군집 돌진 / 범위 베기) 학습 |
| 4-2 사슬 작업장 | `SealedNest` (Wisp 봉인, 해제 0.60) | 군집의 주재료인 Wisp(Tank 군)가 막혀 군집이 얇다 - 해제 시 Wisp 가 한꺼번에 합류해 군집이 두꺼워진다 |
| 4-3 연기 낀 제련소 | `HolySpring` | 영웅이 군집을 따라 샘 밖에 머물러 회복 효율이 낮다(체류율 0.25 가정). 샘 위치 근처에 군집을 유도하는 선택지 |
| 4-4 불타는 병기고 | `SealedNest` (Reaper 봉인, 해제 0.70) + `DenseFog` | 주력 딜러(Reaper)가 70% 까지 없다 - Dps 해답이 늦게 열린다. 안개로 Hex 사거리 5.0 -> 3.5 |
| 4-5 용병대장의 용광로 | `BossBulwark` (0.55) + `ArrowRain` (0.35) | 보스 영웅의 결계(받는 피해 x0.6, 15초)로 광란 도박의 가치가 오르고, 후반 용병 궁수대의 화살비가 군집 한가운데(광전사가 선 곳)에 떨어져 몬스터도 피해를 입는다 |

**보스 페이즈 대안 비교**

| 안 | 페이즈 구성 | 평가 |
|---|---|---|
| **A. 결계 0.55 + 화살비 0.35 - 권장** | 2페이즈, 3장 보스(0.75 결계 + 0.35 덫)와 구성·순서가 다름 | 화살비가 군집 위치(광전사가 선 곳)를 노려 Swarm 약 압박이 후반에 강해지고, 결계가 중반 도박 구간을 만든다. 규칙 2개로 Ch3 와 같은 복잡도 |
| B. 화살비 0.75 + 결계 0.55 + 덫 0.35 | 3페이즈 | 첫 보스 3페이즈로 에스컬레이션 가능하나 6종 중 3종이 겹쳐 필드 점유 12마리 미만 위험이 높고 ruleFactor 추정치가 더 커진다. 검증 부담 증가 |
| C. 결계 0.75 + 덫 0.35 (3장 복제) | 2페이즈 | 구현 부담 최소이나 4장의 개성이 사라진다 |

### 5.2 규칙 row (`Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`)

| chapterId | stageIndex | ruleIndex | ruleId | 트리거 hpFraction | 파라미터 |
|---|---|---|---|---|---|
| 4 | 2 | 1 | SealedNest | (row 없음 = 상시) | `lockedSpecies` 0 (Wisp), `unlockHpFraction` 0.60 |
| 4 | 3 | 1 | HolySpring | (없음) | `centerX` 0.0, `centerZ` 0.0, `radius` 3.0, `healPercentPerSec` 0.4, `activeSec` 8, `periodSec` 40, `firstActiveSec` 20 |
| 4 | 4 | 1 | SealedNest | (없음) | `lockedSpecies` 2 (Reaper), `unlockHpFraction` 0.70 |
| 4 | 4 | 2 | DenseFog | (없음) | `monsterRangeMul` 0.7, `minRange` 0.8 |
| 4 | 5 | 1 | BossBulwark | 0.55 | `damageTakenMul` 0.6, `durationSec` 15 |
| 4 | 5 | 2 | ArrowRain | 0.35 | `intervalSec` 10, `searchRadius` 8.0, `radius` 3.5, `tickCount` 4, `tickIntervalSec` 0.5, `damagePerTick` 50 |

- 파라미터 값은 기존 규칙의 승인값(chapter-system §3.4 · chapter-3-hero §5.2)을 그대로 쓴다. 신규 파라미터 키 없음.
- `EMonster` 정수값: Wisp 0 · Wraith 1 · Reaper 2 · Hex 3 · Plague 4 · Phantom 5.
- HP 임계 중복 방지(chapter-system §3.2): 보스 페이즈 0.55 / 0.35 는 패시브(0.9~0.1)·스킬(0.85/0.65/0.45)과 0.05 이상 이격한다. 봉인 해제 0.60 / 0.70 은 패시브 임계와 일치하므로 해제 효과(스폰 버스트)는 카드 팝업이 닫힌 뒤 처리(chapter-system E-4).
- 4-5 의 `BossBulwark`(x0.6)와 `피의 광란`(x1.2)이 겹치면 곱연산 0.72, 이후 `ArrowRain` 은 영웅에게 피해가 없다(몬스터 대상 규칙).

### 5.3 스테이지 표 (`Stage.json`, T1 = 130초 기준 도출, 모두 G0/G1 검증 후 확정)

도출식(chapter-system §0.4): `hpMultiplier = round2(targetClearSec x M_ref / T1 / (hpScale x ruleFactor))`, `eq 배수 = targetClearSec x M_ref / T1`. `hpScale` = 1.25. `M_ref` Ch4 = 1.26 / 1.27 / 1.28 / 1.29 / 1.30 (Ch3 보스 1.25 에서 스테이지당 +0.01).

> **M_ref 증가폭을 +0.02 에서 +0.01 로 낮춘 이유**: Ch3 와 같은 +0.02 로 이어가면 4-5 가 1.35 가 되어 255 x 1.35 / 130 = 2.65 로 chapter-system §0.4 의 Ch4 참고 상한 2.60 을 초과한다. +0.01 이면 4-5 M_ref 1.30 이 상점 최대 효과(0.35)의 0.30 / 0.35 = 86% 소진이며 255 x 1.30 / 130 = 2.55 로 상한 이내다. Ch5 몫으로 14%(0.05)가 남는다.
>
> **raw `hpMultiplier` 가 3장 마지막(3.20)보다 낮아 보이는 이유**: `hpScale` 이 암살자 0.70 에서 광전사 1.25 로 올라 분모가 커졌기 때문이다. 이어지는 난이도는 raw 배수가 아니라 **eq 배수**(3-5 2.45 -> 4-1 1.60 -> 4-5 2.55)로 판단한다. 절대 HP = `A x 1.25 x hpMultiplier` (`A` = G0 확정 HP 앵커).

| 스테이지 | 스테이지명(id) | 규칙 | targetClearSec | M_ref | eq 배수 | ruleFactor | **hpMultiplier** | **powerMultiplier** | `ScaleMultiplier` |
|---|---|---|---|---|---|---|---|---|---|
| 4-1 | 식어가는 화덕 (1131) | 없음 | 165 | 1.26 | 1.60 | 1.000 | **1.28** | **1.30** | 1.00 |
| 4-2 | 사슬 작업장 (1132) | SealedNest(Wisp) | 185 | 1.27 | 1.81 | 1.073 | **1.35** | **1.40** | 1.00 |
| 4-3 | 연기 낀 제련소 (1133) | HolySpring | 205 | 1.28 | 2.02 | 1.040 | **1.55** | **1.55** | 1.00 |
| 4-4 | 불타는 병기고 (1134) | SealedNest(Reaper) + DenseFog | 225 | 1.29 | 2.23 | 1.101 | **1.62** | **1.70** | 1.00 |
| 4-5 | 용병대장의 용광로 (1135) | BossBulwark + ArrowRain | 255 | 1.30 | 2.55 | 1.093 | **1.87** | **1.85** | 1.35 |

**`ruleFactor` 산식** (dps-share 비중은 chapter-system §4.1: Wisp 17.0% · Wraith 15.3% · Reaper 30.6% · Hex 18.3% · Plague 6.1% · Phantom 12.7%)

| 스테이지 | 산식 | 값 |
|---|---|---|
| 4-2 | Wisp 비중 17.0% x 봉인 시간 비율(1 - 0.60 = 40%) = 6.8% 압력 감소 -> 1 / (1 - 0.068) = 1 / 0.932 | 1.073 |
| 4-3 | 개방 횟수 floor((205 - 20) / 40) + 1 = 5회. 5 x (0.4% x 8s = 3.2%) x 영웅 체류율 0.25(군집을 따라 샘 밖에 머묾) = 4.0% -> 1 + 0.040 | 1.040 |
| 4-4 | Reaper 비중 30.6% x (1 - 0.70 = 30%) = 9.18% -> 1 / (1 - 0.0918) = 1 / 0.9082 = 1.1011. 안개는 미보정(Hex 위협 감소는 영웅 측 이득이나 정량 모델 없음) | 1.101 |
| 4-5 | BossBulwark: 15s x 40% 경감 x 후반 피격 가중 1.3 / 목표 255s = 7.8 / 255 = 3.06% -> 1.031. ArrowRain 추정 6% -> 1.06. 곱 1.031 x 1.06 = 1.0929 | 1.093 |

> 4-5 의 ruleFactor 1.093 은 **추정치**다(정량 모델 없음, 3-5 와 같은 값). qa-simulator 게이트 G9/G10 실측 후 도출식으로 재산출한다.

**검산** (T1 = 130, round2 직전 비반올림 값)

| 스테이지 | eq 비반올림 | 분모 hpScale x ruleFactor | hpMultiplier 비반올림 | 역검산(1.25 x hpMult x ruleFactor) |
|---|---|---|---|---|
| 4-1 | 165 x 1.26 / 130 = 207.9 / 130 = 1.5992 | 1.25 x 1.000 = 1.2500 | 1.2794 -> 1.28 | 1.25 x 1.28 x 1.000 = 1.600 |
| 4-2 | 185 x 1.27 / 130 = 234.95 / 130 = 1.8073 | 1.25 x 1.073 = 1.3413 | 1.3475 -> 1.35 | 1.25 x 1.35 x 1.073 = 1.811 |
| 4-3 | 205 x 1.28 / 130 = 262.4 / 130 = 2.0185 | 1.25 x 1.040 = 1.3000 | 1.5527 -> 1.55 | 1.25 x 1.55 x 1.040 = 2.015 |
| 4-4 | 225 x 1.29 / 130 = 290.25 / 130 = 2.2327 | 1.25 x 1.101 = 1.3763 | 1.6223 -> 1.62 | 1.25 x 1.62 x 1.101 = 2.230 |
| 4-5 | 255 x 1.30 / 130 = 331.5 / 130 = 2.5500 | 1.25 x 1.093 = 1.3663 | 1.8664 -> 1.87 | 1.25 x 1.87 x 1.093 = 2.555 |

표의 eq 배수 열은 위 비반올림 값의 round2(1.60 / 1.81 / 2.02 / 2.23 / 2.55)다. 절대 공격력 = 앵커 Power 50 x 0.60 x `powerMultiplier`(§3.2).

`powerMultiplier` 는 hpMultiplier 와 별개로 톱니를 따른다: 2-5 1.60 -> 3-1 1.20 -> 3-5 1.75 -> 4-1 **1.30**(챕터 S1: 1.00 / 1.10 / 1.20 / 1.30), 챕터 내 증가폭 +0.10 / +0.15 / +0.15 / +0.15(보스 +0.15).

### 5.4 톱니형 곡선과 상한 점검

| 구간 | eq 배수 | 비고 |
|---|---|---|
| Ch3 S5 -> Ch4 S1 | 2.45 -> **1.60** | **-34.7% 하강** (톱니 골). (2.45 - 1.60) / 2.45 = 0.347. Ch1->Ch2 / Ch2->Ch3 의 -37% 보다 약간 얕음(이유: 챕터 S1 끼리 1.00 -> 1.25 -> 1.44 -> 1.60 으로 골의 높이가 올라가는 방향 유지) |
| Ch4 S1 -> S5 | 1.60 -> 1.81 -> 2.02 -> 2.23 -> 2.55 | 단조 증가 (+0.21 / +0.21 / +0.21 / +0.32: 보스 도약) |
| 챕터 S1 끼리 | 1.00 -> 1.25 -> 1.44 -> 1.60 | 챕터를 거듭할수록 골의 높이가 상승 (+0.25 / +0.19 / +0.16) |

- 상한 점검: 최대 eq 2.55 <= chapter-system §0.4 의 Ch4 참고 상한 2.60, 절대 상한 `C(130)` 2.80 이내. 분자 `hpScale x hpMultiplier` 최대 = 1.25 x 1.87 = 2.3375 로 3.5 미만.
- **난이도 방향**: 4-1 은 Ch3 3~4번째 스테이지 수준에서 재시작해 광전사 AI(군집 돌진)와 범위 베기를 학습, 4-2~4-4 는 봉인 -> 샘 -> 봉인 + 안개로 군집 재료가 바뀌는 변주, 4-5 는 보스 페이즈 2종으로 마감. 모든 수치는 **G0 -> G1 -> G8 -> G9 -> G10 순 검증 후 확정**하며 어긋나면 위 도출식으로 `Stage.json` 을 일괄 재산출한다(규칙 구조·파라미터 불변).

---

## 6. 맵 배경 - "불꽃의 용광로"

> 시안: `.mockups/scene-bg-ch4-ember.html` (3장 그림자 성채와 나란히 비교, 4장은 도트 단위 신규 구현). 캔버스 규격(800x352)과 팔레트 값(`TH[4]`)은 `scene-bg-battle-chapters.html` 구조를 따른다.

### 6.1 에셋 규격과 키

- 파일 `Battle_Backdrop_EmberForge.png`, **1600x704** (시안 캔버스 800x352 의 도트 2배 확대), PPU 48, Point 필터 · 무압축 · 밉맵 off (`Backdrop2D_SheetSpec.json` 에 엔트리 추가: `folder: Backdrop2D`, `size: [1600, 704]`, `pivot: [0.5, 0.5]`, `ppu: 48`, `content: [0, 0, 1600, 704]`).
- `EBackdrop.Battle_Backdrop_EmberForge` = `ChapterRow.backdropKey` 값명 = Addressables 주소 = 파일명.
- 기존 UI풍 공통 요소 유지: 상단 **금 코니스**(`#b9832a` / `#f7c64a` 2선), 투기장 림의 **금 인레이 라인 + 12개 금 리벳**, 바깥 **룬 점선 회전**(중앙은 4장 고유의 용광로 구덩이, §6.3), 가장자리 **비네트**(`rgba(2,4,10,.62)`), 6개 스포너 슬롯은 기존 스프라이트 그대로(배경에 제단 본체 없음), HUD 가독성을 위해 가장자리·상단은 어둡게.

### 6.2 장별 비교와 4장 팔레트

| 항목 | 1장 던전 투기장 | 2장 어둠의 숲 | 3장 그림자 성채 | **4장 불꽃의 용광로 (신규)** |
|---|---|---|---|---|
| 발광색 `GL` (rgb) | 소울 그린 `94,240,180` | 라임 `170,255,100` | 바이올렛 `181,140,255` | **엠버 주홍 `255,106,61`** |
| 보조 발광 `GL2` | 금 `247,198,74` | 소울 그린 | 금 | 금 `247,198,74` (UI 금 유지) |
| 석재 A / B | `#171d2a` / `#222a3b` | `#1c1a2c` / `#2a2a3a` | `#1b1a30` / `#2a284a` | `#241a1c` / `#372729` |
| 림 하이 / 로우 | `#48546e` / `#313b52` | `#5a6e58` / `#3a4a3e` | `#5c5a88` / `#3c3a62` | `#7a5a52` / `#4d3a3a` |
| 배경색 / 측면 | `#060911` / `#171d2a` | `#070612` / `#1a1a26` | `#07060f` / `#18162a` | `#0b0606` / `#201516` |
| 바닥 중심 반사광 `LIFT` | `[40,96,92]` | `[70,120,60]` | `[90,70,150]` | `[130,58,28]` |

- 발광색 선택 이유: 소울 그린(1장)·라임(2장)·바이올렛(3장) 어느 것과도 겹치지 않는 **따뜻한 주홍**이며(색상각 14°), UI 의 금(43°)과 구분된다. 영웅 대비: 광전사 A안 4-2 철록(112.8°)·4-3 강청(220.0°)은 주홍 배경과 보색에 가까워 잘 보이고, 4-4 강철 백은 밝아서 보이며, 4-5 흑철 + 금 림은 어두운 배경에서 림(금)이 윤곽을 만든다. 4-5 발광(`#FF6A3D`)만 배경 발광과 같은 계열이라 시안에서 확인한다.

### 6.3 4장 연출 설계

| 층 | 정적 베이스 | 동적(애니메이션) |
|---|---|---|
| 상단 벽 | 리벳 철판 + 구리 파이프 + 금 코니스. **아치형 화구 4개**(쇠창살) | 쇠창살 너머 불길이 일렁임. 4.0~8.0초마다 화구 한 곳이 분출하며 불똥을 뿜음 |
| 양옆 | 철 프레임 안 **용암 폭포 2줄** | 용암이 흘러내림. 오른쪽은 시너지 패널 뒤라 밝기 0.6 |
| 투기장 바닥 | 동심원 바닥 + 금 인레이 + 도트 **용암 균열 7줄 + 가지** | 중심에서 바깥으로 빛이 흐르며 균열이 맥동 |
| 중앙 | 룬 링 대신 **쇠창살 용광로 구덩이** | 구덩이 안 쇳물이 끓음. 바깥 룬 점선은 회전 유지 |
| 림 | 금 리벳 12개 + **철 환기구 8개** | 환기구에서 불똥이 튐 |
| 주변 | 재·슬래그 고리, 아래쪽 모루·주괴·쇠사슬 실루엣 | 고리 위 불씨가 깜빡임 |
| 입자 | - | **불똥**이 노랑 -> 주황 -> 적색으로 식으며 상승 |
| 연기·열기 | - | 천장 검은 연기, **열 아지랑이**(중앙 영역만) |
| 공통 | - | 비네트 |

- 성능: 동적 도트를 ImageData 버퍼 1장에 써서 그린다. 시안 측정 기준 프레임당 10ms 수준이다.
- 시안과 PNG 의 관계: 시안은 합의용이다. 승인 후 정적 베이스를 시안의 `window.__ch4Static()` 으로 추출해 PNG 로 만든다. 동적 연출(불씨·글로우·분출)은 Unity 측 기존 장별 연출 경로와 같은 방식으로 구현한다(방식은 gameplay-programmer 판단).

---

## 7. 에셋 요구 / 구현 요청사항 (gameplay-programmer 용 - 승인 후)

### 7.1 에셋 요구 (Claude 제작)

| 에셋 | 사양 |
|---|---|
| `Berserker_Sheet.png` + `Berserker_Sheet_S2~S5.png` (+ 각 `_Emission.png`) + `Berserker_SheetSpec.json` | `Knight_SheetSpec.json`/`Archer_SheetSpec.json`/`Assassin_SheetSpec.json` 동일 규격(§3.4). 시안 `hero-ch4-berserker.html` 은 암살자 리그를 재사용한 부위 리그 시안이며, 시트 베이크·최종 실루엣 실측은 승인 후 별도 작업이다 |
| `HeroIcons/Berserker.png`, `Berserker_S2~S5.png` | 스테이지 초상 5장 (기존 영웅 초상 규격 동일) |
| FX (프리팹 = `EVisual` 값명) | `BerserkerSlashFx`(범위 베기 원호) `HeroCrushingLeapFx` `HeroChainPullFx` `HeroBloodFrenzyFx` (규칙 FX 는 기존 `StageSpringFx` `StageFogFx` `StageSealChainFx` `StageBulwarkFx` `StageArrowRainFx` 재사용) |
| 배경 `Battle_Backdrop_EmberForge.png` | §6. `Battle_Backdrop.png` 해상도·레이어 규격 동일, 정적 베이스는 시안 `window.__ch4Static()` 으로 추출 |
| 사운드 | 이번 범위 제외(후속) |

### 7.2 Enum 값 (`CommonEnum.cs`, 카테고리별 분리 - Rule 02 §8, Rule 03 §2)

| Enum | 추가 값 | 비고 |
|---|---|---|
| `EHero` | `Berserker` | `Assassin` 뒤에 추가(기존 직렬화 정합) |
| `EHeroAi` | `Charger` | `Melee`, `Kiter`, `Skirmisher` 뒤 |
| `EData` | `HeroArchetype_Charge`, `HeroArchetype_Cleave`, `HeroSkillLoadout_Berserker` | JSON/SO 파일명 = 값명 |
| `EBackdrop` | `Battle_Backdrop_EmberForge` | `ChapterRow.backdropKey` 값명 |
| `EVisual` | `BerserkerSlashFx`, `HeroCrushingLeapFx`, `HeroChainPullFx`, `HeroBloodFrenzyFx` | 기존 값 뒤에 추가(int 직렬화 정합), 프리팹 `CHMPool` 대상 |

`EStageRule`·`EUI` 신규 값 없음.

### 7.3 Interface (`CommonInterface*.cs`, Rule 02 §9)

- 영웅 AI 설정 주입: 암살자 `SkirmishSettings` 와 같은 방식으로 `ChargeSettings`(`HeroArchetype_ChargeRow` 로부터 주입) 와 `CleaveSettings`(`HeroArchetype_CleaveRow`). 인터페이스 신설 여부는 gameplay-programmer 판단.
- 군집 수 질의(`clusterScore` 계산에 몬스터 위치 접근): 기존 몬스터 컨텍스트에 읽기 전용 질의로 추가하는지는 gameplay-programmer 판단. 카드로 변동된 현재 필드 몬스터 위치를 반영해야 한다는 요구만 고정한다.
- 신규 스킬 효과 컨텍스트: 몬스터 강제 이동(`ChainPull`), 영웅 공격 쿨다운 배율(`BloodFrenzy`). 기존 `IHeroSkillContext` 확장 여부는 gameplay-programmer 판단.

### 7.4 에셋 키 / 데이터 파일 (파일명 = Enum 값명)

- JSON(`Assets/_Lair/Data/Json/`): `HeroArchetype_Charge.json`(1 row), `HeroArchetype_Cleave.json`(1 row), `Chapter.json`(row 4 추가), `Stage.json`(5 row 추가), `Stage_Rule.json` / `Stage_Rule_Param.json` / `Stage_Rule_Trigger.json`(§5.2 row 추가), `HeroArchetype.json`(Berserker 추가), `hero_skills.json`(스킬 3종 + loadout 3 row 추가), `Strings_Ko.json`(1004 / 1014 / 1024 / 1131~1135 추가).
- SO: `HeroSkillLoadout_Berserker.asset`, 스킬 SO `HeroSkill_CrushingLeap.asset` `HeroSkill_ChainPull.asset` `HeroSkill_BloodFrenzy.asset`(`Art/Skills/`), `HeroStageVariantConfig.asset`(ChapterId=4 엔트리 5개: `Tier` 0~4, `UseEmission` false/false/true/true/true, `EmissionIntensity` 0/0/1.5/2.2/3.2, `ScaleMultiplier` 1/1/1/1/1.35).
- 프리팹: `Berserker.prefab`(`Art/Characters/`, Knight 프리팹과 동일 컴포넌트 구성, `_centerPullEnabled` off), `EVisual` 4종 FX 프리팹(`Art/FX/`).
- 스프라이트·배경: §7.1, `Backdrop2D_SheetSpec.json` 엔트리 1개.
- 로드 검증(`ChapterDatabase`): 챕터당 스테이지 5개, 복합키 중복 0, FK(`ChapterRow.heroId` -> `HeroArchetype`, `HeroArchetype_Charge.heroId` -> `HeroArchetype` 의 aiType=Charger, `HeroArchetype_Cleave.heroId` -> `HeroArchetype`), aiType=Charger 영웅은 Charge row 필수(역방향 검증 포함), 규칙 필수 키 집합 일치, 트리거 hpFraction 이 스킬 게이트/패시브 임계와 불일치.

---

## 8. 영향 범위 체크리스트 (챕터 3 추가 때 손댄 곳 기준)

| # | 대상 | 변경 | 근거 |
|---|---|---|---|
| 1 | `ChapterGate.PlayableChapterCount` | **3 -> 4** (const. `PlayableStageCount` 15 -> 20). 챕터 3 문서 §7.4 체크리스트에는 없었던 터치포인트이며, 이 값이 올라야 월드맵·마을 캐러셀·출격이 4장을 연다. **구현 분할의 마지막 슬라이스에서만 올린다**(AI·스킬·아트가 갖춰지기 전에 4장이 출격 가능해지는 것을 막음, 3장도 마지막 슬라이스 (d)에서 열었다). 그 전 중간 기간에는 `Chapter.json` row 4 가 먼저 들어가므로 `Chapter3OpenedWorldTests` 60행 단정(`ShippedDb().ChapterCount == PlayableChapterCount`, 4 != 3)이 깨진다 -> (a) 슬라이스에서 이 단정을 "게이트 상향 전까지 `ChapterCount - 1`"로 임시 완화하고 게이트 상향과 함께 원복한다 | `ChapterGate.cs` 주석 "새 챕터는 에셋·데이터가 갖춰진 뒤 이 값만 올리면 한 번에 풀린다" |
| 2 | `StageProgress.MaxStage` | `Chapter.json` row 수 기반 런타임 값이라 row 4 추가로 15 -> 20 (코드 변경 없음). `DefaultChapterCount`(2, 테스트 기본값)는 유지 | `ConfigureChapterCount(db.ChapterCount)` |
| 3 | `MetaProfile.Version` / `MetaProfileMigration.CurrentVersion` | **6 유지 권장.** 스키마 필드 변경 없고 범위만 0~15 -> 0~20 으로 넓어져 v6 프로필이 그대로 유효하다. `schemaVersion` 은 클라우드 문서에 저장만 되고 복원 시 거부 로직이 없으므로(`FirebaseSdkApiClient` 확인) 7 로 올려도 구버전 클라이언트 보호 효과가 없다. 올리면 모든 v6 프로필이 불필요하게 마이그레이션(SelectedHero 재설정)을 한 번 더 탄다. 알려진 제약: MaxStage 15 인 구버전 클라이언트가 Cleared 16~20 문서를 복원하면 15 로 잘린다(E-C3-8 과 같은 성격) -> 신버전 동시 배포 | `MetaProfileMigration.cs`, `NetDtos.cs`, `FirebaseSdkApiClient.cs` |
| 4 | `StageLeaderboard.IsValidStage` | 1~20 (`MaxStage` 참조라 코드 변경 없음). 문서 ID `{stage}_{uid}` 의 stage 는 선형 번호, `hero` 필드 = "Berserker" | chapter-system §2.3 |
| 5 | **사용자 작업(Firebase 콘솔)** | Firestore 보안 규칙 `stage` 허용 범위를 1~15 -> 1~20 으로 확장 (복합 인덱스 `(stage, clearTimeMs)` 는 그대로 유효). 규칙 변경 전에는 16~20 제출이 서버에서 거부되며 클라이언트는 조용히 실패 처리 | chapter-3-hero D-C3-10 과 같은 성격 |
| 6 | `WorldMapViewModel` / `HeroSelectPopup` | 코드·프리팹 변경 없음을 목표로 한다(4번째 챕터 탭은 데이터 구동). 챕터 탭 위젯 수가 5개 고정(출시 4 + 미출시 1)인지 확인 - 미출시 탭 `5장 ???` 1개 유지. 잠금 안내 문구 1314/1315 는 `{n}` 포맷이라 재사용. **스테이지 노드 4-5 상세 패널**(규칙 0 + 페이즈 2건) 레이아웃은 3-5 와 같은 구조 | chapter-3-hero "이월" 항목 |
| 7 | `VillageViewModel` 영웅 표기 분기 | `EHero.Knight` / `EHero.Archer` 비교 분기(104~106행)가 있어 Assassin 처리와 함께 Berserker 표기(영웅명·초상) 분기 확인 | `VillageViewModel.cs` |
| 8 | `BattleController` | ① `EHeroAi.Charger` 분기(AI 설정 주입, Skirmisher 분기 664행 인접) ② 로드아웃 맵(802~804행)에 `{ EHero.Berserker, EData.HeroSkillLoadout_Berserker }` ③ `WarmVisual` 영웅별 FX 워밍(1557~1566행)에 Berserker 추가 ④ 챕터 4 영웅 프리팹 선택 ⑤ 랭킹 제출 `hero` | `BattleController.cs` |
| 9 | `ChapterDatabase` | `HeroArchetype_Charge` / `HeroArchetype_Cleave` 로드·FK 검증·역방향 검증(`AddSkirmishes` 패턴 복제). `ChapterDatabaseLoader` 로딩 목록에 2종 추가 | `ChapterDatabase.cs` 210~238행 |
| 10 | `AutoCombatAI` | Charger 분기 + 범위 베기 판정 (기사·궁수·암살자 경로 불변) | §3.3 |
| 11 | 스킬 | `CrushingLeapSkillData` `ChainPullSkillData` `BloodFrenzySkillData` 신규 `$type`, 컨텍스트 확장 | §4 |
| 12 | `HeroStageVariantApplier` | 조회 키 (챕터, 인덱스) - 변경 없음, 엔트리만 추가 (엔트리 누락 시 로드 오류 E-11) | chapter-system §1.2 |
| 13 | `RankingPopup` / `RecordsPopup` | 스테이지 선택 컨트롤 1~20, 기록 행 20 (풀링 스크롤이라 행 추가) | chapter-system §2.3 |
| 14 | 마을 캐러셀(`VillageController` / `VillageViewModel`) | ◀/▶ 1~20 순회, `STAGE 4-N` 표기, 쇼케이스 영웅 광전사 교체 | chapter-system §6.4 |
| 15 | 배경 | `Battle_Backdrop_EmberForge.png` + `Backdrop2D_SheetSpec.json` 엔트리 + Addressables 등록 | §6 |

### 8.1 영향받는 기존 테스트 (값 갱신 대상, 파일명 단위)

| 파일 | 영향 |
|---|---|
| `Chapter3OpenedWorldTests` | `PlayableChapterCount == 3`, `PlayableStageCount == 15` 단정이 4 / 20 으로 갱신되거나 "챕터 3 시점 스냅샷"으로 명시 분리. `ShippedDb().ChapterCount` 와 `PlayableChapterCount` 일치 단정은 유지(4 == 4) |
| `ChapterGateAndStageLabelsTests` (참조 21건) | 챕터 4 가 이제 플레이 가능 - "4장 잠금/준비 중" 단정이 있으면 5장으로 이동 |
| `WorldMapViewModelTests` · `WorldMapViewModelBehaviorTests` · `WorldMapStringsAndPrefabTests` · `WorldMapRedesignTests` | 챕터 수 4, 미출시 탭 1개, 노드 20 |
| `VillageCarouselAndRecordsLabelTests` · `VillageStageCarouselTests` · `RecordsVisibleStageCountTests` · `RecordsStageRowTests` | 캐러셀 상한 20, 기록 행 20 |
| `Chapter3DataValidationTests` · `ChapterDatabaseTests` · `ChapterDatabaseValidationTests` · `ChapterTestSupport` · `StageRuleFactoryAndDataTests` · `StageRuleTests` | 챕터 수·스테이지 수·규칙 row 수 기대값, 새 FK 검증 |
| `StageProgressEdgeTests` · `StageProgressChapterEdgeTests` · `ChapterProgressTests` · `MetaProfileMigrationTests` | 경계(15<->16, 20), `ResolveClearedStage` 상한 20, v6 로드 시 Cleared 16~20 보존 |
| `HeroStatTableRegressionTests` | Berserker 0.60 x 1.30 등 `ScaleStat` 단일 반올림 |
| `Chapter3ArtIntegrityTests` · `Backdrop2DImportTests` · `DotSceneRegressionTests` | 신규 `Chapter4ArtIntegrityTests`(시트 5장 + 에미션 + 초상 + 배경)로 선례 복제, 배경 임포트 목록에 EmberForge |
| `UiRedesignDataTests` | 챕터 수 의존 단정 점검 |

---

## 9. 테스트 / QA 검증 항목

### 9.1 test-engineer (EditMode)

| 영역 | 항목 |
|---|---|
| `StageProgress` | 선형 <-> (챕터, 인덱스) 경계(15<->16, 20), `IsUnlocked` 챕터 경계(cleared 15 -> 16 해금, 17 잠금), `ResolveClearedStage` 상한(MaxStage=20), `ScaleStat` 단일 반올림(Berserker 0.60 x 1.55 = 46.5 -> 47 소수 경계 0.5) |
| 마이그레이션 | v6(Cleared 0/10/15)->v6 값 보존·`Version == 6`, Cleared 16~20 보존, `StageRecords` 범위 밖(21 이상) 제거, 클라우드 복원 후 재적용 |
| JSON 검증 | `HeroArchetype_Charge` · `HeroArchetype_Cleave` FK, aiType=Charger 영웅의 Charge row 필수, 챕터 4 스테이지 5개, 트리거 hpFraction 0.55 / 0.35 가 스킬 게이트/패시브 임계와 불일치, 규칙 필수 키 집합 일치 |
| 군집 선택 | `clusterScore` 순위(군집 수 최대 우선, 동점 시 가까운 몬스터), `retargetCountMargin` 2 히스테리시스, 봉인 종족 제외, 후보 없음 시 제자리 대기 |
| Charger 상태 | 이동->교전->재평가 전이, `minDwellSec` 진동 방지, 목적지 `arenaSoftRadius` 클램프, 공포(FleeMode) 우선, 기사·궁수·암살자 경로 회귀(동작 불변) |
| 범위 베기 | 반경 2.2 안 가까운 순 최대 4마리, 첫 대상 100% · 나머지 x0.5, 반경 밖 제외, 단일 판정 영웅(기사·궁수·암살자) 회귀 |
| 스킬 | 분쇄 도약 최소 거리 3.0 미만 미시전·도약 중 무적 0.3초, 사슬 갈고리 최대 6마리·4마리 미만 미시전, 피의 광란 쿨다운 x0.6 · 받는 피해 x1.2 · 6.0초 후 복구, 우선순위(갈고리 > 광란 > 도약) |

### 9.2 PlayMode / UnityMCP

- 월드맵 플로우: 마을 -> 지도 -> 챕터 4 탭 -> 4-1 선택 -> 출격 -> 규칙 발동 -> 결과 -> 해금(4-2). 챕터 3 보스 클리어 후 4장 탭 해금.
- 4-5 보스까지 실제 플레이: 보스 페이즈 55% / 35% 발동, 스킬 게이트 85/65/45%, 카드 일시정지와 비중첩.

### 9.3 qa-simulator 게이트 (리포트 `docs/qa-reports/`)

| 게이트 | 대상 | 측정 | 통과 기준 |
|---|---|---|---|
| **G8** | 광전사 AI | 교전 시 평균 베기 대상 수, 교전 시간 비율, 군집 도달 시간(재평가~교전 진입), 목표 교체 빈도, 전장 이탈, 기본 공격 실효 DPS | 베기당 평균 대상 수 2.0 이상, 교전 시간 비율 50~80%(생존 시간 대비), 군집 도달 평균 4.0초 이하, 목표 교체 10초당 3회 이하, 전장 밖(`arenaSoftRadius` + 1) 프레임 0, 실효 DPS(스킬 제외) 가 같은 인덱스 스테이지 기사 DPS 의 115~145% (§3.2 설계 중심 124~131% 를 감싸는 범위) |
| **G9** | Ch4 곡선 | 스테이지별 평균 처치 시간 | 각 `targetClearSec` +-15%, 톱니(4-1 기대 시간 < 3-5 기대 시간) 성립, 4축 빌드별 편차 +-30초 |
| **G10** | Ch4 보스 | 4-5 첫 도전 승률, 보스 구간 시간(보스 밴드 210~270초), 페이즈 발동 후 필드 평균 몬스터 수, 빌드별 승률 | 승률 40~50%, 필드 점유 평균 12마리 이상(`ArrowRain` + 광전사 베기가 필드를 비우지 않음), **Swarm 빌드 승률 < Tank·Dps·Debuff 각 빌드 승률**(가위바위보 의도). 어긋나면 §3.2 베기 수치만 조정 |
| 보조 | 성수 샘(4-3) | 샘 개방 중 영웅 체류율 | 0.25 +-0.15 (`ruleFactor` 가정 검증) |
| 보조 | 스킬 | 사슬 갈고리 후 베기 대상 수, 분쇄 도약 처치율, 피의 광란 구간 영웅 피해량 | 갈고리 후 첫 베기 대상 3.5 이상, 도약 대상 처치율 60% 이상, 광란 구간 영웅 DPS 증가분 +50% 이상 / 받는 피해 증가분 +20% 이내 |
| 보조 | Fear 카드 | 광전사 스테이지 Fear 픽률/효과 | 기사 스테이지 대비 효과 저하가 확인되면 카드 재조정안은 별도 기획 |

---

## 10. 엣지 케이스

| # | 케이스 | 처리 |
|---|---|---|
| E-C4-1 | 필드에 군집이 없다(몬스터 1마리씩 흩어짐) | 군집 수 1 중 가장 가까운 몬스터가 목표. 베기는 단일 판정과 같음(의도된 약점, 단일 DPS 32.5~46.7) |
| E-C4-2 | 군집 한가운데에서 영웅이 정지해 둘러싸임 | 광전사는 이탈 행동이 없다(의도). 생존은 HP 두께(`hpScale` 1.25) + Tank 빌드에 반해 Dps 빌드의 압박 구간 |
| E-C4-3 | `SealedNest`(Wisp, 4-2)로 군집 재료가 얇은 동안 목표가 계속 바뀜 | `retargetCountMargin` 2 와 `minDwellSec` 0.4 가 진동을 막는다. 해제 시점(영웅 HP 60% 이하, 즉시 1회 스폰 버스트)에 군집이 급증하면 자연스럽게 재평가 |
| E-C4-4 | `BossBulwark`(x0.6)와 `피의 광란`(x1.2) 동시 | 곱연산 0.72. 하한 없음, G10 에서 보스 구간 영웅 생존 시간 확인 |
| E-C4-5 | 분쇄 도약 목적지가 전장 밖 | 목적지를 `arenaSoftRadius` 7.0 안으로 클램프 |
| E-C4-6 | 사슬 갈고리로 당긴 직후 군집 수가 급증해 재평가가 목표를 바꿈 | 갈고리 시전 중·직후 0.6초 재평가 중단 (§3.3 #6) |
| E-C4-7 | `HolySpring`(4-3) 위에 군집이 있어 영웅이 샘 안에 오래 머묾 | 회복이 `ruleFactor` 가정(체류율 0.25)보다 커짐. 보조 게이트 체류율로 확인. 샘 위치를 노려 군집을 유도하는 것은 의도된 플레이 |
| E-C4-8 | 클라우드 복원 시 구버전(MaxStage 15) 클라이언트가 ClearedStage 16~20 문서를 복원 | Clamp 로 15 까지 보존되는 데이터 손실 가능. 챕터 4 출시 시 신버전 동시 배포(E-C3-8 과 같은 성격) |
| E-C4-9 | `DenseFog` 하한 0.8 이 영웅 충돌 거리보다 작음 | 충돌 거리 + 0.1 로 상향하고 보고(chapter-system E-7) |
| E-C4-10 | 범위 베기 대상 중 일부가 봉인/사망 처리 중 | 베기 판정은 살아 있는 필드 몬스터만 대상으로 하며, 사망 판정은 기존 `IDamageable` 경로 |

---

## 11. 미결 사항 · 사용자 승인 항목

| # | 항목 | 추천안 | 상태 |
|---|---|---|---|
| **D-C4-1** | 챕터 4 범위 (CLAUDE.md §8 ⑥ 에 이미 승격). 신규 규칙 0종이므로 별도 승인 불필요 | 승격 완료 | 승인 완료 (2026-10-17 CLAUDE.md 반영) |
| **D-C4-2** | 챕터 4 영웅·맵 테마 (§1.2): A 방패병 / B 룬 마법사 / C 광전사 | **C. 광전사** (불꽃의 용광로) | **사용자 선택 필요** |
| **D-C4-3** | 광전사 팔레트 (§3.5): A 무채-유채 혼합 점프형 / B 무채색 철 램프 + 발광 단일 색조. A 의 4-3 강청이 암살자 인디고와 18.7° | **A** (4-3 이 눈에 띄면 4-3 무채색 변형, 그래도 부족하면 B) | **사용자 선택 필요** |
| **D-C4-4** | 데이터 구조표 (§2): 신규 스키마 `HeroArchetype_Charge` · `HeroArchetype_Cleave` 2종 분리 | 제안 구조 | **사용자 승인 필요** |
| **D-C4-5** | 스킬 3종 신규 `$type` (§4: 분쇄 도약 / 사슬 갈고리 / 피의 광란) 및 신규 효과(몬스터 강제 이동, 공격 쿨다운 배율) | 제안안 | **사용자 승인 필요** |
| **D-C4-6** | 공격 클립 이벤트 `OnAttackStrike` 0.200s + `OnAttackEnd` 0.580s 베이크, `slash02`/`stab` = `slash01` 행 복제 (§3.4) | 제안안 채택 | **사용자 승인 필요** |
| **D-C4-7** | 5단계 `ScaleMultiplier` 1.35 (§3.5) | 채택 | **사용자 승인 필요** |
| **D-C4-8** | 규칙 배정 (§5.1: 신규 규칙 0종, 봉인 대상 Wisp / Reaper) 및 보스 페이즈 안 (A 결계 0.55 + 화살비 0.35 / B 3페이즈 / C 3장 복제) | **A** | **사용자 선택 필요** |
| **D-C4-9** | `M_ref` 증가폭 +0.01 (Ch3 의 +0.02 대신), 4-5 eq 2.55 (§5.3). Ch4 참고 상한 2.60 유지 | 채택 | **사용자 승인 필요** |
| **D-C4-10** | `MetaProfile.Version` 6 유지 (§8 #3). 대안: 7 로 승격 | **6 유지** | **사용자 선택 필요** |
| D-C4-11 | 사용자 작업(Firebase 콘솔): Firestore 보안 규칙 `stage` 허용 범위 1~20 확장 | - | 사용자 작업 |
| D-C4-12 | 수치 초안 (스탯 §3.2, 스킬 §4, 스테이지 §5.3) - G0/G1 -> G8/G9/G10 검증 후 확정 | 초안 채택, 검증 후 확정 | **사용자 승인 필요** |
| D-C4-13 | 월드맵 4번째 탭 UI 목업: 신규 위젯이 없어 별도 목업 불필요. 4-5 상세 패널 가변 높이는 3장 "이월" 항목과 같이 후속 | 별도 목업 없음 | 확인만 필요 |

**수치 확정 방식**: 본 문서 모든 배수·`ruleFactor`·`M_ref`·AI·스킬 수치는 초안이다. 확정 순서는 G0(T1 확정, chapter-system §0.3) -> G1(Ch1) -> G3(Ch2) -> G5~G7(Ch3) -> G8(광전사 AI) -> G9(Ch4 곡선) -> G10(Ch4 보스). 결정 메트릭은 §9.3 표, 재산출 규칙은 chapter-system §0.4 도출식.

---

## 12. Self-Review

- **Placeholder 잔존**: 미정 마커·애매한 권유·두 갈래 위임(디자인 결정 영역)·어림 표현 없음. 미확정 수치는 "초안 - G0/G1 검증 후 확정" 과 결정 메트릭(§9.3 G8/G9/G10)으로 명시. 사용자 선택이 필요한 항목은 §11 에 "사용자 선택 필요" 로 모았다. 구현 판단 영역(인터페이스 신설 여부)만 "gameplay-programmer 판단"으로 위임(코드 구조 지정 금지 원칙).
- **내부 일관성**: 스탯(1.25/0.60/1.8/1.2/3.4), 베기(2.2/4/0.5), 스킬 수치, 스테이지 표(hp 1.28/1.35/1.55/1.62/1.87, power 1.30/1.40/1.55/1.70/1.85, Scale 1.35), 규칙 파라미터가 §2·§3·§4·§5·§7·시안에서 동일. 검산표 §5.3 에 산식 병기.
- **명명 일관성**: `Berserker` / `Charger` / `HeroArchetype_Charge`(`HeroArchetype_ChargeRow`) / `HeroArchetype_Cleave`(`HeroArchetype_CleaveRow`) / `ChargeSettings` / `CleaveSettings` / `HeroSkill_CrushingLeap` / `HeroSkill_ChainPull` / `HeroSkill_BloodFrenzy`(`CrushingLeapSkillData` / `ChainPullSkillData` / `BloodFrenzySkillData`) / `Battle_Backdrop_EmberForge` / `clusterScore` 일관. 규칙·테이블 이름은 chapter-system 과 동일.
- **스코프**: 기획 + 시안까지만. 구현 분할 제안은 §7.5 대신 아래 표. CLAUDE.md 는 이미 승격되어 있어 수정하지 않는다.
- **구현 요청사항 완전성**: Enum / Interface / 에셋 키 / JSON·SO 스키마 §2·§7 수록, 영향 범위 §8.
- **UI 목업**: 신규 UI 화면 없음(월드맵 4번째 탭은 데이터 구동). 배경/영웅 시안은 `.mockups/` 에 작성.

### 구현 분할 제안

| 단위 | 내용 | 의존 |
|---|---|---|
| (a) 데이터/진행 | Chapter 4 row · Stage · 규칙 row, 스키마 2종 + 검증, Strings (`ChapterGate` 는 올리지 않음) | 없음 |
| (b) 영웅 AI/스킬 | Charger AI, 범위 베기, 스킬 3종 | (a) |
| (c) 아트 | 시트 5장 + 에미션 + 초상 5장 + FX 4종 + 배경 | 시안 승인 |
| (d) UI + 개방 | 월드맵 4번째 탭 확인(위젯 신설 없음), 4-5 상세 패널 검수, **마지막에 `ChapterGate.PlayableChapterCount` 4 상향** + 영향 테스트 갱신 | (a)(b)(c) |
