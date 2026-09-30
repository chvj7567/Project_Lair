using System.Collections;
using ChvjUnityInfra;
using TMPro;
using UnityEngine;

namespace Lair.Character
{
    //# 월드스페이스 데미지 숫자. Rule 03 §3 — TMP 엔 CHText 동반(프리팹에 부착).
    //# 12fps 계단 부상(rise) + 4단 알파 후 자동 CHMPool.Push. 트윈 없음 → 코루틴.
    [RequireComponent(typeof(CHPoolable))]
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] private TMP_Text _text;        //# 프리팹 인스펙터 참조 (CHText 동반)
        [SerializeField] private float _rise = 1.2f;     //# 기획서 §4 — 부상 1.2 유닛
        [SerializeField] private float _duration = 0.7f; //# 기획서 §4 — 0.7초

        //# 명도 분기 외곽선 색 (기획서 §4.2). 임계 L=128 (Rec.601, 0~255 스케일).
        private static readonly Color OutlineDark = new Color(0.102f, 0.102f, 0.102f, 1f);  //# #1A1A1A
        private static readonly Color OutlineLight = Color.white;                            //# #FFFFFF
        private const float OutlineThreshold = 128f;

        private Coroutine _co;
        private Camera _cam;

        private void OnEnable() => _cam = Camera.main;

        private void OnDisable()
        {
            //# 풀 재사용 리셋 — 진행 중 코루틴 정리.
            if (_co != null)
            {
                StopCoroutine(_co);
                _co = null;
            }
        }

        public void Play(Vector3 worldPos, int amount, Color color)
        {
            transform.position = worldPos;
            if (_text != null)
            {
                _text.text = amount.ToString();
                _text.color = new Color(color.r, color.g, color.b, 1f);
                ApplyOutline(color);
            }
            if (_co != null)
                StopCoroutine(_co);
            _co = StartCoroutine(PlayCo(worldPos, color));
        }

        //# fill 명도로 외곽선 색 결정. fontMaterial(인스턴스) 에만 써서 동시 활성 팝업 간 간섭 방지.
        //# _text.outlineColor(공유 머티리얼 경유) 는 쓰지 않는다 — 풀 동시 활성 팝업이 서로 덮는다.
        private void ApplyOutline(Color fill)
        {
            float l = (0.299f * fill.r + 0.587f * fill.g + 0.114f * fill.b) * 255f;
            Color outline = l >= OutlineThreshold ? OutlineDark : OutlineLight;
            Material fontMat = _text.fontMaterial;   //# 접근 시 인스턴스화 → 팝업별 독립
            if (fontMat != null)
                fontMat.SetColor(ShaderUtilities.ID_OutlineColor, outline);
        }

        //# 도트 연출 시간 함수(순수) — 12fps 프레임 지수 n = floor(t × 12). 기획서 fx-2d-conversion §6.3.
        private const float StepFps = 12f;
        private const int RiseFrames = 7;

        public static int StepIndex(float t)
        {
            if (t <= 0f)
                return 0;
            return (int)Mathf.Floor(t * StepFps + 0.0001f);
        }

        //# 부상 비율 0~1 — 프레임 단위 ease-out(n=7 이후 1.0 정지).
        public static float RiseRatio(int n)
        {
            float k = Mathf.Min(Mathf.Max(n, 0) / (float)RiseFrames, 1f);
            float inv = 1f - k;
            return 1f - inv * inv * inv;
        }

        //# 알파 4단 계단 — n≤4 1.00 / 5 0.75 / 6 0.50 / 7 0.25 / ≥8 0.
        public static float StepAlpha(int n)
        {
            if (n <= 4)
                return 1f;
            if (n >= 8)
                return 0f;
            return 1f - (n - 4) * 0.25f;
        }

        private IEnumerator PlayCo(Vector3 start, Color color)
        {
            float t = 0f;
            int lastStep = -1;
            while (t < _duration)
            {
                t += Time.deltaTime;
                int n = StepIndex(t);
                if (n != lastStep)
                {
                    //# 12fps 계단 — 프레임이 바뀔 때만 위치·알파 갱신.
                    lastStep = n;
                    transform.position = start + Vector3.up * (_rise * RiseRatio(n));
                    if (_text != null)
                        _text.color = new Color(color.r, color.g, color.b, StepAlpha(n));
                }
                if (_cam != null)
                    //# 카메라 회전 복사 → 쿼드가 카메라 평면과 평행 = 화면상 항상 똑바로(스크린-정렬 빌보드).
                    //# LookRotation(위치 방향) 은 카메라 피치를 받아 화면상 기울어진다.
                    transform.rotation = _cam.transform.rotation;
                yield return null;
            }
            _co = null;
            CHPoolable self = GetComponent<CHPoolable>();
            if (self != null)
                CHMPool.Instance.Push(self);
        }
    }
}
