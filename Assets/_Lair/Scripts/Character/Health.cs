using System;
using UnityEngine;

namespace Lair.Character
{
    //# IHealth 구현체. 본 슬라이스 한정 POCO + MonoBehaviour 양립.
    //# Unity 컴포넌트로 사용 시: GameObject 에 AddComponent.
    //# 테스트에서는 new Health() 직접 생성 후 SetMax 로 초기화.
    public class Health : MonoBehaviour, IHealth
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

        //# 카드 리뉴얼 v0.6 [B1] — 일시 HP 배율 오버레이. MonsterBuffService 가 매 tick 재설정.
        //# 기본 1.0. 변경 시 CurrentHp/MaxHp 비율 보존 (예: 80% 상태에서 1→2 변경 → CurrentHp 도 2배).
        //# buff 종료 후 Tick 에서 1.0 으로 복원되면 CurrentHp 도 1/2 로 축소 (최소 1 보존).
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

        public event Action<int, int> OnChanged;
        public event Action OnDied;

        //# MonoBehaviour 라이프사이클 — 인스펙터 _max 로 초기화.
        private void Awake()
        {
            Current = _max;
        }

        //# CHMPool 재사용 시 사망 상태로 풀에서 빠져나온 인스턴스를 복원.
        //# Pop → SetActive(true) → OnEnable. Current > 0 이면 그대로 유지.
        //# B3 — 오버레이 배율도 풀 재사용 시 잔존 방지 위해 1.0 리셋.
        //# [B1] HpMaxScale 도 풀 재사용 시 1.0 리셋 (setter 우회: 직접 필드 — 비율 보존 로직 불요).
        private void OnEnable()
        {
            if (Current <= 0) Current = _max;
            DamageTakenScale = 1f;
            _hpMaxScale = 1f;
        }

        public void TakeDamage(int amount)
        {
            if (IsAlive == false) return;
            //# B3 — 받는 데미지 배율 적용
            amount = Mathf.RoundToInt(amount * DamageTakenScale);
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
