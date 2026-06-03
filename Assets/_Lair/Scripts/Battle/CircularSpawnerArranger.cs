using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 중앙(transform.position) 기준 원형 스포너 배치 설정. 실제 생성은 에디터(CircularSpawnerArrangerEditor).
    public class CircularSpawnerArranger : MonoBehaviour
    {
        [SerializeField] private float _radius = 13f;
        [SerializeField] private EMonster[] _monsters = System.Array.Empty<EMonster>();
        [SerializeField] private float _startAngleDeg = 90f;

        public float Radius => _radius;
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
