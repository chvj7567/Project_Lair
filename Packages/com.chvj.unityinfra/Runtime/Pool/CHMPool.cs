using System.Collections.Generic;
using UnityEngine;

namespace ChvjUnityInfra
{
    /// <summary>
    /// 프리팹별 GameObject 풀 매니저. 키는 원본 prefab의 이름.
    /// <see cref="Init"/>로 루트 컨테이너를 만든 뒤, <see cref="CreatePool"/>로 사전 워밍 또는
    /// 첫 <see cref="Pop"/>에서 lazy 생성한다. 반환은 <see cref="Push"/>.
    /// </summary>
    public class CHMPool : CHSingletonStatic<CHMPool>
    {
        private class CHPool
        {
            public GameObject Original { get; private set; }
            public Transform Root { get; set; }
            public string Key { get; private set; }

            private Stack<CHPoolable> _stPool = new Stack<CHPoolable>();

            public void Init(GameObject original, int count = 5)
            {
                Original = original;
                Key = original.name;
                Root = new GameObject($"{original.name}Root").transform;

                for (int i = 0; i < count; ++i)
                {
                    Push(Create());
                }
            }

            private CHPoolable Create()
            {
                GameObject go = Object.Instantiate<GameObject>(Original);
                go.name = Original.name;
                var poolable = go.GetOrAddComponent<CHPoolable>();
                poolable.PoolKey = Key;
                return poolable;
            }

            public void Push(CHPoolable poolable)
            {
                if (poolable == null) return;

                poolable.transform.SetParent(Root, false);
                poolable.IsUse = false;
                poolable.gameObject.SetActive(false);

                _stPool.Push(poolable);
            }

            public CHPoolable Pop(Transform parent)
            {
                // 씬 전환 race 방어: parent가 fake-null(이미 Destroy 진행 중)이면 Pop 자체를 건너뜀.
                // 그대로 SetParent 호출 시 "Cannot set the parent ... while its new parent is being destroyed" 에러 발생.
                if (parent != null && parent.gameObject == null) return null;

                CHPoolable poolable;

                if (_stPool.Count > 0)
                {
                    poolable = _stPool.Pop();
                }
                else
                {
                    poolable = Create();
                }

                if (poolable == null) return null;

                poolable.transform.SetParent(parent, false);
                poolable.IsUse = true;
                poolable.gameObject.SetActive(true);

                return poolable;
            }

            public int PooledCount => _stPool.Count;
        }

        private Dictionary<string, CHPool> _poolDic = new Dictionary<string, CHPool>();
        private GameObject _rootObject;

        /// <summary>풀 루트 컨테이너 생성. DontDestroyOnLoad. 중복 호출 무해.</summary>
        public void Init()
        {
            if (_rootObject != null) return;

            _rootObject = GameObject.Find("@CHMPool");
            if (_rootObject == null)
            {
                _rootObject = new GameObject { name = "@CHMPool" };
            }

            Object.DontDestroyOnLoad(_rootObject);
        }

        /// <summary>지정 prefab의 풀을 사전 워밍. 같은 키로 두 번 호출하면 무시(경고).</summary>
        public void CreatePool(GameObject original, int count = 5)
        {
            if (original == null) return;
            if (_rootObject == null) Init();

            if (_poolDic.ContainsKey(original.name))
            {
                Debug.LogWarning($"[CHMPool] Pool already exists for '{original.name}', ignoring duplicate CreatePool.");
                return;
            }

            CHPool pool = new CHPool();
            pool.Init(original, count);
            pool.Root.parent = _rootObject.transform;

            _poolDic.Add(original.name, pool);
        }

        /// <summary>
        /// 인스턴스를 풀로 반환. <see cref="CHPoolable.PoolKey"/>로 소속 풀 식별 → GameObject 이름이 바뀌어도 안전.
        /// 소속 풀이 없으면 파괴.
        /// </summary>
        public void Push(CHPoolable poolable)
        {
            if (poolable == null) return;

            string key = poolable.PoolKey;
            // PoolKey가 비어있으면 호환을 위해 gameObject 이름으로 fallback
            if (string.IsNullOrEmpty(key)) key = poolable.gameObject.name;

            if (_poolDic.ContainsKey(key) == false)
            {
                Object.Destroy(poolable.gameObject);
                return;
            }

            _poolDic[key].Push(poolable);
        }

        /// <summary>풀에서 인스턴스를 꺼낸다. 풀이 없으면 자동 생성(기본 count로 워밍).
        /// parent가 fake-null(이미 Destroy 진행 중)이면 null 반환 — 호출자가 안전하게 fallback.</summary>
        public CHPoolable Pop(GameObject original, Transform parent = null)
        {
            if (original == null) return null;
            // 씬 전환 race 방어: parent의 gameObject가 destroy 중이면 즉시 null 반환.
            if (parent != null && parent.gameObject == null) return null;
            if (_poolDic.ContainsKey(original.name) == false)
            {
                CreatePool(original);
            }

            return _poolDic[original.name].Pop(parent);
        }

        /// <summary>풀 키로 원본 prefab 조회. 없으면 null.</summary>
        public GameObject GetOriginal(string name)
        {
            if (_poolDic.ContainsKey(name) == false)
                return null;
            return _poolDic[name].Original;
        }

        /// <summary>해당 풀에 비축된(idle) 개수. 없는 풀이면 0.</summary>
        public int GetPooledCount(string name)
        {
            return _poolDic.TryGetValue(name, out var pool) ? pool.PooledCount : 0;
        }

        /// <summary>모든 풀과 인스턴스 파괴. 씬 전환·테스트 reset용.</summary>
        public void Clear()
        {
            if (_rootObject != null)
            {
                foreach (Transform child in _rootObject.transform)
                {
                    Object.Destroy(child.gameObject);
                }
            }

            _poolDic.Clear();
        }
    }
}
