namespace Lair.Net
{
    //# FirebaseSdkApiClient.PutSaveAsync 트랜잭션 안 충돌 판정을 Firebase 타입(DocumentSnapshot·Timestamp) 없이
    //# 순수 함수로 뽑아낸 것 — 게이트(#if UNITY_INFRA_FIREBASE) 밖에 둬서 EditMode 테스트가 도달 가능하게 한다.
    public static class CloudSaveConflict
    {
        //# 3-way 분기: 최초생성 기대 / 레거시(serverVersion 필드 없음) / 정상(버전 비교).
        //# versionsEqual 은 Timestamp 비교(호출부가 원시 bool 로 미리 계산)를 그대로 받는다 — 로직은 원본과 동일.
        public static bool IsConflict(bool expectedHasValue, bool expectedDocExists, bool snapExists, bool snapHasServerVersion, bool versionsEqual)
        {
            if (expectedHasValue)
            {
                //# 기대: 문서가 존재하고 serverVersion 이 캐시와 같다.
                bool sameVersion = snapExists && snapHasServerVersion && versionsEqual;
                return sameVersion == false;
            }
            if (expectedDocExists)
            {
                //# 레거시(REST) 문서 — serverVersion 필드가 없어 baseline 비교 불가. 존재 여부만 확인한다.
                return snapExists == false;
            }
            //# 기대: 최초 생성 — 문서가 없어야 한다.
            return snapExists;
        }

        //# 유효 클리어 시간 판정 — clearTimeMs 는 소요시간(ms)이라 0/음수는 실제 클리어일 수 없다.
        public static bool IsRankedClearTime(long ms) => ms > 0;
    }
}
