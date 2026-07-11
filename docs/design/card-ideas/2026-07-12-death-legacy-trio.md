# Card Ideas — 2026-07-12 — 사멸 유산: 죽음이 낳는 힘

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: **사멸 유산 (Death Legacy)** — 몬스터가 쓰러지는 순간 남긴 힘(Power·분노·희생)이 살아있는 동료와 던전 전체에 전해지는 카드 3종. "죽음이 낭비되지 않는다"는 역설적 서사.
- 목록: SoulForge (영혼 단조) / RageTransfer (분노 이전) / DespairEcho (절망의 메아리)
- 기존 25장 + git log 과거 22회차와의 중복 회피 확인됨
  - 가장 유사한 기존 카드 BloodThirst: "처치 시 주변 몬스터 HP +30" (살아있는 주변 전체에 소량 즉시 치유, 영웅의 처치 행위가 트리거). SoulForge·RageTransfer 는 "같은 종 사망이 계보·생존자를 강화"하는 것으로 대상·조건·전략적 용도 모두 상이.
  - 가장 유사한 과거 회차: 2026-06-02 death-echo-spawn-trio (사망 위치에 스폰), 2026-06-09 kill-echo-penalty-trio (영웅 처치 시 페널티). 본 제안은 "사망한 몬스터 → 살아있는 동료 버프 전이"이므로 방향·효과 모두 다름.

---

## 1. SoulForge — 영혼 단조 (가칭)

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**:
  - Wisp 또는 Wraith 1마리가 사망할 때마다, **같은 스포너에서 다음으로 소환될 동종 개체에게 "단조 버프"** 를 예약 등록한다.
  - 단조 버프: 해당 개체 소환 즉시 HP ×1.4 + Power ×1.3 영구 적용 (해당 개체 생존 기간 내내 유지).
  - 같은 스포너에서 다음 Pop 이 일어나기 전까지 버퍼에 1회 보관. 소환과 동시에 소비 (다음 개체에게만 1회).
  - 이 카드를 복수 픽하면 단조 배율이 추가 중첩 (2픽: HP ×1.4² = ×1.96, Power ×1.3²).
- **구현 패턴**:
  - `MonsterBuffService.OnMonsterDeath` 이벤트 구독.
  - `ISpawner.PendingInheritBuff` 필드에 (HP배율, Power배율) 예약 저장.
  - `CHMPool.Pop` 직후 `ISpawner` 가 해당 버퍼를 읽어 즉시 적용 후 초기화.
  - `IBattleContext` 에서 스포너 참조 가능 전제 (기존 `SpawnerHaste` 패턴과 동일 접근).
- **시너지 후크**:
  - `WispHpBoost` + `SoulForge`: 이미 HP×1.5인 Wisp 계보에 사망마다 ×1.4 추가 → "강한 위스프가 더 강한 위스프를 낳는" 복리 성장.
  - `SpawnWraith` + `SoulForge`: Wraith 스포너 출력을 늘려 사망 기회(=강화 기회)를 늘림.
  - Dps축 `SpawnReapers` 와 교차 금지 조합 — Tank 단조 버프가 리퍼에게 적용되지 않으므로 축 집중 권장.
- **구현 비용 추정**: 3 (스포너 인터페이스에 `PendingInheritBuff` 필드 추가 + OnDeath 구독 + Pop 후크)
- **중복 재검증**: BloodThirst "처치→주변 HP+30 즉시 치유(살아있는 전체, 소량)" 와 달리 본 카드는 "사망 개체 스포너→다음 소환체에 배율 버프(1:1 계승, 미래에 적용)". 트리거 주체(영웅 처치 행위 vs 몬스터 사망 이벤트), 대상(현재 생존 주변 vs 아직 태어나지 않은 다음 개체), 효과(HP 소량 회복 vs HP+Power 배율 영구 강화) 모두 상이. 개념적 중복 없음.

---

## 2. RageTransfer — 분노 이전 (가칭)

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Reaper 또는 Hex 1마리가 사망할 때, **현재 필드에 살아있는 모든 Reaper·Hex 에게 Power +10 영구 추가**.
  - 이 카드 1픽 기준, 처치당 Power 누적: 10마리 죽으면 +100 Power (Reaper 기본 Power ~40 → 최대 140).
  - 복수 픽 시 처치당 +10 중첩 (2픽 → +20/처치).
  - **영웅이 딜러를 처치할수록 남은 딜러들이 강해지는 역설 구조** — 플레이어가 "처치를 두려워하지 말 것"이라는 게임 언어 반전.
- **구현 패턴**:
  - `MonsterBuffService.OnMonsterDeath` 이벤트 구독.
  - 사망 개체가 `EMonster.Reaper` 또는 `EMonster.Hex` 일 때만 발동.
  - `MonsterBuffService.ApplyPermanentBuff(type: DpsTypes, power: +10)` — 기존 타입 기반 영구 버프 패턴 재사용 (`WispHpBoost` 등 동일 구조).
- **시너지 후크**:
  - `SpawnReapers` + `RageTransfer`: 스포너 출력 증가 → 더 많은 리퍼 공급 → 더 많이 죽어도 괜찮은 구조 → 스택 가속.
  - `Frenzy` + `RageTransfer`: 이미 공속이 빠른 상태에서 Power까지 올라가면 폭발적 순간 딜.
  - `MarkOfDeath` + `RageTransfer`: 영웅에게 표식 걸고 집중 공격 → 리퍼 사망 전 많은 딜 → Power 인계 후 남은 리퍼들이 그 효과 수령.
- **구현 비용 추정**: 2 (OnDeath 구독 + 타입 필터 + 영구 Power 버프 — 모두 기존 패턴)
- **중복 재검증**: 기존 `ReaperAtkSpeed` (공속 ×0.7), `HexRangeBoost` (사거리 ×1.4) 는 픽 시 즉시 전체 적용. RageTransfer 는 "처치 이벤트마다 점진적 Power 누적"으로 발동 조건·누적 구조·플레이 감 모두 다름. 과거 회차 중 "개체 생존 성장 베테랑"(VeteranReaper)은 살아있는 리퍼 자신이 성장, 본 카드는 죽은 리퍼 → 살아있는 동료 전이로 방향이 반대.

---

## 3. DespairEcho — 절망의 메아리 (가칭)

- **카테고리**: 액티브 저주 (Swarm 축)
- **효과 모델**:
  - 발동 시 **이번 런에서 지금까지 사망한 몬스터 총수 × 0.4초** 만큼 영웅 이동속도 ×0.6 (둔화). 최대 지속 15초.
  - 예시: 런 시작 30초(1차 액티브) → 약 5~10마리 사망 → 2~4초 둔화. 2분 경(5차 액티브) → 약 80~120마리 사망 → 15초 최대 둔화.
  - 픽 타이밍이 전략 — 런 중반 이후 픽할수록 강함. 초반 픽 = 약, 후반 = 최대 상한 보장.
- **구현 패턴**:
  - `IBattleContext.TotalDeathCount` (런 통산 사망수) — `RunRecorder` 에 이미 처치 기록 파이프라인 존재 (QA 리포트 §4 `RecordPick` / `lair_runs.jsonl` 참조). 동일 카운터 활용.
  - `duration = Mathf.Min(totalDeathCount * 0.4f, 15f)`
  - `HeroDebuffService.ApplySlow(factor: 0.6f, duration: duration)` — 기존 `SlowEffect` / `FearEffect` 패턴과 동일.
- **시너지 후크**:
  - `SpawnPhantoms` + `DespairEcho`: 팬텀 대량 소환 → 영웅에게 빠르게 닿고 죽음 → 총 사망수 증가 → 후반 DespairEcho 강화.
  - `SpawnerHaste` + `DespairEcho`: 스폰 주기 단축 → 몬스터 더 많이 공급·사망 → 카운터 가속.
  - `Multiply` (FastBreedingEffect) 와 조합: 팬텀 대량 번식 → 사망수 가속 → 동일 효과.
  - **Swarm 빌드에서 "대량 공급 → 대량 사망 → 강한 둔화"** 내러티브가 일관됨.
- **구현 비용 추정**: 2 (`TotalDeathCount` 접근 + 계산식 + 기존 Slow 적용)
- **중복 재검증**: `TimeStop` (영웅 5초 완전 정지), `Slow` (이속 ×0.5 + 몬스터 ×1.3, 10초) 는 발동 즉시 고정값 효과. DespairEcho 는 "런 중 누적 사망수"에 따라 지속시간이 동적으로 변하는 런 히스토리 기반 효과 — 발동값 자체가 런의 경과를 반영하는 새 메커니즘.

---

## 4. 공통 테마 고찰

**왜 "사멸 유산"인가?**

현재 28장 카드 중 몬스터 사망 이벤트를 활용하는 카드는 사실상 없다 (BloodThirst 는 영웅의 처치 행위가 트리거이므로 능동적 관점). QA 리포트는 헤드리스 시뮬 미실행으로 픽률 데이터가 없으나, 컨셉 §8 기준으로 2~4분 클리어 게임에서 **몬스터는 끊임없이 태어나고 죽는다** — 이 "죽음 자체"가 게임 이벤트로 활용되지 않고 있다는 공백이 이 테마의 출발점.

**플레이어 경험 관점 — 역설의 재미:**
- SoulForge: 내 탱커가 죽어도 그 희생이 다음 세대를 더 강하게 만든다 → 패배감이 희망으로.
- RageTransfer: 영웅이 딜러를 처치할수록 남은 딜러들이 분노로 강해진다 → 영웅의 전투 행위가 오히려 부메랑.
- DespairEcho: 내가 희생시킨 몬스터들의 무게가 영웅을 짓누른다 → "희생의 규모"가 전술적 자산.

세 카드 모두 **죽음 이벤트를 손실이 아닌 자원으로 전환**한다는 공통 서사. v0.2 풀 확장에서 탱커/딜러/스웜 축에 각 1장씩 자연스럽게 배치 가능.

---

## 5. 채택 흐름 제안

- 채택 시 `game-designer` 호출 입력으로 이 문서 + `docs/design/card-renewal.md` §3(28장 마스터 표) 를 함께 전달
- `SoulForge` 는 스포너 인터페이스 확장 필요 → `gameplay-programmer` 와 구현 복잡도 사전 협의 권장
- `RageTransfer` 는 기존 패턴 그대로 — 즉시 구현 가능
- `DespairEcho` 는 `RunRecorder.TotalDeathCount` 공개 여부 확인 필요 (비공개면 `IBattleContext` 에 getter 추가)
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 몬스터가 영웅에게 죽으면 그냥 사라집니다. 그런데 "사멸 유산" 카드를 고르면, 죽은 몬스터들이 그냥 사라지지 않아요. 마치 선배가 힘을 후배에게 물려주듯, 죽은 탱커의 체력과 힘이 같은 스포너에서 나올 다음 동료에게 이어집니다. 죽은 공격수의 분노는 살아남은 모든 공격수에게 스며들어 다 같이 더 강해집니다. 그리고 얼마나 많은 몬스터들이 희생되었는지를 영웅이 느껴서, 더 많은 희생이 있을수록 영웅의 발이 더 무거워집니다. 그래서 오늘 제안하는 카드 3장은: 몬스터의 "죽음"을 다음 싸움의 연료로 바꾸는 "사멸 유산" 카드입니다.
