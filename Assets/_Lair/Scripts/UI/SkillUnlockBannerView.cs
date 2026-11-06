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

        //# 전투 내내 상주하는 핸들 — ESC 로 닫히면 이후 컷인이 비활성 객체를 노린다.
        public override bool CanCloseByEsc => false;

        public override void InitUI(UIArg arg) => HideImmediate();

        [SerializeField] private float _offscreenMargin = 100f;   //# 캔버스 끝에서 밴드가 더 벗어나는 여유(px, 부모 로컬)

        //# 부모 로컬(앵커 기준점 원점) 캔버스 좌우 끝 + 밴드 폭 + 마진 → 밴드(pivot 0.5) 중심이 놓일 완전 퇴장 X.
        public static void ComputeHiddenX(float canvasLeft, float canvasRight, float bandWidth, float margin, out float leftX, out float rightX)
        {
            leftX = canvasLeft - bandWidth * 0.5f - margin;
            rightX = canvasRight + bandWidth * 0.5f + margin;
        }

        //# 캔버스 월드 좌우 끝을 _root 부모 로컬로 변환 — PopupRoot(SafeArea) 중심이 캔버스 중심과 어긋나도 자동 반영.
        private void GetHiddenX(out float leftX, out float rightX)
        {
            float bandWidth = _root.rect.width;
            float canvasLeft = -bandWidth * 0.5f;
            float canvasRight = bandWidth * 0.5f;
            RectTransform parent = _root.parent as RectTransform;
            RectTransform canvasRect = _root.root as RectTransform;
            if (parent != null && canvasRect != null)
            {
                Vector3[] corners = new Vector3[4];
                canvasRect.GetWorldCorners(corners);
                //# 앵커 기준점(부모 rect 에서 앵커 중앙) 원점으로 평행이동 — anchoredPosition 과 같은 좌표계.
                float anchorRefX = parent.rect.xMin + (_root.anchorMin.x + _root.anchorMax.x) * 0.5f * parent.rect.width;
                canvasLeft = parent.InverseTransformPoint(corners[0]).x - anchorRefX;
                canvasRight = parent.InverseTransformPoint(corners[2]).x - anchorRefX;
            }
            ComputeHiddenX(canvasLeft, canvasRight, bandWidth, Mathf.Max(_offscreenMargin, 0f), out leftX, out rightX);
            leftX = Mathf.Min(leftX, -_offscreenX);
            rightX = Mathf.Max(rightX, _offscreenX);
        }

        public void HideImmediate()
        {
            if (_root == null)
                return;
            GetHiddenX(out float leftX, out float rightX);
            _root.anchoredPosition = new Vector2(leftX, _root.anchoredPosition.y);
        }

        public IEnumerator PlayCo(string text)
        {
            if (_label != null)
                _label.SetText(text);

            float y = _root != null ? _root.anchoredPosition.y : 0f;
            float leftX = 0f;
            float rightX = 0f;
            if (_root != null)
            {
                GetHiddenX(out leftX, out rightX);
            }

            //# 인 — 왼쪽밖 → 중앙
            yield return SlideCo(new Vector2(leftX, y), new Vector2(0f, y), _slideInDuration);
            //# 홀드
            float t = 0f;
            while (t < _holdDuration)
            {
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            //# 아웃 — 중앙 → 오른쪽밖
            yield return SlideCo(new Vector2(0f, y), new Vector2(rightX, y), _slideOutDuration);

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
