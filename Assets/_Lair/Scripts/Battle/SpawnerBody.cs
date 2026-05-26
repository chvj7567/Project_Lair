using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# Spawner 본체 Cylinder 디스크에 부착. 출력 종 변경 시 머티리얼 교체로 틴트를 갱신한다.
    //# 빌더(LairSpawnerVisualBuilder)가 _renderer 와 _materials 를 주입.
    //# Rule 06 — 상위 ISpawnerOutputProvider 인터페이스로만 Spawner 를 참조.
    //# Rule 03 — GetComponentInParent<ISpawnerOutputProvider>() 로 느슨한 결합.
    public class SpawnerBody : MonoBehaviour
    {
        [SerializeField] private Renderer _renderer;
        //# 6종 머티리얼 배열 — 빌더가 EMonster 순서(0=Wisp, 1=Wraith …)대로 주입.
        [SerializeField] private Material[] _materials;

        private ISpawnerOutputProvider _provider;

        //# Rule 06 — 구체 클래스 Spawner 가 아닌 인터페이스로 상위 참조.
        private void OnEnable()
        {
            _provider = GetComponentInParent<ISpawnerOutputProvider>();
            if (_provider == null) return;

            _provider.OnOutputTypeChanged += HandleTypeChanged;
            //# OnEnable 순서 비보장 대응: 이벤트 구독 직후 현재 값으로 즉시 동기화.
            HandleTypeChanged(_provider.CurrentType);
        }

        private void OnDisable()
        {
            if (_provider != null)
                _provider.OnOutputTypeChanged -= HandleTypeChanged;
        }

        private void HandleTypeChanged(EMonster type)
        {
            if (_renderer == null || _materials == null) return;

            int index = (int)type;
            if (index >= 0 && index < _materials.Length && _materials[index] != null)
                _renderer.sharedMaterial = _materials[index];
        }
    }
}
