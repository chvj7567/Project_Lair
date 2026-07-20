using Lair.Battle;
using UnityEngine;

namespace Lair.Character
{
    //# 영웅 zone 진입 단계 드라이버. AutoCombatAI 가 비활성화된 동안 영웅을 BattleZone.Center 로 이동시킨다.
    //# 도달 시 BattleZone.NotifyHeroReachedCenter() 호출 후 자기 비활성화. AutoCombatAI 활성화는 BattleController 가 담당.
    [RequireComponent(typeof(LairCharacter))]
    public class HeroEntryDriver : MonoBehaviour
    {
        //# 도달 임계값 (m). spec §10 — 0.5m.
        [SerializeField] private float _arriveThreshold = 0.5f;

        //# 스폰 게이트 fallback (hero-animation-timing-sync §1.3·§7.6). spawn 1.33s × 1.35 마진.
        //# OnSpawnAnimEnd 이벤트 유실 시 영구 봉인 방지 — enabled 후 이 시간 지나면 게이트 강제 open.
        [SerializeField] private float _spawnGateFallback = 1.8f;

        private BattleZone _zone;
        private IMover _mover;
        private IRotator _rotator;
        private IHealth _health;
        private bool _notified;

        //# march 게이트 (§1.2). 기본 닫힘 — enabled 여도 open 전엔 Stop 유지(march 안 함).
        //# OnSpawnAnimEnd relay 또는 fallback 으로 open. 풀 재사용 대비 OnEnable 리셋.
        private bool _marchGateOpen;
        private float _enabledTime;

        public void Bind(BattleZone zone)
        {
            _zone = zone;
            _notified = false;
        }

        private void Awake()
        {
            LairCharacter character = GetComponent<LairCharacter>();
            _mover = character.Get<IMover>();
            _rotator = character.Get<IRotator>();
            _health = character.Get<IHealth>();
        }

        //# 풀 재사용 + enabled 토글 시 게이트 잔존 방지 — 매 활성화마다 닫힘으로 리셋.
        private void OnEnable()
        {
            _marchGateOpen = false;
            _enabledTime = Time.time;
        }

        //# OnSpawnAnimEnd relay(§B4) 또는 fallback 이 호출 — march 시작 허용.
        public void OpenMarchGate() => _marchGateOpen = true;

        private void Update()
        {
            if (_zone == null) return;
            if (_notified) return;
            //# 영웅 사망 — march 중단 (현실적으로 발생 안 함, 안전망).
            if (_health != null && _health.IsAlive == false)
            {
                _mover?.Stop();
                return;
            }

            //# 게이트 닫힘 — spawn 모션 재생 중. fallback 초과 시 강제 open(이벤트 유실 안전망).
            if (_marchGateOpen == false)
            {
                if (Time.time - _enabledTime >= _spawnGateFallback)
                {
                    _marchGateOpen = true;
                }
                else
                {
                    _mover?.Stop();
                    return;
                }
            }

            Vector3 center = _zone.Center;
            Vector3 dir = center - transform.position;
            //# Y 평면 거리만 — SimpleMover 가 Y=0 고정이라 일관성 유지.
            dir.y = 0f;
            float dist = dir.magnitude;

            if (dist <= _arriveThreshold)
            {
                _mover?.Stop();
                _notified = true;
                _zone.NotifyHeroReachedCenter();
                enabled = false;   //# 1회 동작 후 비활성화. BattleController 가 AutoCombatAI.enabled = true 로 전환.
                return;
            }

            _rotator?.FaceDirection(dir);
            _mover?.MoveTo(center);
        }
    }
}
