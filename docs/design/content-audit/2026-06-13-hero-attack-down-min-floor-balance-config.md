# Content Audit — 2026-06-13 — Debuff 패시브 HeroAttackDown 곱연산 3픽 + Tier2 자동 등록 복합 하한 미설계 — BalanceConfig 손잡이 추가 제안

> 자동 생성 — Project Lair 컨텐츠 감사 루틴 (Rule 01 자동화 예외).
> 이 보고는 제안이며, 정식 기획화는 game-designer 호출이 필요하다.

## 0. 입력 스냅샷
- 컨셉서 버전: v0.7 (2026-06-10 갱신)
- 참조 spec/plan 수: 28개 (specs 28 / plans 28)
- 참조 QA 리포트 수: 1개 (최신: 2026-05-22, BLOCKED)
- 과거 감사 이력 (git log): 5건 (가장 최근: 2026-06-11)

---

## 1. 현황

| 카테고리 | 컨셉 §11.3 | 실제 에셋 | 차이 |
|---|---|---|---|
| 영웅 | 1 | 1 (Knight.prefab) | 0 |
| 몬스터 | 6 | 6 (Wisp/Wraith/Reaper/Hex/Plague/Phantom) | 0 |
| 패시브 카드 | 16 | 16 (.asset 확인) | 0 |
| 액티브 카드 | 12 | 12 (.asset 확인) | 0 |
| 카드 효과 클래스 | 28 | 28 (Effects/*.cs 확인) | 0 |

### 계획 있으나 미구현
- **SwarmRush 미구현**: `card-renewal.md` §3.4 에서 `Multiply` 자리를 SwarmRush(Phantom 6마리 즉시 소환)로 교체 예정이었으나, 현행 `Multiply.asset`("빠른 번식", `FastBreedingEffect`) 잔존. 원안의 "광역 압살 방지" 의도 미실현.
- **Debuff Tier3 EternalBleedAura**: `card-renewal.md` §4.2 에 "구현 검증 필요" 주석. 영구 출혈 aura 별도 클래스 필요 여부 미확정.
- **Swarm Tier2·3 구현**: 같은 §4.2 주석 — `SwarmSynergyTier2`(스포너 주기 ×0.85) / `SwarmSynergyTier3`(스포너 출력 +1) 코드 존재 확인 됐으나 효과량 12개 전체 코드-기획서 일치 미검증.

### QA 권고 미해결
- `DebugAutoPicker` 훅 미구현 (2026-05-22 리포트 §3). `BattleController` 에 `#if UNITY_EDITOR` 델리게이트 추가 없이는 헤드리스 시뮬레이션 자동 픽 불가. → qa-simulator 본격 가동 차단 중.
- 이로 인해 컨셉 §8 기준("영웅 2~4분 사이 사망") 정량 검증이 아직 이루어지지 않음.

### 과거 감사 후보 (git log 조회 결과)

| 날짜 | 커밋 SHA | subject 설명 |
|---|---|---|
| 2026-06-11 | e4c765b | Swarm 액티브 TimeStop·Fear 지속시간 누적 상한 캡 BalanceConfig 손잡이 추가 제안 |
| 2026-06-10 | abe2ecd | Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계 제안 |
| 2026-06-09 | 440794c | Dps 패시브 HexRangeBoost 3픽+Tier3 중첩 시 ring 반경 초과 배율 재조정 제안 |
| 2026-06-08 | 2002c8b | Debuff 액티브 출혈 카드 비율 재조정 제안 (Bleed 2%→1%/s) |
| 2026-06-07 | 307ec17 | Dps 축 ReaperAtkSpeed 배율 재조정 제안 (Cooldown ×0.7→×0.75, Tier2 중첩 하한 없음 해소) |

---

## 2. 추가 컨텐츠 후보 (권장 1개)

### Debuff 패시브 HeroAttackDown 곱연산 3픽 × Debuff Tier2 자동 등록 복합 하한 미설계 — BalanceConfig `MinHeroAttackScale` 손잡이 추가

- **카테고리**: Debuff 패시브 카드 수치 재조정 + BalanceConfig 손잡이 추가
- **요지**: `HeroAttackDown` 패시브(영웅 공격력 영구 ×0.75, 3픽 상한)는 3픽 시 ×0.421875 까지 감소하는데, Debuff Tier2 시너지가 `HeroAttackDown`과 동일한 `HeroAttackDownAura(0.85f)`를 자동 등록해 복합 누적이 발생한다. 3픽+Tier2 도달 시 영웅 공격력은 원래의 **×0.358(−64%)** 까지 내려가며, 현행 코드에 하한(floor)이 없어 이 수치가 실제 전투에서 그대로 발동한다. `BalanceConfig`에 `MinHeroAttackScale` 손잡이를 추가해 하한을 외부 조정 가능하게 만드는 것이 목표다.
- **검증/구현/시너지/데이터**: 4/2/4/4 → 종합 **16**
- **근거**:
  - `card-renewal.md` §3.3 #4: `HeroAttackDown` 효과 클래스 `HeroAttackDownAura.OnAttached`가 매번 `PowerScale *= 0.75` 곱연산 누적, 2픽=×0.5625, 3픽=×0.422
  - `card-3pick-cap.md` §2.1: 3픽 천장 ×0.422 명시
  - `card-renewal.md` §4.2 Debuff Tier2: "HeroAttackDown 자동 등록 (영웅 공격력 ×0.85 영구, 카드와 곱연산 누적)"
  - 복합 계산: ×0.422 × 0.85 = **×0.3587** — 영웅 공격력 50/타 → 약 18/타 (−64%)
  - 컨셉 §8: "HP가 30초 안에 깎임 → 액티브 1~2번밖에 못 씀 → 빌드업 X (튜닝 실패)" — 영웅이 너무 무력화되면 반대로 몬스터 DPS가 영웅 HP를 너무 빠르게 소진시켜 패시브 선택지 횟수가 줄고 빌드업 기회 자체가 사라질 위험
- **MVP 범위**: 컨셉 §11.2 "패시브 카드 16장" 포함 항목. BalanceConfig 손잡이는 기존 `MonsterStatRow.SpawnPeriod` 이관(`spawn-period-balance.md`)과 같은 경로의 "데이터 단일화(SoT)" 작업.

#### 유저 플로우 (9개 항목)

**1. 노출 시점·트리거**
영웅 HP 10%마다 발생하는 패시브 카드 선택지 3택 팝업에서 `HeroAttackDown`("약화의 저주") 카드가 등장할 수 있다. Debuff 축(총 4종 패시브 카드) 풀에서 무작위 추첨되며, 같은 카드가 이미 2픽된 상태라면 "2/3" 배지와 함께 다시 노출될 수 있다. 3픽 이후는 `CardDeck.Draw` 후보에서 제외되어 팝업에 등장하지 않는다.

**2. 화면 변화**
카드 선택 팝업 상단의 Debuff 빌드 카운트 셀이 갱신되어 현재 축 카운트를 보여준다. `HeroAttackDown` 카드에는 Debuff 축 색(보라 `#A855F7`) 테두리가 표시되고, 이미 1픽 이상이면 우상단에 "N/3" 배지가 표시된다. 전투 필드에서는 영웅 위의 상태 아이콘 패널에 영향을 줄 수 있다(`hero-status-icons.md` 참조).

**3. 입력 행동**
플레이어가 세 선택지 중 `HeroAttackDown` 카드를 클릭한다. 이 선택은 `CHButton.OnClick`을 통해 `CardSelectionArg.OnPicked` 콜백을 호출하며 팝업을 닫는다. 픽 카운터가 해당 `ECardId.HeroAttackDown`에 +1 누적된다.

**4. 시스템 반응**
`HeroAttackDownEffect.Apply(ctx)` 가 실행되어 `ctx.Hero`의 `Attacker` 컴포넌트에 `HeroAttackDownAura` 인스턴스를 새로 생성·부착(`IBattleContext.ApplyHeroAura`)한다. `HeroAttackDownAura.OnAttached`가 즉시 `_attacker.PowerScale *= 0.75f`를 실행해 영웅의 공격력이 픽 전 대비 -25% 영구 감소한다. 동시에 `BattleViewModel.AddPick`과 `BuildSynergyService`가 Debuff 축 카운트를 +1 올리고, Tier2(5장) 도달 시에는 `HeroAttackDownAura(0.85f)`가 추가로 자동 등록된다.

**5. 반복·재발생 패턴**
HP 10%마다 선택지가 나오므로, 최대 9번의 패시브 픽 기회 중 2픽·3픽 이 발생할 수 있다. 2픽 시 `OnAttached`가 다시 호출되어 `PowerScale *= 0.75`가 재적용(×0.5625 누적). 3픽 시 ×0.421875 누적. 이와 별개로, Debuff 축 합산 카운트가 5장에 도달하면 Tier2가 1회 자동 발화해 `HeroAttackDownAura(0.85f)`가 추가 등록된다 — 이 Tier 발화는 선택지 픽과 무관하게 동 픽 직후 즉시 발생한다.

**6. 종료·해소 조건**
`HeroAttackDownAura`는 `ApplyHeroAura(aura, -1f)`(무한 지속)로 등록된다. 전투 종료(승리/패배/재시작) 전까지 해소되지 않는다. 플레이어가 선택을 되돌리거나 효과를 해제하는 UI 경로는 MVP 범위 내에 존재하지 않는다.

**7. 다른 시스템과 상호작용**
`HeroAttackDownAura`가 등록된 상태에서 `Weaken`(액티브, 영웅 공격력 ×0.5, 10초)이 추가로 발동하면, `HeroAttackDownAura`(영구)와 `WeakenAura`(시한)가 `PowerScale` 위에서 곱연산 중첩된다. 3픽+Tier2+Weaken 동시 구간에서 영웅 공격력은 ×0.3587 × 0.5 = **×0.179** (−82%)까지 내려갈 수 있다. 이 복합 상태가 영웅의 자동 공격 의사결정(AI는 공격력과 무관하게 가장 가까운 몬스터를 향해 이동·공격 — `autocombatai-hysteresis.md` §1)에는 영향을 주지 않지만, 영웅 HP 감소 속도에는 직접 영향을 준다.

**8. 엣지 케이스**
HeroAttackDown 3픽(×0.422) + Debuff Tier2 자동 등록(×0.85)의 복합 하한 부재가 핵심 엣지다. 현행 `HeroAttackDownAura.OnAttached`는 `_attacker.PowerScale *= factor` 에서 `factor`가 몇 번 곱해지든 클램프를 두지 않는다. 또한 `ApplyHeroAura`는 새 인스턴스를 매번 추가하는 구조로, 같은 `HeroAttackDownAura` 타입이라도 인스턴스가 여러 개 쌓일 수 있다(버프 dedup 없음 — 몬스터 버프의 `AddBuff` dedup 패턴과 달리 영웅 Aura는 중복 체크 미존재 가능성). Tier2에서 ×0.85 aura가 1개 더 들어오는 것이 "최초 1회"만 적용되는지 아니면 Tier2 발화마다 추가되는지도 검증 필요 항목이다.

**9. 유저 정보·피드백**
현재 플레이어는 영웅의 실시간 공격력 배율을 전투 중 확인할 UI가 없다(`hero-status-icons.md`가 상태 아이콘을 추가했으나, 수치 표시는 아니라 아이콘 존재 여부만). Debuff 빌드 카운트 바(좌상단)에서 현재 Debuff 축 픽 수는 알 수 있지만, HeroAttackDown의 누적 공격력 감소량(예: "현재 영웅 공격력 −58%")은 팝업이나 HUD 어디에도 표시되지 않는다. 플레이어가 "이 카드를 3픽하면 영웅 공격력이 어느 수준까지 낮아지는가"를 파악할 직접적인 피드백 경로가 부재하다.

---

### 보류

- **SwarmRush 구현(Multiply → SwarmRush 교체)**: 구현 비용이 높고(새 효과 클래스 + SO), 이미 Swarm 액티브 관련 이슈가 06-11 감사에서 다뤄짐 — 7일 이내 동일 Swarm 축. 다음 사이클로 보류.
- **PhantomMoveSpeedBoost 3픽 × Swarm Tier1 복합 이속 극단화**: 팬텀 이속 ×3.375 × 1.3 = ×4.39. 이동속도 자체는 직접 DPS가 아니라 포위 속도이므로 전투 결과 영향이 간접적 — QA 시뮬레이션 데이터 없이 명확한 문제 규정이 어려움. 보류.
- **HeroPoisonAura DPS 재조정(Debuff 패시브)**: DPS 5, 5s 지속 → 75 HP(-1.6%) 최대. 약한 카드이지만 오늘 채택 후보와 같은 Debuff 패시브 카테고리로 중복을 피하기 위해 보류.
- **Debuff Tier3 EternalBleedAura 구현 검증**: `card-renewal.md §4.2` "구현 검증 필요" 항목. 코드 실재 여부 미확인이라 "재조정"이 아닌 "구현 존재 여부 확인"이 선행 필요 — game-designer 단독 작업 범위 밖.

---

## 3. 과거 감사 대비 차별성

git log 조회 5건 검토 완료.

가장 유사했던 과거 커밋: `abe2ecd` (2026-06-10, Tank 액티브 3중 방어 동시 활성 시 DamageTakenScale 하한 설계) — 동일하게 "하한(floor) 미설계" 문제.

차별점:
- **06-10**: 몬스터 방어력 감소(`DamageTakenScale`) 의 하한 — 몬스터가 받는 데미지의 하한 캡 문제. **Tank 액티브 3종 카드 + MonsterBuffService 영역**.
- **06-13(본 감사)**: 영웅 공격력 감소(`PowerScale`) 의 하한 — 영웅이 입히는 데미지의 하한 캡 문제. **Debuff 패시브 카드 + Tier2 시너지 자동 등록 교차 영역**. 방어/공격 방향 반대, 대상 캐릭터(몬스터 vs 영웅) 반대, 관여 시스템(MonsterBuffService vs HeroAttackDownAura + BuildSynergyService) 다름.

06-08 (Debuff 액티브 Bleed 비율): 단일 액티브 카드의 출혈 비율 수치 단독 조정. 오늘 제안은 패시브 카드 곱연산 × 시너지 자동 등록의 **시스템 레벨 복합 하한 설계** 문제로, 카드 타입(액티브 vs 패시브)·영향 범위·설계 레벨이 다름.

---

## 4. 제외 (범위 밖)

- **영웅 추가**: 컨셉 §13 미정 항목, v0.2 단계 금지(CLAUDE.md §8).
- **신규 몬스터 종 추가**: 컨셉 §11.2 "몬스터 6종" lock.
- **신규 카드 추가(29번째 카드)**: 컨셉 §11.2 "패시브 16 + 액티브 12" lock.
- **서버/클라우드 연동**: CLAUDE.md §8 금지.
- **메인 메뉴 신설**: CLAUDE.md §8 금지.
- **영웅 스킬 수치 재조정**: 오늘 제안 범위 밖 — 독립 시스템(`hero-skills.md`).

---

## 5. 다음 단계 제안

1. 채택 시 `game-designer` 에게 정식 기획 요청 — 확정 사항:
   - `BalanceConfig`에 `float MinHeroAttackScale;` 필드 추가 (권장 기본값: 0.40)
   - `HeroAttackDownAura.OnAttached`에서 `_attacker.PowerScale`을 `MinHeroAttackScale`로 클램프
   - Debuff Tier2 자동 등록 aura(×0.85)가 별도 인스턴스로 추가되는지, 아니면 기존 누적에 합산되는지 코드 확인 후 하한 적용 위치 최종 확정
2. `DebugAutoPicker` 훅 구현(QA 2026-05-22 §3 권고) 이후 Debuff 빌드 시뮬레이션으로 3픽+Tier2 복합 시나리오 메트릭 수집 가능

---

## 6. 쉬운 설명 (비개발자 요약)

이 게임에서 플레이어는 "약화의 저주" 카드를 최대 세 번 고를 수 있고, 세 번 다 고르면 영웅의 공격력이 절반 이하로 뚝 떨어진다. 게다가 같은 유형의 카드를 다섯 장 모으면 자동으로 또 한 번 공격력을 깎는 보너스가 발동해서, 결국 영웅이 칼을 들고 있어도 솜뭉치로 때리는 수준이 돼버린다 — 구체적으로 계산해보면 원래 공격력의 36%밖에 안 남는다. 현재는 이 수치가 얼마나 낮아지든 막아주는 안전장치가 없어서 플레이어가 의도했든 아니든 영웅을 지나치게 무력하게 만들 수 있다. 그래서 이번에 제안하는 것은: "영웅 공격력이 아무리 낮아져도 원래의 40% 이하로는 내려가지 않는 하한선"을 설정 파일에서 조절할 수 있도록 추가하자는 것이다.
