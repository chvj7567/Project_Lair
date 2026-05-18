# Rule 07 — ChvjPackage 기준 작업

## 룰
모든 작업은 `com.chvj.unityinfra` 패키지를 **기준 인프라**로 두고 진행한다.

## 위치
`Packages/com.chvj.unityinfra/`

## 모듈
- `Runtime/Core` — 싱글톤, 유틸 확장
- `Runtime/Resource` — Addressables 리소스 로더
- `Runtime/Pool` — GameObject 풀링
- `Runtime/Audio` — 오디오 매니저
- `Runtime/UI` — UI 매니저
- `Runtime/Ads` — 광고
- `Runtime/Iap` — 인앱결제
- `Runtime/Social` — 소셜
- `Editor/` — 에디터 도구
- `Tests/` — 테스트

## 가이드
- **신규 기능 작성 전** 패키지 내 동일/유사 기능 존재 여부 먼저 확인
- 게임 프로젝트 코드(`Assets/`)에서 중복 구현하지 말고 패키지 API 사용
- 패키지에 없는 공통 기능은 **패키지에 추가**한 뒤 사용 (게임 코드에 산재시키지 않음)
- 패키지 변경 시 `Tests/` 갱신 (Rule 03/05/06 동일하게 적용)
- 외부 의존성 추가 시 `package.json` 의 `dependencies` 에 명시
- 버전 변경 시 `version` 필드 업데이트

## 체크리스트 (작업 시작 전)
- [ ] 필요한 기능이 `com.chvj.unityinfra`에 이미 있는가?
- [ ] 없다면, 패키지에 추가하는 것이 적절한가? (재사용성 판단)
- [ ] 패키지 의존성 방향이 올바른가? (게임 → 패키지 ✅ / 패키지 → 게임 ❌)
- [ ] asmdef 참조가 올바르게 설정되어 있는가?

## 패키지 의존성 원칙
```
Assets/ (게임 코드)
   ↓ 참조
Packages/com.chvj.unityinfra (인프라)
   ↓ 참조
Unity 표준 패키지 (Addressables 등)
```
역방향 참조 금지.
