using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace ChvjUnityInfra
{
    /// <summary>
    /// UI 매니저 — UI 캐싱/재사용/ESC 자동닫기.
    /// 키는 이름 문자열(EnumNameCache 경유) — CHMResource 와 동일 계약이며 Enum 키 박싱을 없앤다.
    /// 게임별 EUI 타입은 여전히 몰라도 됨(제네릭 &lt;TEnum&gt; 로 받음). Enum 매개변수 오버로드는 하위 호환용 Obsolete.
    /// </summary>
    public class CHMUI : CHSingleton<CHMUI>
    {
        private bool _initialize = false;
        private Transform _rootTransform;

        // 현재 활성 UI
        private Dictionary<string, UIBase> _dicCurrentUI = new Dictionary<string, UIBase>();
        // 비동기 로딩 중 닫기 요청이 들어온 UI 키
        private HashSet<string> _dicWaitCloseUI = new HashSet<string>();
        // 인스턴스화한 UI 캐시 (재사용용)
        private Dictionary<string, UIBase> _dicCashingUI = new Dictionary<string, UIBase>();

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
        /// 활성 UI 중 ESC로 닫을 수 있는(CanCloseByEsc) 최상위 UI 한 개를 닫는다. ESC 핸들러가 호출.
        /// 닫을 수 있는 UI가 없으면(예: 베이스 HUD만 떠 있음) 아무것도 닫지 않는다.
        /// Input System Only 프로젝트에서는 게임 측 InputAction 콜백에서 직접 호출하면 동일 동작.
        /// </summary>
        public void CloseTopUI()
        {
            //# Dictionary 열거 순서는 제거 후 신뢰 불가 → 시각적 최상위는 sibling index 로 판정(ActivateUI 가 SetAsLastSibling 호출).
            UIBase target = null;
            int topSibling = -1;
            foreach (UIBase ui in _dicCurrentUI.Values)
            {
                if (ui == null)
                    continue;
                if (ui.CanCloseByEsc == false)
                    continue;

                int sibling = ui.transform.GetSiblingIndex();
                if (sibling > topSibling)
                {
                    topSibling = sibling;
                    target = ui;
                }
            }

            if (target == null)
                return;

            CloseUI(target);
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

        /// <summary>박싱 없는 진입점. 게임 측은 이 오버로드로 자동 바인딩된다(제네릭 identity 변환이 Enum 박싱 변환보다 우선).</summary>
        public void ShowUI<TEnum>(TEnum uiType, UIArg arg = null, Action<UIBase> callback = null) where TEnum : struct, Enum
        {
            ShowUIByName(EnumNameCache<TEnum>.Get(uiType), arg, callback);
        }

        /// <summary>ShowUI의 async/await 버전. 캔버스 확보·인스턴스 로드 실패 시 결과는 null.</summary>
        public Task<UIBase> ShowUIAsync<TEnum>(TEnum uiType, UIArg arg = null) where TEnum : struct, Enum
        {
            TaskCompletionSource<UIBase> tcs = new TaskCompletionSource<UIBase>();
            ShowUIByName(EnumNameCache<TEnum>.Get(uiType), arg, ui => tcs.TrySetResult(ui));
            return tcs.Task;
        }

        /// <summary>박싱 없는 진입점.</summary>
        public void CloseUI<TEnum>(TEnum uiType, bool reuse = false) where TEnum : struct, Enum
        {
            CloseUIByName(EnumNameCache<TEnum>.Get(uiType), reuse);
        }

        [Obsolete("제네릭 오버로드를 사용하세요 — Enum 매개변수는 박싱됩니다", false)]
        public void ShowUI(Enum uiType, UIArg arg = null, Action<UIBase> callback = null)
        {
            //# 여기서는 이미 호출부에서 박싱된 Enum 을 받은 뒤라 추가 박싱이 아님.
            ShowUIByName(uiType.ToString(), arg, callback);
        }

        [Obsolete("제네릭 오버로드를 사용하세요 — Enum 매개변수는 박싱됩니다", false)]
        public Task<UIBase> ShowUIAsync(Enum uiType, UIArg arg = null)
        {
            TaskCompletionSource<UIBase> tcs = new TaskCompletionSource<UIBase>();
            ShowUIByName(uiType.ToString(), arg, ui => tcs.TrySetResult(ui));
            return tcs.Task;
        }

        [Obsolete("제네릭 오버로드를 사용하세요 — Enum 매개변수는 박싱됩니다", false)]
        public void CloseUI(Enum uiType, bool reuse = false)
        {
            CloseUIByName(uiType.ToString(), reuse);
        }

        private void ShowUIByName(string uiTypeName, UIArg arg, Action<UIBase> callback)
        {
            if (arg == null)
                arg = new UIArg();

            EnsureRootAsync(root =>
            {
                //# 캔버스 확보 실패 — callback(null) 로 ShowUIAsync 의 Task 를 null 완료시켜 await 가 풀리게 한다.
                if (root == null)
                {
                    callback?.Invoke(null);
                    return;
                }
                ShowUIInternal(root, uiTypeName, arg, callback);
            });
        }

        private void ShowUIInternal(Transform root, string uiTypeName, UIArg arg, Action<UIBase> callback)
        {
            // 캐시된 UI가 있다면 재사용
            if (_dicCashingUI.TryGetValue(uiTypeName, out UIBase cached))
            {
                ActivateUI(cached, uiTypeName, arg, callback);
            }
            else
            {
                // 비동기 로드 — 콜백에서 _dicWaitCloseUI 검사 (race 처리)
                CHMResource.Instance.Instantiate<GameObject>(uiTypeName, (ui) =>
                {
                    //# 프리팹 로드/인스턴스 실패 — callback(null) 로 ShowUIAsync 의 await 가 멈추지 않게 한다.
                    if (ui == null)
                    {
                        callback?.Invoke(null);
                        return;
                    }
                    //# 콜백이 씬 전환 후에 도착한 경우 — root가 무효화됐으니 인스턴스 폐기 + callback(null).
                    //# (prefab이 활성 상태로 저장된 경우 InitUI 없이 Start가 호출돼 NullRef 위험.)
                    if (_rootTransform == null)
                    {
                        UnityEngine.Object.Destroy(ui);
                        callback?.Invoke(null);
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
                    //# UIBase 컴포넌트 부재 — 표시할 수 없으니 폐기 + callback(null).
                    if (newUI == null)
                    {
                        UnityEngine.Object.Destroy(ui);
                        callback?.Invoke(null);
                        return;
                    }

                    _dicCashingUI[uiTypeName] = newUI;

                    //# 로딩 중에 CloseUI 요청이 들어왔다면 즉시 닫음 — 표시된 UI 없음이므로 callback(null).
                    if (_dicWaitCloseUI.Contains(uiTypeName))
                    {
                        _dicWaitCloseUI.Remove(uiTypeName);
                        newUI.gameObject.SetActive(false);
                        callback?.Invoke(null);
                        return;
                    }

                    ActivateUI(newUI, uiTypeName, arg, callback);
                });
            }
        }

        private void ActivateUI(UIBase uiBase, string uiTypeName, UIArg arg, Action<UIBase> callback)
        {
            uiBase.Init(uiTypeName);
            uiBase.InitUI(arg);
            uiBase.gameObject.SetActive(true);
            uiBase.transform.SetAsLastSibling();

            _dicCurrentUI[uiTypeName] = uiBase;

            callback?.Invoke(uiBase);
        }

        public void CloseUI(UIBase uiBase, bool reuse = false)
        {
            if (uiBase == null)
                return;

            CloseUIByName(uiBase.UITypeName, reuse);
        }

        private void CloseUIByName(string uiTypeName, bool reuse)
        {
            // 활성 UI에 있다면 즉시 닫음
            if (_dicCurrentUI.TryGetValue(uiTypeName, out UIBase uiBase))
            {
                uiBase.gameObject.SetActive(false);
                _dicCurrentUI.Remove(uiTypeName);
            }
            else
            {
                // 아직 로딩 중이라면 닫기 대기열에 추가 (ShowUI 콜백에서 처리)
                _dicWaitCloseUI.Add(uiTypeName);
            }

            if (reuse == false)
            {
                RemoveCashingUI(uiTypeName);
            }
        }

        private void RemoveCashingUI(string uiTypeName)
        {
            if (_dicCashingUI.TryGetValue(uiTypeName, out UIBase uiBase))
            {
                _dicCashingUI.Remove(uiTypeName);
                if (uiBase != null)
                {
                    Destroy(uiBase.gameObject);
                }
            }
        }
    }
}
