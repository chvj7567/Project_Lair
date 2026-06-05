using System.Collections;
using System.Threading.Tasks;
using ChvjUnityInfra;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Lair.Character;
using Lair.Data;

namespace Lair.Tests.PlayMode
{
    //# HeroSkillFx.SpawnAttached — Fear 스컬 FX 가 영웅 Transform 자식으로 부착돼 따라다니는지 검증.
    //# 실제 FearSkull 프리팹 + CHMPool 의존이라 PlayMode. EditMode 는 CHMResource/CHMPool null 가드로 no-op.
    public class HeroSkillFxAttachPlayTests : BattlePlayTestBase
    {
        private IEnumerator LoadPrefab(EVisual key, System.Action<GameObject> onLoaded)
        {
            Task<GameObject> task = CHMResource.Instance.LoadAsync<GameObject>(key);
            yield return new WaitUntil(() => task.IsCompleted);
            onLoaded(task.Result);
        }

        //# 정상 — 영웅 자식으로 부착되고, 영웅이 이동하면 FX 월드 위치도 따라간다.
        [UnityTest]
        public IEnumerator SpawnAttached_영웅_자식으로_부착돼_따라간다()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.FearSkull, p => prefab = p);
            Assert.IsNotNull(prefab, "FearSkull 프리팹 로드");
            CHMPool.Instance.CreatePool(prefab, 1);

            GameObject hero = new GameObject("hero");
            hero.transform.position = new Vector3(3f, 0f, 5f);

            CHPoolable fx = HeroSkillFx.SpawnAttached(EVisual.FearSkull, hero.transform, new Vector3(0f, 0.5f, 0f), 1f);
            Assert.IsNotNull(fx, "SpawnAttached 핸들 반환");
            Assert.AreSame(hero.transform, fx.transform.parent, "FX 가 영웅의 자식");
            Assert.AreEqual(new Vector3(0f, 0.5f, 0f), fx.transform.localPosition, "로컬 오프셋 적용");

            //# 영웅 이동 → 부모 자식이므로 FX 월드 위치도 같이 이동.
            hero.transform.position = new Vector3(10f, 0f, 5f);
            yield return null;
            Assert.AreEqual(new Vector3(10f, 0.5f, 5f), fx.transform.position, "영웅 따라 이동");

            CHMPool.Instance.Push(fx);
            Object.DestroyImmediate(hero);
        }

        //# 엣지 — 풀 반환(Push) 시 영웅 자식에서 떨어져 풀 루트로 재부모화(영웅과 함께 비활성/누수 안 됨).
        [UnityTest]
        public IEnumerator Push시_영웅에서_떨어져_풀루트로_복귀()
        {
            yield return EnsureCHMReady();

            GameObject prefab = null;
            yield return LoadPrefab(EVisual.FearSkull, p => prefab = p);
            Assert.IsNotNull(prefab);
            CHMPool.Instance.CreatePool(prefab, 1);

            GameObject hero = new GameObject("hero");

            CHPoolable fx = HeroSkillFx.SpawnAttached(EVisual.FearSkull, hero.transform, new Vector3(0f, 0.5f, 0f), 1f);
            Assert.IsNotNull(fx);
            Assert.AreSame(hero.transform, fx.transform.parent, "Push 전 — 영웅 자식");

            CHMPool.Instance.Push(fx);
            Assert.AreNotSame(hero.transform, fx.transform.parent, "Push 후 — 영웅에서 분리(풀 루트로 재부모화)");
            Assert.IsFalse(fx.gameObject.activeSelf, "Push 후 — 비활성(풀 idle)");

            Object.DestroyImmediate(hero);
        }
    }
}
