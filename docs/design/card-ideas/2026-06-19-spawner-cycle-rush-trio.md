# Card Ideas — 2026-06-19 — 소모될수록 더 빠르게: 종 스포너 집중 가속 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

---

## 0. 오늘 제안 개요

- **테마**: 종 스포너 집중 가속 — 팬텀 스포너 주기 ×0.6(기존 Multiply)과 같은 구조를 Reaper·Wraith·Plague 세 종에 적용. 기존 28장에 팬텀 외 다른 종의 스포너 주기 가속 카드가 전무한 공백을 채운다. "영웅이 몬스터를 처치해도 해당 종이 더 빠르게 보충된다"는 지속 압박 설계.
- **목록**: 리퍼 홍수 (ReaperOverflow) / 레이스 파도 (WraithTide) / 역병 확산 (PlagueSpread)
- **기존 28장 + git log 과거 21회차와의 중복 회피 확인됨**
  - 기존 28장:
    - Multiply = Phantom 스포너 주기 ×0.6 영구 (`FastBreedingEffect`) — 팬텀 전용
    - SpawnerHaste = 모든 스포너 주기 ×0.8 영구 — 전체 대상, 약한 배율
    - SpawnReapers/SpawnWraith/SpawnPlagues = 동시 출력 +1 (주기는 건드리지 않음)
    - Reaper·Wraith·Plague 각각의 스포너 주기 가속 카드: 전무 ✅
  - 과거 21회차 전부:
    - 5/28 전장 상태 스냅샷 / 5/29 종간 공존 시너지 / 5/30 플레이그 독 연쇄 / 5/31 영구 낙인 누적
    - 6/01 리퍼·헥스 딜러 심화 (Reaper Power + Hex 공속 — 스포너 주기 아님) / 6/02 처치 반향 스폰
    - 6/03 위스프 벽 포위 / 6/04 Dps×Debuff 교차 사냥 / 6/05 타이머 연동 압박
    - 6/06 군단 밀도 압박 / 6/07 레이스·팬텀 각성 (Wraith Power·MoveSpeed — 주기 아님)
    - 6/08 도주 처벌 / 6/09 킬 반향 패널티 / 6/10 탱크 재생·분열 / 6/11 크로스 축 전환
    - 6/12 팬텀·플레이그·헥스 스탯 채우기 / 6/13 전술 즉시 배치 / 6/15 공격 반격 패널티
    - 6/16 와일드 카드 신설 / 6/17 딜러 내구도 (Reaper·Hex HP) / 6/18 스포너 다양성 보상
    - 어느 회차도 "특정 단일 종의 스포너 주기만 가속"하는 패턴 미제안 ✅

---

## 1. 리퍼 홍수 (ReaperOverflow) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Reaper 스포너 주기 영구 ×0.55.
  - 기준: Reaper 기본 스폰 주기를 T라 하면, 픽 후 0.55T로 단축 — 같은 5분 동안 약 82% 더 많은 Reaper 보충.
  - 게임플레이 변화: 현재 Reaper(HP 100, 공격력 40)는 영웅에게 빠르게 처치된다. 이 카드 픽 후 Reaper 라인이 처치 → 재등장 사이클이 짧아져 필드에 항상 2~3마리 유지가 보장됨.
  - SpawnerHaste(전체 ×0.8) 비교: 이 카드는 Reaper 전용이고 배율이 더 강함(×0.55). 그러나 Reaper 한 종에만 집중 투자이므로 Reaper 스포너가 없는(교체된) 빌드에서는 무효.
  - 밸런스 근거 (컨셉 §8): 영웅 2~4분 사망 기준. Reaper DPS 40 × 필드 평균 3마리 = 120 DPS. 빠른 보충으로 이 평균 3마리를 안정적으로 유지하는 구조. SpawnerHaste 조합 시 Reaper 주기 ×0.55 × ×0.8 = ×0.44 — 절반 이하 주기이므로 ReplaceReapersToHex(Reaper 스포너 → Hex 교체)와는 양립 불가 (교체 후 Reaper 스포너 비활성, 이 카드 무의미). 플레이어가 스포너 유지 vs 교체를 선택해야 하는 전략 분기점 생성.
- **구현 패턴**:
  - `ReaperOverflowEffect.cs` — `FastBreedingEffect` 구조 그대로, 대상을 `EMonster.Reaper`, 배율을 `0.55f` 로 교체.
  - `IBattleContext.GetSpawner(EMonster.Reaper)` → `spawner.PeriodMultiplier *= 0.55f`. 스포너가 없거나 교체된 경우 Early Return (효과 없음).
  - 스포너 주기 배율 누적 방식: SpawnerHaste / Multiply 와 동일 곱연산 적용 (배율들 곱). 신규 API 불필요.
- **시너지 후크**:
  - **ReaperAtkSpeed** (쿨다운 ×0.7): Reaper 공속 빠름 + 더 빠른 보충 → 필드 내 항상 빠른 Reaper 다수 유지. Dps 축 핵심 이중 강화.
  - **SpawnReapers** (출력 +1): 스포너 1개에서 2마리씩 더 빠르게 — 보충 수량 × 보충 속도 극대화.
  - **MarkOfDeath** (영웅 받는 피해 ×1.5, 5s): Reaper가 많을수록 MarkOfDeath 5s 창의 피해 총량 증가.
  - **BloodThirst** (처치 시 인근 몬스터 HP +30 회복): Reaper가 빠르게 처치 → BloodThirst 발동 빈도 증가.
- **구현 비용 추정**: 1 (FastBreedingEffect 구조 그대로 복사. 종 Enum·배율 교체만. 신규 패턴·API 없음)
- **중복 재검증**:
  - Multiply = Phantom 전용 ×0.6. 이 카드는 Reaper 전용 ×0.55. 구조 동일하나 종과 배율이 다름 ✅
  - SpawnReapers = 출력 +1 (주기 무관). 이 카드는 주기 단축 (출력 무관). 다른 차원 ✅
  - 6/01 리퍼·헥스 딜러 심화: Reaper Power ×1.35 + Hex 공속. 스포너 주기 아님 ✅

---

## 2. 레이스 파도 (WraithTide) — 가칭

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wraith 스포너 주기 영구 ×0.6.
  - Wraith(HP 500, DPS 20, "매우 느림")는 현재 영웅이 처치하는 데 오랜 시간이 걸려 필드 유지율이 높지만, 스포너 주기가 길면 처치 후 재등장 사이를 영웅이 활용할 수 있다. 이 카드 픽 후 빈 틈을 줄여 Wraith 전선이 상시 유지.
  - Wraith 평균 처치 시간 ≈ 500 / (50 DPS 영웅 기본) = 10s. 주기 ×0.6이면 재등장 간격이 단축 → "영웅이 Wraith를 처치하기도 전에 새 Wraith가 등장" 상황 발생.
  - 밸런스 근거: Wraith는 HP가 높아 영웅이 처치하면 이미 상당한 피해를 입는다. ×0.6 주기 단축은 "처치해도 바로 대체"하는 구조로 사망 저항이 높은 탱커의 특성을 극대화. SpawnWraith(출력+1)와 달리 이 카드는 주기만 건드려 기존 SpawnWraith와 완전히 다른 설계 차원.
  - ReplaceWispsToWraith(Wisp 스포너 → Wraith 교체)와의 조합: Wraith 스포너가 2개가 되고 각각 ×0.6 주기 가속 → Wraith 공급량 급증. 주의: 글로벌 캡 18 도달 시 자연 백오프.
- **구현 패턴**:
  - `WraithTideEffect.cs` — FastBreedingEffect 구조 그대로, `EMonster.Wraith`, `periodMultiplier: 0.6f`.
  - Wraith 스포너가 존재하지 않으면(ReplaceWispsToWraith 이전 상태에서 기본 1개는 항상 있음) 정상 적용.
  - SpawnWraith(출력+1)와 조합 시 수치 처리: 스포너 out 수는 SpawnWraith가 담당, 주기는 이 카드가 담당. 각각 독립 필드이므로 충돌 없음.
- **시너지 후크**:
  - **SpawnWraith** (출력 +1) + WraithTide: 더 많은 Wraith를 더 빠르게 — Wraith 군단화.
  - **WraithDamageBoost** (Wraith HP ×1.5): Wraith가 더 오래 살아남는다 + 더 빠르게 보충 → 전선 두께 극대화.
  - **GuardianRage** (Wisp·Wraith HP ×2 + 방어, 15s): WraithTide로 필드에 많은 Wraith가 있을 때 GuardianRage 발동 → 15s 창에서 HP 1500짜리 탱커가 빠르게 보충되는 압박 절정.
  - **ReplaceWispsToWraith**: Wraith 스포너 2개 모두에 주기 가속 적용 → 복리 효과.
- **구현 비용 추정**: 1 (FastBreedingEffect 구조 그대로. 종·배율 교체만)
- **중복 재검증**:
  - 6/07 레이스 각성: Wraith Power ×1.5 + Wraith MoveSpeed ×1.5 — 스탯 배율. 스포너 주기 아님 ✅
  - SpawnWraith = 출력 +1. 이 카드는 주기 ×0.6. 다른 차원 ✅
  - Multiply = Phantom 전용. WraithTide = Wraith 전용 ✅

---

## 3. 역병 확산 (PlagueSpread) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - Plague 스포너 주기 영구 ×0.6.
  - Plague(HP 50, DPS 5, "공격 시 영웅 둔화 20%")는 유틸 유닛이라 처치 우선순위가 낮고 필드에 오래 남는 편. 그러나 처치될 경우 재등장이 늦으면 둔화 효과 공백이 발생. 이 카드로 공백 없는 둔화 덮개 유지.
  - PlagueSlowBoost(SlowFactor ×0.75, 더 강한 둔화) 조합 시: 더 많은 Plague가 더 강하게 영웅을 둔화시킴. 기존에는 PlagueSlowBoost가 둔화 "강도"를 높였다면, PlagueSpread는 둔화 "밀도"를 높이는 역할.
  - SpawnPlagues(컨셉 §11.3 — Plague Spawner #4 활성화)와 연계: SpawnPlagues로 Plague 스포너가 추가 활성화된 후 PlagueSpread로 해당 스포너 주기까지 단축 → Plague 공급량 2배 × 속도 167% = 대폭 증가.
  - 밸런스 근거: Plague는 DPS 5로 직접 딜 기여가 낮고 둔화가 메인. 둔화된 영웅은 몬스터와의 교전 시간이 늘어 간접적으로 전체 몬스터 생존율을 높인다. 영웅이 이동속도 ×0.8(PlagueSlowBoost ×0.75 적용 후 = 슬로우 20%→25%)로 느려지면 포위망 탈출이 어려워 Wraith·Reaper의 접근 성공률 상승.
- **구현 패턴**:
  - `PlagueSpreadEffect.cs` — FastBreedingEffect 구조 그대로, `EMonster.Plague`, `periodMultiplier: 0.6f`.
  - SpawnPlagues가 Plague 스포너 #4를 활성화하는 시점 이후에 PlagueSpread를 픽하면 활성화된 스포너 전부에 주기 가속 적용. 활성화 전 스포너 #1에만 적용되어도 기본 유효.
- **시너지 후크**:
  - **PlagueSlowBoost** (SlowFactor ×0.75): 더 강한 둔화 + PlagueSpread 더 빠른 보충 → "영웅이 항상 더 강하게 둔화된 상태" 달성. Debuff 축 핵심 이중 강화.
  - **SpawnPlagues** (Plague 스포너 #4 활성화): 스포너 수 증가 + 주기 단축 조합. Plague 군집 모델.
  - **HeroPoisonAura** (영웅 발 밑 독 장판 5 DPS): 둔화로 느려진 영웅이 독 장판 위에 오래 머무름 → HeroPoisonAura 기여 시간 증가.
  - **Bleed** (영웅 이동 시 HP -2%/s, 10s): 둔화된 영웅이 빠져나가지 못하고 Bleed가 지속 → 이동 패널티 극대화. PlagueSpread + PlagueSlowBoost + Bleed = Debuff 축 완성 빌드.
- **구현 비용 추정**: 1 (FastBreedingEffect 구조 그대로. 종·배율 교체만. SpawnPlagues와 Spawner #4 공유 — 기존 로직 충돌 없음)
- **중복 재검증**:
  - SpawnPlagues = Plague 스포너 출력 +1. 이 카드는 주기 ×0.6. 완전히 다른 차원 ✅
  - 6/12 팬텀·플레이그·헥스 스탯 채우기: Plague Power 배율 제안. 스포너 주기 아님 ✅
  - 6/18 스포너 다양성 보상: 6종 모두 유지 시 전체 강화. 개별 종 주기 아님 ✅
  - Multiply = Phantom 전용. PlagueSpread = Plague 전용 ✅

---

## 4. 공통 테마 고찰

세 카드 모두 "팬텀 스포너 주기 ×0.6 영구(Multiply)"와 같은 `FastBreedingEffect` 구조를 다른 종에 적용한다. Dps·Tank·Debuff 세 축 각 1장씩, 대칭 구조.

**왜 오늘 이 테마인가:**

1. **명확한 공백**: Multiply(팬텀)가 기존 28장에 있는데도 불구하고 Reaper·Wraith·Plague 버전이 21회차 동안 한 번도 제안되지 않았음. 구현 비용 최저(1) + 명확한 게임플레이 의미 + 기존 28장 어느 카드와도 중복 없음 — 가장 확실한 공백.

2. **QA 컨텍스트**: 현재 QA 시뮬레이터가 BLOCKED 상태(2026-05-22 리포트)라 실측 데이터 없음. 그러나 게임 디자인 관점에서 "단일 종 스포너 주기 가속이 없다"는 비대칭은 명백한 콘텐츠 공백. Dps·Debuff 축은 "강도" 카드(공격력·둔화)는 있지만 "공급 속도" 카드가 팬텀 외에 없음.

3. **전략 다양성**: 이 세 카드가 추가되면 v0.2에서 플레이어가 "스포너 수 vs 스포너 속도" 두 축에서 투자를 결정해야 함. 현재는 SpawnX(출력 +1)만 있어 "수"에만 투자 가능.

**오늘 테마가 해결하는 약한 카테고리 / 비어있는 시너지 영역**:
- Reaper 빌드: AtkSpeed + Power(6/01) + Overflow(오늘) → 3장 조합으로 완성. 현재는 AtkSpeed 1장만 단독 운영 강요.
- Wraith 빌드: DamageBoost + SpawnWraith + Tide(오늘) → 3장으로 Wraith 완전 빌드 가능.
- Plague 빌드: SlowBoost + SpawnPlagues + Spread(오늘) → 3장으로 둔화 덮개 완성.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출, 이 문서를 입력으로 전달.
- 구현 순서 제안: PlagueSpread → ReaperOverflow → WraithTide (Debuff 빌드 완성 → Dps 빌드 완성 → Tank 빌드 완성 순). 세 카드 모두 구현비용 1이라 단일 gameplay-programmer 사이클로 3장 동시 구현 가능.
- v0.2 진입 전까지 backlog 보관.
- CommonEnum.cs에 `ECardId.ReaperOverflow`, `ECardId.WraithTide`, `ECardId.PlagueSpread` 추가 필요.

---

## 6. 쉬운 설명 (비개발자 요약)

던전 주인이 몬스터를 배치해두면, 그 몬스터들이 영웅과 싸우다가 죽을 때가 있습니다. 보통은 죽으면 한참 기다려야 다시 나타나지만, 오늘 제안하는 카드들은 "죽어도 금방 다시 나타나게" 해주는 카드입니다. 마치 공장의 생산 속도를 높이는 것처럼, 리퍼는 더 빠르게, 레이스는 더 빠르게, 역병 몬스터는 더 빠르게 다시 채워집니다. 영웅이 아무리 몬스터를 처치해도 "이미 새 몬스터가 왔네?" 하는 상황을 만들어주는 거죠.

그래서 오늘 제안하는 카드 3장은: "영웅이 열심히 싸워도 던전이 끊임없이 보충되어 지치게 만드는" 카드입니다 — 리퍼 홍수, 레이스 파도, 역병 확산.
