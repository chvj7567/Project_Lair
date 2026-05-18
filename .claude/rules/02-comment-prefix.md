# Rule 02 — 주석 접두어 `//#`

## 룰
모든 주석은 `//#` 으로 시작한다.

## 적용 범위
- C# 스크립트 단일 라인 주석
- 새로 작성하는 모든 주석에 적용
- 기존 주석은 수정할 때 함께 변환

## 예시
```csharp
//# 싱글톤 초기화 시점 (씬 로드 전 1회)
private void Awake()
{
    //# DontDestroyOnLoad는 Root에서만 호출
    if (transform.parent != null) return;
    DontDestroyOnLoad(gameObject);
}
```

## 금지
```csharp
// 일반 주석 (X)
/* 블록 주석 (X — 가능한 한 사용 금지) */
```

## 비고
- XML doc 주석 `///` 은 예외 (IDE 인텔리센스용)
- `#region` 등 컴파일러 디렉티브는 그대로
