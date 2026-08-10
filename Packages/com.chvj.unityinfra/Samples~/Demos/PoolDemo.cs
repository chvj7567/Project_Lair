using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace ChvjUnityInfraDemos
{
    /// <summary>
    /// CHMPool 데모: Stack 기반 GameObject 풀.
    /// 사전 준비: prefab 필드에 CHPoolable 컴포넌트가 붙은 프리팹 할당.
    /// (혹은 데모는 자체 큐브 생성)
    /// </summary>
    public class PoolDemo : MonoBehaviour
    {
        [SerializeField] private GameObject prefab;

        private void Start()
        {
            // prefab이 없으면 자체 큐브 생성 + CHPoolable 부착
            if (prefab == null)
            {
                prefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
                prefab.name = "Cube";
                prefab.AddComponent<CHPoolable>();
                prefab.SetActive(false);
            }

            // 1) Pool 매니저 초기화 — @CHMPool 루트 GameObject 생성됨
            CHMPool.Instance.Init();

            // 2) 풀 미리 생성 (선택, Pop 시 자동 생성도 됨)
            CHMPool.Instance.CreatePool(prefab, count: 5);

            // 3) 데모: 매 0.5초마다 Pop, 2초 후 Push
            StartCoroutine(SpawnLoop());
        }

        private IEnumerator SpawnLoop()
        {
            for (int i = 0; i < 10; i++)
            {
                // Pop — 풀에서 꺼냄 (없으면 자동 생성)
                CHPoolable instance = CHMPool.Instance.Pop(prefab, transform);
                instance.transform.position = new Vector3(i, 0, 0);

                // 2초 후 풀로 반환
                StartCoroutine(ReturnAfter(instance, 2f));

                yield return new WaitForSeconds(0.5f);
            }
        }

        private IEnumerator ReturnAfter(CHPoolable poolable, float delay)
        {
            yield return new WaitForSeconds(delay);
            // Push — 풀로 반환 (비활성화 + Stack에 들어감)
            CHMPool.Instance.Push(poolable);
        }

        private void OnDisable()
        {
            // 풀 전체 정리 (선택)
            CHMPool.Instance.Clear();
        }
    }
}
