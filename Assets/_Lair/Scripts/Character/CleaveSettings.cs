using System.Collections.Generic;
using Lair.Data;
using UnityEngine;

namespace Lair.Character
{
    //# HeroArchetype_Cleave row → 불변 설정 (HeroArchetype_Cleave.json, chapter-4 §2.2). row 가 없는 영웅은 단일 판정 그대로.
    public readonly struct CleaveSettings
    {
        public readonly float Radius;
        public readonly int MaxTargets;
        public readonly float SecondaryMul;

        public CleaveSettings(float radius, int maxTargets, float secondaryMul)
        {
            Radius = radius;
            MaxTargets = maxTargets;
            SecondaryMul = secondaryMul;
        }

        public static CleaveSettings From(HeroArchetype_CleaveRow row)
        {
            return new CleaveSettings(row.cleaveRadius, row.cleaveMaxTargets, row.cleaveSecondaryMul);
        }

        //# 가까운 순 rank(0 시작) 번째 대상의 피해 배율 — 첫 대상 100%, 나머지 SecondaryMul.
        public float DamageMulOf(int rank)
        {
            return rank == 0 ? 1f : SecondaryMul;
        }
    }

    //# ICleaveTargetSource 실구현 — CharacterRegistry.Monsters 에서 교전 몬스터를 가까운 순으로 모은다.
    public class RegistryCleaveTargets : ICleaveTargetSource
    {
        private readonly List<KeyValuePair<float, IHealth>> _scratch = new List<KeyValuePair<float, IHealth>>();

        public int CollectNearest(Vector3 center, float radius, int maxTargets, List<IHealth> results)
        {
            results.Clear();
            _scratch.Clear();
            float sqrRadius = radius * radius;
            foreach (CharacterRegistry.Entry e in CharacterRegistry.Monsters)
            {
                if (e == null || e.Transform == null || e.Health == null || e.Health.IsAlive == false || e.IsEngaging == false)
                    continue;
                Vector3 rel = e.Transform.position - center;
                rel.y = 0f;
                float sqr = rel.sqrMagnitude;
                if (sqr > sqrRadius)
                    continue;
                _scratch.Add(new KeyValuePair<float, IHealth>(sqr, e.Health));
            }
            _scratch.Sort((a, b) => a.Key.CompareTo(b.Key));
            int count = Mathf.Min(maxTargets, _scratch.Count);
            for (int i = 0; i < count; ++i)
            {
                results.Add(_scratch[i].Value);
            }
            return count;
        }
    }
}
