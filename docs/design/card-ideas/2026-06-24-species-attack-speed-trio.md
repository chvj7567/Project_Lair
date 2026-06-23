# Card Ideas — 2026-06-24 — 종별 공속 전문화 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: **종별 공속 전문화** — 현재 Reaper 에만 존재하는 '공격 쿨다운 감소' 카드를 Wisp·Hex·Plague 3종으로 확장. 각 카드가 해당 축의 기존 카드(WispHpBoost, HexRangeBoost, PlagueSlowBoost)와 시너지 빌드를 완성시킴.
- 목록: WispAtkSpeedBoost / HexAtkSpeedBoost / PlagueAtkSpeedBoost
- 기존 28장 + git log 과거 15회차와의 중복 회피 확인됨
- QA 리포트(`docs/qa-reports/2026-05-22.md`) 는 헤드리스 픽 훅 미구현으로 BLOCKED 상태 — 카드 픽률 데이터 없음. 이번 제안은 카드 풀 공백 분석 기반.

---

## 1. WispAtkSpeedBoost — 위스프 공속 강화

- **카테고리**: 패시브 강화 (Tank 축)
- **효과 모델**: 위스프 종 공격 쿨다운 영구 ×0.7 (공속 +43%). 현재 위스프 DPS 10, Cooldown 1.2s 가정 시 → 유효 DPS 14.3. 카드 중첩 시 ×0.7×0.7=×0.49 (공속 ×2.04).
- **구현 패턴**: `MonsterBuffService.ApplyGlobalBuff(EMonster.Wisp, attackCooldownScale: 0.7f)` — 기존 `ReaperAtkSpeedEffect` 와 완전 동일 패턴, `EMonster.Wisp` 로만 대상 변경.
- **시너지 후크**:
  - WispHpBoost(HP ×1.5) + WispAtkSpeedBoost(공속 ×0.7) = "공격 탱커 위스프" — 버티면서 딜도 넣음
  - WallOfWisps(즉시 4마리 소환) + WispAtkSpeedBoost = 소환 직후 빠른 연속 공격으로 체력 갉아내기
  - Tank Tier1(Wisp·Wraith HP ×1.3) 발동 상태에서 WispAtkSpeedBoost 중첩 시 탱크 딜 완성
- **구현 비용 추정**: 1 (ReaperAtkSpeedEffect.cs 를 WispAtkSpeedEffect.cs 로 복제 후 enum 1개 변경)
- **중복 재검증**: 기존 28장에 Wisp 공속 카드 없음(WispHpBoost만 존재). git log 15회차 검토 — 위스프 공속 제안 없음. 위스프 관련 제안: 2026-06-03(WispWall 포위전 — 소환 배치), 2026-06-10(Tank 재생·분열 — 재생/분열), 2026-06-11(크로스 전환 — 스포너 종 변경). 모두 다른 메커니즘.

---

## 2. HexAtkSpeedBoost — 헥스 공속 강화

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**: 헥스 종 공격 쿨다운 영구 ×0.7 (공속 +43%). 현재 헥스 DPS 30, Cooldown 1.0s 가정 시 → 유효 DPS 42.9. ReaperAtkSpeed(리퍼 공속)와 동일 수치, 대상만 헥스.
- **구현 패턴**: `MonsterBuffService.ApplyGlobalBuff(EMonster.Hex, attackCooldownScale: 0.7f)` — ReaperAtkSpeedEffect 와 완전 동일, EMonster.Hex 로만 변경.
- **시너지 후크**:
  - HexRangeBoost(사거리 ×1.4) + HexAtkSpeedBoost(공속 ×0.7) = 원거리에서 빠른 사격 — 영웅이 접근하기 전부터 체력 소모 시작
  - ReplaceReapersToHex(리퍼→헥스 전환) + HexAtkSpeedBoost = 헥스 중심 "원거리 속사" 빌드
  - Dps Tier2(Reaper·Hex Cooldown ×0.8) 이미 발동 후 HexAtkSpeedBoost 추가 시 총 ×0.56 (공속 ×1.79)
  - SpawnHexes(r11 제안: 헥스 스포너 출력 +1) 와 결합 시 필드에 헥스가 많아지고 각각 빠른 공격
- **구현 비용 추정**: 1 (ReaperAtkSpeedEffect.cs 복제, EMonster.Hex 로 변경)
- **중복 재검증**: 기존 28장에 HexAtkSpeed 카드 없음(HexRangeBoost 만 존재). git log 15회차 — 헥스 공속 제안 없음. 헥스 관련 제안: 2026-06-12(팬텀·플레이그·헥스 스탯 채우기 — SpawnHexes: 헥스 '소환 수'), 2026-06-17(딜러 내구도 — HexBarrier: 헥스 'HP'). 공속은 미제안. 개념 중복 없음.

---

## 3. PlagueAtkSpeedBoost — 플레이그 공속 강화

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**: 플레이그 종 공격 쿨다운 영구 ×0.7 (공속 +43%). 플레이그의 공격 = 영웅에게 둔화 적용이므로, 공속 증가 = 둔화 재적용 빈도 증가 → 영웅이 거의 항상 최대 둔화 상태 유지. 플레이그 유효 DPS: 5 → 7.1 (소소하지만 주 효과는 둔화 지속). SlowFactor 기본 0.8 기준 공격당 둔화 재갱신 주기가 1.0s → 0.7s 로 줄어, PlagueSlowBoost(SlowFactor ×0.75) 와 조합 시 영웅이 0.6 속도로 거의 항상 이동하게 됨.
- **구현 패턴**: `MonsterBuffService.ApplyGlobalBuff(EMonster.Plague, attackCooldownScale: 0.7f)` — ReaperAtkSpeedEffect 와 동일, EMonster.Plague 로만 변경. 둔화 재적용 빈도 증가는 추가 코드 없이 플레이그 공격 속도 증가에서 자연 발생.
- **시너지 후크**:
  - PlagueSlowBoost(SlowFactor ×0.75 강화) + PlagueAtkSpeedBoost(재적용 빈도 +43%) = 강한 둔화 × 높은 빈도 → 영웅이 거의 완전 둔화 상태
  - Bleed(이동 시 HP -2%/s, 액티브) 와 조합: 항상 둔화 중이라도 이동 시 출혈 발동 → Debuff 축 전략의 핵심 조합
  - Debuff Tier2(HeroAttackDown 자동 등록) + PlagueAtkSpeedBoost = 느리게 걷는 + 공격 약화 영웅이 플레이그 무리에 둘러싸임
  - SpawnPlagues(플레이그 출력 +1) + PlagueAtkSpeedBoost = 더 많은 플레이그가 더 빠르게 둔화 적용
- **구현 비용 추정**: 1 (ReaperAtkSpeedEffect.cs 복제, EMonster.Plague 로 변경)
- **중복 재검증**: 기존 28장에 PlagueAtkSpeed 카드 없음. git log 15회차 — 플레이그 공속 제안 없음. 플레이그 관련 제안: 2026-06-12(PhantomHpBoost·PlaguePowerBoost·SpawnHexes — PlaguePowerBoost 는 '공격력', 나의 카드는 '공격 쿨다운': 다름), 2026-06-19(PlagueSpread — '스포너 주기 가속': 다름), 2026-06-23(PlaguePersistence — '패시브 체력 재생': 다름). 공속 미제안.

---

## 4. 공통 테마 고찰

**왜 오늘 이 테마인가: 카드 풀 공백 — 공속 카드의 비대칭성**

기존 28장 중 '공격 쿨다운 감소' 카드는 `ReaperAtkSpeed` 단 1장뿐이다. Reaper만 공속 전문화 카드를 가지고 있고, Wisp·Hex·Plague·Wraith·Phantom 은 없다. 이 비대칭은 Dps 축(Reaper 중심)에만 "공격 리듬 가속" 선택지가 존재하는 구조적 공백이다.

v0.2 풀 확장(패시브 30~40장)을 위해 각 축이 최소 1개씩의 공속 카드를 가져야 "공속 전략"이 특정 축의 독점이 아닌 범용 빌드 요소가 된다.

오늘 제안이 Wraith·Phantom 을 포함하지 않는 이유: Wraith 는 매우 느린 탱커로 공속보다 HP가 핵심(WraithDamageBoost가 HP로 리뉴얼된 이유), Phantom 은 DPS 5로 낮아 공속보다 수량(SpawnPhantoms)이 더 효율적. 따라서 공속 전문화의 우선순위는 Wisp(탱딜 전환) > Hex(원거리 보완) > Plague(둔화 효율) 순.

**QA 공백 관점**: 유일한 QA 리포트는 시뮬레이터 훅 미구현으로 BLOCKED. 카드 픽률 데이터 없음. 이번 제안은 카드 설계 구조 분석(공속 카드 비대칭성)에서 도출.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 — 이 문서 + `docs/design/card-renewal.md` §3 를 입력으로 전달
- ECardId 추가 위치: `CommonEnum.cs` 의 Dps 구간 뒤(HexAtkSpeedBoost), Tank 구간(WispAtkSpeedBoost), Debuff 구간(PlagueAtkSpeedBoost)에 각각 삽입
- SO 파일명: `WispAtkSpeedBoost.asset` / `HexAtkSpeedBoost.asset` / `PlagueAtkSpeedBoost.asset` (Enum 값명 = 파일명 Rule 03 §2)
- 효과 클래스: `WispAtkSpeedEffect.cs` / `HexAtkSpeedEffect.cs` / `PlagueAtkSpeedEffect.cs` (ReaperAtkSpeedEffect.cs 복제 기준)
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 리퍼라는 몬스터만 "빠른 공격 속도" 전문 카드를 가지고 있어요. 다른 몬스터들은 HP를 올리거나 숫자를 늘리는 카드는 있지만, 공격 속도를 높이는 카드가 없는 거죠. 이번에 제안하는 카드 3장은 위스프(탱커), 헥스(원거리 마법사), 플레이그(느리게 만드는 유틸) 각각에 "더 빠르게 공격"하는 능력을 줘요. 특히 플레이그는 공격할 때마다 영웅을 느리게 만드는 효과가 있는데, 더 자주 공격하면 영웅이 거의 항상 느릿느릿 걸어다니게 되는 시너지가 생깁니다.

그래서 오늘 제안하는 카드 3장은: 탱커가 딜도 넣는 "공격 탱크" 위스프, 멀리서 빠르게 쏘는 원거리 중심 헥스, 영웅을 느리게 만들어 꼼짝 못하게 하는 플레이그를 위한 공속 카드들입니다.
