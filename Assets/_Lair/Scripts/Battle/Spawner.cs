using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 지속 스폰 — 씬에 사전 배치되는 컴포넌트 (Addressables 프리팹 아님 → Rule 12 예외).
    //# 한 판 동안 고정 주기로 출력 종 몬스터를 동시 출력 수만큼 스폰한다.
    //# 첫 스폰은 t=InitialDelay, 이후 t=InitialDelay+주기×n (§2.4 위상 오프셋).
    //# ISpawnerProgress — 쿨다운 진행도(0~1) 노출 (SpawnerStatusCell 매 프레임 폴링).
    //# ISpawnerOutputProvider — 출력 종 변경 + 동시 출력 수 노출 (SpawnerBody · SpawnerStatusCell).
    public class Spawner : MonoBehaviour, ISpawnerProgress, ISpawnerOutputProvider
    {
        //# === 인스펙터 직렬화 — 스타터 프리셋 (§5.3) ===
        [Tooltip("이 Spawner 가 스폰하는 몬스터 종 (초기값 — 융합 카드로 런타임 변경됨)")]
        [SerializeField] private EMonster _outputType = EMonster.Wisp;
        [Tooltip("스폰 간격 (초)")]
        [SerializeField] private float _spawnPeriod = 9f;
        [Tooltip("첫 스폰까지 대기 (초) — 위상 오프셋")]
        [SerializeField] private float _initialDelay = 0f;
        //# Spawner 본체가 맵 밖에 있을 때 실제 스폰 지점을 분리 지정.
        //# null 이면 transform.position 을 fallback 으로 사용 (기존 동작 보전).
        [Tooltip("실제 몬스터 스폰 위치. null이면 Spawner transform.position 사용")]
        [SerializeField] private Transform _spawnPoint;

        //# === 런타임 내부 상태 (직렬화 안 함) ===
        //# 현재 출력 종 — 융합 카드(ReplaceSpawnerOutput)로 변경.
        private EMonster _currentType;
        //# 동시 출력 수 — 기본 1, 추가소환 카드(IncrementSpawnerOutput)로 +1. Spawner 슬롯에 영구 귀속.
        private int _outputCount = 1;
        //# 경과 누적 타이머. 첫 발사 전엔 InitialDelay 까지, 첫 발사 후엔 Period 마다 리셋.
        private float _timer;
        //# 첫 발사 완료 여부 — 첫 발사는 t=InitialDelay, 이후는 매 Period (§2.4).
        private bool _firstSpawnDone;

        private ISpawnerHost _host;

        //# 현재 출력 종 — IBattleContext 카드 API 가 매칭/변경에 사용.
        public EMonster CurrentType => _currentType;

        //# ISpawnerOutputProvider — 동시 출력 수. VM 이 AttachSpawners 시점에 직접 폴링.
        public int OutputCount => _outputCount;

        //# ISpawnerProgress 구현 — SpawnerStatusCell 이 매 프레임 폴링.
        //# 초기 지연 국면(firstSpawnDone==false): 0f 고정.
        //# 주기 국면: _timer / _spawnPeriod 클램프 [0, 1].
        public float Progress
        {
            get
            {
                if (!_firstSpawnDone) return 0f;
                if (_spawnPeriod <= 0f) return 1f;
                return Mathf.Clamp01(_timer / _spawnPeriod);
            }
        }

        //# ISpawnerOutputProvider 구현 — SpawnerBody 가 구독.
        public event System.Action<EMonster> OnOutputTypeChanged;

        //# ISpawnerOutputProvider 구현 — VM 이 IncrementOutput 발생 시 구독해 셀 갱신.
        //# OnEnable 시점엔 발행 안 함 — VM 의 AttachSpawners 가 OutputCount 를 직접 폴링한다.
        public event System.Action<int> OnOutputCountChanged;

        //# 풀 재사용은 없지만(씬 정적 오브젝트) 씬 재진입 시 상태 초기화 일관성 유지 (Rule 12 정신).
        private void OnEnable()
        {
            _currentType = _outputType;
            _outputCount = 1;
            //# 타이머 0 시작 — 첫 발사 전이라 _timer >= InitialDelay 도달 시점이 첫 발사 (t=InitialDelay).
            _timer = 0f;
            _firstSpawnDone = false;
            //# 초기 틴트 설정을 위해 OnEnable 에서도 이벤트 발행 — SpawnerBody 가 초기 색상 수신.
            OnOutputTypeChanged?.Invoke(_currentType);
        }

        //# BattleController 가 수집 시 1회 주입 (Rule 03 — 인터페이스 주입, 싱글톤 직접 호출 회피).
        public void Bind(ISpawnerHost host) => _host = host;

        //# BattleController 가 매 프레임 호출 — Update 직접 사용 대신 호스트가 구동 시점을 통제.
        //# Pause 중엔 호스트가 dt=0 또는 미호출로 자연 정지.
        public void Tick(float dt)
        {
            if (_host == null) return;
            _timer += dt;

            if (_firstSpawnDone == false)
            {
                //# 첫 발사 — t=InitialDelay 도달 시 1회. 이후 주기 발사로 전환 (§2.4).
                if (_timer < _initialDelay) return;
                _firstSpawnDone = true;
                //# 첫 발사를 t=InitialDelay 기준으로 보고, 초과분만 남겨 다음 주기 위상 유지.
                _timer -= _initialDelay;
            }
            else
            {
                //# 주기 발사 — t=InitialDelay+주기×n. 한 주기 경과 시 1회.
                if (_timer < _spawnPeriod) return;
                //# 한 주기 경과 — 다음 주기로. (누적 dt 가 커도 1주기씩만 소모 — 폭주 스폰 방지)
                _timer -= _spawnPeriod;
            }

            //# 캡 검사는 사이클 단위 (§4.3) — 호스트가 캡 이상이면 사이클 전량 skip.
            //# _spawnPoint 미할당(null)이면 transform.position fallback — Spawner 본체와 스폰 위치 분리 지원.
            Vector3 spawnPos = _spawnPoint != null ? _spawnPoint.position : transform.position;
            _host.SpawnFromSpawner(_currentType, spawnPos, _outputCount);
        }

        //# 추가소환 카드 — 동시 출력 +1 (Spawner 슬롯에 영구 귀속, §3.2).
        //# 호출 시 OnOutputCountChanged 발행 — VM 셀이 ×N 갱신.
        public void IncrementOutput()
        {
            _outputCount++;
            OnOutputCountChanged?.Invoke(_outputCount);
        }

        //# 융합 카드 — 출력 종 영구 변경. 동시 출력 수는 유지 (§3.5 케이스 3).
        //# 변경 후 OnOutputTypeChanged 발행 — SpawnerBody 가 틴트 즉시 갱신.
        public void ReplaceOutput(EMonster to)
        {
            _currentType = to;
            OnOutputTypeChanged?.Invoke(_currentType);
        }
    }
}
