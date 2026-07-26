# 시너지 티어 설명 스트링 테이블화 + 밸런스 상수 단일화 Design Spec

- **작성일**: 2026-07-26
- **단계**: v0.3 (UI 로컬라이즈·리팩터 — 신규 콘텐츠 아님)
- **분류**: UI / 데이터 (시너지 모달 티어 설명)
- **문서 성격**: spec — 골격 + 결정 락.

---

## 1. 의도

인게임 시너지 모달(`SynergyModalPopup`)의 티어 설명 12행이 지금 **UI 코드에 하드코딩된 문자열**(`TierDesc` 딕셔너리)이다. 이걸 (a) **표시 문자열은 스트링 테이블(`Strings_Ko.json`)에서**, (b) **수치(×1.3 등)는 각 시너지 tier의 밸런스 상수에서** 단일 소스로 가져오게 리팩터한다.

**해결하려는 문제**: 지금은 `TierDesc` 가 티어 효과 로직(각 `*SynergyTier*` 클래스의 `const`)과 **별개로 수치를 하드코딩**한다 → 밸런스를 바꾸면 두 곳(효과 상수 + 표시 문자열)을 고쳐야 하고, 실제로 **drift 가 이미 발생**했다(§5 Tank3).

## 2. 범위

### 포함
- `TierDesc` 하드코딩 제거. 각 tier 가 자기 설명(스트링 테이블 id + 상수 유래 수치)을 소유.
- `Strings_Ko.json` 에 12개 티어 설명 **템플릿** 추가(몬스터명·스탯명은 한글 baked, 수치는 `{0}` placeholder).
- 수치는 tier 의 기존 `const` 에서 계산 → 단일 소스. 파생 표기(쿨다운→공속%, 비율→%/s)는 tier 가 자기 상수에서 계산.
- 테스트 갱신: 스트링 provider 주입 구조로 회귀 커버.

### 비포함
- **축 헤더 라벨**(`TANK`/`DPS`/`DEBUFF`/`SWARM`, 현재 영어 `BuildSynergyPanel.AxisLabel`) 로컬라이즈 — 후속(사용자 미요청).
- 밸런스 **수치 자체 변경** — 표시만 손댄다(단, Tank3 는 표시가 실제 효과와 어긋나 있어 단일 소스화로 자동 교정, §5).
- 신규 언어(En 등) 추가 — 구조만 로컬라이즈 친화적으로.

## 3. 핵심 결정 (락)

1. **tier 가 자기 설명을 소유** — `IBuildSynergyTier` 에 설명 노출 멤버 추가:
   - `int DescriptionStringId { get; }` — 스트링 테이블 템플릿 id.
   - `string[] DescriptionArgs { get; }` — 자기 `const` 에서 포맷한 수치 인자(예: `["1.3"]`, `["25"]`).
   - 이유: 티어가 자기 상수 + 자기 표기법을 안다 → 수치·표기의 단일 소스. UI 는 조립만.
2. **스트링 테이블 = 템플릿** — `Strings_Ko.json` 에 12개 항목. 몬스터명·스탯명은 한글로 baked, 수치는 `{0}` 치환:
   - 예: `"도깨비불·망령 HP ×{0}"`, `"사신·저주술사 공속 +{0}%"`, `"모든 스포너 동시 출력 +{0}"`.
   - id 는 기존 최대(37) 와 겹치지 않게 **200 블록**(200~211) 사용.
3. **수치 단일 소스 = tier const** — 각 tier 의 `DescriptionArgs` 가 자기 `const` 를 문자열로 포맷. 파생 표기는 tier 가 계산:
   - Dps2: `CooldownMul 0.8` → `공속 +{0}%` 의 `{0}` = `(1/0.8-1)*100 = 25`.
   - Debuff3: `Ratio 0.01` → `-{0}%/s` 의 `{0}` = `1`.
4. **BuildRows 는 provider 를 주입받는다** — `SynergyModalPopup.BuildRows(countOf, tierOf, strings)` 로 `IStringProvider` 를 파라미터로 받아 `string.Format(strings.GetString(tier.DescriptionStringId), tier.DescriptionArgs)`. null provider 가드(설명 빈 문자열 대신 안전 처리). 테스트는 fake provider 주입 → 회귀 가능.
5. **tier 조회** — `BuildSynergyService` 에 `IBuildSynergyTier GetTier(EBuildAxis axis, int threshold)` 추가(바인딩된 `_tiers` 노출). BuildRows 가 축·티어로 실제 tier 인스턴스를 얻어 설명을 뽑는다.

## 4. 아키텍처 / 데이터 흐름

```
SynergyModalPopup.BuildRows(countOf, tierOf, strings)
  └ 활성 축·티어마다:
      tier = tierOf(axis, threshold)              // BuildSynergyService.GetTier
      template = strings.GetString(tier.DescriptionStringId)   // Strings_Ko.json
      desc = string.Format(template, tier.DescriptionArgs)     // 수치 = tier const 유래
      Label = $"Tier{n}  {desc}"
```

- 의존: UI → `IBuildSynergyTier`(설명 계약) + `IStringProvider`(스트링). tier 는 여전히 `IBattleContext` 만 알고, 추가로 자기 설명 메타(id+args)만 노출 — 스트링 provider 나 UI 를 모른다.
- `IStringProvider` 는 기존 `ChvjUnityInfra` 인터페이스(`GetString(int)`). 실 provider 는 `CHText.StringProvider`(부팅 세팅), 테스트는 fake.

## 5. ⚠️ Tank Tier3 표시-효과 불일치 (기존 버그, 단일 소스화로 자동 교정)

- 현재 `TierDesc[(Tank,3)]` = `"필드 캡 +6 (18→24)"`.
- 실제 `TankSynergyTier3.Apply` = `도깨비불·망령 HP ×1.4` (클래스 주석: *"구 캡 +6 을 캡 제거에 따라 테마 일관 내구 강화로 교체"*). 효과는 교체됐는데 **표시만 안 바뀐 stale 버그**.
- 본 리팩터로 tier 가 자기 상수(HpMul 1.4)에서 설명을 뽑으면 **자동으로 `"도깨비불·망령 HP ×1.4"` 로 교정**된다. README 시너지 표(탱커 7장 "필드 몬스터 상한 ↑")도 같은 stale — 별도 후속으로 표기 정리 권장.

## 6. 티어별 템플릿·인자 (구현 기준표)

| 축·Tier | const (소스) | 스트링 템플릿(Ko) | arg |
|---|---|---|---|
| Tank1 | HpMul 1.3 | `도깨비불·망령 HP ×{0}` | `1.3` |
| Tank2 | PowerMul 1.2 | `도깨비불·망령 공격력 ×{0}` | `1.2` |
| Tank3 | HpMul 1.4 | `도깨비불·망령 HP ×{0}` | `1.4` |
| Dps1 | PowerMul 1.3 | `사신·저주술사 공격력 ×{0}` | `1.3` |
| Dps2 | CooldownMul 0.8 | `사신·저주술사 공속 +{0}%` | `25` |
| Dps3 | RangeMul 1.3 | `사신·저주술사 사거리 ×{0}` | `1.3` |
| Debuff1 | SlowMul 0.8 | `역병귀 둔화 ×{0}` | `0.8` |
| Debuff2 | Factor 0.85 | `영웅 공격력 ×{0}` | `0.85` |
| Debuff3 | Ratio 0.01 | `출혈 영구 — 이동 시 1s당 HP -{0}%` | `1` |
| Swarm1 | MoveMul 1.3 | `환령·도깨비불 이동속도 ×{0}` | `1.3` |
| Swarm2 | PeriodMul 0.85 | `모든 스포너 주기 ×{0}` | `0.85` |
| Swarm3 | OutputDelta 1 | `모든 스포너 동시 출력 +{0}` | `1` |

- 수치 포맷: 배율은 소수 그대로(`1.3`), 정수(`1`)·파생 퍼센트(`25`)는 정수. tier 가 `DescriptionArgs` 에서 문화권 무관 포맷(`InvariantCulture`, 불필요한 0 제거)으로 만든다.

## 7. 테스트 관점

- 기존 `SynergyModalPopupBuildTests.TierDesc_12개_키_전부_채워짐()` 등은 provider 미주입 시 깨진다 → **fake `IStringProvider`(12개 템플릿 반환) 주입**으로 갱신. test-engineer 담당.
- 신규: 각 tier 의 `DescriptionStringId`/`DescriptionArgs` 가 상수와 일치(예: Dps2 arg=="25", Tank3 arg=="1.4"), 12 tier 전부 id·arg 채워짐, `string.Format` 결과가 기대 문자열.
- 회귀: `BuildRows` 구조(행 수·RowKind·헤더 라벨·Tier 접두)·시너지 발화 로직(`BuildSynergyService`) 무변경.
- null provider 가드: provider null 이어도 예외 없이 안전 문자열.

## 8. 리스크 / 주의

- `IBuildSynergyTier` 인터페이스 확장 → 12개 구현체 전부 멤버 추가(누락 시 컴파일 에러가 잡아줌).
- 스트링 id 200 블록이 기존과 안 겹치는지 확인(현재 최대 37).
- Tank3 표시가 바뀌므로(player-facing) 최종 사용자 확인 대상으로 명시.
