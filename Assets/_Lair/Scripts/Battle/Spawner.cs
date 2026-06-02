using Lair.Data;
using UnityEngine;

namespace Lair.Battle
{
    //# 지속 스폰 — 씬에 사전 배치되는 컴포넌트 (Addressables 프리팹 아님 → Rule 12 예외).
    //# 한 판 동안 고정 주기로 출력 종 몬스터를 동시 출력 수만큼 스폰한다.
    public class Spawner : MonoBehaviour, ISpawnerProgress, ISpawnerOutputProvider
    {
        //# === 인스펙터 직렬화 — 스타터 프리셋 (§5.3) ===
        [Tooltip("이 Spawner 가 스폰하는 몬스터 종 (초기값 — 융합 카드로 런타임 변경됨)")]
        [SerializeField] private EMonster _outputType = EMonster.Wisp;
        [Tooltip("스폰 간격 (초)")]
        [SerializeField] private float _spawnPeriod = 9f;
        //# (no-op) 첫 스폰은 전투 시작 직후(첫 Tick) 즉시 발사 — 이 필드는 더 이상 위상 오프셋에 쓰지 않는다.
        //# 씬 직렬화 churn 방지를 위해 필드는 보존하나 Tick 에서 미사용.
        [Tooltip("(미사용) 첫 스폰은 첫 Tick 에 즉시 발사된다")]
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
        //# 경과 누적 타이머. 첫 발사 후엔 Period 마다 리셋.
        private float _timer;
        //# 첫 발사 완료 여부 — 첫 발사는 첫 Tick(t≈0) 에 즉시, 이후는 매 Period.
        private bool _firstSpawnDone;

        private ISpawnerHost _host;
        //# (no-op) Bind 시 주입되나 스폰 위치 산정엔 미사용 — 스폰 위치는 _spawnPoint → transform.position.
        //# Bind(host, zone) 시그니처 보존을 위해 필드만 유지.
        private BattleZone _zone;

        //# 현재 출력 종 — IBattleContext 카드 API 가 매칭/변경에 사용.
        public EMonster CurrentType => _currentType;

        //# ISpawnerOutputProvider — 동시 출력 수. VM 이 AttachSpawners 시점에 직접 폴링.
        public int OutputCount => _outputCount;

        //# ISpawnerProgress 구현 — SpawnerStatusCell 이 매 프레임 폴링.
        //# 초기 지연 국면(firstSpawnDone==false): 0f 고정.
        public float Progress
        {
            get
            {
                if (_firstSpawnDone == false) return 0f;
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
            //# 타이머 0 시작 — 첫 Tick 에서 _firstSpawnDone==false 이므로 즉시 첫 발사.
            _timer = 0f;
            _firstSpawnDone = false;
            //# 초기 틴트 설정을 위해 OnEnable 에서도 이벤트 발행 — SpawnerBody 가 초기 색상 수신.
            OnOutputTypeChanged?.Invoke(_currentType);
        }

        //# BattleController 가 수집 시 1회 주입. zone 은 스폰 위치에 미사용 (시그니처 보존용).
        //# 스폰 위치는 항상 _spawnPoint → transform.position 순으로 산정.
        public void Bind(ISpawnerHost host, BattleZone zone)
        {
            _host = host;
            _zone = zone;
        }

        //# BattleController 가 매 프레임 호출 — Update 직접 사용 대신 호스트가 구동 시점을 통제.
        //# Pause 중엔 호스트가 dt=0 또는 미호출로 자연 정지.
        public void Tick(float dt)
        {
            if (_host == null) return;
            _timer += dt;

            if (_firstSpawnDone == false)
            {
                //# 첫 발사 — 전투 시작 직후 첫 Tick 에 즉시 1회 (_initialDelay 무시). 이후 주기 발사로 전환.
                _firstSpawnDone = true;
                //# 첫 발사 위상 기준점 — _timer 0 으로 맞춰 다음 발사가 정확히 _spawnPeriod 후가 되게.
                _timer = 0f;
            }
            else
            {
                //# 주기 발사 — 한 주기 경과 시 1회.
                if (_timer < _spawnPeriod) return;
                //# 한 주기 경과 — 다음 주기로. (누적 dt 가 커도 1주기씩만 소모 — 폭주 스폰 방지)
                _timer -= _spawnPeriod;
            }

            //# 스폰 위치 산정 후 호스트에 사이클 위임 (동시 캡 제거, spec §2.A — 호스트가 count 전량 스폰).
            //# 스폰 위치 — 각 스포너 자기 위치 우선: _spawnPoint > transform.position (zone 픽 미사용).
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

        //# 카드 리뉴얼 v0.6 — 모든 스포너 출력 +delta (Swarm Tier3). 가산 누적.
        //# delta < 1 일 때도 음수 누적 방지 위해 Max(1) 클램프.
        public void IncrementOutput(int delta)
        {
            if (delta <= 0) return;
            _outputCount += delta;
            OnOutputCountChanged?.Invoke(_outputCount);
        }

        //# 카드 리뉴얼 v0.6 — 스폰 주기 ×mul (SpawnerHaste 카드 / Swarm Tier2). 곱연산 누적.
        //# mul <= 0 입력은 무시 (안전 가드). 최소 주기 0.05s 클램프 — 폭주 스폰 방지.
        public void ScalePeriod(float mul)
        {
            if (mul <= 0f) return;
            _spawnPeriod = Mathf.Max(0.05f, _spawnPeriod * mul);
        }

        //# 카드 리뉴얼 v0.6 — 디버그 / 테스트용 read-only 노출. 곱연산 누적 검증.
        public float SpawnPeriod => _spawnPeriod;

        //# 융합 카드 — 출력 종 영구 변경. 동시 출력 수는 유지 (§3.5 케이스 3).
        //# 변경 후 OnOutputTypeChanged 발행 — SpawnerBody 가 틴트 즉시 갱신.
        public void ReplaceOutput(EMonster to)
        {
            _currentType = to;
            OnOutputTypeChanged?.Invoke(_currentType);
        }
    }
}
