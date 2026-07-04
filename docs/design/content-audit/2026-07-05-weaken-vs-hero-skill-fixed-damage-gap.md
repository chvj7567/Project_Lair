# Content Audit — 2026-07-05 — Weaken 카드 영웅 스킬 도입 후 실효성 급감 — WeakenFactor 손잡이 미설계

> 작성: Daily Content Audit 루틴 (자동)
> 날짜: 2026-07-05 (KST)
> 입력: 컨셉서 v0.7 · card-renewal.md · hero-skills.md · QA 리포트 1건 · 과거 감사 21건

---

## §0 입력 스냅샷

| 항목 | 값 |
|---|---|
| 컨셉서 버전 | v0.7 (`docs/design/project_lair_concept.md`) |
| 참조 기획서 | card-renewal.md, hero-skills.md |
| 참조 spec/plan | 32건+ (`docs/superpowers/specs/`, `docs/superpowers/plans/`) |
| QA 리포트 | 1건 — 2026-05-22 BLOCKED (DebugAutoPicker 훅 미구현, 시뮬 미실행) |
| 과거 감사 이력 | 21건 (git log `# [Routines][Daily Content Audit]` 조회 기준) |
| 최근 감사 | 2026-07-04 KST — HexRangeBoost 영웅 AI 회피 설계 부재 |
| 오늘 감사 날짜 | 2026-07-05 KST |

---

## §1 현황표

### 영웅

| 항목 | 컨셉 목표 | 현재 구현 |
|---|---|---|
| 영웅 종류 | 1 (Knight) | 1 (Knight) |
| 영웅 스킬 페이즈 | 3 (HP 85%/65%/45%) | 3 구현 완료 |

영웅 HP = 4000 (BalanceConfig.asset 실측). Hero Power = 50/hit, Cooldown = 1.0s.
스킬: DashStrike(P1@85%, `_damage=80`), AoeNova(P2@65%, `_damage=100`), OrbitingBlade(P3@45%, `_damage=15`×다중타격).
**핵심**: 스킬 `_damage` 필드는 ScriptableObject 고정값 — Hero Power 스탯에서 독립.

### 몬스터

| 항목 | 컨셉 목표 | 현재 구현 |
|---|---|---|
| 종류 수 | 6 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) |

### 카드

| 축 | 패시브 | 액티브 | 컨셉 목표 |
|---|---|---|---|
| 탱크 | 4 | 3 | 4P+3A |
| DPS | 4 | 3 | 4P+3A |
| 디버프 | 4 | 3 | 4P+3A |
| 스웜 | 4 | 3 | 4P+3A |
| **합계** | **16** | **12** | **28** |

미구현: SwarmRush (향후 계획, 컨셉 §11.3 명시).
QA: BLOCKED (DebugAutoPicker 훅 대기 중, qa-reports/2026-05-22.md).

---

## §2 후보 카드 — Weaken (디버프 액티브 #7)

### 2.1 카드 개요

| 항목 | 값 |
|---|---|
| ECardId | `Weaken` |
| 설계 파일 | card-renewal.md §3.3 디버프 액티브 #7 |
| 설계 시점 | 2026-05-31 (card-renewal.md 작성일) |
| 효과 클래스 | `WeakenEffect` |
| 핵심 필드 | `_factor=0.5`, `_duration=10` |
| 설계 의도 | 영웅 PowerScale ×0.5 (10초) → 영웅 근접 데미지 -50% |
| 지속시간 누적 | 중첩 시 타이머 연장 (리셋 방식) |

### 2.2 문제 — 영웅 스킬은 PowerScale 와 무관

hero-skills.md §0 명시:

> "스킬 데미지 수치(§2 수치표)와 몇 타에 어느 종이 죽는가(§3)는 **몬스터 HP에만 의존한다**."

즉 `DashStrike._damage=80`, `AoeNova._damage=100`, `OrbitingBlade._damage=15` 는 게임 중 영웅 Power 스탯이 얼마이든 변하지 않는 고정값이다. `WeakenEffect` 가 줄이는 것은 Hero PowerScale — 영웅의 **평타 데미지**에만 적용된다.

결과적으로:
- HP 100%~85% (P1 활성 전): Weaken = 평타 데미지 ×0.5 → **실효 감소 100%** (평타만 존재)
- HP 85% 이후 (DashStrike 활성): Weaken = 평타 ×0.5, DashStrike 영향 없음
- HP 65% 이후 (AoeNova 추가): 영웅 딜의 DashStrike+AoeNova 비중 ↑, Weaken 실효성 ↓
- HP 45% 이후 (OrbitingBlade 추가): 전 스킬 활성, 전투 극후반 → 스킬 딜 비중 최고, Weaken 실효성 **최저**

Weaken은 플레이어가 점수를 내야 하는 후반부(HP 낮을수록 스킬 누적)에 오히려 가장 쓸모없어진다.

### 2.3 타이밍 분석

액티브 트리거: 30초마다. Weaken 지속시간 = 10초. 재트리거 간격(30s) > 지속시간(10s).
→ Weaken을 매 액티브 트리거마다 계속 골라야 10초씩 끊어 유지. **"영구 유지" 불가**.
→ 다른 액티브 카드를 고르면 Weaken 공백이 20초 발생.

한편 HeroAttackDown (디버프 패시브 #4, `_factor=0.75` 영구 누적)은 조건 없이 항시 적용.
→ 영구 패시브 HeroAttackDown 대비 Weaken의 "10초 타임드" 구조는 리워드 대비 코스트가 높음.

### 2.4 WeakenFactor 손잡이 미설계

`WeakenEffect._factor` 는 SO 직렬화 필드지만 **BalanceConfig.asset 에 노출되어 있지 않다**.
밸런스 조정 흐름(project.md §밸런스 조정 흐름)은 게임-디자이너 → gameplay-programmer 통해 SO 직접 수정이 필요 — 빠른 반복 튜닝 불가.

만약 Weaken에 스킬 데미지 감소 효과(예: `_skillFactor`)를 추가한다면:
- `WeakenEffect.cs` 수정 필요
- SO에 필드 추가 필요
- BalanceConfig 손잡이 추가 권장

### 2.5 4축 점수

| 축 | 점수 (1~5) | 근거 |
|---|---|---|
| 검증가치 | 5 | 스킬 도입 전 설계된 카드가 스킬 도입 후 약해졌는지 검증 — 밸런스 정합성 직결 |
| 구현비용 | 3 | WeakenEffect.cs 필드 추가 + BalanceConfig 손잡이 (~30줄 예상). gameplay-programmer 필요 |
| 시너지폭 | 3 | 디버프 축 Tier2(HeroAttackDown 자동등록), Tier3(영구 출혈)와 결합 시 복합 감소 가능 |
| 데이터근거 | 5 | hero-skills.md §0 + §2 수치표, card-renewal.md §3.3 #7 = 직접 인용 가능한 명시적 근거 |
| **종합** | **16 / 20** | `5 + (6−3) + 3 + 5 = 16` |

### 2.6 9가지 유저 플로우

1. **플레이어가 Weaken을 처음 고를 때**: 전투 초반(30s 경과)에 고르면 아직 스킬 없거나 P1만 있음 → 평타 감소 효과 체감 가능. 팝업 설명 "영웅 데미지 -50% (10초)"를 보고 강력하다고 인식.

2. **Weaken 지속 중 스킬 발동 순간**: DashStrike가 뚫고 들어와 Wisp 무리를 쓸어버림. 플레이어는 "Weaken이 있는데도 스킬은 막히지 않는다"는 것을 직관적으로 느낄 수 있음 — 하지만 UI에 아무 피드백 없음.

3. **10초 후 Weaken 만료**: 영웅 평타가 원래대로 돌아옴. 플레이어 입장에서 변화 체감 어려움 — 스킬은 계속 고정 데미지.

4. **30초 뒤 다음 액티브 트리거**: Weaken을 다시 골라 재유지할지, 다른 카드를 고를지 선택 압박. 지속 유지 비용이 높음 (매 트리거 1슬롯 소비).

5. **HP 65% — AoeNova 추가**: 광역 폭발이 고정 100 데미지를 뿌림. Weaken 중이어도 AoeNova 데미지 그대로. 몬스터가 폭발에 녹는 것을 보고 "Weaken이 뭔가 기여하는 건가?" 모호함.

6. **HP 45% — OrbitingBlade 추가**: 회전 칼날 `_damage=15`×`3개`×빠른 `_hitInterval=0.3`. 이 시점 총 DPS의 상당 부분이 스킬. Weaken의 평타 기여 비중 최저.

7. **디버프 Tier2 달성 (5픽)**: HeroAttackDown 자동 등록(영구 ×0.75 감소). Weaken(×0.5, 10초) + HeroAttackDown(×0.75, 영구) 동시 적용 시 평타 ×0.375. 하지만 스킬은 여전히 고정값 — 시너지가 평타에만 쏠림.

8. **디버프 Tier3 달성 (7픽)**: 영구 출혈 (이동 시 1초당 HP -1%). Weaken이 출혈 데미지에 관여하는지 명시 없음(관여 안 함). Tier3 조건 달성에 Weaken 7픽이 필요하지만, Weaken 자체 실효성은 여기서 가장 낮음.

9. **전투 종료 직전**: 영웅 HP가 20%~0% 구간 — 3종 스킬 전부 활성, 스킬 딜이 지배. Weaken을 보유하든 아니든 결과 차이가 미미. "Weaken 이 시점엔 의미가 없었다"는 사후 감각.

---

## §3 과거 감사 21건 대비 차별성

| 감사 날짜 (KST) | 슬러그 | Weaken 관련 여부 |
|---|---|---|
| 2026-05-28 ~ 2026-07-04 | (21건 전체) | 없음 |

과거 21건에서:
- Weaken 카드를 단독으로 분석한 감사 없음.
- 영웅 스킬 도입(hero-skills.md, 2026-06-04) 이후 기존 카드 실효성 변화를 다룬 감사 없음.
- 디버프 축은 `2026-06-08-dps-reaper-cooldown-tier2-floor.md` 등 DPS 연계 분석이 주였으며, 디버프-스킬 교차 분석은 없음.

→ 이번 감사는 **설계 시점 vs. 도입 시점 불일치로 인한 실효성 역전** 이라는 새 각도.

---

## §4 구현 방향 (참고 — gameplay-programmer 판단 영역)

**Option A — WeakenEffect 스킬 데미지 감소 확장** (권장)
- `WeakenEffect.cs` 에 `[SerializeField] private float _skillFactor = 1.0f` 추가
- 영웅 스킬 실행 시 적용 가능한 인터페이스/훅 경유 (Hero PowerScale 과 분리된 별도 배율)
- BalanceConfig.asset 에 `WeakenFactor`, `WeakenSkillFactor` 손잡이 추가
- 구현비용: ~30~50줄. code-reviewer + test-engineer 필요.

**Option B — 지속시간 연장만** (최소)
- `_duration=10` → `_duration=30` (액티브 트리거 주기와 동기)
- 스킬 실효성 격차는 해소 안 됨, 하지만 "매 트리거 재투자" 문제는 해소
- 구현비용: SO 필드 수정 1줄. 밸런스 효과는 제한적.

**Option C — 설계 보류 (데이터 우선)**
- QA 시뮬레이터 BLOCKED 해소 (DebugAutoPicker 훅 구현) 후 실측 데이터 확보
- Weaken 픽률/승률 데이터 확인 후 판단
- qa-reports/2026-05-22.md §5 참조

---

## §5 다음 단계

1. **사용자 / game-designer**: Option A/B/C 결정
2. **gameplay-programmer** (A 선택 시): WeakenEffect 확장 + BalanceConfig 손잡이 추가
3. **code-reviewer**: 변경 검토
4. **test-engineer**: 회귀 테스트 (WeakenEffect 기존 평타 감소 동작 보존 확인)
5. **qa-simulator** (BLOCKED 해소 후): Weaken 유무 전략 비교 시뮬

---

## §6 쉬운 설명

Weaken 카드는 "영웅을 잠깐 약하게 만드는" 카드입니다. 그런데 영웅이 HP가 낮아질수록 특수 스킬(DashStrike, AoeNova, OrbitingBlade)을 더 많이 쓰는데, 이 스킬들의 데미지는 Weaken의 영향을 **전혀 받지 않습니다.** 결국 플레이어가 "이거 강하겠다!"하고 고른 Weaken이, 전투가 후반으로 갈수록 점점 아무 효과가 없는 카드로 변해버립니다.

그래서 이번에 제안하는 것은: **Weaken이 스킬 데미지도 함께 줄이도록 확장하거나, 적어도 지속시간을 늘려서 유지 비용을 낮추는 것**입니다.
