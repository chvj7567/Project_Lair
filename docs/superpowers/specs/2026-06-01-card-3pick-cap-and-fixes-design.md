# Spec — 카드 3픽 캡(전역 제외) + GuardianRage 정합 + 문서/주석 정리

> 작성: superpowers:brainstorming (메인 오케스트레이터)
> 작성일: 2026-06-01
> 파이프라인: `/start-develop-auto` (uses_superpowers: true)
> 후속: `docs/superpowers/plans/2026-06-01-card-3pick-cap-and-fixes.md` (writing-plans)
> 참고: `docs/design/card-renewal.md` (현행 에셋 동기화본), `docs/design/content-audit/2026-06-01-spawner-haste-stack-cap.md`

---

## 1. 의도 / 검증 가설

- **의도**: 같은 카드를 무한정 반복 픽해 단일 카드로 빌드를 굳히는 패턴을 제한하고(중첩 상한 3), 빌드 시너지(Layer1) Tier3 도달을 *서로 다른 카드 조합*으로만 가능하게 만들어 픽 다양성을 강제한다. 동시에 동기화 과정에서 드러난 GuardianRage 노출/메커니즘 불일치를 코드 쪽에서 정합시키고, 잔여 문서·주석 stale 2건을 정리한다.
- **검증 가설**: "카드별 중첩 상한 3 + 단일 카드 단독 Tier3 불가"가 빌드 다양성을 높이면서도 5분 자동전투의 재미·페이싱을 깨지 않는다.
- **현재 단계 적합성**: MVP 범위 내. 컨셉 §11 카드 매수(P16/A12) lock 불변, 메커니즘만 추가. 아트·사운드·메타 미작업.

## 2. 범위 (3건)

### (A) 전역 3픽 캡 — 신규 메커니즘

- **규칙**: 한 런 동안 같은 카드(ECardId)를 3번 픽하면, 이후 모든 3택 후보 추첨에서 그 카드를 영구 제외한다. 28장 전역 적용.
- **결과**: 모든 카드의 실효 중첩 상한 = 3픽. 4픽 이상 발생 불가 → `card-renewal.md §7` 곱연산 누적표(2픽 ×2.25, 3픽 ×3.375 등)의 3픽이 상한.
- **시너지 상호작용**: 카드 1장은 자기 축 Layer1 카운트에 최대 3 기여. 단일 카드 반복 픽으로 Tier3(7장 임계) 단독 도달 불가 — 같은 축의 서로 다른 카드 조합으로만 7 도달.
- **리셋 단위**: 런(= 라운드, 단일 런 MVP). 전투 Restart 시 `BuildSynergyService.Reset()`과 동일 시점에 픽 카운터도 초기화.

#### 구현 표면 (개요 — 상세는 plan)

- **`CardPickCounter` (신규 POCO)** — `Dictionary<ECardId,int>`. `RecordPick(id)`로 +1, `GetCount(id)`, `IsCapped(id)`(≥3), `Reset()`. BattleController 가 보유.
- **픽 기록 경로 통합** — 기존 픽 지점 2곳(sim `BattleController:594`, 실제 `:615`)에서 카운터 +1. `_recorder.RecordPick` / `_vm.AddPick` 와 같은 블록.
- **`CardDeck.Draw` 제외 필터** — `Draw(int n)` 가 `IsCapped` 카드를 후보에서 제외. 제외 판정은 카운터 주입(예: `Draw(int n, Func<ECardId,bool> excluded)` 또는 생성 시 카운터 참조). 기존 `Min(n, pool.Count)` graceful fallback 유지.
- `_passiveDeck` · `_activeDeck` 가 동일 카운터 공유 (카드는 풀 disjoint 이므로 per-card 캡이 곧 전역).

#### UI (MVP 비주얼 — 프리미티브 + 텍스트 + 4색)

- 3택 팝업의 각 `CardView` 우상단에 픽 카운트 배지: `N/3` (현재 픽수/상한). 예: 2번 픽한 카드 = "2/3" → 다음이 마지막. 3 도달 카드는 추첨 제외되어 노출 안 됨.
- `CHText` 사용 (Rule 03 §3). 신규 색 불요 — 검정 텍스트.

#### 엣지

- **풀 축소**: 패시브 16 / 액티브 12, 런당 픽 약 10회(HP 10% 트리거 ~10 + 30s 트리거 ~10), 캡 3 → 항상 ≥3장 적격. 비현실적 극단에서도 `Draw` 가 가능한 만큼만 반환(기존 동작).
- **3픽째 == Tier 임계 도달 동시**: 픽 카운터 +1 과 시너지 발화는 독립 처리. 3픽째에 axis 카운트도 +1 되어 임계 발화 → 정상. 그 직후 카드가 제외될 뿐. 순서 가드 불요.
- **디버그 경로** (`DebugApplyCard`, `DebugAutoPicker`): sim 경로도 카운터 +1 동일 적용(시뮬 데이터 정합).

### (B) GuardianRage HP×2.0 제거

- `MonsterBuffService.cs` GuardianRage case 에서 `HpMaxScale *= 2f` 제거, `DamageTakenScale *= 0.5f` 만 유지.
- 결과: `Berserk.asset` `_description="...받는 데미지 -50% (15초)"` 와 코드 동작 일치. `card-renewal.md §3.1 #7` 의 "노출/메커니즘 불일치" 플래그 해소.
- **수용한 부작용**: GuardianRage(Tank 보호 액티브)가 약화되어 IronWill(전 몬스터 -30% 15s)·ToughHide/WallOfWisps(Wisp·Wraith -25% 영구)와 역할 일부 중복. 단 Wisp·Wraith 한정 -50% 는 여전히 최강 단일 감소. 본 spec 범위에서 별도 리밸런스 없음(필요 시 후속 밸런스 사이클).

### (C) 정리 2건

- **문서**: `docs/design/card-renewal.md §10` 헤더에 "표면·enum 은 이미 구현 완료(`CommonEnum.cs:92-94`, `BattleContext.cs:119-130`), 본 절은 명세 보존용" 한 줄 보강. (design-reviewer MINOR 권고)
- **주석**: `CommonEnum.cs:82-83` 의 stale 주석(미구현 `SwarmRush`/`SwarmRushEffect`/"스웜 러시"를 구현된 듯 기술)을 `//# 폐기 (SO/풀 ref 제거, enum 자리만 보존, 실제 효과 FastBreedingEffect/빠른 번식)` 으로 정정. (`card-renewal.md §10.1` 요청과 정합)
- **동기화 후속**: game-designer 가 (A) 도입에 맞춰 `card-renewal.md §7 중첩정책`("4픽 이상 발생 불가 — 전역 3픽 캡") + `§9.6 SpawnerHaste`("미구현" → "전역 3픽 캡으로 처리됨") 서술 갱신.

## 3. 비범위 (YAGNI)

- SpawnerHaste 단독 캡(content-audit 의 effect-cap 안) — (A) 전역 캡이 이를 포섭하므로 별도 구현 안 함.
- 카드별 차등 캡(카드마다 다른 상한) — 전역 3 고정.
- 캡 도달 카드의 별도 토스트/사운드 피드백 — MVP §8 사운드 금지, 배지로 충분.
- GuardianRage 외 다른 카드 리밸런스.

## 4. 성공 기준

1. 같은 카드 3픽 후 그 카드가 이후 3택에 나오지 않는다.
2. 모든 곱연산 카드의 실효 상한이 ×value³ 다 (4픽 불가).
3. 단일 카드 반복으로 Layer1 Tier3 단독 발화가 불가능하다.
4. 3택 카드에 `N/3` 배지가 표시된다.
5. GuardianRage 적용 시 Wisp·Wraith 의 HP 가 변하지 않고 받는 데미지만 ×0.5 다.
6. 전투 Restart 시 픽 카운터가 0 으로 초기화된다.
7. EditMode 테스트가 위 1·2·3·5·6 을 커버한다.

## 5. 영향 파일 (개요)

- 신규: `Assets/_Lair/Scripts/Card/CardPickCounter.cs`
- 수정: `CardDeck.cs`(Draw 필터), `BattleController.cs`(카운터 보유·픽 경로·리셋·choices 주입), `MonsterBuffService.cs`(GuardianRage), `CommonEnum.cs`(주석), `CardSelectionPopup.cs`/`CardView`(배지)
- 문서: `docs/design/card-renewal.md`
- 테스트: `Assets/_Lair/Tests/EditMode/` (신규 케이스)
