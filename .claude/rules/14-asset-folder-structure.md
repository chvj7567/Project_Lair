# Rule 14 — 에셋 폴더 구조 (Art 하위 타입별 정리)

## 룰
Addressable 로 로드되는 모든 게임 에셋(프리팹/SO/이미지/머티리얼)은
`Assets/_Project/Art/` 하위에 **에셋 타입별**로 정리한다.

## 폴더 구조
```
Assets/_Project/Art/
  ├ Characters/  — 캐릭터 프리팹 (영웅/몬스터)
  ├ FX/          — 이펙트/시각효과 프리팹 (PoisonAura, 상태 visual 등)
  ├ UI/          — UI 프리팹 (BattleHud, ResultPopup, HpBar 등)
  ├ Cards/       — CardPool SO + Items/ 하위에 카드 데이터 SO
  ├ Materials/   — 모든 머티리얼
  └ Sprites/     — 모든 이미지/스프라이트
```

## 분류 기준 — 에셋 "타입"
- **프리팹** — 종류별 폴더 (`Characters` / `FX` / `UI`)
- **이미지/스프라이트** — 전부 `Sprites/`
- **머티리얼** — 전부 `Materials/`
- **ScriptableObject** — 도메인 폴더 (`Cards` 등)

각 폴더엔 그 타입만 둔다. 예: `UI/` 엔 UI 프리팹만 (이미지는 `Sprites/`, 머티리얼은 `Materials/`).

## 가이드
- Editor 빌더(`LairCharacterPrefabBuilder` 등)의 경로 상수는 `Art/` 기준으로 통일
- 에셋 이동 시 **`.meta` 동행** — GUID 보존 → Addressables 엔트리·프리팹 참조 무손실
- `Resources/` 특수 폴더 사용 금지 — 본 프로젝트는 Addressables(`CHMResource`) 사용
- 새 에셋 타입(아이콘/오디오 등) 추가 시 같은 패턴으로 `Art/` 하위 폴더 신설

## 비대상 (Art 밖)
- `Scripts/`, `Scenes/`, `Editor/`, `Tests/` — 코드/씬
- `Data/Fonts/` — TMP 폰트 (Addressable 아님)
- 씬 사전 배치 비-Addressable 머티리얼(예: Floor) — `Materials/` 에 함께 둬도 무방

## 금지 예시
```
//# (X) 프리팹과 머티리얼이 같은 폴더에 섞임
Assets/_Project/Art/Characters/Slime.prefab
Assets/_Project/Art/Characters/Mat_Slime.mat   ← Materials/ 로

//# (X) 에셋이 Prefabs/ · Data/ 등 Art 밖에 흩어짐
Assets/_Project/Prefabs/Characters/Slime.prefab
Assets/_Project/Data/Cards/SlimeHpBoost.asset
```

## 권장 예시
```
//# (O) 타입별 Art 하위 정리
Assets/_Project/Art/Characters/Slime.prefab
Assets/_Project/Art/Materials/Mat_Slime.mat
Assets/_Project/Art/Cards/Items/SlimeHpBoost.asset
Assets/_Project/Art/Sprites/HpBar.png
```

## Rule 08 과의 관계
- Rule 08: Enum 키 = 에셋 파일명 (대소문자 일치)
- Rule 14: 그 에셋들이 놓이는 *폴더 구조*
- 둘 다 충족 — 파일명은 Enum 값명, 위치는 `Art/` 하위 타입별
