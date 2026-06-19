# Card Ideas — 2026-06-20 — 영웅 저체력 포식자 3종

> 자동 생성 (매일 07:01 KST) — Project Lair Daily Card Idea 루틴 (Rule 01 자동화 예외).
> v0.2 풀 확장 대비 비축. MVP §11 매수 lock 은 고수.

## 0. 오늘 제안 개요

- 테마: 픽 시점 영웅 HP 비율 조건부 수치 분기 — 영웅이 약할수록 효과가 극대화되는 포식자형 카드 3종
- 목록: BloodScent (피 냄새) / PanicStampede (공황 돌격) / DeathKnell (임박 종소리)
- 기존 28장 + git log 과거 회차(10회)와의 중복 회피 확인됨

## 1. BloodScent — 피 냄새

- **카테고리**: 패시브 강화 (Dps 축)
- **효과 모델**: 픽 시점 영웅 HP ≤ 50% → Reaper 종 글로벌 공격력 × 1.5 영구 적용. 픽 시점 HP > 50% → × 1.15 적용. "언제 픽하느냐"가 효과 강도를 결정하는 전략 요소. 중첩 픽 시 배율 누적(× 1.5 × 1.5 등).
- **구현 패턴**: `ICardEffect.Apply(IBattleContext ctx)` 내에서 `ctx.HeroHpRatio < 0.5f ? 1.5f : 1.15f` 분기 → `MonsterBuffService.ApplyGlobalBuff(EMonsterType.Reaper, EStat.Power, multiplier, permanent: true)`. 기존 `HeroAttackDownEffect` 의 IBattleContext 읽기 패턴 참조.
- **시너지 후크**: ReaperAtkSpeed(공속 × 0.7) + BloodScent(공격력 × 1.5) = HP 50% 이후 Reaper 가 "빠르고 세진다". Debuff 축(Bleed·HeroPoisonAura)으로 영웅 HP 를 50% 아래로 먼저 깎으면 BloodScent 최대값 픽이 보장됨 — 축 간 준비-수확 시너지.
- **구현 비용 추정**: 2 (Apply 내 HP 비율 분기 1줄 추가. MonsterBuffService 확장 불필요, 기존 GlobalBuff API 재사용)
- **중복 재검증**: 기존 ReaperAtkSpeed(공속), HexRangeBoost(사거리)는 무조건 영구 버프. BloodScent 는 "픽 타이밍에 따라 수치가 달라지는 조건부 분기" 첫 사례. git log 10회차 subject 어디에도 "픽 시점 HP 분기" 패턴 없음.

## 2. PanicStampede — 공황 돌격

- **카테고리**: 패시브 강화 (Swarm 축)
- **효과 모델**: 픽 시점 영웅 HP ≤ 30% → Phantom 종 글로벌 이동속도 × 2.0 영구 적용. 픽 시점 HP > 30% → × 1.3 적용. (기존 PhantomMoveSpeedBoost 는 무조건 × 1.5 — 조건부 2.0 은 후반 전용 고위험·고보상 슬롯.) 중첩 픽 시 배율 누적.
- **구현 패턴**: `ctx.HeroHpRatio < 0.3f ? 2.0f : 1.3f` → `MonsterBuffService.ApplyGlobalBuff(EMonsterType.Phantom, EStat.MoveSpeed, multiplier, permanent: true)`. PhantomMoveSpeedBoost 의 `PhantomMoveSpeedBoostEffect` 를 템플릿으로 분기 로직만 추가.
- **시너지 후크**: PhantomMoveSpeedBoost(× 1.5 영구) + SpawnPhantoms(출력 +1) + PanicStampede(HP ≤ 30% 시 × 2.0) = "말기 Phantom 포위망" 빌드. HP 트리거가 10%마다 발생하므로 HP 30% 이하 구간(3번 남은 패시브)에서 PanicStampede 픽 타이밍 계산이 핵심 의사결정이 됨.
- **구현 비용 추정**: 2 (PhantomMoveSpeedBoostEffect 파생 클래스 또는 분기 로직 5줄 추가)
- **중복 재검증**: git log 06-11 회차의 "PhantomHpBoost" 는 HP(체력) 증가. PanicStampede 는 이동속도 조건부 분기. 효과 유형·발동 조건 모두 다름. 과거 10회차 어디에도 "픽 시점 30% 이하 이동속도 분기" 없음.

## 3. DeathKnell — 임박 종소리

- **카테고리**: 액티브 저주 (Debuff 축)
- **효과 모델**: 발동 시 현재 영웅 HP 비율에 따라 공포(도주) 지속시간이 달라짐. 공식: `duration = 3 + (1 - hpRatio) × 7` 초. HP 100% = 3 s, HP 50% = 6.5 s, HP 20% = 8.6 s, HP 10% = 9.3 s (사실상 최대 10 s). 기존 Fear(고정 3 s) 대비 후반 극대화형 저주.
- **구현 패턴**: `FearEffect.Apply` 를 참조해 `DeathKnellEffect.Apply(IBattleContext ctx)` 구현. `float duration = 3f + (1f - ctx.HeroHpRatio) * 7f;` → `IHeroAura.AddFear(duration)` 호출. 약 10줄 신규 파일.
- **시너지 후크**: Fear(3 s 고정) → DeathKnell(HP 비례 최대 10 s). Slow(이동속도 × 0.5) 와 같이 쓰면 도망치는 영웅이 느리기까지 해 Reaper 추격 시간 확보. 영웅 HP 를 빠르게 깎는 Dps/Debuff 빌드에서 DeathKnell 의 실효 duration 이 자연스럽게 증가함.
- **구현 비용 추정**: 2 (FearEffect 복사 + duration 계산식 1줄 교체. IBattleContext.HeroHpRatio 이미 exist 전제)
- **중복 재검증**: 기존 Fear 는 3 s 고정. Bleed 는 이동 시 HP 감소(디버프 종류 다름). DeathKnell 은 "HP 비례 공포 지속" 첫 사례. git log 10회차에 "HP 비례 액티브 저주" 없음.

## 4. 공통 테마 고찰

3장 모두 `IBattleContext.HeroHpRatio` 를 Apply 시점에 읽어 효과 강도 또는 지속시간을 동적으로 결정한다. 기존 28장의 효과는 "무조건 × N" 형 영구/시간 제한 버프였고, 과거 루틴 10회차도 이 틀 안에서 움직였다. 오늘 3장은 "언제 픽/발동하는가"가 효과 크기를 바꾸는 첫 조건부 분기 패턴 제안이다.

게임 흐름 상 영웅 HP 는 단방향 감소(자연 회복 없음). 따라서 이 조건은 "런의 단계"와 동의어다 — 초반(HP 높음)에 픽하면 약한 버전, 후반(HP 낮음)에 픽하면 강한 버전. 플레이어가 "지금 픽 vs 나중 픽"을 저울질하는 새 의사결정 축이 생긴다.

QA 리포트(2026-05-22) 는 시뮬레이션 미실행 상태이므로 픽률 데이터는 없으나, 컨셉 §8 밸런싱 기준(영웅 2~4분 사망) 안에서 "후반 2~4분 구간에 킬 포텐셜 집중"이 필요한 시너지 공백을 메운다.

## 5. 채택 흐름 제안

- 채택 시 game-designer 호출 입력으로 이 문서를 전달
- IBattleContext.HeroHpRatio 인터페이스 메서드 현재 존재 여부를 gameplay-programmer 가 사전 확인 필요
- v0.2 진입 전까지 backlog 보관

## 6. 쉬운 설명 (비개발자 요약)

지금 게임에서 영웅은 시간이 지날수록 점점 HP가 줄어든다. 보통 카드를 고를 때는 "이 카드가 좋은가"만 보면 됐는데, 오늘 제안하는 카드들은 "지금 영웅 HP가 얼마냐"에 따라 효과가 달라진다. 예를 들어 리퍼 몬스터를 강화하는 카드를 영웅이 많이 다쳤을 때 고르면 평소보다 훨씬 강한 버전이 적용되고, 영웅이 거의 죽기 직전에 공포 카드를 쓰면 평소 3초짜리 도망이 10초 가까이 늘어난다. 쉽게 말해 "다친 영웅을 더 세게 두들겨 패는" 카드들이다. 그래서 오늘 제안하는 카드 3장은: 영웅이 약해진 타이밍을 놓치지 않고 더 강한 버프/저주를 터뜨리는 포식자형 카드들이다.
