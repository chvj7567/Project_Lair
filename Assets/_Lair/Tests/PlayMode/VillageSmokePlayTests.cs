using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Lair.Meta;
using Lair.Net;
using Lair.UI;
using Lair.Village;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Lair.Tests.PlayMode
{
    //# Village 씬 스모크 — 씬 로드 → VillageController 초기화 → VillageHud 표시 1사이클 (기획서 §8 / plan G3 자동화분).
    //# 프로필은 MetaProfilePlayModeIsolation 이 주입한 임시 스토어 — 실제 persistentDataPath 비접촉.
    public class VillageSmokePlayTests : BattlePlayTestBase
    {
        //# 마을 진입은 MetaSession.EnsureNetworkAsync 를 await 한다(VillageController.cs:46).
        //# Api 를 미리 채워 두면 조기 반환하므로 Firebase 초기화가 돌지 않는다 — 스모크를 네트워크에서 격리.
        [SetUp]
        public void 클라우드_격리()
        {
            MetaSession.Api = new PlayModeStubApiClient();
        }

        [TearDown]
        public void 클라우드_격리_해제()
        {
            MetaSession.Api = null;
            MetaSession.Cloud = null;
            MetaSession.Ranking = null;
        }

        [TearDown]
        public void 정리()
        {
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator Village씬_로드시_컨트롤러가_초기화되고_허브HUD가_표시된다()
        {
            yield return EnsureCHMReady();

            //# 격리 가드 확인 — SetUpFixture 가 임시 스토어를 주입한 상태여야 실프로필 오염이 없다.
            Assert.IsNotNull(MetaSession.Store, "MetaProfilePlayModeIsolation 이 임시 스토어를 주입해야 함");

            yield return SceneManager.LoadSceneAsync("Village");
            yield return null;

            //# VillageController 존재 대기 (씬 활성화 직후 프레임 흔들림 흡수).
            VillageController controller = null;
            float waitInit = 0f;
            while (waitInit < 5f)
            {
                controller = Object.FindFirstObjectByType<VillageController>();
                if (controller != null)
                    break;
                waitInit += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(controller, "VillageController 가 Village 씬에 있어야 함");

            //# Start 의 비동기 체인(프로필 로드 → 해골 배치 → VillageHud ShowUIAsync) 완료 대기.
            VillageHud hud = null;
            float waitHud = 0f;
            while (waitHud < 10f)
            {
                hud = Object.FindFirstObjectByType<VillageHud>();
                if (hud != null && hud.gameObject.activeInHierarchy)
                    break;
                waitHud += Time.unscaledDeltaTime;
                yield return null;
            }
            Assert.IsNotNull(hud, "VillageHud 가 표시돼야 함 (프리팹 로드/캔버스 확보)");
            Assert.IsTrue(hud.gameObject.activeInHierarchy, "VillageHud 가 활성 상태여야 함");

            //# 프로필이 세션에 로드됐는지 — 출격/상점 동선의 전제.
            Assert.IsNotNull(MetaSession.Profile, "Village 진입 후 MetaSession.Profile 이 로드돼야 함");

            yield return null;
        }
    }

    //# PlayMode 스모크 전용 — 모든 op 가 즉시 "아무것도 없음" 을 반환한다(네트워크 미사용).
    public class PlayModeStubApiClient : ILairApiClient
    {
        public Task<bool> AuthenticateAsync() => Task.FromResult(true);
        public Task<SaveResponseBody> GetSaveAsync() => Task.FromResult<SaveResponseBody>(null);
        public Task<CloudSaveResult> PutSaveAsync(MetaProfile profile, string clientUpdatedAt) => Task.FromResult(CloudSaveResult.Success);
        public Task<bool> SubmitScoreAsync(int clearTimeMs, string hero, string displayName) => Task.FromResult(true);
        public Task<List<RankingRowDto>> GetTopAsync(int top) => Task.FromResult(new List<RankingRowDto>());
        public Task<List<RankingRowDto>> GetMyRankAsync() => Task.FromResult(new List<RankingRowDto>());
        public Task<DisplayNameResult> ChangeDisplayNameAsync(string displayName) => Task.FromResult(new DisplayNameResult(DisplayNameStatus.Success, displayName));
    }
}
