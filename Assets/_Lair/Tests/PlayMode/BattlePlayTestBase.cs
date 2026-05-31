using System.Collections;
using System.Threading.Tasks;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Tests.PlayMode
{
    //# Battle 씬 직접 로드 PlayMode 테스트의 공통 베이스.
    //# 인게임에서는 Loading 씬의 LoadingController 가 CHMResource.Init() / CHMUI.Init() / CHMPool.Init() 수행.
    public abstract class BattlePlayTestBase
    {
        //# 각 [UnityTest] 첫 줄에서 yield return EnsureCHMReady(); 호출.
        //# 첫 호출 시 CHMResource.Init() 완료까지 yield, 이후 호출은 이미 완료된 Task 라 즉시 반환.
        protected IEnumerator EnsureCHMReady()
        {
            Task<bool> initTask = CHMResource.Instance.Init();
            yield return new WaitUntil(() => initTask.IsCompleted);
            CHMUI.Instance.Init();
            CHMPool.Instance.Init();
        }
    }
}
