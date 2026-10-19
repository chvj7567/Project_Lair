using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 잠긴 난이도 안내 카드 — 제목·본문·진행 점·줄어드는 타이머 바. 표시·타이밍만(Rule 02 §6), 문구·진행 값은 VM 데이터.
    public class WorldMapDifficultyLockNoticeView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup _group;
        [SerializeField] private RectTransform _card;
        [SerializeField] private CHText _titleText;
        [SerializeField] private CHText _bodyText;
        [SerializeField] private Image[] _pips = new Image[0];
        [SerializeField] private RectTransform _timerBar;   //# 피벗 좌측 — localScale.x 로 줄어든다

        private const float LifeSeconds = 2.4f;
        private const float FadeSeconds = 0.18f;
        private const float SlideDistance = 10f;

        private static readonly Color PipOff = new Color32(0x0a, 0x0d, 0x14, 0xff);

        private bool _visible;
        private float _shownAt;
        private Vector2 _cardBase;

        private void Awake()
        {
            if (_card != null)
            {
                _cardBase = _card.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            Hide();
        }

        //# 표시 중 재호출되면 내용을 갱신하고 타이머를 재시작한다.
        public void Show(WorldMapDifficultyLockData data)
        {
            if (data == null)
                return;

            if (_titleText != null)
            {
                _titleText.SetText(data.TitleText);
                _titleText.SetColor(UiDotPalette.DifficultyColor(data.Difficulty));
            }
            if (_bodyText != null)
            {
                _bodyText.SetText(data.HintText);
            }
            for (int i = 0; i < _pips.Length; ++i)
            {
                if (_pips[i] == null)
                    continue;
                _pips[i].gameObject.SetActive(i < data.StepCount);
                _pips[i].color = i <= data.ClearedMax ? UiDotPalette.Soul : (i == data.ClearedMax + 1 ? UiDotPalette.Gold : PipOff);
            }
            _visible = true;
            _shownAt = Time.unscaledTime;
            Apply(0f);
        }

        public void Hide()
        {
            _visible = false;
            Apply(-1f);
        }

        private void Update()
        {
            if (_visible == false)
                return;

            float t = Time.unscaledTime - _shownAt;
            if (t >= LifeSeconds + FadeSeconds)
            {
                Hide();
                return;
            }
            Apply(t);
        }

        //# t<0 이면 숨김. 페이드인(0~Fade) → 유지 → 페이드아웃(Life~Life+Fade). 알파·위치 소폭 이동, 타이머는 Life 기준 선형.
        private void Apply(float t)
        {
            float k = 0f;
            if (t >= 0f)
            {
                k = t < FadeSeconds ? t / FadeSeconds : (t < LifeSeconds ? 1f : 1f - (t - LifeSeconds) / FadeSeconds);
            }
            if (_group != null)
            {
                _group.alpha = k;
            }
            if (_card != null)
            {
                _card.anchoredPosition = _cardBase + new Vector2(0f, (k - 1f) * SlideDistance);
            }
            if (_timerBar != null)
            {
                float life = t < 0f ? 0f : Mathf.Clamp01(1f - t / LifeSeconds);
                _timerBar.localScale = new Vector3(life, 1f, 1f);
            }
        }
    }
}
