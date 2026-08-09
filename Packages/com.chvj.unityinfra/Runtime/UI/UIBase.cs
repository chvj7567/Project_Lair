using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ChvjUnityInfra
{
    public class UIArg { }

    public abstract class UIBase : MonoBehaviour
    {
        //# 키 체계는 이름 문자열(EnumNameCache 경유) — CHMResource 와 동일 계약, Enum 박싱 없음.
        [HideInInspector] public string UITypeName { get; private set; }

        //# ESC(뒤로가기)로 닫을 수 있는지 여부. 기본 true(기존 동작 유지). 베이스 HUD 등은 override 로 false.
        public virtual bool CanCloseByEsc => true;

        [SerializeField] private Button _backgroundButton;
        [SerializeField] private Button _backButton;

        protected CompositeDisposable closeDisposable = new CompositeDisposable();

        internal void Init(string uiTypeName)
        {
            closeDisposable?.Clear();
            closeDisposable = new CompositeDisposable();
            UITypeName = uiTypeName;

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
