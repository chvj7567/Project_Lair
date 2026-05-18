using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace ChvjUnityInfra
{
    [RequireComponent(typeof(TMP_Text))]
    [MovedFrom(false, null, null, "TextEx")]
    public class CHText : MonoBehaviour
    {
        /// <summary>
        /// stringID로 문자열을 조회할 때 사용할 프로바이더.
        /// 게임 부팅 시 한 번만 등록: CHText.StringProvider = new GameStringProvider();
        /// </summary>
        public static IStringProvider StringProvider;

        /// <summary>
        /// stringID 모드일 때 사용할 폰트/머티리얼 프로바이더.
        /// 게임 부팅 시 한 번만 등록: CHText.FontProvider = new GameFontProvider();
        /// </summary>
        public static IFontProvider FontProvider;

        [SerializeField] private int _stringID = -1;

        private TMP_Text _text;
        private object[] _arrArg;
        private bool _initialize;

        private void Awake()
        {
            Init();
        }

        private void Init()
        {
            if (_initialize)
                return;

            _initialize = true;

            _text = GetComponent<TMP_Text>();

            if (_stringID != -1)
            {
                if (StringProvider != null)
                {
                    _text.text = StringProvider.GetString(_stringID);
                }

                if (FontProvider != null)
                {
                    _text.font = FontProvider.GetFont();
                    _text.material = FontProvider.GetFontMaterial();
                }
            }
        }

        public void SetText(params object[] arrArg)
        {
            Init();

            _arrArg = arrArg;

            if (_stringID == -1)
            {
                StringBuilder sb = new StringBuilder();
                foreach (var arg in arrArg)
                {
                    sb.Append(arg?.ToString() ?? string.Empty);
                }

                _text.text = sb.ToString();
            }
            else if (StringProvider != null)
            {
                _text.text = string.Format(StringProvider.GetString(_stringID), arrArg);
            }
        }

        public void SetColor(Color color)
        {
            Init();

            _text.color = color;
        }

        public void SetStringID(int stringID)
        {
            Init();

            _arrArg = null;
            _stringID = stringID;

            if (StringProvider != null)
            {
                _text.text = StringProvider.GetString(_stringID);
            }
        }

        public void SetPlusString(string plusString)
        {
            Init();

            if (string.IsNullOrEmpty(plusString) == false)
            {
                _text.text = _text.text + " + " + plusString;
            }
        }

        public string GetString()
        {
            Init();

            return _text.text;
        }
    }
}
