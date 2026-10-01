using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 중앙(transform.position) 기준 원형 스포너 배치 설정. 실제 생성은 에디터(CircularSpawnerArrangerEditor).
    public class CircularSpawnerArranger : MonoBehaviour
    {
        //# X 반축(원 배치면 반지름). Z 반축 _radiusZ 가 0 이하면 _radius 와 같은 원.
        [SerializeField] private float _radius = 13f;
        [SerializeField] private float _radiusZ = 0f;
        [SerializeField] private EMonster[] _monsters = System.Array.Empty<EMonster>();
        [SerializeField] private float _startAngleDeg = 90f;

        public float Radius => _radius;
        public float RadiusZ => _radiusZ > 0f ? _radiusZ : _radius;
        public IReadOnlyList<EMonster> Monsters => _monsters;
        public float StartAngleDeg => _startAngleDeg;

        //# N개 균등 분배 각 간격. count<=0 이면 0.
        public static float AngleStep(int count) => count <= 0 ? 0f : 360f / count;

        //# 탑다운 평면(XZ) 원주 위 좌표. +Z 가 angleDeg=90 기준 (cos→x, sin→z).
        public static Vector3 PositionOnCircle(Vector3 center, float radius, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(center.x + radius * Mathf.Cos(rad), center.y, center.z + radius * Mathf.Sin(rad));
        }

        //# 탑다운 평면(XZ) 타원 위 좌표 — X 반축 radiusX, Z 반축 radiusZ. 반축이 같으면 PositionOnCircle 과 동일.
        public static Vector3 PositionOnEllipse(Vector3 center, float radiusX, float radiusZ, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return new Vector3(center.x + radiusX * Mathf.Cos(rad), center.y, center.z + radiusZ * Mathf.Sin(rad));
        }

        //# count 개 타원 균등 각 배치 좌표.
        public static Vector3[] ComputeEllipsePositions(Vector3 center, float radiusX, float radiusZ, int count, float startDeg)
        {
            if (count <= 0)
                return new Vector3[0];

            Vector3[] result = new Vector3[count];
            float step = AngleStep(count);
            for (int i = 0; i < count; ++i)
                result[i] = PositionOnEllipse(center, radiusX, radiusZ, startDeg + step * i);
            return result;
        }

        //# count 개 균등 배치 좌표. startDeg 부터 360/count 씩. count<=0 이면 빈 배열.
        public static Vector3[] ComputePositions(Vector3 center, float radius, int count, float startDeg)
        {
            if (count <= 0)
                return new Vector3[0];

            Vector3[] result = new Vector3[count];
            float step = AngleStep(count);
            for (int i = 0; i < count; ++i)
                result[i] = PositionOnCircle(center, radius, startDeg + step * i);
            return result;
        }
    }
}
