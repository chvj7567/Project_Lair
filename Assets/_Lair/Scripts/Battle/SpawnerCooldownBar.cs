using UnityEngine;
using UnityEngine.UI;

namespace Lair.Battle
{
    //# Spawner 위에 부착되는 쿨다운 진행 바 컴포넌트.
    //# WorldSpace Canvas 래퍼(CooldownBarWrapper)에 부착. 자식 HpBar.prefab 의 Fill 을 빌더가 주입.
    //# 매 프레임 ISpawnerProgress.Progress 를 읽어 fillAmount + 색상을 갱신한다.
    //# LateUpdate 에서 Camera.main 방향으로 빌보드 회전 — MonsterHpBar 와 동일 패턴.
    //# Rule 06 — GetComponentInParent<ISpawnerProgress>() 로 상위 인터페이스만 참조.
    //# Rule 11 예외 — WorldSpace 디버그/진행 바는 raw Image 직접 사용 허용 (MonsterHpBar 선례).
    public class SpawnerCooldownBar : MonoBehaviour
    {
        [SerializeField] private Image _fill;   //# HpBar.prefab 내부 Background/Fill Image — 빌더가 주입.

        //# 2단계 색상 threshold (기획서 §3.2).
        private const float WarmThreshold = 0.7f;

        //# Fill Cool 색 (#60A5FA).
        private static readonly Color CoolColor = new Color(0.376f, 0.647f, 0.980f, 1f);
        //# Fill Warm 색 (#F97316).
        private static readonly Color WarmColor = new Color(0.976f, 0.451f, 0.086f, 1f);

        private ISpawnerProgress _host;
        private Transform _cam;

        //# Rule 06 — 상위 Spawner 를 인터페이스로 탐색. 카메라 재캐시.
        private void OnEnable()
        {
            var mainCam = Camera.main;
            _cam = mainCam != null ? mainCam.transform : null;

            if (_host == null)
                _host = GetComponentInParent<ISpawnerProgress>();
        }

        //# 매 프레임 진행도 읽기 → fillAmount + 색상 갱신.
        private void Update()
        {
            if (_host == null || _fill == null) return;

            float progress = _host.Progress;
            _fill.fillAmount = progress;
            _fill.color = progress < WarmThreshold ? CoolColor : WarmColor;
        }

        //# 빌보드 — 바 Canvas 가 카메라 정면을 향하게 (MonsterHpBar.LateUpdate 와 동일 패턴).
        private void LateUpdate()
        {
            if (_cam != null) transform.rotation = _cam.rotation;
        }
    }
}
