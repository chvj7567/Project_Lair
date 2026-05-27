using System;
using ChvjUnityInfra;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 빌드 패널의 카드 1개 셀 — 아이콘 + 카테고리 색 프레임 + ×N 배지. CHMPool 로 스폰 (Rule 12).
    public class BuildIconCell : MonoBehaviour
    {
        [SerializeField] private Image _iconImage;
        [SerializeField] private Image _frameImage;
        [SerializeField] private CHText _countText;
        [SerializeField] private CHButton _button;
        //# 클릭 리스너 수명 관리 — OnEnable 에서 Clear 해 풀 재사용 시 리스너 누적 방지.
        private readonly CompositeDisposable _disposable = new CompositeDisposable();

        //# 풀 재사용 시 상태·클릭 리스너 초기화 (Rule 12).
        private void OnEnable()
        {
            _disposable.Clear();
            if (_countText != null) _countText.gameObject.SetActive(false);
            if (_iconImage != null) _iconImage.sprite = null;
            if (_frameImage != null) _frameImage.color = Color.gray;
        }

        //# 카드 바인딩 — 프레임 색·아이콘·클릭 콜백 설정.
        //# onClick == null 이면 자식 셀은 raycast 를 받지 않도록 Button.interactable=false 처리.
        //# 패널 루트가 클릭을 받아 모달을 띄우기 위해서 (기획서 §2.6.2).
        public void Bind(CardData card, Action onClick)
        {
            if (card == null) return;
            if (_frameImage != null)
                _frameImage.color = CardView.CategoryColor(card.Category);
            if (_iconImage != null)
            {
                _iconImage.sprite = card.Icon;
                //# 아이콘 누락 시 비활성 — 프레임 색이 폴백.
                _iconImage.enabled = card.Icon != null;
            }
            if (_button != null)
            {
                if (onClick != null)
                {
                    _button.Interactable = true;
                    _button.OnClick(onClick, _disposable);
                }
                else
                {
                    //# 자식 raycast 가 패널 루트 클릭을 가로채지 않게 비활성.
                    _button.Interactable = false;
                }
            }

            //# 셀 raycast 도 닫아 패널 루트 클릭이 막히지 않게 (기획서 §2.6.2).
            //# onClick != null 인 케이스(기존 호환)에선 raycast 유지.
            bool acceptRaycast = onClick != null;
            if (_frameImage != null) _frameImage.raycastTarget = acceptRaycast;
            if (_iconImage  != null) _iconImage.raycastTarget  = acceptRaycast;
        }

        //# ×N 배지 — N >= 2 일 때만 표시.
        public void SetCount(int count)
        {
            if (_countText == null) return;
            bool show = count >= 2;
            _countText.gameObject.SetActive(show);
            if (show) _countText.SetText($"×{count}");
        }
    }
}
