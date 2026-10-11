using System;
using ChvjUnityInfra;
using Lair.Village;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 월드맵 스테이지 노드 1칸 — 표시 + 클릭 수신만 (Rule 02 §6). 모든 위젯은 [SerializeField] private, 외부엔 의도 API 만 노출(§6.1).
    //# 상태별 석판 스프라이트(해금/보스/클리어/잠금)는 프리팹에 직접 참조해 스왑한다.
    public class WorldMapNodeView : MonoBehaviour
    {
        [SerializeField] private CHButton _button;
        [SerializeField] private Image _background;
        [SerializeField] private CHText _labelText;          //# 노드 안 "1-1" / "잠금"
        [SerializeField] private CHText _nameText;           //# 노드 아래 스테이지명 / "???"
        [SerializeField] private CHText _clearedTagText;     //# 노드 아래 "클리어"
        [SerializeField] private GameObject _bossTag;        //# 위쪽 "BOSS" 태그 루트
        [SerializeField] private GameObject _selectedMarker; //# 선택 ▼ + 금색 외곽선 루트
        [SerializeField] private Sprite _openSprite;
        [SerializeField] private Sprite _bossSprite;
        [SerializeField] private Sprite _clearedSprite;
        [SerializeField] private Sprite _lockedSprite;

        private static readonly Color LockedTextColor = new Color(0.349f, 0.380f, 0.478f, 1f);

        //# BOSS 태그 y 오프셋(노드 상단 기준 위로) — 기획서 §4.3. 태그 앵커는 노드 상단 중앙(하단 피벗)으로 둔다.
        private const float BossTagNormalY = 20f;
        private const float BossTagSelectedY = 44f;

        //# 클릭 핸들러 1회 등록 — 팝업 InitUI 에서 호출. disposable.Clear() 시 해제된다.
        public void SetClickHandler(Action onClick, CompositeDisposable disposable)
        {
            if (_button != null)
            {
                _button.OnClick(onClick, disposable);
            }
        }

        //# 매 갱신마다 전체 상태를 덮어쓴다(이전 상태 잔존 방지).
        public void Show(WorldMapNodeData data)
        {
            if (data == null)
                return;

            bool locked = data.State == EWorldMapNodeState.Locked;
            if (_background != null)
            {
                _background.sprite = PickSprite(data);
                _background.color = Color.white;
            }
            if (_labelText != null)
            {
                _labelText.SetText(data.LabelText);
                _labelText.SetColor(locked ? LockedTextColor : Color.white);
            }
            if (_nameText != null)
            {
                _nameText.SetText(data.NameText);
                _nameText.SetColor(locked ? LockedTextColor : Color.white);
            }
            if (_clearedTagText != null)
            {
                _clearedTagText.SetText(data.ClearedTagText);
                _clearedTagText.gameObject.SetActive(data.State == EWorldMapNodeState.Cleared);
            }
            if (_bossTag != null)
            {
                _bossTag.SetActive(data.IsBoss);
                //# 선택 ▼ 마커와 겹치지 않게 보스 선택 시 태그를 위로 올린다.
                RectTransform tagRect = _bossTag.transform as RectTransform;
                if (tagRect != null)
                {
                    Vector2 pos = tagRect.anchoredPosition;
                    pos.y = data.IsSelected ? BossTagSelectedY : BossTagNormalY;
                    tagRect.anchoredPosition = pos;
                }
            }
            if (_selectedMarker != null)
            {
                _selectedMarker.SetActive(data.IsSelected);
            }
        }

        private Sprite PickSprite(WorldMapNodeData data)
        {
            Sprite sprite;
            switch (data.State)
            {
                case EWorldMapNodeState.Locked:
                    sprite = _lockedSprite;
                    break;
                case EWorldMapNodeState.Cleared:
                    sprite = _clearedSprite;
                    break;
                default:
                    sprite = data.IsBoss ? _bossSprite : _openSprite;
                    break;
            }
            return sprite != null ? sprite : _background.sprite;
        }
    }
}
