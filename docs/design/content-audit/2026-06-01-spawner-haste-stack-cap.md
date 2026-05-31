# Content Audit — 2026-06-01 — SpawnerHaste 패시브 중첩 상한 3픽 캡 도입

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.6 (2026-05-31)
- 참조 spec/plan 수: 12개 (docs/superpowers/specs/ 12개 · docs/superpowers/plans/ 12개)
- 참조 QA 리포트 수: 5개 (최신: 2026-05-26, 6차 — `2026-05-26-continuous-spawn-6th-validation.md`)
- 과거 감사 이력 (git log): 4건 (가장 최근: 2026-05-30)

---

## 1. 현황

| 카테고리 | 컨셉 §11.3 기준 (v0.6) | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 (Knight) | 1 (`Knight.prefab`) | 0 |
| 몬스터 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 6 (`Characters/` 프리팹 6종 + Knight) | 0 |
| 패시브 카드 | 16 (4축 × P4) | 16 (`Cards/Items/` SO 확인 — WispHpBoost·WraithDamageBoost·SpawnWraith·ReplaceWispsToWraith·ReaperAtkSpeed·HexRangeBoost·SpawnReapers·ReplaceReapersToHex·PlagueSlowBoost·SpawnPlagues·HeroPoisonAura·HeroAttackDown·PhantomMoveSpeedBoost·SpawnPhantoms·SpawnWisps·SpawnerHaste) | 0 |
| 액티브 카드 | 12 (4축 × A3) | 12 (`Cards/Items/` SO — IronWill·WallOfWisps·Berserk·Frenzy·BloodThirst·MarkOfDeath·Fear·Bleed·Weaken·TimeStop·Multiply·Slow) | 0 |

> **참고**: 컨셉 §11.2 표에는 "패시브 15장·액티브 10장" 이 남아있으나, §11.3 v0.6 갱신에서 28장(16P+12A)으로 실질 확정됨. §11.2 표 업데이트 누락 — 문서 불일치 주의.

### 계획 있으나 미구현

| 항목 | 출처 | 상태 |
|---|---|---|
| QA 7차 검증 시뮬 | `card-renewal.md` §9.7 — "QA 7차에서 검증" 항목 3개 (SpawnerHaste 포화·Fear/TimeStop 픽률·④⑤ 통과 여부) | 미실시 |
| SpawnerHaste 중첩 캡 (사전 의도 (a)) | `card-renewal.md` §9.6 — "QA 7차 데이터로 실제로 캡 포화 5초가 발생할 때 발동" | specs/plans 없음, 설계 노트에만 존재 |
| BuildSynergyPanel (좌측 상시 4축 표시) | `card-renewal.md` §8.0 | 구현 여부 미확인 (v0.6 spec 2026-05-31) |

### QA 권고 미해결

| 기준 | QA 6차 결과 | 후속 조치 | 현재 상태 |
|---|---|---|---|
| ③ 평균 사망 ≥80s | 76.04s ❌ | HP 4000→4600 (감사 2026-05-30 적용) | QA 7차 미검증 |
| ④ 5분 타임오버 ≥1판 | 0판 ❌ | `card-renewal.md` §9.7: 빌드 분산 확장으로 접근 예정 | QA 7차 미검증 |
| ⑤ 클리어율 ≤80% | 100% ❌ | 동상 | QA 7차 미검증 |

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-05-30 | 666d39f | BalanceConfig 영웅 HP 조정 (4000→4600, QA 6차 권고 ③ 통과) |
| 2026-05-29 | 531ad9d | 패시브 카드 실효성 회복 (Plague 스포너 배치로 SpawnPlagues·PlagueSlowBoost 활성화) |
| 2026-05-28 | 586dfde | 저주 카드 4종 효과값·지속시간 재조정 (픽률 하위권 해소) |
| 2026-05-28 | 2c53f3e | BalanceConfig 손잡이 추가 (스폰 주기 배율) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### SpawnerHaste 패시브 카드 — 중첩 상한 3픽 캡 도입

- **카테고리**: 패시브 카드 효과값 재조정 (중첩 누적 상한 추가)
- **요지**: `SpawnerHaste` (모든 스포너 주기 ×0.8 영구, Swarm 축 패시브) 의 곱연산 누적을 **3픽까지만 허용**하고 4픽 이상은 Layer 1 시너지 카운트만 누적한다. `card-renewal.md` §9.6 에서 사전 의도 (a)로 미리 설계됐으나 QA 7차 데이터 대기 상태인 조정안을 QA 이전에 선제 적용하는 제안이다.
- **검증/구현/시너지/데이터**: 4/1/4/5 → 종합 **18**
- **근거**: `docs/design/card-renewal.md` §9.6 (SpawnerHaste 중첩 위험 계산 + 사전 의도 (a) 명기) · `docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md` §6.3·§6.4 (④⑤ 미해결·분산 확장 필요)
- **MVP 범위**: 컨셉 §11.2 "패시브 카드 15→16장 매수 lock, 효과값 재조정 허용"

#### 유저 플로우

1. **노출 시점·트리거**  
   영웅 HP가 10% 감소할 때마다 패시브 카드 3장 선택지가 화면에 표시된다. `SpawnerHaste`(던전 박동)가 4번째 이상 선택지로 등장할 수 있으며, 이미 3픽 이상 누적된 경우 카드 우상단 배지가 `×3 MAX`로 표시된다. 3픽까지는 기존과 동일하게 동작한다.

2. **화면 변화**  
   4번째 이상 노출 시 SpawnerHaste 카드 배지 색상이 활성 색(Swarm 축 검정 `#1F2937`)에서 그레이 `#9CA3AF`로 변경되며 `×3 MAX` 텍스트가 표시된다. 카드 설명란 하단에 `중첩 최대 3회 (이후 카운트만 누적)` 한 줄이 추가된다. 카드 자체의 선택 가능 상태는 유지된다 (비활성 UI 아님).

3. **입력 행동**  
   플레이어가 4번째 이상의 SpawnerHaste를 선택한다. 선택 자체는 완전히 허용된다. 픽 애니메이션·카드 사운드 hook 없음(MVP §8)은 그대로 유지.

4. **시스템 반응**  
   카드 픽 카운트가 +1 증가하여 Swarm 축 Layer 1 시너지 카운트가 누적된다. 단, `IBattleContext.ScaleAllSpawnerPeriods(0.8f)` 호출이 **스킵**되어 스폰 주기 추가 단축은 발생하지 않는다. 3픽 완료 시점의 ×0.8³=×0.512가 이 카드로 도달 가능한 최소 스폰 주기 배율이 된다.

5. **반복·재발생 패턴**  
   라운드 내 SpawnerHaste는 이후에도 선택지에 계속 등장할 수 있다. 플레이어가 5번·6번·9번 픽해도 스폰 주기 효과는 추가되지 않고 Swarm 축 카운트만 오른다. Swarm Tier1(3장)·Tier2(5장)·Tier3(7장) 임계는 이 무효 픽도 카운트에 포함되므로 SpawnerHaste를 반복 픽하여 Tier를 빠르게 달성하는 전략이 계속 유효하다.

6. **종료·해소 조건**  
   라운드 종료(승리 또는 5분 타임오버)와 동시에 모든 스폰 주기 버프가 초기값으로 리셋된다. MVP 는 단일 런이므로 런 간 캐리오버 없음. 3픽 누적 상한은 런마다 새로 시작한다.

7. **다른 시스템과 상호작용**  
   Swarm Tier2 시너지(`ScaleAllSpawnerPeriods(0.85f)`)는 SpawnerHaste와 독립적으로 발화한다. SpawnerHaste 3픽(×0.512) + Swarm Tier2(×0.85) = 최종 ×0.435 → Phantom 스포너 6.0s×0.435≈**2.61s**. `card-renewal.md` §9.6 이 "허용 가능 범위 안"으로 분석한 수치이며 캡 도입의 목표는 이 수치 이하로 더 내려가는 것(4픽 이상 시 ×0.8⁴=×0.41 이하)을 차단하는 것이다. `SpawnPhantoms`·`SpawnWisps` 로 스포너 출력이 증가한 빌드에서도 주기 단축의 무한 누적이 차단되어 조합 전체가 허용 범위 내 유지된다.

8. **엣지 케이스**  
   SpawnerHaste 3번째 픽이 동시에 Swarm Tier1(3장) 발화 트리거가 되는 경우: Tier1 발동(Phantom·Wisp MoveSpeed ×1.3)과 캡 도달이 같은 픽 시점에 동시에 발생한다. 두 처리 순서 모두 독립적이므로 별도 가드 불요. 4번째 픽 시 SpawnerHaste 효과 없이 카운트 4가 되면 Tier1(3)·Tier2(5) 사이 진행 중으로 표시되며, 플레이어는 배지의 그레이아웃과 카운트 바로 4번째 픽이 "카운트는 올리지만 주기는 단축하지 않는 픽"임을 인지할 수 있어야 한다.

9. **유저 정보·피드백**  
   카드 설명란에 중첩 한도를 명시한다 (`중첩 최대 3회`). 빌드 카운트 바(`BuildSynergyPanel`)의 Swarm 카운트는 4번째 픽 이후에도 정상 증가하므로 플레이어는 "픽이 완전히 무효가 아님"을 시각적으로 확인한다. `×3 MAX` 배지는 "스폰 주기 효과"만의 상한을 표시하며, 시너지 카운트 기여가 지속됨을 배지 서브텍스트 또는 툴팁으로 전달할 수 있다 (MVP 비주얼 범위 내 텍스트).

---

### 보류

| 후보 | 보류 사유 |
|---|---|
| MarkOfDeath 효과값 검토 (Dps A 신규 카드 ×1.5 dmg·5s) | v0.6 신규 카드로 QA 데이터 전무 — QA 7차 후 수치 근거 확보 후 검토 |
| Debuff Tier3 영구 출혈 강도 재검토 (HP 4600 × 이동 1s당 −1%) | HP 4600 조정도 QA 7차 미검증 상태 — 스택된 변수 2개를 동시 조정하면 단일 손잡이 원칙 위반 |
| Tank Tier3 캡 +6(18→24) × Swarm 조합 분석 | §9.5에서 두 Tier3 동시 발동은 수학적으로 불가능함이 이미 분석됨 — 별도 조정 불요 |

---

## 3. 과거 감사 대비 차별성

git log 조회 4건 검토 완료.

가장 유사했던 과거 커밋: `2c53f3e` (2026-05-28) "BalanceConfig 손잡이 추가 (스폰 주기 배율)" — 차별점:
- **과거 (2c53f3e)**: 전역 `BalanceConfig.asset` 에 글로벌 스폰 주기 배율 손잡이 추가 — QA 시뮬레이터가 다양한 배율을 테스트할 수 있도록 하는 **운영 도구(tuning knob)**
- **오늘**: 개별 카드 `SpawnerHaste` 의 중첩 횟수 상한 추가 — 플레이어 빌드 안전망을 위한 **카드 설계 결정**. `card-renewal.md §9.6` 의 사전 의도 (a) 를 정식 기획 항목으로 끌어올리는 것.

동일 카테고리(스포너 주기 관련)이지만 목적·대상·메커니즘이 모두 다르다. `2026-05-28`은 전역 BalanceConfig, 오늘은 카드 개별 스택 캡.

---

## 4. 제외 (범위 밖)

- 신규 몬스터 종 추가: 컨셉 §11.2 "몬스터 6종 고정(MVP)"
- 카드 매수 변경 (28장 이상): §11.3 "4축 × 7장 = 28장" lock
- 메타 진행/서버/사운드/비주얼 아트: CLAUDE.md §8 금지 항목
- QA 7차 시뮬 실행: qa-simulator 영역 — 본 루틴 범위 외

---

## 5. 다음 단계 제안

- 채택 시 game-designer 에게 정식 기획 요청 (`SpawnerHasteEffect.cs` 의 픽 카운트 체크 추가 + SO 필드 `_maxStackEffect: 3` 명세 포함)
- 기획 확정 후 QA 7차 시뮬 진행 시 SpawnerHaste 캡 적용 상태에서 `④ 5분 타임오버 ≥1판` / `⑤ 클리어율 ≤80%` 검증 병행 권장
- QA 7차 이후에도 ④⑤ 미달이면 스폰 주기 전역 연장(`continuous-spawn-round.md §6.4 v10 후보`) 이 다음 밸런스 사이클 대상

---

## 6. 쉬운 설명 (비개발자 요약)

Project Lair 에서 `SpawnerHaste` 는 "던전의 모든 몬스터 스포너가 더 빠르게 몬스터를 내보내게 하는" 카드다. 이 카드를 한 번 쓸 때마다 스폰 속도가 20% 빨라지는데, 현재는 이 카드를 계속 반복해서 고를 수 있어 스폰 속도가 무한정 빨라질 수 있다. 디자이너가 이미 "3번까지만 효과를 쌓자"고 메모해 두었는데, 아직 게임에 반영이 안 된 상태다. 4번 이상 고르면 전략 점수(시너지 카운트)만 오르고 스폰 속도는 더 빠르지 않게 해서, 너무 쉽게 타임오버 없이 영웅을 잡는 상황을 방지할 수 있다. 그래서 이번에 제안하는 것은: 이 "3번 상한" 규칙을 공식 기획서로 만들어 게임에 반영하자는 것이다.
