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

Addressable 로 로드되는 모든 게임 에셋은 `Assets/_Lair/Art/` 하위에 에셋 타입별로 정리한다.

```
Assets/_Lair/Art/
  ├ Characters/  — 캐릭터 프리팹 (영웅/몬스터)
  ├ FX/          — 이펙트/시각효과 프리팹
  ├ UI/          — UI 프리팹 (BattleHud, ResultPopup 등)
  ├ Cards/       — CardPool SO + Items/ 하위에 카드 데이터 SO
  ├ Materials/   — 모든 머티리얼
  └ Sprites/     — 모든 이미지/스프라이트
```

각 폴더엔 그 타입만 둔다. 예: `UI/` 엔 UI 프리팹만 (이미지는 `Sprites/`, 머티리얼은 `Materials/`).

**가이드**:
- 에셋 이동 시 `.meta` 동행 — GUID 보존 → Addressables 엔트리·프리팹 참조 무손실
- `Resources/` 특수 폴더 사용 금지 — Addressables(`CHMResource`) 사용
- 새 에셋 타입 추가 시 `Art/` 하위 폴더 신설

```
//# (X) 프리팹과 머티리얼이 같은 폴더에 섞임
Assets/_Lair/Art/Characters/Slime.prefab
Assets/_Lair/Art/Characters/Mat_Slime.mat  ← Materials/ 로

//# (O)
Assets/_Lair/Art/Characters/Slime.prefab
Assets/_Lair/Art/Materials/Mat_Slime.mat
Assets/_Lair/Art/Cards/Items/SlimeHpBoost.asset
```

에셋 파일명 = Enum 값명 (대소문자 일치) — Rule 03 §2 참조.

**비대상** (Art 밖): `Scripts/`, `Scenes/`, `Editor/`, `Tests/`, `Data/Fonts/`
