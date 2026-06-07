# Content Audit — 2026-06-08 — Dps 축 ReaperAtkSpeed 배율 ×0.7 → ×0.75 조정 (Tier2 중첩 쿨다운 하한 없음 해소)

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.6 (2026-05-31)
- 참조 spec/plan 수: 27개 (specs 27 / plans 28)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22 — BLOCKED 상태, 시뮬 미실행)
- 과거 감사 이력 (git log, `# [Routines][Daily Content Audit]` grep): 3건 (가장 최근: 2026-06-06 KST)
- 추가 조회 (`# [docs] - 컨텐츠 감사` grep): 0건

---

## 1. 현황

### 컨셉 §11 대비 실제 에셋 수

| 카테고리 | 컨셉 목표 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1명 (Knight) | Knight.prefab 1개 ✅ | 0 |
| 몬스터 | 6종 | Wisp·Wraith·Reaper·Hex·Plague·Phantom 각 1 ✅ | 0 |
| 패시브 카드 | 16장 | 16장 SO 파일 확인 (28장 전체 중 P16) ✅ | 0 |
| 액티브 카드 | 12장 | 12장 SO 파일 확인 ✅ | 0 |
| 카드 이펙트 클래스 | 28개 | 28개 .cs 확인 ✅ | 0 |

### 계획 있으나 미구현

- **SwarmRush** (`Multiply` 자리 교체 예정): `card-renewal.md` §3.4 #6 에 명기. `Multiply.asset`(FastBreedingEffect) 잔존, SwarmRush.asset·SwarmRushEffect.cs 미존재. → 2026-06-04 감사(KST)에서 기제안(git log `399560e`), 이번 감사 제외.
- **EternalBleedAura** (Debuff Tier3): `card-renewal.md` §10.3 에 구현 검증 필요 플래그. DebuffSynergyTier3의 구현 완료 여부 미확인. → 2026-06-02 내부 파일 기제안 (git log 미포함), 이번 감사 제외.
- **DebuffSynergyTier2 구현 경로 불일치**: `card-renewal.md` §10.3 에 `new HeroAttackDownEffect { _factor=0.85 }` 가 존재하지 않는 필드 참조임이 명시됨 (`HeroAttackDownAura(atk, 0.85f)` 경로 필요). 구현 검증 대상으로 남아있음.

### QA 권고 미해결

- **2026-05-22 BLOCKED**: `BattleController.TryProcessNext()` 의 `await tcs.Task` 구조로 인해 자동 픽 불가. `DebugAutoPicker` 델리게이트 훅 미구현 → 시뮬레이션 전면 차단.
  - QA 권고 ④ (타임오버 ≥1판) · ⑤ (클리어율 ≤80%) 검증 미완.
  - 현재 모든 밸런스 분석은 수치 계산 + 기획 문서 기반 정성 분석으로 대체.

### 과거 감사 후보 (git log 조회 결과 — `# [Routines][Daily Content Audit]`)

| 날짜 (KST) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-06 | e198aa4 | Swarm Tier2×SpawnerHaste 복합 스택 — Nova 쿨 가드레일 재검토 |
| 2026-06-05 | c4f4215 | Tank Tier3 시너지 새 효과 수치 결정 (Wisp·Wraith HP ×1.5 영구, 캡 제거 공백 해소) |
| 2026-06-04 | 399560e | Multiply 액티브 카드 → SwarmRush(팬텀 즉발 소환) 교체 제안 (Swarm A#6 미구현 해소) |

> git log 3건 모두 Swarm·Tank 축. **Dps 축은 git log 기준 과거 감사 미탐색.**

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### [권장] ReaperAtkSpeed 카드 배율 ×0.7 → ×0.75 상향 조정 — Dps Tier2 중첩 시 쿨다운 하한 없음 해소

- **카테고리**: 패시브 카드 효과값 재조정
- **요지**: `ReaperAtkSpeed(P, Dps#1)` 의 `_cdMul=0.7` 이 3픽 캡 (×0.7³ = ×0.343) + Dps Tier2 Cooldown ×0.8 동시 적용 시 Reaper 공격 쿨다운이 `0.5 × 0.343 × 0.8 = 0.137s` (7.3 공격/s) 까지 내려가는데, 이에 대한 최소 쿨다운 하한이 코드에 없다. ×0.75 로 1단계 상향 조정하면 최악 케이스가 `0.5 × 0.75³ × 0.8 = 0.169s` (5.9 공격/s) 로 완화되며, Spawner.ScalePeriod 의 0.05s 클램프 패턴(기존 인프라)을 동일하게 적용할 수 있다.
- **검증 / 구현 / 시너지 / 데이터**: 4 / 1 / 3 / 4 → 종합 **16**
  - 검증가치 4: QA BLOCKED 상태에서 수치 계산만으로 확인 가능한 과부하 리스크. Dps 빌드 체감에 직접 영향.
  - 구현비용 1: SO 필드 1개(`_cdMul`) 수정 + (선택) `AttackerComponent` 에 클램프 상수 추가. 신규 클래스·인터페이스 없음.
  - 시너지폭 3: Dps Tier1 Power×1.3 및 SpawnReapers 카드와 함께 Reaper 단위 DPS 공식 전체에 영향.
  - 데이터근거 4: `continuous-spawn-round.md` §4 기본 스탯 + `card-renewal.md` §3.2 중첩 정책 + §4.2 Tier2 수치로 계산 가능. 명시적 수치 근거.
- **근거 경로**:
  - `docs/design/continuous-spawn-round.md` §4 — Reaper 기본 스탯 (Cooldown 0.5s, Power 6)
  - `docs/design/card-renewal.md` §3.2 #1 — ReaperAtkSpeed `_cdMul=0.7`, 중첩 정책 "곱연산 누적"
  - `docs/design/card-renewal.md` §4.2 — Dps Tier2 Reaper·Hex Cooldown ×0.8 (5장 임계)
  - `docs/design/card-renewal.md` §7.1 — "전역 3픽 캡으로 3픽 값이 실효 상한"
  - `docs/design/card-renewal.md` §9.6 — SpawnerHaste+Tier2 유사 중첩 리스크 선례 (Swarm 축 참고)
- **MVP 범위**: 컨셉 §11.2 "패시브 카드 16장" 포함 항목. 카드 SO 수치 조정(매수 lock 불변).

#### 유저 플로우

1. **노출 시점·트리거**
   플레이어가 Dps 축 패시브 픽 중 `ReaperAtkSpeed` 를 3번 선택한 뒤(전역 3픽 캡 도달), 5번째 Dps 패시브 픽으로 Dps Tier2(5장 임계)가 발동할 때 이 문제가 발생한다. 첫 픽은 HP 90%~50% 선택지(최대 5번 패시브 기회) 사이에 이루어지므로, 전투 초~중반부에 이 상태가 형성된다.

2. **화면 변화**
   Dps Tier2 도달 순간 "Dps 시너지 Tier 2 발동!" 토스트가 표시되고, 빌드 패널의 Dps 행에 아이콘 2개가 점등된다. 이후 필드의 Reaper 들이 현저히 빠른 속도로 공격하는 모습이 시각적으로 확인된다. `_cdMul=0.7` 현행에서는 Reaper 한 마리가 0.137s마다 공격하므로 초당 7회 이상 타격 이펙트가 발생한다.

3. **입력 행동**
   플레이어는 Dps 축 카드를 지속 선택하는 의사결정을 한다. 특히 HP 90·80·70% 선택지에서 `ReaperAtkSpeed` 를 반복 픽하거나, HP 60·50% 선택지에서 다른 Dps 카드를 2장 추가 픽해 Tier2 임계에 도달하는 경로를 취한다. 이 과정에서 Tier2 도달 타이밍이 전투 시간의 50% 이전에 발생하는 경우가 많다.

4. **시스템 반응**
   `ReaperAtkSpeed` 3픽 후 Reaper 의 `CooldownScale` 은 0.343 (= 0.7³) 으로 기록된다. Dps Tier2 발화 시 `RegisterMonsterTypeBuff(Reaper, Cooldown, 0.8)` 가 추가 곱연산되어 `CooldownScale = 0.343 × 0.8 = 0.275` 가 된다. Reaper 실제 쿨다운은 `0.5 × 0.275 = 0.137s` 로, 현행 코드에 최소 쿨다운 클램프가 없으면 이 값이 그대로 적용된다. `SpawnReapers` 카드(Reaper 출력 +1) 가 선행 픽되어 있으면 동시에 2마리 이상의 Reaper 가 이 빠른 공속으로 공격한다.

5. **반복·재발생 패턴**
   이 상태는 픽 적용 후 라운드 끝까지 유지된다(글로벌 영구 버프). Tier2 는 라운드당 1회 발화하며 이후 재발화 없이 누적 상태가 유지된다. 추가로 `Frenzy` 액티브 카드(공속 +50%, 10s)와 중첩되면 최대 0.137s × (1/1.5) = 0.091s 쿨다운까지 내려갈 수 있다 — Frenzy 는 별도 `EMonsterBuff` 표면이라 상한 없음.

6. **종료·해소 조건**
   `_cdMul` 의 공속 버프는 라운드 종료(승리 또는 패배) 시 사라진다. 단 라운드 안에서는 영구 적용되어 해소 수단이 없다. 영웅이 사망하면 승리로 라운드가 끝나 자동 해소된다. Dps 빌드 의도대로 영웅을 빠르게 처치하면 이 과부하가 실제 문제가 되지 않는다는 시나리오도 있다.

7. **다른 시스템과 상호작용**
   Dps Tier1(Power ×1.3)과 동시 적용 시 Reaper 단위 DPS = `(6 × 1.3) / 0.137 ≈ 57 DPS` 로 기본(12 DPS)의 4.75배. `SpawnReapers`(+1 출력) 적용 시 동시 2마리 기준 114 DPS. 영웅 HP 4000 기준 순수 Reaper DPS 만으로 약 35s 이내 처치가 가능하다. `MarkOfDeath` 액티브(받피 ×1.5, 5s) 와 동시 적용 시 5초 창 내 Reaper DPS 는 `57 × 1.5 = 85.5 DPS/마리` 로 상승한다. 또한 `Spawner.ScalePeriod()` 에는 `Mathf.Max(0.05f, period)` 클램프가 이미 있지만 `AttackerComponent` 의 쿨다운에는 유사 클램프가 없어 계층 간 정책 불일치가 생긴다.

8. **엣지 케이스**
   `Frenzy` 액티브를 별도 픽하면 추가 ×0.67 쿨다운 배율이 곱해져 최악 케이스에서 쿨다운이 0.09s 이하로 떨어질 수 있다. 이 경우 프레임 단위 공격 처리가 예상 외로 빠를 수 있고, `MonsterBuffService.AddBuff` 의 dedup 처리(Frenzy 는 시한 buff)가 중간에 갱신되면 공속 버프가 복합 층위로 쌓여 디버깅이 어려워진다. `HexRangeBoost` 카드와 달리 `ReaperAtkSpeed` 는 쿨다운에만 관여해 Reaper 의 Range(1.5)에는 영향이 없으므로, 빠른 Reaper 가 근접 거리를 유지하면서 연타하는 시각적 쏠림이 두드러질 수 있다.

9. **유저 정보·피드백**
   플레이어가 `ReaperAtkSpeed` 를 3픽까지 쌓았을 때 카드 픽 팝업의 `×3` 배지로 중첩 횟수를 확인할 수 있다. 빌드 패널의 Dps 아이콘 1·2·3개로 Tier 진행 상황을 실시간으로 파악하고 Tier2 도달 시 토스트가 표시된다. 그러나 현재 UI 는 **최종 쿨다운 수치(초)** 를 플레이어에게 직접 표시하지 않아, 빌드 강도가 예상보다 훨씬 높아졌음을 직관적으로 알기 어렵다. `×0.75` 로 조정해도 3픽+Tier2 최솟값(0.169s)은 기본 대비 여전히 3배 빠르므로 Dps 빌드의 정체성 유지에는 충분하다.

### 보류

- **BloodThirst (Dps A#6) 트리거 명확화**: "처치 시" 가 영웅이 몬스터를 죽일 때 발동(가장 합리적)인지 아닌지 확인 필요. 현재 코드 열람 없이는 정성 판단만 가능. 다음 감사에서 구현 확인 후 제안.
- **Dps Tier3 Range ×1.3 Reaper 근접 전환 리스크**: Reaper 기본 Range 1.5 × 1.3 = 1.95. 이 값이 근접 몬스터의 "공격 가능 거리" 판정에 어떤 영향을 주는지 AI 이동 패턴과 함께 검토 필요. 현재 우선순위 낮음 — Tier2 이슈가 선결.

---

## 3. 과거 감사 대비 차별성

- git log 조회 3건 검토 완료.
- 가장 유사했던 과거 커밋: e198aa4 (2026-06-06 KST) "Swarm Tier2×SpawnerHaste 복합 스택 — Nova 쿨 가드레일 재검토"
  - 차별점: ① 축이 다름(Swarm vs **Dps**). ② Swarm 건은 스포너 스폰 주기(period)의 과도 단축 이슈, 이번 건은 몬스터 개체 공격 쿨다운(attack cooldown)의 하한 부재 — 대상 스탯과 적용 레이어가 다름. ③ 이번 제안은 카드 SO 수치 단일 변경으로 해결 가능(구현비용 1), 이전 Swarm 건은 가드레일 로직 신설이었음.
- git log 외 폴더의 `2026-06-02-debuff-tier3-eternal-bleed-aura-balance.md` 존재 확인: Debuff Tier3·EternalBleedAura 주제 — 본 후보(Dps·ReaperAtkSpeed·Tier2)와 축·카드·효과 모두 다름. 중복 없음.

---

## 4. 제외 (범위 밖)

- **Hex에도 동일 Cooldown 중첩 적용**: Dps Tier2 는 Reaper·Hex 둘 다 적용. 그러나 Hex 기본 Cooldown = 1.0s 이고 HexRangeBoost 는 Range 만 강화(Cooldown 미관여). Hex 는 쿨다운 강화 카드가 없어 Tier2 단독 ×0.8 만 적용 → 1.0 × 0.8 = 0.8s. 이는 과도하지 않으므로 조정 불필요.
- **Frenzy 액티브 추가 중첩**: Frenzy 는 EMonsterBuff 시한 buff 로 전체 종에 적용. 이 중첩 자체를 막는 것은 범위 밖 — Frenzy 의 역할과 충돌. 단 Frenzy 가 Reaper 최악 케이스를 0.09s 이하로 악화시키는 점은 §1 QA 권고 미해결 목록에 기록하고 qa-simulator 복구 후 재검토.
- **신규 몬스터 종 추가**: 컨셉 §11.2 MVP 제외 항목.
- **ReaperAtkSpeed 카드 삭제 또는 장수 변경**: 매수 lock(패시브 16장 고정) 위반.

---

## 5. 다음 단계 제안

- 채택 시 **game-designer** 에게 정식 기획 요청.
  - 입력: 본 문서 + `card-renewal.md` §3.2(ReaperAtkSpeed 중첩) + `continuous-spawn-round.md` §4(기본 스탯)
  - 결정 항목: ① `_cdMul` 최종값 (×0.75 제안 or 대안 검토) ② `AttackerComponent` 최소 쿨다운 클램프(0.1s 또는 0.15s) 신설 여부 ③ BalanceConfig 에 `MinAttackCooldown` 손잡이 추가 여부
- 채택 시 **gameplay-programmer**: SO `_cdMul` 수정 + (선택) AttackerComponent 클램프 1줄 추가.
- **qa-simulator**: DebugAutoPicker 훅 구현 후 첫 시뮬 목표 메트릭에 "Dps 7장 빌드 평균 사망 시각" 포함 요청.

---

## 6. 쉬운 설명 (비개발자 요약)

게임에서 리퍼(Reaper) 는 칼을 빠르게 휘두르는 빨간 몬스터입니다. 지금 게임 규칙상, "리퍼 공격속도 올리기" 카드를 3번 뽑고 나서 "Dps 시너지 2단계" 보너스까지 받으면 리퍼가 1초에 7번 이상 공격하는 상태가 됩니다. 일반적인 칼싸움 게임에서 1초에 7번은 극단적으로 빠른 속도라, 실제로는 영웅을 30초 안에 해치울 수 있어 목표(2~4분 안에 처치)보다 훨씬 일찍 끝나 버립니다. 이걸 조금 늦추기 위해 카드 1장의 숫자를 아주 조금 올리는(×0.7 → ×0.75) 제안입니다. 바꾼 뒤에도 리퍼는 여전히 3배 가까이 빨리 공격하므로 "빠르게 깎는다"는 Dps 빌드 느낌은 그대로 살아있습니다. 그래서 이번에 제안하는 것은: 리퍼 공격속도 카드의 배율을 ×0.7에서 ×0.75로 한 단계 줄여 가장 강한 Dps 빌드가 지나치게 빨리 끝나는 문제를 완화하는 것입니다.
