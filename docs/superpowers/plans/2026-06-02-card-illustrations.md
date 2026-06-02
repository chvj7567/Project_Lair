# 카드 일러스트 표시 Implementation Plan

> **For agentic workers:** 이 플랜은 Project Lair 의 start-develop 파이프라인(game-designer → design-reviewer → 승인 → gameplay-programmer → code-reviewer → test-engineer)으로 실행된다. gameplay-programmer 가 Task 단위로 구현하고, game-designer 가 정한 도메인 수치(아트:텍스트 비율 등)를 반영한다. 스텝은 `- [ ]` 체크박스로 추적.
>
> **프로젝트 룰 주의 (Rule 01):** `git commit` 직접 실행 금지. 각 Task 의 "Commit" 스텝은 **`git add`(스테이징) + 한글 커밋 메시지(안) 제시**로 해석한다. 자동 커밋하지 않는다.

**Goal:** 외부 생성 카드 일러스트(3:4) 28장을 3택1 카드 선택 팝업의 `CardView`(상단 아트 + 하단 텍스트)에 표시한다.

**Architecture:** 이미 동작하는 아이콘 파이프라인(`_icon` + `Art/Sprites/CardIcons/{ID}.png` + `LairCardPrefabBuilder` 자동 배정)을 그대로 미러링한다. `CardData` 에 `_cardImage` 필드를 추가하고, 빌더가 `Art/Sprites/CardArt/{ECardId}.png` 를 자동 배정하며, `CardView` 가 상단에 표시한다. 아이콘 파이프라인·빌드 모달 셀은 불변.

**Tech Stack:** Unity 6 / C# / Unity Test Framework(NUnit, EditMode) / Addressables / ChvjPackage(CHText·CHButton).

---

## File Structure

| 파일 | 책임 | 변경 |
|---|---|---|
| `Assets/_Lair/Scripts/Card/CardData.cs` | 카드 데이터 — `_cardImage` 필드 추가 | Modify |
| `Assets/_Lair/Art/Sprites/CardArt/{ECardId}.png` ×28 | 일러스트 에셋 | Create |
| `Assets/_Lair/Editor/LairCardPrefabBuilder.cs` | 빌더 — `LoadCardImage` + `_cardImage` 배정 | Modify |
| `Assets/_Lair/Scripts/UI/CardView.cs` | 3택1 카드 뷰 — `_artImage` 표시 + null 폴백 | Modify |
| `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` | CardView 프리팹 — 아트 영역 추가, 텍스트 재배치 | Modify |
| `Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs` | 신규 테스트 스위트 | Create (test-engineer) |

> `CardSelectionPopup.cs` 는 변경 없음(슬롯 Bind 그대로). `BuildModalCardCell` / `BuildIconCell` / `_icon` / `CardIcons/` 는 불변.

---

## Task 1: CardData 에 `_cardImage` 필드 추가

**Files:**
- Modify: `Assets/_Lair/Scripts/Card/CardData.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs`

- [ ] **Step 1: 실패 테스트 작성** — `CardData` 가 `Sprite CardImage` 게터를 노출하는지 리플렉션 확인

```csharp
using System.Reflection;
using Lair.Card;
using NUnit.Framework;
using UnityEngine;

namespace Lair.Tests.Card
{
    public class CardIllustrationTests
    {
        [Test]
        public void CardData_CardImage_게터_존재_타입_Sprite()
        {
            PropertyInfo p = typeof(CardData).GetProperty("CardImage");
            Assert.IsNotNull(p, "CardData.CardImage 게터 없음");
            Assert.AreEqual(typeof(Sprite), p.PropertyType, "CardImage 타입은 Sprite");
        }
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — Unity Test Runner(EditMode) 또는 UnityMCP 로 실행. 기대: `CardData.CardImage 게터 없음` FAIL.

- [ ] **Step 3: 최소 구현** — `CardData.cs` 에 `_icon` 바로 아래 필드·게터 추가

```csharp
//# 빌드 패널 아이콘 — LairCardPrefabBuilder 가 ECardId 이름 PNG 로 배정. 없으면 null.
[SerializeField] private Sprite _icon;
//# 3택1 팝업 카드 일러스트(3:4) — LairCardPrefabBuilder 가 CardArt/{ECardId}.png 로 배정. 없으면 null.
[SerializeField] private Sprite _cardImage;
```

그리고 게터 영역(`public Sprite Icon => _icon;` 아래):

```csharp
public Sprite Icon => _icon;
public Sprite CardImage => _cardImage;
```

- [ ] **Step 4: 테스트 통과 확인** — EditMode 재실행. 기대: PASS.

- [ ] **Step 5: Commit (Rule 01 — stage + 메시지안)**

```
git add Assets/_Lair/Scripts/Card/CardData.cs Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs.meta
```
커밋 메시지(안): `# [feat] - CardData 에 카드 일러스트 _cardImage 필드 추가`

---

## Task 2: 일러스트 28장 가져오기 (`Art/Sprites/CardArt/`)

**Files:**
- Create: `Assets/_Lair/Art/Sprites/CardArt/{ECardId}.png` ×28
- Test: `Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs` (추가)

- [ ] **Step 1: 실패 테스트 작성** — `CardArt/*.png` 28장이 `ECardId` 28개와 정확히 일치하는지

```csharp
using System.IO;
using System.Linq;
using Lair.Data;
// (위 using 에 더해)

[Test]
public void CardArt_PNG_28장_ECardId_이름_정합()
{
    string dir = "Assets/_Lair/Art/Sprites/CardArt";
    Assert.IsTrue(Directory.Exists(dir), $"CardArt 폴더 부재: {dir}");

    var pngStems = Directory.GetFiles(dir, "*.png")
        .Select(Path.GetFileNameWithoutExtension)
        .OrderBy(s => s).ToArray();
    var ids = System.Enum.GetNames(typeof(ECardId)).OrderBy(s => s).ToArray();

    Assert.AreEqual(28, pngStems.Length, "CardArt PNG 28장");
    CollectionAssert.AreEqual(ids, pngStems, "CardArt 파일명 = ECardId 28개 정확 일치(대소문자 포함)");
}
```

- [ ] **Step 2: 테스트 실패 확인** — 기대: 폴더 부재 FAIL.

- [ ] **Step 3: 일러스트 복사** — Bash 로 28장을 복사·리네임 (원본 보존). Windows 경로 주의. 파일명 패턴 `<축접두어>_<ID>_card.png` → `<ID>.png`. (아이콘 import 선례와 동일 방식.)

```bash
SRC="/c/Users/GVNC/Downloads/Project_Lair/png/cards"
DST="Assets/_Lair/Art/Sprites/CardArt"
mkdir -p "$DST"
for f in "$SRC"/*_card.png; do
  base=$(basename "$f" _card.png)        # 예: T1_WispHpBoost
  id="${base#*_}"                         # 접두어 Tn_/Dn_/Bn_/Sn_ 제거 → WispHpBoost
  cp "$f" "$DST/$id.png"
done
ls "$DST"/*.png | wc -l                   # 28 기대
```

> 검증: `ECardId` 28개 ↔ 생성된 `{ID}.png` 28개 대소문자까지 일치 확인. T7 은 `Berserk.png`. `.meta` 는 Unity 가 생성하게 둔다(직접 만들지 않음).

- [ ] **Step 4: 에셋 인식 + 테스트 통과 확인** — UnityMCP `editor_refresh_assets` 후 EditMode 재실행. 기대: PASS.

- [ ] **Step 5: Commit (Rule 01)** — 신규 PNG 는 추가(A)이므로 `.png.meta` 동반 스테이징.

```
git add Assets/_Lair/Art/Sprites/CardArt/
```
커밋 메시지(안): `# [asset] - 카드 일러스트 28장 CardArt 로 가져오기 (ECardId 파일명 일치)`

---

## Task 3: 빌더가 `_cardImage` 자동 배정

**Files:**
- Modify: `Assets/_Lair/Editor/LairCardPrefabBuilder.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs` (추가)

- [ ] **Step 1: 실패 테스트 작성** — 빌더 실행 후 28장 모두 `CardImage` non-null

```csharp
using UnityEditor;

[Test]
public void 모든_CardData_28장_CardImage_충전()
{
    string[] guids = AssetDatabase.FindAssets("t:CardData",
        new[] { "Assets/_Lair/Art/Cards/Items" });
    Assert.AreEqual(28, guids.Length, "CardData SO 28장");
    foreach (string guid in guids)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        CardData card = AssetDatabase.LoadAssetAtPath<CardData>(path);
        Assert.IsNotNull(card.CardImage, $"{card.name}.CardImage 미연결");
    }
}
```

- [ ] **Step 2: 테스트 실패 확인** — 기대: `_cardImage` 미배정 → FAIL.

- [ ] **Step 3: 빌더 확장** — `LairCardPrefabBuilder.cs`

(3-1) `IconDir` 상수 아래에 추가:

```csharp
public const string IconDir = "Assets/_Lair/Art/Sprites/CardIcons";
public const string CardArtDir = "Assets/_Lair/Art/Sprites/CardArt";
```

(3-2) `RebuildAllCards` 의 `EnsureDir(IconDir);` 아래에:

```csharp
EnsureDir(IconDir);
EnsureDir(CardArtDir);
```

(3-3) `BuildCardsAndPool` 의 `_icon` 배정 줄 바로 아래에 `_cardImage` 배정 추가:

```csharp
so.FindProperty("_icon").objectReferenceValue = LoadCardIcon(spec.Id);
//# 3택1 팝업 일러스트 — ECardId 이름 PNG 자동 배정 (없으면 null). 매번 재설정.
so.FindProperty("_cardImage").objectReferenceValue = LoadCardImage(spec.Id);
```

(3-4) `LoadCardIcon` 메서드 바로 아래에 미러 메서드 추가:

```csharp
//# ECardId 이름의 일러스트 PNG 를 Sprite 로 로드. 미존재 시 null. import 설정 보정은 LoadCardIcon 과 동일.
private static Sprite LoadCardImage(ECardId id)
{
    string path = $"{CardArtDir}/{id}.png";
    if (File.Exists(path) == false) return null;

    TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
    if (imp != null && (imp.textureType != TextureImporterType.Sprite
                        || imp.spriteImportMode != SpriteImportMode.Single))
    {
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.SaveAndReimport();
    }
    return AssetDatabase.LoadAssetAtPath<Sprite>(path);
}
```

- [ ] **Step 4: 빌더 실행 + 테스트 통과 확인** — UnityMCP `editor_execute_menu` `Lair/Setup/B3 - Rebuild All Cards` 실행 → 로그 에러 0 확인 → EditMode 재실행. 기대: PASS. (비파괴 — `_effect`·`_icon` 보존 확인.)

- [ ] **Step 5: Commit (Rule 01)** — 빌더 .cs + 갱신된 28 .asset + 2 풀 .asset. 수정(M) .asset 의 .meta 는 제외.

```
git add Assets/_Lair/Editor/LairCardPrefabBuilder.cs Assets/_Lair/Art/Cards/Items/*.asset Assets/_Lair/Art/Cards/CardPool_Active.asset Assets/_Lair/Art/Cards/CardPool_Passive.asset
```
커밋 메시지(안): `# [feat] - LairCardPrefabBuilder 가 카드 일러스트 _cardImage 자동 배정`

---

## Task 4: CardView 가 일러스트 표시 (+ null 폴백)

**Files:**
- Modify: `Assets/_Lair/Scripts/UI/CardView.cs`
- Test: `Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs` (추가)

- [ ] **Step 1: 실패 테스트 작성** — `CardImage` 가 null 인 카드를 Bind 하면 아트 영역이 비활성

```csharp
using Lair.UI;

[Test]
public void CardView_Bind_CardImage_null_이면_아트영역_비활성()
{
    GameObject go = new GameObject("CardViewTest");
    CardView cv = go.AddComponent<CardView>();

    GameObject artGo = new GameObject("Art");
    artGo.transform.SetParent(go.transform);
    UnityEngine.UI.Image art = artGo.AddComponent<UnityEngine.UI.Image>();

    //# 최소 의존 필드만 리플렉션 주입 (_artImage). 나머지 텍스트/버튼은 Bind 가 null 가드한다고 가정하지 말고
    //# 실제 CardView 구현에 맞춰 필요한 필드만 세팅 — 본 테스트는 _artImage 폴백만 검증.
    typeof(CardView).GetField("_artImage",
        System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
        .SetValue(cv, art);

    CardData card = ScriptableObject.CreateInstance<CardData>();   //# _cardImage = null
    cv.ApplyArt(card);   //# 아래 Step3 에서 분리한 아트 전용 메서드

    Assert.IsFalse(art.gameObject.activeSelf, "CardImage null 이면 아트 영역 숨김");
    Object.DestroyImmediate(go);
}
```

> 참고: 전체 `Bind` 는 `CHText`/`CHButton` 의존이 있어 EditMode 단위 테스트가 무겁다. 아트 표시 로직을 작은 `ApplyArt(CardData)` 로 분리해 단위 테스트 가능하게 한다(SRP).

- [ ] **Step 2: 테스트 실패 확인** — 기대: `_artImage` 필드/`ApplyArt` 없음 → 컴파일 또는 FAIL.

- [ ] **Step 3: 구현** — `CardView.cs`

(4-1) 필드 추가 (`_border` 아래):

```csharp
[SerializeField] private Image _border;
//# 3택1 팝업 상단 일러스트. CardData.CardImage 가 null 이면 영역을 숨긴다.
[SerializeField] private Image _artImage;
```

(4-2) 아트 전용 메서드 추가:

```csharp
//# 일러스트 적용 — null 이면 아트 영역 비활성(폴백). Bind 에서 호출.
public void ApplyArt(CardData card)
{
    if (_artImage == null)
        return;

    Sprite art = card != null ? card.CardImage : null;
    if (art == null)
    {
        _artImage.gameObject.SetActive(false);
        return;
    }

    _artImage.gameObject.SetActive(true);
    _artImage.sprite = art;
}
```

(4-3) `Bind(CardData, Action, int)` 안에서 호출 (테두리 색 설정 근처):

```csharp
_border.color = CardBorderColors.BorderColorOf(card.Id);
ApplyArt(card);
```

- [ ] **Step 4: 테스트 통과 확인** — EditMode 재실행. 기대: PASS.

- [ ] **Step 5: Commit (Rule 01)**

```
git add Assets/_Lair/Scripts/UI/CardView.cs Assets/_Lair/Tests/EditMode/Card/CardIllustrationTests.cs
```
커밋 메시지(안): `# [feat] - CardView 가 카드 일러스트 표시 (null 폴백 포함)`

---

## Task 5: CardView 프리팹 레이아웃 — 상단 아트 + 하단 텍스트

**Files:**
- Modify: `Assets/_Lair/Editor/LairUIPrefabBuilder.cs` (`BuildCardViewSlot`, 622–706)

> 도메인 수치(아트:텍스트 비율, 마진)는 game-designer 기획서가 단일 진실. 아래는 결정 락 기본값 **아트 상단 60% / 텍스트 하단 40%**. game-designer 가 다른 값을 지정하면 그 값으로 대체.

- [ ] **Step 1: 아트 Image 추가** — `BuildCardViewSlot` 의 `Bg` 생성 블록 다음(NameText 앞)에 삽입. 카드 상단 60% 차지.

```csharp
//# CardArt — 상단 60% 아트 영역 (런타임에 CardView.ApplyArt 가 sprite 설정/표시여부 제어)
GameObject artGo = new GameObject("CardArt", typeof(RectTransform));
artGo.transform.SetParent(slot.transform, false);
RectTransform artRt = (RectTransform)artGo.transform;
artRt.anchorMin = new Vector2(0f, 0.4f);   //# 하단 40% 위 = 상단 60%
artRt.anchorMax = new Vector2(1f, 1f);
artRt.offsetMin = new Vector2(12f, 4f);
artRt.offsetMax = new Vector2(-12f, -12f);
Image artImg = artGo.AddComponent<Image>();
artImg.preserveAspect = true;              //# 3:4 비율 유지
artImg.color = Color.white;
```

- [ ] **Step 2: NameText·DescText 를 하단 40% 로 재배치** — 기존 `nameRt`/`descRt` 앵커 값 교체.

NameText (아트 바로 아래, 텍스트 영역 상단):

```csharp
nameRt.anchorMin = new Vector2(0f, 0.3f);
nameRt.anchorMax = new Vector2(1f, 0.4f);
nameRt.offsetMin = new Vector2(8f, 0f);
nameRt.offsetMax = new Vector2(-8f, 0f);
nameRt.anchoredPosition = Vector2.zero;
nameRt.sizeDelta = new Vector2(nameRt.sizeDelta.x, 0f);
```

DescText (하단 텍스트 영역 본문):

```csharp
descRt.anchorMin = new Vector2(0f, 0f);
descRt.anchorMax = new Vector2(1f, 0.3f);
descRt.offsetMin = new Vector2(16f, 12f);
descRt.offsetMax = new Vector2(-16f, -4f);
```

- [ ] **Step 3: `_artImage` 필드 와이어링** — CardView 컴포넌트 세팅 블록(698–703)에 추가.

```csharp
SetObjectField(cvSo, "_border", borderImg);
SetObjectField(cvSo, "_artImage", artImg);
SetObjectField(cvSo, "_pickButton", chBtn);
```

- [ ] **Step 4: 프리팹 재빌드 + 검증** — UnityMCP `editor_execute_menu` 로 CardSelectionPopup 빌드 메뉴 실행(LairUIPrefabBuilder 진입점). 로그 에러 0 확인. CardSelectionPopup.prefab 의 각 CardView 슬롯에 `_artImage` 가 와이어됐는지 확인.

> 검증 테스트(선택, test-engineer): `CardSelectionPopup.prefab` 로드 → 3개 CardView 의 `_artImage` SerializedProperty 가 non-null.

- [ ] **Step 5: Commit (Rule 01)**

```
git add Assets/_Lair/Editor/LairUIPrefabBuilder.cs Assets/_Lair/Art/UI/CardSelectionPopup.prefab
```
커밋 메시지(안): `# [feat] - CardView 프리팹 상단 아트 + 하단 텍스트 레이아웃`

---

## Self-Review

**1. Spec coverage:**
- spec §3.1 (CardData `_cardImage`) → Task 1 ✅
- spec §3.2 (에셋 파이프라인: 폴더·복사·빌더 `LoadCardImage`·`_cardImage` 배정) → Task 2 + Task 3 ✅
- spec §3.3 (CardView 상단 아트 + 하단 텍스트 + null 폴백, CardSelectionPopup 불변) → Task 4 + Task 5 ✅
- spec §5 성공 기준(28:28 네이밍·28장 충전·비파괴·null 폴백) → Task 2/3/4 테스트 ✅
- spec §2 제외(아이콘·빌드모달셀 불변) → 어느 Task 도 해당 파일 미수정 ✅
- spec §4 (MVP §8 경계) → 코드 아닌 판정. game-designer 기획서가 확정(파이프라인 2단계).

**2. Placeholder scan:** "TBD/TODO" 없음. 모든 코드 스텝에 실제 코드 포함. 아트:텍스트 비율은 결정 락 기본값(60/40) 구체값으로 명시하고 game-designer 가 override 한다고 표기 — 플레이스홀더 아님.

**3. Type consistency:** `_cardImage`(필드)/`CardImage`(게터)/`_artImage`(CardView 필드)/`ApplyArt(CardData)`/`LoadCardImage(ECardId)`/`CardArtDir` — Task 간 명칭 일관.

> **후속:** game-designer 가 아트:텍스트 비율·마진을 확정하면 Task 5 의 앵커 수치를 그 값으로 갱신(plan↔기획서 sync, project.md 규칙).
