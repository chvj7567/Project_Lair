using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;

namespace ChvjUnityInfra
{
    public class CHMResource : CHSingletonStatic<CHMResource>
    {
        public const string DefaultLabel = "Resource";

        private string _label = DefaultLabel;
        private Task<bool> _initTask;

        private Dictionary<string, IResourceLocation> _dicAssetInfo = new Dictionary<string, IResourceLocation>();
        // 로드된 핸들 보관 — 같은 키 재로드 시 캐시 사용 + 명시적 Unload 가능
        private Dictionary<string, AsyncOperationHandle> _loadedHandles = new Dictionary<string, AsyncOperationHandle>();

        /// <summary>Init에 지정한 라벨. 키 인덱싱·기본 Preload 대상.</summary>
        public string Label => _label;

        /// <summary>
        /// Addressables 초기화 + 라벨 키 인덱싱.
        /// 동시 호출 시 같은 Task를 반환(race 방지). 실패 시 _initTask를 비워 다음 호출에서 retry 가능.
        /// </summary>
        /// <param name="label">키 인덱싱에 사용할 Addressables 라벨. 기본 "Resource".</param>
        public Task<bool> Init(string label = DefaultLabel)
        {
            if (_initTask != null) return _initTask;
            _label = label;
            _initTask = InitInternal();
            return _initTask;
        }

        private async Task<bool> InitInternal()
        {
            TaskCompletionSource<bool> initComplete = new TaskCompletionSource<bool>();

            Addressables.InitializeAsync().Completed += (handle) =>
            {
                bool ok = handle.Status == AsyncOperationStatus.Succeeded;
                initComplete.TrySetResult(ok);
                // InitializeAsync 결과는 Addressables가 자체 lifecycle 관리. release 호출 안 함.
            };

            if (await initComplete.Task == false)
            {
                _initTask = null;
                return false;
            }

            bool saveOk = await SaveLocationInfo();
            if (saveOk == false)
            {
                _initTask = null;
            }
            return saveOk;
        }

        private async Task<bool> SaveLocationInfo()
        {
            TaskCompletionSource<bool> saveComplete = new TaskCompletionSource<bool>();

            Addressables.LoadResourceLocationsAsync(_label).Completed += (handle) =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    saveComplete.TrySetResult(false);
                }
                else
                {
                    foreach (var pathInfo in handle.Result)
                    {
                        string key = pathInfo.ToString().Split('/').Last().Split('.').First();
                        if (_dicAssetInfo.ContainsKey(key) == false)
                        {
                            _dicAssetInfo.Add(key, pathInfo);
                        }
                    }

                    saveComplete.TrySetResult(true);
                }

                Addressables.Release(handle);
            };

            return await saveComplete.Task;
        }

        /// <summary>
        /// 라벨에 속한 Addressables의 번들 의존성을 download/캐시 워밍 + 키 리스트 순회로 진행률 보고.
        /// onProgress(ratio 0~1, currentKey)로 현재 표시할 키 하나를 전달.
        /// 에셋을 메모리에 적재하지는 않음 — 게임 측 Load&lt;T&gt;에서 lazy 로드됨.
        /// 타입 mismatch (PNG가 Texture2D로 캐싱되어 Sprite 로드 시 실패) 회피.
        /// </summary>
        /// <param name="label">대상 라벨. null이면 Init에 지정한 라벨 사용.</param>
        public async Task<bool> PreloadByLabelAsync(string label = null, Action<float, string> onProgress = null)
        {
            if (string.IsNullOrEmpty(label)) label = _label;
            var locHandle = Addressables.LoadResourceLocationsAsync(label);
            await locHandle.Task;

            if (locHandle.Status != AsyncOperationStatus.Succeeded)
            {
                Addressables.Release(locHandle);
                onProgress?.Invoke(1f, string.Empty);
                return false;
            }

            var locations = locHandle.Result;
            int total = locations.Count;
            if (total == 0)
            {
                Addressables.Release(locHandle);
                onProgress?.Invoke(1f, string.Empty);
                return true;
            }

            // 키 표시용 순회 진행률과 번들 다운로드를 병렬 처리.
            // 번들 다운로드는 로컬 빌드에서 거의 즉시 완료되고, 원격에서는 실제 바이트 진행을 반영.
            var downloadHandle = Addressables.DownloadDependenciesAsync(label, false);

            int displayIdx = 0;
            while (!downloadHandle.IsDone)
            {
                float ratio = downloadHandle.PercentComplete;
                string key = locations[displayIdx % total].ToString().Split('/').Last().Split('.').First();
                onProgress?.Invoke(ratio, key);
                displayIdx++;
                await Task.Yield();
            }

            bool success = downloadHandle.Status == AsyncOperationStatus.Succeeded;
            Addressables.Release(downloadHandle);
            Addressables.Release(locHandle);

            onProgress?.Invoke(1f, string.Empty);
            return success;
        }

        /// <summary>키(enum 이름)로 에셋 비동기 로드. 콜백에 결과 전달. 실패/키 없음 시 콜백에 null.</summary>
        public void Load<T>(Enum key, Action<T> callback) where T : UnityEngine.Object
        {
            Load(key.ToString(), callback);
        }

        /// <summary>키(문자열)로 에셋 비동기 로드. 실패/키 없음 시 콜백에 null.</summary>
        public void Load<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            if (_dicAssetInfo.ContainsKey(key) == false)
            {
                Debug.LogWarning($"[CHMResource] Asset key not found: {key}");
                callback?.Invoke(null);
                return;
            }

            // 캐시 사용 — 같은 키로 이미 로드됐고 타입 일치하면 즉시 반환
            if (_loadedHandles.TryGetValue(key, out var cached) && cached.IsValid())
            {
                if (cached.Result is T cachedResult)
                {
                    callback?.Invoke(cachedResult);
                    return;
                }
                // 타입 mismatch — 기존 핸들 release 후 새 타입으로 재로드 (핸들 leak 방지)
                Addressables.Release(cached);
                _loadedHandles.Remove(key);
            }

            // address(string)으로 로드 — IResourceLocation 사용 시 ResourceType과 T가 정확히 일치해야 하지만
            // 문자열 키로 로드하면 Addressables의 sub-asset resolution이 작동
            // (예: Sprite-imported PNG location.ResourceType이 Texture2D여도 LoadAssetAsync<Sprite>가 sprite 반환).
            var handle = Addressables.LoadAssetAsync<T>(key);
            _loadedHandles[key] = handle;
            handle.Completed += h =>
            {
                if (h.Status == AsyncOperationStatus.Succeeded)
                {
                    callback?.Invoke(h.Result);
                }
                else
                {
                    Debug.LogWarning($"[CHMResource] Load failed: {key}");
                    callback?.Invoke(null);
                }
            };
        }

        /// <summary>Load의 async/await 버전. 결과가 null이면 실패.</summary>
        public Task<T> LoadAsync<T>(Enum key) where T : UnityEngine.Object => LoadAsync<T>(key.ToString());

        /// <summary>Load의 async/await 버전. 결과가 null이면 실패.</summary>
        public Task<T> LoadAsync<T>(string key) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<T>();
            Load<T>(key, result => tcs.TrySetResult(result));
            return tcs.Task;
        }

        /// <summary>키로 에셋 로드 후 Instantiate. 실패/키 없음 시 콜백에 null.</summary>
        public void Instantiate<T>(Enum key, Action<T> callback) where T : UnityEngine.Object
        {
            Instantiate(key.ToString(), callback);
        }

        /// <summary>키로 에셋 로드 후 Instantiate. 실패/키 없음 시 콜백에 null.</summary>
        public void Instantiate<T>(string key, Action<T> callback) where T : UnityEngine.Object
        {
            Load<T>(key, (resource) =>
            {
                if (resource == null)
                {
                    callback?.Invoke(null);
                    return;
                }

                callback?.Invoke(UnityEngine.Object.Instantiate(resource));
            });
        }

        /// <summary>Instantiate의 async/await 버전.</summary>
        public Task<T> InstantiateAsync<T>(Enum key) where T : UnityEngine.Object => InstantiateAsync<T>(key.ToString());

        /// <summary>Instantiate의 async/await 버전.</summary>
        public Task<T> InstantiateAsync<T>(string key) where T : UnityEngine.Object
        {
            var tcs = new TaskCompletionSource<T>();
            Instantiate<T>(key, result => tcs.TrySetResult(result));
            return tcs.Task;
        }

        /// <summary>
        /// 특정 키에 대해 로드된 Addressables 핸들을 release. 같은 키 다음 Load 시 다시 로드됨.
        /// </summary>
        public void Unload(Enum key) => Unload(key.ToString());

        public void Unload(string key)
        {
            if (_loadedHandles.TryGetValue(key, out var handle))
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
                _loadedHandles.Remove(key);
            }
        }

        /// <summary>
        /// 로드된 모든 Addressables 핸들 release. Scene 전환 시 메모리 회수 등에 사용.
        /// </summary>
        public void UnloadAll()
        {
            foreach (var handle in _loadedHandles.Values)
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
            _loadedHandles.Clear();
        }
    }
}
