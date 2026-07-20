using System;
using System.Collections.Generic;
using UnityEngine;

namespace Lair.Character
{
    //# 캐릭터 GameObject 당 서비스 로케이터 — 흩어진 GetComponent<IXxx> 를 단일 진입점으로 총괄.
    //# 얇은 접근자만 (게임 로직·상태 없음, Rule 02 §5). lazy 해석이라 실행순서/Awake 선행에 비의존.
    //# 클래스명이 네임스페이스 leaf(Lair.Character)와 겹쳐 외부 네임스페이스에서 CS0118 → LairCharacter 로 명명.
    [DefaultExecutionOrder(-1000)]
    public class LairCharacter : MonoBehaviour
    {
        //# 해석된 서비스 캐시 — non-null 만 저장. 미부착 서비스는 미캐싱(재호출 시 재해석).
        private readonly Dictionary<Type, object> _services = new();

        //# lazy — 최초 호출 시 GetComponent 로 해석·캐싱. 소비자 Awake 시점에 불려도 Character.Awake 선행 불필요.
        public T Get<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out object cached))
                return (T)cached;
            T service = GetComponent<T>();
            if (service != null)
                _services[typeof(T)] = service;
            return service;
        }

        public bool TryGet<T>(out T service) where T : class
        {
            service = Get<T>();
            return service != null;
        }

        public IHealth Health => Get<IHealth>();
        public IMover Mover => Get<IMover>();
        public IAttacker Attacker => Get<IAttacker>();
        public IRotator Rotator => Get<IRotator>();
        public ITargetProvider TargetProvider => Get<ITargetProvider>();
        public IAttackGate AttackGate => Get<IAttackGate>();
        public IDamageColorSink DamageColorSink => Get<IDamageColorSink>();
    }
}
