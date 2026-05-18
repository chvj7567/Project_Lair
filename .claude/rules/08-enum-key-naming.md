# Rule 08 — Enum 키 기반 로드 / 이름 일치 강제

## 룰
ChvjPackage(`CHMResource` / `CHMUI`)는 **Enum.ToString()** 을 키로 사용해 에셋을 로드한다.
따라서 다음 항목은 **Enum 값 이름과 에셋(또는 Addressables 주소) 이름이 정확히 일치**해야 한다.

- Addressables 에셋 (프리팹, ScriptableObject, Sprite 등)
- `CHMUI.ShowUI(EUI.XXX)` 로 띄우는 UI 프리팹
- `CHMResource.Load/Instantiate(EAsset.XXX)` 로 로드하는 모든 에셋
- 씬 로드를 Enum 키로 추상화하는 경우 씬 파일명

## 동작 원리 (참고)
- `CHMResource.Load<T>(Enum key, Action<T>)` → 내부에서 `key.ToString()` 호출
- 키 인덱싱: `Addressables.LoadResourceLocationsAsync(label)` 결과에서
  `pathInfo.ToString().Split('/').Last().Split('.').First()` 로 파일명만 추출해 dict에 저장
- 즉 **에셋 파일명(확장자 제외)이 키**

## 규칙
1. **대소문자 정확히 일치** — `EUI.BattleHud` ↔ `BattleHud.prefab` (`BattleHUD`/`battlehud` 모두 X)
2. **Enum은 카테고리별로 분리** — `EUI`, `EMonster`, `EHero`, `EStats` 등. 한 Enum에 섞지 않음.
3. **Enum은 ChvjPackage가 아닌 게임 코드의 공용 파일 [`CommonEnum.cs`](09-common-enum-single-file.md) 에 정의** — Rule 09 참조
4. **Addressables 주소 = 파일명** 으로 등록 (Use Asset Address as Address X — 파일명만 사용)
5. **변경 시 Enum과 파일명을 동시에** 갱신 (둘 중 하나만 바뀌면 런타임에 Load 실패하고 경고 로그)
6. **씬 이름** 도 동일 정책 — Enum으로 추상화하는 경우 씬 파일명과 Enum 값명 일치

## Enum 정의 예시
```csharp
namespace Lair.Data
{
    //# CHMUI / CHMResource 에서 UI 프리팹 로드 키
    public enum EUI
    {
        BattleHud,      //# Addressables: BattleHud.prefab
        ResultPopup,    //# Addressables: ResultPopup.prefab
    }

    //# CHMResource 에서 캐릭터 프리팹 로드 키
    public enum EMonster
    {
        Slime,
        Golem,
        Orc,
    }

    //# CHMResource 에서 ScriptableObject Stats 로드 키
    public enum EStats
    {
        HeroStats,
        SlimeStats,
        GolemStats,
        OrcStats,
    }
}
```

## 에셋 명명 체크리스트
- [ ] 프리팹 파일명 = Enum 값명 (대소문자 포함)
- [ ] Addressables 주소 = 파일명 (그룹 설정에서 "Simplify Addressable Names" 가 아닌 파일명 직사용)
- [ ] Addressables 라벨 = `CHMResource.Init(label)` 에 전달한 라벨과 일치 (기본 `"Resource"`)
- [ ] 같은 Enum 값에 두 개 이상 에셋이 매핑되지 않음 (중복 키 경고)

## 금지 예시
```csharp
//# (X) 하드코딩 문자열 키 — Enum 사용
CHMResource.Instance.Load<GameObject>("Slime", cb);

//# (X) Enum 값명과 파일명 불일치
public enum EMonster { Slime }  //# 파일은 slime.prefab → 로드 실패

//# (X) 단일 Enum에 카테고리 혼재
public enum EAsset { BattleHud, Slime, HeroStats }
```

## 권장 예시
```csharp
//# (O) Enum 키 사용
CHMResource.Instance.LoadAsync<GameObject>(EMonster.Slime);
CHMUI.Instance.ShowUI(EUI.BattleHud);
```

## 비고
- ChvjPackage의 `Enum` API가 없는 경로(예: `Instantiate<T>(string key, ...)`)도 가능은 하지만, **본 프로젝트에서는 Enum 오버로드만 사용**한다 (오타 방지 + IDE 자동완성).
- Sprite/Audio 등도 동일 원칙 — 다른 Enum(`EUISprite`, `EBgm`, `ESfx` 등) 으로 분리하고 파일명과 일치시킨다.
