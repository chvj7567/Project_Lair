# 카드 전체 리뉴얼 — 4축 빌드 + 2-Layer 시너지

> 작성: game-designer
> 작성일: 2026-05-31
> 입력 spec: `docs/superpowers/specs/2026-05-31-card-renewal-design.md`
> 입력 plan: `docs/superpowers/plans/2026-05-31-card-renewal.md`
> 참고: `docs/design/project_lair_concept.md` v0.5 §4.2 / §5.2 / §11.3 / §11.4, `docs/design/continuous-spawn-round.md`, `docs/qa-reports/2026-05-26-continuous-spawn-6th-validation.md`, `docs/design/content-audit/2026-05-30-plague-spawner-passive-unlock.md`

---

## § 헤더

- **목표**: 카드 28장을 Tank/Dps/Debuff/Swarm 4축에 균등 배치(축당 패시브 4 + 액티브 3)하고, 같은 축 N장 픽 시 3·5·7장 임계에서 즉시 발화하는 2-Layer 시너지를 도입한다.
- **검증 가설**: 카드 픽이 단일 종 강화로 수렴하던 패턴이 **4가지 명확한 빌드 패턴(탱커/DPS/디버프/스웜)** 으로 분기되고, 각 빌드가 영웅 처치 경로의 차이를 만들어 *재플레이 동기*를 만든다.
- **현재 단계 범위 적합성**: 범위 내. 컨셉 §11.2 항목 표의 "패시브 15 → 16장 / 액티브 10 → 12장 / 몬스터 6종 고정 / 영웅 1명 고정" 안에서 동작한다. 메타·서버·사운드·아트는 손대지 않는다. (§11.4 카드 테두리 7색 → 4색 매핑 갱신은 컨셉서 동기화 범위.)
- **핵심 메커니즘**:
  1. 카드 카테고리를 7종(강화/추가/교체/환경/저주/버프/와일드) → **4축(Tank/Dps/Debuff/Swarm)** 으로 교체.
  2. **Layer 1** — 같은 축 3·5·7장 누적 시 임계 도달 즉시 1회 발화 (12 시너지 효과).
  3. **Layer 2** — 같은 카드 K번 픽 시 그 카드의 효과량/지속시간이 누적 강화 (기존 곱연산 정책 일반화).
  4. **Plague Spawner 1개** 배치 (Wisp 스포너 #4(180°) 를 Plague 로 전환) → 디버프 축 작동의 구조 전제 해소.

---

## 1. 디자인 원칙 (이 기획의 결정 기준)

- **빌드 식별성** — 한 라운드 9픽(패시브 9 + 액티브 9) 중 같은 축을 우선 픽하면 3·5장 임계가 자연스럽게 들어오도록 한다. 7장 임계는 *진성 빌드 보상*(평균 사망 시간 단축 또는 클리어율 하락 기여).
- **빌드 정체성** — 각 축의 효과군은 같은 *승리 패턴* 을 향한다.
  - Tank = 영웅을 **묶어 둔다** (HP·맷집·진로 방해)
  - Dps = 영웅을 **빠르게 깎는다** (공속·사거리·치사 누적)
  - Debuff = 영웅을 **갉아내고 무력화** (둔화·출혈·공포·약화·독)
  - Swarm = **머릿수로 압도** (스폰 수·이동 속도·스폰 주기 가속·전장 통제)
- **결정 부담 최소** — 픽 한 번에 "이 카드가 어느 축인가"가 카드 테두리 색·헤더 텍스트 한 줄로 즉시 식별돼야 한다. 4색만 사용한다.
- **No-op 회피** — 모든 카드가 픽 시점에 가시적 변화를 만든다. SpawnPlagues·PlagueSlowBoost 는 Plague Spawner 추가로 활성화. SpawnWraith 처럼 *Wraith 스포너 없음* 같은 경우엔 대응 시너지 임계 도달 시점에 우회 발화 경로를 마련하지 않고 **교체 카드를 그 자리에 두는** 정책으로 단순화한다 (§9.1 엣지 참조).
- **QA 정합성** — Hero HP 4600 / 평균 사망 76s 베이스라인을 깨지 않는다. 한 축 5장 임계 도달 빌드가 평균 사망을 **5~10s 단축** 정도로 가정 (강화는 했지만 영웅이 더 빨리 죽는 정도). 7장 임계는 **클리어율 하락(타임오버 발생)** 까지는 못 만든다 — 그건 별도 밸런스 사이클의 일이다. 본 기획은 빌드 *다양성* 우선, *난이도 상승* 은 부수.
- **MVP 비주얼** — 프리미티브 + 4색만. UI 는 흰 배경 + 검정 텍스트 + 4축 테두리색. 시너지 임계 표시도 텍스트 + 4축 색 사각형.

---

## 2. 4축 정의 — 빌드 정체성

| 축 | 키 색 (Hex) | 핵심 몬스터 | 빌드 슬로건 | 승리 경로 |
|---|---|---|---|---|
| Tank | `#22C55E` 초록 | Wisp · Wraith | "영웅을 묶어 둔다" | 영웅 진로를 막아 다른 축 몬스터/카드가 일할 시간을 만든다 |
| Dps | `#EF4444` 빨강 | Reaper · Hex | "빠르게 깎는다" | 공속·사거리·치사 누적으로 영웅 HP 를 직접 깎는다 |
| Debuff | `#A855F7` 보라 | Plague + 액티브 저주 콤보 | "갉아내고 무력화한다" | 둔화/출혈/공포로 영웅의 행동을 제약 → 다른 축 효율 ↑ |
| Swarm | `#1F2937` 검정 | Phantom (+ 보조 Plague) | "머릿수로 압도한다" | 스폰 수/주기/속도를 늘려 영웅이 행동할 틈을 안 준다 |

### 색 선택 근거 (컨셉 §11.4 몬스터 매핑과 정합)
- Tank 초록 = Wisp 색(`#22C55E`) — 가장 익숙한 탱커 종 색 그대로.
- Dps 빨강 = Reaper 색(`#EF4444`) — 깡딜 종 색 그대로.
- Debuff 보라 = Plague 색(`#A855F7`) — 둔화 디버퍼 색 그대로.
- Swarm 검정 = Phantom 색(`#1F2937`) — 스웜 종 색 그대로.

→ "내가 픽한 카드 색 = 내가 키우는 몬스터 색" 직관 매핑. 색·종 학습이 1회로 끝난다.

---

## 3. 카드 28장 라인업 표 (마스터)

다음 컬럼 의미:
- **ECardId**: `CommonEnum.cs` 의 enum 값명 (= 에셋 SO 파일명, Rule 03 §2 일치).
- **분류**: 기존 25장 = 보존/리뉴얼, Multiply = 삭제, 신규 3장 = 신규.
- **축**: Tank/Dps/Debuff/Swarm.
- **T**: 트리거 — P=Passive(HP 10%), A=Active(30s).
- **중첩**: 같은 카드 2픽·3픽 시의 누적 방식.

### 3.1 Tank 축 (7장: P4 + A3)

| # | ECardId | 분류 | T | 한글명 | 효과 요약 | 핵심 수치 | 중첩 정책 |
|---|---|---|---|---|---|---|---|
| 1 | `WispHpBoost` | 보존 | P | 끈질긴 위스프 | Wisp 종 글로벌 HP ×1.5 | `_hpMul=1.5` | 곱연산 누적 (2픽=×2.25, 3픽=×3.375) |
| 2 | `WraithDamageBoost` | **축 이동** (Dps→Tank) + 리뉴얼 | P | 망령의 압박 | Wraith 종 글로벌 HP ×1.5 (구 데미지 ×1.5 폐기) | `_hpMul=1.5` | 곱연산 누적 |
| 3 | `SpawnWraith` | 보존 | P | 더 많은 망령 | Wraith 스포너 동시 출력 +1 | `+1` | 가산 누적 (2픽=+2, 3픽=+3, 캡 18 적용) |
| 4 | `ReplaceWispsToWraith` | 보존 | P | 망령으로 진화 | Wisp 스포너 → Wraith 출력 영구 변경 | n/a | 멱등 (이미 Wraith 면 no-op — 컨셉 §6.3 규칙 그대로) |
| 5 | `IronWill` | 보존 | A | 강철 의지 | 모든 몬스터 받는 데미지 ×0.7, 15s | `_duration=15` (배율 ×0.7 은 `MonsterBuffService` IronWill case 내 상수 — 코드 변경 없음) | **지속시간 누적**: 2픽 → 효과 진행 중 재픽 시 잔여+15s, 효과량 그대로 |
| 6 | `WallOfWisps` *(신규 1)* | 신규 | A | 위스프 장벽 | 영웅 주변 4방위에 Wisp 즉시 4마리 소환 (캡 18 적용) | `_count=4 _radius=2.5` | 가산 누적 (2픽 = 8마리, 캡 truncate) |
| 7 | `Berserk` → **`GuardianRage`** (리네임은 하지 않고 enum 자리 유지, 효과만 리뉴얼) | **리뉴얼 (자살 구조 해소)** | A | 수호자의 분노 | Wraith·Wisp 의 HP ×2.0 + 받는 데미지 ×0.5, 15s | `_duration=15` (배율 HP×2.0 / 받는데미지×0.5 + 적용 종 `{Wisp, Wraith}` 는 `MonsterBuffService.cs` GuardianRage case 내 상수 — §10.1) | 지속시간 누적 (잔여+15s) |

> *Berserk 처리*: ECardId.Berserk enum 값은 **자리 보존** (int 직렬화 정합), 효과 클래스는 신규 `GuardianRageEffect` 로 교체. SO 파일명도 `Berserk.asset` 유지 (Addressable 키 = enum 값 정합, Rule 03 §2). UI displayName 만 "수호자의 분노" 로 변경. 자살 구조(몬스터 HP -50%) 폐기 → Tank 보호 카드로 재해석.

> *몬스터 글로벌 버프 카드 (IronWill / Frenzy / GuardianRage) 의 SO 필드 노출 정책*: 본 기획은 **기존 코드 동작 보존** 을 전제로 한다.
> - `IronWillEffect.cs` 는 `_duration` 만 들고 있고, 받는 데미지 배율 ×0.7 은 `MonsterBuffService.cs` 의 IronWill case 안 상수(현재 코드: `hp.DamageTakenScale *= 0.7f`) 다. 본 기획은 이 상수를 SO 필드로 끌어올리지 **않는다**. SO 에는 `_duration=15` 만.
> - `FrenzyEffect.cs` 도 `_duration=10` 만. 공속 +50% 는 `MonsterBuffService.cs` 의 Frenzy case 안 상수(현재 코드: 공격 쿨다운 ×0.67 = 공속 +50%). 본 기획은 변경 안 함.
> - `BerserkPowerEffect.cs` 의 데미지 ×N 상수도 동일 정책 — 단 본 기획은 Berserk 자살 구조 자체를 폐기하고 `GuardianRageEffect` 로 교체하므로 BerserkPower buff 적용 자체가 사라진다 (§10.4).
> - 신규 `GuardianRageEffect` 도 같은 패턴 — SO 에는 `_duration=15` 만, 배율 (`HP ×2.0` + `받는데미지 ×0.5`) 은 `MonsterBuffService.cs` 의 GuardianRage case 내 상수로 들어간다.
> 정책 근거: 본 기획 범위는 *카드 라인업·축·시너지* 의 리뉴얼이며, 몬스터 글로벌 버프 시스템의 SO 스키마 변경은 범위 외다 (D1~D11 어디에도 없음). 배율 튜닝이 필요하면 후속 밸런스 사이클에서 `MonsterBuffService` 상수만 조정.

### 3.2 Dps 축 (7장: P4 + A3)

| # | ECardId | 분류 | T | 한글명 | 효과 요약 | 핵심 수치 | 중첩 정책 |
|---|---|---|---|---|---|---|---|
| 1 | `ReaperAtkSpeed` | 보존 | P | 신속한 사신 | Reaper 종 공격 쿨다운 ×0.7 | `_cdMul=0.7` | 곱연산 누적 |
| 2 | `HexRangeBoost` | 보존 | P | 저주의 시야 | Hex 종 사거리 ×1.4 | `_rangeMul=1.4` | 곱연산 누적 |
| 3 | `SpawnReapers` | 보존 | P | 사신 떼거리 | Reaper 스포너 동시 출력 +1 | `+1` | 가산 누적 |
| 4 | `ReplaceReapersToHex` | 보존 | P | 헥스로 진화 | Reaper 스포너 → Hex 출력 영구 변경 | n/a | 멱등 |
| 5 | `Frenzy` | 보존 | A | 광폭화 | 모든 몬스터 공격속도 +50%, 10s | `_duration=10` (Frenzy 버프=공속+50%) | 지속시간 누적 |
| 6 | `BloodThirst` | **축 이동** (Swarm→Dps) | A | 피의 갈증 | 처치 시 주변 몬스터 HP +30 회복, 30s | `_duration=30` | 지속시간 누적 |
| 7 | `MarkOfDeath` *(신규 2)* | 신규 | A | 죽음의 표식 | 영웅에게 표식 부착 — 다음 5초간 영웅이 받는 데미지 ×1.5 | `_dmgTakenMul=1.5 _duration=5` | 지속시간 누적 (잔여+5s) |

> *BloodThirst 축 이동 근거*: 컨셉 원안 "버프"는 도구일 뿐, "처치 시 회복"은 DPS 축이 영웅 HP 를 깎는 동안 자신을 유지하는 효과로 가장 잘 작동한다. Swarm 의 "머릿수로 압도"는 회복보다 즉시 충원이 어울려 BloodThirst 를 Dps 로 이동.

### 3.3 Debuff 축 (7장: P4 + A3)

| # | ECardId | 분류 | T | 한글명 | 효과 요약 | 핵심 수치 | 중첩 정책 |
|---|---|---|---|---|---|---|---|
| 1 | `PlagueSlowBoost` | 보존 (Plague Spawner 추가로 활성화) | P | 역병의 손길 | Plague 종 SlowFactor ×0.75 (강한 둔화) | `_slowFactor=0.75` | 곱연산 누적 |
| 2 | `SpawnPlagues` | 보존 (Plague Spawner 추가로 활성화) | P | 역병 증식 | Plague 스포너 동시 출력 +1 | `+1` | 가산 누적 |
| 3 | `HeroPoisonAura` | 보존 | P | 독장판 | 영웅 발 밑 독장판 — 영역 내 1초당 5 DPS, 5s 지속 (영웅 발에 부착, 영웅 이동 시 따라 이동) | `_dps=5 _duration=5 _radius=1.25` | **지속시간 누적**: 잔여+5s |
| 4 | `HeroAttackDown` | 보존 | P | 약화의 저주 | 영웅 공격력 영구 ×0.75 | `_factor=0.75` | 곱연산 누적 (2픽=×0.5625) |
| 5 | `Fear` | 보존 | A | 공포 | 영웅 3s 도주 | `_duration=3` | 지속시간 누적 |
| 6 | `Bleed` | 보존 | A | 출혈 | 영웅 이동 시 1s당 HP -2%, 10s | `_ratio=0.02 _duration=10` | 지속시간 누적 |
| 7 | `Weaken` | 보존 | A | 무력화 | 영웅 공격력 ×0.5, 10s | `_factor=0.5 _duration=10` | 지속시간 누적 |

> `Slow` 카드는 어떻게 됐나? — 디버프 축 액티브 풀이 4장(Fear/Bleed/Weaken/Slow)이라 1장 과잉이었다. Slow 는 **Swarm 축으로 이동 + 리뉴얼** (§3.4 #5 `TimeStop` 자리 보존, Slow 는 §3.4 #7 신규 슬롯으로 재해석). 자세한 처리는 §6 잠정 매핑 정합화 참조.

### 3.4 Swarm 축 (7장: P4 + A3)

| # | ECardId | 분류 | T | 한글명 | 효과 요약 | 핵심 수치 | 중첩 정책 |
|---|---|---|---|---|---|---|---|
| 1 | `PhantomMoveSpeedBoost` | 보존 | P | 환령의 발걸음 | Phantom 종 이동속도 ×1.5 | `_speedMul=1.5` | 곱연산 누적 |
| 2 | `SpawnPhantoms` | 보존 | P | 환령 떼 | Phantom 스포너 동시 출력 +1 | `+1` | 가산 누적 |
| 3 | `SpawnWisps` | **축 이동** (Tank→Swarm) | P | 위스프 떼 | Wisp 스포너 동시 출력 +1 | `+1` | 가산 누적 |
| 4 | `SpawnerHaste` *(신규 3)* | 신규 | P | 던전 박동 | **모든 스포너** 의 스폰 주기 ×0.8 (영구) | `_periodMul=0.8` | 곱연산 누적 (2픽=×0.64, 3픽=×0.512) |
| 5 | `TimeStop` | 보존 | A | 시간 정지 | 영웅 5s 정지 | `_duration=5` | 지속시간 누적 |
| 6 | `SwarmRush` *(Multiply 자리 리뉴얼)* | **Multiply 대체** | A | 스웜 러시 | **Phantom 즉시 6마리 영웅 근처 소환** (캡 18 truncate) | `_count=6` | 가산 누적 (2픽 = 12마리, 캡 truncate) |
| 7 | `Slow` | **축 이동** (Debuff→Swarm) + 리뉴얼 | A | 던전의 점성 | 영웅 이동속도 ×0.5, 10s + **모든 몬스터 이동속도 ×1.3, 10s** | `_heroFactor=0.5 _monsterMul=1.3 _duration=10` | 지속시간 누적 |

> *Multiply 폐기 자리에 `SwarmRush` 신설*: 기존 Multiply 가 "광역 빌드에 절대적이라 다른 액티브 선택지를 압살"한 이유는 "최다 종 즉시 2배" 의 **빌드 누적 효과**에 있었다. 신 `SwarmRush` 는 **Phantom 고정 6마리 즉시 소환** 으로 *축이 정해진* 효과로 좁혀 압살을 막는다. Phantom Spawner 가 0개여도 액티브가 직접 SpawnMonster 호출로 작동하므로 활성화 보장.

> *Slow 리뉴얼 근거*: 단순 "영웅 둔화" 만으로는 Debuff 축 액티브 4장(Fear/Bleed/Weaken/Slow) 안에서 차별이 없었다. Swarm 축으로 이동하며 **이중 효과(영웅 -50% + 몬스터 +30%)** 로 전환 → Swarm 의 "머릿수로 압도" 정체성에 맞춤. 영웅이 느려진 동안 몬스터가 빨라져 둘러싸는 그림.

> *SpawnWisps 축 이동 근거*: Wisp 는 "느리고 작고 떼로 몰리는" 종(컨셉 §11.3). Tank 축의 "묶어 둔다"보다 Swarm 축의 "머릿수" 정체성에 더 가깝다. Tank 축의 Wisp 정체성은 `WispHpBoost`(HP 강화 = 묶어 두기)가 담당, Swarm 축은 `SpawnWisps`(수량 = 머릿수)가 담당하는 분담으로 정리.

### 3.5 라인업 통계 (균등 분배 검증)

| 축 | 패시브 | 액티브 | 합 | 보존 | 리뉴얼 | 축 이동 | 신규 |
|---|---|---|---|---|---|---|---|
| Tank | 4 | 3 | 7 | 4 (WispHpBoost · SpawnWraith · ReplaceWispsToWraith · IronWill) | 1 (Berserk→GuardianRage) | 1 (WraithDamageBoost: Dps→Tank, 효과도 HP로 리뉴얼) | 1 (WallOfWisps) |
| Dps | 4 | 3 | 7 | 5 (ReaperAtkSpeed · HexRangeBoost · SpawnReapers · ReplaceReapersToHex · Frenzy) | 0 | 1 (BloodThirst: Swarm→Dps) | 1 (MarkOfDeath) |
| Debuff | 4 | 3 | 7 | 7 (PlagueSlowBoost · SpawnPlagues · HeroPoisonAura · HeroAttackDown · Fear · Bleed · Weaken) | 0 | 0 | 0 |
| Swarm | 4 | 3 | 7 | 3 (PhantomMoveSpeedBoost · SpawnPhantoms · TimeStop) | 1 (Multiply→SwarmRush) | 2 (SpawnWisps: Tank→Swarm, Slow: Debuff→Swarm 리뉴얼) | 1 (SpawnerHaste) |
| **합계** | **16** | **12** | **28** | **19** | **2** | **4** | **3** |

> 검산: 16 = 4×4 ✓ / 12 = 4×3 ✓ / 28 = 16 + 12 ✓ / 보존 19 + 리뉴얼 2 + 축 이동 4 + 신규 3 = 28 ✓.

---

## 4. 12개 빌드 시너지 Tier 표 (Layer 1)

### 4.1 발동 규칙
- **카운트 대상**: 한 라운드 동안 픽한 카드 중 해당 축에 속한 카드의 **고유 카운트**가 아니라 **누적 픽 카운트** (= 같은 카드를 2번 픽하면 2장으로 카운트). 같은 카드 중복 픽으로도 임계에 도달 가능.
- **발화 시점**: 임계 도달 시 즉시 1회 발화. 같은 임계는 라운드당 1회만 (4장째, 5장째 픽해도 Tier1=3장 임계는 재발화 X).
- **누적 정책 (Tier1 → Tier2 도달 시)**: **유지 + 추가**. Tier1 효과는 그대로 영구 유지되고, Tier2 도달 시점에 Tier2 효과가 추가 등록. Tier3 도 동일. → 5장 빌드는 Tier1+Tier2 효과 동시 적용, 7장 빌드는 Tier1+Tier2+Tier3 모두 동시 적용.
- 적용 표면: `IBattleContext.RegisterMonsterTypeBuff` (영구 글로벌 버프) 사용. Layer 2 곱연산 시스템과 동일 표면 = 같은 종 강화 카드와 곱연산 누적.

### 4.2 Tier 효과 마스터 표

| 축 | Tier1 (3장) | Tier2 (5장) | Tier3 (7장) |
|---|---|---|---|
| **Tank** | Wisp·Wraith HP ×1.3 (글로벌 영구) | Wisp·Wraith Power ×1.2 (글로벌 영구) | **필드 캡 +6** (18→24, 영구) |
| **Dps** | Reaper·Hex Power ×1.3 | Reaper·Hex Cooldown ×0.8 (=공속 +25%) | Reaper·Hex Range ×1.3 |
| **Debuff** | Plague SlowFactor ×0.8 (강한 둔화 추가) | **HeroAttackDown 자동 등록** (영웅 공격력 ×0.85 영구) | **출혈 영구 등록** — 영웅 이동 시 1s당 HP -1%, 라운드 끝까지 |
| **Swarm** | Phantom·Wisp MoveSpeed ×1.3 | **모든 스포너 주기 ×0.85** (영구) | **모든 스포너 동시 출력 +1** (영구) |

### 4.3 수치 근거 — 컨셉 §8 밸런싱과 정합

- **Tier1 의도**: "내가 이 축을 잡고 있다" 는 가시적 신호. 평균 사망 5s 단축 정도 (76s → 71s 가정).
- **Tier2 의도**: "이 빌드의 색깔이 명확해진다". 평균 사망 추가 5s 단축 (71s → 66s).
- **Tier3 의도**: "진성 빌드 보상". 평균 사망 추가 5~8s 단축 (66s → 58~60s). 9픽 중 7픽을 한 축에 몰아야 도달 → 한 라운드에 한 축만 가능하고, 다른 축 시너지는 포기.
- 모든 보너스는 **양의 보너스** (영웅 사망 시간 단축 방향). QA 6차 ⑤ 클리어율 100% 문제는 본 기획만으로는 해소 안 됨 — 그건 본질적으로 *평균 사망이 86s 라도 영웅이 사망함* 의 문제. 본 기획은 **분포 분산 확장**으로 일부 빌드(약축 분산 픽)는 사망이 늦어지고 일부(7장 임계 빌드)는 빨라지게 만들어 **타임오버 발생 가능성을 처음으로 연다**. 5분 타임오버 1판 발생은 QA 7차에서 검증.

### 4.4 Tier3 특수 효과의 설계 의도

- **Tank Tier3 (필드 캡 +6)**: 컨셉 §3 의 글로벌 캡 18 은 빌드 다양성 천장 역할도 한다 (스폰 효과를 아무리 누적해도 캡에서 막힘). Tank 7장 빌드는 *모든 진로를 막는* 빌드이므로 캡 자체를 확장. **단 이 한 축에만**.
- **Dps Tier3 (Range ×1.3)**: Reaper(근접)·Hex(원거리) 모두 사거리 ↑ → 영웅이 도주(공포·시간정지 등)해도 닿는 거리. *깡딜의 절정*.
- **Debuff Tier3 (출혈 영구)**: 5분 동안 영웅 이동 시 1s당 1% (= 분당 60%). 영웅이 자기 위치에서 안 움직이면 무효 → AutoCombat AI 가 가장 가까운 몬스터로 이동하는 구조상 거의 항상 발동. *영웅이 살아있는 시간이 곧 패널티*.
- **Swarm Tier3 (스포너 출력 +1)**: 모든 스포너 동시 출력 +1 = Phantom 스포너 1마리 → 2마리, Wisp 1마리 → 2마리. 캡 18 에 막히지만 *재충전 속도*가 두 배가 되어 영웅 주변 밀집도 ↑.

### 4.5 시너지 적용 표면 (구현 요청)

- Tank Tier1·2 / Dps Tier1·2·3 / Debuff Tier1 / Swarm Tier1 = `IBattleContext.RegisterMonsterTypeBuff(EMonster, EMonsterStatKind, multiplier)` 호출 (해당 종 2개에 각각 1회씩).
- Tank Tier3 = **신규 표면 필요**: `IBattleContext.IncrementGlobalMonsterCap(int delta)` — BattleController 의 글로벌 캡 dict 를 +6.
- Debuff Tier2 = `HeroAttackDown` 카드와 동일 효과 — 신규 표면 불요, `HeroAttackDownEffect` 인스턴스를 Tier 클래스 안에서 직접 생성·Apply 호출.
  - **누적 정책**: 적용 표면은 **기존 `IBattleContext.ApplyHeroAura(new HeroAttackDownAura(_attacker, 0.85f), -1f)` 호출** — 신규 표면 없음. `HeroAttackDownAura.OnAttached` 가 매번 `PowerScale *= 0.85` 를 곱하므로 카드 픽으로 부착된 Aura(×0.75) 와 Tier 부착 Aura(×0.85) 가 동일 PowerScale 위에서 자연 곱연산 누적된다. 1픽 + Tier2 = ×0.6375, 2픽 + Tier2 = ×0.6375 × 0.75 = ×0.4781.
- Debuff Tier3 = **신규 표면 필요**: `IBattleContext.ApplyHeroAura(IHeroAura aura, float duration)` 에 `-1f` (무제한) 로 새 `EternalBleedAura` 등록 (기존 BleedAura 의 ratio 0.01 / 무제한 변형).
- Swarm Tier2 = **신규 표면 필요**: `IBattleContext.ScaleAllSpawnerPeriods(float mul)` — 모든 Spawner 의 `_spawnPeriod` 에 ×mul. (Swarm 신규 카드 `SpawnerHaste` 와 동일 표면 공유.)
- Swarm Tier3 = `IBattleContext.IncrementAllSpawnerOutputs(int delta)` — 신규 표면, 모든 Spawner 의 `_outputCount += delta`. Swarm Tier3 호출은 `(1)`.

→ 구현 요청사항 §10 에 신규 표면 4개 정리.

---

## 5. Plague Spawner 슬롯 결정

### 5.1 결정

| 항목 | 값 |
|---|---|
| 전환 대상 | `continuous-spawn-round.md §3.1` 의 **Spawner #4 (180°, 위치 (-14.0, 0.0))** |
| 전환 후 종 | Wisp → **Plague** |
| 스폰 주기 | **10.0s** (Wisp 9.0s 보다 약간 느림) |
| 초기 지연 | **1.5s** (기존 Wisp #4 의 1.5s 보존) |

### 5.2 근거

- **#1(0°) vs #4(180°) 중 #4 선택 이유**:
  - #1 은 영웅 등 뒤 / #4 는 영웅 정면 — 컨셉 §4.1 의 *영웅은 자동 이동* 구조에서 정면 스포너의 몬스터를 먼저 만난다. Plague 의 둔화는 **영웅이 둔화 상태인 시간을 길게 만드는 것이 핵심** 이므로 영웅이 더 자주 접촉하는 #4 가 효과 가시성에 유리.
  - Wisp 스포너가 2개(#1·#4)였던 구조에서 1개 남기면, **#1(0°) Wisp · #4(180°) Plague** 가 *대칭 위치*로 좌우 균형 유지. 시야상 한쪽이 비지 않음.
- **주기 10.0s 근거**:
  - Plague HP 50 / DPS 5 (컨셉 §11.3) — Wisp HP 200 보다 훨씬 약함.
  - 9.0s → 10.0s 로 약간 느리게 = 약한 종이 너무 많이 쌓이지 않도록.
  - Plague 의 둔화 효과는 *한 마리만 닿아도 발동* 이라 수량보다 *접촉 지속*이 핵심.
- **초기 지연 1.5s 보존 근거**:
  - 6개 스포너의 초기 지연이 0 / 0.5 / 1.0 / 1.5 / 2.0 / 2.5 로 균등 분산되어 시작 직후 몰림을 방지(`continuous-spawn-round.md §3.1`). #4 자리를 그대로 쓰면 이 분산이 유지됨.

### 5.3 갱신 후 Spawner 6개 표 (continuous-spawn-round.md §3.1 갱신용)

| # | 각도 | 위치 (x, z) | 종 | 스폰 주기 | 초기 지연 |
|---|---|---|---|---|---|
| 1 | 0° | (14.0, 0.0) | Wisp | 9.0s | 0.0s |
| 2 | 60° | (7.0, 12.124) | Reaper | 12.0s | 0.5s |
| 3 | 120° | (-7.0, 12.124) | Phantom | 6.0s | 1.0s |
| 4 | 180° | (-14.0, 0.0) | **Plague** | **10.0s** | 1.5s |
| 5 | 240° | (-7.0, -12.124) | Wraith | 20.0s | 2.0s |
| 6 | 300° | (7.0, -12.124) | Hex | 15.0s | 2.5s |

→ 종 분포: Wisp 1 / Reaper 1 / Phantom 1 / Plague 1 / Wraith 1 / Hex 1 — **6종 모두 1개씩** 균등. 컨셉 §11.3 의 6종 모두 필드에 등장.

---

## 6. 잠정 매핑 정합화 — 7장 균등 달성 경로

### 6.1 문제 정리 (요청서 §3 그대로)

| 축 | spec 잠정 매핑 | 패시브 | 액티브 | 문제 |
|---|---|---|---|---|
| Tank | WispHp·WraithDmg·SpawnWisps·SpawnWraith·ReplaceWispsToWraith / IronWill | 5 | 1 | 패시브 +1 / 액티브 −2 |
| Dps | ReaperAtkSpeed·HexRange·SpawnReapers·ReplaceReapersToHex / Frenzy | 4 | 1 | 액티브 −2 |
| Debuff | PlagueSlow·SpawnPlagues·HeroPoison·HeroAttackDown / Fear·Bleed·Weaken·Slow | 4 | 4 | 액티브 +1 |
| Swarm | PhantomMS·SpawnPhantoms / BloodThirst·TimeStop | 2 | 2 | 패시브 −2 / 액티브 −1 |

### 6.2 해결 (본 기획의 최종 배치)

| 카드 | 잠정 매핑 → 최종 매핑 | 사유 |
|---|---|---|
| `WraithDamageBoost` | Tank P 유지하되 **효과를 데미지 → HP 로 리뉴얼** | "탱커 = HP" 정체성 강화. 데미지 강화는 Dps Tier1 시너지로 흡수 |
| `SpawnWisps` | Tank P → **Swarm P 이동** | Wisp 는 "느리고 작고 떼" 종성격 — 수량은 Swarm 정체성. Tank 의 Wisp 정체성은 `WispHpBoost` 가 담당 |
| `BloodThirst` | Swarm A → **Dps A 이동** | "처치 시 회복"은 DPS 축이 영웅 HP 깎으며 자기 유지 — Dps 정체성과 일치 |
| `Slow` | Debuff A → **Swarm A 이동 + 리뉴얼** (이중 효과: 영웅 둔화 + 몬스터 가속) | Debuff 액티브 4장 압축, Swarm 액티브 채움. "영웅 느려지고 몬스터 빨라짐" = 스웜 정체성 |
| `Berserk` | spec 자살 구조 폐기 → **Tank A 로 리뉴얼** (`GuardianRageEffect`) | 자살 구조 해소, Tank 의 보호 액티브로 재해석 |
| `Multiply` | spec 삭제 → **Swarm A 자리에 `SwarmRush` 신설** | 광역 압살 폐기, *축이 정해진* Phantom 6마리 즉시 소환으로 좁힘 |
| **신규 1: `WallOfWisps`** | — → Tank A | Tank "묶어두기" 액티브 정체성. 영웅 주변 4방위 Wisp 4마리 즉시 |
| **신규 2: `MarkOfDeath`** | — → Dps A | "다음 5초간 영웅이 받는 데미지 ×1.5" — 깡딜 보조, 짧고 강함 |
| **신규 3: `SpawnerHaste`** | — → Swarm P | "모든 스포너 주기 ×0.8 영구" — Swarm 의 "수적 압박" 정체성 |

### 6.3 정합화 후 카운트 검증

각 축 7장(P4 + A3):

| 축 | Passive 4장 | Active 3장 |
|---|---|---|
| Tank | WispHpBoost, WraithDamageBoost(HP리뉴얼), SpawnWraith, ReplaceWispsToWraith | IronWill, WallOfWisps(신규), Berserk(GuardianRage 리뉴얼) |
| Dps | ReaperAtkSpeed, HexRangeBoost, SpawnReapers, ReplaceReapersToHex | Frenzy, BloodThirst, MarkOfDeath(신규) |
| Debuff | PlagueSlowBoost, SpawnPlagues, HeroPoisonAura, HeroAttackDown | Fear, Bleed, Weaken |
| Swarm | PhantomMoveSpeedBoost, SpawnPhantoms, SpawnWisps, SpawnerHaste(신규) | TimeStop, SwarmRush(Multiply대체), Slow(리뉴얼) |

→ 4 × (P4 + A3) = 28 ✓

---

## 7. 카드 중첩 정책 (Layer 2) 상세

### 7.1 정책 종류

| 정책 | 의미 | 적용 카드 |
|---|---|---|
| **곱연산 누적** | K픽 = 기준값^K | 종 글로벌 스탯 강화 (HP·Power·Cooldown·Range·MoveSpeed·SlowFactor 배율 카드) |
| **가산 누적** | K픽 = 기준값 ×K | Spawner 동시 출력 +N 카드, 즉시 소환 마릿수 N |
| **지속시간 누적** | 효과 진행 중 재픽 시 잔여+duration | 액티브 디버프·버프·오라 (효과량은 그대로) |
| **멱등** | 같은 결과로 수렴, 2픽 이상 효과 없음 | Spawner 출력 종 변경 카드 (이미 그 종이면 no-op) |
| **곱연산 누적 (영구)** | 영구 효과 배율 곱연산 | HeroAttackDown (영구 ×0.75 → 2픽 ×0.5625) |

### 7.2 카드별 명시 (28장 전체)

위 §3 표의 "중첩 정책" 컬럼 참조. 본 절은 **액티브 카드 중첩** (game-designer 결정 요청 사항) 만 별도 정리:

- **모든 액티브 카드의 기본 정책 = 지속시간 누적**. 효과량(데미지·둔화율·공속%)은 1픽 기준값 그대로, 효과 진행 중 재픽 시 잔여시간 + 카드의 base duration 만큼 연장.
- **예외 1 — `SwarmRush` / `WallOfWisps`**: 즉시 소환 카드는 지속시간이 없음. **가산 누적** (2픽 = 2배 마릿수, 캡 18 적용 truncate).
- **예외 2 — `Frenzy` / `IronWill` / `Berserk(GuardianRage)`**: 몬스터 글로벌 버프 시스템(`MonsterBuffService`) 위임 카드들. 시스템상 이미 *효과 진행 중 재호출 시 duration 갱신* 으로 동작 (기존 코드). 정책 그대로 = 지속시간 누적.
- **예외 3 — `MarkOfDeath`**: 영웅이 받는 데미지 ×1.5 효과 — `HeroAura` 패턴. 지속시간 누적 (잔여+5s).
- **예외 4 — `BloodThirst`**: 처치 시 회복 효과 — `BloodThirstService` 위임. 지속시간 누적 (잔여+30s).
- **예외 5 — `TimeStop` / `Fear`**: 영웅 행동 제약 강력. 5s / 3s 가 *반복 부여 시 픽률 ↑* 우려 있지만 본 기획은 지속시간 누적 일관 정책 유지 → 중첩 시 영웅 8s 정지 가능. *밸런스 모니터링 항목* (QA 7차에서 픽률 추적).

### 7.3 곱연산 누적의 표면 (이미 구현됨, 확인용)

`IBattleContext.RegisterMonsterTypeBuff(type, stat, multiplier)` 호출 시 기존 dict 값 × multiplier 로 누적. 본 기획의 모든 종 글로벌 스탯 카드 + 시너지 Tier 효과가 동일 표면 사용.

---

## 8. 시너지 UI 노출 — MVP 비주얼 안

### 8.1 카드 픽 팝업 (`CardSelectionPopup`) 갱신 항목

**현재**: 3장 카드만 가로 나열, 각 카드는 흰 배경 + 카테고리 색 테두리 + 한글 이름 + 한글 설명.

**갱신**: 팝업 상단에 **빌드 카운트 바** 신설. 4축 × 4셀 (= 4 행) 또는 가로 4셀 (= 1 행).

```
┌──────────────────────────────────────────────────┐
│  [Tank 2/3]  [Dps 1/3]  [Debuff 0/3]  [Swarm 3/5]│  ← 빌드 카운트 바
├──────────────────────────────────────────────────┤
│                                                  │
│  [카드 1]  [카드 2]  [카드 3]                     │  ← 카드 3택 (기존)
│                                                  │
└──────────────────────────────────────────────────┘
```

### 8.2 빌드 카운트 셀 명세

각 셀 한 칸:
- **폭/높이**: 가로 160 × 세로 36 px (4셀 × 160 = 640 폭, 팝업 폭 안쪽 패딩 -20 양쪽 → 팝업 폭 ≥ 680 권장).
- **배경**: 해당 축 키 색(§2 표) 의 30% 알파.
- **테두리**: 해당 축 키 색 100% 알파, 두께 2px.
- **텍스트** (CHText): `<축이름> N/<다음 임계>` — 예: `Tank 2/3`, `Dps 5/7`.
  - 폰트 색: 다음 임계 미달이면 검정 `#000000`, **임계 도달 직후 1픽 동안** = 해당 축 색으로 강조.
- **임계 도달 표시**: 텍스트 옆에 작은 사각형 마커. Tier1 도달 = `■` 1개, Tier2 = `■■`, Tier3 = `■■■`. 색은 해당 축 키 색.
  - 7장 (Tier3 도달) 시 표기: `Tank 7+ ■■■` (다음 임계 없음).

### 8.3 카드 자체 UI

- **테두리 색**: 해당 카드 축의 키 색 (§2 표) 그대로.
- **카드 헤더 1줄** (현재 한글 이름 위에 추가): `<축이름>` 1단어 + 작은 사각형 (해당 축 색) — 카드가 어느 축인지 즉시 식별.
  - 예시: `Tank ■  끈질긴 위스프  /  모든 위스프 HP +50%`
- **수치 표시**: 같은 카드를 이미 K번 픽한 상태면 카드 우측 상단에 작은 `×K+1` 배지 (검정 텍스트). 픽 시 K+1 픽 → Layer 2 누적 단계 시각화.

### 8.4 임계 발화 피드백

- 카드 픽 후 카운트 갱신 → 임계 도달 시 **빌드 카운트 바 셀이 0.3s 동안 펄스** (셀 배경 알파 30% → 80% → 30%).
- 동시에 화면 중앙 상단에 큰 토스트 텍스트 1.5s: `Tank 시너지 Tier 1 발동!` (해당 축 색).
- 사운드 hook 미연결 (MVP §8).

### 8.5 ChvjPackage 사용 (Rule 03 준수)

- 빌드 카운트 셀의 텍스트 = `CHText` (Rule 03 §3).
- 카드 버튼 = `CHButton` (기존 유지).
- 토스트 띄움 = `CHMUI.ShowUI(EUI.<신규 토스트 UI>)` — 신규 EUI 값 `SynergyToast` 추가.
- 펄스 애니메이션 = `CHText` 의 color tween 또는 Coroutine. 별도 인프라 불요.

---

## 9. 엣지 케이스 / 밸런스 노트

### 9.1 Spawner 0개 시 시너지 효과

- **Dps Tier1·2·3 (Reaper·Hex 강화)** — Reaper 스포너가 Hex 로 교체된 빌드(ReplaceReapersToHex 픽)에서 Reaper 가 필드에 없을 수 있음. RegisterMonsterTypeBuff 는 "이후 스폰 + 현재 필드 소급" 이므로 *Reaper 가 0마리면 무영향, 다시 Reaper 가 스폰되면 자동 반영* — 시스템상 이미 안전. 별도 처리 불요.
- **Tank Tier1·2 (Wisp·Wraith 강화)** — 동일 처리. 모든 Wisp 가 Wraith 로 교체된 빌드에서 Wisp 0마리 → 무영향, Wraith 만 강화됨 → 의도된 결과.
- **Debuff Tier1 (Plague SlowFactor)** — Plague Spawner 가 1개 보장되므로 항상 작동.
- **Swarm Tier3 (모든 스포너 동시 출력 +1)** — Plague Spawner 가 1개 → 동시 출력 2 가능. 캡 18 영향 미미 (Plague Power 2).

### 9.2 같은 카드 K번 픽 → 시너지 카운트 K번 누적

- spec D6·D7 의 해석: "같은 카드 K번 픽은 빌드 시너지 카운트와 카드 중첩 카운트를 동시에 올린다". 본 기획 채택.
- 예: `WispHpBoost` 를 3픽 → Layer 2: HP ×1.5³ = ×3.375 / Layer 1: Tank 카운트 +3 → Tier1 즉시 도달.
- → *Tank 빌드의 빠른 진입 경로*가 열림. 디자인 의도와 일치.

> **[트레이드오프 T2 — 사용자 승인 게이트 명시 항목]**: 같은 카드 K번 픽으로 Tier 즉시 발동을 *허용* 한다. 대안은 "고유 카드 K장만 카운트" 였으나, 본 기획은 **누적 픽 카운트** 를 채택. 효과: 단일 카드 반복 픽 빌드(예: WispHpBoost ×3) 가 1·2·3턴 안에 Tier1 시너지를 발화시켜 *빠른 빌드 정체성 확립* 을 만든다. 부작용: *카드 종류 다양성 ≠ 빌드 다양성* 이라는 가설을 수용해야 하며, 같은 카드를 7번 픽하면 Tier3 까지 즉시 도달한다. 사용자가 "고유 카드 K장 카운트" 로 정책을 뒤집고 싶으면 본 기획 §4.1 / §7 / §9.2 / §9.3 를 함께 갱신해야 한다.

### 9.3 9픽 한계 vs 임계

- 한 라운드 9 패시브 + 9 액티브 = 최대 18픽. 단 패시브와 액티브 풀이 분리되어 있고 각 축은 P4 + A3 → **한 축에 9 패시브 + 9 액티브 다 몰 수 없음** (해당 축 카드 풀이 4 + 3 = 7장이라 풀 소진).
- 그러나 같은 카드 중복 픽이 가능하면 **단일 축 9 패시브 + 9 액티브 = 18픽 한 축** 도 이론상 가능. 그러면 Tier3 (7장 임계) 는 도달.
- 9패시브 모두 Tank → Tank 카운트 9 → Tier1·2·3 모두 발동. 추가 시너지 없음 (4장째 도달부터 Tier 효과는 *유지+추가* 이므로 카드 효과 본체만 누적).
- → 빌드 다양성 ≠ 카드 종류 다양성. 같은 카드 반복 픽으로 누적 강화하는 것도 *빌드*. 정합.

### 9.4 다른 축 픽으로 임계 못 도달 시

- 9 패시브를 4축에 분산(예: T2 D2 De2 Sw3) → 어느 축도 3장 미달, Tier1 0개 발동 → 시너지 보너스 0.
- 이 빌드도 **클리어는 가능** (베이스라인 76s 영웅 사망). 시너지 없는 *기본 빌드*. 의도된 선택지.

### 9.5 캡 24 (Tank Tier3) + Swarm Tier3 (스포너 출력 +1) 동시

- 한 라운드는 7+α 픽 후 7+β 픽 못 함 (한 축 7장 임계 도달은 9픽 중 7픽 소모 → 다른 축은 최대 2장만). 두 Tier3 동시 발동은 *원리상 불가*.
- 시뮬레이션 코드에서 안전 가드: `BuildSynergyService` 가 *임계 도달 1회만 발화* 이고 효과는 영구이므로, *논리상 동시 발화 가능성 0* → 별도 가드 불요.

### 9.6 SpawnerHaste 와 Swarm Tier2 (스포너 주기 ×0.85) 동시

- `SpawnerHaste` 1픽 = 모든 스포너 주기 ×0.8 / Swarm Tier2 도달 = 추가 ×0.85.
- 곱연산 누적: ×0.8 × ×0.85 = ×0.68 → 9.0s 주기 → 6.12s.
- 너무 빠르지 않은지? Phantom 6.0s × 0.68 = 4.08s → *밸런스 모니터링 항목*. QA 7차에서 캡 포화 시간 측정.

**최악 시나리오 수치 예측 (SpawnerHaste 다중 픽 + Swarm Tier2)**:
- `SpawnerHaste` 3픽 곱연산 = ×0.8³ = ×0.512. Swarm Tier2 추가 ×0.85 → 합 ×0.512 × ×0.85 = ×0.435.
- Phantom 스포너 적용: 6.0s × 0.435 = **2.61s 주기**. 캡 18 도달 시간이 라운드 초반 수십 초로 폭주할 가능성 = §1 "디자인 원칙" 의 *한 축 5장 임계는 평균 사망 5~10s 단축* 가정의 범위를 7+ 픽 + SpawnerHaste 중복 픽 시 깨뜨릴 위험.
- **후속 조정 사전 의도** (QA 7차에서 *캡 포화를 5초 안에 만들면* 본 기획 가정 위반 → 다음 둘 중 하나로 후속 조정):
  - **(a) 우선** — `SpawnerHaste` 곱연산 누적 캡 (3픽까지만 누적, 4픽 이상은 효과 없음 + 카드 픽 카운트만 +1 → Layer 1 시너지 카운트는 계속 누적).
  - (b) 대안 — Swarm Tier2 의 ×0.85 → ×0.92 완화.
- **사전 의도 = (a) 우선** 근거: (b) 는 빌드 정체성 효과량 자체를 낮춰 Tier2 의 "이 빌드의 색깔이 명확해진다" 의도를 약화 — 약한 빌드가 됨. (a) 는 단일 카드 중첩의 캡 도입으로 *균형* 만 회복하면서 Swarm Tier2 효과량은 유지.
- 본 기획 시점에는 (a) 를 **선반영하지 않는다** — QA 7차 데이터로 *실제로* 캡 포화 5초가 발생할 때 발동.

### 9.7 QA 6차 권고와 본 기획 결합

| QA 권고 | 본 기획에서 처리 |
|---|---|
| ① 1라운드 회귀 방지 (≥30s) | 변화 없음. 패스 유지. |
| ② 액티브 발화 (≥1픽) | 변화 없음. 패스 유지. |
| ③ 평균 사망 ≥80s (Hero HP 4600) | 본 기획과 독립. Hero HP 4600 은 이미 적용된 상태. ③ 통과 |
| ④ 5분 타임오버 ≥1판 | 본 기획이 처음으로 **분포 분산 확장** 을 만든다 (약축 분산 픽 빌드는 사망 시간 ↑). QA 7차 검증 항목. |
| ⑤ 클리어율 ≤80% | ④와 같은 맥락. QA 7차 검증. |

→ **본 기획의 가설**: 7장 임계 시너지 + 약축 분산 픽 빌드의 격차가 분포 분산을 5s 이상 확장하면 ④⑤ 통과 가능성이 처음으로 열린다. 단정 못 함, QA 7차 후 결정.

> **[트레이드오프 T1 — 사용자 승인 게이트 명시 항목]**: 본 기획만으로는 QA 6차 권고 ⑤ (클리어율 ≤80%) 가 **미해결** 이다. 본 기획의 1차 목표는 *빌드 다양성·정체성 확립* 이지 *난이도 상승* 이 아니다 (§1 디자인 원칙 마지막 항목). ⑤ 해결은 별도 밸런스 사이클 (Hero HP 추가 인상 / Hero 공격력 상향 / 스포너 주기 조정 등) 의 일이며, 본 기획 적용 후 QA 7차 데이터를 바탕으로 후행 처리한다. 사용자가 "본 기획에서 ⑤ 까지 함께 해결해야 한다" 로 요구를 변경하면 본 기획 §4.2 Tier3 효과량 / Hero HP / 스포너 주기를 함께 재산정해야 한다 (현재 범위 외).

---

## 10. 구현 요청사항 (gameplay-programmer 용)

### 10.1 신규 Enum 값 (Rule 02 §8, Rule 03 §2)

**`CommonEnum.cs` — `ECardId`** (끝에 3개 추가, int 직렬화 정합):

```
WallOfWisps,    //# 신규 25 — Tank A
MarkOfDeath,    //# 신규 26 — Dps A
SpawnerHaste,   //# 신규 27 — Swarm P
```

> `Multiply` enum 자리 (값 20) 는 **삭제하지 않고 보존**. SO/json/풀에서만 ref 제거. (spec/plan 동일 정책.)
> `Berserk` enum 자리 (값 24) 도 **보존**. 효과 클래스만 `GuardianRageEffect` 로 교체.
>
> **Berserk enum 값 위 주석 규약 (Rule 02 §1 `//#`)**: `CommonEnum.cs` 의 `ECardId.Berserk` 라인 위에 다음 한 줄을 *반드시* 둔다 — 향후 코드 read·grep 시 카드 본질 혼동 방지.
> ```csharp
> //# GuardianRage (구 Berserk 자리 — 카드 리뉴얼 v0.6 으로 효과·displayName 교체, enum 값명만 보존)
> Berserk,    //# 값 24
> ```
> 동일 정책: `Multiply` enum 값 위에도 `//# 폐기 (카드 리뉴얼 v0.6 — SO/풀 ref 제거, enum 자리만 보존)` 주석 한 줄.

**`CommonEnum.cs` — `EBuildAxis`** (spec/plan 정의 그대로):

```
public enum EBuildAxis
{
    Tank,    //# 0
    Dps,     //# 1
    Debuff,  //# 2
    Swarm,   //# 3
}
```

`ECardCategory` Enum 삭제 (plan Task 4).

**`CommonEnum.cs` — `EUI`** (시너지 토스트 추가):

```
SynergyToast,   //# 시너지 임계 발화 토스트 UI (1.5s 자동 close)
```

> 에셋 키 일치: `Assets/_Lair/Art/UI/SynergyToast.prefab` (Addressable 주소 = enum 값명).

**`CommonEnum.cs` — `EMonsterBuff`** (Frenzy/IronWill/BerserkPower 보존 + `GuardianRage` 신규 추가):

> 검토했던 대안 옵션 — (A) `GuardianRage` buff 신규 추가, (B) IronWill + Hp 강화 두 buff 합성으로 처리, (C) HeroAura 패턴 재활용. **결정: 옵션 (A)**. 근거: (B) 는 적용 종 한정(Wisp·Wraith)이 IronWill 의 *전체 종 적용* 의미와 충돌, (C) 는 영웅에 부착되는 Aura 가 몬스터 강화에 부적합. (A) 가 적용 종 한정 + 두 스탯(HP·받는데미지) 동시 적용 + 영구 vs 시한 분리가 가장 깔끔.

**결정**: 옵션 (A) — `EMonsterBuff.GuardianRage` 신규 추가.

```
public enum EMonsterBuff
{
    Frenzy,        //# 공격속도 ↑ (전체 종)
    IronWill,      //# 받는 데미지 ↓ (전체 종)
    BerserkPower,  //# 데미지 ↑ (전체 종) — 본 기획에서 Berserk 카드 폐기로 미사용, enum 자리 보존
    GuardianRage,  //# 신규 — Tank 한정 ({Wisp, Wraith}): 받는 데미지 ×0.5 + HP ×2.0
}
```

**GuardianRage 의 적용 종 = `{ EMonster.Wisp, EMonster.Wraith }` 고정. 이는 디자인 결정이지 시스템 결정이 아니다.** 본 기획 §1 "빌드 정체성" 원칙(Tank = Wisp·Wraith 강화 축) 의 직접 귀결이며, 이 필터가 빠지면 GuardianRage 가 *모든 몬스터* 의 HP×2 + 받는데미지×0.5 트럼프 카드가 되어 4축 분리가 깨진다.

`MonsterBuffService` 의 구현 옵션 (어느 쪽을 택해도 결과는 동일):
- 옵션 (i): `MonsterBuffService.ApplyBuff` 시그니처에 `IReadOnlyList<EMonster> targetTypes` 인자 추가 — 호출자(GuardianRageEffect)가 명시.
- 옵션 (ii): `MonsterBuffService` 내부에 `EMonsterBuff → IReadOnlyList<EMonster>` 매핑 dict 를 두고 buff 종류로 자동 필터.

→ 옵션 (i) 와 (ii) 의 선택은 gameplay-programmer 의 구현 결정 영역. 단 **적용 종 집합 자체는 디자인 결정** 으로 본 기획이 `{Wisp, Wraith}` 로 단정한다. 다른 종에 GuardianRage 가 적용되면 본 기획 위반.

> 동일 정책이 §10.4 `MarkOfDeathEffect` 의 IHealth 받는 데미지 배율 표면에도 적용된다 — 표면 신규 추가 자체가 디자인 단정이지 회색지대 아님 (§10.4 참조).

### 10.2 신규 Interface 표면 (`IBattleContext` 확장)

기존 표면(plan Task 4 정의: `RegisterCardPick(EBuildAxis)`, `GetBuildCount(EBuildAxis)`) 외 본 기획에서 신규 추가 필요:

```csharp
//# Tank Tier3 — 필드 글로벌 캡을 delta 만큼 영구 증가 (음수 입력은 무시).
//# 기준값 18 → delta=6 입력 시 24.
void IncrementGlobalMonsterCap(int delta);

//# Swarm 카드 SpawnerHaste / Swarm Tier2 — 모든 Spawner 의 _spawnPeriod 에 mul 곱연산 (영구).
//# mul < 1 = 가속 (주기 단축), mul > 1 = 감속.
void ScaleAllSpawnerPeriods(float mul);

//# Swarm Tier3 — 모든 Spawner 의 _outputCount 를 delta 만큼 가산 (영구).
void IncrementAllSpawnerOutputs(int delta);

//# Debuff Tier3 — 영구 출혈 오라 등록 (라운드 끝까지). 본 표면은 이미 ApplyHeroAura(aura, -1f) 로 가능 →
//# 신규 표면 불요. 신규 IHeroAura 구현체 EternalBleedAura 만 추가.

//# (시너지 적용 표면) — Wisp+Wraith 두 종에 같은 배율 등록 등 편의 호출 없이,
//# 기존 RegisterMonsterTypeBuff 를 종별 2회 호출하는 방식 채택. 별도 표면 불요.
```

### 10.3 신규 IBuildSynergyTier 구현체 12개

plan Task 11 의 12 클래스. 각 클래스의 Apply(IBattleContext ctx) 본문은 §4.2 표 그대로:

- `TankSynergyTier1` — ctx.RegisterMonsterTypeBuff(Wisp, Hp, 1.3) + (Wraith, Hp, 1.3)
- `TankSynergyTier2` — ctx.RegisterMonsterTypeBuff(Wisp, Power, 1.2) + (Wraith, Power, 1.2)
- `TankSynergyTier3` — ctx.IncrementGlobalMonsterCap(6)
- `DpsSynergyTier1` — ctx.RegisterMonsterTypeBuff(Reaper, Power, 1.3) + (Hex, Power, 1.3)
- `DpsSynergyTier2` — ctx.RegisterMonsterTypeBuff(Reaper, Cooldown, 0.8) + (Hex, Cooldown, 0.8)
- `DpsSynergyTier3` — ctx.RegisterMonsterTypeBuff(Reaper, Range, 1.3) + (Hex, Range, 1.3)
- `DebuffSynergyTier1` — ctx.RegisterMonsterTypeBuff(Plague, SlowFactor, 0.8)
- `DebuffSynergyTier2` — new HeroAttackDownEffect { _factor=0.85 }.Apply(ctx) (효과량 0.75 → 0.85 로 약화 인스턴스)
- `DebuffSynergyTier3` — ctx.ApplyHeroAura(new EternalBleedAura(0.01f), -1f)
- `SwarmSynergyTier1` — ctx.RegisterMonsterTypeBuff(Phantom, MoveSpeed, 1.3) + (Wisp, MoveSpeed, 1.3)
- `SwarmSynergyTier2` — ctx.ScaleAllSpawnerPeriods(0.85f)
- `SwarmSynergyTier3` — ctx.IncrementAllSpawnerOutputs(1)

> `EternalBleedAura` 는 `BleedAura` 의 *ratio 0.01 / duration -1 (무제한)* 변형. **디자인 결정**: ratio 0.01 / 무제한 지속 / 영웅 이동 시 발동 조건은 본 기획이 단정. **시스템 구현 결정 영역**: 기존 `BleedAura` 의 ratio 필드 파라미터화로 재사용할지 신규 `EternalBleedAura` 클래스를 만들지의 선택.

### 10.4 카드 효과 클래스 — 신규/리뉴얼 5개

| 신규/리뉴얼 | 클래스명 | 필드 | Apply 본문 |
|---|---|---|---|
| 신규 | `WallOfWispsEffect` | `_count=4`, `_radius=2.5f` | 영웅 transform 위치 기준 4방위(0°/90°/180°/270°) 에 `_radius` 거리에서 ctx.SpawnMonster(EMonster.Wisp, pos) 를 `_count` 회. 캡 검사는 SpawnMonster 내부 (기존 글로벌 캡 로직). |
| 신규 | `MarkOfDeathEffect` | `_dmgTakenMul=1.5f`, `_duration=5f` | 영웅에 신규 `MarkOfDeathAura` (IHeroAura 패턴) 부착 — Mark 부착 중 영웅 받는 데미지 ×1.5. `ApplyHeroAura(new MarkOfDeathAura(hero, _dmgTakenMul), _duration)`. **본 표면은 디자인 결정**: §10.2 와 동일 격으로 단정 — `Hero` 의 받는 데미지 배율 적용 경로를 본 기획에서 신규 도입한다 (Mark 부착 중 영웅 받는 데미지 ×1.5 효과를 만들기 위한 *기획서 요청사항*). 구현 옵션 둘 중 어느 쪽이든 결과는 동일: (i) `IHealth.TakeDamage(int amount)` → `IHealth.TakeDamage(int amount, float scale = 1f)` 시그니처 확장, (ii) `Hero` 에 `DamageTakenScale` 필드 신규 도입 + `MarkOfDeathAura` 가 그 필드에 곱연산 누적/복원. 옵션 선택은 gameplay-programmer 의 구현 결정 영역, 단 **×1.5 배율과 Mark 부착 중 적용 보장은 디자인 결정**. |
| 신규 | `SpawnerHasteEffect` | `_periodMul=0.8f` | ctx.ScaleAllSpawnerPeriods(_periodMul) |
| Multiply→대체 | `SwarmRushEffect` | `_count=6` | 영웅 transform 위치에서 _count 회 ctx.SpawnMonster(EMonster.Phantom, pos). 캡 truncate 는 SpawnMonster 내부. |
| Berserk→리뉴얼 | `GuardianRageEffect` | `_duration=15f` (배율 HP×2.0 / 받는데미지×0.5 + 적용 종 `{Wisp, Wraith}` 는 `MonsterBuffService` GuardianRage case 내 상수 — §3.1 SO 노출 정책과 동일 패턴, §10.1 적용 종 디자인 단정 참조) | `ctx.AddMonsterBuff(EMonsterBuff.GuardianRage, _duration)`. `MonsterBuffService` 의 GuardianRage case 가 적용 종 `{EMonster.Wisp, EMonster.Wraith}` 에만 HP×2.0 + 받는 데미지×0.5 영구 곱연산 적용. 적용 종 집합은 **디자인 단정** (§10.1), 구현 옵션 (i)/(ii) 는 시스템 결정. |
| WraithDamageBoost 리뉴얼 | `WraithDamageBoostEffect` (이름 유지) | `_hpMul=1.5f` (구 `_mul=1.5` 데미지) | ctx.RegisterMonsterTypeBuff(EMonster.Wraith, EMonsterStatKind.Hp, _hpMul). 효과 본문이 Hp 로 바뀜. 필드 재명명은 안전(기존 _mul 도 1.5 였음, SO 재저장 필요). |

### 10.5 카드 SO 마이그레이션 (28장)

기존 24장(Multiply 제외) `.asset` 의 `_category` 필드 (4값) 를 `_axis` 필드 (EBuildAxis, 4값) 로 마이그레이션. 매핑은 §3 표 그대로:

| ECardId | 새 _axis (int) | EBuildAxis |
|---|---|---|
| WispHpBoost | 0 | Tank |
| WraithDamageBoost | 0 | Tank (이전 분류와 다름 — 효과도 HP 로 리뉴얼) |
| ReaperAtkSpeed | 1 | Dps |
| HexRangeBoost | 1 | Dps |
| PlagueSlowBoost | 2 | Debuff |
| PhantomMoveSpeedBoost | 3 | Swarm |
| SpawnWisps | 3 | Swarm (이전 Tank 같았으나 Swarm 이동) |
| SpawnWraith | 0 | Tank |
| SpawnReapers | 1 | Dps |
| SpawnPlagues | 2 | Debuff |
| SpawnPhantoms | 3 | Swarm |
| ReplaceWispsToWraith | 0 | Tank |
| ReplaceReapersToHex | 1 | Dps |
| HeroPoisonAura | 2 | Debuff |
| HeroAttackDown | 2 | Debuff |
| Fear | 2 | Debuff |
| Bleed | 2 | Debuff |
| Weaken | 2 | Debuff |
| Slow | 3 | Swarm (이전 Debuff에서 이동 + 효과 리뉴얼) |
| Frenzy | 1 | Dps |
| BloodThirst | 1 | Dps (이전 Swarm 같았으나 Dps 이동) |
| IronWill | 0 | Tank |
| TimeStop | 3 | Swarm |
| Berserk | 0 | Tank (효과 클래스 GuardianRage 로 교체, displayName "수호자의 분노") |

신규 3장 SO:
- `Assets/_Lair/Art/Cards/Items/WallOfWisps.asset` — `_id=25, _axis=0, _displayName="위스프 장벽", _description="영웅 주변에 위스프 4마리 즉시 소환"`
- `Assets/_Lair/Art/Cards/Items/MarkOfDeath.asset` — `_id=26, _axis=1, _displayName="죽음의 표식", _description="다음 5초간 영웅이 받는 데미지 +50%"`
- `Assets/_Lair/Art/Cards/Items/SpawnerHaste.asset` — `_id=27, _axis=3, _displayName="던전의 박동", _description="모든 스포너 주기 -20% (영구)"`

**효과 SerializeReference**: 각 SO 의 `_effect` 필드는 위 §10.4 클래스의 인스턴스. 수치는 §3 표·§10.4 표 그대로.

**삭제 (Multiply)**:
- `Assets/_Lair/Art/Cards/Items/Multiply.asset` (+ `.meta`)
- `Assets/_Lair/Scripts/Card/Effects/MultiplyEffect.cs` (+ `.meta`)

### 10.6 SpawnerConfig (씬 Spawner 인스펙터 값 수정)

`Assets/_Lair/Scenes/Battle.unity` 의 Spawner #4 (180°) 컴포넌트:

```yaml
_outputType: 4         # EMonster.Plague (구: 0 = Wisp)
_spawnPeriod: 10       # 구: 9
_initialDelay: 1.5     # 보존
```

> Spawner #4 의 식별은 GameObject 이름 또는 Transform 위치 (-14.0, ?, 0.0) 으로. 정확 GameObject 경로는 gameplay-programmer 가 씬 검수 시 확정.

### 10.7 CardPool 갱신

- `Assets/_Lair/Art/Cards/CardPool_Passive.asset` — 카드 ref 15 → **16** (신규 `SpawnerHaste` 추가, 그 외 보존+축이동만이라 ref 카운트는 +1).
- `Assets/_Lair/Art/Cards/CardPool_Active.asset` — 카드 ref 10 → **12** (Multiply ref 제거, 신규 `WallOfWisps`·`MarkOfDeath`·`SwarmRush` 3개 추가 → 순 +2).

→ **검산 (§3.5 라인업 통계 표 기준 — SO 파일 카운트 기준 합산)**:
>
> **패시브 풀 16장**:
> - 보존 (SO 그대로) = 13장: WispHpBoost · SpawnWraith · ReplaceWispsToWraith · ReaperAtkSpeed · HexRangeBoost · SpawnReapers · ReplaceReapersToHex · PlagueSlowBoost · SpawnPlagues · HeroPoisonAura · HeroAttackDown · PhantomMoveSpeedBoost · SpawnPhantoms
> - 축이동 (SO 보존, `_axis` 필드만 변경) = 2장: WraithDamageBoost (Dps→Tank, 효과 필드도 리뉴얼) · SpawnWisps (Tank→Swarm)
> - 신규 (SO 신규 생성) = 1장: SpawnerHaste
> - 합계 = 13 + 2 + 1 = **16** ✓
>
> **액티브 풀 12장**:
> - 보존 (SO 그대로) = 6장: IronWill · Frenzy · Fear · Bleed · Weaken · TimeStop
> - 축이동 (SO 보존, `_axis` 필드만 변경) = 2장: BloodThirst (Swarm→Dps) · Slow (Debuff→Swarm, 효과도 리뉴얼하므로 `_effect` 필드도 변경)
> - 리뉴얼 (SO 보존, `_effect` 필드 클래스 교체) = 1장: Berserk (효과 BerserkPowerEffect → GuardianRageEffect, displayName "수호자의 분노")
> - 신규 (SO 신규 생성) = 3장: WallOfWisps · MarkOfDeath · SwarmRush (SwarmRush 는 Multiply.asset 자리 교체 — Multiply.asset 은 §10.5 에서 삭제하므로 SwarmRush 는 신규 카운트)
> - 합계 = 6 + 2 + 1 + 3 = **12** ✓
>
> **풀 ref 변경 검산**:
> - Passive ref: 15 + 1 (SpawnerHaste 추가) = 16 ✓
> - Active ref: 10 − 1 (Multiply 제거) + 3 (WallOfWisps · MarkOfDeath · SwarmRush 추가) = 12 ✓
>
> **28장 총합 검산**:
> - 패시브 16 + 액티브 12 = 28 ✓
> - SO 파일 카운트 기준 총합: 보존 19 (= 패시브 보존 13 + 액티브 보존 6) + 축이동 4 (= 패시브 축이동 2 + 액티브 축이동 2) + 리뉴얼 1 (= 액티브 리뉴얼 1, Berserk→GuardianRage) + 신규 4 (= 패시브 신규 1 + 액티브 신규 3) = 19 + 4 + 1 + 4 = 28 ✓
>
> > **§3.5 통계 표와의 관계**: §3.5 표는 *카드 기준* (28장 라인업의 변화 분류 — 보존 19 / 리뉴얼 2 / 축이동 4 / 신규 3 = 28). 본 §10.7 검산은 *SO 파일 기준* (보존 19 / 축이동 4 / 리뉴얼 1 / 신규 4 = 28). 보존 19 는 양쪽 표에서 일치. 리뉴얼/신규 카운트 차이는 SwarmRush 의 분류만 다름 — §3.5 는 *Multiply 자리 리뉴얼* (리뉴얼 2 = Berserk + SwarmRush), §10.7 은 *SO 신규 생성* (신규 4 = WallOfWisps + MarkOfDeath + SpawnerHaste + SwarmRush). 두 표 모두 합계 28 일치.

### 10.8 LairCardPrefabBuilder 4축 색 매핑

`Assets/_Lair/Editor/LairCardPrefabBuilder.cs`:

```csharp
private static readonly Dictionary<EBuildAxis, Color> AxisBorderColor = new Dictionary<EBuildAxis, Color>
{
    { EBuildAxis.Tank,   new Color(0.133f, 0.773f, 0.369f, 1f) },  //# #22C55E
    { EBuildAxis.Dps,    new Color(0.937f, 0.267f, 0.267f, 1f) },  //# #EF4444
    { EBuildAxis.Debuff, new Color(0.659f, 0.333f, 0.969f, 1f) },  //# #A855F7
    { EBuildAxis.Swarm,  new Color(0.122f, 0.157f, 0.220f, 1f) },  //# #1F2937
};
```

기존 7카테고리 색 dict 삭제.

### 10.9 cards.json / card_pools.json

- `cards.json` 항목 25→28, 필드 `category` 제거, 필드 `axis` 추가 ("Tank|Dps|Debuff|Swarm"). 신규 3장 항목 추가, Multiply 항목 삭제.
- `card_pools.json` Passive 16개 / Active 12개.

수치는 §3 표·§10.4 표 그대로.

---

## 11. 컨셉서 / 기존 기획서 갱신 항목 (Phase 3)

### 11.1 `project_lair_concept.md`

- **§4.2** — 변경 없음 (HP 10% / 30s 트리거 유지).
- **§5.2** — 시너지 방향성을 4축 × 3Tier 로 재정렬:
  - 기존 "언데드 5+ → 50% 부활" / "동족 → 데미지 +30%" / "둔화+광역 → 광역 데미지↑" 예시 폐기
  - 본 기획 §4.2 표를 그대로 인용 (12개 Tier 효과)
- **§11.3** — 패시브 15장 / 액티브 10장 표 → **본 기획 §3 표** 로 교체 (28장 4축 × 7장).
- **§11.4** — 카드 테두리 색 매핑:
  - 기존 7색 (강화 초록 / 추가 파랑 / 교체 주황 / 환경 보라 / 저주 빨강 / 버프 노랑 / 와일드 무지개) → **4색** (Tank 초록 / Dps 빨강 / Debuff 보라 / Swarm 검정)
  - 헥스 코드 §2 표 그대로
- **변경 이력 v0.6** 추가:
  ```
  - v0.6 (2026-05-31): 카드 전체 리뉴얼. 25장 → 28장. 카테고리 7종 → 4축(Tank/Dps/Debuff/Swarm). 2-Layer 시너지(빌드 카운트 임계 3/5/7 + 카드 중첩) 도입. Plague Spawner 1개 추가(Spawner #4: Wisp→Plague). Multiply 카드 삭제, Berserk 효과 GuardianRage 로 리뉴얼, 신규 3장(WallOfWisps/MarkOfDeath/SpawnerHaste) 추가. 정합: spec docs/superpowers/specs/2026-05-31-card-renewal-design.md, plan docs/superpowers/plans/2026-05-31-card-renewal.md, 기획서 docs/design/card-renewal.md.
  ```

### 11.2 `continuous-spawn-round.md`

- **§3.1** — Spawner #4 행 갱신 (본 기획 §5.3 표).
- **§5** — 글로벌 캡 18 행: **Tank Tier3 시너지 발동 시 영구 +6 (= 24)** 부연 한 줄.
- **§7** — "Plague 는 초기 Spawner 6개에 포함되지 않는다 … no-op 이다 … 의도된 설계" 문구 **삭제** 또는 다음으로 갱신:
  > 카드 리뉴얼 v0.6 (2026-05-31) 부터 Plague Spawner 1개(슬롯 #4) 가 기본 배치된다. SpawnPlagues·PlagueSlowBoost 두 카드는 *활성화* 상태로, Debuff 빌드 축의 작동 전제다.

---

## 12. Self-Review

### 12.1 design-reviewer 리뷰 반영 결과 (v0.6.1 → v0.6.2, 2026-05-31)

**2차 리뷰 BLOCKER 반영 (v0.6.2)**:
- **[N1] §4.5 Debuff Tier2 표면명 정정** → `IBattleContext.RegisterHeroAttackBuff` (실재하지 않는 표면) 단정을 제거하고 **실제 표면 `IBattleContext.ApplyHeroAura(new HeroAttackDownAura(_attacker, 0.85f), -1f)`** 로 정정. `HeroAttackDownAura.OnAttached` 가 매번 `PowerScale *= 0.85` 곱하는 메커니즘 명시. 누적 예시: 1픽+Tier2 = ×0.6375, 2픽+Tier2 = ×0.4781.
- **[N2] §3.5 라인업 통계 표 행 합 정정** → 보존 컬럼을 카드별 분류 기준에 맞춰 재산정 (Tank 5→4 / Dps 4→5 / Debuff 7 보존 / Swarm 2→3, 합계 18→19). 가로행 모두 7 정합. 검산 주석에서 "Multiply 폐기 1 추가" 제거 — 보존 19 + 리뉴얼 2 + 축이동 4 + 신규 3 = 28 로 직행. §10.7 끝의 §3.5 정합 박스도 보존 19 / 19 일치로 단순화, SwarmRush 분류 차이만 명시.

**1차 리뷰 반영 결과 (v0.6.1)**:

**BLOCKER**:
- **[B1] GuardianRage 적용 종 Wisp/Wraith 한정 디자인 단정** → §10.1 `EMonsterBuff` 결정 블록에 단정 명시 (옵션 (i)/(ii) 구현 분기는 system 결정으로 분리, 적용 종 집합은 디자인 결정).

**권장수정**:
- **[W1] IronWill 표기 정정** → §3.1 #5 표 수치 컬럼을 `_duration=15` 만 남기고 ×0.7 은 `MonsterBuffService` 상수임을 명시. §3.1 표 아래에 IronWill/Frenzy/GuardianRage SO 필드 노출 정책 일괄 박스 추가.
- **[W2] Debuff Tier2 HeroAttackDown 누적 정책** → §4.5 의 Debuff Tier2 항목에 곱연산 누적 명시 (×0.75 × ×0.85 = ×0.6375).
- **[W3] MarkOfDeath IHealth 표면 디자인 단정** → §10.4 `MarkOfDeathEffect` 행 Apply 본문을 *디자인 결정* 으로 격상. 구현 옵션 (i)/(ii) 분기는 system 결정, ×1.5 배율과 Mark 부착 중 적용 보장은 디자인 결정.
- **[W4] SpawnerHaste 최악 시나리오 + 후속 조정 사전 의도** → §9.6 에 ×0.8³ × ×0.85 = ×0.435 / Phantom 2.61s 주기 계산 + 후속 조정 (a) 우선·(b) 대안 명시.
- **[W5] 카드 풀 검산 수식 정정** → §10.7 검산 블록을 §3.5 라인업 통계 표 기준 SO 파일 카운트 기준으로 재작성. 패시브 (보존 13 + 축이동 2 + 신규 1 = 16) / 액티브 (보존 6 + 축이동 2 + 리뉴얼 1 + 신규 3 = 12) 합산 + §3.5 분류 기준과 차이 해설.
- **[추가 권장] Berserk enum 보존 주석 규약** → §10.1 ECardId 추가 블록 끝에 `//#` 주석 한 줄 규약 명시 (Berserk + Multiply 둘 다).

**트레이드오프 (수정 대상 아님, 사용자 승인 게이트 명시 보강)**:
- **[T1] ⑤ 클리어율 미해결** → §9.7 끝에 명시 블록 추가.
- **[T2] 같은 카드 K번 픽으로 Tier 즉시 발동 OK** → §9.2 끝에 명시 블록 추가.

### 12.2 결정 락 (D1~D11) 보존 확인

- D1 (25 → 28 + 신규 3) ✓ §3 (변경 없음)
- D2 (빌드 다양성 1순위) ✓ §1 (변경 없음)
- D3 (4축 = Tank/Dps/Debuff/Swarm, 7카테고리 폐지) ✓ §2 (변경 없음)
- D4 (둔화/속박 → Debuff 축) ✓ §3.3 + §6.2 (변경 없음)
- D5 (한 축당 P4 + A3 = 7장) ✓ §3.5 (변경 없음)
- D6 (Layer 1 = 3/5/7 임계 즉시 발화) ✓ §4.1 (변경 없음)
- D7 (Layer 2 = 같은 카드 K번 픽 누적, 곱연산 일반화) ✓ §7 (변경 없음)
- D8 (환경 → 4축 흡수) ✓ §3.3 (변경 없음)
- D9 (Plague Spawner 추가) ✓ §5 (변경 없음)
- D10 (Multiply 삭제) ✓ §3.4 + §10.5 + §10.7 (변경 없음)
- D11 (컨셉서 §11.3·§11.4·§5.2 갱신, §4.2 유지) ✓ §11 (변경 없음)
- 4축 색 매핑 (Tank `#22C55E` · Dps `#EF4444` · Debuff `#A855F7` · Swarm `#1F2937`) ✓ §2 (변경 없음)
- 28장 라인업 (§3.1 / §3.2 / §3.5 통계) ✓ (변경 없음)
- 12 시너지 Tier 효과량 (§4.2 표 수치) ✓ (변경 없음)
- Plague Spawner #4 (180°) ✓ §5 (변경 없음)
- Multiply 삭제 / Berserk → GuardianRage ✓ §10.1 + §10.4 (변경 없음)

### 12.3 Placeholder 잔존 점검

- **미정 마커 (TBD/추후 결정/???/미정)**: 0건.
- **애매한 권유 (적절히/적당히/유연하게)**: 0건.
- **두 갈래 위임**:
  - `MonsterBuffService` 구현 옵션 (i)/(ii) — *system 구현 결정* 으로 명시 (디자인 결정 영역 아님). 디자인 결정 (Wisp·Wraith 한정) 은 단정.
  - `MarkOfDeath` IHealth 표면 구현 옵션 (i)/(ii) — *system 구현 결정* 으로 명시. 디자인 결정 (×1.5 + Mark 부착 중 적용) 은 단정.
  - `EternalBleedAura` 기존 BleedAura 재사용 여부 — *system 구현 결정* 으로 명시 (효과 ratio 0.01 / 무제한 은 단정).
  - 씬 Spawner #4 GameObject 정확 경로 — *씬 검수 시 확정* (디자인 결정은 위치 (-14.0, 0.0) 단정).
  - → 디자인 결정 영역에서 두 갈래 위임 0건.
- **본문 비움 참조**: 0건. 모든 § 가 결정값 본문 작성.
- **검산 누락**: 0건.
  - §10.7 카드 풀 검산 패시브 16 = 13 + 2 + 1 / 액티브 12 = 6 + 2 + 1 + 3 (산식 명시).
  - §9.6 SpawnerHaste 최악 시나리오 ×0.8³ × ×0.85 = ×0.435 / Phantom 6.0s × 0.435 = 2.61s (산식 명시).
  - §4.5 HeroAttackDown 누적 ×0.75 × ×0.85 = ×0.6375 (산식 명시).

### 12.4 내부 일관성 / 시그니처 명명 일관성

- 28장 카운트 = §3.5 (4축 × 7장) = §10.7 (CardPool 16+12) = §6.3 (각 축 P4+A3) 일치. **§10.7 검산이 §3.5 통계와 분류 기준 차이를 명시적으로 해소**.
- 색 매핑 = §2 표 = §8.5 = §10.8 일치 (Hex 그대로).
- Plague Spawner 슬롯 = §5.1 (Spawner #4) = §10.6 = §11.2 일치.
- ECardId 신규 3 = `WallOfWisps` / `MarkOfDeath` / `SpawnerHaste` — §3·§10.1·§10.5·§10.7 모두 동일.
- 효과 클래스명 = `WallOfWispsEffect` / `MarkOfDeathEffect` / `SpawnerHasteEffect` / `SwarmRushEffect` / `GuardianRageEffect` — §10.4 정의, §3 매핑 일치.
- 신규 IBattleContext 표면 = `IncrementGlobalMonsterCap` / `ScaleAllSpawnerPeriods` / `IncrementAllSpawnerOutputs` — §10.2 정의, §4.5 / §10.3 호출 모두 동일.
- EBuildAxis = `Tank / Dps / Debuff / Swarm` — 모든 § 일관.
- EUI 신규 = `SynergyToast` — §8.4 호출, §10.1 정의 일치.
- EMonsterBuff 신규 = `GuardianRage` — §10.1 정의, §10.4 GuardianRageEffect Apply 본문 호출 일치.
- Aura 신규 = `MarkOfDeathAura` / `EternalBleedAura` — §10.3 / §10.4 정의, 본문 호출 일치.

### 12.5 스코프 / 구현 요청사항 완전성

- **스코프**: 단일 구현 단위로 적정. plan 의 Phase 1·2·3 17 task 와 1:1 매핑 유지. 본 갱신은 *디자인 단정 격상* 과 *검산 정정* 이라 plan task 카운트에 영향 없음.
- **구현 요청사항 완전성**: §10 에 Enum / Interface / 에셋 키 / SO 스키마 / 씬 수정 / json 갱신 / Editor 색 매핑 모두 명세. 본 갱신으로 §10.1 (GuardianRage 적용 종) / §10.4 (MarkOfDeath 표면) 두 항목이 *system 구현 결정 영역과 디자인 결정 영역의 경계* 가 명확해짐.

**Self-Review 결과**: 통과 (BLOCKER 1건 + 권장수정 5건 + 추가 권장 1건 모두 반영, 트레이드오프 2건 명시 보강 완료. 결정 락 D1~D11 + 색 매핑 + 28장 라인업 + Tier 효과량 + Plague Spawner #4 + Multiply/Berserk 정책 모두 보존).
