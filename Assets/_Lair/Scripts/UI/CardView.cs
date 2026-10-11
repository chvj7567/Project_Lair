using System;
using ChvjUnityInfra;
using Lair.Card;
using Lair.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 단일 카드 표시 — 이름/설명/카테고리 색 테두리/픽 버튼/3픽 캡 배지.
    public class CardView : MonoBehaviour
    {
        [SerializeField] private CHText _nameText;
        [SerializeField] private CHText _descText;
        [SerializeField] private Image _border;
        //# 3택1 팝업 상단 일러스트. CardData.CardImage 가 null 이면 영역을 숨긴다.
        [SerializeField] private Image _artImage;
        [SerializeField] private CHButton _pickButton;
        //# 3픽 캡 — 이미 픽한 횟수 N (0 이면 숨김, 1~2 면 "N/3"). 3 도달 카드는 후보에 안 나옴.
        [SerializeField] private CHText _countBadge;
        //# 종류 표시 — 패시브=소울 / 액티브=금. 프레임 링 tint + 종류 라벨 + 선택 버튼 스프라이트(soul/gold).
        [SerializeField] private Image _kindRing;
        [SerializeField] private CHText _kindLabel;
        [SerializeField] private Image _pickButtonImage;
        [SerializeField] private Sprite _passiveButtonSprite;
        [SerializeField] private Sprite _activeButtonSprite;
        [SerializeField] private GameObject _countBadgeBg;   //# CountBadge 뒤 배지 배경 — 배지와 함께 토글
        //# 시너지 축 아이콘(좌상단 종류 라벨 아래) — 아이콘이 없으면 루트째 숨김(빈 박스 금지).
        [SerializeField] private GameObject _axisIconRoot;
        [SerializeField] private Image _axisIconBorder;
        [SerializeField] private Image _axisIconImage;

        //# 비주얼 폴리시 장식 — 전부 미할당 허용(해당 표시만 건너뜀).
        [SerializeField] private Image[] _kindTintImages;
        [SerializeField] private Image[] _axisTintImages;
        [SerializeField] private Image[] _axisGems;
        [SerializeField] private Image _frameGold;
        [SerializeField] private Image _hoverGlow;
        [SerializeField] private Image _pickBright;
        [SerializeField] private Image _dimOverlay;
        [SerializeField] private GameObject _hoverGuard;
        [SerializeField] private CardPointerRelay _pointerRelay;
        [SerializeField] private float _tweenSeconds = 0.16f;

        private const float LiftPixels = 6f;
        private const float PressScale = 1.03f;
        private const float GlowHoverAlpha = 0.8f;
        private const float GlowPressAlpha = 1f;
        private const float BrightHoverAlpha = 0.15f;
        private const float BrightPressAlpha = 0.3f;
        private const float DimAlpha = 0.5f;
        private static readonly Color FrameHoverColor = new Color(1f, 0.8392157f, 0.4f, 1f);
        private static readonly Color FramePressColor = new Color(1f, 0.9411765f, 0.6588235f, 1f);
        private static readonly Color GemOffColor = new Color(0.227451f, 0.2784314f, 0.3921569f, 1f);

        //# 눌림 상태가 바뀔 때 (자신, 눌림 여부) 통지 — 팝업이 나머지 카드를 어둡게 한다.
        public event Action<CardView, bool> PressChanged;
        public bool IsPressed => _pressed;

        private bool _hovered;
        private bool _pressed;
        private bool _dimmed;
        private float _hoverT;
        private float _pressT;
        private float _dimT;
        private bool _liftActive;
        private float _baseY;
        private bool _hasAxis;
        private Color _axisColor;
        private Color? _restFrame;
        private float? _restGlowAlpha;
        private float? _restBrightAlpha;
        private float? _restDimAlpha;

        private void Awake() => CaptureRest();

        private void OnEnable()
        {
            CaptureRest();
            ResetVisual();
            if (_pointerRelay != null)
            {
                _pointerRelay.HoverChanged += OnHoverChanged;
                _pointerRelay.PressedChanged += OnPressedChanged;
            }
        }

        private void OnDisable()
        {
            if (_pointerRelay != null)
            {
                _pointerRelay.HoverChanged -= OnHoverChanged;
                _pointerRelay.PressedChanged -= OnPressedChanged;
            }
            ResetVisual();
        }

        private void Update()
        {
            if (IsAtTarget())
            {
                //# 레이아웃 재빌드가 y 를 되돌려도 리프트 유지 중에는 매 프레임 복구.
                if (_liftActive)
                {
                    ApplyLift(LiftPixels * _hoverT);
                }
                return;
            }

            float step = _tweenSeconds > 0f ? Time.unscaledDeltaTime / _tweenSeconds : 1f;
            _hoverT = Mathf.MoveTowards(_hoverT, TargetHover, step);
            _pressT = Mathf.MoveTowards(_pressT, TargetPress, step);
            _dimT = Mathf.MoveTowards(_dimT, TargetDim, step);
            ApplyVisual();
        }

        private float TargetHover => (_hovered || _pressed) ? 1f : 0f;
        private float TargetPress => _pressed ? 1f : 0f;
        private float TargetDim => (_dimmed && _pressed == false) ? 1f : 0f;

        private bool IsAtTarget()
        {
            return Mathf.Approximately(_hoverT, TargetHover)
                && Mathf.Approximately(_pressT, TargetPress)
                && Mathf.Approximately(_dimT, TargetDim);
        }

        private void OnHoverChanged(bool hovered)
        {
            _hovered = hovered;
            OnStateChanged();
        }

        private void OnPressedChanged(bool pressed)
        {
            if (_pressed == pressed)
                return;

            _pressed = pressed;
            OnStateChanged();
            PressChanged?.Invoke(this, pressed);
        }

        //# 어두움 설정 — 자신이 눌림이면 표시에서 무시된다.
        public void SetDimmed(bool dimmed)
        {
            _dimmed = dimmed;
            OnStateChanged();
        }

        private void OnStateChanged()
        {
            if (_hoverGuard != null)
            {
                _hoverGuard.SetActive(TargetHover > 0f);
            }
            if (_tweenSeconds <= 0f)
            {
                _hoverT = TargetHover;
                _pressT = TargetPress;
                _dimT = TargetDim;
                ApplyVisual();
            }
        }

        private void CaptureRest()
        {
            if (_restFrame == null && _frameGold != null)
            {
                _restFrame = _frameGold.color;
            }
            if (_restGlowAlpha == null && _hoverGlow != null)
            {
                _restGlowAlpha = _hoverGlow.color.a;
            }
            if (_restBrightAlpha == null && _pickBright != null)
            {
                _restBrightAlpha = _pickBright.color.a;
            }
            if (_restDimAlpha == null && _dimOverlay != null)
            {
                _restDimAlpha = _dimOverlay.color.a;
            }
        }

        //# 풀/재오픈 리셋 — 상태·진행값·표시를 휴지 기준값으로.
        private void ResetVisual()
        {
            _hovered = false;
            _pressed = false;
            _dimmed = false;
            _hoverT = 0f;
            _pressT = 0f;
            _dimT = 0f;
            if (_hoverGuard != null)
            {
                _hoverGuard.SetActive(false);
            }
            ApplyVisual();
        }

        private void ApplyVisual()
        {
            if (_restFrame != null && _frameGold != null)
            {
                Color c = Color.Lerp(_restFrame.Value, FrameHoverColor, _hoverT);
                _frameGold.color = Color.Lerp(c, FramePressColor, _pressT);
            }
            if (_restGlowAlpha != null)
            {
                SetAlpha(_hoverGlow, Mathf.Lerp(Mathf.Lerp(_restGlowAlpha.Value, GlowHoverAlpha, _hoverT), GlowPressAlpha, _pressT));
            }
            if (_restBrightAlpha != null)
            {
                SetAlpha(_pickBright, Mathf.Lerp(Mathf.Lerp(_restBrightAlpha.Value, BrightHoverAlpha, _hoverT), BrightPressAlpha, _pressT));
            }
            if (_restDimAlpha != null)
            {
                SetAlpha(_dimOverlay, Mathf.Lerp(_restDimAlpha.Value, DimAlpha, _dimT));
            }

            transform.localScale = Vector3.one * Mathf.Lerp(1f, PressScale, _pressT);
            ApplyLift(LiftPixels * _hoverT);
        }

        //# 레이아웃이 위치를 소유 — y 만, 리프트 중에는 기준 y 에 매번 덧쓰고 0 이 되면 복원.
        private void ApplyLift(float lift)
        {
            RectTransform rt = transform as RectTransform;
            if (rt == null)
                return;

            if (lift > 0f)
            {
                if (_liftActive == false)
                {
                    _baseY = rt.anchoredPosition.y;
                    _liftActive = true;
                }
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _baseY + lift);
            }
            else if (_liftActive)
            {
                rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, _baseY);
                _liftActive = false;
            }
        }

        private static void SetAlpha(Image image, float alpha)
        {
            if (image == null)
                return;

            Color c = image.color;
            c.a = alpha;
            image.color = c;
        }

        //# rgb 만 칠하고 각 원소의 현재 알파는 보존. null 원소·배열은 건너뜀.
        internal static void TintRgb(Image[] images, Color color)
        {
            if (images == null)
                return;

            for (int i = 0; i < images.Length; ++i)
            {
                if (images[i] == null)
                    continue;

                Color c = color;
                c.a = images[i].color.a;
                images[i].color = c;
            }
        }

        private static void SetActiveAll(Image[] images, bool active)
        {
            if (images == null)
                return;

            for (int i = 0; i < images.Length; ++i)
            {
                if (images[i] != null)
                {
                    images[i].gameObject.SetActive(active);
                }
            }
        }

        //# Tier 보석 — activeTier 개는 축 색, 나머지는 꺼짐색. 아이콘이 없어 숨겨진 상태면 켜지 않는다.
        public void SetAxisTier(int activeTier)
        {
            if (_hasAxis == false || _axisGems == null)
                return;

            for (int i = 0; i < _axisGems.Length; ++i)
            {
                if (_axisGems[i] != null)
                {
                    _axisGems[i].color = i < activeTier ? _axisColor : GemOffColor;
                }
            }
        }

        public void Bind(CardData card, Action onClick) => Bind(card, onClick, 0);

        //# 카드 종류 표시 — 팝업이 Bind 전에 호출. 미할당 위젯은 건너뛴다.
        public void SetKind(bool isPassive)
        {
            TintRgb(_kindTintImages, UiDotPalette.CardKind(isPassive));
            if (_kindRing != null)
            {
                _kindRing.color = UiDotPalette.CardKind(isPassive);
            }
            if (_kindLabel != null)
            {
                _kindLabel.SetText(isPassive ? "패시브" : "액티브");
                _kindLabel.SetColor(UiDotPalette.CardKind(isPassive));
            }
            Sprite buttonSprite = isPassive ? _passiveButtonSprite : _activeButtonSprite;
            if (_pickButtonImage != null && buttonSprite != null)
            {
                _pickButtonImage.sprite = buttonSprite;
            }
        }

        //# 카드 축 아이콘 — SO 에서 card.Axis 에 해당하는 Sprite 를 찾아 표시. 카드·SO·Sprite 중 하나라도 없으면 숨김.
        public void SetAxisIcon(CardData card, SynergyVisualConfig config)
        {
            if (_axisIconRoot == null)
                return;

            Sprite icon = card != null && config != null ? config.GetIcon(card.Axis) : null;
            _axisIconRoot.SetActive(icon != null);
            _hasAxis = icon != null;
            SetActiveAll(_axisTintImages, _hasAxis);
            SetActiveAll(_axisGems, _hasAxis);
            if (icon == null)
                return;

            _axisColor = BuildSynergyPanel.AxisColor[card.Axis];
            TintRgb(_axisTintImages, _axisColor);

            if (_axisIconImage != null)
            {
                _axisIconImage.sprite = icon;
            }
            if (_axisIconBorder != null)
            {
                _axisIconBorder.color = BuildSynergyPanel.AxisColor[card.Axis];
            }
        }

        public void Bind(CardData card, Action onClick, int pickCount)
        {
            _nameText.SetText(card.DisplayName);
            _descText.SetText(card.Description);
            //# 테두리 색 — 카드 ID 기준 단일 출처 (CardBorderColors).
            _border.color = CardBorderColors.BorderColorOf(card.Id);
            ResetVisual();
            ApplyArt(card);
            _pickButton.OnClick(onClick);
            UpdateBadge(pickCount);
        }

        //# 일러스트 적용 — null 이면 아트 영역 비활성(폴백). Bind 에서 호출.
        public void ApplyArt(CardData card)
        {
            if (_artImage == null)
                return;

            Sprite art = card != null ? card.CardImage : null;
            if (art == null)
            {
                _artImage.gameObject.SetActive(false);
                return;
            }

            _artImage.gameObject.SetActive(true);
            _artImage.sprite = art;
        }

        private void UpdateBadge(int pickCount)
        {
            if (_countBadge == null)
                return;

            if (pickCount <= 0)
            {
                _countBadge.gameObject.SetActive(false);
                if (_countBadgeBg != null)
                {
                    _countBadgeBg.SetActive(false);
                }
                return;
            }

            _countBadge.gameObject.SetActive(true);
            if (_countBadgeBg != null)
            {
                _countBadgeBg.SetActive(true);
            }
            _countBadge.SetText(pickCount + "/" + Lair.Card.CardPickCounter.Cap);
        }
    }

    //# PickButton 에 붙는 포인터 릴레이 — Selectable 이 이벤트를 소비해도 같은 오브젝트 핸들러는 모두 호출된다. 클릭 판정 없음.
    public class CardPointerRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public event Action<bool> HoverChanged;
        public event Action<bool> PressedChanged;

        public void OnPointerEnter(PointerEventData eventData) => HoverChanged?.Invoke(true);

        public void OnPointerExit(PointerEventData eventData)
        {
            HoverChanged?.Invoke(false);
            PressedChanged?.Invoke(false);
        }

        public void OnPointerDown(PointerEventData eventData) => PressedChanged?.Invoke(true);

        public void OnPointerUp(PointerEventData eventData) => PressedChanged?.Invoke(false);
    }
}
