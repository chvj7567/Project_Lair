using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChvjUnityInfra
{
    /// <summary>
    /// UI 매니저 — UI 캐싱/재사용/ESC 자동닫기.
    /// 키는 Enum 베이스로 받아 게임별 EUI에 의존하지 않음.
    /// </summary>
    public class CHMUI : CHSingleton<CHMUI>
    {
        private const string UICanvasTag = "UICanvas";
        private const string UICanvasKey = "UICanvas";
        private bool _initialize = false;
        private bool _canvasRequestInFlight = false;
        private Transform _rootTransform;

        // 현재 활성 UI
        private Dictionary<Enum, UIBase> _dicCurrentUI = new Dictionary<Enum, UIBase>();
        // 비동기 로딩 중 닫기 요청이 들어온 UI 키
        private HashSet<Enum> _dicWaitCloseUI = new HashSet<Enum>();
        // 인스턴스화한 UI 캐시 (재사용용)
        private Dictionary<Enum, UIBase> _dicCashingUI = new Dictionary<Enum, UIBase>();

        public bool CheckUI => _dicCurrentUI.Count > 0;

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (_dicCurrentUI.Count > 0)
                {
                    CloseUI(_dicCurrentUI.Last().Value);
                }
            }
        }

        public void Init()
        {
            if (_initialize)
                return;

            SceneManager.sceneLoaded += OnSceneLoaded;
            _initialize = true;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // 새 씬으로 전환되면 기존 캔버스/인스턴스 캐시는 무효. 다음 ShowUI 호출 시 새 캔버스를 찾음.
            _rootTransform = null;
            _dicCurrentUI.Clear();
            _dicCashingUI.Clear();
            _dicWaitCloseUI.Clear();
        }

        // 패키지 사용자가 미리 UICanvas 인스턴스를 주입할 수 있는 hook. null이면 ShowUI가 직접 instantiate.
        public void SetRoot(Transform root)
        {
            _rootTransform = root;
        }

        // 비동기로 UI 전용 캔버스 확보:
        // 1) 이미 _rootTransform 있으면 즉시 콜백
        // 2) 씬에 UICanvas 태그된 GameObject가 있으면 그것을 사용
        // 3) 둘 다 없으면 UICanvas 프리팹을 Addressables로 instantiate
        private void EnsureRootAsync(Action<Transform> onReady)
        {
            if (_rootTransform != null)
            {
                onReady?.Invoke(_rootTransform);
                return;
            }

            var existing = GameObject.FindGameObjectWithTag(UICanvasTag);
            if (existing != null)
            {
                _rootTransform = existing.transform;
                onReady?.Invoke(_rootTransform);
                return;
            }

            if (_canvasRequestInFlight)
            {
                // 동시 ShowUI 호출 race 처리 — 한 프레임 뒤 재시도
                StartCoroutine(WaitAndRetry(onReady));
                return;
            }

            _canvasRequestInFlight = true;
            CHMResource.Instance.Instantiate<GameObject>(UICanvasKey, canvas =>
            {
                _canvasRequestInFlight = false;
                if (canvas == null)
                {
                    Debug.LogWarning($"[CHMUI] '{UICanvasKey}' 프리팹을 로드할 수 없습니다.");
                    onReady?.Invoke(null);
                    return;
                }
                _rootTransform = canvas.transform;
                onReady?.Invoke(_rootTransform);
            });
        }

        private System.Collections.IEnumerator WaitAndRetry(Action<Transform> onReady)
        {
            while (_canvasRequestInFlight)
                yield return null;
            EnsureRootAsync(onReady);
        }

        public void ShowUI(Enum uiType, UIArg arg = null, Action<UIBase> callback = null)
        {
            if (arg == null)
                arg = new UIArg();

            EnsureRootAsync(root =>
            {
                if (root == null)
                    return;
                ShowUIInternal(root, uiType, arg, callback);
            });
        }

        /// <summary>ShowUI의 async/await 버전. 캔버스 확보·인스턴스 로드 실패 시 결과는 null.</summary>
        public Task<UIBase> ShowUIAsync(Enum uiType, UIArg arg = null)
        {
            var tcs = new TaskCompletionSource<UIBase>();
            ShowUI(uiType, arg, ui => tcs.TrySetResult(ui));
            return tcs.Task;
        }

        private void ShowUIInternal(Transform root, Enum uiType, UIArg arg, Action<UIBase> callback)
        {
            // 캐시된 UI가 있다면 재사용
            if (_dicCashingUI.TryGetValue(uiType, out var cached))
            {
                ActivateUI(cached, uiType, arg, callback);
            }
            else
            {
                // 비동기 로드 — 콜백에서 _dicWaitCloseUI 검사 (race 처리)
                CHMResource.Instance.Instantiate<GameObject>(uiType, (ui) =>
                {
                    if (ui == null)
                        return;
                    // 콜백이 씬 전환 후에 도착한 경우 — root가 무효화됐으니 인스턴스 폐기.
                    // (prefab이 활성 상태로 저장된 경우 InitUI 없이 Start가 호출돼 NullRef 위험.)
                    if (_rootTransform == null)
                    {
                        UnityEngine.Object.Destroy(ui);
                        return;
                    }
                    ui.transform.SetParent(_rootTransform, false);
                    var rectTransform = ui.GetComponent<RectTransform>();
                    // Stretch 설정
                    rectTransform.anchorMin = new Vector2(0, 0);
                    rectTransform.anchorMax = new Vector2(1, 1);

                    // Offset 0으로 설정
                    rectTransform.offsetMin = Vector2.zero;
                    rectTransform.offsetMax = Vector2.zero;

                    // 위치/스케일 초기화
                    rectTransform.anchoredPosition3D = Vector3.zero;
                    rectTransform.localScale = Vector3.one;

                    UIBase newUI = ui.GetComponent<UIBase>();
                    if (newUI == null)
                    {
                        UnityEngine.Object.Destroy(ui);
                        return;
                    }

                    _dicCashingUI[uiType] = newUI;

                    // 로딩 중에 CloseUI 요청이 들어왔다면 즉시 닫음
                    if (_dicWaitCloseUI.Contains(uiType))
                    {
                        _dicWaitCloseUI.Remove(uiType);
                        newUI.gameObject.SetActive(false);
                        return;
                    }

                    ActivateUI(newUI, uiType, arg, callback);
                });
            }
        }

        private void ActivateUI(UIBase uiBase, Enum uiType, UIArg arg, Action<UIBase> callback)
        {
            uiBase.Init(uiType);
            uiBase.InitUI(arg);
            uiBase.gameObject.SetActive(true);
            uiBase.transform.SetAsLastSibling();

            _dicCurrentUI[uiType] = uiBase;

            callback?.Invoke(uiBase);
        }

        public void CloseUI(UIBase uiBase, bool reuse = false)
        {
            if (uiBase == null)
                return;

            CloseUI(uiBase.UIType, reuse);
        }

        public void CloseUI(Enum uiType, bool reuse = false)
        {
            // 활성 UI에 있다면 즉시 닫음
            if (_dicCurrentUI.TryGetValue(uiType, out var uiBase))
            {
                uiBase.gameObject.SetActive(false);
                _dicCurrentUI.Remove(uiType);
            }
            else
            {
                // 아직 로딩 중이라면 닫기 대기열에 추가 (ShowUI 콜백에서 처리)
                _dicWaitCloseUI.Add(uiType);
            }

            if (reuse == false)
            {
                RemoveCashingUI(uiType);
            }
        }

        private void RemoveCashingUI(Enum uiType)
        {
            if (_dicCashingUI.TryGetValue(uiType, out var uiBase))
            {
                _dicCashingUI.Remove(uiType);
                if (uiBase != null)
                {
                    Destroy(uiBase.gameObject);
                }
            }
        }
    }
}
