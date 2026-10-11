using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Lair.UI
{
    //# PickButton 에 붙는 포인터 릴레이 — Selectable 이 이벤트를 소비해도 같은 오브젝트 핸들러는 모두 호출된다. 클릭 판정 없음.
    //# Unity 가 MonoBehaviour 를 스크립트 에셋으로 인식하려면 파일명=클래스명이라 CardView.cs 와 분리(기획서 §5.3 '같은 파일' 불가).
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
