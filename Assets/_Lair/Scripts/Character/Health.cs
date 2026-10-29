using System;
using UnityEngine;

namespace Lair.Character
{
    //# IHealth 구현체. 본 슬라이스 한정 POCO + MonoBehaviour 양립.
    //# Unity 컴포넌트로 사용 시: GameObject 에 AddComponent.
    public class Health : MonoBehaviour, IHealth, IInvulnerable
    {
        [SerializeField] private int _max = 100;

        public int Max => _max;
        public int Current { get; private set; }
        //# 카드 리뉴얼 v0.6 [B1] — Effective Max (HpMaxScale 적용). 외부 UI/AI 가 영웅의 "실효 Max" 를 볼 때 사용.
        public int EffectiveMaxHp => Mathf.Max(1, Mathf.RoundToInt(_max * HpMaxScale));
        //# Ratio 는 EffectiveMaxHp 기준 — HpMaxScale=2 상태에서 Current 가 base max 1배 이하면 0.5 등.
        public float Ratio => EffectiveMaxHp > 0 ? (float)Current / EffectiveMaxHp : 0f;
        public bool IsAlive => Current > 0;

        //# B3 — 받는 데미지 배율 오버레이. MonsterBuffService 가 매 tick 설정. 기본 1.0.
        public float DamageTakenScale { get; set; } = 1f;

        //# 육성 각성(monster-growth-currencies §3.2) — 받는 피해·넉백 배율. DamageTakenScale(버프 틱이 1 로 되돌리는 값)과 분리해 곱으로 합성한다.
        public float GrowthDamageTakenMul { get; private set; } = 1f;
        public float KnockbackTakenMul { get; private set; } = 1f;

        //# 둥지 숙련 Lv3 스폰 보호 — 종료 시각(Time.time)까지 받는 피해 x _spawnGuardMul (§3.3).
        private float _spawnGuardMul = 1f;
        private float _spawnGuardUntil = float.NegativeInfinity;

        //# 스폰·소급마다 절대 대입 (풀 재사용 복리 없음). 영웅은 호출하지 않아 1 유지.
        public void ConfigureGrowthTaken(float damageTakenMul, float knockbackTakenMul)
        {
            GrowthDamageTakenMul = damageTakenMul;
            KnockbackTakenMul = knockbackTakenMul;
        }

        //# 환령 특성 "잔상" — 남은 회피 횟수만큼 피해 인스턴스를 무효화하고 OnDodged 발행 (monster-growth-currencies §3.4).
        private int _dodgeCharges;
        public event Action OnDodged;

        public void GrantDodges(int count)
        {
            _dodgeCharges = Mathf.Max(0, count);
        }

        public void GrantSpawnGuard(float damageTakenMul, float seconds)
        {
            if (seconds <= 0f)
                return;
            _spawnGuardMul = damageTakenMul;
            _spawnGuardUntil = Time.time + seconds;
        }

        //# 지금 받는 피해에 곱할 전체 배율 — 버프 오버레이 x 각성 x (스폰 보호 중이면) 보호 배율.
        public float EffectiveDamageTakenMul => DamageTakenScale * GrowthDamageTakenMul * (Time.time < _spawnGuardUntil ? _spawnGuardMul : 1f);

        //# 카드 리뉴얼 v0.6 [B1] — 일시 HP 배율 오버레이. MonsterBuffService 가 매 tick 재설정.
        //# 기본 1.0. 변경 시 CurrentHp/MaxHp 비율 보존 (예: 80% 상태에서 1→2 변경 → CurrentHp 도 2배).
        private float _hpMaxScale = 1f;
        public float HpMaxScale
        {
            get => _hpMaxScale;
            set
            {
                if (Mathf.Approximately(_hpMaxScale, value)) return;
                float prevScale = _hpMaxScale;
                _hpMaxScale = value;
                //# 비율 보존: CurrentHp 도 같은 비율로 스케일. 살아있는 경우만 (사망 후 부활 방지).
                if (Current > 0 && prevScale > 0f)
                {
                    float ratio = value / prevScale;
                    Current = Mathf.Max(1, Mathf.RoundToInt(Current * ratio));
                }
                //# EffectiveMaxHp 가 갱신됐으므로 UI 통지.
                OnChanged?.Invoke(Current, EffectiveMaxHp);
            }
        }

        //# 무적 종료 시각(Time.time 기준) — 암살자 슬립/그림자 습격(chapter-3-hero §3.3·§4). 보호막 배율(DamageTakenScale)과 독립이라 틱 재설정에 안 지워진다.
        private float _invulnerableUntil = float.NegativeInfinity;
        public bool IsInvulnerable => Time.time < _invulnerableUntil;

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        //# seconds 동안 데미지 무시. 이미 더 긴 무적이 있으면 유지.
        public void GrantInvulnerability(float seconds)
        {
            if (seconds <= 0f)
                return;
            _invulnerableUntil = Mathf.Max(_invulnerableUntil, Time.time + seconds);
        }

        //# MonoBehaviour 라이프사이클 — 인스펙터 _max 로 초기화.
        private void Awake()
        {
            Current = _max;
        }

        //# CHMPool 재사용 시 사망 상태로 풀에서 빠져나온 인스턴스를 복원.
        //# Pop → SetActive(true) → OnEnable. Current > 0 이면 그대로 유지.
        private void OnEnable()
        {
            if (Current <= 0) Current = _max;
            DamageTakenScale = 1f;
            GrowthDamageTakenMul = 1f;
            KnockbackTakenMul = 1f;
            _spawnGuardMul = 1f;
            _spawnGuardUntil = float.NegativeInfinity;
            _dodgeCharges = 0;
            _hpMaxScale = 1f;
            _invulnerableUntil = float.NegativeInfinity;
        }

        public void TakeDamage(int amount)
        {
            if (IsAlive == false) return;
            if (IsInvulnerable)
                return;
            //# 잔상 회피 — 피해량과 무관하게 1회 무효(E-18).
            if (_dodgeCharges > 0)
            {
                _dodgeCharges--;
                OnDodged?.Invoke();
                return;
            }
            //# B3 — 받는 데미지 배율 적용 (버프 오버레이 x 각성 x 스폰 보호)
            amount = Mathf.RoundToInt(amount * EffectiveDamageTakenMul);
            int next = Mathf.Max(0, Current - amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, EffectiveMaxHp);
            if (Current == 0) OnDied?.Invoke();
        }

        //# B3 — 피의 갈증 카드. EffectiveMaxHp 초과 불가 — HpMaxScale=2 상태에선 최대 2배 회복 가능.
        public void Heal(int amount)
        {
            if (IsAlive == false) return;
            int next = Mathf.Min(EffectiveMaxHp, Current + amount);
            if (next == Current) return;
            Current = next;
            OnChanged?.Invoke(Current, EffectiveMaxHp);
        }

        public void SetMax(int max, bool resetCurrent = true)
        {
            _max = max;
            if (resetCurrent) Current = max;
            OnChanged?.Invoke(Current, EffectiveMaxHp);
        }
    }
}
