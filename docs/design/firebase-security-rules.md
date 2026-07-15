# Firestore 보안 규칙 · 데이터 모델 (Spec 1)

- **작성일**: 2026-07-15
- **단계**: Project Lair v0.3 (Firebase 백엔드 피벗)
- **상위 문서**: `docs/design/firebase-backend-pivot.md` · plan `docs/superpowers/plans/2026-07-14-firebase-backend-pivot.md`
- **성격**: 이 레포는 규칙을 **문서로만 보관**한다. 실제 등록은 Firebase 콘솔(Firestore → 규칙)에서 수행한다.

## 데이터 모델

- `saves/{uid}` — `profile`(string, `MetaProfile` JSON) · `schemaVersion`(int) · `updatedAt`(string, 클라이언트 ISO8601 — Firestore `stringValue` 로 저장. 충돌 감지는 문서 `updateTime` precondition 을 쓰므로 이 필드는 표시·참고용)
- `leaderboard/{uid}` — `uid` · `displayName` · `clearTimeMs`(int) · `hero`  (정렬·조회는 `clearTimeMs` 만 사용)
- `displayNames/{name}` — `uid` (표시명 유일성 잠금 문서, 문서ID = 정규화된 표시명)

> 참고: 상위 문서(spec §5·plan·브리프)는 `leaderboard` 에 `createdAt` 을 명시하나 현재 구현(`SubmitScoreAsync`)은 쓰지 않는다 — 랭킹이 안 쓰는 필드라 문서 모델에서 제외했다(YAGNI). 향후 필요 시 추가는 오너 판단.

## Firestore 보안 규칙

```
service cloud.firestore {
  match /databases/{database}/documents {
    match /saves/{uid}        { allow read, write: if request.auth.uid == uid; }
    match /leaderboard/{uid}  { allow read: if true;
                                allow write: if request.auth.uid == uid; }
    match /displayNames/{name}{ allow read: if true;
                                allow create: if request.auth.uid == request.resource.data.uid;
                                allow delete: if request.auth.uid == resource.data.uid; }
  }
}
```

## 알려진 한계

- **랭킹 조작 가능** — `leaderboard/{uid}` 규칙은 "본인 uid 문서에만 쓰기"까지만 강제하고 `clearTimeMs` 값의 정당성은 검증하지 않는다. 클라이언트가 임의 값을 제출할 수 있다(v0.3 범위 밖, 기획서 §2.2).
- **익명 인증은 기기이전 불가** — 재설치·새 기기는 새 uid = 새 계정을 만든다. 옛 세이브·표시명에 접근할 수 없다. 진짜 기기이전은 후속 Spec 2(구글 로그인 계정 연동)에서 해소한다(기획서 §2.1).
- **표시명 재점유 불가** — 재설치한 익명 유저는 새 uid 를 받으므로, 예전에 쓰던 표시명의 `displayNames/{name}` 잠금 문서(옛 uid 소유)를 재점유할 수 없다. 다른 이름을 골라야 한다(기획서 §2.3).
- **장시간 세션 중 토큰 재인증 부재** (v0.3 수용) — `MetaSession.EnsureNetworkAsync` 는 세션당 1회만 인증하고, 개별 데이터 op 에 `401 → refresh → 재시도` 경로가 없다(spec §6 이 명시했으나 v0.3 미구현). idToken TTL 이 1시간이라, **1시간을 넘겨 마을에 머문 세션**에서는 토큰 만료 후 클라우드 op(백업·리더보드 등)가 조용히 실패한다. **데이터 손상은 없다** — 만료 요청은 401 을 받고, `ClassifyCommit` 이 401(≠409, 본문에 `FAILED_PRECONDITION` 없음, 비-2xx)을 `Failed` 로 분류하므로 거짓 Conflict/복원권유가 뜨지 않으며, 다음 앱 실행의 refresh-first 인증이 세션을 복구한다. 데이터 op 401 재시도 도입은 후속(fast-follow) 판단.
