# Rule 00 — 프로젝트 메타 파일 `.claude/project.md`

> 0번 룰은 다른 모든 룰·에이전트의 **진입점**이다. 코딩 룰(01~14) 이 적용되기 전에, 이 룰이 정의하는 메타 파일을 통해 프로젝트 자체가 식별된다.

## 룰
모든 서브에이전트(`.claude/agents/*.md`)는 **`.claude/project.md`** 을 작업 시작 시 가장 먼저 읽는다. agent 정의는 도메인 정보를 직접 들고 있지 않고 이 파일을 통해서만 프로젝트를 인지한다.

이 규약 덕분에 agent 와 룰은 도메인 비종속이며, 다른 프로젝트로 옮길 때는 **`.claude/project.md` 한 파일만 갈아끼우면 된다**. `CLAUDE.md` 는 자유 양식으로 작성해도 무방하다.

## 파일 위치
`.claude/project.md`

## 형식
Markdown. `## 섹션 ` 으로 그룹, `- **key**: value` 로 키-값. 중첩은 하위 bullet 들여쓰기.

```markdown
## 코드 / 인프라
- **engine**: Unity 6 (6000.0.68f1)
- **infrastructure**
  - **package_id**: com.chvj.unityinfra
  - **alias**: ChvjPackage
```

키 참조는 dot notation 으로 한다 — 예: `infrastructure.package_id`, `test_paths.edit_mode`.

## 필수 키

| 키 | 의미 |
|---|---|
| `name` | 프로젝트 이름 |
| `one_liner` | 한 줄 컨셉 |
| `concept_doc` | 프로젝트 컨셉서 경로 (예: `docs/design/<project>_concept.md`) |
| `stage` | 현재 단계 (MVP / Alpha / Beta 등) |
| `stage_goal` | 현재 단계의 검증 가설 |
| `code_root` | 게임 코드 루트 폴더 |
| `test_paths.edit_mode` · `test_paths.play_mode` | 테스트 폴더 (Unity 외 엔진이면 다른 키 추가) |
| `infrastructure.package_id` · `alias` · `path` | 인프라 패키지 식별자 |
| `docs.design` · `docs.qa_reports` · `docs.specs` · `docs.plans` | 문서 폴더들 |

## 선택 키
- `concept_sections.<name>` — 컨셉서 안의 § 번호 단축키. agent 가 자주 점프하는 섹션 (예: `stage_scope: 11`)
- `namespace` · `architecture` · `engine` · `language` · `test_framework` · `test_method_naming`
- `balance_config_asset` · `card_data_folder` 같은 프로젝트 도메인 데이터 경로

선택 키가 없으면 agent 는 해당 정보를 건너뛰거나 사용자에게 묻는다.

## 갱신 규칙
- **다른 프로젝트로 이전 시** — 이 파일만 새 프로젝트 값으로 갱신. agent / 룰은 그대로 둔다.
- **단계 변경 시** (예: MVP → Alpha) — `stage` · `stage_goal` · `concept_sections` 를 갱신
- **폴더 구조 변경 시** — `code_root` · `test_paths` · `docs.*` 를 갱신
- **인프라 패키지 변경 시** — `infrastructure.*` 와 룰 07/11/12 (인프라 종속 룰) 을 함께 검토

## 읽기 패턴 (agent 측)
agent 본문은 다음 패턴으로 메타 파일을 읽는다:

```
1. `.claude/project.md` 을 읽는다 — 프로젝트 메타 파악
2. `concept_doc` 경로로 컨셉서를 읽는다 — 핵심 메커니즘 · 밸런싱 기준
3. (필요 시) `concept_sections.<name>` § 번호로 컨셉서의 해당 섹션으로 점프
4. `code_root` · `test_paths` · `docs.*` 등으로 작업 위치 식별
```

## 금지 예시

```markdown
## 프로젝트
- **name**: My Game
- **stage**: MVP

<!-- (X) concept_doc 누락 — agent 가 컨셉서를 못 찾음 -->
```

```markdown
- **concept_doc**: ./docs/design/concept.md

<!-- (X) 상대 경로 prefix `./` 사용 — agent 의 경로 해석이 OS별로 다를 수 있음 -->
```

## 권장 예시

`.claude/project.md` 의 본 프로젝트 값은 실제 파일 참조. 다른 프로젝트의 최소 예시:

```markdown
# Project Meta

## 프로젝트
- **name**: My New Project
- **one_liner**: 짧은 한 줄 설명

## 컨셉 / 단계
- **concept_doc**: `docs/design/mynewproject_concept.md`
- **stage**: MVP
- **stage_goal**: 검증하려는 가설 한 줄

## 코드 / 인프라
- **code_root**: `Assets/_MyProject/`
- **test_paths**
  - **edit_mode**: `Assets/_MyProject/Tests/EditMode/`
  - **play_mode**: `Assets/_MyProject/Tests/PlayMode/`
- **infrastructure**
  - **package_id**: com.chvj.unityinfra
  - **alias**: ChvjPackage
  - **path**: `Packages/com.chvj.unityinfra/`

## 문서 위치
- **docs**
  - **design**: `docs/design/`
  - **qa_reports**: `docs/qa-reports/`
  - **specs**: `docs/superpowers/specs/`
  - **plans**: `docs/superpowers/plans/`
```

## 메인 오케스트레이터 행동 규칙

`.claude/project.md` 은 메타 파일이자 **메인 오케스트레이터(사용자와 직접 대화하는 최상위 Claude)의 행동 규칙**도 함께 정의한다. agent 가 아닌 메인이 사용자의 메시지를 받았을 때 따르는 정책이며, 도메인 비종속이므로 다른 프로젝트로 그대로 이식된다.

### 스킬 미지정 요청 — 후보 제시 게이트

사용자가 **코드/에셋 변경이 명확한 작업 요청**을 보냈는데 메시지에 스킬 이름(`/<skill>`)을 명시하지 않은 경우, 메인은 즉시 작업을 시작하지 않는다. 프로젝트가 제공하는 작업 스킬 후보를 표로 제시하고 사용자가 선택할 때까지 멈춘다.

**필수 키 추가**:

`.claude/project.md` 에 다음 섹션이 있어야 메인이 후보 표를 만들 수 있다:

```markdown
### 스킬 미지정 요청 — 메인 후보 제시 규칙

| 스킬 | 적합 작업 | 파이프라인 단계 |
|---|---|---|
| `/start-develop`       | ... | ... |
| `/start-develop-auto`  | ... | ... |
| `/start-develop-simple`| ... | ... |
| `/start-develop-quick` | ... | ... |
```

후보 스킬 수는 프로젝트마다 다를 수 있다 (2~N개). 표 헤더는 `스킬 | 적합 작업 | 파이프라인 단계` 컬럼 3개 권장.

**제외 케이스** (후보 제시 없이 메인이 즉시 답변):
- 메타 질문 ("X 가 뭐임?", "어떻게 해야 돼?")
- 단순 조회 · 탐색 · 파일 읽기 요청
- 조언 · 추천 요청 (코드 변경을 명시하지 않은 의견 요청)
- 일반 대화 · 명령 의도가 모호한 발화

**스킬 이름이 메시지에 명시된 경우** — 후보 제시 생략. 박힌 스킬로 즉시 진행.

**메인의 자체 분기 금지** — 메인이 "이건 quick 으로 충분" 같은 임의 판단으로 한 스킬에 직진하지 않는다. 잘못 판단하면 큰 작업이 게이트 없이 직진할 위험이 있으므로 사용자 선택이 단일 진실.

### 적용 흐름

```
1. 사용자 메시지 수신
2. .claude/project.md 읽기 (이미 캐시되어 있으면 생략)
3. 메시지가 "코드/에셋 변경 요청"인지 판단
   - 예 + 스킬 이름 명시 없음 → 후보 표 제시 후 멈춤
   - 예 + 스킬 이름 명시 → 해당 스킬 즉시 호출
   - 아니오 (제외 케이스) → 즉시 답변
```

## 적용 시점
- 새 프로젝트에 본 agent / 룰 세트를 적용할 때 — `.claude/project.md` 을 새 프로젝트 값으로 작성
- 단계·폴더·인프라가 바뀔 때 — 해당 키 갱신
- agent 가 새 메타를 의존하게 될 때 — 이 룰의 필수/선택 키 표를 갱신
- 메인 오케스트레이터 행동 규칙이 추가/변경될 때 — 본 룰의 "메인 오케스트레이터 행동 규칙" 섹션 갱신

## 비고
- 이 파일은 **`CLAUDE.md` 와 별개**다. `CLAUDE.md` 는 사람을 위한 자유 양식 문서, `project.md` 는 agent 가 읽는 구조화된 메타 (Markdown 이지만 키-값 규약 준수).
- 두 곳에 중복된 정보가 있으면 `project.md` 가 진실의 원천(SoT)이다.
