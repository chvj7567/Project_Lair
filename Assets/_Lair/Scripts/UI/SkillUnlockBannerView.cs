using System.Collections;
using ChvjUnityInfra;
using UnityEngine;

namespace Lair.UI
{
    //# 컨트롤러가 배너 구체 대신 참조 — EditMode 모킹. (Rule 03 §5 — 페어 정의 같은 파일)
    public interface ISkillUnlockBanner
    {
        //# text 를 좌→중→우 1회 슬라이드 재생. 코루틴 완료 = 아웃 종료.
        IEnumerator PlayCo(string text);
        //# 즉시 화면 밖 숨김(초기/리셋).
        void HideImmediate();
    }

    //# Rule 03 §5 — UIArg 는 페어 UIBase 와 같은 파일.
    public class SkillUnlockBannerArg : UIArg { }

    //# 스킬 해금 컷인 배너. 독립 EUI 팝업(빌더 생성). 슬라이드 연출만 — 정지/쉐이크/큐는 컨트롤러.
    //# Rule 02 §6.1 — 위젯 private 소유, 외부엔 의도 API(PlayCo/HideImmediate) 만.
    public class SkillUnlockBannerView : UIBase, ISkillUnlockBanner
    {
        [SerializeField] private RectTransform _root;   //# 슬라이드 대상(가로 밴드)
        [SerializeField] private CHText _label;         //# "영웅의 '...' 스킬 해제"

        [SerializeField] private float _slideInDuration = 0.35f;
        [SerializeField] private float _holdDuration = 1.2f;
        [SerializeField] private float _slideOutDuration = 0.35f;
        [SerializeField] private float _offscreenX = 1300f;   //# 화면 밖 X 최소값(레퍼런스 1280 기준 floor). 실제는 밴드 실폭으로 보정 (기획서 §3.5/§6)

        public override void InitUI(UIArg arg) => HideImmediate();

        //# 모바일 종횡비에서 캔버스 실폭이 1280 초과면 풀폭 밴드가 _offscreenX(1300) 로는 안 빠짐 → 일부 잔존.
        //# 밴드 실폭(rect.width) 으로 보정해 해상도 무관 완전 퇴장 보장.
        private float OffscreenX()
        {
            float bandWidth = _root != null ? _root.rect.width : 0f;
            return Mathf.Max(_offscreenX, bandWidth);
        }

        public void HideImmediate()
        {
            if (_root == null)
                return;
            _root.anchoredPosition = new Vector2(-OffscreenX(), _root.anchoredPosition.y);
        }

        public IEnumerator PlayCo(string text)
        {
            if (_label != null)
                _label.SetText(text);

            float y = _root != null ? _root.anchoredPosition.y : 0f;
            float offscreenX = OffscreenX();

            //# 인 — 왼쪽밖 → 중앙
            yield return SlideCo(new Vector2(-offscreenX, y), new Vector2(0f, y), _slideInDuration);
            //# 홀드
            float t = 0f;
            while (t < _holdDuration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            //# 아웃 — 중앙 → 오른쪽밖
            yield return SlideCo(new Vector2(0f, y), new Vector2(offscreenX, y), _slideOutDuration);

            HideImmediate();
        }

        private IEnumerator SlideCo(Vector2 from, Vector2 to, float dur)
        {
            if (_root == null || dur <= 0f)
                yield break;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(t / dur));
                _root.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                yield return null;
            }
            _root.anchoredPosition = to;
        }
    }
}
