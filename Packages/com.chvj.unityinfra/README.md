# UnityInfra

Unity 게임용 인프라 패키지. 싱글톤 베이스, Addressables 리소스 로더, UI 매니저, GameObject 풀, 사운드, 옵트인 광고/IAP/소셜 모듈을 한 패키지에 통합.

핵심 모듈은 **외부 라이브러리 의존 없음** (Unity 기본만). 광고/IAP/소셜은 옵트인 모듈이라 안 쓰는 프로젝트는 해당 의존성 임포트 안 해도 됨.

---

## 설치

### 1. 임베디드 복사 (가장 빠름)
`Packages/com.chvj.unityinfra/` 폴더 자체를 다른 프로젝트의 같은 위치에 복사. Unity가 자동 인식.

### 2. 로컬 경로 참조 (한 머신 동시 개발)
대상 프로젝트의 `Packages/manifest.json`:
```json
{
  "dependencies": {
    "com.chvj.unityinfra": "file:../../이패키지가-있는-프로젝트/Packages/com.chvj.unityinfra"
  }
}
```

### 3. Git URL (정공법, 협업/배포)
```json
{
  "dependencies": {
    "com.chvj.unityinfra": "https://github.com/chvj7567/unityinfra.git#v0.1.0"
  }
}
```

---

## 빠른 시작

### 1) 게임별 enum 정의 (게임 측 코드)
```csharp
public enum EUI    { UIMain, UIShop, UISettings }
public enum EAudio { None, BGM, Click, Coin }    // "None"/"Max"는 skip, BGM은 Init에 명시
public enum EJson  { Items, Stages }
```

### 2) Addressables 라벨 셋업
- 모든 리소스(UI 프리팹, 사운드, JSON, 폰트, 스프라이트 등)에 Addressables 라벨 **"Resource"** 부여
- 파일명은 enum 항목명과 **정확히 일치** (예: `EUI.UIShop` → `UIShop.prefab`)

### 3) 게임 부팅 코드 — `ChvjUnityInfraSDK.Initialize` 한 번 호출
```csharp
using ChvjUnityInfra;

public async Task InitManager()
{
    await ChvjUnityInfraSDK.Initialize(new InfraInitConfig<EAudio>
    {
        ClickSoundHook    = () => CHMSound.Instance.Play(EAudio.Click),
        StringProvider    = new GameStringProvider(),
        FontProvider      = new GameFontProvider(),
        AfterResourceInit = async () =>
        {
            // CHMResource.Init 직후 실행할 게임 측 작업 (JSON 로드, 폰트 로드 등)
            await MyJsonLoader.LoadAllAsync();
            await LoadFontAsync();
        }
    });
}
```

내부에서 처리되는 것 (순서대로):
1. `CHMResource.Init`
2. `config.AfterResourceInit` (게임 측 추가 로드)
3. `CHMUI.Init`, `CHMSound.Init<TAudio>`
4. Hook + Provider 자동 등록 (`CHButton.ClickSoundHook`, `CHToggle.ChangeSoundHook`, `CHText.StringProvider`, `CHText.FontProvider`)
5. 옵트인 모듈 — `Tools/ChvjUnityInfra/Settings`에서 켰을 때만:
   - Use Admob ✓ → `CHMAdmob.Init` (AdConfig 자동 로드)
   - Use IAP ✓ → `CHMIAP.Init` (IAPProductConfig 자동 로드)
   - Use GPGS는 명시 Init 없음 — `CHMGPGS.Login` 시 자동 초기화

모든 `InfraInitConfig` 필드는 nullable — 필요한 것만 채우면 됨.

### 4) Provider 구현체 (CHText의 i18n/폰트 쓰는 경우)
```csharp
public class GameStringProvider : IStringProvider
{
    public string GetString(int stringID) => MyStringTable.Get(stringID);
}

public class GameFontProvider : IFontProvider
{
    public TMP_FontAsset GetFont()          => _font;
    public Material      GetFontMaterial()  => _fontMaterial;
}
```

---

## 모듈 가이드

### Core
- `CHSingletonStatic<T>` — 일반 클래스 싱글톤, `T.Instance`로 접근
- `CHSingleton<T>` — MonoBehaviour 싱글톤, 자동 GameObject 생성
- `CHUtil` — `List<T>.IsNullOrEmpty()`, `GameObject.GetOrAddComponent<T>()`, `FindChild<T>()`
- `CompositeDisposable` — IDisposable 묶음 관리
- `JsonArrayUtility.FromJsonArray<T>(json)` — `JsonUtility`로 최상위 배열 파싱
- `ReadOnlyAttribute` — Inspector ReadOnly 마커

### Resource (Addressables 래퍼)
```csharp
CHMResource.Instance.Load<AudioClip>(EAudio.Click, clip => ...);
CHMResource.Instance.Load<Material>($"{EFont.Main}Material", mat => ...);  // string 오버로드
CHMResource.Instance.Instantiate<GameObject>(EUI.UIShop, ui => ...);
```

### Pool
```csharp
CHMPool.Instance.Init();
CHMPool.Instance.CreatePool(prefab, count: 10);
var instance = CHMPool.Instance.Pop(prefab, parent);
CHMPool.Instance.Push(instance.GetComponent<CHPoolable>());
```

### Audio
```csharp
CHMSound.Instance.Init<EAudio>(EAudio.BGM);  // BGM 키 명시 (params, 0~N개)
CHMSound.Instance.Play(EAudio.BGM);          // loop=true (명시했으므로)
CHMSound.Instance.Play(EAudio.Click);        // PlayOneShot
CHMSound.Instance.SetBGMVolume(0.7f);        // PlayerPrefs에 영구 저장
```

### UI
```csharp
// 표시
CHMUI.Instance.ShowUI(EUI.UIShop, new UIShopArg { ... }, ui => { /* 콜백 */ });

// 닫기 (UIBase 안에서)
Close();

// ESC 키: 가장 최근 UI 자동 닫힘

// UIBase 상속
public class UIShop : UIBase
{
    public override void InitUI(UIArg arg) { ... }
}

// 인자 전달
public class UIShopArg : UIArg { public int category; }
```

**UI 컴포넌트**:
- `CHButton` — 클릭 SFX 자동 (`ClickSoundHook`), `OnClick(callback, disposable)` API
- `CHText` — stringID 기반 i18n + 폰트 자동 (Provider 주입)
- `CHPoolingScrollView<TItem, TData>` — 풀링 스크롤뷰 (서브클래스에서 `InitItem` 구현)
- `CHToggle` — 토글 SFX 자동
- `CHDebugLog` — 게임 내 로그 표시기

### Ads (옵트인 — `UNITY_INFRA_ADS`)
1. **선결**: Google Mobile Ads Unity Plugin 임포트
2. **활성화**: `Tools/ChvjUnityInfra/Settings` → Ads 탭 → `Use Admob` ✓
3. **설정**: 같은 탭에서 "AdConfig 에셋 편집" 클릭 → Inspector에서 광고 ID 입력
4. **호출**:
```csharp
CHMAdmob.Instance.ShowBanner(AdPosition.Bottom);
CHMAdmob.Instance.ShowInterstitialAd();
CHMAdmob.Instance.ShowRewardedAd();
CHMAdmob.Instance.AcquireReward += () => { /* 리워드 지급 */ };
```

**안전장치**:
- 에디터 빌드는 항상 테스트 광고 (UseTestAds 무관)
- 프로덕션 ID 비어있으면 디바이스 빌드에서도 테스트 광고 fallback
- AdConfig 에셋 없으면 경고 + 기본 테스트 광고

### IAP (옵트인 — `UNITY_INFRA_IAP`)
1. **선결**: Window > Package Manager에서 "In-App Purchasing" 패키지 설치
2. **활성화**: Settings → IAP 탭 → `Use IAP` ✓
3. **설정**: "IAPProductConfig 에셋 편집" → Inspector에서 Products 추가
   - productName: 게임 코드 식별자 (예: `"RemoveAD"`)
   - productID: 스토어 등록 ID (예: `"com.yourgame.removead"`)
   - productType: Consumable / NonConsumable / Subscription
4. **호출**:
```csharp
CHMIAP.Instance.purchaseState += result =>
{
    if (result.state == EPurchase.Success) { /* 지급 */ }
};
CHMIAP.Instance.Init();
CHMIAP.Instance.Purchase("RemoveAD");
```

### Social — Google Play Games (옵트인 — `UNITY_INFRA_SOCIAL`, Android only)
1. **선결**: Google Play Games Plugin for Unity 임포트
2. **활성화**: Settings → Social 탭 → `Use GPGS` ✓
3. **호출** (Android only):
```csharp
#if UNITY_INFRA_SOCIAL && UNITY_ANDROID
CHMGPGS.Instance.Login((success, user) => { ... });
CHMGPGS.Instance.SaveCloud("save.dat", json, ok => { ... });
CHMGPGS.Instance.UnlockAchievement("gpgs_id_string");
CHMGPGS.Instance.ReportLeaderboard("leaderboard_id", score);
#endif
```

---

## 에디터 툴: `Tools/ChvjUnityInfra/Settings`

상단 탭 3개(Ads / IAP / Social) + 각 탭에 모듈 토글 + Config 에셋 편집 버튼 + 사용 스텝 가이드.

토글 클릭 시 ProjectSettings의 ScriptingDefineSymbols에 `UNITY_INFRA_*`가 추가/제거되어 옵트인 모듈의 어셈블리 컴파일 여부를 결정.

---

## 외부 의존성

| 모듈 | 외부 라이브러리 | 활성 조건 |
|---|---|---|
| Core/Resource/Pool/Audio/UI | Unity Addressables, TextMeshPro | 항상 |
| Ads | Google Mobile Ads Unity Plugin | `UNITY_INFRA_ADS` |
| IAP | Unity In-App Purchasing (Registry) | `UNITY_INFRA_IAP` |
| Social | Google Play Games Plugin (Android) | `UNITY_INFRA_SOCIAL` |

---

## 주요 컨벤션

- **enum 이름 = Addressables 파일명**: `EUI.UIShop` → `UIShop.prefab`
- **"None"/"Max" 항목**: 자동 skip (AudioSource 안 만듦, 보편적 sentinel 컨벤션)
- **BGM 채널**: `CHMSound.Init<EAudio>(EAudio.MyBGM)`처럼 키 명시 (params, 0~N개). 이름 자유
- **suffix 패턴**: enum 키 + 접미사가 필요하면 string 오버로드 — `Load<Material>($"{EFont.Main}Material", cb)`
- **using 한 줄**: `using ChvjUnityInfra;` — 모든 타입을 단일 namespace에서 접근

---

## 테스트

EditMode 테스트:
- **Test Runner** (Window > General > Test Runner) > EditMode 탭
- `JsonArrayUtilityTests` — `FromJsonArray<T>` 3종 (정상/빈/null)

---

## 자주 하는 실수

- ❌ Addressables 라벨 "Resource" 안 붙임 → `[CHMResource] Asset key not found` 경고
- ❌ enum 이름과 파일명 불일치 → 같은 경고
- ❌ `Init<TAudio>()` 호출 안 함 → `[CHMSound] AudioSource not found` 경고
- ❌ `CHText.StringProvider` 등록 안 함 → stringID 텍스트 비어 보임
- ❌ `Use Admob` 켜놓고 `AdConfig` 미설정 → 경고 + 임시 테스트 광고로 자동 fallback

---

## 라이선스 / 작성

작성: chvj7567
대상: 본인 Unity 게임 프로젝트들의 공통 인프라
