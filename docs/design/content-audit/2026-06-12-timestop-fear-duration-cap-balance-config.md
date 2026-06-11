# Content Audit — 2026-06-12 — TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7
- 참조 spec/plan 수: 28개 (전체)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED 상태)
- 과거 감사 이력 (git log `# [Routines][Daily Content Audit]`): 4건 (가장 최근: 2026-06-10)
  - 폴더 존재 확인 시 14개 파일 추가 발견 (구 포맷 커밋 — git log 누락분). 중복 회피는 14개 기준으로 교차 적용.

---

## 1. 현황

| 카테고리 | 컨셉 §11.3 기준 | 실제 에셋 수 | 차이 |
|---|---|---|---|
| 영웅 | 1 (Knight) | 1 (Knight.prefab) | 0 |
| 몬스터 | 6종 | 6종 (Wisp·Wraith·Reaper·Hex·Plague·Phantom.prefab) | 0 |
| 패시브 카드 | 16장 | 16장 (asset 확인) | 0 |
| 액티브 카드 | 12장 | 12장 (asset 확인) | 0 |
| 카드 이펙트 클래스 | 28개 | 28개 (.cs 확인) | 0 |

### 계획 있으나 미구현
- **Multiply → SwarmRush 교체**: `card-renewal.md §3.4` 원안 "팬텀 6마리 즉시 소환(SwarmRush)" 미구현. `FastBreedingEffect`("빠른 번식", 팬텀 스포너 주기 ×0.6 영구) 잔존.
- **QA 시뮬 훅**: `DebugAutoPicker` 미구현 → qa-simulator 전면 차단 (2026-05-22 QA 리포트 §3 요청).
- **Village 메타허브**: 현재 구현 진행 중 (v0.2 목표 기능 — `village-meta-hub.md`).

### QA 권고 미해결
- `DebugAutoPicker` 훅 미구현 → 시뮬레이션 인프라(`LairSimWindow`/`SimDriver`) 착수 불가.
- 베이스라인 재측정 미수행 — 현행 76.04s(QA 6차 인용치)가 영웅 스킬·카드 리뉴얼·3픽 캡 반영 이전 수치.
- 만렙 + SpawnerHaste 3픽 + Swarm Tier2 프로필 시뮬 게이트 미수행 (`village-meta-hub.md §3.4`).

### 과거 감사 후보 (git log 조회 결과 + 폴더 교차 확인)

**git log 4건 (`# [Routines][Daily Content Audit]` 포맷)**

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-07 | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |

**폴더 교차 확인 — 구 포맷 10건 (git log 누락, 참고용)**

| 파일일자 | slug 요약 |
|---|---|
| 2026-05-28 | spawn-interval-multiplier-balance-config |
| 2026-05-29 | curse-card-rebalance-effectiveness |
| 2026-05-30 | plague-spawner-passive-unlock |
| 2026-05-31 | hero-hp-4600-balance-tuning |
| 2026-06-01 | spawner-haste-stack-cap |
| 2026-06-02 | debuff-tier3-eternal-bleed-aura-balance |
| 2026-06-03 | swarm-tier3-spawner-output-scope-trim |
| 2026-06-04 | multiply-to-swarm-rush-active-replace |
| 2026-06-05 | tank-tier3-durability-buff-design |
| 2026-06-06 | swarm-tier2-spawnerhaste-period-stack-nova-guard |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### TimeStop·Fear 지속시간 누적 상한 캡 — BalanceConfig 손잡이 추가

- **카테고리**: Swarm 액티브 수치 재조정 + BalanceConfig 손잡이
- **요지**: `TimeStop`(5s)과 `Fear`(3s)는 지속시간 누적 정책(`잔여+duration`)이라, HP% 패시브·30초 액티브가 짧은 간격 내 동시 큐에 쌓일 때 두 카드가 연속 픽되면 영웅 행동 제약이 8s+ 연속으로 이어질 수 있다. `card-renewal.md §7.2`는 이를 "밸런스 모니터링 항목"으로 명시했으나, 현재 `BalanceConfig`에 조정 손잡이가 없어 설계 단계에서 상한을 고정할 수밖에 없다. `TimeStopMaxDuration`·`FearMaxDuration` 두 필드를 `BalanceConfig`에 추가해 연장 상한을 인스펙터에서 튜닝 가능하게 한다.
- **검증가치 / 구현비용 / 시너지폭 / 데이터근거**: 4 / 2 / 3 / 4 → 종합 **15**
- **근거**: `docs/design/card-renewal.md §7.2` — "TimeStop / Fear. 영웅 행동 제약 강력. 5s / 3s. 지속시간 누적 일관 정책 → 중첩 시 영웅 8s 정지 가능. *밸런스 모니터링 항목*."
- **MVP 범위**: 컨셉 §11.2 "액티브 카드 효과값/지속시간 재조정" + "BalanceConfig 손잡이 추가" 항목.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   30초 액티브 트리거(0:30, 1:00, …, 4:30)가 발동하면 카드 팝업이 뜬다. `TimeStop`(Swarm A)과 `Fear`(Debuff A)는 모두 액티브 카드이므로 패시브 트리거와는 별개로 이 팝업에만 등장한다. HP% 패시브 트리거와 30초 액티브 트리거가 동시에 발화하면 §4.3 큐 처리 원칙에 따라 패시브 → 액티브 순으로 연속 처리되며, 이 "버스트" 구간에 TimeStop을 픽하면 영웅 정지 상태에서 다음 픽까지 시간이 이어질 수 있다.

2. **화면 변화**
   TimeStop 픽 즉시 영웅이 완전 정지(위치 고정)하고 화면 상 영웅 위 정지 아이콘이 표시된다. Fear 픽 시 영웅이 반대 방향으로 도주한다. 현재 구현에서는 두 효과 모두 지속시간 누적 정책이라, 이미 정지 중인 영웅에게 TimeStop을 다시 픽하면 `잔여 + 5s`로 연장된다. 제안된 캡 적용 시 잔여시간이 캡 이상이면 연장이 발생하지 않고 이미 "만차" 상태임이 UI 바를 통해 표시된다.

3. **입력 행동**
   플레이어가 카드 선택 팝업에서 TimeStop 또는 Fear 카드를 클릭한다. 같은 카드는 한 런 내 최대 3번(전역 3픽 캡)까지 픽 가능하다. 두 카드는 서로 다른 축(Swarm·Debuff)이라 동일 팝업에 함께 제시될 수 없고, 반드시 연속 트리거를 통해서만 두 효과를 같은 런에 쌓을 수 있다.

4. **시스템 반응**
   현재: `TimeStopEffect.Apply()` → `HeroActionConstraintAura` 에 `Math.Max(잔여, 0) + _duration` 로 잔여 연장. 제안 변경: `Math.Min(Math.Max(잔여, 0) + _duration, BalanceConfig.TimeStopMaxDuration)` 으로 상한 적용. `Fear`도 `FearEffect.Apply()` 에서 동일 패턴으로 `BalanceConfig.FearMaxDuration` 참조. `BalanceConfig`에 두 필드(기본값 제안 `TimeStopMaxDuration = 8f`, `FearMaxDuration = 5f`)를 추가한다.

5. **반복·재발생 패턴**
   TimeStop은 30초 간격 액티브 트리거에서 최대 3픽(3픽 캡) 가능하다. 30초 간격이면 각 TimeStop 효과(5s)는 다음 트리거 전에 이미 만료된 상태이므로 정상 플레이에서는 순차 5s×3회로 나타난다. 위험 구간은 HP% 패시브 트리거와 30초 액티브 트리거가 같은 큐 버스트에서 처리될 때 — 패시브 픽 종료 후 곧바로 액티브 픽이 이어지면, 각 픽 사이 실제 경과 시간이 거의 0에 가까워 TimeStop + Fear가 사실상 동시 적용 구간을 만들 수 있다.

6. **종료·해소 조건**
   TimeStop 잔여시간이 0이 되면 영웅 이동·공격이 자동 재개된다. Fear 잔여시간이 0이 되면 영웅 도주가 멈추고 가장 가까운 몬스터를 향해 복귀한다. 두 효과가 동시에 걸린 경우(이론상) 더 긴 효과가 끝날 때 정상 상태로 돌아온다. 캡이 적용되면 "픽했는데 연장 안 됨" 상황이 발생하므로, 플레이어에게 현재 잔여시간을 수치 또는 게이지로 노출하는 UI 피드백이 중요하다.

7. **다른 시스템과 상호작용**
   Swarm Tier3(스포너 동시 출력 +1)이 활성화된 상태에서 TimeStop이 적용되면, 영웅이 정지한 동안 추가 스포너 출력이 계속 쌓여 영웅 주변 밀집도가 극적으로 올라간다. 캡이 없으면 TimeStop 연속 연장으로 Tier3 효과가 증폭되는 구조. `MarkOfDeath`(받는 데미지 ×1.5, 5s)는 지속시간 누적이지만 영웅을 정지시키지 않아 비교적 안전하다. `Bleed`(이동 시 HP -2%)는 TimeStop과 시너지가 역방향(정지 중에는 Bleed 비발동)이라 무관하다.

8. **엣지 케이스**
   TimeStop 잔여시간이 7.5s(캡 `8f` 대비 0.5s 여유)인 상태에서 다시 픽하면 연장량이 0.5s에 불과하다. 이 경우 플레이어가 "픽이 거의 낭비됐다"고 느낄 수 있으므로 카드 선택 팝업에서 현재 잔여시간 또는 "연장 가능 여유" 정보를 표시하는 것이 권장된다. 캡값이 지나치게 낮으면 TimeStop의 첫 픽(5s)이 캡 대부분을 채워 2번째 픽이 의미 없어지므로, `TimeStopMaxDuration` 기본값은 최소 `2 × _duration = 10s` 이상으로 설정하고 `8s`는 보수적 시작점으로 제안한다.

9. **유저 정보·피드백**
   현재 영웅 위에 상태 아이콘 시스템(`hero-status-icons.md`)이 구현되어 있으므로, TimeStop 효과 중 정지 아이콘 + 잔여시간 숫자를 표시하면 "얼마나 남았는지"를 플레이어가 인지할 수 있다. Fear도 동일하게 도주 아이콘 + 잔여시간 표시. 캡 도달 상태에서 픽을 시도하면 효과 적용 직후 "최대 도달" 토스트(기존 시너지 Tier 발화 피드백(`card-renewal.md §8.4`) 패턴 활용)를 보여줄 수 있다.

### 보류 (채점 2~3위)

| 후보 | 카테고리 | 종합 | 보류 사유 |
|---|---|---|---|
| Swarm 액티브 Multiply 팬텀 주기 배율 재조정 | Swarm 액티브 수치 | 15 | 데이터근거 3 (TimeStop 4 대비 열세) — 또한 `2026-06-04-multiply-to-swarm-rush-active-replace` 폴더 파일 존재, 주제 근접 |
| Swarm 패시브 PhantomMoveSpeedBoost 3픽 배율 상한 | Swarm 패시브 수치 | 14 | 데이터근거 3, 구현비용 동일 — TimeStop 제안과 종합점수 열세 |

---

## 3. 과거 감사 대비 차별성

git log 4건 + 폴더 교차 10건 = **14건 검토 완료.**

- **Swarm 축 액티브 이전 감사**: `2026-06-04-multiply-to-swarm-rush-active-replace` (Multiply→SwarmRush 교체), `2026-06-06-swarm-tier2-spawnerhaste-period-stack-nova-guard` (Swarm Tier2 + SpawnerHaste 주기 스택) — 본 제안과 카테고리가 "Swarm 축"으로 겹치지만, 이전 감사들은 **스포너 출력/주기**에 집중한 반면 본 제안은 **영웅 행동 제약(정지/도주) 지속시간 캡**이라는 완전히 다른 레이어의 문제를 다룬다.
- **가장 유사한 이전 파일**: `2026-06-06-swarm-tier2-spawnerhaste-period-stack-nova-guard` (SHA — git log 미포함 구 포맷) — 차별점: 해당 파일은 스포너 주기 누적 상한이고, 본 제안은 영웅 행동 제약 카드의 지속시간 누적 상한. 대상 메커니즘·대상 엔티티(스포너 vs. 영웅) 모두 다르다.
- "TimeStop 지속시간 캡" 또는 "Fear 지속시간 캡" 키워드를 가진 파일은 14개 중 없음.
- Debuff 축 감사 이후 6일(2026-06-08)이 지났으나, 본 제안의 Fear는 Debuff 축이 아닌 "영웅 행동 제약 카드군" 묶음으로 접근하므로 Debuff 수치 재조정과는 구별된다.

---

## 4. 제외 (범위 밖)

- `SwarmRush` 신규 카드 구현 — `Multiply` 현행 `FastBreedingEffect` 유지인 상태에서 SwarmRush 신규 구현은 코드 작업 수반, 본 감사 루틴 범위 밖. (별도 감사 기록: `2026-06-04`)
- `DebugAutoPicker` 훅 구현 — gameplay-programmer 영역 (QA 리포트 2026-05-22 §3 요청 대기 중).
- Village 메타허브 콘텐츠 — 현재 구현 진행 중인 v0.2 목표 기능이므로 별도 기획서 단일 진실.
- 신규 영웅·몬스터·카드 리소스 추가 — CLAUDE.md §8 명시 금지 (v0.3+).
- TimeStop 효과를 완전 제거하거나 타 축으로 이동 — 기존 `ECardId.TimeStop` Enum/에셋/빌드 카운트 체계 파괴로 §9 절대 금지 수준 공사.

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청: `TimeStopMaxDuration`·`FearMaxDuration` 적정 기본값(8s·5s 안) 검증, `BalanceConfig` SO 필드 추가 명세 작성.
- 이후 gameplay-programmer 가 `BalanceConfig.cs`에 필드 추가 + `TimeStopEffect.Apply()` / `FearEffect.Apply()` 에 캡 체크 삽입.
- qa-simulator 훅(`DebugAutoPicker`) 구현 후 TimeStop 3픽 프로필로 실측 — 영웅 정지 총 시간 분포가 §8 밸런싱 밴드 내인지 확인.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어는 30초마다 카드를 골라 영웅을 약화시키는데, 그 중 "시간 정지" 카드를 고르면 영웅이 5초 동안 꼼짝 못한다. 같은 카드를 여러 번 고르면 멈추는 시간이 계속 늘어나는 구조라서, HP가 깎이면서 패시브 카드와 액티브 카드 선택이 연달아 뜨는 순간에 "시간 정지"와 "공포(도주)" 카드를 한꺼번에 고르면 영웅이 8초 넘게 제대로 움직이지 못하는 상황이 생길 수 있다. 게임 기획 문서에서도 이 조합이 지켜봐야 할 항목이라고 명시해 두었는데, 아직 설정 파일에 "최대 몇 초까지" 라는 제한선이 없어 코드를 뜯지 않고는 수치를 바꿀 수가 없다. 그래서 이번에 제안하는 것은: 설정 파일에 "시간 정지 최대 지속 시간"과 "공포 최대 지속 시간" 두 개의 조절 손잡이를 추가해서, 개발자가 나중에 숫자 하나만 바꿔도 게임 느낌을 빠르게 조정할 수 있게 하는 것이다.
