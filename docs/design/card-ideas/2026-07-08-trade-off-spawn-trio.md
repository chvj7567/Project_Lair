# Card Ideas — 2026-07-08 — 스포너 증산·영웅 약화 동시 거래형 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- **테마**: 대가 강화 (Trade-Off Spawn) — 스포너 동시 출력 +1(영구)을 받는 대신 영웅에게 영구 패널티를 함께 부여하는 "거래형 패시브" 3종. 기존 28장은 카드 효과에 댓가가 없음 (SpawnX는 순수 출력 증가, HeroDebuff는 순수 약화). 이 3장은 강화와 약화를 묶어 선택 자체를 딜레마로 만든다.
- **목록**: BloodPact / SoulBargain / SwampBond
- **기존 28장 + git log 과거 39회 회차와의 중복 회피 확인됨**: 스포너 출력과 영웅 영구 디버프를 하나의 카드에 묶은 거래형 패턴은 어느 회차에서도 제안된 적 없음. (가장 유사한 회차: `2026-06-11-cross-axis-swap-trio.md`는 스포너 종 교체이며 영웅 패널티 없음. `2026-06-09-kill-echo-penalty-trio.md`는 처치 패널티이며 스포너 출력 변경 없음.)

---

## 1. BloodPact (피의 계약) — 가칭

- **카테고리**: 패시브 추가 (Spawn) / **Dps 축**
- **효과 모델**
  - Reaper 스포너 동시 출력 +1 (영구). (= SpawnReapers 동일 효과)
  - 동시에 영웅 공격력 영구 ×0.85 (= HeroAttackDown의 -15% 버전).
  - 두 효과가 하나의 Apply()에서 순차 발동. 되돌릴 수 없음.
  - 수치 근거: 영웅 공격력 50/타 → 42.5/타. Reaper HP 100이면 2타로 처치(기존 1타 → 2타 전환). 리퍼 생존 시간 1배 → 2배 ≈ 리퍼 1마리가 영웅에게 주는 누적 DPS가 최대 2배 올라간다. 영웅 2~4분 사망 목표 안에서 의미 있는 압박.
- **구현 패턴**: `SpawnReapersEffect` 로직 + `HeroAttackDownEffect` 의 HeroAttackDownAura 부착을 단일 `BloodPactEffect.Apply()` 에서 순차 호출. 인터페이스 확장 0. 신규 시스템 0.
- **시너지 후크**
  - Dps Tier1 (Reaper·Hex Power ×1.3) → 리퍼가 늘어나면서 동시에 영웅 공격력까지 낮아져 리퍼가 더 오래 생존.
  - ReaperAtkSpeed와 조합 시 "많고 빠른 리퍼 + 약해진 영웅" 압박 극대화.
  - 역시너지: BloodPact 선택 후 Weaken(영웅 공격력 ×0.5, 10초) 추가 시 교전 중 영웅 공격력이 ×0.425로 급락 → 의도적 조합 가능.
- **구현 비용 추정**: 1 (기존 두 효과 클래스 인스턴스 생성 + 순차 Apply 호출만)
- **중복 재검증**: SpawnReapers(순수 추가, 댓가 없음)와 구별. HeroAttackDown(순수 디버프, 스포너 없음)과 구별. Dps 축 스폰 카드 중 댓가 있는 변종은 기존 및 과거 39회 제안에 없음.

---

## 2. SoulBargain (영혼 거래) — 가칭

- **카테고리**: 패시브 추가 (Spawn) / **Swarm 축**
- **효과 모델**
  - Phantom 스포너 동시 출력 +1 (영구). (= SpawnPhantoms 동일 효과)
  - 동시에 영웅 최대 HP 영구 ×0.93 (현재 HP 비율 유지): `hero.TakeDamage(Math.Round(hero.Max × 0.07))` + MaxHp 내부 배율 조정.
  - 적용 후 영웅 HP 1000 → 930. 10% 임계는 93 단위로 내려감 → 패시브 트리거 간격이 실제론 같지만 절대 HP 여유가 줄어 타이밍 압박 상승.
  - 수치 근거: Phantom이 무리 압박을 강화하는 동시에 영웅의 생명 풀을 갉아먹어 "영혼을 내준 거래" 내러티브 완성. ×0.93 × 복수 픽 시 0.93^2 ≈ 0.865 (최대 2~3 중첩 허용 가정).
- **구현 패턴**: `SpawnPhantomsEffect` 로직 + `IHealth.Max × 0.07` 만큼 `TakeDamage` 즉발 (현재 HP 보정). IHealth 인터페이스 확장 없이 구현 가능. HeroAuraRunner 불필요 (무제한 지속이 아니라 즉발 Max HP 변경 모델).
- **시너지 후크**
  - Swarm Tier3 (스포너 동시 출력 +1 영구) + SoulBargain 중첩 시 팬텀 스포너에서 3마리/주기 출력 가능.
  - SpawnerHaste + SoulBargain: 팬텀이 빠르게 쏟아지는 와중 영웅 HP 풀이 좁아져 패시브 트리거가 촉진되는 느낌.
  - 역시너지: 영웅 HP가 낮아질수록 IronWill 등 수비 카드의 "영웅 공격력 낮추기" 시너지가 줄어드는 희석 효과도 발생 → 선택 고민 강화.
- **구현 비용 추정**: 2 (스포너 출력 패턴 + MaxHp 조정 처리 — MaxHp 프로퍼티 setter 또는 즉발 데미지 처리 분기)
- **중복 재검증**: SpawnPhantoms(순수 추가)와 구별. 과거 39회 중 "최대 HP 감소 + 스포너 증가" 묶음 제안 없음.

---

## 3. SwampBond (늪의 유대) — 가칭

- **카테고리**: 패시브 환경 (Environment) / **Debuff 축**
- **효과 모델**
  - Plague 스포너 동시 출력 +1 (영구). (= SpawnPlagues 동일 효과)
  - 동시에 영웅 이동속도 영구 ×0.9 (PermanentMoveSlowAura, 무제한): `ctx.ApplyHeroAura(new PermanentMoveSpeedAura(0.9f), -1f)`.
  - Slow(액티브, ×0.5 × 10초)과 다름: 이 카드는 영구이며 수치가 -10%로 완만하지만 중첩 시 0.9^2 ≈ 0.81.
  - 수치 근거: 플레이그 추가 스폰 → 둔화 공격 몬스터 증가 + 영웅 기본 이속까지 낮아짐. 플레이그가 공격 시 둔화 20%를 부여하므로 SwampBond 후 영웅 이속이 기본 ×0.9 × 0.8 = ×0.72 수준까지 떨어질 수 있음 → 탈출 어려움.
- **구현 패턴**: `SpawnPlaguesEffect` 로직 + `ApplyHeroAura(PermanentMoveSpeedAura, -1f)`. `PermanentMoveSpeedAura`는 `OnAttached` 시 `IMover.Speed ×= 0.9`, `OnDetached` 시 역 복원 (무제한이라 OnDetached 발동 안 됨).
- **시너지 후크**
  - PlagueSlowBoost + SwampBond: 플레이그 둔화가 배율 ×0.75로 강해지는 동시에 영웅 기본 이속도 줄어 "완벽한 함정".
  - Bleed(이동 시 HP -2%/초, 10초) + SwampBond: 영웅이 느리게 이동하더라도 계속 이동해야 하므로 출혈 틱이 지속적으로 발동.
  - Swarm Tier1 (Phantom·Wisp 이속 ×1.3): 영웅 이속은 줄고 팬텀·위스프 이속은 오르니 추격 격차가 더 벌어짐.
- **구현 비용 추정**: 2 (스포너 출력 + 영구 이속 감소 오라 — PermanentMoveSpeedAura 클래스 신규, 단 PoisonAura 패턴 그대로)
- **중복 재검증**: SpawnPlagues(순수 추가)와 구별. Slow(10초 한시, ×0.5, 몬스터 이속도 함께 변경)와 구별. `2026-05-30-plague-poison-chain.md` 는 플레이그+독 연쇄이며 스포너 댓가형 아님.

---

## 4. 공통 테마 고찰

세 카드 모두 "스포너 출력 +1(영구)"이라는 강력한 이득과 "영웅 영구 약화"라는 비용을 함께 묶는다. 기존 28장 패턴(SpawnX = 순수 이득, HeroDebuff = 순수 약화)과 달리, 이 3장은 한 번의 선택에서 두 축이 동시에 움직인다.

**전략적 효과**: 플레이어가 "리퍼 1마리 더 나오는 게 영웅 공격력 -15%를 감수할 만한가?"를 계산해야 한다. 이미 HeroAttackDown을 픽했다면 BloodPact의 중첩 공격력 감소(×0.75 × 0.85 = ×0.637)는 달콤한 기회. 반대로 Tank 빌드로 영웅을 이미 충분히 묶어 뒀다면 BloodPact 없이도 충분할 수 있다 → 상황마다 픽 가치가 달라진다.

**QA 연계**: 유일한 QA 리포트(2026-05-22)는 시뮬레이션 미실행(BLOCKED) 상태여서 카드 픽률 데이터가 없다. 단, Debuff 축 SpawnPlagues는 플레이그 스포너가 기본적으로 비활성이라 SpawnPlagues 없이는 아예 등장하지 않는 구조인데, SwampBond를 추가하면 "Plague 스폰 + 이속 감소"를 한 픽으로 해결해 초기 빌드 진입장벽이 낮아진다.

---

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- 세 카드를 동시에 풀에 넣거나, 하나씩 순차 검증 후 채택 가능
- 테스트 체크리스트(채택 시 test-engineer 참조):
  - BloodPact: Apply 후 Reaper 스포너 출력 +1 확인 + `IAttacker.PowerScale` 에 0.85 배율 적용 확인
  - SoulBargain: Apply 후 Phantom 스포너 출력 +1 확인 + 영웅 현재 HP가 ×0.93 비율 유지 상태로 조정 확인
  - SwampBond: Apply 후 Plague 스포너 출력 +1 확인 + 영웅 `IMover.Speed` 배율 0.9 적용 확인 (무제한 지속)
  - 공통: 다른 디버프와 중첩 시 곱연산으로 누적되는지 확인
- v0.2 진입 전까지 backlog 보관

---

## 6. 쉬운 설명 (비개발자 요약)

보통 카드를 고르면 좋은 일만 생긴다. 리퍼 한 마리 더 나오거나, 영웅이 느려지거나 — 둘 중 하나. 그런데 오늘 제안하는 세 카드는 다르다. "리퍼가 하나 더 나오고 싶으면 영웅 공격력을 깎아라", "팬텀 무리를 늘리고 싶으면 영웅 최대 HP를 줄여라"처럼, 강화를 받는 대가로 반드시 무언가를 내줘야 한다. 마치 악당이 악마와 계약하는 것처럼, 던전이 강해질수록 영웅도 조금씩 약해지는 쪽으로 판이 기울어진다. 그래서 오늘 제안하는 카드 3장은: 리퍼 계약(BloodPact), 팬텀 거래(SoulBargain), 플레이그 유대(SwampBond) — 모두 "더 강한 몬스터, 더 약한 영웅"을 동시에 만드는 양날의 카드다.
