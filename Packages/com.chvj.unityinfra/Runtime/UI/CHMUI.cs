using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChvjUnityInfra
{
    /// <summary>
    /// UI 매니저 — UI 캐싱/재사용/ESC 자동닫기.
    /// 키는 Enum 베이스로 받아 게임별 EUI에 의존하지 않음.
    /// </summary>
    public class CHMUI : CHSingleton<CHMUI>
    {
        private bool _initialize = false;
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
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                CloseTopUI();
            }
#endif
        }

        /// <summary>
        /// 활성 UI 스택의 최상위 UI 한 개를 닫는다. ESC 핸들러가 호출하는 메서드와 동일.
        /// Input System Only 프로젝트에서는 ESC 자동 처리가 안 되므로
        /// 게임 측 InputAction 콜백에서 직접 호출하면 동일한 동작을 얻을 수 있다.
        /// </summary>
        public void CloseTopUI()
        {
            if (_dicCurrentUI.Count > 0)
            {
                CloseUI(_dicCurrentUI.Last().Value);
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

        // UI 전용 캔버스 확보 (우선순위):
        // 1) SetRoot로 명시 지정된 _rootTransform이 있으면 그대로 사용 (커스텀 캔버스/sortingOrder 등)
        // 2) 씬에 Tag="UICanvas"인 GameObject가 있고 Canvas 컴포넌트를 가지면 그걸 사용
        // 3) 씬에 아무 Canvas나 있으면 그 중 첫 번째를 사용
        // 4) 위 모두 없으면 Canvas + CanvasScaler + GraphicRaycaster를 코드로 즉시 생성
        // 씬에 EventSystem이 없으면 같이 생성 — 버튼 클릭 이벤트 처리 위해 필수.
        private void EnsureRootAsync(Action<Transform> onReady)
        {
            if (_rootTransform == null)
            {
                var found = FindSceneCanvas();
                if (found != null)
                {
                    _rootTransform = found.transform;
                }
                else
                {
                    var go = new GameObject("@CHMUICanvas",
                        typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                    go.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
                    _rootTransform = go.transform;
                }

                EnsureEventSystem();
            }
            onReady?.Invoke(_rootTransform);
        }

        // 씬에서 UICanvas 태그가 붙은 Canvas → 없으면 임의의 Canvas 순으로 탐색.
        // "UICanvas" 태그가 TagManager에 등록돼 있지 않으면 FindWithTag가 UnityException을 던지므로 try-catch로 무시.
        private static Canvas FindSceneCanvas()
        {
            GameObject tagged = null;
            try { tagged = GameObject.FindWithTag("UICanvas"); }
            catch (UnityException) { /* 태그 미등록 — 무시하고 다음 단계로 */ }

            if (tagged != null)
            {
                var c = tagged.GetComponent<Canvas>();
                if (c != null) return c;
            }

            return UnityEngine.Object.FindFirstObjectByType<Canvas>(FindObjectsInactive.Exclude);
        }

        // 씬에 EventSystem이 없으면 코드로 생성. Input System 패키지가 활성된 프로젝트면
        // InputSystemUIInputModule을 reflection으로 부착(없으면 StandaloneInputModule로 fallback).
        // 패키지에 com.unity.inputsystem를 hard dependency로 박지 않기 위해 reflection 사용.
        private static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;

            var es = new GameObject("@CHMUIEventSystem", typeof(EventSystem));

#if ENABLE_INPUT_SYSTEM
            // Input System 패키지가 있고 활성 — InputSystemUIInputModule 부착
            var moduleType = Type.GetType(
                "UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem",
                throwOnError: false);
            if (moduleType != null)
            {
                es.AddComponent(moduleType);
                return;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            es.AddComponent<StandaloneInputModule>();
#endif
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
