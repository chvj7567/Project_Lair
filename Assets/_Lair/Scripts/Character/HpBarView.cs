using System.Collections.Generic;
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.Character
{
    //# HpBar.prefab 루트에 붙는 View 컴포넌트. 외부는 SetHp / 상태 아이콘 API 만 호출 —
    //# 내부 Fill/txtHp/아이콘 슬롯 위젯은 노출하지 않는다 (Rule 02 §5/§6 캡슐화).
    public class HpBarView : MonoBehaviour
    {
        [SerializeField] private Image _fill;     //# HpBar 내부 Fill (채움)
        [SerializeField] private CHText _txtHp;   //# HpBar 내부 txtHp ("현재/최대")

        //# 상태 아이콘 행 — HorizontalLayoutGroup 컨테이너(기본 비활성) + 정적 8 슬롯.
        //# 몬스터 바도 이 프리팹을 공유 — 컨테이너 기본 비활성이라 영웅 경로만 채워진다.
        [SerializeField] private GameObject _statusIconRow;
        [SerializeField] private Image[] _iconSlots;

        //# key(aura 타입) ↔ 점유 슬롯 인덱스 매핑.
        private readonly Dictionary<object, int> _keyToSlot = new();

        private bool _showText = true;

        //# 텍스트 표시 토글 — 몬스터 바는 false 로 숨김(영웅 HUD 는 기본 true).
        public void SetTextVisible(bool visible)
        {
            _showText = visible;
            if (_txtHp != null)
                _txtHp.gameObject.SetActive(visible);
        }

        public void SetHp(int current, int max)
        {
            if (_fill != null)
                _fill.fillAmount = max > 0 ? (float)current / max : 0f;
            if (_showText == false) return;
            if (_txtHp != null)
                _txtHp.SetText($"{current}/{max}");
        }

        //# 상태 아이콘 추가 — 가장 낮은 빈 슬롯에 채움(기획서 §4.1). 같은 key 중복은 무시.
        //# sprite null 이면 슬롯 미표시(graceful) — dict 매핑 누락 안전망(기획서 §3.3).
        public void AddStatusIcon(object key, Sprite icon)
        {
            if (key == null || _iconSlots == null) return;
            if (_keyToSlot.ContainsKey(key)) return;
            for (int i = 0; i < _iconSlots.Length; i++)
            {
                if (_iconSlots[i] != null && _iconSlots[i].gameObject.activeSelf == false)
                {
                    _iconSlots[i].sprite = icon;
                    _iconSlots[i].enabled = icon != null;
                    _iconSlots[i].gameObject.SetActive(true);
                    _keyToSlot[key] = i;
                    if (_statusIconRow != null)
                        _statusIconRow.SetActive(true);
                    return;
                }
            }
        }

        //# 상태 아이콘 제거 — 해당 key 슬롯 비활성 + 매핑 해제. 마지막 제거 시 컨테이너 비활성.
        public void RemoveStatusIcon(object key)
        {
            if (key == null || _iconSlots == null) return;
            if (_keyToSlot.TryGetValue(key, out int slot) == false) return;
            if (slot >= 0 && slot < _iconSlots.Length && _iconSlots[slot] != null)
            {
                _iconSlots[slot].sprite = null;
                _iconSlots[slot].gameObject.SetActive(false);
            }
            _keyToSlot.Remove(key);
            if (_keyToSlot.Count == 0 && _statusIconRow != null)
                _statusIconRow.SetActive(false);
        }

        //# 모든 상태 아이콘 제거 — 영웅 풀 반환/재사용 시 잔존 방지.
        public void ClearStatusIcons()
        {
            if (_iconSlots != null)
            {
                foreach (Image slot in _iconSlots)
                {
                    if (slot != null)
                    {
                        slot.sprite = null;
                        slot.gameObject.SetActive(false);
                    }
                }
            }
            _keyToSlot.Clear();
            if (_statusIconRow != null)
                _statusIconRow.SetActive(false);
        }
    }
}
