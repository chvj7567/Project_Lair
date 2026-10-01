using UnityEngine;

namespace Lair.Stage
{
    //# 씬마다 1개 — 도트 조명 양자화 격자 원점(화면 중앙의 배경 도트)을 전역 셰이더 값으로 설정(§1.5 · §7.6).
    [ExecuteAlways]
    public class DotLightingSettings : MonoBehaviour
    {
        private static readonly int GridOriginId = Shader.PropertyToID("_DotGridOrigin");

        //# 배틀 (614,352), 마을·로딩 (240,135)
        [SerializeField] private Vector2 _gridOrigin = new Vector2(614f, 352f);

        private void Awake()
        {
            Shader.SetGlobalVector(GridOriginId, new Vector4(_gridOrigin.x, _gridOrigin.y, 0f, 0f));
        }
    }
}
