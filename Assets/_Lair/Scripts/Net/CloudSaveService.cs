using System;
using System.Threading.Tasks;
using Lair.Meta;

namespace Lair.Net
{
    //# 클라우드 세이브 오케스트레이션 — 백업/복원. 충돌(409)은 호출부가 프롬프트로 처리.
    public class CloudSaveService
    {
        private readonly ILairApiClient _api;

        public CloudSaveService(ILairApiClient api)
        {
            _api = api;
        }

        //# 자동 백업 — best-effort. 결과(성공/충돌/실패) 반환.
        public async Task<CloudSaveResult> BackupAsync(MetaProfile profile)
        {
            if (profile == null)
                return CloudSaveResult.Failed;
            string nowIso = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            return await _api.PutSaveAsync(profile, nowIso);
        }

        //# 수동 복원 — 서버 프로필 반환(없거나 실패면 null). 로컬 교체는 호출부 책임.
        public async Task<MetaProfile> RestoreAsync()
        {
            SaveResponseBody res = await _api.GetSaveAsync();
            if (res == null || res.profile == null)
                return null;
            return res.profile;
        }
    }
}
