# Card Ideas — 2026-06-01 — 리퍼·헥스 딜러 심화: 빠르게, 아프게, 많이

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 리퍼·헥스 Dps 심화 — 현재 Dps 축 7장은 Reaper 공속·Hex 사거리·스포너 수량·즉시 소환에 집중되어 있으나, **Reaper 공격력 배율 / Hex 공속 배율 / Reaper+Hex 즉시 다종 소환** 세 슬롯이 비어 있다. 오늘 카드 3장이 이 공백을 채운다.
- **목록**: 리퍼 격살 / 헥스 연사 / 처형 부대
- **기존 28장 + git log 과거 5회차와의 중복 회피 확인됨**
  - 기존 ReaperAtkSpeed = 공속 ×0.7 (공격력 배율 없음). 기존 HexRangeBoost = 사거리 ×1.4 (공속 배율 없음).
  - 기존 SwarmRush = Phantom 한 종 6마리. 기존 WallOfWisps = Wisp 한 종 4마리. → Reaper+Hex 혼합 즉시 소환 없음.
  - 과거 5회차(전장 상태 감지·종간 연계·플레이그-독·낙인 트리오): 모두 "상태 감지" / "종 간 협약" / "플레이그·독" / "영구 낙인 중첩" 테마. 이번 테마와 축 다름.

---

## 1. 리퍼 격살 (Reaper Lethal Strike) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Reaper 종 공격력 글로벌 영구 ×1.35.
  - 기본 Reaper: HP 100, 공격력 40, 공속 1s → 40 DPS.
  - 이 카드 픽 후: 공격력 54 → 54 DPS.
  - ReaperAtkSpeed(공속 ×0.7, 즉 쿨다운 0.7s) 와 조합: 54 / 0.7 ≈ **77 DPS** (기본 대비 193%).
  - 필드 Reaper 평균 3~4마리 × 77 DPS = 231~308 DPS 딜러 라인 단독.
  - 밸런스 근거 (컨셉 §8 2~4분 밴드): 영웅 HP 1000 기준, Reaper 라인만 단독으로 약 3.3~4.3s에 영웅 처치 가능한 DPS지만, 실제로는 영웅이 몬스터 처치하며 수를 줄이므로 전체 딜의 20~30% 기여로 조정됨. ×1.35 는 Wisp HP ×1.5 대비 보수적.
- **구현 패턴**: `ReaperLethalStrikeEffect.cs` — MonsterBuffService 의 종 특화 공격력 배율 메서드 (WispHpBoost 구조 그대로, stat: AttackPower, multiplier: 1.35f, species: EMonster.Reaper). IBattleContext 신규 API 불필요.
- **시너지 후크**:
  - ReaperAtkSpeed: 공속 × 공격력 → DPS 193%. 핵심 Dps 빌드 완성 카드.
  - MarkOfDeath (영웅 받는 데미지 ×1.5, 5s) + Lethal Strike: 5s 창에서 Reaper 최대 DPS = 77 × 1.5 = 115.5 DPS/마리.
  - BloodThirst (처치 시 인근 몬스터 HP +30 회복): Reaper가 빠르게 처치 → 회복 트리거 빈도 증가.
- **구현 비용 추정**: 1 (WispHpBoostEffect 구조 그대로. 신규 패턴 없음)
- **중복 재검증**: 기존 28장에 Reaper 공격력 배율 없음. 과거 5회차 모두 종간 협약·상태 감지·플레이그·낙인 테마. 이 카드는 단일 종 스탯 배율로 완전 신규.

---

## 2. 헥스 연사 (Hex Rapid Fire) — 가칭

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**:
  - Hex 종 공격 쿨다운 글로벌 영구 ×0.75 (= 공속 약 +33%).
  - 기본 Hex: HP 60, 공격력 30, 원거리. 공속 기준 30 DPS.
  - 이 카드 픽 후: 쿨다운 ×0.75 → 30 / 0.75 = **40 DPS**.
  - HexRangeBoost (사거리 ×1.4) 조합: 멀리서(안전 거리 확보) 40 DPS 사격.
  - 밸런스 근거: ReaperAtkSpeed 가 Reaper 에 ×0.7 쿨다운(+43%)을 적용하는 것과 비교해 ×0.75(+33%)로 약간 보수적 — Hex 는 원거리라 죽을 위험이 낮아 DPS 기여 지속성이 높으므로 조정.
- **구현 패턴**: `HexRapidFireEffect.cs` — ReaperAtkSpeedEffect 구조 그대로, EMonster.Hex로 종만 교체. cooldownMultiplier = 0.75f.
- **시너지 후크**:
  - HexRangeBoost + 헥스 연사: 원거리 포병 모델. 사거리 ×1.4 + DPS +33% → Hex 가 더 안전한 위치에서 더 자주 사격.
  - MarkOfDeath + 연사: 5s 창 Hex DPS = 40 × 1.5 = 60 DPS/마리. 필드 Hex 평균 2~3마리면 120~180 DPS 추가.
  - ReplaceReapersToHex (Reaper 스포너 → Hex 출력 교체): Hex 수 극대화 + 연사 조합 → 원거리 포격 빌드.
- **구현 비용 추정**: 1 (ReaperAtkSpeedEffect 구조 그대로. 종 Enum 교체만)
- **중복 재검증**: 기존 28장에 Hex 공속 배율 없음 (HexRangeBoost만 존재). 과거 5회차와 테마 무관.

---

## 3. 처형 부대 (Execution Squad) — 가칭

- **카테고리**: 액티브 Dps 버프 (와일드 성격)
- **효과 모델**:
  - 즉시 영웅 주변(반경 4m)에 **Reaper 3마리 + Hex 2마리** 소환 (총 5마리).
  - 글로벌 캡(18마리) 초과 방지: 캡 여유분만큼만 소환 (여유가 3이면 Reaper 2 + Hex 1 우선순위 순).
  - 소환된 유닛은 정규 스포너 유닛과 동일 — 즉 Reaper 격살(공격력 ×1.35) · 헥스 연사(공속 ×0.75) · BloodThirst 등 기존 버프 모두 적용.
  - 밸런스 근거: SwarmRush (Phantom 6마리, 저DPS 5), WallOfWisps (Wisp 4마리, 탱커), 처형 부대(Reaper·Hex 5마리, 고DPS) 로 급을 나눔. 처형 부대가 가장 강력한 즉시 소환이나 캡 제한과 Reaper·Hex 의 낮은 생존력(HP 100/60)으로 균형.
  - 최적 콤보 예시: MarkOfDeath(영웅 받는 데미지 ×1.5, 5s) → 처형 부대 → 5마리 집중 공격 → 5s 창에서 최대 (77×3 + 60×2) × 1.5 = (231+120)×1.5 ≈ 526 DPS (캡 여유 있을 때 상한). 실제는 영웅 몬스터 처치로 감쇄.
- **구현 패턴**: `ExecutionSquadEffect.cs` — SwarmRushEffect / WallOfWispsEffect 패턴 그대로. CHMPool.Pop(reaperPrefab) × 3 + CHMPool.Pop(hexPrefab) × 2, 각 위치는 영웅 위치 ± 랜덤 오프셋(WallOfWisps 4방위처럼). 캡 체크는 IBattleContext.MonsterCount 조회.
- **시너지 후크**:
  - MarkOfDeath (가장 높은 시너지): 피해 강화 창에 집중 공격 트리거.
  - Frenzy (전체 공속 +50%, 10s): 처형 부대 소환 직후 Frenzy → 5마리 모두 과공속.
  - 리퍼 격살 + 헥스 연사가 이미 활성화된 상태에서 처형 부대 → 강화된 딜러 5마리 즉시 투입.
- **구현 비용 추정**: 2 (SwarmRushEffect 구조 재사용. Reaper+Hex 두 종 prefab 참조 + 캡 체크 로직 추가)
- **중복 재검증**: 기존 SwarmRush = Phantom 단일 종 6마리. WallOfWisps = Wisp 단일 종 4마리. 처형 부대 = Reaper+Hex 혼합 5마리. 혼합 즉시 소환 패턴은 기존에 없음. 과거 5회차 전부와 테마 무관.

---

## 4. 공통 테마 고찰

현재 Dps 축 7장(ReaperAtkSpeed·HexRangeBoost·SpawnReapers·ReplaceReapersToHex·Frenzy·BloodThirst·MarkOfDeath)은 **스폰 수량·공속·사거리·즉시 소환(단일 종)** 카드로 구성된다. 공격력 배율 슬롯이 Reaper·Hex 모두 비어 있어, Dps 빌드가 후반에도 수치 성장감 없이 고원을 맞는다.

오늘 3장이 필요한 이유:
1. **리퍼 격살**: Dps 빌드의 "더 세게" 슬롯 채우기.
2. **헥스 연사**: 원거리 라인의 "더 빠르게" 슬롯 채우기.
3. **처형 부대**: "더 많이" 즉시 투입의 Dps 버전 채우기 (기존 즉시 소환은 Phantom·Wisp 탱크/유틸 계열만).

세 카드가 동시 활성화되면 Dps 축이 완성되며, Tank 축의 WallOfWisps + Swarm 축의 SwarmRush와 대비되는 **딜러 특화 즉시 투입** 전술이 생긴다.

---

## 5. 채택 흐름 제안

- 채택 시 `/start-develop` 또는 `/start-develop-auto` 로 game-designer 호출. 이 문서를 입력으로 전달.
- ECardId 후보: `ReaperLethalStrike`, `HexRapidFire`, `ExecutionSquad`.
- Dps 축 7장에 추가하면 총 10장 → 컨셉 §5.3 v0.2 목표(패시브 30~40, 액티브 20~30) 달성에 기여.
- v0.2 진입 전까지 backlog 보관.

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 리퍼와 헥스는 공격이 빠르고 아프지만 조금 아쉬운 부분이 있다. 리퍼는 한 방이 좀 약하고, 헥스는 공격 텀이 살짝 길다. 또 영웅 쪽으로 즉시 달려가는 "특공대"를 보낼 때는 항상 유령(팬텀)이나 위스프만 갔는데, 정작 제일 강한 딜러들은 특공대 자리가 없었다. 오늘 제안은 이 세 가지 아쉬운 점을 하나씩 채우는 카드들이다. 그래서 오늘 제안하는 카드 3장은: "리퍼를 더 아프게 만드는 카드", "헥스가 더 빠르게 쏘게 하는 카드", "리퍼와 헥스로만 구성된 최정예 특공대를 즉시 소환하는 카드".
