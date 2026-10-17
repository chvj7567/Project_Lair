using System.Collections;
using Lair.Card;
using UnityEngine;
using UnityEngine.UI;

namespace Lair.UI
{
    //# 시너지 7칸 진행 트랙(기획서 card-synergy-indicator §2.1). 칸 = 테두리(_frames) + 안쪽 채움(_fills). 3·5·7번째가 Tier 칸.
    public class SynergyTrack : MonoBehaviour
    {
        [SerializeField] private Image[] _frames;
        [SerializeField] private Image[] _fills;

        private static readonly Color EmptyFill = new Color32(0x0B, 0x0E, 0x14, 0xFF);
        private static readonly Color FrameNormal = new Color32(0x07, 0x09, 0x0E, 0xFF);
        private static readonly Color FrameTierEmpty = new Color32(0xB9, 0xB0, 0x9A, 0xFF);
        private static readonly Color FrameTierFilled = new Color32(0xE8, 0xE1, 0xCF, 0xFF);
        private const float FlashSeconds = 0.3f;

        private Coroutine _flashRoutine;
        private Color _axisColor;
        private int _flashIndex = -1;

        //# 풀 재사용 시 점멸 잔상 제거.
        private void OnEnable()
        {
            StopFlash();
        }

        //# 장수·축 색으로 7칸을 전부 재계산(잔상 금지). flashNewCell 이면 방금 채워진 칸을 0.3초 점멸.
        public void Bind(int count, Color axisColor, bool flashNewCell)
        {
            StopFlash();
            _axisColor = axisColor;
            int filled = SynergyProgress.FilledCells(count);
            int length = _fills != null ? _fills.Length : 0;
            for (int i = 0; i < length; ++i)
            {
                bool isFilled = i < filled;
                bool isTier = SynergyProgress.IsTierCell(i);
                if (_fills[i] != null)
                    _fills[i].color = isFilled ? axisColor : EmptyFill;
                if (_frames != null && i < _frames.Length && _frames[i] != null)
                    _frames[i].color = isTier ? (isFilled ? FrameTierFilled : FrameTierEmpty) : FrameNormal;
            }

            //# _fills 미배선·범위 밖이면 FlashRoutine 이 NRE 이므로 점멸 생략.
            if (flashNewCell && filled > 0 && count <= SynergyProgress.TrackLength && isActiveAndEnabled && _fills != null && filled <= _fills.Length)
            {
                _flashIndex = filled - 1;
                _flashRoutine = StartCoroutine(FlashRoutine());
            }
        }

        private void StopFlash()
        {
            if (_flashRoutine != null)
                StopCoroutine(_flashRoutine);
            _flashRoutine = null;
            if (_flashIndex >= 0 && _fills != null && _flashIndex < _fills.Length && _fills[_flashIndex] != null)
                _fills[_flashIndex].color = _axisColor;
            _flashIndex = -1;
        }

        //# 흰색 → 중간색 → 축 색 3단계 steps.
        private IEnumerator FlashRoutine()
        {
            Image fill = _fills[_flashIndex];
            Color mid = Color.Lerp(Color.white, _axisColor, 0.5f);
            fill.color = Color.white;
            yield return new WaitForSecondsRealtime(FlashSeconds / 3f);
            fill.color = mid;
            yield return new WaitForSecondsRealtime(FlashSeconds / 3f);
            fill.color = _axisColor;
            _flashIndex = -1;
            _flashRoutine = null;
        }
    }
}
