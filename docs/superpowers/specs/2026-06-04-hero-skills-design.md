# 영웅 스킬 시스템 — Design Spec

> 작성일: 2026-06-04
> 상태: 합의 완료 (brainstorming) → writing-plans 진입 예정
> 주제: 영웅(적 모험가)의 Survivor.io식 광역/회전/돌진 자동 스킬
> 관련 컨셉: `docs/design/project_lair_concept.md` §11 (MVP 범위 — 본 spec 이 확장)

---

## §0. 범위 — ⚠️ MVP §11 확장임을 명시

컨셉 §11 의 MVP 범위는 **영웅 1명 / 근접 단일 공격(`MeleeAttacker`) / 몬스터 6종 / 카드 28장** 이 단일 기준이며, **영웅 액티브 스킬은 그 안에 없다.**

본 기능은 **사용자 명시 승격(2026-06-04)으로 §11 을 확장**한다 (CLAUDE.md §8: "범위 밖 기능은 game-designer 가 명시적으로 승격하기 전까지 착수하지 않는다"). 따라서:

- game-designer 단계에서 컨셉 §11 과 §8 밸런스 타깃에 이 확장을 **정식 반영(annotate)** 한다.
- 비주얼은 §8 프리미티브 + 4색 원칙을 유지한다 (회전 큐브·팽창 실린더 등 기본 도형).
- 사운드·메타·서버는 여전히 비작업 (§8 그대로).

---

## §1. 핵심 발상

영웅 = **역(逆) 서바이버**. 플레이어(던전 주인)가 몰아붙이는 몬스터 무리를, 영웅이 Survivor.io(탕탕특공대)식 광역·회전·돌진기로 쓸어담는다.

- 현재 영웅은 `AutoCombatAI` + `MeleeAttacker` 로 **가장 가까운 몬스터 1마리만 근접 타격**한다.
- 본 기능은 영웅에게 무리를 쓸어내는 자동 스킬을 부여해 **더 강한 보스**로 만든다.
- 플레이어는 이를 압도할 더 강한 빌드를 강요받는다 → 게임에 **새 긴장축**이 추가된다.

검증 가설(컨셉 §11.1 "5분 자동전투 + 트리거 선택지가 재미있는가")에 "영웅이 능동적 위협이 되면 카드 선택의 무게가 커지는가" 를 덧붙인다.

---

## §2. 메커니즘 (brainstorming Q&A 로 확정 — 결정 락)

| 결정 항목 | 확정값 |
|---|---|
| 스킬 주체 | **영웅(적)** 이 자동 시전. 플레이어 조작 아님 |
| 대상 | 영웅에게 몰려오는 **몬스터 무리** |
| 발동 | 쿨다운마다 자동 |
| 획득 모델 | **에스컬레이션** — HP 구간 3페이즈로 점진 획득 (누적, 이전 페이즈 스킬 유지) |
| 트리거 | **HP 구간 3페이즈** (시간 트리거 아님) |
| 스킬 수 | 3종 (페이즈와 1:1) |

### 페이즈 ↔ 스킬 매핑

스킬 3종 ↔ 페이즈 3개를 1:1 로 매핑한다. 페이즈는 **스킬을 추가만** 하고 이전 스킬을 유지한다. 전투 시작(HP 100%) 시점엔 스킬이 없고, 첫 페이즈(HP 90%)부터 순차 획득한다.

| 페이즈 | HP 게이트 (사용자 확정 2026-06-04) | 새로 추가되는 스킬 |
|---|---|---|
| P1 | **90%** | **돌진/관통 (Dash Strike)** — 직선 경로로 돌진하며 일직선상 몬스터 관통 데미지 |
| P2 | **60%** | **+ 회전 블레이드 (Orbiting Blade)** — 영웅 주위를 도는 궤도 투사체. 접촉한 몬스터에 지속 데미지 |
| P3 | **30%** | **+ AOE 노바 (Aoe Nova)** — 쿨다운마다 영웅 주변 원형 폭발. 반경 내 몬스터 일괄 데미지 + 넉백 |

HP 게이트가 카드 픽 트리거(HP 10% 패시브)와 같은 축이므로, 페이즈 전환이 카드 픽 순간과 겹쳐 **극적 연출**이 된다 (플레이어가 위협 증가에 즉시 카드로 대응).

---

## §3. 아키텍처 — 데이터드리븐 폴리모픽 ScriptableObject (사용자 선택: B안)

스킬 behavior 를 SO 서브클래스에 캡슐화하는 **전략 패턴 SO**. 영웅·스킬이 늘어날 v0.2 를 대비하는 확장형.

### 데이터 계층 (ScriptableObject)

- **`HeroSkillData : ScriptableObject` (abstract)** — 모든 스킬의 베이스.
  - 공통 튜닝 필드: `Cooldown`, `DisplayName` 등
  - 팩토리: `IHeroSkillRuntime CreateRuntime()` — 가변 상태를 담는 런타임 인스턴스 생성
- **구체 SO 3종** — 각자 `[CreateAssetMenu]`, 인스펙터 튜닝 필드 보유:
  - `OrbitingBladeSkillData` — 궤도 반경 / 블레이드 수 / 회전 속도 / 데미지 / 히트 간격
  - `AoeNovaSkillData` — 폭발 반경 / 데미지 / 넉백 세기 / 쿨다운
  - `DashStrikeSkillData` — 돌진 거리·길이 / 폭 / 데미지 / 쿨다운
- **`HeroSkillLoadout : ScriptableObject`** — `{ float HpFraction; HeroSkillData Skill; }` 순서 리스트 (페이즈 정의).
  - 기존 `CardPool` SO 패턴을 그대로 따른다. `EData` enum 키로 `CHMResource` 로드 (Rule 03 §2 — 파일명 = enum 값명).

### 런타임 계층

- **`HeroSkillRunner : MonoBehaviour`** (영웅 프리팹 부착) — `HeroAuraRunner` 의 검증된 라이프사이클을 본뜬다.
  - 로드아웃 로드 → 영웅 HP% 폴링 → 임계 돌파 시 해당 SO `CreateRuntime()` 호출해 활성 리스트에 추가 → 매 프레임 활성 런타임 `Tick(ctx, dt)`
  - **풀 재사용 리셋**: 영웅은 풀 객체(count 1)이므로 `OnEnable`/`OnDisable` 에서 활성 리스트·시각 인스턴스를 반드시 리셋 (`HeroAuraRunner` 동일 패턴)
- **`IHeroSkillRuntime`** — `Tick(HeroSkillContext ctx, float dt)`. 가변 상태(쿨다운 타이머·궤도 각도)는 **SO 가 아니라 이 런타임 객체에 보관** (SO 는 공유 에셋이라 인스턴스 상태 금지).
- **`HeroSkillContext`** — 스킬이 월드와 상호작용하는 통로:
  - 영웅 `Transform`
  - 반경 내 몬스터 쿼리 (`CharacterRegistry.Monsters` 기반)
  - 데미지 적용 (`Health.TakeDamage`) / 넉백 적용

> 책임 분리: SO = 데이터 + behavior 정의, Runtime = 가변 상태, Runner = 라이프사이클·페이즈 게이트, Context = 월드 접근. 각 단위는 독립 이해·테스트 가능.

---

## §4. 데이터 흐름

```
영웅 HP 변화
  → HeroSkillRunner 가 HP% 폴링
  → 페이즈 임계(HpFraction) 돌파 감지
  → 해당 HeroSkillData.CreateRuntime() 생성
  → 활성 런타임 리스트에 추가
  → 매 프레임 runtime.Tick(ctx, dt)
       ├ 쿨다운 경과 검사
       ├ ctx 로 반경 내 몬스터 쿼리
       ├ Health.TakeDamage / 넉백
       └ 시각 이펙트 Pop/추적/Push (CHMPool)
```

- **Pause 연동**: 카드 픽 일시정지 중엔 `Time.deltaTime = 0` → Tick 자연 정지 (기존 `MonsterBuffService`/`HeroAuraRunner` 와 동일 패턴).
- **전투 종료 연동**: 영웅 사망/타임오버 시 `HeroSkillRunner` 의 `OnDisable`(풀 Push 또는 AI 정지)이 시각 인스턴스를 정리.

---

## §5. ⚠️ 밸런스 상호작용 (advisor 필수 지적 — 명시 섹션)

메커니즘(궤도/광역/돌진)은 단순하지만, **밸런스가 본 기능의 진짜 설계 난제**다.

### 리스크 1 — Swarm 축 하드카운터
스웜 클리어형 영웅은 4축 중 **Swarm 빌드를 직접 카운터**한다. 28장 카드 세트는 §8 "영웅 2~4분 처치" 에 맞춰 튜닝돼 있어, 영웅의 클리어력이 추가되면 이 튜닝이 깨질 수 있다.

### 리스크 2 — 종반 데스스파이럴
HP 에스컬레이션은 **플레이어가 영웅 HP 를 깎을수록 영웅 클리어력이 커지는** 구조다. 피니시 라인(HP 30% — AOE 노바 획득 시점)에서 영웅이 가장 강해져 feel-bad(역전 불가) 가능성. "코너에 몰린 보스의 발악"(좋은 긴장) vs "피니시 직전 좌절"(나쁜 경험) 사이에서 디자인이 명시적 입장을 취해야 한다.

### 가드레일 (game-designer 가 수치로 확정)
- 노바 쿨다운을 **스웜 리필 시간보다 길게** — 무리가 다시 찰 틈을 보장.
- 페이즈는 **스킬만 추가, 기본 스탯(HP·공격력) 동시 램프 금지** — 이중 강화로 인한 폭주 방지.
- **초당 클리어 상한(clear-per-second cap)** 고려 — 영웅이 무한정 쓸어담지 못하도록.

### 검증
- 본 파이프라인(start-develop)은 **qa-simulator 를 생략**한다. 영웅 스킬은 게임플레이 영향이 큰 케이스이므로, **구현 완료 후 qa-simulator 별도 호출을 강력 권장**한다 (마무리 단계에서 사용자에게 제안).

---

## §6. 구현 순서 (writing-plans 에 위임할 시퀀싱)

big-bang 금지. 스킬 1개씩 추가하며 각 스킬을 스웜 상대로 튜닝 후 다음을 올린다. 페이즈 순서대로 구현한다.

1. SO 베이스 + 런타임/러너/컨텍스트 골격 + **돌진/관통** (P1, HP 90%)
2. **+ 회전 블레이드** (P2, HP 60%)
3. **+ AOE 노바** (P3, HP 30%)

각 단계는 독립 verification gate 를 가진다.

---

## §7. 테스트 / 비주얼

### 테스트 (test-engineer)
- 페이즈 임계 돌파 시 정확히 해당 스킬만 활성화
- 반경/경로 내 몬스터만 피격, 밖은 무피해
- 쿨다운 동작 (연타 방지)
- 풀 재사용(`OnEnable`/`OnDisable`) 후 상태 잔존 없음
- `now`/`dt` 외부 주입형으로 테스트 가능성 확보 (`MeleeAttacker` 패턴)

### 비주얼 (프리미티브, §8 준수)
- 회전 블레이드: 영웅 자식 또는 추적 큐브(들)
- AOE 노바: 팽창하는 반투명 실린더/원판
- 돌진/관통: 늘어난 큐브 또는 라인
- 모두 `CHMPool` Pop/Push + `EVisual` enum 키 (파일명 일치, Rule 03 §2). 신규 EVisual 값 추가 필요.

---

## §8. 미정 항목 (다음 단계 위임)

| 항목 | 위임 대상 |
|---|---|
| ~~HP 게이트 정확값~~ → **확정: 90%/60%/30%** (2026-06-04 사용자) | — |
| 각 스킬 수치 (데미지·반경·쿨다운·넉백) | game-designer |
| 가드레일 수치 (노바 쿨다운, clear-per-second cap) | game-designer |
| 파일 경로·시그니처·TDD 단계·verification gate | writing-plans |
| EVisual/EData 신규 enum 값 명명 | writing-plans → gameplay-programmer |
| §11 / §8 컨셉서 정식 annotate | game-designer |

---

## 변경 이력
- **v0 (2026-06-04)**: 초안. brainstorming Q&A 4문항으로 메커니즘 확정 (영웅 자동 스킬 / HP 3페이즈 에스컬레이션 / 회전·광역·돌진 3종 / 데이터드리븐 SO 아키텍처). 범위 확장·밸런스 상호작용을 명시 섹션으로 포함.
- **v0.1 (2026-06-04)**: 페이즈 매핑 사용자 확정 — P1 HP 90% Dash Strike / P2 HP 60% Orbiting Blade / P3 HP 30% AOE Nova. HP 게이트가 미정→확정으로 이동, 구현 순서를 페이즈 순(돌진→회전→노바)으로 재정렬.
