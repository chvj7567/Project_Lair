# Content Audit — 2026-07-08

> 카테고리: Debuff 패시브 / 영구 공격력 감소 복합
> 슬러그: `debuff-hero-attack-down-perm-mul-floor`
> 작성: Daily Content Audit 루틴 (자동)

---

## 0. 후보 선정 근거

### 검토한 후보 목록

| 후보 | 검증가치 | 구현비용 | 시너지폭 | 데이터근거 | 종합 |
|---|---|---|---|---|---|
| HeroAttackDown 영구 복합 (채택) | 5 | 2 | 5 | 4 | **18** |
| GlobalAtkInterval 누적 (액티브) | 4 | 3 | 3 | 3 | 11 |
| Tank Tier3 완전 면역 판정 오류 | 4 | 2 | 3 | 2 | 11 |
| Swarm 집중 모드 Tier1 미발동 | 3 | 2 | 2 | 3 | 10 |

> 점수 공식: 검증가치 + (6−구현비용) + 시너지폭 + 데이터근거 (각 1~5)

채택 이유: 기획서 §3.3 "2픽=×0.5625" 라는 수치가 코드 구현과 다를 가능성이 있으며,
Debuff Tier2 자동 부착(factor=0.85) 과의 복합 시 영구 하한이 §8 밸런스 기준(2~4분 사망)을
위반할 수 있다. 별도 BalanceConfig 손잡이(MinHeroAttackScale 영구 버전)가 부재해 즉시
대응이 어렵다는 점에서 검증가치·시너지폭 모두 최고점을 부여했다.

---

## 1. 핵심 요약 (3줄)

- `HeroAttackDown` (Debuff 패시브, ECardId.HeroAttackDown) 은 픽마다 영웅 공격력에
  `PowerScale *= 0.75f` 를 영구 적용하며, 3픽 상한 내에서 최대 ×0.421875 까지 감소한다.
- Debuff Tier2 는 factor=0.85f 의 독립 HeroAttackDownAura 를 자동 등록해 추가 ×0.85 를
  곱하므로, 이론 최솟값은 ×0.421875 × 0.85 ≈ **×0.359**.
- BalanceConfig 에 영구 공격력 하한(MinHeroAttackScale 상시) 손잡이가 없어 이 극단값을
  런타임에서 클램핑할 수단이 기획·코드 양측에 부재하다.

---

## 2. 상세 분석

### 2.1 카드 메커니즘

**파일**: `Assets/_Lair/Scripts/Card/Effects/HeroAttackDownEffect.cs`

```csharp
public void Apply(IBattleContext ctx)
{
    Transform heroT = ctx.GetHeroTransform();
    if (heroT == null) return;
    IAttacker atk = heroT.GetComponent<IAttacker>();
    if (atk == null) return;
    ctx.ApplyHeroAura(new HeroAttackDownAura(atk), durationSeconds: -1f);
}
```

**파일**: `Assets/_Lair/Scripts/Card/Auras/HeroAttackDownAura.cs`

- `factor = 0.75f` (기본값)
- `OnAttached` 에서 `_attacker.PowerScale *= _factor` 1회 적용 후 `_applied = true`
- `ShouldStackAsNew`: 동일 factor → `false` (기존 인스턴스 재사용), 다른 factor → `true` (신규 인스턴스)
- `durationSeconds = -1f` → `HeroAuraRunner` 영구 유지

### 2.2 기획서-코드 불일치 가능성

`docs/design/card-renewal.md` §3.3:

> "HeroAttackDown — factor=0.75, 곱연산 누적, **2픽=×0.5625**"

그러나 코드 분석 결과:

| 시나리오 | 동작 |
|---|---|
| 1픽 | 새 인스턴스 부착 → `_applied=true`, `PowerScale×=0.75` |
| 2픽 (같은 factor) | `ShouldStackAsNew` → `false` → **기존 인스턴스 재사용** → `_applied=true` 가드 → **OnAttached 호출 안 됨** |
| 2픽 (다른 factor) | `ShouldStackAsNew` → `true` → 신규 인스턴스 부착 → `PowerScale×=새factor` |

`ShouldStackAsNew` 가 `false` 를 반환하면 `HeroAuraRunner` 는 기존 인스턴스를 유지하므로
**동일 factor 카드를 2회 이상 픽해도 PowerScale 에는 1회분만 적용**된다.

→ 기획서의 "2픽=×0.5625" 는 현 코드에서 재현되지 않을 가능성이 높다.
→ 실제 동작 최대치는 **×0.75 (1회)** — 코드 수준 확인 필요.

> **주의**: 이것이 의도된 설계(중복 픽 방지)인지, 아니면 누적 의도가 있는데 구현 버그인지
> 기획 의도 명확화가 필요하다.

### 2.3 Debuff Tier2 자동 부착

`docs/design/card-renewal.md` §4.2, §4.5:

> Debuff Tier2: `ApplyHeroAura(new HeroAttackDownAura(_attacker, 0.85f), -1f)`

factor=0.85f 는 카드 픽 factor=0.75f 와 다르므로 `ShouldStackAsNew` → `true` → **항상 독립 부착**.

결합 시 `PowerScale` 계산:

| 상태 | 공식 | 결과 |
|---|---|---|
| 카드 1픽 + Tier2 | 1.0 × 0.75 × 0.85 | **0.6375** |
| 카드 3픽(스택 작동 가정) + Tier2 | 1.0 × 0.75³ × 0.85 | **≈ 0.359** |
| 카드 1픽(현 코드) + Tier2 | 1.0 × 0.75 × 0.85 | **0.6375** |

### 2.4 전투 영향 추정

기준: 영웅 공격 50/hit, 1초 간격 (컨셉 §11.3 / QA 리포트 평균 사망 76s)

| 시나리오 | 유효 공격력/hit | Reaper(HP=100) 처치 시간 |
|---|---|---|
| 기본 (손댐 없음) | 50 | 2s |
| 1픽+Tier2 (현실) | 50 × 0.6375 ≈ 31.9 | ~3.1s |
| 3픽+Tier2 (기획 의도 누적 시) | 50 × 0.359 ≈ 18.0 | ~5.6s |
| 3픽+Tier2+Weaken(×0.5, 10s) | 50 × 0.359 × 0.5 ≈ 9.0 | ~11.1s |

단일 몬스터 처치 시간이 5~11초로 늘어나면 몬스터 6종 합산 DPS 압박이 크게 완화되어
§8 "2~4분 이내 사망" 달성이 어려워질 수 있다. 다만 전체 전투는 몬스터 집단 vs 영웅이므로
단일 처치 시간 2배 증가가 전체 사망 시각에 선형으로 연결되지는 않는다.

### 2.5 BalanceConfig 손잡이 부재

`Assets/_Lair/Data/BalanceConfig.asset` 검토 결과:

- `MinHeroAttackScale` (상시·영구 하한) 키 **없음**
- Weaken 임시 버전(`MinHeroAttackScaleFloor`)이 2026-07-05 감사에서 언급되었으나 영구 버전은 별개
- 영구 공격력이 과도하게 낮아져도 런타임 클램핑 수단이 없다

---

## 3. 발견된 문제

### 문제 A — 기획서-코드 불일치: 동일 factor 픽 누적 여부

- **기획서** (`card-renewal.md` §3.3): HeroAttackDown 2픽 = ×0.5625 (곱연산 누적 의도)
- **코드** (`HeroAttackDownAura.ShouldStackAsNew`): 동일 factor → `false` → 기존 인스턴스 재사용 → **OnAttached 재호출 없음**
- `_applied = true` 가드가 이미 있어 설령 재부착이 일어나도 2회 곱산은 일어나지 않는다
- 결과: 카드를 2~3회 픽해도 ×0.75 만 1회 적용 — 기획 의도 실현 불가

**우선순위**: 높음 (기획 수치와 구현이 다르면 밸런스 시뮬레이션 전체가 잘못된 전제 위에 돌아감)

**해결 방향 A-1 (의도가 누적)**: `ShouldStackAsNew` 에서 같은 factor 도 `true` 반환,
  `_applied` 가드를 제거해 픽마다 새 인스턴스로 `PowerScale *= 0.75` 를 매번 적용.

**해결 방향 A-2 (의도가 1회 상한)**: 기획서 §3.3 을 "1픽 = ×0.75, 추가 픽 무효" 로 수정.
  현 코드가 정합하도록 문서를 정정.

### 문제 B — MinHeroAttackScale (영구) 손잡이 미설계

- 영구 영웅 공격력 하한이 없어 Debuff Tier2 + 카드 3픽 복합 시 어떤 수치까지 내려가도
  제어할 수 없다
- 임시 Weaken 과 달리 영구 효과는 전투 내 복구 수단이 없다

**우선순위**: 중간 (문제 A 의 누적 여부가 확정된 뒤 필요성이 결정됨)

**해결 방향**: `BalanceConfig` 에 `MinPermanentAttackScale: float` (예: 기본값 0.3f) 추가.
`HeroAuraRunner` 또는 `MeleeAttacker` 의 `PowerScale` 설정 시 클램프 적용.

---

## 4. 영향 범위

| 구성요소 | 파일 | 영향 |
|---|---|---|
| HeroAttackDownAura | `Assets/_Lair/Scripts/Card/Auras/HeroAttackDownAura.cs` | 문제 A 수정 시 변경 |
| BalanceConfig | `Assets/_Lair/Data/BalanceConfig.asset` | 문제 B 수정 시 필드 추가 |
| card-renewal.md | `docs/design/card-renewal.md` §3.3 | 문제 A 의도 명확화 후 수정 |
| Debuff Tier2 효과 | 관련 Tier2 Effect 파일 | 직접 변경 없음 (로직 자체는 정상) |
| HeroAttackDownEffect | `Assets/_Lair/Scripts/Card/Effects/HeroAttackDownEffect.cs` | 직접 변경 없음 |

---

## 5. 권장 다음 단계

1. **기획자 확인 (즉시)**: HeroAttackDown 동일 카드 중복 픽 시 누적 의도인지 1회 상한 의도인지 결정
2. **기획자 → 기획서 수정 또는 프로그래머 → 코드 수정** (의도에 따라):
   - 누적 의도: `ShouldStackAsNew` + `_applied` 로직 수정 → test-engineer 회귀 테스트
   - 1회 상한 의도: `card-renewal.md` §3.3 수치 정정 ("2픽=×0.5625" 삭제)
3. **문제 B (순서 2 이후)**: 누적이 의도라면 BalanceConfig 에 `MinPermanentAttackScale` 추가 검토
4. **qa-simulator**: DebugAutoPicker 훅(`docs/qa-reports/2026-05-22.md` §3 참조)이 구현된 뒤
   Debuff 빌드 N판 시뮬레이션으로 실제 사망 분포 측정

---

## 6. 참조 파일

- `Assets/_Lair/Scripts/Card/Auras/HeroAttackDownAura.cs`
- `Assets/_Lair/Scripts/Card/Effects/HeroAttackDownEffect.cs`
- `Assets/_Lair/Data/BalanceConfig.asset`
- `docs/design/card-renewal.md` §3.3, §4.2, §4.5
- `docs/design/project_lair_concept.md` §8 (밸런스 기준), §11.3 (카드 목록)
- `docs/qa-reports/2026-05-22.md` (DebugAutoPicker 훅 요청 및 기준 수치)

---

## 쉬운 설명 (비개발자 요약)

**무슨 문제인가?**

HeroAttackDown 카드는 영웅의 공격력을 25% 영구적으로 줄이는 패시브 카드다.
기획서에는 "이 카드를 2번 고르면 공격력이 43.75%로 줄어든다(0.75×0.75=0.5625)"고 적혀 있다.
하지만 실제 게임 코드를 보면 같은 카드를 두 번 골라도 두 번째엔 효과가 적용되지 않아
실제로는 첫 번째 픽의 75%만 유지된다.

또한 Debuff 시너지 2단계가 되면 자동으로 공격력이 15% 더 줄어드는 효과가 붙는다.
만약 기획 의도대로 카드 3번+시너지가 모두 쌓인다면 영웅 공격력이 원래의 약 36% 수준까지
내려갈 수 있는데, 이 하한선을 조정하는 조절 손잡이가 밸런스 설정 파일에 없다.

**왜 문제인가?**

1. 기획서 수치와 실제 동작이 달라 밸런스 설계 자체가 잘못된 숫자 위에 세워진다.
2. 영웅 공격력이 너무 낮아지면 몬스터를 거의 못 죽여 게임이 5분 안에 끝나지 않는 경우가 생긴다.
   목표는 "2~4분 안에 영웅을 처치"인데, 영웅이 너무 약하면 이 목표가 달성되지 않는다.

**지금 해야 할 일**

기획자가 먼저 "이 카드를 여러 번 골랐을 때 효과가 쌓이는 것이 맞는가, 아닌가"를 결정해야 한다.
그 결정에 따라 코드를 수정하거나 기획서를 수정한다.
