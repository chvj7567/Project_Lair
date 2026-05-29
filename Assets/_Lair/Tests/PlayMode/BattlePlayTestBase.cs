using System.Collections;
using System.Threading.Tasks;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.Tests.PlayMode
{
    //# Battle 씬 직접 로드 PlayMode 테스트의 공통 베이스.
    //# 인게임에서는 Loading 씬의 LoadingController 가 CHMResource.Init() / CHMUI.Init() / CHMPool.Init() 수행.
    //# PlayMode 테스트는 Loading 씬 건너뛰고 Battle 씬을 직접 LoadSceneAsync 하므로 init 누락 → Knight 프리팹 로드 실패.
    //# 본 베이스가 그 init 책임을 대신함. CHMResource.Init() 는 _initTask 캐싱으로 idempotent (동시·재호출 안전).
    //# CHMUI.Init() / CHMPool.Init() 도 패키지 측 가드로 idempotent.
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
