# 타격 피드백 (Hit Feedback) — 설계 spec

- **날짜**: 2026-06-01
- **단계**: MVP (A안 — 프리미티브 기반 게임플레이 피드백. 본격 VFX 아트=B안은 명시적 범위 밖)
- **검증 가설 정합**: 자동전투의 "때리는 순간"이 시각적으로 읽혀 타격감/가독성이 올라가는가.

---

## 1. 의도 · 범위

몬스터/영웅이 **때릴 때**와 **맞을 때** 프리미티브 도형 + 색상만으로 타격 피드백을 준다. 신규 아트 에셋 0개 — 모든 시각물은 기존 `LairVisualPrefabBuilder` 패턴으로 프리미티브를 자동 생성한다.

### 포함 요소 (4종 + 기존 1종 유지)

| 요소 | 트리거 | 대상 | 비고 |
|---|---|---|---|
| 공격자 스케일 펀치 | `MeleeAttacker.OnHit` | 때리는 쪽 | localScale 순간 확대 → 원복 |
| 공격자 색 플래시 | `MeleeAttacker.OnHit` | 때리는 쪽 | "공격 모션" 느낌, 피격자 플래시와 구분 |
| 임팩트 파티클 | `Health.OnChanged` 델타<0 | 맞는 쪽 | DoT 포함 모든 HP 감소 |
| 데미지 숫자 팝업 | `Health.OnChanged` 델타<0 | 맞는 쪽 | DoT 포함 모든 HP 감소 |
| (기존) 피격자 색 플래시 | `HitFlash` (변경 없음) | 맞는 쪽 | 그대로 유지 |

### 방향 · 밀도
- **양방향 전부** — 영웅↔몬스터 (이 게임엔 몬스터끼리 교전 없음).
- **동시 표시 상한 없음** — 타격 하나당 숫자 하나. CHMPool 풀링으로만 관리.
- 데미지 숫자는 **위로 부상 + 알파 0까지 페이드** 후 자동 풀 반환. 트윈 라이브러리 없음 → **코루틴 기반 lerp** (기존 `HitFlash` 코루틴 방식과 동일 결).

---

## 2. 데미지 숫자 색상 규칙

### 2.1 직격 타격 → 공격자 대표색
- 공격자 몸체 머티리얼 `_BaseColor` (= `HitFlash` 가 이미 읽는 색, `Aura`/`HpBar` 제외) 를 대표색으로 사용.
- 종별 색 (참고, `SpawnerStatusCell.SpeciesColor`):
  Wisp 🟢`#22C55E` · Wraith ⬜`#6B7280` · Reaper 🟥`#EF4444` · Hex 🟡`#EAB308` · Plague 🟪`#A855F7` · Phantom ⬛`#1F2937` · 영웅 ⬜`#FFFFFF`.

### 2.2 DoT 틱 → 디버프별 의미색 (충돌 회피 + FX 동기)
- DoT(독·출혈)는 "때리는 쪽"이 없으므로 디버프별 고유색을 쓴다.
- **충돌 문제**: 독 연두 `#84CC16` ↔ Wisp 녹색, 출혈 빨강 `#DC2626` ↔ Reaper 빨강이 겹친다.
- **해결**: 독·출혈 색을 **어둡게 + 색상(hue) 비틀어** 6종 대표색과 명확히 분리한다.
  - 독 → 짙은 에메랄드/틸 계열 (녹색 hue를 청록 쪽으로 비틀어 Wisp 녹색과 분리)
  - 출혈 → 짙은 크림슨/마룬 계열 (빨강 hue를 자홍 쪽으로 비틀어 Reaper 빨강과 분리)
- **FX 프리팹도 동기화** — `PoisonAura`·`BleedStatus` 프리팹 머티리얼 색을 동일한 새 색으로 변경한다 (숫자색과 상태 비주얼색 일치).
- **정확한 hex 값 + 인게임 분리도 검증은 game-designer 단계**에서 확정. 명도뿐 아니라 hue 분리를 함께 줄 것. 인게임에서 구분이 약하면 백색 폴백.

> 그 외 hero-scope 디버프(표식 등 DoT 아닌 것)는 본 기능 데미지 숫자 대상이 아니다 — HP를 직접 깎는 DoT는 독·출혈 계열만.

### 2.3 색상 전달 메커니즘 (Health 시그니처 무변경)
- 데미지 숫자는 **피격자 `Health.OnChanged`** 로 뜨지만 이벤트는 공격자/출처를 모른다.
- **스탬프 방식**: 데미지를 입히는 주체가 `TakeDamage` 호출 **직전** 피격자 쪽 피드백 컴포넌트에 "다음 데미지 색"을 스탬프한다. `OnChanged` 가 `TakeDamage` 내부에서 동기 발행되므로, 직전 스탬프 값이 핸들러에서 정확히 읽힌다.
  - `MeleeAttacker` → 공격자 대표색 스탬프.
  - DoT 아우라(`PoisonAura`/`BleedAura`) → 자신의 디버프색 스탬프.
- `IHealth`/`Health` 의 이벤트 시그니처는 변경하지 않는다 (저위험). 스탬프는 별도 사이드 채널(작은 메서드/필드).
- 미스탬프 시 폴백색(백색 또는 피격자 자기색) — 정상 흐름에선 발생하지 않음.

---

## 3. 아키텍처 — 엔티티별 컴포넌트 (HitFlash 패턴 일관)

중앙 서비스/레지스트리 없음 (상한이 없으니 불필요). 각 캐릭터 프리팹에 컴포넌트 부착:

### 3.1 `AttackJuice` (공격자 측)
- 자신의 `MeleeAttacker.OnHit` 구독.
- 동작: ① 스케일 펀치(localScale 확대→코루틴 원복) ② 공격자 색 플래시(`HitFlash` 의 material-instance 캐시 방식 재사용) ③ 피격자에 대표색 스탬프.
- 풀 재사용: `OnEnable`/`OnDisable` 에서 스케일·색 원복 (Rule 03 §4).

### 3.2 `DamageFeedback` (피격자 측)
- 자신의 `Health.OnChanged` 구독. 델타<0(데미지)일 때만 반응, 회복 무시 (`HitFlash` 와 동일 `_lastHp` 델타 추적 — 데미지량 = `lastHp - current`).
- 동작: ① 임팩트 파티클 풀 Pop ② 데미지 숫자 팝업 풀 Pop(스탬프된 색 적용).
- 스탬프 수신 메서드 제공(예: `SetNextDamageColor(Color)`).
- 풀 재사용 리셋.

> `HitFlash` 의 델타 추적 로직과 중복되므로, 구현 시 공통화/재사용 여부는 plan 단계에서 결정 (단, `Health` 이벤트 시그니처 무변경 원칙 유지).

---

## 4. 신규 FX 프리팹 2종 (CHMPool 대상)

`EVisual` 에 키 2개 추가. `LairVisualPrefabBuilder` 확장으로 프리미티브 자동 생성 + Addressables 등록.

### 4.1 `HitImpact`
- `ParticleSystem` — **메시 렌더 = 작은 큐브/구체 (텍스처 미사용)**. 버스트 N개가 튀어 흩어지며 축소·소멸 후 자동 `CHMPool.Push`.
- 색은 머티리얼 (프리미티브 + 색상 원칙).

### 4.2 `DamagePopup`
- 월드스페이스 `TMP_Text` + **`CHText`** (Rule 03 §3 — TMP엔 CHText 필수).
- 컴포넌트: ① 숫자 텍스트 세팅 ② 카메라 빌보드 ③ 코루틴 위로 부상 + 알파 페이드 ④ 종료 시 `CHMPool.Push`.
- `OnEnable` 풀 리셋.

### 4.3 기존 프리팹 색 변경
- `PoisonAura`·`BleedStatus` 머티리얼 색을 §2.2 새 색으로 변경 (빌더 spec 갱신).

---

## 5. 풀링 · 워밍 (Rule 03 §4)

- `BattleController` 진입점에서 `HitImpact`·`DamagePopup` 프리팹 `LoadAsync` → `CHMPool.CreatePool` 사전 워밍.
- 워밍 count는 상한이 없으므로 넉넉히 (예: 20~30) — **정확한 수치는 game-designer**.
- `Pop`/`Push` 만 사용. `Instantiate`/`CreatePrimitive` 직접 호출 금지 (런타임).

---

## 6. MVP 범위 정당화 (design-reviewer 대비)

- 프리미티브 도형 + 색상만. **신규 아트 에셋 0**. 파티클도 메시=프리미티브, 텍스처 미사용 → §8 "프리미티브 고정·아트 금지" 준수.
- 목적은 **게임플레이 가독성/타격감 피드백**이지 VFX 아트가 아님. 본격 VFX(B안)는 명시 범위 밖.
- 사운드 hook 미등록 유지 (§8).

---

## 7. 인지된 트레이드오프 (사용자 승인)

- **동시 표시 상한 없음** → 스웜 + DoT가 영웅에 집중되면 숫자/파티클이 다수 겹칠 수 있음. 사용자가 타격감 우선으로 명시 선택. 부상+페이드로 잔상은 빠르게 정리.
- 원거리 Hex도 `MeleeAttacker` 경로 → 스케일 펀치는 발사체 없이 제자리 발생, 임팩트 파티클은 타겟 위치에 정상 표시 (의도된 동작).

---

## 8. 테스트 윤곽 (test-engineer 단계 상세화)

- EditMode: `AttackJuice` 가 OnHit에 스케일/색 변경 후 복원 · 피격자 색 스탬프. `DamageFeedback` 이 델타<0에만 반응·회복 무시 · 스탬프색 적용 · 풀 Pop/Push 호출(모킹).
- 풀 재사용(OnEnable/OnDisable) 상태 리셋 회귀.
- DoT 경로(아우라)에서도 데미지 숫자가 뜨고 색이 디버프색으로 스탬프되는지.

---

## 9. 미해결 → 다음 단계로 위임

- 정확한 색 hex (독 진녹/틸, 출혈 진빨/마룬) + 분리도 검증 → **game-designer**.
- 스케일 펀치 배율·지속, 숫자 부상 거리·시간·폰트 크기, 파티클 개수·수명, 워밍 count → **game-designer**.
- `HitFlash` 델타 로직 공통화 여부, 색 스탬프 사이드 채널 정확한 형태 → **writing-plans / gameplay-programmer**.
