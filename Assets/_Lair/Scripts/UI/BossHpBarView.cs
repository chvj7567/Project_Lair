using System.Collections.Generic;
using ChvjUnityInfra;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 배틀 HUD 상단 보스 바 View(Rule 02 §6.1) — scene-2d-conversion §4.3. 내부 위젯은 private, 외부는 의도 API 만 호출.
    //# 이름줄(제목·HP 수치) · 채움 · 피해 잔상 · 패시브 눈금 9개(◆ 획득 표시) · 상태 아이콘 행.
    public class BossHpBarView : MonoBehaviour
    {
        //# 피해 잔상이 채움을 따라잡는 시간(시안 transition .7s linear)
        public const float LagDuration = 0.7f;

        [SerializeField] private CHText _title;
        [SerializeField] private CHText _hpText;
        [SerializeField] private Image _fill;
        [SerializeField] private Image _lag;
        //# 정적 눈금 9개(프리팹 고정) — 앵커 x 를 임계로 설정, 자식 Gem 은 _tickGems 로 참조
        [SerializeField] private RectTransform[] _ticks;
        [SerializeField] private Image[] _tickGems;
        [SerializeField] private GameObject _statusIconRow;
        [SerializeField] private Image[] _iconSlots;

        private static readonly Color GemOff = new Color32(0x48, 0x54, 0x6E, 0xFF);
        private static readonly Color GemOn = new Color32(0xF7, 0xC6, 0x4A, 0xFF);

        private readonly Dictionary<object, int> _keyToSlot = new Dictionary<object, int>();
        private bool _lagInitialized;
        private float _lagStart;
        private float _lagTarget;
        private float _lagCurrent;
        private float _lagElapsed;

        //# 시작값 → 목표로 duration 동안 선형 감소. 목표가 시작값 이상(회복)이면 즉시 목표.
        public static float EvaluateLag(float start, float target, float elapsed, float duration)
        {
            if (target >= start || duration <= 0f)
                return target;
            float t = Mathf.Clamp01(elapsed / duration);
            return start + (target - start) * t;
        }

        private void OnEnable()
        {
            _lagInitialized = false;
            ClearStatusIcons();
        }

        //# "침입자 {제목}" — 이름줄 왼쪽
        public void SetTitle(string title)
        {
            if (_title != null)
                _title.SetText($"<color=#FF6B5A>침입자</color> {title}");
        }

        public void SetHp(int current, int max)
        {
            float ratio = max > 0 ? Mathf.Clamp01((float)current / max) : 0f;
            if (_fill != null)
                _fill.fillAmount = ratio;
            if (_hpText != null)
                _hpText.SetText($"{current} / {max}");

            if (_lagInitialized == false)
            {
                _lagInitialized = true;
                _lagCurrent = ratio;
                _lagStart = ratio;
                _lagTarget = ratio;
                _lagElapsed = 0f;
            }
            else
            {
                _lagStart = _lagCurrent;
                _lagTarget = ratio;
                _lagElapsed = 0f;
                if (ratio >= _lagCurrent)
                {
                    _lagCurrent = ratio;
                }
            }
            ApplyLag();
        }

        //# 임계(0.9 … 0.1) → 눈금 앵커 x. 남는 눈금은 숨김.
        public void SetTicks(IReadOnlyList<float> thresholds)
        {
            if (_ticks == null)
                return;
            for (int i = 0; i < _ticks.Length; i++)
            {
                if (_ticks[i] == null)
                    continue;
                bool used = thresholds != null && i < thresholds.Count;
                _ticks[i].gameObject.SetActive(used);
                if (used == false)
                    continue;
                _ticks[i].anchorMin = new Vector2(thresholds[i], 0f);
                _ticks[i].anchorMax = new Vector2(thresholds[i], 1f);
                _ticks[i].anchoredPosition = Vector2.zero;
                SetTickAcquired(i, false);
            }
        }

        public void SetTickAcquired(int index, bool acquired)
        {
            if (_tickGems == null || index < 0 || index >= _tickGems.Length || _tickGems[index] == null)
                return;
            _tickGems[index].color = acquired ? GemOn : GemOff;
        }

        private void Update()
        {
            if (_lagInitialized == false || Mathf.Approximately(_lagCurrent, _lagTarget))
                return;
            //# 카드 선택 일시정지(timeScale 0) 중에도 잔상이 마저 줄어 다음 화면에 남지 않게 unscaled
            _lagElapsed += Time.unscaledDeltaTime;
            _lagCurrent = EvaluateLag(_lagStart, _lagTarget, _lagElapsed, LagDuration);
            ApplyLag();
        }

        private void ApplyLag()
        {
            if (_lag != null)
                _lag.fillAmount = _lagCurrent;
        }

        //# 상태 아이콘 — 가장 낮은 빈 슬롯부터, 마지막 제거 시 행 숨김(HpBarView 와 같은 동작)
        public void AddStatusIcon(object key, Sprite icon)
        {
            if (key == null || _iconSlots == null || _keyToSlot.ContainsKey(key))
                return;
            for (int i = 0; i < _iconSlots.Length; i++)
            {
                if (_iconSlots[i] == null || _iconSlots[i].gameObject.activeSelf)
                    continue;
                _iconSlots[i].sprite = icon;
                _iconSlots[i].enabled = icon != null;
                _iconSlots[i].gameObject.SetActive(true);
                _keyToSlot[key] = i;
                if (_statusIconRow != null)
                    _statusIconRow.SetActive(true);
                return;
            }
        }

        public void RemoveStatusIcon(object key)
        {
            if (key == null || _iconSlots == null || _keyToSlot.TryGetValue(key, out int slot) == false)
                return;
            if (slot >= 0 && slot < _iconSlots.Length && _iconSlots[slot] != null)
            {
                _iconSlots[slot].sprite = null;
                _iconSlots[slot].gameObject.SetActive(false);
            }
            _keyToSlot.Remove(key);
            if (_keyToSlot.Count == 0 && _statusIconRow != null)
                _statusIconRow.SetActive(false);
        }

        public void ClearStatusIcons()
        {
            if (_iconSlots != null)
            {
                foreach (Image slot in _iconSlots)
                {
                    if (slot == null)
                        continue;
                    slot.sprite = null;
                    slot.gameObject.SetActive(false);
                }
            }
            _keyToSlot.Clear();
            if (_statusIconRow != null)
                _statusIconRow.SetActive(false);
        }
    }
}
