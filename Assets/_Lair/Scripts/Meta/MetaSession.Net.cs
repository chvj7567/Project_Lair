using System.Threading.Tasks;
using Lair.Net;
using UnityEngine;

namespace Lair.Meta
{
    //# 서버 연동 핸들·플래그 — Village 진입 시 1회 구성. null 이면 클라우드 기능 비활성(오프라인).
    public static partial class MetaSession
    {
        public static ILairApiClient Api;
        public static CloudSaveService Cloud;
        public static RankingClient Ranking;

        //# 자동 백업(PUT /save)이 409 충돌을 받으면 set — 클라우드 메뉴 배지 + 진입 시 복원 권유 트리거(기획서 §3).
        public static bool CloudConflictPending;
        //# 충돌 복원 권유를 세션당 1회만 노출하기 위한 가드(기획서 §3).
        public static bool CloudConflictPromptShownThisSession;

        //# 연결됨 여부 — 인증 성공해 Api 가 구성됐는지.
        public static bool IsCloudConnected => Api != null;

        //# 충돌 복원 권유 게이트(기획서 §3) — pending 이고 아직 안 보였으면 true 반환 + 이번 세션 노출 플래그 set.
        //# 세션당 1회만 true(재팝업 폭주 방지). 호출부(OpenCloud)는 결과로 권유 영역 노출 여부 결정.
        public static bool TryConsumeConflictPrompt()
        {
            if (CloudConflictPending == false || CloudConflictPromptShownThisSession)
                return false;
            CloudConflictPromptShownThisSession = true;
            return true;
        }

        //# client/서비스 구성 + 익명 인증 보장. best-effort. 설정은 google-services.json.
        public static async Task EnsureNetworkAsync()
        {
            if (Api != null)
                return;
#if UNITY_INFRA_FIREBASE
            FirebaseSdkApiClient client = new FirebaseSdkApiClient();
            bool authed = await client.AuthenticateAsync();
            if (authed == false)
            {
                Debug.LogWarning("[MetaSession] 익명 인증 실패 — 클라우드 비활성");
                return;
            }
            Api = client;
            Cloud = new CloudSaveService(Api);
            Ranking = new RankingClient(Api);
#else
            Debug.LogWarning("[MetaSession] Firebase 모듈 꺼짐 — 클라우드 비활성");
            await Task.CompletedTask;
#endif
        }
    }
}
