using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChvjUnityInfra
{
    public class UIArg { }

    public abstract class UIBase : MonoBehaviour
    {
        [HideInInspector] public Enum UIType { get; private set; }

        [SerializeField] private Button _backgroundButton;
        [SerializeField] private Button _backButton;

        protected CompositeDisposable closeDisposable = new CompositeDisposable();

        internal void Init(Enum uiType)
        {
            closeDisposable?.Clear();
            closeDisposable = new CompositeDisposable();
            UIType = uiType;

            if (_backgroundButton)
            {
                UnityAction action = () => Close();
                _backgroundButton.onClick.AddListener(action);
                closeDisposable.Add(() =>
                {
                    if (_backgroundButton != null)
                        _backgroundButton.onClick.RemoveListener(action);
                });
            }

            if (_backButton)
            {
                UnityAction action = () => Close();
                _backButton.onClick.AddListener(action);
                closeDisposable.Add(() =>
                {
                    if (_backButton != null)
                        _backButton.onClick.RemoveListener(action);
                });
            }
        }

        public abstract void InitUI(UIArg arg);

        public virtual void Close(bool reuse = true)
        {
            closeDisposable.Clear();
            CHMUI.Instance.CloseUI(this, reuse);
        }
    }
}
