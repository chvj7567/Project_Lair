using Lair.Data;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 해금 연출의 테두리 펄스·링·스파클(monster-codex §6.1) — 칸(축소판)과 상세(전체 강도)가 같이 쓴다. 수치는 Grade_CodexUnlock/CodexConfig 에서.
    //# 위젯은 프리팹 정적 배치 Image(코드 생성 없음). 몸 페이드·이펙트 레이어 페이드는 호출자 소관.
    public class CodexUnlockFx : MonoBehaviour
    {
        [SerializeField] private Image _pulseBorder;
        [SerializeField] private Image[] _rings;
        [SerializeField] private Image[] _sparks;

        private const float RingStepSec = 0.25f;
        private const float SparkDelaySec = 0.5f;

        private CodexConfigRow _cfg;
        private Grade_CodexUnlockRow _row;
        private Color _color;
        private int _pulseMax;
        private int _ringMax;
        private int _sparkMax;
        private float _sparkRadius;
        private float _delay;
        private float _time;
        private bool _active;

        public bool IsPlaying => _active;

        //# 상한 인자로 강도를 정한다 — 칸: cellPulseMax/cellRingMax/cellSparkMax, 상세: int.MaxValue(등급 값 그대로).
        public void Play(CodexConfigRow cfg, Grade_CodexUnlockRow row, Color color, float delaySec, int pulseMax, int ringMax, int sparkMax, float sparkRadius)
        {
            if (cfg == null || row == null)
                return;
            _cfg = cfg;
            _row = row;
            _color = color;
            _pulseMax = pulseMax;
            _ringMax = ringMax;
            _sparkMax = sparkMax;
            _sparkRadius = sparkRadius;
            _delay = delaySec;
            _time = 0f;
            _active = true;
        }

        public void Stop()
        {
            _active = false;
            Hide();
        }

        private void OnEnable()
        {
            Stop();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void Update()
        {
            if (_active == false)
                return;
            _time += Time.unscaledDeltaTime;
            float t = _time - _delay;
            if (t < 0f)
                return;
            bool pulseDone = TickPulse(t);
            bool ringDone = TickRings(t);
            bool sparkDone = TickSparks(t);
            if (pulseDone && ringDone && sparkDone)
            {
                Stop();
            }
        }

        private bool TickPulse(float t)
        {
            int count = Mathf.Min(_row.pulseCount, _pulseMax);
            if (_pulseBorder == null)
                return true;
            bool running = count > 0 && _cfg.pulseSec > 0f && t < count * _cfg.pulseSec;
            _pulseBorder.gameObject.SetActive(running);
            if (running == false)
                return true;
            float phase = (t % _cfg.pulseSec) / _cfg.pulseSec;
            float spread = _row.spreadPx * phase;
            _pulseBorder.rectTransform.sizeDelta = new Vector2(spread * 2f, spread * 2f);
            _pulseBorder.color = WithAlpha(_color, 1f - phase);
            return false;
        }

        private bool TickRings(float t)
        {
            if (_rings == null)
                return true;
            int count = Mathf.Min(_row.ringCount, _ringMax);
            bool allDone = true;
            for (int i = 0; i < _rings.Length; ++i)
            {
                Image ring = _rings[i];
                if (ring == null)
                    continue;
                float local = t - i * RingStepSec;
                bool running = i < count && _cfg.ringSec > 0f && local >= 0f && local < _cfg.ringSec;
                ring.gameObject.SetActive(running);
                if (i < count && local < _cfg.ringSec)
                {
                    allDone = false;
                }
                if (running == false)
                    continue;
                float phase = local / _cfg.ringSec;
                float size = Mathf.Lerp(0.4f, 1.2f, phase);
                ring.rectTransform.localScale = new Vector3(size, size, 1f);
                ring.color = WithAlpha(_color, 1f - phase);
            }
            return allDone;
        }

        private bool TickSparks(float t)
        {
            if (_sparks == null)
                return true;
            int count = Mathf.Min(Mathf.Min(_row.sparkCount, _sparkMax), _sparks.Length);
            float local = t - SparkDelaySec;
            bool running = count > 0 && _cfg.sparkSec > 0f && local >= 0f && local < _cfg.sparkSec;
            for (int i = 0; i < _sparks.Length; ++i)
            {
                Image spark = _sparks[i];
                if (spark == null)
                    continue;
                bool on = running && i < count;
                spark.gameObject.SetActive(on);
                if (on == false)
                    continue;
                float phase = local / _cfg.sparkSec;
                float angle = i * (Mathf.PI * 2f / count);
                spark.rectTransform.anchoredPosition = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * (_sparkRadius * phase);
                spark.color = WithAlpha(_color, 1f - phase);
            }
            return count == 0 || local >= _cfg.sparkSec;
        }

        private void Hide()
        {
            if (_pulseBorder != null)
            {
                _pulseBorder.gameObject.SetActive(false);
            }
            HideAll(_rings);
            HideAll(_sparks);
        }

        private static void HideAll(Image[] images)
        {
            if (images == null)
                return;
            foreach (Image image in images)
            {
                if (image != null)
                {
                    image.gameObject.SetActive(false);
                }
            }
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }
    }
}
