# ChvjUnityInfra Demos

각 매니저별 사용 패턴 예시. 빈 GameObject 만들고 해당 스크립트를 컴포넌트로 붙여서 Play 실행하면 동작.

## 데모 목록

| 파일 | 매니저 | 외부 셋업 필요 |
|---|---|---|
| `CoreDemo.cs` | Singleton, CHUtil, JsonArrayUtility | 없음 — 바로 동작 |
| `ResourceDemo.cs` | CHMResource | Addressables 라벨 "Resource"가 붙은 에셋 |
| `PoolDemo.cs` | CHMPool, CHPoolable | 프리팹 1개 |
| `AudioDemo.cs` | CHMSound | AudioClip 에셋 (Click, BGM 등) |
| `UIDemo.cs` + `DemoUI.cs` | CHMUI, UIBase, CHButton, CHText | UI 프리팹 1개 |
| `AdsDemo.cs` | CHMAdmob | Use Admob 토글 + AdConfig |
| `IAPDemo.cs` | CHMIAP | Use IAP 토글 + IAPProductConfig |
| `SocialDemo.cs` | CHMGPGS | Use GPGS 토글 + Android 빌드 |

## 통합 초기화 데모

`BootstrapDemo.cs`에서 `ChvjUnityInfraSDK.Initialize`를 사용한 전체 셋업 흐름 확인 가능.

## 옵트인 모듈

Ads/IAP/Social 데모는 `Tools/ChvjUnityInfra/Settings`에서 해당 토글을 켜야 컴파일됨.
