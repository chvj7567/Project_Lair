using UnityEngine;

namespace Lair.Stage
{
    //# 궤도 점 링 — 정적 자식 N개(동적 생성 금지)를 매 프레임 타원 궤도에 도트 단위로 배치(§7.2).
    public class DotOrbitRing : MonoBehaviour
    {
        [SerializeField] private SpriteRenderer[] _dots;
        [SerializeField] private float _rx = 38f;
        [SerializeField] private float _ry = 10f;
        [SerializeField] private float _omega = 0.5f;
        [SerializeField] private float _phase;
        [SerializeField] private Color _color = Color.white;

        //# i 번째 점의 도트 오프셋(화면 아래 +y). a = 위상 + ω·t + i·2π/N, 반올림 = floor(v + 0.5)
        public static Vector2Int DotOffset(int i, int count, float t, float omega, float phase, float rx, float ry)
        {
            if (count <= 0)
                return Vector2Int.zero;
            float a = phase + omega * t + i * Mathf.PI * 2f / count;
            return new Vector2Int(Mathf.FloorToInt(Mathf.Cos(a) * rx + 0.5f), Mathf.FloorToInt(Mathf.Sin(a) * ry + 0.5f));
        }

        private void Awake()
        {
            if (_dots == null)
                return;
            foreach (SpriteRenderer dot in _dots)
            {
                if (dot != null)
                {
                    dot.color = _color;
                }
            }
        }

        private void Update()
        {
            if (_dots == null)
                return;
            int n = _dots.Length;
            for (int i = 0; i < n; i++)
            {
                if (_dots[i] == null)
                    continue;
                Vector2Int o = DotOffset(i, n, Time.time, _omega, _phase, _rx, _ry);
                _dots[i].transform.localPosition = new Vector3(o.x / DotStageMapping.Ppu, -o.y / DotStageMapping.Ppu, 0f);
            }
        }
    }
}
