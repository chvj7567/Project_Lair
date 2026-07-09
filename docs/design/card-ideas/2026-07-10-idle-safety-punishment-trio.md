# Card Ideas — 2026-07-10 — 안일 응징: 안전한 영웅을 처벌하는 역습 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 안일 응징 (Idle Safety Punishment) — 영웅이 피해를 받지 않거나(무피해 구간), 체력이 넉넉하거나(고체력 상태), 필드를 완전히 비울 때 던전이 역습하는 3종. "여유 부릴수록 더 위험해진다."
- **목록**: VoidRage (공허의 분노) / SafetyPunish (자만의 흠집) / ComplacencyCurse (안일의 저주)
- **기존 28장 + git log 과거 회차 + card-ideas 폴더 40개 파일 중복 회피 확인됨**
  - 기존 28장 중 영웅 "안전 상태 (무피해·고체력·필드 공백)" 에 반응하는 카드 없음 — 모든 기존 카드는 단순 고정 버프·디버프 또는 영웅이 "피해를 받을 때" 작동
  - 2026-07-06 `hero-hit-reaction-trio` (피격 반응): "영웅이 맞을수록 던전이 강해진다" — 오늘 3장은 **정반대** 방향: "영웅이 맞지 않을수록 던전이 강해진다"
  - 2026-06-08 `escape-punishment-trio` (도주 처벌): 영웅 HP 하락 → 이동속도 감소 — 오늘은 HP 높거나 무피해일 때 패널티. 방향 반전
  - 2026-06-15 `attack-backfire-penalty-trio`: 영웅이 공격할 때 반격 — 오늘은 "공격하지 않아도 / 피해를 안 받아도" 발동. 트리거 다름
  - 2026-06-19 `wounded-hero-punisher-trio` (저체력 포식자): 저체력 영웅에게 던전 강화 — 오늘 ComplacencyCurse 는 **고체력** 영웅에게 패널티. 반전 각도
  - 2026-06-09 `kill-echo-penalty-trio` (처치 반향): 처치 누적 카운터 기반 — VoidRage 는 "필드 0마리 + 2초" 트리거로 다름
  - 2026-06-25 `event-burst-spawn-trio` (이벤트 연동): HpSurgeSpawn(HP drop 트리거), ActiveEcho(액티브 픽 트리거) — VoidRage 는 "완전 청소 후 공백" 트리거로 다름
  - 40개 파일 전수 검토: "무피해 구간 조건부 효과" / "고체력 환경 페널티" / "필드 완전 청소 → 역습 스폰" 세 조합 모두 미제안 ✅

---

## 1. VoidRage (공허의 분노) — 가칭

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**:
  - 필드 전체 몬스터 수가 0마리인 상태가 2초 이상 지속되면 즉시 발동: 6개 스포너 전체가 각각 현재 출력 종 1마리씩을 동시 팝 (정상 스폰 주기 무시, 단 글로벌 캡 체크 적용).
  - 발동 후 15초 쿨다운 — 연속 발동 방지.
  - 중첩 픽 시 쿨다운 감소: 2픽 → 10초, 3픽 → 7초.
  - **밸런스 근거**: 영웅이 모든 몬스터를 처치한 시점은 이미 강한 상태임 → 이 패널티 스폰으로 "필드 클린" 이 의미 없게 만듦. 스폰 최대 6마리(캡 18 기준)이므로 과부하 아님. 쿨다운으로 연속 트리거 방지.
- **구현 패턴**:
  - `IBattleContext.FieldMonsterCount` Tick 감시 → 0 진입 시 2초 카운터 시작.
  - 2초 도달 → `IBattleContext.GetAllSpawners()` 순회 + 캡 여유 확인 후 `CHMPool.Instance.Pop(spawner.MonsterPrefab, spawner.Position)`.
  - `DesolationMarchEffect`(06-09) 의 "스포너 순회 팝" 패턴 그대로 재사용. 조건 분기만 다름.
  - 쿨다운 타이머는 `MonsterBuffService` 내 float 필드로 관리.
- **시너지 후크**:
  - `SpawnerHaste` (스폰 주기 ×0.8) + VoidRage → VoidRage 팝 직후 정상 스폰도 빠르게 재개 → 빈 필드 간격 더 짧음
  - Swarm Tier3 (글로벌 캡 +6, 18→24) + VoidRage → 캡 여유로 팝 불발 빈도 감소
  - `SpawnPhantoms` + VoidRage → Phantom 스포너 출력이 높아 팝 1회에 즉시 고밀도 소환
- **구현 비용 추정**: 3 (FieldMonsterCount 접근자가 IBattleContext 에 있으면 2, 없으면 신규 추가 필요로 3)
- **중복 재검증**: `DesolationMarchEffect`(06-09) = 처치 카운터 8 → 스포너 팝. VoidRage = 필드 완전 공백 2초 → 일제 팝. 트리거 조건(처치 카운트 vs. 공백 기간)과 의도(영웅 효율성 처벌 vs. 필드 청소 처벌) 모두 다름 ✓

---

## 2. SafetyPunish (자만의 흠집) — 가칭

- **카테고리**: 패시브 강화 (Debuff 축)
- **효과 모델**:
  - 영웅이 마지막으로 피해를 받은 이후 5초 이상 경과하면 "자만 표식" 활성화: 영웅 공격력 ×0.85 (영구적이 아닌 조건부 — 표식 활성 중에만 적용).
  - 영웅이 피해를 다시 받는 순간 표식 해제 + 공격력 원복. 5초 무피해 재달성 시 재활성화.
  - 중첩 픽 시 표식 배율 누적: 2픽 → ×0.85×0.85 = ×0.72, 3픽 → ×0.61 (최대 3픽까지).
  - **밸런스 근거**: 영웅이 범위를 벗어나거나 몬스터가 접근 전 상태일 때 자동으로 ATK 약화. 전투 중(피해 받는 중)에는 효과 없어 "무력화 + 압박 동시" 는 불가 → 피해받는 영웅에게는 패널티 없음. 밸런스상 단독으로는 약하지만 Weaken/Bleed 콤보 시 피해 없이 잠시 버티다 재참전하는 영웅에게 지속 페널티.
- **구현 패턴**:
  - `HeroAura` 에 `float TimeSinceLastDamage` 필드 추가 (Tick 에서 +deltaTime, OnDamageTaken 에서 0 초기화).
  - `SafetyPunishEffect` 가 Tick 마다 `TimeSinceLastDamage ≥ 5f` 확인 → `IHeroAura.SetAtkMultiplier(0.85f)` 조건부 적용/해제.
  - `HeroAttackDownEffect` 의 영구 ATK 조작 패턴을 조건부로 응용 — 해제 가능한 조건부 버전.
- **시너지 후크**:
  - `Weaken` (A, 공격력 ×0.5, 10s) + SafetyPunish: Weaken 기간 끝나 회피 중 → 5초 무피해 → SafetyPunish 재발동 → 연속 공격력 약화
  - `PlagueSlowBoost` (P, 강한 둔화) + SafetyPunish: 슬로당한 영웅이 피격 빈도 높아 SafetyPunish 발동 어려움 — Debuff 빌드가 SafetyPunish 를 방해하지 않는 구조 (시너지 아닌 독립 공존)
  - `HeroAttackDown` (P, 영구 ×0.75) + SafetyPunish (조건부 ×0.85) → 영구 ×0.75 × 조건부 ×0.85 = 최악 ×0.64 공격력 (2중 누적). Debuff 빌드 완성형.
- **구현 비용 추정**: 3 (`IHeroAura` 에 TimeSinceLastDamage 추가 + 조건부 ATK 배율 토글)
- **중복 재검증**: `HeroAttackDown` (기존 P) = 픽 시 영구 ×0.75, 무조건. SafetyPunish = 무피해 5초 후만 ×0.85 조건부. 트리거(픽 시 vs. 시간 조건)와 지속성(영구 vs. 조건부) 완전히 다름. 06-08 `WoundedPursuit` = HP 낮을수록 이속 감소. SafetyPunish = 피해 없이 지낼수록 ATK 감소. 방향과 효과 다름 ✓

---

## 3. ComplacencyCurse (안일의 저주) — 가칭

- **카테고리**: 액티브 저주 (Debuff 축)
- **효과 모델**:
  - 발동 후 20초간, 3초마다 영웅 HP 비율에 따라 다른 효과:
    - HP 70% 이상: 최대 HP의 4% 즉시 피해 ("여유 있는 영웅에게 패널티")
    - HP 50~69%: 효과 없음 (중립 구간)
    - HP 49% 이하: 최대 HP의 2% 회복 ("빈사의 영웅에게 소폭 자비")
  - 20초 ÷ 3초 = 최대 6~7회 틱.
  - **밸런스 근거**: HP 70% 이상일 때 3틱만 받아도 -12% 최대 HP 피해. 영웅 HP 1000 기준 4% = 40 피해. 6틱 최악 = -240 HP. 평균 빌드(2~4분 사망 목표)에서 의미있는 기여. 저체력 영웅에게 2% = 20 HP 회복은 생존 연장을 소폭 도와 런 후반부 선택지 추가 발동 기회 확보.
- **구현 패턴**:
  - `ComplacencyCurseEffect` — `IHeroAura.RegisterTimedEffect(20s)` 내에서 3초 주기 Tick 구독.
  - Tick 마다 `IHeroAura.CurrentHpRatio` 확인 → 분기:
    - ≥ 0.7f → `hero.TakeDamage(maxHp × 0.04f)`
    - < 0.5f → `hero.Heal(maxHp × 0.02f)` (Heal API 가 없으면 음수 TakeDamage 또는 HeroHealth 직접 호출)
  - 지속시간·틱 패턴은 `BleedEffect` (이동 시 HP 소모, 10s) 의 타이머 구조 재사용. HP 비율 조건 분기만 추가.
- **시너지 후크**:
  - `WispHpBoost` (탱커 강화) + ComplacencyCurse: 탱커가 많아져 영웅이 계속 피해받으면 HP 70% 미만 유지 → ComplacencyCurse 중립 or 회복 구간에 머물러 효과 상쇄 — 카운터플레이 흥미로움
  - `Bleed` (이동 시 HP -2%, 10s) + ComplacencyCurse: Bleed 로 이미 HP 70% 미만 유도 → ComplacencyCurse 패널티 구간 회피 + 회복 구간 진입 가능성 — Debuff 연쇄 미묘한 시너지
  - `MarkOfDeath` (영웅 받는 데미지 ×1.5, 5s) + ComplacencyCurse: MarkOfDeath 기간 중 빠르게 HP 50% 이하 → ComplacencyCurse 소폭 회복 → MarkOfDeath 데미지와 회복이 교차하는 긴장
- **구현 비용 추정**: 3 (BleedEffect 타이머 + HP 비율 조건 분기. Heal 미구현 시 +1 추가)
- **중복 재검증**: `Bleed` (기존 A) = 이동 시 HP -2%, 10s. ComplacencyCurse = HP 70% 이상이면 3s마다 -4% (이동과 무관), HP 49% 이하이면 회복. 트리거(이동 여부 vs. HP 비율), 지속시간(10s vs. 20s), 회복 분기 전부 다름. 06-19 `DeathKnell·BloodScent` (저체력 포식자) = 낮은 HP 시 던전 강화. ComplacencyCurse = 높은 HP 시 패널티 + 낮은 HP 시 소폭 자비. 방향 완전 반전 ✓

---

## 4. 공통 테마 고찰

### 왜 "안일 응징" 인가

기존 40개 아이디어 파일과 28장 카드를 전수 검토한 결과, **"영웅이 안전하거나 우세할 때 던전이 반응하는"** 방향이 완전히 비어 있다.

현재 카드들의 트리거 구조를 정리하면:
| 트리거 방향 | 예시 (기존) |
|---|---|
| 영웅 피해받을 때 | Bleed, Fear, WoundedPursuit (06-08) |
| 영웅이 처치할 때 | BloodThirst, SoulCurse (06-02), BloodEcho (06-09) |
| 영웅 HP 낮을 때 | BloodScent, PanicStampede (06-19) |
| 필드 몬스터 많을 때 | 군단 밀도 압박 (06-06) |
| 영웅이 공격할 때 | CounterThorns (06-15) |

**비어 있는 방향**: 영웅이 **피해를 안 받을 때 / HP 가 높을 때 / 필드를 비울 때.**

이 역방향 트리거는 두 가지 전략적 긴장을 만든다:
1. **"잘 하면 더 위험해진다"** — 영웅 입장에서 "최선을 다해도 던전이 쫓아온다"는 새로운 압박
2. **전략 분기** — 플레이어가 "영웅을 빠르게 제압할 것인가 vs. 느리게 갉아낼 것인가"를 선택할 때, "빠른 제압(필드 클린+고체력 달성)"에 던전 측 카운터가 생김

v0.2 풀 확장(패시브 30~40장 / 액티브 20~30장)에서 이 세 카드는 **Debuff 축의 다양성**을 넓힌다. 현재 Debuff 축은 PlagueSlowBoost / SpawnPlagues / HeroPoisonAura / HeroAttackDown (P4), Fear / Bleed / Weaken (A3). 이 중 "조건부 반응형" 카드가 없어 패시브적임. 오늘 3장은 Debuff 축에 "반응형 압박" 레이어를 추가.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- 구현 묶음 제안: VoidRage + SafetyPunish 는 `IBattleContext` 확장 (FieldMonsterCount, TimeSinceLastDamage) 이 겹치므로 1회 스프린트에 묶어 진행 권장
- ComplacencyCurse 는 Heal API 유무에 따라 구현 비용이 달라지므로, 기존 Hero HP 조작 경로 확인 후 착수
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서는 영웅이 맞을수록 던전이 더 많은 몬스터를 내보내거나 강해지는 식이었습니다. 그런데 오늘 제안하는 카드들은 반대 상황에 주목합니다. 영웅이 "나는 잘 되고 있어" 하며 여유 부릴 때가 오히려 더 위험해지도록 만드는 카드들입니다. 예를 들어, 영웅이 모든 몬스터를 다 쓸어버리고 잠깐 숨 돌리는 순간 던전 입구가 전부 한꺼번에 열리거나, 5초 동안 한 대도 안 맞고 있으면 오히려 영웅의 공격력이 슬금슬금 떨어지는 식입니다. 마치 "배부른 자에게 경고한다"는 느낌입니다. 그래서 오늘 제안하는 카드 3장은: 필드가 텅 비는 순간 던전이 전체 역습 소환을 하는 "공허의 분노", 5초 동안 맞지 않으면 영웅 공격력이 깎이는 "자만의 흠집", 체력이 70% 이상으로 너무 여유로울 때 3초마다 피해를 받지만 반대로 반피 이하에선 조금씩 회복되는 "안일의 저주"입니다.
