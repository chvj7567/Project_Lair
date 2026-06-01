# Content Audit — 2026-06-02 — Debuff Tier3 EternalBleedAura 효과량 상향 (-1%/s → -1.5%/s)

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷

- 컨셉서 버전: v0.6 (2026-05-31 카드 전체 리뉴얼 반영)
- 참조 spec/plan 수: 12개 (`docs/superpowers/specs/` 11개 + `docs/superpowers/plans/` 12개)
- 참조 QA 리포트 수: 6개 (최신: 2026-05-26 6차)
- 과거 감사 이력 (git log): 5건 (가장 최근: 2026-06-01 KST, SHA cce7243)

## 1. 현황

### 컨셉 §11 대비 에셋 현황

| 카테고리 | 컨셉 §11 목표 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 없음 ✅ |
| 몬스터 | 6종 | 6 (Wisp·Wraith·Reaper·Hex·Plague·Phantom) | 없음 ✅ |
| 패시브 카드 | 16장 (v0.6: 4축×4) | 16 (전 28장 SO 완비) | 없음 ✅ |
| 액티브 카드 | 12장 (v0.6: 4축×3) | 12 (전 28장 SO 완비) | 없음 ✅ |
| 시너지 Tier 시스템 | 12 Tier 효과 (4축×Tier1/2/3) | 설계 완료(`card-renewal.md §4`) · QA 미검증 | QA 7차 미실행 |

### 계획 있으나 미구현 / 미검증

- **QA 7차 미실행**: 카드 리뉴얼 v0.6 (2026-05-31) 이후 4축 시너지 Tier 효과 전체가 QA로 검증된 바 없음. 특히 Tier3 효과(Tank 캡+6 / Dps Range×1.3 / Debuff EternalBleedAura / Swarm 스포너출력+1) 는 수치 설계는 완료됐으나 실측 데이터 0.
- **EternalBleedAura 구현 여부 미확인**: `Assets/_Lair/Scripts/Card/Effects/` 내 `EternalBleedAura.cs` 또는 유사 파일이 별도 확인되지 않음 (`card-renewal.md §10.2` 는 신규 IBattleContext 표면 필요 명시). Tier3 발화 로직이 `BuildSynergyService.cs` 등 다른 위치에 있을 수 있으나, 본 감사 범위에서 직접 확인 불가.

### QA 권고 미해결

| QA 기준 | 목표 | 최신 결과 (6차, HP 4000 기준) | 현 상태 |
|---|---|---|---|
| ① 1라운드 회귀 방지 | 전판 ≥30s | 최솟값 64.93s | ✅ 통과 |
| ② 액티브 카드 발화 | ≥1픽 | 전판 ≥30s 초과 | ✅ 통과 (추정) |
| ③ 평균 사망 ≥80s | ≥80s | 76.04s (HP 4000 기준) | ⚠️ HP 4600 적용 후 미재측 |
| ④ 5분 타임오버 ≥1판 | ≥1판 | 0판 | ❌ 미달 (v0.6 후 미재측) |
| ⑤ 클리어율 ≤80% | ≤80% | 100% | ❌ 미달 (v0.6 후 미재측) |

> ④⑤ 해결 경로: `card-renewal.md §9.7` 은 v0.6 4축 시너지가 "분포 분산을 처음으로 확장"해 타임오버 가능성을 연다고 가설화. QA 7차 미실행으로 미검증.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 (KST) | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-01 | cce7243 | 패시브 카드 재조정 — SpawnerHaste 중첩 상한 3픽 캡 도입 (Swarm 패시브) |
| 2026-05-31 | 666d39f | BalanceConfig 영웅 HP 조정 — 4000→4600 (QA 6차 권고 ③ 통과 목표) |
| 2026-05-30 | 531ad9d | 패시브 카드 실효성 회복 — Plague 스포너 배치로 SpawnPlagues·PlagueSlowBoost 활성화 |
| 2026-05-29 | 586dfde | 저주 카드 4종 효과값·지속시간 재조정 — Fear·Bleed·Weaken·Slow 픽률 하위권 해소 |
| 2026-05-28 | 2c53f3e | BalanceConfig 손잡이 추가 — 스폰 주기 배율 (분모 변수 투입) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Debuff Tier3 EternalBleedAura 효과량 상향 — 이동 시 HP -1%/s → -1.5%/s

- **카테고리**: 패시브 카드 (시너지 Tier3 효과값 재조정)
- **요지**: Debuff 7장 임계 시너지인 EternalBleedAura의 영구 출혈 효과를 이동 시 HP -1%/s에서 -1.5%/s로 강화해 7장 Debuff 빌드의 "진성 빌드 보상"으로서의 실효성을 높인다. QA ④⑤(타임오버 0판, 클리어율 100%) 해결을 위한 분포 분산 확장의 핵심 경로 중 하나다.
- **검증/구현/시너지/데이터**: 4/2/4/3 → 종합 **15**
  - 검증가치 4: QA ④⑤ 미해결이 가장 큰 현안이며, Tier3 EternalBleedAura는 강력한 Debuff 빌드를 통한 분포 분산 확장의 핵심 메커니즘.
  - 구현비용 2: MonsterBuffService 상수 또는 Tier 설정 수치 1개 변경. 이미 설계된 메커니즘의 파라미터 조정.
  - 시너지폭 4: 영웅 이동 패턴 × Plague 둔화(SlowFactor) × 출혈 복합. Debuff Tier1(둔화) + Tier2(HeroAttackDown) + Tier3(영구 출혈)의 상승 강화 구조와 직결.
  - 데이터근거 3: QA 6차 ④⑤ 직접 미달 + "분포 분산 확장에는 분모 변수 투입 필수" 권고(`docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md §6.3`).
- **근거**:
  - `docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md §6.3` — ④⑤ 해결은 분포 분산 확장이 필요, HP 단독으로는 불가.
  - `docs/design/card-renewal.md §4.2` — Debuff Tier3 = "영구 출혈 등록 — 영웅 이동 시 1s당 HP -1%, 라운드 끝까지".
  - `docs/design/card-renewal.md §9.7` — "본 기획이 처음으로 분포 분산 확장을 만든다 (약축 분산 픽 빌드는 사망이 늦어지고 일부(7장 임계 빌드)는 빨라진다)".
  - `docs/design/card-renewal.md §4.3` — "Tier3 의도: 진성 빌드 보상. 평균 사망 추가 5~8s 단축 (66s → 58~60s)".
- **MVP 범위**: 컨셉 §11.2 "패시브 선택지(15→16장)" 내 시너지 수치 조정 — 카드 매수 불변, 효과량만 변경.

#### 유저 플로우 (9개 항목)

1. **노출 시점·트리거**
   Debuff 축(보라, `#A855F7`) 카드를 패시브·액티브 합산 7번 픽하는 순간 Debuff Tier3 시너지가 즉시 발화한다. 전형 루트는 패시브 4픽(PlagueSlowBoost·SpawnPlagues·HeroPoisonAura·HeroAttackDown) + 액티브 3픽(Fear·Bleed·Weaken). 7번째 픽 확정 시 `BuildSynergyService` 가 임계 도달을 감지하고 `IBattleContext.ApplyHeroAura(EternalBleedAura, duration=-1f)` 를 호출해 영구 출혈 오라를 등록한다. 같은 카드 반복 픽도 빌드 카운트에 누적되므로 PlagueSlowBoost 7픽 단독으로도 도달 가능하다(`card-renewal.md §4.1`).

2. **화면 변화**
   카드 픽 팝업 상단 Debuff 빌드 카운트 셀이 "7+ ■■■"(Tier3 도달, 보라 `#A855F7` 색)으로 갱신된다. 동시에 화면 중앙 상단에 "Debuff 시너지 Tier3 발동!" 보라색 토스트가 1.5초 표시된다(`card-renewal.md §8.4`). 팝업이 닫히면 영웅 몸체에 진빨강 출혈 시각(`#991B1B`, 컨셉 §11.4) 이 영구히 표시된다. 영웅 HP 바의 하락 속도가 이동 중 육안으로 식별 가능한 수준으로 빨라진다(-1%/s → -1.5%/s 상향 시 50% 더 빠른 감소).

3. **입력 행동**
   플레이어(던전 주인)는 별도 조작 없이 자동 발화를 확인한다. Tier3 발화 후 남은 픽(최대 11픽 = 패시브 5 + 액티브 6 잔여)으로 Debuff 빌드를 심화(같은 카드 반복 픽으로 Layer 2 강화)하거나 다른 축 카드를 교차 픽해 보조 시너지를 노릴 수 있다. Tier3 자체는 발화 후 추가 입력 없이 라운드 끝까지 작동한다.

4. **시스템 반응**
   EternalBleedAura가 영웅에 등록된 후, 매 Update 프레임마다 영웅의 이동 속도(`SimpleMover.CurrentSpeed`)가 0보다 크면 `ratio × deltaTime × heroMaxHp` 의 HP 손실이 적용된다. 현재 설계값(-1%/s = ratio 0.01): HP 4600 기준 이동 중 -46 HP/s. 제안값(-1.5%/s = ratio 0.015): -69 HP/s. EternalBleedAura는 기존 Bleed 액티브 카드의 BleedAura(`ratio=0.02, duration=10s`)와 독립 스택되어 동시에 두 효과가 적용된다. 영구(-1f) 등록이므로 `HeroAuraService` 의 duration 만료 체크에 걸리지 않는다.

5. **반복·재발생 패턴**
   같은 임계(7장)는 라운드당 1회만 발화한다(`card-renewal.md §4.1`). 이미 EternalBleedAura가 등록된 상태에서 8번째·9번째 Debuff 픽이 들어와도 재발화 없음. 단 Bleed 액티브 카드를 별도 픽하면 BleedAura(-2%/s, 10s)가 추가로 중첩되어 일시적으로 총 -3.5%/s(이동 100% 기준 = 이동 70% 기준 -2.45%/s)까지 누적된다.

6. **종료·해소 조건**
   EternalBleedAura는 영웅 HP 0(승리) 또는 5분 타임오버(패배) 시 라운드 종료와 함께 소멸한다. 라운드 중 해소 조건 없음 — 영구 등록(`duration = -1f`). 영웅이 완전히 정지한 순간(CurrentSpeed = 0)에만 손실이 멈추지만, 영웅 AI(가장 가까운 몬스터로 자동 이동·공격) 특성상 완전 정지 구간은 실질적으로 매우 짧다.

7. **다른 시스템과 상호작용**
   - **Plague 둔화(SlowFactor)**: Plague Spawner 상시 가동 + PlagueSlowBoost 픽 시 영웅 이동속도 ×0.6(2픽 ×0.45). 이동 속도가 낮아져도 "이동 중(CurrentSpeed > 0)" 조건은 유지되므로 EternalBleedAura는 계속 발동한다. 이동 속도가 낮아지면 몬스터를 향한 1회 이동의 지속 시간이 길어져 총 이동 비율은 유사하거나 증가한다.
   - **HeroAttackDown Tier2 자동 등록(×0.85 영구)**: 영웅 공격력 하락 → 몬스터 처치 시간 증가 → 이동 횟수 증가 → EternalBleedAura 발동 빈도 상승. Debuff 빌드 내 세 레이어가 상승 강화 구조를 형성한다.
   - **Fear 액티브 카드(3s 도주)**: 영웅 도주 중 이동 = EternalBleedAura 발동. Fear가 EternalBleedAura 추가 발동 기회를 만드는 의도치 않은 시너지.
   - **Slow 카드(Swarm A, 영웅 ×0.5 + 몬스터 ×1.3)**: Slow로 영웅 이동속도 ×0.5 → EternalBleedAura 발동 조건 유지. 몬스터 ×1.3 이속은 별개 효과.

8. **엣지 케이스**
   - **영웅 완전 정지**: 몬스터가 영웅과 밀착 공격 중 `SimpleMover` 가 목표 좌표를 현재 위치 근처로 설정하면 실질적 CurrentSpeed≈0이 되어 EternalBleedAura 손실이 0에 수렴한다. Debuff Tier3 진입 후 영웅 주변이 몬스터로 완전 포위된 경우 이 엣지가 발생 가능 — QA 7차에서 Debuff-focus 전략으로 포위 시나리오 실측 필요.
   - **EternalBleedAura + Bleed 중첩 과다**: -1.5%/s(Tier3) + -2%/s(Bleed 카드, 10s) = 최대 -3.5%/s. 이동 70% 기준 -2.45%/s = HP 4600에서 실효 약 112.7 HP/s. 영웅 잔여 HP가 30%(1380)일 때 약 12.2s 만에 사망. Bleed 카드 지속시간(10s) 내 처치 가능 = 극단 시나리오로 밸런스 모니터링 필요.
   - **PlagueSlowBoost 7픽 단독 극단 빌드**: Layer 2 곱연산 ×0.75^7 ≈ ×0.089 → 영웅 이동속도 8.9%(거의 정지). EternalBleedAura 실효 손실이 거의 0. 그러나 이 빌드는 Plague 둔화가 매우 강해 몬스터 DPS가 충분히 보완한다고 가정 — 이 경우 Tier3 효과 기여 부재가 7장 빌드를 약하게 만드는 모순 발생. 별도 설계 검토 필요.

9. **유저 정보·피드백**
   현재 피드백 표면: (a) 픽 팝업 Debuff 셀 "7+ ■■■", (b) 토스트 1.5s, (c) 영웅 진빨강 출혈 시각(`#991B1B`). **개선 여지**: EternalBleedAura(영구 출혈)와 Bleed 카드(임시 출혈)의 시각 구분이 없다 — 두 효과가 동시에 걸리면 플레이어가 어느 것이 영구인지 식별 불가. 제안: 영구 출혈 발동 중 영웅 우상단에 "∞" 아이콘 상시 표시, 임시 출혈은 기존 진빨강만 유지.

### 보류

| 후보 | 종합 | 보류 사유 |
|---|---|---|
| Tank Tier1 HP×1.3 수치 재검토 (WispHpBoost 복합) | 14 | QA 6차 픽률 데이터가 구버전 몬스터명(Slime)이라 직접 근거 부족. v0.6 이후 첫 QA에서 Tank 픽률 확인 후 재검토 권장. |
| Slow(던전의 점성) 이중효과 밸런스 (영웅×0.5 + 몬스터×1.3) | 14 | 2026-05-29 저주카드 재조정 audit에서 Slow가 포함됐을 가능성. v0.6 리뉴얼 이후 Swarm 축 새 정체성 데이터 부족. SpawnerHaste cap(2026-06-01)과 동일 Swarm 축 → 7일 내 재검토 신중. |
| MarkOfDeath + BloodThirst 조합 처치 루프 | 11 | 신규 카드 2종 조합이라 QA 데이터 전무(근거 1점). v0.6 이후 첫 QA 결과를 보고 판단 권장. |

---

## 3. 과거 감사 대비 차별성

git log 조회 5건 검토 완료.

**가장 유사했던 과거 커밋**: 586dfde (2026-05-29 KST) — "저주 카드 4종 효과값·지속시간 재조정 (픽률 하위권 해소)"

**차별점**:
- 과거 audit (586dfde): Fear·Bleed·Weaken·Slow 등 **개별 액티브 카드**의 효과값·지속시간 조정 (카드 레벨 수치 변경).
- 이번 후보: Debuff **Tier3 시너지 임계 효과**의 수치 조정 (7장 누적 달성 보상 레벨 — 개별 카드와 별개 레이어).

다른 관련 과거 audit (531ad9d, 2026-05-30 KST "Plague 스포너 배치")는 Debuff 축 **인프라**(Plague 스포너 존재 여부) 레벨이었고, 이번 후보는 **밸런스 수치** 레벨이다.

세 레벨(인프라·카드개별·Tier시너지)은 서로 독립적인 설계 층이라 중복 간주하지 않는다.

---

## 4. 제외 (범위 밖)

- **QA 7차 즉시 실행**: 본 감사는 설계 제안을 담는다. QA 실행 결정은 사용자 및 qa-simulator 영역.
- **EternalBleedAura 구현(코드 작성)**: gameplay-programmer 호출 영역. 본 제안은 구현이 완료되거나 완료 예정임을 전제로 수치를 논한다.
- **Tier3 메커니즘 교체**: "영구 출혈" 대신 다른 효과로 변경하는 안은 v0.6 컨셉 결정(`card-renewal.md §4.2`)을 뒤집어야 하므로 game-designer 호출 후 별도 기획 필요 — 본 감사 범위 밖.
- **영웅 추가 / 몬스터 7번째 종 추가**: 컨셉 §11.2 범위 밖 (MVP 후 v0.2+).
- **신규 카드 매수 추가**: 패시브·액티브 매수는 잠금 상태 (16P + 12A = 28장 고정).
- **UI 색상·아트 개편**: CLAUDE.md §8 금지 (프리미티브 고정).
- **메타 진행 / 서버 연동**: CLAUDE.md §8 금지.

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청 — `card-renewal.md §4.2` Debuff Tier3 수치(`ratio=0.01f → 0.015f`) 갱신 + EternalBleedAura 구현 여부 gameplay-programmer 와 확인.
- EternalBleedAura 미구현 확인 시 gameplay-programmer 에게 `card-renewal.md §10.2` 신규 IBattleContext 표면 구현 요청.
- 구현 완료 후 qa-simulator 에게 QA 7차 요청 — Debuff-focus 전략(PlagueSlowBoost×4 + Fear+Bleed+Weaken 우선픽) 포함, EternalBleedAura 실효 손실량 및 Tier3 기여 평균 사망 단축 측정.
- 8번 엣지(완전 정지 시나리오)에 대한 별도 단위 테스트 검토.

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어는 던전 주인이 되어 영웅 한 명을 5분 안에 처치해야 한다. 카드를 골라 몬스터를 강화하는 건데, 같은 종류의 카드를 7장 이상 고으면 특별한 보너스(시너지)가 생긴다. "Debuff(방해)" 방향으로 카드를 7장 모으면 영웅 몸에 영구 출혈을 걸 수 있다 — 영웅이 걷기만 해도 조금씩 피가 깎인다. 지금은 1초마다 피를 1%씩 깎는데, 이 정도면 7장이나 모은 보상치고 좀 약할 수 있다. 게다가 요즘 QA 데이터를 보면 5분이 다 돼도 영웅이 죽지 않는 상황이 하나도 없어서, 더 강한 압박이 필요한 상황이다. 그래서 이번에 제안하는 것은: 영구 출혈 효과를 1초당 1.5%로 살짝 높여서, "Debuff 7장" 빌드를 만들었을 때 영웅이 더 확실한 압박을 느끼게 하자는 것이다.
