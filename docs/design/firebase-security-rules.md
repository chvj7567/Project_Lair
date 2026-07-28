# Firestore 보안 규칙 · 데이터 모델 (Spec 1)

- **작성일**: 2026-07-15
- **단계**: Project Lair v0.3 (Firebase 백엔드 피벗)
- **상위 문서**: `docs/design/firebase-backend-pivot.md` · plan `docs/superpowers/plans/2026-07-14-firebase-backend-pivot.md`
- **성격**: 이 레포는 규칙을 **문서로만 보관**한다. 실제 등록은 Firebase 콘솔(Firestore → 규칙)에서 수행한다.

## 데이터 모델

- `saves/{uid}` — `profile`(string, `MetaProfile` JSON) · `schemaVersion`(int) · `updatedAt`(string, 클라이언트 ISO8601 — Firestore `stringValue` 로 저장, 표시·참고용) · `serverVersion`(timestamp — 매 `PutSaveAsync` 가 `FieldValue.ServerTimestamp` 로 갱신)
- **충돌 감지 방식 (2026-07-28 SDK 전환)**: 문서 `updateTime` precondition 이 아니라 **`serverVersion` 값을 트랜잭션 안에서 캐시와 비교**하는 방식이다 — Firebase Unity SDK 13.14.0 의 `DocumentSnapshot` 에는 `UpdateTime`(`CreateTime`·`ReadTime` 도)이 없어 precondition 방식 자체가 불가능해졌다. 레거시 문서(REST 시절 작성 — `serverVersion` 없음)는 영구 거짓 충돌을 내므로 3-way 분기(문서 없음 / `serverVersion` 없음=레거시 / 있음=비교)로 구분한다.
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
                                allow update: if request.auth.uid == resource.data.uid
                                              && request.resource.data.uid == request.auth.uid;
                                allow delete: if request.auth.uid == resource.data.uid; }
  }
}
```

`update` 조건이 둘인 이유: 기존 소유자가 나여야 하고(`resource.data.uid`), 바꾼 뒤에도 소유자가 나여야 한다(`request.resource.data.uid`) — 뒤쪽이 없으면 내 표시명 잠금을 남의 uid 로 넘길 수 있다. (2026-07-28 SDK 전환 — 표시명 재점유가 본인 소유면 `transaction.Set` 으로 기존 문서를 갱신하는데, 기존 문서에 대한 `Set` 은 Firestore 규칙상 `create` 가 아니라 `update` 라 이 허용이 없으면 `PERMISSION_DENIED` 로 막힌다.)

**이 규칙 변경은 Firebase 콘솔에도 등록해야 적용된다** — 이 레포는 규칙을 문서로만 보관하며 실제 반영은 콘솔(Firestore → 규칙)에서 별도로 수행해야 한다.

## 알려진 한계

- **랭킹 조작 가능** — `leaderboard/{uid}` 규칙은 "본인 uid 문서에만 쓰기"까지만 강제하고 `clearTimeMs` 값의 정당성은 검증하지 않는다. 클라이언트가 임의 값을 제출할 수 있다(v0.3 범위 밖, 기획서 §2.2).
- **익명 인증은 기기이전 불가** — 재설치·새 기기는 새 uid = 새 계정을 만든다. 옛 세이브·표시명에 접근할 수 없다. 진짜 기기이전은 후속 Spec 2(구글 로그인 계정 연동)에서 해소한다(기획서 §2.1).
- **표시명 재점유 불가** — 재설치한 익명 유저는 새 uid 를 받으므로, 예전에 쓰던 표시명의 `displayNames/{name}` 잠금 문서(옛 uid 소유)를 재점유할 수 없다. 다른 이름을 골라야 한다(기획서 §2.3).
- **~~장시간 세션 중 토큰 재인증 부재~~ (2026-07-28 SDK 전환으로 해소)** — REST 시절에는 세션당 1회만 인증해 idToken TTL 1시간을 넘기면 클라우드 op 가 조용히 실패했다. Firebase Unity SDK 가 토큰 갱신을 자동 처리하므로 이 한계는 사라졌다.
