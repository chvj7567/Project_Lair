# Rule 04 — Unity 에셋

> 구 Rule 04(프리팹화), 14(에셋 폴더 구조) 통합.

---

## 1. 반복 에셋 프리팹화

씬/하이어라키에서 2회 이상 반복되거나 재사용 가능성이 있는 GameObject 구성은 프리팹으로 만든다.

**적용 대상**: UI 셀/아이템, 캐릭터·투사체·이펙트(풀링 대상), 동일 구조 환경 오브젝트, 모든 동적 생성 오브젝트.

**가이드**:
- 변형이 필요한 경우 Prefab Variant 사용 (사본 복제 X)
- 풀링 대상 프리팹은 `IPoolable` 등 표준 인터페이스 구현
- 인스펙터 직접 드래그 대신 Addressables 로 로드

체크리스트:
- [ ] 같은 구조가 2번 이상 등장하는가? → 프리팹화
- [ ] 변형이 있는가? → Prefab Variant
- [ ] 런타임 동적 생성/파괴되는가? → 풀링 + 프리팹 (Rule 03 §4)
- [ ] Addressables 키로 등록되어 있는가? (Rule 03 §2)

---

## 2. 에셋 폴더 구조

Addressable 로 로드되는 모든 게임 에셋은 `<code_root>Art/` 하위에 에셋 타입별로 정리한다. `<code_root>` 는 `project.md` 의 `code_root` 키 값이다.

```
<code_root>Art/
  ├ Characters/  — 캐릭터 프리팹 (플레이어/적)
  ├ FX/          — 이펙트/시각효과 프리팹
  ├ UI/          — UI 프리팹
  ├ Materials/   — 모든 머티리얼
  └ Sprites/     — 모든 이미지/스프라이트
```

위 5개는 어느 프로젝트에나 있는 공통 타입이다. **도메인 고유 에셋 타입은 프로젝트마다 폴더를 신설**하고 `project.md` 의 도메인 데이터 키에 경로를 등록한다 (예: 장비 SO → `Gear/`, 맵 SO → `Maps/`).

각 폴더엔 그 타입만 둔다. 예: `UI/` 엔 UI 프리팹만 (이미지는 `Sprites/`, 머티리얼은 `Materials/`).

**가이드**:
- 에셋 이동 시 `.meta` 동행 — GUID 보존 → Addressables 엔트리·프리팹 참조 무손실
- `Resources/` 특수 폴더 사용 금지 — Addressables(`CHMResource`) 사용
- 새 에셋 타입 추가 시 `Art/` 하위 폴더 신설

```
//# (X) 프리팹과 머티리얼이 같은 폴더에 섞임
<code_root>Art/Characters/Grunt.prefab
<code_root>Art/Characters/Mat_Grunt.mat  ← Materials/ 로

//# (O)
<code_root>Art/Characters/Grunt.prefab
<code_root>Art/Materials/Mat_Grunt.mat
```

에셋 파일명 = Enum 값명 (대소문자 일치) — Rule 03 §2 참조.

**비대상** (Art 밖): `Scripts/`, `Scenes/`, `Editor/`, `Tests/`, `Data/Fonts/`

---

## 3. 프리팹 생성 에디터 툴 — 생성 후 삭제 (일회용)

프리팹을 **코드로 찍어내는 에디터 툴**(예: `*PrefabBuilder` / `<Project>/Build/...` 메뉴)은 **프리팹을 생성한 직후 삭제**한다. 레포에 영구 보존하지 않는다.

- **단일 진실은 생성된 프리팹(.prefab + .meta)** 이다 — 빌더가 아니다. 생성 후 프리팹을 직접 수정/관리한다.
- 빌더를 남겨두면 (a) 실수로 재실행 시 손-편집한 프리팹을 덮어쓰고(clobber), (b) "빌더 vs 프리팹" 두 진실이 갈린다. 그래서 일회용으로 쓰고 지운다.
- 프리팹 구조를 다시 찍어야 하면 그때 빌더를 재작성해 한 번 돌리고 다시 삭제한다.
- 삭제 대상은 **프리팹 *생성*(authoring) 툴 한정.** 빌드/파이프라인 상시 툴(예: 플레이어 빌드 툴, Addressables 빌드 툴)은 반복 사용하므로 **보존**한다.

```
//# (O) 생성 → 커밋엔 프리팹만, 빌더는 삭제
XxxUIPrefabBuilder.cs 실행 → ConfirmPopup/ToastView/... .prefab 생성
→ 프리팹 5종 + .meta 커밋, XxxUIPrefabBuilder.cs 는 삭제

//# (X) 빌더를 레포에 영구 보존 → 재실행 clobber 위험 + 이중 진실
```

체크리스트:
- [ ] 이 에디터 툴이 프리팹을 *생성*하는 일회용 authoring 툴인가? → 생성 후 삭제
- [ ] 생성된 프리팹·.meta 가 커밋되고 Addressable 등록까지 끝났는가? (Rule 03 §2)
- [ ] 상시 빌드/파이프라인 툴(플레이어 빌드·Addressables 빌드 등)은 아닌가? → 그건 보존
